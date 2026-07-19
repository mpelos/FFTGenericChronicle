# DCL unified clean-v1 offline integration

## Scope

Clean-v1 is the canonical job-free combat-mechanism profile after retirement of mid-route
interruption and Fear. It authors no job, no final-tile ability, and no final balance values.

## Composition

- Base: affinity-corrected runtime with Fear and Approach inactive.
- Added: live-proven final-tile producer at `ExecuteFirst 0xD45A2A2`.
- Removed from the serialized profile: all `DclApproach*`, all `DclFear*`, and
  `DclReactionReservationArbitrationEnabled`.
- Preserved data: v2 action-data NXD/SQLite, ability metadata, item/weapon table, charge/aim table,
  and exclusive Disable/Immobilize duration pair.

The strict composer now supports an explicit top-level `remove` list. Every named removal must exist
in the composed input; missing or duplicate removals fail instead of silently producing a partially
clean profile.

## Bound artifacts

| Artifact | SHA-256 |
| --- | --- |
| Runtime settings | `F9C3A5BC2B70A07AF75AA25C52DA232FC320275A36362A270C70791BF6939830` |
| Runtime/data pair | `7AADD61C00660A0113D3F4986E6ED83810BF93AA52351230FA6E6EE8A44C17B3` |
| Action-data NXD | `44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9` |
| Action-data SQLite | `91D4D768BBDED63D3088DDEC0715812F1ACD2B13624CC2804111E5848B38E749` |
| StatusEffectData XML | `C207DF78A1BED5C19D6F0260A9D104339A81E70E781568C105A8549C511D58C9` |

## Offline gates

- Settings validation passes with warnings only for deliberately live-gated mechanisms.
- Runtime/data pair validation passes.
- Status-duration ownership validation passes with fourteen owned ability/status pairs.
- Composition check proves the output is current and contains zero retired setting keys.
- The clean live-regression matrix passes with seventeen ordered cases and forty-one exact tags.

## Remaining gate

Run the exact clean-v1 installation preflight and the seventeen-case live matrix. The open design
policy for authored final-tile effects remains outside the technical producer and does not justify
inventing a placeholder ability.
