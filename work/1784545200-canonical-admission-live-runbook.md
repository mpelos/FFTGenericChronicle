# Canonical admission live runbook

Created: 2026-07-20

## Purpose

Prove that the guarded `0x281EF7` callback receives one complete ordinary Fire target sweep, builds
the configured DirectNumeric policy-ticket template, and settles the retained ActionInstance through
the canonical bridge without enabling post-apply, Reaction-completion, presentation, or other native
writer callbacks. Native selection, status, movement, AI, and presentation must remain natural.

This is the first live proof in `work/1784674066-dcl-live-proof-sequence.json`; the broader
clean-regression matrix stays blocked until this canonical-admission/template bridge is proven.

## Prepared fixture

- Runtime settings: `work/1784673033-battle-runtime-settings.canonical-admission-sentinel.json`
- Actions/states: `work/1784673033-dcl-actions.json`
- Item metadata: `work/1784673033-dcl-items.json`
- Ability bindings: `work/1784673033-dcl-bindings.json`
- Reaction bindings: `work/1784673033-dcl-reaction-bindings.json`
- Policy-ticket templates: `work/1784673033-dcl-policy-ticket-templates.json`
- Verified pre-action autosave fixture:
  `work/1784092904-fft-autoenhanced-snapshot.png` with sidecar
  `work/1784092904-fft-autoenhanced-snapshot.png.fixture.json`

The settings validator passes with zero errors. Expected warnings are limited to the guarded
admission hook still needing live proof. The sentinel includes a DirectNumeric policy-ticket
template for ability `16`, so a complete Fire admission should not remain parked as
`MissingTemplate`.

To refresh the fixture after a contract change, build the smoke-test executable and run:

```powershell
dotnet fftivc.generic.chronicle.codemod.smoketests.dll `
  --emit-canonical-admission-sentinel work <unix-timestamp>
```

The emitter serializes the same validated catalog exercised by the integrated smoke suite and writes
all canonical runtime files, including the policy-ticket template bundle, with the required
timestamp prefix.

## Preconditions

1. Close the game and Reloaded-II before deployment. Do not inspect or monitor unrelated Windows
   processes.
2. Deploy the current code-mod build and the prepared runtime settings through the ordinary stopped-
   application deployment workflow. Do not edit Reloaded-II's enabled-mod configuration directly.
3. Keep the admission test limited to the guarded admission/template bridge. Do not enable
   post-apply, Reaction-completion, Fear, Approach, hit-control, status-control, or formula rewrite
   switches.

Before deployment, run the offline fixture-readiness gate:

```powershell
python tools\analyze_dcl_canonical_admission_probe_readiness.py --check-only
```

This gate verifies that the sentinel settings reference existing canonical files, keep the live
profile admission-only, and bind `ability=16` to one DirectNumeric Fire action plus one matching
policy-ticket template.

To prepare the actual live run without editing Reloaded-II's enabled-mod configuration:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\prepare-canonical-admission-live.ps1 -DryRun
powershell -ExecutionPolicy Bypass -File codemod\prepare-canonical-admission-live.ps1
```

The helper runs the readiness gate, delegates settings validation/build/deploy to
`codemod\build-deploy.ps1`, and archives the old game-side `battleprobe_log.txt` so the next launch
starts with fresh evidence.

## Fast game route

1. Launch Enhanced through Reloaded-II.
2. Press Enter at the intro to skip it before it restarts.
3. Prefer **Continue** only when an exact canonical-admission pre-action autosave snapshot has just
   been restored and verified. A snapshot created for this proof should carry fixture metadata:
   `-FixtureKind canonical-admission-pre-action`.
4. Do not use **Load > Manual Saves > 05** as the repeated test entry point. The current save `05`
   is a world-map baseline, useful for constructing a fixture but not for the immediate Fire proof.
5. From the verified Josephine Black Mage fixture, use one ordinary Fire ability (`abilityId=16`)
   on one unit and let the complete animation/result finish.
6. Close the game normally.
7. Collect and analyze the fresh log:

```powershell
python tools\collect_dcl_canonical_admission_live_log.py
```

Anything that makes future tests faster or more reliable may be added to this runbook as it is
proved.

## Acceptance

The runtime log must contain all of the following:

- `[DCL-CANONICAL-ADMISSION-HOOK]` at RVA `0x281EFA`;
- a battle reset with `canonicalBattle=1`;
- one completed admission with `ability=16`, `strikes=1`, `targetCount=1`, and `complete=1`;
- a template build status of `Built` and a ticket/final bridge status of `Published` for the same
  ActionInstance;
- no `[DCL-CANONICAL-ADMISSION-ERR]`.

A positive later `[DAMAGE]` line for the admitted target is useful corroboration when present, but
it is not required for the bridge proof. To require it for a stricter delivery-adjacent run, pass
`--require-damage` to the analyzer/collector.

Copy the raw log into a new timestamp-prefixed `work/` file before analysis. Only after the exact
source, target, action, one-sweep cardinality, template build, and same-ActionInstance bridge
settlement pass should the hook boundary be promoted from Strong/static to Proven/live.

Use the offline analyzer for the mechanical gate:

```powershell
python tools\collect_dcl_canonical_admission_live_log.py
```

The analyzer requires the hook, canonical battle reset, exactly one completed `ability=16`
single-target/single-Strike admission, `Published`/`Built`/`Published`/`Published` status
continuity, and zero canonical admission errors. The optional `[DAMAGE]` line is emitted by the base
HP sampler on HP changes; `LogHpEventProbe` is not required for that stricter gate.

## Current automation blocker

The Windows computer-control skill failed to initialize its runtime with `failed to write kernel
assets: path not found`. This blocks autonomous UI execution of this runbook, not deployment
preparation, offline validation, or further DCL implementation. Retry the supported skill runtime;
do not substitute an unreviewed input-injection helper.
