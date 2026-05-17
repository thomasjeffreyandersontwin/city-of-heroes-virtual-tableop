namespace Library.GameCommunicator;

/// <summary>
/// Test double: discards all commands. Exposes LastCommand so tests can assert
/// which keybind string was sent without touching the game process.
/// </summary>
public class NoOpGameCommandExecutor : IGameCommandExecutor
{
    public string? LastCommand { get; private set; }

    public void ExecuteCmd(string command) => LastCommand = command;
}
