#!/usr/bin/env python3
"""Smoke tests for strict runtime-settings composition."""
from __future__ import annotations

import json
import tempfile
from pathlib import Path

from compose_runtime_settings import CompositionError, compose_manifest


def write(path: Path, value: object) -> None:
    path.write_text(json.dumps(value), encoding="utf-8")


def main() -> int:
    with tempfile.TemporaryDirectory(dir=Path.cwd()) as raw:
        temp = Path(raw)
        left = temp / "left.json"
        right = temp / "right.json"
        manifest = temp / "manifest.json"
        output = temp / "out.json"
        left_rel = left.relative_to(Path.cwd()).as_posix()
        right_rel = right.relative_to(Path.cwd()).as_posix()
        output_rel = output.relative_to(Path.cwd()).as_posix()

        write(left, {"Shared": 1, "FormulaVariables": {"a": 1}, "Rules": [{"Name": "A", "Value": 1}]})
        write(right, {"Shared": 2, "FormulaVariables": {"b": 2}, "Rules": [{"Name": "B", "Value": 2}]})
        write(manifest, {"inputs": [left_rel, right_rel], "output": output_rel, "note": "fixture"})
        try:
            compose_manifest(manifest)
        except CompositionError as exc:
            assert "unresolved conflicts: Shared" in str(exc)
        else:
            raise AssertionError("conflicting scalar was silently accepted")

        write(
            manifest,
            {
                "inputs": [left_rel, right_rel],
                "resolutions": {"Shared": 3},
                "patch": {"ProbeEnabled": False},
                "remove": ["FormulaVariables"],
                "output": output_rel,
                "note": "fixture",
            },
        )
        result, resolved_output, conflicts = compose_manifest(manifest)
        assert resolved_output == output
        assert [conflict.path for conflict in conflicts] == ["Shared"]
        assert result["Shared"] == 3
        assert "FormulaVariables" not in result
        assert [rule["Name"] for rule in result["Rules"]] == ["A", "B"]
        assert result["ProbeEnabled"] is False

        write(
            manifest,
            {
                "inputs": [left_rel, right_rel],
                "resolutions": {"Shared": 3, "Unused": 1},
                "output": output_rel,
                "note": "fixture",
            },
        )
        try:
            compose_manifest(manifest)
        except CompositionError as exc:
            assert "resolutions without a matching conflict: Unused" in str(exc)
        else:
            raise AssertionError("unused resolution was silently accepted")

        write(
            manifest,
            {
                "inputs": [left_rel, right_rel],
                "resolutions": {"Shared": 3},
                "remove": ["Missing"],
                "output": output_rel,
                "note": "fixture",
            },
        )
        try:
            compose_manifest(manifest)
        except CompositionError as exc:
            assert "remove keys are missing from composed settings: Missing" in str(exc)
        else:
            raise AssertionError("missing removal was silently accepted")

    print("runtime settings composition smoke tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
