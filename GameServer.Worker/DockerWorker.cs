using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using GameServer.Core.Settings;

namespace GameServer.Worker
{
    public class DockerWorker : IDaemonWorker
    {
        Dictionary<string, DockerContainer> ContainerCache = new Dictionary<string, DockerContainer>();
        private DockerClient client;

        public IDataProvider DataProvider { get; }

        public DockerWorker(DaemonSettings settings, IDataProvider dataProvider)
        {

            DataProvider = dataProvider;
            client = new DockerClientConfiguration()
                .CreateClient();

            _ = InitCache(settings.ContainerSettings);
        }

        private async Task InitCache(ContainerSettings settings)
        {
            var containerRequest = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                All = true
            });
            var containerCount = containerRequest.Count;

            // FetchDatabase(); get container info from Database
            // comparte database to existing and running container 
            // remove all containers not managed by GameServer
            List<string> containers = new()
            {
                "44853dc05afbec1227f06d1302c7a167c99bf90660a0a1cb4bf03c6a6043647e",
                "bfbc9e28f48964d0bde361857fba8981f0698d792541b58c2fefcf8121dccdb0",
                "fdf99e181c6e5b6751918b58134b19b1caf46a07dadf35e83320226bd5d45f20"
            };

            List<Task> pool = new();

            for (int i = 0; i < containerCount; i++)
            {
                if (!containers.Remove(containerRequest[i].ID))
                    continue;

                var container = new DockerContainer(client, containerRequest[i].ID);
                pool.Add(container.Start());
                ContainerCache.Add(containerRequest[i].ID, container);
            }

            // warning if containers still has entries

            await Task.WhenAll(pool);
        }

        public async Task<IContainer> GetServer(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            return container;
        }

        public async Task<ContainerStatus> GetServerStatus(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            return await container.GetStatus();
        }

        public async Task<IList<string>> ImportServer(ContainerConfig config)
        {
            var warnings = DockerContainer.FromConfig(client, config, out var container);
            ContainerCache.Add(container.ID, container);
            await container.Install();
            await container.Start();
            return warnings;
        }

        public async Task StartServer(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
                await container.Start();
        }

        public async Task<IContainer[]> GetAllServer()
        {
            return ContainerCache.Values.ToArray();
        }

        public async Task StopServer(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
                await container.Stop();
        }

        public void Dispose()
        {
            foreach (var id in ContainerCache.Keys)
            {
                ContainerCache.Remove(id, out var container);
                container?.Stop();
                container?.Dispose();
            }
        }

        public async Task<string> GetServerLogs(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
            {
                (string stderr, string stdout) = await container.GetLogs();
                return stdout;
            }

            return "";
        }

        public async Task Update(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
                await container.Update();
        }
    }
}
