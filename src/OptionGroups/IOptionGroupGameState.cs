using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.OptionGroups
{
    public interface IOptionGroupGameState
    {
        bool IsPlayingAttack { get; set; }
        string CurrentActiveWindowName { get; set; }
        AnimatedAbility GlobalDefaultSweepAbility { get; }
    }

    public class OptionGroupGameStateBridge : IOptionGroupGameState
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

        public AnimatedAbility GlobalDefaultSweepAbility
        {
            get { return Helper.GlobalDefaultSweepAbility; }
        }
    }
}
