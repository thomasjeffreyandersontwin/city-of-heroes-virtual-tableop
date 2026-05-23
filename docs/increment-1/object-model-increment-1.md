---
state: domain-model
increment: 1
scope: Character and Crowd Library
date: 2026-05-18
---

# Object Model — Increment 1: Character and Crowd Library

---

## **Application Shell**

The Application Shell subsystem initialises the Hero VTT process: it validates the COH Game Directory, controls the Game Directory Prompt dialog, and hands off to the Prism Module once a valid path is confirmed.

### **Application Shell** << Service >>
+ ApplicationShell(config: AppConfiguration, directoryPrompt: GameDirectoryPrompt)
------
+ cohGameDirectory: COHGameDirectory
----
+ start(): void
	Invariant: Prism Module is never initialised until COH Game Directory passes validation
	Interaction:
		storedDirectory: COHGameDirectory = config.readStoredDirectory()
		if storedDirectory is null or not storedDirectory.isValid():
			confirmedPath: Path = directoryPrompt.awaitConfirmedPath()
			confirmedDirectory: COHGameDirectory = new COHGameDirectory(confirmedPath)
			config.storeDirectory(confirmedDirectory)
			this.cohGameDirectory = confirmedDirectory
		else:
			this.cohGameDirectory = storedDirectory
		prismModule: PrismModule = new PrismModule(this.cohGameDirectory)
		prismModule.register()

### **Prism Module** << Service >>
+ PrismModule(directory: COHGameDirectory)
------
+ crowdTree: CrowdTree
----
+ register(): void
	Invariant: registers Crowd Tree views, view models, and services with the IoC container; navigates to Crowd Tree as the initial surface
	Interaction:
		activeList: ActiveCrowdList = new ActiveCrowdList(directory.dataDirectory)
		repository: CrowdRepository = new CrowdRepository()
		this.crowdTree = new CrowdTree(repository, activeList)
		// register views and services with Prism IoC container
		this.crowdTree.loadActiveFilesOnOpen()

### **Game Directory Prompt** << Entity >>
+ GameDirectoryPrompt()
------
+ enteredPath: Path
+ isValid: Boolean
+ validationMessage: String
+ canContinue: Boolean
	Invariant: true only when enteredPath resolves to a valid COH installation directory
----
+ setPath(path: Path): void
	Interaction:
		this.enteredPath = path
		this.isValid = COHGameDirectory.validatePath(path)
		this.canContinue = this.isValid
		this.validationMessage = this.isValid ? "" : buildValidationMessage(path)
+ awaitConfirmedPath(): Path
	Invariant: shows the dialog; blocks until the GM supplies a valid path and confirms; returns the confirmed Path
- buildValidationMessage(path: Path): String
	Invariant: returns a human-readable explanation of why path is not a valid COH installation directory

### references
**Ref — Application Shell KA**
Source: docs/increment-1/crc-increment-1.md
Locator: lines 12–40

```source
Application Shell
require valid game directory before startup | COH Game Directory
require GM-supplied directory when absent or invalid | Game Directory Prompt
initialize Prism module after directory confirmed | Prism Module
present crowd tree as initial surface | Crowd Tree
```

### decisions made
- Application Shell is a `<< Service >>` — it orchestrates startup; the confirmed COHGameDirectory is the output, not persistent internal state
- Prism Module is a `<< Service >>` — it registers domain surfaces and retains the initialised CrowdTree reference; its IoC-container mechanics are below domain fidelity
- Game Directory Prompt is an `<< Entity >>` — it carries live validation state (`enteredPath`, `isValid`, `canContinue`) over its dialog lifetime
- `AppConfiguration` is a boundary type (wraps application settings persistence); its detailed structure is deferred to the implementation phase
- `COHGameDirectory.validatePath()` is a static factory guard invoked by both `GameDirectoryPrompt.setPath` and `ApplicationShell.start`; neither class hardcodes the validation logic itself
- `ActiveCrowdList` is constructed with `directory.dataDirectory` as its storage root; this path is the only dependency it needs from the COH Game Directory at initialisation time

---

## **COH Game Directory**

### **COH Game Directory** << ValueObject >>
+ COHGameDirectory(confirmedPath: Path)
------
+ confirmedPath: Path
	Invariant: fixed at construction; points to an existing COH installation directory
+ dataDirectory: Path
	Invariant: derived as confirmedPath / "data" /; computed once at construction and immutable thereafter
----
+ isValid(): Boolean
	Invariant: returns true when confirmedPath references a readable directory containing expected COH installation artifacts
+ static validatePath(path: Path): Boolean
	Invariant: stateless check; returns true if path is a valid COH installation directory; does not construct a COHGameDirectory instance

### references
**Ref — COH Game Directory KA**
Source: docs/increment-1/crc-increment-1.md
Locator: lines 43–58

