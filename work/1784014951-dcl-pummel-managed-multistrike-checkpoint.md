# DCL managed Pummel multistrike checkpoint

## Scope

This pass implements and validates the offline runtime mechanism needed to turn native Pummel's one
aggregate debit into three independent DCL physical contests. No installed runtime profile was
changed and no game process was launched. The user separately confirmed that Enhanced still starts
normally through Reloaded-II.

## Runtime mechanism

- Approved metadata arms the route only when `side_effect_policy=managed_multistrike` and
  `strike_count` is within 2..99. A nonzero strike count with any other policy fails the entire
  overlay closed.
- Each authored strike re-evaluates derived variables, Attack Skill, Dodge, Parry, Block, defense
  permission, and defense modifier with its own zero/one-based strike indices.
- Each strike receives independent attack and defense 3d6 rolls. The local Guard snapshot loses a
  selected finite defense after either a successful or failed defense; criticals and attack
  misses/fumbles do not request or spend a defense.
- The cache stores one outer decision plus aggregate hit, critical, miss, fumble, defended, Parry,
  and Block counts. Preview/charge/AI refires reuse that decision.
- The aggregate chance is exact rather than path-estimated. A dynamic state table retains only
  no-hit branches and tracks the remaining Parry/Block charges across all 3d6 outcomes.
- Pre-clamp formulas receive the rolled aggregate before derived/damage evaluation. The Guard pool
  commits every expected finite-defense attempt once, after the staged result succeeds; duplicate
  callbacks cannot spend again.
- The carrier remains one native staged result. The managed route does not synthesize per-strike
  applies or reactions.

## Formula surface

Preview receives the authored strike count and zero outcome counts. Execution receives:

```text
dcl.strike.count
dcl.strike.hitCount
dcl.strike.normalHitCount
dcl.strike.criticalCount
dcl.strike.attackMissCount
dcl.strike.fumbleCount
dcl.strike.defendedCount
dcl.strike.parryAttempts
dcl.strike.blockAttempts
dcl.strike.anyHit
```

During per-strike contest evaluation, `dcl.strike.index` is zero-based and
`dcl.strike.number` is one-based. Both are zero in aggregate contexts.

## Deterministic probe

The paired files are:

- `1784014720-dcl-pummel-probe-metadata.csv`;
- `1784014720-battle-runtime-settings.dcl-pummel-multistrike.json`;
- `1784014720-dcl-pummel-multistrike-live-plan.md`.

The probe uses skills 12/11/10, attack rolls 10, defense rolls 12, Block 13, Parry 11, Dodge 8,
and one use of each finite defense. The deterministic execution aggregate is therefore two normal
hits plus one defended strike, one Block attempt, one Parry attempt, and 80 HP damage at probe power
40. Its exact nominal any-hit forecast is 48%; the all-hit preview amount is 120.

## Validation

- C# build: zero warnings and zero errors.
- Formula/runtime smoke tests: pass, including aggregate cache equality/idempotence, hit/critical/
  fumble counts, finite-Guard probability, the exact 48% probe forecast, and formula variables.
- The settings validator now opens any referenced ability overlay and its base catalog. Missing or
  invalid metadata becomes a deploy error. The Pummel probe loads 512 base abilities and exactly one
  approved metadata row with zero errors.
- Full `codemod/run-offline-checks.ps1`: pass across Python tooling, executable analyzers, C# build,
  smoke tests, settings validation/simulation, dry-run deploy helpers, and whitespace checks.
- Whole-DCL coverage was regenerated at prefix `1784014950`.

## Remaining live boundary

Offline evidence cannot prove that the Enhanced result renderer, native reaction queue, and HP
apply expose exactly one outer Pummel result under injection. The minimal live gate is one Pummel
execution from save 05 under the deterministic probe. It must show three `[DCL-STRIKE]` lines, one
aggregate `[DCL-HIT]`, one 80-HP `[DCL]` commit, one atomic `[DCL-GUARD]` commit, one selector
delivery, and at most one target reaction.

Pummel remains a mixed classification record until that carrier/cardinality gate passes and final
production power is ratified. Dual-wield and managed magic multistrikes remain separate mechanisms;
this implementation does not infer their strike identity or reuse Pummel's aggregate carrier.
