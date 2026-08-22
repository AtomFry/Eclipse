# Plan — B-15: extract list construction and the custom-list query engine

**Status: complete. All stages delivered and verified.**

## Context

`MainWindowViewModel` is 1385 lines. Roughly 40% of it builds game lists, and none of that
work is a view concern. Backlog item `B-15` covers it, and `TRACEABILITY.md` already records
the right instinct about it:

> **Should be split.** Category-list construction and the reflection-based custom-list query
> engine are independent, carry different risks, and have different characterization needs.

This plan splits it as **B-15a** (category-list construction) and **B-15b** (the query
engine), and puts a verifiable baseline in front of both.

### What is in scope, at current line numbers

| Member | Lines | Size |
|---|---|---|
| `GetGamesByListCategoryType` | `MainWindowViewModel.cs:357-459` | 103 |
| `CreateGameLists` | `:552-569` | 18 |
| `DoMoreLikeCurrentGame` | `:571-747` | 177 |
| `ResetGameLists` | `:850-871` | 22 |
| `SaveStateForGameListChange` / `CheckResetGameLists` / `ResetListsAfterChange` | `:966-1063` | 98 |
| `CustomGameListServiceExtensionMethods` | `:1264-1385` | 122 |

Position restoration (`ResetListsAfterChange` and friends) is `B-14`, not `B-15`. It is listed
here only because it is interleaved with the rest; **this plan does not touch it.**

### Who calls in

Only `CreateGameLists`, `ResetGameLists`, `DoMoreLikeCurrentGame` and `CheckResetGameLists`
are called from outside the view model, all from `Eclipse/State/*`. `GetGamesByListCategoryType`
and the whole query engine are private to this file, so they can move without touching any
caller.

---

## Findings from re-reading the code

Things a mechanical extraction would get wrong, or would quietly change.

**1. `RULE-BROWSE-005` is stale.** It states custom list filters apply to "the platform-category
projection of the index". The code now filters `gameCatalog.Games` — the whole library, every
game once — and says so in a comment at `:371`. The doc needs correcting either way; doing it
during this work keeps the rule honest about what the tests then lock in.

**2. `ShowGameCountInList` is a sort key.** Lists are ordered `OrderBy(SortOrder).ThenBy(ListDescription)`,
and `GameList` appends ` (N)` to `ListDescription` when that setting is on (`GameList.cs:117`).
Sorting therefore runs over a string that changes with a setting. Characterization must pin the
setting explicitly, and comparisons should key on `ListTypeValue`, not `ListDescription`.

**3. The filter is a compiled delegate, not a query.** `ApplyDynamicFilter` calls `.Compile()`
and hands a `Func<T,bool>` to `Where`, which binds to `Enumerable.Where` and is re-wrapped with
`AsQueryable()`. So this is LINQ-to-objects with deferred execution, and the pipeline is
enumerated twice per custom list — once for `Any()` at `:424`, once for `ToList()` at `:426`.
Every filter delegate runs twice on every rebuild, and a rebuild happens on every favourite,
every rating change and every game launch.

**4. Three ways a user's `CustomLists.json` can throw.** All of them at query-build or
evaluation time, none of them caught:

- `Expression.Convert(Expression.Constant(value), type)` at `:1327` — the value arrives from
  JSON as `long`/`double`/`string`/`bool`. `long` to `int` converts; `string` to `bool` throws.
- `IsNull`/`IsNotNull` compare against `Expression.Constant(null)`; against a non-nullable
  value-type property that throws.
- `Contains` calls `string.Contains` on the property instance — a null string field throws at
  evaluation, and the comparison is case-sensitive while every other list comparison in the
  product is not.

These are current behaviour. The tests should record what happens today; whether to *change* it
is a separate decision, noted at the end.

