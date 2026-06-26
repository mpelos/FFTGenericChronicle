# DCL Validation — Decision Register

Status: Phase A complete for docs 00–13; doc 14 (equipment) appended separately. **Conflict-of-interest: ALL decisions are COI** (the whole corpus was co-authored by the validating model with Marcelo) → every load-bearing critic must be cross-model (GPT-5.5 via codex) or two-disjoint-prompt same-model.

Format: `id · Lx · neutral statement · [status]`. Rationale/alternatives live in the source docs (cited) and are reproduced in the critic packets with rationale **redacted**. Full granular fragments (with rationale+alternatives+deps) are in the Phase-A agent transcripts and `critics/register-frag-00-12.md`.

Layers: L0 pillar · L1 player-experience · L2 system-architecture · L3 mechanic · L4 parameter.

---

## Pillars (doc 00) — see map.md §1 for the attacked/refined set
P1 L0 · No weapon-type/trait/build strictly better; every advantage offset on another axis · [decided→sharpened P1′]
P2 L0 · Every FFT attribute used or replaced; none dead · [decided]
P3 L0 · Damage deterministic (preview=result); RNG only in hit + active-defense · [decided]
P4 L0 · Brave/Faith/Zodiac each a permanent two-sided slider, no universally-best setting · [decided]
P5 L0 · Transparent/readable math; no hidden multipliers · [decided]
P6 L0 · No new items; recharacterize existing via type/reach/modifier · [decided]
P7 L0(promoted) · Heroic: full effectiveness until 0 HP; no death-spiral/HP-threshold penalty · [decided]
P9 L0(PENDING USER) · Action economy is the primary balance constraint; nothing dominates by converting to more turns without hard tradeoff · [open-pillar-risk]
P10 L0(PENDING USER) · AI/enemy counterplay: each mechanic usable/legible by AI, OR explicitly player-only scoped · [open-pillar-risk]
C1 L0(cross-cut) · Complexity budget: depth must fit FFT bandwidth; visible≠predictable-in-play · [open-pillar-risk]

## Scope/architecture (doc 00)
D00-1 L0 · DCL is a separate track from v0.2 formula-balance; inherits none of it · [decided]
D00-2 L2 · Mitigation is subtractive DR, not multiplicative C-bounded · [decided]
D00-3 L2 · GURPS magnitudes re-ranged/bridged to FFT HP/PA scale, not raw · [decided]
D00-4 L2 · GURPS body-type/injury-tolerance NOT imported · [decided]

## Attribute map (doc 01)
D01-1 L0 · Every character-menu attribute assigned a role (reuse/repurpose) · [decided]
D01-2 L2 · PA→ST, base PA only as damage-table input; weapon PA excluded (anti-double-count) · [decided]
D01-3 L2 · HP decoupled from ST; own FFT pool, existing curves · [decided]
D01-4 L3 · Base HP = HT analogue: resists physical statuses (poison/disease/stun/knockdown); base HP only · [decided] ⚠cross-doc
D01-5 L2 · Speed double-duty: feeds Dodge floor + drives CT/turn frequency · [decided] ★load-bearing
D01-6 L2 · Move/Jump retained as positioning, "unchanged in spirit" · [decided] ⚠(14 Weight reduces Move)
D01-7 L2 · C-Ev → Dodge (innate non-depleting floor) · [decided]
D01-8 L2 · S-Ev → Block (strong, depleting, shield-only) · [decided]
D01-9 L3 · W-Ev → Parry (strong, depleting, ≈skill/2+3) · [partially-open]
D01-10 L2 · WP → weapon flat additive damage mod (wmod), scales fast late · [decided]
D01-11 L2 · Weapon range → Reach (outrange/escape-counter/point-blank-weak) · [decided]
D01-12 L2 · Job Level drives weapon-skill growth (per-job/level tables), not JP · [decided]
D01-13 L2 · JP stays ability-purchase currency, does not drive skill · [decided]
D01-14 L2 · MA = magic output on magic axis (FFT-native) · [decided]
D01-15 L2 · MP = spell resource (FFT-native) · [decided]
D01-16 L3 · Faith = magic temperament, two-sided, floor 0.60 · [decided]
D01-17 L2 · Magic Evade → magic dodge · [decided]
D01-18 L3 · Brave = physical temperament + Will analogue (offense/courage-reactions/mental-resist vs −active-defense); no magic · [decided] ⚠cross-doc(stun)
D01-19 L2 · Zodiac → transparent elemental affinity/weakness (hidden mult removed) · [decided]
D01-20 L1 · Brave/Faith/Zodiac = trio of permanent transparent two-sided axes, all surfaced · [decided]
D01-21 L3 · Guard resets on own turn ⇒ turn frequency = emergent defense · [decided] ★load-bearing
D01-22 L2 · Speed = frequency+evasion only, never damage (else ~Speed² compounding) · [decided]
D01-23 L2 · Finesse (Speed→light-weapon damage) considered and rejected · [decided]
D01-24 L1 · "Agile" breadth comes from job skills, not an attribute-scaling axis · [decided]
D01-25 L3 · Weapon +Speed grants (knife) allowed as modest stat sticks, ≠ finesse · [decided]

