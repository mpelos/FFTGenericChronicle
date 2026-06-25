#!/usr/bin/env python3
"""Report each battle unit's Level (+0x29) and HP per [DUMP] capture, with file mtime.

Stats (HP/PA/MA/...) drift as units level up between test sessions, so attribute mapping
must be restricted to captures matching a unit's CURRENT level. This diagnostic shows which
levels appear per unit in a given time window so we can pick the level-consistent captures.

Usage:
  python tools/dump_levels.py LOG [LOG ...] [--since 2026-06-23T19:00]
"""
import argparse
import glob
import os
import re
import sys
from datetime import datetime
from pathlib import Path

DUMP_RE = re.compile(r"\[DUMP ptr=0x([0-9A-Fa-f]+) id=0x([0-9A-Fa-f]+)\]\s*(.*)")
NAMES = {0x01: "Ramza", 0x1E: "Agrias", 0x1F: "Beowulf", 0x32: "Cloud", 0x80: "Ninja"}
LEVEL_OFF = 0x29
HP_OFF = 0x30


def parse_file_units(path):
    """Return {cid: (level, hp)} first-seen per cid in this file."""
    out = {}
    for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
        m = DUMP_RE.search(line)
        if not m:
            continue
        cid = int(m.group(2), 16)
        if cid in out:
            continue
        hexpart = m.group(3).replace("|", " ")
        vals = [int(b, 16) for b in hexpart.split() if re.fullmatch(r"[0-9A-Fa-f]{2}", b)]
        if len(vals) <= HP_OFF + 1:
            continue
        hp = vals[HP_OFF] | (vals[HP_OFF + 1] << 8)
        out[cid] = (vals[LEVEL_OFF], hp)
    return out


def label(cid):
    return NAMES.get(cid, f"id0x{cid:02X}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("logs", nargs="+")
    ap.add_argument("--since", default=None, help="ISO datetime; only files with mtime >= this")
    args = ap.parse_args()
    since = datetime.fromisoformat(args.since) if args.since else None

    paths = []
    for g in args.logs:
        paths.extend(glob.glob(g))

    rows = []
    skipped = 0
    for p in paths:
        mt = datetime.fromtimestamp(os.path.getmtime(p))
        if since and mt < since:
            skipped += 1
            continue
        for cid, (lv, hp) in parse_file_units(p).items():
            rows.append((mt, cid, lv, hp, Path(p).name))
    rows.sort()

    if since:
        print(f"Window: mtime >= {since}  ({len(paths) - skipped} files in window, {skipped} excluded)\n")

    bycid = {}
    for mt, cid, lv, hp, name in rows:
        bycid.setdefault(cid, []).append((mt, lv, hp, name))

    if not bycid:
        print("No in-window [DUMP] captures found.", file=sys.stderr)
        return 1

    print(f"{'unit':<10} levels seen (count)            | newest capture")
    print("-" * 78)
    for cid in sorted(bycid):
        entries = bycid[cid]
        lvls = {}
        for mt, lv, hp, name in entries:
            lvls.setdefault(lv, []).append(mt)
        summ = ", ".join(f"L{lv}x{len(v)}" for lv, v in sorted(lvls.items()))
        consistent = "OK" if len(lvls) == 1 else f"!! {len(lvls)} LEVELS"
        latest = max(entries, key=lambda e: e[0])
        print(f"{label(cid):<10} {summ:<30} | L{latest[1]} HP{latest[2]} @ "
              f"{latest[0]:%m-%d %H:%M}  [{consistent}]")
        if len(lvls) > 1:
            for lv, mts in sorted(lvls.items()):
                print(f"             L{lv}: {min(mts):%m-%d %H:%M}..{max(mts):%m-%d %H:%M}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
