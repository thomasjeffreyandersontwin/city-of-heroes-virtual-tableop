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
      private readonly ObservableCollection<IOption> _items = new();

      public void Add(IOption option)
      {
          if (_index.ContainsKey(option.Key)) return; // uniqueness enforced here
          _index[option.Key] = option;
          _items.Add(option);
          CollectionChanged?.Invoke(this, ...);
      }
      ...
  }

  // Character.cs exposes it
  public OptionGroup Identities { get; } = new();

  // IdentityEditorViewModel.cs — pass-through
  public OptionGroup Identities => _character.Identities;
  public DelegateCommand<IIdentity> AddIdentityCommand { get; }
  // handler: id => _character.Identities.Add(id)
  ```

- Name the extracted concept in `docs/domain/ubiquitous-language.md` before or immediately after creating the class.

### DO NOT

- Keep the Dictionary + ObservableCollection pair in the ViewModel after identifying the pattern.

  **Example (fail) — before extraction:**
  ```csharp
  // IdentityEditorViewModel.cs — structural concern living in presentation layer
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

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Domain extraction trigger: when code review spots a Dictionary + ObservableCollection pair kept in sync manually, ordering logic, or any structural concern spreading across more than one ViewModel — name it in the ubiquitous language, create the domain class, delete the ViewModel plumbing."
