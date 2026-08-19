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
| EPIC-PRESENT | M-4, M-8, S-3, S-4, S-11, C-1, C-3, C-5 |
| EPIC-MEDIA | M-1, M-2, M-3, M-4, M-9, S-4, S-7 |
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
| M-1 resolve-once race | EPIC-MEDIA (FEAT-MEDIA-011) | Rarely — duplicate scaling | Correctness | **Yes** — localised, testable in isolation |
| M-2 bitmap leak | EPIC-MEDIA (FEAT-MEDIA-010) | No | Resource | **Yes** |
| M-3 stream leak | EPIC-MEDIA (FEAT-MEDIA-013) | No | Resource | **Yes** |
| M-4 subscription/disposal asymmetry | EPIC-PRESENT, EPIC-MEDIA, ~~EPIC-ATTRACT~~, EPIC-CURATE, EPIC-CONFIG | No, until it does | Resource | **No** — removing a live subscription silently kills a feature |
| M-5 cached recogniser failure | EPIC-SEARCH | **Yes** — feature dead until restart | Correctness | **Yes** |
| M-6 silent no-op failures | EPIC-SEARCH, and any catch site | **Yes** — keypress does nothing | Correctness / UX | **Decision needed** — changes what the user sees |
| M-7 log destination | EPIC-INTEGRATE | No | Infrastructure | **Yes** |
| M-8 view held by state/services | ~~EPIC-ATTRACT~~, EPIC-PRESENT, EPIC-INPUT | No | Structural | **Done for ATTRACT** via `IAttractModePresenter`; still **No** for the rest |
| M-9 async void loading pump | EPIC-MEDIA, EPIC-INTEGRATE | No — failures are invisible | Correctness | **Yes**, but expect new (real) errors to surface |
| S-1 god view model | BROWSE, SEARCH, LAUNCH, CURATE, PRESENT, INPUT | No | Structural | **No** — touches six capabilities |
| S-2 no LaunchBox boundary | BROWSE, SEARCH, MEDIA, LAUNCH, CURATE, INTEGRATE | No | Structural | **No** |
| S-3 fixed 13-slot window | EPIC-PRESENT, EPIC-BROWSE | Yes if wrong | Structural | **No** — visual fidelity risk |
| S-4 logic in code-behind | EPIC-PRESENT, EPIC-MEDIA, ~~EPIC-ATTRACT~~ | No | Structural | **No** |
| S-5 triplicated settings | EPIC-CONFIG + every epic it parameterises | Yes if live/restart semantics change | Structural | **No** — needs the live-vs-restart table |
| S-6 unsafe persistence | EPIC-CONFIG | Only on corruption | Correctness | **Yes** |
| S-7 per-pixel crop | EPIC-MEDIA | Startup time only | Performance | **Yes**, with golden-file output equivalence |
| S-8 index fan-out | EPIC-BROWSE, EPIC-SEARCH | Startup/memory only | Performance | **No** — changes the substrate of two epics |
| S-9 full rebuild on mutation | EPIC-CURATE, EPIC-LAUNCH, EPIC-BROWSE | Latency only | Performance | **No** — membership must be proven equivalent |
| S-10 LINQ per read | EPIC-BROWSE | No | Performance | **Yes** — pure memoisation |
| S-11 WPF brush on model | EPIC-PRESENT | No | Structural | **Yes** — likely already dead (`DEAD-005`) |
| S-12 sleep-loop | EPIC-LAUNCH, EPIC-SEARCH | 500 ms delay | Correctness | **No** — compensating for an undiagnosed race |
| S-13 async void commands | EPIC-CONFIG | Silent save failures | Correctness | **Yes** |
| S-14 broad catch | All | Yes — failures vanish | Correctness | **Decision needed** (same as M-6) |
| C-1 PropertyChanged boilerplate | EPIC-PRESENT | No | Cleanup | **No** — some setters notify deliberately without equality checks |
| C-2 category-picker switch | EPIC-BROWSE | No | Cleanup | **Yes** |
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
| EPIC-BROWSE | B-07, B-12, B-14, B-15, B-16, B-17, B-30 |
| EPIC-SEARCH | B-03, B-04, B-12, B-30 |
| EPIC-PRESENT | B-18, B-19, B-21, B-31, B-32, B-33 |
| EPIC-MEDIA | B-05, B-06, B-20, B-22, B-28, B-29 |
| EPIC-LAUNCH | B-11, B-12, B-16, B-24 |
| EPIC-CURATE | B-11, B-12, B-14, B-16 |
| EPIC-ATTRACT | ~~B-18~~ delivered, ~~B-23~~ satisfied |
| EPIC-INPUT | B-18, B-19, B-26 |
| EPIC-CONFIG | B-10, B-13, B-23, B-25, B-26, B-27 |
| EPIC-INTEGRATE | B-02, B-11, B-13, B-22 |

