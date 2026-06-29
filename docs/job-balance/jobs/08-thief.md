# 08 — Thief · "The Evasive Cutpurse"

The mobility/theft specialist, reframed as an **evasive tempo skirmisher**: the fastest, hardest-to-hit
body on the field, who **de-fangs dangerous weapon-users**, slips through the enemy line to strike from
behind, and is nearly impossible to corner. Rare-equipment theft survives as a deliberately **niche
loot mini-game** (as in vanilla) — not the everyday battle loop.

This doc records the design decision for the Thief on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a
  mid-tree unlock reached past a first-rank job (`docs/job-balance/00-job-tree.md`), alongside Monk and
  Geomancer.

## The vanilla problem it solves

Vanilla Thief (`docs/job-balance/vanilla/08-thief.md`) is the textbook **mine-don't-field** job: its
lasting value is the mobility it *exports* (Move +2/+3, evasion reactions) onto a better body, while
Steal deals no damage, is Speed-gated, and is unrewarding to spam. Two fixes:

1. **Steal is returned to what it should be** — a **niche, fun rare-loot tool** (prep, then grab the
   rare gear), explicitly **not** the everyday battle command.
2. **A real everyday battle kit** is built from what only the Thief has — top Speed, top Dodge,
   mobility, facing — so you **field** it for the fight, not just mine it.

## Fantasy

The cutpurse: the quickest, slipperiest body on the field. It pulls the fangs from whoever hits hardest,
weaves through the enemy formation to knife the soft target behind it, and dances out of reach.

**Role in one line:** the evasive tempo skirmisher that **de-fangs dangerous weapon-users** (Knock
Loose) and **punishes exposed positioning** (Backstab + Slip Through). Ninja kills harder, Archer
controls range, Thief disrupts.

## Chassis

- **Highest Speed** on the roster (paid by **low per-hit**) · low HP · Move 4 · **Brave LOW → best
  Dodge** (the honest evasive lane, `07`/B9) · Faith none.
- **Clothes & Suits** · **no shield** · a single **1H knife** (no dual-wield), reach-1 thrust.
- Defence = **pure Dodge** — it never depletes (unlike a shield's Block), but it gives **no defence
  against magic**, and flank/back open the unit up. Low HP means once hits land, it dies fast.

## Innate — Light Fingers (free, and exported)

The Thief's theft skill is **meaningfully better**, and it has it **free** (no support slot spent). Per
the portability rules (`15`), **Light Fingers is also a learnable Support** — any job can run
Steal-secondary + Light Fingers and steal well. The Thief's moat is **not** exclusivity; it is the
**chassis** (highest Speed → more turns and better steal prep, best Dodge → survives the grab) plus the
free innate slot. A Ninja-thief is legitimate and welcome; the Thief simply out-thieves it on stats.

## Command — the everyday battle kit

**Core (used every fight):**

- **Knock Loose** *(the signature)* — a knife maneuver that **denies the target's next weapon action**
  (battle-scoped, no loot, no permanent loss). The target still **moves, casts, uses items, punches, and
  acts** — it just can't use its weapon next time. Distinct from Knight *Bind Weapon* (suppresses
  parry/reaction, weapon stays in hand) and from the niche Steal Weapon (permanent loot). **Strong vs
  hard-hitting weapon-users** (Knight/Samurai/Ninja); **dead vs monsters / unarmed / casters** (no
  weapon) — its honest negative space.
- **Backstab** — a **single** precision strike with a flank/back bonus (not multi-hit). The Thief's real
  damage button and the facing exploiter.
- **Mug** — a knife strike that also pilfers **gil / a common item** on hit; the default useful attack
  with economy texture.

**Steal (niche — the rare-loot mini-game, not the loop):** permanent, **symmetric**, slot-specific
(Weapon / Armor / Accessory / Helm / Shield + Gil / Item), **low base chance** lifted by **preparation**
(top Speed, flank/back facing, Haste-self / Slow-target), capped **below certainty**. It is the tool to
*prepare for* and grab the rare gear you want — not a button you press every fight.

**Tier-2:**

- **Snatch / Pilfer Boon** — steal **one** positive status (Haste/Protect/Shell/Regen) off an enemy and
  take it for the remaining duration. It does **not generate** the buff (no overlap with Time/White),
  only robs a buffed enemy. Excellent when live, **dead vs an unbuffed team** → correctly situational.
- **Disarm** — the full version: knock the weapon to the ground for the fight (the target punches/casts/
  uses items, or spends a turn to recover it). Tier-2 because it needs a recover/re-equip primitive. It
  stays fair because **basic item use is universal** (see *Open*), so a disarmed unit is never dead
  weight.

## R / S / M (a set, not one of each)

- **Reaction — Vigilance** — when targeted by an attack it can see, raise its own Dodge for that attack.
  Guardrail: **once per Thief turn-cycle**, no self-stack. An FFT-native evade reaction (no
  reaction-movement, which the engine cannot do).
- **Support — Light Fingers** — the theft edge (innate to the Thief; exported here per the portability
  rule).
