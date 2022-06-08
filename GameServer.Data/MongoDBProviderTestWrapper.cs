using GameServer.Core.Database.Daemon;
using GameServer.Core.Settings;
using GameServer.Data;
using Microsoft.Extensions.Logging;
using System;

namespace GameServer.Data
{
    public class MongoDBProviderTestWrapper : MongoDBProvider
    {
        public MongoDBProviderTestWrapper(IGameServerSettings gameServerSettings, ILogger<MongoDBProvider> logger) : base(gameServerSettings, logger)
        {}

        public async Task Delete()
        {
            await _dbClient.DropDatabaseAsync(_settings.DbName);
        }

        public async Task ClearDatabase()
        {
            var db = _dbClient.GetDatabase(_settings.DbName);
            await db.DropCollectionAsync("ServerEntitys");
        }

        public async Task FillDatabase(List<ServerEntity> initDBValues)
        {
            foreach (var server in initDBValues)
            {
                await SaveServer(server);
            }
        }
    }
}