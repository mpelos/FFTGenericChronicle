#!/usr/bin/env python3
"""Watch a live mapping log until the current runtime emits [RUNTIME] evidence."""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path

from analyze_battleprobe_log import (
    DEATH_EVENT_RE,
    DEFAULT_LOG,
    MEMTABLE_FOUND_RE,
    MEMTABLE_ROW_RE,
    OLD_UNIT_RE,
    REWRITE_EVENT_RE,
    RUNTIME_RE,
    parse_number,
    parse_runtime_context,
    parse_rewrite_line,
)
from analyze_actor_probe_ct import parse_actor_probe_line, resolve_ct_attackers
from promote_runtime_offsets import (
    DEFAULT_BASE,
    DEFAULT_OUTPUT,
    DEFAULT_POLICY_BASE,
    DEFAULT_POLICY_OUTPUT,
)

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_ANALYSIS = ROOT / "work" / "battleprobe_analysis.md"


@dataclass(frozen=True)
class LogState:
    exists: bool
    mtime: float
    is_fresh: bool
    has_current_header: bool
    has_old_header: bool
    old_unit_lines: int
    runtime_events: int
    action_signal_events: int = 0
    action_signal_counts: tuple[tuple[str, int], ...] = ()
    action_var_counts: tuple[tuple[str, int], ...] = ()
    target_slot_present_events: int = 0
    attacker_slot_present_events: int = 0
    equipment_dr_events: int = 0
    response_events: int = 0
    trace_var_counts: tuple[tuple[str, int], ...] = ()
    rewrite_events: int = 0
    rewrite_failures: int = 0
    placeholder_rewrites: int = 0
    hp_healing_rewrites: int = 0
    lethal_hp_rewrites: int = 0
    large_vanilla_rewrites: int = 0
    mp_loss_rewrites: int = 0
    mp_gain_rewrites: int = 0
    death_events: int = 0
    death_writes: int = 0
    death_write_failures: int = 0
    death_write_skips: int = 0
    memory_table_found: int = 0
    memory_table_rows: int = 0
    actor_probe_events: int = 0
    ct_actor_resolved: int = 0
    ct_drop_resolved: int = 0
    ct_lowest_resolved: int = 0

    @property
    def is_ready(self) -> bool:
        return self.exists and self.is_fresh and self.has_current_header and self.runtime_events > 0


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Wait for fresh Generic Chronicle [RUNTIME] mapping evidence.")
    p.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    p.add_argument("--runtime-events", type=int, default=1, help="Minimum [RUNTIME] lines to wait for.")
    p.add_argument("--action-signals", type=int, default=0, help="Minimum [RUNTIME] lines with a nonzero action.signal.")
    p.add_argument("--require-action-signal", action="append", default=[], help="Specific action.signal value that must appear at least once; can be repeated.")
    p.add_argument("--require-action-var", action="append", default=[], help="Specific nonzero action variable that must appear at least once; can be repeated.")
    p.add_argument("--target-slots-present", type=int, default=0, help="Minimum [RUNTIME] target slot observations with state=present.")
    p.add_argument("--attacker-slots-present", type=int, default=0, help="Minimum [RUNTIME] attacker slot observations with state=present.")
    p.add_argument("--equipment-dr-events", type=int, default=0, help="Minimum [RUNTIME] events with positive equipmentDr.")
    p.add_argument("--response-events", type=int, default=0, help="Minimum [RUNTIME] events with response rules > 0.")
    p.add_argument("--require-trace-var", action="append", default=[], help="Formula trace variable that must appear with a nonzero value; can be repeated.")
    p.add_argument("--actor-probes", type=int, default=0, help="Minimum [ACTOR-PROBE] events to wait for.")
    p.add_argument("--ct-attackers", type=int, default=0, help="Minimum [ACTOR-PROBE] events with a resolved CT attacker.")
    p.add_argument("--ct-drop-attackers", type=int, default=0, help="Minimum CT attacker resolutions from a recent CT drop.")
    p.add_argument("--ct-lowest-attackers", type=int, default=0, help="Minimum CT attacker resolutions from lowest absolute CT.")
    p.add_argument("--rewrite-events", type=int, default=0, help="Minimum [REWRITE] lines to wait for.")
    p.add_argument("--max-rewrite-failures", type=int, default=None, help="Maximum [REWRITE-FAILED]/[MP-REWRITE-FAILED] lines allowed before failing.")
    p.add_argument("--placeholder-rewrites", type=int, default=0, help="Minimum HP [REWRITE]/dry-run lines with positive vanillaDamage within --max-placeholder-damage.")
    p.add_argument("--hp-healing-rewrites", type=int, default=0, help="Minimum HP [REWRITE]/dry-run lines with negative vanillaDamage.")
    p.add_argument("--lethal-hp-rewrites", type=int, default=0, help="Minimum HP [REWRITE]/dry-run lines with finalDamage>=9999 and HP written to 0.")
    p.add_argument("--mp-loss-rewrites", type=int, default=0, help="Minimum MP [MP-REWRITE]/dry-run lines with negative vanillaMpChange.")
    p.add_argument("--mp-gain-rewrites", type=int, default=0, help="Minimum MP [MP-REWRITE]/dry-run lines with positive vanillaMpChange.")
    p.add_argument("--max-placeholder-damage", type=int, default=30, help="Maximum positive vanillaDamage considered placeholder-sized.")
    p.add_argument("--max-large-vanilla-rewrites", type=int, default=None, help="Maximum HP [REWRITE]/dry-run lines with positive vanillaDamage above --max-placeholder-damage allowed before failing.")
    p.add_argument("--death-events", type=int, default=0, help="Minimum [DEATH-*] lines to wait for.")
    p.add_argument("--death-writes", type=int, default=0, help="Minimum concrete [DEATH-WRITE] lines to wait for.")
    p.add_argument("--max-death-events", type=int, default=None, help="Maximum [DEATH-*] lines allowed before failing.")
    p.add_argument("--max-death-write-failures", type=int, default=None, help="Maximum [DEATH-WRITE-FAILED] lines allowed before failing.")
    p.add_argument("--settle-seconds", type=float, default=0.0, help="After positive evidence is ready, keep watching this long so delayed failure guards can fire.")
    p.add_argument("--memtable-found", type=int, default=0, help="Minimum [MEMTABLE-FOUND] lines to wait for.")
    p.add_argument("--memtable-rows", type=int, default=0, help="Minimum [MEMTABLE-ROW] lines to wait for.")
    p.add_argument("--timeout", type=float, default=300.0, help="Seconds to wait before failing.")
    p.add_argument("--interval", type=float, default=1.0, help="Seconds between log checks.")
    p.add_argument("--allow-existing", action="store_true", help="Accept a log older than this watcher start.")
    p.add_argument("--analysis-output", type=Path, default=DEFAULT_ANALYSIS)
    p.add_argument("--skip-analyze", action="store_true")
    p.add_argument("--promote", action="store_true", help="Run promote_runtime_offsets.py after enough runtime events.")
    p.add_argument("--min-events", type=int, default=3, help="Promotion min-events value.")
    p.add_argument("--base-settings", type=Path, default=DEFAULT_BASE)
    p.add_argument("--promoted-output", type=Path, default=DEFAULT_OUTPUT)
    p.add_argument("--also-policy", action="store_true")
    p.add_argument("--policy-base-settings", type=Path, default=DEFAULT_POLICY_BASE)
    p.add_argument("--policy-output", type=Path, default=DEFAULT_POLICY_OUTPUT)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    start_time = time.time()
    deadline = start_time + max(0.1, args.timeout)
    required_runtime_events = max(0, args.runtime_events)
    required_action_signal_events = max(0, args.action_signals)
    required_action_signals = tuple(normalize_action_signal(value) for value in args.require_action_signal)
    required_action_vars = tuple(normalize_action_var(value) for value in args.require_action_var)
    required_target_slot_present_events = max(0, args.target_slots_present)
    required_attacker_slot_present_events = max(0, args.attacker_slots_present)
    required_equipment_dr_events = max(0, args.equipment_dr_events)
    required_response_events = max(0, args.response_events)
    required_trace_vars = tuple(normalize_trace_var(value) for value in args.require_trace_var)
    required_actor_probes = max(0, args.actor_probes)
    required_ct_attackers = max(0, args.ct_attackers)
    required_ct_drop_attackers = max(0, args.ct_drop_attackers)
    required_ct_lowest_attackers = max(0, args.ct_lowest_attackers)
    required_rewrite_events = max(0, args.rewrite_events)
    max_rewrite_failures = normalize_optional_max(args.max_rewrite_failures)
    required_placeholder_rewrites = max(0, args.placeholder_rewrites)
    required_hp_healing_rewrites = max(0, args.hp_healing_rewrites)
    required_lethal_hp_rewrites = max(0, args.lethal_hp_rewrites)
    required_mp_loss_rewrites = max(0, args.mp_loss_rewrites)
    required_mp_gain_rewrites = max(0, args.mp_gain_rewrites)
    max_placeholder_damage = max(1, args.max_placeholder_damage)
    max_large_vanilla_rewrites = normalize_optional_max(args.max_large_vanilla_rewrites)
    required_death_events = max(0, args.death_events)
    required_death_writes = max(0, args.death_writes)
    max_death_events = normalize_optional_max(args.max_death_events)
    max_death_write_failures = normalize_optional_max(args.max_death_write_failures)
    settle_seconds = max(0.0, args.settle_seconds)
    required_memtable_found = max(0, args.memtable_found)
    required_memtable_rows = max(0, args.memtable_rows)
    last_message = ""
    ready_since: float | None = None

    print(f"watching {args.log}")
    print(
        f"waiting for current runtime header, {required_runtime_events} [RUNTIME] event(s), "
        f"{required_action_signal_events} action signal event(s), "
        f"{required_target_slot_present_events} target slot present event(s), "
        f"{required_attacker_slot_present_events} attacker slot present event(s), "
        f"{required_equipment_dr_events} equipment DR event(s), "
        f"{required_response_events} response event(s), "
        f"{required_actor_probes} [ACTOR-PROBE] event(s), "
        f"{required_ct_attackers} CT attacker resolution(s), "
        f"{required_ct_drop_attackers} CT-drop attacker resolution(s), "
        f"{required_ct_lowest_attackers} CT-lowest attacker resolution(s), "
        f"{required_rewrite_events} [REWRITE] event(s), "
        f"{required_placeholder_rewrites} placeholder rewrite(s), "
        f"{required_hp_healing_rewrites} HP healing rewrite(s), "
        f"{required_lethal_hp_rewrites} lethal HP rewrite(s), "
        f"{required_mp_loss_rewrites} MP loss rewrite(s), "
        f"{required_mp_gain_rewrites} MP gain rewrite(s), "
        f"{required_death_events} [DEATH-*] event(s), "
        f"{required_death_writes} [DEATH-WRITE] event(s), "
        f"{required_memtable_found} [MEMTABLE-FOUND] event(s), "
        f"and {required_memtable_rows} [MEMTABLE-ROW] event(s)"
    )
    guard_parts = failure_guard_parts(
        max_large_vanilla_rewrites,
        max_rewrite_failures,
        max_death_events,
        max_death_write_failures,
    )
    if guard_parts:
        print("failure guards: " + ", ".join(guard_parts))
    if settle_seconds > 0:
        print(f"settle window: {settle_seconds:g}s after positive evidence is ready")

    while time.time() <= deadline:
        state = inspect_log(args.log, start_time, args.allow_existing, max_placeholder_damage)
        failure = describe_failure_guard(
            state,
            max_large_vanilla_rewrites,
            max_rewrite_failures,
            max_death_events,
            max_death_write_failures,
        )
        if failure:
            print(failure)
            if not args.skip_analyze:
                run_analyzer(args.log, args.analysis_output)
            return 1
        message = describe_state(
            state,
            required_runtime_events,
            required_rewrite_events,
            required_placeholder_rewrites,
            required_death_events,
            required_death_writes,
            required_hp_healing_rewrites=required_hp_healing_rewrites,
            required_lethal_hp_rewrites=required_lethal_hp_rewrites,
            required_mp_loss_rewrites=required_mp_loss_rewrites,
            required_mp_gain_rewrites=required_mp_gain_rewrites,
            required_action_signal_events=required_action_signal_events,
            required_action_signals=required_action_signals,
            required_action_vars=required_action_vars,
            required_target_slot_present_events=required_target_slot_present_events,
            required_attacker_slot_present_events=required_attacker_slot_present_events,
            required_equipment_dr_events=required_equipment_dr_events,
            required_response_events=required_response_events,
            required_trace_vars=required_trace_vars,
            required_actor_probes=required_actor_probes,
            required_ct_attackers=required_ct_attackers,
            required_ct_drop_attackers=required_ct_drop_attackers,
            required_ct_lowest_attackers=required_ct_lowest_attackers,
            required_memtable_found=required_memtable_found,
            required_memtable_rows=required_memtable_rows,
        )
        if message != last_message:
            print(message)
            last_message = message

        ready = (
            state.exists
            and state.is_fresh
            and state.has_current_header
            and state.runtime_events >= required_runtime_events
            and state.action_signal_events >= required_action_signal_events
            and has_required_action_signals(state, required_action_signals)
            and has_required_action_vars(state, required_action_vars)
            and state.target_slot_present_events >= required_target_slot_present_events
            and state.attacker_slot_present_events >= required_attacker_slot_present_events
            and state.equipment_dr_events >= required_equipment_dr_events
            and state.response_events >= required_response_events
            and has_required_trace_vars(state, required_trace_vars)
            and state.actor_probe_events >= required_actor_probes
            and state.ct_actor_resolved >= required_ct_attackers
            and state.ct_drop_resolved >= required_ct_drop_attackers
            and state.ct_lowest_resolved >= required_ct_lowest_attackers
            and state.rewrite_events >= required_rewrite_events
            and failure_guards_pass(
                state,
                max_large_vanilla_rewrites,
                max_rewrite_failures,
                max_death_events,
                max_death_write_failures,
            )
            and state.placeholder_rewrites >= required_placeholder_rewrites
            and state.hp_healing_rewrites >= required_hp_healing_rewrites
            and state.lethal_hp_rewrites >= required_lethal_hp_rewrites
            and state.mp_loss_rewrites >= required_mp_loss_rewrites
            and state.mp_gain_rewrites >= required_mp_gain_rewrites
            and state.death_events >= required_death_events
            and state.death_writes >= required_death_writes
            and state.memory_table_found >= required_memtable_found
            and state.memory_table_rows >= required_memtable_rows
        )
        if ready:
            if settle_seconds > 0:
                now = time.time()
                if ready_since is None:
                    ready_since = now
                    deadline = max(deadline, ready_since + settle_seconds)
                    print(
                        f"positive evidence is ready; settling for {settle_seconds:g}s "
                        "to catch delayed failure evidence"
                    )
                if now < ready_since + settle_seconds:
                    time.sleep(max(0.1, args.interval))
                    continue
            print("runtime evidence is ready")
            if not args.skip_analyze:
                run_analyzer(args.log, args.analysis_output)
            if args.promote:
                run_promoter(args)
            return 0

        time.sleep(max(0.1, args.interval))

    state = inspect_log(args.log, start_time, args.allow_existing, max_placeholder_damage)
    print("timeout waiting for runtime mapping evidence")
    failure = describe_failure_guard(
        state,
        max_large_vanilla_rewrites,
        max_rewrite_failures,
        max_death_events,
        max_death_write_failures,
    )
    if failure:
        print(failure)
    print(describe_state(
        state,
        required_runtime_events,
        required_rewrite_events,
        required_placeholder_rewrites,
        required_death_events,
        required_death_writes,
        required_hp_healing_rewrites=required_hp_healing_rewrites,
        required_lethal_hp_rewrites=required_lethal_hp_rewrites,
        required_mp_loss_rewrites=required_mp_loss_rewrites,
        required_mp_gain_rewrites=required_mp_gain_rewrites,
        required_action_signal_events=required_action_signal_events,
        required_action_signals=required_action_signals,
        required_action_vars=required_action_vars,
        required_target_slot_present_events=required_target_slot_present_events,
        required_attacker_slot_present_events=required_attacker_slot_present_events,
        required_equipment_dr_events=required_equipment_dr_events,
        required_response_events=required_response_events,
        required_trace_vars=required_trace_vars,
        required_actor_probes=required_actor_probes,
        required_ct_attackers=required_ct_attackers,
        required_ct_drop_attackers=required_ct_drop_attackers,
        required_ct_lowest_attackers=required_ct_lowest_attackers,
        required_memtable_found=required_memtable_found,
        required_memtable_rows=required_memtable_rows,
    ))
    if not args.skip_analyze and state.exists:
        run_analyzer(args.log, args.analysis_output)
    return 1


