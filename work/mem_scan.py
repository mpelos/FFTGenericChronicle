#!/usr/bin/env python3
"""External memory scanner for FFT (Denuvo) — locate the on-screen preview hit% in memory.

The VM computes the hit% but MUST materialize it in normal memory for the UI to draw it.
We find that address with a Cheat-Engine-style differential scan:

  1) verify  -> confirm external ReadProcessMemory works at all (read a known address)
  2) find    -> scan all committed RW regions for a value (the % currently on screen)
  3) filter  -> re-read prior candidates, keep those equal to the new on-screen value

Repeat find/filter across 2-3 different on-screen % values to converge to the address.

Usage (pid auto-detected from FFT_enhanced.exe if not given):
  python mem_scan.py verify [--addr 0x141855CE0] [--size 64]
  python mem_scan.py find  <value> [--types i32,i16,u8] [--out cand.json]
  python mem_scan.py filter <value> [--in cand.json] [--out cand.json]

Notes: matches small ints (e.g. 50) appear MANY times on the first 'find'; that is fine —
'filter' across changing values is what isolates the real address.
"""
from __future__ import annotations
import argparse, json, struct, sys, time
import ctypes
from ctypes import wintypes as w

k32 = ctypes.WinDLL("kernel32", use_last_error=True)

PROCESS_QUERY_INFORMATION = 0x0400
PROCESS_VM_READ = 0x0010
PROCESS_VM_WRITE = 0x0020
PROCESS_VM_OPERATION = 0x0008
MEM_COMMIT = 0x1000
PAGE_READWRITE = 0x04
PAGE_EXECUTE_READWRITE = 0x40
PAGE_WRITECOPY = 0x08
PAGE_EXECUTE_WRITECOPY = 0x80
RW_PROT = {PAGE_READWRITE, PAGE_EXECUTE_READWRITE, PAGE_WRITECOPY, PAGE_EXECUTE_WRITECOPY}
CAND_FILE = "D:/Projects/FFTGenericChronicle/work/mem_scan_candidates.json"


class MBI(ctypes.Structure):
    _fields_ = [
        ("BaseAddress", ctypes.c_ulonglong),
        ("AllocationBase", ctypes.c_ulonglong),
        ("AllocationProtect", w.DWORD),
        ("__a", w.DWORD),
        ("RegionSize", ctypes.c_ulonglong),
        ("State", w.DWORD),
        ("Protect", w.DWORD),
        ("Type", w.DWORD),
        ("__b", w.DWORD),
    ]