## 4. Backlog → capability

| Item | Capability affected | Enabling? | Behaviour at risk | Required verification |
|---|---|---|---|---|
| B-01 test project | none | **Yes** | none | suite runs; plugin output unchanged |
| B-02 logging | EPIC-INTEGRATE | **Yes** | log location changes | VER-INTEGRATE-005 |
| B-03 recogniser retry | EPIC-SEARCH | no | voice search recovery | VER-SEARCH-004 |
| B-04 surface failures | all | no | **what the user sees on failure** | VER-INTEGRATE-005; needs a product decision |
| B-05 resolve-once race | EPIC-MEDIA | no | duplicate scaling | unit test on concurrent resolve |
| B-06 GDI+ leaks | EPIC-MEDIA | no | none | VER-MEDIA-002 |
| B-07 memoise count | EPIC-BROWSE | no | row slot population | VER-BROWSE-007 |
| B-08 dead code | none | no | none | build + visual sweep |
| B-09 DI | all | **Yes** | initialisation order | init-order trace before/after |
| B-10 split file | EPIC-CONFIG | no | none | build |
| B-11 LaunchBox adapter | INTEGRATE + 5 epics | **Yes** | catalogue reads, launching, saving | VER-LAUNCH-001, VER-CURATE-001 |
| B-12 game model | BROWSE, SEARCH, MEDIA, LAUNCH, CURATE | **Yes** | **XAML bindings — silent empty controls** | binding-error trace clean; VER-PRESENT-002/006 |
| B-13 Prism removal | EPIC-CONFIG, EPIC-INTEGRATE | no | settings editor event flow | VER-CONFIG-005 |
| B-14 position restoration | EPIC-BROWSE, EPIC-CURATE | no | **where the user lands** | VER-BROWSE-005 — characterization first |
| B-15 list construction | EPIC-BROWSE | no | list membership and order | VER-BROWSE-006 — characterization first |
| B-16 incremental rebuild | EPIC-CURATE, EPIC-LAUNCH | no | stale lists after curation | VER-CURATE-001; equivalence vs full rebuild |
| B-17 picker switch | EPIC-BROWSE | no | category selection | VER-BROWSE-001 |
| B-18 attract presenter | ~~EPIC-ATTRACT~~ **delivered**, EPIC-PRESENT | **Yes** | **attract timing** | VER-ATTRACT-001 — now writable against the presenter seam, blocked only on `B-01` |
| B-19 view delegates | EPIC-PRESENT, EPIC-INPUT | **Yes** | fade/animation dispatch | VER-PRESENT-001 |
| B-20 bezel consolidation | EPIC-MEDIA | no | which bezel is chosen | VER-MEDIA-001 — enumerate chain first |
| B-21 brush off model | EPIC-PRESENT | no | none if dead | confirm `DEAD-005` |
| B-22 loading pipeline | EPIC-MEDIA, EPIC-INTEGRATE | no | loading completion signalling | VER-INTEGRATE-005 |
| B-23 disposal symmetry | PRESENT, MEDIA, ~~ATTRACT~~, CONFIG | no | **a removed subscription kills a feature** | open/close settings ×20; 30 min idle soak |
| B-24 sleep loop | EPIC-LAUNCH | no | **video/screensaver during gameplay** | VER-LAUNCH-001 ×10 |
| B-25 async void commands | EPIC-CONFIG | no | save/delete failure reporting | VER-CONFIG-004 |
| B-26 settings consolidation | EPIC-CONFIG + all | no | **live vs restart semantics** | live-vs-restart table (`OQ-011`) |
| B-27 safe persistence | EPIC-CONFIG | no | settings loading | VER-CONFIG-003, 004 |
| B-28 instrumentation | EPIC-MEDIA, EPIC-BROWSE | **Yes** | none | baseline recorded |
| B-29 crop rewrite | EPIC-MEDIA | no | cropped image output | VER-MEDIA-003 golden files |
| B-30 index fan-out | EPIC-BROWSE, EPIC-SEARCH | no | **list membership + voice results** | VER-BROWSE-006, VER-SEARCH-003 |
| B-31 slot window | EPIC-PRESENT | no | **layout and navigation latency** | VER-PRESENT-004; latency vs baseline |
| B-32 PropertyChanged | EPIC-PRESENT | no | notification semantics | VER-PRESENT-006 |
| B-33 converters | EPIC-PRESENT | no | bindings | binding-error trace clean |

