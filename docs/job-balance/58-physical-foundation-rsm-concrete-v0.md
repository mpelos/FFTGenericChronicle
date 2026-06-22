# Physical Foundation RSM Concrete Provisional V0

Status: Accepted as W3 physical/foundation R/S/M concrete-provisional values
Date: 2026-06-22
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/54-monk-thief-concrete-v0.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/job-balance/56-samurai-ninja-concrete-v0.md`
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-physical-foundation-rsm-concrete-v0.json`

## Purpose

This is the W3 candidate producer for concrete-provisional reaction, support, and movement values
for the physical/foundation roster and Ramza.

It covers:

- `Squire`;
- `Chemist`;
- `Knight`;
- `Archer`;
- `Monk`;
- `Thief`;
- `Orator`;
- `Dragoon`;
- `Samurai`;
- `Ninja`;
- `Vanguard`;
- Ramza's chapter-progressing protagonist job.

It does not finalize the exact prerequisite tree, final JP economy, equipment availability timing,
caster RSM, performer RSM, Necromancer RSM, monster scope, or W4/W5 pass/fail incidence.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning R/S/M values:

- `docs/reference/README.md` as the navigation entrypoint;
- `docs/reference/fft-vanilla-ability-effect-index.md` for individual reaction, support, and
  movement ability records;
- `docs/reference/fft-vanilla-command-skillset-effect-map.md` for the vanilla Reaction, Support,
  Movement, Jump unlock, and relevant job-command palettes;
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md` for `reaction`, `support`,
  `movement`, `defense`, `accuracy`, `damage_boost`, `equipment_unlock`, `jp_exp`, `healing`,
  `revive`, `timing`, and `throw` overlap;
- `docs/reference/fft-vanilla-status-effect-map.md` for `Invisible`, `Reraise`, `Protect`,
  `Shell`, `Critical`, `Defending`, and movement/status vocabulary.

Vanilla overlap checked:

| Effect family | Vanilla records checked | This producer read |
| --- | --- | --- |
| Reaction defense | `Parry`, `Shirahadori`, `Reflexes`, `Archer's Bane`, `Vanish`, `Auto-Potion`, `Dragonheart` | Preserved as recognizable vocabulary, but capped and channel-bound. |
| Reaction retaliation | `Counter`, `Counter Tackle`, `Bonecrusher`, `First Strike`, `Nature's Wrath` | Preserved as narrow retaliation; no recursion and no broad immunity. |
| Reaction stat/morale | `Bravery Surge`, `Faith Surge`, `Speed Surge`, `Critical: Quick` | Brave/Faith kept battle-scoped; Speed Save becomes CT gain, not Speed stat growth. |
| Equipment unlock supports | `Equip Heavy Armor`, `Equip Shields`, `Equip Katana`, `Equip Crossbows`, `Equip Polearms`, `Equip Guns`, `Reequip` | Retained as build routes but band/equipment-gated; no broad `Equip Swords`. |
| Damage/support engines | `Brawler`, `Doublehand`, `Dual Wield`, `Attack Boost`, `Concentration` | Stress engines remain recognizable; support-slot conflicts are explicit. |
| Progression/economy supports | `EXP Boost`, `Poach`, `Tame`, `Beast Tongue` | `JP Boost` is intentionally removed by doc 61; monster/economy supports remain deferred. |
| Movement | `Movement +1/+2/+3`, `Jump +1/+2/+3`, `Ignore Elevation`, `Ignore Terrain`, `Lifefont`, `Manafont`, `Teleport`, `Fly`, `Treasure Hunter` | Movement ladder split by role so no single late default is accepted. |

No vanilla R/S/M family is intentionally omitted from this physical/foundation pass without being
retained, narrowed, rejected, or deferred above.

## Bundle Pin

The RSM constants used below are pinned in `work/sim-inputs-v0.2.1.json`:

```text
rsm_constants.version = physical_foundation_rsm_v0_proposed
```

The pin was committed in:

```text
1ef121e66529b0c5fd5feb609b0a1d378fb04f17
```

The committed bundle blob hash is:

```text
d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95
```

The existing stress engines remain unchanged:

| Stress engine | Value |
| --- | ---: |
| `two_hands` | 1.80 |
| `two_swords_hits` | 2 |
| `attack_boost` | 1.3333333333333333 |
| `high_brave` | 97 |
| `accuracy_evasive_hitrate` | 0.75 |

