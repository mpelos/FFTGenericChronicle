# Sprite / Character Art Asset Pipeline (Generic Chronicle)

How unit/character sprites are stored in **FINAL FANTASY TACTICS - The Ivalice Chronicles**
(Steam, Enhanced) and how to **reskin** an existing job's appearance — e.g. repurposing the
Calculator and Mime slots into two new jobs.

This surface is **separate from the battle-data/formula work** in `00`-`07`. Changing a job's
*data* (stats/skills/name via `JobData.xml` + `OverrideAbilityActionData`) does NOT change how the
unit *looks*; the art lives in the G2D graphics container documented here. The two are fully
independent: you can swap the job kit purely in data and leave the sprite alone, or reskin the
sprite and leave the kit alone.

Everything below was **verified empirically on this install** by extracting and parsing the real
game files (FF16Tools + a custom YOX parser), unless tagged otherwise.

---

## TL;DR

- All FFTO 2D art (unit sprites, portraits, UI) is packed in a single container:
  **`system/ffto/g2d.dat`**, which lives inside **`data/enhanced/0007.pac`** (and the classic
  variant under `data/classic/`).
- `g2d.dat` is a **YOX** container: a top header + **2450 sub-entries**, each addressed by an
  ordinal **index 0..2449**. Each sub-entry is `YOX header (0x10) + a zlib stream` that
  decompresses to FFTO's **internal indexed (paletted) texture format** — NOT a standard DDS.
- The mod loader overrides art **per entry index** by dropping a file at
  **`FFTIVC/data/enhanced/system/ffto/g2d/tex_<index>.bin`** in a Reloaded-II asset mod. You do
  not repack the whole `g2d.dat`; you supply replacement entries by index.
- The engine's working texture size cap is **512px**: the largest real entries are exactly
  **262144 bytes = 512×512 at 1 byte/pixel (8-bit indexed)**.
- **Reskin = replace the target job's sprite-sheet entry (and/or its palette entry) with new art**
  at the same index. Because it's in-place, the job→sprite mapping is untouched.
- The community **FFT IVC Sprite Modding Toolkit** (`FFTSpriteToolkit.exe`, Nexus) already decodes
  this exact YOX/indexed format to PNG, applies palettes, and repacks `tex_<index>.bin`. Use it for
  the art round-trip unless we choose to RE the indexed pixel format ourselves (see Open items).

---

## Where the art lives (verified)

```text
data/enhanced/0007.pac
  └─ system/ffto/g2d.dat        (0x00CF6120 ≈ 13.6 MB; compressed in the pac as ~9.4 MB)
```

Found by grepping the per-pac `*_files.txt` manifests Steam ships next to each `.pac`:
`0007_files.txt` is the only enhanced pac containing `system/ffto/g2d.dat`. (`0004` only has an
unrelated `nxd/debugfidg2d.nxd`.)

Extract it with FF16Tools (game type `fft`):

```bash
FF16Tools.CLI.exe unpack -g fft \
  -i ".../data/enhanced/0007.pac" \
  -f "system/ffto/g2d.dat" \
  -o work/sprite-extract
```

---

## The `g2d.dat` (YOX) container format (reverse-engineered here)

### Top header (offset 0x00)
```text
0x00  char[4]  magic  = "YOX\0"  (59 4F 58 00)
0x04  u32      0
0x08  u32      data size           = 0x00CEC800
0x0C  u32      entry count         = 2450   <-- number of sub-entries
0x10  u16      0x07E9 (2025)  \  build date-ish (year 2025, 0x0A = month 10)
0x12  u16      0x000A (10)    /
0x14  u16      0x0003
0x16  u16      0x0001
0x18  u16/u16  0x0010 0x0025   \  category/section counts (16,37,52,868) — not yet fully decoded
0x1C  u16/u16  0x0034 0x0364   /
... rest of the first 0x800 block is zero-padded.
```

### Sub-entries (start at 0x800, each aligned to 0x800)
Each of the 2450 entries is itself a YOX block:
```text
+0x00  char[4]  "YOX\0"
+0x04  u32      type            (observed: 2 for zlib-compressed graphics)
+0x08  u32      uncompressed size of the payload
+0x0C  u32      0
+0x10  ...      zlib stream  (starts with 78 9C); inflate to get the resource
```
Entries are laid out sequentially, each padded up to the next 0x800 boundary. The override
**index** is simply the ordinal position (0-based) of the entry, matching the loader's
`g2d/tex_<index>.bin` convention.

### Decompressed payload = FFTO indexed texture (NOT DDS)
Inflating an entry yields FFTO's own 2D resource, e.g. headers like `00 01 01 00 00 10 00 18 ...`
or `00 00 02 00 ...`. There is **no `DDS `/`TEX ` magic** — it is an indexed/paletted bitmap plus
palette in the engine's format. This is why a dedicated decoder (the toolkit, or our own RE) is
needed to view/edit the pixels.

