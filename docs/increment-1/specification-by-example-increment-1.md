---
state: specification-by-example
increment: 1
scope: Increment 1 — Character and Crowd Library (full)
date: 2026-05-18
---

# Specification by Example — Browse / Activate / Save Crowd Files

> Scope: the five stories changed by this feature on the **Manage Crowd Repository** sub-epic:
> **Browse and Activate Crowd Files**, **Load Active Crowd Files on Startup**, **Track Source File per Crowd**,
> **Save Dirty Crowds to Source Files**, **Save Crowd to New File**.
>
> Authoring rules from the `abd-specification-by-example` skill that this file obeys:
>
> 1. **Plain Scenarios use inline values** — bold for domain concepts, italic for values. No Gherkin data tables in plain Scenarios.
> 2. **Scenario Outlines use `{token}` placeholders + relationship-based Examples tables** — one table per domain concept, linked by foreign-key columns, never collapsed into a wide row.
> 3. **Invariants become scenarios** — `When <trigger> / Then <observable>`. No internal-state assertions in `Then`.
> 4. **Hierarchy is expressed in step prose** — nested *crowds* and their *characters* read in plain English, indented to convey containment.

## Domain terms

*active crowd list* · *crowd file* · *source file* · *crowd repository* · *crowd tree* ·
*crowd* · *crowd member* · *character* · *cloned character* · *all characters crowd* · *daily backup* ·
*COH data directory* · *character crowd main workspace* · *COH game directory* ·
*game directory prompt* · *Prism module* · *application shell* · *nested crowd* ·
*linked member* · *clipboard* · *option group* · *default crowd collection* ·
*browse mode* · *concept tag* · *group type* · *COH faction tag* · *dirty flag*

---

## Background — every scenario in this file assumes

```gherkin
Background:
  Given the **COH Data Directory** is *"C:\COH\data"*
  And the **Active Crowd List** is persisted at *"C:\COH\data\active-crowds.json"*
```

The contents of the *active crowd list* and the existence of any *crowd file* are stated **per scenario** — never assumed by the background.

---

## Story: Browse and Activate Crowd Files

### Scenario Outline: Activating crowd files loads their Crowd structures into the Crowd Tree

**CrowdFile** (Given — above scenario):

| scenario    | absoluteFilePath              |
| ----------- | ---------------------------- |
| Single file | C:\COH\data\armageddons.json |
| Two files   | C:\COH\data\heroes.json      |
| Two files   | C:\COH\data\villains.json    |

**Crowd** (Given — above scenario, root crowds only, FK: sourceFile → CrowdFile):

| scenario    | crowdName        | sourceFile                   |
| ----------- | ---------------- | ---------------------------- |
| Single file | Armageddon Squad | C:\COH\data\armageddons.json |
| Two files   | Freedom Phalanx  | C:\COH\data\heroes.json      |
| Two files   | Council Empire   | C:\COH\data\villains.json    |

**NestedCrowd** (Given — above scenario, FK: parentCrowd → Crowd):

| scenario    | crowdName       | parentCrowd      |
| ----------- | --------------- | ---------------- |
| Single file | Demolition Team | Armageddon Squad |

**Character** (Given — above scenario, FK: crowdName → Crowd or NestedCrowd):

| scenario    | characterName   | crowdName        |
| ----------- | --------------- | ---------------- |
| Single file | Battle Maiden   | Armageddon Squad |
| Single file | Manticore       | Armageddon Squad |
| Single file | Demo Lead       | Demolition Team  |
| Two files   | Statesman       | Freedom Phalanx  |
| Two files   | Positron        | Freedom Phalanx  |
| Two files   | Marcus Valerius | Council Empire   |

```gherkin
Given **Crowd File** {absoluteFilePath} per **CrowdFile** above exists on disk
  containing top-level **Crowd** {crowdName} with {sourceFile} per **Crowd** above
And {parentCrowd} contains **NestedCrowd** {crowdName} per **NestedCrowd** above
And {crowdName} contains **Character** {characterName} per **Character** above
And the persisted **Active Crowd List** is empty
And the **Character Crowd Main Workspace** is open
When the GM clicks **Browse Crowd Files** and selects {absoluteFilePath} in listed order
Then the **Crowd Tree** shows **Crowd** {crowdName} per **Crowd** above in activation sequence
  with **NestedCrowd** {crowdName} under {parentCrowd} per **NestedCrowd** above
  and {characterName} in {crowdName}
And the persisted **Active Crowd List** contains {absoluteFilePath} in selection order
And the **All Characters Crowd** shows {characterName} from **AllCharacters** in alphabetical order
```

**AllCharacters** (Then — below scenario, alphabetical order, FK: crowdName → Crowd or NestedCrowd):

| scenario    | characterName   |
| ----------- | --------------- |
| Single file | Battle Maiden   |
| Single file | Demo Lead       |
| Single file | Manticore       |
| Two files   | Marcus Valerius |
| Two files   | Positron        |
| Two files   | Statesman       |

> `scenario` joins all tables. **Crowd** holds root crowds only. **NestedCrowd** holds children; `parentCrowd` is the FK to the parent **Crowd** row.

---

```gherkin
Scenario: Cloning a Crowd File suffixes only top-level Crowd names, leaving nested Crowd names alone
  Given a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"*
      with nested **Crowd** *"Vampyri"* with **Character** *"Galaxy"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\villains.json"*
  When the GM clicks **Browse Crowd Files** and selects *"C:\COH\data\villains.json"* a second time
  Then a new **Crowd File** *"C:\COH\data\villains (2).json"* exists on disk
    containing top-level **Crowd** *"Council Empire (2)"*
      with nested **Crowd** *"Vampyri"* (name unchanged) with **Character** *"Galaxy"*
  And the original **Crowd File** *"C:\COH\data\villains.json"* is byte-unchanged on disk

Scenario: A malformed Crowd File is reported and skipped without aborting the others
  Given a **Crowd File** *"C:\COH\data\broken.json"* exists on disk but contains malformed JSON
  And a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"*
  And the persisted **Active Crowd List** is empty
  And the **Character Crowd Main Workspace** is open
  When the GM clicks **Browse Crowd Files** and selects both *"C:\COH\data\broken.json"* and *"C:\COH\data\heroes.json"*
  Then an error notification names *"C:\COH\data\broken.json"* and its deserialization reason
  And the persisted **Active Crowd List** contains exactly *"C:\COH\data\heroes.json"*
  And the **Crowd Tree** shows top-level **Crowd** *"Freedom Phalanx"* and no **Crowds** from *"broken.json"*

Scenario: Cancelling the file picker changes nothing
  Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk
    containing top-level **Crowd** *"Armageddon Squad"* with **Character** *"Battle Maiden"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  And the **Crowd Tree** already shows top-level **Crowd** *"Armageddon Squad"*
  When the GM clicks **Browse Crowd Files** and cancels the file picker
  Then the persisted **Active Crowd List** still contains exactly *"C:\COH\data\armageddons.json"*
  And the **Crowd Tree** is unchanged
```

### Scenario Outline: Re-activating an active Crowd File picks the next available integer suffix

**ActiveCrowdEntry** (Given — above scenario):

| scenario        | absoluteFilePath                    |
| --------------- | ----------------------------------- |
| First clone     | (none — only the original)          |
| Second clone    | C:\COH\data\armageddons (2).json    |
| Third clone     | C:\COH\data\armageddons (2).json    |
| Third clone     | C:\COH\data\armageddons (3).json    |
| Fill the gap    | C:\COH\data\armageddons (3).json    |

