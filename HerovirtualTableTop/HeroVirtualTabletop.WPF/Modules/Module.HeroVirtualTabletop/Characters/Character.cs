using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared.Enumerations;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Framework.WPF.Extensions;
using System.Runtime.Serialization;
using Module.HeroVirtualTabletop.Movements;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Utility;
using System.Threading;
using Module.HeroVirtualTabletop.HCSIntegration;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
    {
        private KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
        protected internal Camera camera = new Camera();
        private string keybind;
        private string[] keybinds;
        protected internal IMemoryElement gamePlayer;

        [JsonConstructor()]
        public Character()
        {
            InitializeCharacter();
        }

        protected void InitializeCharacter()
        {
            //availableIdentities = new OptionGroup<Identity>();
            //animatedAbilities = new OptionGroup<AnimatedAbility>();
            optionGroups = new HashedObservableCollection<IOptionGroup, string>(x => x.Name);
            OptionGroups = new ReadOnlyHashedObservableCollection<IOptionGroup, string>(optionGroups);
            this.AttackConfigurationMap = new Dictionary<Guid, Tuple<Attack, AttackConfiguration>>();
            this.OptionGroupExpansionStates = new Dictionary<string, bool>();
        }

        public Character(string name): this()
        {
            Name = name;
            IOptionGroup tmp = AvailableIdentities;
            tmp = AnimatedAbilities;
            tmp = Movements;
            tmp = null;
            SetActiveIdentity();
        }

        public Character(string name, string surface, IdentityType identityType): this(name)
        {
            SetActiveIdentity(surface, identityType);
        }

        [OnDeserialized]
        private void AfterDeserialized(StreamingContext stream)
        {
            foreach (IOptionGroup opt in this.OptionGroups)
            {
                opt.UpdateIndices();
            }
            IOptionGroup x = AvailableIdentities;
            x = AnimatedAbilities;
            x = Movements;
            x = null;
        }

        /// <summary>
        /// Create an Identity based on the parameters or the character name and set it as active.
        /// </summary>
        /// <param name="surface">The surface to be used. If null the method will look up for an existing costume with the same name of the character.</param>
        /// <param name="identityType">Can be either Model or Costume. In the second case the surface parameter will be validated checking if the costume exists.</param>
        private void SetActiveIdentity(string surface = null, IdentityType identityType = IdentityType.Model)
        {
            if (surface == null) //No surface passed
            {
                //We look for a costume with the same name of the character
                if (File.Exists(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME, name + Constants.GAME_COSTUMES_EXT)))
                {
                    if (!AvailableIdentities.ContainsKey(name))
                    {
                        AvailableIdentities.Add(new Identity(name, IdentityType.Costume, name));
                    }
                    //Costume exists, use it
                    this.ActiveIdentity = AvailableIdentities[name];
                    this.DefaultIdentity = AvailableIdentities[name];
                    if (AvailableIdentities.ContainsKey("Base"))
                    {
                        AvailableIdentities.Remove("Base");
                    }
                }
            }
            else if (identityType == IdentityType.Costume) //A surface has been passed and it should be a Costume
            {
                //Validate the surface by checking if the costume exists
                if (File.Exists(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME, surface + Constants.GAME_COSTUMES_EXT)))
                {
                    if (!AvailableIdentities.ContainsKey(surface))
                    {
                        AvailableIdentities.Add(new Identity(surface, identityType, surface));
                    }
                    //If valid, use it
                    this.ActiveIdentity = AvailableIdentities[surface];
                }
            }
            else //A surface has been passed and it should be a model
            {
                //To do: Validate the model??
                //Use the surface as model
                if (!AvailableIdentities.ContainsKey(surface))
                {
                    AvailableIdentities.Add(new Identity(surface, identityType, surface));
                }
                //If valid, use it
                this.ActiveIdentity = AvailableIdentities[surface];
            }
        }

        [JsonProperty(PropertyName = "Name")]
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

                OldName = name;
                name = value;
                SetActiveIdentity();
                OnPropertyChanged("Name");
                
            }
        }

        private Dictionary<Guid, Tuple<Attack, AttackConfiguration>> attackConfigMap;
        [JsonIgnore]
        public Dictionary<Guid, Tuple<Attack, AttackConfiguration>> AttackConfigurationMap
        {
            get
            {
                return attackConfigMap;
            }
            set
            {
                attackConfigMap = value;
                OnPropertyChanged("AttackConfigurationMap");
            }
        }

        private float currentDistanceCount;
        [JsonIgnore]
        public float CurrentDistanceCount
        {
            get
            {
                return currentDistanceCount;
            }
            set
            {
                currentDistanceCount = value;
                OnPropertyChanged("CurrentDistanceCount");
            }
        }
        private float currentDistanceLimit;
        [JsonIgnore]
        public float CurrentDistanceLimit
        {
            get
            {
                return currentDistanceLimit;
            }
            set
            {
                currentDistanceLimit = value;
                OnPropertyChanged("CurrentDistanceLimit");
            }
        }
        private float maxDistanceLimit = float.MinValue;
        [JsonIgnore]
        public float MaxDistanceLimit
        {
            get
            {
                if(maxDistanceLimit == float.MinValue)
                {
                    var maxMovementDistance = this.Movements.Max(m => m.DistanceLimit);
                    if (maxMovementDistance != float.MinValue)
                        maxDistanceLimit = maxMovementDistance + ReachLimit;
                    if(maxDistanceLimit <= 0f)
                        maxDistanceLimit = 6f; // default
                }
                return maxDistanceLimit;
            }
        }
        [JsonIgnore]
        public float ReachLimit { get; set; }

        [JsonIgnore]
        public string OldName { get; private set; }
        [JsonIgnore]
        public Vector3 CurrentStartingPositionVectorForDistanceCounting { get; set; }
        
        private IMemoryElementPosition position;
        [JsonIgnore]
        public IMemoryElementPosition Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
            }
        }

        private bool hasBeenSpawned;
        [JsonIgnore]
        public bool HasBeenSpawned
        {
            get
            {
                return hasBeenSpawned;
            }
        }
        [JsonIgnore]
        public string Label
        {
            get
            {
                return GetLabel();
            }
        }
        [JsonIgnore]
        public Microsoft.Xna.Framework.Matrix CurrentModelMatrix
        {
            get
            {
                return (Position as Position).GetModelMatrix();
            }
            set
            {
                (Position as Position).SetModelMatrix(value);
            }
        }

        [JsonIgnore]
        public Vector3 CurrentPositionVector
        {
            get
            {
                return Helper.GetRoundedVector((Position as Position).GetPositionVector(), 2);
            }
            set
            {
                (Position as Position).SetPosition(value);
                this.UpdateDistanceCount();
            }
        }

        [JsonIgnore]
        public Vector3 CurrentFacingVector
        {
            get
            {
                return Helper.GetRoundedVector((Position as Position).GetFacingVector(), 2);
            }
            set
            {
                (Position as Position).SetTargetFacing(value);
            }
        }

        [JsonIgnore]
        public bool IsFollowed
        {
            get;
            set;
        }
        [JsonIgnore]
        public bool IsMoving
        {
            get;
            set;
        }
        [JsonIgnore]
        public Dictionary<string, bool> OptionGroupExpansionStates
        {
            get;
            set;
        }

        protected virtual string GetLabel()
        {
            return name;
        }

        [JsonProperty(PropertyName = "OptionGroups", Order = 0)]
        private HashedObservableCollection<IOptionGroup, string> optionGroups;
        [JsonIgnore]
        public ReadOnlyHashedObservableCollection<IOptionGroup, string> OptionGroups { get; private set; }


        public void RemoveOptionGroup(IOptionGroup optGroup)
        {
            optionGroups.Remove(optGroup);
            OnPropertyChanged("OptionGroups");
        }

        public void RemoveOptionGroupAt(int index)
        {
            optionGroups.RemoveAt(index);
            OnPropertyChanged("OptionGroups");
        }

        public void AddOptionGroup(IOptionGroup optGroup)
        {
            optionGroups.Add(optGroup);
            OnPropertyChanged("OptionGroups");
        }

        public void InsertOptionGroup(int index, IOptionGroup optGroup)
        {
            optionGroups.Insert(index, optGroup);
            OnPropertyChanged("OptionGroups");
        }
        
        //private OptionGroup<Identity> availableIdentities;
        //[JsonProperty(Order = 0)]
        [JsonIgnore]
        public OptionGroup<Identity> AvailableIdentities
        {
            get
            {
                OptionGroup<Identity> availableIdentities = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.IDENTITY_OPTION_GROUP_NAME; }) as OptionGroup<Identity>;
                if (availableIdentities == null)
                {
                    availableIdentities = new OptionGroup<Identity>(Constants.IDENTITY_OPTION_GROUP_NAME);
                    optionGroups.Add(availableIdentities);
                }
                return availableIdentities;
            }
            //set
            //{
            //    availableIdentities = value;
            //    OnPropertyChanged("AvailableIdentities");
            //}
        }

        [JsonProperty(PropertyName = "defaultIdentity")]
        private Identity defaultIdentity;
        [JsonProperty(Order = 1)]
        public Identity DefaultIdentity
        {
            get
            {
                if (defaultIdentity == null || !AvailableIdentities.Contains(defaultIdentity))
                {
                    if (AvailableIdentities.Count > 0)
                    {
                        defaultIdentity = AvailableIdentities[0];
                    }
                    else
                    {
                        defaultIdentity = new Identity("Model_Statesman", IdentityType.Model, "Base");
                        AvailableIdentities.Add(defaultIdentity);
                    }
                }
                return defaultIdentity;
            }
            set
            {
                AvailableIdentities.UpdateIndices();
                if (value != null && !AvailableIdentities.ContainsKey(value.Name))
                {
                    AvailableIdentities.Add(value);
                }
                if (value != null)
                    defaultIdentity = AvailableIdentities[value.Name];
                else
                    defaultIdentity = null;
                OnPropertyChanged("DefaultIdentity");
            }
        }

        private CharacterMovement defaultMovement;
        [JsonProperty(Order = 3)]
        public CharacterMovement DefaultMovement
        {
            get
            {
                return defaultMovement;
            }
            set
            {
                defaultMovement = value;
                OnPropertyChanged("DefaultMovement");
            }
        }

        private MovementInstruction movementInstruction;
        [JsonIgnore]
        public MovementInstruction MovementInstruction
        {
            get
            {
                return movementInstruction;
            }
            set
            {
                movementInstruction = value;
            }
        }

        private Identity activeIdentity;
        [JsonProperty(Order = 2)]
        public Identity ActiveIdentity
        {
            get
            {
                if (activeIdentity == null || !AvailableIdentities.ContainsKey(activeIdentity.Name))
                {
                    activeIdentity = DefaultIdentity;
                }
                
                return activeIdentity;
            }

            set
            {
                //Deactivate all active persistent abilities on Identity Change
                AnimatedAbilities.Where((ab) => { return ab.IsActive; }).ToList().ForEach((ab) => { ab.Stop(); });
                //Deactive any effect activated as a result of former Identity loading
                if (activeIdentity != null && activeIdentity.AnimationOnLoad != null)
                    activeIdentity.AnimationOnLoad.Stop();
                if (activeIdentity != null && activeIdentity.Type == IdentityType.Model)
                    this.RemoveGhost();
                AvailableIdentities.UpdateIndices();
                if (value != null && !AvailableIdentities.ContainsKey(value.Name))
                {   
                    AvailableIdentities.Add(value);
                }
                if (value != null)
                {
                    activeIdentity = AvailableIdentities[value.Name];
                    if (HasBeenSpawned)
                    {
                        Target();
                        activeIdentity.Render(Target: this);
                    }
                        
                }
                else
                {
                    activeIdentity = null;
                }
                OnPropertyChanged("ActiveIdentity");
            }
        }

        private CharacterMovement activeMovement;
        [JsonIgnore]
        public CharacterMovement ActiveMovement
        {
            get
            {
                return activeMovement;
            }
            set
            {
                activeMovement = value;
                OnPropertyChanged("ActiveMovement");
            }
        }

        private AnimatedAbility defaultAbility;
        [JsonProperty(Order = 4)]
        public AnimatedAbility DefaultAbility
        {
            get
            {
                return defaultAbility;
            }
            set
            {
                defaultAbility = value;
                OnPropertyChanged("DefaultAbility");
            }
        }

        private AnimatedAbility activeAbility;
        [JsonIgnore]
        public AnimatedAbility ActiveAbility
        {
            get
            {
                return activeAbility;
            }
            set
            {
                activeAbility = value;
                OnPropertyChanged("ActiveAbility");
            }
        }
        [JsonIgnore]
        public AnimatedAbility DefaultAbilityToActivate
        {
            get
            {
                AnimatedAbility ability = null;

                if(this.DefaultAbility != null)
                    ability = this.DefaultAbility;
                else if(this.AnimatedAbilities != null && this.AnimatedAbilities.Count > 0)
                {
                    ability = this.AnimatedAbilities.First();
                }

                return ability;
            }
        }
        [JsonIgnore]
        public string LastCostumeFile
        {
            get;
            set;
        }

        [JsonIgnore]
        public Character GhostShadow
        {
            get; set;
        }

        private bool isActive;
        [JsonIgnore]
        public bool IsActive
        {
            get
            {

                return isActive;
            }
            set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        private bool isGangLeader;
        [JsonIgnore]
        public bool IsGangLeader
        {
            get
            {

                return isGangLeader;
            }
            set
            {
                isGangLeader = value;
                OnPropertyChanged("IsGangLeader");
            }
        }
        [JsonIgnore]
        public bool IsAttacker
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.AttackMode == AttackMode.Attack);
            }   
        }
        [JsonIgnore]
        public bool IsDefender
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.AttackMode == AttackMode.Defend);
            }
        }
        [JsonIgnore]
        public bool IsStunned
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.IsStunned);
            }
        }
        [JsonIgnore]
        public bool IsUnconscious
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.IsUnconcious);
            }
        }
        [JsonIgnore]
        public bool IsDying
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.IsDying);
            }
        }
        [JsonIgnore]
        public bool IsDead
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.IsDead);
            }
        }
        [JsonIgnore]
        public bool IsKnockedBack
        {
            get
            {
                return this.AttackConfigurationMap.Any(ac => ac.Value.Item2.IsKnockedBack);
            }
        }
        
        public void RefreshAttackConfigurationParameters()
        {
            OnPropertyChanged("AttackConfigurationMap");
            OnPropertyChanged("IsAttacker");
            OnPropertyChanged("IsDefender");
            OnPropertyChanged("IsStunned");
            OnPropertyChanged("IsUnconscious");
            OnPropertyChanged("IsDying");
            OnPropertyChanged("IsDead");
            OnPropertyChanged("IsKnockedBack");
        }
        public void UpdateDistanceCount()
        {
            if(this.CurrentPositionVector != Vector3.Zero && this.CurrentStartingPositionVectorForDistanceCounting != Vector3.Zero)
            {
                float currentDistance = Vector3.Distance(this.CurrentPositionVector, this.CurrentStartingPositionVectorForDistanceCounting);
                if (currentDistance < 5)
                    currentDistance = 5;
                this.CurrentDistanceCount = (float)Math.Round((currentDistance) / 8f, 2);
            }
        }

        public void UpdateDistanceCount(Vector3 positionVector)
        {
            if(positionVector != Vector3.Zero)
            {
                float currentDistance = Vector3.Distance(positionVector, this.CurrentStartingPositionVectorForDistanceCounting);
                if (currentDistance < 5)
                    currentDistance = 5;
                this.CurrentDistanceCount = (float)Math.Round((currentDistance) / 8f, 2);
            }
        }

        private static void SpawnLog(string msg)
        {
            try { System.IO.File.AppendAllText(@"C:\temp\spawn_log.txt", System.DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\r\n"); } catch { }
        }

        public string Spawn(bool completeEvent = true)
        {
            SpawnLog("Spawn START name=" + name + " completeEvent=" + completeEvent + " hasBeenSpawned=" + hasBeenSpawned);
            if (ManeuveringWithCamera)
            {
                ManeuveringWithCamera = false;
            }
            if (hasBeenSpawned)
            {
                SpawnLog("Spawn: already spawned, deleting existing");
                Target();
                WaitUntilTargetIsRegistered();
                if (gamePlayer != null)
                {
                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, Label);
                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
                    gamePlayer = null;
                }
                else
                    hasBeenSpawned = false;
            }
            
            string model = "Model_Statesman";
            if (ActiveIdentity != null && ActiveIdentity.Type == IdentityType.Model)
            {
                model = ActiveIdentity.Surface;
            }
            SpawnLog("Spawn: ActiveIdentity=" + (ActiveIdentity != null ? ActiveIdentity.Name + "/" + ActiveIdentity.Surface : "NULL") + " model=" + model + " label=" + Label);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, model, Label);
            SpawnLog("Spawn: GenerateKeyBindsForEvent SpawnNpc done");
            Target(false);
            SpawnLog("Spawn: Target(false) done");
            keybind = ActiveIdentity.RenderWithoutAnimation(completeEvent, this);
            SpawnLog("Spawn: RenderWithoutAnimation done keybind=" + keybind);
            if (completeEvent)
            {
                WaitUntilTargetIsRegistered();
                SpawnLog("Spawn: WaitUntilTargetIsRegistered done");
                gamePlayer = new MemoryElement();
                Position = new Position();
            }
            SpawnLog("Spawn END");
            return keybind;
        }
        [JsonIgnore]
        public bool IsSyncedWithGame
        { get; set; }

        [JsonIgnore]
        public bool IsTargeted
        {
            get
            {
                try
                {
                    MemoryElement currentTarget = new MemoryElement();
                    if (currentTarget.Label == this.Label)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }

            set
            {
                if (value == true)
                {
                    Target();
                }
                else
                {
                    if (value == false)
                    {
                        UnTarget();
                    }
                }
            }
        }

        [JsonIgnore]
        public bool IsInViewForTargeting
        {
            get
            {
                this.Target(false);
                keyBindsGenerator.CompleteEvent();
                MemoryElement currentTarget = new MemoryElement();
                return Label == currentTarget.Label;
            }
        }

        public string Target(bool completeEvent = true)
        {
            if (gamePlayer != null && gamePlayer.IsReal)
            {
                if (completeEvent)
                {
                    gamePlayer.Target(); //This ensure targeting even if not in view
                    WaitUntilTargetIsRegistered();
                }
                else
                    keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, Label);
            }
            else
            {
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, Label);
                if (completeEvent)
                {
                    keybind = keyBindsGenerator.CompleteEvent();
                    gamePlayer = WaitUntilTargetIsRegistered();
                    Position = new Position();
               }
            }
            return keybind;
            //}
            //return string.Empty;
        }
        
        public void SuperImposeGhost()
        {
            if (this.ActiveIdentity.Type == IdentityType.Model)
            {
                if (this.GhostShadow == null)
                {
                    CreateGhostShadow();
                }
                if (!this.GhostShadow.HasBeenSpawned)
                    this.GhostShadow.Spawn();
                this.Target();
                AlignGhost();
            }
        }

        public void AlignGhost()
        {
            if (this.ActiveIdentity.Type == IdentityType.Model && this.GhostShadow != null && this.GhostShadow.HasBeenSpawned)
            {
                this.GhostShadow.CurrentPositionVector = this.CurrentPositionVector;
                this.GhostShadow.CurrentModelMatrix = this.CurrentModelMatrix;
            }
        }

        public void CreateGhostShadow()
        {
            string ghostShadowName = "ghost_" + this.Name;
            //CreateGhostCostumeFileForThisCharacter("Director Solair");
            //this.GhostShadow = new Character(ghostShadowName, "Director Solair", IdentityType.Costume);
            CreateGhostCostumeFileForThisCharacter();
            this.GhostShadow = new Character(ghostShadowName, ghostShadowName, IdentityType.Costume);
            CreateGhostMovements();
        }

        public void CreateGhostCostumeFileForThisCharacter(string costumeName = null)
        {
            string costumeDirectory = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string ghostShadowCostumeFileName = ("ghost_" + this.Name) + Constants.GAME_COSTUMES_EXT;
            string ghostShadowCostumeFile = Path.Combine(costumeDirectory, ghostShadowCostumeFileName);
            string originalGhostCostumeFileName = (costumeName != null ? costumeName + "_original" : "ghost_original") + Constants.GAME_COSTUMES_EXT;
            string ghostCostumeFileName = (costumeName ?? Constants.GAME_GHOST_COSTUMENAME) + Constants.GAME_COSTUMES_EXT;
            string originalGhostCostumeFile = Path.Combine(costumeDirectory, originalGhostCostumeFileName);
            string ghostCostumeFile = Path.Combine(costumeDirectory, ghostCostumeFileName);
            if (File.Exists(originalGhostCostumeFile))
            {
                File.Copy(originalGhostCostumeFile, ghostShadowCostumeFile, true);
            }
            else if(File.Exists(ghostCostumeFile))
            {
                File.Copy(ghostCostumeFile, ghostShadowCostumeFile, true);
            }
            
        }

        public void CreateGhostMovements()
        {
            foreach(CharacterMovement cm in this.Movements)
            {
                CharacterMovement cmClone = cm.Clone();
                cmClone.Character = this.GhostShadow;
                this.GhostShadow.Movements.Add(cmClone);
            }
        }

        public void RemoveGhost(bool completeEvent = true)
        {
            if(this.GhostShadow != null)
            {
                this.GhostShadow.ClearFromDesktop(completeEvent);
                this.GhostShadow = null;
            }
        }

        public void SetActive()
        {
            this.IsActive = true;
        }

        public void ResetActive()
        {
            this.IsActive = false;
            this.IsGangLeader = false;
        }

        public void ResetOrientation()
        { 
            Vector3 currentPositionVector = this.CurrentPositionVector;
            Vector3 currentForwardVector = this.CurrentModelMatrix.Forward;
            Vector3 currentFacing = this.CurrentFacingVector;

            Microsoft.Xna.Framework.Matrix defaultMatrix = new Microsoft.Xna.Framework.Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            this.CurrentModelMatrix = defaultMatrix;
            this.CurrentFacingVector = currentFacing;
            this.CurrentPositionVector = currentPositionVector;

            AlignGhost();

            #region old angle based calculations - doesn't work for all scenarios
            //Vector3 currentRightVector = this.CurrentModelMatrix.Right;
            //Vector3 currentLeftVector = this.CurrentModelMatrix.Left;
            //double forwardX = Math.Round(currentForwardVector.X, 2);
            //double forwardZ = Math.Round(currentForwardVector.Z, 2);
            //float negY = -1 * currentForwardVector.Y;
            //float sineValue = negY > 1 ? 1 : negY < -1 ? -1 : negY;
            //float currentPitchAngle = (float)(Math.Asin(sineValue) * 180 / Math.PI);
            
            //var yaw = Math.Atan2(forwardX, forwardZ);
            //var yawAngle = (float)(yaw * 180 / Math.PI);

            //var roll = Vector3.Dot(Vector3.Up, this.CurrentModelMatrix.Up);
            //var rollValue = roll > 1 ? 1 : roll < -1 ? -1 : roll;
            //var rollAngle1Red= Math.Acos(rollValue);
            //var rollAngle = (float)(rollAngle1Red * 180 / Math.PI);

            //currentPitchAngle = (float)Math.Round(currentPitchAngle, 2);
            //var currentYawAngle = (float)Math.Round(yawAngle, 2);
            //var currentRollAngle = (float)Math.Round(rollAngle, 2);

            //if (currentPitchAngle != 0)
            //{
            //    if (forwardX < 0)
            //        currentPitchAngle = 180 - currentPitchAngle;
            //    Microsoft.Xna.Framework.Matrix rotatedMatrix = Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(currentRightVector, (float)Helper.GetRadianAngle(currentPitchAngle));
            //    this.CurrentModelMatrix *= rotatedMatrix;
            //    this.CurrentPositionVector = currentPositionVector; // Keep position intact; 
            //}
            #endregion
        }

        public void TargetAndFollow(bool completeEvent = true)
        {
            Target(false);
            if(!this.IsMoving)
            {
                //keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.NoClip);
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
                this.IsFollowed = true;
            }

            if (completeEvent)
            {
                keyBindsGenerator.CompleteEvent();
                //Action d = delegate ()
                //{
                //    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.NoClip);
                //    keyBindsGenerator.CompleteEvent();
                //};
                //AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 7000);
                //adex.ExecuteAsyncDelegate();
            }
        }

        public void UnFollow()
        {
            if(this.IsFollowed)
            {
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
                keyBindsGenerator.CompleteEvent();
                this.IsFollowed = false;
            }
        }

        public void TeleportToCamera()
        {
            Target();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.MoveNPC);
            keyBindsGenerator.CompleteEvent();
            AlignGhost();
        }

        public void TeleportToCameraWithRelativePositioning(Character closestCharacter, Vector3 closestCharacterStartingPositionVector)
        {
            Vector3 destinationPositionVector = new Camera().GetPositionVector();
            Vector3 mainVector = destinationPositionVector - closestCharacterStartingPositionVector;
            mainVector.Normalize();
            float distance = Vector3.Distance(destinationPositionVector, closestCharacterStartingPositionVector);
            if (closestCharacter != this)
            {
                Vector3 targetPositionVectorForThisCharacter = this.CurrentPositionVector + mainVector * distance;
                this.Position.X = targetPositionVectorForThisCharacter.X;
                this.Position.Y = targetPositionVectorForThisCharacter.Y;
                this.Position.Z = targetPositionVectorForThisCharacter.Z;
            }
            else
            {
                this.Position.X = destinationPositionVector.X;
                this.Position.Y = destinationPositionVector.Y;
                this.Position.Z = destinationPositionVector.Z;
            }
            AlignGhost();
            UpdateDistanceCount();
        }

        public void TeleportToCameraWithOptimalPositioning(Character closestCharacter, Vector3 closestCharacterStartingPositionVector, ref Vector3 lastReferenceVector, ref List<Vector3> usedUpPositions)
        {
            Vector3 destinationPositionVector = new Camera().GetPositionVector();
            Vector3 mainVector = destinationPositionVector - closestCharacterStartingPositionVector;
            if (lastReferenceVector == Vector3.Zero)
            {
                lastReferenceVector = mainVector;
            }
            if (closestCharacter != this)
            {
                Vector3 nextReferenceVector;
                Vector3 targetPositionVectorForThisCharacter = GetNextTargetPositionVector(destinationPositionVector, lastReferenceVector, out nextReferenceVector, ref usedUpPositions);
                lastReferenceVector = nextReferenceVector;
                this.Position.X = targetPositionVectorForThisCharacter.X;
                this.Position.Y = targetPositionVectorForThisCharacter.Y;
                this.Position.Z = targetPositionVectorForThisCharacter.Z;
                //lastLocationVector = destinationPositionVector;
                this.TurnTowards(destinationPositionVector);
            }
            else
            {
                this.Position.X = destinationPositionVector.X;
                this.Position.Y = destinationPositionVector.Y;
                this.Position.Z = destinationPositionVector.Z;
            }
            AlignGhost();
            UpdateDistanceCount();
        }

        public void PlaceOptimallyAround(Character mainCharacter, ref Vector3 lastReferenceVector, ref List<Vector3> usedUpPositions)
        {
            if(lastReferenceVector == Vector3.Zero)
            {
                lastReferenceVector = mainCharacter.CurrentPositionVector + 500 * mainCharacter.CurrentFacingVector;
            }
            if(mainCharacter != this)
            {
                Vector3 nextReferenceVector;
                Vector3 targetPositionVectorForThisCharacter = GetNextTargetPositionVector(mainCharacter.CurrentPositionVector, lastReferenceVector, out nextReferenceVector, ref usedUpPositions);
                lastReferenceVector = nextReferenceVector;
                this.Position.X = targetPositionVectorForThisCharacter.X;
                this.Position.Y = targetPositionVectorForThisCharacter.Y;
                this.Position.Z = targetPositionVectorForThisCharacter.Z;
                this.AlignFacingWith(mainCharacter);
            }
        }

        public void PlaceOptimallyAround(Vector3 targetLoactionVector, ref Vector3 lastReferenceVector, ref List<Vector3> usedUpPositions)
        {
            Vector3 facingVector = targetLoactionVector - this.CurrentPositionVector;
            if (lastReferenceVector == Vector3.Zero)
            {
                lastReferenceVector = targetLoactionVector + 500 * facingVector;
                lastReferenceVector.Y = targetLoactionVector.Y;
            }
            Vector3 nextReferenceVector;
            Vector3 targetPositionVectorForThisCharacter = GetNextTargetPositionVector(targetLoactionVector, lastReferenceVector, out nextReferenceVector, ref usedUpPositions);
            lastReferenceVector = nextReferenceVector;
            this.Position.X = targetPositionVectorForThisCharacter.X;
            this.Position.Y = targetPositionVectorForThisCharacter.Y;
            this.Position.Z = targetPositionVectorForThisCharacter.Z;
        }

        public void AlignFacingWith(Character character)
        {
            Vector3 leaderFacingVector = character.CurrentFacingVector;
            Vector3 distantPointInSameDirection = character.CurrentPositionVector + leaderFacingVector * 500;
            (this.Position as Position).SetTargetFacing(distantPointInSameDirection);
        }

        public string UnTarget(bool completeEvent = true)
        {
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            this.UnFollow();
            if (completeEvent)
            {
                keybind = keyBindsGenerator.CompleteEvent();
                try
                {
                    MemoryElement currentTarget = new MemoryElement();
                    while (currentTarget.Label != string.Empty)
                    {
                        currentTarget = new MemoryElement();
                    }
                }
                catch
                {

                }
            }
            return keybind;
        }

        public MemoryElement WaitUntilTargetIsRegistered()
        {
            int w = 0;
            MemoryElement currentTarget = new MemoryElement();
            while (Label != currentTarget.Label)
            {
                w++;
                currentTarget = new MemoryElement();
                if (w > 5)
                {
                    currentTarget = null;
                    break;
                }
            }
            if(currentTarget != null)
            {
                SetAsSpawned();
            }
            return currentTarget;
        }

        public void ScanAndFixMemoryPointer()
        {
            if (HasBeenSpawned)
            {
                this.Target();
                var memoryElement = WaitUntilTargetIsRegistered();

                if (memoryElement == null)
                {
                    this.Target(false);
                    keyBindsGenerator.CompleteEvent();
                    MemoryElement currentTarget = new MemoryElement();
                    
                    if (Label == currentTarget.Label)
                    {
                        this.gamePlayer = currentTarget;
                        this.Position = new Position();
                        //var lastKnownPosition = this.CurrentPositionVector;
                        //this.ClearFromDesktop();
                        //this.Spawn();
                        //this.CurrentPositionVector = lastKnownPosition;
                    }
                } 
            }
        }

        private string clearFromDesktop(bool completeEvent = true)
        {
            if(this.HasBeenSpawned)
            {
                this.UnFollow();
                Target(false);
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
                if (completeEvent)
                {
                    keyBindsGenerator.CompleteEvent();
                }
                if (this.GhostShadow != null && this.GhostShadow.HasBeenSpawned)
                    this.RemoveGhost(completeEvent);
            }
            gamePlayer = null;
            hasBeenSpawned = false;
            return keybind;
        }

        public string ClearFromDesktop(bool completeEvent = true)
        {
            if (ManeuveringWithCamera)
            {
                ManeuveringWithCamera = false;
            }
            return clearFromDesktop(completeEvent);
        }

        public string ClearFromDesktop(bool completeEvent, bool callingFromCamera = false)
        {
            if (callingFromCamera)
                return clearFromDesktop(completeEvent);
            else
                return ClearFromDesktop(completeEvent);
        }

        public void ToggleTargeted()
        {
            IsTargeted = !IsTargeted;
        }

        [JsonIgnore]
        public bool PlayDefaultMovement { get; set; }

        //Jeff - added so we always get the right movement when dragging, moving, etc...
        [JsonIgnore]
        public CharacterMovement DefaultMovementToActivate
        {
            get {
                CharacterMovement defaultMovementToActivate =null;
                if (this.ActiveMovement != null)
                {
                    defaultMovementToActivate = this.ActiveMovement;
                }
                else
                {
                    if (this.DefaultMovement != null)
                    {
                        defaultMovementToActivate = this.DefaultMovement;
                    }
                    else
                    {

                        if (this.Movements["Walk"] != null)
                        {
                            defaultMovementToActivate = this.Movements["Walk"];
                        }
                        else
                        {
                            if (this.Movements.Count> 0)
                            {
                                defaultMovementToActivate = this.Movements[0];
                            }
                        }
                    }
                }
                return defaultMovementToActivate;
            }
        }
        public string MoveToCamera(bool completeEvent = true)
        {
            Target();
            CharacterMovement characterMovement = this.DefaultMovementToActivate;
            
            // characterMovement = this.Movements.FirstOrDefault(cm => cm.IsActive || cm == this.DefaultMovement || cm.Name == "Walk");
            if (characterMovement == null)
            {
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.MoveNPC);
                if (completeEvent)
                {
                    keyBindsGenerator.CompleteEvent();
                }
            }
            else
            {
                var cameraPos = new Camera().GetPositionVector();
                MoveToLocation(cameraPos);
            }
            return keybind;
        }

        public void ToggleManueveringWithCamera()
        {
            ManeuveringWithCamera = !ManeuveringWithCamera;
        }

        private bool maneuveringWithCamera;
        [JsonIgnore]
        public bool ManeuveringWithCamera
        {
            get
            {
                return maneuveringWithCamera;
            }

            set
            {
                maneuveringWithCamera = value;
                if (value == true)
                {
                    camera.ManeuveredCharacter = this;
                    keybinds = camera.LastKeybinds;
                }
                else
                {
                    if (value == false)
                    {
                        camera.ManeuveredCharacter = null;
                        keybinds = camera.LastKeybinds;
                    }
                }
            }
        }

        public void AddDefaultMovements()
        {
            string[] defaultMovementNames = new string[] { "Walk", "Run", "Swim" };
            
            foreach (CharacterMovement cm in Helper.GlobalMovements)
            {
                if (this.Name != Constants.DEFAULT_CHARACTER_NAME && this.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME)
                {
                    if(defaultMovementNames.Contains(cm.Name) && this.Movements.FirstOrDefault(m => m.Name == cm.Name) == null)
                    {
                        CharacterMovement cmDefault = new CharacterMovement(cm.Name, this);
                        cmDefault.Movement = cm.Movement;
                        this.Movements.Add(cmDefault);
                    }
                    // Must uncomment the line below
                    if(this.Movements.Any(m => m.Name == cm.Name)) //&& m.ActivationKey == System.Windows.Forms.Keys.None))
                    {
                        CharacterMovement characterMovement = this.Movements.First(m => m.Name == cm.Name);
                        switch (characterMovement.Name)
                        {
                            case "Walk":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.K;
                                break;
                            case "Run":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.U;
                                break;
                            case "Swim":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.S;
                                break;
                            case "Fly":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.F;
                                break;
                            case "Jump":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.J;
                                break;
                            case "Steam Jump Jetpack":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.P;
                                break;
                            case "Beast":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.B;
                                break;
                            case "Ninja Jump":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.M;
                                break;
                            case "Ice Slide":
                                characterMovement.ActivationKey = System.Windows.Forms.Keys.I;
                                break;
                        }
                    }
                }
            }
        }

        public void AddDefaultAbilities()
        {
            if(this.OptionGroups.Any(og => og.Name == Constants.DEFAULT_ABILITIES_OPTION_GROUP_NAME))
            {
                var defaultOptGroup = this.OptionGroups.First(og => og.Name == Constants.DEFAULT_ABILITIES_OPTION_GROUP_NAME);
                this.RemoveOptionGroup(defaultOptGroup);
            }
            if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0 && !this.OptionGroups.Any(og => og.Name == Constants.DEFAULT_ABILITIES_OPTION_GROUP_NAME))
            {
                OptionGroup<AnimatedAbility> optGroup = new OptionGroup<AnimatedAbility>(Constants.DEFAULT_ABILITIES_OPTION_GROUP_NAME);
                foreach (var defaultAbility in Helper.GlobalDefaultAbilities)
                {
                    if(IsCoreDefaultAbility(defaultAbility))
                        optGroup.Add(defaultAbility);
                }
                this.AddOptionGroup(optGroup);
            }
        }
        List<string> defaultAbilities = new List<string>
            {
                "Recovery",
                "Stun Recovery",
                "Pass Turn",
                "Half Phase Action",
                "Hold Action",
                "Draw A Weapon",
                "Dodge",
                "Strike",
                "Haymaker",
                "Prone",
                "Move By",
                "Move Through",
                "Grab",
                "Disarm",
                "Block",
                "Set",
                "Sweep",
                "Rapid Fire",
                "Off Ground",
                "Generic Damage/Power"
            };
        private bool IsCoreDefaultAbility(AnimatedAbility ability)
        {
            return defaultAbilities.Contains(ability.Name);
        }
        public void SetAsSpawned()
        {
            hasBeenSpawned = true;
            gamePlayer = new MemoryElement();
            Position = new Position();
        }

        [JsonIgnore]
        public OptionGroup<AnimatedAbility> AnimatedAbilities
        {
            get
            {
                OptionGroup<AnimatedAbility> animatedAbilities = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.ABILITY_OPTION_GROUP_NAME; }) as OptionGroup<AnimatedAbility>;
                if (animatedAbilities == null)
                {
                    animatedAbilities = new OptionGroup<AnimatedAbility>(Constants.ABILITY_OPTION_GROUP_NAME);
                    optionGroups.Add(animatedAbilities);
                }
                return animatedAbilities;
            }
        }

        [JsonIgnore]
        public OptionGroup<CharacterMovement> Movements
        {
            get
            {
                OptionGroup<CharacterMovement> movements = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.MOVEMENT_OPTION_GROUP_NAME; }) as OptionGroup<CharacterMovement>;
                if (movements == null)
                {
                    movements = new OptionGroup<CharacterMovement>(Constants.MOVEMENT_OPTION_GROUP_NAME);
                    optionGroups.Add(movements);
                }
                return movements;
            }
        }

        public void Activate()
        {
            //if (HasBeenSpawned && this.ActiveIdentity.Type == IdentityType.Costume)
            //{
            //    Target(false);
            //    //ChangeCostumeColor(new ColorExtensions.RGB() { R = 255, G = 0, B = 51 });
            //}
        }

        private void changeColorIntoCharacterCostumeFile(string origFile, string newFile, ColorExtensions.RGB color, int colorNumber = 2)
        {
            if (colorNumber < 1 || colorNumber > 4)
            {
                return;
            }
            string fileStr = File.ReadAllText(origFile);
            string color2 = "Color" + colorNumber + @"\s+([\d]{1,3}),\s+([\d]{1,3}),\s+([\d]{1,3})";
            Regex re = new Regex(color2);
            fileStr = re.Replace(fileStr, string.Format("Color2 {0}, {1}, {2}", color.R, color.G, color.B));

            File.AppendAllText(newFile, fileStr);
        }

        public void Deactivate()
        {

            if (this.ActiveIdentity.Type != IdentityType.Costume)
                return;

            bool persistentAbilityActive = this.AnimatedAbilities.Where(aa => aa.Persistent && aa.IsActive).FirstOrDefault() != null || (this.ActiveIdentity.AnimationOnLoad != null && this.ActiveIdentity.AnimationOnLoad.Persistent);

            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                ActiveIdentity.Surface + "_original" + Constants.GAME_COSTUMES_EXT);
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                ActiveIdentity.Surface + Constants.GAME_COSTUMES_EXT);
            // Load persistent fx if present
            if (persistentAbilityActive)
            {
                string persistentCostumeFile = Path.Combine(
                    Settings.Default.CityOfHeroesGameDirectory,
                    Constants.GAME_COSTUMES_FOLDERNAME,
                    this.ActiveIdentity.Surface + "_persistent" + Constants.GAME_COSTUMES_EXT);
                if (File.Exists(persistentCostumeFile))
                {
                    Target();
                    File.Copy(persistentCostumeFile, origFile, true);
                    KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, ActiveIdentity.Surface);
                    keyBindsGenerator.CompleteEvent();
                }
                
            } // Otherwise load default costume
            else if (File.Exists(archFile))
            {
                Target();
                File.Copy(archFile, origFile, true);
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, ActiveIdentity.Surface);
                keyBindsGenerator.CompleteEvent();
            }
        }

        public void RefreshAbilitiesActivationEligibility(List<AbilityActivationEligibility> eligibilityCollection = null)
        {
            if (this.OptionGroups != null && this.OptionGroups.Count > 0)
            {
                foreach (var optionGroup in this.OptionGroups)
                {
                    foreach (ICharacterOption option in optionGroup.Options)
                    {
                        if (eligibilityCollection != null)
                        {
                            AbilityActivationEligibility abilityActivationEligibility = eligibilityCollection.FirstOrDefault(e => e.AbilityName == option.Name);
                            if (abilityActivationEligibility != null)
                                option.IsEnabled = abilityActivationEligibility.IsEnabled;
                        }
                        else
                            option.IsEnabled = true;
                    }
                }
            }
        }

        public void DisableAttacks(List<Attack> exclusionList = null)
        {
            if (this.OptionGroups != null && this.OptionGroups.Count > 0)
            {
                foreach (var optionGroup in this.OptionGroups)
                {
                    foreach (ICharacterOption option in optionGroup.Options)
                    {
                        if (option is AnimatedAbility)
                        {
                            if ((option as AnimatedAbility).IsAttack && !(exclusionList != null && exclusionList.Contains(option as Attack)))
                                option.IsEnabled = false;
                        }
                        else
                            option.IsEnabled = true;
                    }
                }
            }
        }
        public void DisableSimpleAbilities(List<AnimatedAbility> exclusionList = null)
        {
            if (this.OptionGroups != null && this.OptionGroups.Count > 0)
            {
                foreach (var optionGroup in this.OptionGroups)
                {
                    foreach (ICharacterOption option in optionGroup.Options)
                    {
                        if (option is AnimatedAbility)
                        {
                            if (!(option as AnimatedAbility).IsAttack && !(exclusionList != null && exclusionList.Contains(option as AnimatedAbility)))
                                option.IsEnabled = false;
                        }
                        else
                            option.IsEnabled = true;
                    }
                }
            }
        }

        public Guid AddAttackConfiguration(Attack attack, AttackConfiguration attackConfig, Guid configKey = default(Guid))
        {
            if (configKey == Guid.Empty)
                configKey = Guid.NewGuid();
            if (this.AttackConfigurationMap.ContainsKey(configKey))
            {
                this.AttackConfigurationMap[configKey] = new Tuple<Attack, AttackConfiguration>(attack, attackConfig);
            }
            else
            {
                this.AttackConfigurationMap.Add(configKey, new Tuple<Attack, AttackConfiguration>(attack, attackConfig));
            }
            this.RefreshAttackConfigurationParameters();

            return configKey;
        }

        public void RemoveAttackConfiguration(Guid configKey)
        {
            if (this.AttackConfigurationMap.ContainsKey(configKey))
            {
                this.AttackConfigurationMap.Remove(configKey);
                this.RefreshAttackConfigurationParameters();
            }
        }

        #region Movements

        public void MoveToLocation(Vector3 destinationVector)
        {
            CharacterMovement characterMovement = this.DefaultMovementToActivate;
            //CharacterMovement characterMovement = this.Movements.FirstOrDefault(cm => cm.IsActive || cm == this.DefaultMovement || cm.Name == "Walk");
            if (characterMovement != null)
            {
                characterMovement.Movement.Move(this, destinationVector);
            }
        }

        public void MoveToLocationWithRelativePositioning(Vector3 destinationPositionVector, Character cloasestCharacter, Vector3 closestCharacterStartingPositionVector)
        {
            Vector3 mainVector = destinationPositionVector - closestCharacterStartingPositionVector;
            mainVector.Normalize();
            if(cloasestCharacter != this)
            {
                float distanceToTravel = Vector3.Distance(destinationPositionVector, closestCharacterStartingPositionVector);
                Vector3 targetPositionVector = this.CurrentPositionVector + mainVector * distanceToTravel;
                this.MoveToLocation(targetPositionVector);
            }
            else
            {
                this.MoveToLocation(destinationPositionVector);
            }
        }
        public void MoveToLocationWithOptimalPositioning(Vector3 destinationPositionVector,  Character closestCharacter, Vector3 closestCharacterStartingPositionVector, ref Vector3 lastReferenceVector, ref List<Vector3> usedUpPositions)
        {
            Vector3 mainVector = destinationPositionVector - closestCharacterStartingPositionVector;
            if (lastReferenceVector == Vector3.Zero)
            {
                lastReferenceVector = mainVector;
            }
            if (closestCharacter != this)
            {
                Vector3 nextReferenceVector;
                Vector3 altPosVector = GetNextTargetPositionVector(destinationPositionVector, lastReferenceVector, out nextReferenceVector, ref usedUpPositions);
                lastReferenceVector = nextReferenceVector;
                this.MoveToLocation(altPosVector);
                lastLocationVector = destinationPositionVector;
                this.DefaultMovementToActivate.Movement.MovementFinished += TurnOnMovementFinished;
            }
            else
            {
                this.MoveToLocation(destinationPositionVector);
            }
        }

        private Vector3 GetNextTargetPositionVector(Vector3 locationVector, Vector3 lastReferenceVector, out Vector3 nextReferenceVector, ref List<Vector3> usedUpPositions)
        {
            lastReferenceVector.Normalize();

            float x = lastReferenceVector.X;
            float y = lastReferenceVector.Z;
            float top1 = (float)(2 * .9 * (Math.Sqrt(x * x + y * y)) * x);
            float top2 = (float)Math.Sqrt(4 * 0.9 * 0.9 * x * x *(x * x + y * y) - 4 * ( x * x + y * y) * (0.9 * 0.9 * (x * x + y * y) - y * y));
            float bottom = (2 * (y * y + x * x));
            float resultX1 = (top1 + top2) / bottom;
            float resultX2 = (top1 - top2) / bottom;
            float resultY1 = (float)Math.Sqrt(1 - (resultX1 * resultX1));
            float resultY2 = (float)Math.Sqrt(1 - (resultX2 * resultX2));

            var nextRef1 = new Vector3(resultX1, lastReferenceVector.Y, resultY1);
            nextRef1.Normalize();
            var nextRef2 = new Vector3(resultX1, lastReferenceVector.Y, -1 * resultY1);
            nextRef2.Normalize();
            var nextRef3 = new Vector3(resultX2, lastReferenceVector.Y, resultY2);
            nextRef3.Normalize();
            var nextRef4 = new Vector3(resultX2, lastReferenceVector.Y, -1 * resultY2);
            nextRef4.Normalize();

            Vector3 nextTargetVector1 = locationVector + nextRef1 * 8;
            Vector3 nextTargetVector2 = locationVector + nextRef2 * 8;
            Vector3 nextTargetVector3 = locationVector + nextRef3 * 8;
            Vector3 nextTargetVector4 = locationVector + nextRef4 * 8;

            Vector3[] targetVectors = new Vector3[] { nextTargetVector1, nextTargetVector2, nextTargetVector3, nextTargetVector4 };
            Vector3[] refVectors = new Vector3[] { nextRef1, nextRef2, nextRef3, nextRef4 };

            int i = 0;
            bool foundNothing = false;
            while (usedUpPositions.Any(p => Vector3.Distance(p, targetVectors[i]) < 3))
            {
                i++;
                if (i == 3)
                {
                    foundNothing = true;
                    break;
                }
            }

           Vector3 nextTargetVector = locationVector + refVectors[i] * 8;
            if(foundNothing)
            {
                nextTargetVector.X += 2;
                nextTargetVector.Z += 2;
            }

            usedUpPositions.Add(nextTargetVector);
            nextReferenceVector = refVectors[i];

            return nextTargetVector;
        }

        private Vector3 lastLocationVector = Vector3.Zero;
        private void TurnOnMovementFinished(object sender, EventArgs e)
        {
            if (lastLocationVector != Vector3.Zero)
            {
                this.DefaultMovementToActivate.Movement.MovementFinished -= TurnOnMovementFinished;
                Action d = delegate ()
                {
                    this.TurnTowards(lastLocationVector);
                    lastLocationVector = Vector3.Zero;
                };
                AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 1000);
                adex.ExecuteAsyncDelegate();
            }
        }

        public void TurnTowards(Vector3 targetVector)
        {
            if (this.MovementInstruction == null)
                this.MovementInstruction = new HeroVirtualTabletop.Movements.MovementInstruction();
            CharacterMovement characterMovement = this.DefaultMovementToActivate;
            Vector3 currentPositionVector = this.CurrentPositionVector;
            Microsoft.Xna.Framework.Matrix newRotationMatrix = Microsoft.Xna.Framework.Matrix.CreateLookAt(currentPositionVector, targetVector, this.CurrentModelMatrix.Up);
            if (float.IsNaN(newRotationMatrix.M11) || float.IsNaN(newRotationMatrix.M13) || float.IsNaN(newRotationMatrix.M31) || float.IsNaN(newRotationMatrix.M33))
                return;
            newRotationMatrix.M11 *= -1;
            newRotationMatrix.M33 *= -1;
            var newModelMatrix = new Microsoft.Xna.Framework.Matrix
            {
                M11 = newRotationMatrix.M11,
                M12 = this.CurrentModelMatrix.M12,
                M13 = newRotationMatrix.M13,
                M14 = this.CurrentModelMatrix.M14,
                M21 = this.CurrentModelMatrix.M21,
                M22 = this.CurrentModelMatrix.M22,
                M23 = this.CurrentModelMatrix.M23,
                M24 = this.CurrentModelMatrix.M24,
                M31 = newRotationMatrix.M31,
                M32 = this.CurrentModelMatrix.M32,
                M33 = newRotationMatrix.M33,
                M34 = this.CurrentModelMatrix.M34,
                M41 = this.CurrentModelMatrix.M41,
                M42 = this.CurrentModelMatrix.M42,
                M43 = this.CurrentModelMatrix.M43,
                M44 = this.CurrentModelMatrix.M44
            };
            //target.CurrentModelMatrix = newModelMatrix;
            // Turn to destination, figure out angle
            Vector3 targetForwardVector = newModelMatrix.Forward;
            Vector3 currentForwardVector = this.CurrentModelMatrix.Forward;
            bool isClockwiseTurn;
            float origAngle = MathHelper.ToDegrees(Helper.Get2DAngleBetweenVectors(currentForwardVector, targetForwardVector, out isClockwiseTurn));
            var angle = origAngle;

            if (isClockwiseTurn)
                this.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Upward;
            else
            {
                this.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Downward;
            }

            bool turnCompleted = false;
            while (!turnCompleted)
            {
                if (angle > 2)
                {
                    characterMovement.Movement.Turn(this, 2);
                    angle -= 2;
                }
                else
                {
                    characterMovement.Movement.Turn(this, angle);
                    turnCompleted = true;
                }
                Thread.Sleep(5);
            }
        }

        #endregion
    }
}
