# EPIC-BROWSE — Game Discovery & Browsing

**Scope.** Turning the LaunchBox library into browsable, ordered lists of games, and
moving around them. Owns list construction, list membership, ordering, navigation and
browsing position. Does not own what those games look like on screen (EPIC-PRESENT) or
how their media is found (EPIC-MEDIA).

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### The browsing model

Eclipse presents the library as a **list set**: an ordered collection of named lists,
each containing an ordered collection of games. The user moves horizontally within a
list and vertically between lists. Exactly one game is "current" at any time.

The user chooses which list set is active by picking a **category** — platform, genre,
series, playlist, play mode, developer, publisher or release year. Voice search and
"more like this" produce list sets too, so results from those features are browsed
identically to a category.

### Features

**FEAT-BROWSE-001 — Browse by category.** Selecting a category rebuilds the active list
set, one list per distinct value of that category, each containing the games carrying
that value. A game appears in every list whose value it carries — a game with three
genres appears in three genre lists.

**FEAT-BROWSE-002 — Custom lists.** Users define named lists with filter expressions,
sort expressions and an optional maximum size. Custom lists are pinned above the
category lists in the list set. Each definition declares which categories it appears in.
Two are created on first run: *Favorites* and *History*.

**FEAT-BROWSE-003 — Playlists among platforms.** When browsing by platform, LaunchBox
playlists that are marked to be included with platforms also appear as lists.

**FEAT-BROWSE-004 — Navigate within a list.** Left and right move the selection through
the current list, wrapping at both ends.

**FEAT-BROWSE-005 — Navigate between lists.** Up and down move between lists in the set,
wrapping at both ends.

**FEAT-BROWSE-006 — Page jump.** A configurable input jumps several games at once within
the current list, wrapping.

**FEAT-BROWSE-007 — Random game.** Selects a game uniformly at random from the whole
active list set and navigates to it, wherever it lives.

**FEAT-BROWSE-008 — More like this.** Builds a new list set from the current game's
series, genres, platform, developers, publishers, play modes and release year, and
switches to browsing it.

**FEAT-BROWSE-009 — Category picker.** A pane listing the available categories plus
*Random* and, when enabled, *Voice search*. The user's default category is pre-selected.

**FEAT-BROWSE-010 — Position restoration.** After the lists are rebuilt, Eclipse returns
the user as close as possible to where they were. See `RULE-BROWSE-010`.

**FEAT-BROWSE-011 — Repeat to fill.** When a list has fewer games than the row can show,
games repeat so the row is visually full rather than showing gaps. Configurable.

**FEAT-BROWSE-012 — Game counts.** List titles may include the number of games.

**FEAT-BROWSE-013 † — Library filtering.** Games marked broken or hidden in LaunchBox are
excluded unless the user opts them in.

**FEAT-BROWSE-014 † — Game index.** A single in-memory index of the library, keyed by
category, is built once at startup and is the substrate for every list, random selection
and voice match.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-BROWSE-001 | A game appears once per distinct category value it carries, so the same game legitimately appears in multiple lists of one set. | Deduplicating would silently change list membership. |
| RULE-BROWSE-002 | Category lists are ordered by list name; games within a category list are ordered by LaunchBox sort-title-or-title. | Ordering is user-visible and relied upon for navigation muscle memory. |
| RULE-BROWSE-003 | Custom lists sort ahead of category lists, in their defined order; category lists follow, name-ordered. | Users arrange custom lists deliberately (FEAT-CONFIG-006). |
| RULE-BROWSE-004 | A custom list with no matching games is omitted from the set entirely. | Empty lists would otherwise appear as blank rows. |
| RULE-BROWSE-005 | Custom list filters are applied to the platform-category projection of the index, not to the raw library. | Changing the source projection changes membership. |
| RULE-BROWSE-006 | When a custom list defines sort expressions they replace the default sort; the first expression establishes order and later ones refine it. | Multi-key ordering is meaningful for e.g. History. |
| RULE-BROWSE-007 | A custom list size cap is applied *after* sorting, so the cap selects the top N by the defined order. | Applying the cap first would produce arbitrary members. |
| RULE-BROWSE-008 | Navigation wraps in all four directions. | Wrapping at list ends is core to the browsing feel. |
| RULE-BROWSE-009 | Page jump moves a fixed number of games; for lists shorter than that, it moves half the list length instead. | Prevents a page jump from being a no-op or a full loop on short lists. |
| RULE-BROWSE-010 | After a list rebuild, position is restored by trying in order: the same game in the same list; the same index in that list; the previous index; the first game in that list; a random game. | **High risk.** Subtle, four-deep fallback, no automated coverage. |
| RULE-BROWSE-011 | Random game selects an index across the whole list set, then locates which list owns that index — so selection is weighted by list size. | A game in a large list is more likely than one in a small list. This is observable and may or may not be intended (`OQ-002`). |
| RULE-BROWSE-012 | "More like this" appends one list per matching series, genre, platform, developer, publisher, play mode and release year, in that order, and does not deduplicate. | The same game appears many times; the ordering is the relevance signal. |
| RULE-BROWSE-013 | The category picker's default option is the configured default category and is sorted first; remaining options follow a fixed order. | Ordering is stable so muscle memory works. |
| RULE-BROWSE-014 | Broken/hidden filtering happens once during index construction, not per list. | Changing it requires a full index rebuild, i.e. a restart. |
| RULE-BROWSE-015 | The row shows a fixed window of games around the selection; the window size is fixed at 13 slots. | Coupled to presentation; see FEAT-PRESENT-001. |
| RULE-BROWSE-016 | With repeat-to-fill disabled, slots beyond the list length are empty rather than wrapped. | Directly user-visible on short lists. |

