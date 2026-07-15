#!/usr/bin/env python3
"""Summarize action-identity evidence from Generic Chronicle battleprobe logs.

This is a focused companion to analyze_battleprobe_log.py. It answers one question:
can the runtime identify caster + ability/action id for DCL formulas?
"""
from __future__ import annotations

import argparse
import csv
import re
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt"
)
DEFAULT_ABILITIES = ROOT / "work" / "baseline_abilities.csv"

ACTOR_CTX_RE = re.compile(
    r"\[PRECLAMP-ACTOR-CTX event=(?P<event>\d+) now=(?P<now>\d+) "
    r"target=(?P<target>0x[0-9A-F]+)/id=(?P<target_id>0x[0-9A-F]{2}) "
    r"oldDebit=(?P<old_debit>-?\d+) (?P<body>.*?) "
    r"actionId=(?P<action_id>-?\d+) verdict=(?P<verdict>[^ ]+) actors=\[(?P<actors>[^\]]*)\]"
)
CASTER_RE = re.compile(r"caster=(?P<caster>0x[0-9A-F]+)/id=(?P<caster_id>0x[0-9A-F]{2}) casterActor=(?P<actor>0x[0-9A-F]+)")
PENDING_TRACK_RE = re.compile(
    r"\[PENDING-ACTION-TRACK (?P<kind>enter|update|resolve-open|resolve-close|abandon) "
    r".*?(?:caster|0x[0-9A-F]+)?=?0x?(?P<ptr>[0-9A-F]+)?[^]]*?\bact=(?P<action_id>\d+)"
)
PENDING_MATCH_RE = re.compile(
    r"\[PENDING-ACTION-MATCH kind=(?P<kind>[^ ]+) event=(?P<event>\d+) "
    r"target=(?P<target>0x[0-9A-F]+)/id=(?P<target_id>0x[0-9A-F]{2}) "
    r"(?P<body>.*)\]"
)
PENDING_TARGET_RE = re.compile(
    r"\[PENDING-ACTION-TARGET (?P<kind>enter|reenter|clear|drop) "
    r"target=(?P<target>0x[0-9A-F]+)/id=(?P<target_id>0x[0-9A-F]{2}) "
    r"(?P<body>.*)\]"
)
PENDING_RESOLVED_RE = re.compile(
    r"resolved=(?P<caster>0x[0-9A-F]+)/id=(?P<caster_id>0x[0-9A-F]{2}) "
    r"source=(?P<source>[^ ]+) batch=(?P<batch>\d+) act=(?P<action_id>\d+) "
    r".*?confidence=(?P<confidence>[^ ]+) score=(?P<score>-?\d+)"
)
IMMEDIATE_RE = re.compile(
    r"\[PRECLAMP-IMMEDIATE-CANDIDATES target=(?P<target>0x[0-9A-F]+)/id=(?P<target_id>0x[0-9A-F]{2}) "
    r"oldDebit=(?P<old_debit>-?\d+) oldCredit=(?P<old_credit>-?\d+) .*? (?P<selected>selected=[^]]+)\]"
)
IMMEDIATE_SELECTED_RE = re.compile(
    r"selected=(?P<caster>0x[0-9A-F]+)/id=(?P<caster_id>0x[0-9A-F]{2})/act=(?P<action_id>-?\d+)/"
    r"score=(?P<score>-?\d+)/runnerUp=(?P<runner_up>-?\d+)/margin=(?P<margin>-?\d+)"
)
FORMULA_CANDIDATE_RE = re.compile(
    r"\[PRECLAMP-FORMULA-CANDIDATE event=(?P<event>\d+) ptr=(?P<target>0x[0-9A-F]+) "
    r"id=(?P<target_id>0x[0-9A-F]{2}) .*?oldDebit=(?P<old_debit>-?\d+) oldCredit=(?P<old_credit>-?\d+) "
    r".*?attacker=(?P<attacker>[^ ]+) source=(?P<source>[^ ]+) (?P<body>.*?) now=(?P<now>\d+) action=(?P<action>.*)\]"
)
RUNTIME_ACTION_RE = re.compile(r"\[PRECLAMP-FORMULA-RUNTIME .*? action=(?P<action>[^ |]+)")
EQUIP_RE = re.compile(
    r"\[PRECLAMP-EQUIP event=(?P<event>\d+) side=(?P<side>target|caster) "
    r"ptr=(?P<ptr>0x[0-9A-F]+)/id=(?P<id>0x[0-9A-F]{2}) (?P<body>.*)"
)
ACTION_STATE_RE = re.compile(r"\[(?:ACTION-STATE|ACTION-BOUNDARY).*?\bact=(?P<action_id>\d+)")
HP_EVENT_RE = re.compile(
    r"\[HP-EVENT-PROBE kind=(?P<kind>[^ ]+) event=(?P<event>\d+) "
    r"ptr=(?P<target>0x[0-9A-F]+) id=(?P<target_id>0x[0-9A-F]{2}) .*?"
    r"appliedHpLoss=(?P<hp_loss>-?\d+) appliedHpGain=(?P<hp_gain>-?\d+) .*? action=(?P<action>.*)\]"
)
SELECTOR_RE = re.compile(
    r"\[SELECTOR-PROBE event=(?P<event>\d+) evadeType=0x(?P<evade_type>[0-9A-F]{2})"
    r"\((?P<evade_name>[^)]*)\) actor=(?P<actor>0x[0-9A-F]+):(?P<actor_text>.*?) "
    r"record=(?P<record>0x[0-9A-F]+) (?P<unit_text>unit:[^ ]+) now=(?P<now>\d+) "
    r"rec\+1BB=(?P<rec_1bb>[0-9A-F-]{2}) rec\+1BE=(?P<rec_1be>[0-9A-F-]{2}) "
    r"rec\+1C0=(?P<rec_1c0>[0-9A-F-]{2}) rec\+1C4\(dmg\)=(?P<rec_dmg>-?\d+|----) "
    r"rec\+1E5=(?P<rec_1e5>[0-9A-F-]{2})\]"
)
SELECTOR_UNIT_ID_RE = re.compile(r"unit:id=(?P<unit_id>0x[0-9A-F]{2})")
SELECTOR_CONTEXT_RE = re.compile(r"ctxRegs=\[(?P<regs>[^\]]*)\] ctxStack=\[(?P<stack>[^\]]*)\]")
SELECTOR_CONTEXT_ACTOR_RE = re.compile(
    r"(?P<source>[^=,\]]+)=0x(?P<actor_ptr>[0-9A-F]+):"
    r"(?P<role>actor(?::record-unit)?):id=(?P<unit_id>0x[0-9A-F]{2}):"
    r"unit=(?P<unit_ptr>0x[0-9A-F]+):act=(?P<action_id>-?\d+)"
)
HOOK_REGS_EVENT_RE = re.compile(
    r"\[HOOK-REGS-EVENT kind=(?P<kind>[^ ]+) event=(?P<event>\d+) "
    r".*?hookPtr=(?P<hook_ptr>0x[0-9A-F]+) targetPtr=(?P<target>0x[0-9A-F]+) "
    r"id=(?P<target_id>0x[0-9A-F]{2})\] (?P<body>.*)"
)
LANDMARK_HIT_RE = re.compile(
    r"\[LANDMARK-HIT event=(?P<event>\d+) id=(?P<id>\d+) name=(?P<name>[^ ]+) "
    r"rva=0x(?P<rva>[0-9A-F]+) .*?base=(?P<base>[^ ]+) now=(?P<now>\d+) "
    r"(?P<body>.*)"
)
REGISTER_ACTOR_REF_RE = re.compile(
    r"(?P<source>[A-Za-z0-9_+\-x]+)=0x(?P<actor_ptr>[0-9A-F]+):"
    r"actor:id=(?P<unit_id>0x[0-9A-F]{2}):unit=(?P<unit_ptr>0x[0-9A-F]+):act=(?P<action_id>-?\d+)"
)
REGISTER_UNIT_REF_RE = re.compile(
    r"(?P<source>[A-Za-z0-9_+\-x]+)=0x(?P<unit_ptr>[0-9A-F]+):"
    r"unit(?::(?P<label>[A-Za-z0-9_\-]+))?:id=(?P<unit_id>0x[0-9A-F]{2}):"
    r"team=(?P<team>-?\d+):hp=(?P<hp>-?\d+):ct=(?P<ct>-?\d+)"
)
SELECTOR_ACTOR_TEXT_RE = re.compile(
    r"(?P<role>actor(?::record-unit)?):id=(?P<unit_id>0x[0-9A-F]{2}):"
    r"unit=(?P<unit_ptr>0x[0-9A-F]+):act=(?P<action_id>-?\d+)"
)
ACTION_ID_IN_TEXT_RE = re.compile(r"(?:^|[^A-Za-z])(?:act|actionid|id|signal)=(?P<action_id>-?\d+)", re.I)


