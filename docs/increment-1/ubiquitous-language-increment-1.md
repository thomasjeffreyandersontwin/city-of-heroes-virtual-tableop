---
state: ubiquitous-language
increment: 1
scope: Character and Crowd Library
date: 2026-05-17
---

# Ubiquitous Language — Increment 1: Character and Crowd Library

> Scope: the vocabulary needed to create, organize, browse, and persist *characters* and *crowds* without any live game connection. No game bridge, no identity rendering, no movement or ability execution. Tests at this increment validate CRUD, clipboard operations, filtering, and JSON persistence only.

---

## Key Abstractions

| KA | One-line definition |
| --- | --- |
| **Character** | The named data entity the GM creates and organizes; the subject of all crowd membership operations |
| **Crowd** | A named, hierarchical container of *characters* and nested *crowds* the GM organizes for scene staging |
| **Crowd Repository** | The persistent JSON store that serializes, deserializes, and backs up the entire *crowd* hierarchy |
| **Application Shell** | The Prism-based WPF host that initializes modules and opens the *crowd manager* workspace on startup |
| **COH Game Directory** | The file-system path to the City of Heroes installation, validated at startup and used to locate the data directory for the *crowd repository* |

---

## Application Shell

The *application shell* is the Prism WPF host process that starts the Hero Virtual Tabletop. It validates the *COH game directory*, loads the *Prism module* that hosts the *crowd manager*, and opens the *character crowd main workspace* as the initial view. In Increment 1 the shell performs no game connection — its sole concern is configuration validation and surface initialization.

### application shell

- validates the *COH game directory* on startup before loading any module; blocks module initialization if the directory is absent or invalid
- prompts the GM to supply or correct the *COH game directory* path when validation fails, displaying a modal *game directory prompt*
- loads the *Prism module* after a valid *COH game directory* is confirmed, registering views and view-models with the Prism container
- opens the *character crowd main workspace* as the first visible surface after module load
- **Invariant:** the *Prism module* is never loaded until the *COH game directory* passes validation; the *game directory prompt* must be dismissed with a valid path before the workspace appears

### Prism module

- is the WPF plug-in assembly that registers the *crowd manager* views, view-models, and services with the Prism IoC container
- is loaded by the *application shell* on confirmed startup; it owns the *crowd manager* navigation region
- **Decision:** `Prism module` is a concept at Increment 1 because module load is an observable, testable event (module initialized vs. not); subsequent increments add additional modules

### game directory prompt

- is a modal dialog the *application shell* displays when the *COH game directory* is absent or fails validation
- presents a single form: a text input for the directory path, a Browse button that opens a folder picker, a validation feedback label, and a Continue button disabled until the path is valid
- dismisses when the GM supplies a path that passes validation and clicks Continue, unblocking *application shell* startup
- **Invariant:** the Continue button remains disabled until the entered path resolves to a directory containing the expected COH game files; a missing or unreadable directory shows an inline error label

### character crowd main workspace

- is the primary pre-session surface opened by the *application shell* after module load
- hosts the *crowd manager* — the *crowd tree* panel on the left and the tabbed content body on the right (Identities tab active in Increment 1, Abilities and Movements tabs inactive/greyed)
- **Decision:** `character crowd main workspace` is the UI workspace name; it is the same concept as the *crowd manager* in session context — two names for the same surface

---

## COH Game Directory

The *COH game directory* is the file-system path to the City of Heroes (Titan Icon) installation that the Hero Virtual Tabletop must locate before performing any file operations. In Increment 1 it is needed only to determine the *COH data directory* where the *crowd repository* JSON file is stored. Validation checks that the path exists and is a readable directory with the expected COH structure.

### COH game directory

- is stored in application configuration (user settings) and read on startup
- is validated by checking that the path refers to an existing, readable directory containing the expected COH installation artifacts
- triggers the *game directory prompt* when absent from configuration or when the stored path fails validation
- provides the root for the *COH data directory* — the `<coh_dir>/data/` subdirectory where the *crowd repository* file is stored
- **Invariant:** no *crowd repository* file operations (load, save, backup) can proceed until the *COH game directory* is confirmed valid; the path is fixed for the session once accepted

### COH data directory

- is the `<coh_dir>/data/` subdirectory derived from the *COH game directory*
- is the storage location for the *crowd repository* JSON file and its daily backup
- **Decision:** `COH data directory` is a property of *COH game directory*, not a separate concept; it has no independent behavior

