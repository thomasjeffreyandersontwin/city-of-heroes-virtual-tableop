using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.UnitTest.Crowds
{
    /// <summary>
    /// RED acceptance tests for the <c>Browse and Activate Crowd Files</c> story
    /// (specification-by-example-increment-1.md). One <c>[TestMethod]</c> per
    /// scenario; Given/When/Then steps delegate to orchestrator helpers below.
    ///
    /// Surface under test: <see cref="CrowdRepository"/> driven directly. The
    /// existing async callback contract is reused; tests wrap the callback in a
    /// <see cref="ManualResetEventSlim"/> wait so each test reads synchronously.
    /// </summary>
    [TestClass]
    public class CrowdRepositoryTestSuite
    {
        private const int AwaitTimeoutMs = 5000;
        private const string ActiveCrowdListFilename = "active-crowds.json";

        private string _dataDirectory;
        private string _activeCrowdListPath;
        private CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDirectory = Path.Combine(
                Path.GetTempPath(),
                "coh-vtt-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDirectory);

            _activeCrowdListPath = Path.Combine(_dataDirectory, ActiveCrowdListFilename);

            _repository = new CrowdRepository { DataDirectory = _dataDirectory };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_dataDirectory))
                    Directory.Delete(_dataDirectory, recursive: true);
            }
            catch
            {
                // best-effort temp cleanup; an orphaned dir is not a test failure
            }
        }

        // ====================================================================
        //                           Scenarios
        // ====================================================================

        [TestMethod]
        public void ActivateSingleCrowdFile()
        {
            GivenCrowdFileExistsOnDisk("armageddons.json", file => file
                .TopLevel("Armageddon Squad")
                    .WithCharacter("Battle Maiden")
                    .WithCharacter("Manticore")
                    .WithNested("Demolition Team", nested => nested
                        .WithCharacter("Demo Lead")));
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmBrowsesAndActivates("armageddons.json");

            ThenCrowdTreeShowsTopLevelCrowdMatchingFileOnDisk("Armageddon Squad", "armageddons.json");
            ThenAllCharactersCrowdListsAlphabetically("Battle Maiden", "Demo Lead", "Manticore");
            ThenPersistedActiveCrowdListContainsExactly("armageddons.json");
        }

        [TestMethod]
        public void ActivateSeveralCrowdFilesInOneBrowseProcessedInSelectionOrder()
        {
            GivenCrowdFileExistsOnDisk("heroes.json", file => file
                .TopLevel("Freedom Phalanx")
                    .WithCharacter("Statesman")
                    .WithCharacter("Positron"));
            GivenCrowdFileExistsOnDisk("villains.json", file => file
                .TopLevel("Council Empire")
                    .WithCharacter("Marcus Valerius"));
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmBrowsesAndActivates("heroes.json", "villains.json");

            ThenCrowdTreeContainsTopLevelCrowdsInOrder("Freedom Phalanx", "Council Empire");
            ThenPersistedActiveCrowdListContainsInOrder("heroes.json", "villains.json");
        }

        [TestMethod]
        public void CloningCrowdFileSuffixesOnlyTopLevelCrowdNamesLeavingNestedCrowdNamesAlone()
        {
            GivenCrowdFileExistsOnDisk("villains.json", file => file
                .TopLevel("Council Empire")
                    .WithNested("Vampyri", nested => nested
                        .WithCharacter("Galaxy")));
            GivenPersistedActiveCrowdListContains("villains.json");
            GivenCharacterCrowdMainWorkspaceIsOpen();
            byte[] originalBytes = ReadBytes("villains.json");

            WhenGmBrowsesAndActivates("villains.json");

            ThenCloneOnDiskHasTopLevelAndNested(
                cloneFilename:        "villains (2).json",
                expectedTopLevelName: "Council Empire (2)",
                expectedNestedName:   "Vampyri",
                expectedCharacter:    "Galaxy");
            ThenCrowdFileIsByteUnchanged("villains.json", originalBytes);
        }

        [TestMethod]
        public void MalformedCrowdFileIsSkippedWithoutAbortingTheOthers()
        {
            GivenCrowdFileExistsOnDiskWithMalformedJson("broken.json");
            GivenCrowdFileExistsOnDisk("heroes.json", file => file
                .TopLevel("Freedom Phalanx")
                    .WithCharacter("Statesman"));
            GivenPersistedActiveCrowdListIsEmpty();
            GivenCharacterCrowdMainWorkspaceIsOpen();

            WhenGmBrowsesAndActivates("broken.json", "heroes.json");

            ThenPersistedActiveCrowdListContainsExactly("heroes.json");
            ThenCrowdTreeHasTopLevelCrowdNamed("Freedom Phalanx");
            ThenNoCrowdFromFileAppearsInCrowdTree("broken.json");
        }

        // -------- Scenario Outline: Re-activating picks the next available integer suffix --------

        [TestMethod]
        public void ReActivatingActiveCrowdFile_FirstClone_PicksSuffix2()
        {
            GivenArmageddonsOriginalOnDisk();
            GivenPersistedActiveCrowdListContains("armageddons.json");

            WhenGmBrowsesAndActivates("armageddons.json");

            ThenCloneOnDiskHasTopLevel(
                cloneFilename:        "armageddons (2).json",
                expectedTopLevelName: "Armageddon Squad (2)");
            ThenPersistedActiveCrowdListAlsoContains("armageddons (2).json");
        }

        [TestMethod]
        public void ReActivatingActiveCrowdFile_SecondClone_PicksSuffix3()
        {
            GivenArmageddonsOriginalOnDisk();
            GivenCloneAlreadyOnDisk("armageddons (2).json", topLevelName: "Armageddon Squad (2)");
            GivenPersistedActiveCrowdListContains("armageddons.json", "armageddons (2).json");

            WhenGmBrowsesAndActivates("armageddons.json");

            ThenCloneOnDiskHasTopLevel(
                cloneFilename:        "armageddons (3).json",
                expectedTopLevelName: "Armageddon Squad (3)");
            ThenPersistedActiveCrowdListAlsoContains("armageddons (3).json");
        }

        [TestMethod]
        public void ReActivatingActiveCrowdFile_ThirdClone_PicksSuffix4()
        {
            GivenArmageddonsOriginalOnDisk();
            GivenCloneAlreadyOnDisk("armageddons (2).json", topLevelName: "Armageddon Squad (2)");
            GivenCloneAlreadyOnDisk("armageddons (3).json", topLevelName: "Armageddon Squad (3)");
            GivenPersistedActiveCrowdListContains("armageddons.json", "armageddons (2).json", "armageddons (3).json");

            WhenGmBrowsesAndActivates("armageddons.json");

            ThenCloneOnDiskHasTopLevel(
                cloneFilename:        "armageddons (4).json",
                expectedTopLevelName: "Armageddon Squad (4)");
            ThenPersistedActiveCrowdListAlsoContains("armageddons (4).json");
        }

        [TestMethod]
        public void ReActivatingActiveCrowdFile_FillTheGap_PicksSuffix2WhenSuffix2IsMissing()
        {
            GivenArmageddonsOriginalOnDisk();
            // suffix (2) was deleted out-of-band; only (3) survives on disk and in the active list
            GivenCloneAlreadyOnDisk("armageddons (3).json", topLevelName: "Armageddon Squad (3)");
            GivenPersistedActiveCrowdListContains("armageddons.json", "armageddons (3).json");

            WhenGmBrowsesAndActivates("armageddons.json");

            ThenCloneOnDiskHasTopLevel(
                cloneFilename:        "armageddons (2).json",
                expectedTopLevelName: "Armageddon Squad (2)");
            ThenPersistedActiveCrowdListAlsoContains("armageddons (2).json");
        }

        // ====================================================================
        //                       Given / When / Then helpers
        // ====================================================================

        private void GivenCrowdFileExistsOnDisk(string filename, Action<CrowdFileBuilder> build)
        {
            var builder = new CrowdFileBuilder();
            build(builder);
            Helper.SerializeObjectAsJSONToFile(PathFor(filename), builder.Build());
        }

        private void GivenCrowdFileExistsOnDiskWithMalformedJson(string filename)
        {
            File.WriteAllText(PathFor(filename), "{ this is not valid json :::");
        }

        private void GivenPersistedActiveCrowdListIsEmpty()
        {
            if (File.Exists(_activeCrowdListPath))
                File.Delete(_activeCrowdListPath);
        }

        private void GivenPersistedActiveCrowdListContains(params string[] filenames)
        {
            var absolutePaths = filenames.Select(PathFor).ToList();
            Helper.SerializeObjectAsJSONToFile(_activeCrowdListPath, absolutePaths);
        }

        /// <summary>
        /// Equivalent to opening the Character Crowd Main Workspace — the
        /// repository reads the persisted active crowd list and loads every
        /// active crowd file into the in-memory aggregate.
        /// </summary>
        private void GivenCharacterCrowdMainWorkspaceIsOpen()
        {
            AwaitLoadActiveCrowdFiles();
        }

        private void GivenArmageddonsOriginalOnDisk()
        {
            GivenCrowdFileExistsOnDisk("armageddons.json", file => file
                .TopLevel("Armageddon Squad")
                    .WithCharacter("Battle Maiden")
                    .WithNested("Demolition Team", nested => nested
                        .WithCharacter("Demo Lead")));
        }

        private void GivenCloneAlreadyOnDisk(string filename, string topLevelName)
        {
            GivenCrowdFileExistsOnDisk(filename, file => file
                .TopLevel(topLevelName)
                    .WithCharacter("Battle Maiden")
                    .WithNested("Demolition Team", nested => nested
                        .WithCharacter("Demo Lead")));
        }

        private IList<CrowdModel> WhenGmBrowsesAndActivates(params string[] filenames)
        {
            return AwaitBrowseAndActivate(filenames.Select(PathFor).ToArray());
        }

        private void ThenCrowdTreeHasTopLevelCrowdNamed(string crowdName)
        {
            IList<CrowdModel> crowds = AwaitGetCrowdCollection();
            Assert.IsTrue(
                crowds.Any(c => c.Name == crowdName),
                $"Expected top-level Crowd '{crowdName}' in the in-memory aggregate. " +
                $"Actual: {FormatCrowdList(crowds)}");
        }

        private void ThenCrowdTreeShowsTopLevelCrowdMatchingFileOnDisk(string crowdName, string filename)
        {
            IList<CrowdModel> crowds = AwaitGetCrowdCollection();
            CrowdModel inMemory = crowds.FirstOrDefault(c => c.Name == crowdName);
            Assert.IsNotNull(inMemory,
                $"Expected top-level Crowd '{crowdName}' in the in-memory aggregate. " +
                $"Actual: {FormatCrowdList(crowds)}");

            var onDiskList = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(PathFor(filename));
            CrowdModel onDisk = onDiskList?.FirstOrDefault(c => c.Name == crowdName);
            Assert.IsNotNull(onDisk, $"File '{filename}' on disk does not contain top-level Crowd '{crowdName}'.");

            Assert.AreEqual(
                FormatCrowdShape(onDisk),
                FormatCrowdShape(inMemory),
                $"In-memory Crowd shape differs from on-disk shape for '{crowdName}'.");
        }

        private void ThenCrowdTreeContainsTopLevelCrowdsInOrder(params string[] crowdNames)
        {
            IList<CrowdModel> crowds = AwaitGetCrowdCollection();
            List<string> actualTopLevels = crowds
                .Where(c => c.Name != "All Characters")
                .Select(c => c.Name)
                .ToList();
            CollectionAssert.AreEqual(
                crowdNames.ToList(),
                actualTopLevels,
                $"Top-level crowd order mismatch. Expected: [{string.Join(", ", crowdNames)}]. " +
                $"Actual: [{string.Join(", ", actualTopLevels)}].");
        }

        private void ThenAllCharactersCrowdListsAlphabetically(params string[] characterNames)
        {
            IList<CrowdModel> crowds = AwaitGetCrowdCollection();
            CrowdModel allCharacters = crowds.FirstOrDefault(c => c.Name == "All Characters");
            Assert.IsNotNull(allCharacters,
                "The All Characters crowd is missing from the in-memory aggregate.");

            List<string> actualNames = allCharacters.CrowdMemberCollection
                .Select(m => m.Name)
                .ToList();
            CollectionAssert.AreEqual(
                characterNames.ToList(),
                actualNames,
                $"All Characters listing mismatch. Expected: [{string.Join(", ", characterNames)}]. " +
                $"Actual: [{string.Join(", ", actualNames)}].");
        }

        private void ThenNoCrowdFromFileAppearsInCrowdTree(string filename)
        {
            // Observable at the repository layer: the active list does NOT contain
            // the failing file; therefore no crowd sourced from that file is in the
            // aggregate. We assert the active-list absence directly.
            string fullPath = PathFor(filename);
            List<string> activeList = ReadActiveCrowdListOrEmpty();
            CollectionAssert.DoesNotContain(activeList, fullPath,
                $"Active Crowd List unexpectedly contains failing file '{filename}'.");
        }

        private void ThenPersistedActiveCrowdListContainsExactly(params string[] filenames)
        {
            List<string> expected = filenames.Select(PathFor).ToList();
            List<string> actual = ReadActiveCrowdListOrEmpty();
            CollectionAssert.AreEquivalent(expected, actual,
                $"Active Crowd List mismatch. Expected: [{string.Join(", ", expected)}]. " +
                $"Actual: [{string.Join(", ", actual)}].");
        }

        private void ThenPersistedActiveCrowdListContainsInOrder(params string[] filenames)
        {
            List<string> expected = filenames.Select(PathFor).ToList();
            List<string> actual = ReadActiveCrowdListOrEmpty();
            CollectionAssert.AreEqual(expected, actual,
                $"Active Crowd List order mismatch. Expected: [{string.Join(", ", expected)}]. " +
                $"Actual: [{string.Join(", ", actual)}].");
        }

        private void ThenPersistedActiveCrowdListAlsoContains(string filename)
        {
            string fullPath = PathFor(filename);
            List<string> actual = ReadActiveCrowdListOrEmpty();
            CollectionAssert.Contains(actual, fullPath,
                $"Active Crowd List does not contain expected entry '{filename}'. " +
                $"Actual: [{string.Join(", ", actual)}].");
        }

        private void ThenCloneOnDiskHasTopLevel(string cloneFilename, string expectedTopLevelName)
        {
            string clonePath = PathFor(cloneFilename);
            Assert.IsTrue(File.Exists(clonePath),
                $"Expected clone file '{cloneFilename}' to exist on disk after re-activation.");

            var loaded = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(clonePath);
            Assert.IsNotNull(loaded, $"Clone file '{cloneFilename}' is empty or unreadable.");
            Assert.IsTrue(loaded.Any(c => c.Name == expectedTopLevelName),
                $"Clone file '{cloneFilename}' is missing top-level Crowd '{expectedTopLevelName}'. " +
                $"Found: [{string.Join(", ", loaded.Select(c => c.Name))}].");
        }

        private void ThenCloneOnDiskHasTopLevelAndNested(
            string cloneFilename,
            string expectedTopLevelName,
            string expectedNestedName,
            string expectedCharacter)
        {
            string clonePath = PathFor(cloneFilename);
            Assert.IsTrue(File.Exists(clonePath),
                $"Expected clone file '{cloneFilename}' to exist on disk.");

            var loaded = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(clonePath);
            CrowdModel topLevel = loaded?.FirstOrDefault(c => c.Name == expectedTopLevelName);
            Assert.IsNotNull(topLevel,
                $"Clone '{cloneFilename}' is missing top-level Crowd '{expectedTopLevelName}'.");

            CrowdModel nested = topLevel.CrowdMemberCollection
                .OfType<CrowdModel>()
                .FirstOrDefault(c => c.Name == expectedNestedName);
            Assert.IsNotNull(nested,
                $"Clone '{cloneFilename}' top-level '{expectedTopLevelName}' is missing nested Crowd " +
                $"'{expectedNestedName}'. Nested names found: " +
                $"[{string.Join(", ", topLevel.CrowdMemberCollection.OfType<CrowdModel>().Select(c => c.Name))}]. " +
                "Nested Crowd names must NOT receive the integer suffix.");

            Assert.IsTrue(nested.CrowdMemberCollection.Any(m => m.Name == expectedCharacter),
                $"Clone '{cloneFilename}' nested Crowd '{expectedNestedName}' is missing Character '{expectedCharacter}'.");
        }

        private void ThenCrowdFileIsByteUnchanged(string filename, byte[] originalBytes)
        {
            byte[] currentBytes = ReadBytes(filename);
            CollectionAssert.AreEqual(
                originalBytes,
                currentBytes,
                $"Original file '{filename}' was unexpectedly modified during the clone operation.");
        }

        // ====================================================================
        //                       Sync wrappers (callback → blocking)
        // ====================================================================

        private IList<CrowdModel> AwaitBrowseAndActivate(params string[] selectedPaths)
        {
            using (var done = new ManualResetEventSlim(initialState: false))
            {
                IList<CrowdModel> result = null;
                _repository.BrowseAndActivate(selectedPaths, crowds =>
                {
                    result = crowds;
                    done.Set();
                });
                Assert.IsTrue(
                    done.Wait(AwaitTimeoutMs),
                    $"BrowseAndActivate did not complete within {AwaitTimeoutMs} ms.");
                return result;
            }
        }

        private void AwaitLoadActiveCrowdFiles()
        {
            using (var done = new ManualResetEventSlim(initialState: false))
            {
                _repository.LoadActiveCrowdFiles(_ => done.Set());
                Assert.IsTrue(
                    done.Wait(AwaitTimeoutMs),
                    $"LoadActiveCrowdFiles did not complete within {AwaitTimeoutMs} ms.");
            }
        }

        private IList<CrowdModel> AwaitGetCrowdCollection()
        {
            using (var done = new ManualResetEventSlim(initialState: false))
            {
                List<CrowdModel> result = null;
                _repository.GetCrowdCollection(crowds =>
                {
                    result = crowds;
                    done.Set();
                });
                Assert.IsTrue(
                    done.Wait(AwaitTimeoutMs),
                    $"GetCrowdCollection did not complete within {AwaitTimeoutMs} ms.");
                return result ?? new List<CrowdModel>();
            }
        }

        // ====================================================================
        //                       Utility
        // ====================================================================

        private string PathFor(string filename) => Path.Combine(_dataDirectory, filename);

        private byte[] ReadBytes(string filename) => File.ReadAllBytes(PathFor(filename));

        private List<string> ReadActiveCrowdListOrEmpty()
        {
            if (!File.Exists(_activeCrowdListPath))
                return new List<string>();
            return Helper.GetDeserializedJSONFromFile<List<string>>(_activeCrowdListPath)
                   ?? new List<string>();
        }

        private static string FormatCrowdList(IList<CrowdModel> crowds) =>
            "[" + string.Join(", ", crowds.Select(c => c?.Name ?? "<null>")) + "]";

        private static string FormatCrowdShape(CrowdModel crowd)
        {
            // Stable signature: top-level name + sorted nested-crowd + character signatures.
            if (crowd == null) return "<null>";
            var members = crowd.CrowdMemberCollection?
                .OrderBy(m => m is CrowdModel ? 0 : 1)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .Select(FormatMember)
                .ToList() ?? new List<string>();
            return crowd.Name + "{" + string.Join(",", members) + "}";
        }

        private static string FormatMember(ICrowdMember m)
        {
            if (m is CrowdModel nested) return FormatCrowdShape(nested);
            return m.Name;
        }

        // ====================================================================
        //                       Domain DSL (test fixture builders)
        // ====================================================================

        /// <summary>Builds a <see cref="List{CrowdModel}"/> for a crowd-file payload.</summary>
        private class CrowdFileBuilder
        {
            private readonly List<CrowdModel> _topLevels = new List<CrowdModel>();

            public CrowdBuilder TopLevel(string name)
            {
                var crowd = new CrowdModel { Name = name };
                _topLevels.Add(crowd);
                return new CrowdBuilder(crowd, this);
            }

            public List<CrowdModel> Build() => _topLevels;
        }

        private class CrowdBuilder
        {
            private readonly CrowdModel _crowd;
            private readonly CrowdFileBuilder _fileBuilder;

            public CrowdBuilder(CrowdModel crowd, CrowdFileBuilder fileBuilder)
            {
                _crowd = crowd;
                _fileBuilder = fileBuilder;
            }

            public CrowdBuilder WithCharacter(string name)
            {
                _crowd.Add(new CrowdMemberModel { Name = name });
                return this;
            }

            public CrowdBuilder WithNested(string nestedCrowdName, Action<CrowdBuilder> buildNested)
            {
                var nested = new CrowdModel { Name = nestedCrowdName };
                _crowd.Add(nested);
                buildNested(new CrowdBuilder(nested, _fileBuilder));
                return this;
            }

            public CrowdBuilder TopLevel(string name) => _fileBuilder.TopLevel(name);
        }
    }
}
