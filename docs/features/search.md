# EPIC-SEARCH — Finding a Game

**Scope.** Finding a game other than by browsing to it. Two modalities: **typing** on an
on-screen keyboard, and **speaking**. Owns text analysis, index construction, matching, ranking,
the search screen and the recognition session. The results themselves are ordinary game lists,
so browsing them is EPIC-BROWSE.

The two modalities share nothing but their purpose. They have separate indexes, separate
matching, separate ranking and separate rules, and neither is a prerequisite for the other. That
is deliberate — see *Why the two are separate* below.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features — text search

**FEAT-SEARCH-010 — On-screen keyboard.** A key grid navigated with the four directions; Enter
types the highlighted key. Space, Backspace and Clear are keys in the grid, because the input
contract has nowhere else to put them. Two layouts: alphabetical (default) and QWERTY.

**FEAT-SEARCH-011 — Live title search.** The library filters as the user types. Results update
on every keystroke and are shown by the ordinary browsing surface.

**FEAT-SEARCH-013 — Metadata term suggestions.** What is typed is matched against metadata values
as well as titles. Matching values are offered beside the keyboard with their facet and the
number of games selecting one would leave, and a term that would leave none is never offered.

**FEAT-SEARCH-014 — Stacking filters.** Selecting a suggestion applies it as a filter and
consumes the query, so the next thing typed narrows what the last one left. Filters of different
facets combine with AND; two values of the same facet accept either.

**FEAT-SEARCH-015 — Removing filters.** The applied filters are a navigable row; Enter on one
removes it, widening the results without retyping anything. The `clear` key removes all of them
at once.

**FEAT-SEARCH-016 — Selecting a result.** Enter on a game closes the search panel and opens that
game, honouring *bypass details* exactly as browsing does.

**FEAT-SEARCH-017 — Session memory.** Leaving and re-entering search restores the query, the
results and the place in them, for the lifetime of the Big Box session.

**FEAT-SEARCH-018 — Feedback.** A status line reports the live result count, or says why there
is none — nothing typed, nothing found, or the index not ready yet.

**FEAT-SEARCH-019 † — Search index.** A token index over every title, built once in the
background after the catalog is ready.

*FEAT-SEARCH-012 (typo tolerance) is designed but not built — see* Deliberately not built *below.*

### Features — voice search

**FEAT-SEARCH-001 † — Grammar construction.** At startup, every game title in the library is
decomposed into searchable phrases and registered with the speech engine, so that speaking any
part of a title can be recognised. Only when voice search is enabled.

**FEAT-SEARCH-002 — Voice search session.** The user triggers voice search (by default Page
Down, or from the category picker). Eclipse shows a listening indicator, stops video and
animation, and listens for a single utterance.

**FEAT-SEARCH-003 — Phrase matching.** Every phrase the recogniser hypothesised is matched
against the registered title phrases to produce candidate games.

**FEAT-SEARCH-004 — Relevance scoring.** Each candidate is scored from how the phrase matched
(whole title, main title, subtitle or a contained fragment), how much of the title the phrase
covered, and the recogniser's confidence.

**FEAT-SEARCH-005 — Result presentation.** Results become a list set — one list per recognised
phrase, ordered by relevance — and the user browses them like any category.

**FEAT-SEARCH-006 — Failure reporting.** If the recogniser heard nothing, timed out, or errored,
the user is shown a message rather than being silently returned.

### Text search — analysis and matching

| ID | Rule |
|---|---|
| RULE-SEARCH-049 | The analysis chain — case fold, Unicode fold, `&`→`and`, `+`→`plus`, apostrophes dropped without splitting the word, other punctuation to whitespace, collapse, tokenize — is applied **identically** to indexed titles and to query text. |
| RULE-SEARCH-048 | Numeral folding is **additive and index-time only**: a title's `vii` is findable as `vii`, `7` and `seven`. Nothing is ever replaced, so no spelling is lost. One is excluded (`i` is the English word far more often than the numeral); `x` is included, unlike the voice path's RULE-SEARCH-004. |
| RULE-SEARCH-047 | Results rank by comparing signals **in order of authority**, never by summing them: match quality, then popularity, then brevity, then whether the query matched the title's first word, then catalog order. A weaker signal can never outvote a stronger one. |
| — | All query terms must match (AND). Terms narrow; they never widen. The term still being typed is matched as a prefix; completed terms are matched exactly. |

