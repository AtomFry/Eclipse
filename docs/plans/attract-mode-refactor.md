# Plan — Attract mode: settings, technical debt and correctness

**Status: complete, pending a manual verification sweep of Stages 5 and 6.**

## Context

Attract mode is the idle screensaver: after a configurable delay Eclipse fades to black and
cycles random games, each with a slow background pan and a clear-logo fade. The feature
worked, but it carried the worst layering violation in the product and a set of real defects.

`docs/features/attract-mode.md` recorded four findings — `M-8` (both the service *and* the
state held a concrete `MainWindowView`), `S-4` (the visual sequence lived entirely in view
code-behind), `M-4` (three timers never disposed), `C-5` (every timing an unexplained
literal). Reading the code turned up nine more, several of which could crash or stall the
host.

**Scope decision (product owner):** a *technical debt exercise*. Refine, don't redesign — a
user should not notice a different screensaver. Feature and look-and-feel ideas were written
down for later rather than built; they live in
[attract-mode-enhancements.md](attract-mode-enhancements.md). **The timings came out of the
code first**, so nothing that followed depended on hardcoded values.

---

## Outcome

All four documented findings are closed for this epic, and all fourteen defects are fixed.

| Concern | Where it lives now |
|---|---|
| Idle timer, entry, and the running slideshow | `Service/AttractModeService.cs` |
| The sequence | `Service/AttractModeSlideshow.cs` — one `async` loop |
| Durations | `Service/AttractModeTimings.cs` — from settings, as `TimeSpan`s |
| The seam | `Service/IAttractModePresenter.cs` — five calls |
| Image decoding | `Service/AttractModeImageLoader.cs` — off-thread, frozen |
| Visuals | `View/AttractModeView.xaml(.cs)` — the only presenter implementation |
| Entry/exit state | `State/AttractModeState.cs` — remembers where to return to |

`MainWindowView` is out of the attract mode path entirely, apart from hosting the control and
handing the service its presenter.

---

## Defects found and fixed

| # | Defect | Resolution |
|---|---|---|
| 1 | `NextAttractModeGame()` called **twice per slide**, on two threads | The view's call is gone; selection happens once, in the slideshow. The non-thread-safe `static Random` is no longer touched from a timer thread. |
| 2 | No media-readiness guard — `BackgroundImage`/`ClearLogo` are null until `SetupFiles()` runs, and hydration is lazy, so a cold session could hand `null` to `new BitmapImage` and throw | The slideshow awaits `SetupFiles()` for the picked game before showing its slide, so it displays real artwork. A still-null background falls back to `ResourceImages.DefaultBackground`; a missing logo shows no logo. |
| 3 | `if (activeAttractModeBackgroundImage != null)` after `new BitmapImage` | Dead guards — `new` throws rather than returning null. Removed. |
| 4 | Blocking `Dispatcher.Invoke` from timer threads in all five presenter methods | `CheckAccess()` inline, `InvokeAsync` otherwise. The slideshow never waits on the UI thread. |
| 5 | `ShiftFrameworkElement` opened a **nested** `Dispatcher.Invoke` | Gone with the marshalling rework. |
| 6 | Full-size `BitmapImage` decoded synchronously on the UI thread, once per slide | `AttractModeImageLoader` decodes with `CacheOption.OnLoad` and `Freeze()`s on a background thread. The decode runs *concurrently with the hold on black*, so it costs the slide nothing. |
| 7 | The three `AttractModeState` timers never checked `IsPlayingGame` | The timers are gone; the loop checks `canContinue()` once per slide. |
| 8 | Exit did not stop running animations | `StopPan()` freezes the pan at its current offset. The three reset sites had been clearing `RenderTransform` on the background *image*, but the pan animates the inner *canvas* — so it had never had any effect. |
| 9 | `using System.Security.RightsManagement;` | Removed. |
| 10 | **Attract mode only started after a video preview finished.** A game with no video never triggered the screen saver. | `DoAnimateGameChange` called `StopEverything()`, which switches off the idle timer, on every game change — so only a video's `MediaEnded` ever re-armed it. It now re-arms the timer directly. |
| 11 | A video that **fails to load** never raises `MediaEnded` | Added a `MediaFailed` handler that logs, fades the backgrounds back in and restarts the idle timer. |
| 12 | **Attract background left black bars.** Two causes. | **(a)** The pan measured overhang as `GetMonitorWidth() - Image.Width`, mixing physical pixels with device-independent units — they agree only at 100% DPI. Now measured against the control's own `ActualWidth`. **(b)** `Image_AttractModeBackgroundImage` set `Width` but not `Height`, so `UniformToFill` derived height from the artwork's aspect ratio. Both now bind. |
| 13 | `StopAttractMode()` turned the visuals off but left the slideshow running | It stops the slideshow too. Previously any exit that was not through an input handler left the loop cycling invisibly until the next entry replaced it. |
| 14 | `Stop()` disposed the `CancellationTokenSource` while `RunAsync` might still use it | `Stop()` only cancels; `RunAsync` disposes in a `finally`. Otherwise an ordinary exit could log an `ObjectDisposedException` as a real error. |