**5. "More like this" corrupts list indices in other sets.** `DoMoreLikeCurrentGame` adds the
*same* `GameList` instances that already live in the Genre, Platform, Series (etc.) sets into a
new `MoreLikeThis` set. The `GameListSet.GameLists` setter (`GameList.cs:22-28`) rewrites
`ListSetStartIndex` on every list handed to it. Those instances are shared, so building
More-Like-This overwrites the start indices that the Platform and Genre sets depend on — and
`ListSetStartIndex` is what `DoRandomGame` and position restoration navigate by. It self-heals
on the next `CreateGameLists`. **This is a real defect, not a refactoring artefact.** It is
called out here so it is a decision rather than a surprise; see "Out of scope".

**6. Two host singletons are reached into mid-loop.** `PlaylistGameService.Instance.Playlists`
at `:438` calls `PluginHelper.DataManager`, and `GameList`'s constructor reads
`EclipseSettingsDataProvider.Instance`. The first must become an input for any of this to be
testable. The second already has a public setter, so tests can inject settings without a new
seam.

**7. `GameListSets` is mutated in place** (`RemoveAll` then `Add`). Nothing depends on the
order of that list — every consumer queries it by `ListCategoryType` — so a builder that
returns sets and lets the view model assign them is safe.

---

## The fixture question (is this really blocked on B-12?)

`VERIFICATION.md` lists `VER-BROWSE-006` as blocked by `B-12` (an Eclipse-owned game model).
On re-reading, that is stronger than it needs to be:

- `IGame` is an **interface** — 88 properties, 34 methods.
- The contract assembly is **vendored** at `Eclipse/lib/LaunchBox14/Unbroken.LaunchBox.Plugins.dll`,
  so a test project can reference exactly what the plugin references, with no LaunchBox install.
- `GameMatch` has a parameterless constructor and a settable `Game`, so a fixture never has to
  go through `GameFiles` or the media pipeline.

So the blocker is fixture *ergonomics*, not feasibility: something has to implement 122 members.
Two routes:

| Route | Cost | Notes |
|---|---|---|
| Generated `FakeGame : IGame` | one ~250-line file, generated once from reflection | no new dependency; reusable by every later test; the fixture the rest of the backlog also needs |
| A mocking package (NSubstitute/Moq) | a test-only PackageReference | less code, but a dependency and a new idiom in a repo that has neither |

**Recommendation: the generated fake.** It is a one-time mechanical cost, it ships nothing, and
`B-14`, `B-16`, `B-30` and the search tests all need the same fixture.

---

## Stages

Each stage builds and deploys on its own. `B-15a` is Stages -1 to 2; `B-15b` is Stages 4 to 5b.
Stages 1 and 3 - the test project and its characterization suite - were deferred at the user's
direction in favour of manual verification, and the probes described below stand in for them.

### Stage -1 — Stop using a display string as a sort key and an identity — **done**

`GameList.ListDescription` was doing four incompatible jobs: the name on screen, the tie-break
sort key for every list set, the identity used to find your list again after a rebuild, and —
via the parameterless constructor in voice search — the recognised phrase itself.

It is built at construction as `ListTypeValue` plus ` (N)` when `ShowGameCountInList` is on,
which defaults to `true`. So the identity key changed whenever the list's membership changed:

1. You are in the Favorites list, which renders as `Favorites (12)`. That exact string is saved.
2. You un-favourite the game. Lists rebuild. The list is now `Favorites (11)`.
3. The lookup matches on the old string, finds nothing, falls to the `else` — and calls
   `DoRandomGame()`.

The four-deep fallback that `RULE-BROWSE-010` describes — same game, next game, previous game,
first in the list — was skipped entirely and the user landed on a random game elsewhere in the
library. It only bit when the count of the list *you were in* changed, which is the
favourites-and-history case the feature exists for; browsing by platform and favouriting
something was unaffected, which is presumably why it survived.

`ListTypeValue` already held the raw value, so the fix was to use it:

- `MainWindowViewModel.cs:456` — lists are ordered by `ListTypeValue`.
- `MainWindowViewModel.cs:979, :1011` — `preChangeListDescription` became
  `preChangeListTypeValue`, captured and matched on `ListTypeValue`.
