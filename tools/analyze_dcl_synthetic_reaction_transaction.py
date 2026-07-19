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
    Anchor("configurable-owner", "mod", "public int DclSyntheticReactionCarrierId { get; set; } = -1;", "the equipped owner identity is configurable"),
    Anchor("configurable-delivery", "mod", "public int DclSyntheticReactionDeliveryId { get; set; } = -1;", "the native delivery identity is separate and configurable"),
    Anchor("explicit-trigger", "mod", 'public string DclSyntheticReactionTrigger { get; set; } = "successful-hit-survivor";', "the currently owned trigger family is explicit"),
    Anchor("exact-carrier-rule", "validator", "exactly one DclReactionRules entry for its configured owner carrier", "one taxonomy rule owns the configured owner"),
    Anchor("hit-context", "mod", "incoming.HitDecisionKnown && incoming.Hit", "only a known landed incoming action may reserve the trigger"),
    Anchor("cadence-peek", "cadence", "public bool CanConsumeAttackerAction", "eligibility can be checked without early consumption"),
    Anchor("no-early-consume", "core", "Evaluation reserves but never consumes reaction cadence", "the chance callback owns no cadence commit"),
    Anchor("successful-hit-owner", "mod", "ReserveDclSyntheticReactionFromCommittedHit", "the successful result path can own a missing native trigger"),
    Anchor("synthetic-only-preclamp-entry", "mod", "(hasManagedOutput || settings.DclSyntheticReactionEnabled)", "a synthetic-only profile reaches the committed pre-clamp result owner"),
    Anchor("synthetic-only-no-numeric-write", "mod", "return hasManagedOutput ? dclDebit : -1;", "a synthetic-only log gate observes the result without rewriting its native HP debit"),
    Anchor("equipped-carrier-check", "mod", "defender.ReadUInt16(0x14) != carrierId", "only an exact equipped-carrier owner is evaluated"),
    Anchor("survivor-check", "mod", "DclLifecycle.WouldBeLethal(defender.Hp, hpCredit, hpDebit)", "a lethal incoming result cannot reserve a reaction through the shared staged-HP equation"),
    Anchor("final-hp-check", "mod", '"cmp word [r8+30h], 0"', "the pass-2 producer rejects a defender killed by a later strike"),
    Anchor("final-ko-check", "mod", '"test byte [r8+61h], 20h"', "the pass-2 producer rejects effective KO state"),
    Anchor("committed-origin", "mod", 'Origin: "committed-preclamp"', "the reservation records successful-result provenance"),
    Anchor("duplicate-gate", "core", "ShouldRequestProducer: false", "replayed callbacks never restage"),
    Anchor("dynamic-mailbox", "mod", "SRP_STATES + defenderTableIndex, SRP_STATE_REQUESTED", "accepted defenders arm their own producer slot"),
    Anchor("bounded-producer-loop", "mod", '".synthetic_reaction_producer_loop:"', "one indexed loop handles every battle-table slot without exceeding the hook assembler budget"),
    Anchor("producer-loop-cardinality", "mod", '"cmp edx, 21"', "the compact producer still covers all 21 battle-table slots"),
    Anchor("empty-slot", "mod", '"cmp word [r8+1CEh], 0"', "the producer never overwrites a native candidate"),
    Anchor("delivery-stage", "mod", '$"mov word [r8+1CEh], {deliveryId}"', "the producer stages the configured native delivery carrier"),
    Anchor("bounded-stage", "validator", "synthetic Reaction live producer writes must be bounded within 1..32", "live staging is capped"),
    Anchor("special-delivery-set", "validator", "434, 435, 436, 437, 440, 441, or 442", "delivery is restricted to carriers proven to traverse the guarded materialization path"),
    Anchor("delivery-validation-required", "validator", "live synthetic Reaction requires native delivery-validation capture", "live composition distinguishes native rejection from accepted materialization"),
    Anchor("typed-family-validation-hook", "mod", "DCL_REACTION_TYPED_FAMILY_VALIDATION_RVA = 0x283019", "the shared typed-family helper result for ids 435/436/437/442 is observed before rejection"),
    Anchor("bonecrusher-validation-hook", "mod", "DCL_REACTION_BONECRUSHER_VALIDATION_RVA = 0x283148", "Bonecrusher's separate typed-helper result is observed before rejection"),
    Anchor("final-validation-hook", "mod", "DCL_REACTION_FINAL_VALIDATION_HOOK_RVA = 0x283157", "the final VM-owned result hook begins at its call rather than stealing the later restore target"),
    Anchor("final-validation-hook-length", "mod", "DCL_REACTION_FINAL_VALIDATION_HOOK_LENGTH = 7", "the final hook relocates exactly call plus test"),
    Anchor("final-validation-restore-target", "mod", "DCL_REACTION_FINAL_RESTORE_RVA = 0x283160", "the typed-rejection restore entry is named as an external hook boundary"),
    Anchor("final-validation-execute-after", "mod", "AsmHookBehaviour.ExecuteAfter, DCL_REACTION_FINAL_VALIDATION_HOOK_LENGTH", "the final result is captured after the relocated validator call and test"),
    Anchor("delivery-rejected-state", "mod", "SRP_STATE_DELIVERY_REJECTED", "a consumed but rejected exact delivery cannot masquerade as pending or committed"),
    Anchor("optional-rewrite-delivery-match", "validator", "optional accepted-order rewrite must use the configured delivery id", "an optional rewrite binds to delivery, never owner identity"),
    Anchor("pass2-only", "mod", "queuePass != 2 || reactionId != deliveryId || !idsAgree", "commit accepts only the exact pass-2 delivery identity"),
    Anchor("commit-unit-record", "mod", "ProcessDclSyntheticReactionCommit(queuePass, reactionId, idsAgree, record, sourceIndex);", "the actor's captured +0x148 unit record, not the actor object, owns the reservation"),
    Anchor("delivery-owned-state", "mod", "SRP_STATE_DELIVERY_OWNED", "native materialization publishes successful delivery ownership"),
    Anchor("delivery-handshake", "mod", '$"mov byte [rdi+r9+{SRP_STATES}], {SRP_STATE_DELIVERY_OWNED}"', "only an exact staged delivery observed at materialization advances the producer state"),
    Anchor("producer-owned-commit", "mod", "only an exact staged request observed on its configured delivery path may commit", "blocked or unrelated native orders cannot consume cadence"),
    Anchor("source-commit", "core", "pending.ActionToken.SourceIdx != sourceTableIndex", "the committed source must equal the reserved incoming source"),
    Anchor("cadence-commit", "mod", ".TryConsumeAttackerAction(carrierId, reservation.ActionToken)", "cadence is consumed only after pass-2 acceptance"),
    Anchor("native-delivery", "mod", '"materialized-delivery-owned"', "effect delivery belongs to the configured native carrier, not hard-coded managed writes"),
    Anchor("duplicate-commit", "core", "pending.Phase != DclSyntheticReactionReservationPhase.Requested", "the reservation can commit only once"),
)

FORBIDDEN = (
    ("mod", "DclHexWard", "no job-specific settings remain in the runtime"),
    ("mod", "Hex Ward", "no job-specific name remains in the runtime"),
    ("core", "Hex Ward", "the coordinator is job-agnostic"),
    ("validator", "Hex Ward", "validation is job-agnostic"),
    ("mod", "synthetic_reaction_producer_{unitIndex}_done", "the producer is never expanded into 21 assembler blocks"),
    ("mod", "ProcessDclSyntheticReactionCommit(queuePass, reactionId, idsAgree, actor, sourceIndex);", "an actor object is never mistaken for its +0x148 unit record"),
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
        "request without overwriting native candidates and stages a separately configured delivery",
        "carrier. Three native validation-result hooks distinguish rejected candidates from accepted",
        "materialization and change only the private mailbox on rejection. The special-family",
        "materialization hook confirms that exact staged delivery before its pass-2 commit consumes",
        "attacker-action cadence. Owner identity, native delivery identity,",
        "and an optional order rewrite remain separate. The runtime contains no job name, fixed id,",
        "or hard-coded status/stat effect.",
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
