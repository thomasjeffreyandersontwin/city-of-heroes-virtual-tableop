using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace HeroVTT.HCSIntegration
{
    public class HcsFileWatcher : IHcsFileWatcher
    {
        private static FileSystemWatcher _fileSystemWatcher;
        private readonly Timer _timer;
        private readonly object _lockObj = new object();

        private const int TIMER_INITIAL_DELAY_MS = 5;
        private const int TIMER_POLL_INTERVAL_MS = 2000;
        private const int FILE_READ_DELAY_MS = 500;
        private const int FILE_WRITE_DELAY_MS = 500;
        private const int FILE_DELETE_RETRY_DELAY_MS = 1000;
        private const int DEFAULT_RETRY_COUNT = 5;

        public event EventHandler<HcsFileChangedEventArgs> CombatantsFileChanged;
        public event EventHandler<HcsFileChangedEventArgs> ChronometerFileChanged;
        public event EventHandler<HcsFileChangedEventArgs> ActiveCharacterFileChanged;
        public event EventHandler<HcsFileChangedEventArgs> EligibleCombatantsFileChanged;
        public event EventHandler<HcsFileChangedEventArgs> AttackResultFileChanged;

        private string _eventInfoDirectoryPath;
        public string EventInfoDirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(_eventInfoDirectoryPath))
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string dir = Path.GetDirectoryName(path);
                    string eventInfoDir = Path.Combine(dir, "EventInfo");
                    if (Directory.Exists(eventInfoDir))
                        _eventInfoDirectoryPath = eventInfoDir;
                }
                return _eventInfoDirectoryPath;
            }
        }

        public HcsFileWatcher()
        {
            if (_fileSystemWatcher == null)
            {
                _fileSystemWatcher = new FileSystemWatcher();
                _fileSystemWatcher.Path = string.Format("{0}\\", EventInfoDirectoryPath);
                _fileSystemWatcher.IncludeSubdirectories = false;
                _fileSystemWatcher.Changed += OnFileChanged;
                _fileSystemWatcher.Created += OnFileChanged;
                _fileSystemWatcher.Renamed += OnFileChanged;
            }
            _fileSystemWatcher.EnableRaisingEvents = false;
            _timer = new Timer(OnTimerElapsed);
        }

        public void StartWatching()
        {
            if (EventInfoDirectoryPath != null)
                _fileSystemWatcher.EnableRaisingEvents = true;
            _timer.Change(TIMER_INITIAL_DELAY_MS, TIMER_POLL_INTERVAL_MS);
        }

        public void StopWatching()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public string ReadFileContents(string fileName)
        {
            return ReadFileContents(fileName, DEFAULT_RETRY_COUNT);
        }

        public string ReadFileContents(string fileName, int retryCount)
        {
            string filePath = Path.Combine(EventInfoDirectoryPath, fileName);
            string json = null;
            try
            {
                if (File.Exists(filePath))
                {
                    Thread.Sleep(FILE_READ_DELAY_MS);
                    using (StreamReader r = new StreamReader(filePath))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch
            {
                if (retryCount > 0)
                    json = ReadFileContents(fileName, retryCount - 1);
            }
            return json;
        }

        public void WriteJsonToFile(string fileName, object jsonObject)
        {
            string filePath = Path.Combine(EventInfoDirectoryPath, fileName);
            JsonSerializer serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            using (StreamWriter sw = new StreamWriter(filePath))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, jsonObject);
                sw.Flush();
            }
        }

        public void WriteJsonToFileAsync(string fileName, object jsonObject)
        {
            Action writeAction = () =>
            {
                WriteJsonToFile(fileName, jsonObject);
                WriteJsonToFile("AbilityActivatedFromDesktopRecent.info", jsonObject);
            };
            AsyncDelegateExecuter executer = new AsyncDelegateExecuter(writeAction, FILE_WRITE_DELAY_MS);
            executer.ExecuteAsyncDelegate();
        }

        public bool FileExists(string fileName)
        {
            string filePath = Path.Combine(EventInfoDirectoryPath, fileName);
            return File.Exists(filePath);
        }

        public void DeleteFile(string fileName)
        {
            DeleteFile(fileName, DEFAULT_RETRY_COUNT);
        }

        private void DeleteFile(string fileName, int retryCount)
        {
            string filePath = Path.Combine(EventInfoDirectoryPath, fileName);
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (IOException)
            {
                if (retryCount > 0)
                {
                    Thread.Sleep(FILE_DELETE_RETRY_DELAY_MS);
                    DeleteFile(fileName, retryCount - 1);
                }
            }
            catch { }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lockObj)
            {
                string ext = Path.GetExtension(e.FullPath);
                bool isRelevant = (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
                    && (ext == ".info" || ext == ".event");

                if (!isRelevant)
                    return;

                var args = new HcsFileChangedEventArgs { FileName = e.Name, FilePath = e.FullPath };

                if (e.Name == Constants.COMBATANTS_FILE_NAME || e.Name == Constants.CHRONOMETER_FILE_NAME)
                {
                    var handler = CombatantsFileChanged;
                    if (handler != null) handler(this, args);
                }
                else if (e.Name == Constants.ACTIVE_CHARACTER_FILE_NAME)
                {
                    var handler = ActiveCharacterFileChanged;
                    if (handler != null) handler(this, args);
                }
                else if (e.Name == Constants.ELIGIBLE_COMBATANTS_FILE_NAME)
                {
                    var handler = EligibleCombatantsFileChanged;
                    if (handler != null) handler(this, args);
                }
            }
        }

        private void OnTimerElapsed(object state)
        {
            if (FileExists(Constants.ATTACK_RESULT_FILE_NAME))
            {
                var handler = AttackResultFileChanged;
                if (handler != null) handler(this, new HcsFileChangedEventArgs { FileName = Constants.ATTACK_RESULT_FILE_NAME });
            }
        }
    }
}
