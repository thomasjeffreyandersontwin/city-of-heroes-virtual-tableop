using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.Shared.Events;
using Module.Shared.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Movements
{
    public class CharacterMovement : CharacterOption
    {
        #region Events

        public event EventHandler<CustomEventArgs<CharacterMovement>> MovementActivatedForGangLeader;
        public void OnMovementActivatedForGangLeader(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (MovementActivatedForGangLeader != null)
                MovementActivatedForGangLeader(sender, e);
        }

        public event EventHandler<CustomEventArgs<CharacterMovement>> MovementDeactivatedForGangLeader;
        public void OnMovementDeactivatedForGangLeader(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (MovementDeactivatedForGangLeader != null)
                MovementDeactivatedForGangLeader(sender, e);
        }

        #endregion
        private IntPtr hookID;

        [JsonConstructor]
        private CharacterMovement() { }

        public CharacterMovement(string name, Character owner = null)
        {
            this.Name = name;
            this.Character = owner;

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

        private bool isPaused;
        [JsonIgnore]
        public bool IsPaused
        {
            get
            {
                return isPaused;
            }
            set
            {
                isPaused = value;
                if (value)
                {
                    this.Movement.PauseMovement(this.Character);
                }
                else
                {
                    this.Movement.ResumeMovement(this.Character);
                    Helper.GlobalVariables_CharacterMovement = this;

                }
                OnPropertyChanged("IsPaused");
            }
        }

        private bool isNonCombatMovement;
        public bool IsNonCombatMovement
        {
            get
            {
                return isNonCombatMovement;
            }
            set
            {
                isNonCombatMovement = value;
                OnPropertyChanged("IsNonCombatMovement");
            }
        }
        private float distanceLimit = float.MinValue;
        [JsonIgnore]
        public float DistanceLimit
        {
            get
            {
                return distanceLimit;
            }
            set
            {
                distanceLimit = value;
                OnPropertyChanged("DistanceLimit");
            }
        }

        private Keys activationKey;
        public Keys ActivationKey
        {
            get
            {
                return activationKey;
            }
            set
            {
                activationKey = value;
                OnPropertyChanged("ActivationKey");
            }
        }

        private double movementSpeed;
        public double MovementSpeed
        {
            get
            {
                if (movementSpeed == 0)
                    movementSpeed = 1;
                return movementSpeed;
            }
            set
            {
                movementSpeed = value;
                OnPropertyChanged("MovementSpeed");
            }
        }

        private Character character;
        public Character Character
        {
            get
            {
                return character;
            }
            set
            {
                character = value;
                OnPropertyChanged("Character");
            }
        }

        private Movement movement;
        public Movement Movement
        {
            get
            {
                return movement;
            }
            set
            {
                movement = value;
                OnPropertyChanged("Movement");
            }
        }
        public List<Character> CharactersToMove
        {
            get;
            set;
        }

        private string optionTooltip;
        public override string OptionTooltip
        {
            get
            {
                optionTooltip = Name + "(Alt + " + ActivationKey.ToString() + ")";
                return optionTooltip;
            }

            set
            {
                optionTooltip = value;
                OnPropertyChanged("OptionTooltip");
            }
        }

        public CharacterMovement Clone()
        {
            CharacterMovement clonedCharacterMovement = new Movements.CharacterMovement(this.Name, this.Character);
            clonedCharacterMovement.Movement = this.Movement != null ? this.Movement.Clone() : null;
            clonedCharacterMovement.ActivationKey = this.ActivationKey;
            clonedCharacterMovement.MovementSpeed = this.MovementSpeed;

            return clonedCharacterMovement;
        }

        private void EnableCamera(bool enable)
        {
            string cameraFileName = enable ? Constants.GAME_ENABLE_CAMERA_FILENAME : Constants.GAME_DISABLE_CAMERA_FILENAME;
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, cameraFileName);
            keyBindsGenerator.CompleteEvent();
        }

        public void DeactivateMovement()
        {
            // Reset Active
            this.IsActive = false;
            //// Back to still move - commented out as Jeff doesn't want still to be played when cancelling a move because he wants to resume the move later
            //this.Movement.MoveStill(this.Character);
            // Reset MovementInstruction
            this.Character.MovementInstruction = null;
            this.Character.ActiveMovement = null;
            // Enable Camera
            this.EnableCamera(true);
            // Unload Keyboard Hook
            KeyBoardHook.UnsetHook(hookID);
            this.Movement.StopMovement(this.Character);
            this.Character.PlayDefaultMovement = false;
            Helper.GlobalVariables_CharacterMovement = null;
            //if (this.Character.GhostShadow != null && this.Character.GhostShadow.HasBeenSpawned && this.Character.ActiveIdentity.Type == IdentityType.Model)
            //{
            //    CharacterMovement cmGhost = this.Character.GhostShadow.Movements.FirstOrDefault(cm => cm.Name == this.Name);
            //    if (cmGhost != null)
            //        cmGhost.DeactivateMovement();
            //}
            //if (this.Character.IsGangLeader)
            //    OnMovementDeactivatedForGangLeader(this, new CustomEventArgs<CharacterMovement> { Value = this });
        }

        public void ActivateMovement()
        {
            // Deactivate Current Movement
            CharacterMovement activeCharacterMovement = this.Character.Movements.FirstOrDefault(cm => cm != this && cm.IsActive);
            if (activeCharacterMovement != null)
                activeCharacterMovement.DeactivateMovement();

            // Set Active
            this.IsActive = true;
            this.Character.ActiveMovement = this;
            // Set the still Move
            this.Movement.MoveStill(this.Character);
            // Initialize MovementInstruction
            this.Character.MovementInstruction = new MovementInstruction();
            this.Character.MovementInstruction.IsMoving = false;
            this.Character.MovementInstruction.IsTurning = false;
            this.Character.MovementInstruction.IsMovingToDestination = false;
            this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None;
            this.Character.MovementInstruction.LastMovementDirection = MovementDirection.None;
            // Disable Camera Control
            this.EnableCamera(false);
            // Load Keyboard Hook
            hookID = KeyBoardHook.SetHook(this.PlayMovementByKeyProc);
            Helper.GlobalVariables_CharacterMovement = this;

            //if (this.Character.IsGangLeader)
            //    OnMovementActivatedForGangLeader(this, new CustomEventArgs<CharacterMovement> { Value = this });
        }

        public void ActivateMovement(Character character)
        {
            if (this.Character == character)
                ActivateMovement();
            else if (character.Movements.Any(m => m.Name == this.Name))
            {
                CharacterMovement cm = character.Movements.First(m => m.Name == this.Name);
                cm.ActivateMovement();
            }
            else
            {
                CharacterMovement cm = this.Clone();
                cm.Character = character;
                character.Movements.Add(cm);
                cm.ActivateMovement();
            }
        }

        public void DeactivateMovement(Character character)
        {
            if (this.Character == character)
                DeactivateMovement();
            else if (character.Movements.Any(m => m.Name == this.Name && m.IsActive))
            {
                CharacterMovement cm = character.Movements.First(m => m.Name == this.Name && m.IsActive);
                cm.DeactivateMovement();
            }
            else
            {
                CharacterMovement cm = character.Movements.FirstOrDefault(m => m.IsActive);
                if (cm != null)
                    cm.DeactivateMovement();
            }
            character.PlayDefaultMovement = false;
        }

        public void DeactivateMovement(List<Character> targets)
        {
            this.CharactersToMove = null;
            DeactivateMovement();
            targets.ForEach(t => t.PlayDefaultMovement = false);
        }

        public void ActivateMovement(List<Character> targets)
        {
            this.CharactersToMove = targets;
            // Deactivate Current Movement
            if (targets.Any(t => t.Movements.Any(m => m.IsActive)))
            {
                foreach (CharacterMovement cm in targets.Where(t => t.Movements.Any(m => m.IsActive)).SelectMany(t => t.Movements.Where(m => m.IsActive)))
                {
                    cm.DeactivateMovement();
                }
            }

            // Set Active
            this.IsActive = true;
            this.Character.ActiveMovement = this;
            // Set the still Move
            foreach(var target in targets)
            { 
                target.DefaultMovementToActivate.Movement.MoveStill(target);
            }
            //this.Movement.MoveStill(targets);
            // Initialize MovementInstruction
            this.Character.MovementInstruction = new MovementInstruction();
            this.Character.MovementInstruction.IsMoving = false;
            this.Character.MovementInstruction.IsTurning = false;
            this.Character.MovementInstruction.IsMovingToDestination = false;
            this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None;
            this.Character.MovementInstruction.LastMovementDirection = MovementDirection.None;
            this.Character.MovementInstruction.AdjustPositionToAvoidCollision = true;
            this.Character.MovementInstruction.BodyPartsToConsiderForCollision
                = new List<BodyPart> { BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle, BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom };
            // Disable Camera Control
            this.EnableCamera(false);
            this.Movement.AlignFacingWithLeader(targets);

            // Load Keyboard Hook
            hookID = KeyBoardHook.SetHook(this.PlayMovementForMultipleCharactersByKeyProc);
            Helper.GlobalVariables_CharacterMovement = this;
        }

        private IntPtr PlayMovementByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardDLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardDLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;

                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    var cohWindow = WindowsUtilities.FindWindow("CrypticWindow", null);
                    var currentProcId = Process.GetCurrentProcess().Id;
                    //bool keyUp = wmKeyboard == KeyboardMessage.WM_KEYUP;
                    var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                    if (foregroundWindow == cohWindow
                        || currentProcId == wndProcId)
                    {
                        if (inputKey == Key.CapsLock && !this.IsPaused)
                        {
                            this.IsPaused = true;
                            //this.Character.TargetAndFollow();
                            this.EnableCamera(true);
                            IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                            WindowsUtilities.SetForegroundWindow(winHandle);
                        }
                        else if (inputKey == Key.CapsLock && this.IsPaused)
                        {
                            this.IsPaused = false;
                            this.EnableCamera(false);
                            //this.Character.UnFollow();
                        }
                        else if (!this.IsPaused && this.Character.MovementInstruction != null)
                        {
                            if (inputKey == Key.Escape)
                            {
                                DeactivateMovement();
                                this.Character.ActiveMovement = null;
                            }
                            else if (inputKey == Key.Left || inputKey == Key.Right || inputKey == Key.Up || inputKey == Key.Down)
                            {
                                MovementDirection turnDirection = GetTurnAxisDirectionFromKey(inputKey);
                                if (turnDirection != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = false;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = true;
                                    if (this.Character.MovementInstruction.CurrentRotationAxisDirection != turnDirection)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision as the character is changing the facing
                                        //Jeff - next two lines this was interfering with clean move, turn, move
                                        //this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        //this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None; // Reset key movement
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = turnDirection;
                                        this.Movement.StartMovement(this.Character);
                                    }
                                }
                            }
                            else
                            {
                                MovementDirection direction = Helper.GetMovementDirectionFromKey(inputKey);
                                if (direction != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = true;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = false;
                                    if (this.Character.MovementInstruction.CurrentMovementDirection != direction)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None; // Reset turn
                                        this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        this.Character.MovementInstruction.CurrentMovementDirection = direction;
                                        this.Movement.StartMovement(this.Character);
                                    }
                                }

                            }
                        }
                    }
                    //WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private IntPtr PlayMovementForMultipleCharactersByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardDLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardDLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;

                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    var cohWindow = WindowsUtilities.FindWindow("CrypticWindow", null);
                    var currentProcId = Process.GetCurrentProcess().Id;
                    //bool keyUp = wmKeyboard == KeyboardMessage.WM_KEYUP;
                    var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                    if (foregroundWindow == cohWindow
                        || currentProcId == wndProcId)
                    {
                        if (inputKey == Key.CapsLock && !this.IsPaused)
                        {
                            this.IsPaused = true;
                            //this.Character.TargetAndFollow();
                            this.EnableCamera(true);
                            IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                            WindowsUtilities.SetForegroundWindow(winHandle);
                        }
                        else if (inputKey == Key.CapsLock && this.IsPaused)
                        {
                            this.IsPaused = false;
                            this.EnableCamera(false);
                            //this.Character.UnFollow();
                        }
                        else if (!this.IsPaused && this.Character.MovementInstruction != null && this.CharactersToMove != null)
                        {
                            //if (inputKey == Key.Escape)
                            //{
                            //    DeactivateMovement(this.CharactersToMove);
                            //    this.Character.ActiveMovement = null;
                            //}
                            //else 
                            if (inputKey == Key.Left || inputKey == Key.Right || inputKey == Key.Up || inputKey == Key.Down)
                            {
                                MovementDirection turnDirection = GetTurnAxisDirectionFromKey(inputKey);
                                if (turnDirection != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = false;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = true;
                                    if (this.Character.MovementInstruction.CurrentRotationAxisDirection != turnDirection)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision as the character is changing the facing
                                        //Jeff - next two lines this was interfering with clean move, turn, move
                                        //this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        //this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None; // Reset key movement
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = turnDirection;
                                        this.Movement.StartMovement(this.CharactersToMove);
                                    }
                                }
                            }
                            else
                            {
                                MovementDirection direction = Helper.GetMovementDirectionFromKey(inputKey);
                                if (direction != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = true;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = false;
                                    if (this.Character.MovementInstruction.CurrentMovementDirection != direction)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None; // Reset turn
                                        this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        this.Character.MovementInstruction.CurrentMovementDirection = direction;
                                        this.Movement.StartMovement(this.CharactersToMove);
                                    }
                                }

                            }
                        }
                    }
                    //WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private MovementDirection GetTurnAxisDirectionFromKey(Key key)
        {
            MovementDirection turnAxisDirection = MovementDirection.None;
            bool modifierKeyPresent = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            switch (key)
            {
                case Key.Up:
                    turnAxisDirection = MovementDirection.Right;
                    break;
                case Key.Down:
                    turnAxisDirection = MovementDirection.Left;
                    break;
                case Key.Left:
                    if (modifierKeyPresent)
                        turnAxisDirection = MovementDirection.Backward;
                    else
                        turnAxisDirection = MovementDirection.Downward;
                    break;
                case Key.Right:
                    if (modifierKeyPresent)
                        turnAxisDirection = MovementDirection.Forward;
                    else
                        turnAxisDirection = MovementDirection.Upward;
                    break;
            }
            return turnAxisDirection;
        }
    }
    public class Movement : NotifyPropertyChanged
    {
        private Dictionary<Character, System.Threading.Timer> characterMovementTimerDictionary;
        //private System.Threading.Timer timer;
        [JsonConstructor]
        private Movement() { }
        private ILogManager logManager = new FileLogManager(typeof(Movement));

        public event EventHandler<CustomEventArgs<Tuple<Character, Guid>>> MovementFinished;
        public void OnMovementFinished(object sender, CustomEventArgs<Tuple<Character, Guid>> e)
        {
            if (MovementFinished != null)
                MovementFinished(sender, e);
        }
        public Movement(string name)
        {
            this.Name = name;
            this.characterMovementTimerDictionary = new Dictionary<Character, System.Threading.Timer>();
            this.AddDefaultMemberAbilities();
        }

        //public bool IsPlaying { get; set; }


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

        private List<AnimationElement> supportingAnimationElementsForMovement;

        private ObservableCollection<MovementMember> movmementMembers;
        public ObservableCollection<MovementMember> MovementMembers
        {
            get
            {
                return movmementMembers;
            }
            set
            {
                movmementMembers = value;
                OnPropertyChanged("MovementMembers");
            }
        }

        private bool hasGravity;
        public bool HasGravity
        {
            get
            {
                return hasGravity;
            }
            set
            {
                hasGravity = value;
                OnPropertyChanged("HasGravity");
            }
        }

        #region Clone

        public Movement Clone()
        {
            Movement clonedMovement = new Movements.Movement(this.Name);
            clonedMovement.HasGravity = this.HasGravity;
            clonedMovement.MovementMembers.Clear();
            foreach (MovementMember member in this.MovementMembers)
            {
                MovementMember clonedMember = member.Clone();
                clonedMovement.MovementMembers.Add(clonedMember);
            }

            return clonedMovement;
        }

        #endregion

        #region Move, Move Back

        public async Task Move(Character target)
        {
            double rotationAngle = GetRotationAngle(target.MovementInstruction.CurrentMovementDirection);
            Vector3 facingToDest = new Vector3();
            Vector3 directionVector = new Vector3();
            if (target.MovementInstruction.IsMovingToDestination)
            {
                if (target.MovementInstruction.IsDestinationPointAdjusted)
                {
                    var distance = Vector3.Distance(target.CurrentPositionVector, target.MovementInstruction.DestinationVector);
                    var adjustmentDest = target.MovementInstruction.DestinationPointHeightAdjustment < 1 ? 10 : target.MovementInstruction.DestinationPointHeightAdjustment < 3 ? 20 : 30;
                    if (distance < adjustmentDest)
                    {
                        target.MovementInstruction.DestinationVector = target.MovementInstruction.OriginalDestinationVector;
                        target.MovementInstruction.IsDestinationPointAdjusted = false;
                    }
                }

                //determine facing vector from current and target
                facingToDest = target.MovementInstruction.DestinationVector - target.CurrentPositionVector;
                facingToDest.Normalize();
                directionVector = GetDirectionVector(0, target.MovementInstruction.CurrentMovementDirection, facingToDest);
                if (IsNan(directionVector))
                {
                    //logManager.Info(string.Format("DirectionVector is NAN: {0}, target: {1}, facingToDest: {2}", directionVector, target.Name, facingToDest));
                }
            }
            else
            {
                directionVector = GetDirectionVector(target);
            }
            target.MovementInstruction.CurrentDirectionVector = directionVector;
            if (!IsNan(directionVector))
            {
                //increment character position
                DateTime startTime = DateTime.Now;
                Vector3 allowableDestinationVector = GetAllowableDestinationVector(target, directionVector);
                DateTime endTime = DateTime.Now;
                float dist = Vector3.Distance(allowableDestinationVector, target.CurrentPositionVector);
                //logManager.Info(string.Format("GetAllowableDestinationVector took {0} milliseconds for {1} who advanced {2} units", (endTime - startTime).Milliseconds, target.Name, dist));
                target.CurrentPositionVector = allowableDestinationVector;
                target.AlignGhost();
            }
            else
            {

            }
        }

        private Guid currentConfigKey = Guid.Empty;

        public void MoveBack(Character target, Vector3 lookatVector, Vector3 destinationVector, Guid configKey = default(Guid))
        {
            if(configKey == default(Guid))
            {
                //configKey = target.AttackConfigurationMap.Where(ac => ac.Value.Item1.IsExecutionInProgress).Select(ac => ac.Key).FirstOrDefault();
                foreach (var key in target.AttackConfigurationMap.Keys)
                {
                    if (target.AttackConfigurationMap[key].Item1.IsExecutionInProgressFor(key))
                    {
                        configKey = key;
                        break;
                    }
                }
            }
            this.currentConfigKey = configKey;
            if (target.CurrentPositionVector == destinationVector)
                return;

            if (target.MovementInstruction == null)
                target.MovementInstruction = new MovementInstruction();

            SetFacingToDestination(target, lookatVector);

            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
            {
                target.MovementInstruction.AdjustPositionToAvoidCollision = false;
                target.MovementInstruction.BodyPartsToConsiderForCollision = new List<BodyPart> { BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle };
            }
            else
            {
                target.MovementInstruction.AdjustPositionToAvoidCollision = true;
                target.MovementInstruction.BodyPartsToConsiderForCollision
                    = new List<BodyPart> { BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle, BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom };
            }

            if (this.HasGravity)
            {
                Vector3 collisionGroundUp = new Vector3(destinationVector.X, destinationVector.Y + 2f, destinationVector.Z);
                Vector3 collisionGroundDown = new Vector3(destinationVector.X, -100f, destinationVector.Z);
                Vector3 collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                if (collisionVectorGround.Y < destinationVector.Y)
                {
                    // double check
                    new PauseElement("", 500).Play();

                    collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                    if (collisionVectorGround.Y < destinationVector.Y)
                    {
                        destinationVector = new Vector3(destinationVector.X, collisionVectorGround.Y, destinationVector.Z);
                    }
                }

                if(IsNanAny(collisionGroundUp, collisionGroundDown, collisionVectorGround))
                {

                }
            }
            target.MovementInstruction.DestinationVector = destinationVector;
            target.MovementInstruction.OriginalDestinationVector = destinationVector;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = true;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Backward;
            target.MovementInstruction.MovementStartTime = DateTime.UtcNow;
            if (IsNan(destinationVector))
            {

            }
            //this.knockbackDist = target.AttackConfigurationMap[target.ActiveAbility as Attack].KnockBackDistance;
            this.StartMovement(target);
            if (target.GhostShadow != null && target.GhostShadow.HasBeenSpawned && target.ActiveIdentity.Type == IdentityType.Model)
            {
                this.MovementFinished += delegate (object sender, CustomEventArgs<Tuple<Character, Guid>> e)
                {
                    DateTime endTime = DateTime.Now;
                    //logManager.Info(string.Format("Knockback Distance {0} blocks, time taken {1} milliseconds", target.AttackConfigurationMap[target.ActiveAbility as Attack].KnockBackDistance, (endTime - startTime).Milliseconds));
                    if (e.Value.Item1 == target)
                        target.AlignGhost();
                };

            }
        }

        public void Move(Character target, Vector3 destinationVector)
        {
            if (target.CurrentPositionVector == destinationVector)//
                return;//

            if (target.MovementInstruction == null)//
                target.MovementInstruction = new MovementInstruction();

            SetFacingToDestination(target, destinationVector);

            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.AdjustPositionToAvoidCollision = true;
            target.MovementInstruction.BodyPartsToConsiderForCollision
                = new List<BodyPart> { BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle, BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom };
            if (this.HasGravity)
            {
                Vector3 collisionGroundUp = new Vector3(destinationVector.X, destinationVector.Y + 2f, destinationVector.Z);
                Vector3 collisionGroundDown = new Vector3(destinationVector.X, -100f, destinationVector.Z);
                Vector3 collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                if (collisionVectorGround.Y < destinationVector.Y)
                {
                    // double check
                    new PauseElement("", 500).Play();
                    collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                    if (collisionVectorGround.Y < destinationVector.Y)
                    {
                        destinationVector = new Vector3(destinationVector.X, collisionVectorGround.Y, destinationVector.Z);
                    }
                }
            }
            target.MovementInstruction.DestinationVector = destinationVector;
            target.MovementInstruction.OriginalDestinationVector = destinationVector;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = false;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Forward;
            target.MovementInstruction.MovementStartTime = DateTime.UtcNow;
            this.StartMovement(target);
        }

        public void Move(List<Character> targets, Vector3 destinationVector)
        {
            targets.ForEach(t => t.IsMoving = false);

            Character target = this.GetLeadingCharacterForMovement(targets);
            if (target.CurrentPositionVector == destinationVector)//
                return;//

            if (target.MovementInstruction == null)//
                target.MovementInstruction = new MovementInstruction();


            SetFacingToDestination(target, destinationVector);
            AlignFacingWithLeader(targets);

            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision  //c
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.AdjustPositionToAvoidCollision = true;
            target.MovementInstruction.BodyPartsToConsiderForCollision
                = new List<BodyPart> { BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle, BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom };
            if (this.HasGravity)
            {
                Vector3 collisionGroundUp = new Vector3(destinationVector.X, destinationVector.Y + 2f, destinationVector.Z);
                Vector3 collisionGroundDown = new Vector3(destinationVector.X, -100f, destinationVector.Z);
                Vector3 collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                if (collisionVectorGround.Y < destinationVector.Y)
                {
                    // double check
                    new PauseElement("", 500).Play();
                    collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                    if (collisionVectorGround.Y < destinationVector.Y)
                    {
                        destinationVector = new Vector3(destinationVector.X, collisionVectorGround.Y, destinationVector.Z);
                    }
                }
            }
            target.MovementInstruction.DestinationVector = destinationVector;
            target.MovementInstruction.OriginalDestinationVector = destinationVector;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = false;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Forward;
            target.MovementInstruction.MovementStartTime = DateTime.UtcNow;
            this.StartMovement(targets);
        }

        public async Task Move(List<Character> targets)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            Vector3 facingToDest = new Vector3();
            Vector3 directionVector = new Vector3();
            if (target.MovementInstruction.IsMovingToDestination)
            {
                if (target.MovementInstruction.IsDestinationPointAdjusted)
                {
                    var distance = Vector3.Distance(target.CurrentPositionVector, target.MovementInstruction.DestinationVector);
                    var adjustmentDest = target.MovementInstruction.DestinationPointHeightAdjustment < 1 ? 10 : target.MovementInstruction.DestinationPointHeightAdjustment < 3 ? 20 : 30;
                    if (distance < adjustmentDest)
                    {
                        target.MovementInstruction.DestinationVector = target.MovementInstruction.OriginalDestinationVector;
                        target.MovementInstruction.IsDestinationPointAdjusted = false;
                    }
                }

                //determine facing vector from current and target
                facingToDest = target.MovementInstruction.DestinationVector - target.CurrentPositionVector;
                facingToDest.Normalize();
                directionVector = GetDirectionVector(0, target.MovementInstruction.CurrentMovementDirection, facingToDest);
            }
            else
            {
                directionVector = GetDirectionVector(target, target.MovementInstruction.CurrentMovementDirection);
            }
            target.MovementInstruction.CurrentDirectionVector = directionVector;
            if (directionVector.X != float.NaN && directionVector.Y != float.NaN && directionVector.Z != float.NaN)
            {
                //increment character
                DateTime startTime = DateTime.Now;
                Vector3 allowableDestinationVector = GetAllowableDestinationVector(target, directionVector);
                DateTime endTime = DateTime.Now;
                float dist = Vector3.Distance(allowableDestinationVector, target.CurrentPositionVector);
                //logManager.Info(string.Format("GetAllowableDestinationVector took {0} milliseconds for {1} who advanced {2} units", (endTime - startTime).Milliseconds, target.Name, dist));
                var xDiff = allowableDestinationVector.X - target.CurrentPositionVector.X;
                var yDiff = allowableDestinationVector.Y - target.CurrentPositionVector.Y;
                var zDiff = allowableDestinationVector.Z - target.CurrentPositionVector.Z;
                target.CurrentPositionVector = allowableDestinationVector;
                target.AlignGhost();

                foreach (Character movingTarget in targets.Where(t => t != target))
                {
                    Vector3 newPositionVector = new Vector3(movingTarget.CurrentPositionVector.X + xDiff, movingTarget.CurrentPositionVector.Y + yDiff, movingTarget.CurrentPositionVector.Z + zDiff);
                    movingTarget.CurrentPositionVector = newPositionVector;
                    movingTarget.AlignGhost();
                }
            }
        }

        #endregion

        #region Set Facing to Destination, Align Facing with Leader
        private void SetFacingToDestination(Character target, Vector3 destinationVector)
        {
            Vector3 currentPositionVector = target.CurrentPositionVector;
            Matrix newRotationMatrix = Matrix.CreateLookAt(currentPositionVector, destinationVector, target.CurrentModelMatrix.Up);
            if (float.IsNaN(newRotationMatrix.M11) || float.IsNaN(newRotationMatrix.M13) || float.IsNaN(newRotationMatrix.M31) || float.IsNaN(newRotationMatrix.M33))
                return;
            newRotationMatrix.M11 *= -1;
            newRotationMatrix.M33 *= -1;
            var newModelMatrix = new Matrix
            {
                M11 = newRotationMatrix.M11,
                M12 = target.CurrentModelMatrix.M12,
                M13 = newRotationMatrix.M13,
                M14 = target.CurrentModelMatrix.M14,
                M21 = target.CurrentModelMatrix.M21,
                M22 = target.CurrentModelMatrix.M22,
                M23 = target.CurrentModelMatrix.M23,
                M24 = target.CurrentModelMatrix.M24,
                M31 = newRotationMatrix.M31,
                M32 = target.CurrentModelMatrix.M32,
                M33 = newRotationMatrix.M33,
                M34 = target.CurrentModelMatrix.M34,
                M41 = target.CurrentModelMatrix.M41,
                M42 = target.CurrentModelMatrix.M42,
                M43 = target.CurrentModelMatrix.M43,
                M44 = target.CurrentModelMatrix.M44
            };
            //target.CurrentModelMatrix = newModelMatrix;
            // Turn to destination, figure out angle
            Vector3 targetForwardVector = newModelMatrix.Forward;
            Vector3 currentForwardVector = target.CurrentModelMatrix.Forward;
            bool isClockwiseTurn;
            float origAngle = MathHelper.ToDegrees(Helper.Get2DAngleBetweenVectors(currentForwardVector, targetForwardVector, out isClockwiseTurn));
            var angle = origAngle;

            if (isClockwiseTurn)
                target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Upward;
            else
            {
                target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Downward;
            }

            bool turnCompleted = false;
            while (!turnCompleted)
            {
                if (angle > 2)
                {
                    Turn(target, 2);
                    angle -= 2;
                }
                else
                {
                    Turn(target, angle);
                    turnCompleted = true;
                }
                Thread.Sleep(5);
            }
        }

        public void AlignFacingWithLeader(List<Character> targets)
        {
            Character leader = GetLeadingCharacterForMovement(targets);
            //distantPointInSameDirection.Normalize();
            foreach (Character target in targets)
            {
                target.AlignFacingWith(leader);
            }
        }

        #endregion

        #region Movement Calculations - Get Unit, Rot Angle, Speed, Collision Vector, Allowable Dest Vector etc.

        private bool IsNan(
            Vector3 vector)
        {
            return float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z);
        }

        private bool IsNanAny(params Vector3[] vectors)
        {
            return vectors.Any(v => IsNan(v));
        }

        private double GetRotationAngle(MovementDirection direction)
        {
            double rotationAngle = 0d;
            switch (direction)
            {
                case MovementDirection.Still:
                case MovementDirection.Forward:
                    rotationAngle = 0d;
                    break;
                case MovementDirection.Backward:
                    rotationAngle = 180d;
                    break;
                case MovementDirection.Left:
                    rotationAngle = 270d;
                    break;
                case MovementDirection.Right:
                    rotationAngle = 90d;
                    break;
                case MovementDirection.Upward:
                    rotationAngle = 90d;
                    break;
                case MovementDirection.Downward:
                    rotationAngle = -90d;
                    break;
            }
            return rotationAngle;
        }
        private float GetMovementUnit(Character target)
        {
            var movementSpeed = GetMovementSpeed(target);
            // Distance is updated once in every 33 milliseconds approximately - i.e. 30 times in 1 sec
            // So, normally he can travel 30 * 0.5 = 15 units per second if unit is 0.5
            float unit = (float)movementSpeed * 0.5f;
            if (target.MovementInstruction.IsMovingToDestination)
            {
                var distanceFromDestination = Vector3.Distance(target.MovementInstruction.OriginalDestinationVector, target.CurrentPositionVector);
                if (distanceFromDestination < 50) // 1 sec
                {
                    unit = (float)distanceFromDestination / 30;
                }
                else if (distanceFromDestination < 150) // 2 sec
                {
                    unit = (float)distanceFromDestination / 30 / 2;
                }
                else // 3 sec
                {
                    unit = (float)distanceFromDestination / 30 / 3;
                }

                unit *= (float)movementSpeed / 2; // Dividing by two to reduce the speed as high speeds tend to cause more errors
            }
            return unit;
        }
        private double GetMovementSpeed(Character target)
        {
            double movementSpeed = 1;
            var activeCharacterMovement = target.Movements.FirstOrDefault(cm => cm.IsActive && cm.Name == this.Name); // See if this character has speed defined for this movement
            if (activeCharacterMovement != null)
                movementSpeed = activeCharacterMovement.MovementSpeed;
            else // use speed from default character
            {
                activeCharacterMovement = Helper.GlobalMovements.FirstOrDefault(cm => cm.Name == this.Name);
                if (activeCharacterMovement != null)
                    movementSpeed = activeCharacterMovement.MovementSpeed;
            }

            return movementSpeed;
        }
        private Vector3 GetCollisionVector(Vector3 sourceVector, Vector3 destVector)
        {
            float distance = Vector3.Distance(sourceVector, destVector);
            Vector3 collisionVector = new Vector3(0, 0, 0);
            int numRetry = 3; // try 3 times
            while (numRetry > 0)
            {
                try
                {
                    var collisionInfo = IconInteractionUtility.GetCollisionInfo(sourceVector.X, sourceVector.Y, sourceVector.Z, destVector.X, destVector.Y, destVector.Z);
                    collisionVector = Helper.GetCollisionVector(collisionInfo);
                    float collisionDistance = Vector3.Distance(sourceVector, collisionVector);
                    if (!HasCollision(collisionVector) || collisionDistance <= distance) // proper collision
                        break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(500);
                    numRetry--;
                }
            }
            if(float.IsNaN(collisionVector.X) || float.IsNaN(collisionVector.Y) || float.IsNaN(collisionVector.Z))
                collisionVector = new Vector3(0, 0, 0);
            return collisionVector;
        }
        private Vector3 GetAllowableDestinationVector(Character target, Vector3 directionVector)//todo
        {
            DateTime startTime, endTime;
            Vector3 currentPositionVector = target.CurrentPositionVector;
            Vector3 destinationVectorNext = GetDestinationVector(directionVector, target.MovementInstruction.MovementUnit, target);//
            Vector3 destinationVectorFar = GetDestinationVector(directionVector, 100f, target);//

            if(IsNanAny(currentPositionVector, destinationVectorNext, destinationVectorFar))
            {
                //logManager.Info(string.Format("Nan value found. CurrentPositionVector: {0}, DestinationVectorNext: {1}, DestinationVectorFar: {2}", currentPositionVector, destinationVectorNext, destinationVectorFar));
            }

            MovementDirection direction = target.MovementInstruction.CurrentMovementDirection;//
            float distanceFromDest = Vector3.Distance(currentPositionVector, destinationVectorNext);//
            float distanceFromCollisionPoint = 0f;
            Vector3 collisionBodyPoint = new Vector3();
            bool needToCheckAdjustment = false;
            Vector3 collisionVector = new Vector3();// cc

            if (target.MovementInstruction.LastCollisionFreePointInCurrentDirection.X == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Z == -10000f)
            // Need to recalculate next collision point
            {
                startTime = DateTime.Now;
                collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);//check
                if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                    collisionVector = GetDestinationVector(-1 * directionVector, 8, collisionVector);
                endTime = DateTime.Now;
                //logManager.Info(string.Format("CalculateNextCollisionPoint took {0} milliseconds at {1}", (endTime - startTime).Milliseconds, target.CurrentPositionVector.ToString()));
                if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                    target.MovementInstruction.IsCollisionAhead = true;
                }
                else // No collision in 20 units, so free to move next 20 units
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                    target.MovementInstruction.IsCollisionAhead = false;
                }
            }

            collisionBodyPoint = Vector3.Add(currentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);//check
            collisionBodyPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y;
            distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);//check

            if (distanceFromDest > distanceFromCollisionPoint || distanceFromCollisionPoint < 1)
            // Collision point nearer, so can't move to destination without checking first
            {
                if (target.MovementInstruction.IsCollisionAhead)
                // the LastCollisionFreePointInCurrentDirection is a collision point
                {
                    if (target.MovementInstruction.AdjustPositionToAvoidCollision)
                    {
                        bool canAvoidCollision;
                        startTime = DateTime.Now;
                        Vector3 nextTravelPointToAvoidCollision = GetNextTravelPointToAvoidCollision(target,
                            out canAvoidCollision); //check
                        endTime = DateTime.Now;
                        //logManager.Info(string.Format("GetNextTravelPointToAvoidCollision took {0} milliseconds at {1}", (endTime - startTime).Milliseconds, target.CurrentPositionVector.ToString()));
                        collisionVector = nextTravelPointToAvoidCollision;
                        if (IsNan(nextTravelPointToAvoidCollision))
                        {
                            //logManager.Info(string.Format("NextTravelPointToAvoidCollision is Nan: {0}", nextTravelPointToAvoidCollision));
                        }
                        target.MovementInstruction.IsInCollision = !canAvoidCollision;
                        if (canAvoidCollision)
                        {
                            destinationVectorNext = nextTravelPointToAvoidCollision;
                            //target.MovementInstruction.CollisionAhead = false;
                            //target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // we adjusted position, so force recalculation of collision at next iteration
                        }
                        else
                        {
                            target.MovementInstruction.IsInCollision = true;
                        }
                    }
                    else
                    {
                        target.MovementInstruction.IsInCollision = true;
                        collisionVector = target.MovementInstruction.LastCollisionFreePointInCurrentDirection;
                    }
                }
                else
                // the LastCollisionFreePointInCurrentDirection is just the last calculated point, so we need to recalculate the collisions in the current direction
                {
                    startTime = DateTime.Now;
                    collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);//check
                    if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                        collisionVector = GetDestinationVector(-1 * directionVector, 8, collisionVector);
                    endTime = DateTime.Now;
                    //logManager.Info(string.Format("CalculateNextCollisionPoint took {0} milliseconds at {1}", (endTime - startTime).Milliseconds, target.CurrentPositionVector.ToString()));
                    if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                    {
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                        target.MovementInstruction.IsCollisionAhead = true;
                    }
                    else // No collision in 20 units, so free to move next 20 units
                    {
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                        target.MovementInstruction.IsCollisionAhead = false;
                    } 
                }
            }
            else
                needToCheckAdjustment = true;


            Vector3 allowableDestVector = new Vector3();
            // Now we should move to the next destination point or the LastCollisionFreePointInCurrentDirection - whichever is nearer
            collisionBodyPoint = Vector3.Add(currentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
            collisionBodyPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y;
            distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
            if ((distanceFromDest > distanceFromCollisionPoint || distanceFromCollisionPoint < 1) && target.MovementInstruction.IsInCollision)
            {
                allowableDestVector = collisionVector;
                if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                {
                    this.ApplyGravityToDestinationPoint(target, ref allowableDestVector);
                }
            }
            else
            {
                bool canAvoidCollision;
                if (target.MovementInstruction.IsPositionAdjustedToAvoidCollision && needToCheckAdjustment)
                {
                    startTime = DateTime.Now;
                    allowableDestVector = GetNextTravelPointToAvoidCollision(target, out canAvoidCollision);
                    endTime = DateTime.Now;
                    //logManager.Info(string.Format("GetNextTravelPointToAvoidCollision took {0} milliseconds at {1}", 
                    //    (endTime - startTime).Milliseconds, target.CurrentPositionVector.ToString()));
                }
                else
                    allowableDestVector = new Vector3(destinationVectorNext.X, destinationVectorNext.Y, destinationVectorNext.Z);
            }
            
            if(IsNanAny(allowableDestVector, collisionBodyPoint, collisionVector))
            {
                //logManager.Info(string.Format("Nan value found. AllowableDestVector: {0}, CollisionBodyPoint: {1}, CollisionVector: {2}", allowableDestVector, collisionBodyPoint, collisionVector));
            }

            if (this.HasGravity)
                this.ApplyGravityToDestinationPoint(target, ref allowableDestVector);

            return allowableDestVector;
        }


        private Vector3 GetNextTravelPointToAvoidCollision(Character target, out bool canAvoidCollision)
        {
            Vector3 nextTravelPoint = new Vector3();

            //Vector3 destinationVectorNext = GetDestinationVector(target.MovementInstruction.CurrentDirectionVector, target.MovementInstruction.MovementUnit, target);
            Vector3 destinationVectorNext = GetDestinationVector(target.MovementInstruction.CurrentDirectionVector, 0.25f, target);
            Vector3 collisionBodyPoint = Vector3.Add(target.CurrentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
            float distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
            float distanceFromDest = Vector3.Distance(target.CurrentPositionVector, destinationVectorNext);
            nextTravelPoint = destinationVectorNext;

            canAvoidCollision = false;

            if (target.MovementInstruction.IsPositionAdjustedToAvoidCollision)
            {
                nextTravelPoint.Y = target.CurrentPositionVector.Y; // maintain same Y till collision point is passed //check
                canAvoidCollision = true;//check
                var collDistance = Vector3.Distance(nextTravelPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
                if (collDistance > target.MovementInstruction.DistanceFromCollisionFreePoint)
                {
                    // collision point passed, so need to re-calculate
                    target.MovementInstruction.IsCollisionAhead = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // force recalculation of collision at next iteration
                    target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
                }
                target.MovementInstruction.DistanceFromCollisionFreePoint = collDistance;
            }
            else
            {
                // Check if Collision is bottom, then we might have a chance to avoid collision by adjusting position
                BodyPart bodyPart = GetBodyPartFromOffsetVector(target.MovementInstruction.CharacterBodyCollisionOffsetVector);
                if (bodyPart == BodyPart.Bottom || bodyPart == BodyPart.BottomSemiMiddle) //check
                {
                    // Check if other collisions are also present at same or less distance
                    Dictionary<BodyPart, bool> bodyPartMap = new Dictionary<BodyPart, bool>();
                    bodyPartMap.Add(BodyPart.Bottom, false);
                    bodyPartMap.Add(BodyPart.BottomSemiMiddle, true);
                    bodyPartMap.Add(BodyPart.BottomMiddle, true);
                    bodyPartMap.Add(BodyPart.Middle, true);
                    bodyPartMap.Add(BodyPart.TopMiddle, true);
                    bodyPartMap.Add(BodyPart.Top, true);

                    Vector3 destinationVector = GetDestinationVector(target.MovementInstruction.CurrentDirectionVector, 5f, target);
                    Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = GetCollisionPointsForBodyParts(target, destinationVector, bodyPartMap);
                    //logManager.Info(string.Format("calculate collision at {0}", DateTime.Now.ToString("HH:mm:ss.fff")));
                    bool hasCollision = false;
                    foreach (BodyPart key in bodyPartCollisionMap.Keys)
                    {
                        if (bodyPartCollisionMap[key] != null)
                        {
                            hasCollision = true;
                            break;
                        }

                    }
                    if (hasCollision) //todo
                    {
                        // Check if Collision is only bottom semi middle and no other parts, then we can still adjust
                        if (bodyPartCollisionMap[BodyPart.BottomSemiMiddle] != null && bodyPartCollisionMap[BodyPart.BottomMiddle] == null && bodyPartCollisionMap[BodyPart.Middle] == null
                            && bodyPartCollisionMap[BodyPart.TopMiddle] == null && bodyPartCollisionMap[BodyPart.Top] == null)
                        {
                            // only bottom semi middle collision, so adjust position
                            canAvoidCollision = true;
                            if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                                && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                            {
                                nextTravelPoint.Y += 0.25f;
                            }
                            else // we're going downwards
                            {
                                nextTravelPoint = destinationVectorNext;
                                if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                    nextTravelPoint.Y = target.CurrentPositionVector.Y;
                                //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                                //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                            }
                            var destVector = target.MovementInstruction.DestinationVector;
                            destVector.Y += 0.25f;
                            target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                            target.MovementInstruction.DestinationVector = destVector;
                            target.MovementInstruction.IsDestinationPointAdjusted = true;
                            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                        }
                        else
                        {
                            // more collisions exist
                            // check if all the collision distances are significantly larger than current collision distance
                            bool upperCollisionsFar = true;
                            foreach (BodyPart key in bodyPartCollisionMap.Keys)
                            {
                                if (bodyPartCollisionMap[key] != null)
                                {
                                    CollisionInfo collisionInfo = bodyPartCollisionMap[key];
                                    if (collisionInfo.CollisionBodyPart != BodyPart.Bottom && collisionInfo.CollisionBodyPart != BodyPart.BottomSemiMiddle)
                                    {
                                        if (distanceFromCollisionPoint + 0.25f > collisionInfo.CollisionDistance) // check if the collisions are further from current collision so that it is worth moving up 
                                        {
                                            upperCollisionsFar = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (upperCollisionsFar)
                            {
                                // we can try to avoid collision by moving up a bit
                                canAvoidCollision = true;
                                if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                                    && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                                {
                                    if (bodyPartCollisionMap[BodyPart.BottomSemiMiddle] != null) // bottom semi collision present, so lift up .75 units
                                        nextTravelPoint.Y += 0.75f;
                                    else
                                        nextTravelPoint.Y += 0.25f; // bottom collision only, so lift up .25 units

                                }
                                else // we're going downwards
                                {
                                    nextTravelPoint = destinationVectorNext;
                                    if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                        nextTravelPoint.Y = target.CurrentPositionVector.Y;
                                    //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                                    //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                                }
                                var destVector = target.MovementInstruction.DestinationVector;
                                destVector.Y += 0.25f;
                                target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                                target.MovementInstruction.DestinationVector = destVector;
                                target.MovementInstruction.IsDestinationPointAdjusted = true;
                                target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                            }
                            else
                            {
                                // no way - definite collision

                            }
                        }
                    }
                    else
                    {
                        // We can avoid collision at the bottom by adjusting position
                        canAvoidCollision = true;
                        if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                            && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                        {
                            nextTravelPoint.Y += 0.25f;
                        }
                        else // we're going downwards
                        {
                            nextTravelPoint = destinationVectorNext;
                            if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                nextTravelPoint.Y = target.CurrentPositionVector.Y;
                            //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                            //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                        }

                        var destVector = target.MovementInstruction.DestinationVector;
                        destVector.Y += 0.25f;
                        target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                        target.MovementInstruction.DestinationVector = destVector;
                        target.MovementInstruction.IsDestinationPointAdjusted = true;
                        target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                    }
                }
            }

            return nextTravelPoint;
        }
        private Dictionary<BodyPart, CollisionInfo> GetCollisionPointsForBodyParts(Character target, Vector3 destinationVector, Dictionary<BodyPart, bool> bodyPartMap)
        {
            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = new Dictionary<BodyPart, CollisionInfo>();
            foreach (var bp in Enum.GetValues(typeof(BodyPart)))
                bodyPartCollisionMap.Add((BodyPart)bp, null);

            Vector3 currentPositionVector = target.CurrentPositionVector;

            Vector3 topOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, destinationVector.Y + topOffsetVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);

            Thread.Sleep(5);

            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, destinationVector.Y + topMiddleOffsetVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);

            Thread.Sleep(5);

            Vector3 middleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, destinationVector.Y + middleOffsetVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, destinationVector.Y + bottomMiddleOffsetVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomSemiMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomSemiMiddle);
            Vector3 currentBottomSemiMiddleVector = new Vector3(currentPositionVector.X + bottomSemiMiddleOffsetVector.X, currentPositionVector.Y + bottomSemiMiddleOffsetVector.Y, currentPositionVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 destinationBottomSemiMiddleVector = new Vector3(destinationVector.X + bottomSemiMiddleOffsetVector.X, destinationVector.Y + bottomSemiMiddleOffsetVector.Y, destinationVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomSemiMiddle = GetCollisionVector(currentBottomSemiMiddleVector, destinationBottomSemiMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Bottom);
            Vector3 currentBottomVector = new Vector3(currentPositionVector.X + bottomOffsetVector.X, currentPositionVector.Y + bottomOffsetVector.Y, currentPositionVector.Z + bottomOffsetVector.Z);
            Vector3 destinationBottomVector = new Vector3(destinationVector.X + bottomOffsetVector.X, destinationVector.Y + bottomOffsetVector.Y, destinationVector.Z + bottomOffsetVector.Z);
            Vector3 collisionVectorBottom = GetCollisionVector(currentBottomVector, destinationBottomVector);

            float distanceFromCollisionPoint = 10000f;
            if (HasCollision(collisionVectorBottom) && bodyPartMap[BodyPart.Bottom])
            {
                float collDist = Vector3.Distance(currentBottomVector, collisionVectorBottom);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Bottom] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomOffsetVector,
                        CollisionBodyPart = BodyPart.Bottom,
                        CollisionPoint = collisionVectorBottom,
                        CollisionDistance = collDist
                    };
                }

            }
            else
            {
                bodyPartMap[BodyPart.Bottom] = false;
            }
            if (HasCollision(collisionVectorBottomSemiMiddle) && bodyPartMap[BodyPart.BottomSemiMiddle])
            {
                float collDist = Vector3.Distance(currentBottomSemiMiddleVector, collisionVectorBottomSemiMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomSemiMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomSemiMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomSemiMiddle,
                        CollisionPoint = collisionVectorBottomSemiMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomSemiMiddle] = false;
            }
            if (HasCollision(collisionVectorBottomMiddle) && bodyPartMap[BodyPart.BottomMiddle])
            {
                float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomMiddle,
                        CollisionPoint = collisionVectorBottomMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomMiddle] = false;
            }
            if (HasCollision(collisionVectorMiddle) && bodyPartMap[BodyPart.Middle])
            {
                float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Middle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = middleOffsetVector,
                        CollisionBodyPart = BodyPart.Middle,
                        CollisionPoint = collisionVectorMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Middle] = false;
            }
            if (HasCollision(collisionVectorTopMiddle) && bodyPartMap[BodyPart.TopMiddle])
            {
                float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.TopMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.TopMiddle,
                        CollisionPoint = collisionVectorTopMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.TopMiddle] = false;
            }
            if (HasCollision(collisionVectorTop) && bodyPartMap[BodyPart.Top])
            {
                float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Top] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topOffsetVector,
                        CollisionBodyPart = BodyPart.Top,
                        CollisionPoint = collisionVectorTop,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Top] = false;
            }

            return bodyPartCollisionMap;
        }
        private Vector3 CalculateNextCollisionPoint(Character target, Vector3 destinationVector)
        {
            Vector3 collisionVector = new Vector3();
            bool foundCollision = false;

            Vector3 currentPositionVector = target.CurrentPositionVector;
            MovementDirection direction = target.MovementInstruction.CurrentMovementDirection;


            Vector3 topOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, destinationVector.Y + topOffsetVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.Top))
            {
                collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);
                Thread.Sleep(5);
            }


            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, destinationVector.Y + topMiddleOffsetVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.TopMiddle))
            {
                collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);
                Thread.Sleep(5);
            }


            Vector3 middleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, destinationVector.Y + middleOffsetVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.Middle))
            {
                collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);
                Thread.Sleep(5);
            }


            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, destinationVector.Y + bottomMiddleOffsetVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.BottomMiddle))
            {
                collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);
                Thread.Sleep(5);
            }


            Vector3 bottomSemiMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomSemiMiddle);
            Vector3 currentBottomSemiMiddleVector = new Vector3(currentPositionVector.X + bottomSemiMiddleOffsetVector.X, currentPositionVector.Y + bottomSemiMiddleOffsetVector.Y, currentPositionVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 destinationBottomSemiMiddleVector = new Vector3(destinationVector.X + bottomSemiMiddleOffsetVector.X, destinationVector.Y + bottomSemiMiddleOffsetVector.Y, destinationVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomSemiMiddle = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.BottomSemiMiddle))
            {
                collisionVectorBottomSemiMiddle = GetCollisionVector(currentBottomSemiMiddleVector, destinationBottomSemiMiddleVector);
                Thread.Sleep(5);
            }


            Vector3 bottomOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Bottom);
            Vector3 currentBottomVector = new Vector3(currentPositionVector.X + bottomOffsetVector.X, currentPositionVector.Y + bottomOffsetVector.Y, currentPositionVector.Z + bottomOffsetVector.Z);
            Vector3 destinationBottomVector = new Vector3(destinationVector.X + bottomOffsetVector.X, destinationVector.Y + bottomOffsetVector.Y, destinationVector.Z + bottomOffsetVector.Z);
            Vector3 collisionVectorBottom = Vector3.Zero;
            if (target.MovementInstruction.BodyPartsToConsiderForCollision.Contains(BodyPart.Bottom))
            {
                collisionVectorBottom = GetCollisionVector(currentBottomVector, destinationBottomVector);
            }


            if (direction == MovementDirection.Downward)
            {
                collisionVector = collisionVectorBottom;
                if (HasCollision(collisionVector))
                {
                    foundCollision = true;
                    target.MovementInstruction. CharacterBodyCollisionOffsetVector = bottomOffsetVector;
                }

            }
            else if (direction == MovementDirection.Upward)
            {
                collisionVector = collisionVectorTop;
                if (HasCollision(collisionVector))
                {
                    foundCollision = true;
                    target.MovementInstruction.CharacterBodyCollisionOffsetVector = topOffsetVector;
                }
            }
            else
            {
                float minDist = 10000;
                if (HasCollision(collisionVectorBottom))
                {
                    float collDist = Vector3.Distance(currentBottomVector, collisionVectorBottom);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottom;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomOffsetVector;
                    }

                }
                if (HasCollision(collisionVectorBottomSemiMiddle))
                {
                    float collDist = Vector3.Distance(currentBottomSemiMiddleVector, collisionVectorBottomSemiMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottomSemiMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomSemiMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorBottomMiddle))
                {
                    float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottomMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorMiddle))
                {
                    float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = middleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorTopMiddle))
                {
                    float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorTopMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = topMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorTop))
                {
                    float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorTop;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = topOffsetVector;
                    }
                }
            }

            if (!foundCollision)
                target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();

            return collisionVector;
        }
        private Vector3 GetBodyPartOffsetVector(Character target, BodyPart bodyPart)
        {
            Vector3 bodyPartOffsetVector = new Vector3(-10000, -10000, -10000);
            switch (bodyPart)
            {
                case BodyPart.Bottom:
                    bodyPartOffsetVector = new Vector3(0, 0, 0);
                    break;
                case BodyPart.BottomSemiMiddle:
                    bodyPartOffsetVector = new Vector3(0, 0.75f, 0);
                    break;
                case BodyPart.BottomMiddle:
                    bodyPartOffsetVector = new Vector3(0, 1.5f, 0);
                    break;
                case BodyPart.Middle:
                    bodyPartOffsetVector = new Vector3(0, 3, 0);
                    break;
                case BodyPart.TopMiddle:
                    bodyPartOffsetVector = new Vector3(0, 4.5f, 0);
                    break;
                case BodyPart.Top:
                    bodyPartOffsetVector = new Vector3(0, 6, 0);
                    break;
            }

            return bodyPartOffsetVector;
        }
        private bool HasCollision(Vector3 collisionVector)
        {
            return !(collisionVector.X == 0f && collisionVector.Y == 0f && collisionVector.Z == 0f);
        }
        private BodyPart GetBodyPartFromOffsetVector(Vector3 bodyPartOffsetVector)
        {
            BodyPart bodyPart = BodyPart.None;
            if (bodyPartOffsetVector.Y == 0f)
                bodyPart = BodyPart.Bottom;
            else if (bodyPartOffsetVector.Y == 0.75f)
                bodyPart = BodyPart.BottomSemiMiddle;
            else if (bodyPartOffsetVector.Y == 1.5f)
                bodyPart = BodyPart.BottomMiddle;
            else if (bodyPartOffsetVector.Y == 3f)
                bodyPart = BodyPart.Middle;
            else if (bodyPartOffsetVector.Y == 4.5f)
                bodyPart = BodyPart.TopMiddle;
            else if (bodyPartOffsetVector.Y == 6f)
                bodyPart = BodyPart.Top;
            return bodyPart;
        }
        private void ApplyGravityToDestinationPoint(Character target, ref Vector3 allowableDestVector)
        {
            if (allowableDestVector.Y > 0 && !target.MovementInstruction.IsPositionAdjustedToAvoidCollision && (target.MovementInstruction.CurrentMovementDirection != MovementDirection.Upward && target.MovementInstruction.CurrentMovementDirection != MovementDirection.Downward))
            {
                Vector3 collisionGroundUp = new Vector3(allowableDestVector.X, allowableDestVector.Y + 2f, allowableDestVector.Z);
                Vector3 collisionGroundDown = new Vector3(allowableDestVector.X, -100f, allowableDestVector.Z);
                Vector3 collisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                if (collisionVectorGround.Y < allowableDestVector.Y)
                {
                    // check if ground collision result is suspicious. 
                    if (((collisionVectorGround.X == 0f && collisionVectorGround.Y == 0f && collisionVectorGround.Z == 0f) || collisionVectorGround.Y < 1f) && Vector3.Distance(allowableDestVector, collisionVectorGround) > 1.5)
                    {
                        //// rest a while and then measure again
                        //new PauseElement("", 500).Play();
                        var prevCollisionVectorGround = collisionVectorGround;
                        var newCollisionVectorGround = GetCollisionVector(collisionGroundUp, collisionGroundDown);
                        if (prevCollisionVectorGround != newCollisionVectorGround && newCollisionVectorGround.Y > prevCollisionVectorGround.Y)
                            collisionVectorGround = newCollisionVectorGround; // the calculation was wrong, so fix it
                        else
                        {
                            // CALIBRATION: further check if there is really nothing between this point and ground. To confirm, check ground collisions for four more points - 
                            // one 0.1 unit ahead, 0.1 unit back, 0.1 unit left and 0.1 unit right
                            // If any of the four does not lead to ground and has collision in between, we won't go to ground
                            Vector3 destLeft = GetDestinationVector(new Vector3(1, 0, 0), -0.5f, allowableDestVector);
                            Vector3 collisionVectorGroundLeft = GetCollisionVector(new Vector3(destLeft.X, destLeft.Y + 2, destLeft.Z), new Vector3(destLeft.X, -100f, destLeft.Z));
                            Vector3 desRight = GetDestinationVector(new Vector3(1, 0, 0), 0.5f, allowableDestVector);
                            Vector3 collisionVectorGroundRight = GetCollisionVector(new Vector3(desRight.X, desRight.Y + 2, desRight.Z), new Vector3(desRight.X, -100f, desRight.Z));
                            Vector3 destBack = GetDestinationVector(new Vector3(0, 0, 1), -0.5f, allowableDestVector);
                            Vector3 collisionVectorGroundBack = GetCollisionVector(new Vector3(destBack.X, destBack.Y + 2, destBack.Z), new Vector3(destBack.X, -100f, destBack.Z));
                            Vector3 destFront = GetDestinationVector(new Vector3(0, 0, 1), 0.5f, allowableDestVector);
                            Vector3 collisionVectorGroundFront = GetCollisionVector(new Vector3(destFront.X, destFront.Y + 2, destFront.Z), new Vector3(destFront.X, -100f, destFront.Z));

                            List<float> groundCollisionYPositionsForSurroundingPoints = new List<float> { collisionVectorGroundLeft.Y, collisionVectorGroundRight.Y, collisionVectorGroundBack.Y, collisionVectorGroundFront.Y };

                            var maxYPositionForGroundCollisionForSurroundingPoints = groundCollisionYPositionsForSurroundingPoints.Max();
                            if (maxYPositionForGroundCollisionForSurroundingPoints > collisionVectorGround.Y)
                                collisionVectorGround.Y = maxYPositionForGroundCollisionForSurroundingPoints;
                            else
                            {
                                //More Calibration: ???
                            }
                        }
                    }
                    if (collisionVectorGround.Y <= 0f)
                        allowableDestVector.Y = collisionVectorGround.Y + 0.25f;
                    else
                        allowableDestVector.Y = collisionVectorGround.Y;
                }
            }
        }

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Character target)
        {
            return GetDestinationVector(directionVector, units, target.CurrentPositionVector);
        }

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Vector3 positionVector)
        {
            Vector3 vCurrent = positionVector;
            directionVector.Normalize();
            var destX = vCurrent.X + directionVector.X * units;
            var destY = vCurrent.Y + directionVector.Y * units;
            var destZ = vCurrent.Z + directionVector.Z * units;
            Vector3 dest = new Vector3(destX, destY, destZ);
            dest = Helper.GetRoundedVector(dest, 2);
            return dest;
        }


        public Vector3 GetDirectionVector(double rotaionAngle, MovementDirection direction, Vector3 facingVector)
        {
            float vX, vY, vZ;
            double rotationAxisX = 0, rotationAxisY = 1, rotationAxisZ = 0;
            if (direction == MovementDirection.Upward)
            {
                vX = 0;
                vY = 1;
                vZ = 0;
            }
            else if (direction == MovementDirection.Downward)
            {
                vX = 0;
                vY = -1;
                vZ = 0;
            }
            else
            {
                double rotationAngleRadian = Helper.GetRadianAngle(rotaionAngle);
                double tr = 1 - Math.Sin(rotationAngleRadian);
                //a1 = (t(r) * X * X) + cos(r)
                var a1 = tr * rotationAxisX * rotationAxisX + Math.Cos(rotationAngleRadian);
                //a2 = (t(r) * X * Y) - (sin(r) * Z)
                var a2 = tr * rotationAxisX * rotationAxisY - Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //a3 = (t(r) * X * Z) + (sin(r) * Y)
                var a3 = tr * rotationAxisX * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisY;
                //b1 = (t(r) * X * Y) + (sin(r) * Z)
                var b1 = tr * rotationAxisX * rotationAxisY + Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //b2 = (t(r) * Y * Y) + cos(r)
                var b2 = tr * rotationAxisY * rotationAxisY + Math.Cos(rotationAngleRadian);
                //b3 = (t(r) * Y * Z) - (sin(r) * X)
                var b3 = tr * rotationAxisY * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c1 = (t(r) * X * Z) - (sin(r) * Y)
                var c1 = tr * rotationAxisX * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisY;
                //c2 = (t(r) * Y * Z) + (sin(r) * X)
                var c2 = tr * rotationAxisY * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c3 = (t(r) * Z * Z) + cos (r)
                var c3 = tr * rotationAxisZ * rotationAxisZ + Math.Cos(rotationAngleRadian);


                Vector3 facingVectorToDestination = facingVector;
                vX = (float)(a1 * facingVectorToDestination.X + a2 * facingVectorToDestination.Y + a3 * facingVectorToDestination.Z);
                vY = (float)(b1 * facingVectorToDestination.X + b2 * facingVectorToDestination.Y + b3 * facingVectorToDestination.Z);
                vZ = (float)(c1 * facingVectorToDestination.X + c2 * facingVectorToDestination.Y + c3 * facingVectorToDestination.Z);
            }

            return Helper.GetRoundedVector(new Vector3(vX, vY, vZ), 2);
        }

        public Vector3 GetDirectionVector(Character target)
        {
            Vector3 directionVector = new Vector3();
            switch (target.MovementInstruction.CurrentMovementDirection)
            {
                case MovementDirection.Forward:
                    directionVector = target.CurrentFacingVector;
                    break;
                case MovementDirection.Backward:
                    directionVector = target.CurrentModelMatrix.Backward;
                    directionVector.X *= -1;
                    directionVector.Y *= -1;
                    directionVector.Z *= -1;
                    break;
                case MovementDirection.Upward:
                    directionVector = target.CurrentModelMatrix.Up;
                    break;
                case MovementDirection.Downward:
                    directionVector = target.CurrentModelMatrix.Down;
                    break;
                case MovementDirection.Left:
                    directionVector = target.CurrentModelMatrix.Left;
                    break;
                case MovementDirection.Right:
                    directionVector = target.CurrentModelMatrix.Right;
                    break;
            }
            return directionVector;
        }

        public Vector3 GetDirectionVector(Character target, MovementDirection direction)
        {
            Vector3 directionVector = new Vector3();
            switch (direction)
            {
                case MovementDirection.Forward:
                    directionVector = target.CurrentFacingVector;
                    break;
                case MovementDirection.Backward:
                    directionVector = target.CurrentModelMatrix.Backward;
                    directionVector.X *= -1;
                    directionVector.Y *= -1;
                    directionVector.Z *= -1;
                    break;
                case MovementDirection.Upward:
                    directionVector = target.CurrentModelMatrix.Up;
                    break;
                case MovementDirection.Downward:
                    directionVector = target.CurrentModelMatrix.Down;
                    break;
                case MovementDirection.Left:
                    directionVector = target.CurrentModelMatrix.Left;
                    break;
                case MovementDirection.Right:
                    directionVector = target.CurrentModelMatrix.Right;
                    break;
            }

            return directionVector;
        }


        #endregion

        #region Stop-Pause-Reset

        public void PauseMovement(Character target)
        {
            if (this.characterMovementTimerDictionary != null && this.characterMovementTimerDictionary.ContainsKey(target))
            {
                target.MovementInstruction.IsMovementPaused = true;
                target.IsMoving = false;
                System.Threading.Timer timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void ResumeMovement(Character target)
        {
            if (this.characterMovementTimerDictionary != null && this.characterMovementTimerDictionary.ContainsKey(target))
            {
                target.MovementInstruction.IsMovementPaused = false;
                target.IsMoving = true;
                System.Threading.Timer timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(1, Timeout.Infinite);
            }
        }

        private void ResetMovement(Character target)
        {
            MovementMember stillMem = this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Still);
            PlayMovementMember(stillMem, target);
            target.MovementInstruction.IsMovingToDestination = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
        }

        private void ResetMovement(List<Character> targets)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            MovementMember stillMem = this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Still);
            PlayMovementMember(stillMem, targets);
            target.MovementInstruction.IsMovingToDestination = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
        }

        public void StopMovement(Character target)
        {
            if (this.characterMovementTimerDictionary != null && this.characterMovementTimerDictionary.ContainsKey(target))
            {
                System.Threading.Timer timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                this.characterMovementTimerDictionary[target] = null;
                target.IsMoving = false;
            }
        }

        #endregion

        #region Start Movement

        public void StartMovement(Character target) //
        {
            if (this.characterMovementTimerDictionary == null) //
                this.characterMovementTimerDictionary = new Dictionary<Character, System.Threading.Timer>();//
            StopMovement(target);//
            target.UnFollow();//
            target.IsMoving = true;//
            System.Threading.Timer timer = new System.Threading.Timer(timer_Elapsed, target, Timeout.Infinite, Timeout.Infinite);//
            if (this.characterMovementTimerDictionary.ContainsKey(target))//
                this.characterMovementTimerDictionary[target] = timer;//
            else
                this.characterMovementTimerDictionary.Add(target, timer);//
            target.MovementInstruction.MovementUnit = this.GetMovementUnit(target);//
            timer.Change(1, Timeout.Infinite);//
        }

        private void timer_Elapsed(object state)
        {
            Action d = async delegate ()
            {
                Character target = state as Character;
                if (target.MovementInstruction != null && !target.MovementInstruction.IsMovementPaused)
                {
                    if (target.MovementInstruction != null && target.MovementInstruction.IsMoving)
                    {
                        MovementMember movementMember = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.CurrentMovementDirection);
                        if (target.MovementInstruction.CurrentMovementDirection == target.MovementInstruction.LastMovementDirection)
                        {
                            await IncrementPosition(target, movementMember); //
                        }
                        else
                        {
                            ChangeDirectionAndIncrement(target, movementMember);
                        }
                    }
                    else if (target.MovementInstruction != null && target.MovementInstruction.IsTurning)
                    {
                        await TurnCharacter(target);//
                    }
                    else if (target.MovementInstruction != null && target.MovementInstruction.IsMovingToDestination)
                    {
                        await moveTodestination(target);
                    }
                }

            };
            System.Windows.Application.Current.Dispatcher.BeginInvoke(d);

        }

        public void StartMovement(List<Character> targets)
        {
            if (this.characterMovementTimerDictionary == null)
                this.characterMovementTimerDictionary = new Dictionary<Character, System.Threading.Timer>();//
            Character target = GetLeadingCharacterForMovement(targets);

            StopMovement(target);
            target.UnFollow();
            target.IsMoving = true;
            System.Threading.Timer timer = new System.Threading.Timer(timer_MultipleCharacters_Elapsed, targets, Timeout.Infinite, Timeout.Infinite);//
            if (this.characterMovementTimerDictionary.ContainsKey(target))
                this.characterMovementTimerDictionary[target] = timer;
            else
                this.characterMovementTimerDictionary.Add(target, timer);
            target.MovementInstruction.MovementUnit = this.GetMovementUnit(target);
            timer.Change(1, Timeout.Infinite);//
        }

        private void timer_MultipleCharacters_Elapsed(object state)
        {
            Action d = async delegate ()
            {
                List<Character> targets = state as List<Character>;
                Character target = GetLeadingCharacterForMovement(targets);
                if (target.MovementInstruction != null && !target.MovementInstruction.IsMovementPaused)
                {
                    if (target.MovementInstruction != null && target.MovementInstruction.IsMoving)
                    {
                        MovementMember movementMember = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.CurrentMovementDirection);
                        if (target.MovementInstruction.CurrentMovementDirection == target.MovementInstruction.LastMovementDirection)
                        {
                            await IncrementPosition(targets, movementMember); //
                        }
                        else
                        {
                            ChangeDirectionAndIncrement(targets, movementMember);
                        }
                    }
                    else if (target.MovementInstruction != null && target.MovementInstruction.IsTurning)
                    {
                        await TurnCharacter(targets);//
                    }
                    else if (target.MovementInstruction != null && target.MovementInstruction.IsMovingToDestination)
                    {
                        await moveTodestination(targets);
                    }
                }

            };
            System.Windows.Application.Current.Dispatcher.BeginInvoke(d);

        }

        private Character GetLeadingCharacterForMovement(List<Character> characters)
        {
            if (characters.Count > 1)
            {
                if (characters.Any(c => c.IsGangLeader))
                    return characters.First(c => c.IsGangLeader);
                else if (characters.Any(c => c.IsActive))
                    return characters.First(c => c.IsActive);
                else if (characters.Any(c => c.ActiveMovement != null))
                    return characters.First(c => c.ActiveMovement != null);
                else return characters.First();
            }
            else
                return characters.First();
        }

        #endregion

        #region Move to Destination
        private async Task moveTodestination(Character target)
        {
            if (target.MovementInstruction.DestinationVector.X != -10000f &&
                target.MovementInstruction.DestinationVector.Y != -10000f &&
                target.MovementInstruction.DestinationVector.Z != -10000f)
            {
                var dist = Vector3.Distance(target.MovementInstruction.DestinationVector, target.CurrentPositionVector);
                if (dist < 5)
                {
                    if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                    {
                        MovementMember downMem =
                            this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Downward);
                        PlayMovementMember(downMem, target);
                    }
                    this.ResetMovement(target);
                    this.StopMovement(target);
                    OnMovementFinished(this, new CustomEventArgs<Tuple<Character, Guid>> { Value = new Tuple<Character, Guid>(target, currentConfigKey) });
                }
                else
                {   
                    if (target.MovementInstruction.CurrentMovementDirection == MovementDirection.None)
                    {
                        MovementMember directionMem =
                            this.MovementMembers.First(
                                mm =>
                                    mm.MovementDirection == target.MovementInstruction.MovmementDirectionToUseForDestinationMove);
                        PlayMovementMember(directionMem, target);
                        target.MovementInstruction.CurrentMovementDirection = directionMem.MovementDirection;
                        target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                    }
                    else
                    {
                        if (!target.MovementInstruction.IsInCollision)
                        {
                            await Move(target); //
                        }
                        else
                        {
                            if (target.MovementInstruction.StopOnCollision)
                            {
                                this.ResetMovement(target);
                                this.StopMovement(target);
                                OnMovementFinished(this, new CustomEventArgs<Tuple<Character, Guid>> { Value = new Tuple<Character, Guid>(target, currentConfigKey) });
                            }
                        }
                    }
                }
                await Task.Delay(2);
                //if (this.IsPlaying)
                {
                    var timer = this.characterMovementTimerDictionary[target];
                    if (timer != null)
                        timer.Change(5, Timeout.Infinite);
                }
            }
        }

        private async Task moveTodestination(List<Character> targets)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            if (target.MovementInstruction.DestinationVector.X != -10000f &&
                target.MovementInstruction.DestinationVector.Y != -10000f &&
                target.MovementInstruction.DestinationVector.Z != -10000f)
            {
                var dist = Vector3.Distance(target.MovementInstruction.DestinationVector, target.CurrentPositionVector);
                if (dist < 5)
                {
                    if (this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                    {
                        MovementMember downMem =
                            this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Downward);
                        PlayMovementMember(downMem, targets);
                    }
                    this.ResetMovement(targets);
                    this.StopMovement(target);
                    //OnMovementFinished(this, new CustomEventArgs<Characters.Character> { Value = target });
                }
                else
                {
                    if (target.MovementInstruction.CurrentMovementDirection == MovementDirection.None)
                    {
                        MovementMember directionMem =
                            this.MovementMembers.First(
                                mm =>
                                    mm.MovementDirection == target.MovementInstruction.MovmementDirectionToUseForDestinationMove);
                        PlayMovementMember(directionMem, targets);
                        target.MovementInstruction.CurrentMovementDirection = directionMem.MovementDirection;
                        target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                    }
                    else
                    {
                        if (!target.MovementInstruction.IsInCollision)
                        {
                            if ((DateTime.UtcNow - target.MovementInstruction.MovementStartTime).Seconds > 15)
                            {
                                var xDiff = target.MovementInstruction.OriginalDestinationVector.X - target.CurrentPositionVector.X;
                                var yDiff = target.MovementInstruction.OriginalDestinationVector.Y - target.CurrentPositionVector.Y;
                                var zDiff = target.MovementInstruction.OriginalDestinationVector.Z - target.CurrentPositionVector.Z;
                                target.CurrentPositionVector = target.MovementInstruction.OriginalDestinationVector;
                                foreach (Character c in targets.Where(t => t != target))
                                {
                                    c.CurrentPositionVector = new Vector3(c.CurrentPositionVector.X + xDiff, c.CurrentPositionVector.Y + yDiff, c.CurrentPositionVector.Z + zDiff);
                                }
                                this.ResetMovement(targets);
                                this.StopMovement(target);
                                //OnMovementFinished(this, new CustomEventArgs<Characters.Character> { Value = target });
                            }
                            else
                                await Move(targets); //
                        }
                        else
                        {
                            if (target.MovementInstruction.StopOnCollision)
                            {
                                this.ResetMovement(targets);
                                this.StopMovement(target);
                                //OnMovementFinished(this, new CustomEventArgs<Characters.Character> { Value = target });
                            }
                        }
                    }
                }
                await Task.Delay(5);
                //if (this.IsPlaying)
                {
                    var timer = this.characterMovementTimerDictionary[target];
                    if (timer != null)
                        timer.Change(5, Timeout.Infinite);
                }
            }
        }

        #endregion

        #region Turn Character

        public void Turn(Character target, float angle = 5) //
        {
            Vector3 currentPositionVector = target.CurrentModelMatrix.Translation;
            Vector3 currentForwardVector = target.CurrentModelMatrix.Forward;
            Vector3 currentBackwardVector = target.CurrentModelMatrix.Backward;
            Vector3 currentRightVector = target.CurrentModelMatrix.Right;
            Vector3 currentLeftVector = target.CurrentModelMatrix.Left;
            Vector3 currentUpVector = target.CurrentModelMatrix.Up;
            Vector3 currentDownVector = target.CurrentModelMatrix.Down;
            Matrix rotatedMatrix = new Matrix();

            switch (target.MovementInstruction.CurrentRotationAxisDirection)
            {
                case MovementDirection.Upward: // Rotate against Up Axis, e.g. Y axis for a vertically aligned model
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentUpVector, (float)Helper.GetRadianAngle(angle));
                    break;
                case MovementDirection.Downward: // Rotate against Down Axis, e.g. -Y axis for a vertically aligned model
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentDownVector, (float)Helper.GetRadianAngle(angle));
                    break;
                case MovementDirection.Right: // Rotate against Right Axis, e.g. X axis for a vertically aligned model will tilt the model forward
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentRightVector, (float)Helper.GetRadianAngle(angle));
                    break;
                case MovementDirection.Left: // Rotate against Left Axis, e.g. -X axis for a vertically aligned model will tilt the model backward
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentLeftVector, (float)Helper.GetRadianAngle(angle));
                    break;
                case MovementDirection.Forward: // Rotate against Forward Axis, e.g. Z axis for a vertically aligned model will tilt the model on right side
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentForwardVector, (float)Helper.GetRadianAngle(angle));
                    break;
                case MovementDirection.Backward: // Rotate against Backward Axis, e.g. -Z axis for a vertically aligned model will tilt the model on left side
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentBackwardVector, (float)Helper.GetRadianAngle(angle));
                    break;
            }

            target.CurrentModelMatrix *= rotatedMatrix; // Apply rotation
            target.CurrentPositionVector = currentPositionVector; // Keep position intact;
            target.AlignGhost();

        }

        public void Turn(List<Character> targets, float angle = 5) //
        {
            Character mainTarget = GetLeadingCharacterForMovement(targets);
            foreach (Character target in targets)
            {
                Vector3 currentPositionVector = target.CurrentModelMatrix.Translation;
                Vector3 currentForwardVector = target.CurrentModelMatrix.Forward;
                Vector3 currentBackwardVector = target.CurrentModelMatrix.Backward;
                Vector3 currentRightVector = target.CurrentModelMatrix.Right;
                Vector3 currentLeftVector = target.CurrentModelMatrix.Left;
                Vector3 currentUpVector = target.CurrentModelMatrix.Up;
                Vector3 currentDownVector = target.CurrentModelMatrix.Down;
                Matrix rotatedMatrix = new Matrix();

                switch (mainTarget.MovementInstruction.CurrentRotationAxisDirection)
                {
                    case MovementDirection.Upward: // Rotate against Up Axis, e.g. Y axis for a vertically aligned model
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentUpVector, (float)Helper.GetRadianAngle(angle));
                        break;
                    case MovementDirection.Downward: // Rotate against Down Axis, e.g. -Y axis for a vertically aligned model
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentDownVector, (float)Helper.GetRadianAngle(angle));
                        break;
                    case MovementDirection.Right: // Rotate against Right Axis, e.g. X axis for a vertically aligned model will tilt the model forward
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentRightVector, (float)Helper.GetRadianAngle(angle));
                        break;
                    case MovementDirection.Left: // Rotate against Left Axis, e.g. -X axis for a vertically aligned model will tilt the model backward
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentLeftVector, (float)Helper.GetRadianAngle(angle));
                        break;
                    case MovementDirection.Forward: // Rotate against Forward Axis, e.g. Z axis for a vertically aligned model will tilt the model on right side
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentForwardVector, (float)Helper.GetRadianAngle(angle));
                        break;
                    case MovementDirection.Backward: // Rotate against Backward Axis, e.g. -Z axis for a vertically aligned model will tilt the model on left side
                        rotatedMatrix = Matrix.CreateFromAxisAngle(currentBackwardVector, (float)Helper.GetRadianAngle(angle));
                        break;
                }

                target.CurrentModelMatrix *= rotatedMatrix; // Apply rotation
                target.CurrentPositionVector = currentPositionVector; // Keep position intact;
                target.AlignGhost();
            }
        }

        private bool CheckIfAssociatedTurnKeysPressed(MovementDirection turnAxisDirection)
        {
            bool keyPressed = false;
            switch (turnAxisDirection)
            {
                case MovementDirection.Upward:
                    keyPressed = Keyboard.IsKeyDown(Key.Right);
                    break;
                case MovementDirection.Downward:
                    keyPressed = Keyboard.IsKeyDown(Key.Left);
                    break;
                case MovementDirection.Forward:
                    keyPressed = Keyboard.IsKeyDown(Key.Right) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                    break;
                case MovementDirection.Backward:
                    keyPressed = Keyboard.IsKeyDown(Key.Left) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                    break;
                case MovementDirection.Right:
                    keyPressed = Keyboard.IsKeyDown(Key.Up);
                    break;
                case MovementDirection.Left:
                    keyPressed = Keyboard.IsKeyDown(Key.Down);
                    break;
            }
            return keyPressed;
        }

        private async Task TurnCharacter(Character target)
        {
            bool changeDir = false;
            bool associatedKeyPressed = CheckIfAssociatedTurnKeysPressed(target.MovementInstruction.CurrentRotationAxisDirection);//
            if (associatedKeyPressed)//
            {
                Turn(target);//
            }//
            else//
            {
                MovementDirection otherDirection = MovementDirection.None;
                MovementMember otherMember = null;
                foreach (MovementMember mm in this.MovementMembers)
                {
                    if (Keyboard.IsKeyDown(mm.AssociatedKey) && mm.MovementDirection != MovementDirection.Still)
                    {
                        otherDirection = mm.MovementDirection;
                        otherMember = mm;
                        changeDir = true;
                        break;
                    }
                }
                if (otherDirection != MovementDirection.None && otherDirection != MovementDirection.Still && otherMember != null)
                {
                    target.MovementInstruction.IsMoving = true;
                    target.MovementInstruction.IsTurning = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                    // reset collision
                    target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None; // Reset turn
                    target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                    target.MovementInstruction.IsInCollision = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                    target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
                    // Play movement
                    // PlayMovementMember(otherMember, target); Jeff -  this was interfering with clean move, turn, move 
                    target.MovementInstruction.CurrentMovementDirection = otherDirection;
                    target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                }
            }

            await Task.Delay(5);
            var timer = this.characterMovementTimerDictionary[target];
            if (timer != null)
                timer.Change(changeDir ? 1 : 5, Timeout.Infinite);
        }

        private async Task TurnCharacter(List<Character> targets)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            bool changeDir = false;
            bool associatedKeyPressed = CheckIfAssociatedTurnKeysPressed(target.MovementInstruction.CurrentRotationAxisDirection);//
            if (associatedKeyPressed)//
            {
                Turn(targets);//
            }//
            else//
            {
                MovementDirection otherDirection = MovementDirection.None;
                MovementMember otherMember = null;
                foreach (MovementMember mm in this.MovementMembers)
                {
                    if (Keyboard.IsKeyDown(mm.AssociatedKey) && mm.MovementDirection != MovementDirection.Still)
                    {
                        otherDirection = mm.MovementDirection;
                        otherMember = mm;
                        changeDir = true;
                        break;
                    }
                }
                if (otherDirection != MovementDirection.None && otherDirection != MovementDirection.Still && otherMember != null)
                {
                    target.MovementInstruction.IsMoving = true;
                    target.MovementInstruction.IsTurning = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                    // reset collision
                    target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None; // Reset turn
                    target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                    target.MovementInstruction.IsInCollision = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                    target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
                    // Play movement
                    // PlayMovementMember(otherMember, target); Jeff -  this was interfering with clean move, turn, move 
                    target.MovementInstruction.CurrentMovementDirection = otherDirection;
                    target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                }
            }

            await Task.Delay(5);
            var timer = this.characterMovementTimerDictionary[target];
            if (timer != null)
                timer.Change(changeDir ? 1 : 5, Timeout.Infinite);
        }

        #endregion

        #region Core Movement - Change Direction, Increment position

        private void ChangeDirectionAndIncrement(Character target, MovementMember movementMember)
        {
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
            // Play movement
            PlayMovementMember(movementMember, target);
            target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
            target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
            var timer = this.characterMovementTimerDictionary[target];
            if (timer != null)
                timer.Change(1, Timeout.Infinite);
        }
        private void ChangeDirectionAndIncrement(List<Character> targets, MovementMember movementMember)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
            // Play movement
            if(targets.Any(t => t.PlayDefaultMovement))
            {
                PlayMovementMember(movementMember, target);
                foreach(Character movingTarget in targets.Where(t => t != target))
                {
                    var alternateMember = movingTarget.DefaultMovementToActivate.Movement.MovementMembers.First(mm => mm.MovementDirection == movementMember.MovementDirection);
                    PlayMovementMember(alternateMember, movingTarget);
                }
            }
            else
            {
                foreach (Character movingTarget in targets)
                {
                    PlayMovementMember(movementMember, movingTarget);
                }
            }
            
            target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
            target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
            var timer = this.characterMovementTimerDictionary[target];
            if (timer != null)
                timer.Change(1, Timeout.Infinite);
        }
        private async Task IncrementPosition(Character target, MovementMember movementMember)
        {
            if (!target.MovementInstruction.IsInCollision)
            {
                bool changeDir = false;
                if (target.MovementInstruction.CurrentMovementDirection != target.MovementInstruction.LastMovementDirection)
                {
                    target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                }
                Key key = movementMember.AssociatedKey;
                if (movementMember.MovementDirection != MovementDirection.Still && Keyboard.IsKeyDown(key))
                {
                    await Move(target);
                }
                else
                {
                    MovementDirection otherDirection = MovementDirection.None;
                    MovementMember otherMember = null;
                    foreach (MovementMember mm in this.MovementMembers)
                    {
                        if (Keyboard.IsKeyDown(mm.AssociatedKey) && mm.MovementDirection != MovementDirection.Still)
                        {
                            otherDirection = mm.MovementDirection;
                            otherMember = mm;
                            changeDir = true;
                            break;
                        }
                    }
                    if (otherDirection != MovementDirection.None && otherDirection != MovementDirection.Still &&
                        otherMember != null)
                    {
                        target.MovementInstruction.IsInCollision = false;
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f,
                            -10000f);
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
                        // Play movement
                        PlayMovementMember(otherMember, target);
                        target.MovementInstruction.CurrentMovementDirection = otherDirection;
                        target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                    }
                }
                await Task.Delay(5);
                var timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(changeDir ? 1 : 5, Timeout.Infinite);
            }
        }

        private async Task IncrementPosition(List<Character> targets, MovementMember movementMember)
        {
            Character target = GetLeadingCharacterForMovement(targets);
            if (!target.MovementInstruction.IsInCollision)
            {
                bool changeDir = false;
                if (target.MovementInstruction.CurrentMovementDirection != target.MovementInstruction.LastMovementDirection)
                {
                    target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                }
                Key key = movementMember.AssociatedKey;
                if (movementMember.MovementDirection != MovementDirection.Still && Keyboard.IsKeyDown(key))
                {
                    await Move(targets);
                }
                else
                {
                    MovementDirection otherDirection = MovementDirection.None;
                    MovementMember otherMember = null;
                    foreach (MovementMember mm in this.MovementMembers)
                    {
                        if (Keyboard.IsKeyDown(mm.AssociatedKey) && mm.MovementDirection != MovementDirection.Still)
                        {
                            otherDirection = mm.MovementDirection;
                            otherMember = mm;
                            changeDir = true;
                            break;
                        }
                    }
                    if (otherDirection != MovementDirection.None && otherDirection != MovementDirection.Still &&
                        otherMember != null)
                    {
                        target.MovementInstruction.IsInCollision = false;
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f,
                            -10000f);
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
                        // Play movement
                        PlayMovementMember(otherMember, targets);
                        target.MovementInstruction.CurrentMovementDirection = otherDirection;
                        target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
                    }
                }
                await Task.Delay(5);
                var timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(changeDir ? 1 : 5, Timeout.Infinite);
            }
        }

        #endregion

        #region Play Movement Members
        public void MoveStill(Character target)
        {
            MovementMember stillMovement = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Still);
            PlayMovementMember(stillMovement, target);
        }

        public void MoveStill(List<Character> targets)
        {
            MovementMember stillMovement = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Still);
            PlayMovementMember(stillMovement, targets);
        }

        public void PlayMovementMember(MovementMember movementMember, Character target)
        {
            if (movementMember != null)
            {
                if (movementMember.MemberAbility != null) // && !movementMember.MemberAbility.IsActive)
                {
                    StopMovementMember(movementMember, target);
                    movementMember.MemberAbility.Play(false, target, false, true); // always target using memory
                    BuildSupportingMovementAnimationElementsList(movementMember);
                }
            }
        }


        public void PlayMovementMember(MovementMember movementMember, List<Character> targets)
        {
            if (movementMember != null)
            {
                if (movementMember.MemberAbility != null) // && !movementMember.MemberAbility.IsActive)
                {
                    foreach (Character target in targets)
                    {
                        StopMovementMember(movementMember, target);
                        movementMember.MemberAbility.Play(false, target, false, true); // always target using memory
                    }
                    BuildSupportingMovementAnimationElementsList(movementMember);
                }
            }
        }

        #endregion

        #region Supporting Movement Member Animations e.g. Sounds

        private void BuildSupportingMovementAnimationElementsList(MovementMember movementMember)
        {
            var allAnimationList = new List<AnimationElement>();
            this.supportingAnimationElementsForMovement = new List<AnimationElement>();
            if (movementMember.MemberAbility != null && !movementMember.MemberAbility.Persistent)
            {
                if (movementMember.MemberAbility.Reference != null && movementMember.MemberAbility.Reference.AnimationElements != null && movementMember.MemberAbility.Reference.AnimationElements.Count > 0)
                {
                    allAnimationList.AddRange(movementMember.MemberAbility.Reference.GetFlattenedAnimationList());
                    if (allAnimationList.Count > 0)
                    {
                        bool foundSound = false;
                        foreach (var animationElement in allAnimationList)
                        {
                            if (animationElement is MOVElement || animationElement is FXEffectElement)
                            {
                                if (!foundSound) // if no sound elements found so far, clear pause elements
                                    supportingAnimationElementsForMovement.Clear();
                            }
                            else if (animationElement is SoundElement)
                            {
                                foundSound = true;
                                supportingAnimationElementsForMovement.Add(animationElement);
                            }
                            else
                            {
                                supportingAnimationElementsForMovement.Add(animationElement);
                            }
                        }
                    }
                }
            }
        }

        public void StopMovementMember(MovementMember movementMember, Character target)
        {
            foreach (var mm in this.MovementMembers.Where(mm => mm != movementMember)) // && mm.MemberAbility.IsActive))
            {
                mm.MemberAbility.Stop(target, true);
            }
        }

        //to do understand
        private async Task PlaySupportingMovementAnimationsAsync(Character target)
        {
            int interval = GetSupportingAnimationInterval(target);
            if (target.MovementInstruction.LastMovmentSupportingAnimationPlayTime.HasValue && target.MovementInstruction.LastMovmentSupportingAnimationPlayTime.Value.AddMilliseconds(interval) <= DateTime.UtcNow)
            {
                foreach (AnimationElement ae in this.supportingAnimationElementsForMovement)
                {
                    if (ae is SoundElement)
                    {
                        SoundElement se = ae as SoundElement;
                        se.Play(false, target);
                    }
                    else
                        ae.Play(false, target);
                }
                target.MovementInstruction.LastMovmentSupportingAnimationPlayTime = DateTime.UtcNow;
            }

            await Task.Delay(1);
        }

        private int GetSupportingAnimationInterval(Character target)
        {
            var movementSpeed = GetMovementSpeed(target);
            // for 0.5x we give 650, for 4x we give 300, the rest is divided uniformly
            // solving for y: (y-650)/(650 - 300) = (x - 0.5)/(0.5 - 4) where x is movementSpeed
            var interval = 650 - (movementSpeed - 0.5) * 100;

            return (int)interval;
        }

        #endregion

        #region Add Default Member Abilities
        private void AddDefaultMemberAbilities()
        {
            if (this.MovementMembers == null || this.MovementMembers.Count == 0)
            {
                this.MovementMembers = new ObservableCollection<MovementMember>();

                MovementMember movementMemberLeft = new MovementMember { MovementDirection = MovementDirection.Left, MemberName = "Left" };
                movementMemberLeft.MemberAbility = new ReferenceAbility("Left", null);
                movementMemberLeft.MemberAbility.DisplayName = "Left";
                this.MovementMembers.Add(movementMemberLeft);

                MovementMember movementMemberRight = new MovementMember { MovementDirection = MovementDirection.Right, MemberName = "Right" };
                movementMemberRight.MemberAbility = new ReferenceAbility("Right", null);
                movementMemberRight.MemberAbility.DisplayName = "Right";
                this.MovementMembers.Add(movementMemberRight);

                MovementMember movementMemberForward = new MovementMember { MovementDirection = MovementDirection.Forward, MemberName = "Forward" };
                movementMemberForward.MemberAbility = new ReferenceAbility("Forward", null);
                movementMemberForward.MemberAbility.DisplayName = "Forward";
                this.MovementMembers.Add(movementMemberForward);

                MovementMember movementMemberBackward = new MovementMember { MovementDirection = MovementDirection.Backward, MemberName = "Back" };
                movementMemberBackward.MemberAbility = new ReferenceAbility("Back", null);
                movementMemberBackward.MemberAbility.DisplayName = "Back";
                this.MovementMembers.Add(movementMemberBackward);

                MovementMember movementMemberUpward = new MovementMember { MovementDirection = MovementDirection.Upward, MemberName = "Up" };
                movementMemberUpward.MemberAbility = new ReferenceAbility("Up", null);
                movementMemberUpward.MemberAbility.DisplayName = "Up";
                this.MovementMembers.Add(movementMemberUpward);

                MovementMember movementMemberDownward = new MovementMember { MovementDirection = MovementDirection.Downward, MemberName = "Down" };
                movementMemberDownward.MemberAbility = new ReferenceAbility("Down", null);
                movementMemberDownward.MemberAbility.DisplayName = "Down";
                this.MovementMembers.Add(movementMemberDownward);

                MovementMember movementMemberStill = new MovementMember { MovementDirection = MovementDirection.Still, MemberName = "Still" };
                movementMemberStill.MemberAbility = new ReferenceAbility("Still", null);
                movementMemberStill.MemberAbility.DisplayName = "Still";
                this.MovementMembers.Add(movementMemberStill);
            }
        }

        #endregion
    }

    public class MovementMember : NotifyPropertyChanged
    {
        private ReferenceAbility memberAbility;
        public ReferenceAbility MemberAbility
        {
            get
            {
                return memberAbility;
            }
            set
            {
                memberAbility = value;
                OnPropertyChanged("MemberAbility");
            }
        }

        private string memberName;
        public string MemberName
        {
            get
            {
                return memberName;
            }
            set
            {
                memberName = value;
                OnPropertyChanged("MemberName");
            }
        }

        private MovementDirection movementDirection;
        public MovementDirection MovementDirection
        {
            get
            {
                return movementDirection;
            }
            set
            {
                movementDirection = value;
                OnPropertyChanged("MovementDirection");
            }
        }
        [JsonIgnore]
        public Key AssociatedKey
        {
            get
            {
                Key key = Key.None;
                switch (MovementDirection)
                {
                    case MovementDirection.Forward:
                        key = Key.W;
                        break;
                    case MovementDirection.Backward:
                        key = Key.S;
                        break;
                    case MovementDirection.Left:
                        key = Key.A;
                        break;
                    case MovementDirection.Right:
                        key = Key.D;
                        break;
                    case MovementDirection.Upward:
                        key = Key.Space;
                        break;
                    case MovementDirection.Downward:
                        key = Key.Z;
                        break;
                    case MovementDirection.Still:
                        key = Key.X;
                        break;
                }
                return key;
            }
        }

        public MovementMember Clone()
        {
            MovementMember clonedMember = new Movements.MovementMember();
            clonedMember.MemberName = this.MemberName;
            clonedMember.MemberAbility = this.MemberAbility;
            clonedMember.MovementDirection = this.MovementDirection;

            return clonedMember;
        }
    }
    public class MovementInstruction
    {
        //to do
        private object lockObj = new object();
        public bool IsMoving { get; set; }
        public bool IsTurning { get; set; }
        public bool IsMovingToDestination { get; set; }
        public Vector3 DestinationVector { get; set; }
        public Vector3 OriginalDestinationVector { get; set; }
        public Vector3 CurrentDirectionVector { get; set; }
        public MovementDirection CurrentMovementDirection { get; set; }

        private MovementDirection _lastMovementDirection = MovementDirection.Forward;
        public MovementDirection LastMovementDirection
        {
            get
            {
                return _lastMovementDirection;
            }

            set
            {
                if (this._lastMovementDirection != value)
                {
                    this._lastMovementDirection = value;
                }
            }
        }
        public MovementDirection CurrentRotationAxisDirection { get; set; }
        public MovementDirection MovmementDirectionToUseForDestinationMove { get; set; }
        public bool StopOnCollision { get; set; }

        public float MovementUnit { get; set; }

        private bool isInCollision;
        public bool IsInCollision
        {
            get
            {
                lock (lockObj)
                {
                    return isInCollision;
                }
            }
            set
            {
                lock (lockObj)
                {
                    isInCollision = value;
                }
            }
        }
        /// <summary>
        /// This is the point upto which we have performed collision calculations, and the character can move to this point in current movement direction without recalculating collision
        /// </summary>
        public Vector3 LastCollisionFreePointInCurrentDirection
        {
            get;
            set;
        }
        /// <summary>
        /// Indicates whether there is a collision in next 20 units in current direction. True means the LastCollisionFreePointInCurrentDirection is an actual collision point. False means
        /// we have only calculated upto the LastCollisionFreePointInCurrentDirection point, but it is not an actual collision
        /// </summary>
        public bool IsCollisionAhead { get; set; }
        /// <summary>
        /// The point in character body that will have collision in the current direction
        /// </summary>
        public Vector3 CharacterBodyCollisionOffsetVector
        {
            get;
            set;
        }

        public float DistanceFromCollisionFreePoint { get; set; }
        public bool IsPositionAdjustedToAvoidCollision { get; set; }
        public float DestinationPointHeightAdjustment { get; set; }
        public bool IsDestinationPointAdjusted { get; set; }

        public DateTime? LastMovmentSupportingAnimationPlayTime { get; set; }

        public bool IsMovementPaused { get; set; }

        public DateTime MovementStartTime { get; set; }
        public List<BodyPart> BodyPartsToConsiderForCollision { get; set; }
        public bool AdjustPositionToAvoidCollision { get; set; }
    }

    public class CollisionInfo
    {
        public Vector3 BodyCollisionOffsetVector { get; set; }
        public BodyPart CollisionBodyPart { get; set; }
        public float CollisionDistance { get; set; }
        public Vector3 CollisionPoint { get; set; }
        public object CollidingObject { get; set; }
    }

    public class MovementProcessor
    {
        public static IMemoryElementPosition IncrementPosition(MovementDirection direction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public static List<IMemoryElementPosition> CalculateIncrementalPositions(MovementDirection direction, Vector3 facing, Vector3 pitch)
        {
            return new List<IMemoryElementPosition>();
        }

        public static IMemoryElementPosition IncrementPositionFromInstruction(MovementInstruction instruction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }
        public static Vector3 CalculateNewFacingPitch(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public static bool ArePositionsBesideEachOther(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return false;
        }
        public static Vector3 CalculateTurn(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public static Vector3 CalculateCameraDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
