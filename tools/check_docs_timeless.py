#!/usr/bin/env python3
"""Reject journal/chronology language from timeless documentation.

The repository keeps investigation history in ``work/``.  Markdown below the
paths passed to this tool must describe the current model only.  Fenced code
and inline code are ignored so literal field names, commands, and evidence
paths do not create false positives.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class Rule:
    name: str
    pattern: re.Pattern[str]
    guidance: str


RULES = (
    Rule(
        "dated-body",
        re.compile(r"\b20\d{2}-\d{2}-\d{2}\b"),
        "move the dated evidence to work/ and keep only the present-tense fact",
    ),
    Rule(
        "live-test-id",
        re.compile(r"\bLT\d+[A-Za-z0-9-]*\b", re.IGNORECASE),
        "replace the campaign/test id with the durable engine fact",
    ),
    Rule(
        "journal-heading",
        re.compile(
            r"(?:offline investigation sweep|construction status|live results|"
            r"coverage audit complete|test profiles?\s*:|known open questions|"
            r"awaiting live|same day)",
            re.IGNORECASE,
        ),
        "rewrite the passage as a current definition; planning and chronology belong in work/",
    ),
    Rule(
        "status-update-marker",
        re.compile(r"^\s*(?:[-*]\s*)?(?:status\s*:|update\s*[(:])", re.IGNORECASE),
        "replace journal status/update markers with an inline confidence tag or move them to work/",
    ),
    Rule(
        "journey-voice",
        re.compile(r"\b(?:we tried|we proved|we discovered|we found)\b", re.IGNORECASE),
        "state the fact directly in present tense",
    ),
    Rule(
        "planning-marker",
        re.compile(r"\b(?:TODO|next step)\b", re.IGNORECASE),
        "move planning to a timestamped work/ file",
    ),
)


def markdown_files(paths: list[Path]) -> list[Path]:
    files: set[Path] = set()
    for path in paths:
        if path.is_file() and path.suffix.lower() == ".md":
            files.add(path)
        elif path.is_dir():
            files.update(candidate for candidate in path.rglob("*.md") if candidate.is_file())
    return sorted(files)


def strip_inline_code(line: str) -> str:
    # Documentation uses ordinary single-backtick spans.  Replacing their contents is enough for
    # this policy gate and deliberately leaves surrounding prose/links visible to the rules.
    return re.sub(r"`[^`]*`", "``", line)


def violations(path: Path) -> list[tuple[int, str, Rule]]:
    found: list[tuple[int, str, Rule]] = []
    in_fence = False
    for number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        stripped = raw.lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        inspected = strip_inline_code(raw)
        for rule in RULES:
            if rule.pattern.search(inspected):
                found.append((number, raw.strip(), rule))
    return found


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        default=[Path("docs/modding")],
        help="Markdown file or directory roots (default: docs/modding)",
    )
    args = parser.parse_args()

    files = markdown_files(args.paths)
    if not files:
        print("ERROR: no Markdown files found")
        return 2

    count = 0
    for path in files:
        for number, line, rule in violations(path):
            count += 1
            print(f"{path}:{number}: {rule.name}: {line}")
            print(f"  guidance: {rule.guidance}")
    if count:
        print(f"docs timeless check FAIL: {count} violation(s) in {len(files)} file(s)")
        return 1
    print(f"docs timeless check PASS: {len(files)} file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