## Damage model (doc 02)
D02-1 L0 · Damage computed never rolled; preview=result; confirmed undefended hit is exact · [decided]
D02-2 L2 · All RNG lives only in hit roll + active-defense roll · [decided]
D02-3 L2 · Keep GURPS structure (ST→dmg table, types, subtractive armor), drop GURPS damage RNG · [decided]
D02-4 L3 · injury = max(pen_floor, max(0,[base(PA)+wmod]−DR_type)) × wound_mult × G · [decided] ★load-bearing
D02-5 L3 · base(PA) from base PA only via re-ranged thrust/swing table; weapon excluded · [decided]
D02-6 L3 · wmod = weapon flat additive (WP), inflates to large late numbers · [partially-open]
D02-7 L2 · DR_type subtractive, type-specific, subtracted before wound_mult · [decided] ★load-bearing
D02-8 L3 · pen_floor: ≈15–33% of pre-mult raw always lands (anti chip-zero) · [partially-open]
D02-9 L3 · PA re-ranged to ST band ≈ PA+4 before table · [partially-open]
D02-10 L3 · DR scales with wmod inflation (armor stays relevant late) · [partially-open]
D02-11 L3 · G = global bridge constant GURPS-scale→FFT-HP-scale · [partially-open]
D02-12 L2 · Magic does NOT use physical pipeline (no DR/wound-mult) · [decided]
D02-13 L4 · Open: G, PA→ST offset, pen_floor frac, DR-scaling curve, per-weapon wmod · [open]

## Damage types & armor (doc 03)
D03-1 L2 · Exactly four physical types: swing/thrust/crush/missile · [decided]
D03-2 L3 · Swing/cutting ×1.5 wound (swords, axes) · [decided] ⚠P1 thrust-vs-swing
D03-3 L3 · Thrust/impaling ×2 wound (spears, rapiers) · [decided] ⚠P1 thrust-vs-swing
D03-4 L3 · Crush ×1 wound (maces, hammers, staves, fists) · [decided]
D03-5 L3 · Missile/piercing ×1 wound, ranged, pairs w/ armor divisor · [decided]
D03-6 L3 · Wound mult applied post-DR (if DR eats all, mult ×pen_floor only) · [decided]
D03-7 L4 · Crush carries ~1.5× the wmod of equal cutting/impaling weapon · [partially-open]
D03-8 L1 · Types shaped as tradeoffs (raw↔mult), different shapes not a ranking · [decided]
D03-9 L2 · Armor gives type-specific DR; classes resist types differently · [decided]
D03-10 L3 · Full-plate rule: high DR vs cut, partial vs thrust, low vs crush · [decided] ★load-bearing
D03-11 L1 · Plate has 3 answers (crush / armor-divisor ranged / brute force) · [decided]
D03-12 L3 · Armor divisor: missile/gun halves(+) DR before subtraction · [partially-open]
D03-13 L3 · Brute force: enough raw overwhelms DR; wrong type = tax not lock · [decided]
D03-14 L4 · Sharpness MEDIUM: right tool ~+35%, wrong tool ~2× tankier · [partially-open] ★load-bearing
D03-15 L2 · Body-type/injury-tolerance removed; types+armor carry all "what hurts what" · [decided]
D03-16 L4 · Open: per-class DR per type, crush ~1.5×, divisor values, sharpness tuning · [open]

