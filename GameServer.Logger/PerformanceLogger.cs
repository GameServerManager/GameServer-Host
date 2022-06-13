using Docker.DotNet;
using Docker.DotNet.Models;
using GameServer.Core.Database;
using GameServer.Core.Database.Logger;
using GameServer.Core.Logger;
using GameServer.Core.Settings;
using Microsoft.Extensions.Logging;

namespace GameServer.Logger
{
    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly LoggingSettings? loggingSettings;
        private readonly ILoggerDataProvider dataProvider;
        private readonly Dictionary<string, CancellationTokenSource> StatToken = new();
        private readonly Dictionary<string, DateTime> LastUpdate = new();
        private readonly DockerClient client;
        private readonly ILogger<PerformanceLogger> _logger;

        public PerformanceLogger(IGameServerSettings gameServerSettings, ILoggerDataProvider loggerProvider, ILogger<PerformanceLogger> logger)
        {
            _logger = logger;
            this.loggingSettings = gameServerSettings.LoggingSettings;
            this.dataProvider = loggerProvider;
            client = new DockerClientConfiguration()
                .CreateClient();
        }

        #region IPerformanceLogger  
        public async Task<List<DataPoint>> GetHistory(string id)
        {
            return await dataProvider.GetHistory(id);
        }

        public async Task StartLogging(string id)
        {
            LastUpdate.Add(id, DateTime.MinValue);
            var token = new CancellationTokenSource();
            StatToken.Add(id, token);

            await client.Containers.GetContainerStatsAsync(id, new Docker.DotNet.Models.ContainerStatsParameters(), new Progress<Docker.DotNet.Models.ContainerStatsResponse>(StatsRecieved), token.Token);
        }

        private async void StatsRecieved(ContainerStatsResponse res)
        {
            if (DateTime.Now <= LastUpdate[res.ID] + new TimeSpan(0, 0, loggingSettings.LoggerInterval))
                return;
            LastUpdate[res.ID] = DateTime.Now;
            var usedMemory = res.MemoryStats.Usage - res.MemoryStats.Stats["cache"];
            var availableMemory = res.MemoryStats.Limit;
            var cpuDelta = res.CPUStats.CPUUsage.TotalUsage - res.PreCPUStats.CPUUsage.TotalUsage;
            var systemCpuDelta = res.CPUStats.SystemUsage - res.PreCPUStats.SystemUsage;
            var numberCpus = res.CPUStats.OnlineCPUs;

            var point = new DataPoint()
            {
                Time = res.Read,
                Cpu = new()
                {
                    CpuDelta = cpuDelta,
                    SystemCpuDelta = systemCpuDelta,
                    NumberCpus = numberCpus
                },
                Ram = new()
                {
                    UsedMemory = usedMemory,
                    AvailableMemory = availableMemory
                },
                Disk = new()
                {
                    ReadCountNormalized = res.StorageStats.ReadCountNormalized ,
                    ReadSizeBytes = res.StorageStats.ReadSizeBytes ,
                    WriteCountNormalized = res.StorageStats.WriteCountNormalized ,
                    WriteSizeBytes = res.StorageStats.WriteSizeBytes

                },
                Networks = new Dictionary<string, Core.Database.Logger.NetworkStats>()
            };

            foreach (var network in res.Networks)
            {
                point.Networks.Add(network.Key, new()
                {
                    RxBytes = network.Value.RxBytes,
                    RxPackets = network.Value.RxPackets,
                    RxErrors = network.Value.RxErrors,
                    RxDropped = network.Value.RxDropped,
                    TxBytes = network.Value.TxBytes,
                    TxPackets = network.Value.TxPackets,
                    TxErrors = network.Value.TxErrors,
                    TxDropped = network.Value.TxDropped
                });
            }

            await dataProvider.AppendLogs(res.ID, point);
        }

        public async Task StopLogging(string id)
        {
            if(StatToken.Remove(id, out var token))
                token.Cancel();

        }
        #endregion

        public void Dispose()
        {
            dataProvider.Dispose();
        }
    }
}