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
rating mode where left and right adjust the user's star rating in half-star steps.

**FEAT-CURATE-003 † — Persistence.** Changes are written back to the LaunchBox library.

**FEAT-CURATE-004 † — List refresh.** Because favourites and ratings can determine
custom-list membership, the lists are rebuilt and the user's position restored.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-CURATE-001 | Rating adjusts in ±0.5 steps and is clamped to the range 0–5. | |
| RULE-CURATE-002 | Rating changes are applied to the in-memory game immediately and displayed immediately, but are **saved only when rating mode is exited** — by Enter, Up, Down or Escape. | The user can scrub the rating without a write per step. |
| RULE-CURATE-003 | Favourite is saved immediately on toggle. | Asymmetric with rating — see `OQ-014`. |
| RULE-CURATE-004 | Toggling favourite updates every copy of that game across all category projections, not just the visible one. | The same game exists many times in the index (`RULE-BROWSE-001`); missing this would leave stale copies. |
| RULE-CURATE-005 | Saving a rating re-resolves the game's rating imagery and refreshes it on screen without a full media re-resolution. | |
| RULE-CURATE-006 | Curation marks the lists dirty; the rebuild happens when the detail overlay is **closed**, not at the moment of the change. | Avoids the row shifting under the user mid-interaction. |
| RULE-CURATE-007 | Page Up / Page Down are deliberately ignored while in rating mode. | The original author judged it ambiguous whether they should save or cancel. Preserve this. |
| RULE-CURATE-008 | Rating is the user's own star rating; the community rating is displayed but never modified. | Two separate values, two separate images. |

---

## Current implementation

| Concern | Location |
|---|---|
| Favourite toggle & propagation | `View/MainWindowViewModel.cs` — `FavoriteCurrentGame` |
| Rating adjust / save | `View/MainWindowViewModel.cs` — `RateCurrentGame`, `SaveRatingCurrentGame` |
| Rating-mode input | `State/GameDetailOptionRatingState.cs` |
| Favourite input | `State/GameDetailOptionFavoriteState.cs` |
| Deferred rebuild | `View/MainWindowViewModel.cs` — `CheckResetGameLists`; the `OnEscape` handlers of the detail states |
| Value exposure | `Models/GameMatch.cs` — `Favorite`, `UserRating` (write straight through to `IGame`) |
| Rating imagery refresh | `Models/GameFiles.cs` — `ResetStarRatingImage`; `View/MainWindowView.xaml.cs` — `UpdateRatingImage` |

LaunchBox SDK dependencies: `IGame.Favorite`, `IGame.StarRatingFloat`,
`PluginHelper.DataManager.Save(false)`.

## Technical debt

| Finding | Effect |
|---|---|
| S-1 | Curation logic and its persistence call live in the god view model. |
| S-2 | `GameMatch.Favorite`/`UserRating` setters mutate `IGame` directly — a model type writing to the SDK. |
| S-9 | Each curation change rebuilds every list in every category. |
| M-4 | The refresh callback into the view is a never-detached delegate. |

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
