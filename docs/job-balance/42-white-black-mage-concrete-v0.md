# White Mage And Black Mage Concrete Provisional V0

Status: Accepted for concrete-provisional action values
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/19-white-black-mage-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/33-mitigation-stack-composition-schema.md`
- `docs/job-balance/35-resource-economy-model-schema.md`
- `docs/job-balance/38-spell-routing-reflect-composition-schema.md`
- `docs/job-balance/41-area-hp-over-time-composition-schema.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/gpt-wm-bm-concrete-v0.json`

## Purpose

This is the first concrete-provisional value pass for White Mage and Black Mage.

It sets provisional action-skill values for review. It does not set final implementation data
because `work/baseline_weapons.csv` is still missing and formula-balance v1 has not replaced the
v0.2.1 provisional baseline.

This pass intentionally does not finalize reaction, support, or movement values. T2.1 populated
build incidence is still pending concrete accepted-provisional builds, so RSM numbers would be
premature. White Mage and Black Mage RSM skills remain design placeholders until a later T2.1 pass
can test mandatory-piece pressure across real caster builds.

## Shared Formula

Offensive magic and healing use the current v0.2.1 Faith routine:

```text
amount = floor(round(K * MA * max(0.60, casterFaith * targetFaith / 10000)) * ordinary_layers)
```

Percent-of-max-HP effects use the same round-before-floor guard:

```text
percent_amount = floor(round(percent_of_max_hp * max_hp, 6))
```

This applies to Raise/Arise revive HP, Regen ticks, Poison ticks, and any future percent-based
HP drain or recovery effect.

Default simulation rows use:

```text
casterFaith = 70
targetFaith = 70
faith_factor = 0.60
Shell = 0.667
Protect = 0.667
Weak element = 2.0
```

The weak-element multiplier follows vanilla FFT behavior. This is why Black Mage elemental K values
are deliberately lower than Flare: element selection should be strong when the target is weak, but
Firaja should not become a universal non-elemental nuke.

## White Mage Values

White Mage remains delayed Faith-linked recovery and protection.

| Skill | K / value | MP | CT | JP | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `Cure` | K 14 | 5 | 2 | 50 | T3/T3xT5/T9 | Early delayed heal beats Potion only when timing allows. |
| `Cura` | K 20 | 12 | 3 | 180 | T3/T3xT5/T9 | Mid basic recovery. |
| `Curaga` | K 26 | 22 | 4 | 450 | T3/T3xT5/T9 | High recovery with real CT/MP pressure. |
| `Curaja` | K 32 | 34 | 5 | 800 | T3/T3xT5/T9 | Premium recovery; not emergency-safe. |
| `Raise` | 25% max HP | 12 | 4 | 200 | T3xT5 | Must resolve before death clock; same-tick is unsafe. |
| `Arise` | 70% max HP | 30 | 5 | 600 | T3xT5 | Strong revive, expensive and slower. |
| `Reraise` | one auto-revive status | 36 | 5 | 1000 | T3/T5/T9 | Preemptive safety, not upkeep. |
| `Regen` | 10% max HP per tick, 4 ticks | 10 | 2 | 350 | T3/T5/T3xT5xT11 | Attrition prevention, not burst healing. |
| `Protect` | 0.667 physical layer | 6 | 2 | 70 | T6xPS/T9 | Separate ordinary mitigation layer. |
| `Protectja` | 0.667 physical layer, area | 20 | 5 | 600 | T6xPS/T11/T9 | Area protection pays with CT/MP. |
| `Shell` | 0.667 magic layer | 6 | 2 | 70 | T6xPS/T9/F4 | Separate ordinary mitigation layer. |
| `Shellja` | 0.667 magic layer, area | 20 | 5 | 600 | T6xPS/T11/T9/F4 | Area Shell pays with CT/MP. |
| `Wall` | Protect + Shell layers | 24 | 4 | 400 | T6xPS/T9 | One target; represented as both layers active. |
| `Esuna` | status clear | 12 | 2 | 300 | T4/T9 | Reactive cleanup, not status immunity. |
| `Holy` | K 32 | 42 | 5 | 600 | F4/T9/T5 | Focused holy damage; weaker than Black Mage capstone offense. |

White Mage simulation highlights from `work/gpt-wm-bm-concrete-v0.json`:

| Row | Result |
| --- | ---: |
| Early `Cure`, MA 6 | 50 HP |
| Mid `Cura`, MA 9 | 108 HP |
| Late `Curaga`, MA 11 | 172 HP |
| Stress `Curaja`, MA 13 | 250 HP |
| Stress `Holy`, MA 13 | 250 damage before Shell |

Timing checks:

| Scenario | Result |
| --- | --- |
| Potion 30 now, 40 HP target, 60 damage at tick 2 | survives at 10 HP |
| Cure 50 at CT 2, 60 damage also at tick 2 | same-tick unsafe, KO |
| Cure 50 at CT 2, 60 damage at tick 3 | survives at 30 HP |
| Cura 72 at CT 3, 90 damage at tick 4 | survives at 32 HP |

Revive checks:

| Scenario | Result |
| --- | --- |
| `Raise` CT 4 into death clock 5 | resolves, 45 HP on 180 max HP target |
| `Raise` CT 5 into death clock 5 | same-tick unsafe, fails |
| `Arise` CT 5 into death clock 6 | resolves, 126 HP on 180 max HP target |

## Black Mage Values

Black Mage remains Faith-linked elemental offense. Elements are not a pure bigger-number ladder:
lower tiers are faster and cheaper; upper tiers buy area/reach/payoff; Flare is the non-elemental
capstone when elemental routing is wrong.

| Skill group | K | MP | CT | JP | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `Fire` / `Thunder` / `Blizzard` | 14 | 5 | 2 | 50 | F4/T5/T9 | Cheap first-tier offense. |
| `Fira` / `Thundara` / `Blizzara` | 18 | 12 | 3 | 200 | F4/T5/T9 | Main mid tier. |
| `Firaga` / `Thundaga` / `Blizzaga` | 21 | 22 | 4 | 500 | F4/T5/T9 | High tier, still element-dependent. |
| `Firaja` / `Thundaja` / `Blizzaja` | 22 | 34 | 5 | 900 | F4/T5/T9/T11 | Premium tier pays mostly for area/reach and affinity payoff. |
| `Poison` | 10% max HP per tick, 4 ticks; 70% base status | 8 | 2 | 150 | T3/T4/T5/T9 | Attrition status; immunity and undead behavior must be respected. |
| `Toad` | 35% base status | 24 | 4 | 500 | T4/T5/T9 | Hard control, not a boss answer. |
| `Death` | 25% base instant KO | 36 | 5 | 600 | T4/T5/T8/T9 | High-risk fantasy; immunity-heavy. |
| `Flare` | K 30 | 46 | 6 | 1000 | F4/T5/T9 | Non-elemental burst; expensive and slower than elements. |

Black Mage simulation highlights from `work/gpt-wm-bm-concrete-v0.json`:

| Row | Neutral | Shell | Weak x2 | Stress neutral / top physical |
| --- | ---: | ---: | ---: | ---: |
| Early tier I, MA 8 | 67 | 44 | 134 | - |
| Mid tier II, MA 12 | 130 | 86 | 260 | - |
| Late tier III, MA 15 | 189 | 126 | 378 | - |
| Stress tier IV, MA 18 | 238 | 158 | 476 | 0.573 |
| Stress `Flare`, MA 18 | 324 | 216 | n/a | 0.781 |

Scale checks:

| Row | Result |
| --- | ---: |
| Stress tier IV neutral / Black Mage HP 390 | 0.610 |
| Stress tier IV weak x2 / Black Mage HP 390 | 1.221 |
| Stress `Flare` / Black Mage HP 390 | 0.831 |
| Stress `Flare` / top physical 415 | 0.781 |

These rows keep normal elemental damage below Flare against neutral targets, keep weak-element payoff
inside the v0.2.1 scale-band limit, and keep the non-elemental capstone below the strongest physical
stress reference.

Early weak-element rows can be much higher than neutral rows because vanilla `Weak` doubles
elemental damage. That is acceptable only because it is conditional target-profile payoff. The real
roster F5 pass must recheck weak-element burst once actual per-job HP/MA and spell availability are
bound to campaign phases.

## Mitigation And Upkeep Checks

T6xPS remains load-bearing for White Mage defensive values.

| Case | Base response | Protect/Shell | Wall both layers | Wall distance from 0.25 clamp |
| --- | ---: | ---: | ---: | ---: |
| Plate swing | 0.65 | 0.43355 | 0.289178 | 0.039178 |
| Mail swing | 0.75 | 0.50025 | 0.333667 | 0.083667 |
| Cloth magic | 1.00 | 0.667 | 0.444889 | 0.194889 |

The worst ordinary Wall case stays above the low clamp. Rows with additional elemental resistance
and bad Zodiac may still hit the clamp, but that is a target-profile counter case, not ordinary
Protect/Shell upkeep.

T9 MP sequences:

| Sequence | Starting MP | Casts | Ending MP | Read |
| --- | ---: | --- | ---: | --- |
| BM early basic elements | 45 | four tier-I spells | 25 | Cheap spells are available. |
| BM mid mixed elements | 80 | Fira, Firaga, Fira, Firaga | 12 | Mid mixed casting spends most MP. |
| BM late capstone pressure | 110 | Flare, Firaja, Flare | 30 | Second Flare fails; capstones are limited. |
| WM support mixed | 80 | Cure, Cura, Raise, Protect, Esuna | 33 | Support caster can cover a mixed fight. |
| WM Wall upkeep pressure | 80 | Wall x4 | 8 | Fourth Wall fails; Wall cannot be permanent free upkeep. |

The MP pools above are tactical scenario assumptions for T9, not final MP growth data.

## Lane Separation

Accepted lane separation after this pass:

- Chemist remains the emergency healer because items are immediate and ignore CT/Faith.
- White Mage wins when delayed, Faith-linked healing/protection has time to resolve.
- Monk revive/sustain remains frontline and positional, not replaced by White Mage.
- Black Mage wins through elemental offense, Shell/Faith-aware magic, and capstone Flare.
- Summoner should still own larger delayed area payoff later; Black Mage's area tiers are smaller
  and more element-dependent.

## Deferred Items

Still deferred:

- final RSM values for both jobs, pending T2.1 populated build incidence;
- full five-metric formula sweep for these new spell K constants, including no-dominance and magic
  coexistence, during the T1/real-roster F5 pass;
- exact area shapes and ranges for spell tiers, pending data-layout proof and T11 use;
- exact status formula implementation for Poison, Toad, and Death after T4/T8 review;
- exact MP pools and JP economy after campaign pacing work;
- final status duration constants if data mod constraints differ from these provisional rows;
- final acceptance until T1 Windows weapon dump and formula-balance v1.

## Claude Review Request

Claude should review whether:

- White Mage healing values preserve Chemist and Monk lanes;
- Raise/Arise timing uses T3xT5 correctly;
- Protect/Shell/Wall stay clear of practical immunity under T6xPS;
- Black Mage elemental K values are high enough to fix magic weakness without making weak-element
  hits exceed the scale band;
- Flare is strong enough to matter but not strong enough to erase elements;
- MP costs preserve T9 pressure;
- this action-only concrete pass is acceptable while RSM values wait for T2.1.

Claude review verdict: accepted as concrete-provisional by claude-opus-4-8 on 2026-06-21.

Review notes:

- all 72 White Mage and Black Mage phase rows independently recomputed with 0 errors;
- T6xPS mitigation products, T3xT5 healing/revive races, and T9 MP sequences independently
  reproduced;
- `Arise` revive HP uses the central percent-of-max-HP guard and resolves to 126 HP on a 180 max HP
  target;
- final implementation acceptance remains blocked by T1 Windows weapon dump, formula-balance v1, and
  full real-roster F5 re-sim;
- RSM values remain deferred to a T2.1 populated build-incidence pass.
