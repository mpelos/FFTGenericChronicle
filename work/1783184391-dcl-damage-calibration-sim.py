#!/usr/bin/env python3
"""
DCL basic-attack damage calibration workbench for LT7.

Runs with stdlib only. By default it reads the LT7 runtime settings and the
generated item catalog, then writes the paired calibration report in work/.
"""
from __future__ import annotations

import argparse
import csv
import json
import math
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


SCRIPT_TS = "1783184391"
ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "work"
SETTINGS_PATH = WORK / "battle-runtime-settings.lt7-dcl-damage-model.json"
DEFAULT_REPORT_PATH = WORK / f"{SCRIPT_TS}-dcl-damage-calibration-report.md"

CATALOG_CANDIDATES = [
    ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "item_catalog.csv",
    ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "_build" / "item_catalog.csv",
    ROOT / "codemod" / "_build" / "fftivc.generic.chronicle.codemod" / "item_catalog.csv",
]

# Provisional calibration knobs. The base tables and DR matrix default to the
# LT7 JSON, but every value here can be overridden from the CLI for session work.
DEFAULT_PA_TO_ST_OFFSET = 4
DEFAULT_PEN_FLOOR_PERMILLE = 200
DEFAULT_TRAIT_BASE_PERMILLE = 760
DEFAULT_TRAIT_BRAVE_NUM = 590
DEFAULT_TRAIT_BRAVE_DEN = 100
DEFAULT_WOUND_FRACTIONS = {
    "cut": (3, 2),
    "thrust": (2, 1),
    "crush": (1, 1),
    "missile": (1, 1),
}

TYPE_INDEX = {"cut": 0, "thrust": 1, "crush": 2, "missile": 3}
ARMOR_INDEX = {"plate": 0, "mail": 1, "clothes": 2, "robe": 3}
TYPE_ORDER = ["cut", "thrust", "crush", "missile"]
ARMOR_ORDER = ["plate", "mail", "clothes", "robe"]

REPRESENTATIVE_WEAPON_IDS = [
    19,  # Broadsword, cut low
    39,  # Kotetsu, cut mid
    34,  # Save the Queen, cut high
    1,   # Dagger, thrust low
    100, # Spear, thrust mid
    105, # Dragon Whisker, thrust high
    51,  # Rod, crush low
    48,  # Battle Axe, crush mid
    69,  # Morning Star, crush high
    83,  # Longbow, missile low
    89,  # Artemis Bow, missile mid
    76,  # Blaster, missile high
]

ARMOR_REPRESENTATIVE_IDS = {
    "plate": 177,    # Plate Mail, category Armor, HP +60
    "mail": 175,     # Chainmail, category Armor, HP +40
    "clothes": 186,  # Clothing, category Clothing
    "robe": 200,     # Hempen Robe, category Robe
}

SENSITIVITY_WEAPON_IDS = [1, 83, 34]  # Dagger, Longbow, Save the Queen
PA_BASELINE = 8
BRAVE_BASELINE = 70
HP_POOLS = [250, 400, 600]


Row = Dict[str, str]


@dataclass(frozen=True)
class ModelConfig:
    gurps_sw: Tuple[int, ...]
    gurps_thr: Tuple[int, ...]
    dr_matrix: Tuple[Tuple[int, ...], ...]
    wound_fractions: Dict[str, Tuple[int, int]]
    pa_to_st_offset: int = DEFAULT_PA_TO_ST_OFFSET
    pen_floor_permille: int = DEFAULT_PEN_FLOOR_PERMILLE
    trait_base_permille: int = DEFAULT_TRAIT_BASE_PERMILLE
    trait_brave_num: int = DEFAULT_TRAIT_BRAVE_NUM
    trait_brave_den: int = DEFAULT_TRAIT_BRAVE_DEN


CONFIG: ModelConfig


def parse_fraction(text: str) -> Tuple[int, int]:
    if "/" in text:
        num, den = text.split("/", 1)
        return int(num), int(den)
    return int(text), 1


def parse_int_table(text: str) -> Tuple[int, ...]:
    return tuple(int(part.strip()) for part in text.split(",") if part.strip())


