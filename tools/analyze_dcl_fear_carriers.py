#!/usr/bin/env python3
"""Audit isolated technical carriers for the job-free DCL Fear packet."""
from __future__ import annotations

import argparse
import csv
import json
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = ROOT / "work/wotl_ability_action_baseline.csv"
DEFAULT_SETTINGS = ROOT / "work/1784398672-battle-runtime-settings.dcl-unified-sentinel-v3.json"


@dataclass(frozen=True)
class Candidate:
    ability_id: int
    name: str
    native_status: str
    area: int
    ct: int
    mp_cost: int
    score: tuple[int, int, int, int]


def _rows(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def _settings(path: Path) -> dict:
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        raise ValueError("settings must be a JSON object")
    return value


def integrated_ability_ids(settings: dict) -> set[int]:
    ids = {0}  # Basic Attack is the canonical physical sentinel transaction.
    for key in ("DclStatusRules", "DclInstantKoRules", "DclInterruptRules", "DclReactionRules"):
        rules = settings.get(key, [])
        if not isinstance(rules, list):
            continue
        ids.update(
            int(rule["AbilityId"])
            for rule in rules
            if isinstance(rule, dict) and isinstance(rule.get("AbilityId"), int)
        )
    for key in (
        "DclSyntheticReactionCarrierId",
        "DclSyntheticReactionDeliveryId",
        "DclApproachOwnerReactionId",
        "DclApproachDeliveryReactionId",
    ):
        value = settings.get(key)
        if isinstance(value, int) and value >= 0:
            ids.add(value)
    return ids


def candidates(catalog: list[dict[str, str]], settings: dict) -> list[Candidate]:
    used = integrated_ability_ids(settings)
    result: list[Candidate] = []
    for row in catalog:
        ability_id = int(row["id_dec"])
        statuses = row["inflict_statuses"].strip()
        if (
            ability_id in used
            or row["ability_type"] != "Normal"
            or row["formula_hex"] != "0x0A"
            or row["inflict_status_mode"] != "AllOrNothing"
            or not statuses
            or "|" in statuses
        ):
            continue

        area = int(row["aoe"])
        ct = int(row["ct"])
        mp_cost = int(row["mp_cost"])
        # A native Berserk carrier is the closest mechanical mirror of Fear. A single-target row
        # keeps packet validation separate from the independent native AoE-authority probe.
        score = (
            0 if statuses == "Berserk" else 1,
            0 if area == 0 else 1,
            ct,
            ability_id,
        )
        result.append(Candidate(
            ability_id=ability_id,
            name=row["name_ivc"].strip() or row["name_wotl"].strip(),
            native_status=statuses,
            area=area,
            ct=ct,
            mp_cost=mp_cost,
            score=score,
        ))
    return sorted(result, key=lambda candidate: candidate.score)


def validate(catalog: list[dict[str, str]], settings: dict, ranked: list[Candidate]) -> list[str]:
    errors: list[str] = []
    ids = [int(row["id_dec"]) for row in catalog]
    if ids != list(range(512)):
        errors.append("catalog must contain the exact ordered ability ids 0..511")
    if not ranked:
        errors.append("no isolated formula-0x0A single-status carrier is available")
        return errors
    recommended = ranked[0]
    if recommended.ability_id != 53 or recommended.native_status != "Berserk":
        errors.append(
            "expected Fervor 53/Berserk to be the highest-ranked isolated Fear carrier, got "
            f"{recommended.ability_id}/{recommended.native_status}"
        )
    if 53 in integrated_ability_ids(settings):
        errors.append("recommended Fervor 53 is already owned by the integrated sentinel")
    return errors


def render(catalog_path: Path, settings_path: Path) -> tuple[str, str, list[str]]:
    catalog = _rows(catalog_path)
    settings = _settings(settings_path)
    ranked = candidates(catalog, settings)
    errors = validate(catalog, settings, ranked)

    lines = [
        "# DCL Fear technical-carrier audit",
        "",
        "## Inputs",
        "",
        f"- catalog: `{catalog_path.relative_to(ROOT).as_posix()}`",
        f"- integrated settings: `{settings_path.relative_to(ROOT).as_posix()}`",
        f"- complete catalog rows: `{len(catalog)}`",
        f"- isolated formula-0x0A candidates: `{len(ranked)}`",
        "",
        "## Result",
        "",
        "Fervor `53` is the isolated technical carrier. It is a standard formula-`0x0A`,",
        "single-status, single-target action whose native packet is Berserk. Fear replaces that",
        "execution packet through `replaced-post-calc-reskin`, explicitly clearing native Berserk byte `2`, mask `0x08`, before staging finite Chicken byte `2`, mask `0x04`;",
        "native Berserk is not applied. The id is absent from every v3 status, KO, Interrupt,",
        "Reaction, synthetic, and Approach owner set.",
        "",
        "Basic Attack `0` is rejected as an integration carrier because it is the canonical physical",
        "sentinel. Fire `16` remains only the independent native AoE target-expansion falsifier; Fear",
        "authorization observes actions performed by a Feared unit and does not depend on the action",
        "that originally applied Fear.",
        "",
        "## Ranked candidates",
        "",
        "| Rank | Ability | Name | Native packet | Area | CT | MP |",
        "| ---: | ---: | --- | --- | ---: | ---: | ---: |",
    ]
    csv_lines = ["rank,ability_id,name,native_status,area,ct,mp_cost"]
    for index, candidate in enumerate(ranked, start=1):
        lines.append(
            f"| {index} | {candidate.ability_id} | {candidate.name} | {candidate.native_status} | "
            f"{candidate.area} | {candidate.ct} | {candidate.mp_cost} |"
        )
        csv_lines.append(
            f"{index},{candidate.ability_id},{candidate.name},{candidate.native_status},"
            f"{candidate.area},{candidate.ct},{candidate.mp_cost}"
        )
    if errors:
        lines.extend(["", "## Errors", "", *[f"- {error}" for error in errors]])
    lines.append("")
    return "\n".join(lines), "\n".join(csv_lines) + "\n", errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--settings", type=Path, default=DEFAULT_SETTINGS)
    parser.add_argument("--prefix", type=int)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    markdown, csv_text, errors = render(args.catalog.resolve(), args.settings.resolve())
    if not args.check_only:
        prefix = args.prefix or int(time.time())
        md_path = ROOT / "work" / f"{prefix}-dcl-fear-technical-carrier-audit.md"
        csv_path = ROOT / "work" / f"{prefix}-dcl-fear-technical-carrier-audit.csv"
        md_path.write_text(markdown, encoding="utf-8", newline="\n")
        csv_path.write_text(csv_text, encoding="utf-8", newline="\n")
        print(f"wrote {md_path}")
        print(f"wrote {csv_path}")
    for error in errors:
        print(f"ERROR: {error}")
    print("DCL Fear carrier audit PASS" if not errors else "DCL Fear carrier audit FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
