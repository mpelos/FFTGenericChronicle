#!/usr/bin/env python3
"""Find real-code unit/equipment reads relevant to Dual Wield active-hand identity.

The scan decodes padded real-code functions in the combat-heavy RVA range and groups
memory operands by function/base register. A candidate must read a word at unit-like
+0x20 or +0x24; nearby unit-stat/equipment offsets are retained as corroboration.
"""
from __future__ import annotations

import re
import sys
from collections import defaultdict

from capstone import CS_AC_READ, CS_AC_WRITE
from capstone.x86 import X86_OP_MEM

sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import BASE, DATA, EXSECS, MD, REAL_MAX, hb  # noqa: E402


FIELDS = {
    0x00: "charId",
    0x03: "jobId",
    0x20: "rightWeapon",
    0x22: "rightShield",
    0x24: "leftWeapon",
    0x26: "leftShield",
    0x29: "level",
    0x38: "rawPa",
    0x39: "rawMa",
    0x3A: "rawSpeed",
    0x3E: "pa",
    0x3F: "ma",
    0x44: "weaponAtkR",
    0x45: "weaponAtkL",
    0x46: "weaponParryR",
    0x47: "weaponParryL",
    0x4A: "shieldPhysParry",
    0x4B: "physEva",
}


def iter_functions():
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
                yield sec_rva + lo + cursor, region[cursor:end]
            cursor = pad.end()
        if cursor < len(region):
            yield sec_rva + lo + cursor, region[cursor:]


def main() -> int:
    candidates = []
    for head, code in iter_functions():
        refs = []
        for ins in MD.disasm(code, BASE + head):
            for op in ins.operands:
                if op.type != X86_OP_MEM or op.mem.disp not in FIELDS:
                    continue
                if op.mem.base == 0:
                    continue
                access = ""
                if op.access & CS_AC_READ:
                    access += "R"
                if op.access & CS_AC_WRITE:
                    access += "W"
                refs.append((
                    ins.address - BASE,
                    ins,
                    MD.reg_name(op.mem.base),
                    op.mem.disp,
                    op.size,
                    access or "?",
                ))
        word_weapons = [row for row in refs if row[3] in (0x20, 0x24) and row[4] == 2]
        if not word_weapons:
            continue
        fields = {row[3] for row in refs}
        score = len(fields & {0x20, 0x22, 0x24, 0x26}) * 4 + len(fields & set(FIELDS))
        candidates.append((score, head, refs))

    print(f"image_base=0x{BASE:X} candidates={len(candidates)}")
    for score, head, refs in sorted(candidates, reverse=True):
        fields = sorted({row[3] for row in refs})
        print(f"\n## fn=0x{head:X} score={score} fields=" + ",".join(f"+0x{x:X}:{FIELDS[x]}" for x in fields))
        for rva, ins, base, disp, size, access in refs:
            print(
                f"0x{rva:X} [{access}] base={base} disp=+0x{disp:X} size={size} "
                f"{hb(ins.bytes):<28} {ins.mnemonic} {ins.op_str}"
            )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
