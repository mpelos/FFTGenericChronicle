#!/usr/bin/env python3
"""Generate and validate the runtime-settings profile audit.

This report keeps the live/offline boundary explicit: which profiles are no-op, dry-run,
simulation-only, death-gate, or policy profiles, and which invariants make them safe to use.
"""
from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any


REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "work/runtime_profile_audit.md"


@dataclass(frozen=True)
class ProfileSpec:
    name: str
    path: Path
    role: str
    intent: str
    live_mutation: str
    required: tuple[str, ...] = ()


PROFILES: tuple[ProfileSpec, ...] = (
    ProfileSpec(
        "scan-live-noop",
        REPO / "work/battle-runtime-settings.v0.2.scan.live-noop.json",
        "live mapping",
        "Collect target/attacker slot, action, response, and trace evidence while preserving vanilla HP.",
        "writes HP back to vanilla result",
        ("live_noop", "has_mapping_context"),
    ),
    ProfileSpec(
        "scan-policy",
        REPO / "work/battle-runtime-settings.v0.2.scan.generated.json",
        "policy live candidate",
        "Apply the generated v0.2 response policy with scan-mode equipment slots.",
        "rewrites HP when action/attacker context is present",
        ("policy_profile", "has_mapping_context", "guarded_context_formula"),
    ),
    ProfileSpec(
        "matrix-policy",
        REPO / "work/battle-runtime-settings.v0.2.matrix.generated.json",
        "policy live candidate",
        "Apply the generated v0.2 matrix response policy with scan-mode equipment slots.",
        "rewrites HP when action/attacker context is present",
        ("policy_profile", "has_mapping_context", "guarded_context_formula"),
    ),
    ProfileSpec(
        "exact-policy-template",
        REPO / "work/battle-runtime-settings.v0.2.generated.json",
        "policy simulation/exact-offset template",
        "Generated v0.2 policy profile with exact configured slot offsets in fixture/simulation form.",
        "rewrites HP when action/attacker context is present",
        ("policy_profile", "guarded_context_formula"),
    ),
    ProfileSpec(
        "dry-run-evaluation",
        REPO / "docs/modding/examples/battle-runtime-settings.dry-run.example.json",
        "live-safe dry-run",
        "Evaluate HP damage, HP healing, MP loss, and MP gain formulas without memory writes.",
        "no writes because DryRunRewrites=true",
        ("dry_run", "hp_mp_dry_run"),
    ),
    ProfileSpec(
        "neuter-spotcheck",
        REPO / "work/battle-runtime-settings.neuter-spotcheck.json",
        "live-safe dry-run",
        "Verify data-layer neuter placeholder deltas before attempting lethal HP rewrites.",
        "no writes because DryRunRewrites=true",
        ("dry_run", "live_noop", "no_response_or_dr"),
    ),
    ProfileSpec(
        "death-flag-capture",
        REPO / "work/battle-runtime-settings.death-flag-capture.json",
        "observe-only live capture",
        "Observe vanilla deaths and log struct death diffs/follow-up without rewriting HP/MP.",
        "no HP/MP rewrites",
        ("observe_only_death_capture",),
    ),
    ProfileSpec(
        "actor-probe",
        REPO / "work/battle-runtime-settings.actor-probe.json",
        "observe-only attacker RE capture",
        "Snapshot the 0x40-0x52 unit window on damage events to validate CT-based attacker resolution.",
        "no HP/MP rewrites",
        ("actor_probe_observe_only",),
    ),
    ProfileSpec(
        "engine-death-test",
        REPO / "work/battle-runtime-settings.engine-death-test.json",
        "live architecture proof",
        "Prove engine-owned death with MinHpFloor=1: custom lethal results leave at 1 HP, then vanilla kills.",
        "rewrites HP, but never below MinHpFloor",
        ("engine_owned_death",),
    ),
    ProfileSpec(
        "death-test-hp-only",
        REPO / "work/battle-runtime-settings.death-test.json",
        "legacy/refuted death-write probe",
        "Historical Test 2b profile: force foe HP to 0 without writing the KO flag. Live evidence proved this creates a zombie, not death.",
        "writes HP to 0 for foes only; do not use as success path",
        ("legacy_death_hp_only", "foes_only", "known_ko_flag_configured"),
    ),
    ProfileSpec(
        "death-test-killflag",
        REPO / "work/battle-runtime-settings.death-test-killflag.json",
        "legacy/refuted death-write probe",
        "Historical Test 2c profile: force foe HP to 0 and set +0x61 |= 0x20. Live evidence proved the bit is an effect, not a trigger.",
        "writes HP to 0 and the KO flag for foes only; do not use as success path",
        ("legacy_death_killflag", "foes_only", "known_ko_flag_configured"),
    ),
    ProfileSpec(
        "static-dr-example",
        REPO / "docs/modding/examples/battle-runtime-settings.static-dr.example.json",
        "offline/live DR canary",
        "Prove global flat DR without attacker/equipment context.",
        "rewrites HP if deployed",
        ("static_dr", "no_response_or_dr"),
    ),
    ProfileSpec(
        "gurps-dr-example",
        REPO / "docs/modding/examples/battle-runtime-settings.gurps-dr.example.json",
        "offline GURPS DR proof",
        "GURPS-like swing/thrust tables, item-catalog DR, and wound multipliers.",
        "rewrites HP if deployed with required context",
        ("gurps_dr", "guarded_context_formula"),
    ),
    ProfileSpec(
        "sentinel-bands-example",
        REPO / "docs/modding/examples/battle-runtime-settings.sentinel-bands.example.json",
        "offline action-signal proof",
        "Classify placeholder-sized vanilla damage bands into action variables.",
        "rewrites HP if deployed",
        ("sentinel_bands", "no_response_or_dr"),
    ),
    ProfileSpec(
        "mp-example",
        REPO / "docs/modding/examples/battle-runtime-settings.mp.example.json",
        "offline MP proof",
        "Exercise signed MP loss/gain formulas and MP rewrite gates.",
        "rewrites MP if deployed",
        ("mp_rewrite",),
    ),
    ProfileSpec(
        "memtable-probe-disabled",
        REPO / "work/memtable-probe-candidates.disabled.json",
        "observe-only candidate bank",
        "Store reviewed MEMTABLE candidates disabled until live validation.",
        "no runtime effect while probes are disabled",
        ("memtable_disabled",),
    ),
)