def inspect_log(
    log: Path,
    start_time: float = 0.0,
    allow_existing: bool = False,
    max_placeholder_damage: int = 30,
) -> LogState:
    if not log.exists():
        return LogState(False, 0.0, False, False, False, 0, 0)

    mtime = log.stat().st_mtime
    is_fresh = allow_existing or mtime >= start_time - 0.5
    has_current_header = False
    has_old_header = False
    old_unit_lines = 0
    runtime_events = 0
    action_signal_counts: dict[str, int] = {}
    action_var_counts: dict[str, int] = {}
    target_slot_present_events = 0
    attacker_slot_present_events = 0
    equipment_dr_events = 0
    response_events = 0
    trace_var_counts: dict[str, int] = {}
    rewrite_events = 0
    rewrite_failures = 0
    placeholder_rewrites = 0
    hp_healing_rewrites = 0
    lethal_hp_rewrites = 0
    large_vanilla_rewrites = 0
    mp_loss_rewrites = 0
    mp_gain_rewrites = 0
    death_events = 0
    death_writes = 0
    death_write_failures = 0
    death_write_skips = 0
    memory_table_found = 0
    memory_table_rows = 0
    actor_probe_events = []

    for line_no, line in enumerate(log.read_text(encoding="utf-8", errors="replace").splitlines(), start=1):
        if "Generic Chronicle Battle Runtime Harness" in line:
            has_current_header = True
        if "Generic Chronicle Battle Validation Harness" in line:
            has_old_header = True
        if OLD_UNIT_RE.search(line):
            old_unit_lines += 1
        if runtime_match := RUNTIME_RE.search(line):
            runtime_events += 1
            runtime = parse_runtime_context(runtime_match.group(1), runtime_match.group("body"))
            action = runtime.get("action", {})
            if isinstance(action, dict):
                signal = normalize_action_signal(str(action.get("signal", "")))
                if is_present_action_signal(signal):
                    action_signal_counts[signal] = action_signal_counts.get(signal, 0) + 1
                variables = action.get("variables", {})
                if isinstance(variables, dict):
                    for name, value in variables.items():
                        variable = normalize_action_var(str(name))
                        if variable and is_present_action_var(str(value)):
                            action_var_counts[variable] = action_var_counts.get(variable, 0) + 1
            target_slot_present_events += count_present_slots(runtime.get("target_slots", []))
            attacker_slot_present_events += count_present_slots(runtime.get("attacker_slots", []))
            if runtime_equipment_dr_value(runtime.get("equipment_dr", "")) > 0:
                equipment_dr_events += 1
            response = runtime.get("response", {})
            if isinstance(response, dict) and int(response.get("rules", 0) or 0) > 0:
                response_events += 1
            trace_vars = runtime.get("trace_vars", {})
            if isinstance(trace_vars, dict):
                for name, value in trace_vars.items():
                    variable = normalize_trace_var(str(name))
                    if variable and is_present_trace_var(str(value)):
                        trace_var_counts[variable] = trace_var_counts.get(variable, 0) + 1
        if actor_probe_event := parse_actor_probe_line(line, line_no):
            actor_probe_events.append(actor_probe_event)
        if REWRITE_EVENT_RE.search(line):
            rewrite_events += 1
            rewrite = parse_rewrite_line(line, line_no)
            if rewrite and rewrite.get("status") == "failed":
                rewrite_failures += 1
            if rewrite and rewrite.get("status") in {"write", "dry-run"}:
                vanilla = rewrite.get("vanilla")
                final = rewrite.get("final")
                if rewrite.get("resource") == "hp":
                    if isinstance(vanilla, int):
                        if vanilla > 0:
                            if vanilla <= max_placeholder_damage:
                                placeholder_rewrites += 1
                            else:
                                large_vanilla_rewrites += 1
                        elif vanilla < 0:
                            hp_healing_rewrites += 1
                    if isinstance(final, int) and final >= 9999 and rewrite.get("to_value") == 0:
                        lethal_hp_rewrites += 1
                elif rewrite.get("resource") == "mp" and isinstance(vanilla, int):
                    if vanilla < 0:
                        mp_loss_rewrites += 1
                    elif vanilla > 0:
                        mp_gain_rewrites += 1
        if death_match := DEATH_EVENT_RE.search(line):
            death_events += 1
            tag = death_match.group("tag")
            if tag == "DEATH-WRITE":
                death_writes += 1
            elif tag == "DEATH-WRITE-FAILED":
                death_write_failures += 1
            elif tag == "DEATH-WRITE-SKIP":
                death_write_skips += 1
        if MEMTABLE_FOUND_RE.search(line):
            memory_table_found += 1
        if MEMTABLE_ROW_RE.search(line):
            memory_table_rows += 1

    ct_resolutions = resolve_ct_attackers(actor_probe_events)
    ct_actor_resolved = sum(1 for resolution in ct_resolutions if resolution.attacker_id is not None)
    ct_drop_resolved = sum(1 for resolution in ct_resolutions if resolution.source == "ct-drop")
    ct_lowest_resolved = sum(1 for resolution in ct_resolutions if resolution.source == "ct-lowest")

    return LogState(
        True,
        mtime,
        is_fresh,
        has_current_header,
        has_old_header,
        old_unit_lines,
        runtime_events,
        sum(action_signal_counts.values()),
        tuple(sorted(action_signal_counts.items(), key=lambda item: (-item[1], item[0]))),
        tuple(sorted(action_var_counts.items(), key=lambda item: (-item[1], item[0]))),
        target_slot_present_events,
        attacker_slot_present_events,
        equipment_dr_events,
        response_events,
        tuple(sorted(trace_var_counts.items(), key=lambda item: (-item[1], item[0]))),
        rewrite_events,
        rewrite_failures,
        placeholder_rewrites,
        hp_healing_rewrites,
        lethal_hp_rewrites,
        large_vanilla_rewrites,
        mp_loss_rewrites,
        mp_gain_rewrites,
        death_events,
        death_writes,
        death_write_failures,
        death_write_skips,
        memory_table_found,
        memory_table_rows,
        len(actor_probe_events),
        ct_actor_resolved,
        ct_drop_resolved,
        ct_lowest_resolved,
    )


