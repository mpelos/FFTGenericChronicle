# Reverse-Engineering Strategy - Using Classic FFT Knowledge to Hook the IVC Damage Routine

Answers: "classic FFT / WotL have fully-documented formulas and existing damage mods - can we
use them to help RE the remaster?" Short answer: **yes, as a knowledge map (not as portable
code)**, and combined with newly-found public artifacts the Tier-2 RE is now well-scoped.

## What ports and what does not

- **Does NOT port:** the classic/WotL ASM patches themselves. Classic = PSX (MIPS R3000,
  `BATTLE.BIN`), WotL = PSP (MIPS). IVC = x86-64 `FFT_enhanced.exe`, a from-scratch
  re-implementation on the FF16 "Faith" engine. The 1997 source was lost; SE rebuilt it. So no
  legacy MIPS routine or byte patch applies.
- **DOES port:** the *design knowledge* - exact formulas, data-struct layout, and the
  formula-dispatch architecture. IVC faithfully reproduces the WotL ruleset and the classic
  ability-action data model (same `Formula/X/Y/Element/CT/MP`), so classic docs tell us exactly
  what the x64 code must compute and what struct it reads. This converts RE from "blind" to
  "guided": find the function that computes *known math* over a *known struct*.

## The public artifacts we can build on (all found, see Sources)

### A. PSX Ghidra decomp = conceptual function map
`Talcall/FFT-1997-Decomp` - named Ghidra decomp of FFT PSX US, 606 named `BATTLE_*` functions.
Directly relevant names:

```text
BATTLE_calculator_routine(CurActionTargetData*)   // core damage calc
CalcHitPercent, AttackEvadeCalc, BowWeatherCalc
BATTLE_ability_hit_processing, BATTLE_award_xp_and_jp
Calculate_Stat_Real, Equipment_Stat_Calc, Calculate_Zodiac_Sign
struct BattleUnitData  (AllActionUnitData / CurActionUnitData / CurActionTargetData)
```

Addresses/packing differ on x64, but the math and control flow are the map. NOTE: not a
matching/recompilable decomp, and there is NO decomp of WotL or the remaster engine.

### B. Public Cheat Engine table = REMASTER struct offsets + AOB signatures
Source: `bbfox0703/Mydev-Cheat-Engine-Tables → FFT_enhanced.CT` (mirror of the FearLess /
OpenCheatTables tables), targets `FFT_enhanced.exe`, Steam v1.0 Oct 2025. **Verbatim, verified.**

In-battle unit struct (base pointer = unit; `rcx`/`rdi` at the hook sites):

```text
+0x00  char id (byte)            +0x30  HP        (word)
+0x04  team/group id (byte)      +0x32  MaxHP     (word)
+0x05  friend/foe (bit 0x10)     +0x34  MP        (word)
+0x28  EXP (byte)                +0x36  MaxMP     (word)
+0x29  Level (byte)              +0x3E  PA        (byte)
+0x2A  MaxBrave (byte)           +0x3F  MA        (byte)
+0x2B  Brave (byte)              +0x40  Speed     (byte)
+0x2C  MaxFaith (byte)           +0x42  Move      (byte)
+0x2D  Faith (byte)              +0x43  Jump      (byte)
                                 +0x4F/0x50/0x51  X / Y / Dir (byte)
```

AOB scan signatures (`aobscanmodule(NAME, FFT_enhanced.exe, <pattern>)`):

```text
battle base ptr      0F B7 41 30 66 89 42 0C ...   (inject @ FFT_enhanced.exe+0x21305C)
DAMAGE multiplier    0F B7 47 30 2B C2 85 C0 41 0F 4E CE 8A D1 E8 F2
damage multiplier #2 2B C8 8D 04 11
JP multiplier        03 C2 8B CF 41 3B C0
XP multiplier        0F B7 84 7B 1E 01 00 00
min brave/faith      41 0F B6 5A 2B
min spd/jmp/mov      0F B6 47 42 66 89 43 30
```

