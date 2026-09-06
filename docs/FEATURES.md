# Eclipse — Product Map

Eclipse is a plugin and theme for LaunchBox's **Big Box** mode. It replaces the platform
list view with a Netflix-style browsing interface: rows of box art grouped into
categories, a large background image and video preview for the selected game, voice
search, and a random-game feature. A companion settings window is reachable from the
LaunchBox desktop application.

This document is the index. Each epic has its own file under [features/](features/) with
behavioural rules, implementation locations, technical debt and verification.

---

## Taxonomy

Ten epics, derived from what the product does rather than how it is built.

| Epic | Scope |
|---|---|
| [EPIC-BROWSE](features/browsing.md) | Turning the LaunchBox library into browsable, navigable lists of games |
| [EPIC-SEARCH](features/search.md) | Finding a game by typing or speaking its name |
| [EPIC-PRESENT](features/presentation.md) | What is on screen: box art row, details, overlays, view modes |
| [EPIC-MEDIA](features/media.md) | Locating, caching and preparing the artwork and video for a game |
| [EPIC-LAUNCH](features/launching.md) | Starting a game, including alternate versions |
| [EPIC-CURATE](features/favorites-and-ratings.md) | Marking favourites and setting star ratings |
| [EPIC-ATTRACT](features/attract-mode.md) | Idle screensaver behaviour |
| [EPIC-INPUT](features/input.md) | Controller/keyboard input and how it routes to behaviour |
| [EPIC-CONFIG](features/configuration.md) | Settings, custom-list definitions, and their persistence |
| [EPIC-INTEGRATE](features/launchbox-integration.md) | Living inside LaunchBox: plugin registration, data access, startup |

### Why these boundaries

* **BROWSE vs PRESENT** — browsing is *which games are in which order*; presentation is
  *what the user sees*. They are separable: voice search and random game both feed the
  same presentation surface.
* **MEDIA is separate from PRESENT** — media resolution is a caching and file-discovery
  concern that runs on background threads, largely independent of what is displayed.
  It is also the most performance-sensitive area of the product.
* **CURATE is separate from BROWSE** even though favouriting rebuilds lists — the user
  intent ("mark this game") is distinct from the consequence ("lists change").
* **INTEGRATE** collects everything that exists only because Eclipse is a plugin. If
  Eclipse were ever hosted elsewhere, this is the epic that would be replaced.

---

## Feature inventory

72 features, of which 1 is designed but not built. `†` marks a feature that is supporting
rather than directly user-facing; `‡` marks one that is designed but not built.

### EPIC-BROWSE — Game Discovery & Browsing

| ID | Feature |
|---|---|
| FEAT-BROWSE-001 | Browse games grouped by category (platform, genre, series, playlist, play mode, developer, publisher, release year) |
| FEAT-BROWSE-002 | User-defined custom lists with filters, sorting and a size cap |
| FEAT-BROWSE-003 | Playlists shown alongside platforms when the playlist opts in |
| FEAT-BROWSE-004 | Move between games within a list (left/right) |
| FEAT-BROWSE-005 | Move between lists (up/down) |
| FEAT-BROWSE-006 | Page jump within a long list |
| FEAT-BROWSE-007 | Jump to a random game |
| FEAT-BROWSE-008 | "More like this" — build a result set from the current game's metadata |
| FEAT-BROWSE-009 | Category picker pane |
| FEAT-BROWSE-010 | Restore browsing position after the lists are rebuilt |
| FEAT-BROWSE-011 | Repeat games to fill a short row |
| FEAT-BROWSE-012 | Show game counts in list titles |
| FEAT-BROWSE-013 † | Exclude broken and hidden games from the library |
| FEAT-BROWSE-014 † | Build the in-memory game index that all lists are derived from |

### EPIC-SEARCH — Finding a Game

Two modalities, sharing nothing but their purpose. `‡` marks a feature that is designed but not
built — see *Deliberately not built* in [features/search.md](features/search.md).

**By voice**

