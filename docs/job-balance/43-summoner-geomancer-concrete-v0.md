# Summoner And Geomancer Concrete Provisional V0

Status: Accepted for concrete-provisional action values
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/21-summoner-geomancer-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/33-mitigation-stack-composition-schema.md`
- `docs/job-balance/34-area-terrain-model-schema.md`
- `docs/job-balance/35-resource-economy-model-schema.md`
- `docs/job-balance/38-spell-routing-reflect-composition-schema.md`
- `docs/job-balance/41-area-hp-over-time-composition-schema.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/gpt-summoner-geomancer-concrete-v0.json`

## Purpose

This is the first concrete-provisional action value pass for Summoner and Geomancer.

It uses the vanilla skill/effect/status reference created before this pass, but it does not try to
preserve vanilla power values. The objective is to preserve recognizable FFT identities while making
both jobs usable inside the new formula direction.

This pass intentionally does not finalize reaction, support, or movement values. T2.1 populated
build incidence is still pending concrete accepted-provisional builds, so RSM numbers would be
premature.

## Shared Formula Contracts

Summon damage and instant summon healing use the current v0.2.1 Faith routine:

```text
amount = floor(round(K * MA * max(0.60, casterFaith * targetFaith / 10000)) * ordinary_layers)
```

Default simulation rows use:

```text
casterFaith = 70
targetFaith = 70
faith_factor = 0.60
top_physical_stress_reference = 415
Black Mage stress tier I = 151
Black Mage stress tier IV = 238
Black Mage stress Flare = 324
```

`Lich` and any future proportional HP summon do not use MA and do not use the ordinary Faith damage
band:

```text
percent_amount = floor(round(percent_of_current_hp * current_hp, 6))
per_target_amount = min(percent_amount, per_target_cap)
```

Geomancy must reuse the validated `pampa_wp` hybrid routine:

```text
base = floor((PA + MA) / 2) * terrain_wp
damage = floor(round(base * armor_response[target_armor][damage_type], 6))
```

This is deliberately not a new terrain formula. Terrain identity comes from terrain access,
damage type, element, and status rider, not from a new arithmetic routine.

## Summoner Values

Summoner is a delayed area caster. The V0 numeric lane is:

- low per-target damage compared with single-target capstones;
- strong value when two or three targets are clustered;
- expensive enough that premium summons are not routine every fight;
- no ordinary summon total exceeds the current stress physical reference under neutral damage;
- elemental weak x2 two-target proof remains below the stress physical reference, while three
  weak targets is explicitly deferred to real-roster F5.

| Skill | Effect | K / value | MP | CT | JP | Area proof | Gate binding |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `Moogle` | area healing | K 9 | 8 | 3 | 110 | max 3 allies | T3/T3xT5/T11/T9 |
| `Shiva` | ice area damage | K 9 | 18 | 4 | 200 | max 3 enemies | F4/T5/T11/T9 |
| `Ramuh` | lightning area damage | K 9 | 18 | 4 | 200 | max 3 enemies | F4/T5/T11/T9 |
| `Ifrit` | fire area damage | K 9 | 18 | 4 | 200 | max 3 enemies | F4/T5/T11/T9 |
| `Titan` | earth area damage | K 10 | 22 | 5 | 220 | max 3 enemies | F4/T5/T11/T9 |
| `Golem` | physical protection | Protect-equivalent 0.667 layer | 32 | 3 | 500 | max 3 allies | T6xPS/T11/T9 |
| `Carbuncle` | Reflect routing | Reflect status | 28 | 4 | 350 | max 3 allies | T8xSR/T11/T9 |
| `Bahamut` | non-elemental area damage | K 15 | 60 | 7 | 1000 | max 3 enemies | F4/T5/T11/T9 |
| `Odin` | non-elemental area damage | K 14 | 54 | 6 | 900 | max 3 enemies | F4/T5/T11/T9 |
| `Leviathan` | water area damage | K 11 | 50 | 6 | 860 | max 3 enemies | F4/T5/T11/T9 |
| `Salamander` | fire area damage | K 11 | 50 | 6 | 860 | max 3 enemies | F4/T5/T11/T9 |
| `Sylph` | wind/spirit area damage | K 8 | 16 | 4 | 400 | max 3 enemies | F4/T5/T11/T9 |
| `Faerie` | area healing | K 13 | 24 | 4 | 400 | max 3 allies | T3/T3xT5/T11/T9 |
| `Lich` | current-HP drain | 25% current HP, cap 120, 50% drain | 50 | 5 | 600 | max 2 enemies | T3/T5/T11/T9/T37 |
| `Cyclops` | non-elemental area damage | K 14 | 58 | 6 | 1000 | max 3 enemies | F4/T5/T11/T9 |
| `Zodiark` | hidden ultimate | deferred | deferred | deferred | 9999 | separate proof | hidden/boss late proof |

`Golem` uses the same physical mitigation layer as `Protect` and does not stack with `Protect`.
That keeps it from becoming a mandatory defensive upkeep loop while still giving Summoner a faster,
more expensive physical protection summon.

`Carbuncle` is a routing tool, not pure magic immunity. It inherits the accepted T8xSR backfire,
fizzle, targetability, and one-reflection-only rules.

`Zodiark` is retained as a hidden reward vocabulary item, but it is not part of ordinary V0 balance.
It should not raise the normal Summoner ceiling before a separate late/boss proof pass.

### Summoner Area Checks

Stress rows use Summoner MA 15 and the default Faith floor.

| Skill | Per target | Max targets | Neutral total | Total / 415 | Weak x2 proof targets | Weak x2 proof total |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `Shiva` / `Ramuh` / `Ifrit` | 81 | 3 | 243 | 0.586 | 2 | 324 |
| `Titan` | 90 | 3 | 270 | 0.651 | 2 | 360 |
| `Sylph` | 72 | 3 | 216 | 0.520 | 2 | 288 |
| `Leviathan` / `Salamander` | 99 | 3 | 297 | 0.716 | 2 | 396 |
| `Odin` / `Cyclops` | 126 | 3 | 378 | 0.911 | n/a | n/a |
| `Bahamut` | 135 | 3 | 405 | 0.976 | n/a | n/a |

The important read is that Summoner beats Black Mage through area total, not through per-target
damage. Stress `Bahamut` at 135 per target is far below stress `Flare` at 324 on a single target,
but a three-target cluster reaches 405 total after paying CT 7 and MP 60.

`Bahamut` is the binding Summoner row for the real-roster F5 sweep. At 405/415, it has almost no
headroom. If the T1 weapon dump lowers the real top-physical reference, or if real Summoner MA
exceeds the MA 15 anchor, three-target neutral `Bahamut` crosses the provisional ceiling first.

The dangerous row is three weak-element targets for fire summons:

```text
Salamander stress weak x2 on 3 targets = 594 total
Ifrit stress weak x2 on 3 targets = 486 total
```

That case is not accepted as final. It is explicitly deferred to the real-roster F5 pass because it
is constructible inside this same job pair: `Magma Surge` can apply `Oil`, and a Summoner can then
convert the prepared fire weakness with `Ifrit` or `Salamander` into the over-ceiling area row. F5
must test this built combo directly, not only natural enemy weakness density. Possible final levers
include a per-cast total ceiling, diminishing value per extra weak target, lower fire-summon K, or
stricter terrain/area constraints. Until that pass, premium elemental summon proof only accepts weak
x2 on two targets.

### Summoner Healing And Lich Checks

| Skill | Early | Mid | Late | Stress | Max stress total |
| --- | ---: | ---: | ---: | ---: | ---: |
| `Moogle` | 32 | 54 | 65 | 81 | 243 |
| `Faerie` | 47 | 78 | 94 | 117 | 351 |

`Moogle` and `Faerie` are deliberately weaker per target than White Mage's focused healing. They
become attractive when multiple allies can wait for the delayed resolution.

`Lich` uses current HP and a cap, not MA:

| Target current HP | Raw 25% | Per-target after cap | Max 2-target damage | Max drain healing |
| ---: | ---: | ---: | ---: | ---: |
| 180 | 45 | 45 | 90 | 44 |
| 390 | 97 | 97 | 194 | 96 |
| 624 | 156 | 120 | 240 | 120 |

This makes `Lich` a high-HP pressure tool without letting Summoner scale it through MA, Faith, or
premium caster gear.

### Summoner MP Checks

| Scenario | Starting MP | Successful casts | Failed casts | Ending MP | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| Early budget | 55 | 4 | 1 | 3 | Basic summons are available, but repeated damage runs dry. |
| Mid support mix | 90 | 3 | 1 | 16 | Defense/heal/damage mixing has real opportunity cost. |
| Late premium pressure | 120 | 3 | 1 | 2 | One premium plus support is possible; repeated premiums fail. |
| Stress premium chain | 150 | 2 | 1 | 36 | Two premium summons are possible; a third does not fit. |

## Geomancer Values

Geomancer is a mail-armored terrain hybrid. The V0 numeric lane is:

- no MP and no CT on Geomancy;
- modest damage because free reliable pressure cannot compete with dedicated casters;
- status riders are useful, but gated by terrain availability and status immunity;
- terrain actions use `pampa_wp`, not a new formula;
- Geomancer wins through map fit and flexibility, not raw damage.

All Geomancy records keep JP 150 in V0.

| Skill | Terrain WP | Type | Element axis | Status rider | Status rate | Terrain availability |
| --- | ---: | --- | --- | --- | ---: | ---: |
| `Sinkhole` | 8 | crush | earth | Immobilize | 25% | 0.50 |
| `Torrent` | 8 | crush | water | Slow | 25% | 0.35 |
| `Tanglevine` | 7 | swing | plant | Immobilize | 30% | 0.45 |
| `Contortion` | 7 | crush | none | Confuse | 20% | 0.40 |
| `Tremor` | 9 | crush | earth | Disable | 20% | 0.50 |
| `Wind Slash` | 8 | swing | wind | Blind | 25% | 0.60 |
| `Will-o'-the-Wisp` | 8 | swing | fire/spirit | Silence | 25% | 0.45 |
| `Quicksand` | 7 | crush | earth | Slow | 30% | 0.35 |
| `Sandstorm` | 7 | swing | earth/wind | Blind | 35% | 0.35 |
| `Snowstorm` | 8 | swing | ice | Slow | 25% | 0.30 |
| `Wind Blast` | 8 | swing | wind | Immobilize | 20% | 0.55 |
| `Magma Surge` | 9 | crush | fire/earth | Oil | 30% | 0.20 |

