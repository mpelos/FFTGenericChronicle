#!/usr/bin/env python3
"""Unsupervised per-offset classifier for the battle unit struct (roadmap U5 / full map).

For each offset, using each unit's MANY level-matched captures, decide:
  VOLATILE  - the byte changes within a single unit at fixed level => runtime state
              (CT, action, current-HP, charge, current status, ...), NOT a static attribute.
  CONST     - static within every unit AND identical across units => global constant / pad /
              type marker.
  VARIES    - static within every unit but differs across units => candidate per-unit attribute.

VARIES offsets are the interesting ones (job, abilities, raw stats, unmapped fields). The script
labels offsets already confirmed and guesses the rest by value range. Reads a ground-truth JSON
(same shape as map_attributes.py) only for the unit id list and per-unit "level" filter.

Usage:
  python tools/profile_struct.py GROUND_TRUTH.json LOG [LOG ...] [--max 0x90] [--since ISO]
"""
import argparse
import glob
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path

DUMP_RE = re.compile(r"\[DUMP ptr=0x([0-9A-Fa-f]+) id=0x([0-9A-Fa-f]+)\]\s*(.*)")
DEFAULT_NAMES = {0x01: "Ramza", 0x1E: "Agrias", 0x1F: "Beowulf", 0x32: "Cloud", 0x80: "Ninja"}
LEVEL_OFF = 0x29

# Confirmed/known labels (offset -> short label) for annotation.
KNOWN = {
    0x00: "charId", 0x04: "team", 0x05: "foe-bit", 0x06: "gender-flags", 0x09: "zodiac(hi)",
    0x28: "EXP", 0x29: "Level", 0x2A: "MaxBrave", 0x2B: "Brave", 0x2C: "MaxFaith", 0x2D: "Faith",
    0x30: "HP", 0x32: "MaxHP", 0x34: "MP", 0x36: "MaxMP",
    0x38: "rawPA?", 0x39: "rawMA?", 0x3A: "rawSpd?",
    0x3E: "PA", 0x3F: "MA", 0x40: "Speed", 0x41: "CT", 0x42: "Move", 0x43: "Jump",
    0x1A: "eq:head", 0x1C: "eq:body", 0x1E: "eq:acc", 0x20: "eq:Rwpn", 0x22: "eq:Rsh",
    0x24: "eq:Lwpn", 0x26: "eq:Lsh", 0x61: "status(KO bit5)",
}


def parse_all(paths):
    out = {}
    for path in paths:
        mt = os.path.getmtime(path)
        for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
            m = DUMP_RE.search(line)
            if not m:
                continue
            cid = int(m.group(2), 16)
            vals = [int(b, 16) for b in m.group(3).replace("|", " ").split()
                    if re.fullmatch(r"[0-9A-Fa-f]{2}", b)]
            if len(vals) < 0x40:
                continue
            out.setdefault(cid, []).append((mt, vals))
    return out


def guess(vals):
    """Heuristic label for a VARIES offset given its per-unit byte values."""
    s = sorted(set(vals))
    if all(0 <= v <= 11 for v in vals):
        return "small0-11 (zodiac/elem/idx?)"
    if all(v in (0, 1) for v in vals):
        return "flag 0/1"
    if all(v < 0xB0 for v in vals) and len(s) == len(vals):
        return "all-distinct (job/ability/uid?)"
    if max(vals) >= 0x80:
        return "high byte (id/flags?)"
    return "small int (stat?)"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("ground_truth")
    ap.add_argument("logs", nargs="+")
    ap.add_argument("--max", default="0x90")
    ap.add_argument("--since", default=None)
    args = ap.parse_args()
    max_off = int(args.max, 0)
    since = datetime.fromisoformat(args.since).timestamp() if args.since else None

    gt = json.loads(Path(args.ground_truth).read_text(encoding="utf-8"))
    names = dict(DEFAULT_NAMES)
    for k, v in (gt.get("names") or {}).items():
        names[int(k, 0)] = v
    units_gt = {int(k, 0): v for k, v in (gt.get("units") or {}).items()}
    target_levels = {c: units_gt[c]["level"] for c in units_gt if "level" in units_gt[c]}

    paths = []
    for g in args.logs:
        paths.extend(glob.glob(g))
    alld = parse_all(paths)

    # per-unit list of level-matched byte arrays
    pools = {}
    for cid in units_gt:
        pool = alld.get(cid, [])
        if since is not None:
            pool = [e for e in pool if e[0] >= since]
        tl = target_levels.get(cid)
        if tl is not None:
            pool = [e for e in pool if e[1][LEVEL_OFF] == tl]
        if pool:
            pools[cid] = [v for _, v in pool]
    order = [c for c in (0x01, 0x1F, 0x1E, 0x32, 0x80) if c in pools] + \
            [c for c in pools if c not in (0x01, 0x1F, 0x1E, 0x32, 0x80)]
    if not order:
        print("no level-matched pools", file=sys.stderr)
        return 1

    def label(c):
        return names.get(c, f"id0x{c:02X}")

    print("Unit pools (level-matched capture counts): " +
          ", ".join(f"{label(c)}={len(pools[c])}" for c in order))
    print(f"\n{'off':>5} | " + " | ".join(f"{label(c)[:7]:>7}" for c in order) +
          " | class    | note")
    print("-" * (8 + 10 * len(order) + 30))

    varies, consts, volatile = [], [], []
    for off in range(max_off):
        per_unit_static = {}
        is_vol = False
        vol_units = []
        for c in order:
            valset = {buf[off] for buf in pools[c] if off < len(buf)}
            if len(valset) == 1:
                per_unit_static[c] = next(iter(valset))
            else:
                is_vol = True
                vol_units.append(c)
        if is_vol:
            cls = "VOLATILE"
            note = "runtime; varies in: " + ",".join(label(c) for c in vol_units)
            volatile.append(off)
            cells = []
            for c in order:
                if c in per_unit_static:
                    cells.append(str(per_unit_static[c]))
                else:
                    rng = sorted({buf[off] for buf in pools[c] if off < len(buf)})
                    cells.append(f"{rng[0]}~{rng[-1]}")
        else:
            vals = [per_unit_static[c] for c in order]
            cells = [str(v) for v in vals]
            if len(set(vals)) == 1:
                cls = "CONST"
                note = KNOWN.get(off, f"=={vals[0]}")
                consts.append(off)
            else:
                cls = "VARIES"
                note = KNOWN.get(off) or guess(vals)
                varies.append(off)
        # only print VARIES and known offsets (skip the noise of consts/volatile unless known)
        if cls == "VARIES" or off in KNOWN:
            print(f"+0x{off:02X} | " + " | ".join(f"{x:>7}" for x in cells) +
                  f" | {cls:<8} | {note}")

    print(f"\nSummary: {len(varies)} VARIES (candidate attrs), {len(consts)} CONST, "
          f"{len(volatile)} VOLATILE (runtime).")
    print("VARIES offsets:", ", ".join(f"0x{o:02X}" for o in varies))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
