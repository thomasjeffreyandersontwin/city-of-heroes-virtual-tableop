namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    /// <summary>
    /// Unit-test stand-in: does not load HookCostume or touch the game process.
    /// </summary>
    internal sealed class NoOpGameCommandExecutor : IGameCommandExecutor
    {
        internal static readonly NoOpGameCommandExecutor Instance = new NoOpGameCommandExecutor();

        private NoOpGameCommandExecutor() { }

        public void ExecuteCmd(string command)
        {
            // Intentionally empty — tests assert bind files / generated strings only.
        }
    }
}
