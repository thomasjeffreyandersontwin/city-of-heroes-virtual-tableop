// Tier 1 — Domain test.
// Pure: no ViewModel, no WPF, no real COH process.
// COH seam replaced by FakeMemoryInstance + NoOpGameCommandExecutor.
using FluentAssertions;
using Library.GameCommunicator;
using Library.ProcessCommunicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;

namespace Module.UnitTest.Domain;

[TestClass]
public class TestCharacterSpawn
{
    private NoOpGameCommandExecutor _executor = null!;
    private FakeMemoryInstance      _memory   = null!;
    private Character               _hero     = null!;

    [TestInitialize]
    public void GivenACharacterWithAnIdentityAndMemoryConfirmingTheSpawn()
    {
        _executor = new NoOpGameCommandExecutor();
        _memory   = new FakeMemoryInstance();
        _hero     = new Character("Hero1", _executor, _memory);

        _hero.Identities.Add(new Identity("Statesman", "Model_Statesman"));

        // Pre-seed: simulate COH confirming the NPC is in memory after spawn
        _memory.SetLabel("Hero1");
    }

    [TestMethod]
    public void WhenSpawned_ThenIsSpawnedIsTrue()
    {
        _hero.Spawn();

        _hero.IsSpawned.Should().BeTrue();
    }

    [TestMethod]
    public void WhenSpawned_ThenCommandContainsModelSurfaceAndCharacterName()
    {
        _hero.Spawn();

        // Path 1: assert the keybind string sent through the game seam
        _executor.LastCommand.Should().Contain("Model_Statesman").And.Contain("Hero1");
    }

    [TestMethod]
    public void WhenIdentityAddedTwiceWithSameName_ThenOnlyOneIdentityExists()
    {
        // OptionGroup uniqueness invariant — no ViewModel involved
        _hero.Identities.Add(new Identity("Statesman", "Model_Statesman"));  // duplicate

        _hero.Identities.Should().HaveCount(1);
    }
}
