# Plan — Text search with an on-screen keyboard

> **Status: designed, not started.** No code has been written. This document is the
> functional and technical design for the most-requested missing feature in Eclipse.
>
> The metadata filter half is large enough to stand on its own and is designed separately in
> [text-search-metadata-filters.md](text-search-metadata-filters.md). This document owns the
> screen, the input model, the matching engine and the results, and states where the filter
> sub-feature plugs in.
>
> Companion reading: [../features/voice-search.md](../features/voice-search.md) (EPIC-SEARCH
> as it stands), [../features/browsing.md](../features/browsing.md) (the list-set model the
> results reuse), [../features/input.md](../features/input.md) (the eight-input contract that
> constrains the keyboard).

---

## 1. Why this, and why now

[../features/voice-search.md](../features/voice-search.md) opens with a single sentence that
is the whole problem:

> Eclipse has **no text search**. Voice is the only search modality.

Every recurring request on the forums is a consequence of that sentence:

| Request | Why voice does not answer it |
|---|---|
| A text search box | There is none |
| Controller-accessible search | Voice needs a microphone, not a controller |
| Search without a microphone | Shield / Moonlight streaming has no local mic path |
| Search for a child who does not speak English | Speech recognition is trained on the host language |
| Search a library of several thousand games without scrolling | Browsing is the only alternative and it is linear |
| Filter by metadata rather than title | Matching is title-only by construction |
| Joystick-based search (asked again as late as April 2025) | Same as controller-accessible |

The prior proof of concept — typing produces matching metadata values that can be selected as
filters — is the right shape. This plan is that idea, designed against the code as it exists
today rather than as it existed in 2023.

**Scope note.** Text search does not replace voice search and does not change it. They are two
front ends over the same library, they install their results the same way, and neither is a
prerequisite for the other. Nothing in `Service/SpeechRecognizer.cs`,
`Service/VoiceSearchIndex.cs` or `Models/GameTitleGrammarBuilder.cs` is touched by stages 1-4
below. Section 11 explains why converging them is a *later* decision gated on tests that do
not exist yet.

---

## 2. What the user gets

### 2.1 The search screen

A full-screen surface, reached from browsing, with four interactive zones and a live result
row. The result row is the point: **the library filters as you type**, so a search is a
conversation rather than a form you submit.

```
 col 0                    11                20                       31
    +-------------------------------------------------------------------+
 r0 |  SEARCH                                             142 games      |
 r1 |  son_                                                              |   query line
 r2 |  [ Genre: Platform  x ]  [ Platform: Sega Genesis  x ]             |   filter chips
 r3 +--------------------+-------------------------+--------------------+
 r4 |   A  B  C  D  E  F |  Sonic          series  |                     |
 r5 |   G  H  I  J  K  L |  Sonic Team     dev  18 |    box art of the   |
 r6 |   M  N  O  P  Q  R |  Sony           pub   9 |   selected result   |
 r7 |   S  T  U  V  W  X |                         |                     |
 r8 |   Y  Z  0  1  2  3 |                         |                     |
 r9 |   4  5  6  7  8  9 |                         |                     |
r10 |  [space][del][clr] |                         |                     |
r11 +--------------------+-------------------------+--------------------+
r12 |  Results                                                           |
r13 |  [box][box][box][box][box][box][box][box][box][box][box] ...       |
r17 +-------------------------------------------------------------------+
```

The layout sits on the same 32 x 18 design stage as everything else
(`LayoutGeometry`, `ApplyStageGeometry`), so it keeps its proportions on any display and the
surplus row on a 16:10 panel goes to the results band exactly as it does to the game list.

### 2.2 The interaction, end to end

1. The user presses the search key while browsing. The screen opens with the previous
   session's query and filters intact (§4.6).
2. They press `S`, `O`, `N`. After the second character the results row is already showing
   Sonic games and the suggestion column is already offering `Sonic` as a series.
3. They can stop here — move Down to the results, pick a game, press Enter, play it. Three
   letters and a move.
4. Or they select the `Sonic` suggestion. It becomes a chip, the query line clears, and the
   result set narrows to the series.
5. They type `gen`, select `Sega Genesis`, and now hold two chips. Type `fo`, select the
   `Football` genre, three chips. Every chip is removable, and removing one widens the results
   again without retyping anything (see the sub-feature document).
6. Pressing Enter on the results row hands the whole ranked result set to the ordinary
   browsing surface as a list set, and the search screen closes.

### 2.3 Features

| ID | Feature |
|---|---|
| FEAT-SEARCH-010 | **On-screen keyboard.** A key grid navigated with Up/Down/Left/Right; Enter types the highlighted key. Space, Backspace and Clear are keys in the grid, because the input contract has nowhere else to put them. |
| FEAT-SEARCH-011 | **Live title search.** The result row updates on every keystroke against the whole library, ranked by relevance. |
| FEAT-SEARCH-012 | **Typo tolerance.** `sonik` finds *Sonic the Hedgehog*; `castlevaina` finds *Castlevania*. Applied only once the typed term is long enough for a typo to be distinguishable from an early prefix (§5.4). |
| FEAT-SEARCH-013 | **Metadata term suggestions.** What is typed is matched against metadata values as well as titles, and matching values are offered as selectable filters. See the sub-feature doc. |
| FEAT-SEARCH-014 | **Stacking filters.** Selected terms become chips that combine; the result set narrows with each. See the sub-feature doc. |
| FEAT-SEARCH-015 | **Removing filters.** Any chip can be toggled off, widening the results without retyping. See the sub-feature doc. |
| FEAT-SEARCH-016 | **Commit to browsing.** The result set is installed as a list set and browsed with the ordinary row UI (§6). |
| FEAT-SEARCH-017 | **Session memory.** Re-entering search restores the last query, chips and selection for the session. |
| FEAT-SEARCH-018 | **Empty-result feedback.** A search that matches nothing says so, says which chip is responsible when one is, and never leaves a blank screen. |
| FEAT-SEARCH-019 † | **Search index.** A token index over titles and metadata values, built once in the background after the catalog is ready. |