- `VoiceRecognitionState.cs:175, :182, :191` — voice results carry the phrase in `ListTypeValue`
  as well as `ListDescription`, and the index lookup and match scoring read it from the value.
  Voice lists had no `ListTypeValue` at all before this, so they had no usable identity.
- `GameList.cs` — the constructor's first parameter is named for what it actually sets, and both
  properties now document which job is theirs.

**This is a behaviour change, which is why it sits before the Stage 0 baseline rather than
inside the region that baseline guards.** Two effects:

- Position restoration now works in the case it was written for. Verify with `VER-BROWSE-005`.
- The tie-break order of category lists can shift where names share a prefix, because the
  comparison is now `Action` rather than `Action (12)` and culture-aware comparison weights the
  parenthesis differently from a letter. Any such move is visible on the category picker.

**Noted, not changed:** `GameList.ListCategoryType` is never assigned anywhere in the product,
so every list carries the default (`VoiceSearch`) and the `list.ListCategoryType ==
preChangeListCategoryType` half of the restoration match always passes. The whole match rests on
the string. That belongs to `B-14`.

Also not changed: `GameList` still reads `EclipseSettingsDataProvider.Instance` — removing it
from the constructor buys nothing while the field initializer keeps reading the same singleton
for `RepeatGamesToFillScreen` in `RefreshGames`. That belongs with `B-31`.

- **Files:** `Eclipse/Models/GameList.cs`, `Eclipse/View/MainWindowViewModel.cs`,
  `Eclipse/State/VoiceRecognitionState.cs`
- **Verify:** builds clean; `VER-BROWSE-005` by hand; a voice search still returns results;
  the category picker still lists categories in a sane order

### Stage 0 — Capture the baseline — **built, awaiting a baseline run**

The "done when" criterion is *a real user `CustomLists.json` produces byte-identical list
membership*. That needs a recording of today's output before anything moves.

`GameListDump.WriteIfEnabled` walks `GameListSets` and writes, per set: the category type, then
per list its `SortOrder`, `ListSetStartIndex`, `MatchCount` and `ListTypeValue`, then every game
in list order with its position, id and title. It is called once from the loading path
immediately after `CreateGameLists`, and does nothing unless `DumpGameLists` is set.

Three decisions worth recording:

- **Sets are written in category-name order, not build order.** Nothing reads `GameListSets`
  positionally — every consumer looks a set up by its category — so a build that produces the
  same sets in a different order is not a behaviour change and should not read as one.
- **Lists within a set are written in the set's own order**, because that order is what the
  user scrolls through and is exactly what the diff is protecting.
- **The header records `ShowGameCountInList`, `IncludeHiddenGames` and `IncludeBrokenGames`.**
  The first changes what a list is called; the other two change what the library contains. A
  dump is only comparable against another taken with the same three values.

`ListSetStartIndex` is in the output deliberately: it is the field finding 5 corrupts, so if
that ever happens during this work the diff will show it.

The dump routine and its setting stay in the tree until Stage 5 is verified, then come out.

- **Files:** `Eclipse/Service/GameListDump.cs` (new), `Eclipse/State/LoadingState.cs` (one
  call), `Eclipse/Models/EclipseSettings.cs` (one hidden setting)
- **Deviation from the plan as written:** the call went in `LoadingState.cs` rather than
  `MainWindowViewModel.cs`. The loading path is where `CreateGameLists` is already invoked, and
  putting it inside `CreateGameLists` itself would have written a file on every favourite
  toggle, every rating change and every game launch.
- **Verify:** with `DumpGameLists` on, a dump file appears in `<LaunchBox>\Plugins\Eclipse` and
  its contents are plausible; with it off (the default) nothing is written and browsing is
  unchanged

### Stage 1 — Test project (`B-01`)

- New `Eclipse.Tests` project: `net10.0-windows`, `UseWPF=true`, referencing `Eclipse` and the
  same vendored `Unbroken.LaunchBox.Plugins.dll`.
