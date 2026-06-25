#!/usr/bin/env python3
"""General attribute-offset mapper for the battle unit struct (roadmap U5 / full-stat map).

Generalizes analyze_equipment_dumps.py --find from equipment ids to ANY attribute
(stats, job, zodiac, gender, ability ids, ...). Each [DUMP ptr=0x.. id=0xNN] line carries
a >=0x200-byte snapshot of a battle unit. Given ground-truth attribute values for several
units, for each attribute this ranks struct offsets by how many ground-truth units match,
under several interpretations:

  b   = byte            buf[off]
  w   = word (LE)       buf[off] | buf[off+1]<<8
  n0  = low nibble      buf[off] & 0x0F
  n1  = high nibble     (buf[off] >> 4) & 0x0F
  bitK= single bit K    (buf[off] >> K) & 1     (only when the attribute is 0/1)

Ranking by match-count (not strict intersection) means a single mutable/drifted value
(e.g. a unit that leveled since the capture) does not hide an otherwise-correct offset.

Ground-truth JSON shape (values are integers; unknown attrs may be omitted per unit):
  {
    "names":  { "0x03": "Orlandeau", "0x81": "Archer", "0x82": "Monster" },
    "known":  { "hp": "0x30:w", "maxHp": "0x32:w", "pa": "0x3E:b", "faith": "0x2D:b" },
    "units": {
      "0x01": { "job": 2, "zodiac": 5, "gender": 0, "brave": 70, "faith": 65, "pa": 12 },
      "0x1E": { "job": 7, "zodiac": 1, "gender": 1, "brave": 68, "faith": 72, "pa": 10 }
    }
  }

Usage:
  python tools/map_attributes.py GROUND_TRUTH.json LOG [LOG ...] [--max 0x90]
                                 [--out work/attribute-map.result.json] [--show 0x30,0x3e]
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
LEVEL_OFF = 0x29  # Level byte; stats drift with level, so map only level-matched dumps.


def parse_all_dumps(paths):
    """Return {char_id: [(mtime, [byte,...]), ...]} for every [DUMP] line across files.

    Unlike a first-seen scan, this keeps ALL captures per unit so the caller can select a
    dump at a specific level (a unit that leveled mid-testing has stale stats in old dumps).
    """
    out = {}
    for path in paths:
        mt = os.path.getmtime(path)
        for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
            m = DUMP_RE.search(line)
            if not m:
                continue
            cid = int(m.group(2), 16)
            hexpart = m.group(3).replace("|", " ")
            vals = [int(b, 16) for b in hexpart.split() if re.fullmatch(r"[0-9A-Fa-f]{2}", b)]
            if len(vals) < 0x40:
                continue
            out.setdefault(cid, []).append((mt, vals))
    return out


def select_dumps(all_dumps, target_levels, since=None):
    """Pick one representative dump per unit: the NEWEST capture, restricted to dumps whose
    +0x29 level matches the unit's target level (when given) and to mtime >= since.

    Returns (chosen {cid: bytes}, meta {cid: (mtime, level, pool_size)}, dropped {cid: reason}).
    """
    chosen, meta, dropped = {}, {}, {}
    for cid, entries in all_dumps.items():
        pool = entries
        if since is not None:
            pool = [e for e in pool if e[0] >= since]
        tl = target_levels.get(cid)
        if tl is not None:
            pool = [e for e in pool if e[1][LEVEL_OFF] == tl]
        if not pool:
            seen = sorted({e[1][LEVEL_OFF] for e in entries})
            dropped[cid] = f"no dump at level {tl} (saw {seen})" if tl is not None else "no dump in window"
            continue
        mt, vals = max(pool, key=lambda e: e[0])
        chosen[cid] = vals
        meta[cid] = (mt, vals[LEVEL_OFF], len(pool))
    return chosen, meta, dropped


def extract(buf, off, kind):
    if off >= len(buf):
        return None
    if kind == "b":
        return buf[off]
    if kind == "w":
        return buf[off] | (buf[off + 1] << 8) if off + 1 < len(buf) else None
    if kind == "n0":
        return buf[off] & 0x0F
    if kind == "n1":
        return (buf[off] >> 4) & 0x0F
    if kind.startswith("bit"):
        return (buf[off] >> int(kind[3:])) & 1
    return None


def kinds_for(values):
    """Choose which interpretations make sense given the attribute's value range."""
    ks = ["b", "w"]
    if values and all(0 <= v <= 0x0F for v in values):
        ks += ["n0", "n1"]
    if values and all(v in (0, 1) for v in values):
        ks += [f"bit{k}" for k in range(8)]
    return ks


# Sort preference among equally-scoring candidates: simpler/lower interpretation first.
KIND_RANK = {"b": 0, "w": 1, "n0": 2, "n1": 3, **{f"bit{k}": 4 + k for k in range(8)}}


def rank_attribute(attr, gt_by_cid, dumps, max_off):
    """Return ranked candidate offsets for one attribute."""
    cids = [c for c in gt_by_cid if c in dumps]
    values = [gt_by_cid[c] for c in cids]
    if not cids:
        return None
    n = len(cids)
    kinds = kinds_for(values)
    cands = []
    for off in range(0, max_off):
        for kind in kinds:
            matches = []
            ok = True
            for c in cids:
                got = extract(dumps[c], off, kind)
                if got is None:
                    ok = False
                    break
                if got == gt_by_cid[c]:
                    matches.append(c)
            if not ok:
                continue
            mc = len(matches)
            if mc == 0:
                continue
            cands.append((mc, off, kind, matches))
    # rank: more matches, then simpler kind, then lower offset
    cands.sort(key=lambda t: (-t[0], KIND_RANK.get(t[2], 99), t[1]))
    return {"n": n, "cids": cids, "values": dict(zip(cids, values)), "cands": cands}


