#!/usr/bin/env python3
"""Tests for the Enhanced autosave status-clear fixture builder."""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

import build_fft_autosave_ct_fixture as fixturefmt
import build_fft_autosave_reaction_fixture as savefmt
import build_fft_manual_ability_fixture as manualfmt


REPO = Path(__file__).resolve().parents[1]
SOURCE = REPO / "work" / "1784171803-dcl-dual-wield-fast-attack-ct-order-fixture.png"


def command(output: Path, prefix: str, mask: str = "0x10") -> list[str]:
    return [
        sys.executable,
        str(REPO / "tools" / "build_fft_autosave_status_clear_fixture.py"),
        "--prefix", prefix,
        "--source-save", str(SOURCE),
        "--label", "status-clear-test",
        "--output-dir", str(output),
        "--clear", f"RionInvisible:8010035900089003:2:{mask}",
    ]


def main() -> int:
    source_hash = manualfmt.sha256(SOURCE)
    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_status_test_") as tmp:
        root = Path(tmp)
        good = root / "good"
        good.mkdir()
        subprocess.run(command(good, "1784172300"), check=True)
        fixture = good / "1784172300-status-clear-test-fixture.png"
        manifest = good / "1784172300-status-clear-test-fixture-manifest.md"
        assert fixture.is_file() and manifest.is_file()
        assert "status byte `2`, mask `0x10`" in manifest.read_text(encoding="utf-8")

        with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_status_unpack_") as unpacked:
            files = savefmt.unpack_save(manualfmt.DEFAULT_FF16TOOLS, fixture, Path(unpacked))
            for name in fixturefmt.MAIN_LIVE_COMPONENTS:
                data = files[name]
                hits = fixturefmt.signature_hits(data, bytes.fromhex("8010035900089003"))
                assert len(hits) == 1
                record = hits[0]
                assert data[record + 0x63] == 0x20
                assert data[record + 0x1F1] == 0x00
                assert data[record + 0x59] == 0x20

        bad = root / "bad"
        bad.mkdir()
        result = subprocess.run(command(bad, "1784172301", "0x08"), check=False, capture_output=True, text=True)
        assert result.returncode != 0
        assert "is not present" in result.stderr
        assert not list(bad.iterdir())

    assert manualfmt.sha256(SOURCE) == source_hash
    print("autosave status-clear fixture tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
