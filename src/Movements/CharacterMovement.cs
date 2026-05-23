using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace HeroVTT.Movements
{
    /// <summary>
    /// Binds a Movement to a Character — owns activation lifecycle, keyboard hooks, and camera control.
    /// Seams injected: IMovementGlobals, IKeyboardHookService.
    /// </summary>
    public class CharacterMovement : CharacterOption
    {
        private readonly IMovementGlobals _globals;
        private readonly IKeyboardHookService _keyboardHook;
        private IntPtr _hookID;

        public event EventHandler<CustomEventArgs<CharacterMovement>> MovementActivatedForGangLeader;
        public event EventHandler<CustomEventArgs<CharacterMovement>> MovementDeactivatedForGangLeader;

        [JsonConstructor]
        private CharacterMovement() { }

        public CharacterMovement(string name, Character owner, IMovementGlobals globals, IKeyboardHookService keyboardHook)
        {
            Name = name;
            Character = owner;
            _globals = globals;
            _keyboardHook = keyboardHook;
        }

        public CharacterMovement(string name, Character owner = null)
        {
            Name = name;
            Character = owner;
        }

        private bool isActive;
        [JsonIgnore]
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; OnPropertyChanged("IsActive"); }
        }

        private bool isPaused;
        [JsonIgnore]
        public bool IsPaused
        {
            get { return isPaused; }
            set
            {
                isPaused = value;
                if (value)
                    Movement.PauseMovement(Character);
                else
                {
                    Movement.ResumeMovement(Character);
                    if (_globals != null) _globals.ActiveCharacterMovement = this;
                }
                OnPropertyChanged("IsPaused");
            }
        }

        private bool isNonCombatMovement;
        public bool IsNonCombatMovement
        {
            get { return isNonCombatMovement; }
            set { isNonCombatMovement = value; OnPropertyChanged("IsNonCombatMovement"); }
        }

        private float distanceLimit = float.MinValue;
        [JsonIgnore]
        public float DistanceLimit
        {
            get { return distanceLimit; }
            set { distanceLimit = value; OnPropertyChanged("DistanceLimit"); }
        }

        private Keys activationKey;
        public Keys ActivationKey
        {
            get { return activationKey; }
            set
            {
                if (value != Keys.None && character != null && character.Movements != null)
                {
                    foreach (CharacterMovement other in character.Movements)
                    {
                        if (!ReferenceEquals(other, this) && other.activationKey == value)
                            other.activationKey = Keys.None;
                    }
                }
                activationKey = value;
                OnPropertyChanged("ActivationKey");
            }
        }

        private double movementSpeed;
        public double MovementSpeed
        {
            get { return movementSpeed == 0 ? 1 : movementSpeed; }
            set { movementSpeed = value; OnPropertyChanged("MovementSpeed"); }
        }

        private Character character;
        public Character Character
        {
            get { return character; }
            set { character = value; OnPropertyChanged("Character"); }
        }

        private Movement movement;
        public Movement Movement
        {
            get { return movement; }
            set { movement = value; OnPropertyChanged("Movement"); }
        }

        public List<Character> CharactersToMove { get; set; }

        private string optionTooltip;
        public override string OptionTooltip
        {
            get
            {
                optionTooltip = Name + "(Alt + " + ActivationKey.ToString() + ")";
                return optionTooltip;
            }
            set { optionTooltip = value; OnPropertyChanged("OptionTooltip"); }
        }

        public CharacterMovement Clone()
        {
            var clone = new CharacterMovement(Name, Character);
            clone.Movement = Movement?.Clone();
            clone.ActivationKey = ActivationKey;
            clone.MovementSpeed = MovementSpeed;
            return clone;
        }

        public void DeactivateMovement()
        {
            IsActive = false;
            Character.MovementInstruction = null;
            Character.ActiveMovement = null;
            EnableCamera(true);
            if (_keyboardHook != null) _keyboardHook.UnsetHook(_hookID);
            Movement.StopMovement(Character);
            Character.PlayDefaultMovement = false;
            if (_globals != null) _globals.ActiveCharacterMovement = null;
        }

        public void ActivateMovement()
        {
            CharacterMovement activeMovement = Character.Movements.FirstOrDefault(cm => cm != this && cm.IsActive);
            if (activeMovement != null)
                activeMovement.DeactivateMovement();

            IsActive = true;
            Character.ActiveMovement = this;
            Movement.MoveStill(Character);
            Character.MovementInstruction = CreateDefaultInstruction();
            EnableCamera(false);
            if (_keyboardHook != null)
                _hookID = _keyboardHook.SetHook(PlayMovementByKeyProc);
            if (_globals != null) _globals.ActiveCharacterMovement = this;
        }

        public void ActivateMovement(Character character)
        {
            if (Character == character) { ActivateMovement(); return; }

            if (character.Movements.Any(m => m.Name == Name))
                character.Movements.First(m => m.Name == Name).ActivateMovement();
            else
            {
                var cm = Clone();
                cm.Character = character;
                character.Movements.Add(cm);
                cm.ActivateMovement();
            }
        }

        public void ActivateMovement(List<Character> targets)
        {
            CharactersToMove = targets;
            DeactivateActiveMovementsFor(targets);

            IsActive = true;
            Character.ActiveMovement = this;
            foreach (var target in targets)
                target.DefaultMovementToActivate.Movement.MoveStill(target);

            Character.MovementInstruction = CreateCollisionAwareInstruction();
            EnableCamera(false);
            Movement.AlignFacingWithLeader(targets);

            if (_keyboardHook != null)
                _hookID = _keyboardHook.SetHook(PlayMovementForMultipleCharactersByKeyProc);
            if (_globals != null) _globals.ActiveCharacterMovement = this;
        }

        public void DeactivateMovement(Character character)
        {
            if (Character == character) { DeactivateMovement(); return; }

            var active = character.Movements.FirstOrDefault(m => m.Name == Name && m.IsActive)
                ?? character.Movements.FirstOrDefault(m => m.IsActive);
            active?.DeactivateMovement();
            character.PlayDefaultMovement = false;
        }

        public void DeactivateMovement(List<Character> targets)
        {
            CharactersToMove = null;
            DeactivateMovement();
            targets.ForEach(t => t.PlayDefaultMovement = false);
        }

        private MovementInstruction CreateDefaultInstruction()
        {
            return new MovementInstruction
            {
                IsMoving = false,
                IsTurning = false,
                IsMovingToDestination = false,
                DestinationVector = new Vector3(-10000f, -10000f, -10000f),
                LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f),
                CurrentMovementDirection = MovementDirection.None,
                CurrentRotationAxisDirection = MovementDirection.None,
                LastMovementDirection = MovementDirection.None
            };
        }

        private MovementInstruction CreateCollisionAwareInstruction()
        {
            var instruction = CreateDefaultInstruction();
            instruction.AdjustPositionToAvoidCollision = true;
            instruction.BodyPartsToConsiderForCollision = new List<BodyPart>
            {
                BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle,
                BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom
            };
            return instruction;
        }

        private void DeactivateActiveMovementsFor(List<Character> targets)
        {
            var activeMovements = targets
                .Where(t => t.Movements.Any(m => m.IsActive))
                .SelectMany(t => t.Movements.Where(m => m.IsActive));
            foreach (var cm in activeMovements)
                cm.DeactivateMovement();
        }

        private void EnableCamera(bool enable)
        {
            string cameraFileName = enable ? Constants.GAME_ENABLE_CAMERA_FILENAME : Constants.GAME_DISABLE_CAMERA_FILENAME;
            var keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, cameraFileName);
            keyBindsGenerator.CompleteEvent();
        }

        private IntPtr PlayMovementByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)hookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;

                if (wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN)
                {
                    var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                    if (IsRelevantWindow())
                        HandleMovementKeyPress(inputKey);
                }
            }
            return _keyboardHook != null
                ? _keyboardHook.CallNextHook(_hookID, nCode, wParam, lParam)
                : KeyBoardHook.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private IntPtr PlayMovementForMultipleCharactersByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)hookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;

                if (wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN)
                {
                    var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                    if (IsRelevantWindow())
                        HandleMultiCharacterKeyPress(inputKey);
                }
            }
            return _keyboardHook != null
                ? _keyboardHook.CallNextHook(_hookID, nCode, wParam, lParam)
                : KeyBoardHook.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsRelevantWindow()
        {
            IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
            uint wndProcId;
            WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
            var cohWindow = WindowsUtilities.FindWindow("CrypticWindow", null);
            var currentProcId = Process.GetCurrentProcess().Id;
            return foregroundWindow == cohWindow || currentProcId == wndProcId;
        }

        private void HandleMovementKeyPress(Key inputKey)
        {
            if (inputKey == Key.CapsLock)
            {
                TogglePause();
                return;
            }
            if (IsPaused || Character.MovementInstruction == null) return;

            if (inputKey == Key.Escape)
            {
                DeactivateMovement();
                Character.ActiveMovement = null;
            }
            else if (IsArrowKey(inputKey))
                HandleTurnInput(inputKey, Character, Movement);
            else
                HandleDirectionalInput(inputKey, Character, Movement);
        }

        private void HandleMultiCharacterKeyPress(Key inputKey)
        {
            if (inputKey == Key.CapsLock)
            {
                TogglePause();
                return;
            }
            if (IsPaused || Character.MovementInstruction == null || CharactersToMove == null) return;

            if (IsArrowKey(inputKey))
                HandleTurnInput(inputKey, Character, Movement, CharactersToMove);
            else
                HandleDirectionalInput(inputKey, Character, Movement, CharactersToMove);
        }

        private void TogglePause()
        {
            if (!IsPaused)
            {
                IsPaused = true;
                EnableCamera(true);
                IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                WindowsUtilities.SetForegroundWindow(winHandle);
            }
            else
            {
                IsPaused = false;
                EnableCamera(false);
            }
        }

        private static bool IsArrowKey(Key key)
        {
            return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down;
        }

        private static void HandleTurnInput(Key inputKey, Character character, Movement movement, List<Character> targets = null)
        {
            MovementDirection turnDirection = GetTurnAxisDirectionFromKey(inputKey);
            if (turnDirection == MovementDirection.None) return;

            character.MovementInstruction.IsMoving = false;
            character.MovementInstruction.IsMovingToDestination = false;
            character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
            character.MovementInstruction.IsTurning = true;

            if (character.MovementInstruction.CurrentRotationAxisDirection != turnDirection)
            {
                character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                character.MovementInstruction.CurrentRotationAxisDirection = turnDirection;
                if (targets != null)
                    movement.StartMovement(targets);
                else
                    movement.StartMovement(character);
            }
        }

        private static void HandleDirectionalInput(Key inputKey, Character character, Movement movement, List<Character> targets = null)
        {
            MovementDirection direction = Helper.GetMovementDirectionFromKey(inputKey);
            if (direction == MovementDirection.None) return;

            character.MovementInstruction.IsMoving = true;
            character.MovementInstruction.IsMovingToDestination = false;
            character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
            character.MovementInstruction.IsTurning = false;

            if (character.MovementInstruction.CurrentMovementDirection != direction)
            {
                character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None;
                character.MovementInstruction.LastMovementDirection = character.MovementInstruction.CurrentMovementDirection;
                character.MovementInstruction.CurrentMovementDirection = direction;
                if (targets != null)
                    movement.StartMovement(targets);
                else
                    movement.StartMovement(character);
            }
        }

        private static MovementDirection GetTurnAxisDirectionFromKey(Key key)
        {
            bool modifierKeyPresent = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            switch (key)
            {
                case Key.Up: return MovementDirection.Right;
                case Key.Down: return MovementDirection.Left;
                case Key.Left: return modifierKeyPresent ? MovementDirection.Backward : MovementDirection.Downward;
                case Key.Right: return modifierKeyPresent ? MovementDirection.Forward : MovementDirection.Upward;
                default: return MovementDirection.None;
            }
        }
    }
}