- Generated `FakeGame : IGame` — auto-properties for all 88 properties, `NotImplementedException`
  for the 34 methods.
- Small builders: `AGame.WithGenres(...)`, `ACustomList.Filtering(...)`, plus a settings
  injection helper.
- Two or three tests against code that already exists and needs no changes — `ListCycle`,
  `GameTitleGrammarBuilder` — purely to prove the harness runs.
- Solution updated.

**No production code changes in this stage.** The plugin output must be identical.

- **Risk to resolve here:** the `RuntimeIdentifier win-x64` on `Eclipse.csproj` and the WPF
  reference may need matching settings on the test project. If it fights, the fallback is to
  keep the test project on the framework reference without an RID.
- **Files:** `Eclipse.Tests/*` (new), `Eclipse.sln`
- **Verify:** `dotnet test` green; `dotnet build Eclipse` output unchanged

### Stage 2 — Move, don't change — **built, awaiting the dump diff**

`GetGamesByListCategoryType` is private on a view model that cannot be constructed outside
BigBox, so it cannot be tested where it sits. The characterization tests the backlog asks for
require the move to happen first — which is why Stage 0 exists. This stage is guarded by the
golden file, not by tests.

- `Eclipse/Service/GameListBuilder.cs` takes what it needs rather than reaching for it: a
  catalog source, the custom list definitions, and the playlist-inclusion dictionary. `BuildAll`
  returns the eight sets; `Build` returns one.
- `Eclipse/Service/IGameCatalogSource.cs` — `Games` and `ByCategory`, implemented by
  `GameCatalog`. A narrow seam, not `B-11`.
- The query engine moved to `Eclipse/Service/CustomListQuery.cs`, still reflection-based. Only
  the class name changed; **no logic changed.**
- `CreateGameLists` reads the definitions, builds, and replaces each set by category. It
  replaces rather than clears, exactly as before, so a rebuild triggered by favouriting a game
  does not discard the voice search or more-like-this sets.
- `System.Linq.Expressions` and `System.Reflection` are no longer used by the view model and
  came out with the code that needed them.

**One initialization-order detail had to be preserved deliberately.** Setting up `GameCatalog`
is what populates `PlaylistGameService`, and the playlist dictionary used to be read part-way
through building the platform set — always after the catalog had been touched. Read from a cold
service, the dictionary builds itself and is then rebuilt during the catalog's own setup,
leaving the caller holding the previous instance. The contents are identical either way, but
`CreateGameLists` touches the catalog first so there is only ever one owner.

**Deliberately not done:** passing `ShowGameCountInList` into the builder so `GameList` would
stop reading the settings singleton. It buys nothing while `GameList`'s field initializer still
reads the same singleton for `RepeatGamesToFillScreen` — the same reasoning as Stage -1.

- **Files:** `Eclipse/Service/GameListBuilder.cs` (new), `Eclipse/Service/CustomListQuery.cs`
  (new), `Eclipse/Service/IGameCatalogSource.cs` (new), `Eclipse/Service/GameCatalog.cs`
  (declare the interface), `Eclipse/View/MainWindowViewModel.cs` (255 lines deleted, 21 added)
- **Verify:** dump again in BigBox; **diff must be empty** against the Stage 0 baseline

### Stage 3 — `VER-BROWSE-006`

Now write the tests the backlog asks for, against `GameListBuilder` and `CustomListQuery`:

- every `FilterFieldOperator` against a field of each `GameFieldType` — including the three
  throwing cases from finding 4, recorded as they behave today
- both sort directions; two sort expressions, asserting the second only breaks ties of the first
- `MaxGamesInList` applied *after* sorting (`RULE-BROWSE-007`)
- sort expressions replacing the default title sort rather than refining it (`RULE-BROWSE-006`)
- a custom list appearing in several `ListCategoryTypes`
- empty custom lists excluded from the set
- playlist inclusion in the Platform set, both flags
- combined ordering: custom lists first in definition order, then category lists alphabetically
- a game with three genres appearing in three lists (`VER-BROWSE-002`, free at this point)

