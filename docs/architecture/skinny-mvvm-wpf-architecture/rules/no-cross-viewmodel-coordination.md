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
          // Domain subscribes to domain — no ViewModel involved
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
  // This is cross-ViewModel coordination — the domain no longer owns consistency
  ```

- Hold a reference to another ViewModel.

  **Example (fail):**
  ```csharp
  public class RosterExplorerViewModel : BindableBase
  {
      private readonly CrowdExplorerViewModel _crowdVm;  // wrong — VM knows VM
      ...
  }
  ```

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Cross-feature state consistency is handled in the domain layer, not between ViewModels."
