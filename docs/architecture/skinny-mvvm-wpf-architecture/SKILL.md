---
name: skinny-mvvm-wpf-architecture
description: >-
  Generate WPF ViewModels and domain classes that follow the Skinny ViewModel
  pattern from the Hero Virtual Tabletop architecture. Every command handler
  is a one-liner that delegates to a domain method. Every observable property
  is a direct reference to a domain property — no copy, no sync. When a
  structural concern spreads across ViewModels, extract it as a named domain
  class. Use this skill when adding a new feature ViewModel, refactoring a
  fat ViewModel, or extracting a repeated UI concern into the domain.
---
# skinny-mvvm-wpf-architecture

## Purpose

WPF ViewModels become fat when nobody enforces the rule. Business logic creeps in — first a null check, then a filter, then a sort, then the ViewModel knows about keybinds. This skill makes the architecture active: it tells you exactly what shape every ViewModel and domain class must take, and it gives rules a reviewer can apply to any pull request. When the skill is followed, the codebase has one answer to "where does this logic live?" — in the domain — and one answer to "what does the ViewModel do?" — translate gestures and expose domain state.

Principles enforced:
- **ViewModel is a binding adapter.** Command handlers are one-liners. No business logic.
- **Observable domain chain.** Domain objects raise `PropertyChanged` / `CollectionChanged`. ViewModels bind directly — no copy, no sync.
- **No cross-ViewModel coordination.** Domain objects subscribe to each other. ViewModels are passive.
- **Extract UI concerns to domain.** When a structural concern spreads across ViewModels, it is a domain concept.

## When to use this skill

- Adding a **new feature ViewModel** — use the template and rules to start in the right shape.
- **Refactoring a fat ViewModel** (`RosterExplorerViewModel` is the primary AS-IS example) — rules identify exactly what to move.
- A **code review comment** says "this belongs in the domain" but you are not sure what shape the domain class should take.
- A **repeated structural concern** (keyed collection, ordered list, selection state) is appearing in a second ViewModel and you need to decide whether to extract it.
- Onboarding a new engineer — load this skill and the reference once; they can produce a correct ViewModel without pairing.

---

## Agent Instructions

1. **Load the reference.** Read [`inputs/architecture-reference.md`](inputs/architecture-reference.md) — it is the authoritative source for the Skinny ViewModel mechanism: principles, patterns, file structure, participants, flow, walkthrough, and testing. Every decision in this skill traces back to that document.

2. **Identify the scope.** Decide whether you are:
   - **Adding a new ViewModel + domain class** (full module — follow all Build steps).
   - **Refactoring an existing ViewModel** (partial — follow steps 3–5 only).
   - **Extracting a domain concept** from a ViewModel (follow step 4 only).

3. **Code and test standards.** Code generated under this skill follows `abd-clean-code` (domain language, constructor injection, small focused methods, no anemic data bags). Test code follows `abd-acceptance-test-driven-development` (class per story, method per scenario, Given/When/Then helpers, no defensive checks). These are the project's standards; do not reinvent them here.

4. **Use the template.** `templates/viewmodel-module.template.txt` shows the correct shape for a ViewModel + domain class pair. Follow the filled example; do not improvise a different structure.

5. **Validate against rules.** After generating, walk each rule in the bundled block below against the produced code. Fix violations before submitting.

---

## What is the Skinny ViewModel pattern?

A ViewModel in this architecture is a **binding adapter**, not a controller. It has exactly two jobs: translate a user gesture into a domain method call, and expose domain state for XAML binding. Everything else — business rules, COH game logic, ordering, filtering, selection invariants — belongs in the domain.

The domain is **observable by design**: domain objects implement `INotifyPropertyChanged` and `INotifyCollectionChanged` directly. ViewModels bind to domain properties and collections directly — no intermediate copy, no manual sync. When domain state changes, XAML updates automatically through the binding system.

