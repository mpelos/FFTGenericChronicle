# 13 — Geomancer · "The Elemental Reaver"

The terrain warrior, rebuilt as a **melee fighter whose tactical options come from the ground underfoot**.
It fights up close with a strong weapon (best with the axe), and weaponizes the tile it stands on through
**Geomancy** — a short-range, terrain-keyed toolkit for reaching what melee cannot, striking an elemental
weakness, and landing a reliable, tactical status. It is not the do-everything jack of the vanilla job; it
is a real combatant that the **land fights alongside**.

This doc records the design decision for the Geomancer on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a
  mid-tree unlock reached past a first-rank job (`docs/job-balance/00-job-tree.md`), alongside Monk and
  Thief. It is reached **after strong melee jobs (Knight, Monk)**, so it is authored to be a melee peer —
  never a downgrade into a fragile caster.

## The vanilla problems it solves

Vanilla Geomancer (`docs/job-balance/vanilla/13-geomancer.md`) is "good at everything, best at nothing": a
sturdy hybrid with a free, instant, ranged elemental attack. Four concrete feel-bad problems are fixed,
each mapped to a design move:

1. **Bad at approaching, then useless up close.** The DCL Geomancer **is a melee bruiser** — it *wants* to
   be in the fight (strong normal attack, Axe A), and its mobility (Move 4 + Ignore Terrain) is the
   *wind-up*: moving to the tile you want is how you choose your element. Approach is no longer a tax; it
   is the setup.
2. **Weak in melee.** Its bread-and-butter is a **strong normal weapon attack** on an Axe-A body — a real
   melee threat, a peer to the jobs before it.
3. **Weak ranged elementals.** Geomancy is no longer the whole offense (so it need not carry the job); it
   is a **tactical short-range tool**, deliberately *below* a normal melee hit in damage, used for reach /
   element / status — never as the main damage.
4. **Status by pure luck.** The terrain status is made **reliable by position**: standing on the matching
   terrain is what unlocks that status at all, at a high base rate (still subject to resistance/immunity),
   so it is a **planned tactic**, not a coin-flip rider.

## Fantasy

The elemental reaver: a frontline fighter who reads the land and turns it into a weapon — strikes hard
with steel up close, splits the earth or floods a gap when it must, and carves the terrain's element into
whatever stands on the wrong ground.

## Chassis

- **Strong PA** (it powers both the weapon *and* Geomancy — one stat, two registers) · decent HP · Speed
  moderate · **Move 4** · Faith **low**.
- **Faith low = magic-resistant** (`docs/deep-combat-layer/08`): incoming magic is shrugged, and the
  Geomancer's own offense is **PA-scaled, not Faith-scaled**, so low Faith costs it nothing — its
  durability lane is magic resistance, not DR.
