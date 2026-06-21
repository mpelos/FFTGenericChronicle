#!/usr/bin/env python3
"""Smoke tests for battle runtime log tooling."""
from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

from analyze_battleprobe_log import (
    build_warnings,
    parse_death_line,
    parse_hp_event_line,
    parse_mp_event_line,
    parse_memory_table_events,
    parse_rewrite_line,
    parse_runtime_contexts,
    render_death_summary,
    render_death_gate_summary,
    render_dr_response_proof_summary,
    render_hp_write_proof_summary,
    render_memory_table_summary,
    render_mp_rewrite_summary,
    render_neuter_placeholder_summary,
    render_runtime_summary,
)
from promote_runtime_offsets import (
    apply_promotions,
    find_promotions,
    read_runtime_contexts,
)
from watch_live_mapping import describe_failure_guard, describe_state, inspect_log


SAMPLE_RUNTIME_1 = (
    "event=damage | attacker=0x1000:recent-unit | "
    "action=sword from attacker weapon:source=vanilla-damage:signal=101:"
    "vars=family_sword=1,routine_pa_wp=1,swing=1,wp=16 | "
    "targetSlots=body(present,id=172:Leather Armor,off=0x70,width=Byte,matches=1) | "
    "attackerSlots=weapon(present,id=19:Broadsword,off=0x50,width=Byte,matches=1) | "
    "equipmentDr=0:NoEquipmentDR | "
    "response=raw950/permille950/rules1/clamped0:DamageResponse(leather swing) | "
    "vars=gross=192,penetrating=182,result.final=20 | "
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
    "vars=gross=200,penetrating=180,result.final=24 | "
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
    check(runtime_details[0]["action"]["variables"]["swing"] == "1", "action variables should parse")
    check(runtime_details[0]["target_slots"][0]["offset_int"] == 112, "target offset should parse")
    check(runtime_details[0]["attacker_slots"][0]["item_name"] == "Broadsword", "attacker item should parse")
    check(runtime_details[0]["response"]["permille"] == 950, "response permille should parse")
    check(runtime_details[0]["trace_vars"]["gross"] == "192", "formula trace vars should parse")

    report = "\n".join(render_runtime_summary(runtime_details))
    check("Candidate exact `Offset=112`, `Width=Byte`" in report, "target offset recommendation missing")
    check("Candidate exact `Offset=80`, `Width=Byte`" in report, "attacker offset recommendation missing")
    check("DamageResponse(leather swing)" in report, "response summary missing")
    check("Action Variables" in report and "`swing`" in report and "`wp`" in report, "action variable summary missing")
    check("Formula Trace Variables" in report and "`gross`" in report and "192..200" in report, "trace var summary missing")
    dr_response_report = "\n".join(render_dr_response_proof_summary(runtime_details))
    check("DR/Response Proof Check" in dr_response_report, "DR/response proof heading missing")
    check("response-context candidate" in dr_response_report, "response-only runtime details should classify as response-context candidate")
    dr_runtime_details = parse_runtime_contexts(
        [
            (
                "0x2000",
                SAMPLE_RUNTIME_1
                .replace("equipmentDr=0:NoEquipmentDR", "equipmentDr=6:ArmorDR")
                .replace("final=20:FinalDamageFormula", "final=182:FinalDamageFormula+DamageResponse(leather swing)"),
            )
        ]
    )
    dr_response_report = "\n".join(render_dr_response_proof_summary(dr_runtime_details))
    check("pass-candidate" in dr_response_report, "DR + response runtime details should classify as pass-candidate")

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
    ambiguous_report = "\n".join(render_dr_response_proof_summary(ambiguous_details))
    check("slot scan ambiguity" in ambiguous_report, "DR/response proof should flag ambiguous slot scans")
    ambiguous_promotions, ambiguous_rejected = find_promotions(ambiguous_details, min_events=1)
    check(
        all(not (promotion.scope == "target" and promotion.slot == "body") for promotion in ambiguous_promotions),
        "ambiguous target slot should not promote",
    )
    check(
        any("target.body" in line and "non-present" in line for line in ambiguous_rejected),
        "ambiguous rejection reason missing",
    )

    hp_event = parse_hp_event_line("[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 100 -> 72 = 28 sampleAgeMs=25", 10)
    hp_rewrite = parse_rewrite_line(
        "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=28 finalDamage=1 HP 72->99",
        11,
    )
    check(hp_event is not None and hp_event["sample_age_ms"] == 25, "HP sample age should parse")
    check(hp_rewrite is not None and hp_rewrite["final"] == 1, "HP rewrite final damage should parse")
    hp_proof = "\n".join(render_hp_write_proof_summary([hp_event], [hp_rewrite]))
    check("pass-candidate" in hp_proof, "fresh finalDamage=1 proof should pass as candidate")

    old_hp_event = parse_hp_event_line("[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 100 -> 72 = 28", 12)
    old_hp_proof = "\n".join(render_hp_write_proof_summary([old_hp_event], [hp_rewrite]))
    check("sampleAgeMs" in old_hp_proof and "inconclusive" in old_hp_proof, "old HP proof logs should be inconclusive")

    failed_rewrite = parse_rewrite_line("[GC-Probe] [REWRITE-FAILED ptr=0x2000 id=0x80] range not writable 0x1234+0x2", 13)
    failed_proof = "\n".join(render_hp_write_proof_summary([hp_event], [hp_rewrite, failed_rewrite]))
    check("rewrite failures" in failed_proof and "failed" in failed_proof, "HP proof should flag rewrite failures")

    mp_loss_event = parse_mp_event_line("[GC-Probe] [MPLOSS ptr=0x2000 id=0x80] 20 -> 12 = 8 sampleAgeMs=25", 20)
    mp_gain_event = parse_mp_event_line("[GC-Probe] [MPGAIN ptr=0x2001 id=0x81] 10 -> 18 = 8 sampleAgeMs=30", 21)
    mp_loss_rewrite = parse_rewrite_line(
        "[GC-Probe] [MP-REWRITE-DRY-RUN ptr=0x2000 id=0x80] rule=FinalMpChangeFormula vanillaMpChange=-8 finalMpChange=-11 MP 12->9",
        22,
    )
    mp_gain_rewrite = parse_rewrite_line(
        "[GC-Probe] [MP-REWRITE ptr=0x2001 id=0x81] rule=FinalMpChangeFormula vanillaMpChange=8 finalMpChange=11 MP 18->21",
        23,
    )
    mp_failed_rewrite = parse_rewrite_line("[GC-Probe] [MP-REWRITE-FAILED ptr=0x2001 id=0x81] range not writable 0x1234+0x2", 24)
    check(mp_loss_event is not None and mp_loss_event["change"] == -8, "MP loss event should parse signed change")
    check(mp_gain_event is not None and mp_gain_event["change"] == 8, "MP gain event should parse signed change")
    check(mp_loss_rewrite is not None and mp_loss_rewrite["final"] == -11, "MP loss rewrite final change should parse")
    check(mp_gain_rewrite is not None and mp_gain_rewrite["final"] == 11, "MP gain rewrite final change should parse")
    mp_report = "\n".join(render_mp_rewrite_summary([mp_loss_event, mp_gain_event], [mp_loss_rewrite, mp_gain_rewrite]))
    check("MP Rewrite Check" in mp_report, "MP rewrite summary heading missing")
    check("MP loss rewrites: 1" in mp_report and "MP gain rewrites: 1" in mp_report, "MP rewrite summary should count loss/gain")
    check("pass-candidate" in mp_report and "finalMpChange" in mp_report, "MP rewrite summary should pass clean rewrites")
    failed_mp_report = "\n".join(render_mp_rewrite_summary([mp_loss_event], [mp_loss_rewrite, mp_failed_rewrite]))
    check("MP rewrite failures: 1" in failed_mp_report and "failed: MP rewrite failures" in failed_mp_report, "MP rewrite summary should flag failures")

    death_gate_rewrite = parse_rewrite_line(
        "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=1 finalDamage=9999 HP 99->0",
        17,
    )
    check(death_gate_rewrite is not None, "death-gate rewrite should parse")
    neuter_report = "\n".join(render_neuter_placeholder_summary([death_gate_rewrite]))
    check("pass-candidate" in neuter_report and "Death-gate lethal rewrites" in neuter_report, "neuter summary should pass placeholder-sized damage")
    stat_placeholder_rewrite = parse_rewrite_line(
        "[GC-Probe] [REWRITE-DRY-RUN ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=24 finalDamage=24 HP 76->76",
        18,
    )
    check(stat_placeholder_rewrite is not None, "stat-sized placeholder rewrite should parse")
    stat_neuter_report = "\n".join(render_neuter_placeholder_summary([stat_placeholder_rewrite]))
    check("pass-candidate" in stat_neuter_report and "`1..30`" in stat_neuter_report, "stat-sized placeholder damage should pass")
    high_vanilla_rewrite = parse_rewrite_line(
        "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=126 finalDamage=9999 HP 99->0",
        19,
    )
    check(high_vanilla_rewrite is not None, "large vanilla rewrite should parse")
    high_neuter_report = "\n".join(render_neuter_placeholder_summary([high_vanilla_rewrite]))
    check("attention: large vanilla damage" in high_neuter_report, "neuter summary should flag large vanilla damage")

    death_diff = parse_death_line("[GC-Probe] [DEATH-DIFF ptr=0x2000 id=0x80] alive->dead +0x30->00 +0x61:00->20", 14)
    death_write = parse_death_line("[GC-Probe] [DEATH-WRITE ptr=0x2000 id=0x80] KO flag +0x61 w1 0->20", 15)
    death_failed = parse_death_line("[GC-Probe] [DEATH-WRITE-FAILED ptr=0x2000 id=0x80] KO flag: range not writable", 16)
    check(death_diff is not None and death_diff["offsets"] == ["30", "61"], "death diff offsets should parse")
    check(death_write is not None and death_write["offset_int"] == 0x61 and death_write["to_value"] == 0x20, "death write should parse")
    death_report = "\n".join(render_death_summary([death_diff, death_write]))
    check("+0x61" in death_report and "write-candidate" in death_report, "death summary should highlight KO flag writes")
    failed_death_report = "\n".join(render_death_summary([death_diff, death_failed]))
    check("death-state write failures" in failed_death_report, "death summary should flag write failures")
    hp_only_death_gate_report = "\n".join(render_death_gate_summary([death_gate_rewrite], [death_diff]))
    check(
        "Death Gate Outcome" in hp_only_death_gate_report and "HP-only branch evidence" in hp_only_death_gate_report,
        "death gate summary should classify HP-only death evidence",
    )
    zombie_death_gate_report = "\n".join(render_death_gate_summary([death_gate_rewrite], []))
    check("zombie-candidate" in zombie_death_gate_report, "death gate summary should classify missing death evidence")
    killflag_death_gate_report = "\n".join(render_death_gate_summary([death_gate_rewrite], [death_diff, death_write]))
    check("killflag-branch evidence" in killflag_death_gate_report, "death gate summary should classify KO flag writes")
    large_death_gate_report = "\n".join(render_death_gate_summary([high_vanilla_rewrite], []))
    check("large vanilla damage" in large_death_gate_report, "death gate summary should warn on non-placeholder lethal rewrites")
    failed_death_gate_report = "\n".join(render_death_gate_summary([death_gate_rewrite], [death_failed]))
    check("death-state write failures" in failed_death_gate_report, "death gate summary should fail on death write failures")

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
        report_path = Path(tmp) / "battleprobe_analysis.md"
        log_path.write_text(
            "\n".join(
                [
                    "==== Generic Chronicle Battle Runtime Harness (iter 20) ====",
                    "settings: RewriteObservedDamage=True CauseDeathOnZeroHp=False",
                    "[GC-Probe] [UNIT ptr=0x2000 id=0x80 foe  t3] Lv1 HP250 PA4 MA3 Sp6 Mv5 Jp3 Br59 Fa51",
                    "[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 250 -> 249 = 1 sampleAgeMs=25",
                    "[GC-Probe] [RUNTIME ptr=0x2000 id=0x80] " + SAMPLE_RUNTIME_1,
                    "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=1 finalDamage=9999 HP 249->0",
                    "[GC-Probe] [DEATH-DIFF ptr=0x2000 id=0x80] alive->dead +0x30->00 +0x61:00->20",
                    "",
                ]
            ),
            encoding="utf-8",
        )
        subprocess.run(
            [
                sys.executable,
                str(Path(__file__).with_name("analyze_battleprobe_log.py")),
                str(log_path),
                "-o",
                str(report_path),
                "--no-catalog",
            ],
            check=True,
            stdout=subprocess.DEVNULL,
        )
        report = report_path.read_text(encoding="utf-8")
        check("## Death Gate Outcome" in report, "full analyzer report should include Death Gate Outcome")
        check("HP-only branch evidence" in report, "full analyzer report should classify HP-only death gate evidence")
        check("### Neuter Placeholder Check" in report, "full analyzer report should keep neuter placeholder check")

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
        check("waiting for [MEMTABLE-FOUND]" in describe_state(fresh_state, 0, required_memtable_found=1), "watcher should wait for memory table found evidence")

        memory_log = fresh_log + "\n".join(SAMPLE_MEMTABLE_LINES[:3]) + "\n"
        log_path.write_text(memory_log, encoding="utf-8")
        memory_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(memory_state.memory_table_found == 1, "watcher should count found memory table probes")
        check(memory_state.memory_table_rows == 1, "watcher should count memory table rows")
        check("[MEMTABLE-FOUND]" in describe_state(memory_state, 1), "watcher message should mention memory tables")
        check("ready: 0 [RUNTIME]" in describe_state(memory_state, 0, required_memtable_found=1, required_memtable_rows=1), "watcher should accept required memory-table evidence")
        check("waiting for [MEMTABLE-ROW]" in describe_state(memory_state, 0, required_memtable_found=1, required_memtable_rows=2), "watcher should wait for enough memory-table rows")

        ready_log = memory_log + "[GC-Probe] [RUNTIME ptr=0x2000 id=0x80] " + SAMPLE_RUNTIME_1 + "\n"
        log_path.write_text(ready_log, encoding="utf-8")
        ready_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(ready_state.is_ready, "ready watcher state should be ready")
        check(ready_state.action_signal_events == 1, "watcher should count nonzero action signals")
        check(dict(ready_state.action_var_counts).get("swing") == 1, "watcher should count action variables")
        check(ready_state.target_slot_present_events == 1, "watcher should count present target slots")
        check(ready_state.attacker_slot_present_events == 1, "watcher should count present attacker slots")
        check(ready_state.response_events == 1, "watcher should count response-rule runtime events")
        check(dict(ready_state.trace_var_counts).get("gross") == 1, "watcher should count nonzero trace vars")
        check("ready: 1 [RUNTIME]" in describe_state(ready_state, 1), "ready watcher message should mention runtime count")
        check("action signals 101=1" in describe_state(ready_state, 1), "watcher message should summarize action signals")
        check("action vars" in describe_state(ready_state, 1) and "swing=1" in describe_state(ready_state, 1), "watcher message should summarize action vars")
        check("target slot present" in describe_state(ready_state, 1), "watcher message should summarize target slot evidence")
        check("response" in describe_state(ready_state, 1), "watcher message should summarize response evidence")
        check("waiting for action signals" in describe_state(ready_state, 0, required_action_signal_events=2), "watcher should wait for enough action signals")
        check("waiting for action signal(s) 301" in describe_state(ready_state, 0, required_action_signals=("301",)), "watcher should wait for required action signal")
        check("ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_action_signals=("101",)), "watcher should accept required action signal")
        check("waiting for action variable(s) thrust" in describe_state(ready_state, 0, required_action_vars=("thrust",)), "watcher should wait for required action var")
        check("ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_action_vars=("swing",)), "watcher should accept required action var")
        check(
            "ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_target_slot_present_events=1),
            "watcher should accept required target slot evidence",
        )
        check(
            "ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_attacker_slot_present_events=1),
            "watcher should accept required attacker slot evidence",
        )
        check(
            "ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_response_events=1),
            "watcher should accept required response evidence",
        )
        check(
            "ready: 1 [RUNTIME]" in describe_state(ready_state, 0, required_trace_vars=("gross",)),
            "watcher should accept required trace variable evidence",
        )
        check(
            "waiting for equipment DR evidence" in describe_state(ready_state, 0, required_equipment_dr_events=1),
            "watcher should wait for positive equipment DR evidence",
        )
        check(
            "waiting for trace variable(s) missing" in describe_state(ready_state, 0, required_trace_vars=("missing",)),
            "watcher should wait for missing required trace variables",
        )
        check("waiting for [REWRITE]" in describe_state(ready_state, 0, 1), "watcher should wait for rewrite evidence when requested")

        dr_log = memory_log + "[GC-Probe] [RUNTIME ptr=0x2000 id=0x80] " + SAMPLE_RUNTIME_1.replace(
            "equipmentDr=0:NoEquipmentDR",
            "equipmentDr=6:ArmorDR",
        ) + "\n"
        log_path.write_text(dr_log, encoding="utf-8")
        dr_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(dr_state.equipment_dr_events == 1, "watcher should count positive equipment DR events")
        check(
            "ready: 1 [RUNTIME]" in describe_state(dr_state, 0, required_equipment_dr_events=1),
            "watcher should accept required equipment DR evidence",
        )

        rewrite_log = (
            ready_log
            + "[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 200 -> 74 = 126 sampleAgeMs=25\n"
            + "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=126 finalDamage=1 HP 74->199\n"
        )
        log_path.write_text(rewrite_log, encoding="utf-8")
        rewrite_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(rewrite_state.rewrite_events == 1, "watcher should count rewrite events")
        check(rewrite_state.rewrite_failures == 0, "successful rewrite should not count as failed")
        check("1 [REWRITE]" in describe_state(rewrite_state, 0, 1), "watcher ready message should mention rewrite count")
        check("waiting for placeholder-sized HP rewrites" in describe_state(rewrite_state, 0, 1, 1), "watcher should wait for placeholder rewrites when requested")
        check("large vanilla rewrite" in describe_state(rewrite_state, 0, 1, 1), "watcher message should flag large vanilla rewrites")
        check(
            "too many large vanilla HP rewrites" in str(describe_failure_guard(rewrite_state, max_large_vanilla_rewrites=0)),
            "watcher failure guard should reject large vanilla rewrites",
        )

        failed_rewrite_log = ready_log + "[GC-Probe] [REWRITE-FAILED ptr=0x2000 id=0x80] range not writable 0x1234+0x2\n"
        log_path.write_text(failed_rewrite_log, encoding="utf-8")
        failed_rewrite_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(failed_rewrite_state.rewrite_events == 1, "watcher should count failed rewrite events")
        check(failed_rewrite_state.rewrite_failures == 1, "watcher should count rewrite failures")
        check("[REWRITE-FAILED]" in describe_state(failed_rewrite_state, 0), "watcher message should mention rewrite failures")
        check(
            "too many rewrite failures" in str(describe_failure_guard(failed_rewrite_state, max_rewrite_failures=0)),
            "watcher failure guard should reject rewrite failures",
        )

        placeholder_log = (
            ready_log
            + "[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 100 -> 76 = 24 sampleAgeMs=25\n"
            + "[GC-Probe] [REWRITE-DRY-RUN ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=24 finalDamage=24 HP 76->76\n"
        )
        log_path.write_text(placeholder_log, encoding="utf-8")
        placeholder_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(placeholder_state.placeholder_rewrites == 1, "watcher should count placeholder-sized rewrites")
        check(placeholder_state.hp_healing_rewrites == 0, "damage rewrite should not count as HP healing")
        check(placeholder_state.mp_loss_rewrites == 0 and placeholder_state.mp_gain_rewrites == 0, "HP rewrite should not count as MP")
        check(placeholder_state.large_vanilla_rewrites == 0, "watcher should not flag placeholder-sized rewrites as large")
        check(describe_failure_guard(placeholder_state, max_large_vanilla_rewrites=0) is None, "placeholder rewrite should pass large-vanilla guard")
        check("1 placeholder rewrite" in describe_state(placeholder_state, 0, 0, 1), "watcher ready message should mention placeholder rewrites")
        check(
            "waiting for HP healing rewrites" in describe_state(placeholder_state, 0, required_hp_healing_rewrites=1),
            "watcher should wait for HP healing rewrites when requested",
        )
        check(
            "waiting for lethal HP rewrites" in describe_state(placeholder_state, 0, required_lethal_hp_rewrites=1),
            "watcher should wait for lethal HP rewrites when requested",
        )
        check("waiting for [DEATH-WRITE]" in describe_state(placeholder_state, 0, 0, 1, 0, 1), "watcher should wait for death writes when requested")

        healing_log = (
            ready_log
            + "[GC-Probe] [HEALING ptr=0x2000 id=0x80] 80 -> 92 = 12 sampleAgeMs=25\n"
            + "[GC-Probe] [REWRITE-DRY-RUN ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=-12 finalDamage=-19 HP 92->99\n"
        )
        log_path.write_text(healing_log, encoding="utf-8")
        healing_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(healing_state.hp_healing_rewrites == 1, "watcher should count HP healing rewrites")
        check("1 HP healing rewrite" in describe_state(healing_state, 0, required_hp_healing_rewrites=1), "watcher ready message should mention HP healing rewrites")
        check(
            "waiting for HP healing rewrites" in describe_state(healing_state, 0, required_hp_healing_rewrites=2),
            "watcher should wait for enough HP healing rewrites",
        )

        mp_log = (
            ready_log
            + "[GC-Probe] [MP-REWRITE-DRY-RUN ptr=0x2000 id=0x80] rule=FinalMpChangeFormula vanillaMpChange=-8 finalMpChange=-11 MP 12->9\n"
            + "[GC-Probe] [MP-REWRITE-DRY-RUN ptr=0x2001 id=0x81] rule=FinalMpChangeFormula vanillaMpChange=8 finalMpChange=11 MP 18->21\n"
        )
        log_path.write_text(mp_log, encoding="utf-8")
        mp_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(mp_state.mp_loss_rewrites == 1, "watcher should count MP loss rewrites")
        check(mp_state.mp_gain_rewrites == 1, "watcher should count MP gain rewrites")
        check("1 MP loss rewrite" in describe_state(mp_state, 0, required_mp_loss_rewrites=1), "watcher ready message should mention MP loss rewrites")
        check("1 MP gain rewrite" in describe_state(mp_state, 0, required_mp_gain_rewrites=1), "watcher ready message should mention MP gain rewrites")
        check(
            "waiting for MP loss rewrites" in describe_state(mp_state, 0, required_mp_loss_rewrites=2),
            "watcher should wait for enough MP loss rewrites",
        )
        check(
            "waiting for MP gain rewrites" in describe_state(mp_state, 0, required_mp_gain_rewrites=2),
            "watcher should wait for enough MP gain rewrites",
        )

        lethal_log = (
            ready_log
            + "[GC-Probe] [DAMAGE ptr=0x2000 id=0x80] 100 -> 99 = 1 sampleAgeMs=25\n"
            + "[GC-Probe] [REWRITE ptr=0x2000 id=0x80] rule=FinalDamageFormula vanillaDamage=1 finalDamage=9999 HP 99->0\n"
        )
        log_path.write_text(lethal_log, encoding="utf-8")
        lethal_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(lethal_state.placeholder_rewrites == 1, "lethal watcher rewrite should also count as placeholder-sized")
        check(lethal_state.lethal_hp_rewrites == 1, "watcher should count lethal HP rewrites")
        check("1 lethal HP rewrite" in describe_state(lethal_state, 0, required_lethal_hp_rewrites=1), "watcher ready message should mention lethal HP rewrites")
        check(
            "waiting for lethal HP rewrites" in describe_state(lethal_state, 0, required_lethal_hp_rewrites=2),
            "watcher should wait for enough lethal HP rewrites",
        )

        death_log = (
            lethal_log
            + "[GC-Probe] [DEATH-DIFF ptr=0x2000 id=0x80] alive->dead +0x30->00 +0x61:00->20\n"
            + "[GC-Probe] [DEATH-WRITE ptr=0x2000 id=0x80] KO flag +0x61 w1 0->20\n"
        )
        log_path.write_text(death_log, encoding="utf-8")
        death_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(death_state.death_events == 2, "watcher should count death events")
        check(death_state.death_writes == 1, "watcher should count concrete death writes")
        check(death_state.lethal_hp_rewrites == 1, "watcher should preserve lethal HP rewrite count with death evidence")
        check("1 [DEATH-WRITE]" in describe_state(death_state, 0, 1, 1, 0, 1), "watcher ready message should mention death writes")
        check(
            "ready:" in describe_state(death_state, 0, 0, 0, 1, 1, required_lethal_hp_rewrites=1),
            "watcher should accept lethal HP rewrite plus death evidence",
        )
        check(
            "too many death events" in str(describe_failure_guard(death_state, max_death_events=0)),
            "watcher failure guard should reject unexpected death evidence",
        )
        check(
            describe_failure_guard(lethal_state, max_death_events=0) is None,
            "watcher death-event guard should pass when no death evidence is present",
        )

        failed_death_log = death_log + "[GC-Probe] [DEATH-WRITE-FAILED ptr=0x2000 id=0x80] KO flag: range not writable\n"
        log_path.write_text(failed_death_log, encoding="utf-8")
        failed_death_state = inspect_log(log_path, start_time=0.0, allow_existing=True)
        check(failed_death_state.death_write_failures == 1, "watcher should count death write failures")
        check("[DEATH-WRITE-FAILED]" in describe_state(failed_death_state, 0, 1, 1, 0, 1), "watcher message should mention death write failures")
        check(
            "too many death write failures" in str(describe_failure_guard(failed_death_state, max_death_write_failures=0)),
            "watcher failure guard should reject death write failures",
        )

    print("runtime tooling smoke tests passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
