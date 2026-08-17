# Eclipse — Development Setup

How to build Eclipse, deploy it to your own LaunchBox installation for testing, and what
to watch out for.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Visual Studio 2026 (Community is fine) | Or the .NET 10 SDK plus MSBuild |
| .NET 10 SDK | Eclipse targets `net10.0-windows`, x64 |
| LaunchBox / Big Box 14.x | Needed to *run* Eclipse, **not** to build it |

You do **not** need LaunchBox installed to build. The LaunchBox 14 plugin contract
assembly is vendored at `Eclipse/lib/LaunchBox14/Unbroken.LaunchBox.Plugins.dll`, so a
clean clone builds on any machine.

## Build

```
git clone https://github.com/AtomFry/Eclipse
cd Eclipse
msbuild Eclipse.sln -t:Restore
msbuild Eclipse.sln -p:Configuration=Release
```

Or just open `Eclipse.sln` in Visual Studio and build. Debug and Release both build with
zero warnings; treat any warning as a regression.

Output lands in `Eclipse/bin/<Config>/net10.0-windows/win-x64/`, and the deployable
payload is staged under `.../Eclipse/LaunchBox/`, mirroring the folder layout of a
LaunchBox installation:

```
bin/Release/net10.0-windows/win-x64/Eclipse/LaunchBox/
├── Plugins/Eclipse/          Eclipse.dll, its dependencies, and bundled media
├── Themes/Eclipse/           the Big Box theme
└── StartupThemes/Eclipse/    the startup/shutdown theme
```

---

## Deploying to your LaunchBox for testing

By default the build **stages only** — it copies nothing into LaunchBox, and tells you so:

```
Eclipse staged to ...\bin\Release\net10.0-windows\win-x64\Eclipse\ (not deployed).
To deploy to LaunchBox on every build, copy Eclipse.csproj.user.template to
Eclipse.csproj.user and set LaunchBoxDeployRoot - see docs/DEVELOPMENT.md
```

To turn on automatic deployment, copy the template and edit one line:

```
cd Eclipse
copy Eclipse.csproj.user.template Eclipse.csproj.user
```

Then set the path to *your* LaunchBox root — the folder containing `Core`, `Plugins`,
`Themes` and `Data`:

```xml
<LaunchBoxDeployRoot>D:\LaunchBox</LaunchBoxDeployRoot>
```

That's the whole setup. Building in Visual Studio now copies the staged payload into your
LaunchBox installation, and the build reports where it went:

```
Eclipse deployed to D:\LaunchBox
```

`Eclipse.csproj.user.template` is committed; your `Eclipse.csproj.user` is git-ignored.
If you mistype the path, the build **warns** and stages without deploying rather than
failing silently.

### Why a `.csproj.user` file

* It is **git-ignored** (`.gitignore` line 9: `*.user`), so your machine's path never
  reaches the repository. Eclipse previously carried a hard-coded deploy path in a
  post-build event, which broke for every developer except the original author.
* Visual Studio and MSBuild import it automatically — no command-line arguments, no
  environment variables, no build configuration switching.
* Deleting the file restores staging-only behaviour.

### Deploying from the command line instead

If you would rather not keep a `.user` file, pass the property per build:

```
msbuild Eclipse.sln -t:Build -p:Configuration=Release -p:LaunchBoxDeployRoot="D:\LaunchBox"
```

### Verifying it worked

The build prints `Eclipse deployed to <path>`. To double-check, confirm the timestamp
moved on `<LaunchBox>\Plugins\Eclipse\Eclipse.dll`, then start Big Box with the Eclipse
theme selected.

---

## Gotchas

**Close LaunchBox and Big Box before building.** A running host holds `Eclipse.dll` open
and the copy fails partway, leaving a mixed set of files. If a deploy fails oddly, this
is why.

**Never copy the payload with `xcopy /D`.** That flag means *"copy only if the source is
newer"*, and package files carry their original NuGet timestamps rather than build
timestamps. This has already caused a real bug: `System.Speech.dll`'s platform stub is
8 seconds *newer* than the correct Windows build inside the same package, so `/D`
silently skipped the one file that mattered and voice search stayed broken through a
"successful" deploy. Use the `LaunchBoxDeployRoot` property, which copies
unconditionally.

