using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Events;
using Module.Shared.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class ActiveAttackViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private IDesktopKeyEventHandler desktopKeyEventHandler;

        #endregion

        #region Public Properties

        private Attack activeAttack;
        public Attack ActiveAttack
        {
            get
            {
                return activeAttack;
            }
            set
            {
                activeAttack = value;
                OnPropertyChanged("ActiveAttack");
            }
        }

        private List<Character> defendingCharacters;
        public List<Character> DefendingCharacters
        {
            get
            {
                return defendingCharacters;
            }
            set
            {
                defendingCharacters = value;
                OnPropertyChanged("DefendingCharacters");
            }
        }

        private ObservableCollection<DefenderActiveAttackConfiguration> defenderActiveAttackConfigurations;
        public ObservableCollection<DefenderActiveAttackConfiguration> DefenderActiveAttackConfigurations
        {
            get
            {
                return defenderActiveAttackConfigurations;
            }
            set
            {
                defenderActiveAttackConfigurations = value;
                OnPropertyChanged("DefenderActiveAttackConfigurations");
            }
        }

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

        private string attackSummaryText;
        public string AttackSummaryText
        {
            get
            {
                return attackSummaryText;
            }
            set
            {
                attackSummaryText = value;
                OnPropertyChanged("AttackSummaryText");
            }
        }

        private bool showAttackSummaryText;
        public bool ShowAttackSummaryText
        {
            get
            {
                return showAttackSummaryText;
            }
            set
            {
                showAttackSummaryText = value;
                OnPropertyChanged("ShowAttackSummaryText");
            }
        }

        public Guid AttackConfigKey { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<object> CenterTargetChangedCommand { get; private set; }
        public DelegateCommand<object> SetActiveAttackCommand { get; private set; }
        public DelegateCommand<object> CancelActiveAttackCommand { get; private set; }
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }
        public DelegateCommand<object> AttackHitChangedCommand { get; private set; }

        #endregion

        #region Constructor

        public ActiveAttackViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, IDesktopKeyEventHandler keyEventHandler, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.desktopKeyEventHandler = keyEventHandler;
            InitializeCommands();
            //this.eventAggregator.GetEvent<ConfigureActiveAttackEvent>().Subscribe(this.ConfigureActiveAttack);
            //this.eventAggregator.GetEvent<ConfirmAttackEvent>().Subscribe(this.SetActiveAttack);
            InitializeDesktopKeyEventHandlers();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.SetActiveAttackCommand = new DelegateCommand<object>(this.SetActiveAttack);
            this.CancelActiveAttackCommand = new DelegateCommand<object>(this.CancelActiveAttack);
            this.CenterTargetChangedCommand = new DelegateCommand<object>(this.ChangeCenterTarget);
            this.ActivatePanelCommand = new DelegateCommand<string>(this.ActivatePanel);
            this.DeactivatePanelCommand = new DelegateCommand<string>(this.DeactivatePanel);
            this.AttackHitChangedCommand = new DelegateCommand<object>(this.ChangeAttackHit);
        }

        public void InitializeDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        public void RemoveDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.RemoveKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        #endregion

        #region Methods

        private void ChangeCenterTarget(object state)
        {
            if (this.ActiveAttack.IsAreaEffect)
            {
                Character character = state as Character;
                if (character != null && character.AttackConfigurationMap[AttackConfigKey].Item2.IsCenterTarget)
                {
                    foreach (Character ch in this.DefendingCharacters.Where(dc => dc.Name != character.Name))
                    {
                        ch.AttackConfigurationMap[AttackConfigKey].Item2.IsCenterTarget = false;
                        ch.RefreshAttackConfigurationParameters();
                    }
                    character.RefreshAttackConfigurationParameters();
                }
            }
        }

        private void ChangeAttackHit(object state)
        {
            Character target = state as Character;
            if (target != null)
            {
                if (target.AttackConfigurationMap[AttackConfigKey].Item2.AttackResults.Any(ar => ar.IsHit))
                    target.AttackConfigurationMap[AttackConfigKey].Item2.IsHit = true;
                else
                    target.AttackConfigurationMap[AttackConfigKey].Item2.IsHit = false;
                target.RefreshAttackConfigurationParameters();
            }
        }

        public void ConfigureActiveAttack(Tuple<List<Character>, Attack, Guid> tuple)
        {
            //this.DefendingCharacters = new ObservableCollection<Character>(tuple.Item1);
            this.DefendingCharacters = tuple.Item1;
            this.ActiveAttack = tuple.Item2;
            this.AttackConfigKey = tuple.Item3;

            this.DefenderActiveAttackConfigurations = new ObservableCollection<DefenderActiveAttackConfiguration>();
            foreach(var defender in this.DefendingCharacters)
            {
                this.DefenderActiveAttackConfigurations.Add(new DefenderActiveAttackConfiguration { Defender = defender, ActiveAttackConfiguration = defender.AttackConfigurationMap[AttackConfigKey].Item2 });
                if (defender.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget)
                    this.MoveAttackerToTarget = true;
            }

            if (Helper.GlobalVariables_IntegrateWithHCS)
            {
                this.ShowAttackSummaryText = true;
                this.SetAttackSummaryText();
            }
            else
                this.ShowAttackSummaryText = false;
            //this.AttackSummaryText = "Hi there";
        }

        private void SetActiveAttack(object state)
        {
            SetActiveAttack();
        }
        public void SetActiveAttack()
        {
            foreach (Character ch in this.DefendingCharacters)
            {
                SetAttackParameters(ch);
            }
            //if (this.DefendingCharacters.Any(dc => dc.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget))
            foreach (Character dc in this.DefendingCharacters)
                dc.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget = this.MoveAttackerToTarget;

            //// Change mouse pointer to back to bulls eye
            //Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            //Mouse.OverrideCursor = cursor;

            //this.eventAggregator.GetEvent<CloseActiveAttackWidgetEvent>().Publish(null);
            //this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<List<Character>, Attack>(this.DefendingCharacters.ToList(), this.ActiveAttack));
        }
        private void SetAttackParameters(Character ch)
        {
            if (!ch.AttackConfigurationMap[AttackConfigKey].Item2.HasMultipleAttackers)
            {
                if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsHit)
                    ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackResult = AttackResultOption.Hit;
                else
                    ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackResult = AttackResultOption.Miss;
            }
            else
            {
                foreach (AttackResult ar in ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackResults)
                {
                    ar.AttackResultOption = ar.IsHit ? AttackResultOption.Hit : AttackResultOption.Miss;
                }
            }

            if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsDead)
                ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackEffectOption = AttackEffectOption.Dead;
            else if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsDying)
                ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackEffectOption = AttackEffectOption.Dying;
            else if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsUnconcious)
                ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackEffectOption = AttackEffectOption.Unconcious;
            else if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsStunned)
                ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackEffectOption = AttackEffectOption.Stunned;
            else
                ch.AttackConfigurationMap[AttackConfigKey].Item2.AttackEffectOption = AttackEffectOption.None;

            if (ch.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockedBack)
                ch.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackOption = KnockBackOption.KnockBack;
            else
            {
                ch.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackOption = KnockBackOption.None;
                FileLogManager.ForceLog("Setting None as Knockback option for {0}", ch.Name);
            }

            ch.RefreshAttackConfigurationParameters();
        }
        public void CancelActiveAttack(object state)
        {
            foreach (var c in this.DefendingCharacters)
            {
                //c.AddAttackConfiguration(this.ActiveAttack, new AttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None });
                c.RemoveAttackConfiguration(AttackConfigKey);
            }
            //this.eventAggregator.GetEvent<CloseActiveAttackWidgetEvent>().Publish(null);
            //this.eventAggregator.GetEvent<CancelActiveAttackEvent>().Publish(this.DefendingCharacters.ToList());
        }

        private void ActivatePanel(string panelName)
        {
            Helper.GlobalVariables_CurrentActiveWindowName = panelName;
        }

        private void DeactivatePanel(string panelName)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == panelName)
                Helper.GlobalVariables_CurrentActiveWindowName = "";
        }

        private void SetAttackSummaryText()
        {
            this.AttackSummaryText = "";
            List<Character> hitCharacters = this.DefendingCharacters.Where(dc => dc.AttackConfigurationMap[AttackConfigKey].Item2.IsHit).ToList();
            List<Character> missCharacters = this.DefendingCharacters.Where(dc => !dc.AttackConfigurationMap[AttackConfigKey].Item2.IsHit).ToList();
            List<Character> knockbackCharacters = this.DefendingCharacters.Where(dc => dc.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockedBack).ToList();
            StringBuilder summary = new StringBuilder("The attack hit ");
            bool hitCharactersFound = hitCharacters.Count > 0;
            bool missCharactersFound = missCharacters.Count > 0;
            bool knockbackCharactersFound = knockbackCharacters.Count > 0;
            Dictionary<Character, bool> summarizedCharacters = new Dictionary<Character, bool>();
            foreach(Character c in hitCharacters) {
                if (!summarizedCharacters.ContainsKey(c))
                    summarizedCharacters.Add(c, false);
            }
            if (hitCharactersFound)
            {
                for (int i = 0; i < hitCharacters.Count; i++)
                {
                    if (i == 0)
                        summary.Append(hitCharacters[0].Name);
                    else if (i == hitCharacters.Count - 1 && !missCharactersFound)
                        summary.AppendFormat(" and {0}", hitCharacters[i].Name);
                    else
                        summary.AppendFormat(", {0}", hitCharacters[i].Name);
                }
            }
            if (missCharactersFound)
            {
                if(!hitCharactersFound)
                    summary = new StringBuilder("The attack missed ");
                else
                    summary.Append(" and missed ");
                for (int i = 0; i < missCharacters.Count; i++)
                {
                    if (i == 0)
                        summary.Append(missCharacters[0].Name);
                    else if (i == missCharacters.Count - 1)
                        summary.AppendFormat(" and {0}", missCharacters[i].Name);
                    else
                        summary.AppendFormat(", {0}", missCharacters[i].Name);
                }
            }

            foreach (var character in hitCharacters)
            {
                if (summarizedCharacters[character])
                    continue;
                if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockedBack)
                {
                    summary.AppendLine();
                    summary.AppendFormat("{0} is knocked back {1} hexes", character.Name, character.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackDistance);
                    if(character.AttackConfigurationMap[AttackConfigKey].Item2.ObstructingCharacters != null && character.AttackConfigurationMap[AttackConfigKey].Item2.ObstructingCharacters.Count > 0)
                    {
                        foreach(Character obsCharacter in character.AttackConfigurationMap[AttackConfigKey].Item2.ObstructingCharacters)
                        {
                            summary.AppendLine();
                            if(character.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockbackObstruction)
                                summary.AppendFormat("{0} collided with {1}", character.Name, obsCharacter.Name);
                            else
                                summary.AppendFormat("Attack is intercepted by {0}", obsCharacter.Name);
                            string obsEffect = GetEffectsString(obsCharacter);
                            if (obsEffect != "" || obsCharacter.AttackConfigurationMap[AttackConfigKey].Item2.Body != null)
                            {
                                summary.AppendLine();
                                if (obsCharacter.AttackConfigurationMap[AttackConfigKey].Item2.Body != null && obsEffect != "")
                                {
                                    summary.AppendFormat("{0} now has {1} BODY and is {2}", obsCharacter.Name, obsCharacter.AttackConfigurationMap[AttackConfigKey].Item2.Body, obsEffect);
                                }
                                else if (obsCharacter.AttackConfigurationMap[AttackConfigKey].Item2.Body != null)
                                {
                                    summary.AppendFormat("{0} now has {1} BODY", obsCharacter.Name, obsCharacter.AttackConfigurationMap[AttackConfigKey].Item2.Body);
                                }
                                else
                                {
                                    summary.AppendFormat("{0} is {1}", obsCharacter.Name, obsEffect);
                                }
                            }
                            summarizedCharacters[obsCharacter] = true;
                        }
                    }
                }
                //else
                {
                    if (character.AttackConfigurationMap[AttackConfigKey].Item2.Stun != null || character.AttackConfigurationMap[AttackConfigKey].Item2.Body != null)
                    {
                        summary.AppendLine();
                    }
                    if (character.AttackConfigurationMap[AttackConfigKey].Item2.Stun != null && character.AttackConfigurationMap[AttackConfigKey].Item2.Body != null)
                    {
                        summary.AppendFormat("{0} has {1} Stun and {2} BODY left", character.Name, character.AttackConfigurationMap[AttackConfigKey].Item2.Stun,
                              character.AttackConfigurationMap[AttackConfigKey].Item2.Body);
                    }
                    else if (character.AttackConfigurationMap[AttackConfigKey].Item2.Stun != null)
                    {
                        summary.AppendFormat("{0} has {1} Stun left", character.Name, character.AttackConfigurationMap[AttackConfigKey].Item2.Stun);
                    }
                    else if (character.AttackConfigurationMap[AttackConfigKey].Item2.Body != null)
                    {
                        summary.AppendFormat("{0} has {1} BODY left", character.Name, character.AttackConfigurationMap[AttackConfigKey].Item2.Body);
                    }

                    string effects = GetEffectsString(character);
                    if (!string.IsNullOrEmpty(effects))
                    {
                        if (character.AttackConfigurationMap[AttackConfigKey].Item2.Stun == null && character.AttackConfigurationMap[AttackConfigKey].Item2.Body == null)
                        {
                            summary.AppendLine();
                            summary.AppendFormat("{0} is {1}", character.Name, effects);
                        }
                        else
                        {
                            summary.AppendFormat(" and is {0}", effects);
                        }
                    }
                        
                }
                summarizedCharacters[character] = true;
            }
            this.AttackSummaryText = summary.ToString();
        }

        private string GetEffectsString(Character character)
        {
            List<string> effectsStr = new List<string>();
            string efstr = "";
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsStunned)
                effectsStr.Add("Stunned");
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsUnconcious)
                effectsStr.Add("Unconscious");
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsDying)
                effectsStr.Add("Dying");
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsDead)
                effectsStr.Add("Dead");
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsDestroyed)
                effectsStr.Add("Destroyed");
            if (character.AttackConfigurationMap[AttackConfigKey].Item2.IsPartiallyDestryoed)
                effectsStr.Add("Partially Destroyed");
            if (effectsStr.Count > 0)
            {
                efstr = String.Join(", ", effectsStr);
                if (efstr.IndexOf(",") != efstr.LastIndexOf(","))
                {
                    efstr = efstr.Replace(efstr[efstr.LastIndexOf(", ")].ToString(), " and");
                }
                efstr += ".";
            }
            return efstr;
        }

        #region Desktop Key Handling
        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ACTIVE_ATTACK_WIDGET)
            {
                if (inputKey == Key.Enter)
                {
                    if (this.SetActiveAttackCommand.CanExecute(null))
                        this.SetActiveAttackCommand.Execute(null);
                }
                else if (inputKey == Key.Escape)
                {
                    if (this.CancelActiveAttackCommand.CanExecute(null))
                        this.CancelActiveAttackCommand.Execute(null);
                }
                else if (inputKey == Key.H || inputKey == Key.M || inputKey == Key.S || inputKey == Key.U
                    || inputKey == Key.Y || inputKey == Key.D || inputKey == Key.K || inputKey == Key.N || inputKey == Key.T
                    || (inputKey >= Key.D0 && inputKey <= Key.D9) || (inputKey >= Key.NumPad0 && inputKey <= Key.NumPad9))
                {
                    foreach (var defender in this.DefendingCharacters)
                    {
                        if (inputKey == Key.H)
                        {
                            if (!defender.AttackConfigurationMap[AttackConfigKey].Item2.HasMultipleAttackers)
                                defender.AttackConfigurationMap[AttackConfigKey].Item2.IsHit = true;
                            else
                            {
                                foreach (var ar in defender.AttackConfigurationMap[AttackConfigKey].Item2.AttackResults)
                                {
                                    ar.IsHit = true;
                                }
                            }
                        }
                        else if (inputKey == Key.M)
                        {
                            if (!defender.AttackConfigurationMap[AttackConfigKey].Item2.HasMultipleAttackers)
                                defender.AttackConfigurationMap[AttackConfigKey].Item2.IsHit = false;
                            else
                            {
                                foreach (var ar in defender.AttackConfigurationMap[AttackConfigKey].Item2.AttackResults)
                                {
                                    ar.IsHit = false;
                                }
                            }
                        }
                        else if (inputKey == Key.S)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsStunned = true;
                        }
                        else if (inputKey == Key.U)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsUnconcious = true;
                        }
                        else if (inputKey == Key.Y)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsDying = true;
                        }
                        else if (inputKey == Key.D)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsDead = true;
                        }
                        else if (inputKey == Key.K)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockedBack = true;
                        }
                        else if (inputKey == Key.N)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.IsKnockedBack = false;
                        }
                        else if (inputKey == Key.T)
                        {
                            defender.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget = true;
                        }
                        else if ((inputKey >= Key.D0 && inputKey <= Key.D9) || (inputKey >= Key.NumPad0 && inputKey <= Key.NumPad9))
                        {
                            var intkey = (inputKey >= Key.D0 && inputKey <= Key.D9) ? inputKey - Key.D0 : inputKey - Key.NumPad0;
                            if (defender.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackDistance > 0)
                            {
                                string current = defender.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackDistance.ToString();
                                current += intkey.ToString();
                                defender.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackDistance = Convert.ToInt32(current);
                            }
                            else
                            {
                                defender.AttackConfigurationMap[AttackConfigKey].Item2.KnockBackDistance = intkey;
                            }
                        }

                        defender.RefreshAttackConfigurationParameters();
                    }
                }
            }
            return null;
        }

        #endregion

        #endregion
    }
}
