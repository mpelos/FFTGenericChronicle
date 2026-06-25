# Equipment-in-formula — deterministic offline proof (2026-06-24)

## What this proves

A fully custom damage formula can read the **equipment of BOTH the attacker and
the target** from the real battle-unit struct (16-bit word slots at +0x1A..+0x26),
join it against `item_catalog.csv` (weapon family, weapon power, armor HP bonus),
and **branch the damage result on that equipment** — through the exact same code
path that runs live (`ReadUInt16` → `EquipmentSlotProbe` → `AddSlotVariables` →
formula engine).

This closes the U5 payoff question: *can the redesign formula depend on
attributes AND equipment of both sides?* — **yes**, demonstrated end-to-end.

## Why offline (and why that is sufficient here)

The live equipment **read** was already proven in-game by the `[PRECLAMP-EQUIP]`
probe (Ramza → Ninja capture, committed `b5c818e`): at the native pre-clamp
damage frame the mod read the Ninja's full kit (Thief's Cap / Ninja Gear /
Chantage / Iga Blade / Koga Blade) correctly from a live battle.

What remained was proving the read flows *into the formula context* and the
formula *computes on it*. That is pure, deterministic logic — it does not depend
on game timing — so the settings simulator (`settingssimulate`) is the correct,
unambiguous tool: it injects equipment at the real offsets and runs the identical
pipeline. No live capture, no battle-start CT-charge flakiness, no guesswork.

## Setup

- Profile: `work/battle-runtime-settings.equipment-formula-dryrun.json`
  - `EquipmentSlots` (target): Head/Body/Accessory/Weapon/Shield at off 26/28/30/32/38, all `UInt16`
  - `AttackerEquipmentSlots`: Weapon/Shield at off 32/38, `UInt16`
  - `ItemCatalogPath: item_catalog.csv`
  - Derived vars:
    - `dv.bladed   = a.present && (aslot.weapon.category_sword || ...knightsword || ...katana || ...ninjablade)`
    - `dv.weaponBonus = if(dv.bladed, aslot.weapon.weaponPower, 0)`
    - `dv.armorDr  = floorDiv(tslot.body.armorHpBonus, 10)`
    - `dv.final    = max(1, vanillaDamage + dv.weaponBonus - dv.armorDr)`
  - `FinalDamageFormula = dv.final`
- Scenarios: `work/equipment-formula-sim-scenarios.json` (equipment injected via per-unit `raw` words at the confirmed offsets)
- Command:
  ```
  dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- "work\battle-runtime-settings.equipment-formula-dryrun.json" "work\equipment-formula-sim-scenarios.json"
  ```

## Result — 4/4 pass

| Scenario | Attacker weapon (family, WP) | Target body (armorHP) | vanilla | bladed | weaponBonus | armorDr | **final** |
|---|---|---|---|---|---|---|---|
| 1 Ramza | Chaos Blade (KnightSword, 40) | Ninja Gear (20) | 150 | 1 | 40 | 2 | **188** |
| 2 Cloud | Materia Blade Plus **id=256** (Sword, 10) | Ninja Gear (20) | 100 | 1 | 10 | 2 | **108** |
| 3 Bow | Artemis Bow (Bow, 10) | Ninja Gear (20) | 80 | **0** | **0** | 2 | **78** |
| 4 no-attacker | (none) | Ninja Gear (20) | 50 | 0 | 0 | 2 | **48** |

Every row matched its asserted `finalDamage` and `desiredHp` (`expect=pass`).

### The four things this nails

1. **Both sides read at once.** Scenario 1 trace shows `attackerSlots=weapon(present,id=37:Chaos Blade...)`
   AND `targetSlots=head(168:Thief's Cap),body(197:Ninja Gear),accessory(236:Chantage),weapon(17:Iga Blade)`.
2. **16-bit word width is load-bearing.** Scenario 2 reads `id=256:Materia Blade Plus` — id 256
   is impossible to read as a single byte (would be 0). UInt16 width is mandatory and works.
3. **Formula branches on weapon family.** Chaos Blade / Materia Blade → `bladed=1` (bonus applied);
   Artemis Bow → `bladed=0` (bonus suppressed). The family flag comes from the catalog join.
4. **Target armor → DR, and degrades gracefully.** `Ninja Gear.armorHpBonus=20 → armorDr=2` in every
   row; scenario 4 (attacker=none, `attackerSlots=weapon(missing...)`) still applies the target-side
   DR — no crash, attacker terms simply zero out.

## Raw trace (verbatim)

