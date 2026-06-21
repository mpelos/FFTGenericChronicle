# Deferred Campaign Economy And Permanent State Policy

Status: Deferred policy track
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`

## Purpose

This document records the campaign-economy and permanent-state surface that appeared during
Thief/Orator design.

It is not a combat validation gate. It is not a dual-simulation model. It does not belong in
`docs/job-balance/07-validation-infrastructure-roadmap.md`, whose tracks are combat-resolution
models requiring independent GPT/Claude calculation paths and zero row mismatches.

## Deferred Scope

This policy track covers effects whose primary value is outside the current battle or persists beyond
one battle:

- keeping stolen equipment or loot;
- gil, EXP, JP, treasure, poach, or economy rewards;
- permanent recruitment or roster expansion;
- permanent Brave/Faith changes;
- permanent campaign progression shortcuts or grind reducers.

Concrete examples:

- Thief `Steal Gil`, `Steal EXP`, permanent equipment theft, `Poach`, `Treasure Hunter`, and
  `Sticky Fingers` if it grants permanent loot;
- Orator recruitment, `Defraud`, `Tame`, `Beast Tongue`, and permanent Brave/Faith speech;
- future campaign-economy hooks from Bard, Dancer, Necromancer, Special Knight, or Ramza.

## Current Phase Policy

The current job-balance phase may continue with battle-scoped designs without waiting for this
policy track.

Safe defaults:

- equipment steal is battle-scoped suppression during combat;
- recruitment is battle-scoped control during combat;
- Brave/Faith manipulation is battle-scoped during combat;
- gil/EXP/JP/loot skills are not counted as combat power;
- monster-dependent effects stay deferred because monsters are out of current scope.

If a later pass wants permanent value, it must reopen this policy track explicitly.

## Acceptance Shape

This track should be accepted by policy review, not by dual numeric simulation.

It should answer:

- whether permanent rewards create mandatory grind chores;
- whether permanent Brave/Faith changes harm build planning;
- whether recruitment breaks roster progression or encounter design;
- whether loot/economy rewards distort normal campaign pacing;
- whether campaign value should be separated from combat benchmark builds.

## Current Deferred Questions

- Should stolen equipment ever become permanent inventory?
- Should recruitment stay mostly flavor/campaign, or be a major progression tool?
- Should Brave/Faith changes ever persist beyond battle?
- Should Poach, Tame, Beast Tongue, and monster-facing effects wait until monster scope opens?
- Should Treasure Hunter and similar economy tools be combat-neutral campaign conveniences?
