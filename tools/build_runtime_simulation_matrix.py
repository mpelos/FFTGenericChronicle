#!/usr/bin/env python3
"""Build an offline runtime-simulation scenario matrix for the v0.2 policy.

The generated bundle intentionally uses catalog item ids that exercise the runtime bridge:
attacker weapon category -> action signal -> armor class -> response rule -> final HP rewrite.
Use the C# settings simulator to attach expectations after this file is generated.
"""
from __future__ import annotations

import argparse
import json
from copy import deepcopy
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUTPUT = ROOT / "docs" / "modding" / "examples" / "runtime-simulation-matrix.v0.2.example.json"

ATTACKS = [
    ("swing-sword", 19, "Broadsword"),
    ("thrust-spear", 99, "Javelin"),
    ("crush-staff", 59, "Oak Staff"),
    ("missile-crossbow", 77, "Bowgun"),
    ("crush-fists", 0, "Nothing Equipped"),
]

ARMORS = [
    ("plate", 177, "Plate Mail"),
    ("mail", 175, "Chainmail"),
    ("leather", 172, "Leather Armor"),
    ("cloth", 200, "Hempen Robe"),
]

TARGET_BASE = {
    "ptr": "0x2000",
    "charId": 128,
    "level": 12,
    "hp": 280,
    "maxHp": 300,
    "team": 2,
    "isFoe": True,
    "pa": 10,
    "ma": 8,
    "speed": 7,
    "move": 4,
    "jump": 3,
    "brave": 70,
    "faith": 60,
    "raw": {},
}

ATTACKER_BASE = {
    "ptr": "0x1000",
    "charId": 1,
    "level": 14,
    "hp": 40,
    "maxHp": 40,
    "team": 1,
    "isFoe": False,
    "pa": 12,
    "ma": 7,
    "speed": 8,
    "move": 5,
    "jump": 4,
    "brave": 75,
    "faith": 65,
    "raw": {},
}


def build_scenarios() -> list[dict[str, object]]:
    scenarios: list[dict[str, object]] = []
    event_index = 100
    for attack_name, weapon_id, weapon_name in ATTACKS:
        for armor_name, armor_id, armor_item_name in ARMORS:
            target = deepcopy(TARGET_BASE)
            target["raw"] = {"0x70": armor_id}
            attacker = deepcopy(ATTACKER_BASE)
            attacker["raw"] = {"0x50": weapon_id}
            scenarios.append(
                {
                    "name": f"matrix-{attack_name}-vs-{armor_name}",
                    "_note": f"{weapon_name} vs {armor_item_name}",
                    "previousHp": 300,
                    "currentHp": 280,
                    "vanillaDamage": 20,
                    "eventIndex": event_index,
                    "eventSeed": 20000 + event_index,
                    "target": target,
                    "attacker": attacker,
                }
            )
            event_index += 1
    return scenarios


def main() -> int:
    parser = argparse.ArgumentParser(description="Build runtime simulation matrix scenarios.")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    bundle = {"scenarios": build_scenarios()}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {args.output}")
    print(f"scenarios={len(bundle['scenarios'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
