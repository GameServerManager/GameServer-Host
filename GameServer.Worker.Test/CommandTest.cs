using GameServer.Core.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace GameServer.Test
{
    [TestClass]
    public class CommandTest
    {
        [TestMethod]
        public void EasyCommand()
        {
            var command = new Command("testCommand");

            Assert.AreEqual(command.Name, "testCommand");
            Assert.AreEqual(command.Args.Count, 0);
        }

        [TestMethod]
        public void SingleArgs()
        {
            var command = new Command("testCommand arg1");

            Assert.AreEqual(command.Name, "testCommand");
            Assert.AreEqual(command.Args.Count, 1);
            Assert.AreEqual(command.Args[0], "arg1");
        }

        [TestMethod]
        public void MultiArgs()
        {
            var command = new Command("testCommand arg1 arg2 arg3");

            Assert.AreEqual(command.Name, "testCommand");
            Assert.AreEqual(command.Args.Count, 3);
            Assert.AreEqual(command.Args[0], "arg1");
            Assert.AreEqual(command.Args[1], "arg2");
            Assert.AreEqual(command.Args[2], "arg3");
        }

        [TestMethod]
        public void EscapedComment()
        {
            var command = new Command("testCommand arg1 \"arg2 arg3\"");

            Assert.AreEqual(command.Name, "testCommand");
            Assert.AreEqual(command.Args.Count, 2);
            Assert.AreEqual(command.Args[0], "arg1");
            Assert.AreEqual(command.Args[1], "arg2 arg3");
        }

        [TestMethod]
        public void EscapedCommentMultiArgs()
        {
            var command = new Command("testCommand arg1 \"ar\\\"g2 arg3\" realArg4");

            Assert.AreEqual(command.Name, "testCommand");
            Assert.AreEqual(command.Args.Count, 3);
            Assert.AreEqual(command.Args[0], "arg1");
            Assert.AreEqual(command.Args[1], "ar\"g2 arg3");
            Assert.AreEqual(command.Args[2], "realArg4");
        }

        [TestMethod]
        public void PTAttackChecker()
        {
            var mount1 = @"C:\Users\Daniel\Documents\AnimalsMapTool";
            var mount2   = @"C:\Users\Daniel\Documents\..\Documents\AnimalsMapTool";

            if (mount1.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Contains(".."))
                throw new ApplicationException("PT Attack");

            Assert.ThrowsException<ApplicationException>(() => {
                if (mount2.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Contains(".."))
                    throw new ApplicationException("PT Attack");
            });
        }
        
    }
}