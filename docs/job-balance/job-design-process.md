# Job Design Process

How a job is taken from blank page to a registered decision doc on the Deep Combat Layer (DCL). This
doc owns the **workflow**: the order of operations, the adversarial review loop, the battle-simulation
gate, the human approval gate, and the recurring fix-patterns. It does **not** restate the design
*laws* — those are `docs/deep-combat-layer/15-job-authoring.md` (J1–J5, tiers, feasibility, the chassis
axes, the grade budget). This doc tells you *how to run the work*; doc 15 tells you *what the work must
satisfy*.

A job is **not done** until it has passed all four gates in order: the **draft** gate (a complete spec),
the **simulation** gate (it survives a falsification pass in play), the **consensus** gate (Claude and
the GPT peer agree 100%), and the **human** gate (Marcelo validates). Only then is it registered.

## Roles

- **Claude** — author and orchestrator. Drafts the job, runs the simulations as code, drives the loop.
- **GPT peer** (Codex MCP, adversarial reviewer) — defaults to objecting. It must understand the DCL
  and the job recipe deeply; it never rubber-stamps. A job reaches consensus only when the peer cannot
  find a remaining break. Treat its `broken` verdicts as binding when they carry a concrete reproducible
  scenario (the same adjudication bar as `.claude/skills/game-design-validation`).
- **Marcelo** — the human design owner and the final gate. His feedback is **direction, not law**: apply
  game-design judgment toward a better-feeling FFT, justify deviations, stay inside the hard rails
  (`CLAUDE.md`, `00`).

## The per-job workflow

### 1 — Ground in the vanilla job

Read `docs/job-balance/vanilla/NN`. State plainly **what the job is**, its vanilla tier, and — the part
that matters most — **its vanilla problems**. The recurring ones, which the design must consciously
solve or consciously keep:

- **Mine-don't-field / donor > destination** — the job's real value is an export (a support, an
  equip-access, a stat) splashed onto a *better* body, so nobody fields the job itself.
- **Situational / weak command** — the signature is accuracy-gated, telegraphed, dead vs monsters, or
  out-tempo'd by just attacking.
- **The JP grind wart** — trivial functionality split across many separately-bought abilities, so the
  job is a grind for small payoffs.
- **Over-capability / no-strictly-better** — the strong jobs do everything in one chassis; the risk is
  omnicapability, not weakness.
- **Feel-bad mechanics** — effects that are miserable to receive (permanent loss) or pointless to
  inflict (one-off enemies).

### 2 — Anchor in the DCL

Re-read the constraints the job must live inside before drafting: the design laws and chassis axes
(`15`), the pillars (`00`: contextual differentiation / no-strictly-better, two-sided traits,
legibility, deterministic damage + random contest), the tree position (`00-job-tree.md`), the grade
budget (`15` / `10`), the cross-job rations (control-status ≤2 doors each costed, lane ownership, no
redundancy — `pass-final` roster rules), and the armour model (`14`: three classes, job-gated).

### 3 — Draft around one identity

Produce the full spec: **fantasy · chassis · innate · command (Core vs Tier-2) · R/S/M · equipment &
weapon aptitude · early/mid/late · J1 (the pick) + the two-sided wrong-pick.** Tier is **acquisition
position** (S/A/B/C/D, S = only the hardest jobs to reach), and balance is enforced *within* the tier
(`15`, *Tiers*). Every ability — especially every R/S/M and every movement — is **explained
concretely**: what it actually does mechanically, not just a name.

### 4 — Simulate in play (the falsification gate)

See *The battle-simulation gate* below. This runs **before** consensus, not after — its job is to break
the draft, and a draft that has not survived it is not ready for the peer.

### 5 — Iterate with the GPT peer to 100% consensus

Hand the peer the full draft *and* the simulation results. It attacks; Claude revises; repeat. No
critic-shopping: a `broken` is answered by **editing the draft** to kill the reproducible scenario, not
by re-asking until the peer relents. Consensus is reached only when neither party can find a remaining
break across the DCL laws, the pillars, the rations, and the simulation spread.

### 6 — Present to Marcelo (clean spec, not the journey)

