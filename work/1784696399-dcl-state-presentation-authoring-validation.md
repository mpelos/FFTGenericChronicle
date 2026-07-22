# DCL state presentation authoring validation

## Context

The presentation resolver can separate unit icons, CT timeline icons, selected-state details, and detail-only technical state facts. The next offline risk was authoring drift: a normalized action could materialize a persistent state while omitting that state's presentation profile from the action's declared `StatePresentationIds`.

## Work completed

- Added bundle-level reference validation for state presentation ownership.
- `StatusApplication` effects now require the action's `PresentationProfile.StatePresentationIds` to include the referenced state's `PresentationProfile`.
- `StoredReraise` revive effects now require the same presentation link for the stored trigger state.
- `StatusRemoval` is intentionally not forced through this rule because it removes an existing state rather than materializing a new persistent state.
- Updated canonical smoke sentinel profiles so every status/rider/Area-rider/physical-rider/Reraise/Quick state materialization declares the state presentation it can create.

## Validation

- First canonical smoke run failed on the missing presentation IDs in the sentinel bundle, proving the new validation catches the intended authoring gap.
- After updating the sentinel authoring records, `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.
- Two stale smoke-test processes held build outputs after failed runs. Only the exact PIDs reported by MSBuild were terminated to release the local test binaries.

## Remaining boundary

Native UI binding still has to consume these presentation IDs and channel snapshots at the real game UI surfaces. This pass only closes the normalized authoring/runtime contract offline.
