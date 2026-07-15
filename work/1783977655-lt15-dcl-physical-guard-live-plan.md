# LT15 — DCL physical contest and finite guard ladder

## Prerequisite

LT14 must pass first. The unmodified Enhanced game must reach its title/menu before injection is retried. This profile is prepared offline and is not the installed runtime profile while LT14 remains pending.

## Objective

Prove that a basic Attack uses the DCL's independent attack/defense rolls, spends exactly one finite defense only at real application, falls through the guard ladder, and refreshes Block/Parry at the defender's next own turn.

## Deterministic setup

Profile: `1783977655-battle-runtime-settings.lt15-dcl-physical-guard.json`.

- basic Attack only (`action.type == 1`, ability 0);
- attack skill 12, forced attack roll 10: every test swing connects before defense;
- forced defense roll 12;
- Block 13 with one use;
- Parry 11 with one use;
- Dodge 8, non-depleting;
- no DCL damage formula: a landed hit retains native damage.

## Sequence

1. Launch Enhanced, press Enter to skip the intro, choose Load, Manual Saves, and the first entry (save 05).
2. Confirm all guarded hooks install and no `SKIP`/`FAILED` line appears.
3. Preview the first basic Attack. The forecast must use the exact skill-12-versus-Block-13 probability. Cancel the preview at least once and reopen it; no `[DCL-GUARD] ... spent` line may appear.
4. Execute the first Attack. Defense roll 12 is at or below Block 13, so result is `Defended`, damage is zero, and exactly one Block charge is spent.
5. Before the defender receives a turn, execute a second Attack. Block is unavailable; Parry 11 is selected. Defense roll 12 fails, native damage lands, and exactly one Parry charge is spent.
6. Before the defender receives a turn, execute a third Attack. Only Dodge 8 remains. Defense roll 12 fails, native damage lands, and no finite charge is spent.
7. Let the defender's own turn begin. Exactly one `own-turn-refresh` log must restore Block and Parry to 1/1.
8. Attack again after that refresh. Block 13 must be selected and successfully defend again.

## Mandatory multistrike gate

If any player-controlled unit in save 05 can use a dual-wield basic Attack, run this before the
separate-attack ladder sequence. Do not substitute two ordinary attacks: the purpose is to observe
the engine's event ordering inside one action.

With LT15's deterministic rolls, one dual-wield action must produce:

1. strike 1 selects Block 13, is defended, deals zero, and spends Block once;
2. strike 2 independently selects Parry 11, fails to defend against roll 12, deals native damage,
   and spends Parry once.

Capture the uninterrupted log span from the first `[CALC]` through the second selector event. Count
and preserve the order of `[DCL-HIT]`, `[DCL-REACTION-GATE]`, `[DCL]`, `[DCL-GUARD]`,
`[DCL-SELECTOR]`, and `[DCL-KIND]`. A pass requires two independent `[DCL-HIT]` decisions and two
selector deliveries; two HP events by themselves do not prove independent contests.

## Pass gates

- preview/cancel never spends guard;
- each real connected attack selects the currently best available defense;
- Block and Parry each spend once, including when their defense roll fails;
- Dodge never spends;
- authored misses/defenses suppress native hit reactions;
- successful hits retain native damage;
- refresh happens once at own-turn start, not on repeated active polling and not on another unit's turn;
- forecast changes as the finite ladder depletes and matches the selected defense in `[DCL-HIT]` logs.

## Failure interpretation

- charge spent during preview: guard commit is occurring before successful apply;
- repeated `spent` for one execution: decision consumption is not idempotent;
- `stale-no-charge`: cached availability diverged before apply and needs reservation or execution-time recomputation;
- no refresh or early refresh: `+0x1B8` is not the correct own-turn rising edge;
- second/third swing reuses the first outcome/defense: cache lifetime is too broad for successive strikes;
- dual-wield strike 2 has no `[DCL-HIT]`/guard decision: calc-entry does not refire per strike and
  the next contest must be generated at the per-strike pre-clamp boundary;
- both dual-wield `[DCL-HIT]` lines say `cached=1` with the same rolls: the key needs a proven
  execution/strike generation rather than only `(target,caster,ability,type)`;
- HP changes on the defended first swing: authored miss delivery did not cover the physical defense outcome;
- wrong forecast after depletion: preview is using stale guard state or stale per-target decision data.
