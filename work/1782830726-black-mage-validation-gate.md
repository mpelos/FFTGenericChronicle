# Black Mage (#06) — design convergence & validation gate

Date: 2026-06-30. Closes the Black Mage design (the offensive caster, second DCL caster). Registered doc:
`docs/job-balance/jobs/06-black-mage.md`. Sim harness: `work/1782830726-sim-black-mage.py`.

## Process

Designed from the vanilla Black Mage (`docs/job-balance/vanilla/06-black-mage.md`) + the DCL magic system
(`docs/deep-combat-layer/11`), never anchored on a prior draft. GPT peer (thread
`019f1030-9ddc-7ab2-8179-0f90cf106a30`, gpt-5.5) participated per the divergence + every-decision directives —
and **materially redirected the design** three times (recorded below). Awaiting Marcelo validation (new job).

## The DCL reshape (why Black is a different job than vanilla)

Vanilla Black is tier B→C because of *reliability*: interruptible/wasted charge, late tier convergence,
out-scaled by summons/Calculator. The DCL removes all three — charges aren't damage-interrupted (`11`),
multiplicative spell-centric tiers stay meaningful at every MA (`11`), and the Calculator is gone (→
Necromancer). It also hands Black DR-ignore (anti-armor) + uncosted AoE. So the design risk **flipped** from
"underpowered" to "**strictly-better / press-Firaga**." The whole design is brakes + identity, not power.

## GPT's three material redirects (the divergence working)

1. **Compass.** My "Siege Battery" (slow heavy barrage) → GPT's **"The Arsenal"** (nimble breadth: element ×
   tier × shape × placement × timing). My compass would have consumed the **future Summoner's** space (the
   heavy barrage). Conceded.
2. **-ga placement.** I floated -ga as Tier-2 → GPT: the tier ladder **is** the core identity; -ga is Core.
   Conceded.
3. **Movement.** I lazily copied White's Move-MP-Up → GPT: it dissolves the MP-budget brake. Then I proposed
   Float → GPT: Float is Time/Geo-owned, and the only "safe" glass-cannon movement is terrain utility (not
   Black's to own). **Movement = `[deferred pending Time Mage]`**, not filled to pad the kit. Conceded.

I went **firmer than GPT** on one fork: status. GPT said "de-emphasize Toad"; I proposed **cutting both Toad
and Poison** (keep only Death) to sharpen every caster lane — GPT agreed strongly.

## Locked design

- **Identity:** the widest *attack* spellbook; mastery = reading the board. Throughlines: **anti-armor**
  (DR-ignore) + **flexible burst**. Pure offense, no support/sustain beyond the bolt floor.
- **Chassis:** Robes (sprite), highest MA, HP ~75, HIGH Faith (two-sided glass cannon), low Brave, Move 3, off
  the weapon axis (Rod A → free range-3 elemental bolt).
- **Innate — Rod Attunement** (free + exported): matching-element **floor + basic tier only** (stronger bolt,
  cheaper basic casts); never -ra/-ga/Flare/Death, no CT cut, no burst amp. Pre-battle element commitment with
  a built-in two-sided cost. Fallback = pure proficiency if it loosens the budget brake.
- **Command — Black Magic.** Core: the elemental ladder (Fire/Blizzard/Thunder × basic/-ra/-ga, distinct
  profiles) over the free Rod bolt. Tier-2: Flare (resist-proof single-target, no AoE), Death (3d6 inverse-Faith
  cruelty, boss-immune).
- **Status cut:** Toad → Mystic/Oracle; Poison → Necromancer (access promises, not silent removal).
- **Identity wall vs White:** Black owns ladder+AoE+elements+burst; White's ceiling = one basic-Fire Holy.
- **Summoner boundary:** Black = faster/flexible/smaller AoE + burst; the big slow barrage is the Summoner's.
- **R/S/M:** Rod Counter (guardrailed bolt retaliate) · Rod Attunement + Rod Training exports · Movement
  deferred.

## Sim read (`work/1782830726-sim-black-mage.py`, 9 SIMs, all pass; no forced changes)

- **SIM1 tier profiles:** k=1 dmg/MP Fire 7.83 > Fira 6.52 > Firaga 5.22 — Firaga is the **worst** single-target
  pick; overtakes only at k≥2. (Wrinkle: Fira edges Fire on dmg/CT at k=1, 34.8 vs 31.3 — but MP is the binding
  budget, so Fira reads as the flexible MIDDLE; Firaga stays dominated single-target on both axes.) Crossover OK.
- **SIM2 Attunement:** bolt 41.8→52.2, basic Fire MP 8→5; Fira/Firaga/Flare untouched; resist board craters the
  cheap plan (62.6→43.8). Floor not burst; two-sided cost intact.
- **SIM3 anti-armor:** Black Fire 62.6 **flat** across heavy/clothes/robes; fighter sword 21.8 vs heavy / 87.1
  vs robes. Bolt floor 41.8 still chips plate. Canon anti-armor role holds.
- **SIM4 glass cannon:** thief dive TTK 1.4; enemy Firaga 247 one-shots the high-Faith body (mage-kills-mage
  mirror; Magic-Evade 50% coin-flip save). Two-sided confirmed.
- **SIM5 ★ friendly fire:** Firaga 146/target, self-hit 146 = lethal → FF is the load-bearing press-ga brake.
  **Flagged as a doc-11 dependency** (see below).
- **SIM6 element read:** weak→Firaga 190, resist→Flare 125 wins. Flare = costly resist-proof single-target.
- **SIM7 budget:** burst (2× Firaga then bolt floor) vs sustain (9× Fire); both keep casting; only charge turns
  are zero-output.
- **SIM8 wall vs White:** White Holy 63.3 == Black basic Fire 62.6, then Black climbs. No overlap.
- **SIM9 Death:** inverse-Faith land low 26 / neutral 50 / high 74%, boss 0%. Swingy, boss-immune cruelty.

## Open dependencies / calibration (tagged in the doc)

- **★ Friendly fire is a BLOCKING system assumption for large AoE.** Doc 11 does not state it; it is the
  cleanest brake on press-ga. **Confirm/promote in doc 11.** If the DCL rejects friendly fire, the -ga
  radius/CT/MP must be **re-simmed immediately** against the no-FF backups (k=1 inefficiency, modest radius,
  long CT, per-target Magic-Evade).
- **Movement deferred** pending the Time Mage's caster-movement map (Float candidate; don't lock the negative).
- All numbers are frozen DCL placeholders (G_m=0.58, Faith [0.70,1.30], Zodiac ×1.30/×0.70, Shell ×0.50, MA 18,
  spell tiers, MP budget 72, CTs). Real calibration is `docs/deep-combat-layer/12`. The uncosted-AoE cluster
  reward (M2) remains the magic system's open risk; Black's friendly-fire brake is the job-side answer to it.
