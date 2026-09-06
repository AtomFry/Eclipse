# Plan — Metadata filters (text search sub-feature)

> **Status: designed, not started.** Sub-feature of
> [text-search.md](text-search.md), which owns the search screen, the input model, the
> matching engine and the results. This document owns only what happens when the typed text
> matches a *metadata value* rather than a title: how those are offered, how selecting them
> stacks, and what stacking means.
>
> It is separated because it is the largest and least-settled part of the feature, and because
> it can be designed, argued about and revised without touching the parent plan. It lands as
> **stage 4** there — after text search is already shipping and useful.

---

## 1. The interaction being designed

From the request, restated as a script:

1. Type `spo` → the genre **Sports** is offered → select it → only sports games remain, and
   `Sports` is shown as an applied filter.
2. Type `fo` → the genre **Football** is offered → select it → both filters apply, and only
   games carrying *both* genres remain.
3. Type `ea` → the publisher **EA Sports** is offered → select it → EA Sports football sports
   games.
4. Type `seg` → the platform **Sega Genesis** is offered → select it → narrower again.
5. Remove the **Football** filter → Sega Genesis EA Sports games.
6. Remove the **Sega Genesis** filter → EA Sports games on every platform.

This is **faceted search with an autocomplete term picker** — the pattern behind every retail
site's left-hand filter rail, driven from a keyboard instead of checkboxes because Eclipse has
a d-pad and no mouse. Naming it matters: it means the hard parts are already solved problems
with known answers, and the parts that need deciding are the two in §3 and §4.

---

## 2. Vocabulary

| Term | Meaning |
|---|---|
| **Facet** | A dimension the library can be sliced on: platform, genre, series, developer, publisher, play mode, release year, playlist. |
| **Value** | One value within a facet: `Sports`, `Sega Genesis`, `EA Sports`. |
| **Term** | A `(facet, value)` pair. What a suggestion offers and a chip holds. |
| **Chip** | A term the user has selected, shown on the filter row and removable. |
| **Filter set** | The games surviving every chip. Computed as `int[]` of catalog indices. |
| **Free text** | The uncommitted query buffer. Filters titles live; never becomes a chip on its own. |

**The distinction that makes the whole thing work:** chips are *structured* filters over
metadata; the query buffer is an *unstructured* filter over titles. They apply
simultaneously and independently. `sonic` in the buffer with a `Sega Genesis` chip means
"Genesis games whose title matches sonic". That is the model behind every faceted search box
users have already met, and it is why the flow in §1 needs no mode switch.

---

## 3. Decision one — the filter algebra

The request describes every chip narrowing the results. That is right for most facets and
**wrong for two**, and getting it wrong produces a result set that is silently always empty.

### 3.1 The trap

A game has *many* genres, developers, publishers, play modes, series and playlists. It has
exactly **one** platform and **one** release year.

So `genre = Sports AND genre = Football` is a meaningful narrowing — a game can carry both.
But `platform = Sega Genesis AND platform = SNES` can never match anything, because no game
has two platforms. Under a naive "every chip ANDs" rule, a user who selects a second platform
gets zero results and no explanation.

`GameCatalog.BuildCategoryIndex` already draws this line in code: `Platform` and `ReleaseYear`
are indexed with a plain `ToLookup` over a single value, while genre, publisher, developer,
series, play mode and playlist go through `Expand`, which fans one game into several buckets.
The distinction exists; the filter algebra just has to respect it.

### 3.2 The rule

| ID | Rule |
|---|---|
| RULE-SEARCH-050 | Chips of **different** facets combine with AND. |
| RULE-SEARCH-051 | Chips of the **same facet** combine with **OR** — the game must carry one of them. Every facet, uniformly. |
| RULE-SEARCH-052 | *(Superseded — merged into 051. This pair once split single-valued facets from multi-valued ones; see docs/features/search.md and the note in FacetIndex for why the split was dropped.)* |
| RULE-SEARCH-053 | Free text applies as an additional AND over whatever the chips leave. |

