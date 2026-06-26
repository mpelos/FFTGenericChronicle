# Cross-model pillar critic — GPT-5.5 via codex exec
Launched (UTC): Fri Jun 26 02:02:58 PM UTC 2026
---
Reading additional input from stdin...
OpenAI Codex v0.142.0
--------
workdir: /home/mpelos/Projects/FFTGenericChronicle
model: gpt-5.5
provider: openai
approval: never
sandbox: danger-full-access
reasoning effort: xhigh
reasoning summaries: none
session id: 019f043d-ac02-7032-ae30-4158c36fb785
--------
user
You are an adversarial design critic. You did NOT design this system — that is the point. Your job is to attack a set of design PILLARS, not to ratify them.

## Context
This is a clean-sheet combat system for a Final Fantasy Tactics (FFT) tactical-RPG mod, called the Deep Combat Layer (DCL). It is strongly inspired by GURPS 4e but adapted to FFT's feel; it is explicitly NOT trying to be GURPS-faithful — game balance and feel win over fidelity. FFT is a grid-based, turn-based tactical RPG: units have HP, attributes (PA physical-attack, MA magic-attack, Speed, Move, Brave, Faith, Zodiac sign), jobs/classes, equipment (weapon, armor, shield, helmet, accessory), and a CT (charge-time) turn order driven by Speed.

## What a "pillar" is
A pillar is an L0 non-negotiable: the YARDSTICK every downstream mechanic is judged against. It must be (a) genuinely load-bearing, (b) not secretly in conflict with another pillar, (c) a real design value and not an implementation detail wearing a pillar's clothes.

## The pillar set under attack
- **P1 — Contextual differentiation / no strictly-better option.** Every option, especially every weapon type, is best in some context and worse in others. An advantage on one axis is always paid for on another (a bigger hit costs defense; reach costs point-blank safety; penetration costs raw damage; scaling-with-a-stat costs a low floor). The deliverable is a roster of situational choices, never a power ranking.
- **P2 — Every existing FFT attribute is used or replaced.** Nothing in the character menu may be dead weight. Each attribute either drives a mechanic or is explicitly removed/reskinned.
- **P3 — Deterministic damage, random contest.** A confirmed hit's damage is computed, not rolled; the preview equals the result. Randomness is confined to WHETHER you hit and WHETHER the defender turns the blow aside.
- **P4 — Two-sided permanent traits.** Brave, Faith, and Zodiac are each a permanent per-unit slider with a real upside AND a real downside — no universally-best setting.
- **P5 — Legibility over hidden math.** Transparent, readable systems replace vanilla FFT's hidden multipliers (e.g. the hidden Zodiac compatibility multiplier, opaque evade stacking).
- **P6 — No new equipment.** The mod re-uses existing items only; weapons gain meaning through type, reach, and modifier, never through new items.
- **P7 (candidate, possibly implicit) — Heroic feel.** A unit fights at full effectiveness until 0 HP; no death-spiral, no HP-threshold penalties. At 0 HP, vanilla death/countdown.
- **P8 (candidate, possibly implicit) — GURPS-adapted but game-balance-first.** Borrow GURPS' rich pre-balanced formulas, but bend or drop any of it the moment FFT balance/feel demands.

## Your task — attack the pillar SET itself (not the mechanics under it)
Default to "this set is flawed." Specifically hunt for:
1. **A MISSING pillar** — a non-negotiable this system clearly needs but that isn't stated. (Think: what about turn economy / Speed dominance? AI legibility? the power-progression curve early/mid/late? the single-player-vs-PvP frame? counterplay availability to the AI? Is any of these actually a missing L0 yardstick?)
2. **Two pillars that QUIETLY CONFLICT** — where serving one forces violating the other. (E.g. does P1 "no strictly-better option" collide with P2 "every attribute used"? Does P3 determinism collide with P5 legibility or with P1? Does P5 legibility collide with the GURPS depth P8 imports? Does P6 "no new equipment" make P1 contextual-differentiation impossible to fully achieve with a fixed item list?)
3. **A pillar that is really an IMPLEMENTATION DETAIL** wearing a pillar's clothes — too specific/mechanical to be an L0 yardstick.
4. **A pillar that is UNFALSIFIABLE or unmeasurable** — so vague it can't actually adjudicate a downstream decision (is P1 measurable? how would you prove a weapon ISN'T strictly better?).
5. **A pillar stated as universal that the design will be forced to violate** in practice.

For EACH objection you raise, provide: (a) the objection in one sentence, (b) a CONCRETE scenario or example showing the problem bites, (c) a severity: BLOCKING (the set is wrong/incomplete in a way that corrupts downstream validation) or NON-BLOCKING (worth noting, not fatal), (d) a suggested fix (add pillar X / merge Y+Z / demote W / sharpen V).

