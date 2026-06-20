#!/usr/bin/env python3
"""Build the full pinned sim-inputs-v0 bundle for the dual-independent sim (doc 05).

Both simulators (GPT's tools/sim_damage.py and Claude's independent checker) load
THIS exact artifact, so any output divergence is pure formula logic, not inputs.

Provenance labels:
  - job multipliers / effective stats: verified-local (work/baseline_jobs.csv 74-92)
  - raw stat bases per band, weapon WP, armor responses, calc constants: WotL-fallback
    / design-provisional (no IVC weapon dump yet -> work/baseline_weapons.csv empty).
Result class for anything using this: "conceptually viable, pending verified-baseline re-sim".
"""
import csv, json, os

HERE = os.path.dirname(os.path.abspath(__file__))
JOBS = os.path.join(HERE, "baseline_jobs.csv")
OUT = os.path.join(HERE, "sim-inputs-v0.json")
ANCHORS = set(range(74, 93))

# ---- stat model (shared) -------------------------------------------------
BANDS = {
    "early":  {"pa": 5,  "ma": 5,  "spd": 6, "hp_raw": 150},
    "mid":    {"pa": 8,  "ma": 8,  "spd": 7, "hp_raw": 280},
    "late":   {"pa": 10, "ma": 10, "spd": 8, "hp_raw": 430},
    "stress": {"pa": 12, "ma": 12, "spd": 9, "hp_raw": 520},
}
eff = lambda raw, mult: round(raw * mult / 100)

# ---- which armor class each anchor TARGET wears (from equip lists) --------
ARMOR_CLASS = {
    "Knight": "plate", "Dragoon": "plate",
    "Squire": "mail", "Geomancer": "mail", "Samurai": "mail",
    "Archer": "leather", "Thief": "leather", "Ninja": "leather",
    "Chemist": "leather", "Orator": "leather", "Dancer": "leather",
    "White Mage": "cloth", "Black Mage": "cloth", "Time Mage": "cloth",
    "Summoner": "cloth", "Mystic": "cloth", "Arithmetician": "cloth",
    "Bard": "cloth", "Monk": "cloth",
}

# ---- weapon families: routine, physical damage type, WotL-fallback WP,
#      and family-fixed penetration in [0,1] (Q2: delivery maps into a
#      lower-resisted bucket; implemented as response-softening toward ceiling) -
ROUTINES = {
    "pa_wp": "PA*WP",
    "br_pa_wp": "floor(PA*Br/100)*WP",
    "spd_pa_wp": "floor((PA+SPD)/2)*WP",
    "ma_wp": "MA*WP",
    "rdm_pa_wp": "Rdm(1..PA)*WP  (expected=(PA+1)/2*WP)",
    "wp_wp": "WP*WP",
    "br_pa_pa": "floor(PA*Br/100)*PA",
    "pampa_wp": "floor((PA+MA)/2)*WP",
}
FAMILIES = {
    # name            routine        type      WP  pen   note
    "sword":        ("pa_wp",      "swing",   16, 0.00),
    "knight_sword": ("br_pa_wp",   "swing",   20, 0.00),
    "katana":       ("br_pa_wp",   "swing",   18, 0.00),
    "knife":        ("spd_pa_wp",  "thrust",  12, 0.10),
    "ninja_blade":  ("spd_pa_wp",  "swing",   13, 0.00),
    "longbow":      ("spd_pa_wp",  "missile", 14, 0.15),
    "crossbow":     ("pa_wp",      "missile", 14, 0.35),
    "gun":          ("wp_wp",      "missile",  9, 0.70),  # anti-plate identity
    "spear":        ("pa_wp",      "thrust",  18, 0.20),  # armor-piercing thrust
    "staff":        ("ma_wp",      "crush",   14, 0.00),
    "rod":          ("pa_wp",      "crush",   10, 0.00),
    "pole":         ("ma_wp",      "crush",   16, 0.00),
    "axe":          ("rdm_pa_wp",  "crush",   20, 0.00),
    "flail":        ("rdm_pa_wp",  "crush",   18, 0.00),
    "fists":        ("br_pa_pa",   "crush",    0, 0.15),  # WP n/a (uses PA)
    "instrument":   ("pampa_wp",   "missile", 10, 0.00),
    "book":         ("pampa_wp",   "crush",   12, 0.00),
    "cloth_weapon": ("pampa_wp",   "swing",   12, 0.00),
    "bag":          ("rdm_pa_wp",  "crush",   14, 0.00),
}

