# Eclipse — Claude Code Instructions

## Project

Eclipse is a LaunchBox/BigBox theme-element plugin implemented as a WPF
.NET Framework application. The plugin is hosted inside BigBox.

The repository contains:
- The Eclipse plugin/application
- The BigBox theme that hosts the plugin
- The Eclipse configuration UI
- Supporting assets and LaunchBox integration code

## Development Principles

### Preserve behavior first

During modernization, preserve existing Eclipse behavior unless a task
explicitly calls for a behavior change.

Treat the current implementation as the behavioral baseline.

### Incremental modernization

Modernization must happen in small, independently buildable steps.

Do not combine framework upgrades, dependency upgrades, architectural
refactoring, and feature work into a single change.

### Verify before changing

Before making significant changes:
1. Inspect the existing implementation.
2. Explain the proposed change and its expected impact.
3. Make the smallest reasonable change.
4. Build and test.
5. Report the result.

### Do not assume

LaunchBox/BigBox is the host application. Do not assume that standard
standalone WPF/.NET application practices are compatible with the
LaunchBox plugin environment.

Verify LaunchBox API and runtime requirements before changing:
- target framework
- LaunchBox plugin references
- assembly loading behavior
- WPF integration
- plugin interfaces

### Build integrity

The repository must remain buildable after each modernization milestone.

Do not silently upgrade dependencies or change target frameworks as part
of unrelated fixes.

### Testing

Prefer automated tests for logic that can be separated from the WPF/BigBox
host.

For host-dependent behavior, use a local LaunchBox installation for
manual verification.

### Git

Do not commit, push, create branches, or modify repository history unless
explicitly requested.

Before significant changes, report the files that will be modified.

### Scope

Stay within the requested task.

Avoid opportunistic refactoring, cleanup, renaming, formatting changes,
or dependency upgrades unless they are required by the task.