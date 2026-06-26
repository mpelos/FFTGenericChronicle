# Equipment — Weapons and Gear

Status: Draft (all weapon families + Weight model + armor + shield designed; helmet / accessory pending)
Date: 2026-06-25
Depends on: 02-damage-model, 03-damage-types-and-armor, 04-hit-and-defense, 06-reach,
10-weapon-skill, 11-magic.
Review: Pending — design-in-progress.

## Scope and lens

This document characterizes every existing FFT weapon (and, later, every armor / shield / helmet /
accessory) inside the DCL rules. **No new equipment is created** — each existing item is re-expressed
in DCL terms.

The governing lens is the core philosophy (`00`): **balance through contextual differentiation —
no weapon is strictly better than another.** Every family is best in some context and worse in
others, and any advantage on one dial is paid for on another. The weapon tables below were each
checked against that rule.

The numbers here are **relative tiers** (baixo / médio / alto / muito alto), not final values —
exact `wmod`, parry, divisor, and reach constants are deferred calibration (`12`).

## The weapon dial template

Every weapon is expressed through six dials. The first four map directly onto existing FFT attributes
(`01`), so this is re-characterization, not new data:

| Dial | Source (FFT) | What it does |
|------|--------------|--------------|
| **Tipo de dano** | WP-type assignment | corte ×1.5 / perfuração ×2 / impacto ×1 / míssil ×1 — the wound multiplier and the armor matchup (`03`). |
| **wmod** | WP | flat additive damage modifier on top of `base(PA)` (`02`). |
| **Reach** | weapon range | melee **1** or **2** (reach `06`); ranged weapons use the FFT projectile range instead. |
| **Parry** | W-Ev | depleting active-defense bonus (`04`, ≈ skill/2 + 3). |
| **Mãos** | item slot | **1H** (off-hand free) or **2H** (both hands). See the off-hand model below. |
| **Especial** | item properties | element, status rider, magic-power mod, armor divisor, arc/line, Draw Out, etc. |

A seventh, implicit dial is the **skill-family**: each weapon family is its own skill family, and the
job×family grade (A–F) governs accuracy (`10`). It is the family identity itself, not a per-weapon
number.

Two locks set during design (Marcelo, 2026-06-25):

- **One damage type per weapon.** No swing/thrust dual-mode toggle (too much UI/decision load for
  FFT). The GURPS richness is captured instead by *spreading* types across the roster.
- **Reach is 1 or 2 only.** No GURPS reach codes (C / 1,2,3 / 1-7). Melee is 1 or 2; ranged uses
  projectile range.
- **No "Unbalanced".** The GURPS "can't parry the turn you attack" flag is not used.

## The off-hand model

Handedness is just **1H vs 2H**. What fills a free off-hand is a *separate* axis, driven by job
abilities — not a weapon property:

- **1H** — one hand; the off-hand can hold a **shield** (anyone who can equip one), a **second
  weapon** (only with Two Swords / dual-wield, on qualifying 1H weapons), the same weapon in **both
  hands** (Doublehand, +wmod, no off-hand), or nothing.
- **2H** — both hands; **no shield, no dual-wield, no Doublehand.**

Consequence used throughout: a weapon's defensive identity comes from its **parry**, not from
"accepting a shield" (every 1H weapon accepts one).

## Damage-type recap (`03`)

| Type | Mult | Note |
|------|------|------|
| Corte (cutting) | ×1.5 | anti-flesh; the ×1.5 also scales the **PA** contribution, so cutting beats crush vs unarmored. Blunted by plate. |
| Perfuração (impaling) | ×2 | highest multiplier; armor-defeating point. |
| Impacto (crush) | ×1 | no multiplier, but **wmod runs ~1.5×** a cutting weapon and **plate has low crush-DR** → the plate-answer. |
| Míssil (missile) | ×1 | ranged; pairs with **armor divisor** (DR halving) as its armor-answer, not the GURPS arrow=impaling. |

---

## Weapon families

