#!/usr/bin/env python3
"""DCL kind-paint analysis: who reads +0x1C0 for MESSAGE/ANIMATION, and the
call ordering between compute-point 0x281F8A, selector 0x205210, apply 0x30A66F.

Answers:
 (1) enumerate every real-code READER of unit+0x1C0, classify (message/anim vs logic).
 (2) call ordering: xrefs to 0x309A44, 0x205210, 0x30A66F; who is reachable after
     0x281F8A; where +0x1C0 is FINALLY consumed for rendering; is +0x1C0 re-written
     by the engine after the selector.
 (3) verdict inputs: companion fields (+0x1BE/+0x1C4/+0x1E5) touched near the readers.
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
SELECTOR = 0x205210
COMPUTE_CALL = 0x309A44   # call target at 0x281F85
SWEEP_CALLSITE = 0x281F85 # the call instruction
HOOK = 0x281F8A           # post-call compute point (our OUTPUT lever)
APPLY = 0x30A66F

DISPS = {0x1C0: "+1C0 kind", 0x1BE: "+1BE fcast", 0x1C4: "+1C4 dmg",
         0x1E5: "+1E5 flag", 0x1C1: "+1C1", 0x1C2: "+1C2", 0x1C3: "+1C3"}


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if not exe:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    def disasm_at(start, length=12):
        b = data[off(start): off(start) + length]
        return next(md.disasm(b, base + start), None)

    def window(rva, before, after, marks=None):
        marks = marks or {}
        b = data[off(rva - before): off(rva) + after]
        out = []
        for ins in md.disasm(b, base + (rva - before)):
            r = ins.address - base
            if r > rva + after:
                break
            t = f"{ins.mnemonic} {ins.op_str}".strip()
            low = t.lower()
            mk = ""
            for needle, label in marks.items():
                if needle in low:
                    mk = "  " + label
                    break
            out.append(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")
        return out

    print(f"exe={exe}\nbase=0x{base:X}\n")

    # ---------- xref scanner: find all E8/E9 rel32 to a target ----------
    def xrefs_to(target):
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw: praw + sz]
            n = len(blob)
            for i in range(n - 5):
                rva = va + i
                if rva >= REAL_MAX:
                    break
                op = blob[i]
                if op in (0xE8, 0xE9):
                    rel = int.from_bytes(blob[i+1:i+5], "little", signed=True)
                    dst = (rva + 5 + rel) & 0xFFFFFFFFFFFFFFFF
                    # rva-relative
                    if dst == target:
                        hits.append((rva, "call" if op == 0xE8 else "jmp"))
        return hits

    # ============================================================
    # (A) call-ordering xrefs
    # ============================================================
    print("=" * 72)
    print("(A) XREFS (E8 call / E9 jmp rel32) to key functions")
    print("=" * 72)
    for name, tgt in [("compute 0x309A44", COMPUTE_CALL),
                      ("selector 0x205210", SELECTOR),
                      ("apply 0x30A66F", APPLY)]:
        xs = xrefs_to(tgt)
        print(f"\n  --> {name}: {len(xs)} xref(s)")
        for rva, k in xs:
            print(f"       0x{rva:X}  ({k})")

    # ============================================================
    # (B) sweep driver around 0x281F85 / hook 0x281F8A
    #     show ordering: is selector called before/after the hook in this fn?
    # ============================================================
    print("\n" + "=" * 72)
    print(f"(B) SWEEP DRIVER window around callsite 0x{SWEEP_CALLSITE:X} / hook 0x{HOOK:X}")
    print("=" * 72)
    for line in window(HOOK, 0x90, 0x140, marks={
        "0x309a44": "<== COMPUTE call",
        "0x205210": "<== SELECTOR call",
        "0x30a66f": "<== APPLY call",
        "0x1c0": "(+1C0)", "0x1be": "(+1BE)", "0x1c4": "(+1C4)", "0x1e5": "(+1E5)",
    }):
        print(line)

    # ============================================================
    # (C) all real-code READERS of +0x1C0 with wide context + companion scan
    # ============================================================
    print("\n" + "=" * 72)
    print("(C) REAL-code READERS of +0x1C0 (movzx/movsx/mov r8/cmp/grp1)")
    print("=" * 72)

    def is_disp32(blob, i, val):
        return (i + 4 <= len(blob)
                and blob[i] == (val & 0xFF)
                and blob[i+1] == ((val >> 8) & 0xFF)
                and blob[i+2] == ((val >> 16) & 0xFF)
                and blob[i+3] == ((val >> 24) & 0xFF))

    read_hits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 8):
            rva = va + i
            if rva >= REAL_MAX:
                break
            j = i
            if 0x40 <= blob[j] <= 0x4F:
                j += 1
            b0 = blob[j]
            kind = None
            disp_at = None
            if b0 == 0x0F and j+2 < n and blob[j+1] in (0xB6, 0xBE):
                modrm = blob[j+2]
                if (modrm >> 6) == 0x02 and (modrm & 7) != 4:
                    disp_at = j+3; kind = "movzx/movsx r,[r+d32]"
            elif b0 == 0x8A:
                modrm = blob[j+1]
                if (modrm >> 6) == 0x02 and (modrm & 7) != 4:
                    disp_at = j+2; kind = "mov r8,[r+d32]"
            elif b0 in (0x38, 0x3A):
                modrm = blob[j+1]
                if (modrm >> 6) == 0x02 and (modrm & 7) != 4:
                    disp_at = j+2; kind = "cmp involving +1C0"
            elif b0 == 0x80:
                modrm = blob[j+1]
                if (modrm >> 6) == 0x02 and (modrm & 7) != 4:
                    disp_at = j+2; kind = "grp1 byte[r+d32],imm8"
            if kind and disp_at and is_disp32(blob, disp_at, 0x1C0):
                read_hits.append((rva, kind))
    read_hits = sorted(set(read_hits))
    print(f"\n  total +0x1C0 READ sites: {len(read_hits)}\n")
    for rva, kind in read_hits:
        ins = disasm_at(rva)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        # find enclosing function-ish: scan forward for calls in window to classify
        print(f"\n  ---- READER 0x{rva:X}: {txt}  [{kind}] ----")
        for line in window(rva, 0x10, 0x60, marks={
            "0x1c0": "<== +1C0 READ",
            "0x1c4": "(+1C4 dmg)", "0x1be": "(+1BE)", "0x1e5": "(+1E5)",
            "call": "(call)", "jmp qword": "<== JUMPTAB",
        }):
            print(line)

    # ============================================================
    # (D) selector 0x205210 body — does it WRITE +0x1C0 (re-derive)?
    #     Show writes to +0x1C0 / +0x1BE inside selector.
    # ============================================================
    print("\n" + "=" * 72)
    print(f"(D) SELECTOR 0x{SELECTOR:X} body — writes to +0x1C0/+0x1BE (re-derive check)")
    print("=" * 72)
    b = data[off(SELECTOR): off(SELECTOR) + 0x300]
    for ins in md.disasm(b, base + SELECTOR):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mk = ""
        if "0x1c0" in low:
            mk = "  <== +1C0 (write here = RE-DERIVE)" if ins.mnemonic in ("mov",) and "[" in low.split(",")[0] else "  <== +1C0"
        elif "0x1be" in low:
            mk = "  <== +1BE"
        elif "0x1c4" in low:
            mk = "  (+1C4)"
        elif "0x1e5" in low:
            mk = "  (+1E5)"
        elif ins.mnemonic == "call":
            mk = "  (call)"
        print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")
        if r - SELECTOR > 0x2E0:
            break

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
