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
| `0x12` | `Hit_F(MA+X)%` grant immediate turn (Set_Quick) | Quick (Time Magick) |
| `0x14` | Activate the native team-indexed Golem mitigation pool | Golem |
| `0x1A` | Stat reduction hit using `MA + Y` | Ruin/Drain stat skills |
| `0x1E` | MA result formula used by RandomFire actions | Truth-like repeated targeting |
| `0x1F` | Faith-inverted MA result formula used by RandomFire actions | Untruth-like repeated targeting |
| `0x20` | `MA * Y` non-Faith damage | Iaido/Draw Out style |
| `0x24` | `((PA + Y) / 2) * MA` style | Geomancy style |
| `0x25` | Equipment break success | Break skills |
| `0x26` | Equipment steal success | Steal skills |
| `0x27` | Gil steal success and transfer | Steal Gil / Plunder Gil |
| `0x28` | EXP steal success and transfer | Steal EXP / Plunder EXP |
| `0x2A` | Talk skill Brave/Faith/status behavior | Speech/Talk skills |
| `0x2D` | `PA * (WP + Y)` plus status | Holy Sword / swordskills |
| `0x2E` | Equipment break plus `PA * WP` damage | Shellbust-like skills |
| `0x2F` | MP drain using `PA * WP` | Dark Sword style |
| `0x30` | HP drain using `PA * WP` | Night Sword style |
| `0x31` | `((PA + Y) / 2) * PA` | Punch Art damage |
| `0x32` | One staged PA debit multiplied by `Rdm(1..X)` | Repeating Fist style aggregate damage |
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
| `0x51` | `Hit_(MA+X)%` apply InflictStatus entry (cancel sets work) | Choco Esuna style |
| `0x52` | Split Self-Destruct: missing-HP victim damage, current-HP caster debit, victim-only rider | Self-Destruct |
| `0x53` | Percent damage plus MA hit | Hurricane style |
| `0x58` | Generic-unit transformation transaction | Malboro Spores / Moldball Virus |
| `0x5A` | Dragon Check + `Hit(100)%` apply InflictStatus | Dragon's Charm |
| `0x5B` | Dragon Check + paired target HP credit/source HP debit + status-cancel rider | Dragon's Gift |
| `0x5C` | Dragon Check + Scream body (= `0x3B` on target) | Dragon's Might |
| `0x5D` | Dragon Check + Set_Quick | Dragon's Speed |
| `0x5E` | MA result variant used by a RandomFire action | Hydra style repeated targeting |
| `0x5F` | MA single-hit variant with RandomFire clear | Nanoflare style |
| `0x60` | Unevadeable MA multi-hit variant | Special monster formulas |
| `0x63` | `Sp * WP` | Throw |
| `0x64` | Jump formula | Jump |
| `0x65` | 80% staged damage followed by native HP-drain pairing | Sanguine Sword / Chant slot |
| `0x66` | 80% staged damage followed by native MP-drain pairing | Infernal Strike |
| `0x67` | Direct wrapper alias of formula `0x2D` | Crushing Blow |
| `0x68` | Distance-sensitive sword damage/status pipeline | Abyssal Blade |
| `0x69` | Non-Faith MA damage with MaxHP-derived power plus status | Unholy Sacrifice |
| `0x6A` | Dynamic equipped-weapon formula dispatch plus normal-attack postprocess | Barrage |

The `0x65` and `0x66` handlers first compute an ordinary staged debit, replace it with
`trunc(debit * 8 / 10)`, and call the same HP- or MP-transfer helper used by the established drain
families. The paired drain record fields, Undead reversal, result cap, and native HP/MP application
are defined in `04-engine-memory-model.md`. Formula `0x67` jumps directly to the `0x2D` handler.

Formula `0x69` constructs the non-Faith formula-`0x4E` power term as:

```text
Y = PA + floor((3 * MaxHP) / (10 * MA))
damage = MA * Y
```

