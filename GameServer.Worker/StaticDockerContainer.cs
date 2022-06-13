using Docker.DotNet;
using Docker.DotNet.Models;
using GameServer.Core.Daemon.Config;

namespace GameServer.Worker
{
    partial class DockerContainer
    {
        public static IList<string> FromConfig(DockerClient client, ServerConfig config, out DockerContainer container)
        {
            var binds = ConvertMountConfig(config);
            var ports = ConvertPortConfig(config.Ports);
            var env = ConvertEnvConfig(config.Variables);
            var exposedPorts = ConvertExposedPortsConfig(config.Ports);
            var hostConfig = ConvertHostConfig(binds, ports);

            ProcessScripts(config);

            var labels = new Dictionary<string, string?>()
            {
                { "GameServerManaged", "True"},
                { "GameServer.Name", config.Name},
                { "GameServer.Comment", config.Comment},
                { "GameServer.Description", config.Description},
            };

            var param = new Docker.DotNet.Models.CreateContainerParameters()
            {
                AttachStdin = true,
                AttachStderr = true,
                AttachStdout = true,
                Tty = true,
                HostConfig = hostConfig,
            };

            if (!string.IsNullOrEmpty(config.Image))
                param.Image = config.Image;

            if (!string.IsNullOrEmpty(config.Name))
                param.Name = config.Name;

            if (labels != null && labels.Count != 0)
                param.Labels = labels;

            if (env != null && env.Count != 0)
                param.Env = env;

            if (exposedPorts != null && exposedPorts.Count != 0)
                param.ExposedPorts = exposedPorts;

            client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = config.Image,
                        Tag = "latest"
                    },
                    null,
                    new Progress<JSONMessage>((m) => { }),
                    default).Wait();

            var containerInfo = client.Containers.CreateContainerAsync(param).Result;

            container = new DockerContainer(client, containerInfo.ID, env);
            return containerInfo.Warnings;
        }

        private static Docker.DotNet.Models.HostConfig ConvertHostConfig(List<string> binds, Dictionary<string?, IList<PortBinding>> ports)
        {
            var hostConfig = new Docker.DotNet.Models.HostConfig()
            {
                RestartPolicy = new()
                {
                    Name = Docker.DotNet.Models.RestartPolicyKind.UnlessStopped
                },
            };
            if (binds != null && binds.Count != 0)
                hostConfig.Binds = binds;

            if (ports != null && ports.Count != 0)
                hostConfig.PortBindings = ports;
            return hostConfig;
        }

        private static IDictionary<string?, EmptyStruct> ConvertExposedPortsConfig(PortMap[]? configPorts)
        {
            Dictionary<string?, EmptyStruct> ports = new();

            foreach (var port in configPorts)
                ports.Add(port.ServerPort, new Docker.DotNet.Models.EmptyStruct());

            return ports;
        }

        private static void ProcessScripts(ServerConfig config)
        {
            GenerateScript(config.ContainerScripts.StartScript, "StartScript", config.Name);
            GenerateScript(config.ContainerScripts.UpdateScript, "UpdateScript", config.Name);
            GenerateScript(config.ContainerScripts.InstallationScript, "InstallationScript", config.Name);
        }

        private static void GenerateScript(Script? script, string scriptName, string? contianerName)
        {
            string envVar = GetServerRootPath();

            var scriptPath = @$"{envVar}\{contianerName}\scripts\{scriptName}.sh";

            File.WriteAllText(scriptPath, script.ScriptCommand);
        }

        private static List<string> ConvertMountConfig(ServerConfig config)
        {
            string envVar = GetServerRootPath();

            var path = @$"{envVar}\{config.Name}";
            if (Directory.Exists(path))
                throw new ApplicationException("Server folder alreadt exists change name");

            DirectoryInfo dir = Directory.CreateDirectory(path);
            var invalidChars = Path.GetInvalidFileNameChars();
            DirectoryInfo scriptDir = Directory.CreateDirectory($@"{ dir.FullName }/scripts");

            List<string> binds = new()
            {
                { $@"{ scriptDir }:/Home/scripts" }
            };

            IEnumerable<string> collection()
            {
                foreach (var mount in config.Mounts)
                {
                    if (mount.HostPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Contains(".."))
                        throw new ApplicationException("Client tries traversal Attack");

                    Directory.CreateDirectory($@"{dir.FullName}/{mount.HostPath}");
                    yield return $@"{dir.FullName}/{mount.HostPath}:{mount.ServerPath}";
                }
            }

            binds.AddRange(collection());
            return binds;
        }

        private static bool IsSubfolder(string parentPath, string childPath)
        {

            return false;
        }

        private static string GetServerRootPath()
        {
            var envVar = Environment.GetEnvironmentVariable("ServerRoot");
            if (envVar == null)
                throw new ApplicationException("Env var not found run installer");
            return envVar;
        }

        private static List<string> ConvertEnvConfig(Variable[] configVariables)
        {
            List<string> env = new();

            foreach (var variable in configVariables)
            {
                if (string.IsNullOrEmpty(variable.EnvVariable))
                    env.Add($"{variable.Name}={variable.DefaultValue}");
                else
                    env.Add($"{variable.Name}={variable.EnvVariable}");
            }

            return env;
        }

        private static Dictionary<string?, IList<PortBinding>> ConvertPortConfig(PortMap[]? configPorts)
        {
            Dictionary<string?, IList<PortBinding>> ports = new();

            foreach (var port in configPorts)
            {
                List<Docker.DotNet.Models.PortBinding> hostPorts = new();
                hostPorts.AddRange(from hostPort in port.HostPorts
                                   select new Docker.DotNet.Models.PortBinding() { HostPort = hostPort });

                ports.Add(port.ServerPort, hostPorts);
            }

            return ports;
        }
    }
}