### Blades — the cutting archetype

| Família | Tipo | wmod | Reach | Parry | Mãos | Especial |
|---------|------|------|-------|-------|------|----------|
| **Knife** | Perfuração ×2 | baixo | 1 | baixo | 1H | +Speed (grant) |
| **Ninja Blade** | Corte ×1.5 | baixo-médio | 1 | médio | 1H | leve |
| **Sword** | Corte ×1.5 | médio | 1 | **alto** | 1H | melhor parry 1H |
| **Katana** | Corte ×1.5 | **alto** | 1 | **alto** | **2H** | Draw Out |
| **Knight Sword** | Corte ×1.5 | **muito alto** | 1 | médio | **2H** | exclusivo cavaleiro |

- Blades are the **anti-light** archetype: great vs unarmored, blunted by plate. Within the family
  there is no plate-answer — you bring crush/penetration or brute `wmod`.
- **Knife** is the lone exception (perfuração ×2): low `wmod`, but ×2 makes it the **assassin's
  finisher** — it punches above its weight on weakened or unarmored targets, and is useless vs plate.
  Its power is the multiplier + utility, not raw damage. Its `+Speed` is a **modest stat grant** (the
  wielder gets a touch faster) — **not** finesse: knife damage still uses `base(PA)`, never Speed.
  (Finesse — scaling damage off Speed — was considered and rejected; see `01`. The wielder's real
  speed comes from the *job*, not the blade.)
- 1H trio: **Knife** (burst vs squishy), **Ninja Blade** (light, dual-wield-friendly), **Sword** (the
  defensive baseline — **best 1H parry**).
- 2H pair, split by **weight**: **Knight Sword** = the brute (**highest `wmod` in the game**, only
  médio parry, knight-exclusive); **Katana** = the lighter 2H (alto `wmod` *below* Knight Sword, but
  **best parry among 2H** + **Draw Out**). The lighter weapon trades raw damage for parry + utility.
- **Draw Out no longer consumes the blade** → it becomes repeatable and needs another cost (MP); that
  is a Samurai job-design detail, deferred.

### Impact / crush — the anti-armor archetype

| Família | Tipo | wmod *(escala crush ≈1.5×)* | Reach | Parry | Mãos | Especial |
|---------|------|------|-------|-------|------|----------|
| **Axe** | Impacto ×1 | **muito alto** | 1 | baixo | 1H | brute, quebra-placa |
| **Flail** | Impacto ×1 | alto | 1 | **muito baixo** | 1H | fura-guarda |
| **Bag** | Impacto ×1 | baixo | 1 | baixo | 1H | **arma de utilidade — buff ou debuff** (função a definir nos jobs) |

- The crush archetype: ×1 multiplier but big `wmod` (~1.5× a cutting weapon, by `03`) + low plate
  crush-DR → **smashes knights**. Weak vs unarmored/light (the ×1 can't match cutting/impaling, whose
  multiplier also scales PA). Self-balancing matchup.
- All **defend poorly with the weapon** (low parry) — they smash, they don't fence — but are 1H, so
  you bolt on a **shield** (crush + shield = the anti-knight tank).
- **Axe** = pure brute (max crush `wmod`). **Flail** = the anti-defense crusher: beats armor (crush)
  *and* active defense (**fura-guarda** = the GURPS −4-to-be-parried / −2-to-be-blocked, which
  survives our locks because it penalizes the *defender*, not the cut "Unbalanced"). **Bag** is in the
  group only by damage type; its role is **utility** (buff/debuff), with the specific function tied to
  jobs (deferred) — it is not a damage or anti-armor option.
- **Vanilla random damage is dropped** (Axe/Flail/Bag) — the DCL is deterministic (`02`).

### Reach / haste — defined by reach 2

| Família | Tipo | wmod | Reach | Parry | Mãos | Especial |
|---------|------|------|-------|-------|------|----------|
| **Spear** | Perfuração ×2 | médio | **2** | médio | 2H | reach ofensivo |
| **Pole/Stick** | Impacto ×1 | baixo-médio | **2** | **muito alto** | 2H | reach defensivo; melhor parry do jogo (quarterstaff) |

