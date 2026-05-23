using System;

namespace HeroVTT.HCSIntegration
{
    public interface IHcsFileWatcher
    {
        string EventInfoDirectoryPath { get; }
        void StartWatching();
        void StopWatching();
        string ReadFileContents(string fileName);
        string ReadFileContents(string fileName, int retryCount);
        void WriteJsonToFile(string fileName, object jsonObject);
        void WriteJsonToFileAsync(string fileName, object jsonObject);
        bool FileExists(string fileName);
        void DeleteFile(string fileName);

        event EventHandler<HcsFileChangedEventArgs> CombatantsFileChanged;
        event EventHandler<HcsFileChangedEventArgs> ChronometerFileChanged;
        event EventHandler<HcsFileChangedEventArgs> ActiveCharacterFileChanged;
        event EventHandler<HcsFileChangedEventArgs> EligibleCombatantsFileChanged;
        event EventHandler<HcsFileChangedEventArgs> AttackResultFileChanged;
    }

    public class HcsFileChangedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
