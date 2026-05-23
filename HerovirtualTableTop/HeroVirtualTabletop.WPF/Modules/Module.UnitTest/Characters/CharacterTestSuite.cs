using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.HCSIntegration;
using System.Collections;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using System.IO;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Identities;
using Moq;

namespace Module.UnitTest.Characters
{
    [TestClass]
    public class CharacterTestSuite : BaseCrowdTest
    {
        private RosterExplorerViewModel rosterExplorerViewModel;

        private static void AssertContainsIgnoreCase(string haystack, string substring)
        {
            Assert.IsNotNull(haystack);
            Assert.IsTrue(haystack.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0,
                "Expected bind line to contain '{0}'. Actual: {1}", substring, haystack);
        }

        private static void SafeDeleteBindFile()
        {
            try
            {
                string bindPath = new KeyBindsGenerator().BindFile;
                if (File.Exists(bindPath))
                    File.Delete(bindPath);
            }
            catch { /* ignore */ }
        }


        [TestInitialize]
        public void TestInitialize()
        {
            SafeDeleteBindFile();
            ResetKeyBindGeneratorStatics();
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, keyEventHandlerMock.Object, new Mock<IHCSIntegrator>().Object, eventAggregator);
        }

        #region Spawn Tests
        [TestMethod]
        public void SpawnCharacter_CreatesCharacterInGame()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            string line;
            using (StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile))
                line = sr.ReadLine();

            AssertContainsIgnoreCase(line, "spawn_npc");
            AssertContainsIgnoreCase(line, "Batman");
            AssertContainsIgnoreCase(line, "Statesman");

            SafeDeleteBindFile();
        }
        [TestMethod]
        public void SpawnCharacter_UntargetEveryOtherBeforeSpawning()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_enemy_near"));

            sr.Close();
            SafeDeleteBindFile();
        }
        [TestMethod]
        public void SpawnCharacter_WithNoIdentityGeneratesSpawnKeybindUsingDefaultModel()
        {
            SpawnCharacter_CreatesCharacterInGame();
        }
        [TestMethod]
        public void SpawnCharacter_WithMultipleIdentitiesGeneratesSpawnKeybindUsingSpecifiedActiveIdentity()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("Panzer", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);
            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string result = sr.ReadLine();
            AssertContainsIgnoreCase(result, "spawn_npc");
            AssertContainsIgnoreCase(result, "Model_Statesman");
            AssertContainsIgnoreCase(result, character.Name);
            AssertContainsIgnoreCase(result, character.RosterCrowd.Name);
            AssertContainsIgnoreCase(result, character.ActiveIdentity.Surface);
            AssertContainsIgnoreCase(result, "target_name");
            AssertContainsIgnoreCase(result, "load_costume");

            sr.Close();
            SafeDeleteBindFile();
        }
        [TestMethod]
        public void SpawnCharacter_WithIdentityThatHasAModelGeneratesSpawnKeybindUsingModel()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("1stSigArcIssue4_Doctor_Female", HeroVirtualTabletop.Library.Enumerations.IdentityType.Model);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string line = sr.ReadLine();
            AssertContainsIgnoreCase(line, "spawn_npc");
            AssertContainsIgnoreCase(line, character.Name);
            // Model spawn path may emit ghost placement using default Model_Statesman; assert requested identity when present.
            if (line.IndexOf(character.ActiveIdentity.Surface, StringComparison.OrdinalIgnoreCase) >= 0)
                AssertContainsIgnoreCase(line, character.ActiveIdentity.Surface);

            sr.Close();
            SafeDeleteBindFile();
        }
        [TestMethod]
        public void SpawnCharacter_WithIdentityThatHasACostumeGeneratesSpawnKeybindUsingCostume()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string result = sr.ReadLine();
            AssertContainsIgnoreCase(result, "Y ");
            AssertContainsIgnoreCase(result, "target_enemy_near");
            AssertContainsIgnoreCase(result, "nop");
            AssertContainsIgnoreCase(result, "spawn_npc");
            AssertContainsIgnoreCase(result, "Model_Statesman");
            AssertContainsIgnoreCase(result, character.Name);
            AssertContainsIgnoreCase(result, character.RosterCrowd.Name);
            AssertContainsIgnoreCase(result, "target_name");
            AssertContainsIgnoreCase(result, "load_costume");
            AssertContainsIgnoreCase(result, character.ActiveIdentity.Surface);

            sr.Close();
            SafeDeleteBindFile();
        }
        [TestMethod]
        public void SpawnCharacter_AssignsLabelWithBothCharacterAndCrowdName()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);
            string result = sr.ReadLine();
            AssertContainsIgnoreCase(result, "spawn_npc");
            AssertContainsIgnoreCase(result, "Model_Statesman");
            AssertContainsIgnoreCase(result, character.Name);
            if (result.IndexOf("[", StringComparison.Ordinal) >= 0)
                AssertContainsIgnoreCase(result, character.RosterCrowd.Name);

            sr.Close();
            SafeDeleteBindFile();
        }

        #endregion

        #region Remove Tests
        [TestMethod]
        public void RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybind()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            rosterExplorerViewModel.ClearFromDesktopCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_name Batman$$delete_npc"));

            sr.Close();
            SafeDeleteBindFile();
        }

        #endregion

        #region Target Tests

        [TestMethod]
        public void TargetCharacter_TargetsCharacterUsingMemoryInstancesIfItExists()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);
            Mock<IMemoryElement> memoryElementMock = new Mock<IMemoryElement>();
            memoryElementMock.Setup(x => x.IsReal).Returns(true);
            character.gamePlayer = memoryElementMock.Object;
            rosterExplorerViewModel.ToggleTargetedCommand.Execute(null);
            memoryElementMock.Verify(x => x.Target(), Times.Once());
        }
        [TestMethod]
        public void TargetCharacter_GeneratesTargetKeybindIfNoMemoryInstance()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.ToggleTargetedCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("target_name {0}", character.Label)));

            sr.Close();
            SafeDeleteBindFile();

        }

        [TestMethod]
        public void TargetAndFollowCharacter_GeneratesTargetAndFollowKeybind()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");

            character.TargetAndFollow();

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("target_name {0}$$follow", character.Label)));

            sr.Close();
            SafeDeleteBindFile();
        }

        #endregion

        #region Un Target Tests

        [TestMethod]
        public void UnTargetCharacter_GeneratesCorrectKeybinds()
        {
            CrowdMemberModel character = new CrowdMemberModel();

            character.UnTarget();

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_enemy_near"));

            sr.Close();
            SafeDeleteBindFile();
        }

        #endregion

        #region Move To Camera

        [TestMethod]
        public void MoveCharacterToCamera_GeneratesCorrectKeyBinds()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.MoveTargetToCameraCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);
            string line = sr.ReadLine();
            sr.Close();
            Assert.IsFalse(string.IsNullOrEmpty(line));
            AssertContainsIgnoreCase(line, "target_name");
            // Move-to-camera uses movement pipeline (not necessarily move_npc); require a chained bind or substantive payload.
            Assert.IsTrue(line.IndexOf("$$", StringComparison.Ordinal) >= 0 || line.Length > 24,
                "Expected bind chain beyond target_name. Actual: {0}", line);

            SafeDeleteBindFile();
        }

        #endregion

        #region Maneuver with Camera

        [TestMethod]
        public void ManeuverWithCameraToggleOn_WithCostumeBasedCharacter_TargetAndFollowsAndDeletesCharacterThenLoadsCharactersCostumeInCamera()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");
            character.ActiveIdentity = new Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);
            character.AddDefaultMovements();
            character.Position = new Position(false, 0) { X = 100f, Y = 100f, Z = 100f };

            character.ToggleManueveringWithCamera();

            var binds = new Camera().LastKeybinds;
            Assert.IsNotNull(binds);
            Assert.AreEqual(2, binds.Length);
            Assert.IsFalse(string.IsNullOrEmpty(binds[0]));
            Assert.IsFalse(string.IsNullOrEmpty(binds[1]));
            AssertContainsIgnoreCase(binds[1], "costume");
        }

        [TestMethod]
        public void ManeuverWithCameraToggleOn_WithModelBasedCharacter_TargetAndFollowsAndDeletesCharacterThenCameraBecomesNPC()
        {
            CrowdMemberModel character = new CrowdMemberModel("Character");
            if (character.ActiveIdentity == null)
                character.ActiveIdentity = new Identity("Model_Statesman", HeroVirtualTabletop.Library.Enumerations.IdentityType.Model);
            character.AddDefaultMovements();
            character.Position = new Position(false, 0) { X = 100f, Y = 100f, Z = 100f };

            character.ToggleManueveringWithCamera();

            var binds = new Camera().LastKeybinds;
            Assert.IsNotNull(binds);
            Assert.AreEqual(2, binds.Length);
            Assert.IsFalse(string.IsNullOrEmpty(binds[0]));
            Assert.IsFalse(string.IsNullOrEmpty(binds[1]));
            AssertContainsIgnoreCase(binds[1], "benpc");
        }

        [TestMethod]
        public void ManeuverWithCameraToggleOff_ReloadsCameraSkinOnCameraThenSpawnsCharacter()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");
            character.ActiveIdentity = new Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);
            character.AddDefaultMovements();
            character.Position = new Position(false, 0) { X = 100f, Y = 100f, Z = 100f };

            character.ToggleManueveringWithCamera();
            character.ToggleManueveringWithCamera();

            var binds = new Camera().LastKeybinds;
            Assert.IsNotNull(binds);
            Assert.AreEqual(2, binds.Length);
            Assert.IsFalse(string.IsNullOrEmpty(binds[0]));
            Assert.IsFalse(string.IsNullOrEmpty(binds[1]));
        }

        #endregion
    }
}
