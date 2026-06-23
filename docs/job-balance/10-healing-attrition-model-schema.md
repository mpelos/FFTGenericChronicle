# Healing And Attrition Model Schema V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/05-squire-chemist-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/08-build-incidence-benchmark-schema.md`
- `docs/formula-balance/13-brave-faith-combat-policy-v0.md`
- `work/sim-inputs-v0.2.1.json`
- `work/t3-healing-attrition-scenarios-v0.json`

## Purpose

This document starts T3, the healing/sustain/attrition validation track.

T3 exists to validate skills that can alter:

- healing amount;
- effective healing after overheal cap;
- MP or inventory attrition;
- revive reliability;
- automatic recovery reactions;
- sustain loops over repeated uses;
- item reliability versus Faith/MA/MP-based healing.

The immediate consumers are:

- Squire `First Aid`;
- Chemist `Potion` line, `Field Salve`, `Phoenix Down`, `Auto-Potion`, and `Item Lore`;
- Monk sustain/revive concepts;
- White Mage comparison rows.

## Source Notes

The T3.0 item rows use the Final Fantasy Tactics Battle Mechanics Guide by AeroStar for baseline
item values:

- Potion restores 30 HP;
- Hi-Potion restores 70 HP;
- X-Potion restores 150 HP;
- Phoenix Down revives with random low HP in vanilla.

Reference URL:

```text
https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
```

The T3.0 spell rows use the accepted Generic Chronicle v0.2 magic model from
`work/sim-inputs-v0.2.1.json`:

```text
K * MA * max(faith_factor_floor, (casterFaith / 100) * (targetFaith / 100))
faith_factor_floor = 0.60
```

This keeps healing and damage faith assumptions aligned until a later accepted document changes
them.

Per `docs/formula-balance/13-brave-faith-combat-policy-v0.md`, non-item healing expected to remain
combat-useful across the campaign must scale through a visible lever such as attributes, level
bands, max HP percentage, missing HP, equipment, or another accepted system variable. Item healing
is the exception because Potion/Hi-Potion/X-Potion progression already provides campaign scaling,
and Auto-Potion-style reactions consume that item progression rather than scaling through Faith.

## Pinned Bundle

Pinned input bundle:

```text
work/t3-healing-attrition-scenarios-v0.json
```

The bundle defines the formulas, scenario rows, resources, and expected values for the first
dual-independent T3 run.

## T3.0 Scope

T3.0 is action-normalized.

It answers:

- how much recovery does one action produce?
- how much of that recovery is wasted as overheal?
- how much inventory or MP is consumed?
- how much expected recovery does a reaction produce when trigger chance is included?
- how do finite resources cap repeated use?

T3.0 does not yet answer:

- CT-normalized healing throughput;
- cast-time interruption;
- cross-delivery reliability balance between items, spells, and reactions;
- movement/range opportunity cost;
- multi-unit area healing;
- enemy target selection;
- death-clock race conditions;
- status prevention versus cure.

Those require T5 or later encounter models. T3.0 still records `resolution_delay_ticks` as context so
T5 can consume the same rows later.

No concrete skill value may be accepted on cross-delivery reliability grounds, such as Chemist item
healing versus White Mage spell healing, until a T3xT5 composition exists. T3.0 validates recovery
amount and resource plumbing only.

## Formula Contract

All healing is capped by missing HP:

```text
effective_heal = min(raw_heal, missing_hp)
overheal = max(0, raw_heal - missing_hp)
```

For fixed item healing:

```text
raw_heal = item_power
expected_heal = effective_heal
```

For spell healing:

```text
faith_factor = max(faith_factor_floor, (caster_faith / 100) * (target_faith / 100))
raw_heal = floor(K * MA * faith_factor)
expected_heal = effective_heal
```

Magical revive and Raise-style recovery may require a more generous floor than ordinary magical
damage or status reliability. That is an anti-frustration tuning question for a later revive row,
not a reason to make all Faith-based magic ignore Faith.

For automatic reactions:

```text
effective_triggers = min(incoming_triggers, per_round_cap)
expected_heal = effective_heal * trigger_chance * effective_triggers
expected_resource_consumed = trigger_chance * effective_triggers * resource_cost
uses_resolved = effective_triggers
total_expected_heal = expected_heal
```

Active non-reaction repeated-use rows apply resource limits before multiplying by per-use healing:

```text
uses_resolved = min(planned_uses, floor(resource_available / resource_cost))
resource_consumed = resource_cost * uses_resolved
total_expected_heal = expected_heal_per_use * uses_resolved
```

The repeated-use formula above does not apply to reaction rows. Reaction `expected_heal` already
folds in the number of effective triggers.

`success_chance` is fixed at `1.0` in T3.0. Heal accuracy, whiff chance, silence, and similar
delivery reliability belong to a later T3xT4/T5 composition.

Numeric form:

- `raw_heal`, `effective_heal`, and `overheal` are integers;
- spell `raw_heal` is floored;
- `expected_heal`, `resource_consumed`, and `total_expected_heal` are expected-value floats;
- expected-value floats compare at six decimal places.

T3.0 uses deterministic expected values. It does not roll random revive HP, reaction triggers, or
Phoenix Down's vanilla random low revive HP. The deterministic `revive_hp` row is plumbing only, not
an accepted Phoenix Down value.

## Scenario Set

The first bundle includes arithmetic rows for:

- fixed Potion healing;
- overheal cap;
- X-Potion high item healing;
- v0.2 faith-floor spell healing;
- high-Faith spell healing above the floor;
- finite inventory;
- finite MP;
- Auto-Potion expected value;
- Auto-Potion per-round cap;
- Auto-Potion multi-trigger cap above one trigger;
- Phoenix Down revive low-HP reliability.

These rows prove the model plumbing. They do not yet balance final healing values.

## Expected Counter Output

GPT and Claude T3 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- `raw_heal`;
- `effective_heal`;
- `overheal`;
- `expected_heal`;
- `resource_consumed`;
- `uses_resolved`;
- `total_expected_heal`;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T3 output can be used to accept or reject skill values.

## What T3.0 Does Not Decide

Still open for later T3 versions:

- exact First Aid value;
- exact Potion-line redesign;
- exact Auto-Potion limiter;
- whether Item Lore changes amount, inventory efficiency, range, or reliability;
- whether Phoenix Down remains low-HP revive or changes;
- exact White Mage spell list and CT profile;
- whether attrition should become CT-normalized after T5 lands.

## Claude Review Request

Claude should review whether:

- action-normalized T3.0 is acceptable before T5 CT modeling;
- formula contract is specific enough for independent implementation;
- item, spell, resource, and reaction rows cover the right first cases;
- v0.2 faith floor is reused correctly;
- any additional row is required before implementing counters.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-20).
