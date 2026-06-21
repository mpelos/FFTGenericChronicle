# Job Validation Infrastructure Roadmap

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/04-foundation-physical-jobs-proposal.md`
- `docs/job-balance/05-squire-chemist-v1-proposal.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

The first concrete job slices surfaced a validation gap.

The design direction for Squire, Chemist, Knight, and Archer is accepted for provisional design, but
many concrete skill values cannot honestly be validated by the current damage-only harness. If the
project keeps producing concrete job kits without extending the validation model, the docs will
accumulate required checks that are named but not runnable.

This document records the validation tracks needed before provisional skill designs can become
accepted implementation data.

## Non-Goals

This document does not implement any harness.

It does not decide final numbers for skills, JP, CT, hit rates, healing, damage, equipment, growth,
or prerequisites.

It does not block role-map or identity-level documents that avoid concrete formula-affecting data.

## Current State

Accepted/provisional job documents now refer to checks in these categories:

- formula v0.2 damage-family checks;
- mandatory reaction/support/move incidence;
- equipment-unlock incidence;
- secondary skillset incidence;
- immunity and attrition checks;
- dynamic armor response;
- enemy-offense disruption;
- accuracy, evasion, line-of-fire, and height;
- CT, delayed actions, overwatch, and interrupts;
- AI targeting or challenge behavior;
- mitigation stacking across armor response, Protect/Shell, element, zodiac, and clamps;
- resource and MP economy beyond per-action recovery plumbing;
- spell routing/Reflect composition;
- action grants and turn-economy recursion.

Only the damage-family side is currently represented by the existing formula harness. Several other
categories are design-required but not yet executable.

## Validation Tracks

### T1 - Real Weapon Baseline

Goal: replace provisional weapon assumptions with the real extracted weapon data.

Unblocks:

- formula-balance v1;
- final job damage bands;
- concrete weapon-family and equipment-access acceptance.

Notes:

- this is still the strongest source-of-truth gap for final numeric work;
- this track is scheduled and awaiting Marcelo's Windows machine dump through
  `tools/dump_weapons.py` into `work/baseline_weapons.csv`;
- it does not block designing or building T2, T3, or T4 against the current fallback WP data;
- when the real weapon baseline lands, affected outputs must be revalidated through Gate F5 before
  final acceptance.

### T2 - Build-Incidence Matrix

Goal: track how often each secondary, reaction, support, movement, and equipment-unlock appears in
accepted strong builds.

Unblocks:

- `M-RSM-COUNT-LATE`;
- `M-RSM-COUNT-STRESS`;
- `M-SECONDARY-COUNT`;
- `M-EQUIP-UNLOCK`;
- mandatory-piece warnings and failures.

Notes:

- this can start as a structured spreadsheet or JSON benchmark set before becoming a full simulator;
- it must include every primary role, armor class, physical damage mode, magic, Brave, Faith, and
  Speed-sensitive build required by `01-cross-job-build-principles.md`;
- it must honor the anti-bias requirements in `01-cross-job-build-principles.md`: do not
  over-represent dominant builds, under-represent weak roles, or curate the benchmark set to make a
  favored secondary, reaction, support, movement, or equipment unlock pass.

### T3 - Healing, Sustain, And Attrition Model

Goal: model multi-turn survival with healing, revive, MP recovery plumbing, automatic reactions,
inventory cost, and action economy.

Unblocks:

- Squire `First Aid`;
- Chemist `Potion` line, `Field Salve`, `Phoenix Down`, `Auto-Potion`, `Item Lore`;
- Monk sustain and revive concepts;
- White Mage and support-caster comparison;
- `I-ATTRITION`.

Notes:

- this should be prioritized before final concrete values for any repeatable heal or auto-heal;
- it should compare item reliability against Faith/MA/MP/CT-based healing rather than treating all
  recovery as equivalent;
- it does not fully validate MP economy. Skills that change long-run spell availability, MP
  efficiency, or MP recovery loops require T9.

### T4 - Accuracy, Evasion, Line-Of-Fire, And Height Model

Goal: model hit chance, evasion sources, shields, facing, projectile lanes, line-of-fire, and height
enough to validate ranged and defensive skills.

Unblocks:

- Archer `Concentration`, `Arrow Guard`, `High-Ground Shot`, and part of `Aimed Shot`;
- Chemist `Smoke Bomb`;
- Knight `Shield Break` and `Parry`;
- broad immunity checks involving evasion and shield stacks.

Notes:

