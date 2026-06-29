# DCL runtime feasibility analysis

This file is a checkpoint for the question: do we already have enough runtime control to implement
the Deep Combat Layer (DCL) formulas, or are there still missing engine hooks/context channels?

## Short answer

We have enough to express and execute custom numeric formulas for damage, healing, MP, and preview
numbers. We do not yet have enough to implement the full DCL combat loop as designed.

The main blocker is not math. The code mod can already run arbitrary C# formula expressions with
attacker/target stats, tables, maps, matrices, equipment slots, and action-context variables. The
missing pieces are mostly context and authority:

- knowing the exact action/ability/weapon/spell being resolved in every case;
- authoring hit/miss/parry/block/magic-evade outcomes per action, not through static probes;
- tracking DCL state such as depleted defenses;
- exposing/enriching geometry, item metadata, elements, statuses, and ability metadata;
- making preview, execution, and AI agree.

For DCL implementation, the architecture should be treated as feasible in layers, not complete.

## What the DCL requires

### Physical damage

DCL physical damage needs the runtime to compute:

```text
injury =
  max(pen_floor, max(0, base_damage - DR_type))
  * wound_mult
  * trait_mult
  * zodiac_mult
  * global_scale
```

Inputs required:

- attacker PA or derived thrust/swing value;
- weapon family and weapon damage mode;
- weapon modifier and optional over-cap skill contribution;
- target armor class and damage-type DR;
- wound type multiplier: cut, thrust, crush, missile, etc.;
- Brave-based offense/defense traits;
- zodiac compatibility;
- target/attacker identity.

Runtime status:

- Numeric math is feasible now.
- Attacker/target stats are mostly available now.
- Equipment slots are available now.
- Item metadata is partially available now.
- True weapon/action identity is not complete enough for all cases.
- Armor DR can be implemented as formula tables/maps once item metadata is enriched.

### Magic and healing

DCL magic damage needs:

```text
dmg = base(MA) * spell_power * faith_mult * element_mult * zodiac_mult * global_magic_scale
```

DCL healing needs:

```text
heal = base(MA) * heal_power * faith_caster * faith_target * zodiac_mult * global_magic_scale
```

Inputs required:

- caster MA;
- spell identity and spell power;
- caster Faith and target Faith;
- element;
- target elemental affinity/resistance/absorb/nullify/halve/weak/strong;
- zodiac compatibility;
- Shell/status modifiers if kept;
- target list for AoE;
- healing vs damage classification.

Runtime status:

- Damage and healing execution rewrites are proven.
- Delayed damage and delayed explicit healing can be resolved through pending cache / credit cache.
- Preview damage/heal numbers can be controlled.
- Formula execution can differ from vanilla and match UI when configured.
- Spell identity, element identity, spell power tables, and magic evade are not complete.

### Hit and defense model

DCL wants two independent 3d6 decisions:

1. attacker hit roll against weapon skill;
2. defender active defense roll against best available defense.

DCL also wants:

- crit bypasses defense;
- fumble misses;
- Dodge never depletes;
- Parry and Block deplete per attack;
- depleted defenses reset on defender's own turn;
- facing modifies defense;
- multi-hit consumes defenses per hit.

Runtime status:

- Preview hit percent can be written.
- Physical hit/miss/parry/block style control has proof paths:
  - result selector hook;
  - pre-clamp zero debit;
  - live evade-byte manipulation.
- Brave reaction gating through stat puppeteering is proven for some reactions.
- Shipping-grade per-action outcome authority is not done.
- Magic evade / spell avoidance remains an open RE target.
- Native miss-before-damage is a structural risk: if native logic misses before HP debit, the HP
  hooks never see an event. DCL likely needs either force-native-hit upstream then downgrade output,
  or a reliable pre-roll/per-action result hook.

### Facing, position, reach

DCL needs:

- attacker position;
- target position;
- target facing;
- distance and reach;
- front/side/back defense modifiers;
- reach-2 outrange and point-blank behavior;
- counter/reaction context.

Runtime status:

- `docs/modding/05-reverse-engineering.md` records `+0x4F/+0x50/+0x51` as X/Y/Dir, but this is not
  yet treated as a fully integrated formula surface.
- The runtime currently does not expose X/Y/Dir in `AddUnitVariables`.
- Height/tile data is not yet a reliable formula input.
- Reach can be modeled from item metadata once range/weapon-family fields are exposed.
- Reaction/counter handling still needs robust action context.

### Weapon skill

DCL needs weapon skill by weapon family:

```text
skill = base[grade] + rate[grade] * (J * (jobLevel - 1) + K * (jobLevel / 8) * (charLevel - 1))
```

Then:

- skill is capped for hit chance;
- over-cap can become damage or penetration;
- crossbows/guns may use skill directly as damage/penetration input.

Runtime status:

- Job, level, job-growth/mult fields, and equipment are exposed.
- Weapon family can be approximated from catalog category fields.
- Job level / ability level data may need verification and exposure.
- Grade matrix and weapon-skill curves can be implemented as formula maps/matrices.

### Statuses and reactions

DCL wants status contests by category:

- mental/will using Brave;
- taunt/caution inverted by low Brave;
- body/physical using HP or related toughness;
- magic/spiritual using inverted Faith or caster MA;
- reactions using courage/caution/neutral curves.

Runtime status:

- Status bits can be written, with at least Undead proven.
- Brave can suppress some reactions by live stat writes.
- Status bit map and native status lifecycle are not complete.
- Replacing native status probability with DCL contests is not complete.
- Reaction ownership/source and Hamedo/First Strike edge cases need more action-context work.

### Equipment and item metadata

DCL needs every equipment piece to carry:

- armor class;
- DR by damage type;
- Weight;
- shield Block;
- weapon family;
- damage type;
- reach;
- parry;
- element;
- special behavior.

Runtime status:

- Live equipment slots are known:
  - head `+0x1A`;
  - body `+0x1C`;
  - accessory `+0x1E`;
  - right-hand weapon `+0x20`;
  - right-hand shield `+0x22`;
  - left-hand weapon `+0x24`;
  - left-hand shield `+0x26`.
- `ItemCatalog` exposes basic numeric fields and category booleans.
- `work/item_catalog.csv` already contains richer fields such as range, attack flags, elements,
  option ability id, status flags, and elemental affinities, but `ItemCatalogEntry` does not expose
  all of them yet.
- DCL should enrich this catalog before relying on formula profiles.

## Capability matrix

### Available/proven enough for DCL numeric formulas

- Arbitrary integer formula expressions in the code mod.
- Formula tables, maps, and matrices.
- Attacker and target stats:
  - PA, MA, Speed, CT, Move, Jump, Brave, Faith, HP, MP, level, job, zodiac, gender.
- Raw and effective PA/MA/Speed.
- Equipment slot reads.
- Basic item catalog metadata.
- Runtime damage rewrite, including same-hit KO through native pre-clamp.
- Runtime healing rewrite, including delayed explicit heals through credit cache.
- Runtime MP rewrite/control.
- Full preview number/bar control for damage and healing.
- Preview hit-percent control.
- Pending-action tracker for charged/delayed actions and AoE damage targets.
- Physical output-control experiments for hit/miss/block/parry.

### Available but needs productization

- Actor/context resolver:
  - works well for charged/delayed action tests;
  - immediate actions and reactions still need hardening.
- Action identity:
  - pending action fields and cache scores help;
  - true ability id / spell id is not complete.
- Result authority:
  - proof hooks exist;
  - need per-action arming and deterministic DCL roll integration.
- Defense depletion:
  - feasible as code-mod state;
  - not implemented as DCL state machine.
- Forecast/execution sync:
  - damage/heal is proven;
  - hit/defense and status forecast still need integration.

### Missing or high-risk for full DCL

- Perfect ability/spell/action id capture for every action type.
- Magic evade control.
- Per-action roll/outcome authority before native logic can discard an event.
- Facing/position/reach exposed as stable formula variables.
- Tile/height data.
- Status contest replacement and status duration/source handling.
- AI scoring for rewritten formulas.
- A complete item/ability metadata layer for DCL concepts.

## Recommended implementation layers

### Layer 1: Numeric DCL damage/heal without DCL hit/defense

Goal:

- prove the DCL formula model can produce physical damage, magic damage, and healing using runtime
  stats and enriched equipment/item data.

Work:

- add DCL formula profile with thrust/swing tables;
- add DR maps by armor class and damage type;
- add Brave/Faith/Zodiac curves;
- expose more item catalog fields;
- expose X/Y/Dir only after confirming confidence;
- keep native hit/avoidance for this layer.

Why first:

- this validates the formula surface without mixing in roll-control complexity.

### Layer 2: DCL preview parity

Goal:

- preview shows the same DCL number that execution will apply.

Work:

- drive preview damage/heal from the same formula profile;
- ensure delayed actions use pending context or cached context;
- show correct damage/heal bars for physical, magic, and healing.

Why second:

- DCL design depends on deterministic preview/result parity.

### Layer 3: DCL physical hit/defense authority

Goal:

- replace native physical hit/defense with DCL 3d6 hit + active defense.

Work:

- implement formula-driven roll result;
- force or preserve native hit path safely;
- downgrade to miss/block/parry through result selector / zero debit;
- implement Parry/Block depletion and turn reset;
- add preview hit percent from DCL math.

Why third:

- this is the first layer where native mechanics can prevent HP events, so it requires careful
  hook timing.

### Layer 4: Magic evade/status/reactions

Goal:

- replace magic avoidance, status odds, and reaction behavior with DCL rules.

Work:

- find magic avoidance control path;
- map status bits and lifecycle;
- implement status contest formulas;
- implement reaction eligibility/outcome policy;
- test Hamedo/First Strike/counter/multi-hit/AoE edge cases.

Why fourth:

- this is the most edge-case-heavy part of the DCL.

## Current verdict

The code mod is powerful enough to be the DCL formula engine. It is not yet complete as the DCL
combat engine.

The practical next step is to stop asking "can the DSL do the math?" and start building the missing
runtime substrate:

1. enriched item/action metadata;
2. robust action identity;
3. per-action outcome authority;
4. DCL state tracking for depleted defenses and similar combat resources.

Once those exist, the DCL formulas themselves are straightforward to encode.
