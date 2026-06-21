# Custom Formula Feasibility (Tier 2 / Code-Mod) - FFT: The Ivalice Chronicles

This note answers: can Generic Chronicle use *arbitrary* damage math (beyond repointing the
hardcoded formula catalog), and what does it cost? Research is web + source-level; claims are
tagged confirmed vs. inferred.

## Bottom Line

> **UPDATE (2026-06-20): the optimism below was tempered by a hard finding.** The generic hook
> *mechanism* is proven on this exe, but the **damage routine itself is Denuvo-virtualized**: its
> prologue AOB is absent/relocated across launches (we scanned 343 MB; pattern not found), so it
> **cannot be located by static signature scan**. Arbitrary custom math is therefore **NOT yet
> proven achievable on this build** - it is blocked at "find the function." It might still be
> reachable via runtime debugger tracing (HP-write breakpoint -> walk call stack), but until that
> succeeds, treat LEVEL 2 (our own formula algorithm) as an OPEN RISK, not a solved problem.
> What IS fully in reach today is LEVEL 1: re-pointing every ability to the hardcoded formula
> catalog + tuning X/Y/Element/Status via OverrideAbilityActionData (data-only, pipeline proven
> live). See `00-overview.md` "CENTRAL QUESTION" and `04-re-strategy.md` (Denuvo section).

Arbitrary custom damage math is **not** available through any existing modding API. In principle
it requires your **own Reloaded-II C# mod that hooks the damage routine inside
`FFT_enhanced.exe`** - but on this build the damage routine can't be found by static AOB scan
(Denuvo), so the practical path is runtime tracing first, and success is unconfirmed.

```text
Existing API gives custom damage math?   No.
Achievable via static-AOB CreateHook?    No on this build - damage routine is Denuvo-virtualized.
Achievable at all?                       Unconfirmed; needs runtime tracing to even locate the fn.
Reading attacker+target attrs live?      YES - proven via our probe (full unit struct readable).
LEVEL 1 (catalog re-point + X/Y tune)?   YES - data-only, pipeline proven live.
Main cost if pursuing LEVEL 2?           Runtime RE of a virtualized routine (debugger, not AOB).
Vanilla baseline Formula/X/Y dump?        None public; use FFHacktics WotL + IVC rebalances.
```

## 1. The modding APIs do NOT expose damage logic

Confirmed from source (`Nenkai/fftivc.utility.modloader`, `master`).

- The C# table managers (`IFFTOAbilityDataManager` and ~30 siblings, all `: IFFTOTableManager`)
  only do data-table patching: `ApplyTablePatch(modId, model)`, `GetOriginal*`, `Get*`,
  `ApplyPendingFileChanges()`. It is a **file/table replacement system, not a runtime hook.**
- The `Ability` model exposes only `JPCost`, `ChanceToLearn`, `Flags`, `AbilityType`,
  `AIBehaviorFlags`. The backing `ABILITY_COMMON_DATA` is 4 bytes. **Formula / X / Y / Element /
  Power are surfaced by NO interface.** There is **no event/delegate/hook for damage calc.**

Important distinction (keeps Tier 1 valid): the per-ability `Formula/X/Y/Element` columns DO
exist in the **Nex `OverrideAbilityActionData` table**, and the loader merges `.nxd` cells. So
we can still override formula/X/Y by editing that `.nxd` file directly (Tier 1). What's missing
is only a *code* API for it and any *damage-routine* hook.

## 2. Faith Framework is a live data editor, not a hook API

