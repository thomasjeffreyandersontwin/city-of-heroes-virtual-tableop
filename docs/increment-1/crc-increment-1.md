---
state: crc
increment: 1
scope: Character and Crowd Library
date: 2026-05-18
---

# CRC — Increment 1: Character and Crowd Library

---

## **Application Shell**

The Application Shell subsystem initializes the Hero Virtual Tabletop process: validating the COH Game Directory, loading the Prism Module, and opening the initial workspace surface.

### **Application Shell**
require valid game directory before startup | COH Game Directory
                                    |   invariant: Prism Module is never initialized until COH Game Directory passes validation
require GM-supplied directory when absent or invalid | Game Directory Prompt
initialize Prism module after directory confirmed | Prism Module
present crowd tree as initial surface | Crowd Tree

### **Prism Module**
register crowd tree views and services | Crowd Tree
own crowd tree navigation region    | Crowd Tree

### **Game Directory Prompt**
accepted directory path             | COH Game Directory
validate directory path in real time | COH Game Directory
                                    |   invariant: startup may not proceed until the accepted path resolves to a valid COH installation directory
confirm valid path to allow startup | Application Shell, COH Game Directory

### references
Source: docs/increment-1/ubiquitous-language-increment-1.md · lines 26–56

### decisions made
- `character crowd main workspace` and `crowd manager` are the same surface per the UL decision; canonical name is `Crowd Tree`; CRC block appears in the Crowd KA; Application Shell decisions note the alias
- Game Directory Prompt holds the path entry and real-time validation behavior; Application Shell owns the gate (require directory before startup) and the outcome (proceed or re-prompt)
- Prism Module's CRC responsibilities cover the domain boundary it establishes (registered views and navigation region); internal IoC container mechanics are below CRC fidelity

---

## **COH Game Directory**

The COH Game Directory is the configured file-system path to the City of Heroes installation. It provides the root for the COH data directory where the Crowd Repository file is stored.

### **COH Game Directory**
stored configuration path           | (file path from user settings)
COH data directory root             | (derived path: stored path / data /)
                                    |   invariant: crowd repository file operations cannot proceed until this path is confirmed as a valid COH installation directory; path is fixed for the session once accepted

### references
Source: docs/increment-1/ubiquitous-language-increment-1.md · lines 59–76

### decisions made
- `COH data directory` is a derived path property on `COH Game Directory`; no independent CRC block — its storage-location role is captured as `COH data directory root` and its constraint is folded into the invariant on `stored configuration path`
- Validation of the path is triggered by Application Shell; COH Game Directory owns only the stored path value and the derived data directory root

---

## **Character**

A Character is the named data entity the GM creates and organizes in crowds. In Increment 1 a Character holds only its name and three empty Option Groups.

### **Character**
character name                      | (text)
                                    |   invariant: name must be unique within every crowd the character belongs to at any moment
option groups                       | Option Group
rename                              | (text)
                                    |   invariant: rename propagates to all crowds the character belongs to; name must remain unique in every crowd it belongs to at the moment of rename
delete from crowd                   | (containing Crowd, Linked Member, All Characters Crowd)
                                    |   invariant: deleting from one crowd removes only that crowd's reference if the character is a linked member elsewhere; deleting from the last crowd removes the character from the repository entirely
clone                               | Crowd, Crowd Member
                                    |   invariant: the clone and the original share no state
linked clone                        | Crowd, Crowd Member, Linked Member
cut to clipboard                    | Clipboard, Crowd Member

### **Option Group**
canonical group name                | (Identities, Abilities, or Movements)
ordered name-keyed options          | (empty in Increment 1)
                                    |   invariant: exactly three canonical option groups must always exist on every character; each is created on first access but never absent

### references
Source: docs/increment-1/ubiquitous-language-increment-1.md · lines 79–97

### decisions made
- Character implements `Crowd Node`; rename and delete live on Character; rename propagation to all crowds is automatic because Crowd Member wraps the same Character instance — updating the name on the instance is reflected everywhere it is referenced
- clone, linked clone, and cut-to-clipboard are Character-specific operations; they originate from a specific Character instance
- Option Group is modeled as a class because its three-group invariant (always present, always exactly three, fixed canonical names) is independently testable and the named groups are the anchor for Identities, Abilities, and Movements in future increments

---

## **Crowd**

The Crowd KA covers the crowd domain model, membership patterns, the crowd tree display, name filtering, and all crowd management operations.

### **Crowd**
crowd name                          | (text)
                                    |   invariant: name must be unique among siblings in the same parent crowd or at the repository root level
crowd members                       | Crowd Member
                                    |   invariant: member names must be unique within the crowd; the collection rejects duplicates on add or rename
source file (top-level crowds only) | Crowd File
                                    |   invariant: every loaded top-level crowd has exactly one source file; nested crowds inherit the source file of their top-level ancestor
dirty flag                          | (boolean)
                                    |   invariant: set on any structural change (rename, add/remove member, add/remove nested crowd, saved-position change); cleared only when the crowd is written to its source file
