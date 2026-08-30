# Eclipse — Verification Model

How we know Eclipse still works. This document defines behaviour-oriented verification
scenarios for the capabilities in [FEATURES.md](FEATURES.md), states what is covered
today, and identifies what must exist **before** the corresponding modernization work
begins.

---

## Current coverage

**A test project exists and is green.** `Eclipse.Tests` (xunit, `net10.0-windows`) runs
140 passing tests. It arrived with the 16:10 layout work rather than as `B-01`, and for a
while covered only layout geometry; the media and presentation refactor added six more
suites - image cropping, the bezel choice, the video failure policy, the selection timings,
the once-only hydration guard, and the selected-game sequence.

That still leaves most of the product verified only by manual play-testing, which remains
the largest risk to the modernization programme: every planned change is
behaviour-preserving, and for most capabilities the only mechanism for proving
preservation is a human remembering what the product used to do. **What has changed is
that there is now somewhere to put a test**, so the remaining gap is coverage rather than
infrastructure.

| Layer | Status |
|---|---|
| Unit tests | 140 — layout geometry, image cropping, bezel choice, video failure policy, selection timings, once-only hydration, selected-game sequencing |
| Integration tests | none |
| Host-dependent tests | none |
| Manual regression script | none written down (the plugin README documents usage, not verification) |
| Performance baseline | browse and startup measured under `B-28`; recorded in `docs/plans/box-art-row-refactor.md`. The instrumentation that produced it has since been removed (`B-34`) |

## What is testable, and when

Most of Eclipse cannot be unit tested today because LaunchBox SDK types are the domain
model — see `S-2` and backlog item `B-12`.

| Testability | Applies to | Available |
|---|---|---|
| Testable **now** | Title decomposition, match scoring, list-window cycling, dynamic filter/sort expression building | Immediately |
| Testable after `B-11`/`B-12` | Position restoration, alternate-version filtering, media path resolution | After the adapter and game model exist |
| Testable **now** | List construction and custom-list membership - `GameListBuilder` takes an `IGameCatalogSource` since `B-15a`, and `IGame` is an interface in a vendored assembly, so a fixture needs a hand-written fake rather than the full game model | The project exists; the `IGame` fake does not yet |
| Testable **now** (was `B-18`) | Attract-mode sequencing and presenter call ordering — `IAttractModePresenter` exists and `AttractModeSlideshow` takes it plus its timings by constructor | Immediately |
| Testable **now** | Image cropping and scaling — `ImageScaler` is static and file-to-file, so a fixture image and an expected rectangle are the whole test | Immediately |
| Testable **now** (was `B-19`) | Selected-game sequencing and presenter call ordering — `ISelectedGamePresenter` exists and `SelectedGameSequence` takes it, its timings and its waits by constructor | Immediately; fourteen such tests exist |
| Testable **now** | The video failure escalation (`VideoFailurePolicy`) and the bezel choice (`BezelRules`) — both pure functions | Immediately |
| Host-dependent, manual only | Plugin registration, theme hosting, menu item, game launching, video playback, speech recognition | Always |

---

## Verification scenarios

Each scenario is written so a developer can execute it without reading the code.

### EPIC-BROWSE

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-BROWSE-001 | Select each browse category in turn. Every category produces at least one list; lists are ordered with custom lists first, then alphabetically; each list title shows a count when enabled. | Manual | RULE-BROWSE-002, 003, 012 |
| VER-BROWSE-002 | Pick a game with three genres. Confirm it appears in all three genre lists. | Manual / unit after B-12; also visible in the `B-15` golden dump | RULE-BROWSE-001 |
| VER-BROWSE-003 | Navigate right from the last game in a list; confirm wrap to the first. Repeat for left, up, down. | Manual | RULE-BROWSE-008 |
| VER-BROWSE-004 | In a list of 100 games, press the page key; confirm the selection moves 7. In a list of 4, confirm it moves 2. | Manual / unit | RULE-BROWSE-009 |
| VER-BROWSE-005 | **Position restoration.** Browse to a known game in the Favorites list. Un-favourite it. Confirm the user lands on the next game in that list, not at the top and not in a different list. Repeat for: game still present; list now empty; list gone entirely. | **Characterization — unit** | RULE-BROWSE-010 |
| VER-BROWSE-006 | Define a custom list with a filter, two sort expressions and a max size. Confirm membership matches the filter, order matches both sorts, and the cap selects the top N *after* sorting. | **Covered by probe, not by tests** — `CustomListQueryProbe` runs every field × operator and both sort directions against the real library; it guarded `B-15b`. Still wanted as a unit test (`B-01`), because a probe proves behaviour is unchanged, not that it is right. | RULE-BROWSE-005, 006, 007 |
| VER-BROWSE-007 | Browse to a list with 3 games with repeat-to-fill on, then off. Confirm the row repeats in the first case and shows gaps in the second. | Manual | RULE-BROWSE-011, 016 |
| VER-BROWSE-008 | Trigger random game 50 times from a set with one large and one small list; confirm selection is weighted by list size. | Exploratory | RULE-BROWSE-011 (`OQ-002`) |

