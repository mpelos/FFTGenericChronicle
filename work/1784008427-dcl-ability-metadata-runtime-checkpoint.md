# DCL ability metadata runtime checkpoint

## Control-state clarification

The user launched Enhanced successfully through Reloaded-II after selecting the Reloaded profile.
The game installation, Enhanced profile, and Reloaded launch path are healthy. The prior failure is
isolated to the Codex computer-control channel and does not block offline investigation.

## Offline classification result

`tools/report_dcl_ability_classification.py` now emits conservative per-ability candidates for:

- action kind;
- damage type;
- avoidance policy;
- status resistance category;
- side-effect/transaction policy.

The classifier keeps source-sensitive `Don't Act` / `Don't Move`, untyped magic, physical ability
damage types, special formulas, and other design-dependent identities explicitly open. Damage plus
status riders use a chained landing policy. Multi-hit magic uses per-strike Magic Evade.

Latest artifacts:

- `work/1784008322-dcl-ability-classification-candidates.csv`
- `work/1784008322-dcl-ability-classification-candidates.md`
- `work/1784008322-dcl-ability-metadata-authoring-template.csv`

Coverage is 324 candidate-complete rows and 188 authoring-open rows across the full 512-row catalog.
The authoring template pre-fills proven candidates, blanks only unresolved fields, and leaves every
row `approved=0`.

## Runtime authoring surface

`AbilityCatalog` accepts an optional `DclAbilityMetadataPath` CSV overlay. The baseline remains
unchanged. Only explicitly approved rows are merged. Invalid schema, duplicate ids, unknown enums,
or invalid power disable the entire overlay while preserving the 512-row vanilla catalog.

Approved rows expose `ability.dcl.*` variables for power and one-hot action/damage/avoidance/status/
side-effect classifications. Default variables are present and zero when no approved row exists.
The overlay and base catalog both hot-reload.

## Verification and installed state

- ability-classification smoke: pass;
- whole-DCL coverage audit: 27 mechanisms valid;
- runtime anchor audit: 26/26 pass (`work/1784008385-runtime-hook-anchor-audit.md`);
- C# Release build: pass, zero warnings/errors;
- runtime smoke tests: pass;
- installed LT23 settings validation: pass with the existing observe-only reaction warning;
- `git diff --check`: pass except the pre-existing CRLF warning for
  `work/runtime_formula_context.md`;
- source and installed DLL SHA256:
  `C20E67A1AFCF886183FFD4E621FF82161E0D88E5F19B655D0021D7B9062BBF19`;
- installed settings SHA256 remains:
  `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`.

## Next offline queue

1. Resolve the 188 open rows by common family, starting with untyped magic and physical ability
   damage types, without approving job-design choices that are not ratified.
2. Separate special-family avoidance decisions (steal, break, drains, stat/trait changes,
   Bardsong/Dance, custom IVC formulas) from missing engine mechanisms.
3. Approve only rows whose complete DCL identity is owned by existing design definitions.
4. Compose the approved metadata overlay into the future unified DCL profile after open design and
   live gates are closed.

Live gates LT23/LT27/LT28/LT31 remain unchanged; this slice required no new live test.
