#!/usr/bin/env python3
"""Prove that every status-bearing catalog action has one explicit DCL mechanism owner."""
from __future__ import annotations

import argparse
import csv
import io
import re
import time
from collections import Counter
from pathlib import Path

import analyze_dcl_formula38_carriers as formula38
import analyze_dcl_special_status_carriers as special
import analyze_dcl_status_transactions as status_tx
import build_neuter_data as neuter


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = status_tx.DEFAULT_CATALOG
CARRIER_SOURCE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclStatusNativeCarrier.cs"

RETAINED_IDS = frozenset({81, 84} | formula38.SAFE_IDS)
ORDINARY_RIDER_IDS = frozenset(neuter.DCL_STATUS_RIDER_ABILITY_IDS)
SUPPORT_RIDER_IDS = frozenset(neuter.DCL_SUPPORT_STATUS_RIDER_ABILITY_IDS)
CONDITIONAL_RIDER_IDS = frozenset(neuter.DCL_CONDITIONAL_STATUS_RIDER_ABILITY_IDS)
INSTANT_KO_IDS = frozenset(neuter.DCL_INSTANT_KO_ABILITY_IDS)
NATIVE_LIFECYCLE_IDS = frozenset({5, 6, 107, 312, 314})
CONDITIONAL_FORMULAS = frozenset({0x0A, 0x0B} | (set(special.EXPECTED_COUNTS) - {0x57}))
PERFORMANCE_FORMULAS = frozenset({0x1C, 0x1D})
RANDOM_FIRE_FORMULAS = frozenset({0x1E, 0x1F})

EXPECTED_COUNTS = {
    "retained-native-carrier": 22,
    "ordinary-rider-data-suppression": 26,
    "support-rider-data-suppression": 1,
    "conditional-rider-data-suppression": 1,
    "conditional-producer": 82,
    "performance-producer": 2,
    "randomfire-producer": 2,
    "native-lifecycle": 5,
    "managed-instant-ko": 9,
}