---

## Character

A *character* is the foundational named data entity the GM creates and organizes within *crowds*. In Increment 1, a *character* is a pure data object: a name, a membership in one or more *crowds*, and the data structures for its future *identities*, *abilities*, and *movements* (which are empty *option groups* at this increment). Every crowd membership operation — create, rename, clone, link, cut, drag-drop — acts on a *character* as its subject. No game-side execution (spawn, move, animate) occurs in Increment 1.

### character

- is created in a *crowd* by the GM supplying a name; the name must be unique within the target *crowd*
- is renamed by the GM via inline edit in the *crowd tree*; the rename propagates to all *crowds* the *character* belongs to
- is deleted from a *crowd* by the GM; if the *character* is a *linked member* in other *crowds*, those links are also removed
- holds three empty *option groups* (Identities, Abilities, Movements) at creation — placeholders for future increments
- **Invariant:** a *character's* name must be unique within every *crowd* it belongs to at any moment

### option group

- groups *character options* — identities, *animated abilities*, or *character movements* — under a named key, maintaining insertion order and name-keyed lookup
- is always present on a *character* for each of the three canonical types (Identities, Abilities, Movements), created lazily on first access but never absent
- is empty for all three groups in Increment 1 — no identities, abilities, or movements are authored in this increment
- **Invariant:** exactly three canonical *option groups* must always exist on every *character*; the three names are fixed

---

## Crowd

A *crowd* is a named, hierarchical container of *crowd members* — each of whom is either a *character* or a nested *crowd* — that the GM organizes for scene staging and group management. It is the organizing unit of the *crowd repository* and the persistence boundary for the entire *character* collection. A *crowd* can contain other *crowds* as nested children, enabling arbitrarily deep grouping hierarchies. The *crowd manager* presents the crowd hierarchy as the *crowd tree* — the primary navigation surface of the *character crowd main workspace*.

### crowd

- is created by the GM supplying a name; the name must be unique among siblings in the same parent *crowd* or at the repository root level
- is renamed by the GM via inline edit in the *crowd tree*; all references to the *crowd* by name are updated
- is deleted by the GM; deletion removes the *crowd* and all its *crowd members* from the repository, with a confirmation prompt before execution
- is nested inside another *crowd* by drag-drop in the *crowd tree*, making it a child of the target *crowd*
- contains an ordered, name-keyed collection of *crowd members*, which may be *characters* or nested *crowds*; the collection notifies observers on every structural change
- filters its visible *crowd members* by name substring match, collapsing non-matching entries and expanding those that match; the filter is applied live as the GM types
- is browsed through one of four *browse modes*: By Concept, By Gangs/Crews/Squads, By COH Structure, or All Characters
- **Invariant:** *crowd member* names must be unique within a *crowd* — the collection is keyed by name and rejects duplicates on add or rename

### crowd member

- is a *character* (or nested *crowd*) that participates in a *crowd* with a back-reference to its containing *crowd*
- can be cloned into an independent copy that shares no state with the original (see *clone*)
- can be linked across multiple *crowds* as a shared reference so that a single *character* data object appears in more than one *crowd* (see *linked member*)
- can be cut from its containing *crowd* and held on the *clipboard* for paste into another *crowd*
- can be drag-dropped from one *crowd* to another *crowd* in the *crowd tree*
- **Invariant:** a *crowd member's* name must be unique within every *crowd* it belongs to at any moment

### nested crowd

- is a *crowd* that appears as a child node within another *crowd* in the *crowd tree*
- is treated as a *crowd member* of its parent for membership and display purposes
- retains its own collection of *crowd members* independently of its parent

### crowd tree

- is the panel-region of the *crowd manager* that displays the full *crowd* hierarchy as a tree of expandable nodes
- each crowd node shows: crowd name, member count, expand/collapse toggle; each character node shows: character name, and (in later increments) type and spawned status
- supports all structural operations: create crowd, create character, rename, delete, nest, clone, cut, link, clone-link, flatten-copy, clone memberships, drag-drop
- has a filter bar above the tree (text input and clear button) that applies the *name filter*
- has a browse bar below or beside the filter (category buttons) that switches the active *browse mode*

### linked member

