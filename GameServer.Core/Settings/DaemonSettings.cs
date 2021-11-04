using System.ComponentModel;

namespace GameServer.Core.Settings
{
    public class DaemonSettings
    {
        [DefaultValue(true)]
        public string DaemonTest { get; set; } = "testDaemon";
        public ContainerSettings ContainerSettings { get; set; }
    }
}