Damage mechanism observed: the damage-mult site tags player/enemy via `[rdi+5]&0x10` and
`[rdi+4]`, scales `edx` (damage) via AVX float vectors, then **stores the result as a word at
`[rax+0x06]`**. That `[rax+6]` store is the prime anchor to walk back to the formula dispatch.
These patterns are build-specific - re-scan if a patch shifts them.

### C. Table->code read-site (already public)
`OverrideAbilityActionData.layout` annotates the read-site:

```text
// PC/Steam patch 1 handling code @ FFT_enhanced.exe+eea6e50  (VA 0x14EEA6E50)
// Switch v0 handling code        @ FFT_enhanced.nso+1e21b0   (VA 0x71001e21b0)
columns: Flags12[] Flags34[] Range EffectArea Vertical Element Formula X Y InflictStatus CT MPCost
```

Each cell >=0 is cast to byte and patches the classic in-memory ability-action struct. So the
code at the read-site resolves an ability's Formula/X/Y just before combat math runs - the
natural place to start tracing toward the dispatcher.

## Formula fingerprint sheet (grep these constants in the x64 disassembly)

Because we know the math, we recognize the routines by their invariant constants/operations.
Highest-signal first:

```text
1638400            stat display divisor: DisplayedStat = RawStat * JobMult / 1638400
                   -> marks the stat-derivation routine (near-unique constant)
10000  (or /100,/100)  Faith term: MA * Q * CasterFaith/100 * TargetFaith/100
5/4 -> 4/3 -> 3/2 -> 3/2 -> 2/3 -> 2/3   physical XA modifier chain, truncating each step
                   (Strengthen, Atk-UP, Martial Arts, Berserk, Def-UP, Protect) - most
                   distinctive physical-routine fingerprint
Zodiac  3/2, 5/4, 3/4, 1/2   gated on a 12x12 compatibility lookup table
(PA+Speed)/2       knife / longbow XA
WP*WP              gun XA           PA*PA*Brave/100   bare fists
MA*WP              staff / magic gun
2/3                Protect / Shell damage reduction
element            weak *2, half /2, absorb -> negate sign
Speed*WP           Throw            PA*WP (*3/2 if polearm)   Jump
XA + rand(1..XA) - 1                critical hit
8 + 2*JobLevel + Level/4            JP per action;  share *1/4
EXP base 10, +/-1 per level diff, EXP-Boost *3/2
CT += Speed; act at CT>=100; reset 100/80/60; charged action wait = 100/Speed
```

Disambiguation: `5/4` and `3/4` appear in both the XA chain and Zodiac; Zodiac applies once,
after the XA chain, gated on the 12x12 table. SPECULATIVE: which damage-variance variant the
remaster uses (`Dam/10 +- ` vs `*rand(100..150)/100`) and exact truncation order - verify in
the binary.

## CONFIRMED on this install (probe iteration 1)

Battle Probe code mod ran on the live game (Steam, base 0x140000000, module size 0x190EB000).
All 7 signatures matched -> the cheat-table AOBs are valid on THIS build. Confirmed RVAs (offset
from module base; stable across ASLR):

```text
battle_base_ptr     module+0x226D98    movzx eax, word [rcx+0x30]   -> +0x30 HP (word) CONFIRMED
damage_multiplier   module+0x7ED4A52   movzx eax,[rdi+0x30]; sub eax,edx -> applies damage; rdi=target, edx=damage
damage_mult_2       module+0x30A685
jp_multiplier       module+0x283754
xp_multiplier       module+0x283767
min_brave_faith     module+0x8D9D0E0   movzx ebx, byte [r10+0x2B]   -> +0x2B Brave (byte) CONFIRMED
min_spd_jmp_mov     module+0x36027F    movzx eax, byte [rdi+0x42]   -> +0x42 Move (byte) CONFIRMED
override read-site  RVA 0xEEA6E50       WITHIN module (VA 0x14EEA6E50) -> valid lead, not bad
```

