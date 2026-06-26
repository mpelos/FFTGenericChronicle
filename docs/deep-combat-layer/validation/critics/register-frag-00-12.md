# Register fragment — docs 00 (overview/pillars) + 12 (open questions, resolved items)
# (mechanism-only statements; rationale captured separately)

## L0 PILLARS (doc 00, "Guiding principles")

### P1
- **statement:** No option (weapon type, trait, build) is strictly better than another; every advantage on one axis is offset by a cost on another axis. The roster is situational, not rank-ordered.
- **layer:** L0
- **rationale:** Contextual differentiation is the core philosophy; "bring the right tool" must be a real decision. It is why GURPS (rich, pre-balanced formulas) is adapted at all.
- **alternatives_rejected:** power-ranked roster
- **dependencies:** all weapon/equipment/trait decisions
- **source:** 00 §Guiding principles
- **status:** decided

### P2
- **statement:** Every existing FFT attribute either drives a mechanic or is explicitly removed/reskinned; none is dead weight.
- **layer:** L0
- **rationale:** Hard constraint; nothing in the character menu may be inert. Audit lives in 01.
- **alternatives_rejected:** leaving vanilla attributes vestigial
- **dependencies:** 01-attribute-map
- **source:** 00 §Guiding principles
- **status:** decided

### P3
- **statement:** A confirmed hit's damage is computed deterministically; preview equals result. RNG is confined to the hit roll and the active-defense roll.
- **layer:** L0
- **rationale:** Legible damage; what the preview shows is what happens.
- **alternatives_rejected:** rolled/variable damage
- **dependencies:** 02-damage-model, 04-hit-and-defense
- **source:** 00 §Guiding principles
- **status:** decided

### P4
- **statement:** Brave, Faith, and Zodiac are each a permanent per-unit slider with a real upside and a real downside; no setting is universally best.
- **layer:** L0
- **rationale:** Two-sided permanent traits.
- **alternatives_rejected:** one-directional "more is better" stats
- **dependencies:** 07, 08, 09
- **source:** 00 §Guiding principles
- **status:** decided

### P5
- **statement:** Combat math is transparent/readable; hidden multipliers and opaque stacking are replaced by surfaced systems.
- **layer:** L0
- **rationale:** Legibility over hidden math; replaces vanilla's hidden Zodiac multiplier and opaque evade.
- **alternatives_rejected:** vanilla hidden math
- **dependencies:** all systems; presentation (item 6)
- **source:** 00 §Guiding principles
- **status:** decided

### P6
- **statement:** No new items are added; existing equipment is recharacterized via type, reach, and modifier only.
- **layer:** L0
- **rationale:** Project-wide rule.
- **alternatives_rejected:** new SKUs/weapons
- **dependencies:** 14-equipment
- **source:** 00 §Guiding principles
- **status:** decided

### P7 (candidate pillar — feel)
- **statement:** A unit performs at full effectiveness until 0 HP; there is no death-spiral and no HP-threshold penalty. At 0 HP, vanilla death/countdown applies.
- **layer:** L0/L1 (candidate)
- **rationale:** FFT is heroic; persistent HP-debuff would punish melee more than ranged.
- **alternatives_rejected:** GURPS death-spiral / shock penalties / major-wound stun as automatic; persistent ≤1/3-HP penalty
- **dependencies:** HP, Brave composure, interruption, status system
- **source:** 00 §Status; 12 item 1
- **status:** decided

### P8 (candidate pillar — stance)
- **statement:** GURPS formulas are adopted as a starting point but overridden wherever FFT balance/feel demands; GURPS fidelity is not itself a goal.
- **layer:** L0 (candidate)
- **rationale:** Borrow decades of pre-balanced math; bend to FFT.
- **alternatives_rejected:** GURPS-faithful port
- **dependencies:** all
- **source:** 00 §Why GURPS; user directive "não precisamos ser GURPS fiel"
- **status:** decided

## SCOPE / ARCHITECTURE (doc 00)

### D00-1
- **statement:** The DCL is a separate track from the v0.2 formula-balance work and does not inherit its decisions.
- **layer:** L0 (scope)
- **rationale:** "another perspective" on combat, kept side by side.
- **alternatives_rejected:** evolving v0.2
- **dependencies:** none
- **source:** 00 §What this is
- **status:** decided

### D00-2
- **statement:** Damage mitigation uses subtractive damage resistance (DR), not a multiplicative C-bounded model.
- **layer:** L2
- **rationale:** intentional disagreement with v0.2's multiplicative-no-DR policy.
- **alternatives_rejected:** multiplicative/C-bounded (the v0.2 model)
- **dependencies:** 02, 03, armor model
- **source:** 00 §What this is
- **status:** decided

### D00-3
- **statement:** GURPS magnitudes are re-ranged/bridged to FFT's HP and PA scales rather than used as raw thrust/swing values.
- **layer:** L2
- **rationale:** fit FFT number scale.
- **alternatives_rejected:** literal GURPS number scale
- **dependencies:** 02 (G bridge constant)
- **source:** 00 §Why GURPS
- **status:** decided

### D00-4
- **statement:** GURPS body-type / injury-tolerance distinctions are not imported.
- **layer:** L2 (scope)
- **rationale:** cut by Marcelo.
- **alternatives_rejected:** importing injury tolerance
- **dependencies:** 03
- **source:** 00 §Why GURPS
- **status:** decided

## RESOLVED-IN-12 (mechanics)

### D12-1
- **statement:** There is no automatic damage-triggered reeling/stun; large single hits do not trigger a hidden resist-or-reel check. Knockdown/stun/fear arrive only from explicit job skills and weapon properties.
- **layer:** L3
- **rationale:** a hidden universal "big hit → stun" rule is illegible (unit stunned with no clear cause); clashes with legibility (P5).
- **alternatives_rejected:** automatic "single hit > ½ HP → hidden resist → reel"; revisitable only if telegraphed as a weapon property
- **dependencies:** P5 legibility; 13 status system; Brave composure
- **source:** 12 item 1
- **status:** decided

### D12-2
- **statement:** A fumble (critical failure on the hit roll) is an automatic miss with no additional penalty.
- **layer:** L3
- **rationale:** keep it clean and non-punishing, consistent with heroic model.
- **alternatives_rejected:** fumble with extra penalty (drop weapon, self-hit, etc.)
- **dependencies:** 04 hit roll; P7 heroic
- **source:** 12 item 2; documented in 04
- **status:** decided

### D12-3
- **statement:** Taking damage does not interrupt a charged action; interruption occurs only via a specific interrupt skill or full incapacitation (KO / stopping status), and Brave composure resists it.
- **layer:** L3
- **rationale:** part of the skill-driven status system, not an automatic damage effect.
- **alternatives_rejected:** vanilla-style damage interrupts charge; HP-threshold interruption
- **dependencies:** P7 heroic; 07 Brave composure; 11 magic charge; 13 status
- **source:** 12 item 3; see 11
- **status:** decided

### D12-4
- **statement:** For multi-hit and dual-wield, each strike resolves independently — its own hit roll, its own defense roll, each strike depletes one of the defender's active defenses, and each can crit or fumble. Per-strike power is lower than a single-strike weapon's.
- **layer:** L3
- **rationale:** makes multi-hit a guard-shredder (focus-fire to deplete defenses), balanced by lower power per strike.
- **alternatives_rejected:** one defense roll for the whole flurry; full power per strike
- **dependencies:** 04 active defenses + depletion; P1 contextual balance
- **source:** 12 item 4; documented in 04
- **status:** decided (per-strike power penalty = open calibration)