```gherkin
Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk
  containing top-level **Crowd** *"Armageddon Squad"*
    with **Character** *"Battle Maiden"*
    and nested **Crowd** *"Demolition Team"* with **Character** *"Demo Lead"*
And for each row in **ActiveCrowdEntry** above matching {scenario},
  a **Crowd File** at {absoluteFilePath} exists on disk
  containing top-level **Crowd** named after the file (without `.json`) with **Character** *"Battle Maiden"*
And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  plus every {absoluteFilePath} listed under **ActiveCrowdEntry** above for this {scenario}
When the GM clicks **Browse Crowd Files** and selects *"C:\COH\data\armageddons.json"* a second time
Then a new **Crowd File** {absoluteFilePath} from **CloneResult** exists on disk
  containing top-level **Crowd** {crowdName}
  with the same nested structure as the original
And the original **Crowd File** *"C:\COH\data\armageddons.json"* is byte-unchanged on disk
And the persisted **Active Crowd List** now also contains {absoluteFilePath} from **CloneResult**
And the **Crowd Tree** shows {crowdName} in addition to whatever was already there
```

**CloneResult** (Then — below scenario, FK: crowdName identifies the new top-level Crowd):

| scenario        | absoluteFilePath                      | crowdName                |
| --------------- | ------------------------------------- | ------------------------ |
| First clone     | C:\COH\data\armageddons (2).json      | Armageddon Squad (2)     |
| Second clone    | C:\COH\data\armageddons (3).json      | Armageddon Squad (3)     |
| Third clone     | C:\COH\data\armageddons (4).json      | Armageddon Squad (4)     |
| Fill the gap    | C:\COH\data\armageddons (2).json      | Armageddon Squad (2)     |

> `scenario` joins `ActiveCrowdEntry` to `CloneResult`. The `First clone` row in `ActiveCrowdEntry` is intentionally `(none)` — the original file is the only one in the Active Crowd List for that example. `Fill the gap` proves the rule picks the lowest available integer when a prior clone has been deleted out-of-band.

---

## Story: Load Active Crowd Files on Startup

```gherkin
Scenario: An empty Active Crowd List loads no Crowds and no defaults
  Given the **Active Crowd List** file *"C:\COH\data\active-crowds.json"* does not exist
  When the **Character Crowd Main Workspace** opens
  Then the **Crowd Tree** shows only the protected **All Characters Crowd** with no **Characters** under it
  And no file is created under *"C:\COH\data\"* by the load step
  And the **Default Crowd Collection** is not loaded
```

### Scenario Outline: Loading active Crowd Files on startup restores their crowd structure in list order

**CrowdFile** (Given — above scenario):

| scenario             | absoluteFilePath              |
| -------------------- | ---------------------------- |
| Single nested file   | C:\COH\data\villains.json    |
| Two files list order | C:\COH\data\heroes.json      |
| Two files list order | C:\COH\data\villains.json    |

**Crowd** (Given — above scenario, root crowds only, FK: sourceFile → CrowdFile):

| scenario             | crowdName       | sourceFile                |
| -------------------- | --------------- | ------------------------- |
| Single nested file   | Council Empire  | C:\COH\data\villains.json |
| Two files list order | Freedom Phalanx | C:\COH\data\heroes.json   |
| Two files list order | Council Empire  | C:\COH\data\villains.json |

**NestedCrowd** (Given — above scenario, FK: parentCrowd → Crowd):

| scenario           | crowdName      | parentCrowd    |
| ------------------ | -------------- | -------------- |
| Single nested file | Vampyri        | Council Empire |
| Single nested file | Galaxy Council | Council Empire |

**Character** (Given — above scenario, FK: crowdName → Crowd or NestedCrowd):

| scenario             | characterName   | crowdName       |
| -------------------- | --------------- | --------------- |
| Single nested file   | Marcus Valerius | Council Empire  |
| Single nested file   | Galaxy          | Vampyri         |
| Single nested file   | Vandal          | Vampyri         |
| Single nested file   | Black Swan      | Galaxy Council  |
| Two files list order | Statesman       | Freedom Phalanx |
| Two files list order | Marcus Valerius | Council Empire  |

```gherkin
Given **Crowd File** {absoluteFilePath} per **CrowdFile** above exists on disk
  containing top-level **Crowd** {crowdName} with {sourceFile} per **Crowd** above
And {parentCrowd} contains **NestedCrowd** {crowdName} per **NestedCrowd** above
And {crowdName} contains **Character** {characterName} per **Character** above
And the persisted **Active Crowd List** contains {absoluteFilePath} in listed order
When the **Character Crowd Main Workspace** opens
Then the **Crowd Tree** shows **Crowd** {crowdName} per **Crowd** above in list order
  with **NestedCrowd** {crowdName} under {parentCrowd} per **NestedCrowd** above
  and {characterName} in {crowdName}
And the **All Characters Crowd** shows {characterName} from **AllCharacters** in alphabetical order
```

**AllCharacters** (Then — below scenario, alphabetical order, FK: crowdName → Crowd or NestedCrowd):

| scenario             | characterName   |
| -------------------- | --------------- |
| Single nested file   | Black Swan      |
| Single nested file   | Galaxy          |
| Single nested file   | Marcus Valerius |
| Single nested file   | Vandal          |
| Two files list order | Marcus Valerius |
| Two files list order | Statesman       |

> `scenario` joins all tables. **Crowd** holds root crowds only. **NestedCrowd** holds children; `parentCrowd` is the FK to the parent **Crowd** row. The `Two files list order` scenario proves **Freedom Phalanx** appears before **Council Empire** because `heroes.json` precedes `villains.json` in the persisted **Active Crowd List**.

```gherkin
Scenario: A missing path on disk is reported and skipped, others still load
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"*
  And no file exists at *"C:\COH\data\missing.json"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"* and *"C:\COH\data\missing.json"*
  When the **Character Crowd Main Workspace** opens
  Then a warning notification names *"C:\COH\data\missing.json"* as not found
  And the persisted **Active Crowd List** still contains both paths (the missing path is left for GM action)
  And the **Crowd Tree** shows top-level **Crowd** *"Freedom Phalanx"* only

Scenario: A malformed active Crowd File is reported and skipped
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"*
  And a **Crowd File** *"C:\COH\data\corrupt.json"* exists on disk but contains malformed JSON
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"* and *"C:\COH\data\corrupt.json"*
  When the **Character Crowd Main Workspace** opens
  Then an error notification names *"C:\COH\data\corrupt.json"* and its deserialization reason
  And the **Crowd Tree** shows top-level **Crowd** *"Freedom Phalanx"* from *"heroes.json"*
  And no **Crowd** from *"corrupt.json"* appears anywhere in the **Crowd Tree**
```

---

## Story: Track Source File per Crowd

> The *source file* property is internal. It is exercised through save behavior:
> a changed loaded *crowd* writes back to the file it came from; a save targets that file and no other;
> a nested *crowd* moved to a different top-level ancestor saves into the new ancestor's *source file*.

```gherkin
Scenario: Saving a changed loaded Crowd writes back to its own Source File and leaves others untouched
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"*
  And a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"* with **Character** *"Marcus Valerius"*
  And the persisted **Active Crowd List** contains both paths
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed **Crowd** *"Freedom Phalanx"* to *"Freedom Phalanx Reformed"*
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\heroes.json"* is overwritten and now contains top-level **Crowd** *"Freedom Phalanx Reformed"* with **Character** *"Statesman"*
  And *"C:\COH\data\villains.json"* is byte-unchanged on disk
  And a **Daily Backup** of *"C:\COH\data\heroes.json"* exists for today's date

Scenario: A nested Crowd moved between two top-level Crowds in different Source Files writes both files
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"*
      with **Character** *"Statesman"*
      and nested **Crowd** *"Phalanx Recruits"* with **Character** *"Apprentice 1"*
  And a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"* with **Character** *"Marcus Valerius"*
  And the persisted **Active Crowd List** contains both paths
  And the **Character Crowd Main Workspace** has been opened
  And the GM has drag-dropped nested **Crowd** *"Phalanx Recruits"* from *"Freedom Phalanx"* into *"Council Empire"*
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\heroes.json"* is overwritten and now contains top-level **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"* only (no nested **Crowd**)
  And *"C:\COH\data\villains.json"* is overwritten and now contains top-level **Crowd** *"Council Empire"*
    with **Character** *"Marcus Valerius"*
    and nested **Crowd** *"Phalanx Recruits"* with **Character** *"Apprentice 1"*

Scenario: A Character added inside a nested Crowd writes the parent file
  Given a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"*
      with nested **Crowd** *"Vampyri"* with **Character** *"Galaxy"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\villains.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has added **Character** *"Vandal"* to nested **Crowd** *"Vampyri"*
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\villains.json"* is overwritten and now contains top-level **Crowd** *"Council Empire"*
    with nested **Crowd** *"Vampyri"* containing **Characters** *"Galaxy"* and *"Vandal"*

Scenario: Renaming a nested Crowd writes the parent file
  Given a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"*
      with nested **Crowd** *"Vampyri"* with **Character** *"Galaxy"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\villains.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed nested **Crowd** *"Vampyri"* to *"Vampyri Cabal"*
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\villains.json"* is overwritten and now contains top-level **Crowd** *"Council Empire"*
    with nested **Crowd** *"Vampyri Cabal"* with **Character** *"Galaxy"*
  And *"C:\COH\data\villains.json"* no longer contains any **Crowd** named *"Vampyri"*
```

