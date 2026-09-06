# Text search — faceted result rows (stage 5)

**Status:** stage 5a done (the measurement in §6); 5b–5d not yet implemented. Supersedes
[text-search.md](text-search.md) §6.2, which planned a different shape — see §8.

---

## 1. What this is

Today a search produces one row, however many filters are applied. When those
filters over-narrow — Capcom **and** Shooter **and** NES **and** 1988 leaves two
games — the only way out is to take a filter off, and the user has to guess
which one was the expensive one.

This stage answers that guess for them. Below the primary row, one row per
constraint, each showing what the search would have been **without** that one
constraint.

## 2. The shape: leave one out

With `Capcom · Shooter · Nintendo Entertainment System · 1988` applied:

```
Search: Capcom · Shooter · NES · 1988          2 games      <- primary
Capcom · Shooter · NES                        11 games      <- any year
Capcom · Shooter · 1988                        6 games      <- any platform
Capcom · NES · 1988                            4 games      <- any genre
Shooter · NES · 1988                           9 games      <- anyone
```

Every row is a **near miss** to what was asked for. The alternative considered
and rejected was one row per filter applied *alone* — all Capcom, all Shooters,
all NES, all 1988. Those rows are the same whatever else is applied, they run to
thousands of games on a real library, and they answer "browse by genre" rather
than "what did I over-narrow".

**The typed query is one of the things that can be left out.** If the user has
typed `mega` as well, it is a constraint like any other, and the row that drops
it — every filter, no text — is very often the one they wanted, because a typo
narrows exactly as hard as a wrong year does. Leaving the query out of the
leave-one-out set instead would mean every secondary row silently dropped two
constraints, which is not the thing this stage is named after.

### 2.1 One filter, not one facet

Leave-one-out drops a single **filter**, not a whole facet. With
`Sports · Football · NES` the rows are `Sports · NES` and `Football · NES`, not
`NES`. **Revised in 5b: no new `FilterSet` method was needed.** The plan expected a
sibling to `ApplyWithout` (which drops a whole facet, for the stage 4 widening
counts). But a `SearchRow` carries its own filter list, so plain
`FilterSet.Apply(row.Filters, index)` already resolves it. Adding the sibling
would have been a second way to do the same thing, reachable from nowhere.

## 3. When the rows appear

**Whenever two or more constraints are applied**, whether or not text has been
typed.

Not from one constraint: leaving out the only one produces the unfiltered
library, and the primary and secondary rows would differ by everything or by
nothing depending on the query. That is the duplicate-row case `HasDistinctNextList`
was written for, and the right answer is not to build the row at all.

## 4. Naming the rows

Every row is named by `TextSearchState.DescribeSearch` over its own constraint
set — the same code that names the primary today. Two consequences:

* The heading keeps working while the panel is faded (RULE-SEARCH-069/070). It has
  to, because with the panel gone the heading is the only thing that distinguishes
  one row from the next.
* Only the primary carries the `Search:` prefix. A secondary row reading
  `Search: Capcom · Shooter · NES` would claim to be the search when it is
  explicitly not it.

## 5. What this lands on

Three things are currently shaped for exactly one row. All three are part of
this stage, not surprises within it.

### 5.1 Down out of the results

`SearchSession.MoveDown` treats the results zone as a single row and returns the
cursor to the keyboard. With several rows, Down moves to the next row and only
leaves the results at the bottom. `TextSearchState` already delegates Left/Right
to the navigator when the cursor is in the results; Up/Down join them, and the
session keeps owning only the question of which *zone* has the cursor.

Up from the first results row still returns to the keyboard, so the way back is
unchanged and does not get longer as rows are added.

### 5.2 The remembered place

`SearchSession.RememberedResultIndex` is a game index with no row. Returning to a
search after Escape would land on the right column of the wrong row. It becomes a
row **and** an index, and both are cleared together when the constraint set
changes, for the reason the index alone is cleared today: a new set of rows is a
new place, in which the old one means nothing.

### 5.3 Building more than one row

`PublishResults` builds a single `GameList` and installs a one-list set. It grows a
loop. `VoiceRecognitionState` already installs multi-row sets and
`GameListBuilder.BuildMoreLikeThis` already builds a set of rows from one game's
metadata, so nothing here is new ground — this is the third caller of a pattern
that works.

## 6. Cost per keystroke — measured, and not a problem

**Resolved in stage 5a. The rows fit, the query stays a constraint, and none of
the fallbacks below are needed.** `SearchPerformanceTests.Building_the_leave_one_out_rows_is_measured`
is the measurement; re-run it if any of this changes.

### 6.1 What was feared

Five rows means up to five searches per keystroke, and a leave-one-out row is by
construction *broader* than the primary. Against the numbers already on record for
this library — 6.4 ms for a first character, and **65.9 ms** for one character
selecting the whole library — five of those would blow the 16 ms frame target and
reach the smoke budget.

### 6.2 What it actually costs

100,000 games, one library carrying both titles and metadata, worst keystroke of a
query typed a character at a time:

| Scenario | Primary only | With rows | Cost of rows |
|---|---|---|---|
| Four filters, over-narrowed (42 games) | 0.44 ms | **2.11 ms** | 1.67 ms |
| Two filters, one very broad (1,667 games; rows of 3,334 and 9,290) | 0.78 ms | **5.28 ms** | 4.50 ms |

5.28 ms against a 16 ms target, in the scenario chosen to be the bad one.

### 6.3 Why the fear was wrong

`SearchEngine.Search` ranks only what survives `candidates ∩ restrictTo`, and
ranking is the expensive half. The frightening numbers in §6.1 are all
**unfiltered** — they are the cost of ranking a candidate set the size of the
library. A fan-out cannot occur unfiltered, because it takes two constraints to
produce a single secondary row.

So the cost of the rows scales with **the sum of the row sizes**, not with five
times the unfiltered worst case. Widening a filter set by a factor of six, as the
broad scenario does, costs a few milliseconds rather than a few tens.

The second scenario exists because the first one flatters the feature: four
filters leave so few games that the engine ranks almost nothing and the rows are
nearly free. True, but not the case that could hurt. Keep both if this is ever
re-measured — a single narrow scenario would report a number that means little.

### 6.4 The fallbacks, kept for the record

Not needed, and not implemented. If a future change makes the rows expensive
again, in preference order:

1. Rebuild the secondary rows only when the **constraint set** changes, not on
   every keystroke — the query then narrows only the primary, at the cost of §2's
   claim that the query is just another constraint.
2. Build them on a short idle delay after typing stops.
3. Cap the row count below the constraint count.

(1) is a design compromise with a clear statement, (2) is a timing behaviour that
is hard to reason about, (3) makes the feature arbitrary.

## 7. Rules this adds

| ID | Rule | Why it matters |
|---|---|---|
| RULE-SEARCH-073 | With two or more constraints applied, the results carry one row per constraint beneath the primary, each showing the search **without** that one constraint. | Over-narrowing is the failure mode stacked filters produce, and the user cannot tell which constraint cost them the results. This answers it without their having to take anything off. |
| RULE-SEARCH-074 | The typed query is a constraint like any other for this purpose, and can be the one left out. | A typo narrows as hard as a wrong year. The row with every filter and no text is often the one wanted. |
| RULE-SEARCH-075 | No secondary rows below two constraints. | Leaving out the only constraint yields the unfiltered library — a row that is either everything or a duplicate of the primary. |
| RULE-SEARCH-076 | Each row is named by its own constraint set; only the primary is prefixed `Search:`. | The heading is the only thing telling the rows apart once the panel fades (RULE-SEARCH-069). A secondary row claiming to be the search would be false. |
| RULE-SEARCH-077 | Down moves through the rows and leaves the results only at the bottom; Up returns to the keyboard from the first row. | The way back must not get longer as rows are added. |
| RULE-SEARCH-078 | Secondary rows run most-recent-suspect first: the query's row leads, then the filters in reverse order of application. | Free text is the likeliest constraint to be wrong, and after that the last thing added is the likeliest culprit for whatever just disappeared. One rule, both halves. |
| RULE-SEARCH-079 | The rows are entered and left as a block: leaving them downward rewinds to the first, and wrapping up into them lands on the last. | The zone and the row are two separate cursors. Leaving the row where it was produced a dead end — Down out of the last row parked the rows at the bottom, so coming back down the keyboard re-entered at the last row and left again immediately, making the middle rows unreachable that way round. It read as having lost your place. |

## 8. What this replaces

[text-search.md](text-search.md) §6.2 planned rows derived from **dominant facet
values in the result set** — one row per platform, per genre, per series, ordered
by size. That was written before filters existed as a feature.

It is the weaker design for the case that actually hurts: it needs a result set
with variety left in it to group by, and the moment worth helping is the one where
the set has collapsed to two games and has none. Deriving the rows from the
applied constraints instead is deterministic, gives every row a name for free, and
degrades to nothing when there is nothing to say.

## 9. Not in this stage

* Fuzzy matching and search history — on hold by decision, unchanged here.
* Showing the near-miss rows when the primary finds nothing. `RULE-SEARCH-044` still hides
  the surface, deliberately left alone in 5c rather than changed as a side effect — but it is
  the moment the fan-out would help most. See `OQ-033`.
* Rows derived from result-set contents (§8) — dropped, not deferred.
* Any change to how the primary row is ranked, hydrated or drawn.

## 10. Stages

| | | |
|---|---|---|
| 5a | ~~A combined title + facet perf harness, and the measurement in §6.~~ **Done** — 5.28 ms worst keystroke against a 16 ms target. §6 resolved; nothing is gated on it. | |
| 5b | ~~The constraint-set enumeration and naming. Pure, tested below the UI boundary.~~ **Done** — `SearchRow` / `SearchRows`, 15 tests. `TextSearchState.DescribeSearch` now delegates to it. No behaviour change. | |
| 5c | ~~`PublishResults` builds and installs the rows. Row navigation and the remembered place.~~ **Done** — 8 more tests, 642 total. Unverified on a real library; see 5d. | |
| 5d | Manual verification on a real library; fold the results into the rules above. | |