Terrain availability is a V0 proof proxy, not final map data. It exists so the job is not balanced
as if every terrain action were always available on every map.

### Geomancer Stress Checks

Stress rows use Geomancer PA 13 and MA 13, so:

```text
floor((13 + 13) / 2) = 13
```

| Skill group | Max stress damage | Best target | Damage / BM tier I | Damage / BM tier IV | Damage / 415 |
| --- | ---: | --- | ---: | ---: | ---: |
| `Tremor` / `Magma Surge` | 134 | plate | 0.887 | 0.563 | 0.323 |
| `Sinkhole` / `Torrent` | 119 | plate | 0.788 | 0.500 | 0.287 |
| `Contortion` / `Quicksand` | 104 | plate | 0.689 | 0.437 | 0.251 |
| `Wind Slash` / `Will-o'-the-Wisp` / `Snowstorm` / `Wind Blast` | 104 | cloth | 0.689 | 0.437 | 0.251 |
| `Tanglevine` / `Sandstorm` | 91 | cloth | 0.603 | 0.382 | 0.219 |

The highest Geomancy row is 134, below stress Black Mage tier I at 151, far below stress Black Mage
tier IV at 238, and far below the current top physical stress reference at 415.

The no-dominance read uses raw Geomancy damage, not availability-adjusted damage. Terrain
availability is a map-frequency proxy; when the right terrain is present, `Tremor` and `Magma Surge`
really hit for 134 in the stress row. That raw number is still below the Black Mage tier I reference.

