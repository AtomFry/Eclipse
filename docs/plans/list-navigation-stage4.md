# Stage 4 — Detailed plan: one owner for "where the user is"

**Status: complete. 4a, 4b, 4c and 4d all delivered and verified.**

Stages 1–3 of `list-navigation-refactor.md` are done. This is the detailed design for Stage 4,
written functionally first and then technically, as asked. **No performance change is intended
or expected** — nothing here adds or removes work per keypress; it moves where the work is
written down.

---

# Part 1 — What this is about, in plain terms

## The feature

Eclipse shows a **row of games**. The row belongs to a **list** — "Nintendo Entertainment
System", "Shooter", "Konami", "Favorites". Lists are grouped into **sets** — all the platform
lists together, all the genre lists together, and so on. At any moment the user is on one game,
in one list, in one set.

The things the user can do to change that:

| Key | What it does |
|---|---|
| Left / Right | move one game within the list |
| Up / Down | move to the previous / next list in the set |
| Page keys | jump seven games (or half a short list) — if bound to paging |
| Random game | jump to a random game anywhere in the set, weighted by list size |
| Options pane | switch to a different set — browse by genre instead of platform |
| Voice search | replace the results set and switch to it |
| More like this | build a set from the current game's genres, series, platform… and switch to it |
| Favourite / rate / launch a game | rebuild the lists and try to put the user back where they were |

Every one of those changes "where the user is". And every one of them has to make the rest of
the screen follow: the background art, the clear logo, the game details, the video.

## What is actually wrong today

Nothing is broken. The problem is that **the rules for "where the user is" are spread across
four files, and one of them is only correct by luck.**

### Problem 1 — Some moves tell the screen to update, and some rely on someone else doing it

There is a single function, `CallGameChangeFunction`, that means "the selection changed, update
everything else". It is what makes the background, the logo, the details and the video follow the
row.

Some navigation operations call it themselves. Others do not, and expect their caller to. There
is nothing that tells you which is which — you find out by reading both sides.

Here is the case that shows why that matters. In the **options pane** (the category picker),
pressing Left moves the selection back one game and closes the pane:

```csharp
// SelectingOptionsState.OnLeft
eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleBackward();   // moves the game
eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
eclipseStateContext.TransitionToState(... SelectingGameState ...);          // closes the pane
```

**It never says the selection changed.** The screen updates anyway — but only because switching
to the browsing state happens to call `CallGameChangeFunction` on the way in. Reorder those two
lines, or change what happens when the browsing state is entered, and the row moves while the
background stays on the old game. Nothing in the code warns you.

The same operation in the *browsing* state does call it:

```csharp
// SelectingGameState.OnLeft
eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleBackward();
eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();          // ...here it does
```

Two places, same operation, different obligations.

### Problem 2 — Answering "what happens when I press Right?" takes four files

`SelectingGameState` → `MainWindowViewModel` → `GameList` → `ListCycle`. Each hands off part of
the job, and the part that updates the screen is a separate call the first file has to remember.

### Problem 3 — The internals are public, and other code reaches into them

To decide whether the featured-game screen should open, the browsing state does this:

```csharp
// SelectingGameState.OnUp — "am I on the first list?"
if (... && eclipseStateContext.MainWindowViewModel.listCycle.GetIndexValue(0) == 0)
```

`listCycle` is the internal bookkeeping for which list is showing. A state class is reaching
through it and doing index arithmetic to ask a question that deserves a name.

Likewise the collection of all sets is a public field that voice search edits directly:

```csharp
// VoiceRecognitionState
MainWindowViewModel.GameListSets.RemoveAll(set => set.ListCategoryType == ListCategoryType.VoiceSearch);
MainWindowViewModel.GameListSets.Add(new GameListSet { ... });
```

That "remove the old set of this kind and put a new one in" operation happens in three separate
places in three different files, written out longhand each time.

### Problem 4 — Switching category is eight copies of the same two lines

Choosing a category in the options pane is a switch with ten branches, eight of which are
identical apart from the category name:

```csharp
case ListCategoryType.Genre:
    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Genre);
    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
    break;
// ... seven more, identical but for the name
```

## What the change is, in plain terms

**Put "where the user is" into one object, and make every move a single call that finishes the
job — including telling the screen.**

After the change, pressing Right is one line in the browsing state, and it is impossible to move
the selection without the screen following, because moving and notifying are the same operation.

**The user sees no difference at all.** Same keys, same behaviour, same speed.

---

# Part 2 — The technical design

