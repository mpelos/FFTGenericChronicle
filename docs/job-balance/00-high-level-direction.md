# Job Balance High-Level Direction V0

Status: Accepted high-level direction
Date: 2026-06-20

## Purpose

This document records the high-level direction for the job rebalance phase of Generic Chronicle.

It is not a final job design, skill list, JP table, prerequisite tree, or numeric balance sheet.
Its purpose is to preserve the user's design intent before GPT and Claude begin detailed job
design, review, simulation, and documentation.

The next job-balance work must use this document as the design boundary.

## Design North Star

The goal is not perfect competitive balance.

The goal is to preserve the pleasure of FFT build planning while making far more jobs and builds
feel powerful, distinct, and worth using.

The player should feel:

- "I built something strong."
- "This job has a real reason to exist."
- "This weapon, armor, reaction, support, movement, and secondary choice matters."
- "This still feels like FFT, only better balanced."

The mod should prevent monotony, not prevent power. Strong builds are desirable. The problem is
only when one build is too universal, too automatic, too cheap, or makes the rest of the system
feel pointless.

## Core Principles

### Preserve FFT Identity

The job system architecture must remain recognizably FFT:

- active job;
- secondary skillset;
- reaction slot;
- support slot;
- movement slot;
- job unlock progression;
- JP investment;
- equipment identity;
- cross-job build planning.

The mod may rewrite skills, stats, equipment access, JP costs, prerequisites, and job roles, but
it must not remove the core FFT feeling of constructing a character from multiple jobs.

### Jobs Are Tactical Identities

Each job should have a real tactical identity. A job should not exist only because it existed in
vanilla FFT.

Each job design should eventually answer:

- central fantasy;
- primary combat function;
- secondary combat function;
- weapon and armor synergy;
- main attributes used;
- what the job does that no other job does;
- what the job is bad at;
- what counters or pressures the job;
- why the job is useful early, mid, or late;
- why the job may still matter in late game.

Jobs should be handled case by case. There is no fixed rule that early jobs must become bad, or
that advanced jobs must always be better in every way.

### Progression Still Matters

Advanced jobs can represent progression. They may offer:

- stronger global build pieces;
- deeper toolkits;
- broader flexibility;
- more complex mechanics;
- more rewarding support/reaction/movement skills.

However, progression should not make earlier jobs irrelevant by default. A basic or intermediate
job can still have excellent skills if those skills support its identity.

### Action Abilities Are Tactical, Not Pure Vertical Upgrades

Action abilities should mostly be horizontal tactical choices.

An action ability should be chosen because of situation, target, range, cost, area, CT, element,
status, weapon synergy, armor matchup, risk, or utility. It should not normally be a simple ladder
where the later ability always replaces the earlier one.

Reaction, support, and movement skills may carry clearer vertical progression, because they are
long-term build rewards. Even then, powerful passive pieces must have real opportunity cost,
late access, high JP cost, or clear counters.

### Build Crafting Is Sacred

One of the best parts of FFT is planning a character across multiple jobs:

- secondary from one job;
- reaction from another;
- support from another;
- movement from another;
- equipment access from another;
- active job as the final shell.

This must remain a central pleasure of the mod.

Every job should function on its own, but the highest power ceiling should come from deliberate
cross-job combinations. A strong build should feel earned, not accidental.

Allowed:

- very strong specialized builds;
- very strong late-game builds;
- builds that reward heavy JP investment;
- builds that solve several problems but still have counters.

Not allowed:

- builds that ignore map, target, CT, MP, chance, Faith, Brave, armor, equipment, and positioning;
- builds that make most other choices feel foolish;
- skills that become mandatory in nearly every serious build.

## Scope

### In Scope For This Phase

This phase covers:

- all generic jobs;
- full active skillset redesign where needed;
- reaction, support, and movement redesign;
- JP cost rebalance;
- equipment compatibility by job;
- job multipliers;
- simplified job growth profiles;
- prerequisites when changing them makes sense;
- Calculator replacement;
- Mime replacement;
- Ramza's unique job progression by chapter.

### Out Of Scope For This Phase

This phase does not cover:

- non-Ramza unique character jobs, such as Holy Knight, Sword Saint, Engineer, Divine Knight, and
  similar special jobs;
- monsters;
- individual equipment balance beyond job compatibility;
- battle/map encounter design;
- final numeric implementation data.

