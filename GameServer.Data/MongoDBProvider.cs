using GameServer.Core.Database;
using GameServer.Core.Database.Daemon;
using GameServer.Core.Database.Logger;
using GameServer.Core.Settings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace GameServer.Data
{
    public class MongoDBProvider : IDaemonDataProvider, ILoggerDataProvider
    {
        internal readonly MongoClient _dbClient;
        private readonly string _connectionString;
        private object _ServerLogLock = new();
        private object _ScriptLogLock = new();
        internal DataProviderSettings _settings;

        internal IMongoCollection<ServerEntity> ServerCollection { get; private set; }
        internal Dictionary<string, IMongoCollection<DataPoint>> LoggerCollections { get; private set; } = new();

        public MongoDBProvider(IGameServerSettings gameServerSettings, ILogger<MongoDBProvider> logger)
        {
            _settings = gameServerSettings.ProviderSettings;
            _connectionString = $"mongodb://{_settings.UserName}:{_settings.Password}@{_settings.Host}:{_settings.Port}/";
            _dbClient = new MongoClient(_connectionString);
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
        public async Task<IEnumerable<string>> GetAllServerID()
        {
            var server = await ServerCollection.FindAsync(new BsonDocument());
            var a = await server.ToListAsync();
            return a.Select(s => s.ID);
        }

        public async Task SaveServer(ServerEntity server)
        {
            await ServerCollection.InsertOneAsync(server);
        }

        public async Task<ServerEntity> ServerByID(string id)
        {
            var filter = Builders<ServerEntity>.Filter.Eq(server => server.ID, id);

            var server = await ServerCollection.FindAsync(filter);
            return await server.FirstOrDefaultAsync();

        }

        public async Task AppendLog(string id, string scriptName, string execID, string targetStream, string message)
        {
            lock (_ScriptLogLock)
            {
                var appendQuery = UpdateQuery(scriptName, execID, targetStream, message);
                var appendFilter = Builders<ServerEntity>.Filter.Eq(server => server.ID, id);
                var appendUpdate = Builders<ServerEntity>.Update.Pipeline(appendQuery);

                var result = ServerCollection.UpdateMany(appendFilter, appendUpdate);
                if (result.ModifiedCount != 0)
                    return;

                InitLogField(id, scriptName, execID, message);
            }

        }

        private void InitLogField(string id, string scriptName, string execID, string message)
        {
            InitServerLogField(id, scriptName);
            InitScriptLogField(id, scriptName, execID, message);
            InitFirstMessage(id, scriptName, execID, message);
        }

        private void InitServerLogField(string id, string scriptName)
        {
            var filter = Builders<ServerEntity>.Filter;
            var serverIdFilter = filter.Eq(x => x.ID, id);

            var ServerLogs = ServerCollection.Find(serverIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName)).SingleOrDefault();

            if (ServerLogs == null)
            {
                var update2 = Builders<ServerEntity>.Update;
                var courseLevelSetter2 = update2.AddToSet("Log", new ServerLog() { ScriptName = scriptName });
                var res1 = ServerCollection.UpdateOne(serverIdFilter, courseLevelSetter2);
            }
        }

        private void InitScriptLogField(string id, string scriptName, string execID, string message)
        {
            var filter = Builders<ServerEntity>.Filter;
            var update1 = Builders<ServerEntity>.Update;
            var serverIdFilter = filter.Eq(x => x.ID, id);

            var ScriptLogs = ServerCollection.Find(serverIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName) & filter.Where(x => x.Log.Where(log => log.ScriptLogs.Where(scriptLog => scriptLog.ID == execID).Any()).Any())).SingleOrDefault();

            if (ScriptLogs == null)
            {
                var courseLevelSetter = update1.AddToSet("Log.$[n].ScriptLogs", new ScriptLog() { ID = execID });
                var res = ServerCollection.UpdateOne(serverIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter, new UpdateOptions()
                {
                    ArrayFilters = new List<ArrayFilterDefinition>
                        {
                            new JsonArrayFilterDefinition<ServerLog>("{'n.ScriptName': '" + scriptName + "'}"),
                        }
                });
            }
        }

        private void InitFirstMessage(string id, string scriptName, string execID, string message)
        {
            var filter = Builders<ServerEntity>.Filter;
            var serverIdFilter = filter.Eq(x => x.ID, id);
            var update1 = Builders<ServerEntity>.Update;

            var courseLevelSetter = update1.Set("Log.$[n].ScriptLogs.$[t].StdOut", message);
            var res = ServerCollection.UpdateOne(serverIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter, new UpdateOptions()
            {
                ArrayFilters = new List<ArrayFilterDefinition>
                    {
                        new JsonArrayFilterDefinition<ServerLog>("{'n.ScriptName': '" + scriptName + "'}"),
                        new JsonArrayFilterDefinition<ScriptLog>("{'t.ID': '" + execID + "'}")
                    }
            });
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

        public void Dispose()
        {
            Disconnect();
        }

        private BsonDocument[] UpdateQuery(string scriptName, string execID, string targetStream, string message)
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
                               [{'$ne': ['$$outer.ScriptName', '" + scriptName + @"']}, '$$outer',
                                 {'$mergeObjects': 
                                   ['$$outer',
                                     {'ScriptLogs': 
                                       {'$map': 
                                         {'input': '$$outer.ScriptLogs',
                                          'as': 'inner',
                                          'in': 
                                           {'$cond': 
                                             [{'$ne': ['$$inner.ID', '" + execID + @"']}, '$$inner',
                                               {'$mergeObjects': 
                                                 ['$$inner',
                                                   {'StdOut': 
                                                     {'$concat': 
                                                       ['$$inner.StdOut','" + message + @"']}}]}]}}}}]}]}}}}}
                ");

            return new BsonDocument[]
            {
                query1
            };
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

            string loggerDatabaseName = "Logger";

            var db = _dbClient.GetDatabase(loggerDatabaseName);
            var ids = await GetAllServerID();

            foreach (var id in ids)
            {
                LoggerCollections.Add(id, db.GetCollection<DataPoint>(id));
            }
        }

        private void InitServerDatabase()
        {
            BsonClassMap.RegisterClassMap<ServerEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.ID));
            });

            string serverDatabaseName = _settings.DbName;
            string serverCollectionName = "ServerEntitys";

            var db = _dbClient.GetDatabase(serverDatabaseName);
            ServerCollection = db.GetCollection<ServerEntity>(serverCollectionName);
        }
    }
}
