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
    public class MongoDBProvider : IDaemonDataProvider, ILoggerDataProvider
    {
        private readonly MongoClient _dbClient;
        private readonly string _connectionString;

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

        public async Task AppendLog(string id, string message)
        {
            var filter = Builders<ServerEntity>.Filter.Eq(server => server.ID, id);
            var update = Builders<ServerEntity>.Update.Pipeline(UpdateQuery(message));

            await ServerCollection.UpdateOneAsync(filter, update);
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
        private BsonDocument[] UpdateQuery(string message)
        {
            return new BsonDocument[]
            {
                BsonDocument.Parse(
                    @"
                    { 
                        $set: {
                            Log:
                                { 
                                $concat:[ '$Log', '" + message + @"' ] 
                            }
                        }
                    }"
            )};
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
