using Framework.WPF.Services.MessageBoxService;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared.Messages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace HeroVTT.Movements
{
    public sealed class MovementAddResult
    {
        public Movement Movement { get; set; }
        public CharacterMovement UpdatedCurrent { get; set; }
        public ObservableCollection<Movement> AvailableMovements { get; set; }
    }

    /// <summary>
    /// Authoring rules for movement types on a character — add, rename, remove, defaults, unique names.
    /// </summary>
    public class MovementAuthoring
    {
        public string GetNewValidMovementName(Character defaultCharacter, string name = "Movement")
        {
            string suffix = string.Empty;
            int i = 0;
            while (defaultCharacter.Movements.Any(cm => cm.Movement != null && cm.Movement.Name == name + suffix))
                suffix = string.Format(" ({0})", ++i);
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        public ObservableCollection<Movement> BuildAvailableMovements(
            Character defaultCharacter,
            Character editingCharacter,
            string currentMovementName)
        {
            var allMovements = defaultCharacter.Movements.Select(cm => cm.Movement).Where(m => m != null).Distinct();
            var editingCharacterMovements = editingCharacter.Movements
                .Select(cm => cm.Movement).Where(m => m != null && m.Name != currentMovementName).Distinct();
            return new ObservableCollection<Movement>(allMovements.Except(editingCharacterMovements));
        }

        public MovementAddResult AddMovement(
            Character defaultCharacter,
            CharacterMovement currentCharacterMovement,
            ObservableCollection<Movement> availableMovements)
        {
            if (availableMovements == null)
                availableMovements = new ObservableCollection<Movement>();

            string validMovementName = GetNewValidMovementName(defaultCharacter);
            Movement movement = new Movement(validMovementName);
            var result = new MovementAddResult { Movement = movement, AvailableMovements = availableMovements };

            if (currentCharacterMovement.Character != defaultCharacter
                || (currentCharacterMovement.Character == defaultCharacter && currentCharacterMovement.Movement != null))
            {
                CharacterMovement cmDefault = new CharacterMovement(movement.Name, defaultCharacter);
                cmDefault.Movement = movement;
                defaultCharacter.Movements.Add(cmDefault);

                if (currentCharacterMovement.Character == defaultCharacter)
                {
                    result.UpdatedCurrent = cmDefault;
                    result.AvailableMovements = new ObservableCollection<Movement> { movement };
                }
                else
                {
                    currentCharacterMovement.Movement = movement;
                    result.AvailableMovements.Add(movement);
                }
            }
            else
            {
                currentCharacterMovement.Movement = movement;
                result.AvailableMovements.Add(movement);
            }

            return result;
        }

        public bool TrySubmitRename(
            object state,
            ref string originalName,
            Movement selectedMovement,
            CharacterMovement currentCharacterMovement,
            Character defaultCharacter,
            IMessageBoxService messageBoxService,
            Action<object> cancelEditMode)
        {
            if (originalName == null) return false;

            string updatedName = Helper.GetTextFromControlObject(state);
            bool duplicateName = updatedName != originalName
                && defaultCharacter.Movements.FirstOrDefault(m => m.Name == updatedName) != null;

            if (duplicateName)
            {
                messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Movement", MessageBoxButton.OK, MessageBoxImage.Error);
                cancelEditMode(state);
                return false;
            }

            RenameMovement(originalName, updatedName, selectedMovement, currentCharacterMovement, defaultCharacter);
            originalName = null;
            return true;
        }

        public void RenameMovement(
            string originalName,
            string updatedName,
            Movement selectedMovement,
            CharacterMovement currentCharacterMovement,
            Character defaultCharacter)
        {
            if (originalName == updatedName) return;

            selectedMovement.Name = updatedName;
            currentCharacterMovement.Movement = selectedMovement;
            currentCharacterMovement.Name = updatedName;
            currentCharacterMovement.Character.Movements.UpdateKey(originalName, updatedName);

            CharacterMovement cmDefault = defaultCharacter.Movements.FirstOrDefault(m => m.Name == originalName);
            if (cmDefault != null)
            {
                cmDefault.Name = updatedName;
                defaultCharacter.Movements.UpdateKey(originalName, updatedName);
            }
        }

        public void ApplyDefaultMovement(Character character, CharacterMovement currentCharacterMovement, bool isCurrentlyDefault)
        {
            character.DefaultMovement = isCurrentlyDefault ? currentCharacterMovement : null;
        }
    }
}
