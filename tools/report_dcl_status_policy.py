#!/usr/bin/env python3
"""Expand the ability catalog into an explicit DCL ability-by-status policy manifest.

The catalog's InflictStatus field mixes add, clear, revive, lifecycle, and bundled behavior.  This
report separates those concerns and deliberately leaves undocumented status natures as design gates.
"""
from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = ROOT / "work" / "1783987218-dcl-ability-classification.csv"


STATUS_BITS: dict[str, tuple[int, int, str]] = {
    "Crystal": (0, 0x40, "crystal"), "Dead": (0, 0x20, "ko"), "Undead": (0, 0x10, "undead"),
    "Petrify": (1, 0x80, "petrify"), "Invite": (1, 0x40, "traitor"), "Darkness": (1, 0x20, "blind"),
    "Confusion": (1, 0x10, "confuse"), "Silence": (1, 0x08, "silence"), "BloodSuck": (1, 0x04, "vampire"),
    "Oil": (2, 0x80, "oil"), "Float": (2, 0x40, "float"), "Reraise": (2, 0x20, "reraise"),
    "Transparent": (2, 0x10, "invisible"), "Berserk": (2, 0x08, "berserk"), "Frog": (2, 0x02, "frog"),
    "Poison": (3, 0x80, "poison"), "Regen": (3, 0x40, "regen"), "Protect": (3, 0x20, "protect"),
    "Shell": (3, 0x10, "shell"), "Haste": (3, 0x08, "haste"), "Slow": (3, 0x04, "slow"),
    "Stop": (3, 0x02, "stop"), "Faith": (4, 0x80, "faith"), "Innocent": (4, 0x40, "innocent"),
    "Charm": (4, 0x20, "charm"), "Sleep": (4, 0x10, "sleep"), "DontMove": (4, 0x08, "immobilize"),
    "DontAct": (4, 0x04, "disable"), "Reflect": (4, 0x02, "reflect"), "DeathSentence": (4, 0x01, "doom"),
}

BENEFICIAL = {"Float", "Reraise", "Transparent", "Regen", "Protect", "Shell", "Haste", "Reflect"}
MENTAL_BRAVE = {"Charm", "Confusion", "Berserk"}
PHYSICAL_BODY = {"Poison"}
MAGICAL_INVERSE_FAITH = {"Petrify", "Frog", "Stop", "Slow", "Sleep", "DontMove", "DontAct", "DeathSentence"}
UNRESOLVED_NATURE = {"Darkness", "Silence", "BloodSuck", "Oil", "Undead", "Faith", "Innocent"}

CLEAR_NEGATIVE = {14, 56, 105, 149, 167, 252, 268}
CLEAR_POSITIVE = {232, 244}
REMOVE_KO = {5, 6, 107, 312}
SPECIAL_OPERATION = {277, 287, 314, 326}
PHYSICAL_DA_DM = {213, 214}


FIELDS = [
    "ability_id", "ability_name", "formula_hex", "route", "status", "status_byte_index",
    "status_mask_hex", "runtime_status_name", "operation", "resist_category", "resist_stat",
    "offense_stat", "duration_policy", "bundle_size", "readiness", "mechanism", "rationale",
]


def operation_for(ability_id: int, status: str) -> str:
    if ability_id in CLEAR_NEGATIVE:
        return "remove-negative"
    if ability_id in CLEAR_POSITIVE:
        return "remove-positive"
    if ability_id in REMOVE_KO and status == "Dead":
        return "remove-ko"
    if ability_id in SPECIAL_OPERATION:
        return "special-operation-review"
    if status in BENEFICIAL or status in {"Faith", "Innocent"}:
        return "add-buff-or-trait"
    return "add-harmful"


