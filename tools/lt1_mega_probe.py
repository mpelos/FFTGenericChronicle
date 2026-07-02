#!/usr/bin/env python3
"""LT1 mega-probe: read-only, maximum-information live observer for one battle.

Watches, at ~40 Hz, with per-change (delta) logging:
  - the current-action global block 0x14186AF60..F8 (ability id, caster index, ptrs)
  - the forecast-object global 0x142FF3CF8
  - the staged status hit-% global g_7B07AC (0x1407B07AC)
  - every valid unit slot (table 0x141853CE0, stride 0x200): identity, HP/MP,
    turn markers (+0x1B8/+0x1BA), position/facing (+0x4F/50/51), all four status
    arrays (+0x57/+0x5C/+0x61/+0x1EF) with classic-bit decode, duration block,
    pending record (+0x18D/+0x1A1/+0x1A2/+0x1AA/+0x1AC/+0x1AE/+0x1B0),
    staged result fields (+0x1A8/+0x1C0/+0x1C4/+0x1C6/+0x1C8/+0x1CA/+0x1D0/+0x1D8/+0x1E5/+0x1EA),
    JP arrays (+0xF0/+0x11E) and job-level nibbles (+0xE4..0xEE)
  - any OTHER changed byte in the 0x200 struct is reported as an unknown-offset
    delta (auto-muted if it turns out to be frame-noise), so the test also
    DISCOVERS fields we did not predict.

Also appends a raw JSONL record (full 0x200 hex) for every unit change event,
so everything can be re-analyzed after the battle without re-running it.

Read-only: opens the process with PROCESS_VM_READ only. Never writes.

Usage:  python tools/lt1_mega_probe.py            (Ctrl+C to stop)
        python tools/lt1_mega_probe.py --duration 900
"""
from __future__ import annotations

import argparse
import ctypes
import json
import struct
import sys
import time
from collections import defaultdict, deque
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_live_unit_pointers import CloseHandle, OpenProcess, ReadProcessMemory, find_pid  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
LOG_PATH = ROOT / "work" / "lt1_mega_probe.log"
RAW_PATH = ROOT / "work" / "lt1_mega_probe.raw.jsonl"

UNIT_TABLE = 0x141853CE0
STRIDE = 0x200
SLOTS = 24

ACTION_BLOCK = 0x14186AF60          # ptrs at +0/ +8/ +0x10/ +0x18
ACTION_ABILITY_ID = 0x14186AFF0     # u16
ACTION_CASTER_IDX = 0x14186AFF4     # u32
FORECAST_GLOBAL = 0x142FF3CF8       # qword -> target_unit+0x1BE
STATUS_PCT = 0x1407B07AC            # u32 staged status hit-%
TILE_TABLE = 0x140D8DCB0            # 8 B/tile, 256 tiles/level, 2 levels (Strong-static)
TILE_TABLE_SIZE = 0x1000
MAP_DIMS = 0x140C6AD6A              # width byte, height byte

# classic PSX status bit names (Strong-static candidates; proven bits marked *)
STATUS_BITS = {
    0: {0x80: "b0.80?", 0x40: "Crystal", 0x20: "Dead*", 0x10: "Undead*", 0x08: "Charging*",
        0x04: "Jumping", 0x02: "Defending", 0x01: "Performing"},
    1: {0x80: "Petrify", 0x40: "Invite", 0x20: "Darkness", 0x10: "Confusion", 0x08: "Silence",
        0x04: "BloodSuck", 0x02: "Cursed", 0x01: "Treasure"},
    2: {0x80: "Oil", 0x40: "Float", 0x20: "Reraise", 0x10: "Invisible", 0x08: "Berserk",
        0x04: "Chicken", 0x02: "Frog", 0x01: "Critical"},
    3: {0x80: "Poison", 0x40: "Regen", 0x20: "Protect", 0x10: "Shell", 0x08: "Haste",
        0x04: "Slow", 0x02: "Stop", 0x01: "Wall"},
    4: {0x80: "Faith", 0x40: "Innocent", 0x20: "Charm", 0x10: "Sleep", 0x08: "DontMove",
        0x04: "DontAct", 0x02: "Reflect", 0x01: "DeathSentence"},
}

