# DCL status-rider offline closure checkpoint

## Scope

This checkpoint records the offline promotion of the remaining status riders whose native numeric
transactions can be preserved while only `InflictStatus` is suppressed. It covers custom damage,
Blood Drain, Dragon's Gift, and Self-Destruct after the earlier ordinary, grouped, and retained
carrier work.

## Hardened findings

- The ordinary data-suppressed allowlist contains 26 actions. It includes Muramasa's random-one
  group, Crushing Blow and Unholy Sacrifice custom-formula riders, and Blood Drain's paired HP
  debit/credit rider.
- Dragon's Gift 252 is the sole support-rider suppression. Formula 0x5B preserves its Dragon/Hydra
  eligibility gate, target heal, and paired source sacrifice while eleven managed remove rules own
  its complete Cancel packet.
- Self-Destruct 277 is the sole conditional split-result suppression. Formula 0x52 gives non-self
  victims the caster's missing-HP debit and calls the status finalizer, while the caster receives a
  current-HP lethal debit and skips that finalizer.
- `dcl.isSelf` is derived from exact target/attacker pointer equality. The Self-Destruct validator
  accepts exactly one victim Oil rule and requires `dcl.isSelf == 0`, preventing Oil from leaking
  onto the caster result.
- Every one of the 150 status-bearing catalog actions still has exactly one authority owner.

## Exact authority partition

| Authority | Count |
| --- | ---: |
| retained native carrier | 22 |
| ordinary rider data suppression | 26 |
| support rider data suppression | 1 |
| conditional rider data suppression | 1 |
| conditional producer | 82 |
| native lifecycle | 5 |
| managed instant KO | 9 |
| special transaction | 4 |

The four remaining special transactions are Nameless Song, Forbidden Dance, Celestial Void, and
Corporeal Void. Their unresolved boundaries are performance cadence or RandomFire
per-target/per-strike cardinality, both already isolated as live gates.

## Validation

- `tools/analyze_dcl_dragon_gift.py --check-only`: PASS.
- `tools/analyze_dcl_self_destruct.py --check-only`: PASS.
- `tools/analyze_dcl_status_authority.py --check-only`: PASS.
- Runtime settings validation passes for the Dragon's Gift and Self-Destruct profiles.
- `codemod/run-offline-checks.ps1`: PASS in 43.5 seconds after regenerating the formula-context
  inventory for `dcl.isSelf`; C# build reports zero warnings and zero errors.

## Evidence and profiles

- `work/1784068683-dcl-dragon-gift-analysis.md`
- `work/1784068553-battle-runtime-settings.dcl-dragon-gift-rider.json`
- `work/1784069063-dcl-self-destruct-analysis.md`
- `work/1784068927-battle-runtime-settings.dcl-self-destruct-rider.json`
- `work/1784069064-dcl-status-action-authority.md`
- `work/1784069064-dcl-implementation-coverage.md`

## Next offline frontier

The rider-carrier sweep is exhausted without using a known live-gated mechanism. Continue with the
82 conditional producers by mapping exact producer inputs and packet semantics offline. Arming roll
consumption remains gated on LT28 execution provenance. The four remaining special transactions
should stay deferred until their existing performance/RandomFire live gates can be run.
