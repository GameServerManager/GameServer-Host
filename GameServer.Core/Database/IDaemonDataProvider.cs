using GameServer.Core.Database.Daemon;

namespace GameServer.Core.Database
{
    public interface IDaemonDataProvider : IDatabaseProvider, IDisposable
    {
        Task<IEnumerable<string?>> GetAllServerId();
        Task<ServerEntity> ServerById(string? id);
        Task SaveServer(ServerEntity server);
        Task AppendLog(string? id, string scriptName, string execId, string targetStream, string message);
    }
}
