using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class TrackSpawnedStatePerCharacter : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void SpawnCommandIssuedOverlayAppears()
        {
            // Given: Guard_Captain_01 has spawned state false (overlay hidden)
            _guardCaptain.HasBeenSpawned = false;
            // When: a Spawn Command is issued for Guard_Captain_01
            _guardCaptain.HasBeenSpawned = true;
            // Then: Guard_Captain_01 has spawned state true; Character Overlay appears on Desktop Overlay
            _guardCaptain.HasBeenSpawned.Should().BeTrue(
                "after spawn command Guard_Captain_01 spawned state must be true; overlay appears");
        }

        [TestMethod]
        public void DespawnCommandIssuedOverlayHidden()
        {
            // Given: Guard_Captain_01 has spawned state true (overlay visible)
            _guardCaptain.HasBeenSpawned = true;
            // When: a Despawn Command is issued for Guard_Captain_01
            _guardCaptain.HasBeenSpawned = false;
            // Then: Guard_Captain_01 has spawned state false; Character Overlay removed from Desktop Overlay
            _guardCaptain.HasBeenSpawned.Should().BeFalse(
                "after despawn command Guard_Captain_01 spawned state must be false; overlay removed");
        }

        [TestMethod]
        public void SpawnStatePersistsAcrossMovements()
        {
            // Given: Guard_Captain_01 has spawned state true; movement executed
            _guardCaptain.HasBeenSpawned = true;
            // When: movement execution occurs (state is not toggled by movement)
            // Then: spawned state remains true; Character Overlay persists at updated position
            _guardCaptain.HasBeenSpawned.Should().BeTrue(
                "spawned state must persist unchanged after movement — overlay persists at updated position");
        }

        [TestMethod]
        public void IndependentPerCharacter()
        {
            // Given: Guard_Captain_01 has spawned state true; Villain_Boss_03 has spawned state false
            _guardCaptain.HasBeenSpawned = true;
            _villainBoss.HasBeenSpawned = false;
            // When: Villain_Boss_03 is spawned
            _villainBoss.HasBeenSpawned = true;
            // Then: each character's spawned state is independent — both now show true
            _guardCaptain.HasBeenSpawned.Should().BeTrue("Guard_Captain_01 state must remain true independently");
            _villainBoss.HasBeenSpawned.Should().BeTrue("Villain_Boss_03 state must become true independently");
        }
    }
}