- **Files:** `Eclipse.Tests/*` only
- **Verify:** suite green; no production change

### Stage 3 — `VER-BROWSE-006` — **replaced by a probe, at the user's direction**

The test project (Stage 1) and this suite were skipped in favour of manual verification. That
leaves Stage 4 rewriting the engine for 27 fields and 9 operators with only the startup dump
behind it — and a `CustomLists.json` uses a handful of those combinations at most. The rest are
what *other people's* lists use, and those files are not in this repository.

`CustomListQueryProbe` gets that coverage from a dump instead of from tests. It builds one
synthetic custom list per field/operator pair, runs each through the real `GameListBuilder`
against the real library, and records the outcome: the first 25 members in order, `EMPTY` if the
filter excluded everything, or `THREW` with the exception type and message.

Two details make the probes represent real custom lists rather than a friendlier version:

- **Filter values are sampled from the library and re-boxed the way JSON would deliver them** —
  `long` for whole numbers, `double` for fractional ones. A real value arrives from
  `CustomLists.json` boxed as Newtonsoft left it, and the conversion to the property's type is
  exactly where finding 4 says it can throw. The sampled values are written to the file so a
  library change explains itself rather than leaving a hundred probes to be puzzled over.
- **Each probe runs in its own builder with its own try/catch.** The builder has no
  per-definition error handling, which is part of what is being recorded, and a throwing
  combination must not stop the ones after it.

`THREW` is a valid result to lock in. A combination that throws today must still throw
tomorrow; making it stop throwing is a decision, not a refactor.

Sort coverage is included: every field in both directions, plus three two-key definitions to
show the second key only breaking ties of the first. Every probe carries a size cap, so
cap-after-sort is exercised throughout.

- **Files:** `Eclipse/Service/CustomListQueryProbe.cs` (new), `Eclipse/State/LoadingState.cs`
  (one call). Runs with `DumpGameLists`; comes out with the rest of the dump machinery.
- **Verify:** a probe baseline must be captured *before* Stage 4 is written

### Stage 4 — Replace reflection with an explicit accessor map (`B-15b`) — **built, awaiting the diffs**

The point of the whole item. `GameFields` maps each `GameFieldEnum` to a compile-checked
expression — `gameMatch => gameMatch.Game.StarRatingFloat` — instead of the string
`"Game.StarRatingFloat"` walked by reflection. Renaming a projected property now stops the
build. That all 27 entries compiled is itself the first result: every name `ToFieldName`
produces does resolve against the real `IGame`.

**The expressions are kept as expressions, not compiled delegates.** Two reasons, and both are
about not changing behaviour while removing the reflection:

- The filter is still assembled from the field's expression *body* over the field's own
  parameter, feeding the same `Expression.Equal`/`Convert`/`Call` switch as before. Comparison,
  conversion and — critically — *which combinations throw* are unchanged. Hand-writing typed
  predicates instead would have quietly changed all three.
- The sort hands the whole expression to `Queryable`, so ordering still uses
  `Comparer<T>.Default` for the property's real type. An accessor returning `object` would have
  boxed every key and changed comparison semantics for nullable and numeric fields.

Both ordering switches keep their missing `default`, so a `SortDirection` outside the enum —
which a hand-edited `CustomLists.json` can produce — still leaves the order untouched.

`ApplyOrder`, the four `OrderBy`/`ThenBy` string extensions and `ApplyDynamicFilter`'s property
walk are gone. `Queryable` is no longer reached through `GetMethods().Single(...)`.

**Finding 3 fixed.** The query is materialised once instead of being run for `Any()` and again
for `ToList()`. Every filter delegate used to run over the whole library twice on every rebuild,
and a rebuild happens on every favourite, rating change and game launch.

