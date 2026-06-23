# Archer Good-Job Rediscussion V0

Status: Accepted (GPT/Claude consensus) — pending Marcelo validation
Date: 2026-06-23
Scope: Archer only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Archer under the updated good-job premises:

- learned skills should feel useful and readable;
- persistent combat effects should use named visible feedback instead of hidden math drift;
- strong setup combos are healthy when they require real delay, position, line, or party follow-up;
- direct damage and recovery should scale modestly via a formula or visible progression hook (no
  fixed-forever values), while item-based recovery scales through item tier;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Archer rows in docs 06, 53, and 58 where they conflict. It does not change
Knight, final JP costs, prerequisites, equipment lists, item economy, or implementation data.

## Archer Identity

Archer is the ranged prediction specialist and the only true bow/crossbow job. It should remain
useful through the endgame by choosing the right shot for the map state: fast reliability, delayed
payoff, movement pinning, piercing a prepared target, or covering a tile.

Archer should feel strong when it owns height, line of fire, target prediction, and missile-favored
armor matchups. It should be weaker when rushed, terrain-blocked, denied line of fire, forced into
close melee, or asked to solve shield/evasion without setup.

The vanilla `Aim` ladder is not preserved as eight numeric variants. Its identity becomes a smaller
set of readable shot choices.

## Shared Status Vocabulary

Archer uses visible states for effects that persist beyond the instant shot.

| Status | Meaning | Primary Archer source | Guardrail |
| --- | --- | --- | --- |
| `Pinned` | Move -1 | `Pinning Shot` | Non-stacking; not `Immobilize`, `Slow`, `Stop`, or a hard lock. |
| `Pierced Mark` | next bow/crossbow hit gains piercing benefit | `Piercing Shot` | Consumed once; bow/crossbow only. |
| `Covered Zone` | visible delayed tile threat | `Covering Shot` | Telegraph, delay, and fizzle are the counterplay. |

These may use existing visual/icon carriers if implementation requires it, but their mechanical
meaning should stay distinct from hard-control statuses.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Quick Shot` | Action | bow/crossbow output x0.75; hit +0.15 | CT 0; no Charging; not a normal-attack replacement. |
| `Aimed Shot` | Action | bow/crossbow output x1.35; hit +0.15 | CT 2; Charging risk; target prediction matters. |
| `Pinning Shot` | Action | bow/crossbow output x0.70; target -12 CT and `Pinned` | `Pinned` is Move -1, non-stacking, short duration. |
| `Piercing Shot` | Action | bow/crossbow output x1.05; applies `Pierced Mark` | Allied bow/crossbow consumption; no guns/spells. |
| `High-Ground Shot` | Action | cut | Height reward folds into bow/crossbow baseline hit. |
| `Covering Shot` | Action | delayed visible tile zone; one enemy hit x1.00, hit +0.10 | No instant interrupt; no multi-hit AoE. |
| `Arrow Guard` | Reaction | fixed/capped 65%; incoming missile hit chance x0.50 | Missile weapon hits only; not Brave-scaled as main lever. |
| `Speed Save` | Reaction | fixed/capped 60%; +8 CT | Survivor-only; once per unit round; no Speed stat growth. |
| `Equip Bow` / `Equip Crossbows` | Support | longbow/crossbow unlock | No guns; no damage or accuracy rider. |
| `Concentration` | Support | bow/crossbow and Archer-command hit floor 0.75 | No universal evasion bypass; no guaranteed-hit stacking. |
| `Bow Mastery` | Innate active Archer trait | bow/crossbow damage x1.10; hit +0.05 | Not a support slot; active Archer native-shell payoff. |
| `Jump +1` | Movement | vertical utility +1 | No Move bonus; no late-mobility replacement. |

## Action Notes

### Quick Shot

`Quick Shot` is Archer's low-commitment reliability button.

```text
damage = bow/crossbow output x0.75
hit = normal hit +0.15
CT = 0
```

Rules:

- no `Charging` state;
- bow/crossbow only;
- no guns;
- no status rider;
- should not become the default attack at ordinary hit rates.

The trade is meant to be obvious: give up 25% output to land a shot that matters now.

### Aimed Shot

`Aimed Shot` replaces the vanilla `Aim +1` through `Aim +20` ladder with one readable delayed
payoff.

```text
damage = bow/crossbow output x1.35
hit = normal hit +0.15
CT = 2
state = Charging while queued
```

Rules:

- bow/crossbow only;
- no guns;
- the user takes normal `Charging` risk;
- if the target leaves the selected legal panel or line before resolution, the shot can fail or lose
  value;
- setup with `Pinned`, `Taunt`, terrain, or ally positioning is intentional.

The goal is to preserve the fantasy of lining up a stronger shot without repeating vanilla's bloat or
feel-bad delay ladder.

### Pinning Shot

`Pinning Shot` is movement and tempo pressure, not hard control.

```text
damage = bow/crossbow output x0.70
on hit = target -12 CT and Pinned
Pinned = Move -1 until target completes its next action
```

Rules:

- `Pinned` is visible;
- `Pinned` is non-stacking;
- `Pinned` is not `Immobilize`;
- `Pinned` is not `Slow` or `Stop`;
- the CT loss affects the target immediately and is visible through turn order.

The -12 CT carries most of the bite. Move -1 keeps the positional wound readable without turning
multiple Archers into a hard kite-lock engine.

### Piercing Shot

`Piercing Shot` turns armor-piercing into a visible missile-family setup.

```text
CT = 1
state = Charging while queued
on declaration = target gains Pierced Mark
Pierced Mark = next bow/crossbow hit against target deals x1.20 final damage and consumes the mark
declaring Piercing Shot damage before mark consumption = bow/crossbow output x1.05
```

Rules:

- `Pierced Mark` is visible;
- `Pierced Mark` is consumed once;
- allied bow/crossbow hits can consume the mark;
- guns, spells, items, thrown weapons, and generic all-damage effects cannot consume it;
- if the target acts, the shot resolves, or the target becomes illegal before consumption, the mark
  expires;
- only one `Pierced Mark` can be active on a target.

`x1.20 final damage` is the provisional clean implementation of "penetration +0.20." It avoids an
invisible armor-response delta while preserving the feel of a prepared piercing shot. If later formula
work prefers "ignore 20% of target armor mitigation," this row must be re-reviewed before final data.

The allied-consumption version is accepted because it creates a bounded multi-Archer combo: it costs
delay, line, weapon family, and at least one follow-up action.

### High-Ground Shot

`High-Ground Shot` is cut as a separate action.

Instead, height becomes part of the bow/crossbow baseline:

```text
if attacker height advantage >= 2:
  eligible bow/crossbow hit +0.10
