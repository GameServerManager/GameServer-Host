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
        Task<string> GetServerLogs(string id);
        Task<IList<string>> ImportServer(ServerConfig id);
        Task StartServer(string id);
        Task StopServer(string id);
        Task Update(string id);
        void AttachServer(string id, Action<string> callback);
    }
}
