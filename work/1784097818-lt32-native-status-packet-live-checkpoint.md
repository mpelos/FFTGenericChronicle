# LT32 ordinary native status-packet live checkpoint

## Result

The ordinary damage-plus-status vertical passes. Josephine's basic Attack on Arthur preserved the
native 14 HP debit, staged Blind only in the native status-add packet, set the native status result
bit, and left Auto-Potion available as the ordinary downstream Reaction. Arthur displayed the red
Blind icon immediately after native delivery.

## Runtime transaction

- Caster: Josephine, character `0x81`.
- Target: Arthur, character `0x80`.
- Action: basic Attack, ability `0`, action type `0x01`.
- Forecast and delivered HP debit: `14`.
- Authored rule: status byte `1`, mask `0x20` (Blind), resistance `10`, forced 3d6 roll `18`.
- Packet result: `packetAdd=0x20`, `packetRemove=0x00`.
- Result flags: native HP debit `0x80` became the HP-plus-status composite `0x88`.
- Downstream reaction: Auto-Potion `441` retained its native 30 HP credit transaction.

The runtime emitted one `[DCL-STATUS]` row and one matching basic-Attack `[DCL]` delivery row. No
callback exception, rollback, duplicate status transaction, or guarded-hook failure occurred.

## Evidence

- Settings: `work/1784019532-battle-runtime-settings.lt32-dcl-native-status-packet.json`
- Runtime log: `work/1784097430-lt32-dcl-native-status-packet-live.log`
- Log SHA-256: `568FF12C3E08262D695AC323DD052F4A92C493422C93A857A9481EDC79326A4D`
- Analyzer: `tools/analyze_dcl_native_status_packet_live.py`
- Analysis: `work/1784097818-lt32-native-status-packet-live-analysis.md`

## Fixture and controls

The reusable Death/Raise autosave also serves this probe. It opens on Josephine's actionable turn
with Arthur adjacent. The shortest path is **Abilities > Attack**, click Arthur, move the feather to
the title bar, then confirm forecast and execution with `F`. The forecast identifies Arthur and
shows Auto-Potion before execution.

## Scope boundary

This proof closes the ordinary HP-result carrier and native add-packet delivery. It does not by
itself prove packet removal, formula-0x22/0x38 retained status-only carriers, split-result carriers,
Song/Dance cadence, or RandomFire per-repeat packet ownership. Those families retain their targeted
integration gates.

## Cleanup

FFT Enhanced and Reloaded-II are stopped. The installed DLL, runtime settings, Reloaded profile,
runtime log, and Enhanced autosave match their pre-test backups by SHA-256.
