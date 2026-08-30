# Plan — The media pipeline, then the presentation seam

**Status: complete, pending the manual sweep. All stages delivered.**

| Stage | State |
|---|---|
| 0b — remove browse instrumentation (`B-34`) | **Done**, first. 475 lines plus eight call sites; `RefreshGames`/`RefreshGamesCore` collapsed back into one method. Build clean, 72 tests green. |
| 0a — documentation catch-up | **Done**, second — deliberately after 0b, so the docs were written once and could record `B-34` as delivered rather than being edited twice. |
| A1 — dead startup work | **Done and verified.** `VER-MEDIA-009` passed: cache deleted, cold start repopulated it fully and browsing stayed smooth. Measured first: 58–70 ms warm, 136 ms cold — real but minor, and the plan's "startup win" framing is withdrawn. Four methods deleted with it. |
| A2 — resolve-once race | **Done**, pending the manual soak. Deviated: the lock-and-task went into a `RunOnce` helper rather than inline in `GameFiles`, because inline it could not be tested — see the stage. Six tests added, two of which fail against the old design. |
| A3 — measure the crop | **Done.** The `GetPixel` scan is **10%** of image work; 71% is file I/O and codecs. Instrumentation removed. |
| A4 — crop bounds correction | **Done.** Reduced to the bounds fix — `B-29`'s `LockBits` rewrite withdrawn on the A3 evidence. **D1 answered as D1-b**: documented in `README.md`, cache not swept. Ten tests added, eight of which fail against the old bounds. `B-06`'s failure-path residual fixed alongside, in the same two methods. |
| A5 — bezel consolidation (`B-20`) | **Done.** Five levels enumerated in `media.md` first, as `B-20` required. `BezelService.ResolveBezel` is now the single entry point; `BezelRules` holds the decision and is unit tested. The view's copy is deleted. Equivalence swept over ~200,000 dimension pairs. Corrected `RULE-MEDIA-024`. |
| B1 — timings as settings | **Done.** **D2 answered as D2-a**: new Presentation tab, with *Video delay* moved onto it. **D3 answered: no** — the video failure thresholds stay constants. Five settings, `SelectionTimings`, nine tests including the upgrade case. |
| B2 — video failure policy | **Done.** `VideoFailurePolicy.Decide` holds the escalation; the view keeps only the effect. Sixteen tests, both thresholds enumerated either side. Deviated: made it a static class to match `BezelRules`, rather than the sealed class with an instance method the plan sketched — it holds no state. |
| B3 — presenter interface | **Done**, folded into B4. `ISelectedGamePresenter` — nine members; `MainWindowView` implements it. |
| B4 — selection sequence | **Done.** `SelectedGameSequence` owns the ordering and cancellation; the view holds only what appears on screen. Thirteen tests. Mutation-tested: three deliberate breaks, one of which survived and produced a fourteenth test. View down from 833 to 720 lines. |
| B5 — delegates and bezel call | **Done.** The two delegate properties and their forwarders are two events the view subscribes to; three delegate types deleted, one of which was already unused. The bezel decode moved off the UI thread. Found and fixed a marshalling bug B4 introduced. |
| B6 — documentation | **Done.** `presentation.md`, `media.md`, `configuration.md`, `VERIFICATION.md`, `TRACEABILITY.md`, `UNRESOLVED.md` and `README.md`. Nine `VER-*` scenarios added, seven of them automated. |

**Decided during implementation:** front box art is **not** cropped, only scaled. This was
raised in review against the possibility that cropping had been lost in the modernization.
It had not — see the note under A1.

Two pieces of work in one staged plan. **Part A** is the image and media pipeline —
`ImageScaler`, the startup pre-scale pass, the resolve-once race and the bezel chain.
**Part B** is the selected-game presentation sequence, which still lives in
`View/MainWindowView.xaml.cs` and is the last large body of logic in view code-behind.

They are one plan because Part A ends by building the bezel API that Part B's last stage
needs to call, and because Part A's stages are the ones that put a fixture harness into
`Eclipse.Tests` — which is the verification Part B does not otherwise have.

Addresses `M-1`/`B-05`, `S-7`/`B-29`, `B-20`, `B-34`, `S-4`, `M-8` (presentation share),
`C-5` (presentation share) and `B-19`. Answers `OQ-007`.

---

## Decisions taken before writing

Four questions were put before this plan was drafted. The answers shape it, so they are
recorded here rather than buried in the stages.

| # | Question | Answer |
|---|---|---|
| 1 | Which part runs first? | **Media pipeline first.** Every media stage is independently verifiable; the presentation stages are not, and they inherit the harness. |
| 2 | What happens to the selection sequence's hardcoded timings? | **They become settings.** This is deliberate feature work, not a pure debt exercise — see B1. It answers `OQ-007` in the affirmative. |
| 3 | What is the acceptance criterion for the crop rewrite? | **Correct the bounds.** The retained transparent row and column are a defect and are removed, so cropped output changes and is *not* byte-identical to what is cached today. |
| 4 | Does the plan carry a Stage 0? | **Yes — documentation catch-up and `B-34`.** Both touch files the later stages edit. |

**Answered.** Answer 3 had a consequence that answer 3 did not itself settle: every existing
installation holds clear logos cropped the old way, and nothing regenerates them. That is
**decision D1**, below, and it was settled as **D1-b** — documented in `README.md`, not swept.

---

## Baseline

Verified on this branch (`modernization`, at `d63dabb`) on 2026-08-25, before any of the
work below:

| Check | Result |
|---|---|
| `dotnet build Eclipse.sln` | succeeds, 0 warnings, 0 errors |
| `dotnet test Eclipse.sln` | 72 passed, 0 failed |
| `View/MainWindowView.xaml.cs` | 833 lines |
| `Service/ImageScaler.cs` | 610 lines |
| `Service/BrowsePerformanceMonitor.cs` | 475 lines |

**The documentation is behind the code**, which is why Stage 0a exists. Four commits
(`66767e5`, `075b976`, `1b858dd`, `d63dabb`) changed code and no docs. Specifically:

* `Eclipse.Tests/` exists and holds 72 passing tests. `B-01` is listed everywhere as an
  unmet prerequisite; it is partly met.
* `Helpers/LayoutGeometry.cs`, `Service/RowImageDecoder.cs`,
  `Service/FrozenImageLoader.cs` and `Service/ISpeechSession.cs` are not described
  outside `presentation.md`.
* `S-4` is recorded as "684 lines of code-behind". The sequence has since been rewritten
  into one cancellable async method. The finding still stands in kind, but not in the
  terms it is written in.
* `M-2` and `M-3` (the GDI+ bitmap and stream leaks) appear **closed** — every path in
  `ImageScaler` is inside a `using`, and `ScaleDefaultBoxFront` carries a comment
  explaining the stream's scope. `B-06` should be confirmed and closed by Stage 0a rather
  than scheduled.
