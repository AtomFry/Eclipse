# EPIC-CONFIG — Configuration

**Scope.** The settings that parameterise every other epic, the desktop window used to
edit them, the custom-list definition editor, and how both are persisted.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-CONFIG-001 — Settings model.** 45 settings covering the default browse category,
voice search, attract mode, video, layout, detail visibility, alternate versions and
navigation. Every setting has a default, and a missing setting in a saved file takes its
default.

**FEAT-CONFIG-002 — Settings window.** A tabbed window opened from LaunchBox's Tools
menu, presenting the settings for editing with Save and Cancel.

**FEAT-CONFIG-003 — Custom list editor.** Create, edit and delete custom list
definitions: description, filter expressions, sort expressions, maximum size, and which
browse categories the list appears in.

**FEAT-CONFIG-004 † — Persistence.** Settings and custom lists are stored as two JSON
files under the plugin's own folder.

**FEAT-CONFIG-005 † — Backups.** Every save first copies the existing file into a
timestamped backup.

**FEAT-CONFIG-006 — Reorder custom lists.** Custom lists can be moved up and down; the
order determines their order in the browse view.

### When settings take effect

**Every setting applies the next time Big Box starts. None of them apply live.** This is
structural, not a limitation anyone chose:

* The settings editor is a LaunchBox menu item (`ShowInBigBox => false`), so it runs in
  LaunchBox.exe — a different process from BigBox.exe.
* Big Box reads the settings file once, into a cache that lives for the process, and nothing
  writes to that cache. There is no file watcher and no reload path.

So a change saved in LaunchBox cannot reach a Big Box that is already running. Start Big Box
after saving and the change is there.

`OQ-011` asked which settings were live and which needed a restart, on the premise that six
were "mirrored for live binding". That was a misreading of the mechanism: those six were
mirrored so the theme XAML had a binding target, and nothing ever set them after construction.

### Settings inventory

| Group | Settings |
|---|---|
| Browsing | `DefaultListCategoryType`, `ShowGameCountInList`, `RepeatGamesToFillScreen`, `IncludeBrokenGames`, `IncludeHiddenGames` |
| Navigation | `PageUpFunction`, `PageDownFunction`, `OpenSettingsPaneOnLeft`, `DisplayOptionsOnEscape`, `BypassDetails` |
| Voice | `EnableVoiceSearch` |
| Attract mode | `EnableScreenSaver`, `ScreensaverDelayInSeconds`, and the ten slideshow timings below |
| Video | `DisableVideos`, `VideoDelayInMilliseconds`, `DefaultVideoVolume` |
| Presentation | `DisplayFeaturedGame`, `ShowMatchPercent`, `ShowReleaseYear`, `ShowStarRating`, `ShowPlayMode`, `ShowPlatformLogo`, `ShowOptionsIcon`, `SelectedGameDetailsPadding`, `BoxFrontMargin{Left,Right,Top,Bottom}` |
| Alternate versions | `AdditionalVersionsEnable`, `AdditionalVersionsExcludeRunBefore`, `AdditionalVersionsExcludeRunAfter`, `AdditionalVersionsOnlyEmulatorOrDosBox`, `AdditionalApplicationDisplayField`, `AdditionalVersionsRemovePlayPrefix`, `AdditionalVersionsRemoveVersionPostfix` |

#### Screen saver slideshow timings

Every duration in the attract mode slideshow is a setting, on its own **Screen saver** tab.
All are `int` milliseconds; the tab displays them in seconds via `MillisecondsToSecondsConverter`.
Each default is the value that was previously hardcoded, so an existing installation behaves
exactly as it did before — see `RULE-ATTRACT-004`.

| Property | Default | Label on the tab |
|---|---|---|
| `ScreensaverDelayBetweenImagesMilliseconds` | 4000 | Hold on black |
| `ScreensaverBackgroundFadeInMilliseconds` | 3000 | Background fade in |
| `ScreensaverPanMilliseconds` | 17000 | Pan across screen |
| `ScreensaverLogoDelayMilliseconds` | 4000 | Logo appears after |
| `ScreensaverLogoFadeInMilliseconds` | 1500 | Logo fade in |
| `ScreensaverGameDurationMilliseconds` | 15000 | Time on each game |
| `ScreensaverBackgroundFadeOutMilliseconds` | 3000 | Background fade out |
| `ScreensaverLogoFadeOutMilliseconds` | 500 | Logo fade out |
| `ScreensaverFadeInMilliseconds` | 1000 | Screen saver fade in |
| `ScreensaverExitFadeMilliseconds` | 500 | Exit fade out |

They are consumed only through `Service/AttractModeTimings.cs`, which converts them to
`TimeSpan`s once per run. Nothing else converts milliseconds or reads the settings provider
for them. Two constraints are enforced there rather than in the UI: a negative value falls
back to its default, and *time on each game* minus *logo appears after* is clamped at zero, so
a logo delay longer than the game duration shortens the slide instead of producing a negative
wait.

