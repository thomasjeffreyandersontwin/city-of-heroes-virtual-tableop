namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    /// <summary>
    /// Active command sink for slash-commands. The host app uses HookCostume;
    /// unit tests replace <see cref="ActiveExecutor"/> (see Module.UnitTest assembly initialize).
    /// </summary>
    public static class GameCommandExecution
    {
        private static IGameCommandExecutor _activeExecutor = new HookCostumeGameCommandExecutor();

        public static IGameCommandExecutor ActiveExecutor
        {
            get { return _activeExecutor; }
            set { _activeExecutor = value ?? new HookCostumeGameCommandExecutor(); }
        }

        public static void ExecuteCmd(string command)
        {
            _activeExecutor.ExecuteCmd(command);
        }
    }
}