- Both live on **reach 2** (outrange, escape-counter, stop-hit) with the **point-blank weakness**
  (`06`) — a fast melee that closes the gap shuts them down.
- **Spear** = offensive reach: impaling ×2 at range; `wmod` only médio because the reach is the
  premium (and ×2 must not explode). **Pole** = defensive reach: the quarterstaff — **best parry in
  the game** + crush + reach, but low `wmod` (controls space and outlasts; does not kill).
- Both **2H** (a reach weapon gives up the shield).

### Magic weapons — the caster's amplifier

| Família | Tipo (físico) | wmod físico | **Mod mágico** | Reach | Parry | Mãos | Especial |
|---------|---------------|-------------|----------------|-------|-------|------|----------|
| **Rod** | Impacto ×1 | baixo | **+magia ofensiva** (alto) | 1 | baixo | 1H | elementos em SKUs (sinergia Zodiac `09`) |
| **Staff** | Impacto ×1 | baixo | **+magia de suporte/cura** (alto) | 1 | baixo-médio | 1H | cura ao atacar (healing staff); holy em SKUs |

- A magic weapon is the **magic analogue of a physical weapon**: it adds a **magic-power modifier to
  MA** the way a physical weapon adds `wmod` to PA. This completes the symmetry of `01`:
  **Físico = PA + wmod, escalado por Brave** ; **Mágico = MA + mod-mágico, escalado por Faith.**
- Physical profile deliberately weak (you do not bring a mage to melee). Physical attack is reach 1;
  spells use the magic range system (`11`).
- **Rod** = offensive magic (damage spells; elemental SKUs feed Zodiac `09`). **Staff** = support
  magic (healing/holy; some heal on attack). Same offense/support split as the other pairs, on the
  magic axis.

### Ranged — contextual balance, no rate-of-fire

This group is the clearest example of the core philosophy. **Rate-of-fire / reload is deliberately
not modeled** (it does not fit FFT); the three are differentiated purely by *context*, and the extra
power of each only appears inside its own context.

| Família | Tipo | wmod | Alcance / LoS | Divisor | Escala c/ PA | Mãos |
|---------|------|------|---------------|---------|--------------|------|
| **Bow** | Míssil ×1 | médio | médio-longo, **arco** | baixo | **sim** | 2H, sem parry |
| **Crossbow** | Míssil ×1 | médio | médio, **linha reta** | médio | não (fixo) | 2H, sem parry |
| **Gun** | Míssil ×1 | **médio-baixo** | **longo**, linha reta | **alto** | não (fixo) | 2H, sem parry |

- **Bow** — best in **vertical / broken terrain** (the **arc** clears walls, units, height) **and** on
  a **high-PA archer** (damage scales with PA). Weak on flat open ground, on a low-PA user, and vs
  armor. (Its PA-scaling keeps **PA alive for archer builds**, serving the use-or-replace rule `01`.)
- **Crossbow** — best in the **open with a direct line**, on **any unit** (flat damage, no PA needed).
  Weak in terrain/height (straight line is blocked) and gains nothing from high PA.
- **Gun** — best **vs armored targets** (high divisor) and at **extreme range**. It **trades base
  damage for penetration**: vs unarmored it does *less* than a crossbow (lower `wmod`, divisor
  wasted). Weak vs squishy, terrain-blocked, and rare. Some SKUs are elemental (Zodiac `09`).
- All are **missile ×1, 2H, no parry** → defensively naked in melee; their defense is staying at
  range (a harder version of the reach point-blank weakness). The armor-answer is the **divisor**
  (`03`), scaling Bow(baixo) → Crossbow(médio) → Gun(alto).
- No strict winner: flat field vs flesh → Crossbow/Bow; height → Bow; high PA → Bow; vs armor →
  Gun; low-PA in the open → Crossbow.

