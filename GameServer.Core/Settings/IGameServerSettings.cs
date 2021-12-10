namespace GameServer.Core.Settings
{
    public interface IGameServerSettings
    {
        DataProviderSettings ProviderSettings { get; set; }
        LoggingSettings LoggingSettings { get; set; }
        DaemonSettings DaemonSettings { get; set; }
    }
}