@dataclass(frozen=True)
class ActorContext:
    line_no: int
    event: int
    now: int
    target: str
    target_id: str
    old_debit: int
    caster: str | None
    caster_id: str | None
    caster_actor: str | None
    action_id: int
    verdict: str
    actors: str


@dataclass(frozen=True)
class PendingMatch:
    line_no: int
    kind: str
    event: int
    target: str
    target_id: str
    resolved: bool
    caster: str | None
    caster_id: str | None
    source: str | None
    batch: int | None
    action_id: int | None
    confidence: str | None
    score: int | None
    active_batches: int
    tracked_pending: int
    tracked_resolving: int


@dataclass(frozen=True)
class PendingTargetCache:
    line_no: int
    kind: str
    target: str
    target_id: str
    damage: int
    credit: int
    charge: int
    result_kind: int
    phase: int
    cache_text: str


@dataclass(frozen=True)
class ImmediateCandidate:
    line_no: int
    target: str
    target_id: str
    old_debit: int
    old_credit: int
    selected: bool
    caster: str | None
    caster_id: str | None
    action_id: int | None
    score: int | None
    margin: int | None


@dataclass(frozen=True)
class FormulaCandidate:
    line_no: int
    event: int
    target: str
    target_id: str
    old_debit: int
    old_credit: int
    attacker: str
    source: str
    body: str
    action_text: str


@dataclass(frozen=True)
class TargetCacheSourceHint:
    cache_line_no: int
    formula_line_no: int
    distance: int
    target: str
    target_id: str
    damage: int
    attacker: str
    source: str
    action_ids: tuple[int, ...]
    action_text: str


@dataclass(frozen=True)
class RegisterActorRef:
    source: str
    actor_ptr: str
    unit_id: str
    unit_ptr: str
    action_id: int


@dataclass(frozen=True)
class HookRegsEvent:
    line_no: int
    kind: str
    event: int
    hook_ptr: str
    target: str
    target_id: str
    actor_refs: tuple[RegisterActorRef, ...]


@dataclass(frozen=True)
class LandmarkHit:
    line_no: int
    event: int
    name: str
    rva: int
    actor_refs: tuple[RegisterActorRef, ...]


@dataclass(frozen=True)
class SelectorActorRef:
    source: str
    actor_ptr: str
    role: str
    unit_id: str
    unit_ptr: str
    action_id: int


@dataclass(frozen=True)
class SelectorProbe:
    line_no: int
    event: int
    evade_type: int
    evade_name: str
    actor: str
    record: str
    unit_id: str | None
    rec_1bb: int | None
    rec_1be: int | None
    rec_1c0: int | None
    rec_dmg: int | None
    rec_1e5: int | None
    control: bool
    actor_refs: tuple[SelectorActorRef, ...]


