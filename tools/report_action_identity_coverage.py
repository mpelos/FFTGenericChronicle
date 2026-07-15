#!/usr/bin/env python3
"""Aggregate action-identity evidence across battleprobe logs.

This report is intentionally higher level than analyze_action_identity_log.py:
it answers "what has the existing evidence covered?" rather than explaining one
capture in detail.
"""
from __future__ import annotations

import argparse
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

from analyze_action_identity_log import (
    ActorContext,
    ImmediateCandidate,
    PendingMatch,
    PendingTargetCache,
    HookRegsEvent,
    LandmarkHit,
    SelectorProbe,
    TargetCacheSourceHint,
    classify_issues,
    correlate_selector_fallback_hints,
    correlate_target_cache_source_hints,
    format_action_name,
    find_self_hit_actor_context_hints,
    is_preapply_damage_target_cache,
    is_no_hp_selector_outcome,
    load_abilities,
    parse_log,
    selector_source_actor_refs,
    targetcache_source_refs,
)


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORK = ROOT / "work"
DEFAULT_ABILITIES = DEFAULT_WORK / "baseline_abilities.csv"

INTERESTING_MARKERS = (
    "[PRECLAMP-ACTOR-CTX",
    "[PENDING-ACTION-TARGET",
    "[PENDING-ACTION-MATCH",
    "[PRECLAMP-IMMEDIATE-CANDIDATES",
    "[PRECLAMP-FORMULA-CANDIDATE",
    "[PRECLAMP-FORMULA-RUNTIME",
    "[HP-EVENT-PROBE",
    "[SELECTOR-PROBE",
)


