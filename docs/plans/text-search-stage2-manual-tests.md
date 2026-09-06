# Text search — manual validation checklist

Everything a person has to look at, in the order it is quickest to work through. Named for
stage 2, where it started; it has since grown to cover stages 4 and 5 as those shipped. 658
automated tests already cover the engine, the filter algebra, the keyboard cursor, the row
enumeration and the session state machine; **nothing below repeats those**. What is here is
what only eyes and a controller can answer: does it look right, does it feel right, and does it
still leave the rest of Eclipse alone.

Report failures by number (`T4.3 failed: …`).

## How to read the risk marks

| Mark | Meaning |
|---|---|
| 🔴 | **Highest risk.** Untested by anything automated, and I have low confidence. Do these first. |
| 🟡 | Worth real attention — integration points, or things I fixed late. |
| ⚪ | Confirmation. Expected to pass; cheap to check. |

**If you only have twenty minutes**, do §0, §7, §8b, §8c and §10.

---

## §0 — Deploy

| | Step | Expected |
|---|---|---|
| T0.1 | Close **both** LaunchBox and Big Box completely. | A running host holds `Eclipse.dll` open and the copy fails partway, leaving a mixed set of files. |
| T0.2 | `dotnet build Eclipse.sln -c Release` | `Build succeeded. 0 Warning(s) 0 Error(s)` and `Eclipse deployed to D:\LaunchBox`. |
| T0.3 | ⚪ Check `D:\LaunchBox\Plugins\Eclipse\` does **not** contain `Unbroken.LaunchBox.Plugins.dll` or `manifest.json`. | Absent. Both have previously caused total feature loss (`RULE-INTEGRATE-011`/`012`). Stage 2 added no dependencies, so this is a cheap confirmation rather than a real worry. |
| T0.4 | Start Big Box with the Eclipse theme. | Loads as before. No new startup delay. |

---

## §1 — Settings (P7)

LaunchBox desktop → **Tools → Manage Eclipse → Inputs**.

| | Step | Expected |
|---|---|---|
| T1.1 | ⚪ Look at the Inputs tab. | A new checkbox **"Enable search"**, ticked, and a new combo **"Keyboard layout"** showing `Alphabetical`. Both sit above the existing "Enable voice search". |
| T1.2 | ⚪ Open the **Page up** and **Page down** combos. | Each now lists `TextSearch` as the last entry. |
| T1.3 | 🟡 Set **Page down** to `TextSearch`, save, close. | Saves without error. |
| T1.4 | 🔴 Open `…\LaunchBox\Plugins\Eclipse\EclipseSettings.json` in a text editor. | `EnableTextSearch`, `SearchKeyboardLayout`, `SearchFuzzyMatching`, `SearchMaxSuggestions`, `SearchCommitRowCount` are present. **`PageUpFunction`, `PageDownFunction` and `DefaultListCategoryType` still hold the values they had before the upgrade** (except Page down, which you just changed). |

> **T1.4 is the single highest-risk item in this list.** `ListCategoryType` and `PageFunction`
> are persisted as integers. I appended the new values rather than inserting them, which should
> mean every existing saved value keeps its meaning — but this is the check that proves it. If
> your page keys or default category have silently changed to something else, stop and tell me.

---

## §2 — Entry points

| | Step | Expected |
|---|---|---|
| T2.1 | 🟡 In Big Box/Eclipse, press **Escape** (or Left at the first game) to open the category pane. | The list now contains **"Search"**, positioned after "Random" and before "Voice search". |
| T2.2 | ⚪ Highlight "Search", press **Enter**. | The search screen opens. |
| T2.3 | ⚪ Escape back to browsing. Press **Page down** (mapped in T1.3). | The search screen opens. |
| T2.4 | 🟡 In settings, untick **Enable search**, save, restart Big Box, open the category pane. | "Search" is **absent** from the list, and the page key does nothing. Re-tick it before continuing. |

---

## §3 — Screen layout 🔴

This is the section I have least confidence in — none of it is covered by a test, and I could
not see it while building it.

| | Step | Expected |
|---|---|---|
| T3.1 | 🔴 Open search with nothing typed. | A large query line top left with the grey placeholder **"Search for a game"**, the status line **"Type to search"** under it, and the keyboard below that — all in the upper-left area where the clear logo normally sits. The rest of the screen is dark, because there are no results yet. Nothing overlapping, clipped or off-screen. |
| T3.2 | 🔴 Look at the keyboard block. | Fills its area sensibly — evenly spaced, neither tiny in a corner nor overflowing into the game row below. |
| T3.3 | 🔴 Look at the selected key (top-left, `A`). | A filled near-white tile with **black** text. Unmistakable at normal viewing distance. |
| T3.4 | ⚪ Look at the unselected keys. | Faint tiles, light grey text, legible but clearly secondary. |
| T3.5 | 🔴 Now type a few characters so results appear. | The panel sits over a **soft dark backing that fades out to the right**, so the keyboard stays readable while the game's background artwork shows through on the right of the screen. No hard edge across the middle. |
| T3.6 | 🟡 Confirm the clear logo and game details are hidden while searching. | The panel occupies that space instead. They come back the moment you leave search. |
| T3.7 | 🔴 Read everything from your normal seating distance. | All text legible. Tell me anything too small — the sizes are guesses. |

---

## §4 — Keyboard, alphabetical layout

| | Step | Expected |
|---|---|---|
| T4.1 | ⚪ Read the key grid top to bottom. | `ABCDEF` / `GHIJKL` / `MNOPQR` / `STUVWX` / `YZ0123` / `456789`, then a row of three: **space**, **delete**, **clear**. |
| T4.2 | ⚪ Confirm the function row keys are wider than the letter keys. | They are — three keys sharing the width six normally share. |
| T4.3 | ⚪ Press **Right** repeatedly along the top row past `F`. | Wraps to `A`. |
| T4.4 | ⚪ Press **Left** from `A`. | Wraps to `F`. |
| T4.5 | ⚪ Press **Down** repeatedly from `C`. | `C → I → O → U → 1 → 7 → delete`. |
| T4.6 | ⚪ From `delete`, press **Down** once more (with no results on screen). | Wraps to the top row, same column — `C`. |

---

## §5 — Keyboard, QWERTY layout

Settings → Keyboard layout → `Qwerty`. Save, **restart Big Box** (the layout is read once at
startup).

| | Step | Expected |
|---|---|---|
| T5.1 | ⚪ Read the grid. | `1234567890` / `QWERTYUIOP` / `ASDFGHJKL` / `ZXCVBNM`, then space / delete / clear. |
| T5.2 | 🟡 Note the row widths. | Rows have different key counts, and each row fills the full width — so `ZXCVBNM` keys are noticeably wider than the number row's. This is intended. |
| T5.3 | 🟡 Go to `0` (top right), then press **Down** three times. | `0 → P → L → M`. It clamps into each shorter row rather than falling off the end. |
| T5.4 | 🟡 From `M`, press **Up** three times. | Returns to `0`. Passing through short rows does not drag the cursor sideways permanently. |

Set the layout back to `Alphabetical` for the rest of the list (or don't — everything else
works the same).

---

## §6 — Typing and live results

Use games from **your** library; the examples are placeholders.

| | Step | Expected |
|---|---|---|
| T6.1 | ⚪ Type one character. | The query line shows it, the placeholder disappears, results appear, and the status shows a count like `1,284 games`. |
| T6.2 | 🔴 Type three or four more characters of a game you own. | Results narrow visibly with each press. Box art appears in the row. Count falls. |
| T6.3 | 🔴 Watch the box art while typing quickly. | Behaves exactly like the main browsing row — placeholder box art first, then the real artwork fills in. **No black boxes and no stalling**, even typing fast. |
| T6.4 | 🟡 Type something that matches nothing (`zzzz`). | Status reads **"No games found"**, and the whole browsing surface hides — no stale row, no stale artwork. Just the keyboard and query on a dark screen. |
| T6.5 | ⚪ Navigate to **delete** and press Enter. | Last character removed, results widen again. |
| T6.6 | ⚪ Navigate to **clear** and press Enter. | Query empties, placeholder returns, status reads **"Type to search"**, results empty. |
| T6.7 | ⚪ Navigate to **space** and press Enter on an empty query. | Nothing happens — a leading space is deliberately ignored. |
| T6.8 | 🟡 Type a two-word query with a space (e.g. `super mario`). | Narrows to games with **both** words, in any order. |

---

## §7 — Zones, focus and navigation 🔴 *(rewritten after the overlay rework)*

The results are now an **ordinary Eclipse game list**, installed the way voice search installs
its results and drawn by the browsing surface underneath the keyboard. So everything the main
row does, this row now does — because it *is* that row.

| | Step | Expected |
|---|---|---|
| T7.1 | 🔴 Type enough to get results. | The normal Eclipse box art row appears at the bottom, with the **white selection box** around the selected game, and the **previous game** half off-screen to the left. There is no "Results" label any more. |
| T7.2 | 🔴 Compare the row to the main browsing row. | Identical — same box sizes, same margins, same selection chrome, same scroll feel. |
| T7.3 | 🔴 Look at the artwork area on the right and the list heading. | The selected game's **background artwork** is showing, and the list heading reads `Search: sonic` — the query and any filters, so the row says what it is once the panel fades. |
| T7.4 | 🔴 Press **Up** from the top keyboard row. | Focus moves to the row. The selected key stops being a filled white tile and becomes an **outline** — it keeps your place without looking active. |
| T7.5 | 🔴 Press **Right** / **Left** in the results row. | Scrolls exactly like the main row: one game per press, selection box stays put, artwork and background follow. |
| T7.6 | 🔴 Hold **Right** through the row. | Box art keeps up — placeholder then real art, no black boxes, no stalling. This was the worst symptom before. |
| T7.7 | 🟡 Move into the results, then press **Down**. | Returns to the keyboard, and the selected key becomes a filled tile again. |
| T7.8 | 🔴 **Hold** Down for a few seconds on the keyboard, then **Hold** Up. | Travels the keyboard rows and wraps within the keyboard — never shoots into the results. |
| T7.9 | 🟡 Clear the query while standing in the results row. | The row disappears, the browsing surface hides, and focus is back on the keyboard. You should see just the keyboard and query on a dark screen. |
| T7.10 | 🟡 Type a query matching nothing. | Same as T7.9 — the row and artwork are hidden rather than showing stale results. Status reads "No games found". |
| T7.11 | 🟡 Press **Left** on the leftmost key of any row. | Wraps to the right of that row. Never opens the Eclipse options pane. |
| T7.12 | 🔴 With the keyboard focused, watch the artwork area while typing. | Background artwork follows the selection, but **no video starts**. Videos are gated to the results zone. |
| T7.13 | 🔴 Move down into the results and wait a moment. | The video preview starts, as it does while browsing. |

## §7b — Escape restores your place 🔴 *(new)*

| | Step | Expected |
|---|---|---|
| T7b.1 | 🔴 Note which category, list and game you are on. Open search, type something, get results. | Results replace the row. |
| T7b.2 | 🔴 Press **Escape**. | You are back on **exactly the game, list and category you started from** — not on the search results, and not at the top of some list. |
| T7b.3 | 🟡 Re-open search. | Query and results are still there (RULE-SEARCH-035), and the cursor is on the keyboard. |
| T7b.4 | 🟡 In the results row, move to the third game, press **Escape**, then re-open search. | The results row comes back with the **third game** still selected. |

---

## §8 — Page Up / Page Down inside search 🟡

| | Step | Expected |
|---|---|---|
| T8.1 | 🟡 Type a few characters. Press **Page Up**. | Acts as backspace — one character removed. |
| T8.2 | 🟡 Press **Page Up** repeatedly until the query is empty, then once more. | Nothing breaks; query stays empty. |
| T8.3 | 🟡 Type a query with results. Press **Page Down**. | **Nothing happens.** It used to commit the search; under the overlay design the results are already the live list, so there is nothing to commit. The press is swallowed rather than passed to Big Box. |
| T8.4 | 🟡 With a page key mapped to `TextSearch`, open search with it, then press it again. | Nothing. Escape is the way out. Tell me if a toggle would feel better — it is a one-liner. |

## §8b — The duplicate row is gone 🔴 *(new)*

| | Step | Expected |
|---|---|---|
| T8b.1 | 🔴 Search for something with results. | **One** row of box art, not two identical ones stacked. |
| T8b.2 | 🔴 Leave search and browse a normal category. | The next-list teaser is still there below the current row, showing a *different* list, exactly as before. |
| T8b.3 | 🟡 Run a voice search that matches a single phrase. | Also shows one row rather than two — the fix is general, not search-specific. |

## §8c — Ranking 🔴 *(new)*

| | Step | Expected |
|---|---|---|
| T8c.1 | 🔴 Type `zel` or `zelda`. | **The Legend of Zelda is first**, not Zelda II. This is the case the rework was for — compare against your screenshot. |
| T8c.2 | 🔴 Type a few franchise names you own — `mario`, `sonic`, `final fantasy`, `castlevania`. | The game you'd expect is at or near the top. Ratings drive this, so a franchise where you have not rated anything falls back to shortest title first. |
| T8c.3 | 🟡 Find a game with a "Special Edition" or "Collection" variant. | The plain game outranks the long variant. |
| T8c.4 | 🟡 Watch the order as you type the last letter of a word. | It should not lurch — completing a word does not change the ranking. |

---

## §9 — Escape and session persistence 🟡

| | Step | Expected |
|---|---|---|
| T9.1 | 🟡 Open search, type `sonic` (or similar), move into the results row and select the third result. Press **Escape**. | Returns to normal browsing, exactly where you were before opening search. Lists unchanged. |
| T9.2 | 🔴 Re-open search. | **The query is still there, the results are still there, and the cursor is still on the third result in the results row.** This is RULE-SEARCH-035 and it is what makes Escape safe. |
| T9.3 | 🟡 Press **Escape** from the keyboard zone rather than the results zone. | Also leaves immediately. Escape always exits, from any zone, in one press. |
| T9.4 | ⚪ Open the category pane, pick a different category, then open search again. | Query and results still preserved. |

---

## §10 — Committing and selecting a game 🔴

| | Step | Expected |
|---|---|---|
| T10.1 | 🔴 Search for a game, move into the results row, highlight a game that is **not** the first, press **Enter**. | The search panel goes away and the **game detail overlay opens on the game you highlighted** — with its artwork and background, not the previous game's. |
| T10.2 | 🔴 Look at the screen carefully at that moment. | Nothing is blank or missing. The box art row, list heading and details are all visible behind the overlay. *(This is a bug I found and fixed late — the overlay lives inside the results grid, so a mistake here shows as a blank screen.)* |
| T10.3 | 🔴 Press **Escape** from the overlay. | **You are back in search**, on the same game in the same row, with the query, filters and keyboard intact and the panel faded out. Escape abandons the game rather than committing to it, so it must not cost you the search (RULE-SEARCH-080). |
| T10.3a | 🔴 Press **Up** once. | The search panel fades back in and the cursor is on the keyboard - one press, not several. It must not land on `clear filters`. |
| T10.3b | 🔴 Press **Escape** again. | Now you leave search, and land back at the library position you opened search from - not in the search results. |
| T10.3c | 🟡 Repeat T10.1, then step out to **more info** and back to the overlay, then Escape. | Still returns to search. The detour must not reset where Escape goes. |
| T10.4 | 🟡 Navigate Left/Right in the results row. | Behaves exactly like any other Eclipse list. |
| T10.5 | 🟡 Press **Up/Down**. | With fewer than two constraints applied there is one row, so it wraps back to itself. With two or more, Up/Down walk the faceted rows — see the stage 5 checks below. |
| T10.6 | 🟡 Play a game from the search results. Exit the game. | Returns normally; History/Favorites lists update as usual. |
| T10.7 | 🟡 Turn on **"bypass game details"** in settings, restart, repeat T10.1. | The game **launches directly** instead of opening the overlay — matching what Enter does while browsing. |
| T10.8 | ⚪ Open search with **no filters applied**, type something matching nothing. | You stay in search with your query intact — the browsing surface is hidden rather than showing a stale row. |
| T10.9 | 🔴 Now apply two filters and type something that matches nothing within them. | The surface is **not** hidden: the near-miss rows are shown, each named for the constraints it keeps, and the status line reads `No games match everything` (RULE-SEARCH-044). |

---

## §11 — Matching behaviour end to end ⚪

Substitute equivalents from your own library.

| | Step | Expected |
|---|---|---|
| T11.1 | Type `final fantasy 7` (with the space). | Finds Final Fantasy **VII**. The digit form works even though the title uses roman numerals. |
| T11.2 | Type `final fantasy vii` **without** a trailing space. | Finds **both VII and VIII** — `vii` is still being typed and is a legitimate prefix of `viii`. Type a space and it narrows to VII. This is intended; see the automated test of the same name. |
| T11.3 | Type `castlevania 3` and `castlevania iii`. | Both find the same game. |
| T11.4 | Type `pokemon` (no accent). | Finds Pokémon titles. |
| T11.5 | Type `rock n roll`. | Finds *Rock n' Roll Racing*, if you have it — the apostrophe does not break the word. |
| T11.6 | Type `sonic and knuckles`. | Finds *Sonic & Knuckles*. The keyboard has no `&` key, so this is the only route to it. |
| T11.7 | Type a word from the **middle** of a long title. | Finds it. Word order does not matter. |
| T11.8 | Type `10`. | Returns titles containing X (e.g. *Mega Man X*, *Final Fantasy X*). **This is a known, documented consequence** of folding numerals additively — not a bug. |

---

## §12 — Known gaps (confirm they are still gaps) ⚪

These must **fail to find anything**. They are what stage 3 turns on.

| | Step | Expected |
|---|---|---|
| T12.1 | Type `sonik`. | **No results.** Fuzzy matching is stage 3. |
| T12.2 | Type `castlevaina` (transposed). | **No results.** Same reason. |
| T12.3 | Type `ff7`. | **No results.** Acronyms are not in the plan at all. |

If any of these *do* return results, something unintended is happening — tell me.

---

## §13 — Index readiness

| | Step | Expected |
|---|---|---|
| T13.1 | 🟡 Untick "Enable search", restart, and reach search via a page key mapped to `TextSearch`. | The screen does not open at all (the entry point is gated). Re-tick afterwards. |
| T13.2 | ⚪ On a very large library, start Big Box and open search **as fast as you possibly can**. | Either results work immediately, or the status briefly reads **"Getting search ready..."** and then works. It must never appear broken or empty-forever. |

> Honest note: the index builds in ~58 ms per 100,000 games, so **T13.2 is very unlikely to be
> reachable** on a real library — you will almost certainly always see it ready. That is the
> correct outcome. The `Preparing` and `Failed` states are covered by automated tests instead.

---

## §14 — Attract mode interaction 🟡

I deliberately changed this: unlike voice search, the search screen can sit open indefinitely,
so the screensaver timer keeps running rather than being switched off.

| | Step | Expected |
|---|---|---|
| T14.1 | 🟡 Open search, type a few characters, then **leave the controller alone** for longer than your screensaver delay. | Attract mode starts normally. |
| T14.2 | 🔴 Press any button. | You are returned **to the search screen**, with your query, results and cursor position intact. |
| T14.3 | ⚪ Confirm typing resets the idle timer. | Attract mode does not cut in while you are actively pressing keys. |

---

## §15 — Regression: voice search and browsing 🔴

Stage 2 must not have touched any of this. `git diff` says the voice path is byte-identical
apart from one sort-order number, but that is worth confirming with your hands.

| | Step | Expected |
|---|---|---|
| T15.1 | 🔴 Run a **voice search** exactly as before. | Works identically. Results grouped by phrase, ranked as before, match percentages unchanged. |
| T15.2 | 🟡 Trigger a voice search that matches nothing. | Same "no games matched" notice in the list heading as before. |
| T15.3 | ⚪ Browse by each category — platform, genre, series, playlist, play mode, developer, publisher, year. | All present and correct. |
| T15.4 | ⚪ Confirm your **custom lists** (Favorites, History, any of your own) still appear and contain the right games. | Unchanged. `CustomLists.json` is read as before. |
| T15.5 | ⚪ Favourite a game, change a rating, launch a game. | Lists rebuild and your position is restored, as before. |
| T15.6 | ⚪ Random game, More like this, flip box, zoom box. | All unchanged. |
| T15.7 | ⚪ Featured game view (Up from the first list). | Unchanged. |

---

## §16 — Regression: data file compatibility 🔴

The highest-consequence risk in the whole stage, and cheap to check.

| | Step | Expected |
|---|---|---|
| T16.1 | 🔴 Open the settings window and read every tab. | Every setting holds the value you had before the upgrade. Nothing has silently reset or shifted. |
| T16.2 | 🔴 Confirm **Page up** and **Page down** show the functions you actually had mapped. | Unchanged (except what you set in T1.3). |
| T16.3 | 🔴 Confirm **Default group** on the Lists tab is still your category. | Unchanged. |
| T16.4 | 🟡 Open a custom list for editing in the settings window. | Its filters, sorts and category checkboxes are intact. |

---

## §17 — Feel and performance ⚪

Automated measurement says 0.63 ms average per keystroke on a synthetic 100,000-game library.
This is about whether it *feels* right on yours.

| | Step | Expected |
|---|---|---|
| T17.1 | Type a full game name at speed. | No lag between button press and the query line updating. No stutter. |
| T17.2 | Hold a direction across the keyboard. | Smooth cursor travel. |
| T17.3 | Type a single common letter (`s`, `a`, `t`). | Results appear promptly even though tens of thousands of games may match. |
| T17.4 | Repeatedly type and delete a word. | Stays responsive; no gradual slowdown. |

---

## What I expect to need adjustment

Being upfront, so you know where to look hardest. The first round of feedback removed three of
the four items that used to be here — the bespoke results row is gone, so its selection marker,
its box art sizing and its selected-game panel went with it.

1. **Keyboard proportions and font sizes (T3.2, T3.7)** — still chosen blind. The panel now has
   less vertical room than it did (rows 3–7 rather than a whole screen), so the keys may be
   squatter than ideal.
2. **The fade on the panel backing (T3.5)** — where it starts and how dark it is were guesses.
   It has to keep the keyboard readable without hiding the artwork.
3. **Whether hiding the clear logo is the right call (T3.6)** — the alternative is shrinking the
   keyboard to sit beside it.
4. **Video gating (T7.12, T7.13)** — you chose "only when focus is in the results row". Worth
   confirming that moving into the row and back out does not leave a video running or cause
   flicker.

All four are layout or one-condition changes.

## Still open from the first round

- **Page Up / Page Down (§8)** — you said you weren't sure about these. Under the overlay design
  Page Down does less than it used to: the results are already the live list, so it only closes
  the panel. Worth deciding what, if anything, those two buttons should do now.

---

## §18 — Metadata filter suggestions (stage 4c) 🔴 *new*

The first visible increment of the filter feature: suggestions appear beside the keyboard, and
selecting one applies it. The chips are visible but **not yet focusable or removable** — that is
stage 4d, so the only way to take a filter off in this build is to search for it again and
select it a second time.

| | Step | Expected |
|---|---|---|
| T18.1 | 🔴 Open search and type one character. | No suggestions yet. |
| T18.2 | 🔴 Type a second character. | A column of suggestions appears to the **right of the keyboard**, and the dark panel behind widens to hold it — the artwork on the far right should still be visible. |
| T18.3 | 🔴 Read a suggestion row. | Three things: the **value** on the left, its **facet** in the middle ("genre", "platform", "publisher", "series", "developer", "play mode", "year"), and a **count** on the right. |
| T18.4 | 🔴 Type a franchise you own, e.g. `son`, `mari`, `zel`. | The series and any matching developer/publisher are offered. Counts look right against your library. |
| T18.5 | 🟡 Press **Right** repeatedly from the left of a keyboard row. | The cursor walks to the end of the row, and the *next* press moves into the suggestion column. It does **not** jump straight across from the middle of the row. |
| T18.6 | 🟡 **Hold** Right. | Wraps within the keyboard row and never crosses into the suggestions. |
| T18.7 | 🔴 With the cursor in the suggestions, look at the keyboard. | The last selected key is an **outline**, not a filled tile — the same affordance as when you are in the results row. |
| T18.8 | 🟡 Press **Up** / **Down** in the suggestion list. | Moves through it, wrapping at the top. |
| T18.9 | 🟡 Press **Down** from the last suggestion. | Moves into the results row. |
| T18.10 | 🟡 Press **Left** from the suggestions. | Back to the keyboard. |
| T18.11 | 🔴 Press **Enter** on a suggestion. | The filter is applied: a **chip** appears under the query, the **query line clears**, the cursor returns to the keyboard, and the results narrow to that filter. |
| T18.12 | 🔴 Look at the result count after applying. | It matches the count the suggestion promised before you selected it. *(This is the invariant the whole design rests on — tell me if it ever disagrees.)* |
| T18.13 | 🔴 With a filter applied, type another term and select a second suggestion. | Two chips. Results narrow further. This is the stacking the feature was asked for. |
| T18.14 | 🔴 Apply a genre, then search a term whose only matches lie outside it. | Those terms are **not offered at all** — you should never be able to select a suggestion that leads to an empty screen. |
| T18.15 | 🟡 Apply two platforms. | Widens rather than narrows — a game has one platform, so requiring both could never match. Two genres, by contrast, narrow. |
| T18.16 | 🟡 Search for a filter you already applied. | It is offered again, underlined to show it is on. Selecting it **removes** it. |
| T18.17 | 🟡 Type a broad two letters like `sp` on a big library. | No one facet fills the whole list — at most a few developers, leaving room for genres and platforms. |
| T18.18 | 🟡 Apply a filter, then press **Escape**. | Back to where you were browsing. Re-open search: the filter is still applied. |
| T18.19 | 🔴 Watch for lag while typing with filters applied. | Suggestions and counts recompute on every keystroke; it should still feel instant. |

### Known for this stage

* Chips cannot be removed by pointing at them — 4d adds that. Re-selecting the term is the way.
* Playlists are not offered as a filter at all. Deliberate; see `features/search.md`.
* `clear` still only clears the query. Clearing all filters at once arrives with 4d.

---

## §19 — The filter row (stage 4d) 🔴 *new*

The chips are now focusable and removable, they say how they combine, and `clear` takes them all
off. This completes the script the feature was asked for: apply, stack, and back out.

| | Step | Expected |
|---|---|---|
| T19.1 | 🔴 Apply a filter, then press **Up** from the top row of the keyboard. | The cursor moves to the chip row; the selected chip fills white and the keyboard key it left drops to an outline. |
| T19.2 | 🟡 With **no** filters applied, press Up from the top keyboard row. | Goes to the results (or wraps within the keyboard if there are none). There is no empty chip row to get stuck in. |
| T19.3 | 🟡 Press **Down** from the chip row. | Back to the top row of the keyboard. |
| T19.4 | 🔴 **Hold** Up on the keyboard. | Wraps within the keyboard. It must never land in the chip row. |
| T19.5 | 🟡 Apply two filters, then move into the chips and press **Left/Right**. | Moves between them, wrapping at both ends. |
| T19.6 | 🔴 Press **Enter** on a chip. | That filter is removed and the results widen immediately — **nothing retyped**. This is step 5 of the original request. |
| T19.7 | 🔴 Remove the last remaining chip. | The row disappears and the cursor returns to the keyboard rather than being stranded. |
| T19.8 | 🟡 Type a query, apply a filter, then remove the chip. | The query is untouched and still filtering. |
| T19.9 | 🔴 Apply two **genres**. | The row reads `Sports` **and** `Football` — and results require both. |
| T19.10 | 🔴 Apply two **platforms**. | The row reads `Genesis` **or** `SNES` — and results widen rather than narrow. This is the rule that would otherwise be invisible. |
| T19.11 | 🔴 Apply a platform, then a genre, then a *second* platform. | The two platforms sit **together** with `or` between them, and the genre follows with `and`. The second platform does not go on the end of the row. |
| T19.12 | 🔴 With filters applied and nothing typed, look at the `clear` key. | It reads **"clear filters"**. |
| T19.13 | 🔴 Press it. | Every filter comes off at once. |
| T19.14 | 🟡 Now type something and look at the key again. | Back to **"clear"** — and pressing it clears only the text, leaving the filters on. |
| T19.15 | 🟡 With text *and* filters, press `clear` twice. | First press clears the text, second removes the filters. |
| T19.16 | 🟡 Apply filters, press **Escape**, re-open search. | The filters are still applied and the row is still there. |

### Still open after this stage

* Suggestion threshold is two characters — say if that feels wrong on your library.
* Playlists are still not offered as a filter.
* A search still produces **one** ranked list. Grouping results into a row per facet is stage 5.

---

## §20 — Stage 4e

Mostly tuning, and most of it needs your library rather than a test. The one new control is the
suggestion count.

| | Step | Expected |
|---|---|---|
| T20.1 | ⚪ Settings → Inputs. | A new **"Search suggestions"** box, showing 8. |
| T20.2 | 🟡 Set it to 3, restart, and type a broad two letters. | At most three suggestions are offered. |
| T20.3 | 🟡 Set it to 0, restart, type. | No suggestions at all — a legitimate way to turn the filter feature off. Title search still works. Put it back to 8. |
| T20.4 | 🔴 On your real library, type two letters and read the suggestions. | Do they look useful, or arbitrary? This is the question the threshold decision rests on. |
| T20.5 | 🔴 **Type a single letter and read what is offered.** | Suggestions appear from the first character — this is the trial. Are they useful, or eight arbitrary high-count values? That is the whole question, and it is the one thing that changes code. |
| T20.6 | 🔴 Apply two or three filters and keep typing. | Suggestions and counts still keep up. This is the path that was 60 ms on a synthetic worst case before the intersection fix. |
| T20.7 | 🟡 Search for something with many matching developers or publishers. | The list stays a cross-section rather than filling with one facet. |

### Settled in 4e

Answered and recorded in the code, so they are not re-litigated:

* **Suggestion ordering** — good as it stands. Match quality first, then count.
* **Release year and play mode** — useful, staying in `Facets.Filterable`.
* **Playlist filtering** — not wanted. Stays out of the projection.
* **Eight suggestions** — right for a television.

### The threshold - settled

**One character (T20.5).** Tried on a real library and kept: a single letter offers terms worth
having rather than eight arbitrary high-count values. It shipped at two on a guess the threshold
itself made unverifiable, since it hid the evidence.

---

## §21 — Faceted result rows (stage 5) 🔴

Apply **two or more constraints** first — the query counts as one, so a query plus a filter is
enough. Below two there is deliberately only one row (RULE-SEARCH-075).

| | Step | Expected |
|---|---|---|
| T21.1 | 🔴 Apply three or four filters. | Below the search's own row there is one row per constraint, each showing the search **without** that one. Only the top row is prefixed `Search:`. |
| T21.2 | 🔴 Read the row headings. | The query's row comes first, then the filters most-recently-applied first. Each heading names exactly the constraints that row keeps. |
| T21.3 | 🔴 Type something as well, then look again. | There is now a row carrying every filter and **no** text — the row that rescues a typo. |
| T21.4 | 🔴 Walk **Down** from the keyboard through every row and past the last. | You reach the keyboard's top row again. |
| T21.5 | 🔴 Keep pressing Down through the keyboard until you re-enter the rows. | You arrive at the **first** row, not the one you left from, and every row is reachable again (RULE-SEARCH-079). This was a bug — check it deliberately. |
| T21.6 | 🔴 From the keyboard's top row press **Up** repeatedly. | You wrap into the rows at the **last** one and walk upward through them. |
| T21.7 | 🟡 Move to a row that is not the first, Escape out of search, re-open search. | You are back in that row, on that game. |
| T21.8 | 🟡 Apply two filters of the **same** facet — two developers, say. | The chip row reads `or` between them and the row **grows**. Adding a filter making the list bigger is correct here (RULE-SEARCH-051). |
| T21.9 | 🟡 With two same-facet filters applied, look at the secondary rows. | Dropping one of them makes that row **smaller** than the search, not larger. Correct, and the one case where a near miss is narrower. |
| T21.10 | ⚪ Over-narrow until nothing matches. | The near-miss rows are still shown and the status line reads `No games match everything`. |
| T21.11 | ⚪ Watch the responsiveness while typing with several filters applied. | No perceptible lag. Measured at 5.28 ms per keystroke against a 16 ms frame on a synthetic 100,000-game library, but a real library is the judge. |

**One question still open here.** A query plus a *single* filter fans out to three rows, one of
which is the bare query — a very broad row under a narrow one. Correct by the rule, possibly
wrong in practice. If it reads badly, the threshold is a one-line change.