# watched named fields: name -> (offset, size)  (size 1/2 little-endian unsigned)
FIELDS = {
    "charId": (0x00, 1), "slotIdx": (0x01, 1), "jobId": (0x03, 1), "team": (0x04, 1),
    "level": (0x29, 1), "brave": (0x2B, 1), "faith": (0x2D, 1),
    "hp": (0x30, 2), "maxHp": (0x32, 2), "mp": (0x34, 2), "maxMp": (0x36, 2),
    "posX": (0x4F, 1), "posY": (0x50, 1), "facing": (0x51, 1),
    "turnMark": (0x1B8, 1), "actOwner": (0x1BA, 1),
    "pendTimer": (0x18D, 1), "pendType": (0x1A1, 1), "pendAbilityId": (0x1A2, 2),
    "pendAA": (0x1AA, 1), "pendEpiX": (0x1AC, 2), "pendMid": (0x1AE, 2), "pendEpiY": (0x1B0, 2),
    "stagedAilment": (0x1A8, 2), "evadeKind": (0x1C0, 1),
    "stagedDmg": (0x1C4, 2), "stagedHeal": (0x1C6, 2),
    "stagedMpDebit": (0x1C8, 2), "stagedMpCredit": (0x1CA, 2),
    "applyMask": (0x1D0, 1), "resMeta": (0x1D8, 1), "resFlag": (0x1E5, 1), "hitPct": (0x1EA, 2),
}
STATUS_ARRAYS = {"src": 0x57, "imm": 0x5C, "eff": 0x61, "master": 0x1EF}
DURATION_BLOCK = (0x66, 0x14)       # +0x66..0x79 watched as opaque block
NIBBLES = (0xE4, 0x0B)              # job-level nibble table
JP1 = (0xF0, 23)                    # 23 u16 words
JP2 = (0x11E, 23)
NOISY_DEFAULT = {0x41}              # CT ticks constantly
AUTO_MUTE_HITS = 12                 # changes within window -> mute
AUTO_MUTE_WINDOW = 3.0              # seconds
RAW_RATE_LIMIT = 6                  # raw jsonl dumps per unit per second

log_file = None


def out(line: str) -> None:
    stamp = time.strftime("%H:%M:%S") + f".{int(time.time() * 1000) % 1000:03d}"
    text = f"{stamp} {line}"
    print(text, flush=True)
    if log_file:
        log_file.write(text + "\n")
        log_file.flush()


def read_mem(handle, address: int, size: int) -> bytes | None:
    buf = ctypes.create_string_buffer(size)
    n = ctypes.c_size_t()
    if not ReadProcessMemory(handle, ctypes.c_void_p(address), buf, size, ctypes.byref(n)):
        return None
    return bytes(buf.raw[: n.value])


def u16(b: bytes, off: int) -> int:
    return struct.unpack_from("<H", b, off)[0]


def u32(b: bytes, off: int) -> int:
    return struct.unpack_from("<I", b, off)[0]


def u64(b: bytes, off: int) -> int:
    return struct.unpack_from("<Q", b, off)[0]


def field_val(block: bytes, off: int, size: int) -> int:
    return block[off] if size == 1 else u16(block, off)


def unit_valid(block: bytes) -> bool:
    max_hp = u16(block, 0x32)
    hp = u16(block, 0x30)
    level = block[0x29]
    return 0 < max_hp < 1000 and hp <= max_hp and 1 <= level <= 99


def decode_bits(idx: int, old: int, new: int) -> str:
    names = []
    diff = old ^ new
    for bit, name in STATUS_BITS.get(idx, {}).items():
        if diff & bit:
            names.append(("+" if new & bit else "-") + name)
    return ",".join(names) if names else f"bits^{diff:02X}"


def unit_tag(block: bytes, slot: int) -> str:
    return (f"u{slot}[chr={block[0x00]:02X} job={block[0x03]:02X} team={block[0x04]}"
            f" lv={block[0x29]} hp={u16(block, 0x30)}/{u16(block, 0x32)}]")


