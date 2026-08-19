# Parked — Attract mode enhancements

Ideas raised while refactoring attract mode and deliberately **not** built. That work was a
technical debt exercise with an explicit product constraint — *refine, don't redesign; a user
should not notice a different screensaver* — so every feature and look-and-feel idea was
written down here instead.

**None of these are designed.** They are a list of what was considered, not a backlog.
Anything taken from here needs its own design pass, and most would want `RULE-ATTRACT-004`
covered by a test first, since they all change the sequence that rule describes.

## Information on screen

- Game title, release year, platform and star rating shown during a slide.
- An optional clock overlay — the common request for a screensaver on an arcade cabinet.

## Motion and transitions

- Ken Burns style slow zoom in addition to the pan.
- Crossfade directly between games instead of returning to black between slides.
- Downsample large artwork with `BitmapImage.DecodePixelWidth`. Deliberately avoided in the
  refactor: the image is intentionally oversized so the pan has somewhere to travel, so
  downsampling trades memory for visible quality on exactly the artwork the feature exists to
  show. It would need a measurement first — `B-28` — and a decision about how much quality
  loss is acceptable.

## What gets shown

- Selection sources other than the whole library: favourites, the category currently being
  browsed, or only games with real artwork. Note this overlaps `OQ-015`, which asks whether
  attract mode should respect the browsed list set — that is an open product question, not
  an enhancement, and should be answered first.
- Skip games whose background resolves to the default placeholder, rather than showing it.
- Video previews in attract mode.

## Reach

- Multi-monitor support.

## Related open questions

`OQ-015` — should attract mode respect the browsed list set? See
[../UNRESOLVED.md](../UNRESOLVED.md).