---

## Story: Save Dirty Crowds to Source Files

```gherkin
Scenario: Save skips a clean Crowd
  Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk
    containing top-level **Crowd** *"Armageddon Squad"* with **Character** *"Battle Maiden"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has made no changes
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\armageddons.json"* is not opened for writing
  And no **Daily Backup** is created for that file
  And the GM sees a *"Nothing to save"* status

Scenario: Save writes only the dirty files among many loaded
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk containing top-level **Crowd** *"Freedom Phalanx"*
  And a **Crowd File** *"C:\COH\data\villains.json"* exists on disk containing top-level **Crowd** *"Council Empire"*
  And a **Crowd File** *"C:\COH\data\neutrals.json"* exists on disk containing top-level **Crowd** *"Wandering Wraith"*
  And the persisted **Active Crowd List** contains all three paths
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed only *"Freedom Phalanx"* to *"Freedom Phalanx Reformed"*
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\heroes.json"* is overwritten and now contains top-level **Crowd** *"Freedom Phalanx Reformed"*
  And *"C:\COH\data\villains.json"* is byte-unchanged on disk
  And *"C:\COH\data\neutrals.json"* is byte-unchanged on disk
  And a save summary tells the GM that *1 file was saved* and *0 failed*

Scenario: A Daily Backup is created before overwriting a dirty file
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk containing top-level **Crowd** *"Freedom Phalanx"*
  And no **Daily Backup** file for *"heroes.json"* exists for today's date
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed *"Freedom Phalanx"* to *"Freedom Phalanx Reformed"*
  When the GM invokes **Save Dirty Crowds**
  Then a **Daily Backup** file *"C:\COH\data\heroes.<today>.bak"* exists with the pre-save content of *"heroes.json"*
  And *"C:\COH\data\heroes.json"* is then overwritten with the post-save content

Scenario: One failing write leaves that file dirty and does not block the others
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk and is writable, containing top-level **Crowd** *"Freedom Phalanx"*
  And a **Crowd File** *"C:\COH\data\readonly.json"* exists on disk and is marked read-only, containing top-level **Crowd** *"Read Only Squad"*
  And the persisted **Active Crowd List** contains both
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed both top-level **Crowds** to new values
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\heroes.json"* is overwritten with the post-save content
  And *"C:\COH\data\readonly.json"* is byte-unchanged on disk
  And an error message names *"C:\COH\data\readonly.json"* and its failure reason
  And invoking **Save Dirty Crowds** a second time still attempts to write *"readonly.json"*
  But invoking **Save Dirty Crowds** a second time does not re-write *"heroes.json"*

Scenario: Saving a never-saved Crowd opens the Save Crowd to New File dialog
  Given the persisted **Active Crowd List** is empty
  And the **Character Crowd Main Workspace** has been opened
  And the GM has created a new top-level **Crowd** *"New Squad"* in memory via **Create Crowd**
  When the GM invokes **Save Dirty Crowds**
  Then the system opens the **Save Crowd to New File** dialog for *"New Squad"*
  And the dialog defaults the filename to *"New Squad.json"* in the **COH Data Directory**
  And no file is created under *"C:\COH\data\"* until the GM confirms the dialog

Scenario: Save handles a mix of Source-bound and never-saved Crowds in one invocation
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
    containing top-level **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed *"Freedom Phalanx"* to *"Freedom Phalanx Reformed"*
  And the GM has created a new top-level **Crowd** *"Splinter Cell"* in memory via **Create Crowd**
  When the GM invokes **Save Dirty Crowds**
  Then *"C:\COH\data\heroes.json"* is overwritten and now contains top-level **Crowd** *"Freedom Phalanx Reformed"*
  And the system opens the **Save Crowd to New File** dialog for *"Splinter Cell"*
  And the dialog defaults the filename to *"Splinter Cell.json"* in the **COH Data Directory**

Scenario: Cancelling the auto-prompted Save Crowd to New File leaves that Crowd unsaved
  Given the persisted **Active Crowd List** is empty
  And the **Character Crowd Main Workspace** has been opened
  And the GM has created a new top-level **Crowd** *"New Squad"* in memory via **Create Crowd**
  When the GM invokes **Save Dirty Crowds** and cancels the **Save Crowd to New File** dialog
  Then no file is created under *"C:\COH\data\"*
  And the persisted **Active Crowd List** is still empty
  And the **Crowd Tree** still shows *"New Squad"* as an unsaved top-level **Crowd**
  And a subsequent **Save Dirty Crowds** opens the **Save Crowd to New File** dialog for *"New Squad"* again

Scenario: Closing with unsaved changes prompts before exit
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk containing top-level **Crowd** *"Freedom Phalanx"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed *"Freedom Phalanx"* to *"Freedom Phalanx Reformed"*
  When the GM closes the application
  Then the application shows a prompt offering **Save**, **Discard**, and **Cancel**
  And no file is overwritten until the GM picks an option
```

---

## Story: Save Crowd to New File