`Nenkai/FaithFramework` (MIT, Nexus #24; shared base for FFXVI + FFT IVC). Feature surface:

- **ImGui API** - in-game debug/dev overlay GUIs.
- **Nex Runtime Interface** - read/write Nex table cells while the game runs.
- Resource/Camera managers (mostly TODO).

It is a **live Nex/NXD editor + debug-UI toolkit**. No hook-registration API, no event system.
Live-editing a formula *parameter* only helps if it lives in a Nex table - and the ability
action *math* is hardcoded in the exe, not in a table. Editing the *algorithm* needs
Reloaded.Hooks (from the Reloaded-II platform), not Faith Framework.

## 3. How custom math is actually done: Reloaded-II hook

The mechanism is solid and already used on this exact executable.

- The mod loader itself sig-scans and hooks `FFT_enhanced.exe` in
  `Hooks/FFTOResourceManagerHooks.cs` via `IStartupScanner.AddMainModuleScan(...)` +
  `CreateHook<T>` with real IVC signatures. **Use that file as the literal template.**
- Pattern:

```csharp
[Function(CallingConventions.Microsoft)]                  // x64
public delegate nint DamageCalcDelegate(nint pAttacker, nint pTarget, nint pAbility);
private IHook<DamageCalcDelegate> _hook;                  // MUST be a field (GC)

_scanner.AddMainModuleScan("48 89 5C 24 ?? 55 56 57 ...", r => {
    if (!r.Found) return;
    _hook = _hooks.CreateHook<DamageCalcDelegate>(MyDamage, _base + r.Offset).Activate();
});

private nint MyDamage(nint pAtk, nint pTar, nint pAbi) {
    // read attacker/target battle-stats structs from the arg pointers (RCX/RDX/R8)
    // ... arbitrary integer math over any stat ...
    return _hook.OriginalFunction(pAtk, pTar, pAbi);      // or replace entirely
}
```

`AsmHook` (mid-function, needs >=7 bytes) is also available; a normal `CreateHook` is cleaner
when args arrive as pointers.

### The hard part: the formula DISPATCH is not yet RE'd (but we now have strong leads)

UPDATE: a follow-up research pass changed this section substantially. See
`04-re-strategy.md` for the full picture. Corrections to the original pessimism here:

- **The `+0xEEA6E50` offset is REAL and usable as a starting lead** - it is the documented
  read-site of `OverrideAbilityActionData` (RVA `0xEEA6E50` / VA `0x14EEA6E50`), annotated by
  Nenkai's own layout file. It is build-specific ("PC/Steam patch 1"), so re-locate via AOB if
  the retail build differs - but it is not a bad value. (My earlier "unusable" claim was wrong.)
- **The in-battle unit struct IS mapped** for `FFT_enhanced.exe` by a public Cheat Engine table
  (HP `+0x30`, MP `+0x34`, Level `+0x29`, Brave `+0x2B`, Faith `+0x2D`, PA `+0x3E`, MA `+0x3F`,
  Speed `+0x40`...). Full offsets + AOB patterns in `04-re-strategy.md`.
- **A community AOB already locates the damage path** (a "damage multiplier" site and the
  `[rax+0x06]` damage-store) - a direct anchor to trace back to the formula dispatch.
- A named **PSX FFT Ghidra decomp** (`BATTLE_calculator_routine`, `CalcHitPercent`,
  `BattleUnitData`) gives a conceptual function map.
- Still genuinely unmapped: the **formula-dispatch switch/jump table** downstream of the
  read-site, and the exact truncation/variance order in the remaster's float math.
- Both exes are **Denuvo-protected** - static analysis/debugging is harder; runtime Reloaded-II
  hooking still works (the loader already hooks this exe).

### Practical RE path

```text
1. Known HP address from a cheat table -> hardware breakpoint on write -> walk call stack up to
   the damage routine. (x64dbg / Ghidra / IDA)
2. Derive a stable prologue AOB (model on Nenkai's existing sigs).
3. Hook via AddMainModuleScan + CreateHook<T>; read attacker/target from RCX/RDX/R8/R9.
4. Optionally upstream the engine-level enabler into Faith Framework.
```

## 4. Prior art (proof the exe can be hooked for gameplay)

- **FFTacticsFix** (cipherxof, Nexus #7, source on GitHub): native DLL, MinHook + pattern
  scan + `DetourFunction` + `VirtualProtect`. It RE'd and hooked real `FFT_enhanced.exe`
  functions (`CFFT_STATE::SetRenderSize`, `WorldToScreen`, `InitScriptVM`, ...) - all
  **presentation/engine, zero battle logic**. Closest existing proof that gameplay routines can
  be RE'd and hooked here.
- Everything else shipped for IVC is data: ~80+ Nexus mods (new jobs, rebalances, sprites) are
  table/XML/asset edits. Cheat Engine / WeMod do runtime memory edits but aren't distributed
  gameplay mods.
- **No confirmed mod hooks the combat/damage routine.** A custom-formula code hook would be
  first-of-its-kind for the enhanced engine.

## 5. Vanilla baseline data (per-ability Formula/X/Y)

- **No public IVC dump** of base Formula/X/Y exists; `OverrideAbilityActionData` is sparse `-1`,
  and FF16Tools only exposes the override (the `-1`s), not the resolved base. To get true IVC
  base values: static-disassemble the hardcoded table, or read resolved values from runtime
  memory.
- **Use FFHacktics WotL ability data as the design baseline.** `Ability_Data` documents the
  8-byte entry: `0x07 Element, 0x08 Formula, 0x09 X, 0x0A Y, 0x0C CT, 0x0D MP`. Layout +
  semantics are shared PSX->WotL->IVC. Prefer WotL values over PSX where they differ, then apply
  IVC rebalances.

### Documented IVC-vs-WotL rebalances (community RE; unofficial, possibly incomplete)

```text
Damage scaling:  enemies take ~30% less, allies deal ~20% more (global tuning).
CT:              broadly reduced (Protect/Shell 25->34 speed, Bahamut 10->15, Graviga 12->10).
MP:              tweaked (Protectja 24->20, Lich 40->50).
JP:              changed (Teleport 600->3000; Meteor 1500->900; +30% JP from own actions).
Other:           Arithmeticks attack-spell damage reduced; Chemist innate Treasure Hunter;
                 Ribbons/perfumes no longer gender-locked.
Ability set:     same WotL set, value rebalances only - no ID renumbering.
```

The global "enemies -30% / allies +20%" tuning is itself a strong hint that a single damage
multiplier sits near the end of the damage routine - a good first hook target and a good way to
locate the function.

## 6. Recommendation for Generic Chronicle

```text
Tier 1 (data) first: do the entire job/skill/weapon/status/encounter redesign via
  OverrideAbilityActionData (Formula/X/Y/Element/CT/MP) + TableData XML. No RE required.

Tier 2 (code mod) only for signature mechanics the catalog can't express: stand up a small
  Reloaded-II C# mod, RE the damage routine (HP-address -> breakpoint -> callstack), hook it
  with CreateHook, and compute custom integer math over the battle-stats struct. Budget the
  cost as reverse-engineering, not plumbing. This would be first-of-its-kind for IVC.
```

## Sources

- Loader API: https://nenkai.github.io/ffxvi-modding/modding/mod_loader_api_fft/
- Loader interfaces/models: https://github.com/Nenkai/fftivc.utility.modloader (Interfaces/Tables)
- Hook template: `fftivc.utility.modloader/Hooks/FFTOResourceManagerHooks.cs`
- Faith Framework: https://github.com/Nenkai/FaithFramework
- Reloaded hooking: https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
- Reloaded sig scan: https://reloaded-project.github.io/Reloaded-II/CheatSheet/SignatureScanning/
- FFTacticsFix (prior-art code mod): https://github.com/cipherxof/FFTacticsFix
- FFHacktics Ability_Data: https://ffhacktics.com/wiki/Ability_Data
- FFHacktics IVC Hacking board: https://ffhacktics.com/smf/index.php?board=85.0
- IVC changes guide: https://gamefaqs.gamespot.com/pc/538659-final-fantasy-tactics-the-ivalice-chronicles/faqs/82197
- Cheat table (damage multiplier): https://fearlessrevolution.com/viewtopic.php?t=36719
