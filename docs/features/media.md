# EPIC-MEDIA — Media Resolution & Processing

**Scope.** Finding, caching and preparing the artwork and video associated with a game:
box art, clear logos, backgrounds, platform logos, rating imagery, play-mode icons,
videos and bezels. Owns the pre-scaled image cache. Does not own how media is laid out
(EPIC-PRESENT).

This is the most performance-sensitive epic and the one that touches the filesystem most.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### The media model

Each game has a set of resolved media locations. Resolution is **lazy** — a game's media
is resolved the first time it is needed, not at startup — and **cached to disk** at the
display resolution so scaling is not repeated on every launch.

### Features

**FEAT-MEDIA-001 — Box art.** Front and back box art are resolved from LaunchBox's image
folders. Two forms are kept: a pre-scaled version sized for the row, and the original for
enlarged display. A placeholder is used when a game has no art.

**FEAT-MEDIA-002 — Clear logo.** The game's clear logo, cropped to remove transparent
borders and cached.

**FEAT-MEDIA-003 — Background.** The game's background image, falling back to a
screenshot, falling back to a bundled default.

**FEAT-MEDIA-004 — Platform logo.** The platform's clear logo, cropped and cached.

**FEAT-MEDIA-005 — Star rating imagery.** Two independent images: the community rating
and the user's own rating, each selected by rounding the rating to one decimal and
looking up a matching bundled image.

**FEAT-MEDIA-006 — Play-mode icon.** An icon for the game's play mode, with a bundled
fallback icon.

**FEAT-MEDIA-007 — Video preview.** After the selected game settles, its video (if any)
plays, the background fades out behind it, and when the video ends the background fades
back in.

**FEAT-MEDIA-008 — Bezels.** A decorative frame drawn around the video, resolved through
a multi-level fallback chain.

**FEAT-MEDIA-009 † — Image cache.** Scaled and cropped images are written to a
resolution-specific folder mirroring the LaunchBox image folder structure, so the same
library at a different resolution gets its own cache. The mirror is produced by
string-replacing the LaunchBox application path with the cache root, so the two trees are
identical below that point:

```
<LaunchBox>\Images\<Platform>\<image type>\...            source
<LaunchBox>\Images\Platforms\<Platform>\Clear logo\...    source, platform logos
<LaunchBox>\Plugins\Eclipse\Media\<W>x<H>\Images\...      cache, same shape below Images
```

Cached artwork is written by whichever path first needs it — the destination folder is
created at that point, not in advance.

**FEAT-MEDIA-010 † — Logo cropping.** Clear logos are cropped to their non-transparent
bounds before caching.

**FEAT-MEDIA-011 † — Lazy hydration.** A background pump resolves media continuously,
prioritising the current list, then the next list, then anything else.

**FEAT-MEDIA-012 — Video volume.** Preview volume is adjustable from input and has a
configurable default.

**FEAT-MEDIA-013 † — Placeholder generation.** A default box-art placeholder is scaled
once at first run.

### Resolution rules

| ID | Rule |
|---|---|
| RULE-MEDIA-001 | Box art is discovered using the image-type priority order configured in LaunchBox itself. **Corrected:** the "built-in priority list" this rule used to mention was Eclipse's own fallback, read from `Data/Settings.xml` → `FrontImageTypePriorities` and used *only* by the bulk pre-scale loop that was removed in Feb 2022. It never affected which image the user sees — at runtime Eclipse reads `IGame.FrontImagePath` and LaunchBox applies the priority itself. The dead reader was deleted in `A1`. |
| RULE-MEDIA-002 | Cache paths are derived by string-replacing the LaunchBox application path with the resolution-specific media folder — so cache identity depends on absolute paths. |
| RULE-MEDIA-003 | If a cached image is absent it is generated on demand, synchronously, at the point of first use. |
| RULE-MEDIA-004 | Pre-scaled box height is `(monitor height × 100 ÷ 324) − 4`. The 4 accounts for a 2px border on each side. |
| RULE-MEDIA-005 | Monitor dimensions come from the window's screen; if that fails, defaults of 1440 high and 2560 wide are used. |
| RULE-MEDIA-006 | Background resolution order: background image → screenshot → bundled default. |
| RULE-MEDIA-007 | Star rating images are chosen by rounding to one decimal and looking up `<rating>.png`; a missing file yields no image rather than a fallback. |
| RULE-MEDIA-008 | Play mode falls back to a bundled `Fallback.png` when no icon matches. |
| RULE-MEDIA-009 | A game's media is resolved exactly once per session. **Corrected:** the flag used to be set *before* resolution began, so a caller arriving mid-hydration saw "resolved" against unpopulated paths. It is now set after, and a caller arriving mid-hydration waits for the run in flight. |
| RULE-MEDIA-010 | Media hydration priority is: a game in the current list, then a game in the next list, then any unresolved game. |
| RULE-MEDIA-011 | Game titles are converted to filenames by replacing every invalid filename character with `_`. |

