namespace GameServer.Core.Database.Daemon
{
    public class ServerLog
    {
        public string? ScriptName { get; set; }
        public ScriptLog[] ScriptLogs { get; set; } = Array.Empty<ScriptLog>();
    }
}