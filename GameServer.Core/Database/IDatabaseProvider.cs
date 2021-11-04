namespace GameServer.Core.Database
{
    public interface IDatabaseProvider
    {
        void Connect();
        void Disconnect();
    }
}
