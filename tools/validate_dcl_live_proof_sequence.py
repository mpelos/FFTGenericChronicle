#!/usr/bin/env python3
"""Validate the ordered, job-free DCL live proof sequence."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SEQUENCE = ROOT / "work" / "1784674066-dcl-live-proof-sequence.json"
ALLOWED_STATUSES = {
    "planned",
    "ready-for-live",
    "blocked-by-prior-proof",
    "proven-live",
    "refuted",
}
ALLOWED_STAGES = {"offline-preflight", "live-observe", "live-regression"}
REQUIRED_FIRST_PROOF = "canonical-admission-template-bridge"
FORBIDDEN_TERMS = {"fear", "approach", "job draft", "job-specific"}
FORBIDDEN_ACTIVE_ARTIFACTS = {
    "work/1784395365-dcl-da-dm-status-duration-pair.json",
    "work/1784397292-dcl-unified-sentinel-v2-runtime-data-pair.json",
    "work/1784397292-dcl-unified-sentinel-v2-status-duration-pair.json",
    "work/1784470893-dcl-unified-clean-v1-runtime-data-pair.json",
    "work/1784470893-dcl-unified-clean-v1-status-duration-pair.json",
}


class SequenceError(ValueError):
    pass


def load_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise SequenceError(f"cannot read {label} {path}: {error}") from error
    if not isinstance(value, dict):
        raise SequenceError(f"{label} must be a JSON object")
    return value


def ensure_nonempty_string(value: Any, label: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise SequenceError(f"{label} must be a non-empty string")
    return value


def ensure_string_list(value: Any, label: str, *, nonempty: bool = True) -> list[str]:
    if not isinstance(value, list) or (nonempty and not value):
        raise SequenceError(f"{label} must be a {'non-empty ' if nonempty else ''}string list")
    if not all(isinstance(item, str) and item.strip() for item in value):
        raise SequenceError(f"{label} must contain only non-empty strings")
    return value


def validate_sequence(path: Path = DEFAULT_SEQUENCE) -> list[str]:
    sequence = load_object(path, "DCL live proof sequence")
    if sequence.get("schemaRevision") != 1:
        raise SequenceError("schemaRevision must be 1")
    note = ensure_nonempty_string(sequence.get("note"), "note").lower()
    for token in ("job-free", "live proof"):
        if token not in note:
            raise SequenceError(f"note must preserve {token!r} boundary")
    if sequence.get("job_free") is not True or sequence.get("ignores_jobs") is not True:
        raise SequenceError("sequence must declare job_free=true and ignores_jobs=true")
    if sequence.get("live_tests_only_after_offline_exhausted") is not True:
        raise SequenceError("sequence must require offline exhaustion before live tests")

    retired = ensure_string_list(sequence.get("retired_controls_forbidden"), "retired_controls_forbidden")
    for required in (
        "DclApproachEnabled",
        "DclFearControlEnabled",
        "DclFearForcedFleeControlEnabled",
        "DclFearPlayerConfirmEnforcementEnabled",
    ):
        if required not in retired:
            raise SequenceError(f"retired_controls_forbidden must include {required}")

    proofs = sequence.get("proofs")
    if not isinstance(proofs, list) or not proofs:
        raise SequenceError("proofs must be a non-empty list")

    seen: set[str] = set()
    details: list[str] = []
    for index, proof in enumerate(proofs):
        if not isinstance(proof, dict):
            raise SequenceError(f"proof {index + 1} must be an object")
        proof_id = ensure_nonempty_string(proof.get("id"), f"proof {index + 1}.id")
        if index == 0 and proof_id != REQUIRED_FIRST_PROOF:
            raise SequenceError(f"first proof must be {REQUIRED_FIRST_PROOF}")
        if proof_id in seen:
            raise SequenceError(f"duplicate proof id: {proof_id}")
        if proof.get("job_free") is not True:
            raise SequenceError(f"proof {proof_id} must declare job_free=true")
        if proof.get("writes_save") is not False:
            raise SequenceError(f"proof {proof_id} must declare writes_save=false")
        status = proof.get("status")
        if status not in ALLOWED_STATUSES:
            raise SequenceError(f"proof {proof_id} has invalid status {status!r}")
        stage = proof.get("stage")
        if stage not in ALLOWED_STAGES:
            raise SequenceError(f"proof {proof_id} has invalid stage {stage!r}")
        purpose = ensure_nonempty_string(proof.get("purpose"), f"proof {proof_id}.purpose")
        fixture = ensure_nonempty_string(proof.get("fixture"), f"proof {proof_id}.fixture")
        lower_scope = f"{proof_id} {purpose} {fixture}".lower()
        if any(term in lower_scope for term in FORBIDDEN_TERMS):
            raise SequenceError(f"proof {proof_id} text contains forbidden draft/retired scope")
        dependencies = ensure_string_list(proof.get("depends_on", []), f"proof {proof_id}.depends_on", nonempty=False)
        missing = set(dependencies) - seen
        if missing:
            raise SequenceError(f"proof {proof_id} depends on missing or later proofs: {sorted(missing)}")
        preflight_commands = ensure_string_list(proof.get("preflight_commands"), f"proof {proof_id}.preflight_commands")
        expected_artifacts = ensure_string_list(proof.get("expected_artifacts"), f"proof {proof_id}.expected_artifacts")
        pass_evidence = ensure_string_list(proof.get("pass_evidence"), f"proof {proof_id}.pass_evidence")
        blocks = ensure_string_list(proof.get("blocks", []), f"proof {proof_id}.blocks", nonempty=False)
        active_text = "\n".join(
            [proof_id, purpose, fixture]
            + preflight_commands
            + expected_artifacts
            + pass_evidence
            + blocks
            + [
                str(proof.get("prepare_command", "")),
                str(proof.get("collect_command", "")),
                str(proof.get("analysis_command", "")),
            ]
        )
        for artifact in FORBIDDEN_ACTIVE_ARTIFACTS:
            if artifact in active_text:
                raise SequenceError(
                    f"proof {proof_id} references historical inactive artifact {artifact}"
                )
        if stage.startswith("live") and status in {"ready-for-live", "blocked-by-prior-proof"}:
            ensure_nonempty_string(proof.get("analysis_command"), f"proof {proof_id}.analysis_command")
        if proof_id == REQUIRED_FIRST_PROOF:
            commands = "\n".join(
                ensure_string_list(proof.get("preflight_commands"), f"proof {proof_id}.preflight_commands")
                + [ensure_nonempty_string(proof.get("prepare_command"), f"proof {proof_id}.prepare_command")]
                + [ensure_nonempty_string(proof.get("collect_command"), f"proof {proof_id}.collect_command")]
                + [ensure_nonempty_string(proof.get("analysis_command"), f"proof {proof_id}.analysis_command")]
            )
            for term in (
                "analyze_dcl_canonical_admission_probe_readiness.py",
                "prepare-canonical-admission-live.ps1",
                "collect_dcl_canonical_admission_live_log.py",
                "analyze_dcl_canonical_admission_template_live.py",
            ):
                if term not in commands:
                    raise SequenceError(f"canonical admission proof must reference {term}")
        seen.add(proof_id)
        details.append(f"proof={proof_id} status={status} stage={stage}")

    return details


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sequence", nargs="?", type=Path, default=DEFAULT_SEQUENCE)
    args = parser.parse_args()
    try:
        details = validate_sequence(args.sequence.resolve())
    except SequenceError as error:
        print(f"ERROR: {error}")
        return 1
    print("DCL live proof sequence validation PASS")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
