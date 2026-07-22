#!/usr/bin/env python3
"""Smoke tests for the canonical-admission probe readiness checker."""
from __future__ import annotations

import json
import tempfile
from pathlib import Path

from analyze_dcl_canonical_admission_probe_readiness import DEFAULT_SETTINGS, load_json, validate


def main() -> int:
    summary, failures = validate(DEFAULT_SETTINGS)
    assert not failures, failures
    assert summary["binding_action"] == "spell.targeting.test"
    assert summary["template_family"] == "directNumeric"

    with tempfile.TemporaryDirectory() as tmp:
        bad_settings = Path(tmp) / "bad-settings.json"
        data = load_json(DEFAULT_SETTINGS)
        data["DclApproachEnabled"] = True
        bad_settings.write_text(json.dumps(data), encoding="utf-8")
        _, bad_failures = validate(bad_settings)
        assert any("DclApproachEnabled" in failure for failure in bad_failures)

    with tempfile.TemporaryDirectory() as tmp:
        bad_settings = Path(tmp) / "bad-settings.json"
        data = load_json(DEFAULT_SETTINGS)
        data["DclCanonicalPolicyTicketTemplatesPath"] = str(Path(tmp) / "missing-templates.json")
        bad_settings.write_text(json.dumps(data), encoding="utf-8")
        _, bad_failures = validate(bad_settings)
        assert any("DclCanonicalPolicyTicketTemplatesPath path does not exist" in failure for failure in bad_failures)

    _, wrong_ability_failures = validate(DEFAULT_SETTINGS, ability=99999)
    assert any("missing ability binding" in failure for failure in wrong_ability_failures)
    assert any("missing policy-ticket template" in failure for failure in wrong_ability_failures)

    print("canonical admission probe readiness tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
