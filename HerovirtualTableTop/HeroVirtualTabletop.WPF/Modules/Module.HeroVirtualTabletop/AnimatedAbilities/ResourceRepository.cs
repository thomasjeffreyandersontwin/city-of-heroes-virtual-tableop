using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public interface IResourceRepository
    {
        List<AnimationResource> GetMoveResources();
        void SaveMoveResources(List<AnimationResource> moveResources);
        List<AnimationResource> GetFXResources();
        void SaveFXResources(List<AnimationResource> fxResources);
        List<AnimationResource> GetSoundResources();
        void SaveSoundResources(List<AnimationResource> soundResources);
    }
    public class ResourceRepository : IResourceRepository
    {
        private string moveRepositoryPath;
        private string fxRepositoryPath;
        private string soundRepositoryPath;

        public ResourceRepository()
        {
            this.moveRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MOVE_REPOSITORY_FILENAME);
            this.fxRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_FX_REPOSITORY_FILENAME);
            this.soundRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_SOUND_REPOSITORY_FILENAME);
        }

        public List<AnimationResource> GetMoveResources()
        {
            List<AnimationResource> resouceCollection = Helper.GetDeserializedJSONFromFile<List<AnimationResource>>(moveRepositoryPath);
            if (resouceCollection == null || resouceCollection.Count == 0)
            {
                resouceCollection = new List<AnimationResource>();
                Assembly assembly = Assembly.GetExecutingAssembly();

                string resName = "Module.HeroVirtualTabletop.Resources.MOVElements.csv";
                using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
                {
                    while (!Sr.EndOfStream)
                    {
                        string resLine = Sr.ReadLine();
                        string[] resArray = resLine.Split(';');
                        resouceCollection.Add(new AnimationResource(resArray[1], Path.GetFileNameWithoutExtension(resArray[1]), tags: resArray[0]));
                    }
                }
                SaveMoveResources(resouceCollection);
            }

            return resouceCollection;
        }
        public void SaveMoveResources(List<AnimationResource> moveResources)
        {
            Helper.SerializeObjectAsJSONToFile(moveRepositoryPath, moveResources);
        }

        public List<AnimationResource> GetFXResources()
        {
            List<AnimationResource> resouceCollection = Helper.GetDeserializedJSONFromFile<List<AnimationResource>>(fxRepositoryPath);
            if (resouceCollection == null || resouceCollection.Count == 0)
            {
                resouceCollection = new List<AnimationResource>();

                Assembly assembly = Assembly.GetExecutingAssembly();
                string resName = "Module.HeroVirtualTabletop.Resources.FXElements.csv";
                using (StreamReader Sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
                {
                    while (!Sr.EndOfStream)
                    {
                        string resLine = Sr.ReadLine();
                        string[] resArray = resLine.Split(';');
                        resouceCollection.Add(new AnimationResource(resArray[2], resArray[1], tags: resArray[0]));
                    }
                }
                SaveFXResources(resouceCollection);
            }

            return resouceCollection;
        }
        public void SaveFXResources(List<AnimationResource> fxResources)
        {
            Helper.SerializeObjectAsJSONToFile(fxRepositoryPath, fxResources);
        }

        public List<AnimationResource> GetSoundResources()
        {
            List<AnimationResource> resouceCollection = Helper.GetDeserializedJSONFromFile<List<AnimationResource>>(soundRepositoryPath);
            if (resouceCollection == null || resouceCollection.Count == 0)
            {
                resouceCollection = LoadSoundResources();
            }
            else
            {
                //// Check for addition/deletion of sound files - Feature turned off due to performance issue
                //List<AnimationResource> currentResourceCollection = LoadSoundResources();
                //var addedResources = currentResourceCollection.Where(ar => resouceCollection.Where(rc => rc.Value == ar.Value).FirstOrDefault() == null);
                //if (addedResources.Count() > 0)
                //{
                //    foreach (var addedResource in addedResources)
                //    {
                //        resouceCollection.Add(addedResource);
                //    }
                //}
                //var deletedResources = new List<AnimationResource>(resouceCollection.Where(rc => currentResourceCollection.Where(ar => ar.Value == rc.Value).FirstOrDefault() == null));
                //if (deletedResources.Count() > 0)
                //{
                //    foreach (var deletedResource in deletedResources)
                //    {
                //        var resourceToDelete = resouceCollection.First(ar => ar.Value == deletedResource.Value);
                //        resouceCollection.Remove(resourceToDelete);
                //    }
                //}
            }
            SaveSoundResources(resouceCollection);
            return resouceCollection;
        }

        public List<AnimationResource> LoadSoundResources()
        {
            List<AnimationResource> resouceCollection = new List<AnimationResource>();
            string soundDir = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_SOUND_FOLDERNAME);
            if (!Directory.Exists(soundDir))
            {
                Directory.CreateDirectory(soundDir);
            }
            var soundFiles = Directory.EnumerateFiles
                        (soundDir,
                        "*.ogg", SearchOption.AllDirectories);//.OrderBy(x => { return Path.GetFileNameWithoutExtension(x); });

            foreach (string file in soundFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string resourceVal = file.Substring(soundDir.Length);
                string[] tmpTags = file.Substring(soundDir.Length).Split('\\').Where((s) =>
                {
                    return !string.IsNullOrWhiteSpace(s);
                }).ToArray();
                string[] tags = new string[1];

                string sound = tmpTags[tmpTags.Length - 1];

                string tag = tmpTags.Length >= 2 ? tmpTags[tmpTags.Length - 2] : "Sound";
                tag = tag[0].ToString().ToUpper() + tag.Substring(1);

                Regex re = new Regex(@"_{1}");
                if (!re.IsMatch(sound, 1))
                    re = new Regex(@"[A-Z,0-9,\-]{1}");
                string tmp;
                if (re.IsMatch(sound, 1))
                {
                    tmp = sound.Substring(0, re.Match(sound, 1).Index);
                    tmp = tmp[0].ToString().ToUpper() + tmp.Substring(1);
                    tag += tmp;
                }

                tags[0] = tag;
                resouceCollection.Add(new AnimationResource(resourceVal, name, tags));
            }
            return resouceCollection;
            
        }

        public void SaveSoundResources(List<AnimationResource> soundResources)
        {
            Helper.SerializeObjectAsJSONToFile(soundRepositoryPath, soundResources);
        }
    }
}
