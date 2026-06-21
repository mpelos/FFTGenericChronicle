#!/usr/bin/env python3
"""
Summarize Generic Chronicle battleprobe_log.txt.

The current harness emits pointer-keyed unit lines plus [CANDIDATES], [DIFF], [DAMAGE],
[HEALING], [CTX], [RUNTIME], and [REWRITE] records. This script turns that noisy log into a
compact report for mapping equipment/status/action fields and checking HP-write proof evidence.

Run from the project root:
    python tools/analyze_battleprobe_log.py

Optional:
    python tools/analyze_battleprobe_log.py path\\to\\battleprobe_log.txt -o work\\battleprobe_analysis.md
"""
from __future__ import annotations

import argparse
import csv
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_LOG = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt"
)
DEFAULT_OUT = ROOT / "work" / "battleprobe_analysis.md"
DEFAULT_CATALOG = ROOT / "work" / "item_catalog.csv"
PLACEHOLDER_DAMAGE_MAX = 30

UNIT_RE = re.compile(
    r"\[UNIT ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2}) (?P<faction>ally|foe ) t(?P<team>\d+)\] (?P<stats>.+)"
)
OLD_UNIT_RE = re.compile(r"\[UNIT id=(0x[0-9A-F]{2}) (?P<faction>ally|foe ) t(?P<team>\d+)\] (?P<stats>.+)")
CAND_RE = re.compile(r"\[CANDIDATES ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
DUMP_RE = re.compile(r"\[DUMP ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
DIFF_RE = re.compile(r"\[DIFF ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
HP_EVENT_RE = re.compile(
    r"\[(?P<kind>DAMAGE|HEALING) ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] "
    r"(?P<prev>\d+) -> (?P<hp>\d+) = (?P<amount>\d+)(?: sampleAgeMs=(?P<sample_age>-?\d+))?"
)
MP_EVENT_RE = re.compile(
    r"\[(?P<kind>MPLOSS|MPGAIN) ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] "
    r"(?P<prev>\d+) -> (?P<mp>\d+) = (?P<amount>\d+)(?: sampleAgeMs=(?P<sample_age>-?\d+))?"
)
CTX_RE = re.compile(r"\[CTX ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
RUNTIME_RE = re.compile(r"\[RUNTIME ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
RUNTIME_MP_RE = re.compile(r"\[RUNTIME-MP ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
REWRITE_EVENT_RE = re.compile(
    r"\[(?P<tag>(?:MP-)?REWRITE(?:-DRY-RUN|-FAILED|-SKIP|-ECHO-SKIP)?) "
    r"ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)"
)
DEATH_EVENT_RE = re.compile(
    r"\[(?P<tag>DEATH-(?:DUMP|DIFF|FOLLOW|WRITE(?:-FAILED|-SKIP)?)) "
    r"ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})(?P<meta>[^\]]*)\] (?P<body>.*)"
)
DEATH_WRITE_BODY_RE = re.compile(
    r"(?P<name>.*?) \+0x(?P<offset>[0-9A-F]{2,4}) w(?P<width>\d+) "
    r"(?P<from>[0-9A-F]+)->(?P<to>[0-9A-F]+)"
)
HP_REWRITE_BODY_RE = re.compile(
    r"rule=(?P<rule>.*?) vanillaDamage=(?P<vanilla>-?\d+) finalDamage=(?P<final>-?\d+) "
    r"HP (?P<from>\d+)->(?P<to>\d+)"
)
MP_REWRITE_BODY_RE = re.compile(
    r"rule=(?P<rule>.*?) vanillaMpChange=(?P<vanilla>-?\d+) finalMpChange=(?P<final>-?\d+) "
    r"MP (?P<from>\d+)->(?P<to>\d+)"
)
MEMTABLE_CONFIG_RE = re.compile(r"\[MEMTABLE\] configured=(?P<configured>\d+) enabled=(?P<enabled>\d+)")
MEMTABLE_EVENT_RE = re.compile(r"\[(?P<tag>MEMTABLE(?:-[A-Z]+)*) (?P<name>[^\]]+)\] (?P<body>.*)")
MEMTABLE_FOUND_RE = re.compile(r"\[MEMTABLE-FOUND (?P<name>[^\]]+)\] (?P<body>.*)")
MEMTABLE_ROW_RE = re.compile(r"\[MEMTABLE-ROW (?P<name>[^\]]+)\] (?P<body>.*)")
MEMTABLE_KV_RE = re.compile(r"(?P<key>[A-Za-z_][A-Za-z0-9_]*)=(?P<value>\S+)")
OFFSET_RE = re.compile(r"\+0x([0-9A-F]{2,4})")
CAND_VALUE_RE = re.compile(r"\+0x(?P<offset>[0-9A-F]{2,4})(?P<width>w?)=(?P<value>\d+)")
HEX_BYTE_RE = re.compile(r"^[0-9A-F]{2}$")
SLOT_ENTRY_RE = re.compile(r"(?P<slot>[A-Za-z0-9_]+)\((?P<body>[^)]*)\)")
RESPONSE_RE = re.compile(
    r"raw(?P<raw>-?\d+)/permille(?P<permille>-?\d+)/rules(?P<rules>\d+)/clamped(?P<clamped>[01]):(?P<rule>.*)"
)
FINAL_RE = re.compile(r"(?P<value>-?\d+):(?P<rule>.*)")


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser()
    p.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    p.add_argument("-o", "--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    p.add_argument("--no-catalog", action="store_true")
    return p.parse_args()


def main() -> int:
    args = parse_args()
    if not args.log.exists():
        raise SystemExit(f"log not found: {args.log}")

    units: dict[str, dict[str, str]] = {}
    candidates: dict[str, list[str]] = defaultdict(list)
    dumps: dict[str, list[int]] = {}
    diffs: dict[str, list[str]] = defaultdict(list)
    diff_offsets: Counter[str] = Counter()
    hp_events: list[dict[str, object]] = []
    mp_events: list[dict[str, object]] = []
    contexts: list[tuple[str, str]] = []
    runtime_contexts: list[tuple[str, str]] = []
    rewrites: list[dict[str, object]] = []
    death_events: list[dict[str, object]] = []
    memory_table_events: list[dict[str, object]] = []
    headers: list[str] = []
    old_unit_lines = 0
    item_catalog = {} if args.no_catalog else load_item_catalog(args.catalog)

    for line_no, line in enumerate(args.log.read_text(encoding="utf-8", errors="replace").splitlines(), start=1):
        stripped = line.strip()
        if "==== Generic Chronicle" in line or stripped.startswith("settings"):
            headers.append(line)

        if event := parse_memory_table_line(line):
            memory_table_events.append(event)
            continue

        if m := UNIT_RE.search(line):
            ptr, cid = m.group(1), m.group(2)
            units[ptr] = {
                "id": cid,
                "faction": m.group("faction").strip(),
                "team": m.group("team"),
                "stats": m.group("stats"),
            }
            continue

        if OLD_UNIT_RE.search(line):
            old_unit_lines += 1
            continue

        if m := CAND_RE.search(line):
            candidates[m.group(1)].append(m.group("body"))
            continue

        if m := DUMP_RE.search(line):
            dumps[m.group(1)] = parse_dump_bytes(m.group("body"))
            continue

        if m := DIFF_RE.search(line):
            ptr = m.group(1)
            body = m.group("body")
            diffs[ptr].append(body)
            diff_offsets.update(OFFSET_RE.findall(body))
            continue

        if event := parse_hp_event_line(line, line_no):
            hp_events.append(event)
            continue

        if event := parse_mp_event_line(line, line_no):
            mp_events.append(event)
            continue

        if m := CTX_RE.search(line):
            contexts.append((m.group(1), m.group("body")))
            continue

        if m := RUNTIME_RE.search(line):
            runtime_contexts.append((m.group(1), m.group("body")))
            continue

        if m := RUNTIME_MP_RE.search(line):
            runtime_contexts.append((m.group(1), m.group("body")))
            continue

        if rewrite := parse_rewrite_line(line, line_no):
            rewrites.append(rewrite)
            continue

        if death := parse_death_line(line, line_no):
            death_events.append(death)

    args.output.parent.mkdir(exist_ok=True)
    args.output.write_text(
        render(args.log, headers, units, candidates, dumps, diffs, diff_offsets, hp_events, mp_events, contexts, runtime_contexts, rewrites, death_events, memory_table_events, old_unit_lines, item_catalog, args.catalog),
        encoding="utf-8",
    )
    print(f"wrote {args.output}")
    return 0


def load_item_catalog(path: Path) -> dict[int, dict[str, str]]:
    if not path.exists():
        return {}

    rows: dict[int, dict[str, str]] = {}
    with path.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            try:
                item_id = int(row.get("item_id", ""))
            except ValueError:
                continue
            rows[item_id] = row
    return rows


def candidate_item_hits(body: str, item_catalog: dict[int, dict[str, str]]) -> list[tuple[str, str, dict[str, str]]]:
    hits: list[tuple[str, str, dict[str, str]]] = []
    for m in CAND_VALUE_RE.finditer(body):
        value = int(m.group("value"))
        item = item_catalog.get(value)
        if item is None:
            continue
        width = "word" if m.group("width") else "byte"
        hits.append((f"+0x{m.group('offset')}", width, item))
    return hits


def parse_dump_bytes(body: str) -> list[int]:
    values: list[int] = []
    for token in re.split(r"\s+|\|", body):
        token = token.strip().upper()
        if not HEX_BYTE_RE.match(token):
            continue
        values.append(int(token, 16))
    return values


def dump_item_hits(raw: list[int], item_catalog: dict[int, dict[str, str]], start: int = 0x44) -> list[tuple[str, str, dict[str, str]]]:
    hits: list[tuple[str, str, dict[str, str]]] = []
    if not raw:
        return hits

    scan_start = min(max(start, 0), len(raw) - 1)
    for i in range(scan_start, len(raw)):
        value = raw[i]
        if value == 0:
            continue
        item = item_catalog.get(value)
        if item is not None:
            hits.append((f"+0x{i:X}", "byte", item))

    for i in range(scan_start, len(raw) - 1):
        value = raw[i] | (raw[i + 1] << 8)
        if value == 0:
            continue
        item = item_catalog.get(value)
        if item is not None:
            hits.append((f"+0x{i:X}", "word", item))

    return hits


def parse_runtime_contexts(runtime_contexts: list[tuple[str, str]]) -> list[dict[str, object]]:
    return [parse_runtime_context(ptr, body) for ptr, body in runtime_contexts]


def parse_memory_table_events(lines: list[str]) -> list[dict[str, object]]:
    events: list[dict[str, object]] = []
    for line in lines:
        if event := parse_memory_table_line(line):
            events.append(event)
    return events


def parse_memory_table_line(line: str) -> dict[str, object] | None:
    if m := MEMTABLE_CONFIG_RE.search(line):
        configured = int(m.group("configured"))
        enabled = int(m.group("enabled"))
        return {
            "tag": "MEMTABLE",
            "name": "",
            "body": f"configured={configured} enabled={enabled}",
            "fields": {"configured": str(configured), "enabled": str(enabled)},
            "configured": configured,
            "enabled": enabled,
        }

    if not (m := MEMTABLE_EVENT_RE.search(line)):
        return None

    body = m.group("body").strip()
    fields = parse_memory_table_kv(body)
    event: dict[str, object] = {
        "tag": m.group("tag"),
        "name": m.group("name").strip(),
        "body": body,
        "fields": fields,
    }

    for key in ("row", "present", "count"):
        if key in fields:
            event[key] = parse_number(fields[key])
    if "fields" in fields:
        event["field_count"] = parse_number(fields["fields"])
    if "addr" in fields:
        event["addr"] = fields["addr"]
    if "table" in fields:
        event["table"] = fields["table"]
    if "stride" in fields:
        event["stride"] = fields["stride"]
        event["stride_int"] = parse_number(fields["stride"])
    if "scan" in fields:
        event["scan"] = fields["scan"]

    return event


def parse_memory_table_kv(body: str) -> dict[str, str]:
    return {m.group("key"): m.group("value") for m in MEMTABLE_KV_RE.finditer(body)}


def parse_hp_event_line(line: str, line_no: int = 0) -> dict[str, object] | None:
    m = HP_EVENT_RE.search(line)
    if not m:
        return None

    sample_age = m.group("sample_age")
    return {
        "line": line_no,
        "kind": m.group("kind"),
        "ptr": m.group(2),
        "id": m.group(3),
        "previous": parse_int(m.group("prev")),
        "current": parse_int(m.group("hp")),
        "amount": parse_int(m.group("amount")),
        "sample_age_ms": parse_int(sample_age) if sample_age is not None else None,
    }


def parse_mp_event_line(line: str, line_no: int = 0) -> dict[str, object] | None:
    m = MP_EVENT_RE.search(line)
    if not m:
        return None

    sample_age = m.group("sample_age")
    previous = parse_int(m.group("prev"))
    current = parse_int(m.group("mp"))
    return {
        "line": line_no,
        "kind": m.group("kind"),
        "ptr": m.group(2),
        "id": m.group(3),
        "previous": previous,
        "current": current,
        "amount": parse_int(m.group("amount")),
        "change": current - previous,
        "sample_age_ms": parse_int(sample_age) if sample_age is not None else None,
    }


def parse_rewrite_line(line: str, line_no: int = 0) -> dict[str, object] | None:
    m = REWRITE_EVENT_RE.search(line)
    if not m:
        return None

    tag = m.group("tag")
    body = m.group("body")
    rewrite: dict[str, object] = {
        "line": line_no,
        "tag": tag,
        "ptr": m.group(2),
        "id": m.group(3),
        "body": body,
        "resource": "mp" if tag.startswith("MP-") else "hp",
        "status": rewrite_status(tag),
    }

    body_re = MP_REWRITE_BODY_RE if rewrite["resource"] == "mp" else HP_REWRITE_BODY_RE
    if body_match := body_re.search(body):
        rewrite.update(
            {
                "rule": body_match.group("rule"),
                "vanilla": parse_int(body_match.group("vanilla")),
                "final": parse_int(body_match.group("final")),
                "from_value": parse_int(body_match.group("from")),
                "to_value": parse_int(body_match.group("to")),
            }
        )

    return rewrite


def parse_death_line(line: str, line_no: int = 0) -> dict[str, object] | None:
    m = DEATH_EVENT_RE.search(line)
    if not m:
        return None

    tag = m.group("tag")
    body = m.group("body")
    event: dict[str, object] = {
        "line": line_no,
        "tag": tag,
        "status": death_status(tag),
        "ptr": m.group(2),
        "id": m.group(3),
        "meta": m.group("meta").strip(),
        "body": body,
        "offsets": OFFSET_RE.findall(body),
    }

    if write_match := DEATH_WRITE_BODY_RE.search(body):
        event.update(
            {
                "name": write_match.group("name").strip(),
                "offset": f"+0x{write_match.group('offset')}",
                "offset_int": int(write_match.group("offset"), 16),
                "width": parse_int(write_match.group("width")),
                "from_value": parse_number("0x" + write_match.group("from")),
                "to_value": parse_number("0x" + write_match.group("to")),
            }
        )

    return event


def death_status(tag: str) -> str:
    if tag.endswith("-FAILED"):
        return "failed"
    if tag.endswith("-SKIP"):
        return "skip"
    if tag == "DEATH-WRITE":
        return "write"
    if tag == "DEATH-DIFF":
        return "diff"
    if tag == "DEATH-FOLLOW":
        return "follow"
    return "dump"


def rewrite_status(tag: str) -> str:
    if tag.endswith("-DRY-RUN"):
        return "dry-run"
    if tag.endswith("-FAILED"):
        return "failed"
    if tag.endswith("-ECHO-SKIP"):
        return "echo-skip"
    if tag.endswith("-SKIP"):
        return "skip"
    return "write"


def parse_runtime_context(ptr: str, body: str) -> dict[str, object]:
    fields = split_runtime_fields(body)
    return {
        "ptr": ptr,
        "raw": body,
        "event": fields.get("event", ""),
        "attacker": fields.get("attacker", ""),
        "action": parse_action(fields.get("action", "")),
        "target_slots": parse_slot_list(fields.get("targetSlots", "")),
        "attacker_slots": parse_slot_list(fields.get("attackerSlots", "")),
        "equipment_dr": fields.get("equipmentDr", ""),
        "response": parse_response(fields.get("response", "")),
        "trace_vars": parse_trace_vars(fields.get("vars", "")),
        "final": parse_final(fields.get("final", "")),
    }


def split_runtime_fields(body: str) -> dict[str, str]:
    fields: dict[str, str] = {}
    for part in body.split(" | "):
        key, sep, value = part.partition("=")
        if not sep:
            continue
        fields[key.strip()] = value.strip()
    return fields


def parse_action(text: str) -> dict[str, object]:
    if not text or text == "none":
        return {"name": "none", "source": "", "signal": "", "vars": "", "variables": {}}

    m = re.match(r"(?P<name>.*?):source=(?P<source>.*?):signal=(?P<signal>-?\d+)(?::vars=(?P<vars>.*))?$", text)
    if not m:
        return {"name": text, "source": "", "signal": "", "vars": "", "variables": {}}

    variables = m.group("vars") or ""
    return {
        "name": m.group("name"),
        "source": m.group("source"),
        "signal": m.group("signal"),
        "vars": variables,
        "variables": parse_trace_vars(variables),
    }


def parse_trace_vars(text: str) -> dict[str, str]:
    values: dict[str, str] = {}
    if not text or text == "none":
        return values

    for piece in text.split(","):
        name, sep, value = piece.partition("=")
        name = name.strip()
        if not sep or not name:
            continue
        values[name] = value.strip()
    return values


def parse_slot_list(text: str) -> list[dict[str, object]]:
    if not text or text == "none":
        return []

    slots: list[dict[str, object]] = []
    for m in SLOT_ENTRY_RE.finditer(text):
        slot_name = m.group("slot")
        inner = m.group("body")
        pieces = [piece.strip() for piece in inner.split(",") if piece.strip()]
        state = pieces[0] if pieces else "unknown"
        values: dict[str, str] = {}
        for piece in pieces[1:]:
            key, sep, value = piece.partition("=")
            if sep:
                values[key.strip()] = value.strip()

        item_id, item_name = parse_item_label(values.get("id", "0"))
        offset_text = values.get("off", "?")
        slots.append(
            {
                "slot": slot_name,
                "state": state,
                "item_id": item_id,
                "item_name": item_name,
                "offset": offset_text,
                "offset_int": parse_offset(offset_text),
                "width": values.get("width", "?"),
                "matches": parse_int(values.get("matches", "0")),
            }
        )

    return slots


def parse_item_label(text: str) -> tuple[int, str]:
    item_id_text, sep, item_name = text.partition(":")
    return parse_int(item_id_text), item_name if sep else ""


def parse_offset(text: str) -> int | None:
    if not text.startswith("0x"):
        return None
    try:
        return int(text, 16)
    except ValueError:
        return None


def parse_response(text: str) -> dict[str, object]:
    m = RESPONSE_RE.match(text)
    if not m:
        return {"raw": "", "permille": "", "rules": "", "clamped": "", "rule": text}
    return {
        "raw": parse_int(m.group("raw")),
        "permille": parse_int(m.group("permille")),
        "rules": parse_int(m.group("rules")),
        "clamped": parse_int(m.group("clamped")),
        "rule": m.group("rule"),
    }


def parse_final(text: str) -> dict[str, object]:
    m = FINAL_RE.match(text)
    if not m:
        return {"value": "", "rule": text}
    return {"value": parse_int(m.group("value")), "rule": m.group("rule")}


def parse_int(text: str) -> int:
    try:
        return int(text)
    except (TypeError, ValueError):
        return 0


def parse_number(text: str) -> int | None:
    try:
        if text.lower().startswith("0x"):
            return int(text, 16)
        return int(text)
    except (AttributeError, ValueError):
        return None


def render(
    log: Path,
    headers: list[str],
    units: dict[str, dict[str, str]],
    candidates: dict[str, list[str]],
    dumps: dict[str, list[int]],
    diffs: dict[str, list[str]],
    diff_offsets: Counter[str],
    hp_events: list[dict[str, object]],
    mp_events: list[dict[str, object]],
    contexts: list[tuple[str, str]],
    runtime_contexts: list[tuple[str, str]],
    rewrites: list[dict[str, object]],
    death_events: list[dict[str, object]],
    memory_table_events: list[dict[str, object]],
    old_unit_lines: int,
    item_catalog: dict[int, dict[str, str]],
    catalog_path: Path,
) -> str:
    runtime_details = parse_runtime_contexts(runtime_contexts)
    lines: list[str] = []
    lines.append("# Battle Probe Analysis")
    lines.append("")
    lines.append(f"Source: `{log}`")
    if item_catalog:
        lines.append(f"Item catalog: `{catalog_path}` ({len(item_catalog)} ids)")
    lines.append("")

    if headers:
        lines.append("## Header")
        for h in headers[:12]:
            lines.append(f"- `{h}`")
        lines.append("")

    warnings = build_warnings(headers, units, runtime_contexts, old_unit_lines)
    if warnings:
        lines.append("## Warnings")
        for warning in warnings:
            lines.append(f"- {warning}")
        lines.append("")

    lines.append("## Units")
    if not units:
        lines.append("No pointer-keyed `[UNIT]` lines found. Restart the game so the current DLL loads.")
    else:
        lines.append("| Ptr | Id | Faction | Team | Stats |")
        lines.append("| --- | --- | --- | --- | --- |")
        for ptr, info in units.items():
            lines.append(f"| `{ptr}` | `{info['id']}` | {info['faction']} | {info['team']} | {info['stats']} |")
    lines.append("")

    lines.append("## Candidate Fields")
    if not candidates:
        lines.append("No `[CANDIDATES]` lines found.")
    else:
        for ptr, bodies in candidates.items():
            unit = units.get(ptr, {})
            for body in bodies:
                lines.append(f"### `{ptr}` `{unit.get('id', '?')}` {unit.get('faction', '')} t{unit.get('team', '?')}")
                lines.append(f"`{body}`")
    lines.append("")

    if item_catalog:
        lines.append("## Candidate Item Hits")
        if not candidates and not dumps:
            lines.append("No candidates or dumps to compare with the item catalog.")
        else:
            any_hit = False
            for ptr, raw in dumps.items():
                hits = dump_item_hits(raw, item_catalog)
                if not hits:
                    continue
                any_hit = True
                unit = units.get(ptr, {})
                lines.append(f"### `{ptr}` `{unit.get('id', '?')}` {unit.get('faction', '')} t{unit.get('team', '?')} dump scan")
                lines.append("| Offset | Width | Item | Type | Category | Secondary |")
                lines.append("| --- | --- | --- | --- | --- | --- |")
                for offset, width, item in hits[:80]:
                    item_label = f"{item.get('item_id', '?')} {item.get('name', '')}".strip()
                    lines.append(
                        "| "
                        f"`{offset}` | {width} | {item_label} | {item.get('type_flags', '')} | "
                        f"{item.get('item_category', '')} | {item.get('secondary_kind', '')} |"
                    )
                if len(hits) > 80:
                    lines.append(f"")
                    lines.append(f"_Truncated: {len(hits) - 80} more dump item-id hits._")
            for ptr, bodies in candidates.items():
                if ptr in dumps:
                    continue
                hits = []
                for body in bodies:
                    hits.extend(candidate_item_hits(body, item_catalog))
                if not hits:
                    continue
                any_hit = True
                unit = units.get(ptr, {})
                lines.append(f"### `{ptr}` `{unit.get('id', '?')}` {unit.get('faction', '')} t{unit.get('team', '?')} candidate summary")
                lines.append("| Offset | Width | Item | Type | Category | Secondary |")
                lines.append("| --- | --- | --- | --- | --- | --- |")
                for offset, width, item in hits[:40]:
                    item_label = f"{item.get('item_id', '?')} {item.get('name', '')}".strip()
                    lines.append(
                        "| "
                        f"`{offset}` | {width} | {item_label} | {item.get('type_flags', '')} | "
                        f"{item.get('item_category', '')} | {item.get('secondary_kind', '')} |"
                    )
            if not any_hit:
                lines.append("No candidate values matched item ids in the catalog.")
        lines.append("")

    lines.append("## Diff Offset Frequency")
    if not diff_offsets:
        lines.append("No `[DIFF]` lines found.")
    else:
        lines.append("| Offset | Count |")
        lines.append("| --- | ---: |")
        for off, count in diff_offsets.most_common():
            lines.append(f"| `+0x{off}` | {count} |")
    lines.append("")

    lines.append("## Unit Diffs")
    if diffs:
        for ptr, entries in diffs.items():
            unit = units.get(ptr, {})
            lines.append(f"### `{ptr}` `{unit.get('id', '?')}`")
            for entry in entries[:20]:
                lines.append(f"- `{entry}`")
    else:
        lines.append("No per-unit diffs.")
    lines.append("")

    lines.extend(render_memory_table_summary(memory_table_events))
    lines.extend(render_runtime_summary(runtime_details))
    lines.extend(render_death_summary(death_events))
    lines.extend(render_death_gate_summary(rewrites, death_events))

    lines.append("## HP Events / Rewrite")
    damage = [event for event in hp_events if event.get("kind") == "DAMAGE"]
    healing = [event for event in hp_events if event.get("kind") == "HEALING"]
    lines.append(f"- Damage events: {len(damage)}")
    lines.append(f"- Healing events: {len(healing)}")
    sample_ages = [int(event["sample_age_ms"]) for event in hp_events if isinstance(event.get("sample_age_ms"), int) and int(event["sample_age_ms"]) >= 0]
    if sample_ages:
        lines.append(f"- HP sample age: min={min(sample_ages)}ms max={max(sample_ages)}ms")
    if damage:
        dmg_counts = Counter(str(event.get("ptr", "")) for event in damage)
        lines.append("- Damage by unit: " + ", ".join(f"`{ptr}`={count}" for ptr, count in dmg_counts.items()))
    if healing:
        heal_counts = Counter(str(event.get("ptr", "")) for event in healing)
        lines.append("- Healing by unit: " + ", ".join(f"`{ptr}`={count}" for ptr, count in heal_counts.items()))
    lines.append(f"- Context events: {len(contexts)}")
    if contexts:
        resolved = sum(1 for _, body in contexts if "resolved=0x" in body)
        lines.append(f"- Context resolved: {resolved}/{len(contexts)}")
        for ptr, body in contexts[:20]:
            lines.append(f"  - `{ptr}` {body}")
    lines.append(f"- Runtime context events: {len(runtime_contexts)}")
    for ptr, body in runtime_contexts[:20]:
        lines.append(f"  - `{ptr}` {body}")
    lines.append(f"- Rewrite events: {len(rewrites)}")
    if rewrites:
        status_counts = Counter(str(rewrite.get("status", "?")) for rewrite in rewrites)
        lines.append("- Rewrite status: " + ", ".join(f"{status}={count}" for status, count in status_counts.items()))
    for rewrite in rewrites[:20]:
        parsed = ""
        if "final" in rewrite:
            parsed = f" final={rewrite.get('final')} {rewrite.get('from_value')}->{rewrite.get('to_value')}"
        lines.append(f"  - `{rewrite.get('ptr')}` `{rewrite.get('tag')}`{parsed} {rewrite.get('body')}")
    lines.append("")
    lines.extend(render_neuter_placeholder_summary(rewrites))
    lines.extend(render_hp_write_proof_summary(hp_events, rewrites))
    lines.extend(render_mp_rewrite_summary(mp_events, rewrites))
    lines.append("")

    return "\n".join(lines)


def render_neuter_placeholder_summary(rewrites: list[dict[str, object]]) -> list[str]:
    lines = ["### Neuter Placeholder Check"]
    hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp"
        and rewrite.get("status") in {"write", "dry-run"}
        and isinstance(rewrite.get("vanilla"), int)
    ]
    failed_hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp" and rewrite.get("status") == "failed"
    ]

    if not hp_rewrites:
        lines.append("- No parsed HP rewrite lines with `vanillaDamage`.")
        lines.append("- Verdict: **no HP placeholder evidence**")
        lines.append("")
        return lines

    positive = [rewrite for rewrite in hp_rewrites if int(rewrite.get("vanilla", 0)) > 0]
    healing = [rewrite for rewrite in hp_rewrites if int(rewrite.get("vanilla", 0)) < 0]
    lethal_zero = [
        rewrite for rewrite in hp_rewrites
        if int(rewrite.get("final", 0)) >= 9999 and rewrite.get("to_value") == 0
    ]

    lines.append(f"- HP rewrite lines with parsed vanilla damage: {len(hp_rewrites)}")
    lines.append(f"- Positive damage rewrites: {len(positive)}")
    if positive:
        values = [int(rewrite.get("vanilla", 0)) for rewrite in positive]
        small = [value for value in values if 0 < value <= PLACEHOLDER_DAMAGE_MAX]
        one = [value for value in values if value == 1]
        large = [value for value in values if value > PLACEHOLDER_DAMAGE_MAX]
        lines.append(f"- Positive `vanillaDamage`: min={min(values)} max={max(values)}")
        lines.append(f"- Placeholder-sized positive rewrites (`1..{PLACEHOLDER_DAMAGE_MAX}`): {len(small)}/{len(values)}")
        lines.append(f"- Exact `vanillaDamage=1` rewrites: {len(one)}/{len(values)}")
        if large:
            lines.append(f"- Large positive vanilla deltas (`>{PLACEHOLDER_DAMAGE_MAX}`): {len(large)}")
    if healing:
        values = [int(rewrite.get("vanilla", 0)) for rewrite in healing]
        lines.append(f"- Healing rewrite vanilla deltas: min={min(values)} max={max(values)}")
    lines.append(f"- Death-gate lethal rewrites (`finalDamage>=9999`, HP -> 0): {len(lethal_zero)}")
    lines.append(f"- HP rewrite failures: {len(failed_hp_rewrites)}")
    lines.append(f"- Verdict: **{neuter_placeholder_verdict(positive, failed_hp_rewrites)}**")
    lines.append("")
    return lines


def neuter_placeholder_verdict(
    positive_rewrites: list[dict[str, object]],
    failed_hp_rewrites: list[dict[str, object]],
) -> str:
    if failed_hp_rewrites:
        return "failed: HP rewrite failures logged"
    if not positive_rewrites:
        return "no positive HP damage rewrites to evaluate"

    values = [int(rewrite.get("vanilla", 0)) for rewrite in positive_rewrites]
    if all(0 < value <= PLACEHOLDER_DAMAGE_MAX for value in values):
        return "pass-candidate: observed positive vanilla damage is placeholder-sized"
    return "attention: large vanilla damage observed; data neuter may be incomplete or this is not a neuter run"


def render_death_summary(death_events: list[dict[str, object]]) -> list[str]:
    lines: list[str] = ["## Death State"]
    if not death_events:
        lines.append("No parsed `[DEATH-*]` lines.")
        lines.append("")
        return lines

    tag_counts = Counter(str(event.get("tag", "?")) for event in death_events)
    status_counts = Counter(str(event.get("status", "?")) for event in death_events)
    lines.append("- Events: " + ", ".join(f"`{tag}`={count}" for tag, count in tag_counts.most_common()))
    lines.append("- Status: " + ", ".join(f"{status}={count}" for status, count in status_counts.most_common()))

    diff_events = [event for event in death_events if event.get("status") in {"diff", "follow"}]
    diff_offsets = Counter(
        str(offset)
        for event in diff_events
        for offset in event.get("offsets", [])
    )
    if diff_offsets:
        lines.append("- Death diff offsets: " + ", ".join(f"`+0x{off}`={count}" for off, count in diff_offsets.most_common()))

    writes = [event for event in death_events if str(event.get("tag", "")).startswith("DEATH-WRITE")]
    concrete_writes = [event for event in writes if event.get("status") == "write"]
    failed_writes = [event for event in writes if event.get("status") == "failed"]
    skipped_writes = [event for event in writes if event.get("status") == "skip"]
    if writes:
        lines.append(
            f"- Death writes: concrete={len(concrete_writes)}, failed={len(failed_writes)}, skipped={len(skipped_writes)}"
        )

    ko_flag_diffs = [
        event for event in diff_events
        if "61" in event.get("offsets", []) or "+0x61" in str(event.get("body", ""))
    ]
    ko_flag_writes = [
        event for event in concrete_writes
        if event.get("offset_int") == 0x61
    ]
    lines.append(f"- Verdict: **{death_summary_verdict(death_events, failed_writes, ko_flag_diffs, ko_flag_writes)}**")

    if diff_events:
        lines.append("")
        lines.append("### Death Diffs")
        for event in diff_events[:12]:
            lines.append(f"- line {event.get('line')}: `{event.get('tag')}` `{event.get('ptr')}` {event.get('body')}")
        if len(diff_events) > 12:
            lines.append(f"- _Truncated: {len(diff_events) - 12} more death diff/follow event(s)._")

    if writes:
        lines.append("")
        lines.append("### Death Writes")
        for event in writes[:12]:
            parsed = ""
            if "offset" in event:
                parsed = f" parsed={event.get('offset')} w{event.get('width')} {event.get('from_value')}->{event.get('to_value')}"
            lines.append(f"- line {event.get('line')}: `{event.get('tag')}` `{event.get('ptr')}`{parsed} {event.get('body')}")
        if len(writes) > 12:
            lines.append(f"- _Truncated: {len(writes) - 12} more death write event(s)._")

    lines.append("")
    return lines


def death_summary_verdict(
    death_events: list[dict[str, object]],
    failed_writes: list[dict[str, object]],
    ko_flag_diffs: list[dict[str, object]],
    ko_flag_writes: list[dict[str, object]],
) -> str:
    if failed_writes:
        return "failed: death-state write failures logged"
    if ko_flag_writes:
        return "write-candidate: KO flag +0x61 was written; confirm the unit actually dies in-game"
    if ko_flag_diffs:
        return "mapping-candidate: death diffs include KO flag +0x61"
    if death_events:
        return "death-state evidence present; review diffs/writes"
    return "no death-state evidence"


def render_death_gate_summary(
    rewrites: list[dict[str, object]],
    death_events: list[dict[str, object]],
) -> list[str]:
    lines: list[str] = ["## Death Gate Outcome"]
    hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp" and rewrite.get("status") in {"write", "dry-run"}
    ]
    lethal_zero = [
        rewrite for rewrite in hp_rewrites
        if isinstance(rewrite.get("final"), int)
        and int(rewrite.get("final", 0)) >= 9999
        and rewrite.get("to_value") == 0
    ]
    concrete_lethal_zero = [rewrite for rewrite in lethal_zero if rewrite.get("status") == "write"]
    dry_run_lethal_zero = [rewrite for rewrite in lethal_zero if rewrite.get("status") == "dry-run"]
    failed_hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp" and rewrite.get("status") == "failed"
    ]
    placeholder_lethal = [
        rewrite for rewrite in lethal_zero
        if isinstance(rewrite.get("vanilla"), int) and 0 < int(rewrite.get("vanilla", 0)) <= PLACEHOLDER_DAMAGE_MAX
    ]
    large_lethal = [
        rewrite for rewrite in lethal_zero
        if isinstance(rewrite.get("vanilla"), int) and int(rewrite.get("vanilla", 0)) > PLACEHOLDER_DAMAGE_MAX
    ]
    death_writes = [event for event in death_events if event.get("status") == "write"]
    failed_death_writes = [event for event in death_events if event.get("status") == "failed"]
    ko_flag_writes = [event for event in death_writes if event.get("offset_int") == 0x61]
    ko_flag_diffs = [
        event for event in death_events
        if event.get("status") in {"diff", "follow"}
        and ("61" in event.get("offsets", []) or "+0x61" in str(event.get("body", "")))
    ]

    lines.append(f"- Lethal HP rewrites (`finalDamage>=9999`, HP -> 0): {len(lethal_zero)}")
    lines.append(f"- Concrete lethal HP writes: {len(concrete_lethal_zero)}")
    lines.append(f"- Dry-run lethal HP decisions: {len(dry_run_lethal_zero)}")
    lines.append(f"- Placeholder-sized lethal rewrites: {len(placeholder_lethal)}/{len(lethal_zero)}")
    if large_lethal:
        lines.append(f"- Large-vanilla lethal rewrites: {len(large_lethal)}")
    lines.append(f"- Death events: {len(death_events)}")
    lines.append(f"- KO flag diffs (`+0x61`): {len(ko_flag_diffs)}")
    lines.append(f"- KO flag writes (`+0x61`): {len(ko_flag_writes)}")
    lines.append(f"- HP rewrite failures: {len(failed_hp_rewrites)}")
    lines.append(f"- Death-write failures: {len(failed_death_writes)}")
    lines.append(f"- Verdict: **{death_gate_verdict(lethal_zero, concrete_lethal_zero, failed_hp_rewrites, death_events, failed_death_writes, ko_flag_writes, ko_flag_diffs, large_lethal)}**")
    if concrete_lethal_zero and not death_events:
        lines.append("- Note: absence of `[DEATH-*]` is strongest when this log was captured with the watcher `--settle-seconds` negative gate.")

    if lethal_zero:
        lines.append("")
        lines.append("### Lethal Rewrite Samples")
        for rewrite in lethal_zero[:8]:
            lines.append(
                f"- line {rewrite.get('line')}: `{rewrite.get('tag')}` `{rewrite.get('ptr')}` "
                f"vanilla={rewrite.get('vanilla')} final={rewrite.get('final')} "
                f"{rewrite.get('from_value')}->{rewrite.get('to_value')} rule={rewrite.get('rule', '?')}"
            )
        if len(lethal_zero) > 8:
            lines.append(f"- _Truncated: {len(lethal_zero) - 8} more lethal rewrite event(s)._")
    lines.append("")
    return lines


def death_gate_verdict(
    lethal_zero: list[dict[str, object]],
    concrete_lethal_zero: list[dict[str, object]],
    failed_hp_rewrites: list[dict[str, object]],
    death_events: list[dict[str, object]],
    failed_death_writes: list[dict[str, object]],
    ko_flag_writes: list[dict[str, object]],
    ko_flag_diffs: list[dict[str, object]],
    large_lethal: list[dict[str, object]],
) -> str:
    if failed_hp_rewrites:
        return "failed: HP rewrite failures logged"
    if failed_death_writes:
        return "failed: death-state write failures logged"
    if not lethal_zero:
        return "no death-gate lethal HP rewrite evidence"
    if not concrete_lethal_zero:
        return "dry-run only: formulas reached HP 0, but no live HP write was attempted"
    if large_lethal:
        return "attention: lethal rewrite came from large vanilla damage; verify the neuter placeholder first"
    if ko_flag_writes:
        return "killflag-branch evidence: runtime wrote KO flag +0x61; confirm the unit dies in-game"
    if ko_flag_diffs:
        return "HP-only branch evidence: HP=0 write coincided with KO flag diff +0x61; confirm in-game death"
    if death_events:
        return "HP-only branch evidence: death events appeared after HP=0 write; review diffs/visual result"
    return "zombie-candidate: HP=0 was written, but no death-state evidence appears in this log"


def render_memory_table_summary(memory_table_events: list[dict[str, object]]) -> list[str]:
    lines: list[str] = []
    lines.append("## Memory Table Probes")
    if not memory_table_events:
        lines.append("No parsed `[MEMTABLE]` lines.")
        lines.append("")
        return lines

    counts = Counter(str(event.get("tag", "")) for event in memory_table_events)
    lines.append("- Events: " + ", ".join(f"`{tag}`={count}" for tag, count in counts.most_common()))

    configs = [event for event in memory_table_events if event.get("tag") == "MEMTABLE"]
    for config in configs[-3:]:
        lines.append(f"- Configured probes: {config.get('configured', '?')}, enabled: {config.get('enabled', '?')}")
    lines.append("")

    found = [event for event in memory_table_events if event.get("tag") == "MEMTABLE-FOUND"]
    lines.append("### Found Tables")
    if not found:
        lines.append("No `[MEMTABLE-FOUND]` events.")
    else:
        lines.append("| Probe | Scan | Table | Stride | Count | Fields |")
        lines.append("| --- | --- | --- | ---: | ---: | ---: |")
        for event in found:
            fields = memory_table_fields(event)
            lines.append(
                "| "
                f"{event.get('name', '?')} | `{fields.get('scan', '?')}` | `{fields.get('table', '?')}` | "
                f"`{fields.get('stride', '?')}` | {fields.get('count', '?')} | {fields.get('fields', '?')} |"
            )
    lines.append("")

    rows = [event for event in memory_table_events if event.get("tag") == "MEMTABLE-ROW"]
    lines.append("### Rows")
    if not rows:
        lines.append("No `[MEMTABLE-ROW]` events.")
    else:
        lines.append("| Probe | Row | Address | Presence | Fields |")
        lines.append("| --- | ---: | --- | ---: | --- |")
        for event in rows[:80]:
            fields = memory_table_fields(event)
            lines.append(
                "| "
                f"{event.get('name', '?')} | {fields.get('row', '?')} | `{fields.get('addr', '?')}` | "
                f"{fields.get('present', '?')} | `{format_memory_table_row_fields(event)}` |"
            )
        if len(rows) > 80:
            lines.append(f"")
            lines.append(f"_Truncated: {len(rows) - 80} more memory-table rows._")
    lines.append("")

    issue_tags = {"MEMTABLE-SKIP", "MEMTABLE-NOTFOUND", "MEMTABLE-FAILED", "MEMTABLE-ROW-SKIP", "MEMTABLE-ROWS"}
    issues = [event for event in memory_table_events if event.get("tag") in issue_tags]
    lines.append("### Probe Issues")
    if not issues:
        lines.append("No probe issues logged.")
    else:
        for event in issues[:80]:
            name = f" {event.get('name')}" if event.get("name") else ""
            body = str(event.get("body", ""))
            lines.append(f"- `{event.get('tag')}{name}` {body}")
        if len(issues) > 80:
            lines.append(f"- _Truncated: {len(issues) - 80} more probe issue(s)._")
    lines.append("")
    return lines


def render_hp_write_proof_summary(hp_events: list[dict[str, object]], rewrites: list[dict[str, object]]) -> list[str]:
    lines = ["### HP Write Proof Check"]
    hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp" and rewrite.get("status") in {"write", "dry-run"}
    ]
    concrete_rewrites = [rewrite for rewrite in hp_rewrites if rewrite.get("status") == "write"]
    failed_hp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "hp" and rewrite.get("status") == "failed"
    ]
    final_one = [rewrite for rewrite in concrete_rewrites if rewrite.get("final") == 1]
    correlations = correlate_hp_rewrites(hp_events, concrete_rewrites)
    mismatches = [row for row in correlations if not row.get("matches_previous_minus_final", False)]
    sample_ages = [
        int(event["sample_age_ms"])
        for event in hp_events
        if isinstance(event.get("sample_age_ms"), int) and int(event["sample_age_ms"]) >= 0
    ]

    lines.append(f"- Concrete HP rewrites: {len(concrete_rewrites)}")
    lines.append(f"- Concrete `finalDamage=1` rewrites: {len(final_one)}/{len(concrete_rewrites)}")
    lines.append(f"- HP rewrite failures: {len(failed_hp_rewrites)}")
    if sample_ages:
        lines.append(f"- Baseline sample age: min={min(sample_ages)}ms max={max(sample_ages)}ms")
    else:
        lines.append("- Baseline sample age: missing")

    if correlations:
        matched = len(correlations) - len(mismatches)
        lines.append(f"- Rewrite/event correlation: {matched}/{len(correlations)} desired HP values match `previousHp - finalDamage`")

    verdict = hp_write_proof_verdict(concrete_rewrites, failed_hp_rewrites, final_one, sample_ages, mismatches)
    lines.append(f"- Verdict: **{verdict}**")

    if failed_hp_rewrites:
        for rewrite in failed_hp_rewrites[:8]:
            lines.append(f"  - failure line {rewrite.get('line')}: `{rewrite.get('body')}`")
    if mismatches:
        for row in mismatches[:8]:
            rewrite = row["rewrite"]
            event = row.get("event")
            event_line = event.get("line") if isinstance(event, dict) else "?"
            lines.append(
                "  - mismatch "
                f"rewrite line {rewrite.get('line')} vs event line {event_line}: "
                f"desired={rewrite.get('to_value')} expected={row.get('expected_to')}"
            )
    lines.append("")
    return lines


def hp_write_proof_verdict(
    concrete_rewrites: list[dict[str, object]],
    failed_hp_rewrites: list[dict[str, object]],
    final_one: list[dict[str, object]],
    sample_ages: list[int],
    mismatches: list[dict[str, object]],
) -> str:
    if not concrete_rewrites:
        return "no concrete HP rewrites found"
    if failed_hp_rewrites:
        return "failed: HP rewrite failures logged"
    if len(final_one) != len(concrete_rewrites):
        return "inconclusive: not all concrete HP rewrites have finalDamage=1"
    if mismatches:
        return "failed: at least one rewrite does not match previousHp - finalDamage"
    if not sample_ages:
        return "inconclusive: log does not include sampleAgeMs; rebuild/retest with the current DLL"
    if max(sample_ages) > 150:
        return "failed: stale HP baseline sample age above 150ms"
    return "pass-candidate: HP rewrites are finalDamage=1 with fresh baselines"


def render_mp_rewrite_summary(mp_events: list[dict[str, object]], rewrites: list[dict[str, object]]) -> list[str]:
    lines = ["### MP Rewrite Check"]
    mp_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "mp" and rewrite.get("status") in {"write", "dry-run"}
    ]
    concrete_rewrites = [rewrite for rewrite in mp_rewrites if rewrite.get("status") == "write"]
    dry_run_rewrites = [rewrite for rewrite in mp_rewrites if rewrite.get("status") == "dry-run"]
    failed_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "mp" and rewrite.get("status") == "failed"
    ]
    skipped_rewrites = [
        rewrite for rewrite in rewrites
        if rewrite.get("resource") == "mp" and rewrite.get("status") in {"skip", "echo-skip"}
    ]
    loss_rewrites = [
        rewrite for rewrite in mp_rewrites
        if isinstance(rewrite.get("vanilla"), int) and int(rewrite.get("vanilla", 0)) < 0
    ]
    gain_rewrites = [
        rewrite for rewrite in mp_rewrites
        if isinstance(rewrite.get("vanilla"), int) and int(rewrite.get("vanilla", 0)) > 0
    ]
    loss_events = [event for event in mp_events if event.get("kind") == "MPLOSS"]
    gain_events = [event for event in mp_events if event.get("kind") == "MPGAIN"]
    sample_ages = [
        int(event["sample_age_ms"])
        for event in mp_events
        if isinstance(event.get("sample_age_ms"), int) and int(event["sample_age_ms"]) >= 0
    ]
    final_values = [int(rewrite["final"]) for rewrite in mp_rewrites if isinstance(rewrite.get("final"), int)]
    desired_values = [int(rewrite["to_value"]) for rewrite in mp_rewrites if isinstance(rewrite.get("to_value"), int)]
    status_counts = Counter(str(rewrite.get("status", "?")) for rewrite in rewrites if rewrite.get("resource") == "mp")

    lines.append(f"- MP events: loss={len(loss_events)}, gain={len(gain_events)}")
    if sample_ages:
        lines.append(f"- MP sample age: min={min(sample_ages)}ms max={max(sample_ages)}ms")
    elif mp_events:
        lines.append("- MP sample age: missing")
    lines.append(f"- Parsed MP rewrite decisions: {len(mp_rewrites)}")
    lines.append(f"- Concrete MP writes: {len(concrete_rewrites)}")
    lines.append(f"- Dry-run MP decisions: {len(dry_run_rewrites)}")
    lines.append(f"- MP loss rewrites: {len(loss_rewrites)}")
    lines.append(f"- MP gain rewrites: {len(gain_rewrites)}")
    lines.append(f"- MP rewrite failures: {len(failed_rewrites)}")
    lines.append(f"- MP rewrite skips/echo-skips: {len(skipped_rewrites)}")
    if status_counts:
        lines.append("- MP rewrite status: " + ", ".join(f"{status}={count}" for status, count in status_counts.items()))
    if final_values:
        lines.append(f"- `finalMpChange`: min={min(final_values)} max={max(final_values)}")
    if desired_values:
        lines.append(f"- Desired MP: min={min(desired_values)} max={max(desired_values)}")

    lines.append(f"- Verdict: **{mp_rewrite_verdict(mp_events, mp_rewrites, failed_rewrites, sample_ages)}**")

    if failed_rewrites:
        for rewrite in failed_rewrites[:8]:
            lines.append(f"  - failure line {rewrite.get('line')}: `{rewrite.get('body')}`")
    if skipped_rewrites:
        for rewrite in skipped_rewrites[:8]:
            lines.append(f"  - skip line {rewrite.get('line')}: `{rewrite.get('body')}`")
    lines.append("")
    return lines


