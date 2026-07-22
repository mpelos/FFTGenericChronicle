#!/usr/bin/env python3
"""Smoke test for the explicit DCL ability routing manifest."""
from __future__ import annotations

from collections import Counter

import report_dcl_ability_classification as report


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    rows, errors = report.load_manifest(report.DEFAULT_CATALOG)
    check(not errors, "classification errors:\n" + "\n".join(errors))
    check(len(rows) == 512, f"expected 512 abilities, got {len(rows)}")
    check(not any(row["route"] == "unclassified" for row in rows), "manifest contains unclassified routes")

    by_id = {int(row["ability_id"]): row for row in rows}
    check(by_id[16]["route"] == "magic_damage" and by_id[16]["readiness"] == "wired",
          "Fire must route to the wired numeric-magic spine")
    check(by_id[16]["candidate_damage_type"] == "elemental" and
          by_id[16]["candidate_avoidance_policy"] == "magic_evade_per_target",
          "Fire must be elemental and use per-target Magic Evade")
    check(by_id[0]["route"] == "basic_attack" and by_id[0]["readiness"] == "wired",
          "runtime action id 0 must override the catalog sentinel and route as Basic Attack")
    check(by_id[1]["route"] == "magic_healing" and by_id[1]["readiness"] == "wired",
          "Cure must route to the wired healing spine")
    check(by_id[1]["candidate_avoidance_policy"] == "none",
          "healing must not use Magic Evade")
    check(by_id[28]["route"] == "magical_status", "Poison must route to the DCL status contest")
    check(by_id[28]["candidate_status_category"] == "physical",
          "Poison's DCL resistance category is physical even when spell-delivered")
    check(by_id[53]["candidate_status_category"] == "mental",
          "Berserk must move to the DCL mental-Will category")
    check(by_id[203]["candidate_status_category"] == "physical" and
          "then_status_contest" in by_id[203]["candidate_avoidance_policy"],
          "Bio's Poison rider must get its own physical status contest after Magic Evade")
    check(by_id[213]["candidate_status_category"] == "physical",
          "Leg Shot's Don't Move flag is the authored physical Knockdown-style category")
    check(by_id[169]["candidate_action_kind"] == "magic_damage" and
          by_id[169]["candidate_avoidance_policy"] == "magic_evade_per_target" and
          by_id[169]["candidate_side_effect_policy"] == "native_multistrike",
          "multi-hit magic must preserve the DCL spell-level per-target Magic Evade policy")
    check(by_id[173]["candidate_avoidance_policy"] == "magic_evade_per_target_then_status_contest" and
          by_id[173]["candidate_side_effect_policy"] == "native_multistrike_status_rider",
          "status-bearing RandomFire must preserve both multistrike and rider ownership")
    check(by_id[257]["candidate_action_kind"] == "magic_damage" and
          by_id[257]["candidate_avoidance_policy"] == "magic_evade_per_target" and
          "damage_type" in by_id[257]["metadata_open_fields"],
          "an unoverridden MA*Y special remains magic-facing with its untyped DCL identity authored")
    check(by_id[127]["candidate_action_kind"] == "hybrid_damage" and
          by_id[127]["candidate_damage_type"] == "elemental" and
          "then_status_contest" in by_id[127]["candidate_avoidance_policy"],
          "elemental Geomancy must combine Magic Evade with a status-rider contest")
    check(by_id[248]["candidate_action_kind"] == "magic_damage" and
          by_id[248]["candidate_damage_type"] == "elemental",
          "elemental MA*Y monster breaths must classify without a live test")
    for ability_id in (76, 77, 78, 80, 83, 85):
        check(by_id[ability_id]["candidate_action_kind"] == "physical_damage" and
              by_id[ability_id]["candidate_damage_type"] == "swing" and
              by_id[ability_id]["candidate_avoidance_policy"] == "physical_contest" and
              by_id[ability_id]["metadata_basis"].startswith("job-decision:samurai"),
              f"Samurai damage Iaido {ability_id} must follow the documented physical katana-spirit route")
    check(by_id[81]["candidate_side_effect_policy"] == "one_hit_guard" and
          by_id[81]["candidate_avoidance_policy"] == "none",
          "Kiyomori must author the documented one-hit ally guard, not a hostile status contest")
    check(by_id[82]["candidate_status_category"] == "magical" and
          by_id[82]["candidate_avoidance_policy"] == "physical_contest_then_status_contest",
          "Muramasa must deliver its Slow rider through the DCL status contest after physical landing")
    check(by_id[84]["candidate_side_effect_policy"] == "status_and_ct_transaction",
          "Masamune must expose its Regen plus CT transaction")
    check(by_id[84]["metadata_blocking_scope"] == "closed",
          "job-backed Samurai candidates must be technically complete while remaining unapproved")
    check(by_id[31]["metadata_blocking_scope"] == "design",
          "Flare's untyped identity is a design authoring gate, not missing engine research")
    check(by_id[86]["metadata_blocking_scope"] == "design",
          "Song native cadence is preserved; remaining metadata work is authored policy")
    check(by_id[110]["readiness"] == "native-special-preserved" and
          by_id[110]["metadata_blocking_scope"] == "design",
          "equipment Steal must preserve native transfer and leave only authored chance policy")
    check(by_id[115]["readiness"] == "surface-ready" and
          by_id[115]["metadata_blocking_scope"] == "design",
          "EXP Steal must use its mapped paired-delta surface")
    check(by_id[45]["route"] == "drain" and
          by_id[45]["mechanism"] == "dcl_hp_mp_channels" and
          by_id[45]["metadata_blocking_scope"] == "design",
          "formula 0x65 must reuse the mapped paired drain transaction with only policy authoring open")
    check(by_id[47]["metadata_blocking_scope"] == "design" and
          by_id[48]["metadata_blocking_scope"] == "design" and
          by_id[164]["metadata_blocking_scope"] == "design" and
          by_id[165]["metadata_blocking_scope"] == "design",
          "ordinary HP/MP drain families must be technically closed and design-gated")
    check(by_id[219]["route"] == "physical_ability_damage" and
          by_id[219]["metadata_blocking_scope"] == "design",
          "formula 0x67 must follow its direct formula-0x2D alias")
    check(by_id[101]["candidate_side_effect_policy"] == "managed_multistrike",
          "Pummel must preserve managed physical-multistrike ownership in candidate metadata")
    check(by_id[358]["mechanism"] == "dcl_physical_multistrike" and
          by_id[358]["candidate_side_effect_policy"] == "native_multistrike" and
          by_id[358]["metadata_blocking_scope"] == "design",
          "Barrage must expose its native four-repeat weapon transaction without managed aggregation")
    check(by_id[349]["route"] == "magic_damage" and
          by_id[349]["mechanism"] == "explicit_damage_rule" and
          by_id[349]["candidate_avoidance_policy"] == "magic_evade_per_target" and
          by_id[349]["metadata_blocking_scope"] == "design",
          "Nanoflare is a single-hit MA action, not a multistrike mechanism")
    check(by_id[65]["mechanism"] == "native_team_barrier_pool" and
          by_id[65]["readiness"] == "native-pool-mapped" and
          by_id[65]["metadata_blocking_scope"] == "design",
          "Golem must reuse the mapped native team pool and leave only authored activation policy")
    check(by_id[329]["route"] == "unit_transformation" and
          by_id[329]["mechanism"] == "preserve_native_transformation" and
          by_id[329]["metadata_blocking_scope"] == "closed",
          "Malboro Spores must preserve its native transformation transaction, not route as status")
    check(by_id[510]["readiness"] == "reserved-inert" and
          by_id[511]["readiness"] == "reserved-inert",
          "blank terminal sentinels must not remain in the technical investigation queue")
    check(by_id[138]["readiness"] == "native-special-mapped" and
          by_id[138]["mechanism"] == "equipment_break_repoint_or_suppress" and
          by_id[138]["metadata_blocking_scope"] == "design",
          "Rend native break/nested-Attack carriers are mapped; only reversible DCL policy authoring remains")
    check(by_id[160]["readiness"] == "native-special-mapped" and
          by_id[160]["candidate_side_effect_policy"] == "reversible_control_authored_open" and
          by_id[160]["metadata_blocking_scope"] == "design",
          "Crush must expose suppression/repointing of permanent equipment loss as a design choice")
    check(by_id[143]["mechanism"] == "dcl_status_or_stat_control" and
          by_id[143]["metadata_blocking_scope"] == "design",
          "Rend stat channels are mapped; temporary/permanent policy is a design choice")
    check(by_id[117]["mechanism"] == "dcl_status_or_trait_control" and
          by_id[117]["metadata_blocking_scope"] == "design",
          "Talk Brave/Faith carriers are mapped; campaign-safe policy is a design choice")
    check(by_id[351]["mechanism"] == "paired_target_caster_hp_results" and
          by_id[351]["readiness"] == "surface-mapped" and
          by_id[351]["metadata_blocking_scope"] == "design",
          "recoil abilities must use the mapped paired result records and native HP/KO lifecycle")
    check(by_id[155]["route"] == "physical_ability_damage", "Judgment Blade needs physical ability authoring")
    check("damage_type" in by_id[155]["metadata_open_fields"],
          "physical abilities must not inherit a fabricated weapon damage type")
    check(by_id[368]["route"] == "item_command", "Potion must remain externally dispatched")
    check(by_id[382]["route"] == "command_meta", "Throw Shuriken is command metadata, not a blank unknown")
    check(by_id[314]["mechanism"] == "preserve_native_bequeath" and
          by_id[314]["readiness"] == "native-special-preserved",
          "Bequeath Bacon must preserve native level/Crystal lifecycle")

    blank = [row for row in rows if not row["formula_hex"]]
    check(len(blank) == 144, f"expected 144 blank-formula metadata/passive rows, got {len(blank)}")
    reserved = {int(row["ability_id"]) for row in blank if row["route"] == "reserved_or_unknown"}
    check(reserved == {510, 511}, f"only terminal unknown records 510/511 may remain reserved, got {sorted(reserved)}")

    observed_formulas = {row["formula_hex"] for row in rows if row["formula_hex"]}
    check(observed_formulas <= report.FORMULAS.keys(), "observed formula lacks explicit mapping")
    readiness = Counter(row["readiness"] for row in rows)
    blocking_scope = Counter(row["metadata_blocking_scope"] for row in rows)
    check(readiness["wired"] == 46, f"expected 46 wired Basic Attack/magic/heal records, got {readiness['wired']}")
    check(readiness["reverse-engineering"] == 0, "offline formula/record reverse-engineering queue must be empty")
    check(blocking_scope == Counter({"closed": 332, "design": 180}),
          f"expected every mapped multistrike to leave only design authoring open, got {dict(blocking_scope)}")
    check(sum(blocking_scope.values()) == 512 and blocking_scope["technical"] == 0 and blocking_scope["mixed"] == 0,
          "every ability must have an explicit scope and no record may remain technically unclassified")

    fire_overlay = report.overlay_row(by_id[16])
    check(fire_overlay["approved"] == 0 and fire_overlay["action_kind"] == "magic_damage" and
          fire_overlay["damage_type"] == "elemental" and
          fire_overlay["avoidance_policy"] == "magic_evade_per_target" and
          fire_overlay["strike_count"] == "",
          "template must carry inferred Fire metadata but never pre-approve it")
    judgment_overlay = report.overlay_row(by_id[155])
    check(judgment_overlay["action_kind"] == "physical_damage" and judgment_overlay["damage_type"] == "",
          "template must leave Judgment Blade's unresolved physical type blank")
    print("DCL ability classification smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
