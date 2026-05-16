---
state: ubiquitous-language
increment: 1
---

# Hero Virtual Tabletop — Increment 1: Character and Crowd Library

**Scope:** Terms that an Increment 1 story directly creates, reads, updates, deletes, or persists. No game connection is required in this increment; concepts that only become meaningful at spawn time (position, identity rendering, animation, movement, roster) are excluded.

**Terms**:
- **Character**
  - **character** — the foundational data unit the GM creates, names, and organizes; all runtime capabilities are deferred to later increments
  - **option group** — a named, keyed, ordered collection of *character options* on a *character*; the three canonical groups (Identities, Abilities, Movements) are always structurally present even when empty
- **Crowd**
  - **crowd** — a named, hierarchical container of *crowd members* the GM organizes for scene staging and group management
  - **crowd member** — a *character* that participates in a *crowd* with membership behavior (roster reference, clone, link)
  - **crowd repository** — the persistent JSON store for the entire *crowd* hierarchy, with daily backup
  - **crowd manager** — the pre-session library surface where the GM creates, organizes, browses, and persists *crowds* and *characters*
  - **all characters crowd** — a special protected root *crowd* that aggregates every *character* in the *crowd repository* as a flat alphabetical list; cannot be deleted
  - **clipboard** — the transient in-memory buffer that holds a cut or copied *crowd member* awaiting paste
  - **flatten-copy** — the operation that replaces a *crowd's* membership with independently numbered deep-copy *characters*, breaking shared references within that *crowd*
  - **gang mode** — a boolean flag on *crowd* indicating coordinated gang activation; set during library organization but not activated until Increment 5

> Boundary concepts touched in this increment: **COH game directory** (owned by City of Heroes Platform) — the validated file-system path used to locate the *crowd repository* JSON file in the COH data directory.

---

The Hero Virtual Tabletop lets the *GM* orchestrate live superhero RPG sessions. Before any session begins, the GM uses the *crowd manager* to build and organize the *character* library. *Characters* are grouped into *crowds* and the full hierarchy is loaded from and saved to the *crowd repository*. The *GM* can cut, copy, paste, clone, link, flatten, and filter *crowd members* to build the scene library. The *all characters crowd* always reflects the complete character population. No game engine connection is needed in this increment — the entire surface is the *crowd manager*.

---

# Core Domain

## Character

A *character* is the foundational data unit of the system — the entity the GM creates, names, and organizes in the *crowd manager*. In Increment 1, a *character* is a named, persistable node in the *crowd* hierarchy. Its runtime capabilities (spawn, move, animate, attack) are deferred to later increments.

### character

- holds an ordered *option group* for each capability class — Identities, Abilities, Movements — created on demand and never absent, even when empty in this increment
- is created with a unique name within its containing *crowd*
- can be renamed, cloned into an independent copy, or deleted from a *crowd*
- is serialized as part of the *crowd* hierarchy when the *crowd repository* saves
- **Invariant:** exactly three canonical *option groups* (Identities, Abilities, Movements) must always exist on every *character* — created lazily but never absent

### option group

- groups *character options* — identities, *animated abilities*, or *character movements* — under a named key, maintaining insertion order and name-keyed lookup
- adds, inserts, removes, and replaces *character options* by index or name, notifying observers on every structural change
- enforces uniqueness by name within the group; duplicate names are rejected on add
- **Invariant:** the three canonical *option groups* (Identities, Abilities, Movements) are always present on a *character*, created on first access if not already present

### Decisions made

- `character` is the central domain concept — it has distinct identity (name) and is the subject of every significant behavior in the system; its runtime state (spawned, active, positioned) is deferred to later increments
- `option group` is a concept: it has distinct identity (name), state (ordered keyed collection), behavior (add/remove/find), and carries the invariant that canonical groups must exist

---

## Crowd

A *crowd* is a named, hierarchical container of *crowd members* — each of whom is either a *character* or a nested *crowd* — that the GM organizes for scene staging and group management. It is the organizing unit of the *crowd repository* and the persistence boundary for the entire *character* collection. The *crowd manager* is the GM's library workspace for building and maintaining this hierarchy.

### crowd

- contains an ordered, name-keyed collection of *crowd members*, which may be *characters* or nested *crowds*
- adds, removes, clones, and reorders *crowd members*; notifies observers on every structural change
- filters its visible *crowd members* by name regex, collapsing non-matching branches and expanding matching ones
- **Invariant:** *crowd member* names must be unique within a *crowd* — the collection is keyed by name and rejects duplicates

