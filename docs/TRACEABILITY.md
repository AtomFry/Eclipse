# Eclipse — Traceability

Connects product capability ↔ technical debt ↔ modernization work ↔ verification, in
both directions. Source IDs: the architecture assessment (`M-*`, `S-*`, `C-*`) and the
modernization backlog (`B-NN`).

---

## 1. Feature → technical debt

| Epic | Debt affecting it |
|---|---|
| EPIC-BROWSE | S-1, S-2, S-3, S-8, S-9, S-10, C-2 |
| EPIC-SEARCH | M-5, M-6, S-2, S-8, S-14 |
| EPIC-PRESENT | M-4, M-8 (partly), S-3, ~~S-4~~ resolved, S-11, C-1, C-3, ~~C-5~~ resolved |
| EPIC-MEDIA | ~~M-1, M-2, M-3~~ resolved, M-4, M-9, ~~S-4~~ resolved, ~~S-7~~ closed |
| EPIC-LAUNCH | S-1, S-2, S-9, S-12 |
| EPIC-CURATE | M-4, S-1, S-2, S-9 |
| EPIC-ATTRACT | ~~M-4, M-8, S-4, C-5~~ — all four **closed for this epic** |
| EPIC-INPUT | M-8, S-1, C-5 |
| EPIC-CONFIG | M-4, S-5, S-6, S-13, C-7, C-8 |
| EPIC-INTEGRATE | M-7, M-9, S-2, C-6, C-7 |

## 2. Technical debt → feature

The important column is the last one: whether a finding can be fixed without
feature-specific regression coverage.

| Finding | Affected capabilities | User-visible? | Nature | Safe without feature coverage? |
|---|---|---|---|---|
| ~~M-1~~ resolve-once race | EPIC-MEDIA (FEAT-MEDIA-011) | Rarely — duplicate scaling, and attract mode showing the placeholder background | Correctness | **RESOLVED** — see `B-05`. The "localised, testable in isolation" call was right in spirit but not in detail: `GameFiles` itself is not constructible in a test, so the invariant moved to `Helpers/RunOnce`, which is. |
| ~~M-2~~ bitmap leak | EPIC-MEDIA (FEAT-MEDIA-010) | No | Resource | **RESOLVED** — normal paths by `B-06`, the failure paths in `A4`. |
| ~~M-3~~ stream leak | EPIC-MEDIA (FEAT-MEDIA-013) | No | Resource | **RESOLVED** by `B-06`. |
| M-4 subscription/disposal asymmetry | EPIC-PRESENT, EPIC-MEDIA, ~~EPIC-ATTRACT~~, EPIC-CURATE, EPIC-CONFIG | No, until it does | Resource | **No** — removing a live subscription silently kills a feature |
| ~~M-5~~ cached recogniser failure | EPIC-SEARCH | **Yes** — feature dead until restart | Correctness | **RESOLVED** — failure is no longer a silent permanent latch; see `B-03` |
| M-6 silent no-op failures | EPIC-SEARCH, and any catch site | **Yes** — keypress does nothing | Correctness / UX | **Decision needed** — changes what the user sees |
| M-7 log destination | EPIC-INTEGRATE | No | Infrastructure | **Yes** |
| M-8 view held by state/services | ~~EPIC-ATTRACT~~, ~~EPIC-PRESENT~~, EPIC-INPUT | No | Structural | **Done for ATTRACT** via `IAttractModePresenter` and for **PRESENT** via `ISelectedGamePresenter`. What is left is `AttractModeService.MainWindowViewModel` - a view *model* reference, which belongs with `B-09` |
| M-9 async void loading pump | EPIC-MEDIA, EPIC-INTEGRATE | No — failures are invisible | Correctness | **Yes**, but expect new (real) errors to surface |
| S-1 god view model | BROWSE, SEARCH, LAUNCH, CURATE, PRESENT, INPUT | No | Structural | **No** — touches six capabilities |
| S-2 no LaunchBox boundary | BROWSE, SEARCH, MEDIA, LAUNCH, CURATE, INTEGRATE | No | Structural | **No** |
| S-3 fixed 13-slot window | EPIC-PRESENT, EPIC-BROWSE | Yes if wrong | Structural | **Not debt** — `B-28` measured it at 1.7 ms/keypress, flat over 1,000 moves. The slot *boilerplate* is gone (`PreviousGame`/`SelectedGame`/`UpcomingGames`); the window itself stays. See `B-31`. |
| ~~S-4~~ logic in code-behind | ~~EPIC-PRESENT, EPIC-MEDIA, EPIC-ATTRACT~~ | No | Structural | **RESOLVED.** Attract mode first, then the selected-game sequence, its timings, the video failure policy and the bezel choice. The "No" was right about the risk and wrong about the remedy: the answer was not feature-wide regression coverage but four seams, each testable on its own. |
| S-5 triplicated settings | EPIC-CONFIG + every epic it parameterises | Yes if live/restart semantics change | Structural | **No** — needs the live-vs-restart table |
| S-6 unsafe persistence | EPIC-CONFIG | Only on corruption | Correctness | **Yes** |
| ~~S-7~~ per-pixel crop | EPIC-MEDIA | Startup time only | Performance | **CLOSED by measurement.** Wrong about where (first-run hydration, not startup) and wrong about how much (the scan is 10% of image work; reading and re-encoding files is 71%). See `B-29`. |
| S-8 index fan-out | EPIC-BROWSE, EPIC-SEARCH | Startup/memory only | Performance | **No** — changes the substrate of two epics |
| S-9 full rebuild on mutation | EPIC-CURATE, EPIC-LAUNCH, EPIC-BROWSE | Latency only | Performance | **No** — membership must be proven equivalent |
| S-10 LINQ per read | EPIC-BROWSE | No | Performance | **Not worth doing** for `MatchCount` — see `B-07`, closed. Any other instance still stands. |
| S-11 WPF brush on model | EPIC-PRESENT | No | Structural | **Yes** — likely already dead (`DEAD-005`) |
| S-12 sleep-loop | EPIC-LAUNCH, EPIC-SEARCH | 500 ms delay | Correctness | **No** — compensating for an undiagnosed race |
| S-13 async void commands | EPIC-CONFIG | Silent save failures | Correctness | **Yes** |
| S-14 broad catch | All | Yes — failures vanish | Correctness | **Decision needed** (same as M-6) |
| C-1 PropertyChanged boilerplate | EPIC-PRESENT | No | Cleanup | **No** — some setters notify deliberately without equality checks |
| ~~C-2~~ category-picker switch | EPIC-BROWSE | No | Cleanup | **RESOLVED** — the ten-branch switch in `SelectingOptionsState.OnEnter` is now three branches over `GameListBuilder.BrowsableCategories` |
| C-3 duplicate converters | EPIC-PRESENT | No | Cleanup | **Yes** — with a binding-error sweep |
| C-4 dead assets | none | No | Cleanup | **Yes** |
| C-5 magic values | PRESENT, ~~ATTRACT~~, MEDIA, INPUT | No | Cleanup | **Yes** |
| ~~C-6~~ unused deps.json | EPIC-INTEGRATE | No | Cleanup | **RESOLVED** — removed from the payload |
| C-7 Prism load failure | EPIC-CONFIG, EPIC-INTEGRATE | No (latent) | Infrastructure | **Yes** |
| C-8 four-class file | EPIC-CONFIG | No | Cleanup | **Yes** |

