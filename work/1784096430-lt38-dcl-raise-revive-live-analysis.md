# DCL Raise/revive live analysis

## Checks

- PASS — compute-point writer installed
- PASS — forced Death executed once
- PASS — Death reached native zero HP
- PASS — Raise execution stages authored 111 credit
- PASS — native max-HP clamp applies 91 of staged 111
- PASS — native lifecycle clears effective Dead after HP apply
- PASS — legacy instant-KO fallback unused
- PASS — no managed failure

## Interpretation

A passing transaction proves that the unified compute-point writer replaces Raise's native
46-credit packet with the authored 111 credit while preserving the native KO-target packet and
lifecycle tail. The native apply clamps that credit to the target's 91 maximum HP, restores HP,
and only then clears the effective Dead mirror; no direct status write is required.
