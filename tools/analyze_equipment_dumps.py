#!/usr/bin/env python3
"""Cross-correlate live unit-struct [DUMP] lines against the item catalog to locate
equipment-id offsets in the battle unit struct (roadmap U5).

Each [DUMP ptr=0x.. id=0xNN] line carries a 0x200-byte snapshot of a battle unit.
Equipment ids are static per unit, so a candidate equipment-slot offset is one whose
byte value, across many units, consistently decodes to a valid equippable item id of a
single equipment family (weapon / shield / armor / headgear / accessory).

Usage:
  python tools/analyze_equipment_dumps.py LOG [LOG ...] [--start 0x00] [--end 0x40]
"""
import argparse
import csv
import glob
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CATALOG = REPO / "work" / "item_catalog.csv"

# Equipment type tokens carried in type_flags (ItemTypeFlags vocab).
EQUIP_TOKENS = ["Weapon", "Shield", "Armor", "Headgear", "Accessory"]

DUMP_RE = re.compile(r"\[DUMP ptr=0x([0-9A-Fa-f]+) id=0x([0-9A-Fa-f]+)\]\s*(.*)")
ACTOR_RE = re.compile(r"\[PRECLAMP-ACTOR-DUMP .*?id=0x([0-9A-Fa-f]+) bytes=\d+\]\s*(.*)")
# Known char ids -> friendly name (best-effort labels for readability).
CHAR_NAMES = {
    0x01: "Ramza", 0x1E: "Agrias", 0x1F: "Beowulf", 0x32: "Cloud", 0x80: "Ninja",
}