* Voice search is done. `TRACEABILITY.md` still recommends it as the first capability.

---

## Why media first

The order was chosen, not inherited. Three reasons:

1. **Part A can be proven; Part B cannot.** A crop or a scale is a pure function from a
   file to a file. Its correctness is a fixture and a comparison. The selection sequence's
   correctness is "the fade looked right", and the only instrument for that is a person
   watching it. Doing the provable work first means the fixture harness, the test project
   conventions and the habit of writing them all exist before the unprovable work starts.
2. **Part A builds the API Part B wants to call.** The decision "which bezel, and which
   way round" is made today in `Video_SelectedGame_MediaOpened`, in the view, using the
   video's natural dimensions. Stage A5 consolidates the whole five-level chain behind one
   call. Stage B5 then deletes the view's copy and calls it. Doing Part B first would mean
   inventing a temporary home for that decision and moving it twice.
3. ~~**Part A is where the measured, every-run cost is.**~~ **Withdrawn — this was asserted
   before it was measured, and the measurement does not support it.** A1's two dead
   enumerations cost 58–70 ms warm and 136 ms cold, not the startup share the wording
   implied. See the measurement under A1. Reasons 1 and 2 stand on their own and are why
   the order is unchanged.

---

# Part A — the media pipeline

## Stage 0 — clear the ground

Two things that touch the files every later stage edits.

### 0a — bring the documentation level with the code

Correct the drift listed under **Baseline**. Concretely:

* `VERIFICATION.md` — replace "There is no test project and no automated tests of any
  kind" with what is actually there, and move the rows that say "needs `B-01` only" into
  "available now".
* `TRACEABILITY.md` — mark `B-01` partly delivered; mark `B-03`/`M-5` and the voice search
  epic delivered; retire the "Recommended first capability" section, which is spent.
* `docs/features/media.md` — confirm and close `M-2`, `M-3` and `B-06`, or, if the
  confirmation fails, record precisely what is still leaking.
* `docs/features/presentation.md` — restate `S-4` in current terms and record
  `RowImageDecoder` and `FrozenImageLoader` in the implementation map.

**Files:** `docs/*.md`, `docs/features/media.md`, `docs/features/presentation.md`.
**Verification:** none — no code changes.

### 0b — remove the browse instrumentation (`B-34`)

`d63dabb` deleted `StartupPerformanceMonitor` (636 lines) as spent scaffolding.
`BrowsePerformanceMonitor` is the same thing for the same refactor and is still wired in.

Delete `Service/BrowsePerformanceMonitor.cs`, the `MeasureBrowsePerformance` setting, and
the eight call sites: `Models/GameList.cs:127,135,146`,
`View/MainWindowView.xaml.cs:408`, `View/MainWindowViewModel.cs:438,446,459,469`.

Note the helper in `MainWindowViewModel` that exists **only** to be counted — `NeedsSetup`,
and through it the `PumpPredicateEvaluated` call. `NeedsSetup` collapses back to
`!gameFiles.IsSetup` at its call sites, but keep the method: Stage A2 changes what
"already set up" means, and one place to change it is worth the indirection.

The `B-28` measurements this scaffolding produced are recorded in
`docs/plans/box-art-row-refactor.md` and are not lost by deleting the code that produced
them.

**Files:** `Service/BrowsePerformanceMonitor.cs` (deleted), `Models/EclipseSettings.cs`,
`Models/GameList.cs`, `View/MainWindowView.xaml.cs`, `View/MainWindowViewModel.cs`.
**Verification:** build; `dotnet test`; browse sweep (`VER-BROWSE-003`, `VER-BROWSE-004`).

---

## Stage A1 — stop doing two library scans that produce nothing

**This is a new finding, not a backlog item.**

`GameCatalog.Setup()` calls `PrescaleImages()` (`Service/GameCatalog.cs:91`) on the
startup path, inside the setup lock, before any game is built. `PrescaleImages` (`:158`)
opens with three enumerations:

```csharp
List<FileInfo> gameFrontFilesToProcess  = ImageScaler.GetMissingGameFrontImageFiles();
List<FileInfo> platformLogosToProcess   = ImageScaler.GetMissingPlatformClearLogoFiles();
List<FileInfo> gameClearLogosToProcess  = ImageScaler.GetMissingGameClearLogoFiles();
```

`platformLogosToProcess` is iterated and cropped. **The other two are never used for
anything except the `||` gate that decides whether to compute `desiredHeight`** — and
`desiredHeight` is needed only when the default box front is missing, which the fourth
condition in that gate already says on its own. Game box art and game clear logos are
scaled and cropped lazily in `GameFiles` (`Models/GameFiles.cs:580,600,621`), which is
where that work moved.

Each of the two dead calls walks every platform directory, every configured front-image or
clear-logo subfolder, every child folder of those, and does a `GetFiles` on each — then
`Except`s the two sides with a `FileInfo` comparer. On a large library that is the whole
image tree, twice, on every single start, discarded.

**The change:**

```csharp
if (!ImageScaler.DefaultBoxFrontExists())
{
    ImageScaler.ScaleDefaultBoxFront(ImageScaler.GetDesiredHeight());
}

foreach (FileInfo fileInfo in ImageScaler.GetMissingPlatformClearLogoFiles())
{
    ImageScaler.CropImage(fileInfo);
}
```

### Where the work these lists describe actually went

Checked against `master` and the full history, because "the modernization dropped some
image processing" was the obvious thing to suspect:

* **`master` has the identical dead code** — the same three enumerations, the same two
  never iterated. The modernization moved it from `GameBagService` to `GameCatalog`
  unchanged.
