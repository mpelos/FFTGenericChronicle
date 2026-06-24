# Hook Register Context Probe

Status: offline implemented, live validation pending.

## Purpose

The direct damage formula remains blocked by Denuvo virtualization, so the next safe RE path is to
mine the stable `battle_base_ptr` touchpoint for context. That hook is non-virtualized and already
fires with `rcx = battle unit`.

Before doing a live register capture, the static helper can confirm the local executable still has
the known stable anchors and that the volatile damage site is not present in the static file:

```powershell
python tools\scan_static_code_patterns.py --strict-enhanced
```

Review `work\static_code_pattern_scan.md`. This is only a read-only sanity check; any same-hit or
pre-damage path still needs runtime evidence.

`HookRegisterProbe` adds an opt-in, short register snapshot at that hook. It does not call managed
code from the hook and does not rewrite HP/MP. The assembly hook writes register values into the
existing native buffer; the polling thread logs them later as `[HOOK-REGS]`.

Goal: see whether any register at the stable unit touchpoint consistently points to:

- the unit currently acting;
- the current target/action context;
- a battle controller/context object we can follow with later memory probes.

If this exposes a stable pointer, it can become a better attacker/action-context source than CT
and may unlock a pre-damage or same-hit path.

## Profile

Use:

```powershell
work\battle-runtime-settings.hook-register-probe.json
```

Key settings:

```json
{
  "RewriteObservedDamage": false,
  "RewriteObservedHealing": false,
  "RewriteObservedMpLoss": false,
  "RewriteObservedMpGain": false,
  "HookRegisterProbe": true,
  "HookRegisterProbeMaxLogs": 2000,
  "HookRegisterProbeOnHpEvent": true,
  "HookRegisterProbeOnMpEvent": true,
  "HookRegisterProbeOnCtDrop": true,
  "HookRegisterProbeEventMaxLogs": 64,
  "HookRegisterProbeStackSlots": 16,
  "HookRegisterProbePointerScanBytes": 512,
  "HookRegisterProbePointerMaxLogs": 64,
  "HookRegisterProbePointerMaxPointersPerRoot": 8
}
```

The profile is observe-only. It intentionally validates with warnings because register probing
should be limited to short RE captures. The continuous `[HOOK-REGS]` stream is broad, while
`[HOOK-REGS-EVENT]` records the latest hook snapshot when the polling layer observes an HP/MP
event. Event snapshots are not guaranteed to be the exact CPU frame that wrote HP, but they are
much easier to correlate with controlled actions than the first-N hook stream alone.

`[HOOK-PTRSCAN-EVENT]` follows readable, non-unit register roots from the event snapshot and scans
their first bytes for exact known battle-unit pointers. This is the next clue-finding layer: if a
register points at a battle controller/action-context object, the scan should reveal actor/target
unit pointers inside that object without mutating game state.

For charged actions, the most interesting event may be `kind=ctdrop`, because the caster often
resets CT when the action is scheduled while the HP damage lands several turns later.

The follow-up executing-action probe adds two more correlation points:

- `HookRegisterProbeOnPendingResolve=true` emits `[HOOK-REGS-EVENT kind=pendingresolve]` when the
  pending-action tracker sees a caster transition into a short resolving window.
- `PreClampPointerScanBytes>0` emits `[PRECLAMP-PTRSCAN]` from the native staged-damage frame,
  scanning register and stack roots for exact registered unit pointers.

Use this profile when the specific goal is to find a real current executing action/controller
object, especially for delayed AoE actions:

```powershell
work\battle-runtime-settings.executing-action-pointer-probe.json
```

It is observe-only. The pre-clamp hook is enabled only in `LogOnly` mode.

## Live Prep

Dry-run first:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\prepare-live-mapping.ps1 -RuntimeSettings work\battle-runtime-settings.hook-register-probe.json -DryRun
```

When Reloaded-II and FFT are closed, run the same command without `-DryRun` to deploy the code mod
settings and archive the old log.

## Watch Command

After launching through Reloaded-II:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --hook-regs 80 --skip-analyze
```

Then analyze:

```powershell
python tools\analyze_battleprobe_log.py
```

Review `Hook Register Probe` in `work\battleprobe_analysis.md`.

## External Pointer Scan

If the register probe still only sees the stable unit touchpoint, use the external read-only scanner
while the game is sitting between a charged action's cast and resolution. For the Cloud Braver test,
pause/stop input after Cloud schedules Braver but before the damage lands, then run:

```powershell
python tools\scan_live_unit_pointers.py --unit-id Cloud=0x32 --unit-id Beowulf=0x1F --unit-id Agrias=0x1E --near-bytes 0x400
```

The scanner resolves the latest unit pointers from `battleprobe_log.txt`, opens
`FFT_enhanced.exe` read-only, and scans committed readable non-executable memory for places where
two or more requested unit pointers appear close together. Review:

```text
work\live_unit_pointer_scan.md
```

This is meant to find a pending-action / charged-action object that contains source and target
unit pointers. Run it before the charged action resolves; after damage, that pending object may
already be gone or reused.

## Log Shape

Example:

```text
[GC-Probe] [HOOK-REGS count=7 ptr=0x2000 id=0x80] rax=0x0:zero rcx=0x2000:unit:touched rdx=0x3000:readable ...
[GC-Probe] [HOOK-REGS-EVENT kind=damage event=1 hookCount=120 hookPtr=0x2000 targetPtr=0x3000 id=0x01] rax=...
[GC-Probe] [HOOK-REGS-EVENT kind=ctdrop event=2 hookCount=128 hookPtr=0x4000 targetPtr=0x4000 id=0x32] rax=...
[GC-Probe] [HOOK-PTRSCAN-EVENT kind=damage event=1 targetPtr=0x3000 id=0x01] rbx@0x5000:hits=+0x20->0x2000:unit:id=0x80;ptrs=...
```

Classifications:

- `unit:touched` means the register equals the unit pointer that fired the hook.
- `unit:id=...` means the register equals another registered battle-unit pointer.
- `readable` means the address points to readable process memory, but is not yet identified.
- `unreadable` means `VirtualQuery` did not consider the address readable.
- `zero` is literal zero.

## Offline Contract

Covered by:

```powershell
python tools\test_runtime_tooling.py
python tools\test_runtime_profiles.py
python tools\test_static_code_patterns.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```

## Caveats

- This is not a formula hook and does not prove action identity by itself.
- The stable hook may be a UI/stat touchpoint, so registers may only show the unit being read.
- The signal is still useful: if a second unit pointer or battle-context object appears
  consistently around actions, it gives us a concrete next pointer to probe.
