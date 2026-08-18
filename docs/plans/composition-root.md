# Deferred plan — Composition root & test seam

> **Status: deferred.** Scope agreed (all four stages), not started. This document exists so
> the design and its evidence survive until we pick it up.
>
> Addresses `RC-2` and backlog items `B-01` (test project) and `B-09` (dependency injection).
>
> **Superseded in part (2026-08-17).** The game-index refactor replaced `GameBagService` with
> `GameCatalog` and `VoiceSearchIndex`. Singleton references below naming `GameBagService`
> should be read as those two services; the dependency argument is unchanged. The net effect
> is one more singleton than the count below, not one fewer.

## Context

Eclipse cannot be observed or exercised without a running Big Box. Dependencies are *taken*
from 13 `public static Instance` singletons rather than *given*, so nothing that touches
game data, settings or paths can be constructed in isolation. 33 of 87 source files reach a
static singleton directly; most of the rest reach one transitively through
`EclipseStateContext` → `MainWindowViewModel` → `GameBagService.Instance`.

The cost is measured, not theoretical:

- **Zero automated tests in five years** — for most of the code it isn't possible.
- **The LaunchBox 14 runtime investigation cost days** and produced four falsified
  hypotheses; answering basic questions ("does the plugin type load", "does `GetTypes()`
  throw") required building four throwaway console harnesses.
- **Two latent bugs found in a single evening** — `StringLengthConverter`'s self-assignment
  and `DirectoryInfoHelper.BigBoxSettingsFile`'s infinite recursion — both in trivially
  testable code, both years old, both surviving because there was no test project at all.
- **The architecture assessment was wrong in four places** (`M-1`, `S-10`, `S-11`,
  `DEAD-008`/`012`) because the code could be read but not run.

Every remaining backlog item is behaviour-preserving, and the only current proof of
preservation is manual play-testing — the same mechanism that let the `manifest.json` and
`System.Speech` regressions reach a working install unnoticed.

**Outcome:** a composition root and constructor injection, so services can be substituted
and tested. Runtime behaviour unchanged — that is the acceptance criterion.

## The constraint that shapes the design

Eclipse has no `Main()`. All three entry points are constructed **by the host** with
parameterless constructors:

| Entry point | Constructed by |
|---|---|
| `View/MainWindowView.xaml.cs` | WPF, from the theme's `<Eclipse:MainWindowView/>` |
| `View/MainWindowViewModel.cs` | WPF, from `<local:MainWindowViewModel/>` in `MainWindowView.xaml:15` |
| `Plugins/EclipseSettingsMenuItem.cs` | LaunchBox, via reflection |

The container cannot be threaded in from above. Therefore:

- **A lazily-initialised static composition root**, resolved at those three boundaries only.
- **Constructor injection everywhere below them.**

Service location is confined to the boundary the host owns. This is the standard add-in
pattern, not a compromise worth engineering around.

## Design

New folder `Eclipse/Composition/`:

- **`EclipseServices.cs`** — static class exposing `IServiceProvider Current`, built on first
  access via `Lazy<T>`; plus an `internal Reset(IServiceProvider)` for tests.
- **`ServiceRegistration.cs`** — one `AddEclipse(this IServiceCollection)` extension
  registering every service, all singleton-scoped so lifetimes are preserved.

**Package: `Microsoft.Extensions.DependencyInjection`. Verified safe** — it and
`.Abstractions`, `Logging` and `Options` are on LaunchBox's trusted-assembly list *and*
physically present in `<LaunchBox>\Core`. Unlike Prism (`C-7`), it resolves in the host.
Do **not** use `Microsoft.Extensions.Configuration` — only its `.Abstractions` is present.

## Stages

One commit per stage; each builds clean in Debug and Release and is deployed and
smoke-tested before the next begins.

### Stage 0 — Test project (`B-01`)

`Eclipse.Tests/` at repo root — outside `Eclipse/Eclipse/`, so the payload glob
`$(MSBuildProjectDirectory)\Eclipse\**\*` cannot sweep it in. `net10.0-windows`, xUnit.

Characterization tests for what is testable **today**, no DI required:

- `Models/GameTitleGrammarBuilder.cs` — colon/slash splitting, roman numerals (including
  the deliberate `X` exclusion), noise words, contiguous-run phrase generation
- `Models/GameMatch.cs` — `SetupVoiceMatchPercentage` scoring arithmetic
- `Models/ListCycle.cs` — window cycling and wrap behaviour
- `Converters/` — the generic base converters

Verify the staged plugin payload is byte-identical before and after adding the project.

### Stage 1 — Composition root, no behaviour change

Add `EclipseServices` and `ServiceRegistration`; register all 13 singletons as singletons.
Rewrite each existing `Instance` property as a container lookup:

```csharp
public static GameBagService Instance => EclipseServices.Current.GetRequiredService<GameBagService>();
```

No call sites change. This is the reversible half of the work.

Files — the 13 singleton declarations: `Helpers/DirectoryInfoHelper.cs`,
`DisplayInfoHelper.cs`, `EventAggregatorHelper.cs`, `OpacityBrushHelper.cs`,
`Service/AttractModeService.cs`, `BezelService.cs`, `CustomListDefinitionDataProvider.cs`
(contains **four** types — see `C-8`), `GameBagService.cs`, `ImageScaler.cs`,
`OptionListService.cs`, `PlaylistGameService.cs`, `SpeechRecognizer.cs`,
`State/KeyStrategy/IKeyStrategy.cs` (`KeyStrategyCache`).

### Stage 2 — Constructor injection, leaf services

No Eclipse dependencies first: `DisplayInfoHelper`, `OpacityBrushHelper`,
`EventAggregatorHelper`, then `DirectoryInfoHelper` (depends only on `DisplayInfoHelper`).
`Instance` remains as a shim.

### Stage 3 — Constructor injection, dependent services

Up the graph: `ImageScaler` → `DirectoryInfoHelper`; `BezelService` → `DirectoryInfoHelper`;
`EclipseSettingsDataService` / `CustomListDefinitionDataService` → `DirectoryInfoHelper`;
`OptionListService` and `KeyStrategyCache` → settings; `PlaylistGameService` → LaunchBox
data; `GameBagService` → most of the above; `SpeechRecognizerService` → `GameBagService`.

`AttractModeService` is deliberately **last and hardest** — it holds a concrete
`MainWindowView` (`M-8`). Register it, but leave the view reference alone; untangling that
is `B-18`, not this plan.

### Stage 4 — Resolve at the boundaries, delete the shims

Resolve from `EclipseServices.Current` in the three entry points, then delete every
`public static Instance`. `MainWindowViewModel` keeps its parameterless constructor (XAML
requires it) and resolves what it needs from the root.

**This is the real change** — ~33 files, call sites rewritten. Review it on its own.

## Verification

**The acceptance criterion is an unchanged initialisation-order trace.** Today the order is
implicit in first-touch and genuinely subtle: `MainWindowViewModel`'s constructor runs
*before* `MainWindowView`'s body because XAML builds the DataContext first, and
`GameBagService.GameBag` lazily triggers image pre-scaling, which reads settings, which
creates folders on disk.

1. **Before Stage 0**, add temporary trace logging to every singleton constructor and capture
   the sequence on a cold start with an empty image cache. Commit that trace as the reference.
2. Re-capture after every stage. **The order must match exactly.** A container that
   constructs eagerly will reorder it — register lazily and verify.
3. Full manual capability sweep after Stages 1 and 4: browse several categories, navigate,
   open the detail overlay, favourite, rate, launch and exit a game, voice search, idle into
   attract mode, open the settings window.
4. Confirm the payload gains only `Microsoft.Extensions.DependencyInjection*.dll` and still
   contains no `Unbroken.LaunchBox.Plugins.dll` and no `manifest.json`.
5. `Eclipse.txt` gains no new entries across the sweep.

If a stage produces an order mismatch or a behaviour change, stop there — earlier stages
remain shippable on their own.

## Risks

| Risk | Mitigation |
|---|---|
| **Initialisation order changes** — highest risk; folders and cache could be created at a different time | Lazy registration; before/after trace is the acceptance criterion |
| XAML `<local:MainWindowViewModel/>` needs a parameterless ctor | Keep it; resolve from the root inside the ctor rather than injecting |
| A new package fails to resolve in the host, as Prism does | Verified on the TPA list *and* in `Core` before starting |
| Big-bang conversion becomes unreviewable | Four stages; Stages 0–3 change no call sites |
| `AttractModeService` holds the concrete view | Out of scope — registered but untouched (`B-18`) |

## Out of scope

`B-11`/`B-12` (LaunchBox adapter and Eclipse-owned game model), `B-18`/`B-19` (severing the
view from the state machine), any change to `MainWindowViewModel`'s size or
responsibilities, and any behaviour change whatsoever.

DI alone does **not** make everything testable — code calling `PluginHelper` directly stays
untestable until `B-11`. This plan delivers the seam; `B-11` makes the game-data code
testable through it.
