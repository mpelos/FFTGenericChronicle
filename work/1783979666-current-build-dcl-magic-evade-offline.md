# Current build — DCL Magic Evade offline checkpoint

## Implemented mechanism

The DCL hit-control layer now has an explicit `MagicEvade` model, separate from the generic percent
fallback and the physical two-roll contest.

- `DclMagicEvadeConditionFormula` owns action taxonomy. The intended baseline is
  `ability.formula == 8`, which selects the catalog's native magic-damage family while excluding
  formula `0x0C` healing and formula `0x0A` status-only spells.
- `DclMagicEvadeFormula` computes the target's built Magic Evade. It can use item-catalog variables
  such as shield/accessory magical evasion plus job-derived bonuses.
- `DclMagicEvadeCapPct` clamps the result (default 50), so stacking can never become immunity.
- Hit chance is exactly `100 - capped evade`; the existing per-target calc-entry RNG makes a separate
  decision for each AoE victim.
- Miss delivery uses the already-proven action-independent path: force-connect the VM, zero staged
  HP/MP debit and credit at pre-clamp, author kind `0x06`/result `0` at the selector, and suppress
  native hit reactions covered by the reaction gate.
- Healing and status-only actions fall through to `DclHitChanceFormula` and are therefore not evaded
  by Magic Evade. Status resistance remains owned by the DCL 3d6 status rules.

The formula context now also exposes the raw candidate bytes `evade48`, `evade4C`, `evade4D`, and
`magEvaRawMax` for diagnostics. Production profiles should prefer item-catalog variables: hit-control
intentionally zeroes the native evade bytes, so their live values are not a stable authored-stat
source while DCL authority is active.

## Offline validation

- Negative evade clamps to 0.
- Evade 35 produces hit chance 65.
- Evade 80 with cap 50 produces hit chance 50.
- A formula-`0x08`, equipment-derived profile passes runtime validation.
- Missing taxonomy, unknown formula variables, incomplete miss delivery, and caps above 100 are
  rejected.
- Hit logs identify `model=magic-evade magicEvade=N`, so AoE per-target cardinality is auditable.

Build configuration `MagicEvadeAudit3`: 0 warnings, 0 errors. Full smoke suite: pass.

Release deployment was limited to the code-mod DLL. Local and installed SHA-256 are both
`5083FDFEDBD94A4412A7956CB65D4A6FB6E0B46EBED7CA7DB3AAACEEFBC3109B`. The installed settings hash
remains `2BF29CB0217CDBD56591349A41E13C9279CF4308A126C0AE7C71A2BFB1B66551`, the pending LT14 profile.

## Live boundary

The underlying calc-entry-per-AoE-victim and authored-miss delivery are already proven separately,
but the composed Magic Evade slice has not run live. LT18 is prepared to prove:

1. two or more Fire AoE victims receive distinct `model=magic-evade` decisions;
2. an evaded victim loses no HP/MP and receives no native status rider;
3. a connected victim receives deterministic DCL/vanilla-preserved output as configured;
4. Cure uses `model=percent` at 100% rather than Magic Evade;
5. a status-only spell uses the DCL status contest, not Magic Evade.

The installed LT14 profile is not replaced while the base v1.5.1 game still fails to reach its menu.