- **Armour: Clothes & Suits** (`docs/deep-combat-layer/14`) — a **mobile frontliner**, not a Heavy anvil
  (Heavy's −Move would fight the move-to-choose-your-element loop and push it toward the Knight).
- **Off-hand fork** (1H weapon): **Shield (Block)** for a mobile bruiser with finite active defence, or
  **Doublehand** for more damage and exposure. The Knight remains the true Heavy wall.
- Movement **Ignore Terrain** — see *R / S / M*.

## Innate — Landreader (free, and exported)

The terrain sense that ties the melee fighter and the elementalist into one job: Landreader **reads the
tile the unit stands on** and lets it call that terrain's **Geomancy** variant (element + status). The
Geomancer has it **free** (innate). Per the portability rules (`docs/deep-combat-layer/15`), it is **also a
learnable Support**: an off-job running **Geomancy secondary + Landreader support** becomes a real terrain
elementalist too — without the support, off-job Geomancy cannot key to the tile (no element/status access),
so the command travels but the full package costs secondary **+** support.

The Geomancer's moat is **not** exclusivity — it is getting Landreader **free** on an **Axe-A melee
chassis** with the strong normal attack and Ignore-Terrain mobility native. A splashed Geomancy user pays
two slots and brings its own body and weapon grade; a welcome splash, never a strictly-better Geomancer
(`docs/job-balance/job-design-process.md`).

## Command — Geomancy

The **normal attack** is the melee bread-and-butter — a **strong plain weapon hit** (Axe A). No ability
below replaces it; **Geomancy is a tactical toolkit, never a "better normal attack."**

**Geomancy** is **one command**, short range (**2–3 tiles**, single-target at Core), whose effect is
**keyed to the tile the Geomancer stands on** — the vanilla "different effect per terrain" signature,
reworked to short range. Its damage is **deliberately below an adjacent normal hit**: the reason to use it
is **reach, element, or status**, not raw damage, so it is never spammed in place of attacking.

Core terrain variants (one command, terrain-expressed — grouped, not ten independent unlocks):

| Variant (by terrain) | Element | Status | Tactical use |
|------|---------|--------|--------------|
| **Magma Surge** (lava / ash / volcanic) | Fire | **Oil** | sets up a fire follow-up (its own or an ally's); status lands *after* damage — no same-action self-multiply |
| **Sandstorm** (sand / desert / dust) | Earth/Wind | **Blind** | anti-physical — blanks an archer / bruiser's accuracy |
| **Tanglevine** (grass / forest / bog) | Earth | **Poison** | attrition against a durable target |
| **Torrent** (water / marsh) | Water | — | reach / hit a water weakness |
| **Snowstorm** (snow / ice) | Ice | — | reach / hit an ice weakness |
| **Wind Slash** (cliff / high / open) | Wind | — | a height-flavoured short poke |

**Reliable status, by existing means.** The terrain both **selects** the variant and is what **grants
access** to that status — so reliability is *earned by standing on the right ground*. The base status rate
is high (a planned tactic), but it still respects **resistance and immunity** and never bypasses them — it
is **reliable, not guaranteed, not hard control**. The elemental damage is **DR-subject** (the
roster-level contrast with the Black Mage's DR-ignoring Faith magic, `docs/deep-combat-layer/11`).

**Tier-2 (costed, via unbuilt engine hooks):**

- **Scar the World** — an axe hit marks the target with a **terrain-element exposure** until its next turn
  (the team's and its own follow-ups hit that element harder). The melee, risk-paid version of a
  weakness-tag — short and visible, not a passive backline aura.
- **Worldsplitter** — a larger faultline, or a true **terrain reshape / hazard tile** (convert a tile to a
  persistent elemental hazard) if the terrain-creation hook ships. The "I command the battlefield" ceiling;
  the job is already complete without it.

## R / S / M

- **Reaction — Nature's Wrath** — when struck or targeted at close range by a visible attacker, retaliate
  with a **basic Geomancy** keyed to the Geomancer's tile (no status rider, no Scar). Melee-relevant, once
  per attacker action — the diver-punish that keeps it from being helpless up close without making it a
  Monk-grade reaction (∝Brave, minor → portable per `docs/deep-combat-layer/15`).
- **Support — Landreader** (the innate, learnable — see *Innate*): keys Geomancy's variant to the holder's
  tile.
- **Support — Axe Mastery** (weapon-proficiency export, `15`): grants **Axe A** to whatever job equips it
  (a single weapon lane per support, so a splash cannot buy the whole arsenal at once). *(Axe is a normal
  family, not an exclusive SKU, so it exports like the Archer's Bow A; the magnitude sits under the pending
  grade-budget reconciliation, `15` — Hypothesis.)*
- **Support — Sidearms** (weapon-proficiency export): grants **Sword C + Knife D**.
- **Movement — Ignore Terrain** — move freely over rough / elevation terrain. This is what makes
  "choose your tile = choose your element" real, and the offensive wind-up of the kit.

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per
slot.)*

## Equipment & weapon aptitude

Pool **7** (`docs/deep-combat-layer/15`, *Weapon aptitude*; mechanic owned by `10`). On-axis melee: its A
is real, but it spends nothing on ranged weapons (Geomancy is its reach).

| Slot | Grant |
|------|-------|
| Armour | **Clothes & Suits** |
| Off-hand | **Shield / Doublehand** (the fork; with a 1H weapon) |
| Axe | **A** — the signature: crush / swing, the bread-and-butter melee |
| Sword | **C** — a competent fallback |
| Knife | **D** — utility / emergency |

*(No bow / crossbow / gun — its ranged answer is short-range Geomancy, not a weapon. No A elsewhere: one A
below the capstone tier, `15`.)*

## Early / mid / late

- **Early.** Immediately a real melee threat (strong normal attack) with a short-range elemental answer for
  gaps — no dependence on a late weapon, no fragile dead band.
- **Mid.** The terrain reaver — read the ground, key Magma Surge → Oil for a fire combo, Sandstorm → Blind
  a shooter, Tanglevine → Poison a wall, and reach the unreachable; punish divers with Nature's Wrath.
- **Late.** A destination by matchup: a durable, magic-resistant melee fighter who turns terrain into
  tactics. Not a burst-deleter (Black) and not a wall (Knight) — the fighter the land arms.

## Battle dynamics

**What the player does with it.** Field the Geomancer as a **melee bruiser that reads the ground**: advance
with Ignore Terrain to fight from the terrain you want, **hit hard with the normal attack** (Axe A) up
close, and reach for **Geomancy** as the *tactic* — close a gap to a runner, hit an elemental weakness, or
land a planned status (Oil before a fire follow-up, Blind on a shooter, Poison on a turtle). Build the fork
to the map: **Shield** to survive a grind, **Doublehand** to hit harder. As a donor it lends Landreader
(terrain elementalist), Axe A / Sword C / Knife D (weapon exports), and — at Tier-2 — Scar the World.

**How an enemy version harms the player.** An enemy Geomancer is a **durable frontline fighter that blinds
your archers, oils your fire-weak units, and poisons your wall** while shrugging your magic (low Faith).
Counterplay: it is **melee and Clothes & Suits** — kite it (its only reach is short Geomancy), **focus-fire
the soft body** (no Heavy DR; Shield Block drains under pressure), bring **element-neutral / resistant**
units (its terrain status and element find no purchase), and deny it the terrain it wants (cramped or
uniform ground shrinks its tactics).

## Two-sided cost (why it is not strictly-better)

- **Melee / short-range exposure** — its only reach is short Geomancy; a kiting line outranges it.
- **Terrain-dependent tactics** — barren / uniform ground gives no element or status to key (it is still a
  real axe fighter, just without the upside); cramped maps shrink the move-to-choose loop.
- **Status is resistible / immune** — reliable, never guaranteed; immune enemies blank it.
- **No sustain, no hard control, no guard-shred** — it does not heal (Monk/White/Chemist), lock down
  (Archer/Chemist pins, Knight trip), or shred guard (Knight); evasive and high-Block targets are an
  Archer/Knight problem.
- **Clothes & Suits, low Parry on the axe** — it punishes divers but does not tank them forever; coordinated
  focus-fire drops it.
- **DR-subject elemental** — Heavy bodies blunt the elemental damage; the Black Mage (DR-ignoring) is the
  pick against high-DR clusters.

Distinct from **Knight** (Heavy wall / guard-break), **Monk** (self-sufficient unarmed dive / sustain),
**Samurai** (Faith-free area burst), **Dragoon** (reach / height / Jump), and **Black** (DR-ignoring
Faith burst).

## J1 — the pick / wrong-pick

- **The pick:** a melee fight on **varied terrain** vs a line with **exploitable elemental weaknesses** —
  fight up close on the ground you choose, key the element to the matchup, blind the shooters and oil the
  fire-weak, and stay standing through magic (low Faith). Rough / vertical maps reward Ignore Terrain.
- **Wrong pick (two-sided):** a **kite-heavy** fight (short reach), **barren / uniform / cramped** maps (no
  terrain to key), **element-neutral / immune** enemies (no purchase), a **pure burst race** (bring Black),
  and **high-Block / evasive** targets (no guard-shred, no anti-Dodge).
