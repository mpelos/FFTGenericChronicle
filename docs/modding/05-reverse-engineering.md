# Reverse-Engineering Reference — Hooking the IVC Damage Engine

The single source of truth for the reverse-engineering picture of FFT: The Ivalice
Chronicles (IVC). It states what the engine is, what ports from classic FFT and what does
not, where the damage logic actually lives, which code anchors are stable and hookable, and
the live-validated control levers exposed by those anchors.

Cross-references: live struct offsets, the battle actor array, and the action-context /
attacker-resolution model are owned by `04-engine-memory-model.md`; the post-damage code-mod
reconciler and its formula DSL are owned by `06-code-mod-runtime-dsl.md`; per-formula-id
behavior is owned by `02-formula-id-catalog.md`.

## 1. Architectural truth

IVC is `FFT_enhanced.exe`, an x86-64 from-scratch re-implementation built on the FFXVI
"Faith" engine. The 1997 source was lost, so Square Enix rebuilt the game. The build
faithfully reproduces the WotL ruleset and the classic ability-action data model — the same
`Formula / X / Y / Element / CT / MP` per ability — running over a battle-unit struct whose
layout matches the classic one.

`FFT_classic.exe` is a sibling on the same Faith engine. It is NOT a PSX emulator and offers
no easier static-RE path; Enhanced is the better-mapped target. Both executables are
Denuvo-protected.

**Ports from classic/WotL FFT — design knowledge only:** exact formulas, data-struct layout,
and the formula-dispatch architecture. Because the math and the struct are known, RE is
guided rather than blind: the task is to find the function that computes *known math* over a
*known struct*. The named PSX Ghidra decomp (`Talcall/FFT-1997-Decomp`, 606 named `BATTLE_*`
functions — `BATTLE_calculator_routine(CurActionTargetData*)`, `CalcHitPercent`,
`AttackEvadeCalc`, `Calculate_Stat_Real`, `Calculate_Zodiac_Sign`, struct `BattleUnitData`)
is a conceptual function map. It is not matching/recompilable, and there is no decomp of WotL
or the remaster engine.

**Does NOT port — any machine code:** classic = PSX MIPS R3000 (`BATTLE.BIN`); WotL = PSP
MIPS. IVC is x86-64. No legacy MIPS routine, byte patch, or AOB carries over. Only the design
knowledge transfers.

## 2. Denuvo conclusion — the damage routine cannot be AOB-hooked

The damage routine is **Denuvo-virtualized** (translated into a private VM bytecode). Its
16-byte damage-apply prologue is absent from the static executable and, when momentarily
present at runtime, relocates to a different address every launch. An in-battle full-memory
scan over all executable-readable regions finds the x86 damage-apply pattern nowhere, while
nothing is execute-only (nothing hidden from reading). Therefore the routine is not native
x86 at all — it cannot be located by static signature scan and cannot be AOB-hooked.
Arbitrary custom damage math by hooking the formula dispatcher is blocked by Denuvo by design.

Consequences:
- Get damage numbers the Denuvo-proof way: read a unit's HP through a stable anchor and log
  HP deltas (an HP drop is damage taken).
- The "damage multiplier" / `[rax+0x06]` damage-store AOBs published in community cheat tables
  match transiently or coincidentally; they are not reliable hook targets on this build.
- A hardware breakpoint (VEH + DRx) on the HP write is robust to relocation but lands inside
  the Denuvo VM dispatch, where registers do not cleanly map to game state. Last resort only.

## 3. Stable hookable anchors

These are real-code instructions (`.text` / `.xcode`), ASLR-stable, RVA < 0x400000, image
base `0x140000000`, module `FFT_enhanced.exe`. They are non-virtualized and hookable with a
Reloaded-II `CreateAsmHook` / `CreateHook`. All values are Steam v1.0 (Oct 2025) Enhanced;
a game patch shifts them — re-locate via the neighboring stable bytes.

### `.text` anchors

```text
battle_base_ptr    0x226D98   movzx eax, word [rcx+0x30]   rcx = battle unit; reads +0x30 HP (word)
damage_mult_2      0x30A685   HP-bar / UI math (rdi = one unit; edx = MaxHP; eax = MaxHP-curHP) — NOT the formula
jp_multiplier      0x283754
xp_multiplier      0x283767
min_spd_jmp_mov    0x36027F   movzx eax, byte [rdi+0x42]   reads +0x42 Move (byte)
```

Byte signatures — the sig-scan keys to re-locate each `.text` anchor after a game patch shifts the
RVAs (`battle_base_ptr` is also given with its full hook context in `04-engine-memory-model.md` §4.1):