### Findings with no meaningful feature dependency

`C-4`, `C-6`, `C-8`, and `M-7` are pure infrastructure or cleanup with no behavioural
contract attached. **These are safe to perform independently at any time** and are the
natural first commits of any modernization effort. `C-5` is nearly in this category —
naming a constant changes nothing, provided the value is preserved exactly.

## 3. Feature → backlog

| Epic | Backlog items |
|---|---|
| EPIC-BROWSE | ~~B-07~~ closed, B-12, B-14, ~~B-15a~~ delivered, ~~B-15b~~ delivered, B-16, B-17, B-30, B-35 |
| EPIC-SEARCH | B-03, B-04, B-12, B-30 |
| EPIC-PRESENT | ~~B-18~~ delivered, ~~B-19~~ delivered, B-21, ~~B-31~~ closed, B-32, B-33, B-35 |
| EPIC-MEDIA | ~~B-05~~ delivered, ~~B-06~~ delivered, ~~B-20~~ delivered, B-22, ~~B-28~~ delivered, ~~B-29~~ closed |
| EPIC-LAUNCH | B-11, B-12, B-16, B-24 |
| EPIC-CURATE | B-11, B-12, B-14, B-16 |
| EPIC-ATTRACT | ~~B-18~~ delivered, ~~B-23~~ satisfied |
| EPIC-INPUT | ~~B-18~~ delivered, ~~B-19~~ delivered, ~~B-26~~ delivered |
| EPIC-CONFIG | B-10, B-13, B-23, B-25, B-26, B-27 |
| EPIC-INTEGRATE | B-02, B-11, B-13, B-22 |

