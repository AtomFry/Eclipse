# Eclipse — Unresolved

Two inventories that must not be lost during modernization:

1. **Suspected dead or obsolete code** — with the evidence needed before deleting it.
2. **Open questions** — behaviour the repository does not establish. These are *not*
   guessed at.

Modernization should shrink both lists. Nothing here should be acted on without the
verification named against it.

---

## 1. Suspected dead or obsolete

Confidence reflects how certain we are that the item is genuinely unused — **not** how
safe deletion is.

| ID | Location | Suspected purpose | Evidence | Feature | Confidence | Verify before deleting |
|---|---|---|---|---|---|---|
| DEAD-001 | `Resources/_DefaultBezelHorizontal.png` | Superseded bezel art | Not in the csproj `<Resource>` list; not referenced by `ResourceImages`; underscore prefix suggests deliberate retirement | none | **High** | Grep `.cs` and `.xaml` for the filename; `pack://` URIs are strings the compiler cannot check |
| DEAD-002 | `Resources/_VoiceRecognitionGif.gif`, `__VoiceRecognitionGif.gif` | Earlier versions of the listening animation | Same as above; the live one is `VoiceRecognitionGif.gif` | EPIC-SEARCH | **High** | Same |
| DEAD-003 | `Resources/EclipseSettingsIcon.bmp` | Predecessor of `EclipseSettingsIcon1.png` | Declared as `<None>`, not `<Resource>`; the resx references the `.png` | EPIC-CONFIG | **High** | Confirm the resx entry resolves to the png only |
| DEAD-004 | `using System.Data;` in `MainWindowViewModel.cs` | Leftover | No `DataTable`/`DataSet` usage anywhere | none | **High** | Build |
| DEAD-005 | `GameList.Brush`, `GameList.Confidence` display use | Voice-search confidence colouring in an earlier UI | `Confidence` is still *written* during voice search and used for ranking; `Brush` appears unbound | EPIC-SEARCH / EPIC-PRESENT | **Medium** | Search XAML for a binding to `Brush`. **`Confidence` is live — do not remove it.** Only `Brush` is a candidate |
| DEAD-006 | `Models/Option.cs` — the non-generic `Option` class | Predecessor of `Option<T>` | `Option<T>` in `GameDetailOptionList.cs` is the type actually used by `OptionList` | EPIC-BROWSE | **Medium-High** | Confirm no reference to the non-generic type |
| ~~DEAD-007~~ | ~~`Eclipse.deps.json` in the deployed plugin~~ | Dependency resolution hint | LaunchBox has no `AssemblyDependencyResolver`, so it was never read | EPIC-INTEGRATE | — | **RESOLVED** — removed from the payload. Note the deploy step does not prune, so existing installs keep a stale copy until deleted by hand |
| DEAD-008 | `Helpers/MessageDialogHelper.cs` | Modal dialogs for the settings UI | Small helper; usage appears limited to the custom-list editor | EPIC-CONFIG | **Low** | Grep for `ShowOKCancelDialog` / `ShowOKDialog` before assuming |
| DEAD-009 | `Eclipse/Unbroken.LaunchBox.Plugins.dll` (repo root of project) | Historical netstandard2.0 plugin contract | Deliberately retained as Milestone 0 reference material; no longer referenced by the build | none | **High** (unused by build) | **Do not delete** — it is the historical baseline reference |
| DEAD-010 | `Eclipse/LaunchBox/Themes/Eclipse/BigBoxTheme.csproj` + `.sln` | Design-time IntelliSense shim for theme XAML | Not in the Eclipse solution; targets netcoreapp3.1; references LaunchBox assemblies that no longer all exist (`Caliburn.Micro`, `System.Windows.Interactivity` absent from LB14) | EPIC-INTEGRATE | **Medium** | Confirm no developer relies on it for theme editing. It is shipped to users' Themes folder, which is itself questionable |
| DEAD-011 | `Eclipse/LaunchBox/Themes/Eclipse/9.4.5.txt`, `10.0.txt` | LaunchBox theme version markers | Convention files for older LaunchBox versions | EPIC-INTEGRATE | **Low** | LaunchBox may still read these to determine theme compatibility. **Do not remove without checking LaunchBox theme docs** |
| DEAD-012 | `GameFieldType` enum in `EclipseSettings.cs` | Typing for custom-list field editing | Declared but its use in the editor is unclear | EPIC-CONFIG | **Low** | Trace the custom-list editor's field-type handling |