```source
COH Game Directory
stored configuration path | (file path from user settings)
COH data directory root   | (derived path: stored path / data /)
```

### decisions made
- COH Game Directory is a `<< ValueObject >>` — once confirmed, the path is immutable; two instances with the same `confirmedPath` are interchangeable and equal
- `dataDirectory` is derived internally at construction; the caller receives it as a read-only property
- `validatePath` is a static factory guard so `ApplicationShell` and `GameDirectoryPrompt` can call it without constructing a full instance first
- Validation of the path is triggered by callers; COH Game Directory owns only the stored path value and the derived data directory root

---

## **Character**

A Character is the named data entity the GM creates and organises in crowds. In Increment 1 a Character holds only its name and three empty Option Groups.

### **Character** << Entity >>
+ Character(name: String, parentCrowd: Crowd)
------
+ name: String
	Invariant: unique within every crowd the character belongs to at any moment
+ parentCrowd: Crowd
	Invariant: set once at construction to the crowd in which this character was originally created; immutable thereafter; used by CrowdMember.isLinked() to derive linked status
+ << composition >> optionGroups: Dictionary<String, OptionGroup>
	Invariant: always exactly three keys — Identities, Abilities, Movements; each OptionGroup created lazily on first access; never absent
+ memberships: List<CrowdMember>
	Invariant: every CrowdMember in this list wraps this character; the list is the authoritative set of crowds this character belongs to
----
+ isDirty(): Boolean
	Invariant: Character does not track its own dirty flag; returns false; dirtiness is tracked by each containing Crowd
+ rename(newName: String): void
	Invariant: new name must be unique within every crowd this character currently belongs to; rename propagates to all memberships atomically — either all containing crowds accept the new name or the rename is rejected
	Interaction:
		guard: for every membership in memberships, membership.containingCrowd.findMember(newName) is null
		oldName: String = this.name
		this.name = newName
		for each membership in memberships:
			membership.containingCrowd.onMemberRenamed(membership, oldName, newName)
		allCharactersCrowd.onCharacterRenamed(oldName, newName)
+ delete(crowd: Crowd): void
	Invariant: removes this character's entry from the specified crowd; if linked in other crowds, only that crowd's reference is removed; removing the last membership removes the character from the repository entirely
	Interaction:
		member: CrowdMember = crowd.findMember(this.name)
		crowd.removeMember(member)
		this.memberships.remove(member)
		if this.memberships is empty:
			allCharactersCrowd.removeCharacter(this)
		crowd.markDirty()
+ clone(targetCrowd: Crowd): Character
	Invariant: result is fully independent of this character; no shared state; result is placed in targetCrowd with a unique name; result appears in All Characters Crowd; targetCrowd becomes the clone's parentCrowd
	Interaction:
		clonedName: String = generateUniqueName(this.name, targetCrowd)
		clonedCharacter: Character = new Character(clonedName, targetCrowd)
		clonedCharacter.optionGroups = deepCopyOptionGroups(this.optionGroups)
		clonedMember: CrowdMember = new CrowdMember(clonedCharacter, targetCrowd)
		targetCrowd.addMember(clonedMember)
		allCharactersCrowd.addCharacter(clonedCharacter)
		return clonedCharacter
+ linkedClone(sourceCrowd: Crowd, targetCrowd: Crowd): Character
	Invariant: independent clone placed in sourceCrowd (parentCrowd = sourceCrowd); clone also added as a CrowdMember in targetCrowd; result and this character share no state; clone's CrowdMember in targetCrowd is "linked" because clone.parentCrowd ≠ targetCrowd
	Interaction:
		clonedCharacter: Character = this.clone(sourceCrowd)
		targetCrowd.linkMember(clonedCharacter)
		return clonedCharacter
+ cutToClipboard(clipboard: Clipboard, crowd: Crowd): void
	Invariant: removes this character from crowd immediately; places the crowd member entry on the clipboard; crowd is marked dirty
	Interaction:
		member: CrowdMember = crowd.findMember(this.name)
		crowd.removeMember(member)
		crowd.markDirty()
		clipboard.hold(member)
- generateUniqueName(baseName: String, crowd: Crowd): String
	Invariant: returns "baseName (Copy)" if available in crowd; else "baseName 2", "baseName 3" etc. using the lowest available integer suffix not already used in crowd
- deepCopyOptionGroups(source: Dictionary<String, OptionGroup>): Dictionary<String, OptionGroup>
	Invariant: result shares no references with source; each OptionGroup is a new instance with the same canonicalName

### **Option Group** << Entity >>
+ OptionGroup(canonicalName: String)
------
+ canonicalName: String
	Invariant: one of: Identities, Abilities, Movements; fixed at construction
+ << composition >> options: List<Option>
	Invariant: empty in Increment 1

