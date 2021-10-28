using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Settings
{
    public class LoggingSettings
    {
        [DefaultValue(true)]
        public string LoggingTest { get; set; } = "testLogging";
    }
}
