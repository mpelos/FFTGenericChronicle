#!/usr/bin/env python3
"""Smoke tests for battle runtime log tooling."""
from __future__ import annotations

import tempfile
from pathlib import Path

from analyze_battleprobe_log import (
    build_warnings,
    parse_memory_table_events,
    parse_runtime_contexts,
    render_memory_table_summary,
    render_runtime_summary,
)
from promote_runtime_offsets import (
    apply_promotions,
    find_promotions,
    read_runtime_contexts,
)
from watch_live_mapping import describe_state, inspect_log


SAMPLE_RUNTIME_1 = (
    "event=damage | attacker=0x1000:recent-unit | "
    "action=sword from attacker weapon:source=vanilla-damage:signal=101:"
    "vars=family_sword=1,routine_pa_wp=1,swing=1,wp=16 | "
    "targetSlots=body(present,id=172:Leather Armor,off=0x70,width=Byte,matches=1) | "
    "attackerSlots=weapon(present,id=19:Broadsword,off=0x50,width=Byte,matches=1) | "
    "equipmentDr=0:NoEquipmentDR | "
    "response=raw950/permille950/rules1/clamped0:DamageResponse(leather swing) | "
    "final=20:FinalDamageFormula"
)

SAMPLE_RUNTIME_2 = (
    "event=damage | attacker=0x1100:recent-unit | "
    "action=sword from attacker weapon:source=vanilla-damage:signal=101:"
    "vars=family_sword=1,routine_pa_wp=1,swing=1,wp=16 | "
    "targetSlots=body(present,id=175:Chainmail,off=0x70,width=Byte,matches=1) | "
    "attackerSlots=weapon(present,id=20:Longsword,off=0x50,width=Byte,matches=1) | "
    "equipmentDr=0:NoEquipmentDR | "
    "response=raw750/permille750/rules1/clamped0:DamageResponse(mail swing) | "
    "final=24:FinalDamageFormula"
)

AMBIGUOUS_RUNTIME = (
    "event=damage | attacker=0x1000:recent-unit | "
    "action=sword from attacker weapon:source=vanilla-damage:signal=101:vars=swing=1 | "
    "targetSlots=body(ambiguous,id=0,off=0x70,width=Byte,matches=2) | "
    "attackerSlots=weapon(present,id=19:Broadsword,off=0x50,width=Byte,matches=1) | "
    "equipmentDr=0:NoEquipmentDR | response=raw1000/permille1000/rules0/clamped0:NoDamageResponse | "
    "final=20:FinalDamageFormula"
)

SAMPLE_MEMTABLE_LINES = [
    "[GC-Probe] [MEMTABLE] configured=2 enabled=1",
    "[GC-Probe] [MEMTABLE-FOUND Roster Unit Table] scan=module+0x1A2B table=0x000001234000 stride=0x258 count=55 fields=3",
    "[GC-Probe] [MEMTABLE-ROW Roster Unit Table] row=0 addr=0x000001234000 present=2 unitIndex=7 job=12 rosterWord=0x1234",
    "[GC-Probe] [MEMTABLE-NOTFOUND Action Table] pattern not found",
]