End with a one-paragraph verdict: is this pillar set SOUND-ENOUGH to validate a combat system against, or does it need repair first? Be blunt. Do not flatter. If it is mostly fine, say which 1-2 objections are nonetheless worth logging as open risks.
codex
I’m using the `fft-job-redesign` skill because this is a FFTGenericChronicle combat design review. I’ll read its rules first, then critique the pillar set directly rather than drifting into mechanic design.
hook: PreToolUse
hook: PreToolUse Completed
exec
/bin/bash -lc "sed -n '1,240p' /home/mpelos/.agents/skills/fft-job-redesign/SKILL.md" in /home/mpelos/Projects/FFTGenericChronicle
 succeeded in 0ms:
---
name: fft-job-redesign
description: "Use this skill whenever redesigning, reviewing, revising, or documenting a Final Fantasy Tactics / FFTGenericChronicle job, skillset, job-balance artifact, or combat/campaign job mechanic. Trigger for prompts about job redesign, job balance, Squire/Chemist/Knight/Archer/Monk/Thief/Orator/etc., action skills, reactions/support/move skills, monster recruitment, Brave/Faith policy, Poach, visibility of statuses/marks, whether a proposed skill is useful, or whether a vanilla skill belongs in a different job. This skill enforces a repeatable process: diagnose vanilla, audit dependent global systems, define the job compass, explicitly ask whether a non-vanilla mechanic would be much cooler for the job identity, separate identity from the vanilla skill list, preserve essential FFT systems, generate ambitious redesign options, audit every skill by value layer, check battle/campaign coherence, keep mechanics readable, and produce a reviewable proposal with validation gates."
---

# FFT Job Redesign

Use this skill to redesign or review a job as a game-design system, not as a loose list of cool
abilities. The goal is to produce a job that feels like a better FFT job: strong identity,
meaningful build planning, useful turns, readable effects, preserved long-term systems, and enough
creative ambition that the job is not just "vanilla, but numbers changed."

This skill is written for FFTGenericChronicle, but the process also applies to FFT-like job systems.

## Core Principle

Every skill must justify the resource it consumes in the layer where it exists.

- A combat skill must justify the combat turn, CT, MP, risk, setup, or target restriction it
  consumes.
- A campaign/economy/roster skill does not need to beat attacking in battle, but it must create
  meaningful long-term value.
- A reaction/support/movement skill must justify its slot and not become a universal default.
- A mechanic can be creative, but any persistent status, mark, zone, wound, charge, or flag must be
  visible and understandable to the player.
- Preserve FFT identity, not necessarily every vanilla skill name. A skill list can change if the
  job's fantasy, systems, and player-facing texture are better served.
- Do not remove core FFT systems by accident. If a skill supports monster recruitment, breeding,
  Poach, Brave/Faith roster planning, permanent recruitment, item/economy routing, equipment theft,
  or map treasure, preserve a route for that system or explicitly defer it with an access promise.
- When changing an existing skill, first identify every mechanic the old skill carried. A weak
  combat action may still be the only route for a build, roster, economy, recruitment, monster,
  equipment, status, map, or long-term planning mechanic. Reworking the battle effect is allowed;
  silently deleting the carried mechanic is not.
- Balance primarily inside battle. Do not add external/campaign restrictions, custom caps,
  per-battle limits, custom permanent-stat rates, recruitment friction, economy friction, or extra
  grind friction just because they make balance easier. If a rule makes it harder for the player to
  build the character they want, it must create a clear player-facing benefit. Otherwise, preserve
  the vanilla external mechanic and tune the battle-facing effect instead.

## Required Context Read

When working inside the FFTGenericChronicle repo, read the smallest current set that applies:

1. `docs/job-balance/00-high-level-direction.md`
2. `docs/job-balance/01-cross-job-build-principles.md`
3. `docs/job-balance/02-job-design-protocol.md`
4. `docs/job-balance/03-job-roster-and-role-map.md`
5. The current job artifact, if it exists.
6. The relevant vanilla reference sections:
   - `docs/reference/fft-vanilla-command-skillset-effect-map.md`
   - `docs/reference/fft-vanilla-ability-effect-index.md`
   - `docs/reference/fft-vanilla-status-effect-map.md`

If the user asks only for a conceptual review, do not overread the whole repo. Read enough to avoid
inventing facts.

## Workflow

Work one job at a time unless the user explicitly asks for a cross-job plan.

### 1. Diagnose Vanilla

