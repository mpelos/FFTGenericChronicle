# Dragoon (#12) — final validation gate & adversarial corrections

Date: 2026-06-29. Closes the Dragoon design. Registered doc: `docs/job-balance/jobs/12-dragoon.md`.
Sim harness: `work/1782784366-sim-dragoon-v5-corrected.py` (v4 `…-v4-adopted.py` is the pre-correction draft).

## Process

Marcelo closed the v4 draft (Heavy plate, **unevadable** Jump, Aerial Training innate, Dragon Dive + High
Jump Tier-2, Dragonheart cut). Before registering I ran the promised adversarial pass. The GPT peer
(thread `019f1030-9ddc-7ab2-8179-0f90cf106a30`) was rate-limited, so I used a **fresh-context Claude critic**
(rationale-redacted, told to default to "broken") — which read the **registered** Archer/Knight/reach docs I
had not cross-checked and found two BLOCKING breaks. Verified against canon, fixed, re-simmed, registered.

## The critic's findings, verified

| # | Objection | Verified against | Severity | Resolution |
|---|-----------|------------------|----------|------------|
| 1 | Unevadable Jump is bimodal: its only counter (relocate) is turn-order-gated → free kill on a soft, just-acted unit. | follows from #2 | BLOCKING | Fixed by #2 (Jump made evadable → defence roll is a second, always-available counter). |
| 2 | Free Jump beating Dodge+Parry+Block is the un-priced **union** of the roster's rationed defence-bypass. | `jobs/04-archer.md:92` (Concentration ignores **Dodge only**, paid, **excludes Jump**; Aim "defence still rolls") + `jobs/03-knight.md:84` (Guard Break **Block+Parry only, never Dodge**, "defence still rolls") | BLOCKING | **Reverted unevadable.** Jump rolls defence normally; value = over-the-line **reach** + untargetable mid-air (`06-reach.md` outrange identity), not bypass. |
| 3 | High Jump = "more reach, same damage, no cost" = strict upgrade = the banned jump-ladder. | pillar #1; design's own "reach is one innate, not a ladder" | BLOCKING | **Cut High Jump.** Reach/height already live in the Aerial Training innate; no separate reach-tier. |
| 4 | Aerial Training: parasitic Support + silent off-job reach nerf (legibility). | J2 / `jobs/11-geomancer.md` Landreader precedent | NON-BLOCKING | **Kept.** Added: reduced off-job reach shown in the targeting preview; cited Landreader as the accepted parasitic-innate-export precedent. |
| 5 | Dragon's Fury feeble at point-blank (reach rule) — unstated; Dragon Dive crowds the anti-cluster lane. | `06-reach.md` (reach-2 penalised adjacent; **stop-hit reserved for lancer abilities**) | NON-BLOCKING | Stated Dragon's Fury **obeys reach** (penalised point-blank → "rush inside" blunts it). Added Tier-2 **Stop-hit** (the reach-doc lancer ability) as the headline Tier-2; kept Dragon Dive as a justified ceiling (delivery vector). |

The critic explicitly **upheld** the Dragonheart cut (no pillar hole; field-reason and donor-reason survive)
and the chassis/fantasy/Skewer-vs-normal shape.

## Net change from what Marcelo closed (two reversals to flag)

- **Jump: unevadable → evadable.** The "best idea" I pitched contradicts registered canon (Archer
  Concentration *excludes Jump*; even premium Aim "defence still rolls"; Knight Guard Break never touches
  Dodge). A free all-defence-bypass leap can't stand without rewriting those jobs. Reverted.
- **High Jump: cut.** Strict-upgrade ladder; redundant with the Aerial Training innate.
- (Smaller) Stop-hit added as the on-identity Tier-2; Dragon's Fury reach-ruling stated; Aerial Training
  legibility note.

## Final sim read (v5, all gates pass)

- **Design law:** 2 normals (164) > 1 Jump (115); Jump's edge is reach/untargetable, not raw or bypass.
- **Jump (evadable):** one-shots a boxed-in **low-Dodge** caster (153 vs 95 HP, ~76% connect); **whiffs a
  high-Dodge** backliner (~35% connect — Archer's job, not the Dragoon's); 25% chip into Heavy. No degenerate
  cell (defence roll + relocate both available).
- **Dragon Dive:** distinct by delivery vector; evadable + telegraphed → not strict up/down-grade.
- **Two-sided:** physical TTK 8.4 / magic TTK 2.5; grounded exposure real.
- **Reach/lanes:** spear outrange + point-blank weakness; Dragoon goes *over* (reach), Archer beats Dodge,
  Knight breaks Block/Parry, Samurai wide-bursts, Thief slips through. No overlap.

## Still open / deferred

- Numbers are frozen DCL placeholders (G=5.0, PEN=0.25, etc.) — real calibration is `docs/deep-combat-layer/12`.
- Stop-hit, Dragon Dive, and the fixed-CT leap lean on engine hooks (delayed-trigger / AoE / CT-decoupling) —
  flagged Tier-2 / Strong-Hypothesis in the doc; confirm on Windows.
- GPT peer never got its pass (rate limit). The fresh-context critic stood in; re-run GPT on a later job if a
  cross-model check is wanted.
