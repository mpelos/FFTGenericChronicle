#!/usr/bin/env python3
"""Report the implementation boundary for every authored DCL reaction.

The matrix separates five different questions that are easy to conflate: whether a predicate is
visible to formulas, whether the engine opens the reaction window, whether the requested effect can
be delivered, whether cadence can be committed safely, and what still needs live proof.
"""
from __future__ import annotations

import argparse
import csv
import time
from collections import Counter
from dataclasses import asdict, dataclass
from pathlib import Path

import report_dcl_reaction_manifest as manifest


ROOT = Path(__file__).resolve().parents[1]


@dataclass(frozen=True)
class Capability:
    job: str
    reaction: str
    filter_readiness: str
    filter_formula_surface: str
    missing_filter_signal: str
    trigger_window: str
    effect_delivery: str
    cadence_commit: str
    next_offline_step: str
    live_gate: str


ROWS = (
    Capability("Squire", "Counter Tackle", "ready-in-context", "source/action + hit + weapon-strike + grid distance", "none", "native unit+0x95 bit plus exact-id filter", "native pass 2 emits source-target type-0x0B Rush 147; formula 0x37 preserves knockback while DCL owns magnitude", "native/default", "author the landed-melee condition and decide multi-hit cadence; no synthesized shove is needed", "confirm one native Rush knockback, source direction, and chosen multi-hit cadence"),
    Capability("Squire", "Grit", "design-open", "low-HP and Critical status are visible", "final trigger, payoff, threshold, amount, and cadence are under-specified", "native Critical: Recover HP 431 post-hit window passes Brave and then performs a full missing-HP heal", "if the payoff is healing, preserve carrier 431 but replace its HP credit with the authored bounded amount", "condition only", "finish the visible minor-clutch design, then bind the chosen amount to reaction provenance instead of accepting the native full heal", "only after the design is fixed; verify trigger threshold, bounded credit, and cadence"),
    Capability("Chemist", "Auto-Potion", "partial-context", "hit-known + survivor HP/KO state", "reaction context has no committed-damage amount/phase signal", "native Auto-Potion window", "native selector is Potion/Hi-Potion/X-Potion only, first available; inventory decrement is a Strong candidate; final Chemist authors no Item Lore multiplier", "primitive-only: own-turn cycle; all three execution commits are probed", "map the VM-to-item-decrement edge and bind cadence at the owning commit pass", "prove post-damage timing, survivor guard, one-item consumption, native heal amount, and cadence"),
    Capability("Knight", "Riposte", "ready-in-context", "miss/defended + weapon-strike + adjacency", "none", "native Counter 442 can be staged at unit+0x1CE before pass 2; the bounded empty-slot producer is implemented log-only but source lifetime is not live-proven", "Counter 442 already authors type-1/payload-0 attack against the source global; action replacement vertical slice is prepared", "primitive-only: attacker action; all three execution commits are probed", "run LT23/LT28, then bind the proven execution decision to the one-write producer and commit cadence", "prove source ownership, one counter per attacker action, and no forecast consumption"),
    Capability("Archer", "Countershot", "partial-context", "source/action + Invisible + coordinates/range/AoE + native Arc/Direct resolver contract", "synchronous resolver verdict is not integrated; ranged/magic/AoE provenance remains partial", "mixed: native Archer's Bane window is insufficient; Counter 442 is a candidate generic source-target carrier and its empty-slot producer is prepared log-only", "basic-action replacement is prepared log-only inside an accepted carrier window; native resolver equality owns weapon LoS", "primitive-only: own-turn cycle; all three execution commits are probed", "bind the pass-2 producer to the shot decision and replacement while preserving native resolver target equality", "prove AoE inclusion, magic trigger, shot replacement, LoS, and cadence"),
    Capability("White Mage", "Regenerator", "ready-in-context", "native HP-damage result bit + staged debit; survivor HP/KO is formula-visible", "none for the authored landed-damage trigger", "real-code reaction bit + HP-damage branch stages exact id 428 and debit payload", "native Regen effect candidate; survivor/lethal rejection is outside the mapped branch", "native/default", "keep the native window and verify surrounding delivery rejects lethal hits before relying on it unchanged", "vertical slice for Caution chance, native Regen, and lethal-hit negative control"),
    Capability("Black Mage", "Rod Counter", "partial-context", "hostile/non-self + survivor + direct flag + Invisible + distance + Rod/range + native weapon resolver contract", "Rod-bolt trajectory flag and synchronous resolver verdict are not integrated; reaction/DoT/field provenance remains partial", "mixed: Magick Counter does not cover every direct hostile action; the generic empty-slot producer is prepared log-only", "native Magick Counter is proven spell-copy (type 0x0B + incoming id); final Rod bolt needs a type-1/payload-0 basic-action carrier, with Counter 442 structurally preferred", "primitive-only: attacker action; execution commit candidate/probe prepared", "pair provenance and Rod trajectory policy with the Counter-442 producer rather than copied-spell delivery", "prove exclusions, LoS, Magic Evade, replacement, and cadence"),
    Capability("Monk", "Adrenaline Rush", "partial-context", "HP threshold and Critical status are visible", "damage-event and own-turn-start trigger producers", "missing for combined damage/start-turn policy", "design open between PA and Speed/CT; temporary non-stack layer missing", "battle-reset/visible-tier ownership missing", "resolve the payoff, then implement turn-start/damage producers and visible temporary state", "prove non-stack, duration, reset, and no permanent growth"),
    Capability("Thief", "Vigilance", "ready-in-context", "source/action + Invisible status", "none if visible means not Invisible; true LoS would be a new requirement", "managed physical contest is the correct pre-roll phase", "native effect is Defending and must be suppressed; transient Dodge bonus needs execution-provenance reservation", "primitive-only: own-turn cycle", "add a reservation/commit token to DclDodgeFormula after calc provenance distinguishes forecast from execution", "prove only the qualifying executed attack receives Dodge, cadence commits once, and native Defending is absent"),
    Capability("Mystic", "Hex Ward", "ready-in-context", "successful incoming result + exact source/action token + survivor", "none; custom owner supplies the absent native trigger", "exact-owner successful-result commit resolves the Caution roll and arms a bounded per-defender empty-slot producer for carrier 443", "accepted-order source retarget plus exact producer-owned pass-2 commit delivers immunity-aware Blind or floored current-Brave decrease; duplicate callbacks/commits are idempotent", "once per attacker action, consumed only at pass-2 commit", "run the validator-clean log-only profile, then one bounded live producer/retarget/effect vertical", "prove source lifetime, target direction, presentation, Blind immunity, Brave floor, and state-0x2C audit correlation"),
    Capability("Time Mage", "Mana Shield", "ready-in-context", "hit-known + original damage in dcl.oldMpDebit + current HP/MP", "none at the combined pre-clamp boundary; calc provenance still governs action attribution", "native Mana Shield window", "native redirect is Strong and over-generous (one MP can prevent the whole hit); paired HP/MP formulas can author a split, but result-flag/presentation normalization is missing", "per hit while MP remains", "author the ratio/floor policy, then add coherent HP/MP result-flag ownership for rejected or partial redirects", "prove atomic HP/MP result, depletion floor, popup/forecast parity, miss composition, and Caution chance"),
    Capability("Geomancer", "Nature's Wrath", "ready-in-context", "source/action + Invisible status + close grid distance", "none for the documented visibility rule", "native Nature's Wrath window and own-tile terrain selector", "native selector maps the reactor tile to ordinary Geomancy payload 126..137; damage delivery exists, but reaction-only rider suppression needs execution provenance", "primitive-only: attacker action; execution commit candidate/probe prepared", "feed reaction provenance into status-output rules while preserving the selected payload for damage/element lookup", "prove own-tile selection, no rider/Scar, cause filter, and cadence"),
    Capability("Dragoon", "Dragon's Fury", "ready-in-context", "source/action + Invisible status + reach distance + target Polearm equipment", "none for the documented trigger", "Counter 442 pass-2 staging is a candidate new counter window and its empty-slot producer is prepared log-only", "basic-strike replacement is prepared log-only inside an accepted carrier window", "primitive-only: attacker action; all three execution commits are probed", "bind the producer to target weapon/reach context after execution provenance is proven", "prove reach 2, point-blank penalty, weapon gate, source ownership, and cadence"),
    Capability("Orator", "Open", "intentional-open", "not applicable", "no reaction is authored", "not applicable", "not applicable", "not applicable", "do not fabricate a mechanism before the design chooses a reaction", "none"),
    Capability("Summoner", "Open", "intentional-open", "not applicable", "no reaction is authored", "not applicable", "not applicable", "not applicable", "do not fabricate a mechanism before the design chooses a reaction", "none"),
    Capability("Dancer", "Earplugs", "design-partial", "exact ability-id families can be expressed through a FormulaMap; real-code formula 0x2A covers Speechcraft 116..125", "the final performance/speech/morale membership list is not authored; Bardsong 0x1C and Dance 0x1D are outside the mapped branch", "native Earplugs real-code window for Speechcraft; any performance VM path is unresolved", "native avoidance exists for the mapped family; broader narrow-scope suppression needs an authored producer", "per eligible effect", "author exact protected ids, then map or synthesize the 0x1C/0x1D protection path", "prove narrow scope against real Bard/Dancer/Orator actions"),
)