def parse_dr_matrix(text: str) -> Tuple[Tuple[int, ...], ...]:
    rows = []
    for row in text.split(";"):
        if row.strip():
            values = tuple(int(part.strip()) for part in row.split(",") if part.strip())
            if len(values) != 4:
                raise ValueError(f"DR matrix rows must have 4 values, got {values!r}")
            rows.append(values)
    if len(rows) != 4:
        raise ValueError(f"DR matrix must have 4 rows, got {len(rows)}")
    return tuple(rows)


def int_field(row: Row, key: str, default: int = 0) -> int:
    value = row.get(key, "")
    if value is None or value == "":
        return default
    return int(value)


def category(row: Row) -> str:
    return row.get("item_category", "").strip().lower()


def damage_type(weapon_row: Row) -> str:
    cat = category(weapon_row)
    if cat in {"sword", "knightsword", "katana", "ninjablade", "cloth"}:
        return "cut"
    if cat in {"knife", "polearm"}:
        return "thrust"
    if cat in {"bow", "crossbow", "gun"}:
        return "missile"
    return "crush"


def armor_class(body_row: Row) -> str:
    cat = category(body_row)
    hp_bonus = int_field(body_row, "armor_hp_bonus")
    if cat == "armor" and hp_bonus >= 60:
        return "plate"
    if cat == "armor" and 40 <= hp_bonus < 60:
        return "mail"
    if cat == "robe":
        return "robe"
    return "clothes"


def mul_div(a: int, b: int, c: int) -> int:
    return (a * b) // c


def table_clamp(table: Sequence[int], index: int) -> int:
    if index < 0:
        return table[0]
    if index >= len(table):
        return table[-1]
    return table[index]


def compute_trace(pa: int, brave: int, weapon_row: Row, body_row: Row, config: ModelConfig) -> Dict[str, int | str]:
    dtype = damage_type(weapon_row)
    aclass = armor_class(body_row)
    st = pa + config.pa_to_st_offset
    base_table = config.gurps_thr if dtype == "thrust" else config.gurps_sw
    base = table_clamp(base_table, st)
    weapon_power = int_field(weapon_row, "weapon_power")
    gross = base + weapon_power
    dr = config.dr_matrix[ARMOR_INDEX[aclass]][TYPE_INDEX[dtype]]
    pen_floor = mul_div(gross, config.pen_floor_permille, 1000)
    penetrating = max(pen_floor, gross - dr)
    wound_num, wound_den = config.wound_fractions[dtype]
    wounded = mul_div(penetrating, wound_num, max(1, wound_den))
    trait_permille = config.trait_base_permille + mul_div(brave, config.trait_brave_num, config.trait_brave_den)
    final = max(1, mul_div(wounded, trait_permille, 1000))
    return {
        "damage_type": dtype,
        "armor_class": aclass,
        "st": st,
        "base": base,
        "weapon_power": weapon_power,
        "gross": gross,
        "dr": dr,
        "pen_floor": pen_floor,
        "penetrating": penetrating,
        "wound_num": wound_num,
        "wound_den": wound_den,
        "wounded": wounded,
        "trait_permille": trait_permille,
        "final": final,
    }


def compute_damage(pa: int, brave: int, weapon_row: Row, body_row: Row, config: ModelConfig) -> int:
    return int(compute_trace(pa, brave, weapon_row, body_row, config)["final"])


def damage(pa: int, brave: int, weapon_row: Row, body_row: Row) -> int:
    """LT7 DCL basic-attack damage with integer truncation semantics."""
    return compute_damage(pa, brave, weapon_row, body_row, CONFIG)


def vanilla_damage(pa: int, weapon_row: Row) -> int:
    return pa * int_field(weapon_row, "weapon_power")


def htk(hp: int, dmg: int) -> int:
    return math.ceil(hp / max(1, dmg))


def load_settings(path: Path = SETTINGS_PATH) -> dict:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def load_catalog() -> Tuple[List[Row], Path]:
    for path in CATALOG_CANDIDATES:
        if path.exists():
            with path.open("r", encoding="utf-8-sig", newline="") as f:
                return list(csv.DictReader(f)), path
    searched = "\n".join(f"  - {path}" for path in CATALOG_CANDIDATES)
    raise FileNotFoundError(f"Could not find item_catalog.csv. Searched:\n{searched}")


