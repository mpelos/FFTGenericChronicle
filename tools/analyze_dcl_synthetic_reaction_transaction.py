#!/usr/bin/env python3
"""Fail closed on the generic synthetic-Reaction transaction."""
from __future__ import annotations

import argparse
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MOD = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "Mod.cs"
DEFAULT_CORE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclSyntheticReaction.cs"
DEFAULT_CADENCE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclReactionCadence.cs"
DEFAULT_VALIDATOR = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "RuntimeSettingsValidator.cs"


@dataclass(frozen=True)
class Anchor:
    name: str
    owner: str
    needle: str
    meaning: str


ANCHORS = (
    Anchor("disabled-default", "mod", "public bool DclSyntheticReactionEnabled { get; set; } = false;", "the composed mechanism is opt-in"),
    Anchor("log-only-default", "mod", "public bool DclSyntheticReactionLogOnly { get; set; } = true;", "the mechanism starts observe-only"),
    Anchor("configurable-carrier", "mod", "public int DclSyntheticReactionCarrierId { get; set; } = -1;", "no Reaction id is built in"),
    Anchor("explicit-trigger", "mod", 'public string DclSyntheticReactionTrigger { get; set; } = "successful-hit-survivor";', "the currently owned trigger family is explicit"),
    Anchor("exact-carrier-rule", "validator", "exactly one DclReactionRules entry for its configured carrier", "one taxonomy rule owns the configured carrier"),
    Anchor("hit-context", "mod", "incoming.HitDecisionKnown && incoming.Hit", "only a known landed incoming action may reserve the trigger"),
    Anchor("cadence-peek", "cadence", "public bool CanConsumeAttackerAction", "eligibility can be checked without early consumption"),
    Anchor("no-early-consume", "core", "Evaluation reserves but never consumes reaction cadence", "the chance callback owns no cadence commit"),
    Anchor("successful-hit-owner", "mod", "ReserveDclSyntheticReactionFromCommittedHit", "the successful result path can own a missing native trigger"),
    Anchor("synthetic-only-preclamp-entry", "mod", "(hasManagedOutput || settings.DclSyntheticReactionEnabled)", "a synthetic-only profile reaches the committed pre-clamp result owner"),
    Anchor("synthetic-only-no-numeric-write", "mod", "return hasManagedOutput ? dclDebit : -1;", "a synthetic-only log gate observes the result without rewriting its native HP debit"),
    Anchor("equipped-carrier-check", "mod", "defender.ReadUInt16(0x14) != carrierId", "only an exact equipped-carrier owner is evaluated"),
    Anchor("survivor-check", "mod", "defender.Hp + hpCredit - hpDebit <= 0", "a lethal incoming result cannot reserve a reaction"),
    Anchor("final-hp-check", "mod", '"cmp word [r8+30h], 0"', "the pass-2 producer rejects a defender killed by a later strike"),
    Anchor("final-ko-check", "mod", '"test byte [r8+61h], 20h"', "the pass-2 producer rejects effective KO state"),
    Anchor("committed-origin", "mod", 'Origin: "committed-preclamp"', "the reservation records successful-result provenance"),
    Anchor("duplicate-gate", "core", "ShouldRequestProducer: false", "replayed callbacks never restage"),
    Anchor("dynamic-mailbox", "mod", "SRP_STATES + defenderTableIndex, 1", "accepted defenders arm their own producer slot"),
    Anchor("bounded-producer-loop", "mod", '".synthetic_reaction_producer_loop:"', "one indexed loop handles every battle-table slot without exceeding the hook assembler budget"),
    Anchor("producer-loop-cardinality", "mod", '"cmp edx, 21"', "the compact producer still covers all 21 battle-table slots"),
    Anchor("empty-slot", "mod", '"cmp word [r8+1CEh], 0"', "the producer never overwrites a native candidate"),
    Anchor("carrier-stage", "mod", '$"mov word [r8+1CEh], {carrierId}"', "the producer stages the configured carrier"),
    Anchor("bounded-stage", "validator", "synthetic Reaction live producer writes must be bounded within 1..32", "live staging is capped"),
    Anchor("rewrite-carrier-match", "validator", "accepted-order rewrite must use the same exact carrier id", "producer and order rewrite cannot disagree"),
    Anchor("pass2-only", "mod", "queuePass != 2 || reactionId != carrierId || !idsAgree", "commit accepts only exact pass-2 carrier identity"),
    Anchor("producer-owned-commit", "mod", "only the dynamic producer's exact staged request may commit", "an unrelated native carrier cannot commit the reservation"),
    Anchor("source-commit", "core", "pending.ActionToken.SourceIdx != sourceTableIndex", "the committed source must equal the reserved incoming source"),
    Anchor("cadence-commit", "mod", ".TryConsumeAttackerAction(carrierId, reservation.ActionToken)", "cadence is consumed only after pass-2 acceptance"),
    Anchor("generic-delivery", "mod", '"accepted-order-owned"', "effect delivery belongs to the generic accepted order, not hard-coded managed writes"),
    Anchor("duplicate-commit", "core", "pending.Phase != DclSyntheticReactionReservationPhase.Requested", "the reservation can commit only once"),
)