@dataclass(frozen=True)
class SelectorFallbackHint:
    actor_line_no: int
    selector_line_no: int
    distance: int
    event: int
    target_id: str
    debit: int
    selector_event: int
    selector_evade_type: int
    selector_evade_name: str
    source_refs: tuple[SelectorActorRef, ...]
    target_refs: tuple[SelectorActorRef, ...]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    parser.add_argument("-o", "--output", type=Path, help="Write markdown report to this path.")
    parser.add_argument("--abilities", type=Path, default=DEFAULT_ABILITIES)
    parser.add_argument("--strict", action="store_true", help="Exit nonzero when the report has hard failures.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.log.exists():
        raise SystemExit(f"log not found: {args.log}")

    abilities = load_abilities(args.abilities)
    parsed = parse_log(args.log)
    report = render_report(args.log, abilities, parsed)

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8")
        print(f"wrote {args.output}")
    else:
        print(report)

    if args.strict and hard_failures(abilities, parsed):
        return 1
    return 0


def load_abilities(path: Path) -> dict[int, str]:
    if not path.exists():
        return {}

    rows: dict[int, str] = {}
    with path.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            try:
                ability_id = int(row.get("Id", ""))
            except ValueError:
                continue
            name = (row.get("Name") or "").strip()
            rows[ability_id] = name or f"ability#{ability_id}"
    return rows


def parse_log(path: Path) -> dict[str, object]:
    actor_contexts: list[ActorContext] = []
    pending_matches: list[PendingMatch] = []
    pending_target_caches: list[PendingTargetCache] = []
    pending_track_ids: Counter[int] = Counter()
    immediate_candidates: list[ImmediateCandidate] = []
    formula_candidates: list[FormulaCandidate] = []
    formula_runtime_actions: Counter[str] = Counter()
    equip_events: Counter[tuple[int, str]] = Counter()
    action_state_ids: Counter[int] = Counter()
    hp_events: Counter[int] = Counter()
    selector_probes: list[SelectorProbe] = []
    hook_regs_events: list[HookRegsEvent] = []
    landmark_hits: list[LandmarkHit] = []

    for line_no, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), start=1):
        if m := ACTOR_CTX_RE.search(line):
            caster = CASTER_RE.search(m.group("body"))
            actor_contexts.append(
                ActorContext(
                    line_no=line_no,
                    event=int(m.group("event")),
                    now=int(m.group("now")),
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    old_debit=int(m.group("old_debit")),
                    caster=caster.group("caster") if caster else None,
                    caster_id=caster.group("caster_id") if caster else None,
                    caster_actor=caster.group("actor") if caster else None,
                    action_id=int(m.group("action_id")),
                    verdict=m.group("verdict"),
                    actors=m.group("actors"),
                )
            )
            continue

        if m := PENDING_MATCH_RE.search(line):
            resolved = PENDING_RESOLVED_RE.search(m.group("body"))
            body = m.group("body")
            pending_matches.append(
                PendingMatch(
                    line_no=line_no,
                    kind=m.group("kind"),
                    event=int(m.group("event")),
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    resolved=resolved is not None,
                    caster=resolved.group("caster") if resolved else None,
                    caster_id=resolved.group("caster_id") if resolved else None,
                    source=resolved.group("source") if resolved else None,
                    batch=int(resolved.group("batch")) if resolved else None,
                    action_id=int(resolved.group("action_id")) if resolved else None,
                    confidence=resolved.group("confidence") if resolved else None,
                    score=int(resolved.group("score")) if resolved else None,
                    active_batches=int_field(body, "activeBatches"),
                    tracked_pending=int_field(body, "trackedPending"),
                    tracked_resolving=int_field(body, "trackedResolving"),
                )
            )
            continue

        if m := PENDING_TARGET_RE.search(line):
            cache_text = extract_pending_target_cache_text(m.group("body"))
            pending_target_caches.append(
                PendingTargetCache(
                    line_no=line_no,
                    kind=m.group("kind"),
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    damage=int_field(cache_text, "dmg1C4"),
                    credit=int_field(cache_text, "cred1C6"),
                    charge=int_field(cache_text, "chg1D8"),
                    result_kind=int_field(cache_text, "f1E5"),
                    phase=int_field(cache_text, "bb"),
                    cache_text=cache_text,
                )
            )
            continue

        if m := PENDING_TRACK_RE.search(line):
            pending_track_ids[int(m.group("action_id"))] += 1
            continue

        if m := IMMEDIATE_RE.search(line):
            selected = IMMEDIATE_SELECTED_RE.search(m.group("selected"))
            immediate_candidates.append(
                ImmediateCandidate(
                    line_no=line_no,
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    old_debit=int(m.group("old_debit")),
                    old_credit=int(m.group("old_credit")),
                    selected=selected is not None,
                    caster=selected.group("caster") if selected else None,
                    caster_id=selected.group("caster_id") if selected else None,
                    action_id=int(selected.group("action_id")) if selected else None,
                    score=int(selected.group("score")) if selected else None,
                    margin=int(selected.group("margin")) if selected else None,
                )
            )
            continue

        if m := FORMULA_CANDIDATE_RE.search(line):
            formula_candidates.append(
                FormulaCandidate(
                    line_no=line_no,
                    event=int(m.group("event")),
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    old_debit=int(m.group("old_debit")),
                    old_credit=int(m.group("old_credit")),
                    attacker=m.group("attacker"),
                    source=m.group("source"),
                    body=m.group("body"),
                    action_text=m.group("action"),
                )
            )
            continue

        if m := RUNTIME_ACTION_RE.search(line):
            formula_runtime_actions[m.group("action")] += 1
            continue

        if m := EQUIP_RE.search(line):
            equip_events[(int(m.group("event")), m.group("side"))] += 1
            continue

        if m := ACTION_STATE_RE.search(line):
            action_state_ids[int(m.group("action_id"))] += 1
            continue

        if m := HP_EVENT_RE.search(line):
            hp_events[int(m.group("event"))] += 1
            for action_id in action_ids_from_text(m.group("action")):
                action_state_ids[action_id] += 1
            continue

        if m := SELECTOR_RE.search(line):
            unit_id = SELECTOR_UNIT_ID_RE.search(m.group("unit_text"))
            selector_probes.append(
                SelectorProbe(
                    line_no=line_no,
                    event=int(m.group("event")),
                    evade_type=int(m.group("evade_type"), 16),
                    evade_name=m.group("evade_name"),
                    actor=m.group("actor"),
                    record=m.group("record"),
                    unit_id=unit_id.group("unit_id") if unit_id else None,
                    rec_1bb=hex_field(m.group("rec_1bb")),
                    rec_1be=hex_field(m.group("rec_1be")),
                    rec_1c0=hex_field(m.group("rec_1c0")),
                    rec_dmg=int_field_or_none(m.group("rec_dmg")),
                    rec_1e5=hex_field(m.group("rec_1e5")),
                    control="[CONTROL " in line,
                    actor_refs=parse_selector_actor_refs(line, m.group("actor"), m.group("actor_text")),
                )
            )
            continue

        if m := HOOK_REGS_EVENT_RE.search(line):
            hook_regs_events.append(
                HookRegsEvent(
                    line_no=line_no,
                    kind=m.group("kind"),
                    event=int(m.group("event")),
                    hook_ptr=m.group("hook_ptr"),
                    target=m.group("target"),
                    target_id=m.group("target_id"),
                    actor_refs=parse_register_actor_refs(m.group("body")),
                )
            )
            continue

        if m := LANDMARK_HIT_RE.search(line):
            landmark_hits.append(
                LandmarkHit(
                    line_no=line_no,
                    event=int(m.group("event")),
                    name=m.group("name"),
                    rva=int(m.group("rva"), 16),
                    actor_refs=parse_register_actor_refs(m.group("body")),
                )
            )

    return {
        "actor_contexts": actor_contexts,
        "pending_matches": pending_matches,
        "pending_target_caches": pending_target_caches,
        "pending_track_ids": pending_track_ids,
        "immediate_candidates": immediate_candidates,
        "formula_candidates": formula_candidates,
        "formula_runtime_actions": formula_runtime_actions,
        "equip_events": equip_events,
        "action_state_ids": action_state_ids,
        "hp_events": hp_events,
        "selector_probes": selector_probes,
        "hook_regs_events": hook_regs_events,
        "landmark_hits": landmark_hits,
    }