**This algebra is settled for v1.** It is the one place in either plan where the interaction
model is more complicated than the retail convention, the complication is inherited from the
data rather than invented, and RULE-SEARCH-067 below is what makes it legible. The per-chip
override that would let a user say "sports *or* football" is real future work and is tracked as
`OQ-028` — it is not built now, and it is not a reason to revisit the default.

RULE-SEARCH-052 is not a compromise, it is the more useful behaviour on its own merits:
"Genesis or SNES" is a request people actually make, and it is the only thing a second
platform chip could sensibly mean.

This is the conventional algebra, unadjusted. Retail facet rails OR within a facet and AND
across facets, and that is now exactly what Eclipse does.

> **This paragraph used to describe an adjustment**: multi-valued facets ANDing within
> themselves, because "sports and football" is what the request asked for and a game genuinely
> carrying two genres makes it meaningful. That shipped, and was removed later. The reasoning was
> sound for genre and wrong everywhere else — the schema calls developer, publisher, series and
> play mode multi-valued, but real libraries do not use them that way, so those filters ANDed to
> nothing and their second value was never even offered. Genre intersection was the price of the
> single rule. See `docs/features/search.md` RULE-SEARCH-051/052 and the measurement in
> `FacetIndex`.

### 3.3 The consequence, and why the display of it is a rule rather than polish

Under RULE-SEARCH-051 and RULE-SEARCH-052, two chips of the same facet behave differently
*depending on the facet*. That is a semantic rule derived from the shape of the LaunchBox data
model, and there is no reason a user should be expected to know it. Left implicit, it is a
hidden rule; made visible, it is self-explanatory.

| ID | Rule |
|---|---|
| RULE-SEARCH-067 | The chip row renders `or` between chips of the same facet, and nothing between chips of different facets. |

```
[ Genre: Sports ]  and  [ Genre: Football ]      [ Platform: Genesis ]  or  [ Platform: SNES ]
```

Two or three characters of text that remove an entire class of confusion, which is why this is
a numbered rule staged with the chip row (4d) rather than an appearance item that gets dropped
when 4e runs short. The algebra is only defensible if it is legible.

### 3.4 The alternative that was rejected

Make every facet OR-within-itself, matching the retail convention exactly. Rejected because
the request's central example — sports **and** football — is the thing that makes stacking
worth building, and OR-within-genre would turn it into "sports or football", which is *wider*
than sports alone. Recorded as `OQ-028` in case real use disagrees; the fix would be a per-chip
toggle, not a different default.

---

## 4. Decision two — what suggestions are computed against

The other decision that determines whether this feels intelligent or hostile.

| ID | Rule |
|---|---|
| RULE-SEARCH-054 | Suggestions are computed against the **current filter set**, not the whole library. |
| RULE-SEARCH-055 | A term that would produce zero results is never offered. |
| RULE-SEARCH-056 | Each suggestion shows the count it *would* produce, not the count it has in the library. |

Together these make it **impossible to reach an empty result set by selecting suggestions**.
The user cannot pick `Football` after `Sports` unless a sports football game exists; if none
does, `Football` is simply not offered. The screen never has to explain a dead end because it
never creates one, and the counts turn the suggestion list into a preview of the next step
rather than a list of guesses.

This is the single highest-value rule in this document, and it is only affordable because the
posting lists are already `int[]` (parent plan, prerequisite P4) — the count is the size of an
intersection, computed for a handful of candidates per keystroke.

**Treat RULE-SEARCH-054 as an invariant, not an optimisation target.** It is the difference
between a system that feels like it understands the library and one that feels like a database
front end, and the obvious way to make suggestions cheaper — rank them against the whole
library and filter afterwards, or cache them across chip changes — breaks it. If suggestion
evaluation ever needs to be faster, make the intersection faster; do not widen what it is
evaluated against. `VER-SEARCH-023` and `VER-SEARCH-027` exist to keep that trade honest.

The cost is real and should be stated: with many chips applied, the suggestion list gets short.
That is correct behaviour, and it is why the chip row must always be visible and always
navigable — the way out of an over-narrowed search is to remove a chip, and that has to be
obvious from the screen rather than remembered.

---

## 5. The suggestion list

### 5.1 What is matched

The typed text is analysed by the same chain as titles (parent plan §5.2) and matched against
the analysed tokens of every facet value:

* **Exact value match** — `sports` matches the genre `Sports`.
* **Prefix of any token in the value** — `ea` matches `EA Sports`; `spo` matches `Sports`;
  `gen` matches `Sega Genesis`.
* **Fuzzy**, under the same length-gated budget as titles, once stage 3 of the parent plan
  exists.

Prefix-on-any-token rather than prefix-on-the-whole-value matters: `sports` should find
`EA Sports`, and `genesis` should find `Sega Genesis`. Users type the distinctive word, not the
first one.

### 5.2 Ranking

| ID | Rule |
|---|---|
| RULE-SEARCH-057 | Suggestions rank by match quality first (exact > whole-value prefix > token prefix > fuzzy), then by resulting result count descending. |
| RULE-SEARCH-058 | At most `SearchMaxSuggestions` (default 8) are shown, and no more than three from any one facet. |
| RULE-SEARCH-059 | Suggestions are shown once they are worth showing. **On trial at one typed character** — it shipped at two, and two was a guess that the threshold itself made unverifiable. A display threshold, not an engine limit. |

RULE-SEARCH-058's second half is the important one. A library with two hundred developers
starting `ea` would fill the entire list with developers and bury the one genre the user
wanted. Capping per facet keeps the list a cross-section of the library rather than a slice of
its largest facet. Facets are considered in a fixed order — series, genre, platform, publisher,
developer, play mode, release year, playlist — which mirrors
`GameListBuilder.MoreLikeThisCategories`, so the two features agree on what "most relevant kind
of metadata" means.

RULE-SEARCH-059 needs care in how it is implemented. Two characters looked like a reasonable
starting point — one character usually matches too much to be informative, and the list would
thrash on the second keystroke anyway. But on a d-pad **every character is expensive**, so if a
single character can produce a useful suggestion, refusing to show it is a real cost.

So `SuggestionRanker` must be **capable of a one-character query** and must not carry the
threshold itself. The threshold lives in the session as a constant, the ranker is asked and
answers, and where the line goes is settled by trying it on a real library rather than by this
document. On a library where `s` yields *Sports*, *Sega Genesis* and *Shooter* as the top three,
showing them is clearly right; where it yields three arbitrary developers, it is clearly wrong.

**On trial at one (stage 4e).** Two shipped first, and then made itself unverifiable: the
question is whether a single character is useful, and the threshold was what hid the evidence.
The golden corpus shows one character answering usefully, but a corpus of 110 metadata terms
cannot stand in for a library of thousands. It is set to one to be looked at.

### 5.3 What a suggestion looks like

```
Sonic                    series      31
Sonic Team               developer   18
Sony Computer Ent.       publisher    9
```

Value, facet, and the count it would produce. All three are **required**, not display polish.

The facet label is not decoration — `Sonic` the series and `Sonic Team` the developer are
different filters, and without the label the user cannot tell which one they are about to
apply.

The count is load-bearing for a different reason: **on a controller there is no hover, no
tooltip and no way to inspect anything.** The count is the entire preview mechanism. It is what
turns a list of guesses into a list of consequences, and it is what makes stacked filtering
safe to explore — a user looking at

```
Football          genre        34
EA Sports         publisher    18
Sega Genesis      platform     12
```

can see, before committing an input, which branch is worth taking and which one narrows to
almost nothing. Without it, every selection is a gamble that costs several button presses to
undo. It ships in 4c with the suggestion list, not in 4e with the polish.

### 5.4 Selecting one

| ID | Rule |
|---|---|
| RULE-SEARCH-060 | Selecting a suggestion adds it as a chip **and clears the query buffer**. |
| RULE-SEARCH-061 | Selecting a term already applied removes it (the suggestion list shows applied terms as selected). |
| RULE-SEARCH-062 | After selecting, focus returns to the keyboard. |

RULE-SEARCH-060 is what makes the §1 script flow: `spo` → Sports → `fo` → Football.

State it as an invariant rather than as a behaviour, because it is the thing every state
transition in the search screen has to preserve:

> **The query buffer is scratch space used to discover a metadata term. Selecting a suggestion
> consumes it.**