Start by understanding the original job.

Answer:

- What fantasy does the vanilla job promise?
- What parts already work or are beloved?
- What parts are weak, tedious, redundant, hidden, overpowered, or campaign-only?
- Which skills are actually combat tools?
- Which skills support campaign, economy, roster, monster, breeding, Poach, recruitment, equipment,
  map treasure, or long-term build systems?
- What would players miss if the job were simplified too aggressively?

This stage prevents "fixing" a bad skill by deleting the system it carried.

### 2. Audit Dependent Global Systems

Before deciding skills, identify the broader systems the job relies on.

Examples:

- Orator: Brave, Faith, recruitment, monster recruitment, social statuses, guns.
- Knight: equipment break, armor identity, weapon access, defensive stats.
- Archer: range, charge time, CT, line of sight, height, bows/crossbows.
- Chemist: items, healing scaling, item economy, revive/status recovery.
- Thief: stealing, Poach, loot, economy, mobility, enemy gear.

For each system, write:

```text
System:
How this job uses it:
Current policy / assumption:
What is stable:
What is still open:
Design consequence for this job:
```

Open global-system questions do not automatically block a job proposal. They must be made explicit
so the proposal does not pretend the foundation is settled. The artifact can still propose a clear
direction, but it should name which decisions depend on later system policy.

### 3. Define The Job Compass

Write a short compass before designing skills.

The compass must answer:

- What should this job be in the mod?
- When should the player want to use it?
- What should it be better at than other jobs?
- What weaknesses should it keep?
- What combat and campaign systems must it preserve?
- What should it not become?

Use this as the decision filter. If a skill does not serve the compass, rework, move, merge, cut, or
replace it.

Before inspecting the vanilla skill list as a constraint, ask the creative identity question:

```text
Is there something this job should do that would be much cooler, more tactical, or more memorable
for its identity, even though vanilla does not have it?
```

If the answer is yes, discuss it explicitly before locking the skill inventory. Name the strongest
non-vanilla mechanic candidates, explain why they fit the job, and only reject them if they fail
FFT feel, protected-system, visibility, complexity, value-layer, or balance checks. Do not let the
vanilla list consume the job's design space before this question is answered.

### 4. Separate Identity From Vanilla Skill Names

Do not confuse preserving a job with preserving its exact vanilla skill list.

Write two lists:

```text
Identity pillars:
- ...

Vanilla names that are truly important:
- ...

Vanilla names that are optional nostalgia:
- ...
```

Ask:

- Is this skill name the best expression of the job fantasy, or are we keeping it out of habit?
- Would a new skill express the same identity more clearly?
- Would this skill make more sense in another job?
- Does keeping the name force incoherent mechanics?

Preserve recognizable FFT texture where it helps the player feel the job, but do not let nostalgia
protect weak or misplaced mechanics.

### 5. List Protected Systems

Explicitly list systems that must not disappear during this job pass.

Examples:

- monster recruitment and breeding;
- Poach and monster-item routing;
- permanent recruitment;
- Brave/Faith roster and build planning;
- item economy;
- equipment theft and special enemy gear planning;
- map treasure or exploration hooks;
- job-specific weapons or equipment access.

If any protected system is out of current scope, write an access promise:

```text
System:
Current job-pass decision:
How the player will still access it:
Future pass required:
What must not be removed now:
```

### 6. Preserve Good Mechanics Before Simplifying

When revising an older proposal, do not throw away good ideas only because the old version was too
complex.

Create a mechanic preservation list:

| Mechanic | Why it was good | What was too complex | Simpler visible form | Keep / merge / move / cut |
| --- | --- | --- | --- | --- |

Knight and Archer are caution examples: strong ideas can fail if they ask the player to track too
many custom marks or statuses. The correct response is to preserve the useful tactical idea while
making the visible state simpler.

### 7. Generate Ambitious Alternatives

Before locking the kit, run a deliberate ambition pass.

Answer:

- If we were not attached to the vanilla skill list, what are 3-5 stronger mechanics this job could
  own while still feeling like FFT?
- Which vanilla skills would those mechanics replace, merge, or move?
- Which ideas are too complex, too invisible, or too far from FFT?
- Which idea is the most promising version of the job fantasy?

This pass prevents a redesign from becoming only "vanilla with better numbers." Be creative, then
converge back through readability, protected systems, and job identity.

### 8. Check Thematic Ownership

For every skill, ask whether this job is the right owner.

Examples:

- A charm/seduction/social-conversion skill may belong to Orator more than Thief.
- A loot/equipment/economy action may belong to Thief more than Orator.
- A Brave/Faith action may require a Brave/Faith policy before its exact effect is satisfying.

