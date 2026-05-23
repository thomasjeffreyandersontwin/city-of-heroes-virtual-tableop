using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using System.Collections.Generic;

namespace HeroVTT.Movements
{
    public interface IMovementGlobals
    {
        string CurrentActiveWindowName { get; set; }
        bool IsPlayingAttack { get; }
        CharacterMovement ActiveCharacterMovement { get; set; }
        IEnumerable<CharacterMovement> GlobalMovements { get; }
    }

    /// <summary>Production bridge to static Helper.GlobalVariables_* and Helper.GlobalMovements.</summary>
    public sealed class LiveMovementGlobals : IMovementGlobals
    {
        public static readonly LiveMovementGlobals Instance = new LiveMovementGlobals();

        private LiveMovementGlobals() { }

        public string CurrentActiveWindowName
        {
            get { return Helper.GlobalVariables_CurrentActiveWindowName; }
            set { Helper.GlobalVariables_CurrentActiveWindowName = value; }
        }

        public bool IsPlayingAttack
        {
            get { return Helper.GlobalVariables_IsPlayingAttack; }
        }

        public CharacterMovement ActiveCharacterMovement
        {
            get { return Helper.GlobalVariables_CharacterMovement; }
            set { Helper.GlobalVariables_CharacterMovement = value; }
        }

        public IEnumerable<CharacterMovement> GlobalMovements
        {
            get { return Helper.GlobalMovements; }
        }
    }
}
