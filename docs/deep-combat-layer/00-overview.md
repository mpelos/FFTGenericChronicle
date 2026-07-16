# Deep Combat Layer

The Deep Combat Layer (DCL) adapts the physical and supernatural combat structure of GURPS 4e to
the grid, turn economy, equipment roster, fixed ability lists, MP economy, and heroic progression of
Final Fantasy Tactics. It uses GURPS as a balance framework rather than as a promise to reproduce
every tabletop subsystem.

The shared combat pipeline has five layers, resolved in order:

1. attributes produce derived characteristics;
2. CT establishes initiative and a unit receives Movement plus Action;
3. an action tests its DX- or IQ-based skill;
4. delivery grants the target an active defense, resistance contest, or neither;
5. the effect applies DR, injury, healing, status, CT, or another authored result.

The document map below is the rules index. Scope exclusions, deliberate FFT adaptations, and
retired legacy fields are owned by
[Scope, Calibration, and Retired Fields](10-scope-calibration-and-retired-fields.md).

## Document map

| File | Owns |
| --- | --- |
| [01 — Attributes and Derived Stats](01-attributes-and-derived-stats.md) | ST, DX, IQ, HT, HP, MP, Will, Speed, Move, Jump, Base Dodge, Basic Lift, and legacy-stat ownership. |
| [02 — Turns, Movement, and Actions](02-turns-movement-and-actions.md) | Initial CT, linear CT growth, Movement, Action, Attack, Cast, Ready, Aim, and Stand Up. |
| [03 — Skills and Active Defenses](03-skills-and-active-defenses.md) | Difficulty, Rank, aptitude tiers, Job Level, Weapon Skill, Shield Skill, Dodge, Parry, Block, and criticals. |
| [04 — Facing, Reach, and Targeting](04-facing-reach-and-targeting.md) | Facing, Reach 1/2, target locations, physical cover/elevation/awareness, and the magic-targeting exception. |
| [05 — Damage, Armor, and Injury](05-damage-armor-and-injury.md) | Thrust/swing dice, weapon modifiers, shared DR/injury, armor divisors, wound multipliers, Shock, Major Wounds, and KO boundary. |
| [06 — Equipment and Encumbrance](06-equipment-and-encumbrance.md) | Physical and supernatural weapon, shield, body, head, and accessory schemas; Weight and Basic Lift bands. |
| [07 — Ranged Combat](07-ranged-combat.md) | Range penalties, Accuracy, Aim, trajectories, ranged defenses, and final hit chance. |
| [08 — Status Resistance and Posture](08-status-resistance-and-posture.md) | HT, Will, and Spiritual resistance ownership; Stun, Knockdown, Prone, and temporary action/movement loss. |
| [09 — Player-Facing Information](09-player-facing-information.md) | Required physical/magical status, equipment, targeting, timing, and forecast information. |
| [10 — Scope, Calibration, and Retired Fields](10-scope-calibration-and-retired-fields.md) | Excluded systems, configurable tables, and intentionally retired FFT fields. |
| [11 — Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md) | Source/delivery/effect separation, magical traditions, IQ-based training, MP, fixed costs, and overcasting. |
| [12 — Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md) | Cast declaration, movement order, CastCT, concentration, unit tracking, tile targeting, range, and no-LoS magic. |
| [13 — Magic Resolution and Defenses](13-magic-resolution-and-defenses.md) | External/internal delivery, active defenses, resistance, Magic Resistance, damage, DR, Faith, elements, Shell, and Reflect. |
| [14 — Magic Effects and Persistence](14-magic-effects-and-persistence.md) | Healing, revive, status duration, global ticks, Haste, Slow, Quick, Stop, Silence, Dispel, stacking, Undead, and Summon. |
| [15 — Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md) | Shared growth profiles, deterministic level-up, Brave growth, Faith permanence, additive job chassis, and the job-authoring recipe. |

Each rule has one owning file. Other documents link to the owner instead of restating the rule.

The external mechanics behind GURPS maneuvers, techniques, cinematic skills, perks, advantages,
Imbuements, powers, templates, lenses, and power-ups are cataloged separately in
[GURPS Heroic Combat Abilities and Tradeoffs](../reference/gurps-heroic-combat-abilities-and-tradeoffs.md).
That reference explains source-system tradeoffs but does not add rules to the DCL.
