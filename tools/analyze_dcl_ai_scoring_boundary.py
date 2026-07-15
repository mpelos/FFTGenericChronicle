#!/usr/bin/env python3
"""Verify the native boundaries available for making DCL results visible to AI scoring."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_calc_provenance as provenance
import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
SWEEP_RVA = 0x281CE8
CALC_RVA = 0x3099AC
DEFAULT_BASELINE_LOG = ROOT / "work" / "1784087707-lt35-ai-score-visible-dual-target-baseline-live.log"
DEFAULT_FORCED_LOG = ROOT / "work" / "1784088085-lt35-ai-score-visible-dual-target-forced-live.log"
DEFAULT_LT36_RANKING_LOG = ROOT / "work" / "1784089341-lt36-compute-point-ai-ranking-live.log"
DEFAULT_LT36_DELIVERY_LOG = ROOT / "work" / "1784089584-lt36b-compute-point-delivery-live.log"


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("target-sweep-entry", SWEEP_RVA, "48 89 5C 24 18 55 56 57", "VM-owned affected-target/result sweep"),
    Anchor("per-target-index", 0x281EFA, "8A 54 1D D8", "load one selected target index"),
    Anchor("per-target-calc", 0x281F0D, "E8 9A 7A 08 00", "calculate one action/target result"),
    Anchor("post-calc-bundle", 0x281F12, "48 FF C3 48 83 FB 15", "known staged-bundle write window"),
    Anchor("result-list-copy", 0x281F67, "48 8D 57 02", "build sweep output after every target calculation"),
    Anchor("sweep-output-count", 0x281F94, "44 88 47 01", "publish affected-target count in output"),
    Anchor("formula-dispatch", 0x309F4B, "40 0F B6 C7 41 FF 94 C4 C8 2B 68 00", "dispatch native formula"),
    Anchor("protected-finalizer", 0x309F72, "8A CB E8 9F 01 00 00", "finalize result through protected target-index path"),
    Anchor("result-pointer-read", 0x309F79, "48 8B 05 F0 0F 56 01", "read finalized current-result pointer"),
    Anchor("execution-pre-clamp", 0x30A5D7, "0F BF 45 06 0F B7 57 30", "later apply-only DCL amount rewrite"),
)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def has(text: str, pattern: str) -> bool:
    return re.search(pattern, text, flags=re.MULTILINE) is not None


def validate_lt35(baseline_log: Path, forced_log: Path) -> tuple[dict[str, bool], str, str]:
    baseline = baseline_log.read_text(encoding="utf-8", errors="replace")
    forced = forced_log.read_text(encoding="utf-8", errors="replace")
    checks = {
        "baseline enemy 3 evaluates Ramza target 16": has(baseline, r"origin=outer-sweep .*battleState=0x5 .*turnOwner=3 sourceIdx=3 .*targetIdx=16"),
        "baseline enemy 3 evaluates Rion target 17": has(baseline, r"origin=outer-sweep .*battleState=0x5 .*turnOwner=3 sourceIdx=3 .*targetIdx=17"),
        "baseline Ramza bundle is natural 122 damage": has(baseline, r"\[BUNDLE\].*targetIdx=16 charId=0x01 .*stagedDmg=122 .*resFlag=0x80"),
        "baseline Rion bundle is natural 79 damage": has(baseline, r"\[BUNDLE\].*targetIdx=17 charId=0x80 .*stagedDmg=79 .*resFlag=0x80"),
        "baseline forecast selects Rion": has(baseline, r"origin=forecast-trace .*turnOwner=3 sourceIdx=3 .*targetIdx=17"),
        "baseline execution selects Rion": has(baseline, r"origin=outer-sweep .*battleState=0x2A .*turnOwner=3 sourceIdx=3 .*targetIdx=17"),
        "forced hook declares only Ramza lethal bundle": has(forced, r"\[BUNDLE-HOOK\].*forceChar=1 kind=0 .*dmg=4095 resFlag=128"),
        "forced enemy 3 still evaluates Ramza target 16": has(forced, r"origin=outer-sweep .*battleState=0x5 .*turnOwner=3 sourceIdx=3 .*targetIdx=16"),
        "forced enemy 3 still evaluates Rion target 17": has(forced, r"origin=outer-sweep .*battleState=0x5 .*turnOwner=3 sourceIdx=3 .*targetIdx=17"),
        "forced Ramza bundle is 4095 damage": has(forced, r"\[BUNDLE\].*targetIdx=16 charId=0x01 .*stagedDmg=4095 .*resFlag=0x80"),
        "forced Rion bundle remains 79 damage": has(forced, r"\[BUNDLE\].*targetIdx=17 charId=0x80 .*stagedDmg=79 .*resFlag=0x80"),
        "forced forecast switches to Ramza": has(forced, r"origin=forecast-trace .*turnOwner=3 sourceIdx=3 .*targetIdx=16"),
        "forced execution switches to Ramza": has(forced, r"origin=outer-sweep .*battleState=0x2A .*turnOwner=3 sourceIdx=3 .*targetIdx=16"),
        "forced native delivery reaches Ramza": has(forced, r"\[DAMAGE .*id=0x01\] 569 -> 0 = 569"),
    }
    return checks, baseline, forced


def validate_lt36(ranking_log: Path, delivery_log: Path) -> dict[str, bool]:
    ranking = ranking_log.read_text(encoding="utf-8", errors="replace")
    delivery = delivery_log.read_text(encoding="utf-8", errors="replace")
    return {
        "ranking writer is installed at the post-calc boundary": has(ranking, r"\[POST-CALC-HOOK\].*bundleProbe=1 numericWriter=1"),
        "ranking pass publishes Ramza 4095 and Rion 79": has(ranking, r"\[DCL-COMPUTE-POINT\].*battleState=0x5 .*target=0x01 .*hp=122/0->4095/0 .*cached=0") and has(ranking, r"\[DCL-COMPUTE-POINT\].*battleState=0x5 .*target=0x80 .*hp=79/0->79/0 .*cached=0"),
        "ranking pass selects Ramza for confirmed execution": has(ranking, r"origin=outer-sweep .*battleState=0x2A .*turnOwner=3 sourceIdx=3 .*targetIdx=16"),
        "delivery pass publishes Ramza 122 and Rion 4095": has(delivery, r"\[DCL-COMPUTE-POINT\].*battleState=0x5 .*target=0x01 .*hp=122/0->122/0 .*cached=0") and has(delivery, r"\[DCL-COMPUTE-POINT\].*battleState=0x5 .*target=0x80 .*hp=79/0->4095/0 .*cached=0"),
        "delivery pass selects Rion for forecast and execution": has(delivery, r"origin=forecast-trace .*turnOwner=3 sourceIdx=3 .*targetIdx=17") and has(delivery, r"origin=outer-sweep .*battleState=0x2A .*turnOwner=3 sourceIdx=3 .*targetIdx=17"),
        "confirmed Rion result is cached at compute point": has(delivery, r"\[DCL-COMPUTE-POINT\].*battleState=0x2A .*target=0x80 .*hp=79/0->4095/0 .*cached=1"),
        "pre-clamp consumes the exact cached Rion result": has(delivery, r"\[DCL\].*target=0x80 .*debit=4095 oldDebit=79 .*computePoint=1"),
        "cached result lands through native HP apply": has(delivery, r"\[DAMAGE .*id=0x80\] 60 -> 0 = 60"),
        "delivered transaction has no compute-point cache miss": "no-compute-point-result" not in delivery,
    }


def render(
    exe: Path,
    output: Path,
    baseline_log: Path,
    forced_log: Path,
    lt36_ranking_log: Path,
    lt36_delivery_log: Path,
) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = parse_hex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        ok = actual == expected
        anchors_ok &= ok
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | "
            f"{'PASS' if ok else f'FAIL (`{hex_bytes(actual)}`)'} | `{anchor.expected}` | {anchor.meaning} |"
        )

    sweep_callers = dispatch.direct_callers(pe, raw, SWEEP_RVA)
    sweep_raw_rel32_matches = provenance.all_direct_callers(pe, raw, SWEEP_RVA)
    calc_callers = provenance.all_direct_callers(pe, raw, CALC_RVA)
    callers_ok = sweep_callers == [] and calc_callers == [0x281F0D, 0x307ED0, 0xEF53F0F]

    mod_source = (ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "Mod.cs").read_text(encoding="utf-8")
    source_checks = {
        "the existing staged-bundle probe owns RVA `0x281F12`": "StagedBundleProbeRva { get; set; } = 0x281F12" in mod_source,
        "the execution damage rewrite owns RVA `0x30A5D7`": "PreClampDamageRewriteRva { get; set; } = 0x30A5D7" in mod_source,
        "the post-calc status producer explicitly rejects non-execution state": "battleState != DclCalcProvenance.ConfirmedExecutionBattleState" in mod_source,
        "the permanent numeric writer is separately gated": "DclComputePointNumericEnabled { get; set; } = false" in mod_source,
        "confirmed execution results are cached at compute point": "_dclComputePointCache.Record(" in mod_source,
        "pre-clamp refuses to evaluate an ambiguous compute-point miss twice": 'QueueDclCacheMiss(settings, "no-compute-point-result"' in mod_source,
    }
    source_ok = all(source_checks.values())
    live_checks, _, _ = validate_lt35(baseline_log, forced_log)
    live_ok = all(live_checks.values())
    lt36_checks = validate_lt36(lt36_ranking_log, lt36_delivery_log)
    lt36_ok = all(lt36_checks.values())

    lines = [
        "# DCL AI-scoring boundary analysis",
        "",
        "Generated by `tools/analyze_dcl_ai_scoring_boundary.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Native byte anchors",
        "",
        "| Name | RVA | Result | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
        *rows,
        "",
        "## Call ownership",
        "",
        f"- Sweep direct callers across executable sections: {', '.join(f'`0x{x:X}`' for x in sweep_callers) or 'none'}.",
        f"- Unaligned raw rel32 byte matches for the sweep: {', '.join(f'`0x{x:X}`' for x in sweep_raw_rel32_matches) or 'none'}; these are not decoded call instructions.",
        f"- Calc direct callers across executable sections: {', '.join(f'`0x{x:X}`' for x in calc_callers) or 'none'}.",
        f"- Expected VM-owned sweep and three calc origins: **{'PASS' if callers_ok else 'FAIL'}**.",
        "",
        "## Runtime-source checks",
        "",
        *[f"- **{'PASS' if ok else 'FAIL'}:** {name}." for name, ok in source_checks.items()],
        "",
        "## LT35 controlled live comparison",
        "",
        f"- Baseline: `{baseline_log}` (`SHA-256 {hashlib.sha256(baseline_log.read_bytes()).hexdigest().upper()}`).",
        f"- Forced: `{forced_log}` (`SHA-256 {hashlib.sha256(forced_log.read_bytes()).hexdigest().upper()}`).",
        *[f"- **{'PASS' if ok else 'FAIL'}:** {name}." for name, ok in live_checks.items()],
        "",
        "## LT36 permanent-writer live comparison",
        "",
        f"- Ranking/native-outcome pass: `{lt36_ranking_log}` (`SHA-256 {hashlib.sha256(lt36_ranking_log.read_bytes()).hexdigest().upper()}`).",
        f"- Cached-delivery pass: `{lt36_delivery_log}` (`SHA-256 {hashlib.sha256(lt36_delivery_log.read_bytes()).hexdigest().upper()}`).",
        *[f"- **{'PASS' if ok else 'FAIL'}:** {name}." for name, ok in lt36_checks.items()],
        "",
        "## Native flow",
        "",
        *dispatch.disassembly(md, pe, raw, 0x281EFA, 0xAA),
        "",
        *dispatch.disassembly(md, pe, raw, 0x309F4B, 0x45),
        "",
        "## Static interpretation",
        "",
        "- **Proven:** every selected target is calculated before the sweep builds its output record.",
        "  The existing `0x281F12` boundary can rewrite the target's finalized staged bundle and is",
        "  early enough for native execution; live LT4 already proves that a forced HP debit there is",
        "  the value ultimately applied.",
        "- **Proven:** the current DCL amount rewrite at `0x30A5D7` is downstream in the execution apply",
        "  path. It cannot by itself change a score already consumed during an AI candidate sweep.",
        "- **Strong:** the narrow formula-side alternative is between dispatch `0x309F4F` and protected",
        "  finalization `0x30A118`. It would let native finalization see authored numeric inputs, but the",
        "  global formula scratch block is family-dependent and is not a safe universal amount ABI.",
        "- **Implementation boundary:** `0x281F12` is the safe universal candidate because it exposes",
        "  the normalized staged HP/MP/status bundle. A final implementation must be idempotent with",
        "  the later pre-clamp path, so execution does not evaluate formulas twice from rewritten inputs.",
        "- **Proven live:** with the same enemy, action, legal candidates, and restored autosave, changing",
        "  only Ramza's post-calc staged debit from 122 to 4095 switches both forecast and execution",
        "  from Rion to Ramza. Protected enemy utility therefore consumes the normalized bundle after",
        "  `0x281F12` for target ranking.",
        "- **Implementation consequence:** the permanent writer belongs at `0x281F12`. Confirmed",
        "  execution results are cached and reused at `0x30A5D7`; AI evaluations are transient, so no",
        "  formula runs twice against already-rewritten staged inputs.",
        "- **Proven live:** LT36 exercises that permanent path. A formula-owned Rion debit changed",
        "  protected target ranking, was cached only at confirmed execution, and the exact `4095`",
        "  result reached pre-clamp with `computePoint=1` before native HP/KO delivery.",
        "",
    ]
    return "\n".join(lines), anchors_ok and callers_ok and source_ok and live_ok and lt36_ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--baseline-log", type=Path, default=DEFAULT_BASELINE_LOG)
    parser.add_argument("--forced-log", type=Path, default=DEFAULT_FORCED_LOG)
    parser.add_argument("--lt36-ranking-log", type=Path, default=DEFAULT_LT36_RANKING_LOG)
    parser.add_argument("--lt36-delivery-log", type=Path, default=DEFAULT_LT36_DELIVERY_LOG)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-ai-scoring-boundary-analysis.md"
    report, ok = render(
        args.exe,
        output,
        args.baseline_log,
        args.forced_log,
        args.lt36_ranking_log,
        args.lt36_delivery_log,
    )
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all AI-scoring boundary checks PASS" if ok else "one or more AI-scoring boundary checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
