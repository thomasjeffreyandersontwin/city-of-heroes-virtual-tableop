using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.HCSIntegration
{
    public class CombatantsCollection
    {
        public List<Combatant> Combatants { get; set; }
    }

    public class Combatant : NotifyPropertyChanged, IComparable
    {
        public string Phase { get; set; }
        [JsonProperty("Character")]
        public string CharacterName { get; set; }
        [JsonIgnore]
        public Character CombatantCharacter { get; set; }
        [JsonIgnore]
        public int Order { get; set; }
        [JsonIgnore]
        private bool isActivePhase;
        public bool IsActivePhase
        {
            get
            {
                return isActivePhase;
            }
            set
            {
                isActivePhase = value;
                OnPropertyChanged("IsActivePhase");
            }
        }

        public int CompareTo(object cmb)
        {
            Combatant cmb2 = cmb as Combatant;
            string s1 = this.Phase;
            string s2 = cmb2.Phase;
            if (this.Phase == cmb2.Phase)
            {
                s1 = this.CharacterName;
                s2 = cmb2.CharacterName;
            }
            else if (this.Phase == "Not Found")
            {
                s2 = "_";
            }
            else if (cmb2.Phase == "Not Found")
            {
                s1 = "_";
            }

            return Helper.CompareStrings(s1, s2);
        }
    }

    public class ActiveCharacterInfo
    {
        public string Name { get; set; }
        public object Perks { get; set; }
        public object Disadvantages { get; set; }
        public object Talents { get; set; }
        public object Equipment { get; set; }
        public object Skills { get; set; }
        public object Defenses { get; set; }
        public object Stats { get; set; }
        public object Powers { get; set; }
        public object Defaults { get; set; }
        public object Effects { get; set; }
        [JsonProperty("States")]
        public CharacterStates CharacterStates { get; set; }
        [JsonIgnore]
        public  List<AbilityActivationEligibility> AbilitiesEligibilityCollection { get; set; }
    }

    public class CharacterStates
    {
        [JsonProperty("Is Holding For Dex")]
        public bool? IsHoldingForDex { get; set; }
        [JsonProperty("Is Dying")]
        public bool? IsDying { get; set; }
        [JsonProperty("Is Alive")]
        public bool? IsAlive { get; set; }
        [JsonProperty("Is Abortable")]
        public bool? IsAbortable { get; set; }
        [JsonProperty("Is Unconsious")] 
        public bool? IsUnconscious { get; set; }
        [JsonProperty("Is Dead")]
        public bool? IsDead { get; set; }
        [JsonProperty("Is Stunned")]
        public bool? IsStunned { get; set; }
    }

    public class Chronometer
    {
        public string CurrentPhase { get; set; }
    }

    public class AbilityActivationEligibility
    {
        public string AbilityName { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class SimpleAbility
    {
        public string Type { get; set; }
        public string Ability { get; set; }
        public string Character { get; set; }
    }

    public class SimpleMovement
    {
        public string Type { get; set; }
        public string Movement { get; set; }
        public int Distance { get; set; }
    }

    public class AttackRequestBase
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public string Ability { get; set; }
        public int? SpreadDistance { get; set; }
        public List<string> Obstructions { get; set; }
        [JsonProperty("Potential Knockback Collisions")]
        public List<PotentialKnockbackCollision> PotentialKnockbackCollisions { get; set; }
    }

    public class AttackRequest : AttackRequestBase
    {
        public string Defender { get; set; }
        public int? Range { get; set; }
        public int? PushedStr { get; set; }
        public int? Generic { get; set; }
        [JsonProperty("Off Hand")]
        public bool? OffHand { get; set; }
        [JsonProperty("Unfamiliar Weapon")]
        public bool? UnfamiliarWeapon { get; set; }
        public int? Encumbrance { get; set; }
        public bool? Surprised { get; set; }
        [JsonProperty("Targeting Sense")]
        public string TargetingSense { get; set; }
        [JsonProperty("To Hit Modifiers")]
        public ToHitModifiers ToHitModifiers { get; set; }

    }

    public class ToHitModifiers
    {
        [JsonProperty("From Behind")]
        public bool? FromBehind { get; set; }
        [JsonProperty("Defender Entangled")]
        public bool? DefenderEntangled { get; set; }
        [JsonProperty("Surprise Move")]
        public int? SurpriseMove { get; set; }
    }

    public class PotentialKnockbackCollision
    {
        [JsonProperty("Collision Object")]
        public string CollisionObject { get; set; }
        [JsonProperty("Collision Distance")]
        public int CollisionDistance{ get; set; }
    }

    public class AreaAttackRequest : AttackRequestBase
    {
        [JsonProperty("AOE Center")]
        public string Center { get; set; }
        [JsonProperty("Attack Targets")]
        public List<AttackRequest> Targets { get; set; }
    }

    public class AutoFireAttackRequest : AttackRequestBase
    {
        public int Width { get; set; }
        [JsonProperty("Spray Fire")]
        public bool? Spray { get; set; }
        public int? Shots { get; set; }
        [JsonProperty("Attack Targets")]
        public List<AttackRequest> Targets { get; set; }
    }

    public class SweepAttackRequest : AttackRequestBase
    {
        [JsonProperty("Attack Targets")]
        public List<AttackRequestBase> Attacks { get; set; }
    }

    public class AttackResponseBase
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public string Ability { get; set; }
        [JsonProperty("Hit")]
        public bool IsHit { get; set; }
        [JsonProperty("Move Before Attack Required")]
        public bool? MoveBeforeAttackRequired { get; set; }
    }

    public class AttackResponse : AttackResponseBase
    {
        public TargetObject Defender { get; set; }
        [JsonProperty("Obstruction Damage Results")]
        public List<AttackResponse> ObstructionDamageResults { get; set; }
        [JsonProperty("Damage Results")]
        public DamageResults DamageResults { get; set; }
        [JsonProperty("Knockback Result")]
        public KnockbackResult KnockbackResult { get; set; }
    }

    public class TargetObject
    {
        public string Name { get; set; }
        [JsonProperty("STUN")]
        public HealthMeasures Stun { get; set; }
        [JsonProperty("BODY")]
        public HealthMeasures Body { get; set; }
        [JsonProperty("END")]
        public HealthMeasures Endurance { get; set; }
        public List<String> Effects { get; set; }
    }

    public class MultiTargetAttackResponse : AttackResponseBase
    {
        [JsonProperty("Affected Targets")]
        public List<AttackResponse> Targets { get; set; }
    }

    public class AreaAttackResponse : MultiTargetAttackResponse
    {
        
    }
    public class AutoFireAttackResponse : MultiTargetAttackResponse
    {

    }

    public class SweepAttackResponse : AttackResponseBase
    {
        [JsonProperty("Affected Targets")]
        public List<AttackResponseBase> Attacks { get; set; }
    }


   
    public class KnockbackResult
    {
        public List<KnockbackCollision> Collisions { get; set; }
        public int Distance { get; set; }
    }

    public class KnockbackCollision
    {
        [JsonProperty("Object Collided With")]
        public TargetObject CollidingObject { get; set; }
        [JsonProperty("Collision Damage Results")]
        public DamageResults CollisionDamageResults { get; set; }
    }

    public class HealthMeasures
    {
        public double? Max { get; set; }
        public double? Current { get; set; }
        public double? Starting { get; set; }
        public string Name { get; set; }
    }

    public class DamageResults
    {
        [JsonProperty("STUN")]
        public double? Stun { get; set; }
        [JsonProperty("BODY")]
        public double? Body { get; set; }
        [JsonProperty("END")]
        public double? Endurance { get; set; }
    }

    public class AttackConfirmation
    {
        public string Type { get; set; }
        [JsonProperty("Status")]
        public string ConfirmationStatus { get; set; }
    }

    public enum HCSIntegrationStatus
    {
        Started,
        Stopped
    }
    public enum HCSIntegrationAction
    {
        DeckUpdated,
        ActiveCharacterUpdated,
        EligibleCombatantsUpdated,
        AttackInitiated,
        AttackCancelled,
        AttackConfirmed,
        AttackResultReceived
    }

    public enum HCSAttackType
    {
        None,
        Vanilla,
        Area,
        AutoFire,
        Sweep
    }
}
