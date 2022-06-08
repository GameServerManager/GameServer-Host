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
        static MongoDBProviderTestWrapper _dBProvider;
        string[] ServerIDs = new[] { 
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };

        [ClassInitialize]
        public static void InitDatabase(TestContext ctx)
        {
            var mock = new Mock<ILogger<MongoDbProvider>>();
            ILogger<MongoDbProvider> logger = mock.Object;

            logger = Mock.Of<ILogger<MongoDbProvider>>();

            _dBProvider = new MongoDBProviderTestWrapper(new GameServerSettings()
            {
                ProviderSettings = new DataProviderSettings()
                {
                    UserName = "root",
                    Password = "example",
                    DbName = "ServerTest"
                }
            }, logger);
        }

        [TestInitialize]
        public async Task FillDatabase()
        {
            List<ServerEntity> initDBValues = new();

            foreach (var id in ServerIDs)
            {
                initDBValues.Add(new ServerEntity(id)
                {
                    Config = new Core.Daemon.Config.ServerConfig()
                    {
                        Comment = "TestComment",
                        ContainerScripts = new ServerScripts()
                        {
                            InstalationScript = new Script()
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
                        Discription = "TestDiscription",
                        Image = "Debian",
                        Mounts = Array.Empty<MountingPoint>(),
                        Ports = Array.Empty<PortMap>(),
                        Name = id
                    }
                });
            }
            await _dBProvider.FillDatabase(initDBValues);
        }

        [TestCleanup]
        public async Task ClearDatabase()
        {
            await _dBProvider.ClearDatabase();
            await _dBProvider.Delete();
        }

        [TestMethod]
        public async Task GetAllServerIDTest()
        {
            var ids = await _dBProvider.GetAllServerID();
            Assert.AreEqual(ids.ToArray().Length, ServerIDs.Length);

            foreach (var id in ids)
            {
                Assert.IsTrue(ServerIDs.Contains(id));
            }
        }

        [TestMethod]
        public async Task ServerByIDTest()
        {
            var server = await _dBProvider.ServerByID(ServerIDs[0]);

            Assert.AreEqual(server.ID, ServerIDs[0]);
            Assert.AreEqual(server.Config?.Name, ServerIDs[0]);
        }

        [TestMethod]
        public async Task ServerByNonExistingIDTest()
        {
            var server = await _dBProvider.ServerByID(Guid.NewGuid().ToString());
            Assert.AreEqual(server, null);
        }

        [TestMethod]
        public async Task SaveServerTest()
        {
            var id = Guid.NewGuid().ToString();
            var name = id;
            var comment = "TestComment";
            var des = "TestDiscription";
            var img = "Debian";

            var server = new ServerEntity(id)
            {
                Config = new Core.Daemon.Config.ServerConfig()
                {
                    Comment = comment,
                    ContainerScripts = new ServerScripts()
                    {
                        InstalationScript = new Script()
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
                    Discription = des,
                    Image = img,
                    Mounts = Array.Empty<MountingPoint>(),
                    Ports = Array.Empty<PortMap>(),
                    Name = id
                }
            };
            await _dBProvider.SaveServer(server);

            var serverFromDB = await _dBProvider.ServerByID(id);

            Assert.AreEqual(serverFromDB.ID, id);
            Assert.AreEqual(serverFromDB.Config?.Name, id);
            Assert.AreEqual(serverFromDB.Config?.Comment, comment);
            Assert.AreEqual(serverFromDB.Config?.Discription, des);
            Assert.AreEqual(serverFromDB.Config?.Image, img);
        }

        [TestMethod]
        public async Task AppendLogTest()
        {
            var id = ServerIDs[0];
            var scriptName = "AppendTest";
            var target = "StandardOut";
            var text = "Aenean eu neque eget ex ultricies auctor. Aliquam eget eleifend massa, non tincidunt erat. Etiam sit amet leo justo. Curabitur vestibulum congue turpis in vestibulum. Donec nec iaculis neque, et semper dui. Ut interdum leo a accumsan feugiat. Aenean convallis ornare nunc, ac blandit mi fermentum id. Sed iaculis, mi sit amet imperdiet fermentum, ipsum sapien laoreet augue, non imperdiet sem neque sed nisi. Praesent ac eros in erat suscipit dictum. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Vivamus id urna in est finibus volutpat sed eget lorem.";
            var splitText = text.Split(" ");
            var validationText = "";
            var newAppendGuid = Guid.NewGuid().ToString();

            foreach (var word in splitText)
            {
                await _dBProvider.AppendLog(id, scriptName, newAppendGuid, target, word + " ");
                validationText += word + " ";
            }

            var server = await _dBProvider.ServerByID(id);
            var selectedLog = server.Log.Where(e => e.ScriptName == scriptName).First().ScriptLogs;

            var newOut = selectedLog.Where(e => e.ID == newAppendGuid).First();
            Assert.AreEqual(validationText, newOut.StdOut);
        }
    }
}
