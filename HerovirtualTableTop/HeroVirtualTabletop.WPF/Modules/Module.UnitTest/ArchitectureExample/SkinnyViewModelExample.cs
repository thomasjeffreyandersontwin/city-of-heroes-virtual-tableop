// ─────────────────────────────────────────────────────────────────────────────
// Architecture example: Skinny ViewModel pattern
// Reference: docs/architecture/architecture-reference.md §Mechanism: Skinny ViewModel
//
// Three things in one file so it reads as a unit:
//   1. ExampleCharacter       — domain class, constructor-injected seam
//   2. ExampleRosterViewModel — skinny ViewModel, one-liner commands, direct bindings
//   3. Tests                  — Tier 1 (domain only) + Tier 2 (ViewModel + domain)
//
// Run with the existing scripts/test.ps1 — runner.exe picks these up automatically.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using FluentAssertions;

namespace Module.UnitTest.ArchitectureExample
{
    // ── 1. Domain class ───────────────────────────────────────────────────────
    //
    // IGameCommandExecutor injected via constructor.
    // Never references HookCostumeGameCommandExecutor or GameCommandExecution.ActiveExecutor.
    // All business rules live here. ViewModel knows nothing about this logic.

    public class ExampleCharacter : INotifyPropertyChanged
    {
        private readonly IGameCommandExecutor _executor;

        public string Name    { get; private set; }
        public string Surface { get; private set; }   // NPC model name for spawn_npc

        private bool _isSpawned;
        public bool IsSpawned
        {
            get { return _isSpawned; }
            private set { _isSpawned = value; OnPropertyChanged(); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); }
        }

        public ExampleCharacter(string name, string surface, IGameCommandExecutor executor)
        {
            Name      = name;
            Surface   = surface;
            _executor = executor;
        }

        // Business rule: build the keybind string and send it through the seam.
        // ViewModel calls this in one line — it knows nothing about keybind format.
        public void Spawn()
        {
            _executor.ExecuteCmd(string.Format("spawn_npc {0} {1}", Surface, Name));
            IsSpawned = true;
        }

