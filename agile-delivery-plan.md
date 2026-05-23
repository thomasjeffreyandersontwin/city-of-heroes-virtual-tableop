# Agile Delivery Plan — Hero Virtual Tabletop

## Context Inventory

**Provided:**

- Acceptance criteria for increments 1–6 (`docs/increment-N/acceptance-criteria-increment-N.md`)
- Ubiquitous language for increments 1–6 (`docs/increment-N/ubiquitous-language-increment-N.md`)
- Architecture reference (`docs/architecture/architecture-reference.md`) — WPF C# three-layer (Presentation/Domain/COH Integration)
- Architecture skill (`hero-vtt-technical-architecture`) with mechanisms: Skinny ViewModel, COH Game Bridge Seam, Direct Memory Manipulation
- Existing e2e tests for increment-1 (`tests/e2e/`) — MSTest + FlaUI, 19 passing / 3 failing
- Existing CRC, SBE, and object model for increment-1 (complete)
- UX mockups and lo-fi wireframes for all increments

**Missing:**

- No CRC, SBE, object model, or tests for increments 2–6
- No production code changes in scope (tests only)

## Risk Classification

- **Low technical risk** — architecture is established, increment-1 is a working reference
- **Medium consistency risk** — 5 increments of domain modeling must stay aligned with a shared vocabulary
- **Low delivery risk** — pipeline is mechanical once patterns are set

## Strategy

**Sequential-increment pipeline with shared agents.** Each increment follows CRC → Review → SBE → Review → ATDD → Review → Run Tests → Fix. One Analyst, one Engineer, one Reviewer for the entire engagement. Increment-1 tests are fixed first as a baseline.

## Runs

### Run 0 — Stabilize increment-1 tests

**Rationale:** Establish a green baseline before generating new tests.

- Stage: Fix 3 failing e2e tests (Engineer)
- Exit: All 22 tests pass

### Run 1 — Increment-2 pipeline

**Rationale:** First full pipeline run; validates the CRC→SBE→ATDD flow and sets patterns for subsequent increments.

- Stage 1: CRC (Analyst)
- Stage 2: Review CRC (Reviewer)
- Stage 3: SBE (Analyst)
- Stage 4: Review SBE (Reviewer)
- Stage 5: ATDD (Engineer)
- Stage 6: Review tests (Reviewer)
- Stage 7: Run tests
- Stage 8: Fix failures (Engineer)
- Exit: Tests pass, all reviews PASS

### Run 2 — Increment-3 pipeline

**Rationale:** Second increment; patterns established, expect faster throughput.

- Same stages as Run 1 with increment-3 artifacts

### Run 3 — Increment-4 pipeline

**Rationale:** Continuation.

- Same stages as Run 1 with increment-4 artifacts

### Run 4 — Increment-5 pipeline

**Rationale:** Continuation.

- Same stages as Run 1 with increment-5 artifacts

### Run 5 — Increment-6 pipeline

**Rationale:** Final increment.

- Same stages as Run 1 with increment-6 artifacts

---

## Refactoring Runs (R0–R5)

**Context:** `tests/domain/` is currently empty. The refactoring pipeline writes Tier 1 + Tier 2 domain tests from SBE, runs them GREEN, then extracts production domain types. The hard gate is non-negotiable: tests GREEN → THEN extract. See `docs/refactoring-thin-slicing.md` for the full vertical slice breakdown.

**Hard gate (locked):** No production extraction until `dotnet test` passes for the domain test project scoped to that increment.

**Thin-slicing reference:** `docs/refactoring-thin-slicing.md`

### Run R0 — Scaffold `tests/domain/` + COH seam fakes

**Rationale:** Unlock testability for all subsequent domain tests — `FakeMemoryInstance` and `NoOpGameCommandExecutor` must exist before any Tier 1 test can compile without a live game process.

- Stage: Engineering — create `CrowdManagement.DomainTests.csproj`, `tests/domain/Support/` (`FakeMemoryInstance.cs`, `NoOpGameCommandExecutor.cs`, `GameCommandTestAssemblyHooks.cs`)
- No test bodies; no production changes
- Exit: `dotnet build` on domain test project returns zero errors

### Run R1 — Increment 1: Crowd Persistence and Tree Domain Tests → Extract

**Rationale:** Crowd persistence and CRUD are the lowest-dependency domain behaviors (no game connection). Tests first against existing production API; extraction follows immediately once green. First concrete validation of the Tier 1 test pattern and ATDD structure.

- Stage 1: Acceptance tests (Engineer) — write Tier 1 + Tier 2 tests for `manage_crowd_repository` in `tests/domain/`; 5 story files; class/method names mirror `tests/e2e/manage_crowd_repository/`
- Stage 2: Review tests (Reviewer) — ATDD structure, orchestrator pattern, domain language, mock boundaries
- Stage 3: Run tests (`dotnet test`) — must reach GREEN
- Stage 4: Engineering — extract `CrowdTree`, `CrowdRepository`, `Crowd`, `CrowdMember` (fix inheritance), `Clipboard` to `src/Crowds/`; slim `CharacterExplorerViewModel` commands to ≤ 3 lines
- Stage 5: Run full test suite — domain + E2E (`manage_crowd_repository/`) green
- Exit: Tier 1+2 green; `CharacterExplorerViewModel` crowd persistence commands ≤ 3 lines; E2E `manage_crowd_repository/` green

