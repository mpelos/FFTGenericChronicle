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
  "HookRegisterProbeMaxLogs": 24
}
```

The profile is observe-only. It intentionally validates with one warning because register probing
should be limited to short RE captures.

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
python tools\watch_live_mapping.py --runtime-events 0 --hook-regs 12 --skip-analyze
```

Then analyze:

```powershell
python tools\analyze_battleprobe_log.py
```

Review `Hook Register Probe` in `work\battleprobe_analysis.md`.

## Log Shape

Example:

```text
[GC-Probe] [HOOK-REGS count=7 ptr=0x2000 id=0x80] rax=0x0:zero rcx=0x2000:unit:touched rdx=0x3000:readable ...
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
