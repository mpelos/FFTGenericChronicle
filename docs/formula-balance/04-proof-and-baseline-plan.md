# Proof And Baseline Plan

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/formula-balance/00-envelope.md`
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
Review: Approved by Claude on 2026-06-20 after incorporating discriminating routine tests,
Formula/X/Y proof, noise controls, and the future formula simulation gate.
Amended: 2026-06-20, approved by Claude, to clarify that proof failures block final numeric and
implementation acceptance, not high-level formula planning.

## Purpose

This plan defines the next required Windows-machine validation session before concrete formula
numbers are finalized for implementation.

The project now has accepted conceptual guidance, and combat-formula design may continue while
this proof work is pending. Formula implementation and final numeric acceptance still depend on
proof. This plan aims to close the biggest evidence gaps in one focused session:

1. Capture baseline weapon data.
2. Prove the basic data-merge pipeline with a cheap ability smoke test.
3. Prove whether per-weapon `Formula` edits change the actual base weapon routine in game.
4. Prove whether `OverrideAbilityActionData.Formula/X/Y` affect damage, not only CT/MP/range.
5. Optionally confirm runtime struct offsets if Tier-2 work becomes necessary.

No concrete formula set should be implemented or treated as final before these checks are
complete or explicitly waived. Conceptual formula architecture, family identity, and simulation
drafts may proceed before this proof session.

## Machine Requirement

Run this plan on the Windows game machine, not this Linux checkout.

Expected local paths from existing docs:

```text
Game:        D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles
Mod loader:  C:\Reloaded-II\Mods\fftivc.utility.modloader
This repo:   D:\Projects\FFTGenericChronicle
FF16Tools:   D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-1.13.2-win-x64\win-x64\FF16Tools.CLI.exe
```

If any path differs, record the actual path in the session notes.

## Output Artifacts

Expected artifacts from this session:

- `work/baseline_weapons.csv`
- proof notes under `docs/formula-balance/proofs/` or `notes/`
- any generated test mod files under `mod/fftivc.generic.chronicle/`
- screenshots, save notes, or manual observations sufficient to reproduce the result

If generated binary game data cannot be committed for copyright or size reasons, record the
commands and hashes instead.

## Step 1 - Capture Weapon Baseline

Run:

```powershell
cd D:\Projects\FFTGenericChronicle
python tools\dump_weapons.py
```

Expected result:

```text
work/baseline_weapons.csv
distinct weapon Formula ids present (...)
```

This answers:

- which weapon formula ids appear in the current data;
- which weapon families currently use which formula ids;
- each weapon's WP, range, evasion, element, attack flags, and option ability id.

Decision impact:

```text
R1(a): already confirmed by schema; strengthened by real baseline values.
Taxonomy: can stop relying only on the working catalog for weapon-data facts.
```

Proof labels after success:

```text
Dependency: Tier-1
Proof state: weapon data captured on game machine
Confidence: high for schema/values, still medium for in-game behavior until proof patch
```

## Shared Noise Controls For Damage Tests

Any before/after damage test must control common FFT noise sources:

- same attacker and target unless the test intentionally varies one input;
- same facing/direction;
- neutral or recorded Zodiac compatibility;
- no elemental weakness, resistance, absorb, nullify, or strengthen unless that is the tested
  variable;
- no critical hit in the recorded sample set;
- Brave and Faith recorded and held constant unless they are the tested input;
- same status conditions;
- multiple samples per condition, recording min, max, mode, and any visible outlier.

One before/after hit is weak evidence. A useful proof varies the input that should matter under
the new routine and shows that damage follows the new routine's dependencies.

## Step 2 - Prove Basic Table Merge With A Cheap Ability Smoke Test

Goal: confirm the mod loader applies `OverrideAbilityActionData` edits at all.

Test design:

- Pick one easy-to-access ability.
- Change a low-risk visible field such as CT or MP.
- Deploy and verify the visible value or behavior in game.

Evidence to record:

```text
ability id/name:
field changed:
original CT/MP/visible behavior:
new CT/MP/visible behavior:
deploy path:
result: success/failure/partial
```

Decision impact:

```text
Success: proceed to weapon Formula and ability Formula/X/Y proof.
Failure: stop implementation proof and debug the data merge pipeline before relying on data-only
formula edits. Conceptual formula design may continue, but must be labeled unproven for
implementation.
```

Proof labels after success:

```text
Dependency: Tier-1
Proof state: in-game proof patch
Confidence: high for basic merge, not yet for Formula/X/Y damage fields
```

## Step 3 - Prove Per-Weapon Formula Routing R1(b)

Goal: confirm whether editing `ItemWeaponData.Formula` changes the actual base computation in
game, and whether the behavior carries cleanly.

Test design:

- Pick one easy-to-observe, low-risk weapon.
- Record its original `Formula`, `Power`, range, flags, and expected baseline behavior.
- Change only its `Formula` field to a visibly different existing weapon formula that uses a
  different input dependency.
- Keep `Power` and other fields unchanged if possible.
- Deploy through the mod loader.
- Attack a controlled target before and after the change.
- After the change, vary an input used by the old routine but not the new one, or vice versa.

Example discriminating test:

- Change a `PA * WP` weapon to a `WP * WP`-style routine.
- Use a setup where `PA != WP`.
- Record baseline damage.
- After the change, vary PA through job/equipment/level while keeping WP constant.
- If damage stops responding to PA and behaves like a function of WP, the replacement routine
  likely ran.

General rule:

```text
The test must vary an input that distinguishes the old routine from the new routine.
```

Good test candidates:

- a weak early weapon in a simple save;
- a weapon whose original formula and replacement formula should produce visibly different damage;
- avoid story-critical or rare endgame gear for the first test.

Evidence to record:

```text
weapon id/name:
original Formula:
test Formula:
attacker job/stats:
target job/stats:
before damage samples:
after damage samples:
input varied after edit:
expected dependency if old routine still runs:
expected dependency if new routine runs:
does behavior match the replacement routine? yes/no/partial
any animation, range, accuracy, or side effect mismatch:
```

Decision impact:

```text
R1(b) yes: Tier-1 weapon routine reassignment is a major design lever.
R1(b) partial: each family reassignment needs separate proof.
R1(b) no: family identity must rely on native routine + other data levers, or move to Tier-2.
```

Proof labels after success:

```text
Dependency: Tier-1
Proof state: in-game proof patch
Confidence: medium/high depending on how many formulas are tested
```

## Step 4 - Prove Ability Formula/X/Y Damage Override

Goal: confirm that `OverrideAbilityActionData` edits are not limited to CT/MP/range and that
`Formula`, `X`, or `Y` can change damage.

Risk: vanilla IVC uses the override table mostly for CT and MP. The existence of Formula/X/Y
columns is not proof that the engine consumes those columns for damage.

Test design:

- Pick one harmless, easy-to-access damage ability.
- Change `X`, `Y`, or `Formula` in a way that should visibly alter damage.
- Use the shared noise controls above.
- Prefer a discriminating change where the expected damage dependency changes, not only the
  magnitude.
- Record multiple samples before and after.

Evidence to record:

```text
ability id/name:
field changed:
original Formula/X/Y if known:
test Formula/X/Y:
attacker stats:
target stats:
before damage samples:
after damage samples:
expected dependency before:
expected dependency after:
deploy path:
result: success/failure/partial
```

Decision impact:

```text
Success: Tier-1 ability formula and skillset identity work can proceed.
Failure: stop treating Formula/X/Y as an available Tier-1 damage lever. Conceptual formula design
may continue, but implementation planning must re-scope the palette/taxonomy to
CT/MP/range/element/status plus weapon routine, or move formula work to Tier 2.
```

Proof labels after success:

```text
Dependency: Tier-1
Proof state: in-game proof patch
Confidence: high for the tested field, medium for untested fields
```

## Step 5 - Optional Tier-2 Readiness Check

Only do this if the session has time or if a near-term design requires Tier 2.

Goal: confirm that public runtime struct offsets and AOB signatures match this build.

Use the existing code mod:

```text
codemod/fftivc.generic.chronicle.codemod
```

Evidence to record:

```text
battleprobe_log.txt:
found signatures:
not found signatures:
module size/base:
struct offsets confirmed live? yes/no/partial
```

Decision impact:

```text
Success: Tier-2 feasibility improves, but formula hooks still require more RE.
Failure: Tier-2 remains possible but needs fresh signature work.
```

Proof labels after success:

```text
Dependency: Tier-2
Proof state: live confirmation on this build
Confidence: medium
```

## Step 6 - Update Design Docs After The Session

After the Windows session:

1. Commit `work/baseline_weapons.csv` if it contains only derived table data acceptable for this
   repo.
2. Update `03-family-taxonomy-and-viability.md` with verified weapon formula/value facts.
3. Update `00-envelope.md` if the proof changes Tier-1/Tier-2 assumptions.
4. Add proof notes or a small proof log document.
5. Ask Claude for review before moving to concrete formula proposals.

## Stop Conditions

Stop and debug before finalizing implementation formulas if:

- `dump_weapons.py` cannot find `ItemWeaponData.xml`;
- the mod loader does not apply changed data;
- weapon `Formula` edits do not affect in-game behavior as expected;
- CT/MP smoke-test ability overrides do not affect in-game behavior;
- ability `Formula`, `X`, or `Y` overrides do not affect damage;
- the running game version differs from the documented target in a way that changes data schemas.

These stop conditions do not block high-level formula planning, architecture discussion, or
simulation drafts that assume full attacker/target access. They block final implementation claims
and concrete numeric acceptance.

## Future Formula Simulation Gate

After this proof plan runs, every concrete formula proposal must include simulation before it can
be accepted.

The detailed simulation protocol lives in `docs/formula-balance/05-formula-proposal-protocol.md`.
That accepted protocol is now the gate for formula proposals. No concrete formula can move
beyond exploratory status unless it follows that protocol.

Minimum simulation requirements:

- early-game, mid-game, and late-game scenarios;
- multiple attacker profiles, including at least one strong user and one average user;
- multiple target profiles, including light, durable, and magically relevant targets where
  applicable;
- different equipment contexts for the family being tested;
- comparison against the current FFT/IVC baseline, or WotL fallback when IVC values are not yet
  captured;
- explicit input table, formula, expected damage, and conclusion.

For formulas with randomness or evasion:

- report min, max, mode or expected value where possible;
- document hit-rate assumptions;
- separate damage variance from accuracy variance.

Review rule:

```text
GPT must document the simulation.
Claude must review the simulation critically and recompute a subset through an independent path
before approving any non-trivial formula.
No formula is accepted on concept alone.
```

## What Remains Blocked Until This Plan Runs

- final numeric weapon formulas;
- final per-family formula assignments;
- claims that weapon routine reassignment works in-game;
- claims about specific weapon values in IVC;
- Tier-2 custom damage formula design beyond conceptual exploration.
