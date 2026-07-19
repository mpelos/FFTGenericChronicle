# DCL Fear v7 native-carrier delivery deviation

## Scope

Validate the Fear status transaction and forced-flee route with Josephine using Fervor on Arthur.
Arthur is the intended target and the fixture gives him 999 maximum HP.

## Exact artifacts

- Full live log: `work/1784421460-dcl-fear-v7-fervor-status-delivery-deviation-live.log`
- Log SHA-256: `55FFC191957B1F6D72B01E44BE5A7AE3E4E1306F4AFBDB72C614D9E35F5E611C`
- Runtime/data pair: `work/1784404744-dcl-unified-sentinel-v6-runtime-data-pair.json`
- Fear carrier: Fervor (`ability 53`), native Berserk packet byte `2` mask `0x08`
- DCL output: Chicken/Fear packet byte `2` mask `0x04`

## Observations

1. The first Fervor resolved against Arthur (`casterIdx=17`, `targetIdx=16`) and the DCL contest
   resisted it: `resistance=9`, `roll=9`, `outcome=resisted`. The producer removed the native
   Berserk result (`flags=0x08->0x00`).
2. The second Fervor resolved against the same target and the producer selected a successful DCL
   result (`carriesResult=1`), but the native result entered the outer-sweep callback with no result
   flag. The callback changed `flags=0x00->0x08`.
3. That second transaction never emitted `packet-add-staged`, never changed Arthur's durable
   status byte `+0x63` from `00` to `04`, and never changed Arthur visually into Chicken.
4. Arthur died to later native enemy damage before a forced-flee route could be observed.
5. The offline analyzer correctly rejects this log because no forced-flee event exists.

## Classification

The forced-flee route is not the failed boundary in this run. The failure occurs earlier, while
delivering the replacement status. The current post-calculation reskin works when the native
formula already materializes a status result (`flags=0x08`), as shown by the earlier successful
Chicken transaction, but setting that result flag after a native formula miss (`flags=0x00`) is not
sufficient to resurrect the status-application path.

Fervor remains formula `0x0A`, whose native status result is Faith-scaled. The DCL hit/status contest
can therefore pass while the technical native carrier independently produces no result. That makes
the carrier nondeterministic and violates the mechanism's intended ownership: the native formula
should only materialize a packet, while the DCL decides whether the output status applies.

## Next hypothesis and gate

Use an isolated action-data override that makes the technical carrier deterministically emit its
native Berserk packet, then let `replaced-post-calc-reskin` clear that packet and apply the DCL
Chicken bit according to the authored resistance roll. Validate the exact override in the paired
runtime/data manifest before another live run.

The next live gate requires, in order:

1. producer receives an already materialized native result;
2. `packet-add-staged` contains byte `2`, mask `0x04`;
3. Arthur's durable status byte changes `+0x63:00->04`;
4. the Chicken dispatcher chooses the DCL route;
5. the forced-flee coordinator stages a nonempty route and returns Arthur to an action opportunity.