### references
**Ref — Character KA**
Source: docs/increment-1/crc-increment-1.md
Locator: lines 61–90

```source
Character
character name    | (text)
parent crowd      | Crowd
option groups     | Option Group
rename            | (text)
delete from crowd | (containing Crowd, All Characters Crowd)
clone             | Crowd, Crowd Member
linked clone      | Crowd, Crowd Member
cut to clipboard  | Clipboard, Crowd Member
```

### decisions made
- Character is an `<< Entity >>` — identity matters; two characters with the same name in different crowds are distinct domain objects with independent lifecycles
- Character implements `CrowdNode` (defined in the Crowd KA): supplies `name`, `isDirty()`, and `rename()`; the interface enables `CrowdMember` to treat Character and Crowd uniformly for tree display and dirty-indicator propagation
- `parentCrowd` is set once at construction to the crowd in which the character was originally created; it is the anchor for `CrowdMember.isLinked()` — any CrowdMember wrapping this Character in a different crowd is "linked"; no `LinkedMember` subclass is needed
- `memberships: List<CrowdMember>` is an association (not composition); Character does not own CrowdMember lifecycle; the list exists solely as the reverse-navigation needed by `rename` to propagate across all crowds and by `delete` to detect last-membership removal
- `allCharactersCrowd` is accessed via CrowdRepository in the implementation; shown as a direct collaborator in interaction steps to reflect the CRC without specifying the property access path
- `isDirty()` always returns false on Character; the containing Crowd carries the dirty flag; implementing `CrowdNode` still requires the method to enable uniform delegation in `CrowdMember.isDirty()`
- Option Group is an `<< Entity >>` — its `canonicalName` is its identity within a character's `optionGroups` collection; future increments will add options to it; modeled as a full Entity now to avoid structural breakage

---

## **Crowd**

The Crowd KA covers the crowd domain model, membership patterns, the Crowd Tree display surface, name filtering, and all management operations. **CrowdNode** is an interface introduced here to formalise the substitutability between `Character` (Character KA) and `Crowd` that the CRC deferred to the object model phase.

### **Crowd** << Entity >>
+ Crowd(name: String)
------
+ name: String
	Invariant: unique among siblings in the same parent crowd or at the repository root level
+ << composition >> members: List<CrowdMember>
	Invariant: member names must be unique within the crowd; addMember and onMemberRenamed reject duplicates
+ sourceFile: CrowdFile
	Invariant: present on every loaded top-level crowd; absent on in-memory crowds not yet persisted; nested crowds inherit source file from their top-level ancestor; updated only by CrowdTree.saveCrowdToNewFile
+ dirty: Boolean
	Invariant: set on any structural change (rename, add/remove member, add/remove nested crowd); cleared only when the crowd is successfully written to its source file
+ conceptTag: String
+ groupType: String
+ cohFactionTag: String
----
+ isDirty(): Boolean
	Invariant: returns this.dirty; implements CrowdNode to enable uniform dirty-indicator delegation in CrowdMember
+ rename(newName: String): void
	Invariant: new name must be unique among siblings; marks this crowd dirty on success
	Interaction:
		guard: no sibling in the same parent holds newName
		this.name = newName
		this.markDirty()
		this.notifyObservers()
+ delete(parent: Crowd): void
	Invariant: removes this crowd and all its members from the repository; linked members in other crowds that reference members of this crowd are removed; parent is marked dirty
+ addMember(member: CrowdMember): void
	Invariant: member name must be unique within this crowd; containingCrowd is set to this crowd on add
	Interaction:
		guard: findMember(member.name) is null
		this.members.add(member)
		member.containingCrowd = this
		this.markDirty()
+ removeMember(member: CrowdMember): void
	Invariant: removes the member entry only; does not delete the underlying character or crowd from the repository
	Interaction:
		this.members.remove(member)
		this.markDirty()
+ findMember(name: String): CrowdMember
	Invariant: returns the member whose wrapped node has the given name; returns null when absent
+ iterateMembers(): List<CrowdMember>
+ markDirty(): void
+ notifyObservers(): void
+ onMemberRenamed(member: CrowdMember, oldName: String, newName: String): void
	Invariant: updates any internal name-keyed structures so the member is retrievable by its new name; triggers observer notification
+ applyNameFilter(filter: NameFilter): List<CrowdMember>
	Invariant: returns crowd members whose name contains filter.text (case-insensitive); empty filter returns all members
+ pasteFromClipboard(clipboard: Clipboard): void
	Invariant: clipboard must hold an item; places the held crowd member into this crowd; clears the clipboard
	Interaction:
		guard: clipboard.hasItem() is true
		releasedMember: CrowdMember = clipboard.release()
		releasedMember.containingCrowd = this
		this.addMember(releasedMember)
