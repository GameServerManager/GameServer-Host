using Grpc.Core;

namespace GameServer.Host.Api.Services
{
    public class HealthCheckService : HealthCheckAPI.HealthCheckAPIBase
    {
        private readonly ILogger<HealthCheckService> _logger;
        public HealthCheckService(ILogger<HealthCheckService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> Echo(HelloRequest request, ServerCallContext context)
        {
            return base.Echo(request, context);
        }
    }
}
