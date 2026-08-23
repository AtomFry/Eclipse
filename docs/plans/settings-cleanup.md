# Plan — Settings: the defects and smells left after the refactor

**Status: complete, pending the manual sweep in `VER-CONFIG-018`…`021`.**

**D1 answered: option A** — everything defers to the settings window's Save. **D2 answered: leave
`double` alone**, per the recommendation; C7 is withdrawn rather than deferred.

**The stages were resequenced during implementation, and two of them dissolved.** Verifying the
plan before starting showed that the per-item custom-list CRUD had exactly two callers, both in
the editor, and that option A removes both — so Stage 5 deletes the very methods Stage 2 was
going to fix and Stage 4 was going to protect. Running them in plan order would have been wasted
work. Actual order and outcome:

| Stage | Outcome |
|---|---|
| 1 — naming and dead code | Done. Four dead members, the orphaned converter, the "patcher" naming. |
| 5 — one save model (D1: A) | Done, second. Editor works on a copy; edit, add, delete and reorder all commit on the settings window's Save. |
| 3 — provider split | Done, third. `CustomListDefinitionDataProvider` deleted; `EclipseSettingsDataProvider` reduced to the cached instance with a private setter. |
| 2 — throwing lookups | **Dissolved.** The methods containing them no longer exist. Re-checked: no id lookups remain in the settings path. |
| 4 — serialise read-modify-write | **Dissolved.** There is no read-modify-write pair left — the custom-list file is written whole. No semaphore needed. |

`CustomListDefinitionDataService` went from nine members to three:
`GetAllCustomListDefinitions`, `GetAllCustomListDefinitionsAsync`, `SaveCustomListDefinitionsAsync`.

Worth recording as a pattern: **two of the five findings were symptoms of the sixth.** C2 (lookups
that throw) and C5 (unsynchronised read-modify-write) both existed only because per-item writes
existed, and per-item writes existed only because the save model was inconsistent. Fixing the
model deleted the code rather than repairing it.

Follows `settings-refactor.md` (storage, saving, binding) and `settings-window-layout.md`
(tabs, rows, styling). This is the residue: what an end-to-end read found once those two had
landed and the code was small enough to see clearly.

## What was asked

A review pass over the settings subsystem for defects, duplication and suspicious
implementation. Every point below was re-verified against the code before being written down;
two of the original findings were wrong in detail and are corrected in place.

---

## Findings

### C1 — The custom-list editor mutates the settings window's own object — **worse than first reported**

`EclipseSettingsViewModel.OnEditExecute` passes `SelectedCustomListDefinition` — the live
instance in the grid's `ObservableCollection` — straight into the editor:

```csharp
customListDefinitionEditViewModel = new CustomListDefinitionEditViewModel(SelectedCustomListDefinition);
```

The editor never copies it. Every property writes through:

```csharp
public string Description
{
    get { return customListDefinition.Description; }
    set { customListDefinition.Description = value; OnPropertyChanged("Description"); }
}
```

**The correction:** the first report assumed the collections (filters, sorts, categories) were
only written on Save. They are not. They are written from **`CollectionChanged` handlers** —
`FilterExpressions_CollectionChanged`, `SortExpressions_CollectionChanged`,
`SelectedListGroups_CollectionChanged` — each of which clears the shared object's collection and
refills it the instant the user adds or removes a row.

So **Cancel in the editor reverts nothing at all**, scalar or collection. Closing the editor only
nulls the view references; it does not reload.

Two consequences:

* The grid immediately shows edits the user cancelled.
* Cancel the edit, then reorder a list, then Save. `SaveCustomListsIfChangedAsync` writes
  `CustomListDefinitions.ToList()` — the mutated objects — so **the cancelled edit is persisted**.

Note the asymmetry: `OnAddExecute` passes `new CustomListDefinition()`, so cancelling an *add*
loses nothing. Only *edit* aliases.

This is entangled with `OQ-017`, and together they give the custom-lists tab **three different
save semantics**: reorder is deferred to Save, delete is immediate, and edit is immediate-in-
memory-but-deferred-to-disk.

### C2 — Three lookups throw on an id that is not in the file

```csharp
CustomListDefinition existing = customListDefinitions.SingleOrDefault(f => f.Id == customListDefinition.Id);
int indexOfExisting = customListDefinitions.IndexOf(existing);   // -1 when null
customListDefinitions.Insert(indexOfExisting, customListDefinition);   // ArgumentOutOfRangeException
```

