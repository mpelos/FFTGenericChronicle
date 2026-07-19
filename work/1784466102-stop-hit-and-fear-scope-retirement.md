# Stop-hit and Fear scope retirement

## Human decision

- Stop-hit and every other effect that interrupts a unit between route tiles are removed from the
  DCL. Position-triggered abilities may resolve only after the mover finishes on a qualifying tile.
- Fear is removed from the current investigation and implementation scope. It will receive a new
  design separately; the Chicken/Fervor mechanism is not a basis for further work.

## Immediate runtime containment

The installed runtime profile was the unified sentinel v6 and had all four unsafe controls enabled.
The installed configuration now has:

- `DclApproachEnabled=false`
- `DclFearControlEnabled=false`
- `DclFearForcedFleeControlEnabled=false`
- `DclFearPlayerConfirmEnforcementEnabled=false`

The obsolete `dcl-fear` status rule was also removed and `DclFearStatusRuleName` was cleared. The
disabled installed settings hash is
`12085DCD38B6DF478C7C73B8C777035B6D56F5BA31FC924B5A81B1F30B331245`.
The reusable disabled profile is
`work/1784466102-battle-runtime-settings.dcl-no-approach-no-fear.json`.

The temporary instant-Fervor NXD was removed first. The remaining v7 NXD still contained the
Fear-carrier Formula `0x38/0/0` rewrite, so both the repository payload and installed data mod were
restored from the pre-Fear unified v2 NXD. All three copies hash to
`44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9`.

Pre-change backups:

- `work/1784466102-pre-stop-hit-fear-retirement-installed-settings.json`
- `work/1784466102-pre-stop-hit-fear-retirement-installed-nxd.nxd`

## Documentation and coverage consequence

- `docs/deep-combat-layer/06-reach.md` owns the final-tile-only movement rule.
- `docs/deep-combat-layer/13-statuses-and-reactions.md` gives Fear no active DCL contract.
- The implementation-coverage generator no longer counts Approach or Fear as DCL completion work.
  It tracks a new missing mechanism for final-tile position triggers.
- The former v3-v7 sentinels and exact-v4 regression matrix are historical artifacts, not valid
  whole-DCL completion profiles.

## Preserved engineering evidence

The Approach and Fear code, probes, logs, and engine maps remain as historical reverse-engineering
evidence. They are dormant and provide no authorization to enable, repair, regress, or extend either
mechanism. A later cleanup may remove unreachable runtime code independently from the current DCL
mechanism investigation.