Use these decisions:

- `Keep here`: the skill belongs in this job.
- `Move`: another job expresses the skill better.
- `Share with distinction`: two jobs can have related effects if delivery, cost, target, payoff, or
  risk are clearly different.
- `Replace`: the name/slot is not the right expression.

The goal is not tidiness. The goal is that each job owns the mechanics that make the most thematic
and tactical sense.

### 9. Inventory Every Skill

Classify every vanilla and proposed skill.

Before changing, moving, merging, replacing, or removing a vanilla skill, write a carried-mechanic
audit:

hook: PostToolUse
hook: PostToolUse Completed
hook: PreToolUse
hook: PreToolUse Completed
exec
/bin/bash -lc "sed -n '241,520p' /home/mpelos/.agents/skills/fft-job-redesign/SKILL.md" in /home/mpelos/Projects/FFTGenericChronicle
 succeeded in 0ms:

```text
Original skill:
Old battle function:
Old build/campaign/system function:
Who used this and why:
What breaks if this exact mechanic disappears:
Replacement route or explicit access promise:
```

This is required even for skills that looked weak. Bad combat value is not proof that the skill had
no purpose elsewhere. For example, a Brave/Faith speech skill can be a weak battle turn while still
being the player's long-term route for shaping caster or Brave-scaling builds.

Allowed decisions:

- `Keep`: the existing skill already works.
- `Rework`: the fantasy is right, but the mechanics are weak or unreadable.
- `Merge`: multiple skills serve the same tactical/campaign role.
- `Replace`: the slot/name can serve the job better with a new effect.
- `Move`: the skill belongs in another job or system.
- `Share with distinction`: keep a similar idea in more than one job with clear differences.
- `Defer with access promise`: the current pass cannot solve it, but the system remains protected.
- `Remove`: only if it does not preserve fantasy, combat value, campaign value, build value, or a
  protected system.

Never use "out of scope" as a silent delete.

### 10. Write A Skill Card For Each Skill

Every skill needs a card. Use the correct value layer.

```text
Skill:
Source: vanilla / old proposal / new proposal
Decision: keep / rework / merge / replace / move / share / defer / remove
Value layer: combat / campaign / economy / roster / monster / build / reaction / support / movement
Previous mechanics carried:
Function:
Why the player uses it:
Immediate or durable impact:
Visibility:
Cost and constraints:
What prevents spam or mandatory grind:
What breaks if removed:
Replacement route for any old build/campaign/system function:
Verdict:
```

For combat-facing actions, add:

```text
Why use this instead of attacking, killing, healing, or moving to safety?
What target or battle state makes this the right turn?
What payoff window does it create?
```

For campaign-facing actions, add:

```text
What long-term system does this support?
Why is the long-term value worth the turn, JP, risk, or opportunity cost?
How does this avoid becoming mandatory grind?
Does this preserve existing external/campaign mechanics unless changing them has a clear benefit?
```

For reactions/support/movement, add:

```text
Why is this worth the slot?
Which builds want it?
Which builds should not want it?
What prevents it from becoming universal?
```

For skills with both battle and campaign effects, add:

```text
Do the battle effect and campaign effect tell the same story?
Can the target state at the end of battle contradict what happened during battle?
If the campaign payoff is permanent, what visible in-battle event justifies it?
```

### 11. Audit Permanent And Roster-Affecting Effects

Any permanent stat, roster, recruitment, economy, or loot change needs explicit policy.

Answer:

- Can this skill increase a permanent value?
- Can it decrease or damage a permanent value?
- Can the player accidentally ruin a unit?
- Is the change reversible?
- Is there a cap, floor, or convergence band?
- Does it affect recruited units differently from temporary battle units?
- Does it create mandatory grind?
- Is the permanent result visible after battle?
- Does the proposed rule make desired builds harder to assemble?
- What does the player gain from changing the external mechanic instead of balancing the battle
  effect directly?

If the user has given a policy, follow it. Example: if permanent Brave/Faith loss is rejected,
design only permanent gain/repair routes and keep negative effects battle-scoped.

Do not invent custom caps, floors, per-battle limits, permanent-stat rates, recruitment friction,
JP friction, or economy friction unless the change solves a real player-facing problem. "It is
easier to balance" is not enough. The default posture is to preserve external mechanics and tune the
combat effect, access timing, visibility, cost, reliability, target restrictions, or counterplay
instead.

### 12. Visibility, Complexity, And Constraint Audit

Every persistent effect must be readable.

For each status, mark, wound, charge, tile zone, forced-target rule, delayed threat, conditional
flag, cooldown, or per-round/per-battle limitation, answer:

- What is the visible representation?
- Who or what carries it: unit, tile, equipment slot, action, or party state?
- How long does it last?
- Does it stack?
- Who can consume it?
- What happens if the target dies, moves, becomes illegal, is cured, or the source changes jobs?
- Can a normal player track this during FFT combat without checking external notes?
- Is this limitation intuitive, or is it only an invisible balance patch?

If the answer is weak, simplify:

- reuse a vanilla visible status;
- use a direct stat, CT, HP, MP, equipment, or position change;
- make the effect immediate;
- balance through chance, cost, range, target restriction, CT, MP, risk, or immunity instead of
  invisible cooldowns;
- merge similar states;
- cut the mechanic.

Use a small signature vocabulary per job. A normal job should not require many overlapping custom
states with different durations and consumption rules.

### 13. Assemble The Kit

After individual cards pass, inspect the whole job.

Check:

- Does the active job have useful turns at first access, midgame, and late game?
- Does the job have a fallback when its main mechanic is resisted?
- Are combat skills and campaign skills clearly separated?
- Are battle/campaign dual effects coherent?
- Is there redundant skill bloat?
- Are there stronger new ideas that should replace nostalgia-only skills?
- Are the strongest skills visible and emotionally satisfying?
- Does the job preserve FFT identity without becoming mandatory?
- Does it have clear weaknesses and counters?
- Does the kit still feel like FFT, not a different game?

### 14. Run The Gates

Use these gates before accepting a proposal:

| Gate | Pass condition |
| --- | --- |
| Vanilla diagnosis | The proposal explains what the original job promised and what failed. |
| Dependent systems | The proposal names global systems it relies on and states current assumptions. |
| Job compass | The proposal defines what the job should be in the mod. |
| Creative identity question | The proposal asks whether a much stronger non-vanilla identity mechanic exists and discusses it before locking the kit. |
| Identity vs skill list | The proposal preserves job identity without blindly preserving every vanilla name. |
| Ambition pass | The proposal considered stronger non-vanilla mechanics before converging. |
| Carried-mechanic audit | Changed vanilla skills name the old battle, build, campaign, and system functions they carried. |
| Protected systems | Core FFT systems are preserved or explicitly deferred with an access promise. |
| Thematic ownership | Skills are owned by the job that best expresses them, or shared with clear distinction. |
| Skill value | Every skill justifies its turn, slot, JP, item stock, or campaign opportunity. |
| Combat action value | Combat actions explain when they beat attacking, killing, healing, or repositioning. |
| Campaign value | Campaign/economy/roster skills explain durable game value and anti-grind guardrails. |
| Battle/campaign coherence | Dual-layer skills do not contradict their own story or end-state. |
| Permanent-effect policy | Permanent gains/losses/recruitment/loot changes obey an explicit policy. |
| External-mechanic friction | Any campaign/external rule that makes builds harder names a clear player-facing benefit. |
| Visibility | Persistent effects and constraints are visible and understandable. |
| Complexity budget | Custom states and special limitations are few, simple, and trackable. |
| Mechanic preservation | Good prior mechanics are simplified before being cut. |
| Build texture | The job creates builds, not one universal answer. |
| Weaknesses | The job has real counters and bad matchups. |
| No hidden removal | No beloved FFT subsystem disappears by accident. |

If a gate fails, mark the proposal `Needs revision` and record why. Do not paper over the failure
with "pending validation" language. If a gate depends on a later system decision, state the
assumption and the expected consequence if that assumption changes.

### 15. Reviewer Loop

If the workflow includes a second AI reviewer, use them as a blocking reviewer.

Recommended loop:

1. Draft diagnosis, dependent systems, compass, protected systems, and skill inventory.
2. Send to reviewer for critique before locking the kit.
3. Draft ambitious alternatives and thematic ownership decisions.
4. Send to reviewer if the job is identity-sensitive or likely to preserve too much vanilla.
5. Draft skill cards and kit assembly.
6. Send to reviewer again.
7. Revise until both sides can explain why every skill passes its intended value layer and why the
   final kit is the strongest expression of the job identity.
8. Only then write or mark the final job artifact as accepted.

The reviewer should challenge weak turns, invisible state, accidental subsystem removal, redundant
skills, misplaced skill ownership, stale vanilla preservation, battle/campaign incoherence, and
over-complex mechanics.

### 16. Human Validation

Before moving to the next job, present a concise summary to the human:

- job compass;
- dependent systems and assumptions;
- final skill list;
- what each skill does;
- why each skill exists;
- which vanilla skills were kept, moved, replaced, or removed;
- protected systems and access promises;
- mechanics preserved from prior drafts;
- ambitious alternatives considered;
- risky decisions that need human validation.

