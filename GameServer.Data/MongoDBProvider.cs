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
        private readonly MongoClient _dbClient;
        private readonly string _connectionString;
        private object _ServerLogLock = new();
        private object _ScriptLogLock = new();

        public IMongoCollection<ServerEntity> ServerCollection { get; private set; }
        public Dictionary<string, IMongoCollection<DataPoint>> LoggerCollections { get; private set; } = new();

        public MongoDBProvider(IGameServerSettings gameServerSettings, ILogger<MongoDBProvider> logger)
        {
            DataProviderSettings settings = gameServerSettings.ProviderSettings;
            _connectionString = $"mongodb://{settings.UserName}:{settings.Password}@{settings.Host}:{settings.Port}/";
            _dbClient = new MongoClient(_connectionString);
            Connect();
        }

        #region IDatabaseProvider
        public void Connect()
        {
            InitServerDatabase();
            InitLoggerDatabase();
        }

        public void Disconnect()
        {

        }
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
            return await server.FirstAsync();

        }

        public async Task AppendLog(string id, string scriptName, string execID, string targetStream, string message)
        {
            //var filter = Builders<ServerEntity>.Filter.Eq(server => server.ID , id);
            
            var filter = Builders<ServerEntity>.Filter;
            var studentIdAndCourseIdFilter = filter.Eq(x => x.ID, id);
            // find student with id and course id
            lock (_ServerLogLock)
            {

                var ServerLogs = ServerCollection.Find(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName)).SingleOrDefault();

                if (ServerLogs == null)
                {
                    var update2 = Builders<ServerEntity>.Update;
                    var courseLevelSetter2 = update2.AddToSet("Log", new ServerLog() { ScriptName = scriptName});
                    var res1 = ServerCollection.UpdateOne(studentIdAndCourseIdFilter, courseLevelSetter2);
                }
            }

            lock (_ScriptLogLock)
            {
                var ScriptLogs = ServerCollection.Find(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName) & filter.Where(x => x.Log.Where(log => log.ScriptLogs.Where(scriptLog => scriptLog.ID == execID).Any()).Any())).SingleOrDefault();

                var update1 = Builders<ServerEntity>.Update;
                UpdateDefinition<ServerEntity> courseLevelSetter1;
                if (ScriptLogs == null)
                {
                    courseLevelSetter1 = update1.AddToSet("Log.$[n].ScriptLogs", new ScriptLog() { ID = execID });
                    var res2 = ServerCollection.UpdateOne(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter1, new UpdateOptions()
                    {
                        ArrayFilters = new List<ArrayFilterDefinition>
                        {
                            new JsonArrayFilterDefinition<ServerLog>("{'n.ScriptName': '" + scriptName + "'}"),
                        }
                    });
                   
                    var courseLevelSetter2 = update1.Set("Log.$[n].ScriptLogs.$[t].StdOut", message);
                    var res3 = ServerCollection.UpdateOne(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter2, new UpdateOptions() 
                    { 
                        ArrayFilters = new List<ArrayFilterDefinition>
                        {
                            new JsonArrayFilterDefinition<ServerLog>("{'n.ScriptName': '" + scriptName + "'}"),
                            new JsonArrayFilterDefinition<ScriptLog>("{'t.ID': '" + execID + "'}")
                        }
                    });

                }
                else
                {
                    var a = ScriptLogs.Log.Where(log => log.ScriptLogs.Where(scriptLog => scriptLog.ID == execID).Any()).Select(p => p.ScriptLogs).First().First();
                    var courseLevelSetter2 = update1.Set("Log.$[n].ScriptLogs.$[t].StdOut", a.StdOut + message);
                    var res3 = ServerCollection.UpdateOne(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter2, new UpdateOptions()
                    {
                        ArrayFilters = new List<ArrayFilterDefinition>
                        {
                            new JsonArrayFilterDefinition<ServerLog>("{'n.ScriptName': '" + scriptName + "'}"),
                            new JsonArrayFilterDefinition<ScriptLog>("{'t.ID': '" + execID + "'}")
                        }
                    });
                    //if (targetStream == "StandardOut")
                    //{
                    //    courseLevelSetter1 = update1.Set("Log.$.ScriptLogs.$.StdOut", ScriptLogs. message );
                    //}
                    //else
                    //{
                    //    courseLevelSetter1 = update1.AddToSet("Log.$.ScriptLogs.$.StdErr", new ScriptLog() { ID = execID, StdErr = message, StdOut = "" });
                    //}
                }
            }

            // update with positional operator
            //var update = Builders<ServerEntity>.Update;
            //var courseLevelSetter = update.AddToSet("Log.$.ScriptLogs", new ScriptLog()
            //{
            //    ID = execID,
            //    StdOut = message
            //});
            //var res = ServerCollection.UpdateOne(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), courseLevelSetter);
            //var update = Builders<ServerEntity>.Update.Pipeline(UpdateQuery(scriptName, execID, targetStream, message));


            //var res = await ServerCollection.UpdateOneAsync(studentIdAndCourseIdFilter & filter.ElemMatch(x => x.Log, c => c.ScriptName == scriptName), update);
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
            if (targetStream == "StandardOut")
            {
                return new BsonDocument[]
                {
                    BsonDocument.Parse(
                        @"
                        { 
                            $set: { 
                                'Log.$[elem1].ScriptLogs.$[elem2].StdOut': 
                                { 
                                    $concat:[ '$Log.$[elem1].ScriptLogs.$[elem2].StdOut', '" + message + @"' ] 
                                } 
                            }
                        }")
                };
            }
            else
            {
                return new BsonDocument[]
                {
                    BsonDocument.Parse(
                        @"
                        { 
                            $set: { 
                                'Log.$[elem1].ScriptLogs.$[elem2].StdOut': 
                                { 
                                    $concat:[ '$Log.$[elem1].ScriptLogs.$[elem2].StdOut', '" + message + @"' ] 
                                } 
                            }
                        }")
                };
            }
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

            string serverDatabaseName = "Server";
            string serverCollectionName = "ServerEntitys";

            var db = _dbClient.GetDatabase(serverDatabaseName);
            ServerCollection = db.GetCollection<ServerEntity>(serverCollectionName);
        }
    }
}
