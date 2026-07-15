#!/usr/bin/env python3
"""Build an explicit DCL routing manifest for all 512 ability records.

The manifest classifies what each record *is allowed* to enter. It deliberately does not invent
final balance: special formulas stay on an explicit-review route until an authored DCL rule exists.
"""
from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"


@dataclass(frozen=True)
class FormulaClass:
    route: str
    mechanism: str
    readiness: str
    risk: str
    rationale: str


@dataclass(frozen=True)
class MetadataCandidate:
    """Conservative DCL metadata inferred from the route and immutable catalog facts.

    Values ending in ``_open`` are deliberate authoring gates, not fallback behavior.
    """

    action_kind: str
    damage_type: str
    avoidance_policy: str
    status_category: str
    side_effect_policy: str
    certainty: str
    open_fields: str
    basis: str


def fc(route: str, mechanism: str, readiness: str, risk: str, rationale: str) -> FormulaClass:
    return FormulaClass(route, mechanism, readiness, risk, rationale)


FORMULAS: dict[str, FormulaClass] = {}


def add(ids: str, entry: FormulaClass) -> None:
    for formula_id in ids.split():
        if formula_id in FORMULAS:
            raise RuntimeError(f"duplicate formula classification {formula_id}")
        FORMULAS[formula_id] = entry


add("0x00 0x01", fc(
    "physical_damage", "dcl_physical", "formula-map-required", "medium",
    "Weapon-basis damage can use the DCL physical spine after its weapon/ability metadata is authored."))
add("0x08", fc(
    "magic_damage", "dcl_magic", "wired", "low",
    "Canonical Faith-scaled MA*Y damage is implemented by the numeric-magic profile."))
add("0x09 0x0E 0x17 0x45 0x53", fc(
    "special_hp_damage", "explicit_special_rule", "authoring-required", "high",
    "Percent/current/missing-HP effects must not enter the ordinary physical or magic damage spine."))
add("0x0A", fc(
    "magical_status", "dcl_status_contest", "surface-ready", "medium",
    "Offensive Faith status spells use the authored DCL status contest, not numeric magic damage."))
add("0x0B 0x22 0x38 0x3D 0x41 0x51", fc(
    "status_or_buff", "dcl_status_control", "surface-ready", "medium",
    "Status/buff output is writable, but each status needs authored offense/resistance/duration policy."))
add("0x0C", fc(
    "magic_healing", "dcl_magic", "wired", "low",
    "Canonical Faith-scaled MA*Y healing is implemented, including Undead inversion."))
add("0x0D 0x35", fc(
    "revive_or_percent_heal", "explicit_healing_rule", "authoring-required", "high",
    "Raise/revive percent healing has KO and hit semantics outside ordinary healing."))
add("0x0F 0x10 0x2F 0x30 0x47 0x4D", fc(
    "drain", "dcl_hp_mp_channels", "authoring-required", "high",
    "Paired target/source HP and MP result records, Undead reversal, the 999 result cap, and native resource clamps are mapped; each action still needs authored amount, hit, element, and reversal policy."))
add("0x12 0x15", fc(
    "turn_control", "explicit_status_or_ct_rule", "authoring-required", "high",
    "Quick/CT-zero modifies turn state rather than HP damage."))
add("0x14", fc(
    "golem_pool", "native_team_barrier_pool", "native-pool-mapped", "high",
    "Native Golem owns four team-indexed 16-bit pools, initializes from caster MaxHP, saturating-debits intercepted HP damage, and participates in battle-state import/export."))
add("0x16 0x1B 0x21 0x2C 0x44", fc(
    "mp_damage", "dcl_mp_channel", "surface-ready", "medium",
    "MP debit is proven, but each percent/current/stat formula needs an authored amount rule."))
add("0x1A 0x2B 0x55 0x56 0x59 0x61 0x62", fc(
    "stat_or_trait_debuff", "dcl_status_or_stat_control", "authoring-required", "medium",
    "Direct PA/MA/SP/level/Brave changes require explicit duration, floor, and restoration policy."))