---

## Stage 1 — Every timing into settings

Ten timing literals became user settings on a new **Screen saver** tab, with the two existing
screen saver settings moved there so the whole feature lives in one place. Defaults are the
values that were hardcoded, so an existing install behaves identically.

Stored as `int` milliseconds on `Models/EclipseSettings.cs`, displayed in seconds through
`MillisecondsToSecondsConverter`. Defaulting was already solved by the existing
`[DefaultValue(x)]` + `DefaultValueHandling.Populate` pattern, so no migration code was
needed. The full table is in
[../features/configuration.md](../features/configuration.md#screen-saver-slideshow-timings).

`Service/AttractModeTimings.cs` is the only consumer: it converts once and enforces the one
ordering constraint, clamping *time on each game* minus *logo delay* at zero.

**After Stage 1 there were no timing literals left in attract mode code.**

## Stage 2 — Extract the seam

`IAttractModePresenter` added; the service and state hold the interface rather than a
concrete `MainWindowView` (`M-8`).

**Deviation, deliberate:** rather than leaving the implementation in `MainWindowView`, the
visuals were lifted into their own `AttractModeView` user control. `MainWindowView` is the
largest file in the project and the attract mode markup was a self-contained grid inside it.
Moving it made the presenter implementation ~240 lines beside the markup it animates, which
is what actually closed `S-4` — an interface alone would have left the code-behind in place.

## Stage 3 — Replace the timers with an async sequencer

`Service/AttractModeSlideshow.cs` holds the sequence as one linear loop, so
`RULE-ATTRACT-004` is the body of `RunAsync` instead of three `System.Timers.Timer`
instances starting each other across two files. A `CancellationTokenSource` per run replaces
"stop three timers", closing `M-4`. The one remaining timer is the idle delay, which is
correct.

**Deviation, deliberate.** The plan called for running the loop on the UI thread so the
presenter could drop marshalling entirely. That needs a `Dispatcher` from somewhere, and the
only routes were leaking WPF into `IAttractModePresenter` or depending on
`Application.Current` being non-null inside a host plugin. The loop stays off the UI thread
and the presenter marshals through `CheckAccess()`/`InvokeAsync`, which closes #4 without the
interface knowing WPF exists.

## Stage 4 — Correctness

Brought forward ahead of Stage 3: these are the defects that actually affected the user, and
none of them needed the async rewrite. Defects #1, #3, #7, #8, #9 above, plus the first half
of #2.

## Stage 5 — Selection, hydration and decode off the UI thread

The remaining layering leak and the remaining performance defect were the same change. The
presenter used to pick the game, resolve its Uris and decode a full-size bitmap on the UI
thread. It now receives a decoded, frozen `ImageSource` and knows nothing about games:

```
ShowBackground(ImageSource image, bool slideLeft);
ShowLogo(ImageSource logo);          // null => no logo
```

Selection moved to the slideshow via a `Func<GameMatch>`; `NextAttractModeGame()` returns its
pick. Loading moved to `AttractModeImageLoader`. Both the background and the logo decode
**concurrently with the wait that precedes them** — the background during the hold on black,
the logo during the logo delay — so the 19 s cadence is unchanged. This closed #6 and the
remaining half of #2.

Also in this stage: slideshow ownership moved from `AttractModeState` to
`AttractModeService`, so `StopAttractMode()` genuinely stops it (#13), and the state is now
only "where do I return to, and end the run on input". `Presenter` and `MainWindowViewModel`
became dead properties on the state and were removed.

`GameFiles.SetupFiles()` sets `IsSetup = true` before it populates, so a game already being
hydrated elsewhere returns immediately with its Uris still null. The default-background
fallback is kept after the await for that reason. **That race belongs to `EPIC-MEDIA`** and
was deliberately not fixed here.

## Stage 6 — Documentation

`docs/features/attract-mode.md` — implementation table, `M-8`/`M-4`/`S-4`/`C-5` closed,
`B-18` marked delivered. `RULE-ATTRACT-004` now describes a configurable sequence;
`RULE-ATTRACT-005` was **describing defect #12(a)** and was corrected; `RULE-ATTRACT-009`
added for on-demand hydration. `docs/TRACEABILITY.md` — `EPIC-ATTRACT` struck from the four
debt rows and the two backlog rows. `docs/features/configuration.md` — the settings table and
the restart semantics. `docs/VERIFICATION.md` — `VER-ATTRACT-001` is writable today rather
than blocked on `B-18`; `VER-ATTRACT-004`/`005` added.
[attract-mode-enhancements.md](attract-mode-enhancements.md) — parked ideas.

---

## Verification

No golden-capture equivalent exists — this is visual and time-based, so it is a manual sweep.
Set **`ScreensaverDelayInSeconds` to ~10** to make it fast to exercise. Stages 1–4 have been
verified; the sweep below is what Stages 5 and 6 still need.

| Check | Expectation |
|---|---|
| **Cold session** (`VER-ATTRACT-004`) | Restart Big Box and trigger attract mode before hydration finishes. Games must show their **own** artwork, not the placeholder — this is the half of #2 Stage 5 closed |
| **Slide hitch** | Watch the moment the background appears, on 4K artwork. The synchronous-decode stutter should be gone |
| **Cadence unchanged** | Time three consecutive slides: ~19 s each at defaults, logo ~4 s after the background. Decode runs during the black hold, so it must not add to the total |
| Missing logo | Background alone, no exception |
| Missing background | Default background, no exception |
| `VER-ATTRACT-001` idle entry | Fades to black after the configured delay |
| `VER-ATTRACT-002` exit | Any key returns to the **exact** prior state, including mid-overlay and mid-decode |
| `RULE-ATTRACT-005` / `VER-ATTRACT-005` | Pan direction alternates, drifts *into* the screen, fills edge to edge. Worth one pass at non-100% display scaling, since #12(a) is invisible at 100% |
| `RULE-ATTRACT-006` / `007` | No attract mode while a game runs; a video preview stops it; **a game with no video still starts it** (#10) |
| A setting bites | Set *Pan across screen* to 5 s and *Time on each game* to 8 s; confirm the picture changes. Restart required |
| Bad combination | *Logo appears after* longer than *Time on each game* must clamp, not hang or throw |
| Soak | 10+ minutes idle: no stutter growth, no memory growth. `Freeze()` plus `OnLoad` should make this flatter than before |
| Log | Nothing logged during a normal session — in particular no `ObjectDisposedException` on exit (#14) |

## Out of scope

Visual or feature change — parked in
[attract-mode-enhancements.md](attract-mode-enhancements.md). Live-applying settings without
a restart (`OQ-011`). The `GameFiles.IsSetup` hydration race (`EPIC-MEDIA`). A test project
(`B-01`) — the presenter interface makes a recording fake feasible and the docs call that the
highest-value gap, but standing up the first test project in the codebase is its own piece of
work and was not bundled here.