### Text search — the screen

| ID | Rule | Why it matters |
|---|---|---|
| RULE-SEARCH-030 | Left/Right move within the focused zone and wrap where no adjacent zone lies that way. | Deliberate divergence from RULE-BROWSE-008, confined to this screen. |
| RULE-SEARCH-031 | Up/Down move within the zone; at its top or bottom edge they move to the zone above or below. | |
| RULE-SEARCH-032 | A zone with no content is skipped, never focused and never shown. | An empty zone that can still be entered is a dead end. |
| RULE-SEARCH-033 | Enter acts on whatever is highlighted — a key types, a result opens the game. | One button, one meaning. |
| RULE-SEARCH-034 | Escape always leaves the search screen, from any zone, in one press. | A ladder that unwinds zone by zone leaves the user unable to tell how many presses gets them out. |
| RULE-SEARCH-035 | Leaving keeps the query, the results and the place in them for the session. | Makes RULE-SEARCH-034 safe: an accidental Escape costs nothing. |
| RULE-SEARCH-036 | Left never opens the options pane here, whatever `OpenSettingsPaneOnLeft` says. | Falling into the settings pane off the leftmost key would be indefensible. |
| RULE-SEARCH-037 | Held movement never crosses a zone boundary. | Lets the user travel the key grid without shooting out of it. |
| RULE-SEARCH-038 | The `clear` key is context-sensitive: it empties the query while there is one, and removes every applied filter once there is not. The key says which of the two it will do. | With eight inputs there is no room for a second key, and per-chip removal alone does not scale to an over-narrowed search. The changing label is what stops a context-sensitive destructive key being a surprise. |
| RULE-SEARCH-042 | Page Up is Backspace, whatever the user has mapped it to. Page Down does nothing. | On QWERTY, reaching the `delete` key and returning costs about eight presses against one. |
| RULE-SEARCH-080 | Enter on a result opens the game detail overlay **without ending the search**; Escape from that overlay returns to the search screen, with the query, filters, rows and place in them intact. Playing the game does end it. | Escape from the overlay abandons rather than commits — it is the clearest possible statement that this was not the game the user was looking for, which is the worst moment to have thrown their search away. Playing a game is the opposite: they asked for it and got it. |

### Text search — the search surface

| ID | Rule | Why it matters |
|---|---|---|
| RULE-SEARCH-043 | Results are installed **live**, on every keystroke, as a one-list `GameListSet` under `ListCategoryType.TextSearch`, and drawn by the ordinary browsing surface. | This is why the selection box, the previous-game affordance, artwork hydration, decode-ahead, background image and video preview all behave exactly as they do while browsing — they *are* the browsing surface. |
| RULE-SEARCH-044 | A query matching nothing — including the empty query the screen opens on — hides the browsing surface rather than showing a stale row. | `ShowCategory` refuses to switch to an empty set, and a stale row would claim to be results that no longer exist. |
| RULE-SEARCH-045 | Escape restores the browsing position held when search was opened. | The user gets their place back, not the search results. |
| RULE-SEARCH-046 | The video preview holds while the cursor is on the keyboard, and plays once it moves to the results. Background artwork follows the selection either way. | The selection changes on every keystroke; a video starting on each one is noise rather than preview. |
| RULE-SEARCH-069 | Moving the cursor into the results fades out the **whole** search panel — keys, suggestions, query line, chips and backing — and moving back to the keyboard fades it in again. | The panel occupies the same corner as the clear logo and the game details. While the user is choosing, the game has to be the thing on screen; a half-faded panel leaving a query line and a chip row floating over a dimmed logo reads as clutter covering the game rather than as a search. Half a swap is worse than either whole one. |
| RULE-SEARCH-070 | The results list is named for the search: the query and every applied filter, in the chip row's order. | It is the only thing still saying what the user searched for once RULE-SEARCH-069 has taken the chips away — and it is what position restoration matches the list by, and the name the list keeps after the user commits and browses on. |

### Text search — metadata filters

