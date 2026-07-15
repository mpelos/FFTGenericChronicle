# LT11 — selector delivery and selective reaction gate

Date: 2026-07-12  
Status: **PASS — clean authored miss and Counter suppression proven live.**

## Objective

Validate the LT10 follow-up implementation on a real close-range basic attack against a defender
with Counter:

1. a DCL miss reaches the result selector as a clean miss outcome rather than a zero-damage hit;
2. the same cached per-target decision suppresses the defender's Brave-gated Counter before it arms;
3. the native hit path remains untouched when the DCL decision is a hit or absent.

Profile: `work/1783912841-battle-runtime-settings.lt11-selector-reaction.json`  
Raw log: `work/1783916102-lt11-selector-reaction-gate-live.log`

## Live action

- Save 05, random battle.
- Ramza (`id=0x01`, caster index 16) moved adjacent to Ianthe the Chocobo
  (`id=0x82`, target index 4).
- The native forecast still displayed 100% and Counter; this is a separate forecast-parity gap.
- Ramza executed basic Attack (`action.type=0x01`, ability `0`).
- The LT11 profile assigned basic attacks a 50% DCL hit chance.

## Decisive evidence

The execution decision was a miss:

```text
[DCL-HIT] caster=0x01 target=0x82 ability=0 type=0x01 pct=50 roll=90 outcome=miss ...
```

The reaction gate consumed the same cached action/target decision and suppressed the natural
Brave chance without changing the unit's Brave:

```text
[DCL-REACTION-GATE] targetIdx=4 casterIdx=16 ability=0 type=0x01 chance=73->0 decision=miss
```

The staged 532 damage was reduced to zero, and the selector authored the engine's evade branch:

```text
[DCL] caster=0x01 target=0x82 abilityId=0 ability=<Nothing> actionType=0x01
      result=0 debit=0 oldDebit=532 outcome=forced-miss
[SELECTOR-PROBE event=3 ... hp=205 ... rec+1BE=00 rec+1C0=06 rec+1C4(dmg)=0 ...]
[DCL-SELECTOR] targetIdx=4 casterIdx=16 ability=0 type=0x01
               naturalKind=0x00->0x06 resultCode=0x01->0x00 decision=miss
```

Observed battle result:

- Ianthe remained alive at 205 HP.
- No damage number was applied.
- No Counter animation or counterattack followed.
- The game advanced directly to Ramza's facing/end-turn prompt.

## Verdict

The LT11 mechanism is sufficient for per-target DCL miss execution:

- `DclHitDecisionCache` bridges the calc-entry decision to both downstream consumers.
- Pre-clamp output control owns the HP debit.
- Selector `0x205210` owns the rendered/result kind (`+0x1C0=0x06`, `+0x1BE=0`).
- The four `0x30BExx` Brave-roll sites can selectively suppress Counter-class reactions for only
  the cached miss; cache misses and DCL hits preserve the native chance.
- The old late `+0x1D8` presentation write is not required for this path.

## Remaining gap exposed by the test

The native forecast showed 100% hit and Counter even though execution used the DCL 50% contest and
suppressed Counter on the miss. Execution authority is now proven; forecast hit-percent and reaction
preview parity remain to be wired to the same decision model.

## Operational discovery

Mouse hover can prevent keyboard confirmation from acting on the selected tactical target. The
reliable sequence is:

1. click the target once;
2. verify the bottom-right target pane;
3. click the game window's title bar to move the pointer off the battlefield without changing target;
4. press `F` to enter the forecast;
5. press `F` again to execute.