+ flattenToNumberedCopies(): void
	Invariant: replaces character-level members with independently numbered deep-copy characters; nested crowds left in place; no two resulting characters share any state
	Interaction:
		characterMembers: List<CrowdMember> = members.where(m => m.wrappedNode is Character)
		for each characterMember in characterMembers:
			original: Character = characterMember.wrappedNode as Character
			numberedName: String = generateNumberedName(original.name)
			numberedCopy: Character = new Character(numberedName, this)
			numberedCopy.optionGroups = original.deepCopyOptionGroups()
			allCharactersCrowd.addCharacter(numberedCopy)
			this.removeMember(characterMember)
			this.addMember(new CrowdMember(numberedCopy, this))
+ linkMember(character: Character): CrowdMember
	Invariant: adds character as a CrowdMember in this crowd; rejects if character is already present here by name; the resulting member is "linked" because character.parentCrowd ≠ this crowd
	Interaction:
		guard: findMember(character.name) is null
		member: CrowdMember = new CrowdMember(character, this)
		this.addMember(member)
		return member
+ copyMembershipsTo(targetCrowd: Crowd): void
	Invariant: adds each direct character member as a linked member in targetCrowd; skips members already present in targetCrowd by name; source crowd is unchanged
	Interaction:
		for each member in iterateMembers():
			if targetCrowd.findMember(member.name) is null:
				targetCrowd.linkMember(member.wrappedNode as Character)
- generateNumberedName(baseName: String): String
	Invariant: returns "baseName 1", "baseName 2" etc.; uses the lowest integer not already used in this crowd's member names

### **CrowdNode** << (interface) >>
------
+ name: String
----
+ isDirty(): Boolean
+ rename(newName: String): void

### **Crowd Member** << Entity >>
+ CrowdMember(wrappedNode: CrowdNode, containingCrowd: Crowd)
------
+ wrappedNode: CrowdNode
	Invariant: the crowd member's displayed name is wrappedNode.name; wrappedNode is never null
+ containingCrowd: Crowd
+ expanded: Boolean
----
+ name(): String
	Invariant: returns wrappedNode.name
+ isLinked(): Boolean
	Invariant: returns true when wrappedNode is a Character and wrappedNode.parentCrowd ≠ containingCrowd; returns false for Crowd-wrapping members and for Characters whose parentCrowd equals containingCrowd
	Interaction:
		if not (wrappedNode is Character): return false
		return (wrappedNode as Character).parentCrowd != containingCrowd
+ remove(): void
	Invariant: removes this entry from containingCrowd; does not delete the underlying character or crowd from the repository
	Interaction:
		containingCrowd.removeMember(this)
+ isDirty(): Boolean
	Invariant: delegates to wrappedNode.isDirty(); for Character wrapping always false; for Crowd wrapping returns crowd.dirty

### **Nested Crowd : Crowd** << Entity >>
+ NestedCrowd(name: String, parentCrowd: Crowd)
------
+ parentCrowd: Crowd

> **LinkedMember** — retired. Linked status is derived via `CrowdMember.isLinked()`: returns true when `(wrappedNode as Character).parentCrowd ≠ containingCrowd`. The shared-identity invariant (renaming from any crowd renames everywhere) is guaranteed by the fact that all CrowdMembers across all crowds wrap the same Character instance — no subclass is needed to enforce it.

### **Crowd Tree** << Service >>
+ CrowdTree(repository: CrowdRepository, activeList: ActiveCrowdList)
------
+ << aggregation >> topLevelMembers: List<CrowdMember>
	Invariant: wraps only top-level Crowds (not nested); preserves activation order
+ activeNameFilter: NameFilter
+ activeBrowseMode: BrowseMode
+ activeCharacterTab: CharacterTab
	Invariant: Identities tab is active in Increment 1; Abilities and Movements tabs are inactive
----
+ loadActiveFilesOnOpen(): void
	Invariant: reads active list at open; loads each crowd file in list order; skips missing or malformed files with a GM notification; tree is fully populated before the GM can interact
	Interaction:
		filePaths: List<Path> = activeList.read()
		for each filePath in filePaths:
			if not fileExists(filePath):
				notifyGM(new MissingFileWarning(filePath))
				continue
			crowdFile: CrowdFile = new CrowdFile(filePath)
			crowdFile.createBackupBeforeLoad()
			crowds: List<Crowd> = crowdFile.deserializeCrowds()
			if crowds is null:
				notifyGM(new DeserializationError(filePath))
				continue
			for each crowd in crowds:
				topLevelMember: CrowdMember = new CrowdMember(crowd, null)
				topLevelMembers.add(topLevelMember)
				repository.registerCrowd(crowd)
