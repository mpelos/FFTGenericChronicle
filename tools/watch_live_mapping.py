#!/usr/bin/env python3
"""Watch a live mapping log until the current runtime emits [RUNTIME] evidence."""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path

from analyze_battleprobe_log import DEFAULT_LOG, MEMTABLE_FOUND_RE, MEMTABLE_ROW_RE, OLD_UNIT_RE, RUNTIME_RE
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
    memory_table_found: int = 0
    memory_table_rows: int = 0

    @property
    def is_ready(self) -> bool:
        return self.exists and self.is_fresh and self.has_current_header and self.runtime_events > 0


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Wait for fresh Generic Chronicle [RUNTIME] mapping evidence.")
    p.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    p.add_argument("--runtime-events", type=int, default=1, help="Minimum [RUNTIME] lines to wait for.")
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
    required_runtime_events = max(1, args.runtime_events)
    last_message = ""

    print(f"watching {args.log}")
    print(f"waiting for current runtime header and {required_runtime_events} [RUNTIME] event(s)")

    while time.time() <= deadline:
        state = inspect_log(args.log, start_time, args.allow_existing)
        message = describe_state(state, required_runtime_events)
        if message != last_message:
            print(message)
            last_message = message

        if state.exists and state.is_fresh and state.has_current_header and state.runtime_events >= required_runtime_events:
            print("runtime mapping evidence is ready")
            if not args.skip_analyze:
                run_analyzer(args.log, args.analysis_output)
            if args.promote:
                run_promoter(args)
            return 0

        time.sleep(max(0.1, args.interval))

    state = inspect_log(args.log, start_time, args.allow_existing)
    print("timeout waiting for runtime mapping evidence")
    print(describe_state(state, required_runtime_events))
    return 1


def inspect_log(log: Path, start_time: float = 0.0, allow_existing: bool = False) -> LogState:
    if not log.exists():
        return LogState(False, 0.0, False, False, False, 0, 0)

    mtime = log.stat().st_mtime
    is_fresh = allow_existing or mtime >= start_time - 0.5
    has_current_header = False
    has_old_header = False
    old_unit_lines = 0
    runtime_events = 0
    memory_table_found = 0
    memory_table_rows = 0

    for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
        if "Generic Chronicle Battle Runtime Harness" in line:
            has_current_header = True
        if "Generic Chronicle Battle Validation Harness" in line:
            has_old_header = True
        if OLD_UNIT_RE.search(line):
            old_unit_lines += 1
        if RUNTIME_RE.search(line):
            runtime_events += 1
        if MEMTABLE_FOUND_RE.search(line):
            memory_table_found += 1
        if MEMTABLE_ROW_RE.search(line):
            memory_table_rows += 1

    return LogState(
        True,
        mtime,
        is_fresh,
        has_current_header,
        has_old_header,
        old_unit_lines,
        runtime_events,
        memory_table_found,
        memory_table_rows,
    )


def describe_state(state: LogState, required_runtime_events: int) -> str:
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
            f"({state.runtime_events}/{required_runtime_events}){memory_table_suffix(state)}"
        )
    return f"ready: {state.runtime_events} [RUNTIME] event(s){memory_table_suffix(state)}"


def memory_table_suffix(state: LogState) -> str:
    parts: list[str] = []
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
