using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
using Module.Shared.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HeroVTT.HCSIntegration
{
    public class HcsCombatEventProcessor : IHCSIntegrator
    {
        private readonly IHcsFileWatcher _fileWatcher;
        private readonly CollisionEngine _collisionEngine;
        private readonly object _lockAttackSensor = new object();

        private string _currentToken;
        private bool _deckUpdatePending;
        private bool _eligibleCombatantsUpdatePending;
        private bool _attackInfoUpdatedForCurrentAttack;
        private HCSAttackType _savedAttackType = HCSAttackType.None;

        public List<Character> InGameCharacters { get; set; }
        public CombatantsCollection CurrentOnDeckCombatants { get; set; }
        public HCSIntegrationAction LastIntegrationAction { get; set; }
        public ActiveCharacterInfo CurrentActiveCharacterInfo { get; set; }
        public AttackResponseBase CurrentAttackResult { get; set; }
        public string CurrentAttackResultFileContents { get; set; }
        public string CurrentOnDeckCombatantsFileContents { get; set; }
        public string CurrentChronometerFileContents { get; set; }
        public string CurrentActiveCharacterInfoFileContents { get; set; }
        public string CurrentEligibleCombatantsFileContents { get; set; }
        public HCSAttackType CurrentAttackType { get; set; }
        public HCSIntegrationStatus CurrentIntegrationStatus { get; set; }
        public List<Tuple<Attack, List<Character>, List<Character>, Guid>> AttacksToConfigure { get; set; }
        public List<Tuple<Guid, Character>> RespondedConfigKeysWithDefenders { get; set; }

        public event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        public event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        public event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        public event EventHandler<CustomEventArgs<Object>> SweepAttackResultsUpdated;
        public event EventHandler<CustomEventArgs<Object>> EligibleCombatantsUpdated;

        public HcsCombatEventProcessor(IHcsFileWatcher fileWatcher)
        {
            _fileWatcher = fileWatcher;
            _collisionEngine = new CollisionEngine();
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;

            _fileWatcher.CombatantsFileChanged += OnCombatantsFileChanged;
            _fileWatcher.ActiveCharacterFileChanged += OnActiveCharacterFileChanged;
            _fileWatcher.EligibleCombatantsFileChanged += OnEligibleCombatantsFileChanged;
            _fileWatcher.AttackResultFileChanged += OnAttackResultFileChanged;
        }

        public void StartIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Started;
            _fileWatcher.StartWatching();
        }

        public void StopIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;
            _fileWatcher.StopWatching();
        }

        #region File Change Handlers

        private void OnCombatantsFileChanged(object sender, HcsFileChangedEventArgs e)
        {
            if (LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                UpdateSequence();
            else
                _deckUpdatePending = true;
        }

        private void OnActiveCharacterFileChanged(object sender, HcsFileChangedEventArgs e)
        {
            UpdateActiveCharacter();
        }

        private void OnEligibleCombatantsFileChanged(object sender, HcsFileChangedEventArgs e)
        {
            if (LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                UpdateEligibleCombatantsInfo();
            else
                _eligibleCombatantsUpdatePending = true;
        }

        private void OnAttackResultFileChanged(object sender, HcsFileChangedEventArgs e)
        {
            if (CurrentIntegrationStatus != HCSIntegrationStatus.Stopped)
                ProcessAttackResults();
        }

        #endregion

        #region Sequence and Character Updates

        private void UpdateSequence()
        {
            string combatantsJson = _fileWatcher.ReadFileContents(Constants.COMBATANTS_FILE_NAME);
            string chronometerJson = _fileWatcher.ReadFileContents(Constants.CHRONOMETER_FILE_NAME);

            bool combatantsChanged = CurrentOnDeckCombatantsFileContents != combatantsJson;
            bool chronometerChanged = CurrentChronometerFileContents != chronometerJson;

            if (combatantsJson != null && chronometerJson != null && (chronometerChanged || combatantsChanged))
            {
                LastIntegrationAction = HCSIntegrationAction.DeckUpdated;
                object sequenceInfo = GetLatestSequenceInfo();
                object[] seqArray = sequenceInfo as object[];
                if (seqArray != null && seqArray.Length == 4 && seqArray[0] != null && seqArray[1] != null && seqArray[2] != null && seqArray[3] != null)
                    { var h = SequenceUpdated; if (h != null) h(null, new CustomEventArgs<object> { Value = seqArray }); }
            }
        }

        private void UpdateActiveCharacter()
        {
            string json = _fileWatcher.ReadFileContents(Constants.ACTIVE_CHARACTER_FILE_NAME);
            if (CurrentActiveCharacterInfoFileContents != json)
            {
                LastIntegrationAction = HCSIntegrationAction.ActiveCharacterUpdated;
                ActiveCharacterInfo info = GetCurrentActiveCharacterInfo();
                if (info != null)
                    { var h = ActiveCharacterUpdated; if (h != null) h(null, new CustomEventArgs<object> { Value = info }); }
            }
        }

        private void UpdateEligibleCombatantsInfo()
        {
            string json = _fileWatcher.ReadFileContents(Constants.ELIGIBLE_COMBATANTS_FILE_NAME);
            if (CurrentEligibleCombatantsFileContents != json)
            {
                LastIntegrationAction = HCSIntegrationAction.EligibleCombatantsUpdated;
                CombatantsCollection coll = GetCurrentEligibleCombatants();
                if (coll != null)
                    { var h = EligibleCombatantsUpdated; if (h != null) h(null, new CustomEventArgs<object> { Value = coll }); }
            }
        }

        public object GetLatestSequenceInfo()
        {
            Chronometer chrono = GetCurrentChronometer();
            CombatantsCollection combatants = GetCurrentCombatants();
            ActiveCharacterInfo active = GetCurrentActiveCharacterInfo();
            CombatantsCollection eligible = GetCurrentEligibleCombatants();
            return new object[] { combatants, chrono, active, eligible };
        }

        #endregion

        #region Data Deserialization

        private CombatantsCollection GetCurrentCombatants()
        {
            string json = _fileWatcher.ReadFileContents(Constants.COMBATANTS_FILE_NAME);
            CurrentOnDeckCombatantsFileContents = json;
            CombatantsCollection result = json != null ? JsonConvert.DeserializeObject<CombatantsCollection>(json) : null;
            CurrentOnDeckCombatants = result;
            return result;
        }

        private Chronometer GetCurrentChronometer()
        {
            string json = _fileWatcher.ReadFileContents(Constants.CHRONOMETER_FILE_NAME);
            CurrentChronometerFileContents = json;
            return json != null ? JsonConvert.DeserializeObject<Chronometer>(json) : null;
        }

        private ActiveCharacterInfo GetCurrentActiveCharacterInfo()
        {
            string json = _fileWatcher.ReadFileContents(Constants.ACTIVE_CHARACTER_FILE_NAME);
            CurrentActiveCharacterInfoFileContents = json;
            if (json == null) return null;

            CurrentActiveCharacterInfo = JsonConvert.DeserializeObject<ActiveCharacterInfo>(json);
            CurrentActiveCharacterInfo.AbilitiesEligibilityCollection = GetAbilityActivationEligibilityCollection();
            return CurrentActiveCharacterInfo;
        }

        private CombatantsCollection GetCurrentEligibleCombatants()
        {
            string json = _fileWatcher.ReadFileContents(Constants.ELIGIBLE_COMBATANTS_FILE_NAME);
            CurrentEligibleCombatantsFileContents = json;
            return json != null ? JsonConvert.DeserializeObject<CombatantsCollection>(json) : null;
        }

        private List<AbilityActivationEligibility> GetAbilityActivationEligibilityCollection()
        {
            var result = new List<AbilityActivationEligibility>();
            var dict = new Dictionary<string, bool>();

            if (CurrentActiveCharacterInfo.Powers != null)
                dict = ParseAbilityEligibility(CurrentActiveCharacterInfo.Powers.ToString());

            if (CurrentActiveCharacterInfo.Defaults != null)
            {
                var additional = ParseAbilityEligibility(CurrentActiveCharacterInfo.Defaults.ToString());
                foreach (var kvp in additional)
                    if (!dict.ContainsKey(kvp.Key))
                        dict[kvp.Key] = kvp.Value;
            }

            foreach (var entry in dict)
                if (!result.Any(e => e.AbilityName == entry.Key))
                    result.Add(new AbilityActivationEligibility { AbilityName = entry.Key, IsEnabled = entry.Value });

            return result;
        }

        private Dictionary<string, bool> ParseAbilityEligibility(string json)
        {
            var dict = new Dictionary<string, bool>();
            JToken outer = JToken.Parse(json);
            foreach (var child in outer.Children())
            {
                JProperty prop = child as JProperty;
                if (prop == null) continue;

                JObject obj = prop.Value as JObject;
                if (obj == null) continue;

                var enabledValues = obj.Properties().Where(p => p.Name == "Is Enabled").Select(p => p.Value);
                if (enabledValues.Any() && !dict.ContainsKey(prop.Name))
                    dict.Add(prop.Name, enabledValues.First().Value<bool>());
            }
            return dict;
        }

        #endregion

        #region Attack Result Processing

        private bool ProcessAttackResults()
        {
            lock (_lockAttackSensor)
            {
                AttackResponseBase attackResult = GetAttackResults();
                bool matched = attackResult != null && attackResult.Token != null && attackResult.Token == _currentToken;

                if (matched)
                {
                    if (LastIntegrationAction == HCSIntegrationAction.AttackInitiated)
                    {
                        LastIntegrationAction = HCSIntegrationAction.AttackResultReceived;
                        _attackInfoUpdatedForCurrentAttack = false;
                        DispatchAttackResults(attackResult);
                    }
                    else if (_attackInfoUpdatedForCurrentAttack)
                    {
                        _attackInfoUpdatedForCurrentAttack = false;
                        DispatchAttackResults(attackResult);
                    }

                    if (_deckUpdatePending) { _deckUpdatePending = false; UpdateSequence(); }
                    if (_eligibleCombatantsUpdatePending) { _eligibleCombatantsUpdatePending = false; UpdateEligibleCombatantsInfo(); }
                }

                return matched;
            }
        }

        private AttackResponseBase GetAttackResults()
        {
            string json = _fileWatcher.ReadFileContents(Constants.ATTACK_RESULT_FILE_NAME);
            if (string.IsNullOrEmpty(json)) return null;

            if (json != CurrentAttackResultFileContents)
                _attackInfoUpdatedForCurrentAttack = true;
            CurrentAttackResultFileContents = json;

            switch (CurrentAttackType)
            {
                case HCSAttackType.Area: return JsonConvert.DeserializeObject<AreaAttackResponse>(json);
                case HCSAttackType.Vanilla: return JsonConvert.DeserializeObject<AttackResponse>(json);
                case HCSAttackType.AutoFire: return JsonConvert.DeserializeObject<AutoFireAttackResponse>(json);
                case HCSAttackType.Sweep: return DeserializeSweepResponse(json);
                default: return null;
            }
        }

        private AttackResponseBase DeserializeSweepResponse(string json)
        {
            var sweep = new SweepAttackResponse { Attacks = new List<AttackResponseBase>() };
            dynamic obj = JsonConvert.DeserializeObject(json);
            sweep.Token = obj.Token;

            JToken root = JToken.Parse(json);
            var targetsToken = root.Children().FirstOrDefault(t => t is JProperty && (t as JProperty).Name == "Affected Targets");
            if (targetsToken == null) return sweep;

            JArray attacks = (targetsToken as JProperty).Value.Value<JArray>();
            if (attacks == null) return sweep;

            foreach (var attack in attacks)
            {
                var attackJson = JsonConvert.SerializeObject(attack);
                var response = JsonConvert.DeserializeObject<AttackResponse>(attackJson);
                if (response != null)
                    sweep.Attacks.Add(response);
            }
            return sweep;
        }

        private void DispatchAttackResults(AttackResponseBase attackResult)
        {
            CurrentAttackResult = attackResult;
            if (CurrentAttackType != HCSAttackType.Sweep)
            {
                var targets = ParseAttackTargetsFromAttackResult(attackResult);
                { var h = AttackResultsUpdated; if (h != null) h(this, new CustomEventArgs<object> { Value = targets }); }
            }
            else
            {
                var sweepResponse = attackResult as SweepAttackResponse;
                var attacksWithTargets = ParseSweepTargets(sweepResponse);
                { var h = SweepAttackResultsUpdated; if (h != null) h(this, new CustomEventArgs<object> { Value = attacksWithTargets }); }
            }
        }

        #endregion

        #region Attack Target Parsing

        private void ParseEffects(AttackConfiguration config, List<string> effects)
        {
            if (effects == null) return;
            if (effects.Contains("Stunned")) config.IsStunned = true;
            if (effects.Contains("Unconscious")) config.IsUnconcious = true;
            if (effects.Contains("Dying")) config.IsDying = true;
            if (effects.Contains("Dead")) config.IsDead = true;
            if (effects.Contains("Partially Destroyed")) config.IsPartiallyDestryoed = true;
            if (effects.Contains("Destroyed")) config.IsDestroyed = true;
        }

        private List<Character> ParseAttackTargetsFromAttackResult(AttackResponseBase attackResult)
        {
            switch (CurrentAttackType)
            {
                case HCSAttackType.Area: return ParseMultiTargetResponse(attackResult as AreaAttackResponse);
                case HCSAttackType.Vanilla: return ParseSingleTargetResponse(attackResult as AttackResponse);
                case HCSAttackType.AutoFire: return ParseMultiTargetResponse(attackResult as AutoFireAttackResponse);
                default: return null;
            }
        }

        private List<Character> ParseSingleTargetResponse(AttackResponse response, AttackResponseBase parent = null)
        {
            var targets = new List<Character>();
            Character primary = InGameCharacters.FirstOrDefault(c => c.Name == response.Defender.Name);
            if (primary == null) return targets;

            string abilityName = response.Ability ?? (parent != null ? parent.Ability : null);
            Attack respondedAttack = AttacksToConfigure.Where(ac => ac.Item1.Name == abilityName).Select(ac => ac.Item1).First();
            Guid key = GetConfigKeyForRespondedAttack(response, parent);

            targets.Add(primary);
            AttackConfiguration cfg = primary.AttackConfigurationMap.ContainsKey(key) ? primary.AttackConfigurationMap[key].Item2 : new AttackConfiguration();

            cfg.IsHit = response.IsHit;
            cfg.Body = (int?)response.Defender.Body.Current;
            cfg.Stun = (int?)response.Defender.Stun.Current;
            if (response.MoveBeforeAttackRequired.HasValue)
                cfg.MoveAttackerToTarget = response.MoveBeforeAttackRequired.Value;

            var secondaryTargets = new List<Character>();
            ProcessKnockbackCollisions(response, cfg, key, respondedAttack, primary, targets);
            ProcessObstructionDamage(response, cfg, key, respondedAttack, primary, targets, secondaryTargets);

            ParseEffects(cfg, response.Defender.Effects);
            primary.AddAttackConfiguration(respondedAttack, cfg, key);
            if (!RespondedConfigKeysWithDefenders.Any(rcd => rcd.Item1 == key))
                RespondedConfigKeysWithDefenders.Add(new Tuple<Guid, Character>(key, primary));

            return targets;
        }

        private void ProcessKnockbackCollisions(AttackResponse response, AttackConfiguration cfg, Guid key, Attack attack, Character primary, List<Character> targets)
        {
            if (response.KnockbackResult == null || response.KnockbackResult.Distance == 0)
                return;

            cfg.IsKnockedBack = true;
            cfg.KnockBackDistance = response.KnockbackResult.Distance;

            if (response.KnockbackResult.Collisions == null || response.KnockbackResult.Collisions.Count == 0)
                return;

            cfg.ObstructingCharacters = new List<Character>();
            foreach (var collision in response.KnockbackResult.Collisions)
            {
                var secondary = InGameCharacters.FirstOrDefault(c => c.Name == collision.CollidingObject.Name);
                if (secondary == null) continue;

                AttackConfiguration secondaryCfg = secondary.AttackConfigurationMap.ContainsKey(key)
                    ? secondary.AttackConfigurationMap[key].Item2
                    : new AttackConfiguration();

                secondaryCfg.Body = (int)collision.CollisionDamageResults.Body;
                if (collision.CollidingObject.Effects != null && collision.CollidingObject.Effects.Count > 0)
                    ParseEffects(secondaryCfg, collision.CollidingObject.Effects);

                targets.Add(secondary);
                secondaryCfg.IsHit = true;
                secondaryCfg.PrimaryTargetCharacter = primary;
                secondaryCfg.ObstructingCharacters = null;
                secondaryCfg.IsKnockbackObstruction = true;
                secondary.AddAttackConfiguration(attack, secondaryCfg, key);
                cfg.ObstructingCharacters.Add(secondary);
            }
        }

        private void ProcessObstructionDamage(AttackResponse response, AttackConfiguration cfg, Guid key, Attack attack, Character primary, List<Character> targets, List<Character> secondaryTargets)
        {
            if (response.ObstructionDamageResults == null || response.ObstructionDamageResults.Count == 0)
                return;

            if (cfg.ObstructingCharacters == null)
                cfg.ObstructingCharacters = new List<Character>();

            foreach (var obstruction in response.ObstructionDamageResults.Where(odr => odr.IsHit && odr.Defender != null))
                secondaryTargets.AddRange(ParseSingleTargetResponse(obstruction));

            foreach (var secondary in secondaryTargets)
            {
                if (targets.Contains(secondary)) continue;
                targets.Add(secondary);
                secondary.AttackConfigurationMap[key].Item2.PrimaryTargetCharacter = primary;
                cfg.ObstructingCharacters.Add(secondary);
            }
        }

        private List<Character> ParseMultiTargetResponse(MultiTargetAttackResponse multiResponse)
        {
            var targets = new List<Character>();
            foreach (var response in multiResponse.Targets)
            {
                Guid key = GetConfigKeyForRespondedAttack(response, multiResponse);
                var responseTargets = ParseSingleTargetResponse(response, multiResponse);
                foreach (var target in responseTargets)
                {
                    if (response.MoveBeforeAttackRequired.HasValue && response.MoveBeforeAttackRequired.Value)
                    {
                        target.AttackConfigurationMap[key].Item2.IsCenterTarget = false;
                        target.AttackConfigurationMap[key].Item2.MoveAttackerToTarget = true;
                    }
                    if (!targets.Contains(target))
                        targets.Add(target);
                }
            }
            return targets;
        }

        private List<Tuple<Guid, List<Character>>> ParseSweepTargets(SweepAttackResponse sweepResponse)
        {
            var result = new List<Tuple<Guid, List<Character>>>();
            foreach (var response in sweepResponse.Attacks)
            {
                List<Character> targets;
                if (response is AreaAttackResponse)
                    targets = ParseMultiTargetResponse(response as AreaAttackResponse);
                else
                    targets = ParseSingleTargetResponse(response as AttackResponse);

                result.Add(new Tuple<Guid, List<Character>>(new Guid(response.Token), targets));
            }
            return result;
        }

        private Guid GetConfigKeyForRespondedAttack(AttackResponse response, AttackResponseBase parent = null)
        {
            if (!string.IsNullOrEmpty(response.Token) && parent != null)
                return new Guid(response.Token);

            string abilityName = response.Ability ?? (parent != null ? parent.Ability : null);
            var keys = AttacksToConfigure.Where(ac => ac.Item1.Name == abilityName).Select(ac => ac.Item4).ToList();

            if (keys.Count == 1)
                return keys.First();

            foreach (var key in keys)
            {
                if (!RespondedConfigKeysWithDefenders.Any(rcd => rcd.Item1 == key && rcd.Item2.Name == response.Defender.Name))
                    return key;
            }

            return Guid.Empty;
        }

        #endregion

        #region Attack Configuration and Initiation

        public void ConfigureAttacks(List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToConfigure, bool sweep = false)
        {
            ResetAttackParameters();
            AttacksToConfigure = attacksToConfigure.ToList();

            if (!sweep)
            {
                var first = attacksToConfigure[0];
                ConfigureAttack(first.Item1, first.Item2, first.Item3, first.Item4);
            }
            else
            {
                ConfigureSweepAttack();
            }
        }

        private void ConfigureAttack(Attack attack, List<Character> attackers, List<Character> defenders, Guid configKey)
        {
            if (attack.AttackInfo != null && attack.AttackInfo.AttackType == AttackType.Area)
            {
                if (attackers.Count <= 1)
                    ConfigureAreaAttack(attack, attackers[0], defenders, configKey);
            }
            else if (attack.IsAutoFire)
            {
                ConfigureAutoFireAttack(attack, attackers[0], defenders, configKey);
            }
            else
            {
                if (attackers.Count <= 1 && defenders.Count <= 1)
                    ConfigureVanillaAttack(attack, attackers[0], defenders[0], configKey);
            }
        }

        private void ConfigureVanillaAttack(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            CurrentAttackType = HCSAttackType.Vanilla;
            GenerateAttackMessage(attack, attacker, defender, configKey);
            LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
        }

        private void ConfigureAreaAttack(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            CurrentAttackType = HCSAttackType.Area;
            GenerateAreaAttackMessage(attack, attacker, defenders, configKey);
            LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
        }

        private void ConfigureAutoFireAttack(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            CurrentAttackType = HCSAttackType.AutoFire;
            GenerateAutoFireAttackMessage(attack, attacker, defenders, configKey);
            LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
        }

        private void ConfigureSweepAttack()
        {
            CurrentAttackType = HCSAttackType.Sweep;
            _currentToken = Guid.NewGuid().ToString();
            var request = new SweepAttackRequest
            {
                Token = _currentToken,
                Type = Constants.SWEEP_ATTACK_INITIATION_TYPE_NAME,
                Ability = Constants.SWEEP_ATTACK_INITIATION_TYPE_NAME,
                Attacks = new List<AttackRequestBase>()
            };

            foreach (var tuple in AttacksToConfigure)
            {
                if (tuple.Item1.AttackInfo != null && tuple.Item1.AttackInfo.AttackType == AttackType.Area)
                {
                    var area = BuildAreaAttackRequest(tuple.Item1, tuple.Item2[0], tuple.Item3, tuple.Item4);
                    area.Type = Constants.AREA_ATTACK_INITIATION_TYPE_NAME;
                    area.Token = tuple.Item4.ToString();
                    request.Attacks.Add(area);
                }
                else if (!tuple.Item1.IsAutoFire)
                {
                    var vanilla = BuildVanillaAttackRequest(tuple.Item1, tuple.Item2.First(), tuple.Item3.First(), tuple.Item4);
                    vanilla.Type = Constants.ATTACK_INITIATION_TYPE_NAME;
                    vanilla.Token = tuple.Item4.ToString();
                    request.Attacks.Add(vanilla);
                }
            }

            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, request);
            LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
        }

        public void ConfirmAttack()
        {
            var confirmation = new AttackConfirmation
            {
                Type = Constants.ATTACK_CONFIRMATION_TYPE_NAME,
                ConfirmationStatus = Constants.ATTACK_CONFIRMED_STATUS
            };
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, confirmation);
            ResetAttackParameters();
        }

        public void CancelAttack()
        {
            var confirmation = new AttackConfirmation
            {
                Type = Constants.ATTACK_CONFIRMATION_TYPE_NAME,
                ConfirmationStatus = Constants.ATTACK_CANCELLED_STATUS
            };
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, confirmation);
            ResetAttackParameters();
        }

        public void ResumeAttack()
        {
            CurrentAttackType = _savedAttackType;
            var results = GetAttackResults();
            if (results != null)
                DispatchAttackResults(results);
        }

        public void AbortAction(List<Character> abortingCharacters)
        {
            foreach (Character ch in abortingCharacters)
            {
                var msg = new SimpleAbility { Type = Constants.ABORT_ACTION_TYPE_NAME, Character = ch.Name };
                _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, msg);
                System.Threading.Thread.Sleep(1000);
            }
            _savedAttackType = CurrentAttackType;
        }

        public void ActivateHeldCharacter(Character heldCharacter)
        {
            var msg = new SimpleAbility { Type = Constants.ACTIVATE_HELD_CHARACTER_TYPE_NAME, Character = heldCharacter.Name };
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, msg);
            System.Threading.Thread.Sleep(1000);
            _savedAttackType = CurrentAttackType;
        }

        public void PlaySimpleAbility(Character target, AnimatedAbility ability)
        {
            var msg = new SimpleAbility { Ability = ability.Name, Type = Constants.SIMPLE_ABILITY_TYPE_NAME };
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, msg);
        }

        public void NotifyStopMovement(CharacterMovement movement, double distanceTravelled)
        {
            var msg = new SimpleMovement { Movement = NormalizeMovementName(movement.Name), Type = Constants.MOVEMENT_TYPE_NAME, Distance = (int)distanceTravelled };
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, msg);
        }

        private void ResetAttackParameters()
        {
            CurrentAttackResult = null;
            CurrentAttackResultFileContents = null;
            CurrentAttackType = HCSAttackType.None;
            AttacksToConfigure = new List<Tuple<Attack, List<Character>, List<Character>, Guid>>();
            RespondedConfigKeysWithDefenders = new List<Tuple<Guid, Character>>();
        }

        #endregion

        #region Movement and Attack Info

        public float GetMovementDistanceLimit(CharacterMovement activeMovement)
        {
            if (CurrentActiveCharacterInfo == null)
                GetCurrentActiveCharacterInfo();

            if (CurrentActiveCharacterInfo.Powers == null)
                return 0f;

            string movementName = NormalizeMovementName(activeMovement.Name.ToLower());
            return ParseDistanceLimitForMovement(movementName, activeMovement.IsNonCombatMovement);
        }

        public AttackInfo GetAttackInfo(string powerName)
        {
            var info = new AttackInfo { AttackShape = AttackShape.None, AttackType = AttackType.None, Range = 0, TargetSelective = false, CanSpread = false, AutoFireMaxShots = 0 };
            if (CurrentActiveCharacterInfo == null)
                GetCurrentActiveCharacterInfo();
            if (CurrentActiveCharacterInfo.Defaults == null)
                return info;

            JToken outer = JToken.Parse(CurrentActiveCharacterInfo.Defaults.ToString());
            bool exists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == powerName);
            if (!exists) return info;

            JObject inner = outer[powerName].Value<JObject>();
            dynamic d = inner;
            ParseAreaEffectInfo(d, info);
            ParseAutoFireInfo(d, info);
            ParseRangedAndSpreadInfo(d, info);

            return info;
        }

        private void ParseAreaEffectInfo(dynamic d, AttackInfo info)
        {
            if (d.Description == null) return;
            string desc = d.Description.ToString();
            if (!desc.Contains("Area Effect") || d.Advantages == null) return;

            JObject advantages = d.Advantages;
            var areaEffect = advantages.Properties().Where(p => p.Name == "Area Effect").Select(p => p.Value);
            if (areaEffect == null || !areaEffect.Any()) return;

            dynamic ae = areaEffect.First();
            info.AttackType = AttackType.Area;
            if (ae.Range != null) info.Range = ae.Range;
            if (ae.Type != null && ae.Type == "Selective") info.TargetSelective = true;
            if (ae.Shape != null && !string.IsNullOrEmpty(ae.Shape.ToString()))
                info.AttackShape = ParseAreaAttackShape(ae.Shape.ToString());
            else if (ae.Description != null && !string.IsNullOrEmpty(ae.Description.ToString()))
                info.AttackShape = ParseAreaAttackShape(ae.Description.ToString());
        }

        private void ParseAutoFireInfo(dynamic d, AttackInfo info)
        {
            if (d.Description == null) return;
            string desc = d.Description.ToString();
            if (!desc.Contains("AF (") || d.Advantages == null) return;

            JObject advantages = d.Advantages;
            var autoFire = advantages.Properties().Where(p => p.Name == "Autofire").Select(p => p.Value);
            if (autoFire == null || !autoFire.Any()) return;

            dynamic af = autoFire.First();
            info.AttackType = AttackType.AutoFire;
            if (af.MaxShots != null) info.AutoFireMaxShots = af.MaxShots;
        }

        private void ParseRangedAndSpreadInfo(dynamic d, AttackInfo info)
        {
            JObject details = d.Details;
            if (details != null)
            {
                TryParseBoolProperty(details, "IsRanged", v => info.IsRanged = v);
                TryParseBoolProperty(details, "CanSpread", v => info.CanSpread = v);
            }
            if (d.IsRanged != null) { bool b; if (Boolean.TryParse(d.IsRanged.ToString(), out b)) info.IsRanged = b; }
            if (d.CanSpread != null) { bool b; if (Boolean.TryParse(d.CanSpread.ToString(), out b)) info.CanSpread = b; }
        }

        private void TryParseBoolProperty(JObject obj, string propName, Action<bool> setter)
        {
            var values = obj.Properties().Where(p => p.Name == propName).Select(p => p.Value);
            if (values == null || !values.Any()) return;
            dynamic val = values.First();
            if (val == null || val.Value == null) return;
            bool b;
            if (Boolean.TryParse(val.Value.ToString(), out b))
                setter(b);
        }

        private float ParseDistanceLimitForMovement(string movementName, bool nonCombat)
        {
            string limitString = GetDistanceLimitString(movementName);
            string[] tokens = limitString.Split(',');
            if (tokens.Length != 3) return 0;

            string tokenToConsider = nonCombat ? tokens[2] : tokens[1];
            string[] numbers = Regex.Split(tokenToConsider, @"\D+");
            foreach (string n in numbers)
            {
                int k;
                if (int.TryParse(n, out k))
                    return k;
            }
            return 0;
        }

        private string GetDistanceLimitString(string movementName)
        {
            JToken outer = null;
            if (CurrentActiveCharacterInfo.Powers != null)
                outer = JToken.Parse(CurrentActiveCharacterInfo.Powers.ToString());

            bool exists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            if (!exists && CurrentActiveCharacterInfo.Defaults != null)
            {
                outer = JToken.Parse(CurrentActiveCharacterInfo.Defaults.ToString());
                exists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            }
            if (!exists) return "";

            dynamic d = outer[movementName].Value<JObject>();
            return (string)d.Description ?? "";
        }

        private AttackShape ParseAreaAttackShape(string shapeString)
        {
            string lower = shapeString.ToLower();
            if (lower.Contains("radius")) return AttackShape.Radius;
            if (lower.Contains("one-hex")) return AttackShape.Line;
            if (lower.Contains("cone")) return AttackShape.Cone;
            return AttackShape.None;
        }

        private string NormalizeMovementName(string name)
        {
            switch (name.ToLower())
            {
                case "running": case "run": return "Running";
                case "walking": case "walk": return "Walking";
                case "swimming": case "swim": return "Swimming";
                case "leaping": case "leap": return "Leaping";
                case "flying": case "fly": return "Flying";
                case "jumping": case "jump": return "Leaping";
                default: return "";
            }
        }

        #endregion

        #region Attack Request Building

        private const float HEXES_TO_UNITS = 8f;
        private const int MAX_KNOCKBACK_DISTANCE_HEXES = 50;

        private void GenerateAttackMessage(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            CurrentAttackResult = null;
            _currentToken = Guid.NewGuid().ToString();
            var request = BuildVanillaAttackRequest(attack, attacker, defender, configKey);
            request.Token = _currentToken;
            request.Type = Constants.ATTACK_INITIATION_TYPE_NAME;
            if (attack.CanSpread && attack.SpreadDistance > 0)
                request.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, request);
        }

        private void GenerateAreaAttackMessage(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            CurrentAttackResult = null;
            _currentToken = Guid.NewGuid().ToString();
            var request = BuildAreaAttackRequest(attack, attacker, defenders, configKey);
            request.Type = Constants.AREA_ATTACK_INITIATION_TYPE_NAME;
            request.Token = _currentToken;
            if (attack.CanSpread && attack.SpreadDistance > 0)
                request.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, request);
        }

        private void GenerateAutoFireAttackMessage(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            CurrentAttackResult = null;
            _currentToken = Guid.NewGuid().ToString();
            var request = BuildAutoFireAttackRequest(attack, attacker, defenders, configKey);
            request.Type = Constants.AUTO_FIRE_ATTACK_INITIATION_TYPE_NAME;
            request.Token = _currentToken;
            if (attack.CanSpread && attack.SpreadDistance > 0)
                request.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            _fileWatcher.WriteJsonToFileAsync(Constants.ABILITY_ACTIVATED_FILE_NAME, request);
        }

        private AttackRequest BuildVanillaAttackRequest(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            var request = BuildBaseAttackRequest(attack, attacker, defender);
            request.Ability = attack.Name;
            return request;
        }

        private AreaAttackRequest BuildAreaAttackRequest(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            var request = new AreaAttackRequest { Ability = attack.Name, Targets = new List<AttackRequest>() };
            foreach (Character defender in defenders)
            {
                AttackRequest target = BuildBaseAttackRequest(attack, attacker, defender);
                RemoveFriendlyObstructions(target, defenders);
                request.Targets.Add(target);
            }
            request.Center = defenders.Any(d => d.AttackConfigurationMap[configKey].Item2.IsCenterTarget)
                ? defenders.First(d => d.AttackConfigurationMap[configKey].Item2.IsCenterTarget).Name
                : Constants.HEX;
            return request;
        }

        private AutoFireAttackRequest BuildAutoFireAttackRequest(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            var request = new AutoFireAttackRequest { Ability = attack.Name, Targets = new List<AttackRequest>() };
            Vector3 facing = defenders[0].CurrentPositionVector - attacker.CurrentPositionVector;
            Vector3 left = Helper.GetAdjacentPoint(attacker.CurrentPositionVector, facing, true, 1000);
            Vector3 right = Helper.GetAdjacentPoint(attacker.CurrentPositionVector, facing, false, 1000);
            Vector3 refVector = right - left;
            var projections = new List<Vector3>();

            foreach (Character defender in defenders)
            {
                int shots = defender.AttackConfigurationMap[configKey].Item2.NumberOfShotsAssigned;
                for (int i = 0; i < shots; i++)
                {
                    AttackRequest target = BuildBaseAttackRequest(attack, attacker, defender);
                    RemoveFriendlyObstructions(target, defenders);
                    request.Targets.Add(target);
                }
                Vector3 proj = defender.CurrentPositionVector - left;
                projections.Add(Helper.GetIntersectionPointOfPerpendicularProjectionVectorOnAnotherVector(refVector, proj));
            }

            var maxDist = Helper.CalculateMaximumDistanceBetweenTwoPointsInASetOfPoints(projections.ToArray());
            request.Width = (int)Math.Round(maxDist / HEXES_TO_UNITS, MidpointRounding.AwayFromZero);
            request.Shots = attack.AttackInfo.AutoFireMaxShots;
            if (defenders.Count > 1) request.Spray = true;

            return request;
        }

        private AttackRequest BuildBaseAttackRequest(Attack attack, Character attacker, Character defender)
        {
            var request = new AttackRequest();
            float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
            request.Range = (int)Math.Round(range / HEXES_TO_UNITS, MidpointRounding.AwayFromZero);
            request.Defender = defender.Name;

            var others = InGameCharacters.Where(c => c != attacker && c != defender).ToList();
            request.ToHitModifiers = new ToHitModifiers
            {
                FromBehind = !Helper.DetermineIfOneObjectIsInFrontOfAnotherObject(
                    defender.CurrentPositionVector, defender.CurrentFacingVector, attacker.CurrentPositionVector)
            };

            request.Obstructions = new List<string>();
            var obstructions = _collisionEngine.FindObstructingObjects(attacker, defender, others);
            if (obstructions != null)
            {
                foreach (var obs in obstructions)
                    request.Obstructions.Add(obs.CollidingObject is Character ? (obs.CollidingObject as Character).Name : obs.CollidingObject.ToString());
            }

            request.PotentialKnockbackCollisions = new List<PotentialKnockbackCollision>();
            var knockback = _collisionEngine.CalculateKnockbackObstructions(attacker, defender, MAX_KNOCKBACK_DISTANCE_HEXES, others);
            if (knockback != null)
            {
                foreach (var kb in knockback)
                {
                    request.PotentialKnockbackCollisions.Add(new PotentialKnockbackCollision
                    {
                        CollisionObject = kb.CollidingObject is Character ? (kb.CollidingObject as Character).Name : kb.CollidingObject.ToString(),
                        CollisionDistance = (int)Math.Round(kb.CollisionDistance / HEXES_TO_UNITS, MidpointRounding.AwayFromZero)
                    });
                }
            }

            return request;
        }

        private void RemoveFriendlyObstructions(AttackRequest request, List<Character> defenders)
        {
            foreach (Character def in defenders.Where(d =>
                request.Obstructions.Contains(d.Name) &&
                CurrentOnDeckCombatants.Combatants.Any(c => c.CharacterName == d.Name)))
            {
                request.Obstructions.Remove(def.Name);
            }
            foreach (Character def in defenders.Where(d =>
                request.PotentialKnockbackCollisions.Any(c => c.CollisionObject == d.Name) &&
                CurrentOnDeckCombatants.Combatants.Any(c => c.CharacterName == d.Name)))
            {
                var pkc = request.PotentialKnockbackCollisions.First(c => c.CollisionObject == def.Name);
                request.PotentialKnockbackCollisions.Remove(pkc);
            }
        }

        #endregion
    }
}