### Performer / utility — job platforms, not weapons

These exist for their **job** abilities (the Bard's songs, the Dancer's dances, the Orator's talk),
deferred to job design. The physical attack is vestigial; they are balanced on the **utility axis**,
not in combat — exactly like the Bag.

| Família | Tipo | wmod | Reach | Parry | Mãos | Papel (job, deferred) |
|---------|------|------|-------|-------|------|------------------------|
| **Harp / Instrument** | Impacto ×1 | muito baixo | 1 | baixo | 2H | broadcast support (songs / AoE-over-time) — Bard |
| **Cloth** | Impacto ×1 | muito baixo | **2** | baixo | 1H | debuff / damage-over-time (dances) — Dancer |
| **Book / Dictionary** | Impacto ×1 | muito baixo | 1 | baixo | 1H | social / control (talk → status) — Orator |

- Nobody brings these to trade blows (minimal wmod, low parry, crush ×1) — the value is the job skill.
- **Cloth = reach 2** is the group's one real combat dial: the Dancer's whip/ribbon gets a
  positioning edge; the other two are reach-1 shells.
- The Orator's talk delivers exactly the mental statuses moved onto Brave (Charm / Confuse / Berserk,
  `13`).

### Unarmed / Martial Arts — the Monk's body as the weapon

Unarmed is a weapon with **no item**, so its profile is **job-derived**. **Martial Arts** is the
Monk's "weapon family" (a GURPS-Karate analogue): a single skill, grown by Monk job level (`10`),
that supplies accuracy + damage + parry from the body.

| Família | Tipo | wmod | Reach | Parry | Mãos | Especial |
|---------|------|------|-------|-------|------|----------|
| **Unarmed** | Impacto ×1 | job-derived (below) | 1 | **MA parry = skill/2 + 3** (depletes/resets) | no weapon (no shield / no Block) | self-scaling style |

**Damage uses the standard pipeline (`02`), crush ×1 vs DR_crush — only the `wmod` source differs:**

- **Common unarmed** (any unit, no Martial Arts):
  `injury = max(pen_floor, max(0, [base(PA) − fist_pen] − DR_crush)) × 1 × G`
  — `wmod = 0` **plus** a small flat **untrained-fist penalty** `fist_pen` (the GURPS `thr-1`): a
  bare, untrained punch is strictly a last-resort chip, clearly below any weapon. Scales only with PA.
- **Monk unarmed** (Martial Arts):
  `injury = max(pen_floor, max(0, [base(PA) + MA_wmod] − DR_crush)) × 1 × G`
  — no penalty; `MA_wmod` is the Karate-style bonus that **scales with Martial Arts level** (a
  per-job-level curve like `10`), reaching a real weapon's wmod at master level. The Monk's entire
  offensive edge over a random puncher is `MA_wmod`.

Both also pass through the **Brave** offense régua (`07`): Monk damage = `(base(PA) + MA_wmod) ×
Brave-offense`, crush ×1 — stacking **PA + Brave + Martial-Arts level**, all Monk-heavy stats (and
matching the vanilla feel of Monk damage scaling with Brave).

**Balance point:** at the top `MA_wmod` ≈ a strong weapon's wmod, but **crush ×1 vs cutting ×1.5** →
a master Monk does *less* than a swordsman vs the unarmored, but *more* vs plate (crush defeats
armor), with no reach, no shield, no weapon specials. A self-sufficient **anti-armor** striker that
dominates no one.

**Defense:** the Monk parries barehanded (MA parry = skill/2 + 3, depleting like a weapon parry,
`04`) but has **no Block** (no shield); its defensive identity = MA-parry + high HP + Speed/Dodge +
reactions (Counter / Hamedo-like). Common unarmed has only a negligible parry.

**Deferred (Monk job design):** techniques — kicks (bigger crush at −accuracy), grapples/throws
(status delivery), special strikes. The framework allows them; none are defined now.

---

## Gear