This is intentional. Geomancer has mail durability, no MP cost, no CT cost, weapon fallback, and
status riders. It should not win a raw damage contest against Black Mage, Knight, Monk, Archer, or
Summoner.

## Lane Separation

Accepted lane target if this pass survives review:

- Black Mage remains the faster, stronger per-target elemental caster.
- Summoner wins when delayed clustered area payoff is worth the MP and CT.
- White Mage remains the better focused healer and revive caster.
- Geomancer remains the best terrain-texture hybrid, not a caster replacement.
- Knight and Monk remain stronger direct physical pressure jobs.
- Archer remains the dedicated reliable range job; Geomancy range/status should not replace bows.

## Deferred Items

Still deferred:

- final RSM values for Summoner and Geomancer, pending T2.1 populated build incidence;
- final Summoner area shapes, vertical tolerance, and exact target filters after data-layout proof;
- weak-element three-target summon rows during real-roster F5, especially the constructible
  `Magma Surge`/`Oil` into `Ifrit` or `Salamander` combo;
- final terrain availability table after map archetype data is captured;
- exact status formula implementation for Geomancy riders after T4/T8 review;
- Golem duration, refresh behavior, and final model choice after T6xPS upkeep rows. V0 uses a
  non-stacking Protect-equivalent multiplier, but a future absorption-pool mini-model may preserve
  more of vanilla Golem's barrier identity if it can be bounded;