```
== ramza-chaosblade(knightsword,WP40)-vs-ninja(ninjagear,armorHP20)
rewrite=True vanillaDamage=150 hp=300->150->112 finalDamage=188 rule=FinalDamageFormula
expect=pass
event=damage | attacker=0x1000:scenario | action=none | targetSlots=head(present,id=168:Thief's Cap,off=0x1A,width=UInt16,matches=1),body(present,id=197:Ninja Gear,off=0x1C,width=UInt16,matches=1),accessory(present,id=236:Chantage,off=0x1E,width=UInt16,matches=1),weapon(present,id=17:Iga Blade,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | attackerSlots=weapon(present,id=37:Chaos Blade,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | equipmentDr=0:NoEquipmentDR | response=raw1000/permille1000/rules0/clamped0:NoDamageResponse | vars=trace.atkpresent=1,trace.atkweaponid=37,trace.atkweaponpower=40,trace.atkweaponformula=1,trace.atkbladed=1,trace.atkshieldid=0,trace.tgtheadid=168,trace.tgtbodyid=197,trace.tgtbodyarmorhp=20,trace.tgtaccid=236,trace.weaponbonus=40,trace.armordr=2,trace.vanilla=150,trace.final=188 | final=188:FinalDamageFormula

== cloud-materiabladeplus(sword,WP10)-vs-ninja(ninjagear)
rewrite=True vanillaDamage=100 hp=300->200->192 finalDamage=108 rule=FinalDamageFormula
expect=pass
event=damage | attacker=0x1000:scenario | action=none | targetSlots=head(present,id=0:Nothing Equipped,off=0x1A,width=UInt16,matches=1),body(present,id=197:Ninja Gear,off=0x1C,width=UInt16,matches=1),accessory(present,id=0:Nothing Equipped,off=0x1E,width=UInt16,matches=1),weapon(present,id=0:Nothing Equipped,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | attackerSlots=weapon(present,id=256:Materia Blade Plus,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | equipmentDr=0:NoEquipmentDR | response=raw1000/permille1000/rules0/clamped0:NoDamageResponse | vars=trace.atkpresent=1,trace.atkweaponid=256,trace.atkweaponpower=10,trace.atkweaponformula=1,trace.atkbladed=1,trace.atkshieldid=0,trace.tgtheadid=0,trace.tgtbodyid=197,trace.tgtbodyarmorhp=20,trace.tgtaccid=0,trace.weaponbonus=10,trace.armordr=2,trace.vanilla=100,trace.final=108 | final=108:FinalDamageFormula

== bowuser-artemisbow(bow,non-bladed)-vs-ninja(ninjagear)
rewrite=True vanillaDamage=80 hp=300->220->222 finalDamage=78 rule=FinalDamageFormula
expect=pass
event=damage | attacker=0x1000:scenario | action=none | targetSlots=head(present,id=0:Nothing Equipped,off=0x1A,width=UInt16,matches=1),body(present,id=197:Ninja Gear,off=0x1C,width=UInt16,matches=1),accessory(present,id=0:Nothing Equipped,off=0x1E,width=UInt16,matches=1),weapon(present,id=0:Nothing Equipped,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | attackerSlots=weapon(present,id=89:Artemis Bow,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | equipmentDr=0:NoEquipmentDR | response=raw1000/permille1000/rules0/clamped0:NoDamageResponse | vars=trace.atkpresent=1,trace.atkweaponid=89,trace.atkweaponpower=10,trace.atkweaponformula=1,trace.atkbladed=0,trace.atkshieldid=0,trace.tgtheadid=0,trace.tgtbodyid=197,trace.tgtbodyarmorhp=20,trace.tgtaccid=0,trace.weaponbonus=0,trace.armordr=2,trace.vanilla=80,trace.final=78 | final=78:FinalDamageFormula

== no-attacker-target-armor-still-applies
rewrite=True vanillaDamage=50 hp=300->250->252 finalDamage=48 rule=FinalDamageFormula
expect=pass
event=damage | attacker=none | action=none | targetSlots=head(present,id=0:Nothing Equipped,off=0x1A,width=UInt16,matches=1),body(present,id=197:Ninja Gear,off=0x1C,width=UInt16,matches=1),accessory(present,id=0:Nothing Equipped,off=0x1E,width=UInt16,matches=1),weapon(present,id=0:Nothing Equipped,off=0x20,width=UInt16,matches=1),shield(present,id=0:Nothing Equipped,off=0x26,width=UInt16,matches=1) | attackerSlots=weapon(missing,id=0,off=?,width=?,matches=0),shield(missing,id=0,off=?,width=?,matches=0) | equipmentDr=0:NoEquipmentDR | response=raw1000/permille1000/rules0/clamped0:NoDamageResponse | vars=trace.atkpresent=0,trace.atkweaponid=0,trace.atkweaponpower=0,trace.atkweaponformula=0,trace.atkbladed=0,trace.atkshieldid=0,trace.tgtheadid=0,trace.tgtbodyid=197,trace.tgtbodyarmorhp=20,trace.tgtaccid=0,trace.weaponbonus=0,trace.armordr=2,trace.vanilla=50,trace.final=48 | final=48:FinalDamageFormula
```

## Scope / honesty notes

- This is the **read + formula computation** proof. It is dry-run semantics
  (`rewrite=True` but in the live profile `DryRunRewrites=true`, so HP is logged,
  not written). The HP-**write** primitive is independently proven live
  (see live-runtime-findings).
- Offline, the attacker is injected directly (`attacker=...:scenario`). Live, the
  attacker pointer comes from the `[PRECLAMP-ACTOR-CTX]` resolver — a separate,
  already-working concern. The equipment-read offsets/width are identical in both.
- Next milestone after this: flip to a real (non-dry-run) equipment-derived
  rewrite in a live battle, and design the actual GC damage-reduction tags
  (U5 Q3–Q5).
```
