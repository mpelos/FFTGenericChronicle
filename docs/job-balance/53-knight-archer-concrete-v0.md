# Knight And Archer Concrete Provisional V0

Status: Accepted as W3 Knight/Archer concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/09-accuracy-evasion-model-schema.md`
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-knight-archer-concrete-v0.json`

## Purpose

This is the second W3 physical/foundation concrete action-value producer.

It covers Knight and Archer because they are the first-specialist physical pair in Band B. The goal
is to make active Knight and active Archer feel like real upgrades before their strongest exports
open. `Equip Armor`, `Equip Shield`, `Equip Bow`, `Concentration`, `Bow Mastery`, `Parry`, and
movement values remain future RSM/equipment producer work.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Knight Battle Skill and Archer Aim/Charge records in
  `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `equipment_break`, `damage`, `mp`, `stat_down`, `timing`, `accuracy`, `defense`, `support`, `movement`, and
  `equipment_unlock` tags in `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Defending, Charging, Slow, Stop, Immobilize, Blind, and related status vocabulary in
  `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/reference/README.md` as the navigation layer.

The design does not copy vanilla Battle Skill or Aim. It preserves the recognizable FFT read:
Knight pressures armed frontlines, while Archer owns bow/crossbow range, height, and timing.

## Scope Boundary

Included here:

- Knight action values;
- Knight stat-pressure boundary values;
- Archer action values;
- T6 guard/armor-response boundary rows;
- T4 shield-break accuracy rows;
- Band B first-specialist damage checks.

Deferred:

- final reaction/support/movement values;
- final equipment-unlock pricing and shop timing;
- final prerequisite tree;
- a separate `Rend Helm` implementation split if later data work supports head/body distinction;
- AI taunt, overwatch, interrupt, and hard movement-control behavior;
- full W4 populated T2.1 incidence and W5 real-roster dominance verdict.

## Shared Formula Contracts

Physical rows use the current v0.2.1 family model from `work/sim-inputs-v0.2.1.json`.

Knight armor/guard changes use the accepted T6 model:

```text
final_response = clamp(penetrated_response + response_delta, 0.25, 2.50)
```

This pass uses armor-specific `Rend Armor` deltas so the setup is strongest against heavy armor and
not a generic cloth vulnerability:

```text
response_delta_by_armor = { plate: +0.12, mail: +0.10, leather: +0.06, cloth: +0.00 }
response_cap = 1.20
duration = next two physical hits or target's next turn, whichever comes first
```

The cap and duration are intentionally conservative. `Rend Armor` should create a Knight-flavored
follow-up window, not become the mandatory setup button for every physical party.

Shield/guard accuracy rows use the accepted T4 model. `Shield Break` only reduces shield or weapon
guard layers. It does not erase class evasion, accessory evasion, rear-facing value, or immunity.

Knight offense-down rending uses one output-pressure channel per outgoing action. `Rend Weapon` and
`Rend Power` do not multiply into `0.75 x 0.85 = 0.6375`. If both apply to the same weapon physical
action, use the stronger single output reduction, currently `Rend Weapon` at x0.75. `Rend Power`
matters when the target's outgoing physical action is unarmed, natural, or otherwise not already
covered by a stronger weapon-output rend. `Rend Magick` is a separate magic/healing output channel
and follows the same non-stacking rule within its own channel.

## Baseline Damage Check

If Knight or Archer appear too early, their basic attacks already beat starter texture.

Early warning rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Knight sword | 31 | 36 | 45 | 48 |
| Archer longbow | 38 | 49 | 43 | 45 |
| Archer crossbow | 38 | 46 | 42 | 43 |

Mid first-specialist rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Knight sword | 78 | 90 | 114 | 120 |
| Knight fists | 80 | 68 | 71 | 71 |
| Archer longbow | 76 | 99 | 87 | 91 |
| Archer crossbow | 85 | 103 | 94 | 97 |
| Monk fists reference | 80 | 68 | 71 | 71 |

Read:

- active Knight and Archer have enough ordinary output to work before passives;
- Archer already owns clear missile pressure into mail;
- Knight fists matching Monk's mid fist row is a warning only, not a protected identity, because
  Knight lacks Monk's punch-action package and sustain. Knight crush actions must stay control-first.

## Knight Values

Knight should win by armed control and durable engagement, not by becoming the best sword shell.

