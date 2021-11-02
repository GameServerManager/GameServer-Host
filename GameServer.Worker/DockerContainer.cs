using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using GameServer.Core;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using Newtonsoft.Json;

namespace GameServer.Worker
{
    public partial class DockerContainer : IContainer, IDisposable
    {
        private CancellationTokenSource _cancellation;

        private DockerClient client { get; }
        private List<string> _stdoutCache { get; } = new List<string>();
        private List<string> _stderrCache { get; } = new List<string>();

        public string ID { get; }

        public IList<string> Names { get; set; }

        public string Image { get; set; }

        public string ImageID { get; set; }

        public DockerContainer(DockerClient client, string id)
        {
            this.client = client;
            ID = id;

            var container = GetOwnContainer().Result;
            Image = container.Image;
            ImageID = container.ImageID;
            Names = container.Names;
        }

        public async Task Start()
        {
            await client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());

            await ExecFromName("StartScript");
        }

        private void onMessage(string msg)
        {
            _stdoutCache.Add($"{msg}");
        }

        public async Task Stop()
        {
            await client.Containers.StopContainerAsync(ID,new Docker.DotNet.Models.ContainerStopParameters());
        }

        public async Task<ContainerStatus> GetStatus()
        {
            var container = await GetOwnContainer();

            return new ContainerStatus()
            {
                State = container.State,
                Status = container.Status
            };
        }

        private async Task<Docker.DotNet.Models.ContainerListResponse> GetOwnContainer()
        {
            var containerList = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "id",
                        new Dictionary<string, bool>
                        {
                            { ID, true}
                        }
                    }
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

        private async Task ExecFromName(string name, string endpoint = "/bin/bash")
        {
            var createParams = new Docker.DotNet.Models.ContainerExecCreateParameters()
            {
                AttachStdin = true,
                AttachStderr = true,
                AttachStdout = true,
                Tty = true,
                Cmd = new List<string>() { endpoint, "-c", $"/Home/scripts/{name}.sh", }
            };

            var exec = await client.Exec.ExecCreateContainerAsync(ID, createParams);
            //await client.Exec.StartContainerExecAsync(exec.ID);
            var token = new CancellationTokenSource();


            var stream = await client.Exec.StartAndAttachContainerExecAsync(exec.ID, true, token.Token);
            var (stdout, stderr) = await stream.ReadOutputToEndAsync(token.Token);
            if(!string.IsNullOrEmpty(stdout))
                _stdoutCache.Add(stdout);

            if(!string.IsNullOrEmpty(stderr))
                _stderrCache.Add(stderr);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task Install()
        {
            await client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("InstalationScript.sh");
        }

        public async Task Update()
        {
            await ExecFromName("UpdateScript");
        }

        public async Task<string[]> GetLogs()
        {
            var token = new CancellationTokenSource();
            var stream = await client.Containers.GetContainerLogsAsync(ID, true, new Docker.DotNet.Models.ContainerLogsParameters() { ShowStderr = true, ShowStdout = true}, token.Token);

            var (stdout, stderr) = await stream.ReadOutputToEndAsync(token.Token);

            return _stdoutCache.ToArray();
        }
    }
}
