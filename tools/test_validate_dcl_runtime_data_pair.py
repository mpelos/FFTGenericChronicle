#!/usr/bin/env python3
"""Smoke tests for the fail-closed runtime/action-data pairing gate."""
from __future__ import annotations

import hashlib
import json
import sqlite3
import tempfile
from contextlib import closing
from pathlib import Path

from validate_dcl_runtime_data_pair import PairValidationError, ROOT, validate_pair


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main() -> int:
    with tempfile.TemporaryDirectory(dir=ROOT) as raw:
        temp = Path(raw)
        settings = temp / "settings.json"
        database = temp / "action.sqlite"
        nxd = temp / "action.nxd"
        manifest = temp / "pair.json"

        settings.write_text(json.dumps({
            "DclComputePointNumericEnabled": True,
            "DclInstantKoControlEnabled": True,
            "DclHitForcedRoll": -1,
            "DclInstantKoRules": [{
                "AbilityId": 30,
                "NativeKoSuppressedByData": True,
            }],
        }), encoding="utf-8")
        with closing(sqlite3.connect(database)) as con:
            con.execute(
                "CREATE TABLE OverrideAbilityActionData "
                "(Key INTEGER PRIMARY KEY, Formula INTEGER, X INTEGER, Y INTEGER, InflictStatus INTEGER)"
            )
            con.execute("INSERT INTO OverrideAbilityActionData VALUES (30, 8, 1, 1, 0)")
            con.commit()
        nxd.write_bytes(b"round-trip-audited-fixture")

        def write_manifest() -> None:
            manifest.write_text(json.dumps({
                "settings": settings.relative_to(ROOT).as_posix(),
                "action_data_sqlite": database.relative_to(ROOT).as_posix(),
                "action_data_nxd": nxd.relative_to(ROOT).as_posix(),
                "sha256": {
                    "action_data_sqlite": digest(database),
                    "action_data_nxd": digest(nxd),
                },
                "required_instant_ko_abilities": [30],
            }), encoding="utf-8")

        write_manifest()
        assert any(detail == "instant_ko_abilities=30" for detail in validate_pair(manifest))

        with closing(sqlite3.connect(database)) as con:
            con.execute("UPDATE OverrideAbilityActionData SET InflictStatus=-1 WHERE Key=30")
            con.commit()
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "not safely neutralized" in str(exc)
        else:
            raise AssertionError("native Death was accepted beside a managed Death rule")

        settings_value = json.loads(settings.read_text(encoding="utf-8"))
        settings_value["DclStatusForcedRoll"] = 18
        settings.write_text(json.dumps(settings_value), encoding="utf-8")
        with closing(sqlite3.connect(database)) as con:
            con.execute("UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key=30")
            con.commit()
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "probe-only DclStatusForcedRoll" in str(exc)
        else:
            raise AssertionError("forced probe roll was accepted in an integrated profile")

    print("DCL runtime/data pair smoke tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
