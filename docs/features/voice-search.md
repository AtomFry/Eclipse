# EPIC-SEARCH — Voice Search

**Scope.** Finding a game by speaking its name. Owns grammar construction, the
recognition session, phrase→game matching, relevance scoring and result ranking. The
results themselves are ordinary game lists, so browsing them is EPIC-BROWSE.

Eclipse has **no text search**. Voice is the only search modality.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-SEARCH-001 † — Grammar construction.** At startup, every game title in the
library is decomposed into searchable phrases and registered with the speech engine, so
that speaking any part of a title can be recognised. This happens only when voice search
is enabled.

**FEAT-SEARCH-002 — Voice search session.** The user triggers voice search (by default
Page Down, or from the category picker). Eclipse shows a listening indicator, stops video
and animation, and listens for a single utterance.

**FEAT-SEARCH-003 — Phrase matching.** Every phrase the recogniser hypothesised is
matched against the registered title phrases to produce candidate games.

**FEAT-SEARCH-004 — Relevance scoring.** Each candidate is scored from how the phrase
matched (whole title, main title, subtitle or a contained fragment), how much of the
title the phrase covered, and the recogniser's confidence.

**FEAT-SEARCH-005 — Result presentation.** Results become a list set — one list per
recognised phrase, ordered by relevance — and the user browses them like any category.

**FEAT-SEARCH-006 — Failure reporting.** If the recogniser heard nothing, timed out, or
errored, the user is shown a message rather than being silently returned.

### Title decomposition rules

| ID | Rule |
|---|---|
| RULE-SEARCH-001 | A title containing `/` is split into multiple independent titles, each decomposed separately. |
| RULE-SEARCH-002 | A title containing `:` splits into a main title and a subtitle; both are registered, and the stored title becomes the two joined by a space (the colon is dropped). |
| RULE-SEARCH-003 | The characters `:`, `"` and `!` are removed before word splitting. |
| RULE-SEARCH-004 | Roman numerals `II`–`XIX` are replaced with digits. **`X` is deliberately excluded** because too many titles contain a literal X. |
| RULE-SEARCH-005 | Every contiguous run of words from each starting position is registered as a phrase — so "super mario bros" registers "super", "super mario", "super mario bros", "mario", "mario bros", "bros". |
| RULE-SEARCH-006 | Phrases equal to a noise word are not registered. Noise words: `the, a, of, at, as, and, to, n', 'n, b, in, on`. |
| RULE-SEARCH-007 | Noise-word filtering is applied to the *accumulated* phrase, not to individual words, so "the legend" is registered while "the" alone is not. |

### Scoring and ranking rules

| ID | Rule |
|---|---|
| RULE-SEARCH-010 | Match types carry base scores: whole title 100, main title 95, subtitle 90, contained fragment 60. |
| RULE-SEARCH-011 | The remaining headroom to 100 is awarded in proportion to how much of the title the spoken phrase covered (phrase length ÷ title length). |
| RULE-SEARCH-012 | The result is multiplied by the recogniser's confidence, and 0.001 is subtracted so a score can never reach exactly 100%. |
| RULE-SEARCH-013 | If the same phrase is hypothesised more than once, only the highest confidence is kept. |
| RULE-SEARCH-014 | Within a phrase's list, games are ordered by descending match percentage. |
| RULE-SEARCH-015 | Lists are ordered by descending best-match percentage, then by descending longest matching title length. |
| RULE-SEARCH-016 | A phrase that matched no games produces no list. |
| RULE-SEARCH-017 | The score is displayed to the user as a two-digit "NN% Match". |

### Other rules

| ID | Rule |
|---|---|
| RULE-SEARCH-020 | Grammar construction is skipped entirely when voice search is disabled, and the *Voice search* option is removed from the category picker. |
| RULE-SEARCH-021 | Entering voice search stops attract mode and suppresses video and animation for the duration. |
| RULE-SEARCH-022 | Recognition is a single-utterance session with a 5-second initial silence timeout. |
| RULE-SEARCH-023 | If the recogniser could not be created at startup, triggering voice search currently returns to browsing with no message. **This is a defect** — see `M-5`/`M-6` and `OQ-005`. |

---

## Current implementation

| Concern | Location |
|---|---|
| Title decomposition | `Models/GameTitleGrammarBuilder.cs` — `GameTitleGrammar`, `IsNoiseWord`, `GetRomanNumeralReplacement` |
| Phrase registration | `Service/GameBagService.cs` — the `ListCategoryType.VoiceSearch` clones |
| Engine setup & session | `Service/SpeechRecognizer.cs` — `SpeechRecognizerService`, `SpeechRecognizer` |
| State & result assembly | `State/VoiceRecognitionState.cs` — `DoRecognize`, `RecognizeCompleted` |
| Scoring | `Models/GameMatch.cs` — `SetupVoiceMatchPercentage`, `SetMatchDescription` |
| Ranking | `State/VoiceRecognitionState.cs`; `Models/GameList.cs` — `MaxMatchPercentage`, `MaxTitleLength` |
| Listening indicator | `View/MainWindowView.xaml` (animated GIF, `IsRecognizing`) |
| Trigger | `State/KeyStrategy/KeyStrategyVoiceSearch.cs`; `State/EclipseStateContext.cs` — `DoVoiceSearch` |

Platform dependency: `System.Speech` (Windows Speech Recognition). Requires the
RID-specific Windows assembly — the platform-agnostic stub throws on every call.

## Technical debt

| Finding | Effect |
|---|---|
| M-5 | A failed recogniser construction is cached permanently; voice search stays dead until restart. |
| M-6 | That failure presents as a keypress doing nothing. |
| S-8 | Phrase registration is O(words²) per title and is the largest contributor to index size. |
| S-2 | Scoring operates on `IGame`, so it cannot be tested without the host. |
| S-14 | The recogniser catch logs and continues, leaving a null recogniser. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-03 | Fixes the permanent failure caching (`M-5`). |
| B-04 | Makes the silent failure visible (`M-6`). |
| B-30 | Would reduce phrase fan-out — must not change match results. |
| B-12 | Scoring moves onto the Eclipse-owned game model. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-SEARCH-001` … `VER-SEARCH-005`.

**Currently automated:** none.
**Highest-value gap:** title decomposition (`RULE-SEARCH-001`…`007`) and scoring
(`RULE-SEARCH-010`…`012`) are pure functions over strings — they are the most testable
logic in the entire product and are completely uncovered.

## Open questions

`OQ-005`, `OQ-006` — see [UNRESOLVED.md](../UNRESOLVED.md).
