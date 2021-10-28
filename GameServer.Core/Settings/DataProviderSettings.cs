using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Settings
{
    public class DataProviderSettings
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        [DefaultValue(true)]
        public string Host { get; set; } = "localhost";
        [DefaultValue(true)]
        public string Port { get; set; } = "27017";

    }
}
