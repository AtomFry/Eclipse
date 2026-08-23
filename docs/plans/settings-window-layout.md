# Plan — The settings window: tabs, rows and the boilerplate between them

**Status: Stages 1 and 2 complete. Stage 3 piloted on one tab, awaiting a visual check before
the remaining five.**

| Stage | State |
|---|---|
| 1 — brushes and implicit styles | **Done.** 224 repeated literals gone; seven brushes named. |
| 2 — section header | **Done.** Nine copies of the `Border`+`TextBlock` collapsed to two styles. |
| 3 — row styles | **Done, all six list-shaped tabs.** Custom lists keeps its `Grid` by design — a `DataGrid` beside a button column is a real two-dimensional layout. Design corrected — see the note in the stage. |
| 3b — `SliderRow` | **Done.** All 18 sliders. Added on top of the plan; see L7 and the slider notes. |
| 4 — tab templating | **Done.** Seven `DataTemplate`s and one `ContentControl`. |
| 5 — documentation | Not started. |

| Measure | Before | After |
|---|---|---|
| XAML lines | 940 | 877 |
| `RowDefinition` elements | 161 | 22 |
| `ColumnDefinition` elements | 58 | 17 |
| Hand-written `Grid.Row=` | 100 | 27 |
| `<Style>` elements | 0 | 14 |
| `EclipseSettingsViewModel` lines | 976 | 487 |

The line count barely moved, which is the least interesting number here: about 250 lines of what
remains is now reusable styles and templates, and the per-setting markup that replaced the rest
carries no indices. What actually changed is the cost of an edit — a setting is one element in
one place, and a tab is one `DataTemplate`.

The view model halved across this plan and `settings-refactor.md` together: 45 delegating
properties, a 44-line self-assigning initialiser, seven `Visibility` properties and a 32-line
`UpdateTabVisibility` switch, all gone.

Follows `settings-refactor.md`, which dealt with how settings are stored, saved and bound. This
one deals with how the editor is *built*. No backlog item covers it; it came out of a question
about why the window felt clunky to work on despite being fine to use.

## What was asked

> It works great from a user experience but from a development experience, I remember when I
> implemented it, it felt very clunky and I felt surely there's a better way to handle the
> tabbing and the layout of the settings. But I'd like to keep the same functional design.

So: **the window must look and behave exactly as it does now.** Nothing here is a redesign. The
target is the cost of changing it.

---

## How it works today

`EclipseSettingsView.xaml` is **940 lines with zero `<Style>` elements.**

The window is a 3×5 grid. Row 1, column 1 holds a `ListBox` of tab names; row 1, column 3 holds
**seven full-size `Grid` panels stacked on top of each other**, each gated on its own
`Visibility` property. All seven are constructed and live at all times; six are `Collapsed`.

Each tab panel is a header `Border` plus an inner `Grid` with a block of `Auto` rows and eight
star columns, and every control is placed by hand-written `Grid.Row` / `Grid.Column` /
`Grid.ColumnSpan`.

---

## Findings

### L1 — The row definitions are a fixed scaffold that controls are indexed into

161 `RowDefinition` elements, 137 of them `Height="Auto"`. Six of the seven tabs declare
**25 rows regardless of their content**:

| Tab | Rows declared | Highest row used |
|---|---|---|
| List settings | 25 | 4 |
| Input | 25 | 4 |
| Versions | 25 | 6 |
| Other | 25 | 12 |
| Custom lists | 5 | 1 |
| Box margin | 25 | 12 |
| Screen saver | 28 | 24 |

A block of twenty-five `Auto` rows was pasted into each tab as headroom. The Box margin tab
starts at row 1, leaving row 0 empty.

**This is the clunkiness.** There are 100 `Grid.Row=` attributes. Inserting a setting in the
middle of a tab means renumbering every one below it, and nothing catches a mistake — two
controls assigned the same row silently overlap. Reordering two settings means editing two
numbers and hoping.

### L2 — The eight columns are a measuring stick, not a layout

Every tab declares eight equal columns. How they are used:

| Placement | Controls | Means |
|---|---|---|
| `Column="0" ColumnSpan="8"` | 39 | full width |
| `Column="0" ColumnSpan="4"` | 23 | half width |
| `Column="1" ColumnSpan="3"` | 5 | field beside a label |
| everything else | 2 | — |

62 of ~70 positioned controls resolve to "all of it" or "half of it". Two columns — `Auto` for
the label and `*` for the field — would say the same thing, and would size the label column to
the labels rather than to an eighth of the window.