```text
battle_base_ptr   0F B7 41 30 66 89 42 0C          read-site +0x226D98; cheat-engine inject point +0x21305C
damage_mult_2     2B C8 8D 04 11
jp_multiplier     03 C2 8B CF 41 3B C0
xp_multiplier     0F B7 84 7B 1E 01 00 00
min_brave_faith   41 0F B6 5A 2B                   reads +0x2B Brave (byte)
min_spd_jmp_mov   0F B6 47 42 66 89 43 30          reads +0x42 Move (byte)
```

The community damage-store AOB `0F B7 47 30 2B C2 85 C0 41 0F 4E CE 8A D1 E8 F2` is a non-anchor: it
matches only transiently/coincidentally and is not a reliable hook target (see §2).

The matched instruction bytes statically confirm struct offsets: +0x30 HP (word), +0x2B Brave
(byte), +0x42 Move (byte). `damage_mult_2` is display code, not the damage formula — it sees
only one unit and no ability/attacker.

### `.xcode` anchors (outcome dispatch / apply / animation)

The roll *arithmetic* (hit%, evade, damage math) is virtualized, but the **outcome dispatch,
the result/animation selection, and the apply step run in normal hookable code**.

```text
unit/result RECORD array     0x1853CE0   stride 0x200, <=21 entries (runtime 0x141853CE0).
                             The per-unit battle struct IS this array.
result DISPATCHER            0x38A4FC    event-queue loop; DECISION branch at 0x38A6F1
                             (cmp edx,0x300), edx = (category<<8)|unitIdx:
                             0x300 = apply HP/MP/stat -> 0x30A51C
                             0x200 = status | 0x100 = turn-done | 0xFF00 = terminator | 0xE000 = init
APPLY path                   0x30A51C    newHP = clamp(HP + word[unit+0x1C6] - word[unit+0x1C4], 0, MaxHP)
                             reads staged dmg word[+0x1C4], heal word[+0x1C6]; consults NO hit-flag
pre-clamp staged-dmg HOOK    0x30A66F    0F BF 45 06 = movsx eax, word[rbp+6] (= word[unit+0x1C4]);
                             the damage-rewrite hook — controls hit-vs-zero damage
result/animation SELECTOR    0x205210    prologue 48 89 5C 24 08 48 89 6C 24 10;
                             r8 = actor, record = [r8+0x148], cl (arg) = evade-type; reads +0x1E5, +0x1C4
evade-type teardown copy      0x205B38   mov byte [rdi+0x1C0], r12b (real-code copy; the authoring
                             value is produced inside the VM roll 0x30FA34, not here)
combat-popup digit RENDER    0x266AE0    int->digit->glyph; value [rdi+0x344]; 3-digit split 0x2671BE;
                             glyph map 0x267350; popup value mirror 0x3740200
```

Virtualized neighbors (E9/E8 into `.edata`, not hookable, not needed): roll gate `0x30FA34`;
hit%/evade helpers `0x269760 0x2759F8 0x2B8F30 0x2740A0`; result handlers `0x38ABBC 0x38BBFC`;
staging helpers `0x30BC3C 0x30BCF8`; evade-input scratch `0x7832E6`. The category producer at
`0x30F0C4` walks records by the global phase dword `[0x186B044]`; its 0x300 (apply) gate is the
VM thunk `0x30FA34` (the roll lives there).

### Table read-site

The `OverrideAbilityActionData` read-site is at RVA `0xEEA6E50` (VA `0x14EEA6E50`,
"PC/Steam patch 1"), within the module. Each `>= 0` cell in that table
(`Flags12 Flags34 Range EffectArea Vertical Element Formula X Y InflictStatus CT MPCost`) is
cast to byte and patches the in-memory ability-action struct just before combat math runs —
the natural starting point for tracing toward the formula dispatcher, though the dispatcher
itself is virtualized downstream.

## 4. Live-validated evade-type enum

The evade-type byte at record `+0x1C0` (also passed in `cl` to the selector `0x205210`) is the
lever that selects hit vs. which evade animation. The full enum (**Proven**):

```text
0x00 = HIT                         (+1BB=02 +1BE=01 +1C4=dmg +1E5=0x80 ; damage applies)
0x01 = cloak / accessory evade     (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x02 = weapon parry (RH/LH guard)  (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x03 = shield parry / block (LH)   (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x04 = class evade ("Miss")        (+1BB=01 +1BE=00 +1C4=0   +1E5=00 ; +1C2=FF observed)
0x06 = plain miss (failed accuracy roll, e.g. Steal/Charm) (+1BB=01 +1BE=00 +1C4=0 +1E5=00)
0x0B = Blade Grasp (Brave%-reaction)  (+1BB=01 +1BE=00 +1C4=0 +1E5=00 ; live-observed via SELECTOR-PROBE)
```

