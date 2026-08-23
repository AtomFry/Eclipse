# Plan — Settings you can trust

**Status: all stages built and green, pending a manual verification sweep.** Approved as
"Option 1" of the next-refactor options. Stage 0 was dropped before implementation — see the
note under Stages.

**What actually landed**

| Stage | Outcome |
|---|---|
| 1 — split the four-type file | Done. `CustomListDefinitionDataProvider.cs` (417 lines, four types) is now four files. |
| 2 — safe persistence | Done, and wider than planned: the custom-list file shares the same failure mode, so both now go through `Helpers/JsonFileStore`. |
| 3 — reportable failures | Done. Six `async void` handlers, plus the unawaited custom-list write, plus two more unawaited calls in `OnAddExecute`/`OnEditExecute` that the plan had not found. |
| 4 — detach subscriptions | Done. The measurement step was skipped: the fix is the same whichever way the Prism weak-reference question resolves, and Stage 6 replaced the mechanism anyway. |
| 5 — collapse the triplication | Done. 41 of 45 delegating pairs deleted; the four box-front margins kept, because their setters refresh the preview. |
| 6 — remove Prism | Done. `Prism.dll` is no longer in the shipped payload. |
| 7 — documentation | Done. |

**Deviation worth recording:** Stage 5 was planned to give `EclipseSettings`
`INotifyPropertyChanged`. It does not. Doing so would have meant 46 backing fields on the
persistence model — trading view-model boilerplate for model boilerplate — and it is only
needed by the margin preview. Keeping four wrappers for the four properties that need them
costs 40 lines and leaves the model a plain serialisable object.

Addresses `B-10`, `B-13`, `B-25`, `B-26`, `B-27` and the EPIC-CONFIG share of `B-23`. Resolves
findings `S-5`, `S-6`, `S-13`, `M-4`, `C-7`, `C-8`, and answers `OQ-011` — see S8, which turned
out to be a code-reading exercise rather than the research task this plan originally scheduled.

## What was asked

Make Eclipse's configuration trustworthy, maintainable and extensible, without disturbing the
Big Box runtime. Tests are out of scope by decision; verification is by hand and by
development-time probes.

## Why this area, and why now

Configuration is the densest concentration of debt in the product and the least risky place to
work on it. **Every one of the types involved lives in the LaunchBox desktop windows, not the
Big Box runtime.** Prism appears in six files, every one of them a settings window or its event
helper — `Event/Event.cs` is literally commented *"events for eclipse settings views"*. Nothing
in browsing, voice search, attract mode or launching a game can break as a consequence of this
work, and verification is a person opening a window and changing things.

The one exception is Stage 5, which reaches into `MainWindowViewModel` and the theme's
bindings. It is called out separately.

---

## How it works today

```
EclipseSettings.json  ──read──►  EclipseSettingsDataService (singleton, file I/O)
                                          │
                                          ▼
                                 EclipseSettingsDataProvider (singleton, caches one copy)
                                          │
                        ┌─────────────────┴──────────────────┐
                        ▼                                    ▼
        EclipseSettingsViewModel                    the running plugin
        (45 delegating property pairs)      (MainWindowViewModel mirrors 6 of them;
                        │                    the rest are read once and cached in
                        ▼                    KeyStrategyCache, AttractModeService,
        EclipseSettingsView.xaml             GameList, MainWindowView.xaml.cs …)
        (940 lines of controls)
```

Two windows edit configuration: `EclipseSettingsView` (the main editor) and
`CustomListDefinitionEditView` (one custom list). Both run in LaunchBox, not Big Box. They talk
to each other through four Prism `PubSubEvent` types.

---

## Findings

### S1 — The settings code lives in a file named after something else

`Service/CustomListDefinitionDataProvider.cs` is 417 lines holding **four** public types:

| Type | Lines |
|---|---|
| `CustomListDefinitionDataProvider` | 12 |
| `CustomListDefinitionDataService` | 52 |
| `EclipseSettingsDataProvider` | 248 |
| `EclipseSettingsDataService` | 304 |

