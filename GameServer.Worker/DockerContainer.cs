using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer.Worker
{
    public partial class DockerContainer : IServer, IDisposable
    {
        public string ID { get; }
        public List<string>? Env { get; }
        public string ImageID { get; set; }
        public string Image { get; set; }
        public IList<string> Names { get; set; }
        public event IServer.NewOutHandler NewOutStreamMessage;
        private DockerClient Client { get; }

        private IOCache ioCache { get; } = new IOCache();
        private Dictionary<string, MultiplexedStream> StdIn = new Dictionary<string, MultiplexedStream>();
        public DockerContainer(DockerClient client, string id, List<string>? env)
        {
            Client = client;
            ID = id;
            Env = env;
            var container = GetOwnContainer().Result;
            Image = container.Image;
            ImageID = container.ImageID;
            Names = container.Names;
            NewOutStreamMessage += OnOutStreamMessage;
        }

        public async Task Start()
        {
            await Client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("StartScript");
        }

        public async Task Stop()
        {
            foreach(var stdIn in StdIn.Keys)
            {
                NewOutStreamMessage.Invoke(this, new OutEventArgs("", "None", stdIn, "", "closed"));
            }
            ioCache.Clear();
            await Client.Containers.StopContainerAsync(ID, new Docker.DotNet.Models.ContainerStopParameters());
        }

        public async Task<ServerStatus> GetStatus()
        {
            var container = await GetOwnContainer();

            return new ServerStatus()
            {
                State = container.State,
                Status = container.Status
            };
        }

        private async Task<Docker.DotNet.Models.ContainerListResponse> GetOwnContainer()
        {
            var containerList = await Client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "id", new Dictionary<string, bool>{ { ID, true} } }
                },
                All = true
            });
            return containerList.First();
        }

        public async Task Exec(Script script, string name)
        {
            if (name is null)
                name = Guid.NewGuid().ToString();

            GenerateScript(script, name, this.Names.First());
            await ExecFromName(name);
        }

        public async Task Install()
        {
            await Client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("InstalationScript");
        }

        public async Task Update()
        {
            await Client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("UpdateScript");
        }

        public Dictionary<string, Dictionary<string, (string stderr, string stdout)>> GetLogs()
        {
            return ioCache.GetAll();
        }

        public async Task Interact(string execId, string command)
        {
            var info = await Client.Exec.InspectContainerExecAsync(execId);

            if (info.ContainerID != ID)
                throw new ApplicationException("Wrong Container ID");
            if (!info.Running)
                return;

            if (StdIn.TryGetValue(execId, out var stream))
            {
                var token = new CancellationTokenSource();

                byte[] buffer = Encoding.ASCII.GetBytes(command);
                await stream.WriteAsync(buffer, 0, buffer.Length, token.Token);
            }

        }

        private async Task ExecFromName(string name, string endpoint = "/bin/bash")
        {
            var createParams = new Docker.DotNet.Models.ContainerExecCreateParameters()
            {
                AttachStdin = true,
                AttachStderr = true,
                AttachStdout = true,
                Tty = true,
                Env = Env,
                Cmd = new List<string>() { endpoint, "-c", $"/Home/scripts/{name}.sh", }
            };

            var exec = await Client.Exec.ExecCreateContainerAsync(ID, createParams);
            var token = new CancellationTokenSource();
            var stream = await Client.Exec.StartAndAttachContainerExecAsync(exec.ID, true, token.Token);

            var buffer = new byte[1];
            MultiplexedStream.ReadResult res;
            StdIn.Add(exec.ID, stream);
            do
            {
                res = await stream.ReadOutputAsync(buffer, 0, 1, token.Token);

                if (res.Count != 0)
                    NewOutStreamMessage.Invoke(this, new OutEventArgs(System.Text.Encoding.Default.GetString(buffer), res.Target.ToString(), exec.ID, name, "message"));
            } while (!res.EOF);

            NewOutStreamMessage.Invoke(this, new OutEventArgs("", "None", exec.ID, name, "closed"));
            StdIn.Remove(exec.ID);
        }

        private void OnOutStreamMessage(object sender, OutEventArgs e)
        {
            if (e.Type == "closed")
            {
                ioCache.Remove(e.ExecID, e.ScriptName);
            }
            else if (e.Type == "message")
            {
                ioCache.Add(e.ExecID, e.ScriptName, e.Target, e.Message);
            }
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