```gherkin
Scenario: Save As writes a fresh Crowd File and activates it
  Given the persisted **Active Crowd List** is empty
  And the **Character Crowd Main Workspace** has been opened
  And the GM has built a new top-level **Crowd** *"New Squad"* in memory
    with **Characters** *"Recruit Alpha"* and *"Recruit Beta"*
  When the GM invokes **Save Crowd to New File** with *"New Squad"* selected
    and confirms *"C:\COH\data\new-squad.json"* in the save-file dialog
  Then a **Crowd File** *"C:\COH\data\new-squad.json"* exists on disk
    containing top-level **Crowd** *"New Squad"* with **Characters** *"Recruit Alpha"* and *"Recruit Beta"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\new-squad.json"*
  And after re-opening the **Character Crowd Main Workspace**, *"New Squad"* loads automatically from *"C:\COH\data\new-squad.json"*

Scenario: Save As of a top-level Crowd with nested Crowds writes the full subtree
  Given the persisted **Active Crowd List** is empty
  And the **Character Crowd Main Workspace** has been opened
  And the GM has built top-level **Crowd** *"Council Empire"* in memory
    with **Character** *"Marcus Valerius"*
    and nested **Crowd** *"Vampyri"* with **Characters** *"Galaxy"* and *"Vandal"*
  When the GM invokes **Save Crowd to New File** with *"Council Empire"* selected
    and confirms *"C:\COH\data\villains.json"* in the save-file dialog
  Then a **Crowd File** *"C:\COH\data\villains.json"* exists on disk
    containing top-level **Crowd** *"Council Empire"*
      with **Character** *"Marcus Valerius"*
      and nested **Crowd** *"Vampyri"* with **Characters** *"Galaxy"* and *"Vandal"*

Scenario: Save As of a loaded Crowd switches its Source File to the new path
  Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk
    containing top-level **Crowd** *"Armageddon Squad"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed *"Armageddon Squad"* to *"Armageddon Squad Reforged"*
  When the GM invokes **Save Crowd to New File** with *"Armageddon Squad Reforged"* selected
    and confirms *"C:\COH\data\armageddon-reforged.json"* in the save-file dialog
  Then a **Crowd File** *"C:\COH\data\armageddon-reforged.json"* exists on disk containing top-level **Crowd** *"Armageddon Squad Reforged"*
  And *"C:\COH\data\armageddons.json"* is byte-unchanged on disk
  And a subsequent rename followed by **Save Dirty Crowds** writes to *"C:\COH\data\armageddon-reforged.json"*, not to *"C:\COH\data\armageddons.json"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddon-reforged.json"*

Scenario: Cancelling the Save As dialog leaves everything untouched
  Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk containing top-level **Crowd** *"Armageddon Squad"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  And the **Character Crowd Main Workspace** has been opened
  And the GM has renamed *"Armageddon Squad"* to *"Armageddon Squad Reforged"*
  When the GM invokes **Save Crowd to New File** and cancels the save-file dialog
  Then no new file is created under *"C:\COH\data\"*
  And *"C:\COH\data\armageddons.json"* is byte-unchanged on disk
  And a subsequent **Save Dirty Crowds** still writes to *"C:\COH\data\armageddons.json"*

Scenario: Save As to an existing path replaces it without creating a Daily Backup of the prior file
  Given a **Crowd File** *"C:\COH\data\target.json"* exists on disk with arbitrary prior content
  And the **Character Crowd Main Workspace** has been opened
  And the GM has built a new top-level **Crowd** *"Replacement Squad"* in memory with **Character** *"Replacement"*
  When the GM invokes **Save Crowd to New File** with *"Replacement Squad"* selected
    and confirms *"C:\COH\data\target.json"* in the save-file dialog (acknowledging the overwrite prompt)
  Then *"C:\COH\data\target.json"* on disk now contains only top-level **Crowd** *"Replacement Squad"* with **Character** *"Replacement"*
  And no **Daily Backup** file *"C:\COH\data\target.<today>.bak"* exists from this operation

Scenario: Save As is rejected when a nested Crowd is the selection
  Given a **Crowd File** *"C:\COH\data\armageddons.json"* exists on disk
    containing top-level **Crowd** *"Armageddon Squad"*
      with nested **Crowd** *"Demolition Team"* with **Character** *"Demo Lead"*
  And the persisted **Active Crowd List** contains *"C:\COH\data\armageddons.json"*
  And the **Character Crowd Main Workspace** has been opened
  When the GM invokes **Save Crowd to New File** with the nested **Crowd** *"Demolition Team"* selected in the **Crowd Tree**
  Then no save-file dialog opens
  And a status message tells the GM that **Save Crowd to New File** requires a top-level **Crowd** selection
  And no file is created or modified under *"C:\COH\data\"*
```

---

## Story: Validate City of Heroes Game Directory

```gherkin
Scenario: Valid stored path proceeds to module load without showing the prompt
  Given the application configuration stores **COH Game Directory** *"C:\Games\City of Heroes"*
  And *"C:\Games\City of Heroes"* is an existing readable directory containing the expected COH installation artifacts
  When the **Application Shell** starts
  Then the system proceeds to load the **Prism Module** without displaying the **Game Directory Prompt**
  And the **COH Data Directory** is derived as *"C:\Games\City of Heroes\data"*

Scenario: Absent stored path opens the Game Directory Prompt before any module loads
  Given the application configuration contains no stored **COH Game Directory** path
  When the **Application Shell** starts
  Then the system displays the **Game Directory Prompt** as a modal dialog
  And the **Prism Module** is not loaded
  And the **Character Crowd Main Workspace** is not opened

Scenario: Invalid stored path opens the prompt with a descriptive validation feedback message
  Given the application configuration stores **COH Game Directory** *"C:\Games\NotCOH"*
  And *"C:\Games\NotCOH"* does not contain the expected COH installation artifacts
  When the **Application Shell** starts
  Then the system displays the **Game Directory Prompt** with a validation feedback message (e.g. *"Not a valid COH installation"*)
  And the Continue button is disabled
  And the **Prism Module** is not loaded
```

---

## Story: Prompt for Game Directory if Invalid

```gherkin
Scenario: Typing a valid path enables the Continue button and clears the error message
  Given the **Game Directory Prompt** is displayed with the path input empty and Continue disabled
  When the GM types *"C:\Games\City of Heroes"* into the path input field
  And *"C:\Games\City of Heroes"* is a valid COH installation directory
  Then the Continue button becomes enabled
  And the validation feedback label is blank

Scenario: Clearing the path input disables Continue and shows a prompt message
  Given the **Game Directory Prompt** is displayed with a valid path entered and Continue enabled
  When the GM clears the path input field entirely
  Then the Continue button is disabled
  And the validation feedback label shows *"Please enter a directory path"*

Scenario: Browse button opens the folder picker and populates the path input
  Given the **Game Directory Prompt** is displayed
  When the GM clicks Browse and selects *"C:\Games\City of Heroes"* in the OS folder picker
  Then the path input shows *"C:\Games\City of Heroes"*
  And if *"C:\Games\City of Heroes"* is valid, the Continue button is enabled immediately

Scenario: Clicking Continue while enabled dismisses the prompt and starts module load
  Given the **Game Directory Prompt** is displayed with *"C:\Games\City of Heroes"* entered and Continue enabled
  When the GM clicks Continue
  Then the **Game Directory Prompt** is dismissed
  And the system proceeds to load the **Prism Module**
  And the **Character Crowd Main Workspace** opens
```

---

## Story: Load Prism Shell and Module

```gherkin
Scenario: Successful module load navigates to the Character Crowd Main Workspace
  Given the **COH Game Directory** *"C:\Games\City of Heroes"* has been confirmed valid
  When the **Prism Module** finishes loading
  Then the system navigates to the **Character Crowd Main Workspace** as the initial view
  And the **Crowd Tree** is visible and ready for interaction

Scenario: Module load failure shows an error dialog and does not open the workspace
  Given the **COH Game Directory** has been confirmed valid
  When the **Prism Module** fails to load due to a missing assembly
  Then the system displays an error dialog describing the failure
  And the **Character Crowd Main Workspace** is not opened
```

---

## Story: Open Character Crowd Main Workspace

```gherkin
Scenario: Workspace shows Identities tab active, Abilities and Movements greyed
  Given the **Prism Module** has finished loading
  When the **Character Crowd Main Workspace** opens
  Then the Identities tab is active
  And the Abilities tab is visible but non-interactive
  And the Movements tab is visible but non-interactive
  And the **Crowd Tree** panel is shown on the left

Scenario: Workspace opens cleanly with an empty Crowd Tree when no crowds are loaded
  Given the **Active Crowd List** is absent
  When the **Character Crowd Main Workspace** opens
  Then the **Crowd Tree** shows only the protected **All Characters Crowd** with no characters under it
  And no error state is shown
  And the GM can immediately invoke Create Crowd
```

---

## Story: Deserialize Crowd Collection from JSON

```gherkin
Scenario: Deserialization restores the full crowd hierarchy preserving member order
  Given a **Crowd File** *"C:\COH\data\villains.json"* contains serialized top-level **Crowd** *"Council Empire"*
    with **Character** *"Marcus Valerius"*
    and nested **Crowd** *"Vampyri"* containing **Characters** *"Galaxy"* then *"Vandal"* in that order
  When the system deserializes *"C:\COH\data\villains.json"*
  Then the **Crowd Tree** shows **Crowd** *"Council Empire"*
    containing **Character** *"Marcus Valerius"*
    and nested **Crowd** *"Vampyri"* with *"Galaxy"* then *"Vandal"* in the original order

Scenario: Linked members are restored as a single in-memory instance referenced across crowds
  Given a **Crowd File** *"C:\COH\data\crosslinks.json"* encodes **Character** *"Shared Hero"* once
    and references it in both **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the system deserializes *"C:\COH\data\crosslinks.json"*
  Then *"Shared Hero"* appears as a **Linked Member** in both *"Team Alpha"* and *"Team Beta"*
  And renaming *"Shared Hero"* from *"Team Alpha"* updates the name in *"Team Beta"* as well

Scenario: Duplicate member names are tolerated — first occurrence loads and others are discarded
  Given a **Crowd File** *"C:\COH\data\dupes.json"* contains **Crowd** *"Patrol"*
    with two serialized **Characters** both named *"Guard"*
  When the system deserializes *"C:\COH\data\dupes.json"*
  Then a data integrity warning is logged naming *"Guard"* as a duplicate in *"Patrol"*
  And **Crowd** *"Patrol"* contains exactly one **Character** *"Guard"*
  And the system does not crash or refuse to load the file
```