Everything follows from that sentence. The buffer empties on selection because its job is
finished. The free text that survives is, by definition, only text the user chose *not* to turn
into a filter — which is exactly the text they meant as a title search, which is why §2's split
between structured chips and unstructured text needs no mode switch to explain it. And a user
who types `sonic` and selects the *Sonic* series has made a deliberate promotion of their typed
text into structured metadata; the buffer clearing is the visible confirmation that the
promotion happened.

RULE-SEARCH-061 makes the suggestion list idempotent: an already-applied term shows as selected
and re-selecting it removes it, so a user who does not remember whether they applied something
cannot end up applying it twice. Duplicate chips are not deduplicated later — they are
unreachable, which is the stronger guarantee. `VER-SEARCH-028` is the test.

---

## 6. The chip row

| ID | Rule |
|---|---|
| RULE-SEARCH-063 | Chips display grouped by facet. Within a facet they are in the order they were added, and the facet groups appear in the order their first chip arrived. |
| RULE-SEARCH-064 | The chip row is focusable whenever it is non-empty, and Enter on a chip removes it. |
| RULE-SEARCH-065 | Removing a chip re-runs the query; nothing else is retyped or reset. |
| RULE-SEARCH-066 | Chips survive leaving the search screen for the session (parent RULE-SEARCH-035). |
| RULE-SEARCH-068 | All chips can be removed at once, via the context-sensitive `clear` key (parent RULE-SEARCH-038): it clears the query while there is text, and clears every chip when the query is empty. |

RULE-SEARCH-068 exists because per-chip removal alone does not scale to the situation it is
most needed in. A user four filters deep who has over-narrowed wants to start again, and making
that cost four deliberate Enter presses on a row they must first navigate to is the wrong shape
— especially since the alternative they will actually reach for is Escape, which under
RULE-SEARCH-034 keeps the session and therefore keeps every chip. Without a clear-all, the
over-narrowed state is genuinely awkward to escape.

It is deliberately *not* a separate key. The input surface has no room (parent §3), and folding
it into `clear` costs nothing as long as the key says which of the two it is about to do — which
is why the changing label is part of RULE-SEARCH-038 rather than an implementation detail.

**Revised during 4d.** This originally said strict insertion order, on the grounds that the user
built the filter one step at a time and the row should read as the record of that. Building the
joining words showed that cannot survive interleaving: apply a platform, then a genre, then a
second platform, and a pairwise reading renders *"Genesis and Sports or SNES"* — a grouping the
algebra does not use. Same-facet chips have to be adjacent for RULE-SEARCH-067 to be true where
it stands.

Grouping keeps what the original rule was protecting. It is not a sort — nothing is alphabetised,
nothing jumps to the front — so the row still reads as a record of what the user did at the level
that matters: which facets they narrowed on, and in what order. A new chip joins the end of its
own facet's run rather than the end of the row.

Removal re-running the query rather than rebuilding from scratch is the whole point of the
filter set being a computed intersection instead of a destructively narrowed list. Step 6 of
the §1 script — remove Sega Genesis, see EA Sports games on every platform — costs one Enter
press and no retyping.

---

## 7. Technical design

### 7.1 Data

Everything needed is a projection of an index `GameCatalog` already builds at startup for
browsing. `ByCategory(listCategoryType)` returns `ILookup<string, GameMatch>` — value to games,
already grouped, already covering every facet in §2.

`FacetIndex` turns that into search-shaped data once, on the background build:

```csharp
sealed class FacetTerm
{
    ListCategoryType Facet;      // which dimension
    string Value;                // "Sega Genesis" - as displayed
    string[] Tokens;             // analysed, for matching
    int[] Games;                 // ascending catalog indices
}
```

Plus the same term/posting structures the title index uses, over facet-value tokens instead of
title tokens, so §5.1's matching is the same code as the parent plan's §5.3.

Size: one entry per distinct metadata value in the library, and the posting lists together hold
one `int` per game-value pair — the same fan-out `Expand` already produces in memory as object
references, at four bytes instead of eight and without the `ILookup` overhead.

### 7.2 Evaluating the filter set

```
filterSet = all games
for each facet with chips:
    facetSet = union of the chips' postings                       (RULE-SEARCH-051)
    filterSet = intersect(filterSet, facetSet)                    (RULE-SEARCH-050)
```

