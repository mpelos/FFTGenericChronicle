#!/usr/bin/env python3
"""Offline RE: AI-anchor call-graph pass.

Stage 1:
  - scan .xcode-range (RVA 0x1000..0x610000) for E8 rel32 calls whose target
    is inside the range -> caller map + approximate function-head set
  - report callers of key functions:
      0x304DF0 magic-accuracy, 0x3065F0 status-proc, 0x278EE0 roll,
      0x30BCF8/0x30BC3C staging helpers, 0x2BB0D4 ability-row resolver (VM thunk)
  - find enclosing function heads for known sites:
      finalizers 0x30637E 0x308D8F 0x307DC4 0x309664,
      sub-sites 0x307B32 0x307C06 (call 0x304DF0), 0x307DE8/0x307E1C/0x307E80/0x307E94
      roll-verdict 0x30F4A7 / 0x30F49C / category producer 0x30F0C4
      pre-clamp 0x30A66F
  - then callers-of-callers for the enclosing functions (2 levels up)
"""
import sys
from collections import defaultdict
from pathlib import Path

import pefile

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
LO, HI = 0x1000, 0x610000
BASE = 0x140000000

pe = pefile.PE(str(EXE), fast_load=True)
data = pe.__data__
secs = [(s.VirtualAddress, s.VirtualAddress + s.Misc_VirtualSize,
         s.PointerToRawData, s.SizeOfRawData, s.Name.rstrip(b"\0").decode())
        for s in pe.sections]

def rva_to_off(rva):
    for va, vend, raw, rawsz, name in secs:
        if va <= rva < vend and (rva - va) < rawsz:
            return raw + (rva - va)
    return None

# pull the whole real-code range into one blob (assume contiguous mapping)
off_lo = rva_to_off(LO)
blob = data[off_lo: off_lo + (HI - LO)]

# --- scan E8 rel32 ---
callers = defaultdict(list)   # target_rva -> [callsite_rva]
i = 0
n = len(blob)
while True:
    j = blob.find(b"\xE8", i)
    if j < 0 or j + 5 > n:
        break
    i = j + 1
    rel = int.from_bytes(blob[j+1:j+5], "little", signed=True)
    src = LO + j
    tgt = src + 5 + rel
    if LO <= tgt < HI:
        callers[tgt].append(src)

heads = sorted(callers.keys())
print(f"E8 targets in range: {len(heads)}; total callsites: {sum(len(v) for v in callers.values())}")

import bisect
def enclosing(rva):
    """approx function head: nearest E8-called head <= rva"""
    k = bisect.bisect_right(heads, rva) - 1
    return heads[k] if k >= 0 else None

KEY = {
    0x304DF0: "magic-accuracy fn",
    0x3065F0: "status-proc fn",
    0x278EE0: "roll(range,chance)",
    0x30BCF8: "staging helper A",
    0x30BC3C: "staging helper B",
    0x2BB0D4: "ability-row resolver (VM thunk)",
    0x30D484: "position helper",
    0x30B584: "reaction dispatcher",
    0x30BE54: "brave gate 1", 0x30BEAC: "brave gate 2",
    0x30BEFC: "brave gate 3", 0x30BF48: "brave gate 4",
    0x2832F8: "order-record setter",
    0x30FA34: "VM roll thunk",
    0x30F900: "apply call (from verdict branch)",
}
print("\n=== callers of key functions ===")
for t, label in sorted(KEY.items()):
    cs = callers.get(t, [])
    encl = sorted({enclosing(c) for c in cs})
    print(f"0x{t:06X} {label}: {len(cs)} callsites")
    for c in sorted(cs)[:30]:
        print(f"    callsite 0x{c:06X}  (in fn ~0x{enclosing(c):06X})")

SITES = [0x30637E, 0x308D8F, 0x307DC4, 0x309664, 0x307B32, 0x307C06,
         0x307DE8, 0x307E1C, 0x307E80, 0x307E94, 0x30F4A7, 0x30F49C,
         0x30F0C4, 0x30A66F, 0x304E33, 0x306636, 0x30F8C7, 0x3062EC,
         0x30AA80, 0x309687, 0x3096A3]
print("\n=== enclosing fn heads of known sites ===")
encl_fns = {}
for s in SITES:
    e = enclosing(s)
    encl_fns[s] = e
    print(f"site 0x{s:06X} -> fn ~0x{e:06X}")