- **Rod Attunement knob** (floor/basic discount magnitudes) — Hypothesis; tighten or fall back to pure
  proficiency if sim shows budget-brake erosion.

## Time Mage impasse (raised by Marcelo during Black validation; resolved with GPT)

Marcelo: "Black is taking all/most offensive magic — what's left for Time? And Time's current problem is it's
TOO support, nearly useless alone." Real concern; resolved before finalizing Black.

- **Black does not invade Time's lane.** Black has zero tempo, zero Gravity/%-HP, zero CT manipulation, zero
  Reflect/Float/Teleport. The ONLY overlap is non-elemental flat burst (Flare vs Comet/Meteor).
- **Fork decided — Flare STAYS on Black (Proposal Y), no Black kit change.** I leaned toward moving
  non-elemental → Time (Proposal X, to trim Black); GPT diverged and won the argument: a tempo controller
  (Haste/Slow/Stop/Quick/Teleport/Short Charge) PLUS a resist-proof nuke would crowd Black far more than Black
  crowds Time, and Black needs its one resist-proof answer or it stops being "an arsenal." Black change =
  **only a protected-lane note** (added to jobs/06): the HP-axis (Gravity/%-HP) and the clock (CT/Speed, Haste/
  Slow/Stop/Quick), plus Reflect/Float/Teleport and any Meteor-style prediction spell, are the **Time Mage's**.
- **Fix for "Time too support" (DIRECTION for #10, not designed yet):** make Time's OWN material active offense,
  not buffs-for-others — (1) **Gravity** as a real anti-giant softener (percent-HP, DR/Faith-independent — a
  model Black can't do), NOT made max-HP-lethal (that would make Time the best boss-killer and break HP-gated
  encounters); the free Rod/Staff bolt finishes what Gravity leaves low; (2) **tempo as offense** (Slow/Stop
  deny enemy turns; Haste/Quick make windows) framed as dismantling the enemy clock; (3) **Meteor** as a late
  Tier-2 long-telegraph prediction capstone (lower reliability than Black's flexible spells, distinct from the
  Summoner's committed barrage). Identity: **Black = "I delete it" (magnitude); Time = "I own the clock and
  crush the giants" (tempo + %-HP)** — both active, neither replaceable. **Feel lever (Marcelo's bar):** make
  Gravity's damage chunky/visible and give Time a real capstone moment, so it reads as an active combatant, not
  a debuff bot. To be carried into the Time Mage (#10) design.

### Comet — Marcelo's counter (accepted, refined with GPT)

Marcelo pushed back on the above: (1) Gravity-anti-giant is **too niche** (high-HP bodies are rare → a dead
button most fights); (2) tempo-as-offense is "really still support" and he's FINE with Time's main axis being
support — it just needs a bit of **basic direct offense** so it isn't the 100%-support trap (= our
every-job-needs-minimum-offense law); (3) Meteor is end-game, can't be the base. His proposal: give Time a
**Comet** — single-target, so it doesn't steal Black's axis and Time is self-sufficient without splashing
Black Magic.

This is NARROWER than the Proposal X GPT vetoed (that was a flexible non-elemental ARSENAL + AoE). **Accepted**
with GPT's guardrails:
- **Comet = Time's minimum direct offense, NOT Time's damage plan** (hard wording lock).
- Single-target, non-elemental, **no AoE, no tier ladder, no rider** (no Slow/CT hybrid — that would overload
  it into the control-damage lane), real MP/CT.
- Calibrated to **~-ra effective output after Time's lower MA** (NOT the -ga Marcelo floated — a fast,
  never-resisted -ga nuke on the Short-Charge/Quick job would become the default single-target button). It may
  beat Black's neutral basic Fire per cast (it costs more, never gets Attunement/weakness-spike/AoE/tier
  scaling), but must **lose to** Black exploiting weakness, Black -ga at k≥2, Black Flare, and Black's
  dmg-per-MP sustain. Exact number simmed at #10.
- **Gravity de-niched** in parallel: framed as %-current-HP useful as an opener vs ANY healthy target, not
  "anti-giant only." Time's offense = Comet (reliable flat) + Gravity (%-HP opener) + free bolt (finisher
  floor) + tempo (control).
- **Flare stays Black's.** Black remains the damage arsenal + burst ceiling.

Black-doc impact: surgical — the Time boundary note (Command section + the "Distinct from" list) updated to
"clock + Gravity + one modest Comet for self-sufficiency," Flare explicitly retained on Black. No kit change.
