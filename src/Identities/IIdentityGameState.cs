using Module.HeroVirtualTabletop.Library.Utility;

namespace HeroVTT.Identities
{
    public interface IIdentityGameState
    {
        bool IsPlayingAttack { get; }
    }

    public class IdentityGameStateBridge : IIdentityGameState
    {
        public bool IsPlayingAttack
        {
            get { return Helper.GlobalVariables_IsPlayingAttack; }
        }
    }
}