add("0x1C 0x1D", fc(
    "song_or_dance", "periodic_action_state", "native-periodic-preserved", "medium",
    "Native Performing state retains exact action identity, schedules ordinary result application, and owns cleanup; DCL can rewrite each staged tick without replacing cadence."))
add("0x1E 0x1F 0x5E", fc(
    "multihit_magic", "dcl_magic_multistrike", "authoring-required", "high",
    "RandomFire actions need managed per-strike Magic Evade, amount, status, target, and cache consumption policy; the shared MA handler itself produces one result."))
add("0x5F", fc(
    "magic_damage", "explicit_damage_rule", "formula-map-required", "medium",
    "Nanoflare is a single-hit MA action with RandomFire clear; it needs authored amount, type, and ordinary per-target Magic Evade policy, not a multistrike carrier."))
add("0x20 0x24 0x4E", fc(
    "noncanonical_magic_or_hybrid_damage", "explicit_damage_rule", "formula-map-required", "high",
    "Non-Faith MA or PA/MA hybrid damage needs an ability-level DCL identity decision."))
add("0x23 0x4C", fc(
    "noncanonical_healing", "explicit_healing_rule", "formula-map-required", "medium",
    "Non-Faith healing may share the output channel but needs an authored Faith and scaling policy."))
add("0x25", fc(
    "equipment_break", "equipment_break_repoint_or_suppress", "native-special-mapped", "high",
    "Rend stages the selected item/slot and native break bit, but falls back through a nested synthetic Attack; DCL replaces permanent loss by repointing or suppressing the break carrier."))
add("0x2E", fc(
    "equipment_break", "equipment_break_repoint_or_suppress", "native-special-mapped", "high",
    "Crush stages the shared native break bit plus ordinary physical damage; DCL can suppress the break carrier or repoint the action while authoring reversible control."))
add("0x26", fc(
    "steal", "inventory_side_effect", "native-special-preserved", "high",
    "Native exact-id slot selection stages the equipped item and slot mask; DCL preserves the permanent transfer and authors only eligibility/chance."))
add("0x27", fc(
    "steal", "inventory_side_effect", "native-special-preserved", "high",
    "Native Gil transfer stages a symmetric signed value pair and commits through the existing campaign-value bridge; DCL preserves the transaction."))
add("0x28", fc(
    "steal", "inventory_side_effect", "surface-ready", "high",
    "Native EXP transfer stages paired source/target deltas and the ordinary state-apply path owns the bounded EXP update."))
add("0x29", fc(
    "gender_gated_status", "dcl_status_contest", "authoring-required", "medium",
    "Gender-gated Charm is a status contest with a special eligibility gate."))
add("0x2A", fc(
    "talk_trait_or_status", "dcl_status_or_trait_control", "authoring-required", "high",
    "Talk actions mix status and permanent Brave/Faith changes and need campaign-safe policy."))
add("0x2D 0x31 0x37", fc(
    "physical_ability_damage", "dcl_physical", "formula-map-required", "medium",
    "Sword/monk/knockback damage can use the physical spine after type, wmod, reach, and rider authoring."))
add("0x32", fc(
    "multihit_physical", "dcl_physical_multistrike", "authoring-required", "high",
    "Native Pummel multiplies one staged HP debit by Rdm(1..X); DCL needs a managed strike loop for per-strike hit and Guard depletion plus an authored once-per-action reaction policy."))
add("0x33 0x3F 0x40 0x50", fc(
    "physical_status", "dcl_status_contest", "surface-ready", "medium",
    "Physical status skills route to the DCL status contest with authored base-HP or other resistance."))
add("0x34", fc(
    "hp_mp_healing", "dcl_hp_mp_channels", "surface-ready", "medium",
    "Dual HP/MP restoration channels are proven; Chakra needs an authored non-Faith amount rule."))
add("0x36 0x39 0x3A 0x3B", fc(
    "stat_or_trait_buff", "dcl_status_or_stat_control", "authoring-required", "medium",
    "Direct PA/MA/SP/Brave buffs need duration, cap, stacking, and campaign policy."))
add("0x3C", fc(
    "sacrifice_heal", "dcl_hp_channels", "authoring-required", "high",
    "Caster self-damage and target healing must commit atomically across two units."))