## Slot Model And Achievability

Each unit has one reaction slot, one support slot, and one movement slot.

Any row combining two support-slot abilities is a theoretical stress probe unless one part is
native/innate to the active job. It is not a legal single-unit build.

Important consequences:

- `Brawler + Martial Discipline` is a theoretical multiplicative probe, not an equippable build;
  both are Monk support skills. The legal unarmed support ceiling is `Brawler` alone at x1.25.
- Samurai cannot equip both `Equip Knight Swords` and `Doublehand`; both are support-slot effects.
  Samurai knight-sword rows are stat-ceiling probes, not legal Samurai builds.
- A native active Ninja can use innate two-hit melee while equipping a support such as `Attack
  Boost`. A non-Ninja using learned `Dual Wield` spends the support slot and cannot also equip
  `Attack Boost`.

## Shared Reaction Policy

Unless a row says otherwise, reactions use:

```text
trigger chance = min(Brave / 100, trigger_cap)
```

Global reaction rules:

- one roll per triggering action;
- no reaction recursion;
- damage caused by reactions cannot trigger reactions;
- one round-cap trigger for ordinary capped reactions unless a stricter cap is listed;
- the strongest single mitigation channel applies; mitigation channels do not multiply.

This deliberately keeps Brave meaningful without letting high-Brave stacking turn defensive
reactions into practical immunity.

## Strongest Single Mitigation Channel

The following all belong to the same mitigation channel:

- `Grit`;
- `Parry`;
- `Brace`;
- `Arrow Guard`;
- `Reflexes`;
- `Brace Landing`;
- Vanguard `Intervention`;
- `Armor Discipline`;
- `Protect`;
- `Shell`;
- Samurai `Kiyomori`-style mitigation from doc 56;
- Vanguard `Aegis Stance`;
- Vanguard `Intercede`.

If multiple channels apply to the same hit, use the single strongest applicable result. Do not
multiply them together.

This channel is separate from the base armor-response table. `Equip Armor` changes what armor class
the unit can actually wear; it does not create an extra free mitigation layer.

### Defensive Stack Proof Row

Incoming hit: `120`.

| Channel | Multiplier | Result |
| --- | ---: | ---: |
| `Armor Discipline` | 0.90 | 108 |
| `Aegis Stance` | 0.85 | 102 |
| `Protect` | 0.667 | 80 |
| `Parry` | 0.60 | 72 |
| Product if incorrectly stacked | 0.306 | 36 |
| Accepted strongest-single result | 0.60 | 72 |

The accepted result is `72`, not `36`.

## Accuracy And Evasion Assumptions

The bundle does not yet contain the full T4 facing/evasion model. This producer uses the current
reviewer-derived T4 assumptions and marks them proof-first:

- facing is multiplicative;
- final hit rounding uses round-style behavior rather than floor-only behavior;
- class evade is non-directional;
- front-facing physical checks can include shield, accessory, and weapon evade;
- side checks can include shield;
- rear checks can include class evade;
- status immunity and boss protection remain separate from hit rate.

`Concentration` is not vanilla "ignore everything." It only raises eligible physical weapon and
Archer action reliability against evasive targets to the bundle stress row:

```text
accuracy_evasive_hitrate = 0.75
```

It does not bypass reaction rolls, boss/status immunity, equipment state, resource cost, CT timing,
or the strongest-single mitigation channel.

`Shirahadori` is a reaction block with cap `0.55`. It does not cover magic, status-only actions,
area effects, guns, Throw, bombs, Jump landings, monster specials, or proof-first unique actions.

`Vanish` is a one-action Invisible window after a successful reaction. It gives no stealth damage
bonus, no guaranteed back attack, no permanent untargetability loop, and no status-immunity bypass.

`Reflexes` is represented as damage reduction to `0.70` for eligible direct weapon hits, not as
another independent evasion layer.

## Job RSM Values

