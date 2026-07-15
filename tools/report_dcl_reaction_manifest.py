#!/usr/bin/env python3
"""Build the native and final-job DCL reaction implementation manifest.

The report deliberately separates the exact chance surface from trigger, effect, cadence, and
content readiness. A reaction is not called implemented merely because its Brave curve is writable.
"""
from __future__ import annotations

import argparse
import csv
import time
from collections import Counter
from dataclasses import asdict, dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"


@dataclass(frozen=True)
class NativeReaction:
    ability_id: int
    expected_name: str
    category: str
    category_confidence: str
    evaluation_route: str
    disposition: str
    note: str


@dataclass(frozen=True)
class FinalReaction:
    job: str
    design_name: str
    native_id: int | None
    assignment: str
    category: str
    trigger: str
    effect: str
    cadence: str
    trigger_filter: str
    effect_delivery: str
    cadence_control: str
    data_text: str
    readiness: str
    source: str


NATIVE = (
    NativeReaction(422, "Strength Surge", "neutral", "Hypothesis", "real-code-or-native-special", "candidate-repurpose", "Reactive stat utility; final category depends on retained design."),
    NativeReaction(423, "Magick Surge", "neutral", "Hypothesis", "real-code-or-native-special", "candidate-repurpose", "Reactive stat utility; final category depends on retained design."),
    NativeReaction(424, "Speed Surge", "neutral", "Hypothesis", "real-code-or-native-special", "candidate-Adrenaline-Rush", "Candidate record for the Monk's bounded temporary buff."),
    NativeReaction(425, "Vanish", "caution", "Strong", "real-code-or-native-special", "unassigned", "Defensive avoidance reaction."),
    NativeReaction(426, "Vigilance", "caution", "Strong", "stages-id-plus-action; downstream-native-Defending", "final-Vigilance", "Final Thief reaction must replace native Defending with transient pre-roll DCL Dodge."),
    NativeReaction(427, "Dragonheart", "caution", "Strong", "real-code-or-native-special", "candidate-Dragon's-Fury", "Reraise design is removed; record is a semantic candidate for Dragoon repurpose."),
    NativeReaction(428, "Regenerate", "caution", "Proven-design", "real-code-or-native-special", "final-Regenerator", "Final White Mage design explicitly uses inverse Brave."),
    NativeReaction(429, "Bravery Surge", "neutral", "Strong", "real-code-or-native-special", "unassigned", "Trait utility, not an offensive counter or avoidance reaction."),
    NativeReaction(430, "Faith Surge", "neutral", "Strong", "real-code-or-native-special", "unassigned", "Trait utility, not an offensive counter or avoidance reaction."),
    NativeReaction(431, "Critical: Recover HP", "caution", "Strong", "post-hit-real-code-full-missing-hp-credit", "candidate-Grit", "The native effect heals MaxHP-currentHP in full; a minor Grit heal must replace the credited amount."),
    NativeReaction(432, "Critical: Recover MP", "caution", "Strong", "post-hit-real-code", "unassigned", "Low-HP resource recovery."),
    NativeReaction(433, "Critical: Quick", "caution", "Strong", "post-hit-real-code", "unassigned", "Low-HP defensive tempo."),
    NativeReaction(434, "Bonecrusher", "courage", "Strong", "post-hit-real-code", "unassigned", "Aggressive retaliation."),
    NativeReaction(435, "Magick Counter", "courage", "Strong", "real-code-counter-class", "candidate-Rod-Counter", "Final effect is a basic Rod bolt, not spell-copy."),
    NativeReaction(436, "Counter Tackle", "courage", "Strong", "source-target-type0B-Rush147", "final-Counter-Tackle", "Native Rush formula 0x37 supplies the retaliatory knockback carrier."),
    NativeReaction(437, "Nature's Wrath", "courage", "Proven-design", "real-code-counter-class", "final-Nature's-Wrath", "Final job spec explicitly scales it with Brave."),
    NativeReaction(438, "Absorb MP", "neutral", "Strong", "real-code-or-native-special", "unassigned", "Resource utility."),
    NativeReaction(439, "Gil Snapper", "neutral", "Strong", "post-hit-real-code", "unassigned", "Campaign/resource utility."),
    NativeReaction(440, "MARKED FOR DELETION - REPORT IF DISPLAYED Reflect", "neutral", "Proven-data", "reserved", "reserved", "Deleted display record; do not ship unchanged."),
    NativeReaction(441, "Auto-Potion", "caution", "Strong", "post-hit-real-code", "final-Auto-Potion", "Defensive sustain; final guardrails are custom."),
    NativeReaction(442, "Counter", "courage", "Proven-design", "real-code-counter-class", "candidate-Riposte", "Generic counter record is the leading Knight Riposte candidate."),
    NativeReaction(443, "", "caution", "Strong", "generic-pass2-carrier-no-native-producer", "candidate-Hex-Ward", "Generic carrier accepts custom staging; target inversion and managed effect are required."),
    NativeReaction(444, "Cup of Life", "neutral", "Strong", "real-code-or-native-special", "unassigned", "Utility/lifecycle effect."),
    NativeReaction(445, "Mana Shield", "caution", "Proven-design", "real-code-counter-class", "final-Mana-Shield", "Final Time Mage design explicitly uses inverse Brave."),
    NativeReaction(446, "Soulbind", "caution", "Strong", "real-code-or-native-special", "unassigned", "Defensive damage response."),
    NativeReaction(447, "Parry", "caution", "Strong", "passive-evasion-route-unresolved", "unassigned", "Defensive avoidance; native route must not be confused with DCL finite Parry."),
    NativeReaction(448, "Earplugs", "caution", "Strong", "real-code-or-native-special", "final-Earplugs", "Narrow defensive counter-performance reaction."),
    NativeReaction(449, "Reflexes", "caution", "Strong", "passive-evasion-route-unresolved", "unassigned", "Defensive avoidance reaction."),
    NativeReaction(450, "Sticky Fingers", "caution", "Strong", "real-code-or-native-special", "unassigned", "Defensive anti-theft response."),
    NativeReaction(451, "Shirahadori", "caution", "Proven-live", "vm-internal-avoidance", "unassigned", "Only reaction currently proven to bypass the four real-code gates."),
    NativeReaction(452, "Archer's Bane", "caution", "Strong", "passive-evasion-route-unresolved", "candidate-Countershot", "Record may be repurposed; native effect is avoidance, not a returned shot."),
    NativeReaction(453, "First Strike", "courage", "Proven-design", "real-code-or-native-special", "unassigned", "Aggressive pre-emptive retaliation."),
)


