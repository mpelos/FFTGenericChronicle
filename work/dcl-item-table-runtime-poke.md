# DCL — Item-Table Runtime Poke: Equipment Evade at the Source (DEFINITIVE)

**Date:** 2026-07-03 · **Method:** offline static disassembly of `FFT_enhanced.exe` (pefile + capstone), byte-pattern verification against `work/item_catalog.csv`. NO live tests in this pass.
**Scripts:** `work/disasm_itemtable_lookup.py`, `work/disasm_itemtable_fetchers.py`, `work/disasm_itemtable_scan.py`, `work/disasm_itemtable_verify.py`, `work/disasm_itemtable_xrefs.py`, `work/disasm_itemtable_peek.py`, `work/disasm_itemtable_peek2.py`.

## VERDICT

**YES — the mod can zero equipment evade at runtime by writing the loaded item stat tables.** They are **static arrays at fixed VAs inside the exe image** (no ASLR, no pointer indirection, no heap). The item-id→row math is done by REAL code (fn `0x2B8CB8`) with hardcoded absolute addresses; the per-category stat rows live in a single contiguous writable blob directly after the ItemData table. Every value was verified byte-for-byte against the item catalog (128 weapon + 16 shield + 64 armor + 32 accessory constraints, each yielding exactly ONE candidate location in the whole 400 MB file — no duplicates anywhere).

Denuvo virtualizes the *readers* (`0x2B8CE8/0x2B8D48/0x2B8DB0/0x2B8E14` are `jmp 0x1500xxxxx` VM thunks) but the *data* is plain, exactly as the data-not-code rule predicts.

## The poke table (image base 0x140000000, fixed)

| Table | VA (runtime) | RVA | Stride | Rows | Index | Fields |
|---|---|---|---|---|---|---|
| **ItemData** (id < 0x100) | `0x14080EA90` | 0x80EA90 | 0xC | 256 | item id | +4 `additional_data_id`, +5 category (0x13 = shield), +7 weapon-type, +8 price u16, +A shop availability |
| **ItemData** (id ≥ 0x100) | `0x14067F910 + id*0xC` | 0x67F910 | 0xC | ids 0x100–0x104 | item id (full, incl. 0x100 offset) | same layout; **in read-only .rodata** |
| **Weapon stats** | `0x14080F690` | 0x80F690 | **8** | 128 | `additional_data_id` | +0 range, +1 attack-flags, +2 formula, +3 0xFF, **+4 WP, +5 W-Ev**, +6 elements, +7 options-ability |
| **Shield stats** | `0x14080FA90` | 0x80FA90 | **2** | 16 | `additional_data_id` | **+0 physical evade, +1 magical evade** |
| **Armor stats** | `0x14080FAB0` | 0x80FAB0 | 2 | 64 | `additional_data_id` | +0 HP bonus, +1 MP bonus (do NOT zero) |
| **Accessory stats** | `0x14080FB30` | 0x80FB30 | **2** | 32 | `additional_data_id` | **+0 physical evade, +1 magical evade** |
| Weapon-type table | `0x14080FEA0` | 0x80FEA0 | 0x1A | 0x55 | ItemData+7 | element/flag bits per weapon class |

All poke targets (weapon/shield/armor/accessory stat tables + low ItemData) are in section **`.debug$P`, which is WRITABLE** (Characteristics W, not X) — `WriteProcessMemory`/direct write works with no `VirtualProtect` needed. Only the high-id ItemData rows (ids 256–260) are in read-only `.rodata`, and those don't matter for evade: their `additional_data_id` points into the same writable sub-tables.

### Exact zero-evade writes

```
W-Ev   : for aid in 0..127:  *(u8*)(0x14080F695 + aid*8) = 0     // weapon row +5 only (keep +4 WP!)
S-Ev   : memset((void*)0x14080FA90, 0, 0x20)                     // 16 shields × (phys, magic)
A-Ev   : memset((void*)0x14080FB30, 0, 0x40)                     // 32 accessories × (phys, magic)
```

Sanity anchors to verify before/after poking (vanilla values):
- `0x14080FAAC` = `32 19` — **Venetian Shield: 50 phys / 25 magic** (the "Ramza's shield 50% = 0x32" item; aid 14).
- `0x14080FAAE` = `4B 32` — Escutcheon (unique): 75/50.
- `0x14080F695 + 6*8 = 0x14080F6C5` = `28` — Main Gauche W-Ev 40.
- `0x14080FB64` = `28 1E` — Featherweave Cloak 40/30.

Class evade (C-Ev, unit+0x4B) is **not** in these tables — it comes from job data (separate lever, already mapped).

## The proven deref chain (question 1)

`fn 0x285394` / `fn 0x3965B0` (Writer B, live-proven writers of unit+0x44..0x47) → `fn 0x287410` / `0x396C8C`: loop over the unit's **5 equipment slot ids (words at unit+0x54..0x5D)**, per slot call `fn 0x286D04` / `0x396A18 (id & 0x3FF, out1, out2, slotIndex)`:

