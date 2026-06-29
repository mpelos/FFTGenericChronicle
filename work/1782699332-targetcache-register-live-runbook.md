# Target-Cache Register Probe Live Runbook

Objective: identify whether the engine exposes the original incoming attacker/action at the
target-cache transition for reactions that interrupt the incoming action before HP apply
(First Strike / Hamedo-like behavior).

## Current State

- The next live profile is:

```text
work/1782698530-action-identity-targetcache-register-probe.json
```

- The profile has already passed offline validation and the full offline gate.
- Do not edit Reloaded-II enabled-mod JSON. The user controls which mods are enabled.
- Deploy only after `FFT_enhanced` and `Reloaded-II` are closed.

## Deploy Command

Run after the game and Reloaded-II are closed:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\build-deploy.ps1 -RuntimeSettings work\1782698530-action-identity-targetcache-register-probe.json
```

Expected behavior:

- Builds and deploys the code mod DLL.
- Copies `battle-runtime-settings.json` into the Reloaded-II code mod folder.
- Archives any previous `battleprobe_log.txt`.
- Does not enable/disable Reloaded mods.

## Mods To Enable

The user should enable only the Generic Chronicle code mod needed for this probe. Do not ask the
user to edit JSON directly. If a data mod is not required for the current probe, keep the setup as
minimal as possible.

## Preferred Live Test

First run the already-known reproducible case:

```text
Cloud -> Ninja with a basic attack.
Ninja reaction: First Strike / counter-before.
Expected preview: 100% hit chance.
Expected outcome: Ninja reacts before Cloud's attack.
```

This does not test `act > 0`, but it is the cleanest next test because earlier evidence already
shows the interrupted incoming surface on the Ninja target cache. The current probe specifically
tests whether that target-cache transition has register/stack actor refs for Cloud.

Best bonus case, only if easy and reliable: trigger First Strike or Hamedo using a named incoming
action, so the interrupted action has `act > 0`.

If a named incoming action does not trigger the reaction, do not spend time forcing it yet. The basic
Cloud -> Ninja First Strike case is still the priority proof.

User report format:

```text
attacker -> defender
action used
reaction expected/equipped
preview chance and damage
reaction result: fired before attack / parry / normal hit / other
UI damage and HP change for attacker
UI damage and HP change for defender
any status/effect/critical
then close the game
```

## What The Probe Logs

The profile logs three key surfaces:

- `[PENDING-ACTION-TARGET ...]` for target-cache state on potential pre-apply damage rows.
- `[HOOK-REGS-EVENT kind=targetcache ...]` at those pre-apply target-cache rows.
- Target-cache hook-register rows are emitted from both the normal unit-observation path and the
  global pending-action tracker refresh, so a target cache observed only during event refresh should
  still get a correlated register/stack snapshot.
- `[LANDMARK-HIT name=target-cache-write-1c4 ...]` and
  `[LANDMARK-HIT name=target-cache-init-1c4 ...]` around the known writes to the staged damage field.

Register/stack values that look like actor structs should print as:

```text
actor:id=0x..:unit=0x...:act=...
```

## Success Criteria

Strong proof:

- A target-cache hook or landmark row for the interrupted defender record contains an actor ref for
  the original incoming attacker with the expected action id before First Strike/Hamedo cancels it.

Useful negative:

- Target-cache hook/landmark rows appear, but all actor refs are target-only or unrelated. That
  means this cache transition is probably not the source-bearing frame, and the next search should
  move earlier to the reaction/roll dispatch.

Insufficient:

- Only line-near `[PRECLAMP-FORMULA-CANDIDATE]` hints appear. Those are useful for narrowing, but
  they are not authoritative enough to become the primary DCL action identity path.

## Post-Test Analysis

After the user closes the game, archive the active log with a new timestamp and run:

```powershell
python tools\analyze_action_identity_log.py <archived-log> > work\<timestamp>-targetcache-register-report.md
python tools\report_action_identity_coverage.py > work\<timestamp>-action-identity-existing-log-coverage.md
```

Review these sections first:

- `Target-Cache Register Verdict`
- `Register Actor Refs`
- `Hook-reg events`
- `Landmark hits`
- `Pending Target Caches`
- `Target Cache Source Hints`

Then update:

```text
work/1782680077-action-identity-goal-checkpoint.md
```

Promote only stable engine facts to `docs/modding/`; keep live-test chronology in `work/`.
