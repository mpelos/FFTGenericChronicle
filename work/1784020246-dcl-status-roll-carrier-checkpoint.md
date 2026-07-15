# DCL status-roll carrier checkpoint

## Question

Can formula `0x0A/0x0B` status-only magic use the same retained native carrier as formula `0x22`, or
does the DCL contest need an earlier producer?

## Offline result

The three handlers all converge on the shared packet finalizer, but their entry contracts differ:

- `0x22` unconditionally calls the shared result builder and tail-jumps to the finalizer;
- `0x0A` skips the finalizer when either of two native prerequisite/roll helpers returns failure;
- `0x0B` skips the finalizer when its native result flag is zero.

The legacy `StatusChanceControl` site at `0x30659B` is inside the shared builder whose exact direct
callers are `0x307D50`, `0x307D84`, `0x307DE8`, and `0x307DFC`. Neither `0x0A` nor `0x0B` calls that
builder, so this control cannot become a global DCL shortcut.

Catalog inventory is exact:

- formula `0x0A`: 38 actions — 35 add, 3 cancel/remove;
- formula `0x0B`: 15 actions — 13 add, 2 cancel/remove;
- formula `0x22`: 2 actions — Kiyomori and Masamune.

## Consequence

A decision made only at pre-clamp cannot convert a native `0x0A/0x0B` miss into success because the
miss skips finalization and never presents a packet for replacement. Forcing a shared 100% chance is
also invalid: it is not scoped to one authored action/target and does not preserve forecast or AI
probability.

The required producer sits before the conditional finalizer and has three modes:

- forecast: expose the authored 3d6 success probability without consuming a roll;
- AI scoring: expose the same probability without consuming persistent execution state;
- execution: consume exactly one 3d6 decision and cache it for packet commit.

This depends on the calc-provenance live gate (LT28). Until that gate identifies the modes, formulas
`0x0A/0x0B` remain excluded from `retained-as-carrier`. No new hook or data mutation is staged.
