# Plan — The box art row: `ListCycle`, the 13 slots, and what feeds them

**Status: Stages 0, 1 and 5a delivered and verified. Stage 3 reduced to a look-ahead decode
after the long-session data; Stage 5b reduced to the `ListCycle` simplification.**

**Decisions taken:** `B-07` closed (Stage 0). F5 re-trigger left alone; the pump hydrates the
selection first instead (Stage 2). Test project skipped (Stage 4). Frozen-image retention is a
bounded cache, sized from Stage 1's measurement (Stage 3).

## What was asked

Review how the game cycle works — specifically how games are displayed as individual images,
how the cycle tracks them and rotates them — decide whether the design is sound, and plan a
refactor for technical design and performance. Plus: is there other work that should come first
to make that easier?

**Is it a good design?** The *concept* is right and is kept unchanged. The *data structure* is
redundant, the *update strategy* does thirteen times the work it needs to, and the *boilerplate*
is 183 lines expressing what an array expresses in one. But the two largest performance problems
in this area are not in the cycle at all — they are in what feeds it, and they are cheaper to
fix.

**What comes first?** Close a backlog item that should not be done, measure, then fix the two
feeder problems, then rewrite the cycle. In that order, because the feeder fixes change how much
the cycle rewrite is worth and the measurement is what tells us.

---

## How it works today

```
keypress
  → SelectingGameState.OnRight
      → CurrentGameList.CycleForward()
          → gameCycle.CycleForward()        // ListCycle<GameMatch>, 13 indices
          → RefreshGames()                  // assigns Game0 … Game12
              → 13 × PropertyChanged
                  → 13 × Image.Source rebind
      → CallGameChangeFunction()
          → DoAnimateGameChange()           // dim, then the 1s settle sequence
```

| Piece | Where | Size |
|---|---|---|
| `ListCycle<T>` — the index ring | `Models/ListCycle.cs` | 115 lines |
| `Game0` … `Game12` + `RefreshGames` | `Models/GameList.cs:135-159`, `:268-450` | 183 lines of boilerplate |
| Row `<Image>` elements | `View/MainWindowView.xaml:885-1210` | 13 current + 13 next, hand-written |

`ListCycle` has exactly **two** call sites — the row (`gameCycle`, 13 slots) and the
list-of-lists (`listCycle`, 2 slots). The category picker has its own index logic and does not
use it. Between them they use five members: `GetItem`, `GetIndexValue`, `CycleForward`,
`CycleBackward`, `SetCurrentIndex`.

---

## Baseline — measured 22 Aug 2026

694 games, 2560×1600 display, warm disk. Held right through 300+ games, then 30 single presses.

| Measurement | Result |
|---|---|
| Keypress → slots assigned | **1.7 ms** avg, 4.0 ms max |
| Keypress → rendered | **3.9 ms** avg, 9.8 ms max (the max in the first window, pump still running) |
| `DoAnimateGameChange` | 0.06 ms |
| Slots changed per move | **13 of 13** |
| Media pump | 674 iterations, **490,833** predicate evaluations, 5,039 ms, 694/694 hydrated |
| Decoded row image | avg **722 KB**, max 1360 KB (712×489 at 32bpp); source files avg 368 KB on disk |
| Holding one bitmap per game | **489 MB**; holding 100 → 70.5 MB |

**What it confirms.** F2 is exactly as described — 13 of 13 slots change per move, every move.
F4 is exactly as described: the predicted quadratic is `2 × 674²/2 ≈ 454,000` for the library
scan plus roughly `674 × 54` for a ~54-game current list re-scanned every iteration, totalling
~490,000 against 490,833 observed. The model fits to within a fraction of a percent.

**What it refutes.** That any of this is slow. 3.9 ms from keypress to rendered is a quarter of
one frame at 60 Hz. The 490,833 predicate evaluations cost single-digit milliseconds of a
5,039 ms pump that is otherwise file I/O — the quadratic is real and costs almost nothing at
this library size. Render time is flat across all 400 moves, with no upward drift.