`†` supporting rather than user-facing, following the convention in
[../FEATURES.md](../FEATURES.md).

---

## 3. The constraint everything else is shaped by

Eclipse observes **eight inputs and nothing else**: Up, Down, Left, Right, Enter, Escape,
Page Up, Page Down — with a *held* flag on the four directions
([../features/input.md](../features/input.md)). There is no keyboard event, no character
input, no shoulder button, no mouse. Big Box reads the devices; Eclipse is told about eight
things.

Three consequences drive the whole design:

1. **Every character costs several inputs.** A five-letter word costs roughly 12-20 button
   presses. The design must make short queries productive: suggestions after two characters,
   results live from the first, and filters that persist so refining never means retyping.
2. **Backspace, Space and Clear must be keys on the grid.** They cannot be a second button.
3. **Page Up / Page Down are already user-remapped to browse functions** (RULE-INPUT-007,
   RULE-INPUT-008). Inside the search screen those functions are meaningless, so they are
   rebound locally (RULE-SEARCH-042) rather than routed through `KeyStrategyCache`.

---

## 4. Input model

### 4.1 Zones and focus

The screen has four focusable zones. Exactly one holds focus.

| Zone | Present when | Contents |
|---|---|---|
| `Keyboard` | always | the key grid |
| `Suggestions` | at least one suggestion | vertical list of metadata terms |
| `Chips` | at least one filter applied | horizontal list of applied filters |
| `Results` | at least one result | the box-art row |

Focus opens on `Keyboard` and returns there whenever the zone it was in disappears.

### 4.2 Rules

| ID | Rule | Why |
|---|---|---|
| RULE-SEARCH-030 | Left/Right move within the focused zone's row and **do not wrap** when an adjacent zone lies in that direction; they wrap when none does. | The zone layout is horizontal. Wrapping unconditionally, as browsing does (RULE-BROWSE-008), would make the suggestion column unreachable. This is a deliberate divergence from RULE-BROWSE-008 and is confined to this screen. |
| RULE-SEARCH-031 | Up/Down move within the focused zone's column; at the zone's top or bottom edge they move to the zone above or below. | Two-dimensional movement inside the keyboard, vertical traversal between zones. |
| RULE-SEARCH-032 | The zone order top to bottom is `Chips`, `Keyboard`/`Suggestions`, `Results`. Zones with no content are skipped, never focused, and never shown. | An empty zone that can still be entered is a dead end the user has to guess their way out of. |
| RULE-SEARCH-033 | Enter on a keyboard key applies that key. Enter on a suggestion applies it as a filter. Enter on a chip removes that filter. Enter on a result opens the game detail overlay, exactly as it does while browsing. | One button, and its meaning is always "act on what is highlighted". |
| RULE-SEARCH-034 | Escape always leaves the search screen and returns to browsing, whatever the focused zone. The session is kept (RULE-SEARCH-035). | A single, predictable exit. An Escape ladder that unwinds zone by zone means the user cannot tell how many presses gets them out. |
| RULE-SEARCH-035 | Leaving search keeps the query, the chips and the result selection for the lifetime of the Big Box session. Re-entering restores them. | Makes RULE-SEARCH-034 safe: an accidental Escape costs nothing, because the work is still there. |
| RULE-SEARCH-036 | Left never opens the options pane inside the search screen, whatever `OpenSettingsPaneOnLeft` says. | RULE-INPUT-002 is a browsing rule. Falling into the options pane from the leftmost key of the keyboard would be indefensible. |
| RULE-SEARCH-037 | Holding a direction repeats the move. Held movement never crosses a zone boundary. | Lets the user travel across the key grid quickly without shooting past it into the results. |
| RULE-SEARCH-038 | The `clear` key is context-sensitive: it clears the query buffer while there is text, and clears **all applied filters** when the buffer is empty and chips exist. Its label changes to say which it will do. | With eight inputs there is no room for a separate "clear filters" key, and a user who has over-narrowed needs one press to start again rather than one press per chip. The changing label is what stops a context-sensitive destructive key from being a surprise — it is part of the rule, not decoration. |
| RULE-SEARCH-042 | Inside the search screen, Page Up is Backspace, regardless of how the user has mapped it. Page Down does nothing. **Hard-coded**; making it a setting is deferred to `OQ-024`. | Its configured browse function has no meaning here, and one free button makes text entry materially faster — on QWERTY, reaching the `delete` key and coming back costs about eight presses against one. Page Down originally committed the search; under the overlay design (§6) the results are already the live list, so there is nothing left to commit. |

### 4.3 Keyboard layout

Six columns by six rows for `A`-`Z` and `0`-`9`, then a function row: `space`, `⌫`, `clear`.
Thirty-nine cells, seven rows, one shape. `clear` is the context-sensitive key of
RULE-SEARCH-038 and renders as `clear` or `clear filters` depending on what it will do.

**Alphabetical by default, QWERTY optional.** Alphabetical is what Netflix and tvOS use on
exactly this input device, and it is the layout a child or a non-English speaker can navigate
without knowing the layout in advance — which is one of the requests this feature exists to
answer. QWERTY is faster for anyone who already knows where the letters are, so it is offered
as a setting rather than argued about.