Show the finished spec — "tudo sobre o job" — with the **simulation conclusion** ("why it plays well /
where it is the wrong pick"), not the internal scratchwork. Do not narrate how Claude and the peer
reached it. Explain every ability and movement concretely. Grill open questions one at a time, each
with a recommendation.

**A spec is incomplete without a "Battle dynamics" section** (required, both in the presentation and in
the registered job doc). It is not enough to list the kit — describe how the job actually *plays*:

- **What the player does with it** — the moment-to-moment loop and the plan against the spread of other
  classes (who it beats, who beats it, which allies it pairs with, which terrain it wants).
- **How an enemy version harms the player** — what it is like to *face* this job when the AI fields it,
  and the player's counterplay. Every job is also an enemy; if facing it isn't legible and counterable,
  it is not done.

### 7 — Register

On validation, write the timeless decision doc at `docs/job-balance/jobs/NN-job.md` (present tense, no
journal). Update any cross-job artifact the job touched (the aptitude/grade-budget read, the
control-status rations, lane ownership). Then move to the next job. Never design more than one job at a
time.

## The battle-simulation gate

A draft that only "reads well" is not a draft. We must understand how the unit actually behaves in
battle: how it fares against the **spread of other classes**, whether it is useless in some matchups,
whether it survives, and — above all — whether its gameplay is **fun**. This gate makes that judgment
earned rather than assumed.

It is a **formal draft-stage gate with frozen assumptions and written failures** — never a vibe check
run after we already like the kit, and never numbers reverse-engineered to flatter the conclusion.

### Method

Numbers are deferred to calibration (`docs/deep-combat-layer/12`), so the sim uses **labelled
placeholder numbers derived from the DCL tiers, frozen before the outcome is known**, to test the
**shape** of the design — not its final balance. Where the question is numeric (damage and defense over
rounds, survival, tempo, action value), **compute it as a throwaway script**, never in your head;
multi-round arithmetic done by hand becomes confident wrong "evidence". Save scripts under
`work/` (dated) or a scratch `simulations/` dir; record the setup, the frozen numbers, the scenario
spread, and the read.

### Each sim is specified before it is run

```
Job under test:
Progression band:   early / mid / late
Build:              primary command + secondary + R/S/M + equipment assumptions
Opponent archetype:
Terrain:           open / cramped / vertical / chokepoint / rough
Initiative:        acts first / acts second / under pressure
Objective:         kill race / hold point / rescue / steal / survive / disable
Placeholder numbers frozen:
Predicted play loop, turn by turn:
Result:
Failure mode:
Design implication:
```

### The opposing archetype suite (stable, not invented per job)

Run against a fixed spread so coverage is comparable across jobs:

- **Plate wall** — Heavy Armor + shield Knight type.
- **Evasive skirmisher** — Thief / Ninja type, high Dodge and facing pressure.
- **Ranged controller** — Archer / crossbow / bow pressure.
- **Melee bruiser** — Monk type, high HP, crush, sustain.
- **Robe caster** — Black / White / Time / Mystic style, Faith / magic / status pressure.
- **Support attrition** — Chemist / White sustain and revive.
- **Monster / unarmed target** — no gear, so no theft / rend / disarm assumptions apply.
- **Capstone pressure** — a late-game high-output body, used only for mid/late checks.

### The required gates (all must pass before consensus)

1. **J1 proof** — simulate the claimed "this is the pick" scenario and show the job *contributes* before
   it dies or is ignored.
2. **Wrong-pick proof** — simulate a bad matchup and confirm it loses **for the intended reason**, not
   because the kit is generally nonfunctional.
3. **Early / mid / late proof** — the loop exists at first access, gains breadth or magnitude later, and
   does not collapse into a splash-only export.
4. **Survival proof** — it can execute its loop under plausible pressure. If the fantasy needs two setup
   turns and it dies in one, the fantasy is fake.
5. **Tempo proof** — compare the special action to "just attack / heal / move / kill". If the special
   action loses every time, it is decorative.
6. **Portability proof** — simulate the best abusive host for each R/S/M and the secondary command.
   Portability is *allowed* (kits travel by design); it fails only if an off-job host is a **strictly
   better home** — i.e., it does the job's signature *as well or better* without paying the moat (the
   main job's free innate + matching chassis/attributes/equipment). If the splash is merely good but the
   main still does it best, that's a pass. If a portable ability is degenerate on a burst host, prefer a
   **mechanical knob** (extra cost, guardrail) over a primary-only lock.
7. **Map proof** — at least one favorable and one hostile terrain. Terrain identity must matter without
   making the job unplayable on half the maps.
8. **Enemy-use proof** — is the job still fun when **enemies** use it against the player? This catches
   Rend-style feel-bad and oppressive enemy pressure.
9. **Variance proof** — for random contests, check the average **and** the bad-luck floor. A "fun prep
   minigame" becomes misery if three misses means the unit did nothing all fight.
10. **Fun-loop proof** — the first three meaningful turns present **choices**, not a single obvious
    script or repeated low-odds whiffing.

A job that fails a gate is revised and re-simulated, not argued past.

## The fix-pattern toolkit

Patterns established across the roster. Reuse them; each new job should reach for these before inventing
a one-off.

- **Main-job moat, not a kit lock (portability is the default).** Kits are meant to travel — liking a
  job's command and running it as a *secondary* is fine and even desirable. What makes a unit *main* a
  job is its **free innate + chassis + attribute bonuses + equipment access**, none of which a splash
  can take. So a signature ability is homed by making the job its **best host** (it gets the innate
  free, and its stats/equipment suit the kit), *not* by forbidding the ability elsewhere. Pattern: the
  innate is a **learnable Support** too, so an off-job that spends a secondary + a support slot can do
  it — worse, because it lacks the free innate slot and the matching chassis. (Thief *Light Fingers* is
  the Thief's free innate **and** a Support others can equip; the Thief out-thieves them on Speed/Dodge,
  not on exclusivity.) **Hard locks (primary-only) are an explicit exception**, justified case-by-case
  and verified by the portability sim — never the default. (The earlier "primary-gating innate" framing
  for Chemist *Field Lab* / Knight *Weapon Master* is being revisited under this rule.)
- **Job-gated armour (B10).** Durability comes from *choosing a durable job*, not from an Equip-armour
  splash — there are no Equip Heavy Armor / Equip Shields supports.
- **Engine-real vs Tier-2.** A flagship effect must land on **ordinary hits** (reliable now);
  speculative effects that need unbuilt engine hooks are **Tier-2 upside, never a floor** the job depends
  on. (`15`, *Feasibility*.)
- **No feel-bad permanent destruction**, but **keep legitimate iconic mechanics.** Permanent gear
  destruction (Rend) is cut — useless vs one-off enemies, miserable when used on the player. But Steal is
  a core, symmetric, two-way mechanic and is kept (low base chance + ways to improve it, so the player
  *prepares* to steal rare gear).
- **Control-status ration.** Loss-of-control statuses cost a real currency and have **≤2 deliberately
  different doors** across the roster (e.g. Charm = Orator; the two pin doors = Archer Pinning Shot +
  Chemist Snare).
- **No unique-character identity bleed.** A generic job does not get a hero's signature: no generic
  gun-specialist (gun = Mustadio, and it is a late-game weapon), no generic "Holy" blade (= Agrias).
- **Respect FFT signature names.** Keep canonical item / ability names — Potion, Hi-Potion, Phoenix
  Down, Remedy, Ether. Do not rename them (no Potion → "Tend").
- **Good JP pacing (J5).** The basic function is cheap and early; JP buys **breadth and magnitude**, not
  the basics. Consolidate trivial unlocks (e.g. one status-cure ability that unlocks all single cures).
- **Two-sided traits.** Lean on the Brave / Faith forks (`B9` / `A2`) so a stat is a build *choice* with
  a real downside, not a pure bonus.
- **Explain abilities concretely + verify FFT-implementability.** Every ability and movement is
  specified mechanically, and is checked against what the engine can actually do — no reaction-movement
  (the tile behind an enemy may be occupied; the engine has no such primitive), no effect that assumes a
  hook the DCL lists as unbuilt without flagging it Tier-2.

## Output discipline

- **English in `docs/`**, present tense, no dates / no `Status:` / no journal (`CLAUDE.md`).
- Human-facing specs carry the **simulation conclusion**, not the full math. Marcelo sees "why this
  plays well", not pages of internal scratch — unless he asks.
- One fact, one owner: cross-reference the DCL docs rather than restating their rules.
