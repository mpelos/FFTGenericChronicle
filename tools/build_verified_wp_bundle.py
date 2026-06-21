#!/usr/bin/env python3
"""Build a v0.2 stress-audit sim bundle using verified IVC family WP maxima.

This does not accept a new balance policy. It answers a narrower question:
what happens if the v0.2 policy is fed the local IVC weapon table maxima instead
of the design-provisional WP values?
"""
from __future__ import annotations

import argparse
import csv
import json
from copy import deepcopy
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"


def read_family_wp_max(path: Path) -> dict[str, int]:
    if not path.exists():
        raise SystemExit(f"missing family summary: {path}; run tools/summarize_weapon_baseline.py first")
    values: dict[str, int] = {}
    with path.open(newline="", encoding="utf-8") as fh:
        for row in csv.DictReader(fh):
            family = row.get("family", "")
            if not family:
                continue
            values[family] = int(row.get("wp_max") or 0)
    return values


def build_bundle(source: Path, family_summary: Path, output: Path) -> dict[str, Any]:
    data = json.loads(source.read_text(encoding="utf-8"))
    bundle = deepcopy(data)
    wp_max = read_family_wp_max(family_summary)

    source_version = str(bundle.get("version", source.stem))
    bundle["version"] = f"{source_version}-verified-wpmax"
    provenance = dict(bundle.get("provenance", {}))
    provenance["source_bundle"] = source.name
    provenance["weapon_wp"] = "verified-local wp_max from work/baseline_weapon_families.csv"
    provenance["result_class"] = "stress audit with verified IVC WP maxima; not accepted tuning"
    provenance["audit_note"] = (
        "Family WP values were replaced by maximum Power observed in work/baseline_weapons.csv. "
        "This intentionally stresses late/rare/endgame equipment and should trigger retune review."
    )
    bundle["provenance"] = provenance

    changed: dict[str, dict[str, int]] = {}
    for family, spec in bundle.get("families", {}).items():
        if family not in wp_max:
            continue
        original = int(spec.get("wp", 0))
        verified = wp_max[family]
        spec["wp"] = verified
        spec["v0_2_design_wp"] = original
        spec["verified_wp_max"] = verified
        changed[family] = {"from": original, "to": verified}

    bundle["verified_wp_replacements"] = changed
    output.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(changed)} family WP replacements)")
    return bundle


def main() -> int:
    parser = argparse.ArgumentParser(description="Build verified-WP stress sim bundle.")
    parser.add_argument("--source", type=Path, default=WORK / "sim-inputs-v0.2.json")
    parser.add_argument("--family-summary", type=Path, default=WORK / "baseline_weapon_families.csv")
    parser.add_argument("--output", type=Path, default=WORK / "sim-inputs-v0.2-verified-wpmax.json")
    args = parser.parse_args()
    build_bundle(args.source, args.family_summary, args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