### EPIC-SEARCH

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-SEARCH-001 | Given the title `"Final Fantasy VII: Remake"`, assert the registered phrase set: main title, subtitle, all contiguous word runs, roman numeral converted to `7`, colon removed, noise words excluded. | **Characterization — unit** | RULE-SEARCH-001…007 |
| VER-SEARCH-002 | Given `"Final Fantasy X"`, assert `X` is **not** converted to `10`. | **Characterization — unit** | RULE-SEARCH-004 |
| VER-SEARCH-003 | Given a phrase, match type and confidence, assert the computed match percentage; assert it never reaches 100. | **Characterization — unit** | RULE-SEARCH-010…012 |
| VER-SEARCH-004 | Speak a partial game name. Confirm results are grouped by phrase, ordered by best match, and that speaking a nonsense phrase yields a message rather than silence. | Manual | RULE-SEARCH-014…016, 006 |
| VER-SEARCH-005 | With voice search disabled, confirm the category picker omits it and no grammar is built at startup. | Manual | RULE-SEARCH-020 |

### EPIC-PRESENT

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-PRESENT-001 | Hold right through a list. Confirm the background and details do **not** change while moving, dim immediately, and fade in ~1s after stopping. | Manual | RULE-PRESENT-001, 002 |
| VER-PRESENT-002 | Select a game with no clear logo. Confirm the title text is shown instead. | Manual | RULE-PRESENT-003 |
| VER-PRESENT-003 | Open the detail overlay. Press Down four times and confirm the ring order Play → Favourite → More → Rating → Play. Repeat with Up. | Manual | RULE-PRESENT-005 |
| VER-PRESENT-004 | Move to the first game in a list; confirm the slot to its left is empty rather than showing the last game. | Manual | RULE-PRESENT-004 |
| VER-PRESENT-005 | Enable featured game. Press Up from the first list; confirm the featured view rather than wrapping. Disable it; confirm wrapping. | Manual | RULE-PRESENT-007, RULE-INPUT-006 |
| VER-PRESENT-006 | Toggle each detail-visibility setting; confirm exactly the corresponding element disappears. | Manual | FEAT-PRESENT-009 |
| VER-PRESENT-007 | Change each of the five selection timings on the **Presentation** tab, save, restart Big Box, and confirm the corresponding delay or fade changes and nothing else does. | Manual | RULE-PRESENT-001, 002 |
| VER-PRESENT-008 | Selected-game sequence: the call order, the four durations, the debounce, cancellation by a newer selection *and* by a slow decode, a zero video delay skipping the fade-in, a game with no video, videos disabled, and a game already running. | **Unit** — `SelectedGameSequenceTests`, 14 tests | RULE-PRESENT-001, 002; RULE-MEDIA-030…033 |
| VER-PRESENT-009 | Video failure escalation: nothing below three consecutive failures, reopen at three, abandon for the session at ten, nothing further once abandoned. | **Unit** — `VideoFailurePolicyTests` | FEAT-MEDIA-007 |

