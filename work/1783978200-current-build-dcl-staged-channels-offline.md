# DCL staged HP/MP outcome channels — offline checkpoint

## Implemented slice

The successful DCL apply callback now owns all four numeric staged channels on the target record:

| Channel | Offset | Runtime formula | Input variable |
| --- | ---: | --- | --- |
| HP debit / damage | `+0x1C4` | `DclDamageFormula` | `dcl.oldDebit` |
| HP credit / healing | `+0x1C6` | `DclHealingFormula` | `dcl.oldCredit` |
| MP debit / loss | `+0x1C8` | `DclMpDebitFormula` | `dcl.oldMpDebit` |
| MP credit / restoration | `+0x1CA` | `DclMpCreditFormula` | `dcl.oldMpCredit` |

All configured formulas evaluate before any channel is committed. Formula failure preserves the native outcome. If a staged write throws after an earlier write, the callback attempts to restore every already-written channel before failing open.

A DCL-authored miss now cancels every positive channel, not only HP/MP damage. This closes the case where a missed healing or restoration action could still apply its credit while the output was presented as a miss. Status plans remain suppressed on the authored miss path.

The runtime validator treats any HP/MP channel formula as a complete DCL callback outcome, validates each formula independently, prevents the legacy pre-clamp plan/static writers from overwriting it, and permits a heal-only or MP-only profile with no damage formula.

## Offline gates passed

- Release build: zero warnings, zero errors.
- Full formula/runtime smoke suite: passed.
- Formula context exposes both staged MP inputs and preserves the existing HP inputs.
- Validator accepts HP-credit/MP-only ownership and rejects unknown inputs independently on each formula.
- LT14 status and LT15 physical profiles still validate with the new optional settings absent.

## Remaining live boundary

The offsets and native apply clamp are Strong/static and the MP-debit zero path is already live-proven through Mana Shield. Formula ownership of HP credit, MP debit, and MP credit still needs a controlled action test. LT16 uses Cure, Chakra, and Rend MP where the loaded roster exposes them. Forecast wiring for DCL healing/MP values is a separate mechanism; LT16 validates execution first.
