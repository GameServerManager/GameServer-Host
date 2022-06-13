using System.ComponentModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameServer.Core.Daemon.Config
{
    public class ServerConfig
    {
        public string? Name { get; set; }
        public string? Comment { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }

        [DefaultValue(true)]
        public MountingPoint[]? Mounts { get; set; }

        [DefaultValue(true)]
        public PortMap[]? Ports { get; set; }

        [DefaultValue(true)]
        public ServerScripts? ContainerScripts { get; set; }

        [DefaultValue(true)]
        public Variable[] Variables { get; set; } = Array.Empty<Variable>();

        public static ServerConfig FromFile(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            //yml contains a string containing your YAML
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<ServerConfig>(yaml);
        }
    }
}