### Squire

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Grit` | Reaction | A/B | cap 0.65; next incoming direct hit x0.90 | Early morale defense; one hit only; channel member. |
| `Basic Training` | Support | B/C | Squire/Fundaments-style action output x1.10 | Squire's sole intentional support export; does not affect ordinary attacks, weapons, spells, items, or `Ultima`. |
| `Move +1` | Movement | A | Move +1 | Early floor comfort; no terrain/elevation bypass. |

`Basic Training` is intentionally narrow. If W4 shows that it is weak or never chosen, the first fix
direction is improving Squire actions, not broadening this support into generic physical, weapon,
spell, or item output.

### Chemist

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Throw Item` | Support | A/B | item range +2 | Requires item stock; does not improve item power. |
| `Auto-Potion` | Reaction | C | cap 0.70; Potion-only 30 HP; 1/round | Post-damage, survivor-only, no Item Lore interaction, no Hi/X-Potion. |
| `Item Lore` | Support | C | HP items x1.30; Ether items x1.20 | Does not modify Auto-Potion; economy-gated. |
| `Safeguard` | Support | C | blocks battle-scoped equipment break/steal effects | No generic damage mitigation. |
| `Reequip` | Support | C/D | tactical equipment swap hook only | Final implementation/equipment timing deferred. |
| `Move-Find Item` | Movement | B/C | campaign treasure hook | No combat mobility value accepted here. |

This producer finalizes the doc 52 Auto-Potion boundary. The late value is intentionally flat and
small: 30 HP does not scale into late practical immunity.

### Knight

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Parry` | Reaction | B/C | cap 0.60; eligible direct physical hit x0.60 | Channel member; no magic/status/area coverage. |
| `Brace` | Reaction | B/C | cap 0.65; eligible direct physical hit x0.80 | Weaker broad fallback; channel member. |
| `Equip Armor` | Support | C | enables heavy armor use | Changes armor profile; no extra multiplier. |
| `Equip Shield` | Support | C | enables shield use | T4 evasion proof pending; no free mitigation layer. |
| `Defensive Training` | Support | C/D | Arts of War/guard effects +1 protected action where applicable | Does not reduce all incoming damage. |
| `Shield March` | Movement | C | Move +1 while using shield or heavy posture | No terrain/elevation bypass. |

`Equip Armor` preserves armor identity. Against an incoming neutral `120`, cloth-to-plate changes:

| Damage type | Cloth result | Plate result |
| --- | ---: | ---: |
| Swing | 120 | 78 |
| Thrust | 120 | 78 |
| Crush | 120 | 138 |
| Missile | 120 | 96 |

Plate helps against swing/thrust/missile but becomes worse against crush.

### Archer

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Arrow Guard` | Reaction | B/C | cap 0.65; missile hit x0.50 | Channel member; missile only. |
| `Speed Save` | Reaction | C/D | cap 0.55; +4 CT, 1/round | No Speed stat snowball. |
| `Equip Bow` | Support | C | enables longbow/crossbow use | No guns; Archer remains native bow shell. |
| `Concentration` | Support | C/D | eligible evasive hit-rate floor 0.75 | Physical weapon/Archer actions only; no status/boss bypass. |
| `Bow Mastery` | Support | C | bow/crossbow damage x1.10; hit +0.05 | Narrow bow route; competes with Concentration. |
| `Jump +1` | Movement | B | Jump +1 | Early vertical answer; no Move bonus. |

Representative longbow rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Archer longbow late | 114 | 148 | 131 | 137 |
| Bow Mastery late | 125 | 163 | 144 | 150 |
| Concentration late evasive expected | 85.5 | 111 | 98.25 | 102.75 |
| Archer longbow stress | 139 | 181 | 160 | 167 |
| Bow Mastery stress | 153 | 199 | 176 | 184 |

### Monk

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Counter` | Reaction | B/C | cap 0.70; adjacent retaliation x0.75 | Does not prevent incoming hit; no recursion. |
| `First Strike` | Reaction | D | cap 0.45; pre-hit adjacent retaliation x0.70 | Late, adjacent direct weapon only; no broad denial. |
| `Brawler` | Support | C/D | unarmed/fists output x1.25 | Build-defining unarmed route; no weapons, Throw, spells, or Dual Wield. |
| `Martial Discipline` | Support | C | Monk action output x1.10 | Action-set support; does not modify all physical attacks. |
| `Lifefont` | Movement | C | heal 8% max HP after voluntary movement, cap 40 | HP only; no MP; no heal if immobilized/no movement. |

Monk unarmed convergence row:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Monk fists mid | 80 | 68 | 71 | 71 |
| Brawler mid | 100 | 85 | 88 | 88 |
| Brawler + Martial Discipline mid | 110 | 93 | 97 | 97 |
| Monk fists late | 134 | 113 | 118 | 118 |
| Brawler late | 168 | 142 | 148 | 148 |
| Brawler + Martial Discipline late | 185 | 156 | 163 | 163 |
| Monk fists stress | 172 | 145 | 152 | 152 |
| Brawler + Martial Discipline stress | 237 | 200 | 209 | 209 |

The combined `1.375x` row is a theoretical two-support probe. It is not a legal single-unit build.
The real buildable unarmed support ceiling is `Brawler` alone at x1.25. `Martial Discipline` is an
alternative support for Monk-action specialists, not a stackable second support.

### Thief

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Sticky Fingers` | Reaction | C | cap 0.60; battle-scoped steal/economy response | No damage prevention; no permanent reward accepted here. |
| `Light Fingers` | Support | C | Steal hit +0.15 | Steal actions only; does not stack with Concentration. |
| `Poach` | Support | Deferred | monster/economy route | Monsters remain out of scope. |
| `Move +2` | Movement | C | Move +2 | No terrain/elevation bypass; competes with role movement. |
| `Treasure Hunter` | Movement | C | campaign treasure hook | No combat mobility value accepted here. |

