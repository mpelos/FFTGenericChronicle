#!/usr/bin/env python3
"""Validate the offline readiness of the canonical-admission live probe fixture."""
from __future__ import annotations

import argparse
import json
import time
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SETTINGS = ROOT / "work" / "1784673033-battle-runtime-settings.canonical-admission-sentinel.json"
ALLOWED_ENABLED_TRUE = {
    "DclCanonicalRuntimeEnabled",
    "DclCanonicalAdmissionEnabled",
}
REQUIRED_CANONICAL_PATHS = {
    "DclCanonicalAuthoringPath",
    "DclCanonicalItemMetadataPath",
    "DclCanonicalAbilityBindingsPath",
    "DclCanonicalReactionBindingsPath",
    "DclCanonicalPolicyTicketTemplatesPath",
}
REQUIRED_FALSE = {
    "RewriteObservedDamage",
    "RewriteObservedHealing",
    "RewriteObservedMpLoss",
    "RewriteObservedMpGain",
    "DryRunRewrites",
    "DclPipelineEnabled",
    "DclComputePointNumericEnabled",
    "DclResultFlagsControlEnabled",
    "DclHitControlEnabled",
    "DclPhysicalContestEnabled",
    "DclMagicEvadeEnabled",
    "DclPreviewAmountEnabled",
    "DclPreviewHitPctEnabled",
    "DclStatusControlEnabled",
    "DclFearControlEnabled",
    "DclInterruptControlEnabled",
    "DclInstantKoControlEnabled",
    "DclReactionTaxonomyEnabled",
    "DclMissOutputControlEnabled",
    "DclMissSelectorOutcomeEnabled",
    "DclMissSuppressReactionsEnabled",
    "DclMissPresentationEnabled",
    "PreClampDamageRewriteEnabled",
    "PreClampManagedCallbackEnabled",
    "DclCanonicalPostApplyEnabled",
    "DclCanonicalReactionEffectCompletionEnabled",
    "DclCanonicalReactionCompletionEnabled",
    "DclApproachEnabled",
    "DclSyntheticReactionEnabled",
    "DclReactionReservationArbitrationEnabled",
}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def repo_relative_or_absolute(path_value: str) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else ROOT / path


def find_binding(bindings: dict[str, Any], ability: int) -> dict[str, Any] | None:
    for binding in bindings.get("bindings", []):
        if binding.get("abilityId") == ability:
            return binding
    return None


def find_action(authoring: dict[str, Any], action_id: str, revision: int) -> dict[str, Any] | None:
    for action in authoring.get("actions", []):
        if action.get("actionId") == action_id and action.get("profileRevision") == revision:
            return action
    return None


def find_template(templates: dict[str, Any], ability: int) -> dict[str, Any] | None:
    for template in templates.get("templates", []):
        if template.get("abilityId") == ability:
            return template
    return None


