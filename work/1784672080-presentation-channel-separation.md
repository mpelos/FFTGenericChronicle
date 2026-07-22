# Persistent-state presentation channel separation

## Context

The DCL presentation row had a pure resolver for position, palette, symbolic icons, Shock counters, and selected-state detail. The design also says several technical states/resources are visible, but not as above-unit ailment icons: Ready/Unready lives in selected equipment, spent Block and repeated-Parry counters live in defensive panel/forecast, and QuickLock lives in the CT timeline.

## Conclusion

- Added `DclStatePresentation.UnitIconFor`.
- Added `DclStatePresentation.TimelineIconFor`.
- `UnitIconFor` keeps ailment-like custom states on the above-unit icon channel, but returns no unit icon for QuickLock, Ready, Unready, spent Block, and repeated-Parry counters.
- `TimelineIconFor("quicklock")` maps to the existing Haste icon asset.
- The existing `IconFor` remains the symbolic detail/icon resolver and is not used as an automatic above-unit fallback for every technical state.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclStatePresentation.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c PresentationChannels1 --no-restore -m:1 --nologo -v:minimal`
