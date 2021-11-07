using GameServer.Core.Database;
using GameServer.Core.Settings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace GameServer.Data
{
    public class MongoDBProvider : IDaemonDataProvider, ILoggerDataProvider
    {
        private readonly string _connectionString;
        private MongoClient _dbClient;
        private readonly string ServerCollectionName = "ServerEntitys";
        private readonly string ServerDatabaseName = "Server";
        private readonly string LoggerDatabaseName = "Logger";

        public MongoDBProvider(DataProviderSettings settings)
        {
            _connectionString = $"mongodb://{settings.UserName}:{settings.Password}@{settings.Host}:{settings.Port}/";
            _dbClient = new MongoClient(_connectionString);
        }

        public void Connect()
        {
            InitLoggerDatabase();
            InitServerDatabase();
        }

        public void Disconnect()
        {

        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task<IEnumerable<string>> GetAllServerID()
        {
            var db = _dbClient.GetDatabase(ServerDatabaseName);
            var collection = db.GetCollection<ServerEntity>(ServerCollectionName);

            var server = await collection.FindAsync(new BsonDocument());
            var a = await server.ToListAsync();
            return a.Select(s => s.ID);
        }

        public async Task SaveServer(ServerEntity server)
        {
            var db = _dbClient.GetDatabase(ServerDatabaseName);
            var collection = db.GetCollection<ServerEntity>(ServerCollectionName);
            await collection.InsertOneAsync(server);
        }

        public async Task<ServerEntity> ServerByID(string id)
        {
            var db = _dbClient.GetDatabase(ServerDatabaseName);
            var collection = db.GetCollection<ServerEntity>(ServerCollectionName);

            var filter = Builders<ServerEntity>.Filter.Eq(server => server.ID, id);

            var server = await collection.FindAsync(filter);
            return await server.FirstAsync();

        }

        public async Task UpdateServer(string id, Func<ServerEntity, ServerEntity> p)
        {
            var a = this;
            var server = await ServerByID(id);

            var modifyServer = p(server);

            await SaveServer(modifyServer);
        }

        private void InitLoggerDatabase()
        {


            var db = _dbClient.GetDatabase(LoggerDatabaseName);
        }

        private void InitServerDatabase()
        {
            BsonClassMap.RegisterClassMap<ServerEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.ID));
            });

            var db = _dbClient.GetDatabase(ServerDatabaseName);
        }
    }
}
