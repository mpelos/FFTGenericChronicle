#!/usr/bin/env python3
"""Generate effective unit stats per anchor job per phase band for sim-inputs-v0.

Data provenance:
  - Job multipliers: work/baseline_jobs.csv (verified-local, generic block 74-92).
  - Raw stat bases per phase band: WotL-fallback (no IVC level-curve dump yet).
Stat model (shared, documented): effective = round(raw_base[band] * multiplier / 100).
This is a comparability substrate for the dual-independent sim (doc 05); both sims
load identical numbers so any output divergence is pure formula logic, not inputs.
"""
import csv, json, os

HERE = os.path.dirname(os.path.abspath(__file__))
JOBS = os.path.join(HERE, "baseline_jobs.csv")
OUT = os.path.join(HERE, "sim_inputs_v0_stats.json")

ANCHORS = set(range(74, 93))  # generic player job block

# WotL-fallback raw bases per phase band. Tunable; labeled non-verified.
BANDS = {
    "early":  {"pa": 5,  "ma": 5,  "spd": 6, "hp_raw": 150},
    "mid":    {"pa": 8,  "ma": 8,  "spd": 7, "hp_raw": 280},
    "late":   {"pa": 10, "ma": 10, "spd": 8, "hp_raw": 430},
    "stress": {"pa": 12, "ma": 12, "spd": 9, "hp_raw": 520},
}

def eff(raw, mult):
    return round(raw * mult / 100)

rows = {}
with open(JOBS, newline="") as f:
    for r in csv.DictReader(f):
        jid = int(r["Id"])
        if jid not in ANCHORS or not r["Name"].strip():
            continue
        hpm = int(r["HPMultiplier"]); spm = int(r["SpeedMultiplier"])
        pam = int(r["PAMultiplier"]); mam = int(r["MAMultiplier"])
        bands = {}
        for b, base in BANDS.items():
            bands[b] = {
                "hp":  eff(base["hp_raw"], hpm),
                "pa":  eff(base["pa"], pam),
                "ma":  eff(base["ma"], mam),
                "spd": eff(base["spd"], spm),
            }
        rows[r["Name"]] = {
            "job_id": jid,
            "multipliers": {"hp": hpm, "spd": spm, "pa": pam, "ma": mam},
            "bands": bands,
        }

bundle = {
    "version": "sim-inputs-v0/stats",
    "provenance": {
        "multipliers": "verified-local (work/baseline_jobs.csv, block 74-92)",
        "raw_bases": "WotL-fallback (no IVC level curve dumped yet)",
        "stat_model": "effective = round(raw_base[band] * multiplier / 100)",
    },
    "phase_bands": BANDS,
    "jobs": rows,
}

with open(OUT, "w") as f:
    json.dump(bundle, f, indent=2)

# console preview: late-band effective stats sorted by PA
print("LATE-band effective stats (anchor jobs), by PA desc:")
print(f"{'job':<14}{'PA':>4}{'MA':>4}{'SPD':>4}{'HP':>6}")
for name, d in sorted(rows.items(), key=lambda kv: -kv[1]['bands']['late']['pa']):
    l = d["bands"]["late"]
    print(f"{name:<14}{l['pa']:>4}{l['ma']:>4}{l['spd']:>4}{l['hp']:>6}")
print(f"\nwrote {OUT}")
