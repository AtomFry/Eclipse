# Plan — How game lists are navigated

**Status: complete. Stages 1-3 and all four parts of Stage 4 delivered and verified.**

## What was asked

Performance of the row is settled and is not the concern. Review how game lists are *managed and
navigated* and find where the technical design can be improved — same functionality, same
performance, less debt.

**The short version.** There is one structural problem and everything else follows from it:
navigation runs in **two coordinate systems**, and almost every operation converts from one to
the other and back for no reason. Removing the second one deletes a linear search from every
page jump, removes eighty lines of duplicated key-strategy code, and makes `OQ-022` — the
more-like-this index corruption — structurally impossible rather than merely unfixed.

---

## N1 — Two coordinate systems, and a round trip between them

A game's position is expressed two ways:

| Coordinate | Where it lives | Meaning |
|---|---|---|
| **List-local** | `GameList.CurrentGameIndex` | index of the selected game within its list |
| **Set-global** | `GameList.ListSetStartIndex` + local index | index across every game in every list of the set |

The set-global one exists to serve exactly one feature: random game, which picks a number in
`[0, TotalGameCount)` so that selection is weighted by list size (`RULE-BROWSE-011`).

Everything else pays for it. `DoRandomGame(int)` is the only jump-to-a-game entry point, it takes
a **set-global** index, and it finds the target by scanning every list in the set:

```csharp
for (int listIndex = 0; listIndex < CurrentGameListSet.GameLists.Count; listIndex++)
{
    GameList gameList = CurrentGameListSet.GameLists[listIndex];
    if (gameList.ListSetStartIndex <= randomIndex && gameList.ListSetEndIndex >= randomIndex)
    { ... }
}
```

Now look at what calls it. Of the nine call sites, **one** is actually random:

| Caller | What it already knows | What it does |
|---|---|---|
| Page up / page down | the current list, and the target index within it | adds `ListSetStartIndex`, calls `DoRandomGame`, which scans every list to find the current list again |
| Position restoration ×4 | the exact `GameList` object and the index within it | same round trip, four times |
| Random game ×2 | nothing | the only genuine use |

Page up knows it is staying in the current list. Position restoration is *holding the `GameList`
reference* when it converts to a global index and hands it to a search that finds that same
object. Both convert local→global→local through a linear scan.

**And random game does not need the stored indices either.** Picking `n` in `[0, total)` and
walking the lists accumulating `MatchCount` gives the same weighted selection with no stored
state at all.

So `ListSetStartIndex` has one writer, fifteen readers, and no reason to exist.

## N2 — `ListSetStartIndex` is why `OQ-022` exists

`GameListSet.GameLists`'s setter walks the lists it is given and writes `ListSetStartIndex` on
each. More-like-this builds a set from `GameList` instances **borrowed from the genre, platform
and series sets**, so assigning that set rewrites the start indices belonging to those sets, and
random game and position restoration then navigate by corrupted values until the next rebuild.

`OQ-022` is currently recorded as a defect awaiting a decision. **Delete the coordinate system
and the defect cannot occur** — there is no derived state on a shared object left to corrupt.
That is a better outcome than fixing it, and it comes free with N1.

## N3 — `DoRandomGame` is three operations wearing one name

It is the random picker, the jump-to-index primitive, and the page-jump implementation. Its
name matches the first and its signature serves the third. `DoRandomGame(gameList.ListSetStartIndex)`
— "restore my position" — reads as nonsense at the call site.

## N4 — Page up and page down are 112 lines of near-identical code

Two 56-line files differing in `++` versus `--` and which bound they wrap at. Both:

- recompute the list length from `ListSetEndIndex - ListSetStartIndex + 1`, when `MatchCount`
  is that number;
- step one index at a time in a loop to move seven places, instead of arithmetic;
- convert to set-global coordinates purely to call `DoRandomGame`.

The whole of both files is one method with a signed delta, in list-local coordinates.

## N5 — Half the navigation operations notify; the other half require the caller to

| Operation | Raises the game-change notification? |
|---|---|
| `CycleListForward` / `CycleListBackward` | **yes**, via `RefreshGameLists` |
| `DoRandomGame` | **yes**, at the end |
| `CurrentGameList.CycleForward` / `CycleBackward` | **no** — the caller must |