Punctuation is deliberately absent: the analysis chain (§5.2) strips it from titles too, so
there is nothing a user could type with it that they cannot type without it.

### 4.4 Suggestions

Designed in the sub-feature document. From this document's point of view a suggestion is a
`(facet, value, count)` triple that becomes a chip when selected.

### 4.5 Query line

Shows the typed text and a caret, plus the live result count on the right. The count is the
most valuable feedback on the screen: it tells the user whether the next character will help
or whether they have already over-narrowed.

### 4.6 Session

One `SearchSession` object holds the query buffer, the chip list, the focused zone and the
per-zone selection indices. It is created once and survives leaving the screen
(RULE-SEARCH-035). It does not survive a restart — persisting a search across sessions would
mean a user who searched last night starts Big Box tomorrow inside a filtered library with no
memory of why.

---

## 5. The matching engine

This section answers the question directly: *is there a better industry-standard approach
than expanding every title into every contiguous run of its words?*

### 5.1 What the current approach is, and why it is the way it is

`VoiceSearchIndex.BuildPhrasesForGame` registers, for every game, every contiguous run of
words in the title from every starting position, plus the full title, main title and subtitle
(RULE-SEARCH-005). *The Legend of Zelda: A Link to the Past* produces on the order of forty
phrases. That is O(w²) per title in both index size and build time, and it is recorded as
`S-8` — "the largest contributor to index size".

**This is not a mistake for voice.** `System.Speech` needs an *enumerated grammar*: the set of
utterances it is allowed to recognise has to be handed to it up front. There is no way to say
"any sub-phrase of these titles". The expansion is the price of the recogniser.

**For text it is unnecessary**, because the query arrives incrementally and can be intersected
against the index at query time instead of being pre-enumerated. That single change is the
biggest structural improvement available:

| | Phrase expansion | Token index |
|---|---|---|
| Entries for a 7-word title | ~30 | 7 |
| A 100,000-game library | millions of phrase entries | ~600,000 postings (~2.4 MB as `int`) |
| Multi-word query | must have been enumerated at build time | intersect postings at query time |
| Partial word (`son` → `sonic`) | not supported — phrases are whole words | prefix range over the sorted term array |
| Typo (`sonik` → `sonic`) | not supported | bounded edit distance over the term dictionary |

### 5.2 Analysis chain

The standard first stage of any search engine, and the part most worth getting right, because
everything downstream inherits its mistakes. Applied identically to indexed text and to query
text — that symmetry is what makes the engine predictable.

| Step | Detail |
|---|---|
| Case fold | invariant lower-case |
| Unicode fold | NFKD, strip non-spacing marks — `Pokémon` and `Pokemon` become the same token |
| Symbol fold | `&` → `and`; `+` → `plus` |
| Apostrophes | removed without inserting a space, so `Rock n' Roll` yields `rock`, `n`, `roll` |
| Punctuation | everything else becomes a space |
| Whitespace | collapse, trim |
| Tokenize | split on space |
| Numeral folding | **additive, not destructive**: `vii` also indexes as `7`, `7` also indexes as `vii` and as `seven` |

The last row is a deliberate improvement over the voice path. `GameTitleGrammar` *replaces*
roman numerals with digits (RULE-SEARCH-004), which means the digit form is findable and the
roman form is not. Indexing both as alternates for the same position costs a few extra
postings and lets the user type whichever they see on the box. The `X` exclusion that
RULE-SEARCH-004 carries — too many titles contain a literal X — disappears with it, because an
additive alternate for `x` never removes the ability to match the letter.

Article handling is nearly free: LaunchBox already supplies `SortTitleOrTitle`, so `Legend of
Zelda, The` can be indexed alongside the display title with no special casing.

### 5.3 Index structure

```
terms      : string[]   sorted ordinal, distinct         ("castlevania", "sonic", ...)
postings   : int[][]    parallel to terms, ascending catalog indices
trigrams   : Dictionary<int, int[]>   trigram hash -> term ids     (fuzzy candidates)
```

* **Exact term** — binary search of `terms`.
* **Prefix term** — two binary searches give the `[start, end)` range of terms sharing the
  prefix; union their postings. This is what an "edge n-gram" or as-you-type query is, without
  the index bloat of materialising every prefix.
* **Fuzzy term** — §5.4.

Postings are **catalog indices, not object references**. `GameCatalog.Setup` already orders
the library deterministically (`SortTitleOrTitle`, then `Id`) precisely so downstream sorts are
reproducible, so position in `GameCatalog.Games` is already a stable identity. Integers make
set intersection cheap and keep the index at a few megabytes on a very large library.

### 5.4 Typo tolerance

The industry answer, in three layers, all of which Eclipse can implement in-process without a
dependency:

**Candidate generation — character trigrams.** Index each term's overlapping 3-grams
(`sonic` → `son`, `oni`, `nic`). A misspelling shares most of its trigrams with the correct
term, so candidates are the terms sharing at least one trigram with the query term. This is
what PostgreSQL's `pg_trgm` does. It exists so the next layer never has to scan a 30,000-term
dictionary.

**Verification — bounded Damerau-Levenshtein.** Edit distance with transposition, computed with
a band limited to the maximum allowed distance and abandoned as soon as the band is exceeded.
Transposition matters: `teh`/`the` and `sonci`/`sonic` are one Damerau edit and two plain
Levenshtein edits.

**Budget — length-dependent, structured the way Elasticsearch's `fuzziness: AUTO` is, but
shipped one notch more conservative:**