### Deliberately *not* listed as dead

* **The `Media/Bezels/<platform>/` folders that ship empty** (Arcade, Nintendo 64, Sega
  Saturn, …) are intentional extension points — users drop their own bezels in.
* **`Eclipse.txt`-era log entries referencing `C:\Users\Adam\`** are historical artifacts
  in a stray root-level file, not code.
* **`ListCycle` window size of 13** is used, not dead — see `RULE-BROWSE-015`.

---

## 2. Open questions

Behaviour the repository does not establish. Each needs a product decision or an
experiment, not a guess.

### OQ-001 — Should a game appear multiple times in one list set?
**Feature:** FEAT-BROWSE-001 · **Evidence:** `GameCatalog.BuildCategoryIndex`, `RULE-BROWSE-001`
A game with three genres appears in three genre lists. **Interpretations:** (a) intended —
that is what browsing by genre means; (b) acceptable side effect of the index design.
**Observed:** it happens and looks correct. **Why it matters:** `B-30` may restructure the
index; if (a), multiplicity is a contract. **Resolve by:** product confirmation.

### OQ-002 — Should random game be weighted by list size?
**Feature:** FEAT-BROWSE-007 · **Evidence:** `DoRandomGame`, `RULE-BROWSE-011`
Random picks an index across the whole set, so games in big lists are likelier.
**Interpretations:** (a) intended — uniform across *games*; (b) accidental — the user may
expect uniform across *lists*. **Observed:** weighted by list size. **Why it matters:**
random game is a headline feature. **Resolve by:** product decision. Attract mode
(`RULE-ATTRACT-003`) now genuinely picks uniformly across games; note that when this question
was written the contrast drawn here did not actually hold - attract mode was itself weighted
by clone count until the index refactor. `DoRandomGame` is unchanged and remains weighted by
list size, so the open question stands.

### OQ-003 — Is "more like this" ordering intentional?
**Feature:** FEAT-BROWSE-008 · **Evidence:** `DoMoreLikeCurrentGame`, `RULE-BROWSE-012`
Lists are appended in a fixed order (series, genre, platform, developer, publisher, play
mode, year) with no deduplication and no relevance weighting. **Interpretations:** (a) the
order *is* the relevance model; (b) it is simply the order the code was written in.
**Why it matters:** any future ranking work needs to know. **Resolve by:** product decision.

### OQ-004 — Why is the browsing window 13 slots?
**Feature:** FEAT-PRESENT-001 · **Evidence:** `new ListCycle<GameMatch>(MatchingGames, 13)`
13 = 1 previous + 12 visible, presumably matched to the XAML grid. **Why it matters:**
`B-31` would make it configurable; without knowing the constraint, a wrong value breaks
layout. **Resolve by:** derive from the XAML grid definition and record it.

### OQ-005 — What should the user see when voice search is unavailable?
**Feature:** FEAT-SEARCH-002 · **Evidence:** `VoiceRecognitionState.DoRecognize` null guard
Currently nothing happens at all. **Interpretations:** (a) deliberate quiet failure in a
fullscreen UI; (b) an oversight. **Observed:** silent. **Why it matters:** this is `B-04`,
and it is a product decision about how loud failures should be. **Resolve by:** product
decision. Recommendation: loud in the log, quiet-but-present in the UI.

### OQ-006 — Is the noise-word list complete/correct?
**Feature:** FEAT-SEARCH-001 · **Evidence:** `GameTitleGrammar.IsNoiseWord`
The list includes `b`, which is unusual, and omits common words like `for` and `with`.
**Interpretations:** (a) empirically tuned against a real library; (b) ad hoc.
**Why it matters:** changing it changes recognition results. **Resolve by:** treat as
frozen and characterize (`VER-SEARCH-001`); revisit only with recognition data.

### OQ-007 — Is the 1-second selection settle delay tunable?
**Feature:** FEAT-PRESENT-002 · **Evidence:** hard-coded `new Timer(1000)`
Video delay is configurable but this is not. **Interpretations:** (a) deliberate constant;
(b) simply never exposed. **Why it matters:** it is the single biggest contributor to
perceived responsiveness. **Resolve by:** product decision on whether to expose it.

### OQ-008 — Should flip-box state persist across navigation?
**Feature:** FEAT-PRESENT-005 · **Evidence:** `KeyStrategyFlipBox` mutates the game's media
Flipping swaps the stored front/back media for that game, so it persists until flipped
back or media is re-resolved. **Interpretations:** (a) intended per-game toggle;
(b) accidental — mutating cached media as a view concern. **Why it matters:** `B-12` moves
media onto an owned model and may not preserve mutability. **Resolve by:** product decision.

### OQ-009 — Why is the bezel aspect-ratio threshold 1.7?
**Feature:** FEAT-MEDIA-008 · **Evidence:** `RULE-MEDIA-024`
1.7 is just below 16:9 (1.778). **Interpretations:** (a) deliberately catches 16:10 as
well; (b) an approximation of 16:9. **Why it matters:** `B-20` consolidates bezel logic.
**Resolve by:** record as-is and characterize; do not "correct" it to 1.778.

### OQ-010 — What is the intended image-cache invalidation strategy?
**Feature:** FEAT-MEDIA-009 · **Evidence:** `RULE-MEDIA-002`, `RULE-MEDIA-003`
Cache entries are never invalidated — if the user replaces artwork in LaunchBox, the
stale scaled copy is used indefinitely. **Interpretations:** (a) known limitation;
(b) unnoticed. **Observed:** stale art persists. **Why it matters:** a real user-facing
limitation with no workaround short of deleting the cache folder. **Resolve by:** product
decision — likely a genuine bug worth its own item.

### OQ-011 — Which settings apply live and which require a restart?
**Feature:** FEAT-CONFIG-001 · **Evidence:** `RULE-CONFIG-004`, the six mirrored
properties in `MainWindowViewModel`, `RULE-INPUT-008`
Some settings are read once and cached; six are mirrored for live binding. There is no
authoritative list. **Why it matters:** `B-26` collapses the triplication and could
silently make restart-only settings live. **Resolve by:** **empirical research task** —
change each of the 45 settings with Big Box running and record which take effect. This
should be its own backlog item; it is a prerequisite for `B-26`.

### OQ-012 — What race does the 500 ms stop-loop compensate for?
**Feature:** FEAT-LAUNCH-004 · **Evidence:** `StopVideoAndAnimationHandler`; commits
`1a61c70` "Fix for screensaver and game videos playing while a game is playing" and
`a487e97` "Fix bug where videos would restart and screen saver would start while playing
games" **Interpretations:** a race between MediaElement state and timer callbacks.
**Why it matters:** `B-24` removes the loop; removing it blind reintroduces a fixed bug.
**Resolve by:** reproduce with the loop removed and diagnose the actual race.

### OQ-013 — How does Eclipse know a game has exited?
**Feature:** FEAT-LAUNCH-001 · **Evidence:** `RULE-LAUNCH-007` — the running flag is
cleared by *any input*, not by a host notification
LaunchBox exposes `IGameLaunchingPlugin.OnGameExited`, which Eclipse does not implement.
**Interpretations:** (a) the input heuristic predates awareness of that interface;
(b) deliberate simplicity. **Why it matters:** if a game exits and the user does nothing,
Eclipse still believes a game is running and attract mode stays suppressed. **Resolve by:**
product decision — implementing `IGameLaunchingPlugin` would be a behaviour improvement,
not a refactor.

### OQ-014 — Why is favourite saved immediately but rating deferred?
**Feature:** FEAT-CURATE-001/002 · **Evidence:** `RULE-CURATE-002` vs `RULE-CURATE-003`
**Interpretations:** (a) deliberate — rating is scrubbed continuously, favourite is a
single toggle; (b) inconsistency. **Observed:** the asymmetry is coherent. **Why it
matters:** a "consistency" refactor could add a write per rating step. **Resolve by:**
document as intended unless the product owner disagrees.

### OQ-015 — Should attract mode respect the browsed list set?
**Feature:** FEAT-ATTRACT-002 · **Evidence:** `RULE-ATTRACT-003`
Attract mode picks from the whole library, ignoring the current category or search
results. **Interpretations:** (a) intended — it is a screensaver for the whole library;
(b) an oversight. **Why it matters:** a user who has just voice-searched sees unrelated
games. **Resolve by:** product decision.

### OQ-016 — Should page-key remapping take effect without a restart?
**Feature:** FEAT-INPUT-002 · **Evidence:** `RULE-INPUT-008` — strategies cached on first use
**Why it matters:** `B-26` may make settings live, changing this incidentally.
**Resolve by:** product decision; note as an intended change if pursued.

### OQ-017 — Should deleting a custom list respect Cancel?
**Feature:** FEAT-CONFIG-003 · **Evidence:** `RULE-CONFIG-009` vs `RULE-CONFIG-010`
Reordering is only persisted on Save, but deletion writes immediately. **Interpretations:**
(a) deletion is intentionally immediate and irreversible; (b) inconsistency.
**Why it matters:** a user who deletes then cancels loses the list. **Resolve by:**
product decision. Note commit `202c8bc` fixed a related bug where saving or deleting
reloaded settings and discarded pending changes — this area has a history.

### OQ-018 — Why does shipping a manifest suppress the Tools menu item?
**Feature:** FEAT-INTEGRATE-002 · **Evidence:** `RULE-INTEGRATE-012`; established
empirically during the LB14 migration
A `manifest.json` causes LaunchBox 14 to treat Eclipse as a managed plugin, and the
`ISystemMenuItemPlugin` entry does not appear. **Interpretations:** (a) intended
LaunchBox behaviour — managed plugins are configured through the Plugin Manager;
(b) a LaunchBox defect. **Why it matters:** it currently forces a choice between a Tools
menu item and a Plugin Manager listing. **Resolve by:** ask the LaunchBox developers.

### OQ-019 — Is the startup theme's background path still correct?
**Feature:** FEAT-INTEGRATE-003 · **Evidence:** the startup theme references
`pack://siteoforigin:,,,/Plugins/Eclipse/Media/DefaultBackground/DefaultBackground.jpg`
`siteoforigin` resolves against the host's base directory, which became `Core\` when
LaunchBox moved its executables. **Interpretations:** (a) still works by some mechanism;
(b) silently broken since LaunchBox 13. **Why it matters:** users following the README's
startup-theme instructions may see a missing background. **Resolve by:** enable the
startup theme and look.