| Skill | Value | MP | CT | JP | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `Guarded Strike` | current weapon x0.85 | 0 | 0 | 120 | T6xPS/T6xT7 | Next direct weapon hit before Knight's next turn is reduced by 15%; no self-stacking. |
| `Rend Weapon` | current weapon x0.50 plus enemy weapon output x0.75 | 0 | 0 | 150 | T7/T6xT7 | Armed enemies only; next two weapon actions; no inventory deletion. |
| `Rend Armor` | current weapon x0.50 plus armor-specific response delta | 0 | 0 | 180 | T6/T2.1 | Non-stacking; capped at response 1.20; next two physical hits or target's next turn. |
| `Shield Break` | current weapon x0.35 plus shield/weapon evasion x0.50 | 0 | 0 | 220 | T4/T6xT7 | Does not affect class/accessory evasion or rear-facing hit. |
| `Rend MP` | current weapon x0.35 plus MP damage min(30, 25% max MP) | 0 | 0 | 160 | T9/T2.1 | Anti-caster resource pressure; no HP burst plan. |
| `Rend Speed` | current weapon x0.35 plus Speed -1 | 0 | 0 | 240 | T5/T10/T2.1 | Until target's next action; non-stacking; no Slow replacement. |
| `Rend Power` | current weapon x0.35 plus physical output x0.85 | 0 | 0 | 200 | T7/T6xT7 | Next two PA/weapon physical actions; weaker than `Rend Weapon` against armed targets but broader. |
| `Rend Magick` | current weapon x0.35 plus magic/healing output x0.85 | 0 | 0 | 200 | F4/T9/T2.1 | Next two MA/Faith actions; no Silence replacement. |
| `Crushing Blow` | current crush route x0.85 | 0 | 0 | 260 | F5/T6 | Fists/heavy impact route; guard pressure, not Monk replacement. |
| `Challenge` | deferred | - | - | - | T8/T10 | No hard taunt or boss lock accepted in this pass. |

Offense-down stacking rule: `Rend Weapon` and `Rend Power` do not compound multiplicatively on the
same target. If both apply to one physical action, use the stronger applicable physical-output
reduction, meaning the lower multiplier, not the product. `Rend Magick` is a separate
magic/healing-output axis.

`Shield Break` deliberately keeps a guard-break name instead of `Rend Shield` because it reduces
shield/weapon evasion layers, not a stat, equipment slot, or output value.

`Rend Helm` is deliberately not accepted as a separate action here. The current formula model has
armor-class response, not a clean head/body split. Its vanilla equipment-break vocabulary is folded
into `Rend Armor` for this W3 pass. If the implementation pass exposes a meaningful head-slot rule,
`Rend Helm` may split back out only by dividing the existing exposure budget, not by adding another
stacking vulnerability.

### Knight Damage Rows

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| `Guarded Strike` sword x0.85 | 66 | 76 | 96 | 102 |
| `Rend Weapon` sword x0.50 | 39 | 45 | 57 | 60 |
| `Rend Armor` sword x0.50 | 39 | 45 | 57 | 60 |
| `Shield/stat Rend` sword x0.35 | 27 | 31 | 39 | 42 |
| `Crushing Blow` fists x0.85 | 68 | 57 | 60 | 60 |

Read:

- `Guarded Strike` is an active Knight safety tool, not a damage upgrade;
- `Rend Weapon` and `Rend Armor` spend immediate damage to create a controlled follow-up window;
- `Crushing Blow` does not beat Monk's fist reference and does not give Knight Monk's sustain.

### Knight T6/T4 Rows

`Rend Armor` T6 rows, base damage per hit 100:

| Armor | Type | Base response | After `Rend Armor` | Projected damage |
| --- | --- | ---: | ---: | ---: |
| plate | swing | 0.65 | 0.77 | 77 |
| plate | crush | 1.15 | 1.20 | 120 |
| mail | missile | 1.10 | 1.20 | 120 |
| leather | thrust | 0.95 | 1.01 | 101 |
| cloth | swing | 1.00 | 1.00 | 100 |

`Shield Break` T4 rows, base hit 80, class evade 10, shield evade 30, accessory evade 10:

| Facing | Before | After |
| --- | ---: | ---: |
| front | 45 | 55 |
| side | 50 | 61 |
| rear | 72 | 72 |

`Rend Weapon` enemy-offense rows:

| Incoming weapon damage | After `Rend Weapon` |
| ---: | ---: |
| 80 | 60 |
| 120 | 90 |

Knight stat-pressure rows:

| Effect | Before | After |
| --- | ---: | ---: |
| `Rend Weapon` plus `Rend Power` same action | 100 | 75 |
| `Rend Power` physical output | 100 | 85 |
| `Rend Magick` magic/healing output | 100 | 85 |
| `Rend Speed` Speed | 7 | 6 |
| `Rend MP` max MP 80 | 80 | 60 |
| `Rend MP` max MP 140 | 140 | 110 |

These rows keep Knight control visible but bounded. They also create W4 pressure: if every physical
party wants one Knight setup action, `Rend Armor`, `Shield Break`, or the stat-pressure line must be
narrowed further.

## Archer Values

Archer should be the best native bow/crossbow shell before `Equip Bow` or `Concentration` exports.

