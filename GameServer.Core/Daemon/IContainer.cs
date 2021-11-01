using GameServer.Core.Daemon.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Daemon
{
    public interface IContainer
    {
        string ID { get; }

        IList<string> Names { get; set; }

        Task Start();
        Task Stop();
        Task Install();
        Task Update();
        Task Exec(Script script);
        Task<ContainerStatus> GetStatus();
        Task<string[]> GetLogs();
    }
}