print("\n=== callers of those enclosing fns (level up) ===")
seen = set()
for s, e in sorted(encl_fns.items(), key=lambda kv: kv[1]):
    if e in seen:
        continue
    seen.add(e)
    cs = callers.get(e, [])
    print(f"fn 0x{e:06X} (encloses site 0x{s:06X}): {len(cs)} callers")
    for c in sorted(cs)[:25]:
        print(f"    from 0x{c:06X}  (in fn ~0x{enclosing(c):06X})")
#!/usr/bin/env python3
"""Stage 3:
- verify fn head 0x307178 prologue + first instructions (is it the formula dispatcher?)
- full disasm fn 0x309A44 (calls into formula family from outside) + its call list
- find enclosing function of callsite 0x281F85 (scan back for int3 run) and disasm
- disasm around 0x307F68 (internal caller of 0x309A44)
- grep disasm of key regions for byte [reg+4] team reads and cmp
- check who calls fn 0x307060 / 0x3070BC (wrappers calling 0x3062EC)
- phase-2 charge-resolution block 0x30F8C1..0x30F9xx full disasm
"""
import bisect
from collections import defaultdict
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
LO, HI = 0x1000, 0x610000
BASE = 0x140000000

pe = pefile.PE(str(EXE), fast_load=True)
data = pe.__data__
secs = [(s.VirtualAddress, s.VirtualAddress + s.Misc_VirtualSize,
         s.PointerToRawData, s.SizeOfRawData, s.Name.rstrip(b"\0").decode())
        for s in pe.sections]

def rva_to_off(rva):
    for va, vend, raw, rawsz, name in secs:
        if va <= rva < vend and (rva - va) < rawsz:
            return raw + (rva - va)
    return None

off_lo = rva_to_off(LO)
blob = data[off_lo: off_lo + (HI - LO)]

md = Cs(CS_ARCH_X86, CS_MODE_64)

def find_head(rva):
    """scan back for >=2 consecutive int3 (or ret+int3) then next non-int3 byte"""
    off = rva_to_off(rva)
    k = off
    while k > off - 0x3000:
        if data[k] == 0xCC and data[k-1] == 0xCC:
            # head = first non-CC after
            h = k + 1
            while data[h] == 0xCC:
                h += 1
            return rva - (off - h)
        k -= 1
    return None

def disasm(start, maxlen=0x1000, stop_ret_pad=True):
    off = rva_to_off(start)
    out = []
    for ins in md.disasm(data[off: off + maxlen], BASE + start):
        rva = ins.address - BASE
        out.append((rva, ins.mnemonic, ins.op_str))
        if ins.mnemonic == "int3":
            break
        if stop_ret_pad and ins.mnemonic == "ret":
            if data[rva_to_off(rva + ins.size)] in (0xCC,):
                break
    return out

def show(label, start, maxlen=0x1000, marks=()):
    print("=" * 90)
    print(f"{label} @ 0x{start:06X}")
    print("=" * 90)
    for rva, mn, op in disasm(start, maxlen):
        mark = ">>" if rva in marks else "  "
        print(f" {mark} 0x{rva:06X}  {mn:8s} {op}")
    print()

# 1. fn 0x307178 head
show("FN 0x307178 head (formula dispatcher?) first 0x180 bytes", 0x307178, 0x180)

# 2. fn 0x309A44 full
show("FN 0x309A44 (external-called formula driver)", 0x309A44, 0x800)

# 3. enclosing fn of 0x281F85
h = find_head(0x281F85)
print(f"callsite 0x281F85 -> function head found at 0x{h:06X}\n")
show(f"FN 0x{h:06X} (contains call 0x309A44 at 0x281F85)", h, 0x900, marks=(0x281F85,))

# 4. context of 0x307F68
show("context 0x307F00..0x308000 (internal caller of 0x309A44)", 0x307EF0, 0x140, )

# 5. callers of 0x307060 / 0x3070BC
targets = {0x307060, 0x3070BC, 0x2059AC, 0x228A08}
calls = defaultdict(list)
i = 0
n = len(blob)
while True:
    j = blob.find(b"\xE8", i)
    if j < 0 or j + 5 > n:
        break
    i = j + 1
    rel = int.from_bytes(blob[j+1:j+5], "little", signed=True)
    tgt = LO + j + 5 + rel
    if tgt in targets:
        calls[tgt].append(LO + j)
print("callers:")
for t in sorted(targets):
    print(f"  0x{t:06X}: " + (", ".join(f"0x{s:06X}" for s in sorted(calls[t])) if calls[t] else "(none)"))
