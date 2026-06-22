# Vanguard And Ramza Concrete Provisional V0

Status: Accepted as W3 Vanguard/Ramza concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/29-special-knight-v1-proposal.md`
- `docs/job-balance/49-vanguard-rename-decision-v0.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-vanguard-ramza-concrete-v0.json`

## Purpose

This is the sixth W3 physical/foundation concrete action-value producer and closes the generic
physical roster plus Ramza chapter values for this pass.

It covers:

- `Vanguard`, the accepted replacement identity for the old Mime slot;
- Ramza's chapter-progressing protagonist job.

This document sets provisional action values and dominance boundaries. It does not finalize
reaction/support/movement values, prerequisite trees, final multipliers, exact equipment records,
AI challenge behavior, cover implementation, final Ramza chapter equipment, or W4/W5 incidence
verdicts.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Ramza Squire records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- Squire/Fundaments records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `Aegis` and unique/boss defense palette records in
  `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `brave_up`, `stat_up`, `timing`, `healing`, `damage`, `magical`, `unique`, `ally_buff`,
  `defense`, `status_clear`, `physical`, and `random` tags in
  `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Haste, Regen, Protect, Shell, Brave/Chicken, Faith, Charging, Defending, Reflect, Shell, and
  KO/Undead vocabulary in `docs/reference/fft-vanilla-status-effect-map.md`.

The design preserves the FFT read: Vanguard is a late vanguard/protection job, not a Mime; Ramza
evolves by chapter and becomes a broad knight/mage protagonist without erasing specialists.

## Shared Formula Contracts

Physical rows use `work/sim-inputs-v0.2.1.json` with:

```text
effwp_rounding = none
```

Vanguard uses a provisional late roster row, because the sim bundle does not yet contain the Mime
replacement as a real job entry:

```text
armor_class = plate
late stats = HP 516, PA 12, MA 6, Speed 7
native concrete weapon families = sword, spear, axe, fists
native knight_sword = no
support-gated knight_sword = deferred Equip Knight Swords only
```

Ramza uses provisional chapter rows:

| Chapter row | Band | HP | PA | MA | Speed | Read |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Chapter 1 | 0/A | 150 | 4 | 4 | 6 | Squire-floor parity, not better starter damage. |
| Chapter 2 | B/C | 280 | 8 | 7 | 7 | Flexible physical support, not first-specialist dominance. |
| Chapter 3 | C/D | 308 | 9 | 8 | 7 | Hybrid bridge; still below specialist jobs in protected lanes. |
| Chapter 4 | E | 473 | 12 | 12 | 8 | Final broad knight/mage hybrid. |

Ramza does not receive native staff/rod/pole MA-crush weapon access in this pass. His magical
scaling appears through chapter actions, not through an unbounded staff-melee anti-plate route.

## Vanguard Values

Vanguard should be valuable because formation, protection, and setup matter. It must not become
Knight with more weapons and better numbers.

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Breach` | axe/fists/shield-crush x0.75 plus exposure mark | 0 | 0 | 220 | E | T6/T6xT7/F5 | Committed guard-pressure art; no sword/spear modal rider. |
| `Intercede` | protected ally incoming direct hit x0.75; Vanguard takes 25% chip | 0 | 0 | 260 | E | T6xPS/T8/T5 | Nearby ally only; single direct hit; no global cover. |
| `Aegis Stance` | self/adjacent allies incoming hit x0.85; Vanguard outgoing x0.75 | 0 | 0 | 320 | E | T6xPS/T5/T2.1 | Lasts until Vanguard next turn; no stacking with Protect/Shell/Kiyomori-like mitigation. |
| `Sunder Guard` | current weapon x0.45 plus guard/exposure mark | 0 | 0 | 380 | E | T4/T6/T6xT7 | Setup tool; does not destroy gear and is weaker than Knight's deeper Rend windows. |
| `Commanding Challenge` | soft challenge +25; ignored target output x0.85 | 0 | 0 | 420 | E | T8/T5 | Local mark only; no hard boss lock or forced AI script. |
| `Decisive Strike` | current weapon x1.20 if setup-marked, otherwise x0.75 | 0 | 0 | 600 | E | F5/T4/T6 | Setup finisher; no instant KO, no Holy Sword clone, no free range. |

`Breach` exposure mark:

```text
response_delta_by_armor = { plate: +0.06, mail: +0.05, leather: +0.03, cloth: +0.00 }
response_cap = 1.15
duration = next one direct physical hit or target's next turn
```

`Sunder Guard` uses the same exposure mark and also reduces shield/weapon guard layers to x0.60
for the next direct attack or target's next turn. It does not affect class/accessory evasion and
does not delete equipment.

Vanguard output-pressure and mitigation do not stack multiplicatively with equivalent channels.
If `Aegis Stance`, `Intercede`, Protect/Shell-like mitigation, or Kiyomori-like mitigation apply to
the same incoming hit, use the strongest single applicable mitigation for that channel.

### Vanguard Damage Rows

Vanguard late baseline:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| sword | 124 | 144 | 182 | 192 |
| spear | 125 | 198 | 173 | 181 |
| axe expected | 149 | 123 | 130 | 130 |
| fists | 110 | 93 | 97 | 97 |

Action rows:

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| `Breach` axe x0.75 | 112 | 92 | 97 | 97 |
| `Breach` fists x0.75 | 82 | 70 | 73 | 73 |
| `Sunder Guard` sword x0.45 | 56 | 64 | 82 | 86 |
| `Sunder Guard` axe x0.45 | 67 | 55 | 58 | 58 |
| `Decisive Strike` sword x1.20, setup | 149 | 172 | 218 | 230 |
| `Decisive Strike` spear x1.20, setup | 150 | 237 | 208 | 218 |
| `Decisive Strike` axe x1.20, setup | 179 | 148 | 156 | 156 |
| `Decisive Strike` sword x0.75, no setup | 93 | 108 | 136 | 144 |

Read:

- Vanguard has normal weapon breadth, but its actions are not raw upgrades over attacking;
- `Breach` and `Sunder Guard` are setup and protection pressure, not Knight Rend replacements;
- `Decisive Strike` can be high only after a setup mark and still uses normal armor response;
- spear access must be watched against Dragoon, but Vanguard has no Jump/reach mastery.

### Vanguard Mitigation Rows

Incoming direct hit 120:

| State | Ally final damage | Vanguard chip | Read |
| --- | ---: | ---: | --- |
| no protection | 120 | 0 | baseline |
| `Intercede` | 90 | 30 | protection with real frontline cost |
| `Aegis Stance` | 102 | 0 | weaker broad local mitigation |
| both eligible | 90 | 30 | strongest single mitigation channel, not multiplied |

Exposure rows, base hit 100:

| Armor | Type | Base response | After mark | Projected damage |
| --- | --- | ---: | ---: | ---: |
| plate | swing | 0.65 | 0.71 | 71 |
| plate | crush | 1.15 | 1.15 | 115 |
| mail | missile | 1.10 | 1.15 | 115 |
| leather | thrust | 0.95 | 0.98 | 98 |
| cloth | swing | 1.00 | 1.00 | 100 |

This is deliberately below Knight `Rend Armor`'s longer and stronger exposure window.

## Ramza Chapter Values

Ramza should feel useful in every chapter because he is always present. He should not be the best
Squire, best Knight, best caster, best controller, or best late vanguard in the same row.

Chapter skills are story-unlocked at `JP 0` in this pass. This guarantees chapter evolution without
turning Ramza into another hidden grind path.

| Chapter | Skill | Value | MP | CT | JP | Gate binding | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 1 | Squire fundamentals | inherit doc 52 Squire values | 0 | 0 | inherited | P0/F5 | Same early damage floor as Squire, not better. |
| 1 | `Tailwind` | self CT +8 | 0 | 0 | 0 | T5/T10 | Self only; once per round; no Speed stat, no ally Rally replacement. |
| 2 | `Steel` | Brave +6, cap 80 | 0 | 0 | 0 | F5/T2.1 | Self or one ally; battle-scoped; weaker than Orator `Praise`. |
| 2 | `Chant` | ally heals 35 HP; Ramza loses 15 HP | 0 | 0 | 0 | T3/T3xT5 | No revive, no status clear, no Faith scaling. |
| 3 | `Spellblade` | hybrid sword x0.85 | 0 | 0 | 0 | F4/F5 | Single target; uses floor((PA+MA)/2) sword pressure, swing response. |
| 3 | `Ward` | next incoming magic hit x0.85 | 0 | 0 | 0 | F4/T6xPS | Self or one ally; one hit; no Shell replacement. |
| 4 | `Shout` | self CT +12 and Brave +6, cap 80 | 0 | 0 | 0 | T5/F5/T10 | Self only; once per round; no PA/MA stat stack. |
| 4 | `Arc Blade` | hybrid sword x1.00 | 0 | 0 | 0 | F4/F5 | Single target hybrid strike; no line/AoE Holy Sword clone. |
| 4 | `Ultima` | K22 MA/Faith magic, small area | 40 | 4 | 0 | F4/T5/T11/T9 | Below dedicated Black Mage high spell; Shell/Faith/Reflect policy applies. |

