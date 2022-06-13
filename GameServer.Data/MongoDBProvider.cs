using GameServer.Core.Database;
using GameServer.Core.Database.Daemon;
using GameServer.Core.Database.Logger;
using GameServer.Core.Settings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace GameServer.Data
{
    public class MongoDbProvider : IDaemonDataProvider, ILoggerDataProvider
    {
        internal readonly MongoClient DbClient;
        private readonly object _scriptLogLock = new();
        internal DataProviderSettings? Settings;

        internal IMongoCollection<ServerEntity>? ServerCollection { get; private set; }
        internal Dictionary<string, IMongoCollection<DataPoint>> LoggerCollections { get; } = new();

        public MongoDbProvider(IGameServerSettings gameServerSettings, ILogger<MongoDbProvider> logger)
        {
            Settings = gameServerSettings.ProviderSettings;
            var connectionString = $"mongodb://{Settings?.UserName}:{Settings?.Password}@{Settings?.Host}:{Settings?.Port}/";
            DbClient = new MongoClient(connectionString);
            Connect();
        }

        #region IDatabaseProvider
        public void Connect()
        {
            InitServerDatabase();
            InitLoggerDatabase();
        }

        public void Disconnect(){}
        #endregion

        #region IDaemonDataProvider
        public async Task<IEnumerable<string?>> GetAllServerId()
        {
            var server = await ServerCollection.FindAsync(new BsonDocument());
            var a = await server.ToListAsync();
            return a.Select(s => s.Id);
        }

        public async Task SaveServer(ServerEntity server)
        {
            await ServerCollection?.InsertOneAsync(server);
        }

        public async Task<ServerEntity> ServerById(string? id)
        {
            var filter = ServerIdFilter(id);

            var server = await ServerCollection.FindAsync(filter);
            return await server.FirstOrDefaultAsync();

        }

        public async Task AppendLog(string? id, string scriptName, string execId, string targetStream, string message)
        {
            lock (_scriptLogLock)
            {
                var appendQuery = UpdateQuery(scriptName, execId, targetStream, message);
                var appendFilter = ServerIdFilter(id);
                var appendUpdate = Builders<ServerEntity>.Update.Pipeline(appendQuery);

                var result = ServerCollection?.UpdateMany(appendFilter, appendUpdate);
                if (result?.ModifiedCount != 0)
                    return;

                InitLogField(id, scriptName, execId, message);
            }

        }

        private void InitLogField(string? id, string scriptName, string execId, string message)
        {
            InitServerLogField(id, scriptName);
            InitScriptLogField(id, scriptName, execId, message);
            InitFirstMessage(id, scriptName, execId, message);
        }

        private void InitServerLogField(string? id, string scriptName)
        {
            var scriptFilter = ScriptFilter(id, scriptName);
            var serverIdFilter = ServerIdFilter(id);
            var serverLogs = ServerCollection?.Find(scriptFilter).SingleOrDefault();

            if (serverLogs != null) 
                return;

            var update = Builders<ServerEntity>.Update;
            var courseLevelSetter = update.AddToSet("Log", new ServerLog { ScriptName = scriptName });
            ServerCollection?.UpdateOne(serverIdFilter, courseLevelSetter);
        }

        private void InitScriptLogField(string? id, string scriptName, string execId, string message)
        {
            var update1 = Builders<ServerEntity>.Update;
            var execFilter = ExecFilter(id, scriptName, execId);

            var scriptLogs = ServerCollection.Find(execFilter).SingleOrDefault();

            if (scriptLogs != null) 
                return;

            var scriptFilter = ScriptFilter(id, scriptName);
            var courseLevelSetter = update1.AddToSet("Log.$[i].ScriptLogs", new ScriptLog() { Id = execId });
            var options = new UpdateOptions()
            {
                ArrayFilters = new List<ArrayFilterDefinition>
                {
                    new JsonArrayFilterDefinition<ServerLog>("{'i.ScriptName': '" + scriptName + "'}"),
                }
            };
            ServerCollection?.UpdateOne(scriptFilter, courseLevelSetter, options);
        }

        private void InitFirstMessage(string? id, string scriptName, string execId, string message)
        {
            var update1 = Builders<ServerEntity>.Update;
            var scriptFilter = ScriptFilter(id, scriptName);
            var arrayOptions = new UpdateOptions()
            {
                ArrayFilters = new List<ArrayFilterDefinition>
                {
                    new JsonArrayFilterDefinition<ServerLog>("{'i.ScriptName': '" + scriptName + "'}"),
                    new JsonArrayFilterDefinition<ScriptLog>("{'j._id': '" + execId + "'}")
                }
            };

            var courseLevelSetter = update1.Set("Log.$[i].ScriptLogs.$[j].StdOut", message);
            ServerCollection?.UpdateOne(scriptFilter, courseLevelSetter, arrayOptions);
        }

        private static FilterDefinition<ServerEntity> ExecFilter(string? id, string scriptName, string execId)
        {
            var filter = Builders<ServerEntity>.Filter;
            var scriptFilter = ScriptFilter(id, scriptName);
            var execIdFilter = filter.Where(x => x.Log.Any(
                log => log.ScriptLogs.Any(
                    scriptLog => scriptLog.Id == execId)));
            var execFilter = scriptFilter & execIdFilter;
            return execFilter;
        }

        private static FilterDefinition<ServerEntity> ServerIdFilter(string? id)
        {
            var filter = Builders<ServerEntity>.Filter;
            var serverIdFilter = filter.Eq(x => x.Id, id);
            return serverIdFilter;
        }

        private static FilterDefinition<ServerEntity> ScriptFilter(string? id, string scriptName)
        {
            var filter = Builders<ServerEntity>.Filter;
            var serverIdFilter = ServerIdFilter(id);
            var scriptNameFilter = filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName);
            var scriptFilter = serverIdFilter & scriptNameFilter;
            return scriptFilter;
        }

        #endregion

        #region ILoggerDataProvider
        public async Task AppendLogs(string id, DataPoint point)
        {
            await LoggerCollections[id].InsertOneAsync(point);
        }

        public async Task<List<DataPoint>> GetHistory(string id)
        {
            var collection = await LoggerCollections[id].FindAsync(Builders<DataPoint>.Filter.Empty);
            var history = await collection.ToListAsync();
            return history;
        }
        #endregion

        public void Dispose() => Disconnect();

        private static BsonDocument[] UpdateQuery(string scriptName, string execId, string targetStream, string message)
        {
            var query1 = BsonDocument.Parse(
                @"
                    {'$set': 
                       {'Log': 
                         {'$map': 
                           {'input': '$Log',
                            'as': 'outer',
                            'in': 
                             {'$cond': 
                               [{'$ne': ['$$outer.ScriptName', '" + JsonEscape(scriptName) + @"']}, '$$outer',
                                 {'$mergeObjects': 
                                   ['$$outer',
                                     {'ScriptLogs': 
                                       {'$map': 
                                         {'input': '$$outer.ScriptLogs',
                                          'as': 'inner',
                                          'in': 
                                           {'$cond': 
                                             [{'$ne': ['$$inner._id', '" + JsonEscape(execId) + @"']}, '$$inner',
                                               {'$mergeObjects': 
                                                 ['$$inner',
                                                   {'StdOut': 
                                                     {'$concat': 
                                                       ['$$inner.StdOut','" + JsonEscape(message) + @"']}}]}]}}}}]}]}}}}}
                ");
            return new []
            {
                query1
            };
        }

        private static string JsonEscape(string unescaped)
        {
            var escaped = unescaped.Replace(@"\", @"\\")
                                   .Replace("\b", @"\b")
                                   .Replace("\f", @"\f")
                                   .Replace("\n", @"\n")
                                   .Replace("\r", @"\r")
                                   .Replace("\t", @"\t")
                                   .Replace("'", @"\'")
                                   .Replace("\"", "\\\"")
                                   .Replace("\0", "");
            return escaped;
        }

        private async void InitLoggerDatabase()
        {
            BsonClassMap.RegisterClassMap<DataPoint>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            BsonClassMap.RegisterClassMap<CpuStats>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.CpuUsage);
            });
            BsonClassMap.RegisterClassMap<MemoryStats>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.MemoryUsage);
            });

            const string loggerDatabaseName = "Logger";

            var db = DbClient.GetDatabase(loggerDatabaseName);
            var ids = await GetAllServerId();

            foreach (var id in ids)
            {
                if (id != null) 
                    LoggerCollections.Add(id, db.GetCollection<DataPoint>(id));
            }
        }

        private void InitServerDatabase()
        {
            BsonClassMap.RegisterClassMap<ServerEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
            });

            var serverDatabaseName = Settings?.DbName;
            const string serverCollectionName = "ServerEntitys";

            var db = DbClient.GetDatabase(serverDatabaseName);
            ServerCollection = db.GetCollection<ServerEntity>(serverCollectionName);
        }
    }
}
