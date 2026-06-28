#!/usr/bin/env python3
"""Verify the preview hit-% hook WITHOUT the screen.

Reads battleprobe_log.txt for the hook buffer address, then reads (external RPM):
  buffer: [0]=fire count, [4]=last natural %, [8]=forced (-1=logOnly), [12]=site rva
  display buffer 0x1407832C0 (what the renderer draws)
  natural source = *(u16*)(*(u64*)0x142FF3CF8 + 0x2C)
Run while an attack preview is open in-game.
"""
import ctypes, re, struct, subprocess, sys
from ctypes import wintypes as w
from pathlib import Path

LOG = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/battleprobe_log.txt")
HITPCT_VA = 0x1407832C0
GLOBAL_VA = 0x142FF3CF8
OBJ_FIELD = 0x2C

k32 = ctypes.WinDLL("kernel32", use_last_error=True)
k32.OpenProcess.restype = w.HANDLE
k32.OpenProcess.argtypes = [w.DWORD, w.BOOL, w.DWORD]
k32.ReadProcessMemory.argtypes = [w.HANDLE, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]


def find_pid():
    out = subprocess.run(["powershell", "-NoProfile", "-Command",
                          "(Get-Process -Name FFT_enhanced -ErrorAction SilentlyContinue).Id"],
                         capture_output=True, text=True).stdout.strip()
    return int(out.splitlines()[0]) if out else None


def read(h, addr, size):
    buf = (ctypes.c_char * size)()
    n = ctypes.c_size_t(0)
    if k32.ReadProcessMemory(h, ctypes.c_void_p(addr), buf, size, ctypes.byref(n)):
        return buf.raw[:n.value]
    return None


def main():
    buf_addr = None
    if LOG.exists():
        for line in LOG.read_text(errors="ignore").splitlines():
            m = re.search(r"\[PREVIEW-HITPCT-HOOK\].*buf=0x([0-9A-Fa-f]+)", line)
            if m:
                buf_addr = int(m.group(1), 16)
        if buf_addr is None:
            print("no [PREVIEW-HITPCT-HOOK] line in log yet — is the new DLL loaded? (relaunch via Reloaded)")
    else:
        print(f"log not found: {LOG}")

    pid = find_pid()
    if not pid:
        sys.exit("FFT_enhanced not running")
    h = k32.OpenProcess(0x0410, False, pid)
    if not h:
        sys.exit(f"OpenProcess failed err={ctypes.get_last_error()}")
    print(f"pid={pid}")

    if buf_addr:
        b = read(h, buf_addr, 16)
        if b:
            cnt, natural, forced, site = struct.unpack("<IiiI", b)
            print(f"  hook buffer @0x{buf_addr:X}: fireCount={cnt}  lastNatural={natural}  "
                  f"forced={forced if forced != -1 else 'logOnly'}  site=0x{site:X}")
            if cnt == 0:
                print("  -> fireCount=0: hook installed but the hooked site has not run for the preview yet.")

    disp = read(h, HITPCT_VA, 2)
    shown = struct.unpack("<H", disp)[0] if disp else "?"
    gp = read(h, GLOBAL_VA, 8)
    obj = struct.unpack("<Q", gp)[0] if gp else 0
    natural = "?"
    if obj:
        d = read(h, obj + OBJ_FIELD, 2)
        if d:
            natural = struct.unpack("<H", d)[0]
    print(f"  display buffer 0x{HITPCT_VA:X} = {shown}   (this is what the renderer draws)")
    print(f"  natural source object+0x{OBJ_FIELD:X} = {natural}")
    if shown == 7:
        print("  VERDICT: display buffer == 7 (forced) -> control works; screen shows our value.")
    elif isinstance(shown, int) and shown == natural:
        print("  VERDICT: display == natural -> not forced here (preview maybe closed, or wrong site).")
    k32.CloseHandle(h)


if __name__ == "__main__":
    main()
