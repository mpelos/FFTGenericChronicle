# DCL Tilemap Candidates — Battle Map/Tile Structure (offline RE, 2026-07-02)

Offline static-analysis deliverable for `docs/modding/08-dcl-information-requirements.md` §2
"Height and tile occupancy" — the last front with no runtime surface. **No live process was
touched.** Method: full disasm of the position helper `0x30D484` (already known to produce the
pending-target tile params at caster `+0x1AC/+0x1AE/+0x1B0`), then a RIP-displacement brute scan
of `.xcode` for every real-code reference to the discovered globals, then context disasm of the
readers. Script + raw output archived: `work/disasm_tilemap.py`, `work/disasm_tilemap.out.txt`.
Calibration passed (`0x30A66F` = `0F BF 45 06`).

Confidence labels per the modding manual: **Strong (static)** = read directly from real-code
disasm, multiple corroborating sites; **Hypothesis** = single-site or inferred; nothing here is
live-proven yet.

---

## 1. TL;DR — the map surface

```text
TILE TABLE        VA 0x140D8DCB0  (RVA 0xD8DCB0, .debug$P data section — ordinary writable memory)
                  8 bytes/tile, 256 tiles/level, 2 levels: level-1 copy at +0x800; total 0x1000
TILE INDEX        idx = (level << 8) + y*mapWidth + x        (row-major, row length = width)
UNIT -> TILE      x = unit+0x4F, y = unit+0x50, level = unit+0x51 bit7   ((b>>7)<<8 in ALL sites)
MAP WIDTH         byte [0x140C6AD6A]     MAP HEIGHT (depth/Y)  byte [0x140C6AD6B]
SELECTION MAP     VA 0x1417F04E0, 5 bytes/tile, +0x500/level  (move/target workspace + occupancy)
CORNER HEIGHT     h(corner) = 2*tile[2] + ((tile[4] >> 2*corner) & 3) * (tile[3] & 0x1F)
```

All of it is static-address data (no pointer chain, no ASLR) readable by the existing poller and
by pre-clamp-frame callbacks. Both tables sit in the same data section as the unit array
(`0x141853CE0`), i.e. normal writable memory — Denuvo virtualizes code, not data.

## 2. Per-tile record layout (8 bytes) — field hypotheses

| Off | Meaning | Confidence | Evidence (RVAs) |
| --- | --- | --- | --- |
| +0 | **terrain/surface type**, bits 0–5 (`& 0x3F`); bits 6–7 unknown | Strong (static) | `0x304F58 mov cl,[rdx+rax*8]; and cl,0x3F` then `cmp cl,0x12 / cmp cl,0x1C` (two terrain ids rejected for landing — water-ish) |
| +1 | unknown (never seen read in scanned sites) | — | |
| +2 | **height** (base elevation; doubled into half-units by consumers) | Strong (static) | helper level-select reads `+2`/`+0x802` and picks nearest level (`0x30D575..0x30D5C7`); corner formula `0x3050BC lea ebx,[rdx+rax*2]` |
| +3 | bits 0–4 = **slope height** (`& 0x1F`), bits 5–7 = **water depth** (`>> 5`) | Strong (static) | corner formula multiplies `b3 & 0x1F`; landing check `0x304F65` requires `b3 < 0x20` and `(b3 & 0x1F) <= 2`; AoE loop `0x27EF8A` adds `b3 >> 5` to height (submerged surface) |
| +4 | **slope corner weights** — four 2-bit fields, shift = `2*corner`, factor 0–3 on slope height | Strong (static) | `0x3050A5 movzx edx,[r14+idx*8+4]; shr edx,cl; and edx,3; imul edx,(b3&0x1F)` with `cl` derived from direction (0/2/4/6) — the remaster's re-encoding of the classic slope-type enum |
| +5 | **dynamic per-action mark byte**: bit 0x40 mass-cleared then set per action phase; bit 0x80 also tested | Strong (static) for the mechanics; meaning = Hypothesis ("tile in current effect/valid set") | mass-clear loop `0x2099B7 and byte[rax+rdx+5],0xBF` over `0x1000` bytes (= all 512 records); gate `0x30D3F0 test byte[r12+rax*8+0xD8DCB5],0x40` immediately before an order-record write and the `0x30D42A` status recompute; sign-bit test `0x282566 cmp byte[rdx],0; jge` in a min/max sweep |
| +6 | **static flags**: bit0 = unselectable/invalid; bit1 = unwalkable/blocked | Strong (static) | helper `0x30D540/0x30D548` tests bit0 on both levels (both set ⇒ return −1); landing check `0x304F5E test …+6,2`; combined `0x27DFA2 test …+6,3`; boolean extractor `0x0D9D6C mov al,[rcx+rax*8]; shr al,1; and al,1` (base `0xD8DCB6` = +6) |
| +7 | unknown (never seen read) | — | |

This matches the classic PSX/GNS 8-byte tile record in spirit (terrain, height, slope height +
depth, slope shape, walk/select flags) with two IVC re-encodings: depth is 3 bits (classic:
nibble) and the slope-type enum became explicit per-corner 2-bit weights in byte +4.

## 3. Map-dimension and cursor globals

- `0x140C6AD6A` (byte) = **map width** — X bound in the helper (`0x30D4FA`), row multiplier in
  every index computation (`imul y, width`). ~120 real-code readers.
- `0x140C6AD6B` (byte) = **map height** — Y bound (`0x30D50C`), usually read as the paired loop
  bound. The two bytes are also written together at `0x1D5C2A/0x1D5C36` (map-load path candidate).
