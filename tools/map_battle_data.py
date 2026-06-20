#!/usr/bin/env python3
"""
Map every battle-relevant data surface into one inventory.

Outputs work/battle_data_inventory.md with:
  - For each TableData XML: entry tag, count, every field, and the distinct token
    vocabulary for enum/flag fields (elements, status, equip types, ability flags, etc.).
  - For each curated battle Nex .layout: the column list (name + type).

This is the "what variables exist and where" reference. Run from project root:
    python tools/map_battle_data.py
"""
from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"
TABLEDATA = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData")
LAYOUTS = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\Nex\Layouts\ffto")

# Fields whose text is an enum/flag vocabulary worth enumerating fully.
ENUM_HINT = re.compile(r"(Flags|Elements|Status|Equippable|AttackFlags|Behavior|Type|Items)$")

# Battle-relevant Nex layouts to include (schema = column list).
BATTLE_LAYOUTS = [
    "OverrideAbilityActionData", "Ability", "AbilityReactionVoiceType",
    "Job", "GeneralJob", "JobType", "JobCommand",
    "Item", "Battle", "BattleObjective",
    "CharaZodiacStoneCLUT", "CharShapeLUTParam", "CharTacticalViewParam",
    "ContinuousBattleTimeline", "MapVariationRandomBattle",
    "UIStatusEffect", "UIUnitStatusNumParam",
]


def map_tabledata() -> list[str]:
    out = ["# TableData XML surfaces (hardcoded tables, edited via FFTIVC/tables)\n"]
    for xml in sorted(TABLEDATA.glob("*.xml")):
        try:
            root = ET.parse(xml).getroot()
        except ET.ParseError:
            continue
        entries_node = root.find("Entries")
        entries = list(entries_node) if entries_node is not None else []
        if not entries:
            # tables that are pure pointers (e.g. AbilityActionData) have no Entries
            out.append(f"## {xml.name}\n\n_(no Entries - see header / pointer table)_\n")
            continue
        tag = entries[0].tag
        fields = OrderedDict()
        enum_vocab: dict[str, set] = {}
        for e in entries:
            for child in e:
                fields.setdefault(child.tag, True)
                if ENUM_HINT.search(child.tag) and child.text:
                    toks = [t.strip() for t in re.split(r"[,\s]+", child.text) if t.strip()]
                    enum_vocab.setdefault(child.tag, set()).update(
                        t for t in toks if not t.isdigit())
        out.append(f"## {xml.name}  ({tag} x {len(entries)})\n")
        out.append("Fields: " + ", ".join(fields) + "\n")
        for f, vocab in enum_vocab.items():
            vocab.discard("None")
            if vocab:
                out.append(f"- `{f}` vocab ({len(vocab)}): " + ", ".join(sorted(vocab)))
        out.append("")
    return out


def parse_layout(path: Path) -> list[str]:
    cols = []
    meta = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line.startswith("add_column|"):
            line = line.split("//", 1)[0].strip()      # drop inline comments
            parts = line.split("|")
            name = parts[1].strip() if len(parts) > 1 else "?"
            typ = parts[2].strip() if len(parts) > 2 else ""
            cols.append(f"{name}:{typ}" if typ else name)
        elif line.startswith(("table_name|", "set_table_type|", "use_base_row_id|")):
            meta.append(line.replace("|", "="))
    return cols, meta


def map_layouts() -> list[str]:
    out = ["\n# Nex/NXD table schemas (battle-relevant; edited via .nxd override)\n"]
    for name in BATTLE_LAYOUTS:
        p = LAYOUTS / f"{name}.layout"
        if not p.exists():
            out.append(f"## {name}\n\n_(layout not found)_\n")
            continue
        cols, meta = parse_layout(p)
        out.append(f"## {name}  ({len(cols)} columns)")
        if meta:
            out.append("  " + "  ".join(meta))
        out.append("Columns: " + ", ".join(cols) + "\n")
    return out


if __name__ == "__main__":
    WORK.mkdir(exist_ok=True)
    lines = map_tabledata() + map_layouts()
    out = WORK / "battle_data_inventory.md"
    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {out}  ({len(lines)} lines)")