0x05 / 0x07–0x0A are unobserved gaps, likely unused. Hit = 0x00
(damage applies); 0x01–0x06 and 0x0B are all no-damage outcomes (`+1C4 = 0`) that differ ONLY in
`+0x1C0`, the byte that selects the on-screen animation. Every evade shares `+1BE=00` and `+1C4=0`;
`+0x1E5` carries the action's effect-kind (`0x00` for a basic-attack evade, nonzero when an evaded
ability still carries an effect). Evadable physical abilities route through 0x01–0x04 like a basic
Attack; a failed *accuracy* roll (Steal / status hit%) routes through 0x06.

### Control recipe

**Proven live (2026-06-26): full hit↔miss authority against the engine's roll.** The result render
and the HP debit are TWO INDEPENDENT paths on different call trees, so authoring an outcome needs the
right write on EACH path:

```text
force-HIT(D):   pre-clamp 0x30A66F -> word[rbp+6]=D (=target+0x1C4) ; word[rbp+8]=0 (=+0x1C6)
                selector 0x205210  -> leave +0x1BE=01 / +0x1C0=0x00          => damage popup, HP -D
force-EVADE(t): pre-clamp 0x30A66F -> word[rbp+6]=0 ; word[rbp+8]=0           (no HP change)  AND
                selector 0x205210  -> +0x1BE=0 ; +0x1C0=t (0x04 class / 0x03 shield / 0x02 weapon
                                      / 0x01 cloak / 0x06 miss) + force saved cl=t  => evade anim, HP kept
```

The decisive proof: a guaranteed 100%-to-hit basic attack (Agrias→Ramza) was rendered as a
class-evade "Miss" AND dealt 0 damage (HP 567/567) — the pre-clamp forced the staged debit `184->0`
and the selector flipped `+0x1C0` `0x00(hit)->0x04(class-evade)`. Proof log
`work/battleprobe_log.hit-to-miss-v2-PASS.*.txt`; full analysis
`work/1782517714-miss-block-parry-control-definitive.md`.

