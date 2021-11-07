using Docker.DotNet;
using GameServer.Core.Daemon;
using GameServer.Core.Daemon.Config;

namespace GameServer.Worker
{
    public partial class DockerContainer : IServer, IDisposable
    {
        private DockerClient Client { get; }
        private string StdoutCache { get; set; } = "";
        private string StderrCache { get; set; } = "";

        public string ID { get; }

        public IList<string> Names { get; set; }

        public string Image { get; set; }

        public string ImageID { get; set; }

        public DockerContainer(DockerClient client, string id)
        {
            Client = client;
            ID = id;

            var container = GetOwnContainer().Result;
            Image = container.Image;
            ImageID = container.ImageID;
            Names = container.Names;
        }

        public async Task Start()
        {
            await Client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());

            await ExecFromName("StartScript");
        }

        public async Task Stop()
        {
            await Client.Containers.StopContainerAsync(ID, new Docker.DotNet.Models.ContainerStopParameters());
        }

        public async Task<ServerStatus> GetStatus()
        {
            var container = await GetOwnContainer();

            return new ServerStatus()
            {
                State = container.State,
                Status = container.Status
            };
        }

        private async Task<Docker.DotNet.Models.ContainerListResponse> GetOwnContainer()
        {
            var containerList = await Client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "id",
                        new Dictionary<string, bool>
                        {
                            { ID, true}
                        }
                    }
                },
                All = true
            });
            return containerList.First();
        }

        public async Task Exec(Script script, string name)
        {
            if (name is null)
                name = Guid.NewGuid().ToString();

            GenerateScript(script, name, this.Names.First());

            await ExecFromName(name);
        }

        private async Task ExecFromName(string name, string endpoint = "/bin/bash")
        {
            var createParams = new Docker.DotNet.Models.ContainerExecCreateParameters()
            {
                AttachStdin = true,
                AttachStderr = true,
                AttachStdout = true,
                Tty = true,
                Cmd = new List<string>() { endpoint, "-c", $"/Home/scripts/{name}.sh", }
            };

            var exec = await Client.Exec.ExecCreateContainerAsync(ID, createParams);
            //await client.Exec.StartContainerExecAsync(exec.ID);
            var token = new CancellationTokenSource();


            var stream = await Client.Exec.StartAndAttachContainerExecAsync(exec.ID, true, token.Token);

            var buffer = new byte[1];
            var offset = 0;
            MultiplexedStream.ReadResult res;

            do
            {
                res = await stream.ReadOutputAsync(buffer, offset, 1, token.Token);

                if (res.Count != 0)
                    StdoutCache += System.Text.Encoding.Default.GetString(buffer);

            } while (!res.EOF);

        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public async Task Install()
        {
            await Client.Containers.StartContainerAsync(ID, new Docker.DotNet.Models.ContainerStartParameters());
            await ExecFromName("InstalationScript");
        }

        public async Task Update()
        {
            await ExecFromName("UpdateScript");
        }

        public (string stderr, string stdout) GetLogs()
        {
            return (StderrCache, StdoutCache);
        }
    }
}