### L3 — Tabs are a hand-rolled `TabControl` spread across five places

Adding a tab means editing all of:

1. `EclipseSettingsTabs` — a string constant
2. `InitializeTabPages()` — a `TabPages.Add(...)`
3. A `Visibility` property with a backing field — seven of them, about 70 lines
4. `UpdateTabVisibility()` — 32 lines that set all seven to `Collapsed`, then switch on a string
   to set one `Visible`
5. The panel itself in the XAML

Four of those five are bookkeeping. None of the seven panel `x:Name`s
(`ListSettingsGrid`, `InputSettingsGrid`, …) is referenced from code-behind — they are
decorative.

### L4 — No styles, so every visual value is a literal repeated per control

| Literal | Occurrences |
|---|---|
| `Foreground="#D3D3D5"` | 110 |
| `Margin="10,2"` | 68 |
| `Background="#343542"` | 46 |

Changing the colour scheme is a find-and-replace across 940 lines. There is no resource
dictionary and no brush is named.

Measured against the three field types, the repetition is **completely uniform**:

| Type | Count | `Margin="10,2"` | `Foreground` | `Background` | `H/V Alignment="Stretch"` |
|---|---|---|---|---|---|
| `CheckBox` | 23 | 23 | 23 | 23 | 23 |
| `ComboBox` | 4 | 4 | 4 | 4 | 4 |
| `Slider` | 18 | 18 | 18 | 18 | 18 |
| `Label` | 53 | 4 | 52 | 0 | 0 |

The three field types can take an implicit style covering all five attributes with no visual
change at all. `Label` varies in margin and alignment, so its style can only carry `Foreground` —
and the one label that is not `#D3D3D5` (the window title, `#F0F0F0`) sets it locally, which
beats a style setter.

### L5 — The section header is copy-pasted seven times

```xml
<Border Grid.Row="0" Height="35" Background="#2A2B34"
        HorizontalAlignment="Stretch" VerticalAlignment="Top" Margin="5,5,5,10">
    <TextBlock Text="List settings"
               HorizontalAlignment="Center" TextAlignment="Center" VerticalAlignment="Center"
               Background="#2A2B34" Foreground="#D3D3D5" />
</Border>
```

Once per tab, plus again for sub-sections inside the screen saver tab.

---

## Proposal

Four changes, none of which alters a pixel:

1. **A resource dictionary** — named brushes and implicit styles for the three uniform field
   types, plus `Foreground` for `Label`.
2. **A `SectionHeader` control** for L5.
3. **A `SettingRow` control, and `StackPanel` + `Grid.IsSharedSizeScope` instead of row indices.**
   Settings sit in document order; there are no row numbers to maintain. Labels still align
   across rows, because that is what `SharedSizeGroup` is for.
4. **One `ContentControl` with seven `DataTemplate`s** instead of seven gated panels — deleting
   the seven `Visibility` properties and the 32-line switch, and realising only the selected tab.

What a setting looks like before and after:

```xml
<!-- now: 10 lines, two hand-maintained indices -->
<Label Grid.Row="0" Grid.Column="0" Content="Default group" Foreground="#D3D3D5"
       HorizontalAlignment="Left" VerticalAlignment="Center" Margin="10,2"/>
<ComboBox Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="3" Margin="10,2"
          HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
          Background="#343542" Foreground="#D3D3D5"
          ToolTip="Specify which list category group to load by default on startup"
          SelectedItem="{Binding Settings.DefaultListCategoryType, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
          ItemsSource="{Binding DefaultListTypes}"/>

<!-- after -->
<local:SettingRow Label="Default group">
    <ComboBox ToolTip="Specify which list category group to load by default on startup"
              SelectedItem="{Binding Settings.DefaultListCategoryType, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
              ItemsSource="{Binding DefaultListTypes}"/>
</local:SettingRow>
```

### The tab-templating detail that matters

Bind the `ContentControl`'s `Content` to the **view model**, not to the selected tab name, and
switch `ContentTemplate` on the name. Bind it to the name and the DataContext inside each
template becomes a `string`, and every binding in the tab breaks silently.

```xml
<ContentControl Grid.Row="1" Grid.Column="3" Content="{Binding}">
    <ContentControl.Style>
        <Style TargetType="ContentControl">
            <Style.Triggers>
                <DataTrigger Binding="{Binding SelectedTabPage}" Value="Lists">
                    <Setter Property="ContentTemplate" Value="{StaticResource ListSettingsTab}"/>
                </DataTrigger>
                <!-- …six more -->
            </Style.Triggers>
        </Style>
    </ContentControl.Style>
</ContentControl>
```