def mp_rewrite_verdict(
    mp_events: list[dict[str, object]],
    mp_rewrites: list[dict[str, object]],
    failed_rewrites: list[dict[str, object]],
    sample_ages: list[int],
) -> str:
    if failed_rewrites:
        return "failed: MP rewrite failures logged"
    if not mp_events and not mp_rewrites:
        return "no MP rewrite evidence"
    if mp_events and not mp_rewrites:
        return "no parsed MP rewrite decisions"
    if mp_events and not sample_ages:
        return "inconclusive: MP event log does not include sampleAgeMs; rebuild/retest with the current DLL"
    return "pass-candidate: MP rewrite decisions parsed without failures"


def correlate_hp_rewrites(
    hp_events: list[dict[str, object]],
    hp_rewrites: list[dict[str, object]],
) -> list[dict[str, object]]:
    by_ptr: dict[str, list[dict[str, object]]] = defaultdict(list)
    for event in hp_events:
        by_ptr[str(event.get("ptr", ""))].append(event)

    correlations: list[dict[str, object]] = []
    for rewrite in hp_rewrites:
        ptr = str(rewrite.get("ptr", ""))
        line = int(rewrite.get("line", 0) or 0)
        candidates = [
            event for event in by_ptr.get(ptr, [])
            if int(event.get("line", 0) or 0) <= line
        ]
        if not candidates:
            continue

        event = candidates[-1]
        final_damage = rewrite.get("final")
        expected_to = None
        matches = False
        if isinstance(final_damage, int):
            expected_to = int(event.get("previous", 0)) - final_damage
            matches = rewrite.get("from_value") == event.get("current") and rewrite.get("to_value") == expected_to
        correlations.append(
            {
                "rewrite": rewrite,
                "event": event,
                "expected_to": expected_to,
                "matches_previous_minus_final": matches,
            }
        )

    return correlations


