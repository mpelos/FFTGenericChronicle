# Pre-clamp managed callback offline checkpoint

## Objective

Test whether the existing native pre-clamp staged-debit hook can call a managed C# callback
synchronously, inside the same HP-apply frame, and use the callback return value to replace the
staged debit before the engine clamps/writes HP.

This is an architecture probe, not a DCL formula implementation yet. The long-term reason is to
avoid relying on prequeued immediate-action plans when the strongest action context is visible at
the native pre-clamp frame itself.

## Why this matters

The current pre-clamp plan architecture is engine-safe and proven for charged/pending actions and
some immediate cases, but the plan must exist before `0x30A66F` executes. Native actor context
(`actor+0x148 -> unit`, `actor+0x142 -> action id`) is often visible in the pre-clamp stack at that
same frame, but the current C# resolver reads it later from the captured ring buffer. That makes it
excellent evidence, but too late for the same hit.

A synchronous callback would change the shape:

```text
native pre-clamp frame
-> ASM saves volatile state
-> call C# callback with target, staged state, original stack, pre-clamp buffer
-> C# computes/returns forced debit
-> ASM writes [rbp+6] before vanilla reads it
-> vanilla owns HP clamp, KO, UI result
```

If the bridge is stable, the next step is to move from a fixed-debit callback to a callback that
resolves actor context from the original stack and eventually evaluates the real formula.

## Implemented offline

Code touched: `codemod/fftivc.generic.chronicle.codemod/Mod.cs`.

New opt-in runtime settings:

- `PreClampManagedCallbackEnabled` (default `false`)
- `PreClampManagedCallbackForcedDebit` (default `-1`)

New hook behavior when enabled:

- `InstallPreClampDamageRewriteIfEnabled` creates and keeps alive a Reloaded reverse wrapper for
  `PreClampManagedCallback`.
- `BuildPreClampDamageRewriteAsm` calls that wrapper from the existing mid-function ASM hook at
  `0x30A66F`.
- The ASM passes:
  - `rcx = rdi` target unit pointer
  - `rdx = rbp` staged state/result record pointer
  - `r8 = original rsp` at the hook frame
  - `r9 = pre-clamp unmanaged buffer`
- The ASM saves/restores:
  - already-saved volatile GPRs through the existing push/pop block
  - flags through the existing `pushfq`/`popfq`
  - `xmm0..xmm5` around the managed call
- The callback returns:
  - `-1` to skip mutation
  - any non-negative debit to write to `word [rbp+6]`; ASM also clears `word [rbp+8]`
- The callback honors the existing pre-clamp guards:
  - `PreClampDamageRewriteTargetCharId`
  - `PreClampDamageRewriteTargetTeam`
  - `PreClampDamageRewriteMinHp` / `MaxHp`
  - `PreClampDamageRewriteExpectedDebit`
  - `PreClampDamageRewriteExpectedCredit`
  - `PreClampDamageRewriteLogOnly` forces skip
- The unmanaged pre-clamp buffer now has `P_MANAGED_CALLBACKS` at header offset `0x0C`.
- The poller logs `[PRECLAMP-MANAGED-CALLBACK calls=N now=...]` whenever that count changes.

## Offline evidence

`dotnet build codemod/fftivc.generic.chronicle.codemod/fftivc.generic.chronicle.codemod.csproj -c Release`
passes with 0 warnings and 0 errors.

`powershell -ExecutionPolicy Bypass -File codemod/run-offline-checks.ps1` passes. This includes:

- Python tool smoke tests
- static code pattern scan
- C# release build
- C# formula smoke tests
- runtime settings validation
- settings simulator fixtures
- helper dry-runs
- whitespace check

## What offline cannot prove

Offline build cannot prove that Reloaded's runtime FASM assembler accepts the new exact assembly
sequence, nor that the native-to-managed callback is ABI-safe inside the live game thread. A failed
FASM assembly should log `[PRECLAMP-REWRITE-FAILED]`. A serious ABI error could crash or hang the
game, so the first live test must be tiny and tightly guarded.

## Live proof profile

Profile prepared:

`work/1782759291-preclamp-managed-callback-abi-profile.json`

Intent:

- Target only Beowulf (`charId 0x1F`).
- Only mutate an HP apply whose native staged debit is exactly `151`.
- Return managed debit `45`.
- Leave all CT attribution disabled.
- Do not require the Generic Chronicle data mod; this is a code-mod/runtime ABI probe.

Expected live behavior if the managed callback works:

- Preview for Agrias basic attack into Beowulf remains the native value, expected around `151`.
- Execution popup and HP loss become `45`.
- Log contains `[PRECLAMP-REWRITE-HOOK ... managedCallback=1 managedForcedDebit=45]`.
- Log contains `[PRECLAMP-MANAGED-CALLBACK calls=...]`.

If preview is not `151`, do not confirm the attack; the guard would skip and the test would be
uninformative. In that case, update `PreClampDamageRewriteExpectedDebit` to the observed preview
or choose another stable action.

## Risk control

This is not enabled by default. Shipping profiles must leave `PreClampManagedCallbackEnabled=false`
until the live ABI probe proves stable.

The callback currently does not run the formula engine and does not inspect actor context. It is a
minimal bridge test. After a passing live proof, the next offline implementation should be:

1. make the callback resolve the caster actor from `originalRsp` using the proven actor-array rules;
2. return fixed formula results based on caster/target stats without allocations or logging;
3. only then wire the full formula engine or a hot-path-safe compiled subset.
