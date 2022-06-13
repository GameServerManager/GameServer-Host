namespace GameServer.Core.Database.Daemon
{
    public class ScriptLog
    {
        public string? Id { get; set; }
        public string StdOut { get; set; } = "";
        public string StdErr { get; set; } = "";
}
}