Bonus: the matched instruction bytes themselves statically confirm struct offsets (+0x30 HP
word, +0x2B Brave byte, +0x42 Move byte) - matching the map in `05`.

### IMPORTANT: stability across runs (Denuvo) - probe iteration 2

Re-running the probe showed the matches split into two groups:

```text
STABLE (byte-identical across runs; RVA < 0x400000 = real .text):
  battle_base_ptr 0x226D98 | damage_mult_2 0x30A685 | jp 0x283754 | xp 0x283767 | min_spd_jmp_mov 0x36027F
UNSTABLE (move or vanish between runs; RVA in the 130-280 MB range):
  damage_multiplier (16-byte): run1 0x7ED4A52, run2 NOTFOUND
  min_brave_faith (5-byte): run1 0x8D9D0E0, run2 0x10885C7D  (too short -> coincidental)
```

Conclusion: the damage routine lives in a **Denuvo runtime-decrypted/relocated region** - a static
main-module AOB cannot reliably find or hook it (its address changes per launch). So:

- Hook only the **stable .text anchors** (e.g. `battle_base_ptr`, rcx = a unit) for live reads.
- Get damage numbers the **Denuvo-proof** way: sample a unit's HP via the stable anchor and log
  **HP deltas** (HP drop = damage taken) instead of hooking the damage routine.
- The formula-dispatch RE (Tier 2 custom math) will need runtime tracing in a debugger (HW
  breakpoint -> callstack while Denuvo code is live), not static AOB - or we stay data-only.

### Offline static scan helper (2026-06-21)

`tools/scan_static_code_patterns.py` now makes the AOB check reproducible without launching the
game:

```powershell
python tools\scan_static_code_patterns.py --strict-enhanced
```

Current report: `work/static_code_pattern_scan.md`.

On this install, the scanner maps raw file offsets through the PE section table and confirms that
the expected `FFT_enhanced.exe` stable anchors still exist at the same RVAs observed live:

```text
battle_base_ptr  0x226D98
damage_mult_2    0x30A685
jp_multiplier    0x283754
xp_multiplier    0x283767
min_spd_jmp_mov  0x36027F
```

It also confirms `damage_multiplier` has **zero static-file matches**. That is useful negative
evidence: the direct damage application site is still not a static AOB target in the checked-in
Steam executable, so the same-hit/pre-damage route must come from live/runtime evidence (for
example `[HOOK-REGS]`, debugger callstacks, or a newly discovered stable context pointer).

Probe iteration 3 implements the stable-anchor unit dump + HP-delta damage logging.

### CONFIRMED (probe iteration 3)

Hooked `battle_base_ptr` (rcx = unit). Read EVERY struct field live with sane values, e.g.
`Lv55 HP458/458 MP81/81 PA15 MA11 Sp9 Mv7 Jp3 Br97 Fa70` (ally), a monster at `MA39`, allies
team=0 / foes team=3 with foe bit +0x05&0x10. The full runtime struct map (section A of `05`) is
verified. HP-delta damage capture works (e.g. friendly-fire 473->290 = 183; lethal 298->0 = 298),
giving real damage numbers without touching the Denuvo-locked damage routine.

What this harness does NOT yet capture: the **attacker + ability/weapon** per hit (those live in
registers at the damage routine, which is Denuvo-relocated). So full back-computation of a
formula needs either (a) controlled data-layer experiments (known attacker/weapon via ENTD/data,
read damage via HP delta), or (b) runtime debugger tracing of the relocated damage code.

### Probe iter 5: stable sites are UI, not the formula

Register-dumped the stable `damage_mult_2` (module+0x30A685). Only `rdi` = one unit; `edx` =
MaxHP, `eax` = MaxHP-currentHP. So that "secondary damage-mult" match is **HP-bar/UI math**, not
the damage formula - no attacker, no ability. Confirms: the stable .text sites are display code;
the real damage routine is only in the **Denuvo-relocated region** (present during battle).

