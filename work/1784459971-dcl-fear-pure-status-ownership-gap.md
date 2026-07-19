# DCL Fear pure-status ownership gap

## Live evidence

- Frozen log: `work/1784459135-dcl-fear-instant-fervor-native-autopotion-live.log`
- Size: `40363` bytes
- SHA-256: `84F7E985CF92DCF6E70A8D87C8E42C6D0E8C30D75F27C78DCCDC5EFC72D3D883`
- Test data changed only Fervor ability `53` CT from `-1` to `0`.
- Arthur visibly received Chicken from Josephine's Fervor before any hostile acted.

The hostile Choco Beak against Arthur reached the complete native Auto-Potion path:

- Reaction `441` committed for reactor index `16`, source index `7`.
- Preselection contained `16:441:active=True`.
- Final delivery validation returned `result=0 accepted=1`.
- Materialization produced action type `6`, action id `441`, item id `240`, target index `16`.
- Calculation and the reaction-effect hook both observed action `441` on Arthur.

Every Fear target observation after Fervor nevertheless reported `owned=0`, including the hostile
attack and Auto-Potion itself. No `[DCL-STATUS]` line was emitted for the successful Fervor packet;
only `[DCL-STATUS-PRODUCER] ... carriesResult=1` appeared.

## Offline diagnosis

The post-calc conditional producer stages the managed status packet at the outer-sweep boundary and
caches its exact contest decision. Before this fix it called `LogCommittedDclStatusPacket`, which
also registers finite duration ownership, only when `carriesResult` was false. A successful pure
status action has `carriesResult=true` but does not necessarily enter the numeric pre-clamp path.
Fervor therefore applied visible Chicken natively without ever adding the `dcl-fear` duration owner.

`IsDclFearOwned` intentionally requires both a matching duration-owner entry and the effective
Chicken bit. The missing entry, rather than reaction suppression, explains `owned=0`.

## Correction under validation

The outer-sweep producer now finalizes logging and duration ownership for every prepared packet and
marks the cached plan `LoggedAtProducer=true`. A later pre-clamp consumer reuses the same plan but
does not duplicate the ownership/log transaction. Effective-bit gating keeps the interval before
native packet consumption inert.

## Environment restoration

FFT Enhanced and Reloaded-II were closed through the privileged UI channel. The installed NXD was
restored from `work/1784457355-pre-instant-fervor-installed-nxd-backup.nxd`; the restored SHA-256 is
`077ACA440092B212B362CCADEAB715E01100B253F961DCB069FF5C09AA89F175`, exactly matching the backup.

The live result proves native Reaction delivery end to end, but it does not yet prove Reaction
delivery while Fear is owned. That gate requires a repeat live run after the producer-ownership fix.