So `SelectingGameState.OnRight` and `OnLeft` each call `CallGameChangeFunction()` by hand while
`OnUp` and `OnDown` must not. Nothing in the API says which is which; you find out by reading
both sides. A new navigation operation is one forgotten line away from a row that moves without
the background and details following it.

## N6 — States do index arithmetic on the view model's internals

```csharp
// SelectingGameState.OnUp
if (... && eclipseStateContext.MainWindowViewModel.listCycle.GetIndexValue(0) == 0)
```

`listCycle` is a **public field** on the view model, and a state class is reaching through it to
ask "am I on the first list?" — a question that deserves a name. `GameListSets` is a public
field too.

## ~~N7 — Key strategies decide whether their caller was allowed to call them~~ — **withdrawn**

The original claim: `GameDetailOptionsState.OnPageDown` invokes `PageDownStrategy`, whose
`IsValidForState` returns true only for `SelectingGameState`, so paging in the overlay reports
the key as handled and does nothing — a rule about the overlay living inside the page-jump code.

**That was wrong, and it was wrong because I had not read the settings.** `PageUpFunction` and
`PageDownFunction` bind each physical key to one of ten functions — page, random game, voice
search, flip box, zoom box, volume, details, play. `KeyStrategyCache` resolves the setting;
`PageUpStrategy` does not mean "page up", it means "whatever the user put on that key".

A state with a page key therefore *cannot* know what the key does, so each function has to
declare where it applies. The guard is the mechanism, not a wart, and every strategy has one for
the same reason. Nothing about it should move.

What survives is only the duplication: `KeyStrategyPageUp` and `KeyStrategyPageDown` were two
56-line files differing in `++` versus `--`.

## N8 — Smaller things in the same area

- `GameList.ListCategoryType` is **never assigned** anywhere in the product, so every list
  carries the default. The position-restoration match tests it and it always passes.
- `MatchingGames` is a property whose setter rebuilds the cycle and repopulates the row. It is a
  command spelled as an assignment.
- `GameList` reads `EclipseSettingsDataProvider.Instance` from a field initializer, so no list
  can be constructed without the settings singleton.
- The page amount comes from `EclipseConstants.GamesToPage`, a public mutable static field in a
  class whose own comment is "todo: rework this / this is hacky as shit".

---

## Proposal — one navigator, one coordinate system

Introduce `GameListNavigator`: the object that owns *where the user is* in a set of lists, and
the only thing that moves them.

```
GameListNavigator(GameListSet set)

    GameList CurrentList { get; }
    GameList NextList    { get; }
    bool     IsOnFirstList { get; }

    void MoveToNextGame()        // right
    void MoveToPreviousGame()    // left
    void PageForward()           // page down
    void PageBackward()          // page up
    void MoveToNextList()        // down
    void MoveToPreviousList()    // up
    void MoveToRandomGame()      // weighted by list size, no stored indices
    bool MoveTo(GameList list, int gameIndexInList)   // position restoration

    event EventHandler SelectionChanged               // raised exactly once per move
```

Everything is list-local. There is no set-global index anywhere.

**What this deletes**

| | Now | After |
|---|---|---|
| `ListSetStartIndex` / `ListSetEndIndex` | 1 writer, 15 readers | gone |
| Mutation of borrowed `GameList` objects | the cause of `OQ-022` | gone |
| `KeyStrategyPageUp` + `KeyStrategyPageDown` | 112 lines | one method, ~15 lines |
| Linear scan per page jump and per position restore | every time | none |
| `DoRandomGame` overloads | 3 jobs, 9 call sites | 2 named methods |
| `listCycle`, `GameListSets` as public fields on the view model | exposed | behind the navigator |
| Notification asymmetry (N5) | caller-dependent | one event, one place |

**What it does not change.** Every visible behaviour: wrap in all four directions
(`RULE-BROWSE-008`), page of 7 or half a short list (`RULE-BROWSE-009`), random weighted by list
size (`RULE-BROWSE-011`), the four-deep restoration fallback (`RULE-BROWSE-010`), and the
featured-game entry from the first list. Performance is unchanged — the only complexity that
moves is a linear scan being removed.

**And it is testable.** `GameListNavigator` is lists and integers: no WPF, no LaunchBox, no
settings singleton beyond the page size, which becomes a constructor argument. This is the piece
that would finally give `B-01` something worth covering — and unlike `ListCycle`, it carries
rules that a test would pin rather than just arithmetic.