add("0x42", fc(
    "recoil_physical_damage", "paired_target_caster_hp_results", "surface-mapped", "high",
    "Formula 0x42 stages target and caster HP debits in the paired result records at qword globals 0x14186AF70/0x14186AF60; DCL can author both before native clamp/KO apply."))
add("0x43 0x4F 0x52", fc(
    "missing_hp_or_self_destruct", "explicit_special_rule", "authoring-required", "high",
    "Missing-HP and self-destruct damage do not use ordinary PA/MA scaling and may affect the caster."))
add("0x54", fc(
    "mp_healing", "dcl_mp_channel", "surface-ready", "medium",
    "MP credit is proven; the amount and target policy remain authored."))
add("0x57", fc(
    "level_gain", "preserve_native_bequeath", "native-special-preserved", "high",
    "Formula 0x57 owns the bounded level gain and caster Crystal lifecycle; DCL preserves the native special unchanged."))
add("0x58", fc(
    "unit_transformation", "preserve_native_transformation", "native-special-preserved", "high",
    "Malboro Spores stages a dedicated native transformation bit with generic-unit eligibility and status/effect reset; it is not an InflictStatus bundle."))
add("0x5A 0x5B 0x5C 0x5D", fc(
    "dragon_gated_support", "species_gate_plus_special", "data-or-authoring-required", "high",
    "Dragon Check is species-gated; use the proven data bypass or an explicit replacement formula."))
add("0x65 0x66", fc(
    "drain", "dcl_hp_mp_channels", "authoring-required", "high",
    "Dark Knight drain variants scale the ordinary staged debit to 80% and reuse the mapped native paired HP/MP result transactions."))
add("0x67", fc(
    "physical_ability_damage", "dcl_physical", "formula-map-required", "medium",
    "Crushing Blow is a direct wrapper alias of formula 0x2D and can use the physical spine plus an authored status rider."))
add("0x68 0x69", fc(
    "noncanonical_magic_or_hybrid_damage", "explicit_damage_rule", "formula-map-required", "high",
    "The Dark Knight handlers expose explicit distance- or MaxHP-derived power before reusing ordinary staged damage/status output."))
add("0x6A", fc(
    "multihit_physical", "dcl_physical_multistrike", "authoring-required", "high",
    "Barrage dynamically delegates each strike to the equipped weapon formula and the native normal-attack postprocessor."))


PASSIVE_TYPES = {"Movement", "Reaction", "Support"}
META_TYPES = {"Arithmetick", "Charging", "Jumping", "Throwing"}

ABILITY_OVERRIDES: dict[int, FormulaClass] = {
    0: fc(
        "basic_attack", "dcl_physical_or_magic_bolt", "wired", "medium",
        "Runtime action id 0 is Basic Attack despite the catalog's <Nothing>/0x0D sentinel row; weapon family selects physical or Rod/Staff bolt routing."),
    510: fc(
        "reserved_or_unknown", "no_formula", "reserved-inert", "low",
        "Blank sentinel record: no formula or action payload, no raw action record, and not enemy-used."),
    511: fc(
        "reserved_or_unknown", "no_formula", "reserved-inert", "low",
        "Blank sentinel record: no formula or action payload, no raw action record, and not enemy-used."),
}


PHYSICAL_DAMAGE_ROUTES = {
    "basic_attack", "physical_damage", "physical_ability_damage", "multihit_physical",
    "recoil_physical_damage",
}
MAGIC_DAMAGE_ROUTES = {"magic_damage", "multihit_magic"}
HEALING_ROUTES = {
    "magic_healing", "noncanonical_healing", "revive_or_percent_heal", "hp_mp_healing",
    "mp_healing", "sacrifice_heal",
}
STATUS_ROUTES = {
    "magical_status", "physical_status", "gender_gated_status", "status_or_buff",
    "talk_trait_or_status",
}