### Weight — the mobility cost of all gear

**Every equipment piece carries a `Weight` value** — armor, shield, helmet, accessory, even a weapon.
This is non-negotiable: gear *must* have weight, because Weight is the single, legible currency that
the heavy/light tradeoff is paid in. Light pieces carry a small or zero Weight; heavy pieces carry a
lot. The player **sees Weight climbing** as they equip, and equips *consciously* (Marcelo, 2026-06-26).

The total Weight (the **sum** of all equipped pieces) is converted **through a calculation** into the
mobility penalty — it is **never** a flat `−Move` printed on a piece. The reason is FFT-specific:
**Move is a coarse, brutal stat** (base ~3–4 tiles; "2 of Move is a lot"), so a flat per-item `−Move`
has no middle ground (either −0 = nothing, or −1 = huge) and stacking pieces would annihilate Move.
The penalty therefore has to be *born from a curve* on aggregate Weight.

**The curve maps the two costs differently, because Move and Dodge have different granularities:**

- **Weight → Move = coarse, with a dead-zone + a few wide steps.** This *protects* Move: most builds
  sit at **−0**, only heavy commitment reaches −1, and only extremes drop to −2 / −3 (the
  "barely-walks" fortress). Move is never zeroed by accident.
- **Weight → Dodge = fine, near-smooth.** This is the *within-band* gradient (heavier always dodges a
  little worse). Dodge is a 3d6 modifier, so it tolerates the continuous scale that Move cannot.

Illustrative only (numbers are calibration placeholders, `12`):

| Weight total | Move | who lands here |
|--------------|------|----------------|
| 0–12 | **−0** | robe/mage, leather, **even leather + shield** |
| 13–26 | **−1** | mail bruiser; "normal" plate |
| 27–40 | **−2** | plate + heavy shield/helm |
| 41+ | **−3** | the walking fortress |

Tuning intent: a **generous dead-zone** (a light build, even with a shield, pays **zero** Move — only
Dodge); **−1 is the typical heavy cost**; −2/−3 is reserved for deliberately turning yourself into a
bunker. **The UI telegraphs the next breakpoint** ("Weight 24 / 26 → +2 more and Move drops") — that
breakpoint *is* the equip decision.

Two locks on the Weight model (Marcelo, 2026-06-26 — approved):

- **No PA/ST in the calculation.** Same Weight → same penalty for everyone. If PA reduced Weight, the
  high-PA melee would wear plate at almost no mobility cost → **leather becomes pointless for it**.
  That cliff stays closed (it is the same hazard as a finesse-style stat shortcut).
- **Weight is coupled to DR by default.** More protection costs more Weight, at *every* tier
  (early→late), so the heavy/light tradeoff is **invariant** across the whole game and never gets
  power-crept away. A "tough **and** light" piece (high DR, low Weight) is the exact item that
  *dissolves* the tradeoff and kills leather builds — so it exists only as a **rare, costed premium**
  (a mithril exception paid for in rarity / elemental weakness / slot cost), never the baseline.

**Nature / feasibility:** the per-piece Weight value is plain **data** (easy); the **Weight → Move /
Dodge curve is a computed hook** (Tier-2), flagged in `12` (item 7).

### Armor (body slot) — mitigation vs avoidance

Armor is the body slot, and it is the game's main **defense-paradigm dial**. Heavy armor reduces
**Move and Dodge** (via its Weight, above) **but never action frequency** (CT stays a pure function of
the Speed stat, `01`); so armor trades two opposed currencies:

- **Heavy → mitigation:** high **DR** (by type, `03`) + a modest **HP** buffer, paid for with high
  **Weight** (→ −Move / −Dodge).
- **Light → avoidance + positioning:** full **Move and Dodge** (low Weight), paid for with low DR / HP.

The axis is therefore **mitigation (DR) vs avoidance (Dodge) + positioning (Move)** — two different
ways of not dying, not "more vs less of the same."

