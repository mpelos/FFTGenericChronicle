#!/usr/bin/env python3
"""Smoke tests for the DCL active-weapon live analyzer."""
from __future__ import annotations

from analyze_dcl_active_weapon_live import analyze_text


def row(
    n: int,
    state: int,
    payload: int,
    active: int,
    index: int,
    *,
    turn_owner: int = 16,
    caster: int = 16,
    right: int = 17,
    left: int = 18,
) -> str:
    return (
        f"[DCL-CALC-PROVENANCE] n={n} origin=outer-sweep returnRva=0x281F12 "
        f"battleState=0x{state:X} turnOwner={turn_owner} sourceIdx={turn_owner} forecastPtr=0x1 casterIdx={caster} "
        f"type=0x01 abilityId=0 payload={payload} activeWeapon={active} "
        f"repeat={index}/2 nativeWeapons={right}/{left} targetIdx=6"
    )


def preclamp(state: int, payload: int, debit: int, *, caster: int = 16) -> str:
    return (
        f"[DCL-PRECLAMP] target=0x80 targetIdx=6 debit={debit} credit=0 battleState=0x2C "
        f"latest=outer-sweep:state=0x{state:X}:caster={caster}:type=0x01:ability=0:payload={payload}"
    )


def main() -> int:
    valid = "\n".join((
        row(31, 0x2A, 18, 17, 0),
        preclamp(0x2A, 18, 210),
        row(32, 0x2F, 18, 18, 1),
        preclamp(0x2F, 18, 126),
    ))
    pairs, errors = analyze_text(valid, payload=18, expected_right=17, expected_left=18)
    assert len(pairs) == 1 and not errors, errors

    _, stale_payload_errors = analyze_text(valid, payload=17, expected_right=17, expected_left=18)
    assert any("no matching" in error for error in stale_payload_errors)

    _, wrong_active_errors = analyze_text(
        valid.replace("activeWeapon=18 repeat=1/2", "activeWeapon=17 repeat=1/2"),
        payload=18,
        expected_right=17,
        expected_left=18,
    )
    assert any("state-0x2F selected" in error for error in wrong_active_errors)

    _, wrong_carrier_errors = analyze_text(
        valid.replace("nativeWeapons=17/18", "nativeWeapons=18/17", 1),
        payload=18,
        expected_right=17,
        expected_left=18,
    )
    assert any("first row carrier" in error for error in wrong_carrier_errors)

    cancelled_then_complete = "\n".join((
        row(19, 0x2A, 18, 17, 0),
        preclamp(0x2A, 18, 273),
        row(20, 0x2F, 18, 17, 0),
        preclamp(0x2F, 18, 0),
        row(37, 0x2A, 18, 17, 0),
        preclamp(0x2A, 18, 189),
        row(38, 0x2F, 18, 18, 1),
        preclamp(0x2F, 18, 189),
    ))
    pairs, cancelled_errors = analyze_text(
        cancelled_then_complete,
        payload=18,
        expected_right=17,
        expected_left=18,
    )
    assert len(pairs) == 2 and not cancelled_errors, cancelled_errors

    reaction_only = "\n".join((
        row(25, 0x2A, 18, 17, 0, turn_owner=9),
        preclamp(0x2A, 18, 189),
        row(26, 0x2F, 18, 18, 1, turn_owner=9),
        preclamp(0x2F, 18, 189),
    ))
    _, reaction_only_errors = analyze_text(
        reaction_only,
        payload=18,
        expected_right=17,
        expected_left=18,
    )
    assert any("no completed ordinary-owner pair" in error for error in reaction_only_errors)
    print("active-weapon live analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