MENTAL_STATUSES = {"berserk", "charm", "confusion", "fear", "invite"}
PHYSICAL_STATUSES = {"bloodsuck", "disease", "oil", "poison"}
MAGICAL_STATUSES = {
    "darkness", "deathsentence", "faith", "frog", "innocent", "petrify", "silence",
    "sleep", "slow", "stop", "undead",
}
BENEFICIAL_STATUSES = {
    "float", "haste", "protect", "reflect", "regen", "reraise", "shell", "transparent",
}


def mc(
    action_kind: str,
    damage_type: str,
    avoidance_policy: str,
    status_category: str,
    side_effect_policy: str,
    basis: str,
) -> MetadataCandidate:
    return MetadataCandidate(
        action_kind, damage_type, avoidance_policy, status_category, side_effect_policy,
        "candidate-complete", "", basis,
    )


# DCL-facing identity can deliberately replace the native formula's family. These overrides are
# candidates backed by an owning job decision, but the emitted authoring template still leaves
# approved=0 until the full job/human gate is satisfied.
ABILITY_METADATA_OVERRIDES: dict[int, MetadataCandidate] = {
    76: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
    77: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
    78: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
    79: mc("healing", "none", "none", "none",
           "managed_resource_commit", "job-decision:samurai-support-iaido"),
    80: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
    81: mc("special_control", "none", "none", "none",
           "one_hit_guard", "job-decision:samurai-support-iaido"),
    82: mc("physical_damage", "swing", "physical_contest_then_status_contest", "magical",
           "managed_status_rider", "job-decision:samurai-curse-iaido"),
    83: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
    84: mc("status_control", "none", "none", "none",
           "status_and_ct_transaction", "job-decision:samurai-support-iaido"),
    85: mc("physical_damage", "swing", "physical_contest", "none",
           "none_or_catalog_visuals", "job-decision:samurai-damage-iaido"),
}


# These families need more than a per-ability policy choice before they can ship. ``mixed`` means
# the identity/amount policy is also open; ``technical`` means native ownership must be mapped first.
MIXED_MECHANISM_GAPS = {
    "dcl_hp_channels",
}


def metadata_blocking_scope(entry: FormulaClass, metadata: MetadataCandidate) -> tuple[str, str]:
    """Separate missing design decisions from missing native/runtime mechanisms."""
    if metadata.certainty == "candidate-complete":
        return "closed", "candidate metadata complete; approval remains a separate job/human gate"
    if entry.readiness == "reverse-engineering":
        return "technical", "map native ownership and commit/cadence behavior before authoring metadata"
    if entry.mechanism in MIXED_MECHANISM_GAPS:
        return "mixed", "finish the runtime transaction/state mechanism and author the per-ability policy"
    return "design", "author the open per-ability identity/policy on an already known or preserved surface"


def normalized_tokens(text: str) -> set[str]:
    return {
        part.strip().lower().replace(" ", "")
        for part in text.replace(",", "|").split("|")
        if part.strip()
    }


def candidate_status_category(row: dict[str, str], route: str) -> tuple[str, bool]:
    """Return (category, unresolved).

    Don't Act/Move are intentionally source-sensitive in the DCL. Leg/Arm Shot are the only
    catalog records whose physical reskin is already explicit; every other use remains open.
    """
    if row.get("inflict_status_mode", "").strip().lower() == "cancel":
        return "none", False

    ability_id = int(row.get("id_dec", "-1"))
    statuses = normalized_tokens(row.get("inflict_statuses", ""))
    if not statuses:
        return ("none", False) if route not in STATUS_ROUTES else ("ability_authored_open", True)
    if statuses <= BENEFICIAL_STATUSES:
        return "none", False
    if ability_id in {213, 214}:
        return "physical", False

    categories: set[str] = set()
    if statuses & MENTAL_STATUSES:
        categories.add("mental")
    if statuses & PHYSICAL_STATUSES:
        categories.add("physical")
    if statuses & MAGICAL_STATUSES:
        categories.add("magical")
    if statuses & {"dontact", "dontmove"}:
        categories.add("source_authored")
    if statuses & {"dead", "crystal"}:
        categories.add("lifecycle_special")

    unknown = statuses - (
        MENTAL_STATUSES | PHYSICAL_STATUSES | MAGICAL_STATUSES | BENEFICIAL_STATUSES |
        {"dontact", "dontmove", "dead", "crystal"}
    )
    if unknown:
        categories.add("authoring_open")
    if not categories:
        return "ability_authored_open", True
    if len(categories) == 1:
        category = next(iter(categories))
        return category, category.endswith("open") or category in {"source_authored", "lifecycle_special"}
    return "+".join(sorted(categories)), True


