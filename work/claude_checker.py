#!/usr/bin/env python3
"""Claude-side INDEPENDENT simulator for the dual gate (doc 05).

Independent re-implementation from the sim-inputs-v0 spec (NOT shared with GPT's
tools/sim_damage.py). Loads work/sim-inputs-v0.json verbatim. Deterministic.
First sweep: each family's representative attacker vs all four armor classes at
the late band, plus a magic baseline, with a quick viability/dominance/plate read.
"""
import json, os, math

HERE = os.path.dirname(os.path.abspath(__file__))
B = json.load(open(os.path.join(HERE, "sim-inputs-v0.json")))
CALC = B["calc"]; CEIL = CALC["penetration_ceiling"]
LO, HI = CALC["combined_multiplier_clamp"]; FLOOR = CALC["chip_floor"]
BR = CALC["default_brave"]

# representative attacker job per family (late band)
REP = {
    "sword": "Knight", "knight_sword": "Knight", "katana": "Samurai",
    "knife": "Thief", "ninja_blade": "Ninja", "longbow": "Archer",
    "crossbow": "Archer", "gun": "Chemist", "spear": "Dragoon",
    "staff": "White Mage", "rod": "Black Mage", "pole": "Mystic",
    "axe": "Geomancer", "flail": "Squire", "fists": "Monk",
    "instrument": "Bard", "book": "Mystic", "cloth_weapon": "Dancer", "bag": "Dancer",
}

def stats(job, band="late"):
    return B["jobs"][job]["bands"][band]

def pressure(fam, st):
    r = fam["routine"]; wp = fam["wp"]; pa = st["pa"]; ma = st["ma"]; spd = st["spd"]
    if r == "pa_wp":      return pa * wp
    if r == "br_pa_wp":   return (pa * BR // 100) * wp
    if r == "spd_pa_wp":  return ((pa + spd) // 2) * wp
    if r == "ma_wp":      return ma * wp
    if r == "rdm_pa_wp":  return ((pa + 1) / 2) * wp          # expected
    if r == "wp_wp":      return wp * wp
    if r == "br_pa_pa":   return (pa * BR // 100) * pa
    if r == "pampa_wp":   return ((pa + ma) // 2) * wp
    raise ValueError(r)

def resp(armor_class, dtype, pen):
    base = B["armor_response"][armor_class][dtype]
    e = base + pen * (CEIL - base) if base < CEIL else base
    return min(HI, max(LO, e))

def dmg(fam, st, armor_class):
    p = pressure(fam, st)
    m = resp(armor_class, fam["damage_type"], fam["penetration"])
    return max(FLOOR, math.floor(p * m))

CLASSES = ["plate", "mail", "leather", "cloth"]
print(f"LATE band. attacker per family. damage vs each armor class.\n")
print(f"{'family':<13}{'atk':<10}{'type':<8}{'pres':>5}  " + "".join(f"{c:>8}" for c in CLASSES) + "   best")
rows = []
for name, fam in B["families"].items():
    job = REP[name]; st = stats(job)
    p = pressure(fam, st)
    ds = {c: dmg(fam, st, c) for c in CLASSES}
    best = max(ds, key=ds.get)
    rows.append((name, fam["damage_type"], ds, best))
    print(f"{name:<13}{job:<10}{fam['damage_type']:<8}{p:>5.0f}  " +
          "".join(f"{ds[c]:>8}" for c in CLASSES) + f"   {best}")

# magic baseline (mid spell), ignores armor; vs cloth target faith 70
mK = B["magic"]["sample_spells"]["mid"]; bm = stats("Black Mage")
mdmg = round(mK * bm["ma"] * 0.70 * 0.70)
print(f"\n{'black_magic':<13}{'Black Mage':<10}{'magic':<8}{'-':>5}  magic (ignores armor) ~ {mdmg} vs Faith70 (Shell ->x.667={round(mdmg*0.667)})")

# quick reads
print("\n--- DOMINANCE: who is best vs each armor class (by expected dmg) ---")
for c in CLASSES:
    rank = sorted(rows, key=lambda r: -r[2][c])[:4]
    print(f"  vs {c:<8}: " + ", ".join(f"{n}({d[c]})" for n, t, d, b in rank))

print("\n--- PLATE MATCHUP CHECK (crush should top; gun should be ~neutral) ---")
plate_rank = sorted(rows, key=lambda r: -r[2]["plate"])
print("  top vs plate: " + ", ".join(f"{n}({d['plate']})" for n, t, d, b in plate_rank[:6]))

print("\n--- VIABILITY: each family's best-target dmg vs global best ---")
gbest = max(r[2][r[3]] for r in rows)
for n, t, d, b in sorted(rows, key=lambda r: -r[2][r[3]]):
    pct = d[b] / gbest * 100
    flag = "" if pct >= 50 else "  <-- WEAK"
    print(f"  {n:<13} best={d[b]:>5} vs {b:<8} ({pct:4.0f}% of global best){flag}")