print()

# 6. phase-2 charge resolution block
show("phase-2 charge-resolution 0x30F8C1..", 0x30F8C1, 0x180)

# 7. team-byte scan: disasm regions and report insns touching [reg+4] byte
print("=" * 90)
print("byte [reg+4] accesses in regions of interest")
REGIONS = [(0x281B00, 0x2833C0), (0x304580, 0x304C00), (0x307060, 0x309F00),
           (0x30F0C4, 0x30FA30), (0x30A51C, 0x30AC10)]
import re
pat = re.compile(r"\[r[a-z0-9]+ \+ 4\]$")
for lo, hi2 in REGIONS:
    off = rva_to_off(lo)
    for ins in md.disasm(data[off: off + (hi2 - lo)], BASE + lo):
        if ins.op_str.endswith("+ 4]") and ("byte ptr" in ins.op_str):
            print(f"  0x{ins.address - BASE:06X}  {ins.mnemonic:8s} {ins.op_str}")
#!/usr/bin/env python3
"""Stage 4:
- E8 callers of 0x281D60 (target-sweep result driver), 0x2827CC (target enumerator),
  0x309960 (pre-sweep init), 0x281ADC, 0x281C9C, 0x30A4A4 (record clear), 0x306864,
  0x30A1B0, 0x30B080
- disasm windows around each caller of 0x281D60 with enclosing fn head
- fixed team-byte scan: any '+ 4]' memory operand in regions of interest
- disasm fn 0x228A08 head (forecast UI builder?) to see if it calls 0x281D60
"""
from collections import defaultdict
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
LO, HI = 0x1000, 0x610000
BASE = 0x140000000

pe = pefile.PE(str(EXE), fast_load=True)
data = pe.__data__
secs = [(s.VirtualAddress, s.VirtualAddress + s.Misc_VirtualSize,
         s.PointerToRawData, s.SizeOfRawData, s.Name.rstrip(b"\0").decode())
        for s in pe.sections]

def rva_to_off(rva):
    for va, vend, raw, rawsz, name in secs:
        if va <= rva < vend and (rva - va) < rawsz:
            return raw + (rva - va)
    return None

off_lo = rva_to_off(LO)
blob = data[off_lo: off_lo + (HI - LO)]
md = Cs(CS_ARCH_X86, CS_MODE_64)

def find_head(rva):
    off = rva_to_off(rva)
    k = off
    while k > off - 0x4000:
        if data[k] == 0xCC and data[k-1] == 0xCC:
            h = k + 1
            while data[h] == 0xCC:
                h += 1
            return rva - (off - h)
        k -= 1
    return None

def disasm(start, maxlen=0x800):
    off = rva_to_off(start)
    out = []
    for ins in md.disasm(data[off: off + maxlen], BASE + start):
        rva = ins.address - BASE
        out.append((rva, ins.mnemonic, ins.op_str))
        if ins.mnemonic == "int3":
            break
        if ins.mnemonic == "ret" and data[rva_to_off(rva + ins.size)] == 0xCC:
            break
    return out

TARGETS = {0x281D60: "target-sweep driver", 0x2827CC: "target enumerator",
           0x309960: "pre-sweep init", 0x281ADC: "helper A", 0x281C9C: "helper B",
           0x306864: "post helper", 0x30A1B0: "post helper 2", 0x30B080: "post helper 3",
           0x228A08: "fn calling reaction dispatcher"}
calls = defaultdict(list)
i, n = 0, len(blob)
while True:
    j = blob.find(b"\xE8", i)
    if j < 0 or j + 5 > n:
        break
    i = j + 1
    rel = int.from_bytes(blob[j+1:j+5], "little", signed=True)
    tgt = LO + j + 5 + rel
    if tgt in TARGETS:
        calls[tgt].append(LO + j)

print("callers:")
for t, label in sorted(TARGETS.items()):
    cs = calls.get(t, [])
    heads = [find_head(c) for c in cs]
    print(f"  0x{t:06X} {label}:")
    for c, h in zip(sorted(cs), heads):
        print(f"      callsite 0x{c:06X} in fn 0x{h:06X}" if h else f"      callsite 0x{c:06X}")
print()

