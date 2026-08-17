# EPIC-PRESENT — Game Presentation

**Scope.** What the user sees: the box art row, the selected-game details, the detail
overlay, the featured-game view, and the loading and error states. Owns layout and view
modes. Does not own which games are shown (EPIC-BROWSE) or where their media comes from
(EPIC-MEDIA).

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### The screen

The interface is a full-screen layout with a background image or video for the selected
game, a horizontal row of box art representing the current list, and a details area for
the selected game. Moving the selection changes the background, details and video.

### Features

**FEAT-PRESENT-001 — Box art row.** A horizontal row showing a window of games from the
current list, with the selected game emphasised. The window is 13 slots wide; one slot
behind the selection is shown so the user can see where they came from.

**FEAT-PRESENT-002 — Selected game details.** Title, clear logo, release year, match
percentage, community star rating, user star rating, play-mode icon and platform logo.
Each can be individually hidden (FEAT-PRESENT-009).

**FEAT-PRESENT-003 — Detail overlay.** Pressing Enter on a game opens an overlay with
four options arranged in a vertical ring: **Play**, **Favourite**, **Rating** and
**More like this**. Up/down move between them; Enter activates the current one; Escape
closes the overlay.

**FEAT-PRESENT-004 — Featured game.** When enabled, pressing Up from the first list
shows a large featured presentation of the current game with Play and More Info options,
rather than wrapping to the last list.

**FEAT-PRESENT-005 — Flip box.** Swaps the front and back box art for the current game,
in both the row and the enlarged view.

**FEAT-PRESENT-006 — Zoom box.** Toggles an enlarged view of the current game's box art.

**FEAT-PRESENT-007 — Loading state.** While the library is being indexed at startup, a
loading indication is shown and input is absorbed.

**FEAT-PRESENT-008 — Error state.** A dedicated state displays an error message; the
user dismisses it to return to browsing.

**FEAT-PRESENT-009 — Detail field visibility.** Match percent, release year, star
rating, play mode, platform logo and the options icon each have an independent on/off
setting.

**FEAT-PRESENT-010 — Spacing controls.** Box-art margins (four sides) and selected-game
details padding are configurable.

**FEAT-PRESENT-011 — Options icon.** An affordance indicating the options pane is
available.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-PRESENT-001 | Changing the selected game does not immediately swap the background and details. A 1-second idle delay elapses first; holding a direction restarts the delay. | Prevents thrashing while scrolling. Directly shapes the browsing feel. |
| RULE-PRESENT-002 | On selection change, the background, logo and details are dimmed immediately; they fade back in when the idle delay elapses. | The dim is instant feedback; the fade-in is deferred. |
| RULE-PRESENT-003 | The game title text is a fallback for a missing clear logo — the logo fades to 0.15 opacity while the title fades to 0. | Both are always present; opacity decides which is seen. |
| RULE-PRESENT-004 | The slot behind the selection is blanked when the selection is at index 0 of the list. | Avoids showing a wrapped game as "previous" at the start. |
| RULE-PRESENT-005 | The detail overlay ring order is Play → Favourite → More → Rating → Play when moving down, and the reverse moving up. | Non-obvious: More sits between Favourite and Rating. |
| RULE-PRESENT-006 | Leaving the detail overlay with Escape triggers a deferred list rebuild if curation changed anything while the overlay was open. | Lists refresh on exit, not on each change. |
| RULE-PRESENT-007 | In the featured view, Down returns to the list; Enter plays. Featured view is only reachable from the first list. | |
| RULE-PRESENT-008 | Flip box swaps both the scaled row image and the full-size image together, and the swap persists until flipped back or the game's media is re-resolved. | State lives on the game's media, not on the view. |
| RULE-PRESENT-009 | Zoom is a toggle that persists across navigation until toggled off. | |
| RULE-PRESENT-010 | Background image and video occupy different grid spans in featured mode versus normal mode. | Layout constants; see `C-5`. |
| RULE-PRESENT-011 | Rating display updates immediately as the rating is adjusted, before it is saved. | Adjustment is live; persistence is separate (EPIC-CURATE). |

---

## Current implementation

| Concern | Location |
|---|---|
| Layout and bindings | `View/MainWindowView.xaml` — 1,756 lines, 165 bindings, 97 converter references, 30 multi-bindings |
| Animation, fades, timing | `View/MainWindowView.xaml.cs` — `DoAnimateGameChange`, `FadeInCurrentGame`, `FadeForMovie`, `FadeFrameworkElementOpacity` |
| View-state flags | `View/MainWindowViewModel.cs` — `IsDisplayingResults`, `IsDisplayingFeature`, `IsDisplayingMoreInfo`, `IsPickingCategory`, `IsZoomingBox`, `IsRatingGame`, `IsRecognizing`, `IsInitializing`, `IsDisplayingError` |
| Row slots | `Models/GameList.cs` — `Game0`…`Game12`, `RefreshGames()` |
| Detail overlay states | `State/GameDetailOptionPlayState.cs`, `…FavoriteState`, `…RatingState`, `…MoreState` |
| Featured view states | `State/FeatureOptionPlayState.cs`, `State/FeatureOptionMoreInfoState.cs` |
| Loading / error states | `State/LoadingState.cs`, `State/DisplayingErrorState.cs` |
| Flip / zoom | `State/KeyStrategy/KeyStrategyFlipBox.cs`, `KeyStrategyZoomBox.cs` |
| Layout constants | `Helpers/EclipseConstants.cs` |
| Value conversion | `Converters/` — 24 converters |
| Opacity mask | `Helpers/OpacityBrushHelper.cs` |

## Technical debt

| Finding | Effect |
|---|---|
| S-4 | Animation sequencing and presentation decisions live in 684 lines of code-behind. |
| S-3 | The 13-slot window is 13 hard-coded properties and 68 XAML references. |
| M-8 | The state machine and a service hold the concrete view, so presentation cannot be substituted. |
| S-11 | A WPF `Brush` is exposed from a model type. |
| C-1 | 73 magic-string `PropertyChanged` raises drive these bindings. |
| C-3 | Near-duplicate converters. |
| C-5 | Layout constants are unexplained magic numbers. |
| M-4 | Timers and handlers driving fades are never detached. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-18 | Removes the concrete view from state/services (attract-mode presenter). |
| B-19 | Replaces the view↔viewmodel callback delegates. |
| B-31 | Replaces the fixed 13-slot window. **Measure navigation latency first** — the fixed slots may be a deliberate performance choice. |
| B-32 | Removes `PropertyChanged` boilerplate — must preserve setters that deliberately notify without an equality check. |
| B-33 | Consolidates converters. |
| B-21 | Removes WPF types from the model layer. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-PRESENT-001` … `VER-PRESENT-006`.

**Currently automated:** none.
**Highest-value gap:** `RULE-PRESENT-001`/`002` — the idle-delay and fade sequence has no
written specification anywhere except the code, and it is the behaviour most likely to be
altered accidentally by `B-19` or `B-31`.

## Open questions

`OQ-007`, `OQ-008` — see [UNRESOLVED.md](../UNRESOLVED.md).
