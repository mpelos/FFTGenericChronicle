#!/usr/bin/env python3
"""Smoke tests for the data-layer neuter placeholder artifacts."""
from __future__ import annotations

import sqlite3
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

import build_neuter_data as neuter
import report_neuter_coverage as coverage


EXPECTED_WEAPON_COUNT = 126
EXPECTED_ABILITY_COUNT = 168
EXPECTED_SKIPPED_COUNT = 32
EXPECTED_CHARGE_AIM_COUNT = 7
DAMAGING_SAMPLES = {
    16: "Fire",
    20: "Thunder",
    24: "Blizzard",
}
NON_DAMAGING_SAMPLES = {
    1: "Cure",
    2: "Cura",
}


def main() -> int:
    check(neuter.TEMPLATE.exists(), f"weapon template missing: {neuter.TEMPLATE}")
    check(neuter.ABILITY_TEMPLATE.exists(), f"ability template missing: {neuter.ABILITY_TEMPLATE}")
    check(neuter.BASE_OVERRIDE_SQLITE.exists(), f"base override sqlite missing: {neuter.BASE_OVERRIDE_SQLITE}")
    check(neuter.NEUTER_OVERRIDE_SQLITE.exists(), f"neuter override sqlite missing: {neuter.NEUTER_OVERRIDE_SQLITE}")
    check(neuter.OUT.exists(), f"weapon neuter XML missing: {neuter.OUT}")
    check(neuter.CHARGE_AIM_TEMPLATE.exists(), f"charge/aim template missing: {neuter.CHARGE_AIM_TEMPLATE}")
    check(neuter.CHARGE_AIM_OUT.exists(), f"charge/aim neuter XML missing: {neuter.CHARGE_AIM_OUT}")
    check(neuter.ABILITY_NXD_OUT.exists(), f"ability neuter NXD missing: {neuter.ABILITY_NXD_OUT}")
    check(neuter.ABILITY_NXD_OUT.stat().st_size > 0, "ability neuter NXD should not be empty")

    check_weapon_xml()
    check_charge_aim_xml()
    damaging = set(neuter.classify_damaging_abilities())
    check_ability_classification(damaging)
    check_sentinel_classification()
    check_override_sqlite(damaging)
    check_nxd_round_trip()
    check_coverage_report()

    print("neuter data smoke tests passed")
    return 0


def check_weapon_xml() -> None:
    root = ET.parse(neuter.OUT).getroot()
    entries = root.find("Entries")
    check(entries is not None, "weapon neuter XML missing <Entries>")

    rows = entries.findall("ItemWeapon")
    check(len(rows) == EXPECTED_WEAPON_COUNT, f"expected {EXPECTED_WEAPON_COUNT} neutered weapons, got {len(rows)}")

    ids: set[int] = set()
    for row in rows:
        child_tags = [child.tag for child in row]
        check(child_tags == ["Id", "Power"], f"weapon neuter row should only ship Id+Power, got {child_tags}")
        item_id = int(row.findtext("Id", "-1"))
        power = int(row.findtext("Power", "-1"))
        check(item_id not in ids, f"duplicate weapon id in neuter XML: {item_id}")
        ids.add(item_id)
        check(power == neuter.NEUTER_POWER, f"weapon {item_id} Power should be {neuter.NEUTER_POWER}, got {power}")