Cross-feature consistency (e.g. removing a character from a Crowd also removes them from the Roster) is handled entirely in the domain layer — domain objects subscribe to each other's change events. ViewModels never know other ViewModels exist.

---

## Core concepts

### Layers

| Layer | Tech | Responsibility |
|---|---|---|
| **Presentation** | WPF XAML, `*ViewModel.cs`, Prism `BindableBase`, `DelegateCommand` | Layout, event routing, data binding. No business logic. |
| **Domain** | Plain C# classes, `INotifyPropertyChanged`, `INotifyCollectionChanged` | All rules, state, and orchestration. Observable by design. |
| **COH Integration** | `IGameCommandExecutor`, `IMemoryInstance`, `IIconInteractionUtility` | Seam between domain and game. Injected — never imported directly in ViewModels. |

### Four ViewModel relationship types

Every ViewModel has exactly four types of relationships with its domain. The template shows all four for `RosterExplorerViewModel`; apply the same shape to every feature.

| Type | Rule |
|---|---|
| **Command → Domain Method** | One-liner handler calls one domain method. No branching, no business logic. |
| **Bound Property → Domain Source** | Direct reference, not a copy. If the domain property is observable, re-expose it as-is. |
| **Domain → Domain Subscription** | Domain objects subscribe to each other for cross-feature consistency. ViewModel is not involved. |
| **Domain Field → XAML Binding** | XAML binds directly to the ViewModel property that references the domain field. |

### Domain extraction trigger

When code review spots **any** of these in a ViewModel, stop and extract:
- A `Dictionary<K, V>` paired with an `ObservableCollection<V>` kept in sync manually
- Ordering or sorting logic
- A structural concern (keyed lookup, uniqueness invariant, grouping) appearing in more than one ViewModel

Name the concept in `ubiquitous-language.md`, create the domain class, delete the ViewModel plumbing. `OptionGroup` (used for `Character.Identities`, `.Abilities`, `.Movements`) is the canonical example.

---

## Example

A correct feature module for `Rosters/`:

```
Rosters/
├── RosterExplorerView.xaml          ← XAML bindings only
├── RosterExplorerViewModel.cs       ← thin: 4 commands, 3 properties, constructor injection
└── Roster.cs                        ← domain: all rules, INotifyPropertyChanged
```

```csharp
// RosterExplorerViewModel.cs — thin adapter
public class RosterExplorerViewModel : BindableBase
{
    private readonly IRoster _roster;

    public RosterExplorerViewModel(IRoster roster)
    {
        _roster = roster;
        ActivateCharacterCommand = new DelegateCommand(Activate, CanActivate);
        SpawnCommand             = new DelegateCommand(Spawn, CanSpawn);
    }

    // Bound properties — direct domain references, no copy
    public ObservableCollection<ICrowdMember> Participants => _roster.Members;
    public ICrowdMember? SelectedCharacter { get; set; }

    public DelegateCommand ActivateCharacterCommand { get; }
    public DelegateCommand SpawnCommand { get; }

    // One-liner handlers — domain owns the logic
    private void Activate() => _roster.ActivateCrowdMember(SelectedCharacter!);
    private void Spawn()    => _roster.SpawnCrowdMember(SelectedCharacter!);
    private bool CanActivate() => SelectedCharacter != null;
    private bool CanSpawn()    => SelectedCharacter != null && !SelectedCharacter.IsSpawned;
}
```

---

## The shape of a good module

```
{Feature}/
├── {Feature}View.xaml          ← XAML only; no code-behind logic
├── {Feature}ViewModel.cs       ← BindableBase; commands + properties; constructor injection
└── {Feature}.cs                ← domain entity; INotifyPropertyChanged; owns all rules

-- when extracting a domain concept --
{Feature}/
├── {Concept}.cs                ← new domain class; named in ubiquitous language
└── {Entity}.cs                 ← updated to expose {Concept} as a property
```

