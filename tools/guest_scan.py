#!/usr/bin/env python3
"""Scan all ENTD files for NAMED ALLY/GUEST slots and report their level encoding.

Ally vs enemy is read from byte 0x18 bit 0x10 (set = enemy/Red team, clear = ally/guest/player).
A slot is 'named' if char id (byte 0x02) is not 0x00/0xFF.
Level byte is at 0x03. Values: 1-99 = fixed (scalable guest target), 254 = default/save, >=100 = already relative.
"""
from pathlib import Path
ENTRY=0x280; SLOT=0x28
BASE=Path("extracted/enhanced_0002_selected/fftpack")
FILES=[("entd1",1),("entd2",2),("entd3",3),("entd4",4)]

def kind_of(rec):
    cid=rec[0x02]
    if cid in (0x00,0xFF): return "enemy/generic"
    enemy = bool(rec[0x18] & 0x10)
    return "named-enemy" if enemy else "named-ally"

rows=[]
for name,no in FILES:
    p=BASE/f"battle_{name}_ent.bin"
    if not p.exists(): continue
    data=p.read_bytes()
    for local in range(128):
        g=(no-1)*128+local; b=local*ENTRY
        for s in range(16):
            rec=data[b+s*SLOT:b+s*SLOT+SLOT]
            if not any(rec): continue
            if rec[0x02] in (0x00,0xFF): continue
            if rec[0x18] & 0x10: continue   # enemy
            rows.append((g,name,s,rec[0x02],rec[0x03],rec[0x0A]))

# Only ally/guest named slots with a FIXED numeric level (scalable targets)
print("=== NAMED ALLY/GUEST slots with FIXED level (1-99) -> scalable guests ===")
print("entry file  slot charId lvl job")
fixed=[r for r in rows if 1<=r[4]<=99]
for g,name,s,cid,lvl,job in fixed:
    print(f"{g:4d} {name} {s:2d}  0x{cid:02X}   {lvl:3d} 0x{job:02X}")
print(f"\nTotal scalable-guest occurrences: {len(fixed)}")

# Distinct ally charIds and how their levels are encoded
from collections import defaultdict
enc=defaultdict(lambda:defaultdict(int))
for g,name,s,cid,lvl,job in rows:
    bucket = "fixed(1-99)" if 1<=lvl<=99 else ("default(254)" if lvl==254 else ("relative(>=100)" if lvl>=100 else f"other({lvl})"))
    enc[cid][bucket]+=1
print("\n=== ally charId -> level-encoding histogram ===")
for cid in sorted(enc):
    print(f"0x{cid:02X}: "+", ".join(f"{k}={v}" for k,v in sorted(enc[cid].items())))
