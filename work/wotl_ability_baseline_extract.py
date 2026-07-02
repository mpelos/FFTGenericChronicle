# Extract WotL (PSP) vanilla ability action data from FFTPatcher resources
# and emit work/wotl_ability_action_baseline.csv
import csv, os, re, sqlite3, sys
import xml.etree.ElementTree as ET

SCRATCH = os.path.dirname(os.path.abspath(__file__))
FFTP = os.path.join(SCRATCH, "fftpatcher", "PatcherLib.Resources", "Resources")
REPO = r"D:\Projects\FFTGenericChronicle"
WORK = os.path.join(REPO, "work")

PSP_BIN = os.path.join(FFTP, "PSP", "bin", "Abilities.bin")
PSX_BIN = os.path.join(FFTP, "PSX-US", "bin", "Abilities.bin")
PSP_INFLICT = os.path.join(FFTP, "PSP", "bin", "InflictStatuses.bin")
PSP_NAMES = os.path.join(FFTP, "PSP", "Abilities", "Abilities.xml")
PSX_NAMES = os.path.join(FFTP, "PSX-US", "Abilities", "Abilities.xml")
FORMULAS = os.path.join(FFTP, "AbilityFormulas.xml")

def bits_msb(b):
    return [(b >> (7 - i)) & 1 for i in range(8)]  # index 0 = 0x80

ELEMENTS = ["Fire", "Lightning", "Ice", "Wind", "Earth", "Water", "Holy", "Dark"]  # 0x80..0x01

STATUS_NAMES = [
    "NoEffect","Crystal","Dead","Undead","Charging","Jump","Defending","Performing",
    "Petrify","Invite","Darkness","Confusion","Silence","BloodSuck","DarkEvilLooking","Treasure",
    "Oil","Float","Reraise","Transparent","Berserk","Chicken","Frog","Critical",
    "Poison","Regen","Protect","Shell","Haste","Slow","Stop","Wall",
    "Faith","Innocent","Charm","Sleep","DontMove","DontAct","Reflect","DeathSentence",
]

ABILITY_TYPES = ["Blank","Normal","Item","Throwing","Jumping","Charging","Arithmetick",
                 "Reaction","Support","Movement","Unknown1","Unknown2","Unknown3",
                 "Unknown4","Unknown5","Unknown6"]

THROW_TYPES = {  # FFTPatcher ItemSubType enum ordinals
    0:"Nothing",1:"Knife",2:"NinjaBlade",3:"Sword",4:"KnightsSword",5:"Katana",
    6:"Axe",7:"Rod",8:"Staff",9:"Flail",10:"Gun",11:"Crossbow",12:"Bow",
    13:"Instrument",14:"Book",15:"Polearm",16:"Pole",17:"Bag",18:"Cloth",
    32:"Shuriken",33:"Bomb",
}

def load_names(path):
    txt = open(path, encoding="utf-8-sig").read()
    root = ET.fromstring(txt)
    names = {}
    for a in root.iter("Ability"):
        names[int(a.get("value"))] = a.get("name") or ""
    return names

def load_formulas(path):
    txt = open(path, encoding="utf-8-sig").read()
    root = ET.fromstring(txt)
    f = {}
    for a in root.iter("Ability"):
        f[int(a.get("value"), 16)] = (a.text or "").strip()
    return f

def load_inflict(path):
    data = open(path, "rb").read()
    assert len(data) == 0x80 * 6, len(data)
    table = {}
    for i in range(0x80):
        e = data[i*6:(i+1)*6]
        fb = bits_msb(e[0])
        mode = []
        if fb[0]: mode.append("AllOrNothing")
        if fb[1]: mode.append("Random")
        if fb[2]: mode.append("Separate")
        if fb[3]: mode.append("Cancel")
        sts = []
        idx = 0
        for byte in e[1:6]:
            for bit in bits_msb(byte):
                if bit: sts.append(STATUS_NAMES[idx])
                idx += 1
        table[i] = ("+".join(mode), "|".join(sts))
    return table

