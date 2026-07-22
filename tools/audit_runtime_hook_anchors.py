#!/usr/bin/env python3
"""Verify every fixed runtime-hook anchor against the installed Enhanced executable."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass
from pathlib import Path

import pefile


REPO = Path(__file__).resolve().parents[1]
GAME_DIRS = (
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles"),
)
DEFAULT_EXE = next((path / "FFT_enhanced.exe" for path in GAME_DIRS if (path / "FFT_enhanced.exe").exists()), GAME_DIRS[0] / "FFT_enhanced.exe")


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    role: str


ANCHORS = (
    Anchor("preview-hit-pct", 0x227F86, "41 BA 02 00 00 00", "DCL forecast hit percentage"),
    Anchor("result-selector", 0x205210, "48 89 5C 24 08 48 89 6C 24 10", "result/outcome selector"),
    Anchor("dcl-miss-kind", 0x205B38, "44 88 A7 C0 01 00 00", "execution miss-kind commit"),
    Anchor("calc-entry", 0x3099AC, "48 89 5C 24 18 55 56 57", "per-action/per-target calculation entry"),
    Anchor("pre-clamp", 0x30A5D7, "0F BF 45 06", "same-result HP/status apply window"),
    Anchor("canonical-admission", 0x281EFA, "8A 54 1D D8 80 FA FF 74 0F", "complete outer-action TargetBatch at the first per-target index read"),
    Anchor("canonical-admission-order", 0x281ED6, "48 8D 05 23 E1 D7 FF 4C 8D B0 80 3E 85 01 4C 03 F6 49 8B CE", "source unit+0x1A0 order record carrying selected X/map-level/Y"),
    Anchor("canonical-post-apply", 0x30AB4D, "48 8B 5C 24 60 48 8B 6C 24 70", "post-target state-apply convergence for source effect and payment"),
    Anchor("canonical-reaction-complete", 0xD90CFA2, "E8 A9 90 8F F2 48 8B 5C 24", "terminal empty native queue scan after the outer action or a Reaction chain"),
    Anchor("staged-bundle", 0x281F12, "48 FF C3 48 83 FB 15", "post-calculation staged bundle"),
    Anchor("evade-input", 0x30F404, "48 8B D3 88 4B 41", "pre-roll target input"),
    Anchor("counter-path", 0x30C700, "48 89 5C 24 08 57 48 83 EC 20 80 79 01 FF", "counter/reaction staging probe"),
    Anchor("reaction-commit-p2", 0x206421, "40 88 B3 D3 01 00 00", "accepted-reaction queue pass-2 commit probe/control"),
    Anchor("reaction-commit-p0", 0x2066AE, "40 88 B3 D3 01 00 00", "accepted-reaction queue pass-0 commit probe"),
    Anchor("reaction-commit-p1", 0x206743, "40 88 B7 D3 01 00 00", "accepted-reaction queue pass-1 commit probe"),
    Anchor("reaction-preselector-p2", 0x2063A9, "48 8D 4D D2 E8 86 CA 07 00", "pass-2 candidate snapshot before exact-id selector"),
    Anchor("reaction-effect-complete", 0x212C2E, "66 44 01 70 0C", "state-0x2C executed-reaction actor before cleanup"),
    Anchor("auto-potion-consume", 0x2816B2, "2A CB 43 88 8C 05 00 7C 1A 01", "shared item inventory decrement before native subtraction"),
    Anchor("weapon-lof-arc-result", 0x28030B, "8B F8 85 F6 78 08 85 C0 78 07 3B F0 75 03 44 8A FB", "Arc resolver result before target-equality gate"),
    Anchor("weapon-lof-direct-result", 0x2803A3, "8B F8 85 F6 78 08 85 C0 78 07 3B F0 75 03 44 8A FB", "Direct resolver result before target-equality gate"),
    Anchor("roll-verdict", 0x30F40F, "44 8B D0 BD 01 00 00 00 85 C0", "post-avoidance verdict"),
    Anchor("reaction-r1", 0x30BDEE, "E8 75 D0 F6 FF", "Brave reaction roll 1"),
    Anchor("reaction-r2", 0x30BE44, "E8 1F D0 F6 FF", "Brave reaction roll 2"),
    Anchor("reaction-r3", 0x30BE9A, "E8 C9 CF F6 FF", "Brave reaction roll 3"),
    Anchor("reaction-r4", 0x30BEDA, "E8 89 CF F6 FF", "Brave reaction roll 4"),
    Anchor("evade-copier-b", 0x2854DB, "4C 8D 5C 24 60 49 8B 5B 10", "equipment evade copier"),
    Anchor("evade-copier-c", 0x3966BF, "48 8B D7 48 8B CE", "equipment evade copier twin"),
    Anchor("magic-chance", 0x304D96, "B9 64 00 00 00", "legacy magic accuracy probe"),
    Anchor("status-chance", 0x30659B, "8D 4B 5C", "legacy native status chance probe"),
    Anchor("roll-rng", 0x278E68, "E9 EB 38 98 0E", "shared native RNG trampoline"),
)


def steam_build_id(exe: Path) -> str:
    steamapps = exe.parents[2]
    manifest = steamapps / "appmanifest_1004640.acf"
    if not manifest.exists():
        return "unknown"
    match = re.search(r'"buildid"\s+"(\d+)"', manifest.read_text(encoding="utf-8", errors="replace"))
    return match.group(1) if match else "unknown"


def nearby_rvas(pe: pefile.PE, data: bytes, anchor: Anchor, radius: int = 0x400) -> list[int]:
    pattern = bytes.fromhex(anchor.expected)
    hits: list[int] = []
    start = 0
    while True:
        raw = data.find(pattern, start)
        if raw < 0:
            break
        start = raw + 1
        try:
            rva = pe.get_rva_from_offset(raw)
        except Exception:
            continue
        if abs(rva - anchor.rva) <= radius:
            hits.append(rva)
    return hits


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    exe = args.exe.resolve()
    output = args.output or REPO / "work" / f"{int(time.time())}-runtime-hook-anchor-audit.md"
    data = exe.read_bytes()
    pe = pefile.PE(str(exe), fast_load=True)
    digest = hashlib.sha256(data).hexdigest().upper()

    rows: list[str] = []
    failures = 0
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        try:
            raw = pe.get_offset_from_rva(anchor.rva)
            actual = data[raw : raw + len(expected)]
        except Exception:
            actual = b""
        ok = actual == expected
        nearby = nearby_rvas(pe, data, anchor) if not ok else [anchor.rva]
        if not ok:
            failures += 1
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {'PASS' if ok else 'FAIL'} | "
            f"`{actual.hex(' ').upper() or '-'}` | "
            f"{', '.join(f'`0x{rva:X}`' for rva in nearby) or '-'} | {anchor.role} |"
        )

    text = "\n".join(
        [
            "# Runtime Hook Anchor Audit",
            "",
            f"- Executable: `{exe}`",
            f"- Steam build ID: `{steam_build_id(exe)}`",
            f"- SHA-256: `{digest}`",
            f"- Result: `{'PASS' if failures == 0 else 'FAIL'}` ({len(ANCHORS) - failures}/{len(ANCHORS)} anchors)",
            "",
            "| Anchor | RVA | Result | Actual bytes | Nearby exact matches | Role |",
            "| --- | ---: | --- | --- | --- | --- |",
            *rows,
            "",
            "A failed anchor disables its guarded runtime feature. Nearby matches are diagnostic candidates only; they are not accepted automatically.",
            "",
        ]
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(text, encoding="utf-8")
    print(output)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
