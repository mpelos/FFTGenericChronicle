#!/usr/bin/env python3
"""Smoke tests for the canonical-admission template live analyzer."""
from __future__ import annotations

from pathlib import Path
import re

from analyze_dcl_canonical_admission_template_live import analyze_text


ROOT = Path(__file__).resolve().parents[1]
MOD_CS = ROOT / "codemod/fftivc.generic.chronicle.codemod/Mod.cs"


VALID = "\n".join(
    (
        "[DCL-CANONICAL-ADMISSION-HOOK] rva=0x281EFA addr=0x7FF612345678",
        "[DCL-STATE-RESET] generation=12 reason=battle-start canonicalBattle=1",
        "[DCL-CANONICAL-ADMISSION] sweep=7 action=1001 source=2:0x22 actionType=0x02 "
        "ability=16 selected=4:0x44 tile=10,0,12 repeat=0/1 strikes=1 targetCount=1 "
        "targets=[4:0x44] complete=1 admissionStatus=Published templateStatus=Built "
        "ticketStatus=Published bridgeStatus=Published",
        "[DAMAGE ptr=0x141855CE0 id=0x44] 199 -> 128 = 71 sampleAgeMs=27",
    )
)


def main() -> int:
    mod_source = MOD_CS.read_text(encoding="utf-8-sig")
    assert 'strikes={completed.Admissions.Count}' in mod_source
    for method in (
        "BuildDclCanonicalAdmissionShimLines",
        "BuildDclCanonicalPostApplyShimLines",
        "BuildDclCanonicalReactionEffectCompletionShimLines",
        "BuildDclCanonicalReactionCompletionShimLines",
    ):
        match = re.search(
            rf"{method}\(\).*?return\s*\[\s*(?://[^\n]*\n\s*)*\"use64\"",
            mod_source,
            flags=re.S,
        )
    assert match is not None, f"{method} must start its FASM block with use64"
    assert "push rbx" in mod_source
    assert "sub rsp, 88h" in mod_source
    assert "pop rax\", \"pop rbx" in mod_source
    assert "ShouldSuppressDclCanonicalAdmissionDuplicate" in mod_source
    assert "BuildDclCanonicalAdmissionDuplicateKey" in mod_source
    assert "StopwatchTicksFromMilliseconds(250)" in mod_source
    assert "_dclCanonicalAdmissionDuplicateSuppressor.Clear()" in mod_source

    summary, failures = analyze_text(
        VALID,
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=True,
    )
    assert not failures, failures
    assert summary["matching_action"] == 1001
    assert summary["matching_target_char"] == "0x44"

    _, no_damage_failures = analyze_text(
        "\n".join(line for line in VALID.splitlines() if "[DAMAGE " not in line),
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=True,
    )
    assert any("missing later positive [DAMAGE]" in failure for failure in no_damage_failures)

    _, wrong_status_failures = analyze_text(
        VALID.replace("templateStatus=Built", "templateStatus=MissingTemplate"),
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=True,
    )
    assert any("templateStatus" in failure for failure in wrong_status_failures)

    _, duplicate_failures = analyze_text(
        VALID + "\n" + VALID.splitlines()[2],
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=False,
    )
    assert any("expected exactly one complete admission" in failure for failure in duplicate_failures)

    _, wrong_hook_failures = analyze_text(
        VALID.replace("rva=0x281EFA", "rva=0x281EFB"),
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=False,
    )
    assert any("missing active canonical admission hook" in failure for failure in wrong_hook_failures)

    print("canonical admission template live analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