---

## Story: Load Default Crowd Members from Embedded Resource

```gherkin
Scenario: First run with no crowd repository loads the embedded default crowds
  Given the **Active Crowd List** is absent
  And no crowd file exists under *"C:\COH\data\"*
  When the **Character Crowd Main Workspace** opens
  Then the **Crowd Tree** shows the embedded default root **Crowds** (e.g. *"Animals"*, *"Armed Forces"*, *"Civilians"*, *"Vehicles"*)
  And each default **Crowd** contains its pre-defined **Characters**
  And the **All Characters Crowd** is populated with all default **Characters** in alphabetical order

Scenario: Saving after loading defaults writes crowd files so subsequent launches do not reload defaults
  Given the default **Crowd Collection** has been loaded on first run
  When the GM invokes **Save Dirty Crowds** and confirms file paths for each top-level **Crowd**
  Then a **Crowd File** exists for each default root **Crowd** under *"C:\COH\data\"*
  And the **Active Crowd List** contains the paths of those files
  And on the next application launch the embedded default **Crowd Collection** is not loaded
```

---

## Story: Serialize Crowd Collection to JSON

```gherkin
Scenario: Linked member is encoded once and cross-referenced in each crowd it appears in
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the system serializes the **Crowd File** containing both crowds
  Then the JSON encodes *"Shared Hero"* exactly once
  And each crowd's member list holds a reference to *"Shared Hero"* rather than a full embedded copy
  And deserializing the same file restores *"Shared Hero"* as a single in-memory instance in both crowds

Scenario: All Characters Crowd is excluded from the serialized JSON
  Given the **Crowd Repository** contains **Crowd** *"Freedom Phalanx"* with **Character** *"Statesman"*
  And the **All Characters Crowd** aggregates *"Statesman"*
  When the system serializes *"C:\COH\data\heroes.json"*
  Then the JSON does not contain a top-level **Crowd** entry representing the **All Characters Crowd**
  And on deserialization the **All Characters Crowd** is reconstructed from the loaded crowds

Scenario: Serialization preserves insertion order within each crowd
  Given **Crowd** *"Council Empire"* contains **Characters** *"Marcus Valerius"* then *"Black Swan"* in that insertion order
  When the system serializes and then deserializes the crowd file
  Then **Crowd** *"Council Empire"* contains *"Marcus Valerius"* then *"Black Swan"* in the same order
```

---

## Story: Create Daily Backup of Crowd Repository

```gherkin
Scenario: First save of the day creates a dated backup before overwriting
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk with prior content
  And no backup file for *"heroes.json"* exists for today's date
  When the system saves **Crowd** *"Freedom Phalanx Reformed"* to *"C:\COH\data\heroes.json"*
  Then a dated backup *"C:\COH\data\heroes_<today>.bak"* exists containing the pre-save content
  And *"C:\COH\data\heroes.json"* is overwritten with the post-save content

Scenario: A second save on the same day does not create a second backup
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
  And a backup *"C:\COH\data\heroes_<today>.bak"* already exists from an earlier save today
  When the system saves *"C:\COH\data\heroes.json"* a second time
  Then no additional backup file is created for today's date
  And the existing backup *"C:\COH\data\heroes_<today>.bak"* is unchanged

Scenario: Backup failure still allows the save to proceed with a GM warning
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
  And the system cannot create a backup (e.g. disk full)
  When the system saves *"C:\COH\data\heroes.json"*
  Then the GM is notified that the backup was not created
  And *"C:\COH\data\heroes.json"* is still overwritten with the new content
```

---

## Story: Store Crowd Repository in COH Data Directory

```gherkin
Scenario: New crowd file is written to the COH data directory
  Given the **COH Data Directory** is *"C:\COH\data"* and it exists on disk
  When the GM confirms *"C:\COH\data\patrol.json"* in the **Save Crowd to New File** dialog
  Then the **Crowd File** is written to *"C:\COH\data\patrol.json"*

Scenario: A missing COH data directory is created before writing
  Given the **COH Data Directory** *"C:\COH\data"* does not yet exist on disk
  When the system writes a **Crowd File** to *"C:\COH\data\patrol.json"*
  Then the system creates *"C:\COH\data\"* (and any required parent directories) before writing
  And *"C:\COH\data\patrol.json"* exists on disk after the write
```

---

## Story: Back Up Repository on Load

```gherkin
Scenario: Startup creates a pre-load backup before reading each active Crowd File
  Given a **Crowd File** *"C:\COH\data\heroes.json"* exists on disk
  And no backup for *"heroes.json"* exists for today's date
  And the persisted **Active Crowd List** contains *"C:\COH\data\heroes.json"*
  When the **Character Crowd Main Workspace** opens
  Then a backup *"C:\COH\data\heroes_<today>.bak"* is created before the file is read
  And *"C:\COH\data\heroes.json"* is then read and deserialized normally

Scenario: Same-day load backup and save backup result in exactly one backup file
  Given the startup backup for *"C:\COH\data\heroes.json"* was already created this morning
  When the GM saves *"C:\COH\data\heroes.json"* later the same day
  Then no additional backup file is created for *"heroes.json"* for today's date
  And the backup from the morning load is unchanged
```

---

## Story: Create Crowd

```gherkin
Scenario: Create Crowd adds a root-level node in inline-edit mode and marks collection dirty
  Given the **Character Crowd Main Workspace** is open with no crowds loaded
  When the GM invokes Create Crowd from the toolbar
  Then a new **Crowd** node named *"New Crowd"* appears in the **Crowd Tree** with its name in inline-edit mode
  And the **Crowd Collection** is marked dirty

Scenario: Confirming a unique name completes creation and selects the new crowd
  Given a new **Crowd** is in inline-edit mode at root level
  When the GM types *"Patrol Alpha"* and presses Enter
  Then the **Crowd** is named *"Patrol Alpha"*
  And the inline edit collapses with *"Patrol Alpha"* selected in the **Crowd Tree**

Scenario: Confirming a duplicate sibling name is rejected with a validation message
  Given **Crowd** *"Patrol Alpha"* already exists at root level
  And a new **Crowd** is in inline-edit mode at root level
  When the GM types *"Patrol Alpha"* and presses Enter
  Then the system rejects the name
  And the inline-edit remains active with the message *"A crowd with this name already exists"*

Scenario: Pressing Escape during creation cancels and removes the provisional Crowd entry
  Given a new **Crowd** is in inline-edit mode at root level
  When the GM presses Escape
  Then the provisional **Crowd** entry is removed from the **Crowd Tree**
  And the **Crowd Collection** is not marked dirty
```

---

## Story: Rename Crowd

```gherkin
Scenario: Rename succeeds and marks the Crowd Collection dirty
  Given **Crowd** *"Old Patrol"* exists in the **Crowd Tree**
  When the GM double-clicks *"Old Patrol"* and types *"New Patrol"* then presses Enter
  Then the **Crowd Tree** shows *"New Patrol"* in place of *"Old Patrol"*
  And the **Crowd Collection** is marked dirty

Scenario: Rename to an existing sibling name is rejected with a validation message
  Given **Crowd** *"Patrol Alpha"* and **Crowd** *"Patrol Beta"* both exist at root level
  When the GM renames *"Patrol Beta"* to *"Patrol Alpha"*
  Then the system rejects the rename
  And the inline-edit remains active with the message *"A crowd with this name already exists"*

Scenario: Pressing Escape during rename restores the original name without marking dirty
  Given **Crowd** *"Patrol Alpha"* is in inline-edit mode
  When the GM types *"Patrol Z"* then presses Escape
  Then the **Crowd Tree** still shows *"Patrol Alpha"*
  And the **Crowd Collection** is not marked dirty

Scenario: Renaming the All Characters Crowd is silently ignored
  Given the **All Characters Crowd** is visible in the **Crowd Tree**
  When the GM attempts to rename the **All Characters Crowd** node
  Then the system ignores the rename attempt
  And the **All Characters Crowd** name is unchanged
```

