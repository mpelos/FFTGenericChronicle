# Native policy-source ledger

## Context

The retained-action bridge can execute from direct policy-source requests, but production/live still needs a battle-scoped holding boundary where native context can publish explicit policy sources by ActionInstance before execution consumes them.

## Result

Added `DclCanonicalNativePolicySourceLedger` and attached it to `DclCanonicalBattleRuntime` as `NativePolicySources`.

The ledger stores one `DclCanonicalNativePolicySourceTicket` per retained ActionInstance. Publishing a ticket validates:

- the retained admitted action exists;
- the ticket belongs to the same battle generation;
- unit policy sources cover the retained frozen rows exactly;
- the family policy source matches the runtime-classified ability family.

Publishing a ticket does not draw RNG, publish native carriers, or retire the admitted action. Duplicate tickets fail closed.

`DclCanonicalNativeRetainedActionBridge.ResolvePublishAndRetirePolicyTicket` consumes a retained ticket and retires both the ticket and admitted action only after successful same-ActionInstance native publication. Battle transient reset and admission divergence clear the policy-source ledger.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativePolicySourceLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalBattleRuntime.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

Live native code still needs to produce these policy-source tickets from proven native context. The ledger deliberately validates and stores explicit sources but does not infer them.
