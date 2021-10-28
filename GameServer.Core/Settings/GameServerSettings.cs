using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameServer.Core.Settings
{
    public class GameServerSettings
    {
        public DataProviderSettings ProviderSettings { get; set; }
        public LoggingSettings LoggingSettings { get; set; }
        public DaemonSettings DaemonSettings { get; set; }

        public static GameServerSettings FromFile(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)  // see height_in_inches in sample yml 
                .Build();

            //yml contains a string containing your YAML
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<GameServerSettings>(yaml);
        }
    }
}