### crowd member

- participates in one or more *crowds* by maintaining a back-reference to its *roster crowd*
- can be cloned into an independent copy, linked across multiple *crowds* as a shared reference, or flattened into a numbered standalone *character*
- **Invariant:** a *crowd member's* name must be unique within any *crowd* it belongs to at any moment

### crowd repository

- deserializes the full *crowd* hierarchy from a JSON file in the COH data directory on session start, restoring all *crowd members* with their *option groups*
- serializes the full *crowd* hierarchy back to JSON on save, preserving the complete *character* and *crowd* state tree
- creates a daily backup copy of the valid JSON file before overwriting, protecting against corruption
- seeds the collection from an embedded resource of default *crowd members* when no file is found on first run
- **Invariant:** exactly one *crowd repository* file is the source of truth; the backup is read-only and used only for disaster recovery

### crowd manager

- is the pre-session library surface — the main application screen that opens at startup and shows the *crowd* hierarchy
- presents the *crowd* tree where the GM creates, renames, deletes, nests, clones, links, filters, and browses *crowds* and *crowd members*
- triggers loading of the *crowd repository* on open and saving on explicit save actions
- **Invariant:** the *crowd manager* is always the first surface the GM sees; the *desktop* (live session overlay) is only active once a game session begins

### all characters crowd

- is a special protected root *crowd* that aggregates every *character* in the *crowd repository* as a flat alphabetically sorted list
- is automatically maintained — any *character* added to any *crowd* also appears here
- cannot be deleted; attempts to delete it are blocked
- **Invariant:** the *all characters crowd* is always present and always current; it reflects the full character population of the *crowd repository* at all times

### clipboard

- holds at most one cut or copied *crowd member* (or *crowd*) at a time, ready for paste into any *crowd*
- is populated when a *crowd member* is cut (removes it from the source *crowd*) or copied (leaves the source intact)
- is consumed and cleared when the GM pastes into a target *crowd*
- **Invariant:** cutting a *crowd member* immediately removes it from the source *crowd*; pasting places the held item into the target *crowd* and clears the *clipboard*

### flatten-copy

- is an operation on *crowd* — replaces its membership with independently numbered deep-copy *characters* (e.g. "Guard 1", "Guard 2")
- breaks any shared *crowd member* references within the flattened *crowd* — the resulting copies are fully independent
- leaves nested *crowds* in place; only character-level members are numbered and replaced
- **Invariant:** after flatten-copy, no two resulting *characters* share state; modifying one does not affect any other

### gang mode

- is a property of *crowd* — a boolean flag indicating whether the *crowd* is operating as a coordinated gang with a designated *gang leader*
- is set during library organization in Increment 1; gang coordination and activation behavior belongs to Increment 5

### Decisions made

- `crowd` is a concept: distinct identity (name), rich state (member collection, gang mode), and behaviors that operate on the collection as a whole
- `crowd member` is a concept: it extends *character* with explicit membership behavior (roster crowd reference, clone/link, filter participation) that plain *characters* do not have
- `saved position` is **excluded** from this increment — position is only meaningful for spawned characters (Increment 4–5)
- `crowd collection` is a *property* — the typed observable collection on *crowd*; no independent concept
- `crowd repository` is a concept: distinct identity (file path), distinct behavior (serialize, deserialize, backup, seed), and a clear persistence lifecycle
- `gang mode` is a property: a boolean slot on *crowd*; the coordination behavior belongs to *crowd* and *roster* concepts and is deferred to Increment 5
- `gang leader` is a property of *character* — a boolean flag on a *crowd member*; role designation only, not an independent concept

---

# Boundary Domain

## COH Game Directory

Owned by: City of Heroes Platform

- the file-system path to the City of Heroes installation, validated at startup
- provides the root path for the COH data directory where the *crowd repository* JSON file is stored and read
- is stored in application configuration and prompted from the GM when absent or invalid
- **Invariant:** the *crowd repository* cannot be loaded or saved until the *COH game directory* is confirmed valid

### Decisions made

- `COH game directory` is included in Increment 1 solely as the storage root for the *crowd repository* — no game engine communication, DLL loading, or keybind writing occurs in this increment; those behaviors belong to the full *Game Bridge* concept in Increment 2+