def render_report(path: Path, abilities: dict[int, str], parsed: dict[str, object]) -> str:
    actor_contexts: list[ActorContext] = parsed["actor_contexts"]  # type: ignore[assignment]
    pending_matches: list[PendingMatch] = parsed["pending_matches"]  # type: ignore[assignment]
    pending_target_caches: list[PendingTargetCache] = parsed["pending_target_caches"]  # type: ignore[assignment]
    pending_track_ids: Counter[int] = parsed["pending_track_ids"]  # type: ignore[assignment]
    immediate_candidates: list[ImmediateCandidate] = parsed["immediate_candidates"]  # type: ignore[assignment]
    formula_candidates: list[FormulaCandidate] = parsed["formula_candidates"]  # type: ignore[assignment]
    formula_runtime_actions: Counter[str] = parsed["formula_runtime_actions"]  # type: ignore[assignment]
    equip_events: Counter[tuple[int, str]] = parsed["equip_events"]  # type: ignore[assignment]
    action_state_ids: Counter[int] = parsed["action_state_ids"]  # type: ignore[assignment]
    selector_probes: list[SelectorProbe] = parsed["selector_probes"]  # type: ignore[assignment]
    hook_regs_events: list[HookRegsEvent] = parsed["hook_regs_events"]  # type: ignore[assignment]
    landmark_hits: list[LandmarkHit] = parsed["landmark_hits"]  # type: ignore[assignment]

    lines: list[str] = [
        "# Action Identity Log Analysis",
        "",
        f"Log: `{path}`",
        "",
        "## Summary",
        "",
    ]

    actor_verdicts = Counter(ctx.verdict for ctx in actor_contexts)
    resolved_actor = sum(1 for ctx in actor_contexts if is_resolved_verdict(ctx.verdict))
    actor_with_known_actions = sum(
        1 for ctx in actor_contexts if is_resolved_verdict(ctx.verdict) and is_known_action(ctx.action_id, abilities)
    )
    pending_resolved = sum(1 for match in pending_matches if match.resolved)
    immediate_selected = sum(1 for candidate in immediate_candidates if candidate.selected)
    preapply_target_caches = [cache for cache in pending_target_caches if is_preapply_damage_target_cache(cache)]
    target_cache_source_hints = correlate_target_cache_source_hints(pending_target_caches, formula_candidates)
    hinted_cache_lines = {hint.cache_line_no for hint in target_cache_source_hints}
    selector_fallback_hints = correlate_selector_fallback_hints(actor_contexts, selector_probes)
    lines.extend(
        [
            f"- Pre-clamp actor contexts: {len(actor_contexts)} (`resolved`={resolved_actor}, `ambiguous`={actor_verdicts.get('ambiguous', 0)}, `none`={actor_verdicts.get('no-caster-actor', 0)}).",
            f"- Actor contexts with known ability/basic ids: {actor_with_known_actions}.",
            f"- Pending matches: {len(pending_matches)} (`resolved`={pending_resolved}).",
            f"- Pending target caches: {len(pending_target_caches)} (`pre-apply damage candidates`={len(preapply_target_caches)}).",
            f"- Immediate candidate snapshots: {len(immediate_candidates)} (`selected`={immediate_selected}).",
            f"- Formula candidates: {len(formula_candidates)}.",
            f"- Selector probes: {len(selector_probes)}.",
            f"- Hook-reg events: {len(hook_regs_events)} (`targetcache`={sum(1 for event in hook_regs_events if event.kind == 'targetcache')}).",
            f"- Landmark hits: {len(landmark_hits)}.",
            "",
        ]
    )

    issues = classify_issues(abilities, actor_contexts, pending_matches, immediate_candidates, pending_track_ids)
    lines.extend(["## Readiness Signals", ""])
    if not issues:
        lines.append("- No hard action-identity gaps detected in this log.")
    else:
        lines.extend(f"- {issue}" for issue in issues)
    no_hp_selector = [probe for probe in selector_probes if is_no_hp_selector_outcome(probe)]
    if no_hp_selector:
        with_source = sum(1 for probe in no_hp_selector if selector_source_actor_refs(probe))
        lines.append(f"- Selector no-HP outcomes with non-target source actor refs: {with_source}/{len(no_hp_selector)}.")
        without_source = len(no_hp_selector) - with_source
        if without_source:
            lines.append(f"- Selector no-HP outcome(s) without source actor refs: {without_source}. These may need pending/current-action fallback.")
    if preapply_target_caches:
        lines.append(
            f"- Pre-apply damage target-cache candidate(s): {len(preapply_target_caches)}. "
            "These may include interrupted/cancelled incoming actions and need register-backed source proof."
        )
    if target_cache_source_hints:
        lines.append(
            f"- Pre-apply target-cache source hint(s): {len(target_cache_source_hints)} "
            f"across {len(hinted_cache_lines)} cache(s). These are line-near formula correlations, not primary proof."
        )
    if selector_fallback_hints:
        lines.append(
            f"- Selector fallback source hint(s): {len(selector_fallback_hints)} unresolved positive-debit actor context(s) "
            "had a nearby selector frame with non-target source actor refs."
        )
    self_hit_hints = find_self_hit_actor_context_hints(actor_contexts, pending_matches)
    if self_hit_hints:
        lines.append(f"- Legacy self-hit/AoE actor-context hint(s): {len(self_hit_hints)}. Retest with `resolved-self` probe.")
    lines.append("")

    lines.extend(render_action_id_table("Actor Context Action IDs", count_actor_actions(actor_contexts), abilities))
    lines.extend(render_action_id_table("Pending Action IDs", count_pending_actions(pending_matches, pending_track_ids), abilities))
    lines.extend(render_action_id_table("Immediate Candidate Action IDs", count_immediate_actions(immediate_candidates), abilities))
    lines.extend(render_action_id_table("Action-State IDs", action_state_ids, abilities))
    lines.extend(render_pending_target_cache_summary(pending_target_caches))
    lines.extend(render_target_cache_source_hints(target_cache_source_hints, abilities))
    lines.extend(render_selector_fallback_hints(selector_fallback_hints, abilities))
    lines.extend(render_targetcache_register_verdict(hook_regs_events, landmark_hits, abilities))
    lines.extend(render_register_event_summary(hook_regs_events, landmark_hits))
    lines.extend(render_selector_summary(selector_probes))

    lines.extend(["## Actor Context Events", ""])
    if not actor_contexts:
        lines.append("- No `[PRECLAMP-ACTOR-CTX]` records found.")
    else:
        lines.append("| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |")
        lines.append("| ---: | ---: | --- | --- | --- | ---: | --- | --- |")
        for ctx in actor_contexts[:80]:
            target = f"{ctx.target}/{ctx.target_id}"
            caster = f"{ctx.caster}/{ctx.caster_id}" if ctx.caster else "none"
            equip = equip_summary(ctx.event, equip_events)
            lines.append(
                f"| {ctx.line_no} | {ctx.event} | `{target}` | `{caster}` | {format_action(ctx.action_id, abilities)} | {ctx.old_debit} | `{ctx.verdict}` | {equip} |"
            )
        if len(actor_contexts) > 80:
            lines.append(f"| ... | ... | ... | ... | ... | ... | ... | +{len(actor_contexts) - 80} more |")
    lines.append("")

    lines.extend(["## Pending Matches", ""])
    if not pending_matches:
        lines.append("- No `[PENDING-ACTION-MATCH]` records found.")
    else:
        lines.extend(render_pending_contention_summary(pending_matches))
        lines.append("| Line | Event | Kind | Target | Caster | Action | Confidence | Score | Active | Pending | Resolving |")
        lines.append("| ---: | ---: | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: |")
        for match in pending_matches[:80]:
            target = f"{match.target}/{match.target_id}"
            caster = f"{match.caster}/{match.caster_id}" if match.caster else "none"
            action = format_action(match.action_id, abilities) if match.action_id is not None else "`none`"
            score = "" if match.score is None else str(match.score)
            confidence = match.confidence or "none"
            lines.append(
                f"| {match.line_no} | {match.event} | `{match.kind}` | `{target}` | `{caster}` | {action} | `{confidence}` | {score} | "
                f"{match.active_batches} | {match.tracked_pending} | {match.tracked_resolving} |"
            )
        if len(pending_matches) > 80:
            lines.append(f"| ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | +{len(pending_matches) - 80} more |")
    lines.append("")

    lines.extend(["## Immediate Candidates", ""])
    if not immediate_candidates:
        lines.append("- No `[PRECLAMP-IMMEDIATE-CANDIDATES]` records found.")
    else:
        lines.append("| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |")
        lines.append("| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |")
        for candidate in immediate_candidates[:80]:
            target = f"{candidate.target}/{candidate.target_id}"
            caster = f"{candidate.caster}/{candidate.caster_id}" if candidate.caster else "none"
            action = format_action(candidate.action_id, abilities) if candidate.action_id is not None else "`none`"
            score = "" if candidate.score is None else str(candidate.score)
            margin = "" if candidate.margin is None else str(candidate.margin)
            lines.append(
                f"| {candidate.line_no} | `{target}` | `{caster}` | {action} | {candidate.old_debit} | {candidate.old_credit} | {score} | {margin} |"
            )
        if len(immediate_candidates) > 80:
            lines.append(f"| ... | ... | ... | ... | ... | ... | ... | +{len(immediate_candidates) - 80} more |")
    lines.append("")

    lines.extend(["## Formula Candidate Sources", ""])
    if not formula_candidates:
        lines.append("- No `[PRECLAMP-FORMULA-CANDIDATE]` records found.")
    else:
        source_counts = Counter(candidate.source for candidate in formula_candidates)
        lines.extend(f"- `{source}`: {count}" for source, count in sorted(source_counts.items()))
    lines.append("")

    if formula_runtime_actions:
        lines.extend(["## Runtime Action Signals", ""])
        for action, count in formula_runtime_actions.most_common(20):
            ids = ", ".join(format_action(action_id, abilities) for action_id in action_ids_from_text(action))
            suffix = f" ({ids})" if ids else ""
            lines.append(f"- `{action}`: {count}{suffix}")
        lines.append("")

    return "\n".join(lines)