def check_charge_aim_xml() -> None:
    root = ET.parse(neuter.CHARGE_AIM_OUT).getroot()
    entries = root.find("Entries")
    check(entries is not None, "charge/aim neuter XML missing <Entries>")

    rows = entries.findall("AbilityChargeAim")
    check(len(rows) == EXPECTED_CHARGE_AIM_COUNT, f"expected {EXPECTED_CHARGE_AIM_COUNT} neutered Aim rows, got {len(rows)}")

    ids: set[int] = set()
    for row in rows:
        child_tags = [child.tag for child in row]
        check(child_tags == ["Id", "Power"], f"charge/aim neuter row should only ship Id+Power, got {child_tags}")
        ability_id = int(row.findtext("Id", "-1"))
        power = int(row.findtext("Power", "-1"))
        check(ability_id not in ids, f"duplicate Aim ability id in neuter XML: {ability_id}")
        ids.add(ability_id)
        check(407 <= ability_id <= 413, f"charge/aim neuter should only cover Aim +2..+20, got {ability_id}")
        check(
            power == neuter.NEUTER_CHARGE_AIM_POWER,
            f"Aim ability {ability_id} Power should be {neuter.NEUTER_CHARGE_AIM_POWER}, got {power}",
        )

    check(406 not in ids, "Aim +1 should be omitted because vanilla Power is already 1")


def check_ability_classification(damaging: set[int]) -> None:
    check(len(damaging) == EXPECTED_ABILITY_COUNT + EXPECTED_SKIPPED_COUNT, f"unexpected damaging ability count: {len(damaging)}")
    for ability_id, name in DAMAGING_SAMPLES.items():
        check(ability_id in damaging, f"{name} ({ability_id}) should be classified as damaging")
    for ability_id, name in NON_DAMAGING_SAMPLES.items():
        check(ability_id not in damaging, f"{name} ({ability_id}) should not be classified as damaging")


def check_sentinel_classification() -> None:
    bands = neuter.classify_damaging_ability_bands()
    check(bands[16] == "high", "Fire should classify into the high/magical sentinel band")
    check(bands[20] == "high", "Thunder should classify into the high/magical sentinel band")
    check(bands[100] == "mid", "PhysicalAttack sample 100 should classify into the mid sentinel band")
    check(bands[382] == "low", "Throw high-id fallback currently has only the low generic sentinel band")
    check(neuter.ability_placeholder_xy(16, bands, "uniform") == neuter.NEUTER_XY, "uniform ability placeholder should stay X/Y=1")
    check(neuter.ability_placeholder_xy(16, bands, "sentinel-coarse-v1") == neuter.SENTINEL_BAND_VALUES["high"], "high sentinel ability placeholder should use the high band value")
    check(neuter.ability_placeholder_xy(100, bands, "sentinel-coarse-v1") == neuter.SENTINEL_BAND_VALUES["mid"], "mid sentinel ability placeholder should use the mid band value")

    categories = neuter.load_weapon_categories_by_weapon_data_id()
    check(neuter.weapon_sentinel_band(19, categories) == "low", "Sword weapon-data id 19 should classify into low/swing band")
    check(neuter.weapon_sentinel_band(73, categories) == "mid", "Gun weapon-data id 73 should classify into mid/ranged band")
    check(neuter.weapon_placeholder_power(19, 4, categories, "sentinel-coarse-v1") == neuter.SENTINEL_BAND_VALUES["low"], "low sentinel weapon should use low band power")
    check(neuter.weapon_placeholder_power(73, 5, categories, "sentinel-coarse-v1") == neuter.SENTINEL_BAND_VALUES["mid"], "mid sentinel weapon should use mid band power")
    check(neuter.charge_aim_placeholder_power("sentinel-coarse-v1") == neuter.SENTINEL_BAND_VALUES["mid"], "Aim/Charge sentinel fallback should use the mid band")