- formula v0.2 includes only coarse damage targets, not a full hit/evasion model;
- this track should prevent broad accuracy supports from becoming mandatory by accident.

### T5 - CT, Delay, Overwatch, And Interrupt Model

Goal: model delayed actions, action timing, target movement, overwatch, interrupts, and short-lived
tempo reactions.

Unblocks:

- Archer `Aimed Shot`, `Covering Shot`, `Pinning Shot`, and `Speed Save`;
- Squire `Rally` if it changes CT or initiative;
- any later Time Mage or movement-tempo design.

Notes:

- this can be scenario-based rather than a full battle simulator at first;
- delayed attacks must be evaluated for player predictability, not only average damage.
- T5.0 does not model action grants such as Quick. Effects that grant an immediate action or change
  post-action recursion require T10.

### T6 - Dynamic Armor Response And Guard-Break Model

Goal: model temporary changes to armor response, guard state, defense-down, penetration, or damage
type vulnerability.

Unblocks:

- Knight `Rend Armor`, `Shield Break`, and `Crushing Blow`;
- Archer `Piercing Shot`;
- Monk anti-plate or guard-breaking techniques;
- any later Geomancer or Mystic defense-response effects.

Notes:

- this extends the static armor-response table from formula v0.2;
- effects must be scoped so setup skills do not become mandatory for all physical parties.

### T7 - Enemy-Offense And Disarm Model

Goal: model what happens when an enemy's weapon output, equipped family, or attack routine is
reduced, jammed, disabled, or replaced.

Unblocks:

- Knight `Rend Weapon`;
- Safeguard/counterplay against equipment disruption;
- future Thief or Orator disruption effects.

Notes:

- this can be a focused encounter/offense model rather than full AI simulation at first;
- permanent equipment deletion should be evaluated separately from temporary offense pressure.

### T8 - AI Targeting And Challenge Feasibility

Goal: decide whether challenge, taunt, mark, or forced-target behavior is data-moddable and healthy.

Unblocks:

- Knight `Challenge`;
- future controller or defender identities that depend on enemy targeting.

Notes:

- this may be an implementation feasibility track, not only a math track;
- if robust targeting changes are not data-moddable, the design should use zone, CT, movement, or
  counter-pressure alternatives.

### T6xPS - Protect/Shell Mitigation Stacking Composition

Goal: model how Protect, Shell, Wall, element, zodiac, armor response, and the shared clamp compose
in the full formula operation order.

Unblocks:

- White Mage `Protect`, `Shell`, and `Wall`;
- any later defensive status, mitigation package, or magic-defense support that stacks with armor
  response;
- F4 Shell-on magic/physical coexistence rows.

Notes:

- the first composition must exercise the full order:
  `armor type_response * protect_shell * element * zodiac`, clamped to `[0.25, 2.50]`;
- rows must include realistic Protect plus plate, Shell plus faith-floor magic, Wall as combined
  Protect/Shell, and at least one high-stack row that approaches the low clamp;
- the gate should prove that defensive upkeep does not manufacture de-facto immunity or make
  armor/Faith counters invisible to the player.

### T9 - Resource And MP Economy Model

Goal: model MP recovery, MP efficiency, spell availability, finite resources, and long-run resource
loops.

Unblocks:

- Monk `Chakra` if MP restore remains in scope;
- White Mage and Black Mage spell MP values;
- caster supports that change MP cost, MP recovery, or spell availability;
- Summoner, Time Mage, Mystic, Necromancer, Bard, Dancer, and Ramza resource designs that depend on
  MP or similar finite resources.

Notes:

- T3 can record per-action resource consumption and simple finite-resource caps, but T9 is the
  track that decides whether a resource loop changes encounter pacing or spell access;
- caster values that alter MP economy should not reach accepted implementation data until this
  track exists and passes the dual-independent gate;
- T9 should distinguish tactical MP pressure from campaign economy so resource scarcity does not
  become hidden difficulty inflation.

### T8xSR - Spell Routing And Reflect Composition

Goal: model how reflected or rerouted spells choose targets after the original spell target is
changed.

Unblocks:

- Time Mage `Reflect`;
- future spell-routing, redirect, decoy, or target-redirection effects;
- any controller design that relies on a spell bouncing to a different legal target.

Notes:

- this composes with T8 because reflected single-target magic changes targeting, not only damage;
- rows should include beneficial spell backfire, hostile spell bounce, no-legal-target cases, and
  player-legible routing risk;
- Reflect should stay a tactical routing decision, not a pure anti-magic immunity layer.

### T10 - Turn-Grant And Action-Economy Model