def validate(settings_path: Path, *, ability: int = 16) -> tuple[dict[str, Any], list[str]]:
    failures: list[str] = []
    settings_path = settings_path.resolve()
    if not settings_path.exists():
        return {"settings": str(settings_path)}, [f"settings file does not exist: {settings_path}"]
    settings = load_json(settings_path)

    for key in REQUIRED_FALSE:
        if settings.get(key) is True:
            failures.append(f"{key} must be false for the admission-only sentinel")

    enabled_true = sorted(
        key
        for key, value in settings.items()
        if key.endswith("Enabled") and value is True
    )
    unexpected_enabled = [key for key in enabled_true if key not in ALLOWED_ENABLED_TRUE]
    if unexpected_enabled:
        failures.append("unexpected Enabled=true switches: " + ", ".join(unexpected_enabled))

    for key in ALLOWED_ENABLED_TRUE:
        if settings.get(key) is not True:
            failures.append(f"{key} must be true")

    referenced: dict[str, Path] = {}
    for key in sorted(REQUIRED_CANONICAL_PATHS):
        raw = settings.get(key)
        if not isinstance(raw, str) or not raw.strip():
            failures.append(f"{key} must be a nonempty path")
            continue
        path = repo_relative_or_absolute(raw)
        referenced[key] = path
        if not path.exists():
            failures.append(f"{key} path does not exist: {path}")

    authoring: dict[str, Any] = {}
    bindings: dict[str, Any] = {}
    templates: dict[str, Any] = {}
    try:
        if "DclCanonicalAuthoringPath" in referenced and referenced["DclCanonicalAuthoringPath"].exists():
            authoring = load_json(referenced["DclCanonicalAuthoringPath"])
        if "DclCanonicalAbilityBindingsPath" in referenced and referenced["DclCanonicalAbilityBindingsPath"].exists():
            bindings = load_json(referenced["DclCanonicalAbilityBindingsPath"])
        if (
            "DclCanonicalPolicyTicketTemplatesPath" in referenced
            and referenced["DclCanonicalPolicyTicketTemplatesPath"].exists()
        ):
            templates = load_json(referenced["DclCanonicalPolicyTicketTemplatesPath"])
    except (OSError, json.JSONDecodeError) as exc:
        failures.append(f"invalid referenced JSON: {exc}")

    binding = find_binding(bindings, ability) if bindings else None
    action = None
    if binding is None:
        failures.append(f"missing ability binding for ability={ability}")
    else:
        expected_binding = {
            "nativeFormula": 8,
            "carrierKind": "singleResult",
            "rewritePolicy": "replaceCompleteResult",
            "nativeStrikeCountPolicy": "exactProfile",
        }
        for key, expected in expected_binding.items():
            if binding.get(key) != expected:
                failures.append(f"ability={ability} binding {key}={binding.get(key)!r}, expected {expected!r}")
        action_id = binding.get("actionId")
        revision = binding.get("profileRevision")
        if isinstance(action_id, str) and isinstance(revision, int):
            action = find_action(authoring, action_id, revision)
            if action is None:
                failures.append(f"missing action {action_id}@{revision} for ability={ability}")
        else:
            failures.append(f"ability={ability} binding must name actionId and numeric profileRevision")

    if action is not None:
        if action.get("targetProfile", {}).get("targetMode") != "unit":
            failures.append(f"ability={ability} action targetMode must be unit")
        if action.get("magnitudeProfile", {}).get("magnitudeKind") != "damage":
            failures.append(f"ability={ability} action magnitudeKind must be damage")
        if action.get("magnitudeProfile", {}).get("element") != "fire":
            failures.append(f"ability={ability} action element must be fire")
        if action.get("transactionProfile", {}).get("strikeCount") != 1:
            failures.append(f"ability={ability} action strikeCount must be 1")

    template = find_template(templates, ability) if templates else None
    if template is None:
        failures.append(f"missing policy-ticket template for ability={ability}")
    else:
        family_policy = template.get("familyPolicy", {})
        if family_policy.get("family") != "directNumeric":
            failures.append(f"ability={ability} template family must be directNumeric")
        if not isinstance(family_policy.get("directNumeric"), dict):
            failures.append(f"ability={ability} template must contain directNumeric payload")
        unit_policy = template.get("unitPolicy", {})
        for key in ("sourceTileHeight", "targetTileHeight"):
            if not isinstance(unit_policy.get(key), int) or unit_policy.get(key) < 0:
                failures.append(f"ability={ability} template unitPolicy.{key} must be a nonnegative integer")

    summary: dict[str, Any] = {
        "settings": str(settings_path),
        "enabled_true": enabled_true,
        "referenced_paths": {key: str(path) for key, path in referenced.items()},
        "ability": ability,
        "binding_action": binding.get("actionId") if binding else None,
        "template_family": template.get("familyPolicy", {}).get("family") if template else None,
    }
    return summary, failures


def render(settings_path: Path, *, ability: int = 16) -> tuple[str, bool]:
    summary, failures = validate(settings_path, ability=ability)
    ok = not failures
    lines = [
        "# DCL canonical admission probe readiness",
        "",
        "Generated by `tools/analyze_dcl_canonical_admission_probe_readiness.py`.",
        "",
        f"- Settings: `{summary['settings']}`",
        f"- Ability: `{summary['ability']}`",
        f"- Enabled=true switches: `{', '.join(summary.get('enabled_true', []))}`",
        f"- Bound action: `{summary.get('binding_action')}`",
        f"- Template family: `{summary.get('template_family')}`",
        "",
        "## Referenced canonical files",
        "",
    ]
    referenced_paths = summary.get("referenced_paths", {})
    if isinstance(referenced_paths, dict):
        lines.extend(f"- `{key}`: `{value}`" for key, value in referenced_paths.items())
    if failures:
        lines.extend(["", "## Failures", "", *[f"- {failure}" for failure in failures]])
    lines.extend(["", f"Overall readiness gate: **{'PASS' if ok else 'FAIL'}**.", ""])
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("settings", nargs="?", type=Path, default=DEFAULT_SETTINGS)
    parser.add_argument("--ability", type=int, default=16)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()

    report, ok = render(args.settings, ability=args.ability)
    if not args.check_only:
        output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-canonical-admission-probe-readiness.md"
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("canonical admission probe readiness PASS" if ok else "canonical admission probe readiness FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