def infer_metadata(row: dict[str, str], entry: FormulaClass) -> MetadataCandidate:
    ability_id = int(row.get("id_dec", "-1"))
    if ability_id in ABILITY_METADATA_OVERRIDES:
        return ABILITY_METADATA_OVERRIDES[ability_id]

    route = entry.route
    formula = row.get("formula_hex", "").strip()
    mode = row.get("inflict_status_mode", "").strip().lower()
    elements = normalized_tokens(row.get("elements", ""))
    statuses = normalized_tokens(row.get("inflict_statuses", ""))
    hostile_status_rider = mode != "cancel" and bool(statuses - BENEFICIAL_STATUSES)
    native_repeat = row.get("RandomFire", "").strip().lower() in {"1", "true", "yes"} or formula == "0x6A"
    open_fields: list[str] = []

    if route in PHYSICAL_DAMAGE_ROUTES:
        action_kind = "physical_damage"
    elif route in MAGIC_DAMAGE_ROUTES or (route == "noncanonical_magic_or_hybrid_damage" and formula in {"0x20", "0x4E"}):
        action_kind = "magic_damage"
    elif route == "noncanonical_magic_or_hybrid_damage" and formula == "0x24":
        action_kind = "hybrid_damage"
    elif route in HEALING_ROUTES:
        action_kind = "healing"
    elif route in STATUS_ROUTES:
        action_kind = "status_control"
    elif route in {"mp_damage", "drain", "special_hp_damage", "missing_hp_or_self_destruct"}:
        action_kind = "resource_damage"
    elif route in {"stat_or_trait_buff", "stat_or_trait_debuff"}:
        action_kind = "stat_or_trait_change"
    elif route in {"passive"}:
        action_kind = "passive"
    elif route in {"item_command"}:
        action_kind = "item_dispatch"
    elif route in {"command_meta"}:
        action_kind = "command_dispatch"
    elif route in {"equipment_break", "steal", "turn_control", "level_gain", "golem_pool", "unit_transformation"}:
        action_kind = "special_control"
    else:
        action_kind = "special_or_hybrid_open"
        open_fields.append("action_kind")

    if route in {"basic_attack", "physical_damage"}:
        damage_type = "weapon_defined"
    elif route in {"physical_ability_damage", "multihit_physical", "recoil_physical_damage"}:
        damage_type = "ability_physical_open"
        open_fields.append("damage_type")
    elif route in MAGIC_DAMAGE_ROUTES or (route == "noncanonical_magic_or_hybrid_damage" and formula in {"0x20", "0x4E"}):
        if elements & {"holy", "dark"} and not elements - {"holy", "dark"}:
            damage_type = "spiritual"
        elif elements:
            damage_type = "elemental"
        else:
            damage_type = "magic_untyped_open"
            open_fields.append("damage_type")
    elif route == "noncanonical_magic_or_hybrid_damage" and formula == "0x24":
        if elements:
            damage_type = "elemental"
        else:
            damage_type = "hybrid_untyped_open"
            open_fields.append("damage_type")
    elif route in {"noncanonical_magic_or_hybrid_damage", "special_hp_damage", "missing_hp_or_self_destruct"}:
        damage_type = "special_or_hybrid_open"
        open_fields.append("damage_type")
    else:
        damage_type = "none"

    if route in PHYSICAL_DAMAGE_ROUTES:
        avoidance_policy = (
            "physical_contest_then_status_contest" if hostile_status_rider else "physical_contest"
        )
    elif route in MAGIC_DAMAGE_ROUTES or route == "noncanonical_magic_or_hybrid_damage":
        # The DCL owns Magic Evade at spell level, once per final target.  RandomFire may expose
        # several native apply events, but that carrier cardinality does not change the authored
        # avoidance rule.  Per-strike avoidance remains a supported explicit override, not the
        # conservative classification default.
        base_avoidance = "magic_evade_per_target"
        avoidance_policy = (
            f"{base_avoidance}_then_status_contest" if hostile_status_rider else base_avoidance
        )
    elif route in STATUS_ROUTES:
        avoidance_policy = "none" if mode == "cancel" else "status_contest"
    elif route in HEALING_ROUTES or route in {"passive", "command_meta", "item_command"}:
        avoidance_policy = "none"
    elif route == "unit_transformation":
        avoidance_policy = "native_formula_hit"
    else:
        avoidance_policy = "ability_authored_open"
        open_fields.append("avoidance_policy")

    status_category, status_open = candidate_status_category(row, route)
    if status_open:
        open_fields.append("status_category")

    if route in PHYSICAL_DAMAGE_ROUTES | MAGIC_DAMAGE_ROUTES | {"noncanonical_magic_or_hybrid_damage"}:
        if route in {"multihit_physical", "multihit_magic"} and native_repeat and hostile_status_rider:
            side_effect_policy = "native_multistrike_status_rider"
        elif route in {"multihit_physical", "multihit_magic"} and native_repeat:
            side_effect_policy = "native_multistrike"
        elif route in {"multihit_physical", "multihit_magic"} and hostile_status_rider:
            side_effect_policy = "managed_multistrike_status_rider"
        elif hostile_status_rider:
            side_effect_policy = "managed_status_rider"
        elif route in {"multihit_physical", "multihit_magic"}:
            side_effect_policy = "managed_multistrike"
        else:
            side_effect_policy = "none_or_catalog_visuals"
    elif route in STATUS_ROUTES:
        side_effect_policy = "managed_status_commit"
    elif route == "equipment_break":
        side_effect_policy = "reversible_control_authored_open"
        open_fields.append("side_effect_policy")
    elif route == "steal":
        side_effect_policy = "inventory_transaction"
    elif route in {"drain", "sacrifice_heal", "recoil_physical_damage"}:
        side_effect_policy = "multi_unit_transaction"
    elif route in {"stat_or_trait_buff", "stat_or_trait_debuff"}:
        side_effect_policy = "stat_or_trait_commit"
    elif route == "level_gain":
        side_effect_policy = "preserve_native_special"
    elif route == "golem_pool":
        side_effect_policy = "native_team_pool_transaction"
    elif route == "unit_transformation":
        side_effect_policy = "preserve_native_transformation"
    elif route in {"passive", "command_meta", "item_command"}:
        side_effect_policy = "external_or_data_dispatch"
    elif route in HEALING_ROUTES | {"mp_damage"}:
        side_effect_policy = "managed_resource_commit"
    else:
        side_effect_policy = "special_handler_open"
        open_fields.append("side_effect_policy")

    open_fields = list(dict.fromkeys(open_fields))
    certainty = "candidate-complete" if not open_fields else "authoring-open"
    return MetadataCandidate(
        action_kind, damage_type, avoidance_policy, status_category, side_effect_policy,
        certainty, "|".join(open_fields), "formula-and-catalog",
    )


