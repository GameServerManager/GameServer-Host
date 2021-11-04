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
