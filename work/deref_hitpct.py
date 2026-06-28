#!/usr/bin/env python3
"""Confirm the hit% data flow on the LIVE (clean-observe) game, no screen needed.

RE says: 0x227FEA `mov rbp,[rip+disp]` loads a forecast-object pointer from a global;
0x227FFA `movzx eax,word[rbp+0x2C]` reads the computed hit%; 0x228004 stores it to the
display buffer 0x7832C0. So if a forecast is active, *(u16*)(*(u64*)global + 0x2C) should
equal *(u16*)0x1407832C0 (the proven on-screen %). This validates the writer/source we
plan to hook, before any rebuild.
"""
import ctypes, struct, subprocess, sys
from ctypes import wintypes as w

k32 = ctypes.WinDLL("kernel32", use_last_error=True)
k32.OpenProcess.restype = w.HANDLE
k32.OpenProcess.argtypes = [w.DWORD, w.BOOL, w.DWORD]
k32.ReadProcessMemory.argtypes = [w.HANDLE, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]

IMAGE_BASE = 0x140000000
# global pointer = next_instr + disp ; instr 0x227FEA (len 7), disp 0x02DCBD07
GLOBAL_VA = IMAGE_BASE + (0x227FEA + 7 + 0x02DCBD07)
HITPCT_VA = 0x1407832C0
OBJ_FIELD = 0x2C


def find_pid(name="FFT_enhanced"):
    out = subprocess.run(["powershell", "-NoProfile", "-Command",
                          f"(Get-Process -Name {name} -ErrorAction SilentlyContinue).Id"],
                         capture_output=True, text=True).stdout.strip()
    return int(out.splitlines()[0]) if out else None


def read(h, addr, size):
    buf = (ctypes.c_char * size)()
    n = ctypes.c_size_t(0)
    ok = k32.ReadProcessMemory(h, ctypes.c_void_p(addr), buf, size, ctypes.byref(n))
    return buf.raw[:n.value] if ok else None


def main():
    pid = find_pid()
    if not pid:
        sys.exit("FFT_enhanced not running")
    h = k32.OpenProcess(0x0410, False, pid)  # QUERY_INFO | VM_READ
    if not h:
        sys.exit(f"OpenProcess failed err={ctypes.get_last_error()}")
    print(f"pid={pid}  global_ptr@0x{GLOBAL_VA:X}")
    gp = read(h, GLOBAL_VA, 8)
    obj = struct.unpack("<Q", gp)[0] if gp else 0
    print(f"  forecast object ptr = 0x{obj:X}")
    natural = "?"
    if obj:
        d = read(h, obj + OBJ_FIELD, 2)
        if d:
            natural = struct.unpack("<H", d)[0]
    disp = read(h, HITPCT_VA, 2)
    shown = struct.unpack("<H", disp)[0] if disp else "?"
    print(f"  object+0x{OBJ_FIELD:X} (hit% source) = {natural}")
    print(f"  display buffer 0x{HITPCT_VA:X} = {shown}")
    if natural == shown and natural != "?":
        print("  MATCH -> source feeds display buffer. RE confirmed.")
    else:
        print("  (differ: preview may be closed / object stale, or another panel owns the buffer right now)")
    k32.CloseHandle(h)


if __name__ == "__main__":
    main()
