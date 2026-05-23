using System;
using System.Collections.Generic;
using System.Linq;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Movements;
using Prism.Events;
using Module.Shared;
using Module.Shared.Events;

namespace HeroVTT.Roster
{
    public class CharacterActivator
    {
        private readonly IRosterGameState gameState;
        private readonly EventAggregator eventAggregator;

        public CharacterActivator(IRosterGameState gameState, EventAggregator eventAggregator)
        {
            this.gameState = gameState;
            this.eventAggregator = eventAggregator;
        }

        public void ActivateCharacter(Character character, HashedObservableCollection<ICrowdMemberModel, string> participants, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            if (!character.HasBeenSpawned)
                character.Spawn();

            if (gameState.ActiveCharacterMovement != null && gameState.ActiveCharacterMovement.Character.IsActive)
            {
                gameState.ActiveCharacterMovement.IsPaused = true;
                gameState.FormerActiveCharacterMovement = gameState.ActiveCharacterMovement;
            }

            if (gameState.ActiveCharacterMovement != null && !gameState.ActiveCharacterMovement.Character.IsActive)
            {
                var otherCharacter = gameState.ActiveCharacterMovement.Character;
                if (otherCharacter != gameState.ActiveCharacter)
                {
                    eventAggregator.GetEvent<StopMovementEvent>().Publish(gameState.ActiveCharacterMovement);
                }
            }

            character.SetActive();

            var pausedMovement = character.Movements.FirstOrDefault(cm => cm.IsPaused && gameState.FormerActiveCharacterMovement == cm);
            if (pausedMovement != null)
            {
                pausedMovement.IsPaused = false;
            }

            eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(new Tuple<Character, string, string>(character, selectedOptionGroupName, selectedOptionName));
        }

        public void DeactivateCharacter(Character character, HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            if (character == null)
                return;

            if (character.IsActive)
            {
                if (gameState.FormerActiveCharacterMovement != null)
                {
                    gameState.FormerActiveCharacterMovement.IsPaused = false;
                    gameState.ActiveCharacterMovement = gameState.FormerActiveCharacterMovement;
                }
                character.ResetActive();
                eventAggregator.GetEvent<DeactivateCharacterEvent>().Publish(character);
            }
        }

        public void ActivateGang(List<Character> gangMembers, Character targetedCharacter)
        {
            foreach (var gm in gangMembers)
            {
                gm.SetActive();
                if (gm == targetedCharacter)
                {
                    gm.IsGangLeader = true;
                }
            }
            if (!gangMembers.Any(c => c.IsGangLeader))
            {
                gangMembers[0].IsGangLeader = true;
            }

            eventAggregator.GetEvent<ActivateGangEvent>().Publish(gangMembers);
        }

        public void DeactivateGang(HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            foreach (var c in participants)
            {
                (c as Character).ResetActive();
            }

            eventAggregator.GetEvent<DeactivateGangEvent>().Publish(null);
        }
    }
}
