using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Database
{
    public interface IDaemonDataProvider : IDatabaseProvider, IDisposable
    {
        Task<List<string>> GetAllServerID();
        Task<IServerEntity> ServerByID(string id);
        Task SaveServer(string id);
        Task UpdateServer(string id);
    }
}