def policy_for(ability_id: int, status: str, operation: str, byte_index: int) -> tuple[str, str, str, str, str, str, str]:
    if operation == "remove-ko":
        return ("lifecycle", "none", "native-revive-rule", "immediate", "native-lifecycle-preserved",
                "preserve_native_revive", "The native revive action owns KO removal and lifecycle state. DCL may rewrite only the staged HP credit and must not clear status byte 0 directly.")
    if status == "Crystal":
        return ("lifecycle", "none", "native-Bequeath-rule", "campaign", "native-lifecycle-preserved",
                "preserve_native_bequeath", "Bequeath Bacon's native formula owns the bounded level gain and caster Crystal lifecycle. DCL preserves it unchanged and never writes Crystal directly.")
    if operation.startswith("remove-"):
        return ("none", "none", "none", "immediate", "surface-ready", "dcl_status_remove",
                "Removal uses the authored status-bit clear path and does not roll resistance.")
    if operation == "special-operation-review":
        if byte_index == 0:
            return ("lifecycle", "explicit-rule", "explicit-rule", "lifecycle", "lifecycle-mechanism-required",
                    "explicit_lifecycle_rule", "This special touches protected status byte 0 and needs lifecycle-safe ownership.")
        return ("special", "explicit-rule", "explicit-rule", "explicit-rule", "authoring-required",
                "explicit_special_status_rule", "Catalog metadata does not prove whether this special adds, clears, or targets the caster.")
    if operation == "add-buff-or-trait":
        return ("beneficial", "none", "caster-MA-or-authored", "authoring-required", "authoring-required",
                "dcl_status_add", "Beneficial status has no defensive resistance contest; hit/duration policy remains ability-authored.")
    if status in MENTAL_BRAVE:
        return ("mental-will", "high-Brave", "authored-skill-or-MA", "authoring-required", "surface-ready",
                "dcl_status_contest", "DCL explicitly moves will-override mental statuses onto Brave resistance.")
    if status == "Poison":
        return ("physical-body", "base-HP", "authored-skill-or-MA", "authoring-required", "surface-ready",
                "dcl_status_contest", "Poison follows its physical affliction nature even when delivered by a spell.")
    if status in {"DontMove", "DontAct"} and ability_id in PHYSICAL_DA_DM:
        label = "Knockdown" if status == "DontMove" else "Stun"
        return ("physical-body", "base-HP", "authored-physical-skill", "1-target-turn", "surface-ready",
                "dcl_status_contest", f"{label} is the physical one-turn reskin of the shared native flag.")
    if status in MAGICAL_INVERSE_FAITH:
        return ("magical-inverted", "inverse-Faith", "caster-MA", "authoring-required", "surface-ready",
                "dcl_status_contest", "DCL explicitly assigns this magical status to inverse-Faith resistance.")
    if status == "Dead":
        return ("lifecycle", "explicit-rule", "explicit-rule", "lifecycle", "data-authoring-required",
                "dcl_instant_ko", "The runtime can deliver engine-owned KO through lethal staged HP after the native Dead rider is removed in action data; ordinary damage and resistance formulas remain per-ability authoring.")
    if status == "Invite":
        return ("campaign-side-effect", "design-decision-required", "design-decision-required", "campaign",
                "campaign-mechanism-required", "campaign_recruitment_rule",
                "Invite/traitor changes battle allegiance and may affect recruitment; it is not an ordinary temporary bit.")
    if status in UNRESOLVED_NATURE:
        readiness = "design-decision-required"
        return ("unresolved-nature", "design-decision-required", "design-decision-required", "authoring-required",
                readiness, "dcl_status_policy_required",
                "The DCL status chapter does not yet assign this status to physical, mental, or magical resistance.")
    raise KeyError(f"no policy for status {status}")


def load_manifest(path: Path) -> tuple[list[dict[str, str]], list[str]]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        abilities = list(csv.DictReader(handle))
    rows: list[dict[str, str]] = []
    errors: list[str] = []
    for ability in abilities:
        raw = ability.get("inflict_statuses", "").strip()
        if not raw:
            continue
        statuses = [token.strip() for token in raw.split("|") if token.strip()]
        ability_id = int(ability["ability_id"])
        for status in statuses:
            bit = STATUS_BITS.get(status)
            if bit is None:
                errors.append(f"ability {ability_id} has unmapped status token {status}")
                continue
            byte_index, mask, runtime_name = bit
            operation = operation_for(ability_id, status)
            try:
                category, resist, offense, duration, readiness, mechanism, rationale = policy_for(
                    ability_id, status, operation, byte_index)
            except KeyError as exc:
                errors.append(str(exc))
                continue
            rows.append({
                "ability_id": str(ability_id), "ability_name": ability["name"],
                "formula_hex": ability["formula_hex"], "route": ability["route"], "status": status,
                "status_byte_index": str(byte_index), "status_mask_hex": f"0x{mask:02X}",
                "runtime_status_name": runtime_name, "operation": operation,
                "resist_category": category, "resist_stat": resist, "offense_stat": offense,
                "duration_policy": duration, "bundle_size": str(len(statuses)), "readiness": readiness,
                "mechanism": mechanism, "rationale": rationale,
            })
    if len(abilities) != 512:
        errors.append(f"expected 512 source abilities, found {len(abilities)}")
    if len(rows) != 294:
        errors.append(f"expected 294 ability-status rows, found {len(rows)}")
    return rows, errors