| Query term length | Edits allowed — **ship with** | Elasticsearch `AUTO` |
|---|---|---|
| 1-3 | 0 | 0 |
| 4-6 | 1 | 1 |
| 7+ | **1** | 2 |

Below four characters a "typo" is indistinguishable from an early prefix, and allowing edits
there turns every short query into noise — `son` would match `sun`, `sin`, `sos`, `ton`, `won`.
This is the rule that makes fuzzy matching feel like help rather than interference.

Two edits on a long term is where fuzzy matching starts producing results the user cannot
explain, and game titles are full of unusual names that make it worse. Every example this plan
claims to solve is reachable at distance one — `sonik`/`sonic` is one substitution,
`castlevaina`/`castlevania` is one transposition — so shipping at one costs nothing
demonstrable and avoids the class of result that makes people distrust a search box.

The **tiered structure stays in the code** — `MaxEdits(int length)` is one function with three
branches — so raising the 7+ tier to two is a one-line change made on evidence from a real
library rather than a rewrite. Do not flatten it to a constant.

**The last term is never fuzzy while it is still being typed.** A term the user is part-way
through is a prefix by definition; treating it as a misspelling of a complete word produces
results that jump around as they type. Fuzzy applies to terms followed by a space, and to the
final term only when a prefix search returned nothing.

**Optional fourth layer — phonetic.** Double Metaphone reduces `sonic` and `sonik` to the same
code, and does the same for `fantasy`/`fantasee` and `knight`/`nite`. It catches the class of
error a child or a non-native speaker makes, which is the population this feature exists to
serve, and it costs one more index keyed by phonetic code. Deliberately staged last (stage 5)
because it is additive and because it should be judged against the edit-distance lane on a real
library rather than in advance.

### 5.5 Ranking

Titles are short documents from a homogeneous corpus, so BM25's document-length and term-rarity
machinery earns little here and costs predictability. A fixed additive table is easier to tune,
easier to explain, and easy to unit test — and it lives in one place rather than being spread
across the scorer the way the voice percentages currently are.

> **Revised after stage 2, on evidence.** This section originally specified a single additive
> score. It was built that way, shipped, and the golden corpus immediately caught the problem
> the section itself predicted — see "Why not one number" below. What follows is the design as
> it now stands.

**Rank by comparing signals in order of authority, not by adding them up.** Match quality
decides; the softer signals only break ties beneath it. A weak signal can never outvote a strong
one, because they are never summed.

| Order | Signal | Value |
|---|---|---|
| 1 | **Match quality**, averaged across query terms | exact 100; prefix 80 + 20 × (query length ÷ term length); fuzzy 1 edit 55, 2 edits 40; phonetic 45 (stage 5) |
| 2 | **Popularity** | 0–5 from star rating and play count |
| 3 | **Brevity** | 5 × (1 − min(1, title tokens ÷ 8)) |
| 4 | **Matched the first token** | boolean |
| 5 | **Catalog index** | ascending — deterministic, and it is sort-title order |

All query terms must match (AND). Two terms narrow; they never widen.

**Why not one number.** The numbers were the problem. A flat +15 for matching a title's first
word is larger than the entire popularity range plus the entire brevity range combined, so it
silently decided every result where match quality tied. For `zelda` it put *Zelda II: The
Adventure of Link* above *The Legend of Zelda* — the sequel starts with the word, the original
has it fourth behind an article. No choice of weights fixes that class of problem in general;
they only move which case is wrong. Ordering removes the possibility.

**Why popularity, and why second.** Nothing in the *text* distinguishes those two Zelda titles —
both contain the word exactly, and the sequel has the better textual claim. What separates them
is which one is famous, and the only evidence of that available here is the rating and the play
count. The plan originally guessed popularity "may turn out not to belong in a search ranking at
all"; the evidence says the opposite. It belongs, and it was being shouted over.

**Why this is robust without ratings.** On a library where nothing is rated, popularity ties at
zero everywhere and brevity decides — and brevity points the same way: four words beats six.
The answer is right either way, which is what makes it safe to ship to libraries we cannot see.

The values in the table are still initial constants. What is no longer up for tuning is whether
a weak signal can outvote a strong one — it cannot, by construction.

The **coverage** term from the original table is gone. It was `+10 × (matched ÷ total)`, which is
constant at 1.0 under strict AND, and in an ordered comparison a constant cannot break a tie.

**All query terms must match** (AND). Two terms narrow; they never widen. This is the behaviour
every user expects from a search box, and it is the opposite of what the voice path does, where
each recognised phrase produces its own list.

The constants belong in one table with this document's reasoning beside them. The existing
scorer's comment — *"Feels like this shit could be tweaked endlessly and never settle on an
ideal solution?"* (`GameMatch.SetupVoiceMatchPercentage`) — is correct, and the answer to it is
not a better formula but putting the formula somewhere a test can pin it.

### 5.6 Performance

| Concern | Design |
|---|---|
| Build cost | Built in the background after `GameCatalog` is ready — the same lesson `VoiceSearchIndex` already learned. Never on the startup path. |
| Build time | Dominated by tokenizing every title. Parallel over games, merged in catalog order, exactly as `VoiceSearchIndex.Setup` does today, so results are reproducible. |
| Query cost | Binary search plus posting-list intersection, smallest list first. Target: under 16 ms per keystroke on 100,000 games, so the row updates within one frame. |
| Memory | Terms plus `int` postings. ~2.4 MB of postings at 100,000 games; the term strings dominate and are still small. |
| Result row | Only the top N (200) are materialised into the live row. The full ranked set is materialised on commit. |
| Artwork | Box art warming is debounced ~150 ms after the last keystroke, so typing does not queue a decode per character. `RowImageDecoder` already caps at 96 decoded images. |

