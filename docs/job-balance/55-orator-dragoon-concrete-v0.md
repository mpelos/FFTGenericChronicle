# Orator And Dragoon Concrete Provisional V0

Status: Accepted as W3 Orator/Dragoon concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/24-dragoon-samurai-v1-proposal.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/39-timed-untargetability-composition-schema.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-orator-dragoon-concrete-v0.json`

## Purpose

This is the fourth W3 physical/foundation concrete action-value producer.

It covers Orator and Dragoon because both jobs can easily become unhealthy if their identity
is priced incorrectly:

- Orator can become a mandatory Brave/Faith chore, a duplicate Mystic, or only an `Equip Guns`
  detour;
- Dragoon can become a generic spear Knight, a safe off-board loop, or a range/elevation tax ladder.

This document sets provisional action values and boundary values for validation. It does not
finalize reaction/support/movement values, prerequisite trees, permanent recruit/economy behavior,
monster interaction, exact range/line implementation, full RSM values, or W4/W5 incidence verdicts.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Orator Speechcraft records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- Dragoon Jump unlock records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `brave_up`, `brave_down`, `faith_up`, `faith_down`, `recruit`, `status_add`, `instant_ko`,
  `timing`, `jump`, `movement`, `equipment_unlock`, `reaction`, and `revive` tags in
  `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Charm, Berserk, Sleep, Doom, Silence, Jump, Charging, Faith, Brave/Chicken, KO, and Undead
  vocabulary in `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/reference/README.md` as the navigation layer.

The design preserves the FFT read: Orator speaks, recruits, manipulates morale, and uses guns;
Dragoon jumps, uses spears, exploits height, and risks delayed commitment. It does not preserve
vanilla's exact permanent Brave/Faith, instant-death, or Jump grind behavior.

## Scope Boundary

Included here:

- Orator Speechcraft action values;
- battle-scoped Brave/Faith boundaries;
- speech status boundaries for Entice, Condemn, Insult, and Mimic Darlavon;
- Orator gun baseline rows;
- Dragoon Jump damage, timing, and reach/elevation unlock values;
- T5 and T5xT8 timing rows for Jump.

Deferred:

- final `Bravery Surge`, `Faith Surge`, `Equip Guns`, `Tame`, `Beast Tongue`, and
  `Social Positioning` values;
- final `Dragonheart`, `Brace Landing`, `Equip Polearms`, `Jump Training`, movement `Jump +N`,
  and `Ignore Elevation` values;
- permanent recruitment, permanent Brave/Faith change, permanent gil/economy effects, monster
  interaction, and roster policy;
- exact speech range, line, immunity tables, boss resistance, and AI-control implementation;
- non-spear Jump routes;
- full W4 populated T2.1 incidence and W5 real-roster dominance verdict.

## Shared Formula Contracts

Physical rows use the current v0.2.1 family model from `work/sim-inputs-v0.2.1.json`.
The bundle pins effective weapon power as continuous after phase scaling:

```text
effective_wp = family_wp * phase_wp_scalar
effwp_rounding = none
```

Damage is quantized at the final `floor` step, not when weapon power is scaled. This keeps doc 55
consistent with the committed Squire/Chemist, Knight/Archer, and Monk/Thief concrete rows.

Orator's gun lane is intentionally stat-independent:

```text
routine = wp_wp
damage_type = missile
penetration = 0.70
```

This makes gun access useful for low-stat utility jobs, but also makes `Equip Guns` a high-risk
support because it can patch too many builds. This pass gives Orator gun baseline visibility but
does not finalize `Equip Guns`.

Dragoon's spear lane is:

```text
routine = pa_wp
damage_type = thrust
penetration = 0.10
```

Dragoon should be a clean anti-mail reach job. Plate resists thrust, so Jump is not allowed to
become the universal answer to heavy armor.

Speech effects are battle-scoped in this pass. Permanent Brave/Faith, recruit, gil, monster, or
economy effects are deferred campaign policy, not hidden combat value.

Enemy speech is status-like: it must respect Silence on the Orator, immunity/resistance on the
target, and final T4 range/line rules. Ally morale speech is allowed to be reliable because it
costs an action and is bounded by small caps.

## Orator Baseline Check

Mid Orator and Chemist gun rows are identical because guns are `wp_wp`.

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Orator gun, mid | 81 | 89 | 85 | 86 |
| Chemist gun, mid | 81 | 89 | 85 | 86 |
| Orator gun, late | 145 | 158 | 151 | 154 |

Read:

- Orator always has a useful fallback when speech is resisted;
- Orator cannot be balanced by PA/MA stat weakness if gun access is too broad;
- `Equip Guns` must be treated as a C/D strong global piece, not a flavor unlock.

## Orator Values

Orator should create social and morale windows. It should not become a better Mystic, a safer
Thief Charm button, or a mandatory permanent-stat chore.

Speech accuracy boundary:

```text
ally morale speech = reliable on eligible allies
enemy debuff speech base rate = 60%
enemy hard-status speech base rate = 30-35%
speech requires line/range in final T4 rules
Silence blocks Speechcraft
boss/protected target policy = deferred immunity/resistance row
```

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Entice` | temporary recruit/control 35% base | 0 | 0 | 220 | C/D | T4/T8/economy | Eligible human targets only in this pass; one controlled action or damage break; no permanent recruit. |
| `Stall` | CT -12, 60% base | 0 | 0 | 120 | C | T5/T4 | Tempo speech; no Slow, no Speed stat change, no hard lock. |
| `Praise` | Brave +8, cap 80 | 0 | 0 | 200 | C | F5/T2.1 | Battle-scoped; one active Orator Brave-up speech per target; no permanent Brave chore. |
| `Intimidate` | Brave -8, floor 50, 60% base | 0 | 0 | 200 | C | F5/T4/T2.1 | Battle-scoped; does not cause Chicken; anti-reaction/anti-Brave scaling. |
| `Preach` | Faith +5, cap 80 | 0 | 0 | 200 | C/D | F4/F5/T2.1 | Battle-scoped and weaker than Mystic Faith control; double-edged because target also receives more magic. |
| `Enlighten` | Faith -5, floor 50, 60% base | 0 | 0 | 200 | C/D | F4/T4/T2.1 | Anti-caster or anti-healing setup; cannot invalidate ordinary magic by itself. |
| `Condemn` | Doom 35% base, countdown 4 | 0 | 0 | 450 | C/D | T4/T5/T8 | Delayed lethal pressure; no instant KO in this pass; immunity respected. |
| `Defraud` | battle-scoped gil/economy marker | 0 | 0 | 100 | C | economy | No combat power accepted here; permanent reward deferred. |
| `Insult` | Berserk 30% base | 0 | 0 | 300 | C/D | T4/T5/T8 | Expires after one forced enemy action or damage break; not Confuse and not broad hard control. |
| `Mimic Darlavon` | Sleep 30% base | 0 | 0 | 300 | C/D | T4/T5/T8 | Damage break; expires after the target misses one action; single-target only in this pass. |

`Preach` and `Enlighten` deliberately move Faith less than `Praise` and `Intimidate` move Brave.
Orator owns morale first; Mystic owns spiritual/Faith control first.
At ordinary 70/70 Faith, `Preach` is intentionally not a default damage button because the formula
stays on the 0.60 Faith floor. Its main role is high-Faith setup, countersetup, healing/risk
planning, and interaction with later Mystic-led Faith routes.

`Entice` is separated from Thief `Steal Heart` by channel and cost: Thief gets chip plus a
short-range Charm attempt; Orator gets speech-range temporary control/recruit texture with no
weapon damage and deferred campaign reward.

`Condemn` is separated from Monk `Doom Fist`: Monk's version is adjacent, has chip, and uses a
shorter countdown; Orator's version is ranged speech pressure with lower immediacy and no damage.
Because this creates a second Doom source, T8 must prove a real cure, immunity, or encounter policy
before Doom becomes common enemy pressure.

### Brave And Faith Rows

Brave impact rows, cloth target:

| Route | Brave 62 | Brave 70 | Brave 78 | Brave 80 | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| Monk fists, mid | 60 | 71 | 71 | 81 | `Praise +8` may be a threshold setup, not guaranteed damage. |
| Monk fists, late | 105 | 118 | 131 | 131 | Late Brave routes get about +11% from +8 Brave. |
| Samurai katana, late | 144 | 162 | 180 | 180 | Brave routes benefit, but not enough to make Orator mandatory. |
| Knight sword, late | 140 | 160 | 180 | 180 | Knight-sword Brave routes also need F5 incidence checks. |

Faith impact rows, Black Mage mid K20 sample:

| Caster Faith | Target Faith | Damage | Read |
| ---: | ---: | ---: | --- |
| 70 | 70 | 144 | baseline at the 0.60 Faith floor |
| 75 | 70 | 144 | one `Preach` on caster does not move ordinary damage |
| 70 | 75 | 144 | one `Preach` on target does not move ordinary damage |
| 75 | 75 | 144 | two small boosts still stay on the floor |
| 80 | 80 | 153 | high setup finally moves damage by about +6% |
| 85 | 85 | 173 | stress setup is real and must stay Mystic-led |
| 85 | 80 | 163 | one `Enlighten` meaningfully trims high-Faith stress |

Read:

- Orator Faith speech is a setup/countersetup tool, not a default caster damage routine;
- Brave speech has clearer physical value, which matches Orator's morale identity;
- permanent Brave/Faith manipulation would create chores and remains rejected for this pass.

## Dragoon Baseline Check

Mid and late spear rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Dragoon spear, mid | 78 | 123 | 108 | 113 |
| `Jump` spear x1.25, mid | 97 | 154 | 135 | 142 |
| Dragoon spear, late | 125 | 198 | 173 | 181 |
| `Jump` spear x1.25, late | 156 | 247 | 217 | 227 |

Read:

- Dragoon has a strong anti-mail spear lane;
- plate still resists Dragoon enough that Monk/Knight/armor setup remain relevant;
- `Jump x1.25` is acceptable only because it pays CT, prediction, whiff, and landing risk.

## Dragoon Values

Dragoon should buy better ways to Jump, not a stack of boring numeric damage upgrades.

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Jump` | spear x1.25 | 0 | `ceil(50 / Speed)` | 200 | C | T4/T5/T5xT8/F5 | Spear/thrust only in this pass; whiffs if target/panel is invalid at resolution. |
| `Horizontal Jump +1` | max Jump reach 3 | 0 | passive unlock | 120 | C | T5/T8 | Early usability band; no damage bonus. |
| `Horizontal Jump +2` | max Jump reach 4 | 0 | passive unlock | 240 | C | T5/T8 | Ordinary committed reach; no damage bonus. |
| `Horizontal Jump +3` | max Jump reach 5 | 0 | passive unlock | 380 | C/D | T5/T8/T2.1 | Stronger map control; no Archer replacement. |
| `Horizontal Jump +4` | max Jump reach 6 | 0 | passive unlock | 550 | D | T5/T8/T2.1 | High investment reach. |
| `Horizontal Jump +7` | max Jump reach 8 | 0 | passive unlock | 800 | D/E | T5/T8/T2.1 | Specialist capstone reach; still not safe ranged melee. |
| `Vertical Jump +/-2` | vertical tolerance 2 | 0 | passive unlock | 100 | C | T5/T8 | Basic height usability. |
| `Vertical Jump +/-3` | vertical tolerance 3 | 0 | passive unlock | 180 | C | T5/T8 | Early elevation reliability. |
| `Vertical Jump +/-4` | vertical tolerance 4 | 0 | passive unlock | 300 | C/D | T5/T8 | Meaningful height-map investment. |
| `Vertical Jump +/-5` | vertical tolerance 5 | 0 | passive unlock | 420 | C/D | T5/T8 | Committed elevation reach. |
| `Vertical Jump +/-6` | vertical tolerance 6 | 0 | passive unlock | 560 | D | T5/T8/T2.1 | Late vertical mastery. |
| `Vertical Jump +/-7` | vertical tolerance 7 | 0 | passive unlock | 720 | D | T5/T8/T2.1 | High cliff answer. |
| `Vertical Jump +/-8` | vertical tolerance 8 | 0 | passive unlock | 900 | D/E | T5/T8/T2.1 | Master elevation answer, competing with `Ignore Elevation`. |