def validate() -> list[str]:
    errors: list[str] = []
    expected = [(row.job, row.design_name) for row in manifest.FINAL]
    actual = [(row.job, row.reaction) for row in ROWS]
    if actual != expected:
        errors.append(f"final roster mismatch: expected={expected!r} actual={actual!r}")
    allowed = {"ready-in-context", "partial-context", "design-open", "design-partial", "intentional-open"}
    for row in ROWS:
        if row.filter_readiness not in allowed:
            errors.append(f"invalid filter readiness for {row.job}/{row.reaction}: {row.filter_readiness}")
        if not row.next_offline_step.strip():
            errors.append(f"missing offline step for {row.job}/{row.reaction}")
    return errors


def render_report(csv_path: Path) -> str:
    counts = Counter(row.filter_readiness for row in ROWS)
    esc = lambda value: value.replace("|", "\\|")
    lines = [
        "# DCL reaction capability matrix",
        "",
        "Generated by `tools/report_dcl_reaction_capabilities.py`. Do not hand-edit.",
        "",
        "The matrix treats a formula filter, a reaction trigger window, an effect, and cadence as",
        "separate mechanisms. `ready-in-context` means the requested predicate is expressible only",
        "when the engine or a future producer has already opened the correct reaction evaluation.",
        "It does not mean the reaction is implemented.",
        "",
        "## Filter-surface count",
        "",
        "| Classification | Entries |",
        "| --- | ---: |",
        *[f"| `{key}` | {value} |" for key, value in sorted(counts.items())],
        "",
        "## Final roster",
        "",
        "| Job | Reaction | Formula-filter surface | Missing filter signal | Trigger window | Effect delivery | Cadence |",
        "| --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in ROWS:
        lines.append(
            f"| {row.job} | {row.reaction} | `{row.filter_readiness}`: {esc(row.filter_formula_surface)} | "
            f"{esc(row.missing_filter_signal)} | {esc(row.trigger_window)} | {esc(row.effect_delivery)} | "
            f"{esc(row.cadence_commit)} |"
        )
    lines.extend(
        [
            "",
            "## Ordered offline work",
            "",
            "1. Pass LT23 for the execution-only commit/target boundary, then LT24 log-only and one-shot action-id replacement.",
            "2. Generalize the proven single-carrier action replacement into rules and build missing trigger producers.",
            "3. Integrate the mapped native Arc/Direct target-equality verdict synchronously and finish authoritative action provenance mapping.",
            "4. Split native effect bundles where the final reaction forbids riders or narrows item/action families.",
            "5. Keep design-open effects out of runtime profiles until their visible payoff is fixed.",
            "",
            "## Next step and live boundary by reaction",
            "",
            "| Job / reaction | Next offline step | Eventual live proof |",
            "| --- | --- | --- |",
        ]
    )
    for row in ROWS:
        lines.append(f"| {row.job} / {row.reaction} | {esc(row.next_offline_step)} | {esc(row.live_gate)} |")
    lines.extend(["", f"Machine-readable rows: `{csv_path}`.", ""])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prefix", type=int, default=int(time.time()))
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    errors = validate()
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    if args.check_only:
        print(f"DCL reaction capability matrix validated: {len(ROWS)} entries")
        return 0

    csv_path = ROOT / "work" / f"{args.prefix}-dcl-reaction-capabilities.csv"
    md_path = ROOT / "work" / f"{args.prefix}-dcl-reaction-capabilities.md"
    with csv_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(asdict(ROWS[0])))
        writer.writeheader()
        writer.writerows(asdict(row) for row in ROWS)
    md_path.write_text(render_report(csv_path), encoding="utf-8", newline="\n")
    print(f"wrote {csv_path}")
    print(f"wrote {md_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