The human's rejection is authoritative design feedback. If they reject the package, mark the job
artifact `Needs revision after human validation` and update the process notes so the failure does
not repeat.

## Output Template

Use this structure for a job artifact or design brief:

```markdown
# [Job] Redesign V[version]

Status:
Date:
Scope: [Job] only

## Vanilla Diagnosis
## Dependent Global Systems
## Job Compass
## Identity Pillars Versus Vanilla Skill Names
## Protected Systems And Access Promises
## Mechanic Preservation List
## Ambitious Alternatives Considered
## Thematic Ownership Decisions
## Skill Inventory
## Skill Cards
## Permanent And Roster-Affecting Policy
## Kit Assembly
## Visibility And Complexity Audit
## Validation Gates
## Expected Player Use
## Weaknesses And Counters
## Open Risks
## Reviewer Notes
## Human Validation Notes
```

## Common Failure Modes

- Making all skills weak because the designer is afraid of imbalance.
- Treating "out of scope" as permission to remove an essential FFT system.
- Designing combat debuffs that are worse than attacking.
- Designing campaign skills with no real long-term payoff.
- Preserving a skill name while deleting the reason players cared about it.
- Reworking a skill's combat effect while accidentally deleting its build, campaign, or system role.
- Preserving the whole vanilla skill list when the job identity would be stronger with new skills.
- Moving too fast past an unstable global system without naming the assumption.
- Giving a skill battle and campaign effects that contradict each other.
- Letting permanent stat loss accidentally ruin a character.
- Adding external/campaign friction that makes desired builds harder without a real player benefit.
- Putting a skill in the wrong job because vanilla placed it there.
- Adding invisible cooldowns, per-round clauses, marks, or special exceptions as balance patches.
- Adding many clever marks that the player cannot see or remember.
- Cutting good mechanics instead of simplifying them.
- Letting a support/reaction/move become the obvious best choice for most builds.
- Declaring consensus before each skill has passed its value layer.

## Fast Review Checklist

hook: PostToolUse
hook: PostToolUse Completed
hook: PreToolUse
hook: PreToolUse Completed
exec
/bin/bash -lc "sed -n '521,760p' /home/mpelos/.agents/skills/fft-job-redesign/SKILL.md" in /home/mpelos/Projects/FFTGenericChronicle
 succeeded in 0ms:

Before accepting any job package, verify:

- [ ] The vanilla diagnosis is accurate.
- [ ] Dependent global systems and assumptions are named.
- [ ] The job compass is clear.
- [ ] The proposal asks whether the job needs a much cooler non-vanilla identity mechanic before locking the vanilla skill list.
- [ ] Job identity is separated from vanilla skill-name nostalgia.
- [ ] Every changed vanilla skill has a carried-mechanic audit.
- [ ] Protected systems are listed.
- [ ] Ambitious alternatives were considered.
- [ ] Thematic ownership was checked.
- [ ] Every skill has a decision and value layer.
- [ ] Combat actions justify the combat turn.
- [ ] Campaign actions justify long-term value.
- [ ] Dual battle/campaign effects are coherent.
- [ ] Permanent or roster-affecting effects have policy.
- [ ] External/campaign mechanics are preserved unless changing them clearly improves play.
- [ ] R/S/M skills justify their slot.
- [ ] Persistent states and constraints are visible.
- [ ] Complexity is trackable.
- [ ] Good prior mechanics were preserved or deliberately cut with cause.
- [ ] The job has clear weaknesses.
- [ ] The human can understand the proposed kit without reading hidden math.

hook: PostToolUse
hook: PostToolUse Completed
codex
**Objections**

1. **Missing pillar: turn economy dominance is not controlled.**  
Scenario: if Speed/CT remains even slightly too efficient, a dagger user with more turns beats a slower polearm/axe user despite all weapon-context work.  
Severity: **BLOCKING**.  
Fix: add a pillar: **“Action economy is the primary balance constraint; no damage, defense, reach, or stat package may dominate by simply converting into more turns without hard tradeoffs.”**

2. **Missing pillar: enemy/AI counterplay is absent.**  
Scenario: a weapon or trait is “situational” only because a human can exploit facing, reach, Brave/Faith tuning, or Zodiac matchup, while AI units cannot recognize or answer it. The system becomes player-favored puzzle solving, not tactical combat.  
Severity: **BLOCKING**.  
Fix: add a pillar: **“Every major mechanic must be legible and usable by enemies, or explicitly balanced as player-only.”**