def memory_table_fields(event: dict[str, object]) -> dict[str, str]:
    fields = event.get("fields", {})
    if isinstance(fields, dict):
        return {str(key): str(value) for key, value in fields.items()}
    return {}


def format_memory_table_row_fields(event: dict[str, object]) -> str:
    metadata = {"row", "addr", "present", "scan", "table", "stride", "count", "fields"}
    fields = memory_table_fields(event)
    values = [f"{key}={value}" for key, value in fields.items() if key not in metadata]
    return ", ".join(values) if values else "-"


def render_runtime_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    lines: list[str] = []
    lines.append("## Runtime Context Summary")
    if not runtime_details:
        lines.append("No parsed `[RUNTIME]` lines.")
        lines.append("")
        return lines

    event_counts = Counter(str(ctx.get("event", "")) or "unknown" for ctx in runtime_details)
    attacker_resolved = sum(1 for ctx in runtime_details if str(ctx.get("attacker", "")) not in ("", "none"))
    lines.append(f"- Parsed runtime contexts: {len(runtime_details)}")
    lines.append("- Events: " + ", ".join(f"{name}={count}" for name, count in event_counts.items()))
    lines.append(f"- Attackers resolved: {attacker_resolved}/{len(runtime_details)}")
    lines.append("")

    lines.extend(render_action_summary(runtime_details))
    lines.extend(render_action_var_summary(runtime_details))
    lines.extend(render_trace_var_summary(runtime_details))
    lines.extend(render_slot_summary(runtime_details))
    lines.extend(render_response_summary(runtime_details))
    lines.extend(render_dr_response_proof_summary(runtime_details))
    return lines


