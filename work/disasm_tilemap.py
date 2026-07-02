#!/usr/bin/env python3
"""Offline RE: locate the battle MAP/TILE data structure (height, slope, flags, dims).

Method:
  1. Full disasm of position helper 0x30D484 (writes caster +0x1AC/+0x1AE/+0x1B0):
     it reads unit +0x4F/+0x50 (X/Y), map dims (RIP), and indexes a static tile table.
  2. RIP-displacement brute scan of .xcode (every 4-byte window interpreted as a
     disp32 ending an instruction) for targets = tile table base +/- window and the
     map-dimension global block 0xC6AD40..0xC6ADD0.
  3. Context disasm of the best readers to fix per-field semantics.

Key results (see work/dcl-tilemap-candidates.md):
  tile table   VA 0x140D8DCB0, 8 B/tile, 256 tiles/level, 2 levels (+0x800), 0x1000 total
  index        (level<<8) + y*width + x ; unit level bit = unit+0x51 bit7
  width/height VA 0x140C6AD6A / 0x140C6AD6B (bytes)
  fields       +0 terrain(low6) +2 height +3 slopeH(low5)|depth(hi3) +4 corner-slope 2-bit*4
               +5 dynamic marks (0x40 mass-cleared @0x2099B7) +6 static flags (bit0 no-select, bit1 no-walk)
  corner height = 2*b2 + ((b4 >> 2*corner) & 3) * (b3 & 0x1F)   [fn 0x304FD8.., site 0x3050A5]
  selection/path map VA 0x1417F04E0, 5 B/tile, +0x500/level (occupancy cleared from unit X/Y/tier @0x280DE1)
"""
import struct
import re_common as rc

rc.load()
print("calib 0x30A66F:", rc.read_rva(0x30A66F, 4).hex(), "(expect 0fbf4506)")

print("\n=== position helper 0x30D484 (full, to first ret) ===")
for ins in rc.disasm(0x30D484, 0x400):
    print(rc.fmt(ins))
    if ins.mnemonic == "ret":
        break
print("\n=== level-select continuation 0x30D571 ===")
for ins in rc.disasm(0x30D571, 0x70):
    print(rc.fmt(ins))
    if ins.mnemonic == "ret":
        break

print("\n=== RIP-disp scan for tile table / map globals ===")
xs = [s for s in rc.sections() if s[4] == '.xcode'][0]
va, vs, ptr, rs, name = xs
data = rc._data
TILE = 0xD8DCB0
targets = {t: 'tile' for t in range(TILE - 0x20, TILE + 0x1010)}
targets.update({t: 'mapglob' for t in range(0xC6AD40, 0xC6ADD0)})
targets.update({t: 'selmap' for t in range(0x17F04E0 - 8, 0x17F04E0 + 0xA08)})
raw_hi = ptr + min(rs, 0x610000 - va)
hits = []
for p in range(ptr, raw_hi - 4):
    disp = struct.unpack_from('<i', data, p)[0]
    tgt = va + (p - ptr) + 4 + disp
    if tgt in targets:
        hits.append((va + (p - ptr) - 3, tgt, targets[tgt]))   # approx ins start
for site, tgt, kind in hits:
    print(f"disp@~0x{site:06X} -> 0x{tgt:06X} [{kind}]")

print("\n=== key evidence contexts ===")
for rva, label in [(0x304F3E, "walk/landing check: terrain&0x3F, b6&2, b3 depth/slope"),
                   (0x30506B, "corner-height formula from target unit [0x14186AF68]"),
                   (0x27EF7F, "AoE/height-tolerance loop + 5-stride selection map"),
                   (0x280DE1, "occupancy clear from unit +0x4F/50/51"),
                   (0x30D3DC, "b5&0x40 gate before order write (near 0x30D42A)"),
                   (0x209994, "mass-clear b5&=0xBF over 0x1000 bytes (512 tiles)"),
                   (0x0D9D62, "b6>>1&1 walkability query")]:
    print(f"--- {label} @0x{rva:06X} ---")
    for i in rc.disasm(rva - 0x10, 0x90):
        print(rc.fmt(i))
    print()
