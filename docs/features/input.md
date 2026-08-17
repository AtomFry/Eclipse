# EPIC-INPUT — Input & Navigation

**Scope.** How controller and keyboard input reaches Eclipse, how it is routed to
behaviour, and which inputs the user can remap. Eclipse does not read devices directly —
Big Box does, and calls Eclipse.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### The input contract

Big Box delivers eight inputs to Eclipse: **Up, Down, Left, Right, Enter, Escape,
Page Up, Page Down**. Directional inputs also indicate whether the key is being *held*.
For each input Eclipse answers a single question: *did I handle it?* If Eclipse says no,
Big Box handles it — which is how the user reaches Big Box's own menus.

This is the complete input surface. Eclipse cannot observe any other key, button, or
gesture.

### Features

**FEAT-INPUT-001 — Input contract.** The eight inputs above, plus a selection-changed
notification from Big Box that Eclipse ignores.

**FEAT-INPUT-002 — Remappable page keys.** Page Up and Page Down are each independently
assignable to one of ten functions: page up, page down, random game, voice search, flip
box, zoom box, volume up, volume down, show details, or play game. Defaults are random
game (Page Up) and voice search (Page Down).

**FEAT-INPUT-003 — Held keys.** Directional inputs indicate whether the key is held,
which changes behaviour at list boundaries.

**FEAT-INPUT-004 — Volume from input.** Video preview volume can be bound to the page
keys and adjusts in 0.05 steps.

**FEAT-INPUT-005 — Escape routing.** Escape either opens the options pane or is declined
so Big Box shows its own menu, depending on configuration and current state.

**FEAT-INPUT-006 † — State routing.** Every input is routed through the current state, so
the same key does different things in different contexts.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-INPUT-001 | Returning `false` from an input handler hands the input to Big Box. This is the only way the user reaches Big Box's menus. | Returning `true` everywhere would trap the user inside Eclipse. |
| RULE-INPUT-002 | Left at the first game in a list opens the options pane — but **only when the key is not held**. Holding left instead wraps to the end of the list. | Lets a user scroll through a list without falling into the options pane. |
| RULE-INPUT-003 | When "open settings pane on left" is disabled, left always navigates and never opens the pane. | |
| RULE-INPUT-004 | Escape from browsing opens the options pane when configured to do so; otherwise it is declined and Big Box takes over. | This is how the user exits Big Box. |
| RULE-INPUT-005 | Escape from the options pane is always declined, handing control to Big Box. | |
| RULE-INPUT-006 | Up from the first list enters the featured view when featured game is enabled; otherwise it wraps to the last list. | |
| RULE-INPUT-007 | Each remappable function declares which states it is valid in and is a no-op elsewhere. Volume is valid in every state. | A remapped key silently doing nothing in some states is intended, not a bug. |
| RULE-INPUT-008 | The page-key strategies are resolved once, on first use, and cached for the process lifetime. | Changing the mapping requires a restart. |
| RULE-INPUT-009 | Any input while the running-game flag is set clears that flag. | See `RULE-LAUNCH-007` and `OQ-013`. |
| RULE-INPUT-010 | Input during loading is accepted and discarded, not passed to Big Box. | Prevents interaction with a half-built list set. |
| RULE-INPUT-011 | Page Up/Down are ignored in rating mode by design. | See `RULE-CURATE-007`. |

---

## Current implementation

| Concern | Location |
|---|---|
| Input contract | `View/MainWindowView.xaml.cs` — `IBigBoxThemeElementPlugin` implementation |
| Routing to state | `View/MainWindowViewModel.cs` — `DoUp`, `DoDown`, …; `State/EclipseStateContext.cs` |
| Per-state handling | `State/*State.cs` — one method per input |
| Remapping | `State/KeyStrategy/IKeyStrategy.cs` (`KeyStrategyCache`) and the ten `KeyStrategy*` classes |
| Page jump size | `Helpers/EclipseConstants.cs` — `GamesToPage` |
| Volume | `State/KeyStrategy/KeyStrategyVolumeUp.cs`, `KeyStrategyVolumeDown.cs`; `View/MainWindowViewModel.cs` — `AdjustVideoVolume` |

LaunchBox SDK dependency: `IBigBoxThemeElementPlugin`.

## Technical debt

| Finding | Effect |
|---|---|
| S-1 | Input entry points sit on the god view model. |
| M-8 | State handlers reach the view through non-UI layers. |
| C-5 | Page jump size and volume step are unexplained constants. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-18, B-19 | Decouple state from the view, making input handling testable headlessly. |
| B-26 | Remapping currently requires a restart (`RULE-INPUT-008`); live settings would change that — an **intended behaviour change** if pursued. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-INPUT-001` … `VER-INPUT-004`.

**Currently automated:** none.
**Highest-value gap:** `RULE-INPUT-001`/`004`/`005` — the handled/declined contract. If a
refactor accidentally returns `true` where it returned `false`, the user can no longer
exit Big Box, and no test would catch it.

## Open questions

`OQ-016` — see [UNRESOLVED.md](../UNRESOLVED.md).