### EPIC-MEDIA

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-MEDIA-001 | **Bezel chain.** For each of the five levels in turn, arrange only that level's asset to exist and confirm it is chosen. Then confirm a 16:9 video gets no bezel. | **Characterization — unit + manual** | RULE-MEDIA-020…026 |
| VER-MEDIA-002 | **Cache identity.** Given a source image path and a display resolution, assert the derived cache path. Delete the cache, restart, confirm regeneration and that the second start does not regenerate. | **Characterization — unit** | RULE-MEDIA-002, 003 |
| VER-MEDIA-003 | **Crop equivalence.** Hash the cropped output of several hundred real clear logos before and after any cropping change. Hashes must be identical. | **Golden file** | RULE-MEDIA-010, backlog B-29 |
| VER-MEDIA-004 | Select a game with a video. Confirm background fades in, then video starts after the configured delay, then background fades back when the video ends. | Manual | RULE-MEDIA-030, 034 |
| VER-MEDIA-005 | Set the video delay to 0. Confirm playback starts immediately with no background fade-in step. | Manual | RULE-MEDIA-031 |
| VER-MEDIA-006 | Disable videos. Confirm the background image remains and no video plays. | Manual | RULE-MEDIA-035 |
| VER-MEDIA-007 | Select a game with no box art. Confirm the placeholder appears rather than a blank slot. | Manual | FEAT-MEDIA-001, 013 |
| VER-MEDIA-008 | Start cold with an empty cache on a large library; record time to first interaction and peak memory. | **Performance baseline** | backlog B-28 |
| VER-MEDIA-009 | Delete the resolution-specific media folder and start Big Box. Artwork populates as you browse and browsing stays smooth. Repeat warm and confirm nothing is regenerated. | Manual | FEAT-MEDIA-009, 011 |
| VER-MEDIA-010 | Crop bounds, per fixture: transparent border, no border, border on one side only, fully transparent, 1×1, an internal transparent row, a partly transparent pixel. Exact expected rectangle asserted. | **Unit** — `ImageCropTests`, 10 tests | FEAT-MEDIA-010 |
| VER-MEDIA-012 | Bezel choice: game bezel beats everything at any aspect ratio; the widescreen cutoff either side of 1.7; orientation from the video; unknown dimensions. | **Unit** — `BezelRulesTests`, 12 tests | RULE-MEDIA-020…027 |
| VER-MEDIA-013 | Two callers ask for the same game's media at once: it is resolved once, and the second caller waits rather than seeing it as resolved with nothing populated. | **Unit** — `RunOnceTests` | RULE-MEDIA-009 |

### EPIC-LAUNCH

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-LAUNCH-001 | Launch a game, exit it, confirm Eclipse returns with video and attract mode working and the same game selected. | Manual | RULE-LAUNCH-001, 007 |
| VER-LAUNCH-002 | For a game with additional applications including a run-before, a run-after and a non-emulator app, toggle each exclusion setting and assert the resulting version list. | **Characterization — unit after B-12** | RULE-LAUNCH-003 |
| VER-LAUNCH-003 | For a game whose additional application shares the game's application path, confirm the base game is not listed twice. | **Characterization — unit after B-12** | RULE-LAUNCH-004 |
| VER-LAUNCH-004 | Enable bypass details. Confirm Enter launches directly from browsing. | Manual | FEAT-LAUNCH-003 |

### EPIC-CURATE

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-CURATE-001 | Favourite a game from the Genre view. Switch to Platform view and confirm it appears in Favorites there too. | Manual | RULE-CURATE-004 |
| VER-CURATE-002 | Enter rating mode, adjust several steps, confirm the displayed rating updates live. Commit with Enter and confirm the value survives a Big Box restart. | Manual | RULE-CURATE-002, 005 |
| VER-CURATE-003 | Favourite a game while the overlay is open and confirm the row does **not** rebuild until the overlay closes. | Manual | RULE-CURATE-006 |
| VER-CURATE-004 | Confirm Page Up/Down do nothing while in rating mode. | Manual | RULE-CURATE-007 |
| VER-CURATE-005 | Enter rating mode, change the rating, exit with **Escape**. Confirm the original rating is restored and still there after a Big Box restart. | Manual | RULE-CURATE-009 |

### EPIC-ATTRACT

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-ATTRACT-001 | **Sequence.** Drive `AttractModeSlideshow` with a recording `IAttractModePresenter` and a short `AttractModeTimings`; assert the call order and the delay between calls against `RULE-ATTRACT-004`. | **Characterization — writable now** (`B-18` delivered; blocked only on `B-01`) | RULE-ATTRACT-004, 005 |
| VER-ATTRACT-002 | Idle into attract mode from the detail overlay. Press a key. Confirm return to the overlay, not to browsing. | Manual | RULE-ATTRACT-008 |
| VER-ATTRACT-003 | Launch a game and leave it running past the idle delay. Confirm attract mode does not start. | Manual | RULE-ATTRACT-006 |
| VER-ATTRACT-004 | **Cold session.** Restart Big Box and let attract mode trigger before media hydration finishes. Confirm games show their own artwork, not the default background. | Manual | RULE-ATTRACT-009 |
| VER-ATTRACT-005 | **Pan geometry.** At a non-100% Windows display scaling, confirm the background fills the screen with no black bands and the pan drifts into the screen in both directions. | Manual | RULE-ATTRACT-005 |

