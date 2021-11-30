using GameServer.Core.Daemon;
using GameServer.Core.Logger;
using GameServer.Core.Settings;
using GameServer.Data;
using GameServer.Host.Api.Services;
using GameServer.Logger;
using GameServer.Worker;

var builder = WebApplication.CreateBuilder(args);
var settings = GameServerSettings.FromFile(@".\config.yml");
var dataProvider = new MongoDBProvider(settings.ProviderSettings);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<IDaemonWorker>(s => new DockerWorker(settings.DaemonSettings, dataProvider));
builder.Services.AddSingleton<IPerformanceLogger>(s => new PerformanceLogger(settings.LoggingSettings, dataProvider));
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ServerService>();
app.MapGrpcService<LoggerService>();
app.MapGrpcService<HealthCheckService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
