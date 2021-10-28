using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using GameServer.Core;
using GameServer.Core.Daemon;

namespace GameServer.Worker
{
    public class DockerContainer : IContainer, IDisposable
    {
        private DockerClient client { get; }

        public string ID { get; }

        public IList<string> Names { get; set; }

        public string Image { get; set; }

        public string ImageID { get; set; }

        public DockerContainer(DockerClient client, string id)
        {
            this.client = client;
            ID = id;
            _ = Update();
        }

        private async Task Update()
        {
            var container = await GetOwnContainer();

            Image = container.Image;
            ImageID = container.ImageID;
            Names = container.Names;
        }

        public async Task Start()
        {
            await client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters()); 
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

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
