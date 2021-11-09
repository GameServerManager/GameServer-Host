using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private string StdoutCache { get; set; } = "";
        private string StderrCache { get; set; } = "";

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

        public (string stderr, string stdout) GetLogs()
        {
            return (StderrCache, StdoutCache);
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

            do
            {
                res = await stream.ReadOutputAsync(buffer, 0, 1, token.Token);

                if (res.Count != 0)
                    NewOutStreamMessage.Invoke(this, new OutEventArgs(System.Text.Encoding.Default.GetString(buffer), res.Target.ToString()));
            } while (!res.EOF);

        }

        private void OnOutStreamMessage(object sender, OutEventArgs e)
        {
            if (e.Target == OutEventArgs.TargetStream.StandardOut)
            {
                StdoutCache += e.Message;
            }
            else if (e.Target == OutEventArgs.TargetStream.StandardError)
            {
                StderrCache += e.Message;
            }
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