---

## Stages

Each is independently shippable and behaviour-preserving.

### Stage 1 — Give `GameList` a local-coordinate jump, and use it

`SetGameIndex` already exists and already takes a list-local index. Repoint the four position
restoration calls and both page strategies at it, so they stop converting to set-global.
`DoRandomGame(int)` loses every caller except random itself.

- **Files:** `MainWindowViewModel.cs` (`ResetListsAfterChange`), both page strategies
- **Verify:** `VER-BROWSE-005` (restore after un-favouriting), page up and down in a long list,
  a short list, and at both ends

### Stage 2 — Make random game weighted without stored indices

Walk the lists accumulating `MatchCount` instead of comparing against `ListSetStartIndex`. Then
delete `ListSetStartIndex`, `ListSetEndIndex`, and the mutation in the `GameListSet.GameLists`
setter.

**`OQ-022` closes here** — not by fixing the corruption but by removing the state that was being
corrupted.

- **Files:** `MainWindowViewModel.cs` (`DoRandomGame`), `Models/GameList.cs`
- **Verify:** `VER-BROWSE-008` (random selection weighted across a set with one large and one
  small list); more-like-this followed by random game and by favouriting — the case `OQ-022`
  describes, which should now be correct rather than self-healing

### Stage 3 — Collapse the page strategies — **done**

`KeyStrategyPageUp` and `KeyStrategyPageDown` become one `KeyStrategyPage` carrying a direction,
built through `KeyStrategyPage.Forward()` / `.Backward()` from `KeyStrategyCache`. 112 lines
become 69.

**The behaviour question was answered before it was asked.** The choice was whether paging
should do nothing outside the row (today) or should move the row from anywhere; the answer is
"do nothing", which is what the existing guard already does — and, per the withdrawn N7, the
guard is where it belongs, because the page keys are user-configurable and a state cannot know
what its key is bound to. So nothing moved and no behaviour changed.

- **Files:** `KeyStrategyPage.cs` (new), `KeyStrategyPageUp.cs` and `KeyStrategyPageDown.cs`
  (deleted), `IKeyStrategy.cs` (two construction sites)
- **Verify:** `VER-BROWSE-004`; page keys bound to something other than paging still work; paging
  still does nothing in the detail overlay and options pane

### Stage 4 — Extract `GameListNavigator`

Move current/next list tracking, the six movement operations, random, and restoration into one
type. The view model keeps `CurrentGameList`/`NextGameList` as bound properties fed by the
navigator's event. `listCycle` and `GameListSets` stop being public fields.

The states become one-liners: `OnRight` is `navigator.MoveToNextGame()`, and the notification is
the navigator's, not the caller's — closing N5.

- **Files:** `Service/GameListNavigator.cs` (new), `MainWindowViewModel.cs`,
  `State/SelectingGameState.cs`, `State/SelectingOptionsState.cs`, `State/VoiceRecognitionState.cs`
- **Verify:** every navigation key from every state; `VER-BROWSE-001` … `008`; the featured-game
  entry from the first list

### Stage 5 — Tidy what is left (optional)

`GameList.ListCategoryType` is dead — either assign it or delete it and simplify the restoration
match. `GamesToPage` becomes a real setting or a real constant. `ListCycle`'s consecutive-run
invariant gets documented, or the class gets the one-index rewrite now that the navigator is its
only game-side caller.

---

## Risks

| Risk | Mitigation |
|---|---|
| Navigation is the most-used code in the product | Four independent stages, each behaviour-preserving and separately verifiable by hand |
| Position restoration has a four-deep fallback that is easy to get subtly wrong | Stage 1 only changes the *coordinate*, not the fallback order; `VER-BROWSE-005` covers each branch |
| Random selection weighting could shift | Stage 2's walk must accumulate in the same list order the start indices were assigned in; `VER-BROWSE-008` |
| `OQ-022`'s corruption may be masking a compensating behaviour somewhere | It self-heals on the next rebuild, so nothing can depend on it — but random game and restoration should be exercised immediately after a more-like-this |
| No tests | Stage 4 produces the first genuinely test-worthy unit in the browsing path; `B-01` is worth revisiting at that point |
