#!/usr/bin/env python3
"""Smoke test for the generated runtime formula context report."""
from __future__ import annotations

import report_runtime_formula_context as report


def main() -> int:
    generated = report.build_report()
    check(report.OUT.exists(), f"runtime formula context report missing: {report.OUT}")
    actual = report.OUT.read_text(encoding="utf-8")
    check(actual == generated, "runtime formula context report is stale; run python tools/report_runtime_formula_context.py")

    for text in [
        "`diceavg`",
        "`diceroll`",
        "`targetbyte`",
        "`attackerbyte`",
        "`tableclamp`",
        "`matrixclamp`",
        "`mapor`",
        "`vanillaDamage`",
        "`equipmentDr`",
        "`response.permille`",
        "`result.finalDamage`",
        "`attacker.sourceCt`",
        "`attacker.sourceCounter`",
        "`status.invisible`",
        "`status.ko`",
        "`element.absorb.fire`",
        "## Ability Variables",
        "`dcl.approved`",
        "`dcl.side_effect_managed_multistrike`",
        "`dcl.strike.hitCount`",
        "`guard.blockRemaining`",
        "`direct`",
        "`counter_magic`",
        "`inflict_darkness`",
        "`MinHpFloor=1`",
        "`<slotName>.present`",
        "`<slotName>.scanMatches`",
        "`<slotName>.armorHpBonus`",
        "`category_armor`",
        "`ActionSignalRules.Variables`",
        "`FormulaDerivedVariables`",
    ]:
        check(text in generated, f"report missing expected capability: {text}")

    check("\n- `approved`\n" not in generated, "DCL approval metadata must not be reported at ability.approved")
    check("\n- `strike_count`\n" not in generated, "DCL strike count must not be reported at ability.strike_count")

    check("Test 2b still has to" not in generated, "report still describes the refuted KO write path as pending")

    print("runtime formula context report smoke test passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
