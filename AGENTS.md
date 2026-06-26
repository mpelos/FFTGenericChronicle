# AGENTS.md

Generic Chronicle is a battle-rebalancing mod for **Final Fantasy Tactics: The Ivalice Chronicles**
(Steam, Enhanced), built on Reloaded-II. See `README.md` for the goal and `docs/modding/00-overview.md`
for the technical entry point.

## The rule that matters most: `docs/` vs `work/`

This repo separates **timeless truth** from **dated journey**. Always put a file on the right side.

### `docs/` — definitions only, never a chronicle

- `docs/` holds **timeless documentation**: what something *is* — engine facts, table/field
  definitions, formulas, specs, design decisions. Write in the present tense.
- **No temporal / journal content in `docs/`.** No dates in the body, no `Status:`, no
  `UPDATE (date):`, no "we tried / proved / discovered", no per-test logs, no next-step or TODO
  planning. If you are narrating *what happened and when*, it belongs in `work/`, not here.
- **One fact, one owner.** Don't restate a fact another doc already owns — cross-reference the
  owning doc. When you move or rename a doc, fix every link that points at it (and any tool that
  reads it by path).
- For engine facts that aren't certain, tag confidence inline — **Proven / Strong / Hypothesis /
  Refuted** — instead of telling the story of how you learned it.

### `work/` — the dated journal and scratch space

- `work/` is where the investigation lives: live-test logs, memory probes, checkpoints, handoffs,
  plans, and generated reports.
- **Every file in `work/` is named with a leading Unix timestamp.** Prefix the filename with the
  epoch seconds of creation, e.g. `1782485483-action-context-checkpoint.md` (generate with
  `date +%s`). This applies to every file with no exception — notes, generated reports, and data
  dumps alike.
- When a `work/` finding hardens into a durable fact, **promote it** into the right `docs/` file
  (stripped of the journey) and leave the raw evidence behind in `work/`.

## Repository layout

- `docs/modding/` — the engine manual of truth: how IVC's battle system works and what is editable.
- `docs/formula-balance/`, `docs/job-balance/`, `docs/deep-combat-layer/` — design tracks.
  formula-balance (policy v0.2) and the Deep Combat Layer are **intentionally parallel and
  divergent** — do not merge or reconcile them into one.
- `mod/fftivc.generic.chronicle/` — the Reloaded-II data package (NXD, TableData XML, ENTD).
- `codemod/` — the C# Reloaded-II code mod (runtime hooks + formula engine).
- `tools/` — Python helpers; `check_*.py` are design-validation gates, `dump_*.py` build baselines.

## Working norms

- **Data first.** Do as much as possible through data (NXD / TableData XML / ENTD); reach for the
  code mod only for math the existing formula catalog cannot express.
- **No new items or equipment.** The mod re-tunes existing content; it never adds new SKUs. Weapons
  gain identity through type, reach, and modifiers — not new entries.
- **The live game runs on Windows, not here.** This checkout is Linux: you cannot launch the game
  or run live memory tests from it. Those happen on the Windows machine and their results land in
  `work/`.
- Don't commit unless asked, and don't bundle unrelated changes into one commit.
