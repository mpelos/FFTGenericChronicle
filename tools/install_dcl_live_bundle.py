#!/usr/bin/env python3
"""Install one exact hash-bound DCL live bundle without inspecting host processes."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from validate_dcl_live_install import (
    DEFAULT_APP_CONFIG,
    DEFAULT_INSTALLED_ABILITY_CATALOG,
    DEFAULT_INSTALLED_ABILITY_CHARGE_AIM_XML,
    DEFAULT_INSTALLED_DLL,
    DEFAULT_INSTALLED_ITEM_CATALOG,
    DEFAULT_INSTALLED_ITEM_WEAPON_XML,
    DEFAULT_INSTALLED_NXD,
    DEFAULT_INSTALLED_SETTINGS,
    DEFAULT_LOCAL_DLL,
    DEFAULT_PAIR,
    REQUIRED_MODS,
    analyze_install,
)
from validate_dcl_runtime_data_pair import PairValidationError, ROOT, validate_pair


PAIR_FILES = (
    ("settings", "settings"),
    ("action_data_nxd", "action data"),
    ("item_weapon_data_xml", "item weapon data"),
    ("ability_charge_aim_data_xml", "ability charge/aim data"),
    ("item_catalog_csv", "item catalog"),
    ("ability_catalog_csv", "ability catalog"),
)

RETIRED_RUNTIME_CONTROLS = (
    "DclApproachEnabled",
    "DclFearControlEnabled",
    "DclFearForcedFleeControlEnabled",
    "DclFearPlayerConfirmEnforcementEnabled",
)


class InstallError(ValueError):
    pass


@dataclass(frozen=True)
class LivePaths:
    app_config: Path
    installed_settings: Path
    installed_nxd: Path
    installed_item_weapon_xml: Path
    installed_ability_charge_aim_xml: Path
    installed_item_catalog: Path
    installed_ability_catalog: Path
    local_dll: Path
    installed_dll: Path


DEFAULT_PATHS = LivePaths(
    DEFAULT_APP_CONFIG,
    DEFAULT_INSTALLED_SETTINGS,
    DEFAULT_INSTALLED_NXD,
    DEFAULT_INSTALLED_ITEM_WEAPON_XML,
    DEFAULT_INSTALLED_ABILITY_CHARGE_AIM_XML,
    DEFAULT_INSTALLED_ITEM_CATALOG,
    DEFAULT_INSTALLED_ABILITY_CATALOG,
    DEFAULT_LOCAL_DLL,
    DEFAULT_INSTALLED_DLL,
)


@dataclass(frozen=True)
class PlannedWrite:
    label: str
    source: Path | None
    destination: Path
    content: bytes
    sha256: str


def digest_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest().upper()


def load_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise InstallError(f"cannot read {label} {path}: {error}") from error
    if not isinstance(value, dict):
        raise InstallError(f"{label} must be a JSON object: {path}")
    return value


def repo_source(raw: object, label: str) -> Path:
    if not isinstance(raw, str) or not raw:
        raise InstallError(f"pair {label} must be a repository-relative path")
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as error:
        raise InstallError(f"pair {label} escapes the repository: {raw}") from error
    if not path.is_file():
        raise InstallError(f"pair {label} is missing: {path}")
    return path


def desired_app_config(path: Path) -> bytes:
    app = load_object(path, "Reloaded app config")
    location = app.get("AppLocation")
    if not isinstance(location, str) or Path(location).name.lower() != "fft_enhanced.exe":
        raise InstallError("Reloaded profile must target FFT_enhanced.exe")
    app["EnabledMods"] = list(REQUIRED_MODS)
    return (json.dumps(app, ensure_ascii=False, indent=2) + "\n").encode("utf-8")


def build_plan(pair: dict[str, Any], paths: LivePaths) -> list[PlannedWrite]:
    hashes = pair.get("sha256")
    if not isinstance(hashes, dict):
        raise InstallError("pair sha256 must be an object")
    destinations = (
        paths.installed_settings,
        paths.installed_nxd,
        paths.installed_item_weapon_xml,
        paths.installed_ability_charge_aim_xml,
        paths.installed_item_catalog,
        paths.installed_ability_catalog,
    )
    writes: list[PlannedWrite] = []
    for (key, label), destination in zip(PAIR_FILES, destinations, strict=True):
        source = repo_source(pair.get(key), key)
        content = source.read_bytes()
        actual = digest_bytes(content)
        expected = hashes.get(key)
        if not isinstance(expected, str) or actual != expected.upper():
            raise InstallError(
                f"pair source hash mismatch for {key}: expected {expected}, got {actual}"
            )
        if key == "settings":
            settings = load_object(source, "runtime settings")
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
                raise InstallError(
                    "runtime settings enable retired DCL controls: "
                    + ", ".join(enabled_retired)
                )
        writes.append(PlannedWrite(label, source, destination.resolve(), content, actual))

    if not paths.local_dll.is_file():
        raise InstallError(f"current Release DLL is missing: {paths.local_dll}")
    dll = paths.local_dll.read_bytes()
    writes.append(
        PlannedWrite(
            "code-mod DLL",
            paths.local_dll.resolve(),
            paths.installed_dll.resolve(),
            dll,
            digest_bytes(dll),
        )
    )
    app = desired_app_config(paths.app_config)
    writes.append(
        PlannedWrite(
            "Reloaded Enhanced app config",
            None,
            paths.app_config.resolve(),
            app,
            digest_bytes(app),
        )
    )
    return writes


def _atomic_replace(write: PlannedWrite, stamp: int) -> None:
    write.destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = write.destination.with_name(f"{write.destination.name}.tmp-dcl-bundle-{stamp}")
    try:
        temporary.write_bytes(write.content)
        os.replace(temporary, write.destination)
    finally:
        temporary.unlink(missing_ok=True)


def apply_plan(writes: list[PlannedWrite], stamp: int) -> dict[Path, Path | None]:
    backups: dict[Path, Path | None] = {}
    for write in writes:
        destination = write.destination
        if destination.exists():
            backup = destination.with_name(f"{destination.name}.bak-dcl-bundle-{stamp}")
            if backup.exists():
                raise InstallError(f"backup already exists: {backup}")
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(destination, backup)
            backups[destination] = backup
        else:
            backups[destination] = None

    applied: list[Path] = []
    try:
        for write in writes:
            _atomic_replace(write, stamp)
            applied.append(write.destination)
    except Exception as error:
        for destination in reversed(applied):
            backup = backups[destination]
            if backup is None:
                destination.unlink(missing_ok=True)
            else:
                shutil.copy2(backup, destination)
        raise InstallError(f"installation failed and was rolled back: {error}") from error
    return backups


def verify_installed(pair: dict[str, Any], paths: LivePaths) -> list[str]:
    app = load_object(paths.app_config, "Reloaded app config")
    errors, details = analyze_install(
        pair,
        app,
        paths.installed_settings,
        paths.installed_nxd,
        paths.installed_item_weapon_xml,
        paths.installed_ability_charge_aim_xml,
        paths.installed_item_catalog,
        paths.installed_ability_catalog,
        paths.local_dll,
        paths.installed_dll,
    )
    if errors:
        raise InstallError("post-install preflight failed: " + "; ".join(errors))
    return details


def install_bundle(
    pair: dict[str, Any], paths: LivePaths, *, apply: bool, stamp: int | None = None
) -> tuple[list[PlannedWrite], dict[Path, Path | None], list[str]]:
    writes = build_plan(pair, paths)
    if not apply:
        return writes, {}, []
    actual_stamp = int(time.time()) if stamp is None else stamp
    backups = apply_plan(writes, actual_stamp)
    try:
        details = verify_installed(pair, paths)
    except Exception:
        for destination, backup in reversed(list(backups.items())):
            if backup is None:
                destination.unlink(missing_ok=True)
            else:
                shutil.copy2(backup, destination)
        raise
    return writes, backups, details


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--pair", type=Path, default=DEFAULT_PAIR)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    if args.pair is None:
        print(
            "ERROR: no active integrated runtime/data pair is defined; "
            "pass --pair explicitly after building a current pair"
        )
        return 1

    pair_path = args.pair.resolve()
    try:
        validate_pair(pair_path)
        pair = load_object(pair_path, "runtime/data pair")
        writes, backups, details = install_bundle(pair, DEFAULT_PATHS, apply=args.apply)
    except (PairValidationError, InstallError, OSError) as error:
        print(f"ERROR: {error}")
        return 1

    mode = "APPLY" if args.apply else "DRY RUN"
    print(f"DCL exact live bundle install {mode}")
    print("  process inspection: none")
    for write in writes:
        current = "missing"
        if write.destination.is_file():
            current = hashlib.sha256(write.destination.read_bytes()).hexdigest().upper()
        print(f"  {write.label}: {write.destination}")
        print(f"    desired={write.sha256} current={current}")
    if not args.apply:
        print("No files changed. Use --apply only after FFT and Reloaded are visibly closed.")
        return 0
    for destination, backup in backups.items():
        print(f"  backup {destination} -> {backup or '<new file>'}")
    print("DCL live installation preflight PASS")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
