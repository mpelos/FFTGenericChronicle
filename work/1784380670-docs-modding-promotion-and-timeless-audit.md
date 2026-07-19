# docs/modding promotion and timeless-content audit

Created: 2026-07-18

## Question

Verify that hardened findings are promoted out of the `work/` investigation journal and into the
timeless `docs/modding/` manual, and ensure the manual remains organized by fact owner.

## Audit result

Recent DCL findings were already being promoted, principally into:

- `04-engine-memory-model.md`: native memory fields, action/result ownership, active weapon,
  position commit, movement route/per-step boundaries, battle lifecycle, Interrupt, and Reaction
  transaction facts;
- `06-code-mod-runtime-dsl.md`: settings, formula contexts, output-control contracts, physical and
  Magic Evade decisions, status authority, synthetic Reactions, Approach, Interrupt, and Fear
  runtime contracts;
- `08-dcl-information-requirements.md`: requirement-to-owner and coverage inventory.

The audit also found legacy documentation debt: nine modding documents contained 144 dated/journal
markers before cleanup. Most violations were in `05-reverse-engineering.md`,
`06-code-mod-runtime-dsl.md`, and `08-dcl-information-requirements.md`. The content included live
campaign ids, dated headings, validation profiles, per-test narration, and next-step language.

## Corrections

- Rewrote `05-reverse-engineering.md` as a compact, present-tense native-boundary reference.
- Consolidated the legacy action-context, numeric pipeline, hit/miss, MP-side miss, counter-path,
  and miss-presentation narratives in `06-code-mod-runtime-dsl.md` into current contracts.
- Rewrote `08-dcl-information-requirements.md` as a nonduplicating coverage/ownership matrix.
- Removed remaining dated/journal markers from `00-overview.md` and
  `04-engine-memory-model.md`.
- Added `tools/check_docs_timeless.py` plus its self-test.
- Added both the self-test and the full `docs/modding` scan to `codemod/run-offline-checks.ps1`.

## Verification

```text
check_docs_timeless self-tests PASS
docs timeless check PASS: 9 file(s)
PowerShell parse PASS
```

The checker rejects dates in prose, live-test campaign ids, status/update headings, test-plan
headings, journey voice, and TODO/next-step language while ignoring fenced and inline code.

## Promotion rule

Every new investigation remains in a timestamp-prefixed `work/` artifact while evidence is being
collected. Once a conclusion hardens, the timeless fact is added to its single owner in
`docs/modding/`, stripped of chronology and fixture narration. The `work/` evidence remains as the
audit trail. The offline suite now prevents new journal markers from entering the manual.
