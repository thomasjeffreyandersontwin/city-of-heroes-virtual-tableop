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
      _roster.ClearFromDesktop(SelectedCharacter!);  // why is clear here? orchestration leak
      _roster.SpawnCrowdMember(SelectedCharacter!);
  }
  ```

**Source:** `inputs/architecture-reference.md` — Mechanism: Skinny ViewModel, Principles & Patterns: "Every command handler is a one-liner: call the domain method, done."