## 4. Backlog → capability

| Item | Capability affected | Enabling? | Behaviour at risk | Required verification |
|---|---|---|---|---|
| ~~B-01~~ test project | none | **Yes** | none | **Partly delivered.** `Eclipse.Tests` exists (xunit, `net10.0-windows`) and is green at 72 tests, but it arrived with the 16:10 layout work and covers layout geometry only. The project is no longer a prerequisite for anything; writing the characterization tests still is. |
| B-02 logging | EPIC-INTEGRATE | **Yes** | log location changes | VER-INTEGRATE-005 |
| ~~B-03~~ recogniser retry | EPIC-SEARCH | no | voice search recovery | **Delivered.** `SpeechRecognizerService` publishes a `VoiceSearchAvailability` and records why it failed, instead of latching dead until restart. Closes `M-5`. |
| B-04 surface failures | all | no | **what the user sees on failure** | VER-INTEGRATE-005; needs a product decision |
| ~~B-05~~ resolve-once race | EPIC-MEDIA | no | duplicate scaling | **Delivered.** The flag was set before the work, so a caller arriving mid-hydration was told "done" and found nulls - which is why attract mode showed the placeholder background for games that had one. Now a `Helpers/RunOnce` task handle: callers wait for the run in flight. Six tests; two fail against the old design. |
| ~~B-06~~ GDI+ leaks | EPIC-MEDIA | no | none | **Delivered, and now complete.** Every `Bitmap` and the placeholder stream are inside `using` blocks; the residual — `ResizeImage` and `Crop` leaking their result if the draw threw — was fixed in `A4` alongside the crop bounds. |
| ~~B-07~~ memoise count | EPIC-BROWSE | no | row slot population | **Closed, not done.** `MatchCount` returns `MatchingGames.Count()`, and `Enumerable.Count()` takes the `ICollection<T>` fast path on a `List<T>` - it is already `O(1)`. Memoising would add an invalidation obligation (`MatchingGames` is publicly settable and is replaced on every curation rebuild) in exchange for nothing. `VER-BROWSE-007` stands on its own - it verifies repeat-to-fill slot population, not the count. |
| B-08 dead code | none | no | none | build + visual sweep |
| B-09 DI | all | **Yes** | initialisation order | init-order trace before/after |
| ~~B-10~~ split file | EPIC-CONFIG | no | none | **delivered** - four types, four files |
| B-11 LaunchBox adapter | INTEGRATE + 5 epics | **Yes** | catalogue reads, launching, saving | VER-LAUNCH-001, VER-CURATE-001 |
| B-12 game model | BROWSE, SEARCH, MEDIA, LAUNCH, CURATE | **Yes** | **XAML bindings — silent empty controls** | binding-error trace clean; VER-PRESENT-002/006 |
| ~~B-13~~ Prism removal | EPIC-CONFIG, EPIC-INTEGRATE | no | settings editor event flow | **delivered** - `RelayCommand` + static `SettingsEvents`; closes `C-7`, and `B-32` should be re-evaluated since most hand-written notification went with `B-26` |
| B-14 position restoration | EPIC-BROWSE, EPIC-CURATE | no | **where the user lands** | VER-BROWSE-005 — characterization first. **Partly overtaken:** the logic was rewritten into list-local coordinates and moved to `GameListNavigator.RememberPosition`/`RestorePosition`, and `OQ-022` closed with it. What remains of this item is making it *verifiable*, which still wants `B-01`. |
| ~~B-15a~~ list construction | EPIC-BROWSE | no | list membership and order | **delivered** — `GameListBuilder`; verified by golden dump diff |
| ~~B-15b~~ custom-list query engine | EPIC-BROWSE | no | list membership and order | **delivered** — `GameFields` accessor map; verified by `CustomListQueryProbe` diff over every field/operator |
| B-16 incremental rebuild | EPIC-CURATE, EPIC-LAUNCH | no | stale lists after curation | VER-CURATE-001; equivalence vs full rebuild |
| B-17 picker switch | EPIC-BROWSE | no | category selection | VER-BROWSE-001 |
| B-18 attract presenter | ~~EPIC-ATTRACT~~ **delivered**, EPIC-PRESENT | **Yes** | **attract timing** | VER-ATTRACT-001 — now writable against the presenter seam, blocked only on `B-01` |
| ~~B-19~~ view delegates | EPIC-PRESENT, EPIC-INPUT | **Yes** | fade/animation dispatch | **Delivered.** Two settable delegate properties and two forwarders became two events the view subscribes to. Three delegate types deleted, one already dead. VER-PRESENT-001 still wanted manually. |
| ~~B-20~~ bezel consolidation | EPIC-MEDIA | no | which bezel is chosen | **Delivered.** Five levels enumerated in `media.md` first, then `BezelService.ResolveBezel` became the single entry point; the decision itself is in `BezelRules`, unit tested. Old and new agree over a ~200,000 case sweep. Corrected `RULE-MEDIA-024`: the widescreen cutoff never applied to game-specific bezels. |
| B-21 brush off model | EPIC-PRESENT | no | none if dead | confirm `DEAD-005` |
| B-22 loading pipeline | EPIC-MEDIA, EPIC-INTEGRATE | no | loading completion signalling | VER-INTEGRATE-005 |
| B-23 disposal symmetry | PRESENT, MEDIA, ~~ATTRACT~~, ~~CONFIG~~ | no | **a removed subscription kills a feature** | open/close settings ×20; 30 min idle soak |
| B-24 sleep loop | EPIC-LAUNCH | no | **video/screensaver during gameplay** | VER-LAUNCH-001 ×10 |
| ~~B-25~~ async void commands | EPIC-CONFIG | no | save/delete failure reporting | **delivered** - all six report or log; also fixed an unawaited custom-list write |
| ~~B-26~~ settings consolidation | EPIC-CONFIG + all | no | **live vs restart semantics** | **delivered** - `OQ-011` answered from the code: nothing is live, so there were no semantics to preserve |
| ~~B-27~~ safe persistence | EPIC-CONFIG | no | settings loading | **delivered** - `Helpers/JsonFileStore`; VER-CONFIG-006..008 added |
| ~~B-28~~ instrumentation | EPIC-MEDIA, EPIC-BROWSE | **Yes** | none | **delivered, and since removed** by `B-34`. Baselines recorded in `docs/plans/box-art-row-refactor.md`. |
| ~~B-29~~ crop rewrite | EPIC-MEDIA | no | cropped image output | **Closed, not done** — the third item closed by measurement, after `B-07` and `B-31`. The `LockBits` rewrite would save ~25 s of an eight-minute background job. The bounds *defect* it would have carried is being fixed on its own as `A4` of `docs/plans/media-and-presentation-refactor.md`, which also takes `B-06`'s failure-path residual. |
| B-30 index fan-out | EPIC-BROWSE, EPIC-SEARCH | no | **list membership + voice results** | VER-BROWSE-006, VER-SEARCH-003 |
| B-31 slot window | EPIC-PRESENT | no | **layout and navigation latency** | VER-PRESENT-004; latency vs baseline. **The `B-28` data it was gated on now exists and does not support it** — see the note below. |
| B-32 PropertyChanged | EPIC-PRESENT | no | notification semantics | VER-PRESENT-006 |
| B-33 converters | EPIC-PRESENT | no | bindings | binding-error trace clean |
| ~~B-34~~ remove browse instrumentation | none | no | none | **Delivered.** `BrowsePerformanceMonitor`, the `MeasureBrowsePerformance` setting and all eight call sites are gone; `RefreshGames`/`RefreshGamesCore` collapsed back into one method and `CountChangedSlots` went with them. The baselines it produced are in `docs/plans/box-art-row-refactor.md`. Same call `d63dabb` made for `StartupPerformanceMonitor`. |
| B-35 navigation odds and ends | EPIC-BROWSE, EPIC-PRESENT | no | none expected | Two small things noticed during the navigation refactor and deliberately left alone. (a) `GameList.ListCategoryType` is **never assigned** anywhere, so every list carries the default and the `list.ListCategoryType == rememberedListCategory` half of the position-restoration match always passes — either populate it when lists are built or drop it from the match. (b) `EclipseConstants.GamesToPage` is a public mutable static in a class whose own comment reads "todo: rework this…this is hacky as shit"; the page size is arguably a setting. Verify with `VER-BROWSE-004`, `VER-BROWSE-005` |