**Both writes are required (this corrects the earlier note).** Writing only `+0x1C0`/`+0x1BE` at the
selector is render-complete but **damage-incomplete**: the selector render and the HP apply run on
different frames, so the staged debit survives and HP still drops. To make a downgraded hit deal no
damage you MUST also zero the debit at the pre-clamp. (The old "the engine still owns HP via the
`+0x1C4` debit" held only for a NATURAL evade, which already arrives with `+0x1C4=0`.)

**Miss → hit is impossible at the dispatcher** — a miss never stages damage (`+0x1C4=0`, no `0x300`
apply emitted; the accuracy gate `0x30FA34` is VM). Never try to promote a miss. Instead force the
engine to (near-)always-hit upstream, then DOWNGRADE the guaranteed hit to whatever the custom roll
wants — collapsing control to two quadrants (keep-hit, downgrade-hit), both inside the two proven hooks.

**Neutralize the engine's avoidance** — three layers, all upstream of our hooks; each must be killed or
a residual engine outcome pre-empts ours. (Layer C is now neutralized in MEMORY — write the defender's
evade bytes to `0` before the roll, proven above — so the DATA edits below are only needed for the
reaction layers A/B, or as a static fallback.)

- Layer A — Hamedo / First-Strike (Monk reaction): pre-empts the ENTIRE attack before any hook runs.
- Layer B — reaction avoidance (Blade Grasp, Arrow Guard, Catch, Reflect): rolls BEFORE the evade
  bytes; Brave%-triggered; **zeroing evade bytes does NOT stop it.**
- Layer C — the 4 equipment/class evade bytes (class `+0x4B`, shield `+0x4A`/`+0x4E`, weapon
  `+0x46`/`+0x47`).

Levers: strip the reaction slot / blank the reaction table (kills A+B); `Evadeable` flag off and/or
zero the evade bytes / Concentrate (kills C). Layer B is now ALSO controllable in MEMORY via the
defender's **Brave** (`+0x2B`) — ✅ proven live (see below) — so data-disable of A+B is a static
fallback, not the only path. (Hamedo/Blade Grasp survive zeroed *evade* bytes by canon, which is why
they need the Brave lever or a data-disable, not evade-zeroing.)

### Input-control — ✅ PROVEN LIVE 2026-06-27 (the cleaner primary path)

**Write the DEFENDER's evade bytes on its live battle struct BEFORE the VM avoidance roll, and the
Denuvo VM honors them.** Denuvo virtualizes CODE, not DATA — the unit structs the VM reads are normal
writable memory, so planting the inputs makes the native roll produce our outcome, with the engine
rendering everything (animation, 0 damage, forecast %) on its own. No data-gutting, no result-forging.

Proof (`work/input-control-evade-PROVEN.md`, log `work/battleprobe_log.evade-override-PASS*.txt`): a
poll wrote Ramza's class-evade `+0x4B` `0→0x64` on his live struct; the attack preview then showed
**0% hit** and Ramza **evaded** — `[SELECTOR-PROBE evadeType=0x04(class-evade) rec+1BE=00 rec+1C0=04
rec+1C4=0 hp=567/567]`, no pre-clamp fired. The VM read live memory, not a cached forecast copy.

Byte → outcome, set on the **defender** (`unitPtr + off`, values 0–100; evade applies front/side only):

```text
+0x4B          = high   -> class evade ("Miss")  evadeType 0x04
+0x46 / +0x47  = high   -> weapon parry          evadeType 0x02
+0x4A / +0x4E  = high   -> shield block          evadeType 0x03
all five       = 0      -> guaranteed HIT (neutralizes avoidance in MEMORY, no data edit)  0x00
```

Harness: the poller writes these every ~20 ms via `EvadeOverride*` (+ `EvadeOverrideSweepSlots`, which
sweeps the unit array so even untracked units — i.e. the actual defender — get boosted; the two earlier
"failures" were invalid because only the attacker/idle units were boosted, never the defender). Next
(engineering): per-action, formula-driven writes — identify the defender via the pending-action tracker,
compute the formula for that (attacker, defender) pair, write its evade before the roll. Damage value
stays on the proven pre-clamp.

**DEAD ENDS (both refuted live; do not retry):**
- Hook **`0x30F49C`** ("last real instr before the roll"): `rbx` is the **ATTACKER**, not the defender —
  the defender is never in a register here. (Also corrects the older `0x226EBC`/`0x226F39` anchor, a UI
  status-panel exporter with no real-code readers.)
- Hook **`0x30F4A7`** (roll-verdict `eax` override): a per-unit CT/turn eval, `eax` is always `0`, not
  the accuracy verdict. The avoidance roll, evade-source combine, and `+0x1C0` write all live inside the
  one VM call `0x30FA34`; there is no real-code verdict to flip.

**Reactions (Layers A/B)** are controllable by the SAME live-data mechanism — ✅ **proven offline +
confirmed live 2026-06-27**. The reaction trigger is a `roll(100, Brave)` (canonical FFT Brave%-gate),
in the real-code cluster `0x30BE86 / 0x30BEDC / 0x30BF32 / 0x30BF72`; write the defender's **Brave**
(`+0x2B`) before the roll to suppress the trigger. Live: Brave 10 → Blade Grasp suppressed, 3/3 hits.
⚠️ **Chicken floor — never write Brave < 10** (`0x30A9BD cmp [+0x2B],0x0A` flips the unit to panic/
chicken). No reaction-slot byte exists in the struct (reactions resolve via VM `0x2BB0D4` from the
skillset object), so DATA-disable (ENTD / JobCommand R/S/M) remains the static alternative. Shipping
knob: `BraveOverride*` (offline notes `work/reaction-input-control-offline.md`).

The forecast hit% display now follows the live evade bytes too (Ramza showed 0%), so writing evade is
also the lever for an honest custom-% preview.

## 5. Formula fingerprint constants

When tracing real (non-virtualized) code, routines are recognizable by their invariant
constants. Highest-signal first:

```text
1638400            stat-display divisor: DisplayedStat = RawStat * JobMult / 1638400 (near-unique)
10000 (/100,/100)  Faith term: MA * Q * CasterFaith/100 * TargetFaith/100
5/4 -> 4/3 -> 3/2 -> 3/2 -> 2/3 -> 2/3   physical XA modifier chain, truncating each step
                   (Strengthen, Atk-UP, Martial Arts, Berserk, Def-UP, Protect) — most distinctive
Zodiac  3/2, 5/4, 3/4, 1/2   gated on a 12x12 compatibility lookup table (applied once, after the XA chain)
(PA+Speed)/2       knife / longbow XA
WP*WP              gun XA            PA*PA*Brave/100   bare fists
MA*WP              staff / magic gun
2/3                Protect / Shell damage reduction
element            weak *2, half /2, absorb -> negate sign
Speed*WP           Throw            PA*WP (*3/2 if polearm)   Jump
XA + rand(1..XA) - 1                critical hit
8 + 2*JobLevel + Level/4            JP per action; share *1/4
EXP base 10, +/-1 per level diff, EXP-Boost *3/2
CT += Speed; act at CT>=100; reset 100/80/60; charged action wait = 100/Speed
```

Disambiguation: `5/4` and `3/4` appear in both the XA chain and Zodiac; Zodiac applies once,
after the XA chain, gated on the 12x12 table. The exact damage-variance variant and truncation
order used by the remaster's float math remain unverified in the binary.

## 6. In-battle unit struct (community-mapped, live-confirmed)

Base pointer = the unit (`rcx`/`rdi` at the hook sites). Live reads at `battle_base_ptr`
return sane values across every field. The authoritative live offset map is owned by
`04-engine-memory-model.md`; the core fields are:

```text
+0x00  id (byte)               +0x30  HP     (word)     +0x3E  PA   (byte)
+0x04  team/group id (byte)    +0x32  MaxHP  (word)     +0x3F  MA   (byte)
+0x05  friend/foe (bit 0x10)   +0x34  MP     (word)     +0x40  Speed(byte)
+0x28  EXP (byte)              +0x36  MaxMP  (word)     +0x42  Move (byte)
+0x29  Level (byte)            +0x2A  MaxBrave (byte)   +0x43  Jump (byte)
+0x2B  Brave (byte)            +0x2C  MaxFaith (byte)   +0x4F/0x50/0x51  X / Y / Dir (byte)
+0x2D  Faith (byte)
```

## 7. Modding-API feasibility — custom math needs a code mod

Arbitrary custom damage math is not available through any existing modding API; it requires a
Reloaded-II C# code mod.

- The loader's C# table managers (`IFFTOAbilityDataManager` and ~30 siblings, all
  `: IFFTOTableManager`) only do data-table patching: `ApplyTablePatch`, `GetOriginal*`,
  `Get*`, `ApplyPendingFileChanges`. It is a file/table replacement system, not a runtime hook.
  The `Ability` model surfaces only `JPCost`, `ChanceToLearn`, `Flags`, `AbilityType`,
  `AIBehaviorFlags`; the backing `ABILITY_COMMON_DATA` is 4 bytes. There is no
  event/delegate/hook for damage calc.
