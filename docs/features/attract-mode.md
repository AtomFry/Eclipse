# EPIC-ATTRACT — Attract Mode

**Scope.** The idle screensaver: what happens when the user stops interacting, and how
Eclipse returns to normal when they resume.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-ATTRACT-001 — Idle entry.** After a configurable idle period with no input,
Eclipse fades to black and enters attract mode.

**FEAT-ATTRACT-002 — Slideshow.** Attract mode cycles randomly chosen games from the
library. For each game the background image fades in and pans slowly across the screen,
the game's clear logo fades in shortly after, and both fade out before the next game.

**FEAT-ATTRACT-003 — Exit.** Any input exits attract mode and returns to whatever the
user was doing before it started.

**FEAT-ATTRACT-004 † — Suppression.** Attract mode does not start while a game is
running, and is stopped when a video preview begins.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-ATTRACT-001 | The idle timer restarts on essentially every user action and on most state transitions. | Almost every state handler restarts it; missing one produces a screensaver that appears during use. |
| RULE-ATTRACT-002 | The idle delay is configurable; attract mode can be disabled entirely, in which case no timer is created at all. Every screen saver setting requires a Big Box restart. | The plugin caches settings for the process lifetime (`RULE-CONFIG-004`) and the settings window edits its own copy loaded from file. |
| RULE-ATTRACT-003 | Attract mode games are chosen at random from the whole library, **not** from the currently browsed list set, and **each game is equally likely** - selection is never weighted by how many genres, playlists or title words a game has. | Unlike `RULE-BROWSE-011`. Until the index refactor the implementation contradicted this rule: it picked from the flat game bag, where a game appeared once per category value and once per voice phrase, so a 55-entry game was ~7x likelier than an 8-entry one. |
| RULE-ATTRACT-004 | The slideshow sequence per game is: fade to black → wait → pick game, fade in background and begin a pan → wait → fade in logo → wait out the rest of the game's time on screen → fade out background and logo → repeat. **Every duration is a user setting**; the defaults are the values that were previously hardcoded, so the feel is unchanged out of the box: 4s black, 3s background fade-in, 17s pan, 4s logo delay, 1.5s logo fade-in, 15s per game, 3s/0.5s fade-outs — 19s per slide. | These timings are the entire feel of the feature. They used to exist only as literals; they now live as `[DefaultValue]` attributes on `EclipseSettings`. |
| RULE-ATTRACT-005 | The pan direction alternates between games, and the pan distance is the difference between the image width and **the attract mode control's `ActualWidth`**. | Both are device-independent units. Measuring against the monitor resolution instead mixes DIPs with physical pixels, which agree only at 100% display scaling — that was a real defect, and on a scaled display it sent the image the wrong way and far too far. |
| RULE-ATTRACT-009 | A game whose media has not been hydrated yet is hydrated on demand before its slide is shown, so it displays its own artwork rather than the placeholder. A game with no background image falls back to the default; a game with no clear logo shows no logo. | Media hydration is lazy and background, and attract mode picks from the whole library, so early in a session it will land on unhydrated games. |
| RULE-ATTRACT-006 | Attract mode is not entered while a game is running. | |
| RULE-ATTRACT-007 | Starting a video preview stops attract mode; the video ending restarts the idle timer. | |
| RULE-ATTRACT-008 | Exiting returns to the exact state that was active when attract mode started, not to a default state. | The user resumes mid-overlay if that is where they were. |

---

## Current implementation

| Concern | Location |
|---|---|
| Idle timer and entry | `Service/AttractModeService.cs` — `RestartAttractMode`, `StopAttractMode`, `AttractModeDelay_Elapsed`. Also owns the running slideshow. |
| Slideshow sequencing | `Service/AttractModeSlideshow.cs` — one `async` loop, one `CancellationTokenSource` per run |
| Timings | `Service/AttractModeTimings.cs` — reads `EclipseSettings`, exposes `TimeSpan`s, clamps `HoldAfterLogo` |
| Presenter seam | `Service/IAttractModePresenter.cs` — five calls, taking decoded `ImageSource`s |
| Image decoding | `Service/AttractModeImageLoader.cs` — off-thread decode, `OnLoad` + `Freeze` |
| Fades, pan, image swap | `View/AttractModeView.xaml` / `.xaml.cs` — the only implementation of the presenter |
| Game selection | `View/MainWindowViewModel.cs` — `NextAttractModeGame`, indexing `GameCatalog.Games` directly |
| Entry/exit state | `State/AttractModeState.cs` — remembers the previous state, ends the run on any input |
| Timer restarts | Every `EclipseState` implementation |
| Suppression | `View/MainWindowViewModel.cs` — `IsPlayingGame`; `View/MainWindowView.xaml.cs` — video handlers |

## Technical debt

| Finding | Status |
|---|---|
| M-8 | **Closed for this epic.** The service and state depended on a concrete `MainWindowView`; they now depend on `IAttractModePresenter`, and the visuals live in their own `AttractModeView`. |
| M-4 | **Closed for this epic.** The three undisposed timers are gone; the sequence is an `async` loop cancelled by a `CancellationTokenSource` that `RunAsync` disposes in a `finally`. The one remaining timer is the idle delay, which is correct. |
| S-4 | **Closed.** The sequence is in `AttractModeSlideshow`; the code-behind that remains is animation plumbing beside the markup it animates. |
| C-5 | **Closed for this epic.** No timing literal remains in attract mode code — the values are `[DefaultValue]` attributes on `EclipseSettings`. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-18 | Introduces a presenter interface — **delivered for this epic**. `IAttractModePresenter` exists and `MainWindowView` is out of the attract mode path entirely. Still open for `EPIC-PRESENT`. |
| B-23 | **Satisfied for this epic** — attract mode no longer owns any undisposed timer. Still open for the other subsystems it spans. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-ATTRACT-001` … `VER-ATTRACT-003`.

**Currently automated:** none.
**Highest-value gap:** `RULE-ATTRACT-004`. The sequence is now written down here *and* is a
single readable loop in `AttractModeSlideshow.RunAsync`, but nothing asserts it. The seam
`B-18` called for exists, so a recording fake behind `IAttractModePresenter` — asserting call
order and delays against an injected `AttractModeTimings` — is now straightforward. It is
blocked only on `B-01`, standing up the first test project.

## Open questions

`OQ-015` — see [UNRESOLVED.md](../UNRESOLVED.md).

## Parked enhancements

Feature and look-and-feel ideas deliberately excluded from the refactor:
[../plans/attract-mode-enhancements.md](../plans/attract-mode-enhancements.md).
