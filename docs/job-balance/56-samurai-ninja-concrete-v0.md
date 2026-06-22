# Samurai And Ninja Concrete Provisional V0

Status: Accepted as W3 Samurai/Ninja concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/job-balance/24-dragoon-samurai-v1-proposal.md`
- `docs/job-balance/25-ninja-v1-proposal.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-samurai-ninja-concrete-v0.json`

## Purpose

This is the fifth W3 physical/foundation concrete action-value producer.

It covers Samurai and Ninja because these are the first advanced physical jobs whose iconic rewards
can become global convergence engines:

- Samurai can compress Brave-scaled katana damage, Iaido area/support, `Shirahadori`, and
  `Doublehand`;
- Ninja can compress high Speed, innate two hits, Throw reach, `Dual Wield`, Vanish, and elite
  movement.

This document sets provisional action and active-job boundary values for validation. It does not
finalize reaction/support/movement values, prerequisite trees, equipment shop timing, permanent
inventory/economy costs, Vanish behavior, Shirahadori block chance, learned `Dual Wield`, learned
`Doublehand`, or full W4/W5 incidence verdicts.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Samurai Iaido records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- Ninja Throw records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `damage`, `magical`, `special`, `physical`, `throw`, `damage_boost`, `reaction`, `defense`,
  `status_add`, `equipment_unlock`, `support`, and `movement` tags in
  `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Haste, Regen, Protect, Shell, Slow, Confuse, Doom, Invisible, Jump, Charging, and Brave/Faith
  vocabulary in `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/reference/README.md` as the navigation layer.

The design preserves the FFT read: Samurai draws katana spirit and rewards disciplined weapon
commitment; Ninja is the fast two-hit skirmisher and thrown-weapon specialist. It does not preserve
vanilla's exact Faith-independent Iaido magic route, katana break economy, full-family Throw
routing, or broad physical-immunity assumptions.

## Shared Formula Contracts

Physical rows use the current v0.2.1 family model from `work/sim-inputs-v0.2.1.json`.

The shared bundle now pins:

```text
effective_wp = family_wp * phase_wp_scalar
effwp_rounding = none
```

Samurai's katana lane:

```text
routine = br_pa_wp
damage_type = swing
penetration = 0.00
```

Ninja's accepted melee lanes in this pass:

```text
ninja_blade: routine = spd_pa_wp, damage_type = swing, penetration = 0.00
knife:       routine = spd_pa_wp, damage_type = thrust, penetration = 0.10
```

Ninja `flail` access is rejected for this W3 concrete pass. The sim shows it makes Ninja a better
anti-plate crush unit than intended, especially with innate two hits. The later equipment pass must
either remove flails from Ninja or make flails ineligible for Ninja dual-wield use. Until then,
Ninja concrete values should be read as `ninja_blade` plus `knife`, not flail.

Throw uses its own capped value table:

```text
throw_pressure = floor((PA + Speed) / 2) * (throw_value * phase_wp_scalar)
damage_type = missile
penetration = 0.20
effwp_rounding = none
```

Throw does not inherit the normal routine, damage type, penetration, Brave scaling, random scaling,
or armor-response profile of the thrown weapon family.

## Samurai Baseline Check

Katana rows use the pinned continuous mid effective WP:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Samurai katana, mid | 61 | 70 | 89 | 94 |
| Samurai katana, late | 105 | 121 | 153 | 162 |
| Samurai katana, stress | 117 | 135 | 171 | 180 |
| `Doublehand` katana x1.80, late | 189 | 218 | 277 | 291 |

Read:

- katana is strong but shaped by swing response, so plate does not disappear;
- `Doublehand` is a real late payoff and must stay a support-slot competition;
- Brave manipulation from Orator now visibly affects Samurai and must remain F5-visible.

## Samurai Values

Iaido should feel like drawing the spirit of the katana. It should not become free Black Magic on a
plate body.

Damage Iaido route:

```text
katana_spirit_pressure = br_pa_wp katana pressure * skill_multiplier
damage_type = swing
penetration = 0.00
Faith = not used
katana break/inventory cost = not used as the primary balancing lever
```

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Ashura` | katana-spirit x0.60 | 0 | 0 | 140 | D | F5/T11 | Early draw; low ceiling because area/position is the value. |
| `Kotetsu` | katana-spirit x0.75 | 0 | 0 | 220 | D | F5/T11 | Reliable ordinary draw; still below direct katana attack. |
| `Bizen Osafune` | katana-spirit x0.90 | 0 | 0 | 320 | D | F5/T11 | Focused pressure; not a universal best button. |
| `Murasame` | 60 HP ally recovery | 0 | 0 | 380 | D | T3/T11 | Small area ally heal; no revive, no status clear, no Faith scaling. |
| `Ame-no-Murakumo` | katana-spirit x0.85 | 0 | 0 | 450 | D | F5/T11 | Area/formation draw; lower than `Bizen` because target count is the value. |
| `Kiyomori` | next incoming hit x0.85 | 0 | 0 | 520 | D | T6xPS/T11 | Small area ally guard draw; one physical or magical hit; non-stacking with Protect/Shell-like mitigation. |
| `Muramasa` | katana-spirit x0.50 plus Slow 30% | 0 | 0 | 600 | D/E | T4/T5/T11 | Curse draw; status is the value, not damage. |
| `Kiku-ichimonji` | katana-spirit x0.90 line | 0 | 0 | 700 | D/E | F5/T11 | Lane reward; line geometry must prove target-count safety. |
| `Masamune` | Regen plus CT +8 | 0 | 0 | 850 | D/E | T3/T5/T10/T11 | Small area ally momentum; no Haste, Quick, Reraise, or action grant in this pass. |
| `Chirijiraden` | katana-spirit x1.10 | 0 | 0 | 1000 | E | F5/T11 | Premium finisher; narrow target shape required before final. |

`Murasame`, `Kiyomori`, and `Masamune` are support draws. They do not use the damage Iaido route.
They must remain small-area, non-looping support tools rather than replacements for White Mage,
Time Mage, Chemist, or Summoner.

`Muramasa` uses Slow instead of Doom or Confuse in this pass because Doom already has Monk and
Orator sources, and Confuse-style hard control would need a larger T8 policy before becoming a
Samurai area tool.

### Samurai Damage Rows

Late Samurai Iaido on-hit rows:

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| `Ashura` x0.60 | 63 | 72 | 92 | 97 |
| `Kotetsu` x0.75 | 78 | 91 | 115 | 121 |
| `Bizen Osafune` x0.90 | 94 | 109 | 138 | 145 |
| `Ame-no-Murakumo` x0.85 | 89 | 103 | 130 | 137 |
| `Muramasa` x0.50 | 52 | 60 | 76 | 81 |
| `Kiku-ichimonji` x0.90 | 94 | 109 | 138 | 145 |
| `Chirijiraden` x1.10 | 115 | 133 | 169 | 178 |

Mid fractional-WP proof rows:

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Samurai katana, mid | 61 | 70 | 89 | 94 |
| `Ashura` x0.60, mid | 36 | 42 | 53 | 56 |
| `Kotetsu` x0.75, mid | 46 | 53 | 67 | 70 |
| `Bizen Osafune` x0.90, mid | 55 | 63 | 80 | 85 |
| `Chirijiraden` x1.10, mid | 67 | 77 | 98 | 103 |

Read:

- ordinary katana attack remains the direct single-target line;
- Iaido spends damage for shape, support, status, or premium identity;
- `Chirijiraden` can exceed ordinary attack only as a late, narrow, expensive payoff.

## Ninja Baseline Check

Ninja active value is two-hit melee pressure plus finite Throw reach. Learned `Dual Wield` is still
deferred RSM work.

Accepted active Ninja dual-wield boundary:

```text
active Ninja innate dual wield = two eligible weapon hits
eligible in this pass = ninja_blade or knife
second hit multiplier = 1.00
does not apply to Throw, Iaido, spells, reactions, fists, or flails
learned Dual Wield support = deferred D/E build-defining engine
```

Ninja melee rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Ninja blade single, mid | 57 | 65 | 83 | 87 |
| Ninja blade dual, mid | 114 | 130 | 166 | 174 |
| Knife single, mid | 56 | 89 | 78 | 81 |
| Knife dual, mid | 112 | 178 | 156 | 162 |
| Ninja blade single, late | 92 | 107 | 135 | 143 |
| Ninja blade dual, late | 184 | 214 | 270 | 286 |
| Knife single, late | 91 | 145 | 127 | 133 |
| Knife dual, late | 182 | 290 | 254 | 266 |

Rejected flail warning rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Flail single, mid | 113 | 94 | 99 | 99 |
| Flail dual, mid | 226 | 188 | 198 | 198 |
| Flail single, late | 179 | 148 | 156 | 156 |
| Flail dual, late | 358 | 296 | 312 | 312 |

Read:

- Ninja can be a terrifying advanced melee burst job without also owning crush;
- knife dual gives a clear anti-mail burst, but it is adjacent, leather-fragile, and advanced;
- flail dual would erase armor identity, so it is rejected/deferred here.

## Ninja Values

Throw should be tactical reach, not remote access to every weapon formula.

| Skill | Throw value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Throw Shuriken` | 7 | 0 | 0 | 80 | D | T4/T9/F5 | Low-cost reach; below melee. |
| `Throw Daggers` | 8 | 0 | 0 | 140 | D | T4/T9/F5 | Light precision throw; does not inherit knife thrust. |
| `Throw Swords` | 9 | 0 | 0 | 180 | D | T4/T9/F5 | Generic weapon throw; does not inherit sword swing routine. |
| `Throw Flails` | 10 | 0 | 0 | 220 | D | T4/T9/F5 | Missile throw only; no ranged crush. |
| `Throw Katana` | 10 | 0 | 0 | 260 | D | T4/T9/F5 | Does not inherit Brave katana routine or Iaido identity. |
| `Throw Ninja Blades` | 12 | 0 | 0 | 360 | D/E | T4/T9/F5 | Signature thrown burst; high resource/cost pressure. |
| `Throw Axes` | 11 | 0 | 0 | 320 | D/E | T4/T9/F5 | Heavy throw; no random axe routine. |
| `Throw Polearms` | 10 | 0 | 0 | 300 | D | T4/T9/F5 | Does not replace Dragoon spear/thrust reach. |
| `Throw Poles` | 9 | 0 | 0 | 260 | D | T4/T9/F5 | Odd category; candidate for consolidation if duplicate. |
| `Throw Knight's Swords` | 13 | 0 | 0 | 500 | E | T4/T9/F5 | Premium finite throw; not ordinary baseline. |
| `Throw Books` | 9 | 0 | 0 | 240 | D | T4/T9/F5 | Odd category; candidate for consolidation if duplicate. |
| `Throw Bombs` | 10 | 0 | 0 | 200 | D | T4/T9/F5 | Missile special in this pass; element/status deferred. |

