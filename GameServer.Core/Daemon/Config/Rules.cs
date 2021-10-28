namespace GameServer.Core.Daemon.Config
{
    public class Rules
    {

        public bool Required { get; set; } = false;
        public string Type { get; set; }
        public string Constrains { get; set; }
    }
}