Sorted `int[]` throughout, so union and intersection are linear merges and the intersection is
driven from the smallest list. Recomputed from the chips on every change rather than maintained
incrementally: chips are removable in any order, so an incrementally narrowed set would have to
be unwound, and at these sizes the recomputation is not measurable.

The result feeds the title query as its candidate set (parent plan §5.6) — one more
intersection, in the same shape as everything else.

### 7.3 Suggestion evaluation

Per keystroke, once suggestions are active:

1. Analyse the buffer.
2. Collect candidate facet terms by exact / prefix / fuzzy match (§5.1), capped generously.
3. For each candidate, `count = |intersect(candidate.Games, filterSet)|` — count only, no
   materialisation.
4. Drop zero counts (RULE-SEARCH-055).
5. Rank (RULE-SEARCH-057), apply the per-facet cap (RULE-SEARCH-058), take the top N.

Step 3 is the only new cost, it is a counting merge over sorted arrays, and it runs on a few
dozen candidates. It sits comfortably inside the parent plan's 16 ms per-keystroke budget, and
it is worth measuring on a large library precisely because it is the one part of this design
whose cost scales with facet cardinality rather than with library size.

### 7.4 Files

Adds to the parent plan's `Service/Search/` folder:

```
FacetIndex.cs        facet terms, their postings, and matching over them
FilterSet.cs         the algebra of §3 and §7.2                      (pure)
SuggestionRanker.cs  §5.1, §5.2, §7.3                                (pure)
SearchChip.cs        one applied term                                (pure)
```

Three of four are pure functions over `int[]` and plain values — testable with object literals,
no catalog, no Big Box.

### 7.5 The boundary between these four

Parent plan §7.6 states the contract for the whole feature. It is repeated here with the
specifics of this sub-feature because these four components are where it is most likely to
erode: they are conceptually adjacent, they are written in the same sitting, and every shortcut
between them looks reasonable at the time.

| Component | Answers | Knows nothing about |
|---|---|---|
| `FacetIndex` | "here are the metadata terms, and the games carrying each" | selection, chips, sessions, screens |
| `FilterSet` | "given these selected terms, these games survive" | how the terms were chosen, or by whom |
| `SuggestionRanker` | "given this text and this candidate set, these terms are relevant" | that anything will be applied afterwards |
| `SearchChip` / `SearchSession` | "the user has selected these terms" | how matching or counting works |

`FacetIndex` in particular is **library data, not session data**. It is built once from the
catalog and is valid for every search that will ever run in the session. The moment a
`FacetTerm` grows an `IsSelected`, `IsOffered` or `DisplayIndex` field, it stops being library
data, stops being shareable, and stops being testable independently of a session — and the
change that does it will look like a one-line convenience. Selection state belongs on the
session; the ranker is *told* what is already applied, it does not store it.

The same applies in the other direction: `SuggestionRanker` returns terms and counts. It does
not return view models, does not sort for display, and does not know that a screen exists.

---

## 8. Staging within stage 4

| Step | Delivers |
|---|---|
| 4a | `FacetIndex` built from the existing category index, plus tests. Nothing user-visible. |
| 4b | `FilterSet` and the algebra of §3, plus tests for both branches of the single/multi-valued split. Still nothing visible. |
| 4c | Suggestion list rendered and navigable, **with counts and facet labels** (RULE-SEARCH-056, §5.3); selecting a suggestion adds a chip and consumes the query (RULE-SEARCH-060/061); chip row visible but not yet focusable. **First visible increment** — already delivers the §1 script steps 1-4. |
| 4d | Chip row focusable and removable (RULE-SEARCH-064/065), same-facet joining words (RULE-SEARCH-067), clear-all (RULE-SEARCH-068) — script steps 5-6. |
| 4e | Per-facet capping polish, suggestion threshold tuning (RULE-SEARCH-059), ranking adjustment against a real library. |

4c is where the feature becomes real, and 4a-4b are entirely unit-tested before any of it
reaches a screen.