def snapshot_line(block: bytes, slot: int) -> str:
    eff = block[0x61:0x66].hex().upper()
    master = block[0x1EF:0x1F4].hex().upper()
    return (f"  {unit_tag(block, slot)} pos=({block[0x4F]},{block[0x50]}) face={block[0x51]}"
            f" turn={block[0x1B8]}/{block[0x1BA]} eff={eff} master={master}"
            f" pend=({block[0x1A1]:02X},{u16(block, 0x1A2):04X},t={block[0x18D]})"
            f" nib={block[0xE4:0xEF].hex().upper()}")


def tile_of(block: bytes, table: bytes, width: int) -> str:
    x, y = block[0x4F], block[0x50]
    lvl = (block[0x51] >> 7) & 1
    if not width:
        return "tile=?"
    idx = (lvl << 8) + y * width + x
    if idx * 8 + 8 > len(table):
        return f"tile=({x},{y},l{lvl}) OOB"
    rec = table[idx * 8: idx * 8 + 8]
    return (f"tile=({x},{y},l{lvl}) terr={rec[0] & 0x3F:02X} h={rec[2]}"
            f" slope={rec[3] & 0x1F}/{rec[3] >> 5} corners={rec[4]:02X}"
            f" mark={rec[5]:02X} flags={rec[6]:02X}")


def dump_jp(block: bytes, slot: int) -> str:
    jp1 = [u16(block, JP1[0] + 2 * i) for i in range(JP1[1])]
    jp2 = [u16(block, JP2[0] + 2 * i) for i in range(JP2[1])]
    return f"  u{slot} JP1={jp1}\n  u{slot} JP2={jp2}"


