using GameServer.Core.Daemon.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Daemon
{
    public interface IDaemonWorker : IDisposable
    {
        Task StartServer(string id);
        Task<IList<string>> ImportServer(ServerConfig id);
        Task StopServer(string id);
        Task<IServer> GetServer(string id);
        Task<IServer[]> GetAllServer();
        Task<string> GetServerLogs(string id);
        Task Update(string id);
    }
}