Goal: model effects that grant immediate actions, alter post-action CT recursion, or create
additional turns outside normal speed/CT progression.

Unblocks:

- Time Mage `Quick`;
- `Critical: Quick`;
- any later reaction, support, or job action that grants an action now, refunds a turn, repeats an
  action, or bypasses normal post-turn CT costs.

Notes:

- Haste and Slow are speed-state effects and can use T5;
- Swiftspell and similar cast-time reductions can use T5;
- Quick-class action grants are not covered by T5 and require T10 before numeric acceptance;
- the first T10 bundle must include anti-recursion rows, including Quick on a unit that can also
  Quick, reaction-triggered Quick chains, and party turn-economy caps.

## Recommended Sequence

Recommended order before more concrete skill-number work:

1. Schedule `T1 - Real Weapon Baseline`; it is externally gated on Marcelo's machine and does not
   block the next validation tracks.
2. Define `T2 - Build-Incidence Matrix`, because it validates mandatory-piece risk across every job
   slice and does not require full battle simulation.
3. Build `T3 - Healing/Sustain/Attrition` and `T4 - Accuracy/Evasion/Positioning` as co-equal top
   priorities after T2. They unblock different foundation jobs: Squire/Chemist sustain and Archer
   ranged reliability.
4. Build `T5 - CT/Delay/Overwatch` to round out Archer tempo skills and future tempo-control jobs.
5. Build `T6 - Dynamic Armor Response` and `T7 - Enemy-Offense/Disarm` before concrete Knight,
   Monk anti-plate, Archer piercing, Thief disruption, or similar values.
6. Evaluate `T8 - AI Targeting/Challenge` separately; if not feasible, redesign those skills early.
7. Build `T6xPS - Protect/Shell Mitigation Stacking` before concrete Protect, Shell, Wall, or
   broad defensive status values.
8. Build `T9 - Resource/MP Economy` before concrete MP restore, MP discount, or spell-availability
   loops.
9. Build `T8xSR - Spell Routing/Reflect Composition` before concrete Reflect routing values.
10. Build `T10 - Turn-Grant/Action-Economy` before Quick-class or action-refund values.

## Acceptance Gate Per Track

Each new validation model from T3 through T10, including composition tracks such as T3xT5, T6xT7,
T6xPS, and T8xSR, must inherit the same dual-independent discipline that made formula-balance v0.2
trustworthy.

Before a track output can be used to accept concrete skill data:

- GPT must produce one implementation or calculation path;
- Claude must produce an independent reviewer implementation or calculation path;
- both must consume the same pinned input bundle, either by extending `work/sim-inputs-v0.2*.json`
  or by creating a sibling pinned bundle for the track;
- the two outputs must reconcile to `0` row mismatches on the agreed scenario rows;
- any mismatch must be documented and resolved before the track's output is used for acceptance.

When a track touches damage, durability, hit chance, action economy, or another combat result, its
outputs feed Gate F5 or its accepted successor. The track does not replace the existing formula
scorecard; it extends the evidence required for the affected mechanic.

## Work That Can Continue Before These Tracks

The following can continue without waiting for the new harnesses:

- role-map or identity-level documents;
- non-numeric job fantasy and niche documents;
- dependency/prerequisite philosophy documents;
- growth-profile philosophy documents;
- JP economy philosophy documents;
- Bard/Dancer parity planning;
- Necromancer and Special Knight high-level constraints, as long as they avoid final concrete values.

The following should pause after document 06 unless the user explicitly accepts accumulating more
provisional concrete docs:

- concrete Monk skill values;
- concrete caster spell values;
- concrete reaction/support/movement values;
- any skill list that depends heavily on healing, evasion, CT, dynamic armor response, or enemy
  offense disruption.

## Decision (accepted by Marcelo, 2026-06-20)

Marcelo accepted the infrastructure-first sequence:

```text
Pause new concrete skill-slice documents after Knight/Archer.
Continue non-numeric identity docs in parallel.
Start a validation-infrastructure track so concrete skill values can be tested before final
acceptance.
Prioritize accuracy/evasion and healing/attrition models together after the build-incidence matrix.
```

This keeps momentum without pretending the current damage harness can answer questions it does not
model.

## Claude Review Request

Claude should review whether this roadmap:

- accurately captures the validation gaps surfaced by documents 05 and 06;
- orders the validation tracks correctly;
- preserves the user's desire to focus on combat design rather than technical feasibility;
- gives a clear path for what can continue now versus what should pause;
- should be accepted, revised, or blocked.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-20).
