// Tier 3 — E2E key-path test.
// One test for the full gesture → roster indicator path.
// All three layers wired together; COH still stubbed.
using FluentAssertions;
using Library.GameCommunicator;
using Library.ProcessCommunicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Rosters;

namespace Module.UnitTest.E2E;

[TestClass]
public class TestSpawnKeyPath
{
    [TestMethod]
    public void WhenGmClicksSpawn_ThenRosterShowsSpawnedIndicator()
    {
        var memory   = new FakeMemoryInstance();
        var executor = new NoOpGameCommandExecutor();
        var hero     = new Character("Guard1", executor, memory);
        hero.Identities.Add(new Identity("Minion", "Minion_Villain"));
        memory.SetLabel("Guard1");   // game confirms spawn

        var roster = new Roster(executor, memory);
        roster.Members.Add(hero);

        var vm = new RosterExplorerViewModel(roster);
        vm.SelectedCharacter = hero;

        vm.SpawnCommand.Execute();

        hero.IsSpawned.Should().BeTrue();
        vm.Participants.Should().Contain(c => c.Name == "Guard1" && c.IsSpawned);
        executor.LastCommand.Should().Contain("Minion_Villain").And.Contain("Guard1");
    }
}