def render_pending_contention_summary(pending_matches: list[PendingMatch]) -> list[str]:
    max_active = max((match.active_batches for match in pending_matches), default=0)
    max_pending = max((match.tracked_pending for match in pending_matches), default=0)
    max_resolving = max((match.tracked_resolving for match in pending_matches), default=0)
    resolved_under_contention = sum(
        1 for match in pending_matches
        if match.resolved and (match.active_batches > 1 or match.tracked_pending > 1 or match.tracked_resolving > 1)
    )
    contention_rows = sum(
        1 for match in pending_matches
        if match.active_batches > 1 or match.tracked_pending > 1 or match.tracked_resolving > 1
    )
    resolved_batches = Counter(
        (match.batch, match.action_id)
        for match in pending_matches
        if match.resolved and match.batch is not None
    )

    lines = [
        f"- Max pending contention: active={max_active}, trackedPending={max_pending}, trackedResolving={max_resolving}.",
        f"- Rows under contention: {contention_rows}; resolved under contention: {resolved_under_contention}.",
    ]
    if resolved_batches:
        sample = ", ".join(
            f"batch={batch}/act={action_id} x{count}"
            for (batch, action_id), count in resolved_batches.most_common(6)
        )
        lines.append(f"- Resolved batch/action sample: {sample}.")
    lines.append("")
    return lines


def render_pending_target_cache_summary(caches: list[PendingTargetCache]) -> list[str]:
    lines = ["## Pending Target Caches", ""]
    if not caches:
        lines.append("- No `[PENDING-ACTION-TARGET]` records found.")
        lines.append("")
        return lines

    preapply = [cache for cache in caches if is_preapply_damage_target_cache(cache)]
    if preapply:
        lines.append(
            f"- Pre-apply damage candidates (`dmg1C4 > 0`, damage result flag, `bb != 2`): {len(preapply)}."
        )
        lines.append("")

    lines.append("| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |")
    lines.append("| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |")
    for cache in caches[:80]:
        target = f"{cache.target}/{cache.target_id}"
        candidate = "pre-apply damage" if is_preapply_damage_target_cache(cache) else ""
        lines.append(
            f"| {cache.line_no} | `{cache.kind}` | `{target}` | {cache.damage} | {cache.credit} | "
            f"{cache.charge} | `0x{cache.result_kind:02X}` | {cache.phase} | {candidate} |"
        )
    if len(caches) > 80:
        lines.append(f"| ... | ... | ... | ... | ... | ... | ... | ... | +{len(caches) - 80} more |")
    lines.append("")
    return lines


def render_target_cache_source_hints(hints: list[TargetCacheSourceHint], abilities: dict[int, str]) -> list[str]:
    lines = ["## Target Cache Source Hints", ""]
    if not hints:
        lines.append("- No line-near source hints found for pre-apply damage target caches.")
        lines.append("")
        return lines

    lines.append(
        "These rows correlate a pre-apply target cache to nearby formula candidates with the same target and damage. "
        "They are useful for narrowing interrupted-action cases, but they are not register-backed proof."
    )
    lines.append("")
    lines.append("| Cache line | Formula line | Distance | Target | Damage | Attacker | Source | Action hints |")
    lines.append("| ---: | ---: | ---: | --- | ---: | --- | --- | --- |")
    for hint in hints[:80]:
        target = f"{hint.target}/{hint.target_id}"
        action_hint = ", ".join(format_action(action_id, abilities) for action_id in hint.action_ids) if hint.action_ids else ""
        lines.append(
            f"| {hint.cache_line_no} | {hint.formula_line_no} | {hint.distance} | `{target}` | {hint.damage} | "
            f"`{hint.attacker}` | `{hint.source}` | {action_hint} |"
        )
    if len(hints) > 80:
        lines.append(f"| ... | ... | ... | ... | ... | ... | ... | +{len(hints) - 80} more |")
    lines.append("")
    return lines


def render_selector_fallback_hints(hints: list[SelectorFallbackHint], abilities: dict[int, str]) -> list[str]:
    lines = ["## Selector Fallback Hints", ""]
    if not hints:
        lines.append("- No unresolved positive-debit actor contexts had nearby selector source refs.")
        lines.append("")
        return lines

    lines.append(
        "These rows correlate an unresolved positive-debit `[PRECLAMP-ACTOR-CTX]` with a nearby "
        "`[SELECTOR-PROBE]` for the same target and staged damage. The selector frame runs too late "
        "to compute that same pre-clamp rewrite, but it is strong evidence for no-HP outcomes, "
        "reaction/cancel diagnostics, and fallback design."
    )
    lines.append("")
    lines.append("| Actor line | Selector line | Distance | Target | Debit | Selector | Source actor refs | Target/self refs | Actions |")
    lines.append("| ---: | ---: | ---: | --- | ---: | --- | --- | --- | --- |")
    for hint in hints[:80]:
        actions = Counter(ref.action_id for ref in hint.source_refs if ref.action_id >= 0)
        action_text = ", ".join(
            f"{format_action(action_id, abilities)} x{count}"
            for action_id, count in sorted(actions.items())
        )
        lines.append(
            f"| {hint.actor_line_no} | {hint.selector_line_no} | {hint.distance} | `{hint.target_id}` | "
            f"{hint.debit} | `0x{hint.selector_evade_type:02X}` {hint.selector_evade_name} | "
            f"{format_selector_actor_refs(hint.source_refs)} | {format_selector_actor_refs(hint.target_refs)} | {action_text} |"
        )
    if len(hints) > 80:
        lines.append(f"| ... | ... | ... | ... | ... | ... | ... | ... | +{len(hints) - 80} more |")
    lines.append("")
    return lines


