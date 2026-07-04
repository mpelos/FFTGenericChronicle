# 12 — Dragoon · "The Sky-Piercer"

The dragon-knight, rebuilt as a **commitment striker who owns the vertical**. On the ground it is a slow
plate body; its threat is the **leap** — it vaults over the battle line in heavy armour to come down on the
soft target the front rank was protecting, then holds a choke at spear's length. It is no longer the
end-game footnote you raid for a Reraise reaction; it is a real combatant whose lane is the one axis no
other Heavy job contests — **the sky**.

This doc records the design decision for the Dragoon on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier A.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a deep,
  pre-capstone unlock (`docs/job-balance/00-job-tree.md`) reached past mid-tree martials. It is authored as
  a peer to the late martials it sits among — never a weak end-game curio (the vanilla Lancer's fate).

## The vanilla problems it solves

Vanilla Dragoon (`docs/job-balance/vanilla/12-dragoon.md`) is a one-trick unit whose trick you can't aim
and whose real value is mined off it. Four concrete feel-bad problems are fixed, each mapped to a design
move:

1. **A dead progression ladder.** Vanilla's whole skillset is *Jump +N* — each rank a longer ruler that
   obsoletes the last, so only the final unlock is ever worth buying and every earlier purchase is wasted JP.
   **Fix:** the leap's reach/height is **one innate** (Aerial Training), not a ladder; the *learnable* skills
   (Skewer, Dragon's Fury, the Tier-2 lancer abilities) are each a **distinct tool**, never "+1 range" again.
2. **An un-plannable landing.** Vanilla Jump's airtime is Speed-derived and effectively opaque — neither side
   can plan around when it lands. **Fix:** a **fixed, visible charge** (it charges like a spell, with a
   readable CT, `docs/deep-combat-layer/12` — *Strong*); the landing is plannable for both sides.
3. **Mined, not fielded.** The vanilla draw is **Dragonheart (Reraise)** splashed onto other jobs; the body
   itself is a weak end-game tier-C. **Fix:** cut Dragonheart (Reraise belongs to the healer / item lanes,
   not a martial reaction-donor); make the **chassis** worth fielding — over-the-line reach and reach-2 zone
   control that no other Heavy job offers.
4. **A leap whose only value was damage you could walk out of.** Vanilla Jump's payoff was raw damage on a
   target that simply steps away, on a timer you can't read — so it rarely justified a whole turn aloft.
   **Fix:** the airborne turn now buys a **spatial** advantage — it **reaches over the line / terrain** onto
   a target a ground attack can't reach, and the Dragoon is **untargetable mid-air** — so the commitment pays
   even though the strike still rolls defence like any attack.

## Fantasy

The sky-piercer: a knight in full plate who treats the third dimension as a weapon. It leaps above the
shieldwall and comes down on the caster, the archer, the soft body that thought the front line protected it —
then sets its spear and dares the line to close. Earth-bound it is heavy and slow; in the air it is out of
reach, and where it lands the front rank cannot help.

## Chassis

- **High PA** (it powers the spear and the crash) · **HP ~140** · Speed **moderate** · **Move 3** · Faith
  **neutral**.
- **Armour: Heavy** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the Dragoon is a plate
  knight; the mod draws no new art, so the chassis must match the art). This is also the design: the Dragoon
  is a **plate anvil that flies**, not an agile light leaper (that lane would collide with the Thief and
  contradict the sprite).
- **Faith neutral = the deliberate two-sided counter.** Plate DR makes it tanky vs physical, so its weakness
  must live elsewhere: at neutral Faith it takes **full magic damage** (`docs/deep-combat-layer/08`) — magic
  is the clean answer to the flying anvil (TTK ~2.5 vs ~8.4 physical, SIM 4). Low Faith would over-armour it
  and erase the counter.
- **Two-handed spear, no shield** — it forgoes **Block** (the Knight keeps the shield wall,
  `docs/job-balance/jobs/03-knight.md`). Its defence is HP + plate DR + *being airborne*, not a raised guard.
  High Brave compounds this: strong physical and a real physical reaction, but **weak Dodge** — grounded, it
  is easy to hit.
- Movement **Ignore Height** — see *R / S / M*.

## Innate — Aerial Training (free, and exported)

The training that makes the leap a weapon: Aerial Training grants the Jump command its **full reach and
height** — how far across the field and how many tiles of elevation the Dragoon can vault and still come down
on target. It is **one quality**, not a +1..+7 ladder (the vanilla mistake). The Dragoon has it **free**
(innate).

Per the portability rules (`docs/deep-combat-layer/15`), it is **also a learnable Support**. The **Jump
command** itself travels (any job can slot it as a secondary), but **without Aerial Training that off-job
leap is a short, low hop** — only the full-reach, full-height leap needs the support. So an off-job leaper
pays **two slots** (Jump secondary **+** Aerial Training support) for what the Dragoon gets native, and still
brings its own body and PA. The reduced off-job reach is **surfaced in the targeting preview** (legibility:
the player sees the shorter leap, never a silent nerf). This parasitic-innate-export pattern is the same one
the Geomancer's Landreader uses (`docs/job-balance/jobs/11-geomancer.md`): the support is the *key* that
unlocks the secondary command's full function, not a stand-alone effect.

The Dragoon's moat is **not** exclusivity — it is getting Aerial Training **free** on a **plate, high-PA,
Spear-A** chassis with the primary command slot open. A splashed leaper is a welcome splash, never a
strictly-better Dragoon (`docs/job-balance/job-design-process.md`).

## Command — Jump

The **normal attack** is the reach-2 bread-and-butter — a strong plain **spear thrust** at one or two tiles,
immediate, that rolls defence like any attack. No ability below replaces it; **the Jump kit is reach and
spacing, never a "better normal attack."** (Two normal thrusts out-damage one Jump and cost no telegraph —
`simulations/`, SIM 1.)

**Jump** (Core) — the signature. The Dragoon vaults to a marked tile and crashes down after a fixed,
readable charge as a **single-target** spear impact, ~1.4× a standing thrust on the target. What makes it
worth a whole turn aloft is **spatial**, not a defence-bypass:

- **It reaches over the line.** It strikes a tile a ground attack cannot — behind the front rank, across a
  gap, up or down a cliff — landing on the caster/archer the shieldwall was guarding (`docs/deep-combat-layer/06`,
  the reach-2 *outrange* identity). This is its niche: **delivery**, not penetration.
- **The Dragoon is untargetable mid-air.** The leap is also a defensive beat — for the charge, the unit is
  off the field and cannot be hit.
- **It still rolls defence, and still respects DR.** Dodge / Parry / Block all apply, and armour absorbs it
  normally (`docs/deep-combat-layer/04`). So it is **anti-soft, not anti-plate** — it deletes a boxed-in
  **low-Dodge** caster (SIM 2) but **whiffs a high-Dodge target** (~35% connect) and only **chips** a Heavy
  body. Beating Dodge is the **Archer's** paid job (Concentration, which *explicitly excludes Jump*,
  `docs/job-balance/jobs/04-archer.md`); breaking Block/Parry is the **Knight's** (Guard Break,
  `docs/job-balance/jobs/03-knight.md`). Jump deliberately does **neither** — defence-bypass is rationed and
  slot-costed across the roster, and a free baseline leap may not be the un-priced union of both.