`SingleOrDefault` is written to tolerate a miss and the next line cannot.
`GetCustomListDefinitionByIdAsync` and `DeleteCustomListDefinitionAsync` use bare `Single`, which
throws on a miss *and* on a duplicate. Since `settings-refactor.md` Stage 3 these are caught and
reported, so the user gets a message rather than a vanished click — but it is a crash wearing a
dialog.

The insert-then-remove pair is also replace-in-place written backwards:

```csharp
customListDefinitions.Insert(indexOfExisting, customListDefinition);
customListDefinitions.Remove(existing);
```

### C3 — The provider layer is two different jobs sharing one type

`EclipseSettingsDataProvider` (67 lines) and `CustomListDefinitionDataProvider` (51 lines) are
almost entirely forwarders to their `*DataService.Instance`. But counting the call sites shows
the settings provider is not one thing:

| Member | Call sites | Who calls it |
|---|---|---|
| `.EclipseSettings` (cached instance) | **26**, across 16 files | The whole plugin — `GameList`, `GameCatalog`, `AttractModeService`, `KeyStrategyCache`, the states… |
| `.GetEclipseSettingsAsync()` | 1 | The settings editor |
| `.SaveEclipseSettingsAsync()` | 1 | The settings editor |

So it is **a process-wide settings cache with an editor's read/write API bolted to the side**, and
the 26 callers of the first have to look past the second. The cached property also has a
**public setter with zero call sites**, which lets anything in the process swap the settings
object every one of those 26 sites is holding.

The two providers are also inconsistent with each other: the settings one is a singleton, the
custom-list one is `new`ed at three sites — including `MainWindowViewModel.CreateGameLists`,
which is the Big Box runtime path, not the editor.

### C4 — Four dead async members — **corrected**

The original report named `CustomListDefinitionDataService.GetAllCustomListDefinitionsAsync` and
"the provider's `GetCustomListDefinitionByIdAsync`". Checked precisely:

| Member | Verdict |
|---|---|
| `EclipseSettingsDataService.GetEclipseSettingsAsync` | dead |
| `EclipseSettingsDataService.ReadFromFileAsync` | dead |
| `CustomListDefinitionDataService.GetAllCustomListDefinitionsAsync` | dead — the *provider* wraps the sync version in its own `Task.Run` instead |
| `GetCustomListDefinitionByIdAsync` | dead at **both** levels, provider and service |

The provider's `GetAllCustomListDefinitionsAsync` **is** used (`EclipseSettingsViewModel:442`) —
that part of the original claim was wrong.

Both layers grew a sync and an async form of everything, and only some of each pair was ever
wired up.

### C5 — Read-modify-write with no coordination

Every custom-list CRUD method reads the whole file, edits the list in memory and writes it back.
Two operations in flight and the second silently overwrites the first. Unlikely with a single
desktop window — but `JsonFileStore` now makes each *write* atomic while leaving the
read-modify-write *pair* unprotected, which is a slightly false sense of safety.

### C6 — `Task.Run` over synchronous file I/O

Every async path is a sync method wrapped in `Task.Run`. Honest and appropriate for a desktop
window that must not block on a small local file; worth knowing it is not real async I/O and
gains nothing but thread-hopping.

### C7 — `double` for values only ever set to whole numbers

`BoxFrontMargin{Left,Right,Top,Bottom}` and `SelectedGameDetailsPadding` are `double`.
`DefaultVideoVolume` is legitimately `double` (0–1 in 0.05 steps).

**But this one has a trap, and it changes the recommendation.** Until `settings-window-layout`
Stage 3b, none of these sliders snapped — so existing settings files in the wild can contain
values like `2.5`. Newtonsoft will read `3.0` into an `int` happily, and **throws on `2.5`**. A
type change is therefore a schema migration, not a tidy-up. See D2.

### C8 — An orphaned converter, and a doc line it silently falsified

`MillisecondsToSecondsConverter` is still declared in `Window.Resources` as `MsToSeconds` and
used **nowhere** — the `SliderRow` rewrite orphaned it.

Worse, `docs/features/configuration.md` still says the screen saver tab "displays them in seconds
via `MillisecondsToSecondsConverter`". That is now false, and it was false in a doc updated
during Stage 5 without noticing. The tab shows milliseconds.

### C9 — "Patcher" naming from a different product

