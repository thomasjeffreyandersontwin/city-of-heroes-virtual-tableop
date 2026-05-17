namespace Library.GameCommunicator;

/// <summary>
/// Sends slash-commands to City of Heroes (via HookCostume DLL).
/// Injected into domain classes so tests can substitute a no-op.
/// </summary>
public interface IGameCommandExecutor
{
    void ExecuteCmd(string command);
}
