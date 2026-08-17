# EPIC-LAUNCH — Game Launching

**Scope.** Starting a game through LaunchBox, including choosing between alternate
versions, and everything Eclipse must do to get out of the way while a game runs.

---

## Behaviour

*This section describes what Eclipse does. It must remain true after refactoring.*

### Features

**FEAT-LAUNCH-001 — Launch the selected game.** From the detail overlay (or the featured
view), the user starts the current game. LaunchBox performs the actual launch.

**FEAT-LAUNCH-002 — Alternate versions.** When a game has additional applications, the
user cycles between them with left/right in the detail overlay and launches the selected
one. The chosen version is shown by name, region or version depending on configuration.

**FEAT-LAUNCH-003 — Bypass details.** When enabled, pressing Enter while browsing
launches the game immediately instead of opening the detail overlay.

**FEAT-LAUNCH-004 † — Playback suppression.** Launching stops video and animation and
prevents attract mode from starting while the game runs.

**FEAT-LAUNCH-005 † — Last-played.** The game's last-played date is set at launch, which
causes History-style lists to change.

### Behavioural rules

| ID | Rule | Why it matters |
|---|---|---|
| RULE-LAUNCH-001 | Launching sets the last-played date, saves browsing position, rebuilds the lists, then launches. The rebuild happens **before** the game starts. | Position restoration (`RULE-BROWSE-010`) must run while Eclipse is still responsive. |
| RULE-LAUNCH-002 | The selected alternate version is passed to LaunchBox; when no additional application is selected, the base game is launched. | |
| RULE-LAUNCH-003 | Additional applications may be filtered out: those that auto-run before launch, those that auto-run after launch, and those not using an emulator or DOSBox — each independently configurable. | Without filtering, setup/cleanup apps appear as playable versions. |
| RULE-LAUNCH-004 | If an additional application has the same application path as the game itself, the base game is **not** added separately — the additional application represents it. | Prevents a duplicate entry. |
| RULE-LAUNCH-005 | Version labels can strip a leading `"Play "` prefix and a trailing `"Version..."` suffix, each independently configurable. | LaunchBox conventions produce verbose names. |
| RULE-LAUNCH-006 | When alternate versions are disabled, the version list is empty — not a single-entry list containing the base game. | Observable in the overlay. |
| RULE-LAUNCH-007 | While a game is running, any directional or action input clears the running flag. | This is how Eclipse learns the game exited — it infers it from input, it does not observe LaunchBox. See `OQ-013`. |
| RULE-LAUNCH-008 | Launch triggers a repeated stop of video and animation over roughly half a second. | Compensates for an unidentified race — see `OQ-012`; do not remove without diagnosing. |

---

## Current implementation

| Concern | Location |
|---|---|
| Launch | `View/MainWindowViewModel.cs` — `PlayCurrentGame` |
| Version list construction & filtering | `Models/GameFiles.cs` — `ResolveAdditionalGameVersionList`, `GameVersion`, `GameVersionList` |
| Version cycling | `State/GameDetailOptionPlayState.cs` — `OnLeft`, `OnRight` |
| Bypass details | `State/SelectingGameState.cs` — `OnEnter` |
| Launch from featured view | `State/FeatureOptionPlayState.cs` — `OnEnter` |
| Playback suppression | `View/MainWindowView.xaml.cs` — `StopVideoAndAnimations`, `StopEverything` |
| Running-state flag | `View/MainWindowViewModel.cs` — `IsPlayingGame`, `DoUp`/`DoDown`/… |
| Key strategy | `State/KeyStrategy/KeyStrategyPlayGame.cs` |

LaunchBox SDK dependencies: `PluginHelper.BigBoxMainViewModel.PlayGame(game, additionalApplication, null, null)`,
`IGame.LastPlayedDate`, `IGame.GetAllAdditionalApplications()`, `IAdditionalApplication`.

## Technical debt

| Finding | Effect |
|---|---|
| S-1 | Launch orchestration lives in the god view model. |
| S-2 | Launching is a direct `PluginHelper` call from the view model — the deepest layering violation in the product. |
| S-9 | Launch triggers a full rebuild of every list. |
| S-12 | The half-second stop loop blocks on a background worker. |

## Modernization backlog

| Item | Relationship |
|---|---|
| B-11 | Introduces `IGameLauncher`, removing the direct `PluginHelper` call. |
| B-16 | Makes the post-launch list rebuild incremental. |
| B-24 | Removes the stop loop — **must diagnose the underlying race first**. |
| B-12 | Alternate-version data moves onto the Eclipse-owned model. |

## Verification

See [VERIFICATION.md](../VERIFICATION.md): `VER-LAUNCH-001` … `VER-LAUNCH-004`.

**Currently automated:** none.
**Highest-value gap:** alternate-version filtering (`RULE-LAUNCH-003`/`004`) is pure
logic over additional-application metadata and is easily testable once the game model is
Eclipse-owned.

## Open questions

`OQ-012`, `OQ-013` — see [UNRESOLVED.md](../UNRESOLVED.md).
