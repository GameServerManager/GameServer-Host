using GameServer.Core.Daemon.Config;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServer.Core.Daemon
{
    public interface IServer
    {
        string ID { get; }
        IList<string> Names { get; set; }
        delegate void NewOutHandler(object sender, OutEventArgs e);
        event NewOutHandler NewOutStreamMessage;

        Task Start();
        Task Stop();
        Task Install();
        Task Update();
        Task Exec(Script script, string name);
        Task<ServerStatus> GetStatus();
        Dictionary<string, Dictionary<string, (string stderr, string stdout)>> GetLogs();
    }
}
