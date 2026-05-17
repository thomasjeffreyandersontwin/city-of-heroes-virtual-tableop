using System.Collections.ObjectModel;
using Module.HeroVirtualTabletop.Characters;
using Prism.Commands;
using Prism.Mvvm;

namespace Module.HeroVirtualTabletop.Rosters;

/// <summary>
/// Skinny ViewModel — the target pattern from the architecture reference.
///
/// Every command handler is one line: delegate to the domain, done.
/// Observable properties are direct references to domain state — no copy, no sync.
/// No knowledge of game internals, keybinds, memory offsets, or other ViewModels.
///
/// Contrast with the AS-IS RosterExplorerViewModel.cs in the main project
/// which has 1000+ lines, 8 injected services, and business logic in the VM.
/// </summary>
public class RosterExplorerViewModel : BindableBase
{
    private readonly Roster _roster;

    public RosterExplorerViewModel(Roster roster)
    {
        _roster = roster;

        SpawnCommand           = new DelegateCommand(Spawn,   () => SelectedCharacter is not null)
                                     .ObservesProperty(() => SelectedCharacter);
        ActivateCommand        = new DelegateCommand(Activate, () => SelectedCharacter is not null)
                                     .ObservesProperty(() => SelectedCharacter);
        ClearFromDesktopCommand = new DelegateCommand(Clear,  () => SelectedCharacter is not null)
                                     .ObservesProperty(() => SelectedCharacter);
    }

    // Direct reference to domain collection — ViewModel owns no copy
    public ObservableCollection<Character> Participants => _roster.Members;

    private Character? _selectedCharacter;
    public Character? SelectedCharacter
    {
        get => _selectedCharacter;
        set => SetProperty(ref _selectedCharacter, value);
    }

    public Character? ActiveCharacter => _roster.ActiveCharacter;

    public DelegateCommand SpawnCommand            { get; }
    public DelegateCommand ActivateCommand         { get; }
    public DelegateCommand ClearFromDesktopCommand { get; }

    // One-liners — all logic lives in the domain
    private void Spawn()    => _roster.SpawnCrowdMember(SelectedCharacter!);
    private void Activate() => _roster.ActivateCrowdMember(SelectedCharacter!);
    private void Clear()    => _roster.ClearFromDesktop(SelectedCharacter!);
}
