using GameServer.Core.Daemon.Config;

namespace GameServer.Core.Database.Daemon
{
    public class ServerEntity
    {
        public ServerEntity(string? iD)
        {
            ID = iD;
        }

        public string? ID { get; set; }
        public string? Log { get; set; }
        public ServerConfig? Config { get; set; }
    }
}