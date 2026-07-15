# LT10 counter-path, status-stage, and miss-presentation results

## Purpose

Validate the pending LT10 probe against three live questions:

1. whether the candidate `0x30C798` hook is a Counter-specific execution path;
2. which result-record fields distinguish a status-effect resolution from a plain attack;
3. whether the current forced-miss presentation writes produce a visible `Miss` rather than numeric zero.

Raw evidence: `work/1783911817-lt10-counterpath-status-miss-live.log`.

## Test environment

- FFT Enhanced v1.5.0, Manual Save 05, random battle at Mandalia Plain.
- Reloaded profile isolated to `fftivc.utility.modloader` and `fftivc.generic.chronicle.codemod` (plus Reloaded shared hook dependencies).
- LT10 settings: DCL pipeline enabled; basic attacks use 50% hit chance; forced-miss presentation enabled; candidate Counter hook observe-only; status stage log-only.
- Test units: Rion (`0x80`, Ninja), Cloud (`0x32`, Samurai), Janus (`0x82`, Chocobo with Counter).

## Live sequence

1. Rion moved adjacent to Janus and selected Attack. The forecast still displayed 100% hit and 157 damage, with 100% Counter.
2. DCL rolled the attack as a miss (`roll=85`, `pct=50`).
3. The game displayed numeric `0`, then executed Counter anyway.
4. Counter entered the ordinary calculation/result pipeline as a basic attack from Janus to Rion. It rolled `32` against `50`, hit, and dealt 21 HP.
5. Cloud executed Kiyomori (`abilityId=81`, `actionType=0x13`) over four allies to produce a controlled multi-target status application.

## Findings

### Candidate Counter path

**Refuted:** the installed observe-only hook at `0x30C798` is not demonstrated as the Counter execution path. No `[DCL-CTRPATH]` record appeared during a visible Counter.

**Proven:** this Counter produced a normal calculation record and normal DCL result record:

- caster `0x82`, target `0x80`, basic `actionType=0x01`;
- DCL hit roll `32/50`, outcome `hit`;
- result `21`, followed by HP `277 -> 256`.

The Counter therefore needs an explicit discriminator inside or before the shared calculation/result pipeline; the candidate hook cannot currently supply it.

### Forced miss semantics and presentation

**Proven:** the hit-control layer can turn a forecasted 100% basic attack into a real miss. The result rewrite changed damage `157 -> 0` and prevented Janus HP loss.

**Refuted:** the current writes `presentation +0xD8 |= 0x800000` and `kind +0x1C0 = 0x06` are sufficient to display `Miss`. The visible result was `0`.

**Proven:** forcing the late damage result to miss does not cancel a Counter already scheduled from the forecast/earlier resolution phase. Janus countered after the forced miss. A complete hit rewrite must occur before reaction eligibility is decided, or separately suppress reactions for a rewritten miss.

The repeated basic-attack status-stage samples remained `ail=0`, `mask=0`, `kind=0`; `resFlag` was `0x81` for Rion's forced miss and `0x80` for Janus's successful Counter. These values are useful outcome-stage discriminators but are not yet assigned semantic names.

### Status resolution stages

**Strong:** Kiyomori exposes at least two result phases for the same target.

- Early Cloud record: `ail(+0x1A8)=0x002B`, `mask(+0x1D0)=0x00`, `kind(+0x1C0)=0x00`, `resFlag(+0x1E5)=0x08`.
- Later Cloud record: `ail=0x002B`, `mask=0x08`, `kind=0x08`, `resFlag=0x01`.

Other covered allies produced early records with `ail=0`, `mask=0`, `kind=0`, `resFlag=0x08`, while the visible Protect/Shell-style icons were applied. Therefore `ail +0x1A8` alone is not a complete status payload for all targets, and `mask/kind/resFlag` change by resolution phase rather than describing the ability statically.

## Next offline work

1. Trace all reads/writes of result offsets `+0xD8`, `+0x1A8`, `+0x1C0`, `+0x1D0`, and `+0x1E5` around the calculation consumer and presentation selector.
2. Trace reaction eligibility/scheduling backward from the visible Counter event and find a discriminator or cancellation point that precedes Counter execution.
3. Compare Kiyomori's ability-data status payload with the four per-target result records to locate the authoritative per-target applied-status representation.
4. Do not schedule another live test until those static traces produce a narrower hook or field hypothesis.