---

## Story: Delete Crowd

```gherkin
Scenario: Confirming deletion removes the crowd, all its members, and updates All Characters
  Given **Crowd** *"Patrol Alpha"* exists with **Characters** *"Recruit 1"* and *"Recruit 2"*
  And the **All Characters Crowd** lists *"Recruit 1"* and *"Recruit 2"*
  When the GM invokes Delete on *"Patrol Alpha"* and confirms the prompt
  Then *"Patrol Alpha"* no longer appears in the **Crowd Tree**
  And *"Recruit 1"* and *"Recruit 2"* are removed from the **All Characters Crowd**
  And the **Crowd Collection** is marked dirty

Scenario: Linked members in other crowds survive when their shared crowd is deleted
  Given **Character** *"Shared Hero"* is a **Linked Member** in both **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM invokes Delete on *"Team Alpha"* and confirms
  Then *"Team Alpha"* no longer appears in the **Crowd Tree**
  And *"Shared Hero"* still appears in **Crowd** *"Team Beta"*

Scenario: Cancelling the deletion prompt leaves the crowd unchanged
  Given **Crowd** *"Patrol Alpha"* exists in the **Crowd Tree**
  When the GM invokes Delete on *"Patrol Alpha"* and cancels the confirmation prompt
  Then *"Patrol Alpha"* still appears in the **Crowd Tree** unchanged

Scenario: Deleting the All Characters Crowd is silently ignored
  Given the **All Characters Crowd** is visible in the **Crowd Tree**
  When the GM attempts to delete the **All Characters Crowd** node
  Then the system takes no action and the **All Characters Crowd** remains
```

---

## Story: Nest Crowd inside Crowd

```gherkin
Scenario: Drag-drop makes the dragged crowd a nested child of the target
  Given root-level **Crowd** *"Patrol Alpha"* and root-level **Crowd** *"Region North"* exist
  When the GM drag-drops *"Patrol Alpha"* onto *"Region North"*
  Then *"Patrol Alpha"* is no longer at root level
  And *"Patrol Alpha"* appears as a nested child of *"Region North"* in the **Crowd Tree**
  And the **Crowd Collection** is marked dirty

Scenario: Dragging a crowd onto itself is rejected
  Given **Crowd** *"Patrol Alpha"* exists in the **Crowd Tree**
  When the GM attempts to drag *"Patrol Alpha"* onto *"Patrol Alpha"*
  Then the system rejects the operation
  And the **Crowd Tree** is unchanged

Scenario: Nesting is rejected when the child name conflicts with an existing sibling in the target
  Given **Crowd** *"Region North"* already contains a nested **Crowd** named *"Patrol Alpha"*
  And a separate root-level **Crowd** *"Patrol Alpha"* also exists
  When the GM drag-drops the root-level *"Patrol Alpha"* onto *"Region North"*
  Then the system rejects the nesting
  And the GM is notified that a crowd named *"Patrol Alpha"* already exists under *"Region North"*
  And the **Crowd Tree** is unchanged
```

---

## Story: Create Character in Crowd

```gherkin
Scenario: Create Character adds an inline-edit node under the selected crowd with three Option Groups
  Given **Crowd** *"Patrol Alpha"* is selected in the **Crowd Tree**
  When the GM invokes Create Character
  Then a new **Character** *"New Character"* appears under *"Patrol Alpha"* in inline-edit mode
  And the **All Characters Crowd** is updated to include *"New Character"*
  And after confirming the name, the **Character** has exactly three **Option Groups** — *"Identities"*, *"Abilities"*, *"Movements"*
  And the **Crowd Collection** is marked dirty

Scenario: Confirming a duplicate name within the crowd is rejected with a validation message
  Given **Crowd** *"Patrol Alpha"* already contains **Character** *"Recruit 1"*
  When the GM creates a new **Character** and confirms the name *"Recruit 1"*
  Then the system rejects the name
  And the inline-edit remains active with a validation message

Scenario: Pressing Escape cancels creation and removes the provisional character node
  Given a new **Character** node is in inline-edit mode under **Crowd** *"Patrol Alpha"*
  When the GM presses Escape
  Then the provisional **Character** node is removed from the **Crowd Tree**
  And the **All Characters Crowd** does not contain the provisional name
```

---

## Story: Rename Character

```gherkin
Scenario: Rename propagates to all crowds the character appears in and re-sorts All Characters
  Given **Character** *"Shared Hero"* is a **Linked Member** in both **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  And the **All Characters Crowd** contains *"Shared Hero"*
  When the GM renames *"Shared Hero"* to *"Apex"* from **Crowd** *"Team Alpha"*
  Then **Crowd** *"Team Alpha"* shows *"Apex"* where *"Shared Hero"* was
  And **Crowd** *"Team Beta"* shows *"Apex"* where *"Shared Hero"* was
  And the **All Characters Crowd** shows *"Apex"* in its sorted position
  And the **Crowd Collection** is marked dirty

Scenario: Rename rejected when the new name conflicts with a member in any crowd the character belongs to
  Given **Character** *"Shared Hero"* appears in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  And **Crowd** *"Team Beta"* already contains a different **Character** named *"Apex"*
  When the GM attempts to rename *"Shared Hero"* to *"Apex"*
  Then the system rejects the rename
  And the inline-edit remains active with a validation message

Scenario: Pressing Escape restores the original name everywhere without marking dirty
  Given **Character** *"Shared Hero"* is in inline-edit mode showing *"Shared Hero"*
  When the GM types *"New Name"* then presses Escape
  Then *"Shared Hero"* is restored in all crowds and in the **All Characters Crowd**
  And the **Crowd Collection** is not marked dirty
```

---

## Story: Delete Character from Crowd

```gherkin
Scenario: Delete removes a non-linked character from its crowd and from All Characters
  Given **Crowd** *"Patrol Alpha"* contains **Character** *"Recruit 1"* (not linked elsewhere)
  And the **All Characters Crowd** contains *"Recruit 1"*
  When the GM invokes Delete on *"Recruit 1"* in the **Crowd Tree**
  Then *"Recruit 1"* is removed from **Crowd** *"Patrol Alpha"*
  And *"Recruit 1"* is removed from the **All Characters Crowd**
  And the **Crowd Collection** is marked dirty

Scenario: Delete of a Linked Member removes it from that crowd but leaves it intact in others
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM invokes Delete on *"Shared Hero"* from **Crowd** *"Team Alpha"*
  Then *"Shared Hero"* no longer appears in **Crowd** *"Team Alpha"*
  And *"Shared Hero"* still appears in **Crowd** *"Team Beta"*
  And the **All Characters Crowd** still contains *"Shared Hero"*

Scenario: Deleting the last character leaves the Crowd present but empty
  Given **Crowd** *"Patrol Alpha"* contains exactly one **Character** *"Recruit 1"*
  When the GM invokes Delete on *"Recruit 1"*
  Then *"Recruit 1"* is removed
  And **Crowd** *"Patrol Alpha"* still appears in the **Crowd Tree** as an empty crowd
```

---

## Story: Clone Character

