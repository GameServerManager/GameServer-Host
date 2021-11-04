using GameServer.Core.Daemon.Config;

namespace GameServer.Core.Database
{
    public interface IServerEntity
    {
        public string ID { get; set; }
        public string Log { get; set; }
        public ServerConfig Config { get; set; }
    }
}