| ID | Feature |
|---|---|
| FEAT-SEARCH-001 † | Build a speech grammar from every game title in the library |
| FEAT-SEARCH-002 | Start a voice search and capture spoken phrases |
| FEAT-SEARCH-003 | Match recognised phrases to games |
| FEAT-SEARCH-004 | Score each match for relevance |
| FEAT-SEARCH-005 | Rank and present results as browsable lists |
| FEAT-SEARCH-006 | Report recognition failures to the user |

**By typing**

| ID | Feature |
|---|---|
| FEAT-SEARCH-010 | On-screen keyboard, navigable with the four directions |
| FEAT-SEARCH-011 | Live title search — the library filters as you type |
| FEAT-SEARCH-012 ‡ | Typo tolerance |
| FEAT-SEARCH-013 | Metadata term suggestions, with counts |
| FEAT-SEARCH-014 | Stacking metadata filters |
| FEAT-SEARCH-015 | Removing metadata filters |
| FEAT-SEARCH-016 | Select a result and open the game |
| FEAT-SEARCH-017 | Remember the query and position across leaving the screen |
| FEAT-SEARCH-018 | Report the result count, or why there is none |
| FEAT-SEARCH-019 † | Build the token index every text search is answered from |

### EPIC-PRESENT — Game Presentation

| ID | Feature |
|---|---|
| FEAT-PRESENT-001 | Box art row showing the current list |
| FEAT-PRESENT-002 | Selected-game details (title, logo, year, match %, ratings, play mode, platform logo) |
| FEAT-PRESENT-003 | Game detail overlay with Play / Favourite / Rating / More options |
| FEAT-PRESENT-004 | Featured-game view above the first list |
| FEAT-PRESENT-005 | Flip box art to show the reverse |
| FEAT-PRESENT-006 | Zoom box art |
| FEAT-PRESENT-007 | Loading indication during startup |
| FEAT-PRESENT-008 | Error display |
| FEAT-PRESENT-009 | Show/hide individual detail fields |
| FEAT-PRESENT-010 | Box art margins and details padding |
| FEAT-PRESENT-011 | Options icon affordance |

### EPIC-MEDIA — Media Resolution & Processing

| ID | Feature |
|---|---|
| FEAT-MEDIA-001 | Resolve front and back box art |
| FEAT-MEDIA-002 | Resolve clear logo |
| FEAT-MEDIA-003 | Resolve background image (background, else screenshot, else default) |
| FEAT-MEDIA-004 | Resolve platform clear logo |
| FEAT-MEDIA-005 | Resolve community and user star-rating imagery |
| FEAT-MEDIA-006 | Resolve play-mode icon |
| FEAT-MEDIA-007 | Video preview playback |
| FEAT-MEDIA-008 | Bezel resolution and overlay |
| FEAT-MEDIA-009 † | Resolution-specific pre-scaled image cache |
| FEAT-MEDIA-010 † | Clear-logo transparent-border cropping |
| FEAT-MEDIA-011 † | Lazy, prioritised media hydration |
| FEAT-MEDIA-012 | Video preview volume control |
| FEAT-MEDIA-013 † | Default box-art placeholder generation |

### EPIC-LAUNCH — Game Launching

| ID | Feature |
|---|---|
| FEAT-LAUNCH-001 | Launch the selected game |
| FEAT-LAUNCH-002 | Choose between alternate versions / additional applications |
| FEAT-LAUNCH-003 | Bypass the detail overlay and launch directly |
| FEAT-LAUNCH-004 † | Suppress video and animation while a game is running |
| FEAT-LAUNCH-005 † | Record last-played date on launch |

### EPIC-CURATE — Favourites & Ratings

| ID | Feature |
|---|---|
| FEAT-CURATE-001 | Toggle a game as favourite |
| FEAT-CURATE-002 | Set a star rating |
| FEAT-CURATE-003 † | Persist favourite and rating back to LaunchBox |
| FEAT-CURATE-004 † | Refresh affected lists after a curation change |

### EPIC-ATTRACT — Attract Mode

