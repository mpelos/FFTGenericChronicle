# Hit/Miss/Block/Parry — control-mode implementation plan (shovel-ready)

Date: 2026-06-26 · Status: **spec ready; gated on observe-probe live validation first**
Companion to: `work/hit-miss-control-breakthrough.md` (the RE + definitive conclusions) and the
already-built observe-only selector probe (`work/battle-runtime-settings.result-selector-probe.json`,
hook at `0x205210` in `Mod.cs`).

> **Order of operations (do NOT skip):** validate the observe probe live FIRST — confirm the
> `[SELECTOR-PROBE]` evade-type (`cl` / record `+0x1C0`) values for a real hit / miss / dodge / parry
> / block. Only then enable the control writes below, because the exact evade-type→animation values
> are MEDIUM-confidence until the capture confirms them.

## What "control" needs (two writes, one already exists)

| Capability | Field | Hook | Status |
|---|---|---|---|
| Force damage = D / force 0 (hit lands or "no damage") | `word[unit+0x1C4]` (+heal `+0x1C6`) | existing **PreClampDamageRewrite** at `0x30A66F` | ✅ already implemented |
| Render native **miss/dodge/parry/block** | evade-type `byte[record+0x1C0]` (+ result-code `byte[record+0x1BE]=0`) | extend the **selector hook** at `0x205210` | ⬜ to build (this plan) |

So the only NEW code is a guarded write of `+0x1C0`/`+0x1BE` on the result record at the selector
hook we already installed. Damage forcing is the proven pre-clamp path.

## DCL integration logic (how the two combine at runtime)
Per attack, after the engine stages its (virtualized) result, our hooks apply OUR DCL decision:
- **DCL = HIT for D:** ensure `+0x1C4 = D` (pre-clamp), leave evade-type `+0x1C0 = 0x00` (hit). Engine
  applies D and shows the damage popup.
- **DCL = MISS/dodge/parry/block:** set `+0x1C4 = 0` (pre-clamp, no HP change) AND set `+0x1C0` to the
  evade-type for the animation we want (`0x06` miss / `0x04` block / `0x01-3` guard) with `+0x1BE = 0`
  (no-damage result). Engine plays the native evade animation.
- The vanilla roll is forced ~always-hit upstream (data layer: target evade bytes → ~0, or ability
  `Evadeable` off) so the engine reliably reaches the apply/selector path and our hook is authoritative.

## Settings delta (extend the existing ResultSelectorProbe block in `RuntimeSettings`)
All default OFF / LogOnly; mirror the `PreClampDamageRewrite*` safety envelope.

```jsonc
"ResultSelectorControlEnabled": false,   // master; false => observe-only (current behavior)
"ResultSelectorControlLogOnly": true,    // SAFETY: true => log intent, never write
"ResultSelectorControlMaxWrites": 1,     // 1..32 cap on total writes per session
"ResultSelectorControlTargetCharId": -1, // -1=any; else require record[+0x00] charId == this
"ResultSelectorControlMatchEvadeType": -1, // -1=any; else only act when current +0x1C0 == this
"ResultSelectorControlForceEvadeType": -1, // -1=no change; else byte -> [record+0x1C0]
"ResultSelectorControlForceResultCode": -1 // -1=no change; else byte -> [record+0x1BE] (0 for evade)
```
Validator (`RuntimeSettingsValidator.cs`, beside `ValidateResultSelectorProbe`): Warn (strong) when
`...ControlEnabled`; `MaxWrites` 1..32; `TargetCharId`/`MatchEvadeType`/`ForceEvadeType`/`ForceResultCode`
each -1 or 0..255; Error if `...Enabled && !LogOnly` and both Force* == -1 ("control on, nothing to force").

## Hook change (extend `BuildResultSelectorProbeAsm` at `0x205210`)
The observe hook already runs ExecuteFirst with `r8`=actor and resolves `r8 = [r8+0x148]` = record.
Add, AFTER the record is resolved (non-null) and BEFORE the original prologue runs, a guarded block
that writes `+0x1C0`/`+0x1BE`. Guard chain (fail-closed, mirrors PreClamp static path):
1. if `ResultSelectorControlEnabled == 0` or `LogOnly == 1` → skip to record/store (observe only).
2. `cmp [rax+SEL_CTRL_WRITES], MaxWrites; jge skip`.
3. if `TargetCharId >= 0`: `movzx ecx, byte[record+0x00]; cmp ecx, id; jne skip`.
4. if `MatchEvadeType >= 0`: compare incoming `cl` (saved) / `byte[record+0x1C0]`; `jne skip`.
5. inc write counter; record the event (so every forced write is logged).
6. if `ForceEvadeType >= 0`: `mov byte[record+0x1C0], val`.
   if `ForceResultCode >= 0`: `mov byte[record+0x1BE], val`.
All immediates baked at build time from settings (same as the pre-clamp builder bakes its values).
Damage forcing stays in the existing pre-clamp hook (no change there).

## Why it can't brick a battle
- Two explicit opt-ins required (`Enabled=true` AND `LogOnly=false`); ship LogOnly dry-run first.
- `MaxWrites` caps blast radius; `TargetCharId`/`MatchEvadeType` confine to one unit/outcome.
- `ExpectedBytes` (already enforced at `0x205210`) refuses a shifted/patched build.
- We only nudge fields the engine itself already owns and reconciles (`+0x1C0`/`+0x1BE`/`+0x1C4`); we
  never invent state. Fail-closed on any null/mismatch.

## Evade-type values to write (CONFIRM with the observe capture before trusting)
From the deeper RE (selector switch on evade-type when `+0x1BE==0`): `0x00`=hit (no anim),
`0x04`=block/guard (anim `0x13`), `0x01/0x02/0x03`=guard variants (anim `0x12`/`0x13`), `0x06`=plain
miss. The live `[SELECTOR-PROBE]` capture will give us the ground-truth value for each on-screen
outcome — wire those exact numbers into `ForceEvadeType`.

## Profiles to ship after validation
- `work/battle-runtime-settings.result-control-dryrun.json` — `ResultSelectorControlEnabled=true`,
  `LogOnly=true`, `MaxWrites=1`, a placeholder `TargetCharId` (rehearsal; confirms the match fires on
  the right unit/outcome before any write).
- `work/battle-runtime-settings.result-control-live.json` — `LogOnly=false`, `MaxWrites=1`,
  `ForceEvadeType=<confirmed>` (single-shot live proof that a forced evade animation renders).

## Open dependency / risk
The whole control path assumes the live capture confirms (a) the selector hook fires at resolution
with a meaningful `+0x1C0`, and (b) writing `+0x1C0` before `0x205210` reads it changes the rendered
animation. Both are MEDIUM-confidence from static RE; the observe probe + one LogOnly→live test close
them. If writing `+0x1C0` at the selector turns out too late (value already consumed), fall back to the
staging write site `0x205B39` (`mov [rdi+0x1C0], ah`) — hook there instead.
