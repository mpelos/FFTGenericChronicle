# Formula ID Catalog

Catalog of the FFT/WotL Formula ids that drive Ivalice Chronicles' action math. The game's data
surfaces use the same `Formula` concept as classic FFT/WotL; a Formula id selects which hardcoded
routine runs for an ability or weapon.

Editable surfaces:

- Per ability: `OverrideAbilityActionData.Formula`, plus `X` and `Y`.
- Per weapon: `ItemWeaponData.xml` `Formula` and `Power`.
- Formula internals: hardcoded in `FFT_enhanced.exe`.

## Core Notation

| Symbol | Meaning |
| --- | --- |
| `PA` | Physical Attack |
| `MA` | Magick Attack |
| `WP` | Weapon Power |
| `Sp` | Speed |
| `Br` | Brave |
| `Fa` | Faith |
| `X`, `Y` | Ability action constants |
| `F(...)` | Faith-scaled by caster/target Faith |
| `Rdm(a..b)` | Random integer range |

FFT truncates often. When testing, assume integer flooring at intermediate steps until proven
otherwise.

## Weapon Attack Basics

A standard weapon attack computes an attack value based on weapon type, then multiplies by
weapon power.

| Weapon type | Base computation |
| --- | --- |
| Bare hands | `[(PA * Br) / 100] * PA` |
| Knife / Ninja sword | `[(PA + Sp) / 2] * WP` |
| Sword / Rod / Spear / Crossbow | `PA * WP` |
| Knight sword / Katana | `[(PA * Br) / 100] * WP` |
| Staff / Pole | `MA * WP` |
| Flail / Axe / Bag | `Rdm(1..PA) * WP` |
| Longbow | `[(PA + Sp) / 2] * WP` |
| Physical gun | `WP * WP` |
| Instrument / Dictionary / Cloth | `[(PA + MA) / 2] * WP` |

Important modifiers include critical hits, elemental strengthen/weak/half/absorb, Attack
Boost, Martial Arts/Brawler, Berserk, Defense Boost, Protect, charging/sleep/frog/chicken
target states, and Zodiac compatibility.

## High-Value Formula IDs

| ID | Formula / behavior | Typical use |
| --- | --- | --- |
| `0x01` | Weapon damage | Attack and many weapon-like actions |
| `0x02` | Weapon damage with options/proc behavior | Weapons using `OptionsAbilityId` as ability |
| `0x03` | `WP * WP` style weapon damage | Guns / special weapon routines |
| `0x04` | Magic gun behavior | Magic guns |
| `0x06` | HP drain using weapon basis | Drain weapons |
| `0x07` | Healing using weapon basis | Heal weapons |
| `0x08` | Faith-scaled `MA * Y` damage | Fire/Ice/Thunder/Holy/Flare style spells |
| `0x09` | Percent damage plus Faith-scaled hit | Demi/Lich style effects |
| `0x0A` | Faith-scaled offensive status hit | Poison/Slow/Stop style spells |
| `0x0B` | Faith-scaled buff hit | Protect/Shell/Haste/Reflect style spells |
| `0x0C` | Faith-scaled `MA * Y` healing | Cure line |
| `0x0D` | Percent healing plus Faith-scaled hit | Raise style effects |
| `0x0E` | Percent damage plus status | Death style effects |
| `0x0F` | MP drain percent/hit | Spell absorb style effects |
| `0x10` | HP drain percent/hit | Life drain style effects |
| `0x1A` | Stat reduction hit using `MA + Y` | Ruin/Drain stat skills |
| `0x1E` | Multi-hit MA formula | Truth-like multi-hit |
| `0x1F` | Faith-inverted multi-hit MA formula | Untruth-like multi-hit |
| `0x20` | `MA * Y` non-Faith damage | Iaido/Draw Out style |
| `0x24` | `((PA + Y) / 2) * MA` style | Geomancy style |
| `0x25` | Equipment break success | Break skills |
| `0x26` | Equipment steal success | Steal skills |
| `0x2A` | Talk skill Brave/Faith/status behavior | Speech/Talk skills |
| `0x2D` | `PA * (WP + Y)` plus status | Holy Sword / swordskills |
| `0x2E` | Equipment break plus `PA * WP` damage | Shellbust-like skills |
| `0x2F` | MP drain using `PA * WP` | Dark Sword style |
| `0x30` | HP drain using `PA * WP` | Night Sword style |
| `0x31` | `((PA + Y) / 2) * PA` | Punch Art damage |
| `0x32` | Random multi-hit PA damage | Repeating Fist style |
| `0x33` | `PA + X` hit/status | Stigma Magic style |
| `0x34` | `PA * Y` HP and MP healing | Chakra style |
| `0x36` | Raise PA by `Y` | Accumulate/Focus style |
| `0x37` | `Rdm(1..Y) * PA` with knockback | Throw Stone style |
| `0x39` | Raise Speed | Tailwind/Yell style |
| `0x3A` | Raise Brave | Cheer Up style |
| `0x3B` | Raise Brave plus stats | Scream style |
| `0x3C` | Caster HP sacrifice healing | Wish/Energy style |
| `0x3E` | Target current HP minus 1 | Gravity style |
| `0x42` | `PA * Y`, caster recoil by divisor | Destroy/Compress style |
| `0x43` | Caster missing HP damage | Shock/Lifebreak style |
| `0x44` | Target current MP damage | MP damage effect |
| `0x45` | Target missing HP damage | Climhazzard style |
| `0x48` | Potion formula | Items |
| `0x49` | Ether formula | Items |
| `0x4A` | Elixir formula | Items |
| `0x4B` | Phoenix Down formula | Items |
| `0x4C` | `MA * Y` healing | Choco Cure style |
| `0x4E` | `MA * Y` non-Faith damage | Limit/monster style |
| `0x4F` | Goblin Punch style | Monster skill |
| `0x53` | Percent damage plus MA hit | Hurricane style |
| `0x5E` | Multi-hit MA variant | Hydra style |
| `0x5F` | MA single-hit variant | Nanoflare style |
| `0x60` | Unevadeable MA multi-hit variant | Special monster formulas |
| `0x63` | `Sp * WP` | Throw |
| `0x64` | Jump formula | Jump |

