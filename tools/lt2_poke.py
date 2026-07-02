#!/usr/bin/env python3
"""LT2 pokes: tiny, targeted WRITE operations for the live-test session.

Each invocation performs ONE write and exits (auditable, minimal risk):

  python tools/lt2_poke.py imm <slot> <byteIdx 0-4> <bitMask hex>   # OR bit into immunity array +0x5C..0x60
  python tools/lt2_poke.py imm-clear <slot> <byteIdx> <bitMask>     # clear that bit
  python tools/lt2_poke.py statuspct <value>                        # write dword g_7B07AC (0x1407B07AC)
  python tools/lt2_poke.py read <slot>                              # dump the four status arrays + g_7B07AC
"""
from __future__ import annotations

import ctypes
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_live_unit_pointers import CloseHandle, OpenProcess, ReadProcessMemory, find_pid  # noqa: E402

kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
WriteProcessMemory = kernel32.WriteProcessMemory
WriteProcessMemory.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_char_p,
                               ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
WriteProcessMemory.restype = ctypes.c_bool

UNIT_TABLE = 0x141853CE0
STRIDE = 0x200
STATUS_PCT = 0x1407B07AC
IMM_BASE = 0x5C


def read_mem(handle, address: int, size: int) -> bytes:
    buf = ctypes.create_string_buffer(size)
    n = ctypes.c_size_t()
    if not ReadProcessMemory(handle, ctypes.c_void_p(address), buf, size, ctypes.byref(n)):
        raise OSError(f"read failed at 0x{address:X}")
    return bytes(buf.raw[: n.value])


def write_mem(handle, address: int, data: bytes) -> None:
    n = ctypes.c_size_t()
    if not WriteProcessMemory(handle, ctypes.c_void_p(address), data, len(data), ctypes.byref(n)):
        raise OSError(f"write failed at 0x{address:X} (err={ctypes.get_last_error()})")


def main() -> int:
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    pid = find_pid("FFT_enhanced.exe")
    if not pid:
        raise SystemExit("process not found")
    handle = OpenProcess(0x0400 | 0x0010 | 0x0020 | 0x0008, False, pid)  # +VM_WRITE +VM_OPERATION
    if not handle:
        raise SystemExit("OpenProcess failed (need admin?)")
    try:
        if cmd in ("imm", "imm-clear"):
            slot, byte_idx, mask = int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4], 16)
            addr = UNIT_TABLE + slot * STRIDE + IMM_BASE + byte_idx
            old = read_mem(handle, addr, 1)[0]
            new = (old | mask) if cmd == "imm" else (old & ~mask)
            write_mem(handle, addr, bytes([new]))
            print(f"u{slot} imm[{byte_idx}] @0x{addr:X}: {old:02X} -> {new:02X}")
        elif cmd == "statuspct":
            value = int(sys.argv[2])
            old = int.from_bytes(read_mem(handle, STATUS_PCT, 4), "little")
            write_mem(handle, STATUS_PCT, value.to_bytes(4, "little"))
            print(f"g_7B07AC @0x{STATUS_PCT:X}: {old} -> {value}")
        elif cmd == "read":
            slot = int(sys.argv[2])
            base = UNIT_TABLE + slot * STRIDE
            blk = read_mem(handle, base, 0x200)
            print(f"u{slot} src={blk[0x57:0x5C].hex().upper()} imm={blk[0x5C:0x61].hex().upper()}"
                  f" eff={blk[0x61:0x66].hex().upper()} master={blk[0x1EF:0x1F4].hex().upper()}")
            print(f"g_7B07AC={int.from_bytes(read_mem(handle, STATUS_PCT, 4), 'little')}")
        else:
            print(__doc__)
            return 1
    finally:
        CloseHandle(handle)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