**What that means for the plan.** The performance case for Stages 2, 3 and 5b is weak *on this
library*. The design case — 183 lines of slot boilerplate, a redundant index array, 26
hand-written XAML elements — is untouched by the measurement and is the reason to proceed. The
stages are re-prioritised accordingly; see "Revised priority" below.

**Pre-scaling verified.** `ImageScaler` targets `monitorHeight × 100/324 − 4`; at 1600 that is
489, matching the largest sample exactly. The row is not decoding oversized artwork.

### The long session — F3 answered, and not the way F3 said

Twenty minutes and 1,000 moves later there is **no degradation**: the windows at the end read
3.5–4.6 ms, the same as the windows at the start. The weak-cache eviction F3 predicted does not
happen here.

What does happen is visible in two windows around moves 950–975:

| Window | Keypress → slots | Keypress → rendered | Distinct row artwork |
|---|---|---|---|
| 925 | 1.63 ms | 3.52 ms | 408 |
| 950 | **4.45 ms** | **6.92 ms** | 431 |
| 975 | **5.05 ms** | **7.42 ms** | 440 |
| 1000 | 1.55 ms | 3.65 ms | 440 (flat) |

The spike tracks the distinct count exactly. While the row is showing artwork it has shown
before, a move costs ~4 ms; while roughly one new image per move enters the row, it costs ~7.4 ms.
The ~3.5 ms difference is one 722 KB image being decoded for the first time.

**And it lands in *slots assigned*, not just in render.** `PropertyChanged` is raised
synchronously inside the slot assignment, so WPF re-evaluates the binding and runs
`ImageSourceConverter` — decode included — before `RefreshGames` returns. The decode is on the
UI thread inside the property setter, which is F3's mechanism confirmed, but triggered by first
view rather than by eviction.

**This changes Stage 3.** A bounded retention cache solves eviction, which is not occurring. The
actual cost is first-view decode, and the fix for that is much smaller: decode *ahead of the
window*, off the UI thread, so the bitmap is ready before the game scrolls into view. No large
cache, no eviction policy. See the revised Stage 3.

---

## Findings

### F1 — `ListCycle`'s index array is redundant

`indices` is a 13-element array that every operation maintains as
`[start, start+1, …, start+12]` wrapped modulo the list length. `InitializeIndices` walks
`GetNextIndex` from -1; `SetCurrentIndex` sets `indices[0]` then walks; `CycleForward` shifts
left and appends; `CycleBackward` shifts right and prepends. The invariant never breaks.

So the whole array is determined by `indices[0]` and `GenericList.Count`. Thirteen integers and
an `O(window)` loop per move express one integer and an `O(1)` increment.

**A trap the rewrite removes.** `CycleForward`'s guard is `i + 1 < indices.Length - 1`, which
stops the shift one element early — `indices[11]` gets `GetNextIndex(indices[11])` rather than
`indices[12]`. It is correct only because the run is consecutive, which makes those equal. It
reads like an off-by-one, it is load-bearing on an invariant stated nowhere, and anyone
"fixing" it would be changing working code.

### F2 — Moving one game changes all thirteen slots

This is the clunkiness, and it is real work rather than only an aesthetic problem.

The window advances by one: one game enters, one leaves. But the *contents* shift — slot 1 now
holds what slot 2 held, and so on down the row. All thirteen `GameMatch` references change, so
`RefreshGames` raises thirteen `PropertyChanged`, WPF re-evaluates thirteen `Image.Source`
bindings, and the render pass touches thirteen elements — to draw twelve pictures that were
already on screen, in the same order, one position to the left.

The work is proportional to the window size when it should be proportional to what changed.

### F3 — Row images are `Uri`s, so decoding happens on the UI thread

`GameFiles.FrontImage` is a `Uri` bound straight to `Image.Source`. WPF converts it with
`ImageSourceConverter`, decoding through its URI imaging cache.

