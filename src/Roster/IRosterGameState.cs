using HeroVTT.Desktop;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using System.Collections.Generic;

namespace HeroVTT.Roster
{
    public interface IRosterGameState : IDesktopGameState
    {
        CharacterMovement ActiveCharacterMovement { get; set; }
        CharacterMovement FormerActiveCharacterMovement { get; set; }
        Character ActiveCharacter { get; set; }
        bool IsPlayingAttack { get; set; }
        bool IntegrateWithHCS { get; set; }
        string CurrentActiveWindowName { get; set; }
        List<AnimatedAbility> DefaultAbilities { get; }
    }

    public class HelperBackedRosterGameState : IRosterGameState
    {
        public CharacterMovement ActiveCharacterMovement
        {
            get { return Helper.GlobalVariables_CharacterMovement; }
            set { Helper.GlobalVariables_CharacterMovement = value; }
        }

        public CharacterMovement FormerActiveCharacterMovement
        {
            get { return Helper.GlobalVariables_FormerActiveCharacterMovement; }
            set { Helper.GlobalVariables_FormerActiveCharacterMovement = value; }
        }

        public Character ActiveCharacter
        {
            get { return Helper.GlobalVariables_ActiveCharacter; }
            set { Helper.GlobalVariables_ActiveCharacter = value; }
        }

        public bool IsPlayingAttack
        {
            get { return Helper.GlobalVariables_IsPlayingAttack; }
            set { Helper.GlobalVariables_IsPlayingAttack = value; }
        }

        public bool IntegrateWithHCS
        {
            get { return Helper.GlobalVariables_IntegrateWithHCS; }
            set { Helper.GlobalVariables_IntegrateWithHCS = value; }
        }

        public string CurrentActiveWindowName
        {
            get { return Helper.GlobalVariables_CurrentActiveWindowName; }
            set { Helper.GlobalVariables_CurrentActiveWindowName = value; }
        }

        public AnimatedAbility DefaultSweepAbility
        {
            get { return Helper.GlobalDefaultSweepAbility; }
        }

        public List<AnimatedAbility> DefaultAbilities
        {
            get { return Helper.GlobalDefaultAbilities; }
        }
    }
}
