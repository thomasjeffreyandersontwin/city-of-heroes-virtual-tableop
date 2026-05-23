# Delivery Plan Checklist

## Orchestration

- [x] Workspace established
- [x] Delivery plan written (`agile-delivery-plan.md`)
- [x] Checklist created
- [x] Agent instances spawned (Analyst, Engineer)
- [ ] Reviewer instance spawned

## Run 0 — Stabilize increment-1 tests

- [x] Identify failing tests (3 failures from runner-output.txt)
- [x] Engineer fixes test failures
- [ ] Re-run tests — all 22 pass

## Run 1 — Increment-2 pipeline

- [x] Analyst: CRC (`docs/increment-2/crc-increment-2.md`)
- [x] Reviewer: CRC alignment check — PASS (3 low-severity advisories noted)
- [x] Analyst: SBE (`docs/increment-2/specification-by-example-increment-2.md`)
- [x] Reviewer: SBE alignment check — PASS (after 1 fix cycle)
- [x] Engineer: ATDD (`tests/e2e/` — 47 files, 165 methods)
- [x] Reviewer: test alignment check — PASS (1 medium: DataRow refactor opportunity)
- [ ] Run tests (blocked — RED phase, AppDriver stubs needed)
- [ ] Fix failures (if any)

## Run 2 — Increment-3 pipeline

- [x] Analyst: CRC (`docs/increment-3/crc-increment-3.md`)
- [x] Reviewer: CRC alignment check — PASS (1 medium: auto-play trigger gap)
- [x] Analyst: SBE (`docs/increment-3/specification-by-example-increment-3.md`)
- [x] Reviewer: SBE alignment check — PASS
- [x] Engineer: ATDD (`tests/e2e/` — 37 files, 135 methods)
- [x] Reviewer: test alignment check — PASS (clean, 0 findings)
- [ ] Run tests (blocked — RED phase)
- [ ] Fix failures (if any)

## Run 3 — Increment-4 pipeline

- [x] Analyst: CRC (`docs/increment-4/crc-increment-4.md`)
- [x] Reviewer: CRC alignment check — PASS
- [x] Analyst: SBE (`docs/increment-4/specification-by-example-increment-4.md`)
- [x] Reviewer: SBE alignment check — PASS (clean, 0 findings)
- [x] Engineer: ATDD (`tests/e2e/` — 39 files, 103 methods)
- [x] Reviewer: test alignment check — PASS (clean)
- [ ] Run tests
- [ ] Fix failures (if any)

## Run 4 — Increment-5 pipeline

- [x] Analyst: CRC (`docs/increment-5/crc-increment-5.md`)
- [x] Reviewer: CRC alignment check — PASS (1 medium: Context Menu vs Multi-Select contradiction)
- [x] Analyst: SBE (`docs/increment-5/specification-by-example-increment-5.md`)
- [x] Reviewer: SBE alignment check — PASS
- [x] Engineer: ATDD (`tests/e2e/` — 38 files, 136 methods)
- [x] Reviewer: test alignment check — PASS (clean)
- [ ] Run tests
- [ ] Fix failures (if any)

## Run 5 — Increment-6 pipeline

- [x] Analyst: CRC (`docs/increment-6/crc-increment-6.md`)
- [x] Reviewer: CRC alignment check — PASS (1 low: missing Context Menu boundary entry)
- [x] Analyst: SBE (`docs/increment-6/specification-by-example-increment-6.md`)
- [x] Reviewer: SBE alignment check — PASS
- [x] Engineer: ATDD (`tests/e2e/` — 47 files, 160 methods)
- [x] Reviewer: test alignment check — PASS (clean)
- [ ] Run tests
- [ ] Fix failures (if any)