In the common case that cache absorbs F2 — the twelve still-visible games resolve to bitmaps
already decoded. But the cache holds entries **weakly**, and this application puts real pressure
on managed memory with full-screen background art. On eviction the next rebind decodes a file
**synchronously on the UI thread**, during a keypress.

This is the one place the video-pipeline work did not reach. That refactor moved five background
images off the UI thread via `FrozenImageLoader`; the row's images — the ones being looked at
while scrolling — still decode inline.

### F4 — The media hydration pump is quadratic, and it runs while you scroll

Not part of the cycle, but it is what puts pictures in the slots, and it is the largest
measurable inefficiency in this area.

`SetupNextGameFiles` (`MainWindowViewModel.cs:378-435`) is called in a loop, once per game in
the library. Each call performs six linear scans — `.Any()` then `.FirstOrDefault()` over three
collections:

```csharp
CurrentGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup)
NextGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup)
gameFilesBag.Where(gf => !gf.IsSetup)
```

`Any()` short-circuits at the first un-hydrated entry — but that entry moves further along on
every iteration, so the scan lengthens as the work completes. Over a full pass the library scan
is **O(n²)**: for a 5,000-game library, on the order of 25 million predicate evaluations. The
two list scans add `2 × list length × n` more and keep running long after both lists are fully
hydrated.

It runs on a background thread at `BelowNormal` priority, which is why it has never been
intolerable. It is still quadratic work happening precisely when the user is most likely to be
scrolling, and a cursor per collection makes it linear.

### F5 — The pump re-enters the full animation sequence

At `MainWindowViewModel.cs:400`, when the game the pump just hydrated is the selected game, it
calls `CallGameChangeFunction()` — the same entry point a keypress uses. That runs
`DoAnimateGameChange`: dim everything, cancel the in-flight selection sequence, start a fresh
1-second settle.

Refreshing the display when the selected game's artwork arrives is correct. Doing it by
replaying the whole dim-and-settle animation looked unintended.

**Decided: leave the re-trigger alone.** Closer reading shows it is load-bearing rather than
accidental. Most of the screen does update on its own — all 26 row images, the details text, the
ratings, and the clear logo's *visibility* are all data-bound to `GameFiles`. But the background
image, the clear logo's **Source**, the play mode and platform logos, the bezel and the video
are assigned imperatively inside the settle sequence, so nothing shows them without a
re-trigger. `activeVideoPath` in particular is read from `GameFiles.VideoPath`, which does not
exist before hydration — so for a game reached before the pump gets to it, this re-trigger is
the *only* thing that ever starts its video.

**The real problem is ordering, not the re-trigger.** The pump takes the first un-hydrated game
in *list order*, so landing on game 200 of 364 means waiting while it grinds through game 6
onwards, then having the screen dim and re-settle underneath you. Stage 2 addresses this by
hydrating the selection and its visible neighbours first, which makes the re-trigger rare
instead of changing what it does.

### F6 — `B-07` (memoise `MatchCount`) should be closed, not done

`MatchCount` returns `MatchingGames.Count()`. `Enumerable.Count()` takes the `ICollection<T>`
fast path on a `List<T>`, so it is already `O(1)`. Memoising it would add an invalidation
obligation — `MatchingGames` is publicly settable and mutated by curation rebuilds — in exchange
for nothing.

### F7 — A latent edge in empty lists

`RefreshGames` guards each slot with `(MatchCount > i) || RepeatGamesToFillScreen`. With repeat
on and a genuinely empty list the guard passes, and `gameCycle.GetItem(1)` indexes an empty
list. `GameListBuilder` drops empty custom lists and category lookups always have at least one
game, so it is not currently reachable — but it is reachable by construction and the rewrite
should close it rather than carry it.

---

## Is the design sound?

**Keep the concept.** A fixed window of realized elements, with one slot behind the selection,
is the right shape for this interface. `TRACEABILITY.md` notes against `B-31` that the fixed
window may be a deliberate performance choice — fixed elements avoid the container regeneration
a virtualizing `ItemsControl` does per keypress. That caution is fair, and **nothing in this
plan introduces virtualization.**

