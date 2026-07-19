# DCL Dual Wield second-strike gap

## Trigger for the audit

The visible-battle account and the archived LT40 log disagreed about whether Rion survived Choco
Beak. The runtime profile resolves that part: LT40 intentionally used `DclDamageFormula = "1"`, and
the archived log keeps Rion alive. Counter `442` therefore occurred only in the instrumented,
nonlethal capture; a genuinely lethal hit cannot satisfy `successful-hit-survivor`.

Reviewing the two Counter effects exposed a separate DCL delivery defect.

## Exact evidence

The accepted Reaction is one native Counter transaction with two Dual Wield effect rows:

- first strike: calc payload `124`, DCL rewrites `oldDebit=189` to `debit=1`, Chocobo HP
  `332 -> 331`, then state-`0x2C` effect `event=4`;
- second strike: a new calc row again carries payload `124`, but no `[DCL]` commit follows; Chocobo
  HP changes `331 -> 142` by the full vanilla `189`, then state-`0x2C` effect `event=5`.

The later ordinary Dual Wield attack repeats the same shape: its first strike is rewritten to one
point and its second applies the vanilla `126`. This is not specific to Reaction delivery.

The raw anchors are lines `111..121` and `169..182` of
`work/1784167467-lt40-dcl-synthetic-reaction-owner443-delivery442-trivalidator-live.log`.

## What is now proven and refuted

- **Proven live:** native Dual Wield produces two separate calc/result/effect transactions.
- **Proven live for this fixture:** both observed Counter calculations carry the exact active-weapon
  payload `124`.
- **Refuted:** the current managed pre-clamp DCL pipeline controls every native Dual Wield strike.
  It controls the first and fails open to vanilla on the second in this capture.
- **Still unknown:** whether the second calc has non-confirmed battle-state provenance, whether a
  later calc overwrites its action context before pre-clamp, or whether another guard rejects the
  managed callback. The current sampled cache-miss log suppresses events 2..63, and LT40 did not
  enable the calc-provenance probe, so the archived file cannot distinguish these causes.

## Offline conclusion and next gate

Static work already proves that both native debits traverse the pre-clamp boundary and that
`orderRecord+8` is the exact selected weapon. No hand-order heuristic is needed or allowed.

Before changing cache lifetime, capture one bounded Dual Wield action with calc provenance enabled
and unsampled pre-clamp rejection diagnostics. The gate must correlate, for both strikes, return RVA,
battle state, payload, action-cache identity, hit-decision identity, staged debit, DCL commit, HP
apply, and state-`0x2C` delivery. The implementation must then follow the observed lifecycle:

1. preserve or promote the second confirmed context if it exists but is overwritten; or
2. bind a new per-strike context at the already proven pre-clamp transaction if the VM exposes no
   confirmed second execution calc.

Do not infer right/left from event ordinal, HP delta, elapsed time, or result-state bytes.
