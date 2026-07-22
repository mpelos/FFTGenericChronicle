# Native PhysicalDamage policy-ticket template

## Context

PhysicalDamage was the last major canonical native family without template coverage. It is the riskiest family because the policy source owns weapon identity, target context, per-Strike weapon skill, defense candidates, ranged route verdicts, status/Injury policy, optional Reaction candidates, optional per-Strike weapon overrides, optional Protection redirect, and optional Skill Training policy.

## Hypothesis

PhysicalDamage can be admitted into the policy-ticket template surface if the template carries the complete `DclCanonicalNativePhysicalActionPolicySource` explicitly and the loader only validates structure. It must not derive weapon, defense, protection, or Skill Training facts from formula shape, equipment, or job draft content.

## Validation

- Added `PhysicalDamage` as an explicit family-policy source in the strict policy-ticket template surface.
- Added loader validation for valid weapon identity, at least one target policy, at least one Strike policy, valid target identities, nonnegative penalty magnitudes, nonnegative Strike indexes/skills/displacement, explicit defense candidate lists, valid ranged route values, mutually exclusive single-origin versus conditional-origin Injury movement branches, valid nested Injury movement branches, and valid optional StrikeWeapon identities.
- Added smoke coverage that captures a complete Physical NativeRepeat action, builds a PhysicalDamage policy ticket from JSON, publishes it into the retained policy-source ledger, and leaves the retained native carrier unexecuted.
- Added a negative smoke case proving a PhysicalDamage template with no Strike facts fails during strict load.

## Result

PhysicalDamage policy-ticket template production is offline-proven for explicit policy facts and complete NativeRepeat admissions. The template surface now covers all canonical native families currently routed through the policy-source template builder; remaining work is production/binding of those facts from live/native owners, not the strict ticket intake format.
