# LT41H native-repeat provenance result

## Scope

LT41H repeats the owner-`443` / delivery-`442` vertical slice with the final validator hook moved
away from the external restore entry. The same capture keeps calc provenance enabled through the
Counter and a later ordinary Dual Wield Attack.

Raw capture:

- `work/1784201674-lt41h-dcl-dual-wield-safe-final-hook-live.log`
- SHA-256 `F48D4111DC036C8D26AD88AA4BA2E4621E176ACC98E19B9A30BD22FE88BCAD48`

Machine-checked analysis:

- `work/1784249339-dcl-native-repeat-provenance-live-analysis.md`
- SHA-256 `C2CBD564E51A7CA4FED29B963C7EC46311260175525BED59DC7CD3815A44EBA5`

## Exact Reaction order

1. Wenyld's adjacent basic Attack hits Rion for a DCL-authored one point and arms the synthetic
   request.
2. The next preselection does not materialize that distant delivery.
3. Choco Beak reaches confirmed state `0x2A`; its native debit `240` is rewritten to `1`, so Rion
   survives and a new request is armed.
4. Counter `442` commits once, passes final validation, materializes Basic Attack `1/0`, and targets
   the attacking Chocobo.
5. Rion's first Counter strike is calc `n=17`, state `0x2A`, payload `124`; debit `189` becomes `1`.
6. Rion's second Counter strike is calc `n=18`, state `0x2F`, with the same exact identity; the old
   runtime rejects it as non-confirmed and native `189` escapes unchanged.

The visible lethal replay described separately cannot contain this `442`: a lethal Choco Beak does
not satisfy `successful-hit-survivor`.

## Independent corroboration

A later ordinary Dual Wield Attack repeats the same provenance:

- first result `n=58`, state `0x2A`, payload `18`, native `126` rewritten to `1`;
- second result `n=59`, state `0x2F`, same exact identity, native `126` applied unchanged.

The repeated state is therefore not specific to Reaction delivery.

## Hook results

The final hook at `0x283157`, length `7`, survives the distant typed rejection and the rest of the
battle without a new WER crash. The two typed hooks fail installation for a separate reason: the
runtime passes an explicit `HookLength=0`, which Reloaded rejects as a zero-sized memory-permission
request. The implementation must omit the explicit length for those sites and retain the exact
seven-byte length only for the final site.

## Implementation decision

`DclActionContext.IsConfirmedExecution` accepts only outer-sweep rows whose battle state is `0x2A`
or `0x2F`. Compute-point numeric ownership, pre-clamp delivery, and the post-calc status producer use
that common classification. Forecast state `0x19`, AI state `0x05` at apply, unknown outer states,
and nested Rend rows remain excluded.

Offline smoke tests cover first execution, native repeat, unknown state rejection, forecast
rejection, and nested-Rend preservation. A short integrated live regression remains necessary to
prove both debits are rewritten and all three Reaction validation hooks install together.
