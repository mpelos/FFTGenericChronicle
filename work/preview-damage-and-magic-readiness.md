# Preview-damage lever + magic-path readiness (for the two custom-formula tests)

> ## ✅ RESOLVED — LIVE CONFIRMED 2026-06-28
> The full forecast display + result is SOLVED and coherent for **physical AND magic**. Key correction to
> the plan below: the displayed-number paint (Win 1) is **cosmetic** — the HP-bar reads the SOURCE field
> `obj+0x6`==`unit+0x1C4`, not the display buffer. The universal lever is a **poll-write of `obj+0x6`**
> (`PreviewForecastPoke*`), proven for any action; compute-time finalizer hooks (`PreviewForecastSource*`:
> `0x30637E` magic, `0x308D8F` physical candidate) make it first-open-clean. The result rides the pre-clamp
> at the **correct** RVA `0x30A66F` = decimal **3188335** (the earlier `3188847`=`0x30A86F` was a hex→dec
> typo that silently SKIPped). Live: Agrias→Ramza, attack + Fire each = 11% / 500 / bar→67 / −500 HP.
> Canonical write-up: **docs/modding/05 §11** + **docs/modding/06 (Preview display control)**. Profile:
> `work/battle-runtime-settings.coherent-poke-test.json`. Still open: magic always-hit (separate Faith roll).

Date: 2026-06-28 · Status: **preview-damage hook BUILT + compiles + profile validated; live validation pending.**
Context: prep for the two requested tests — (1) custom physical formula Agrias→Ramza applied in preview AND
resolved in result; (2) Fire (magic) formula applied in preview (damage + %) AND resolved on hit.

## Two discoveries (static, byte-grounded)

### Win 1 — the forecast DAMAGE number IS controllable (closes gap-analysis finding #1)
- Display buffer: **`0x1407832BE`** (RVA `0x7832BE`), 2 bytes below the hit% buffer `0x7832C0`.
- Single real-code writer: **`0x228488`** `66 89 15 2F AE 55 00` = `mov word [rip+0x55AE2F], dx` → 0x7832BE.
  Verified: 0x22848F + 0x55AE2F = 0x7832BE. RVA < 0x610000 = real/hookable.
- The value reaches the store via a **format-dispatch**: 10 branches each load a signed word from the
  forecast object (`+0x06/+0x08/+0x0A/+0x0C/+0x0E/+0x2A`, picked by flags `forecast[+0x27]`) into edx and
  **`jmp 0x228488`**. ALL branches park the value in **dx** (incl. the `movsx r8d,[rbp+0xE]` branch, via
  `mov edx,r8d` at 0x22808D → `cmovs` → jmp). So forcing **dx** at the store covers every action type.