**Replace the mechanism.** Three things are wrong with the implementation, and none of them
require changing what the user sees: the index ring stores thirteen numbers to represent one;
the update shifts content through slots so everything changes when one thing did; and the slots
are 183 lines of hand-written properties plus 26 hand-written XAML elements.

**Do the cheap wins first.** F3 and F4 are independent of the cycle entirely and are probably
worth more in felt performance than the cycle rewrite is.

---

## Revised priority, after the baseline

The original order put the cheap performance wins first and the design work last, on the
assumption that the row was slow. It is not. Re-ordered by what the measurement actually
supports:

| Stage | Original case | After the baseline |
|---|---|---|
| 5a — indexed collection | "deletes 500 lines, not a perf change" | **Promoted.** The only claim the data still supports, and the answer to the original question. |
| 5b — cycle rewrite + rotate mapping | "one slot changes instead of thirteen" | **Keep, reframed.** Saves ~1.5 ms nobody can feel. Worth it for `O(1)`, for removing F1's trap, and because it finishes 5a. |
| 2 — pump | "largest measurable inefficiency" | **Demoted.** Correct diagnosis, negligible cost here. Defensible as correctness and as scalability for a 10,000-game library, not as speed. |
| 3 — frozen images | "probably the largest single win" | **Hold.** No eviction signal in 400 moves. And it would *cost* memory: a bounded cache is 70 MB we do not currently pay. Needs the long-session data before it is worth anything. |

At 10,000 games the arithmetic flips for Stage 2 — roughly 100 million predicate evaluations and
a pump running for over a minute, during which F5's re-trigger genuinely does fire under the
user. That is the case for doing it, and it is a case about other people's libraries rather than
this one.

---

## Stages

Ordered so that each stage is independently shippable, and so that the expensive stage is
informed by the cheap ones. **See "Revised priority" above — the baseline changed the order.**

| Stage | Work | Code risk | Independent of the rest? |
|---|---|---|---|
| 0 | Close `B-07` | none — docs only | yes |
| 1 | Instrument the browsing path (`B-28`) | low — additive | yes |
| 2 | Make the hydration pump linear | medium | yes |
| 3 | Frozen `ImageSource` for the row | medium | yes |
| 4 | Test project for `ListCycle` (`B-01`) — **recommended, your call** | none to production | yes |
| 5a | Slots become an indexed collection | **high — XAML** | needs 5b to follow |
| 5b | `ListCycle` rewrite + rotate the mapping | medium | needs 5a |

---

### Stage 0 — Close `B-07`

Documentation only, no code. `B-07` is in the backlog as "memoise `MatchCount`"; per F6 it is a
micro-optimisation of an already-`O(1)` call that would add invalidation risk. Recording *why*
it was closed matters more than closing it — otherwise it reappears.

- **Changes:** `docs/TRACEABILITY.md` — `B-07` marked closed-not-done with the reason.
  `docs/features/browsing.md:140` — the `B-07` line updated. `VER-BROWSE-007` keeps its rule
  (`RULE-BROWSE-011`/`016`, repeat-to-fill) since that is about slot population, not the count.
- **Verify:** nothing to run.
- **Decision needed:** none, unless you disagree with F6.

---

### Stage 1 — Instrument the browsing path (`B-28`)

`TRACEABILITY.md` already says `B-31` is "possibly premature … needs `B-28` latency data before
it is treated as debt at all." That applies to this whole plan. Nobody can currently say whether
a keypress costs 2 ms or 40 ms, or where it goes.

A `BrowsePerformanceMonitor` in the shape `VideoPlaybackMonitor` established — session counters
plus a summary line in the log, off unless a hidden setting is on. Measure:

