using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Movements;
using System;
using System.Linq;

namespace HeroVTT.Roster
{
    /// <summary>
    /// Pauses, stops, and resumes character movements when roster activation changes.
    /// </summary>
    public class ActiveCharacterActivation
    {
        private readonly IRosterGameState _gameState;

        public ActiveCharacterActivation(IRosterGameState gameState)
        {
            _gameState = gameState;
        }

        public void PrepareMovementsForActivation(Action<CharacterMovement> stopMovement)
        {
            var movement = _gameState.ActiveCharacterMovement;
            if (movement != null && movement.Character.IsActive)
            {
                movement.IsPaused = true;
                _gameState.FormerActiveCharacterMovement = movement;
            }

            if (movement != null && !movement.Character.IsActive)
            {
                var otherCharacter = movement.Character;
                if (otherCharacter != _gameState.ActiveCharacter)
                    stopMovement(movement);
            }
        }

        public void ResumePausedMovementFor(Character character)
        {
            var pausedMovement = character.Movements.FirstOrDefault(
                cm => cm.IsPaused && _gameState.FormerActiveCharacterMovement == cm);
            if (pausedMovement != null)
                pausedMovement.IsPaused = false;
        }

        public void RestoreFormerMovementOnDeactivate()
        {
            if (_gameState.FormerActiveCharacterMovement == null)
                return;

            _gameState.FormerActiveCharacterMovement.IsPaused = false;
            _gameState.ActiveCharacterMovement = _gameState.FormerActiveCharacterMovement;
        }
    }
}
