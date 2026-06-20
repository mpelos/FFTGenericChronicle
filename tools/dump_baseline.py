#!/usr/bin/env python3
"""
Build the Generic Chronicle design baseline.

Outputs two CSVs into work/:
  - baseline_jobs.csv      : every job + its stat growth/multipliers/move/jump/etc (from JobData.xml)
  - baseline_abilities.csv : every ability id + name + decoded JP + CT/MP overrides + flags
                             (joins ability_en.sqlite text table with override_ability.sqlite)

These are the starting reference tables for the battle-system redesign. Note: per-ability BASE
Formula/X/Y are NOT in the data files (they're hardcoded in FFT_enhanced.exe); the override
table only shows fields the vanilla game already changed (CT on 28 abilities, MP on 4). Use
FFHacktics WotL data for the baseline formula/X/Y and override only what we redesign.

Run from the project root:
    python tools/dump_baseline.py
"""
from __future__ import annotations

import csv
import re
import sqlite3
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"

# Inputs (override paths here if your setup differs)
JOBDATA_XML = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\JobData.xml")
ABILITY_EN_DB = WORK / "ability_en.sqlite"            # from ability.en.nxd -> sqlite
OVERRIDE_DB = WORK / "override_ability.sqlite"        # from overrideabilityactiondata.nxd -> sqlite

JOB_FIELDS = [
    "JobCommandId",
    "InnateAbilityId1", "InnateAbilityId2", "InnateAbilityId3", "InnateAbilityId4",
    "HPGrowth", "HPMultiplier", "MPGrowth", "MPMultiplier",
    "SpeedGrowth", "SpeedMultiplier", "PAGrowth", "PAMultiplier", "MAGrowth", "MAMultiplier",
    "Move", "Jump", "CharacterEvasion",
    "InnateStatus", "ImmuneStatus", "StartingStatus",
    "AbsorbElements", "NullifyElements", "HalveElements", "WeakElements",
    "MonsterPortrait", "MonsterPalette", "MonsterGraphic",
    "EquippableItems",
]

# <Id>2</Id> <!-- Squire / ... -->  -> capture id and English name (first comment segment)
ID_NAME_RE = re.compile(r"<Id>(\d+)</Id>\s*(?:<!--\s*([^/|]+?)\s*[/|])?")


def dump_jobs() -> int:
    raw = JOBDATA_XML.read_text(encoding="utf-8")
    id_to_name = {int(m.group(1)): (m.group(2) or "").strip() for m in ID_NAME_RE.finditer(raw)}

    tree = ET.parse(JOBDATA_XML)
    out = WORK / "baseline_jobs.csv"
    n = 0
    with out.open("w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["Id", "Name"] + JOB_FIELDS)
        for job in tree.getroot().iter("Job"):
            jid = job.findtext("Id")
            row = [jid, id_to_name.get(int(jid), "")]
            for f in JOB_FIELDS:
                row.append(job.findtext(f, ""))
            w.writerow(row)
            n += 1
    print(f"wrote {out}  ({n} jobs)")
    return n


def dump_abilities() -> int:
    a = sqlite3.connect(ABILITY_EN_DB)
    text = {r[0]: r for r in a.execute(
        'select Key, Name, JpCost1, JpCost2, IsRandomDamage, IsRandomStatus from "Ability-en"'
    )}
    ov = {}
    if OVERRIDE_DB.exists():
        o = sqlite3.connect(OVERRIDE_DB)
        for r in o.execute(
            "select Key, Range, EffectArea, Vertical, Element, Formula, X, Y, InflictStatus, CT, MPCost "
            "from OverrideAbilityActionData"
        ):
            ov[r[0]] = r

    out = WORK / "baseline_abilities.csv"
    n = 0
    with out.open("w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow([
            "Id", "Name", "JP", "JpCost1", "JpCost2", "IsRandomDamage", "IsRandomStatus",
            "ov_Range", "ov_EffectArea", "ov_Vertical", "ov_Element", "ov_Formula",
            "ov_X", "ov_Y", "ov_InflictStatus", "ov_CT", "ov_MPCost",
        ])
        for key in sorted(text):
            _, name, j1, j2, rd, rs = text[key]
            jp = (j1 or 0) + 256 * (j2 or 0)
            o = ov.get(key, [None] * 11)
            # -1 means "inherit base"; blank it for readability
            ovals = ["" if (v is None or v == -1) else v for v in o[1:]]
            w.writerow([key, name, jp, j1, j2, rd, rs] + ovals)
            n += 1
    print(f"wrote {out}  ({n} abilities)")
    return n


if __name__ == "__main__":
    WORK.mkdir(exist_ok=True)
    dump_jobs()
    dump_abilities()
