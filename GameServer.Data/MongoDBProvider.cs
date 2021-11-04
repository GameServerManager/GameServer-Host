using GameServer.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using GameServer.Core.Settings;
using MongoDB.Bson.Serialization;

namespace GameServer.Data
{
    public class MongoDBProvider : IDaemonDataProvider, ILoggerDataProvider
    {
        private readonly string _connectionString;
        private MongoClient _dbClient;

        public MongoDBProvider(DataProviderSettings settings)
        {
            _connectionString = $"mongodb://{settings.UserName}:{settings.Password}@{settings.Host}:{settings.Port}/";
        }

        public void Connect()
        {
            _dbClient = new MongoClient(_connectionString);
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

        void IDatabaseProvider.Connect()
        {
            throw new NotImplementedException();
        }

        void IDatabaseProvider.Disconnect()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        Task<List<string>> IDaemonDataProvider.GetAllServerID()
        {
            throw new NotImplementedException();
        }

        private void InitLoggerDatabase()
        {
            BsonClassMap.RegisterClassMap<ServerEntity>();


            var db = _dbClient.GetDatabase("Server");
        }

        private void InitServerDatabase()
        {
            var db = _dbClient.GetDatabase("Server");
        }

        Task IDaemonDataProvider.SaveServer(string id)
        {
            throw new NotImplementedException();
        }

        Task<IServerEntity> IDaemonDataProvider.ServerByID(string id)
        {
            throw new NotImplementedException();
        }

        Task IDaemonDataProvider.UpdateServer(string id)
        {
            throw new NotImplementedException();
        }
    }
}
