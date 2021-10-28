using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Settings
{
    public class DaemonSettings
    {
        [DefaultValue(true)]
        public string DaemonTest { get; set; } = "testDaemon";
        public ContainerSettings ContainerSettings { get; set; } 
    }
}
