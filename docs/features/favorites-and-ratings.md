# EPIC-CURATE — Favourites & Ratings

**Scope.** Letting the user mark favourites and set star ratings from inside Big Box, and
writing those changes back to LaunchBox. Curation is the only place Eclipse *writes* to
the LaunchBox library.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-CURATE-001 — Toggle favourite.** From the detail overlay, Enter on the Favourite
option toggles the current game's favourite status. The label reflects the current state
("Add to favorites" / "Remove from favorites").

**FEAT-CURATE-002 — Set rating.** From the detail overlay, the Rating option enters a
rating mode where left and right adjust the user's star rating in half-star steps. Enter, Up
or Down commit the change; Escape cancels it.

**FEAT-CURATE-003 † — Persistence.** Changes are written back to the LaunchBox library.

**FEAT-CURATE-004 † — List refresh.** Because favourites and ratings can determine
custom-list membership, the lists are rebuilt and the user's position restored.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-CURATE-001 | Rating adjusts in ±0.5 steps and is clamped to the range 0–5. | |
| RULE-CURATE-002 | Rating changes apply to the in-memory game immediately so the stars track as the user moves them, and are **written to LaunchBox when the editor is committed** — by Enter, Up or Down. A commit with nothing changed does not write. | The user can scrub the rating without a save per step, and repeated Enter presses no longer each trigger a full library save. |
| RULE-CURATE-003 | Favourite is saved immediately on toggle. | Asymmetric with rating — see `OQ-014`. |
| RULE-CURATE-004 | Toggling favourite is visible everywhere that game appears. | Since the index refactor a game is a single object referenced from many category buckets, so this now holds by construction; it previously required updating every clone. |
| RULE-CURATE-005 | The displayed rating follows the underlying value through data binding, with no explicit refresh step. | Replaces a re-resolve of pre-rendered rating imagery pushed to the view through a callback. |
| RULE-CURATE-006 | Curation marks the lists dirty; the rebuild happens when the detail overlay is **closed**, not at the moment of the change. | Avoids the row shifting under the user mid-interaction. |
| RULE-CURATE-007 | Page Up / Page Down are deliberately ignored while in rating mode. | The original author judged it ambiguous whether they should save or cancel. Preserve this. |
| RULE-CURATE-008 | Rating is the user's own star rating; the community rating is displayed but never modified. | Two separate values, drawn as two layers of the same control — user over community, over a faint five-star base. |
| RULE-CURATE-009 | **Escape cancels a rating edit**, restoring the value the editor opened with. | The rating is written into the game as the user moves it, so before this the change persisted even though Eclipse never saved it — Escape looked like a cancel but was not one. |

---

## Current implementation

| Concern | Location |
|---|---|
| Favourite toggle & propagation | `View/MainWindowViewModel.cs` — `FavoriteCurrentGame` |
| Rating adjust / commit / cancel | `View/MainWindowViewModel.cs` — `RateCurrentGame`, `BeginRatingCurrentGame`, `SaveRatingCurrentGame`, `CancelRatingCurrentGame` |
| Rating-mode input | `State/GameDetailOptionRatingState.cs` |
| Favourite input | `State/GameDetailOptionFavoriteState.cs` |
| Deferred rebuild | `View/MainWindowViewModel.cs` — `CheckResetGameLists`; the `OnEscape` handlers of the detail states |
| Value exposure | `Models/GameMatch.cs` — `Favorite`, `UserRating` (write straight through to `IGame`), `CommunityRating` (read only) |
| Star rendering | `View/StarRatingView.xaml(.cs)` — one control for both the details display and the rating editor |

LaunchBox SDK dependencies: `IGame.Favorite`, `IGame.StarRatingFloat`,
`PluginHelper.DataManager.Save(false)`.

## Technical debt

| Finding | Effect |
|---|---|
| S-1 | Curation logic and its persistence call live in the god view model. |
| S-2 | `GameMatch.Favorite`/`UserRating` setters mutate `IGame` directly — a model type writing to the SDK. |
| S-9 | Each curation change rebuilds every list in every category. |
| M-4 | **Resolved for this epic.** The view refresh callback (`UpdateRatingImageFunction`) is gone; the rating display is bound. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-11 | Moves the save call behind an adapter. |
| B-12 | **Important:** mutation must become explicit rather than a property-setter side effect. |
| B-16 | Makes the post-curation rebuild incremental — must not break `RULE-CURATE-004`. |
| B-14 | Position restoration after the rebuild. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-CURATE-001` … `VER-CURATE-004`.

**Currently automated:** none.
**Highest-value gap:** `RULE-CURATE-004` (propagation across all projections) and
`RULE-CURATE-006` (deferred rebuild). Both are easy to break with `B-16` and both are
user-visible — a favourite that does not appear in the Favorites list until restart.

## Open questions

`OQ-014` — see [UNRESOLVED.md](../UNRESOLVED.md).