3. **P1 quietly conflicts with P6.**  
Scenario: “no strictly-better option” across every weapon type is hard when the mod cannot add new items and must reuse FFT’s existing item progression, availability, prices, hands, ranges, and animations. Some vanilla weapons will naturally become tier upgrades unless the system rewrites their meaning heavily.  
Severity: **BLOCKING**.  
Fix: narrow P1 to **weapon families and same-tier choices**, not every individual item; state that item tiers may progress, while weapon types must preserve contextual identities.

4. **P1 is too universal to be falsifiable.**  
Scenario: to prove a spear is not strictly better than a sword, you need agreed test contexts: level bands, jobs, maps, enemy defenses, Brave/Faith/Zodiac, shields, reactions, and AI use. Without that, any designer can invent a context where a weak option is “best.”  
Severity: **BLOCKING**.  
Fix: sharpen P1 with validation language: **each weapon type must have at least N common, player-relevant winning contexts across early/mid/late game, and at least N common losing contexts.**

5. **P2 can become implementation pressure disguised as a value.**  
Scenario: “every existing FFT attribute is used” may force Brave, Faith, Zodiac, PA, MA, Speed, Move, etc. into mechanics even when the cleaner design would remove or collapse one. The pillar protects menu completeness more than combat quality.  
Severity: **NON-BLOCKING**, unless it blocks cuts.  
Fix: rewrite as **“No displayed attribute may mislead the player; dead stats are removed, renamed, or made strategically meaningful.”**

6. **P3 is an implementation detail wearing pillar clothes.**  
Scenario: deterministic damage may be good, but it is a specific resolution policy, not an L0 design value. The real value is preview trust and bounded variance. A fixed-damage rule could still produce unreadable or degenerate outcomes.  
Severity: **NON-BLOCKING**.  
Fix: demote P3 under a broader pillar: **“Combat previews are trustworthy; randomness is bounded, surfaced, and concentrated where it creates tactical tension.”**

7. **P4 and P1 quietly conflict through permanent optimization.**  
Scenario: if low Faith improves magic defense and high Faith improves casting, one value band may still dominate for most physical jobs. If high Brave improves reactions and offense, low Brave must receive a downside/upside strong enough that it may become weird or artificial.  
Severity: **BLOCKING**.  
Fix: merge P4 into a broader **permanent-build-tradeoff pillar** and require archetype-specific validation: no Brave/Faith/Zodiac setting may be best for all builds, all jobs, or all campaign phases.

8. **P5 conflicts with P8 unless complexity has a budget.**  
Scenario: GURPS-style imports can easily create layered contests, penetration, defense choice, reach states, facing, shields, armor, and trait sliders. Even if every formula is visible, the total mental load may stop being FFT-readable.  
Severity: **BLOCKING**.  
Fix: add a pillar or clause: **“Depth must fit FFT’s tactical bandwidth; visible complexity still fails if the player cannot predict outcomes during normal play.”**

9. **P7 is probably a real pillar, but needs to be explicit.**  
Scenario: once armor, wounds, penetration, or GURPS inspiration enter the design, it is very tempting to add injury penalties, crippled limbs, morale collapse, or HP thresholds. That would change FFT’s heroic feel immediately.  
Severity: **BLOCKING if omitted**, because downstream mechanics will drift.  
Fix: promote P7: **“No death spiral: HP is survival only; combat effectiveness does not degrade before 0 HP unless caused by explicit statuses.”**

10. **P8 is unfalsifiable as written.**  
Scenario: “borrow GURPS but balance first” can justify either importing a complex rule or deleting it. It does not adjudicate anything by itself.  
Severity: **NON-BLOCKING**, but weak.  
Fix: demote it to design method, or rewrite as: **“GURPS is a source of candidate mechanics, not authority; any imported rule must pass FFT legibility, AI, action-economy, and progression gates.”**

**Verdict**

This pillar set is **not sound enough yet** to validate a combat system against. The major failures are missing action-economy control, missing AI/counterplay validation, and an overbroad P1 that cannot be tested as stated. Repair those first. P7 should be promoted, P8 should be demoted, and P1/P4 need measurable validation gates instead of aspirational language.
hook: Stop
hook: Stop Completed
tokens used
25,817
**Objections**

1. **Missing pillar: turn economy dominance is not controlled.**  
Scenario: if Speed/CT remains even slightly too efficient, a dagger user with more turns beats a slower polearm/axe user despite all weapon-context work.  
Severity: **BLOCKING**.  
Fix: add a pillar: **“Action economy is the primary balance constraint; no damage, defense, reach, or stat package may dominate by simply converting into more turns without hard tradeoffs.”**