| Classe | DR (corte/perf) | DR (crush) | HP | Weight (→ −Move/−Dodge) | Quem veste |
|--------|-----------------|------------|----|-------------------------|------------|
| **Plate (pesada)** | **alto** | **baixo** *(regra full-plate `03`)* | médio | **muito alto** | cavaleiro-âncora |
| **Mail (média)** | médio | médio-baixo | baixo-médio | médio | bruiser |
| **Leather (leve)** | baixo | baixo | baixo | baixo | skirmisher / archer / monk |
| **Robe (pano)** | mínimo | mínimo | mínimo | mínimo | caster (restrito a robe) |

Design rulings (Marcelo, 2026-06-25 / 26 — approved):

1. **The mobility cost is Weight → Move + Dodge, never CT** (see the Weight model above). Turn-frequency
   stays a pure function of the Speed stat (`01`); armor must not touch it (cutting CT would be the
   inverse of the rejected finesse *compounding* hammer). *A small heavy-armor CT penalty is kept as an
   explicit **reserve knob** (`12`) only if the leather-melee proves too weak in playtest.*
2. **DR-primary, HP-modest.** DR (and its **type matchup**) is the star; the HP contribution is a
   small buffer, not an HP race. This keeps the matchup sharp and keeps **base-HP** clean as the
   status-resist stat (`13`) rather than swamping it with gear HP. (Deliberately unlike vanilla FFT,
   where armor = lots of HP.) *Open thread: whether this modest HP stays on the body slot or migrates
   entirely to the head slot (orthogonal slots: body = DR + Weight, head = the HP/MP pool) is resolved
   with the **helmet** slot — pending.*
3. **DR is type-specific** — the full-plate rule (`03`): plate walls cutting/impaling but is soft to
   **crush**; light armor is thin against everything.
4. **Caster fragility comes from equip restriction** (mage jobs equip robe only, FFT-native), **not**
   a magic-in-armor penalty. Mages cannot buy physical survivability with gear — they stay fragile by
   access, which is simpler and keeps the magic axis clean (`11`).
5. **Light = no penalty**, not a bonus. Its edge is *relative* (it keeps the Move/Dodge that heavy
   loses); it is not over-sweetened with extra mobility.

**Why this balances — the archetype triangle.** Running the builds against each other:

- **Plate-melee (Knight):** reliable physical survival (DR + HP + Block/Parry), holds ground, hits
  big — but **slow** (kited, can't flank) and its DR is **worthless vs magic / crush / status** (a
  low-Brave knight still gets stunned, `13`). Defense = mitigation.
- **Leather-melee (skirmisher):** mobility → **flanks for back-attacks** (ignores the defense roll,
  `05`) + kites + picks engagements, plus real Dodge — but **folds if caught or caught by AoE**.
  Defense = avoidance + positioning; its offense ceiling comes from flanking (higher skill).
- **Caster (robe):** magic **ignores physical DR** (`11`) → **punishes the plate knight** whose whole
  investment is DR; but is the most fragile, so the leather skirmisher closes and bursts it.

This yields a clean **Plate > Leather > Caster > Plate** triangle — each armor archetype has a
predator and a prey. Plate-melee and leather-melee are not mirror-balanced; they are
**context-balanced** (the philosophy, `00`): plate wins grind / open ground / vs-physical, leather
wins broken terrain / flanking / objectives / vs-slow, and their direct duel is a healthy stalemate
(a light weapon can't crack plate DR; plate can't catch leather) **resolved by the team** — bring
crush/magic to break plate, bring accuracy/ranged to pin leather. Archers, monks, and other
mobility-first jobs sit naturally in leather (their defense is range / HP+Dodge+reactions, not DR;
plate would only slow them).

### Shield (off-hand) — the finite active wall

A shield lives in a free off-hand, so it is **1H-weapon-only** (never with a 2H weapon, dual-wield, or
Doublehand). It grants **Block** — the depleting S-Ev active defense (`04`) that resets on the
wielder's turn. The design problem it must solve: **Block must be distinct from Parry**, or it does
not justify the off-hand cost (Parry comes free with most 1H weapons; the Sword has the best parry in
the game). Two things make it distinct:

