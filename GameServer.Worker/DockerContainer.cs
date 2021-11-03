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

        private DockerClient _client { get; }
        private string _stdoutCache { get; set;} = "" ;
        private string _stderrCache { get; set; } = "" ;

        public string ID { get; }

        public IList<string> Names { get; set; }

        public string Image { get; set; }

        public string ImageID { get; set; }

        public DockerContainer(DockerClient client, string id)
        {
            this._client = client;
            ID = id;

            var container = GetOwnContainer().Result;
            Image = container.Image;
            ImageID = container.ImageID;
            Names = container.Names;
        }

        public async Task Start()
        {
            await _client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());

            await ExecFromName("StartScript");
        }

        public async Task Stop()
        {
            await _client.Containers.StopContainerAsync(ID,new Docker.DotNet.Models.ContainerStopParameters());
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
            var containerList = await _client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
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

            var exec = await _client.Exec.ExecCreateContainerAsync(ID, createParams);
            //await client.Exec.StartContainerExecAsync(exec.ID);
            var token = new CancellationTokenSource();


            var stream = await _client.Exec.StartAndAttachContainerExecAsync(exec.ID, true, token.Token);

            var buffer = new byte[1];
            var offset = 0;
            MultiplexedStream.ReadResult res;

            do
            {
                res = await stream.ReadOutputAsync(buffer, offset, 1, token.Token);

                if (res.Count != 0)
                    _stdoutCache += System.Text.Encoding.Default.GetString(buffer);

            } while (!res.EOF);

        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task Install()
        {
            await _client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("InstalationScript");
        }

        public async Task Update()
        {
            await ExecFromName("UpdateScript");
        }

        public async Task<(string stderr, string stdout)> GetLogs()
        {
            return (_stderrCache, _stdoutCache);
        }
    }
}