def describe_state(
    state: LogState,
    required_runtime_events: int,
    required_rewrite_events: int = 0,
    required_placeholder_rewrites: int = 0,
    required_death_events: int = 0,
    required_death_writes: int = 0,
    required_hp_healing_rewrites: int = 0,
    required_lethal_hp_rewrites: int = 0,
    required_mp_loss_rewrites: int = 0,
    required_mp_gain_rewrites: int = 0,
    required_action_signal_events: int = 0,
    required_action_signals: tuple[str, ...] = (),
    required_action_vars: tuple[str, ...] = (),
    required_target_slot_present_events: int = 0,
    required_attacker_slot_present_events: int = 0,
    required_equipment_dr_events: int = 0,
    required_response_events: int = 0,
    required_trace_vars: tuple[str, ...] = (),
    required_actor_probes: int = 0,
    required_ct_attackers: int = 0,
    required_ct_drop_attackers: int = 0,
    required_ct_lowest_attackers: int = 0,
    required_memtable_found: int = 0,
    required_memtable_rows: int = 0,
) -> str:
    if not state.exists:
        return "log not found yet"
    if not state.is_fresh:
        return "log exists but is older than watcher start; waiting for fresh log write"
    if state.has_old_header and not state.has_current_header:
        return f"stale harness log detected ({state.old_unit_lines} old-format unit line(s)); restart through Reloaded-II"
    if not state.has_current_header:
        return "log exists but current runtime header is not present yet"
    if state.runtime_events < required_runtime_events:
        return (
            f"current runtime loaded; waiting for [RUNTIME] events "
            f"({state.runtime_events}/{required_runtime_events}){evidence_suffix(state)}"
        )
    if state.action_signal_events < required_action_signal_events:
        return (
            f"current runtime loaded; waiting for action signals "
            f"({state.action_signal_events}/{required_action_signal_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    missing_signals = missing_required_action_signals(state, required_action_signals)
    if missing_signals:
        return (
            f"current runtime loaded; waiting for action signal(s) "
            f"{', '.join(missing_signals)}; "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    missing_vars = missing_required_action_vars(state, required_action_vars)
    if missing_vars:
        return (
            f"current runtime loaded; waiting for action variable(s) "
            f"{', '.join(missing_vars)}; "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.target_slot_present_events < required_target_slot_present_events:
        return (
            f"current runtime loaded; waiting for target slot present evidence "
            f"({state.target_slot_present_events}/{required_target_slot_present_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.attacker_slot_present_events < required_attacker_slot_present_events:
        return (
            f"current runtime loaded; waiting for attacker slot present evidence "
            f"({state.attacker_slot_present_events}/{required_attacker_slot_present_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.equipment_dr_events < required_equipment_dr_events:
        return (
            f"current runtime loaded; waiting for equipment DR evidence "
            f"({state.equipment_dr_events}/{required_equipment_dr_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.response_events < required_response_events:
        return (
            f"current runtime loaded; waiting for response evidence "
            f"({state.response_events}/{required_response_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    missing_trace_vars = missing_required_trace_vars(state, required_trace_vars)
    if missing_trace_vars:
        return (
            f"current runtime loaded; waiting for trace variable(s) "
            f"{', '.join(missing_trace_vars)}; "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.actor_probe_events < required_actor_probes:
        return (
            f"current runtime loaded; waiting for [ACTOR-PROBE] events "
            f"({state.actor_probe_events}/{required_actor_probes}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.ct_actor_resolved < required_ct_attackers:
        return (
            f"current runtime loaded; waiting for CT attacker resolution evidence "
            f"({state.ct_actor_resolved}/{required_ct_attackers}); "
            f"{state.actor_probe_events} [ACTOR-PROBE] event(s){evidence_suffix(state)}"
        )
    if state.ct_drop_resolved < required_ct_drop_attackers:
        return (
            f"current runtime loaded; waiting for CT-drop attacker evidence "
            f"({state.ct_drop_resolved}/{required_ct_drop_attackers}); "
            f"{state.actor_probe_events} [ACTOR-PROBE] event(s){evidence_suffix(state)}"
        )
    if state.ct_lowest_resolved < required_ct_lowest_attackers:
        return (
            f"current runtime loaded; waiting for CT-lowest attacker evidence "
            f"({state.ct_lowest_resolved}/{required_ct_lowest_attackers}); "
            f"{state.actor_probe_events} [ACTOR-PROBE] event(s){evidence_suffix(state)}"
        )
    if state.rewrite_events < required_rewrite_events:
        return (
            f"current runtime loaded; waiting for [REWRITE] events "
            f"({state.rewrite_events}/{required_rewrite_events}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.placeholder_rewrites < required_placeholder_rewrites:
        return (
            f"current runtime loaded; waiting for placeholder-sized HP rewrites "
            f"({state.placeholder_rewrites}/{required_placeholder_rewrites}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.hp_healing_rewrites < required_hp_healing_rewrites:
        return (
            f"current runtime loaded; waiting for HP healing rewrites "
            f"({state.hp_healing_rewrites}/{required_hp_healing_rewrites}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.lethal_hp_rewrites < required_lethal_hp_rewrites:
        return (
            f"current runtime loaded; waiting for lethal HP rewrites "
            f"({state.lethal_hp_rewrites}/{required_lethal_hp_rewrites}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.mp_loss_rewrites < required_mp_loss_rewrites:
        return (
            f"current runtime loaded; waiting for MP loss rewrites "
            f"({state.mp_loss_rewrites}/{required_mp_loss_rewrites}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.mp_gain_rewrites < required_mp_gain_rewrites:
        return (
            f"current runtime loaded; waiting for MP gain rewrites "
            f"({state.mp_gain_rewrites}/{required_mp_gain_rewrites}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.death_events < required_death_events:
        return (
            f"current runtime loaded; waiting for [DEATH-*] events "
            f"({state.death_events}/{required_death_events}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.death_writes < required_death_writes:
        return (
            f"current runtime loaded; waiting for [DEATH-WRITE] events "
            f"({state.death_writes}/{required_death_writes}); "
            f"{state.runtime_events} [RUNTIME] event(s), "
            f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
        )
    if state.memory_table_found < required_memtable_found:
        return (
            f"current runtime loaded; waiting for [MEMTABLE-FOUND] events "
            f"({state.memory_table_found}/{required_memtable_found}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    if state.memory_table_rows < required_memtable_rows:
        return (
            f"current runtime loaded; waiting for [MEMTABLE-ROW] events "
            f"({state.memory_table_rows}/{required_memtable_rows}); "
            f"{state.runtime_events} [RUNTIME] event(s){evidence_suffix(state)}"
        )
    return (
        f"ready: {state.runtime_events} [RUNTIME] event(s), "
        f"{state.rewrite_events} [REWRITE] event(s){evidence_suffix(state)}"
    )


def normalize_action_signal(value: str) -> str:
    return value.strip()


def normalize_action_var(value: str) -> str:
    return value.strip()


def normalize_trace_var(value: str) -> str:
    return value.strip().lower()


def is_present_action_signal(signal: str) -> bool:
    return bool(signal) and signal != "0"


def is_present_action_var(value: str) -> bool:
    return bool(value) and value != "0"


def is_present_trace_var(value: str) -> bool:
    return bool(value) and value != "0"


def count_present_slots(slots: object) -> int:
    if not isinstance(slots, list):
        return 0
    return sum(1 for slot in slots if isinstance(slot, dict) and str(slot.get("state", "")) == "present")


def runtime_equipment_dr_value(value: object) -> int:
    text = str(value or "")
    raw, _, _ = text.partition(":")
    parsed = parse_number(raw)
    return parsed if parsed is not None else 0


def normalize_optional_max(value: int | None) -> int | None:
    return None if value is None else max(0, value)


def action_signal_count_map(state: LogState) -> dict[str, int]:
    return dict(state.action_signal_counts)


def action_var_count_map(state: LogState) -> dict[str, int]:
    return dict(state.action_var_counts)


def trace_var_count_map(state: LogState) -> dict[str, int]:
    return dict(state.trace_var_counts)


def missing_required_action_signals(state: LogState, required_action_signals: tuple[str, ...]) -> list[str]:
    counts = action_signal_count_map(state)
    return [signal for signal in required_action_signals if is_present_action_signal(signal) and counts.get(signal, 0) <= 0]


def has_required_action_signals(state: LogState, required_action_signals: tuple[str, ...]) -> bool:
    return not missing_required_action_signals(state, required_action_signals)


def missing_required_action_vars(state: LogState, required_action_vars: tuple[str, ...]) -> list[str]:
    counts = action_var_count_map(state)
    return [variable for variable in required_action_vars if variable and counts.get(variable, 0) <= 0]


def has_required_action_vars(state: LogState, required_action_vars: tuple[str, ...]) -> bool:
    return not missing_required_action_vars(state, required_action_vars)


def missing_required_trace_vars(state: LogState, required_trace_vars: tuple[str, ...]) -> list[str]:
    counts = trace_var_count_map(state)
    return [variable for variable in required_trace_vars if variable and counts.get(variable, 0) <= 0]


def has_required_trace_vars(state: LogState, required_trace_vars: tuple[str, ...]) -> bool:
    return not missing_required_trace_vars(state, required_trace_vars)


def failure_guard_parts(
    max_large_vanilla_rewrites: int | None,
    max_rewrite_failures: int | None,
    max_death_events: int | None = None,
    max_death_write_failures: int | None = None,
) -> list[str]:
    parts: list[str] = []
    if max_large_vanilla_rewrites is not None:
        parts.append(f"max {max_large_vanilla_rewrites} large vanilla rewrite(s)")
    if max_rewrite_failures is not None:
        parts.append(f"max {max_rewrite_failures} rewrite failure(s)")
    if max_death_events is not None:
        parts.append(f"max {max_death_events} death event(s)")
    if max_death_write_failures is not None:
        parts.append(f"max {max_death_write_failures} death write failure(s)")
    return parts


def failure_guards_pass(
    state: LogState,
    max_large_vanilla_rewrites: int | None = None,
    max_rewrite_failures: int | None = None,
    max_death_events: int | None = None,
    max_death_write_failures: int | None = None,
) -> bool:
    return describe_failure_guard(
        state,
        max_large_vanilla_rewrites,
        max_rewrite_failures,
        max_death_events,
        max_death_write_failures,
    ) is None


def describe_failure_guard(
    state: LogState,
    max_large_vanilla_rewrites: int | None = None,
    max_rewrite_failures: int | None = None,
    max_death_events: int | None = None,
    max_death_write_failures: int | None = None,
) -> str | None:
    if not (state.exists and state.is_fresh and state.has_current_header):
        return None
    if max_large_vanilla_rewrites is not None and state.large_vanilla_rewrites > max_large_vanilla_rewrites:
        return (
            f"failed: observed too many large vanilla HP rewrites "
            f"({state.large_vanilla_rewrites}/{max_large_vanilla_rewrites} allowed){evidence_suffix(state)}"
        )
    if max_rewrite_failures is not None and state.rewrite_failures > max_rewrite_failures:
        return (
            f"failed: observed too many rewrite failures "
            f"({state.rewrite_failures}/{max_rewrite_failures} allowed){evidence_suffix(state)}"
        )
    if max_death_events is not None and state.death_events > max_death_events:
        return (
            f"failed: observed too many death events "
            f"({state.death_events}/{max_death_events} allowed){evidence_suffix(state)}"
        )
    if max_death_write_failures is not None and state.death_write_failures > max_death_write_failures:
        return (
            f"failed: observed too many death write failures "
            f"({state.death_write_failures}/{max_death_write_failures} allowed){evidence_suffix(state)}"
        )
    return None


def evidence_suffix(state: LogState) -> str:
    parts: list[str] = []
    if state.action_signal_counts:
        summary = ",".join(f"{signal}={count}" for signal, count in state.action_signal_counts[:6])
        parts.append(f"action signals {summary}")
    if state.action_var_counts:
        summary = ",".join(f"{name}={count}" for name, count in state.action_var_counts[:8])
        parts.append(f"action vars {summary}")
    if state.target_slot_present_events:
        parts.append(f"{state.target_slot_present_events} target slot present")
    if state.attacker_slot_present_events:
        parts.append(f"{state.attacker_slot_present_events} attacker slot present")
    if state.equipment_dr_events:
        parts.append(f"{state.equipment_dr_events} equipment DR")
    if state.response_events:
        parts.append(f"{state.response_events} response")
    if state.trace_var_counts:
        summary = ",".join(f"{name}={count}" for name, count in state.trace_var_counts[:8])
        parts.append(f"trace vars {summary}")
    if state.actor_probe_events:
        parts.append(f"{state.actor_probe_events} [ACTOR-PROBE]")
    if state.ct_actor_resolved:
        parts.append(f"{state.ct_actor_resolved} CT attacker")
    if state.ct_drop_resolved:
        parts.append(f"{state.ct_drop_resolved} CT-drop")
    if state.ct_lowest_resolved:
        parts.append(f"{state.ct_lowest_resolved} CT-lowest")
    if state.death_events:
        parts.append(f"{state.death_events} [DEATH-*]")
    if state.death_writes:
        parts.append(f"{state.death_writes} [DEATH-WRITE]")
    if state.death_write_failures:
        parts.append(f"{state.death_write_failures} [DEATH-WRITE-FAILED]")
    if state.death_write_skips:
        parts.append(f"{state.death_write_skips} [DEATH-WRITE-SKIP]")
    if state.rewrite_failures:
        parts.append(f"{state.rewrite_failures} [REWRITE-FAILED]")
    if state.placeholder_rewrites:
        parts.append(f"{state.placeholder_rewrites} placeholder rewrite")
    if state.hp_healing_rewrites:
        parts.append(f"{state.hp_healing_rewrites} HP healing rewrite")
    if state.lethal_hp_rewrites:
        parts.append(f"{state.lethal_hp_rewrites} lethal HP rewrite")
    if state.large_vanilla_rewrites:
        parts.append(f"{state.large_vanilla_rewrites} large vanilla rewrite")
    if state.mp_loss_rewrites:
        parts.append(f"{state.mp_loss_rewrites} MP loss rewrite")
    if state.mp_gain_rewrites:
        parts.append(f"{state.mp_gain_rewrites} MP gain rewrite")
    if state.memory_table_found:
        parts.append(f"{state.memory_table_found} [MEMTABLE-FOUND]")
    if state.memory_table_rows:
        parts.append(f"{state.memory_table_rows} [MEMTABLE-ROW]")
    return "; " + ", ".join(parts) if parts else ""


def run_analyzer(log: Path, output: Path) -> None:
    cmd = [sys.executable, str(ROOT / "tools" / "analyze_battleprobe_log.py"), str(log), "-o", str(output)]
    subprocess.run(cmd, check=True)


def run_promoter(args: argparse.Namespace) -> None:
    cmd = [
        sys.executable,
        str(ROOT / "tools" / "promote_runtime_offsets.py"),
        str(args.log),
        "--base-settings",
        str(args.base_settings),
        "--output",
        str(args.promoted_output),
        "--min-events",
        str(args.min_events),
    ]
    if args.also_policy:
        cmd.extend(
            [
                "--also-policy",
                "--policy-base-settings",
                str(args.policy_base_settings),
                "--policy-output",
                str(args.policy_output),
            ]
        )
    subprocess.run(cmd, check=True)


if __name__ == "__main__":
    raise SystemExit(main())