## Slot-Hardcoded Risk

Changing only the Formula byte may not be enough for some families. These behaviors can be tied
to ability slots, command types, or hardcoded side logic:

- Break target slot: weapon, helm, armor, shield.
- Steal target slot.
- Talk Brave/Faith interpretation.
- Song/Dance ticking.
- Dragon/monster special handling.
- Item Z-values and item inventory behavior.
- Jump range/vertical learned-ability lookup.
- Charge/Aim special CT behavior.
- Knockback tied to specific routines.
- Weapon proc behavior via Formula `0x02`.

Treat these as "test before designing around it."

## Data-First Design Guidance

Good early candidates for redesign:

- Change spell tiers through `Formula=0x08`, `Y`, `CT`, and `MPCost`.
- Make healing less Faith-dependent by moving selected heals from `0x0C` to a non-Faith
  healing formula if one tests cleanly.
- Rebuild swordskills by tuning `0x2D` `Y`.
- Rebuild monk-style damage with `0x31` or other PA formulas.
- Rebuild Iaido-style non-Faith magic damage with `0x20`.
- Use weapons as archetype anchors by changing `ItemWeaponData.xml` formula/power/elements.

Bad early candidates:

- New custom formula math.
- New global scaling resource.
- Removing Zodiac/Faith globally.
- Changing table sizes.
- Relying on obscure slot-hardcoded effects before validation.

## Variables We Can Use, And Whether We Can Write Free Math

Two tiers, this is the core design constraint.

### Tier 1 - data only (no exe): pick a formula, not write one

In `OverrideAbilityActionData` (and weapon `Formula`) we only choose **which existing routine**
runs and feed it parameters. We are NOT free to write arbitrary math here.

Per ability we set: `Formula` (the routine, 0x00-0x64), `X` (byte 0-255), `Y` (byte 0-255),
`Element`, `InflictStatus`, `Range`, `EffectArea`, `Vertical`, `CT`, `MPCost`. Just two numeric
parameters (`X`, `Y`).

The variables a routine can read are fixed by that routine. Across the catalog, the engine's
formulas already use: `PA`, `MA`, `Sp`, `WP`, `Br`, `Fa` (and faith-product `CFa*TFa/100^2`),
target/caster `CurHP/MaxHP/CurMP/MaxMP`, `Level` (via Math skill), `Height`/elevation,
`Distance`, and randomness `Rdm(1..n)`. But only in the **combinations a routine already
implements** - we cannot, in data, make "damage = PA^2 + 3*MA" unless a routine has that shape.

Constraints: integer math, truncates each step; `X`/`Y` are one byte each; multipliers like
Zodiac/Protect/Two-Hands/Martial-Arts and Knockback are bolted to specific routines/slots, not
portable by just swapping the `Formula` byte.

### Tier 2 - code mod (hook the exe): arbitrary math, any variable

To go beyond the catalog (new scaling, new variable mix, more than two parameters) a Reloaded-II
code mod computes the result in C# over the in-memory battle-stats struct (all effective stats,
Br/Fa, HP/MP cur+max, Level, Move/Jump, Zodiac, status, element, height, distance, turn/CT, RNG).
Limits: integer arithmetic, result clamped to the engine's damage width (16-bit), stat bytes. No
modding API exposes a damage hook — neither the loader managers nor Faith Framework — and the
damage routine itself is Denuvo-virtualized, so the code mod owns the final result by rewriting the
engine's staged debit at the pre-clamp hook (`0x30A66F`) before vanilla applies HP — delivering even
lethal damage in the same hit — with a post-damage reconciler as fallback, rather than by hooking the
virtualized routine directly. **Accuracy/avoidance is also Tier-2-controllable** (not a data field):
hit/miss/block/parry by writing the defender's live evade bytes (input-control) or repainting the
result selector (output-control); status, reactions (Brave), MP, and the full forecast display
(hit-%, HP amount number + HP-bar ghost — `obj+0x6`==`unit+0x1C4` for damage, `obj+0x8`==`unit+0x1C6`
for healing, coherent with the applied result) are likewise code-mod levers. The reverse-engineering picture is in `05-reverse-engineering.md`; the
code-mod runtime, formula DSL, and control levers are in `06-code-mod-runtime-dsl.md`.

**Plan for Generic Chronicle:** ~90% via Tier 1 (repoint formulas, retune X/Y/element/status,
rebuild jobs + weapons), plus a small set of Tier 2 custom formulas for signature mechanics the
catalog cannot express.

## Sources

- Local `OverrideAbilityActionData.layout`
- Local `ItemWeaponData.xml`
- FFHacktics formulas: https://ffhacktics.com/wiki/Formulas
- FFHacktics formula table: https://ffhacktics.com/wiki/Formula_Table
- FFHacktics formula hacking: https://ffhacktics.com/wiki/Formula_Hacking
- AeroStar Battle Mechanics Guide: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