- The surrounding block `0x140C6AD40..0xC6ADC0` is a heavily-referenced battle-map global cluster
  (the known current-actor selector `0x140C6AD8C` sits in it too). **Hypothesis**: per-battle map
  header copied at load.
- **Current/selected-tile globals** (Hypothesis, from `0x27CD71..0x27CDB8`): word tile index at
  `0x1418827B2`, pointer to its 8-byte record at `0x141882780`, pointer to its 5-byte selection
  cell at `0x141882790` — written when a tile is selected/cursored; useful as a "cursor tile"
  read if live-confirmed.

## 4. The selection/path workspace (second per-tile array)

`0x1417F04E0`, stride **5 bytes/tile**, level-1 at **+0x500** (= 256×5), total 0xA00:

- `0x27EF71..0x27EFC9` (AoE/height-tolerance loop): for each candidate tile and each of the 2
  levels, computes the tile's effective height from the 8-byte table, applies a height-band test,
  and writes `selmap[idx*5] = 1` when the tile qualifies (`0x27EFB7 mov byte[r11],1`,
  `add r13,0x800 / add r11,0x500` per level).
- `0x280DE1..0x280E18` (**occupancy**): recomputes a unit's tile index directly from the unit
  array (`[0x1853D2F/30/31]` = unit base `0x141853CE0` + `0x4F/0x50/0x51` — X, Y, level-bit) and
  zeroes that tile's selection cell — i.e. tiles under units are knocked out of the valid set.
  **Occupancy is derived from unit structs, not stored in the terrain record** (no occupant-id
  field found; scanning units per tile is the engine's own model).
- `0x280D78 mov byte[rcx+rbx+1],1` writes cell byte +1 (second flag lane).

**Hypothesis**: this is the move/target-range workspace (the blue/red tile set). If confirmed
live, DCL gets both *reach membership* (read cell after selection opens) and even *reach
authoring* (write cells — ordinary data).

## 5. Ties into already-proven combat surfaces

- The corner-height routine (`0x304FD8` neighborhood, site `0x30506B`) loads its unit from
  **`0x14186AF68` — the proven current apply-target global** — and compares target-tile corner
  heights against a neighbor tile: vertical checks in the resolution path run over this exact
  table.
- The walk/landing validator (`0x304F3E..0x304F7B`) sits inside the `0x304DF0` magic-avoidance
  function already documented in `05` §11 — same function family the DCL hooks.
- The position helper `0x30D484` (produces caster `+0x1AC/+0x1AE/+0x1B0`) is the canonical
  consumer: bounds-check against the dim bytes, two-level flag test, nearest-level select by
  height — its out-param 3 is the resolved **level** of the target tile.

## 6. Minimal live-test plan (user GUI + probe reads only)

1. **Table identity**: in any battle, dump `0x140D8DCB0..+0x1000` plus bytes `0xC6AD6A/6B`.
   Expect: `width*height <= 256` sane (e.g. 10–16 per axis); records beyond `y >= height` zeroed.
2. **Height ground truth**: for 2–3 units standing at visibly different elevations, compute
   `idx = (unit[0x51]>>7)<<8 | unit[0x50]*width + unit[0x4F]` and read `tile[idx*8+2]`
   (and `+3/+4`). Expect byte+2 to order the units by visible height; a unit on a slope shows
   nonzero `b3&0x1F` and per-corner bits in `b4`. This calibrates height units (half-tiles: the
   formula `2*b2 + w*slopeH` suggests quarter-ish granularity — verify against the in-game "h"
   number shown on the cursor).
3. **Two-level tile**: stand a unit under and on a bridge; expect the two units to differ only in
   the `+0x51` bit7 and to map to `idx` vs `idx+0x100`.
4. **Dynamic byte+5**: open an AoE target selection and dump byte+5 of all 512 records before /
   during / after; expect 0x40 (and/or 0x80) set exactly on the highlighted tiles — if so, **AoE
   membership becomes a direct memory read**.
5. **Selection map**: same moment, dump `0x1417F04E0..+0xA00`; expect nonzero cells = the shown
   range, and zeroed cells under occupied tiles (occupancy check).
6. **Write test (later)**: flip a tile's `+6` bit1 before opening Move — the tile should become
   unreachable (would prove the table is the live authority, like the evade bytes).

## 7. Open questions

- Bytes +1 and +7 of the record; bits 6–7 of byte+0; full meaning of byte+5's bit set.
- Whether the table is rebuilt per map load at the same static address (expected — it is a
  static-address global, like the unit array) and how ramps between levels are encoded.
- The exact in-game height unit (byte+2 raw vs the displayed `h` value): one calibration read.
- Terrain-type enum values (0x12/0x1C are rejected-for-landing ids — water candidates).

## Sources / method

- Disasm: pefile+capstone over `FFT_enhanced.exe` on disk, `.xcode` RVA `0x1000..0x610000`;
  RIP-displacement brute scan (every dword window as a disp32) for targets
  `0xD8DCB0±window`, `0xC6AD40..0xC6ADD0`, `0x17F04E0±window`. Script `work/disasm_tilemap.py`,
  output `work/disasm_tilemap.out.txt`.
- Prior anchors: `work/dcl-action-id-candidates.md` §4.4 (helper `0x30D484`, map dims),
  `work/dcl-unit-state-candidates.md` §2 (X/Y/facing, tier expectation).
- Classic GNS tile record (FFHacktics/GaneshaDx community knowledge): 8-byte record with terrain,
  height (half-tiles), slope height+depth, slope type, walk/select flags — used as the shape
  prior; matched field-for-field except the two re-encodings noted in §2.
- Cheat table `FFT_enhanced.CT`: no map/tile/height entries (checked; only an "Ignore Terrain"
  movement-flag script).