### Probe iter 6: locate the relocated damage routine in-battle

Approach: at runtime, walk committed EXECUTABLE regions (VirtualQuery; never touch unmapped/guard
pages) and scan for the specific 16-byte damage-apply pattern
`0F B7 47 30 2B C2 85 C0 41 0F 4E CE 8A D1 E8 F2`. Retry every 3s until a battle makes the code
resident; then install a read-only register-dump hook there. The dump shows every register that
points at a unit (attacker + target) plus the damage value - the full formula context for path 2.
Address is session-specific (re-scan each launch). Risk: hooking Denuvo-region code may crash or
be integrity-checked; experimental.

### CONCLUSIVE: the damage routine is Denuvo-VIRTUALIZED (probe iter 6)

An in-battle full-memory scan (ReadProcessMemory, AV-safe) over all executable-readable regions
found the damage routine's 16-byte pattern **nowhere**:

```text
exec regions: 0x140001000 +0x610000 (.text, 0x20) ; 0x143F9E000 +0x14779000 (~340MB, 0x80 Denuvo) ; +others
total scanned: 343 MB exec-readable, 0 execute-only, pattern NOT FOUND across ~55 passes (~2 min)
```

Since nothing was execute-only (so nothing was hidden from reading) yet the x86 pattern is absent,
the damage code is **virtualized by Denuvo** (translated to a private VM bytecode). The one run-1
match was transient/coincidental. **Implication: the formula routine cannot be AOB-hooked.**
Arbitrary custom damage math via hooking the formula is blocked by Denuvo by design.

Remaining options for a damage overhaul:
1. **Data layer (Tier 1, Denuvo-proof):** re-point every ability's `Formula/X/Y/Element/CT/MP` in
   `OverrideAbilityActionData` + weapon `Formula/Power` + `JobData` stats. Limited to the ~100
   existing formula shapes, but total coverage of which/where/how-much.
2. **Post-damage runtime reconciler (current Tier 2 mainline):** use the data layer to neuter
   vanilla damage into safe placeholders, observe HP/MP deltas through `battle_base_ptr`, resolve
   attacker by CT reset, decode coarse action identity through sentinels or later action context,
   compute custom C# formulas, and rewrite the final HP/MP value. This avoids the virtualized
   formula routine entirely; see `06-code-mod-battle-runtime-architecture.md`.
3. **Stable .text touchpoint/pre-damage clue:** hook non-virtualized instructions such as
   `battle_base_ptr`, UI/preview, or future action-state sites and use register/context probes to
   find currently-acting unit/action data before HP changes.
4. **Hardware breakpoint (VEH + DRx) on HP write:** robust to relocation but lands inside the
   Denuvo VM dispatch - messy, registers won't cleanly map to game state. Last resort.

## BREAKTHROUGH (2026-06-26): hit/miss/block/parry runs in NON-virtualized .text

Only the *roll arithmetic* (hit%, evade, damage math) is Denuvo-virtualized. The **outcome
dispatch, the result/animation selection, and the apply step run in normal, hookable `.xcode`**
(RVA < 0x400000, ASLR-stable, same class as `battle_base_ptr`). Byte-verified (capstone+pefile)
and **LIVE-confirmed** via an observe hook at the selector. Image base 0x140000000; RVAs are
module-relative. Found over 3 RE rounds; full detail `work/hit-miss-control-breakthrough.md`.

Code anchors (all real `.xcode`, hookable with CreateAsmHook):