### EPIC-INPUT

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-INPUT-001 | **Exit path.** From browsing, press Escape and confirm you can reach Big Box's menu and exit the application. Repeat with "display options on escape" both on and off. | Manual — **critical** | RULE-INPUT-001, 004, 005 |
| VER-INPUT-002 | At the first game in a list, tap Left (opens options) then hold Left (wraps). Confirm both. | Manual | RULE-INPUT-002 |
| VER-INPUT-003 | Assign each of the ten functions to Page Up in turn and confirm each behaves as named, and is inert in states where it is not valid. | Manual | RULE-INPUT-007 |
| VER-INPUT-004 | Press input during startup loading; confirm nothing happens and Big Box does not react either. | Manual | RULE-INPUT-010 |

### EPIC-CONFIG

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-CONFIG-001 | Delete the settings file, start, confirm defaults are written and the product behaves per defaults. | Manual | RULE-CONFIG-001 |
| VER-CONFIG-002 | Delete the custom lists file, start, confirm Favorites and History are created with the documented filters and sorts. | **Characterization — unit** | RULE-CONFIG-002 |
| VER-CONFIG-003 | Load a settings file missing several properties and containing an unknown property. Confirm defaults populate and the unknown value does not break loading. | **Characterization — unit** | RULE-CONFIG-003 |
| VER-CONFIG-004 | Save settings; confirm a timestamped backup was created and the live file is valid JSON. | Manual | RULE-CONFIG-005, 006 |
| VER-CONFIG-005 | Reorder custom lists, then Cancel; confirm order is unchanged. Reorder, then Save; confirm it persists. Delete a list, then Cancel; observe whether the delete persisted. | Manual — **records `OQ-017`** | RULE-CONFIG-009, 010 |
| VER-CONFIG-006 | Corrupt `EclipseSettings.json` (truncate it mid-object). Start Big Box; confirm it starts on the newest readable backup and the log says why. Repeat with every backup corrupted; confirm it starts on defaults. | Manual | `JsonFileStore.Read` |
| VER-CONFIG-007 | Interrupt a save (kill LaunchBox during the write, or make the folder read-only). Confirm the previous settings file is intact and parses. | Manual | `JsonFileStore.Write` |
| VER-CONFIG-008 | Save 15 times; confirm exactly 10 backups remain and the newest are kept. | Manual | `JsonFileStore.BackupsToKeep` |
| VER-CONFIG-009 | Make `EclipseSettings.json` read-only, then Save. Confirm the failure is reported and the window stays open with the edits intact. | Manual | `B-25` |
| VER-CONFIG-010 | Reorder custom lists and Save; confirm the reorder survives a restart. Repeat with Add and with Edit, which also flush a pending reorder. | Manual | `B-25` |
| VER-CONFIG-011 | Open and close the settings window and the custom-list editor 20× each, then trigger a save. Confirm one handler invocation per event, not twenty. | Manual | `B-23` (config share) |
| VER-CONFIG-012 | Change a setting in LaunchBox while Big Box is running; confirm it takes effect only after Big Box restarts. Characterises the answer to `OQ-011`. | Manual | `OQ-011` |
| VER-CONFIG-013 | Open each of the seven tabs. Confirm every control is present, in the same place, and legible — in particular that a checked box is distinguishable from an unchecked one and that the selected combo value can be read. | Manual | settings-window-layout L6, L7 |
| VER-CONFIG-014 | Drag each of the four box-front margin sliders; confirm the preview updates live. This is the only side effect left in the settings view model. | Manual | settings-window-layout Stage 3 |
| VER-CONFIG-015 | On the Screen saver tab, confirm each slider reads back its value, that values snap to whole steps rather than landing on arbitrary numbers, and that unchecking *Enable screen saver* dims every dependent row. | Manual | settings-window-layout Stage 3b |
| VER-CONFIG-016 | Switch tabs repeatedly. Confirm no flicker, and that a tab returns showing values entered before leaving it — tab content is realised on demand now. | Manual | settings-window-layout Stage 4 |
| VER-CONFIG-017 | Custom lists tab: confirm the grid, the five buttons and double-click-to-edit all behave as before. It is the one tab that kept its `Grid` layout. | Manual | settings-window-layout Stage 3 |

### EPIC-INTEGRATE

