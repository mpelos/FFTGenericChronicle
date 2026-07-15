#!/usr/bin/env python3
"""Enumerate aligned real-code xrefs to staged/effective status fields.

This is the LT13 offline follow-up to the Kiyomori capture. It byte-scans every
executable section for each displacement, then accepts a candidate only when a
Capstone decode from the preceding padded function head reaches the same
instruction boundary. This avoids the false positives produced by a single
linear sweep across embedded tables.
"""
from __future__ import annotations

import sys
import re
from collections import defaultdict

from capstone import CS_AC_READ, CS_AC_WRITE
from capstone.x86 import X86_OP_MEM

sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (  # noqa: E402
    BASE,
    DATA,
    EXSECS,
    MD,
    REAL_MAX,
    callers_of,
    disasm_win,
    find_head,
    hb,
    off,
)


FIELDS = {
    0x57: "innate/equipment status source byte 0",
    0x58: "innate/equipment status source byte 1",
    0x59: "innate/equipment status source byte 2",
    0x5A: "innate/equipment status source byte 3",
    0x5B: "innate/equipment status source byte 4",
    0x5C: "status immunity byte 0",
    0x5D: "status immunity byte 1",
    0x5E: "status immunity byte 2",
    0x5F: "status immunity byte 3",
    0x60: "status immunity byte 4",
    0x61: "effective status mirror byte 0",
    0x62: "effective status mirror byte 1",
    0x63: "effective status mirror byte 2",
    0x64: "effective status mirror byte 3",
    0x65: "effective status mirror byte 4",
    0x1A8: "staged ailment/status payload",
    0x1BE: "staged result-present flag",
    0x1C0: "staged outcome/status kind",
    0x1D0: "staged status apply mask",
    0x1E5: "result-kind bitfield",
    0x1EF: "durable status master byte 0",
    0x1F0: "durable status master byte 1",
    0x1F1: "durable status master byte 2",
    0x1F2: "durable status master byte 3",
    0x1F3: "durable status master byte 4",
}


def aligned_instruction(rva: int):
    head = find_head(rva)
    if head is None:
        return None, None
    blob = DATA[off(head):off(rva) + 16]
    for ins in MD.disasm(blob, BASE + head):
        here = ins.address - BASE
        if here == rva:
            return head, ins
        if here > rva:
            break
    return head, None


def build_field_index():
    """Decode each padded real-code run once and index tracked memory operands."""
    refs = defaultdict(dict)
    for sec_rva, sec_raw, sec_size in EXSECS:
        if sec_rva >= REAL_MAX:
            continue
        blob = DATA[sec_raw:sec_raw + sec_size]
        lo = max(0, 0x200000 - sec_rva)
        hi = min(len(blob), 0x3C0000 - sec_rva)
        if lo >= hi:
            continue
        region = blob[lo:hi]
        cursor = 0
        for pad in re.finditer(b"\xCC\xCC+", region):
            end = pad.start()
            if cursor < end:
                head = sec_rva + lo + cursor
                code = region[cursor:end]
                for ins in MD.disasm(code, BASE + head):
                    rva = ins.address - BASE
                    memops = [
                        op for op in ins.operands
                        if op.type == X86_OP_MEM and op.mem.disp in FIELDS
                    ]
                    if not memops:
                        continue
                    for disp in {op.mem.disp for op in memops}:
                        access = set()
                        for op in memops:
                            if op.mem.disp != disp:
                                continue
                            if op.access & CS_AC_READ:
                                access.add("R")
                            if op.access & CS_AC_WRITE:
                                access.add("W")
                        refs[disp][rva] = (head, ins, "".join(sorted(access)) or "?")
            cursor = pad.end()
        if cursor < len(region):
            head = sec_rva + lo + cursor
            for ins in MD.disasm(region[cursor:], BASE + head):
                rva = ins.address - BASE
                memops = [op for op in ins.operands if op.type == X86_OP_MEM and op.mem.disp in FIELDS]
                for disp in {op.mem.disp for op in memops}:
                    access = set()
                    for op in memops:
                        if op.mem.disp != disp:
                            continue
                        if op.access & CS_AC_READ:
                            access.add("R")
                        if op.access & CS_AC_WRITE:
                            access.add("W")
                    refs[disp][rva] = (head, ins, "".join(sorted(access)) or "?")
    return refs


def build_direct_call_index(targets):
    """Index direct E8 callers in one executable sweep."""
    wanted = set(targets)
    result = defaultdict(list)
    for sec_rva, sec_raw, sec_size in EXSECS:
        if sec_rva >= REAL_MAX:
            continue
        blob = DATA[sec_raw:sec_raw + sec_size]
        for pos in range(max(0, 0x200000 - sec_rva), min(len(blob) - 4, REAL_MAX - sec_rva)):
            if blob[pos] != 0xE8:
                continue
            caller = sec_rva + pos
            rel = int.from_bytes(blob[pos + 1:pos + 5], "little", signed=True)
            target = caller + 5 + rel
            if target in wanted:
                result[target].append(caller)
    return result


def main() -> int:
    print(f"image_base=0x{BASE:X} real_code<0x{REAL_MAX:X}")
    index = build_field_index()
    by_head = defaultdict(list)
    for disp, label in FIELDS.items():
        refs = sorted((rva, *value) for rva, value in index.get(disp, {}).items())
        print(f"\n## +0x{disp:X} — {label} ({len(refs)} aligned refs)")
        for rva, head, ins, access in refs:
            text = f"{ins.mnemonic} {ins.op_str}".strip()
            print(f"0x{rva:X} [{access}] fn=0x{head:X} {hb(ins.bytes):<28} {text}")
            by_head[head].append((disp, rva, access, text))

    print("\n## Functions touching two or more tracked fields")
    call_index = build_direct_call_index(by_head)
    for head, rows in sorted(by_head.items()):
        fields = sorted({row[0] for row in rows})
        if len(fields) < 2:
            continue
        field_text = ", ".join(f"+0x{x:X}" for x in fields)
        callers = ", ".join(f"0x{x:X}" for x in call_index.get(head, [])) or "none"
        print(f"fn=0x{head:X} fields={field_text} callers={callers}")
        for disp, rva, access, text in rows:
            print(f"  0x{rva:X} [{access}] +0x{disp:X} {text}")

    anchors = {
        "status roll/finalizer": (0x3065F0, 0xB0),
        "HP/MP/status apply": (0x30A51C, 0x220),
        "result selector": (0x205210, 0xC0),
        "LT10 refuted counter candidate": (0x30C798, 0x120),
        "status recompute": (0x30D420, 0x80),
    }
    marks = {f"0x{x:x}": f"+0x{x:X} {label}" for x, label in FIELDS.items()}
    for name, (rva, length) in anchors.items():
        print(f"\n## Anchor {name} @ 0x{rva:X}")
        print(disasm_win(rva, 0, length, marks))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
