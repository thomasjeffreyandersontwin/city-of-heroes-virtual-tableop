using Module.HeroVirtualTabletop.Library.GameCommunicator;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Discards all slash commands. Prevents HookCostume loading or game-process attachment during domain tests.
    /// </summary>
    public sealed class NoOpGameCommandExecutor : IGameCommandExecutor
    {
        public static readonly NoOpGameCommandExecutor Instance = new NoOpGameCommandExecutor();

        private NoOpGameCommandExecutor() { }

        public void ExecuteCmd(string command)
        {
        }
    }
}
