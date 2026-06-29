# Named Incoming First Strike / Hamedo Runbook

## Objective

Determine whether the same target-cache register surface that identifies the source of a basic
incoming First Strike / Hamedo-like interruption also exposes a named incoming action (`actionId >
0`).

This is the remaining gap after the basic First Strike proof:

- Basic incoming attack into First Strike is register-proven at target-cache time.
- The reaction damage itself resolves as the defender's own HP-applying action.
- A named incoming action interrupted before HP apply has not yet been observed.

## Why This Matters

The DCL needs to know the action being denied or preempted, not only the reaction damage. If the
incoming action is named and can be interrupted, the runtime needs at least:

- original source unit;
- original target unit;
- original action id;
- staged incoming debit/result flags;
- whether the original action should be cancelled, transformed, or allowed to continue under DCL
  rules.

If the target-cache frame only exposes the source unit but not `actionId > 0`, the shipping runtime
will need to join it with another surface: pending-action state, selector context, action-boundary
state, or a still-earlier native reaction dispatch hook.

## Current Profile

Use the existing observe-heavy target-cache profile:

```text
work/1782698530-action-identity-targetcache-register-probe.json
```

Deploy only after the game and Reloaded-II are closed:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\build-deploy.ps1 -RuntimeSettings work\1782698530-action-identity-targetcache-register-probe.json
```

The profile is not a damage rewrite profile. It does force live input conditions that make the
reaction branch easier to observe:

- Brave `+0x2B = 100` on all units, so Brave-gated reactions should fire when eligible.
- Evade bytes forced to zero, so ordinary evasion does not mask the reaction branch.
- Selector, pre-clamp actor context, target-cache hook-register, and landmark probes enabled.

Do not edit Reloaded-II enabled-mod JSON. Ask the user to enable only the code mod required for this
probe.

## Preferred Live Test

Use the same battle/save shape as the successful First Strike proof if possible.

Required defender:

```text
Ninja with First Strike equipped.
```

Try a named incoming physical action into the Ninja. Candidate order:

1. Cloud -> Ninja with `Braver`, if targetable.
2. Cloud -> Ninja with another single-target Limit, if `Braver` is not usable.
3. Any available adjacent/single-target named physical action into Ninja.

Do not spend a long session searching menus. If no named physical action is easily targetable into
the Ninja, stop this branch and move to the fallback section.

## User Instructions

Before confirming, record:

```text
attacker -> defender
action name
preview hit chance
preview damage/heal/status, if visible
whether the defender has First Strike equipped
```

After confirming, record:

```text
reaction fired before the named action? yes/no
UI damage on attacker, if any
attacker HP before/after, if known
UI damage on defender, if any
defender HP before/after, if known
did the named action still resolve after the reaction?
any critical/status/extra effect
then close the game
```

If the named action simply resolves normally and First Strike does not fire, that is useful but not
the proof we need. Close the game after one such attempt; do not grind repeats unless the first
attempt was ambiguous.

## Success Criteria

Strong positive:

- `Target-Cache Register Verdict` shows source-candidate refs for the original attacker, and at
  least one source-candidate actor ref carries the expected named `actionId > 0`.

Partial positive:

- Source unit is register-proven at target-cache time, but all refs are direct `unit` refs or
  `act=0/-1`. This proves source ownership but not named action identity. Next step: join with
  pending/action-boundary state or search an earlier reaction dispatch hook.

Useful negative:

- The named incoming action does not trigger First Strike. This means the current roster/setup is
  not a viable named-interrupt test. Move to the fallback branch instead of repeating blindly.

Hard negative:

- First Strike triggers, but target-cache hook-register rows contain no source-candidate refs. Then
  the basic proof may be specific to the basic-attack path, and the source hunt needs an earlier
  hook.

## Post-Test Analysis

Archive the live log under a new timestamp and run:

```powershell
python tools\analyze_action_identity_log.py <archived-log> > work\<timestamp>-named-first-strike-report.md
python tools\report_action_identity_coverage.py > work\<timestamp>-action-identity-existing-log-coverage.md
```

Read in this order:

1. `Target-Cache Register Verdict`
2. `Register Unit/Actor Refs`
3. `Pending Target Caches`
4. `Selector Outcomes`
5. `Actor Context Events`

The key row to find is a pre-apply target cache on the Ninja target record, followed by a
`HOOK-REGS-EVENT kind=targetcache` where source-candidate refs point to the original attacker.

## Fallback If Named First Strike Is Not Viable

If First Strike appears to trigger only on basic Attack in the available setup, stop trying to force
this branch and switch to the next DCL identity gap:

1. Multiple simultaneous pending actions.
2. Tile/epicenter target reconstruction for AoE.
3. Named no-HP selector outcome using a different reaction/control path.

The best next fallback is multiple simultaneous pending actions because it directly tests whether
the pending tracker can keep more than one open action batch and still match the right caster/action
to each final HP event.
