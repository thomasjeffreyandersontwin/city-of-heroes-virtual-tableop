# Scanners — skinny-mvvm-wpf-architecture

No automated scanners are shipped with this skill yet.

The following rules are candidates for automated enforcement when tooling is available:

| Rule | Target | TODO |
|---|---|---|
| `viewmodel-is-binding-adapter` | Command handler method bodies > 1 statement | write `scanners/csharp/command_handler_size_scanner.py` |
| `observable-domain-chain` | ViewModel-owned `ObservableCollection` backed properties | write `scanners/csharp/copied_collection_scanner.py` |
| `no-cross-viewmodel-coordination` | `IEventAggregator` usage in ViewModel constructors | write `scanners/csharp/cross_vm_coordination_scanner.py` |
| `extract-ui-concern-to-domain` | `Dictionary<,>` field on a ViewModel class | write `scanners/csharp/dictionary_in_viewmodel_scanner.py` |

Until scanners are present, all rules are **Manual review** (code review + Validate checklist in `SKILL.md`).
