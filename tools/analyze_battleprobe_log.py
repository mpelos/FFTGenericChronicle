#!/usr/bin/env python3
"""
Summarize Generic Chronicle battleprobe_log.txt.

The current harness emits pointer-keyed unit lines plus [CANDIDATES], [DIFF], [DAMAGE],
[HEALING], [CTX], [RUNTIME], and [REWRITE] records. This script turns that noisy log into a compact report that is
easier to use when mapping equipment/status/action fields.

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

UNIT_RE = re.compile(
    r"\[UNIT ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2}) (?P<faction>ally|foe ) t(?P<team>\d+)\] (?P<stats>.+)"
)
OLD_UNIT_RE = re.compile(r"\[UNIT id=(0x[0-9A-F]{2}) (?P<faction>ally|foe ) t(?P<team>\d+)\] (?P<stats>.+)")
CAND_RE = re.compile(r"\[CANDIDATES ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
DUMP_RE = re.compile(r"\[DUMP ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
DIFF_RE = re.compile(r"\[DIFF ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
HP_EVENT_RE = re.compile(r"\[(?P<kind>DAMAGE|HEALING) ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<prev>\d+) -> (?P<hp>\d+) = (?P<amount>\d+)")
CTX_RE = re.compile(r"\[CTX ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
RUNTIME_RE = re.compile(r"\[RUNTIME ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
REWRITE_RE = re.compile(r"\[REWRITE ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2})\] (?P<body>.+)")
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
    hp_events: list[tuple[str, str, str, int]] = []
    contexts: list[tuple[str, str]] = []
    runtime_contexts: list[tuple[str, str]] = []
    rewrites: list[tuple[str, str]] = []
    memory_table_events: list[dict[str, object]] = []
    headers: list[str] = []
    old_unit_lines = 0
    item_catalog = {} if args.no_catalog else load_item_catalog(args.catalog)

    for line in args.log.read_text(encoding="utf-8", errors="replace").splitlines():
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

        if m := HP_EVENT_RE.search(line):
            hp_events.append((m.group("kind"), m.group(2), m.group(3), int(m.group("amount"))))
            continue

        if m := CTX_RE.search(line):
            contexts.append((m.group(1), m.group("body")))
            continue

        if m := RUNTIME_RE.search(line):
            runtime_contexts.append((m.group(1), m.group("body")))
            continue

        if m := REWRITE_RE.search(line):
            rewrites.append((m.group(1), m.group("body")))

    args.output.parent.mkdir(exist_ok=True)
    args.output.write_text(
        render(args.log, headers, units, candidates, dumps, diffs, diff_offsets, hp_events, contexts, runtime_contexts, rewrites, memory_table_events, old_unit_lines, item_catalog, args.catalog),
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


def parse_action(text: str) -> dict[str, str]:
    if not text or text == "none":
        return {"name": "none", "source": "", "signal": "", "vars": ""}

    m = re.match(r"(?P<name>.*?):source=(?P<source>.*?):signal=(?P<signal>-?\d+)(?::vars=(?P<vars>.*))?$", text)
    if not m:
        return {"name": text, "source": "", "signal": "", "vars": ""}

    return {
        "name": m.group("name"),
        "source": m.group("source"),
        "signal": m.group("signal"),
        "vars": m.group("vars") or "",
    }


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
    hp_events: list[tuple[str, str, str, int]],
    contexts: list[tuple[str, str]],
    runtime_contexts: list[tuple[str, str]],
    rewrites: list[tuple[str, str]],
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

    lines.append("## HP Events / Rewrite")
    damage = [event for event in hp_events if event[0] == "DAMAGE"]
    healing = [event for event in hp_events if event[0] == "HEALING"]
    lines.append(f"- Damage events: {len(damage)}")
    lines.append(f"- Healing events: {len(healing)}")
    if damage:
        dmg_counts = Counter(ptr for _, ptr, _, _ in damage)
        lines.append("- Damage by unit: " + ", ".join(f"`{ptr}`={count}" for ptr, count in dmg_counts.items()))
    if healing:
        heal_counts = Counter(ptr for _, ptr, _, _ in healing)
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
    for ptr, body in rewrites[:20]:
        lines.append(f"  - `{ptr}` {body}")
    lines.append("")

    return "\n".join(lines)


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
    lines.extend(render_slot_summary(runtime_details))
    lines.extend(render_response_summary(runtime_details))
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