def check_override_sqlite(damaging: set[int]) -> None:
    base_rows = read_override_rows(neuter.BASE_OVERRIDE_SQLITE)
    neuter_rows = read_override_rows(neuter.NEUTER_OVERRIDE_SQLITE)
    check(base_rows.keys() == neuter_rows.keys(), "base and neuter override sqlite should have the same keys")
    check(len(neuter_rows) == 368, f"expected 368 override rows, got {len(neuter_rows)}")

    present = set(neuter_rows)
    to_neuter = damaging & present
    skipped = damaging - present
    check(len(to_neuter) == EXPECTED_ABILITY_COUNT, f"expected {EXPECTED_ABILITY_COUNT} ability rows neutered, got {len(to_neuter)}")
    check(len(skipped) == EXPECTED_SKIPPED_COUNT, f"expected {EXPECTED_SKIPPED_COUNT} skipped ability ids, got {len(skipped)}")
    check(all(ability_id >= len(neuter_rows) for ability_id in skipped), f"skipped ability ids should be outside the override table: {sorted(skipped)}")

    columns = override_columns(neuter.NEUTER_OVERRIDE_SQLITE)
    x_idx = columns.index("X")
    y_idx = columns.index("Y")
    for key, row in neuter_rows.items():
        base_row = base_rows[key]
        if key in to_neuter:
            check(row[x_idx] == neuter.NEUTER_XY, f"ability {key} X should be {neuter.NEUTER_XY}, got {row[x_idx]}")
            check(row[y_idx] == neuter.NEUTER_XY, f"ability {key} Y should be {neuter.NEUTER_XY}, got {row[y_idx]}")
            for idx, column in enumerate(columns):
                if column in {"X", "Y"}:
                    continue
                check(row[idx] == base_row[idx], f"ability {key} column {column} changed unexpectedly")
        else:
            check(row == base_row, f"non-damaging ability {key} should be unchanged")

    for ability_id, name in DAMAGING_SAMPLES.items():
        row = neuter_rows[ability_id]
        check(row[x_idx] == neuter.NEUTER_XY and row[y_idx] == neuter.NEUTER_XY, f"{name} should have X=Y={neuter.NEUTER_XY}")
    for ability_id, name in NON_DAMAGING_SAMPLES.items():
        row = neuter_rows[ability_id]
        base_row = base_rows[ability_id]
        check(row[x_idx] == base_row[x_idx] and row[y_idx] == base_row[y_idx], f"{name} should keep inherited X/Y")


def check_nxd_round_trip() -> None:
    check(neuter.DEFAULT_FF16TOOLS.exists(), f"FF16Tools CLI missing: {neuter.DEFAULT_FF16TOOLS}")
    with tempfile.TemporaryDirectory(prefix="gc_neuter_nxd_", ignore_cleanup_errors=True) as tmp:
        out_db = Path(tmp) / "roundtrip.sqlite"
        subprocess.run(
            [
                str(neuter.DEFAULT_FF16TOOLS),
                "nxd-to-sqlite",
                "-i",
                str(neuter.ABILITY_NXD_OUT.parent),
                "-o",
                str(out_db),
                "-g",
                "fft",
            ],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.PIPE,
            text=True,
        )
        check(out_db.exists(), "FF16Tools nxd-to-sqlite did not produce a sqlite database")
        sqlite_rows = read_override_rows(neuter.NEUTER_OVERRIDE_SQLITE)
        nxd_rows = read_override_rows(out_db)

    check(sqlite_rows.keys() == nxd_rows.keys(), "round-tripped NXD keys should match neuter sqlite keys")
    for ability_id in [*DAMAGING_SAMPLES.keys(), *NON_DAMAGING_SAMPLES.keys()]:
        check(nxd_rows[ability_id] == sqlite_rows[ability_id], f"round-tripped NXD row mismatch for ability {ability_id}")


def check_coverage_report() -> None:
    check(coverage.DEFAULT_OUT.exists(), f"coverage report missing: {coverage.DEFAULT_OUT}")
    expected = coverage.build_report()
    actual = coverage.DEFAULT_OUT.read_text(encoding="utf-8")
    check(actual == expected, "coverage report is stale; run python tools/report_neuter_coverage.py")


def override_columns(path) -> list[str]:
    with sqlite3.connect(path) as con:
        return [row[1] for row in con.execute("PRAGMA table_info(OverrideAbilityActionData)")]


def read_override_rows(path) -> dict[int, tuple[object, ...]]:
    columns = override_columns(path)
    key_idx = columns.index("Key")
    with sqlite3.connect(path) as con:
        rows = con.execute("SELECT * FROM OverrideAbilityActionData ORDER BY Key").fetchall()
    return {int(row[key_idx]): row for row in rows}


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