FINAL = (
    FinalReaction("Squire", "Counter Tackle", 436, "confirmed-native-record", "courage", "Struck in melee", "Retaliatory shove", "Native/default unless final per-action gate is added", "source/action, landed hit, weapon-strike, and adjacency are formula-visible", "native pass-2 Rush 147 delivery preserves formula-0x37 knockback; DCL owns damage magnitude", "author trigger and multi-hit cadence; live knockback regression only", "rename/data authoring required", "mechanism-near-native", "docs/job-balance/jobs/01-squire.md"),
    FinalReaction("Squire", "Grit", 431, "candidate-record", "undecided", "Low-HP clutch condition", "Minor bounded clutch bonus", "Condition-gated", "low-HP threshold, payoff, amount, and cadence under-specified", "native carrier heals all missing HP; a healing design needs bounded HP-credit replacement", "carrier/timing mapped; design and managed amount replacement pending", "new name/description/data required", "design-and-mechanism-gate", "docs/job-balance/jobs/01-squire.md"),
    FinalReaction("Chemist", "Auto-Potion", 441, "confirmed-native-record", "undecided", "Post-damage and survivor only", "Use Potion-line item without Item Lore", "Once per own-turn-cycle", "hit and survivor state are formula-visible; committed-damage phase/amount is missing", "native effect requires item-line restriction", "own-cycle primitive ready; effect-commit integration pending", "item eligibility/text/data required", "mechanism-partial", "docs/job-balance/jobs/02-chemist.md"),
    FinalReaction("Knight", "Riposte", 442, "candidate-record", "courage", "Adjacent melee attack misses or is defended", "Basic melee counter", "Once per attacker action", "miss/defended, weapon-strike, and adjacency are formula-visible; a missing native window still needs a trigger producer", "counter delivery exists; exact policy integration missing", "attacker-action primitive ready; effect-commit integration pending", "rename/text/data required", "mechanism-partial", "docs/job-balance/jobs/03-knight.md"),
    FinalReaction("Archer", "Countershot", 452, "candidate-repurpose", "courage", "Included in ranged attack, source visible/in range/in LoS", "Basic shot without riders", "Once per Archer turn-cycle", "source/status/coordinates/weapon/range/AoE are visible; the native Arc/Direct target-equality LoS contract is Strong, but its synchronous verdict and ranged-family/AoE provenance are not integrated", "missing synthesized weapon shot", "own-cycle primitive ready; effect-commit integration pending", "rename/text/data required", "mechanism-missing", "docs/job-balance/jobs/04-archer.md"),
    FinalReaction("White Mage", "Regenerator", 428, "confirmed-repurpose", "caution", "Hit", "Gain Regen", "Native trigger unless bounded later", "native real-code staging requires HP-damage result bit and carries the staged debit; survivor state is visible but lethal rejection remains outside the branch", "native Regenerate effect candidate", "native trigger; lethal-hit delivery live-gated", "rename/text/data required", "mechanism-near-native", "docs/job-balance/jobs/05-white-mage.md"),
    FinalReaction("Black Mage", "Rod Counter", 435, "candidate-repurpose", "courage", "Survives direct hostile action; source in Rod range/LoS; no reaction/DoT/field/self", "One basic Rod bolt; Magic Evade applies", "Once per attacker action", "hostile/self/direct/status/distance/Rod filters are visible; the native weapon-resolver LoS contract is Strong, but Rod trajectory choice, its synchronous verdict, and reaction/DoT/field provenance are not integrated", "native 435 is an exact copied-spell order, not a Rod bolt; final delivery needs a type-1/payload-0 basic-action carrier such as Counter 442", "attacker-action primitive ready; effect-commit integration pending", "rename/text/data required", "mechanism-missing", "docs/job-balance/jobs/06-black-mage.md"),
    FinalReaction("Monk", "Adrenaline Rush", 424, "candidate-repurpose", "neutral", "Damaged or starts turn below HP threshold", "Short non-stacking PA or Speed/CT buff", "One visible tier; resets after battle", "HP threshold is visible; damage and own-turn-start trigger producers are missing", "status/stat duration surface partial; final effect undecided", "missing tier/battle lifecycle ownership", "rename/text/data required", "design-and-mechanism-gate", "docs/job-balance/jobs/07-monk.md"),
    FinalReaction("Thief", "Vigilance", 426, "confirmed-native-record", "caution", "Targeted by visible attack", "Raise Dodge for that attack", "Once per Thief turn-cycle; no self-stack", "source/action and Invisible status are visible; managed physical contest is pre-roll", "native effect is Defending; transient Dodge needs calc-provenance reservation/commit", "own-cycle primitive ready; execution timing gate remains", "text/data tuning required", "timing-gate", "docs/job-balance/jobs/08-thief.md"),
    FinalReaction("Mystic", "Hex Ward", 443, "custom-generic-carrier", "caution", "Successful incoming hit while surviving", "Inflict Blind or Brave-down on attacker", "Once per attacker-action token; pass-2 acceptance consumes cadence", "custom exact-owner successful-result reservation owns the missing native 443 trigger; guarded accepted order can target source; pass-2 commit and state 0x2C own accepted/delivered cardinality", "offline-tested dynamic producer, source retarget, exact commit matching, idempotent cadence, immunity-aware Blind, and Brave floor; disabled/log-only defaults", "choose Blind vs Brave-down policy and live-prove one bounded composed vertical", "new name/text/data required", "mechanism-ready-live-gated", "docs/job-balance/jobs/09-mystic.md"),
    FinalReaction("Time Mage", "Mana Shield", 445, "confirmed-native-record", "caution", "Incoming damage", "Spend real MP to absorb HP damage", "Per hit while MP remains", "the combined pre-clamp context exposes native redirected damage through dcl.oldMpDebit plus current HP/MP; authoritative action provenance remains live-gated", "paired HP/MP formulas can author a ratio, but atomic result-flag/presentation ownership is missing", "native per-hit window", "text/data/calibration required", "mechanism-partial", "docs/job-balance/jobs/10-time-mage.md"),
    FinalReaction("Geomancer", "Nature's Wrath", 437, "confirmed-native-record", "courage", "Struck/targeted close by visible attacker", "Basic Geomancy from own tile, no rider/Scar", "Once per attacker action", "source/action, Invisible status, and close distance are formula-visible; filter is suppress-only", "native own-tile selector and Geomancy payload delivery are Strong; reaction-only status-rider suppression requires execution provenance", "attacker-action primitive ready; effect-commit integration pending", "text/data required", "mechanism-partial", "docs/job-balance/jobs/11-geomancer.md"),
    FinalReaction("Dragoon", "Dragon's Fury", 427, "candidate-repurpose", "courage", "Visible attacker within reach 2", "Spear counter-thrust with point-blank penalty", "Once per attacker action recommended by common counter policy", "source/action, Invisible status, reach, and Polearm equipment are formula-visible; filter is suppress-only", "physical/reach formulas exist; reaction strike synthesis missing", "attacker-action primitive ready; effect-commit integration pending", "rename/text/data required", "mechanism-missing", "docs/job-balance/jobs/12-dragoon.md"),
    FinalReaction("Orator", "Open", None, "unassigned", "none", "None authored", "None authored", "None", "not applicable", "not applicable", "not applicable", "no record should be fabricated", "intentional-open-slot", "docs/job-balance/jobs/13-orator.md"),
    FinalReaction("Summoner", "Open", None, "unassigned", "none", "None authored", "None authored", "None", "not applicable", "not applicable", "not applicable", "no record should be fabricated", "intentional-open-slot", "docs/job-balance/jobs/14-summoner.md"),
    FinalReaction("Dancer", "Earplugs", 448, "confirmed-native-record", "caution", "Performance, speech, or morale effect", "Narrow defense; never broad immunity", "Per eligible incoming effect", "formula 0x2A real-code coverage is Strong for Speechcraft 116..125; Bardsong 0x1C and Dance 0x1D need authored membership and a mapped or synthesized protection path", "native effect candidate for the mapped family; exact protected statuses/actions need authoring", "native-or-decision-required", "text/data and Bard field identity required", "mechanism-partial", "docs/job-balance/jobs/15-dancer.md"),
)