- `Formula / X / Y / Element` DO exist as columns in the Nex `OverrideAbilityActionData` table,
  and the loader merges `.nxd` cells — so a data-layer mod can repoint formula/X/Y by editing
  that file (the ~100 existing formula shapes). What is missing is a *code* API for it and any
  *damage-routine* hook.
- Faith Framework (`Nenkai/FaithFramework`, shared base for FFXVI + IVC) is a live Nex/NXD
  editor plus a debug-UI (ImGui) toolkit with a Nex Runtime Interface. It has no
  hook-registration API and no event system. Editing the *algorithm* needs Reloaded.Hooks, not
  Faith Framework.
- The mod loader already sig-scans and hooks `FFT_enhanced.exe`
  (`Hooks/FFTOResourceManagerHooks.cs`, via `IStartupScanner.AddMainModuleScan` + `CreateHook<T>`),
  proving the runtime-hook mechanism on this exact executable. Prior-art code mod FFTacticsFix
  (cipherxof) RE'd and hooked real presentation/engine functions here — but no shipped mod
  hooks the combat/damage routine, so a gameplay-logic code mod is first-of-its-kind for the
  engine.

Therefore custom damage math is achieved by the **post-damage runtime reconciler** owned by
`06-code-mod-runtime-dsl.md`: the data layer neuters vanilla damage into safe placeholders;
HP/MP deltas are observed through `battle_base_ptr`; the attacker is resolved from action
context; a C# formula computes the result; and the final HP/MP value is rewritten — all
without touching the virtualized formula routine. The data layer alone covers a full
job/skill/weapon/status/encounter redesign with no RE required.

### Vanilla baseline data

No public dump of IVC base `Formula/X/Y` exists; `OverrideAbilityActionData` is sparse `-1` and
exposes only overrides, not resolved base values. Use FFHacktics WotL `Ability_Data` as the
design baseline (8-byte entry: `0x07 Element, 0x08 Formula, 0x09 X, 0x0A Y, 0x0C CT, 0x0D MP`;
layout shared PSX -> WotL -> IVC) and apply IVC rebalances on top. Documented IVC-vs-WotL
rebalances (community RE, unofficial, possibly incomplete): enemies take ~30% less / allies
deal ~20% more damage (global tuning, hinting at a single multiplier near the end of the damage
routine); CT broadly reduced; assorted MP/JP tweaks (+30% JP from own actions); Arithmeticks
attack-spell damage reduced; Chemist innate Treasure Hunter; ribbons/perfumes no longer
gender-locked. Same WotL ability set, value rebalances only — no ID renumbering. Documented worked
examples (community RE, possibly incomplete): CT Protect/Shell 25->34, Bahamut 10->15, Graviga
12->10; MP Protectja 24->20, Lich 40->50; JP Teleport 600->3000, Meteor 1500->900.

## 8. Stable-touchpoint register classification

The stable `battle_base_ptr` hook is non-virtualized and fires with `rcx = battle unit`. A
read-only register snapshot at that touchpoint classifies each register against known battle
state. This is a clue-finding layer, not a formula hook, and does not prove action identity by
itself; the touchpoint is a UI/stat read, so registers usually show only the unit being read.
The signal is useful when a second unit pointer or a battle-context object appears consistently
around actions, giving a concrete next pointer to probe.

Register-classification vocabulary:

```text
unit:touched   register == the unit pointer that fired the hook
unit:id=...    register == another registered battle-unit pointer
readable       points at readable process memory, not yet identified
unreadable     VirtualQuery does not consider the address readable
zero           literal zero
```

A pointer-scan layer follows readable non-unit register roots and scans their first bytes for
exact known battle-unit pointers — if a register points at a battle controller / action-context
object, the scan reveals actor/target unit pointers inside it without mutating game state. A
stable engine context pointer found this way would supersede CT as the attacker source.

## 9. CT attacker resolution (diagnostic/fallback)

For immediate physical actions, the attacker can be resolved from CT evidence: `ct-reset` (a
non-target unit whose CT recently dropped) is the strongest signal; `ct-low` (the actor still
near its post-action CT value) is a necessary fallback when polling misses the reset frame. A
CT drop into the low band must refresh the observation timestamp, or poll-only drops are
excluded from `ct-low`. This proves out for immediate physical actions including dual wield.

CT is only a diagnostic/fallback signal. It is not a complete source of truth: charged spells
and delayed actions can land long after the caster's CT reset; Wait changes CT without
producing a damage action; reactions/counters need a separate inversion path; status/poison/
trap/reflect effects need separate handling. The full CT / action-context model and the layered
attacker-resolution architecture are owned by `04-engine-memory-model.md`.

## 10. Forecast hit-% display buffer (Layer 1 — visual control) — ✅ CONFIRMED LIVE 2026-06-27

The **displayed attack hit-%** (the number in the action forecast panel) is materialized in
ordinary memory and is fully read/write-able from outside the process — Denuvo virtualizes
*code*, not *data*. A Cheat-Engine-style differential scan (`work/mem_scan.py`: `find 3` →
`filter 77` → `filter 82`) collapsed 462,950 candidates → 15 → **4 addresses** that track the
on-screen %:

- `0x1407832C0` — **canonical static display buffer** (RVA `0x7832C0`), in the panel-exporter
  region; this is the value the renderer draws.
- three heap mirrors (`0x12DBAF3E0`, `0x12DCAF98A`, `0x436AC1C540`) — UI copies.

External `WriteProcessMemory` to all four **succeeds** (write sticks). But writing while the
panel is already drawn does **not** refresh the text: the UI is **retained-mode** — it draws
the number once and only redraws when the panel is *dirtied* (cursor moves on/off a target),
and dirtying **recomputes** the value, overwriting an external poke. So a naive external write
is racy (the engine wins on the next redraw). Confirmed live: one-shot write stuck in memory
while the cursor was static; the on-screen text stayed at the old value until a redraw.

**Data flow (real code, not VM):**

```
0x227FEA  mov   rbp, [rip+0x2DCBD07]   ; rbp = *(global forecast-object ptr)  @ VA 0x142FF3CF8
0x227FF1  test  rbp, rbp / je …        ; null-check
0x227FFA  movzx eax, word [rbp+0x2C]   ; AX = computed hit%  (source = object+0x2C)
0x227FFE  mov   r10d, 2
0x228004  mov   word [0x7832C0], ax    ; copy → display buffer  (renderer reads here)
```

Additional real-code writers of `0x7832C0` (a second copy path, from static mirrors):
`0x2C7F98`, `0x2C8C16`, `0x2C8E70`, `0x2C9806` (each `mov word [0x7832C0], ax`, inside a SIMD store
block around `0x7832B0`). All are RVA < `0x610000` → **hookable** (`work/disasm_hitpct.py`
enumerates them; the previous scan windowed `0x7832E6..` and missed `0x7832C0` just below it).

**Deterministic control (no race):** hook `0x227FFE` (`mov r10d, 2`, a clean non-RIP site
*between* the load and the store) with `AsmHookBehaviour.ExecuteFirst` and set `AX` to the
forced value before the engine's own store at `0x228004` runs. The game then writes **our**
value at copy time, *before* the renderer reads the buffer on the same redraw → the displayed
% is deterministically ours, no poke race. This is the engine-side analogue of the OUTPUT-paint
rule used for avoidance results. Mod feature: `PreviewHitPctControlEnabled` /
`PreviewHitPctForcedValue` / `PreviewHitPctLogOnly`, default RVA `0x227FFE`, expected bytes
`41 BA 02 00 00 00`. The hook records to a small buffer ([0]=fire count, [4]=last natural %,
[8]=forced, [12]=site RVA; addr printed at install as `[PREVIEW-HITPCT-HOOK] … buf=0x…`) so the
result is verifiable externally without the screen.