Four occurrences, and it is not only comments: `CustomListDefinitionEditView.OnPatcherEditClose`
is a **method name**, referenced at its subscribe and unsubscribe. Plus two copies of
`// if the window is closed (null) and a patcher is selected (not null)` in
`EclipseSettingsViewModel`.

---

## Stages

Ordered so the risky product change is last and everything before it is mechanical.

### Stage 1 — Naming and dead code (C4, C8, C9)

Delete the four dead async members. Delete the `MsToSeconds` resource and, if nothing else uses
it, `Converters/MillisecondsToSecondsConverter.cs`. Rename `OnPatcherEditClose` to
`OnEditWindowCloseRequested` and fix the two "patcher" comments. Correct the
`MillisecondsToSecondsConverter` sentence in `configuration.md`.

Nothing behavioural. Do it first so later diffs are not carrying it.

*Verification:* build; open both windows; the Screen saver tab still reads correctly.

### Stage 2 — Make the lookups honest (C2)

Replace insert-then-remove with an indexed assignment, and make a missing id a reported outcome
rather than an exception:

```csharp
int index = customListDefinitions.FindIndex(f => f.Id == customListDefinition.Id);
if (index < 0)
{
    // it was deleted from under us - add it back rather than throwing
    customListDefinitions.Add(customListDefinition);
}
else
{
    customListDefinitions[index] = customListDefinition;
}
```

Same treatment for delete (a missing id means the work is already done — return) and for
`GetCustomListDefinitionByIdAsync` if it survives Stage 1.

*Verification:* `VER-CONFIG-018` below.

### Stage 3 — Split the provider by role (C3)

Not "delete the provider layer" — that would push 26 call sites onto a `*DataService.Instance`
and lose the one abstraction that is earning its keep. Split it by what it is actually doing:

* **`EclipseSettingsDataProvider`** keeps *only* the cached, process-wide `EclipseSettings`
  property that the 26 sites read. Make the setter `private`. This becomes a small type whose
  single job is stated in its name.
* The editor's two calls (`GetEclipseSettingsAsync`, `SaveEclipseSettingsAsync`) go straight to
  `EclipseSettingsDataService.Instance`, which is where the file I/O already lives.
* **`CustomListDefinitionDataProvider`** disappears: its three `new` sites call
  `CustomListDefinitionDataService.Instance` directly, matching how the settings side works.

Net effect: 118 lines of forwarding gone, one type per job, and the same singleton pattern on
both sides.

*Verification:* build; both windows; a Big Box launch, since 26 runtime sites read that property.

### Stage 4 — Serialise the custom-list read-modify-write (C5)

Give `CustomListDefinitionDataService` a private `SemaphoreSlim(1,1)` and take it around each
read-modify-write pair. Roughly fifteen lines. `EclipseSettingsDataService` does not need it —
its save is a whole-object write with no read step.

Deliberately **not** doing anything about C6. `Task.Run` over a small local file is the right
shape for a desktop window; converting to real async I/O would add `ConfigureAwait` discipline
and buy nothing measurable.

*Verification:* build; add, edit, delete and reorder in quick succession; the file stays coherent.

### Stage 5 — One save model for custom lists (C1, and `OQ-017`) — **needs D1**

The product change. Held to last because it is the only stage that alters what the user
experiences, and because the shape depends on the decision below.

*Verification:* `VER-CONFIG-019`…`021` below.

---

## Verification

| ID | Scenario | Stage |
|---|---|---|
| VER-CONFIG-018 | Open the custom-list editor, and while it is open delete that list's entry from `CustomLists.json` by hand. Save in the editor. Confirm a reported outcome, not a crash dialog. | 2 |
| VER-CONFIG-019 | Edit a custom list's description, filters, sorts and categories, then Cancel. Confirm the grid still shows the original values and that reordering and saving afterwards does not persist the cancelled edit. | 5 |
| VER-CONFIG-020 | Edit a custom list and Save. Confirm the change appears in the grid, survives a settings-window Cancel or Save per whichever model D1 chooses, and matches the file afterwards. | 5 |
| VER-CONFIG-021 | Delete a custom list, then Cancel the settings window. Confirm the outcome matches D1 — and that it is the *same* answer as for a cancelled reorder. | 5 |

---

## Risks

