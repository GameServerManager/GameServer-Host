using System.ComponentModel;

namespace GameServer.Core.Settings
{
    public class LoggingSettings
    {
        [DefaultValue(true)]
        public string LoggingTest { get; set; } = "testLogging";
    }
}
