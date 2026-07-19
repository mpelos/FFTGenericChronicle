# LT41D DCL Dual Wield visible fast provenance live plan

## Fixture

- Runtime profile:
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Autosave: `work/1784172418-dcl-dual-wield-fast-visible-fixture.png`, SHA-256
  `B4DC074EA2344168D3F88CBDAB487C381D0A79A457B89A39BC8FF96FA66E0FED`.
- Its source is the attack/main CT-order fixture from LT41C. The new layer clears only Invisible
  mask `0x10` from Rion's effective `+0x63` and master `+0x1F1` bytes in the three current live
  components. Reraise source byte `+0x59 = 0x20` is preserved.

## Action and stop rule

Use the atomic Continue path. End Rion's open turn with paced inputs **S, S, F**, then confirm facing
with **F**. Adjacent Janus must act next and use Choco Beak against visible Rion. Stop immediately on
any contrary action. If Choco Beak connects, allow the owned `442` transaction through both
state-`0x2C` effects, then close the game.

## Required diagnosis

For each Counter strike correlate calc provenance, hit identity, `[DCL-PRECLAMP]` entry or explicit
guard/callback error, `[DCL]` rewrite or `[DCL-MISS]`, HP delta, and state-`0x2C` effect. Only an
unambiguous second-strike cause authorizes a cache/pipeline change.

Use the six-file backup/archive/restore protocol and verify every pre-test hash.