- **Support — Knife proficiency export** — grants the Knife family at the Thief's grade (B) to whatever
  job equips it (the mandatory weapon-proficiency export, `15`; packaged as the single signature
  family).
- **Movement — Slip Through** *(new — not in vanilla)* — the Thief may **move through tiles occupied by
  enemies** (as it already passes through allies), but **cannot end** its move on an occupied tile.
  Normal Move range, no teleport, **no terrain/height bypass** (that stays the Dragoon's lane), and it
  **cannot pass a Bulwark / hard-obstacle state** (the Knight still holds a line). It is the "slip the
  formation" tool that reaches the backline and sets up Backstab.

## Equipment & weapon aptitude

Pool **6** (`docs/deep-combat-layer/15`, *Weapon aptitude*; mechanic owned by `10`).

| Slot | Grant |
|------|-------|
| Armour | **Clothes & Suits** |
| Off-hand | — (single 1H knife, no shield, no dual-wield) |
| Knife | **B** — the signature (thrust, top Speed grant, the Backstab/Mug tool) |
| Ninja Blade | **C** |
| Sword | **D** — fallback |

*(Light pool — the Thief wins on Speed/utility, not per-hit; **no A in any weapon by design**.)*

## Battle dynamics

**What the player does with it:**

- **vs hard-hitting weapon-users** (Knight/Samurai/Ninja/Dragoon): Knock Loose pulls the fangs of the
  most dangerous attacker on a key turn, then the Thief dances out of reach and flanks with Backstab.
  This is its home.
- **vs the backline** (casters/archers): Slip Through weaves past the front line → Backstab the squishy.
  High-risk (it is exposed and dies in ~2 rounds if surrounded), high-reward (it chips down or drops a
  caster). Best when a frontline ally holds aggro while it operates behind.
- **vs a buffed enemy:** Snatch robs the advantage (and gains it).
- **Best allies:** burst that capitalises on a de-fanged / opened target; a Knight who holds the line so
  the Thief can work the flank and the backline.
- **Terrain:** wants open / vertical maps to dance; struggles in a tight chokepoint with no flank.
- **Wrong pick (two-sided):** an **all-monster / all-caster** enemy comp (Knock Loose dead, no weapon to
  pull, no easy flank), and a stand-up DPS race (low per-hit). Clothes & Suits + reach-1 fold to burst
  and magic.

**How an enemy Thief harms the player:** it **steals the player's rare gear permanently** (a telegraphed
encounter-design tool — see *Open*), **Knock Looses the player's heavy hitter** on a pivotal turn,
**dives the player's White/Black Mage** via Slip Through + Backstab, and is **frustrating to hit** (top
Dodge). **Counterplay is clear and legible:** focus-fire it (Dodge never depletes, but HP ~85 falls in
~4 hits); attack it **from behind or with magic** (both ignore Dodge); use a **Knight Bulwark** to block
Slip Through; do not leave a caster exposed; and punish the dive (it trades its life for your backliner).

## Why it is a destination, not a donor

Under the portability philosophy (`15`, `job-design-process.md`) the Thief's kit is *meant* to travel —
Light Fingers, the knife export, Snatch and Slip Through can all be splashed. It stays worth maining
because the **chassis is the moat**: highest Speed and best Dodge (non-portable) make it the best home
for theft and evasion, and the free innate frees a support slot. An off-job thief is welcome and never
strictly better.

## Early / mid / late

- **Early.** Already an evasive flanker (Mug/Backstab) with Knock Loose against the early bruisers.
- **Mid.** The disruptor: pull the dangerous attacker's fangs, weave to the backline, cross open maps
  (Slip Through), and prep the rare steals you want.
- **Late.** A destination, not a donor — the un-cornerable disruptor; Snatch robs buffed elites; it still
  loses a stand-up DPS race, by design.

## J1 — the pick / wrong-pick

- **The pick for:** enemy comps with **dangerous weapon-users** (Knock Loose neutralises them), exposed
  targets to flank, backlines to dive, open maps to cross, and rare-loot runs.
- **Wrong pick:** **all-monster / all-caster** comps (Knock Loose dead, no flank), stand-up DPS races
  (low per-hit), and burst/magic into its Clothes & Suits + reach-1 fragility.

## Open (decided / pending)

- **Enemy-steal — decided (symmetric & permanent).** Steal works the same for both sides; enemy Thieves
  can permanently take the player's gear. This is balanced as an **encounter-design tool** — visible,
  telegraphed, deliberate, and rare — not a routine threat. (Confidence: Strong — per the design owner's
  directive that Steal is a legitimate two-way mechanic, unlike Rend.)
- **Universal basic-item floor — pending (cross-job, touches `jobs/02-chemist`).** Basic consumable use
  (self/adjacent, action-costed, inventory-consuming, no range) is intended to be universal so a
  weapon-denied unit is never dead weight; the Chemist keeps its identity via advanced/ranged/offensive
  alchemy. Not yet ratified.
- **Grade-budget reconciliation — pending (`15`).** The weapon-proficiency export now brings the source
  grade; the budget's "no job good at everything" guarantee needs a dedicated balance pass.
