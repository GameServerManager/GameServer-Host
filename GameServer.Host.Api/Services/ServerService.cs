using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Host.Api;
using GameServer.Worker;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GameServer.Host.Api.Services
{
    public class ServerService : ServerAPI.ServerAPIBase
    {
        private readonly ILogger<ServerService> _logger;
        private readonly IDaemonWorker _daemonWorker;

        public ServerService(ILogger<ServerService> logger, IDaemonWorker daemonWorker)
        {
            _daemonWorker = daemonWorker;
            _logger = logger;
        }

        public async override Task Attach(AttachRequest request, IServerStreamWriter<StdOut> responseStream, ServerCallContext context)
        {
            _daemonWorker.AttachServer(request.Id, (msg) => responseStream.WriteAsync(new StdOut() { Msg = msg } ));
        }

        public async override Task<Server> Get(ServerRequest request, ServerCallContext context)
        {
            var server = await _daemonWorker.GetServer(request.Id);
            var state = await server.GetStatus();
            var s = new Api.Server()
            {
                Id = server.ID,
                State = state.State,
                Status = state.Status
            };
            s.Name.AddRange(server.Names);
            return s;
        }

        public async override Task<ServerList> GetAll(Empty request, ServerCallContext context)
        {
            var sList = new ServerList();


            var servers = await _daemonWorker.GetAllServer();
            foreach (var server in servers)
            {

                var state = await server.GetStatus();
                var s = new Api.Server()
                {
                    Id = server.ID,
                    State = state.State,
                    Status = state.Status
                };
                s.Name.AddRange(server.Names);

                sList.Servers.Add(s);
            }
            return sList;
        }

        public async override Task<ServerLog> GetLog(LogRequest request, ServerCallContext context)
        {
            var log = await _daemonWorker.GetServerLogs(request.Id);
            return new Api.ServerLog()
            {
                Log = log
            };
        }

        public async override Task Import(ImportRequest request, IServerStreamWriter<StdOut> responseStream, ServerCallContext context)
        {
            var config = new ServerConfig()
            {
                Comment = request.Comment,
                ContainerScripts = new()
                {
                    InstalationScript = new()
                    {
                        Entrypoint = request.ContainerScripts.InstalationScript.Entrypoint,
                        ScriptCommand = request.ContainerScripts.InstalationScript.ScriptCommand
                    },
                    StartScript = new()
                    {
                        Entrypoint = request.ContainerScripts.StartScript.Entrypoint,
                        ScriptCommand = request.ContainerScripts.StartScript.ScriptCommand
                    },
                    UpdateScript = new()
                    {
                        Entrypoint = request.ContainerScripts.UpdateScript.Entrypoint,
                        ScriptCommand = request.ContainerScripts.UpdateScript.ScriptCommand
                    },
                },
                Discription = request.Discription,
                Image = request.Image,
                Name = request.Name,
            };
            List<GameServer.Core.Daemon.Config.MountingPoint> mounts = new();
            foreach (var mount in request.Mounts)
            {
                mounts.Add(new GameServer.Core.Daemon.Config.MountingPoint()
                {
                    HostPath = mount.HostPath,
                    ServerPath = mount.ServerPath,
                });
            }
            config.Mounts = mounts.ToArray();

            List<GameServer.Core.Daemon.Config.PortMap> ports = new();
            foreach (var port in request.Ports)
            {
                ports.Add(new GameServer.Core.Daemon.Config.PortMap()
                {
                    HostPorts = port.HostPorts.ToArray(),
                    ServerPort = port.ServerPort,
                });
            }
            config.Ports = ports.ToArray();

            List<GameServer.Core.Daemon.Config.Variable> vars = new();
            foreach (var var in request.Variables)
            {
                vars.Add(new GameServer.Core.Daemon.Config.Variable()
                {
                    DefaultValue = var.DefaultValue,
                    Description = var.Description,
                    EnvVariable = var.EnvVariable,
                    Name = var.Name,
                    UserEditable = var.UserEditable,
                    UserViewable = var.UserViewable
                });
            }
            config.Variables = vars.ToArray();

            var id = await _daemonWorker.ImportServer(config);
        }

        public async override Task<Status> Start(StartRequest request, ServerCallContext context)
        {
            await _daemonWorker.StartServer(request.Id);
            return new Status() { Status_ = "starting" };
        }

        public async override Task<Status> Stop(StopRequest request, ServerCallContext context)
        {
            await _daemonWorker.StartServer(request.Id);
            return new Status() { Status_ = "stopping" };
        }

        public async override Task<Empty> Update(UpdateRequest request, ServerCallContext context)
        {
            await _daemonWorker.Update(request.Id);
        }
    }
}