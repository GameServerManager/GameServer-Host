using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameServer.Core.Daemon.Config
{
    public class ContainerConfig
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Discription { get; set; }
        public string Images { get; set; }

        [DefaultValue(true)]
        public Script InstalationScript { get; set; } = null;

        [DefaultValue(true)]
        public Script StartScript { get; set; } = null;
        [DefaultValue(true)]
        public Variable[] Variables { get; set; } = new Variable[0];

        public static ContainerConfig FromFile(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)   
                .Build();

            //yml contains a string containing your YAML
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<ContainerConfig>(yaml);
        }
    }
}
