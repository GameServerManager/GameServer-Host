using GameServer.Core.Daemon.Config;

namespace GameServer.Core.Daemon
{
    public interface IDaemonWorker : IDisposable
    {
        Task<IServer> GetServer(string? id);
        Task<IServer[]> GetAllServer();
        Task<Dictionary<string, Dictionary<string, (string stderr, string stdout)>>> GetServerLogs(string? id);
        Task<string?> ImportServer(ServerConfig id);
        Task StartServer(string? id);
        Task StopServer(string? id);
        Task Update(string? id);
        void AttachServer(string? id, Action<string, string, OutEventArgs.TargetStream, string, string> callback);
        Task SendCommand(string? containerId, string execId, string command);
    }
}