def render_action_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    actions: Counter[tuple[str, str, str, str]] = Counter()
    for ctx in runtime_details:
        action = ctx.get("action", {})
        if not isinstance(action, dict):
            continue
        key = (
            str(action.get("name", "")),
            str(action.get("signal", "")),
            str(action.get("source", "")),
            str(action.get("vars", "")),
        )
        actions[key] += 1

    lines = ["### Actions"]
    if not actions:
        lines.append("No action data parsed.")
        lines.append("")
        return lines

    lines.append("| Action | Signal | Source | Vars | Count |")
    lines.append("| --- | ---: | --- | --- | ---: |")
    for (name, signal, source, variables), count in actions.most_common(20):
        lines.append(f"| {name or '?'} | {signal or '?'} | {source or '?'} | `{variables or '-'}` | {count} |")
    lines.append("")
    return lines


def render_action_var_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    by_name: dict[str, Counter[str]] = defaultdict(Counter)
    for ctx in runtime_details:
        action = ctx.get("action", {})
        if not isinstance(action, dict):
            continue
        variables = action.get("variables", {})
        if not isinstance(variables, dict):
            continue
        for name, value in variables.items():
            by_name[str(name)][str(value)] += 1

    lines = ["### Action Variables"]
    if not by_name:
        lines.append("No action variables parsed.")
        lines.append("")
        return lines

    lines.append("| Variable | Count | Numeric Range | Top Values |")
    lines.append("| --- | ---: | --- | --- |")
    for name, values in sorted(by_name.items()):
        numeric_values = [parse_number(value) for value in values.elements()]
        numeric_values = [value for value in numeric_values if value is not None]
        numeric_range = "-"
        if numeric_values:
            numeric_range = f"{min(numeric_values)}..{max(numeric_values)}"
        top_values = ", ".join(f"`{value}` x{count}" for value, count in values.most_common(5))
        lines.append(f"| `{name}` | {sum(values.values())} | {numeric_range} | {top_values or '-'} |")
    lines.append("")
    return lines


