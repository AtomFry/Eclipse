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

### Settings inventory

| Group | Settings |
|---|---|
| Browsing | `DefaultListCategoryType`, `ShowGameCountInList`, `RepeatGamesToFillScreen`, `IncludeBrokenGames`, `IncludeHiddenGames` |
| Navigation | `PageUpFunction`, `PageDownFunction`, `OpenSettingsPaneOnLeft`, `DisplayOptionsOnEscape`, `BypassDetails` |
| Voice | `EnableVoiceSearch` |
| Attract mode | `EnableScreenSaver`, `ScreensaverDelayInSeconds` |
| Video | `DisableVideos`, `VideoDelayInMilliseconds`, `DefaultVideoVolume` |
| Presentation | `DisplayFeaturedGame`, `ShowMatchPercent`, `ShowReleaseYear`, `ShowStarRating`, `ShowPlayMode`, `ShowPlatformLogo`, `ShowOptionsIcon`, `SelectedGameDetailsPadding`, `BoxFrontMargin{Left,Right,Top,Bottom}` |
| Alternate versions | `AdditionalVersionsEnable`, `AdditionalVersionsExcludeRunBefore`, `AdditionalVersionsExcludeRunAfter`, `AdditionalVersionsOnlyEmulatorOrDosBox`, `AdditionalApplicationDisplayField`, `AdditionalVersionsRemovePlayPrefix`, `AdditionalVersionsRemoveVersionPostfix` |

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-CONFIG-001 | On first run, if no settings file exists, a default file is written to disk. | Settings exist as a file from the first launch. |
| RULE-CONFIG-002 | On first run, if no custom-list file exists, two lists are created: **Favorites** (games flagged favourite, sorted by title) and **History** (games with a last-played date, most recent first). Both appear in the Platform and Playlist categories. | These are the shipped defaults and users expect them. |
| RULE-CONFIG-003 | Missing properties in a saved file are populated with their declared defaults on load. This is the **only** migration mechanism — there is no schema version. | Renaming a setting silently resets it. |
| RULE-CONFIG-004 | Settings are read once and cached for the process lifetime; the cached instance is shared. | Most changes require a restart to take effect. |
| RULE-CONFIG-005 | Saving writes the whole file, replacing it in place. | Interruption can truncate it — see `S-6`. |
| RULE-CONFIG-006 | Each save first copies the previous file into a timestamped backup. Backups are never pruned. | The folder grows without bound. |
| RULE-CONFIG-007 | Custom list definitions are identified by a GUID assigned on first save. | Reordering and editing rely on it. |
| RULE-CONFIG-008 | The categories a custom list may appear in exclude *More like this*, *Random* and *Voice search*. | Those sets are generated, not browsed by category. |
| RULE-CONFIG-009 | Reordering custom lists is only persisted when the settings window is saved, not when the move is made. | Cancelling discards reordering. |
| RULE-CONFIG-010 | Deleting a custom list writes the file immediately, independent of Save/Cancel. | Asymmetric with `RULE-CONFIG-009` — see `OQ-017`. |

---

## Current implementation

| Concern | Location |
|---|---|
| Settings model & defaults | `Models/EclipseSettings.cs`; `Service/CustomListDefinitionDataProvider.cs` — `GetDefaultSettings` |
| Custom list model | `Models/EclipseSettings.cs` — `CustomListDefinition`, `FilterExpression`, `SortExpression`, `GameFieldEnum` |
| Persistence | `Service/CustomListDefinitionDataProvider.cs` — `EclipseSettingsDataService`, `CustomListDefinitionDataService` |
| Default custom lists | `Service/CustomListDefinitionDataProvider.cs` — `GetDefaultCustomLists` |
| File locations | `Helpers/DirectoryInfoHelper.cs` — `EclipseSettingsFile`, `CustomListsFile`, `SettingsBackupPath` |
| Settings window | `View/EclipseSettings/EclipseSettingsView.xaml(.cs)`, `EclipseSettingsViewModel.cs` |
| List editor | `View/EclipseSettings/CustomListDefinitionEditView.xaml(.cs)`, `CustomListDefinitionEditViewModel.cs` |
| Window entry point | `Plugins/EclipseSettingsMenuItem.cs` |
| Editor messaging | `Event/Event.cs`; `Helpers/EventAggregatorHelper.cs` (Prism) |

Files on disk: `<LaunchBox>/Plugins/Eclipse/Settings/EclipseSettings.json`,
`CustomLists.json`, `DataBackup/`.

## Technical debt

| Finding | Effect |
|---|---|
| S-5 | 45 settings, ~36 mirrored in the settings view model, 6 mirrored again in the main view model. |
| S-6 | Non-atomic writes, no schema version, unbounded backups. |
| S-13 | Six `async void` command handlers whose failures cannot be observed. |
| M-4 | Prism event subscriptions in the settings view models are never detached, and these windows open and close repeatedly. |
| C-7 | Prism does not resolve inside LaunchBox, so the assembly loads on a partial type list. |
| C-8 | Four types share one misleadingly named file. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-26 | Collapses the triplication — **first produce the live-vs-restart table** (`OQ-011`). |
| B-27 | Atomic writes, schema version, bounded backups. |
| B-25 | Fixes the `async void` command handlers. |
| B-23 | Fixes the subscription leak — settings windows are the highest-value case. |
| B-13 | Removes the Prism dependency. |
| B-10 | Splits the four-class file. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-CONFIG-001` … `VER-CONFIG-005`.

**Currently automated:** none.
**Highest-value gap:** round-trip persistence including unknown/missing fields
(`RULE-CONFIG-003`), and the default custom lists (`RULE-CONFIG-002`) which every new
user sees.

## Open questions

`OQ-011`, `OQ-017` — see [UNRESOLVED.md](../UNRESOLVED.md).
