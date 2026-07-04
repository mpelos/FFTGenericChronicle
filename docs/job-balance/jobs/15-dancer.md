# 15 — Dancer · "The Phase Dancer"

The performer rebuilt around a **phase cycle** instead of map-wide corner cheese. Its texture is the
commitment window: *"I dive into the crowd and dance — untouchable while the show lasts, condemned the
instant it ends."* All seven vanilla dances return **by name**, but they are no longer free accumulating
ticks fired from safety: each is a **charged, radius-2, audience-scaled** performance, and the audience she
gathers is both her **payoff** and her **shield**. She is **high risk / high reward** — a Tier-A skill-ceiling
job meant to stand beside Samurai and Ninja, not a corner attrition bot.

This doc records the design decision for the Dancer on the Deep Combat Layer (DCL). Mechanics it leans on are
owned by the DCL docs and cross-referenced inline; numbers are calibration (`docs/deep-combat-layer/12`).
Method: `docs/job-balance/job-design-process.md`. The Bard (`docs/job-balance/jobs/16-bard.md`, unbuilt) is
its mirror and shares its R/S/M records exactly (the gender-parity law, below).

## Tier & tree position

- **Tier A.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a **deep**
  unlock, the same acquisition depth — and therefore the same power-and-fun bar — as Samurai and Ninja
  (`docs/job-balance/00-job-tree.md`; the performers keep their vanilla tree position, pulled active and
  forward). It earns its keep as a primary (the phase-cycle dive) and as a donor (the **Stage Grace** export;
  see *R / S / M*).

## The vanilla problems it solves

Vanilla Dancer (`docs/job-balance/vanilla/15-dancer.md`) is **cheese-tier attrition**: map-wide,
unmissable, free, accumulating enemy debuffs mined from a safe corner. It is brutal only in long fights,
dead in short ones, passive, and boring. Four concrete problems, each mapped to a design move:

1. **It is fired from safety and never at risk.** **Fix:** dances become **radius-2, self-centred, and
   charged** — she must stand **in the crowd**, and the audience she needs is the same crowd that threatens
   her. Corner play is gone (SIM 1, SIM 7).
2. **Payoff is a slow, unmissable ramp.** **Fix:** the **phase cycle** — magnitude scales with the audience
   **at resolution**, and every cycle ends in a **Curtain Call** exposure window. The dance is a committed,
   telegraphed, counter-playable event, not a free tick (SIM 2, SIM 5).
3. **Fragility makes it a non-combatant.** **Fix:** the innate **Stage Grace** — while she dances, the crowd
   *is* her shield (audience-scaled physical evasion); the **Cloth** weapon's best-in-class parry covers her
   in transit. Her protection is the **performance**, never the body (SIM 11, SIM 12).
4. **The seven dances read as flavours of the same ramp.** **Fix:** each keeps a **distinct board-trigger**
   and a **heroic, risk-premium** number — a real reason to pick *this* dance *this* turn (SIM 7, SIM 8).

## Fantasy

The performer **dives into the thick of it** and dances. While the show lasts she is a whirling blur — a ring
of enemies around her is a ring of missed swings — and the fuller the house, the more devastating the dance
that lands. But a dance is a commitment: she is telegraphed while it charges, and for one beat after it
resolves she is caught mid-bow, defenceless, and a single blow can end her. The skill is reading the enemy
turn order, choosing the moment to commit, and choosing which of seven dances the board is asking for. She is
never the safe support — she is the dazzling, fragile daredevil in the centre of the stage.

## Chassis

- **Cloth** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the performer; the mod draws no new
  art). A light, no-DR chassis whose defence is **avoidance**, never mitigation.
- **HP ~60 (fragile)** · **HIGH Speed** · **Move 4 (high)** · neutral **Brave / Faith**. Fragile *normally*
  is deliberate: the protection is the dance-phase, so the body must stay soft, or the phase risk is fake.