Note what is deliberately **not** designed in: an incremental result cache keyed by query
prefix. It only helps when matching is monotonic, fuzzy matching is not monotonic, and this
repository has twice closed a performance item by measuring it (`B-28`, `B-31`) rather than
building for it. Measure first.

---

## 6. Presenting results

The question in the request — multi-row, single list, or a Netflix-style wall — has a clean
answer once the two moments are separated.

### 6.1 While typing: one live ranked row

The result row inside the search screen is a **single row, ranked, no grouping**. It is the
feedback loop that makes short queries work, and it is nearly free: `GameList` already is a
ranked row of games with a moving window, and `VoiceSearchResultBuilder` already demonstrates
the pattern of constructing one from scored matches.

Grouping while typing would be wrong regardless of cost. The set is changing on every
keystroke; rows appearing and disappearing underneath a moving selection is disorienting, and
the user's question at this moment is "am I getting warmer", which one ranked row answers and a
shifting set of rows obscures.

### 6.2 On commit: the ordinary browsing surface, several rows

> **Superseded twice over. The rows described below are not being built** - stage 5
> derives its rows from the applied constraints instead, which is deterministic, names
> every row for free, and works in the case that actually hurts: a result set collapsed
> to two games has no variety left to group by. See
> [text-search-faceted-rows.md](text-search-faceted-rows.md), which replaces everything
> below this note about what the rows contain.

> **Superseded in part by the stage 2 overlay rework.** There is no longer a commit step: the
> results are installed as a `GameListSet` under `ListCategoryType.TextSearch` and shown *live*,
> on every keystroke, so the browsing surface is already displaying them while the user types.
> Enter on a result closes the panel and opens the game; Escape closes it and restores the
> user's previous position. What survives from this section is the shape of the installed set —
> which is still one ranked list, with the faceted rows below still belonging to stage 5.

Pressing Enter on the results row installs the results as a
`GameListSet` under a new `ListCategoryType.TextSearch` and closes the search screen. The user
lands in the normal browsing UI with:

* **Best matches** — the top of the ranked set, as one row.
* Then one row per dominant facet value in the result set — per platform, per genre, per series
  — capped at a handful of rows and ordered by size.

This is exactly what a streaming service's search results page does, it reuses
`Navigator.InstallSet` / `ShowCategory` unchanged, and it needs no new presentation code at
all: voice search already installs a result set this way
(`VoiceRecognitionState.RecognizeCompleted`), and "more like this" already builds a set of rows
from a game's metadata (`GameListBuilder.BuildMoreLikeThis`).

### 6.3 On a wall view

A grid of box art is a good idea and it is **not part of this feature**. It is an EPIC-PRESENT
capability that would improve browsing as much as search, and making search wait for it would
be the single easiest way to stop search from shipping. It also carries constraints worth
recording before anyone starts:

* Box art is **pre-scaled on disk to one height** — the row height, from
  `ImageScaler.GetDesiredHeight()`. A wall of smaller boxes renders downscaled from that cache,
  which is fine; a wall of *larger* boxes has no source to scale up from.
* `RowImageDecoder` caps at 96 decoded images, sized from the row's 13 slots plus lookahead. A
  static wall of 24-30 fits comfortably. A long scrolling wall does not, and `B-31` records the
  deliberate decision not to virtualize the row.

So: design it separately, on its own evidence, after search exists.

---

## 7. Technical design

### 7.1 New files

```
Service/Search/
  TextAnalyzer.cs          normalize, tokenize, numeral alternates      (pure)
  SearchableGame.cs        the projection the engine works on           (pure)
  TitleIndex.cs            terms, postings, prefix range, trigrams
  FuzzyMatcher.cs          bounded Damerau-Levenshtein, edit budget     (pure)
  SearchScoring.cs         the ranking table                            (pure)
  SearchQuery.cs           parse a buffer into terms                    (pure)
  SearchEngine.cs          query -> ranked int[]                        (pure over an index)
  SearchIndex.cs           builds and owns TitleIndex + facet index
  ISearchIndex.cs          the narrow seam, following IVoicePhraseIndex
  FacetIndex.cs            see the sub-feature document
  SearchSession.cs         query buffer, chips, focus, selections
Models/
  SearchKeyboard.cs        key grid, layouts, cursor movement           (pure)
State/
  TextSearchState.cs       the eight inputs for this screen
State/KeyStrategy/
  KeyStrategyTextSearch.cs enters the screen from a page key
View/
  SearchView.xaml(.cs)     the screen
```

Six of those are pure functions over plain data. That is the point of the shape.

### 7.2 The seam that makes it testable

Everything in the engine works on `SearchableGame` and `int`, never on `IGame`:

```csharp
public sealed class SearchableGame
{
    public int CatalogIndex { get; }        // position in GameCatalog.Games
    public string DisplayTitle { get; }
    public IReadOnlyList<string> TitleTokens { get; }
    public int Popularity { get; }
}
```

One adapter projects `GameMatch` into it, and that adapter is the only search code that knows
LaunchBox exists. This is `B-12`/`S-2` applied to one feature rather than to the whole product
— the same move `IGameCatalogSource` and `IVoicePhraseIndex` already made, for the same reason,
and it is what lets the entire matching engine be exercised against object literals in
`Eclipse.Tests` with no Big Box behind it.

### 7.3 Where it plugs into what exists

