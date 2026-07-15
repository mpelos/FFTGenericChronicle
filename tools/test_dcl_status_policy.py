#!/usr/bin/env python3
"""Smoke test for the DCL ability-by-status policy manifest."""
from __future__ import annotations

from collections import Counter

import report_dcl_status_policy as report


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    rows, errors = report.load_manifest(report.DEFAULT_CATALOG)
    check(not errors, "status policy errors:\n" + "\n".join(errors))
    check(len(rows) == 294, f"expected 294 expanded rows, got {len(rows)}")
    check({row["status"] for row in rows} == set(report.STATUS_BITS), "every observed native status token needs a bit mapping")

    def find(ability_id: int, status: str) -> dict[str, str]:
        return next(row for row in rows if int(row["ability_id"]) == ability_id and row["status"] == status)

    check(find(28, "Poison")["resist_stat"] == "base-HP", "Poison nature must stay physical even from a spell")
    check(find(109, "Charm")["resist_stat"] == "high-Brave", "Charm must use the mental Brave axis")
    check(find(213, "DontMove")["resist_category"] == "physical-body" and find(213, "DontMove")["duration_policy"] == "1-target-turn",
          "Leg Shot candidate must express physical Knockdown semantics")
    check(find(245, "DontAct")["resist_stat"] == "inverse-Faith", "magical Disable must use inverse Faith")
    check(find(14, "Petrify")["operation"] == "remove-negative" and find(14, "Petrify")["resist_stat"] == "none",
          "Esuna rows must be removal without a resistance contest")
    check(find(5, "Dead")["operation"] == "remove-ko" and find(5, "Dead")["readiness"] == "native-lifecycle-preserved" and
          find(5, "Dead")["mechanism"] == "preserve_native_revive",
          "Raise must preserve native revive lifecycle rather than use a generic status clear")
    check(find(157, "Dead")["readiness"] == "data-authoring-required" and
          find(157, "Dead")["mechanism"] == "dcl_instant_ko",
          "offensive instant KO must use lethal staged HP after native-rider data suppression")
    check(find(314, "Crystal")["readiness"] == "native-lifecycle-preserved" and
          find(314, "Crystal")["mechanism"] == "preserve_native_bequeath",
          "Bequeath Bacon must preserve native Crystal/campaign lifecycle")
    check(find(116, "Invite")["readiness"] == "campaign-mechanism-required", "Invite needs campaign-safe handling")
    check(find(234, "Darkness")["readiness"] == "design-decision-required", "unassigned status nature must remain explicit")

    readiness = Counter(row["readiness"] for row in rows)
    check(readiness["surface-ready"] > 0, "known status categories should use the existing contest surface")
    check(readiness["design-decision-required"] > 0, "open status natures must remain visible")
    print("DCL status policy smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