Unique character jobs can be revisited in a later phase. For now, the job rebalance should assume
they remain mostly unchanged, except where future integration work proves that a small compatibility
adjustment is required.

## Replacement Jobs

### Calculator Becomes Necromancer

Calculator will not be preserved.

The replacement job is Necromancer.

High-level direction:

- late-game job;
- dark caster identity;
- death, drain, curse, doom, poison, undead, debuff, and life/death manipulation themes;
- may include corpse or undead interaction if it can be made tactically healthy;
- should not recreate Calculator's problem of solving the whole map through an abstract global
  rule.

The exact Necromancer kit is intentionally not decided yet. It should be one of the last jobs
designed, after the generic job ecosystem is clearer.

### Mime Becomes Vanguard

Mime will not be preserved.

The replacement job is a new vanguard-style job.

High-level direction:

- late-game job;
- should feel comparable in value to a Holy Knight, but different in role;
- should not simply be another Holy Sword clone;
- exact identity, skillset, equipment, and role remain open for GPT and Claude to decide later.

Like Necromancer, this job should be designed late, after the normal job roster has a stable role
map.

### Ramza Gets A Rewritten Hybrid Job

Ramza's job should be rewritten with a clear protagonist identity.

High-level direction:

- hybrid knight/mage identity;
- works with knight-style equipment and magical scaling;
- has some knight-only style skills;
- has some mage-only style skills;
- has some hybrid PA/MA style skills;
- can include leadership/support identity where appropriate;
- evolves by chapter;
- by the last chapter, should be comparable to the strongest jobs in the game.

Ramza should feel special because of flexibility, progression, and identity. He should not erase
the value of generic jobs.

Ramza is an exception by character identity, not by gender.

## Growth And Stats

### Simplified Growth Profiles

Job growth should no longer be a hidden optimization trap.

The player should be able to use the character and job they want without permanently ruining the
unit through bad level-up choices.

There should be only three broad growth profiles:

- physical;
- magical;
- hybrid.

No profile should be strictly better than the others. Each supports a different kind of build.

Jobs still determine the growth profile when a unit levels up, but jobs inside the same broad
profile should not have hidden superior/inferior permanent growth.

### Multipliers Still Define Current Job Feel

Simplifying growth does not mean jobs feel the same.

Current job multipliers should remain a major identity lever:

- physical jobs can have stronger PA, HP, equipment, or durability;
- magical jobs can have stronger MA, MP, Faith synergy, or spell identity;
- hybrid jobs can blend PA and MA without necessarily beating specialists at both;
- fast jobs can express speed and mobility;
- durable jobs can express armor, shields, HP, and battlefield presence.

Growth should be fair and readable. Multipliers should make the currently equipped job feel
distinct.

## Gender Policy

Gender should not create hidden mechanical superiority.

Removed as mechanical differentiators:

- equipment restrictions by gender;
- stat differences by gender;
- growth differences by gender;
- multiplier differences by gender;
- access differences to important global build pieces.

Gender may remain visual, aesthetic, or narrative.

### Bard And Dancer Exception

Bard and Dancer are the only accepted job-access gender exception:

- Bard remains male-only;
- Dancer remains female-only;
- their action abilities remain different;
- their reaction, support, and movement skills must be exactly the same.

This preserves the FFT tradition of Bard/Dancer while ensuring both genders can access the same
global build pieces.

## Equipment Compatibility

Equipment access is part of job design, not a later cosmetic pass.

Each job should eventually define:

- weapon families available;
- armor classes available;
- shield access;
- robe/clothing/heavy armor access;
- accessory assumptions if relevant;
- natural damage modes it supports, such as swing, thrust, crush, missile, magic, or spirit;
- synergy with the formula-balance model.

This is especially important because the formula rebalance makes weapon and armor families more
distinct.

## JP And Prerequisites

JP costs are in scope.

JP should preserve the pleasure of investment and planning, but reduce excessive grind.

The target is:

- enough grind that build planning matters;
- not so much grind that experimentation feels punished;
- basic identity skills should be reachable;
- strong global build pieces can be expensive;
- late-game reactions/supports/movements can require serious investment.

Prerequisites can be changed when it makes sense. They are not required to change, but GPT and
Claude are free to adjust them if the redesigned ecosystem needs it.

Prerequisites should reflect:

- thematic progression;
- job complexity;
- timing of major build pieces;
- prevention of early universal combos;
- access to important playstyles at a satisfying point in the game.

## Problematic Skills

Famous vanilla skills are not protected just because they are famous.

Any skill can be rewritten, moved, limited, or replaced if it:

- removes meaningful decision-making;
- creates practical immunity;
- becomes mandatory;
- ignores map structure;
- invalidates equipment;
- breaks action economy;
- scales too hard with Brave, Faith, or support stacking;
- makes other builds feel pointless.

However, the fantasy should be preserved where possible.

Examples:

- Shirahadori can remain a master defensive concept, but should not create practical physical
  immunity.
- Teleport can remain special movement, but should not erase map traversal.
- Two Swords can remain iconic, but should not define all physical optimization.
- Auto-Potion can remain a Chemist-flavored defensive reaction, but should not be the default best
  answer to survival.

## Orator Direction

Orator will be kept.

Public player opinion suggests Orator is polarizing rather than worthless:

- it has unique value through Brave/Faith manipulation, recruitment, monster interaction, poaching,
  and guns;
- many players view it as niche, tedious, or weak in direct combat;
- its vanilla role often feels more like long-term account maintenance than active battlefield
  contribution.

Design direction:

- preserve Orator's identity as a social/verbal manipulator;
- make it more useful during actual combat;
- keep Brave/Faith/status/recruitment themes where useful;
- consider command, intimidation, surrender, morale, disruption, and battlefield speech tactics;
- avoid making it only a grind tool.

Research references:

- Reddit Orator discussion: https://www.reddit.com/r/finalfantasytactics/comments/yc8ekp/lets_discuss_chapter_15_orator/
- Reddit recent Orator impression: https://www.reddit.com/r/finalfantasytactics/comments/1q9ziqt/the_orator/
- GameFAQs IVC Orator discussion: https://gamefaqs.gamespot.com/boards/538582-final-fantasy-tactics-the-ivalice-chronicles/81056740
- Hack the Minotaur Orator guide: https://hacktheminotaur.com/final-fantasy-tactics-the-ivalice-chronicles/best-orator-build-guide/
- Final Fantasy Wiki Orator summary: https://finalfantasy.fandom.com/wiki/Orator_%28Tactics%29
- PC Gamer job overview: https://www.pcgamer.com/games/strategy/final-fantasy-tactics-best-jobs/

## Bard And Dancer Direction

Bard and Dancer remain separate jobs.

High-level direction:

- Bard: performance-based support, buffs, sustain, rhythm, morale, or timing identity.
- Dancer: performance-based pressure, debuffs, disruption, distraction, or attrition identity.

Their active abilities should differ because their job identities differ.

Their reaction, support, and movement abilities must be identical because Bard and Dancer are the
only remaining gender-restricted jobs.

The vanilla issue where a performer hides in a corner and stops participating should be reviewed.
The final design should give them more interesting decisions if possible.

## Design Order

The job rebalance should proceed in this order:

1. Define global job-balance principles.
2. Map all in-scope jobs and their intended roles.
3. Design basic and intermediate jobs first.
4. Tune JP, equipment compatibility, growth profiles, multipliers, and prerequisites around those
   roles.
5. Design advanced jobs after the base ecosystem is understood.
6. Design Necromancer, Vanguard, and Ramza's final chapter identity late in the process.
7. Simulate representative parties and build combinations.
8. Iterate with Claude review before promoting decisions from proposal to accepted.

## Required Collaboration Standard

GPT is responsible for writing the documents.

Claude is the reviewer and must approve design decisions before they are treated as accepted.

For detailed job formulas, skills, multipliers, JP costs, and progression changes, GPT and Claude
must run simulations or structured scenario checks where possible. The goal is not spreadsheet
perfection, but the design must survive representative early, mid, late, and stress-case tests.

## Open Decisions For GPT And Claude

The following are intentionally not decided by the user at this stage:

- exact job-by-job skill lists;
- exact formulas for action abilities;
- exact reaction/support/movement redesigns;
- exact job multipliers;
- exact JP costs;
- exact prerequisite changes;
- exact Necromancer mechanics;
- exact Vanguard identity;
- exact Ramza chapter-by-chapter skill list;
- exact attribute targets for each job.

GPT and Claude have autonomy to decide these details later, as long as the decisions support this
high-level direction and are documented.