| Risk | Stage | Mitigation |
|---|---|---|
| A runtime site depends on the settings provider's method surface, not just the cached property. | 3 | The 26/1/1 split was counted, not assumed. Build plus a Big Box launch covers it. |
| Making the cached setter private breaks something that sets it. | 3 | Zero call sites today; the compiler catches any that appear. |
| Deleting the converter breaks a binding elsewhere in the theme. | 1 | Grepped: one declaration, no usages. Binding-error trace on the settings window. |
| Stage 5 changes muscle memory for anyone who has learned the current behaviour. | 5 | It is a product decision (D1), taken deliberately rather than drifted into. |

---

## Out of scope

* **C6** — `Task.Run` over file I/O, deliberately kept.
* `CustomListDefinitionEditView.xaml` (188 lines) has the same layout patterns the settings
  window just shed; worth the same treatment, but it is a separate piece of work.
* The composition root (`B-09`) — these providers stay singletons.

---

# Decisions needed

## D1 — What should Cancel mean for custom lists?

### Functionally

Three things can change a custom list, and today each one commits at a different moment:

| Action | When it reaches disk | Does Cancel undo it? |
|---|---|---|
| Reorder | On the settings window's Save | Yes |
| Delete | Immediately | No |
| Edit | On the editor's Save — but the in-memory object changes as you type | Partly, and inconsistently |

A user who edits a list, cancels, reorders, and saves gets their cancelled edit written to disk.
That is the defect. But fixing only the aliasing still leaves reorder and delete disagreeing, so
this is really "pick one model and apply it to all three".

### Technically

The editor is handed the live object from the grid's `ObservableCollection` and writes to it
directly — scalars from property setters, collections from `CollectionChanged` handlers. The
settings window avoids this for settings by loading a fresh `EclipseSettings` from disk, editing
that, and simply not saving on Cancel.

### Options

**Option A — Everything defers to the settings window's Save.**
The editor takes a copy; its Save hands the edited copy back to the parent, which replaces the
item in the collection; Cancel discards it. Delete stops writing immediately and instead marks
the list for removal. Nothing touches disk until the settings window saves.

* *For:* one rule for the whole window, matching how settings already behave. Cancel means
  cancel, everywhere. Closes `OQ-017`.
* *Against:* delete stops feeling immediate, which some users read as "did that work?". The
  parent has to track pending deletions, which is new state.

**Option B — Everything commits immediately.**
The editor writes on its own Save (as now, but from a copy so Cancel works). Delete stays
immediate. Reorder starts writing on each move rather than waiting.

* *For:* smallest change; delete keeps its current feel; no pending state to track.
* *Against:* the settings window's Cancel button then means "cancel the settings, but not the
  custom lists", which is hard to explain and is the thing `OQ-017` was raised about.

**Option C — Fix only the aliasing.**
Editor works on a copy. Reorder stays deferred, delete stays immediate. `OQ-017` stays open.

* *For:* smallest possible change, fixes the data-loss path, no product decision needed.
* *Against:* leaves two of the three semantics disagreeing; we will be back here.

**Recommendation: A.** It is the only one where the Cancel button means the same thing
everywhere, and the extra state is one list of pending deletions. B is defensible if you think
delete should stay instant; C only if you want the defect gone and the inconsistency parked.

## D2 — Should the margins and padding become `int`?

### Functionally

Five settings are stored as decimals for values a user can now only set to whole numbers. Nobody
sees the type — it surfaced only as number-formatting fiddliness, which the `SliderRow` rewrite
has already dealt with.

### Technically

Changing `double` to `int` is a schema migration, not a rename. Those sliders did not snap until
recently, so a file in the wild can hold `2.5`. Newtonsoft reads `3.0` into an `int` without
complaint and **throws** on `2.5`. With `JsonFileStore`'s new fallback, that throw is no longer
fatal — but it would silently drop the user's whole settings file back to the newest backup or
to defaults, which is worse than a wrong type.

Doing it safely means using the `SchemaVersion` stamp for real: read version 1 as `double`,
round, write version 2. That is the first actual migration, built for the least valuable change
available.

### Options

**Option A — Leave them `double`.** The formatting problem is already solved; the type is
cosmetically wrong and harmless.

**Option B — Change to `int` with a real migration.** Correct types, and it proves the
`SchemaVersion` mechanism on something low-stakes.

**Option C — Change to `int` with no migration.** Cheap, and silently resets the settings of any
user with a fractional margin.

**Recommendation: A, and I would drop the finding.** I raised it as a nice-to-have; investigating
it showed the cost is a schema migration and the benefit is a type nobody sees. C is not
acceptable. B is defensible only if you want a rehearsal for a migration you know is coming
anyway — in which case it should be scheduled as *that*, not as this.
