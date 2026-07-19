#!/usr/bin/env python3
"""Offline fail-closed tests for Reaction materialization byte anchors."""
from __future__ import annotations

from dataclasses import dataclass

import analyze_dcl_reaction_materialization as analyzer


@dataclass
class FakePe:
    offsets: dict[int, int]

    def get_offset_from_rva(self, rva: int) -> int:
        return self.offsets[rva]


def main() -> int:
    offsets: dict[int, int] = {}
    raw = bytearray()
    for anchor in analyzer.ANCHORS:
        offsets[anchor.rva] = len(raw)
        raw.extend(bytes.fromhex(anchor.expected))
        raw.extend(b"\xCC")

    pe = FakePe(offsets)
    results = analyzer.validate_anchors(pe, bytes(raw))  # type: ignore[arg-type]
    assert results and all(passed for _, _, passed in results)

    accepted = next(anchor for anchor in analyzer.ANCHORS if anchor.name == "special-pre-target-build-boundary")
    broken = bytearray(raw)
    broken[offsets[accepted.rva]] ^= 0x01
    results = analyzer.validate_anchors(pe, bytes(broken))  # type: ignore[arg-type]
    failures = [anchor.name for anchor, _, passed in results if not passed]
    assert failures == ["special-pre-target-build-boundary"], failures

    print("reaction materialization analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