| Touch point | Change |
|---|---|
| `Models/Option.cs` — `ListCategoryType` | add `TextSearch` |
| `Models/Option.cs` — `IsValidForCustomList` | return `false` for `TextSearch`, beside the existing three |
| `Models/Enums.cs` — `PageFunction` | add `TextSearch` |
| `State/KeyStrategy/IKeyStrategy.cs` — `KeyStrategyCache` | one `case` for the new function |
| `Service/OptionListService.cs` | add the "Search" picker entry, gated on the enable setting, exactly as voice search is |
| `State/EclipseStateContext.cs` | `DoTextSearch()`, beside `DoVoiceSearch()` |
| `View/MainWindowViewModel.cs` | expose `SearchSession`; **`IsDisplayingSearch` already exists and is never read or written anywhere** — it becomes this feature's visibility flag |
| `Service/GameCatalog.cs` | expose facet posting lists (prerequisite P4) |
| `Eclipse.csproj` | register `View/SearchView.xaml` as a `<Page>` — default page globbing is off (`EnableDefaultPageItems=false`), so a new view that is not listed silently fails to compile as a page |
| `Models/EclipseSettings.cs` | the settings in §7.5 |

### 7.4 Threading

`SearchIndex` builds on a background task after `GameCatalog` is ready, behind the same
`volatile bool` + `lock` + publish-then-flag pattern `GameCatalog` and `VoiceSearchIndex`
already use, so a caller arriving mid-build waits rather than starting a second build.

Queries run on the UI thread. At the target of under 16 ms per keystroke that is correct — a
background query would need cancellation, ordering and marshalling to buy nothing. If
measurement on a very large library says otherwise, the fix is last-keystroke-wins on a
background task, and the seam for it is `SearchEngine` already being a pure function.

Availability is reported the way voice search reports it: a `SearchAvailability` the screen can
show ("still getting ready") rather than a keypress that appears to do nothing. `M-6` is the
record of what happens when a feature that is not ready yet says nothing.

### 7.5 Settings

One schema change rather than five. Every property declares its default with `[DefaultValue]`
and `DefaultValueHandling.Populate` — `EclipseSettingsDataService.GetDefaultSettings`
deserialises `{}` precisely so defaults have one home.

| Setting | Default | Notes |
|---|---|---|
| `EnableTextSearch` | `true` | mirrors `EnableVoiceSearch`; gates the index build and the picker entry |
| `SearchKeyboardLayout` | `Alphabetical` | `Alphabetical` \| `Qwerty` |
| `SearchFuzzyMatching` | `true` | off makes matching strictly prefix-based |
| `SearchMaxSuggestions` | `8` | sub-feature doc |
| `SearchCommitRowCount` | `6` | how many facet rows §6.2 produces |

### 7.6 Boundaries that must not erode

The separation between *search infrastructure* and *search interaction* is the single decision
everything else in this plan depends on, and it is the kind of boundary that dissolves quietly
during implementation — one convenience field at a time, each individually defensible.

Stated as a contract, so a review has something to check against:

| Component | Knows about | Must never know about |
|---|---|---|
| `TextAnalyzer`, `FuzzyMatcher`, `SearchScoring` | strings and numbers | games, catalogs, sessions, screens |
| `TitleIndex`, `FacetIndex` | terms and `int[]` postings | which terms are selected, what is on screen |
| `FilterSet` | selected terms and postings | how terms were chosen, or by whom |
| `SearchEngine`, `SuggestionRanker` | an index, a query, a candidate set | the session, the keyboard, the view |
| `SearchSession` | query buffer, chips, focus, selections | how matching works |
| `TextSearchState`, `SearchView` | the session | indexes, postings, scoring |

Two specific temptations to refuse, because both will present themselves:

* **Caching a suggestion's display state on the `FacetTerm`.** A term is library data; whether
  it is currently offered, selected or highlighted is session data. Putting the second on the
  first is how the index becomes unshareable and untestable in one commit.
* **Letting the engine return view models.** It returns ranked `int[]`. The projection from
  catalog index to `GameMatch` to a row belongs at the boundary, in one place.

`IGameCatalogSource` and `IVoicePhraseIndex` are the existing precedents — both deliberately
narrow, both documented as being narrow on purpose. This is the same move, applied to a larger
feature.

---

## 8. Staging

Each stage builds, ships and is worth having on its own — the incremental-modernization rule in
`CLAUDE.md` is not decoration here, it is the only way a feature this size lands safely.

| Stage | Delivers | Notes |
|---|---|---|
| **0. Prerequisites** | §9 | Nothing user-visible. Do not skip P1. |
| **1. Engine** | `TextAnalyzer`, `TitleIndex`, `SearchEngine`, `SearchScoring`, exact + prefix only, no fuzzy, no facets, plus unit tests **and the golden corpus** (§12.1) | No UI. Fully tested against object literals. This is where the feature is actually proven. |
| **2. Screen** | `SearchView`, `SearchKeyboard`, `TextSearchState`, live result row, commit to a single ranked list set, entry from the picker and a page key | **The first shippable increment, and it already answers most of the forum requests.** |
| **3. Fuzzy** | trigram candidates, bounded Damerau-Levenshtein, the length-tiered budget at one edit, the setting | Purely additive to stage 1; the corpus is what says whether the ranking constants and the edit budget are right. |
| **4. Metadata filters** | the whole sub-feature document | The largest stage. Its own design doc for that reason. |
| **5. Refinements** | faceted rows on commit (§6.2), phonetic lane, session-memory polish | Each independently droppable. |

Stages 2 and 4 are both worth shipping to users. Stage 3 is worth shipping and is invisible
until someone mistypes.

---

## 9. Prerequisites

Every one of these is small; together they are the difference between a testable feature and
another thousand lines that can only be verified by playing. But they are **not one phase**.
Treating them as a gate before any search code is written turns a prerequisite list into
another architectural project, which is exactly the failure mode this repository's staging
discipline exists to avoid.