## Hit & defense (doc 04)
D04-1 L2 · Attack = two separate 3d6 rolls (hit by attacker, then defense by defender) · [decided] ★load-bearing
D04-2 L3 · Hit connects on 3d6 ≤ weapon skill; baseline skill 10 · [decided]
D04-3 L3 · If hit connects, defender 3d6 ≤ best active defense turns it aside · [decided]
D04-4 L2 · Damage only if hits AND not defended; then deterministic · [decided]
D04-5 L4 · Skill/defense tuned to 9–16 band of 3d6 · [decided]
D04-6 L3 · Critical hit bypasses the defense roll entirely · [decided] ★load-bearing
D04-7 L4 · Crit on nat 3/4 always; 5 if skill≥15; 6 if skill≥16 · [decided]
D04-8 L3 · Fumble = automatic miss, no extra penalty · [decided]
D04-9 L4 · Fumble on nat 18 always; 17 if skill≤15 · [decided]
D04-10 L2 · Up to three active defenses (Dodge/Parry/Block) from distinct evasion stats · [decided]
D04-11 L3 · Dodge = C-Ev+Speed, always-on, never depletes, baseline ~8 · [partially-open]
D04-12 L3 · Parry = W-Ev, strong, depletes, ≈skill/2+3 · [partially-open]
D04-13 L3 · Block = S-Ev, strong, depletes, shield-only · [partially-open]
D04-14 L2 · System auto-applies single best defense and depletes that one (Q11=auto) · [decided] ★load-bearing
D04-15 L3 · Depleting defenses spent on use; ladder falls to Dodge floor · [partially-open]
D04-16 L3 · Depleted defenses refresh at start of defender's own turn · [decided] ★load-bearing
D04-17 L2 · Speed load-bearing for defense via Dodge floor + refresh rate · [decided] ★load-bearing
D04-18 L4 · Open target (skill≥12 vs Dodge~8) front lands >50% (got 55–67%) · [partially-open]
D04-19 L4 · Shield+parry turtle drops front-on to ~31% · [partially-open]
D04-20 L1 · Two regimes: open dies to direct (>50%), turtle forces positional (~31%) · [decided]
D04-21 L3 · Multi-hit/dual-wield: each strike own hit roll, own defense, each depletes one · [decided]
D04-22 L1 · Multi-hit = guard-shredder (focus-fire engine), not "more DPS" · [decided]
D04-23 L3 · Each multi-hit strike can independently crit/fumble · [decided] ⚠(crit-bypass × N)
D04-24 L4 · Multi-hit balanced by lower power per strike (vs open ≈ single blow) · [partially-open]
D04-25 L3 · Open: counter fires once-per-attack or per-strike · [open]
D04-26 L3 · Targeted/bolt spells evadable via Magic Evade (magic dodge) · [decided]
D04-27 L2 · Magic uses no Parry/Block, not reduced by physical DR · [decided]
D04-28 L4 · Open: exact Dodge/Parry/Block formulas, depletion amounts, fumble detail · [open]

## Facing & positioning (doc 05)
D05-1 L2 · Attack direction modifies target's defense roll · [decided]
D05-2 L4 · Front = full unmodified defense · [decided]
D05-3 L4 · Side/flank = −2 defense · [partially-open]
D05-4 L3 · Back = no defense roll (attack only needs to hit) · [partially-open] ★load-bearing
D05-5 L1 · Defended target overcome by out-positioning; ~31–37% front acceptable · [decided]
D05-6 L1 · Counterplay triangle: flank(facing)/focus-fire(depletion)/crush+pen(type) · [decided] ★load-bearing
D05-7 L2 · Flank route: move around for side/back; costs move, exposes own flank · [decided]
D05-8 L2 · Focus-fire route: drain depleting defenses before target refreshes · [decided]
D05-9 L2 · Crush+pen route: right tool (crush/divisor); costs loadout · [decided]
D05-10 L1 · No route universal; defender can't close all three at once · [decided] ★load-bearing
D05-11 L2 · Facing × depletion stack multiplicatively; all-three collapses a tank · [decided]
D05-12 L3 · Open: back-strike rules vs large/multi-tile units · [open]
D05-13 L3 · Open: facing × area magic · [open]