**Drift guard.** `GameFieldEnum`, `ToFieldName` and the accessor table are three hand-written
lists of the same fields. `GameFields.UnmappedFields()` is checked at startup and logs anything
missing, rather than letting it surface whenever a user's custom list happens to use that field.
This stays after the dump machinery comes out at Stage 6.

**Deliberately unchanged: the combinations that throw.** `IsNull` against a non-nullable field,
`Contains` against a number, a JSON value that will not convert to the property's type — all of
these fail today and a user's `CustomLists.json` can contain any of them. Making them fail
softly is a product decision, not part of removing the reflection.

- **Files:** `Eclipse/Service/GameFields.cs` (new), `Eclipse/Service/CustomListQuery.cs`
  (rewritten), `Eclipse/Service/GameListBuilder.cs`, `Eclipse/State/LoadingState.cs` (drift
  guard)
- **Verify:** probe diff empty against the Stage 3 baseline; startup dump diff empty against a
  baseline taken after the last `CustomLists.json` change

### Stage 5a — Move the selection out, and probe it — **built, awaiting a baseline run**

Splitting Stage 5 in two, for the same reason Stage 2 and Stage 4 were split: the collapse needs
a before-and-after, and more-like-this has nothing at startup for the golden dump to catch. It
is built on demand from whichever game happens to be selected.

`GameListBuilder.BuildMoreLikeThis(currentGame, gameListSets)` holds the selection; the view
model keeps the six lines that follow it — replacing the set, resetting the lists, the three
display flags and `CallGameChangeFunction`. **The moved body was cut, not retyped**, and
verified textually identical to the original modulo indentation and the parameter name. The
view model's diff for this stage is 150 lines removed and one line added.

`MoreLikeThisProbe` then drives that selection over an even stride through the catalog — 100
games, so the sample spans rich metadata and sparse — and writes each game's own series, genres,
platform, developers, publishers, play modes and release year alongside the lists that came
back, in order, with duplicates intact. The metadata is there so that a changed line can be told
apart from a re-scraped game.

**The probe does not build a `GameListSet` from the results.** Those are the same `GameList`
instances that live in the genre, platform and series sets, and `GameListSet`'s setter rewrites
`ListSetStartIndex` on everything handed to it — so constructing one here would corrupt the
indices the rest of the product navigates by. That is finding 5, and a diagnostic must not be
the thing that triggers it.

- **Files:** `Eclipse/Service/GameListBuilder.cs` (method moved in),
  `Eclipse/View/MainWindowViewModel.cs` (150 lines out, 1 in),
  `Eclipse/Service/MoreLikeThisProbe.cs` (new), `Eclipse/State/LoadingState.cs` (one call)
- **Verify:** a probe baseline must be captured *before* Stage 5b is written

### Stage 5b — Collapse `DoMoreLikeCurrentGame` — **built, awaiting the probe diff**

150 lines, seven blocks that differ only in which category set to search and which values to
search it for. Series, genre, developer, publisher and play mode were the same eleven lines five
times; platform and release year the same again with a single value instead of a collection.

Now `MoreLikeThisCategories` — a seven-row table of
`(ListCategoryType, Func<GameMatch, IEnumerable<string>>)` — and one loop, about 30 lines. The
table's order *is* the result order, so what `OQ-003` asks about is preserved and is visible in
one place instead of implied by the order of seven statements.

Two behaviours preserved that a tidier rewrite would have changed:

- **The multi-value collections are still not null-guarded.** A metadata property returning null
  has always thrown here rather than silently matching nothing, and adding `?? Enumerable.Empty`
  would have turned a crash into a wrong answer. `Only()` supplies the none-or-one sequence for
  platform and release year, which is exactly what their null checks did.
- **`gameListSets` is not null-conditional.** The old query syntax threw `ArgumentNullException`
  on a null collection; `FirstOrDefault` without `?.` throws the same.

Duplicates are still not removed, and the caller still shows them as they come.