1. `fn 0x2B8CB8(id)` — REAL code, the master id→row map:
   ```
   row = 0x140000000 + (id < 0x100 ? 0x80EA90 : 0x67F910) + id*0xC
   ```
2. `fn 0x2B8CE8(id)` (VM thunk) → weapon row `0x14080F690 + aid*8`; real code copies **[row+4]=WP, [row+5]=W-Ev** into the stat buffer: slot 0 (right hand) → buf+6/+0xA, slots 1–4 (left hand) → buf+8/+0xC.
3. else `fn 0x2B8D48(id)` → shield row `0x14080FA90 + aid*2`; **[row+0] phys → buf+0x16, [row+1] magic → buf+0x20**.
4. else `fn 0x2B8DB0(id)` → armor row `0x14080FAB0 + aid*2` (HP/MP → out1).
5. else `fn 0x2B8E14(id)` → accessory row `0x14080FB30 + aid*2`; **phys → buf+0x18, magic → buf+0x22**.
6. weapon-type extras from `0x14080FEA0 + type*0x1A`.

Writer B then stores buf+6/+8/+0xA/+0xC → **unit+0x44 (WP R), +0x45 (WP L), +0x46 (W-Ev R), +0x47 (W-Ev L)**. The shield/accessory sums (buf+0x16/0x18/0x20/0x22) are produced by the same helper for its other callers — the Denuvo VM's own evade derivation reads **the same static tables**, which is why refreshing the unit bytes never stuck: the VM re-derives from the tables. Zeroing the tables kills every equipment-evade leg at the source, for both the real-code writers and the VM derivation, for ALL units (player + AI) — matching the DCL goal.

Because these are the only copies of that data in the entire image, `0x2B8CE8/0x2B8D48/0x2B8E14`'s VM internals don't matter: whatever they do, the row pointers they return point into this blob (uniqueness proven by exhaustive file search).

## Writer A is REFUTED — it was never combat code (question 2)

`fn 0x59F550` (the "[rdi+0x10..0x17] → [+0x48..0x4F] copier") and its sole caller `fn 0x59C0B0` are **HID gamepad device enumeration**, not battle code:

- `fn 0x60C539` = `HidD_GetPreparsedData`, `fn 0x60C521` = `HidP_GetCaps`, `fn 0x60C527` = `HidP_GetValueCaps` (IAT slots 0x6110E8/0x6110D8/0x6110E0, HID.DLL), with the `cmp eax, 0x110000` = `HIDP_STATUS_SUCCESS`.
- The "stat block" rdx fed to 0x59F550 is a **heap buffer** (`malloc` via fn-ptr `[0x140C5F380]`, size = capCount*0x20) filled by `HidP_GetValueCaps` — i.e. `HIDP_VALUE_CAPS` records for axes/buttons.
- The `+0x48..0x4F` destination offsets coinciding with the unit struct's accessory/shield evade bytes was a byte-scan false positive.

This closes the mystery of why "Writer A" writes never fired on unit structs in battle: they never touch units at all. **Delete Writer A from the evade-writer model.**

## NXD relationship (question 3)

The catalog columns map 1:1 onto the found rows (`weapon_power/weapon_evasion` = weapon row +4/+5, `shield_physical/magical_evasion` = shield row +0/+1, `accessory_*` = accessory row +0/+1, `armor_hp/mp_bonus` = armor row +0/+1, `additional_data_id` = the sub-table index, `item_category`/`price`/`shop_availability` in ItemData +5/+8/+A). These static arrays ARE the runtime form of ItemData/ItemWeaponData/ItemShieldData/ItemAccessoryData; the exe ships them pre-baked at fixed RVAs. There is **no pointer-chained "loaded NXD object"** for item stats — real code addresses the arrays absolutely.

**Caveat (only open risk):** the sub-table region sits in a writable section, so an NXD-override loader (VM-side, invisible statically) *could* rewrite it at boot or between battles — this is presumably how file-based item mods take effect. Consequences for the mod:
1. Poke **after** game data load; safest is to re-poke on every battle start (mod already has that hook).
2. Live sanity check once: read 2 bytes at `0x14080FAAC` in-process — expect `32 19` (or the NXD-modded values if item files are overridden). If it matches, the static map is confirmed end-to-end.

## Field notes for later reuse

- ItemData row +5 category enum: `0x13` = Shield (used by Writer B to route the item id into the shield display slot).
- Weapon table aid space is shared by high-id items (e.g. Materia Blade Plus id 256 → aid 32, row `01 8E 01 FF 0A 0A 00 00` = rng1/WP10/WEv10 — matches CSV).
- Slot mapping confirmed: unit+0x54 = 5 equipment id words; slot 0 = right hand, slot 1 = left hand, 2–4 = head/body/accessory.
- Gap `0x80FB70..0x80FEA0` likely holds the consumable/chemist-item table (fn `0x2B8F30`, also VM-thunked; not needed for evade).
