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
library at a different resolution gets its own cache.

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
| RULE-MEDIA-001 | Box art is discovered using the image-type priority order configured in LaunchBox itself; if that cannot be read, a built-in priority list is used. |
| RULE-MEDIA-002 | Cache paths are derived by string-replacing the LaunchBox application path with the resolution-specific media folder — so cache identity depends on absolute paths. |
| RULE-MEDIA-003 | If a cached image is absent it is generated on demand, synchronously, at the point of first use. |
| RULE-MEDIA-004 | Pre-scaled box height is `(monitor height × 100 ÷ 324) − 4`. The 4 accounts for a 2px border on each side. |
| RULE-MEDIA-005 | Monitor dimensions come from the window's screen; if that fails, defaults of 1440 high and 2560 wide are used. |
| RULE-MEDIA-006 | Background resolution order: background image → screenshot → bundled default. |
| RULE-MEDIA-007 | Star rating images are chosen by rounding to one decimal and looking up `<rating>.png`; a missing file yields no image rather than a fallback. |
| RULE-MEDIA-008 | Play mode falls back to a bundled `Fallback.png` when no icon matches. |
| RULE-MEDIA-009 | A game's media is resolved exactly once per session; the flag is set before resolution begins. |
| RULE-MEDIA-010 | Media hydration priority is: a game in the current list, then a game in the next list, then any unresolved game. |
| RULE-MEDIA-011 | Game titles are converted to filenames by replacing every invalid filename character with `_`. |

### Bezel rules

| ID | Rule |
|---|---|
| RULE-MEDIA-020 | Bezel resolution order: (1) a game-specific bezel under the plugin's bezel folder for that platform, matching the game's title-as-filename; (2) a MAME artwork bezel; (3) a RetroArch overlay bezel; (4) a platform default; (5) a global default. |
| RULE-MEDIA-021 | Levels 1–3 are resolved when the game's media is resolved. Levels 4–5 are resolved later, in the view, because they depend on the video's dimensions. |
| RULE-MEDIA-022 | MAME and RetroArch are detected by looking for those words in the configured emulator's application path. |
| RULE-MEDIA-023 | RetroArch bezels require a platform-name translation table to locate the overlay folder. |
| RULE-MEDIA-024 | **No bezel is applied when the video's aspect ratio is 1.7 or wider.** Widescreen video fills the frame instead. |
| RULE-MEDIA-025 | Orientation for the default bezel is chosen from the video: vertical when height exceeds width, otherwise horizontal. |
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
| Bezel levels 4–5 & orientation | `Service/BezelService.cs`; `View/MainWindowView.xaml.cs` — `Video_SelectedGame_MediaOpened` |
| Video playback & fades | `View/MainWindowView.xaml.cs` — `FadeForMovie`, `FadeOutForMovieDelay_Elapsed`, `Video_SelectedGame_MediaEnded` |
| Hydration pump | `View/MainWindowViewModel.cs` — `SetupFiles`, `SetupNextGameFiles` |
| Bundled media | `Eclipse/LaunchBox/Plugins/Eclipse/Media/` |
| Placeholder & embedded art | `Models/ResourceImages.cs`; `Resources/` |

LaunchBox SDK dependencies: `IGame` media path members, `GetVideoPath()`,
`PluginHelper.DataManager.GetEmulatorById()`.

## Technical debt

| Finding | Effect |
|---|---|
| M-1 | The resolve-once flag is set non-atomically; two threads can both resolve the same game. |
| M-2 | Cropped bitmaps are never disposed — once per cached logo. |
| M-3 | A stream in placeholder generation is never disposed. |
| S-7 | Cropping reads pixels one at a time through GDI+; this dominates first-run startup. |
| M-9 | The hydration pump is `async void` on a `BackgroundWorker`; its failures are unobservable. |
| S-4 | Bezel orientation is decided in view code-behind. |
| M-4 | Media/video timers are never disposed. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-05 | Fixes the resolve-once race. |
| B-06 | Fixes the bitmap and stream leaks. |
| B-20 | Consolidates bezel resolution — **enumerate all five levels first**. |
| B-22 | Makes the hydration pump observable and cancellable. |
| B-28 | Measures image processing and startup. |
| B-29 | Replaces per-pixel cropping — acceptance is pixel-identical output. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-MEDIA-001` … `VER-MEDIA-008`.

**Currently automated:** none.
**Highest-value gaps:** bezel resolution (`RULE-MEDIA-020`…`026`), cache path derivation
(`RULE-MEDIA-002` — a path-handling change silently invalidates every cached image), and
crop output equivalence.

## Open questions

`OQ-009`, `OQ-010` — see [UNRESOLVED.md](../UNRESOLVED.md).
