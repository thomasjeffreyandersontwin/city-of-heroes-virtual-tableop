// Shared Given/When/Then helpers for all Manage Crowd Repository story tests.
// Story test classes inherit from this base.
//
// Vocabulary (one word per concept - ATDD skill rule):
//   given_*  - setup / preconditions
//   when_*   - actions / triggers
//   then_*   - assertions / observable outcomes
//   create_* - factory helpers
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace CrowdManagement.E2ETests.ManageCrowdRepository
{
    public class ManageCrowdRepositoryHelper
    {
        protected static readonly string CrowdDataDir =
            @"C:\hero-desktop\city-of-heroes-virtual-tabletop\data\crowds";

        private static readonly string CrowdModelType =
            "Module.HeroVirtualTabletop.Crowds.CrowdModel, Module.HeroVirtualTabletop";

        private static readonly string CrowdMemberType =
            "Module.HeroVirtualTabletop.Crowds.CrowdMemberModel, Module.HeroVirtualTabletop";

        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenActiveCrowdListIsEmpty()
        {
            AppDriver.ClearActiveCrowdsJson();
        }

        protected void GivenActiveCrowdListContains(params string[] absoluteFilePaths)
        {
            AppDriver.WriteActiveCrowdsJson(absoluteFilePaths);
        }

        // Returns path only — file must already exist on disk.
        protected string GivenCrowdFileExistsOnDisk(string fileName)
        {
            return Path.Combine(CrowdDataDir, fileName);
        }

        // Creates a crowd file with one top-level crowd and optional direct characters.
        protected string GivenCrowdFileOnDisk(string fileName, string crowdName,
            params string[] characterNames)
        {
            string filePath = Path.Combine(CrowdDataDir, fileName);
            int nextId = 1;
            JObject top = BuildCrowdObject(crowdName, ref nextId);
            JArray members = new JArray();
            foreach (string ch in characterNames)
                members.Add(BuildMemberObject(ch, ref nextId));
            top["CrowdMemberCollection"] = members;
            JArray root = new JArray { top };
            File.WriteAllText(filePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            return filePath;
        }

        // Creates a crowd file with one top-level crowd and one nested crowd.
        protected string GivenCrowdFileOnDiskWithNested(string fileName, string crowdName,
            string nestedCrowdName, params string[] nestedCharacters)
        {
            string filePath = Path.Combine(CrowdDataDir, fileName);
            int nextId = 1;
            JObject nested = BuildCrowdObject(nestedCrowdName, ref nextId);
            JArray nestedMembers = new JArray();
            foreach (string ch in nestedCharacters)
                nestedMembers.Add(BuildMemberObject(ch, ref nextId));
            nested["CrowdMemberCollection"] = nestedMembers;

            JObject top = BuildCrowdObject(crowdName, ref nextId);
            top["CrowdMemberCollection"] = new JArray { nested };

            JArray root = new JArray { top };
            File.WriteAllText(filePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            return filePath;
        }

        // Creates a crowd file with one top-level crowd, direct characters, and multiple nested crowds.
        protected string GivenCrowdFileOnDiskWithMultipleNested(string fileName, string crowdName,
            string[] topLevelCharacters, IList<KeyValuePair<string, string[]>> nestedCrowds)
        {
            string filePath = Path.Combine(CrowdDataDir, fileName);
            int nextId = 1;
            JArray members = new JArray();
            if (topLevelCharacters != null)
            {
                foreach (string ch in topLevelCharacters)
                    members.Add(BuildMemberObject(ch, ref nextId));
            }
            if (nestedCrowds != null)
            {
                foreach (KeyValuePair<string, string[]> kv in nestedCrowds)
                {
                    JObject nested = BuildCrowdObject(kv.Key, ref nextId);
                    JArray nestedMembers = new JArray();
                    foreach (string ch in kv.Value)
                        nestedMembers.Add(BuildMemberObject(ch, ref nextId));
                    nested["CrowdMemberCollection"] = nestedMembers;
                    members.Add(nested);
                }
            }
            JObject top = BuildCrowdObject(crowdName, ref nextId);
            top["CrowdMemberCollection"] = members;
            JArray root = new JArray { top };
            File.WriteAllText(filePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            return filePath;
        }

        // Creates a file with content that will fail JSON deserialization.
        protected string GivenMalformedCrowdFileOnDisk(string fileName)
        {
            string filePath = Path.Combine(CrowdDataDir, fileName);
            File.WriteAllText(filePath, "{ this is NOT valid json ::: broken");
            return filePath;
        }

        // Returns the raw bytes of a file (for later byte-equality checks).
        protected byte[] ReadFileBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenCharacterCrowdMainWorkspaceOpens(string[] crowdFilePaths)
        {
            AppDriver.DeleteCrowdLoadErrorLog();
            Driver = new AppDriver();
            Driver.LaunchWithCrowdFiles(crowdFilePaths);
        }

        // Use when some files in crowdFilePaths won't produce a crowd (missing/malformed).
        // expectedLoadedCrowds sets the polling target in WaitForCrowdsToLoad.
        protected void WhenCharacterCrowdMainWorkspaceOpens(string[] crowdFilePaths,
            int expectedLoadedCrowds)
        {
            AppDriver.DeleteCrowdLoadErrorLog();
            Driver = new AppDriver();
            Driver.LaunchWithCrowdFiles(crowdFilePaths, expectedLoadedCrowds);
        }

        protected void WhenCharacterCrowdMainWorkspaceOpens()
        {
            AppDriver.DeleteCrowdLoadErrorLog();
            Driver = new AppDriver();
            Driver.LaunchWithCrowdFiles(new string[0]);
        }

        // Used by tests whose crowd tree was not yet visible when LaunchWithCrowdFiles returned.
        // Calls WaitForBrowseResultToAppear to give the async crowd load a second polling window.
        protected void WhenBrowseResultAppears(int expectedCount)
        {
            Driver.WaitForBrowseResultToAppear(expectedCount);
        }

        protected void WhenSaveDirtyIsInvoked()
        {
            Driver.ClickSaveButton();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenCrowdTreeShowsCrowd(string crowdName)
        {
            var names = Driver.GetTopLevelCrowdNames();
            CollectionAssert.Contains(names, crowdName,
                string.Format("Expected crowd '{0}' in tree. Actual top-level crowds: [{1}]",
                    crowdName, string.Join(", ", names)));
        }

        protected void ThenCrowdTreeDoesNotShowCrowd(string crowdName)
        {
            var names = Driver.GetTopLevelCrowdNames();
            CollectionAssert.DoesNotContain(names, crowdName,
                string.Format("Expected crowd '{0}' NOT in tree. Actual top-level crowds: [{1}]",
                    crowdName, string.Join(", ", names)));
        }

        protected void ThenCrowdTreeShowsCrowdsInOrder(string[] expectedCrowdNames)
        {
            var actual = Driver.GetTopLevelCrowdNames();
            for (int i = 0; i < expectedCrowdNames.Length; i++)
            {
                Assert.IsTrue(actual.Contains(expectedCrowdNames[i]),
                    string.Format("Missing crowd '{0}'. Actual: [{1}]",
                        expectedCrowdNames[i], string.Join(", ", actual)));
            }
            if (expectedCrowdNames.Length > 1)
            {
                for (int i = 0; i < expectedCrowdNames.Length - 1; i++)
                {
                    int idxA = actual.IndexOf(expectedCrowdNames[i]);
                    int idxB = actual.IndexOf(expectedCrowdNames[i + 1]);
                    Assert.IsTrue(idxA < idxB,
                        string.Format("'{0}' should appear before '{1}' in tree",
                            expectedCrowdNames[i], expectedCrowdNames[i + 1]));
                }
            }
        }

        protected void ThenCrowdTreeShowsChildrenUnder(string parentCrowdName, string[] expectedChildNames)
        {
            var children = Driver.GetChildNamesUnder(parentCrowdName);
            foreach (var child in expectedChildNames)
            {
                CollectionAssert.Contains(children, child,
                    string.Format("Expected child '{0}' under '{1}'. Actual: [{2}]",
                        child, parentCrowdName, string.Join(", ", children)));
            }
        }

        protected void ThenNoCrowdLoadErrorsOccurred()
        {
            string errorLog = AppDriver.ReadCrowdLoadErrorLog();
            Assert.IsNull(errorLog,
                string.Format("crowd-load-error.log was created: {0}", errorLog));
        }

        protected void ThenCrowdTreeIsEmpty()
        {
            var names = Driver.GetTopLevelCrowdNames();
            Assert.AreEqual(0, names.Count,
                string.Format("Expected empty crowd tree but found: [{0}]",
                    string.Join(", ", names)));
        }

        protected void ThenFileContainsTopLevelCrowd(string filePath, string crowdName)
        {
            string json = File.ReadAllText(filePath);
            JArray crowds = JArray.Parse(json);
            Assert.IsNotNull(crowds, "Could not read file: " + filePath);
            bool found = crowds.Any(t => (string)t["Name"] == crowdName);
            Assert.IsTrue(found,
                string.Format("File '{0}' does not contain crowd '{1}'.",
                    Path.GetFileName(filePath), crowdName));
        }

        protected void ThenFileIsByteUnchanged(string filePath, byte[] originalBytes)
        {
            byte[] current = File.ReadAllBytes(filePath);
            CollectionAssert.AreEqual(originalBytes, current,
                string.Format("File '{0}' was unexpectedly modified.", Path.GetFileName(filePath)));
        }

        protected void ThenDailyBackupExists(string filePath)
        {
            string stem = Path.GetFileNameWithoutExtension(filePath);
            string dir = Path.GetDirectoryName(filePath);
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backup = Path.Combine(dir, stem + "." + today + ".bak");
            Assert.IsTrue(File.Exists(backup),
                string.Format("Expected daily backup '{0}' to exist.", Path.GetFileName(backup)));
        }

        protected void ThenActiveCrowdListContains(string filePath)
        {
            string json = File.ReadAllText(AppDriver.ActiveCrowdsJsonPath);
            JArray list = JArray.Parse(json);
            Assert.IsNotNull(list, "Could not read active-crowds.json");
            // Normalize separators so forward-slash paths (written by WriteActiveCrowdsJson)
            // match backslash paths (returned by Path.Combine on Windows).
            string normalizedExpected = filePath.Replace("/", "\\");
            bool found = list.Any(t =>
            {
                string entry = ((string)t ?? string.Empty).Replace("/", "\\");
                return string.Equals(entry, normalizedExpected, StringComparison.OrdinalIgnoreCase);
            });
            Assert.IsTrue(found,
                string.Format("Active crowd list does not contain '{0}'.",
                    Path.GetFileName(filePath)));
        }

        protected void ThenActiveCrowdListContainsAll(params string[] filePaths)
        {
            foreach (string p in filePaths)
                ThenActiveCrowdListContains(p);
        }

        // ---------------------------------------------------------------
        // Private JSON-building helpers
        // ---------------------------------------------------------------

        private JObject BuildCrowdObject(string name, ref int nextId)
        {
            string id = nextId.ToString();
            nextId++;
            return new JObject
            {
                { "$id", id },
                { "$type", CrowdModelType },
                { "Name", name },
                { "CrowdMemberCollection", new JArray() }
            };
        }

        private JObject BuildMemberObject(string name, ref int nextId)
        {
            string id = nextId.ToString();
            nextId++;
            return new JObject
            {
                { "$id", id },
                { "$type", CrowdMemberType },
                { "Name", name }
            };
        }
    }
}