Each is attached to the stage that actually needs it:

| | Needed before | |
|---|---|---|
| P1 | **stage 1** — the engine | voice characterization tests |
| P2 | **stage 1** | `SearchableGame` and its adapter |
| P3 | **stage 1** | stable integer game identity |
| P4 | **stage 4** — metadata filters | facet posting lists |
| P6 | **stage 2** — the screen | search screen in its own view |
| P5 | *during* stage 2 | `B-35(a)`, before the search set is first restored |
| P7 | *during* whichever stage first needs a setting | settings, landed in one schema change |
| P8 | not needed | composition root |

So the true gate before writing any code is **P1, P2, P3** — one test file and two small
projections. Everything else arrives when the stage that needs it does.

### Before stage 1 — the engine

**P1 — Write `VER-SEARCH-001`, `002` and `003`.** Characterization tests for voice title
decomposition and match scoring. [../VERIFICATION.md](../VERIFICATION.md) already ranks these
2nd and 3rd of everything uncovered, notes they need *nothing* and "can be written today", and
they are pure string and arithmetic functions. They matter here specifically: text search
introduces a second analysis chain, and the day someone proposes unifying it with
`GameTitleGrammar` (§11), these tests are the only thing standing between that refactor and a
silent change to every voice result. **Do this first even if the rest of the list is skipped.**

**P2 — Introduce `SearchableGame` and its adapter (§7.2).** `B-12`/`S-2` scoped to one feature.
Without it the engine takes `IGame` and none of it can be tested without Big Box, which is
exactly how the voice scorer ended up untestable.

**P3 — Stable integer identity for a game in `GameCatalog`.** The catalog already orders
deterministically for this reason; the search index needs position-as-identity to keep postings
as `int[]`. Either expose an id→index map or carry the index on `GameMatch`.

### Before stage 4 — metadata filters

**P4 — Facet posting lists on `GameCatalog`.** `ByCategory` already returns
`ILookup<string, GameMatch>` per category — value → games is already built, at startup, for
browsing. Projecting it to `value → int[]` of sorted catalog indices is mechanical, and it is
the entire substrate of the filter sub-feature. Doing it here means the sub-feature adds
interaction, not data.

### Before stage 2 — the screen

**P6 — Put the search screen in its own view.** `MainWindowView.xaml` is already 1,497 lines.
`AttractModeView` is the precedent for a child `UserControl` with its own code-behind. Remember
`Eclipse.csproj` lists `<Page>` items explicitly.

### During the stages, not before them

**P5 — Resolve `B-35(a)`.** `GameList.ListCategoryType` is never assigned anywhere, so the
`list.ListCategoryType == rememberedListCategory` half of position restoration always passes.
Search adds a ninth list-category surface that will be installed and restored. Populate the
field or drop it from the match — but this only becomes load-bearing when a search result set
is first installed and restored, which is stage 2. Fix it there, with the behaviour in front of
you, rather than speculatively.

**P7 — Land the settings in one schema change** (§7.5), rather than adding one per stage.
The trigger is whichever stage first needs a setting — in practice stage 2, for
`EnableTextSearch` and `SearchKeyboardLayout`. Write the whole table then, even though the
later fields are unused, so there is one schema version bump rather than four.

**P8 — Not required: the composition root (`B-09`).** Search would add two more
`public static Instance` singletons under the current pattern, and
[composition-root.md](composition-root.md) counts thirteen already. Narrow interfaces
(`ISearchIndex`, following `IVoicePhraseIndex`) keep the feature testable without the full DI
work, so this is an argument for doing `B-09` sooner rather than a blocker on search.

---

## 10. Risks

| Risk | Mitigation |
|---|---|
| Typing on a d-pad is slow enough that users give up | Live results from character one; suggestions from character two; chips persist so refining never means retyping; session memory; Page Up/Down as Backspace/Commit |
| A large library makes the index build slow or heavy | Background build off the startup path; `int` postings; measure on a 100k library before optimizing |
| Fuzzy matching returns nonsense on short queries | Length-gated edit budget (§5.4); fuzzy never applied to the term still being typed |
| Ranking is tuned by feel and silently regresses | The whole table is one file of pure functions with tests; §5.5 records why each term exists |
| The user filters themselves into an empty set | Suggestions are computed against the current filtered set and zero-result terms are never offered — see the sub-feature document |
| The search screen becomes a second god view | `SearchSession` owns the state, `TextSearchState` owns the input, `SearchView` owns only what is on screen — the split `SelectedGameSequence` and `MainWindowView` already use |
| Voice search regresses while text search is built | Nothing in stages 1-4 touches the voice path; §11 is explicitly deferred and gated on P1 |

---

## 11. On converging with voice search — later, and only with tests

Text search's analysis chain and the voice grammar do overlapping work: both normalize titles,
both handle roman numerals, both split on `/` and `:`. It will be tempting to make them one
thing.

**Not in this plan.** `GameTitleGrammar`'s behaviour is documented as RULE-SEARCH-001…007,
relied on by every voice result, and covered by no test at all. `B-30` is already the backlog
item for reducing that fan-out, and [../TRACEABILITY.md](../TRACEABILITY.md) calls it
"**Premature** … it lacks the characterization coverage that would make it safe."

The right order is: P1 writes the tests; text search ships and its analyzer earns its keep on a
real library; *then* the two are converged with the tests as the contract. Doing it in the
other order changes voice results as a side effect of building text search, and nobody would
know which change caused it.

---

## 12. Verification

### 12.1 The golden corpus

