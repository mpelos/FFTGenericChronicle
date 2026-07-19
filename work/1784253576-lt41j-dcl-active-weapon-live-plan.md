# LT41J DCL active-weapon capture live plan

## Scope

This is the single remaining integration regression for native Dual Wield hand identity. Static
analysis already proves the native carrier and selector; this test checks only that the expanded
managed calc-entry ring captures and propagates those values in the live process.

No job policy or job content is involved.

## Fixture and isolation

- Autosave: `work/1784172418-dcl-dual-wield-fast-visible-fixture.png`, SHA-256
  `B4DC074EA2344168D3F88CBDAB487C381D0A79A457B89A39BC8FF96FA66E0FED`.
- Runtime profile:
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Isolated Reloaded config:
  `work/1784161260-appconfig.synthetic-reaction-isolated.json`.
- Independent external backup:
  `C:\Users\mmpel\AppData\Local\Temp\gc-lt41j-1784253576`.

The backup must contain DLL, PDB, runtime settings, Reloaded AppConfig, Enhanced autosave, and the
pre-existing game log. The game and Reloaded-II must both be stopped before deployment.

## Hypothesis and falsifier

For Rion's mixed Iga Blade `17` / Koga Blade `18` ordinary Dual Wield pair:

- state `0x2A` carries repeat `0/2`, native weapons `17/18`, active weapon `17`;
- state `0x2F` carries repeat `1/2`, native weapons `17/18`, active weapon `18`;
- both rows may retain order payload `18`.

Any different count, index, normalized weapon, or active weapon falsifies the managed integration
even if damage delivery still succeeds.

## Action and stop rule

Use the proven fast **Continue** load and **Auto-battle > Attack Enemy > Wenyld** route. Allow the
established Throw, archer action, Choco Beak, synthetic Counter, and later ordinary Dual Wield
Attack. Stop immediately after the first ordinary payload-`18` state-`0x2A -> 0x2F` pair appears,
or on any crash, hook-install failure, unexpected lethal result, or contrary carrier value.

## Machine gate

Archive the game log and run:

```text
python tools/analyze_dcl_active_weapon_live.py <log> --payload 18 --expected-right 17 --expected-left 18
```

Close FFT Enhanced, restore and hash-verify all six external artifacts, and leave Reloaded-II
closed unless another explicit live test immediately follows.
