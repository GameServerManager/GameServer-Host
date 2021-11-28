using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GameServer.Host.Api.Services
{
    public class LoggerService : LoggerAPI.LoggerAPIBase
    {
        private readonly ILogger<LoggerService> _logger;
        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public override Task<HistoryList> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            return base.GetHistory(request, context);
        }

        public override Task<Empty> StartPerformanceLogger(StartLoggerRequest request, ServerCallContext context)
        {
            return base.StartPerformanceLogger(request, context);
        }

        public override Task<Empty> StopPerformanceLogger(StopLoggerRequest request, ServerCallContext context)
        {
            return base.StopPerformanceLogger(request, context);
        }
    }
}
