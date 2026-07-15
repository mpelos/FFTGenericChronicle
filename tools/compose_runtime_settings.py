#!/usr/bin/env python3
"""Strictly compose runtime-setting fragments without silent last-writer wins."""
from __future__ import annotations

import argparse
import copy
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]


class CompositionError(ValueError):
    pass


@dataclass(frozen=True)
class Conflict:
    path: str
    incoming_source: str

    @property
    def root(self) -> str:
        return self.path.split(".", 1)[0].split("[", 1)[0]


def _display(path: str, key: str) -> str:
    return f"{path}.{key}" if path else key


def _named_list(value: list[Any]) -> bool:
    return bool(value) and all(
        isinstance(item, dict) and isinstance(item.get("Name"), str) and item["Name"]
        for item in value
    )


def _index_named(value: list[dict[str, Any]], path: str) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for item in value:
        name = item["Name"]
        if name in result:
            raise CompositionError(f"duplicate Name {name!r} at {path}")
        result[name] = item
    return result


def _merge(
    current: Any,
    incoming: Any,
    path: str,
    incoming_source: str,
    conflicts: list[Conflict],
) -> Any:
    if current == incoming:
        return copy.deepcopy(current)

    if isinstance(current, dict) and isinstance(incoming, dict):
        merged = copy.deepcopy(current)
        for key, value in incoming.items():
            child = _display(path, key)
            if key not in merged:
                merged[key] = copy.deepcopy(value)
            else:
                merged[key] = _merge(merged[key], value, child, incoming_source, conflicts)
        return merged

    if isinstance(current, list) and isinstance(incoming, list):
        if _named_list(current) and _named_list(incoming):
            merged = copy.deepcopy(current)
            by_name = _index_named(merged, path)
            for item in incoming:
                name = item["Name"]
                if name not in by_name:
                    clone = copy.deepcopy(item)
                    merged.append(clone)
                    by_name[name] = clone
                    continue
                item_path = f"{path}[Name={name}]"
                replacement = _merge(by_name[name], item, item_path, incoming_source, conflicts)
                index = next(i for i, candidate in enumerate(merged) if candidate["Name"] == name)
                merged[index] = replacement
                by_name[name] = replacement
            return merged

    conflicts.append(Conflict(path=path, incoming_source=incoming_source))
    return copy.deepcopy(current)


def _load_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CompositionError(f"cannot read {label} {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise CompositionError(f"{label} must contain a JSON object: {path}")
    return value


def _repo_path(raw: str, label: str) -> Path:
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as exc:
        raise CompositionError(f"{label} escapes the repository: {raw}") from exc
    return path


def compose_manifest(manifest_path: Path) -> tuple[dict[str, Any], Path, list[Conflict]]:
    manifest = _load_object(manifest_path, "manifest")
    inputs = manifest.get("inputs")
    resolutions = manifest.get("resolutions", {})
    patch = manifest.get("patch", {})
    output_raw = manifest.get("output")
    note = manifest.get("note")

    if not isinstance(inputs, list) or len(inputs) < 2 or not all(isinstance(x, str) for x in inputs):
        raise CompositionError("manifest inputs must contain at least two repository-relative paths")
    if not isinstance(resolutions, dict) or not isinstance(patch, dict):
        raise CompositionError("manifest resolutions and patch must be JSON objects")
    if not isinstance(output_raw, str) or not output_raw:
        raise CompositionError("manifest output must be a repository-relative path")
    if not isinstance(note, str) or not note.strip():
        raise CompositionError("manifest note must be a non-empty string")

    merged: dict[str, Any] = {}
    conflicts: list[Conflict] = []
    for raw in inputs:
        source_path = _repo_path(raw, "input")
        source = _load_object(source_path, "input")
        source.pop("_note", None)
        merged = _merge(merged, source, "", raw, conflicts)

    conflict_roots = {conflict.root for conflict in conflicts}
    resolution_roots = set(resolutions)
    unresolved = sorted(conflict_roots - resolution_roots)
    unused = sorted(resolution_roots - conflict_roots)
    if unresolved:
        details = ", ".join(
            f"{conflict.path} ({conflict.incoming_source})"
            for conflict in conflicts
            if conflict.root in unresolved
        )
        raise CompositionError(f"unresolved conflicts: {details}")
    if unused:
        raise CompositionError(f"resolutions without a matching conflict: {', '.join(unused)}")

    for key, value in resolutions.items():
        merged[key] = copy.deepcopy(value)
    for key, value in patch.items():
        merged[key] = copy.deepcopy(value)

    result = {"_note": note.strip()}
    result.update(merged)
    output = _repo_path(output_raw, "output")
    if output in {_repo_path(raw, "input") for raw in inputs}:
        raise CompositionError("manifest output cannot overwrite an input")
    return result, output, conflicts


def _render(value: dict[str, Any]) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False) + "\n"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = args.manifest.resolve()
    try:
        result, output, conflicts = compose_manifest(manifest_path)
        rendered = _render(result)
        if args.check_only:
            if not output.exists():
                raise CompositionError(f"composed output is missing: {output}")
            if output.read_text(encoding="utf-8-sig") != rendered:
                raise CompositionError(f"composed output is stale: {output}")
            print(f"runtime settings composition is current: {output} ({len(conflicts)} resolved conflict(s))")
            return 0
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(rendered, encoding="utf-8", newline="\n")
        print(f"wrote {output} ({len(conflicts)} resolved conflict(s))")
        return 0
    except CompositionError as exc:
        print(f"ERROR: {exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
