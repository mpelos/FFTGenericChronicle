#!/usr/bin/env python3
"""Validate the canonical admission -> policy-ticket template bridge live proof log."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
HOOK_TAG = "[DCL-CANONICAL-ADMISSION-HOOK]"
EVENT_TAG = "[DCL-CANONICAL-ADMISSION]"
ERROR_TAG = "[DCL-CANONICAL-ADMISSION-ERR]"
RESET_TAG = "[DCL-STATE-RESET]"
KV_RE = re.compile(r"([A-Za-z][A-Za-z0-9]*)=([^,\s]+)")
DAMAGE_RE = re.compile(
    r"\[DAMAGE [^\]]*id=(?P<char>0x[0-9A-Fa-f]+)\] "
    r"(?P<before>\d+) -> (?P<after>\d+) = (?P<delta>\d+)"
)


@dataclass(frozen=True)
class AdmissionEvent:
    line: int
    raw: str
    fields: dict[str, str]

    def get_int(self, name: str) -> int | None:
        value = self.fields.get(name)
        if value is None:
            return None
        try:
            return int(value, 0)
        except ValueError:
            return None


def parse_fields(line: str) -> dict[str, str]:
    return {match.group(1): match.group(2) for match in KV_RE.finditer(line)}


def parse_events(text: str) -> tuple[list[str], list[str], list[str], list[AdmissionEvent]]:
    hooks: list[str] = []
    resets: list[str] = []
    errors: list[str] = []
    events: list[AdmissionEvent] = []
    for line_number, line in enumerate(text.splitlines(), 1):
        if HOOK_TAG in line:
            hooks.append(line)
        if RESET_TAG in line and "canonicalBattle=1" in line:
            resets.append(line)
        if ERROR_TAG in line:
            errors.append(line)
        if EVENT_TAG in line:
            events.append(AdmissionEvent(line_number, line, parse_fields(line)))
    return hooks, resets, errors, events


def event_matches(event: AdmissionEvent, ability: int, target_count: int, strikes: int) -> bool:
    return (
        event.get_int("ability") == ability
        and event.get_int("targetCount") == target_count
        and event.get_int("strikes") == strikes
        and event.get_int("complete") == 1
    )


def parse_single_target_char_id(event: AdmissionEvent) -> int | None:
    targets = event.fields.get("targets")
    if targets is None:
        return None
    ids = [int(value, 16) for value in re.findall(r"0x([0-9A-Fa-f]+)", targets)]
    return ids[0] if len(ids) == 1 else None


def has_later_positive_damage(text: str, after_line: int, target_char_id: int) -> bool:
    for line_number, line in enumerate(text.splitlines(), 1):
        if line_number <= after_line:
            continue
        match = DAMAGE_RE.search(line)
        if match is None:
            continue
        if int(match["char"], 16) == target_char_id and int(match["delta"]) > 0:
            return True
    return False


def analyze_text(
    text: str,
    *,
    ability: int,
    target_count: int,
    strikes: int,
    require_damage: bool,
) -> tuple[dict[str, object], list[str]]:
    hooks, resets, errors, events = parse_events(text)
    matching = [event for event in events if event_matches(event, ability, target_count, strikes)]
    failures: list[str] = []

    if not any("rva=0x281EFA" in line for line in hooks):
        failures.append("missing active canonical admission hook at rva=0x281EFA")
    if not resets:
        failures.append("missing DCL state reset with canonicalBattle=1")
    if errors:
        failures.append(f"found {len(errors)} canonical admission error line(s)")
    if len(matching) != 1:
        failures.append(
            f"expected exactly one complete admission for ability={ability}, targetCount={target_count}, "
            f"strikes={strikes}; found {len(matching)}"
        )

    chosen = matching[0] if len(matching) == 1 else None
    if chosen is not None:
        expected_statuses = {
            "admissionStatus": "Published",
            "templateStatus": "Built",
            "ticketStatus": "Published",
            "bridgeStatus": "Published",
        }
        for field, expected in expected_statuses.items():
            actual = chosen.fields.get(field)
            if actual != expected:
                failures.append(f"line {chosen.line}: {field}={actual!r}, expected {expected!r}")
        if chosen.get_int("action") is None:
            failures.append(f"line {chosen.line}: missing numeric ActionInstance field `action`")
        target_char_id = parse_single_target_char_id(chosen)
        if require_damage:
            if target_char_id is None:
                failures.append(f"line {chosen.line}: cannot derive one target CharId from `targets` field")
            elif not has_later_positive_damage(text, chosen.line, target_char_id):
                failures.append(
                    f"line {chosen.line}: missing later positive [DAMAGE] event for target id=0x{target_char_id:02X}"
                )
    else:
        target_char_id = None

    summary: dict[str, object] = {
        "hook_count": len(hooks),
        "canonical_reset_count": len(resets),
        "error_count": len(errors),
        "admission_event_count": len(events),
        "matching_event_count": len(matching),
        "matching_action": chosen.get_int("action") if chosen is not None else None,
        "matching_line": chosen.line if chosen is not None else None,
        "matching_target_char": f"0x{target_char_id:02X}" if target_char_id is not None else None,
    }
    return summary, failures


def render_report(
    log: Path,
    *,
    ability: int,
    target_count: int,
    strikes: int,
    require_damage: bool,
) -> tuple[str, bool]:
    raw = log.read_bytes()
    text = raw.decode("utf-8", errors="replace")
    summary, failures = analyze_text(
        text,
        ability=ability,
        target_count=target_count,
        strikes=strikes,
        require_damage=require_damage,
    )
    ok = not failures
    lines = [
        "# DCL canonical admission template live analysis",
        "",
        "Generated by `tools/analyze_dcl_canonical_admission_template_live.py`.",
        "",
        f"- Log: `{log}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Expected ability: `{ability}`",
        f"- Expected target count: `{target_count}`",
        f"- Expected strikes: `{strikes}`",
        f"- Hook lines: `{summary['hook_count']}`",
        f"- Canonical battle reset lines: `{summary['canonical_reset_count']}`",
        f"- Admission event lines: `{summary['admission_event_count']}`",
        f"- Matching completed admission lines: `{summary['matching_event_count']}`",
        f"- Matching ActionInstance: `{summary['matching_action']}`",
        f"- Matching target CharId: `{summary['matching_target_char']}`",
        f"- Native damage required: `{1 if require_damage else 0}`",
        "",
        "## Contract",
        "",
        "- The guarded admission hook is installed at RVA `0x281EFA`.",
        "- The battle runtime reset reports `canonicalBattle=1`.",
        "- Exactly one completed admission matches the configured ability, target count, and Strike count.",
        "- That same completed admission reports `Published`, `Built`, `Published`, and `Published` for",
        "  admission, template, ticket, and bridge status respectively.",
        "- No canonical admission error line is present.",
        "- When `--require-damage` is passed, a later positive `[DAMAGE]` event must target the same",
        "  single CharId retained by the completed admission.",
        "",
    ]
    if failures:
        lines.extend(["## Failures", "", *[f"- {failure}" for failure in failures], ""])
    lines.append(f"Overall live-evidence gate: **{'PASS' if ok else 'FAIL'}**.")
    lines.append("")
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--ability", type=int, default=16)
    parser.add_argument("--target-count", type=int, default=1)
    parser.add_argument("--strikes", type=int, default=1)
    parser.add_argument(
        "--require-damage",
        action="store_true",
        help="Require a later positive [DAMAGE] line for the admitted target CharId.",
    )
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()

    report, ok = render_report(
        args.log,
        ability=args.ability,
        target_count=args.target_count,
        strikes=args.strikes,
        require_damage=args.require_damage,
    )
    if not args.check_only:
        output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-canonical-admission-template-live-analysis.md"
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("canonical admission template live evidence PASS" if ok else "canonical admission template live evidence FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