def write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)


def render_markdown(rows: list[dict[str, str]], source: Path, csv_path: Path | None) -> str:
    readiness = Counter(row["readiness"] for row in rows)
    categories = Counter(row["resist_category"] for row in rows)
    operations = Counter(row["operation"] for row in rows)
    status_policies: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        status_policies[row["status"]].append(row)
    bundle_abilities = len({row["ability_id"] for row in rows if int(row["bundle_size"]) > 1})
    lines = [
        "# DCL ability-by-status policy manifest", "",
        f"Source: `{source.as_posix()}` (`512` abilities).",
    ]
    if csv_path:
        lines.append(f"Row manifest: `{csv_path.as_posix()}`.")
    lines.extend([
        "", f"Expanded status rows: **{len(rows)}** across **{len(status_policies)}** native status tokens; multi-status abilities: **{bundle_abilities}**.",
        "", "This report separates add, clear, revive, lifecycle, campaign, and bundle semantics. It does",
        "not assign a resist stat where the DCL document still leaves the status nature undecided.",
        "", "## Readiness", "", "| Readiness | Ability-status rows |", "| --- | ---: |",
    ])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(readiness.items()))
    lines.extend(["", "## Operations", "", "| Operation | Rows |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(operations.items()))
    lines.extend(["", "## Resistance categories", "", "| Category | Rows |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(categories.items()))
    lines.extend(["", "## Status-token policy", "", "| Status | Uses | Operations | Categories | Resist stats | Readiness |", "| --- | ---: | --- | --- | --- | --- |"])
    for status, group in sorted(status_policies.items()):
        lines.append(
            f"| {status} | {len(group)} | {', '.join(sorted({r['operation'] for r in group}))} | "
            f"{', '.join(sorted({r['resist_category'] for r in group}))} | {', '.join(sorted({r['resist_stat'] for r in group}))} | "
            f"{', '.join(sorted({r['readiness'] for r in group}))} |")
    lines.extend([
        "", "## Mechanism consequences", "",
        "- Multi-status abilities expand to independent per-status rules; the existing runtime rolls each authored add rule independently.",
        "- `Dead`, `Undead`, and `Crystal` live in status byte 0. Generic `DclStatusRule` whitelists only proven-safe Undead `0x10`; offensive KO uses the dedicated lethal-debit route and Crystal remains native lifecycle state.",
        "- Nine offensive Dead riders have runtime mechanism coverage but still require data-side native-rider suppression plus per-ability ordinary-damage/resistance authoring.",
        "- Raise/Arise/Revive/Squeal preserve the native revive lifecycle. DCL amount authoring is limited to staged HP credit; generic status removal never clears KO.",
        "- Bequeath Bacon preserves native formula `0x57`: bounded target level gain plus caster Crystal/campaign lifecycle. The DCL never writes or clears Crystal.",
        "- `Invite` is a battle/campaign allegiance operation, not a normal temporary status.",
        "- Physical Stun/Knockdown and magical Don't Act/Don't Move share bits; ability ids 213/214 are the current explicit physical candidates, while other uses remain magical by documented nature.",
        "- Darkness, Silence, BloodSuck, Oil, Undead, Faith, and Innocent still need an explicit DCL nature/resistance decision.",
        "",])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--csv", type=Path)
    parser.add_argument("--markdown", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    rows, errors = load_manifest(args.catalog)
    if errors:
        raise SystemExit("\n".join(errors))
    if args.csv:
        write_csv(args.csv, rows)
    markdown = render_markdown(rows, args.catalog, args.csv)
    if args.markdown:
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        args.markdown.write_text(markdown, encoding="utf-8")
    else:
        print(markdown)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
