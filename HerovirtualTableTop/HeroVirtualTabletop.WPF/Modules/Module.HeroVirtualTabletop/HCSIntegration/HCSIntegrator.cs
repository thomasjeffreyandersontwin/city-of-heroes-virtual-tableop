using Microsoft.Xna. Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
using Module.Shared.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Framework.WPF.Extensions;

namespace Module.HeroVirtualTabletop.HCSIntegration
{
    public interface IHCSIntegrator
    {
        event EventHandler<CustomEventArgs<Object>> SequenceUpdated;
        event EventHandler<CustomEventArgs<Object>> ActiveCharacterUpdated;
        event EventHandler<CustomEventArgs<Object>> AttackResultsUpdated;
        event EventHandler<CustomEventArgs<Object>> SweepAttackResultsUpdated;
        event EventHandler<CustomEventArgs<Object>> EligibleCombatantsUpdated;
        List<Character> InGameCharacters { get; set; }
        void StartIntegration();
        void StopIntegration();
        object GetLatestSequenceInfo();
        void ActivateHeldCharacter(Character heldCharacter);
        void ConfigureAttacks(List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToConfigure, bool sweep = false);
        void PlaySimpleAbility(Character target, AnimatedAbility ability);
        void ConfirmAttack();
        void CancelAttack();
        void ResumeAttack();
        void NotifyStopMovement(CharacterMovement characterMovement, double distanceTravelled);
        float GetMovementDistanceLimit(CharacterMovement activeMovement);
        void AbortAction(List<Character> abortingCharacters);
        AttackInfo GetAttackInfo(string powerName);
    }
    public class HCSIntegrator : IHCSIntegrator
    {
        private CollisionEngine collisionEngine;
        private Timer timer;
        private object lockObjFileWatcher = new object();
        private object lockObjAttackSensor = new object();
        private string currentToken = null;
        private static FileSystemWatcher HCSIntegratorFileWatcher;
        private bool deckUpdatePending = false;
        private bool eligibleCombatantsUpdatePending = false;
        private bool attackInfoUpdatedForCurrentAttack = false;
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
        private void OnSequenceUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (SequenceUpdated != null)
                SequenceUpdated(sender, e);
        }
        private void OnActiveCharacterUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (ActiveCharacterUpdated != null)
                ActiveCharacterUpdated(sender, e);
        }
        private void OnAttackResultsUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (AttackResultsUpdated != null)
                AttackResultsUpdated(sender, e);
        }

        private void OnSweepAttackResultsUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (SweepAttackResultsUpdated != null)
                SweepAttackResultsUpdated(sender, e);
        }
        private void OnEligibleCombatantsUpdated(object sender, CustomEventArgs<Object> e)
        {
            if (EligibleCombatantsUpdated != null)
                EligibleCombatantsUpdated(sender, e);
        }

        private string eventInfoDirectoryPath;
        public string EventInfoDirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(eventInfoDirectoryPath))
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string dir = Path.GetDirectoryName(path);
                    string eventInfoDir = Path.Combine(dir, "EventInfo");
                    if (Directory.Exists(eventInfoDir))
                        eventInfoDirectoryPath = eventInfoDir;
                }

                return eventInfoDirectoryPath;
            }
        }

        public HCSIntegrator()
        {
            collisionEngine = new AnimatedAbilities.CollisionEngine();
            if (HCSIntegratorFileWatcher == null)
            {
                HCSIntegratorFileWatcher = new FileSystemWatcher();
                HCSIntegratorFileWatcher.Path = string.Format("{0}\\", EventInfoDirectoryPath);
                HCSIntegratorFileWatcher.IncludeSubdirectories = false;
                //HCSIntegratorFileWatcher.Filter = "*.info";
                //HCSIntegratorFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                HCSIntegratorFileWatcher.Changed += HCSIntegratorFileWatcher_Changed;
                HCSIntegratorFileWatcher.Created += HCSIntegratorFileWatcher_Changed;
                HCSIntegratorFileWatcher.Renamed += HCSIntegratorFileWatcher_Changed;
            }
            HCSIntegratorFileWatcher.EnableRaisingEvents = false;
            timer = new Timer(timer_elapsed);
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;
        }

        public void StartIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Started;
            if (EventInfoDirectoryPath != null)
                HCSIntegratorFileWatcher.EnableRaisingEvents = true;
            timer.Change(5, 2000);
        }

        public void StopIntegration()
        {
            CurrentIntegrationStatus = HCSIntegrationStatus.Stopped;
            HCSIntegratorFileWatcher.EnableRaisingEvents = false;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        private void HCSIntegratorFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (lockObjFileWatcher)
            {
                string fileExt = Path.GetExtension(e.FullPath);
                if ((e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created) && fileExt == ".info" || fileExt == ".event")
                {
                    if (e.Name == Constants.COMBATANTS_FILE_NAME || e.Name == Constants.CHRONOMETER_FILE_NAME)
                    {
                        if (this.LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                        {
                            UpdateSequence();
                        }
                        else
                        {
                            deckUpdatePending = true;
                        }
                    }
                    else if (e.Name == Constants.ACTIVE_CHARACTER_FILE_NAME)
                    {
                        UpdateActiveCharacter();
                    }
                    //else if (e.Name == Constants.ATTACK_RESULT_FILE_NAME && this.CurrentAttackType != HCSAttackType.None)
                    //{
                    //    if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated || this.LastIntegrationAction == HCSIntegrationAction.AttackResultReceived)
                    //    {
                    //        string currentJson = this.GetAttackResultsFileContents();
                    //        if (currentJson != CurrentAttackResultFileContents)
                    //        {
                    //            ProcessAttackResults();
                    //        }
                    //        else
                    //            this.CurrentAttackResultFileContents = null;
                    //    }
                    //}
                    else if (e.Name == Constants.ELIGIBLE_COMBATANTS_FILE_NAME)
                    {
                        if (this.LastIntegrationAction != HCSIntegrationAction.AttackInitiated)
                        {

                            UpdateEligibleCombatantsInfo();
                        }
                        else
                        {
                            eligibleCombatantsUpdatePending = true;
                        }
                    }
                }
            };
        }

        private void timer_elapsed(object state)
        {
            if (CurrentIntegrationStatus != HCSIntegrationStatus.Stopped)
            {
                string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
                if (File.Exists(pathAttackResult))
                {
                    ProcessAttackResults();
                }
            }
        }

        private void UpdateSequence()
        {
            string currentCombatantsJson = this.GetCurrentCombatantsFileContents();
            string currentChronoMeterJson = this.GetCurrentChronometerFileContents();
            if (currentCombatantsJson != null && currentChronoMeterJson != null && this.CurrentChronometerFileContents != currentChronoMeterJson || this.CurrentOnDeckCombatantsFileContents != currentCombatantsJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.DeckUpdated;
                object sequenceInfo = this.GetLatestSequenceInfo();
                object[] seqArray = sequenceInfo as object[];
                if (seqArray != null && seqArray.Length == 4 && seqArray[0] != null && seqArray[1] != null && seqArray[2] != null && seqArray[3] != null)
                {
                    OnSequenceUpdated(null, new CustomEventArgs<object> { Value = seqArray });
                }
            }
        }

        private void UpdateActiveCharacter()
        {
            string currentActiveCharacterInfoJson = this.GetCurrentActiveCharacterInfoFileContents();
            if (this.CurrentActiveCharacterInfoFileContents != currentActiveCharacterInfoJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.ActiveCharacterUpdated;
                ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
                if (activeCharacterInfo != null)
                    OnActiveCharacterUpdated(null, new CustomEventArgs<object> { Value = activeCharacterInfo });
            }
        }

        private void UpdateEligibleCombatantsInfo()
        {
            string currentEligibleCombatantsJson = this.GetCurrentEligibleCombatantsFileContents();
            if (this.CurrentEligibleCombatantsFileContents != currentEligibleCombatantsJson)
            {
                this.LastIntegrationAction = HCSIntegrationAction.EligibleCombatantsUpdated;
                CombatantsCollection eligibleCombatants = GetCurrentEligibleCombatants();
                if (eligibleCombatants != null)
                    OnEligibleCombatantsUpdated(null, new CustomEventArgs<object> { Value = eligibleCombatants });
            }
        }

        public object GetLatestSequenceInfo()
        {
            Chronometer chronometer = GetCurrentChronometer();
            CombatantsCollection onDeckCombatants = GetCurrentCombatants();
            ActiveCharacterInfo activeCharacterInfo = GetCurrentActiveCharacterInfo();
            CombatantsCollection eligibleCombatants = GetCurrentEligibleCombatants();
            return new object[] { onDeckCombatants, chronometer, activeCharacterInfo, eligibleCombatants };
        }

        private CombatantsCollection GetCurrentCombatants()
        {
            CombatantsCollection onDeckCombatants = null;
            string json = GetCurrentCombatantsFileContents();
            this.CurrentOnDeckCombatantsFileContents = json;
            if (json != null)
                onDeckCombatants = JsonConvert.DeserializeObject<CombatantsCollection>(json);
            this.CurrentOnDeckCombatants = onDeckCombatants;
            return onDeckCombatants;
        }

        int combatantsFileReadRetryCount = 5;
        private string GetCurrentCombatantsFileContents()
        {
            string pathCombatants = Path.Combine(EventInfoDirectoryPath, Constants.COMBATANTS_FILE_NAME);

            string json = null;
            try
            {
                if (File.Exists(pathCombatants))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathCombatants))
                    {
                        json = r.ReadToEnd();
                        combatantsFileReadRetryCount = 5;
                    }
                }
            }
            catch (Exception ex)
            {
                if (combatantsFileReadRetryCount-- > 0)
                {
                    json = GetCurrentCombatantsFileContents();
                }
            }
            return json;
        }

        private Chronometer GetCurrentChronometer()
        {
            Chronometer chronometer = null;
            string json = GetCurrentChronometerFileContents();
            this.CurrentChronometerFileContents = json;
            if (json != null)
                chronometer = JsonConvert.DeserializeObject<Chronometer>(json);
            return chronometer;
        }

        private string GetCurrentChronometerFileContents()
        {
            string pathChronometer = Path.Combine(EventInfoDirectoryPath, Constants.CHRONOMETER_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathChronometer))
                {
                    using (StreamReader r = new StreamReader(pathChronometer))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return json;
        }

        private ActiveCharacterInfo GetCurrentActiveCharacterInfo()
        {
            ActiveCharacterInfo activeCharacterInfo = null;
            string json = this.GetCurrentActiveCharacterInfoFileContents();
            this.CurrentActiveCharacterInfoFileContents = json;
            if (json != null)
                activeCharacterInfo = JsonConvert.DeserializeObject<ActiveCharacterInfo>(json);
            this.CurrentActiveCharacterInfo = activeCharacterInfo;
            this.CurrentActiveCharacterInfo.AbilitiesEligibilityCollection = GetAbilityActivationEligibilityCollection();
            return activeCharacterInfo;
        }

        private string GetCurrentActiveCharacterInfoFileContents()
        {
            string pathActiveCharacter = Path.Combine(EventInfoDirectoryPath, Constants.ACTIVE_CHARACTER_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathActiveCharacter))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathActiveCharacter))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return json;
        }
        private CombatantsCollection GetCurrentEligibleCombatants()
        {
            CombatantsCollection eligibleCombatants = null;
            string json = GetCurrentEligibleCombatantsFileContents();
            this.CurrentEligibleCombatantsFileContents = json;
            if (json != null)
                eligibleCombatants = JsonConvert.DeserializeObject<CombatantsCollection>(json);
            return eligibleCombatants;
        }
        int eligibleCombatantsFileReadRetryCount = 5;
        private string GetCurrentEligibleCombatantsFileContents()
        {
            string pathEligibleCombatants = Path.Combine(EventInfoDirectoryPath, Constants.ELIGIBLE_COMBATANTS_FILE_NAME);

            string json = null;
            try
            {
                if (File.Exists(pathEligibleCombatants))
                {
                    System.Threading.Thread.Sleep(500);
                    using (StreamReader r = new StreamReader(pathEligibleCombatants))
                    {
                        json = r.ReadToEnd();
                        eligibleCombatantsFileReadRetryCount = 5;
                    }
                }
            }
            catch (Exception ex)
            {
                if (eligibleCombatantsFileReadRetryCount-- > 0)
                {
                    json = GetCurrentEligibleCombatantsFileContents();
                }
            }
            return json;
        }

        private List<AbilityActivationEligibility> GetAbilityActivationEligibilityCollection()
        {
            List<AbilityActivationEligibility> eligibilityCollection = new List<HCSIntegration.AbilityActivationEligibility>();
            Dictionary<string, bool> abilityEligibilityDictionary = new Dictionary<string, bool>();
            if (this.CurrentActiveCharacterInfo.Powers != null)
            {
                string json = this.CurrentActiveCharacterInfo.Powers.ToString();
                abilityEligibilityDictionary = GetAbilityEligiblityDictionary(json);
            }
            if (this.CurrentActiveCharacterInfo.Defaults != null)
            {
                string json = this.CurrentActiveCharacterInfo.Defaults.ToString();
                var abilityEligibilityDictionaryOther = GetAbilityEligiblityDictionary(json);
                abilityEligibilityDictionary.AddRange(abilityEligibilityDictionaryOther);
            }
            foreach (var entry in abilityEligibilityDictionary)
            {
                if (!eligibilityCollection.Any(e => e.AbilityName == entry.Key))
                    eligibilityCollection.Add(new AbilityActivationEligibility { AbilityName = entry.Key, IsEnabled = entry.Value });
            }

            return eligibilityCollection;
        }

        private Dictionary<string, bool> GetAbilityEligiblityDictionary(string json)
        {
            Dictionary<string, bool> abilityEligibilityDictionary = new Dictionary<string, bool>();
            JToken outer = JToken.Parse(json);
            foreach (var obj in outer.Children())
            {
                JProperty jProp = obj as JProperty;
                if (jProp != null)
                {
                    JObject jObj = jProp.Value as JObject;
                    var values = jObj.Properties().Where(p => p.Name == "Is Enabled").Select(p => p.Value);
                    if (values.Count() > 0)
                    {
                        bool val = values.First().Value<bool>();
                        if (!abilityEligibilityDictionary.ContainsKey(jProp.Name))
                            abilityEligibilityDictionary.Add(jProp.Name, val);
                    }
                }
            }

            return abilityEligibilityDictionary;
        }

        private string GetAttackResultsFileContents()
        {
            string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathAttackResult))
                {
                    System.Threading.Thread.Sleep(1000);
                    using (StreamReader r = new StreamReader(pathAttackResult))
                    {
                        json = r.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return json;
        }
        private AttackResponseBase GetAttackResults()
        {
            AttackResponseBase attackResult = null;
            string json = GetAttackResultsFileContents();
            if(!string.IsNullOrEmpty(json))
            {
                if (json != this.CurrentAttackResultFileContents)
                    this.attackInfoUpdatedForCurrentAttack = true;
                this.CurrentAttackResultFileContents = json;
                switch (this.CurrentAttackType)
                {
                    case HCSAttackType.Area:
                        attackResult = JsonConvert.DeserializeObject<AreaAttackResponse>(json);
                        break;
                    case HCSAttackType.Vanilla:
                        attackResult = JsonConvert.DeserializeObject<AttackResponse>(json);
                        break;
                    case HCSAttackType.AutoFire:
                        attackResult = JsonConvert.DeserializeObject<AutoFireAttackResponse>(json);
                        break;
                    case HCSAttackType.Sweep:
                        attackResult = GetSweepAttackResults(json);
                        break;
                    default:
                        break;
                }
            }

            return attackResult;
        }

        private AttackResponseBase GetSweepAttackResults(string json)
        {
            SweepAttackResponse sweepResponse = new SweepAttackResponse();
            sweepResponse.Attacks = new List<AttackResponseBase>();
            dynamic sweepResponseObj = JsonConvert.DeserializeObject(json);
            sweepResponse.Token = sweepResponseObj.Token;
            JToken responseToekn = JToken.Parse(json);
            var targetsToken = responseToekn.Children().Where(t => t is JProperty && (t as JProperty).Name == "Affected Targets").FirstOrDefault();
            JProperty targetsProperty = targetsToken.Value<JProperty>();
            var value = targetsProperty.Value;
            JArray attacks = value.Value<JArray>();
            //dynamic attacks = sweepResponseObj.Attacks;
            if(attacks != null)
            {
                foreach(var attack in attacks)
                {
                    AttackResponseBase attackResponse = null;
                    var jsonString = JsonConvert.SerializeObject(attack);
                    //AttackResponse[] attackResponses = JsonConvert.DeserializeObject<AttackResponse[]>(jsonString);
                    //attackResponse = attackResponses.FirstOrDefault();
                    attackResponse = JsonConvert.DeserializeObject<AttackResponse>(jsonString);

                    if (attackResponse != null)
                    {
                        sweepResponse.Attacks.Add(attackResponse);
                    }
                }
            }

            return sweepResponse;
        }

        private bool ProcessAttackResults()
        {
            lock (lockObjAttackSensor)
            {
                AttackResponseBase attackResult = GetAttackResults();
                bool resultsReceivedWithToken = attackResult != null && attackResult.Token != null && attackResult.Token == this.currentToken;
                if (resultsReceivedWithToken)
                {
                    if (this.LastIntegrationAction == HCSIntegrationAction.AttackInitiated)
                    {
                        this.LastIntegrationAction = HCSIntegrationAction.AttackResultReceived;
                        attackInfoUpdatedForCurrentAttack = false;
                        ProcessAttackResults(attackResult);
                    }
                    else if (this.attackInfoUpdatedForCurrentAttack)
                    {
                        attackInfoUpdatedForCurrentAttack = false;
                        ProcessAttackResults(attackResult);
                    }

                    if (deckUpdatePending)
                    {
                        deckUpdatePending = false;
                        UpdateSequence();
                    }
                    if (eligibleCombatantsUpdatePending)
                    {
                        eligibleCombatantsUpdatePending = false;
                        UpdateEligibleCombatantsInfo();
                    }
                }
                else if (attackResult != null && attackResult.Token == null)
                {
                    //DeleteCurrentResultFile();
                }
                else
                {

                }

                return resultsReceivedWithToken;
            }
        }

        private void ProcessAttackResults(AttackResponseBase attackResult)
        {
            this.CurrentAttackResult = attackResult;
            if(this.CurrentAttackType != HCSAttackType.Sweep)
            {
                List<Character> attackTargets = ParseAttackTargetsFromAttackResult(attackResult);
                if(attackTargets.Count == 0)
                {

                }
                //DeleteCurrentResultFile();
                OnAttackResultsUpdated(this, new CustomEventArgs<object> { Value = attackTargets });
            }
            else
            {
                SweepAttackResponse sweepResponse = attackResult as SweepAttackResponse;
                List<Tuple<Guid, List<Character>>> attacksWithTargets = this.ParseAttackTargetsFromSweepAttackResponse(sweepResponse);
                OnSweepAttackResultsUpdated(this, new CustomEventArgs<object> { Value = attacksWithTargets });
            }
        }

        int numDeleteRetry = 5;
        private void DeleteCurrentResultFile()
        {
            string pathAttackResult = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            string json = null;
            try
            {
                if (File.Exists(pathAttackResult))
                {
                    File.Delete(pathAttackResult);
                    numDeleteRetry = 5;
                }
            }
            catch (IOException ioex)
            {
                numDeleteRetry--;
                if (numDeleteRetry > 0)
                {
                    Thread.Sleep(1000);
                    DeleteCurrentResultFile();
                }
            }
            catch
            {

            }
        }
        private void ParseEffects(AttackConfiguration attackConfig, List<string> effects)
        {
            if (effects != null)
            {
                if (effects.Contains("Stunned"))
                    attackConfig.IsStunned = true;
                if (effects.Contains("Unconscious"))
                    attackConfig.IsUnconcious = true;
                if (effects.Contains("Dying"))
                    attackConfig.IsDying = true;
                if (effects.Contains("Dead"))
                    attackConfig.IsDead = true;
                if (effects.Contains("Partially Destroyed"))
                    attackConfig.IsPartiallyDestryoed = true;
                if (effects.Contains("Destroyed"))
                    attackConfig.IsDestroyed = true;
            }
        }

        private List<Character> ParseAttackTargetsFromAttackResult(AttackResponseBase attackResult)
        {
            List<Character> targets = null;
            switch (this.CurrentAttackType)
            {
                case HCSAttackType.Area:
                    //targets = ParseAttackTargetsFromAreaAttackResult(attackResult as AttackAreaTargetResponse);
                    targets = ParseAttackTargetsFromMultiTargetAttackResponse(attackResult as AreaAttackResponse);
                    break;
                case HCSAttackType.Vanilla:
                    targets = ParseAttackTargetsFromAttackResponse(attackResult as AttackResponse);
                    break;
                case HCSAttackType.AutoFire:
                    targets = ParseAttackTargetsFromMultiTargetAttackResponse(attackResult as AutoFireAttackResponse);
                    break;
                default:
                    break;
            }

            return targets;
        }

        private List<Character> ParseAttackTargetsFromAttackResponse(AttackResponse attackResponse, AttackResponseBase parentResponse = null)
        {
            List<Character> attackTargets = new List<Character>();
            Character primaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == attackResponse.Defender.Name);
            string abilityName = attackResponse.Ability != null ? attackResponse.Ability : parentResponse != null ? parentResponse.Ability : null;
            Attack respondedAttack = this.AttacksToConfigure.Where(ac => ac.Item1.Name == abilityName).Select(ac => ac.Item1).First();
            Guid respondedAttackKey = this.GetConfigKeyForRespondedAttack(attackResponse, parentResponse);
            if (primaryTarget != null)
            {
                attackTargets.Add(primaryTarget);

                AttackConfiguration attackConfigPrimary = primaryTarget.AttackConfigurationMap.ContainsKey(respondedAttackKey) ? primaryTarget.AttackConfigurationMap[respondedAttackKey].Item2 : new AttackConfiguration();

                attackConfigPrimary.IsHit = attackResponse.IsHit;
                attackConfigPrimary.Body = (int?)attackResponse.Defender.Body.Current;
                attackConfigPrimary.Stun = (int?)attackResponse.Defender.Stun.Current;
                if (attackResponse.MoveBeforeAttackRequired.HasValue)
                    attackConfigPrimary.MoveAttackerToTarget = attackResponse.MoveBeforeAttackRequired.Value;
                List<Character> secondaryTargets = new List<Character>();
                
                if (attackResponse.KnockbackResult != null && attackResponse.KnockbackResult.Distance != 0)
                {
                    attackConfigPrimary.IsKnockedBack = true;
                    attackConfigPrimary.KnockBackDistance = attackResponse.KnockbackResult.Distance;
                    
                    if (attackResponse.KnockbackResult.Collisions != null && attackResponse.KnockbackResult.Collisions.Count > 0)
                    {
                        attackConfigPrimary.ObstructingCharacters = new List<Characters.Character>();
                        foreach (var collision in attackResponse.KnockbackResult.Collisions)
                        {
                            var secondaryTarget = this.InGameCharacters.FirstOrDefault(c => c.Name == collision.CollidingObject.Name);
                            if(secondaryTarget != null)
                            {
                                AttackConfiguration attackConfigSecondary = secondaryTarget.AttackConfigurationMap.ContainsKey(respondedAttackKey) ? secondaryTarget.AttackConfigurationMap[respondedAttackKey].Item2 : new AnimatedAbilities.AttackConfiguration();
                                attackConfigSecondary.Body = (int)collision.CollisionDamageResults.Body;
                                if (collision.CollidingObject.Effects != null
                                && collision.CollidingObject.Effects.Count > 0)
                                {
                                    ParseEffects(attackConfigSecondary, collision.CollidingObject.Effects);
                                }
                                attackTargets.Add(secondaryTarget);
                                attackConfigSecondary.IsHit = true;
                                attackConfigSecondary.PrimaryTargetCharacter = primaryTarget;
                                attackConfigSecondary.ObstructingCharacters = null;
                                attackConfigSecondary.IsKnockbackObstruction = true;
                                secondaryTarget.AddAttackConfiguration(respondedAttack, attackConfigSecondary, respondedAttackKey);
                                attackConfigPrimary.ObstructingCharacters.Add(secondaryTarget);
                            }
                            
                        }
                    }
                }

                if (attackResponse.ObstructionDamageResults != null && attackResponse.ObstructionDamageResults.Count > 0)
                {
                    if(attackConfigPrimary.ObstructingCharacters == null)
                        attackConfigPrimary.ObstructingCharacters = new List<Characters.Character>();
                    foreach (var obstruction in attackResponse.ObstructionDamageResults.Where(odr => odr.IsHit && odr.Defender != null))
                    {
                        List<Character> obstructingTargets = ParseAttackTargetsFromAttackResponse(obstruction);
                        secondaryTargets.AddRange(obstructingTargets);
                    }
                    foreach(var secondaryTarget in secondaryTargets)
                    {
                        if (!attackTargets.Contains(secondaryTarget))
                        {
                            attackTargets.Add(secondaryTarget);
                            secondaryTarget.AttackConfigurationMap[respondedAttackKey].Item2.PrimaryTargetCharacter = primaryTarget;
                            attackConfigPrimary.ObstructingCharacters.Add(secondaryTarget);
                        }
                    }
                }
                ParseEffects(attackConfigPrimary, attackResponse.Defender.Effects);
                primaryTarget.AddAttackConfiguration(respondedAttack, attackConfigPrimary, respondedAttackKey);
                if(!this.RespondedConfigKeysWithDefenders.Any(rcd =>rcd.Item1 == respondedAttackKey))
                    this.RespondedConfigKeysWithDefenders.Add(new Tuple<Guid, Character>(respondedAttackKey, primaryTarget));
            }
            return attackTargets;
        }

        private List<Character> ParseAttackTargetsFromMultiTargetAttackResponse(MultiTargetAttackResponse multiAttackResponse)
        {
            List<Character> attackTargets = new List<Character>();

            foreach (var attackResponse in multiAttackResponse.Targets)
            {
                Attack respondedAttack = this.AttacksToConfigure.Where(ac => ac.Item1.Name == multiAttackResponse.Ability).Select(ac => ac.Item1).First();
                Guid respondedAttackKey = this.GetConfigKeyForRespondedAttack(attackResponse, multiAttackResponse);
                var targetsForThisResponse = ParseAttackTargetsFromAttackResponse(attackResponse, multiAttackResponse);
                foreach (var targetForThisResponse in targetsForThisResponse)
                {
                    if (attackResponse.MoveBeforeAttackRequired.HasValue && attackResponse.MoveBeforeAttackRequired.Value)
                    {
                        targetForThisResponse.AttackConfigurationMap[respondedAttackKey].Item2.IsCenterTarget = false;
                        targetForThisResponse.AttackConfigurationMap[respondedAttackKey].Item2.MoveAttackerToTarget = true;
                    }

                    if (!attackTargets.Contains(targetForThisResponse))
                        attackTargets.Add(targetForThisResponse);
                }
            }

            return attackTargets;
        }

        private List<Tuple<Guid, List<Character>>> ParseAttackTargetsFromSweepAttackResponse(SweepAttackResponse sweepAttackResponse)
        {
            List<Tuple<Guid, List<Character>>> attacksWithDefenders = new List<Tuple<Guid, List<Character>>>();
            foreach(var response in sweepAttackResponse.Attacks)
            {
                if(response is AreaAttackResponse)
                {
                    List<Character> targets = ParseAttackTargetsFromMultiTargetAttackResponse(response as AreaAttackResponse);
                    attacksWithDefenders.Add(new Tuple<Guid, List<Character>>(new Guid(response.Token), targets));
                }
                else if(response is AttackResponse)
                {
                    List<Character> targets = ParseAttackTargetsFromAttackResponse(response as AttackResponse);
                    attacksWithDefenders.Add(new Tuple<Guid, List<Character>>(new Guid(response.Token), targets));
                }
            }

            return attacksWithDefenders;
        }

        private Guid GetConfigKeyForRespondedAttack(AttackResponse attackResponse, AttackResponseBase parentResponse = null)
        {
            Guid currentConfigKey = Guid.Empty;
            if (!string.IsNullOrEmpty(attackResponse.Token) && parentResponse != null) // inner attacks for sweep
                currentConfigKey = new Guid(attackResponse.Token);
            //else if (parentResponse != null && !string.IsNullOrEmpty(parentResponse.Token))
            //    currentConfigKey = new Guid(parentResponse.Token);
            else
            {
                string abilityName = (attackResponse.Ability != null ? attackResponse.Ability : (parentResponse != null ? parentResponse.Ability : null));                                                                                    
                var respondedAttackConfigKeys = this.AttacksToConfigure.Where(ac => ac.Item1.Name == abilityName).Select(ac => ac.Item4).ToList();
                if (respondedAttackConfigKeys.Count == 1)
                {
                    currentConfigKey = respondedAttackConfigKeys.First();
                }
                else if (respondedAttackConfigKeys.Count > 1)
                {
                    foreach (var respondedKey in respondedAttackConfigKeys)
                    {
                        if (!this.RespondedConfigKeysWithDefenders.Any(rcd => rcd.Item1 == respondedKey && rcd.Item2.Name == attackResponse.Defender.Name))
                        {
                            currentConfigKey = respondedKey;
                            break;
                        }
                    }
                }
            }
            
            return currentConfigKey;
        }

        private float ParseDistanceLimitForMovement(string movementName, bool nonCombat)
        {
            string limitString = GetDistanceLimitString(movementName);
            float limit = 0;
            string[] tokens = limitString.Split(',');
            if (tokens.Length == 3)
            {
                string tokenToConsider = nonCombat ? tokens[2] : tokens[1];
                string[] numbers = Regex.Split(tokenToConsider, @"\D+");
                if (numbers.Length > 0)
                {
                    int k;
                    foreach (string n in numbers)
                    {
                        if (int.TryParse(n, out k))
                        {
                            limit = int.Parse(n);
                            break;
                        }
                    }
                }
            }

            return limit;
        }

        private string GetDistanceLimitString(string movementName)
        {
            string limitString = "";
            JToken outer = null;
            if (this.CurrentActiveCharacterInfo.Powers != null)
                outer = JToken.Parse(this.CurrentActiveCharacterInfo.Powers.ToString());
            bool movementExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            if (!movementExists && this.CurrentActiveCharacterInfo.Defaults != null)
            {
                outer = JToken.Parse(this.CurrentActiveCharacterInfo.Defaults.ToString());
                movementExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == movementName);
            }
            if (movementExists)
            {
                JObject inner = outer[movementName].Value<JObject>();
                dynamic d = inner;
                limitString = d.Description;
                // or the following works too
                //var values = inner.Properties().Where(p => p.Name == "Description").Select(p => p.Value);
                //foreach(var value in values)
                //{
                //    string val = value.Value<string>();
                //}
            }

            return limitString;
        }

        private AttackShape ParseAreaAttackShape(string shapeString)
        {
            AttackShape attackShape = AttackShape.None;

            if (shapeString.ToLower().Contains("radius"))
                attackShape = AttackShape.Radius;
            else if (shapeString.ToLower().Contains("one-hex"))
                attackShape = AttackShape.Line;
            else if (shapeString.ToLower().Contains("cone"))
                attackShape = AttackShape.Cone;

            return attackShape;
        }

        private void WaitForAttackUpdates()
        {
            //timer.Change(2000, Timeout.Infinite);
        }

        public void PlaySimpleAbility(Character target, AnimatedAbility ability)
        {
            GenerateSimpleAbilityMessage(target, ability);
        }

        public void NotifyStopMovement(CharacterMovement characterMovement, double distanceTravelled)
        {
            GenerateSimpleMovementMessage(characterMovement, distanceTravelled);
        }

        public float GetMovementDistanceLimit(CharacterMovement activeMovement)
        {
            float maxDistance = 0f;
            if (this.CurrentActiveCharacterInfo == null)
                this.GetCurrentActiveCharacterInfo();

            if (this.CurrentActiveCharacterInfo.Powers != null)
            {
                string movementName = GetMovementName(activeMovement.Name.ToLower());
                maxDistance = ParseDistanceLimitForMovement(movementName, activeMovement.IsNonCombatMovement);
            }

            return maxDistance;
        }

        public AttackInfo GetAttackInfo(string powerName)
        {
            AttackInfo attackInfo = new AttackInfo { AttackShape = AttackShape.None, AttackType = AttackType.None, Range = 0, TargetSelective = false, CanSpread = false, AutoFireMaxShots = 0};
            if (this.CurrentActiveCharacterInfo == null)
                this.GetCurrentActiveCharacterInfo();
            if (this.CurrentActiveCharacterInfo.Defaults != null)
            {
                JToken outer = JToken.Parse(this.CurrentActiveCharacterInfo.Defaults.ToString());
                bool powerExists = outer.Children().Any(c => (c is JProperty) && (c as JProperty).Name == powerName);
                if (powerExists)
                {
                    JObject inner = outer[powerName].Value<JObject>();
                    dynamic d = inner;
                    var description = d.Description;
                    if(description != null && description.ToString().Contains("Area Effect") && d.Advantages != null)
                    {
                        JObject advantagesObj = d.Advantages;
                        var areaEffectObj = advantagesObj.Properties().Where(p => p.Name == "Area Effect").Select(p => p.Value);
                        if (areaEffectObj != null && areaEffectObj.Count() > 0)
                        {
                            dynamic areaEffectInfo = areaEffectObj.First();
                            attackInfo.AttackType = AttackType.Area;
                            if (areaEffectInfo.Range != null)
                                attackInfo.Range = areaEffectInfo.Range;
                            if (areaEffectInfo.Type != null && areaEffectInfo.Type == "Selective")
                                attackInfo.TargetSelective = true;
                            if (areaEffectInfo.Shape != null && !string.IsNullOrEmpty(areaEffectInfo.Shape.ToString()))
                                attackInfo.AttackShape = ParseAreaAttackShape(areaEffectInfo.Shape.ToString());
                            else if (areaEffectInfo.Description != null && !string.IsNullOrEmpty(areaEffectInfo.Description.ToString()))
                                attackInfo.AttackShape = ParseAreaAttackShape(areaEffectInfo.Description.ToString());
                        }
                    }
                    else if(description != null && description.ToString().Contains("AF (") && d.Advantages != null)
                    {
                        JObject advantagesObj = d.Advantages;
                        var areaEffectObj = advantagesObj.Properties().Where(p => p.Name == "Autofire").Select(p => p.Value);
                        if (areaEffectObj != null && areaEffectObj.Count() > 0)
                        {
                            dynamic areaEffectInfo = areaEffectObj.First();
                            attackInfo.AttackType = AttackType.AutoFire;
                            if (areaEffectInfo.MaxShots != null)
                                attackInfo.AutoFireMaxShots = areaEffectInfo.MaxShots;
                        }
                    }

                    JObject detailsObj = d.Details;
                    if(detailsObj != null)
                    {
                        var isRangedObj = detailsObj.Properties().Where(p => p.Name == "IsRanged").Select(p => p.Value);
                        if (isRangedObj != null && isRangedObj.Count() > 0)
                        {
                            dynamic rangeInfo = isRangedObj.First();
                            if(rangeInfo != null && rangeInfo.Value != null)
                            {
                                bool b;
                                if (Boolean.TryParse(rangeInfo.Value.ToString(), out b))
                                    attackInfo.IsRanged = b;
                            }
                        }

                        var canSpreadObj = detailsObj.Properties().Where(p => p.Name == "CanSpread").Select(p => p.Value);
                        if (canSpreadObj != null && canSpreadObj.Count() > 0)
                        {
                            dynamic spreadInfo = canSpreadObj.First();
                            if (spreadInfo != null && spreadInfo.Value != null)
                            {
                                bool b;
                                if (Boolean.TryParse(spreadInfo.Value.ToString(), out b))
                                    attackInfo.CanSpread = b;
                            }
                        }
                    }
                    if (d.IsRanged != null)
                    {
                        bool b;
                        if (Boolean.TryParse(d.IsRanged.ToString(), out b))
                            attackInfo.IsRanged = b;
                    }
                    if (d.CanSpread != null)
                    {
                        bool b;
                        if (Boolean.TryParse(d.CanSpread.ToString(), out b))
                            attackInfo.CanSpread = b;
                    }
                }
            }

            return attackInfo;
        }

        public void ConfigureAttacks(List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToConfigure, bool sweep = false)
        {
            this.ResetAttackParameters();
            this.AttacksToConfigure = attacksToConfigure.ToList();
            if(!sweep)  
            {
                var attackToConfigure = attacksToConfigure[0];
                ConfigureAttack(attackToConfigure.Item1, attackToConfigure.Item2, attackToConfigure.Item3, attackToConfigure.Item4);
            }
            else   
            {
                //sweep attack
                this.ConfigureSweepAttack();
            }       
        }
        private void ConfigureAttack(Attack attack, List<Character> attackers, List<Character> defenders, Guid configKey)
        {
            if (attack.AttackInfo != null && attack.AttackInfo.AttackType == AttackType.Area)
            {
                if (attackers.Count > 1)// Gang Area Attack
                {

                }
                else// Minion Area Attack
                {
                    ConfigureAreaAttack(attack, attackers[0], defenders, configKey);
                }
            }
            else if (attack.IsAutoFire)
            {
                this.ConfigureAutoFireAttack(attack, attackers[0], defenders, configKey);
            }
            else
            {
                if (attackers.Count > 1)// Gang Attack
                {

                }
                else
                {
                    if (defenders.Count > 1) // Multi Target Vanilla Attack
                    {

                    }
                    else // Single Target Vanilla Attack
                    {
                        ConfigureVanillaAttack(attack, attackers[0], defenders[0], configKey);
                    }
                }
            }
        }

        private void ConfigureSweepAttack()
        {
            this.CurrentAttackType = HCSAttackType.Sweep;
            this.currentToken = Guid.NewGuid().ToString();
            SweepAttackRequest sweepAttackRequest = new HCSIntegration.SweepAttackRequest();
            sweepAttackRequest.Token = this.currentToken;
            sweepAttackRequest.Type = Constants.SWEEP_ATTACK_INITIATION_TYPE_NAME;
            sweepAttackRequest.Ability = Constants.SWEEP_ATTACK_INITIATION_TYPE_NAME;
            sweepAttackRequest.Attacks = new List<AttackRequestBase>();

            foreach (var tuple in this.AttacksToConfigure)
            {
                Attack attack = tuple.Item1;
                List<Character> attackers = tuple.Item2;
                List<Character> defenders = tuple.Item3;
                Guid configKey = tuple.Item4;
                if (attack.AttackInfo != null && attack.AttackInfo.AttackType == AttackType.Area)
                {
                    AreaAttackRequest areaAttackRequest = GetAreaAttackRequest(attack, attackers[0], defenders, configKey);
                    areaAttackRequest.Type = Constants.AREA_ATTACK_INITIATION_TYPE_NAME;
                    areaAttackRequest.Token = configKey.ToString();
                    sweepAttackRequest.Attacks.Add(areaAttackRequest);
                }
                else if (!attack.IsAutoFire)
                {
                    AttackRequest attackRequest = GetVanillaAttackRequest(attack, attackers.First(), defenders.First(), configKey);
                    attackRequest.Type = Constants.ATTACK_INITIATION_TYPE_NAME;
                    attackRequest.Token = configKey.ToString();
                    sweepAttackRequest.Attacks.Add(attackRequest);
                }
            }

            WriteToAbilityActivatedFile(sweepAttackRequest);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //GenerateSampleSweepAttackResult(sweepAttackRequest);
        }
        private void ConfigureVanillaAttack(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            this.CurrentAttackType = HCSAttackType.Vanilla;
            GenerateAttackInitiationMessage(attack, attacker, defender, configKey);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //WaitForAttackUpdates();
        }

        private void ConfigureAreaAttack(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            this.CurrentAttackType = HCSAttackType.Area;
            GenerateAreaAttackInitiationMessage(attack, attacker, defenders, configKey);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //WaitForAttackUpdates();
        }
        private void ConfigureAutoFireAttack(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            this.CurrentAttackType = HCSAttackType.AutoFire;
            GenerateAutoFireAttackInitiationMessage(attack, attacker, defenders, configKey);
            this.LastIntegrationAction = HCSIntegrationAction.AttackInitiated;
            //WaitForAttackUpdates();
        }

        public void ConfirmAttack()
        {
            this.GenerateAttackConfirmationMessage(true);
            ResetAttackParameters();
        }

        public void CancelAttack()
        {
            this.GenerateAttackConfirmationMessage(false);
            ResetAttackParameters();
        }

        public void ResumeAttack()
        {
            this.CurrentAttackType = this.savedAttackType;
            var attackResults = this.GetAttackResults();
            if (attackResults != null)
                ProcessAttackResults(attackResults);
        }
        private HCSAttackType savedAttackType = HCSAttackType.None;
        private object areaEffectObj;

        public void AbortAction(List<Character> abortingCharacters)
        {
            foreach (Character character in abortingCharacters)
                GenerateAbortActionMessage(character);
            this.savedAttackType = this.CurrentAttackType;
        }

        public void ActivateHeldCharacter(Character heldCharacter)
        {
            GenerateActivateHeldCharacterMessage(heldCharacter);
            this.savedAttackType = this.CurrentAttackType;
        }

        private void ResetAttackParameters()
        {
            this.CurrentAttackResult = null;
            this.CurrentAttackResultFileContents = null;
            this.CurrentAttackType = HCSAttackType.None;
            this.AttacksToConfigure = new List<Tuple<AnimatedAbilities.Attack, List<Characters.Character>, List<Characters.Character>, Guid>>();
            this.RespondedConfigKeysWithDefenders = new List<Tuple<Guid, Character>>();
        }

        private AttackRequest GetVanillaAttackRequest(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            AttackRequest vanillaRequest = GetAttackRequest(attack, attacker, defender);
            vanillaRequest.Ability = attack.Name;

            return vanillaRequest;
        }

        private AreaAttackRequest GetAreaAttackRequest(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            AreaAttackRequest areaAttackRequest = new AreaAttackRequest
            {
                Ability = attack.Name,
            };

            areaAttackRequest.Targets = new List<AttackRequest>();
            foreach (Character defender in defenders)
            {
                AttackRequest areaEffectTarget = GetAttackRequest(attack, attacker, defender);
                foreach(Character def in defenders.Where(d => areaEffectTarget.Obstructions.Contains(d.Name) && this.CurrentOnDeckCombatants.Combatants.Any(c => c.CharacterName == d.Name)))
                {
                    areaEffectTarget.Obstructions.Remove(def.Name);
                }
                foreach (Character def in defenders.Where(d => areaEffectTarget.PotentialKnockbackCollisions.Any(c => c.CollisionObject == d.Name) && this.CurrentOnDeckCombatants.Combatants.Any(c => c.CharacterName == d.Name)))
                {
                    PotentialKnockbackCollision pkc = areaEffectTarget.PotentialKnockbackCollisions.First(c => c.CollisionObject == def.Name);
                    areaEffectTarget.PotentialKnockbackCollisions.Remove(pkc);
                }
                areaAttackRequest.Targets.Add(areaEffectTarget);
            }
            if (defenders.Any(d => d.AttackConfigurationMap[configKey].Item2.IsCenterTarget))
                areaAttackRequest.Center = defenders.First(d => d.AttackConfigurationMap[configKey].Item2.IsCenterTarget).Name;
            else
                areaAttackRequest.Center = Constants.HEX;
            return areaAttackRequest;
        }

        private AutoFireAttackRequest GetAutoFireAttackRequest(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            AutoFireAttackRequest autoFireAttackRequest = new AutoFireAttackRequest
            {
                Ability = attack.Name,
            };
            Vector3 possibleFacing = defenders[0].CurrentPositionVector - attacker.CurrentPositionVector;
            Vector3 left = Helper.GetAdjacentPoint(attacker.CurrentPositionVector, possibleFacing, true, 1000);
            Vector3 right = Helper.GetAdjacentPoint(attacker.CurrentPositionVector, possibleFacing, false, 1000);
            Vector3 referenceVector = right - left;
            List<Vector3> intersectionPointsOfDefenderProjections = new List<Vector3>();
            autoFireAttackRequest.Targets = new List<AttackRequest>();
            foreach (Character defender in defenders)
            {
                int assignedShots = defender.AttackConfigurationMap[configKey].Item2.NumberOfShotsAssigned;
                for(int i = 0; i < assignedShots; i++)
                {
                    AttackRequest autoFireTarget = GetAttackRequest(attack, attacker, defender);
                    foreach (Character def in defenders.Where(d => autoFireTarget.Obstructions.Contains(d.Name) && this.CurrentOnDeckCombatants.Combatants.Any(c => c.CharacterName == d.Name)))
                    {
                        autoFireTarget.Obstructions.Remove(def.Name);
                    }
                    foreach (Character def in defenders.Where(d => autoFireTarget.PotentialKnockbackCollisions.Any(c => c.CollisionObject == d.Name)))
                    {
                        PotentialKnockbackCollision pkc = autoFireTarget.PotentialKnockbackCollisions.First(c => c.CollisionObject == def.Name);
                        autoFireTarget.PotentialKnockbackCollisions.Remove(pkc);
                    }
                    autoFireAttackRequest.Targets.Add(autoFireTarget);
                }
                
                Vector3 projectingVector = defender.CurrentPositionVector - left;
                Vector3 intersectionPoint = Helper.GetIntersectionPointOfPerpendicularProjectionVectorOnAnotherVector(referenceVector, projectingVector);
                intersectionPointsOfDefenderProjections.Add(intersectionPoint);
            }
            var maxDist = Helper.CalculateMaximumDistanceBetweenTwoPointsInASetOfPoints(intersectionPointsOfDefenderProjections.ToArray());
            autoFireAttackRequest.Width = (int)Math.Round(maxDist / 8f, MidpointRounding.AwayFromZero);
            autoFireAttackRequest.Shots = attack.AttackInfo.AutoFireMaxShots;
            if(defenders.Count > 1)
                autoFireAttackRequest.Spray = true;

            return autoFireAttackRequest;
        }

        private AttackRequest GetAttackRequest(Attack attack, Character attacker, Character defender)
        {
            AttackRequest attackRequest = new HCSIntegration.AttackRequest();
            float range = Vector3.Distance(attacker.CurrentPositionVector, defender.CurrentPositionVector);
            attackRequest.Range = (int)Math.Round((range) / 8f, MidpointRounding.AwayFromZero);
            attackRequest.Defender = defender.Name;
            List<Character> otherCharacters = this.InGameCharacters.Where(c => c != attacker && c != defender).ToList();
            attackRequest.ToHitModifiers = new ToHitModifiers();
            bool attackerInFront = Helper.DetermineIfOneObjectIsInFrontOfAnotherObject(defender.CurrentPositionVector, defender.CurrentFacingVector, attacker.CurrentPositionVector);
            attackRequest.ToHitModifiers.FromBehind = !attackerInFront;
            var obstructions = collisionEngine.FindObstructingObjects(attacker, defender, otherCharacters);
            attackRequest.Obstructions = new List<string>();
            if (obstructions != null && obstructions.Count > 0)
            {
                foreach (var obstruction in obstructions)
                {
                    if (obstruction.CollidingObject is Character)
                        attackRequest.Obstructions.Add((obstruction.CollidingObject as Character).Name);
                    else
                        attackRequest.Obstructions.Add(obstruction.CollidingObject.ToString());
                }
            }

            attackRequest.PotentialKnockbackCollisions = new List<HCSIntegration.PotentialKnockbackCollision>();
            var knockbackObstacles = collisionEngine.CalculateKnockbackObstructions(attacker, defender, 50, otherCharacters);
            if (knockbackObstacles != null && knockbackObstacles.Count > 0)
            {

                foreach (var knockbackObstacle in knockbackObstacles)
                {
                    attackRequest.PotentialKnockbackCollisions.Add(
                        new PotentialKnockbackCollision
                        {
                            CollisionObject = knockbackObstacle.CollidingObject is Character ? (knockbackObstacle.CollidingObject as Character).Name : knockbackObstacle.CollidingObject.ToString(),
                            CollisionDistance = (int)Math.Round((knockbackObstacle.CollisionDistance) / 8f, MidpointRounding.AwayFromZero)
                        }
                        );
                }

            }

            return attackRequest;
        }

        private void WriteToAbilityActivatedFile(object jsonObject)
        {
            Action d = delegate ()
            {
                string pathAttackActivated = Path.Combine(EventInfoDirectoryPath, Constants.ABILITY_ACTIVATED_FILE_NAME);
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Formatting = Formatting.Indented;
                using (StreamWriter streamWriter = new StreamWriter(pathAttackActivated))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        serializer.Serialize(jsonWriter, jsonObject);
                        streamWriter.Flush();
                    }
                }
                WriteToAbilityActivatedFileHistory(jsonObject);
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        private void WriteToAbilityActivatedFileHistory(object jsonObject)
        {
            string pathAttackActivated = Path.Combine(EventInfoDirectoryPath, "AbilityActivatedFromDesktopRecent.info");
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter streamWriter = new StreamWriter(pathAttackActivated))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonWriter, jsonObject);
                    streamWriter.Flush();
                }
            }
        }

        private void GenerateSimpleAbilityMessage(Character target, AnimatedAbility ability)
        {
            SimpleAbility simpleAbility = new SimpleAbility { Ability = ability.Name, Type = Constants.SIMPLE_ABILITY_TYPE_NAME };
            WriteToAbilityActivatedFile(simpleAbility);
        }

        private void GenerateAbortActionMessage(Character target)
        {
            SimpleAbility abortMessage = new SimpleAbility { Type = Constants.ABORT_ACTION_TYPE_NAME, Character = target.Name };
            WriteToAbilityActivatedFile(abortMessage);
            Thread.Sleep(1000);
        }

        private void GenerateActivateHeldCharacterMessage(Character target)
        {
            SimpleAbility abortMessage = new SimpleAbility { Type = Constants.ACTIVATE_HELD_CHARACTER_TYPE_NAME, Character = target.Name };
            WriteToAbilityActivatedFile(abortMessage);
            Thread.Sleep(1000);
        }

        private void GenerateSimpleMovementMessage(CharacterMovement characterMovement, double distanceTravelled)
        {
            SimpleMovement simpleMovement = new SimpleMovement { Movement = GetMovementName(characterMovement.Name), Type = Constants.MOVEMENT_TYPE_NAME, Distance = (int)distanceTravelled };
            WriteToAbilityActivatedFile(simpleMovement);
        }

        private string GetMovementName(string characterMovementName)
        {
            string movementName = "";
            switch (characterMovementName.ToLower())
            {
                case "running":
                case "run":
                    movementName = "Running";
                    break;
                case "walking":
                case "walk":
                    movementName = "Walking";
                    break;
                case "swimming":
                case "swim":
                    movementName = "Swimming";
                    break;
                case "leaping":
                case "leap":
                    movementName = "Leaping";
                    break;
                case "flying":
                case "fly":
                    movementName = "Flying";
                    break;
                case "jumping":
                case "jump":
                    movementName = "Leaping";
                    break;
            }
            return movementName;
        }

        private void GenerateAttackInitiationMessage(Attack attack, Character attacker, Character defender, Guid configKey)
        {
            this.CurrentAttackResult = null;
            this.currentToken = Guid.NewGuid().ToString();
            AttackRequest attackSingleTarget = GetVanillaAttackRequest(attack, attacker, defender, configKey);
            attackSingleTarget.Token = this.currentToken;
            attackSingleTarget.Type = Constants.ATTACK_INITIATION_TYPE_NAME;
            if(attack.CanSpread && attack.SpreadDistance > 0)
                attackSingleTarget.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            WriteToAbilityActivatedFile(attackSingleTarget);
        }

        private void GenerateAttackConfirmationMessage(bool isConfirm)
        {
            AttackConfirmation attackConfirmation = new HCSIntegration.AttackConfirmation
            {
                Type = Constants.ATTACK_CONFIRMATION_TYPE_NAME,
                ConfirmationStatus = isConfirm ? Constants.ATTACK_CONFIRMED_STATUS : Constants.ATTACK_CANCELLED_STATUS
            };
            WriteToAbilityActivatedFile(attackConfirmation);
        }
        private void GenerateAreaAttackInitiationMessage(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            this.CurrentAttackResult = null;
            this.currentToken = Guid.NewGuid().ToString();
            AreaAttackRequest areaAttackRequest = GetAreaAttackRequest(attack, attacker, defenders, configKey);
            areaAttackRequest.Type = Constants.AREA_ATTACK_INITIATION_TYPE_NAME;
            areaAttackRequest.Token = this.currentToken;
            if (attack.CanSpread && attack.SpreadDistance > 0)
                areaAttackRequest.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            WriteToAbilityActivatedFile(areaAttackRequest);
            //GenerateSampleAreaAttackResult(areaAttackRequest);
        }
        private void GenerateSampleAreaAttackResult(AreaAttackRequest areaAttackRequest)
        {
            AreaAttackResponse attackResponse = GetSampleAreaAttackResponse(areaAttackRequest);
            string pathAttackRes = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter streamWriter = new StreamWriter(pathAttackRes))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonWriter, attackResponse);
                    streamWriter.Flush();
                }
            }
        }

        private AreaAttackResponse GetSampleAreaAttackResponse(AreaAttackRequest areaAttackRequest)
        {
            AreaAttackResponse attackResponse = new AreaAttackResponse();
            attackResponse.Ability = areaAttackRequest.Ability;
            attackResponse.IsHit = true;
            attackResponse.MoveBeforeAttackRequired = false;
            attackResponse.Type = areaAttackRequest.Type;
            attackResponse.Token = attackResponse.Token;
            attackResponse.Targets = new List<AttackResponse>();
            foreach (var target in areaAttackRequest.Targets)
            {
                AttackResponse targetResponse = GetSampleAttackResponse(target);
                target.Ability = areaAttackRequest.Ability;
                attackResponse.Targets.Add(targetResponse);
            }

            return attackResponse;
        }

        private AttackResponse GetSampleAttackResponse(AttackRequest attackRequest)
        {
            AttackResponse attackResponse = new HCSIntegration.AttackResponse();
            attackResponse.Ability = attackRequest.Ability;
            attackResponse.IsHit = true;
            attackResponse.MoveBeforeAttackRequired = false;
            attackResponse.Defender = new HCSIntegration.TargetObject
            {
                Name = attackRequest.Defender,
                Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 35 },
                Stun = new HealthMeasures { Name = "Stun", Starting = 40, Current = 15 },
                Endurance = new HealthMeasures { Name = "End", Starting = 90, Current = 25 },
                Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
            };

            if (attackRequest.Obstructions != null && attackRequest.Obstructions.Count > 0)
            {
                attackResponse.ObstructionDamageResults = new List<HCSIntegration.AttackResponse>();
                foreach (var obstr in attackRequest.Obstructions)
                {
                    AttackResponse obsResponse = new HCSIntegration.AttackResponse();
                    obsResponse.IsHit = true;
                    obsResponse.Ability = attackResponse.Ability;
                    obsResponse.Defender = new TargetObject
                    {
                        Name = obstr,
                        Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 5 },
                        Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                        Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                        Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                    };
                    obsResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };
                    attackResponse.ObstructionDamageResults.Add(obsResponse);
                }
            }

            attackResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };

            if (attackRequest.PotentialKnockbackCollisions != null && attackRequest.PotentialKnockbackCollisions.Count > 0)
            {
                attackResponse.KnockbackResult = new KnockbackResult
                {
                    Distance = 10,
                    Collisions = new List<KnockbackCollision>
                        {
                            new KnockbackCollision
                            {
                                CollidingObject = new TargetObject
                                {
                                    Name = attackRequest.PotentialKnockbackCollisions.First(kd => kd.CollisionDistance ==  attackRequest.PotentialKnockbackCollisions.Min(kds => kds.CollisionDistance)).CollisionObject,
                                    Body = new HealthMeasures { Name = "BODY", Starting = 50, Current = 15 },
                                    Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                                    Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                                    Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                                },
                                CollisionDamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15}
                            }
                        }
                };
            }
            return attackResponse;
        }

        private void GenerateAutoFireAttackInitiationMessage(Attack attack, Character attacker, List<Character> defenders, Guid configKey)
        {
            this.CurrentAttackResult = null;
            this.currentToken = Guid.NewGuid().ToString();
            AutoFireAttackRequest autoFireAttackRequest = GetAutoFireAttackRequest(attack, attacker, defenders, configKey);
            autoFireAttackRequest.Type = Constants.AUTO_FIRE_ATTACK_INITIATION_TYPE_NAME;
            autoFireAttackRequest.Token = this.currentToken;
            if (attack.CanSpread && attack.SpreadDistance > 0)
                autoFireAttackRequest.SpreadDistance = (int)Math.Round(attack.SpreadDistance, MidpointRounding.AwayFromZero);
            WriteToAbilityActivatedFile(autoFireAttackRequest);
            //GenerateSampleAutoFireAttackResult(autoFireAttackRequest);
        }

        private void GenerateSampleSweepAttackResult(SweepAttackRequest sweepRequest)
        {
            SweepAttackResponse sweepResponse = new SweepAttackResponse();
            sweepResponse.Attacks = new List<AttackResponseBase>();
            sweepResponse.Token = sweepRequest.Token;
            sweepResponse.Type = Constants.SWEEP_ATTACK_RESULT_TYPE_NAME;
            foreach(var attackRequest in sweepRequest.Attacks)
            {
                if(attackRequest is AttackRequest)
                {
                    AttackResponse response = GetSampleAttackResponse(attackRequest as AttackRequest);
                    response.Type = Constants.ATTACK_RESULT_TYPE_NAME;
                    response.Token = attackRequest.Token;
                    sweepResponse.Attacks.Add(response);
                }
                else if(attackRequest is AreaAttackRequest)
                {
                    AreaAttackResponse response = GetSampleAreaAttackResponse(attackRequest as AreaAttackRequest);
                    response.Type = Constants.AREA_ATTACK_RESULT_TYPE_NAME;
                    response.Token = attackRequest.Token;
                    sweepResponse.Attacks.Add(response);
                }
            }
            string pathAttackRes = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter streamWriter = new StreamWriter(pathAttackRes))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonWriter, sweepResponse);
                    streamWriter.Flush();
                }
            }
        }
        private void GenerateSampleAutoFireAttackResult(AutoFireAttackRequest areaAttackRequest)
        {
            AutoFireAttackResponse attackResponse = new AutoFireAttackResponse();
            attackResponse.Ability = areaAttackRequest.Ability;
            attackResponse.IsHit = true;
            attackResponse.MoveBeforeAttackRequired = false;
            attackResponse.Type = areaAttackRequest.Type;
            attackResponse.Token = attackResponse.Token;
            attackResponse.Targets = new List<AttackResponse>();
            foreach (var target in areaAttackRequest.Targets)
            {
                AttackResponse targetResponse = new HCSIntegration.AttackResponse();
                targetResponse.Ability = areaAttackRequest.Ability;
                targetResponse.IsHit = true;
                targetResponse.MoveBeforeAttackRequired = false;
                targetResponse.Defender = new HCSIntegration.TargetObject
                {
                    Name = target.Defender,
                    Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 35 },
                    Stun = new HealthMeasures { Name = "Stun", Starting = 40, Current = 15 },
                    Endurance = new HealthMeasures { Name = "End", Starting = 90, Current = 25 },
                    Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                };

                if (target.Obstructions != null && target.Obstructions.Count > 0)
                {
                    targetResponse.ObstructionDamageResults = new List<HCSIntegration.AttackResponse>();
                    foreach (var obstr in target.Obstructions)
                    {
                        AttackResponse obsResponse = new HCSIntegration.AttackResponse();
                        obsResponse.IsHit = true;
                        obsResponse.Ability = targetResponse.Ability;
                        obsResponse.Defender = new TargetObject
                        {
                            Name = obstr,
                            Body = new HealthMeasures { Name = "BODY", Starting = 70, Current = 5 },
                            Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                            Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                            Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                        };
                        obsResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };
                        targetResponse.ObstructionDamageResults.Add(obsResponse);
                    }
                }

                targetResponse.DamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15 };

                if (target.PotentialKnockbackCollisions != null && target.PotentialKnockbackCollisions.Count > 0)
                {
                    targetResponse.KnockbackResult = new KnockbackResult
                    {
                        Distance = 10,
                        Collisions = new List<KnockbackCollision>
                        {
                            new KnockbackCollision
                            {
                                CollidingObject = new TargetObject
                                {
                                    Name = target.PotentialKnockbackCollisions.First(kd => kd.CollisionDistance ==  target.PotentialKnockbackCollisions.Min(kds => kds.CollisionDistance)).CollisionObject,
                                    Body = new HealthMeasures { Name = "BODY", Starting = 50, Current = 15 },
                                    Stun = new HealthMeasures { Name = "Stun", Starting = 20, Current = 5 },
                                    Endurance = new HealthMeasures { Name = "End", Starting = 50, Current = 15 },
                                    Effects = new List<string> { "Stunned", "Unconscious", "Dying", "Dead" }
                                },
                                CollisionDamageResults = new DamageResults { Body = 15, Stun = 5, Endurance = 15}
                            }
                        }
                    };
                }

                attackResponse.Targets.Add(targetResponse);
            }
            string pathAttackRes = Path.Combine(EventInfoDirectoryPath, Constants.ATTACK_RESULT_FILE_NAME);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter streamWriter = new StreamWriter(pathAttackRes))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonWriter, attackResponse);
                    streamWriter.Flush();
                }
            }
        }
    }
}
