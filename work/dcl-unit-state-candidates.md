# DCL Unit-State Candidates — Offline RE (2026-07-02)

Offline-only mining for the unmapped unit-state surfaces required by
`docs/modding/08-dcl-information-requirements.md` §2: full status bitmap, position/facing/height,
turn ownership, raw-vs-effective stat pairs, and job-level/JP progression.

**No live process was touched.** Evidence sources:

1. **Snapshot corpus** — 40 unit-state combos from `work/live_unit_struct_snapshot.*.md`
   (5 units × baseline/forecast/confirmed/pending×3/post, Braver + Cross Slash single/AoE),
   parsed and cross-diffed by script (scratchpad `parse_snaps.py`).
2. **Community cheat table** — `bbfox0703/Mydev-Cheat-Engine-Tables`
   `FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.CT` (2025-10, targets this exe),
   including its annotated real-code disassembly and status-bit AA scripts.
3. **Classic PSX layout** — FFHacktics Battle Stats unit data (innate `0x4E-0x52`, immunity
   `0x53-0x57`, current status `0x58-0x5C`, bit meanings; site is bot-walled, layout confirmed
   via search excerpts).
4. **Static disasm** — capstone sweep of `FFT_enhanced.exe` RVA `0x1000..0x610000`
   (non-virtualized region) for `[reg+offset]` accesses (noisy for small offsets; only
   context-anchored hits are cited).

Confidence: **Strong** = multiple independent sources agree; **Hypothesis** = single-source or
inference, needs live check. Everything here is *offline*; the §6 live checklist promotes items.

---

## 1. Status flag bitmap — three parallel 5-byte arrays

### 1.1 Array layout — **Strong**

| Array | Offsets | Classic analog | Role |
| --- | --- | --- | --- |
| **Source (innate/equipment)** | `+0x57..+0x5B` | `0x4E-0x52` innate | always-on statuses granted by job/equipment |
| **Immunity** | `+0x5C..+0x60` | `0x53-0x57` immunity | status immunities (Hypothesis, by cluster position) |
| **Effective (current)** | `+0x61..+0x65` | `0x58-0x5C` current | what the engine acts on this frame |
| **Master (durable)** | `+0x1EF..+0x1F3` | — (IVC-specific) | durable copy; effective is recomputed from it |

Decisive evidence for source/effective/master (script-verified):

- **`effective[i] == master[i] | source[i]` for i = 0..4 held with ZERO mismatches across all
  40 snapshot unit-states.** This generalizes the already-proven byte-0 relation
  (`+0x61 = (+0x1EF & 0xF2) | +0x57`, recompute code at `0x30D42A`) to all five bytes.
  Worked example: Ninja `src[2]=0x20`, `master[2]=0x10` → `eff[2]=0x30` (`+0x63 = 0x30`).
- The classic cluster is contiguous (innate → immunity → current, 15 bytes); IVC keeps the same
  contiguity at `+0x57..+0x65`, which is exactly why the long-known `+0x57` "innate/equip source"
  sits 10 bytes before the long-known `+0x61` effective byte.
- Real-code corroboration (CT-annotated disasm, non-VM): `0x2021EB lea rcx,[r15+0x61]` and
  `0x2021C7 lea rdx,[r15+0x66]` pass the current-status array base and the following block to a
  call (`r15` = unit; same function reads `[r15+0x30]` HP, `[r15+0x32]` MaxHP, `[r15+0x06]`,
  `[r15+0x14C]` name, `[r15+0x1FC]`).
- Master-array maintenance in real code: `0x58E74E/0x58E78E/0x58E7B0` read+write `+0x1F1/+0x1F2/+0x1F3`
  together; `0x30A918 mov [rdi+0x1F3], bl` writes master byte 4 **inside the HP/MP APPLY routine**
  (`0x30A51C..`), i.e. action results land in the master array on the apply path.

### 1.2 Bit map — per-bit provenance

IVC uses the **classic PSX bit assignments verbatim** (byte-for-byte, bit-for-bit). Markers:
✅ live-proven (this repo), ⚙ CT script (writes this exact bit on this exact byte), 🔬 static
disasm site, 📜 classic layout only (needs live check).