rename                              | (text)
delete                              | (parent Crowd or Crowd Tree)
add member                          | Crowd Member
remove member                       | Crowd Member
iterate members                     | Crowd Member
mark dirty on structural change     | (self)
notify observers of structural change | (observers)
apply name filter                   | Name Filter, Crowd Member
paste member from clipboard         | Clipboard, Crowd Member
flatten to numbered independent copies | Character, Crowd Member
                                    |   invariant: after flatten-copy, no two resulting characters share any state; nested crowds are left in place
link crowd member as linked member  | Crowd Member, Character, Linked Member
copy memberships as linked members to crowd | Crowd Member, Character, Linked Member
concept tag                         | (Animals, Armed Forces, Civilians, Vehicles, Supernatural, or absent)
group type                          | (gang, crew, squad, or absent)
COH faction tag                     | (COH faction name or absent)

### **Crowd Member**
wrapped node                        | Character, Crowd
containing crowd                    | Crowd
expanded                            | (boolean)
remove()                            | (parent Crowd or Crowd Tree)
                                    |   invariant: remove() removes this entry from the parent collection without recursively deleting the wrapped node's descendants from the repository
isDirty()                           | (delegates to wrapped Character or Crowd)

### **Nested Crowd : Crowd**
parent crowd                        | Crowd

### **Linked Member : Crowd Member**
shared character identity           | Crowd, Character
                                    |   invariant: all appearances of a linked member across crowds share the same underlying character instance; renaming from any crowd renames the character everywhere

### **Crowd Tree**
top-level crowd members             | Crowd Member
active browse mode                  | (By Concept, By Gangs/Crews/Squads, By COH Structure, All Characters)
active name filter                  | Name Filter
                                    |   invariant: filter applied live; clearing restores all nodes; crowds with no matching descendants collapse; those with at least one match expand
linked member indicator             | Linked Member
active character tab                | (Identities active; Abilities and Movements inactive in Increment 1)
                                    |   invariant: Crowd Tree is always the first surface the GM sees after startup
load active crowd files on open     | Active Crowd List, Crowd File, Crowd
browse and activate crowd files     | Active Crowd List, Crowd File, Crowd
                                    |   invariant: each selected file is loaded one at a time; a failed file is reported but does not abort the remaining selections
save dirty crowds                   | Crowd, Crowd File, Daily Backup
                                    |   invariant: only crowds with dirty flag set are written; each to its own source file; clean crowds are not touched
                                    |   invariant: a dirty crowd with no source file is automatically routed to save crowd to new file; cancellation leaves it unsaved
save crowd to new file              | Crowd, Crowd File, Active Crowd List
                                    |   invariant: on success the chosen path becomes the crowd's source file, appended to active crowd list, dirty flag cleared
                                    |   invariant: only top-level crowds may be saved to new file; nested crowd selection is rejected
add crowd to collection             | Crowd, Crowd Member
add character to crowd              | Character, Crowd, All Characters Crowd
nest crowd inside crowd             | Crowd, Nested Crowd, Crowd Member
                                    |   invariant: a crowd cannot be nested inside itself or inside one of its own descendants; target parent name uniqueness enforced before nest accepted
move crowd member to crowd          | Crowd, Crowd Member
                                    |   invariant: name uniqueness in target crowd is enforced; name conflict is reported and move is rejected

### **Name Filter**
active filter text                  | (case-insensitive substring)
filter match for crowd member       | Crowd Member, Crowd

### **All Characters Crowd : Crowd**
aggregated character population     | Character
                                    |   invariant: always present and always current; every character added to any crowd is automatically reflected here; cannot be deleted, renamed, or re-ordered

### **Clipboard**
held crowd member                   | Crowd Member
                                    |   invariant: at most one item held at any time; cutting immediately removes the crowd member from the source crowd; pasting places the held item into the target crowd and clears the clipboard

### references
Source: docs/increment-1/ubiquitous-language-increment-1.md · lines 100–201

### decisions made
- `character crowd main workspace` resolves to `Crowd Tree`; Crowd Tree is the combined display and management surface; Application Shell decisions note the alias
- `Character` and `Crowd` are substitutable as crowd tree members — both have rename, delete, and isDirty; no separate interface class is needed in the CRC; the substitutability is an implementation concern captured in the object model
- `Crowd Member` is a decorator that wraps a `Character` or `Crowd` and adds expanded state, remove-from-parent, and isDirty delegation; `wrapped node` replaces the old "participating entity" field
- `Crowd Tree` absorbs `Crowd Manager`; file-level and structural operations (load, save, browse-activate, nest, move) live on the tree; per-node operations (rename, delete, clone, cut) live on `Character` or `Crowd`
- `browse mode` is a property on `Crowd Tree` (four enumerated modes); no independent class
- operations distributed: clone/linked-clone/cut-to-clipboard live on `Character`; paste/flatten/link-member/copy-memberships live on `Crowd`; nest/move live on `Crowd Tree`
- `Nested Crowd : Crowd` carries only the parent-crowd-participation delta; all management behavior is inherited from Crowd
- `All Characters Crowd : Crowd` carries only the aggregation and protection delta; Crowd Tree enforces the protection invariant
- `Linked Member : Crowd Member` carries only the shared-identity delta; decorator behaviors (expanded, remove, isDirty) are inherited from Crowd Member
- `concept tag`, `group type`, and `COH faction tag` are classification properties on `Crowd` that drive the three browse modes in `Crowd Tree`; absent when unset — crowds without a tag appear under "Uncategorized" / "Untagged"
- `nest crowd inside crowd` and `move crowd member to crowd` both live on `Crowd Tree` because both involve tree structure; they are distinct: nest changes a crowd's parent, move changes a member's crowd