### Bezel rules

| ID | Rule |
|---|---|
| RULE-MEDIA-020 | Bezel resolution order — the five levels, enumerated in full below. The first level that produces a file wins. |
| RULE-MEDIA-021 | Levels 1–3 are resolved when the game's media is resolved. Levels 4–5 are resolved later, once the video's dimensions are known. |
| RULE-MEDIA-022 | MAME and RetroArch are detected by a case-insensitive substring match for those words in the configured emulator's application path. MAME is tested first, so a path containing both resolves as MAME. |
| RULE-MEDIA-023 | RetroArch bezels require a platform-name translation table (`RetroarchHelper.RetroarchPlatformLookup`) to locate the overlay folder. A platform absent from that table gets no RetroArch bezel, whatever is on disk. |
| RULE-MEDIA-024 | **A widescreen video gets no *default* bezel** — when the aspect ratio is 1.7 or wider, or the video's height is not yet known, levels 4 and 5 are skipped and the video fills the frame. **Corrected:** this applies only to the defaults. A game-specific bezel from levels 1–3 is shown whatever the aspect ratio — the ratio test sits inside the "no game bezel" branch. |
| RULE-MEDIA-025 | Orientation for the default bezel is chosen from the video: vertical when height exceeds width, otherwise horizontal. Equal dimensions are horizontal. |
| RULE-MEDIA-027 | Level 1 matches by wildcard extension and takes the first result the filesystem returns, so two bezels for one game differing only in extension resolve unpredictably. |

### The five levels, in full

| # | Source | Resolved | Condition | Path |
|---|---|---|---|---|
| 1 | Eclipse game bezel | hydration | always tried first | `…\Plugins\Eclipse\Media\Bezels\{Platform}\**\{TitleToFileName}.*` — recursive, first match wins |
| 2 | MAME artwork | hydration | emulator path contains `mame` | `{emulator folder}\artwork\{game file name}\Bezel.png` |
| 3 | RetroArch overlay | hydration | emulator path contains `retroarch` **and** the platform is in the lookup table | `{emulator folder}\overlays\GameBezels\{RetroArch platform}\{game file name}.png` |
| 4 | Platform default | on `MediaOpened` | levels 1–3 found nothing, video height known, ratio < 1.7 | `…\Media\Bezels\{Platform}\{Horizontal\|Vertical}.png` |
| 5 | Global default | on `MediaOpened` | as level 4, and no platform default exists | `…\Media\Bezels\Default\{Horizontal\|Vertical}.png` |

Levels 4 and 5 are a dictionary built once when `BezelService` is constructed, from the
files present at that moment — a bezel dropped into the folder later is not seen until
Big Box restarts. `{game file name}` is the game's application path without its directory
or extension; `{TitleToFileName}` is its title with invalid filename characters replaced
by `_`.
| RULE-MEDIA-026 | When a bezel is shown, a gradient opacity mask is applied to the bezel; when no bezel is shown, the same mask is applied to the video instead. |

### Video rules

| ID | Rule |
|---|---|
| RULE-MEDIA-030 | Video starts only after the selection has settled and the background has faded in — a configurable delay after the 1-second idle delay. |
| RULE-MEDIA-031 | If the video delay is configured to zero, playback starts immediately without the background fade-in step. |
| RULE-MEDIA-032 | Video never starts while a game is running. |
| RULE-MEDIA-033 | Starting a video stops attract mode; the video ending restarts the attract-mode timer. |
| RULE-MEDIA-034 | When the video ends, the background image fades back in — the video does not loop. |
| RULE-MEDIA-035 | Video previews can be disabled entirely; the background image then remains visible. |

---

## Current implementation