```

Rules:

- no damage multiplier;
- applies to normal bow/crossbow attacks and Archer bow/crossbow actions;
- does not apply to guns, spells, items, or melee;
- does not stack into guaranteed hit with `Quick Shot` or `Concentration`.

This keeps the Archer fantasy of owning high ground without creating a CT 0 damage-and-hit spam
button.

### Covering Shot

`Covering Shot` becomes a delayed tile prediction shot rather than instant overwatch.

```text
target = visible tile in legal bow/crossbow range and line
CT = 2
state = Charging while queued
Covered Zone = center tile plus orthogonal adjacent tiles
on resolution = one legal enemy in the zone takes bow/crossbow output x1.00, hit +0.10
```

Target priority:

1. enemy on the center tile;
2. otherwise the legal enemy closest to the center;
3. if no legal enemy is in the zone, the shot fizzles.

Rules:

- visible `Covered Zone` feedback is required;
- no instant interrupt;
- no movement-triggered snap shot;
- no multi-hit AoE in this version;
- no target if line/range legality fails at resolution;
- `Charging` risk applies while the Archer waits.

The counterplay is the warning, delay, and fizzle risk. The payoff is forcing enemies to respect a
tile without making movement impossible.

## Reaction Notes

### Arrow Guard

`Arrow Guard` should be Archer's narrow anti-missile reaction.

```text
trigger = fixed/capped 65%
eligible incoming = bow, crossbow, gun, or thrown-style missile weapon hit
effect = incoming missile hit chance x0.50 before final hit resolution
```

Rules:

- not Brave-scaled as the main lever;
- no melee coverage;
- no spell coverage;
- no status coverage;
- no broad physical immunity.

This reaction is strong in ranged duels and nearly dead elsewhere, which is healthier than another
generic defensive reaction.

### Speed Save

`Speed Save` becomes a short tempo bump, not a stat snowball.

```text
trigger = fixed/capped 60%
timing = post-damage, survivor-only
frequency = once per unit round
effect = +8 CT
```

Rules:

- no permanent Speed increase;
- no battle-scoped Speed increase;
- not Brave-scaled as the main lever;
- no trigger if the unit is KO'd by the hit.

If +8 CT is still too quiet in later feel checks, +10 CT is the first adjustment direction.

## Support And Movement Notes

### Equip Bow / Equip Crossbows

The bow equipment unlock remains a pure support-slot build choice:

- enables longbow/crossbow use where legal;
- no guns;
- no damage rider;
- no accuracy rider;
- active Archer must remain the best native bow/crossbow shell.

Final naming can use the original display vocabulary if implementation wants `Equip Crossbows`, but
the design concept covers the bow/crossbow unlock lane.

### Concentration

`Concentration` is narrowed away from vanilla universal evasion bypass.

```text
eligible actions = bow/crossbow attacks and Archer command actions
effect = evasive hit-rate floor 0.75 after normal accuracy/evasion math
```

Rules:

- no spells;
- no items;
- no status actions;
- no guns;
- no generic melee;
- no boss/status immunity bypass;
- no empty-zone protection for `Covering Shot`;
- should not stack with `Quick Shot` into guaranteed hit.

This is a major departure from vanilla's broad support value. It is accepted provisionally because a
universal accuracy fix would become a convergence risk across too many builds.

### Bow Mastery

`Bow Mastery` becomes an innate active-Archer trait instead of a learned support.

```text
active Archer bow/crossbow damage x1.10
active Archer bow/crossbow hit +0.05
```

Rules:

- active Archer only;
- no support slot;
- no guns;
- no spells;
- no generic missile;
- no stacking with a future portable marksman support unless that support is separately accepted.

The reason is structural: a support that only helps bows risks being dead cross-job value when Archer
is the only native bow job, while a broad portable version competes with generic damage engines. If
later build-incidence review proves that Archer lacks a satisfying portable support hook, a separate
non-stacking `Marksman Training` support can be proposed, but it is not accepted in this first
Archer artifact.

### Jump +1

`Jump +1` remains a vertical utility movement skill:

- +1 vertical reach;
- no Move bonus;
- no terrain bypass;
- no late-mobility replacement.

It supports the folded high-ground hit bonus without making Archer generally mobile.

## Expected Play Patterns

Healthy Archer patterns:

- active Archer uses height and line of fire to pressure mail/leather targets;
- `Quick Shot` solves urgent or evasive moments at reduced output;
- `Aimed Shot` rewards allies who constrain target movement;
- `Pinning Shot` helps a party create a small timing and positioning window;
- `Piercing Shot` into allied bow/crossbow follow-up creates a bounded missile-family combo;
- `Covering Shot` makes enemies respect a visible tile without hard interrupting them;
- non-Archer builds spend support on bow/crossbow access for a deliberate ranged plan.

Unhealthy Archer patterns to watch:

- `Quick Shot` plus `Concentration` makes evasion irrelevant;
- `Pinned` stacks or chains into effective immobilization;
- `Pierced Mark` becomes mandatory setup for every missile party;
- `Covering Shot` locks narrow maps too cheaply despite delay;
- `Concentration` remains too universal even after narrowing;
- innate `Bow Mastery` makes active Archer numerically too high;
- `Arrow Guard` plus evasion creates practical missile immunity;
- cutting `High-Ground Shot` removes too much learned-action texture from the job.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `T4 accuracy/evasion`: test `Quick Shot`, `Concentration`, high-ground hit bonus, `Arrow Guard`,
  and shield/evasion matchups.
- `T5 CT/delay`: test `Aimed Shot`, `Piercing Shot`, `Covering Shot`, `Pinning Shot`, and
  `Speed Save`.
- `T6 armor response`: test whether `Pierced Mark` as x1.20 final damage preserves the intended
  missile anti-armor role.
- `T11 terrain/height`: test folded height bonus and `Jump +1` incidence.
- `M-SECONDARY-COUNT`: count Archer command secondary, `Equip Bow`, `Concentration`, and `Jump +1`
  incidence.
- `F5 real-roster sweep`: test active Archer against rushed, shielded, evasive, bad-line,
  high-ground, and mail-heavy encounters.
- Control identity sweep: ensure `Pinned` and `Covered Zone` create windows, not locks.
- Combo convergence: test multi-Archer `Pierced Mark` consumption and `Covering Shot` zone stacking.

## Reviewer Notes

Claude reviewed the opening Archer package before this artifact was written and approved the core
direction:

- cut `High-Ground Shot` and fold height into bow/crossbow hit;
- make `Pinning Shot` a visible `Pinned` movement wound plus CT pressure;
- make `Piercing Shot` a visible `Pierced Mark`;
- prefer allied bow/crossbow consumption for `Pierced Mark`;
- make `Covering Shot` a delayed visible tile zone, not instant overwatch;
- turn `Speed Save` into +8 CT, not Speed growth;
- narrow `Concentration`;
- keep `Arrow Guard` missile-only and non-Brave-centered.

Claude recommended considering both innate `Bow Mastery` and a portable non-stacking marksman
support. This artifact chooses innate-only for the first pass because a separate bow support conflicts
with `Equip Bow` for most non-native bow builds and would add a support slot that may not actually be
usable. This is a deliberate point for reviewer validation.

This document still requires final Claude artifact review before Archer is considered closed.
