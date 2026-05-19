using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public class CrowdRepository : ICrowdRepository
    {
        List<Mutex> mutexes;
        List<AutoResetEvent> events;

        private string crowdRepositoryPath;
        public string CrowdRepositoryPath
        {
            get { return crowdRepositoryPath; }
            set { crowdRepositoryPath = value; }
        }

        private string crowdsFolderPath;
        public string CrowdsFolderPath
        {
            get { return crowdsFolderPath; }
            set { crowdsFolderPath = value; }
        }

        private bool IsFolderMode
        {
            get { return !string.IsNullOrEmpty(crowdsFolderPath); }
        }

        private string CachePath { get { return crowdRepositoryPath + ".cache"; } }

        private bool IsCacheFresh()
        {
            try
            {
                return File.Exists(CachePath) &&
                       File.GetLastWriteTimeUtc(CachePath) >= File.GetLastWriteTimeUtc(crowdRepositoryPath);
            }
            catch { return false; }
        }

        // ── Folder-mode helpers ───────────────────────────────────────────────

        private List<CrowdModel> LoadAllCrowdsFromFolder()
        {
            var all = new List<CrowdModel>();
            if (!Directory.Exists(crowdsFolderPath))
                return all;
            foreach (string file in Directory.GetFiles(crowdsFolderPath, "*.data"))
            {
                var crowds = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(file);
                if (crowds != null)
                    all.AddRange(crowds);
            }
            return all;
        }

        private void SaveAllCrowdsToFolder(List<CrowdModel> crowdCollection)
        {
            if (!Directory.Exists(crowdsFolderPath))
                Directory.CreateDirectory(crowdsFolderPath);

            var crowdNames = new HashSet<string>(crowdCollection.Select(c => c.Name));

            foreach (var crowd in crowdCollection)
            {
                string filePath = Path.Combine(crowdsFolderPath, crowd.Name + ".data");
                Helper.SerializeObjectAsJSONToFile(filePath, new List<CrowdModel> { crowd });
            }

            foreach (string file in Directory.GetFiles(crowdsFolderPath, "*.data"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!crowdNames.Contains(name))
                    File.Delete(file);
            }
        }

        // ── Repository operations ─────────────────────────────────────────────

        private Action<List<CrowdModel>> getCrowdCollectionCompleted;
        public void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted)
        {
            this.getCrowdCollectionCompleted = GetCrowdCollectionCompleted;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<CrowdModel> crowdCollection;

                if (IsFolderMode)
                {
                    crowdCollection = LoadAllCrowdsFromFolder();
                }
                else
                {
                    crowdCollection = null;

                    if (IsCacheFresh())
                    {
                        try { crowdCollection = Helper.GetDeserializedJSONFromCacheFile<List<CrowdModel>>(CachePath); }
                        catch { crowdCollection = null; }
                    }

                    if (crowdCollection == null)
                    {
                        crowdCollection = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(crowdRepositoryPath);
                        if (crowdCollection == null)
                            crowdCollection = new List<CrowdModel>();
                        Helper.SerializeObjectAsCacheFile(CachePath, crowdCollection);
                    }

                    ThreadPool.QueueUserWorkItem(__ => TakeBackup());
                }

                this.getCrowdCollectionCompleted(crowdCollection);
            });
        }

        private object lockObj = new object();

        private Action saveCrowdCollectionCompleted;
        public void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection)
        {
            this.saveCrowdCollectionCompleted = SaveCrowdCollectionCompleted;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (lockObj)
                {
                    if (IsFolderMode)
                    {
                        SaveAllCrowdsToFolder(crowdCollection);
                    }
                    else
                    {
                        Helper.SerializeObjectAsJSONToFile(crowdRepositoryPath, crowdCollection);
                        Helper.SerializeObjectAsCacheFile(CachePath, crowdCollection);
                    }
                    this.saveCrowdCollectionCompleted();
                }
            });
        }

        public void WaitCompletion()
        {
            WaitHandle.WaitAll(events.ToArray());
        }

        public CrowdRepository()
        {
            crowdRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_CROWD_REPOSITORY_FILENAME);
            crowdsFolderPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_CROWDS_FOLDERNAME);
            mutexes = new List<Mutex>();
            events = new List<AutoResetEvent>();
        }

        private void TakeBackup()
        {
            string backupDir = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_DATA_BACKUP_FOLDERNAME);
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);
            string backupFilePath = Path.Combine(backupDir, "CrowdRepository_Backup" + String.Format("{0:MMddyyyy}", DateTime.Today) + ".data");
            if(!File.Exists(backupFilePath))
            {
                File.Copy(crowdRepositoryPath, backupFilePath, true);
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