- is a *character* that appears in two or more *crowds* through a shared reference — not a copy, but the same data object
- modifying a *linked member's* name or data in one *crowd* is reflected in all other *crowds* that link to it
- is visually distinguished in the *crowd tree* by a chain/link indicator beside the character node
- is created by the Link operation (from source crowd) or Clone-Link operation (clone + immediately link the copy)
- **Invariant:** all appearances of a *linked member* across *crowds* share the same underlying *character* instance; renaming from any *crowd* renames the character everywhere

### clone

- is an operation that produces a deep-copy *character* in the same *crowd* as the original, with a new unique name (e.g. "Guard (Copy)" or "Guard 2")
- the cloned *character* is independent — modifying it does not affect the original
- **Invariant:** after a clone operation, the new *character* and the original share no state

### linked clone (clone-link)

- is an operation that clones a *character* and immediately links the clone into a specified target *crowd*, creating a *linked member* in the target while leaving the original in its source *crowd*

### clipboard

- holds at most one cut *crowd member* (or *crowd*) at a time, ready for paste into any *crowd*
- is populated when a *crowd member* is cut — the cut immediately removes the *crowd member* from the source *crowd* and holds it on the *clipboard*
- is consumed and cleared when the GM pastes the held item into a target *crowd*
- **Invariant:** cutting a *crowd member* immediately removes it from the source *crowd*; pasting places the held item into the target *crowd* and clears the *clipboard*

### flatten-copy

- is an operation on a *crowd* that replaces its *crowd members* with independently numbered deep-copy *characters* (e.g. "Guard 1", "Guard 2", "Guard 3")
- breaks any shared *linked member* references within the flattened *crowd* — the resulting copies are fully independent
- leaves nested *crowds* in place; only character-level members are numbered and replaced
- **Invariant:** after flatten-copy, no two resulting *characters* share state; modifying one does not affect any other

### clone memberships

- is an operation that copies all *crowd members* from a source *crowd* into a target *crowd* as *linked members*, giving the target crowd the same membership set (as links, not copies)
- the source *crowd* is unchanged; the target *crowd* gains one *linked member* entry per member of the source

### name filter

- is the active text entered in the filter bar above the *crowd tree* that reduces the visible nodes to those whose names contain the filter string (case-insensitive substring match)
- is applied live as the GM types; clearing the filter restores all nodes
- collapses crowd nodes that have no matching descendants; expands crowd nodes that contain at least one match

### browse mode

- is the active view applied to the *crowd tree* that determines which *crowds* are shown and in what grouping
- **By Concept** — shows crowds grouped under concept categories (Animals, Armed Forces, Civilians, Vehicles, Supernatural, etc.) as defined by the crowd's concept tag
- **By Gangs, Crews, and Squads** — shows crowds tagged as gang, crew, or squad type groupings
- **By COH Structure** — shows crowds organized according to City of Heroes faction/group hierarchy
- **All Characters Crowd** — shows the *all characters crowd*: a flat alphabetical list of every *character* in the repository

### all characters crowd

- is a special protected root *crowd* that aggregates every *character* in the *crowd repository* as a flat alphabetically sorted list
- is automatically maintained — any *character* added to any *crowd* also appears here
- cannot be deleted, renamed, or re-ordered; attempts to delete it are blocked
- **Invariant:** the *all characters crowd* is always present and always current; it reflects the full character population at all times

### crowd manager

- is the pre-session library surface — the main application screen that opens at startup and shows the *crowd* hierarchy in the *crowd tree*
- presents the full set of crowd and character management operations available in Increment 1
- triggers loading of the *crowd repository* on open and saving on explicit save action
- **Invariant:** the *crowd manager* is always the first surface the GM sees after startup; the desktop (in later increments) is only active once a game session begins

---

## Crowd Repository

The *crowd repository* is the persistent store for the entire *crowd* hierarchy. It serializes the full graph of *crowds*, nested *crowds*, and *characters* (with their empty *option groups*) to a JSON file in the *COH data directory*, and deserializes it on application load. On each load it creates a daily backup of the previous valid file before overwriting. When no file exists on first run, the repository seeds itself from an embedded default crowd collection. The *crowd repository* is the single source of truth for all *crowd* and *character* data between sessions.

### crowd repository