def render_trace_var_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    by_name: dict[str, Counter[str]] = defaultdict(Counter)
    for ctx in runtime_details:
        trace_vars = ctx.get("trace_vars", {})
        if not isinstance(trace_vars, dict):
            continue
        for name, value in trace_vars.items():
            by_name[str(name)][str(value)] += 1

    lines = ["### Formula Trace Variables"]
    if not by_name:
        lines.append("No formula trace variables parsed.")
        lines.append("")
        return lines

    lines.append("| Variable | Count | Numeric Range | Top Values |")
    lines.append("| --- | ---: | --- | --- |")
    for name, values in sorted(by_name.items()):
        numeric_values = [parse_number(value) for value in values.elements()]
        numeric_values = [value for value in numeric_values if value is not None]
        numeric_range = "-"
        if numeric_values:
            numeric_range = f"{min(numeric_values)}..{max(numeric_values)}"
        top_values = ", ".join(f"`{value}` x{count}" for value, count in values.most_common(5))
        lines.append(f"| `{name}` | {sum(values.values())} | {numeric_range} | {top_values or '-'} |")
    lines.append("")
    return lines


def render_slot_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    observations: list[dict[str, object]] = []
    for ctx in runtime_details:
        observations.extend(slot_observations("target", ctx.get("target_slots", [])))
        observations.extend(slot_observations("attacker", ctx.get("attacker_slots", [])))

    lines = ["### Slots"]
    if not observations:
        lines.append("No slot data parsed.")
        lines.append("")
        return lines

    grouped: Counter[tuple[str, str, str, str, str, int, str, int]] = Counter()
    for obs in observations:
        key = (
            str(obs["scope"]),
            str(obs["slot"]),
            str(obs["state"]),
            str(obs["offset"]),
            str(obs["width"]),
            int(obs["item_id"]),
            str(obs["item_name"]),
            int(obs["matches"]),
        )
        grouped[key] += 1

    lines.append("| Scope | Slot | State | Offset | Width | Item | Matches | Count |")
    lines.append("| --- | --- | --- | --- | --- | --- | ---: | ---: |")
    for (scope, slot, state, offset, width, item_id, item_name, matches), count in grouped.most_common(40):
        item = f"{item_id}:{item_name}" if item_name else str(item_id)
        lines.append(f"| {scope} | {slot} | {state} | `{offset}` | {width} | {item} | {matches} | {count} |")
    lines.append("")

    lines.extend(render_slot_recommendations(observations))
    return lines


