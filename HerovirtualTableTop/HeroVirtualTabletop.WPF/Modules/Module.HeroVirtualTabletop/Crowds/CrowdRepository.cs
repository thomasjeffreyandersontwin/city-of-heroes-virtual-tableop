using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdRepository
    {
        string CrowdRepositoryPath
        {
            get;
        }
        string CrowdsFolderPath
        {
            get;
        }
        void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted);
        void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection);
        List<CrowdModel> LoadDefaultCrowdMembers();
    }

    /// <summary>
    /// Summary returned by <see cref="CrowdRepository.SaveDirtyCrowds"/>.
    /// </summary>
    public class SaveSummary
    {
        public SaveSummary()
        {
            FailedPaths = new List<string>();
            CrowdsNeedingNewFile = new List<CrowdModel>();
        }

        public int SavedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> FailedPaths { get; set; }
        /// <summary>Top-level crowds that have no source file and need a Save-As dialog.</summary>
        public List<CrowdModel> CrowdsNeedingNewFile { get; set; }
    }

    public class CrowdRepository : ICrowdRepository
    {
        // ── Active-Crowds mode ─────────────────────────────────────────────────

        private string _dataDirectory;

        public string DataDirectory
        {
            get { return _dataDirectory; }
            set
            {
                _dataDirectory = value;
                _inMemoryCrowds = new List<CrowdModel>();
            }
        }

        private List<CrowdModel> _inMemoryCrowds;

        private string ActiveCrowdListPath
        {
            get { return Path.Combine(_dataDirectory, "active-crowds.json"); }
        }

        private List<string> ReadActiveCrowdList()
        {
            if (!File.Exists(ActiveCrowdListPath))
                return new List<string>();
            try
            {
                return Helper.GetDeserializedJSONFromFile<List<string>>(ActiveCrowdListPath)
                       ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        private void WriteActiveCrowdList(List<string> paths)
        {
            Helper.SerializeObjectAsJSONToFile(ActiveCrowdListPath, paths);
        }

        /// <summary>Adds a crowd to the in-memory aggregate (active-crowds mode).</summary>
        public void AddCrowd(CrowdModel crowd)
        {
            if (_inMemoryCrowds == null) _inMemoryCrowds = new List<CrowdModel>();
            _inMemoryCrowds.Add(crowd);
        }

        // ── Load Active Crowd Files on Startup ─────────────────────────────────

        public void LoadActiveCrowdFiles(Action<IList<CrowdModel>> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<string> activePaths = ReadActiveCrowdList();
                var loaded = new List<CrowdModel>();
                var malformedPaths = new List<string>();

                foreach (string path in activePaths)
                {
                    if (!File.Exists(path))
                        continue; // missing file: skip, leave in list for GM action

                    List<CrowdModel> crowds = null;
                    try { crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(path); }
                    catch { crowds = null; }

                    if (crowds == null)
                    {
                        // Malformed file: remove from active list so it is not retried on next startup
                        malformedPaths.Add(path);
                        continue;
                    }

                    foreach (CrowdModel crowd in crowds)
                    {
                        crowd.SourceFilePath = path;
                        crowd.IsDirty = false;
                    }
                    loaded.AddRange(crowds);
                }

                // Persist the cleaned list only when malformed entries were actually removed
                if (malformedPaths.Count > 0)
                {
                    foreach (string bad in malformedPaths)
                        activePaths.Remove(bad);
                    WriteActiveCrowdList(activePaths);
                }

                _inMemoryCrowds = loaded;
                callback(loaded);
            });
        }

        // ── Browse and Activate ────────────────────────────────────────────────

        public void BrowseAndActivate(string[] selectedPaths, Action<IList<CrowdModel>> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<string> activeList = ReadActiveCrowdList();
                var newlyLoaded = new List<CrowdModel>();

                foreach (string path in selectedPaths)
                {
                    if (activeList.Contains(path, StringComparer.OrdinalIgnoreCase))
                    {
                        CrowdModel clone = CloneActiveCrowdFile(path, activeList);
                        if (clone != null)
                        {
                            newlyLoaded.Add(clone);
                            if (_inMemoryCrowds != null) _inMemoryCrowds.Add(clone);
                        }
                    }
                    else
                    {
                        List<CrowdModel> crowds = null;
                        try { crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(path); }
                        catch { crowds = null; }

                        if (crowds == null) continue;

                        foreach (CrowdModel crowd in crowds)
                        {
                            crowd.SourceFilePath = path;
                            crowd.IsDirty = false;
                        }
                        newlyLoaded.AddRange(crowds);
                        if (_inMemoryCrowds != null) _inMemoryCrowds.AddRange(crowds);
                        activeList.Add(path);
                    }
                }

                WriteActiveCrowdList(activeList);
                callback(newlyLoaded);
            });
        }

        private CrowdModel CloneActiveCrowdFile(string originalPath, List<string> activeList)
        {
            List<CrowdModel> original = null;
            try { original = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(originalPath); }
            catch { return null; }
            if (original == null || original.Count == 0) return null;

            string dir = Path.GetDirectoryName(originalPath);
            string stem = Path.GetFileNameWithoutExtension(originalPath);
            string ext = Path.GetExtension(originalPath);

            // Find lowest available integer suffix
            int n = 2;
            while (true)
            {
                string candidate = Path.Combine(dir, stem + " (" + n + ")" + ext);
                if (!File.Exists(candidate) && !activeList.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    break;
                n++;
            }

            string clonePath = Path.Combine(dir, stem + " (" + n + ")" + ext);
            var cloned = new List<CrowdModel>();
            foreach (CrowdModel c in original)
            {
                List<CrowdModel> reloaded = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(originalPath);
                CrowdModel copy = reloaded != null ? reloaded.FirstOrDefault(x => x.Name == c.Name) : null;
                if (copy == null) continue;
                copy.Name = copy.Name + " (" + n + ")"; // suffix only top-level name
                copy.SourceFilePath = clonePath;
                copy.IsDirty = false;
                cloned.Add(copy);
            }

            Helper.SerializeObjectAsJSONToFile(clonePath, cloned);
            activeList.Add(clonePath);
            return cloned.FirstOrDefault();
        }

        // ── Save Dirty Crowds ──────────────────────────────────────────────────

        public void SaveDirtyCrowds(Action<SaveSummary> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var summary = new SaveSummary();
                if (_inMemoryCrowds == null) { callback(summary); return; }

                foreach (CrowdModel crowd in _inMemoryCrowds.ToList())
                {
                    if (!IsDeepDirty(crowd))
                    {
                        summary.SkippedCount++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(crowd.SourceFilePath))
                    {
                        summary.CrowdsNeedingNewFile.Add(crowd);
                        continue;
                    }

                    try
                    {
                        WriteDailyBackup(crowd.SourceFilePath);
                        var payload = new List<CrowdModel> { crowd };
                        Helper.SerializeObjectAsJSONToFile(crowd.SourceFilePath, payload);
                        crowd.IsDirty = false;
                        ClearDeepDirty(crowd);
                        summary.SavedCount++;
                    }
                    catch
                    {
                        summary.FailedCount++;
                        summary.FailedPaths.Add(crowd.SourceFilePath);
                    }
                }

                callback(summary);
            });
        }

        private static bool IsDeepDirty(CrowdModel crowd)
        {
            if (crowd.IsDirty) return true;
            return crowd.CrowdMemberCollection.OfType<CrowdModel>().Any(IsDeepDirty);
        }

        private static void ClearDeepDirty(CrowdModel crowd)
        {
            crowd.IsDirty = false;
            foreach (CrowdModel nested in crowd.CrowdMemberCollection.OfType<CrowdModel>())
                ClearDeepDirty(nested);
        }

        private static void WriteDailyBackup(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string dir = Path.GetDirectoryName(filePath);
            string stem = Path.GetFileNameWithoutExtension(filePath);
            string today = DateTime.Today.ToString("yyyyMMdd");
            string backupPath = Path.Combine(dir, string.Format("{0}.{1}.bak", stem, today));
            if (!File.Exists(backupPath))
                File.Copy(filePath, backupPath);
        }

        // ── Save Crowd to New File ─────────────────────────────────────────────

        public void SaveCrowdToNewFile(CrowdModel crowd, string filePath, Action onSaved,
            Action onRejected = null)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // Reject nested crowds — only reference-equal top-level crowds are accepted
                bool isTopLevel = _inMemoryCrowds != null &&
                                  _inMemoryCrowds.Contains(crowd);
                if (!isTopLevel)
                {
                    if (onRejected != null) onRejected();
                    if (onSaved != null) onSaved();
                    return;
                }

                var payload = new List<CrowdModel> { crowd };
                Helper.SerializeObjectAsJSONToFile(filePath, payload);

                string oldSource = crowd.SourceFilePath;
                crowd.SourceFilePath = filePath;
                crowd.IsDirty = false;
                ClearDeepDirty(crowd);

                // Update active crowd list: remove old path (if any), add new path
                List<string> activeList = ReadActiveCrowdList();
                if (!string.IsNullOrEmpty(oldSource))
                    activeList.Remove(oldSource);
                if (!activeList.Contains(filePath, StringComparer.OrdinalIgnoreCase))
                    activeList.Add(filePath);
                WriteActiveCrowdList(activeList);

                if (onSaved != null) onSaved();
            });
        }

        // ── ICrowdRepository — not used in active-crowds mode ─────────────────

        public string CrowdRepositoryPath { get { return null; } set { } }
        public string CrowdsFolderPath { get { return null; } set { } }

        // ── Repository operations ─────────────────────────────────────────────

        public void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted)
        {
            LoadActiveCrowdFiles(loaded => GetCrowdCollectionCompleted(new List<CrowdModel>(loaded)));
        }

        public void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection)
        {
            SaveDirtyCrowds(_ => SaveCrowdCollectionCompleted());
        }

        public CrowdRepository()
        {
            // active-crowds.json lives one level above the CoH game folder, in <project-root>\data
            string projectRoot = Path.GetDirectoryName(Settings.Default.CityOfHeroesGameDirectory);
            DataDirectory = Path.Combine(projectRoot, Constants.GAME_DATA_FOLDERNAME);
        }

        public List<CrowdModel> LoadDefaultCrowdMembers()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<CrowdModel> crowdCollection = new List<CrowdModel>();
            string resName = "Module.HeroVirtualTabletop.Resources.DefaultCharactersWithAbilities.data";
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {

                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;

                    crowdCollection = serializer.Deserialize<List<CrowdModel>>(reader);
                }
            }

            return crowdCollection;
        }
    }
}