---

## Current implementation

*This section describes where the behaviour lives today and is expected to change.*

| Concern | Location |
|---|---|
| Game index construction | `Service/GameCatalog.cs` - `Setup()`, `BuildCategoryIndex()` |
| Library filtering | `Service/GameCatalog.cs` (broken/hidden checks) |
| Category list construction | `View/MainWindowViewModel.cs` — `GetGamesByListCategoryType`, `CreateGameLists` |
| Custom list filtering/sorting | `View/MainWindowViewModel.cs` — `CustomGameListServiceExtensionMethods` (`ApplyDynamicFilter`, `ApplyOrder`) |
| Custom list definitions | `Service/CustomListDefinitionDataProvider.cs`; `Models/EclipseSettings.cs` (`CustomListDefinition`) |
| Playlist inclusion | `Service/PlaylistGameService.cs` |
| List set / list model | `Models/GameList.cs` (`GameListSet`, `GameList`) |
| Navigation window | `Models/ListCycle.cs` |
| Random game / index location | `View/MainWindowViewModel.cs` — `DoRandomGame` |
| More like this | `View/MainWindowViewModel.cs` — `DoMoreLikeCurrentGame` |
| Position restoration | `View/MainWindowViewModel.cs` — `SaveStateForGameListChange`, `ResetListsAfterChange`, `CheckResetGameLists` |
| Category picker | `Service/OptionListService.cs`; `Models/Option.cs`; `State/SelectingOptionsState.cs` |
| Navigation input handling | `State/SelectingGameState.cs`, `State/KeyStrategy/KeyStrategyPageUp.cs`, `KeyStrategyPageDown.cs` |

LaunchBox SDK dependencies: `PluginHelper.DataManager.GetAllGames()`, `GetAllPlaylists()`,
and `IGame` metadata members throughout.

## Technical debt

| Finding | Effect on this epic |
|---|---|
| S-1 | List construction, the query engine, random selection and position restoration all live in `MainWindowViewModel`. |
| S-2 | `IGame` is the list element type, so no list logic can be tested without the host. |
| S-8 | Index construction clones a `GameMatch` per category value per game — the dominant startup cost and memory footprint. |
| S-9 | Any curation change rebuilds every list in every category. |
| S-10 | `MatchCount` re-enumerates per read; read 12× per navigation step. |
| S-3 | The 13-slot window is hard-coded as 13 properties. |
| C-2 | The category picker's dispatch is a 10-arm switch, 8 arms identical. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-14 | Extracts position restoration — **the highest-value characterization target in this epic**. |
| B-15 | Extracts list construction and the query engine. |
| B-16 | Makes list rebuild incremental after curation. |
| B-17 | Collapses the category-picker switch. |
| B-07 | Memoises `MatchCount`. |
| B-30 | Reduces index fan-out — gated on measurement (B-28). |
| B-12 | Replaces `IGame` with an Eclipse-owned model; **highest risk to this epic** because list membership and ordering depend on its members. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-BROWSE-001` … `VER-BROWSE-008`.

**Currently automated:** none.
**Highest-value gap:** `RULE-BROWSE-010` (position restoration) and `RULE-BROWSE-005`/`006`/`007`
(custom-list membership and ordering).

## Open questions

`OQ-001`, `OQ-002`, `OQ-003`, `OQ-004` — see [UNRESOLVED.md](../UNRESOLVED.md).