1. **Block is the top rung** of the defense ladder — **Dodge (floor) < Parry < Block** — the wielder's
   single best answer to one committed hit per turn.
2. **Block has the broadest coverage** — it is the **only strong active defense against ranged**. You
   cannot **parry** a missile (Parry is melee-only) and Dodge is just the weak floor, but a shield
   **blocks** arrows / bolts / thrown.

The coverage ladder, now fully defined:

| Defense | Covers | Source | Depletes? |
|---------|--------|--------|-----------|
| **Dodge** | everything (the floor) | Speed (`01`) | no |
| **Parry** | melee only | weapon (`14`) | yes |
| **Block** | melee **and ranged** | shield | yes |

This coverage rule is the shield's identity and belongs to the defense system (`04`).

**The shield is DR/HP-light** — *not* a second armor piece. Its value is the Block roll + the ranged
coverage + the off-hand opportunity cost (you give up dual-wield / Doublehand / a utility off-hand).
Loading it with flat DR would make it redundant passive mitigation and erode the clean "light =
avoidance, plate = mitigation" axis (`above`). **No DB / no passive bonus** either — the shield is a
*finite, legible, per-turn* resource, not an always-on floor-raiser (a modest GURPS-style **Defense
Bonus** is held as a `12` reserve knob only if the shield plays too binary).

A shield **also carries Weight** (`above`): a heavy tower shield adds real Weight (→ the Move/Dodge
penalty), a light buckler almost none. That is the shield's own contribution to the heavy/light
tradeoff — the leather skirmisher takes a light shield (stays in the Move dead-zone), the plate anvil
can afford a heavy one (already past the breakpoint). Weight is *separate* from "DR/HP-light": the
shield adds no body DR, but a heavy one still slows you.

**Why this balances:**

- **Distinct from parry** (bigger, weapon-independent, and covers ranged) and **distinct from armor**
  (active / finite / facing-gated vs passive / always-on).
- **Load-bearing for the armor triangle:** the slow plate-tank (−Move) would otherwise be **kited and
  shot down before closing** — ranged would hard-counter plate. With a shield it **advances under fire
  (blocks shots on the approach)**. The shield is *how the mitigation-tank survives the approach*, and
  the melee answer to archers — completing **ranged > slow no-shield melee > (shield) > ranged**.
- **Two flavors, both real:** leather + shield = a skirmisher who can still eat one hit per turn (pays
  the off-hand for it); plate + shield = the classic anvil that walks into an arrow storm (pays in
  offense — no 2H Knight Sword / Katana).
- **Fully countered:** focus-fire depletes Block; flank / back ignores it (facing, `05`); crit bypasses
  it; **fura-guarda** (Flail) is −2 to be blocked (`14`); and **massed ranged overwhelms** it (blocks
  one shot, then depleted) — so it is never an impassable wall.

### Helmet, accessory — pending

Both still carry **Weight** (`above`) like every other piece — a heavy helm adds to the total, a hat
or a light trinket adds ~none.

- **Helmet** — DR / HP contribution (a second, smaller armor slot). The body-vs-head **home of the HP
  pool** (helmet = +HP martial / hat = +MP・MA caster, the "body vs mind" split) is decided here.
- **Accessory** — the catch-all (resists, movement, special properties).

## Still pending

All weapon families are now characterized: blades, crush, reach, magic, ranged, performer/utility,
and unarmed. *(Rejected: a separate **Fell Sword** family — FFT: The Ivalice Chronicles has no
fell-sword weapon type; the Dark Knight uses existing blades.)* The **armor** and **shield** slots are
now designed (above); **helmet / accessory** remain.

All tiers above are relative; numeric calibration (wmod, parry, divisor, reach, the crush ~1.5×
multiplier) is tracked in `12-open-questions.md`.
