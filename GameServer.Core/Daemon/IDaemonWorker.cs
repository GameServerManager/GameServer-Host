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
        Task<IList<string>> ImportServer(ContainerConfig id);
        Task StopServer(string id);
        Task<IContainer> GetServer(string id);
        Task<IContainer[]> GetAllServer();
        Task<string[]> GetServerLogs(string v);
    }
}