---

## 5. Observations on the backlog

The backlog is sound. These are refinements, not corrections. **No changes have been made
to it** — this section is advisory.

### Enabling vs capability work

Seven items are pure infrastructure with **no capability of their own**: `B-01`, `B-02`,
`B-08`, `B-09`, `B-10`, `B-28`, `B-34`, and arguably `B-13`. They should be tracked separately
from capability work so that "Modernize Game Browsing" does not appear blocked by a test
project.

Conversely, three items are *enabling for a specific capability* and would be better
named as such: `B-14` reads as "extract a method" but is really "make position
restoration verifiable"; `B-18` is really "make attract mode testable"; `B-11`/`B-12`
together are "make the game catalogue substitutable".

### Items whose scope looks wrong

| Item | Observation |
|---|---|
| `B-12` | **Should be split.** It bundles (a) defining an Eclipse-owned game model, (b) rewriting every XAML binding path, and (c) moving curation mutation off property setters. (c) is a behavioural change affecting EPIC-CURATE and deserves its own item with its own verification. |
| `B-15` | **Was split**, as this note recommended, into `B-15a` (category-list construction) and `B-15b` (the query engine). Both delivered. The characterization the note asked for was taken as development-time probes diffed before and after rather than as unit tests, because `B-01` was deferred — see `docs/plans/list-construction-refactor.md`. |
| `B-23` | **Should be split by subsystem.** "94 subscriptions" spans attract mode, media, presentation and the settings windows. Done as one sweep, a mistake is very hard to attribute. *The attract mode share has since been done on its own, as part of `EPIC-ATTRACT` — which is the argument for the split.* |
| `B-04` | **Blocked on a product decision**, not engineering. Should not be scheduled until the "how loud is a degraded failure" question is answered. |
| `B-30` | **Premature.** Explicitly gated on `B-28`, but it also lacks the characterization coverage (`VER-BROWSE-006`, `VER-SEARCH-003`) that would make it safe. Both should be prerequisites. |
| `B-31` | **Answered — do not treat as debt.** The note below said it needed `B-28` latency data first. That data now exists: a keypress costs 1.7 ms to slot assignment and 3.9 ms to rendered, flat across 1,000 moves, on a 694-game library at 2560x1600. The fixed window is not a performance problem, and replacing it with virtualization would trade a measured non-problem for container regeneration per keypress. The row was instead cleaned up structurally (`docs/plans/box-art-row-refactor.md` Stage 5a) without changing the window. |
| `B-26` | ~~**Missing a prerequisite deliverable.** The live-vs-restart table (`OQ-011`) is a research task that should be its own item.~~ **Answered, and the note was wrong about the shape of it.** The prerequisite was real but it was not research: reading the code settled it in minutes, and the answer — nothing is live — meant there were no semantics for `B-26` to preserve. Worth generalising: before scheduling an empirical audit, check whether the code already answers it. |