Done this way, every existing binding inside the tabs keeps working unchanged.

### Why not a real `TabControl`

`TabControl` with `TabStripPlacement="Left"` is the textbook answer and would delete the
`ListBox`, the `TabPages` collection and `SelectedTabPage` as well. It is not proposed because
re-skinning `TabControl` and `TabItem` to match the current chrome exactly is a visual-fidelity
job with real risk, and the brief is that the current look is right. The `ContentControl` route
gets the whole C# saving with none of that exposure. Revisit only if the chrome is ever
redesigned anyway.

### Why not generate the window from setting descriptors

The tempting answer is an `ItemsControl` over a list of setting descriptors. It would delete the
most XAML and it would break the functional design: it cannot express the per-control tooltips,
the slider ranges and tick frequencies, the enum item sources, the composite *"Start after: N
second(s) of inactivity"* caption, or the live margin preview. Every one of those would come back
as an escape hatch in a descriptor model. The tabs are hand-designed and should stay explicit;
the problem is the boilerplate around them, not that they are written out.

---

## Stages

Ordered so each one makes the next smaller. Every stage builds and is checked by opening the
window and looking at the tab that changed.

### Stage 1 — Brushes and implicit styles (L4)

A `ResourceDictionary` in `Window.Resources`:

* Named brushes for the six colours in use: `#2A2B34` window/panel, `#3F404E` panel mid,
  `#4D4F61` panel border, `#343542` field, `#D3D3D5` text, `#F0F0F0` title.
* Implicit styles for `CheckBox`, `ComboBox` and `Slider` carrying all five uniform attributes.
* An implicit `Label` style carrying `Foreground` only.

Deliberately **no** implicit `TextBlock` or `Button` style: `DataGridTextColumn` generates
`TextBlock`s and would inherit one, and the custom-list buttons are currently unstyled WPF
defaults — restyling them would be a visual change, which is out of scope.

*Verification:* every tab looks identical. The custom lists `DataGrid` in particular.

### Stage 2 — `SectionHeader` control (L5)

A small `ContentControl` with a template matching the current `Border`+`TextBlock`. Seven call
sites collapse to `<local:SectionHeader Text="List settings"/>`.

### Stage 3 — Row styles and the end of row indices (L1, L2)

> **Corrected during implementation.** This stage originally specified `Auto` label columns with
> `SharedSizeGroup`. Measuring first showed that would move things: the label column is currently
> a fixed eighth of the window (~105px), while the widest label is nearer 85px, so shared sizing
> would shift every field left by ~20px and change its width. Nicer typography, but it fails the
> brief. The row styles keep the original 1/8 · 3/8 · 4/8 proportions instead.
>
> Three styles cover 67 of ~70 positioned controls, matching the three widths L2 found:
> `SettingRowStyle` (label + field), `HalfWidthRowStyle` (checkboxes), and full width — which in
> a `StackPanel` is just the default, so it needs no style at all.
>
> Shared sizing remains available as a deliberate visual change later, if the extra alignment is
> ever wanted.

Each tab's inner `Grid` becomes a `StackPanel`; controls go in document order with no indices.

**One tab per commit**, simplest first: List settings, Input, Versions, Other, Box margin, Screen
saver. Custom lists keeps its `Grid` — a `DataGrid` beside a button column is a real
two-dimensional layout, not a list of settings.

Checkboxes carry their own label in `Content`, so they go straight into the `StackPanel` without
a `SettingRow`.

### Stage 4 — Tab templating (L3)

Move the seven panels into `DataTemplate`s, add the `ContentControl`, and delete from
`EclipseSettingsViewModel`: seven `Visibility` properties, their backing fields, and
`UpdateTabVisibility()`. `TabPages`, `SelectedTabPage` and `EclipseSettingsTabs` stay — the
`ListBox` strip still needs them.

### Stage 5 — Documentation

Update `docs/features/configuration.md` and this file's status.

---

## Found during implementation — two defects that predate this plan

Both surfaced when the first tab was reviewed against an older screenshot, and neither is caused
by Stages 1–3. They are recorded here because this is where they were found and fixed.

### L6 — The window title could not fit in its own row

`Manage eclipse` was rendering as a sliver and then being painted over by the panel rectangles
below it. The arithmetic: the header row is 50px, the header grid's margin was `15,10,15,25`
leaving **15px** of content height, and a `Label` needs about 29px once its default `Padding="5"`
is counted. The overflow fell into row 1, and the two `Rectangle`s there are declared later in
document order, so they painted on top.

