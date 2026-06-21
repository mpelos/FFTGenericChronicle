#!/usr/bin/env python3
"""Generate an offline-readiness audit for the battle-mechanics code-mod path.

The report is intentionally conservative: it does not claim live completion. It records whether
the repo has the offline artifacts, profiles, tests, and generated reports needed before the next
live pass, then lists the remaining live-only gates.
"""
from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path

import report_runtime_formula_context
import report_runtime_profiles


REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "work" / "offline_readiness_audit.md"


@dataclass(frozen=True)
class Check:
    name: str
    status: str
    evidence: str
    note: str


@dataclass(frozen=True)
class LiveGate:
    name: str
    why_live_only: str
    prepared_offline: str
    command_hint: str


def rel(path: Path) -> str:
    return path.relative_to(REPO).as_posix()


def exists(path: Path) -> bool:
    return path.exists()


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def contains(path: Path, *needles: str) -> bool:
    if not path.exists():
        return False
    text = path.read_text(encoding="utf-8", errors="replace")
    return all(needle in text for needle in needles)


def file_check(name: str, path: Path, note: str, *needles: str) -> Check:
    ok = exists(path) and (not needles or contains(path, *needles))
    evidence = f"`{rel(path)}`" + (f" contains {', '.join(f'`{needle}`' for needle in needles)}" if needles else "")
    return Check(name, "PASS" if ok else "FAIL", evidence, note)


def profile_check() -> Check:
    _rows, errors = report_runtime_profiles.audit()
    return Check(
        "runtime profiles",
        "PASS" if not errors else "FAIL",
        "`tools/report_runtime_profiles.py` / `work/runtime_profile_audit.md`",
        "All live-risk, dry-run, proof, and observe-only profiles satisfy their invariants.",
    )


def formula_context_check() -> Check:
    generated = report_runtime_formula_context.build_report()
    path = report_runtime_formula_context.OUT
    ok = path.exists() and path.read_text(encoding="utf-8") == generated
    return Check(
        "formula context catalog",
        "PASS" if ok else "FAIL",
        "`tools/report_runtime_formula_context.py` / `work/runtime_formula_context.md`",
        "Formula variables/functions/action/slot/source flags are current and source-derived.",
    )


def custom_demo_check() -> Check:
    settings = load_json(REPO / "work" / "battle-runtime-settings.custom-formula-demo.json")
    scenarios = load_json(REPO / "work" / "runtime-simulation.custom-formula-demo.json")
    scenario_text = json.dumps(scenarios, sort_keys=True)
    helper = REPO / "codemod" / "prepare-custom-formula-demo.ps1"
    ok = (
        settings.get("ResolveAttackerByCt") is True
        and settings.get("ResolveCounterFromRecentDamage") is True
        and settings.get("MinHpFloor") == 1
        and "ct-reset" in scenario_text
        and "counter-inversion" in scenario_text
        and "trace.attackersourcect=1" in scenario_text
        and "trace.attackersourcecounter=1" in scenario_text
        and contains(
            helper,
            "Preparing Generic Chronicle custom formula demo",
            "--ct-runtime-attackers",
            "trace.attackerpa",
            "trace.targetfaith",
        )
    )
    return Check(
        "attacker-dependent custom formula demo",
        "PASS" if ok else "FAIL",
        "`work/battle-runtime-settings.custom-formula-demo.json` + `work/runtime-simulation.custom-formula-demo.json` + `codemod/prepare-custom-formula-demo.ps1`",
        "Offline scenario and live helper cover CT attacker source, counter inversion source, CT vars, MinHpFloor, no-attacker guard, and formula trace requirements.",
    )


def static_scan_check() -> Check:
    path = REPO / "work" / "static_code_pattern_scan.md"
    ok = contains(
        path,
        "`battle_base_ptr`",
        "`0x226D98",
        "`damage_multiplier` | 0",
        "PASS_ABSENT",
    )
    return Check(
        "static executable anchors",
        "PASS" if ok else "WARN",
        "`tools/scan_static_code_patterns.py` / `work/static_code_pattern_scan.md`",
        "Read-only PE scan confirms stable enhanced anchors when the installed executable is present; direct damage site remains runtime-only.",
    )