Counterplay is layered and available: the target **rolls its defence** (a high-Dodge unit often evades it),
**or relocates** off the marked tile before the fixed-CT landing, **or** the team punishes the Dragoon on the
ground between leaps (slow, no Block, weak Dodge). The break is **timing and placement** (catching a soft,
low-Dodge target that is boxed in), not luck and not an unanswerable hit.

**Skewer** (Core) — a line-2 spear impale that hits the unit **directly behind** the target as well, ~65%
damage to **each** (SIM 1, SIM 5). It rolls defence and carries **no status riders** — a clean
**formation-punish** for units lined up behind each other, deliberately **under** the Samurai's wide
Faith-free area burst: a spear's line, not artillery.

**Tier-2 (costed; via unbuilt engine hooks):**

- **Stop-hit** — the lancer's spacing punish: a **free strike on an enemy that enters the spear's reach-2
  band** (`docs/deep-combat-layer/06` reserves stop-hit for *lancer abilities* specifically). It makes the
  "keep you at 2" zone real — a rusher eats a thrust on the way in, then is adjacent where the spear is
  clumsy (the spacing duel, paid by the rusher). Needs the delayed-trigger machinery the Archer's Overwatch
  also waits on (`docs/job-balance/jobs/04-archer.md`), so it lands later.