## The object

A new class, `GameListNavigator`. It owns the sets of lists and the current position, and it is
the only thing that changes either.

```csharp
public sealed class GameListNavigator
{
    // --- what exists to browse -------------------------------------------------
    void InstallSet(GameListSet set);          // replaces any set of the same category
    void RebuildCategorySets(IReadOnlyList<GameListSet> sets);
    GameListSet SetFor(ListCategoryType category);

    // --- where the user is -----------------------------------------------------
    GameListSet CurrentSet  { get; }
    GameList    CurrentList { get; }
    GameList    NextList    { get; }
    bool        IsOnFirstList { get; }

    // --- moving ----------------------------------------------------------------
    void MoveToNextGame();
    void MoveToPreviousGame();
    void MoveToNextList();
    void MoveToPreviousList();
    void PageForward();
    void PageBackward();
    void MoveToRandomGame();
    bool ShowCategory(ListCategoryType category);   // switch sets

    // --- surviving a rebuild ---------------------------------------------------
    void RememberPosition();
    void RestorePosition();

    // --- one place that says "the selection changed" ---------------------------
    event EventHandler SelectionChanged;
}
```

**Every public move raises `SelectionChanged` exactly once, at the end.** That is the whole point
of Problem 1: there is no way to move without notifying, because they are the same call.

## Where the pieces come from

| Moves out of `MainWindowViewModel` | Lines (approx) |
|---|---|
| `GameListSets`, `CurrentGameListSet`, `listCycle` | fields |
| `RefreshGameLists`, `CycleListForward`, `CycleListBackward` | 25 |
| `ResetGameLists` | 34 |
| `DoRandomGame`, `MoveToGame` | 45 |
| `SaveStateForGameListChange`, `CheckResetGameLists`, `ResetListsAfterChange`, `RestoredGameIndex`, and the five `preChange…` fields | 105 |
| the set-installing half of `CreateGameLists` and `DoMoreLikeCurrentGame` | 20 |

Roughly **230 lines out of 1,075**, and they are the 230 that currently have no single home.

## What stays in the view model, and why

`CurrentGameList` and `NextGameList` stay as bound properties — the XAML binds to them and that
must not change. They become a **projection**: values the view model copies from the navigator
whenever the navigator says the selection changed. One handler, one place:

```csharp
private void OnSelectionChanged(object sender, EventArgs e)
{
    CurrentGameList = navigator.CurrentList;
    NextGameList    = navigator.NextList;

    CurrentGameList?.WarmRowImages();
    NextGameList?.WarmRowImages();

    CallGameChangeFunction();
}
```

**Single source of truth** means: the navigator decides, the view model repeats. Nothing else
writes `CurrentGameList`. That is what stops the two drifting apart.

Also staying: `DoMoreLikeCurrentGame`'s *display* half (`IsDisplayingResults`,
`IsDisplayingFeature`, `IsDisplayingMoreInfo`) — those are view state, not navigation. It splits
into "navigator, install this set and show it" plus three flag assignments.

## What the states become

```csharp
// SelectingGameState
public bool OnRight(EclipseStateContext c, bool held)
{
    attractModeService.RestartAttractMode();
    c.MainWindowViewModel.Navigator.MoveToNextGame();
    return true;
}

// OnUp — the featured-game check, with a name instead of index arithmetic
if (settings.DisplayFeaturedGame && c.MainWindowViewModel.Navigator.IsOnFirstList)
{
    c.TransitionToState(c.GetState(typeof(FeatureOptionPlayState)));
    return true;
}
c.MainWindowViewModel.Navigator.MoveToPreviousList();
```

And Problem 4's switch collapses, reusing the browsable-category list that `GameListBuilder`
already defines:

```csharp
// SelectingOptionsState.OnEnter
switch (option.EnumOption)
{
    case ListCategoryType.VoiceSearch:  c.DoVoiceSearch(); break;
    case ListCategoryType.RandomGame:   navigator.MoveToRandomGame(); GoBrowsing(c); break;
    default:                            navigator.ShowCategory(option.EnumOption); GoBrowsing(c); break;
}
```

Ten branches become three.

## What does not change

- **Behaviour.** Wrapping in all four directions (`RULE-BROWSE-008`), page of seven or half a
  short list (`RULE-BROWSE-009`), random weighted by list size (`RULE-BROWSE-011`), the
  four-deep restoration fallback (`RULE-BROWSE-010`), featured game from the first list only,
  paging doing nothing outside the row.
