# DCL Fear winning-planner offline fix

## Scope

This checkpoint replaces the refuted scratch-candidate handoff in the Fear forced-flee coordinator.
It does not change a job, ability assignment, balance value, or live save.

## Transaction change

The Chicken dispatcher shim now mirrors the native sequence before route resolution:

1. snapshot active-unit `+0x4F/+0x50/+0x51`;
2. call candidate selector `0x38E11C`;
3. clear the `0x240`-byte planning block at RVA `0x1871A54` through native memset `0x5CA420`;
4. call planner `0x321390` with `(0xFF, 1)`;
5. read the winning X/Y/layer record at RVA `0x1872364`;
6. restore the unit tuple byte-exactly;
7. resolve and stage the ordinary native movement route.

Every selector, planner, identity, restoration, cursor, or route failure still falls back to native
Chicken. The audit record therefore remains bounded and fail-closed.

## Static and offline validation

- `tools/analyze_dcl_fear_flee_route.py --check-only`: PASS;
- `tools/test_dcl_fear_flee_live.py`: PASS;
- C# formula/runtime smoke tests: PASS;
- complete `codemod/run-offline-checks.ps1`: PASS (1,631 output lines);
- Release build: 0 warnings, 0 errors;
- whitespace check: PASS.

The static analyzer now guards the native selector/planner prefix, planning-block reset call,
planner call, winning-record read, and the planner's four-byte winner publication.

## Built artifact

- DLL: `codemod/_build/fftivc.generic.chronicle.codemod/fftivc.generic.chronicle.codemod.dll`
- SHA-256: `3E935E56CBE59BF051568BFB554636D0DCB683197D356541023929559461446A`

The artifact is built but not installed. The currently running Reloaded/game session still owns the
previous coordinator and must be closed before a transactional live install. The next live gate is
one successful `RouteStaged stage=0` event followed by state `0x10 -> 0x11 -> 0x12` and a preserved
post-flee action opportunity.

