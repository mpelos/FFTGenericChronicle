#!/usr/bin/env python3
"""Build the DCL elemental-affinity fragment from canonical job data.

The battle-unit bytes at +0x52..+0x56 are not an authoritative affinity source.  This
fragment instead joins the target job's table-authored affinities with every equipped
item exposed by the item catalog.  The generated formulas deliberately use new
``*Resolved`` names so an older profile cannot silently keep reading the hypothetical
memory block.
"""
from __future__ import annotations

import argparse
import csv
import json
import xml.etree.ElementTree as ET
from pathlib import Path


ELEMENTS = ("Fire", "Ice", "Lightning", "Wind", "Earth", "Water")
JOB_FIELDS = {
    "Absorb": "AbsorbElements",
    "Null": "NullifyElements",
    "Halve": "HalveElements",
    "Weak": "WeakElements",
}
ITEM_PREFIXES = {
    "Absorb": "absorb",
    "Null": "nullify",
    "Halve": "halve",
    "Weak": "weak",
    "Strong": "strong",
}
SLOTS = ("head", "body", "accessory", "rightweapon", "rightshield", "leftweapon", "leftshield")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("job_data", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def split_flags(raw: str | None) -> set[str]:
    return {
        part.strip().lower()
        for part in (raw or "").split(",")
        if part.strip() and part.strip().lower() != "none"
    }


def job_maps(path: Path) -> dict[str, dict[str, int]]:
    if path.suffix.lower() == ".csv":
        with path.open(newline="", encoding="utf-8-sig") as stream:
            rows = list(csv.DictReader(stream))
        jobs = [
            {
                "Id": row.get("Id", "-1"),
                **{field: row.get(field, "") for field in JOB_FIELDS.values()},
            }
            for row in rows
        ]
    else:
        root = ET.parse(path).getroot()
        entries = root.find("Entries")
        if entries is None:
            raise ValueError(f"missing <Entries> in {path}")
        jobs = [
            {
                "Id": job.findtext("Id", "-1"),
                **{field: job.findtext(field, "") for field in JOB_FIELDS.values()},
            }
            for job in entries.findall("Job")
        ]

    result: dict[str, dict[str, int]] = {}
    for affinity, field in JOB_FIELDS.items():
        for element in ELEMENTS:
            values: dict[str, int] = {}
            for job in jobs:
                job_id = int(job["Id"])
                if element.lower() in split_flags(job[field]):
                    values[str(job_id)] = 1
            # An absent map is a valid all-zero family because every generated lookup uses
            # mapOr(..., 0). Runtime settings intentionally reject empty map declarations.
            if values:
                result[f"dclJob{affinity}{element}"] = values
    return result


def affinity_source(affinity: str, element: str) -> str:
    terms: list[str] = []
    if affinity != "Strong":
        terms.append(f"mapOr(dclJob{affinity}{element}, t.jobId, 0)")
    item_prefix = ITEM_PREFIXES[affinity]
    element_name = element.lower()
    terms.extend(f"tslot.{slot}.{item_prefix}_{element_name}" for slot in SLOTS)
    return " || ".join(terms)


def resolved_formula(affinity: str) -> str:
    clauses: list[str] = []
    for element in ELEMENTS:
        source = affinity_source(affinity, element)
        if affinity == "Weak" and element == "Fire":
            source = f"({source} || t.status.oil)"
        clauses.append(f"(dcl.element{element} && ({source}))")
    return "dcl.isElemental && (" + " || ".join(clauses) + ")"


def build(path: Path) -> dict[str, object]:
    derived = [
        {"Name": f"dcl.target{affinity}Resolved", "Formula": resolved_formula(affinity)}
        for affinity in ("Absorb", "Null", "Halve", "Weak", "Strong")
    ]
    derived.extend(
        [
            {
                "Name": "dcl.elementPermilleResolved",
                "Formula": "if(dcl.targetNullResolved, 0, if(dcl.targetHalveResolved, const.dclAffinityHalvePermille, if(dcl.targetWeakResolved, const.dclAffinityWeakPermille, if(dcl.targetStrongResolved, const.dclAffinityStrongPermille, 1000))))",
            },
            {
                "Name": "dcl.magicStackPermilleResolved",
                "Formula": "min(const.dclMagicStackCapPermille, mulDiv(mulDiv(mulDiv(dcl.faithCombinedPermille, if(dcl.isElemental, dcl.elementPermilleResolved, 1000), 1000), dcl.shellPermille, 1000), dcl.zodiacPermille, 1000))",
            },
            {
                "Name": "dcl.magicAmountResolved",
                "Formula": "if(dcl.targetNullResolved, 0, max(1, mulDiv(mulDiv(dcl.magicCore, dcl.magicStackPermilleResolved, 1000), const.dclMagicScalePermille, 1000)))",
            },
            {
                "Name": "dcl.magicAbsorbedResolved",
                "Formula": "dcl.isMagicDamage && dcl.targetAbsorbResolved",
            },
        ]
    )
    return {
        "_note": "Elemental affinity is resolved from canonical JobData plus all seven target equipment slots; unit+0x52..+0x56 remains non-authoritative and is not consumed by these formulas.",
        "FormulaMaps": job_maps(path),
        "DclDerivedVariables": derived,
    }


def main() -> int:
    args = parse_args()
    rendered = json.dumps(build(args.job_data), indent=2, ensure_ascii=False) + "\n"
    if args.check_only:
        if not args.output.exists():
            raise SystemExit(f"missing generated fragment: {args.output}")
        if args.output.read_text(encoding="utf-8-sig") != rendered:
            raise SystemExit(f"stale generated fragment: {args.output}")
        print(f"DCL affinity fragment is current: {args.output}")
        return 0
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(rendered, encoding="utf-8", newline="\n")
    print(f"wrote {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
