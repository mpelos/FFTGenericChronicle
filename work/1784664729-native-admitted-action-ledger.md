# Native admitted action ledger

## Context

The guarded admission hook can capture a complete native outer action at the target-list boundary,
but the completed action was only returned to the callback/log path. Production family-policy
providers need a battle-owned retained object they can consume later without depending on the
immediate callback return.

## Result

Added `DclCanonicalNativeAdmittedActionLedger` and attached it to `DclCanonicalBattleRuntime` as
`NativeAdmittedActions`.

The ledger:

- retains only complete captured outer actions;
- keys them by ActionInstance;
- validates battle generation, current observed UnitKeys, and complete frozen native rows;
- rejects duplicate ActionInstance publication;
- can be queried or retired by later policy-provider/composition work.

`Mod.DclCanonicalAdmissionCallbackImpl` now publishes a completed capture into the ledger and clears
it on admission-hook divergence.

## Evidence

- Smoke test publishes the Direct captured action, retrieves it by ActionInstance, verifies retained
  source/target/frozen row data, and rejects duplicate publication.
- Build passed before documentation updates.

## Remaining

The next production gap is still policy-provider construction: unit-policy inputs and
family-policy inputs must be supplied from live native/runtime providers before the retained
admission can be converted into a composed plan and dispatched.
