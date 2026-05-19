using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimatedAbility : SequenceElement
    {
        [JsonConstructor]
        private AnimatedAbility() : base(string.Empty) { }

        public AnimatedAbility(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, seqType, persistent, order, owner)
        {
            this.ActivateOnKey = activateOnKey;
        }

        private Keys activateOnKey;
        public Keys ActivateOnKey
        {
            get
            {
                return activateOnKey;
            }
            set
            {
                activateOnKey = value;
                OnPropertyChanged("ActivateOnKey");
            }
        }
        private string optionTooltip;
        public override string OptionTooltip
        {
            get
            {
                if (this.ActivateOnKey == Keys.None)
                    optionTooltip = this.Name;
                else
                    optionTooltip = Name + "(Alt + Shift + " + ActivateOnKey.ToString() + ")";
                return optionTooltip;
            }

            set
            {
                optionTooltip = value;
                OnPropertyChanged("OptionTooltip");
            }
        }

        private bool isAttack;
        public bool IsAttack
        {
            get
            {
                return isAttack;
            }
            set
            {
                isAttack = value;
                if (!value)
                    this.IsAreaEffect = false;
                OnPropertyChanged("IsAttack");
            }
        }

        private bool isAreaEffect;
        public bool IsAreaEffect
        {
            get
            {
                return isAreaEffect;
            }
            set
            {
                isAreaEffect = value;
                OnPropertyChanged("IsAreaEffect");
            }
        }

        public override AnimationElement Clone()
        {
            AnimatedAbility clonedAbility = new AnimatedAbility(this.Name, Keys.None, this.SequenceType, this.Persistent);
            clonedAbility.DisplayName = this.DisplayName;
            foreach (var element in this.AnimationElements)
            {
                var clonedElement = (element as AnimationElement).Clone() as AnimationElement;
                clonedAbility.AddAnimationElement(clonedElement);
            }
            clonedAbility.animationElements = new HashedObservableCollection<AnimationElement, string>(clonedAbility.AnimationElements, x => x.Name, x => x.Order);
            clonedAbility.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(clonedAbility.animationElements);
            clonedAbility.ActivateOnKey = this.ActivateOnKey;
            clonedAbility.IsAreaEffect = this.IsAreaEffect;
            clonedAbility.IsAttack = this.IsAttack;
            clonedAbility.Name = this.Name;
            return clonedAbility;
        }

        public static string GetAppropriateAnimationName(AnimationElementType animationType, params List<AnimationElement>[] collections)
        {
            string name = "";
            switch (animationType)
            {
                case AnimationElementType.Movement:
                    name = "Mov Element";
                    break;
                case AnimationElementType.FX:
                    name = "FX Element";
                    break;
                case AnimationElementType.Pause:
                    name = "Pause Element";
                    break;
                case AnimationElementType.Sequence:
                    name = "Seq Element";
                    break;
                case AnimationElementType.Sound:
                    name = "Sound Element";
                    break;
                case AnimationElementType.Reference:
                    name = "Ref Element";
                    break;
                case AnimationElementType.LoadIdentity:
                    name = "Identity Element";
                    break;
            }

            string suffix = " 1";
            string rootName = name;
            int i = 1;
            Regex reg = new Regex(@"\d+");
            MatchCollection matches = reg.Matches(name);
            if (matches.Count > 0)
            {
                int k;
                Match match = matches[matches.Count - 1];
                if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out k))
                {
                    i = k + 1;
                    suffix = string.Format(" {0}", i);
                    rootName = name.Substring(0, match.Index).TrimEnd();
                }
            }
            string newName = rootName + suffix;
            while (collections.Any(c => c.Where(a => a.Name == newName).FirstOrDefault() != null))
            {
                suffix = string.Format(" {0}", ++i);
                newName = rootName + suffix;
            }
            return newName;
        }
    }

    public class AttackEffect : AnimatedAbility
    {
        [JsonConstructor]
        private AttackEffect() : base(string.Empty) { }
        public AttackEffect(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, activateOnKey, seqType, persistent, order, owner)
        {

        }
        public AttackEffectOption Effect
        {
            get;
            set;
        }

        public double KnockBackDistance
        {
            get;
            set;

        }
        public KnockBackOption KnockbackEffect
        {
            get;
            set;
        }

        public Movement KnockBack
        {
            get;
            set;
        }

        public void Render()
        {
            // should probably call Move() of its knockBack effects (Movements). Will be implemented later
        }
    }

    public class Attack : AnimatedAbility
    {
        #region Events

        public event EventHandler AttackInitiated;
        public void OnAttackInitiated(object sender, CustomEventArgs<Attack> e)
        {
            if (AttackInitiated != null)
                AttackInitiated(sender, e);
        }

        public event EventHandler<CustomEventArgs<Tuple<Attack, List<Character>, Guid>>> AttackCompleted;
        public void OnAttackCompleted(object sender, CustomEventArgs<Tuple<Attack, List<Character>, Guid>> e)
        {
            //this.IsExecutionInProgress = false;
            Guid configKey = e.Value.Item3;
            this.ResetExecutionInProgressFor(configKey);
            if (AttackCompleted != null)
                AttackCompleted(sender, e);
        }

        #endregion

        private Dictionary<Character, System.Threading.Timer> characterAnimationTimerDictionary = new Dictionary<Character, System.Threading.Timer>();

        [JsonConstructor]
        private Attack() : base(string.Empty) { }
        public Attack(string name, Keys activateOnKey = Keys.None, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, activateOnKey, seqType, persistent, order, owner)
        {
            this.OnHitAnimation = new AnimatedAbility(this.Name + " - OnHit", Keys.None, AnimationSequenceType.And, false, 1, this.Owner);
        }

        private AnimatedAbility onHitAnimation;
        public AnimatedAbility OnHitAnimation
        {
            get
            {
                return onHitAnimation;
            }
            set
            {
                onHitAnimation = value;
                OnPropertyChanged("OnHitAnimation");
            }
        }

        private AttackInfo attackInfo;
        [JsonIgnore]
        public AttackInfo AttackInfo
        {
            get
            {
                return attackInfo;
            }
            set
            {
                attackInfo = value;
                OnPropertyChanged("AttackInfo");
            }
        }

        private AttackEffect attackEffect;
        public AttackEffect AttackEffect
        {
            get
            {
                return attackEffect;
            }
            set
            {
                attackEffect = value;
                OnPropertyChanged("AttackEffect");
            }
        }

        private bool isActive;
        public override bool IsActive
        {
            get
            {
                return isActive || base.IsActive;
            }
            set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
        public bool IsAutoFire
        {
            get
            {
                return this.AttackInfo != null && this.AttackInfo.AttackType == AttackType.AutoFire;
            }
        }
        public bool IsHandToHand
        {
            get
            {
                return !this.IsRanged;
            }
        }
        public bool IsRanged
        {
            get
            {
                return this.AttackInfo != null && this.AttackInfo.IsRanged;
            }
        }

        public bool CanSpread
        {
            get
            {
                return this.AttackInfo != null && this.AttackInfo.CanSpread;
            }
        }
        //[JsonIgnore]
        //public bool IsExecutionInProgress
        //{
        //    get; set;
        //}
        [JsonIgnore]
        public double SpreadDistance { get; set; }
        private static List<Guid> processedConfigs = new List<Guid>();
        public bool IsExecutionCompleteFor(Guid configKey)
        {
            return processedConfigs.Contains(configKey);
        }
        public void SetExecutionCompleteFor(Guid configKey)
        {
            processedConfigs.Add(configKey);
        }
        private static List<Guid> processingConfigs = new List<Guid>();
        public bool IsExecutionInProgressFor(Guid configKey)
        {
            return processingConfigs.Contains(configKey);
        }
        public void SetExecutionInProgressFor(Guid configKey)
        {
            processingConfigs.Add(configKey);
        }
        public void ResetExecutionInProgressFor(Guid configKey)
        {
            processingConfigs.Remove(configKey);
        }
        public override void Stop(Character Target = null, bool useMemoryTargeting = false)
        {
            IsActive = false;
            // Commenting out following line so that fx does not get stopped after attack.
            //base.Stop(Target); 
            Character target = Target ?? this.Owner;
            if (IsActive)
                IsActive = false;
            if (target != null)
            {
                if (this.IsAttack) // Don't kill FXs
                {
                    foreach (IAnimationElement item in AnimationElements.Where(x => !(x is FXEffectElement) && x.IsActive))
                    {
                        item.Stop(target, useMemoryTargeting);
                    }
                }
                else // Kill everything
                {
                    foreach (IAnimationElement item in AnimationElements.Where(x => x.IsActive))
                    {
                        item.Stop(target, useMemoryTargeting);
                    }
                }
            }
            OnPropertyChanged("IsActive");
        }
        public override void DeActivate(Character Target = null)
        {
            IsActive = false;
            base.DeActivate(Target);
        }

        public string InitiateAttack(bool persistent = false, Character target = null)
        {
            var character = target ?? this.Owner;
            Stop(character);
            //if (this.Persistent || persistent)
            IsActive = true;
            // Change the costume to red color
            //character.Activate();
            // Fire event to update Roster and select target
            OnAttackInitiated(character, new CustomEventArgs<Attack> { Value = this });

            return null;
        }

        public List<Character> CalculateAreaAttackTargets(Character attackingCharacter, List<Character> potentialTargets, Guid configurationKey)
        {
            if (!this.IsAreaEffect)
                throw new InvalidOperationException();
            List<Character> attackTargets = null;
            if(this.AttackInfo != null)
            {
                attackTargets = new List<Character>();
                Vector3 attackCenter = attackingCharacter.AttackConfigurationMap[configurationKey].Item2.AttackCenterPosition;
                int range = this.AttackInfo.Range * 8;
                Vector3 directionVector = attackCenter - attackingCharacter.CurrentPositionVector;
                directionVector.Normalize();
                switch (this.AttackInfo.AttackShape)
                {
                    case AttackShape.Line:
                        Vector3 maxDestinationVector = attackCenter + directionVector * range;
                        var destinationToAttackerVector = maxDestinationVector - attackCenter;
                        destinationToAttackerVector.Normalize();
                        // Calculate points A and B to the left and right of source
                        Vector3 pointA = Helper.GetAdjacentPoint(attackCenter, directionVector, true);
                        Vector3 pointB = Helper.GetAdjacentPoint(attackCenter, directionVector, false);
                        // Calculate points C and D to left and right of target
                        Vector3 pointC = Helper.GetAdjacentPoint(maxDestinationVector, destinationToAttackerVector, false, 4);
                        Vector3 pointD = Helper.GetAdjacentPoint(maxDestinationVector, destinationToAttackerVector, true, 4);
                        foreach (Character target in potentialTargets)
                        {
                            if (Helper.IsPointWithinQuadraticRegion(pointA, pointB, pointC, pointD, target.CurrentPositionVector))
                            {
                                attackTargets.Add(target);
                            }
                        }
                        break;
                    case AttackShape.Radius:
                        foreach (Character target in potentialTargets)
                        {
                            if (Vector3.Distance(attackCenter, target.CurrentPositionVector) < range)
                            {
                                attackTargets.Add(target);
                            }
                        }
                        break;
                    case AttackShape.Cone:
                        Vector3 baseCircleCenterVector = attackCenter + directionVector * range;
                        foreach(Character target in potentialTargets)
                        {
                            if (Helper.isLyingInCone(attackCenter, baseCircleCenterVector, target.CurrentPositionVector, (float)Helper.GetRadianAngle(60)))
                            {
                                attackTargets.Add(target);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return attackTargets;
        }

        private AnimatedAbility GetHitAbility()
        {
            AnimatedAbility ability = null;
            if (this.OnHitAnimation != null && this.OnHitAnimation.AnimationElements != null && this.OnHitAnimation.AnimationElements.Count > 0)
            {
                List<AnimationElement> onhitAnimations = this.onHitAnimation.GetFlattenedAnimationList();
                if (onhitAnimations.Count > 0)
                    ability = this.OnHitAnimation;
            }
            if (ability == null)
            {
                if (Helper.GlobalCombatAbilities != null && Helper.GlobalCombatAbilities.Count > 0)
                {
                    var globalHitAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.HIT_ABITIY_NAME);
                    if (globalHitAbility != null && globalHitAbility.AnimationElements != null && globalHitAbility.AnimationElements.Count > 0)
                    {
                        ability = globalHitAbility;
                    }
                }
            }
            return ability;
        }

        public void AnimateAttack(AttackDirection direction, List<Character> attackers)
        {
            this.SetAttackDirection(this, direction);
            attackers.ForEach(attacker =>
            {
                this.SetAttackerFacing(direction, attacker);
                base.Play(false, attacker);
            }
            );
            // Reset FX direction
            this.SetAttackDirection(this, null);
        }

        public void AnimateAttack(AttackDirection direction, List<Character> attackers, bool useMemoryTargetingForAttackers)
        {
            this.SetAttackDirection(this, direction);
            attackers.ForEach(attacker =>
            {
                this.SetAttackerFacing(direction, attacker);
                base.Play(false, attacker, false, useMemoryTargetingForAttackers);
            }
            );
            // Reset FX direction
            this.SetAttackDirection(this, null);
        }

        private void SetAttackDirection(SequenceElement seqElem, AttackDirection direction)
        {
            foreach (var animation in seqElem.AnimationElements)
            {
                if (animation is FXEffectElement)
                {
                    FXEffectElement fxElement = (animation as FXEffectElement);
                    if (!fxElement.IsNonDirectional)
                        fxElement.AttackDirection = direction;
                    else
                        fxElement.AttackDirection = null;
                }
                else if (animation is SequenceElement)
                {
                    SequenceElement se = animation as SequenceElement;
                    SetAttackDirection(se, direction);
                }
                else if (animation is ReferenceAbility)
                {
                    ReferenceAbility refAbility = animation as ReferenceAbility;
                    SetAttackDirection(refAbility.Reference, direction);
                }
            }
        }

        private void SetAttackerFacing(AttackDirection direction, Character attacker)
        {
            Vector3 facingVector = new Vector3(direction.AttackDirectionX, direction.AttackDirectionY, direction.AttackDirectionZ);
            (attacker.Position as Position).SetTargetFacing(facingVector);
        }

        private AnimatedAbility GetMissAbility()
        {
            AnimatedAbility ability = null;

            if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
            {
                var globalMissAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.MISS_ABITIY_NAME);
                if (globalMissAbility != null && globalMissAbility.AnimationElements != null && globalMissAbility.AnimationElements.Count > 0)
                {
                    ability = globalMissAbility;
                }
            }
            return ability;
        }

        private List<Character> defendersForThisAttack = new List<Character>();
        private List<Character> charactersWithIncompleteKnockback = new List<Character>();
        private Dictionary<Character, List<Character>> defenderVsAttackers = new Dictionary<Characters.Character, List<Character>>();
        
        public void AnimateAttackSequence(List<Character> attackingCharacters, List<Character> defendingCharacters, Guid configurationKey)
        {
            if (this.IsExecutionCompleteFor(configurationKey))
                return;
            Action action = delegate ()
            {
                IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                WindowsUtilities.SetForegroundWindow(winHandle);

                this.SetExecutionInProgressFor(configurationKey);
                this.defendersForThisAttack = defendingCharacters;
                if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                    charactersWithIncompleteKnockback = defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack).ToList();

                if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                {
                    foreach (Character defender in defendingCharacters)
                    { 
                        if (defender.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack)
                        {
                            // Change order so that knockback attacker plays last
                            List<Character> attackersForThisDefender = new List<Characters.Character>();
                            foreach (Character attacker in attackingCharacters.Where(ac => defender.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker == ac && !ar.IsHit)))
                            {
                                if (!attackersForThisDefender.Contains(attacker))
                                    attackersForThisDefender.Add(attacker);
                            }
                            foreach (Character attacker in attackingCharacters.Where(ac => defender.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker == ac && ar.IsHit)))
                            {
                                if (!attackersForThisDefender.Contains(attacker))
                                    attackersForThisDefender.Add(attacker);
                            }
                            if (!defenderVsAttackers.ContainsKey(defender))
                                defenderVsAttackers.Add(defender, attackersForThisDefender);
                        }
                        else
                        {
                            if (!defenderVsAttackers.ContainsKey(defender))
                                defenderVsAttackers.Add(defender, attackingCharacters);
                        }
                    }
                }
                if (this.IsAreaEffect || this.IsAutoFire)
                {
                    if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                    {
                        List<Character> attackersForArea = defenderVsAttackers[defendingCharacters.First(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack)];
                        CompleteTheAttackSequence(attackersForArea, defendingCharacters, configurationKey);
                    }
                    else
                    {
                        CompleteTheAttackSequence(attackingCharacters, defendingCharacters, configurationKey);
                    }
                }
                else
                {
                    if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                    {
                        List<Character> reOrderedDefenders = new List<Character>();
                        foreach (Character defender in defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption != KnockBackOption.KnockBack))
                            reOrderedDefenders.Add(defender);
                        foreach (Character defender in defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                            reOrderedDefenders.Add(defender);
                        CompleteTheAttackSequence(attackingCharacters, reOrderedDefenders, configurationKey);
                    }
                    else
                    {
                        CompleteTheAttackSequence(attackingCharacters, defendingCharacters, configurationKey);
                    }

                }
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(action, 5);
            adex.ExecuteAsyncDelegate();
        }

        private void CompleteTheAttackSequence(List<Character> attackingCharacters, List<Character> defendingCharacters, Guid configurationKey)
        {
            if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.MoveAttackerToTarget))
            {
                Character centerTarget = defendingCharacters.FirstOrDefault(dc => !dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && dc.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget);
                if (centerTarget == null)
                    centerTarget = defendingCharacters.FirstOrDefault();
                if (centerTarget != null)
                {
                    //attackingCharacters.First().DefaultMovementToActivate.Movement.Move(attackingCharacters, centerTarget.CurrentPositionVector);
                    foreach (Character ac in attackingCharacters)
                    {
                        ac.MoveToLocation(centerTarget.CurrentPositionVector);
                    }
                    AnimateAttackSequenceWithMovement(attackingCharacters, defendingCharacters, centerTarget, configurationKey);
                }
            }
            else
            {
                if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                {
                    if (this.IsAreaEffect || this.IsAutoFire)
                    {
                        AnimateAttackSequenceWithoutMovement(attackingCharacters, defendingCharacters, configurationKey);
                    }
                    else
                    {
                        defendingCharacters.Where(dc => !dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList().ForEach(defender =>
                        {
                            List<Character> attackersForVanilla = defenderVsAttackers[defendingCharacters.First(dc => dc.Name == defender.Name)];
                            List<Character> defenders = defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget 
                                && dc.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter == defender).ToList();
                            defenders.Add(defender);
                            AnimateAttackSequenceWithoutMovement(attackersForVanilla,defenders, configurationKey);
                        });
                    }
                }
                else
                {
                    if (this.IsAreaEffect || this.IsAutoFire)
                    {
                                                                                                                                    AnimateAttackSequenceWithoutMovement(attackingCharacters, defendingCharacters, configurationKey);
                    }
                    else
                    {
                        defendingCharacters.Where(dc => !dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList().ForEach(defender =>
                        {
                            List<Character> defenders = defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget
                            && dc.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter == defender).ToList();
                            defenders.Add(defender);
                            AnimateAttackSequenceWithoutMovement(attackingCharacters, defenders, configurationKey);
                        });
                    }
                }
                if (!defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                {
                    OnAttackCompleted(null, new CustomEventArgs<Tuple<Attack, List<Character>, Guid>> { Value = new Tuple<Attack, List<Character>, Guid>(this, defendingCharacters.ToList(), configurationKey) });
                }
                    
            }
        }

        private void AnimateAttackSequence(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey, bool useMemoryTargetingForDefenders = false)
        {
            int attackDelay = 0;
            AttackDirection direction = GetAttackDirection(attackingCharacter, defendingCharacters, configurationKey);

            float distance = GetTargetDistance(attackingCharacter, defendingCharacters, configurationKey);
            PauseElement unitPauseElement = this.AnimationElements.LastOrDefault(a => a.Type == AnimationElementType.Pause && (a as PauseElement).IsUnitPause) as PauseElement;
            if (unitPauseElement != null)
            {
                DelayManager delayManager = new DelayManager(unitPauseElement);
                attackDelay = (int)delayManager.GetDelayForDistance(distance); //here
            }

            AnimateAttack(direction, new List<Character> { attackingCharacter }, true);
            AnimateAttackConsequenceForObstructingCharacters(attackingCharacter, defendingCharacters, configurationKey, attackDelay);
            System.Threading.Thread.Sleep(attackDelay); // Delay between attack and on hit animations

            List<Character> charactersWithImpacts = defendingCharacters.Where(c => !c.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && c.AttackConfigurationMap[configurationKey].Item2.KnockBackOption != KnockBackOption.KnockBack).ToList();

            if (defendingCharacters.Count == 1)
                useMemoryTargetingForDefenders = true;
            if (useMemoryTargetingForDefenders)
            {
                AnimateHitAndMissForOffCameraTargets(attackingCharacter, defendingCharacters, configurationKey);
                AnimateAttackEffectsForOffCameraTargets(attackingCharacter, defendingCharacters, configurationKey);
                AnimateAttackConsequenceForKnockbackObstacles(attackingCharacter, defendingCharacters, configurationKey);
            }
            else
            {
                AnimateHitAndMiss(attackingCharacter, defendingCharacters, configurationKey);
                AnimateAttackEffects(attackingCharacter, charactersWithImpacts, configurationKey);
            }
            // Reset FX direction
            this.SetAttackDirection(this, null);
        }


        System.Threading.Timer timerMoveThenAttack = null;
        private void AnimateAttackSequenceWithMovement(List<Character> attackingCharacters, List<Character> defendingCharacters, Character centerTarget, Guid configurationKey)
        {
            timerMoveThenAttack = new System.Threading.Timer(timerMoveThenAttack_Callback, new object[] { attackingCharacters.ToList(), defendingCharacters.ToList(), centerTarget, configurationKey }, Timeout.Infinite, Timeout.Infinite);
            timerMoveThenAttack.Change(5, Timeout.Infinite);
        }

        private bool waitingForMovementToFinish = false;
        private void timerMoveThenAttack_Callback(object state)
        {
            object[] states = state as object[];
            List<Character> attackingCharacters = states[0] as List<Character>;
            List<Character> defendingCharacters = states[1] as List<Character>;
            Character centerTarget = states[2] as Character;
            Guid configurationKey = (Guid)states[3];
            if (attackingCharacters.Any(ac => ac.IsMoving) || defendersForThisAttack.Any(dc => dc.IsMoving))
                timerMoveThenAttack.Change(50, Timeout.Infinite);
            else if (waitingForMovementToFinish)
            {
                waitingForMovementToFinish = false;
                timerMoveThenAttack.Change(Timeout.Infinite, Timeout.Infinite);
                CompleteTheAttackSequence(attackingCharacters, defendingCharacters, configurationKey);
            }
            else
            {
                waitingForMovementToFinish = false;
                timerMoveThenAttack.Change(Timeout.Infinite, Timeout.Infinite);
                //// Commenting out the following because for Area Attacks if Move to Attacker is enabled, we now want the attacker to go to each target and execute attack animations
                //if (this.IsAreaEffect)
                //{
                //    AnimateAttackSequenceWithoutMovement(attackingCharacters, defendingCharacters);
                //    OnAttackCompleted(null, new CustomEventArgs<List<Character>> { Value = defendingCharacters });
                //}
                //else
                {
                    List<Character> restOfTheDefendingCharacters = defendingCharacters.Where(dc => dc != centerTarget && !dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList();
                    if (restOfTheDefendingCharacters.Count > 0)
                    {
                        List<Character> defenders = restOfTheDefendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && dc.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter == centerTarget).ToList();
                        defenders.Add(centerTarget);
                        if (centerTarget.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && centerTarget.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack)
                        {
                            List<Character> attackersForVanilla = defenderVsAttackers[defendingCharacters.First(dc => dc.Name == centerTarget.Name)];
                            AnimateAttackSequenceWithoutMovement(attackersForVanilla, defenders, configurationKey);
                        }
                        else
                            AnimateAttackSequenceWithoutMovement(attackingCharacters, defenders, configurationKey);
                        // wait for all movements to finish, then resume rest of the attack
                        if (defendersForThisAttack.Any(dc => dc.IsMoving))
                        {
                            waitingForMovementToFinish = true;
                            timerMoveThenAttack = new System.Threading.Timer(timerMoveThenAttack_Callback, new object[] { attackingCharacters, restOfTheDefendingCharacters, centerTarget, configurationKey }, Timeout.Infinite, Timeout.Infinite);
                            timerMoveThenAttack.Change(50, Timeout.Infinite);
                        }
                        else
                            CompleteTheAttackSequence(attackingCharacters, restOfTheDefendingCharacters, configurationKey);
                    }
                    else
                    {
                        List<Character> defenders = restOfTheDefendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && dc.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter == centerTarget).ToList();
                        defenders.Add(centerTarget);
                        if (centerTarget.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && centerTarget.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack)
                        {
                            List<Character> attackersForVanilla = defenderVsAttackers[defendingCharacters.First(dc => dc.Name == centerTarget.Name)];
                            AnimateAttackSequenceWithoutMovement(attackersForVanilla, defenders, configurationKey);
                        }
                        else
                            AnimateAttackSequenceWithoutMovement(attackingCharacters, defenders, configurationKey);
                        if (!defendersForThisAttack.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack))
                            OnAttackCompleted(null, new CustomEventArgs<Tuple<Attack, List<Character>, Guid>> { Value = new Tuple<Attack, List<Character>, Guid>(this, this.defendersForThisAttack.ToList(), configurationKey) });
                    }
                }
            }
        }

        private void AnimateAttackSequenceWithoutMovement(List<Character> attackingCharacters, List<Character> defendingCharacters, Guid configurationKey)
        {
            List<Character> offCameraDefenders = defendingCharacters.Where(d => !d.IsInViewForTargeting).ToList();
            List<Character> inViewDefenders = defendingCharacters.Where(d => d.IsInViewForTargeting && !offCameraDefenders.Contains(d)).ToList();
            attackingCharacters.ForEach(ac =>
            {
                if (inViewDefenders.Count > 0)
                    AnimateAttackSequence(ac, inViewDefenders, configurationKey, false);
                if (offCameraDefenders.Count > 0)
                    AnimateAttackSequence(ac, offCameraDefenders, configurationKey, true);
            });
        }
        
        private void AnimateAttackConsequenceForObstructingCharacters(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey, int delayForPrimaryTarget)
        {
            if(defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget))
            {
                foreach(var secondaryTarget in defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget))
                {
                    var primaryTarget = defendingCharacters.FirstOrDefault(dc => dc == secondaryTarget.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter);
                    if(primaryTarget != null)
                    {
                        var distanceForPrimary = Vector3.Distance(attackingCharacter.CurrentPositionVector, primaryTarget.CurrentPositionVector);
                        var distanceForSecondary = Vector3.Distance(attackingCharacter.CurrentPositionVector, secondaryTarget.CurrentPositionVector);
                        if (distanceForSecondary < distanceForPrimary)
                        {
                            int delayForSecondayTarget = (int)(delayForPrimaryTarget * distanceForSecondary / distanceForPrimary);
                            var timerObstructionAnimation = new System.Threading.Timer(timerObstructionAnimation_Callback, new object[] { attackingCharacter, secondaryTarget, configurationKey }, Timeout.Infinite, Timeout.Infinite);
                            this.AddToAnimationTimerDictionary(secondaryTarget, timerObstructionAnimation);
                            timerObstructionAnimation.Change(delayForSecondayTarget, Timeout.Infinite);
                        }
                    }
                }
            }
        }

        private void timerObstructionAnimation_Callback(object state)
        {
            object[] attackParams = state as object[];
            Character attacker = attackParams[0] as Character;
            Character target = attackParams[1] as Character;
            Guid configurationKey = (Guid)attackParams[2];
            var timerObstructionAnimation = this.characterAnimationTimerDictionary[target];
            timerObstructionAnimation.Change(Timeout.Infinite, Timeout.Infinite);
            
            if(target.AttackConfigurationMap[configurationKey].Item2.IsHit)
            {
                var hitAbility = this.GetHitAbility();
                hitAbility.Play(false, target, false, true);
                if (target.AttackConfigurationMap[configurationKey].Item2.IsKnockedBack)
                {
                    AnimateKnockBack(attacker, new List<Character> { target }, configurationKey);
                }
                AnimateAttackEffectsForOffCameraTargets(attacker, new List<Character> { target }, configurationKey);
            }
            else
            {
                var missAbility = this.GetMissAbility();
                missAbility.Play(false, target, false, true);
            }
        }

        private void AnimateAttackConsequenceForKnockbackObstacles(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            if (defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && dc.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter.AttackConfigurationMap[configurationKey].Item2.IsKnockedBack))
            {
                foreach(var secondaryTarget in defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget))
                {
                    var primaryTarget = defendingCharacters.FirstOrDefault(dc => dc == secondaryTarget.AttackConfigurationMap[configurationKey].Item2.PrimaryTargetCharacter);
                    if(primaryTarget != null)
                    {
                        var distanceForPrimary = Vector3.Distance(attackingCharacter.CurrentPositionVector, primaryTarget.CurrentPositionVector);
                        var distanceForSecondary = Vector3.Distance(attackingCharacter.CurrentPositionVector, secondaryTarget.CurrentPositionVector);
                        if (distanceForSecondary > distanceForPrimary)
                        {
                            int obstacleHitPeriod = 0;
                            int knockbackDistance = primaryTarget.AttackConfigurationMap[configurationKey].Item2.KnockBackDistance;
                            int knockbackDistanceInVectorUnits = knockbackDistance * 8 + 5;
                            var distanceFromPrimaryToSecondary = Vector3.Distance(primaryTarget.CurrentPositionVector, secondaryTarget.CurrentPositionVector);
                            //distanceFromPrimaryToSecondary = (distanceFromPrimaryToSecondary - 5) / 8f;
                            if (knockbackDistanceInVectorUnits < 50) // 1 to 5 blocks - 1 sec
                            {
                                obstacleHitPeriod = 800;// (int)((distanceFromPrimaryToSecondary / knockbackDistance) * 1000);
                            }
                            else if (knockbackDistanceInVectorUnits < 150) // 6 o 18 blocks - 2 sec
                            {
                                obstacleHitPeriod = (int)((distanceFromPrimaryToSecondary / knockbackDistanceInVectorUnits) * 2000);
                            }
                            else // >18 blocks - 3 sec
                            {
                                obstacleHitPeriod = (int)((distanceFromPrimaryToSecondary / knockbackDistanceInVectorUnits) * 3000);
                            }

                            if (obstacleHitPeriod < 800)
                                obstacleHitPeriod = 800;

                            var timerObstructionAnimation = new System.Threading.Timer(timerObstructionAnimation_Callback, new object[] { attackingCharacter, secondaryTarget, configurationKey }, Timeout.Infinite, Timeout.Infinite);
                            this.AddToAnimationTimerDictionary(secondaryTarget, timerObstructionAnimation);
                            timerObstructionAnimation.Change(obstacleHitPeriod, Timeout.Infinite);
                        }
                    }
                }
            }
        }

        private float GetTargetDistance(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            float distance = 0;
            Character centerTargetCharacter = defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget).FirstOrDefault();
            if (centerTargetCharacter == null)
            {
                distance = GetClosestTargetDistance(attackingCharacter, defendingCharacters);
            }
            else
            {
                Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
                Vector3 vCenterTarget = new Vector3(centerTargetCharacter.Position.X, centerTargetCharacter.Position.Y, centerTargetCharacter.Position.Z);
                distance = Vector3.Distance(vAttacker, vCenterTarget);
            }
            return distance;
        }

        private float GetClosestTargetDistance(Character attackingCharacter, List<Character> defendingCharacters)
        {
            float minDistance = 0;
            Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
            foreach (Character defendingCharacter in defendingCharacters)
            {
                Vector3 vDefender = new Vector3(defendingCharacter.Position.X, defendingCharacter.Position.Y, defendingCharacter.Position.Z);
                var distance = Vector3.Distance(vAttacker, vDefender);
                minDistance = minDistance == 0 ? distance : distance < minDistance ? distance : minDistance;
            }

            return minDistance;
        }

        private AttackDirection GetAttackDirection(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            AttackDirection direction = new AttackDirection();
            AttackConfiguration attackerAttackConfiguration = attackingCharacter.AttackConfigurationMap[configurationKey].Item2;
            // Find the center of attack and then animate.
            if ((defendingCharacters == null || defendingCharacters.Count == 0) && (attackerAttackConfiguration.AttackCenterPosition == Vector3.Zero))
            {
                var targetInFacingDirection = (attackingCharacter.Position as Module.HeroVirtualTabletop.Library.ProcessCommunicator.Position).GetTargetInFacingDirection();
                direction.AttackDirectionX = targetInFacingDirection.X;
                direction.AttackDirectionY = targetInFacingDirection.Y;
                direction.AttackDirectionZ = targetInFacingDirection.Z;
            }
            else
            {
                Character centerTargetCharacter = defendingCharacters.Where(dc => dc.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget).FirstOrDefault();
                if(centerTargetCharacter == null && attackerAttackConfiguration.AttackCenterPosition != Vector3.Zero)
                {
                    Vector3 attackCenter = attackingCharacter.AttackConfigurationMap[configurationKey].Item2.AttackCenterPosition;
                    direction.AttackDirectionX = attackCenter.X;
                    direction.AttackDirectionY = attackCenter.Y;
                    direction.AttackDirectionZ = attackCenter.Z;
                }
                else if (centerTargetCharacter == null)
                {
                    var targetInFacingDirection = (attackingCharacter.Position as Module.HeroVirtualTabletop.Library.ProcessCommunicator.Position).GetTargetInFacingDirection();
                    direction.AttackDirectionX = targetInFacingDirection.X;
                    direction.AttackDirectionY = targetInFacingDirection.Y;
                    direction.AttackDirectionZ = targetInFacingDirection.Z;
                }
                else
                {
                    Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
                    Vector3 vCenterTarget = new Vector3(centerTargetCharacter.Position.X, centerTargetCharacter.Position.Y, centerTargetCharacter.Position.Z);

                    bool isCenterTargetHit = false;
                    if (centerTargetCharacter.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers)
                    {
                        isCenterTargetHit = centerTargetCharacter.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker == attackingCharacter && ar.IsHit);
                    }
                    else
                    {
                        isCenterTargetHit = centerTargetCharacter.AttackConfigurationMap[configurationKey].Item2.AttackResult == AttackResultOption.Hit;
                    }
                    if (isCenterTargetHit)
                    {
                        direction.AttackDirectionX = centerTargetCharacter.Position.X;
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 4.0f;
                        direction.AttackDirectionZ = centerTargetCharacter.Position.Z;
                    }
                    else
                    {
                        Random rand = new Random();
                        int randomOffset = rand.Next(2, 7);
                        int multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorX = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionX = centerTargetCharacter.Position.X + randomOffset * multiplyFactorX;
                        multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorY = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionY = centerTargetCharacter.Position.Y + 5.0f + randomOffset * multiplyFactorY;
                        multiplyOffset = rand.Next(11, 20);
                        int multiplyFactorZ = multiplyOffset % 2 == 0 ? 1 : -1;
                        direction.AttackDirectionZ = centerTargetCharacter.Position.Z + randomOffset * multiplyFactorZ;
                    }
                }
            }
            return direction;
        }

        private SequenceElement GetSequenceToPlay(SequenceElement sequenceElement)
        {
            SequenceElement sequenceToPlay = new SequenceElement("SequenceToPlay", AnimationSequenceType.And);
            if (sequenceElement.SequenceType == AnimationSequenceType.And)
            {
                foreach (AnimationElement element in sequenceElement.AnimationElements)
                {
                    var elementToAdd = element.Clone();
                    sequenceToPlay.AddAnimationElement(elementToAdd);
                    elementToAdd.PlayWithNext = element.PlayWithNext;// have to do this separately because playwithnext gets overwritten when adding
                }
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, sequenceElement.AnimationElements.Count);
                var element = sequenceElement.AnimationElements[chosen];
                var elementToAdd = element.Clone();
                sequenceToPlay.AddAnimationElement(elementToAdd);
                elementToAdd.PlayWithNext = element.PlayWithNext;// have to do this separately because playwithnext gets overwritten when adding
            }
            return sequenceToPlay;
        }
        

        private void AnimateHitAndMiss(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            SequenceElement hitMissSequenceElement = new SequenceElement("HitMissSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();
            // find the flattened list of hit and miss animations and their targets
            var hitAbility = this.GetHitAbility();
            var hitAbilityToPlay = this.GetSequenceToPlay(hitAbility);
            var hitAbilityFlattened = hitAbilityToPlay.GetFlattenedAnimationList();

            List<Character> hitTargets = null;
            if (!defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers))
            {
                hitTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResult == AttackResultOption.Hit && !t.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList();
            }
            else
            {
                hitTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter && ar.IsHit && !t.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget) != null).ToList();
            }

            hitTargets = hitTargets.Where(ht => !ht.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList();

            int knockbackPlaySequence = -1; // denotes the index of the animation in the flattened list after which we should play knockback if needed. This is played after the first mov element, and would mostly be played
            // after the 0th animation (which is a mov)

            // add all the hit animations in proper order
            foreach (var anim in hitAbilityFlattened)
            {
                AnimationElement hitElement = anim.Clone();
                hitElement.Name = GetAppropriateAnimationName(hitElement.Type, hitMissSequenceElement.AnimationElements.ToList());
                if ((hitElement.Type == AnimationElementType.Movement || hitElement.Type == AnimationElementType.FX) && knockbackPlaySequence < 0)
                {
                    knockbackPlaySequence = hitElement.Order;
                }
                hitMissSequenceElement.AddAnimationElement(hitElement);
                hitElement.PlayWithNext = anim.PlayWithNext;
                characterAnimationMappingDictionary.Add(hitElement, hitTargets);
            }
            if (knockbackPlaySequence < 0)
            {
                knockbackPlaySequence = 0;
            }
            InjectMissAbility(hitMissSequenceElement, characterAnimationMappingDictionary, attackingCharacter, defendingCharacters, configurationKey);

            // Now we have the flattened sequence ready with character mapping, so play each of them in proper order on respective targets
            bool playWithKnockback = defendingCharacters.Any(dc =>
                           (!dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget && !dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack) // not multiple attackers
                           ||
                           (
                               !dc.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget &&
                               dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack
                               && dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker == attackingCharacter && ar.IsHit)// this attacker hits
                               &&
                               (!(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit)) // no other attackers hit
                               ||
                               (dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit) // other hitting attackers come before this attacker
                               && dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.Attacker != attackingCharacter && ar.IsHit))
                               < dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter))))
                           )
                        );// whether we need to play knockback or not
            if (playWithKnockback)
            {
                Task knockbackTask = new Task(() => { AnimateKnockBack(attackingCharacter, defendingCharacters.Where(c => c.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack).ToList(), configurationKey); });
                Action d = delegate ()
               {
                   AnimateAttackConsequenceForKnockbackObstacles(attackingCharacter, defendingCharacters, configurationKey);
                   AnimateKnockBack(attackingCharacter, defendingCharacters.Where(c => c.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack).ToList(), configurationKey);
               };
               hitMissSequenceElement.PlayFlattenedAnimationsOnTargetsWithKnockbackMovement(characterAnimationMappingDictionary, knockbackPlaySequence, knockbackTask, d);
            }
            else
            {
                hitMissSequenceElement.PlayFlattenedAnimationsOnTargeted(characterAnimationMappingDictionary);
                //hitMissSequenceElement.Play(false, defendingCharacters[0]);
            }
        }

        private void AnimateHitAndMissForOffCameraTargets(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            var hitAbility = this.GetHitAbility();
            List<Character> hitTargets = null;
            if (!defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers))
            {
                hitTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResult == AttackResultOption.Hit).ToList();
            }
            else
            {
                hitTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter && ar.IsHit) != null).ToList();
            }

            var missAbility = this.GetMissAbility();
            List<Character> missTargets = null;
            if (!defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers))
            {
                missTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResult == AttackResultOption.Miss).ToList();
            }
            else
            {
                missTargets = defendingCharacters.Where(t =>
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter && !ar.IsHit) != null
                &&
                !(
                t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption != AttackEffectOption.None
                && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit)
                && (
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker != attackingCharacter && ar.IsHit)) <
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter))
                )
                )
                ).ToList();
            }

            bool playWithKnockback = defendingCharacters.Any(dc =>
                    (!dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack) // not multiple attackers
                    ||
                    (
                    dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers && dc.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack
                    && dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker == attackingCharacter && ar.IsHit)// this attacker hits
                    &&
                    (!(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit)) // no other attackers hit
                    ||
                    (dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit) // other hitting attackers come before this attacker
                    && dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.Attacker != attackingCharacter && ar.IsHit))
                    < dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(dc.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter))))
                    )
                    );// whether we need to play knockback or not

            foreach (Character hitTarget in hitTargets)
            {
                hitAbility.Play(false, hitTarget, false, true);
            }
            foreach (Character missTarget in missTargets)
            {
                missAbility.Play(false, missTarget, false, true);
            }

            if (playWithKnockback)
            {
                AnimateKnockBack(attackingCharacter, defendingCharacters.Where(c => c.AttackConfigurationMap[configurationKey].Item2.KnockBackOption == KnockBackOption.KnockBack).ToList(), configurationKey);
            }
        }

        private void InjectMissAbility(SequenceElement hitMissSequenceElement, Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary, Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            List<Character> missTargets = null;
            if (!defendingCharacters.Any(dc => dc.AttackConfigurationMap[configurationKey].Item2.HasMultipleAttackers))
            {
                missTargets = defendingCharacters.Where(t => t.AttackConfigurationMap[configurationKey].Item2.AttackResult == AttackResultOption.Miss).ToList();
            }
            else
            {
                missTargets = defendingCharacters.Where(t =>
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter && !ar.IsHit) != null
                &&
                !(
                t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption != AttackEffectOption.None
                && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.Any(ar => ar.Attacker != attackingCharacter && ar.IsHit)
                && (
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker != attackingCharacter && ar.IsHit)) <
                t.AttackConfigurationMap[configurationKey].Item2.AttackResults.IndexOf(t.AttackConfigurationMap[configurationKey].Item2.AttackResults.FirstOrDefault(ar => ar.Attacker == attackingCharacter))
                )
                )
                ).ToList();
            }

            missTargets = missTargets.Where(mt => !mt.AttackConfigurationMap[configurationKey].Item2.IsSecondaryTarget).ToList();

            var missAbility = this.GetMissAbility();

            //Jeff if we have one target we can look to see if it has a custom miss animation, if it does we can use that
            //how we get this working for area effect is beyond me thanks to all this crazy sequence injection majic!!!
            if (missTargets.Count == 1)
            {
                if (missTargets[0].AnimatedAbilities[Constants.MISS_ABITIY_NAME] != null)
                {
                    missAbility = missTargets[0].AnimatedAbilities[Constants.MISS_ABITIY_NAME];
                }
            }

            var missAbilityToPlay = this.GetSequenceToPlay(missAbility);
            var missAbilityFlattened = missAbilityToPlay.GetFlattenedAnimationList();
            // Now inject the miss animations where appropriate
            List<AnimationElement> prependMissAnimations = new List<AnimationElement>(); // this list will keep all the non-fx/mov elements from the miss that appear before any mov/fx in the miss
            int missAnimationInjectCurrentPosition = -1, missAnimationInjectionInitialPosition = -1;
            if (missTargets.Count > 0)
            {
                foreach (var anim in missAbilityFlattened)
                {
                    AnimationElement missElement = anim.Clone();
                    missElement.Name = AnimatedAbility.GetAppropriateAnimationName(missElement.Type, hitMissSequenceElement.AnimationElements.ToList(), prependMissAnimations);
                    characterAnimationMappingDictionary.Add(missElement, missTargets);
                    if (missAnimationInjectCurrentPosition >= 0) // already found injection position, so add to the next position
                    {
                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                    }
                    else // find appropriate position if miss animation is mov or fx, else prepend
                    {
                        if (missElement is MOVElement || missElement is FXEffectElement) // need to chain it to the first mov/fx of the hit
                        {
                            var movOrFxToChain = hitMissSequenceElement.AnimationElements.FirstOrDefault(a => a is MOVElement || a is FXEffectElement);
                            if (movOrFxToChain != null)
                            {
                                if (movOrFxToChain.PlayWithNext) // chain at the end of the playwithnext sequence
                                {
                                    movOrFxToChain = hitMissSequenceElement.AnimationElements.FirstOrDefault(a => a.Order > movOrFxToChain.Order && !(a is MOVElement || a is FXEffectElement));
                                    if (movOrFxToChain == null) // add at the end of the hit animations
                                    {
                                        movOrFxToChain = hitMissSequenceElement.AnimationElements.Last();
                                        movOrFxToChain.PlayWithNext = true;
                                        missAnimationInjectCurrentPosition = movOrFxToChain.Order;
                                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                        missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                    }
                                    else // add before this element in the hit animations
                                    {
                                        movOrFxToChain.PlayWithNext = true;
                                        missAnimationInjectCurrentPosition = movOrFxToChain.Order - 1;
                                        hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                        missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                    }

                                }
                                else // chain with the current mov/fx in the hit animation
                                {
                                    missAnimationInjectCurrentPosition = movOrFxToChain.Order;
                                    movOrFxToChain.PlayWithNext = true;
                                    hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                    missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                                }
                            }
                            else // add it to first of hit animation, we don't care as there is nothing to chain
                            {
                                hitMissSequenceElement.AddAnimationElement(missElement, ++missAnimationInjectCurrentPosition);
                                missAnimationInjectionInitialPosition = missAnimationInjectCurrentPosition;
                            }
                        }
                        else // add it to prepend list, we'll add it later
                        {
                            prependMissAnimations.Add(missElement);
                        }
                    }
                    missElement.PlayWithNext = anim.PlayWithNext;
                }

                if (prependMissAnimations.Count > 0) // add these before where we appended the first miss mov/fx
                {
                    missAnimationInjectCurrentPosition = missAnimationInjectionInitialPosition - 1;
                    foreach (AnimationElement anim in prependMissAnimations)
                    {
                        hitMissSequenceElement.AddAnimationElement(anim, ++missAnimationInjectCurrentPosition);
                    }
                }
            }
        }

        private void AnimateAttackEffects(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            SequenceElement attackEffectSequenceElement = new SequenceElement("AttackEffectSequence", AnimationSequenceType.And);
            Dictionary<AnimationElement, List<Character>> characterAnimationMappingDictionary = new Dictionary<AnimationElement, List<Character>>();

            var globalDeadAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DEAD_ABITIY_NAME);
            List<Character> deadTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dead && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dead
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalDyingAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DYING_ABILITY_NAME);
            List<Character> dyingTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dying && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dying
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalUnconciousAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.UNCONCIOUS_ABITIY_NAME);
            List<Character> unconciousTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Unconcious && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Unconcious
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalStunnedAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.STUNNED_ABITIY_NAME);
            List<Character> stunnedTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Stunned && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Stunned
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();

            List<Character> destroyedTargets = defendingCharacters.Where(t =>
            ((t.AttackConfigurationMap[configurationKey].Item2.IsDestroyed || t.AttackConfigurationMap[configurationKey].Item2.IsPartiallyDestryoed) && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            ((t.AttackConfigurationMap[configurationKey].Item2.IsDestroyed || t.AttackConfigurationMap[configurationKey].Item2.IsPartiallyDestryoed)
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();

            if (deadTargets.Count > 0 && globalDeadAbility != null)
            {
                var deadAbilityToPlay = this.GetSequenceToPlay(globalDeadAbility);
                var deadAbilityFlattened = deadAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in deadAbilityFlattened)
                {
                    AnimationElement deadAnimation = animation.Clone();
                    deadAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(deadAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(deadAnimation);
                    deadAnimation.PlayWithNext = animation.PlayWithNext;
                    characterAnimationMappingDictionary.Add(deadAnimation, deadTargets);
                }
            }

            if (dyingTargets.Count > 0 && globalDyingAbility != null)
            {
                var dyingAbilityToPlay = this.GetSequenceToPlay(globalDyingAbility);
                var dyingAbilityFlattened = dyingAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in dyingAbilityFlattened)
                {
                    AnimationElement dyingAnimation = animation.Clone();
                    dyingAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(dyingAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(dyingAnimation);
                    dyingAnimation.PlayWithNext = animation.PlayWithNext;
                    characterAnimationMappingDictionary.Add(dyingAnimation, dyingTargets);
                }
            }

            if (unconciousTargets.Count > 0 && globalUnconciousAbility != null)
            {
                var unconciousAbilityToPlay = this.GetSequenceToPlay(globalUnconciousAbility);
                var unconciousAbilityFlattened = unconciousAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in unconciousAbilityFlattened)
                {
                    AnimationElement unconciousAnimation = animation.Clone();
                    unconciousAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(unconciousAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(unconciousAnimation);
                    unconciousAnimation.PlayWithNext = animation.PlayWithNext;
                    characterAnimationMappingDictionary.Add(unconciousAnimation, unconciousTargets);
                }

            }

            if (stunnedTargets.Count > 0 && globalStunnedAbility != null)
            {
                var stunnedAbilityToPlay = this.GetSequenceToPlay(globalStunnedAbility);
                var stunnedAbilityFlattened = stunnedAbilityToPlay.GetFlattenedAnimationList();
                foreach (var animation in stunnedAbilityFlattened)
                {
                    AnimationElement stunnedAnimation = animation.Clone();
                    stunnedAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(stunnedAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                    attackEffectSequenceElement.AddAnimationElement(stunnedAnimation);
                    stunnedAnimation.PlayWithNext = animation.PlayWithNext;
                    characterAnimationMappingDictionary.Add(stunnedAnimation, stunnedTargets);
                }
            }

            if(destroyedTargets.Count > 0)
            {
                foreach(var destroyedTarget in destroyedTargets)
                {
                    var destroyedAbility = destroyedTarget.AnimatedAbilities.FirstOrDefault(aa => aa.Name == "Explode" || aa.Name == "Dead");
                    if(destroyedAbility != null)
                    {
                        var destroyedAbilityToPlay = this.GetSequenceToPlay(destroyedAbility);
                        var destroyedAbilityFlattened = destroyedAbilityToPlay.GetFlattenedAnimationList();
                        foreach (var animation in destroyedAbilityFlattened)
                        {
                            AnimationElement destroyedAnimation = animation.Clone();
                            destroyedAnimation.Name = AnimatedAbility.GetAppropriateAnimationName(destroyedAnimation.Type, attackEffectSequenceElement.AnimationElements.ToList());
                            attackEffectSequenceElement.AddAnimationElement(destroyedAnimation);
                            destroyedAnimation.PlayWithNext = animation.PlayWithNext;
                            characterAnimationMappingDictionary.Add(destroyedAnimation, new List<Character> { destroyedTarget });
                        }
                    }
                }
            }

            // now make all MOVs and FXs to play with next, except the last one so that they play together
            AnimationElement lastMovOrFx = null;
            foreach (var movOrFxElement in attackEffectSequenceElement.AnimationElements.Where(a => a.Type == AnimationElementType.FX || a.Type == AnimationElementType.Movement))
            {
                movOrFxElement.PlayWithNext = true;
                lastMovOrFx = movOrFxElement;
            }
            if (lastMovOrFx != null)
                lastMovOrFx.PlayWithNext = false;

            attackEffectSequenceElement.PlayFlattenedAnimationsOnTargeted(characterAnimationMappingDictionary);
        }

        private void AnimateAttackEffectsForOffCameraTargets(Character attackingCharacter, List<Character> defendingCharacters, Guid configurationKey)
        {
            var globalDeadAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DEAD_ABITIY_NAME);
            List<Character> deadTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dead && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dead
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalDyingAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.DYING_ABILITY_NAME);
            List<Character> dyingTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dying && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Dying
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalUnconciousAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.UNCONCIOUS_ABITIY_NAME);
            List<Character> unconciousTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Unconcious && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Unconcious
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            var globalStunnedAbility = Helper.GlobalCombatAbilities.FirstOrDefault(a => a.Name == Constants.STUNNED_ABITIY_NAME);
            List<Character> stunnedTargets = defendingCharacters.Where(t =>
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Stunned && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
            ||
            (t.AttackConfigurationMap[configurationKey].Item2.AttackEffectOption == AttackEffectOption.Stunned
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
            && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
            )
            ).ToList();
            List<Character> destroyedTargets = defendingCharacters.Where(t =>
           ((t.AttackConfigurationMap[configurationKey].Item2.IsDestroyed || t.AttackConfigurationMap[configurationKey].Item2.IsPartiallyDestryoed) && t.AttackConfigurationMap[configurationKey].Item2.AttackResults == null)
           ||
           ((t.AttackConfigurationMap[configurationKey].Item2.IsDestroyed || t.AttackConfigurationMap[configurationKey].Item2.IsPartiallyDestryoed)
           && t.AttackConfigurationMap[configurationKey].Item2.AttackResults != null
           && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit) != null
           && t.AttackConfigurationMap[configurationKey].Item2.AttackResults.LastOrDefault(ar => ar.IsHit).Attacker == attackingCharacter
           )
           ).ToList();

            if (deadTargets.Count > 0 && globalDeadAbility != null)
            {
                foreach (Character dead in deadTargets)
                {
                    globalDeadAbility.Play(false, dead, false, true);
                }
            }

            if (dyingTargets.Count > 0 && globalDyingAbility != null)
            {
                foreach (Character dying in dyingTargets)
                {
                    globalDyingAbility.Play(false, dying, false, true);
                }
            }

            if (unconciousTargets.Count > 0 && globalUnconciousAbility != null)
            {
                foreach (Character unconcious in unconciousTargets)
                {
                    globalUnconciousAbility.Play(false, unconcious, false, true);
                }
            }

            if (stunnedTargets.Count > 0 && globalStunnedAbility != null)
            {
                foreach (Character stunned in stunnedTargets)
                {
                    globalStunnedAbility.Play(false, stunned, false, true);
                }
            }

            if (destroyedTargets.Count > 0)
            {
                foreach (var destroyedTarget in destroyedTargets)
                {
                    var destroyedAbility = destroyedTarget.AnimatedAbilities.FirstOrDefault(aa => aa.Name == "Explode" || aa.Name == "Dead" || aa.Name == "Destroy");
                    if (destroyedAbility != null)
                    {
                        destroyedAbility.Play(false, destroyedTarget, false, true);
                    }
                }
            }
        }

        private void AnimateKnockBack(Character attackingCharacter, List<Character> knockedBackCharacters, Guid configurationKey)
        {
            Character centerTargetCharacter = knockedBackCharacters.FirstOrDefault(kbc => kbc.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget);
            foreach (Character character in knockedBackCharacters)
            {
                float knockbackDistance = character.AttackConfigurationMap[configurationKey].Item2.KnockBackDistance * 8f + 5;
                if (knockbackDistance > 0)
                {
                    //PlayKnockBackWithoutMovement(attackingCharacter, character, knockbackDistance);
                    var knockbackMovement = Helper.GlobalMovements.FirstOrDefault(cm => cm.Name == Constants.KNOCKBACK_MOVEMENT_NAME);
                    knockbackMovement.MovementSpeed = 2.5;
                    if (knockbackMovement != null)
                    {
                        Vector3 attackerVector = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
                        Vector3 targetVector = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
                        if(attackerVector == targetVector)
                        {
                            attackerVector.X -= 0.1f;
                            attackerVector.Z -= 0.1f;
                        }
                        Vector3 directionVector = targetVector - attackerVector;
                        if (!character.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget)
                        {
                            if (centerTargetCharacter != null)
                            {
                                directionVector = targetVector - centerTargetCharacter.CurrentPositionVector;
                            }
                        }
                        directionVector.Normalize();
                        var destX = targetVector.X + directionVector.X * knockbackDistance;
                        var destY = targetVector.Y + directionVector.Y * knockbackDistance;
                        var destZ = targetVector.Z + directionVector.Z * knockbackDistance;
                        if (Math.Abs(targetVector.X - destX) < 1)
                            destX = targetVector.X;
                        if (Math.Abs(targetVector.Y - destY) < 1)
                            destY = targetVector.Y;
                        if (Math.Abs(targetVector.Z - destZ) < 1)
                            destZ = targetVector.Z;
                        if(float.IsNaN(destX) || float.IsNaN(destY) || float.IsNaN(destZ))
                        {

                        }
                        Vector3 destVector = new Vector3(destX, destY, destZ);
                        knockbackMovement.Movement.MovementFinished -= this.OnKnockbackCompleted;
                        knockbackMovement.Movement.MovementFinished += this.OnKnockbackCompleted;
                        if (character.AttackConfigurationMap[configurationKey].Item2.IsCenterTarget || centerTargetCharacter == null)
                            knockbackMovement.Movement.MoveBack(character, attackerVector, destVector, configurationKey);
                        else
                            knockbackMovement.Movement.MoveBack(character, centerTargetCharacter.CurrentPositionVector, destVector, configurationKey);
                    }
                }
            }
        }

       private void OnKnockbackCompleted(object sender, CustomEventArgs<Tuple<Character, Guid>> e)
       {
            Character character = e.Value.Item1;
            Guid configKey = e.Value.Item2;
            if(charactersWithIncompleteKnockback.Contains(character)) 
                charactersWithIncompleteKnockback.Remove(character);
            if(charactersWithIncompleteKnockback.Count == 0)
                OnAttackCompleted(null, new CustomEventArgs<Tuple<Attack, List<Character>, Guid>> { Value = new Tuple<Attack, List<Character>, Guid>(this, this.defendersForThisAttack.ToList(), configKey) });
        }
        public override void Play(bool persistent = false, Character target = null, bool playAsSequence = false, bool useMemoryTargeting = false)
        {
            if (this.IsAttack && !playAsSequence)
            {
                this.InitiateAttack(persistent, target);
                Action d = delegate ()
                {
                    IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                    WindowsUtilities.SetForegroundWindow(winHandle);
                };

                AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 150);
                adex.ExecuteAsyncDelegate();
            }
            else
                base.Play(persistent, target, playAsSequence, useMemoryTargeting);
        }

        private void AddToAnimationTimerDictionary(Character character, System.Threading.Timer timer)
        {
            if (this.characterAnimationTimerDictionary.ContainsKey(character))
            {
                //this.characterAnimationTimerDictionary[character].Change(Timeout.Infinite, Timeout.Infinite);
                //this.characterAnimationTimerDictionary[character] = null;
                this.characterAnimationTimerDictionary[character] = timer;
            }
            else
                this.characterAnimationTimerDictionary.Add(character, timer);
        }

        #region old knockback code

        private void PlayKnockBackWithoutMovement(Character attackingCharacter, Character targetCharacter, int knockbackDistance)
        {
            Vector3 vAttacker = new Vector3(attackingCharacter.Position.X, attackingCharacter.Position.Y, attackingCharacter.Position.Z);
            Vector3 vTarget = new Vector3(targetCharacter.Position.X, targetCharacter.Position.Y, targetCharacter.Position.Z);
            Vector3 directionVector = vTarget - vAttacker;
            directionVector.Normalize();
            var destX = vTarget.X + directionVector.X * knockbackDistance;
            var destY = vTarget.Y + directionVector.Y * knockbackDistance;
            var destZ = vTarget.Z + directionVector.Z * knockbackDistance;
            if (Math.Abs(vTarget.X - destX) < 1)
                destX = vTarget.X;
            if (Math.Abs(vTarget.Y - destY) < 1)
                destY = vTarget.Y;
            if (Math.Abs(vTarget.Z - destZ) < 1)
                destZ = vTarget.Z;
            var collisionInfo = IconInteractionUtility.GetCollisionInfo(vTarget.X, vTarget.Y, vTarget.Z, destX, destY, destZ);
            Vector3 targetPosition = Helper.GetCollisionVector(collisionInfo);
            if (targetPosition.X == 0 && targetPosition.Y == 0 && targetPosition.Z == 0)
            {
                targetCharacter.Position.X = destX;
                targetCharacter.Position.Y = destY;
                targetCharacter.Position.Z = destZ;
            }
            else
            {
                targetCharacter.Position.X = targetPosition.X;
                targetCharacter.Position.Y = targetPosition.Y;
                targetCharacter.Position.Z = targetPosition.Z;
            }
        }

        #endregion

        public override AnimationElement Clone()
        {
            Attack clonedAttack = new Attack(this.Name, Keys.None, this.SequenceType, this.Persistent);
            clonedAttack.DisplayName = this.DisplayName;
            foreach (var element in this.AnimationElements)
            {
                var clonedElement = (element as AnimationElement).Clone() as AnimationElement;
                clonedAttack.AddAnimationElement(clonedElement);
            }
            clonedAttack.animationElements = new HashedObservableCollection<AnimationElement, string>(clonedAttack.AnimationElements, x => x.Name, x => x.Order);
            clonedAttack.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(clonedAttack.animationElements);
            clonedAttack.ActivateOnKey = this.ActivateOnKey;
            clonedAttack.IsAreaEffect = this.IsAreaEffect;
            clonedAttack.IsAttack = this.IsAttack;
            clonedAttack.Name = this.Name;
            clonedAttack.OnHitAnimation = this.OnHitAnimation.Clone() as AnimatedAbility;
            return clonedAttack;
        }
    }
    public class AttackConfiguration : NotifyPropertyChanged
    {
        private bool isCenterTarget;
        public bool IsCenterTarget
        {
            get
            {
                return isCenterTarget;
            }
            set
            {
                isCenterTarget = value;
                OnPropertyChanged("IsCenterTarget");
            }
        }

        public Vector3 AttackCenterPosition
        {
            get;set;
        }

        private AttackMode attackMode;
        public AttackMode AttackMode // None/Attack/Defend
        {
            get
            {
                return attackMode;
            }
            set
            {
                attackMode = value;
                OnPropertyChanged("AttackMode");
            }
        }

        private KnockBackOption knockBackOption; // Knockback/KnockDown
        public KnockBackOption KnockBackOption
        {
            get
            {
                return knockBackOption;
            }
            set
            {
                knockBackOption = value;
                OnPropertyChanged("KnockBackOption");
            }
        }
        private AttackResultOption attackResult;
        public AttackResultOption AttackResult // Miss/Hit
        {
            get
            {
                return attackResult;
            }
            set
            {
                attackResult = value;
                OnPropertyChanged("AttackResult");
            }
        }

        private ObservableCollection<AttackResult> attackResults;
        public ObservableCollection<AttackResult> AttackResults
        {
            get
            {
                return attackResults;
            }
            set
            {
                attackResults = value;
                OnPropertyChanged("AttackResults");
                OnPropertyChanged("HasMultipleAttackers");
            }
        }
        public bool HasMultipleAttackers
        {
            get
            {
                return AttackResults != null && AttackResults.Count > 0; ;
            }
        }

        private AttackEffectOption attackEffectOption;
        public AttackEffectOption AttackEffectOption // None/Stunned/Unconcious/Dying/Dead
        {
            get
            {
                return attackEffectOption;
            }
            set
            {
                attackEffectOption = value;
                OnPropertyChanged("AttackEffectOption");
            }
        }

        private int knockBackDistance;
        public int KnockBackDistance
        {
            get
            {
                return knockBackDistance;
            }
            set
            {
                knockBackDistance = value;
                OnPropertyChanged("KnockBackDistance");
            }
        }

        private bool isHit;
        public bool IsHit
        {
            get
            {
                return isHit;
            }
            set
            {
                isHit = value;
                OnPropertyChanged("IsHit");
            }
        }

        private bool isKnockedBack;
        public bool IsKnockedBack
        {
            get
            {
                return isKnockedBack;
            }
            set
            {
                isKnockedBack = value;
                OnPropertyChanged("IsKnockedBack");
            }
        }

        private bool isStunned;
        public bool IsStunned
        {
            get
            {
                return isStunned;
            }
            set
            {
                isStunned = value;
                OnPropertyChanged("IsStunned");
            }
        }

        private bool isUnconcious;
        public bool IsUnconcious
        {
            get
            {
                return isUnconcious;
            }
            set
            {
                isUnconcious = value;
                OnPropertyChanged("IsUnconcious");
            }
        }

        private bool isDying;
        public bool IsDying
        {
            get
            {
                return isDying;
            }
            set
            {
                isDying = value;
                OnPropertyChanged("IsDying");
            }
        }

        private bool isDead;
        public bool IsDead
        {
            get
            {
                return isDead;
            }
            set
            {
                isDead = value;
                OnPropertyChanged("IsDead");
            }
        }

        private int numberOfShotsAssigned;
        public int NumberOfShotsAssigned
        {
            get
            {
                return numberOfShotsAssigned;
            }
            set
            {
                numberOfShotsAssigned = value;
                OnPropertyChanged("NumberOfShotsAssigned");
            }
        }

        public bool IsDestroyed { get; set; }
        public bool IsPartiallyDestryoed { get; set; }
        public bool IsSecondaryTarget { get { return PrimaryTargetCharacter != null; } }
        public Character PrimaryTargetCharacter { get; set; }

        private bool moveAttackerToTarget;
        public bool MoveAttackerToTarget
        {
            get
            {
                return moveAttackerToTarget;
            }
            set
            {
                moveAttackerToTarget = value;
                OnPropertyChanged("MoveAttackerToTarget");
            }
        }

        public int? Stun { get; set; }
        public int? Body { get; set; }
        public List<Character> ObstructingCharacters { get; set; }
        public bool IsKnockbackObstruction { get; set; }
    }
    public class AttackDirection
    {
        public float AttackDirectionX
        {
            get;
            set;
        }
        public float AttackDirectionY
        {
            get;
            set;
        }
        public float AttackDirectionZ
        {
            get;
            set;
        }
        public AttackDirection()
        {

        }
        public AttackDirection(Vector3 direction)
        {
            this.AttackDirectionX = direction.X;
            this.AttackDirectionY = direction.Y;
            this.AttackDirectionZ = direction.Z;
        }
    }

    public class AttackResult : NotifyPropertyChanged
    {
        private Character attacker;
        public Character Attacker
        {
            get
            {
                return attacker;
            }
            set
            {
                attacker = value;
                OnPropertyChanged("Attacker");
            }
        }

        private AttackResultOption attackResultOption;
        public AttackResultOption AttackResultOption
        {
            get
            {
                return attackResultOption;
            }
            set
            {
                attackResultOption = value;
                OnPropertyChanged("AttackResultOption");
            }
        }
        private bool isHit;
        public bool IsHit
        {
            get
            {
                return isHit;
            }
            set
            {
                isHit = value;
                OnPropertyChanged("IsHit");
            }
        }
    }

    public class AttackInfo : NotifyPropertyChanged
    {
        public AttackType AttackType { get; set; }
        public int Range { get; set; }
        public AttackShape AttackShape { get; set; }
        public bool TargetSelective { get; set; }
        public bool IsRanged { get; set; }
        public bool CanSpread { get; set; }
        private int autoFireMaxShots;
        public int AutoFireMaxShots
        {
            get
            {
                return autoFireMaxShots;
            }
            set
            {
                autoFireMaxShots = value;
                OnPropertyChanged("AutoFireMaxShots");
            }
        }
    }

    public class AttackTarget : NotifyPropertyChanged
    {
        private Character defender;
        public Character Defender
        {
            get
            {
                return defender;
            }
            set
            {
                defender = value;
                OnPropertyChanged("Defender");
            }
        }
        private bool targeted;
        public bool Targeted
        {
            get
            {
                return targeted;
            }
            set
            {
                targeted = value;
                OnPropertyChanged("Targeted");
            }
        }
    }

    public class DefenderActiveAttackConfiguration : NotifyPropertyChanged
    {
        private Character defender;
        public Character Defender
        {
            get
            {
                return defender;
            }
            set
            {
                defender = value;
                OnPropertyChanged("Defender");
            }
        }
        public AttackConfiguration attackConfiguration;
        public AttackConfiguration ActiveAttackConfiguration
        {
            get
            {
                return attackConfiguration;
            }
            set
            {
                attackConfiguration = value;
                OnPropertyChanged("ActiveAttackConfiguration");
            }
        }
    }

    public enum AttackType
    {
        None,
        Vanilla,
        Area,
        AutoFire
    }
    public enum AttackShape
    {
        None,
        Line,
        Radius,
        Cone
    }
}
