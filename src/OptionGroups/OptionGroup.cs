using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Library.Utility;
using Framework.WPF.Library;
using Module.Shared.Messages;
using Newtonsoft.Json;

namespace HeroVTT.OptionGroups
{
    public enum OptionType
    {
        Identity,
        Ability,
        CharacterMovement,
        Mixed
    }

    public interface IOptionGroup
    {
        string Name { get; set; }
        IEnumerable Options { get; }
        string GetNewValidOptionName(string name = null);
        OptionType Type { get; }
        void UpdateIndices();
    }

    [JsonObject]
    public class OptionGroup<T> : HashedObservableCollection<T, string>, IOptionGroup where T : ICharacterOption
    {
        [JsonConstructor]
        private OptionGroup(): base(opt => { return opt.Name; })
        {
            switch (typeof(T).Name)
            {
                case "Identity":
                    Type = OptionType.Identity;
                    break;
                case "AnimatedAbility":
                    Type = OptionType.Ability;
                    break;
                case "CharacterMovement":
                    Type = OptionType.CharacterMovement;
                    break;
                case "CharacterOption":
                    Type = OptionType.Mixed;
                    break;
            }
        }

        public OptionGroup(string name) : this()
        {
            Name = name;
        }
        [JsonProperty(PropertyName = "Name", Order = 0)]
        private string name;
        [JsonIgnore]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Name"));
            }
        }
        
        [JsonProperty(Order = 1)]
        public IEnumerable Options
        {
            get
            {
                return Items;
            }
            set
            {
                this.Clear();
                foreach (T opt in value)
                {
                    this.Add(opt);
                }
            }
        }

        public OptionType Type
        {
            get;
            private set;
        }

        public string GetNewValidOptionName(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Option";
            }
            string suffix = string.Empty;
            int i = 0;
            while ((this.Cast<ICharacterOption>().Any((ICharacterOption opt) => { return opt.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }
        
        public void UpdateIndices()
        {
            this.indices.Clear();
            // O(n) single pass with counter — was O(n²) due to IndexOf(item) per item
            for (int i = 0; i < this.Items.Count; i++)
                indices[keySelector(this.Items[i])] = i;
        }
    }
}
