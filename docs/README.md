# Eclipse Documentation

This directory is the **functional source of truth** for Eclipse. It describes what
Eclipse does, the rules that make those behaviours work, where they currently live,
and how to verify they still work after a change.

It was written against the LaunchBox 14 / .NET 10 baseline, which is the known-good
behavioural reference for all modernization work.

## What is here

| Document | Purpose | Audience |
|---|---|---|
| [FEATURES.md](FEATURES.md) | Product map: epics, complete feature inventory, functional dependencies | Everyone |
| [features/](features/) | One file per epic — features, behavioural rules, implementation, debt, verification | Developers |
| [VERIFICATION.md](VERIFICATION.md) | How behaviour is verified; verification matrix; characterization-test candidates | Developers |
| [TRACEABILITY.md](TRACEABILITY.md) | Bidirectional feature ↔ technical-debt ↔ backlog mapping | Maintainers |
| [UNRESOLVED.md](UNRESOLVED.md) | Suspected dead code, uncertain behaviour, open questions | Maintainers |

## The rule that keeps this useful

Every epic file is split into two halves:

* **Behaviour** — what Eclipse does. This must stay true after refactoring.
* **Current implementation** — where that behaviour lives today. This is expected to
  go stale, and that is fine.

When you change the architecture, update the implementation half. If you find yourself
needing to change the behaviour half, you are changing the product — say so explicitly
and record it.

## Identifier scheme

| Prefix | Meaning | Example | Defined in |
|---|---|---|---|
| `EPIC-*` | Product capability area | `EPIC-BROWSE` | this directory |
| `FEAT-*-NNN` | User-facing feature | `FEAT-BROWSE-004` | this directory |
| `RULE-*-NNN` | Behavioural rule that must survive refactoring | `RULE-BROWSE-011` | this directory |
| `VER-*-NNN` | Verification scenario | `VER-BROWSE-003` | [VERIFICATION.md](VERIFICATION.md) |
| `OQ-NNN` | Open question — behaviour not established from the repo | `OQ-007` | [UNRESOLVED.md](UNRESOLVED.md) |
| `DEAD-NNN` | Suspected dead or obsolete code | `DEAD-004` | [UNRESOLVED.md](UNRESOLVED.md) |
| `M-*`, `S-*`, `C-*` | Architecture assessment findings | `S-1` | architecture assessment |
| `B-NN` | Modernization backlog items | `B-14` | modernization backlog |

IDs are **stable**. Once assigned, an ID is never reused or renumbered. If a feature is
removed, its ID is retired with a note rather than recycled.

## Scope and honesty rules

These documents describe **observed current behaviour**, established by reading the
implementation. They deliberately distinguish:

* **Established behaviour** — intentional, relied upon, safe to treat as a contract.
* **Accidental behaviour** — works, but appears to be a side effect rather than a design
  decision. Flagged inline and listed in [UNRESOLVED.md](UNRESOLVED.md).
* **Open questions** — the repository does not contain enough evidence to say what was
  intended. These are *not* guessed at.

Where documentation and implementation disagree, the implementation is described and the
discrepancy is recorded.

## For users

Eclipse is a plugin and theme for [LaunchBox](https://www.launchbox-app.com/)'s Big Box
mode. It presents your game library in a Netflix-style interface with video previews,
voice search and a random-game feature. Installation and usage instructions ship with the
plugin in `Eclipse/LaunchBox/Plugins/Eclipse/README.txt`; [FEATURES.md](FEATURES.md)
describes what the product actually does in more detail.