| Measurement | Why |
|---|---|
| `CycleForward` → slots assigned | isolates F1/F2 from everything downstream |
| Slots assigned → render complete | the binding and decode cost F2 and F3 produce |
| `DoAnimateGameChange` duration | separates animation cost from row cost |
| Count of slots that actually changed per move | should be 13 now, 1 after Stage 5b |
| Distinct vs repeat artwork entering the row | proxy for F3 — see the deviation note below |
| **Decoded size of a row image** | sizes Stage 3's cache from measurement instead of arithmetic |
| Pump iterations and predicate evaluations | quantifies F4, and proves Stage 2 |

**The decoded-size measurement.** A decoded bitmap costs `width × height × bits-per-pixel` and
owes nothing to the size of the file it came from — a 40KB png can be a third of a megabyte in
memory. Since Stage 3 takes ownership of those bitmaps from WPF's weak cache, how many to keep
becomes a decision, and it should rest on this number rather than on an assumed box-art aspect
ratio. The monitor decodes the first 25 distinct row images off the UI thread, records their
dimensions, discards them, and reports the average alongside what holding one per game across
this library would cost. It logs as soon as the sample completes, so it does not depend on the
user having scrolled far enough to trigger a move summary.

**Deviation from this plan as written — image decodes are not counted directly.** WPF's URI
imaging cache is internal; there is no supported way to ask whether a given `Image.Source`
rebind hit it or decoded a file. Counting decodes would mean owning the decode, which is Stage 3.
Two things stand in: **keypress-to-rendered**, which is where a synchronous decode lands and is
the number that actually matters; and **distinct vs repeat row artwork**, which says how much
cache pressure the row generates. If keypress-to-rendered has a long tail while
keypress-to-slots stays flat, that tail is decode.

**Baseline protocol** — run once and keep the numbers, since every later stage is measured
against them:

1. Cold start, note startup pump totals.
2. Hold right through a list of 300+ games; record per-move timings.
3. Single presses, one per second, for 30 moves — the interactive case, where the cache is
   warm and the pump is idle.
4. Repeat 2 after 20 minutes of browsing, when memory pressure makes cache eviction likely.

- **Files:** `Eclipse/Service/BrowsePerformanceMonitor.cs` (new); call sites in
  `Models/GameList.cs`, `View/MainWindowView.xaml.cs`, `View/MainWindowViewModel.cs`;
  `MeasureBrowsePerformance` in `Models/EclipseSettings.cs`.
- **Verify:** numbers appear and are plausible; with the setting off, nothing is written and
  browsing is unchanged.
- **Removed:** at the end of Stage 5b, like the `B-15` dump machinery.

**How it reports.** A summary line every 25 moves, so holding a direction produces output
without needing a session-end hook, and a deliberate 30-press walk also reports. The pump logs
once when it finishes.

**What counts as a "move".** `MoveStarted` is on `GameList.CycleForward`/`CycleBackward`, so a
move is one step of the selection. Page up and down go through `DoRandomGame` rather than
cycling, so they are **not** counted as moves — but they still refresh the slots, so they appear
in the slot-change counts under a larger "assignments" total than "moves". That asymmetry is
intentional: it keeps the per-move timings clean while still showing that a page jump also
rewrites the whole row.

**Cost when off.** Every call site checks `IsEnabled` first, and the slot snapshot is not
allocated at all unless the monitor is on. That matters more than usual here: the pump predicate
counter ticks on the order of tens of millions of times over a startup pass, and paying for it
in a normal session would be worse than the problem being measured.

---

### Stage 2 — Make the hydration pump linear

Replace the three re-scans with a cursor per collection. The pump's contract stays the same:
prefer the current list, then the next list, then anything else; stop when everything is
hydrated.

Points to get right:

- **Cursors must survive list changes.** `CurrentGameList` changes when the user moves between
  lists, so the current/next cursors reset on list change while the library cursor does not.
- **Curation rebuilds replace `GameList` objects** but not `GameMatch`/`GameFiles` instances, so
  the library cursor stays valid across a rebuild. Worth asserting rather than assuming.
- **The `processedCount > GameFilesCount` guard** is an infinite-loop backstop for exactly the
  bug the rescan design invites. With cursors it becomes unnecessary; keep it anyway as a
  cheap assertion, but log if it ever trips.

