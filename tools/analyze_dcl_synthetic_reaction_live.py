#!/usr/bin/env python3
"""Validate bounded live evidence for the generic synthetic-Reaction transaction."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def analyze_text(text: str, carrier: int, mode: str) -> tuple[dict[str, int], list[str]]:
    lines = text.splitlines()
    carrier_token = f"carrier={carrier}"
    gates = [line for line in lines if "[DCL-SYNTHETIC-REACTION-GATE]" in line and carrier_token in line]
    accepted = [line for line in gates if "accepted=1" in line and "mailbox=armed" in line]
    replays = [line for line in gates if "replay=1" in line]
    preselect = [line for line in lines if "[DCL-REACTION-PRESELECT]" in line and carrier_token in line]
    would_stage = [line for line in preselect if "producer=synthetic-would-stage" in line]
    staged = [line for line in preselect if "producer=synthetic-staged" in line]
    native_commits = [
        line for line in lines
        if "[DCL-REACTION-COMMIT]" in line
        and re.search(rf"\breactionId={carrier}\b", line)
    ]
    managed_commits = [
        line for line in lines
        if "[DCL-SYNTHETIC-REACTION-COMMIT]" in line and carrier_token in line
    ]
    consumed = [
        line for line in managed_commits
        if "cadence=consumed" in line and "delivery=accepted-order-owned" in line
    ]
    materialized = [
        line for line in lines
        if "[DCL-REACTION-MATERIALIZED]" in line
        and re.search(rf"\breactionId={carrier}\b", line)
    ]
    failures = [
        line for line in lines
        if ("[DCL-REACTION-" in line or "[DCL-SYNTHETIC-REACTION-" in line)
        and ("-FAILED]" in line or "-SKIP]" in line or "-LOST" in line)
    ]
    hook_mode = [
        line for line in lines
        if "[DCL-REACTION-PRESELECT-HOOK]" in line
        and f"synthetic={mode}:carrier={carrier}" in line
    ]

    counts = {
        "hooks": len(hook_mode),
        "gates": len(gates),
        "accepted": len(accepted),
        "replays": len(replays),
        "would_stage": len(would_stage),
        "staged": len(staged),
        "materialized": len(materialized),
        "native_commits": len(native_commits),
        "managed_commits": len(managed_commits),
        "consumed": len(consumed),
        "failures": len(failures),
    }
    errors: list[str] = []
    if not hook_mode:
        errors.append(f"missing synthetic={mode} pre-selector hook for carrier {carrier}")
    if not accepted:
        errors.append("missing accepted exact-owner gate with an armed mailbox")
    if failures:
        errors.append(f"reaction hook failures/skips/losses observed: {len(failures)}")

    if mode == "log-only":
        if not would_stage:
            errors.append("missing synthetic-would-stage evidence")
        if staged:
            errors.append("log-only capture contains a live staged write")
        if materialized or native_commits or managed_commits:
            errors.append("log-only capture unexpectedly materialized or committed the synthetic carrier")
    else:
        if not staged:
            errors.append("missing bounded synthetic-staged evidence")
        if not materialized:
            errors.append("missing accepted-order materialization for the configured carrier")
        if not native_commits:
            errors.append("missing exact pass-2 native commit for the configured carrier")
        if not consumed:
            errors.append("missing exact producer-owned cadence commit")

    return counts, errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--carrier", type=int, required=True)
    parser.add_argument("--mode", choices=("log-only", "live"), required=True)
    args = parser.parse_args()
    counts, errors = analyze_text(args.log.read_text(encoding="utf-8", errors="replace"), args.carrier, args.mode)
    print(" ".join(f"{key}={value}" for key, value in counts.items()))
    for error in errors:
        print(f"ERROR: {error}")
    print("synthetic-Reaction live evidence PASS" if not errors else "synthetic-Reaction live evidence FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