- **Files:** `Eclipse/Service/GameListBuilder.cs`, `Eclipse/View/MainWindowViewModel.cs`
- **Verified.** The `MoreLikeThisProbe` diff against the Stage 5a baseline was timestamp-only,
  as were the startup dump and the custom-list probe. One of the two runs also toggled
  `ShowGameCountInList` between dumps, and every list held its position — which the pre-Stage -1
  code, sorting on the display string, would not have done. An unplanned confirmation of the
  first stage by the last one's tooling.

### Stage 6 — Documentation — **done**

- `RULE-BROWSE-005` corrected: custom lists filter the whole library, not a category projection
  of it (finding 1). The old wording described pre-`GameCatalog` behaviour, where filtering the
  platform projection amounted to the same thing because each game has one platform.
- `RULE-BROWSE-012` notes that the more-like-this order is now a table.
- `VER-BROWSE-006` marked **covered by probe, not by tests**, with the distinction stated: a
  probe proves behaviour did not change, not that the behaviour is correct. Its rank-4 entry in
  the test-priority list notes that its stated rationale — reflection over property-name strings
  breaking lists silently — no longer applies.
- `VER-BROWSE-002` notes it is visible in the golden dump.
- `B-15` split into `B-15a`/`B-15b`, both marked delivered, with the substitution of probes for
  unit tests recorded rather than glossed. **`B-01` is not marked delivered** — the test project
  was deferred, not built.
- The testability table gains a "testable now" row: list construction no longer waits on
  `B-11`/`B-12`, only on `B-01`.
- `OQ-003` updated — still an open product decision, but now a one-line change to act on.
- `OQ-012` **resolved**, and the summary's "two highest-value questions" reduced to one. The
  stop-loop was compensating for a stale animation `Completed` callback that restarted the video
  timer; cancellation makes that structurally impossible. `B-24` delivered, `S-12` closed.
- Finding 5 recorded as **`OQ-022`** — more-like-this corrupting the `ListSetStartIndex` of other
  sets — flagged as a defect needing a decision, and pointed at `B-14`.

**Done.** `GameListDump`, `CustomListQueryProbe`, `MoreLikeThisProbe`, the `DumpGameLists`
setting and the three calls in `LoadingState` are removed. The
`GameFields.UnmappedFields()` drift guard stays.

---

## Net effect

| | Before | After |
|---|---|---|
| `MainWindowViewModel.cs` | 1385 lines | 1019 |
| `DoMoreLikeCurrentGame` | 177 lines | 21, of which 15 are view concerns |
| Reflection over property-name strings | 2 call sites, 6 entry points | none |
| Filter delegates run per rebuild | 2x per custom list | 1x |
| Renaming a projected property | silently breaks a user's custom lists | compile error |
| Test projects | 0 | 0 - deferred; probes stood in |

Two defects were found and fixed along the way, and one was found and left alone:

- **Fixed (Stage -1):** position restoration matched lists on a display string carrying the game
  count, so un-favouriting from inside Favorites could never find the list again and dropped the
  user on a random game.
- **Fixed (Stage 4):** every custom list filter ran over the whole library twice per rebuild.
- **Left alone (`OQ-022`):** more-like-this corrupts the `ListSetStartIndex` of the sets it
  borrows lists from. Fixing it changes behaviour and belongs with `B-14`.

---

## Out of scope

Named explicitly so they stay decisions rather than drift.

- **The shared-`GameList` index corruption (finding 5).** A behavioural fix, and it belongs to
  whatever covers `DoRandomGame` and position restoration. Worth its own item.
- **Making `Contains` case-insensitive, or making a bad `CustomLists.json` value fail softly**
  (finding 4). Both are improvements; both change what a user's existing lists do.
- **`B-14` position restoration** — interleaved with this code, separately verified.
- **`B-16` incremental rebuild.** Much easier once the builder exists, which is part of the
  argument for doing this first, but not part of it.
- **`B-11`/`B-12`.** `IGameCatalogSource` is deliberately the narrowest seam that makes this
  testable, not a step toward a general adapter.