**What must NOT be in a ViewModel:**
- Any `if` that encodes a business rule
- Any `Dictionary`, `List`, or manual sort
- Any direct call to `IGameCommandExecutor`, `IMemoryInstance`, or `IIconInteractionUtility`
- Any reference to another ViewModel

---

## Build

### Full module (new feature)

1. **Create the feature folder** at `Modules/Module.HeroVirtualTabletop/{Feature}/`.
2. **Create the domain class** `{Feature}.cs` implementing `INotifyPropertyChanged`. Add the behaviour methods the ViewModel will call. Add observable collections as `ObservableCollection<T>` properties. Subscribe to other domain objects in the constructor if cross-feature consistency is needed.
3. **Create the ViewModel** `{Feature}ViewModel.cs` extending `BindableBase`. Constructor-inject the domain interface. Wire commands as `DelegateCommand`. Expose domain properties as direct references.
4. **Create the view** `{Feature}View.xaml`. Bind directly to ViewModel properties which reference domain fields. No code-behind logic.
5. **Register** with Unity IoC in `Bootstrapper.cs` / the module's `Initialize()`.
6. **Write Domain tier tests** — plain C# with `NoOpGameCommandExecutor` / `FakeMemoryInstance`. Assert invariants on the domain class directly.
7. **Write ViewModel + Domain tier tests** — wire the real domain to the ViewModel with fakes. Assert both binding state and domain post-state.

### Extracting a domain concept

1. **Name it** in `docs/domain/ubiquitous-language.md`.
2. **Create** `{Concept}.cs` in the feature folder that owns it. Implement `INotifyCollectionChanged` / `INotifyPropertyChanged` as needed. Move the invariant, ordering, and state management from the ViewModel onto the new class.
3. **Update the owning entity** to expose the concept as a property.
4. **Update the ViewModel** — delete the Dictionary / ObservableCollection plumbing; bind directly to the new domain property.
5. **Write Domain tier tests** for the new concept's invariant.

### Refactoring a fat ViewModel

1. **List every `if`** in the ViewModel. Each one that encodes business logic moves to the domain.
2. **List every collection** kept in sync manually. Each one is a candidate for domain extraction.
3. **List every direct COH call**. Each one should be behind an injected interface (see COH Game Bridge Seam mechanism).
4. Apply the extraction steps above for each identified concern.

---

## Validate

Walk these checks against every ViewModel produced or modified:

- **Command handlers are one-liners** — no branching, no multi-step logic, no business rule.
- **Bound properties are direct domain references** — no `new List<>`, no `.ToList()`, no parallel backing collection.
- **ViewModel constructor injects interfaces, not concrete types** — `IRoster`, not `Roster`.
- **No ViewModel imports another ViewModel** — no `RosterExplorerViewModel` field on `CharacterExplorerViewModel`.
- **No ViewModel imports `IGameCommandExecutor`, `IMemoryInstance`, or `IIconInteractionUtility`** — those belong in domain classes.
- **Domain objects implement `INotifyPropertyChanged` / `INotifyCollectionChanged`** — not the ViewModel.
- **Cross-feature consistency is in the domain** — domain objects subscribe to each other in their constructors; no ViewModel mediator.
- **Extracted concepts are named in the ubiquitous language** — `OptionGroup`, not `IdentityCollection`.
- **Domain tier tests exist** — plain C# assertions on domain invariants; no WPF types.
- **ViewModel + Domain tier tests exist** — ViewModel wired to real domain; COH stubbed.

---

<!-- execute_rules:bundle_rules:begin -->
## Rule: ViewModel is a binding adapter — command handlers are one-liners

A ViewModel command handler must contain **exactly one statement**: a call to the corresponding domain method. No branching logic, no null-coalescing beyond a `!` assertion, no business rule, no loop. The ViewModel's job is to translate a user gesture into a domain call — anything beyond that is business logic that belongs in the domain. A reviewer should be able to read a handler and identify which domain method it calls without understanding the business rule.