def load_json(path: Path) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        raise AssertionError(f"missing profile: {path}")
    except json.JSONDecodeError as exc:
        raise AssertionError(f"invalid JSON {path}: {exc}") from exc


def truthy(settings: dict[str, Any], key: str) -> bool:
    return bool(settings.get(key, False))


def count(settings: dict[str, Any], key: str) -> int:
    value = settings.get(key)
    if isinstance(value, list):
        return len(value)
    if isinstance(value, dict):
        return len(value)
    return 0


def ko_flag_ok(settings: dict[str, Any]) -> bool:
    writes = settings.get("DeathStateWrites")
    if not isinstance(writes, list):
        return False
    for write in writes:
        if not isinstance(write, dict):
            continue
        if write.get("Offset") == 97 and str(write.get("Width", "")).lower() == "byte" and write.get("OrMask") == 32:
            return True
    return False


def invariant_errors(settings: dict[str, Any], invariant: str) -> list[str]:
    errors: list[str] = []

    if invariant == "live_noop":
        if settings.get("FinalDamageFormula") != "vanillaDamage":
            errors.append("FinalDamageFormula must be vanillaDamage")
        if truthy(settings, "ApplyDamageResponseRules"):
            errors.append("ApplyDamageResponseRules must be false")
        if truthy(settings, "ApplyEquipmentDr"):
            errors.append("ApplyEquipmentDr must be false")
    elif invariant == "no_response_or_dr":
        if truthy(settings, "ApplyDamageResponseRules"):
            errors.append("ApplyDamageResponseRules must be false")
        if truthy(settings, "ApplyEquipmentDr"):
            errors.append("ApplyEquipmentDr must be false")
    elif invariant == "policy_profile":
        if not truthy(settings, "RewriteObservedDamage"):
            errors.append("RewriteObservedDamage must be true")
        if not truthy(settings, "ApplyDamageResponseRules"):
            errors.append("ApplyDamageResponseRules must be true")
        if count(settings, "DamageResponseRules") == 0:
            errors.append("DamageResponseRules must not be empty")
    elif invariant == "has_mapping_context":
        for key in ["EquipmentSlots", "AttackerEquipmentSlots", "ActionSignalRules", "FormulaTraceVariables"]:
            if count(settings, key) == 0:
                errors.append(f"{key} must not be empty")
        if not truthy(settings, "LogResolvedRuntimeContext"):
            errors.append("LogResolvedRuntimeContext must be true")
    elif invariant == "guarded_context_formula":
        formula = str(settings.get("FinalDamageFormula", ""))
        if "vanillaDamage" not in formula:
            errors.append("FinalDamageFormula should include a vanillaDamage fallback")
        if "action.present" not in formula and "a.present" not in formula:
            errors.append("FinalDamageFormula should guard optional action/attacker context")
    elif invariant == "dry_run":
        if not truthy(settings, "DryRunRewrites"):
            errors.append("DryRunRewrites must be true")
    elif invariant == "hp_mp_dry_run":
        for key in ["RewriteObservedDamage", "RewriteObservedHealing", "RewriteObservedMpLoss", "RewriteObservedMpGain"]:
            if not truthy(settings, key):
                errors.append(f"{key} must be true")
    elif invariant == "observe_only_death_capture":
        for key in ["RewriteObservedDamage", "RewriteObservedHealing", "RewriteObservedMpLoss", "RewriteObservedMpGain"]:
            if truthy(settings, key):
                errors.append(f"{key} must be false")
        if not truthy(settings, "CaptureStructOnDeath"):
            errors.append("CaptureStructOnDeath must be true")
        if truthy(settings, "CauseDeathOnZeroHp"):
            errors.append("CauseDeathOnZeroHp must be false")
    elif invariant == "actor_probe_observe_only":
        for key in ["RewriteObservedDamage", "RewriteObservedHealing", "RewriteObservedMpLoss", "RewriteObservedMpGain"]:
            if truthy(settings, key):
                errors.append(f"{key} must be false")
        if not truthy(settings, "ActorProbeOnEvent"):
            errors.append("ActorProbeOnEvent must be true")
        start = int(settings.get("ActorProbeStart", -1))
        end = int(settings.get("ActorProbeEnd", -1))
        if start > 0x40 or end < 0x41:
            errors.append("ActorProbeStart/End must include Speed +0x40 and CT +0x41")
        if truthy(settings, "CauseDeathOnZeroHp"):
            errors.append("CauseDeathOnZeroHp must be false")
        if int(settings.get("MinHpFloor", 0)) != 0:
            errors.append("MinHpFloor must remain 0 in observe-only actor probe")
    elif invariant == "engine_owned_death":
        if not truthy(settings, "RewriteObservedDamage"):
            errors.append("RewriteObservedDamage must be true")
        if settings.get("FinalDamageFormula") != "9999":
            errors.append("FinalDamageFormula must be 9999")
        if int(settings.get("MinHpFloor", 0)) != 1:
            errors.append("MinHpFloor must be 1")
        if truthy(settings, "CauseDeathOnZeroHp"):
            errors.append("CauseDeathOnZeroHp must be false")
        if count(settings, "DeathStateWrites") != 0:
            errors.append("DeathStateWrites must be empty/absent for engine-owned death")
        if not truthy(settings, "CaptureStructOnDeath"):
            errors.append("CaptureStructOnDeath must be true")
        if truthy(settings, "DryRunRewrites"):
            errors.append("DryRunRewrites must be false")
        if not truthy(settings, "AffectFoes") or not truthy(settings, "AffectAllies"):
            errors.append("AffectFoes and AffectAllies must both be true for any-target testing")
    elif invariant == "legacy_death_hp_only":
        if settings.get("FinalDamageFormula") != "9999":
            errors.append("FinalDamageFormula must be 9999")
        if truthy(settings, "CauseDeathOnZeroHp"):
            errors.append("CauseDeathOnZeroHp must be false")
        if not truthy(settings, "CaptureStructOnDeath"):
            errors.append("CaptureStructOnDeath must be true")
        if int(settings.get("MinHpFloor", 0)) != 0:
            errors.append("MinHpFloor must stay disabled to preserve the historical HP=0 probe")
    elif invariant == "legacy_death_killflag":
        if settings.get("FinalDamageFormula") != "9999":
            errors.append("FinalDamageFormula must be 9999")
        if not truthy(settings, "CauseDeathOnZeroHp"):
            errors.append("CauseDeathOnZeroHp must be true")
        if not truthy(settings, "CaptureStructOnDeath"):
            errors.append("CaptureStructOnDeath must be true")
        if int(settings.get("MinHpFloor", 0)) != 0:
            errors.append("MinHpFloor must stay disabled to preserve the historical HP=0+KO-flag probe")
    elif invariant == "foes_only":
        if not truthy(settings, "AffectFoes"):
            errors.append("AffectFoes must be true")
        if truthy(settings, "AffectAllies"):
            errors.append("AffectAllies must be false")
    elif invariant == "known_ko_flag_configured":
        if not ko_flag_ok(settings):
            errors.append("DeathStateWrites must include Offset=97 Width=Byte OrMask=32")
    elif invariant == "static_dr":
        if int(settings.get("FlatDamageReduction", 0)) <= 0:
            errors.append("FlatDamageReduction must be positive")
    elif invariant == "gurps_dr":
        formula = str(settings.get("FinalDamageFormula", ""))
        if "finalDamage" not in formula:
            errors.append("FinalDamageFormula should use the derived finalDamage variable")
        if count(settings, "FormulaTables") == 0:
            errors.append("FormulaTables must not be empty")
        if count(settings, "EquipmentDrRules") == 0:
            errors.append("EquipmentDrRules must not be empty")
        if count(settings, "EquipmentSlots") == 0 or count(settings, "AttackerEquipmentSlots") == 0:
            errors.append("GURPS DR proof should include target and attacker equipment slots")
        if count(settings, "ActionSignalRules") == 0:
            errors.append("ActionSignalRules must not be empty")
        if count(settings, "FormulaTraceVariables") == 0:
            errors.append("FormulaTraceVariables must not be empty")
    elif invariant == "sentinel_bands":
        if count(settings, "ActionSignalRules") < 3:
            errors.append("ActionSignalRules should include the three sentinel bands")
        formula = str(settings.get("FinalDamageFormula", ""))
        for name in ["sentinelLow", "sentinelMid", "sentinelHigh", "vanillaDamage"]:
            if name not in formula:
                errors.append(f"FinalDamageFormula should mention {name}")
    elif invariant == "mp_rewrite":
        if not truthy(settings, "RewriteObservedMpLoss") or not truthy(settings, "RewriteObservedMpGain"):
            errors.append("MP profile must rewrite both MP loss and MP gain")
        if not settings.get("FinalMpChangeFormula"):
            errors.append("FinalMpChangeFormula must be set")
    elif invariant == "memtable_disabled":
        probes = settings.get("MemoryTableProbes", [])
        if not isinstance(probes, list):
            errors.append("MemoryTableProbes must be a list")
        for idx, probe in enumerate(probes):
            if isinstance(probe, dict) and probe.get("Enabled", False):
                errors.append(f"MemoryTableProbes[{idx}] must remain disabled")
    else:
        errors.append(f"unknown invariant: {invariant}")

    return errors


