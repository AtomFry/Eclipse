# Stage 2 — manual validation checklist

Everything in stage 2 of [text-search.md](text-search.md) that a person has to look at, in the
order it is quickest to work through. 445 automated tests already cover the engine, the
keyboard cursor and the session state machine; **nothing below repeats those**. What is here is
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
| T7.3 | 🔴 Look at the artwork area on the right and the list heading. | The selected game's **background artwork** is showing, and the list heading reads your query (e.g. `sonic` or `sonic (12)`). |
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
| T10.1 | 🔴 Search for a game, move into the results row, highlight a game that is **not** the first, press **Enter**. | The search screen closes and the **game detail overlay opens on the game you highlighted** — with its artwork and background, not the previous game's. |
| T10.2 | 🔴 Look at the screen carefully at that moment. | Nothing is blank or missing. The box art row, list heading and details are all visible behind the overlay. *(This is a bug I found and fixed late — the overlay lives inside the results grid, so a mistake here shows as a blank screen.)* |
| T10.3 | 🟡 Press **Escape** from the overlay. | You are browsing a list named after your query — e.g. `sonic`, or `sonic (12)` if "show game count in list" is on. |
| T10.4 | 🟡 Navigate Left/Right in that list. | Behaves exactly like any other Eclipse list. |
| T10.5 | 🟡 Press **Up/Down**. | The list set has only one list, so it wraps back to itself. Expected for stage 2 — faceted rows are stage 5. |
| T10.6 | 🟡 Play a game from the search results. Exit the game. | Returns normally; History/Favorites lists update as usual. |
| T10.7 | 🟡 Turn on **"bypass game details"** in settings, restart, repeat T10.1. | The game **launches directly** instead of opening the overlay — matching what Enter does while browsing. |
| T10.8 | ⚪ Open search, type something matching nothing. | You stay in search with your query intact — the browsing surface is hidden rather than showing a stale row. |

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
