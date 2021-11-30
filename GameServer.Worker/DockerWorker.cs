using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using GameServer.Core.Database.Daemon;
using GameServer.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServer.Worker
{
    public class DockerWorker : IDaemonWorker
    {
        readonly Dictionary<string, DockerContainer> ContainerCache = new();
        private readonly DockerClient client;

        public IDaemonDataProvider DataProvider { get; }

        public DockerWorker(DaemonSettings settings, IDaemonDataProvider dataProvider)
        {

            DataProvider = dataProvider;
            client = new DockerClientConfiguration()
                .CreateClient();

            _ = InitCache(settings.ContainerSettings);
        }

        public async Task<IServer> GetServer(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                return container;
            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task<ServerStatus> GetServerStatus(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                return await container.GetStatus();

            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task<IList<string>> ImportServer(ServerConfig config)
        {
            var warnings = DockerContainer.FromConfig(client, config, out var container);
            ContainerCache.Add(container.ID, container);
            await DataProvider.SaveServer(new ServerEntity(container.ID) { Config = config, Log = "" }) ;
            container.NewOutStreamMessage += OnNewOut;
            await container.Install();
            return warnings;
        }

        public async Task StartServer(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                await container.Start();
        }

        public async Task<IServer[]> GetAllServer()
        {
            return ContainerCache.Values.ToArray();
        }

        public async Task StopServer(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                await container.Stop();
        }

        public async Task<string> GetServerLogs(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                return container.GetLogs().stdout;

            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task Update(string id)
        {
            if (ContainerCache.TryGetValue(id, out var container))
                await container.Update();
        }

        public void AttachServer(string id, Action<string> callback)
        {
            if (!ContainerCache.TryGetValue(id, out var container))
                throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);

            Console.WriteLine($"{container.GetLogs().stdout}");

            container.NewOutStreamMessage += (s, e) =>
            {
                callback(e.Message);
                Console.Write($"{e.Message}");
            };
        }

        private async Task InitCache(ContainerSettings settings)
        {
            var containerRequest = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                All = true
            });

            var dbContainers = (await DataProvider.GetAllServerID()).ToList();

            List<Task> pool = new();

            for (int i = 0; i < containerRequest.Count; i++)
            {
                string id = containerRequest[i].ID;
                if (!dbContainers.Remove(id))
                {
                    Console.WriteLine($"[Warning] Not in Database: {id}");
                    continue;
                }

                var container = new DockerContainer(client, id, null);
                container.NewOutStreamMessage += OnNewOut;

                pool.Add(container.Start());
                ContainerCache.Add(id, container);
            }

            foreach (var notTraced in dbContainers)
                Console.WriteLine($"[Warning] Not Tracked Import Again{notTraced}");

            await Task.WhenAll(pool);
        }

        private void OnNewOut(object sender, OutEventArgs e)
        {
            var s = sender as IServer;

            DataProvider.AppendLog(s.ID, e.Message);
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
    }
}
