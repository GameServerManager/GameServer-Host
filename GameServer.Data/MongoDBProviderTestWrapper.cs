using GameServer.Core.Database.Daemon;
using GameServer.Core.Settings;
using GameServer.Data;
using Microsoft.Extensions.Logging;
using System;

namespace GameServer.Data
{
    public class MongoDBProviderTestWrapper : MongoDbProvider
    {
        public MongoDBProviderTestWrapper(IGameServerSettings gameServerSettings, ILogger<MongoDbProvider> logger) : base(gameServerSettings, logger)
        {}

        public async Task Delete()
        {
            await DbClient.DropDatabaseAsync(Settings.DbName);
        }

        public async Task ClearDatabase()
        {
            var db = DbClient.GetDatabase(Settings.DbName);
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