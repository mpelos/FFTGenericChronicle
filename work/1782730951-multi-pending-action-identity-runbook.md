# Multiple Pending Actions Runtime Identity Runbook

## Objective

Validate whether the runtime can track more than one charged/pending action at the same time and
still assign each final HP event to the correct caster/action/target.

This is the next fallback branch if the named incoming First Strike / Hamedo test is not viable.
The aggregate evidence currently has `max active batches=1`, so simultaneous pending ownership is
still unproved.

## Why This Matters

The DCL cannot treat delayed actions as "the most recent pending caster" once more than one charged
action exists. The runtime needs to answer, at each HP apply:

- which pending batch is resolving;
- which caster owns that batch;
- which ability/action id owns that batch;
- which final target belongs to that batch;
- whether AoE/multi-target hits are separate target events inside the same batch;
- whether another pending action remains open while the first one resolves.

If this fails, charged spells/limits/summons become attribution hazards even when basic actor
context works.

## Profile

Prepared profile:

```text
work/1782730951-multi-pending-action-identity-probe.json
```

Deploy only after the game and Reloaded-II are closed:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\build-deploy.ps1 -RuntimeSettings work\1782730951-multi-pending-action-identity-probe.json
```

This profile is observe-only:

- no HP/MP rewrite;
- no preview rewrite;
- no hit/status/reaction control;
- no Brave/evade override;
- noisy action-state, pending, hook-register, actor-context, equipment, HP-event, and target-cache
  logs enabled.

Do not edit Reloaded-II enabled-mod JSON. Ask the user to enable only the code mod required for this
probe unless the current save requires another mod.

## Preferred Live Test

Use two delayed actions with different casters and, ideally, different action ids.

Best candidate if available:

```text
1. Cloud uses Cross Slash on target A or an AoE center.
2. Before Cross Slash resolves, Agrias casts Fire or Cure on target B.
3. Let both pending actions resolve.
```

Alternate candidates:

```text
1. Cloud uses Braver or Cross Slash.
2. Ramza/Agrias/another caster uses Fire, Cure, or another charged spell.
3. Let both resolve.
```

The exact actions matter less than creating overlap. The user should report each preview and each
resolution in chronological order.

## User Instructions

Before each confirmation, record:

```text
caster -> target or center
action name
preview damage/heal and hit chance
whether it is delayed/immediate in the turn order
```

While waiting for resolution, record:

```text
active unit after each Wait / action
which pending action is still visible in the timeline, if visible
```

After each pending action resolves, record:

```text
action that resolved
UI damage/heal per target
HP change per target, if known
active unit immediately after resolution
any miss/status/critical/extra effect
```

Close the game after both pending actions resolve.

## Success Criteria

Strong:

- At least one `[PENDING-ACTION-MATCH]` line reports `activeBatches > 1`, `trackedPending > 1`, or
  `trackedResolving > 1`.
- Each resolving HP event is matched to the correct caster/action id.
- If one action is AoE, all targets of that AoE share the same batch/action while the other pending
  action remains separate.
- `[PRECLAMP-ACTOR-CTX]` agrees with pending attribution at the HP apply frame.

Partial:

- Both actions resolve correctly, but the log never shows more than one active/tracked batch. This
  means the live choreography did not actually overlap from the runtime's point of view.

Failure:

- A HP event is matched to the wrong caster/action while another pending batch is active.
- Pending matches fall back to `none` despite visible pending action state.
- The same batch id is reused for two unrelated actions before both resolve.

## Post-Test Analysis

Archive the live log under a new timestamp and run:

```powershell
python tools\analyze_action_identity_log.py <archived-log> > work\<timestamp>-multi-pending-action-report.md
python tools\report_action_identity_coverage.py > work\<timestamp>-action-identity-existing-log-coverage.md
```

Read:

1. `Pending Matches`
2. `Actor Context Events`
3. `Pending Target Caches`
4. `Formula Candidate Sources`
5. aggregate `DCL Action-Identity Requirement Matrix`

The new `Pending Matches` table includes:

- `Active`
- `Pending`
- `Resolving`

The run is only a real overlap proof if at least one of those columns exceeds `1`.
