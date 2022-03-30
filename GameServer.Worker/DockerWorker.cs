using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using GameServer.Core.Database.Daemon;
using GameServer.Core.Settings;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DockerWorker> _logger;

        public IDaemonDataProvider DataProvider { get; }

        public DockerWorker(IGameServerSettings gameServerSettings, IDaemonDataProvider dataProvider, ILogger<DockerWorker> logger)
        {
            var settings = gameServerSettings.DaemonSettings;
            _logger = logger;
            DataProvider = dataProvider;
            client = new DockerClientConfiguration()
                .CreateClient();

            _ = InitCache(settings.ContainerSettings);
        }

        public async Task<IServer> GetServer(string id)
        {
            _logger.LogDebug($"Getting Server {id}");
            if (ContainerCache.TryGetValue(id, out var container))
                return container;

            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task<ServerStatus> GetServerStatus(string id)
        {
            _logger.LogDebug($"Getting Server Status {id}");
            if (ContainerCache.TryGetValue(id, out var container))
                return await container.GetStatus();

            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task<IList<string>> ImportServer(ServerConfig config)
        {
            _logger.LogDebug($"Import Server");
            var warnings = DockerContainer.FromConfig(client, config, out var container);
            ContainerCache.Add(container.ID, container);
            await DataProvider.SaveServer(new ServerEntity(container.ID) { Config = config}) ;
            container.NewOutStreamMessage += OnNewOut;
            _logger.LogDebug($"Installing Server {container.ID}");
            await container.Install();
            _logger.LogDebug($"Server {container.ID} installed");

            if (warnings.Count != 0)
                _logger.LogWarning($"Import Warning:");
                
            foreach (var warning in warnings)
            {
                _logger.LogWarning($"    {warning}");
            }
            return warnings;
        }

        public async Task StartServer(string id)
        {
            _logger.LogDebug($"Starting Server {id}");

            if (ContainerCache.TryGetValue(id, out var container))
                await container.Start();
        }

        public async Task<IServer[]> GetAllServer()
        {
            var containers = ContainerCache.Values.ToArray();
            _logger.LogDebug($"Server Count {containers.Length}");
            return containers;
        }

        public async Task StopServer(string id)
        {
            _logger.LogDebug($"Stopping Server {id}");

            if (ContainerCache.TryGetValue(id, out var container))
                await container.Stop();
        }

        public async Task<Dictionary<string, Dictionary<string, (string stderr, string stdout)>>> GetServerLogs(string id)
        {
            _logger.LogDebug($"Server Logs {id}:");

            if (ContainerCache.TryGetValue(id, out var container))
            {
                var logs = container.GetLogs();

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var OutByName in logs)
                    {
                        _logger.LogDebug($"    Name: {OutByName.Key}:");

                        foreach (var OutByExec in OutByName.Value)
                        {
                            _logger.LogDebug($"    -ID: {OutByExec.Key}:");
                            _logger.LogDebug($"        Stdout: {OutByExec.Value.stdout}:");
                            _logger.LogDebug($"        Stderr: {OutByExec.Value.stderr}:");

                        }
                    }
                }

                return logs;
            }

            throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);
        }

        public async Task Update(string id)
        {
            _logger.LogDebug($"Updating Server {id}:");

            if (ContainerCache.TryGetValue(id, out var container))
                await container.Update();
        }

        public void AttachServer(string id, Action<string, string, OutEventArgs.TargetStream, string> callback)
        {
            _logger.LogDebug($"Attatched Server {id}:");

            if (!ContainerCache.TryGetValue(id, out var container))
                throw new DockerContainerNotFoundException(System.Net.HttpStatusCode.NotFound, string.Empty);

            container.NewOutStreamMessage += (s, e) =>
            {
                callback(e.ScriptName, e.ExecID, e.Target, e.Message);
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
            _logger.LogDebug($"Init Cache");
            _logger.LogDebug($"{dbContainers.Count} found in Database");
            _logger.LogDebug($"{containerRequest.Count} found on Worker Host");

            for (int i = 0; i < containerRequest.Count; i++)
            {
                string id = containerRequest[i].ID;
                if (!dbContainers.Remove(id))
                {
                    _logger.LogWarning($"Not in Database: {id}");
                    continue;
                }

                var container = new DockerContainer(client, id, null);
                container.NewOutStreamMessage += OnNewOut;

                pool.Add(container.Start());
                ContainerCache.Add(id, container);
            }

            foreach (var notTraced in dbContainers)
                _logger.LogWarning($"Not Tracked Import Again{notTraced}");

            await Task.WhenAll(pool);
        }

        private void OnNewOut(object sender, OutEventArgs e)
        {
            var s = sender as IServer;

            DataProvider.AppendLog(s.ID,e.ScriptName, e.ExecID, e.Target.ToString(), e.Message);
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

        public async Task SendCommand(string containerID, string execId, string command)
        {
            _logger.LogDebug($"Updating Server {containerID}:");

            if (ContainerCache.TryGetValue(containerID, out var container))
                await container.Interact(execId, command);
        }
    }
}