def summarize_profile(settings: dict[str, Any]) -> str:
    flags = [
        f"dryRun={truthy(settings, 'DryRunRewrites')}",
        f"hpDamage={truthy(settings, 'RewriteObservedDamage')}",
        f"hpHeal={truthy(settings, 'RewriteObservedHealing')}",
        f"mpLoss={truthy(settings, 'RewriteObservedMpLoss')}",
        f"mpGain={truthy(settings, 'RewriteObservedMpGain')}",
        f"response={truthy(settings, 'ApplyDamageResponseRules')}/{count(settings, 'DamageResponseRules')}",
        f"equipmentDr={truthy(settings, 'ApplyEquipmentDr')}/{count(settings, 'EquipmentDrRules')}",
        f"slots={count(settings, 'EquipmentSlots')}/{count(settings, 'AttackerEquipmentSlots')}",
        f"actionSignals={count(settings, 'ActionSignalRules')}",
        f"traces={count(settings, 'FormulaTraceVariables')}",
        f"deathWrite={truthy(settings, 'CauseDeathOnZeroHp')}/{count(settings, 'DeathStateWrites')}",
        f"minHpFloor={int(settings.get('MinHpFloor', 0))}",
        f"actorProbe={truthy(settings, 'ActorProbeOnEvent')}",
    ]
    return ", ".join(flags)


