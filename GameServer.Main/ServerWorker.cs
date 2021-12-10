using GameServer.Core.Command;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;
using GameServer.Core.Database;
using GameServer.Core.Logger;
using GameServer.Core.Settings;
using GameServer.Data;
using GameServer.Logger;
using GameServer.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace GameServer.Main
{
    public class ServerWorker : IDisposable
    {
        private readonly Dictionary<string, Action<string[]>> _commandMap;
        public bool Running { get; private set; } = true;
        public CommandQueue CommandQueue { get; }
        private readonly MongoDBProvider _dataProvider;
        private readonly IDaemonWorker _daemonWorker;
        private readonly IPerformanceLogger _performanceLogger;

        public ServerWorker(CommandQueue commandQueue, GameServerSettings settings)
        {
            CommandQueue = commandQueue;
            _commandMap = new Dictionary<string, Action<string[]>>{
                {"cls", (args) => Console.Clear()},
                {"clear", (args) => Console.Clear() },
                {"server", Server },
                {"allserver", AllServer },
                {"start", Start },
                {"stop", Stop},
                {"update", Update},
                {"clog", ServerLog },
                {"attach", Attach },
                {"import", ImportServer },
                {"startlog", StartPerformanceLogger },
                {"stoplog", StopPerformanceLogger },
                {"getlog", GetHistory },
                {"help", Help },
                {"exit", (args) => Running = false}
            };

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug);
            });


            _dataProvider = new MongoDBProvider(settings, loggerFactory.CreateLogger<MongoDBProvider>());
            _daemonWorker = new DockerWorker(settings, _dataProvider, loggerFactory.CreateLogger<DockerWorker>());
            _performanceLogger = new PerformanceLogger(settings, _dataProvider, loggerFactory.CreateLogger<PerformanceLogger>());
        }

        private void Help(string[] obj)
        {
            foreach (var item in _commandMap.Keys)
            {
                Console.WriteLine(item);
            }
        }

        public void Start()
        {
            CommandQueue.NewCommand += OnNewCommand;
            _dataProvider.Connect();
        }

        private async void Server(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var server = await _daemonWorker.GetServer(args[0]);

            var status = await server.GetStatus();
            Console.WriteLine($"{String.Join('|', server.Names)}: {server.ID}");
            Console.WriteLine($"   ->State: {status.State}");
            Console.WriteLine($"   ->Status: {status.Status}");
        }

        private async void AllServer(string[] args)
        {
            if (args.Length != 0)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var servers = await _daemonWorker.GetAllServer();
            Console.WriteLine($"Names: ID");
            foreach (var server in servers)
            {
                var status = await server.GetStatus();
                Console.WriteLine($"{String.Join('|', server.Names)}: {server.ID}");
                Console.WriteLine($"   ->State: {status.State}");
                Console.WriteLine($"   ->Status: {status.Status}");
            }
        }

        private async void Start(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _daemonWorker.StartServer(args[0]);
        }

        private async void Stop(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _daemonWorker.StopServer(args[0]);
        }

        private async void Update(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _daemonWorker.Update(args[0]);
        }

        private async void ServerLog(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var logs = await _daemonWorker.GetServerLogs(args[0]);

            Console.WriteLine(logs);
        }

        private void Attach(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            _daemonWorker.AttachServer(args[0], (msg) => Console.WriteLine(msg));
        }

        private async void ImportServer(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var config = ServerConfig.FromFile(args[0]);
            await _daemonWorker.ImportServer(config);
        }

        private async void StartPerformanceLogger(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _performanceLogger.StartLogging(args[0]);
        }

        private async void StopPerformanceLogger(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            await _performanceLogger.StopLogging(args[0]);
        }

        private async void GetHistory(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp("only One Argument for help");
                return;
            }

            var history = await _performanceLogger.GetHistory(args[0]);

            Console.WriteLine($"[log] Timestamp | CPU | Ram");
            foreach (var data in history)
            {
                Console.WriteLine($"[log]{data.Time} | {data.CPU.CpuUsage} | {data.RAM.MemoryUsage}");
            }
        }

        private async void DisplayHelp(string message)
        {
            Console.WriteLine(message);
        }

        private void OnNewCommand(object sender, NewCommandEventArgs e)
        {
            var c = new Command(e.Command);
            var contains = false;
            if (string.IsNullOrEmpty(c.Name))
            {
                DisplayHelp("Command was empty");
                return;
            }

            contains = _commandMap.TryGetValue(c.Name.ToLower(), out var action);
            if (!contains)
            {
                DisplayHelp("Command not Found");
                return;
            }
            action.Invoke(c.Args.ToArray());
        }

        public void Dispose()
        {
            while (CommandQueue?.IsEmpty == false) { }
            Running = false;
        }
    }
}