def load_catalog() -> dict[int, str]:
    with CATALOG.open(newline="", encoding="utf-8-sig") as handle:
        return {
            int(row["id_dec"]): row.get("name_ivc", "")
            for row in csv.DictReader(handle)
            if 422 <= int(row["id_dec"]) <= 453
        }


def validate() -> list[str]:
    errors: list[str] = []
    catalog = load_catalog()
    expected_ids = set(range(422, 454))
    actual_ids = {row.ability_id for row in NATIVE}
    if actual_ids != expected_ids:
        errors.append(f"native id coverage mismatch: missing={sorted(expected_ids-actual_ids)} extra={sorted(actual_ids-expected_ids)}")
    if len(NATIVE) != len(actual_ids):
        errors.append("duplicate native ids")
    for row in NATIVE:
        if catalog.get(row.ability_id, "") != row.expected_name:
            errors.append(f"catalog name mismatch for {row.ability_id}: {catalog.get(row.ability_id)!r} != {row.expected_name!r}")
        if row.category not in {"courage", "caution", "neutral"}:
            errors.append(f"invalid native category for {row.ability_id}: {row.category}")
    for row in FINAL:
        source = ROOT / row.source
        if not source.exists():
            errors.append(f"missing final-job source: {row.source}")
        elif row.design_name != "Open" and row.design_name.lower() not in source.read_text(encoding="utf-8").lower():
            errors.append(f"source does not name {row.design_name}: {row.source}")
        if row.native_id is not None and row.native_id not in expected_ids:
            errors.append(f"final reaction id outside native range: {row.job} {row.native_id}")
    return errors


