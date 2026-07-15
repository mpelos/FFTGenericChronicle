# LT24 — reaction carrier action-replacement vertical slice

## Purpose

Test the Hypothesis that a queued native Reaction uses `actor+0x18C` for presentation identity and
`actor+0x142` for executable action identity. Counter (`442`) is the carrier and Basic Attack (`0`)
is the replacement because both should produce the same simple counterstrike shape.

## Gate order

1. LT23 must first prove one commit event per visible Counter, no forecast events, agreed ids, and a
   target list pointing from the reactor to the original source.
2. Run `work/1783993237-battle-runtime-settings.lt24-dcl-reaction-action-logonly.json`. A matching
   Counter must log `replacement=would-write:0` while battle behavior remains native.
3. Only after steps 1–2 pass, copy the profile to a new timestamp-prefixed file and change only
   `DclReactionActionReplacementLogOnly` to `false`. Keep `MaxWrites=1`.

## Live-write pass gate

- Hook install and AOB guard pass.
- Exactly one matching commit logs `replacement=wrote:0`.
- The Counter presentation still identifies the native carrier and executes one ordinary weapon
  strike against the staged target.
- No crash, duplicate action, lost target, wrong-side target, CT consumption, or extra item/status
  side effect occurs.

## Hard fail

Any presentation mismatch, no-op effect, wrong target, duplicate action, corrupted cleanup, crash,
or second write is a hard fail. Restore log-only immediately and retain the full log/screenshots.

This test proves only replacement inside an already accepted native Reaction window. It does not
produce new trigger windows for Riposte, Countershot, Rod Counter, or Dragon's Fury.