**Live proof (2026-06-27):** force value 7 deployed; an Agrias→Ramza preview whose true odds
were 3% (Blade Grasp) rendered **7%** on screen for every target. Memory cross-check
(`work/read_hitpct_hook.py`) at that moment: `fireCount=1`, `lastNatural=3`, display buffer
`0x7832C0 = 7`, natural source `object+0x2C = 3` — i.e. the engine computed 3, the hook painted
7 into the buffer the renderer reads, and the real source was left at 3 (the actual roll is
untouched). Both the screen and memory agree: the displayed forecast hit-% is fully ours.

**Purely visual.** The actual hit roll is computed independently inside the VM at execution
time; painting `0x7832C0` changes only the forecast number, not the outcome. This is DCL Layer 1
(show a custom hit-% from our own formula); the matching *outcome* control is sections 4/9.

## 11. Forecast HP preview + result — the unified `+0x1C4` / `+0x1C6` levers

The single most important forecast fact: **the "forecast object" is not a separate UI object — it
is `target_unit + 0x1BE`**. Its HP amount fields are:

- `obj+0x6 == unit+0x1C4`: staged HP-debit / damage.
- `obj+0x8 == unit+0x1C6`: staged HP-credit / healing.

For damage, `obj+0x6` drives **all three** of:

1. the forecast **damage NUMBER** (the red "500 Damage" in the panel),
2. the **HP-bar ghost-depletion** (how much of the target bar greys out), and
3. the **apply path** — it is the *same* staged debit `word[unit+0x1C4]` that APPLY (`0x30A51C`)
   reads and the pre-clamp hook (`0x30A66F`, §4) rewrites at resolution.

For healing, `obj+0x8` drives the forecast **healing NUMBER** (green `+N HP`) and the **HP-bar ghost
refill**. The same staged credit `word[unit+0x1C6]` is read by APPLY (`0x30A51C`) as
`newHP = clamp(HP + credit - debit, 0, MaxHP)`. A natural healing forecast uses `+0x1C4 = 0`,
`+0x1C6 = heal`, and `+0x1E5 = 0x40`. The ghost refill clamps at MaxHP.

The forecast object is reachable through three aliased globals that all hold the same pointer:
`0x142FF3CF8` (used by the number formatter and hit-% copy §10), `0x14186AF70`, `0x14186AF60`.
Companion fields on the same object: `obj+0x2C == unit+0x1EA` = the hit-% source (§10).

**Retained-mode draw (the key timing fact).** The engine computes the forecast amount once when the
preview opens and does not rewrite it per frame. The number and HP-bar ghost are drawn at open time;
a value written after that draw shows only on the next open. This applies to both damage
(`obj+0x6`) and healing (`obj+0x8`).

### Three levers for the preview HP amount (number + bar)

| Lever | What it touches | Timing | Coverage | Mod setting |
| --- | --- | --- | --- | --- |
| **Poke** (poll-write `obj+0x6` or `obj+0x8`) | source field → number **and** bar | shows on (re)open | **universal** (any action) | `PreviewForecastPoke*` |
| **Finalizer hooks** (force the `obj+0x6` store) | source field → number **and** bar | **first-open clean** (no reopen) | per-formula (whack-a-mole) | `PreviewForecastSource*` |
| **Number paint** (force the format dispatch) | display buffer `0x1407832BE` only | first-open clean | number only — **NOT the bar** | `PreviewDamage*` |

1. **Universal poke — the robust catch-all (PROVEN physical + magic).** Each poll, deref
   `[0x142FF3CF8]`; if it points cleanly into the unit table (base `0x141853CE0`, stride `0x200`,
   `obj = unit + 0x1BE`), write our value to the configured forecast field. Use
   `PreviewForecastDamageFieldOffset = 0x6` for damage/debit (`unit+0x1C4`) and `0x8` for
   healing/credit (`unit+0x1C6`). Works for **every** action type because it overwrites whatever any
   finalizer computed. Safe: at resolution the producer re-stages `+0x1C4/+0x1C6` before APPLY, so a
   preview poke never leaks into the real result (the pre-clamp owns the result). Caveat:
   retained-mode means it lands on the **next** open, not while held.

2. **Compute-time finalizer hooks — first-open clean, but per-formula.** Hook the instruction that
   *writes* `obj+0x6` during the forecast compute (`ExecuteFirst`, force the store register) so the
   number+bar are correct on the very first open. Different formulas use different writers — this is
   a whack-a-mole list, so the poke remains the guarantee:
   - `0x30637E` `66 41 89 50 06` `mov [r8+6],dx` — **MAGIC (Fire) — confirmed firing live**.
   - `0x308D8F` `66 89 41 06` `mov [rcx+6],ax`, `rcx = [0x14186AF70]` — Q15-scaled store, the
     **physical-attack candidate** (found via disasm; the 4-byte store is safe to hook — followed by
     a relocatable 5-byte `call`, no internal jump target).
   - `0x307DC4` `mov [r10+6],dx`, `0x309664` `mov [r9+6],cx` — other formula paths.

