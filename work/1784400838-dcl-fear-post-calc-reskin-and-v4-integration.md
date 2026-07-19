# DCL Fear post-calc reskin and v4 integration

## Starting contradiction

The isolated carrier audit selected Fervor `53` instead of basic Attack `0`, but the first v4
composition failed the C# settings validator:

```text
post-calc producer ability 53: packet ownership mismatch;
expected [2/0x08], got [2/0x04]
```

The failure was correct. Fervor's formula-`0x0A` packet natively owns Berserk at byte `2`, mask
`0x08`, while DCL Fear needs Chicken at byte `2`, mask `0x04`. Ordinary
`replaced-post-calc` intentionally requires the native and authored bit identities to match.

## Hypothesis

A safe carrier reskin requires two independent identities:

1. the complete native source packet that the runtime owns and must suppress;
2. the DCL output packet that the runtime may stage after its authored contest.

Allowing different output bits without an explicit source declaration would leave the native rider
unowned and could leak Berserk beside Fear. Loosening the existing ownership equality was therefore
rejected.

## Implementation

The runtime now has a separate `replaced-post-calc-reskin` policy. Each rule declares exactly one
`NativePacketByteIndex`/`NativePacketMask` source bit in addition to its normal DCL output bit.

The validator requires:

- a loaded catalog entry in the statically mapped post-calc formula families;
- `ActionType=-1`;
- one unique native source bit and one unique DCL output bit per rule;
- the source set to equal the catalog's complete native packet;
- native add/remove and bundle-mode semantics to remain unchanged;
- one policy for the whole ability;
- at least one actual source/output identity change;
- reskin fields to remain absent under every other policy.

After validation, packet composition clears every owned native source bit from both native add and
remove lanes before applying the DCL writes. Resisted, immune, unselected, ineligible, and failed
rules therefore suppress the original rider fail-closed as well.

## Fervor-to-Fear fixture

The integrated Fear rule is:

```text
ability 53 / Fervor
native source: byte 2 mask 0x08 / Berserk
DCL output:    byte 2 mask 0x04 / Chicken-Fear
policy:        replaced-post-calc-reskin
duration:      one target turn
resistance:    clamp(target.brave / 10, 3, 18)
```

Fervor is an isolated technical carrier. The fixture assigns no job and contains no final balance
policy. Fire `16` remains a separate AoE target-expansion falsifier and is not the Fear infliction
carrier.

## Evidence

- C# smoke tests prove successful source clearing plus Chicken staging, resisted source clearing,
  missing-source rejection, no-op-reskin rejection, ignored-field rejection, and mixed-policy
  rejection.
- The settings validator accepts the v4 profile with zero errors.
- The Fear fixture analyzer accepts carrier `53` only with the exact native source fields.
- The runtime/data pair validator now has an explicit `required_fear` contract and rejects an altered
  native mask.
- The nested status-duration and runtime/data v4 pair validators pass against exact SHA-256 hashes.
- `codemod/run-offline-checks.ps1` passes the complete repository suite in `107.7 s`, including
  PowerShell/Python syntax, all tooling smoke tests, static executable analyzers, C# build/smoke,
  settings validation, simulators, dry-run deployment checks, and whitespace validation.

Exact integrated artifacts:

- `work/1784399746-battle-runtime-settings.dcl-unified-sentinel-v4.json`
- `work/1784399746-dcl-unified-sentinel-v4-status-duration-pair.json`
- `work/1784399746-dcl-unified-sentinel-v4-runtime-data-pair.json`
- settings SHA-256: `D7DA5E42D498C60DBA5596F9528F40F19973B05C8E269AD6E4A411D0F078E278`

## Remaining live boundary

Player-confirm enforcement remains deliberately false. The previous Fire attempt is inconclusive
because the Generic Chronicle data and code mods were disabled in that Reloaded session. A valid
instrumented Fire probe must prove that the private pre-confirm builder returns every visibly
affected AoE target before voluntary opposing-target rejection can be armed.