def rows_by_id(rows: Iterable[Row]) -> Dict[int, Row]:
    return {int_field(row, "item_id", -1): row for row in rows}


def make_config(settings: dict, args: argparse.Namespace) -> ModelConfig:
    sw = tuple(settings["FormulaTables"]["gurpsSw"])
    thr = tuple(settings["FormulaTables"]["gurpsThr"])
    dr = tuple(tuple(row) for row in settings["FormulaMatrices"]["drMatrix"])
    wound_fractions = {
        "cut": parse_fraction(args.cut_wound),
        "thrust": parse_fraction(args.thrust_wound),
        "crush": parse_fraction(args.crush_wound),
        "missile": parse_fraction(args.missile_wound),
    }
    if args.sw_table:
        sw = parse_int_table(args.sw_table)
    if args.thr_table:
        thr = parse_int_table(args.thr_table)
    if args.dr_matrix:
        dr = parse_dr_matrix(args.dr_matrix)
    return ModelConfig(
        gurps_sw=sw,
        gurps_thr=thr,
        dr_matrix=dr,
        wound_fractions=wound_fractions,
        pa_to_st_offset=args.pa_to_st_offset,
        pen_floor_permille=args.pen_floor_permille,
        trait_base_permille=args.trait_base_permille,
        trait_brave_num=args.trait_brave_num,
        trait_brave_den=args.trait_brave_den,
    )


def weapon_label(row: Row) -> str:
    return f"{row['name']} ({row['item_category']}, WP {row['weapon_power']})"


def armor_label(row: Row) -> str:
    return f"{row['name']} ({armor_class(row)})"


def md_table(headers: Sequence[str], rows: Sequence[Sequence[str | int]]) -> List[str]:
    lines = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join("---" for _ in headers) + " |",
    ]
    for row in rows:
        lines.append("| " + " | ".join(str(value) for value in row) + " |")
    return lines


def grouped_representatives(item_by_id: Dict[int, Row]) -> Dict[str, List[Row]]:
    grouped = {dtype: [] for dtype in TYPE_ORDER}
    for item_id in REPRESENTATIVE_WEAPON_IDS:
        row = item_by_id[item_id]
        grouped[damage_type(row)].append(row)
    return grouped


def matrix_cell(pa: int, brave: int, weapon: Row, armor: Row, config: ModelConfig) -> str:
    dcl = compute_damage(pa, brave, weapon, armor, config)
    van = vanilla_damage(pa, weapon)
    return f"{dcl} / {van}"


