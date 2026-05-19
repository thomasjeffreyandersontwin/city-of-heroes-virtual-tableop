using Module.HeroVirtualTabletop.Characters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framework.WPF.Extensions;
using Module.Shared.Enumerations;
using Module.Shared;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Framework.WPF.Library;
using System.ComponentModel;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdMember : INotifyPropertyChanged
    {
        string Name { get; set; }
        string OldName { get; }
        ICrowd RosterCrowd { get; set; }

        void Place(IMemoryElementPosition position, bool completeEvent = true);
        void SavePosition();
        ICrowdMember Clone();
    }

    public interface ICrowdMemberModel : ICrowdMember
    {
        new ICrowdModel RosterCrowd { get; set; }
        bool IsExpanded { get; set; }
        bool IsMatched { get; set; }
        void ApplyFilter(string filter);
        void ResetFilter();
        int Order { get; set; }
    }

    public class CrowdMember : Character, ICrowdMember
    {
        private ICrowd rosterCrowd;
        [JsonIgnore]
        public ICrowd RosterCrowd
        {
            get
            {
                return rosterCrowd;
            }

            set
            {
                rosterCrowd = value;
                OnPropertyChanged("RosterCrowd");
            }
        }
        
        private IMemoryElementPosition savedPosition;
        public IMemoryElementPosition SavedPosition
        {
            get
            {
                return savedPosition;
            }
            set
            {
                savedPosition = value;
                OnPropertyChanged("SavedPosition");
            }
        }
        [JsonConstructor]
        public CrowdMember(): base() { }
        
        public CrowdMember(string name): base(name) { }

        public CrowdMember(string name, string surface, IdentityType identityType) : base(name, surface, identityType) { }

        public virtual ICrowdMember Clone()
        {
            CrowdMember crowdMember = this.DeepClone() as CrowdMember;
            return crowdMember;
        }

        public virtual void SavePosition()
        {
            if (this.Position != null)
                this.SavedPosition = this.Position.Clone(false);
        }

        public virtual void Place(IMemoryElementPosition position = null, bool completeEvent = true)
        {
            this.Target();
            if( new MemoryElement().Position == null || new MemoryElement().Position.Pointer != this.Position.Pointer
                ||this.HasBeenSpawned)
            {
                this.Spawn();
            }
            if (this.RosterCrowd != null)
            {
                RosterCrowd.Place(this, completeEvent);
            }
            else if (position != null)
            {
                Position = position.Clone(false, (Position as MemoryInstance).GetTargetPointer());
            }
            this.SuperImposeGhost();
        }

        protected override string GetLabel()
        {
            //if (gamePlayer != null && gamePlayer.IsReal)
            //{
            //    return gamePlayer.Label;
            //}
            //else
            {
                string crowdLabel = string.Empty;
                if (RosterCrowd != null && RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME && RosterCrowd.Name != Constants.NO_CROWD_CROWD_NAME)
                {
                    crowdLabel = " [" + RosterCrowd.Name + "]";
                }
                return Name.Trim() + crowdLabel;
            }
        }
        
        internal void CheckIfExistsInGame()
        {
            try
            {
                //bool retV = false;
                MemoryElement oldTargeted = new MemoryElement();
                Target();
                MemoryElement currentTargeted = new MemoryElement();
                if (currentTargeted.Label == Label)
                {
                    //retV = true;
                    SetAsSpawned();
                }
                oldTargeted.Target();
                //return retV;
            }
            catch (Exception)
            {
                
            }
        }

        public new string Spawn(bool completeEvent = true)
        {
            //CheckIfExistsInGame();
            return base.Spawn(completeEvent);
        }
    }
    public class CrowdMemberModel : CrowdMember, ICrowdMemberModel
    {
        private bool isExpanded;
        [JsonIgnore]
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        private bool isMatched = true;
        [JsonIgnore]
        public bool IsMatched
        {
            get
            {
                return isMatched;
            }
            set
            {
                isMatched = value;
                OnPropertyChanged("IsMatched");
            }
        }

        private int order;
        public int Order
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                OnPropertyChanged("Order");
            }
        }

        public void ApplyFilter(string filter)
        {
            if (alreadyFiltered == true && IsMatched == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                IsMatched = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                IsMatched = re.IsMatch(Name);
            }
            IsExpanded = IsMatched;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
        }

        public new ICrowdModel RosterCrowd
        {
            get
            {
                return base.RosterCrowd as ICrowdModel;
            }
            set
            {
                base.RosterCrowd = value;
                OnPropertyChanged("RosterCrowd");//To check if this line is actually needed
            }
        }

        public override ICrowdMember Clone()
        {
            CrowdMemberModel crowdMemberModel = new CrowdMemberModel()
            {
                Name = this.Name,
                RosterCrowd = null
            };
            crowdMemberModel.InitializeCharacter();
            
            foreach (AnimatedAbility ab in this.AnimatedAbilities)
            {
                AnimatedAbility clonedAbility = ab.Clone() as AnimatedAbility;
                clonedAbility.Owner = crowdMemberModel;
                if (clonedAbility.IsAttack)
                {
                    if ((clonedAbility as Attack).OnHitAnimation != null)
                        (clonedAbility as Attack).OnHitAnimation.Owner = crowdMemberModel;
                }
                crowdMemberModel.AnimatedAbilities.Add(clonedAbility);
            }

            foreach (Identity id in this.AvailableIdentities)
            {
                Identity clonedIdentity = id.Clone();
                if(id.AnimationOnLoad != null)
                {
                    AnimatedAbility animationOnLoad = crowdMemberModel.AnimatedAbilities.Where(aa => aa.Name == id.AnimationOnLoad.Name).FirstOrDefault();
                    clonedIdentity.AnimationOnLoad = animationOnLoad;
                }
                crowdMemberModel.AvailableIdentities.Add(clonedIdentity);
            }
            if(this.DefaultIdentity != null)
            {
                Identity defaultIdentity = crowdMemberModel.AvailableIdentities.Where(i => i.Name == this.DefaultIdentity.Name).FirstOrDefault();
                crowdMemberModel.DefaultIdentity = defaultIdentity;
            }
            if (this.ActiveIdentity != null)
            {
                Identity activeIdentity = crowdMemberModel.AvailableIdentities.Where(i => i.Name == this.ActiveIdentity.Name).FirstOrDefault();
                crowdMemberModel.ActiveIdentity = activeIdentity;
            }

            foreach(CharacterMovement characterMovement in this.Movements)
            {
                CharacterMovement clonedCharacterMovement = characterMovement.Clone();
                clonedCharacterMovement.Character = crowdMemberModel;
                crowdMemberModel.Movements.Add(clonedCharacterMovement);
            }

            if(this.DefaultMovement != null)
            {
                CharacterMovement defaultCharacterMovement = crowdMemberModel.Movements.FirstOrDefault(cm => cm.Name == this.DefaultMovement.Name);
                crowdMemberModel.DefaultMovement = defaultCharacterMovement;
            }

            // Custom option groups

            foreach(var customGroup in this.OptionGroups.Where(og => og.Type == HeroVirtualTabletop.OptionGroups.OptionType.Mixed))
            {
                OptionGroup<CharacterOption> optGroup = new OptionGroup<CharacterOption>(customGroup.Name);
                crowdMemberModel.AddOptionGroup(optGroup);
                foreach (var customOption in customGroup.Options)
                {
                    if(customOption is Identity)
                    {
                        Identity id = customOption as Identity;
                        Identity identityToRefer = crowdMemberModel.AvailableIdentities.FirstOrDefault(i => i.Name == id.Name);
                        if (identityToRefer != null)
                            optGroup.Add(identityToRefer);
                    }
                    else if(customOption is AnimatedAbility)
                    {
                        AnimatedAbility ab = customOption as AnimatedAbility;
                        AnimatedAbility abilityToRefer = crowdMemberModel.AnimatedAbilities.FirstOrDefault(aa => aa.Name == ab.Name);
                        if (abilityToRefer != null)
                            optGroup.Add(abilityToRefer);
                    }
                    else if(customOption is CharacterMovement)
                    {
                        CharacterMovement mv = customOption as CharacterMovement;
                        CharacterMovement characterMovementToRefer = crowdMemberModel.Movements.FirstOrDefault(m => m.Name == mv.Name);
                        if (characterMovementToRefer != null)
                            optGroup.Add(characterMovementToRefer);
                    }
                }
            }

            return crowdMemberModel;
        }
        public override void SavePosition()
        {
            base.SavePosition();
            if (this.RosterCrowd != null)
            {
                this.RosterCrowd.SavePosition(this);
            }
        }

        [JsonConstructor]
        public CrowdMemberModel() : base() { }
        public CrowdMemberModel(string name, int order = 0): base(name)
        {
            this.order = order;
        }
        public CrowdMemberModel(string name, string surface, IdentityType identityType, int order = 0) : base(name, surface, identityType)
        {
            this.order = order;
        }
    }
}