Everything about how settings are read, written and backed up is in a file whose name says
"custom list definition". This is `C-8`.

### S2 — Every setting is written down three times

* `Models/EclipseSettings.cs` — **46** properties, each with a `[DefaultValue]`.
* `EclipseSettingsViewModel` — **45** delegating pairs; the getter reads the model, the setter
  writes the model and notifies.
* `MainWindowViewModel` — **6** of them again, same pattern, for live binding in the theme.

Adding one setting today means: a property plus `[DefaultValue]` on the model, a delegating pair
in the settings view model, a line in the initialiser (S3), a control in 940 lines of XAML, and —
if the theme needs it live — a fourth copy in `MainWindowViewModel`. Forgetting any of them
fails silently. This is `S-5`.

### S3 — The initialiser is forty-four lines of self-assignment

`InitializeEclipseSettingsAsync` reads each value off the model and writes it straight back
through a property whose getter reads *the same model*:

```csharp
eclipseSettings = await EclipseSettingsDataProvider.Instance.GetEclipseSettingsAsync();

DefaultListCategoryType = eclipseSettings.DefaultListCategoryType;   // reads model, writes model
DisableVideos           = eclipseSettings.DisableVideos;             // …44 times
```

The net effect of the whole block is to raise `PropertyChanged` — plus, for exactly four of them
(the box-front margins), a call to `updateMarginSample()`. **Any refactor here must preserve that
side effect**; it is the only thing in the block that does real work.

Worth noting that this codebase has form: a self-assignment bug in `StringLengthConverter` is one
of the two latent bugs cited in `composition-root.md` as evidence for the test seam.

### S4 — Saving is not atomic, and its failures are unreportable

```csharp
private void SaveToFile(EclipseSettings eclipseSettings)
{
    BackupDataFile();
    string json = JsonConvert.SerializeObject(eclipseSettings, Formatting.Indented);
    File.WriteAllText(EclipseSettingsFile, json);   // not atomic
}
```

`File.WriteAllText` truncates and rewrites in place. A crash, a power cut or a full disk part-way
through leaves a truncated JSON file. There is no schema version, so there is no hook for a future
migration either. This is `S-6` and `B-27`.

Above it, `OnSaveExecute` is `async void`, so nothing can observe a failed write — and there is a
second defect in the same six lines:

```csharp
private async void OnSaveExecute()
{
    SaveCustomListsIfChanged();     // async void — started, never awaited
    await EclipseSettingsDataProvider.Instance.SaveEclipseSettingsAsync(eclipseSettings);
    OnCloseExecute();               // window closes, custom-list write may still be in flight
}
```

`SaveCustomListsIfChanged` is itself `async void`, so `OnSaveExecute` cannot wait for it. Reordering
custom lists and clicking Save races the custom-list write against the window closing. This is
`S-13`/`B-25` plus one defect the finding did not record.

There are six `async void` handlers in total: four in `EclipseSettingsViewModel`, one in
`CustomListDefinitionEditViewModel`, one in `EclipseSettingsView.xaml.cs`.

### S5 — A corrupt settings file takes the plugin down, and the backups are never read

`ReadFromFile` has no error handling. `JsonConvert.DeserializeObject` on a truncated file throws,
and `EclipseSettingsDataProvider.Instance.EclipseSettings` is reached from field initialisers
(`GameList`), constructors (`AttractModeService`) and the loading path. The exception surfaces
somewhere far from its cause.

Backups are written on every save — and nothing has ever read one.

### S6 — Backups are unbounded

`BackupDataFile` copies the file to `EclipseSettings_yyyyMMdd_H_mm_ss.json` on **every** save,
forever, with no pruning.

### S7 — Subscriptions are attached and never detached

Four `Subscribe` calls, zero `Unsubscribe`, across two view models and two windows that a user
opens and closes repeatedly.

Prism's `PubSubEvent.Subscribe` defaults to a **weak** reference, so this is not automatically a
memory leak, and the finding `M-4` may overstate it. The observable risk is different: while a
previous window instance is still alive, a published event is delivered to it *as well as* to the
current one. Stage 4 confirms empirically which of the two it is before changing anything.

