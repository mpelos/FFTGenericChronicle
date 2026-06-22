#!/usr/bin/env python3
"""Tuning experiment v0.1: apply candidate overrides to the bundle in-memory,
rerun the representative sweep, and score viability/dominance/plate.
This is a PROPOSAL generator, not a committed bundle change."""
import json, os, math, copy

HERE = os.path.dirname(os.path.abspath(__file__))
B0 = json.load(open(os.path.join(HERE, "sim-inputs-v0.json")))

# ---- candidate v0.1 overrides --------------------------------------------
OV = {
    "families": {
        "spear":  {"wp": 15, "penetration": 0.10},   # temper top dominator
        "pole":   {"wp": 13},                          # temper broad mage-crush
        "gun":    {"wp": 12},                          # anti-armor + PA-indep, now viable
        "rod":    {"routine": "ma_wp", "wp": 10},      # mage's crush stick (was PA*WP, absurd on mages)
        "flail":  {"wp": 24},                          # volatile floor lift
        "bag":    {"wp": 20},
        "book":   {"wp": 15},
        "longbow":{"wp": 15},
        "cloth_weapon": {"wp": 14},
    },
    "armor_response": {
        "mail": {"missile": 1.10},   # doc-10 says mail is HIGH vs missile; give missile a mail identity
    },
}

def apply(b, ov):
    b = copy.deepcopy(b)
    for fam, ch in ov.get("families", {}).items():
        b["families"][fam].update(ch)
    for ac, ch in ov.get("armor_response", {}).items():
        b["armor_response"][ac].update(ch)
    return b

REP = {
    "sword":"Knight","knight_sword":"Knight","katana":"Samurai","knife":"Thief",
    "ninja_blade":"Ninja","longbow":"Archer","crossbow":"Archer","gun":"Chemist",
    "spear":"Dragoon","staff":"White Mage","rod":"Black Mage","pole":"Mystic",
    "axe":"Geomancer","flail":"Squire","fists":"Monk","instrument":"Bard",
    "book":"Mystic","cloth_weapon":"Dancer","bag":"Dancer",
}
# families whose identity is NOT raw single-hit damage (role-exempt from strict viability)
ROLE_EXEMPT = {"instrument", "knife", "ninja_blade"}  # support/joblock + dual-wield (needs Two Swords)
CLASSES = ["plate","mail","leather","cloth"]

def run(b, label):
    CALC=b["calc"]; CEIL=CALC["penetration_ceiling"]; LO,HI=CALC["combined_multiplier_clamp"]
    FLOOR=CALC["chip_floor"]; BR=CALC["default_brave"]
    def pres(fam,st):
        r=fam["routine"];wp=fam["wp"];pa=st["pa"];ma=st["ma"];spd=st["spd"]
        return {"pa_wp":pa*wp,"br_pa_wp":(pa*BR//100)*wp,"spd_pa_wp":((pa+spd)//2)*wp,
                "ma_wp":ma*wp,"rdm_pa_wp":((pa+1)/2)*wp,"wp_wp":wp*wp,
                "br_pa_pa":(pa*BR//100)*pa,"pampa_wp":((pa+ma)//2)*wp}[r]
    def dmg(fam,st,ac):
        base=b["armor_response"][ac][fam["damage_type"]];pen=fam["penetration"]
        e=base+pen*(CEIL-base) if base<CEIL else base
        return max(FLOOR,math.floor(pres(fam,st)*min(HI,max(LO,e))))
    rows=[]
    for name,fam in b["families"].items():
        st=b["jobs"][REP[name]]["bands"]["late"]
        ds={c:dmg(fam,st,c) for c in CLASSES}
        rows.append((name,fam["damage_type"],ds,max(ds,key=ds.get)))
    combat=[r for r in rows if r[0] not in ROLE_EXEMPT]
    cbest=max(r[2][r[3]] for r in combat)
    print(f"\n===== {label} =====")
    print(f"combat-family best (benchmark) = {cbest}")
    # dominance: best per class
    print("best vs each class:")
    for c in CLASSES:
        top=sorted(rows,key=lambda r:-r[2][c])[0]
        print(f"  {c:<8}-> {top[0]} ({top[2][c]})")
    bestcount={}
    for c in CLASSES:
        w=sorted(rows,key=lambda r:-r[2][c])[0][0]; bestcount[w]=bestcount.get(w,0)+1
    dom=[k for k,v in bestcount.items() if v>=3]
    print(f"dominance (best in >=3 of 4 classes): {dom if dom else 'NONE'}")
    # viability at 0.65 of combat best (role-exempt excluded)
    THRESH=0.65
    weak=[]
    for n,t,d,btarget in sorted(rows,key=lambda r:-r[2][r[3]]):
        pct=d[btarget]/cbest
        tag=" [role-exempt]" if n in ROLE_EXEMPT else ("" if pct>=THRESH else "  <-- WEAK")
        print(f"  {n:<13} best={d[btarget]:>4} vs {btarget:<8} ({pct*100:3.0f}%){tag}")
        if n not in ROLE_EXEMPT and pct<THRESH: weak.append(n)
    print(f"VIABILITY weak (<{int(THRESH*100)}% combat best, non-exempt): {weak if weak else 'NONE -> PASS'}")
    return rows

run(B0, "BASELINE v0")
B1 = apply(B0, OV)
run(B1, "CANDIDATE v0.1")