`AttractModeTimings.Current` is built per run of the slideshow rather than cached, but that
does **not** make these settings live: the settings window loads its own copy from file and
saves it back, while the running plugin keeps the instance it read at startup
(`RULE-CONFIG-004`). All of them, `EnableScreenSaver` included, require a restart
(`OQ-011`). Building per run only guarantees that the slideshow and the view cannot disagree
about a value.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-CONFIG-001 | On first run, if no settings file exists, a default file is written to disk. | Settings exist as a file from the first launch. |
| RULE-CONFIG-002 | On first run, if no custom-list file exists, two lists are created: **Favorites** (games flagged favourite, sorted by title) and **History** (games with a last-played date, most recent first). Both appear in the Platform and Playlist categories. | These are the shipped defaults and users expect them. |
| RULE-CONFIG-003 | Missing properties in a saved file are populated with their declared defaults on load. Files carry a `SchemaVersion`, but nothing reads it yet — defaulting is still the only active migration mechanism. | Renaming a setting silently resets it; the version stamp gives a future migration something to branch on. |
| RULE-CONFIG-004 | Settings are read once and cached for the process lifetime; the cached instance is shared. | **Every** change requires a Big Box restart — see "When settings take effect" above. |
| RULE-CONFIG-005 | Saving serialises to a temp file in the same folder and then replaces the target in one step. | An interrupted save leaves the previous file intact. |
| RULE-CONFIG-006 | Each save first copies the previous file into a timestamped backup, and the ten most recent are kept. A file that will not parse is recovered from the newest backup that does, then from defaults. | Bounded, and the backups are now actually read. |
| RULE-CONFIG-007 | Custom list definitions are identified by a GUID assigned on first save. | Reordering and editing rely on it. |
| RULE-CONFIG-008 | The categories a custom list may appear in exclude *More like this*, *Random* and *Voice search*. | Those sets are generated, not browsed by category. |
| RULE-CONFIG-009 | Reordering custom lists is only persisted when the settings window is saved, not when the move is made. | Cancelling discards reordering. |
| RULE-CONFIG-010 | Deleting a custom list writes the file immediately, independent of Save/Cancel. | Asymmetric with `RULE-CONFIG-009` — see `OQ-017`. |

---

## Current implementation

| Concern | Location |
|---|---|
| Settings model & defaults | `Models/EclipseSettings.cs`; `Service/EclipseSettingsDataService.cs` — `GetDefaultSettings` |
| Custom list model | `Models/EclipseSettings.cs` — `CustomListDefinition`, `FilterExpression`, `SortExpression`, `GameFieldEnum` |
| Persistence | `Service/EclipseSettingsDataService.cs`, `Service/CustomListDefinitionDataService.cs`, both over `Helpers/JsonFileStore.cs` |
| Default custom lists | `Service/CustomListDefinitionDataService.cs` — `GetDefaultCustomLists` |
| File locations | `Helpers/DirectoryInfoHelper.cs` — `EclipseSettingsFile`, `CustomListsFile`, `SettingsBackupPath` |
| Settings window | `View/EclipseSettings/EclipseSettingsView.xaml(.cs)`, `EclipseSettingsViewModel.cs` |
| List editor | `View/EclipseSettings/CustomListDefinitionEditView.xaml(.cs)`, `CustomListDefinitionEditViewModel.cs` |
| Window entry point | `Plugins/EclipseSettingsMenuItem.cs` |
| Editor messaging | `Event/Event.cs` — the static `SettingsEvents`; commands are `Helpers/RelayCommand.cs`. No framework dependency. |
| Window palette & control styles | `EclipseSettingsView.xaml` — `Window.Resources`: seven brushes, styles for the field types, and templates for `ComboBox`, `CheckBox` and `Slider` |
| Tab layout | `EclipseSettingsView.xaml` — seven `DataTemplate`s in `Grid.Resources`, selected by triggers on `SelectedTabPage` |
| One setting row | `SettingRowStyle` / `HalfWidthRowStyle`; sliders are `View/EclipseSettings/SliderRow.cs` |

Files on disk: `<LaunchBox>/Plugins/Eclipse/Settings/EclipseSettings.json`,
`CustomLists.json`, `DataBackup/`.

## Technical debt

| Finding | Effect |
|---|---|
| S-5 | **Resolved.** Each setting is declared once, on `EclipseSettings`. Both view models bind to that object through a `Settings` property; the four box-front margins keep a wrapper because their setter refreshes the margin preview. |
| S-6 | **Resolved.** `Helpers/JsonFileStore` writes via a temp file and replaces the target, stamps a `SchemaVersion`, keeps the ten most recent backups, and falls back to the newest readable backup - then to defaults - rather than throwing. |
| S-13 | **Resolved.** All six report or log. A failed save now leaves the window open with the edits intact, and the custom-list write is awaited rather than racing the window closing. |
| M-4 | **Resolved.** Both windows detach on `Closed`. Prism held subscribers weakly, so the leak was overstated - the real risk was delivery to a stale instance still alive. |
| C-7 | **Resolved.** Prism is gone - `Helpers/RelayCommand` and the static `SettingsEvents` replaced it, and `Prism.dll` is no longer shipped. |
| C-8 | **Resolved.** Split into `CustomListDefinitionDataProvider`, `CustomListDefinitionDataService`, `EclipseSettingsDataProvider` and `EclipseSettingsDataService`. |

## Modernization backlog

| Item | Relationship |
|---|---|
| ~~B-26~~ | **Delivered.** Triplication collapsed; `OQ-011` answered by reading the code rather than by audit - no setting is live. |
| ~~B-27~~ | **Delivered** - `Helpers/JsonFileStore`, shared with the custom-list file. |
| ~~B-25~~ | **Delivered**, plus one defect the finding did not record: the custom-list write was started and never awaited. |
| B-23 | **Settings share delivered** - both windows detach on Closed. The attract, media and presentation shares remain. |
| ~~B-13~~ | **Delivered** - `RelayCommand` and `SettingsEvents`; `Prism.Core` reference removed. |
| ~~B-10~~ | **Delivered.** |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-CONFIG-001` … `VER-CONFIG-005`.

**Currently automated:** none.
**Highest-value gap:** round-trip persistence including unknown/missing fields
(`RULE-CONFIG-003`), and the default custom lists (`RULE-CONFIG-002`) which every new
user sees.

## Open questions

`OQ-011`, `OQ-017` — see [UNRESOLVED.md](../UNRESOLVED.md).
