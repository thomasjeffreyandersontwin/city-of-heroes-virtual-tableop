using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeroVTT.Combat
{
    public class AttackSummaryBuilder
    {
        public string Build(List<Character> defenders, Guid attackConfigKey)
        {
            var hitCharacters = defenders.Where(dc => dc.AttackConfigurationMap[attackConfigKey].Item2.IsHit).ToList();
            var missCharacters = defenders.Where(dc => !dc.AttackConfigurationMap[attackConfigKey].Item2.IsHit).ToList();

            var summary = new StringBuilder();
            var summarized = hitCharacters.ToDictionary(c => c, c => false);

            AppendHitMissLine(summary, hitCharacters, missCharacters);
            AppendCharacterDetails(summary, hitCharacters, summarized, attackConfigKey);
            return summary.ToString();
        }

        private void AppendHitMissLine(StringBuilder summary, List<Character> hit, List<Character> miss)
        {
            bool hasHit = hit.Count > 0;
            bool hasMiss = miss.Count > 0;

            if (hasHit)
            {
                summary.Append("The attack hit ");
                AppendNameList(summary, hit);
            }

            if (hasMiss)
            {
                summary.Append(hasHit ? " and missed " : "The attack missed ");
                AppendNameList(summary, miss);
            }
        }

        private void AppendNameList(StringBuilder summary, List<Character> characters)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (i == 0)
                    summary.Append(characters[0].Name);
                else if (i == characters.Count - 1)
                    summary.AppendFormat(" and {0}", characters[i].Name);
                else
                    summary.AppendFormat(", {0}", characters[i].Name);
            }
        }

        private void AppendCharacterDetails(StringBuilder summary, List<Character> hitCharacters, Dictionary<Character, bool> summarized, Guid key)
        {
            foreach (var character in hitCharacters)
            {
                if (summarized[character])
                    continue;

                var config = character.AttackConfigurationMap[key].Item2;
                AppendKnockbackDetails(summary, character, config, summarized, key);
                AppendHealthDetails(summary, character, config, key);
                summarized[character] = true;
            }
        }

        private void AppendKnockbackDetails(StringBuilder summary, Character character, AttackConfiguration config, Dictionary<Character, bool> summarized, Guid key)
        {
            if (!config.IsKnockedBack)
                return;

            summary.AppendLine();
            summary.AppendFormat("{0} is knocked back {1} hexes", character.Name, config.KnockBackDistance);

            if (config.ObstructingCharacters == null || config.ObstructingCharacters.Count == 0)
                return;

            foreach (Character obs in config.ObstructingCharacters)
            {
                summary.AppendLine();
                summary.AppendFormat(config.IsKnockbackObstruction
                    ? "{0} collided with {1}" : "Attack is intercepted by {0}",
                    character.Name, obs.Name);

                var obsConfig = obs.AttackConfigurationMap[key].Item2;
                string obsEffect = BuildEffectsString(obs, key);
                AppendObstructionHealth(summary, obs, obsConfig, obsEffect);
                summarized[obs] = true;
            }
        }

        private void AppendObstructionHealth(StringBuilder summary, Character obs, AttackConfiguration config, string effects)
        {
            if (effects == "" && config.Body == null)
                return;

            summary.AppendLine();
            if (config.Body != null && effects != "")
                summary.AppendFormat("{0} now has {1} BODY and is {2}", obs.Name, config.Body, effects);
            else if (config.Body != null)
                summary.AppendFormat("{0} now has {1} BODY", obs.Name, config.Body);
            else
                summary.AppendFormat("{0} is {1}", obs.Name, effects);
        }

        private void AppendHealthDetails(StringBuilder summary, Character character, AttackConfiguration config, Guid key)
        {
            if (config.Stun != null || config.Body != null)
                summary.AppendLine();

            if (config.Stun != null && config.Body != null)
                summary.AppendFormat("{0} has {1} Stun and {2} BODY left", character.Name, config.Stun, config.Body);
            else if (config.Stun != null)
                summary.AppendFormat("{0} has {1} Stun left", character.Name, config.Stun);
            else if (config.Body != null)
                summary.AppendFormat("{0} has {1} BODY left", character.Name, config.Body);

            string effects = BuildEffectsString(character, key);
            if (string.IsNullOrEmpty(effects))
                return;

            if (config.Stun == null && config.Body == null)
            {
                summary.AppendLine();
                summary.AppendFormat("{0} is {1}", character.Name, effects);
            }
            else
            {
                summary.AppendFormat(" and is {0}", effects);
            }
        }

        private string BuildEffectsString(Character character, Guid key)
        {
            var config = character.AttackConfigurationMap[key].Item2;
            var effects = new List<string>();

            if (config.IsStunned) effects.Add("Stunned");
            if (config.IsUnconcious) effects.Add("Unconscious");
            if (config.IsDying) effects.Add("Dying");
            if (config.IsDead) effects.Add("Dead");
            if (config.IsDestroyed) effects.Add("Destroyed");
            if (config.IsPartiallyDestryoed) effects.Add("Partially Destroyed");

            if (effects.Count == 0)
                return "";

            string result = string.Join(", ", effects);
            int lastComma = result.LastIndexOf(", ");
            if (lastComma > result.IndexOf(", ") && lastComma >= 0)
                result = result.Remove(lastComma, 2).Insert(lastComma, " and ");

            return result + ".";
        }
    }
}
