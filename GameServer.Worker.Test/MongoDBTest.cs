using GameServer.Core.Daemon.Config;
using GameServer.Core.Database.Daemon;
using GameServer.Core.Settings;
using GameServer.Data;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Test
{

    [TestClass]
    public class MongoDBTest
    {
        private static MongoDbProviderTestWrapper? _dBProvider;

        private readonly string?[] _serverIDs = { 
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };

        [ClassInitialize]
        public static void InitDatabase(TestContext ctx)
        {
            var mock = new Mock<ILogger<MongoDbProvider>>();
            ILogger<MongoDbProvider> logger = mock.Object;

            logger = Mock.Of<ILogger<MongoDbProvider>>();

            _dBProvider = new MongoDbProviderTestWrapper(new GameServerSettings()
            {
                ProviderSettings = new DataProviderSettings()
                {
                    DbName = "ServerTest"
                }
            }, logger);
        }

        [TestInitialize]
        public async Task FillDatabase()
        {
            List<ServerEntity> initDbValues = new();

            foreach (var id in _serverIDs)
            {
                initDbValues.Add(new ServerEntity(id)
                {
                    Config = new ServerConfig()
                    {
                        Comment = "TestComment",
                        ContainerScripts = new ServerScripts()
                        {
                            InstallationScript = new Script()
                            {
                                Entrypoint = "bash",
                                ScriptCommand = "echo hi"
                            },
                            StartScript = new Script()
                            {
                                Entrypoint = "bash",
                                ScriptCommand = "echo hi"
                            },
                            UpdateScript = new Script()
                            {
                                Entrypoint = "bash",
                                ScriptCommand = "echo hi"
                            }
                        },
                        Variables = Array.Empty<Variable>(),
                        Description = "TestDiscription",
                        Image = "Debian",
                        Mounts = Array.Empty<MountingPoint>(),
                        Ports = Array.Empty<PortMap>(),
                        Name = id
                    }
                });
            }
            await _dBProvider.FillDatabase(initDbValues);
        }

        [TestCleanup]
        public async Task ClearDatabase()
        {
            await _dBProvider.ClearDatabase();
            await _dBProvider.Delete();
        }

        [TestMethod]
        public async Task GetAllServerIdTest()
        {
            var ids = await _dBProvider.GetAllServerId();
            Assert.AreEqual(ids.ToArray().Length, _serverIDs.Length);

            foreach (var id in ids)
            {
                Assert.IsTrue(_serverIDs.Contains(id));
            }
        }

        [TestMethod]
        public async Task ServerByIdTest()
        {
            var server = await _dBProvider.ServerById(_serverIDs[0]);

            Assert.AreEqual(server.Id, _serverIDs[0]);
            Assert.AreEqual(server.Config?.Name, _serverIDs[0]);
        }

        [TestMethod]
        public async Task ServerByNonExistingIdTest()
        {
            var server = await _dBProvider.ServerById(Guid.NewGuid().ToString());
            Assert.AreEqual(server, null);
        }

        [TestMethod]
        public async Task SaveServerTest()
        {
            var id = Guid.NewGuid().ToString();
            const string comment = "TestComment";
            const string des = "TestDiscription";
            const string img = "Debian";

            var server = new ServerEntity(id)
            {
                Config = new Core.Daemon.Config.ServerConfig()
                {
                    Comment = comment,
                    ContainerScripts = new ServerScripts()
                    {
                        InstallationScript = new Script()
                        {
                            Entrypoint = "bash",
                            ScriptCommand = "echo hi"
                        },
                        StartScript = new Script()
                        {
                            Entrypoint = "bash",
                            ScriptCommand = "echo hi"
                        },
                        UpdateScript = new Script()
                        {
                            Entrypoint = "bash",
                            ScriptCommand = "echo hi"
                        }
                    },
                    Variables = Array.Empty<Variable>(),
                    Description = des,
                    Image = img,
                    Mounts = Array.Empty<MountingPoint>(),
                    Ports = Array.Empty<PortMap>(),
                    Name = id
                }
            };
            await _dBProvider.SaveServer(server);

            var serverFromDb = await _dBProvider.ServerById(id);

            Assert.AreEqual(serverFromDb.Id, id);
            Assert.AreEqual(serverFromDb.Config?.Name, id);
            Assert.AreEqual(serverFromDb.Config?.Comment, comment);
            Assert.AreEqual(serverFromDb.Config?.Description, des);
            Assert.AreEqual(serverFromDb.Config?.Image, img);
        }

        [TestMethod]
        public async Task AppendLogTest()
        {
            var id = _serverIDs[0];
            const string scriptName = "AppendTest";
            const string target = "StandardOut";
            const string text = "Aenean eu neque eget ex ultricies auctor. Aliquam eget eleifend massa, non tincidunt erat. Etiam sit amet leo justo. Curabitur vestibulum congue turpis in vestibulum. Donec nec iaculis neque, et semper dui. Ut interdum leo a accumsan feugiat. Aenean convallis ornare nunc, ac blandit mi fermentum id. Sed iaculis, mi sit amet imperdiet fermentum, ipsum sapien laoreet augue, non imperdiet sem neque sed nisi. Praesent ac eros in erat suscipit dictum. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Vivamus id urna in est finibus volutpat sed eget lorem.";
            var splitText = text.Split(" ");
            var validationText = "";
            var newAppendGuid = Guid.NewGuid().ToString();

            foreach (var word in splitText)
            {
                await _dBProvider.AppendLog(id, scriptName, newAppendGuid, target, word + " ");
                validationText += word + " ";
            }

            var server = await _dBProvider.ServerById(id);
            var selectedLog = server.Log.First(e => e.ScriptName == scriptName).ScriptLogs;

            var newOut = selectedLog.First(e => e.Id == newAppendGuid);
            Assert.AreEqual(validationText, newOut.StdOut);
        }
    }
}