# disasm window +-0x60 around each caller of 0x281D60 and 0x2827CC
for t in (0x281D60, 0x2827CC):
    for c in sorted(calls.get(t, [])):
        print("=" * 90)
        print(f"context around callsite 0x{c:06X} -> 0x{t:06X}")
        off = rva_to_off(c - 0x60)
        for ins in md.disasm(data[off: off + 0xC0], BASE + c - 0x60):
            rva = ins.address - BASE
            mark = ">>" if rva == c else "  "
            print(f" {mark} 0x{rva:06X}  {ins.mnemonic:8s} {ins.op_str}")
        print()

# fixed team-byte scan
print("=" * 90)
print("any '+ 4]' memory operand in regions of interest (team byte unit+0x04)")
REGIONS = [(0x281900, 0x2833C0), (0x304580, 0x304C00), (0x307060, 0x30A050),
           (0x30F0C4, 0x30FA30), (0x30A51C, 0x30AC10), (0x2059AC, 0x206400),
           (0x20BFD4, 0x20C400), (0x227E00, 0x229D40)]
for lo, hi2 in REGIONS:
    off = rva_to_off(lo)
    for ins in md.disasm(data[off: off + (hi2 - lo)], BASE + lo):
        if "+ 4]" in ins.op_str:
            print(f"  0x{ins.address - BASE:06X}  {ins.mnemonic:8s} {ins.op_str}")
#!/usr/bin/env python3
"""Stage 5:
- dump formula dispatch table at RVA 0x682BC8 (qword fn ptrs indexed by formula id)
- disasm 0x309960 (pre-sweep init) to see ability-id global writes
- byte-signature uniqueness for hook heads 0x281D60 / 0x309A44 / 0x309960
- disasm 0x2827CC head (target enumerator) briefly
"""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
pe = pefile.PE(str(EXE), fast_load=True)
data = pe.__data__
secs = [(s.VirtualAddress, s.VirtualAddress + s.Misc_VirtualSize,
         s.PointerToRawData, s.SizeOfRawData, s.Name.rstrip(b"\0").decode())
        for s in pe.sections]

def rva_to_off(rva):
    for va, vend, raw, rawsz, name in secs:
        if va <= rva < vend and (rva - va) < rawsz:
            return raw + (rva - va)
    return None

md = Cs(CS_ARCH_X86, CS_MODE_64)

# 1. formula dispatch table
print("=== formula dispatch table @ RVA 0x682BC8 (VA 0x140682BC8) ===")
off = rva_to_off(0x682BC8)
from collections import Counter
tgts = Counter()
entries = []
for idx in range(0x100):
    q = int.from_bytes(data[off + idx*8: off + idx*8 + 8], "little")
    rva = q - BASE
    if not (0x1000 <= rva < 0x610000):
        print(f"  table ends at index 0x{idx:02X} (value 0x{q:X})")
        break
    entries.append((idx, rva))
    tgts[rva] += 1
print(f"  {len(entries)} entries")
for idx, rva in entries:
    print(f"  formula 0x{idx:02X} ({idx:3d}) -> 0x{rva:06X}")
print("\n  distinct handlers:", len(tgts))
for rva, cnt in sorted(tgts.items()):
    print(f"    0x{rva:06X} used by {cnt} formula ids")

# 2. disasm 0x309960
print("\n=== FN 0x309960 (pre-sweep init) ===")
off = rva_to_off(0x309960)
for ins in md.disasm(data[off: off + 0x120], BASE + 0x309960):
    rva = ins.address - BASE
    print(f"  0x{rva:06X}  {ins.mnemonic:8s} {ins.op_str}")
    if ins.mnemonic == "int3":
        break
    if ins.mnemonic == "ret" and data[rva_to_off(rva + ins.size)] == 0xCC:
        break

# 3. signature uniqueness
print("\n=== hook-head signature uniqueness ===")
for head, nbytes in ((0x281D60, 24), (0x309A44, 32), (0x309960, 24), (0x2827CC, 24)):
    o = rva_to_off(head)
    sig = data[o:o+nbytes]
    cnt = data.count(sig)
    print(f"  0x{head:06X}: first {nbytes} bytes {' '.join(f'{b:02X}' for b in sig)}  -> {cnt} match(es) in file")

# 4. 0x2827CC head
print("\n=== FN 0x2827CC (target enumerator) head ===")
off = rva_to_off(0x2827CC)
for ins in md.disasm(data[off: off + 0x100], BASE + 0x2827CC):
    print(f"  0x{ins.address - BASE:06X}  {ins.mnemonic:8s} {ins.op_str}")
