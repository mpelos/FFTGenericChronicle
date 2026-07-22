#!/usr/bin/env python3
"""Static smoke test for the canonical-admission live prep helper."""
from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "codemod" / "prepare-canonical-admission-live.ps1"
RUNBOOK = ROOT / "work" / "1784545200-canonical-admission-live-runbook.md"


def main() -> int:
    script = SCRIPT.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    required_script_terms = [
        "analyze_dcl_canonical_admission_probe_readiness.py",
        "build-deploy.ps1",
        "1784673033-battle-runtime-settings.canonical-admission-sentinel.json",
        "bak-canonical-admission",
        "collect_dcl_canonical_admission_live_log.py",
        "AutosaveSnapshot",
        "Assert-AutosaveFixtureMetadata",
        "RequireFixtureKind",
        "canonical-admission-pre-action",
    ]
    for term in required_script_terms:
        assert term in script, f"helper is missing {term}"
    assert "EnableModInAppConfig" not in script, "helper must not edit Reloaded-II AppConfig"
    assert "Load Manual Save 05" not in script, "helper must not instruct using Save 05 as the proof entry"

    required_runbook_terms = [
        "prepare-canonical-admission-live.ps1 -DryRun",
        "prepare-canonical-admission-live.ps1",
        "analyze_dcl_canonical_admission_probe_readiness.py --check-only",
        "collect_dcl_canonical_admission_live_log.py",
        "canonical-admission pre-action autosave snapshot",
        "-FixtureKind canonical-admission-pre-action",
        "1784092904-fft-autoenhanced-snapshot.png.fixture.json",
    ]
    for term in required_runbook_terms:
        assert term in runbook, f"runbook is missing {term}"
    assert "RVA `0x281EFA`" in runbook, "runbook should require the live loop hook RVA"

    manager = (ROOT / "tools" / "manage_fft_enhanced_autosave.ps1").read_text(encoding="utf-8")
    assert "FixtureKind" in manager
    assert "RequireFixtureKind" in manager
    assert ".fixture.json" in manager

    launcher = (ROOT / "tools" / "launch_fft_enhanced_test.ps1").read_text(encoding="utf-8")
    assert "RequireAutosaveFixtureKind" in launcher
    assert "RequireFixtureKind" in launcher

    fixture_sidecar = ROOT / "work" / "1784092904-fft-autoenhanced-snapshot.png.fixture.json"
    assert fixture_sidecar.exists(), "canonical admission autosave fixture sidecar is missing"
    fixture = fixture_sidecar.read_text(encoding="utf-8")
    assert "canonical-admission-pre-action" in fixture
    assert "1CB4ACEB69388185F4EC9E4BB3A47D052F0CC31ED929713A962C34EEE6951AF8" in fixture

    print("canonical admission live prep helper static test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
