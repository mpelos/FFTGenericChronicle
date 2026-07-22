# Canonical admission live gate: pool and equipment blockers

## Context

The canonical admission live proof was rerun after the hook assembly fix and the explicit-policy
template bridge changes. The active sentinel bundle is still the canonical admission sentinel
runtime/data set under `work/1784673033-*`.

## Evidence

- `work/1784687782-raw-canonical-admission-live.log`
  - Hook installed and battle reset entered `canonicalBattle=1`.
  - First Fire attempt reached MP debit and failed admission with:
    `The native HP/MP pools are not synchronized with the canonical DCL characteristic snapshot.`
- `work/1784688247-raw-canonical-admission-live.log`
  - After adding opt-in native pool synchronization to the policy-ticket template, the previous
    HP/MP synchronization error disappeared.
  - The next fail-closed blocker was:
    `Native equipment slot head item 167 has no canonical metadata.`
- `work/1784688590-raw-canonical-admission-live.log`
  - After adding live-save equipment metadata for item ids 30, 167, 207, and 216, the runtime loaded
    `items=8`.
  - No canonical admission error line was emitted in the partial log.
  - The UI sequence did not actually complete the Fire admission; there was no MPLOSS/admission
    event in that partial collection.

## Conclusion

- Proven: the admission hook installs at the target RVA with the current `use64` shim.
- Proven: the canonical battle reset path enables `canonicalBattle=1` for the live battle.
- Proven: native save rows cannot use the strict default `DclSecondaryInputs()` for live admission;
  the sentinel bridge needs explicit opt-in native pool synchronization policy when admitting
  unconverted FFT save rows.
- Proven: the live sentinel item metadata must cover every equipped native SKU on captured source,
  selected, and target rows; the Fire autosave path currently needs at least 30, 167, 207, and 216.
- Pending: execute a clean Fire sequence after the equipment metadata fix and collect a log with
  MPLOSS plus `[DCL-CANONICAL-ADMISSION]`.

## Next live step

Use the latest mid-battle autosave to skip formation. On Agrias' turn:

1. Open `Abilities`.
2. Open `Black Magicks`.
3. Select `Fire`.
4. Target the adjacent Goblin/archer cluster.
5. Confirm `Unit`.
6. Choose `Wait` and confirm facing/end turn.
7. Collect with `python tools\collect_dcl_canonical_admission_live_log.py`.