```text
unit/result RECORD array   module+0x1853CE0   stride 0x200, <=21 entries (runtime 0x141853CE0).
                           The per-unit battle struct IS this array. Ramza(id 01)=idx16 @0x141855CE0.
result DISPATCHER          module+0x38A4FC    event-queue loop; DECISION branch @0x38A6F1
                           (cmp edx,0x300), edx=(category<<8)|unitIdx:
                           0x300=apply HP/MP/stat ->0x30A51C | 0x200=status (VM 0x38ABBC)
                           0x100=turn-done (VM 0x38BBFC) | 0xFF00=terminator | 0xE000=init
category PRODUCER          module+0x30F0C4    walks records by global phase dword[0x186B044];
                           the 0x300 (apply) gate is VM thunk 0x30FA34 (the roll lives here)
APPLY path                 module+0x30A51C    newHP = clamp(HP + word[unit+0x1C6] - word[unit+0x1C4], 0, MaxHP)
                           reads staged dmg word[+0x1C4], heal word[+0x1C6]; consults NO hit-flag
pre-clamp staged-dmg HOOK  module+0x30A66F    0F BF 45 06 = movsx eax,word[rbp+6] (=word[unit+0x1C4]);
                           our EXISTING damage-rewrite hook -> already controls hit-vs-zero damage
result/animation SELECTOR  module+0x205210    prologue 48 89 5C 24 08 48 89 6C 24 10;
                           r8=actor, record=[r8+0x148], cl(arg)=evade-type; reads +0x1E5,+0x1C4
evade-type STAGING write    module+0x205B39    mov byte[rdi+0x1C0], ah   (engine writes evade-type here)
combat-popup digit RENDER  module+0x266AE0    REAL int->digit->glyph; value [rdi+0x344];
                           3-digit split 0x2671BE; glyph map 0x267350; popup value mirror 0x3740200

VIRTUALIZED (E9/E8 into .edata, NOT hookable - and not needed):
  roll gate 0x30FA34 | hit%/evade helpers 0x269760 0x2759F8 0x2B8F30 0x2740A0
  result handlers 0x38ABBC 0x38BBFC | staging helpers 0x30BC3C 0x30BCF8 | evade-input scratch 0x7832E6
```

**The EVADE-TYPE byte (record `+0x1C0`, also passed in `cl` to the selector) is the lever** that
selects hit vs which evade animation. **LIVE-CONFIRMED enum** (observe hook at 0x205210, 2026-06-26):

```text
0x00 = HIT                         (+1BB=02 +1BE=01 +1C4=dmg +1E5=0x80 ; damage applies)
0x01 = cloak/accessory evade       (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x02 = weapon parry (RH/LH guard)  (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x03 = shield parry/block (LH grd) (+1BB=01 +1BE=00 +1C4=0   +1E5=00)
0x04 = class evade ("Miss")        (+1BB=01 +1BE=00 +1C4=0   +1E5=00 ; +1C2=FF observed)
0x06 = plain miss (failed ability accuracy roll, e.g. Steal/Charm) (+1BB=01 +1BE=00 +1C4=0 +1E5=00)
```
**ENUM COMPLETE — all 6 values LIVE-VALIDATED (2026-06-26).** 0x05 unobserved (gap, likely unused).
Hit = 0x00 (damage applies); 0x01-0x06 are all no-damage outcomes (`+1C4=0`) that differ ONLY in
`+0x1C0`, which selects the on-screen animation. evadable physical abilities (Rend Helm) route through
0x01-0x04 like a basic Attack; a failed *accuracy* roll (Steal/status hit%) routes through 0x06.
```
Every evade shares `+1BE=00 / +1C4=0 / +1E5=00`; ONLY `+0x1C0` changes which animation plays.

**CONTROL RECIPE** (compute our own DCL roll, then write the engine's already-owned fields):
```text
force-HIT(D):   word[unit+0x1C4]=D ; word[+0x1C6]=0 ; +0x1C0=0x00     (existing 0x30A66F hook)
force-EVADE(t): +0x1C0=t (0x01 cloak / 0x03 shield / ...) ; +0x1BE=0 ; +0x1C4=0
                hook the selector 0x205210 (write before it reads) or the staging write 0x205B39
