# Plan — B-15: extract list construction and the custom-list query engine

**Status: proposed. No code written.**

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

Each stage builds and deploys on its own. `B-15a` is Stages 0–3; `B-15b` is Stages 4–5.

### Stage 0 — Capture the baseline

The "done when" criterion is *a real user `CustomLists.json` produces byte-identical list
membership*. That needs a recording of today's output before anything moves.

Add a dump routine that walks `GameListSets` and writes, per set: the category type, then per
list its `SortOrder`, `ListTypeValue`, `MatchCount` and the ordered game `Id`s. Trigger it once
from the loading path behind a hidden setting, run BigBox against the real library and the real
`CustomLists.json`, and keep the file as the golden baseline.

The dump routine stays in the tree until Stage 5 is verified, then comes out.

- **Files:** `Eclipse/Service/GameListDump.cs` (new), `Eclipse/View/MainWindowViewModel.cs`
  (one call), `Eclipse/Models/EclipseSettings.cs` (one hidden setting)
- **Verify:** dump file exists and is plausible; browsing is unchanged

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

### Stage 2 — Move, don't change

`GetGamesByListCategoryType` is private on a view model that cannot be constructed outside
BigBox, so it cannot be tested where it sits. The characterization tests the backlog asks for
require the move to happen first — which is why Stage 0 exists. This stage is guarded by the
golden file, not by tests.

- New `Eclipse/Service/GameListBuilder.cs`. It takes what it needs rather than reaching for it:
  a catalog source, the custom list definitions, and the playlist-inclusion dictionary.
- New `IGameCatalogSource` (`Games`, `ByCategory`) implemented by `GameCatalog` — a narrow seam,
  not `B-11`.
- The query engine moves verbatim to `Eclipse/Service/CustomListQuery.cs`, still
  reflection-based. **Not one line of its logic changes in this stage.**
- `CreateGameLists` becomes: read definitions, read playlists, call the builder, assign the
  result.

- **Files:** `Eclipse/Service/GameListBuilder.cs` (new), `Eclipse/Service/CustomListQuery.cs`
  (new), `Eclipse/Service/IGameCatalogSource.cs` (new), `Eclipse/Service/GameCatalog.cs`
  (declare the interface), `Eclipse/View/MainWindowViewModel.cs` (delete ~225 lines, add ~15)
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

### Stage 4 — Replace reflection with an explicit accessor map (`B-15b`)

The point of the whole item. A `GameFieldEnum` to accessor table replaces property-name strings,
so renaming a projected property becomes a compile error instead of a silently broken
user-defined list.

- One table keyed by `GameFieldEnum`, giving a typed accessor and the field's type. It sits
  beside `ToFieldName` and `ToGameFieldType`, which already enumerate the same 27 values — a
  test asserts all three cover the enum exhaustively.
- Filters become typed predicates built from the accessor; sorts become `OrderBy`/`ThenBy` over
  the accessor.
- Fix the double enumeration from finding 3 — materialise once, test emptiness on the list.
- Delete `CustomGameListServiceExtensionMethods`.

Behaviour is held by the Stage 3 tests plus the golden file. Where the current reflection path
throws, the new path must throw the same way unless we decide otherwise first.

- **Files:** `Eclipse/Models/EclipseSettings.cs` (the accessor table beside the existing
  converters), `Eclipse/Service/CustomListQuery.cs`, `Eclipse.Tests/*`
- **Verify:** suite green; dump diff empty

### Stage 5 — Collapse `DoMoreLikeCurrentGame`

177 lines, seven blocks that differ only in which category set to search and which values to
search it for. Series, genre, developer, publisher and play mode are the same eleven lines five
times; platform and release year are the same again with a single value instead of a collection.

Becomes a table of `(ListCategoryType, Func<GameMatch, IEnumerable<string>>)` and one loop. The
table's order *is* the current result order, so the ordering `OQ-003` asks about is preserved
and becomes visible in one place instead of implied by statement order.

The last six lines — `ResetGameLists`, the three display flags, `CallGameChangeFunction` — stay
in the view model. Only the list selection moves to the builder.

- **Files:** `Eclipse/Service/GameListBuilder.cs`, `Eclipse/View/MainWindowViewModel.cs`
  (delete ~170 lines), `Eclipse.Tests/*`
- **Verify:** a test asserting the seven-category order and duplicate handling; manually, More
  Like This on a game with multiple genres and a series

### Stage 6 — Documentation

- `RULE-BROWSE-005` corrected to say custom lists filter the whole library (finding 1).
- `VER-BROWSE-006` marked covered; `VER-BROWSE-002` likewise.
- `B-15` split into `B-15a`/`B-15b` in `TRACEABILITY.md`; `B-01` marked delivered.
- `OQ-003` answered as far as this work answers it.
- Finding 5 recorded as a new backlog item.

---

## Net effect

| | Before | After |
|---|---|---|
| `MainWindowViewModel.cs` | 1385 lines | ~985 |
| `DoMoreLikeCurrentGame` | 177 lines | ~25 |
| Reflection over property-name strings | 2 call sites, 6 entry points | none |
| Filter delegates run per rebuild | 2x per custom list | 1x |
| Test projects | 0 | 1, with a reusable LaunchBox fixture |

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