**Also in this stage: hydrate the selection first.** Today the pump takes the first un-hydrated
game in list order, which is why F5's re-trigger fires under the user so visibly. Prioritise the
selected game and the games in the window, then the rest of the current list, then the next
list, then the library. The re-trigger itself is left exactly as it is (see F5) — this makes it
rare rather than changing it.

The cursor design has to accommodate this: a cursor per collection is not enough on its own if
the window jumps around, so the window is checked first on each pass and the cursors handle
everything behind it.

- **Files:** `Eclipse/View/MainWindowViewModel.cs` (`SetupFiles`, `SetupNextGameFiles`).
- **Verify:** Stage 1 shows a linear pass; **every game still ends up hydrated** — compare the
  count of `IsSetup == true` across the library after a full pass, before and after; artwork
  still appears promptly for the list you are actually in while scrolling during startup.

---

### Stage 3 — Decode ahead of the window — **built, awaiting verification**

Reduced from "frozen `ImageSource` with a bounded retention cache" after the long-session data.
Eviction is not happening; first-view decode is. The original text follows below for the record.

`RowImageDecoder` decodes box art off the UI thread before the game reaches the row.
`GameFiles.FrontImageSource` holds the frozen result and the six row bindings point at it
instead of at the `Uri`.

Four decisions worth recording:

- **Warming is driven by navigation, not by refresh.** Every list refreshes its slots when it is
  built, and a library has hundreds of lists across the eight category sets — warming there
  would queue tens of thousands of decodes at startup for rows nobody is looking at. The callers
  are `CycleForward`/`CycleBackward`/`SetGameIndex` and the view model when it changes which two
  lists are displayed.
- **Bounded at 96 decoded images**, roughly 68 MB at the measured 722 KB. The row shows 26 at a
  time; the rest is scroll headroom in both directions. One per game would be 489 MB on this
  library alone.
- **Look-ahead is 8 games either side.** A move takes ~40 ms with a direction held and a decode
  takes ~3.5 ms, so the decoder has an order of magnitude more runway than it needs.
- **A changed artwork path invalidates the decoded copy, and re-decodes only if something had
  been decoded.** Two things change the path: flipping the box, and the media pump replacing a
  placeholder with real artwork. Both can happen while the game is on screen. The
  "only if decoded" guard matters — every game passes through the `FrontImage` setter twice
  while the catalog is built, so re-decoding unconditionally would decode the whole library at
  startup.

**The risk to watch.** A slot bound to a decoded bitmap shows *nothing* until the decode lands,
where before it showed the image after a 3.5 ms hitch. Navigation warms ahead so this should not
be visible, but the first row after a list change starts from nothing.

- **Files:** `Eclipse/Service/RowImageDecoder.cs` (new), `Eclipse/Models/GameFiles.cs`,
  `Eclipse/Models/GameList.cs`, `Eclipse/View/MainWindowViewModel.cs`,
  `Eclipse/View/MainWindowView.xaml` (6 bindings)
- **Verify:** the first-view spike (keypress → slots ~4.5 ms, → rendered ~7.4 ms when distinct
  artwork is climbing) should flatten to the ~1.7 ms / ~4 ms of the warm case; no blank slots
  while scrolling or when changing lists; flip box still swaps; artwork still appears as the
  pump hydrates at startup; memory over a long browse

---

### Stage 3 (original) — Frozen `ImageSource` for the row

`GameFiles` gains a frozen `ImageSource`, decoded off the UI thread by `FrozenImageLoader`,
**derived from `FrontImage` rather than replacing it**. The row's `Image` elements bind to the
frozen source; the enlarged view and every other consumer keep the `Uri`.

Deriving rather than replacing is what makes flip-box keep working. `KeyStrategyFlipBox` swaps
`FrontImage`/`BackImage` by assignment; if the frozen source is produced whenever `FrontImage`
is set, the flip triggers a fresh decode and `RULE-PRESENT-008` holds with no extra code.

