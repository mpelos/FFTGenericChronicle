# Foundation Jobs Reconciliation V1

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/04-foundation-physical-jobs-proposal.md`
- `docs/job-balance/05-squire-chemist-v1-proposal.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This addendum reconciles the first foundation-job proposals with the later job-balance process.

Documents 04, 05, and 06 were accepted before the project had the full vanilla ability/status atlas
and before several validation tracks were named. Their core job identities remain accepted:

- Squire stays the starter physical utility job;
- Chemist stays the reliable item/gun specialist;
- Knight stays the durable armed-control frontline;
- Archer stays the endgame-capable bow/crossbow specialist.

This document does not add final numbers, JP costs, multipliers, prerequisites, hit rates, CT
values, item values, equipment records, or implementation data. It updates how those proposals
should be read before any concrete data pass.

## Reconciliation Rules

- Use the current atlas before turning any proposed skill into data.
- Treat old shorthand checks as legacy labels:
  - `M-SECONDARY-COUNT` and `M-EQUIP-UNLOCK` now route through T2/T2.1 incidence checks;
  - `I-ATTRITION` now routes through T3, and through T3xT5 when timing or revive races matter;
  - formula-drift concerns still route through F4/F5 as appropriate.
- Local ability names in `docs/reference/fft-vanilla-ability-effect-index.md` are the current
  baseline names. Earlier familiar labels are allowed as design prose, but final data should either
  use the current local names or explicitly document an intentional rename.
- New/reframed skills are allowed, but they must be labeled as new design roles rather than implied
  vanilla effects.

## Reference Pass

### Squire

Relevant vanilla records:

- `Focus`: `physical`, `stat_up`;
- `Rush`: `damage`, `physical`, `random`;
- `Throw Stone`: `damage`, `physical`, `random`;
- `Salve`: `status_clear`;
- likely borrowed/global rows: `Counter Tackle`, `JP Boost`, `Movement +1`.

Reconciliation notes:

- The `Dash` wording in earlier docs should be read as the current local `Rush` slot unless the
  final data pass intentionally renames it.
- `First Aid`, `Rally`, `Weapon Drill`, `Grit`, and `Basic Training` are new or reframed roles, not
  extracted vanilla mechanics.
- `JP Boost` remains a campaign/economy support unless a combat benchmark deliberately equips it.

### Chemist

Relevant vanilla records:

- `Potion`, `High Potion`, `X-Potion`: `healing`;
- `Ether`, `High Ether`: `mp`;
- `Elixir`: `healing`, `mp`;
- `Antidote`, `Eye Drops`, `Echo Herbs`, `Maiden's Kiss`, `Gold Needle`, `Holy Water`, `Remedy`:
  `status_clear`;
- `Phoenix Down`: `revive`, `random`;
- likely support/reaction/move rows: `Auto-Potion`, `Throw Items`, `Safeguard`, `Reequip`,
  `Treasure Hunter`.

Reconciliation notes:

- `Field Salve`, `Smoke Bomb`, and `Quick Draw` are new or reframed Chemist roles.
- Item reliability remains Chemist's identity, but any concrete item value must be tested against
  White Mage, Monk, Bard, and Samurai recovery options that were added later.
- `Move-Find Item` should be read as the current local `Treasure Hunter` movement row unless final
  data intentionally keeps a different display name.

### Knight

Relevant vanilla records:

- `Rend Helm`, `Rend Armor`, `Rend Shield`, `Rend Weapon`: `equipment_break`;
- `Rend MP`: `mp`, `stat_down`;
- `Rend Speed`: `timing`, `stat_down`;
- `Rend Power`: `physical`, `stat_down`;
- `Rend Magick`: `magical`, `stat_down`;
- likely support/reaction rows: `Equip Heavy Armor`, `Equip Shields`, `Parry`, `Safeguard`.

Reconciliation notes:

- The vanilla Rend fantasy remains the backbone, but permanent equipment deletion is still rejected
  as the default best tactic.
- `Challenge`, `Guarded Strike`, `Crushing Blow`, `Brace`, and `Shield March` are new or reframed
  roles and must not be treated as vanilla-proven mechanics.
- `Crushing Blow` must stay a guard/disruption concept unless F5 proves Knight can receive a real
  crush route without erasing Monk.

### Archer

Relevant vanilla records:

- `Aim +1`, `Aim +2`, `Aim +3`, `Aim +4`, `Aim +5`, `Aim +7`, `Aim +10`, `Aim +20`:
  `ct_action`, `damage`, `physical`;
- likely support/reaction/move rows: `Equip Crossbows`, `Concentration`, `Archer's Bane`,
  `Jump +1`.

Reconciliation notes:

- The old Aim ladder is not preserved as a pure numeric ladder. It becomes the source vocabulary
  for situational bow/crossbow shots.
- `Quick Shot`, `Aimed Shot`, `Pinning Shot`, `Piercing Shot`, `Covering Shot`, `High-Ground Shot`,
  `Arrow Guard`, `Speed Save`, and `Bow Mastery` are new or reframed roles.
- `Equip Bow` in earlier docs should be treated as a bow/crossbow equipment unlock concept; the
  local support row is currently `Equip Crossbows`.

## Updated Gate Bindings

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-SQ-REFERENCE` | Squire final data uses current local names or documents intentional renames. | atlas/data check |
| `J-SQ-SECONDARY` | Fundaments/Squire secondary incidence stays useful but not universal. | T2/T2.1 |
| `J-SQ-RECOVERY` | First Aid or Rally recovery does not replace Chemist, White Mage, Monk, or performer recovery. | T3/T3xT5/T2.1 |
| `J-SQ-DRILL` | Weapon Drill teaches weapon-family texture without becoming the default physical secondary. | T2/T2.1/F5 if formula-affecting |
| `J-CHM-ITEM` | Potion, Ether, Remedy, and Phoenix Down values preserve item reliability without erasing spell recovery. | T3/T3xT5/T9 |
| `J-CHM-AUTOPOTION` | Auto-Potion consumes inventory and avoids universal survival dominance. | T3/T3xT5/T2.1 |
| `J-CHM-FIELD` | Smoke Bomb or Field Salve creates utility without hidden evasion or sustain dominance. | T3/T4/T5 as applicable |
| `J-CHM-GUN` | Chemist gun utility stays distinct from Orator and does not become pure missile damage creep. | F4/T2.1/F5 after real weapon data |
| `J-KNT-REND` | Rend skills pressure equipment/offense without permanent-deletion dominance or casino turns. | T4/T6xT7 |
| `J-KNT-GUARD` | Parry, Shield Break, Guarded Strike, and Shield March do not create practical immunity. | T4/T6xPS/T2.1 |
| `J-KNT-EQUIP` | Equip Heavy Armor and Equip Shields remain deliberate build costs, not default patches. | T2/T2.1/F5 |
| `J-ARC-AIM` | Aimed shots create readable timing choices instead of a flat Aim ladder. | T5/T4 |
| `J-ARC-COVER` | Covering Shot or overwatch creates lane pressure without denying enemy movement. | T5/T8; T10 if extra actions are granted |
| `J-ARC-PIERCE` | Piercing Shot gives Archer anti-mail or line pressure without replacing all missile plans. | T6/F5/T2.1; T11 if the shot hits multiple units in a line |
| `J-ARC-ACCURACY` | Concentration and bow reliability stay bounded and do not become universal hit-rate fixes. | T4/T2.1 |
| `J-ARC-EQUIP` | Bow/crossbow unlocks do not make active Archer irrelevant. | T2/T2.1/F5 |
| `J-FND-RSM` | Counter Tackle, Parry, Auto-Potion, Safeguard, Concentration, Jump +1, and other foundation reaction/support/movement pieces stay bounded. | T2.1 |

These rows are requirements for later concrete data, not final scenario inputs.

## Open Proof Needs

- Exact Squire local-name normalization for `Rush`, `Salve`, and any retained classic display names.
- Whether Squire `Weapon Drill` is worth keeping once weapon formulas are final.
- Concrete item-value envelopes after White Mage, Monk, Bard, Samurai, and Chemist recovery options
  are compared together.
- Whether Chemist should own any gun utility beyond reliable ranged fallback.
- Whether Knight `Challenge` is target-AI behavior, zone pressure, CT pressure, or a different
  frontline-control mechanic.
- Whether Archer overwatch can exist without creating action-economy problems.
- Whether `Equip Crossbows` remains the local data record for the broader bow/crossbow unlock.

## Claude Review Request

Claude should review whether:

- this addendum preserves the accepted Squire/Chemist/Knight/Archer identities;
- the atlas reference pass is accurate enough for future concrete data work;
- legacy check labels were mapped to the right current gates;
- any scenario row is missing or over-scoped;
- this is sufficient to close the foundation reconciliation item before the Mime-replacement job.

Claude review verdict: Accepted after revision (claude-opus-4-8, 2026-06-21).

Claude accepted the addendum after one required edit and two cleanups:

- `J-ARC-PIERCE` now requires T11 if the shot hits multiple units in a line;
- `J-KNT-REND` now routes through T6xT7 for Rend-then-exploit interactions;
- `J-FND-RSM` explicitly binds foundation reaction/support/movement incidence to T2.1.