+ browseAndActivate(filePaths: List<Path>): void
	Invariant: processes each selected path in order; clones when path already in active list; reports failures without aborting; active list updated after each successful load
	Interaction:
		for each filePath in filePaths:
			effectivePath: Path = filePath
			if activeList.contains(filePath):
				effectivePath = activeList.computeClonePath(filePath)
				repository.cloneActiveCrowdFile(filePath, effectivePath)
			crowdFile: CrowdFile = new CrowdFile(effectivePath)
			crowds: List<Crowd> = crowdFile.deserializeCrowds()
			if crowds is null:
				notifyGM(new DeserializationError(effectivePath))
				continue
			for each crowd in crowds:
				topLevelMembers.add(new CrowdMember(crowd, null))
				repository.registerCrowd(crowd)
			activeList.append(effectivePath)
+ saveDirtyCrowds(): void
	Invariant: writes only crowds whose dirty flag is set; routes crowds with no source file to saveCrowdToNewFile; clean crowds are not touched; a failed write leaves that crowd dirty and processing continues
	Interaction:
		dirtyCrowds: List<Crowd> = topLevelMembers.where(m => m.isDirty()).map(m => m.wrappedNode as Crowd)
		if dirtyCrowds is empty:
			notifyGM(new NothingToSaveStatus())
			return
		for each crowd in dirtyCrowds:
			if crowd.sourceFile is null:
				savePath: Path = promptSaveCrowdToNewFile(crowd)
				if savePath is null: continue
				this.saveCrowdToNewFile(crowd, savePath)
			else:
				backup: DailyBackup = new DailyBackup(crowd.sourceFile)
				backup.createIfNotTodayExists()
				saveResult: SaveResult = crowd.sourceFile.serializeCrowd(crowd)
				if saveResult.succeeded:
					crowd.dirty = false
				else:
					notifyGM(new SaveFailureError(crowd.sourceFile, saveResult.reason))
+ saveCrowdToNewFile(crowd: Crowd, path: Path): void
	Invariant: writes the crowd to a new file at path; updates crowd's sourceFile; appends path to activeList; clears dirty flag; rejects if crowd is not top-level
	Interaction:
		guard: topLevelMembers.any(m => m.wrappedNode == crowd)
		crowdFile: CrowdFile = new CrowdFile(path)
		saveResult: SaveResult = crowdFile.serializeCrowd(crowd)
		if not saveResult.succeeded:
			notifyGM(new SaveFailureError(path, saveResult.reason))
			return
		crowd.sourceFile = crowdFile
		activeList.append(path)
		crowd.dirty = false
+ addCrowdToCollection(name: String): Crowd
	Invariant: creates a new Crowd at root level in inline-edit mode; marks dirty; adds to topLevelMembers
	Interaction:
		newCrowd: Crowd = new Crowd(name)
		newCrowd.dirty = true
		repository.registerCrowd(newCrowd)
		topLevelMembers.add(new CrowdMember(newCrowd, null))
		return newCrowd
+ addCharacterToCrowd(crowd: Crowd, name: String): Character
	Invariant: creates a new Character in the specified crowd; adds it to All Characters Crowd; marks crowd dirty
	Interaction:
		newCharacter: Character = new Character(name, crowd)
		newMember: CrowdMember = new CrowdMember(newCharacter, crowd)
		crowd.addMember(newMember)
		repository.allCharactersCrowd.addCharacter(newCharacter)
		return newCharacter
+ nestCrowdInsideCrowd(crowd: Crowd, parentCrowd: Crowd): void
	Invariant: crowd cannot be nested inside itself or one of its own descendants; target parent name uniqueness is enforced before the nest is accepted
	Interaction:
		guard: not isAncestorOf(crowd, parentCrowd)
		guard: parentCrowd.findMember(crowd.name) is null
		this.removeFromCurrentParent(crowd)
		nestedCrowd: NestedCrowd = new NestedCrowd(crowd.name, parentCrowd)
		parentCrowd.addMember(new CrowdMember(nestedCrowd, parentCrowd))
		parentCrowd.markDirty()
+ moveCrowdMember(member: CrowdMember, targetCrowd: Crowd): void
	Invariant: name uniqueness in targetCrowd is enforced; name conflict is reported and move is rejected
	Interaction:
		guard: targetCrowd.findMember(member.name) is null
		sourceCrowd: Crowd = member.containingCrowd
		sourceCrowd.removeMember(member)
		member.containingCrowd = targetCrowd
		targetCrowd.addMember(member)
- isAncestorOf(potentialAncestor: Crowd, crowd: Crowd): Boolean
	Invariant: returns true if potentialAncestor is in the parent chain of crowd; used to prevent circular nesting
- removeFromCurrentParent(crowd: Crowd): void
	Invariant: removes crowd from its current parent crowd, or from topLevelMembers if it is a top-level crowd
- notifyGM(notification: Notification): void

