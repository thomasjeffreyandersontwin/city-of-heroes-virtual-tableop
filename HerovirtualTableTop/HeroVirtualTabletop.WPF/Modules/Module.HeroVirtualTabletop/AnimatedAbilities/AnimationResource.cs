using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimationResource : NotifyPropertyChanged
    {
        [JsonConstructor]
        private AnimationResource()
        {
            this.tags = new ObservableCollection<string>();
            Tags = new ReadOnlyObservableCollection<string>(this.tags);
        }

        public AnimationResource(string value) : this()
        {
            this.Value = value;
        }

        public AnimationResource(AnimatedAbility reference) : this()
        {
            this.Reference = reference;
        }

        public AnimationResource(Identity identity) : this()
        {
            this.Identity = identity;
        }

        public AnimationResource(string value, string name, params string[] tags) : this(value)
        {
            this.Name = name;
            //this.Value = System.IO.Path.GetFileNameWithoutExtension(value);
            this.tags.AddRange(tags);
        }

        public AnimationResource(AnimatedAbility reference, string name, params string[] tags) : this(reference)
        {
            this.Name = name;
            this.tags.AddRange(tags);
        }

        public AnimationResource(Identity identity, string name, params string[] tags) : this(identity)
        {
            this.Name = name;
            this.tags.AddRange(tags);
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private string value;
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                OnPropertyChanged("Value");
            }
        }

        private AnimatedAbility reference;
        public AnimatedAbility Reference
        {
            get
            {
                return reference;
            }
            set
            {
                reference = value;
                OnPropertyChanged("Reference");
            }
        }

        private Identity identity;
        public Identity Identity
        {
            get
            {
                return identity;
            }
            set
            {
                identity = value;
                OnPropertyChanged("Identity");
            }
        }

        [JsonProperty(PropertyName = "Tags")]
        private ObservableCollection<string> tags;
        [JsonIgnore]
        public ReadOnlyObservableCollection<string> Tags { get; private set; }

        [JsonIgnore]
        public string TagLine
        {
            get
            {
                return tags != null ? string.Join(", ", tags) : "";
            }
            set
            {
                tags = new ObservableCollection<string>(value.Split(',').Select((s) => { return s.Trim(); }));
                OnPropertyChanged("TagLine");
            }
        }

        #region String behaviour

        public static implicit operator string(AnimationResource s)
        {
            return s == null ? string.Empty : (s.Value != null ? s.Value.ToString() : 
                (s.Reference != null && s.Reference is AnimatedAbility ? s.Reference.Name : s.Identity != null ? s.Identity.Name : string.Empty));
        }

        public static implicit operator AnimatedAbility(AnimationResource s)
        {
            return s == null ? null : (s.Reference != null ? s.Reference : null);
        }

        public static implicit operator AnimationResource(string s)
        {
            return new AnimationResource(s);
        }

        public static implicit operator AnimationResource(AnimatedAbility s)
        {
            return new AnimationResource(s);
        }

        public static implicit operator AnimationResource(Identity i)
        {
            return new AnimationResource(i);
        }

        public static implicit operator Identity(AnimationResource s)
        {
            return s == null ? null : (s.Identity != null ? s.Identity : null);
        }

        #endregion

        #region Equality Comparer and Operator Overloading
        public static bool operator ==(AnimationResource a, AnimationResource b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.Value == b.Value && (a.Reference == b.Reference || a.Identity == b.Identity) && a.Name == b.Name;
        }

        public static bool operator !=(AnimationResource a, AnimationResource b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            bool areEqual = false;
            if (obj is AnimationResource)
                areEqual = this == (AnimationResource)obj;
            return areEqual;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    public class AnimationResourceComparer : IComparer<AnimationResource>
    {
        public int Compare(AnimationResource ar1, AnimationResource ar2)
        {
            string s1 = ar1.TagLine;
            string s2 = ar2.TagLine;
            if (ar1.TagLine == ar2.TagLine)
            {
                s1 = ar1.Name;
                s2 = ar2.Name;
            }

            return Helper.CompareStrings(s1, s2);
        }
    }
}
