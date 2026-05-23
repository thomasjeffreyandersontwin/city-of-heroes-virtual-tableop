using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace HeroVTT.Combat
{
    public class CombatKeyHandler
    {
        public void HandleKeyInput(Key inputKey, List<Character> defenders, Guid attackConfigKey)
        {
            if (!IsHandledKey(inputKey))
                return;

            foreach (var defender in defenders)
            {
                ApplyKeyToDefender(inputKey, defender, attackConfigKey);
                defender.RefreshAttackConfigurationParameters();
            }
        }

        private bool IsHandledKey(Key key)
        {
            return key == Key.H || key == Key.M || key == Key.S || key == Key.U
                || key == Key.Y || key == Key.D || key == Key.K || key == Key.N || key == Key.T
                || IsNumericKey(key);
        }

        private void ApplyKeyToDefender(Key key, Character defender, Guid configKey)
        {
            var config = defender.AttackConfigurationMap[configKey].Item2;

            switch (key)
            {
                case Key.H: SetAllHits(config, true); break;
                case Key.M: SetAllHits(config, false); break;
                case Key.S: config.IsStunned = true; break;
                case Key.U: config.IsUnconcious = true; break;
                case Key.Y: config.IsDying = true; break;
                case Key.D: config.IsDead = true; break;
                case Key.K: config.IsKnockedBack = true; break;
                case Key.N: config.IsKnockedBack = false; break;
                case Key.T: config.MoveAttackerToTarget = true; break;
                default:
                    if (IsNumericKey(key))
                        AppendKnockbackDigit(config, key);
                    break;
            }
        }

        private void SetAllHits(AttackConfiguration config, bool isHit)
        {
            if (!config.HasMultipleAttackers)
            {
                config.IsHit = isHit;
                return;
            }
            foreach (var ar in config.AttackResults)
                ar.IsHit = isHit;
        }

        private bool IsNumericKey(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9);
        }

        private void AppendKnockbackDigit(AttackConfiguration config, Key key)
        {
            int digit = (key >= Key.D0 && key <= Key.D9) ? key - Key.D0 : key - Key.NumPad0;
            if (config.KnockBackDistance > 0)
            {
                string current = config.KnockBackDistance.ToString() + digit.ToString();
                config.KnockBackDistance = Convert.ToInt32(current);
            }
            else
            {
                config.KnockBackDistance = digit;
            }
        }
    }
}