`Steel` and `Shout` do not create permanent Brave. They use the same battle-scoped doctrine as
Orator morale tools.

`Ultima` is intentionally useful but not the best burst-caster row. At 70/70 Faith, Chapter 4
Ramza's `Ultima` row is 158 before Shell/element/zodiac, while Black Mage late K26 is 234 under the
same Faith floor.

### Ramza Damage And Dominance Rows

Weapon and hybrid rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Chapter 1 Ramza sword | 20 | 24 | 30 | 32 |
| Squire early sword reference | 20 | 24 | 30 | 32 |
| Chapter 2 Ramza sword | 62 | 72 | 91 | 96 |
| Chapter 3 Ramza sword, mid | 70 | 81 | 102 | 108 |
| Chapter 3 `Spellblade` x0.85 | 53 | 61 | 77 | 81 |
| Chapter 4 Ramza sword | 124 | 144 | 182 | 192 |
| Chapter 4 `Arc Blade` x1.00 | 124 | 144 | 182 | 192 |

Magic rows at Faith 70/70:

| Row | Damage |
| --- | ---: |
| Chapter 4 Ramza `Ultima` K22, late | 158 |
| Chapter 4 Ramza `Ultima` K22, stress | 171 |
| Black Mage K26, late reference | 234 |
| Black Mage K26, stress reference | 280 |

Per-band read:

| Band | Ramza check | This pass read |
| --- | --- | --- |
| 0/A | Squire/Chemist | Ramza matches Squire sword floor and has self CT, but does not item-heal like Chemist or Rally allies like Squire. |
| B | Knight/Archer/White/Black | Ramza gains Brave/heal utility, but no Rend, bow identity, item breadth, White revive/protection, or Black burst. |
| C | Time/Mystic/Geomancer/Dragoon/Orator | Ramza bridges with Spellblade/Ward but lacks Time control, Mystic status, terrain, Jump, speech, and gun identity. |
| D | Samurai/Ninja/Summoner/performers | Ramza is broad but does not gain Iaido, two-hit burst, summon area, or global performance. |
| E | Vanguard/Necromancer/older specialists | Final Ramza is top-tier broad, but `Ultima` is below Black burst and Arc Blade is single-target sword/hybrid pressure, not Vanguard protection. |

## RSM Boundary Values

Deferred, but bound by this pass:

| Piece | Boundary |
| --- | --- |
| Vanguard `Intervention` | Local ally-protection reaction only; no global cover, no extra-attack loop unless T10 approves. |
| Vanguard `Last Stand` | Critical survival fantasy; no practical immortality. |
| Vanguard `Equip Knight Swords` | E support, optional and cuttable; cannot revive sword dominance. |
| Vanguard `Vanguard Training` | Vanguard-action specialist support only; no broad physical damage support. |
| Vanguard `Armor Discipline` | Plate/shield specialist support; no mitigation stack immunity. |
| Vanguard `Vanguard March` | Formation movement, not broad late mobility. |
| Ramza R/S/M | Deferred by chapter; any support/movement must preserve the per-band specialist checks above. |

## Review Questions For Claude

1. Is Vanguard's action package distinct enough from Knight's Rend suite, given both share plate and
   sword/fists rows?
2. Are `Intercede` and `Aegis Stance` strong enough to make Vanguard exciting without creating a
   mitigation stack problem?
3. Is `Decisive Strike x1.20` acceptable because it is setup-gated, local, and not a ranged Holy
   Sword clone?
4. Does Ramza's chapter table preserve the per-band specialist-protection rule?
5. Is `Ultima` K22/MP40/CT4 strong enough for final Ramza while staying below Black Mage burst?
