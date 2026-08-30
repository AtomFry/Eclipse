# Eclipse

Eclipse is a plugin and theme for [LaunchBox](https://www.launchbox-app.com/)'s **Big Box**
mode. It presents your game library in a Netflix-style interface: rows of box art grouped
into categories, a full-screen background and video preview for the selected game, voice
search, and a random-game feature.

[Demo video](https://youtu.be/rcrl4AN2Jsw) ·
[Download](https://forums.launchbox-app.com/files/file/3220-eclipse/)

## What it does

| Capability | Summary |
|---|---|
| **Browsing** | Browse by platform, genre, series, playlist, play mode, developer, publisher or release year. Custom user-defined lists (Favorites and History ship by default) appear above the category lists. |
| **Voice search** | Speak part of a game's name to find it. Results are ranked by how well the phrase matched and by recogniser confidence. |
| **Presentation** | Background image or video preview per game, clear logos, star ratings, play mode, platform logo, and an optional featured-game view. |
| **Media** | Artwork is discovered from your LaunchBox library and pre-scaled to your display resolution so browsing stays smooth. Video previews are framed with per-platform bezels. |
| **Launching** | Launch a game, or choose between alternate versions when a game has additional applications configured. |
| **Favourites & ratings** | Mark favourites and set star ratings without leaving Big Box; changes are written back to LaunchBox. |
| **Attract mode** | An idle screensaver that cycles random games with slow pans and fades. |
| **Configuration** | 49 settings and a custom-list editor, available from the LaunchBox desktop app under **Tools → Manage eclipse**. |

## Controls

| Input | Action |
|---|---|
| Up / Down | Move between lists |
| Left / Right | Move between games |
| Enter | Open the game detail overlay (or launch directly, if configured) |
| Escape | Back — eventually returns you to Big Box's own menus |
| Page Up | Random game *(remappable)* |
| Page Down | Voice search *(remappable)* |

Page Up and Page Down can each be reassigned to any of: page up, page down, random game,
voice search, flip box, zoom box, volume up, volume down, show details, or play game.

## Installation

Installation instructions ship with the plugin — see
[`Eclipse/LaunchBox/Plugins/Eclipse/README.txt`](Eclipse/LaunchBox/Plugins/Eclipse/README.txt).

In short: copy the `Plugins`, `Themes` and `StartupThemes` folders into your LaunchBox
installation, then in Big Box set **Options → Views → Theme** to *Eclipse* and
**Theme-Specific Options → Eclipse → Views → Platform List View** to *Platform Wheel 1*.

**Requirements:** LaunchBox / Big Box 14.x on Windows (x64). Voice search additionally
requires Windows Speech Recognition.

## Known limitations

* Voice search is the only search modality — there is no text search.
* Scaled artwork is cached per display resolution and is not invalidated if you later
  replace the artwork in LaunchBox (see `OQ-010`).
* **Clear logos cached before the crop fix keep one extra transparent row and column** on
  their top and left edges. Cropping used to leave them there; it no longer does, but the
  cache is keyed by path alone, so logos already on disk are not regenerated and logos
  cached from now on will differ from them by a pixel. To bring an existing installation
  fully onto the new crop, delete the `Clear Logo` folders under
  `LaunchBox\Plugins\Eclipse\Media\<width>x<height>\Images\` — Eclipse rebuilds them in the
  background as you browse.
* Most settings take effect on restart rather than immediately (see `OQ-011`).
* Eclipse appears under **Tools → Manage eclipse** in desktop LaunchBox; it is
  deliberately not listed in the LaunchBox Plugin Manager (see `OQ-018`).

## Documentation

Full product and engineering documentation lives in [`docs/`](docs/):

| Document | Purpose |
|---|---|
| [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) | Building Eclipse, deploying to your LaunchBox, release process |
| [docs/FEATURES.md](docs/FEATURES.md) | Product map — epics, complete feature inventory, dependencies |
| [docs/features/](docs/features/) | Per-capability detail: behavioural rules, implementation, debt, verification |
| [docs/VERIFICATION.md](docs/VERIFICATION.md) | How behaviour is verified; characterization-test candidates |
| [docs/TRACEABILITY.md](docs/TRACEABILITY.md) | Capability ↔ technical debt ↔ modernization work |
| [docs/UNRESOLVED.md](docs/UNRESOLVED.md) | Suspected dead code and open questions |

Eclipse is currently being modernized. The documentation in `docs/` describes the
**current behaviour**, which is the contract that modernization must preserve.

## Logs

Errors are written to `Eclipse.txt` in your `LaunchBox\Plugins\Eclipse` folder, alongside
the plugin itself.

## Source

Eclipse is written in C# on .NET 10 (WPF), targeting the LaunchBox 14 plugin contract.