def slot_observations(scope: str, slots: object) -> list[dict[str, object]]:
    if not isinstance(slots, list):
        return []
    observations: list[dict[str, object]] = []
    for slot in slots:
        if not isinstance(slot, dict):
            continue
        observations.append({"scope": scope, **slot})
    return observations


def render_slot_recommendations(observations: list[dict[str, object]]) -> list[str]:
    by_slot: dict[tuple[str, str], list[dict[str, object]]] = defaultdict(list)
    for obs in observations:
        by_slot[(str(obs["scope"]), str(obs["slot"]))].append(obs)

    lines = ["### Slot Recommendations"]
    for (scope, slot), rows in sorted(by_slot.items()):
        total = len(rows)
        present = [row for row in rows if row.get("state") == "present"]
        ambiguous = [row for row in rows if row.get("state") == "ambiguous"]
        missing = [row for row in rows if row.get("state") == "missing"]
        offsets = {(row.get("offset"), row.get("offset_int"), row.get("width")) for row in present}

        label = f"{scope}.{slot}"
        if present and not ambiguous and not missing and len(offsets) == 1:
            offset, offset_int, width = next(iter(offsets))
            decimal = f"{offset_int}" if isinstance(offset_int, int) else "?"
            lines.append(
                f"- `{label}` looks stable: `{offset}` / {width}, observed present {len(present)}/{total}. Candidate exact `Offset={decimal}`, `Width={width}`."
            )
        elif ambiguous:
            lines.append(
                f"- `{label}` is ambiguous in {len(ambiguous)}/{total} events. Tighten scan filters or switch to a confirmed exact offset before trusting formulas."
            )
        elif missing and not present:
            lines.append(
                f"- `{label}` was missing in {len(missing)}/{total} events. The scan range/filter may not match this unit state."
            )
        else:
            rendered_offsets = ", ".join(f"{offset}/{width}" for offset, _, width in sorted(offsets))
            lines.append(
                f"- `{label}` is mixed: present {len(present)}/{total}, missing {len(missing)}, ambiguous {len(ambiguous)}, offsets {rendered_offsets or 'none'}."
            )

    lines.append("")
    return lines


