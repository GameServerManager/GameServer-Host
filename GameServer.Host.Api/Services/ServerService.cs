using GameServer.Host.Api;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GameServer.Host.Api.Services
{
    public class ServerService : ServerAPI.ServerAPIBase
    {
        private readonly ILogger<ServerService> _logger;
        public ServerService(ILogger<ServerService> logger)
        {
            _logger = logger;
        }

        public override Task Attach(AttachRequest request, IServerStreamWriter<StdOut> responseStream, ServerCallContext context)
        {
            return base.Attach(request, responseStream, context);
        }

        public override Task<Server> Get(ServerRequest request, ServerCallContext context)
        {
            return base.Get(request, context);
        }

        public override Task<ServerList> GetAll(Empty request, ServerCallContext context)
        {
            return base.GetAll(request, context);
        }

        public override Task<ServerLog> GetLog(LogRequest request, ServerCallContext context)
        {
            return base.GetLog(request, context);
        }

        public override Task Import(ImportRequest request, IServerStreamWriter<StdOut> responseStream, ServerCallContext context)
        {
            return base.Import(request, responseStream, context);
        }

        public override Task<Status> Start(StartRequest request, ServerCallContext context)
        {
            return base.Start(request, context);
        }

        public override Task<Status> Stop(StopRequest request, ServerCallContext context)
        {
            return base.Stop(request, context);
        }

        public override Task<Empty> Update(UpdateRequest request, ServerCallContext context)
        {
            return base.Update(request, context);
        }
    }
}