- Carbuncle area Reflect details after T8xSR spell-routing rows;
- `Zodiark` hidden-ultimate value after separate hidden/boss late proof;
- `Bahamut` as the tightest neutral 3-target Summoner row during T1/formula-v1/F5 re-sim;
- final acceptance until T1 Windows weapon dump and formula-balance v1.

## Claude Review Request

Claude should review whether:

- Summoner target-count-normalized totals are low enough for neutral 3-target clusters;
- the weak x2 two-target proof and three-target F5 deferral are acceptable;
- `Bahamut`, `Odin`, `Cyclops`, and Time Mage `Meteor` still have enough identity separation;
- `Moogle`, `Faerie`, and `Lich` preserve White Mage, Chemist, and Monk recovery lanes;
- `Golem` as non-stacking Protect-equivalent physical protection is acceptable for V0;
- `Carbuncle` is properly routed through T8xSR instead of being treated as immunity;
- Geomancy correctly reuses `pampa_wp`;
- Geomancer damage is low enough given no MP, no CT, mail armor, weapon fallback, and status riders;
- terrain availability/status riders give Geomancer enough identity without making it universal;
- this action-only concrete pass is acceptable while RSM values wait for T2.1.

Claude review verdict: accepted as concrete-provisional by claude-opus-4-8 on 2026-06-21.

Review notes:

- all 48 summon phase rows, 10 summon area total rows, 3 `Lich` percent-HP rows, 7 `Golem`
  mitigation rows, 4 Summoner MP sequences, and 144 Geomancy rows were independently recomputed;
- `Lich` correctly uses the percent-HP float guard and does not scale with MA;
- Geomancy reuses `pampa_wp`; no new hybrid formula was introduced;
- target-count normalization is present for Summoner and remains the binding area discipline;
- `Bahamut` is the tightest neutral F5 watch row at 405/415;
- the constructible `Magma Surge`/`Oil` into `Ifrit` or `Salamander` weak-area combo is explicitly
  deferred to real-roster F5;
- final acceptance remains gated by T1 weapon dump, formula-v1, and real-roster F5;
- RSM values remain deferred to T2.1 populated incidence.
