using System.ComponentModel;
using System.Runtime.CompilerServices;
using Library.GameCommunicator;
using Library.ProcessCommunicator;

namespace Module.HeroVirtualTabletop.Characters;

/// <summary>
/// Domain class. Holds all business rules for a single character.
/// Receives IGameCommandExecutor and IMemoryInstance via constructor —
/// never references HookCostumeGameCommandExecutor or MemoryInstance directly.
/// </summary>
public class Character : INotifyPropertyChanged
{
    private readonly IGameCommandExecutor _executor;
    private readonly IMemoryInstance      _memory;

    public string Name { get; }

    // OptionGroup owns uniqueness and CollectionChanged — ViewModel binds directly, no copy
    public OptionGroup<Identity> Identities { get; } = new(id => id.Name);

    public Identity? ActiveIdentity => Identities.FirstOrDefault();

    private bool _isSpawned;
    public bool IsSpawned
    {
        get => _isSpawned;
        private set { _isSpawned = value; OnPropertyChanged(); }
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public Character(string name, IGameCommandExecutor executor, IMemoryInstance memory)
    {
        Name      = name;
        _executor = executor;
        _memory   = memory;
    }

    /// <summary>
    /// Sends spawn_npc to COH (Path 1 — DLL) then confirms via memory label (Path 2 — MemorySharp).
    /// </summary>
    public void Spawn()
    {
        if (ActiveIdentity is null)
            throw new InvalidOperationException($"Character '{Name}' has no identity assigned.");

        var cmd = $"spawn_npc {ActiveIdentity.Surface} {Name}";
        _executor.ExecuteCmd(cmd);                          // Path 1: keybind → HookCostume

        var confirmed = _memory.GetCurrentTargetLabel();    // Path 2: read process memory
        if (confirmed == Name) IsSpawned = true;
    }

    public void ClearFromDesktop()
    {
        _executor.ExecuteCmd($"target_name {Name}");
        _executor.ExecuteCmd("delete");
        IsSpawned = false;
        IsActive  = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