### OQ-020 — Should Left/Right do something on every detail option?
**Feature:** FEAT-PRESENT (detail overlay) · **Evidence:** `State/GameDetailOptions/`
Left and Right cycle game versions on **Play** and adjust the value on **Rating**, but do
nothing at all on **Favorite** and **More like this** — the keys are accepted and discarded.
**Interpretations:** (a) intended, since neither option has anything to vary; (b) an accident
of each option having been written as its own state class. **Observed:** the keys are dead on
two of four options. **Why it matters:** on a controller the stick is the obvious thing to
push, and half the options ignore it. **Resolve by:** product decision. Surfaced while
collapsing the four option states into one; behaviour deliberately preserved.

### OQ-021 — Should Enter close the detail overlay consistently?
**Feature:** FEAT-PRESENT (detail overlay) · **Evidence:** `State/GameDetailOptions/`
Enter closes the overlay on **Play** (a game launches) and **More like this**, but leaves it
open on **Favorite** and **Rating**. **Interpretations:** (a) intended — favouriting and
rating are adjustments you may want to repeat or see confirmed, whereas the other two navigate
away; (b) inconsistent. **Observed:** two of four close. **Why it matters:** it is the primary
action key and it behaves differently per row. **Resolve by:** product decision. Note that
`RULE-CURATE-002` depends on Rating staying open, since Enter is one of its commit points.

---

## Summary

| | Count |
|---|---|
| Suspected dead items | 12 (`DEAD-001` … `DEAD-012`) |
| — high confidence, safe to remove after a grep | 5 |
| — requires investigation first | 7 |
| Open questions | 21 (`OQ-001` … `OQ-021`) |
| — product decisions | 13 |
| — research/experiment tasks | 5 |
| — external (LaunchBox) questions | 1 |
| — likely genuine defects worth their own items | 2 (`OQ-010` stale cache, `OQ-019` startup background) |

**The two highest-value questions to resolve first** are `OQ-011` (live-vs-restart
settings table — a prerequisite for `B-26`) and `OQ-012` (the stop-loop race — a
prerequisite for `B-24`). Both are research tasks with concrete methods, and both
currently block backlog items that would otherwise be done blind.
