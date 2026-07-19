#!/usr/bin/env python3
"""Map the native movement-to-Reaction interruption and resume surfaces for DCL Approach."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

import analyze_dcl_calc_provenance as provenance
import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE

BATTLE_STATE_RVA = 0xC6B1CC
MOVEMENT_ACTOR_INDEX_RVA = 0xC6AD8C
EXECUTION_ACTOR_INDEX_RVA = 0xCF873C
ACTOR_LIST_HEAD_RVA = 0xD3A410
REACTION_SOURCE_INDEX_RVA = 0x186AFF4
REACTION_QUEUE_PASS_RVA = 0x2FCE87C
MAP_DIMS_RVA = 0xC6AD6A
TILE_TABLE_RVA = 0xD8DCB0

APPROACH_NATIVE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclApproachNative.cs"
APPROACH_COORDINATOR = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclApproach.cs"
APPROACH_RUNTIME = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "Mod.cs"

MOVEMENT_UPDATE_RVA = 0x1FE59C
MOVEMENT_SYNC_BOUNDARY_RVA = 0x1FE793
MOVEMENT_COMPLETE_RVA = 0x203ED4
REACTION_QUEUE_RVA = 0x206344
REACTION_POST_CHAIN_RVA = 0x206050
REACTION_RESUME_OWNER_RVA = 0x205F28


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("dispatcher-state-load", 0x211B23, "8B 0D A3 96 A5 00", "loads the shared battle-state dword"),
    Anchor("state-11-movement-update", 0x211DF4, "E8 A3 F3 FE FF E8 9E E9 04 00 48 8B C8 48 8B D8 E8 93 C7 FE FF", "state 0x11 resolves the movement actor and calls the native updater"),
    Anchor("state-11-post-updater-completion-gate", 0x211E09, "44 38 AB 8B 00 00 00 75 2E 0F B6 8B A8 00 00 00 39 8B A4 00 00 00 72 1F", "after the updater returns, an idle actor at cursor>=length can complete movement"),
    Anchor("movement-complete-call", 0x211E2A, "0F B6 4B 08 E8 65 EC 04 00 48 8B C8 E8 19 91 FE FF E8 94 20 FF FF", "a consumed route completes through 0x203ED4"),
    Anchor("movement-complete-to-12", 0x203EF3, "C7 05 CF 72 A6 00 12 00 00 00", "ordinary movement completion advances to state 0x12"),
    Anchor("state-1e-queue-call", 0x2121F0, "44 89 2D 85 C6 DB 02 E8 48 41 FF FF", "state 0x1E resets the queue pass and calls the queue"),
    Anchor("queue-pass-load", 0x206376, "8B 0D 00 85 DC 02", "the queue reads its 0..2 pass counter"),
    Anchor("queue-accepted-to-29", 0x2063C0, "C7 05 02 4E A6 00 29 00 00 00", "an accepted pass-2 Reaction advances to state 0x29"),
    Anchor("queue-true-constant", 0x20636D, "33 F6 8B D6 E8 4A A4 05 00 8B 0D 00 85 DC 02 8D 5E 1F 48 8B F8 44 8D 76 01", "the queue establishes r14d=1 as its accepted return value"),
    Anchor("queue-accepted-return", 0x20646A, "41 8B C6 48 8B 4D F8", "an accepted queue path returns eax=1"),
    Anchor("queue-empty-return", 0x2066F7, "33 C0 E9 6F FD FF FF", "an exhausted queue returns eax=0 through the shared epilogue"),
    Anchor("trace-chain-queue", 0xD90CF99, "E8 A6 93 8F F2 85 C0 75 05 E8 A9 90 8F F2", "post-Reaction cleanup chains the queue, then falls through when empty"),
    Anchor("post-chain-execution-actor", 0xD7D332A, "E8 91 D4 A8 F2", "post-chain cleanup resolves the execution actor"),
    Anchor("post-chain-resume-owner", 0xD7D33B5, "E8 6E 2B A3 F2", "post-chain cleanup calls the state-resume owner"),
    Anchor("native-reaction-resume-to-28", 0xD7D0A7A, "C6 05 2A 4D 4A F3 00 C7 05 41 A7 49 F3 28 00 00 00", "the native Reaction continuation clears a flag and resumes at state 0x28"),
    Anchor("movement-sync-boundary", MOVEMENT_SYNC_BOUNDARY_RVA, "0F B6 83 A8 00 00 00", "synchronous idle boundary before another route byte is consumed"),
    Anchor("movement-updater-epilogue", 0x1FE940, "48 8B 5C 24 30 48 83 C4 20 5F C3", "safe updater return before another route byte is consumed"),
    Anchor("movement-resolver-index", 0x2607A5, "0F B6 41 08 3B 05 DD A5 A0 00", "movement resolver compares actor+8 with the movement index global"),
    Anchor("execution-resolver-index", 0x2607C9, "0F B6 41 08 3B 05 69 7F A9 00", "execution resolver compares actor+8 with a separate execution index global"),
    Anchor("typed-source-target-mark-gate", 0x2832FE, "F6 84 CD B5 DC D8 00 40", "typed source-target delivery requires tile dynamic-mark bit 0x40"),
    Anchor("typed-source-target-index-write", 0x28331B, "8A 05 D3 7C 5E 01 88 47 0B", "an accepted typed order copies the source index global into order+0x0B"),
)


SOURCE_ANCHORS = (
    (APPROACH_RUNTIME, "DCL_APPROACH_MAP_WIDTH_GLOBAL_RVA = 0xC6AD6A", "binds the native map dimensions"),
    (APPROACH_RUNTIME, "DCL_APPROACH_TILE_TABLE_RVA = 0xD8DCB0", "binds the battle tile table"),
    (APPROACH_NATIVE, "mov byte [rdi+4Fh], cl", "lends the actor's entered X to the source unit"),
    (APPROACH_NATIVE, "SourceUnitTileRestored", "records byte-exact source-coordinate restoration"),
    (APPROACH_NATIVE, "or byte [r10+5], 40h", "lends the source tile's typed-target mark"),
    (APPROACH_NATIVE, "and byte [r10+5], 0BFh", "clears only a borrowed target-mark bit"),
    (APPROACH_NATIVE, "mov qword [rsp+20h], r10", "preserves the exact tile pointer above Win64 shadow space"),
    (APPROACH_NATIVE, "SourceTargetMarkRestored", "records restoration evidence in the native control block"),
    (APPROACH_COORDINATOR, "BlocksSyntheticReservations", "publishes exclusive shared-queue ownership while Approach is active"),
    (APPROACH_RUNTIME, "DclReactionReservationArbitrationEnabled", "requires explicit shared-reservation arbitration"),
    (APPROACH_RUNTIME, "ruleReason: \"approach-reservation\"", "rejects new synthetic reservations while Approach owns pass 2"),
)


def _hex(raw: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in raw)


def rip_target(md: Cs, pe: pefile.PE, raw: bytes, rva: int) -> int | None:
    instruction = next(iter(md.disasm(
        dispatch.rva_bytes(pe, raw, rva, 16), pe.OPTIONAL_HEADER.ImageBase + rva)), None)
    if instruction is None:
        return None
    for operand in instruction.operands:
        if operand.type == X86_OP_MEM and operand.mem.base == X86_REG_RIP:
            return instruction.address + instruction.size + operand.mem.disp - pe.OPTIONAL_HEADER.ImageBase
    return None


def rel32_target(pe: pefile.PE, raw: bytes, rva: int, opcode: int = 0xE8) -> int | None:
    encoded = dispatch.rva_bytes(pe, raw, rva, 5)
    if len(encoded) != 5 or encoded[0] != opcode:
        return None
    return rva + 5 + int.from_bytes(encoded[1:], "little", signed=True)


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    anchor_rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        passed = actual == expected
        anchors_ok &= passed
        anchor_rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | "
            f"{'PASS' if passed else f'FAIL (`{_hex(actual)}`)'} | {anchor.meaning} |"
        )

    rip_checks = (
        ("dispatcher state load", 0x211B23, BATTLE_STATE_RVA),
        ("movement completion state write", 0x203EF3, BATTLE_STATE_RVA),
        ("state-1E queue-pass reset", 0x2121F0, REACTION_QUEUE_PASS_RVA),
        ("queue pass load", 0x206376, REACTION_QUEUE_PASS_RVA),
        ("queue accepted state write", 0x2063C0, BATTLE_STATE_RVA),
        ("native Reaction resume state write", 0xD7D0A81, BATTLE_STATE_RVA),
        ("movement actor index", 0x2607A9, MOVEMENT_ACTOR_INDEX_RVA),
        ("execution actor index", 0x2607CD, EXECUTION_ACTOR_INDEX_RVA),
        ("movement actor-list head", 0x26079C, ACTOR_LIST_HEAD_RVA),
        ("execution actor-list head", 0x2607C0, ACTOR_LIST_HEAD_RVA),
    )
    rip_rows: list[str] = []
    rip_ok = True
    for name, rva, expected in rip_checks:
        actual = rip_target(md, pe, raw, rva)
        passed = actual == expected
        rip_ok &= passed
        actual_text = "none" if actual is None else f"`0x{actual:X}`"
        rip_rows.append(
            f"| {name} | `0x{rva:X}` | {actual_text} | `0x{expected:X}` | "
            f"{'PASS' if passed else 'FAIL'} |"
        )

    call_checks = (
        ("state-11 updater", 0x211E04, MOVEMENT_UPDATE_RVA),
        ("movement completion", 0x211E3B, MOVEMENT_COMPLETE_RVA),
        ("state-1E queue", 0x2121F7, REACTION_QUEUE_RVA),
        ("trace Reaction chaining", 0xD90CF99, REACTION_QUEUE_RVA),
        ("trace empty-chain cleanup", 0xD90CFA2, REACTION_POST_CHAIN_RVA),
        ("post-chain resume owner", 0xD7D33B5, REACTION_RESUME_OWNER_RVA),
    )
    call_rows: list[str] = []
    calls_ok = True
    for name, rva, expected in call_checks:
        actual = rel32_target(pe, raw, rva)
        passed = actual == expected
        calls_ok &= passed
        actual_text = "none" if actual is None else f"`0x{actual:X}`"
        call_rows.append(
            f"| {name} | `0x{rva:X}` | {actual_text} | `0x{expected:X}` | "
            f"{'PASS' if passed else 'FAIL'} |"
        )

    queue_callers = provenance.all_direct_callers(pe, raw, REACTION_QUEUE_RVA)
    movement_callers = provenance.all_direct_callers(pe, raw, MOVEMENT_UPDATE_RVA)
    caller_graph_ok = (
        queue_callers == [0x2121F7, 0xD90CF99]
        and movement_callers == [0x20DCC0, 0x211E04, 0xD95C505]
    )

    source_cache = {path: path.read_text(encoding="utf-8") for path, _, _ in SOURCE_ANCHORS}
    source_rows: list[str] = []
    source_ok = True
    for path, token, meaning in SOURCE_ANCHORS:
        passed = token in source_cache[path]
        source_ok &= passed
        source_rows.append(
            f"| `{path.relative_to(ROOT)}` | {'PASS' if passed else 'FAIL'} | {meaning} |"
        )

    overall = anchors_ok and rip_ok and calls_ok and caller_graph_ok and source_ok
    lines = [
        "# DCL Approach interruption and resume analysis",
        "",
        "Generated by `tools/analyze_dcl_approach_interrupt.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Meaning |",
        "| --- | ---: | --- | --- |",
        *anchor_rows,
        "",
        "## Shared globals",
        "",
        "| Surface | Instruction RVA | Actual target | Expected target | Result |",
        "| --- | ---: | ---: | ---: | --- |",
        *rip_rows,
        "",
        f"- Reaction source/exclusion index available to a synthetic Approach transaction: `0x{REACTION_SOURCE_INDEX_RVA:X}`.",
        f"- Map dimensions: `0x{MAP_DIMS_RVA:X}`; battle tile table: `0x{TILE_TABLE_RVA:X}`.",
        "- `0x26079C` and `0x2607C0` walk the same actor list but select different actor indices.",
        "  Movement therefore retains a turn/movement actor independently from the execution actor",
        "  used by the Reaction queue.",
        "",
        "## Direct call edges",
        "",
        "| Edge | Caller RVA | Actual target | Expected target | Result |",
        "| --- | ---: | ---: | ---: | --- |",
        *call_rows,
        "",
        f"- Complete executable-section queue callers: {', '.join(f'`0x{rva:X}`' for rva in queue_callers) or 'none'}.",
        f"- Complete executable-section movement-updater callers: {', '.join(f'`0x{rva:X}`' for rva in movement_callers) or 'none'}.",
        f"- Expected caller graph: **{'PASS' if caller_graph_ok else 'FAIL'}**.",
        "",
        "## Runtime bridge source anchors",
        "",
        "| Source | Result | Meaning |",
        "| --- | --- | --- |",
        *source_rows,
        "",
        "## State path",
        "",
        "```text",
        "ordinary movement",
        "  state 0x11 -> movement updater 0x1FE59C",
        "  route still active -> synchronous boundary 0x1FE793 -> next step",
        "  route complete -> 0x203ED4 -> state 0x12",
        "",
        "native Reaction",
        "  state 0x1E -> reset queue pass -> queue 0x206344",
        "  accepted -> state 0x29 -> delivery states -> post-Reaction cleanup",
        "  cleanup -> queue 0x206344 again for chaining",
        "  empty chain -> 0x206050 -> 0x205F28 -> state 0x28",
        "```",
        "",
        "There is no native `0x11 -> 0x1E` edge in this path. Arming `unit+0x1CE` or the managed",
        "synthetic mailbox during movement does not itself execute a Reaction. Conversely, invoking",
        "the queue without an owned continuation would finish at state `0x28`, not resume the route.",
        "The queue has an explicit boolean contract: accepted work returns `eax=1`, while an exhausted",
        "three-pass scan returns `eax=0`. A boundary shim can therefore return through updater epilogue",
        "`0x1FE940` only when the direct queue call accepts work; zero can release the original route",
        "instruction stream without guessing from later state.",
        "A final-step interruption also owns the post-updater completion gate at `0x211E09`. Returning",
        "through updater epilogue `0x1FE940` does not by itself pause a terminal route: the caller can",
        "immediately run its cursor>=length tail and replace state `0x11` with `0x12`. The exact mover",
        "must therefore skip to `0x211E40` while its decision is pending, while the command is being",
        "handed to the game thread, and after the direct queue accepts work. Release/rejection/resume",
        "states must fall through so ordinary final completion still occurs.",
        "Counter's typed helper is not abstract target construction: it reads source battle-unit",
        "coordinates, requires dynamic tile mark bit `0x40` on that tile, then copies those same",
        "coordinates into the order. The per-step actor reaches the entered tile before the deferred",
        "battle-unit tuple does. The bridge must lend the entered tuple plus exactly this bit through",
        "synchronous order/actor construction, then restore both byte-exactly before returning.",
        "",
        "## DCL architecture consequence",
        "",
        "- **Proven static:** the movement actor and Reaction execution actor use separate selector",
        "  globals over the same actor list. A queued Reaction can therefore replace the execution",
        "  actor without inherently erasing the mover selected by `0x26079C`.",
        "- **Proven live elsewhere:** `0x1FE793` fires synchronously after each completed step and at",
        "  route termination. It is the earliest owned point where the mover is already on the entered",
        "  tile but the next route byte has not yet been consumed.",
        "- **Strong implementation shape:** an Approach transaction must pause route consumption at",
        "  `0x1FE793`, snapshot the mover/source and entered tile synchronously, arm one-shot eligible",
        "  reactors, reset the queue pass, and invoke `0x206344`. If delivery commits, the transaction",
        "  must replace only its owned terminal `state 0x28` continuation with `state 0x11`; all native",
        "  and non-Approach Reaction transactions retain the original continuation.",
        "- **Typed-target bridge:** validate the deferred unit tuple and entered actor tuple independently",
        "  against map bounds, temporarily expose the entered tuple through unit `+0x4F/+0x50/+0x51`,",
        "  set only entered tile `+5 & 0x40`, retain the exact tile pointer across the queue call, and",
        "  restore the mark and unit tuple before interpreting the queue result.",
        "- **Final-step guard:** the same exact-actor transaction must bypass state-`0x11`'s",
        "  post-updater completion check during pending decision, command handoff, and accepted-queue",
        "  ownership. Release, rejection, and resumed release restore ordinary final completion.",
        "- **Fail-closed requirement:** no queue invocation or continuation rewrite is allowed unless",
        "  the mover identity, source index, route epoch/step, both tile tuples, map bounds, eligible",
        "  reactor set, empty native carrier slots, and bounded write budget all agree. Any disagreement releases the paused",
        "  movement unchanged.",
        "- **Pending live proof:** static evidence cannot prove that direct queue invocation from the",
        "  paused movement frame preserves every VM-owned continuation field. The first control probe",
        "  must therefore use one bounded synthetic delivery, verify `0x11 -> 0x29 -> ... -> 0x11`,",
        "  prove the same mover resumes at the same cursor, and prove native Reactions still end at",
        "  `0x28`.",
        "",
    ]
    return "\n".join(lines), overall


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="validate the current executable without creating a work/ report",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = (
        Path("(check-only)")
        if args.check_only
        else args.output or ROOT / "work" / f"{int(time.time())}-dcl-approach-interrupt-analysis.md"
    )
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("Approach interruption static mapping PASS" if ok else "Approach interruption static mapping FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