def load_rows(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def mapped_carrier_ids(source: str) -> set[int]:
    match = re.search(
        r"bits\s*=\s*abilityId\s+switch\s*\{(?P<body>.*?)\n\s*\};",
        source,
        flags=re.DOTALL,
    )
    if not match:
        return set()
    ids: set[int] = set()
    for selector in re.findall(r"^\s*([0-9][0-9\sor]*)\s*=>\s*\w+Bits,", match.group("body"), flags=re.MULTILINE):
        ids.update(int(value) for value in re.findall(r"\d+", selector))
    return ids


def classify(row: dict[str, str]) -> tuple[str, str, str, str]:
    ability_id = int(row["id_dec"])
    formula = int(row["formula_hex"], 16)
    if ability_id in RETAINED_IDS:
        return (
            "retained-native-carrier",
            "mechanism-ready-live-gated",
            "paired native packet replacement",
            "Run one all-zero HP/MP carrier vertical slice for the applicable carrier family.",
        )
    if ability_id in ORDINARY_RIDER_IDS:
        return (
            "ordinary-rider-data-suppression",
            "mechanism-ready-live-gated",
            "data-neutralized rider plus managed packet",
            "Build the selected InflictStatus override and prove ordinary damage plus managed status delivery.",
        )
    if ability_id in SUPPORT_RIDER_IDS:
        return (
            "support-rider-data-suppression",
            "mechanism-ready-live-gated",
            "eligible numeric support result plus managed remove packet",
            "Build the support-rider override and live-prove numeric support plus complete managed cure delivery.",
        )
    if ability_id in CONDITIONAL_RIDER_IDS:
        return (
            "conditional-rider-data-suppression",
            "mechanism-ready-live-gated",
            "split victim/caster result plus victim-only managed rider",
            "Build the conditional-rider override and live-prove victim Oil plus an Oil-free caster KO result.",
        )
    if formula in CONDITIONAL_FORMULAS:
        return (
            "conditional-producer",
            "mechanism-ready-live-gated",
            f"execution-only formula-0x{formula:02X} post-calc packet producer",
            "Live-prove one formula-0x0A, one formula-0x0B, and one random-one carrier; then bind authored forecast/AI probability.",
        )
    if formula in PERFORMANCE_FORMULAS:
        return (
            "performance-producer",
            "mechanism-ready-live-gated",
            f"execution-only formula-0x{formula:02X} random-one packet producer with native Sleep guard",
            "Live-prove one Song and one Dance across two ticks plus a native stop/cleanup cause.",
        )
    if formula in RANDOM_FIRE_FORMULAS:
        return (
            "randomfire-producer",
            "mechanism-ready-live-gated",
            f"per-repeat formula-0x{formula:02X} post-calc random-one packet producer",
            "Live-confirm the statically mapped selector/pre-clamp/status-apply cadence, including a repeated target.",
        )
    if ability_id in NATIVE_LIFECYCLE_IDS:
        return (
            "native-lifecycle",
            "native-preserved",
            "engine-owned KO/Crystal lifecycle",
            "Rewrite only supported numeric channels; never write Dead or Crystal directly.",
        )
    if ability_id in INSTANT_KO_IDS:
        return (
            "managed-instant-ko",
            "mechanism-ready-live-gated",
            "lethal staged debit plus native KO apply",
            "Pair an authored rule with the explicit formula-0x08 data neutralizer and live-prove resist/success.",
        )
    return "unowned", "blocked", "none", "Assign one explicit mechanism owner before implementation."


def render(catalog_path: Path, carrier_source: Path) -> tuple[str, str, bool]:
    rows = [row for row in load_rows(catalog_path) if row["inflict_statuses"].strip()]
    classified = []
    for row in rows:
        category, state, owner, next_gate = classify(row)
        classified.append((row, category, state, owner, next_gate))

    counts = Counter(category for _, category, _, _, _ in classified)
    ids = {int(row["id_dec"]) for row in rows}
    owner_sets = {
        "retained-native-carrier": set(RETAINED_IDS),
        "ordinary-rider-data-suppression": set(ORDINARY_RIDER_IDS),
        "support-rider-data-suppression": set(SUPPORT_RIDER_IDS),
        "conditional-rider-data-suppression": set(CONDITIONAL_RIDER_IDS),
        "conditional-producer": {
            int(row["id_dec"]) for row in rows if int(row["formula_hex"], 16) in CONDITIONAL_FORMULAS
        },
        "performance-producer": {
            int(row["id_dec"]) for row in rows if int(row["formula_hex"], 16) in PERFORMANCE_FORMULAS
        },
        "randomfire-producer": {
            int(row["id_dec"]) for row in rows if int(row["formula_hex"], 16) in RANDOM_FIRE_FORMULAS
        },
        "native-lifecycle": set(NATIVE_LIFECYCLE_IDS),
        "managed-instant-ko": set(INSTANT_KO_IDS),
    }
    memberships = Counter(ability_id for values in owner_sets.values() for ability_id in values)
    overlaps = sorted(ability_id for ability_id, count in memberships.items() if count != 1)
    union = set().union(*owner_sets.values())

    source = carrier_source.read_text(encoding="utf-8")
    csharp_ids = mapped_carrier_ids(source)
    carrier_ok = csharp_ids == set(RETAINED_IDS)
    grouped_modes_ok = all(
        token in " ".join(source.split())
        for token in (
            "194 or 313 => DclStatusGroups.RandomOne",
            "346 => DclStatusGroups.AllOrNothing",
        )
    )
    conditional_count_ok = len(owner_sets["conditional-producer"]) == 82
    performance_count_ok = len(owner_sets["performance-producer"]) == 2
    randomfire_count_ok = len(owner_sets["randomfire-producer"]) == 2
    counts_ok = dict(counts) == EXPECTED_COUNTS
    inventory_ok = len(rows) == 150 and union == ids and not overlaps and "unowned" not in counts
    ok = carrier_ok and grouped_modes_ok and conditional_count_ok and performance_count_ok and randomfire_count_ok and counts_ok and inventory_ok

    lines = [
        "# DCL status-action authority analysis",
        "",
        "Generated by `tools/analyze_dcl_status_authority.py`.",
        "",
        "## Closure checks",
        "",
        f"- Status-bearing catalog actions: `{len(rows)}`: **{'PASS' if len(rows) == 150 else 'FAIL'}**.",
        f"- Exactly one mechanism owner per action: **{'PASS' if inventory_ok else 'FAIL'}**.",
        f"- C# retained-carrier allowlist equals the expected 22 actions: **{'PASS' if carrier_ok else 'FAIL'}**.",
        f"- Correlated C# carrier modes are fixed for 194/313/346: **{'PASS' if grouped_modes_ok else 'FAIL'}**.",
        f"- Conditional-producer inventory contains 82 actions: **{'PASS' if conditional_count_ok else 'FAIL'}**.",
        f"- Performance-producer inventory contains 2 actions: **{'PASS' if performance_count_ok else 'FAIL'}**.",
        f"- RandomFire-producer inventory contains 2 actions: **{'PASS' if randomfire_count_ok else 'FAIL'}**.",
        f"- Category counts match the exact partition: **{'PASS' if counts_ok else 'FAIL'}**.",
        "",
        "| Authority class | Count | Current boundary |",
        "| --- | ---: | --- |",
        f"| retained native carrier | {counts['retained-native-carrier']} | packet mechanism ready; live carrier gate remains |",
        f"| ordinary rider data suppression | {counts['ordinary-rider-data-suppression']} | packet mechanism ready; per-action data authoring/live gate remains |",
        f"| support rider data suppression | {counts['support-rider-data-suppression']} | numeric carrier statically proven; support integration live gate remains |",
        f"| conditional rider data suppression | {counts['conditional-rider-data-suppression']} | split result statically proven; victim/caster live gate remains |",
        f"| conditional producer | {counts['conditional-producer']} | post-calc packet mechanism ready; live carrier and forecast/AI gates remain |",
        f"| performance producer | {counts['performance-producer']} | packet and Sleep eligibility mechanisms ready; live cadence/cleanup gate remains |",
        f"| RandomFire producer | {counts['randomfire-producer']} | per-repeat packet mechanism ready; live integration confirmation remains |",
        f"| native lifecycle | {counts['native-lifecycle']} | engine ownership preserved |",
        f"| managed instant KO | {counts['managed-instant-ko']} | lethal-debit mechanism ready; data authoring/live gate remains |",
        "",
        "## Per-action authority",
        "",
        "| Id | Ability | Formula | Native mode/statuses | Authority | State | Owner |",
        "| ---: | --- | --- | --- | --- | --- | --- |",
    ]
    for row, category, state, owner, _ in classified:
        name = row["name_ivc"] or row["name_wotl"] or "<blank>"
        lines.append(
            f"| {row['id_dec']} | {name} | {row['formula_hex']} | "
            f"{row['inflict_status_mode']}: {row['inflict_statuses']} | {category} | {state} | {owner} |"
        )

    lines.extend([
        "",
        "## Frontier",
        "",
        "No catalog action is unowned. This is authority closure, not implementation closure:",
        "the 82 conditional actions, two performance actions, and two RandomFire status actions share",
        "one exact per-target execution producer. Performance preserves the native caster-Sleep stop",
        "gate; its cadence and cleanup still require live proof. RandomFire owns one fresh random-one",
        "packet per native repeat while spell-level Magic Evade remains cached per target. Live testing",
        "still confirms integration ordering. Dead and Crystal remain lifecycle",
        "effects rather than generic status bits.",
        "",
    ])

    csv_buffer = io.StringIO(newline="")
    writer = csv.writer(csv_buffer, lineterminator="\n")
    writer.writerow([
        "ability_id", "ability_name", "formula_hex", "native_mode", "native_statuses",
        "authority_class", "state", "owner", "next_gate",
    ])
    for row, category, state, owner, next_gate in classified:
        writer.writerow([
            row["id_dec"], row["name_ivc"] or row["name_wotl"] or "<blank>", row["formula_hex"],
            row["inflict_status_mode"], row["inflict_statuses"], category, state, owner, next_gate,
        ])
    return "\n".join(lines), csv_buffer.getvalue(), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--carrier-source", type=Path, default=CARRIER_SOURCE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--csv-output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    if not args.catalog.exists():
        raise SystemExit(f"ability catalog not found: {args.catalog}")
    if not args.carrier_source.exists():
        raise SystemExit(f"carrier source not found: {args.carrier_source}")

    stamp = int(time.time())
    output = args.output or ROOT / "work" / f"{stamp}-dcl-status-action-authority.md"
    csv_output = args.csv_output or output.with_suffix(".csv")
    report, csv_text, ok = render(args.catalog, args.carrier_source)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        csv_output.write_text(csv_text, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
        print(f"wrote {csv_output}")
    print("all status-action authority checks PASS" if ok else "one or more status-action authority checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
