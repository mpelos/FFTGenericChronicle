# Thief — battle-simulation gate (draft-shape, frozen placeholders)

Script: `tmp/job-drafts/sim_thief.py`. Draft: `tmp/job-drafts/_thief-draft-reworked.md`.
Placeholders frozen from DCL provisional values before reading results. Tests SHAPE, not calibration.

## Reads (against the required gates)

**Survival (dodge-tank) — PASS with honest holes.** Thief avoids ~67% of physical FRONT attacks
(33% land), close to a *fresh-Block* Knight (23%) — but its Dodge never depletes (Block does), so it's a
more sustainable single-target evader. Holes are explicit and wide: flank 53% land, **back 84%**, **magic
100% (no defense vs spells)**, and focus-fire from multiple angles. Low HP (85) → once hits land it dies
in ~4 attacker-turns (Knight front 20.5 eff dmg/turn, TTK 4.2). Not oppressive.

**Offense / wrong-pick — PASS (intended shape).** Knife per-hit is low: ~9 vs Clothes, ~6 vs Heavy,
~24 vs Robe. Front TTK is awful (27–52 turns); flank ~halves it; back best. Vs **Robe caster** it's an
assassin (back TTK 4.4). Vs **Heavy/plate** it's near-useless even from back (TTK ~35) — thrust ×2 can't
overcome DR 8 on a low-wmod knife. Stand-up race vs Knight: Thief 1.3 eff dmg/turn (TTK 128) vs Knight
20.5 (TTK 4.2) → loses catastrophically, **for the intended reason** (low per-hit), screaming "flank or
steal, don't trade." 

**★ Emergent synergy — Steal IS the Thief's anti-armor lane.** The plate hole is *solved by the kit*:
Steal the Armor/Weapon → the plate body becomes a Clothes body (Thief dmg 6→9, and it can't hit back
hard) → the team kills it. Steal-gear is not just loot; it is the Thief's damage-enabler vs armor. Worth
stating in the spec.

**Steal curve + variance — PASS (shape); base is a calibration knob.** Contested 3d6≤target, hard cap
≤13 (84%) so **never certainty**. Thief no-prep ≤9 = 38% (P(0 in 3 tries)=24% → the bad-luck misery
Marcelo flagged). + flank → 62% (whiff-out 5%). + Pickpocket → 84% (whiff-out ~0%). **Prep is what
removes the misery** — exactly the "prepare to steal" intent. NOTE: prep levers *overflow* the cap
(flank+Pickpocket alone caps it; Speed/Haste are then redundant — they matter only when you lack the
others). Base 38% may be tuned lower at calibration (Marcelo wants vanilla-low base); shape holds.

**Portability (donor>destination) — WEAKEST gate, needs a ruling.** Light Fingers gap at equal prep =
**+34%** (Thief-primary 84% vs off-job host 50%); casual splash is unreliable (unprepped 9%, P(0 in 3)=
75%). BUT a *fully-committed* Ninja host (flank+Pickpocket+Haste, sacrificing its own supports) reaches
**74%** — close to Thief. Since Ninja is itself fast/evasive with higher per-hit, "Ninja + Steal" is the
natural abusive host. → **Open question for GPT:** is Light Fingers enough, or must the **premium/rare-
gear steal** (the famous unique-grab) be *hard*-gated to Thief-primary while only Gil/common steal
splashes?

**Vigilance stacking — PASS.** Front hit 33%→15% (Thief) / 43%→23% (light splash host) with the Dodge
spike; back (84%) and magic (100%) bypass it entirely → never "unhittable." Keep the guardrails
(once/turn, no self-stack).

**Enemy-use — needs a ruling (feel-bad).** Steal is symmetric, so enemy Thieves steal *player* gear with
the same curve. Permanent loss of the player's equipment is the feel-bad Marcelo flagged for Rend. →
**Proposed ruling:** enemy steal of player gear is **battle-scoped** (recovered after the fight) or a
disarm-for-the-battle; the player's steal of a *defeated* enemy's gear is permanent (the enemy is gone).
Preserves the rare-gear fantasy without feel-bad.

**Fun-loop — PASS (qualitative).** Turns 1–3 offer real choices: flank vs Steal-which-slot vs Mug vs
Backstab vs Vault-reposition vs hold-for-Vigilance, plus the prep minigame (set up flank+Haste before the
big steal). Not a single script. The unprepped 24% whiff-out is the only un-fun corner; prep removes it.

## Verdicts to resolve with GPT
1. Portability: Light Fingers soft-gate vs hard-gating premium steal to primary.
2. Enemy-use: battle-scoped vs permanent player-gear loss.
3. Steal base level (calibration; intent = vanilla-low + prep-driven).
4. Confirm Steal-as-anti-armor-lane belongs in the spec.