def render_response_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    responses: Counter[tuple[str, str, str, str, str]] = Counter()
    finals: Counter[tuple[str, str]] = Counter()
    for ctx in runtime_details:
        response = ctx.get("response", {})
        if isinstance(response, dict):
            responses[
                (
                    str(response.get("raw", "")),
                    str(response.get("permille", "")),
                    str(response.get("rules", "")),
                    str(response.get("clamped", "")),
                    str(response.get("rule", "")),
                )
            ] += 1
        final = ctx.get("final", {})
        if isinstance(final, dict):
            finals[(str(final.get("value", "")), str(final.get("rule", "")))] += 1

    lines = ["### Responses"]
    if not responses:
        lines.append("No response data parsed.")
        lines.append("")
        return lines

    lines.append("| Raw | Permille | Rules | Clamped | Rule | Count |")
    lines.append("| ---: | ---: | ---: | ---: | --- | ---: |")
    for (raw, permille, rules, clamped, rule), count in responses.most_common(20):
        lines.append(f"| {raw} | {permille} | {rules} | {clamped} | {rule or '?'} | {count} |")
    lines.append("")

    lines.append("### Final Damage")
    lines.append("| Final | Rule | Count |")
    lines.append("| ---: | --- | ---: |")
    for (value, rule), count in finals.most_common(20):
        lines.append(f"| {value} | {rule or '?'} | {count} |")
    lines.append("")
    return lines