---

## **Crowd Repository**

The Crowd Repository KA covers the persistence layer: the in-memory aggregate of every loaded crowd, the active crowd list that drives startup load, the per-file JSON crowd files, daily backups, and the on-demand default seed.

### **Crowd Repository**
in-memory aggregate of every loaded top-level crowd | Crowd, Crowd Member, Character, Option Group
                                    |   invariant: every loaded top-level crowd has exactly one source file; the same crowd file is never loaded twice
load active crowd files on startup  | Active Crowd List, Crowd File, Crowd, Daily Backup
                                    |   invariant: no crowd file is loaded on startup unless its path appears in the active crowd list
add crowd file (GM activation, path not yet in list) | Active Crowd List, Crowd File, Crowd
clone active crowd file on re-activation              | Active Crowd List, Crowd File, Crowd
                                    |   invariant: clone path is `<name> (N).json` in the same directory where N is the lowest integer ≥ 2 not currently used by a `<name> (k).json` file (gaps are filled first); every top-level crowd name in the clone is suffixed with the same ` (N)`; nested crowd names are unchanged; the original crowd file on disk is not modified
remove crowd file (GM deactivation) | Active Crowd List, Crowd File, Crowd
save dirty crowds to source files   | Crowd, Crowd File, Daily Backup
                                    |   invariant: a non-dirty crowd's source file is never written; a failed write leaves the crowd dirty and other writes proceed
                                    |   invariant: a dirty crowd with no source file is routed to Save Crowd to New File — the dialog opens automatically; cancellation leaves that crowd unsaved with no source file
save crowd to new file              | Crowd, Crowd File, Active Crowd List
                                    |   invariant: on success the new path becomes the crowd's source file and is appended to the active crowd list
seed from default crowd collection on explicit request | Default Crowd Collection, Crowd, Character, Option Group
                                    |   invariant: the default crowd collection is never loaded automatically on startup — only when the GM explicitly requests it

### **Crowd File**
absolute file path                  | (path string)
                                    |   invariant: identifies the crowd file uniquely; two crowd files share no path
top-level crowds it contains        | Crowd
serialize contained crowds          | Crowd, Daily Backup
deserialize contained crowds        | Crowd
                                    |   invariant: deserialization round-trips with serialization — every contained crowd reloads into an equivalent in-memory crowd tree

### **Active Crowd List**
persisted list of crowd file paths  | Crowd File, COH Game Directory
                                    |   invariant: contains no duplicate paths; re-activating an already-active crowd file clones it to `<name> (N).json` (lowest integer ≥ 2 not in use, gaps filled first) and the clone's path is appended in place of the duplicate
read at application start           | Crowd Repository
append on activate                  | Crowd File
remove on deactivate                | Crowd File
persist immediately on change       | (self)
                                    |   invariant: a session crash never loses an activation made earlier in the same session; the persisted list is up to date after every successful add or remove

### **Daily Backup**
date-stamped backup file            | Crowd File
                                    |   invariant: at most one backup file per crowd file per calendar day; a second save on the same day to the same crowd file does not create a second backup
create backup before overwriting    | Crowd File
create backup before loading at startup | Crowd File

### **Default Crowd Collection**
embedded starter crowds and characters | Crowd, Character
seed crowd repository on explicit GM request | Crowd Repository, Crowd, Character, Option Group
                                    |   invariant: never seeds the repository automatically — only when the GM invokes a "Load Defaults" action

### references
Source: docs/increment-1/ubiquitous-language-increment-1.md · lines 205–242

### decisions made
- `Crowd Repository` is now an in-memory aggregator, not a single-file store; it owns the load/save lifecycle but no longer holds a `repository file location` of its own
- `Crowd File` is its own class because it has distinct identity (its absolute path), distinct lifecycle (created by Save Crowd to New File, updated by Save Dirty Crowds to Source Files, deactivated by GM removal), and a serialization contract independent of any other file
- `Active Crowd List` is its own class because it has its own persisted file in the COH data directory, its own membership lifecycle (append on activate, remove on deactivate), and a startup behavior that determines what the `Crowd Repository` loads
- `source file` is a property of top-level `Crowd` — a reference to its `Crowd File`; not a separate class
- `dirty flag` is a property of `Crowd` — a boolean indicating unsaved in-memory state; not a separate class
- `JSON serialization` is a behavior of `Crowd File`; it preserves linked member identity (written once, cross-referenced, not duplicated) within a single file
- `Daily Backup` remains its own class — now per `Crowd File` rather than per `Crowd Repository`; the calendar-day uniqueness invariant is per file
- `Default Crowd Collection` remains its own class but is now loaded only on explicit GM request (never automatically on startup)