def csv_rows() -> list[dict[str, str | int]]:
    rows: list[dict[str, str | int]] = []
    for item in NATIVE:
        row = {"record_kind": "native", **asdict(item)}
        rows.append(row)
    for item in FINAL:
        row = {"record_kind": "final", **asdict(item)}
        rows.append(row)
    fields = sorted({key for row in rows for key in row})
    return [{field: "" if row.get(field) is None else row.get(field, "") for field in fields} for row in rows]


def render_report(csv_path: Path) -> str:
    final_counts = Counter(row.readiness for row in FINAL)
    assigned = [row for row in FINAL if row.native_id is not None]
    return "\n".join([
        "# DCL reaction implementation manifest",
        "",
        "Generated by `tools/report_dcl_reaction_manifest.py`.",
        "",
        "## Verdict",
        "",
        "The exact chance input is available for every native Reaction id, including the VM-owned",
        "Shirahadori path. That does not complete the final DCL reaction roster: most final reactions",
        "also require trigger filters, source/action attribution, synthesized effects, or cadence state.",
        "",
        f"The native taxonomy covers `{len(NATIVE)}`/`32` records. The final roster contains `{len(FINAL)}`",
        f"entries, `{len(assigned)}` with a current record assignment and `{len(FINAL)-len(assigned)}` intentional open slots.",
        "",
        "## Final-job readiness",
        "",
        "| Readiness | Count |",
        "| --- | ---: |",
        *[f"| `{key}` | {value} |" for key, value in sorted(final_counts.items())],
        "",
        "## Final roster",
        "",
        "| Job | Reaction | Record | Category | Assignment | Readiness | Main missing mechanism |",
        "| --- | --- | ---: | --- | --- | --- | --- |",
        *[
            f"| {row.job} | {row.design_name} | {row.native_id if row.native_id is not None else '—'} | {row.category} | {row.assignment} | {row.readiness} | {row.trigger_filter}; {row.effect_delivery}; {row.cadence_control} |"
            for row in FINAL
        ],
        "",
        "## Native 32-row taxonomy",
        "",
        "| Id | Native name | Category | Confidence | Evaluation route | DCL disposition |",
        "| ---: | --- | --- | --- | --- | --- |",
        *[
            f"| `{row.ability_id}` | {row.expected_name or '(unnamed)'} | {row.category} | {row.category_confidence} | {row.evaluation_route} | {row.disposition} |"
            for row in NATIVE
        ],
        "",
        "## Hard boundary",
        "",
        "The categories marked `Hypothesis` and assignments marked `candidate` are authoring decisions,",
        "not engine facts. They must not be promoted into a deployment profile as final truth. Static",
        "analysis can continue on dispatcher/effect ownership, action identity, and cadence anchors before",
        "the bounded live slice is needed.",
        "",
        f"Machine-readable rows: `{csv_path}`.",
        "",
    ])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prefix", type=int, default=int(time.time()))
    args = parser.parse_args()
    errors = validate()
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    csv_path = ROOT / "work" / f"{args.prefix}-dcl-reaction-implementation-manifest.csv"
    md_path = ROOT / "work" / f"{args.prefix}-dcl-reaction-implementation-manifest.md"
    rows = csv_rows()
    fields = list(rows[0])
    with csv_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    md_path.write_text(render_report(csv_path), encoding="utf-8")
    print(f"wrote {csv_path}")
    print(f"wrote {md_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
