#!/usr/bin/env python3
"""Smoke tests for the process-free, transactional exact DCL installer."""
from __future__ import annotations

import hashlib
import json
import tempfile
from pathlib import Path

import install_dcl_live_bundle as install


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main() -> int:
    with tempfile.TemporaryDirectory(dir=install.ROOT) as raw:
        temp = Path(raw)
        source = temp / "source"
        destination = temp / "destination"
        source.mkdir()
        destination.mkdir()

        names = {
            "settings": "settings.json",
            "action_data_nxd": "action.nxd",
            "item_weapon_data_xml": "ItemWeaponData.xml",
            "ability_charge_aim_data_xml": "AbilityChargeAimData.xml",
            "item_catalog_csv": "item_catalog.csv",
            "ability_catalog_csv": "wotl_ability_action_baseline.csv",
        }
        pair: dict[str, object] = {"sha256": {}}
        for key, name in names.items():
            path = source / name
            if key == "settings":
                path.write_text(json.dumps({
                    "DclApproachEnabled": False,
                    "DclFearControlEnabled": False,
                    "DclFearForcedFleeControlEnabled": False,
                    "DclFearPlayerConfirmEnforcementEnabled": False,
                }), encoding="utf-8")
            else:
                path.write_bytes(f"v4-{key}".encode())
            pair[key] = path.relative_to(install.ROOT).as_posix()
            pair["sha256"][key] = digest(path)  # type: ignore[index]

        local_dll = source / "current.dll"
        local_dll.write_bytes(b"current-release")
        app_config = destination / "AppConfig.json"
        app_config.write_text(
            json.dumps({
                "AppLocation": r"D:\Games\FFT_enhanced.exe",
                "EnabledMods": ["stale.mod"],
                "SortedMods": list(install.REQUIRED_MODS),
            }),
            encoding="utf-8",
        )
        installed = [destination / name for name in names.values()]
        for path in installed:
            path.write_bytes(b"stale")
        installed_dll = destination / "installed.dll"
        installed_dll.write_bytes(b"stale-dll")
        paths = install.LivePaths(
            app_config,
            installed[0],
            installed[1],
            installed[2],
            installed[3],
            installed[4],
            installed[5],
            local_dll,
            installed_dll,
        )

        writes, backups, details = install.install_bundle(pair, paths, apply=False)
        assert len(writes) == 8
        assert backups == {} and details == []
        assert installed[0].read_bytes() == b"stale"

        writes, backups, details = install.install_bundle(pair, paths, apply=True, stamp=12345)
        assert len(writes) == 8 and len(backups) == 8
        assert details
        assert installed_dll.read_bytes() == local_dll.read_bytes()
        assert json.loads(app_config.read_text(encoding="utf-8"))["EnabledMods"] == list(
            install.REQUIRED_MODS
        )
        for key, destination_path in zip(names, installed, strict=True):
            source_path = install.ROOT / pair[key]  # type: ignore[operator]
            assert destination_path.read_bytes() == source_path.read_bytes()
            backup = backups[destination_path.resolve()]
            assert backup is not None and backup.read_bytes() == b"stale"

        bad_pair = dict(pair)
        bad_pair["sha256"] = dict(pair["sha256"])  # type: ignore[arg-type]
        bad_pair["sha256"]["settings"] = "00" * 32  # type: ignore[index]
        try:
            install.build_plan(bad_pair, paths)
        except install.InstallError as error:
            assert "source hash mismatch" in str(error)
        else:
            raise AssertionError("installer accepted a stale pair source hash")

        retired_settings = source / "retired-settings.json"
        retired_settings.write_text(json.dumps({
            "DclApproachEnabled": False,
            "DclFearControlEnabled": False,
            "DclFearForcedFleeControlEnabled": False,
            "DclFearPlayerConfirmEnforcementEnabled": False,
            "DclFearStatusRuleName": "dcl-fear",
            "DclStatusRules": [{"Name": "dcl-fear"}],
        }), encoding="utf-8")
        retired_pair = dict(pair)
        retired_pair["sha256"] = dict(pair["sha256"])  # type: ignore[arg-type]
        retired_pair["settings"] = retired_settings.relative_to(install.ROOT).as_posix()
        retired_pair["sha256"]["settings"] = digest(retired_settings)  # type: ignore[index]
        try:
            install.build_plan(retired_pair, paths)
        except install.InstallError as error:
            assert "retired DCL controls" in str(error)
        else:
            raise AssertionError("installer accepted retired DCL controls")

    print("DCL exact live bundle installer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
