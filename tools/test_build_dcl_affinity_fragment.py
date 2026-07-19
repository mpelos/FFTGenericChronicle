#!/usr/bin/env python3
"""Smoke tests for canonical job/equipment elemental-affinity generation."""
from __future__ import annotations

import tempfile
from pathlib import Path

from build_dcl_affinity_fragment import build


JOB_XML = """\
<JobTable><Entries>
  <Job><Id>82</Id><AbsorbElements>None</AbsorbElements><NullifyElements>None</NullifyElements><HalveElements>None</HalveElements><WeakElements>None</WeakElements></Job>
  <Job><Id>100</Id><AbsorbElements>Fire</AbsorbElements><NullifyElements>Earth</NullifyElements><HalveElements>Ice</HalveElements><WeakElements>Water</WeakElements></Job>
</Entries></JobTable>
"""


def main() -> int:
    with tempfile.TemporaryDirectory() as raw:
        path = Path(raw) / "JobData.xml"
        path.write_text(JOB_XML, encoding="utf-8")
        fragment = build(path)

    maps = fragment["FormulaMaps"]
    assert maps["dclJobAbsorbFire"] == {"100": 1}
    assert maps["dclJobNullEarth"] == {"100": 1}
    assert maps["dclJobHalveIce"] == {"100": 1}
    assert maps["dclJobWeakWater"] == {"100": 1}
    assert "dclJobAbsorbLightning" not in maps

    formulas = {row["Name"]: row["Formula"] for row in fragment["DclDerivedVariables"]}
    null_formula = formulas["dcl.targetNullResolved"]
    assert "mapOr(dclJobNullFire, t.jobId, 0)" in null_formula
    assert "tslot.head.nullify_fire" in null_formula
    assert "tslot.leftshield.nullify_water" in null_formula
    assert "t.element.null.fire" not in null_formula
    assert "t.status.oil" in formulas["dcl.targetWeakResolved"]
    assert "dcl.elementPermilleResolved" in formulas["dcl.magicStackPermilleResolved"]

    print("DCL affinity fragment tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