def render_targetcache_register_verdict(
    hook_events: list[HookRegsEvent],
    landmark_hits: list[LandmarkHit],
    abilities: dict[int, str],
) -> list[str]:
    lines = ["## Target-Cache Register Verdict", ""]
    targetcache = [event for event in hook_events if event.kind == "targetcache"]
    if not targetcache:
        lines.append("- No `[HOOK-REGS-EVENT kind=targetcache]` records found.")
        if landmark_hits:
            actor_landmarks = sum(1 for hit in landmark_hits if hit.actor_refs)
            lines.append(
                f"- Landmark hits exist (`{actor_landmarks}/{len(landmark_hits)}` with unit/actor refs), "
                "but landmarks are auxiliary until target ownership is explicit."
            )
        lines.append("")
        return lines

    with_source = [(event, targetcache_source_refs(event)) for event in targetcache]
    with_source = [(event, refs) for event, refs in with_source if refs]
    lines.append(f"- Target-cache hook events with source-candidate refs: {len(with_source)}/{len(targetcache)}.")
    if with_source:
        source_action_ids = Counter(
            ref.action_id
            for _event, refs in with_source
            for ref in refs
            if ref.action_id >= 0
        )
        source_unit_only_refs = sum(
            1
            for _event, refs in with_source
            for ref in refs
            if ref.action_id < 0
        )
        lines.append(
            "- Strong candidate proof: at least one target-cache hook saw source-candidate unit/actor refs "
            "for a unit other than the target. In First Strike/Hamedo captures, this is the signal we need "
            "for the interrupted incoming source."
        )
        if source_action_ids:
            lines.append(f"- Source-candidate action ids: {format_action_counter(source_action_ids, abilities)}.")
        if any(action_id > 0 for action_id in source_action_ids):
            lines.append("- Named incoming action proof: at least one source-candidate ref carries `actionId > 0`.")
        else:
            lines.append(
                "- Named incoming action proof: not present in this capture; source-candidate refs are "
                f"basic/implicit or direct unit refs (`unit-only` refs={source_unit_only_refs})."
            )
        lines.append("")
        lines.append("| Line | Event | Hook ptr | Target | Source refs | Target/self refs |")
        lines.append("| ---: | ---: | --- | --- | --- | --- |")
        for event, refs in with_source[:40]:
            target = f"{event.target}/{event.target_id}"
            hook = event.hook_ptr
            lines.append(
                f"| {event.line_no} | {event.event} | `{hook}` | `{target}` | "
                f"{format_register_actor_refs(refs)} | {format_register_actor_refs(targetcache_target_refs(event))} |"
            )
        if len(with_source) > 40:
            lines.append(f"| ... | ... | ... | ... | +{len(with_source) - 40} more | ... |")
    else:
        actor_ref_events = sum(1 for event in targetcache if event.actor_refs)
        lines.append(
            f"- No non-target source-candidate refs found. `{actor_ref_events}/{len(targetcache)}` target-cache "
            "hook event(s) had unit/actor refs, but they were target/self only."
        )
        lines.append(
            "- If this happens in the First Strike/Hamedo capture, move the source hunt earlier than the "
            "target-cache transition."
        )
    if landmark_hits:
        actor_landmarks = sum(1 for hit in landmark_hits if hit.actor_refs)
        lines.append(f"- Landmark unit/actor refs: {actor_landmarks}/{len(landmark_hits)}. Use the detailed table below as auxiliary evidence.")
    lines.append("")
    return lines


def render_register_event_summary(hook_events: list[HookRegsEvent], landmark_hits: list[LandmarkHit]) -> list[str]:
    lines = ["## Register Unit/Actor Refs", ""]
    targetcache = [event for event in hook_events if event.kind == "targetcache"]
    if not hook_events and not landmark_hits:
        lines.append("- No parsed hook-reg or landmark unit/actor refs found.")
        lines.append("")
        return lines

    if targetcache:
        lines.append("Target-cache hook events:")
        lines.append("")
        lines.append("| Line | Event | Target | Unit/actor refs |")
        lines.append("| ---: | ---: | --- | --- |")
        for event in targetcache[:40]:
            target = f"{event.target}/{event.target_id}"
            lines.append(f"| {event.line_no} | {event.event} | `{target}` | {format_register_actor_refs(event.actor_refs)} |")
        if len(targetcache) > 40:
            lines.append(f"| ... | ... | ... | +{len(targetcache) - 40} more |")
        lines.append("")

    actor_landmarks = [hit for hit in landmark_hits if hit.actor_refs]
    if actor_landmarks:
        lines.append("Landmark unit/actor refs:")
        lines.append("")
        lines.append("| Line | Event | Name | RVA | Unit/actor refs |")
        lines.append("| ---: | ---: | --- | ---: | --- |")
        for hit in actor_landmarks[:40]:
            lines.append(f"| {hit.line_no} | {hit.event} | `{hit.name}` | `0x{hit.rva:X}` | {format_register_actor_refs(hit.actor_refs)} |")
        if len(actor_landmarks) > 40:
            lines.append(f"| ... | ... | ... | ... | +{len(actor_landmarks) - 40} more |")
        lines.append("")
    elif landmark_hits:
        lines.append(f"- Landmark hits parsed: {len(landmark_hits)}, but none contained unit/actor refs.")
        lines.append("")

    return lines


def targetcache_source_refs(event: HookRegsEvent) -> tuple[RegisterActorRef, ...]:
    if event.hook_ptr.lower() != event.target.lower():
        hook_refs = tuple(ref for ref in event.actor_refs if ref.unit_ptr.lower() == event.hook_ptr.lower())
        if hook_refs:
            return hook_refs
    return tuple(
        ref for ref in event.actor_refs
        if ref.unit_id != event.target_id and ref.unit_ptr.lower() != event.target.lower()
    )


def targetcache_target_refs(event: HookRegsEvent) -> tuple[RegisterActorRef, ...]:
    return tuple(
        ref for ref in event.actor_refs
        if ref.unit_id == event.target_id or ref.unit_ptr.lower() == event.target.lower()
    )


def render_selector_summary(selector_probes: list[SelectorProbe]) -> list[str]:
    lines = ["## Selector Outcomes", ""]
    if not selector_probes:
        lines.append("- No `[SELECTOR-PROBE]` records found.")
        lines.append("")
        return lines

    outcome_counts = Counter((probe.evade_type, probe.evade_name) for probe in selector_probes)
    lines.append("| Evade type | Meaning | Count |")
    lines.append("| ---: | --- | ---: |")
    for (evade_type, evade_name), count in sorted(outcome_counts.items()):
        lines.append(f"| `0x{evade_type:02X}` | {evade_name} | {count} |")
    lines.append("")

    actor_ref_counts = Counter(ref.action_id for probe in selector_probes for ref in probe.actor_refs if ref.action_id >= 0)
    if actor_ref_counts:
        lines.append("Selector-frame actor/action ids:")
        lines.append("")
        lines.append("| Action id | Count |")
        lines.append("| ---: | ---: |")
        for action_id, count in sorted(actor_ref_counts.items()):
            lines.append(f"| `{action_id}` | {count} |")
        lines.append("")

    no_hp = [probe for probe in selector_probes if is_no_hp_selector_outcome(probe)]
    if no_hp:
        lines.append("No-HP selector context:")
        lines.append("")
        lines.append("| Event | Evade | Unit | Source actor refs | Target/self refs |")
        lines.append("| ---: | --- | --- | --- | --- |")
        for probe in no_hp[:40]:
            source_refs = selector_source_actor_refs(probe)
            self_refs = selector_target_actor_refs(probe)
            lines.append(
                f"| {probe.event} | `0x{probe.evade_type:02X}` {probe.evade_name} | `{probe.unit_id or 'none'}` | "
                f"{format_selector_actor_refs(source_refs)} | {format_selector_actor_refs(self_refs)} |"
            )
        if len(no_hp) > 40:
            lines.append(f"| ... | ... | ... | ... | +{len(no_hp) - 40} more |")
        lines.append("")

    lines.append("| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |")
    lines.append("| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |")
    for probe in selector_probes[:80]:
        unit = probe.unit_id or "none"
        lines.append(
            f"| {probe.line_no} | {probe.event} | `{unit}` | `0x{probe.evade_type:02X}` {probe.evade_name} | "
            f"{format_optional_hex(probe.rec_1be)} | {format_optional_hex(probe.rec_1c0)} | "
            f"{'' if probe.rec_dmg is None else probe.rec_dmg} | {format_optional_hex(probe.rec_1e5)} | "
            f"{format_selector_actor_refs(probe.actor_refs)} | "
            f"{'yes' if probe.control else ''} |"
        )
    if len(selector_probes) > 80:
        lines.append(f"| ... | ... | ... | ... | ... | ... | ... | ... | ... | +{len(selector_probes) - 80} more |")
    lines.append("")
    return lines


