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

- Let XAML bind to the ViewModel property; the domain raises `PropertyChanged` and XAML updates automatically.

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