def load_catalog():
    items = {}
    with open(CATALOG, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            iid = int(row["item_id"])
            tf = row["type_flags"] or ""
            family = None
            for tok in EQUIP_TOKENS:
                if tok in tf:
                    family = tok
                    break
            items[iid] = {
                "name": row["name"],
                "category": row["item_category"],
                "family": family,  # None for consumables / Nothing
            }
    return items


def parse_dumps(paths):
    """Return {char_id: [byte,...]} keeping the first dump seen per char id."""
    units = {}
    order = []
    for path in paths:
        for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
            m = DUMP_RE.search(line)
            if not m:
                continue
            cid = int(m.group(2), 16)
            if cid in units:
                continue
            hexpart = m.group(3).replace("|", " ")
            vals = [int(b, 16) for b in hexpart.split() if re.fullmatch(r"[0-9A-Fa-f]{2}", b)]
            if len(vals) < 0x40:
                continue
            units[cid] = vals
            order.append(cid)
    return units, order


def parse_actor_dumps(paths):
    """Return {char_id: [byte,...]} for [PRECLAMP-ACTOR-DUMP] lines (0x548 actor struct)."""
    actors = {}
    for path in paths:
        for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
            m = ACTOR_RE.search(line)
            if not m:
                continue
            cid = int(m.group(1), 16)
            if cid in actors:
                continue
            hexpart = m.group(2).replace("|", " ")
            vals = [int(b, 16) for b in hexpart.split() if re.fullmatch(r"[0-9A-Fa-f]{2}", b)]
            if len(vals) < 0x40:
                continue
            actors[cid] = vals
    return actors


def label(cid):
    return CHAR_NAMES.get(cid, f"id0x{cid:02X}")


def find_equipment_block(struct_name, dumps, truth, slot_names):
    """Given {charId: bytes} and {charId: [ids per slot]}, find offsets per slot where
    every ground-truth unit's byte equals its id for that slot (intersection)."""
    print(f"\n=== {struct_name}: equipment-block search ===")
    chars = [c for c in truth if c in dumps]
    if len(chars) < 1:
        print("  (no matching dumps for the given char ids)")
        return
    nslots = max(len(v) for v in truth.values())
    for k in range(nslots):
        per_char_offsets = []
        detail = []
        for c in chars:
            ids = truth[c]
            if k >= len(ids):
                continue
            wanted = ids[k]
            buf = dumps[c]
            # Equipment is stored as 16-bit little-endian words (ids exceed 255, e.g. 256).
            offs = {off for off in range(len(buf) - 1)
                    if (buf[off] | (buf[off + 1] << 8)) == wanted}
            per_char_offsets.append(offs)
            detail.append(f"{label(c)}={wanted}@{{{','.join(f'0x{o:X}' for o in sorted(offs)) or '-'}}}")
        if not per_char_offsets:
            continue
        common = set.intersection(*per_char_offsets) if per_char_offsets else set()
        slot = slot_names[k] if k < len(slot_names) else f"slot{k}"
        common_txt = ",".join(f"0x{o:X}" for o in sorted(common)) or "NONE"
        print(f"  [{slot:<10}] common-offset={common_txt}")
        for d in detail:
            print(f"        {d}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("logs", nargs="+")
    ap.add_argument("--start", default="0x00")
    ap.add_argument("--end", default="0x40")
    ap.add_argument("--rank", action="store_true",
                    help="scan full 0x200 struct and rank equipment-slot candidate offsets")
    ap.add_argument("--equip", action="store_true",
                    help="decode the confirmed equipment block (words at +0x1A..+0x26) for every unit")
    ap.add_argument("--find", action="append", default=[],
                    help="ground-truth equipment, e.g. --find 0x32=32,0,49,52,53 "
                         "(charId=weapon,shield,head,body,accessory item ids; use 0 for empty). "
                         "Repeat per unit. Searches unit AND actor dumps for the exact id block.")
    args = ap.parse_args()
    start = int(args.start, 0)
    end = int(args.end, 0)

    items = load_catalog()
    paths = []
    for g in args.logs:
        paths.extend(glob.glob(g))
    units, order = parse_dumps(paths)
    if not units:
        print("No [DUMP] lines parsed.", file=sys.stderr)
        return 1

    print(f"Parsed {len(units)} distinct units: {', '.join(label(c) for c in order)}\n")

    if args.equip:
        # Decode the confirmed equipment block (16-bit LE words) for every parsed unit.
        slots = [(0x1A, "Head"), (0x1C, "Body"), (0x1E, "Accessory"),
                 (0x20, "R-hand"), (0x22, "R-slot2"), (0x24, "L-hand"), (0x26, "L-slot2")]
        for c in order:
            buf = units[c]
            parts = []
            for off, name in slots:
                if off + 1 >= len(buf):
                    continue
                w = buf[off] | (buf[off + 1] << 8)
                if w == 0xFFFF or w == 0x00FF:
                    parts.append(f"{name}=+0x{off:X}:empty")
                else:
                    it = items.get(w)
                    nm = it["name"] if it else f"id?{w}"
                    fam = (it["family"] or "-") if it else "?"
                    parts.append(f"{name}=+0x{off:X}:{nm}({w})[{fam}]")
            print(f"{label(c):<10} " + "  ".join(parts))
        return 0

    if args.find:
        slot_names = ["Weapon", "Shield", "Head", "Body", "Accessory"]
        truth = {}
        for spec in args.find:
            cid_s, ids_s = spec.split("=")
            truth[int(cid_s, 0)] = [int(x, 0) for x in ids_s.split(",")]
        for cid, ids in truth.items():
            named = ", ".join(
                f"{slot_names[i] if i < len(slot_names) else i}={items.get(v, {}).get('name', v)}({v})"
                for i, v in enumerate(ids))
            print(f"  ground-truth {label(cid)}: {named}")
        actors = parse_actor_dumps(paths)
        find_equipment_block("UNIT struct (0x200)", units, truth, slot_names)
        find_equipment_block("ACTOR struct (0x548)", actors, truth, slot_names)
        return 0

    # Family id-space sizes (sparser family => a match is stronger evidence, fewer false positives).
    fam_space = {t: 0 for t in EQUIP_TOKENS}
    for it in items.values():
        if it["family"]:
            fam_space[it["family"]] += 1

    if args.rank:
        # A real equipment slot: EVERY unit's byte is 0 (nothing) or a valid equippable family,
        # the slot varies across units, and ideally maps to a sparse family.
        cands = []
        max_off = min(len(units[c]) for c in order)
        for off in range(0, max_off):
            fams, nonzero_vals, equip_or_zero = {}, set(), 0
            for c in order:
                v = units[c][off]
                it = items.get(v)
                fam = it["family"] if it else None
                if v == 0:
                    equip_or_zero += 1
                elif fam:
                    equip_or_zero += 1
                    nonzero_vals.add(v)
                    fams[fam] = fams.get(fam, 0) + 1
            if not fams:
                continue
            top_fam, top_n = max(fams.items(), key=lambda kv: kv[1])
            frac = equip_or_zero / len(order)
            # sparsity weight: Shield(~12) strong, Weapon(~127) weak
            sparsity = 1.0 - fam_space.get(top_fam, 260) / 261.0
            # composite: must-be-all-equip dominates, then family purity, sparsity, variation
            single_fam = top_n == sum(fams.values())
            comp = (frac, single_fam, sparsity, len(nonzero_vals) / max(1, len(order)))
            cands.append((comp, off, frac, top_fam, top_n, len(nonzero_vals)))
        cands.sort(reverse=True)
        print("Ranked equipment-slot candidates (frac=all-units-equip-or-zero):")
        print(f"{'off':>5} | frac | dom-family   | famN | distinct | single-fam")
        print("-" * 60)
        for comp, off, frac, fam, n, distinct in cands[:24]:
            print(f"+0x{off:02X} | {frac:.2f} | {fam:<12} | {n:>4} | {distinct:>8} | {comp[1]}")
        print()
        return 0

    def decode(v):
        it = items.get(v)
        if not it:
            return f"{v:>3} (id?{v})"
        fam = it["family"] or "-"
        return f"{v:>3} {it['name']}[{fam}]"

    # Per-offset cross-unit view with an equipment-family consistency score.
    print(f"{'off':>5} | " + " | ".join(f"{label(c):<22}" for c in order) + " | family-score")
    print("-" * (8 + 25 * len(order) + 16))
    for off in range(start, end + 1):
        cells = []
        fam_counts = {}
        for c in order:
            v = units[c][off]
            it = items.get(v)
            fam = it["family"] if it else None
            if fam:
                fam_counts[fam] = fam_counts.get(fam, 0) + 1
            cells.append(decode(v))
        # Score: an equipment slot should map to ONE family across most units.
        score = ""
        if fam_counts:
            top_fam, top_n = max(fam_counts.items(), key=lambda kv: kv[1])
            distinct_vals = len({units[c][off] for c in order})
            score = f"{top_fam}x{top_n}/{len(order)} distinct={distinct_vals}"
        print(f"+0x{off:02X} | " + " | ".join(f"{x:<22}" for x in cells) + f" | {score}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
