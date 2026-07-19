# DCL native-result symmetry and MP-budget closure

## Audit findings

The HP-debit path already used the pristine result flag as its source authority, but the opposite
numeric conversions were asymmetric:

- elemental absorb could create HP credit from a cancelled/non-HP result;
- Undead healing inversion could create HP debit from a cancelled/non-credit result;
- generic HP/MP passthrough trusted staged magnitudes without checking their native result kind;
- the own-turn MP trickle mechanism existed in code and an isolated probe profile, but was absent
  from the unified job-free runtime profile and the whole-DCL coverage matrix.

Reflect was also listed as a remaining magic regression despite having no definition anywhere in
`docs/deep-combat-layer/`. Its only policy appears in job-side drafts and legacy artifacts, which are
outside the current objective.

## Implemented closure

- Execution formulas gate HP debit, HP credit, MP debit, and MP credit on their pristine native
  result bits before passthrough or cross-channel conversion.
- Elemental absorb requires native HP debit before producing HP credit.
- Undead inversion requires native HP credit before producing HP debit.
- Preview formulas remain ungated because the engine has not staged a native result at forecast
  time; they still show the expected absorb heal or Undead damage.
- The magic mechanism and both composed profiles enable a job-free MA-scaled trickle fixture over
  native MaxMP. KO and zero-MaxMP units receive zero; the existing edge state and clamp own cadence,
  pointer reuse, and the MaxMP ceiling.
- The whole-DCL coverage matrix now owns MP budget/trickle as a distinct technical mechanism and
  explicitly excludes Reflect until a job-free DCL specification exists.

## Offline falsifiers

The C# suite proves:

1. An HP-damage Fire result on an absorbing target becomes HP credit and composes result flag `0x40`.
2. The same action with a low-bit-only `0x01` result produces neither debit nor credit, while preview
   still shows the expected absorb heal.
3. A native HP-credit Cure heals normally and inverts to debit on Undead.
4. A low-bit-only cancelled Cure produces neither healing nor Undead damage, while preview remains
   informative.
5. The composed scaffold carries the same gates and the MP-trickle formula.
6. The existing MP economy tests reject attach-mid-turn, held-edge duplicates, pointer reuse,
   over-MaxMP credit, negative credit, and full-MP credit.

Final trickle magnitude, spell costs, and Mana Shield split ratio remain balance authoring. Live
gates remain the own-turn MP write/cadence, absorb application/presentation, per-target AoE, and the
partial HP+MP `0xA0` presentation path.
