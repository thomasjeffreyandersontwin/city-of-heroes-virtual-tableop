using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using Module.UnitTest;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.UnitTest.GameCommunicator
{
    [TestClass]
    public class KeyBindsGeneratorTest : BaseTest
    {
        CrowdMember testCrowdMember;
        KeyBindsGenerator keyBindsGenerator;

        [TestInitialize]
        public void Initialize()
        {
            ResetKeyBindGeneratorStatics();
            //Arrange
            testCrowdMember = new Mock<CrowdMember>().Object;
            keyBindsGenerator = new Mock<KeyBindsGenerator>().Object;
            testCrowdMember.Name = "Statesman";
            testCrowdMember.ActiveIdentity = new Identity("model_Statesman", IdentityType.Model);
        }

        /// <summary>
        /// This test ensure that the right command string is returned if we are calling the GenerateKeyBindsForEvent method without parameters
        /// </summary>
        [TestMethod]
        public void GenerateKeyBindsForEvent_CreatesProperKeybindString()
        {
            string actual = string.Empty;
            string valid = string.Empty;

            foreach (GameEvent evt in Enum.GetValues(typeof(GameEvent)))
            {
                actual = keyBindsGenerator.GenerateKeyBindsForEvent(evt);
                valid = keyBindsGenerator.keyBindsStrings[evt];

                Assert.AreEqual(valid, actual);
                keyBindsGenerator.CompleteEvent();
            }
        }

        /// <summary>
        /// This is an acceptance test for keybinds generation
        /// </summary>
        [TestMethod]
        public void GenerateKeyBindsForEvent_CreatesProperSpawnKeybindString()
        {
            string actual = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            //Assert
            string valid = string.Format("spawn_npc {0} {1}", testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);
            Assert.AreEqual(valid, actual);

            //Clean up
            keyBindsGenerator.CompleteEvent();
        }

        /// <summary>
        /// This test ensure that the generated command string from GenerateKeyBindsForEvent method is encapsulated in quotes upon calling CompleteEvent method.
        /// This is an acceptance test
        /// </summary>
        [TestMethod]
        public void GenerateKeyBindsForEvent_SurroundsWithQuotes()
        {
            //Act
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);
            string actual = keyBindsGenerator.CompleteEvent();

            //Assert
            string valid = string.Format("spawn_npc {0} {1}", testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);
            Assert.AreEqual(valid, actual);
        }

        /// <summary>
        /// Tests for correct concatenation of multiple commands.
        /// This is an acceptance test.
        /// </summary>
        [TestMethod]
        public void GenerateKeyBindsForEvent_AppendsMultipleKeybinds()
        {
            //Act
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, testCrowdMember.Name);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
            string actual = keyBindsGenerator.CompleteEvent();

            //Assert
            string valid = string.Format("\"target_name {0}$$follow\"", testCrowdMember.Name);
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_PlacesKeybindInTempBindFile()
        {
            // THIS CANNOT POSSIBLY BE TESTED WITH MOCK AS IT CONTAINS ACTUAL FILE I/O. WE SHOULD USE AN ACTUAL INSTANCE OF KeyBindsGenerator
            keyBindsGenerator = new KeyBindsGenerator();
            //Act
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            keyBindsGenerator.CompleteEvent();

            StreamReader actualFile = new StreamReader(keyBindsGenerator.BindFile);
            string actual = actualFile.ReadLine();
            actualFile.Close();

            //Assert
            string valid = string.Format("{2} \"spawn_npc {0} {1}\"", testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name, keyBindsGenerator.TriggerKey);
            Assert.AreEqual(valid, actual);

            keyBindsGenerator = new Mock<KeyBindsGenerator>().Object;
        }
    }
}
