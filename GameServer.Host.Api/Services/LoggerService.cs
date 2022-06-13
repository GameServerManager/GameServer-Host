using GameServer.Core.Logger;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GameServer.Host.Api.Services
{
    public class LoggerService : LoggerAPI.LoggerAPIBase
    {
        private readonly ILogger<LoggerService> _logger;
        private readonly IPerformanceLogger _performanceLogger;

        public LoggerService(ILogger<LoggerService> logger, IPerformanceLogger performanceLogger)
        {
            _performanceLogger = performanceLogger;
            _logger = logger;
        }
        
        public async override Task<History> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            var history = await _performanceLogger.GetHistory(request.Id);

            var result = new History();

            foreach (var point in history)
            {
                result.History_.Add(new DataPoint() {
                    CPU = new CpuStats()
                    {
                        CpuDelta = (long) point.Cpu.CpuDelta,
                        NumberCpus = (long)point.Cpu.NumberCpus,
                        SystemCpuDelta = (long)point.Cpu.SystemCpuDelta
                    },
                    Disk = new DiskStats()
                    {
                        ReadCountNormalized = (long)point.Disk.ReadCountNormalized,
                        ReadSizeBytes = (long)point.Disk.ReadSizeBytes,
                        WriteCountNormalized = (long)point.Disk.WriteCountNormalized,
                        WriteSizeBytes = (long)point.Disk.WriteSizeBytes
                    },
                    RAM = new MemoryStats()
                    {
                        AvailableMemory = (long)point.Ram.AvailableMemory,
                        UsedMemory = (long)point.Ram.UsedMemory,
                    },
                    Time = point.Time.Ticks
                });
            }

            return result;
        }

        public async override Task<Empty> StartPerformanceLogger(StartLoggerRequest request, ServerCallContext context)
        {
            await _performanceLogger.StopLogging(request.Id);

            return new Empty();
        }

        public async override Task<Empty> StopPerformanceLogger(StopLoggerRequest request, ServerCallContext context)
        {
            await _performanceLogger.StartLogging(request.Id);

            return new Empty();
        }
    }
}
