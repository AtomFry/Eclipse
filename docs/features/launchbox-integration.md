# EPIC-INTEGRATE — LaunchBox & Plugin Integration

**Scope.** Everything that exists because Eclipse is a LaunchBox plugin rather than a
standalone application: registration with the host, theme packaging, reading the game
catalogue, path resolution, the startup sequence, and diagnostics.

If Eclipse were ever hosted somewhere else, this is the epic that would be replaced.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-INTEGRATE-001 — Big Box theme element.** Eclipse registers with Big Box as a theme
element plugin. The Eclipse theme's platform view hosts Eclipse's main control, and Big
Box forwards user input to it.

**FEAT-INTEGRATE-002 — Desktop menu item.** Eclipse adds a **Manage eclipse** item to the
desktop LaunchBox Tools menu, which opens the settings window. It appears in desktop
LaunchBox only, never in Big Box.

**FEAT-INTEGRATE-003 — Packaging.** Eclipse ships three payloads into the LaunchBox
folder: the plugin and its media, a Big Box theme, and a startup theme.

**FEAT-INTEGRATE-004 † — Catalogue access.** Eclipse reads all games, all playlists and
emulator records from LaunchBox, and writes back favourite and rating changes.

**FEAT-INTEGRATE-005 † — Startup.** On first display, Eclipse builds its game index,
pre-scales missing images, optionally builds the voice grammar, constructs the list sets,
and then shows the default category.

**FEAT-INTEGRATE-006 † — Path resolution.** All paths derive from the running host
process location, so Eclipse works wherever LaunchBox is installed.

**FEAT-INTEGRATE-007 † — Diagnostics log.** Errors are appended to a plain-text log.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-INTEGRATE-001 | The LaunchBox root is derived from the host executable's folder; if that folder is named `Core`, its parent is used instead. | LaunchBox 13+ runs from `Core\`. Without this rule every path breaks. |
| RULE-INTEGRATE-002 | The plugin's data lives in `<LaunchBox>/Plugins/Eclipse/` — the same folder the plugin assembly is deployed into. | Code and data share a folder. |
| RULE-INTEGRATE-003 | The image cache folder is named after the current display resolution, so different resolutions maintain separate caches. | Changing monitor invalidates nothing; it builds a second cache. |
| RULE-INTEGRATE-004 | Required folders are created at startup if absent. | First run works with no manual setup. |
| RULE-INTEGRATE-005 | The theme's platform view hosts Eclipse by assembly name. The assembly **must** remain named `Eclipse`. | Renaming breaks the theme and every embedded image URI. |
| RULE-INTEGRATE-006 | Loading runs on a background thread; input is absorbed until it completes. | |
| RULE-INTEGRATE-007 | A failure during loading transitions to the error state rather than leaving a blank screen. | |
| RULE-INTEGRATE-008 | Media hydration starts during loading and continues in the background afterwards. | Eclipse is usable before all media is resolved. |
| RULE-INTEGRATE-009 | The desktop menu item is declared visible in LaunchBox and hidden in Big Box. | |
| RULE-INTEGRATE-010 | The diagnostics log is written to `<LaunchBox>/Plugins/Eclipse/Eclipse.txt`, resolved absolutely and created if absent. If the plugin folder cannot be resolved, it falls back to a relative `Eclipse.txt`. | Previously a bare relative path that landed in the LaunchBox root only by luck. Destination is now deterministic; levels, rotation and thread-safety remain outstanding under `M-7`. |
| RULE-INTEGRATE-011 | Eclipse must not ship a copy of the LaunchBox plugin contract assembly. | Two copies in one process breaks the interface cast and the plugin fails to load. |
| RULE-INTEGRATE-012 | Eclipse must not ship a `manifest.json`. A manifest causes LaunchBox 14 to treat it as a managed plugin, and the Tools menu item does not appear. | Established empirically; see `OQ-018`. |

### Host requirements

| Requirement | Value |
|---|---|
| Host | LaunchBox / Big Box 14.x |
| Runtime | .NET 10 (`net10.0-windows`), x64 |
| Plugin contract | `Unbroken.LaunchBox.Plugins` 14.0.0.0, referenced but not deployed |
| Deployment | `<LaunchBox>/Plugins/Eclipse/` |
| Theme selection | Big Box → Options → Views → Theme: Eclipse; Platform List View: Platform Wheel 1 |

---

## Current implementation

| Concern | Location |
|---|---|
| Theme element registration | `View/MainWindowView.xaml.cs` — implements `IBigBoxThemeElementPlugin` |
| Desktop menu item | `Plugins/EclipseSettingsMenuItem.cs` — implements `ISystemMenuItemPlugin` |
| Theme hosting | `Eclipse/LaunchBox/Themes/Eclipse/Views/PlatformWheel1FiltersView.xaml` |
| Startup theme | `Eclipse/LaunchBox/StartupThemes/Eclipse/` |
| Catalogue access | `Service/DataService.cs`, `Service/PlaylistGameService.cs`, `Service/BezelService.cs`, `Models/GameFiles.cs`, `View/MainWindowViewModel.cs` |
| Startup sequence | `State/LoadingState.cs` |
| Paths | `Helpers/DirectoryInfoHelper.cs`; `Helpers/DisplayInfoHelper.cs` |
| Logging | `Helpers/LogHelper.cs` |
| Packaging | `Eclipse/Eclipse.csproj` — `StageEclipsePayload` target |

LaunchBox SDK surface actually consumed — six operations plus `IGame`/`IAdditionalApplication`/`IEmulator` metadata:

```
PluginHelper.DataManager.GetAllGames()
PluginHelper.DataManager.GetAllPlatforms()
PluginHelper.DataManager.GetAllPlaylists()
PluginHelper.DataManager.GetEmulatorById(id)
PluginHelper.DataManager.Save(false)
PluginHelper.BigBoxMainViewModel.PlayGame(game, additionalApp, null, null)
```

## Technical debt

| Finding | Effect |
|---|---|
| S-2 | The six operations above are called from five files including the view model and the model layer. |
| M-7 | The log path is relative and the writer is unsynchronised. |
| C-7 | Prism does not resolve in the host load context. |
| M-9 | The loading pipeline uses `async void` on a `BackgroundWorker`. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-11 | Wraps the six SDK operations — **the defining item for this epic**. |
| B-02 | Fixes the log destination. |
| B-22 | Fixes the loading pipeline. |
| B-13 | Resolves the Prism load failure. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-INTEGRATE-001` … `VER-INTEGRATE-005`.

**Currently automated:** none — this epic is inherently host-dependent.
**Highest-value gap:** `RULE-INTEGRATE-011` and `RULE-INTEGRATE-012`. Both were
discovered the hard way during the LaunchBox 14 migration, both cause a total feature
loss, and neither is detectable except by deploying and looking. A deployment smoke
check should assert both.

## Open questions

`OQ-018`, `OQ-019` — see [UNRESOLVED.md](../UNRESOLVED.md).