### S8 — No setting is live. All of them apply at the next Big Box start. (`OQ-011`, answered)

`OQ-011` asks which settings apply live and which need a restart, and `UNRESOLVED.md` names it the
highest-value open question in the product. This plan originally scheduled a day of empirical
work to answer it. That was wrong — it falls out of the code:

1. **The editor does not exist in Big Box.** `EclipseSettingsMenuItem` declares
   `ShowInBigBox => false`, `ShowInLaunchBox => true`. Settings are edited in LaunchBox.exe, a
   different process from BigBox.exe.
2. **Big Box reads settings once.** `EclipseSettingsDataProvider.EclipseSettings` caches on first
   access and never re-reads.
3. **Nothing writes that cache.** The provider exposes a public setter; it has zero call sites.
4. **There is no reload path** — no `FileSystemWatcher`, no reload, no refresh.
5. **`MainWindowViewModel.InitializeEclipseSettings()` runs once**, from the constructor.

A change saved in LaunchBox therefore cannot reach a running Big Box. There is no mechanism for
it to.

The premise the open question rests on — *"six are mirrored for live binding"* — is a misreading.
Each of those six is assigned exactly twice: once on the constructor path and once inside its own
setter. Nothing calls the setters afterwards. They are mirrored so the theme XAML has a binding
target on the view model, not because anything updates them. "Live binding" names the WPF
mechanism, not an observable behaviour.

A coverage pass alongside it found every one of the 45 settings has at least one consumer outside
the editor. There are no dead settings.

**What this changes:** the user needs one sentence, not a per-setting marker — *changes take
effect the next time Big Box starts* — and Stage 5 loses the risk that dominated it.

### S9 — Prism buys four events and a command class

| Prism type | Used for |
|---|---|
| `PubSubEvent` ×4 | `EclipseSettingsClose`, `CustomListDefinitionSaved`, `CustomListDefinitionEditClosing`, `CustomListDefinitionEditClose` |
| `DelegateCommand` | 19 commands across the two settings view models |
| `EventAggregator` | one singleton in `EventAggregatorHelper` |

That is the entire dependency. `launchbox-integration.md` records `C-7`: Prism does not resolve in
the host load context, so the assembly loads on a partial type list. We ship a NuGet dependency
that is known not to load cleanly, for four events and a command base class.

---

## Proposal

One declaration per setting; writes that cannot corrupt; failures the user is told about; and an
answer to "does this need a restart?" that is both documented and visible in the editor.

Seven stages, ordered lowest-risk-first, each independently buildable and shippable.

> **Stage 0 removed.** This plan originally opened with a day-long live-vs-restart audit. Reading
> the code answered it in ten minutes — see S8 — so the audit was dropped and its conclusion
> folded into the findings. Stage 5 was gated on it and no longer is.

---

## Stages

### Stage 1 — Split the four-type file (`B-10`, `C-8`)

Pure move, no logic change. `Service/CustomListDefinitionDataProvider.cs` becomes four files:

```
Service/CustomListDefinitionDataProvider.cs
Service/CustomListDefinitionDataService.cs
Service/EclipseSettingsDataProvider.cs
Service/EclipseSettingsDataService.cs
```

First because every later stage edits these types, and doing this first keeps those diffs
readable.

*Verification:* build; open the settings window; save; confirm the file is written.

---

### Stage 2 — Make persistence safe (`B-27`)

Four changes inside `EclipseSettingsDataService`, all local:

1. **Atomic write.** Serialise to a temp file in the same directory, flush, then replace the
   target. A failure part-way leaves the original intact.
2. **Schema version.** Add a `SchemaVersion` property with a `[DefaultValue]`, so a future
   migration has a hook. Nothing reads it yet; that is the point.
3. **Bounded backups.** Keep the N most recent (proposed: 10) and prune the rest after a
   successful write.
