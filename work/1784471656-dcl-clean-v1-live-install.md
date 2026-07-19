# DCL clean-v1 exact live installation

## Scope

The canonical job-free clean-v1 mechanism profile was installed after FFT and Reloaded had no
open windows. No host-process inventory was consulted. The installation changes only the exact
Reloaded Enhanced profile and its Generic Chronicle data/code artifacts.

## Pre-install corrections

- The regression-matrix builder now removes the stale v4 fixture wording.
- The live installer and preflight validator default to the clean-v1 runtime/data pair.
- The clean-v1 pair now binds the item and ability catalogs loaded by runtime settings. The prior
  pair passed the generic data validator but failed the installer dry-run because these two source
  paths were absent.

## Validation

- Full `codemod/run-offline-checks.ps1`: PASS.
- Transactional installer dry-run: PASS after the catalog contract was completed.
- Transactional apply and post-install preflight: PASS.
- Installed settings SHA-256:
  `F9C3A5BC2B70A07AF75AA25C52DA232FC320275A36362A270C70791BF6939830`.
- Installed code-mod DLL SHA-256:
  `06241D434D6D2F801A3AB3EEFC036D33D943FE431D4D49B3274FB90A4A76B1A1`.
- Runtime/data pair SHA-256:
  `7AADD61C00660A0113D3F4986E6ED83810BF93AA52351230FA6E6EE8A44C17B3`.

## Rollback boundary

All eight replaced destinations have `.bak-dcl-bundle-1784471656` siblings. The exact clean-v1
installation is ready for the canonical live-regression matrix; no live case has been claimed by
this installation checkpoint.