        public void ClearFromDesktop()
        {
            _executor.ExecuteCmd(string.Format("target_name {0}", Name));
            _executor.ExecuteCmd("delete");
            IsSpawned = false;
            IsActive  = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(name));
        }
    }

    // ── 2. Skinny ViewModel ───────────────────────────────────────────────────
    //
    // Every command handler: one line, delegates to domain.
    // Participants is a direct reference to the domain collection — no copy, no sync.
    // No knowledge of keybinds, memory, services, or other ViewModels.
    //
    // Contrast with RosterExplorerViewModel.cs (the AS-IS) which injects 8 services
    // and spreads business logic across 1000+ lines.

    public class ExampleRosterViewModel : BindableBase
    {
        private readonly ObservableCollection<ExampleCharacter> _members;

        public ExampleRosterViewModel(ObservableCollection<ExampleCharacter> members)
        {
            _members = members;

            SpawnCommand            = new DelegateCommand(Spawn,    CanExecute);
            ActivateCommand         = new DelegateCommand(Activate, CanExecute);
            ClearFromDesktopCommand = new DelegateCommand(Clear,    CanExecute);
        }

        // Direct reference — ViewModel owns no copy of domain state
        public ObservableCollection<ExampleCharacter> Participants { get { return _members; } }

        private ExampleCharacter _selectedCharacter;
        public ExampleCharacter SelectedCharacter
        {
            get { return _selectedCharacter; }
            set
            {
                SetProperty(ref _selectedCharacter, value);
                SpawnCommand.RaiseCanExecuteChanged();
                ActivateCommand.RaiseCanExecuteChanged();
                ClearFromDesktopCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand SpawnCommand            { get; private set; }
        public DelegateCommand ActivateCommand         { get; private set; }
        public DelegateCommand ClearFromDesktopCommand { get; private set; }

        // One-liners — all logic lives in the domain class
        private void Spawn()    { _selectedCharacter.Spawn(); }
        private void Activate()
        {
            foreach (var m in _members) m.IsActive = false;
            _selectedCharacter.IsActive = true;
        }
        private void Clear() { _selectedCharacter.ClearFromDesktop(); }

        private bool CanExecute() { return _selectedCharacter != null; }
    }

    // ── 3. Tests ──────────────────────────────────────────────────────────────

    // Tier 1 — Domain tests. No ViewModel. COH seam replaced by a capturing fake.
    [TestClass]
    public class ExampleCharacterDomainTests
    {
        private CapturingExecutor _executor;
        private ExampleCharacter  _hero;

        [TestInitialize]
        public void GivenASpawnableCharacter()
        {
            _executor = new CapturingExecutor();
            _hero     = new ExampleCharacter("Hero1", "Model_Statesman", _executor);
        }

        [TestMethod]
        public void WhenSpawned_ThenIsSpawnedIsTrue()
        {
            _hero.Spawn();
            _hero.IsSpawned.Should().BeTrue();
        }

        [TestMethod]
        public void WhenSpawned_ThenCommandContainsSurfaceAndName()
        {
            _hero.Spawn();
            _executor.LastCommand.Should().Contain("Model_Statesman").And.Contain("Hero1");
        }

        [TestMethod]
        public void WhenCleared_ThenIsSpawnedAndIsActiveBothFalse()
        {
            _hero.Spawn();
            _hero.IsActive = true;

            _hero.ClearFromDesktop();

            _hero.IsSpawned.Should().BeFalse();
            _hero.IsActive.Should().BeFalse();
        }
    }

    // Tier 2 — ViewModel + Domain. Real domain, COH still captured.
    // Asserts both the ViewModel binding state AND the domain post-state.
    [TestClass]
    public class ExampleRosterViewModelTests
    {
        private CapturingExecutor                      _executor;
        private ObservableCollection<ExampleCharacter> _members;
        private ExampleRosterViewModel                 _vm;
        private ExampleCharacter                       _hero;

        [TestInitialize]
        public void GivenAViewModelWiredToRealDomain()
        {
            _executor = new CapturingExecutor();
            _hero     = new ExampleCharacter("Hero1", "Model_Statesman", _executor);
            _members  = new ObservableCollection<ExampleCharacter> { _hero };
            _vm       = new ExampleRosterViewModel(_members);
            _vm.SelectedCharacter = _hero;
        }

        [TestMethod]
        public void WhenGmClicksSpawn_ThenParticipantIsMarkedSpawned()
        {
            _vm.SpawnCommand.Execute();

            // Domain post-state
            _hero.IsSpawned.Should().BeTrue();
            // Binding: Participants is a direct reference — reflects domain automatically
            _vm.Participants.Should().Contain(c => c.Name == "Hero1" && c.IsSpawned);
        }

        [TestMethod]
        public void WhenGmClicksActivate_ThenOnlySelectedCharacterIsActive()
        {
            var hero2 = new ExampleCharacter("Hero2", "Model_Villain", _executor);
            _members.Add(hero2);
            hero2.IsActive = true;   // a different character was active first

            _vm.ActivateCommand.Execute();

            _hero.IsActive.Should().BeTrue();
            hero2.IsActive.Should().BeFalse();   // domain deactivated the other one
        }
    }

    // ── Capturing fake ────────────────────────────────────────────────────────
    // Used directly where tests need to assert the exact command string sent to COH.
    // (The global NoOpGameCommandExecutor installed by GameCommandTestAssemblyHooks
    // is for classes that call GameCommandExecution.ActiveExecutor — not needed here
    // because ExampleCharacter receives the executor via constructor injection.)

    internal class CapturingExecutor : IGameCommandExecutor
    {
        public string LastCommand { get; private set; }
        public void ExecuteCmd(string command) { LastCommand = command; }
    }
}
