#!/usr/bin/env python3
"""Smoke test for the runtime-settings profile audit."""
from __future__ import annotations

import report_runtime_profiles as profiles


def main() -> int:
    generated = profiles.build_report()
    rows, errors = profiles.audit()
    check(not errors, "runtime profile audit has errors:\n" + "\n".join(errors))
    check(profiles.OUT.exists(), f"runtime profile audit report missing: {profiles.OUT}")
    actual = profiles.OUT.read_text(encoding="utf-8")
    check(actual == generated, "runtime profile audit report is stale; run python tools/report_runtime_profiles.py")

    names = {spec.name for spec, _settings, _errors in rows}
    for name in [
        "scan-live-noop",
        "scan-policy",
        "dry-run-evaluation",
        "neuter-spotcheck",
        "actor-probe",
        "hook-register-probe",
        "engine-death-test",
        "custom-formula-demo",
        "death-test-hp-only",
        "death-test-killflag",
        "gurps-dr-example",
        "sentinel-coarse-v1",
        "memtable-probe-disabled",
    ]:
        check(name in names, f"missing audited profile: {name}")

    engine_death = get(rows, "engine-death-test")
    check(engine_death.get("FinalDamageFormula") == "9999", "engine death test must force lethal custom damage")
    check(engine_death.get("MinHpFloor") == 1, "engine death test must leave custom lethal writes at 1 HP")
    check(not profiles.truthy(engine_death, "CauseDeathOnZeroHp"), "engine death test must not use KO-flag writes")

    custom_demo = get(rows, "custom-formula-demo")
    check(profiles.truthy(custom_demo, "ResolveAttackerByCt"), "custom formula demo must resolve attacker by CT")
    check(custom_demo.get("MinHpFloor") == 1, "custom formula demo must keep engine-owned death floor")
    check("a.pa" in custom_demo.get("FinalDamageFormula", ""), "custom formula demo must read attacker PA")
    check("t.faith" in custom_demo.get("FinalDamageFormula", ""), "custom formula demo must read target Faith")

    actor_probe = get(rows, "actor-probe")
    check(profiles.truthy(actor_probe, "ActorProbeOnEvent"), "actor probe must log all registered units on HP events")
    check(not profiles.truthy(actor_probe, "RewriteObservedDamage"), "actor probe must not rewrite HP damage")
    check(actor_probe.get("ActorProbeStart", 999) <= 0x40, "actor probe window must include Speed +0x40")
    check(actor_probe.get("ActorProbeEnd", -1) >= 0x41, "actor probe window must include CT +0x41")

    hook_register_probe = get(rows, "hook-register-probe")
    check(profiles.truthy(hook_register_probe, "HookRegisterProbe"), "hook register probe must enable register capture")
    check(not profiles.truthy(hook_register_probe, "RewriteObservedDamage"), "hook register probe must not rewrite HP damage")
    check(hook_register_probe.get("HookRegisterProbeMaxLogs", 0) > 0, "hook register probe must limit log count")

    killflag = get(rows, "death-test-killflag")
    check(profiles.truthy(killflag, "CauseDeathOnZeroHp"), "legacy killflag probe should preserve its KO-flag write")
    check(profiles.ko_flag_ok(killflag), "legacy killflag probe should document the mapped +0x61 KO bit")
    check(killflag.get("MinHpFloor", 0) == 0, "legacy killflag probe must preserve the refuted HP=0+KO-flag setup")

    hp_only = get(rows, "death-test-hp-only")
    check(not profiles.truthy(hp_only, "CauseDeathOnZeroHp"), "legacy HP-only probe must not write KO flag")
    check(profiles.ko_flag_ok(hp_only), "legacy HP-only probe should still document the known KO flag write")
    check(hp_only.get("MinHpFloor", 0) == 0, "legacy HP-only probe must preserve the refuted HP=0 setup")

    live_noop = get(rows, "scan-live-noop")
    check(live_noop.get("FinalDamageFormula") == "vanillaDamage", "scan live-noop must preserve vanilla damage")
    check(not profiles.truthy(live_noop, "ApplyDamageResponseRules"), "scan live-noop must not apply response")

    sentinel = get(rows, "sentinel-coarse-v1")
    check(sentinel.get("MinHpFloor") == 1, "sentinel coarse live candidate must use MinHpFloor=1")
    check(profiles.truthy(sentinel, "ResolveAttackerByCt"), "sentinel coarse live candidate must keep CT attacker resolution")
    check(len(sentinel.get("ActionSignalRules", [])) >= 3, "sentinel coarse live candidate must define low/mid/high bands")

    memtable = get(rows, "memtable-probe-disabled")
    for idx, probe in enumerate(memtable.get("MemoryTableProbes", [])):
        check(not probe.get("Enabled", False), f"memtable probe {idx} must remain disabled")

    print("runtime profile audit smoke test passed")
    return 0


def get(rows, name: str):
    for spec, settings, _errors in rows:
        if spec.name == name:
            return settings
    raise AssertionError(f"missing audited profile: {name}")


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
