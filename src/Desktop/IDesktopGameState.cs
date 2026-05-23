using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Utility;

namespace HeroVTT.Desktop
{
    public interface IDesktopGameState
    {
        AnimatedAbility DefaultSweepAbility { get; }
    }

    public class HelperBackedDesktopGameState : IDesktopGameState
    {
        public AnimatedAbility DefaultSweepAbility
        {
            get { return Helper.GlobalDefaultSweepAbility; }
        }
    }
}