- **Dragon Dive** — the dramatic ceiling: an **area** crash (full centre + ~30% to adjacent tiles) whose
  distinction is the **delivery vector** — dropped from above, **over the front rank / obstacles**, onto a
  back cluster a front-delivered burst can't reach. It is **evadable** (every unit in the zone Dodges or
  relocates) and **longer-telegraphed**, so it is neither a strict upgrade of single-target Jump nor a
  strict-worse Samurai burst (SIM 3). The job is complete without it.

## R / S / M

- **Reaction — Dragon's Fury** — when a visible attacker is within **reach 2**, retaliate with a
  **counter-thrust** of the spear. It **obeys the reach rules** (`docs/deep-combat-layer/06`): a clean punish
  against a reach-2 attacker, but **penalised at point-blank** against an adjacent rusher — so *getting inside
  the Dragoon blunts its counter too*, reinforcing "rush the spearman" rather than breaking it. It is
  **portable** but **naturally gated** (only good on a body with a spear, the reach to use it, and the Brave
  to power it, ∝Brave, `docs/deep-combat-layer/08`) — a splash brings a weak version, not a free
  auto-include. A modest keep-away, not the job's main draw. (It replaces Dragonheart deliberately — a
  fielded zone-holder's reaction, not a mine-able Reraise.)
- **Support — Aerial Training** (the innate, learnable — see *Innate*): grants the full-reach/height leap to
  whatever job pairs it with a Jump secondary.
- **Support — Polearm Training** (weapon-proficiency export, `15`): grants **Spear A** to whatever job
  equips it (a single weapon lane per support). *(Spear is a normal family, not an exclusive SKU, so it
  exports like the Archer's Bow A; magnitude sits under the pending grade-budget reconciliation, `15` —
  Hypothesis.)*
- **Movement — Ignore Height** — vertical traversal: ignore elevation costs/limits when moving. The
  ground-game complement to the leap — the Dragoon owns the third dimension whether it walks or vaults.

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per
slot.)*

## Equipment & weapon aptitude

Pool **7** (`docs/deep-combat-layer/15`, *Weapon aptitude*; mechanic owned by `10`), spent **lean** — a
focused specialist, not a generalist:

| Slot | Grant |
|------|-------|
| Armour | **Heavy** (sprite-mandated) |
| Off-hand | **none** — the spear is **two-handed** (no shield / no Block) |
| Spear | **A** — the signature: thrust, reach 2, the Jump weapon |
| Sword | **D** — emergency fallback only |