def stale_death_gate_text_check() -> Check:
    paths = [
        REPO / "docs" / "modding" / "00-overview.md",
        REPO / "docs" / "modding" / "06-code-mod-battle-runtime-architecture.md",
        REPO / "docs" / "modding" / "07-live-findings.md",
        REPO / "codemod" / "prepare-death-gate.ps1",
        REPO / "codemod" / "check-death-gate-readiness.ps1",
        REPO / "tools" / "report_neuter_coverage.py",
        REPO / "work" / "neuter_coverage.md",
        REPO / "work" / "runtime_formula_context.md",
        REPO / "work" / "live_gate_plan.md",
        REPO / "work" / "battle-runtime-settings.death-flag-capture.json",
        REPO / "work" / "battle-runtime-settings.death-test.json",
        REPO / "work" / "battle-runtime-settings.death-test-killflag.json",
        REPO / "work" / "battle-runtime-settings.death-test-anytarget.json",
        REPO / "work" / "battle-runtime-settings.death-test-killflag-anytarget.json",
        REPO / "work" / "battle-runtime-settings.neuter-spotcheck.json",
    ]
    banned = [
        "NEXT - LEVEL 2 death gate",
        "Current next live gate",
        "runtime still has to prove Test 2b: HP=0 and KO flag ownership",
        "must also write the mapped KO flag",
        "before switching to the killflag profile",
        "Test 2b still has to",
        "Preparing Generic Chronicle death gate",
        "Outcome A: HP=0 alone produced death evidence",
        "Outcome B: lethal HP rewrite happened",
        "next-gate step 2",
        "If it dies -> we can cause death ourselves",
        "If it ZOMBIES, switch",
    ]
    hits: list[str] = []
    for path in paths:
        if not path.exists():
            hits.append(f"missing {rel(path)}")
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        for phrase in banned:
            if phrase in text:
                hits.append(f"{rel(path)} contains {phrase!r}")
    return Check(
        "death/KO doc consistency",
        "PASS" if not hits else "FAIL",
        ", ".join(f"`{rel(path)}`" for path in paths),
        "Canonical docs/reports must not describe HP=0/KO-bit ownership as a pending success path."
        if not hits
        else "; ".join(hits),
    )


def build_checks() -> list[Check]:
    return [
        file_check(
            "offline gate runner",
            REPO / "codemod" / "run-offline-checks.ps1",
            "Single offline gate covers syntax, Python tooling, JSON, installed-exe static scan when available, C# build/smokes, validators, simulators, and dry-run helpers.",
            "Installed executable static scan",
            "Offline checks passed",
        ),
        profile_check(),
        formula_context_check(),
        custom_demo_check(),
        file_check(
            "neuter placeholder builder",
            REPO / "tools" / "build_neuter_data.py",
            "Uniform placeholder neuter remains default; sentinel-coarse-v1 is opt-in calibration data.",
            "placeholder-mode",
            "sentinel-coarse-v1",
        ),
        file_check(
            "neuter gap report",
            REPO / "work" / "neuter_gap_targets.md",
            "Known risky families are enumerated so live spot-checks have concrete targets.",
            "Materia Blade Plus",
            "Gravity",
            "Cloud Limit",
        ),
        file_check(
            "sentinel action identity profile",
            REPO / "work" / "battle-runtime-settings.sentinel-coarse-v1.json",
            "Coarse action identity is wired through ActionSignalRules and guarded by action.present.",
            "sentinelLow",
            "sentinelMid",
            "sentinelHigh",
        ),
        file_check(
            "hook-register probe profile",
            REPO / "work" / "battle-runtime-settings.hook-register-probe.json",
            "Observe-only register snapshot profile exists for pre-damage/current-actor RE.",
            "HookRegisterProbe",
            "HookRegisterProbeMaxLogs",
        ),
        file_check(
            "runtime log analyzer",
            REPO / "tools" / "analyze_battleprobe_log.py",
            "Analyzer understands runtime attacker sources and hook register snapshots.",
            "HOOK_REGS_RE",
            "attacker_source",
        ),
        file_check(
            "live watcher",
            REPO / "tools" / "watch_live_mapping.py",
            "Watcher can require CT/counter attacker evidence, action variables/signals, rewrites, deaths, and hook-register captures.",
            "--ct-runtime-attackers",
            "--counter-runtime-attackers",
            "--hook-regs",
        ),
        file_check(
            "live gate runbook",
            REPO / "work" / "live_gate_plan.md",
            "The remaining live-only gates have an ordered, generated runbook with preparation helpers, watcher commands, and pass evidence.",
            "prepare-custom-formula-demo.ps1",
            "prepare-sentinel-coarse.ps1",
            "battle-runtime-settings.hook-register-probe.json",
            "Direct HP=0",
        ),
        file_check(
            "death/KO canonical finding",
            REPO / "docs" / "modding" / "07-live-findings.md",
            "Docs record that direct HP=0/KO writes are refuted and engine-owned death is the current architecture.",
            "MinHpFloor",
            "REWRITE-SKIP-DEATH",
            "zombie",
            "engine-owned death",
        ),
        stale_death_gate_text_check(),
        static_scan_check(),
    ]