```text
byte 0  (+0x61 / +0x57 / +0x1EF)
  0x40 Crystal📜   0x20 Dead/KO✅   0x10 Undead✅   0x08 Charging✅
  0x04 Jumping📜   0x02 Defending📜 0x01 Performing📜   (0x80 unused📜)

byte 1  (+0x62 / +0x58 / +0x1F0)
  0x80 Petrify📜   0x40 Invite/Traitor📜🔬  0x20 Blind/Darkness⚙   0x10 Confuse⚙
  0x08 Silence⚙    0x04 Vampire/Blood-Suck📜  0x02 Cursed📜   0x01 Treasure/Chest📜

byte 2  (+0x63 / +0x59 / +0x1F1)
  0x80 Oil📜   0x40 Float⚙   0x20 Reraise⚙   0x10 Invisible/Transparent📜🔬
  0x08 Berserk📜   0x04 Chicken📜🔬   0x02 Frog/Toad⚙🔬   0x01 Critical🔬

byte 3  (+0x64 / +0x5A / +0x1F2)
  0x80 Poison📜   0x40 Regen📜   0x20 Protect⚙*   0x10 Shell⚙*
  0x08 Haste⚙     0x04 Slow📜    0x02 Stop📜      0x01 Wall📜

byte 4  (+0x65 / +0x5B / +0x1F3)
  0x80 Faith📜    0x40 Innocent/Atheist📜   0x20 Charm📜   0x10 Sleep⚙
  0x08 Immobilize/Don't-Move⚙   0x04 Disable/Don't-Act⚙   0x02 Reflect⚙   0x01 Doom/Death-Sentence📜
```

