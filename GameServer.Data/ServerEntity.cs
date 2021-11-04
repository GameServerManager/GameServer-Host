using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using MongoDB.Bson.Serialization.Attributes;

namespace GameServer.Data
{
    internal class ServerEntity : IServerEntity
    {
        [BsonId]
        public string ID { get; set; }
        public string Log { get; set; }
        public ServerConfig Config { get; set; }
    }
}