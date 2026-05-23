using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.Shared;
using Prism.Events;
using Module.Shared.Events;

namespace HeroVTT.Roster
{
    public class AttackCoordinator
    {
        private readonly IRosterGameState gameState;
        private readonly EventAggregator eventAggregator;

        private Attack currentAttack;
        private Guid currentAttackConfigKey = Guid.Empty;
        private List<Character> targetCharacters = new List<Character>();
        private List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksInProgress = new List<Tuple<Attack, List<Character>, List<Character>, Guid>>();

        public Attack CurrentAttack { get { return currentAttack; } }
        public Guid CurrentAttackConfigKey { get { return currentAttackConfigKey; } }
        public List<Character> TargetCharacters { get { return targetCharacters; } }
        public List<Tuple<Attack, List<Character>, List<Character>, Guid>> AttacksInProgress { get { return attacksInProgress; } }

        public bool IsPlayingAutoFire
        {
            get { return currentAttack != null && currentAttack.IsAutoFire; }
        }

        public bool IsSpreadableAttackInProgress
        {
            get { return currentAttack != null && currentAttack.CanSpread; }
        }

        public AttackCoordinator(IRosterGameState gameState, EventAggregator eventAggregator)
        {
            this.gameState = gameState;
            this.eventAggregator = eventAggregator;
        }

        public void InitiateAttack(Character attackingCharacter, Attack attack, List<Character> attackingCharacters,
            HashedObservableCollection<ICrowdMemberModel, string> participants, bool isGangModeActive)
        {
            targetCharacters = new List<Character>();
            currentAttackConfigKey = Guid.NewGuid();
            CrowdMemberModel rosterCharacter = participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;

            if (rosterCharacter == null || attack == null)
                return;

            currentAttack = attack;

            attackingCharacters.Clear();
            if (attackingCharacter.IsGangLeader)
            {
                foreach (Character c in participants.Where(p => (p as Character).IsActive && (p as Character).HasBeenSpawned))
                {
                    attackingCharacters.Add(c);
                    c.AddAttackConfiguration(currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, currentAttackConfigKey);
                }
            }
            else if (isGangModeActive && (attackingCharacter as CrowdMemberModel).RosterCrowd.IsGangMode)
            {
                foreach (Character c in participants.Where(p => p.RosterCrowd == (attackingCharacter as CrowdMemberModel).RosterCrowd && (p as Character).HasBeenSpawned))
                {
                    attackingCharacters.Add(c);
                    c.AddAttackConfiguration(currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, currentAttackConfigKey);
                }
            }
            else
            {
                attackingCharacters.Add(attackingCharacter);
                rosterCharacter.AddAttackConfiguration(currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, currentAttackConfigKey);
            }
        }

        public void AddTarget(Character targetCharacter, List<Character> attackingCharacters)
        {
            if (targetCharacter == null || attackingCharacters.Contains(targetCharacter))
                return;

            if (!targetCharacters.Contains(targetCharacter))
            {
                targetCharacters.Add(targetCharacter);
                targetCharacter.AddAttackConfiguration(currentAttack, new AttackConfiguration { AttackMode = AttackMode.Defend, AttackEffectOption = AttackEffectOption.None }, currentAttackConfigKey);
            }
        }

        public void ConfigureAttack(List<Character> attackingCharacters)
        {
            if (currentAttack != null && !attacksInProgress.Any(ap => ap.Item4 == currentAttackConfigKey))
            {
                attacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(
                    currentAttack, attackingCharacters.ToList(), targetCharacters.ToList(), currentAttackConfigKey));
            }
        }

        public void ResetCurrentAttack()
        {
            currentAttack = null;
            currentAttackConfigKey = Guid.Empty;
        }

        public void ResetAllAttackState(List<Character> attackingCharacters)
        {
            attacksInProgress.Clear();
            gameState.IsPlayingAttack = false;
            currentAttack = null;
            currentAttackConfigKey = Guid.Empty;
        }

        public void ResetAttack(Attack attack, List<Character> defenders, Guid configKey, List<Character> attackingCharacters, bool killEffects = false)
        {
            attack.Stop(useMemoryTargeting: true);

            if (attackingCharacters.Count > 0)
            {
                attackingCharacters.ForEach(ac =>
                {
                    if (killEffects)
                        ac.RemoveAttackConfiguration(configKey);
                    else
                        ac.AttackConfigurationMap[configKey].Item2.AttackMode = AttackMode.None;
                    ac.RefreshAttackConfigurationParameters();
                    ac.ScanAndFixMemoryPointer();
                });
            }

            foreach (var defender in defenders)
            {
                if (killEffects)
                    defender.RemoveAttackConfiguration(configKey);
                else if (defender.AttackConfigurationMap.Any(m => m.Key == configKey))
                    defender.AttackConfigurationMap[configKey].Item2.AttackMode = AttackMode.None;
                defender.RefreshAttackConfigurationParameters();
                defender.ScanAndFixMemoryPointer();
            }

            var tuple = attacksInProgress.FirstOrDefault(a => a.Item1 == attack);
            if (tuple != null)
                attacksInProgress.Remove(tuple);
        }

        public List<Character> CalculateAreaTargets(Character attacker, HashedObservableCollection<ICrowdMemberModel, string> participants, List<Character> attackingCharacters, object attackCenter)
        {
            if (currentAttack == null || currentAttack.AttackInfo == null)
                return new List<Character>();

            attackingCharacters.ForEach(ac => ac.AttackConfigurationMap[currentAttackConfigKey].Item2.AttackCenterPosition =
                (attackCenter is Character) ? (attackCenter as Character).CurrentPositionVector : (Vector3)attackCenter);

            return currentAttack.CalculateAreaAttackTargets(
                attackingCharacters.First(),
                participants.Where(p => (p as Character).HasBeenSpawned && !attackingCharacters.Contains(p as Character)).Cast<Character>().ToList(),
                currentAttackConfigKey);
        }

        public void DistributeAutoFireShots(List<Character> targets, int maxShots)
        {
            int remaining = maxShots;
            foreach (Character dc in targets)
                dc.AttackConfigurationMap[currentAttackConfigKey].Item2.NumberOfShotsAssigned = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                if (remaining == 0)
                    break;
                targets[i].AttackConfigurationMap[currentAttackConfigKey].Item2.NumberOfShotsAssigned += 1;
                remaining--;
                if (i == targets.Count - 1 && remaining > 0)
                    i = -1;
            }
        }

        public bool IsSweepInProgress
        {
            get { return gameState.DefaultSweepAbility != null && gameState.DefaultSweepAbility.IsActive; }
        }
    }
}
