using GameServer.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using GameServer.Core.Settings;

namespace GameServer.Data
{
    public class MongoDBProvider : IDataProvider
    {
        private string _connectionString;
        private MongoClient _dbClient;

        public MongoDBProvider(DataProviderSettings settings)
        {
            _connectionString = $"mongodb://{settings.UserName}:{settings.Password}@{settings.Host}:{settings.Port}/";
        }

        public void Connect()
        {
            _dbClient = new MongoClient(_connectionString);
            InitDatabase();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public T Read<T>(string Query) where T : DatabaseEntity
        {
            throw new NotImplementedException();
        }

        public void Write<T>(string Query) where T : DatabaseEntity
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void InitDatabase()
        {
            
        }
    }
}