Thief's RSM value is mobility, stealing reliability, and economy texture, not raw combat math.

### Orator

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Bravery Surge` | Reaction | C/D | cap 0.60; Brave +4 battle-scoped, cap 80 | No permanent Brave; no damage prevention. |
| `Faith Surge` | Reaction | C/D | cap 0.60; Faith +4 battle-scoped, cap 80 | High-risk caster/vulnerability lever. |
| `Equip Guns` | Support | C/D | enables gun use | Requires gun availability timing; no PA scaling. |
| `Tame` | Support | Deferred | monster route | Out of current no-monster pass. |
| `Beast Tongue` | Support | Deferred | monster route | Out of current no-monster pass. |
| `Social Positioning` | Movement | C | Move +1; Speechcraft hit +0.05 after movement | Orator/speech only; no terrain/elevation bypass. |

`Equip Guns` remains a high-risk equipment unlock because guns are PA-independent. The equipment availability
producer must prove when this support is practically online.

### Dragoon

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Dragonheart` | Reaction | C/D | cap 0.35; once/battle Reraise at 20% HP | No re-trigger while active; no immortality loop. |
| `Brace Landing` | Reaction | C | cap 0.60; next hit after landing x0.85 | Jump route only; channel member. |
| `Equip Polearms` | Support | C/D | enables spear/polearm use | Dragoon remains native spear shell. |
| `Jump Training` | Support | C | Jump action horizontal +1 and vertical +1 | No CT reduction, no damage bonus. |
| `Jump +1` | Movement | B/C | Jump +1 | Vertical answer. |
| `Jump +2` | Movement | C | Jump +2 | More commitment than `Jump +1`. |
| `Jump +3` | Movement | D | Jump +3 | Advanced vertical tool. |
| `Ignore Elevation` | Movement | D/E | ignores vertical movement limit | No Move bonus; no terrain bypass. |

This keeps Dragoon as the vertical specialist without making `Ignore Elevation` a universal
horizontal movement answer.

### Samurai

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Shirahadori` | Reaction | D/E | cap 0.55; block eligible direct weapon hit | Does not cover magic/status/area/guns/Throw/Jump/specials. |
| `Bonecrusher` | Reaction | D | cap 0.65; retaliation x0.85 after surviving | Narrower alternative to Shirahadori. |
| `Equip Katana` | Support | D | enables katana use | Katana availability timing required. |
| `Doublehand` | Support | D/E | `two_hands` x1.80 | No Dual Wield; no shield/offhand; D/E only. |
| `Iaido Focus` | Support | D | Iaido hit/reliability +0.05 | Iaido only; no broad magic/damage support. |
| `Waterwalking` | Movement | C/D | water traversal | Map-dependent; not a general mobility answer. |
| `Blade Step` | Movement | D | Samurai stance/position movement hook | No Move +N default; final rule deferred. |

Doublehand rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Samurai katana late | 105 | 121 | 153 | 162 |
| Doublehand katana late | 189 | 218 | 277 | 291 |
| Doublehand katana stress | 210 | 243 | 307 | 324 |
| Knight-sword Knight late | 104 | 120 | 152 | 160 |
| Doublehand knight-sword Knight late | 187 | 216 | 273 | 288 |
| Knight-sword Samurai-stat probe stress | 130 | 150 | 190 | 200 |
| Doublehand knight-sword Samurai-stat probe stress | 234 | 270 | 342 | 360 |

`Doublehand` is intentionally strong, but it consumes the support slot, forbids offhand/shield, and
must remain D/E. The Samurai-stat knight-sword rows are not legal Samurai builds because a Samurai
would need both `Equip Knight Swords` and `Doublehand`. The legal knight-sword + `Doublehand`
warning row here is the Knight-hosted row.

### Ninja

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Vanish` | Reaction | D/E | cap 0.45; one-action Invisible | No stealth damage bonus; no permanent untargetable loop. |
| `Reflexes` | Reaction | D | cap 0.55; direct weapon hit x0.70 | Channel member; no independent evasion stacking. |
| `Dual Wield` | Support | D/E | `two_swords_hits` = 2 | No fists, Throw, Iaido, spells, reactions, non-weapon specials, or Doublehand. |
| `Throw Mastery` | Support | D | Throw damage x1.10; Throw range +1 | Throw only; inventory pressure remains. |
| `Move +3` | Movement | D/E | Move +3 | No terrain/elevation bypass. |
| `Ignore Terrain` | Movement | D | ignores terrain cost/effects | No Move +3; no elevation bypass. |