- **High Speed + Move** is load-bearing, not flavour — the dive-in / cycle-out is her whole survival, and it
  is her **own** movement only (no forced movement of any unit exists in this engine, `docs/deep-combat-layer`
  reach/positioning; see also the roster rule that killed forced-movement kit ideas).
- **The weapons are Cloth and Bag** — the performer families (`docs/deep-combat-layer/14`; see *Equipment*).
  Cloth is the defensive/reach identity; Bag is the close-range utility contrast.

## The phase cycle (the core loop)

Every dance is a **charged** action; the cycle is the same for all seven:

1. **Enter** — she uses her own Move (high Speed / Move 4) to reach a crowd; radius-2 means enemies within two
   tiles are her **audience** (cap 3). Dancing at **one** enemy is deliberately not worth it — low payoff and
   near-no shield (SIM 1, SIM 4).
2. **Charge** — she visibly dances. **Stage Grace** is live: audience-scaled physical-contact evasion,
   re-checked **at each incoming swing** against the attacker's **live** audience, so enemies leaving mid-dance
   drop her defence in real time. Normal dances charge **short**; **Last Waltz** charges **long** (SIM 4).
3. **Resolution** — the effect lands on enemies within radius 2, magnitude scaled by the audience **at
   resolution** (scatter during the charge shrinks or fizzles it). Audience 0 = the dance fizzles.
4. **Curtain Call (the exposure window)** — from resolution until her next turn she has **zero performance
   evasion and no active defence** (caught mid-bow). A blow here is a full hit; on a ~60-HP body it is often
   lethal. This window is the structural cost, and an **invariant**: no tempo tool (Haste / Quick / Short
   Charge / CT alignment) may erase it (the mirror of the Telegraph Invariant, `docs/job-balance/jobs/10-time-mage.md`),
   and short normal charges make it pulse **every cycle** (SIM 2, SIM 5).

**Why this is not an unkillable blender:** the safety is **phased**, not continuous. A melee-only pack still
beats a Dancer who just cycles in place — the exposure window bleeds her out in ~2–3 cycles — and wanting a
full house *guarantees* enemies with turns inside that window (SIM 3). **Projectile physical (arrows, bolts,
guns), magic, and status all bypass** Stage Grace entirely (SIM 4): ranged and casters are hard counters, and
guns keep the Orator's anti-evasion lane intact. Status (Sleep / Stop / Don't-Act / KO) cancels a dance
outright; ordinary **damage does not** cancel it (the FFT charge convention — a lucky chip must not delete her
whole investment *and* feed the window, the double-punish, SIM 9).

## Command — "Dance" (the seven, kept by name)

All seven vanilla dances return, DCL-adapted: **radius 2, charged, audience-scaled (ladders 1 / 2 / 3)**. The
numbers are **heroic** by design — see *Risk-premium pricing*. Each has a distinct board-trigger (SIM 7):