def parse_abilities(binpath):
    data = open(binpath, "rb").read()
    out = {}
    for i in range(512):
        first = data[i*8:(i+1)*8]
        rec = {}
        rec["jp_cost"] = first[0] | (first[1] << 8)
        rec["learn_rate"] = first[2]
        b3 = bits_msb(first[3])
        rec["learn_with_jp"] = not b3[0]
        rec["display_ability_name"] = bool(b3[1])
        rec["learn_on_hit"] = bool(b3[2])
        rec["ability_type"] = ABILITY_TYPES[first[3] & 0x0F]
        rec["ai_flags_hex"] = f"{first[4]:02X}{first[5]:02X}{first[6]:02X}"
        b7 = bits_msb(first[7])
        rec["used_by_enemies"] = bool(b7[0])

        if 0x000 <= i <= 0x16F:  # Normal: 14-byte attributes
            s = data[0x1000 + 14*i : 0x1000 + 14*i + 14]
            rec["range"] = s[0]
            rec["aoe"] = s[1]
            rec["vertical"] = s[2]
            f3 = bits_msb(s[3])
            rec["ForceSelfTarget"] = bool(f3[0]); rec["Blank7"] = bool(f3[1])
            rec["WeaponRange"] = bool(f3[2]); rec["VerticalFixed"] = bool(f3[3])
            rec["VerticalTolerance"] = bool(f3[4]); rec["WeaponStrike"] = bool(f3[5])
            rec["Auto"] = bool(f3[6]); rec["TargetSelf"] = not f3[7]
            f4 = bits_msb(s[4])
            rec["HitEnemies"] = not f4[0]; rec["HitAllies"] = not f4[1]
            rec["TopDownTarget"] = bool(f4[2]); rec["FollowTarget"] = not f4[3]
            rec["RandomFire"] = bool(f4[4]); rec["LinearAttack"] = bool(f4[5])
            rec["ThreeDirections"] = bool(f4[6]); rec["HitCaster"] = not f4[7]
            f5 = bits_msb(s[5])
            rec["Reflectable"] = bool(f5[0]); rec["Arithmetickable"] = bool(f5[1])
            rec["Silenceable"] = not f5[2]; rec["Mimicable"] = not f5[3]
            rec["NormalAttack"] = bool(f5[4]); rec["Persevere"] = bool(f5[5])
            rec["ShowQuote"] = bool(f5[6]); rec["AnimateOnMiss"] = bool(f5[7])
            f6 = bits_msb(s[6])
            rec["CounterFlood"] = bool(f6[0]); rec["CounterMagic"] = bool(f6[1])
            rec["Direct"] = bool(f6[2]); rec["Shirahadori"] = bool(f6[3])
            rec["RequiresSword"] = bool(f6[4]); rec["RequiresMateriaBlade"] = bool(f6[5])
            rec["Evadeable"] = bool(f6[6]); rec["Targeting"] = not f6[7]
            elems = [ELEMENTS[k] for k, bit in enumerate(bits_msb(s[7])) if bit]
            rec["elements"] = "|".join(elems)
            rec["formula"] = s[8]
            rec["x"] = s[9]
            rec["y"] = s[10]
            rec["inflict_status"] = s[11]
            rec["ct"] = s[12]
            rec["mp_cost"] = s[13]
            rec["raw14"] = s.hex().upper()
        elif 0x170 <= i <= 0x17D:
            rec["type_specific"] = f"item_id=0x{data[0x2420 + i - 0x170]:02X}"
        elif 0x17E <= i <= 0x189:
            v = data[0x2430 + i - 0x17E]
            rec["type_specific"] = f"throw_type={THROW_TYPES.get(v, hex(v))}"
        elif 0x18A <= i <= 0x195:
            o = 0x243C + (i - 0x18A) * 2
            rec["type_specific"] = f"jump_range={data[o]},jump_vertical={data[o+1]}"
        elif 0x196 <= i <= 0x19D:
            o = 0x2454 + (i - 0x196) * 2
            rec["type_specific"] = f"charge_ct={data[o]},charge_power={data[o+1]}"
        elif 0x19E <= i <= 0x1A5:
            rec["type_specific"] = f"arithmetick_skill=0x{data[0x2464 + i - 0x19E]:02X}"
        else:  # 0x1A6-0x1FF: reaction/support/movement etc.
            rec["type_specific"] = f"other_id=0x{data[0x246C + i - 0x1A6]:02X}"
        out[i] = rec
    return out