### Run R2 — Increments 2 & 3: OptionGroup Pattern + Identity + Animated Ability Domain Tests → Extract

**Rationale:** Identity and ability share the same OptionGroup selection pattern — writing both sub-epics together forces the `OptionGroup` abstraction to emerge from two concrete cases. Slims `OptionGroupViewModel` from 973 LOC.

- Stage 1: Acceptance tests (Engineer) — write Tier 1 + Tier 2 tests for `identity_management` (6 stories) and `animated_ability_management` (6 stories) in `tests/domain/`
- Stage 2: Review tests (Reviewer) — ATDD structure, selection/active invariants, mock boundaries
- Stage 3: Run tests — must reach GREEN
- Stage 4: Engineering — implement `OptionGroup` abstract base + `IdentityOptionGroup`, `AbilityOptionGroup`, `MovementOptionGroup` in `src/`; slim `OptionGroupViewModel` ≤ 200 LOC
- Stage 5: Run tests — domain + E2E green
- Exit: Tier 1+2 green (12 story files); `OptionGroupViewModel` ≤ 200 LOC; E2E `identity_management/` and `animated_ability_management/` green

### Run R3 — Increment 4: Movement Domain Tests → Extract

**Rationale:** `Movement.cs` (2,834 LOC) is the largest single-file violation. Domain tests for authoring and execution drive the split into focused classes. `FakeMemoryInstance` covers the memory read/write boundary.

- Stage 1: Acceptance tests (Engineer) — write Tier 1 + Tier 2 for `character_movement_authoring` (6 stories) and `movement_execution` (10 stories) in `tests/domain/`
- Stage 2: Review tests (Reviewer) — ATDD structure, COH seam via `FakeMemoryInstance`, domain language
- Stage 3: Run tests — must reach GREEN
- Stage 4: Engineering — split `Movement.cs` into `MovementExecution.cs` + `CharacterMovement.cs`; inject `IMemoryInstance` via constructor; slim `MovementEditorViewModel` from 717 LOC
- Stage 5: Run tests — domain + E2E green
- Exit: Tier 1+2 green (16 story files); `new MemoryElement()` sites in movement domain → 0; E2E `character_movement_authoring/` and `movement_execution/` green

### Run R4 — Increment 5: Roster and Desktop Domain Tests → Extract

**Rationale:** `RosterExplorerViewModel` (3,701 LOC) is the biggest violation. `Roster`, `ActiveCharacter`, and `GangMode` types do not exist — all logic is trapped on the ViewModel. Tests drive the domain API into existence; extraction follows.

- Stage 1: Acceptance tests (Engineer) — write Tier 1 + Tier 2 for `roster` (9 stories) and `desktop_overlay` (6 stories) in `tests/domain/`
- Stage 2: Review tests (Reviewer) — ATDD structure, no EventAggregator in domain, mock boundaries
- Stage 3: Run tests — must reach GREEN
- Stage 4: Engineering — create `Roster`, `RosterEntry`, `ActiveCharacter`, `GangMode` in `src/Roster/`; replace EventAggregator roster sync with domain subscriptions; slim `RosterExplorerViewModel`
- Stage 5: Run tests — domain + E2E green
- Exit: Tier 1+2 green (15 story files); `Roster` domain aggregate exists; `RosterExplorerViewModel` measurably reduced; E2E `roster/` and `desktop_overlay/` green

### Run R5 — Increment 6: Combat and Orchestration Domain Tests → Extract

**Rationale:** Final and highest-dependency slice — requires R1–R4 domain types. Eliminates the last major fat-VM behaviors (`CombatExecution`, `CrowdMove`, `HCSIntegrator` split). Drives `RosterExplorerViewModel` to ≤ 300 LOC.

- Stage 1: Acceptance tests (Engineer) — write Tier 1 + Tier 2 for `crowd_move` (5), `attack_configuration` (14), `combat_execution` (10), `hcs_integration` (10) in `tests/domain/`; use `NoOpGameCommandExecutor` + fake file-watcher seam for HCS
- Stage 2: Review tests (Reviewer) — ATDD structure, HCS seam boundary, combat domain language
- Stage 3: Run tests — must reach GREEN
- Stage 4: Engineering — extract `CrowdMove`, `CombatExecution`, `AttackConfiguration`; split `HCSIntegrator.cs` into `HcsFileWatcher` + `HcsCombatEventProcessor`; `RosterExplorerViewModel` ≤ 300 LOC
- Stage 5: Run tests — domain + E2E green
- Exit: Tier 1+2 green (39 story files); `RosterExplorerViewModel` ≤ 300 LOC; `HCSIntegrator.cs` split; §6.1 violation counts at target

---

## Agent Instances

| Role | Agent ID | Scope |
|------|----------|-------|
| Analyst | (recorded at spawn) | All increments — CRC + SBE |
| Engineer | (recorded at spawn) | All increments — ATDD + fix |
| Reviewer | (recorded at spawn) | All increments — alignment checks |

## Runtime

`runtime: isolated-subagent` — Cursor Task tool with resume. One instance per role for the entire engagement.

## Checkpoint Policy

- After each review verdict (PASS/FAIL)
- After test run results
- Between runs (increment transitions)
