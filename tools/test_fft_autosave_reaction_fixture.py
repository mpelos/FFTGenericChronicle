#!/usr/bin/env python3
"""Integration and negative tests for the Enhanced autosave Reaction fixture builder."""
from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

import build_fft_autosave_reaction_fixture as builder
import build_fft_manual_ability_fixture as savefmt


REPO = Path(__file__).resolve().parents[1]
SOURCE = REPO / "work" / "1784104894-fft-autoenhanced-snapshot.png"


def command(output: Path, prefix: str, expected: int) -> list[str]:
    return [
        sys.executable,
        str(builder.__file__),
        "--prefix",
        prefix,
        "--source-save",
        str(SOURCE),
        "--expected-reaction-id",
        str(expected),
        "--reaction-id",
        "443",
        "--allow-unlearned-reaction",
        "--output-dir",
        str(output),
        "--label",
        "autosave-reaction-test",
    ]


def main() -> int:
    for required in (SOURCE, savefmt.DEFAULT_FF16TOOLS, builder.DEFAULT_ABILITY_BASELINE):
        assert required.is_file(), required
    source_hash = savefmt.sha256(SOURCE)

    assert builder.reaction_set_bit(422) == (0x94, 0x80)
    assert builder.reaction_set_bit(442) == (0x96, 0x08)
    assert builder.reaction_set_bit(443) == (0x96, 0x04)
    assert builder.reaction_set_bit(453) == (0x97, 0x01)
    for invalid in (421, 454):
        try:
            builder.reaction_set_bit(invalid)
        except ValueError:
            pass
        else:
            raise AssertionError(f"Reaction bit lookup accepted invalid id {invalid}")

    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_reaction_test_") as tmp:
        root = Path(tmp)
        good = root / "good"
        good.mkdir()
        subprocess.run(command(good, "1784113991", 442), check=True)
        fixture = good / "1784113991-autosave-reaction-test-fixture.png"
        manifest = good / "1784113991-autosave-reaction-test-fixture-manifest.md"
        assert fixture.is_file() and fixture.stat().st_size > 0
        assert manifest.is_file()
        manifest_text = manifest.read_text(encoding="utf-8")
        assert "`442` (Counter) -> `443`" in manifest_text
        assert "`unit+0x96 & 0x08` -> `unit+0x96 & 0x04`" in manifest_text
        assert "Stale `en01`, `en02`, and `enma` members remain byte-identical" in manifest_text

        source_dir = root / "source"
        fixture_dir = root / "fixture"
        source_dir.mkdir()
        fixture_dir.mkdir()
        source_files = builder.unpack_save(savefmt.DEFAULT_FF16TOOLS, SOURCE, source_dir)
        fixture_files = builder.unpack_save(savefmt.DEFAULT_FF16TOOLS, fixture, fixture_dir)
        assert set(source_files) == set(fixture_files)

        identity = builder.Identity("Rion", 0x80, 0x59, 3, 357, 71, 97, 277)
        _, targets, _ = builder.stage_current_components(source_files, identity, 442, 443)
        audits = builder.audit_roundtrip(source_files, fixture_files, targets, 442, 443)
        changed_names = {audit.name for audit in audits if audit.changed_offsets}
        assert changed_names == set(builder.CURRENT_COMPONENTS), changed_names
        for name, component_targets in targets.items():
            for target in component_targets:
                if target.kind != "live":
                    continue
                source_bits = source_files[name][
                    target.record_offset + builder.LIVE_REACTION_SET_OFFSET :
                    target.record_offset + builder.LIVE_REACTION_SET_OFFSET + builder.LIVE_REACTION_SET_SIZE
                ]
                fixture_bits = fixture_files[name][
                    target.record_offset + builder.LIVE_REACTION_SET_OFFSET :
                    target.record_offset + builder.LIVE_REACTION_SET_OFFSET + builder.LIVE_REACTION_SET_SIZE
                ]
                assert source_bits == bytes.fromhex("00000800")
                assert fixture_bits == bytes.fromhex("00000400")

        inconsistent = dict(source_files)
        broken_name = "resume_en00_main.sav"
        broken_targets = builder.find_live_targets(inconsistent[broken_name], identity, 442)
        assert broken_targets
        broken = bytearray(inconsistent[broken_name])
        relative, mask = builder.reaction_set_bit(442)
        broken[broken_targets[0].record_offset + relative] &= ~mask
        inconsistent[broken_name] = bytes(broken)
        try:
            builder.stage_current_components(inconsistent, identity, 442, 443)
        except AssertionError as error:
            assert "does not carry Reaction 442 in its reaction-set bitfield" in str(error)
        else:
            raise AssertionError("fixture builder accepted an inconsistent source reaction-set")

        bad = root / "bad"
        bad.mkdir()
        result = subprocess.run(
            command(bad, "1784113992", 441),
            check=False,
            capture_output=True,
            text=True,
        )
        assert result.returncode != 0
        assert "expected exactly one roster owner" in result.stderr
        assert not list(bad.iterdir())

    assert savefmt.sha256(SOURCE) == source_hash
    print("autosave equipped-Reaction fixture tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
