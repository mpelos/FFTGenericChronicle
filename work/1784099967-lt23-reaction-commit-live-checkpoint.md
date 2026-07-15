# LT23 reaction-commit live checkpoint

## Scope

This bounded observe-only run tested the three queue hooks configured by
`work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json`. The game executable and all
hook anchors matched the current-build offline audit before launch.

## Capture

- Full log: `work/1784099229-lt23-dcl-reaction-commit-live.log`
- Log SHA-256: `1DDA71AD82362A7F551410F7B4106B3CBB1A772A347D9EF12A4D3356C51954EA`
- Machine analysis: `work/1784099949-lt23-reaction-commit-live-analysis.md`
- The three guarded hooks installed at pass 0 `0x2066AE`, pass 1 `0x206743`, and pass 2
  `0x206421`.
- Three forecast opens/cancels before the first executed attack produced no commit event.
- Josephine's executed Attack against Arthur produced one pass-1 row with id `0`, agreeing zero
  actor copies, and no targets. It is not a native Reaction id.
- A later Red Panther action produced one pass-1 row with id `280`. The baseline catalog identifies
  `280` as Claw, an ordinary active ability. The row had no targets.
- No row carried a native Reaction id in `422..453`; neither pass 0 nor pass 2 fired.

## Fixture correction

Arthur visibly has Auto-Potion. Josephine's in-game Status screen shows Shirahadori, not Counter.
The prior use of candidate word `+0xF2w=442` as evidence of equipped Counter was invalid; that word
does not own the unit's equipped Reaction. The Red Panther attacked Josephine after she moved into
range, but Shirahadori is an avoidance family and did not create a queued Counter action.

## Verdict

The hypothesis that all three post-store sites are accepted-Reaction commits is refuted. Pass 1 is
a generic action-queue boundary and fires for ordinary actions. The run does not refute the pass-2
real-code Reaction path, but it does not live-prove it either because no queued Reaction was visibly
accepted.

The runtime logger now classifies only agreeing ids `422..453` as
`[DCL-REACTION-COMMIT]`; other fires are tagged `[DCL-REACTION-COMMIT-NOISE]`. Mutation controls
remain restricted to pass 2. The next LT23 repetition requires an explicitly equipped Counter unit
and one distinct queued family such as Auto-Potion.

## Cleanup

The game and Reloaded-II were stopped. Installed DLL, runtime settings, Reloaded AppConfig, live log,
and Enhanced autosave were restored byte-for-byte from the pre-run backups. Their SHA-256 hashes are,
respectively:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- Settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- Restored log: `95D29BE4633CB51E04DBE321D41ACB99EBD7E0CE140D8DB488F351AB127DBDAE`
- Autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
