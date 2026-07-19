# DCL Don't Move / Don't Act exclusive-duration offline closure

## Scope

This checkpoint closes the job-free technical composition for the two shared native status bits:

- Don't Move / `Immobilize` / status byte 4 mask `0x08` / StatusEffectData row 36;
- Don't Act / `Disable` / status byte 4 mask `0x04` / StatusEffectData row 37.

It does not assign skills to jobs and does not declare final resistance curves or ordinary magical
durations. The mechanism profile uses explicit sentinel divisors and labels them as calibration-open.

## Native counter transfer

`tools/analyze_dcl_status_counter_authority.py` serializes the modloader source table to the unique
640-byte table in the installed Enhanced executable. Both shared rows have native `Counter=24`.

`tools/build_dcl_status_counter_patch.py` produced the minimal two-row patch:

- `work/1784394860-dcl-da-dm-StatusEffectData.xml`;
- SHA-256 `C207DF78A1BED5C19D6F0260A9D104339A81E70E781568C105A8549C511D58C9`;
- both rows edit only `Id` and `Counter=0`;
- Doom and `Empty_32` are rejected from generic neutralization.

## Complete producer ownership

The catalog has fourteen add-producer pairs for these two bits across twelve abilities. The paired
runtime profile owns all fourteen and also owns the four additional packet bits required by native
atomicity:

- Celestial Stasis 168: Stop + Don't Move + Don't Act, one all-or-nothing contest;
- Bewitching Gaze 301: Petrify + Darkness + Silence + Don't Move + Don't Act, one random-one contest.

Carrier policy is explicit per ability:

- post-calc replacement: 37, 57, 168, 213, 214, 245, 301;
- action-data suppression: 126, 131;
- retained native carrier: 188, 189, 327.

`tools/validate_dcl_status_duration_pair.py` fails when any neutralized producer is absent, duplicated,
wildcarded to an action type, assigned the wrong carrier policy, lacks positive duration, keeps a
forced probe roll, or uses the wrong resistance category. Physical 213/214 must use target base HP
and exactly one target turn. Magical producers must combine inverse target Faith with caster MA.

## Action-data half

Sinkhole 126 and Wind Slash 131 are ordinary numeric carriers. Their inherited status metadata is
removed by an isolated copy of the base override table; no other row or column changes.

- SQLite: `work/1784395365-dcl-da-dm-action-data.sqlite`;
- SQLite SHA-256: `6AFDA36FEECA41B484CF41276D3A07450AC3917F300C0ECC04CB6D6FD2D749D7`;
- NXD: `work/1784395365-dcl-da-dm-overrideabilityactiondata.nxd`;
- NXD SHA-256: `51C402D02237FB32EBAFEE47491F3FBC33985C76A8C522CC3600617BFDA11C74`;
- FF16Tools SQLite -> NXD -> SQLite round-trip: exact.

The runtime/data pair manifest binds the settings, SQLite, NXD, suppression set, complete status-rule
ability set, and all three native-rider policies:

- `work/1784395365-dcl-da-dm-action-data-pair.json`.

## Intrinsic base HP

The DCL physical category requires innate HP rather than equipment-inflated MaxHP. The runtime now
reconstructs:

`baseHp = MaxHp - head.armor_hp_bonus - body.armor_hp_bonus`

from the proven battle-unit item words `+0x1A/+0x1C` and `work/item_catalog.csv`. The exact subtraction
is Strong, not yet Proven live. The formula context exposes `baseHp`, `baseHpResolved`,
`equipmentHpBonus`, `headItemId`, and `bodyItemId` under both target and attacker aliases. Missing
catalog data, unknown item ids, a truncated unit record, or a nonpositive result yields unresolved
base HP. A physical status rule then fails closed instead of treating total MaxHP as intrinsic HP.

## Offline validation

`codemod/run-offline-checks.ps1` completed successfully in 92.8 seconds after integration:

- PowerShell and Python syntax;
- timeless docs audit;
- all Python smoke tests;
- status-counter executable scan;
- status-duration and action-data manifests;
- Release C# build and smoke tests;
- runtime settings validation for the eighteen-rule mechanism profile;
- installed executable static scans;
- `git diff --check`.

The subsequent SQLite-connection cleanup change passed `test_neuter_data.py`,
`test_dcl_integration_data_pair.py`, `test_dcl_da_dm_action_data.py`, and `git diff --check`.

## Remaining live gates

1. Equip-swap comparison: verify that head/body HP bonuses account exactly for the difference between
   battle MaxHP and intrinsic HP.
2. Deploy the exact paired XML, NXD, settings, and current DLL in an isolated Reloaded profile.
3. Physical verticals: Leg Shot and Arm Shot inflict the correct native restriction and presentation,
   survive until the target completes one turn, then clear exactly once.
4. Magical vertical: one Don't Move or Don't Act producer on the same native bit lasts its separately
   authored duration after the native Counter is zero.
5. Representative retained, data-suppressed, post-calc, Celestial Stasis, and Bewitching Gaze packet
   regressions before merging this pair into the unified whole-DCL live profile.

Until those gates pass, the mechanism is offline-complete and live-gated, not live-proven.
