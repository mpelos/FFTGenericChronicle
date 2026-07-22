#!/usr/bin/env python3
"""Smoke tests for the process-free DCL live-install preflight."""
from __future__ import annotations

import hashlib
import json
import tempfile
from pathlib import Path

import validate_dcl_live_install as live


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main() -> int:
    assert live.DEFAULT_PAIR == (
        live.ROOT / "work/1784683300-dcl-active-integrated-runtime-data-pair.json"
    )
    assert live.HISTORICAL_CLEAN_V1_PAIR == (
        live.ROOT / "work/1784470893-dcl-unified-clean-v1-runtime-data-pair.json"
    )
    assert live.DEFAULT_PAIR != live.HISTORICAL_CLEAN_V1_PAIR
    with tempfile.TemporaryDirectory() as raw:
        temp = Path(raw)
        settings = temp / "settings.json"
        nxd = temp / "action.nxd"
        item_weapon = temp / "ItemWeaponData.xml"
        charge_aim = temp / "AbilityChargeAimData.xml"
        item_catalog = temp / "item_catalog.csv"
        ability_catalog = temp / "wotl_ability_action_baseline.csv"
        local_dll = temp / "local.dll"
        installed_dll = temp / "installed.dll"
        settings.write_text(json.dumps({
            "DclApproachEnabled": False,
            "DclFearControlEnabled": False,
            "DclFearForcedFleeControlEnabled": False,
            "DclFearPlayerConfirmEnforcementEnabled": False,
        }), encoding="utf-8")
        nxd.write_bytes(b"action-data-v4")
        item_weapon.write_bytes(b"item-weapon-v4")
        charge_aim.write_bytes(b"charge-aim-v4")
        item_catalog.write_bytes(b"item-catalog-v4")
        ability_catalog.write_bytes(b"ability-catalog-v4")
        local_dll.write_bytes(b"current-release")
        installed_dll.write_bytes(local_dll.read_bytes())

        pair = {"sha256": {
            "settings": digest(settings),
            "action_data_nxd": digest(nxd),
            "item_weapon_data_xml": digest(item_weapon),
            "ability_charge_aim_data_xml": digest(charge_aim),
            "item_catalog_csv": digest(item_catalog),
            "ability_catalog_csv": digest(ability_catalog),
        }}
        app = {
            "AppLocation": r"D:\SteamLibrary\steamapps\common\FFT\FFT_enhanced.exe",
            "EnabledMods": list(live.REQUIRED_MODS),
        }
        errors, details = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert errors == []
        assert any(detail.startswith("enabled_mods=") for detail in details)

        clean_settings_text = settings.read_text(encoding="utf-8")
        retired_settings = json.loads(clean_settings_text)
        retired_settings["DclFearStatusRuleName"] = "dcl-fear"
        retired_settings["DclStatusRules"] = [{"Name": "dcl-fear"}]
        settings.write_text(json.dumps(retired_settings), encoding="utf-8")
        retired_pair = {"sha256": {**pair["sha256"], "settings": digest(settings)}}
        errors, _ = live.analyze_install(
            retired_pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("retired DCL controls" in error for error in errors)
        settings.write_text(clean_settings_text, encoding="utf-8")

        missing_mod = {**app, "EnabledMods": ["fftivc.utility.modloader"]}
        errors, _ = live.analyze_install(
            pair, missing_mod, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("lacks required mods" in error for error in errors)

        extra_mod = {**app, "EnabledMods": [*live.REQUIRED_MODS, "unrelated.mod"]}
        errors, _ = live.analyze_install(
            pair, extra_mod, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("extra mods" in error for error in errors)
        errors, _ = live.analyze_install(
            pair, extra_mod, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll,
            allow_extra_mods=True
        )
        assert errors == []

        nxd.write_bytes(b"stale-action-data")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed action data hash mismatch" in error for error in errors)
        nxd.write_bytes(b"action-data-v4")

        item_weapon.write_bytes(b"stale-item-weapon")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed item weapon data hash mismatch" in error for error in errors)
        item_weapon.write_bytes(b"item-weapon-v4")

        charge_aim.write_bytes(b"stale-charge-aim")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed ability charge/aim data hash mismatch" in error for error in errors)
        charge_aim.write_bytes(b"charge-aim-v4")

        item_catalog.write_bytes(b"stale-item-catalog")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed item catalog hash mismatch" in error for error in errors)
        item_catalog.write_bytes(b"item-catalog-v4")

        ability_catalog.write_bytes(b"stale-ability-catalog")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed ability catalog hash mismatch" in error for error in errors)
        ability_catalog.write_bytes(b"ability-catalog-v4")

        installed_dll.write_bytes(b"stale-release")
        errors, _ = live.analyze_install(
            pair, app, settings, nxd, item_weapon, charge_aim, item_catalog,
            ability_catalog, local_dll, installed_dll
        )
        assert any("installed code-mod DLL differs" in error for error in errors)

    print("DCL live installation preflight tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