| ID | Rule | Why it matters |
|---|---|---|
| RULE-SEARCH-050 | Filters of different facets combine with AND. | |
| RULE-SEARCH-051 | Two filters of the **same facet** accept either — every facet, uniformly: `(Platform1 OR Platform2) AND (Genre1 OR Genre2) AND (Developer1 OR Developer2)`. | One rule, and one the user never has to look up. It replaced a split where platform and release year ORed and everything else ANDed — a claim about the LaunchBox schema rather than about libraries. Measured over 1,475 games: developer is multi-valued in 1.6% of them and publisher in 0.07%, so `Capcom AND Konami` was structurally empty, and because RULE-SEARCH-055 never offers a term that would find nothing, Konami was never even offered once Capcom was applied. |
| RULE-SEARCH-052 | A second value of a facet therefore always **widens**. It can never produce an empty set, and a value wholly contained in one already applied is not offered at all, because it would change nothing. | Makes a same-facet filter impossible to turn into a dead end. The cost, taken knowingly: intersecting within a facet is gone, so `Sports AND Football` — the example the filter feature was originally asked for — is no longer expressible. Genre is the one facet where that was meaningful (65% of games carry several) and it was given up for the single rule. |
| RULE-SEARCH-054 | Suggestions are computed against the games the applied filters leave, not against the whole library. | |
| RULE-SEARCH-055 | A term that would leave no games is never offered. | Together with 054 this makes it impossible to reach an empty result set by picking suggestions — the screen never explains a dead end because it never creates one. |
| RULE-SEARCH-056 | Each suggestion shows the count it *would* produce. Counts ignore the typed text, because selecting a suggestion clears it. | On a controller there is no hover and no way to inspect anything; the count is the entire preview mechanism. |
| RULE-SEARCH-057 | Suggestions rank by match quality, then by resulting count. | Ordered, not weighted — as with title ranking, a big count must not lift a poor match above a good one. |
| RULE-SEARCH-058 | At most a few suggestions, and at most a few from any one facet — but leftover slots are then filled regardless of facet. | The per-facet cap stops two hundred developers burying the one genre. Filling the remainder stops it wasting slots when there is only one facet to show. Diversity is a floor, not a ceiling. |
| RULE-SEARCH-059 | Suggestions are shown from the first typed character. A display threshold only; **on trial at one** — it shipped at two, and two was a guess the threshold itself made unverifiable. | |
| RULE-SEARCH-072 | A second value of a facet that already carries a filter is offered as a suggestion, and its count is the **union** — larger than the set on screen, where every other suggestion's is smaller. A widening term that would bring in no new games is not offered. | Without this `RULE-SEARCH-051` is unreachable: the count was an intersection with the survivors, which for disjoint posting lists is always zero, so `RULE-SEARCH-055` dropped the term. The union must be counted rather than added — outside platform and year the two sets overlap, and adding would promise more than applying delivers. The algebra was correct, fully tested, and no user could produce it. The rising count is the signal that this filter goes the other way, and the `or` on the chip is what explains it. |
| RULE-SEARCH-060 | Selecting a suggestion applies it **and clears the query**. | The query buffer is scratch space used to discover a term; selecting one consumes it. The free text that survives is by definition the text the user chose *not* to turn into a filter. |
| RULE-SEARCH-061 | Selecting a term that is already applied removes it. | Duplicates are unreachable rather than deduplicated later. |
| RULE-SEARCH-062 | After selecting, the cursor returns to the keyboard. | The list it was standing in has just been rebuilt away. |
| RULE-SEARCH-063 | Filters display grouped by facet — in insertion order within a facet, and facet groups in the order their first filter arrived. | Not a sort. Same-facet filters have to be adjacent for RULE-SEARCH-067 to be true where it stands. |
| RULE-SEARCH-064 | Enter on a filter removes it. | |
| RULE-SEARCH-065 | Removing a filter re-runs the query; nothing else is retyped or reset. | Filters are removable in any order because nothing is stored incrementally. |
| RULE-SEARCH-067 | The row renders `or` between two filters of the same facet, and nothing between filters of different facets. | The algebra is only defensible if it is legible, but only half of it needs saying: adjacency already reads as *both*, so an `and` between every pair restates what the layout means and costs space in the tightest row on the panel. `or` is the half that surprises. |
| RULE-SEARCH-068 | Every filter can be removed at once, via the context-sensitive `clear` key (RULE-SEARCH-038). | Per-filter removal does not scale to a user four filters deep who wants to start again. |
| RULE-SEARCH-071 | The chip row wraps to a second line rather than scrolling or clipping. Every applied filter stays on screen. | The row is the answer to "why am I looking at two games", so hiding filters behind a window would remove the one thing it is for — and a window opening mid-row could drop the `or` that explains why two platforms mean either rather than both (RULE-SEARCH-067). |