**Open decision — retention policy.** A frozen bitmap per game, held for the session, is memory
the weak URI cache currently reclaims. Three options:

| Option | Memory | Risk |
|---|---|---|
| Hold for every hydrated game | highest | may be fine — row art is pre-scaled to display height by `ImageScaler` |
| Hold a window around the selection plus a margin | bounded | eviction logic to get right; a fast scroll can outrun it |
| Hold in a size-capped LRU | bounded | most code |

**Decided: bounded cache, from the outset.** "Hold everything" is not a simpler starting point,
it is a build that reaches gigabytes on a real library - a decoded bitmap costs
`width x height x bits-per-pixel` and owes nothing to the file size, so at 1080p a row image is
roughly 325KB and 5,000 of them are ~1.6GB. Stage 1 now measures the real figure on this monitor
with this artwork, and the cap is sized from that. The row shows 26 images at once (13 current +
13 next); a cap around 100 covers that plus scroll history.

The superseded reasoning: start with the simplest - hold
for every hydrated game — and measure. `ImageScaler` already pre-scales row art to the monitor's
height, so the per-image cost may be small enough that nothing more is needed.

There is a real chance this stage alone resolves the felt clunkiness. If it does, Stage 5 is
still worth doing for the ~500 lines it deletes, but it stops being a performance argument —
and that is worth knowing before starting it.

- **Files:** `Eclipse/Models/GameFiles.cs`, `Eclipse/Service/FrozenImageLoader.cs`,
  `Eclipse/View/MainWindowView.xaml`.
- **Verify:** no UI-thread decode during a scroll (Stage 1 counters); memory over a 30-minute
  browse; flip box (`RULE-PRESENT-008`); zoom box; the enlarged/featured views still show
  full-size art; a game with no art still shows the default image.

---

### Stage 4 — Test project for `ListCycle` (`B-01`) — **skipped, by decision**

Recorded rather than deleted, because the reasoning still applies if `B-01` is revisited.

`ListCycle<T>` is pure integer arithmetic over a list — no WPF, no LaunchBox, no settings, no
singletons, five public members. It is the most testable class in the product, it has no
coverage, it drives every navigation the user performs, and Stage 5b rewrites it. For `B-15` the
probes were a fair substitute because the thing under test needed a real library; here there is
nothing to substitute for.

Roughly thirty lines would have covered it: forward and backward wrap; `SetCurrentIndex` with
and without `oneBased`; a list shorter than the window; a single-element list; an empty list
(F7); and the invariant `GetItem(i) == list[(GetIndexValue(0) + i) % count]`, which is the
property the rewrite depends on.

**Consequence for Stage 5b.** It is verified instead by Stage 1's counters — specifically
slots-changed-per-assignment dropping from the window size to 1 — plus manual navigation. The
two-slot `listCycle` (the list-of-lists) is the case to exercise hardest by hand: a window of 2
is where an off-by-one in the rewrite would hide, and no counter will catch it.

---

### Stage 5a — Slots become an indexed collection

`Game0` … `Game12` become one indexed collection on `GameList`. The 26 hand-written `Image`
elements become two `ItemsControl`s over a **fixed-size, non-virtualizing** panel.

Structure only. The update strategy does not change yet — the same thirteen entries are still
assigned per move — so Stage 1's timings should be *unchanged*. That is the acceptance
criterion: if they move, something else changed too.

Moving with it: `RULE-PRESENT-004` (blanking the slot behind the selection at index 0),
`RepeatGamesToFillScreen`, and F7's empty-list edge, which gets closed.

Deletes 183 lines of property boilerplate and roughly 320 lines of XAML.

**This is the highest-risk stage in the plan.** XAML has no compile-time check for a binding
path, and the row bindings reach four levels deep
(`CurrentGameList.Game4.GameFiles.FrontImage`). A typo produces a blank image and no error.
Verification is a **binding-error trace**, not visual inspection — the same reason `B-33` is
specified that way.

