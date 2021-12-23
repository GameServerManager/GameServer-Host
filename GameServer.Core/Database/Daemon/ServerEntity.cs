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
        public ServerLog[] Log { get; set; } = Array.Empty<ServerLog>();
        public ServerConfig? Config { get; set; }
    }
}