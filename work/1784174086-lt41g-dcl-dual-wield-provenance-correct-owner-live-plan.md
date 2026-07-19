# LT41G DCL Dual Wield provenance correct-owner live plan

## Fixture and isolation

- Autosave: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`, SHA-256
  `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`.
- Runtime profile:
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Isolated Reloaded config:
  `work/1784161260-appconfig.synthetic-reaction-isolated.json`.
- Diagnostic DLL/PDB SHA-256:
  `2BDB9C28071AA2F68D94AFB34EC9945B6163E1AC208A6B7360170D177C5B49D3` /
  `F7C4CF068B06E677B417C181018675CC7EAEFFF66D68A0B868BAA033A35017A6`.

Before action, require the startup owner evidence to show carrier `443`, not native `442`.

## Action and stop rule

Use the proven **Auto-battle > Attack Enemy > Wenyld** route. Allow Rion's Throw, Wenyld's rejected
distant synthetic request, then Janus's adjacent Choco Beak and the single owned delivery-`442`
transaction. Stop after its two state-`0x2C` effects or immediately on any contrary event.

## Required diagnosis

For both Counter strikes correlate calc origin/state, latest context at pre-clamp, guard outcome,
DCL rewrite/miss, HP delta, and effect row. Do not change the cache/pipeline until the second strike
has one unambiguous failure reason.

Use a fresh six-file backup and restore every external hash after archiving the raw log.