### Voice search — title decomposition

| ID | Rule |
|---|---|
| RULE-SEARCH-001 | A title containing `/` is split into multiple independent titles, each decomposed separately. |
| RULE-SEARCH-002 | A title containing `:` splits into a main title and a subtitle; both are registered, and the stored title becomes the two joined by a space (the colon is dropped). |
| RULE-SEARCH-003 | The characters `:`, `"` and `!` are removed before word splitting. |
| RULE-SEARCH-004 | Roman numerals `II`–`XIX` are replaced with digits. **`X` is deliberately excluded** because too many titles contain a literal X. |
| RULE-SEARCH-005 | Every contiguous run of words from each starting position is registered as a phrase. |
| RULE-SEARCH-006 | Phrases equal to a noise word are not registered. Noise words: `the, a, of, at, as, and, to, n', 'n, b, in, on`. |
| RULE-SEARCH-007 | Noise-word filtering is applied to the *accumulated* phrase, not to individual words, so "the legend" is registered while "the" alone is not. |

### Voice search — scoring and ranking

| ID | Rule |
|---|---|
| RULE-SEARCH-010 | Match types carry base scores: whole title 100, main title 95, subtitle 90, contained fragment 60. |
| RULE-SEARCH-011 | The remaining headroom to 100 is awarded in proportion to how much of the title the spoken phrase covered. |
| RULE-SEARCH-012 | The result is multiplied by the recogniser's confidence, and 0.001 is subtracted so a score can never reach exactly 100%. |
| RULE-SEARCH-013 | If the same phrase is hypothesised more than once, only the highest confidence is kept. |
| RULE-SEARCH-014 | Within a phrase's list, games are ordered by descending match percentage. |
| RULE-SEARCH-015 | Lists are ordered by descending best-match percentage, then by descending longest matching title length. |
| RULE-SEARCH-016 | A phrase that matched no games produces no list. |
| RULE-SEARCH-017 | The score is displayed to the user as a two-digit "NN% Match". |

### Voice search — other

| ID | Rule |
|---|---|
| RULE-SEARCH-020 | Grammar construction is skipped entirely when voice search is disabled, and the *Voice search* option is removed from the category picker. |
| RULE-SEARCH-021 | Entering voice search stops attract mode and suppresses video and animation for the duration. |
| RULE-SEARCH-022 | Recognition is a single-utterance session with a 5-second initial silence timeout. |
| RULE-SEARCH-023 | A recogniser that could not be created reports why rather than failing silently. |

---

## Why the two are separate

They look like they should share a text pipeline. They must not, yet.

`System.Speech` needs an **enumerated grammar** — every recognisable utterance handed over up
front — which is why RULE-SEARCH-005 expands each title into every contiguous run of its words.
Text search has no such constraint: the query arrives one keystroke at a time and is intersected
against a word index at query time, so a seven-word title costs seven postings instead of about
thirty phrases.

Converging them would change voice results as a side effect of text-search work. The
prerequisite for ever doing it safely is now in place — `VoiceSearchCharacterizationTests` pins
RULE-SEARCH-002…007 and 010…017 — but the convergence itself is deliberately not scheduled. See
`B-30`.

---

## Current implementation

*This section describes where the behaviour lives today and is expected to change.*

### Text search