### DO

- Write every command handler as a single expression body or a single-statement method body that calls one domain method.

  **Example (pass):**
  ```csharp
  private void ActivateSelectedCharacter() =>
      _roster.ActivateCrowdMember(SelectedCharacter!);
  ```

- Put guard conditions on the `CanExecute` predicate, not inside the handler.

  **Example (pass):**
  ```csharp
  private bool CanActivate() => SelectedCharacter != null;
  ```

### DO NOT

- Put any `if` statement inside a command handler.

  **Example (fail):**
  ```csharp
  private void ActivateSelectedCharacter()
  {
      if (SelectedCharacter == null) return;
      if (SelectedCharacter.IsActive) return;  // business rule — belongs in domain
      _roster.ActivateCrowdMember(SelectedCharacter);
  }
  ```

- Call more than one domain method in a handler.

  **Example (fail):**
  ```csharp
  private void Spawn()
  {
      _roster.ClearFromDesktop(SelectedCharacter!);  // orchestration leak
      _roster.SpawnCrowdMember(SelectedCharacter!);
  }
  ```

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Every command handler is a one-liner: call the domain method, done."

---

## Rule: Observable domain chain — bound properties are direct domain references

A ViewModel bound property must be a **direct reference to a domain property or collection** — not a copy, not a re-wrapped version. If the domain object is already `INotifyPropertyChanged` or `INotifyCollectionChanged`, the ViewModel re-exposes it as-is and lets the binding system propagate changes. Creating a separate `ObservableCollection` in the ViewModel that is kept in sync with the domain is the anti-pattern this rule forbids.

### DO

- Expose a domain collection directly as the property the XAML binds to.

  **Example (pass):**
  ```csharp
  // Domain collection — exposed directly, binding system propagates CollectionChanged
  public ObservableCollection<ICrowdMember> Participants => _roster.Members;
  ```

- Expose a domain object property directly.

  **Example (pass):**
  ```csharp
  public OptionGroup Identities => _character.Identities;  // direct — no copy
  ```

### DO NOT

- Create a ViewModel-owned collection and populate it from the domain.

  **Example (fail):**
  ```csharp
  private ObservableCollection<ICrowdMember> _participants = new();
  public ObservableCollection<ICrowdMember> Participants => _participants;

  // somewhere in constructor or event handler:
  foreach (var m in _roster.Members) _participants.Add(m);  // manual sync — wrong
  ```

- Wrap a domain value in a ViewModel-owned property setter that copies it.

  **Example (fail):**
  ```csharp
  private string _characterName = "";
  public string CharacterName
  {
      get => _characterName;
      set { _characterName = value; RaisePropertyChanged(); }
  }
  // populated by: CharacterName = _character.Name;  — stale copy
  ```

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Observable properties are direct references to domain properties — no copy, no sync."

---

## Rule: No cross-ViewModel coordination — domain handles cross-feature consistency

ViewModels must not know other ViewModels exist. Cross-feature state consistency (e.g. removing a character from a Crowd also updates the Roster view) is handled entirely by domain objects subscribing to each other's `CollectionChanged` or `PropertyChanged` events. A ViewModel that mediates between two other ViewModels, uses a Prism `IEventAggregator` to send messages to another ViewModel, or holds a reference to another ViewModel is a violation of this rule.

### DO

- Wire cross-feature consistency as a domain-to-domain subscription in the constructor of the subscribing domain class.

  **Example (pass):**
  ```csharp
  public class Roster : IRoster, INotifyPropertyChanged
  {
      private readonly ICrowd _crowd;

      public Roster(ICrowd crowd)
      {
          _crowd = crowd;
          _crowd.Members.CollectionChanged += OnCrowdMembersChanged;
      }

      private void OnCrowdMembersChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
          if (e.OldItems == null) return;
          foreach (ICrowdMember removed in e.OldItems)
              Members.Remove(removed);
          // RosterExplorerViewModel sees the change automatically through binding
      }
  }
  ```