### **Name Filter** << ValueObject >>
+ NameFilter(text: String)
------
+ text: String
	Invariant: case-insensitive substring used to match crowd member names; empty text matches all names
----
+ matches(name: String): Boolean
	Invariant: returns true if name contains text (case-insensitive); always returns true when text is empty

### **All Characters Crowd : Crowd** << Entity >>
+ AllCharactersCrowd()
------
	Invariant: always present; cannot be deleted, renamed, or structurally modified by the GM; reflects every character in the repository alphabetically sorted by name
----
+ addCharacter(character: Character): void
	Invariant: adds a CrowdMember wrapping character if not already present; list remains alphabetically sorted after each add
+ removeCharacter(character: Character): void
	Invariant: removes the CrowdMember wrapping character; called when character is deleted from its last containing crowd
+ onCharacterRenamed(oldName: String, newName: String): void
	Invariant: re-sorts the crowd's member list after a character rename; no member is added or removed

### **Clipboard** << Service >>
+ Clipboard()
------
+ heldMember: CrowdMember
	Invariant: at most one item held at any time; null when clipboard is empty
----
+ hold(member: CrowdMember): void
	Invariant: discards any previously held item; holds the new member
+ release(): CrowdMember
	Invariant: returns the held member and clears heldMember; caller places the member into the target crowd
	Interaction:
		releasedMember: CrowdMember = this.heldMember
		this.heldMember = null
		return releasedMember
+ hasItem(): Boolean
	Invariant: returns true when heldMember is not null

### references
**Ref — Crowd KA**
Source: docs/increment-1/crc-increment-1.md
Locator: lines 93–188

```source
Crowd
crowd name     | (text)
crowd members  | Crowd Member
dirty flag     | (boolean)
add member     | Crowd Member
flatten to numbered independent copies | Character, Crowd Member
link character as member           | Crowd Member, Character
copy memberships as members to crowd | Crowd Member, Character
paste member from clipboard | Clipboard, Crowd Member

Crowd Tree
top-level crowd members | Crowd Member
load active crowd files on open | Active Crowd List, Crowd File, Crowd
browse and activate crowd files | Active Crowd List, Crowd File, Crowd
save dirty crowds | Crowd, Crowd File, Daily Backup
save crowd to new file | Crowd, Crowd File, Active Crowd List
add crowd to collection | Crowd, Crowd Member
add character to crowd | Character, Crowd, All Characters Crowd
nest crowd inside crowd | Crowd, Nested Crowd, Crowd Member
move crowd member to crowd | Crowd, Crowd Member
```

### decisions made
- Crowd is an `<< Entity >>` — its name within its parent context is its identity; it has a create-modify-persist-delete lifecycle
- `CrowdNode` is introduced as an interface in the object model (not in the CRC); the CRC explicitly deferred substitutability to this phase; it defines `name`, `isDirty()`, and `rename()` — the three operations `CrowdMember` and `CrowdTree` need to treat Character and Crowd uniformly; `delete` is excluded because Character.delete(crowd: Crowd) and Crowd.delete(parent: Crowd) take different parameter types
- Crowd Member is an `<< Entity >>` — it has meaningful state (`expanded`) and a lifecycle within its containing crowd; `wrappedNode` is an association — CrowdMember does not own the lifecycle of the wrapped Character or Crowd
- Crowd Tree is a `<< Service >>` — it is the primary management surface and orchestrator; `topLevelMembers` is an aggregation (Crowd Tree groups its top-level members but does not own their lifecycle); persistence is delegated to `CrowdFile` and `DailyBackup`
- Name Filter is a `<< ValueObject >>` — two NameFilters with the same text are interchangeable; immutable
- All Characters Crowd is a protected subtype of Crowd; `delete` and `rename` inherited from Crowd are no-ops (protected via override); only `addCharacter`, `removeCharacter`, and `onCharacterRenamed` are valid mutation operations
- Clipboard is a `<< Service >>` — transient, session-scoped, one held item at a time; no persistent state
- `BrowseMode` (ByConceptTag, ByGroupType, ByCOHFaction, AllCharacters) and `CharacterTab` (Identities, Abilities, Movements) are enum-like value types; no separate classes required
- `SaveResult`, `MissingFileWarning`, `DeserializationError`, `SaveFailureError`, `NothingToSaveStatus`, `Notification` are domain notification value types; their detailed structure is deferred to the implementation phase
- `LinkedMember` class is retired; `CrowdMember.isLinked()` is a derived boolean (`(wrappedNode as Character).parentCrowd ≠ containingCrowd`); the shared-identity guarantee is preserved because all CrowdMembers across all crowds wrap the same Character instance
- `Crowd.linkMember` now creates a plain `CrowdMember` (not a `LinkedMember` subtype); the resulting member's `isLinked()` returns true automatically because the Character's parentCrowd differs from this crowd
- `Crowd.copyMembershipsTo` links character-level members only; nested Crowd members are not linked into targetCrowd; this is a character-linking operation
- `allCharactersCrowd` in `Character.clone`, `Character.delete`, and `Crowd.flattenToNumberedCopies` is accessed via `CrowdRepository.allCharactersCrowd` in the implementation; shown as a direct collaborator in interaction steps to reflect CRC collaborator intent without specifying the navigation path