4. **Survivable read.** Wrap `ReadFromFile`: on a parse failure, log it, try the most recent
   backup, and fall back to defaults if that fails too. Never throw out of the provider — the
   getter is reached from field initialisers, and an exception there is unrecoverable and
   unattributable.

Point 4 is the one that turns an existing unused mechanism into a real one: we have been writing
backups for years and never reading them.

*Verification:* hand-corrupt `EclipseSettings.json`, confirm Big Box starts on defaults and logs
the reason; confirm the newest backup is preferred over defaults; save 15 times and confirm 10
backups remain; interrupt a write and confirm the original survives.

---

### Stage 3 — Make failures reportable (`B-25`, plus the unawaited save)

* Convert the six `async void` handlers to `async Task` where the caller can await them.
* Where the signature must stay `void` (command handlers), wrap the body in try/catch and report
  via `MessageDialogHelper.ShowOKDialog`, which already exists.
* **Fix `OnSaveExecute` to await the custom-list write** before closing the window (S4).
* A failed save must leave the window open, so the user can retry rather than losing their edits.

*Verification:* make the settings file read-only, click Save, confirm a dialog appears and the
window stays open. Reorder custom lists, save, confirm the reorder survives a restart.

---

### Stage 4 — Detach the subscriptions (EPIC-CONFIG share of `B-23`)

**Confirm before fixing.** Log each handler invocation with the instance's hash code, open and
close the settings window twenty times, then trigger a publish and count the invocations. That
distinguishes "harmless weak references" from "delivered to three dead view models".

Then, whatever the answer, give both windows a symmetric detach on close. Subscriptions are the
kind of thing that should be symmetric whether or not the asymmetry currently costs anything.

*Verification:* the instrumented count is 1 after twenty open/close cycles.

---

### Stage 5 — Collapse the triplication (`B-26`)

The headline change, and the only one that touches the Big Box runtime.

**Design.** `EclipseSettings` becomes the single declaration and implements
`INotifyPropertyChanged`. The 45 delegating pairs are deleted from `EclipseSettingsViewModel`,
which instead exposes the model as one property:

```xml
<!-- before -->  IsChecked="{Binding EnableVoiceSearch, Mode=TwoWay}"
<!-- after  -->  IsChecked="{Binding Settings.EnableVoiceSearch, Mode=TwoWay}"
```

The 44-line initialiser (S3) collapses to assigning `Settings` and raising one notification. The
four `updateMarginSample()` side effects move to a `Settings.PropertyChanged` handler in the view
model that fires when one of the four margin properties changes.

The same treatment removes the six mirrored properties from `MainWindowViewModel`.

**Cancel semantics are preserved.** `InitializeEclipseSettingsAsync` already loads a *fresh*
instance from disk rather than the provider's cached copy, so the editor works on a detached
object and Cancel discards it by simply not saving. Binding to that object directly does not
change this.

**`INotifyPropertyChanged` is only needed for the editor.** Nothing in the theme ever observes a
settings change at runtime (S8), so the Big Box side needs no notification at all. The interface
exists so the editor's margin preview updates as the user types.

**The remaining risk is silent binding breakage.** Binding paths change in two settings windows
*and* the main theme, and WPF binding failures do not error — a broken path renders an empty
control. Mitigations, all required:

* Enable WPF binding-error tracing and sweep both windows and the theme; the trace must be clean.
* Use the probe-and-diff practice: dump the deserialised settings object before and after, diff.
* Do the two settings windows and `MainWindowViewModel` as **three separate commits**, each built
  and swept, so a regression is attributable.

*Verification:* `VER-CONFIG-003`, `VER-CONFIG-004`, `VER-CONFIG-005`; clean binding trace.

---

### Stage 6 — Remove Prism (`B-13`, `C-7`)

With Stage 5 done, what is left of Prism is four events and 19 commands.

* Replace `DelegateCommand` with a ~20-line `RelayCommand` in `Helpers`.
* Replace the four `PubSubEvent` types with plain .NET events on a small settings-scoped
  publisher, which also makes Stage 4's detach explicit rather than a framework detail.
