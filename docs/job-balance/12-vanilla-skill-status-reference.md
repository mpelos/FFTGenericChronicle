# Vanilla Skill And Status Reference

Status: Reference draft
Date: 2026-06-20
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
  - separates local extracted facts from researched effect tags.
- `docs/reference/fft-vanilla-status-effect-map.md`
  - map of locally observed and externally documented vanilla status effects;
  - includes category, core mechanical effect, design hooks, likely validation track, and whether
    the status appears in local JobData status vocabulary.

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

## Reference Boundaries

The ability index is not byte-accurate formula documentation.

`tools/dump_baseline.py` states that base ability Formula/X/Y data is not present in the extracted
local ability table and is hardcoded in `FFT_enhanced.exe`. Therefore:

- local ID/name/JP/random/CT/MP data is authoritative for the current extracted baseline;
- effect tags are a researched design index;
- concrete formula-affecting changes still require the relevant validation model, proof patch, or
  direct extracted formula data before final acceptance.

## Immediate Impact On Current Job-Balance Work

Before continuing from T6 onward:

- T6 dynamic armor response should consult all `equipment_break`, `defense`, `damage_boost`, and
  mitigation/status entries.
- T7 enemy-offense/disarm should consult all `equipment_break`, `steal`, `defense`, `reaction`, and
  weapon-output-adjacent entries.
- Future Monk, Thief, White Mage, Black Mage, Mystic, Time Mage, Geomancer, Dragoon, Orator,
  Summoner, Samurai, Ninja, Bard, Dancer, Necromancer, Special Knight, and Ramza proposals should
  check the atlas before proposing new active/reaction/support/movement identities.

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