def classify_blank(row: dict[str, str]) -> FormulaClass:
    ability_type = row.get("ability_type", "").strip()
    if ability_type in PASSIVE_TYPES:
        return fc("passive", "data_ability", "data-authoring", "low",
                  "Passive ability has no action Formula; behavior is equipped/innate data or hardcoded support logic.")
    if ability_type == "Item":
        return fc("item_command", "item_external_dispatch", "formula-map-required", "high",
                  "Item command records dispatch through item/Z/inventory data rather than this ability Formula field.")
    if ability_type in META_TYPES:
        return fc("command_meta", "hardcoded_command_dispatch", "data-authoring", "medium",
                  "Throw/Jump/Charge/Arithmetick record is command metadata; executed action math lives elsewhere.")
    return fc("reserved_or_unknown", "no_formula", "reverse-engineering", "high",
              "Blank Formula with no recognized passive/command type requires record-specific inspection.")


def load_manifest(path: Path) -> tuple[list[dict[str, str]], list[str]]:
    rows: list[dict[str, str]] = []
    errors: list[str] = []
    with path.open(newline="", encoding="utf-8-sig") as handle:
        source = list(csv.DictReader(handle))

    for row in source:
        formula = row.get("formula_hex", "").strip()
        ability_id = int(row.get("id_dec", "-1"))
        entry = ABILITY_OVERRIDES.get(ability_id)
        if entry is None:
            entry = classify_blank(row) if not formula else FORMULAS.get(formula)
        if entry is None:
            errors.append(f"ability {row.get('id_dec')} uses unclassified formula {formula}")
            entry = fc("unclassified", "unknown", "reverse-engineering", "high", "No formula classification exists.")
        metadata = infer_metadata(row, entry)
        blocking_scope, next_gate = metadata_blocking_scope(entry, metadata)
        rows.append({
            "ability_id": row.get("id_dec", ""),
            "name": row.get("name_ivc") or row.get("name_wotl") or "",
            "ability_type": row.get("ability_type", ""),
            "formula_hex": formula,
            "formula_text": row.get("formula_text", ""),
            "elements": row.get("elements", ""),
            "inflict_statuses": row.get("inflict_statuses", ""),
            "route": entry.route,
            "mechanism": entry.mechanism,
            "readiness": entry.readiness,
            "risk": entry.risk,
            "candidate_action_kind": metadata.action_kind,
            "candidate_damage_type": metadata.damage_type,
            "candidate_avoidance_policy": metadata.avoidance_policy,
            "candidate_status_category": metadata.status_category,
            "candidate_side_effect_policy": metadata.side_effect_policy,
            "metadata_certainty": metadata.certainty,
            "metadata_open_fields": metadata.open_fields,
            "metadata_basis": metadata.basis,
            "metadata_blocking_scope": blocking_scope,
            "metadata_next_gate": next_gate,
            "rationale": entry.rationale,
        })

    ids = [row["ability_id"] for row in rows]
    if len(rows) != 512:
        errors.append(f"expected 512 ability rows, found {len(rows)}")
    if len(set(ids)) != len(ids):
        errors.append("ability ids are not unique")
    return rows, errors