2. **Missing pillar: enemy/AI counterplay is absent.**  
Scenario: a weapon or trait is “situational” only because a human can exploit facing, reach, Brave/Faith tuning, or Zodiac matchup, while AI units cannot recognize or answer it. The system becomes player-favored puzzle solving, not tactical combat.  
Severity: **BLOCKING**.  
Fix: add a pillar: **“Every major mechanic must be legible and usable by enemies, or explicitly balanced as player-only.”**

3. **P1 quietly conflicts with P6.**  
Scenario: “no strictly-better option” across every weapon type is hard when the mod cannot add new items and must reuse FFT’s existing item progression, availability, prices, hands, ranges, and animations. Some vanilla weapons will naturally become tier upgrades unless the system rewrites their meaning heavily.  
Severity: **BLOCKING**.  
Fix: narrow P1 to **weapon families and same-tier choices**, not every individual item; state that item tiers may progress, while weapon types must preserve contextual identities.

4. **P1 is too universal to be falsifiable.**  
Scenario: to prove a spear is not strictly better than a sword, you need agreed test contexts: level bands, jobs, maps, enemy defenses, Brave/Faith/Zodiac, shields, reactions, and AI use. Without that, any designer can invent a context where a weak option is “best.”  
Severity: **BLOCKING**.  
Fix: sharpen P1 with validation language: **each weapon type must have at least N common, player-relevant winning contexts across early/mid/late game, and at least N common losing contexts.**

5. **P2 can become implementation pressure disguised as a value.**  
Scenario: “every existing FFT attribute is used” may force Brave, Faith, Zodiac, PA, MA, Speed, Move, etc. into mechanics even when the cleaner design would remove or collapse one. The pillar protects menu completeness more than combat quality.  
Severity: **NON-BLOCKING**, unless it blocks cuts.  
Fix: rewrite as **“No displayed attribute may mislead the player; dead stats are removed, renamed, or made strategically meaningful.”**

6. **P3 is an implementation detail wearing pillar clothes.**  
Scenario: deterministic damage may be good, but it is a specific resolution policy, not an L0 design value. The real value is preview trust and bounded variance. A fixed-damage rule could still produce unreadable or degenerate outcomes.  
Severity: **NON-BLOCKING**.  
Fix: demote P3 under a broader pillar: **“Combat previews are trustworthy; randomness is bounded, surfaced, and concentrated where it creates tactical tension.”**

7. **P4 and P1 quietly conflict through permanent optimization.**  
Scenario: if low Faith improves magic defense and high Faith improves casting, one value band may still dominate for most physical jobs. If high Brave improves reactions and offense, low Brave must receive a downside/upside strong enough that it may become weird or artificial.  
Severity: **BLOCKING**.  
Fix: merge P4 into a broader **permanent-build-tradeoff pillar** and require archetype-specific validation: no Brave/Faith/Zodiac setting may be best for all builds, all jobs, or all campaign phases.

8. **P5 conflicts with P8 unless complexity has a budget.**  
Scenario: GURPS-style imports can easily create layered contests, penetration, defense choice, reach states, facing, shields, armor, and trait sliders. Even if every formula is visible, the total mental load may stop being FFT-readable.  
Severity: **BLOCKING**.  
Fix: add a pillar or clause: **“Depth must fit FFT’s tactical bandwidth; visible complexity still fails if the player cannot predict outcomes during normal play.”**

9. **P7 is probably a real pillar, but needs to be explicit.**  
Scenario: once armor, wounds, penetration, or GURPS inspiration enter the design, it is very tempting to add injury penalties, crippled limbs, morale collapse, or HP thresholds. That would change FFT’s heroic feel immediately.  
Severity: **BLOCKING if omitted**, because downstream mechanics will drift.  
Fix: promote P7: **“No death spiral: HP is survival only; combat effectiveness does not degrade before 0 HP unless caused by explicit statuses.”**

10. **P8 is unfalsifiable as written.**  
Scenario: “borrow GURPS but balance first” can justify either importing a complex rule or deleting it. It does not adjudicate anything by itself.  
Severity: **NON-BLOCKING**, but weak.  
Fix: demote it to design method, or rewrite as: **“GURPS is a source of candidate mechanics, not authority; any imported rule must pass FFT legibility, AI, action-economy, and progression gates.”**

**Verdict**

This pillar set is **not sound enough yet** to validate a combat system against. The major failures are missing action-economy control, missing AI/counterplay validation, and an overbroad P1 that cannot be tested as stated. Repair those first. P7 should be promoted, P8 should be demoted, and P1/P4 need measurable validation gates instead of aspirational language.