### Items that appear to overlap

* `B-08` (dead code) and `B-21` (brush off model) — `B-21` is likely a no-op if the brush
  is already dead. Confirm `DEAD-005` during `B-08` and `B-21` may disappear.
* `B-13` (Prism removal) and `B-32` (CommunityToolkit generators) — both are "change the
  MVVM dependency". Doing `B-13` first may make `B-32` unnecessary or trivial.
* ~~`B-06` (GDI+ leaks) and `B-29` (crop rewrite) touch the same method.~~ **Both resolved,
  and the note's instinct was right for a reason it did not anticipate.** `B-06` was done
  first and independently, which was correct — and just as well, because `B-29` then failed
  its measurement gate and never happened. Had they been bundled, a safe fix would have
  been thrown away with an unjustified one. **Generalisable: when a cheap safe fix is
  bundled with an expensive gated one, do the safe one separately, because the gate may
  fail.**

### Items lacking functional verification

`B-09` (DI) and `B-19` (delegates) are the two largest behaviour-preserving changes with
the weakest verification stories. `B-09`'s init-order trace is a good idea but is an
implementation-level check, not a behavioural one; it should be paired with a full manual
capability sweep. `B-19` changes the mechanism by which every animation is triggered and
has only `VER-PRESENT-001` behind it.

### ~~Recommended first capability~~ — spent

This section recommended **Voice Search** as the first capability, on the grounds that it
was the only one whose chain already lined up: two characterization tests writable with no
prerequisites, a known user-facing defect (`M-5`/`B-03`), and a narrow blast radius.

**It was done** — `51364db` and `997377f` — and the recommendation is therefore retired
rather than deleted, because the reasoning held up and is worth reusing. What it did *not*
produce was the "characterize, fix, verify" template it promised: the fix landed, the
characterization tests did not, and `VER-SEARCH-001`/`003` are still unwritten against a
`GameTitleGrammar` that is pure string handling and could be tested today.

**Read that as the lesson.** Choosing a capability whose tests are writable does not cause
the tests to be written. The order that follows — see
`docs/plans/media-and-presentation-refactor.md` — puts the provable work first *and* makes
each stage's test part of the stage rather than a follow-up.
