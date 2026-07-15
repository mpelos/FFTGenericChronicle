# DCL reaction roster checkpoint

## Scope

This pass continues the DCL investigation offline. It starts from the exact per-evaluation Reaction
id surface and audits what the final job roster needs beyond a writable Brave curve.

## Confirmed offline

- The native catalog contains exactly 32 Reaction records, ids `422..453`.
- The exact evaluated id is available at all four real-code Brave gates.
- Reaction staging writes the exact id to `orderRecord+2` and to the reaction-evaluation global.
- VM-owned Shirahadori can be selected by exact id at calc entry and receive scoped Brave
  virtualization with guarded restoration.
- The current reaction dispatcher begins at RVA `0x30B4EC`; RVA `0x30B584` is inside the function,
  not its head.
- The final job specifications contain 16 roster entries: 14 assigned/candidate Reaction records
  and two intentional open slots (Orator and Summoner).

## Manifest verdict

`work/1783989749-dcl-reaction-implementation-manifest.csv` owns the machine-readable audit and
`work/1783989749-dcl-reaction-implementation-manifest.md` owns its review form.

The chance surface is not the completion criterion. Final reactions separately need:

- an incoming source/action identity;
- trigger-family and survivor/miss/visibility/range/LoS filters;
- basic counter action synthesis or targeted status/stat effects;
- atomic HP/MP handling where applicable;
- once-per-attacker-action or once-per-own-turn-cycle state;
- data, names, descriptions, eligibility, and calibration.

Current final-roster classification: five mechanism-missing, five mechanism-partial, three
design-and-mechanism gates, one near-native, and two intentional open slots.

## Decisions kept explicit

- `Grit`, `Auto-Potion`, and several retained vanilla utility reactions do not yet have a final
  Courage/Caution/Neutral decision.
- Candidate record assignments are not deployment truth. In particular, Archer's Bane is only a
  possible storage record for Countershot; its native effect is avoidance, not a returned shot.
- The LT21 Neutral Mana Shield row exercises the Neutral code path only. Final Time Mage Mana Shield
  is Caution per the job specification.

## Automated gates

- reaction manifest generator and catalog/source validation: PASS;
- native taxonomy coverage: `32/32`, unique ids: PASS;
- final-job source anchors: PASS;
- Release code-mod build: PASS, zero warnings/errors;
- complete formula-runtime smoke suite: PASS;
- LT21 settings validation: PASS, warnings only;
- current executable reaction-scope analyzer: all twelve anchors PASS after incoming-source mapping;
- `git diff --check`: PASS except the pre-existing line-ending warning for
  `work/runtime_formula_context.md`.

## Next offline investigation

The next mechanism is a stable reaction trigger context keyed by incoming action and source, followed
by cadence ownership for `once per attacker action` and `once per own-turn-cycle`. Existing action,
hit-decision, turn, position, reach, and magic-bolt surfaces should be reused only after their event
lifetime and reaction-path visibility are verified. Live LT21 remains deferred while the base game
cannot reach the menu.

## Continuation: incoming context and cadence primitive

The native Counter branch reads `dword[0x14186AFF4]` as the original attacker, selects that unit,
and writes its index to reaction order-record `+0xB`. The runtime reaction formula context now
exposes the source, incoming action type/id, source/target turn epochs, and ordinary attacker/action/
ability variables. It prefers the fresh calc cache and falls back to source `+0x1A1/+0x1A2`.

`DclReactionCadenceState` owns pure, offline-tested primitives for one use per defender own-turn
cycle and one use per attacker-action token. These primitives do not yet suppress or consume a live
reaction. Consumption remains intentionally deferred until an execution/effect-commit hook is
attached, because the chance callbacks also run during forecast/AI calculation.
