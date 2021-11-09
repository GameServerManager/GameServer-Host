using GameServer.Core.Database.Logger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServer.Core.Logger
{
    public interface IPerformanceLogger : IDisposable
    {
        Task StartLogging(string id);
        Task StopLogging(string id);
        Task<List<DataPoint>> GetHistory(string id);
    }
}
