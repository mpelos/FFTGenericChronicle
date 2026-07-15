#!/usr/bin/env python3
"""Smoke tests for the action identity log analyzer."""
from __future__ import annotations

import tempfile
from pathlib import Path

from analyze_action_identity_log import load_abilities, parse_log, render_report


SAMPLE_LOG = """\
[GC-Probe] [PRECLAMP-ACTOR-CTX event=7 now=1000 target=0x2000/id=0x1F oldDebit=151 caster=0x1000/id=0x1E casterActor=0x5000 actionId=0 verdict=resolved actors=[0x5000->id=0x1E,0x5100->id=0x1F]
[GC-Probe] [PRECLAMP-EQUIP event=7 side=target ptr=0x2000/id=0x1F head(id=1) body(id=2)
[GC-Probe] [PRECLAMP-EQUIP event=7 side=caster ptr=0x1000/id=0x1E weapon(id=12)
[GC-Probe] [PRECLAMP-IMMEDIATE-CANDIDATES target=0x2000/id=0x1F oldDebit=151 oldCredit=0 minScore=1600 minMargin=250 maxAgeMs=3000 requireFreshActive=0 selected=0x1000/id=0x1E/act=0/score=2050/runnerUp=-2147483648/margin=2147483647] 0x1000/id=0x1E/source-like/score=2050/eligible=1/act=0/currentActive=1/freshAct=0/freshActive=1
[GC-Probe] [PRECLAMP-ACTOR-CTX event=8 now=2000 target=0x2100/id=0x01 oldDebit=85 caster=0x1100/id=0x1E casterActor=0x5200 actionId=16 verdict=resolved actors=[0x5200->id=0x1E,0x5300->id=0x01]
[GC-Probe] [PRECLAMP-ACTOR-CTX event=9 now=2100 target=0x1100/id=0x1E oldDebit=99 caster=0x1100/id=0x1E casterActor=0x5200 actionId=16 verdict=resolved-self actors=[0x5200->id=0x1E/act=16]
[GC-Probe] [PENDING-ACTION-TRACK enter caster=0x1100/id=0x1E act=16 now=1500 touch=0 s61=8/t18D=4/act=16/f1EF=8/dmg1C4=0/cred1C6=0/chg1D8=0/f1E5=0/b8=1/ba=1/bb=0]
[GC-Probe] [PENDING-ACTION-MATCH kind=preclamp-cache event=8 target=0x2100/id=0x01 resolved=0x1100/id=0x1E source=pending-clear batch=1 act=16 batchAge=0ms batchEvent=0/16 consume=0 confidence=damage-cache score=1100000 observed=85 activeBatches=1 trackedPending=0 batches=#1:0x1100/id=0x1E/act=16/age=0ms/events=0 currentCache=dmg1C4=85/chg1D8=2/f1E5=128/bb=0/match=1/dmgExact=1/creditExact=0/lethalClamp=0 recentCache=none]
[GC-Probe] [PENDING-ACTION-TARGET reenter target=0x2200/id=0x80 age=2072ms clearAge=1546ms prev=dmg1C4=0/cred1C6=34/chg1D8=0/f1E5=64/bb=1 next=dmg1C4=422/cred1C6=0/chg1D8=130/f1E5=128/bb=1 touch=0]
[GC-Probe] [PRECLAMP-FORMULA-CANDIDATE event=10 ptr=0x2200 id=0x80 hp=276/276 oldDebit=422 oldCredit=0 forcedDebit=-1 forcedCredit=-1 eventKind=damage shouldStage=0 queuedPlan=0 rule=none attacker=0x3000/id=0x32 source=immediate-action pending=none immediate=selected now=1800 action=immediate-action:source=immediate-action:vars=actionid=0]
[GC-Probe] [HOOK-REGS-EVENT kind=targetcache event=11 hookCount=44 hookAgeMs=8 hookPtr=0x2200 targetPtr=0x2200 id=0x80] rcx=0x3000:unit:id=0x32:team=0:hp=428:ct=8 rdx=0x5400:actor:id=0x32:unit=0x3000:act=0 r8=0x2200:unit:touched stack=+0x20=0x5500:actor:id=0x80:unit=0x2200:act=0
[GC-Probe] [LANDMARK-HIT event=12 id=1 name=target_cache_write_1c4 rva=0x2D7AC0 access=write base=rbx=0x2200:unit:base now=1810 baseRead=unit:id=0x80/team=0/hp=276/ct=90 fields=hp=276/ct=90/s61=0/t18D=255/act=0/dmg1C4=422/cred1C6=0/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=1/raw=+0x1C4=A601] regs=rdx=0x5400:actor:id=0x32:unit=0x3000:act=0 r8=0x2200:unit:base
[GC-Probe] [SELECTOR-PROBE event=4 evadeType=0x00(hit) actor=0x5200:actor:id=0x1E:unit=0x1100:act=16 record=0x2100 unit:id=0x01/team=0/hp=482/ct=70 now=2000 rec+1BB=02 rec+1BE=01 rec+1C0=00 rec+1C4(dmg)=85 rec+1E5=80] window=0x1B8: 00 00 00 02 00 00 01 00 00 00 00 00 00 85 00 00 ctxRegs=[r8=0x5200:actor:id=0x1E:unit=0x1100:act=16] ctxStack=[+0x20=0x5300:actor:record-unit:id=0x01:unit=0x2100:act=0]
[GC-Probe] [SELECTOR-PROBE event=5 evadeType=0x0B(blade-grasp) actor=0x5300:actor:record-unit:id=0x01:unit=0x2100:act=0 record=0x2100 unit:id=0x01/team=0/hp=482/ct=70 now=2500 rec+1BB=01 rec+1BE=00 rec+1C0=0B rec+1C4(dmg)=0 rec+1E5=00] window=0x1B8: 00 00 00 01 00 00 00 00 0B 00 00 00 00 00 00 00 ctxRegs=[rdx=0x5000:actor:id=0x1E:unit=0x1000:act=0,r8=0x5300:actor:record-unit:id=0x01:unit=0x2100:act=0] ctxStack=[+0x90=0x5000:actor:id=0x1E:unit=0x1000:act=0]
[GC-Probe] [PRECLAMP-FORMULA-CANDIDATE event=8 ptr=0x2100 id=0x01 hp=567/567 oldDebit=85 oldCredit=0 forcedDebit=223 forcedCredit=0 eventKind=damage shouldStage=1 queuedPlan=1 rule=FinalDamageFormula attacker=0x1100/id=0x1E source=pending-clear pending=batch=1/act=16/event=0/16/confidence=damage-cache/score=1100000 immediate=none now=2000 action=s61=0/t18D=255/act=0/f1EF=0/dmg1C4=85/cred1C6=0/chg1D8=2/f1E5=128/b8=0/ba=0/bb=0]
[GC-Probe] [PRECLAMP-FORMULA-RUNTIME ptr=0x2100 id=0x01] event=damage | attacker=0x1100:pending-clear | action=pending-action-16:source=pending-clear:signal=16:vars=actionid=16,batch=1 | final=223:FinalDamageFormula
"""

