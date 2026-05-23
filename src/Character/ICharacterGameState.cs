using Module.HeroVirtualTabletop.Library.Utility;

namespace HeroVTT.Characters
{
    public interface ICharacterGameState
    {
        bool IsPlayingAttack { get; set; }
        string CurrentActiveWindowName { get; set; }
    }

    public class CharacterGameStateBridge : ICharacterGameState
    {
        public bool IsPlayingAttack
        {
            get { return Helper.GlobalVariables_IsPlayingAttack; }
            set { Helper.GlobalVariables_IsPlayingAttack = value; }
        }

        public string CurrentActiveWindowName
        {
            get { return Helper.GlobalVariables_CurrentActiveWindowName; }
            set { Helper.GlobalVariables_CurrentActiveWindowName = value; }
        }
    }
}