| Dance | DCL effect (placeholder ladder 1 / 2 / 3) | When *this* dance is the turn |
|-------|-------------------------------------------|-------------------------------|
| **Mincing Minuet** | Physical-performance damage, **8 / 16 / 24** per target | The damage default; a full house clears a low pack in two cycles (J6, SIM 6) |
| **Polka** | Physical **offence sap** (outgoing-damage layer), **10 / 18 / 25 %**, until the target acts, non-stacking | A physical pack about to act — blunt a quarter of the incoming round |
| **Heathen Frolic** | Magic offence sap (Polka's mirror, same caps) | A caster pack about to act — blunt the magic round |
| **Slow Dance** | **CT setback**, **−10 / −20 / −35** per target (one-shot pulse; no Stop, no persistence) | A clustered, near-turn pack — steal a whole turn from the group |
| **Witch Hunt** | **MP burn**, **12 / 24 / 35** per target (she gains nothing) | A caster board — kill a charging expensive cast, gut small pools |
| **Forbidden Dance** | Random **soft** status from {Blind, Silence, Slow, Poison}, **35 / 55 / 75 %** per target | Chaos when *any* result helps — a full house erupts ~2+ statuses |
| **Last Waltz** | The capstone: **long** charge, big damage pulse, up to **~40** per target | They cannot or will not scatter — the epic punish |

**The floor:** the **Cloth basic strike** (reach 2) is a real, if modest, turn on an audience-0 board — never
a dead turn (`docs/deep-combat-layer/15`, every job a reliable button; the Dancer's plan is the dances).

### Risk-premium pricing (the lane principle)

The Dancer's numbers are **priced above** the specialists' per-action value, because she pays what no
specialist pays — a melee dive, a charge, and the Curtain Call. **Lane protection comes from mechanism +
delivery + risk, never from weak numbers** (this principle is general and is recorded for the roster). So:

- **Slow Dance vs Time Mage** (`docs/job-balance/jobs/10-time-mage.md`): her −35 CT × a cluster is a *bigger*
  tempo blow than Time's single-target Slow, but it is a **one-shot pulse** paid in melee — no Stop, no Haste,
  no Quick, no persistence, no safe range. Time keeps the **reliable, ranged, persistent clock suite**. There
  is no perma-stagger: enemies still act during her charges and her window pulses every cycle (SIM 3, SIM 8).
- **Forbidden Dance vs Mystic** (`docs/job-balance/jobs/09-mystic.md`): a full house erupts ~2+ statuses — real
  chaos, never "missed all three" — but they are **random and soft** (the hard-control lottery, Stop / Sleep /
  Stone / Charm / Death / Toad, is **excluded**). Mystic keeps **choosing** the affliction — including the hard
  ones — reliably, at range, on the right target (SIM 8).
- **Witch Hunt vs Mystic drain**: it **burns** MP (she gains nothing); Mystic's Invigoration keeps
  **drain-to-self**. It kills a charging cast or guts a small pool; large pools survive (SIM 8).
- **Mincing Minuet / Last Waltz vs Black Mage** (`docs/job-balance/jobs/06-black-mage.md`): her totals
  (~72 / ~120 at a full house) stay **below** Black's nuke; her premium is **repeatable access at melee
  risk**, not out-nuking the sniper (SIM 8).

### The earned-premium rule (the anti-farm lock)

The heroic numbers are **earned by real exposure**. If a **hard risk-deletion** layer protects her during the
dance or the Curtain Call — **Golem** (`docs/job-balance/jobs/14-summoner.md`), Cover-style substitution, full
invulnerability, or a ward that blanks one of her bypass lanes — **every dance payoff caps at the audience-2
band**. Without this, a warded party farms the aud-3 premium risk-free (two safe aud-3 Forbidden Dances put
~82 % of a pack under status); the cap collapses that to ~51 % (SIM 8). **Partial mitigation stays legal**
(armour DR, Protect, Shell, ordinary healing) — the red line is hard risk *deletion*. It is one legible rule
tied to a **visible** party state, not a hidden counter.

## Innate — Stage Grace (the command carries the cost, the innate carries the shield)

**The performance evasion is the innate.** The asymmetry is the whole design: the **Dance command** always
carries the **cost** (the charge and the Curtain Call travel with the dances onto any body), while **Stage
Grace** carries the **shield** — the audience-scaled physical-contact evasion (a placeholder **30 / 70 / 92 %**
ladder), live only while performing a Dance, decaying with the live audience, bypassed by projectiles / magic /
status, and gone entirely during the Curtain Call.

Per the portability rules (`docs/deep-combat-layer/15`), Stage Grace is **one collapsed package** — the innate
*and* the learnable **Support** — and it is **desirable for its own sake** (any body that dances needs it,
period). It is **export-clean by construction**:

- It procs on **Dance actions only** (not "charged performances" in general) — so it needs the Dance command to
  matter, and the **Bard does not get evasion by singing** (his lock holds).
- Exported **without a performer weapon** (Cloth / Bag), it **caps at the audience-2 band** — closing the
  HP-host abuse (a ~110-HP Monk with Dance-secondary + uncapped Stage Grace would out-dance the Dancer by body
  alone; the cap kills it, SIM 12).
- **Heavy armour suppresses it** anyway (B10, `docs/deep-combat-layer/14`) — a plate dancer gets nothing.

The Dancer stays the best host through **chassis** (Speed / Move / Cloth parry), not through a rule.

## R / S / M — identical records with the Bard (the gender-parity law)

The Dancer is **female-only** and the Bard is **male-only** — the one accepted generic gender restriction. By
mandatory rule their **reaction / support / movement records are field-identical**, so no gender is locked out
of a global build piece (`docs/job-balance/jobs/16-bard.md`, when built; enforce mechanically, not by prose).
Action commands differ (Dance vs Sing); the R/S/M set does not.

- **Reaction — Earplugs.** Narrow anti-performance / speech / morale defence — the counter-performer tech
  (enemy Dancers and Bards are real threats now). Narrow scope; never broad status immunity.
- **Support — Stage Grace.** The signature innate export (Dance-action-gated; caps at the audience-2 band
  without a performer weapon; heavy armour suppresses it).
- **Movement — Performance Step** (display name may keep the vanilla "Fly"). **+1 Move and ignore height** on
  her **own** movement — the dramatic stage entrance. **No** pass-through of occupied tiles, ZoC/screen bypass,
  or hover/terrain-immunity package — so it does not eat the **Thief's** slip-through-lines lane
  (`docs/job-balance/jobs/12-thief.md`) or become the universal movement export.

*(**Encore** — a post-resolution CT-cut Support — was **cut**: it would erase the scatter/telegraph
counterplay after the opener and break harder on the Bard's safer songs. Any CT-smoothing belongs in
job-specific tuning, not an exportable Support. R/S/M is a **set**: one reaction, one+ support, one movement —
equip one per slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; families owned by `docs/deep-combat-layer/14`):

| Slot | Grant |
|------|-------|
| Armour | **Cloth** (sprite-mandated; light, no-DR, avoidance chassis) |
| Weapon | **Cloth** (the defensive curtain) or **Bag** (the utility contrast) |
| Off-hand | light only (the chassis is a performer, not a shield-fencer) |

**Cloth — the defensive curtain.** The vanilla Cloth's high W-EV (the silk lineage) becomes its DCL identity:
it is the game's **only 1-handed reach-2 weapon and its best 1-handed parry** (Spear and Pole are the other
reach-2 options, both 2H). It is a **fencing ribbon, not a damage stick** — low `wmod`, impact ×1. Its parry
is the survivability lever in **transit** (entering the crowd, cycling between dances) and against projectiles
/ out-of-audience attackers, but it is **exhaustible and harsh** — a placeholder **50 % first swing → 25 %
second → 0 % after**, resetting on her turn — so four incoming swings still land ~49 expected on a 60-HP body
(survivable with skill, not a tank; a non-depleting 50 % parry was **rejected** as too safe, SIM 11). It is
**suppressed during the Curtain Call** (no active defence mid-bow), it does **not** stack with Stage Grace
against audience contact attacks (one or the other, never both), and **guns bypass** it.

**Bag — the utility contrast.** Reach-1, low damage, the close-range option. It grants **flat debuff riders**
on the debuff dances (small CT / MP / sap / status bonuses) — it **never fakes an audience band** (audience is
the price; a band-fake would be a ~33 % risk discount on the premium tier, **rejected**, SIM 11). Cloth-reach
vs Bag-utility is the weapon build fork; neither improves damage **and** debuff magnitude together.

## Early / mid / late

- **Early.** A real daredevil from the start: dive a loose pack, short-charge Minuet, let Stage Grace and the
  Cloth parry carry the cycle. The basic strike covers audience-0 boards; no late-unlock dependency (J6, SIM 6).
- **Mid.** The phase game proper: read the enemy turn order, pick the dance the board asks for (damage, a sap
  before their round, a tempo blow on a cluster, MP denial on a caster line, or Forbidden chaos), and time the
  commitment so the Curtain Call does not land you dead.
- **Late.** The **Last Waltz** as a battle event — the long charge in the centre of the enemy, where both
  counterplays (scatter during, punish after) are on the table — and **Stage Grace** as the export hub for any
  Dance-secondary build across the roster.

## Battle dynamics

**What the player does with it.** You pilot a fragile daredevil: read the AT list, dive with your own Move
into a crowd of three, and commit a charged dance — untouchable to their melee while it winds up, choosing
which of seven effects the board needs, then cashing at resolution and getting out before the Curtain Call
kills you. Against a full house the numbers are heroic; against one enemy she is weak and unshielded. As a
donor she lends **Stage Grace** to Dance-secondary builds; she never lends the dances' cost-free.

**How an enemy version harms the player.** An enemy Dancer in your cluster is a whirling, near-unhittable
hazard erupting saps, tempo blows, MP burn, or status chaos across your bunched line. Counterplay is rich and
reliable: **spread out** (radius 2 wants you bunched, and she has no way to pull you back), **shoot her**
(projectiles, magic, and status all bypass her evasion), **rush and cluster on her exposure window** (read
*her* CT — the Curtain Call is a guaranteed opening on a ~60-HP body), and **note her weapon** (guns beat the
Cloth parry too). The charge is a readable telegraph; the punish window is always there.

## Two-sided cost (why it is not strictly-better)

- **Fragile and phase-gated** — ~60 HP, no DR; safe **only** mid-dance, and every cycle ends in a defenceless
  Curtain Call. Caught in transit-gone-wrong, rushed, or focus-fired in the window, she dies fast.
- **Hard-countered by range, magic, and status** — none of which her evasion touches; guns beat her parry too.
- **Weak at single targets** — the audience ladder makes dancing at one enemy low-payoff and near-unshielded;
  bosses and duels are her bad matchup (SIM 1, SIM 4).
- **The premium is earned** — hard risk-deletion (Golem, Cover, invulnerability) caps her at the audience-2
  band; she cannot farm the heroic numbers from safety (SIM 8).
- **Spreading denies her** — radius 2 + no forced movement means a disciplined spread is her declared wrong
  board; she still has only the modest Cloth basic there.

Distinct from the **Thief** (always-on evasion-**stat** + slip-through-lines — `docs/job-balance/jobs/12-thief.md`;
hers is **dance-phase only**, and Performance Step does not slip lines), the **Time Mage** (the reliable,
ranged, persistent clock — hers is a one-shot melee tempo pulse), the **Mystic** (chosen, reliable, hard
afflictions — hers are random and soft), the **Black Mage** (bigger, safe, ranged nukes — hers is repeatable
access at risk), and the **Knight** (Guard Break depletes defence — she saps *output*). The **Dancer owns the
phase-cycle dive: audience-scaled evasion and heroic, risk-premium area dances earned in the middle of the
enemy.**

## Open items / calibration

All numbers are frozen DCL placeholders (`docs/deep-combat-layer/12`): the **audience ladders** for each dance
(damage 8/16/24 and ~40 capstone; saps 10/18/25 %; Slow −10/−20/−35 CT; Witch Hunt 12/24/35 MP; Forbidden
35/55/75 %); the **Stage Grace** evasion ladder (30/70/92 %, tuned so a melee-only pack still bleeds her via
the window) and its audience-2 export cap; the **Cloth parry** depletion (50/25/0) and `wmod`; the **Bag**
debuff-rider values; the **charge lengths** (short normal / long Last Waltz) and the **exposure-window**
minimum slice; the **radius** (2) and audience cap (3); and the **earned-premium** trigger set (which
hard-null/redirect layers cap her to audience-2). The **Forbidden Dance** table and its hard-control
exclusions carry over from `docs/deep-combat-layer/13` (the 3d6 status contest). The **Dancer / Bard R/S/M
parity** is owned here and mirrored from `docs/job-balance/jobs/16-bard.md` when the Bard is built.