def build_live_gates() -> list[LiveGate]:
    return [
        LiveGate(
            "custom formula demo live proof",
            "Only the game can prove the deployed DLL/data neuter produces attacker+target-dependent HP outcomes on real battle events.",
            "`prepare-custom-formula-demo.ps1`, `custom-formula-demo` profile, CT/counter source traces, MinHpFloor simulation, and watcher/analyzer support.",
            "codemod\\prepare-custom-formula-demo.ps1 -DryRun; python tools/watch_live_mapping.py --runtime-events 3 --ct-runtime-attackers 1 --require-trace-var trace.attackerpa --require-trace-var trace.targetfaith --require-trace-var trace.finaldamage --max-rewrite-failures 0",
        ),
        LiveGate(
            "sentinel-coarse action identity calibration",
            "The data-layer placeholder bands can overlap or be bypassed by real vanilla formulas; only live attacks can calibrate ranges.",
            "`prepare-sentinel-coarse.ps1`, sentinel coarse profile/scenarios, and watcher action-signal requirements.",
            "python tools/watch_live_mapping.py --runtime-events 3 --action-signals 3 --require-action-signal 301 --require-action-signal 302 --require-action-signal 303 --max-placeholder-damage 90 --max-rewrite-failures 0",
        ),
        LiveGate(
            "equipment slot confirmation for DR",
            "Exact equipment offsets/scan hits must be confirmed in live unit structs before armor DR can be trusted for real battles.",
            "Slot-aware formula context, item catalog, GURPS/static DR simulations, and promote_runtime_offsets.py.",
            "python tools/watch_live_mapping.py --runtime-events 0 --target-slots-present 1 --equipment-dr-events 1 --rewrite-events 1 --max-rewrite-failures 0",
        ),
        LiveGate(
            "hook-register/pre-damage clue capture",
            "Same-hit/pre-damage replacement depends on runtime register/context evidence; the direct damage site is absent from static file scan.",
            "`hook-register-probe` observe-only profile, static AOB scan, analyzer summary, and watcher --hook-regs gate.",
            "python tools/watch_live_mapping.py --runtime-events 0 --hook-regs 12 --skip-analyze",
        ),
        LiveGate(
            "neuter gap spot-checks",
            "Materia Blade+, Gravity/% damage, Cloud Limits, Throw/Jump/Aim may bypass or ignore generic Power/X/Y levers in live formulas.",
            "`work/neuter_gap_targets.md`, neuter builder tests, and placeholder rewrite watcher thresholds.",
            "python tools/watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 3 --max-placeholder-damage 30 --max-large-vanilla-rewrites 0 --max-rewrite-failures 0",
        ),
        LiveGate(
            "same-hit KO path",
            "Direct HP=0 and +0x61 KO writes are refuted; replacing engine death on the same hit requires an undiscovered runtime pre-damage hook.",
            "Engine-owned MinHpFloor path, historical death profiles, death analyzer, and hook-register/static-scan RE aids.",
            "No safe offline command; collect hook/register/debugger evidence first.",
        ),
    ]


def render(checks: list[Check], live_gates: list[LiveGate]) -> str:
    hard_failures = [check for check in checks if check.status == "FAIL"]
    status = "PASS" if not hard_failures else "FAIL"
    lines: list[str] = [
        "# Offline Readiness Audit",
        "",
        "Generated by `tools/report_offline_readiness.py`. Do not hand-edit.",
        "",
        "This report answers whether the repo has exhausted the useful offline preparation for the",
        "flexible battle-mechanics code mod. It does **not** claim the live behavior is proven.",
        "",
        f"Overall offline status: {status}",
        "",
        "## Offline Checks",
        "",
        "| Area | Status | Evidence | Note |",
        "| --- | --- | --- | --- |",
    ]
    for check in checks:
        lines.append(f"| {check.name} | {check.status} | {check.evidence} | {check.note} |")

    lines.extend(
        [
            "",
            "## Live-Only Gates",
            "",
            "| Gate | Why Live-Only | Offline Prep Already Present | Command / Evidence Hint |",
            "| --- | --- | --- | --- |",
        ]
    )
    for gate in live_gates:
        lines.append(
            f"| {gate.name} | {gate.why_live_only} | {gate.prepared_offline} | `{gate.command_hint}` |"
        )

    lines.extend(
        [
            "",
            "## Read",
            "",
            "- If all offline checks pass, the next meaningful evidence must come from controlled live runs.",
            "- Offline work can still add convenience around those runs, but it cannot prove action bands,",
            "  exact equipment slots, runtime register context, or same-hit death behavior by itself.",
            "- Historical HP=0/KO-write probes remain in the repo as diagnostics, not as the accepted death path.",
            "",
        ]
    )
    return "\n".join(lines)


def build_report() -> str:
    return render(build_checks(), build_live_gates())


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the offline-readiness audit report.")
    parser.add_argument("--check", action="store_true", help="Fail if the checked-in report is stale or has hard failures.")
    parser.add_argument("--output", type=Path, default=OUT, help=f"Output path. Default: {OUT}")
    args = parser.parse_args()

    report = build_report()
    hard_fail = "Overall offline status: FAIL" in report

    if args.check:
        if not args.output.exists():
            print(f"missing report: {args.output}", file=sys.stderr)
            return 1
        actual = args.output.read_text(encoding="utf-8")
        if actual != report:
            print(f"stale report: {args.output} (run python tools/report_offline_readiness.py)", file=sys.stderr)
            return 1
        if hard_fail:
            print(f"offline readiness has hard failures: {args.output}", file=sys.stderr)
            return 1
        print("offline readiness report is current")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {args.output}")
    return 1 if hard_fail else 0


if __name__ == "__main__":
    raise SystemExit(main())