def main() -> int:
    runtime_details = parse_runtime_contexts(
        [
            ("0x2000", SAMPLE_RUNTIME_1),
            ("0x2100", SAMPLE_RUNTIME_2),
        ]
    )
    check(runtime_details[0]["event"] == "damage", "event should parse")
    check(runtime_details[0]["action"]["signal"] == "101", "action signal should parse")
    check(runtime_details[0]["target_slots"][0]["offset_int"] == 112, "target offset should parse")
    check(runtime_details[0]["attacker_slots"][0]["item_name"] == "Broadsword", "attacker item should parse")
    check(runtime_details[0]["response"]["permille"] == 950, "response permille should parse")

    report = "\n".join(render_runtime_summary(runtime_details))
    check("Candidate exact `Offset=112`, `Width=Byte`" in report, "target offset recommendation missing")
    check("Candidate exact `Offset=80`, `Width=Byte`" in report, "attacker offset recommendation missing")
    check("DamageResponse(leather swing)" in report, "response summary missing")

    promotions, rejected = find_promotions(runtime_details, min_events=2)
    check(not rejected, f"stable sample should not reject slots: {rejected}")
    check(len(promotions) == 2, f"expected two promotions, got {len(promotions)}")

    base_settings = {
        "_note": "test",
        "FinalDamageFormula": "vanillaDamage",
        "EquipmentSlots": [{"Name": "Body", "SearchStart": 68, "SearchEnd": 383, "SearchWidth": "Byte"}],
        "AttackerEquipmentSlots": [{"Name": "Weapon", "SearchStart": 68, "SearchEnd": 383, "SearchWidth": "Byte"}],
    }
    promoted = apply_promotions(base_settings, promotions)
    check(promoted["EquipmentSlots"][0] == {"Name": "Body", "Offset": 112, "Width": "Byte"}, "body promotion failed")
    check(
        promoted["AttackerEquipmentSlots"][0] == {"Name": "Weapon", "Offset": 80, "Width": "Byte"},
        "weapon promotion failed",
    )
    check(len(promoted["_promotedOffsets"]) == 2, "promotion metadata missing")

    ambiguous_details = parse_runtime_contexts([("0x2000", AMBIGUOUS_RUNTIME)])
    ambiguous_promotions, ambiguous_rejected = find_promotions(ambiguous_details, min_events=1)
    check(
        all(not (promotion.scope == "target" and promotion.slot == "body") for promotion in ambiguous_promotions),
        "ambiguous target slot should not promote",
    )
    check(
        any("target.body" in line and "non-present" in line for line in ambiguous_rejected),
        "ambiguous rejection reason missing",
    )

    warnings = build_warnings(["==== Generic Chronicle Battle Validation Harness (iter 8) ===="], {}, [], 3)
    check(any("old-format" in warning for warning in warnings), "old harness warning missing")

    memory_events = parse_memory_table_events(SAMPLE_MEMTABLE_LINES)
    check(len(memory_events) == 4, "memory table event count should parse")
    check(memory_events[1]["name"] == "Roster Unit Table", "memory table probe name should parse")
    check(memory_events[1]["stride_int"] == 0x258, "memory table stride should parse")
    check(memory_events[2]["row"] == 0 and memory_events[2]["present"] == 2, "memory table row metadata should parse")
    memory_report = "\n".join(render_memory_table_summary(memory_events))
    check("Roster Unit Table" in memory_report, "memory table found summary missing")
    check("`0x258`" in memory_report and "module+0x1A2B" in memory_report, "memory table found fields missing")
    check("unitIndex=7" in memory_report, "memory table row fields missing")
    check("MEMTABLE-NOTFOUND Action Table" in memory_report, "memory table issue summary missing")

    with tempfile.TemporaryDirectory() as tmp:
        log_path = Path(tmp) / "battleprobe_log.txt"
        log_path.write_text(
            "[GC-Probe] [RUNTIME ptr=0x2000 id=0x80] " + SAMPLE_RUNTIME_1 + "\n",
            encoding="utf-8",
        )
        contexts = read_runtime_contexts(log_path)
        check(len(contexts) == 1 and contexts[0][0] == "0x2000", "runtime context file read failed")

    with tempfile.TemporaryDirectory() as tmp:
        log_path = Path(tmp) / "battleprobe_log.txt"
        start_time = 100.0
        missing_state = inspect_log(log_path, start_time, allow_existing=False)
        check(not missing_state.exists, "missing watcher state should not exist")
        check("not found" in describe_state(missing_state, 1), "missing watcher message should mention not found")

        old_log = (
            "==== Generic Chronicle Battle Validation Harness (iter 8) ====\n"
            "[UNIT id=0x80 foe  t3] Lv1 HP35 PA4 MA3 Sp6 Mv9 Jp7 Br59 Fa51\n"
        )
        log_path.write_text(old_log, encoding="utf-8")
        old_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(old_state.has_old_header and old_state.old_unit_lines == 1, "old watcher state should detect stale log")
        check("stale harness" in describe_state(old_state, 1), "old watcher message should mention stale harness")

        fresh_log = "==== Generic Chronicle Battle Runtime Harness (iter 20) ====\n"
        log_path.write_text(fresh_log, encoding="utf-8")
        fresh_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(fresh_state.has_current_header and fresh_state.runtime_events == 0, "fresh state should detect current header")
        check("waiting for [RUNTIME]" in describe_state(fresh_state, 1), "fresh watcher message should wait for runtime")

        memory_log = fresh_log + "\n".join(SAMPLE_MEMTABLE_LINES[:3]) + "\n"
        log_path.write_text(memory_log, encoding="utf-8")
        memory_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(memory_state.memory_table_found == 1, "watcher should count found memory table probes")
        check(memory_state.memory_table_rows == 1, "watcher should count memory table rows")
        check("[MEMTABLE-FOUND]" in describe_state(memory_state, 1), "watcher message should mention memory tables")

        ready_log = memory_log + "[GC-Probe] [RUNTIME ptr=0x2000 id=0x80] " + SAMPLE_RUNTIME_1 + "\n"
        log_path.write_text(ready_log, encoding="utf-8")
        ready_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(ready_state.is_ready, "ready watcher state should be ready")
        check("ready: 1 [RUNTIME]" in describe_state(ready_state, 1), "ready watcher message should mention runtime count")

    print("runtime tooling smoke tests passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
