#!/usr/bin/env python3
"""Integration and negative tests for the Enhanced autosave CT-order fixture builder."""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

import build_fft_autosave_ct_fixture as builder
import build_fft_autosave_reaction_fixture as savefmt
import build_fft_manual_ability_fixture as manualfmt


REPO = Path(__file__).resolve().parents[1]
SOURCE = REPO / "work" / "1784157011-synthetic-reaction-carrier443-consistent-fixture.png"
CHOCO = "Choco:8200FF5E03502003:77:100"
WENYLD = "Wenyld:8106FF4D03504003:84:0"
CHOCO_ATTACK = "attack:ChocoAttack:8200FF5E03502003:0:100"


def command(output: Path, prefix: str, choco: str = CHOCO) -> list[str]:
    return [
        sys.executable,
        str(REPO / "tools" / "build_fft_autosave_ct_fixture.py"),
        "--prefix",
        prefix,
        "--source-save",
        str(SOURCE),
        "--label",
        "ct-order-test",
        "--output-dir",
        str(output),
        "--edit",
        choco,
        "--edit",
        WENYLD,
        "--edit",
        CHOCO_ATTACK,
    ]


def main() -> int:
    snapshot = builder.parse_edit("snapshot:unit-snapshot:0011223344556677:20:0")
    turn = builder.parse_edit("turn:unit-turn:0011223344556677:90:0")
    assert snapshot.scope == "snapshot"
    assert builder.SCOPE_COMPONENTS[snapshot.scope] == ("resume_en00_main.sav",)
    assert turn.scope == "turn"
    assert builder.SCOPE_COMPONENTS[turn.scope] == (
        "resume_en00_fturn.sav",
        "resume_enbtl_main.sav",
    )

    source_hash = manualfmt.sha256(SOURCE)
    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_ct_test_") as tmp:
        root = Path(tmp)
        good = root / "good"
        good.mkdir()
        subprocess.run(command(good, "1784170990"), check=True)
        fixture = good / "1784170990-ct-order-test-fixture.png"
        manifest = good / "1784170990-ct-order-test-fixture-manifest.md"
        assert fixture.is_file() and manifest.is_file()
        text = manifest.read_text(encoding="utf-8")
        assert "CT `77` -> `100`" in text
        assert "CT `84` -> `0`" in text

        with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_ct_unpack_") as unpacked:
            files = savefmt.unpack_save(manualfmt.DEFAULT_FF16TOOLS, fixture, Path(unpacked))
            for name in builder.MAIN_LIVE_COMPONENTS:
                data = files[name]
                choco_offset = builder.signature_hits(data, bytes.fromhex("8200FF5E03502003"))
                wenyld_offset = builder.signature_hits(data, bytes.fromhex("8106FF4D03504003"))
                assert len(choco_offset) == len(wenyld_offset) == 1
                assert data[choco_offset[0] + builder.CT_OFFSET] == 100
                assert data[wenyld_offset[0] + builder.CT_OFFSET] == 0
            for name in builder.ATTACK_LIVE_COMPONENTS:
                data = files[name]
                choco_offset = builder.signature_hits(data, bytes.fromhex("8200FF5E03502003"))
                assert len(choco_offset) == 1
                assert data[choco_offset[0] + builder.CT_OFFSET] == 100

        bad = root / "bad"
        bad.mkdir()
        result = subprocess.run(
            command(bad, "1784170991", "Choco:8200FF5E03502003:76:100"),
            check=False,
            capture_output=True,
            text=True,
        )
        assert result.returncode != 0
        assert "CT mismatch" in result.stderr
        assert not list(bad.iterdir())

    assert manualfmt.sha256(SOURCE) == source_hash
    print("autosave CT-order fixture tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