CSV_FIELDS = [
    "ability_id", "name", "ability_type", "formula_hex", "formula_text", "elements",
    "inflict_statuses", "route", "mechanism", "readiness", "risk",
    "candidate_action_kind", "candidate_damage_type", "candidate_avoidance_policy",
    "candidate_status_category", "candidate_side_effect_policy", "metadata_certainty",
    "metadata_open_fields", "metadata_basis", "rationale",
    "metadata_blocking_scope", "metadata_next_gate",
]

OVERLAY_FIELDS = [
    "ability_id", "approved", "action_kind", "damage_type", "avoidance_policy",
    "status_category", "side_effect_policy", "power", "strike_count",
]


def write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=CSV_FIELDS)
        writer.writeheader()
        writer.writerows(rows)


def overlay_row(row: dict[str, str]) -> dict[str, str | int]:
    open_fields = set(filter(None, row["metadata_open_fields"].split("|")))
    return {
        "ability_id": row["ability_id"],
        "approved": 0,
        "action_kind": "" if "action_kind" in open_fields else row["candidate_action_kind"],
        "damage_type": "" if "damage_type" in open_fields else row["candidate_damage_type"],
        "avoidance_policy": "" if "avoidance_policy" in open_fields else row["candidate_avoidance_policy"],
        "status_category": "" if "status_category" in open_fields else row["candidate_status_category"],
        "side_effect_policy": "" if "side_effect_policy" in open_fields else row["candidate_side_effect_policy"],
        "power": "",
        "strike_count": "",
    }


def write_overlay_template(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=OVERLAY_FIELDS)
        writer.writeheader()
        writer.writerows(overlay_row(row) for row in rows)


