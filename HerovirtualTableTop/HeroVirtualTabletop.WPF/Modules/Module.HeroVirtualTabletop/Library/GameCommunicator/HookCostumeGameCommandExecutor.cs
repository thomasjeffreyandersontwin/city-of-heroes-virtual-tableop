using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    /// <summary>
    /// Production implementation: forwards to <see cref="IconInteractionUtility"/>.
    /// Referencing <see cref="IconInteractionUtility"/> happens on first <see cref="ExecuteCmd"/>, not when this instance is constructed.
    /// </summary>
    public sealed class HookCostumeGameCommandExecutor : IGameCommandExecutor
    {
        public void ExecuteCmd(string command)
        {
            IconInteractionUtility.ExecuteCmd(command);
        }
    }
}
