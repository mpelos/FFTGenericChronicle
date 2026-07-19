# DCL v4 Fire AoE live gate

## Purpose

This is the next bounded live gate after the offline v4 closure. Fire `16` is not a Fear carrier.
It is the independent falsifier for the pre-confirm private native builder: the returned expanded
target list must exactly equal every unit visibly highlighted by Fire's area forecast.

Player-confirm enforcement stays disabled throughout this gate. The test cannot reject an action or
prove the full voluntary Fear filter; it only establishes whether the target source is authoritative
enough to arm that later test.

## Exact paired profile

- runtime/data contract: `work/1784399746-dcl-unified-sentinel-v4-runtime-data-pair.json`
- runtime settings: `work/1784399746-battle-runtime-settings.dcl-unified-sentinel-v4.json`
- settings SHA-256: `D7DA5E42D498C60DBA5596F9528F40F19973B05C8E269AD6E4A411D0F078E278`
- action-data NXD SHA-256: `44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9`
- Fear carrier: Fervor `53`, native Berserk `2/0x08` to Fear/Chicken `2/0x04`
- `DclFearPlayerConfirmEnforcementEnabled=false`

## Mandatory process-free preflight

Run:

```powershell
python tools/validate_dcl_live_install.py
```

The test may launch only after this reports `DCL live installation preflight PASS`. The validator
checks the runtime/data pair itself, exact Reloaded enabled-mod configuration, installed settings,
installed action-data NXD, item/weapon and charge/aim tables, runtime item/ability catalogs, and
installed code-mod DLL. It does not inspect or diagnose processes.

The current installation fails for six independent reasons:

1. `fftivc.generic.chronicle` is absent from `EnabledMods`;
2. installed runtime settings do not match v4;
3. installed action data do not match the paired v4 NXD;
4. installed `ItemWeaponData.xml` does not match v4;
5. installed `AbilityChargeAimData.xml` does not match v4;
6. installed code-mod DLL does not match the current Release build.

The two runtime catalogs already match. After FFT and Reloaded are visibly closed, the transactional
installer can prepare the exact bundle with backups and an automatic post-install preflight:

```powershell
python tools/install_dcl_live_bundle.py
python tools/install_dcl_live_bundle.py --apply
```

The first command is a dry run. The second is the only mutating command and must not be used while
either application is visibly open.

Therefore another launch in the current installation would be invalid and must not be counted.

## In-game action

1. Start the prepared battle fixture.
2. On Josephine's turn, choose Black Magicks > Fire.
3. Center Fire on adjacent Arthur with at least one additional unit visibly inside the highlighted
   area when possible.
4. Record the complete visible affected-unit set before confirming.
5. Confirm Fire once, then close the game after the resulting rows are flushed.

## Required log evidence

The decisive `[DCL-FEAR-CONFIRM]` row must contain:

- `casterSource=turn-owner`;
- Josephine's exact `casterIdx` equal to `turnOwner`;
- `type`/`ability=16`;
- `listAuthoritative=1`;
- `expandedTargetCount` equal to the visible affected-unit count;
- `expandedTargets` equal to the complete visible affected-unit set;
- `decision=allow` because enforcement is disabled and the caster is not a Fear owner.

Every visible target must appear exactly once, and no non-highlighted unit may appear. Missing or
extra targets refute this authority source. Only an exact match permits a later profile to arm
`DclFearPlayerConfirmEnforcementEnabled`.
