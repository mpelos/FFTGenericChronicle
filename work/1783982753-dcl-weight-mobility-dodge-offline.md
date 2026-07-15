# DCL Weight → mobility / Dodge — offline checkpoint

## Result

The current formula runtime can implement the data and combat-formula side of Weight without a new
catalog field or native hook:

```text
item id -> authored Weight map
sum all seven equipment words -> total Weight
total Weight -> coarse Move penalty band
total Weight -> fine Dodge penalty
C-Ev + accessory evade - Weight penalty -> Dodge
Brave modifier -> shared Dodge/Parry/Block modifier
```

The mechanism is integrated into
`work/1783980809-battle-runtime-settings.dcl-weapon-skill-mechanism.json` and the physical-contest
smoke suite. `DclDodgeFormula` now consumes the derived value instead of the old constant `8`.

## Why Weight is an authored map

IVC has no native item Weight field. The runtime already exposes every equipped item id and supports
sparse integer maps, so `FormulaMaps.equipmentWeightByItemId` is the data-first implementation
surface. No item SKU or engine table is invented. The full shipping map still must be authored for
all 261 catalog rows.

The sum reads the canonical equipment words:

| Slot | Unit offset |
| --- | ---: |
| Head | `+0x1A` |
| Body | `+0x1C` |
| Accessory | `+0x1E` |
| Right weapon | `+0x20` |
| Right shield | `+0x22` |
| Left weapon | `+0x24` |
| Left shield | `+0x26` |

Unknown, empty, and sentinel item ids fall back to zero Weight.

## Mechanism fixtures

The map currently contains only five routing fixtures:

| Item | Weight |
| --- | ---: |
| Ninja Blade (11) | 3 |
| Romandan Pistol (71) | 6 |
| Bowgun (77) | 4 |
| Venetian Shield (142) | 5 |
| Leather Armor (172) | 26 |

Leather Armor is an `Armor` record and therefore occupies the DCL's heavy class even though it is an
early low-HP item. This preserves the current three-class DCL and rejects LT7's obsolete HP-threshold
classification.

Move bands use the illustrative curve already present in the DCL design:

```text
0..14 => -0 Move
15..28 => -1 Move
29..40 => -2 Move
41+ => -3 Move
```

The Dodge fixture is deliberately simple and configurable:

```text
Dodge = max(3,
    7
  + floor(C-Ev percent / 10)
  + floor(accessory physical evade / 10)
  - floor(total Weight / 10))

shared defense modifier = trunc((50 - Brave) / 20)
```

Speed and PA are absent by construction. The Brave term is applied after the runtime chooses the
best available Dodge/Parry/Block, so it affects all three once and cannot double-dip Dodge.

Every constant above is a mechanism fixture. Exact C-Ev conversion, Weight-to-Dodge slope, floor,
and Brave band remain calibration decisions owned by the DCL open questions.

## Offline assertions

With C-Ev 10 and no accessory evade:

| Loadout | Weight | Move penalty | Dodge before Brave |
| --- | ---: | ---: | ---: |
| no mapped gear | 0 | 0 | 8 |
| Leather Armor | 26 | 1 | 6 |
| Leather Armor + Venetian Shield | 31 | 2 | 5 |

Brave 70 produces a shared `-1` active-defense modifier. The test proves it is separate from the
Weight/C-Ev Dodge calculation.

Validation passed:

- build `WeightDodgeAudit`: 0 warnings, 0 errors;
- complete formula runtime smoke suite: passed;
- profile JSON parse: passed;
- settings validator `WeightDodgeValidate`: errors 0; expected invasive-hook warnings only;
- runtime profile audit: passed;
- `git diff --check`: clean except the pre-existing CRLF notice for `work/runtime_formula_context.md`.

## Move application gate

The desired penalty is fully calculable offline, but changing tactical movement still depends on
writing the proven Move byte at `unit+0x42`. The bounded one-shot `MovePoke` already exists for this
purpose, but no `[MOVE-POKE]` live evidence exists yet. Do not add a persistent per-poll Move rewrite
until the one-shot shows that movement range and UI both consume the changed value and that the game
does not immediately restore it.

The live gate is:

1. load save 05 and enter a battle with a unit whose current Move and highlighted range are known;
2. capture the baseline range;
3. set `MovePokeTargetCharId`, lower `MovePokeValue` by exactly one, and keep `MovePokeMaxWrites=1`;
4. require one `[MOVE-POKE]` line with the expected old/new bytes;
5. reopen movement selection and verify both displayed Move and reachable tiles drop by one;
6. end/restart the battle to determine whether native initialization restores the original value.

Until that gate passes, Weight → Dodge is implementable/configured and Weight → Move is calculated
but not persistently applied.

