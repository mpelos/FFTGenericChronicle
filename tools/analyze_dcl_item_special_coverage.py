#!/usr/bin/env python3
"""Classify every DCL item-sidecar special rule by its job-free technical owner."""
from __future__ import annotations

import argparse
import csv
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SIDECAR = ROOT / "work" / "1783984192-dcl-item-sidecar.csv"
PHYSICAL_PROFILE = ROOT / "work" / "1783980809-battle-runtime-settings.dcl-weapon-skill-mechanism.json"
MAGIC_PROFILE = ROOT / "work" / "1783983032-battle-runtime-settings.dcl-magic-pipeline-mechanism.json"
ITEM_CATALOG = ROOT / "work" / "item_catalog.csv"


@dataclass(frozen=True)
class Owner:
    kind: str
    owner: str
    evidence: str


OWNERS = {
    "none": Owner("no-special", "native/data identity", "no extra DCL behavior declared"),
    "reach_escape_counter_pointblank": Owner(
        "formula", "physical mechanism profile", "dcl.pointBlank and the Attack Skill modifier"
    ),
    "straight_line": Owner(
        "native-live-gated", "native Direct resolver", "separate Arc/Direct LoS coverage row"
    ),
    "arc_trajectory": Owner(
        "native-live-gated", "native Arc resolver", "separate Arc/Direct LoS coverage row"
    ),
    "modest_speed_grant": Owner(
        "native-data", "ItemEquipBonusData", "item catalog exposes bonus_speed"
    ),
    "support_heal_modifier": Owner(
        "formula", "magic mechanism profile", "dcl.supportMagicMod"
    ),
    "offensive_magic_modifier": Owner(
        "formula", "magic mechanism profile", "dcl.offensiveMagicMod"
    ),
    "defender_parry_minus4_block_minus2": Owner(
        "formula", "physical mechanism profile", "dcl.flailParryPenalty / dcl.flailBlockPenalty"
    ),
    "common_unarmed_thr_minus_penalty_or_martial_arts_wmod": Owner(
        "formula+job-excluded", "physical mechanism profile", "common fist route; Martial Arts excluded"
    ),
    "draw_out_repeatable_mp_cost": Owner(
        "job-excluded", "job/content policy", "Samurai ability economy"
    ),
    "talk_identity_job_authored": Owner(
        "job-excluded", "job/content policy", "Orator action roster"
    ),
    "job_buff_or_debuff_pending": Owner(
        "job-excluded", "job/content policy", "Bag action identity"
    ),
    "dance_identity_job_authored": Owner(
        "job-excluded", "job/content policy", "Dancer action roster"
    ),
    "song_identity_job_authored": Owner(
        "job-excluded", "job/content policy", "Bard action roster"
    ),
}


def load_rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def validate(sidecar: Path) -> tuple[list[dict[str, str]], list[str]]:
    errors: list[str] = []
    rows = load_rows(sidecar)
    rules = {row.get("special_rule", "") for row in rows}
    missing = sorted(rule for rule in rules if rule not in OWNERS)
    stale = sorted(rule for rule in OWNERS if rule not in rules)
    if missing:
        errors.append(f"unclassified special_rule values: {', '.join(missing)}")
    if stale:
        errors.append(f"classification entries no longer present in sidecar: {', '.join(stale)}")

    physical = PHYSICAL_PROFILE.read_text(encoding="utf-8")
    magic = MAGIC_PROFILE.read_text(encoding="utf-8")
    catalog_header = ITEM_CATALOG.read_text(encoding="utf-8-sig").splitlines()[0].split(",")
    required_markers = {
        "physical:dcl.pointBlank": "dcl.pointBlank" in physical,
        "physical:dcl.isUnarmed": "dcl.isUnarmed" in physical,
        "physical:dclUntrainedFistPenalty": "dclUntrainedFistPenalty" in physical,
        "physical:dcl.flailParryPenalty": "dcl.flailParryPenalty" in physical,
        "physical:dcl.flailBlockPenalty": "dcl.flailBlockPenalty" in physical,
        "magic:dcl.supportMagicMod": "dcl.supportMagicMod" in magic,
        "magic:dcl.offensiveMagicMod": "dcl.offensiveMagicMod" in magic,
        "catalog:bonus_speed": "bonus_speed" in catalog_header,
        "native-lof-analyzer": (ROOT / "tools" / "analyze_dcl_weapon_line_of_fire.py").exists(),
    }
    errors.extend(f"missing owner marker {name}" for name, present in required_markers.items() if not present)
    return rows, errors


def render(rows: list[dict[str, str]], sidecar: Path) -> str:
    counts = Counter(row["special_rule"] for row in rows)
    lines = [
        "# DCL item special-rule technical coverage",
        "",
        "## Scope",
        "",
        "This report classifies every `special_rule` value in the DCL item sidecar. Final numeric",
        "item authoring and job-owned action identity are excluded; every job-free technical rule",
        "must still name a formula, native-data, native-runtime, or code owner.",
        "",
        "## Input",
        "",
        f"- Sidecar: `{sidecar.relative_to(ROOT).as_posix()}`",
        f"- Rows: {len(rows)}",
        f"- Distinct rules: {len(counts)}",
        "",
        "## Classification",
        "",
        "| Rule | Rows | Class | Owner | Evidence boundary |",
        "| --- | ---: | --- | --- | --- |",
    ]
    for rule in sorted(counts):
        owner = OWNERS[rule]
        lines.append(
            f"| `{rule}` | {counts[rule]} | `{owner.kind}` | {owner.owner} | {owner.evidence} |"
        )
    lines.extend(
        [
            "",
            "## Result",
            "",
            "Every declared item special has a technical owner. Arc/Direct remain live-gated in the",
            "separate weapon-LoS mechanism. Job/content rules remain explicitly excluded rather than",
            "being mistaken for missing combat-runtime infrastructure.",
            "",
        ]
    )
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sidecar", type=Path, default=DEFAULT_SIDECAR)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    sidecar = args.sidecar.resolve()
    rows, errors = validate(sidecar)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    if args.check_only:
        print(f"all DCL item special rules classified: {len(rows)} rows, {len(OWNERS)} rules")
        return 0
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-item-special-coverage.md"
    output.write_text(render(rows, sidecar), encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print(f"all DCL item special rules classified: {len(rows)} rows, {len(OWNERS)} rules")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