def classify_issues(
    abilities: dict[int, str],
    actor_contexts: list[ActorContext],
    pending_matches: list[PendingMatch],
    immediate_candidates: list[ImmediateCandidate],
    pending_track_ids: Counter[int] | None = None,
) -> list[str]:
    issues: list[str] = []
    if not actor_contexts:
        issues.append("Missing primary evidence: no `[PRECLAMP-ACTOR-CTX]` lines.")
    elif not any(is_resolved_verdict(ctx.verdict) for ctx in actor_contexts):
        issues.append("Primary evidence weak: actor context never resolved a caster.")

    ambiguous = [ctx for ctx in actor_contexts if ctx.verdict == "ambiguous"]
    if ambiguous:
        issues.append(f"Actor context ambiguity present: {len(ambiguous)} event(s).")

    self_hit_hints = set(find_self_hit_actor_context_hints(actor_contexts, pending_matches))
    unresolved_debits = [
        ctx for ctx in actor_contexts
        if ctx.old_debit > 0 and not is_resolved_verdict(ctx.verdict) and ctx not in self_hit_hints
    ]
    if unresolved_debits:
        issues.append(f"Actor context unresolved for positive-debit event(s): {len(unresolved_debits)}.")

    unknown_actor_ids = sorted({ctx.action_id for ctx in actor_contexts if ctx.action_id >= 0 and not is_known_action(ctx.action_id, abilities)})
    if unknown_actor_ids:
        issues.append("Actor context emitted action id(s) not in baseline ability table: " + ", ".join(str(value) for value in unknown_actor_ids) + ".")

    has_pending_evidence = bool(pending_track_ids) or any(
        match.active_batches > 0 or match.tracked_pending > 0 or match.tracked_resolving > 0
        for match in pending_matches
    )
    if has_pending_evidence and pending_matches and not any(match.resolved for match in pending_matches):
        issues.append("Pending tracker evidence exists but no pending match resolved.")

    unknown_pending = sorted(
        {
            match.action_id
            for match in pending_matches
            if match.action_id is not None and match.action_id >= 0 and not is_known_action(match.action_id, abilities)
        }
    )
    if unknown_pending:
        issues.append("Pending tracker emitted action id(s) not in baseline ability table: " + ", ".join(str(value) for value in unknown_pending) + ".")

    if immediate_candidates and not any(candidate.selected for candidate in immediate_candidates):
        issues.append("Immediate candidate snapshots exist but none selected a source.")

    return issues


def int_field(text: str, name: str) -> int:
    match = re.search(rf"\b{re.escape(name)}=(?P<value>-?\d+)", text)
    if not match:
        return 0
    try:
        return int(match.group("value"))
    except ValueError:
        return 0


def extract_pending_target_cache_text(body: str) -> str:
    selected = False
    for marker in (" next=", " prev=", " last="):
        index = body.find(marker)
        if index >= 0:
            body = body[index + len(marker):]
            selected = True
            break
    if selected:
        for marker in (" touch=", " reason=", " age=", " clearAge=", " lastSeen="):
            index = body.find(marker)
            if index > 0:
                body = body[:index]
    return body.strip()


def is_damage_target_cache(cache: PendingTargetCache) -> bool:
    return cache.damage > 0 and (cache.result_kind & 0x80) != 0


def is_preapply_damage_target_cache(cache: PendingTargetCache) -> bool:
    return is_damage_target_cache(cache) and cache.phase != 2


def correlate_target_cache_source_hints(
    caches: list[PendingTargetCache],
    formula_candidates: list[FormulaCandidate],
    max_distance: int = 25,
) -> list[TargetCacheSourceHint]:
    hints: list[TargetCacheSourceHint] = []
    preapply = [cache for cache in caches if is_preapply_damage_target_cache(cache)]
    for cache in preapply:
        for candidate in formula_candidates:
            distance = candidate.line_no - cache.line_no
            if distance < 0 or distance > max_distance:
                continue
            if candidate.target != cache.target or candidate.target_id != cache.target_id:
                continue
            if candidate.old_debit != cache.damage:
                continue
            hints.append(
                TargetCacheSourceHint(
                    cache_line_no=cache.line_no,
                    formula_line_no=candidate.line_no,
                    distance=distance,
                    target=cache.target,
                    target_id=cache.target_id,
                    damage=cache.damage,
                    attacker=candidate.attacker,
                    source=candidate.source,
                    action_ids=tuple(action_ids_from_text(candidate.action_text)),
                    action_text=candidate.action_text,
                )
            )
    return sorted(hints, key=lambda hint: (hint.cache_line_no, hint.distance, hint.formula_line_no))


def correlate_selector_fallback_hints(
    actor_contexts: list[ActorContext],
    selector_probes: list[SelectorProbe],
    max_distance: int = 35,
) -> list[SelectorFallbackHint]:
    hints: list[SelectorFallbackHint] = []
    unresolved = [
        ctx for ctx in actor_contexts
        if ctx.old_debit > 0 and not is_resolved_verdict(ctx.verdict)
    ]
    for ctx in unresolved:
        candidates: list[SelectorFallbackHint] = []
        for probe in selector_probes:
            distance = probe.line_no - ctx.line_no
            if distance < 0 or distance > max_distance:
                continue
            if probe.unit_id != ctx.target_id:
                continue
            if probe.rec_dmg != ctx.old_debit:
                continue
            source_refs = selector_source_actor_refs(probe)
            if not source_refs:
                continue
            candidates.append(
                SelectorFallbackHint(
                    actor_line_no=ctx.line_no,
                    selector_line_no=probe.line_no,
                    distance=distance,
                    event=ctx.event,
                    target_id=ctx.target_id,
                    debit=ctx.old_debit,
                    selector_event=probe.event,
                    selector_evade_type=probe.evade_type,
                    selector_evade_name=probe.evade_name,
                    source_refs=source_refs,
                    target_refs=selector_target_actor_refs(probe),
                )
            )
        if candidates:
            hints.append(sorted(candidates, key=lambda hint: (hint.distance, hint.selector_line_no))[0])
    return sorted(hints, key=lambda hint: (hint.actor_line_no, hint.selector_line_no))


def parse_register_actor_refs(text: str) -> tuple[RegisterActorRef, ...]:
    refs: list[RegisterActorRef] = []
    seen: set[tuple[str, str, str, int]] = set()
    for m in REGISTER_ACTOR_REF_RE.finditer(text):
        ref = RegisterActorRef(
            source=m.group("source"),
            actor_ptr="0x" + m.group("actor_ptr"),
            unit_id=m.group("unit_id"),
            unit_ptr=m.group("unit_ptr"),
            action_id=int(m.group("action_id")),
        )
        key = (ref.source, ref.actor_ptr, ref.unit_ptr, ref.action_id)
        if key in seen:
            continue
        seen.add(key)
        refs.append(ref)
    for m in REGISTER_UNIT_REF_RE.finditer(text):
        ref = RegisterActorRef(
            source=m.group("source"),
            actor_ptr="0x" + m.group("unit_ptr"),
            unit_id=m.group("unit_id"),
            unit_ptr="0x" + m.group("unit_ptr"),
            action_id=-1,
        )
        key = (ref.source, ref.actor_ptr, ref.unit_ptr, ref.action_id)
        if key in seen:
            continue
        seen.add(key)
        refs.append(ref)
    return tuple(refs)


def format_register_actor_refs(refs: tuple[RegisterActorRef, ...]) -> str:
    if not refs:
        return ""
    compact = [
        f"`{ref.source}->{ref.unit_id}/act={ref.action_id}`" if ref.action_id >= 0 else f"`{ref.source}->{ref.unit_id}/unit`"
        for ref in refs[:8]
    ]
    if len(refs) > 8:
        compact.append(f"+{len(refs) - 8} more")
    return "<br>".join(compact)