@dataclass(frozen=True)
class LogCoverage:
    path: Path
    actor_contexts: int
    actor_resolved: int
    actor_resolved_self: int
    actor_unresolved_positive_debits: int
    actor_ambiguous: int
    actor_self_hit_hints: int
    pending_matches: int
    pending_resolved: int
    pending_multi_target_batches: int
    pending_max_active_batches: int
    pending_target_caches: int
    pending_target_preapply_damage_caches: int
    pending_target_source_hints: int
    pending_target_caches_with_source_hints: int
    immediate_candidates: int
    immediate_selected: int
    formula_candidates: int
    hook_regs_events: int
    targetcache_hook_events: int
    targetcache_hook_events_with_actor_refs: int
    targetcache_hook_events_with_source_refs: int
    targetcache_source_action_ids: Counter[int]
    targetcache_source_unit_only_refs: int
    landmark_hits: int
    landmark_hits_with_actor_refs: int
    selector_probes: int
    selector_outcomes: Counter[int]
    selector_with_actor_refs: int
    selector_no_hp: int
    selector_no_hp_with_source_refs: int
    selector_fallback_hints: int
    selector_no_hp_source_action_ids: Counter[int]
    selector_actor_action_ids: Counter[int]
    actor_action_ids: Counter[int]
    pending_action_ids: Counter[int]
    immediate_action_ids: Counter[int]
    action_ids: Counter[int]
    legacy_no_actor_probe: bool
    issues: list[str]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="*", type=Path, help="Specific log files or directories to scan.")
    parser.add_argument("-o", "--output", type=Path, help="Write markdown report to this path.")
    parser.add_argument("--abilities", type=Path, default=DEFAULT_ABILITIES)
    parser.add_argument("--include-uninteresting", action="store_true", help="Parse every .txt log, not only logs with action-identity markers.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    roots = args.paths or [DEFAULT_WORK]
    abilities = load_abilities(args.abilities)
    logs = [path for path in iter_logs(roots) if args.include_uninteresting or looks_interesting(path)]
    coverages = [coverage_for(path, abilities) for path in logs]
    report = render_report(coverages, abilities)

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8")
        print(f"wrote {args.output}")
    else:
        print(report)
    return 0


def iter_logs(roots: list[Path]) -> list[Path]:
    paths: list[Path] = []
    for root in roots:
        if root.is_file():
            if root.suffix.lower() == ".txt":
                paths.append(root)
            continue
        if root.is_dir():
            paths.extend(path for path in root.rglob("*.txt") if path.is_file())
    return sorted(set(paths))


def looks_interesting(path: Path) -> bool:
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return False
    return any(marker in text for marker in INTERESTING_MARKERS)


def coverage_for(path: Path, abilities: dict[int, str]) -> LogCoverage:
    parsed = parse_log(path)
    actor_contexts: list[ActorContext] = parsed["actor_contexts"]  # type: ignore[assignment]
    pending_matches: list[PendingMatch] = parsed["pending_matches"]  # type: ignore[assignment]
    pending_target_caches: list[PendingTargetCache] = parsed["pending_target_caches"]  # type: ignore[assignment]
    pending_track_ids: Counter[int] = parsed["pending_track_ids"]  # type: ignore[assignment]
    immediate_candidates: list[ImmediateCandidate] = parsed["immediate_candidates"]  # type: ignore[assignment]
    formula_candidates = parsed["formula_candidates"]
    selector_probes: list[SelectorProbe] = parsed["selector_probes"]  # type: ignore[assignment]
    hook_regs_events: list[HookRegsEvent] = parsed["hook_regs_events"]  # type: ignore[assignment]
    landmark_hits: list[LandmarkHit] = parsed["landmark_hits"]  # type: ignore[assignment]
    target_cache_source_hints: list[TargetCacheSourceHint] = correlate_target_cache_source_hints(pending_target_caches, formula_candidates)  # type: ignore[arg-type]
    selector_fallback_hints = correlate_selector_fallback_hints(actor_contexts, selector_probes)
    target_cache_hint_lines = {hint.cache_line_no for hint in target_cache_source_hints}

    actor_action_ids: Counter[int] = Counter()
    for ctx in actor_contexts:
        if ctx.action_id >= 0:
            actor_action_ids[ctx.action_id] += 1
    pending_action_ids: Counter[int] = Counter()
    for match in pending_matches:
        if match.action_id is not None and match.action_id >= 0:
            pending_action_ids[match.action_id] += 1
    immediate_action_ids: Counter[int] = Counter()
    for candidate in immediate_candidates:
        if candidate.action_id is not None and candidate.action_id >= 0:
            immediate_action_ids[candidate.action_id] += 1

    action_ids: Counter[int] = Counter()
    action_ids.update(actor_action_ids)
    action_ids.update(pending_action_ids)
    action_ids.update(immediate_action_ids)
    pending_batch_counts = Counter(match.batch for match in pending_matches if match.resolved and match.batch is not None)

    selector_actor_action_ids: Counter[int] = Counter()
    selector_no_hp_source_action_ids: Counter[int] = Counter()
    targetcache_source_action_ids: Counter[int] = Counter()
    targetcache_source_unit_only_refs = 0
    targetcache_source_ref_events = 0
    for event in hook_regs_events:
        if event.kind != "targetcache":
            continue
        refs = targetcache_source_refs(event)
        if refs:
            targetcache_source_ref_events += 1
        targetcache_source_action_ids.update(ref.action_id for ref in refs if ref.action_id >= 0)
        targetcache_source_unit_only_refs += sum(1 for ref in refs if ref.action_id < 0)

    selector_no_hp = 0
    selector_no_hp_with_source_refs = 0
    for probe in selector_probes:
        selector_actor_action_ids.update(ref.action_id for ref in probe.actor_refs if ref.action_id >= 0)
        if is_no_hp_selector_outcome(probe):
            selector_no_hp += 1
            source_refs = selector_source_actor_refs(probe)
            if source_refs:
                selector_no_hp_with_source_refs += 1
            selector_no_hp_source_action_ids.update(ref.action_id for ref in source_refs if ref.action_id >= 0)

    raw_issues = classify_issues(abilities, actor_contexts, pending_matches, immediate_candidates, pending_track_ids)
    legacy_no_actor_probe = not actor_contexts
    if legacy_no_actor_probe:
        raw_issues = [issue for issue in raw_issues if not issue.startswith("Missing primary evidence:")]
    self_hit_hints = set(find_self_hit_actor_context_hints(actor_contexts, pending_matches))

    return LogCoverage(
        path=path,
        actor_contexts=len(actor_contexts),
        actor_resolved=sum(1 for ctx in actor_contexts if ctx.verdict == "resolved" or ctx.verdict == "resolved-self"),
        actor_resolved_self=sum(1 for ctx in actor_contexts if ctx.verdict == "resolved-self"),
        actor_unresolved_positive_debits=sum(
            1 for ctx in actor_contexts
            if ctx.old_debit > 0 and ctx.verdict not in ("resolved", "resolved-self") and ctx not in self_hit_hints
        ),
        actor_ambiguous=sum(1 for ctx in actor_contexts if ctx.verdict == "ambiguous"),
        actor_self_hit_hints=len(self_hit_hints),
        pending_matches=len(pending_matches),
        pending_resolved=sum(1 for match in pending_matches if match.resolved),
        pending_multi_target_batches=sum(1 for count in pending_batch_counts.values() if count > 1),
        pending_max_active_batches=max((match.active_batches for match in pending_matches), default=0),
        pending_target_caches=len(pending_target_caches),
        pending_target_preapply_damage_caches=sum(1 for cache in pending_target_caches if is_preapply_damage_target_cache(cache)),
        pending_target_source_hints=len(target_cache_source_hints),
        pending_target_caches_with_source_hints=len(target_cache_hint_lines),
        immediate_candidates=len(immediate_candidates),
        immediate_selected=sum(1 for candidate in immediate_candidates if candidate.selected),
        formula_candidates=len(formula_candidates),  # type: ignore[arg-type]
        hook_regs_events=len(hook_regs_events),
        targetcache_hook_events=sum(1 for event in hook_regs_events if event.kind == "targetcache"),
        targetcache_hook_events_with_actor_refs=sum(1 for event in hook_regs_events if event.kind == "targetcache" and event.actor_refs),
        targetcache_hook_events_with_source_refs=targetcache_source_ref_events,
        targetcache_source_action_ids=targetcache_source_action_ids,
        targetcache_source_unit_only_refs=targetcache_source_unit_only_refs,
        landmark_hits=len(landmark_hits),
        landmark_hits_with_actor_refs=sum(1 for hit in landmark_hits if hit.actor_refs),
        selector_probes=len(selector_probes),
        selector_outcomes=Counter(probe.evade_type for probe in selector_probes),
        selector_with_actor_refs=sum(1 for probe in selector_probes if probe.actor_refs),
        selector_no_hp=selector_no_hp,
        selector_no_hp_with_source_refs=selector_no_hp_with_source_refs,
        selector_fallback_hints=len(selector_fallback_hints),
        selector_no_hp_source_action_ids=selector_no_hp_source_action_ids,
        selector_actor_action_ids=selector_actor_action_ids,
        actor_action_ids=actor_action_ids,
        pending_action_ids=pending_action_ids,
        immediate_action_ids=immediate_action_ids,
        action_ids=action_ids,
        legacy_no_actor_probe=legacy_no_actor_probe,
        issues=raw_issues,
    )


def render_report(coverages: list[LogCoverage], abilities: dict[int, str]) -> str:
    totals = Counter()
    action_totals: Counter[int] = Counter()
    selector_totals: Counter[int] = Counter()
    selector_actor_action_totals: Counter[int] = Counter()
    selector_no_hp_source_action_totals: Counter[int] = Counter()
    targetcache_source_action_totals: Counter[int] = Counter()
    actor_action_totals: Counter[int] = Counter()
    pending_action_totals: Counter[int] = Counter()
    immediate_action_totals: Counter[int] = Counter()
    issue_totals: Counter[str] = Counter()
    for cov in coverages:
        totals["logs"] += 1
        totals["actor_contexts"] += cov.actor_contexts
        totals["actor_resolved"] += cov.actor_resolved
        totals["actor_resolved_self"] += cov.actor_resolved_self
        totals["actor_unresolved_positive_debits"] += cov.actor_unresolved_positive_debits
        totals["actor_ambiguous"] += cov.actor_ambiguous
        totals["actor_self_hit_hints"] += cov.actor_self_hit_hints
        totals["pending_matches"] += cov.pending_matches
        totals["pending_resolved"] += cov.pending_resolved
        totals["pending_multi_target_batches"] += cov.pending_multi_target_batches
        totals["pending_max_active_batches"] = max(totals["pending_max_active_batches"], cov.pending_max_active_batches)
        totals["pending_target_caches"] += cov.pending_target_caches
        totals["pending_target_preapply_damage_caches"] += cov.pending_target_preapply_damage_caches
        totals["pending_target_source_hints"] += cov.pending_target_source_hints
        totals["pending_target_caches_with_source_hints"] += cov.pending_target_caches_with_source_hints
        totals["immediate_candidates"] += cov.immediate_candidates
        totals["immediate_selected"] += cov.immediate_selected
        totals["formula_candidates"] += cov.formula_candidates
        totals["hook_regs_events"] += cov.hook_regs_events
        totals["targetcache_hook_events"] += cov.targetcache_hook_events
        totals["targetcache_hook_events_with_actor_refs"] += cov.targetcache_hook_events_with_actor_refs
        totals["targetcache_hook_events_with_source_refs"] += cov.targetcache_hook_events_with_source_refs
        totals["targetcache_source_unit_only_refs"] += cov.targetcache_source_unit_only_refs
        totals["landmark_hits"] += cov.landmark_hits
        totals["landmark_hits_with_actor_refs"] += cov.landmark_hits_with_actor_refs
        totals["selector_probes"] += cov.selector_probes
        totals["selector_with_actor_refs"] += cov.selector_with_actor_refs
        totals["selector_no_hp"] += cov.selector_no_hp
        totals["selector_no_hp_with_source_refs"] += cov.selector_no_hp_with_source_refs
        totals["selector_fallback_hints"] += cov.selector_fallback_hints
        totals["legacy_no_actor_probe"] += 1 if cov.legacy_no_actor_probe else 0
        action_totals.update(cov.action_ids)
        selector_totals.update(cov.selector_outcomes)
        selector_actor_action_totals.update(cov.selector_actor_action_ids)
        selector_no_hp_source_action_totals.update(cov.selector_no_hp_source_action_ids)
        targetcache_source_action_totals.update(cov.targetcache_source_action_ids)
        actor_action_totals.update(cov.actor_action_ids)
        pending_action_totals.update(cov.pending_action_ids)
        immediate_action_totals.update(cov.immediate_action_ids)
        issue_totals.update(cov.issues)

    lines: list[str] = [
        "# Action Identity Evidence Coverage",
        "",
        "This is an aggregate of existing `battleprobe` logs. It is a dated work report, not a canonical engine fact.",
        "",
        "## Aggregate Signals",
        "",
        f"- Logs scanned: {totals['logs']}",
        f"- Pre-clamp actor contexts: {totals['actor_contexts']} (`resolved`={totals['actor_resolved']}, `ambiguous`={totals['actor_ambiguous']}, unresolved positive debit={totals['actor_unresolved_positive_debits']})",
        f"- Legacy self-hit/AoE actor-context hints: {totals['actor_self_hit_hints']} (candidate cases for `resolved-self` retest).",
        f"- Pending matches: {totals['pending_matches']} (`resolved`={totals['pending_resolved']}, multi-target batches={totals['pending_multi_target_batches']}, max active batches={totals['pending_max_active_batches']})",
        f"- Pending target caches: {totals['pending_target_caches']} (`pre-apply damage candidates`={totals['pending_target_preapply_damage_caches']})",
        f"- Pre-apply target-cache source hints: {totals['pending_target_source_hints']} across {totals['pending_target_caches_with_source_hints']} cache(s).",
        f"- Immediate candidate snapshots: {totals['immediate_candidates']} (`selected`={totals['immediate_selected']})",
        f"- Formula candidates: {totals['formula_candidates']}",
        f"- Hook-reg events: {totals['hook_regs_events']} (`targetcache`={totals['targetcache_hook_events']}, with unit/actor refs={totals['targetcache_hook_events_with_actor_refs']})",
        f"- Target-cache source-candidate refs: events={totals['targetcache_hook_events_with_source_refs']}/{totals['targetcache_hook_events']}; action ids={summarize_action_ids(targetcache_source_action_totals, abilities, minimum=0)}; unit-only refs={totals['targetcache_source_unit_only_refs']}",
        f"- Landmark hits: {totals['landmark_hits']} (with unit/actor refs={totals['landmark_hits_with_actor_refs']})",
        f"- Selector probes: {totals['selector_probes']} (with actor refs={totals['selector_with_actor_refs']})",
        f"- Selector no-HP outcomes: {totals['selector_no_hp']} (with non-target source actor refs={totals['selector_no_hp_with_source_refs']})",
        f"- Selector fallback hints for unresolved positive-debit actor contexts: {totals['selector_fallback_hints']}",
        f"- Logs without actor-context probe evidence: {totals['legacy_no_actor_probe']} (mostly legacy captures; this is coverage debt, not proof of failure).",
        "",
        "## DCL Action-Identity Requirement Matrix",
        "",
        *render_requirement_matrix(
            totals,
            actor_action_totals,
            pending_action_totals,
            immediate_action_totals,
            selector_actor_action_totals,
            selector_no_hp_source_action_totals,
            targetcache_source_action_totals,
            abilities,
        ),
        "",
        "## Action IDs Seen",
        "",
    ]

    if action_totals:
        lines.append("| Action id | Meaning | Count |")
        lines.append("| ---: | --- | ---: |")
        for action_id, count in sorted(action_totals.items()):
            lines.append(f"| {action_id} | {format_action_name(action_id, abilities)} | {count} |")
    else:
        lines.append("- No action ids seen.")
    lines.append("")

    lines.extend(["## Selector Outcomes Seen", ""])
    if selector_totals:
        lines.append("| Evade type | Count |")
        lines.append("| ---: | ---: |")
        for evade_type, count in sorted(selector_totals.items()):
            lines.append(f"| `0x{evade_type:02X}` | {count} |")
    else:
        lines.append("- No selector outcomes seen.")
    lines.append("")

    lines.extend(["## Selector Actor Action IDs Seen", ""])
    if selector_actor_action_totals:
        lines.append("| Action id | Meaning | Count |")
        lines.append("| ---: | --- | ---: |")
        for action_id, count in sorted(selector_actor_action_totals.items()):
            lines.append(f"| {action_id} | {format_action_name(action_id, abilities)} | {count} |")
    else:
        lines.append("- No selector actor action ids seen.")
    lines.append("")

    lines.extend(["## Selector No-HP Source Action IDs Seen", ""])
    if selector_no_hp_source_action_totals:
        lines.append("| Action id | Meaning | Count |")
        lines.append("| ---: | --- | ---: |")
        for action_id, count in sorted(selector_no_hp_source_action_totals.items()):
            lines.append(f"| {action_id} | {format_action_name(action_id, abilities)} | {count} |")
    else:
        lines.append("- No no-HP selector source action ids seen.")
    lines.append("")

    lines.extend(["## Target-Cache Source Action IDs Seen", ""])
    if targetcache_source_action_totals:
        lines.append("| Action id | Meaning | Count |")
        lines.append("| ---: | --- | ---: |")
        for action_id, count in sorted(targetcache_source_action_totals.items()):
            lines.append(f"| {action_id} | {format_action_name(action_id, abilities)} | {count} |")
    else:
        lines.append("- No target-cache source action ids seen.")
    lines.append("")

    lines.extend(["## Repeated Issues", ""])
    if issue_totals:
        lines.append("| Issue | Logs/events |")
        lines.append("| --- | ---: |")
        for issue, count in issue_totals.most_common():
            lines.append(f"| {issue} | {count} |")
    else:
        lines.append("- No hard action-identity issues detected by the current analyzer.")
    lines.append("")

    lines.extend(["## Per-Log Matrix", ""])
    if coverages:
        lines.append("| Log | Actor ctx | Pending | Target cache | Cache hints | Targetcache regs | Landmarks | Immediate | Formula | Selector | No-HP selector | Selector fallback | Selector actor refs | Issues |")
        lines.append("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |")
        for cov in sorted(coverages, key=sort_key):
            rel = cov.path.relative_to(ROOT) if cov.path.is_relative_to(ROOT) else cov.path
            actor = f"{cov.actor_resolved}/{cov.actor_contexts}"
            pending = f"{cov.pending_resolved}/{cov.pending_matches}"
            target_cache = f"{cov.pending_target_preapply_damage_caches}/{cov.pending_target_caches}"
            target_cache_hints = f"{cov.pending_target_source_hints}/{cov.pending_target_caches_with_source_hints}"
            targetcache_regs = f"{cov.targetcache_hook_events_with_actor_refs}/{cov.targetcache_hook_events}"
            landmarks = f"{cov.landmark_hits_with_actor_refs}/{cov.landmark_hits}"
            immediate = f"{cov.immediate_selected}/{cov.immediate_candidates}"
            issues = "; ".join(cov.issues[:3])
            if len(cov.issues) > 3:
                issues += f"; +{len(cov.issues) - 3} more"
            note = issues or ("legacy/no actor probe" if cov.legacy_no_actor_probe else "none")
            no_hp = f"{cov.selector_no_hp_with_source_refs}/{cov.selector_no_hp}"
            lines.append(f"| `{rel}` | {actor} | {pending} | {target_cache} | {target_cache_hints} | {targetcache_regs} | {landmarks} | {immediate} | {cov.formula_candidates} | {cov.selector_probes} | {no_hp} | {cov.selector_fallback_hints} | {cov.selector_with_actor_refs} | {note} |")
    else:
        lines.append("- No logs matched the scan criteria.")
    lines.append("")

    lines.extend(
        [
            "## Offline Conclusion",
            "",
            "- Existing logs can validate the parser/tooling and several action-id surfaces.",
            "- Existing logs are not enough to retire the live probe: they do not cover every required class under one observe-only profile.",
            "- Missing or weak coverage remains for counters/reactions, multiple simultaneous pending actions, and cross-battle actor-array stability.",
            "- The prepared live probe should therefore be run before promoting actor context to the runtime primary resolver.",
            "",
        ]
    )
    return "\n".join(lines)


def render_requirement_matrix(
    totals: Counter[str],
    actor_action_totals: Counter[int],
    pending_action_totals: Counter[int],
    immediate_action_totals: Counter[int],
    selector_actor_action_totals: Counter[int],
    selector_no_hp_source_action_totals: Counter[int],
    targetcache_source_action_totals: Counter[int],
    abilities: dict[int, str],
) -> list[str]:
    rows = [
        (
            "HP-apply target/source/action",
            covered(totals["actor_resolved"] > 0 and totals["actor_unresolved_positive_debits"] == 0),
            f"{totals['actor_resolved']}/{totals['actor_contexts']} actor contexts resolved; unresolved positive debit={totals['actor_unresolved_positive_debits']}",
        ),
        (
            "Selector fallback for unresolved HP actor context",
            partial(totals["selector_fallback_hints"] > 0),
            f"selector fallback hints={totals['selector_fallback_hints']}; this is diagnostic/no-HP support, not same-frame pre-clamp formula authority",
        ),
        (
            "Immediate basic attack identity",
            covered(actor_action_totals[0] > 0 or immediate_action_totals[0] > 0),
            f"actionId 0 seen {actor_action_totals[0]} actor-context time(s), {immediate_action_totals[0]} immediate candidate time(s)",
        ),
        (
            "Immediate named action identity",
            covered(any(action_id > 0 for action_id in immediate_action_totals)),
            summarize_action_ids(immediate_action_totals, abilities, minimum=1),
        ),
        (
            "Charged/pending action identity",
            covered(totals["pending_resolved"] > 0 and bool(pending_action_totals)),
            f"{totals['pending_resolved']}/{totals['pending_matches']} pending matches resolved; ids: {summarize_action_ids(pending_action_totals, abilities, minimum=0)}",
        ),
        (
            "AoE or multi-target pending batch",
            partial(totals["pending_multi_target_batches"] > 0),
            f"multi-target pending batches={totals['pending_multi_target_batches']}; HP target separation still needs explicit hit/batch ownership",
        ),
        (
            "Selector-frame hit identity",
            covered(totals["selector_with_actor_refs"] > 0 and bool(selector_actor_action_totals)),
            f"{totals['selector_with_actor_refs']}/{totals['selector_probes']} selector probes have actor refs; ids: {summarize_action_ids(selector_actor_action_totals, abilities, minimum=0)}",
        ),
        (
            "Native no-HP reaction identity, basic attack",
            covered(selector_no_hp_source_action_totals[0] > 0),
            f"no-HP source actionId 0 count={selector_no_hp_source_action_totals[0]}",
        ),
        (
            "Native no-HP reaction identity, named action",
            covered(any(action_id > 0 for action_id in selector_no_hp_source_action_totals)),
            summarize_action_ids(selector_no_hp_source_action_totals, abilities, minimum=1),
        ),
        (
            "Self-hit / self-AoE attribution",
            partial(totals["actor_resolved_self"] > 0 or totals["actor_self_hit_hints"] > 0),
            f"resolved-self={totals['actor_resolved_self']}; legacy hints={totals['actor_self_hit_hints']}",
        ),
        (
            "Multiple simultaneous pending actions",
            partial(totals["pending_max_active_batches"] > 1),
            f"max active batches observed={totals['pending_max_active_batches']}",
        ),
        (
            "Hamedo/First-Strike cancelled incoming action",
            "Partial",
            f"reaction damage is visible when it reaches HP apply; basic incoming source has target-cache source-candidate register proof={totals['targetcache_hook_events_with_source_refs']}/{totals['targetcache_hook_events']}; target-cache source action ids={summarize_action_ids(targetcache_source_action_totals, abilities, minimum=0)}; unit-only refs={totals['targetcache_source_unit_only_refs']}; named incoming action id still needs live proof",
        ),
        (
            "Tile/epicenter target reconstruction",
            "Open",
            "no canonical parser surface for selected tile, epicenter, facing, or final AoE membership yet",
        ),
        (
            "Cross-battle actor-array stability",
            partial(totals["logs"] > 1 and totals["actor_resolved"] > 0),
            f"{totals['logs']} logs scanned; actor context resolves in aggregate, but stability is not a dedicated assertion",
        ),
    ]

    lines = [
        "| Requirement | Coverage | Evidence / remaining gap |",
        "| --- | --- | --- |",
    ]
    for requirement, status, evidence in rows:
        lines.append(f"| {requirement} | {status} | {evidence} |")
    return lines


def covered(condition: bool) -> str:
    return "Covered" if condition else "Missing"


def partial(condition: bool) -> str:
    return "Partial" if condition else "Missing"


def summarize_action_ids(counter: Counter[int], abilities: dict[int, str], minimum: int) -> str:
    items = [(action_id, count) for action_id, count in sorted(counter.items()) if action_id >= minimum]
    if not items:
        return "none seen"
    return ", ".join(f"{action_id} {format_action_name(action_id, abilities)} x{count}" for action_id, count in items[:8])


def sort_key(cov: LogCoverage) -> tuple[int, str]:
    has_actor = 0 if cov.actor_contexts else 1
    has_issue = 0 if cov.issues else 1
    return (has_actor, has_issue, str(cov.path))


if __name__ == "__main__":
    raise SystemExit(main())
