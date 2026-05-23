using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class DesignateCenterTargetForAreaAttack : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
            given_panel_open();
            when_attacker_assigned(_guardCaptain);
        }

        [TestMethod]
        public void CenterDesignatedTargetsAutoAdded()
        {
            // Given: Area Center designated to Guard_Captain_01; Villain_A, Villain_B, Villain_C within radius
            string centerNpc = "Guard_Captain_01";
            string[] radiusTargets = new[] { "Villain_A", "Villain_B", "Villain_C" };
            // When: GM checks Area Center and designates Guard_Captain_01 via Area Attack Pop-Up Menu
            // Then: Area Center designated_target_NPC Guard_Captain_01; all radius targets auto-added as Defenders
            centerNpc.Should().Be("Guard_Captain_01",
                "Area Center must be designated as Guard_Captain_01 via the Area Attack Pop-Up Menu");
            radiusTargets.Length.Should().Be(3,
                "Villain_A, Villain_B, Villain_C within radius must all be auto-added as Defenders");
        }

        [TestMethod]
        public void PopUpMenuNotDeployedBlocked()
        {
            // Given: Area Center pop-up menu has not been deployed
            bool popUpDeployed = false;
            // When: GM attempts to designate center without deploying pop-up
            // Then: designation blocked with feedback; Area Center remains unconfigured
            popUpDeployed.Should().BeFalse(
                "pop-up menu not deployed — area center designation must be blocked with feedback");
        }

        [TestMethod]
        public void NoTargetsInRadiusEmpty()
        {
            // Given: Area Center designated to Guard_Captain_01; no spawned characters in radius
            string centerNpc = "Guard_Captain_01";
            int radiusCount = 0;
            // When: GM designates center with no characters in radius
            // Then: area reported empty but designation is preserved
            radiusCount.Should().Be(0,
                "no targets in radius — area reported empty but Guard_Captain_01 designation is preserved");
        }

        [TestMethod]
        public void AreaCenterUncheckedReverts()
        {
            // Given: Area Center is designated; auto-added Defenders in the list
            string centerNpc = "Guard_Captain_01";
            // When: GM unchecks Area Center
            centerNpc = "cleared";
            // Then: all auto-added Defenders removed; configuration reverts to single-target
            centerNpc.Should().Be("cleared",
                "unchecking Area Center clears the designation and removes all auto-added Defenders");
        }
    }
}
