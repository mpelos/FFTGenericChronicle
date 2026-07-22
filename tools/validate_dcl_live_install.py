#!/usr/bin/env python3
"""Fail closed unless Reloaded is configured with one exact hash-bound DCL live bundle."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

from validate_dcl_runtime_data_pair import PairValidationError, ROOT, validate_pair


HISTORICAL_CLEAN_V1_PAIR = ROOT / "work/1784470893-dcl-unified-clean-v1-runtime-data-pair.json"
DEFAULT_PAIR: Path | None = ROOT / "work/1784683300-dcl-active-integrated-runtime-data-pair.json"
DEFAULT_APP_CONFIG = Path(r"C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json")
DEFAULT_INSTALLED_SETTINGS = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json"
)
DEFAULT_INSTALLED_NXD = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle\FFTIVC\data\enhanced\nxd"
    r"\overrideabilityactiondata.nxd"
)

RETIRED_RUNTIME_CONTROLS = (
    "DclApproachEnabled",
    "DclFearControlEnabled",
    "DclFearForcedFleeControlEnabled",
    "DclFearPlayerConfirmEnforcementEnabled",
)
DEFAULT_INSTALLED_ITEM_WEAPON_XML = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle\FFTIVC\tables\enhanced"
    r"\ItemWeaponData.xml"
)
DEFAULT_INSTALLED_ABILITY_CHARGE_AIM_XML = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle\FFTIVC\tables\enhanced"
    r"\AbilityChargeAimData.xml"
)
DEFAULT_INSTALLED_ITEM_CATALOG = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\item_catalog.csv"
)
DEFAULT_INSTALLED_ABILITY_CATALOG = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\wotl_ability_action_baseline.csv"
)
DEFAULT_LOCAL_DLL = (
    ROOT / "codemod/_build/fftivc.generic.chronicle.codemod/fftivc.generic.chronicle.codemod.dll"
)
DEFAULT_INSTALLED_DLL = Path(
    r"C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod"
    r"\fftivc.generic.chronicle.codemod.dll"
)
REQUIRED_MODS = (
    "fftivc.utility.modloader",
    "fftivc.generic.chronicle",
    "fftivc.generic.chronicle.codemod",
)


class LiveInstallError(ValueError):
    pass


def _object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise LiveInstallError(f"cannot read {label} {path}: {error}") from error
    if not isinstance(value, dict):
        raise LiveInstallError(f"{label} must be a JSON object: {path}")
    return value


def _hash(path: Path, label: str) -> str:
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest().upper()
    except OSError as error:
        raise LiveInstallError(f"cannot hash {label} {path}: {error}") from error


def analyze_install(
    pair: dict[str, Any],
    app: dict[str, Any],
    installed_settings: Path,
    installed_nxd: Path,
    installed_item_weapon_xml: Path,
    installed_ability_charge_aim_xml: Path,
    installed_item_catalog: Path,
    installed_ability_catalog: Path,
    local_dll: Path,
    installed_dll: Path,
    *,
    allow_extra_mods: bool = False,
) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    details: list[str] = []
    hashes = pair.get("sha256")
    if not isinstance(hashes, dict):
        return ["pair sha256 must be an object"], details

    enabled_raw = app.get("EnabledMods")
    if not isinstance(enabled_raw, list) or not all(isinstance(value, str) for value in enabled_raw):
        errors.append("AppConfig.EnabledMods must be a string list")
        enabled: set[str] = set()
    else:
        enabled = set(enabled_raw)
        missing = sorted(set(REQUIRED_MODS) - enabled)
        extras = sorted(enabled - set(REQUIRED_MODS))
        if missing:
            errors.append("Reloaded profile lacks required mods: " + ", ".join(missing))
        if extras and not allow_extra_mods:
            errors.append("isolated Reloaded profile has extra mods: " + ", ".join(extras))
        details.append("enabled_mods=" + ",".join(enabled_raw))

    app_location = app.get("AppLocation")
    if not isinstance(app_location, str) or Path(app_location).name.lower() != "fft_enhanced.exe":
        errors.append("Reloaded profile must target FFT_enhanced.exe")
    else:
        details.append(f"app={app_location}")

    for expected, path, label in (
        (hashes.get("settings"), installed_settings, "installed settings"),
        (hashes.get("action_data_nxd"), installed_nxd, "installed action data"),
        (
            hashes.get("item_weapon_data_xml"),
            installed_item_weapon_xml,
            "installed item weapon data",
        ),
        (
            hashes.get("ability_charge_aim_data_xml"),
            installed_ability_charge_aim_xml,
            "installed ability charge/aim data",
        ),
        (hashes.get("item_catalog_csv"), installed_item_catalog, "installed item catalog"),
        (
            hashes.get("ability_catalog_csv"),
            installed_ability_catalog,
            "installed ability catalog",
        ),
    ):
        if not isinstance(expected, str) or len(expected) != 64:
            errors.append(f"pair lacks a valid SHA-256 for {label}")
            continue
        try:
            actual = _hash(path, label)
        except LiveInstallError as error:
            errors.append(str(error))
            continue
        if actual != expected.upper():
            errors.append(f"{label} hash mismatch: expected {expected.upper()}, got {actual}")
        else:
            details.append(f"{label.replace(' ', '_')}_sha256={actual}")

    try:
        settings = _object(installed_settings, "installed settings")
    except LiveInstallError as error:
        errors.append(str(error))
    else:
        enabled_retired = [
            name for name in RETIRED_RUNTIME_CONTROLS if settings.get(name) is True
        ]
        rules = settings.get("DclStatusRules")
        if isinstance(rules, list) and any(
            isinstance(rule, dict) and rule.get("Name") == "dcl-fear" for rule in rules
        ):
            enabled_retired.append("DclStatusRules[dcl-fear]")
        if settings.get("DclFearStatusRuleName") not in (None, ""):
            enabled_retired.append("DclFearStatusRuleName")
        if enabled_retired:
            errors.append(
                "installed settings enable retired DCL controls: "
                + ", ".join(enabled_retired)
            )

    try:
        local_dll_hash = _hash(local_dll, "local Release DLL")
        installed_dll_hash = _hash(installed_dll, "installed DLL")
    except LiveInstallError as error:
        errors.append(str(error))
    else:
        if local_dll_hash != installed_dll_hash:
            errors.append(
                "installed code-mod DLL differs from the current Release build: "
                f"local={local_dll_hash}, installed={installed_dll_hash}"
            )
        else:
            details.append(f"codemod_dll_sha256={local_dll_hash}")

    return errors, details


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--pair", type=Path, default=DEFAULT_PAIR)
    parser.add_argument("--app-config", type=Path, default=DEFAULT_APP_CONFIG)
    parser.add_argument("--installed-settings", type=Path, default=DEFAULT_INSTALLED_SETTINGS)
    parser.add_argument("--installed-nxd", type=Path, default=DEFAULT_INSTALLED_NXD)
    parser.add_argument(
        "--installed-item-weapon-xml", type=Path, default=DEFAULT_INSTALLED_ITEM_WEAPON_XML
    )
    parser.add_argument(
        "--installed-ability-charge-aim-xml",
        type=Path,
        default=DEFAULT_INSTALLED_ABILITY_CHARGE_AIM_XML,
    )
    parser.add_argument(
        "--installed-item-catalog", type=Path, default=DEFAULT_INSTALLED_ITEM_CATALOG
    )
    parser.add_argument(
        "--installed-ability-catalog", type=Path, default=DEFAULT_INSTALLED_ABILITY_CATALOG
    )
    parser.add_argument("--local-dll", type=Path, default=DEFAULT_LOCAL_DLL)
    parser.add_argument("--installed-dll", type=Path, default=DEFAULT_INSTALLED_DLL)
    parser.add_argument("--allow-extra-mods", action="store_true")
    args = parser.parse_args()

    if args.pair is None:
        print(
            "ERROR: no active integrated runtime/data pair is defined; "
            "pass --pair explicitly after building a current pair"
        )
        print("DCL live installation preflight FAIL")
        return 1

    pair_path = args.pair.resolve()
    try:
        validate_pair(pair_path)
        pair = _object(pair_path, "runtime/data pair")
        app = _object(args.app_config.resolve(), "Reloaded app config")
        errors, details = analyze_install(
            pair,
            app,
            args.installed_settings.resolve(),
            args.installed_nxd.resolve(),
            args.installed_item_weapon_xml.resolve(),
            args.installed_ability_charge_aim_xml.resolve(),
            args.installed_item_catalog.resolve(),
            args.installed_ability_catalog.resolve(),
            args.local_dll.resolve(),
            args.installed_dll.resolve(),
            allow_extra_mods=args.allow_extra_mods,
        )
    except (PairValidationError, LiveInstallError) as error:
        errors = [str(error)]
        details = []

    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        print("DCL live installation preflight FAIL")
        return 1
    print("DCL live installation preflight PASS")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
