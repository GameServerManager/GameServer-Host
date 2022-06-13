using GameServer.Core.Daemon.Config;

namespace GameServer.Core.Database.Daemon
{
    public class ServerEntity
    {
        public ServerEntity(string? id)
        {
            Id = id;
        }

        public string? Id { get; set; }
        public ServerLog[] Log { get; set; } = Array.Empty<ServerLog>();
        public ServerConfig? Config { get; set; }
    }
}