**Deployment copies, it never deletes.** The deploy step mirrors files into LaunchBox but
does not prune anything that is no longer part of the payload. If a file stops shipping,
it lingers in your LaunchBox install — and in end users' installs across an upgrade —
until it is deleted by hand. After changing what the payload contains, check the target
folder rather than assuming it matches the stage.

**Eclipse must never ship certain files.** Two of these have already caused total feature
loss in production:

| Must not be in the deployed plugin folder | If it is |
|---|---|
| `Unbroken.LaunchBox.Plugins.dll` | Two contract assemblies load; the interface cast fails and Eclipse does not load at all |
| `manifest.json` | LaunchBox 14 treats Eclipse as a managed plugin and **Tools → Manage eclipse** disappears |

A third is silent rather than fatal: `System.Speech.dll` must be the **Windows** build
(~687 KB), not the platform-agnostic stub (~310 KB). The stub throws
`PlatformNotSupportedException` on every call, so voice search fails while everything
else works. The `RuntimeIdentifier` in the project file selects the correct one — do not
remove it.

These rules are documented as `RULE-INTEGRATE-011`, `RULE-INTEGRATE-012` and
`RULE-MEDIA-*` in [features/launchbox-integration.md](features/launchbox-integration.md),
with `VER-INTEGRATE-001` as the check that should eventually be automated.

**The assembly must stay named `Eclipse`.** The Big Box theme resolves it by name
(`assembly=Eclipse`), and every embedded image is addressed through
`pack://application:,,,/Eclipse;component/...`. Renaming it breaks the theme and all
embedded artwork, with no compile-time error.

---

## Release process

Releases are currently **manual**:

1. Build Release.
2. Zip the staged `Eclipse` folder (`bin/Release/net10.0-windows/win-x64/Eclipse`). The
   resulting archive contains `Eclipse/LaunchBox/{Plugins,Themes,StartupThemes}`, which
   matches the installation instructions in
   `Eclipse/LaunchBox/Plugins/Eclipse/README.txt`.
3. Upload to the [LaunchBox forums file page](https://forums.launchbox-app.com/files/file/3220-eclipse/).
4. Tag the commit (`v0.0.x`).

### Known problems with this process

These are recorded rather than solved. Automating them is proposed but not yet scheduled.

| Problem | Detail |
|---|---|
| **Version is not single-sourced** | `AssemblyInfo.cs` says `1.0.0.0` for every build ever shipped, while releases are tagged `v0.0.x` and the plugin README references `0.0.10`. Nothing in a deployed build identifies which release it is. |
| **Payload gates are not enforced** | The three "must not ship" rules above are checked by memory, not by the build. Two have already been violated in shipped or deployed builds. |
| **Developer artifacts are shipped** | `Themes/Eclipse/BigBoxTheme.csproj` and `BigBoxTheme.sln` are Visual Studio files that end up in users' Themes folder. See `DEAD-010` in [UNRESOLVED.md](UNRESOLVED.md). (`Eclipse.deps.json` was removed from the payload — LaunchBox has no `AssemblyDependencyResolver`, so it was never read.) |
| **No CI** | Nothing verifies that a clean clone builds. Because the plugin contract is vendored, a Windows CI runner *could* build and package without LaunchBox installed. |

---

## Repository layout

```
Eclipse.sln
Eclipse/
├── Eclipse.csproj              SDK-style, net10.0-windows, win-x64
├── Eclipse.csproj.user.template  committed starting point — copy it, don't edit it
├── Eclipse.csproj.user           your local deploy path (git-ignored, your copy)
├── lib/LaunchBox14/            vendored LaunchBox 14 plugin contract (reference only)
├── Unbroken.LaunchBox.Plugins.dll   historical netstandard2.0 contract — reference material,
│                                    no longer referenced by the build. Do not delete.
├── Eclipse/                    the deployable payload (media, theme, startup theme, README)
├── View/ ViewModel/ State/ Service/ Models/ Helpers/ Converters/ Plugins/
docs/                           product and engineering documentation
```

Note the two nested `Eclipse` folders: `Eclipse/` is the project, and `Eclipse/Eclipse/`
is the payload staged into a LaunchBox installation.

---

## Before you change behaviour

Eclipse is being modernized against a documented behavioural baseline. Before changing
anything, read the relevant capability file in [features/](features/) — the behavioural
rules there are the contract a refactor must preserve. If you need to change one, that is
a product change, not a refactor; say so explicitly.

Start at [FEATURES.md](FEATURES.md).