- **Do NOT hook 0x228488 directly:** it is a jump target (all 10 branches land on it) AND RIP-relative —
  stealing it risks corrupting a landing / mis-relocating the disp. Instead hook the **terminal `jmp
  0x228488`** of the relevant branch (clean 5-byte E9, value already in dx) with ExecuteFirst: set dx, then
  the stolen jmp falls into the unmodified store → store writes our dx. No race (engine's own store).
- Branch terminal jumps (RVA → bytes): attack/damage **`0x2280D7` `E9 AC 03 00 00`** (primary, `[rbp+0xE]`);
  others numeric: `0x22802F` `E9 54 04 00 00` (`[rbp+6]`), `0x22806E` `E9 15 04 00 00` (`[rbp+0xC]`),
  `0x228125` `E9 5E 03 00 00` (status val `[rbp+0x2A]`), `0x2281E9` `E9 9A 02 00 00` (r9d), `0x228316`
  `E9 6D 01 00 00` (r9d). Format/zero paths (skip): `0x228050`, `0x228195`, `0x22817E`/`0x228188` (je/jne).
- NOTE: the fall-through path 0x228485 `and edx,0x7f` masks to 0–127 (glyph/format); damage branches jmp
  to 0x228488 and skip that mask, so 0x7832BE carries the full signed number on damage/heal branches.

### Win 2 — magic (Fire) rides the SAME control points as physical (static-confirmed)
- **Damage apply:** the dispatcher keys on effect-kind (`0x38A6F1 cmp edx,0x300` = apply HP/MP), not
  weapon-vs-spell. Producer `0x30F0C4` emits `0x300` for ANY connecting HP-mover → APPLY `0x30A51C` →
  pre-clamp `0x30A66F`. So **Fire HP-damage is rewritten by the existing pre-clamp lever, no new hook.**
  (MP cost rides the same routine via `+0x1C8`/`+0x1CA`.)
- **Forecast hit%:** the display copy (`0x227FEA mov rbp,[0x142FF3CF8]; movzx eax,[rbp+0x2C]; store
  0x228004→0x7832C0`) is category-agnostic → the hit% hook `0x227FFE` paints magic too. (Caveat: confirmed
  for the hit% number; that `object+0x2C` is the magic hit% for a Faith-resisted spell wants a live check.)

### Divergence — magic AVOIDANCE is a different roll (gap for always-hit)
- Physical evade input-control (zero `+0x46/+0x47/+0x4A/+0x4B/+0x4E`) was proven only for a basic physical
  attack. **Magic uses a separate Faith-based roll** (`0x304E33` caller of `0x278EE0`, reads Faith `+0x2C`),
  NOT the physical evade bytes. Zeroing physical bytes is **not** proven to make Fire always-hit. Magic-evade
  bytes `+0x48/+0x4C/+0x4D` are inferred/unconfirmed (`+0x4E` shield-magick is the only mapped one).
- ⚠️ "Desequipar reaction" does NOT remove magic-evade (it's avoidance, not a reaction).

## What was built this session
- `Mod.cs`: `InstallPreviewDamageControlIfEnabled` + `BuildPreviewDamageHookAsm` (twin of the hit% hook),
  fields `_previewDamageBuf`/`_previewDamageHook`, const `PREVIEW_DAMAGE_BUFFER_SIZE`, install call in Start,
  settings `PreviewDamageControlEnabled`/`PreviewDamageRva`(=0x2280D7)/`PreviewDamageExpectedBytes`/
  `PreviewDamageForcedValue`/`PreviewDamageLogOnly`. `RuntimeSettingsValidator.cs`: matching block.
  Compiles 0 errors/0 warnings (local `_build`, not yet deployed).
- Profile `work/battle-runtime-settings.preview-levers-test.json` (validated, errors=0): forces hit%=11 +
  damage=777 + pre-clamp log-only. Validates both preview levers on physical AND magic, probes magic path.

## Test plan / sequencing
1. **Lever validation (this build, NEW DLL):** deploy + run `preview-levers-test.json`. Agrias ATTACK preview
   on Ramza → expect 11% / 777. Agrias FIRE preview on Ramza → if 11% / 777, magic uses the same buffers
   (and the damage hook covers Fire); if Fire shows natural damage, Fire uses another branch (hook it too).
   Cast Fire → [PRECLAMP] log confirms magic damage path.
2. **Formula→preview wiring (NEXT build):** the preview hooks currently take a STATIC forced value. Wire the
   poll to compute the DCL formula for the currently-previewed (attacker,target) pair and feed the hook
   buffers (hooks read buffer instead of an immediate). Needs: read the previewed target identity from the
   forecast object. Then preview = formula.
3. **Test 1 (physical) full:** scoped to Agrias(0x1E)→Ramza(0x01) by charId (sidesteps the blocked general
   action-id). Formula in preview + pre-clamp force in result + evade=0 always-hit (proven physical).
4. **Test 2 (Fire) full:** same, after settling magic always-hit (probe: zero physical bytes → if Fire still
   magic-evades, also zero `+0x48/+0x4C/+0x4D/+0x4E`; whichever forces 6/6 hits = the magic-evade bytes).

## Action-id note (G5)
General magic action-id is BLOCKED (engine id at `actor+0x142` is observe-only, not a formula key). For
these SCOPED tests it's sidestepped by matching attacker+target charId + single action. Promoting `+0x142`
is the broader unlock (gap-analysis backlog #1).
