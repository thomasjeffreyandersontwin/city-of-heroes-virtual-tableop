using System.Collections.ObjectModel;
using Library.GameCommunicator;
using Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Rosters;

/// <summary>
/// Domain class. Manages the set of characters the GM has added to the active session.
/// Cross-feature sync (e.g. removing a character when the crowd changes) happens here —
/// not between ViewModels.
/// </summary>
public class Roster
{
    private readonly IGameCommandExecutor _executor;
    private readonly IMemoryInstance      _memory;

    public ObservableCollection<Character> Members { get; } = new();

    public Character? ActiveCharacter => Members.FirstOrDefault(m => m.IsActive);

    public Roster(IGameCommandExecutor executor, IMemoryInstance memory)
    {
        _executor = executor;
        _memory   = memory;
    }

    public void SpawnCrowdMember(Character character) =>
        character.Spawn();

    public void ActivateCrowdMember(Character character)
    {
        foreach (var m in Members) m.IsActive = false;
        character.IsActive = true;
    }

    public void ClearFromDesktop(Character character) =>
        character.ClearFromDesktop();
}
