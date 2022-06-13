using System.ComponentModel;

namespace GameServer.Core.Daemon.Config
{
    public class ServerScripts
    {

        [DefaultValue(true)]
        public Script? InstallationScript { get; set; }

        [DefaultValue(true)]
        public Script? StartScript { get; set; }

        [DefaultValue(true)]
        public Script? UpdateScript { get; set; }
    }
}