def main() -> int:
    global log_file
    ap = argparse.ArgumentParser()
    ap.add_argument("--process-name", default="FFT_enhanced.exe")
    ap.add_argument("--interval", type=float, default=0.025)
    ap.add_argument("--duration", type=float, default=0, help="seconds; 0 = until Ctrl+C")
    ap.add_argument("--slots", type=int, default=SLOTS)
    args = ap.parse_args()

    pid = find_pid(args.process_name)
    if not pid:
        raise SystemExit(f"process not found: {args.process_name}")
    handle = OpenProcess(0x0400 | 0x0010, False, pid)  # QUERY_INFORMATION | VM_READ
    if not handle:
        raise SystemExit(f"OpenProcess failed for pid={pid}")

    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    log_file = open(LOG_PATH, "a", encoding="utf-8")
    raw_file = open(RAW_PATH, "a", encoding="utf-8")
    out(f"[START] LT1 mega-probe attached pid={pid} interval={args.interval * 1000:.0f}ms "
        f"slots={args.slots} (READ-ONLY)")

    prev_units: dict[int, bytes] = {}
    prev_glob: dict[str, int] = {}
    prev_tiles: bytes | None = None
    tiles: bytes | None = None
    width = 0
    muted: set[int] = set(NOISY_DEFAULT)
    unknown_hits: dict[int, deque] = defaultdict(lambda: deque(maxlen=AUTO_MUTE_HITS))
    raw_stamp: dict[int, deque] = defaultdict(lambda: deque(maxlen=RAW_RATE_LIMIT))
    last_snapshot = 0.0
    t0 = time.time()

    named_offsets: set[int] = set()
    for off, size in FIELDS.values():
        named_offsets.update(range(off, off + size))
    for base in STATUS_ARRAYS.values():
        named_offsets.update(range(base, base + 5))
    named_offsets.update(range(DURATION_BLOCK[0], DURATION_BLOCK[0] + DURATION_BLOCK[1]))
    named_offsets.update(range(NIBBLES[0], NIBBLES[0] + NIBBLES[1]))
    named_offsets.update(range(JP1[0], JP1[0] + 2 * JP1[1]))
    named_offsets.update(range(JP2[0], JP2[0] + 2 * JP2[1]))

    try:
        while True:
            now = time.time()
            if args.duration and now - t0 > args.duration:
                break

            # ---- globals ----
            blk = read_mem(handle, ACTION_BLOCK, 0x20)
            idb = read_mem(handle, ACTION_ABILITY_ID, 8)
            fcg = read_mem(handle, FORECAST_GLOBAL, 8)
            spb = read_mem(handle, STATUS_PCT, 4)
            if idb:
                glob = {
                    "abilityId": u16(idb, 0), "casterIdx": u32(idb, 4),
                    "p60": u64(blk, 0x00) if blk else 0, "p68": u64(blk, 0x08) if blk else 0,
                    "p70": u64(blk, 0x10) if blk else 0, "p78": u64(blk, 0x18) if blk else 0,
                    "forecastObj": u64(fcg, 0) if fcg else 0,
                    "statusPct": u32(spb, 0) if spb else 0,
                }
                for key, val in glob.items():
                    if prev_glob.get(key) != val:
                        old = prev_glob.get(key)
                        if key in ("abilityId", "casterIdx", "statusPct"):
                            out(f"[GLOBAL] {key} {old if old is not None else '?'} -> {val}"
                                + (f" (0x{val:04X})" if key == "abilityId" else ""))
                        else:
                            rel = val - 0x1BE - UNIT_TABLE
                            unit_note = (f" == unit[{rel // STRIDE}]+0x1BE"
                                         if val and rel >= 0 and rel % STRIDE == 0
                                         and rel // STRIDE < args.slots else "")
                            out(f"[GLOBAL] {key} 0x{(old or 0):X} -> 0x{val:X}{unit_note}")
                        prev_glob[key] = val

            # ---- tile table ----
            dims = read_mem(handle, MAP_DIMS, 2)
            width = dims[0] if dims else 0
            tiles = read_mem(handle, TILE_TABLE, TILE_TABLE_SIZE)
            if tiles:
                if prev_tiles is None:
                    out(f"[MAP] dims={dims[0]}x{dims[1]}" if dims else "[MAP] dims unreadable")
                elif tiles != prev_tiles:
                    only5_set, only5_clr, other = [], [], []
                    for i in range(0, TILE_TABLE_SIZE, 8):
                        if tiles[i:i + 8] == prev_tiles[i:i + 8]:
                            continue
                        idx = i // 8
                        lvl, rem = idx >> 8, idx & 0xFF
                        xy = f"({rem % width},{rem // width},l{lvl})" if width else f"#{idx}"
                        rest_same = (tiles[i:i + 5] == prev_tiles[i:i + 5]
                                     and tiles[i + 6:i + 8] == prev_tiles[i + 6:i + 8])
                        if rest_same:
                            (only5_set if tiles[i + 5] > prev_tiles[i + 5] else only5_clr).append(
                                f"{xy}{prev_tiles[i + 5]:02X}->{tiles[i + 5]:02X}")
                        else:
                            other.append(f"{xy} {prev_tiles[i:i + 8].hex()}->{tiles[i:i + 8].hex()}")
                    if only5_set:
                        out(f"[TILES] mark+ x{len(only5_set)}: " + " ".join(only5_set[:20])
                            + (" ..." if len(only5_set) > 20 else ""))
                    if only5_clr:
                        out(f"[TILES] mark- x{len(only5_clr)}: " + " ".join(only5_clr[:20])
                            + (" ..." if len(only5_clr) > 20 else ""))
                    for line in other[:8]:
                        out(f"[TILES] rec {line}")
                    if len(other) > 8:
                        out(f"[TILES] rec ... +{len(other) - 8} more")
                prev_tiles = tiles

            # ---- units (one bulk read) ----
            table = read_mem(handle, UNIT_TABLE, STRIDE * args.slots)
            if table:
                for slot in range(args.slots):
                    block = table[slot * STRIDE:(slot + 1) * STRIDE]
                    if not unit_valid(block):
                        if slot in prev_units:
                            out(f"[UNIT-GONE] u{slot}")
                            del prev_units[slot]
                        continue
                    old = prev_units.get(slot)
                    if old is None:
                        out(f"[UNIT-NEW] {unit_tag(block, slot)} ptr=0x{UNIT_TABLE + slot * STRIDE:X}")
                        out(snapshot_line(block, slot))
                        if tiles:
                            out(f"  u{slot} {tile_of(block, tiles, width)}")
                        prev_units[slot] = block
                        continue
                    if old == block:
                        continue

                    changed = [i for i in range(STRIDE) if old[i] != block[i]]
                    lines: list[str] = []
                    reported: set[int] = set()

                    for name, (off, size) in FIELDS.items():
                        if any(off <= c < off + size for c in changed):
                            ov, nv = field_val(old, off, size), field_val(block, off, size)
                            if ov != nv:
                                lines.append(f"{name} {ov}->{nv}"
                                             + (f" (0x{nv:04X})" if name == "pendAbilityId" else ""))
                            reported.update(range(off, off + size))
                    for aname, base in STATUS_ARRAYS.items():
                        for i in range(5):
                            if base + i in changed:
                                lines.append(f"status.{aname}[{i}] {old[base + i]:02X}->{block[base + i]:02X}"
                                             f" [{decode_bits(i, old[base + i], block[base + i])}]")
                                reported.add(base + i)
                    dur0, dlen = DURATION_BLOCK
                    if any(dur0 <= c < dur0 + dlen for c in changed):
                        lines.append(f"durations {old[dur0:dur0 + dlen].hex()}->{block[dur0:dur0 + dlen].hex()}")
                        reported.update(range(dur0, dur0 + dlen))
                    n0, nlen = NIBBLES
                    if any(n0 <= c < n0 + nlen for c in changed):
                        lines.append(f"jobNibbles {old[n0:n0 + nlen].hex()}->{block[n0:n0 + nlen].hex()}")
                        reported.update(range(n0, n0 + nlen))
                    for label, (jbase, jn) in (("JP1", JP1), ("JP2", JP2)):
                        for i in range(jn):
                            o = jbase + 2 * i
                            if o in changed or o + 1 in changed:
                                lines.append(f"{label}[{i}] {u16(old, o)}->{u16(block, o)}")
                            reported.update((o, o + 1))

                    unknown = [c for c in changed if c not in reported and c not in muted]
                    fresh_unknown = []
                    for c in unknown:
                        unknown_hits[c].append(now)
                        h = unknown_hits[c]
                        if len(h) == AUTO_MUTE_HITS and h[-1] - h[0] < AUTO_MUTE_WINDOW:
                            muted.add(c)
                            out(f"[MUTE] unit offset +0x{c:X} is frame-noise; muted")
                        else:
                            fresh_unknown.append(c)
                    if fresh_unknown:
                        parts = [f"+0x{c:X}:{old[c]:02X}->{block[c]:02X}" for c in fresh_unknown[:16]]
                        more = f" (+{len(fresh_unknown) - 16} more)" if len(fresh_unknown) > 16 else ""
                        lines.append("unknown " + " ".join(parts) + more)
                    if tiles and any(old[o] != block[o] for o in (0x4F, 0x50, 0x51)):
                        lines.append(tile_of(block, tiles, width))

                    if lines:
                        out(f"[u{slot}|chr={block[0x00]:02X}] " + " | ".join(lines))
                        stamps = raw_stamp[slot]
                        if len(stamps) < RAW_RATE_LIMIT or now - stamps[0] > 1.0:
                            stamps.append(now)
                            raw_file.write(json.dumps({
                                "t": round(now - t0, 3), "slot": slot,
                                "changed": [f"0x{c:X}" for c in changed],
                                "block": block.hex(),
                            }) + "\n")
                            raw_file.flush()
                    prev_units[slot] = block

            if now - last_snapshot > 60 and prev_units:
                last_snapshot = now
                out(f"[SNAPSHOT] {len(prev_units)} units")
                for slot, block in sorted(prev_units.items()):
                    out(snapshot_line(block, slot))

            time.sleep(args.interval)
    except KeyboardInterrupt:
        pass
    finally:
        out(f"[END] elapsed={time.time() - t0:.0f}s; final snapshot + JP dump:")
        for slot, block in sorted(prev_units.items()):
            out(snapshot_line(block, slot))
            if tiles:
                out(f"  u{slot} {tile_of(block, tiles, width)}")
            out(dump_jp(block, slot))
        CloseHandle(handle)
        log_file.close()
        raw_file.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
