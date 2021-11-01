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
            _cancellation = new CancellationTokenSource();

            var stream = await client.Containers.AttachContainerAsync(ID, true, new Docker.DotNet.Models.ContainerAttachParameters()
            {
                Stream = true,
                Stdin = true,
                Stdout = true,
                Stderr = true
            });

            MemoryStream stdin = null;
            MemoryStream stdout= null;
            MemoryStream stderr = null;
            await stream.CopyOutputToAsync(stdin, stdout, stderr, _cancellation.Token);



            await client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
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

        public async Task Exec(Script script)
        {

        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task Install()
        {

        }

        public async Task Update()
        {

        }

        public async Task<string[]> GetLogs()
        {
            return _stdoutCache.ToArray();
        }
    }
}