### Empirical entry census (this install)
```text
total sub-entries        : 2450   (matches the count field)
zlib-decompressible       : 1442  (type 2; remaining 1008 are a different/empty encoding)
unique decompressed blobs : 1011  (many identical = blank/placeholder slots)

Top uncompressed-size buckets (size bytes × count → interpretation):
   262144 ×  16   512×512  8-bit indexed   <-- the 512px cap; biggest sheets
   131072 × 161   512×256  8-bit indexed   <-- bulk unit sprite sheets (likely)
   226304 × 133
   118784 × 139
   113152 × 147
   102400 ×  71
    81964 × 127   (magic 00 00 02 00)
     9282 × 181   (magic 00 01 01 00)
     4608 × 135
     3072 × 332   (palette-sized blobs, paired with sheets)
```
The sixteen 512×512 sheets cluster at **even indices 1552-1586** (1552,1556,1558,…,1586), each
interleaved with smaller odd-index entries (palette/auxiliary). That region is a coherent block of
large character sheets. The 161 entries of 512×256 are the most likely home of the generic-job unit
sprite sheets.

A full per-index manifest is generated at `work/sprite-extract/g2d_manifest.json`
(index, offset, type, uncompressed size, zlib-ok, payload magic).

---

## Reskin workflow (Calculator / Mime → new jobs)

Because we only **reskin in place**, the recipe is:

1. **Identify the entry index** of the target job's sprite sheet (and its palette entry). This is
   the one step that needs a visual decode — see Open items. With the Sprite Modding Toolkit this is
   its preview/extraction step; without it we must render the indexed format ourselves.
2. **Paint the new art** over the extracted PNG (respect 512px; keep/adjust the palette). Generic
   human jobs have **per-gender sheets**, so a full reskin handles both male and female.
3. **Repack** the edited art back into the engine's indexed format as `tex_<index>.bin`.
4. **Ship it** as a Reloaded-II asset mod alongside our existing mods:
   ```text
   fftivc.asset.genericchronicle/
     └─ FFTIVC/data/enhanced/system/ffto/g2d/
          tex_<calcSheetIndex>.bin
          tex_<calcPaletteIndex>.bin   (if recoloring)
          tex_<mimeSheetIndex>.bin
          ...
   ```
5. **Launch via Reloaded-II** (the loader writes `modded` files into the game `data/`; without it
   the custom sprites do not display). Keep this as its own asset mod, sibling to the neuter data
   mod and the codemod — art must not tangle with the formula layer.

The job→sprite mapping is **not touched** by a reskin: the engine still loads entry #index for that
job; we just changed what that entry contains.

---

## Tooling

- **FF16Tools.CLI** (`-g fft`) — unpack `g2d.dat` from the pac, and repack a finished asset mod.
- **FFT IVC Sprite Modding Toolkit** (`FFTSpriteToolkit.exe`, Nexus mod 20) — one-click extract of
  the YOX/indexed sheets to PNG/BMP with palette applied, an editor, palette tools, and repack to
  `tex_<index>.bin`. It already implements the indexed-format decode this doc stops short of.
- Our own parser: `work/sprite-extract/` holds the extracted `g2d.dat` and `g2d_manifest.json`
  (entry census above). A scripted extractor/repacker can be added under `tools/` if we decide to
  own the pixel-format decode instead of depending on the toolkit.

---

## Open items (what's NOT yet done)

1. **Index → unit/job identification.** We have the full 0..2449 index space and size census, but
   mapping a specific index to "male Calculator sheet" requires rendering the indexed pixels
   (palette association + the `00 01 01 00 …` / `00 00 02 00 …` pixel layout). Either use the
   toolkit's preview, or RE the indexed format to emit PNGs we can eyeball.
2. **Indexed pixel-format decode.** The `type`/header bytes likely encode width/height/bpp and the
   palette pointer; 1008 non-zlib entries use a second encoding still to be classified.
3. **Per-gender / animation coverage.** Confirm how many entries make up one job (idle + action
   frames, male/female) so a reskin replaces the complete set, not a single frame.

---

## Sources (community)

```text
Nenkai FFT mod guide (folder/override conventions, system/ffto/g2d/tex_<index>.bin):
  https://nenkai.github.io/ffxvi-modding/modding/creating_mods_fft/
Nenkai mod loader:        https://github.com/Nenkai/fftivc.utility.modloader
Sprite Modding Toolkit:   https://www.nexusmods.com/finalfantasytacticstheivalicechronicles/mods/20
Sprite replacement packs: https://www.nexusmods.com/finalfantasytacticstheivalicechronicles/mods/14
Zodi texture pack (fftivc.asset.* example):
  https://github.com/Zodi-ark/Final-Fantasy-Tactics-The-Ivalice-Chronicles-Texture-Pack
The Spriters Resource (FFT IVC rips):
  https://www.spriters-resource.com/pc_computer/finalfantasytacticstheivalicechronicles/
```