⚙ sites (cheat-table `INJECT_BATT_GET_ALL_CHARS` script, hooks AOB `45 0F B7 57 30` =
`movzx r10d, word [r15+0x30]` at RVA `0x2021D0`): sleep `[r15+65]&EF/|10`, blind `[r15+62]&DF/|20`,
don't-act `[r15+65]&FB/|4`, don't-move `[r15+65]&F7/|8`, confuse `[r15+62]&EF/|10`, silence
`[r15+62]&F7/|8`, frog `[r15+63]&FD/|2`, reflect `[r15+65]|2`, reraise `[r15+63]|20`, float
`[r15+63]|40`, haste("fast") `[r15+64]|8`. (*Its "wall" toggle writes `[r15+64]|0x30` =
Protect|Shell — the author's shorthand, not the Wall bit.)

🔬 sites: `0x205544 test byte [rax+0x63],1` — Critical check **inside the proven result/animation
selector `0x205210`**; `0x1ED3C5/0x1ED3D6/0x1ED428` test `+0x63` bits `4/2/0x10`
(Chicken/Frog/Invisible gate cluster); `0x0DCE2F..0x0DDF04` repeatedly `test byte [r+0x62],0x40`
(Invite/Traitor-flavored team check) in a function that also reads **pairs** of `+0x4F` (X) from
two different unit pointers (`0x0DD3B8/0x0DD3C6`) — an AI/targeting routine.

Snapshot sanity decode (all coherent): Ninja `master[2]=0x10` = Invisible (hadn't acted yet —
WotL transparent-until-act); Ninja `src[2]=0x20` = equipment Always:Reraise; Ramza+Ninja
`src[3]=0x40` = Always:Regen (matches the live "Regen kept healing the zombie unit" observation);
Agrias `src[3]=0x30` = Always:Protect+Shell.

### 1.3 Neighboring arrays — **Hypothesis**

- **`+0x5C..+0x60` = status immunity** (classic `0x53-0x57`). Snapshot values are per-unit stable
  and sparse (Ramza `+0x5D=0x20` → Blind-immune, `+0x60=0x10` → Sleep-immune; Ninja `+0x60=0x0C`
  → Don't-Move+Don't-Act-immune) — consistent with endgame accessories.
- **`+0x52..+0x56` = elemental affinity block** (classic `0x49-0x4D` absorb/cancel/half/weak/
  strengthen, element bits `0x80 Fire 0x40 Lightning 0x20 Ice 0x10 Wind 0x08 Earth 0x04 Water
  0x02 Holy 0x01 Dark`). Snapshot values are dense per-unit-stable masks (Ramza
  `9A 40 19 FE 10`), exactly what equipment-derived elemental masks look like. Fills the doc's
  "elemental affinity (likely derived)" gap — it appears to be materialized in the struct.
- **`+0x66..` = status duration/CT block**: real code takes `lea rdx,[r15+0x66]` right alongside
  `lea rcx,[r15+0x61]`; the CT "fast" script writes `[r15+0x6A]=0x20` (a haste duration) when
  setting the haste bit. Element/slot order unknown.

### 1.4 Write rules (unchanged from the proven model, now array-wide)

Force status: OR the bit onto master `+0x1EF+i` AND effective `+0x61+i` (mirror for this frame).
Cure: clear master + effective + source `+0x57+i`. Per-turn masking is known only for byte 0
(`& 0xF2`); whether bytes 1-4 have their own recompute masks is a live question (the recompute at
`0x30D42A` should be re-read for the full 5-byte loop).

---

## 2. Position, facing, height

### 2.1 Unit X/Y — `+0x4F` / `+0x50` (bytes) — **Strong**

- Cheat table "Loc+": `X = +0x4F`, `Y = +0x50` (byte).
- Snapshots: per-unit distinct small ints, stable while nobody moved: Ramza (8,10), Ninja (9,9),
  Agrias (10,9), Cloud (10,10), Beowulf (10,8) — a plausible unit cluster.
- **Cross-field proof**: the caster's pending-action epicenter fields (§2.3) reproduced the
  target's `+0x4F/+0x50` exactly for 3 distinct targets (Ramza 08/0A, Agrias 0A/09, Beowulf 0A/08).
- (Also matches the community mapping already noted in `05-reverse-engineering.md` §6.)

### 2.2 Facing — `+0x51` (byte) — **Strong**

- Cheat table "Direction = +0x51".
- Snapshots: all five units `01` at battle start; **Cloud's `+0x51` flipped `01 → 00` exactly when
  he charged Cross Slash at Agrias/Beowulf (south of him) and stayed `01` when targeting Ramza
  (west)** — i.e. it changes when the unit turns, without moving. Classic facing enum is 0-3
  (S/W/N/E); IVC value-to-compass mapping still needs a live table.

### 2.3 Pending-action target/epicenter on the CASTER — `+0x1AC` (X), `+0x1B0` (Y) — **Strong**

Set at action confirm, alongside the known pending block (`+0x1A2` action id, `+0x18D` timer):

```text
Cross Slash → Ramza  : caster+0x1AC=0x08 +0x1B0=0x0A   (= Ramza  +0x4F/+0x50)
Braver      → Beowulf: caster+0x1AC=0x0A +0x1B0=0x08   (= Beowulf+0x4F/+0x50)
Cross Slash → Agrias : caster+0x1AC=0x0A +0x1B0=0x09   (= Agrias +0x4F/+0x50, AoE epicenter)
```

3/3 distinct coordinate pairs. This gives the DCL a **memory-only pending-target/epicenter**,
complementing the pending tracker (which previously had no target field). Also written at confirm:
`+0x1A0 = 0x13` (= the caster Cloud's unit index `+0x01` — "acting unit index" candidate),
`+0x1A1 = 0x29`, `+0x1AA = 0x05` (meaning unknown). Cleared with the rest of the pending block.

### 2.4 Height / elevation — **not found in the struct** (Hypothesis: map-derived)

No struct byte behaves like an elevation value (all tested states had every unit at ground level,
so nothing could vary). Classic stores unit height in *separate* misc unit data (X `0x7C`, Y
`0x7D`, map-LEVEL flag `0x7E` = upper/lower tier of two-level tiles) and derives render height
from the map tile. Expectation for IVC: tile height comes from map data at (X,Y); the unit-side
datum, if any, is just a one-bit tier flag. Candidates to watch live: high bits of `+0x51`, and
the untouched bytes `+0x1AD..+0x1AF/+0x1B1..+0x1B3` next to the epicenter X/Y (classic packs
target coords as X,Y,level triples).

---

## 3. Turn ownership / active-unit marker

### 3.1 `+0x1B8 == 1` = the unit whose TURN it is — **Strong** (upgrade from "Hypothesis")

Across **all 40 snapshots**: exactly one unit has `+0x1B8 = 1` in every state, and it is always
the unit currently owning the menu/act phase — Cloud through baseline/forecast/confirmed, then
Agrias / Beowulf / Ninja in the "pending-X" states (their turns while Cloud's action charges),
then Ramza in post states (his `CT=0` follow-up turn). It moves unit-to-unit as turns pass and is
**independent of `+0x1BA`**.

`+0x1BA` refined: it is the "committed/charging action owner" — it turns 1 at action confirm,
stays 1 on the caster across *other units' turns* while the action is pending, and (notably)
remains 1 through the post-resolution states until the caster's next turn. So:
`+0x1B8` = whose turn it is now; `+0x1BA` = who owns the in-flight action. The two together
replace any CT heuristic for ownership.

Related identity fields: `+0x01` = unit slot index (`0x10..0x14` here), mirrored at `+0x1BC`;
the dispatcher (`0x38A4FC`) consumes `(category<<8)|unitIdx` events (`0x100` = turn-done), so this
index is the engine's own turn-bookkeeping key.

### 3.2 Global active-unit index — open, with a concrete plan

The dispatcher/category producer walks records via the phase dword `[0x186B044]`
(`05-reverse-engineering.md`), so a static "current active unit index/pointer" almost certainly
lives nearby. Offline scanning can't see runtime statics; live plan in §6.

---

## 4. Raw vs effective stats, progression

### 4.1 Equipment-bonus triple `+0x3B/+0x3C/+0x3D` = PA/MA/Speed bonus — **Strong**

`raw (+0x38/39/3A) + bonus (+0x3B/3C/3D) == effective (+0x3E/3F/40)` held **15/15** across all
five units (e.g. Agrias: PA `07+04=0B`, MA `11+06=17`, Speed `0B+01=0C`). This completes the
raw→effective model with the materialized per-unit equipment delta — formulas can now read "how
much of PA is gear" directly.

### 4.2 JP — two parallel per-job word arrays, index = `jobId - 0x4A` — **Strong**

- **Array A base `+0xF0`**, 23 words (`+0xF0..+0x11D`); **Array B base `+0x11E`**, 23 words
  (`+0x11E..+0x14B`) — the cheat table labels array B "JP EXP 1..23".
- Live-diff evidence: after Cloud (job `0x58`, index `0x58-0x4A = 14`) resolved Cross Slash, his
  `+0x10C` (= `0xF0 + 14*2`) and `+0x13A` (= `0x11E + 14*2`) both **+52**; every ally gained
  **+10 at the same index 14** in both arrays — the classic JP-spillover rule (allies get ¼, in
  the *acting unit's* job). Ramza's post-Braver diffs show the same pair (`+0x10C`, `+0x13A`) +10.
- One array is total-JP (job-level driver), the other spendable-JP; which is which needs a
  JP-spend test (§6). EXP at `+0x28` gained +13 (Cloud) / +12 (Beowulf) on the same actions.

### 4.3 Job levels — nibble-packed table at `+0xE4..+0xEE` — **Hypothesis**

11 bytes = 22 nibbles, every nibble in `0..8` for all five units (e.g. Cloud
`83 63 63 46 54 23 32 85 12 01 31`), per-unit stable — the per-job job-level table (22-23 WotL
jobs, level 0-8, 0 = locked). Nibble order/index base unverified (likely same `jobId-0x4A` index,
one nibble per job). The current job's level (JP formula input `8 + 2*JobLevel + Level/4`) would
be `nibble[jobId-0x4A]`.

### 4.4 R/S/M ability slots `+0x12..+0x19` — **Hypothesis** (cheat table)

CT maps: `+0x12` "Ability" (= secondary skillset?), `+0x13` "Action", `+0x14` Reaction,
`+0x15` "Enabled?", `+0x16` Support, `+0x17` "Enabled?", `+0x18` Movement, `+0x19` "Enabled?".
Snapshot values read better as **16-bit ability ids**: Ninja `+0x14 = 0x01B7 (439)`,
Cloud `+0x14 = 0x01BD (445)`, Ramza `+0x14 = 0x0000` (no reaction) — i.e. word Reaction `+0x14`,
Support `+0x16`, Movement `+0x18`. Fills the doc's "R/S/M somewhere in 0x52..0x8F" gap (it is
actually *before* the equipment block). Needs a live equip-change test.

---

## 5. Bonus identity fields (from the same mining)

| Offset | Meaning | Confidence |
| --- | --- | --- |
| `+0x01` | unit slot index (0x10+n), mirrored at `+0x1BC` | Strong |
| `+0x07` | "Hearts" (CT) | Hypothesis |
| `+0x18F`, `+0x1FC` (dword) | char-id echoes (`0x1FC` read at `0x2021AF` beside name `+0x14C`) | Strong |
| `+0x191` | roster/story unit id (Ramza 01; generics 0x78-0x7B sequential) | Hypothesis |
| `+0x1EE` | mirrors `+0x05` friend/foe flags | Hypothesis |

---

## 6. Minimal live-test plan (LATER — user GUI + probe reads only)

Each test: probe = read/write the named offsets on the live struct (existing harness knobs).

1. **Status bit sweep** (bitmap 1.2): in a control battle, have an enemy inflict Poison, Blind,
   Silence, Sleep, Slow, Stop one at a time; probe dumps `+0x57..+0x66`, `+0x1EF..+0x1F3` after
   each. Expected: the predicted bit sets on effective+master; duration byte appears in `+0x66..`.
   Then write-test: OR Protect `+0x64|=0x20`/`+0x1F2|=0x20` → buff icon + damage reduction 2/3.
2. **Immunity array** (1.3): equip a Sleep-immune accessory → probe expects `+0x60 |= 0x10`;
   cast Sleep at the unit → no effect. Then clear the byte in memory and re-cast → unit sleeps
   (proves the roll reads live immunity, like the evade bytes).
3. **Elemental block** (1.3): equip Chameleon Robe (absorb Holy) / Flame Shield and diff
   `+0x52..+0x56`; cast a matching element and verify absorb/half behavior follows a memory edit.
4. **Position/facing**: Move a unit a few tiles, re-dump — `+0x4F/+0x50` must follow the tile;
   turn in place (end-of-turn facing choice) — `+0x51` cycles 0-3; record value→compass. Stand on
   a two-tier tile (bridge) and look for the tier bit near `+0x51`.
5. **Epicenter fields**: confirm a charged AoE on an empty TILE (no unit) — caster `+0x1AC/+0x1B0`
   should hold the tile coords, proving it is an epicenter (not a unit ref); check neighbors
   `+0x1AD/+0x1B1` for a map-level byte on elevated tiles.
6. **Turn marker**: poll `+0x1B8` for all units every ~25 ms over several rounds incl. charged
   actions + reactions; assert the exactly-one invariant and that transitions align with the
   on-screen "act" banner. Simultaneously log candidate statics: scan a small window around
   `0x186B044` each poll for a dword equal to the active unit's `+0x01`/`+0x1BC` index → the
   global active-unit field.
7. **JP arrays**: note both arrays at index `jobId-0x4A` before/after an action (both +N expected)
   and before/after LEARNING an ability at the menu (only the spendable array drops) → labels
   Array A vs B. Verify job-level nibble `+0xE4` table increments when a job levels up.
8. **R/S/M**: swap Reaction ability in the party menu, enter battle, read `+0x14` word; repeat for
   Support `+0x16`, Movement `+0x18`.

---

## Sources

- Snapshots: `work/live_unit_struct_snapshot.*.md` (this repo, 2026-06 sessions)
- Cheat table: https://github.com/bbfox0703/Mydev-Cheat-Engine-Tables —
  `FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.CT` (offsets `+0x4F/50/51` Loc,
  `+0x11E..` JP, `+0x12..0x19` R/S/M, status-bit AA scripts, annotated disasm `0x2021A6..0x22220F`,
  `0x213036..0x213082`)
- Classic layout: FFHacktics wiki Battle Stats / BATTLE.BIN Data Tables
  (https://ffhacktics.com/wiki/Battle_Stats — innate `0x4E-0x52`, immunity `0x53-0x57`, current
  `0x58-0x5C`; status bit tables; site is Cloudflare-walled to bots, retrieved via search excerpts)
- PSX decomp map: https://github.com/Talcall/FFT-1997-Decomp (named-function reference; the public
  tree carries boot code + a Ghidra archive, no plaintext battle structs — classic offsets taken
  from FFHacktics instead)
- Static disasm: capstone sweep of `FFT_enhanced.exe` (RVA `0x1000-0x610000`), sites cited inline
