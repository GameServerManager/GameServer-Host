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
    public class ServerConfig
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Discription { get; set; }
        public string Image { get; set; }

        [DefaultValue(true)]
        public MountingPoint[] Mounts { get; set; } = null;

        [DefaultValue(true)]
        public PortMap[] Ports { get; set; } = null;

        [DefaultValue(true)]
        public ServerScripts ContainerScripts { get; set; } = null;

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

        public static void ToFile(string path)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            ServerConfig config = new()
            {
                Name = "TestName",
                Comment = "TestComment",
                Discription = "TestDiscription",
                Mounts = new[]
                {
                    new MountingPoint()
                    {
                        HostPath = "./test",
                        ServerPath = "/Home/t1"
                    },
                    new MountingPoint()
                    {
                        HostPath = "./data",
                        ServerPath = "/Home/data"
                    }
                },
                Ports = new[]
                {
                    new PortMap()
                    {
                        HostPorts = new[]
                        {
                            "8080"
                        },
                        ServerPort = "8081"
                    },
                    new PortMap()
                    {
                        HostPorts = new[]
                        {
                            "80"
                        },
                        ServerPort = "81"
                    }
                },
                Image = "debian",
                ContainerScripts = new()
                {
                    InstalationScript = new()
                    {
                        Entrypoint = "bash",
                        ScriptCommand = "Start"
                    },
                    StartScript = new()
                    {
                        Entrypoint = "bash",
                        ScriptCommand = "Start"
                    },
                    UpdateScript = new()
                    {
                        Entrypoint = "bash",
                        ScriptCommand = "Start"
                    }
                },
                Variables = new[]
                {
                    new Variable()
                    {
                        Name = "P_Version",
                        Description = "testtestDes",
                        EnvVariable = "3.7",
                        DefaultValue = "3.7",
                        UserViewable = true,
                        UserEditable = true

                    }
                }
            };

            
            File.WriteAllText(path, serializer.Serialize(config));
        }
    }
}