The horizontal values are design reach bands, not a requirement to preserve vanilla's literal UI
ladder. If implementation keeps vanilla slot names, the player-facing behavior should still be
clear: higher horizontal mastery expands Jump target choice, not damage.

Jump is normally allowed to be non-evadable only while these costs remain true:

- it has delayed resolution from T5;
- same-tick resolution versus target action is unsafe;
- the target can move, become untargetable, die, or otherwise invalidate the locked panel/target;
- airborne untargetability expires before targeting/resolution at the landing tick;
- the Dragoon returns to board state after landing and can be punished.

No `Jump Training` CT reduction is accepted in this action pass. If a later support makes Jump
faster, it must re-open T5, T5xT8, and F5 rows because `Jump x1.25` was tuned assuming the current
delay.

### Dragoon Timing Rows

Jump timing:

| Speed | `jump_ticks` |
| ---: | ---: |
| 6 | 9 |
| 7 | 8 |
| 8 | 7 |
| 9 | 6 |
| 10 | 5 |
| 12 | 5 |

Speed 7 target race against Speed 7 Jump:

| Target CT | Target ticks to action | Verdict |
| ---: | ---: | --- |
| 30 | 10 | Jump lands before target turn. |
| 40 | 9 | Jump lands before target turn. |
| 44 | 8 | Same-tick unsafe. |
| 50 | 8 | Same-tick unsafe. |
| 58 | 6 | Target can act before landing. |
| 70 | 5 | Target can act before landing. |

Airborne targetability window for a Speed 7 Jump starting at tick 0:

| Evaluation tick | Airborne untargetable |
| ---: | --- |
| 0 | yes |
| 1 | yes |
| 4 | yes |
| 7 | yes |
| 8 | no |
| 9 | no |

Read:

- Jump is safest against low-CT or committed targets, not fast targets near their turn;
- same-tick expiry is intentionally not safe;
- enemies regain agency at landing, so Jump cannot be balanced as permanent safety.

## Review Questions For Claude

1. Are Orator's `Praise +8` and `Intimidate -8` acceptable with cap/floor and battle-scoped
   non-permanence, or should morale speech be smaller?
2. Is Orator's Faith movement at `+/-5` too weak to matter, or is that weakness desirable because
   Mystic owns Faith control?
3. Are `Entice`, `Insult`, `Mimic Darlavon`, and `Condemn` status rates low enough given speech
   range and no weapon damage?
4. Is `Jump x1.25` viable with `ceil(50 / Speed)` delay and T5xT8 whiff rules, or should damage
   fall to x1.15 before reach unlocks are tested?
5. Are the Dragoon reach bands sufficient to make Jump progression interesting without replacing
   Archer range or late movement choices?