Note what moved out of 4e. Counts (§5.3) and the joining words (RULE-SEARCH-067) both read as
appearance items and are both load-bearing: the counts are the only preview mechanism a
controller user has, and the joining words are what make the §3 algebra legible rather than
hidden. Anything that ships without them ships a filter system the user has to guess at, so
they belong with the increments they explain. What is left in 4e is genuinely tuning — the
things that need a real library and an opinion, not a decision.

---

## 9. Verification

| ID | Scenario | Kind | Covers |
|---|---|---|---|
| VER-SEARCH-020 | Two genre chips return only games carrying both; two platform chips return games carrying either | Unit | RULE-SEARCH-051/052 |
| VER-SEARCH-021 | A genre chip and a platform chip combine with AND | Unit | RULE-SEARCH-050 |
| VER-SEARCH-022 | Free text plus a chip narrows to titles matching the text within the chip's games | Unit | RULE-SEARCH-053 |
| VER-SEARCH-023 | A term with no games in the current filter set is not offered; every offered term's count matches the set it produces | Unit | RULE-SEARCH-054/055/056 |
| VER-SEARCH-024 | With nine matching developers and one matching genre, the genre is still offered | Unit | RULE-SEARCH-058 |
| VER-SEARCH-025 | Removing a chip restores exactly the set that existed before it was added, whatever order chips were added in | Unit | RULE-SEARCH-065 |
| VER-SEARCH-026 | The §1 script, end to end, on a real library | Manual | the whole sub-feature |
| VER-SEARCH-027 | Suggestion evaluation stays within budget on a library with high facet cardinality (many developers/publishers) | Manual, measured | §7.3 |
| VER-SEARCH-028 | Selecting an already-applied term removes it rather than duplicating it; the same term cannot appear as two chips by any sequence of selections; an applied term renders as selected in the suggestion list | Unit | RULE-SEARCH-061 |
| VER-SEARCH-029 | Selecting a suggestion empties the query buffer; free text surviving alongside chips filters titles within the filter set | Unit | RULE-SEARCH-060, §5.4 invariant |
| VER-SEARCH-030 | Clear-all removes every chip and leaves the buffer empty; clear with text present removes only the text | Unit | RULE-SEARCH-068 |
| VER-SEARCH-031 | The golden corpus, extended with facets: `Genre=Sports + Genre=Football`, `Platform=Genesis + Platform=SNES`, `Genre=Sports + Platform=Genesis`, and `spo`/`fo`/`ea`/`seg` each produce the expected suggestion list with the expected counts | **Integration — unit-testable** | §3, §5, §7.2 together |

Nine of eleven are unit tests over `int[]`, which is the strongest reason to build 4a and 4b
before anything is rendered.

`VER-SEARCH-031` is the facet half of the golden corpus described in parent plan §12.1, and it
is the test that catches what the individual unit tests cannot: the algebra, the suggestion
ranking and the zero-result suppression interacting. The corpus fixture must therefore carry
real metadata — at minimum a game in both *Sports* and *Football*, several platforms, a
publisher whose name contains a word that is also a genre (*EA Sports*), and a facet value
reachable only through another filter. Those cases are listed in §12.1 of the parent plan and
should be added to the same fixture rather than to a second one.

---

## 10. Open questions

| ID | Question |
|---|---|
| OQ-028 | ~~RULE-SEARCH-051 ANDs same-facet multi-valued chips, per the request. Retail convention ORs.~~ **Resolved** — after use, ORs uniformly. The AND was structurally empty outside genre, and genre intersection was given up for one rule. A per-chip AND/OR toggle remains the fix if real use wants both back; still unproven whether it is worth the interaction cost on a d-pad. |
| OQ-029 | Should free text and chips be reorderable, i.e. should selecting a suggestion be undoable with an "undo last" rather than by finding the chip? |
| OQ-030 | Should a committed search's chips be visible while browsing the results, so the user can see what they are inside without re-entering search? |
| OQ-031 | Should facets the user never uses (playlist, play mode, release year on some libraries) be suppressible in settings, to keep the suggestion list dense? |
| OQ-032 | Should a chip be creatable from the currently selected game while browsing — "more games by this publisher" as a filter rather than as a "more like this" set? It would connect this feature to FEAT-BROWSE-008. |