---

## **Crowd Repository**

The Crowd Repository KA covers the persistence layer: the in-memory aggregate of loaded crowds, the active crowd list that drives startup, the per-file JSON crowd files, daily backups, and the on-demand default seed.

### **Crowd Repository** << Repository >>
+ CrowdRepository()
------
+ << aggregation >> loadedCrowds: List<Crowd>
	Invariant: every loaded top-level crowd has exactly one sourceFile; the same crowd file path is never loaded twice
+ allCharactersCrowd: AllCharactersCrowd
----
+ registerCrowd(crowd: Crowd): void
	Invariant: adds crowd to loadedCrowds; rejects if a crowd from the same file path is already registered
+ deregisterCrowd(crowd: Crowd): void
	Invariant: removes crowd from loadedCrowds when the GM deactivates its file
+ cloneActiveCrowdFile(sourcePath: Path, clonedPath: Path): void
	Invariant: clonedPath is "<name> (N).json" in the same directory; N is the lowest integer ≥ 2 not in use; every top-level crowd name in the clone is suffixed with " (N)"; nested crowd names unchanged; original file on disk is not modified
	Interaction:
		cloneSuffix: Integer = computeNextAvailableSuffix(sourcePath)
		clonedPath = computeClonePath(sourcePath, cloneSuffix)
		sourceFile: CrowdFile = new CrowdFile(sourcePath)
		clonedCrowds: List<Crowd> = sourceFile.deserializeCrowds()
		for each crowd in clonedCrowds:
			crowd.name = crowd.name + " (" + cloneSuffix + ")"
		clonedFile: CrowdFile = new CrowdFile(clonedPath)
		clonedFile.serializeCrowds(clonedCrowds)
+ seedFromDefaultCrowdCollection(defaults: DefaultCrowdCollection, crowdTree: CrowdTree): void
	Invariant: only invoked on explicit GM request; never called automatically on startup; populates the crowd tree with default crowds and characters
- computeNextAvailableSuffix(basePath: Path): Integer
	Invariant: returns the lowest integer ≥ 2 where no file "<base> (N).json" exists in the same directory; fills gaps from previously deleted clones
- computeClonePath(basePath: Path, suffix: Integer): Path

### **Crowd File** << Entity >>
+ CrowdFile(absolutePath: Path)
------
+ absolutePath: Path
	Invariant: uniquely identifies the crowd file; two crowd files share no path
+ << aggregation >> containedCrowds: List<Crowd>
----
+ deserializeCrowds(): List<Crowd>
	Invariant: reconstructs the full crowd hierarchy in member order; linked members restored as single in-memory Character instances shared across crowds; duplicate member names trigger a warning with first-occurrence winning; All Characters Crowd is not written to or read from the file
	Interaction:
		rawJson: String = readFile(absolutePath)
		rawCrowds: List<RawCrowdData> = jsonParser.parse(rawJson)
		linkedCharacterRegistry: Dictionary<String, Character> = new Dictionary()
		crowds: List<Crowd> = new List()
		for each rawCrowd in rawCrowds:
			crowd: Crowd = deserializeSingleCrowd(rawCrowd, linkedCharacterRegistry)
			crowd.sourceFile = this
			crowds.add(crowd)
		this.containedCrowds = crowds
		return crowds
+ serializeCrowd(crowd: Crowd): SaveResult
	Invariant: serializes crowd and all its descendants to JSON; linked members encoded once and cross-referenced; All Characters Crowd excluded; UTF-8 encoding
	Interaction:
		crowdJson: String = crowdSerializer.serialize(crowd)
		writeFile(absolutePath, crowdJson)
		return new SaveResult(succeeded: true)
+ serializeCrowds(crowds: List<Crowd>): void
	Invariant: serializes a list of crowds to this file; used for clone file creation; does not clear dirty flags on the source crowds
+ createBackupBeforeLoad(): void
	Invariant: creates a DailyBackup of this file before reading; skipped when backup already exists for today
	Interaction:
		backup: DailyBackup = new DailyBackup(this)
		backup.createIfNotTodayExists()
- deserializeSingleCrowd(raw: RawCrowdData, characterRegistry: Dictionary<String, Character>): Crowd
	Invariant: constructs a Crowd from raw parsed data; characters are registered in characterRegistry on first encounter and reused as the same instance when the same name appears as a linked member elsewhere