## Reach (doc 06)
D06-1 L2 · Weapon range = full GURPS reach identity (strengths + real weakness) · [decided]
D06-2 L4 · Reach bands: 2=spears/poles, 1=swords/axes/maces, 0=fists · [decided]
D06-3 L3 · Reach-2 strikes reach-1 foe from 2 tiles (foe can't strike back) · [decided]
D06-4 L3 · Reach-2 from 2 tiles avoids defender's counter · [decided]
D06-5 L3 · Reach-2 penalized vs adjacent foe (close to counter a spearman) · [partially-open] ★(P1 reach tradeoff)
D06-6 L2 · Stop-hit only a lancer-class ability, not universal · [decided]
D06-7 L1 · Reach = a 4th route (outrange) beyond the triangle · [decided]
D06-8 L1 · Reach gives polearms a distinct mid role (not melee-vs-ranged binary) · [decided]
D06-9 L4 · Open: exact point-blank penalty · [open]
D06-10 L3 · Open: reach-2 attack adjacent freely or at cost · [open]
D06-11 L3 · Open: reach × depletion/counter interaction · [open]
D06-12 L4 · Open: stop-hit numbers · [open]

## Brave (doc 07)
D07-1 L2 · Brave/Faith/Zodiac partitioned by domain (body/spirit/element) · [decided]
D07-2 L2 · Brave = permanent two-sided slider, benefit+cost at every setting · [decided]
D07-3 L1 · Litmus: no universal-best; mage must have reason to avoid high Brave · [decided]
D07-4 L2 · Brave downside = active-defense penalty (option A "aggression dial") · [decided] ★load-bearing
D07-5 L3 · Brave scales physical offense (high=more dmg) · [decided]
D07-6 L3 · Brave inverse-scales active defense (high=−Dodge/Parry/Block) · [decided]
D07-7 L3 · Brave scales composure: resists stun/fear/taunt + interrupt · [decided] ⚠cross-doc(stun)
D07-8 L3 · Composure keys off skill-driven statuses, no auto major-wound trigger · [decided]
D07-9 L2 · Reactions in 3 flavours (courage/caution/neutral), job defines which · [partially-open] ★load-bearing
D07-10 L3 · Courage reactions (Counter/riposte) ∝ Brave% · [decided]
D07-11 L3 · Caution reactions ∝ inverse-Brave (needs code hook) · [partially-open]
D07-12 L3 · Neutral reactions flat / other-stat (needs code hook) · [partially-open]
D07-13 L1 · Reaction slot always live; Brave only shifts which flavour excels · [decided]
D07-14 L2 · Brave = physical only; magic entirely on Faith (mage litmus) · [decided] ★load-bearing
D07-15 L1 · Targets: Tank low, Melee-DPS high, Duelist/Archer real choice, Mage low · [decided]
D07-16 L1 · High Brave ~doubles physical dmg, craters survivability (glass↔bunker) · [partially-open] ★SIM
D07-17 L4 · Offense mult ~0.76× (low) to ~1.56× (high) · [open]
D07-18 L4 · Active-defense shift ~+3 (low) to −2 (high) · [open]
D07-19 L4 · Composure interrupt-resist = p_le(round(Brave/10)+5) on 3d6 · [open]
D07-20 L4 · Sim constants k_off≈0.012, def_div=12, physical-only · [open] ★SIM
D07-21 L2 · Brave downside only bites under fire; protected backliner pays nothing (mitigated by facing/flank) · [decided] ⚠known-softspot
D07-22 L4 · Open: shrink k_off for Brave↔Faith symmetry · [open]
D07-23 L4 · Open: Brave curves, composure threshold, per-band numbers · [open]

## Faith (doc 08)
D08-1 L2 · Faith = magic/spirit member of trait trio · [decided]
D08-2 L2 · Faith keeps vanilla two-sided shape, not repurposed · [decided]
D08-3 L3 · Faith scales own+friendly magic output up · [decided]
D08-4 L3 · Faith scales magic vulnerability up (conduit both ways) · [decided]
D08-5 L1 · High Faith = magic glass-cannon, low = resistant-but-inert (mirror of Brave) · [decided]
D08-6 L4 · Faith multiplier floored at 0.60 · [decided]
D08-7 L2 · Physical→Brave only, magic→Faith only; clean partition · [decided] ★load-bearing
D08-8 L2 · Holy/Dark = spiritual dmg on Faith axis, not Zodiac · [decided]
D08-9 L4 · Open: Faith curve, vuln slope, Faith×Zodiac stacking · [open]

## Zodiac (doc 09)
D09-1 L2 · Zodiac repurposed: hidden compat mult → transparent affinity/weakness · [decided]
D09-2 L2 · Zodiac = elemental-temperament member of trio · [decided]
D09-3 L3 · Sign grants resist to affinity element + weak to opposite · [partially-open]
D09-4 L1 · Zodiac effect small/transparent/visible, a nudge not a dominant mult · [partially-open] ★(P4 two-sided?)
D09-5 L3 · 12 signs → 4 elements (3 each, standard astrology) · [partially-open]
D09-6 L4 · Fire=Ari/Leo/Sag; Earth=Tau/Vir/Cap; Wind=Gem/Lib/Aqu; Water=Can/Sco/Pis · [partially-open]
D09-7 L3 · Oppositions Fire↔Water, Earth↔Wind · [partially-open]
D09-8 L4 · ~1/4 of units carry any given element · [decided]
D09-9 L2 · 4 elements map onto existing FFT elements; no new infra · [decided]
D09-10 L3 · Lightning = neutral non-zodiac element · [partially-open]
D09-11 L2 · Holy/Dark outside wheel, on Faith axis · [decided]
D09-12 L2 · Zodiac runs on existing elemental-multiplier math, shown in menu · [decided]
D09-13 L4 · Open: resist/weak magnitude, affinity-boosts-dealing?, Lightning, phys-elem weapons · [open]

## Weapon skill (doc 10)
D10-1 L2 · Skill tracked per weapon family, set by current job + its development · [decided]
D10-2 L3 · Skill = the 3d6 hit-roll target number · [decided]
D10-3 L3 · Each job has A–F grade per weapon family → base skill · [partially-open]
D10-4 L4 · Grades A=13/B=11/C=9/D=7/F=5 · [decided]
D10-5 L1 · Generalists = many families low grade; specialists = few high · [decided]
D10-6 L2 · Skill grows with Job Level via per-job/family curve · [partially-open]
D10-7 L2 · JP doesn't affect skill (hit reliability vs options = separate tracks) · [decided]
D10-8 L3 · "Sword Master" support ability = +2 skill, costs support slot · [partially-open]
D10-9 L3 · Skill capped ~16; over-cap converts to bonus damage/penetration · [partially-open] ★(interacts w/ Brave/crit)
D10-10 L4 · Skill tuned to live in 9–16 band · [decided]

## Magic (doc 11)
D11-1 L2 · Magic on separate FFT-native axis, not through physical pipeline · [decided]
D11-2 L2 · Magic output from MA/MP, Faith, element, target Shell + Zodiac · [partially-open]
D11-3 L3 · Magic damage deterministic; RNG only in whether it lands · [decided]
D11-4 L2 · Magic not reduced by physical DR/wound-mult; Shell+Faith mitigate · [decided]
D11-5 L3 · Single-target/bolt spells evadable via magic dodge · [partially-open]
D11-6 L2 · Magic dodge = non-depleting floor; no magic Parry/Block · [decided]
D11-7 L3 · Charging spell not interrupted by damage; only KO/stop-status/interrupt-skill · [decided]
D11-8 L3 · Interruption only via dedicated skill; Brave composure resists · [decided]
D11-9 L2 · AoE spells interact with facing/position · [partially-open]
D11-10 L2 · Faith two-sided on magic; Faith (not Brave) = magic offense axis · [decided]
D11-11 L2 · Zodiac gives target elemental resist/weak on elemental spells · [decided]
D11-12 L2 · Brave has zero magic effect · [decided]
D11-13 L3 · Elemental+Faith spell applies both multiplicatively · [partially-open]
D11-14 L3 · Holy/Dark scale+resisted by Faith, outside Zodiac wheel · [decided]
D11-15 L3 · Lightning neutral, no sign · [decided]

## Statuses & reactions (doc 13)
D13-1 L2 · Keep FFT status roster + small set of new statuses · [decided]
D13-2 L2 · One mechanic for all status infliction/resist: a 3d6 contest · [decided] ★load-bearing
D13-3 L3 · On connect, target 3d6 ≤ resist number to resist · [decided]
D13-4 L2 · Resist number derived from the status-category stat · [partially-open]
D13-5 L3 · Equipment/usual immunities auto-resist, bypass 3d6 · [decided]
D13-6 L2 · 3d6 contest replaces %-infliction; needs code hook · [partially-open]
D13-7 L2 · GURPS resilience map: ST→basePA, HT→baseHP, Will→Brave, arcane→Faith · [decided]
D13-8 L3 · Physical-status resist uses base HP only (excl gear) · [decided] ⚠cross-doc(stun)
D13-9 L3 · Armor gives DR (+maybe HP) but no status resistance · [decided]
D13-10 L3 · Brave composure = Will, resists mental statuses · [decided]
D13-11 L1 · Low-Brave + low-base-HP = vulnerable to both mental & physical statuses · [decided]
D13-12 L2 · 3 status categories: mental→Brave, physical→baseHP, magical→Faith/MA · [decided] ★load-bearing
D13-13 L3 · Category membership assignment (fear/taunt/charm/confuse/berserk mental; stun/knockdown/poison/disease physical; sleep/petrify/frog/stop/slow/DA/DM magical) · [partially-open] ⚠cross-doc(stun)
D13-14 L3 · Charm/Confuse/Berserk moved to Brave (mental) axis · [partially-open]
D13-15 L3 · Stun (physical, baseHP) = lose action next turn, can move, ~1 turn, no def penalty · [partially-open] ⚠cross-doc(category)
D13-16 L3 · Stun = reskin of Don't Act (kneel anim, "DA" balloon) · [decided]
D13-17 L3 · Knockdown (physical, baseHP) = stands but can't move, can act, ~1 turn · [partially-open]
D13-18 L3 · Knockdown = reskin of Don't Move (lying anim, "DM" balloon) · [decided]
D13-19 L1 · Stun↔knockdown complementary (action vs movement) · [decided]
D13-20 L3 · Fear (mental, Brave) = auto-flee + no enemy-targeting action; can self/ally/item · [partially-open]
D13-21 L3 · Under Fear, reactions normal incl offensive · [decided]
D13-22 L1 · Fear = mirror of Berserk · [decided]
D13-23 L1 · Control statuses kept uncommon/short/curable/Brave-resistable · [partially-open] ★(guardrail)
D13-24 L3 · Fear uses FFT flee AI + no-enemy-target filter (code hook) · [partially-open]
D13-25 L3 · Taunt (mental, Brave) = directed aggression onto taunter (≠Berserk chaos) · [partially-open]
D13-26 L3 · Taunt ideal = directed compulsion to attack taunter · [open]
D13-27 L3 · Taunt fallback = 1-turn native Berserk (approx when taunter nearest) · [decided]
D13-28 L3 · Under Taunt, reactions normal · [decided]
D13-29 L3 · Taunt ideal needs AI work (out of scope); fallback = Berserk reskin · [partially-open]
D13-30 L3 · Interrupt (skill, Brave-resisted) cancels charged action; damage never interrupts · [decided]
D13-31 L2 · Mental statuses affect only voluntary actions; reactions still fire (instinct) · [decided]
D13-32 L2 · Reactions 3 job-defined flavours (courage∝Brave / caution∝inv-Brave / neutral flat) · [partially-open] ★load-bearing(dup D07-9)
D13-33 L1 · Reaction slot always functional; Brave shifts which types excel · [decided]
D13-34 L2 · Statuses classified by impl cost (native reskin vs code hook) · [partially-open]
D13-35 L4 · Open: Brave→3d6 and baseHP→3d6 resist curves · [open]

## Resolved-in-12 (cross-cutting mechanics; some duplicate above)
D12-1 L3 · No automatic damage-triggered reeling; stun/knockdown/fear only via skills · [decided]
D12-2 L3 · Fumble = automatic miss, no penalty (=D04-8) · [decided]
D12-3 L3 · Damage doesn't interrupt charge; only interrupt-skill/incapacitation; Brave resists (=D11-7/8) · [decided]
D12-4 L3 · Multi-hit each strike independent, guard-shredder, lower power/strike (=D04-21..24) · [decided]

## Equipment (doc 14)
D14-1 L2 · Re-express every existing FFT item in DCL terms; no new items · [decided]
D14-2 L0 · Every weapon family best-in-some-context; none strictly better · [decided] ★P1 anchor
D14-3 L4 · Weapon values as relative tiers; constants deferred · [partially-open]
D14-4 L2 · Six weapon dials: type/wmod/reach/parry/hands/especial · [decided]
D14-5 L2 · 7th dial = skill-family (job×family grade = accuracy/identity) · [decided]
D14-6 L3 · One damage type per weapon; no swing/thrust toggle · [decided]
D14-7 L3 · Melee reach only 1 or 2; ranged uses projectile range · [decided]
D14-8 L3 · No GURPS "Unbalanced" flag · [decided]
D14-9 L2 · Handedness only 1H/2H; off-hand fill = job abilities not weapon prop · [decided]
D14-10 L3 · 1H off-hand: shield / 2nd weapon (Two Swords) / Doublehand / nothing · [decided]
D14-11 L3 · 2H = both hands, no shield/dual-wield/Doublehand · [decided]
D14-12 L1 · Weapon defensive identity = parry value (shield-acceptance universal) · [decided]
D14-13 L3 · Cutting ×1.5 also scales PA contribution; blunted by plate · [decided] ⚠P1 thrust-vs-swing
D14-14 L3 · Impaling ×2 highest multiplier · [decided] ⚠P1 thrust-vs-swing
D14-15 L3 · Crush ×1 but wmod ~1.5× + low plate crush-DR · [decided]
D14-16 L3 · Missile ×1 + armor divisor (DR halving) · [decided]
D14-17 L1 · Blades = cutting archetype: strong vs unarmored, no in-family plate answer · [decided]
D14-18 L4 · Knife: perf ×2, low wmod, reach1, low parry, 1H, +Speed; finisher, useless vs plate · [decided]
D14-19 L3 · Knife +Speed = modest stat grant; damage still base(PA) · [decided]
D14-20 L3 · Finesse rejected for weapons (=D01-23) · [decided]
D14-21 L4 · Ninja Blade: corte ×1.5, low-med wmod, med parry, 1H, light/dual-wield · [decided]
D14-22 L4 · Sword: corte ×1.5, med wmod, reach1, ALTO parry (best 1H), 1H · [decided]
D14-23 L4 · Katana: corte ×1.5, alto wmod, best-2H parry, 2H, Draw Out · [decided]
D14-24 L4 · Knight Sword: corte ×1.5, MUITO ALTO wmod (highest), med parry, 2H, knight-only · [decided]
D14-25 L1 · 2H blades split by weight: Knight Sword brute vs Katana lighter/parry/utility · [decided]
D14-26 L3 · Draw Out no longer consumes blade; needs MP cost · [partially-open]
D14-27 L1 · Crush family = anti-armor: ×1 + big wmod + low plate crush-DR · [decided] ★SIM
D14-28 L1 · Crush weapons low parry but 1H (+shield) → anti-knight tank · [decided]
D14-29 L4 · Axe: impacto ×1, MUITO ALTO wmod, low parry, 1H, plate-breaker · [decided]
D14-30 L4 · Flail: impacto ×1, alto wmod, MUITO BAIXO parry, 1H, fura-guarda · [decided] ★SIM
D14-31 L3 · Fura-guarda = −4 to be parried, −2 to be blocked (on defender) · [decided] ⚠(beats 2 defenses)
D14-32 L4 · Bag: impacto ×1, low wmod, utility weapon (job-tied) · [partially-open]
D14-33 L3 · Vanilla random damage on Axe/Flail/Bag dropped (determinism) · [decided]
D14-34 L1 · Reach family = reach2 (outrange/escape-counter/stop-hit) + point-blank weak · [decided]
D14-35 L4 · Spear: perf ×2, med wmod, reach2, med parry, 2H · [decided] ⚠P1(reach+×2)
D14-36 L4 · Pole: impacto ×1, low-med wmod, reach2, MUITO ALTO parry (best in game), 2H · [decided]
D14-37 L3 · Both reach weapons 2H (no shield) · [decided]
D14-38 L2 · Magic weapon adds mod to MA (×Faith) as physical adds wmod to PA (×Brave) · [decided]
D14-39 L3 · Magic weapons weak physical profile, reach1; spells use magic range · [decided]
D14-40 L4 · Rod: impacto ×1, low phys wmod, +offensive-magic, reach1, 1H, elemental SKUs · [decided]
D14-41 L4 · Staff: impacto ×1, +support/heal-magic, reach1, 1H, heal-on-attack/holy SKUs · [decided]
D14-42 L1 · Ranged differentiated purely by context (terrain/LoS/PA/armor); no strict winner · [decided] ★SIM
D14-43 L4 · Bow: missile ×1, med wmod, arc, low divisor, scales PA, 2H no parry · [decided]
D14-44 L4 · Crossbow: missile ×1, med wmod, straight line, med divisor, flat dmg, 2H no parry · [decided]
D14-45 L4 · Gun: missile ×1, med-low wmod, long line, ALTO divisor, flat dmg, 2H, elemental SKUs · [decided]
D14-46 L3 · All ranged missile ×1, 2H, no parry; armor-answer = divisor Bow<Cross<Gun · [decided]
D14-47 L1 · Performer family = job-ability platform; vestigial physical, utility-balanced · [partially-open]
D14-48 L4 · Harp: impacto ×1, v.low wmod, reach1, low parry, 2H; Bard broadcast support · [partially-open]
D14-49 L4 · Cloth: impacto ×1, v.low wmod, reach2, low parry, 1H; Dancer debuff/DoT · [partially-open]
D14-50 L4 · Book: impacto ×1, v.low wmod, reach1, low parry, 1H; Orator talk→status · [partially-open]
D14-51 L3 · Only Cloth has reach2 among performers · [decided]
D14-52 L3 · Orator talk delivers mental statuses (Charm/Confuse/Berserk) · [decided]
D14-53 L2 · Unarmed = job-derived weapon; Martial Arts = Monk skill family · [decided]
D14-54 L3 · Unarmed: impacto ×1, job wmod, reach1, MA parry skill/2+3, no shield/Block · [decided]
D14-55 L3 · Common unarmed: wmod=0 − fist_pen (untrained), scales only PA · [decided]
D14-56 L3 · Monk unarmed: +MA_wmod (Karate, scales w/ MA level→weapon-tier at master) · [decided] ★SIM
D14-57 L3 · Both unarmed pass through Brave offense régua · [decided]
D14-58 L1 · Master Monk < swordsman vs unarmored, > vs plate; no reach/shield/specials · [decided] ★SIM
D14-59 L3 · Monk defense = MA-parry + high HP + Speed/Dodge + reactions; no Block · [decided]
D14-60 L3 · Monk techniques (kicks/grapples/strikes) allowed, none defined · [open]
D14-61 L2 · EVERY equipment piece carries a Weight value; shown climbing in UI · [decided] ★load-bearing
D14-62 L2 · Total Weight summed → calculation → mobility penalty; never flat per-item −Move · [decided] ★load-bearing
D14-63 L2 · Curve maps Weight to Move and Dodge on two separate granularities · [decided]
D14-64 L4 · Weight→Move coarse: dead-zone + few wide steps (most −0, heavy −1, extreme −2/−3) · [decided] ★SIM
D14-65 L4 · Weight→Dodge fine/near-smooth (within-band gradient) · [decided] ★SIM
D14-66 L4 · Illustrative breakpoints: 0–12→−0, 13–26→−1, 27–40→−2, 41+→−3 · [partially-open]
D14-67 L3 · UI telegraphs next Weight breakpoint · [decided]
D14-68 L3 · No PA/ST in Weight→mobility calc (same Weight = same penalty) · [decided] ★lock
D14-69 L3 · Weight coupled to DR; tough-and-light only as rare costed premium · [decided] ★lock
D14-70 L2 · Per-piece Weight = data (Tier-1); Weight→Move/Dodge curve = Tier-2 hook · [decided]
D14-71 L2 · Armor = body slot, main defense dial; cuts Move+Dodge (Weight) never CT · [decided]
D14-72 L1 · Heavy=mitigation (DR+modest HP, high Weight); light=avoidance+positioning · [decided] ★SIM
D14-73 L4 · Classes: Plate/Mail/Leather/Robe DR-by-type/HP/Weight tiers · [decided]
D14-74 L3 · Ruling1: armor cost = Weight→Move+Dodge, never CT (CT reserve knob only) · [decided]
D14-75 L3 · Ruling2: armor DR-primary, HP-modest; HP-home (body vs head) open · [partially-open]
D14-76 L3 · Ruling3: DR type-specific (full-plate rule) · [decided]
D14-77 L3 · Ruling4: caster fragility via robe-only equip restriction, not magic-in-armor penalty · [decided]
D14-78 L3 · Ruling5: light armor = no penalty (relative edge), not a bonus · [decided]
D14-79 L1 · Armor triangle: Plate > Leather > Caster > Plate (each predator+prey) · [decided] ★SIM load-bearing
D14-80 L3 · Shield = free off-hand (1H-weapon-only); grants depleting Block · [decided]
D14-81 L3 · Block = top rung: Dodge < Parry < Block · [decided]
D14-82 L3 · Block = only strong active defense vs RANGED; coverage Dodge=all/Parry=melee/Block=melee+ranged · [decided] ★load-bearing
D14-83 L3 · Shield DR/HP-light, no passive DB (DB = reserve knob) · [decided]
D14-84 L3 · Shield carries Weight separately (tower=real, buckler=~none) · [decided]
D14-85 L1 · Shield lets slow plate-tank advance under fire → ranged>slow-melee>(shield)>ranged · [decided] ★SIM
D14-86 L1 · Shield counterable: focus-fire/flank/crit/fura-guarda/massed-ranged · [decided]
D14-87 L3 · Helmet+accessory also carry Weight · [decided]
D14-88 L2 · Helmet = 2nd small armor slot (DR/HP); decides HP-pool home (helm+HP / hat+MP·MA) · [open]
D14-89 L2 · Accessory = catch-all (resists/movement/special) · [open]
D14-90 L3 · No Fell Sword family; Dark Knight uses existing blades · [decided]