k32.OpenProcess.restype = w.HANDLE
k32.OpenProcess.argtypes = [w.DWORD, w.BOOL, w.DWORD]
k32.VirtualQueryEx.restype = ctypes.c_size_t
k32.VirtualQueryEx.argtypes = [w.HANDLE, ctypes.c_void_p, ctypes.POINTER(MBI), ctypes.c_size_t]
k32.ReadProcessMemory.restype = w.BOOL
k32.ReadProcessMemory.argtypes = [w.HANDLE, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
k32.WriteProcessMemory.restype = w.BOOL
k32.WriteProcessMemory.argtypes = [w.HANDLE, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]


def find_pid(name="FFT_enhanced.exe"):
    import subprocess
    out = subprocess.run(["powershell", "-NoProfile", "-Command",
                          f"(Get-Process -Name {name.replace('.exe','')} -ErrorAction SilentlyContinue).Id"],
                         capture_output=True, text=True).stdout.strip()
    return int(out.splitlines()[0]) if out else None


def openp(pid, write=False):
    access = PROCESS_QUERY_INFORMATION | PROCESS_VM_READ
    if write:
        access |= PROCESS_VM_WRITE | PROCESS_VM_OPERATION
    h = k32.OpenProcess(access, False, pid)
    if not h:
        sys.exit(f"OpenProcess failed (err={ctypes.get_last_error()}). Denuvo may block external "
                 f"{'writes' if write else 'reads'}.")
    return h


def read(h, addr, size):
    buf = (ctypes.c_char * size)()
    n = ctypes.c_size_t(0)
    ok = k32.ReadProcessMemory(h, ctypes.c_void_p(addr), buf, size, ctypes.byref(n))
    return buf.raw[: n.value] if ok else None


def writemem(h, addr, data):
    n = ctypes.c_size_t(0)
    ok = k32.WriteProcessMemory(h, ctypes.c_void_p(addr), data, len(data), ctypes.byref(n))
    return bool(ok) and n.value == len(data)


def pack_val(value, ty):
    if ty == "i32":
        return struct.pack("<i", value)
    if ty == "i16":
        return struct.pack("<h", value)
    if ty == "f32":
        return struct.pack("<f", float(value))
    return bytes([value & 0xFF])  # u8


def unpack_val(data, ty):
    if ty == "i32":
        return struct.unpack("<i", data[:4])[0]
    if ty == "i16":
        return struct.unpack("<h", data[:2])[0]
    if ty == "f32":
        return struct.unpack("<f", data[:4])[0]
    return data[0]  # u8


def regions(h):
    addr = 0
    limit = 0x7FFFFFFFFFFF
    while addr < limit:
        mbi = MBI()
        if not k32.VirtualQueryEx(h, ctypes.c_void_p(addr), ctypes.byref(mbi), ctypes.sizeof(mbi)):
            break
        if mbi.State == MEM_COMMIT and mbi.Protect in RW_PROT:
            yield mbi.BaseAddress, mbi.RegionSize
        nxt = mbi.BaseAddress + mbi.RegionSize
        if nxt <= addr:
            break
        addr = nxt


def patterns(value, types):
    pats = {}
    if "i32" in types:
        pats["i32"] = struct.pack("<i", value)
    if "i16" in types:
        pats["i16"] = struct.pack("<h", value)
    if "u8" in types:
        pats["u8"] = bytes([value & 0xFF])
    return pats


def cmd_verify(h, args):
    # Generic feasibility test: enumerate committed RW regions and read a few.
    # Confirms external ReadProcessMemory works (i.e. Denuvo does not block it).
    if args.addr:
        addr = int(args.addr[0], 16)
        data = read(h, addr, args.size)
        if data is None:
            print(f"READ FAILED at 0x{addr:X} (err={ctypes.get_last_error()}). External RPM likely blocked.")
            return
        print(f"OK read 0x{addr:X} ({len(data)} bytes): {data[:args.size].hex(' ')}")
        return
    nreg = 0
    nread = 0
    total = 0
    sample = None
    for base, size in regions(h):
        nreg += 1
        total += size
        if nread < 5:
            d = read(h, base, min(64, size))
            if d:
                nread += 1
                if sample is None:
                    sample = (base, d)
    print(f"RW regions found: {nreg}  (total {total/1e6:.0f} MB committed-RW)")
    if nread == 0:
        print("READ FAILED on all sampled regions — external RPM is BLOCKED by Denuvo. Pivot to in-process scan.")
    else:
        b, d = sample
        print(f"OK external RPM works — sample read at 0x{b:X}: {d[:32].hex(' ')}")
        print("Proceed: `find <on-screen %>` then `filter <new %>`.")


def cmd_find(h, args):
    pats = patterns(args.value, args.types.split(","))
    cands = []
    scanned = 0
    for base, size in regions(h):
        # read region in chunks
        off = 0
        CHUNK = 4 * 1024 * 1024
        while off < size:
            n = min(CHUNK, size - off)
            data = read(h, base + off, n)
            if data:
                for ty, pat in pats.items():
                    start = 0
                    while True:
                        i = data.find(pat, start)
                        if i < 0:
                            break
                        cands.append([base + off + i, ty])
                        start = i + 1
                        if len(cands) > 4_000_000:
                            print("too many matches; pick a rarer value or narrower type")
                            return
            off += n
        scanned += size
    with open(args.out, "w") as f:
        json.dump({"value": args.value, "candidates": cands}, f)
    print(f"find {args.value}: {len(cands)} matches across {scanned/1e6:.0f} MB -> {args.out}")
    by_ty = {}
    for _, ty in cands:
        by_ty[ty] = by_ty.get(ty, 0) + 1
    print("  by type:", by_ty)


def cmd_filter(h, args):
    with open(args.infile) as f:
        prev = json.load(f)["candidates"]
    keep = []
    for addr, ty in prev:
        sz = {"i32": 4, "i16": 2, "u8": 1}[ty]
        data = read(h, addr, sz)
        if data is None or len(data) < sz:
            continue
        if ty == "i32":
            v = struct.unpack("<i", data[:4])[0]
        elif ty == "i16":
            v = struct.unpack("<h", data[:2])[0]
        elif ty == "f32":
            v = struct.unpack("<f", data[:4])[0]
        else:  # u8
            v = data[0]
        if v == args.value:
            keep.append([addr, ty])
    with open(args.out, "w") as f:
        json.dump({"value": args.value, "candidates": keep}, f)
    print(f"filter {args.value}: {len(prev)} -> {len(keep)} survivors -> {args.out}")
    for addr, ty in keep[:40]:
        print(f"  0x{addr:X} ({ty})")
    if len(keep) > 40:
        print(f"  ... +{len(keep)-40} more")


def resolve_targets(args):
    """Targets = explicit --addr list, else all candidates in the candidate file."""
    if args.addr:
        return [(int(a, 16), args.type) for a in args.addr]
    with open(args.infile) as f:
        return [(a, t) for a, t in json.load(f)["candidates"]]


def cmd_write(h, args):
    targets = resolve_targets(args)
    for addr, ty in targets:
        before = read(h, addr, {"i32": 4, "i16": 2, "f32": 4, "u8": 1}[ty])
        ok = writemem(h, addr, pack_val(args.value, ty))
        after = read(h, addr, {"i32": 4, "i16": 2, "f32": 4, "u8": 1}[ty])
        b = unpack_val(before, ty) if before else "?"
        a = unpack_val(after, ty) if after else "?"
        stuck = "STUCK" if (after and unpack_val(after, ty) == args.value) else "reverted/failed"
        print(f"0x{addr:X} ({ty}): {b} -> wrote {args.value} -> readback {a}  [{'OK' if ok else 'WRITE-FAIL'} / {stuck}]")


def cmd_poke(h, args):
    targets = resolve_targets(args)
    blobs = [(addr, pack_val(args.value, ty)) for addr, ty in targets]
    end = time.time() + args.secs
    writes = 0
    fails = 0
    while time.time() < end:
        for addr, blob in blobs:
            if writemem(h, addr, blob):
                writes += 1
            else:
                fails += 1
    print(f"poked {len(blobs)} addr(s) with {args.value} for {args.secs}s: {writes} writes, {fails} fails")
    for addr, ty in targets:
        d = read(h, addr, {"i32": 4, "i16": 2, "f32": 4, "u8": 1}[ty])
        print(f"  0x{addr:X} now = {unpack_val(d, ty) if d else '?'}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("cmd", choices=["verify", "find", "filter", "write", "poke"])
    ap.add_argument("value", nargs="?", type=int)
    ap.add_argument("--pid", type=int)
    ap.add_argument("--addr", action="append")
    ap.add_argument("--size", type=int, default=64)
    ap.add_argument("--type", default="i32")
    ap.add_argument("--types", default="i32,i16")
    ap.add_argument("--secs", type=float, default=8.0)
    ap.add_argument("--in", dest="infile", default=CAND_FILE)
    ap.add_argument("--out", default=CAND_FILE)
    args = ap.parse_args()

    pid = args.pid or find_pid()
    if not pid:
        sys.exit("FFT_enhanced.exe not running. Launch the game first.")
    print(f"pid={pid}")
    want_write = args.cmd in ("write", "poke")
    h = openp(pid, write=want_write)
    try:
        if args.cmd == "verify":
            cmd_verify(h, args)
        elif args.cmd == "find":
            if args.value is None:
                sys.exit("find needs a value")
            cmd_find(h, args)
        elif args.cmd == "filter":
            if args.value is None:
                sys.exit("filter needs a value")
            cmd_filter(h, args)
        elif args.cmd == "write":
            if args.value is None:
                sys.exit("write needs a value")
            cmd_write(h, args)
        elif args.cmd == "poke":
            if args.value is None:
                sys.exit("poke needs a value")
            cmd_poke(h, args)
    finally:
        k32.CloseHandle(h)


if __name__ == "__main__":
    main()