def audit() -> tuple[list[tuple[ProfileSpec, dict[str, Any], list[str]]], list[str]]:
    rows: list[tuple[ProfileSpec, dict[str, Any], list[str]]] = []
    all_errors: list[str] = []
    for spec in PROFILES:
        settings = load_json(spec.path)
        errors: list[str] = []
        for invariant in spec.required:
            errors.extend(f"{invariant}: {error}" for error in invariant_errors(settings, invariant))
        rows.append((spec, settings, errors))
        all_errors.extend(f"{spec.name}: {error}" for error in errors)
    return rows, all_errors


def build_report() -> str:
    rows, errors = audit()
    lines: list[str] = [
        "# Runtime Profile Audit",
        "",
        "Generated by `tools/report_runtime_profiles.py`. Do not hand-edit.",
        "",
        "This report keeps live-risk profiles, dry-run profiles, and offline proof fixtures explicit.",
        "It does not prove live behavior; it proves the checked-in profiles still match their intended",
        "safety and test roles before a live session.",
        "",
        f"Overall status: {'PASS' if not errors else 'FAIL'}",
        "",
        "## Profiles",
        "",
        "| Profile | Role | Live mutation | Invariants | Status |",
        "| --- | --- | --- | --- | --- |",
    ]
    for spec, _settings, profile_errors in rows:
        invariant_text = ", ".join(f"`{name}`" for name in spec.required) or "none"
        status = "PASS" if not profile_errors else "FAIL"
        lines.append(f"| `{spec.name}` | {spec.role} | {spec.live_mutation} | {invariant_text} | {status} |")

    lines.extend(["", "## Details", ""])
    for spec, settings, profile_errors in rows:
        relative = spec.path.relative_to(REPO).as_posix()
        lines.extend(
            [
                f"### {spec.name}",
                "",
                f"- Path: `{relative}`",
                f"- Intent: {spec.intent}",
                f"- Live mutation: {spec.live_mutation}",
                f"- Summary: {summarize_profile(settings)}",
            ]
        )
        if profile_errors:
            lines.append("- Errors:")
            lines.extend(f"  - {error}" for error in profile_errors)
        else:
            lines.append("- Errors: none")
        lines.append("")

    lines.extend(
        [
            "## Live Boundary",
            "",
            "- `scan-live-noop`, `dry-run-evaluation`, and `neuter-spotcheck` are the safer live-prep profiles.",
            "- `engine-death-test` is the active death/KO architecture proof: `MinHpFloor=1` delegates the real",
            "  KO transition to the engine.",
            "- `death-test-hp-only` and `death-test-killflag` are preserved only as historical/refuted probes.",
            "  They proved memory writes can create zombie/partial states and must not be treated as a success path.",
            "- `actor-probe` is observe-only evidence capture for CT-based attacker resolution.",
            "- Policy profiles still require live slot/action/attacker evidence before they are trusted for the",
            "  actual redesign.",
            "- Passing this audit is not permission to launch or deploy; it is an offline contract check.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate or check the runtime profile audit.")
    parser.add_argument("--check", action="store_true", help="Fail if the checked-in report is stale or invalid.")
    parser.add_argument("--output", type=Path, default=OUT, help=f"Output path. Default: {OUT}")
    args = parser.parse_args()

    try:
        report = build_report()
        _rows, errors = audit()
    except AssertionError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    if args.check:
        if errors:
            for error in errors:
                print(error, file=sys.stderr)
            return 1
        if not args.output.exists():
            print(f"missing report: {args.output}", file=sys.stderr)
            return 1
        actual = args.output.read_text(encoding="utf-8")
        if actual != report:
            print(f"stale report: {args.output} (run python tools/report_runtime_profiles.py)", file=sys.stderr)
            return 1
        print("runtime profile audit is current")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    print(f"wrote {args.output}")
    if errors:
        print(f"wrote report with {len(errors)} error(s)", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