3. **Display-number paint — cosmetic, number ONLY (the bar-mismatch trap).** The forecast number
   is materialized at display buffer **`0x1407832BE`** (RVA `0x7832BE`, two bytes below the hit-%
   buffer `0x7832C0`). A format dispatch with ~10 branches loads a field from the object (`obj+0x6`
   for damage, picked by flags) into `dx` and `jmp`s to the shared store `0x228488`
   (`mov word [rip+0x55AE2F], dx`). Hook each terminal `jmp 0x228488` (`ExecuteFirst`, set `dx`) —
   a basic attack/Fire uses branch **`0x22802F`** (`[rbp+6]`), *not* the `0x2280D7` first guessed.
   **This paints only the on-screen number; the HP-bar reads `obj+0x6` directly, so painting the
   number leaves the bar natural** — the exact "shows 500 but the bar says 184" symptom. Use it only
   as a number guarantee layered on top of the poke/finalizer, never alone for coherence.

### The result (actual applied HP change)

Force the staged debit at the pre-clamp hook **`0x30A66F`** (§4), bytes `0F BF 45 06`
(`movsx eax, word[rbp+6]` = `word[unit+0x1C4]`). ⚠️ **Decimal gotcha:** `0x30A66F` = **`3188335`**,
not `3188847` (which is `0x30A86F`, where the bytes are `8B D5 41 8D` — a wrong RVA silently logs
`[PRECLAMP-REWRITE-SKIP]` and the result stays natural). **Magic damage rides the same pre-clamp**
(the dispatcher keys on effect-kind `0x300` = apply-HP, not weapon-vs-spell), so one hook covers
physical and magic results alike.

Healing/credit uses the same pre-clamp record at `word[rbp+8] == word[unit+0x1C6]`. A coherent heal
control writes `obj+0x8` for preview and forces the staged credit at pre-clamp for the actual HP
gain; the native APPLY clamp still owns the MaxHP boundary. Passive/side-effect healing uses the
same `+0x1C6` surface, so runtime formulas must distinguish explicit action heals from passive
HP-credit events before forcing credit.

### The coherent recipes

```
damage preview = poke (or finalizer) writes obj+0x6 = formula      -> damage number + ghost depletion
damage result  = pre-clamp 0x30A66F forces word[+0x1C4] = formula  -> actual HP loss

heal preview   = poke writes obj+0x8 = formula                     -> healing number + ghost refill
heal result    = pre-clamp forces word[+0x1C6] = formula           -> actual HP gain
```

Feed both the same formula value and the preview (number **and** bar) equals the result. The forecast
half of the DCL owns hit-% display, damage number + ghost depletion, healing number + ghost refill,
and the applied HP amount for physical, magic, and healing actions.

**Still open — magic AVOIDANCE.** Damage, preview, and the hit-% *display* are unified across
physical/magic, but magic *avoidance* is a separate Faith roll (`0x304E33`, §9); zeroing the
physical evade bytes does **not** make a spell always-hit. That is the remaining always-hit gap for
magic; everything in this section is independent of it (it controls the shown/applied **amount**,
not whether the spell connects).

## Sources

- PSX decomp: https://github.com/Talcall/FFT-1997-Decomp
- Cheat table (offsets + AOBs): https://github.com/bbfox0703/Mydev-Cheat-Engine-Tables (`FFT_enhanced.CT`);
  upstream https://fearlessrevolution.com/viewtopic.php?t=36719 , https://opencheattables.com/viewtopic.php?t=1560
- Classic-data tooling / patches: https://github.com/Glain/FFTPatcher
- Formula docs: https://ffhacktics.com/wiki/Formulas , https://ffhacktics.com/wiki/Battle_Stats ,
  https://ffhacktics.com/wiki/Weapon_Damage_Calculation , https://ffhacktics.com/wiki/Ability_Data
- AeroStar Battle Mechanics Guide: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
- Read-site / layouts: https://github.com/Nenkai/fftivc-nex-layouts (`OverrideAbilityActionData.layout`)
- Loader API: https://nenkai.github.io/ffxvi-modding/modding/mod_loader_api_fft/
- Loader interfaces/models + hook template: https://github.com/Nenkai/fftivc.utility.modloader
  (`Hooks/FFTOResourceManagerHooks.cs`)
- Reloaded hooking: https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
- Reloaded sig scan: https://reloaded-project.github.io/Reloaded-II/CheatSheet/SignatureScanning/
- FFTacticsFix (prior-art code mod): https://github.com/cipherxof/FFTacticsFix
- FaithFramework: https://github.com/Nenkai/FaithFramework
- IVC changes guide: https://gamefaqs.gamespot.com/pc/538659-final-fantasy-tactics-the-ivalice-chronicles/faqs/82197
- FFHacktics IVC board: https://ffhacktics.com/smf/index.php?board=85.0