Dual Wield convergence rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Ninja blade single late | 92 | 107 | 135 | 143 |
| Dual Wield ninja blade late | 184 | 214 | 270 | 286 |
| Dual Wield + Attack Boost ninja blade late | 246 | 286 | 362 | 380 |
| Dual Wield + Attack Boost ninja blade stress | 292 | 338 | 428 | 450 |
| Knife dual late | 182 | 290 | 254 | 266 |
| Dual Wield + Attack Boost knife late | 244 | 386 | 338 | 354 |
| Dual Wield + Attack Boost knife stress | 288 | 456 | 400 | 420 |

`Attack Boost` is not assigned by this producer. It appears here only as a protected stress engine
for convergence testing. `Dual Wield + Attack Boost` is only buildable on a native active Ninja,
because Ninja's two-hit melee is innate. A non-Ninja using learned `Dual Wield` cannot also equip
`Attack Boost`.

Throw Mastery examples:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Throw Ninja Blades late | 113 | 145 | 129 | 134 |
| Throw Mastery Ninja Blades late | 124 | 159 | 142 | 148 |
| Throw Knight Swords late | 122 | 157 | 140 | 145 |
| Throw Mastery Knight Swords late | 135 | 173 | 154 | 160 |

### Vanguard

| Piece | Slot | Band | Value | Boundary |
| --- | --- | --- | --- | --- |
| `Intervention` | Reaction | E | cap 0.60; adjacent ally hit x0.80; Vanguard chip 0.20 | Local ally protection only; no global cover; no extra attack. |
| `Last Stand` | Reaction | E | cap 0.45; once/battle HP floor 1 | Survival panic button, not routine mitigation. |
| `Equip Knight Swords` | Support | E | enables knight swords | Optional and cuttable if sword dominance returns. |
| `Vanguard Training` | Support | E | Vanguard action output x1.10 | Vanguard actions only; no broad physical boost. |
| `Armor Discipline` | Support | E | mitigation x0.90 in strongest-single channel | Not multiplicative with Protect/Aegis/Parry. |
| `Vanguard March` | Movement | E | Move +1 in heavy/formation posture | Formation play; no terrain/elevation bypass. |

`Intervention` and `Intercede` are intentionally different:

| Piece | Slot | Ally multiplier | Vanguard chip | Reliability |
| --- | --- | ---: | ---: | --- |
| `Intercede` | Action | 0.75 | 0.25 | deliberate action |
| `Intervention` | Reaction | 0.80 | 0.20 | cap 0.60 reaction |

The reaction is weaker and chance-gated because it costs no action.

Vanguard Training examples:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Breach axe | 112 | 92 | 97 | 97 |
| Breach axe + Vanguard Training | 123 | 101 | 107 | 107 |
| Decisive sword setup | 149 | 172 | 218 | 230 |
| Decisive sword setup + Vanguard Training | 164 | 190 | 240 | 253 |
| Decisive spear setup | 150 | 237 | 208 | 218 |
| Decisive spear setup + Vanguard Training | 165 | 261 | 229 | 239 |

### Ramza

Ramza receives no unique exportable R/S/M in this producer.

