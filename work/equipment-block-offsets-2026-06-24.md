# Live Equipment Block In The Battle Unit Struct (2026-06-24)

Status: **proven live** (triple-confirmed by ground truth). Roadmap U5 Q1 answered.

## Result

Equipped item ids live **inside the 0x200 battle unit struct**, stored as **16-bit
little-endian words**, in a contiguous block:

| Offset | Slot | Width |
| --- | --- | ---: |
| `+0x1A` | Head | word |
| `+0x1C` | Body | word |
| `+0x1E` | Accessory | word |
| `+0x20` | Right hand - weapon | word |
| `+0x22` | Right hand - shield | word |
| `+0x24` | Left hand - weapon | word |
| `+0x26` | Left hand - shield | word |

The word is the `item_id` from `work/item_catalog.csv` (join to get name, family, WP,
element, evasion, HP/MP bonus, equip-bonus, etc.).

Sentinels:

- empty hand slot on an equip-capable unit = `0x00FF` (255);
- monster / no-equipment unit = `0x0000` (0, "Nothing Equipped") in every slot.

Reading rules for formulas:

- primary weapon = word at `+0x20` (fall back to `+0x24` if `+0x20` is empty/255);
- dual wield = non-empty weapon at both `+0x20` and `+0x24`;
- shield = whichever of `+0x22` / `+0x26` is non-empty (left-hand `+0x26` in all samples);
- two-handed weapon = `+0x20` set, the other three hand-words empty.

Equipment is **NOT** in the 0x548 actor struct (head/body/accessory ids do not appear
there); the unit struct is the source of truth. No roster/ENTD join is required.

## How it was found (zero new captures)

1. Mined the 1143 existing `[DUMP ptr=.. id=..]` lines (full 0x200 unit-struct hex that
   `Mod.cs` already emits on first hook-touch) from `work/live-captures`.
2. Auto-decoding bytes against the catalog was inconclusive (the catalog densely covers
   ids 0-260, so almost every byte "decodes" to some item).
3. Got ground truth from the in-game equip screen for 3 units, then intersected, per slot,
   the offsets where each unit's **word** equals its known item id. Every slot resolved to
   a single common offset across all 3 units.

Tool: `tools/analyze_equipment_dumps.py`
- `--find 0xID=w,s,h,b,a` intersects ground-truth ids to pin offsets (word-aware);
- `--equip` decodes the confirmed block for every parsed unit;
- `--rank` ranks candidate slots by family consistency (used during discovery).

## Ground truth used

```
Cloud  (0x32): R=Materia Blade Plus(256) Lhand=- Head=Genji Helm(155) Body=Genji Armor(183) Acc=Red Shoes(214)
Agrias (0x1E): R=Runeblade(30)           Lhand=- Head=Lambent Hat(167) Body=Lordly Robe(207) Acc=Genji Gloves(216)
Ninja  (0x80): R=Iga Blade(17) L=Koga Blade(18) Head=Thief's Cap(168) Body=Ninja Gear(197) Acc=Chantage(236)
```

## `--equip` decode across 8 units (evidence)

```
Cloud    Head=Genji Helm(155)   Body=Genji Armor(183)    Acc=Red Shoes(214)       R=Materia Blade Plus(256)
Beowulf  Head=Lambent Hat(167)  Body=Luminous Robe(206)  Acc=Magepower Gloves(217) R=Runeblade(30)
Agrias   Head=Lambent Hat(167)  Body=Lordly Robe(207)    Acc=Genji Gloves(216)    R=Runeblade(30)
Ninja    Head=Thief's Cap(168)  Body=Ninja Gear(197)     Acc=Chantage(236)        R=Iga Blade(17)  L=Koga Blade(18)
id0x82   all 0 (monster, no equipment)
Ramza    Head=Grand Helm(156)   Body=Maximillian(185)    Acc=Bracers(218)         R=Chaos Blade(37)  Lshield=Venetian Shield(142)
id0x03   Head=Crystal Helm(154) Body=Mirror Mail(184)    Acc=Bracers(218)         R=Excalibur(35)  L=Defender(33)
id0x81   Head=Thief's Cap(168)  Body=Black Garb(198)     Acc=Elven Cloak(232)     R=Artemis Bow(89)
```

## Why this matters

- Closes the biggest gap to the project goal: custom formulas can now read **attacker and
  target equipment of both sides** directly from the live unit struct.
- Unblocks basic-attack weapon identity: a basic attack carries action id `0`, but the
  attacker's weapon is `attacker_unit+0x20` (e.g. Agrias basic = Runeblade), giving WP /
  element / formula from the catalog.
- No roster mapping needed; the battle unit struct is self-contained for equipment.

## Live read-back validation (2026-06-24) - PASSED

An observe-only probe (`PreClampLogEquipment`, log line `[PRECLAMP-EQUIP]`) reads the block for
both the resolved caster and the target at the native pre-clamp damage frame. A Ramza basic attack
on Ninja in a **fresh game session** produced:

```text
[PRECLAMP-ACTOR-CTX event=2 oldDebit=912 caster=id0x01 actionId=0 verdict=resolved]
[PRECLAMP-EQUIP side=target id=0x80] head=168 body=197 acc=236 rWeapon=17 rShield=255 lWeapon=18 lShield=255
[PRECLAMP-EQUIP side=caster id=0x01] head=156 body=185 acc=218 rWeapon=37 rShield=255 lWeapon=255 lShield=142
```

Both sides matched ground truth exactly. This validates: live read at the damage frame; both sides
in one event; basic-attack (action 0) weapon identity via `+0x20`; the shield slot live (Ramza
Venetian Shield `+0x26`); dual-wield live (Ninja Iga+Koga); offset stability across a fresh session.
Evidence: `work/live-captures/battleprobe_log.equipment-readout-ramza-ninja.snapshot.txt`.

## Open follow-ups

- `+0x22` (right-hand shield) never populated in samples; semantics inferred from the
  left-hand shield (`+0x26`, Ramza Venetian Shield). Confirm with a right-hand-shield unit
  if one ever appears, or a controlled swap.
- Wire equipment into the runtime formula context (attacker/target weapon + armor + family
  + element) and dry-run a formula that branches on weapon family / target armor.