def parse_selector_actor_refs(line: str, actor_ptr: str, actor_text: str) -> tuple[SelectorActorRef, ...]:
    refs: list[SelectorActorRef] = []
    if m := SELECTOR_ACTOR_TEXT_RE.search(actor_text):
        refs.append(
            SelectorActorRef(
                source="actor",
                actor_ptr=actor_ptr,
                role=m.group("role"),
                unit_id=m.group("unit_id"),
                unit_ptr=m.group("unit_ptr"),
                action_id=int(m.group("action_id")),
            )
        )

    if ctx := SELECTOR_CONTEXT_RE.search(line):
        for section_name in ("regs", "stack"):
            for m in SELECTOR_CONTEXT_ACTOR_RE.finditer(ctx.group(section_name)):
                refs.append(
                    SelectorActorRef(
                        source=m.group("source"),
                        actor_ptr="0x" + m.group("actor_ptr"),
                        role=m.group("role"),
                        unit_id=m.group("unit_id"),
                        unit_ptr=m.group("unit_ptr"),
                        action_id=int(m.group("action_id")),
                    )
                )

    # Preserve order, but collapse duplicates from actor= and ctxRegs/ctxStack echoing the same root.
    deduped: list[SelectorActorRef] = []
    seen: set[tuple[str, str, str, int]] = set()
    for ref in refs:
        key = (ref.source, ref.actor_ptr, ref.unit_ptr, ref.action_id)
        if key in seen:
            continue
        seen.add(key)
        deduped.append(ref)
    return tuple(deduped)


def format_selector_actor_refs(refs: tuple[SelectorActorRef, ...]) -> str:
    if not refs:
        return ""
    compact = [
        f"`{ref.source}->{ref.unit_id}/act={ref.action_id}{'/self' if ref.role == 'actor:record-unit' else ''}`"
        for ref in refs[:6]
    ]
    if len(refs) > 6:
        compact.append(f"+{len(refs) - 6} more")
    return "<br>".join(compact)


def is_no_hp_selector_outcome(probe: SelectorProbe) -> bool:
    if probe.rec_1be == 0:
        return True
    if probe.rec_1e5 == 0 and (probe.rec_dmg is None or probe.rec_dmg == 0):
        return True
    return probe.evade_type != 0 and (probe.rec_dmg is None or probe.rec_dmg == 0)


def selector_source_actor_refs(probe: SelectorProbe) -> tuple[SelectorActorRef, ...]:
    return tuple(
        ref for ref in probe.actor_refs
        if ref.role != "actor:record-unit" and (probe.unit_id is None or ref.unit_id != probe.unit_id)
    )


def selector_target_actor_refs(probe: SelectorProbe) -> tuple[SelectorActorRef, ...]:
    return tuple(
        ref for ref in probe.actor_refs
        if ref.role == "actor:record-unit" or (probe.unit_id is not None and ref.unit_id == probe.unit_id)
    )


def int_field_or_none(text: str) -> int | None:
    if text == "----":
        return None
    try:
        return int(text)
    except ValueError:
        return None


def hex_field(text: str) -> int | None:
    if "-" in text:
        return None
    try:
        return int(text, 16)
    except ValueError:
        return None


def format_optional_hex(value: int | None) -> str:
    return "" if value is None else f"`0x{value:02X}`"


def hard_failures(abilities: dict[int, str], parsed: dict[str, object]) -> bool:
    actor_contexts: list[ActorContext] = parsed["actor_contexts"]  # type: ignore[assignment]
    pending_matches: list[PendingMatch] = parsed["pending_matches"]  # type: ignore[assignment]
    immediate_candidates: list[ImmediateCandidate] = parsed["immediate_candidates"]  # type: ignore[assignment]
    return bool(classify_issues(abilities, actor_contexts, pending_matches, immediate_candidates))


def is_resolved_verdict(verdict: str) -> bool:
    return verdict == "resolved" or verdict == "resolved-self"


def find_self_hit_actor_context_hints(
    actor_contexts: list[ActorContext],
    pending_matches: list[PendingMatch],
) -> list[ActorContext]:
    pending_by_event = {
        match.event: match
        for match in pending_matches
        if match.resolved and match.caster == match.target and match.action_id is not None and match.action_id > 0
    }
    hints: list[ActorContext] = []
    for ctx in actor_contexts:
        if ctx.old_debit <= 0 or is_resolved_verdict(ctx.verdict):
            continue
        match = pending_by_event.get(ctx.event)
        if match is None:
            continue
        if match.target == ctx.target and match.caster == ctx.target:
            hints.append(ctx)
    return hints


def count_actor_actions(actor_contexts: list[ActorContext]) -> Counter[int]:
    counts: Counter[int] = Counter()
    for ctx in actor_contexts:
        if ctx.action_id >= 0:
            counts[ctx.action_id] += 1
    return counts


def count_pending_actions(pending_matches: list[PendingMatch], pending_track_ids: Counter[int]) -> Counter[int]:
    counts: Counter[int] = Counter(pending_track_ids)
    for match in pending_matches:
        if match.action_id is not None:
            counts[match.action_id] += 1
    return counts


def count_immediate_actions(candidates: list[ImmediateCandidate]) -> Counter[int]:
    counts: Counter[int] = Counter()
    for candidate in candidates:
        if candidate.action_id is not None:
            counts[candidate.action_id] += 1
    return counts


def render_action_id_table(title: str, counts: Counter[int], abilities: dict[int, str]) -> list[str]:
    lines = [f"## {title}", ""]
    if not counts:
        lines.append("- No action ids observed.")
        lines.append("")
        return lines

    lines.append("| Action id | Meaning | Count | Confidence note |")
    lines.append("| ---: | --- | ---: | --- |")
    for action_id, count in sorted(counts.items()):
        note = "basic attack / implicit weapon action" if action_id == 0 else "matches baseline ability table" if action_id in abilities else "unknown in baseline ability table"
        lines.append(f"| {action_id} | {format_action_name(action_id, abilities)} | {count} | {note} |")
    lines.append("")
    return lines


def format_action(action_id: int | None, abilities: dict[int, str]) -> str:
    if action_id is None:
        return "`none`"
    return f"`{action_id}` {format_action_name(action_id, abilities)}"


def format_action_counter(counts: Counter[int], abilities: dict[int, str]) -> str:
    if not counts:
        return "none"
    return ", ".join(
        f"{format_action(action_id, abilities)} x{count}"
        for action_id, count in sorted(counts.items())
    )


def format_action_name(action_id: int, abilities: dict[int, str]) -> str:
    if action_id == 0:
        return "Basic Attack / implicit weapon"
    name = abilities.get(action_id)
    return name if name is not None else "UNKNOWN"


def is_known_action(action_id: int, abilities: dict[int, str]) -> bool:
    return action_id == 0 or action_id in abilities


def equip_summary(event: int, equip_events: Counter[tuple[int, str]]) -> str:
    target = equip_events.get((event, "target"), 0)
    caster = equip_events.get((event, "caster"), 0)
    if target and caster:
        return "`target+caster`"
    if target:
        return "`target`"
    if caster:
        return "`caster`"
    return ""


def action_ids_from_text(text: str) -> list[int]:
    values: list[int] = []
    for match in ACTION_ID_IN_TEXT_RE.finditer(text):
        try:
            values.append(int(match.group("action_id")))
        except ValueError:
            continue
    return values


if __name__ == "__main__":
    raise SystemExit(main())