| ID | Feature |
|---|---|
| FEAT-ATTRACT-001 | Enter attract mode after an idle period |
| FEAT-ATTRACT-002 | Cycle random games with fades and a slow pan |
| FEAT-ATTRACT-003 | Exit attract mode on any input |
| FEAT-ATTRACT-004 † | Suppress attract mode during video playback and gameplay |

### EPIC-INPUT — Input & Navigation

| ID | Feature |
|---|---|
| FEAT-INPUT-001 | Directional and action input contract with Big Box |
| FEAT-INPUT-002 | Remappable Page Up / Page Down functions |
| FEAT-INPUT-003 | Held-key behaviour |
| FEAT-INPUT-004 | Adjust video preview volume from input |
| FEAT-INPUT-005 | Escape routing and handing control back to Big Box |
| FEAT-INPUT-006 † | Route input through the state machine |

### EPIC-CONFIG — Configuration

| ID | Feature |
|---|---|
| FEAT-CONFIG-001 | Eclipse settings model with defaults |
| FEAT-CONFIG-002 | Settings window (desktop LaunchBox) |
| FEAT-CONFIG-003 | Custom list definition editor |
| FEAT-CONFIG-004 † | Settings and custom-list persistence |
| FEAT-CONFIG-005 † | Backup on write |
| FEAT-CONFIG-006 | Reorder custom lists |

### EPIC-INTEGRATE — LaunchBox & Plugin Integration

| ID | Feature |
|---|---|
| FEAT-INTEGRATE-001 | Big Box theme element plugin registration |
| FEAT-INTEGRATE-002 | Desktop "Manage eclipse" system menu item |
| FEAT-INTEGRATE-003 | Theme and startup-theme packaging |
| FEAT-INTEGRATE-004 † | Read the LaunchBox game catalogue, playlists and emulators |
| FEAT-INTEGRATE-005 † | Startup and loading sequence |
| FEAT-INTEGRATE-006 † | Resolve LaunchBox and plugin directory paths |
| FEAT-INTEGRATE-007 † | Diagnostics log |

---

## Functional dependencies

```
                     EPIC-INTEGRATE
              (catalogue, paths, plugin host)
                            │
                            ▼
                     FEAT-BROWSE-014
                  (in-memory game index)
                    │              │
        ┌───────────┘              └───────────┐
        ▼                                      ▼
   EPIC-BROWSE  ◄────── results ────────  EPIC-SEARCH
        │                                      │
        └──────────────┬───────────────────────┘
                       ▼
                  EPIC-PRESENT ◄──── media ──── EPIC-MEDIA
                       │
        ┌──────────────┼──────────────┐
        ▼              ▼              ▼
   EPIC-LAUNCH   EPIC-CURATE    EPIC-ATTRACT
                       │
                       └──► triggers list rebuild ──► EPIC-BROWSE

   EPIC-INPUT  drives every epic above via the state machine
   EPIC-CONFIG parameterises every epic above
```

Key dependency facts:

* **FEAT-BROWSE-014 is the substrate.** The in-memory game index feeds category lists,
  custom lists, random selection, voice-search matching and attract mode. It is built
  once at startup and rebuilt in full whenever curation changes a game.
* **EPIC-SEARCH produces EPIC-BROWSE output.** Both voice and text search results are ordinary
  game lists, so everything downstream of browsing works unchanged. Text search goes further and
  installs them live, on every keystroke, so the search screen has no result row of its own.
* **EPIC-CURATE feeds back into EPIC-BROWSE.** This is the only backward edge in the
  product, and it is the reason position restoration (FEAT-BROWSE-010) exists.
* **EPIC-CONFIG is read at first use, not continuously.** Some settings apply live,
  others only on restart — see [configuration.md](features/configuration.md) and `OQ-011`.

---

## Where to go next

* Working on a capability → open its file in [features/](features/).
* Planning a change → [TRACEABILITY.md](TRACEABILITY.md) for what debt and backlog work
  touches it.
* Need to prove behaviour survived → [VERIFICATION.md](VERIFICATION.md).
* Found something you cannot explain → check [UNRESOLVED.md](UNRESOLVED.md) before
  assuming it is a bug.