def render_markdown(rows: list[dict[str, str]], catalog: Path, csv_path: Path | None) -> str:
    route_counts = Counter(row["route"] for row in rows)
    readiness_counts = Counter(row["readiness"] for row in rows)
    metadata_counts = Counter(row["metadata_certainty"] for row in rows)
    blocking_scope_counts = Counter(row["metadata_blocking_scope"] for row in rows)
    open_field_counts = Counter(
        field
        for row in rows
        for field in row["metadata_open_fields"].split("|")
        if field
    )
    formula_groups: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        formula_groups[row["formula_hex"] or "(blank)"].append(row)

    lines = [
        "# DCL ability classification manifest",
        "",
        f"Source: `{catalog.as_posix()}` (`{len(rows)}` records).",
    ]
    if csv_path:
        lines.append(f"Row manifest: `{csv_path.as_posix()}`.")
    lines.extend([
        "",
        "This report classifies routing and implementation readiness, not final balance. A route marked",
        "`authoring-required` or `reverse-engineering` must preserve vanilla until its explicit DCL rule exists.",
        "The `candidate_*` columns are conservative technical candidates. `authoring-open` rows are not",
        "runtime defaults and must not be promoted without an explicit per-ability decision.",
        "",
        "## Readiness",
        "",
        "| Readiness | Abilities |",
        "| --- | ---: |",
    ])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(readiness_counts.items()))
    lines.extend(["", "## Metadata candidate coverage", "", "| Certainty | Abilities |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(metadata_counts.items()))
    lines.extend(["", "| Blocking scope | Abilities |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(blocking_scope_counts.items()))
    lines.extend(["", "| Open field | Abilities |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(open_field_counts.items()))
    lines.extend(["", "## Routes", "", "| Route | Abilities |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(route_counts.items()))
    lines.extend([
        "",
        "## Formula groups",
        "",
        "| Formula | Count | Route | Readiness | Examples |",
        "| --- | ---: | --- | --- | --- |",
    ])
    for formula, group in sorted(formula_groups.items(), key=lambda item: (item[0] == "(blank)", item[0])):
        routes = ", ".join(sorted({row["route"] for row in group}))
        readiness = ", ".join(sorted({row["readiness"] for row in group}))
        examples = "; ".join(f"{row['ability_id']}:{row['name'] or '(unnamed)'}" for row in group[:4])
        lines.append(f"| {formula} | {len(group)} | {routes} | {readiness} | {examples} |")

    high_risk = [row for row in rows if row["risk"] == "high"]
    lines.extend([
        "",
        "## Gate summary",
        "",
        f"- Wired now: `{readiness_counts['wired']}` ability records.",
        f"- Formula/status/channel surface ready: `{readiness_counts['surface-ready']}` records.",
        f"- Ability/formula/data authoring still required: `{readiness_counts['authoring-required'] + readiness_counts['formula-map-required'] + readiness_counts['data-or-authoring-required']}` records.",
        f"- Reverse engineering still required: `{readiness_counts['reverse-engineering']}` records.",
        f"- Metadata blocked only by design authoring: `{blocking_scope_counts['design']}` records.",
        f"- Metadata blocked by technical investigation: `{blocking_scope_counts['technical']}` records.",
        f"- Metadata blocked by both mechanism and design: `{blocking_scope_counts['mixed']}` records.",
        f"- High-risk special or externally dispatched records: `{len(high_risk)}`.",
        "- `unclassified` must remain zero; the tool fails when a new nonblank Formula lacks a route.",
        "",
    ])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--csv", type=Path, help="Write the 512-row CSV manifest.")
    parser.add_argument("--markdown", type=Path, help="Write a Markdown summary.")
    parser.add_argument("--overlay-template", type=Path,
                        help="Write an approval-gated DclAbilityMetadataPath authoring template.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    rows, errors = load_manifest(args.catalog)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    if args.csv:
        write_csv(args.csv, rows)
        print(f"wrote {args.csv}")
    if args.overlay_template:
        write_overlay_template(args.overlay_template, rows)
        print(f"wrote {args.overlay_template}")
    report = render_markdown(rows, args.catalog, args.csv)
    if args.markdown:
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        args.markdown.write_text(report, encoding="utf-8")
        print(f"wrote {args.markdown}")
    elif not args.csv and not args.overlay_template:
        print(report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