def fmt_off(off, kind):
    return f"+0x{off:02X}:{kind}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("ground_truth")
    ap.add_argument("logs", nargs="+")
    ap.add_argument("--max", default="0x90", help="scan offsets [0, max) (default 0x90)")
    ap.add_argument("--out", default=None, help="write machine-readable result JSON here")
    ap.add_argument("--show", default=None, help="also print raw bytes at these offsets, e.g. 0x30,0x3e")
    ap.add_argument("--top", type=int, default=4, help="candidates to print per attribute")
    ap.add_argument("--since", default=None, help="ISO datetime; only use captures with mtime >= this")
    args = ap.parse_args()
    max_off = int(args.max, 0)
    since = datetime.fromisoformat(args.since).timestamp() if args.since else None

    gt = json.loads(Path(args.ground_truth).read_text(encoding="utf-8"))
    names = dict(DEFAULT_NAMES)
    for k, v in (gt.get("names") or {}).items():
        names[int(k, 0)] = v
    known = gt.get("known") or {}
    units_gt = {int(k, 0): v for k, v in (gt.get("units") or {}).items()}

    paths = []
    for g in args.logs:
        paths.extend(glob.glob(g))
    all_dumps = parse_all_dumps(paths)
    if not all_dumps:
        print("No [DUMP] lines parsed.", file=sys.stderr)
        return 1

    def label(c):
        return names.get(c, f"id0x{c:02X}")

    # Select one level-matched representative dump per unit (prevents mixing stale stats
    # from before a unit leveled up). The per-unit "level" in ground truth drives the filter.
    target_levels = {c: units_gt[c]["level"] for c in units_gt if "level" in units_gt[c]}
    dumps, meta, dropped = select_dumps(all_dumps, target_levels, since)
    order = sorted(dumps.keys())

    print("Representative dump per unit (newest, level-matched):")
    for c in order:
        mt, lv, pool = meta[c]
        tl = target_levels.get(c)
        tag = f"L{lv}" + ("" if tl is None else f"==target{tl}" if tl == lv else f"!=target{tl}")
        gt = " [GT]" if c in units_gt else ""
        print(f"  {label(c):<10} {tag:<16} from {pool:>3} matched dump(s), newest "
              f"{datetime.fromtimestamp(mt):%m-%d %H:%M}{gt}")
    for c, why in dropped.items():
        if c in units_gt:
            print(f"  !! {label(c):<10} DROPPED: {why}")
    present = [c for c in order if c in units_gt]
    print(f"\n{len(dumps)} units selected; {len(present)} have ground truth: "
          f"{', '.join(label(c) for c in present)}\n")

    if args.show:
        offs = [int(x, 0) for x in args.show.split(",")]
        print("Raw bytes (b / w) at requested offsets:")
        hdr = "unit".ljust(12) + "".join(f"+0x{o:02X}".ljust(14) for o in offs)
        print(hdr)
        for c in order:
            buf = dumps[c]
            row = label(c).ljust(12)
            for o in offs:
                b = buf[o] if o < len(buf) else 0
                w = (buf[o] | (buf[o + 1] << 8)) if o + 1 < len(buf) else 0
                row += f"{b}/{w}".ljust(14)
            print(row)
        print()

    # All attributes mentioned across units.
    attrs = []
    for c in units_gt:
        for a in units_gt[c]:
            if a not in attrs:
                attrs.append(a)

    result = {"attributes": {}}
    print(f"{'attribute':<14} {'verdict':<11} best            (matches)   known/notes")
    print("-" * 78)
    for attr in attrs:
        gt_by_cid = {c: units_gt[c][attr] for c in units_gt if attr in units_gt[c]}
        r = rank_attribute(attr, gt_by_cid, dumps, max_off)
        if not r or not r["cands"]:
            print(f"{attr:<14} {'NO-DATA':<11}")
            continue
        cands = r["cands"]
        n = r["n"]
        best_mc = cands[0][0]
        all_match = [c for c in cands if c[0] == best_mc]
        if best_mc == n and len(all_match) == 1:
            verdict = "SOLID"
        elif best_mc == n:
            verdict = f"AMBIG({len(all_match)})"
        else:
            verdict = f"PARTIAL {best_mc}/{n}"
        mc, off, kind, matches = cands[0]
        knownnote = ""
        if attr in known:
            koff_s, kkind = known[attr].split(":")
            koff = int(koff_s, 0)
            hit = any(c[1] == koff and c[2] == kkind and c[0] == n for c in cands)
            knownnote = f"known={known[attr]} -> {'PASS' if hit else 'MISMATCH'}"
        print(f"{attr:<14} {verdict:<11} {fmt_off(off, kind):<15} ({mc}/{n})     {knownnote}")
        # detail lines for the top candidates
        for mc, off, kind, matches in cands[:args.top]:
            missed = [label(c) for c in r["cids"] if c not in matches]
            miss_txt = "" if not missed else "  miss=" + ",".join(missed)
            print(f"                 {fmt_off(off, kind):<15} {mc}/{n}{miss_txt}")
        result["attributes"][attr] = {
            "verdict": verdict,
            "best": {"offset": cands[0][1], "kind": cands[0][2], "matches": cands[0][0], "n": n},
            "candidates": [
                {"offset": o, "kind": k, "matches": m} for (m, o, k, _ms) in cands[:8]
            ],
            "ground_truth": {f"0x{c:02X}": v for c, v in r["values"].items()},
        }
        print()

    if args.out:
        Path(args.out).write_text(json.dumps(result, indent=2), encoding="utf-8")
        print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
