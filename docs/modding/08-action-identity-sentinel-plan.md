# Action Identity Sentinel Plan

This is the offline implementation plan for the next missing runtime dimension: action identity.
The CT resolver gives us the real attacker; sentinel bands give us a coarse "what kind of action
was used" signal before the true action id is mapped.

## Current Truths

- Runtime formulas can already read target and attacker stats live.
- The real attacker is resolved by CT reset (`+0x41`), with a counter fallback now implemented by
  inverting the previous HP-damage pair.
- Death is engine-owned. The code mod writes numbers and uses `MinHpFloor=1`; the engine performs
  the real KO on a later vanilla chip.
- `ActionSignalRules` already decode observed `vanillaDamage` into `action.*` variables.

## New Opt-In Data Mode

`tools/build_neuter_data.py` now has:

```powershell
python tools\build_neuter_data.py --placeholder-mode sentinel-coarse-v1
```

Default remains `uniform`, which is the proven live neuter:

```text
weapon Power = 1
damaging ability X/Y = 1
Aim/Charge Power = 1
```

`sentinel-coarse-v1` emits distinct placeholder magnitudes instead:

```text
low  = 1
mid  = 4
high = 7
```

These are not action ids. They are a calibration channel: the engine computes a small-ish vanilla
number from the band value, and the runtime decodes the observed damage range back into variables
such as `action.swing`, `action.thrust`, or `action.magical`.

## Coarse V1 Taxonomy

Weapon `ItemWeaponData.Id` is mapped back through `ItemData.AdditionalDataId` so we classify by real
item category instead of raw weapon-data id.

```text
low/swing:
  Knife, NinjaBlade, Sword, KnightSword, FellSword, Katana, Axe, Flail, Rod, Staff, Bag, Cloth

mid/thrust-or-missile:
  Pole, Polearm, Crossbow, Bow, Gun, Book, Instrument

high/magical:
  damaging abilities whose AIBehaviorFlags include MagicalAttack, AffectedByFaith, or Reflectable

mid/physical-skill:
  damaging abilities whose AIBehaviorFlags include PhysicalAttack, Melee3Directions,
  Ranged3Directions, or NonSpearAttack

low/generic-skill:
  remaining offensive HP abilities, including high-id families whose AbilityData flags are too weak
  to classify confidently yet
```

Aim/Charge high-id fallback uses the mid band.

## Runtime Decode Profile

The live calibration profile is:

```text
work/battle-runtime-settings.sentinel-coarse-v1.json
work/runtime-simulation.sentinel-coarse-v1.json
```

It decodes:

```text
1..30  -> action.sentinelLow,  action.swing,   action.cut
31..60 -> action.sentinelMid,  action.thrust,  action.impale, action.missile
61..90 -> action.sentinelHigh, action.spell,   action.magical
```

The profile is wired for live safety architecture (`ResolveAttackerByCt=true`,
`ResolveCounterFromRecentDamage=true`, `MinHpFloor=1`) and traces the decoded variables. It is still
a calibration candidate: use high-HP controlled targets until the observed bands are proven not to
overlap for the tested families.

## Honest Limits

- The observed damage is not the raw band value. FFT formulas multiply the placeholder by PA, MA,
  Speed, Brave, or weapon-specific logic, so live ranges can overlap.
- The `1..30`, `31..60`, and `61..90` decode ranges are starting bands, not final truth.
- The high band can be unsafe on low-HP targets because vanilla damage lands before our poll rewrite.
- Throw, Jump, Aim/Charge, Cloud/Materia Blade, Gravity/% damage, and other formulas that ignore
  `Power` or `X/Y` remain explicit neuter/action-identity risk areas.
- The correct long-term path is still finding true action id or pre-damage context. Sentinel bands
  are the viable bridge while RE continues.

## Offline Contract

Run these before any live calibration:

```powershell
python tools\test_neuter_data.py
python tools\test_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.sentinel-coarse-v1.json work\runtime-simulation.sentinel-coarse-v1.json --no-trace
```

The full gate includes them through:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```

For the live setup path, use the dedicated helper first in dry-run mode:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\prepare-sentinel-coarse.ps1 -DryRun
```

When run without `-DryRun` and with Reloaded-II/FFT closed, it rebuilds the data neuter with
`--placeholder-mode sentinel-coarse-v1`, validates and simulates the runtime profile, deploys the
data/code mods, archives the old log, and prints the watcher command for low/mid/high-band proof.