* The bulk loops were removed in **`cbb3066` (Feb 2022, "Image scale/cache in background as
  images are needed")**, which deleted the front-image loop, commented out the clear-logo
  loop, and moved both to the lazy path in `GameFiles` that runs today. The
  `GetMissing…` calls were left behind. **These are 2022 leftovers, not a regression.**
* The three resolve methods in `GameFiles` are **byte-identical** between `master` and this
  branch.

**Front box art is scaled, not cropped — and never has been.** `ScaleImage` contains no
crop, and the bulk loop it replaced called `ScaleImage(fileInfo, desiredHeight)` too.
`CropImage` has only ever been applied to clear logos, game and platform, since `ef8fab6`.
Reviewed and **confirmed as intended**: box scans are opaque, so a crop would be a no-op on
most of them at the cost of a full alpha scan each, and where it were not a no-op it would
change the box's aspect ratio against a fixed slot height.

`ImageScaler.ScaleImage(FileInfo, int)` is the bulk loop's overload and is also uncalled.
It goes with the two `GetMissing…` methods.

### The measurement — taken 2026-08-26, on a 1,469 game library

| Run | Total | **Dead:** front scan | **Dead:** clear-logo scan | Kept: platform scan | Kept: crop | Placeholder |
|---|---|---|---|---|---|---|
| warm | 58 ms | 52 ms (27 files) | 6 ms (6) | 1 ms (0) | 0 ms | — |
| **cold** | **159 ms** | **126 ms (1,469)** | **10 ms (1,450)** | 1 ms (0) | 0 ms | 23 ms |
| warm | 70 ms | 51 ms (30 files) | 19 ms (16) | 1 ms (0) | 0 ms | — |

**This is not a startup win, and the plan was wrong to call it one.** Deleting the two
scans saves **58–70 ms warm and 136 ms cold**. On a library three times this size it stays
in the same order of magnitude — the scans are directory enumerations, not per-file IO.

The change is still right, for the reason it was always right: **it is dead work.** It
computes two lists, every start, that nothing reads. But "Part A is where the measured,
every-run cost is" was an assumption stated before it was measured, and the measurement
does not support it. The genuine every-run costs are elsewhere, and `A3` is where they get
measured rather than asserted.

**Recorded as a lesson, because it is the second time.** `B-26` learned "before scheduling
an empirical audit, check whether the code already answers it". This is the converse:
*before describing dead code as a performance win, measure it.* Removing it needed no
justification beyond being unread; the performance framing was decoration, and decoration
that turns out to be false is worse than no framing at all.

**One thing the measurement showed that the plan did not predict.** The warm runs report
27–30 missing front images and 6–16 missing clear logos, not zero — and the counts move
between runs. So even when the lists were being used, they never described "the work
remaining": the lazy path caches only the one image LaunchBox picked per game, while the
scan counts every file in every configured priority folder. The two could never have
agreed. That is further evidence the gate they fed was meaningless, not just redundant.

**The side effect that must be checked, not assumed.** `GetMissingFilesInFolder` is not
pure: it calls `Directory.CreateDirectory(pathB)` on the mirrored cache folder for every
folder it inspects. Removing the two calls means those folders are no longer pre-created
at startup. `ScaleImage` and `CropImage` each create their own destination folder before
saving, so the lazy path does not depend on it — **verify this by clearing the resolution
folder and confirming a cold start still populates artwork**, rather than by reading the
code twice.

Also decide, and record, whether `GetMissingGameFrontImageFiles` and
`GetMissingGameClearLogoFiles` are deleted outright. They become uncalled. They are also
the only written record of how the mirrored cache is laid out. Recommendation: delete
them, and put the layout in `media.md`, where it belongs.

**Files:** `Service/GameCatalog.cs`, `Service/ImageScaler.cs`, `docs/features/media.md`.
**Verification:** new `VER-MEDIA-009`; cold-cache start; warm start.

---

## Stage A2 — the resolve-once race (`M-1`, `B-05`)

`GameFiles.SetupFiles` (`Models/GameFiles.cs:443`):

```csharp
if (IsSetup == false)
{
    IsSetup = true;
    ...populate every Uri...
}
```

The flag is set before the work, non-atomically, and `IsSetup` is a plain auto-property.
Two callers reach it concurrently: the hydration pump
(`MainWindowViewModel.SetupNextGameFiles`) and the screen saver
(`AttractModeSlideshow.LoadBackgroundAsync`).

The visible consequence is already written down in the code, as a workaround:

```csharp
// GameFiles.SetupFiles sets IsSetup before it populates, so a game already being
// hydrated elsewhere returns immediately with its Uris still null. Keep the
// fallback: a placeholder background beats a slide with nothing on it.
```

So the race does not merely risk duplicate work — it is the reason attract mode can show
the placeholder background for a game that has one of its own.

**The change:** give `GameFiles` a `Task`-valued hydration handle guarded by a lock, so a
second caller awaits the first's completion instead of being told "done" and finding
nulls. `IsSetup` stays as the public "has this been hydrated" predicate the pump reads,
but becomes true only when population has finished.

Sketch:

```csharp
private readonly object setupLock = new object();
private Task setupTask;

public Task SetupFiles()
{
    lock (setupLock)
    {
        if (setupTask == null)
        {
            setupTask = Task.Run(() => { Populate(); IsSetup = true; });
        }
        return setupTask;
    }
}
```

Two things to be careful about, both behaviour rather than style:

* **`IsSetup` has a public setter and is read by the pump's `NeedsSetup` predicate.**
  Confirm nothing outside `GameFiles` writes it before making it read-only.
* **The pump's loop guard counts iterations against `gameFilesBag.Count`.** If awaiting a
  peer's hydration changes how many iterations a pass takes, the guard is still safe but
  its meaning shifts. Re-read it rather than assuming.

Once this lands, the attract-mode workaround comment no longer describes reality. Either
delete the fallback or keep it and change the comment to say it now covers only a
genuinely missing background — **do not leave a comment claiming a race that is fixed.**

### Deviation, and why — the test the plan asked for could not be written

The plan called for "two concurrent `SetupFiles()` on one `GameFiles`, assert one
population and both callers seeing populated `Uri`s", needing only a hand-written `IGame`
fake. **The `IGame` fake was never the obstacle.** `SetupFiles` reaches
`DirectoryInfoHelper.Instance`, whose `ApplicationPath` is derived from
`Process.GetCurrentProcess().MainModule.FileName` — in a test run, the test host's own
directory — and `DisplayInfoHelper`, which reads the screen. A test would resolve paths
relative to the runner and write cache files next to the test binaries. No fake fixes that;
`B-09` does, and `B-09` is out of scope here.

So the concurrency invariant was extracted to where it *can* be tested, as
`Helpers/RunOnce.cs`: one piece of work, run once, with every caller waiting for it.
`GameFiles` holds one and its body becomes `ResolveAllFiles`. Roughly forty lines, no
LaunchBox coupling, and it names the invariant that was previously implicit in a bool.

**The tests were checked for teeth.** `RunOnce` was temporarily reverted to model the old
flag-before-work design — flag set before the work, later callers handed an
already-completed task — and two tests failed against it:
`Second_caller_arriving_mid_run_waits_for_the_work_to_finish` and
`Work_that_throws_is_attempted_once_and_the_failure_reaches_the_caller`. Restored, all 78
pass. A test that has not been seen to fail is not evidence of anything.

### Error semantics, stated rather than inherited

The old code set `IsSetup` before the work, so a game whose resolution threw was marked
done and never retried, and the exception aborted the rest of the pump's pass — the
next-list and any-game passes and the `CallGameChangeFunction` were all skipped.

`ResolveAllFiles` now catches and logs, so: the game is still marked resolved and still not
retried (same as before), the failure is still logged once (same as before), but **the rest
of the pump's pass now continues**. `RunOnce` itself stays faithful and propagates; the
decision to swallow belongs to `GameFiles`, which is the thing that knows a missing logo is
not worth stopping for.

**Files:** `Helpers/RunOnce.cs` (new), `Models/GameFiles.cs`,
`Service/AttractModeSlideshow.cs`, `Eclipse.Tests/RunOnceTests.cs` (new).
`View/MainWindowViewModel.cs` was untouched — `NeedsSetup` reads `IsSetup`, which is now a
computed property over `RunOnce.HasCompleted` and needed no call-site change.
**Verification:** the six `RunOnceTests`. Manual: a soak on a large library with the screen
saver enabled, watching for a slide that shows the placeholder background for a game that
has its own — the symptom this fixes.

---

## Stage A3 — measure the crop before rewriting it

`S-7` says per-pixel cropping "dominates first-run startup". That was true when it was
written and is **only partly true now**, because the work moved:

| Where crop runs today | When | Thread |
|---|---|---|
| `GameCatalog.PrescaleImages` → platform clear logos | startup, first run only | startup thread, blocking |
| `GameFiles.ResolveClearLogoPath` → game clear logos | during hydration, first sight of each game | pump, `BelowNormal` priority |

The existing measurement in `GameFiles.SetupFiles` — 12.6 s over 1,474 games, of which
99.3% is LaunchBox's own path properties and "everything Eclipse itself does comes to
48 ms" — was taken on a **warm cache**, where no crop or scale runs at all. It says
nothing about first run.

### What the A1 cold start already told us

`VER-MEDIA-009` rebuilt the whole cache from empty in **about 8 minutes** on the 1,469 game
library, with browsing smooth throughout — the pump is `BelowNormal` and the row decoder
stays ahead of the window, so this is background cost, not stall.

Against the warm baseline recorded in `GameFiles.SetupFiles` (12.6 s over 1,474 games,
≈ 8.5 ms per game, of which 99.3% is LaunchBox's own path properties), the arithmetic is:

| | Per game | Over the library |
|---|---|---|
| Warm — LaunchBox path properties, no image work | ≈ 8.5 ms | 12.6 s |
| **Cold — the same, plus scale and crop** | **≈ 325 ms** | **≈ 8 min** |

So image preparation costs roughly **320 ms per game**, nearly forty times what everything
else in hydration costs put together. **`S-7` was right that there is a real first-run cost
— it was only wrong about where it lands** (hydration, not startup). This is worth
attacking.

### What A3 still has to establish

The 320 ms covers **four** operations per game, and the plan must not assume which one
dominates:

| Operation | Where | Cost shape |
|---|---|---|
| Front box art scale | `ScaleImage` | one bicubic resize |
| Back box art scale | `ScaleImage` | one bicubic resize |
| Clear logo normalise | `CropImage` → `ResizeImage` at *original* size | one bicubic redraw, full size |
| Clear logo bounds scan | `Crop` | `GetPixel` per pixel |

**If the two box art scales dominate, rewriting `Crop` buys almost nothing** — and `B-29`
is the wrong thing to do next. The `GetPixel` scan is the obvious suspect, but it is a
suspect, not a finding: a full-size bicubic redraw of a large PNG is not cheap either, and
there are three resizes to its one scan.

### The measurement — taken 2026-08-26, cold cache, 3,750 images

Cumulative totals at the end of the run. 2,390 scales (front and back box art) and 1,360
crops (clear logos) across the 1,469 game library.

| | Load | Resize / normalise | **Scan** | Draw | Save | Total | Avg |
|---|---|---|---|---|---|---|---|
| **Scale** ×2,390 | **104.7 s** | 20.8 s | — | — | 19.8 s | 145.4 s | 60.8 ms |
| **Crop** ×1,360 | 15.1 s | 19.5 s | **24.5 s** | 6.8 s | 34.4 s | 100.3 s | 73.7 ms |

Rolled up across all 245.6 s of image work:

| Phase | Share | What it is |
|---|---|---|
| **Reading and decoding source files** | **48.8%** | `Image.FromFile` |
| **Encoding and writing outputs** | **22.1%** | `Bitmap.Save` |
| Resizing and normalising | 16.4% | `ResizeImage`, bicubic |
| **The `GetPixel` scan (`S-7`)** | **10.0%** | `Crop`'s border search |
| Cropped draw | 2.8% | `DrawImage` |

### `B-29` is not worth doing, and this closes it

**71% of the cost is file I/O and codecs.** Rewriting `Crop`'s scan with `LockBits` —
assuming it went to *zero* — would save 24.5 s of a 245 s job: about **25 seconds off an
eight-minute background task that the user does not wait for**, once, on first run.

`S-7` named the per-pixel scan as the problem. Measured, it is a tenth of the work, and
the four-fifths that matter are inherent to what the feature does: read every image in the
library once, and write a scaled copy. There is no algorithmic win here to take.

**`B-29`'s performance justification is therefore withdrawn**, and it joins `B-07` and
`B-31` as a backlog item closed by measurement rather than by work. What survives is the
**correctness** half, which was never about speed: the retained transparent row and column
are a defect, and A4 corrects them. **A4 loses the `LockBits` rewrite entirely.**

### Two corrections to things stated earlier in this plan

* **"`Image.FromFile` is lazy, so `load` will look near-zero and the decode gets billed to
  the resize."** Wrong, and backwards: load is the single largest line item at 48.8%. The
  caveat was offered confidently and the data contradicts it flatly.
* **"≈ 320 ms per game."** That was 8 minutes divided by 1,469 games, which silently
  charged the whole wall clock to image work. Measured directly it is **≈ 167 ms per game**
  of actual image processing (1.63 scales and 0.93 crops per game). The rest of the eight
  minutes is pump scheduling, `BelowNormal` priority and the decode gate — not this code.

### One thing the data suggests and this plan is *not* taking

`CropImage` normalises through a full-size bicubic redraw (19.5 s, 8% of total) purely to
force 32-bit ARGB so the alpha reads mean anything. For a clear logo already in 32bpp ARGB
— which most PNGs are — that redraw is waste and could be skipped.

**Recommended against.** It is 8% of a background job, and the risk is a silent change in
crop output for indexed or 24-bit sources, which is exactly the class of change `D1` shows
is expensive to make. Recorded here so the option is not lost, not as a proposal.

The rewrite in A4 happens regardless — decision 3 makes it a correctness change, not only
a performance one — but this measurement decides **how far** it goes:

* if the crop is a visible share of first run → replace the `GetPixel` scan with a
  `LockBits` scan over the alpha channel;
* if it is not → correct the bounds in place and leave the scan alone.

**Answered: the second.** See the measurement below.

Remove the instrumentation in the same stage. `B-28` and `B-34` are the lesson:
measurement scaffolding that outlives its question becomes a second thing to delete later.

**Files:** `Service/ImageScaler.cs` (temporarily), this document.
**Verification:** none — the code returns to its previous state.

---

## Stage A4 — the crop bounds correction (`S-7`, `B-29`)

**Reduced by A3.** This was "the crop rewrite". The `LockBits` scan is withdrawn — the
scan is 10% of image work and the rewrite would save ~25 s of an eight-minute background
job. What is left is the bounds defect, the failure-path disposal residual from `M-2`, and
the fixtures that pin both. `Crop`'s algorithm is otherwise untouched.

### What the current code does

`ImageScaler.Crop` (`Service/ImageScaler.cs:479`) finds the transparent border by scanning
rows and columns for `Color.A != 0`, then draws the interior into a new bitmap. Two things
about it are load-bearing and must survive:

1. **The input is always the output of `ResizeImage(original, originalWidth,
   originalHeight)`** — a same-size redraw whose purpose is not resizing. It normalises
   indexed and 24-bit sources into 32-bit ARGB so that reading `.A` means anything. A
   rewrite that reads the source file directly changes the alpha semantics for every
   non-ARGB logo. Keep the normalisation.
2. **`Save(string)` infers the encoder from the file extension.** Keep the destination
   naming exactly as it is.

### The defect being corrected

```csharp
int topmost = 0;
for (int row = 0; row < h; ++row)
{
    if (allWhiteRow(row)) topmost = row;   // last transparent row, not first opaque one
    else break;
}
```

`topmost` ends on the **last fully transparent row**, so the crop begins one row inside the
border and one transparent row survives. The same applies to `leftmost`. The bottom and
right edges do not do this — their loops assign an exclusive bound. The result is a
one-pixel asymmetry on every logo that has a transparent border at all.

Per decision 3 this is corrected: `topmost` and `leftmost` become the first opaque row and
column.

### The consequence, which is decision D1

Cropped output changes by one pixel on two edges. The cache is keyed by path only
(`RULE-MEDIA-002`, `OQ-010`) and is never invalidated, so on an existing installation:

* logos already cached keep the old bounds forever;
* logos cropped after the upgrade get the new bounds;
* the library is quietly inconsistent, and no user action suggests itself.

**D1 answered: D1-b — do nothing in code, document it.** The recommendation was D1-a
(a cache-format marker and a targeted sweep) and it was not taken; the concern was raised,
considered and declined, which makes it settled. Deleting the `Clear Logo` folders by hand
regenerates them, and on the measured library that is ~1m 45s of background work.

The options as they stood:

| Option | What it means |
|---|---|
| D1-a — targeted invalidation | A cache-format marker file in `MediaResolutionSpecificFolder`; on startup, if absent or older than the current format number, delete every `Clear Logo` directory beneath `…\{W}x{H}\Images` and let them regenerate lazily. Bounded, one-time, no user action. Costs a version→artefacts mapping that has to be kept honest. |
| **D1-b — do nothing, document it. CHOSEN.** | Cheapest, and no new startup path to get wrong. The cost is that an existing installation stays mixed: logos cached before the fix keep the extra row and column, logos cached after it do not. |
| D1-c — do not correct the bounds | Reverses decision 3. Output stays byte-identical and the asymmetry is documented as a rule instead. |

**Where it is documented matters.** Eclipse ships to other people, so this went into
`README.md` under known limitations — naming the folder to delete — rather than only into
`docs/`. Someone who notices a one-pixel difference between two logos needs something to
find, and an internal plan is not that.

If the mixed cache ever becomes a real complaint, D1-a is still available, and A3's data
makes it cheap to size: 19 folders and 1,434 logos on the reference library.

### Verification

Because the output deliberately changes, byte equality against today's output is not the
test. Instead:

* **Fixture set** in `Eclipse.Tests`: a transparent-bordered logo, a logo with no border,
  a fully transparent image, a 1×1 image, a non-ARGB (indexed PNG) logo, and a logo with a
  border on one side only.
* **Bounds assertions** per fixture: the exact expected crop rectangle, computed by hand
  and written into the test, not derived from the implementation.
* ~~**Equivalence between old and new scan**~~ — not needed. The scan is unchanged, so
  there is nothing to prove equivalent. `VER-MEDIA-011` is withdrawn with the `LockBits`
  rewrite.
* **The failure-path disposal residual** from `M-2` is fixed here, since it is in the two
  methods this stage already touches: `ResizeImage` and `Crop` each construct their result
  before drawing into it and leak it if the draw throws.

**Files:** `Service/ImageScaler.cs`, `Eclipse.Tests/*` (new fixtures and tests), and for
D1-a `Helpers/DirectoryInfoHelper.cs` and `Service/GameCatalog.cs`.
**Verification:** new `VER-MEDIA-010`.

---

## Stage A5 — consolidate the bezel chain (`B-20`)

`RULE-MEDIA-020` names five levels. They are resolved in three places:

| Level | Where |
|---|---|
| 1 game-specific | `Models/GameFiles.ResolveBezelPath` |
| 2 MAME artwork | `Models/GameFiles`, with `Helpers/RetroarchHelper` alongside |
| 3 RetroArch overlay | same |
| 4 platform default | `Service/BezelService.GetDefaultBezel`, called **from the view** |
| 5 global default | `Service/BezelService.GetDefaultBezel` fallback |

Levels 4 and 5 are resolved late and in the view because they depend on the video's natural
dimensions, which are not known until `MediaOpened` (`RULE-MEDIA-021`). That is a real
constraint and the consolidation must respect it — the point is not to resolve everything
early, it is that **the view should not be the thing that knows the rule.**

**Enumerate first.** `B-20`'s own note says so and it is right: write the five levels out
as a table in `media.md`, with the exact predicate for each, before changing any of them.

**The change:** one entry point on `BezelService`:

```csharp
Uri ResolveBezel(GameFiles gameFiles, IPlatform platform, int videoWidth, int videoHeight);
```

which internally applies, in order: the already-resolved game bezel; then, only if the
video's aspect ratio is under 1.7 and its height is non-zero, the platform default and then
the global default, choosing orientation from the video's dimensions.

Two details to carry across **exactly**:

* the ratio test is `(float)width / (float)height < 1.7` — float division, compared against
  a `double` literal;
* the guard is `NaturalVideoHeight != 0`, and a zero height means **no bezel**, not a
  default one.

The opacity-mask swap (`RULE-MEDIA-026` — mask on the bezel when there is one, on the video
when there is not) stays in the view. It is a property assignment on two named elements,
which is what a view is for. It becomes one line in Part B's presenter.

### What the enumeration turned up

Writing the five levels out before touching them was the right instruction, and it paid:

* **`RULE-MEDIA-024` was wrong.** It read as though a widescreen video never gets a bezel.
  The ratio test sits *inside* the "no game bezel" branch, so a game with its own bezel is
  framed with it at any aspect ratio. The rule is corrected and there is a test named after
  the correction.
* **`RULE-MEDIA-027` is new.** Level 1 matches by wildcard extension and takes the first
  result `Directory.GetFiles` returns, so two bezels for one game differing only in
  extension resolve unpredictably. Recorded, not changed.
* **Levels 4 and 5 are a snapshot.** The dictionary is built in `BezelService`'s
  constructor, so a bezel dropped into the folder later is invisible until Big Box
  restarts. Recorded, not changed.
* **The plan's own sketch was wrong** about the signature: it proposed `IPlatform platform`,
  but `IGame.Platform` is a `string` and that is what the lookup is keyed on.

### The split, and why it is not just one method

`ResolveBezel` lives on `BezelService`, but the *decision* — precedence, the widescreen
cutoff, the orientation — is in `Service/BezelRules.cs`, a static class with no filesystem
and no LaunchBox dependency.

The reason is the same one A2 ran into: `BezelService`'s constructor reads the bezel folder
and asks `PluginHelper.DataManager` for the platform list, so it cannot be constructed in a
test. Its dictionary lookup is trivial; its *rules* are the part worth pinning. Separating
them is what makes twelve assertions possible instead of none.

The float comparison is preserved exactly — `(float)width / (float)height` compared against
the double `1.7` — rather than tidied into double arithmetic, because the widening is what
decides the borderline cases. There is a test either side of the cutoff.

### Equivalence

The old expression was reimplemented in a temporary test and swept against `BezelRules`
over every `hasGameBezel` × width × height combination on a grid to 4000×2400 — about
200,000 cases, all agreeing on source and orientation. The sweep was then deleted rather
than kept: it asserts that new code matches code that no longer exists, which stops being
meaningful the moment the rules are deliberately changed.

**Files:** `Service/BezelRules.cs` (new), `Service/BezelService.cs`,
`View/MainWindowView.xaml.cs`, `Eclipse.Tests/BezelRulesTests.cs` (new),
`docs/features/media.md`. `Models/GameFiles.cs` was **not** touched — levels 1–3 already
resolve during hydration and needed no change.
**Verification:** twelve `BezelRulesTests`; the equivalence sweep above. Manual
`VER-MEDIA-001` across the five levels is still wanted and is the one thing here a test
cannot do — it checks that the *paths* are right, not that the choice between them is.

### Deliberately left for Part B

`Video_SelectedGame_MediaOpened` still calls `new BitmapImage(gameBezelUri)`, which decodes
synchronously on the UI thread — the one image in the product that does not go through
`FrozenImageLoader`. Fixing it means making the handler async, which belongs with B4's
sequence work rather than here. Recorded so it is not lost.

---

# Part B — the presentation seam

Part B follows the shape attract mode already proved: an **interface** for what the screen
must do, a **sequence** object that owns the ordering, and a **timings** object that owns
every duration. `IAttractModePresenter` / `AttractModeSlideshow` / `AttractModeTimings` is
the template, and it should be followed closely enough that a reader of one recognises the
other.

## Stage B1 — `SelectionTimings`, and the settings behind it (`C-5`, `OQ-007`)

Per decision 2 these become settings rather than named constants.

### The values, and where they are today

| Value | Today | Becomes |
|---|---|---|
| Settle delay | `SettleDelayMilliseconds = 1000` (`:26`) | `SelectionSettleMilliseconds`, default 1000 |
| Dim duration | literal `25`, four call sites in `DimBackground` and `DoAnimateGameChange` | `SelectionDimMilliseconds`, default 25 |
| Background fade in | `BackgroundFadeInMilliseconds = 500` (`:27`) | `SelectionBackgroundFadeInMilliseconds`, default 500 |
| Background fade out | `BackgroundFadeOutMilliseconds = 1000` (`:28`) | `SelectionBackgroundFadeOutMilliseconds`, default 1000 |
| Detail fade in | literal `500`, four call sites in `FadeInCurrentGameDetails` and two in `FadeInBackgroundImages` | `SelectionDetailsFadeInMilliseconds`, default 500 |
| Video delay | `VideoDelayInMilliseconds` | unchanged — already a setting, joins `SelectionTimings` |

Every default is the value in the code today, so an existing `settings.json` — which will
not contain the new keys — behaves exactly as it does now. This is the same guarantee the
screen-saver settings gave, by the same mechanism: `[DefaultValue]` plus
`DefaultValueHandling.Populate`.

**Not becoming settings** — and this is **decision D3**, because the question that
authorised this stage listed them: `RecoveryFailureThreshold = 3`,
`AbandonFailureThreshold = 10` and `PlaybackStartCheckMilliseconds = 1500`. These are
diagnostics for a media-stack failure, not presentation timing; a user has no way to judge
a good value and a wrong one silently disables video previews. Recommendation: they become
named constants on the policy object in Stage B2 and stay out of the settings window.
**Confirm before implementing.**

Also not becoming settings: the dim *opacities* (0.25 background, 0.15 logo and details, 0
title). They are look, not timing, and `RULE-PRESENT-003` depends on the relationship
between two of them. They become named constants.

### The UI — decision D2

The screen saver's ten timings got their own tab. These five are siblings of **Video
delay**, which today sits on **Other settings** alongside *Disable videos*, *Default video
volume* and the six `Show …` toggles.

| Option | What it means |
|---|---|
| **D2-a (recommended)** | New **Presentation** tab holding the five new sliders, and move the existing *Video delay* row onto it so the whole sequence is in one place. Moving one row is a relocation, not a semantic change. |
| D2-b | New **Presentation** tab with only the new sliders; *Video delay* stays on *Other settings*. Nothing moves, but the sequence is described in two places. |
| D2-c | Add the five sliders to *Other settings*. No new tab; that tab is already the longest. |

Whichever is chosen, the mechanics are settled: a `const` on `EclipseSettingsTabs`, an
entry in `InitializeTabPages`, a `DataTemplate` plus a `DataTrigger`, and `local:SliderRow`
rows with `Unit="ms"` — exactly as `ScreenSaverTab` does it.

Give each slider a tooltip that says what the user will see, in the register the screen
saver's tooltips use. The settle delay in particular needs one: "How long the selection has
to be still before the game's artwork and details are loaded and shown. Longer means less
happens while you scroll."

### What actually landed

**D2 answered as D2-a**, **D3 answered "no"** — the two failure thresholds and the 1500 ms
playback check stay as named constants. The reasoning is recorded because it is the more
interesting half: they are *diagnostics*, not presentation. Nothing in the UI reports that
recovery fired or that previews were abandoned — it is log-only — so a user cannot observe
the effect of changing them, which makes them untunable in practice. And the failure modes
are silent: "give up after 1" disables previews for the session on a single slow file, with
no visible cause. Constants is also the reversible choice; exposing them later is additive,
un-shipping a setting is not.

**One mapping decision worth recording.** The literal `500` appeared in two places: three
times in `FadeInCurrentGameDetails` (logo, title, details grid) and twice in
`FadeInBackgroundImages` (the background returning after a video ends). They are now two
*different* settings — `SelectionDetailsFadeInMilliseconds` and
`SelectionBackgroundFadeInMilliseconds` — because they are two different visual events that
merely happened to share a number. `SelectionBackgroundFadeInMilliseconds` therefore drives
both background fade-ins, the game change and the post-video return, which are the same
event seen twice. No value changed; two previously independent literals are now coupled,
and that is deliberate.

**The video delay is the one value not clamped.** Every other duration falls back to its
default if negative, because `Task.Delay` throws on one and these reach `Task.Delay`. Zero
or less is a *supported* configuration for the video delay (`RULE-MEDIA-031`, no pause
before the video), so a negative has to survive to the caller that tests for it. That
asymmetry is tested.

**Files:** `Models/EclipseSettings.cs`, `Service/SelectionTimings.cs` (new),
`View/EclipseSettings/EclipseSettingsViewModel.cs`,
`View/EclipseSettings/EclipseSettingsView.xaml`, `View/MainWindowView.xaml.cs`,
`Eclipse.Tests/SelectionTimingsTests.cs` (new).
**Verification:** nine `SelectionTimingsTests`. The one that matters is
`A_settings_file_written_before_these_existed_keeps_the_old_behaviour` — it deserialises a
settings file with none of the new keys and asserts the sequence still runs at 1000/25/500/
1000/500, which is the whole safety claim of this stage. Checked for teeth: changing one
`[DefaultValue]` to 999 fails it and one other. `VER-CONFIG-022` and `VER-PRESENT-007`
remain manual — change a value, save, restart Big Box, watch.

## Stage B2 — the video failure policy

`ApplyFailurePolicy` (`:334`) is the one piece of this file that is pure decision-making:
given a consecutive-failure count and whether the session has already been abandoned,
decide between *do nothing*, *recover* and *abandon*. It has three branches, two thresholds
and no view dependency except the two lines that actually close the player.

Extract the decision — not the effect:

```csharp
public enum VideoFailureAction { None, Recover, Abandon }

public sealed class VideoFailurePolicy
{
    public VideoFailureAction Decide(int consecutiveFailures, bool alreadyAbandoned);
}
```

The view keeps the two lines that call `Close()` and null the `Source`.
`VideoPlaybackMonitor` keeps the counting it already does well.

This is the cheapest genuine test in Part B — a table of counts against expected actions,
including the boundaries at 2/3 and 9/10 — and it is worth doing on its own, before the
larger extraction, because it establishes what a test of this subsystem looks like.

**Files:** `Service/VideoFailurePolicy.cs` (new), `View/MainWindowView.xaml.cs`,
`Eclipse.Tests/VideoFailurePolicyTests.cs` (new).
**Verification:** unit tests; `VER-MEDIA-007` unchanged.

## Stage B3 — `ISelectedGamePresenter`

Write the interface **from the calls the sequence actually makes**, the way
`IAttractModePresenter` was written — a handful of calls, nothing else reachable.

Draft:

```csharp
public interface ISelectedGamePresenter
{
    /// Dim the outgoing game: background, logo, title and details.
    void DimForGameChange();

    /// Put the settled game's decoded artwork and text on screen and fade them in.
    void ShowGameDetails(SelectedGameMedia media);

    /// Fade the new background in over the outgoing one.
    void FadeInBackground();

    /// The background has finished fading in - hand it to the displayed layer at full opacity.
    void SettleBackground();

    /// Hand a file to the player, or clear it when the path is null.
    void OpenVideo(string videoPath);

    /// Start playback and fade the background out behind it.
    void PlayVideo();

    /// Stop playback and animations and put the selected game's artwork back on screen.
    void StopAndRestore();
}
```

`SelectedGameMedia` is a small frozen-image-and-text record: the five `ImageSource`s and
the three strings the view currently holds in its `active*` fields. Building it moves to
the sequence; the fields disappear from the view.

`MainWindowView` implements it. Everything left in the code-behind after Part B is element
manipulation: setting `Source`, setting `Text`, calling `BeginAnimation`, assigning
`OpacityMask`. That is the correct residue and this plan does not try to remove it.

**Files:** `Service/ISelectedGamePresenter.cs` (new), `Models/SelectedGameMedia.cs` (new),
`View/MainWindowView.xaml.cs`.
**Verification:** build only — this stage introduces the interface and makes the view
implement it, without moving the sequence yet.

## Stage B4 — `SelectedGameSequence`

Move `ShowSelectedGameAsync`, `LoadSettledGameMediaAsync` and `PlaySettledGameVideoAsync`
out of the view into one class, keeping the current structure exactly: one
`CancellationTokenSource` per selection, exchanged atomically, the previous one cancelled;
`OperationCanceledException` swallowed; everything else logged.

**Make it testable deliberately.** The sequence is a chain of `Task.Delay` calls, which is
untestable in a suite that has to run in under a second. Inject the wait:

```csharp
public SelectedGameSequence(
    ISelectedGamePresenter presenter,
    SelectionTimings timings,
    Func<SelectedGameMedia> captureSelection,
    Func<Uri, Task<ImageSource>> loadImage,
    Func<TimeSpan, CancellationToken, Task> delay)
```

In production `delay` is `Task.Delay`. In tests it records the requested duration and
returns completed — which turns "did the fades happen in the right order, for the right
durations, and did the video wait for the settle" into an assertion over a recorded call
log. That is the specification `presentation.md` says exists nowhere except the code
(`RULE-PRESENT-001`, `RULE-PRESENT-002`), written down in a form that fails when it stops
being true.

Tests to write here, at minimum:

| Test | Asserts |
|---|---|
| ordinary change | dim → settle wait → load → open → details fade → background fade in → settle → video delay → play |
| cancelled mid-settle | nothing after the settle runs; no presenter calls beyond the dim |
| zero video delay | the background fade-in step is skipped entirely (`RULE-MEDIA-031`) |
| game with no video | sequence ends after `SettleBackground`, and the attract timer is restarted (`RULE-MEDIA-030`, and the bug the current comment describes) |
| videos disabled | no `OpenVideo`; attract timer restarted |

### The tests were mutation-tested, and one mutation survived

Passing on the first run is not evidence, so three deliberate breaks were made to the
sequence to see whether the suite noticed:

| Mutation | Caught? |
|---|---|
| Drop the video delay wait | **Yes** — the durations test |
| Swap `FadeInBackground` and `SettleBackground` | **Yes** — two ordering tests |
| Delete `ThrowIfCancellationRequested` after the load | **No.** |

The third is not cosmetic. It is the guard that stops a scrolled-past game's artwork being
painted over the game the user actually moved to, and on a cold cache the decode is the slow
part — exactly when the window is widest. A fourteenth test now covers it.

**The first attempt at that test also failed to catch the mutation**, which was the more
useful finding. The helper that waited for the sequence used `Task.Yield` in a loop, which
only requeues the *test* on the thread pool — it does not give a released continuation chain
elsewhere time to run. The sequence had genuinely misbehaved; the assertion just ran before
the misbehaviour arrived. The helper now uses real delays, and the negative assertion waits
for the wrong behaviour to appear before concluding it did not.

**Generalisable:** a test that asserts something must *not* happen is worthless unless it
waits long enough that it would have happened. Yielding is not waiting.

### One regression the compiler caught

Moving the sequence out left `activeVideoPath` unassigned — `OpenVideo` had never needed to
record it, because the old code captured it separately at dim time. `CanPlayVideo` reads it,
so it would have been permanently false and **no video would ever have played again**. A
`CS0649` warning on an unused private field is what surfaced it. Worth noting because it is
precisely the class of silent behavioural loss this plan's ordering was designed to avoid,
and no test would have caught it — nothing in the suite exercises the real view.

**Files:** `Service/SelectedGameSequence.cs` (new), `Service/ISelectedGamePresenter.cs` (new),
`Service/IAttractModeTimer.cs` (new), `Models/SelectedGameMedia.cs` (new),
`Service/AttractModeService.cs` (implements the new interface),
`View/MainWindowView.xaml.cs`, `Eclipse.Tests/SelectedGameSequenceTests.cs` (new).
**Verification:** fourteen `SelectedGameSequenceTests`, plus `VER-PRESENT-001`,
`VER-PRESENT-002` and `VER-MEDIA-007` manually — the tests prove ordering, only a person
proves it looks right.

## Stage B5 — the view delegates and the bezel call (`B-19`, `M-8` share)

Two couplings are left.

**The delegates.** `MainWindowViewModel` declares `AnimateGameChangeFunction` and
`StopVideoAndAnimations` (`:18,20`), holds them as settable properties (`:416,417`), and
the view assigns itself into them in its constructor (`:105,108`). With the sequence
extracted, both become calls on `SelectedGameSequence` — the view model asks the sequence
to start or stop, and the sequence talks to the presenter. Delete the two delegate types,
the two properties and `CallStopVideoAndAnimationsFunction`; update the one other caller,
`State/VoiceRecognitionState.cs:132`.

**The bezel.** Delete the orientation and aspect-ratio logic from
`Video_SelectedGame_MediaOpened` and call Stage A5's `BezelService.ResolveBezel`. The
handler keeps: recording the open on the monitor, setting `videoIsOpen`, starting the
playback check, assigning `Image_Bezel.Source`, and the opacity-mask swap.

**Explicitly out of scope:** `AttractModeService.MainWindowViewModel`
(`Service/AttractModeService.cs:10`). It is the other half of `M-8`, and it is a view-model
reference rather than a view reference — a different problem, belonging to whatever
addresses `B-09`. Do not touch it here, and do not describe `M-8` as closed when this plan
finishes.

**Files:** `View/MainWindowViewModel.cs`, `View/MainWindowView.xaml.cs`,
`State/VoiceRecognitionState.cs`.
**Verification:** `VER-PRESENT-001`; `VER-SEARCH-002` (voice search stops the video);
`VER-LAUNCH-001` (launching stops it).

## Stage B6 — documentation

Update the `presentation.md` and `media.md` implementation maps and debt tables; add the
new verification scenarios to `VERIFICATION.md`; update `TRACEABILITY.md` for `B-05`,
`B-19`, `B-20`, `B-29`, `B-34` and the presentation share of `S-4`; close `OQ-007` with the
answer decision 2 gave it; record the A3 measurement.

---

# Decisions still open

These are in the plan because they must be answered by a person, not chosen by whoever
implements the stage. Each names the stage it blocks.

| # | Blocks | Question | Recommendation |
|---|---|---|---|
| **D1** | A4 | Existing installs hold clear logos cropped with the old bounds and nothing regenerates them. Targeted invalidation, do nothing, or don't correct the bounds after all? | **D1-a** — a cache-format marker and a one-time sweep of the `Clear logo` folders only |
| **D2** | B1 | Where do the five new sliders live — a new **Presentation** tab with *Video delay* moved onto it, a new tab without moving it, or *Other settings*? | **D2-a** |
| **D3** | B1/B2 | Do the two video failure thresholds and the 1500 ms playback check become settings too? The question that authorised B1 listed them. | **No** — named constants on `VideoFailurePolicy`; a wrong value silently kills video previews |

---

# Verification added by this plan

| ID | Scenario | Type |
|---|---|---|
| VER-MEDIA-009 | Delete the resolution-specific media folder and start Big Box. Artwork populates, the box row fills, clear logos appear. Repeat on a warm cache and confirm nothing is regenerated. | Manual |
| VER-MEDIA-010 | Crop bounds, per fixture: transparent border, no border, fully transparent, 1×1, indexed PNG, one-sided border. Exact expected rectangle asserted. | **Unit** |
| ~~VER-MEDIA-011~~ | ~~Old and new scans detect the same bounds for every fixture.~~ **Withdrawn** — the scan is not being rewritten. |  |
| VER-MEDIA-012 | Bezel resolution across all five levels, plus aspect ratios 1.69/1.70/1.71, zero height and square video. **Partly automated** — the choice between levels is twelve unit tests over `BezelRules`; that the paths themselves are right is still manual. | **Unit** + manual |
| VER-PRESENT-007 | Change each of the five new timing settings, save, restart, and confirm the corresponding fade or delay changes and nothing else does. | Manual |
| VER-PRESENT-008 | Sequence ordering, cancellation, zero video delay, no video, videos disabled. | **Unit** |
| VER-CONFIG-022 | Open the settings window with a `settings.json` written before this change. The five new sliders show their defaults; save; the file gains the keys. | Manual |

Numbering continues from the current maxima: `VER-MEDIA-008`, `VER-PRESENT-006`,
`VER-CONFIG-021`.

---

# Risks

| Risk | Where | Mitigation |
|---|---|---|
| The crop change makes existing caches inconsistent | A4 | D1. This is the largest user-visible risk in the plan, and it is a decision rather than an accident |
| Removing the startup enumerations removes a folder-creation side effect | A1 | A cold-cache start is part of the stage's verification, not an afterthought |
| The hydration change alters pump timing | A2 | The pump's loop guard is re-read as part of the stage; soak on a large library before moving on |
| Fade timing regresses invisibly | B4 | The injected-delay tests pin ordering and durations; the manual sweep pins the feel. Neither alone is enough |
| New settings widen the surface that must keep working | B1 | Every default is the current constant, so an existing install is unaffected until the user changes something |
| Part B ends with the view still holding named elements | B3–B5 | Intended. The goal is that the view holds *only* element manipulation |

---

# What this plan deliberately does not do

* **`B-09` / the composition root.** Both parts add constructor-injected objects, which
  makes it easier later. Neither does it.
* **`B-31` / the 13-slot window.** Answered and closed by measurement — see
  `TRACEABILITY.md`.
* **`B-32`, `B-33`, `C-1`, `C-3`.** `PropertyChanged` boilerplate and converters. Adjacent
  to Part B, unrelated to it.
* **`OQ-010` / cache invalidation in general.** D1-a introduces a marker for one format
  change. It is not a general invalidation scheme and must not be described as one.
* **The `AttractModeService.MainWindowViewModel` half of `M-8`.**
* **Anything about how the crop or the fades *should* look.** Two behaviour changes are
  authorised here and only two: the crop bounds correction, and the five values becoming
  settings. Everything else preserves what Eclipse does today.