```gherkin
Scenario: Clone creates an independent copy with suffix naming immediately below the original
  Given **Crowd** *"Patrol Alpha"* contains **Character** *"Guard"*
  When the GM invokes Clone on *"Guard"*
  Then a new **Character** *"Guard (Copy)"* appears in **Crowd** *"Patrol Alpha"* immediately below *"Guard"*
  And the **All Characters Crowd** includes *"Guard (Copy)"*
  And modifying *"Guard (Copy)"* does not affect *"Guard"*
  And the **Crowd Collection** is marked dirty

Scenario: Clone of a Linked Member creates a copy only in the crowd where Clone was invoked
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM invokes Clone on *"Shared Hero"* from **Crowd** *"Team Alpha"*
  Then a new independent **Character** *"Shared Hero (Copy)"* appears only in **Crowd** *"Team Alpha"*
  And *"Shared Hero (Copy)"* is not linked into **Crowd** *"Team Beta"*
  And the original *"Shared Hero"* is unchanged
```

---

## Story: Cut Character to Clipboard

```gherkin
Scenario: Cut removes character from source crowd; Paste places it in the target crowd
  Given **Crowd** *"Team Alpha"* contains **Character** *"Recruit 1"*
  And **Crowd** *"Team Beta"* exists (initially empty)
  When the GM invokes Cut on *"Recruit 1"* in **Crowd** *"Team Alpha"*
  Then *"Recruit 1"* is removed from **Crowd** *"Team Alpha"* immediately
  And the **Crowd Collection** is marked dirty
  When the GM pastes into **Crowd** *"Team Beta"*
  Then *"Recruit 1"* appears in **Crowd** *"Team Beta"*
  And the **Clipboard** is cleared

Scenario: A second Cut before Paste discards the previously cut item permanently
  Given **Crowd** *"Team Alpha"* contains **Characters** *"Recruit 1"* and *"Recruit 2"*
  When the GM invokes Cut on *"Recruit 1"*
  And then invokes Cut on *"Recruit 2"* before pasting
  Then *"Recruit 1"* is permanently discarded (lost)
  And only *"Recruit 2"* is held on the **Clipboard**

Scenario: Cutting a Linked Member removes only the entry from the source crowd; links elsewhere are intact
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM invokes Cut on *"Shared Hero"* from **Crowd** *"Team Alpha"*
  Then *"Shared Hero"* no longer appears in **Crowd** *"Team Alpha"*
  And *"Shared Hero"* still appears as a **Linked Member** in **Crowd** *"Team Beta"*
```

---

## Story: Link Character across Crowds

```gherkin
Scenario: Link adds the character as a Linked Member with a link indicator in the target crowd
  Given **Character** *"Shared Hero"* exists in **Crowd** *"Team Alpha"*
  When the GM invokes Link on *"Shared Hero"* and selects **Crowd** *"Team Beta"* as the target
  Then *"Shared Hero"* appears in **Crowd** *"Team Beta"* as a **Linked Member** with a link indicator
  And the **Crowd Collection** is marked dirty

Scenario: Renaming a Linked Member from either crowd updates both appearances
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM renames *"Shared Hero"* to *"Apex"* from **Crowd** *"Team Beta"*
  Then **Crowd** *"Team Alpha"* also shows *"Apex"*
  And the **All Characters Crowd** reflects the new name *"Apex"*

Scenario: Linking a character into a crowd where it already exists is rejected
  Given **Character** *"Shared Hero"* already exists in **Crowd** *"Team Beta"*
  When the GM attempts to link *"Shared Hero"* into **Crowd** *"Team Beta"*
  Then the system rejects the operation and notifies the GM that the character already exists there
```

---

## Story: Clone-Link Character

```gherkin
Scenario: Clone-Link creates an independent deep-copy and links it between source and target crowds
  Given **Character** *"Base Model"* exists in **Crowd** *"Source Crowd"*
  When the GM invokes Clone-Link on *"Base Model"* and selects **Crowd** *"Target Crowd"*
  Then a new independent **Character** *"Base Model (Copy)"* appears in **Crowd** *"Source Crowd"*
  And *"Base Model (Copy)"* also appears as a **Linked Member** in **Crowd** *"Target Crowd"* with a link indicator
  And the original *"Base Model"* is unchanged in **Crowd** *"Source Crowd"*
  And the **All Characters Crowd** is updated to include *"Base Model (Copy)"*
  And the **Crowd Collection** is marked dirty
```

---

## Story: Flatten-Copy Crowd into Numbered Characters

```gherkin
Scenario: Flatten-Copy replaces each direct character member with an independent numbered deep-copy
  Given **Crowd** *"Generic Guards"* contains **Characters** *"Guard A"*, *"Guard B"*, *"Guard C"*
    all of which are fully independent (no linked members)
  When the GM invokes Flatten-Copy on *"Generic Guards"*
  Then each original character is replaced by an independently numbered copy (*"Guard A 1"*, *"Guard B 1"*, *"Guard C 1"*)
  And no two resulting characters share any state
  And the **All Characters Crowd** is updated to include the numbered copies and remove the originals
  And the **Crowd Collection** is marked dirty

Scenario: Flatten-Copy breaks links — numbered copies are independent from entries in other crowds
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  When the GM invokes Flatten-Copy on *"Team Alpha"*
  Then the flattened version of *"Shared Hero"* in *"Team Alpha"* is a new independent character
  And *"Shared Hero"* in **Crowd** *"Team Beta"* is unaffected

Scenario: Flatten-Copy leaves nested crowds in place — only direct character-level members are replaced
  Given **Crowd** *"Region North"* contains **Character** *"Patrol Lead"*
    and nested **Crowd** *"Patrol Alpha"* with **Character** *"Recruit 1"*
  When the GM invokes Flatten-Copy on *"Region North"*
  Then *"Patrol Lead"* is replaced by a numbered independent copy
  And nested **Crowd** *"Patrol Alpha"* and its **Character** *"Recruit 1"* are unchanged
```

---

## Story: Clone Memberships to Another Crowd

```gherkin
Scenario: Clone Memberships adds each direct member of the source crowd as a Linked Member in the target
  Given **Crowd** *"Source Crew"* contains **Characters** *"Crew Alpha"* and *"Crew Beta"*
  And **Crowd** *"Target Crew"* exists (initially empty)
  When the GM invokes Clone Memberships from *"Source Crew"* and selects *"Target Crew"*
  Then *"Crew Alpha"* and *"Crew Beta"* appear as **Linked Members** in *"Target Crew"*
  And **Crowd** *"Source Crew"* is unchanged
  And the **Crowd Collection** is marked dirty

Scenario: Members already in the target crowd are skipped — no duplicate members created
  Given **Crowd** *"Source Crew"* contains **Characters** *"Crew Alpha"* and *"Crew Beta"*
  And **Crowd** *"Target Crew"* already contains **Character** *"Crew Alpha"*
  When the GM invokes Clone Memberships from *"Source Crew"* to *"Target Crew"*
  Then *"Crew Beta"* is added as a **Linked Member** in *"Target Crew"*
  And *"Crew Alpha"* is not duplicated in *"Target Crew"*
```

---

## Story: Drag-Drop Character between Crowds

```gherkin
Scenario: Drag-drop moves the character to the target crowd and removes it from the source
  Given **Crowd** *"Team Alpha"* contains **Character** *"Recruit 1"*
  And **Crowd** *"Team Beta"* exists (initially empty)
  When the GM drags *"Recruit 1"* from *"Team Alpha"* and drops it onto *"Team Beta"*
  Then *"Recruit 1"* no longer appears in **Crowd** *"Team Alpha"*
  And *"Recruit 1"* appears in **Crowd** *"Team Beta"*
  And the **Crowd Collection** is marked dirty

Scenario: Drag-drop of a Linked Member moves only the entry from the source crowd
  Given **Character** *"Shared Hero"* is a **Linked Member** in **Crowd** *"Team Alpha"* and **Crowd** *"Team Beta"*
  And **Crowd** *"Team Gamma"* exists (initially empty)
  When the GM drags *"Shared Hero"* from *"Team Alpha"* and drops it onto *"Team Gamma"*
  Then *"Shared Hero"* no longer appears in **Crowd** *"Team Alpha"*
  And *"Shared Hero"* still appears as a **Linked Member** in **Crowd** *"Team Beta"*
  And *"Shared Hero"* appears in **Crowd** *"Team Gamma"*

Scenario: Drag-drop into a crowd where a member with the same name exists is rejected
  Given **Crowd** *"Team Alpha"* contains **Character** *"Recruit 1"*
  And **Crowd** *"Team Beta"* also contains a **Character** named *"Recruit 1"*
  When the GM drags *"Recruit 1"* from *"Team Alpha"* onto *"Team Beta"*
  Then the system rejects the drag-drop
  And *"Recruit 1"* returns to **Crowd** *"Team Alpha"*
  And the GM is notified of the name conflict
```

