using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Synchronous in-memory ICrowdRepository for Tier 2 ViewModel tests.
    /// GetCrowdCollection calls its callback synchronously so the test stays on one thread.
    /// SaveCrowdCollection captures the passed list without calling the callback, preventing
    /// SaveCrowdCollectionCallback from touching Application.Current.Dispatcher.
    /// </summary>
    public class FakeCrowdRepository : ICrowdRepository
    {
        private List<CrowdModel> _crowds = new List<CrowdModel>();

        public List<CrowdModel> LastSavedCrowds { get; private set; }
        public int SaveCallCount { get; private set; }

        public string CrowdRepositoryPath { get { return string.Empty; } }
        public string CrowdsFolderPath { get { return string.Empty; } }

        public void SetCrowds(List<CrowdModel> crowds)
        {
            _crowds = crowds ?? new List<CrowdModel>();
        }

        public void GetCrowdCollection(Action<List<CrowdModel>> callback)
        {
            callback(new List<CrowdModel>(_crowds));
        }

        public void SaveCrowdCollection(Action callback, List<CrowdModel> crowdCollection)
        {
            LastSavedCrowds = new List<CrowdModel>(crowdCollection);
            SaveCallCount++;
            // Intentionally do NOT invoke callback to avoid Application.Current.Dispatcher
            // in SaveCrowdCollectionCallback.  Tier 2 tests verify LastSavedCrowds instead.
        }

        public List<CrowdModel> LoadDefaultCrowdMembers()
        {
            return new List<CrowdModel>();
        }
    }
}
