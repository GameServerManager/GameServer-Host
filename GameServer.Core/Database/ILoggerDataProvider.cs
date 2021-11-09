using GameServer.Core.Database.Logger;

namespace GameServer.Core.Database
{
    public interface ILoggerDataProvider : IDatabaseProvider, IDisposable
    {
        Task AppendLogs(string id, DataPoint point);
        Task<List<DataPoint>> GetHistory(string id);
    }
}
