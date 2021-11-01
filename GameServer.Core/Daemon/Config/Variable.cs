namespace GameServer.Core.Daemon.Config
{
    public class Variable
    {
        public string Name { get; set;}
        public string Description { get; set;}
        public string EnvVariable { get; set;}
        public string DefaultValue { get; set;}
        public bool UserViewable { get; set;}
        public bool UserEditable { get; set;}
        //public Rules rules { get; set; }
    }
}