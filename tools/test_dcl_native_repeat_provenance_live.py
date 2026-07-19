#!/usr/bin/env python3
"""Smoke tests for the native-repeat provenance live analyzer."""
from __future__ import annotations

from analyze_dcl_native_repeat_provenance_live import analyze_text


def tx(n: int, state: int, payload: int, debit: int, dcl: bool, delta: int) -> str:
    lines = [
        f"[DCL-CALC-PROVENANCE] n={n} origin=outer-sweep returnRva=0x281F12 battleState=0x{state:X} "
        f"turnOwner=0 sourceIdx=0 forecastPtr=0x1 casterIdx=16 type=0x01 abilityId=0 payload={payload} targetIdx=0",
        f"[DCL-PRECLAMP] target=0x82 targetIdx=0 debit={debit} credit=0 battleState=0x2C latest=outer-sweep:state=0x{state:X}",
    ]
    if dcl:
        lines.append(
            f"[DCL] caster=0x80 target=0x82 abilityId=0 ability=<Nothing> actionType=0x01 result=1 "
            f"debit=1 oldDebit={debit} credit=0 oldCredit=0"
        )
    lines.append(f"[DAMAGE ptr=0x1 id=0x82] 400 -> {400-delta} = {delta} sampleAgeMs=1")
    return "\n".join(lines)


def main() -> int:
    valid = "\n".join((
        tx(1, 0x2A, 124, 189, True, 1),
        tx(2, 0x2F, 124, 189, False, 189),
        tx(3, 0x2A, 18, 126, True, 1),
        tx(4, 0x2F, 18, 126, False, 126),
    ))
    pairs, errors = analyze_text(valid)
    assert len(pairs) == 2
    assert not errors, errors

    fixed = "\n".join((
        tx(1, 0x2A, 124, 210, True, 1),
        tx(2, 0x2F, 124, 126, True, 1),
        tx(3, 0x2A, 18, 126, True, 1),
        tx(4, 0x2F, 18, 126, True, 1),
    ))
    fixed_pairs, fixed_errors = analyze_text(fixed, expect_fixed=True)
    assert len(fixed_pairs) == 2
    assert not fixed_errors, fixed_errors

    _, identity_errors = analyze_text(valid.replace("payload=124 targetIdx=0", "payload=125 targetIdx=0", 1))
    assert any("expected at least" in error for error in identity_errors)

    _, missing_rewrite_errors = analyze_text(valid.replace("debit=1 oldDebit=189", "debit=189 oldDebit=189"))
    assert any("not rewritten to one" in error for error in missing_rewrite_errors)

    _, false_escape_errors = analyze_text(valid.replace("[DAMAGE ptr=0x1 id=0x82] 400 -> 211 = 189", "[DAMAGE ptr=0x1 id=0x82] 400 -> 399 = 1", 1))
    assert any("does not equal native debit" in error for error in false_escape_errors)

    _, fixed_escape_errors = analyze_text(valid, expect_fixed=True)
    assert any("fixed state-0x2F transaction was not rewritten" in error for error in fixed_escape_errors)

    bad_fixed_delta = "\n".join((
        tx(1, 0x2A, 124, 189, True, 1),
        tx(2, 0x2F, 124, 189, True, 189),
        tx(3, 0x2A, 18, 126, True, 1),
        tx(4, 0x2F, 18, 126, True, 1),
    ))
    _, fixed_delta_errors = analyze_text(bad_fixed_delta, expect_fixed=True)
    assert any("fixed state-0x2F applied delta" in error for error in fixed_delta_errors)
    print("native repeat provenance live analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
