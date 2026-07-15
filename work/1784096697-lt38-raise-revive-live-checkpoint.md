# LT38 Raise/revive live checkpoint

## Result

LT38 passed after correcting the analyzer's pre-test assumption that authored revive credit bypasses
the target's MaxHP clamp. The compute-point writer replaced Raise's native `46` HP credit with `111`
at confirmed state `0x2A`, with `cached=1`. Native apply then healed Josephine from `0` to her
`91` MaxHP and cleared effective Dead (`+0x61:20->00`) after the HP apply.

This proves the revive ownership boundary:

1. DCL owns the staged HP credit.
2. Native apply owns `min(authored credit, MaxHP)`.
3. The native lifecycle tail clears Dead and returns the unit coherently to battle.
4. A revive formula must not write Dead directly.

## Live transaction

- Caster: Arthur, character `0x80`, ability id `5` (Raise).
- Target: Josephine, character `0x81`.
- KO setup: Josephine casts Death (ability id `30`) on herself with forced 3d6 roll `18`.
- Death execution: staged and applied debit `91`; HP `91 -> 0`; effective Dead
  `+0x61:08->20`.
- Raise natural credit: `46`.
- Raise authored credit: `111`.
- Raise execution packet: `hp=0/46->0/111`, `cached=1`.
- Native applied credit: `91`, equal to Josephine's MaxHP.
- Lifecycle completion: effective Dead `+0x61:20->00` after `[HEALING] 0 -> 91`.
- No legacy `[DCL-KO]`, managed error, rollback, or direct Dead write occurred.

## Evidence

- Live log: `work/1784096430-lt38-dcl-raise-revive-live.log`
- Live log SHA-256: `6D58325E86B580BEB549D07ACBBEA4AF9951A727F0D4A6B04403FFE6FE82AABA`
- Passing analysis: `work/1784096430-lt38-dcl-raise-revive-live-analysis.md`
- Analyzer: `tools/analyze_dcl_raise_revive.py`
- Reusable actionable-turn fixture: `work/1784095864-fft-autoenhanced-snapshot.png`
- Fixture SHA-256: `3C6677ED9E51070D38C13539C2F00B286022D164BD82EDFDE58D354624DEE0E5`

## Control-path improvement

The two-unit fixture is reproducible from Manual Save 05 by placing Arthur, selecting the next
placement tile, pressing `F`, and pressing `E` once to select Josephine. Arthur acts immediately
before Josephine; one Wait reaches Josephine's actionable turn. The fixture reloads through the
atomic Enhanced-to-Continue burst in `27.982` seconds.

Menu hover can override keyboard selection. Moving the feather to the title bar before `F`/`Enter`
prevents stale hover from activating an adjacent row. During LT38, Arthur also had to Teleport away
after queueing Raise because the nearby Claw dealt `162`, leaving him at `37/199` HP before the next
cycle.

## Cleanup

The game and Reloaded-II were closed. Every installed file and save container was restored from the
pre-LT38 backup and verified:

- installed DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- runtime settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- action-data NXD: `E80F48BCCC393BA4B18E9FF41435460E49849316EB4628A9810E2DE6E8C405B2`
- manual Enhanced save: `72EEBE31D0EFA64D9D710E572A2591112B27803C2336B4548931FF9C1F44A7C4`
- Enhanced autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
- Reloaded AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`

