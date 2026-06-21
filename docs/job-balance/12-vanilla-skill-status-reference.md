# Vanilla Skill And Status Reference

Status: Reference audit ready for Claude review
Date: 2026-06-21
Depends on:
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/baseline_abilities.csv`
- `work/baseline_jobs.csv`

## Purpose

Before Generic Chronicle continues concrete job redesign, designers need a common lookup surface for
the abilities, status effects, and effect families that already exist in Final Fantasy Tactics.

This document does not approve any new job design and does not change any prior proposal. It points
future job-balance work to the reference docs that should be checked before inventing, removing, or
rewriting skills.

## Reference Docs

- `docs/reference/fft-vanilla-ability-effect-index.md`
  - complete local index of the 491 non-empty ability records in `work/baseline_abilities.csv`;
  - includes ability ID, name, local JP, local random flags, CT/MP overrides, command bucket,
    vanilla owner, effect summary, and effect tags;
  - includes command-bucket coverage and effect-family counts so designers can search by behavior
    instead of only by job name;
  - separates local extracted facts from researched effect tags.
- `docs/reference/fft-vanilla-status-effect-map.md`
  - map of locally observed and externally documented vanilla status effects;
  - includes category, core mechanical effect, design hooks, likely validation track, and whether
    the status appears in local JobData status vocabulary.
  - includes a design-concern map for control denial, timing, targeting, attrition, movement,
    defeat-state, Faith, Brave, and formula-multiplier interactions.
  - includes two external-only dummied statuses, `Dark/Evil Looking` and `Wall`, as proof-first
    vocabulary because they are documented externally but do not appear in local JobData.

## How To Use This During Job Redesign

Each new job proposal should check these references before finalizing a skill list.

Use the ability index to answer:

- whether an existing ability slot already expresses the desired fantasy;
- whether an effect is an action, reaction, support, movement, monster, or unique-job pattern;
- whether the skill touches `damage`, `healing`, `status_add`, `status_clear`, `equipment_break`,
  `accuracy`, `defense`, `ct_action`, `timing`, `movement`, `throw`, `steal`, `recruit`, or
  `jp_exp` effect families;
- whether a proposed replacement is reusing a local placeholder, duplicate display name, or
  formula-sensitive record.

Use the status map to answer:

- whether a status affects action denial, evasion, CT timing, healing/revive, AI targeting,
  mitigation, terrain, undead behavior, or campaign-only defeat states;
- which validation track is probably required before concrete values can be accepted;
- whether a status is a good fit for a job identity or too broad for a casual rider effect.

## Required Proposal Checklist

Every new or revised job proposal should now include a short reference pass before Claude approval:

- name the vanilla skills, status effects, or effect tags that most closely overlap the proposed
  job identity;
- call out when the proposal intentionally replaces, narrows, combines, or rejects a vanilla
  behavior;
- cite the validation tracks needed when the job touches damage, healing, status, CT/timing,
  global effects, equipment pressure, movement, AI targeting, undead, or corpse/KO behavior;
- flag any formula-sensitive assumption whose base range, area, formula, X/Y value, inflicted
  status, or implementation hook is not present in the local extracted CSV;
- avoid treating monster, boss, or unique rows as mandatory player-skill patterns. They are a
  vocabulary bank unless a proposal explicitly chooses to reuse the behavior.

## Reference Boundaries

The ability index is not byte-accurate formula documentation.

`tools/dump_baseline.py` states that base ability Formula/X/Y data is not present in the extracted
local ability table and is hardcoded in `FFT_enhanced.exe`. Therefore:

- local ID/name/JP/random/CT/MP data is authoritative for the current extracted baseline;
- effect tags are a researched design index;
- concrete formula-affecting changes still require the relevant validation model, proof patch, or
  direct extracted formula data before final acceptance.
- command bucket, vanilla owner, effect summary, status category, design hooks, and validation
  tracks are research/design classifications. They are useful for planning but should not be cited
  as extracted implementation facts.

## Immediate Impact On Current Job-Balance Work

Before continuing any job proposal after this point:

- T6 dynamic armor response should consult all `equipment_break`, `defense`, `damage_boost`, and
  mitigation/status entries.
- T7 enemy-offense/disarm should consult all `equipment_break`, `steal`, `defense`, `reaction`, and
  weapon-output-adjacent entries.
- Necromancer should consult all `undead`, `instant_ko`, `drain`, `healing`, `revive`,
  `status_add`, `status_clear`, `timing`, and KO/defeat-state entries before its V1 proposal is
  accepted.
- Special Knight should consult unique sword skills, `equipment_break`, `damage`, `status_add`,
  `defense`, and `support` identity before its V1 proposal is drafted.
- Ramza should consult Squire/Ramza Squire, Knight, White Mage, Black Mage, Mystic, Time Mage, and
  hybrid damage/support rows before its chapter-scaling identity is drafted.
- All remaining proposals should check the atlas before proposing new active/reaction/support/
  movement identities.

## External References Consulted

- GameFAQs Jobs/Abilities Chart:
  `https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3859`
- AeroStar Battle Mechanics Guide:
  `https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876`
- GameFAQs War of the Lions status guide:
  `https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/status-effects`
- Final Fantasy Wiki ability/status category pages:
  `https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_abilities`
  `https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_statuses`
