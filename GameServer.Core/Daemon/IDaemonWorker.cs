using GameServer.Core.Daemon.Config;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServer.Core.Daemon
{
    public interface IDaemonWorker : IDisposable
    {
        Task<IServer> GetServer(string id);
        Task<IServer[]> GetAllServer();
        Task<Dictionary<string, Dictionary<string, (string stderr, string stdout)>>> GetServerLogs(string id);
        Task<IList<string>> ImportServer(ServerConfig id);
        Task StartServer(string id);
        Task StopServer(string id);
        Task Update(string id);
        void AttachServer(string id, Action<string, string, OutEventArgs.TargetStream, string> callback);
        Task SendCommand(string containerID, string execId, string command);
    }
}
