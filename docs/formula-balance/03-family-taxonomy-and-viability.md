# Weapon Family Taxonomy And Formula Viability

Status: Accepted (conceptual guidance)
Date: 2026-06-20
Depends on:
- `docs/formula-balance/00-envelope.md`
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/modding/02-formula-id-catalog.md`
Review: Approved by Claude on 2026-06-20 as conceptual guidance without baseline CSV, provided
weapon-data facts remain labeled as needing capture/proof.

## Purpose

This document connects weapon-family identity to formula feasibility. In FFT, a weapon family's
identity is partly design fantasy and partly hardcoded formula behavior, so taxonomy and
technical viability must be reviewed together.

This document does not set final numeric formulas. It defines candidate identities, likely
Tier-1 expressions, and likely Tier-2 dependencies.

## Palette Discipline

This document is constrained by `02-variable-palette.md`. Each family must declare:

- variables to emphasize as visible identity;
- variables to use only as secondary tempering;
- variables to avoid unless a later approved design justifies the readability cost.

This is not a restriction on creativity. It is a readability guard: every family can be ambitious,
but the player should still understand why the weapon feels different.

## R1 Gate: Is Family-To-Routine Routing Reassignable?

The first unresolved question is whether Tier 1 can freely reassign a weapon family to a
different base weapon routine.

Questions to prove on the Windows game machine:

1. Does `ItemWeaponData.xml` expose a weapon formula field that directly selects the base weapon
   calculation routine? Current answer: yes, `work/battle_data_inventory.md` confirms
   `ItemWeaponData.xml` has a per-weapon `Formula` field.
2. If edited, does that field actually change the computation used in-game?
3. Does the behavior carry cleanly, or are parts of the weapon behavior still hardcoded to
   weapon type, slot, animation, command, or side logic?

Current decision state:

```text
Decision: R1(a) confirmed in local files; R1(b) still unproven
Dependency: Tier-1 if editable and behavior carries; Tier-2 or constrained Tier-1 if not
Confidence: medium
Proof state: Formula field verified in local file inventory; edit/carry behavior needs in-game proof patch
```

Related local-file confirmation: `ItemWeaponData.xml` also exposes `AttackFlags`, and the current
inventory confirms flags such as `Direct`, `TwoHands`, `TwoSwords`, and `ForcedTwoHands`. These
are real data levers in schema, but their exact battle behavior still needs proof/playtest.

Why this governs the whole design:

- If routing is freely editable and behavior carries, Tier 1 can reshape family identities by
  reassigning weapon routines plus tuning WP, range, element, flags, and related data.
- If routing is not freely editable, Tier 1 identities are mostly limited to the routine already
  attached to each family, plus data tuning around it.
- If routing partially carries, each family needs a specific proof note before relying on it.

Families that keep their native routine are not blocked by R1. They still need the weapon
baseline and playtest proof, but they do not need routine reassignment to exist.

Families that need a different routine from their native cluster are gated by R1. If R1 fails,
those families must either differentiate through WP, range, elements, flags, skillsets, support
abilities, accuracy/evasion, and equipment context, or explicitly move their ideal identity to
Tier 2.

## Routine Cluster Constraint

Families that share the same routine share the same broad scaling shape. Changing WP changes
magnitude, not identity.

Current working clusters from the formula catalog:

- `PA * WP`: sword, spear/polearm, crossbow, rod in the current notes.
- `[(PA + Speed) / 2] * WP`: knife/ninja blade, longbow.
- `[(PA * Brave) / 100] * WP`: knight sword, katana.
- `MA * WP`: staff, pole.
- `Rdm(1..PA) * WP`: axe, flail, bag.
- `WP * WP`: gun.
- `[(PA * Brave) / 100] * PA`: bare hands / martial.
- `[(PA + MA) / 2] * WP`: instrument, dictionary, cloth.

Strategic consequence: without R1, families inside the same cluster need non-routine identity
levers. Ability design, range, accuracy/evasion flags, elements, support abilities, equipment
access, and job context become primary tools for separating them.

## Ability And Skillset Identity

Ability formulas are a primary Tier-1 design surface and should be used alongside base weapon
routines.

For weapon families that share a base routine, family-flavored abilities may carry more identity
than the basic Attack command. The relevant levers are:

- ability formula id;
- `X` and `Y`;
- element;
- status;
- range, vertical, AoE;
- CT and MP;
- ability flags and AI behavior;
- skillset membership and job access.

This keeps design ambitious without making every family dependent on R1 or Tier 2.

## Transversal Dominance Risks

Every family must be checked against support and equipment multipliers:

- Two Hands can turn a balanced high-WP family into the dominant family.
- Dual Wield / Two Swords can turn on-hit or high-efficiency weapons into the dominant family.
- Attack Boost, Martial Arts, Defense Boost, shields, weapon evade, and accessory bonuses can
  change the real value of a family more than its base formula.

The taxonomy below names these as tempering variables even when the family identity is primarily
about damage shape.

## Baseline Source Caveat

The current family routines below come from the working catalog in
`docs/modding/02-formula-id-catalog.md`, not from a fresh committed `ItemWeaponData.xml` CSV.

Required next factual artifact:

```text
work/baseline_weapons.csv
```

Minimum columns:

- weapon id
- weapon name, if available
- family / weapon type
- weapon formula id
- power / WP
- range
- evasion
- element
- attack flags
- option ability id

Until that exists, family taxonomy can move conceptually, but no specific weapon values are
verified in this checkout.

## Candidate Taxonomy

### Sword

Current routine family: `PA * WP` style weapon damage.

Candidate identity: reliable baseline melee. Swords should remain familiar and generally useful
without being the universal best physical answer.

Palette usage:

- Emphasize: PA, WP, weapon routine, job access, skillset access.
- Temper: Two Hands, Attack Boost, shield compatibility, accuracy/evasion flags.
- Avoid: custom opacity such as hidden target-defense math unless a later Tier-2 design requires
  it.

Tier-1 expression:

- Tune WP and availability around sword being stable rather than dominant.
- Preserve simple PA scaling as the readable baseline.
- Use skill and job context to prevent sword access from overwhelming other families.

Tier-2 dependency:

- None required for the baseline identity.
- Only needed if sword needs custom diminishing returns or matchup-specific behavior.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Knight Sword

Current routine family: Brave-scaled weapon damage, `[(PA * Br) / 100] * WP`.

Candidate identity: premium commitment weapon. Higher ceiling than normal swords, but tied to
Brave, job access, or other opportunity cost.

Palette usage:

- Emphasize: PA, Brave, WP, job access, support ability opportunity cost.
- Temper: Two Hands, Attack Boost, shield tradeoff, availability.
- Avoid: making Brave the only check if the result becomes a universal best weapon path.

Tier-1 expression:

- Preserve Brave dependency as the main differentiator.
- Use access, WP, and job restrictions to prevent universal dominance.

Tier-2 dependency:

- Only needed if Brave scaling is not enough to create a distinct endgame tradeoff.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Katana

Current routine family: Brave-scaled weapon damage, `[(PA * Br) / 100] * WP`.

Candidate identity: high-ceiling technique weapon, adjacent to knight swords but less generically
stable. Katanas can lean into Brave, class synergy, and risk/reward.

Palette usage:

- Emphasize: PA, Brave, WP, skillset interaction, job identity.
- Temper: breakability/availability if relevant, Two Hands, ability CT/range/status.
- Avoid: duplicating knight sword identity with only different item names.

Tier-1 expression:

- Brave scaling already gives a distinct axis.
- Tune WP and availability separately from knight swords.
- Preserve any existing skillset interactions as part of the family identity.

Tier-2 dependency:

- Needed only for custom risk mechanics or nonstandard critical behavior.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Knife / Ninja Blade

Current routine family: `[(PA + Speed) / 2] * WP`.

Candidate identity: fast, precise, stat-hybrid physical weapon. Should reward agile units rather
than competing with swords through raw WP alone.

Palette usage:

- Emphasize: Speed, PA, accuracy/reliability, Dual Wield context, ability utility.
- Temper: WP, on-hit effects, evasion bypass flags, job access.
- Avoid: letting Dual Wield turn knives into the new universal damage answer.

Tier-1 expression:

- Existing PA+Speed routine is a strong identity anchor.
- Tune WP and access so Speed investment matters.
- Preserve dual-wield or ninja-style synergies only if they do not become the new universal
  answer.

Tier-2 dependency:

- Needed for deeper precision, armor-gap, or opportunistic-hit models.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Spear / Polearm

Current routine family: `PA * WP` style weapon damage, with spear-specific behavior likely
outside the formula itself.

Candidate identity: directed-force weapon. Spears should feel distinct from swords through
confirmed range/vertical levers, positioning, jump/polearm context, or matchup value rather than
just a different WP curve.

Palette usage:

- Emphasize: range if confirmed, vertical, PA, jump/polearm skill context, accuracy/reliability.
- Temper: WP, shield compatibility, CT on spear skills, job access.
- Avoid: assuming armor penetration or reach exists before data/proof confirms it.

Tier-1 expression:

- Use range, vertical tolerance, flags, WP, and job access.
- If R1 proves routing is editable, consider whether a non-sword routine better supports spear
  identity.

Tier-2 dependency:

- True penetration, armor-gap, facing-specific, or charge-through mechanics likely require Tier
  2 unless mapped to an existing routine.

Confidence: low
Dependency: Tier-1 conditional on R1 for routine reassignment; otherwise constrained Tier-1 or Tier-2
Proof state: needs data capture on game machine + playtest

### Axe / Flail

Current routine family: random physical damage, `Rdm(1..PA) * WP`.

Candidate identity: volatile impact weapon. Axes and flails should offer a strong ceiling or
special matchup value without becoming mathematically unreliable trash.

Palette usage:

- Emphasize: variance/randomness, WP, high ceiling, support ability interaction.
- Temper: accuracy, minimum practical output, Two Hands, job access.
- Avoid: pure coin-flip design where the player cannot plan around the family.

Tier-1 expression:

- Existing random routine creates identity, but may need WP/support tuning to be desirable.
- Keep volatility legible: high risk should buy a meaningful reward.

Tier-2 dependency:

- Needed if the existing random routine cannot be tuned into a satisfying risk/reward profile.
- Needed for armor-crushing or minimum-damage-floor models.

Confidence: medium
Dependency: Mixed
Proof state: needs weapon baseline + playtest

### Longbow

Current routine family: `[(PA + Speed) / 2] * WP`.

Candidate identity: positional ranged weapon. Longbows should reward reach, height, and unit
planning more than raw melee efficiency.

Palette usage:

- Emphasize: range, height if confirmed, Speed, PA, positioning, accuracy/reliability.
- Temper: CT/charge if used, weather/terrain only as background, job access.
- Avoid: making terrain/weather fine print the primary identity.

Tier-1 expression:

- Existing PA+Speed routine supports agile ranged identity.
- Use range, line-of-sight behavior, height interactions, WP, and job access as primary levers.

Tier-2 dependency:

- Needed for custom distance falloff, armor interaction, or height-scaling beyond existing game
  behavior.

Confidence: medium
Dependency: Tier-1 with possible Tier-2 extensions
Proof state: needs weapon baseline + playtest

### Crossbow

Current routine family: `PA * WP` style weapon damage.

Candidate identity: compact ranged force. Crossbows should differ from longbows by reliability,
range profile, job access, or armor/matchup role.

Palette usage:

- Emphasize: PA, reliability, range profile, accuracy/evasion flags, job access.
- Temper: WP, shield/offhand compatibility if relevant, ability utility.
- Avoid: assuming armor punch exists without Tier-1 proof or Tier-2 commitment.

Tier-1 expression:

- Use range, WP, attack flags, and job access.
- If R1 proves routing is editable, test whether a different routine gives crossbows a clearer
  identity.

Tier-2 dependency:

- True armor punch or reload-like tradeoffs likely require Tier 2.

Confidence: low
Dependency: Tier-1 conditional on R1 for routine reassignment; otherwise constrained Tier-1 or Tier-2
Proof state: needs data capture on game machine + playtest

### Staff

Current routine family: `MA * WP`.

Candidate identity: caster weapon that makes magical stats matter in basic attacks without
competing directly with spells.

Palette usage:

- Emphasize: MA, WP, caster job access, magic-adjacent ability utility.
- Temper: Faith, MP economy, status/element options.
- Avoid: making staff attacks replace spellcasting as the best caster action.

Tier-1 expression:

- Existing MA*WP routine gives a strong identity anchor.
- Tune WP and magic-user access so staff attacks are useful, not primary spell replacements.

Tier-2 dependency:

- Needed for deeper spell-channeling or MP-sensitive attack models.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Rod

Current routine family: listed with `PA * WP` style weapon damage in the current catalog notes.

Candidate identity: low-commitment caster sidearm or elemental conduit, depending on actual IVC
weapon data.

Palette usage:

- Emphasize: element, option abilities, caster access, potential MA interaction if proven.
- Temper: WP, accuracy/reliability, MP/status synergy.
- Avoid: leaving rods as weak PA*WP sidearms if no other identity lever exists.

Tier-1 expression:

- Use element, option abilities, WP, and job access.
- If the current routine is truly PA*WP, rods may need non-formula data hooks or reassignment to
  avoid feeling like weak physical weapons.

Tier-2 dependency:

- Needed for custom MA hybrid or spell-amplification behavior if Tier 1 cannot express it.

Confidence: low
Dependency: Tier-1 conditional on R1 or usable option/element data; otherwise Tier-2 likely
Proof state: needs data capture on game machine

### Pole

Current routine family: `MA * WP`.

Candidate identity: magical reach weapon. Poles can be a more aggressive caster physical option
than staffs if range and MA scaling are supported.

Palette usage:

- Emphasize: MA, WP, range if confirmed, caster positioning.
- Temper: Faith, job access, vertical/range behavior.
- Avoid: duplicating staff identity without a positioning or reach distinction.

Tier-1 expression:

- Existing MA*WP routine is a strong identity anchor.
- Tune WP, range, and job access around caster melee/ranged positioning.

Tier-2 dependency:

- Needed only for custom hybrid or status-channel behavior.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Bare Hands / Martial

Current routine family: Brave-scaled PA self-scaling, `[(PA * Br) / 100] * PA`.

Candidate identity: stat-built weaponless combat. Should be strong when a unit invests in the
right job/stat/status context, without invalidating actual weapons.

Palette usage:

- Emphasize: PA, Brave, Martial Arts, job identity, support ability context.
- Temper: equipment opportunity cost, status, survivability.
- Avoid: making unarmed combat the universal best option because it ignores weapon progression.

Tier-1 expression:

- Existing formula already creates a distinct non-WP identity.
- Tune job access, support ability assumptions, and competing weapon options.

Tier-2 dependency:

- Needed for more complex unarmed combo, stance, or matchup mechanics.

Confidence: medium
Dependency: Tier-1
Proof state: needs playtest

### Gun

Current routine family: `WP * WP`.

Candidate identity: stat-independent ranged weapon. Guns should offer reliability and accessibility
without erasing stat-built weapon families.

Palette usage:

- Emphasize: WP, range, reliability, stat independence, accuracy/evasion behavior.
- Temper: availability, special gun types, support interactions.
- Avoid: letting fixed damage erase PA/MA/Speed-based progression.

Tier-1 expression:

- Existing WP*WP routine gives a unique fixed-damage identity.
- Tune WP, range, availability, and special gun types carefully.

Tier-2 dependency:

- Needed for custom ammo, armor, or range-falloff behavior.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Instrument / Dictionary / Cloth

Current routine family: `[(PA + MA) / 2] * WP`.

Candidate identity: hybrid utility weapons. These should reward unusual builds and support jobs
without needing to beat dedicated melee families at their own role.

Palette usage:

- Emphasize: PA+MA hybrid scaling, range/utility if present, support job identity.
- Temper: WP, status/element/option effects, accuracy.
- Avoid: requiring obscure raw stats or hidden conditions to understand the family.

Tier-1 expression:

- Existing PA+MA routine creates a hybrid axis.
- Tune WP, range, access, and any option effects around utility and build diversity.

Tier-2 dependency:

- Needed for deeper support scaling or role-specific effects.

Confidence: medium
Dependency: Tier-1
Proof state: needs weapon baseline + playtest

### Bag

Current routine family: random physical damage, grouped with flail/axe/bag in current notes.

Candidate identity: oddball volatility or utility weapon. It should be allowed to remain unusual,
but not become accidental dead equipment.

Palette usage:

- Emphasize: volatility if confirmed, utility, unusual access, option effects.
- Temper: WP, accuracy, support ability interactions.
- Avoid: novelty-only design with no credible endgame niche.

Tier-1 expression:

- Use the random routine as identity if confirmed.
- Tune WP and availability around novelty plus viable niche use.

Tier-2 dependency:

- Needed for custom chaos/utility effects beyond the existing random routine.

Confidence: low
Dependency: Mixed
Proof state: needs data capture on game machine + playtest

## Axes That Are Not Proven Tier-1 Levers

The following ideas are attractive but not yet proven as data-only levers:

- point-by-point armor DR;
- armor penetration;
- custom thrust/swing tables;
- distance falloff;
- custom critical curves;
- new damage types beyond FFT's existing element/status vocabulary;
- formulas that combine arbitrary amounts of PA, MA, Speed, Brave, Faith, level, HP, MP, and
  target defense.

These ideas should be written as Tier-2 candidates unless they can be mapped to existing formula
routines or data fields.

## Initial Family Viability Read

Likely Tier-1 anchors:

- sword as stable PA baseline;
- knight sword and katana as Brave-scaling premium weapons;
- knife/ninja blade and longbow as PA+Speed families;
- staff and pole as MA*WP families;
- gun as stat-independent WP*WP family;
- martial as Brave/PA self-scaling;
- instrument/dictionary/cloth as PA+MA hybrids.

Likely mixed or weak Tier-1 cases until proven:

- spear, if its identity depends on penetration rather than reach/range;
- crossbow, if it cannot differentiate from PA*WP ranged sword-like damage;
- axe/flail/bag, if random damage cannot be tuned into desirable risk/reward;
- rod, if its actual IVC behavior remains PA*WP and lacks enough elemental/option support.

## Next Required Evidence

Before this document can become accepted guidance, the project needs either:

1. a committed weapon baseline CSV from `ItemWeaponData.xml`, or
2. explicit Claude approval to keep the taxonomy accepted only as conceptual guidance while
   marking all weapon-data facts as `needs data capture on game machine`.

The preferred path is the CSV baseline from the Windows game machine.
