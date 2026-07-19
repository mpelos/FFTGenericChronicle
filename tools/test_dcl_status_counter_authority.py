#!/usr/bin/env python3
"""Regression tests for native/DCL status-duration authority analysis."""
from __future__ import annotations

import analyze_dcl_status_counter_authority as audit


def main() -> int:
    rows = audit.load_rows()
    assert len(rows) == 40
    assert len(audit.encode_table(rows)) == 40 * 16
    assert rows[0].name == "None" and rows[0].enum_value == 1
    assert rows[24].name == "Poison" and rows[24].counter == 36 and "Regen" in rows[24].cancel_flags
    assert rows[28].name == "Haste" and rows[28].counter == 32 and "Slow" in rows[28].cancel_flags
    assert rows[29].name == "Slow" and "Haste" in rows[29].cancel_flags
    assert rows[36].name == "Immobilize" and rows[36].counter == 24
    assert rows[37].name == "Disable" and rows[37].counter == 24
    assert rows[38].name == "Reflect" and rows[38].counter == 32
    assert rows[39].name == "Doom" and rows[39].counter == 3
    assert audit.DCL_TO_NATIVE["DontMove"] == "Immobilize"
    assert audit.DCL_TO_NATIVE["DontAct"] == "Disable"
    assert audit.DCL_TO_NATIVE["DeathSentence"] == "Doom"
    timed, missing = audit.duration_authority(rows)
    assert not missing
    assert {row.name for row in timed} >= {"Poison", "Immobilize", "Disable", "Doom"}
    print("DCL status-counter authority tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

