# Knight Good-Job Rediscussion V0

Status: GPT/Claude consensus for Marcelo validation
Date: 2026-06-23
Scope: Knight only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Knight under the updated good-job premises:

- learned skills should feel useful and readable;
- persistent combat effects should use named visible feedback instead of hidden math drift;
- direct stat breaks can stay vanilla-style direct battle stat changes when that is clearer than
  status spam;
- strong setup combos are healthy when they require real action, positioning, timing, or party
  follow-up;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Knight rows in docs 06, 53, and 58 where they conflict. It does not change
Archer, final JP costs, prerequisites, equipment lists, item economy, or implementation data.

## Knight Identity

Knight is the armored control anchor: a durable frontline job that holds space, makes armed enemies
less dangerous, opens guarded targets for allies, and forces priority enemies to respect the melee
line.

Knight should feel good when the enemy has weapons, shields, armor, or a key attacker to contain. It
should be less dominant into magic/status-heavy encounters, ranged kiting, enemies with no meaningful
equipment lane, and maps where slow armored positioning is punished.

Knight is not a generic sword DPS job, not the best crush job, and not a universal tank template.

## Shared Status Vocabulary

Knight should use a small visible vocabulary that the player can learn once.

| Status | Meaning | Primary Knight source | Guardrail |
| --- | --- | --- | --- |
| `Guarded` | eligible incoming direct damage x0.70 | `Guarded Strike` | One protected hit by default; no broad immunity. |
| `Exposed` | all incoming damage x1.25 | `Rend Armor` | Same meaning as Squire `Exposed`; non-stacking. |
| `Disarmed` | outgoing weapon/basic weapon-skill damage x0.70 | `Rend Weapon` | Armed enemies only; no spell/item/status penalty. |
| `Guard Broken` | shield and weapon guard/evasion layers x0.50 | `Shield Break` | Does not touch class, accessory, rear-facing, or immunity layers. |
| `Taunted` | next offensive action must target or approach the Knight if legal | `Taunt` | No Berserk damage boost; no boss/immunity bypass. |

`Exposed` is intentionally unified with Squire's `All-Out Strike` drawback:

```text
Exposed = incoming damage x1.25
```

The source and duration can differ, but the meaning and magnitude should not. Squire can self-apply
Exposed as a risk; Knight can apply Exposed to enemies as a setup window.

