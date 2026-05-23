using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.Shared.Logging;
using System;
using System.Collections.Generic;

namespace HeroVTT.Combat
{
    public class AttackParameterApplier
    {
        public void ApplyAttackParameters(Character character, Guid attackConfigKey)
        {
            var config = character.AttackConfigurationMap[attackConfigKey].Item2;
            ApplyAttackResult(config);
            ApplyAttackEffect(config);
            ApplyKnockBack(character, config);
            character.RefreshAttackConfigurationParameters();
        }

        private void ApplyAttackResult(AttackConfiguration config)
        {
            if (!config.HasMultipleAttackers)
            {
                config.AttackResult = config.IsHit ? AttackResultOption.Hit : AttackResultOption.Miss;
                return;
            }
            foreach (AttackResult ar in config.AttackResults)
                ar.AttackResultOption = ar.IsHit ? AttackResultOption.Hit : AttackResultOption.Miss;
        }

        private void ApplyAttackEffect(AttackConfiguration config)
        {
            if (config.IsDead)
                config.AttackEffectOption = AttackEffectOption.Dead;
            else if (config.IsDying)
                config.AttackEffectOption = AttackEffectOption.Dying;
            else if (config.IsUnconcious)
                config.AttackEffectOption = AttackEffectOption.Unconcious;
            else if (config.IsStunned)
                config.AttackEffectOption = AttackEffectOption.Stunned;
            else
                config.AttackEffectOption = AttackEffectOption.None;
        }

        private void ApplyKnockBack(Character character, AttackConfiguration config)
        {
            if (config.IsKnockedBack)
            {
                config.KnockBackOption = KnockBackOption.KnockBack;
            }
            else
            {
                config.KnockBackOption = KnockBackOption.None;
                FileLogManager.ForceLog("Setting None as Knockback option for {0}", character.Name);
            }
        }
    }
}