# ---- armor-class coarse type responses (STARTING buckets; sim tunes these) -
# Marcelo's hard constraint: plate LOW vs swing AND thrust, HIGH vs crush.
ARMOR_RESPONSE = {
    #          swing thrust crush missile
    "plate":   {"swing": 0.65, "thrust": 0.65, "crush": 1.15, "missile": 0.80},
    "mail":    {"swing": 0.75, "thrust": 1.10, "crush": 0.95, "missile": 1.00},
    "leather": {"swing": 0.95, "thrust": 0.95, "crush": 1.00, "missile": 0.95},
    "cloth":   {"swing": 1.00, "thrust": 1.00, "crush": 1.00, "missile": 1.00},
}

# ---- magic axis (Q6): armor class does NOT mitigate magic; Shell/element/faith do
MAGIC = {
    "routine": "K*MA*(CFa/100)*(TFa/100)",
    "sample_spells": {"low": 14, "mid": 20, "high": 26},  # K (spell power), WotL-fallback
    "shell_multiplier": 0.667,
    "note": "magic ignores physical armor class; mitigated by Shell, element, target Faith.",
}

# ---- shared calc-spec constants (C model) --------------------------------
CALC = {
    "pipeline": "pressure(routine) -> x type_response(with penetration) -> "
                "x protect/shell -> x element -> x zodiac -> clamp -> floor (chip_floor)",
    "operation_order": ["type_response", "protect_shell", "element", "zodiac"],
    "penetration_ceiling": 1.10,
    "penetration_formula": "eff_resp = base + pen*(ceiling-base) if base<ceiling else base",
    "combined_multiplier_clamp": [0.25, 2.5],  # stacking discipline: bounds blowups
    "chip_floor": 1,
    "protect_multiplier": 0.667,   # FFT Protect ~ x2/3 physical
    "zodiac": {"good": 1.25, "neutral": 1.00, "bad": 0.75},
    "default_brave": 70,
    "default_faith": 70,
}

# ---- build stats from real multipliers -----------------------------------
jobs = {}
with open(JOBS, newline="") as f:
    for r in csv.DictReader(f):
        jid = int(r["Id"])
        if jid not in ANCHORS or not r["Name"].strip():
            continue
        hpm, spm = int(r["HPMultiplier"]), int(r["SpeedMultiplier"])
        pam, mam = int(r["PAMultiplier"]), int(r["MAMultiplier"])
        jobs[r["Name"]] = {
            "job_id": jid,
            "armor_class": ARMOR_CLASS.get(r["Name"], "leather"),
            "multipliers": {"hp": hpm, "spd": spm, "pa": pam, "ma": mam},
            "bands": {b: {"hp": eff(v["hp_raw"], hpm), "pa": eff(v["pa"], pam),
                          "ma": eff(v["ma"], mam), "spd": eff(v["spd"], spm)}
                      for b, v in BANDS.items()},
        }

bundle = {
    "version": "sim-inputs-v0",
    "provenance": {
        "multipliers_stats": "verified-local (work/baseline_jobs.csv, block 74-92)",
        "raw_bases": "WotL-fallback (no IVC level curve)",
        "weapon_wp": "WotL-fallback / design-provisional (work/baseline_weapons.csv empty)",
        "armor_response": "design-provisional starting buckets (sim tunes)",
        "result_class": "conceptually viable, pending verified-baseline re-sim",
    },
    "stat_model": "effective = round(raw_base[band] * multiplier / 100)",
    "phase_bands": BANDS,
    "routines": ROUTINES,
    "families": {k: {"routine": v[0], "damage_type": v[1], "wp": v[2], "penetration": v[3]}
                 for k, v in FAMILIES.items()},
    "armor_response": ARMOR_RESPONSE,
    "magic": MAGIC,
    "calc": CALC,
    "jobs": jobs,
}

with open(OUT, "w") as f:
    json.dump(bundle, f, indent=2)
print(f"wrote {OUT}")
print(f"families={len(bundle['families'])} jobs={len(jobs)} armor_classes={len(ARMOR_RESPONSE)}")