---

## 5. Observations on the backlog

The backlog is sound. These are refinements, not corrections. **No changes have been made
to it** — this section is advisory.

### Enabling vs capability work

Seven items are pure infrastructure with **no capability of their own**: `B-01`, `B-02`,
`B-08`, `B-09`, `B-10`, `B-28`, and arguably `B-13`. They should be tracked separately
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
| `B-15` | **Should be split.** Category-list construction and the reflection-based custom-list query engine are independent, carry different risks, and have different characterization needs. |
| `B-23` | **Should be split by subsystem.** "94 subscriptions" spans attract mode, media, presentation and the settings windows. Done as one sweep, a mistake is very hard to attribute. *The attract mode share has since been done on its own, as part of `EPIC-ATTRACT` — which is the argument for the split.* |
| `B-04` | **Blocked on a product decision**, not engineering. Should not be scheduled until the "how loud is a degraded failure" question is answered. |
| `B-30` | **Premature.** Explicitly gated on `B-28`, but it also lacks the characterization coverage (`VER-BROWSE-006`, `VER-SEARCH-003`) that would make it safe. Both should be prerequisites. |
| `B-31` | **Possibly premature.** The fixed 13-slot window may be a deliberate performance choice — fixed elements avoid container regeneration per keypress. Needs `B-28` latency data before it is treated as debt at all. |
| `B-26` | **Missing a prerequisite deliverable.** The live-vs-restart table (`OQ-011`) is a research task that should be its own item. |

### Items that appear to overlap

* `B-08` (dead code) and `B-21` (brush off model) — `B-21` is likely a no-op if the brush
  is already dead. Confirm `DEAD-005` during `B-08` and `B-21` may disappear.
* `B-13` (Prism removal) and `B-32` (CommunityToolkit generators) — both are "change the
  MVVM dependency". Doing `B-13` first may make `B-32` unnecessary or trivial.
* `B-06` (GDI+ leaks) and `B-29` (crop rewrite) touch the same method. If `B-29` is going
  to happen, `B-06` may be absorbed — though `B-06` should still be done first because it
  is safe now and `B-29` is gated on measurement.

### Items lacking functional verification

`B-09` (DI) and `B-19` (delegates) are the two largest behaviour-preserving changes with
the weakest verification stories. `B-09`'s init-order trace is a good idea but is an
implementation-level check, not a behavioural one; it should be paired with a full manual
capability sweep. `B-19` changes the mechanism by which every animation is triggered and
has only `VER-PRESENT-001` behind it.

### Recommended first capability

**Voice Search.** Not because it is the most valuable, but because it is the only
capability where the entire chain already lines up:

* Two characterization tests can be written **today** with no prerequisites
  (`VER-SEARCH-001`, `VER-SEARCH-003`) — pure string and arithmetic functions.
* It has a known, understood defect (`M-5`/`B-03`) that is a genuine user-facing fix.
* It is narrow: five files, one epic, no shared substrate beyond the index.
* Success produces the template — characterize, fix, verify — for every capability after
  it.

The alternative, **Game Browsing**, is higher value but its characterization work
(`VER-BROWSE-005`, `006`) is blocked behind `B-11`/`B-12`, which is the riskiest work in
the programme. Voice Search proves the process before betting the browsing experience on
it.
