#!/usr/bin/env python3
"""Validate bounded live evidence for the generic synthetic-Reaction transaction."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def parse_startup_dumps(lines: list[str]) -> dict[int, bytes]:
    dumps: dict[int, bytes] = {}
    pattern = re.compile(r"^\[DUMP .*? id=0x([0-9A-Fa-f]+)\]\s+(.*)$")
    for line in lines:
        match = pattern.match(line)
        if not match:
            continue
        character = int(match.group(1), 16)
        payload = bytes(int(token, 16) for token in re.findall(r"\b[0-9A-Fa-f]{2}\b", match.group(2)))
        if character not in dumps:
            dumps[character] = payload
    return dumps


def analyze_text(
    text: str,
    carrier: int,
    mode: str,
    *,
    delivery_id: int | None = None,
    require_startup_owner: bool = False,
    expected_reaction_set: bytes | None = None,
    require_source_retarget: bool = False,
    expected_action_type: int | None = None,
    expected_action_id: int | None = None,
    expected_original_action_type: int | None = None,
    expected_original_action_id: int | None = None,
    require_effect: bool = False,
) -> tuple[dict[str, int], list[str]]:
    lines = text.splitlines()
    explicit_delivery = delivery_id is not None
    delivery = carrier if delivery_id is None else delivery_id
    carrier_token = f"carrier={carrier}"
    delivery_token = f"delivery={delivery}"
    def owner_delivery_match(line: str) -> bool:
        return carrier_token in line and (not explicit_delivery or delivery_token in line)
    gates = [
        line for line in lines
        if "[DCL-SYNTHETIC-REACTION-GATE]" in line
        and owner_delivery_match(line)
    ]
    accepted = [line for line in gates if "accepted=1" in line and "mailbox=armed" in line]
    replays = [line for line in gates if "replay=1" in line]
    preselect = [
        line for line in lines
        if "[DCL-REACTION-PRESELECT]" in line
        and owner_delivery_match(line)
    ]
    would_stage = [line for line in preselect if "producer=synthetic-would-stage" in line]
    staged = [line for line in preselect if "producer=synthetic-staged" in line]
    native_commits = [
        line for line in lines
        if "[DCL-REACTION-COMMIT]" in line
        and re.search(rf"\breactionId={delivery}\b", line)
        and re.search(r"\bpass=2\b", line)
        and re.search(r"\bidsAgree=True\b", line)
    ]
    managed_commits = [
        line for line in lines
        if "[DCL-SYNTHETIC-REACTION-COMMIT]" in line
        and owner_delivery_match(line)
    ]
    consumed = [
        line for line in managed_commits
        if "cadence=consumed" in line and "ownership=materialized-delivery-owned" in line
    ]
    materialized = [
        line for line in lines
        if "[DCL-REACTION-MATERIALIZED]" in line
        and re.search(rf"\breactionId={delivery}\b", line)
    ]
    rewritten_materialized = [line for line in materialized if re.search(r"\brewrite=wrote\b", line)]
    owned_materialized = [line for line in materialized if "syntheticDelivery=owned" in line]

    def field(line: str, name: str) -> int | None:
        match = re.search(rf"\b{re.escape(name)}=(-?\d+)\b", line)
        return int(match.group(1)) if match else None

    source_retargeted = [
        line for line in materialized
        if field(line, "targetMode") == 5
        and field(line, "sourceIdx") is not None
        and field(line, "targetIdx") == field(line, "sourceIdx")
    ]
    expected_actions = [
        line for line in materialized
        if (expected_action_type is None or field(line, "actionType") == expected_action_type)
        and (expected_action_id is None or field(line, "actionId") == expected_action_id)
    ]
    expected_original_actions = [
        line for line in materialized
        if (
            expected_original_action_type is None
            or field(line, "originalActionType") == expected_original_action_type
        )
        and (
            expected_original_action_id is None
            or field(line, "originalActionId") == expected_original_action_id
        )
    ]
    effects = [
        line for line in lines
        if "[DCL-REACTION-EFFECT]" in line
        and re.search(rf"\breactionId={delivery}\b", line)
    ]
    delivery_effects: list[str] = []
    for line in effects:
        source_idx = field(line, "sourceIdx")
        targets_match = re.search(r"\btargets=\[([^]]*)\]", line)
        targets = [] if targets_match is None else [
            int(token) for token in re.findall(r"-?\d+", targets_match.group(1))
        ]
        if (
            source_idx is not None
            and source_idx in targets
            and (expected_action_id is None or field(line, "actionId") == expected_action_id)
        ):
            delivery_effects.append(line)
    failures = [
        line for line in lines
        if ("[DCL-REACTION-" in line or "[DCL-SYNTHETIC-REACTION-" in line)
        and ("-FAILED]" in line or "-SKIP]" in line or "-LOST" in line)
    ]
    hook_mode = [
        line for line in lines
        if "[DCL-REACTION-PRESELECT-HOOK]" in line
        and (
            f"synthetic={mode}:carrier={carrier}:delivery={delivery}" in line
            if explicit_delivery else f"synthetic={mode}:carrier={carrier}" in line
        )
    ]
    materialization_hooks = [
        line for line in lines
        if "[DCL-REACTION-MATERIALIZED-HOOK]" in line
        and re.search(r"\brva=0x2831BD\b", line)
        and re.search(r"\bstage=special-pre-target-build\b", line)
        and (
            f"synthetic={mode}:carrier={carrier}:delivery={delivery}" in line
            if explicit_delivery else True
        )
    ]
    startup_dumps = parse_startup_dumps(lines)
    accepted_defenders = {
        int(match.group(1), 16)
        for line in accepted
        if (match := re.search(r"\bdefender=0x([0-9A-Fa-f]+)\b", line))
    }
    startup_owner_valid = 0

    counts = {
        "hooks": len(hook_mode),
        "materialization_hooks": len(materialization_hooks),
        "gates": len(gates),
        "accepted": len(accepted),
        "replays": len(replays),
        "would_stage": len(would_stage),
        "staged": len(staged),
        "materialized": len(materialized),
        "owned_materialized": len(owned_materialized),
        "rewritten_materialized": len(rewritten_materialized),
        "source_retargeted": len(source_retargeted),
        "expected_actions": len(expected_actions),
        "expected_original_actions": len(expected_original_actions),
        "native_commits": len(native_commits),
        "managed_commits": len(managed_commits),
        "consumed": len(consumed),
        "effects": len(effects),
        "delivery_effects": len(delivery_effects),
        "failures": len(failures),
        "startup_owner_dumps": sum(character in startup_dumps for character in accepted_defenders),
        "startup_owner_valid": 0,
    }
    errors: list[str] = []
    if not hook_mode:
        errors.append(f"missing synthetic={mode} pre-selector hook for owner {carrier} and delivery {delivery}")
    if mode == "live" and not materialization_hooks:
        errors.append("missing special-delivery materialization hook at 0x2831BD")
    if not accepted:
        errors.append("missing accepted exact-owner gate with an armed mailbox")
    if replays:
        errors.append(f"synthetic trigger replays observed: {len(replays)}")
    if failures:
        errors.append(f"reaction hook failures/skips/losses observed: {len(failures)}")
    if expected_reaction_set is not None and len(expected_reaction_set) != 4:
        errors.append("expected reaction-set value must contain exactly four bytes")

    if require_startup_owner or expected_reaction_set is not None:
        if not accepted_defenders:
            errors.append("accepted gate does not expose a defender character id")
        if not 422 <= carrier <= 453:
            errors.append(f"carrier {carrier} is outside the 32-bit Reaction-set range")
        else:
            relative = carrier - 422
            bit_index = relative // 8
            bit_mask = 1 << (7 - relative % 8)
            for defender in sorted(accepted_defenders):
                dump = startup_dumps.get(defender)
                if dump is None:
                    errors.append(f"missing startup dump for accepted defender 0x{defender:02X}")
                    continue
                if len(dump) < 0x1D0:
                    errors.append(
                        f"startup dump for defender 0x{defender:02X} is too short: {len(dump)} bytes"
                    )
                    continue
                equipped = int.from_bytes(dump[0x14:0x16], "little")
                reaction_set = dump[0x94:0x98]
                candidate = int.from_bytes(dump[0x1CE:0x1D0], "little", signed=True)
                owner_errors: list[str] = []
                if equipped != carrier:
                    owner_errors.append(f"equipped={equipped}, expected {carrier}")
                if reaction_set[bit_index] & bit_mask == 0:
                    owner_errors.append(
                        f"reaction-set {reaction_set.hex().upper()} lacks carrier mask 0x{bit_mask:02X}"
                    )
                if expected_reaction_set is not None and reaction_set != expected_reaction_set:
                    owner_errors.append(
                        f"reaction-set={reaction_set.hex().upper()}, expected "
                        f"{expected_reaction_set.hex().upper()}"
                    )
                if candidate != 0:
                    owner_errors.append(f"startup candidate={candidate}, expected 0")
                if owner_errors:
                    errors.append(
                        f"invalid startup owner 0x{defender:02X}: " + "; ".join(owner_errors)
                    )
                else:
                    startup_owner_valid += 1
            counts["startup_owner_valid"] = startup_owner_valid

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
            errors.append("missing special-path materialization for the configured delivery carrier")
        if not owned_materialized:
            errors.append("missing producer-owned materialized delivery handshake")
        if require_source_retarget and not source_retargeted:
            errors.append("missing targetMode=5 retarget to the exact source index")
        if (expected_action_type is not None or expected_action_id is not None) and not expected_actions:
            errors.append(
                "missing expected materialized action "
                f"type={expected_action_type if expected_action_type is not None else 'any'} "
                f"id={expected_action_id if expected_action_id is not None else 'any'}"
            )
        if (
            expected_original_action_type is not None or expected_original_action_id is not None
        ) and not expected_original_actions:
            errors.append(
                "missing expected original materialized action "
                f"type={expected_original_action_type if expected_original_action_type is not None else 'any'} "
                f"id={expected_original_action_id if expected_original_action_id is not None else 'any'}"
            )
        if not native_commits:
            errors.append("missing exact pass-2 native commit for the configured delivery carrier")
        if not consumed:
            errors.append("missing exact producer-owned cadence commit")
        if require_effect and not delivery_effects:
            errors.append("missing delivery-carrier effect delivered to the exact source index")

    return counts, errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--carrier", type=int, required=True)
    parser.add_argument(
        "--delivery-id",
        type=int,
        help="Native delivery Reaction id (defaults to --carrier for legacy captures).",
    )
    parser.add_argument("--mode", choices=("log-only", "live"), required=True)
    parser.add_argument(
        "--require-startup-owner",
        action="store_true",
        help="Require the first accepted defender dump to equip the carrier with an empty candidate slot.",
    )
    parser.add_argument(
        "--require-source-retarget",
        action="store_true",
        help="Require targetMode=5 with targetIdx equal to sourceIdx, whether native or rewritten.",
    )
    parser.add_argument("--expected-action-type", type=int)
    parser.add_argument("--expected-action-id", type=int)
    parser.add_argument("--expected-original-action-type", type=int)
    parser.add_argument("--expected-original-action-id", type=int)
    parser.add_argument(
        "--require-effect",
        action="store_true",
        help="Require the carrier effect to target the exact source index.",
    )
    parser.add_argument(
        "--expected-reaction-set-hex",
        help="Optional exact four-byte unit+0x94..0x97 value, such as 00000400.",
    )
    args = parser.parse_args()
    expected_reaction_set = None
    if args.expected_reaction_set_hex:
        try:
            expected_reaction_set = bytes.fromhex(args.expected_reaction_set_hex)
        except ValueError as error:
            parser.error(f"invalid --expected-reaction-set-hex: {error}")
        if len(expected_reaction_set) != 4:
            parser.error("--expected-reaction-set-hex must contain exactly four bytes")
    counts, errors = analyze_text(
        args.log.read_text(encoding="utf-8", errors="replace"),
        args.carrier,
        args.mode,
        delivery_id=args.delivery_id,
        require_startup_owner=args.require_startup_owner,
        expected_reaction_set=expected_reaction_set,
        require_source_retarget=args.require_source_retarget,
        expected_action_type=args.expected_action_type,
        expected_action_id=args.expected_action_id,
        expected_original_action_type=args.expected_original_action_type,
        expected_original_action_id=args.expected_original_action_id,
        require_effect=args.require_effect,
    )
    print(" ".join(f"{key}={value}" for key, value in counts.items()))
    for error in errors:
        print(f"ERROR: {error}")
    print("synthetic-Reaction live evidence PASS" if not errors else "synthetic-Reaction live evidence FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
