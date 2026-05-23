// Tier 1 — Domain tests.
// Pure: no ViewModel, no WPF, no real file system.
// File-system seam replaced by FakeCrowdFileAccess.
// Game seam replaced by NoOpGameCommandExecutor + FakeMemoryInstance (characters are data-only here).
using FluentAssertions;
using Library.GameCommunicator;
using Library.ProcessCommunicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.Domain;

// ---------------------------------------------------------------------------
// Story: Track Source File per Crowd
// ---------------------------------------------------------------------------

[TestClass]
public class TestCrowdSourceFileTracking
{
    [TestMethod]
    public void WhenCrowdCreated_ThenSourceFileIsNullAndIsDirtyIsTrue()
    {
        var crowd = CrowdTestHelpers.GivenNewCrowd("Dock Workers");

        crowd.SourceFile.Should().BeNull();
        crowd.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void WhenMemberAdded_ThenCrowdIsDirty()
    {
        var crowd = CrowdTestHelpers.GivenLoadedCrowd("Harbor Patrol", sourceFile: @"C:\coh\data\harbor_patrol.json");

        crowd.Add(CrowdTestHelpers.MakeCharacter("Guard 1"));

        crowd.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void WhenCrowdLoadedFromRepository_ThenSourceFileRestoredAndIsDirtyFalse()
    {
        var crowd = CrowdTestHelpers.GivenLoadedCrowd(
            name: "Council Minions",
            sourceFile: @"C:\coh\data\council.json");

        crowd.SourceFile.Should().Be(@"C:\coh\data\council.json");
        crowd.IsDirty.Should().BeFalse();
    }

    [TestMethod]
    public void WhenCrowdLoadedWithNoSourceFileStored_ThenSourceFileIsNull()
    {
        var crowd = CrowdTestHelpers.GivenLoadedCrowd("Random Thugs", sourceFile: null);

        crowd.SourceFile.Should().BeNull();
        crowd.IsDirty.Should().BeFalse();
    }

}

// ---------------------------------------------------------------------------
// Story: Save Dirty Crowds to Source Files
// ---------------------------------------------------------------------------

[TestClass]
public class TestSaveDirtyCrowdsToSourceFiles
{
    private FakeCrowdFileAccess _fs = null!;
    private CrowdRepository     _repo = null!;

    [TestInitialize]
    public void GivenARepositoryWithTwoDirtyCrowdsWithSourceFiles()
    {
        _fs   = new FakeCrowdFileAccess();
        _repo = new CrowdRepository(_fs);

        var longbow = Crowd.Restore("Longbow Squad", @"C:\coh\data\longbow.json", isDirty: true);
        longbow.Add(CrowdTestHelpers.MakeCharacter("Agent 1"));
        var crey = Crowd.Restore("Crey Agents", @"C:\coh\data\crey.json", isDirty: true);
        crey.Add(CrowdTestHelpers.MakeCharacter("Scientist 1"));

        _repo.Add(longbow);
        _repo.Add(crey);
    }

    [TestMethod]
    public void WhenSaveDirtyInvoked_ThenEachDirtyCrowdWrittenToItsSourceFile()
    {
        _repo.SaveDirtyCrowds();

        _fs.WrittenPaths.Should().Contain(@"C:\coh\data\longbow.json");
        _fs.WrittenPaths.Should().Contain(@"C:\coh\data\crey.json");
    }

    [TestMethod]
    public void WhenSaveDirtyInvoked_ThenDirtyFlagsCleared()
    {
        _repo.SaveDirtyCrowds();

        _repo.Crowds.Should().OnlyContain(c => !c.IsDirty);
    }

    [TestMethod]
    public void WhenNoCrowdIsDirty_ThenNoFileIsWritten()
    {
        _fs   = new FakeCrowdFileAccess();
        _repo = new CrowdRepository(_fs);
        _repo.Add(Crowd.Restore("Clean Crowd", @"C:\coh\data\clean.json", isDirty: false));

        _repo.SaveDirtyCrowds();

        _fs.WrittenPaths.Should().BeEmpty();
    }

    [TestMethod]
    public void WhenDirtyCrowdHasNoSourceFile_ThenItIsSkippedByAutoSave()
    {
        _fs   = new FakeCrowdFileAccess();
        _repo = new CrowdRepository(_fs);
        var unsaved = new Crowd("New Gang");    // SourceFile == null, IsDirty == true
        _repo.Add(unsaved);

        _repo.SaveDirtyCrowds();

        _fs.WrittenPaths.Should().BeEmpty();
        unsaved.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void WhenOneWriteFails_ThenThatCrowdRemainsDirectyAndOthersAreStillSaved()
    {
        _fs.FailOnPath = @"C:\coh\data\longbow.json";

        var result = _repo.SaveDirtyCrowds();

        result.Failures.Should().ContainSingle(f => f.Path == @"C:\coh\data\longbow.json");
        _fs.WrittenPaths.Should().Contain(@"C:\coh\data\crey.json");
        _repo.Crowds.First(c => c.Name == "Longbow Squad").IsDirty.Should().BeTrue();
        _repo.Crowds.First(c => c.Name == "Crey Agents").IsDirty.Should().BeFalse();
    }
}

// ---------------------------------------------------------------------------
// Story: Save Crowd to New File
// ---------------------------------------------------------------------------

[TestClass]
public class TestSaveCrowdToNewFile
{
    private FakeCrowdFileAccess _fs   = null!;
    private CrowdRepository     _repo = null!;
    private Crowd               _warriors = null!;

    [TestInitialize]
    public void GivenATopLevelCrowdWithNoSourceFile()
    {
        _fs       = new FakeCrowdFileAccess();
        _repo     = new CrowdRepository(_fs);
        _warriors = new Crowd("Warriors");
        _warriors.Add(CrowdTestHelpers.MakeCharacter("Sword Guy"));
        _repo.Add(_warriors);
    }

    [TestMethod]
    public void WhenSaveToNewFile_ThenFileWrittenAtChosenPath()
    {
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        _fs.WrittenPaths.Should().Contain(@"C:\coh\data\warriors.json");
    }

    [TestMethod]
    public void WhenSaveToNewFile_ThenSourceFileAssignedOnCrowd()
    {
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        _warriors.SourceFile.Should().Be(@"C:\coh\data\warriors.json");
    }

    [TestMethod]
    public void WhenSaveToNewFile_ThenDirtyFlagCleared()
    {
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        _warriors.IsDirty.Should().BeFalse();
    }

    [TestMethod]
    public void WhenSaveToNewFile_ThenPathAddedToActiveCrowdList()
    {
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        _repo.ActiveCrowdList.Should().Contain(@"C:\coh\data\warriors.json");
    }

    [TestMethod]
    public void WhenSaveToNewFileFails_ThenSourceFileUnchangedAndCrowdRemainsDirecty()
    {
        _fs.FailOnPath = @"C:\coh\data\warriors.json";

        var act = () => _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        act.Should().Throw<CrowdSaveException>();
        _warriors.SourceFile.Should().BeNull();
        _warriors.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void WhenSamePathSavedTwice_ThenActiveCrowdListContainsItOnce()
    {
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");
        _warriors.Add(CrowdTestHelpers.MakeCharacter("Shield Guy"));
        _repo.SaveCrowdToNewFile(_warriors, @"C:\coh\data\warriors.json");

        _repo.ActiveCrowdList.Should().ContainSingle(p => p == @"C:\coh\data\warriors.json");
    }

    [TestMethod]
    public void WhenNestedCrowdPassedToSaveToNewFile_ThenThrows()
    {
        var parent = new Crowd("Warriors");
        var nested = new Crowd("Foot Soldiers");
        parent.AddNestedCrowd(nested);
        _repo.Add(parent);

        var act = () => _repo.SaveCrowdToNewFile(nested, @"C:\coh\data\foot.json");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*top-level*");
    }
}

// ---------------------------------------------------------------------------
// Shared test helpers
// ---------------------------------------------------------------------------

file static class CrowdTestHelpers
{
    /// <summary>
    /// Creates a character with no-op game seams — suitable for pure data tests
    /// where the character is never spawned or animated.
    /// </summary>
    internal static Character MakeCharacter(string name) =>
        new Character(name, new NoOpGameCommandExecutor(), new FakeMemoryInstance());

    internal static Crowd GivenNewCrowd(string name) =>
        new Crowd(name);

    internal static Crowd GivenLoadedCrowd(string name, string? sourceFile) =>
        Crowd.Restore(name, sourceFile, isDirty: false);
}
