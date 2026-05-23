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
