# Mitigation And Scaling Policy V0

Status: Accepted
Date: 2026-06-20
Review: Approved by Claude on 2026-06-20 after confirming the C-bounded model, `MISSILE` as a
fourth physical type, Marcelo's full-plate constraint, and dual-sim comparability on the v0
bundle.
Depends on:
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/formula-balance/06-damage-spec-v0.md`
- `docs/formula-balance/08-scenario-set-v0.md`
- `docs/formula-balance/09-combat-formula-design-frame.md`

## Purpose

This document records the first locked mitigation and scaling policy for Generic Chronicle
formula simulations.

It is a decision record, not a final numeric formula set. It defines the combat-response language
that family-specific formulas must use before the project starts tuning exact weapon-family
numbers.

Until IVC weapon data is captured and the Windows proof session in `04-proof-and-baseline-plan.md`
runs, any formula using this policy can only be:

```text
Conceptually viable, pending verified-baseline re-sim
```

## Decision Summary

### Q1 - Defense Fork

Use Option C from `09`: coarse percent/type response without general point DR.

Generic Chronicle will not add a normal subtractive `ArmorRating` to the core damage formula.
Armor, shields, robes, clothing, accessories, statuses, elements, and target build should express
damage texture through readable response buckets.

Subtractive damage reduction may exist later only as a rare special effect. It is not the default
armor model.

Reason:

- protects low-gross and multi-hit families from being gutted by flat subtraction;
- reuses FFT's existing percent-modifier language such as Protect, Shell, element, and Zodiac;
- gives weapon families real matchup identity without turning FFT into a tabletop armor model.

### Q2 - Penetration Source

Penetration is mostly family and delivery fixed.

Under this policy, penetration does not mean subtracting armor points. It means the action's
delivery mode maps to a response bucket the target does not resist as strongly.

Small skill-rank or technique influence may be used as secondary tuning. PA should not be the
main source of penetration, because that would let high-PA builds scale both pressure and bypass.

### Q3 - Player Visibility

`swing`, `thrust`, `crush`, and `missile` are internal design modes.

The player should read the system through weapon feel, target matchups, damage behavior, and
eventual short help text. The mod should not expose a tabletop-style damage-type matrix as a new
primary UI concept.

### Q4 - Buff Floors And Control Dominance

The default posture remains: buff weak-family floors before cutting loved strong tools.

There is one explicit exception. Dominance engines are pre-authorized ceiling-control targets:

- Two Hands / Doublehand;
- Two Swords / Dual Wield;
- Brave-stacked reactions;
- Martial Arts / Brawler;
- Attack Boost;
- extreme Brave/Faith optimization.

This is not an anti-sword policy. It is a scale-protection policy: do not buff every other family
up to a broken premium-sword support stack.

### Q5 - Brave/Faith Policy

Use an F3-leaning hybrid with F2-lite support.

Brave should remain an identity stat, but high-Brave offense and Brave-scaled defensive reactions
should saturate or move partly toward reliability/technique above a threshold. Defensive reaction
extremes must be capped before they approach practical immunity.

Faith should remain meaningful and somewhat double-edged, but target-Faith punishment should be
softened or capped enough that magic builds are not self-punishing by default.

### Q6 - Magic Mitigation

Magic should mirror the same percent-response language as physical damage.

Default magic/spirit mitigation uses Faith, Shell, element, spirit response, status, and equipment
as percent-style response layers. The default magic model does not use subtractive
`SpiritResistance`.

## Physical Damage Types

V0 uses four physical damage types.

| Type | Design meaning | Typical families |
| --- | --- | --- |
| `swing` | broad cutting, slashing, or chopping force | swords, knight swords, katanas, cutting axes, cloth |
| `thrust` | focused point force delivered by reach or precision | knives, polearms, spear-like attacks |
| `crush` | impact, concussion, or guard-breaking force | flails, maces/hammers if added, heavy axes, bags, fists |
| `missile` | ranged projectile force with its own armor and range identity | bows, crossbows, guns |

`missile` is intentionally separate from `thrust`. Arrows, bolts, and bullets need different
range, reliability, armor, and support interactions from hand-delivered spear or knife thrusts.

Hybrid families may choose different types for different actions if future formula proposals
justify it. For example, an axe family may contain both cutting and impact identities, but each
accepted action must name one primary damage type for simulation.

## Armor-Class Response Direction

These are directional buckets, not final multipliers.

Bucket meanings:

- `LOW`: target resists this damage type.
- `MID`: no strong resistance or vulnerability; exact value may sit near neutral.
- `HIGH`: target is vulnerable or comparatively weak to this damage type.
- `FULL`: target has no meaningful physical mitigation through this armor class; damage is not
  reduced by armor class, though other layers can still apply.

| Armor class | Swing | Thrust | Crush | Missile | Design note |
| --- | --- | --- | --- | --- | --- |
| Full plate | LOW | LOW | HIGH | MID-LOW | hard requirement: plate reduces both swing and thrust; weakness is crush/impact |
| Mail / chain | LOW | HIGH | MID | HIGH | rings blunt cuts but points, bolts, and similar focused attacks exploit openings |
| Leather / light | MID | MID | MID | MID | modest physical profile; defense should lean more on evasion, mobility, and utility |
| Cloth / robe | FULL | FULL | FULL | FULL | physically fragile; defense lives mostly on magic/spirit/status/equipment utility |

The full-plate rule is mandatory. It creates an observable target where crush/impact families are
the best physical answer and sword/spear-like families are naturally tempered without a global
nerf.

## Magic And Spirit Axis

Magic and spirit effects use a parallel response language, but they are not physical armor
matchups.

Initial channels:

| Channel | Main response levers |
| --- | --- |
| `magic` | Faith policy, Shell, element, robe/accessory effects, status |
| `spirit` | Faith/Brave policy, Shell or status-specific protection, robe/accessory effects |
| `drain` | HP/MP state, undead/special resistance, strict caps |
| `pure` | rare, expensive, CT/MP/status-gated, minimal mitigation |

Robe and cloth identity should mostly live here. Cloth taking `FULL` physical damage does not
mean cloth users are defenseless; it means their defense should come from FFT-like magic,
mobility, evasion, status, MP, and utility levers instead of physical armor response.

## Operation Order

V0 simulations use this order:

```text
base_pressure = family routine output
type_layer = type_response(target armor class, damage type)
protect_shell_layer = Protect or Shell if applicable
element_layer = element response if applicable
zodiac_layer = Zodiac response if applicable
combined_response = type_layer * protect_shell_layer * element_layer * zodiac_layer
bounded_response = clamp(combined_response, min_total_multiplier, max_total_multiplier)
final_damage = floor(base_pressure * bounded_response)
visible_damage = max(chip_floor, final_damage) for positive base_pressure
```

The harness must report both `combined_response` and `bounded_response`. If a candidate only
works because the cap hides an extreme stack, that is a design warning.

Hard outcomes such as element nullify or absorb are not ordinary percent mitigation. When a
formula proposal depends on nullify or absorb, it must report those cases separately rather than
hiding them inside the stacking cap.

## Stacking Discipline

The stack cap exists to prevent percent layers from producing near-zero damage or runaway burst
when type response, Protect/Shell, element, and Zodiac all line up.

The pinned `work/sim-inputs-v0.json` bundle currently defines these provisional V0 simulation
caps:

```text
min_total_multiplier = 0.25
max_total_multiplier = 2.50
chip_floor = 1
```

These are shared simulation constants, not final game tuning. They live in the pinned bundle so
Claude and GPT cannot accidentally simulate different caps. If a cap changes, the bundle must
change and both simulators must re-run from the same artifact.

Rules:

- type response always participates because it is the core weapon-family matchup layer;
- Protect/Shell, element, and Zodiac remain meaningful, but their product is bounded;
- the cap is applied after multiplying all ordinary percent layers;
- nullify, absorb, immunity, and hard status prevention are reported as separate tactical cases;
- no proposal may claim a pass without showing uncapped and capped multipliers.

## Design Payoffs

### Floor-Raising

The armor matchup map gives weak or awkward families a reason to exist.

Most importantly, crush/impact families receive a signature target: full plate. If axes, flails,
bags, fists, or future mace-like tools cannot exploit plate better than swords and thrust weapons,
the policy has failed its main purpose.

### Matchup-Based Dominance Control

The same map controls dominant families without blanket nerfs.

Swords and spears can stay strong, familiar, and desirable, but they should not be the best answer
to every armored target. Full plate naturally tempers both swing and thrust. Mail creates a
different problem where thrust and missile tools are favored. Leather and cloth avoid becoming
universal anti-physical walls.

## Simulation Implications

Every global formula simulation must show:

- plate: crush beats swing and thrust;
- mail: thrust beats swing;
- missile has at least one real matchup identity distinct from thrust;
- leather does not erase family identity through excessive mitigation;
- cloth remains physically vulnerable but can be defended through magic/spirit/utility levers;
- no family becomes best across most late/stress scenarios;
- magic output remains competitive with strong physical output over a tactical window.

The first simulation harness should consume a pinned `sim-inputs-v0` bundle and emit the required
columns from `08-scenario-set-v0.md` where data exists. Missing weapon data must be labeled
`missing-weapon-baseline`.

The first pinned bundle is:

```text
work/sim-inputs-v0.json
```

The first tuned candidate bundle is:

```text
work/sim-inputs-v0.1.json
```

Supporting notes:

```text
work/sim-inputs-v0-notes.md
```

Important provisional constants in that bundle:

- penetration ceiling: `1.10`;
- combined multiplier clamp: `[0.25, 2.50]`;
- Protect multiplier: `0.667`;
- neutral Zodiac: `1.00`;
- default Brave/Faith: `70`.
- v0.1 mail-vs-missile response: `1.10`, giving missile a real mail matchup identity.

Pinned v0.2 simulation conventions:

- Effective weapon power is `wp_eff = wp * phase_wp_scalar`, kept as a float. Do not round
  `wp_eff`; floor only at the final damage step.
- Viability metrics are scoped by lens. A single-hit benchmark, a dual-wield benchmark, and an
  engine benchmark should not be collapsed into one raw damage comparison.
- Volatile families may credit maximum damage for their own viability, but their maximum damage
  must not raise the benchmark used to judge every other family.

## Review Notes

Claude approved this policy as the baseline. Future reviews should focus on:

1. Whether `MISSILE` as a fourth physical type is represented correctly.
2. Whether the plate/mail/leather/cloth response directions match the intended policy.
3. Whether the pinned `[0.25, 2.50]` clamp from `work/sim-inputs-v0.json` is acceptable for the
   first simulation loop.
4. Whether the operation order is precise enough for independent simulation.
5. Whether any wording accidentally reintroduces point DR as the default model.

This policy is now the base for family-specific simulation proposals and for
`tools/sim_damage.py`.
