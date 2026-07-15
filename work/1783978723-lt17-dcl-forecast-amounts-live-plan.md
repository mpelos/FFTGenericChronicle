# LT17 — formula-driven forecast/result amount parity

## Prerequisites

Run after LT14–LT16. The base Enhanced game must reach its title/menu before injection. This profile remains offline and is not installed while LT14 is pending.

## Objective

Prove that the same authored amount is shown in the forecast number/ghost bar and applied at execution for both damage and healing.

Profile: `1783978723-battle-runtime-settings.lt17-dcl-forecast-amounts.json`.

## Sequence

1. Launch Enhanced, skip the intro with Enter, choose Load, Manual Saves, and the first entry (save 05).
2. Open a basic Attack forecast. It must show 222 damage and the matching ghost-bar loss. Cancel and reopen against another target; both target slots must show 222 without stale cross-target data.
3. Execute Attack. The target must lose exactly 222 HP, capped only by death/remaining HP, and `[DCL]` must report `debit=222`.
4. Damage an ally and open Cure. It must show 111 healing and the matching recovery ghost bar. Cancel/reopen once.
5. Execute Cure. The target must gain exactly 111 HP, capped by Max HP, and `[DCL]` must report `credit=111`.
6. Confirm `[DCL-FORECAST]` lines carry the same target index and debit/credit pair as the visible panel.

## Pass gates

- preview number and ghost bar agree;
- preview amount equals applied amount for Attack and Cure;
- cancel/reopen and target changes do not reuse another target's value;
- preview polling does not leak an early or duplicate result into execution;
- no static preview control is installed and no `[DCL-PREVIEW-ERR]` appears.

## Failure interpretation

- number correct, bar wrong: the selected UI path is not consuming the staged field mirrored by the poller;
- preview correct, execution native: apply formula/context failed independently;
- execution correct, preview native: calc-entry formula callback or forecast-object mirror did not run;
- Cure renders damage or Attack renders healing: writing both channels requires action-kind gating at the mirror;
- stale target value: per-target cache invalidation needs action identity or UI-close lifecycle handling.
