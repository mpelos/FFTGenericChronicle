# Formula Variable Palette

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/formula-balance/00-envelope.md`
- `docs/formula-balance/01-principles.md`
- `docs/modding/05-battle-data-map.md`
- `work/battle_data_inventory.md`
Review: Approved by Claude on 2026-06-20 after adding the Accuracy / Hit Rate domain.

## Purpose

This document maps FFT's combat variables as a design palette before any concrete formula is
chosen.

The goal is not to preserve current FFT formulas for their own sake. The goal is to understand
every useful lever the game exposes, then use those levers creatively to make combat and weapon
families better while preserving FFT feel.

Tier labels in this document are cost and proof labels, not creative vetoes. If a Tier-2 idea is
the right design, it remains available; it simply carries higher implementation and validation
cost.

## Design Role Labels

Each variable has a design role:

- `primary lever`: can carry visible family/formula identity.
- `secondary modifier`: can shape formulas, but should not usually be the main identity alone.
- `background`: useful for tuning, progression, or context, but not ideal as a visible formula
  hook.
- `avoid-for-legibility`: technically interesting, but likely too opaque or unstable for player
  readability unless a later document gives a strong reason.

## Primary Unit Stats

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PA | Runtime unit struct; JobData growth/multiplier; equipment bonuses | Core physical attack stat for most weapon and skill formulas | Main physical power lever; can distinguish heavy, martial, and weapon-skill families | WP, Brave, Attack Boost, Berserk, Protect, job multipliers | Mixed | local data for job/equipment; live struct needs confirmation | primary lever |
| MA | Runtime unit struct; JobData growth/multiplier; equipment bonuses | Core magic attack stat; staff/pole and many spell formulas | Main magic and hybrid weapon lever; can support rods/staves/poles and magic-adjacent weapon families | Faith, spell formulas, elements, rods/staves/poles, Shell | Mixed | local data for job/equipment; live struct needs confirmation | primary lever |
| Speed | Runtime unit struct; JobData growth/multiplier; equipment bonuses | Turn frequency and some weapon formulas such as knives/longbows | Agile weapon scaling, action economy pressure, accuracy-like feel without new accuracy stat | CT, knives, longbows, movement tactics | Mixed | local data for job/equipment; live struct needs confirmation | primary lever |
| Brave | Runtime unit struct; unit state | Used by fists, knight swords, katanas, reaction chance, and some classic mechanics | Commitment/risk/discipline lever for martial, katana, knight sword, and bravery-themed skills | Faith contrast, reactions, Brave manipulation, Chicken risk | Mixed | external struct offsets need live confirmation | secondary modifier |
| Faith | Runtime unit struct; unit state | Scales many magic effects and hit rates; anti-synergy for Atheist | Main magical susceptibility/potency lever; useful for magic balance but risky for physical weapon readability | MA, Cure/damage magic, status, Atheist/Faith statuses | Mixed | external struct offsets need live confirmation | secondary modifier |
| Level | Runtime unit struct; ENTD; unit progression | Used by some math/level effects and overall progression | Can create level-relative formulas, but risks scaling opacity and grind incentives | Math skill, enemy scaling, ENTD levels | Mixed | ENTD local; live struct needs confirmation | background |
| HP / MaxHP | Runtime unit struct; armor HP bonuses; SpawnData | Survival pool; some formulas use current/missing HP | Good for risk/reward, sacrifice, tank identity, and defensive tuning | armor, Wish/Shock-style formulas, healing, damage scale | Mixed | local armor data; live struct needs confirmation | secondary modifier |
| MP / MaxMP | Runtime unit struct; armor MP bonuses; SpawnData | Spell resource; some drain/damage formulas use MP | Good for caster weapon identity, spell economy, MP shield/drain concepts | spells, ethers, MP damage/drain, robes | Mixed | local armor data; live struct needs confirmation | secondary modifier |

## Raw Stats And Growth

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Job growth constants | JobData XML; level-up math | Long-term stat development | Job rebalance, not first-pass formula identity | job multipliers, level, permanent growth | Tier-1 for data; Tier-2 if formula reads raw values | verified in local files for JobData | background |
| Job stat multipliers | JobData XML | Defines displayed HP/MP/PA/MA/Speed by job | Major job-balance lever; can make weapon families job-sensitive indirectly | PA, MA, Speed, HP, MP | Tier-1 | verified in local files | secondary modifier |
| Raw/stat-internal values | Runtime struct / classic model | Underlying stat derivation, not player-facing | Could power advanced formulas but is opaque to players | growth, multipliers, level | Tier-2 | needs reverse engineering/live confirmation | avoid-for-legibility |

## Weapon And Base Combat Variables

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| WP / Power | ItemWeaponData.xml | Main weapon magnitude value | Core weapon tuning lever; can adjust magnitude without changing formula shape | PA, MA, Speed, Brave, weapon routine | Tier-1 | schema verified; values need data capture on game machine | primary lever |
| Weapon routine / Formula | ItemWeaponData.xml; hardcoded routines | Chooses weapon damage computation such as PA*WP, MA*WP, WP*WP, random, etc. | Meta-variable that may define family scaling shape if reassignable | weapon type, R1 gate, flags, animations, side logic | Mixed | needs data capture + in-game proof patch | primary lever |
| Weapon type / family | Job equippable vocab; ItemWeaponData / item category | Determines equipment access and likely routine grouping | Main taxonomy axis for the mod | jobs, skillsets, support abilities, routing | Mixed | needs weapon baseline on game machine | primary lever |
| Weapon evade | ItemWeaponData.xml; shields/accessories also have evasion | Defensive value on equipment | Can make weapon choice defensive, not just offensive | shield evasion, class evasion, facing, evade flags | Tier-1 with engine rules | schema verified; behavior needs playtest | secondary modifier |
| Attack flags | ItemWeaponData.xml | Flags such as Striking, Lunging, Direct, Arc, Throwable, TwoHands, ForcedTwoHands, TwoSwords | Strong family identity and dominance risk lever | Two Hands, Dual Wield, range/arc, weapon animation/side logic | Tier-1 with engine caveats | schema verified; behavior needs data capture/playtest | primary lever |
| Critical hits | Hardcoded battle logic | Occasional damage spike | Can support high-variance families if accessible; risky if opaque | weapon routine, Brave?, status, variance | Tier-2 unless exposed by data | needs reverse engineering | secondary modifier |
| Random damage / variance | Formula routines; some item/ability UI flags | Creates volatility for axes/flails/bags and random skills | Useful for risk/reward identities if floor/ceiling are tunable | PA, WP, random formula ids, player trust | Mixed | formula known from external sources; needs playtest | secondary modifier |
| Ability X/Y | OverrideAbilityActionData | Two byte parameters feeding hardcoded ability formulas | Huge Tier-1 lever for skill and family-flavored abilities | formula id, element, status, CT, MP, range | Tier-1 | schema verified; data pipeline needs in-game proof | primary lever |
| Ability formula id | OverrideAbilityActionData; hardcoded catalog | Selects ability routine 0x00-0x64 | Primary skill identity lever; richer than weapon base routines | X/Y, flags, slot-hardcoded behavior | Tier-1 with side-logic caveats | schema verified; behavior needs proof patch | primary lever |

## Accuracy / Hit Rate

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Physical hit formula | Hardcoded battle logic; classic AttackEvadeCalc / CalcHitPercent map | Resolves physical hit chance against evasion and facing | Can distinguish reliable weapons from volatile weapons, but changing the math is high-risk | weapon evade, shield/class/accessory evasion, facing, status, attack flags | Tier-2 for math changes; Tier-1 only through exposed flags/evasion data | needs reverse engineering/live confirmation | secondary modifier |
| Magic hit formula | Hardcoded battle logic; Faith-scaled status/magic formulas | Resolves many magical hit/effect chances | Important for weapon/magic coexistence and status balance | Faith, MA, Shell?, status immunity, formula id | Mixed | source-derived; needs playtest/live confirmation | secondary modifier |
| Evasion bypass / Direct flags | ItemWeaponData attack flags; ability flags/routines | Makes some attacks ignore or alter evasion checks | Strong reliability lever and major dominance risk if overused | weapon family, shields, evasion, counters, status | Tier-1 with side-logic caveats | schema verified; behavior needs proof/playtest | primary lever |
| Weapon-family accuracy data | Unknown until weapon baseline is captured | May or may not exist as explicit per-weapon accuracy | If present, can separate guns/crossbows/knives/axes by reliability without changing damage | weapon type, attack flags, evasion checks | Tier-1 if data exists, otherwise Tier-2 | needs data capture on game machine | primary lever if present; otherwise unavailable |
| Status accuracy / success | Ability formulas and AI flags; Faith/MA routines | Controls whether debuffs and special effects land | Lets weapon skills trade damage for reliable or risky utility | InflictStatus, Faith, MA, immunity, CT/MP | Mixed | formula-specific; needs proof/playtest | secondary modifier |

## Position And Time

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Range | OverrideAbilityActionData; ItemWeaponData; AbilityJumpData | Target reach | Major readability lever for weapon and skill identity | height, line/arc flags, job movement, counters | Tier-1 | schema verified; behavior needs proof/playtest | primary lever |
| Vertical / height tolerance | OverrideAbilityActionData; AbilityJumpData; map state | Controls target elevation tolerance | Distinguishes polearms, bows, jumps, and terrain tactics | Jump, range, map geometry | Mixed | schema verified; map behavior needs playtest | secondary modifier |
| Map height | Runtime/map state; math skills | Tactical positioning and some formulas | Good for terrain-aware formulas but can be opaque if overused | bows, jump, geomancy, math skills | Mixed | needs runtime/map confirmation | secondary modifier |
| Facing / direction | Runtime unit struct | Affects evasion and tactics | Powerful tactical lever, but too opaque for main damage scaling unless very clear | shield/weapon evade, back attacks, movement | Tier-2 for custom formulas | external struct offsets need confirmation | secondary modifier |
| Move | JobData; equipment bonuses; runtime stat | Tactical mobility | Family/job support lever rather than damage formula core | range, positioning, melee vs ranged | Tier-1 for data; Tier-2 for formula use | local data verified; live struct needs confirmation | background |
| Jump | JobData; equipment bonuses; runtime stat | Vertical mobility and Jump command logic | Can support polearm/jump identity; risky as direct damage stat | Jump formula, map height, vertical | Mixed | local data verified; live behavior needs proof | secondary modifier |
| CT / Charge time | OverrideAbilityActionData; battle clock | Action delay and spell/skill timing | Major balance lever for powerful actions; preserves FFT feel if used clearly | Speed, spell tiers, charge/aim, turn economy | Tier-1 | schema verified; behavior needs proof patch | primary lever |
| AoE / Effect area | OverrideAbilityActionData | Area targeting | Major skill identity lever, especially magic and weapon techniques | range, CT, friendly fire/targeting | Tier-1 | schema verified; behavior needs proof patch | primary lever |

## Mitigation And Survivability

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Class evasion | JobData | Base evasion | Defensive job identity and matchup tuning | weapon/shield/accessory evasion, facing | Tier-1 | verified in local files; behavior needs playtest | secondary modifier |
| Shield evasion | ItemShieldData.xml | Physical/magical evasion | Strong equipment identity; can support defensive builds | facing, attack flags, shield access | Tier-1 | schema verified; values need data capture | primary lever |
| Accessory evasion | ItemAccessoryData.xml | Physical/magical evasion | Specialist defense lever | shield/class evasion, accessory slot opportunity cost | Tier-1 | schema verified; values need data capture | secondary modifier |
| Armor HP/MP bonus | ItemArmorData.xml | Flat survivability/resources | Main armor tuning lever if no point-DR exists | damage scale, healing, MP economy | Tier-1 | schema verified; values need data capture | primary lever |
| Equip stat bonuses | ItemEquipBonusData.xml | PA/MA/Speed/Move/Jump/status/element bonuses | Broad equipment identity lever; can make non-weapon slots matter | weapon scaling, jobs, status, elements | Tier-1 | schema verified; values need data capture | primary lever |
| Protect / Shell | StatusEffectData + hardcoded formula modifiers | Physical/magical mitigation | Clear FFT-native mitigation lever; can replace need for point-DR in many designs | status, magic, support, CT/cost | Mixed | status data verified; exact ratios source-derived/needs playtest | secondary modifier |
| Point armor DR | Not proven as native data lever | Not known as FFT baseline | Attractive GURPS-like concept but not assumed | armor, penetration, damage types | Tier-2 unless mapped to data | needs reverse engineering/proof | primary lever if implemented; otherwise unavailable |
| Armor penetration | Not proven as native data lever | Not known as FFT baseline | Could differentiate thrust/pierce families if implemented | DR, weapon families, damage type | Tier-2 unless mapped to existing routine | needs reverse engineering/proof | primary lever if implemented; otherwise unavailable |

## Elemental Variables

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Element on ability/weapon | OverrideAbilityActionData; ItemWeaponData | Assigns elemental type | Clear family/flavor lever for weapons and magic | weak/half/absorb/nullify/strengthen, equipment | Tier-1 | schema verified; values need data capture/proof | secondary modifier |
| Weak elements | JobData; ItemEquipBonusData | Increases incoming element impact | Matchup design; can make armor/accessories meaningful | enemy design, weapons, spells | Tier-1 | local vocab verified; behavior needs playtest | secondary modifier |
| Halve/nullify/absorb elements | JobData; ItemEquipBonusData | Elemental defense | Strong equipment identity and encounter design lever | spell/weapon element, accessories | Tier-1 | local vocab verified; behavior needs playtest | secondary modifier |
| Strengthen elements | ItemEquipBonusData | Boosts outgoing element | Build identity for elemental weapons/spells | rods, spells, elemental gear | Tier-1 | local vocab verified; behavior needs playtest | secondary modifier |
| Weather / terrain modifiers | Hardcoded; classic BowWeatherCalc / terrain systems | Contextual modifiers in classic FFT | Can add tactical richness, but risks opacity if made central | bows, maps, elements | Tier-2 or existing hardcoded only | needs reverse engineering/playtest | background |

## Status Variables

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| InflictStatus | OverrideAbilityActionData | Status applied by an action | Strong skill identity lever when accuracy/cost/CT are balanced | status table, immunity, AI flags, Faith | Tier-1 | schema verified; behavior needs proof patch | primary lever |
| Innate/immune/starting status | JobData; ItemEquipBonusData | Job/equipment status properties | Strong build/equipment identity lever | status effects, armor/accessory roles | Tier-1 | vocab verified; values need data capture/playtest | primary lever |
| Status duration/counter | StatusEffectData.xml | Duration and ticking behavior | Global status balance lever; high blast radius | CT, poison/regen, disable, stop, haste/slow | Tier-1 with caution | schema verified; behavior needs playtest | secondary modifier |
| Status check/cancel/no-stack flags | StatusEffectData.xml | Defines status interactions | Can improve combat clarity but has high systemic risk | all status interactions | Tier-1 with caution | schema verified; behavior needs playtest | background |
| Full 40-status vocabulary | StatusEffectData / JobData vocab | Available state space | Palette for identity, but not all statuses should carry weapon damage identity | immunity, AI, UI clarity | Tier-1/Tier-2 mixed | vocab verified; individual behavior needs playtest | secondary modifier |

## Zodiac And Compatibility

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Zodiac sign | Unit data/runtime | Compatibility modifier | Preserves FFT flavor but is hard for players to plan around | damage/hit modifiers, story units | Mixed | needs live/source confirmation | background |
| Compatibility table | Hardcoded / CharaZodiacStoneCLUT | Good/bad/best/worst modifiers | Fine-tuning or optional high-skill depth; poor primary identity lever | all affected formulas | Tier-2 or data if CLUT usable | needs data/reverse engineering proof | avoid-for-legibility |

## Equipment And Slots

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Weapon slot | ENTD/runtime/equipment data | Main offensive equipment | Central identity axis | weapon family, job access, support abilities | Mixed | needs weapon baseline/live proof | primary lever |
| Shield slot | ENTD/runtime/equipment data | Defensive equipment or offhand tradeoff | Important tradeoff against dual wield/two-handed weapons | shield evasion, Two Swords, Two Hands | Tier-1/Mixed | values need data capture/playtest | primary lever |
| Head/body/accessory slots | ItemData + armor/accessory/equip bonus data | Survivability and bonuses | Can make armor/accessories strategic, not just bigger HP | HP/MP, stats, status, elements | Tier-1 | schemas verified; values need data capture | primary lever |
| EquippableItems | JobData.xml | Job equipment access | Major build-shaping lever | weapon families, armor types, role identity | Tier-1 | verified in local files | primary lever |
| Item options / option abilities | ItemOptionsData / ItemWeaponData | Weapon/item special effects | Can differentiate families if behavior is understood | proc behavior, formula 0x02, status/effects | Tier-1 with side-logic caveats | schema verified; behavior needs proof | secondary modifier |

## Job And Ability Systems

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Job command / skillset | JobData; JobCommandData | Defines class action list | Primary identity lever; can make weapon families shine through skills | ability formulas, job access, JP | Tier-1 | schema verified; design needs playtest | primary lever |
| Reaction/support/movement slots | JobCommandData; ability data | Build customization | First-order dominance risk and identity lever | Two Hands, Dual Wield, Attack Boost, Martial Arts, Move/Jump | Tier-1 with behavior caveats | schema verified; behavior needs playtest | primary lever |
| Innate abilities | JobData | Built-in job traits | Strong job/family support lever | weapons, statuses, movement, stats | Tier-1 | verified in local files | primary lever |
| JP and learn metadata | AbilityData / Ability NXD | Progression economy | Progression pacing and access tuning; not formula core | job balance, ability access | Tier-1 | local data verified for ability text/JP | background |
| AI behavior flags | AbilityData.xml | AI usability and targeting | Required for enemy use and encounter balance | skills, status, targeting | Tier-1 | vocab verified; behavior needs playtest | secondary modifier |

## Transversal Dominance Risks

These are not just variables; they are multipliers on the whole balance system.

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Two Hands | Support/attack flags/hardcoded behavior | Amplifies weapon damage for eligible setups | Can define heavy-weapon identity; also a major collapse risk | WP, PA, knight swords, shields | Mixed | behavior needs playtest/proof | primary lever |
| Dual Wield / Two Swords | Support/attack flags/hardcoded behavior | Doubles attack opportunities | Can define speed/knife/ninja identity; major dominance risk | offhand, weapon family, on-hit effects | Mixed | behavior needs playtest/proof | primary lever |
| Attack Boost / Martial Arts / similar support | Support abilities/hardcoded modifiers | Multiplies or reshapes damage | Strong identity and dominance lever | PA, fists, weapons, job access | Mixed | source-derived; needs playtest | primary lever |
| Defense Boost / defensive supports | Support abilities/hardcoded modifiers | Reduces incoming pressure | Defensive build identity | armor, Protect/Shell, HP | Mixed | source-derived; needs playtest | secondary modifier |

## Meta-Variables

| Variable | Where It Lives | Current FFT Role | Design Potential | Interactions | Dependency | Proof State | Design Role |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Ability routine selection | OverrideAbilityActionData.Formula | Chooses hardcoded action routine | The richest Tier-1 formula palette currently known | X/Y, status, element, CT, MP, slot side logic | Tier-1 with caveats | schema verified; behavior needs proof patch | primary lever |
| Weapon routine selection | ItemWeaponData.Formula | Chooses or appears to choose weapon formula | Decisive for family scaling diversity if reassignable | R1, weapon type, flags, hardcoded side logic | Mixed | needs data capture + proof patch | primary lever |
| Formula dispatch internals | FFT_enhanced.exe | Executes hardcoded formulas | Full custom formula system if hooked | runtime struct, damage store, hook safety | Tier-2 | needs reverse engineering | primary lever |
| Rounding / truncation regime | FFT_enhanced.exe formula internals | Floors/truncates intermediate values and final results | Hidden global parameter that can materially change balance outcomes | every formula, variance, multipliers | Tier-2 / research | needs reverse engineering and playtest | background |
| Data merge pipeline | Reloaded-II mod loader / FF16Tools outputs | Applies modded tables and NXD | Enables all Tier-1 work | deploy, modded.pac, proof patch | Tier-1 process | needs in-game proof | background |

## Design Guidance From The Palette

1. Use player-legible primary levers first: PA, MA, Speed, WP, weapon routine, ability formula,
   accuracy/evasion flags, range, CT, AoE, elements, status, equipment slots, and job access.
2. Use secondary modifiers to create texture: Brave, Faith, evasion, height, Move/Jump, Protect,
   Shell, elemental resistances, and support abilities.
3. Keep background variables mostly for progression and tuning: growth constants, JP, raw stats,
   zodiac fine print, data merge mechanics.
4. Avoid making opaque variables the visible center of a weapon family unless a later approved
   design explains why the payoff is worth the readability cost.
5. Treat Tier-2 as a valid creative path, but every Tier-2-heavy identity needs a proof plan and a
   Tier-1 fallback or an explicit acceptance that the family depends on engine work.

## What This Changes In The Workflow

The next accepted design step should not be a formula. It should be a revised family taxonomy
that uses this palette to decide which variables each family is allowed to emphasize.

Concrete formulas remain blocked until:

- this palette is approved;
- the family taxonomy is updated against this palette;
- the weapon baseline is captured or its absence is explicitly accepted as a risk;
- the relevant Tier-1 or Tier-2 proof requirements are named.
