# Equipment — Weapons and Gear

Status: Draft (weapon families designed through ranged; performer/utility, fell sword, unarmed, and
the armor/shield/helmet/accessory slots still pending)
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
| **Knife** | Perfuração ×2 | baixo | 1 | baixo | 1H | +Speed |
| **Ninja Blade** | Corte ×1.5 | baixo-médio | 1 | médio | 1H | leve |
| **Sword** | Corte ×1.5 | médio | 1 | **alto** | 1H | melhor parry 1H |
| **Katana** | Corte ×1.5 | **alto** | 1 | **alto** | **2H** | Draw Out |
| **Knight Sword** | Corte ×1.5 | **muito alto** | 1 | médio | **2H** | exclusivo cavaleiro |

- Blades are the **anti-light** archetype: great vs unarmored, blunted by plate. Within the family
  there is no plate-answer — you bring crush/penetration or brute `wmod`.
- **Knife** is the lone exception (perfuração ×2): low `wmod` but ×2 = the **assassin**, devastating
  on the unarmored, useless vs plate. +Speed reinforces the thief/ninja role.
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

---

## Still pending

Weapon families not yet characterized:

- **Performer / utility** — Harp/Instrument (Bard), Cloth (Dancer), Book/Dictionary (Orator).
  Utility-first weapons like the Bag; their combat profile and *role* are open, and their specific
  functions tie to job design (deferred).
- **Fell Sword** (Dark Knight) — a blade family with an HP/MP drain rider.
- **Unarmed / fists** (Monk) — needs its own characterization.

Equipment slots not yet designed at all:

- **Armor** — type-specific DR (`03`), the base-HP pool, no status resistance from gear (`13`).
- **Shield** — Block (the depleting S-Ev defense, `04`).
- **Helmet** — DR / HP contribution.
- **Accessory** — the catch-all (resists, movement, special properties).

All tiers above are relative; numeric calibration (wmod, parry, divisor, reach, the crush ~1.5×
multiplier) is tracked in `12-open-questions.md`.
