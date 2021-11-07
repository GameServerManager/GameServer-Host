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
        private object AppenLock = new object ();
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

        public async Task AppendLog(string id, string message)
        {
            var db = _dbClient.GetDatabase(ServerDatabaseName);
            var collection = db.GetCollection<ServerEntity>(ServerCollectionName);
            var filter = Builders<ServerEntity>.Filter.Eq(server => server.ID, id);

            //var p = new BsonDocument[]
            //{
            //    new BsonDocument { 
            //        { "$set", new BsonDocument("Log", new BsonDocument { 
            //            { "$concat", new BsonDocument("$Log", message)} 
            //        })} 
            //    }
            //};
            var p = new BsonDocument[]
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
}")
            };

            var update = Builders<ServerEntity>.Update.Pipeline(p);
            await collection.UpdateOneAsync(filter, update);
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