* Delete the `Prism.Core` `PackageReference` and `Prism.dll` from the shipped payload.

Removes a dependency the integration notes record as not resolving in the host load context, and
drops one assembly from what we ship.

*Verification:* build; exercise every command in both windows; confirm `Prism.dll` is absent from
the staged payload.

---

### Stage 7 — Documentation

* `docs/features/configuration.md` — retire `S-5`, `S-6`, `S-13`, `M-4`, `C-8`; add the
  live-vs-restart answer from S8.
* `docs/features/launchbox-integration.md` — retire `C-7`.
* `docs/TRACEABILITY.md` — mark `B-10`, `B-13`, `B-25`, `B-26`, `B-27` and the EPIC-CONFIG share
  of `B-23` delivered.
* `docs/UNRESOLVED.md` — close `OQ-011`.
* This file — status to complete, with what was actually found.

---

## Verification

Existing scenarios that must still pass: `VER-CONFIG-001` … `VER-CONFIG-005`.

New scenarios this work should add to `VERIFICATION.md`:

| ID | Scenario | Stage |
|---|---|---|
| VER-CONFIG-006 | Corrupt the settings file; confirm Eclipse starts on the newest backup and logs why. | 2 |
| VER-CONFIG-007 | Interrupt a save; confirm the previous settings file is intact and parseable. | 2 |
| VER-CONFIG-008 | Save 15 times; confirm exactly 10 backups remain, newest kept. | 2 |
| VER-CONFIG-009 | Make the settings file read-only; save; confirm the user is told and the window stays open. | 3 |
| VER-CONFIG-010 | Reorder custom lists and save; confirm the order survives a restart. | 3 |
| VER-CONFIG-011 | Open and close the settings window 20×; confirm one handler invocation per publish. | 4 |
| VER-CONFIG-012 | Change a setting in LaunchBox with Big Box running; confirm it takes effect only after Big Box restarts. | 5 |

---

## Risks

| Risk | Stage | Mitigation |
|---|---|---|
| **Silent binding breakage** — a renamed path renders an empty control instead of erroring. | 5 | Binding-error trace must be clean; three separate commits; manual sweep of both windows and the theme. |
| **`EclipseSettings` implementing `INotifyPropertyChanged` changes serialisation.** | 5 | Json.NET ignores events, but confirm with a byte-diff of a saved file before and after. |
| **The fallback-to-defaults path hides a real problem** — a user runs for months on defaults without noticing. | 2 | Log loudly, and surface it in the settings window on next open. |
| Atomic-write behaviour differs on a network or non-NTFS path. | 2 | Fall back to write-then-move if `File.Replace` is unsupported; the settings folder is beside the plugin, so this is unlikely but cheap to guard. |

---

## Out of scope

* **Anything in the Big Box runtime path** other than the six mirrored properties in Stage 5.
* **Redesigning the settings window** — layout, grouping and wording are untouched. This is a
  debt exercise; a user should not notice a different editor, only a more reliable one.
* **A test project** (`B-01`) — excluded by decision. The probe-and-diff practice in Stage 5 is
  the substitute.
* **Dependency injection** (`B-09` / `composition-root.md`) — the singletons here stay singletons.
* **`B-32`** (source-generated `PropertyChanged`) — Stage 5 deletes most of the hand-written
  notification this would have generated, so re-evaluate afterwards; it may no longer be worth
  doing.

---

## Decisions needed before implementation starts

1. **Backup retention.** Proposed: keep the 10 most recent. Alternative: keep by age.
2. **What the user is told when settings fail to load.** Proposed: log, fall back to the newest
   good backup, and show a one-time notice next time the settings window opens — there is no good
   surface for it inside Big Box.
3. **How the editor says that changes need a restart.** Proposed: one line in the window, not a
   per-setting marker - no setting is live (S8), so 45 identical markers would say nothing.
4. **Stage 5 binding style.** Proposed: bind to `Settings.X` on the model. The alternative — keep
   generated view-model properties — needs a source generator we do not currently take.