| ID | Scenario | Type | Covers |
|---|---|---|---|
| VER-INTEGRATE-001 | **Deployment smoke check.** Assert the deployed plugin folder contains no `Unbroken.LaunchBox.Plugins.dll` and no `manifest.json`. | **Automated build check** | RULE-INTEGRATE-011, 012 |
| VER-INTEGRATE-002 | Start desktop LaunchBox; confirm **Tools → Manage eclipse** appears and opens the settings window. | Manual | FEAT-INTEGRATE-002 |
| VER-INTEGRATE-003 | Start Big Box with the Eclipse theme; confirm Eclipse renders and responds to input. | Manual | FEAT-INTEGRATE-001 |
| VER-INTEGRATE-004 | Confirm the settings and cache folders are created under `<LaunchBox>/Plugins/Eclipse/` on a clean install. | Manual | RULE-INTEGRATE-002, 004 |
| VER-INTEGRATE-005 | Induce a startup failure; confirm the error state appears and the log records it with context. | Manual | RULE-INTEGRATE-007, backlog B-02/B-04 |

---

## Characterization-test candidates, ranked

A characterization test asserts *what the code currently does*, however awkward, so that
a refactor cannot change it silently. These are ranked by (risk of silent breakage) ×
(cost of not noticing).

| Rank | Test | Why it matters | Blocked by |
|---|---|---|---|
| 1 | **Position restoration** (`VER-BROWSE-005`) | Four-deep fallback, entirely undocumented outside the code, user-visible every time they favourite something, and `B-14` rewrites it. Nearly pure logic. | `B-12` for a clean fixture; a crude version is possible sooner |
| 2 | **Voice title decomposition** (`VER-SEARCH-001`, `002`) | Pure string functions with many special cases (colon, slash, roman numerals, noise words). The most testable code in the product and completely uncovered. `B-30` would rewrite it. | Nothing — **can be written today** |
| 3 | **Match scoring** (`VER-SEARCH-003`) | Tuned heuristics the author described as endlessly tweakable. Any change silently reorders results. Pure arithmetic. | Nothing — **can be written today** |
| 4 | **Custom list membership and ordering** (`VER-BROWSE-006`) | **Rationale now partly spent.** The reflection over property-name strings is gone (`B-15b`) - a rename is a compile error - so the silent-break risk it names no longer exists. A test is still wanted to pin what the operators *should* do, which `CustomListQueryProbe` cannot say. | `B-01`; a fixture is easier after `B-12` but not blocked on it |
| 5 | **Bezel resolution** (`VER-MEDIA-001`) | Five-level chain across three files plus a video-aspect rule. `B-20` consolidates it. | Partially now; fully after `B-11` |
| 6 | **Clear-logo crop equivalence** (`VER-MEDIA-003`) | `B-29` rewrites the crop algorithm; the only meaningful acceptance criterion is pixel-identical output. | Nothing — **golden files can be captured today** |
| 7 | **Default custom lists** (`VER-CONFIG-002`) | Every new user sees these; they are constructed in code and easy to alter accidentally. | Nothing |
| 8 | **Settings round-trip** (`VER-CONFIG-003`) | `B-27` changes serialization; missing-property defaulting is the only migration mechanism. | Nothing |
| 9 | **Alternate-version filtering** (`VER-LAUNCH-002`, `003`) | Four independent settings interacting; wrong results are subtle. | `B-12` |
| 10 | **Attract-mode sequence** (`VER-ATTRACT-001`) | Timing *is* the feature. `B-18` has landed and the seam it created makes this a plain unit test. | Nothing — **writable today** |

**Four can be written before any further refactoring** — 2, 3, 6 and now 10 — plus 7 and 8
with minimal setup. Those are the natural contents of the first test project (`B-01`).
Item 10 joined them when `B-18` landed: `AttractModeSlideshow` takes its presenter and its
timings by constructor, so the sequence can be driven at a hundredth of real speed.

---

## Verification gaps by risk

| Risk | Area | Consequence if broken silently |
|---|---|---|
| **Critical** | Escape / handled-input contract (`VER-INPUT-001`) | The user cannot exit Big Box. No test would catch it. |
| **Critical** | Deployment shape (`VER-INTEGRATE-001`) | Total plugin load failure or a silently missing menu item. Both happened during the LB14 migration. |
| **High** | Position restoration | User lands in the wrong place after every favourite/rating/launch. |
| **High** | Custom list membership | User-defined lists silently change or empty. |
| **High** | Bezel resolution | Wrong or missing frame around every video. |
| **Medium** | Voice scoring | Result ordering changes; hard to notice, easy to blame on recognition. |
| **Medium** | Attract-mode timing | Feels wrong; no one can say precisely why. |
| **Medium** | Image cache paths | Full re-scale of the library on every start. |
