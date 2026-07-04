# 14 — Summoner · "The Siege Summoner"

The battlefield-scale caster, rebuilt around **commitment** instead of raw nuke. Its texture is the
charge: *"I commit this square of the map to a summon N turns from now — protect me or punish me."* It is
**not** "Black Mage with a bigger area." It is a different axis — **scale + commitment + team defence** —
built on four pillars: **workhorse summons** that punish a clean cluster, rare **finishers** for a siege
ceiling, **Golem** (the team's physical wall), and **Carbuncle** (the team's magic-routing field). Black
keeps the efficient, flexible, repeatable magic; the Summoner answers the boards Black cannot.

This doc records the design decision for the Summoner on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). The magic damage equation is `docs/deep-combat-layer/11`. Method:
`docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a **mid**
  unlock reached past a first-rank caster (`docs/job-balance/00-job-tree.md`; the Summoner keeps its vanilla
  tree position). It earns its keep as a primary (the committed siege + the two team-defence pillars) and as
  a donor (the **Channeling Ward** export; see *R / S / M*).

## The vanilla problems it solves

Vanilla Summoner (`docs/job-balance/vanilla/14-summoner.md`) is **"the babysat glass cannon"** — highest
MA/MP, slowest, most fragile, and reliant on long charges, so it dies before its payoff and folds into a
generic nuker. Four concrete problems, each mapped to a design move:

1. **It dies before it fires.** Fragile + slow + charged means a fresh summon often never resolves. **Fix:**
   the innate **Channeling Ward** — while charging, a modest ward (survives *chip*, not focus-fire); plus
   **Golem-on-self** as a panic button. The commitment becomes *payable* without becoming *free* (J6, SIM 1).
2. **It is "just another nuker" overlapping Black.** **Fix:** reframe the whole axis. The summons are
   **area-committed, not target-flexible** — a big fixed area with placement + friendly-fire friction. Black's
   small AoE already catches an adjacent **pair**, so the Summoner's edge **only begins at k≥3**; everywhere
   else Black wins or ties (SIM 2, SIM 2b). The Summoner is the *committed-scale* caster, not Black-but-bigger.
3. **AoE healing duplicates the White Mage.** **Fix:** **trim the healing summons** (Moogle / Faerie). Area
   restoration is the **White Mage's** lane (`docs/job-balance/jobs/05-white-mage.md`); the Summoner is an
   offence-and-defence siege job, not a second healer (SIM 8).
4. **The summon list reads as an element ladder.** **Fix:** **few distinct profiles, not a ladder.** Each
   workhorse is a big committed area with its own element hook (Ifrit/Shiva/Ramuh/Titan); there is no
   Fire/Fira/Firaga progression and no cheap/small version — that flexible coverage is Black's (SIM 6).

## Fantasy

The siege-conjurer **commits the battlefield**. It calls a colossus that will fall on a quarter of the map a
few turns from now — devastating if the enemy is still standing there, wasted if they scatter. Between
summons it raises **Golem**, a stone bulwark the whole party hides behind until the enemy batters it apart,
and **Carbuncle**, a shimmering field that turns hostile spells back on their casters. It is the slow, fragile
mind that bends the *scale* of the fight — never the nimble skirmisher, never the efficient sniper.

## Chassis

- **Cloth / robes** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the robed conjurer; the mod
  draws no new art). The most fragile chassis in the roster.
- **HP ~70** (the lowest) · **highest MA / MP** · **Speed the slowest** · **Move/Jump low** (the siege engine
  is not mobile) · **Faith HIGH**.
- **Faith is HIGH, deliberately** — the Summoner is a true damage caster, so its magnitude rides high Faith
  (`docs/deep-combat-layer/11`). This is **two-sided**: high Faith on the most fragile chassis makes it
  *doubly* vulnerable to enemy magic and magical statuses. Neutral Faith was rejected as a **hidden free
  lunch** (it would quietly harden the glass cannon while forcing over-tuned summon powers to compensate).
- **The weapon is a rod / staff** — the channel for the **free range-3 bolt** (minimum offence,
  `docs/deep-combat-layer/11`) and an MA stick. The Summoner's output is its **summons**, not its weapon.

## Innate — Channeling Ward (free, and exported)

**A modest defensive ward while charging.** Whenever the unit is mid-charge on any charged action, it gains a
small flat damage reduction — enough to survive **chip** (one or two incidental hits) but **not focus-fire**
(three or more attackers still kill the fragile caster). It is what makes the Summoner's commitment *payable*
on a fragile chassis without erasing the cost (SIM 4).

Critically, it is **not a crutch and not a cost-cut**: it does **not** reduce charge time, MP, friendly fire,
or targeting friction — the commitment is untouched; the ward only helps the caster *live to see it resolve*.
Per the portability rules (`docs/deep-combat-layer/15`), Channeling Ward is **one collapsed package** that is
both the innate *and* the learnable **Support** — and it is **export-clean by construction**: *any* charger
wants it for its own sake (a Black Mage charging -ga, a Time Mage charging Comet, an Archer holding an Aim),
so it is desirable, not a prop that makes some weak skill "work." Candidate innates **rejected**: an
AoE-friendly-fire-safety (breaks Black's friendly-fire brake, or is a Summon-only crutch) and an MP-sustain
(undermines the very expense that defines the job).

## Command — "Summon" (committed battlefield-scale conjuring)

One command, four pillars. The **damage pillars** (workhorse + finisher) are committed area magic; the
**defence pillars** (Golem + Carbuncle) are the team-protection identity. Every summon is **charged** and
**MP-dear** — the Telegraph Invariant holds (`docs/job-balance/jobs/10-time-mage.md`): no tempo tool resolves
or erases the charge window.

**The commitment is two-tier** — the calibration that answers *"don't over-commit or the job is obsolete"*:

- **Workhorse summons** (Ifrit · Shiva · Ramuh · Titan) — **MODERATE** commitment (a short charge, mid-high
  MP). A big **area-committed** drop with its own element hook. It is the everyday tool and the J6 floor. Its
  whole edge is **area-per-action**: it clears a clean, static, ally-free **k≥3** cluster in one action where
  Black needs several casts (SIM 2b). It is **wrong at k=1, marginal at k=2** (Black's small AoE already
  catches a pair more cheaply), and it carries **friendly-fire + placement friction** — so it does not steal
  Black's flexible/anti-backline job.
- **Finishers** (Bahamut · Odin) — **HEAVY** commitment (a long charge, high MP). The rare **siege ceiling**:
  a neutral, battlefield-scale hit. Reserved to the finisher tier precisely so the *everyday* summon can stay
  moderate (SIM 2). *(Lich is a candidate dark-damage finisher, but its drain / undead-state flavour is the
  **Necromancer's** lane — `docs/job-balance/00-job-tree.md`, the Calculator→Necromancer slot, unbuilt; if
  kept here it is **dark damage only**, never a drain/state tool. Deferred to the Necromancer pass.)*

**Why moderate, not heavy, on the workhorse (the obsolescence guard):** a dial sweep shows that pushing the
workhorse to a heavy charge makes it **strictly slower than Black's multi-cast** on the same cluster — i.e.
**obsolete** — *and* breaks the surrounded-J6 floor (the fragile caster can't survive the longer charge even
with a self-Golem). Two independent pressures pin the everyday summon to a moderate commitment (SIM 2).

**The team-defence pillars (the off-cluster identity):**

- **Golem** — the **team's physical wall**: a single shared pool that **absorbs physical damage, depletes per
  hit, and breaks**. It is **not** physical immunity — it soaks a burst, then the enemy gets through
  (SIM 3). Locks: **one active pool**, a **real MP + charge cost** to (re)cast so it cannot be free-refreshed
  into a turtle, **physical only** (a caster or debuffer ignores it), and it does **not** stack
  multiplicatively with Protect into immunity (the pool soaks; Protect only cuts per-hit). Doubles as the lone
  Summoner's **panic button** (self-cast to survive a charge, J6).
- **Carbuncle** — the **team's magic-routing field**: an area effect that **routes** hostile magic (a
  Reflect-style redirection), **not** immunity. Routing rules cap it (one-reflection / fizzle / targetability)
  and it carries **backfire risk** (it can bounce your *own* through-spells; the enemy can bait it). It is the
  **magic mirror of Golem** — both committed, expensive, area, team-scale. It is **share-with-distinction**
  from the **Time Mage's** surgical single-target Reflect (`docs/job-balance/jobs/10-time-mage.md`): different
  **scope** (group vs single), **cost** (heavy vs mid), and **mechanic** (a routing field vs a clean single
  bounce). Time's version stays the right pick on spread, mixed-magic, and low-MP boards (SIM 8).

**The floor:** the **free range-3 rod/staff bolt** (`docs/deep-combat-layer/11`) — minimum offence when out
of MP or when no cluster is worth a summon. It is a fallback, never a damage plan (`docs/deep-combat-layer/15`,
every job a reliable button; the Summoner's plan is its summons).

## R / S / M

- **Reaction — open.** No signature reaction that isn't a crutch; an honest generic slot beats a fake one.
- **Support — Channeling Ward** (the innate, learnable — see *Innate*): the signature export, the
  protect-while-charging package wanted by any charger; export-clean (J2). It costs a slot and does **not**
  reduce the cost it protects — strong, not degenerate.
- **Support — generic caster economy** (Half MP, Short Charge and the like) are equippable as on any caster;
  the Summoner wants them but does not own them. Short Charge stays **moderate / additive-capped** under the
  Telegraph Invariant — it never makes a summon instant.
- **Movement — open.**

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanics owned by `docs/deep-combat-layer/10`):

| Slot | Grant |
|------|-------|
| Armour | **Cloth / robes** (sprite-mandated; the most fragile chassis) |
| Weapon | **Rod / Staff** — the channel for the free bolt and an MA stick (the output is the summons, not the weapon) |
| Off-hand | light (no heavy shield — the chassis is a robe caster) |

*(No martial weapons — the damage identity is **summons**, not the stick.)*

## Early / mid / late

- **Early.** A real, if slow, combatant: a workhorse summon already clears a clean pack, and Golem covers the
  fragility. Dead turns are avoided by the free bolt; no late-unlock dependency.
- **Mid.** The siege proper: read the board — **shield physical** (Golem), **route magic** (Carbuncle), or
  **commit a workhorse** to a forming cluster. Channeling Ward keeps the charge alive under chip.
- **Late.** Battlefield-scale plays: the rare **Bahamut / Odin** finisher as a siege ceiling, Golem anchoring
  a frontal push, Carbuncle neutralising an enemy caster line — and **Channeling Ward** as the export hub for
  every charger on the roster.

## Battle dynamics

**What the player does with it.** Three questions every fight: *do I shield physical (Golem)? route magic
(Carbuncle)? is this cluster worth a summon?* You park the slow, fragile siege engine behind your line, raise
a wall or a routing field as the board demands, and **commit** a workhorse summon to the square where the
enemy is bunching — eating the charge behind Channeling Ward — then cash the rare finisher when the siege
window opens. As a donor it lends **Channeling Ward** to the party's other chargers; it never lends its
summons.

**How an enemy version harms the player.** An enemy Summoner **threatens the whole board at once** — a
committed area that punishes your cluster, a Golem that blunts your melee push, a Carbuncle that turns your
own spells around. Counterplay is rich and reliable: **spread out** (the committed area wants you bunched),
**rush it** (most fragile, slowest — a diver ends it before the charge resolves), **focus-fire through the
ward** (it survives chip, not three attackers), **wait out / batter down Golem** (it breaks), and **don't
feed Carbuncle** (bait the routing, then cast). The charge is a readable telegraph — the punish window is
always there.

## Two-sided cost (why it is not strictly-better)

- **The most fragile, slowest chassis** — high Faith on robes: it dies to a diver and to enemy magic, and it
  arrives late to every exchange. Its safety is Golem and position, both of which pressure removes.
- **The damage wheelhouse is deliberately narrow** — the workhorse wins **only** the clean, static,
  ally-free, element-appropriate **k≥3** cluster. On spread, moving, mixed-resist, scrum, and small-target
  boards, **Black wins or ties** (SIM 2b). The Summoner is decisive, not omnipresent; **Golem + Carbuncle**
  are what field the job when the board is not a clean cluster — so it is not a one-trick AoE bot.
- **Commitment is real** — every summon is charged and MP-dear; the Telegraph Invariant means no tempo tool
  erases the window, and the high MP means few casts per battle.
- **The defence pillars are not walls** — Golem soaks a burst then breaks (no permanent immunity, no
  free-refresh turtle); Carbuncle routes once and can backfire (not immunity).

Distinct from the **Black Mage** (efficient, flexible, repeatable AoE + the element ladder + anti-armour
DR-ignore + single-target burst — `docs/job-balance/jobs/06-black-mage.md`; the Summoner owns *committed
battlefield-scale* areas only at k≥3), the **White Mage** (AoE healing + per-unit wards —
`docs/job-balance/jobs/05-white-mage.md`), the **Time Mage** (the clock + surgical single-target Reflect —
`docs/job-balance/jobs/10-time-mage.md`; Carbuncle is the *group routing-field* version), and the
**Necromancer** (drain / undead-state / Doom — the Calculator→Necromancer slot, unbuilt). The **Summoner owns
the committed siege barrage, the team PHYSICAL wall (Golem), and the team MAGIC-routing field (Carbuncle)**.

## Open items / calibration

All numbers are frozen DCL placeholders (`docs/deep-combat-layer/12`): the workhorse summons' spell power /
area radius / charge / MP (calibrated so the edge begins at **k≥3**, not k≤2, and the everyday charge stays
**moderate**); the finishers' magnitude / charge / MP; **Golem's** pool size, recast MP/CT, and depletion
rate; **Carbuncle's** routing rules (one-reflection vs fizzle, targetability, backfire scope) and MP/CT;
**Channeling Ward's** flat DR (tuned to *chip-not-focus-fire*); the free bolt's output; and whether **Lich**
is kept as a dark-damage finisher (pending the Necromancer pass). The **Carbuncle vs Time-Reflect** share is
owned here and cross-referenced from `docs/job-balance/jobs/10-time-mage.md`.