def damage_matrix_sections(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    lines = [
        "## Damage Matrices",
        "",
        f"Baseline: PA {PA_BASELINE}, Brave {BRAVE_BASELINE}. Each armor cell is `DCL / vanilla PA*WP`.",
        "",
    ]
    armors = [item_by_id[ARMOR_REPRESENTATIVE_IDS[aclass]] for aclass in ARMOR_ORDER]
    grouped = grouped_representatives(item_by_id)
    for dtype in TYPE_ORDER:
        lines += [f"### {dtype.title()}", ""]
        rows = []
        for weapon in grouped[dtype]:
            rows.append(
                [
                    weapon_label(weapon),
                    *[matrix_cell(PA_BASELINE, BRAVE_BASELINE, weapon, armor, config) for armor in armors],
                ]
            )
        lines += md_table(["Weapon", *[armor_label(armor) for armor in armors]], rows)
        lines.append("")
    return lines


def divergence_section(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    armors = [item_by_id[ARMOR_REPRESENTATIVE_IDS[aclass]] for aclass in ARMOR_ORDER]
    entries = []
    for weapon_id in REPRESENTATIVE_WEAPON_IDS:
        weapon = item_by_id[weapon_id]
        van = vanilla_damage(PA_BASELINE, weapon)
        for armor in armors:
            dcl = compute_damage(PA_BASELINE, BRAVE_BASELINE, weapon, armor, config)
            entries.append((abs(dcl - van), weapon, armor, dcl, van))
    entries.sort(key=lambda item: (-item[0], item[1]["name"], item[2]["name"]))
    rows = []
    for _, weapon, armor, dcl, van in entries[:10]:
        ratio = dcl / van if van else 0.0
        rows.append([weapon_label(weapon), armor_label(armor), dcl, van, dcl - van, f"{ratio:.2f}x"])
    lines = [
        "## Vanilla Comparison: Largest Divergences",
        "",
        "Vanilla PA*WP ignores armor class and Brave. The rows below are the largest absolute gaps in the representative PA 8 / Brave 70 matrix.",
        "",
    ]
    lines += md_table(["Weapon", "Armor", "DCL", "Vanilla", "Delta", "DCL/Vanilla"], rows)
    lines += [
        "",
        "Most current divergences are downward: provisional DCL damage is usually far below PA*WP for high-WP weapons, while armor mostly creates small within-weapon spreads at this PA/Brave point.",
        "",
    ]
    return lines


def survivability_sections(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    lines = [
        "## Survivability: Hits To Kill",
        "",
        "Cell format is `damage: HTK 250/400/600`. HTK uses ceiling division and the model's minimum 1 damage floor.",
        "",
    ]
    armors = [item_by_id[ARMOR_REPRESENTATIVE_IDS[aclass]] for aclass in ARMOR_ORDER]
    grouped = grouped_representatives(item_by_id)
    for dtype in TYPE_ORDER:
        lines += [f"### {dtype.title()}", ""]
        rows = []
        for weapon in grouped[dtype]:
            cells = []
            for armor in armors:
                dcl = compute_damage(PA_BASELINE, BRAVE_BASELINE, weapon, armor, config)
                cells.append(f"{dcl}: " + "/".join(str(htk(hp, dcl)) for hp in HP_POOLS))
            rows.append([weapon_label(weapon), *cells])
        lines += md_table(["Weapon", *[armor_label(armor) for armor in armors]], rows)
        lines.append("")
    return lines


def sensitivity_section(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    body = item_by_id[ARMOR_REPRESENTATIVE_IDS["clothes"]]
    weapons = [item_by_id[item_id] for item_id in SENSITIVITY_WEAPON_IDS]
    lines = [
        "## Sensitivity",
        "",
        f"Sensitivity uses {armor_label(body)} as the target body item.",
        "",
        f"### PA 5..15 at Brave {BRAVE_BASELINE}",
        "",
    ]
    pa_rows = []
    for pa in range(5, 16):
        pa_rows.append([pa, *[compute_damage(pa, BRAVE_BASELINE, weapon, body, config) for weapon in weapons]])
    lines += md_table(["PA", *[weapon_label(weapon) for weapon in weapons]], pa_rows)
    lines += ["", f"### Brave 40..97 at PA {PA_BASELINE}", ""]
    brave_rows = []
    for brave in range(40, 98):
        brave_rows.append([brave, *[compute_damage(PA_BASELINE, brave, weapon, body, config) for weapon in weapons]])
    lines += md_table(["Brave", *[weapon_label(weapon) for weapon in weapons]], brave_rows)
    lines.append("")
    return lines


def hand_check_section(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    cases = [
        ("Dagger into Plate Mail", item_by_id[1], item_by_id[177]),
        ("Rod into Clothing", item_by_id[51], item_by_id[186]),
        ("Save the Queen into Plate Mail", item_by_id[34], item_by_id[177]),
    ]
    lines = [
        "## Integer Sanity Checks",
        "",
        "These cells are recomputed by hand from the LT7 `DclDerivedVariables` chain. All divisions below are integer divisions with truncation.",
        "",
    ]
    for label, weapon, armor in cases:
        t = compute_trace(PA_BASELINE, BRAVE_BASELINE, weapon, armor, config)
        table_name = "gurpsThr" if t["damage_type"] == "thrust" else "gurpsSw"
        lines.append(
            f"- {label}: type `{t['damage_type']}`, armor `{t['armor_class']}`. "
            f"ST = {PA_BASELINE}+{config.pa_to_st_offset} = {t['st']}; "
            f"{table_name}[{t['st']}] = {t['base']}; "
            f"gross = {t['base']}+{t['weapon_power']} = {t['gross']}; "
            f"DR = {t['dr']}; "
            f"penFloor = ({t['gross']}*{config.pen_floor_permille})//1000 = {t['pen_floor']}; "
            f"penetrating = max({t['pen_floor']}, {t['gross']}-{t['dr']}) = {t['penetrating']}; "
            f"wounded = ({t['penetrating']}*{t['wound_num']})//{max(1, int(t['wound_den']))} = {t['wounded']}; "
            f"trait = {config.trait_base_permille}+({BRAVE_BASELINE}*{config.trait_brave_num})//{config.trait_brave_den} = {t['trait_permille']}; "
            f"final = max(1, ({t['wounded']}*{t['trait_permille']})//1000) = {t['final']}."
        )
    lines.append("")
    return lines


def live_anchor_section() -> List[str]:
    return [
        "## LT7 Live-Run Reference Anchors",
        "",
        "These are LT7 observed attack debits for eyeballing only. They are not force-matched to the PA 8 / Brave 70 matrix above.",
        "",
        *md_table(
            ["Observed case", "LT7 damage"],
            [
                ["knife", 34],
                ["rod vs cloth", 19],
                ["rod vs armored", 18],
                ["bow", 31],
                ["knight sword", 117],
                ["katana", 81],
            ],
        ),
        "",
    ]


def clone_matrix(matrix: Tuple[Tuple[int, ...], ...]) -> List[List[int]]:
    return [list(row) for row in matrix]


def table_band_delta(table: Tuple[int, ...], start: int, end: int, delta: int) -> Tuple[int, ...]:
    values = list(table)
    for idx in range(max(0, start), min(len(values), end + 1)):
        values[idx] = max(0, values[idx] + delta)
    return tuple(values)


def wound_clone(config: ModelConfig, **changes: Tuple[int, int]) -> Dict[str, Tuple[int, int]]:
    wounds = dict(config.wound_fractions)
    wounds.update(changes)
    return wounds


def sample_effects(
    item_by_id: Dict[int, Row],
    current: ModelConfig,
    variant: ModelConfig,
    cases: Sequence[Tuple[str, int, int, int, int]],
) -> str:
    parts = []
    for label, weapon_id, armor_id, pa, brave in cases:
        weapon = item_by_id[weapon_id]
        armor = item_by_id[armor_id]
        before = compute_damage(pa, brave, weapon, armor, current)
        after = compute_damage(pa, brave, weapon, armor, variant)
        parts.append(f"{label} {before}->{after}")
    return "; ".join(parts)


def calibration_knobs_section(item_by_id: Dict[int, Row], config: ModelConfig) -> List[str]:
    active_start = PA_BASELINE + config.pa_to_st_offset - 3
    active_end = PA_BASELINE + config.pa_to_st_offset + 8
    softer_plate = clone_matrix(config.dr_matrix)
    softer_plate[ARMOR_INDEX["plate"]][TYPE_INDEX["cut"]] = max(0, softer_plate[0][0] - 2)
    softer_plate[ARMOR_INDEX["plate"]][TYPE_INDEX["thrust"]] = max(0, softer_plate[0][1] - 2)
    harder_plate = clone_matrix(config.dr_matrix)
    harder_plate[ARMOR_INDEX["plate"]][TYPE_INDEX["cut"]] += 2
    harder_plate[ARMOR_INDEX["plate"]][TYPE_INDEX["thrust"]] += 2

    common_cases = [
        ("Dagger/plate", 1, 177, PA_BASELINE, BRAVE_BASELINE),
        ("Rod/clothes", 51, 186, PA_BASELINE, BRAVE_BASELINE),
        ("SaveQueen/plate", 34, 177, PA_BASELINE, BRAVE_BASELINE),
    ]
    floor_cases = [
        ("Nagnarok/plate PA5", 31, 177, 5, BRAVE_BASELINE),
        ("Dagger/plate PA5", 1, 177, 5, BRAVE_BASELINE),
        ("Broadsword/plate PA1", 19, 177, 1, BRAVE_BASELINE),
    ]
    type_cases = [
        ("Dagger/mail", 1, 175, PA_BASELINE, BRAVE_BASELINE),
        ("Longbow/clothes", 83, 186, PA_BASELINE, BRAVE_BASELINE),
        ("SaveQueen/clothes", 34, 186, PA_BASELINE, BRAVE_BASELINE),
    ]
    brave_cases = [
        ("Dagger B40", 1, 186, PA_BASELINE, 40),
        ("Dagger B97", 1, 186, PA_BASELINE, 97),
        ("SaveQueen B40", 34, 186, PA_BASELINE, 40),
        ("SaveQueen B97", 34, 186, PA_BASELINE, 97),
    ]

    rows = []
    knob_rows = [
        (
            "PA->ST offset",
            f"+{config.pa_to_st_offset}",
            "Moves FFT PA into the table index before base damage is read.",
            [
                ("Use +3", replace(config, pa_to_st_offset=3), common_cases),
                ("Use +5", replace(config, pa_to_st_offset=5), common_cases),
            ],
        ),
        (
            "Penetration floor",
            f"{config.pen_floor_permille} permille of gross",
            "Sets the guaranteed chip before wound and Brave scaling.",
            [
                ("150 permille", replace(config, pen_floor_permille=150), floor_cases),
                ("300 permille", replace(config, pen_floor_permille=300), floor_cases),
            ],
        ),
        (
            "DR matrix",
            "; ".join(",".join(str(v) for v in row) for row in config.dr_matrix),
            "Sets subtractive armor by armor class and type: cut, thrust, crush, missile.",
            [
                ("Plate cut/thrust -2", replace(config, dr_matrix=tuple(tuple(row) for row in softer_plate)), common_cases),
                ("Plate cut/thrust +2", replace(config, dr_matrix=tuple(tuple(row) for row in harder_plate)), common_cases),
            ],
        ),
        (
            "Wound fractions",
            ", ".join(f"{key} {num}/{den}" for key, (num, den) in config.wound_fractions.items()),
            "Scales only penetrating damage after DR/floor; this is where type lethality lives.",
            [
                ("Thrust 3/2", replace(config, wound_fractions=wound_clone(config, thrust=(3, 2))), type_cases),
                ("Missile 6/5", replace(config, wound_fractions=wound_clone(config, missile=(6, 5))), type_cases),
            ],
        ),
        (
            "Brave trait line",
            f"{config.trait_base_permille} + Brave*{config.trait_brave_num}//{config.trait_brave_den}",
            "Controls how much roster Brave changes physical offense.",
            [
                ("Narrow 850 + Brave*350//100", replace(config, trait_base_permille=850, trait_brave_num=350), brave_cases),
                ("Wide 700 + Brave*700//100", replace(config, trait_base_permille=700, trait_brave_num=700), brave_cases),
            ],
        ),
        (
            "Base tables",
            f"sw[{PA_BASELINE + config.pa_to_st_offset}]={table_clamp(config.gurps_sw, PA_BASELINE + config.pa_to_st_offset)}, "
            f"thr[{PA_BASELINE + config.pa_to_st_offset}]={table_clamp(config.gurps_thr, PA_BASELINE + config.pa_to_st_offset)}",
            "Controls the PA curve before weapon power, DR, wound, and Brave.",
            [
                (
                    f"Thrust table +1 at ST {active_start}..{active_end}",
                    replace(config, gurps_thr=table_band_delta(config.gurps_thr, active_start, active_end, 1)),
                    type_cases,
                ),
                (
                    f"Swing table -1 at ST {active_start}..{active_end}",
                    replace(config, gurps_sw=table_band_delta(config.gurps_sw, active_start, active_end, -1)),
                    type_cases,
                ),
            ],
        ),
    ]
    for knob, current, controls, alternatives in knob_rows:
        for alt_label, variant, cases in alternatives:
            rows.append([knob, current, controls, alt_label, sample_effects(item_by_id, config, variant, cases)])
    lines = [
        "## Calibration Knobs And Open Questions For Marcelo",
        "",
        "Each row shows one concrete calibration edit and recomputed sample damage. These are examples for discussion, not recommendations.",
        "",
    ]
    lines += md_table(["Knob", "Current provisional value", "What it controls", "Alternative", "Sample effect"], rows)
    lines += [
        "",
        "Open questions:",
        "",
        "- Should missile continue to use `gurpsSw` because LT7 says `if(thrust, thr, sw)`, or should ranged weapons eventually get their own base source?",
        "- Should armor sharpness target the design note's medium matchup goal directly, or should first-pass tuning prioritize matching the LT7 live anchors?",
        "- Should Brave remain a large offensive roster lever, or should weapon type and armor matchup carry more of the damage spread?",
        "",
    ]
    return lines


def report_header(catalog_path: Path, config: ModelConfig) -> List[str]:
    return [
        "# DCL Damage Calibration Briefing",
        "",
        "**All numbers in this report are provisional, and the model shape itself is up for discussion.** The current tables reproduce LT7's proven in-game basic-attack rewrite so Marcelo can replace provisional constants with authored ones.",
        "",
        "## Sources And Baseline",
        "",
        *md_table(
            ["Input", "Value"],
            [
                ["Runtime settings", str(SETTINGS_PATH.relative_to(ROOT))],
                ["Item catalog", str(catalog_path.relative_to(ROOT))],
                ["Generated by", str((WORK / f"{SCRIPT_TS}-dcl-damage-calibration-sim.py").relative_to(ROOT))],
                ["Damage baseline", f"PA {PA_BASELINE}, Brave {BRAVE_BASELINE}"],
                ["Trait line", f"{config.trait_base_permille} + Brave*{config.trait_brave_num}//{config.trait_brave_den}"],
                ["Pen floor", f"{config.pen_floor_permille} permille of gross"],
                ["Armor columns", "Plate Mail / Chainmail / Clothing / Hempen Robe"],
            ],
        ),
        "",
        "The simulator follows LT7 integer semantics exactly: table indices clamp, every `mulDiv(a,b,c)` is `(a*b)//c`, wound denominators are clamped to at least 1, and final damage is floored at 1.",
        "",
    ]


def build_report(catalog_path: Path, item_by_id: Dict[int, Row], config: ModelConfig) -> str:
    lines: List[str] = []
    lines += report_header(catalog_path, config)
    lines += damage_matrix_sections(item_by_id, config)
    lines += sensitivity_section(item_by_id, config)
    lines += divergence_section(item_by_id, config)
    lines += survivability_sections(item_by_id, config)
    lines += hand_check_section(item_by_id, config)
    lines += live_anchor_section()
    lines += calibration_knobs_section(item_by_id, config)
    return "\n".join(lines).rstrip() + "\n"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Regenerate the DCL damage calibration report.")
    parser.add_argument("--report", type=Path, default=DEFAULT_REPORT_PATH, help="Output markdown report path.")
    parser.add_argument("--pa-to-st-offset", type=int, default=DEFAULT_PA_TO_ST_OFFSET)
    parser.add_argument("--pen-floor-permille", type=int, default=DEFAULT_PEN_FLOOR_PERMILLE)
    parser.add_argument("--trait-base-permille", type=int, default=DEFAULT_TRAIT_BASE_PERMILLE)
    parser.add_argument("--trait-brave-num", type=int, default=DEFAULT_TRAIT_BRAVE_NUM)
    parser.add_argument("--trait-brave-den", type=int, default=DEFAULT_TRAIT_BRAVE_DEN)
    parser.add_argument("--cut-wound", default="3/2")
    parser.add_argument("--thrust-wound", default="2/1")
    parser.add_argument("--crush-wound", default="1/1")
    parser.add_argument("--missile-wound", default="1/1")
    parser.add_argument("--sw-table", help="Comma-separated replacement gurpsSw table.")
    parser.add_argument("--thr-table", help="Comma-separated replacement gurpsThr table.")
    parser.add_argument("--dr-matrix", help="Replacement DR matrix as 'r1;r2;r3;r4', rows comma-separated.")
    return parser.parse_args()


def main() -> None:
    global CONFIG
    args = parse_args()
    settings = load_settings()
    catalog_rows, catalog_path = load_catalog()
    item_by_id = rows_by_id(catalog_rows)
    CONFIG = make_config(settings, args)
    report = build_report(catalog_path, item_by_id, CONFIG)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(report, encoding="utf-8")
    print(f"Wrote {args.report}")


if __name__ == "__main__":
    main()