| Concern | Location |
|---|---|
| Analysis chain | `Service/Search/TextAnalyzer.cs` |
| The game projection | `Service/Search/SearchableGame.cs`; `SearchableGameProjection.cs` is the only file below the boundary that knows LaunchBox exists |
| Index | `Service/Search/TitleIndex.cs`, behind `ISearchIndex.cs` |
| Index lifecycle | `Service/Search/SearchIndexService.cs` — background build from `LoadingState`, availability behind `ISearchIndexSource.cs` |
| Query parsing | `Service/Search/SearchQuery.cs` |
| Ranking | `Service/Search/SearchScoring.cs` — `SearchRank` and its comparison |
| Query evaluation | `Service/Search/SearchEngine.cs` |
| Interaction state | `Service/Search/SearchSession.cs`; `Models/SearchKeyboard.cs` |
| Input routing and result installation | `State/TextSearchState.cs`; entry via `State/KeyStrategy/KeyStrategyTextSearch.cs` and `EclipseStateContext.DoTextSearch` |
| The screen | `View/SearchView.xaml`, `View/SearchViewModel.cs` |
| Settings | `Models/EclipseSettings.cs` — `EnableTextSearch`, `SearchKeyboardLayout` |

No LaunchBox SDK dependency below `SearchableGameProjection`. That is what lets the whole engine
be tested — `Eclipse.Tests` has no reference to the plugin contract assembly at all.

### Voice search

| Concern | Location |
|---|---|
| Title decomposition | `Models/GameTitleGrammarBuilder.cs` |
| Phrase registration | `Service/VoiceSearchIndex.cs` |
| Engine setup & session | `Service/SpeechRecognizer.cs` |
| State & result assembly | `State/VoiceRecognitionState.cs` |
| Scoring | `Models/GameMatch.cs` — `SetupVoiceMatchPercentage` |
| Ranking | `Service/VoiceSearchResultBuilder.cs` |
| Trigger | `State/KeyStrategy/KeyStrategyVoiceSearch.cs`; `EclipseStateContext.DoVoiceSearch` |

Platform dependency: `System.Speech`. Requires the RID-specific Windows assembly — the
platform-agnostic stub throws on every call.

---

## Deliberately not built

| Item | Status |
|---|---|
| FEAT-SEARCH-012 — typo tolerance | Designed (stage 3 of the plan: trigram candidates, bounded Damerau-Levenshtein, one edit). **On hold.** On a d-pad the user looks at each key before pressing it, so the typo rate this was specified against may not exist. The `SearchFuzzyMatching` setting ships and is read by nothing. Evidence to gather: how often "No games found" appears for a game the user owns. |
| Faceted result rows | Stage 5. A search produces one ranked list. |
| Search history | Not designed. Raised because every character costs several button presses and nothing exploits repeat searches. |

---

## Technical debt

| Finding | Effect |
|---|---|
| S-8 | Voice phrase registration is O(words²) per title and is the largest contributor to index size. Text search does not share it. |
| S-2 | Voice scoring operates on `IGame`, so it cannot be tested without the host — mitigated for the parts reachable from a string (see `VoiceSearchCharacterizationTests`), unresolved for the rest. |
| ~~B-35(a)~~ | **Resolved.** The field was redundant with the remembered *set* category rather than under-used, so it is gone and the match says what it always did. VER-BROWSE-005 covers the behaviour it was silently part of. |
| — | A single-character query over a library where nearly every title starts with that letter costs ~30 ms, past the 16 ms frame budget. Measured, not optimised; unreachable on a realistic library. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-12 | Partially delivered for search: `SearchableGame` is an Eclipse-owned read model with no LaunchBox types. Voice scoring still operates on `IGame`. |
| B-30 | Would converge the two analysis paths. Its prerequisite characterization tests now exist; the work itself is not scheduled. |
| B-04 | Delivered for both — availability is reported rather than failing silently. |

## Verification

See [../VERIFICATION.md](../VERIFICATION.md): `VER-SEARCH-001` … `VER-SEARCH-019`.

**Currently automated:** the whole text search engine and interaction — analysis, index, query,
ranking, keyboard cursor, session transitions — plus the voice decomposition and scoring
characterization. Roughly 260 tests.

**Remaining gaps:** RULE-SEARCH-001 (slash split) and RULE-SEARCH-005 (contiguous runs) are
unreachable from `Eclipse.Tests`, which has no LaunchBox reference; the search screen's
appearance is manual, per
[../plans/text-search-stage2-manual-tests.md](../plans/text-search-stage2-manual-tests.md).

## Open questions

`OQ-005`, `OQ-006` (voice); `OQ-023` … `OQ-027` (text) — see [../UNRESOLVED.md](../UNRESOLVED.md).
