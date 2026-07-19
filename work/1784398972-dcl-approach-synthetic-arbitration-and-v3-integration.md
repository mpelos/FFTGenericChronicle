# DCL Approach/synthetic arbitration and unified sentinel v3

## Scope

Compose the live-proven Approach movement transaction with the already integrated hit-triggered
synthetic Reaction producer without allowing both mechanisms to own pass-2 mailbox, cadence, or
queue state simultaneously. No job, production ability assignment, or balance value is added.

## Conflict found

Approach already rejected a candidate scan when any private synthetic mailbox state was nonzero.
The inverse was missing: a committed hit delivered inside an active Approach queue could ask the
synthetic producer for a new reservation before the movement transaction returned control. Because
both mechanisms use the same pass-2 queue and the sentinel pair `443 -> 442`, that nested request
could create ambiguous queue/cadence ownership.

## Mechanism

The shared policy is explicit first-owner-wins arbitration:

- any pending synthetic reservation prevents Approach from staging;
- Approach phases `Armed`, `QueueRunning`, and `AwaitingResume` publish exclusive queue ownership;
- while that ownership is active, every new synthetic hit reservation is rejected with reason
  `approach-reservation`;
- `Released`, `Aborted`, and the guarded `Resumed` phase return reservation authority.

`DclReactionReservationArbitrationEnabled` is required whenever live Approach and live synthetic
Reaction are enabled together. Runtime settings validation and the runtime/data pair validator both
fail closed when that switch or either side of the required native boundary set is absent.

## Exact v3 bundle

- composition manifest: `work/1784398672-dcl-runtime-composition-manifest-v3.json`
- Approach fragment: `work/1784398672-battle-runtime-settings.dcl-approach-integration-fragment.json`
- composed settings: `work/1784398672-battle-runtime-settings.dcl-unified-sentinel-v3.json`
  - SHA-256: `6FAB187D2460390284FBCF657A9342BF81F654A5492E8D755253459B58A45392`
- duration pair: `work/1784398672-dcl-unified-sentinel-v3-status-duration-pair.json`
- runtime/data pair: `work/1784398672-dcl-unified-sentinel-v3-runtime-data-pair.json`

The v3 bundle reuses the byte-identical v2 SQLite/NXD action-data artifact. Its pair adds exact
Approach owner/delivery, reach, layer policy, native boundary requirements, and shared arbitration
to the existing status-duration, KO, multistrike, Interrupt, Reaction, metadata, item, charge, and
atomic HP/MP contracts.

## Focused verification

- settings validator: zero errors, 27 expected warnings;
- .NET formula/runtime smoke tests: PASS;
- runtime/data pair smoke tests: PASS, including missing-arbitration rejection;
- v3 runtime/data pair: PASS (`approach=443->442@1-2`);
- v3 status-duration pair: PASS, fourteen owned producer/status pairs;
- Approach native/static analyzer: PASS;
- composition freshness, documentation timeless check, coverage evidence, Python syntax, and
  whitespace check: PASS.

## Remaining integration boundary

Fear remains isolated. Its Basic Attack `0` technical carrier overlaps the central physical
pipeline, and the only attempted Fire AoE authority run loaded neither Generic Chronicle package.
The next offline step is to audit the complete action catalog for a non-conflicting Fear carrier and
to prove whether AoE expansion authority is carrier-family-specific or shared.

