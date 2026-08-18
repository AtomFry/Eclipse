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
| RULE-ATTRACT-002 | The idle delay is configurable; attract mode can be disabled entirely, in which case no timer is created at all. | |
| RULE-ATTRACT-003 | Attract mode games are chosen at random from the whole library, **not** from the currently browsed list set, and **each game is equally likely** - selection is never weighted by how many genres, playlists or title words a game has. | Unlike `RULE-BROWSE-011`. Until the index refactor the implementation contradicted this rule: it picked from the flat game bag, where a game appeared once per category value and once per voice phrase, so a 55-entry game was ~7x likelier than an 8-entry one. |
| RULE-ATTRACT-004 | The slideshow sequence per game is: fade to black → wait 4s → pick game, fade in background over 3s and begin a 17s pan → wait 4s → fade in logo over 1.5s → wait 15s from the pan start → fade out background over 3s and logo over 0.5s → repeat. | These timings are the entire feel of the feature and exist only in code. |
| RULE-ATTRACT-005 | The pan direction alternates between games, and the pan distance is the difference between the image width and the monitor width. | Images narrower than the monitor pan the other way. |
| RULE-ATTRACT-006 | Attract mode is not entered while a game is running. | |
| RULE-ATTRACT-007 | Starting a video preview stops attract mode; the video ending restarts the idle timer. | |
| RULE-ATTRACT-008 | Exiting returns to the exact state that was active when attract mode started, not to a default state. | The user resumes mid-overlay if that is where they were. |

---

## Current implementation

| Concern | Location |
|---|---|
| Idle timer and entry | `Service/AttractModeService.cs` — `RestartAttractMode`, `StopAttractMode`, `AttractModeDelay_Elapsed` |
| Slideshow sequencing | `State/AttractModeState.cs` — three timers |
| Fades, pan, image swap | `View/MainWindowView.xaml.cs` — `AttractModeFadeToBlack`, `AttractModeFadeInAndSlideBackground`, `AttractModeFadeInLogo`, `AttractModeFadeOutBackgroundAndLogo`, `AttractModeTurnOff` |
| Game selection | `View/MainWindowViewModel.cs` - `NextAttractModeGame`, indexing `GameCatalog.Games` directly |
| Timer restarts | Every `EclipseState` implementation |
| Suppression | `View/MainWindowViewModel.cs` — `IsPlayingGame`; `View/MainWindowView.xaml.cs` — video handlers |

## Technical debt

| Finding | Effect |
|---|---|
| M-8 | **Both** `AttractModeState` and `AttractModeService` hold a concrete `MainWindowView`. This epic is the single worst layering violation in the product. |
| M-4 | Three timers created in the state constructor are stopped but never disposed. |
| S-4 | The visual sequence lives entirely in view code-behind. |
| C-5 | Every timing constant is an unexplained literal. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-18 | Introduces a presenter interface — **this epic is the primary driver for that item**. |
| B-23 | Disposes the timers. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-ATTRACT-001` … `VER-ATTRACT-003`.

**Currently automated:** none.
**Highest-value gap:** `RULE-ATTRACT-004`. The timing sequence is unspecified outside the
code, and `B-18` rewrites exactly the seam it runs through. Write the sequence down
before starting that item — a recording fake asserting call order and delays is
straightforward once the presenter interface exists.

## Open questions

`OQ-015` — see [UNRESOLVED.md](../UNRESOLVED.md).