| Concern | Location |
|---|---|
| Per-game media resolution | `Models/GameFiles.cs` — `SetupFiles`, `Resolve*` methods |
| Scaling, cropping, cache paths | `Service/ImageScaler.cs` |
| Cache directory layout | `Helpers/DirectoryInfoHelper.cs` |
| Bezel levels 1–3 | `Models/GameFiles.cs` — `ResolveBezelPath`; `Helpers/RetroarchHelper.cs` |
| Bezel levels 4–5 & orientation | `Service/BezelService.cs` — `ResolveBezel` is the whole chain behind one call; `Service/BezelRules.cs` holds the decision (precedence, widescreen cutoff, orientation) with no filesystem or LaunchBox dependency, so it is unit tested |
| Bezel opacity mask | `View/MainWindowView.xaml.cs` — `Video_SelectedGame_MediaOpened`. All the view still decides is which element the mask goes on (RULE-MEDIA-026) |
| Video playback & fades | `View/MainWindowView.xaml.cs` — `ShowSelectedGameAsync`, `PlaySettledGameVideoAsync`, `Video_SelectedGame_MediaEnded`, `FadeInBackgroundImages`. The two thread-pool timers chained by animation callbacks (`FadeForMovie`, `FadeOutForMovieDelay_Elapsed`) are gone — the whole selection is now one cancellable async sequence |
| Video failure handling | `View/MainWindowView.xaml.cs` — `PlaybackStartCheck_Tick`, `ApplyFailurePolicy`; `Service/VideoPlaybackMonitor.cs` |
| Off-thread image decoding | `Service/FrozenImageLoader.cs` (all callers); `Service/RowImageDecoder.cs` (box art row) |
| Hydration pump | `View/MainWindowViewModel.cs` — `SetupFiles`, `SetupNextGameFiles` |
| Startup image preparation | `Service/GameCatalog.cs` — `PrescaleImages`. Platform clear logos and the box-art placeholder only; game artwork is prepared lazily |
| Bundled media | `Eclipse/LaunchBox/Plugins/Eclipse/Media/` |
| Placeholder & embedded art | `Models/ResourceImages.cs`; `Resources/` |

LaunchBox SDK dependencies: `IGame` media path members, `GetVideoPath()`,
`PluginHelper.DataManager.GetEmulatorById()`.

## Technical debt

| Finding | Effect |
|---|---|
| ~~M-1~~ | **Resolved.** The flag was set *before* the work, so a second caller was told the game was ready and found every path still null — the reason attract mode could show the placeholder background for a game with artwork of its own. Hydration is now a `Helpers/RunOnce` task handle: callers arriving mid-run wait for it. |
| ~~M-2~~ | **Resolved on the normal path.** Every `Bitmap` in `ImageScaler` is now inside a `using`. **Residual:** `ResizeImage` and `Crop` each construct their result before drawing into it, and neither disposes it if the draw throws — a leak on a failure path that also loses the image. Folded into the `B-29` rewrite, which replaces both methods. |
| ~~M-3~~ | **Resolved.** The placeholder stream is in a `using` whose scope deliberately encloses the `Bitmap` built from it, because GDI+ reads the stream lazily. |
| ~~S-7~~ | **Measured and closed as a performance finding.** Two things were wrong with it. *Where:* the bulk pre-scale loops went in Feb 2022 (`cbb3066`), so this is first-run hydration on a `BelowNormal` thread, not startup — only platform clear logos remain on the startup path. *How much:* the per-pixel scan is **10%** of image preparation. The real cost is `Image.FromFile` (49%) and `Bitmap.Save` (22%) — reading and re-encoding every image in the library once, which is what the feature *is*. First run costs ≈ 167 ms per game of image work, ≈ 8 minutes for 1,469 games. The scan's *correctness* defect survives as `A4`. |
| M-9 | The hydration pump is `async void` on a `BackgroundWorker`; its failures are unobservable. |
| S-4 | Bezel orientation is decided in view code-behind. |
| M-4 | Media/video timers are never disposed. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-05 | Fixes the resolve-once race. |
| ~~B-06~~ | **Delivered.** See `M-2`/`M-3` above; the failure-path residual goes with `B-29`. |
| ~~B-20~~ | **Delivered.** All five levels enumerated in the table above first, as the item asked; then `BezelService.ResolveBezel` became the one entry point and the view's copy of the rule was deleted. Equivalence proved by sweeping the new decision against the old expression over ~200,000 dimension pairs. Turned up one documentation defect — see `RULE-MEDIA-024`. |
| B-22 | Makes the hydration pump observable and cancellable. |
| ~~B-28~~ | **Delivered**, and its instrumentation has since been removed (`B-34`). Baselines are in `docs/plans/box-art-row-refactor.md`. |
| ~~B-29~~ | **Closed by measurement, not done.** The `GetPixel` scan `S-7` names is **10%** of image preparation; 71% is reading source files (49%) and encoding outputs (22%), which is inherent to pre-scaling a library. A `LockBits` rewrite would save ~25 s of an eight-minute background job the user never waits for. What remains is the **correctness** half, which was never about speed — the crop retains one transparent row and column on the top and left edges — and that is `A4` of `docs/plans/media-and-presentation-refactor.md`. Measurements are recorded there. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-MEDIA-001` … `VER-MEDIA-008`.

**Currently automated:** none.
**Highest-value gaps:** bezel resolution (`RULE-MEDIA-020`…`026`), cache path derivation
(`RULE-MEDIA-002` — a path-handling change silently invalidates every cached image), and
crop output equivalence.

## Open questions

`OQ-009`, `OQ-010` — see [UNRESOLVED.md](../UNRESOLVED.md).
