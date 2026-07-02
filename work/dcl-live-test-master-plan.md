# DCL live-test master plan — consolidated after the 2026-07-02 offline sweep

Every open front in `docs/modding/08-dcl-information-requirements.md` was advanced offline as far
as static analysis, snapshot mining, and internet research allow. What remains on each front is a
LIVE confirmation. This file is the single ordered checklist; the detailed per-front designs live in
the source docs referenced below. Nothing here has been run yet.

Ground rules (unchanged): probes are read-mostly or single-field pokes; mod-code changes require the
game closed for deploy; enable only `fftivc.utility.modloader` + `fftivc.generic.chronicle.codemod`.
Claude builds/deploys/reads everything; the user only performs the in-game GUI steps.

> **STATUS 2026-07-02: Session 1 EXECUTED (LT1+LT2, one battle) — results in
> `work/lt1-mega-probe-plan.md`.** Items 1-6 resolved: turn owner/position/tile/status/immunity/JP
> all PROVEN; `0x14186AFF0` refuted as action id (it is the reaction-eval id); `g_7B07AC` poke
> refuted (compute-time rewrite). Session 2 (LT3) remains, updated by the results: the primary hook
> is now `computeActionResult 0x309A44` (preview-time ability id + AI discriminator), plus magic
> always-hit `0x304E2B`, status hook `0x30662C`, reaction gates.

## Session 1 — pure external reads/pokes (NO mod rebuild needed)

These need only the running game + external memory probes.

1. **Action-id global (highest value).** With a preview open, read `word[0x14186AFF0]` (ability id)
   and `dword[0x14186AFF4]` (caster index) for: basic attack, a named instant skill, a charged
   spell, an item. Expect the selected ability's id at preview-open time.
   → `work/dcl-action-id-candidates.md` §5 (items 1-3).
2. **Turn-owner marker.** Poll `unit+0x1B8` across several turns; expect the exactly-one invariant
   to track the acting unit, including AI turns.
   → `work/dcl-unit-state-candidates.md` §6 step 6.
3. **Position/facing + TILE MAP.** Read `+0x4F/+0x50/+0x51` before/after a Move and a facing change;
   cross-check the pending epicenter `+0x1AC/+0x1B0` when targeting a tile. Tile table (found
   2026-07-02, `work/dcl-tilemap-candidates.md`): dump `0x140D8DCB0` + dims `0x140C6AD6A/6B`,
   cross-check record `+2` (height) against units at visibly different heights, watch the `+5` mark
   byte during AoE targeting (if `0x40` marks highlighted tiles, AoE membership is a direct read);
   bridge tile checks the level bit (unit `+0x51` bit 7).
   → `work/dcl-unit-state-candidates.md` §6 steps 4-5; `work/dcl-tilemap-candidates.md` §6.
4. **Status bitmap sweep.** Cast known statuses (Poison, Protect, Haste, Blind...) and diff
   `+0x57..0x65` and `+0x1EF..0x1F3`; verify the PSX bit layout table; write-test one immunity byte
   (`+0x5C..0x60`) against a status attempt.
   → `work/dcl-unit-state-candidates.md` §6 steps 1-3.
5. **Status-infliction poke.** During a status-carrying action, poke `dword[0x1407B07AC]` (staged
   status hit-%) to 100 then 0; expect guaranteed apply / guaranteed resist; watch `+0x1D0` mask,
   `+0x1C0=8`, ailment id at target `+0x1A8`.
   → `work/dcl-magic-status-reaction-candidates.md` Goal 2 plan.
6. **JP / job-level candidates.** Read `unit+0xF0`/`+0x11E` word arrays (index `jobId-0x4A`) and the
   nibble table `+0xE4..0xEE` for units with known job histories; end a battle with a JP gain and
   diff. (Roster-block scan via menus is the fallback if the battle-struct candidates fail.)
   → `work/dcl-unit-state-candidates.md` §6 step 7; `work/dcl-ai-and-progression-notes.md` §B.4.

## Session 2 — log-only hooks (one mod rebuild, game closed to deploy)

Build a probe profile with ExecuteFirst LOG-ONLY hooks; then one battle exercising each path.

7. **Magic always-hit lever.** Hook `0x304E2B` (function `0x304DF0`, roll at `0x304E33`): log
   natural `edx` (Faith-scaled chance), then a second run forcing `edx=100` — expect Fire to never
   miss; also log the miss-branch stamp `+0x1C0=0x06`.
   → `work/dcl-magic-status-reaction-candidates.md` Goal 1 plan.
8. **Reaction surfaces.** Log reads of the reaction bitfield `unit+0x94..0x97` at dispatcher
   `0x30B584` and the four Brave-gates (`0x30BE54/AC/FC`, `0x30BF48`); trigger a Counter and a
   Blade-Grasp-style reaction; then force one gate (Brave=100/0) to prove suppression/forcing.
   → `work/dcl-magic-status-reaction-candidates.md` Goal 3 plan.
9. **Action-id at apply time.** In the pre-clamp callback, log `word[0x14186AFF0]` +
   `actor+0x142` + caster ptr `qword[0x14186AF78]` per HP event; expect all three coherent for
   basic, named-instant, charged, AoE (constant across the per-victim event sequence), and
   dual-wield (2 events).
   → `work/dcl-action-id-candidates.md` §5 (items 4-6).
10. **AI same-calc detection.** During an ENEMY think phase, poll/log staged fields
    (`target+0x1C4`, `+0x1C0`, `+0x1EA`) and `0x14186AFF0` — if they pulse across candidate targets
    before the AI commits, the AI uses the shared staged pipeline and every input-side DCL write is
    AI-visible; instrument the old "roll-verdict burst" site as the candidate anchor.
    → `work/dcl-ai-and-progression-notes.md` §A (detection plan).

## Deferred (design/code work, not discovery)

- **DCL catalog build** — extraction script for the EXISTS columns + authored JSON for damage type,
  Weight, armor DR, weapon modifiers; widen `ItemCatalogEntry` to stop dropping CSV columns.
  → `work/dcl-catalog-coverage.md` §4.
- **WotL base ability action table** — DONE: `work/wotl_ability_action_baseline.csv` (512 rows,
  368 Normal abilities fully populated, id alignment 1:1 verified); becomes the DCL
  ability-metadata baseline.
- **Batch state** — engine has no hit index; DCL owns batching keyed on (caster, ability id,
  resolution window). Design only.
- **Event seed** — derive per-hit determinism from (caster idx, target idx, ability id, batch
  ordinal); design only.

## Success criteria

Each numbered item flips its doc-08 row from **Strong (static)** / **Hypothesis** to **Proven** (or
refutes it, which is equally useful). Items 1, 7, 5 are the highest-leverage: together they close
the last unowned control paths (universal action id, magic avoidance, status contest).
