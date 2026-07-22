# DCL state presentation channel snapshot

## Context

The DCL player-facing information contract requires persistent states to be visible without mixing unrelated presentation channels. Above-unit status icons, CT timeline markers, selected-unit details, equipment/defense-panel resources, and technical state facts must stay separate so UI binding can consume the right fact at the right surface.

## Work completed

- Added a pure `DclStatePresentationChannels` snapshot resolver.
- The snapshot emits:
  - above-unit icon rows;
  - CT timeline icon rows;
  - selected-state detail rows;
  - explicit detail-only state kinds for technical states with no icon channel.
- Snapshot ordering is stable by persistent state instance id.
- Missing display names fail closed with `ArgumentException` instead of silently producing incomplete visible state detail.
- Extended the canonical runtime smoke test to cover Shock, QuickLock, and Ready together:
  - Shock appears as an above-unit symbolic Doom counter.
  - QuickLock appears only through the CT timeline Haste symbol.
  - Ready is retained as detail-only and does not leak into icon channels.

## Validation

- `dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release` passed.
- A first parallel `dotnet run` collided with the simultaneous build output lock and failed with `CS2012`; rerunning sequentially passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.

## Remaining boundary

Native asset row resolution and actual UI binding for pose, palette, above-unit icon, CT timeline icon, equipment/defense panel, selected detail, and command disablement remain live-gated.

The Python runner in this Codex session is currently timing out even on trivial `python -c` commands, so the coverage report source was updated but generated coverage artifacts were not regenerated in this pass.