### **Active Crowd List** << Entity >>
+ ActiveCrowdList(storagePath: Path)
------
+ << composition >> paths: List<Path>
	Invariant: no duplicate paths; order reflects activation sequence
+ storagePath: Path
----
+ read(): List<Path>
	Invariant: reads from storagePath; returns empty list when file does not exist
+ append(path: Path): void
	Invariant: adds path to list; persists immediately; rejects duplicate paths
	Interaction:
		guard: not paths.contains(path)
		paths.add(path)
		this.persist()
+ remove(path: Path): void
	Invariant: removes path from list; persists immediately
	Interaction:
		paths.remove(path)
		this.persist()
+ contains(path: Path): Boolean
+ computeClonePath(originalPath: Path): Path
	Invariant: returns "<name> (N).json" in the same directory as originalPath; N is the lowest integer ≥ 2 not currently referenced in paths (gaps filled first)
- persist(): void
	Invariant: writes the current list of paths to storagePath as JSON; a session crash never loses an activation made earlier in the same session

### **Daily Backup** << Service >>
+ DailyBackup(crowdFile: CrowdFile)
------
+ crowdFile: CrowdFile
----
+ createIfNotTodayExists(): void
	Invariant: creates a date-stamped copy of crowdFile only if no backup exists for today's calendar date; at most one backup per crowd file per calendar day
	Interaction:
		backupPath: Path = computeBackupPath(crowdFile.absolutePath, today())
		if fileExists(backupPath): return
		copyFile(crowdFile.absolutePath, backupPath)
- computeBackupPath(filePath: Path, date: Date): Path
	Invariant: returns "<filename>_<date>.bak" in the same directory as filePath

### **Default Crowd Collection** << Service >>
+ DefaultCrowdCollection()
------
----
+ deserializeDefaults(): List<Crowd>
	Invariant: reads the embedded application resource containing starter crowds and characters; returns the default crowd hierarchy; never invoked automatically — only on explicit GM request

### references
**Ref — Crowd Repository KA**
Source: docs/increment-1/crc-increment-1.md
Locator: lines 192–252

```source
Crowd Repository
in-memory aggregate of every loaded top-level crowd | Crowd, Crowd Member, Character, Option Group
load active crowd files on startup  | Active Crowd List, Crowd File, Crowd, Daily Backup
clone active crowd file on re-activation | Active Crowd List, Crowd File, Crowd
save dirty crowds to source files   | Crowd, Crowd File, Daily Backup
save crowd to new file              | Crowd, Crowd File, Active Crowd List

Crowd File
absolute file path | (path string)
serialize contained crowds | Crowd, Daily Backup
deserialize contained crowds | Crowd

Active Crowd List
persisted list of crowd file paths | Crowd File, COH Game Directory
```

### decisions made
- Crowd Repository is a `<< Repository >>` — it is the in-memory aggregate of all loaded crowds and the coordination point for file-cloning and seeding; CrowdTree delegates per-file persistence detail (serialize, backup) directly to `CrowdFile` and `DailyBackup` — CrowdRepository owns the aggregate registry (`registerCrowd`, `deregisterCrowd`) only
- CRC responsibility `load active crowd files on startup | Active Crowd List, Crowd File, Crowd, Daily Backup` on Crowd Repository is executed through `CrowdTree.loadActiveFilesOnOpen()`, which calls `registerCrowd()` as the integration point into the repository; the load flow lives on CrowdTree because the tree owns the display surface; this divergence from the CRC collaborator placement is recorded here
- Crowd File is an `<< Entity >>` — its absolute path is its identity; `containedCrowds` is an aggregation (CrowdFile groups its crowds; Crowd has independent lifecycle via the repository)
- Active Crowd List is an `<< Entity >>` — it has persisted state (the paths list) and a crash-safety invariant; immediate persist-on-change is a lifecycle rule encoded as a domain invariant, not just a behavior
- Daily Backup is a `<< Service >>` — it performs one idempotent file operation per calendar day; no persistent domain state; receives `CrowdFile` as a constructor parameter to keep the dependency explicit
- Default Crowd Collection is a `<< Service >>` — stateless; reads embedded resource and produces Crowd instances on demand; never called at startup
- `SaveResult` is a value type (not modeled as a separate class): `succeeded: Boolean`, `reason: String`; introduced so `serializeCrowd` can communicate outcome without throwing for expected file-system failures
- `RawCrowdData` is an infrastructure parsing type omitted from the domain model; it appears in `deserializeSingleCrowd` to distinguish raw parsed data from a domain Crowd
- `Option Group` appears in the CRC collaborator list for Crowd Repository's aggregate (`in-memory aggregate... | Crowd, Crowd Member, Character, Option Group`) because OptionGroup instances are owned by Character instances that live inside the aggregate; no separate repository-level operation targets OptionGroup directly
