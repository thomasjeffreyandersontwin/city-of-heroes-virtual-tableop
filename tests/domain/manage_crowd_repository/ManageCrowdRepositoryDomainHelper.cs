using FluentAssertions;
using HeroVTT.DomainTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HeroVTT.DomainTests.ManageCrowdRepository
{
    public class ManageCrowdRepositoryDomainHelper
    {
        protected const int AwaitMs = 5000;
        protected const string ActiveListFile = "active-crowds.json";

        protected string _dataDir;
        protected CrowdRepository _repository;

        [TestInitialize]
        public void Init()
        {
            _dataDir = Path.Combine(Path.GetTempPath(), "coh-vtt-domain", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataDir);
            _repository = new CrowdRepository { DataDirectory = _dataDir };
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, true); } catch { }
        }

        // Given helpers

        protected void given_crowd_file_on_disk(string filename, Action<CrowdFileTestBuilder> build)
        {
            CrowdFileTestBuilder b = new CrowdFileTestBuilder();
            build(b);
            Helper.SerializeObjectAsJSONToFile(path_for(filename), b.Build());
        }

        protected void given_malformed_crowd_file_on_disk(string filename)
        {
            File.WriteAllText(path_for(filename), "{ NOT valid json ::: broken [[[");
        }

        protected void given_active_crowd_list_contains(params string[] filenames)
        {
            Helper.SerializeObjectAsJSONToFile(
                path_for(ActiveListFile),
                filenames.Select(path_for).ToList());
        }

        protected byte[] given_file_bytes(string filename)
        {
            return File.ReadAllBytes(path_for(filename));
        }

        // When helpers

        protected List<CrowdModel> when_workspace_opens()
        {
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                List<CrowdModel> result = null;
                _repository.LoadActiveCrowdFiles(loaded =>
                {
                    result = new List<CrowdModel>(loaded);
                    done.Set();
                });
                Assert.IsTrue(done.Wait(AwaitMs), "LoadActiveCrowdFiles timed out.");
                return result ?? new List<CrowdModel>();
            }
        }

        // Then helpers

        protected void then_crowd_in_result(List<CrowdModel> loaded, string crowdName)
        {
            loaded.Any(c => c.Name == crowdName).Should().BeTrue(
                string.Format("Crowd '{0}' must be present in the loaded result", crowdName));
        }

        protected void then_no_crowds_loaded(List<CrowdModel> loaded)
        {
            loaded.Should().BeEmpty("no crowds should load when the active crowd list is absent or empty");
        }

        protected void then_crowd_before(List<CrowdModel> loaded, string first, string second)
        {
            int a = loaded.FindIndex(c => c.Name == first);
            int b = loaded.FindIndex(c => c.Name == second);
            a.Should().BeLessThan(b, string.Format("'{0}' must precede '{1}' in the loaded list", first, second));
        }

        protected void then_nested_crowd_under(List<CrowdModel> loaded, string parent, string child)
        {
            CrowdModel parentCrowd = loaded.FirstOrDefault(c => c.Name == parent);
            parentCrowd.Should().NotBeNull(string.Format("Parent crowd '{0}' must be present", parent));
            parentCrowd.CrowdMemberCollection.Any(m => m.Name == child).Should().BeTrue(
                string.Format("Nested crowd '{0}' must be under '{1}'", child, parent));
        }

        protected void then_active_list_contains(string filename)
        {
            string listPath = path_for(ActiveListFile);
            File.Exists(listPath).Should().BeTrue("active-crowds.json must exist");
            List<string> list = Helper.GetDeserializedJSONFromFile<List<string>>(listPath)
                                 ?? new List<string>();
            list.Any(p => p.EndsWith(filename, StringComparison.OrdinalIgnoreCase)).Should().BeTrue(
                string.Format("active-crowds.json must contain '{0}'", filename));
        }

        protected void then_file_byte_unchanged(string filename, byte[] originalBytes)
        {
            byte[] current = File.ReadAllBytes(path_for(filename));
            current.Should().Equal(originalBytes,
                string.Format("'{0}' must not have been modified", filename));
        }

        protected void then_daily_backup_exists(string filename)
        {
            string stem = Path.GetFileNameWithoutExtension(filename);
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backup = path_for(string.Format("{0}.{1}.bak", stem, today));
            File.Exists(backup).Should().BeTrue(
                string.Format("Daily backup '{0}' must exist after save", Path.GetFileName(backup)));
        }

        protected void then_source_file_on_crowd(List<CrowdModel> loaded, string crowdName, string filename)
        {
            CrowdModel crowd = loaded.FirstOrDefault(c => c.Name == crowdName);
            crowd.Should().NotBeNull(string.Format("Crowd '{0}' must be in the result", crowdName));
            crowd.SourceFilePath.Should().Be(path_for(filename),
                string.Format("SourceFilePath must be set to '{0}' so save-dirty can write back", filename));
        }

        protected string path_for(string filename)
        {
            return Path.Combine(_dataDir, filename);
        }
    }
}
