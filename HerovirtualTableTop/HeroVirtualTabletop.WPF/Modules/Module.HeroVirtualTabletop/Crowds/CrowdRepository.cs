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
            get
            {
                if (string.IsNullOrEmpty(_dataDirectory))
                    return null;
                return Path.Combine(_dataDirectory, "active-crowds.json");
            }
        }

        private List<string> ReadActiveCrowdList()
        {
            string path = ActiveCrowdListPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new List<string>();
            try
            {
                return Helper.GetDeserializedJSONFromFile<List<string>>(path)
                       ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        private void WriteActiveCrowdList(List<string> paths)
        {
            string path = ActiveCrowdListPath;
            if (string.IsNullOrEmpty(path))
                return;
            Helper.SerializeObjectAsJSONToFile(path, paths);
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
                    Exception loadError = null;
                    try { crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(path); }
                    catch (Exception ex) { crowds = null; loadError = ex; }

                    if (crowds == null)
                    {
                        // Log to file for diagnostics
                        try
                        {
                            string logPath = System.IO.Path.Combine(_dataDirectory, "crowd-load-error.log");
                            string msg = string.Format("[{0}] FAILED: {1}\n  Error: {2}\n",
                                System.DateTime.Now, path,
                                loadError != null ? loadError.ToString() : "returned null");
                            System.IO.File.AppendAllText(logPath, msg);
                        }
                        catch { }
                        // Malformed file: remove from active list so it is not retried on next startup
                        malformedPaths.Add(path);
                        continue;
                    }

                    // Assign Order based on the position in the loaded list so that
                    // the top-level crowd sort in the ViewModel preserves the list order
                    // (active-crowds.json order) rather than re-sorting alphabetically.
                    // IsDirty = false must come AFTER Order is set: the Order setter fires
                    // PropertyChanged("Order") which CrowdModel_SelfPropertyChanged turns into
                    // IsDirty = true, so we must reset to false afterwards.
                    int startOrder = loaded.Count;
                    foreach (CrowdModel crowd in crowds)
                    {
                        crowd.SourceFilePath = path;
                        crowd.Order = startOrder++;
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

        // ── Legacy single-file mode (used when DataDirectory is not set) ──────────

        private string _crowdRepositoryPath;

        public string CrowdRepositoryPath
        {
            get { return _crowdRepositoryPath; }
            set { _crowdRepositoryPath = value; }
        }

        private string _crowdsFolderPath;
        public string CrowdsFolderPath
        {
            get { return _crowdsFolderPath; }
            set { _crowdsFolderPath = value; }
        }

        private bool UseFolderMode { get { return !string.IsNullOrEmpty(_crowdsFolderPath); } }

        private bool UseLegacyMode { get { return string.IsNullOrEmpty(_dataDirectory) && !string.IsNullOrEmpty(_crowdRepositoryPath) && !UseFolderMode; } }

        private List<CrowdModel> LoadFromFolder()
        {
            var result = new List<CrowdModel>();
            if (!Directory.Exists(_crowdsFolderPath))
                return result;
            foreach (string file in Directory.GetFiles(_crowdsFolderPath, "*.data"))
            {
                List<CrowdModel> crowds = null;
                try { crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(file); }
                catch { crowds = null; }
                if (crowds != null)
                    result.AddRange(crowds);
            }
            return result;
        }

        private void SaveToFolder(List<CrowdModel> crowds)
        {
            if (!Directory.Exists(_crowdsFolderPath))
                Directory.CreateDirectory(_crowdsFolderPath);

            var crowdNames = new HashSet<string>(crowds.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

            // Delete files for crowds that were removed from the collection
            foreach (string file in Directory.GetFiles(_crowdsFolderPath, "*.data"))
            {
                string nameFromFile = Path.GetFileNameWithoutExtension(file);
                if (!crowdNames.Contains(nameFromFile))
                    File.Delete(file);
            }

            // Write one file per top-level crowd
            foreach (CrowdModel crowd in crowds)
            {
                string filePath = Path.Combine(_crowdsFolderPath, crowd.Name + ".data");
                Helper.SerializeObjectAsJSONToFile(filePath, new List<CrowdModel> { crowd });
            }
        }

        private List<CrowdModel> LoadFromLegacyFile()
        {
            if (!File.Exists(_crowdRepositoryPath))
            {
                Helper.SerializeObjectAsJSONToFile(_crowdRepositoryPath, new List<CrowdModel>());
            }
            return Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(_crowdRepositoryPath) ?? new List<CrowdModel>();
        }

        private void SaveToLegacyFile(List<CrowdModel> crowds)
        {
            Helper.SerializeObjectAsJSONToFile(_crowdRepositoryPath, crowds);
        }

        // ── Repository operations ─────────────────────────────────────────────

        public void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted)
        {
            if (UseFolderMode)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var crowds = LoadFromFolder();
                    GetCrowdCollectionCompleted(crowds);
                });
                return;
            }
            if (UseLegacyMode)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var crowds = LoadFromLegacyFile();
                    GetCrowdCollectionCompleted(crowds);
                });
                return;
            }
            if (_inMemoryCrowds != null)
            {
                GetCrowdCollectionCompleted(new List<CrowdModel>(_inMemoryCrowds));
                return;
            }
            LoadActiveCrowdFiles(loaded => GetCrowdCollectionCompleted(new List<CrowdModel>(loaded)));
        }

        public void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection)
        {
            if (UseFolderMode)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    SaveToFolder(crowdCollection);
                    SaveCrowdCollectionCompleted();
                });
                return;
            }
            if (UseLegacyMode)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    SaveToLegacyFile(crowdCollection);
                    SaveCrowdCollectionCompleted();
                });
                return;
            }
            SaveDirtyCrowds(_ => SaveCrowdCollectionCompleted());
        }

        public CrowdRepository()
        {
            // active-crowds.json lives one level above the CoH game folder, in <project-root>\data
            string gameDir = Settings.Default.CityOfHeroesGameDirectory;
            if (!string.IsNullOrEmpty(gameDir))
            {
                string projectRoot = Path.GetDirectoryName(gameDir);
                if (!string.IsNullOrEmpty(projectRoot))
                    DataDirectory = Path.Combine(projectRoot, Constants.GAME_DATA_FOLDERNAME);
            }
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