```
Force vanilla ~always-hit upstream (target evade bytes ->0, or ability `Evadeable` off) so the
apply/selector path is reached and our write is authoritative.

**FORECAST hit% display: virtualized** (no real-code integer to substitute; both the % math and
the UI draw are VM). Levers: (1) write target evade-input bytes (+0x4B/+0x4A/+0x4E/+0x46/+0x47) -
real but QUANTIZED; (2) wrap a VM-thunk return at a forecast call site
(0x26A618/0x26A746/0x26A89A -> 0x269760) - needs one live capture to pin the % slot; (3) draw our
own glyphs (overlay). No single arbitrary-% write exists.

Harness: `Mod.cs` `ResultSelectorProbe` observe hook at 0x205210; profile
`work/battle-runtime-settings.result-selector-probe.json`. Control plan:
`work/hit-miss-control-implementation-plan.md`.

## Historical direct-hook attack path (not current mainline)

```text
1. Stand up a minimal Reloaded-II C# mod (template: loader's Hooks/FFTOResourceManagerHooks.cs).
2. AOB-scan the damage-multiplier site (0F B7 47 30 2B C2 ...) and the [rax+0x06] damage store.
   That site already has the resolved damage in edx and unit ptrs in rdi/rax.
3. From there, walk UP the call chain (or breakpoint the [rax+6] store) to the routine that
   produced edx = the formula output -> that caller contains/leads to the formula dispatch.
4. Find the switch/jump table indexed by the Formula byte (resolved at the +0xEEA6E50 read-site).
   Pinning this ONE dispatcher exposes all ~100 formula cases at once.
5. Identify each case via the fingerprint constants above; cross-check semantics against the
   Talcall decomp's BATTLE_calculator_routine / CalcHitPercent.
6. Map unit fields with the CT offsets (HP +0x30, PA +0x3E, MA +0x3F, Brave +0x2B, Faith +0x2D).
7. Hook the formula function (or the dispatcher) with CreateHook<T>; implement custom integer
   math; validate output against hand-computed expected values (the classic formulas are the
   oracle).
8. Optionally confirm field->behavior live via FaithFramework's Nex Runtime Interface.
```

This direct-dispatch route remains useful only if we resume same-hit/pre-damage replacement work.
The current mainline is the post-damage reconciler, with focused RE on action identity, equipment
context, hook-register clues, and eventually a pre-damage window.

## Caveats

- Both `FFT_enhanced.exe` and `FFT_classic.exe` are siblings on the Faith engine and **Denuvo-
  protected**. Classic is NOT a PSX emulator and offers no easier static-RE path; Enhanced is the
  better-mapped target. Denuvo complicates static analysis/debugging, but runtime Reloaded-II
  hooking works (the mod loader already hooks this exe).
- All offsets/AOBs are Steam v1.0 (Oct 2025) Enhanced; patches shift them - re-scan via the
  neighboring stable bytes.
- This would still be the first gameplay-logic code mod for the engine; the artifacts above
  remove most of the blind searching, not all of the work.

## Sources

- PSX decomp: https://github.com/Talcall/FFT-1997-Decomp
- Cheat table (offsets + AOBs): https://github.com/bbfox0703/Mydev-Cheat-Engine-Tables (`FFT_enhanced.CT`);
  upstream https://fearlessrevolution.com/viewtopic.php?t=36719 , https://opencheattables.com/viewtopic.php?t=1560
- Classic-data tooling / patches: https://github.com/Glain/FFTPatcher
- Formula docs: https://ffhacktics.com/wiki/Formulas , https://ffhacktics.com/wiki/Battle_Stats ,
  https://ffhacktics.com/wiki/Weapon_Damage_Calculation
- AeroStar Battle Mechanics Guide: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
- Read-site/layouts: https://github.com/Nenkai/fftivc-nex-layouts (`OverrideAbilityActionData.layout`)
- Reloaded hooking: https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
- FaithFramework: https://github.com/Nenkai/FaithFramework
- FFHacktics IVC board: https://ffhacktics.com/smf/index.php?board=85.0
