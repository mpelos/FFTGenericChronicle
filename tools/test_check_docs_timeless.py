#!/usr/bin/env python3
"""Self-tests for check_docs_timeless.py."""
from __future__ import annotations

import tempfile
from pathlib import Path

import check_docs_timeless as checker


def scan(text: str) -> list[tuple[int, str, checker.Rule]]:
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "doc.md"
        path.write_text(text, encoding="utf-8")
        return checker.violations(path)


def main() -> int:
    assert not scan("# Definition\n\nThe field at `+0x44` owns the route state.\n")
    assert not scan("```text\nStatus: literal fixture data\n2026-07-02\n```\n")
    assert not scan("Evidence remains in `work/lt9-result-2026-07-04.md`.\n")

    dated = scan("This was proven on 2026-07-02.\n")
    assert [rule.name for _, _, rule in dated] == ["dated-body"]
    campaign = scan("LT9 live results: the probe passed.\n")
    assert {rule.name for _, _, rule in campaign} == {"live-test-id", "journal-heading"}
    status = scan("Status: waiting for another probe.\n")
    assert [rule.name for _, _, rule in status] == ["status-update-marker"]
    planning = scan("Next step is another live test.\n")
    assert [rule.name for _, _, rule in planning] == ["planning-marker"]

    print("check_docs_timeless self-tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