Fixed by giving the row its height back (`Margin="15,0,15,0"`), zeroing the label's padding and
pinning the icon's height so it does not scale up with the row.

### L7 — Dark fields did not survive the .NET Framework to .NET 10 move

.NET Framework 4.8 used the **Aero** theme; .NET Core and later ship **Aero2**. Aero2 hard-codes
parts of its control chrome that Aero derived from the control's own brushes, and this window is
built entirely on dark fields with light text. Two controls broke, in opposite directions, and
both ended up unreadable:

| Control | What Aero2 does | Symptom |
|---|---|---|
| `ComboBox` | Ignores `Background` entirely - the chrome is hard-coded light | `#D3D3D5` text on near-white; the selected value almost invisible |
| `CheckBox` | *Honours* `Background`, but paints the tick with a hard-coded dark glyph brush | Dark tick inside a dark box - checked and unchecked look identical |

Both are fixed with the smallest templates that keep the behaviour and take their colours from
the control's own `Background` and `Foreground`, so they follow the palette instead of fighting
it. `ComboBox` also needs a `ComboBoxItem` style, or the dropdown stays light.

**`Slider` is the remaining candidate** - 18 of them across the Margin and Screen saver tabs,
carrying the same `Background`/`Foreground` pair, and its track and thumb brushes are
theme-internal in Aero2 too. Not yet checked; look at those two tabs before assuming they are
fine.

### A process note

This plan's own first decision was *"Screenshots first. This whole plan is verified by 'it looks
the same', so a screenshot of each of the seven tabs before Stage 1 is the baseline."* That step
was skipped, and the only comparison available afterwards was a screenshot from a build old
enough to predate the Screen saver tab — a different framework and several refactors ago. Two of
the three differences it showed turned out not to be regressions at all. **Capture the baseline
before resuming Stage 3.**

---

## Verification

The window is trivially checkable, which is what makes this safe without tests.

| ID | Scenario | Stage |
|---|---|---|
| VER-CONFIG-013 | Open each of the seven tabs; confirm every control is in the same place, the same size and the same colour as before. | 1–4 |
| VER-CONFIG-014 | Drag each of the four box-front margin sliders; confirm the preview updates live. | 3 |
| VER-CONFIG-015 | On the screen saver tab, confirm every slider's caption reads back its value and that disabling the screen saver greys the dependent sliders. | 3 |
| VER-CONFIG-016 | Switch tabs repeatedly; confirm no flicker, and that a tab returns with the values previously typed into it. | 4 |
| VER-CONFIG-017 | Custom lists tab: confirm the `DataGrid`, the buttons and double-click-to-edit all behave as before. | 1, 3 |

Add a binding-error trace sweep to Stages 3 and 4; the trace must be clean.

---

## Risks

| Risk | Stage | Mitigation |
|---|---|---|
| An implicit style reaches a control it was not meant to — `DataGrid` internals especially. | 1 | No implicit `TextBlock` or `Button` style. Check the custom lists tab specifically. |
| A control had a locally-set value that differed from the style, and removing it changes the look. | 1 | The uniformity table above was measured, not assumed: the three field types are 100% consistent across all five attributes. |
| `SharedSizeGroup` changes the label column width, shifting fields horizontally. | 3 | The label column is currently 1/8 of the window; shared sizing makes it as wide as the widest label. Compare screenshots per tab; if it drifts, pin a `MinWidth`. |
| A binding breaks silently when moved into a `DataTemplate`. | 4 | `Content="{Binding}"` keeps the DataContext as the view model. Binding-error trace must be clean. |
| Reordering during the row-index removal accidentally changes the on-screen order. | 3 | One tab per commit; compare against a screenshot taken first. |

---

## Out of scope

* Any change to what the window looks like or how it behaves.
* Replacing the `ListBox` strip with a real `TabControl`.
* Restyling the custom-list buttons, which are currently unstyled WPF defaults.
* Generating the UI from setting descriptors.
* `CustomListDefinitionEditView.xaml` (188 lines) — same patterns at a fraction of the size.
  Worth the same treatment afterwards if Stages 1–3 land well.

---

## Decisions needed before implementation starts

1. **Screenshots first.** This whole plan is verified by "it looks the same", so a screenshot of
   each of the seven tabs before Stage 1 is the baseline. Worth doing even though it is manual.
2. **Where the resource dictionary lives.** Proposed: inline in `Window.Resources` for now, since
   only one window uses it; move to a shared file if and when
   `CustomListDefinitionEditView` follows.
