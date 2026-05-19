namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    /// <summary>
    /// Sends slash-commands to the City of Heroes client (via HookCostume).
    /// Injectable so unit tests can substitute a no-op implementation.
    /// </summary>
    public interface IGameCommandExecutor
    {
        void ExecuteCmd(string command);
    }
}