FORBIDDEN = (
    ("mod", "DclHexWard", "no job-specific settings remain in the runtime"),
    ("mod", "Hex Ward", "no job-specific name remains in the runtime"),
    ("core", "Hex Ward", "the coordinator is job-agnostic"),
    ("validator", "Hex Ward", "validation is job-agnostic"),
    ("mod", "synthetic_reaction_producer_{unitIndex}_done", "the producer is never expanded into 21 assembler blocks"),
)


def analyze(texts: dict[str, str]) -> list[tuple[Anchor, bool]]:
    results = [(anchor, anchor.needle in texts[anchor.owner]) for anchor in ANCHORS]
    for owner, needle, meaning in FORBIDDEN:
        results.append((Anchor(f"forbid-{needle.lower().replace(' ', '-')}", owner, needle, meaning), needle not in texts[owner]))
    return results


def render(paths: dict[str, Path], output: Path) -> tuple[str, bool]:
    texts = {owner: path.read_text(encoding="utf-8") for owner, path in paths.items()}
    results = analyze(texts)
    ok = all(passed for _, passed in results)
    rows = [
        f"| `{anchor.name}` | {'PASS' if passed else 'FAIL'} | {anchor.meaning} |"
        for anchor, passed in results
    ]
    report = "\n".join([
        "# DCL synthetic-Reaction transaction analysis",
        "",
        "Generated by `tools/analyze_dcl_synthetic_reaction_transaction.py`.",
        "",
        "| Anchor | Result | Contract |",
        "| --- | --- | --- |",
        *rows,
        "",
        "## Contract",
        "",
        "An exact equipped carrier may reserve one configured taxonomy roll only after a successful",
        "incoming result that the defender survives. The pass-2 pre-selector consumes a per-defender",
        "request without overwriting native candidates. A separately guarded accepted-order rewrite",
        "owns action replacement or retargeting and native delivery. Only an exact producer-owned",
        "pass-2 commit consumes attacker-action cadence. The runtime contains no job name, fixed",
        "carrier id, or hard-coded status/stat effect.",
        "",
        f"Overall offline gate: **{'PASS' if ok else 'FAIL'}**.",
        "",
    ])
    return report, ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-synthetic-reaction-transaction-analysis.md"
    paths = {
        "mod": DEFAULT_MOD,
        "core": DEFAULT_CORE,
        "cadence": DEFAULT_CADENCE,
        "validator": DEFAULT_VALIDATOR,
    }
    report, ok = render(paths, output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("synthetic-Reaction transaction PASS" if ok else "synthetic-Reaction transaction FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
