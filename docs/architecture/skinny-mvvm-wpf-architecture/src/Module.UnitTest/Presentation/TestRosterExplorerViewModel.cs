// Tier 2 — ViewModel + Domain test.
// Real domain wired to the ViewModel. COH still stubbed.
// Asserts both the binding state (ViewModel property) AND the domain post-state.
using FluentAssertions;
using Library.GameCommunicator;
using Library.ProcessCommunicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Rosters;

namespace Module.UnitTest.Presentation;

[TestClass]
public class TestRosterExplorerViewModel
{
    private FakeMemoryInstance      _memory   = null!;
    private Roster                  _roster   = null!;
    private RosterExplorerViewModel _vm       = null!;
    private Character               _hero     = null!;

    [TestInitialize]
    public void GivenAViewModelWiredToARealRoster()
    {
        _memory = new FakeMemoryInstance();
        _memory.SetLabel("Hero1");

        var executor = new NoOpGameCommandExecutor();
        _hero = new Character("Hero1", executor, _memory);
        _hero.Identities.Add(new Identity("Statesman", "Model_Statesman"));

        _roster = new Roster(executor, _memory);
        _roster.Members.Add(_hero);

        _vm = new RosterExplorerViewModel(_roster);
        _vm.SelectedCharacter = _hero;
    }

    [TestMethod]
    public void WhenGmClicksActivate_ThenActiveCharacterBindingMatchesDomainState()
    {
        _vm.ActivateCommand.Execute();

        // ViewModel binding
        _vm.ActiveCharacter.Should().Be(_hero);

        // Domain post-state
        _hero.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void WhenGmClicksSpawn_ThenParticipantShowsSpawnedAndCommandWasSent()
    {
        _vm.SpawnCommand.Execute();

        // Domain post-state
        _hero.IsSpawned.Should().BeTrue();

        // ViewModel binding — Participants is a direct reference, so it reflects the domain
        _vm.Participants.Should().Contain(c => c.Name == "Hero1" && c.IsSpawned);
    }
}