The unit tests below each pin one component. They will not catch the failures that matter most,
because those are **interactions**: an analyzer change that makes the prefix range too wide, a
ranking constant that fixes `sonic` and breaks `zelda`, a fuzzy budget that quietly promotes
noise above exact matches. Nothing composed of individually-passing units notices any of that.

So the engine gets a second layer: a **checked-in corpus of representative games**, and a set
of query→expected-ordering assertions over it.

```
Eclipse.Tests/Search/
  SearchCorpus.cs          the fixture: games with full metadata, as object literals
  SearchCorpusTests.cs     query -> expected results, in order
```

The corpus is `SearchableGame` records plus their facet values — no `IGame`, no catalog, no
Big Box — which is exactly what prerequisite P2 buys.

**Size: 60-80 games, hand-curated.** Deliberately smaller than it could be. The value of a
golden corpus is that when an assertion fails, a human reads the diff and knows immediately
whether the ranking got better or worse. Several hundred generated records destroy that
property: nobody reads them, nobody can say whether a reordering is a regression, and the suite
becomes something people re-baseline rather than think about. Every entry should be there for a
reason someone can state.

What it must contain, because these are the cases that break things:

| Shape | Examples |
|---|---|
| A series with numbered entries and a base game | *Sonic the Hedgehog*, *Sonic the Hedgehog 2*, *Sonic the Hedgehog 3* |
| Titles sharing a prefix across different series | *Sonic Adventure*, *Sonic & Knuckles*, *Sonic Team* as a developer |
| A word that is a prefix of an unrelated word | *Sonic* vs *Son of a Beach* |
| Roman numerals and their digit forms | *Castlevania II*, *Final Fantasy VII*, *Final Fantasy X* |
| Colons and subtitles | *The Legend of Zelda: A Link to the Past* |
| Leading articles | *The Legend of Zelda*, *A Boy and His Blob* |
| Diacritics and symbols | *Pokémon*, *Sonic & Knuckles*, *Rock n' Roll Racing* |
| Slash-separated multi-titles | the shape RULE-SEARCH-001 exists for |
| Long editions of a short base game | *Sonic the Hedgehog 2* vs a hypothetical *…2 Special Edition Collection* |
| Multi-genre games | one carrying both *Sports* and *Football* |
| Single-valued facet variety | several platforms and release years, for §3 of the filter plan |
| A game with sparse metadata | no publisher, no series — the null paths |

Assertions are written as *the expected result list, in order*, not as "contains". Ordering is
the thing being designed, so ordering is the thing asserted.

| ID | Scenario | Kind | Covers |
|---|---|---|---|
| VER-SEARCH-019 | The golden corpus: `son`, `sonic`, `sonik`, `zelda`, `link past`, `ff7`, `final fantasy 7`, `pokemon`, `rock n roll` each produce the expected ranked list, in order | **Integration — unit-testable** | §5.2, §5.4, §5.5 as a whole |

### 12.2 Scenarios

| ID | Scenario | Kind | Covers |
|---|---|---|---|
| VER-SEARCH-010 | Analysis chain: `Pokémon`, `Rock n' Roll`, `Final Fantasy VII`, `Sonic & Knuckles`, `Legend of Zelda, The` each produce the expected token set, including numeral alternates | Unit | §5.2 |
| VER-SEARCH-011 | `son` returns Sonic titles ranked above titles where `son` is a later word; `Sonic the Hedgehog` outranks `Sonic the Hedgehog 2 Collection` | Unit | §5.5 |
| VER-SEARCH-012 | `sonik` finds *Sonic*; `son` does **not** fuzzy-match `sun`; a 3-character term allows zero edits | Unit | §5.4 |
| VER-SEARCH-013 | Two query terms narrow rather than widen; a query term matching nothing yields no results | Unit | §5.5 |
| VER-SEARCH-014 | Keyboard cursor: wraps where no adjacent zone exists, crosses to the adjacent zone where one does, never crosses while held | Unit | RULE-SEARCH-030/031/037 |
| VER-SEARCH-015 | Escape from every zone returns to browsing; re-entering restores query, chips and selection | Manual | RULE-SEARCH-034/035 |
| VER-SEARCH-016 | Commit installs a list set and the ordinary browse controls work on it | Manual | FEAT-SEARCH-016 |
| VER-SEARCH-017 | With text search disabled, the picker omits it and no index is built | Manual | §7.5 |
| VER-SEARCH-018 | Query latency stays under 16 ms per keystroke on a 100,000-game library | Manual, measured | §5.6 |

Stages 1 and 3 are almost entirely covered by unit tests and the golden corpus. That is the
strongest argument for building the engine before the screen: by the time anything is on
screen, the part that is hard to judge by eye has already been pinned by assertions someone can
read.

---

## 13. Open questions

| ID | Question |
|---|---|
| OQ-023 | Should the search screen be reachable as Eclipse's *startup* surface, for a cabinet whose owner wants search-first rather than browse-first? |
| OQ-024 | RULE-SEARCH-042 rebinds Page Up/Down inside the search screen. Should that be fixed, configurable, or should the screen instead respect the user's mapping and lose two useful buttons? |
| OQ-025 | Should a committed search survive a game launch and return, the way a browsing position does (`Navigator.RememberPosition`)? |
| OQ-026 | Should search look inside notes, alternate titles and file names as well as titles and metadata values? Cheap to index, and it changes what "no results" means. |
| OQ-027 | Should the keyboard offer an explicit "search this text as typed" action, for a user who wants the literal string rather than the ranked interpretation of it? |

Related existing questions: `OQ-005`, `OQ-006` (voice search).
