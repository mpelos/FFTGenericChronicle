# Overwatch declaration reservations and sequential dispatch queue

## Scope

This checkpoint follows the revised DCL definition of Overwatch and replaces the earlier assumption
that an effect ActionInstance could be allocated only when movement triggered the state.

Fear, job authoring, stop-hit behavior, and intermediate movement tiles are outside this vertical.

## Specification correction

The authoritative Overwatch definition says that the prepared action reserves its Action when it is
declared. The previous final-tile implementation allocated ActionInstance identities at trigger time.
That was late: a trigger/replay path could determine identity allocation rather than consuming an
already committed preparation.

The materialized Overwatch payload now contains an ordered reservation for every authored trigger.
The battle allocates these identities during state application after validating the incoming payload;
callers cannot inject them. Refresh re-declaration replaces the reservation payload while retaining
the stable state instance. Cancelled or expired unused identities remain burned and are never reused.

The next reservation is selected from original trigger count minus current persistent remaining uses.
The state registry remains the sole owner of state lifetime and remaining-use count.

## Final-tile queue contract

An accepted final-tile batch still validates all candidates before mutation and sorts them by state
instance identity. A firing candidate consumes one use and materializes a zero-cost, zero-CastCT,
unit-target declaration using the next previously reserved ActionInstance. A nonmatching trigger
consumes neither state use nor reservation.

Triggered declarations enter one battle-owned queue in the same stable order. The queue enforces:

- no later final-tile event while any prepared Action is unresolved;
- only the head can enter confirmed execution;
- deterministic physical input must contain the exact reserved declaration;
- deterministic cast-family input must recreate that exact declaration before RNG or publication;
- publication must produce the exact ability, Action/revision, source, and ActionInstance;
- the next entry remains blocked until the head native application reaches `Settled` and is retired;
- GlobalCT advancement and checkpoint capture reject a nonempty queue.

The checkpoint schema is revision 5. It persists unused reservations through the state payload and
rejects nonpositive, duplicate, cursor-forward, or charged-Action-conflicting identities. Queued work
is not serialized because the timeline checkpoint boundary forbids half-applied outer Actions.

## Offline sentinels

- State application reserves two ordered identities before movement and advances the battle cursor.
- A direct prepared-runtime trigger consumes the expected first declared identity.
- The final-tile batch uses the exact identity stored in the firing state payload.
- Wrong deterministic execution identity is rejected before native publication.
- The queue retains the exact published native application.
- Only exact settlement retires the queue and native ledger entry.
- Checkpoint capture fails while the prepared queue is merely reserved, before publication.
- After settlement, checkpoint round-trip preserves the unused Overwatch reservation and later cursor.
- Removing an unpublished ticket's exact source or tracked target cancels that ticket and never
  reuses its burned identity; published applications reject out-of-order identity removal.

## Adjacent Cover/Bodyguard boundary

The same prepared-state audit exposed that the lower-level protection controller accepted only
booleans and did not itself prove the attacker, declared target, protector, or persistent link.
A battle-owned typed redirect boundary now validates those identities against a source-owned state
whose protector is distinct from its protected unit and belongs to the same battle generation.
Identity mismatch fails before mutation. Valid current delivery/range/receive-hit legality consumes
one intercept and returns the protector as final target. Invalid current legality removes the link
and returns the original declared target, matching the revised DCL definition.

Offline sentinels cover wrong-link rejection without mutation, successful exact redirect/use
consumption, and invalid-range cancellation without redirect.

## Verification

- `dotnet build ...smoketests.csproj -c PreparedProtection4 --no-restore -m:1 --nologo`
  succeeded with zero warnings and zero errors.
- `dotnet .../bin/PreparedProtection4/net9.0-windows/...smoketests.dll --test-dcl-injury-movement`
  passed.
- The complete smoke executable passed with `formula runtime smoke tests passed`.
- `python tools/report_dcl_implementation_coverage.py --check-only` passed for all 50 mechanisms.
- `python tools/check_docs_timeless.py` passed.
- `git diff --check` passed.
- The regenerated matrix is `work/1784633337-dcl-implementation-coverage.{md,csv}`.

## Remaining live boundary

The live post-route callback must invoke the batch once, supply the queue head with the exact native
deterministic snapshots for its bound family, and drive the published carriers through native apply,
payment, Reaction, settlement, and presentation. The queue/identity lifecycle itself no longer
requires a live policy decision.