| Skill | Value | MP | CT | JP | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `Quick Shot` | bow/crossbow x0.75, +0.10 hit | 0 | 0 | 90 | T4/F5 | Reliable low-commitment shot, not higher expected damage than normal at ordinary hit rates. |
| `Aimed Shot` | bow/crossbow x1.35, +0.15 hit | 0 | 2 | 180 | T5/T4/F5 | Delayed payoff; fails if target leaves chosen panel/line; same-tick unsafe. |
| `Pinning Shot` | bow/crossbow x0.70, -12 CT, Move -1 | 0 | 0 | 220 | T5/T10/T4 | Until target's next action; no Stop, no Immobilize, no hard lock. |
| `Piercing Shot` | bow/crossbow x1.10, +0.20 penetration | 0 | 1 | 320 | T6/F5 | Requires line or exposed target; does not help guns. |
| `High-Ground Shot` | bow/crossbow x1.15, +0.10 hit | 0 | 0 | 180 | T4/T11/F5 | Requires height advantage 2 or more; no bonus on flat maps. |
| `Covering Shot` | deferred | - | - | - | T5/T8/T10 | No overwatch or interrupt lock accepted in this pass. |

`Aimed Shot` and `Piercing Shot` put the Archer in the normal Charging risk state until resolution.
They do not get an exemption from suspended evasion or boosted incoming physical damage while queued.

### Archer Damage Rows

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| `Quick Shot` longbow x0.75 | 57 | 74 | 65 | 68 |
| `Quick Shot` crossbow x0.75 | 64 | 77 | 71 | 73 |
| `Aimed Shot` longbow x1.35 | 102 | 133 | 118 | 123 |
| `Aimed Shot` crossbow x1.35 | 115 | 140 | 127 | 132 |
| `Pinning Shot` longbow x0.70 | 53 | 69 | 61 | 63 |
| `Pinning Shot` crossbow x0.70 | 59 | 72 | 66 | 68 |
| `Piercing Shot` longbow x1.10 pen+0.20 | 89 | 108 | 99 | 102 |
| `Piercing Shot` crossbow x1.10 pen+0.20 | 100 | 114 | 107 | 109 |
| `High-Ground Shot` longbow x1.15 | 87 | 113 | 100 | 105 |
| `High-Ground Shot` crossbow x1.15 | 98 | 119 | 108 | 112 |

`Aimed Shot` has the largest direct numbers, but it pays CT 2 and target-prediction risk.
`Piercing Shot` has only CT 1, so its damage is kept below `Aimed Shot` and tied to a line/exposed
target requirement. `High-Ground Shot` is strong only when the map lets Archer own height.

### Archer Tempo Row

`Pinning Shot` does not hard-control the target.

| Target CT before | Target CT after |
| ---: | ---: |
| 78 | 66 |
| 10 | 0 |

This should create a tactical window, not solve the fight. If W4 finds that stacking Archers can
deny too many turns, reduce CT damage or make the effect once per target per round.

## Timing Invariant Check

| Invariant | Result |
| --- | --- |
| I1 starter tools not permanent defaults | Preserved: Knight/Archer are Band B first-specialists, not starter replacements. |
| I2 first specialists work before exports | Main purpose: active Knight/Archer get action identity before equipment/support exports. |
| I3 deep secondary on shallow chassis | Watch: Knight body plus Monk secondary and Archer reliability are explicit W4 rows. |
| I4 build-defining supports earned | Preserved here: equipment unlocks and Concentration remain deferred. |
| I5 strong reactions not early skips | Preserved here: `Parry`, `Arrow Guard`, and `Speed Save` not finalized. |
| I6 mobility remains a choice | Preserved here: `Jump +1` and `Shield March` not strengthened. |
| I7 sustain has texture | Not touched directly. |
| I8 protection not upkeep | Watch: `Guarded Strike` and future `Parry` must not stack into routine upkeep. |
| I9 control not replacing damage | Watch: `Rend Weapon`, `Shield Break`, and `Pinning Shot` create windows, not locks. |
| I10 late jobs powerful not mandatory | Not touched. |

## Open W3 Links

Still required:

- RSM producer must validate `Parry`, `Brace`, `Equip Armor`, `Equip Shield`, `Defensive Training`,
  `Shield March`, `Arrow Guard`, `Speed Save`, `Equip Bow`, `Concentration`, `Bow Mastery`, and
  `Jump +1`;
- equipment shop/gil producer must pin sword, shield, armor, bow, and crossbow timing;
- W4 must count Knight action secondary incidence and Archer action secondary incidence;
- W5 must test Knight-body plus Monk secondary, Archer reliability, and early armor/shield support.

## Claude Review Request

Claude should review whether:

- Knight action values make active Knight useful before armor/shield exports;
- `Rend Armor`'s armor-specific delta capped at 1.20 is modest enough or too setup-efficient;
- `Shield Break` respects T4 by only reducing shield/weapon guard layers;
- `Rend MP`, `Rend Speed`, `Rend Power`, and `Rend Magick` cover the vanilla stat-down family
  without replacing Mystic, Time Mage, Orator, or caster-control jobs;
- Archer values make active Archer useful without making `Concentration` necessary;
- `Challenge` and `Covering Shot` should remain deferred, or need bounded concrete values now.
