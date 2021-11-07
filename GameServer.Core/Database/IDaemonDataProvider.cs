namespace GameServer.Core.Database
{
    public interface IDaemonDataProvider : IDatabaseProvider, IDisposable
    {
        Task<IEnumerable<string>> GetAllServerID();
        Task<ServerEntity> ServerByID(string id);
        Task SaveServer(ServerEntity server);
        Task UpdateServer(string id, Func<ServerEntity, ServerEntity> p);
    }
}
