# LT16 — formula-owned HP and MP staged channels

## Prerequisites

Run only after LT14 and LT15. The base Enhanced game must reach its title/menu before injection. The profile is prepared offline and is not installed while LT14 remains pending.

## Objective

Prove that the existing successful apply callback can author HP healing, MP loss, and MP restoration without a new hook and without disturbing unrelated channels.

Profile: `1783978200-battle-runtime-settings.lt16-dcl-staged-channels.json`.

## Sequence

1. Launch Enhanced, skip the intro with Enter, choose Load, Manual Saves, and the first entry (save 05).
2. Confirm all guarded hooks install and no `SKIP`/`FAILED` line appears.
3. Damage an ally, then cast Cure (ability 1). The target must gain exactly 111 HP, capped only by Max HP. The `[DCL]` line must report `credit=111` and preserve MP channels.
4. If Chakra (ability 106) is available, use it on a target missing HP and MP. The target must gain exactly 111 HP and 22 MP in the same resolution. The log must report both credits.
5. If Rend MP (ability 142) is available, use it on a target with at least 33 MP. The target must lose exactly 33 MP and no HP. The log must report `mpDebit=33`.
6. Execute one unrelated damage action. Its debit must remain native and all unused credit channels must remain unchanged/zero.

If an ability is unavailable on save 05, record that fact and substitute another catalog-confirmed action that naturally stages the same channel; change only the exact ability id in the profile and retain the deterministic output value.

## Pass gates

- exact HP credit and MP credit/debit values appear in the UI/state and `[DCL]` log;
- HP-only, MP-only, and combined HP+MP actions do not leak into another channel;
- unrelated actions remain native;
- repeated charged/evaluation callbacks do not apply the credit early or more than once;
- no `[DCL-ERR]` or partial-write symptom appears.

## Failure interpretation

- log shows the authored value but state stays native: the chosen offset is overwritten downstream or the callback is too early;
- HP works but MP does not: the MP clamp path does not consume the same staged record/window;
- result applies during preview/charge: the successful apply guard is insufficient for credit channels;
- unrelated actions change: the ability predicate or action identity is wrong;
- no `[DCL]` line on a pure heal/MP action: that action family bypasses the current calc-entry/apply join and needs its own context probe.