- **Files:** `Eclipse/Models/GameList.cs`, `Eclipse/View/MainWindowView.xaml`.
- **Verify:** binding-error trace clean at `System.Diagnostics.PresentationTraceSources`
  verbose; `VER-PRESENT-004`; repeat-to-fill on and off (`VER-BROWSE-007`); the next-list row;
  Stage 1 timings unchanged.

---

### Stage 5b — `ListCycle` rewrite and rotate the mapping

Two changes that only pay off together.

**The cycle keeps one index.** `GetItem(i)` becomes `list[(start + i) % count]`; forward and
backward are `O(1)`; `GetIndexValue` and `SetCurrentIndex` keep their signatures. Both callers —
the row and the two-slot list-of-lists — must be satisfied by the same rewrite.

**The row rotates the mapping, not the content.** The element leaving the window moves to the
other end and only *its* content is assigned, so a move changes **one** slot instead of
thirteen. The elements stay fixed and no container is regenerated — this is the ring buffer the
current design gestures at, done in the containers rather than in the data.

The visible result must be identical: same games, same order, same emphasis, same animation.

- **Files:** `Eclipse/Models/ListCycle.cs`, `Eclipse/Models/GameList.cs`,
  `Eclipse/View/MainWindowView.xaml` and `.xaml.cs`.
- **Verify:** `ListCycle` tests if Stage 4 was taken; wrap in both directions at both ends of a
  list; page up and page down (`GamesToPage = 7`); `SetCurrentIndex` via random game and via
  position restoration after favouriting; moving between lists; a list shorter than 13 with
  repeat on and off; **Stage 1 counter shows 1 slot changed per move instead of 13**; timings
  versus the Stage 1 baseline.

---

## Risks

| Risk | Mitigation |
|---|---|
| The fixed 13-slot window is a deliberate performance choice and replacing it regresses | No virtualization anywhere in this plan. 5a keeps a fixed panel of fixed elements; 5b keeps them fixed and moves only content. Stage 1 measures before and after. |
| Stage 5a rewrites deep binding paths and a broken one fails silently | Binding-error trace, not visual inspection. |
| Frozen images grow memory where the weak URI cache used to reclaim | Retention policy chosen from measurement, not up front; windowed or LRU cache as fallback. |
| Stage 2 changes what the user sees during startup (F5) | Called out as an explicit decision; not changed without one. |
| The cycle rewrite turns out not to be worth much once Stages 2 and 3 land | A good outcome, not a wasted one — and the reason measurement comes first. 5a still stands on the ~500 lines it deletes. |
| `ListCycle` has two callers with different window sizes (13 and 2) | Both exercised in Stage 4's tests; the two-slot case is the one most likely to expose an off-by-one. |
| Stage 5a and 5b together are a large change to the most-used code in the product | They ship separately. 5a is verified by "timings unchanged", 5b by "timings improved" — different criteria, so a regression in either is attributable. |

---

## Out of scope

- **Virtualization / `B-31` as written.** A different proposal with a different risk profile.
  Revisit only if Stage 1 data says the fixed window is itself the problem.
- **`B-19` view delegates.** The row refactor touches `CallGameChangeFunction`, but replacing
  the whole view↔viewmodel delegate mechanism is a separate, larger change.
- **`B-32` / `B-33`** (PropertyChanged generators, converter consolidation). Stage 5a deletes a
  lot of `PropertyChanged` boilerplate as a side effect; that does not make it `B-32`.
- **Changing what the row looks like.** Window size, emphasis, spacing, and animation timing all
  stay exactly as they are.

---

## Decisions needed before implementation starts

1. **F5** — when artwork arrives for the selected game, replay the animation (today) or refresh
   in place? Blocks Stage 2's completion, not its start.
2. **Stage 4** — build the test project, or verify Stage 5b by instrumentation and hand?
3. **Stage 3 retention policy** — happy to start with "hold everything" and measure, or do you
   want a bounded cache from the outset?

Everything else is mechanical and needs no input.