def main():
    psp = parse_abilities(PSP_BIN)
    psx = parse_abilities(PSX_BIN)
    names_psp = load_names(PSP_NAMES)
    names_psx = load_names(PSX_NAMES)
    formulas = load_formulas(FORMULAS)
    inflict = load_inflict(PSP_INFLICT)

    # IVC names
    ivc_names = {}
    con = sqlite3.connect(os.path.join(WORK, "ability_en.sqlite"))
    for k, n in con.execute('select Key, Name from "Ability-en"'):
        ivc_names[k] = n or ""

    flags = ["ForceSelfTarget","Blank7","WeaponRange","VerticalFixed","VerticalTolerance",
             "WeaponStrike","Auto","TargetSelf","HitEnemies","HitAllies","TopDownTarget",
             "FollowTarget","RandomFire","LinearAttack","ThreeDirections","HitCaster",
             "Reflectable","Arithmetickable","Silenceable","Mimicable","NormalAttack",
             "Persevere","ShowQuote","AnimateOnMiss","CounterFlood","CounterMagic",
             "Direct","Shirahadori","RequiresSword","RequiresMateriaBlade","Evadeable","Targeting"]

    cols = (["id_hex","id_dec","name_wotl","name_psx","name_ivc","ability_type",
             "formula_hex","formula_text","x","y","range","aoe","vertical","ct","mp_cost",
             "elements","inflict_status_hex","inflict_status_mode","inflict_statuses"]
            + flags
            + ["type_specific","jp_cost","used_by_enemies","psx_action_diff","raw14_psp","source"])

    diffs = []
    outpath = os.path.join(WORK, "wotl_ability_action_baseline.csv")
    with open(outpath, "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=cols)
        w.writeheader()
        for i in range(512):
            r = psp[i]
            row = {c: "" for c in cols}
            row["id_hex"] = f"0x{i:03X}"
            row["id_dec"] = i
            row["name_wotl"] = names_psp.get(i, "")
            row["name_psx"] = names_psx.get(i, "")
            row["name_ivc"] = ivc_names.get(i, "")
            row["ability_type"] = r["ability_type"]
            row["jp_cost"] = r["jp_cost"]
            row["used_by_enemies"] = r["used_by_enemies"]
            row["source"] = "FFTPatcher PSP(WotL) vanilla Abilities.bin"
            if "formula" in r:
                row["formula_hex"] = f"0x{r['formula']:02X}"
                row["formula_text"] = formulas.get(r["formula"], "")
                for k in ["x","y","range","aoe","vertical","ct","mp_cost","elements"]:
                    row[k] = r[k]
                row["inflict_status_hex"] = f"0x{r['inflict_status']:02X}"
                if r["formula"] == 0x02:
                    # formula 02: InflictStatus byte is a spell id to cast, not a status entry
                    row["inflict_status_mode"] = "CastSpell"
                    row["inflict_statuses"] = names_psp.get(r["inflict_status"], "")
                elif r["inflict_status"]:
                    m, s = inflict[r["inflict_status"]]
                    row["inflict_status_mode"] = m
                    row["inflict_statuses"] = s
                for f in flags:
                    row[f] = int(r[f])
                row["raw14_psp"] = r["raw14"]
                if psx[i].get("raw14") != r["raw14"]:
                    row["psx_action_diff"] = 1
                    diffs.append(i)
            else:
                row["type_specific"] = r.get("type_specific", "")
        # second pass to keep order; write inside loop instead
            w.writerow(row)

    print("wrote", outpath)
    print("PSX-vs-PSP action-data diffs:", len(diffs))
    print("diff ids:", ", ".join(f"0x{i:03X}({names_psp.get(i,'')}/{names_psx.get(i,'')})" for i in diffs))

    # Spot-checks
    print("\n--- spot checks (WotL name / IVC name / formula / X / Y / rng / aoe / ct / mp / elem) ---")
    for i in [0x01, 0x02, 0x10, 0x11, 0x1F, 0x15, 0x60, 0x150, 0x161, 0x09, 0x0A, 0x0B, 0x0C, 0x21, 0x23]:
        r = psp[i]
        print(f"0x{i:03X} {names_psp.get(i,''):22s} ivc={ivc_names.get(i,''):22s} "
              f"f=0x{r.get('formula',0):02X} x={r.get('x','')} y={r.get('y','')} rng={r.get('range','')} "
              f"aoe={r.get('aoe','')} ct={r.get('ct','')} mp={r.get('mp_cost','')} elem={r.get('elements','')}")

    # name agreement WotL vs IVC
    same = sum(1 for i in range(512) if names_psp.get(i,"").lower() == (ivc_names.get(i) or "").lower() and (ivc_names.get(i) or ""))
    named_ivc = sum(1 for i in range(512) if ivc_names.get(i))
    print(f"\nIVC named rows: {named_ivc}; exact (case-insensitive) match with WotL names: {same}")
    mism = [(i, names_psp.get(i,''), ivc_names.get(i,'')) for i in range(512)
            if (ivc_names.get(i) or '') and names_psp.get(i,'').lower() != ivc_names[i].lower()]
    print("mismatches:", len(mism))
    for i, a, b in mism[:40]:
        print(f"  0x{i:03X} wotl={a!r} ivc={b!r}")

if __name__ == "__main__":
    main()
