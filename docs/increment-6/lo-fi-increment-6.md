# Lo-fi Wireframes — Increment 6: Crowd Orchestration and Combat

> Scope: the attack configuration flyout screen introduced in Increment 6, shown in context with the desktop screen it transitions from and returns to.

---

## Metadata

| Field | Value |
| --- | --- |
| Increment | 6 — Crowd Orchestration and Combat |
| State file | `docs/ux/lo-fi/increment-6-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-6.drawio` |
| Drawio file size | 20,621 bytes |
| Screens | desktop (placeholder, col=3 row=0) · attack configuration (col=4 row=0) |
| Connections | desktop → attack configuration · attack configuration → desktop |
| Generated | 2026-05-17 |

---

## Screens

### desktop (col=3, row=0)

- **Layout:** `split-screen` — roster panel (left 50%) · game overlay + context menu (right 50%)
- **Purpose in this increment:** transition origin and destination; shown as a reference anchor for the attack-configuration connection. Full detail is in Increment 5 lo-fi.
- **Regions covered:**
  - roster panel (list): character name · spawned · active; actions: Add · Spawn · Activate
  - game overlay (list): character overlay · status indicator
  - context menu (list): action rows; action: Activate Attack Ability (primary) — the trigger for the transition to *attack configuration*

---

### attack configuration (col=4, row=0)

- **Layout:** `flyout` — body slot 65% (combatant selectors + attack parameters stacked) · panel slot 35% (unused at IA level)
- **Context:** in-session — opened when the GM activates an attack ability from the context menu on the desktop

```
[attack configuration]
┌─────────────────────────────────────────┬───────────────┐
│ combatant selectors                     │               │
│  character name · role                  │  (no IA       │
│  attacker / defender rows               │   content)    │
├─────────────────────────────────────────┤               │
│  Select Attacker · Add Defender         │               │
│  Remove Defender · [Confirm Targets]    │               │
├─────────────────────────────────────────┤               │
│ attack parameters                       │               │
│  Attack Effect    [Stunned ▼]           │               │
│  Knockback Dist.  [_______]             │               │
│  Attack Result    [Hit    ▼]            │               │
│  Attack Mode      [Attack ▼]            │               │
│  Area Center      [☐]                   │               │
├─────────────────────────────────────────┤               │
│  [Confirm]   Cancel   Abort             │               │
└─────────────────────────────────────────┴───────────────┘
```

**Content regions:**

| Region | Slot | Type | Visible fields | Actions |
| --- | --- | --- | --- | --- |
| combatant selectors | body | list | character name · role (attacker / defender) | Select Attacker · Add Defender · Remove Defender · Confirm Targets *(primary)* |
| attack parameters | body | form | Attack Effect (dropdown) · Knockback Distance (text) · Attack Result (dropdown) · Attack Mode (dropdown) · Area Center (checkbox) | Confirm *(primary)* · Cancel · Abort |
| (no IA content) | panel | chrome | — | — |

**Stories covered by this screen:**
- Select Attacking Character
- Activate Attack Ability
- Select Defender Targets
- Confirm Attack Targets
- Configure Attack for Attacker-Defender Pair
- Set Attack Effect (Stunned / Unconscious / Dying / Dead)
- Set Knockback Distance
- Set Attack Result (Hit or Miss)
- Set Attack Mode (Attack or Defend)
- Designate Center Target for Area Attack
- Execute Ranged Area Attack
- Execute Sweep Attack across Multiple Targets
- Assign Auto-Fire Shots per Target
- Spread Attack across Crowd
- Cancel Active Attack
- Abort Attack in Progress
- Reset Character Combat State

**Domain terms:** *attack configuration* · *attacker* · *defender* · *combatant selectors* · *attack parameters* · *attack effect* · *knockback distance* · *attack result* · *attack mode* · *area center*

---

## Connections

| From | To | Label |
| --- | --- | --- |
| desktop | attack configuration | activates attack ability |
| attack configuration | desktop | confirms / cancels attack |

---

## Design notes

- The flyout layout (65% body / 35% panel) is specified in `initial-ia.md` for the attack configuration screen; the panel slot carries no IA-level content and is rendered as a plain chrome block in the wireframe.
- The *combatant selectors* region stacks above *attack parameters* in the body slot; both scroll independently if the combatant list grows.
- The Confirm button in *attack parameters* is primary (blue fill); Cancel and Abort are secondary. Confirm Targets in *combatant selectors* is primary, representing the mid-flow lock-in action.
- The desktop screen at col=3 is included as a reference anchor to render the transition connections; its full lo-fi treatment lives in `increment-5.drawio`.
- No crowd-move context menu entries (Move Crowd with Relative Positioning, Move Crowd with Optimal Spread Positioning, Turn Characters to Face Destination, Align Character Facing with Gang Leader) are shown as new screens — they are all context menu actions on the existing desktop screen and add no new screens to the IA.

---

## Source references

| Artifact | Path |
| --- | --- |
| Initial IA (attack configuration screen) | `docs/ux/initial-ia.md` lines 356–407 |
| Ubiquitous Language Increment 6 | `docs/domain/ubiquitous-language-increment-6.md` |
| Acceptance Criteria Increment 6 | `docs/stories/acceptance-criteria-increment-6.md` |
| CLI script | `C:\dev\agilebydesign-skills\skills\user-experience-design\abd-lo-mockup\scripts\drawio-mockup.mjs` |