- deserializes the full *crowd* hierarchy from a JSON file in the *COH data directory* on application open, restoring all *crowd members* with their *option groups*
- serializes the full *crowd* hierarchy back to JSON on GM-triggered save, preserving the complete *character* and *crowd* state tree
- creates a dated backup copy of the current valid JSON file before overwriting on save (daily backup — one backup per calendar day)
- seeds the collection from an embedded resource of default *crowd members* (the default crowd collection) when no JSON file is found on first run
- backs up the existing file before loading it at startup, protecting against corruption from a mid-session crash
- stores the repository file in the *COH data directory* (`<coh_dir>/data/`)
- **Invariant:** exactly one *crowd repository* JSON file is the active source of truth; the backup copies are read-only and used only for disaster recovery

### crowd collection

- is the ordered set of root-level *crowds* held by the *crowd repository*, together with the special *all characters crowd*
- is the unit of serialization — the full collection is written to and read from JSON atomically
- **Decision:** `crowd collection` is a property on *crowd repository*, not an independent concept; its behavior (serialize, deserialize, seed) is owned by the *crowd repository*

### JSON serialization

- is the process by which the *crowd repository* encodes the full *crowd* hierarchy to UTF-8 JSON text, preserving crowd names, nesting, crowd member names, *linked member* identity, and empty *option groups*
- preserves *linked member* identity across serialization — a *character* referenced from multiple *crowds* is written once and cross-referenced, not duplicated
- **Decision:** `JSON serialization` is a behavior of *crowd repository*, not a separate concept

### daily backup

- is a date-stamped copy of the *crowd repository* JSON file created once per calendar day before the repository file is overwritten by a save
- is stored alongside the active file in the *COH data directory* (e.g. `crowds_2026-05-17.json`)
- is also created on application load before the file is read, protecting against read-time corruption
- **Invariant:** at most one daily backup file per calendar day; a second save on the same day does not create a second backup — the first backup for that day is preserved

### default crowd collection

- is an embedded resource in the application assembly containing a curated set of starter *crowds* and *characters* (e.g. Animals, Armed Forces, Civilians, Vehicles)
- is deserialized into the *crowd repository* on first run when no JSON file exists in the *COH data directory*
- provides the GM with a useful starting collection rather than an empty workspace on first launch

---

## Decisions made

- `application shell` is a concept at Increment 1: it has distinct observable behavior (validate path → prompt if invalid → load module → open workspace) that is fully testable without a live game
- `game directory prompt` is a concept: distinct structure (path input, browse, feedback label, Continue button) with an explicit enabled/disabled state rule tied to validation
- `COH data directory` is a *property* of *COH game directory*, not a separate concept — it is a derived path with no independent behavior
- `option group` is included because it is created (empty) on every new *character* and must be present in serialized JSON; its invariant (three canonical groups always present) is testable in Increment 1
- `linked member` is a distinct concept (not just a property) because it has observable behavior: rename propagation, link indicator in the UI, shared identity across crowds
- `clone`, `clone-link`, `flatten-copy`, and `clone memberships` are operations (verbs), not concepts; they are described under their host KAs
- `browse mode` is a property of the *crowd tree* view state, not a separate concept; the four modes are enumerated here for precision
- `all characters crowd` is a concept (not just a special crowd) because it has a distinct invariant (always present, auto-maintained, undeletable) and distinct behavior (aggregate all characters)
- `daily backup` is a concept: distinct identity (date-stamped file), distinct behavior (created once per day on load and save), and a clear invariant (at most one per day)
- `default crowd collection` is a concept: distinct identity (embedded resource), distinct behavior (seed on first run), and a clear trigger (no JSON file found)
- the *crowd repository* term "back up on load" means the backup is created *before* reading the file, so a corrupt-on-read scenario is still protected — distinct from the save-time backup

## References

**Ref — thin-slicing.md (Increment 1: Character and Crowd Library)**
Source: docs/stories/thin-slicing.md
Locator: lines 15–54
Extract: whole

**Ref — story-map.md (Manage Characters and Crowds)**
Source: docs/stories/story-map.md
Locator: lines 31–59
Extract: partial

**Ref — ubiquitous-language.md (Character, Crowd, Crowd Repository)**
Source: docs/domain/ubiquitous-language.md
Extract: KA sections — Character, Crowd, Game Bridge (COH game directory)

**Ref — initial-ia.md (game directory prompt, crowd manager — identities)**
Source: docs/ux/initial-ia.md
Locator: lines 25–105
Extract: partial