*(No bow / crossbow / gun — its reach is the **leap**, not a ranged weapon. One A (Spear), per "one A below
the capstone tier", `15`. The underspent pool is deliberate: identity over breadth.)*

## Early / mid / late

- **Early.** Already a real plate combatant — a strong reach-2 spear and a leap that reliably reaches and
  deletes a boxed-in soft target. No dependence on a late unlock, no dead JP ladder.
- **Mid.** The zone-holder: leap over the line onto the caster the front rank was protecting, Skewer two
  enemies stacked in a corridor, hold the choke with Dragon's Fury, and use Ignore Height to fight from
  ground others cannot reach.
- **Late.** A destination by matchup: the unit that **reaches an entrenched backline** the team can't
  otherwise touch, and controls space with the spear. Not a wall (Knight), not an area-burster (Samurai) —
  the one job that attacks along the **vertical**.

## Battle dynamics

**What the player does with it.** Field the Dragoon to **reach a protected target**: read the turn order,
mark the **Jump** onto a soft, low-Dodge backliner who is boxed in (or has already acted and can't relocate),
and crash over the line where a ground attack can't follow — then set the spear and **hold the choke** with
reach-2 normals and Dragon's Fury. Use **Skewer** when the enemy stacks in a line, **Ignore Height** to take
ground others can't, and at Tier-2 **Stop-hit** to punish rushers / **Dragon Dive** to drop on a back
cluster. As a donor it lends **Aerial Training + Jump** (the over-the-line leap), **Spear A** (Polearm
Training), and the reach-2 counter — but every splash pays slots and brings a softer, ground-bound body.

**How an enemy version harms the player.** An enemy Dragoon **goes over your shieldwall** — it lands on the
casters and archers you tucked behind the front rank, and shrugs your physical front with plate DR.
Counterplay is clear and multi-layered: **keep soft units out of leap range or unboxed** so they can roll
Dodge or relocate (high-Dodge units evade it outright — it does **not** beat Dodge); **burn it with magic**
(neutral Faith, no DR vs spells, TTK ~2.5) or **crush** (`docs/job-balance/jobs/07-monk.md`; plate covers
neither); and **catch it grounded** between leaps, where its weak Dodge and absent Block make it easy to
focus. Deny it height/space and it is a slow anvil.

## Two-sided cost (why it is not strictly-better)

- **Soft to magic and crush** — neutral Faith means full magic damage (the clean counter to plate); crush
  bypasses what plate is good at. A burst caster or a Monk punishes it hard.
- **Grounded exposure** — no shield (no Block), weak Dodge (high Brave), Move 3: between leaps it is slow and
  easy to hit. Its safety is *being in the air*, which it cannot be every turn.
- **The leap rolls defence and is avoidable** — it does **not** bypass Dodge/Parry/Block (that is the
  Archer's and Knight's rationed, slot-costed job); a high-Dodge target evades it, and any unit with a turn
  relocates off the marked tile and wastes the airborne turn entirely.
- **Anti-soft, not anti-plate** — the spear's thrust is eaten by Heavy DR; vs a plate cluster it only chips,
  and the team wants the Black Mage (DR-ignoring, `docs/deep-combat-layer/11`) or a crusher instead.
- **Point-blank weakness** — the reach-2 spear (and Dragon's Fury) is penalised against an adjacent foe
  (`docs/deep-combat-layer/06`): rushing inside is the counter to the Dragoon.
- **Single-target-ish, no sustain, no hard control, no guard-shred** — base Jump hits one; it does not heal,
  lock down, or break guard (Knight). Dragon Dive's area is a telegraphed, evadable Tier-2 ceiling.

Distinct from **Knight** (Heavy Block wall / guard-break — breaks Block/Parry, never Dodge), **Samurai**
(Faith-free wide area burst), **Geomancer** (terrain melee), **Thief** (slips *through* a line via
Dodge/facing — the Dragoon goes *over* it), and **Archer** (ranged precision; it, not the Dragoon, is the
roster's anti-Dodge answer).

## J1 — the pick / wrong-pick

- **The pick:** an enemy line that **shelters a soft, low-Dodge backline** (casters/archers behind a front
  rank) you can't reach on the ground — leap over the wall and delete what hides behind it, then hold the
  choke. **Vertical / tiered maps** reward Ignore Height and the leap.
- **Wrong pick (two-sided):** a **magic-heavy** enemy (neutral Faith, no DR vs spells), a **plate cluster**
  (thrust only chips — bring Black/crush), a **high-Dodge** roster (the leap whiffs — that is the Archer's
  job, not the Dragoon's), **open ground with no protected targets** (nothing to leap *over*), a **mobile**
  enemy that relocates off every marked tile, and a **flat, cramped** map that denies the height game.
