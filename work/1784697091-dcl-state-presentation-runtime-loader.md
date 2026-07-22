# DCL state presentation runtime loader

## Context

State presentation profiles existed as a pure validated catalog, but the canonical runtime loader still loaded only authoring, item metadata, ability bindings, Reaction bindings, and optional policy-ticket templates. That left presentation profiles outside the real runtime snapshot.

## Work completed

- Added strict JSON loading for `DclStatePresentationProfileBundle`.
- Added `DclCanonicalRuntimeCatalog.StatePresentations`.
- `DclCanonicalRuntimeLoader.LoadFiles` now requires a state-presentation profile bundle path.
- `DclCanonicalRuntimeLoader.LoadJson` now loads state presentation profiles and validates state-definition references as part of the canonical snapshot.
- Runtime settings gained `DclCanonicalStatePresentationProfilesPath`.
- Runtime settings validation rejects enabled canonical runtime without that path.
- Mod hot-reload tracks the state-presentation profile file path and last-write time, reloads when it changes, and reports loaded presentation count in the canonical runtime log line.
- The canonical admission sentinel emitter now writes the state-presentation profile bundle and includes its path in emitted settings.

## Validation

- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release` passed.
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.

## Notes

- A failed intermediate smoke run exposed that static sentinel generation cannot call the dynamic Shock icon resolver without magnitude; the sentinel profile now uses a symbolic Shock icon token rather than sampling a dynamic Doom counter.
- Existing direct test-only `DclCanonicalRuntimeCatalog` construction remains compatible without a presentation registry; the production loader path is the strict path.

## Remaining boundary

The runtime now loads and validates state presentation profiles, but the native UI still needs binding work for above-unit icon rows, CT timeline markers, selected-detail panels, pose/palette application, entry feedback, and command disablement presentation.