- **Performance.** No work is added or removed per keypress. The measured 1.7 ms / 4 ms should
  be unchanged, and the Stage 1 instrumentation is still in place to confirm that.
- **The XAML.** Not one binding changes.
- **The key-strategy mechanism.** Page keys stay user-bindable and each strategy keeps declaring
  which states it applies to — that was N7, and N7 was withdrawn because that design is correct.

---

# Part 3 — Sub-stages

Deliberately four separate shippable steps rather than one large change, because navigation is
the most-used code in the product and a regression should be attributable to one of them.

### 4a — Close the notification gap, without moving anything

Add `MoveToNextGame` / `MoveToPreviousGame` / `MoveToNextList` / `MoveToPreviousList` /
`MoveToRandomGame` **on the view model**, each ending in `CallGameChangeFunction()`. Repoint the
states at them. No state calls `CallGameChangeFunction` by hand any more, and none calls
`CurrentGameList.CycleForward()` directly.

This fixes Problem 1 on its own and is the highest-value, lowest-risk step. If Stage 4 stopped
here it would still have been worth doing.

- **Files:** `MainWindowViewModel.cs`, `SelectingGameState.cs`, `SelectingOptionsState.cs`,
  `KeyStrategyPage.cs`, `KeyStrategyRandomGame.cs`
- **Verify:** every navigation key from browsing and from the options pane; especially Left out
  of the options pane, which currently relies on the state transition to refresh the screen

### 4b — Give the internals names and hide the fields

`IsOnFirstList` replaces `listCycle.GetIndexValue(0) == 0`. `InstallSet(GameListSet)` replaces
the three hand-written "remove the old one, add the new one" blocks. `listCycle` and
`GameListSets` become private.

- **Files:** `MainWindowViewModel.cs`, `SelectingGameState.cs`, `VoiceRecognitionState.cs`,
  `LoadingState.cs`
- **Verify:** featured game opens from the first list and not elsewhere; voice search still
  installs and shows results; startup still builds lists

### 4c — Collapse the category switch

Ten branches to three, reusing `GameListBuilder`'s browsable-category list so "which categories
can be browsed" is defined once rather than twice.

- **Files:** `SelectingOptionsState.cs`, `GameListBuilder.cs` (expose the list)
- **Verify:** every entry in the options pane

### 4d — Extract `GameListNavigator`

Move the members listed above out of the view model into the new class. The view model keeps its
bound properties and subscribes to `SelectionChanged`.

By this point 4a–4c have already turned the view model's navigation surface into the exact API
the navigator will have, so this step is a move rather than a redesign — the same shape as
`B-15`'s Stage 2, which was verified by a byte-identical dump.

- **Files:** `Service/GameListNavigator.cs` (new), `MainWindowViewModel.cs`, and the states,
  which should need only the receiver changed from `MainWindowViewModel` to `.Navigator`
- **Verify:** the full `VER-BROWSE-001` … `008` sweep

---

# Part 4 — Risks, honestly

| Risk | Why it is manageable |
|---|---|
| Navigation is the most-used code in the product | Four separately shippable steps; 4a and 4b are small and independently testable |
| Two sources of truth — navigator and view model both holding "current list" | The view model only ever assigns them in the `SelectionChanged` handler. Worth reviewing that specifically. |
| A move that forgets to raise `SelectionChanged` | Every public move ends with it; there is one private helper that does the moving, and the public methods wrap it |
| Position restoration is subtle and easy to break | It moves unchanged in 4d, having already been rewritten in Stage 1 and tested |
| My track record on this file | N7 was withdrawn after I proposed it, because I had not read the settings. This plan is written after reading `SelectingOptionsState`, `VoiceRecognitionState`, `LoadingState` and `KeyStrategyCache` in full, and the external surface turned out to be three call sites rather than the sprawl I assumed. |
| No automated tests | 4d finally produces a class worth testing — sets and integers, no WPF, no LaunchBox. `B-01` is worth revisiting then, not before. |

## Recommendation

**Do 4a and 4b.** They fix the two problems that can actually bite — the silent notification
dependency and the reaching into internals — for about sixty lines of change and almost no risk.

**4c is cosmetic but cheap.** Take it if the file is open.

**4d is the one to think about.** It is the largest step and its benefit is organisational rather
than behavioural: 230 lines find a home, and the browsing logic becomes testable. If the view
model's size is not currently causing you pain, 4a–4c capture most of the readability win at a
fraction of the risk, and 4d can wait until there is a reason.
