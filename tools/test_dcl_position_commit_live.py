#!/usr/bin/env python3
"""Offline tests for the DCL position-commit live-log analyzer."""
from __future__ import annotations

import tempfile
from pathlib import Path

import analyze_dcl_position_commit_live as live


def _event(sequence: int, index: int, x: int, y: int, caller: int) -> str:
    return (
        f"[LANDMARK-HIT event={sequence} id=1 name=dcl_position_commit rva=0xE7D735A "
        f"access=observe base=unit-index:rdi={index}->0x141853CE0:unit:base now=1 "
        "baseRead=unit:id=0x01/team=1/hp=100/ct=100 fields=x raw=none] "
        f"regs=rax=0x0:zero rdi=0x{index:X}:unreadable r8=0x3:unreadable "
        f"r9=0x{y:X}:unreadable r10=0x{x:X}:unreadable r14=0x0:zero "
        f"capturedStack=+0x58=0x{0x140000000 + caller + 5:X}:module+0x{caller + 5:X}"
    )


def main() -> int:
    hook = (
        "[LANDMARK-HOOK dcl_position_commit] id=1 rva=0xE7D735A addr=0x14E7D735A "
        "base=r11 battleUnitIndex=rdi captureStack=+0x58 access=observe "
        "expected=46 88 94 1B 2F 3D 85 01"
    )
    text = "\n".join(
        [
            hook,
            _event(1, 5, 3, 4, 0xE46AD63),
            _event(2, 5, 3, 4, 0xE46AD63),
            _event(3, 5, 5, 4, 0x1F24AB),
        ]
    )
    hooks, events, errors = live.parse_log(text)
    assert hooks == [(live.EXPECTED_HOOK_RVA, live.EXPECTED_CAPTURE_OFFSET)]
    assert len(events) == 3 and not errors
    assert events[-1].destination == (5, 4, 3)
    assert events[-1].caller_rva == 0x1F24AB

    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        log = root / "probe.log"
        output = root / "report.md"
        log.write_text(text, encoding="utf-8")
        report, ok = live.analyze(log, output, require_coordinate_changes=1)
        assert ok
        assert "Coordinate changes after each unit's first observation: **1**" in report

        bad = text + "\n" + _event(4, 5, 6, 4, 0x123456)
        log.write_text(bad, encoding="utf-8")
        _, bad_ok = live.analyze(log, output)
        assert not bad_ok

    print("position commit live analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