SELECTOR_FALLBACK_EXTRA = """\
[GC-Probe] [PRECLAMP-ACTOR-CTX event=13 now=3000 target=0x3000/id=0x80 oldDebit=388 caster=none actionId=-1 verdict=no-caster-actor actors=[0x5500->id=0x80/act=0]
[GC-Probe] [SELECTOR-PROBE event=13 evadeType=0x00(hit) actor=0x5500:actor:record-unit:id=0x80:unit=0x3000:act=0 record=0x3000 unit:id=0x80/team=0/hp=0/ct=8 now=3010 rec+1BB=01 rec+1BE=01 rec+1C0=00 rec+1C4(dmg)=388 rec+1E5=80] window=0x1B8: 01 00 01 01 11 00 01 00 00 00 00 00 84 01 00 00 ctxRegs=[rdx=0x5000:actor:id=0x1E:unit=0x1000:act=0,r8=0x5500:actor:record-unit:id=0x80:unit=0x3000:act=0,r15=0x5000:actor:id=0x1E:unit=0x1000:act=0] ctxStack=[+0xA0=0x5000:actor:id=0x1E:unit=0x1000:act=0,+0xA8=0x5500:actor:record-unit:id=0x80:unit=0x3000:act=0]
"""


def main() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        log = root / "battleprobe_log.txt"
        abilities = root / "baseline_abilities.csv"
        log.write_text(SAMPLE_LOG, encoding="utf-8")
        abilities.write_text(
            "Id,Name,JP,JpCost1,JpCost2\n"
            "0,,0,0,0\n"
            "1,Cure,50,50,0\n"
            "16,Fire,50,50,0\n",
            encoding="utf-8",
        )

        ability_map = load_abilities(abilities)
        parsed = parse_log(log)
        report = render_report(log, ability_map, parsed)

        fallback_log = root / "selector_fallback_log.txt"
        fallback_log.write_text(SAMPLE_LOG + SELECTOR_FALLBACK_EXTRA, encoding="utf-8")
        fallback_report = render_report(fallback_log, ability_map, parse_log(fallback_log))

    check("Pre-clamp actor contexts: 3 (`resolved`=3" in report, "actor context summary missing")
    check("| 0 | Basic Attack / implicit weapon | 1 |" in report, "basic attack action id should be summarized")
    check("| 16 | Fire |" in report, "Fire action id should resolve from ability CSV")
    check("`target+caster`" in report, "equipment evidence should join by actor event")
    check("Pending matches: 1 (`resolved`=1)" in report, "pending match summary missing")
    check("Max pending contention: active=1, trackedPending=0, trackedResolving=0." in report, "pending contention summary missing")
    check("| Line | Event | Kind | Target | Caster | Action | Confidence | Score | Active | Pending | Resolving |" in report, "pending contention columns missing")
    check("Pending target caches: 1 (`pre-apply damage candidates`=1)" in report, "pending target cache summary missing")
    check("Pre-apply damage target-cache candidate(s): 1" in report, "pre-apply cache readiness signal missing")
    check("Pre-apply target-cache source hint(s): 1 across 1 cache(s)." in report, "target-cache source hint readiness signal missing")
    check("`reenter` | `0x2200/0x80` | 422 | 0 | 130 | `0x80` | 1 | pre-apply damage |" in report, "pending target cache row missing")
    check("Target Cache Source Hints" in report, "target-cache source hint section missing")
    check("`0x2200/0x80` | 422 | `0x3000/id=0x32` | `immediate-action` | `0` Basic Attack / implicit weapon |" in report, "target-cache source hint row missing")
    check("`resolved-self`" in report, "self-hit actor context should be preserved")
    check("Immediate candidate snapshots: 1 (`selected`=1)" in report, "immediate candidate summary missing")
    check("Selector probes: 2." in report, "selector probe summary missing")
    check("Hook-reg events: 1 (`targetcache`=1)." in report, "hook-reg event summary missing")
    check("Landmark hits: 1." in report, "landmark summary missing")
    check("Target-cache hook events with source-candidate refs: 1/1." in report, "target-cache verdict missing")
    check("Strong candidate proof" in report, "target-cache source verdict should mark source proof")
    check("Source-candidate action ids: `0` Basic Attack / implicit weapon x1." in report, "target-cache action id summary missing")
    check("Named incoming action proof: not present in this capture" in report, "target-cache named-action verdict missing")
    check("Register Unit/Actor Refs" in report, "register unit/actor refs section missing")
    check("`rcx->0x32/unit`" in report and "`rdx->0x32/act=0`" in report and "`+0x20->0x80/act=0`" in report, "target-cache register actor refs missing")
    check("`target_cache_write_1c4`" in report and "`0x2D7AC0`" in report, "landmark actor refs missing")
    check("| `0x00` | hit | 1 |" in report, "selector outcome should be summarized")
    check("| `0x0B` | blade-grasp | 1 |" in report, "Blade Grasp selector outcome should be summarized")
    check("| `16` | 2 |" in report, "selector actor action id should be summarized")
    check("| 4 | `0x01` | `0x00` hit | `0x01` | `0x00` | 85 | `0x80` |" in report, "selector event row missing")
    check("`actor->0x1E/act=16`" in report and "`+0x20->0x01/act=0/self`" in report, "selector actor refs missing")
    check("Selector no-HP outcomes with non-target source actor refs: 1/1." in report, "no-HP selector source coverage missing")
    check("No-HP selector context:" in report, "no-HP selector section missing")
    check("| 5 | `0x0B` blade-grasp | `0x01` |" in report, "Blade Grasp no-HP selector row missing")
    check("`rdx->0x1E/act=0`" in report and "`actor->0x01/act=0/self`" in report, "no-HP source/target refs missing")
    check("No hard action-identity gaps detected" in report, "clean sample should not report hard gaps")
    check("pending-action-16" in report and "`16` Fire" in report, "runtime action signal should cross-reference Fire")
    check("Selector fallback source hint(s): 1 unresolved positive-debit actor context(s)" in fallback_report, "selector fallback readiness signal missing")
    check("Selector Fallback Hints" in fallback_report, "selector fallback section missing")
    check("| 17 | 18 | 1 | `0x80` | 388 | `0x00` hit |" in fallback_report, "selector fallback row missing")
    check("`rdx->0x1E/act=0`" in fallback_report and "`r8->0x80/act=0/self`" in fallback_report, "selector fallback refs missing")

    print("action identity log analyzer smoke test passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