His chapter job may use generic learned reaction/support/movement pieces like any other active job,
but this producer does not add a Ramza-only support, reaction, or movement reward. That keeps his
always-present breadth from becoming a hidden mandatory route.

Ramza's chapter identity remains action-led:

- Chapter 1: Squire-floor parity plus `Tailwind`;
- Chapter 2: `Steel` and `Chant`;
- Chapter 3: `Spellblade` and `Ward`;
- Chapter 4: `Shout`, `Arc Blade`, and `Ultima`.

Ramza may become top-tier in Chapter 4, but he still must borrow specialist RSM through the normal
build system. This preserves the player-facing FFT pleasure of planning cross-job builds instead of
letting the protagonist solve reaction/support/movement alone.

## Movement Ranking

Movement pieces are ranked by role, not by one universal ladder.

| Piece | Band | What it solves | What it does not solve |
| --- | --- | --- | --- |
| `Move +1` | A | Early comfort | Terrain, elevation, late reach. |
| `Jump +1` | B | Early vertical maps | Horizontal reach. |
| `Shield March` | C | Heavy formation tempo | Terrain/elevation. |
| `Lifefont` | C | Bruiser attrition while moving | Burst survival, MP, terrain. |
| `Move +2` | C | Midgame skirmisher reach | Terrain/elevation. |
| `Social Positioning` | C | Orator speech/gun positioning | General traversal. |
| `Jump +2/+3` | C/D | Vertical identity | Horizontal reach. |
| `Ignore Terrain` | D | Terrain-cost maps | Raw range and elevation. |
| `Ignore Elevation` | D/E | Height maps | Terrain and horizontal reach. |
| `Move +3` | D/E | Elite raw reach | Terrain and elevation. |
| `Vanguard March` | E | Heavy formation entry/holding | General map skip. |
| `Teleport` | D/E caster comparison | Terrain/elevation bypass with its own proof track | Not assigned in this producer. |

No single movement piece is accepted as the universal late default. `Move +3` gives raw distance but
does not bypass terrain or elevation. `Ignore Elevation` solves height but not horizontal range.
`Ignore Terrain` solves terrain but not height. Formation movement is deliberately narrow.

## High-Risk Convergence Summary

| Stack | Result read |
| --- | --- |
| Dual Wield + Attack Boost ninja blade stress | 292/338/428/450 is buildable only on native active Ninja with innate two-hit melee; it must stay D/E, high-investment, and non-default. |
| Dual Wield + Attack Boost knife stress | 288/456/400/420 is especially dangerous into mail on native active Ninja and must not become the universal physical route. |
| Doublehand knight-sword Knight late | 187/216/273/288 is the real buildable knight-sword + `Doublehand` warning row. |
| Doublehand knight-sword Samurai-stat stress probe | 234/270/342/360 is a stat-ceiling probe, not a legal Samurai build. |
| Brawler + Martial Discipline stress probe | 237/200/209/209 is a two-support theoretical probe; the legal unarmed support ceiling is `Brawler` x1.25. |
| Mitigation stack | strongest-single gives 72 from incoming 120; incorrect multiplication would give 36 and is rejected. |

## W4/W5 Required Follow-Up

This proposal creates mandatory future rows:

- T2/T2.1 incidence for `Basic Training`, `Auto-Potion`, `Equip Armor`, `Equip Shield`,
  `Concentration`,
  `Brawler`, `Doublehand`, `Dual Wield`, `Move +3`, `Ignore Elevation`, `Intervention`, and
  `Equip Knight Swords`;
- T4 accuracy/evasion rows for `Concentration`, `Parry`, `Arrow Guard`, `Shirahadori`, `Vanish`,
  `Reflexes`, and `Equip Shield`;
- T6xPS mitigation rows using the strongest-single mitigation channel;
- F5 real-roster rows after Vanguard and Ramza are promoted into the sim bundle as real job rows;
- W9 ordinary-progression, optimizer-progression, and grind-heavy rows under the fixed JP model.

## Claude Review Request

Claude should review:

- whether the bundle-pinned constants are used consistently;
- whether every formula-affecting piece has an isolated row or an explicit reason for no row;
- whether the convergence stacks are enough to expose W5;
- whether the defensive channel prevents practical immunity;
- whether `Intercede` and `Intervention` are clearly disambiguated;
- whether movement ranking satisfies I6;
- whether Ramza having no unique exportable R/S/M is the right default for this pass;
- whether any listed piece should be cut, delayed, or split before acceptance.
