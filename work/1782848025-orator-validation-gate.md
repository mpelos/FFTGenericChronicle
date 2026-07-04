# Orator (#13) — design convergence & validation gate

Date: 2026-06-30. Closes the Orator design ("The Demagogue", the fifth controller-lane job after the four
casters). Registered doc: `docs/job-balance/jobs/13-orator.md`. Sim: `work/1782848025-sim-orator.py` (38/38).
Marcelo: **approved**.

## Process

Designed from the vanilla Mediator (`docs/job-balance/vanilla/13-orator.md`) + the DCL status engine
(`docs/deep-combat-layer/13`), gun model (`10`/`14`), and the authoring laws (`15`), never anchored on the
legacy v2. GPT peer (thread `019f1030-9ddc-7ab2-8179-0f90cf106a30`) participated per the divergence directive
across **five rounds** and materially redirected the design; **Marcelo redirected it twice mid-design** (the
big pivots below).

## Marcelo's two mid-design pivots (the design is built on these)

1. **Orator is the GUN job — the sharpshooter.** Generic gun expertise lives on the Orator (the Chemist has
   **no gun**, 02:49 — Mustadio is a unique character, not the owner). The gun becomes a real **damage pillar**,
   not a floor. → solves "useless Mediator". Later: it needs **a few real gun skills** to *read* as a shooter
   (Barrage + Piercing Shot), and the innate must be **attractive every fight** (Master Gunner), not the dead
   monster-rapport innate.
2. **Brave/Faith ownership vs the approved Mystic, resolved by MECHANISM.** Mystic edits *magical* Brave/Faith
   = a temporary **STATUS** (doesn't touch the stat); Orator edits the **REAL stat** (permanent, the vanilla
   **4:1** rule, **any target / friendly-fire**, **symmetric** — enemy Orators can permanently shift player
   stats). This **supersedes** the old "no permanent player loss" stance (Marcelo's explicit call). The 4:1
   rule **replaces** an earlier invented per-battle anti-grind cap (obeys no-invented-friction). The Mystic doc
   (`09`) was updated to say its tuning is a status, not a stat edit, and is friendly-fire-able.

## GPT's material redirects (the divergence working)

1. **Snowball blockers** (pre-pivot): human-recruit cascade + Praise farm + Charm action-economy → cut by
   Invite-removal + active-Traitor cap (1/Orator) + the 4:1 rule.
2. **Gun control is fully rationed away.** Pin is full (Archer Pinning + Chemist Snare = the ≤2 doors); disarm
   = Knight/Thief; Don't-Act = Mystic; Aim/precision = Archer. → the gun is **damage only, no control**, and is
   **dodgeable** (defeats armour, **not** evasion — the ≤1-axis ration), so the Archer keeps evasive/zone/vertical.
3. **The sim laundered an Archer-boundary fail** (gun beat the Aimed bow into evasive) → fixed by making the gun
   dodgeable; the numbers now drive the verdict.
4. **Master Gunner packaging break:** two separate supports (Equip Guns + Master Gunner) are undefeatable (one
   slot) → **collapsed into one** export (Gun A + full penetration), like Monk's Martial Arts. Penetration =
   full gun **divisor**, **not** DR-ignore (stays dodgeable/blockable/LoS) → clean vs Black's magic bypass.
5. **Export stress on the right hosts:** the real break candidate is a **heavy-armour** host (Knight + gun =
   plate sniper), not a caster. Guardrail: borrowed gun = **basic shots only** (no Barrage/Piercing/Speech),
   2H (no shield) + a slot → a flex pick, not a default; no host **mains** it over its own kit (SIM 6).
6. **Two gun skills, not three:** keep Barrage (committed) + Piercing Shot (line); **cut Seal Evil** (undead-only
   Petrify is still hard control). Marcelo also nixed the "Sanctified Shot" anti-undead nod — **anti-undead is
   White Mage territory**, off-theme for the Orator.

## Locked design

- **Identity "The Demagogue":** two pillars — **LEAD** (the gun) + **VOICE** (real Brave/Faith + social control
  + monster recruit).
- **Chassis:** light/cloth, HP ~80, modest PA/MA (gun is stat-independent; MA = the social-contest hook, NOT
  magic), 2H gun (no shield), neutral Brave & Faith, Move/Jump normal.
- **Innate — Master Gunner** (armour-defeating gun mastery; the collapsed innate+export = Gun A + full
  penetration; divisor, not DR-ignore).
- **Command "Speechcraft":** Lead = basic gun shot · **Barrage** (committed spike) · **Piercing Shot** (line).
  Voice = ±Brave/±Faith (real, 4:1, any target) · **Call Out** (taunt door #2) [core]; **Charm** (battle flip,
  active-cap 1) · **Insult/Berserk** · **Tame** (monster recruit) [Tier-2, MP-costed].
- **R/S/M:** Supports = **Master Gunner** + **Beast Tongue** (monster route, now a support not the innate).
  Reaction + Movement **open** (no crutch).
- **Counterplay:** light/no-shield (folds to a diver); gun is dodgeable (evasive enemies + the Archer are its
  weakness); voice is MP-gated; low-Brave/disciplined enemies refuse its social plays.

## Sim read (`work/1782848025-sim-orator.py`, 38/38, no forced design changes)

- **SIM1** lone Orator clears a low-level pack even surrounded — gun-only (the gun is the J6 pillar).
- **SIM2** Brave/Faith = real stat, sole source, 4:1, reversible, symmetric (Marcelo), MP-reliable.
- **SIM3** lane vs Mystic clean (real STAT vs magical STATUS; friendly-fire both); no Time-clock overlap.
- **SIM4** Charm poor fresh / reliable after setup; active-Traitor cap bounds the snowball (+1 vs uncapped +6).
- **SIM5** Call Out baits high-Brave (74%), refused by low-Brave (1.9%); taunt door #2.
- **SIM6** no host (caster OR heavy-armour) mains the borrowed gun; collapsed export; divisor not DR-ignore.
- **SIM7** gun wins armour-LoS only (44>32); Archer wins soft tempo (49>48) and reclaims evasive via Aim (40>27).
- **SIM8** innate Master Gunner (every fight); Beast Tongue = niche support; Tame protected; human Invite cut.
- **SIM9** Barrage/Piercing are damage/geometry, per-hit < basic (not "attack but better"), no control.
- **SIM10** two pillars, control tiered, gun adds damage-shape not a control axis — not omnicapable.

## Cross-job impact

- **Mystic (09):** doc updated — its Brave/Faith tuning is a temporary magical STATUS (not a stat edit) and is
  friendly-fire-able; the **real-stat** edit is the Orator's.
- **Lane-locks confirmed:** Charm/Berserk = Orator; the gun (firearms) = Orator (Chemist has none; Mustadio is
  a unique character). Taunt door #2 = Orator (Knight is #1). Pin remains Archer + Chemist (full).
- **Superseded:** the old "no permanent player Brave/Faith loss" stance — now 4:1 symmetric per Marcelo.

## Open dependencies / calibration (tagged in the doc, all → `docs/deep-combat-layer/12`)

Gun base output + penetration divisor; Barrage hit-count/per-hit/charge; Piercing per-target falloff + line
length; voice MP costs + 3d6 curves (Charm setup, Call Out inversion); active-Traitor cap with multi-Orator
parties; the open Reaction/Movement slots. All numbers are frozen placeholders (GUN_A 50, MA 12, etc.).
