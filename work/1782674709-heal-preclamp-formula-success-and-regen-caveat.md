# Heal pre-clamp formula test: success plus Regen caveat

## Setup

Profile used: `work/1782684780-battle-runtime-settings-heal-formula-preclamp-test.json`.

Intent:

- preview healing via `PreviewForecastPokeValue=210`;
- write the preview/forecast healing credit field with `PreviewForecastDamageFieldOffset=8`
  (`obj+0x8 == unit+0x1C6`);
- rewrite execution healing through the pre-clamp formula plan;
- formula: `-min(t.maxHp - previousHp, a.ma * 10 + t.faith)`;
- test action: Agrias healing Ramza.

The profile was intentionally broad for the proof:

- `RewriteConditionFormula = event.isHealing && a.sourceImmediate && action.sourceImmediate && action.currentActiveAction`;
- `PreClampImmediateActionRequireFreshActive=false`;
- global forecast poke remained enabled while the profile was active.

## User observation

- Preview: `+210HP`, `100%`.
- UI result: `+210HP`.
- Ramza ended full.
- User noticed Regen also appeared to heal `+210HP`, higher than normal.

## Log evidence

Evidence file: `work/1782685331-battleprobe-log-heal-formula-preclamp-success-regen-caught.txt`.

The preview credit poke hit the healing field:

```text
[FORECAST-POKE] obj=0x141855E9E unitIdx=16 wrote unit+0x1C6=210
```

The main formula-backed heal staged a credit plan at pre-clamp:

```text
[PRECLAMP-PLAN-QUEUE slot=0 ptr=0x141855CE0 id=0x01 hp=458/567 oldDebit=0 oldCredit=210 forcedDebit=0 forcedCredit=109 ... context=immediate-action/act=159 ...]
[PRECLAMP-FORMULA-RUNTIME ptr=0x141855CE0 id=0x01] event=healing ... trace.vanillahealing=109 ... trace.attackerma=14 ... trace.targetfaith=70 ... trace.targetmissinghp=109 ... trace.expectedheal=109 | final=-109:FinalDamageFormula
```

Interpretation: the runtime formula path worked. The actual plan credit was `109` because Ramza was
missing only `109` HP at that moment, so the formula's `min(missing, 210)` clamped to the missing HP.
The engine then owns the MaxHP clamp.

The broad profile also touched later HP-credit/passive-style events:

```text
[PRECLAMP-PLAN-QUEUE ... hp=528/567 oldDebit=0 oldCredit=70 forcedDebit=0 forcedCredit=39 ... context=immediate-action/act=159 ...]
[PRECLAMP-FORMULA-RUNTIME ... actionidagems=13946 ... currenttargetcachecredit=70 ... trace.targetmissinghp=39 ... final=-39:FinalDamageFormula
[HEALING ptr=0x141855CE0 id=0x01] 458 -> 528 = 70
```

and later:

```text
[PRECLAMP-ACTOR-CTX ... actionId=1 verdict=resolved actors=[0x140D31AA0->id=0x1E,0x140D31558->id=0x01]]
[HEALING ptr=0x141855EE0 id=0x1E] 343 -> 345 = 2
```

The logs show the same `+0x1C6` HP-credit surface is used for both explicit heals and passive/side
healing. The profile's stale immediate-action allowance let old action context survive long enough
to classify unrelated healing as action-backed.

## Conclusions

Proven:

- `unit+0x1C6` is the healing/HP-credit twin of `unit+0x1C4`.
- `obj+0x8 == unit+0x1C6` drives healing preview number and HP-bar ghost refill.
- The pre-clamp hook can queue formula-backed staged credit writes for execution healing.
- Formula results for healing use negative `finalDamage`; the pre-clamp plan converts that into
  `forcedCredit`.
- Engine MaxHP clamp still owns the final boundary.

Caveat:

- `event.isHealing` alone is too broad. Regen and other passive HP-credit events use the same staged
  credit surface and must be distinguished from explicit action heals.
- `PreviewForecastPoke` is a global proof lever, not a shipping preview system. It can leave
  `cred1C6` values visible on the active forecast/target cache and should become per-action/per-target
  arming before it is used outside controlled tests.
- Immediate-action matching must be fresh for shipping profiles. `currentActiveAction` with
  `PreClampImmediateActionRequireFreshActive=false` can accept stale action context.

## Follow-up

The next healing profile should:

- set `PreClampImmediateActionRequireFreshActive=true`;
- use a small `PreClampImmediateActionMaxAgeMs`;
- gate the formula on a known action id and fresh source, not only `event.isHealing`;
- eventually prefer the pre-clamp actor-context resolver over stale immediate-action heuristics;
- leave global preview poke off unless it is explicitly armed for the selected action/target.