All Throw rows are single-target by default. Inventory consumption, item supply, shop timing, gil
pressure, and post-battle economy remain deferred to the campaign economy policy. If resource
friction proves unimplementable or unfun, Throw values must be retuned downward instead of relying
on invisible scarcity.

### Throw Damage Rows

Mid Throw rows:

| Throw | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Shuriken | 40 | 51 | 46 | 48 |
| Daggers | 46 | 59 | 52 | 55 |
| Swords | 52 | 66 | 59 | 61 |
| Flails | 58 | 74 | 66 | 68 |
| Katana | 58 | 74 | 66 | 68 |
| Ninja Blades | 69 | 89 | 79 | 82 |
| Axes | 63 | 81 | 72 | 75 |
| Polearms | 58 | 74 | 66 | 68 |
| Poles | 52 | 66 | 59 | 61 |
| Knight's Swords | 75 | 96 | 85 | 89 |
| Books | 52 | 66 | 59 | 61 |
| Bombs | 58 | 74 | 66 | 68 |

Late Throw rows:

| Throw | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Shuriken | 66 | 84 | 75 | 78 |
| Daggers | 75 | 96 | 86 | 89 |
| Swords | 85 | 108 | 97 | 100 |
| Flails | 94 | 121 | 107 | 112 |
| Katana | 94 | 121 | 107 | 112 |
| Ninja Blades | 113 | 145 | 129 | 134 |
| Axes | 104 | 133 | 118 | 123 |
| Polearms | 94 | 121 | 107 | 112 |
| Poles | 85 | 108 | 97 | 100 |
| Knight's Swords | 122 | 157 | 140 | 145 |
| Books | 85 | 108 | 97 | 100 |
| Bombs | 94 | 121 | 107 | 112 |

Read:

- Throw is weaker than active dual-wield melee, as intended;
- expensive throws can matter when melee is unsafe;
- no Throw category steals Dragoon's thrust, Samurai's Brave katana, or Geomancer/Monk crush.

## RSM Boundary Values

Deferred, but bound by this pass:

| Piece | Boundary |
| --- | --- |
| `Shirahadori` | Must have a hard block-chance ceiling and must not cover magic, status, area effects, or all projectile/special attacks. |
| `Bonecrusher` | Deferred retaliation value; should not become the default counter over Monk/Thief/Knight reactions. |
| `Equip Katana` | D route unlock; cannot make Samurai only a support stop. |
| `Doublehand` | Protected x1.80 single-weapon engine; no stacking with `Dual Wield`; D/E incidence check required. |
| `Iaido Focus` | Narrow Iaido-only support if retained; no broad magic/damage support. |
| `Vanish` | T5xT8/T4 required; no permanent untargetable loop and no broad stealth-strike bypass. |
| `Reflexes` | Evasion fallback only if Vanish cannot be bounded; T4/T2.1 required. |
| `Dual Wield` | Protected two-hit engine; no fists, Throw, Iaido, spells, reactions, non-weapon specials, or `Doublehand` stacking. |
| `Throw Mastery` | Throw-only support; no compression with learned `Dual Wield` into safe ranged dominance. |
| `Move +3` | D/E donor-pull risk; must compete with Teleport, Move +2, and vertical tools. |
| `Ignore Terrain` | Optional Ninja movement alternative if `Move +3` is too universal. |

## Review Questions For Claude

1. Is rejecting/defering Ninja flail access the right call, given the dual-flail plate rows?
2. Is full x1.00/x1.00 active Ninja dual wield acceptable if learned `Dual Wield` remains D/E and
   excludes Throw/Iaido/fists/flail?
3. Are Iaido damage multipliers low enough for area/line shapes while keeping Samurai exciting?
4. Are `Murasame`, `Kiyomori`, and `Masamune` bounded enough to avoid replacing White/Time/Chemist?
5. Are Throw values strong enough to matter but low enough to avoid remote weapon-family theft?