### DO NOT

- Publish an event aggregator message from one ViewModel and subscribe in another.

  **Example (fail):**
  ```csharp
  // CrowdExplorerViewModel.cs
  _eventAggregator.GetEvent<CharacterRemovedEvent>().Publish(removed);

  // RosterExplorerViewModel.cs
  _eventAggregator.GetEvent<CharacterRemovedEvent>().Subscribe(OnCharacterRemoved);
  // Cross-ViewModel coordination — domain no longer owns consistency
  ```

- Hold a reference to another ViewModel.

  **Example (fail):**
  ```csharp
  public class RosterExplorerViewModel : BindableBase
  {
      private readonly CrowdExplorerViewModel _crowdVm;  // wrong — VM knows VM
  }
  ```

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Cross-feature state consistency is handled in the domain layer, not between ViewModels."

---

## Rule: Extract UI concerns to domain — name structural ViewModel logic as domain concepts

When a structural or display concern appears in a ViewModel — a keyed collection, an ordering invariant, a uniqueness rule, a selection state — and that concern is not purely a rendering detail, it must be extracted into a named domain class. The trigger is any of: a `Dictionary<K,V>` kept in sync with an `ObservableCollection<V>`, ordering logic in a ViewModel, or the same structural pattern appearing in more than one ViewModel. Once extracted, the concept is named in the ubiquitous language, the domain class owns the invariant, and the ViewModel becomes a pass-through.

`OptionGroup` is the canonical example: Character exposes `.Identities`, `.Abilities`, and `.Movements` as `OptionGroup` instances. Uniqueness enforcement, ordering, and `CollectionChanged` live on `OptionGroup`. ViewModels bind directly — they do not duplicate the plumbing.

### DO

- When code review spots a Dictionary + ObservableCollection pair in a ViewModel, name the concept and create the domain class.

  **Example (pass) — after extraction:**
  ```csharp
  // OptionGroup.cs — domain class; owns uniqueness and ordering
  public class OptionGroup : INotifyCollectionChanged
  {
      private readonly Dictionary<string, IOption> _index = new();

      public void Add(IOption option)
      {
          if (_index.ContainsKey(option.Key)) return; // uniqueness enforced here
          _index[option.Key] = option;
          CollectionChanged?.Invoke(this, ...);
      }
  }

  // Character.cs exposes it
  public OptionGroup Identities { get; } = new();

  // IdentityEditorViewModel.cs — pass-through
  public OptionGroup Identities => _character.Identities;
  ```

- Name the extracted concept in `docs/domain/ubiquitous-language.md` before or immediately after creating the class.

### DO NOT

- Keep the Dictionary + ObservableCollection pair in the ViewModel after identifying the pattern.

  **Example (fail) — before extraction:**
  ```csharp
  // IdentityEditorViewModel.cs — structural concern in presentation layer
  private Dictionary<string, IIdentity> _identityIndex = new();
  private ObservableCollection<IIdentity> _identities = new();

  public void AddIdentity(IIdentity id)
  {
      if (_identityIndex.ContainsKey(id.Key)) return;  // uniqueness in VM — wrong
      _identityIndex[id.Key] = id;
      _identities.Add(id);
  }
  ```

- Name the extracted class with a ViewModel term (`IdentityListManager`, `IdentityHelper`) instead of a domain term.

  **Example (fail):** Class named `IdentityCollectionHelper` instead of `OptionGroup`. The concept is hidden from the domain language.

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Domain extraction trigger: when code review spots a Dictionary + ObservableCollection pair kept in sync manually... name it in the ubiquitous language, create the domain class, delete the ViewModel plumbing."
<!-- execute_rules:bundle_rules:end -->