Its status rider remains a separate authored status-contest decision. Formula `0x6A` reads the
weapon formula selected from the exact type-`1` order payload, dispatches through the ordinary formula table, and then enters
the native normal-attack postprocessor. Barrage's action metadata selects `WeaponRange` and
`NormalAttack`; its animation id is `0` and its effect id is `-1`, so the editable animation/effect
tables do not contain a Barrage-specific repeat sequence. **Strong:** the shared native initializer
recognizes formula `0x6A` and writes fixed repeat count `4` at RVA `0x7B0762`. The result producer
increments the shared index at RVA `0x7B0763` and publishes continuation after each result. Because
Barrage clears `RandomFire`, the dedicated random-tile selector is not dispatched; its original
single target remains selected while each repeated calculation delegates to the equipped weapon.

Formulas `0x1E`, `0x5E`, and `0x5F` share the same current-build MA result handler. Repetition is
therefore not an intrinsic property of that handler: the ability action's `RandomFire` flag is the
outer targeting/cadence carrier. Exactly 16 catalog actions set it: ids `169..180`, `255`, and
`342..344`; all use formula `0x1E`, `0x1F`, or `0x5E`. Celestial Void, Corporeal Void, and Dark
Whisper additionally carry hostile status riders. Nanoflare clears `RandomFire` and produces a
single hit. **Strong:** the flag consumer dispatches a selector that marks exactly one eligible tile
for each native repeat, then the ordinary target calculation runs once for that selected tile.
Repeat count and the incremented repeat index share RVAs `0x7B0762` and `0x7B0763` with Barrage. Formulas
`0x1E/0x1F` choose 1..10 repeats with weights `5,5,10,10,20,20,10,10,5,5`; formula `0x5E` uses
`X+1`. The result producer publishes continuation after comparing the incremented index with the
count, so every repeat is a distinct target/calculation event rather than an aggregate result.
Formula `0x32` is also not a native
per-strike transaction: it computes `floor(random15 * X / 32768) + 1` and multiplies one staged HP
debit by that count. DCL per-strike contest and Guard spending require a managed carrier for Pummel.
Pummel's dedicated animation id `104` and effect id `96` are presentation data, not evidence of
separate native result applications.

Formula `0x14` stages activation bit `0x0001`; state application writes the activating unit's MaxHP
into one of four unsigned 16-bit team/faction pools at RVA `0x186B020`. Native interception marks an
incoming result through the sign bit of `result+0x12`, subtracts its staged HP debit from the pool
with saturation at zero, and skips the ordinary HP debit. The triggering hit does not spill residual
damage when it exhausts a smaller remainder. The packed pool participates in the native battle-state
import/export block.

Formula `0x58` is a native transformation, not an InflictStatus bundle. Its only stock member,
Malboro Spores, has no status payload. A successful eligible target stages bit `0x0002`; state apply
clears existing status/effect state and transfers control to the protected native transformation
continuation. Eligibility accepts generic character ids `0x80..0x82`, rejects units with the tested
special flags, and rejects job ids `0x82..0x84`.

Formulas `0x25` and `0x2E` share an exact-action equipment selector. It writes the selected item id
to `result+0x04`, its concrete slot mask to `result+0x1B`, and returns zero for a valid selection.
Native destruction is carried by `result+0x12` bit `0x0004`. Formula `0x2E` continues through its
ordinary physical damage chain after staging the bit. Formula `0x25` is a re-entrant exception: if
the requested equipment is absent, it temporarily writes Basic Attack `(order type=1, action=0)`,
calls the universal calculation entry, and restores the outer Rend order. Repointing a reused action
away from `0x25` removes both the permanent break and this nested calculation.

Formula `0x42` stages a paired recoil transaction. The target result record at
`qword[0x14186AF70]` receives `result+0x06 = PA*Y`; the caster/self result record at
`qword[0x14186AF60]` receives `result+0x06 = trunc((PA*Y)/X)`. Both records are activated and marked
with result flag `0x80`, so native state application performs both HP debits through its ordinary
clamp and KO lifecycle.

## Slot-Hardcoded Risk

Changing only the Formula byte may not be enough for some families. These behaviors can be tied
to ability slots, command types, or hardcoded side logic:

- Break target slot: weapon, helm, armor, shield.
- Steal target slot.
- Talk Brave/Faith interpretation.
- Song/Dance ticking.
- Dragon/monster special handling — the Dragonkin case is fully mapped, see "The Dragon
  Check" below.
- Item Z-values and item inventory behavior.
- Jump range/vertical learned-ability lookup.
- Charge/Aim special CT behavior.
- Knockback tied to specific routines.
- Weapon proc behavior via Formula `0x02`.

Treat these as "test before designing around it."

## The Dragon Check (species-gated formulas)

The only species gate in the formula catalog. Formulas `0x5A`–`0x5D` (Reis' Dragonkin
support abilities 251–254) are thin wrappers: run the **Dragon Check**, then fall through to
a generic routine that other formula ids expose without the check.

- **The check (Proven, PSX decomp + FFHacktics `Dragon_Check`):** force a miss unless the
  target's monster **graphic-set id** is 15 (Dragon family) or 16 (Hydra family). It tests the
  sprite family, not the job id. IVC's `JobData.MonsterGraphic` keeps the same vocabulary
  (1 Chocobo … 14 Behemoth, 15 Dragon, 16 Hydra), so the semantics carry over unchanged.
- **Location in IVC (Strong):** inside the Denuvo-virtualized formula code. A capstone sweep
  of the real `.xcode` region (< `0x610000`) finds no graphic/job range compare, so the check
  cannot be byte-patched from a code mod the way classic patches do it.
- **Bypass A — species widening, data-only (Proven):** the check's species input IS
  `JobData.MonsterGraphic` — patch it. Setting a monster job's `MonsterGraphic` to `15` via
  TableData `JobData.xml` makes the check pass for that species (live-confirmed with
  controls: patched Goblin passed while unpatched Skeleton and a human stayed blocked), and
  nothing else consumes the field — sprites render normally (IVC resolves them through a
  different path; in the PSX decomp the check is the unit Graphic's only gameplay reader).
  This keeps the vanilla formulas and their exact behavior, and keeps humans (graphic `0`)
  excluded. No real-code copy of the JobData monster block into the battle struct exists
  (scan negative), consistent with the boot-time table patch reaching ENTD-spawned units.
- **Bypass B — remove the gate entirely, re-point (Proven):** re-point each ability to the
  check-free formula whose body the wrapper calls. A Formula re-point through
  `OverrideAbilityActionData` is honored by the engine (live-confirmed: formula `0x3B` on
  ability 253 applied its effect to a Chocobo). The abilities then work on ANY unit,
  humans included, with small behavior deltas (Gift loses its status-cancel rider;
  Charm/Speed roll real hit formulas instead of flat 100%):

```text
251 Dragon's Charm  0x5A -> 0x33  Hit_(PA+X)% + InflictStatus   (set X≈100 to mimic Hit(100)%)
252 Dragon's Gift   0x5B -> 0x3C  Wish math, exact              (loses the status-cancel rider)
253 Dragon's Might  0x5C -> 0x3B  Scream on target, IDENTICAL   (X=5 Brave, Y=2 PA/MA/SP inherit)
254 Dragon's Speed  0x5D -> 0x12  Hit_F(MA+X)% Set_Quick        (Faith-scaled; vanilla was flat 100%)
```

  No data field exists to gate an arbitrary ability's targeting by species — the
  `MonsterGraphic` lever (Bypass A) only works because this check happens to read it.
- The three breath attacks (248–250, `0x4E`) and Holy Breath (255, `0x1E`) are not
  dragon-gated.
- Decomp cross-check: the low nibble of each `_N_<Name>` wrapper in the PSX decomp indexes
  its dispatch bank; walking the banks reproduces this catalog's id column exactly
  (`_2_Quick`=0x12, `_3_Stigma`=0x33, `_B_Scream`=0x3B, `_C_Wish`=0x3C, `_A_DragonTame`=0x5A …
  `_4_Jump`=0x64), which is what pins the wrapper→generic equivalences above.

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
engine's staged debit at the pre-clamp hook (`0x30A5D7`) before vanilla applies HP — delivering even
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
