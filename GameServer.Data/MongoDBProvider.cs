using GameServer.Core.Database;
using GameServer.Core.Settings;
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
        public IMongoCollection<ServerEntity> LoggerCollection { get; private set; }

        public MongoDBProvider(DataProviderSettings settings)
        {
            _connectionString = $"mongodb://{settings.UserName}:{settings.Password}@{settings.Host}:{settings.Port}/";
            _dbClient = new MongoClient(_connectionString);
        }


        #region IDatabaseProvider
        public void Connect()
        {
            InitLoggerDatabase();
            InitServerDatabase();
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

        private void InitLoggerDatabase()
        {
            string loggerDatabaseName = "Logger";
            string loggerCollectionName = "LoggerEntitys";

            var db = _dbClient.GetDatabase(loggerDatabaseName);
            LoggerCollection = db.GetCollection<ServerEntity>(loggerCollectionName);
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