---

## Story: Filter Characters by Name

```gherkin
Scenario: Typing in the filter bar collapses the tree to matching nodes in real time
  Given **Crowd** *"Patrol Alpha"* contains **Characters** *"Guard"* and *"Sentry"*
  And **Crowd** *"Patrol Beta"* contains **Character** *"Ghost Rider"*
  When the GM types *"Gu"* in the filter bar
  Then the **Crowd Tree** shows **Crowd** *"Patrol Alpha"* expanded to reveal *"Guard"*
  And *"Sentry"* is hidden from the tree
  And **Crowd** *"Patrol Beta"* is hidden (no members match *"Gu"*)

Scenario: Clearing the filter bar restores all nodes to their prior expand/collapse state
  Given the filter bar currently shows *"Gu"* and the tree is filtered
  When the GM clears the filter bar
  Then the **Crowd Tree** restores all nodes to their prior expand/collapse state
  And all **Crowds** and **Characters** are visible again

Scenario: Filter with no matches shows an empty state message and keeps the clear button available
  Given the filter bar currently shows *"zzz"* with no matching characters in the repository
  Then the **Crowd Tree** shows an empty state message (*"No characters match"*)
  And the clear button is still available
```

---

## Story: Browse Crowds by Concept

### Scenario Outline: By Concept mode groups Crowds under their concept tag category nodes

**Crowd** (Given — above scenario):

| scenario      | crowdName       | conceptTag   |
| ------------- | ---------------- | ------------- |
| Tagged crowds | German Shepherds | Animals       |
| Tagged crowds | City Police      | Armed Forces  |
| Untagged      | Mystery Crowd    | (none)        |

```gherkin
Given **Crowd** {crowdName} exists in the repository with {conceptTag}
When the GM selects the By Concept **Browse Mode**
Then the **Crowd Tree** shows {crowdName} grouped under {conceptTag} node per **CrowdGrouping** below
```

**CrowdGrouping** (Then — below scenario):

| scenario      | conceptTag | crowdName       |
| ------------- | ------------- | ---------------- |
| Tagged crowds | Animals       | German Shepherds |
| Tagged crowds | Armed Forces  | City Police      |
| Untagged      | Uncategorized | Mystery Crowd    |

> `scenario` joins **Crowd** to **CrowdGrouping**. `conceptTag` of `(none)` means the Crowd has no concept tag set; it appears under the *Uncategorized* category node.

---

## Story: Browse Crowds by Gangs, Crews, and Squads

### Scenario Outline: Gangs/Crews/Squads mode groups Crowds by group type, hiding untagged Crowds

**Crowd** (Given — above scenario):

| scenario       | crowdName   | groupType |
| -------------- | ------------ | ---------- |
| Tagged groups  | Street Kings | gang       |
| Tagged groups  | Alpha Squad  | squad      |
| Tagged groups  | Patrol Alpha | (none)     |
| No tagged      | Solo Patrol  | (none)     |

```gherkin
Given **Crowd** {crowdName} exists in the repository with {groupType}
When the GM selects the By Gangs, Crews, and Squads **Browse Mode**
Then the **Crowd Tree** shows {crowdName} under {groupType} heading with {visible} per **BrowseResult** below
```

**BrowseResult** (Then — below scenario):

| scenario       | groupType | crowdName   | visible |
| -------------- | ----------- | ------------ | ------- |
| Tagged groups  | Gangs       | Street Kings | yes     |
| Tagged groups  | Squads      | Alpha Squad  | yes     |
| Tagged groups  | (none)      | Patrol Alpha | no      |
| No tagged      | (empty)     | (empty)      | no      |

> `scenario` joins both tables. `groupType` of `(none)` means no group type is set; that Crowd does not appear in this view. The `No tagged` scenario produces an empty state message: *"No gangs, crews, or squads defined"*.

---

## Story: Browse Crowds by COH Structure

### Scenario Outline: By COH Structure mode groups Crowds under COH faction hierarchy nodes

**Crowd** (Given — above scenario):

| scenario      | crowdName      | cohFactionTag  |
| ------------- | --------------- | ---------------- |
| Tagged factions | Council Empire  | Villain Groups   |
| Tagged factions | Freedom Phalanx | Hero Groups      |
| Untagged      | Generic Patrol  | (none)           |

```gherkin
Given **Crowd** {crowdName} exists in the repository with {cohFactionTag}
When the GM selects the By COH Structure **Browse Mode**
Then the **Crowd Tree** shows {crowdName} grouped under {cohFactionTag} node per **FactionGrouping** below
```

**FactionGrouping** (Then — below scenario):

| scenario        | cohFactionTag   | crowdName      |
| --------------- | -------------- | --------------- |
| Tagged factions | Villain Groups | Council Empire  |
| Tagged factions | Hero Groups    | Freedom Phalanx |
| Untagged        | Untagged       | Generic Patrol  |

> `scenario` joins both tables. `cohFactionTag` of `(none)` means no COH faction tag is set; that Crowd appears under the *Untagged* node.

---

## Story: Browse All Characters Crowd

```gherkin
Scenario: All Characters mode shows every character in the repository alphabetically
  Given **Crowd** *"Team Alpha"* contains **Character** *"Zeta"*
  And **Crowd** *"Team Beta"* contains **Characters** *"Alpha"* and *"Mu"*
  When the GM selects the All Characters **Browse Mode**
  Then the **Crowd Tree** shows the **All Characters Crowd** as the sole root entry
  And expanding it reveals *"Alpha"*, *"Mu"*, *"Zeta"* in alphabetical order

Scenario: A new character created while All Characters view is active appears in its sorted position
  Given the GM is viewing the All Characters **Browse Mode**
  And the **All Characters Crowd** currently lists *"Alpha"* and *"Zeta"*
  When the GM creates **Character** *"Mu"* in any crowd
  Then the **All Characters Crowd** immediately shows *"Alpha"*, *"Mu"*, *"Zeta"*

Scenario: Delete and rename are unavailable on the All Characters Crowd node itself
  Given the GM is viewing the All Characters **Browse Mode**
  When the GM attempts to delete or rename the **All Characters Crowd** node
  Then the system ignores the action
  And the **All Characters Crowd** is unchanged
```

---

## Notes for downstream tests

- Each `Then` is observable: a named file is written, not written, replaced, or byte-unchanged; a notification appears; a status message appears; the **Crowd Tree** shows or hides a named **Crowd** with given contents.
- No `Then` reads internal fields such as the dirty flag or the *source file* property. Those are proven indirectly through save / load behavior.
- **Plain Scenarios** use inline values in step prose — **bold** for domain concepts, *italic* for values. No Gherkin data tables.
- **Scenario Outlines** use `{scenario}` as the join key across relationship-based example tables — one table per domain concept, linked by FK columns, never collapsed into a wide row. Outlines are used when the same Given/When/Then steps apply across genuine data variation (different files, different crowd structures, different tag values).
- The choice of form follows the data-model complexity rule: when two or more interrelated domain concepts (CrowdFile → Crowd → Character, or Crowd → concept tag → grouping node) all vary across rows, an Outline with FK-linked tables is the default. When each scenario tests a distinct behavioral path (different action verb, different assertion type), plain Scenarios remain correct.
- *Saved positions* are intentionally absent from these scenarios; they round-trip through the same serializer and are covered by later position-related stories.
