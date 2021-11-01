using System.ComponentModel;

namespace GameServer.Core.Daemon.Config
{
    public class ContainerScripts
    {

        [DefaultValue(true)]
        public Script InstalationScript { get; set; } = null;

        [DefaultValue(true)]
        public Script StartScript { get; set; } = null;

        [DefaultValue(true)]
        public Script UpdateScript { get; set; } = null;
    }
}