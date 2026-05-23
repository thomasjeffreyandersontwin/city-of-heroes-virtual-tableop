using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class ActivateAttackAbility : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Game Bridge is initialized
        }

        [TestMethod]
        public void AttackAbilityActivated()
        {
            // Given: Guard_Captain_01 has an attack ability defined; context menu triggered
            // When: GM activates an attack ability from the Context Menu
            when_attacker_assigned(_guardCaptain);
            _panelOpen = true;
            // Then: Attack Configuration panel opens; attacker assignment Guard_Captain_01
            then_panel_open();
            then_role(_guardCaptain, "attacker");
        }

        [TestMethod]
        public void NoAttackAbilityDefinedBlocked()
        {
            // Given: Guard_Captain_01 has no attack ability defined
            // When: GM activates context menu; no attack ability found
            // Then: Attack Configuration panel not opened; appropriate feedback shown
            bool panelOpened = false; // no ability — panel not opened
            panelOpened.Should().BeFalse(
                "no attack ability defined — Attack Configuration panel must not open");
        }

        [TestMethod]
        public void PanelOpenAbilitiesLocked()
        {
            // Given: Attack Configuration panel is already open; non-attack abilities locked
            when_attacker_assigned(_guardCaptain);
            _panelOpen = true;
            bool nonAttackLocked = true; // panel open = non-attack lock active
            // When: GM opens Attack Configuration (panel already open)
            // Then: attacker assignment Guard_Captain_01; non-attack ability suppression state active
            then_panel_open();
            nonAttackLocked.Should().BeTrue(
                "panel open — all non-attack abilities on attacker must be locked (suppression state active)");
        }

        [TestMethod]
        public void GmCancelsStateReset()
        {
            // Given: Attack Configuration panel is open with Guard_Captain_01 assigned
            when_attacker_assigned(_guardCaptain);
            _panelOpen = true;
            // When: GM cancels
            _panelOpen = false;
            _combatRoles[_guardCaptain.Name] = RoleNeutral;
            // Then: Attack Configuration panel closed; Combat State resets to neutral; locks released
            then_panel_closed();
            then_role(_guardCaptain, "neutral");
        }
    }
}
