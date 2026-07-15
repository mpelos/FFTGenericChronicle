# LT18 — DCL Magic Evade per target

## Prerequisite

LT14–LT17 remain ahead of this test. Do not inject while the unmodified Enhanced v1.5.1 game still
black-screens before the title menu.

Normal startup: choose Enhanced, press Enter to skip the intro, Load, Manual Saves, first entry
(save 05).

## Profile behavior

Profile: `1783979666-battle-runtime-settings.lt18-dcl-magic-evade.json`.

- catalog formula `0x08` (native magic damage) uses Magic Evade;
- raw evade 80 is capped to 50, so every offensive-magic target shows `pct=50`;
- real RNG remains active, so each target must have its own logged roll;
- formula `0x0C` healing and `0x0A` status-only actions fall through to `pct=100`;
- connected HP/MP amounts remain native because the DCL amount formulas are empty.

## Test A — AoE independence

Cast Fire/Fira/Firaga or another formula-`0x08` AoE so at least two living units are inside the final
area. Preserve the full log span from the first `[CALC]` until every victim's selector event.

Pass requires, for each final victim:

- one `[DCL-HIT] ... model=magic-evade magicEvade=50 pct=50` with that target's index/id;
- its own roll value and `cached=0` at execution authority;
- one matching pre-clamp outcome and selector delivery;
- misses zero every staged HP/MP debit/credit and do not receive a native rider;
- hits retain the connected native amount;
- no physical `defense=`, Parry, Block, or guard-spend log appears.

Distinct target ids with separate decision lines prove per-target authority even if random rolls happen
to have the same numeric value. A single shared `[DCL-HIT]` line followed by multiple HP events fails.

## Test B — healing exclusion

Cast Cure/Cura on a living ally. It must log `model=percent pct=100`, never
`model=magic-evade`. Healing must apply normally and no miss selector may be authored.

## Test C — status exclusion

Use a formula-`0x0A` status-only spell such as Sleep or Silence. It must log the 100% percent fallback,
not Magic Evade. If DCL status control is enabled in a later composed profile, the status's own 3d6
resistance log is the only avoidance contest.

## Failure interpretation

- formula `0x08` shows `model=percent`: ability catalog was unavailable or taxonomy formula failed;
- Cure/status shows `model=magic-evade`: taxonomy is too broad and violates the DCL split;
- only primary AoE target gets a decision: calc-entry target coverage regressed;
- miss loses HP/MP or gains a rider: staged-channel suppression is incomplete for that action family;
- forecast differs from 50%: forecast-copy ownership or target mapping is stale;
- native physical block/parry appears: the all-zero native-evade baseline was not maintained.