def render_dr_response_proof_summary(runtime_details: list[dict[str, object]]) -> list[str]:
    lines = ["### DR/Response Proof Check"]
    if not runtime_details:
        lines.append("No parsed runtime context to evaluate.")
        lines.append("")
        return lines

    target_slot_contexts = [
        ctx for ctx in runtime_details
        if any(slot.get("state") == "present" for slot in slot_observations("target", ctx.get("target_slots", [])))
    ]
    attacker_slot_contexts = [
        ctx for ctx in runtime_details
        if any(slot.get("state") == "present" for slot in slot_observations("attacker", ctx.get("attacker_slots", [])))
    ]
    ambiguous_slot_contexts = [
        ctx for ctx in runtime_details
        if any(
            slot.get("state") == "ambiguous"
            for slot in slot_observations("target", ctx.get("target_slots", []))
            + slot_observations("attacker", ctx.get("attacker_slots", []))
        )
    ]
    equipment_dr_contexts = [
        ctx for ctx in runtime_details
        if runtime_equipment_dr_value(ctx.get("equipment_dr", "")) > 0
    ]
    response_contexts = [
        ctx for ctx in runtime_details
        if isinstance(ctx.get("response"), dict) and int(ctx["response"].get("rules", 0) or 0) > 0
    ]
    non_neutral_response_contexts = [
        ctx for ctx in response_contexts
        if isinstance(ctx.get("response"), dict) and int(ctx["response"].get("permille", 1000) or 1000) != 1000
    ]
    final_response_contexts = [
        ctx for ctx in runtime_details
        if isinstance(ctx.get("final"), dict) and "DamageResponse" in str(ctx["final"].get("rule", ""))
    ]
    trace_contexts = [
        ctx for ctx in runtime_details
        if isinstance(ctx.get("trace_vars"), dict) and bool(ctx.get("trace_vars"))
    ]
    final_trace_contexts = [
        ctx for ctx in runtime_details
        if has_final_trace_variable(ctx.get("trace_vars", {}))
    ]

    lines.append(f"- Runtime contexts: {len(runtime_details)}")
    lines.append(f"- Target slot present contexts: {len(target_slot_contexts)}")
    lines.append(f"- Attacker slot present contexts: {len(attacker_slot_contexts)}")
    lines.append(f"- Ambiguous slot contexts: {len(ambiguous_slot_contexts)}")
    lines.append(f"- Positive equipment DR contexts: {len(equipment_dr_contexts)}")
    lines.append(f"- Response-rule contexts: {len(response_contexts)}")
    lines.append(f"- Non-neutral response contexts (`permille != 1000`): {len(non_neutral_response_contexts)}")
    lines.append(f"- Final rule includes `DamageResponse`: {len(final_response_contexts)}")
    lines.append(f"- Formula trace contexts: {len(trace_contexts)}")
    lines.append(f"- Final trace contexts (`result.final`/`trace.finaldamage`): {len(final_trace_contexts)}")
    lines.append(
        f"- Verdict: **{dr_response_proof_verdict(runtime_details, target_slot_contexts, attacker_slot_contexts, ambiguous_slot_contexts, equipment_dr_contexts, response_contexts, final_response_contexts, final_trace_contexts)}**"
    )
    lines.append("")
    return lines


def runtime_equipment_dr_value(value: object) -> int:
    raw, _, _ = str(value or "").partition(":")
    parsed = parse_number(raw)
    return parsed if parsed is not None else 0


def has_final_trace_variable(trace_vars: object) -> bool:
    if not isinstance(trace_vars, dict):
        return False
    normalized = {str(name).strip().lower() for name in trace_vars}
    return bool(normalized & {"result.final", "trace.finaldamage", "trace.final_damage"})


def dr_response_proof_verdict(
    runtime_details: list[dict[str, object]],
    target_slot_contexts: list[dict[str, object]],
    attacker_slot_contexts: list[dict[str, object]],
    ambiguous_slot_contexts: list[dict[str, object]],
    equipment_dr_contexts: list[dict[str, object]],
    response_contexts: list[dict[str, object]],
    final_response_contexts: list[dict[str, object]],
    final_trace_contexts: list[dict[str, object]],
) -> str:
    if not runtime_details:
        return "no runtime context evidence"
    if ambiguous_slot_contexts:
        return "attention: slot scan ambiguity present; tighten offsets before trusting DR/response"
    if equipment_dr_contexts and response_contexts and final_response_contexts:
        return "pass-candidate: equipment DR and damage response both resolved in runtime context"
    if equipment_dr_contexts and target_slot_contexts:
        return "dr-candidate: target slot and positive equipment DR resolved"
    if response_contexts and final_response_contexts:
        return "response-candidate: response rules affected final damage"
    if response_contexts:
        return "response-context candidate: response rule resolved, but final rule linkage needs review"
    if target_slot_contexts or attacker_slot_contexts:
        return "slot-mapping candidate: slots resolved, but no positive DR/response evidence yet"
    if final_trace_contexts:
        return "formula-trace candidate: formulas traced, but no slot/DR/response evidence yet"
    return "runtime context present, but no DR/response proof evidence"


def build_warnings(
    headers: list[str],
    units: dict[str, dict[str, str]],
    runtime_contexts: list[tuple[str, str]],
    old_unit_lines: int,
) -> list[str]:
    warnings: list[str] = []
    header_text = "\n".join(headers)

    if "Battle Runtime Harness" not in header_text and headers:
        warnings.append(
            "Log header does not look like the current Battle Runtime Harness. Restart the game through Reloaded-II after building the current DLL."
        )
    if old_unit_lines and not units:
        warnings.append(
            f"Found {old_unit_lines} old-format `[UNIT id=...]` lines and no pointer-keyed `[UNIT ptr=...]` lines; this is a stale/old harness log."
        )
    if units and not runtime_contexts:
        warnings.append(
            "No `[RUNTIME]` lines found. For mapping, install a settings file with `RewriteObservedDamage=true` and `LogResolvedRuntimeContext=true`, then create at least one damage event."
        )

    return warnings


if __name__ == "__main__":
    raise SystemExit(main())
