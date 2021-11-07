using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using GameServer.Core.Settings;

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

        private async Task InitCache(ContainerSettings settings)
        {
            var containerRequest = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                All = true
            });
            var containerCount = containerRequest.Count;

            var dbContainers = (await DataProvider.GetAllServerID()).ToList();

            List<Task> pool = new();

            for (int i = 0; i < containerCount; i++)
            {
                string id = containerRequest[i].ID;
                if (!dbContainers.Remove(id))
                {
                    Console.WriteLine($"[Warning] Not in Database: {id}");
                    continue;
                }

                var container = new DockerContainer(client, id);
                container.NewOutStreamMessage += OnNewOut;

                pool.Add(container.Start());
                ContainerCache.Add(id, container);
            }

            foreach (var notTraced in dbContainers)
            {
                Console.WriteLine($"[Warning] Not Tracked Import Again{notTraced}");
            }
            // warning if containers still has entries

            await Task.WhenAll(pool);
        }

        private void OnNewOut(object sender, OutEventArgs e)
        {
            var s = sender as IServer;

            DataProvider.AppendLog(s.ID, e.Message);
        }

        public async Task<IServer> GetServer(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (!contains)
                return null;
            return container;
        }

        public async Task<ServerStatus> GetServerStatus(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (!contains)
                return null;
            
            return await container.GetStatus(); 
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
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
                await container.Start();
        }

        public async Task<IServer[]> GetAllServer()
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
            if (!contains)
                throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);

            (_, string stdout) = container.GetLogs();
            return stdout;
        }

        public async Task Update(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (contains)
                await container.Update();
        }

        public void AttachServer(string id)
        {
            var contains = ContainerCache.TryGetValue(id, out var container);
            if (!contains)
                throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);

            Console.WriteLine($"{container.GetLogs().stdout}");

            container.NewOutStreamMessage += (s, e) =>
            {
                Console.Write($"{e.Message}");
            };
        }
    }
}