`Guarded` should also be considered the preferred shared name for one-hit 30% direct-damage
mitigation. If Squire's `Grit Guard` is later renamed to `Guarded`, the underlying behavior remains
the Squire-specific desperation trigger already accepted in doc 68.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Guarded Strike` | Action | weapon output x0.85; self gains `Guarded` | One eligible direct hit at x0.70 until next Knight turn. |
| `Rend Weapon` | Action | weapon output x0.50; target gains `Disarmed` | Armed weapon/basic weapon-skill offense only. |
| `Rend Armor` | Action | weapon output x0.50; target gains `Exposed` | Visible x1.25 incoming damage window; non-stacking. |
| `Shield Break` | Action | weapon output x0.50; target gains `Guard Broken` | Only shield/weapon guard/evasion layers are halved. |
| `Rend Power` | Action | weapon output x0.35; target PA -2 | Battle-scoped; stacks at most twice, cap -4 PA. |
| `Rend Magick` | Action | weapon output x0.35; target MA -2 | Battle-scoped; stacks at most twice, cap -4 MA. |
| `Rend Speed` | Action | weapon output x0.35; target Speed -1 | Battle-scoped; stacks at most twice, cap -2 Speed. |
| `Rend MP` | Action | weapon output x0.35; MP damage min(30, 25% max MP) | Anti-caster pressure; no HP burst. |
| `Taunt` | Action | range 3 forced-target status | Replaces `Challenge`; no passive AI attractiveness. |
| `Crushing Blow` | Action | cut | Redundant with `Shield Break`; protects Monk's crush identity. |
| `Parry` | Reaction | narrow frontal weapon mitigation | Equipment/facing-bound; not Brave-scaling as the main lever. |
| `Brace` | Reaction | cut or defer | Only returns if a later pass needs hold-ground identity. |
| `Equip Armor` | Support | heavy armor unlock | No free mitigation multiplier. |
| `Equip Shield` | Support | shield unlock | No free mitigation multiplier; needs evasion validation. |
| `Defensive Training` | Support | explicit Knight-action upgrade table | Does not improve ordinary attacks or stat-break caps. |
| `Shield March` | Movement | armored positioning tool | No terrain/elevation bypass; not universal movement. |

## Action Notes

### Guarded Strike

`Guarded Strike` becomes the Knight's safe engagement button.

```text
damage = equipped weapon output x0.85
on use = self gains Guarded
Guarded = next eligible incoming direct damage x0.70
```

Rules:

- `Guarded` lasts until the start of the Knight's next turn or until it absorbs one eligible direct
  incoming hit, whichever comes first;
- it does not stack with itself;
- it uses the strongest single mitigation channel rather than multiplying into immunity with every
  other defensive layer;
- it does not protect against every spell, status, terrain, or indirect effect.

The old 15% reduction was too small to feel like a real Knight button. A 30% one-hit guard is
readable and meaningful while still costing offense and action opportunity.

### Rend Armor

`Rend Armor` changes from armor-response deltas into a visible vulnerability state.

```text
damage = equipped weapon output x0.50
on hit = target gains Exposed until it completes its next action
Exposed = all incoming damage x1.25
```

Rules:

- `Exposed` is visible;
- `Exposed` is non-stacking;
- `Exposed` has the same meaning and magnitude as Squire's Exposed;
- no armor-specific invisible response delta remains in this proposal.

The intended play is a party setup window: Knight spends a melee action to open a target, then allies
cash in with physical or magical follow-up. That combo is acceptable because it costs positioning,
timing, and a Knight action.

### Rend Weapon

`Rend Weapon` replaces hidden output deltas with a visible armed-offense state.

```text
damage = equipped weapon output x0.50
on hit = target gains Disarmed until it completes its next action
Disarmed = outgoing weapon/basic weapon-skill damage x0.70
```

Rules:

- only affects enemies with meaningful weapon or basic weapon-skill offense;
- does not affect spells, item actions, pure status actions, or non-weapon monster/natural attacks
  unless a later monster pass explicitly maps them;
- does not permanently delete inventory or equipment.

The goal is to preserve the classic weapon-break fantasy without making permanent destruction the
default answer.

### Shield Break

`Shield Break` becomes a visible guard disruption tool.

```text
damage = equipped weapon output x0.50
on hit = target gains Guard Broken until it completes its next action
Guard Broken = shield and weapon guard/evasion layers x0.50
```

Rules:

- shield and weapon guard/evasion are eligible;
- class evasion, accessory evasion, rear-facing value, immunity, and generic defense are not
  affected;
- it does not become a universal "ignore defenses" button.

The x0.50 damage keeps the turn from feeling dead while still making the action a setup/control
choice instead of a damage action.

### Rend Power, Rend Magick, And Rend Speed

These Rends should stay clear and vanilla-readable as direct battle stat breaks.

```text
Rend Power = equipped weapon output x0.35; on hit target PA -2
Rend Magick = equipped weapon output x0.35; on hit target MA -2
Rend Speed = equipped weapon output x0.35; on hit target Speed -1
```

Rules:

- PA and MA breaks are battle-scoped and reset after the encounter;
- PA and MA stack at most twice, capped at -4 each;
- Speed breaks are battle-scoped and reset after the encounter;
- Speed stacks at most twice, capped at -2 Speed;
- these are stat breaks, not hidden output statuses;
- `Rend Speed` is not `Slow` and should not replace Time Mage identity.

This keeps the break fantasy legible without adding more tracked status vocabulary than the job
needs.

### Rend MP

`Rend MP` remains anti-caster resource pressure.

```text
damage = equipped weapon output x0.35
MP damage = min(30, floor(0.25 * target_max_mp))
```

Rules:

- no HP burst plan;
- no Silence replacement;
- no broad anti-magic immunity;
- useful mainly against enemies whose MP pool or spell access matters.

### Taunt

`Taunt` replaces `Challenge`.

```text
range = 3
damage = none
on hit = target gains Taunted for its next offensive action
```

Rules:

- no passive "Knight is naturally more attractive to attack" formula;
- `Taunted` must force the target to target the Knight if legal;
- if the Knight cannot be legally targeted, the enemy should approach or choose a graceful legal
  fallback rather than hard-locking;
- `Taunted` may reuse an existing Berserk-style icon/UI if that is the cleanest implementation path;
- `Taunted` must not inherit Berserk's damage boost or any attack-up side effect;
- does not bypass boss immunity or status immunity.

The important part is visible one-action target pressure, not an invisible AI rewrite.

### Crushing Blow

`Crushing Blow` is cut from this Knight package.

Its proposed anti-guard role overlaps too much with `Shield Break`, bloats an already-large kit, and
risks crowding Monk's protected crush identity. Knight can still use normal crush-capable equipment
if the equipment rules allow it, but Knight does not need a protected crush action.

## Reaction Notes

### Parry

`Parry` should be Knight's priority reaction identity.

```text
eligible trigger = frontal direct weapon hit
requirement = shield or parry-capable weapon route
effect = eligible incoming hit x0.60 when triggered
```

Rules:

- not a broad physical immunity reaction;
- no magic, status, area, rear-hit, or indirect coverage;
- equipment and facing matter;
- Brave should not be the main scaling lever. If Brave remains involved for implementation reasons,
  it should be minor or capped rather than the core optimization pressure.

This keeps Knight defense grounded in weapon/shield discipline instead of turning another reaction
into "raise Brave on everyone."

### Brace

`Brace` is cut or deferred in this provisional package.

It can return only if a later pass needs a narrow hold-ground or anti-displacement identity. It
should not become generic damage reduction.

## Support And Movement Notes

### Equip Armor

`Equip Armor` remains a build-shaping support:

- enables heavy armor use where legal;
- does not add a separate mitigation multiplier;
- consumes the support slot;
- must be watched so every fragile job does not patch itself into plate by default.

### Equip Shield

`Equip Shield` remains a build-shaping support:

- enables shield use where legal;
- does not add a separate mitigation multiplier;
- consumes the support slot;
- needs evasion/incidence validation with `Parry`, armor, and other defensive layers.

### Defensive Training

`Defensive Training` improves Knight control actions through an explicit table rather than a blind
defensive multiplier.

| Knight action | Trained result |
| --- | --- |
| `Guarded Strike` | `Guarded` can cover one additional eligible direct hit. |
| `Rend Weapon` | `Disarmed` lasts one additional target action. |
| `Rend Armor` | `Exposed` lasts one additional target action. |
| `Shield Break` | `Guard Broken` lasts one additional target action. |
| `Taunt` | +10 reliability if Taunt uses an accuracy/status check. |

It does not affect:

- ordinary attacks;
- weapon formulas generally;
- spells;
- items;
- `Rend Power`, `Rend Magick`, or `Rend Speed` stat-break caps;
- `Rend MP`;
- reactions, support skills, or movement skills;
- non-Knight command sets.

This keeps the support attractive for committed Knight-action builds without turning it into a
generic tank or damage support.

### Shield March

`Shield March` remains the Knight movement identity candidate.

Provisional direction:

```text
Move +1 while using a shield or heavy frontline posture
```

Rules:

- no terrain bypass;
- no elevation bypass;
- no universal movement export detached from armored identity;
- must be compared against late movement options so plate jobs do not become both too durable and
  too mobile for free.

## Expected Play Patterns

Healthy Knight patterns:

- active Knight anchors a flank with `Guarded Strike`, `Taunt`, and narrow Rends;
- party uses `Rend Armor` into ally burst as a deliberate combo;
- anti-weapon party uses `Rend Weapon` or PA breaks to contain a dangerous attacker;
- non-Knight builds spend support slots on `Equip Armor` or `Equip Shield` for specific defensive
  profiles;
- `Defensive Training` makes Knight secondaries better without improving every physical action.

Unhealthy Knight patterns to watch:

- `Exposed` becomes mandatory setup for every physical and magical party;
- `Guarded Strike`, `Parry`, shield, and armor stack into practical immunity;
- `Taunt` locks too many enemies or breaks boss behavior;
- PA, MA, or Speed breaks trivialize encounters through repeated stacking;
- `Equip Armor` or `Equip Shield` becomes the default fix for every fragile job.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `W4/T2.1 Populated Incidence`: count whether `Equip Armor`, `Equip Shield`, `Parry`, or
  `Defensive Training` become mandatory too early.
- `T4 accuracy/evasion`: test `Shield Break`, shield layers, `Parry`, and `Equip Shield` stacks.
- `T6 armor response`: re-check that replacing armor-specific deltas with `Exposed` does not make
  Knight setup universal.
- `F5 real-roster sweep`: test active Knight parties against magic-heavy, ranged, boss, and
  low-equipment enemy profiles.
- Control identity sweep: test `Taunt` against immunity rules and unreachable-target fallbacks.
- Combo convergence: test `Rend Armor` into ally burst, especially with strong mage follow-up.
- Defense ceiling: test `Guarded Strike` plus `Parry`, shield, armor, and defensive supports.

## Reviewer Notes

Claude approved the opening Knight direction before this artifact was written, with the following
required changes folded in:

- `Exposed` is unified as all incoming damage x1.25 across Squire self-risk and Knight enemy setup;
- `Guarded Strike` uses x0.85 damage and x0.70 one-hit `Guarded`;
- `Disarmed` and `Guard Broken` are accepted as fixed visible statuses;
- PA, MA, and Speed Rends are direct battle stat breaks, not tracked output statuses;
- `Taunt` must not inherit Berserk's damage boost;
- `Crushing Blow` is cut to keep Knight lean and protect Monk identity.

Claude accepted this artifact after review. Knight is closed for this rediscussion pass pending
Marcelo validation.
