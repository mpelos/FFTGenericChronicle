# DCL Validation — Map (pillars + dependency graph)

Status: in progress (Phase B). Pillar section settled pending user checkpoint; dependency graph appended after Phase A inventory consolidates.

---

## 1. L0 Pillars — attacked, refined

The pillar set was extracted from `00-overview.md` (P1–P6) plus two candidates (P7 heroic, P8 GURPS-method). It was then attacked by a **cross-model critic (GPT-5.5 via codex exec)** — transcript: `critics/pillar-attack-RESPONSE.md` (session 019f043d-ac02-7032-ae30-4158c36fb785, 2026-06-26). Default posture of the critic: "this set is flawed." 10 objections returned; my adjudication below.

### Original pillars (as authored)
- **P1 — Contextual differentiation / no strictly-better option.**
- **P2 — Every existing FFT attribute is used or replaced.**
- **P3 — Deterministic damage, random contest.**
- **P4 — Two-sided permanent traits (Brave/Faith/Zodiac).**
- **P5 — Legibility over hidden math.**
- **P6 — No new equipment.**
- **P7 (candidate) — Heroic feel: full effectiveness until 0 HP.**
- **P8 (candidate) — GURPS-adapted but game-balance-first.**

### Adjudication of the critic's 10 objections

| # | Objection | Severity (critic) | My verdict | Action |
|---|-----------|-------------------|-----------|--------|
| 1 | **Missing pillar: action/turn-economy dominance uncontrolled** | BLOCKING | **AGREE — real.** Matches the Speed double/triple-duty doubt (CT + Dodge + refresh, no downside). The vanilla Speed-trap. Design *gestures* at it (finesse rejected for Speed² compounding `01`; "Speed variance" flagged as key knob `12`) but never elevates it to a yardstick. | **ADD pillar P9 (pending user).** |
| 2 | **Missing pillar: AI/enemy counterplay absent** | BLOCKING | **AGREE — biggest blind spot.** Zero docs mention enemy AI. The counterplay triangle, contextual weapons, trait sliders are player affordances; PvE viability depends on whether AI can wield/answer them. | **ADD pillar P10 (pending user — scope fork).** |
| 3 | **P1 conflicts with P6 (fixed tiered item list ⇒ some items strictly better)** | BLOCKING | **AGREE as scope-precision.** FFT items are a tiered progression; within a type later items have higher WP. "No strictly better" can only hold across *types/families at comparable tier*, not across the whole SKU list. Design intends type-level already. | **SHARPEN P1 (scope to families/tiers).** |
| 4 | **P1 unfalsifiable as stated** | BLOCKING | **AGREE — methodologically critical for THIS validation.** Need a testable criterion or I cannot adjudicate "strictly better." | **SHARPEN P1: each weapon type must have ≥N common winning AND ≥N common losing contexts across early/mid/late.** Operationalizes the weapon sims. |
| 5 | P2 is menu-completeness pressure, not combat quality | NON-BLOCKING | **PARTIAL.** User's explicit hard constraint; the critic's reframe ("no displayed attribute may mislead; dead stats removed/renamed/meaningful") is compatible with "used or replaced." Keep user's, note the clarification that it must not force retention of a bad stat. | LOG (non-blocking). |
| 6 | P3 determinism is an implementation detail, not L0 | NON-BLOCKING | **PARTIAL.** Fair that the deeper value is "preview-trust + bounded variance" and determinism is one mechanism. Design has chosen full determinism as principle; internally consistent. | LOG as framing note; keep P3. |
| 7 | P4↔P1 conflict via permanent optimization (a band may dominate) | BLOCKING | **DOWNGRADE to verify-by-sim.** Same class as the honestly-flagged Brave "under-fire dial" caveat (`07`). Not a pillar defect; a claim to test. | Test in Brave/Faith sims; require archetype-band evidence. |
| 8 | **P5 (legibility) ↔ P8 (GURPS depth): no complexity budget** | BLOCKING | **AGREE — arguably the existential risk.** Each mechanic legible ≠ the whole legible. "Visible complexity still fails if the player can't predict outcomes in normal play." No budget stated. | **ADD clause/pillar: depth must fit FFT's tactical bandwidth (predict-in-play test).** |
| 9 | Promote P7 (heroic / no death-spiral) to explicit pillar | BLOCKING if omitted | **AGREE.** Already a resolved principle (`12` item 1); GURPS tempts injury penalties; needs to be a stated guardrail. | **PROMOTE P7 to full pillar.** |
| 10 | P8 unfalsifiable; demote to method | NON-BLOCKING | **AGREE.** "Borrow GURPS but balance first" adjudicates nothing alone. | **DEMOTE P8** to design method: "GURPS = source of candidates, not authority; imports must pass legibility/AI/action-economy/progression gates." |

### Refined working pillar set (for the rest of the validation)
- **P1′ — Contextual differentiation, falsifiable.** No weapon *type/family* is strictly better than another *at comparable tier*; each must have ≥N common winning and ≥N common losing contexts across early/mid/late. (Item tiers may progress; types must keep contextual identity.)
- **P2 — Every attribute used or replaced** (clarified: must not mislead; may remove/rename rather than force-fit).
- **P3 — Deterministic damage, random contest** (serves the deeper value: trustworthy preview, bounded/surfaced variance).
- **P4 — Two-sided permanent traits** (validation gate: no Brave/Faith/Zodiac band best for all builds/jobs/phases — prove per archetype).
- **P5 — Legibility over hidden math.**
- **P6 — No new equipment.**
- **P7 — Heroic: no death-spiral; HP is survival only; effectiveness does not degrade before 0 HP except via explicit, telegraphed statuses.** (PROMOTED)
- **P9 — Action economy is the primary balance constraint** (PENDING USER): no damage/defense/reach/stat package may dominate purely by converting into more turns without a hard tradeoff.
- **P10 — AI/enemy counterplay** (PENDING USER — scope fork): either every major mechanic must be legible-and-usable by enemy AI, OR the system is explicitly scoped as a player-facing PvE affordance with enemies balanced by other means. This must be decided; it changes the downstream validation lens.
- **C1 — Complexity budget / predict-in-play.** Depth must fit FFT's tactical bandwidth; a visible mechanic still fails if a normal player cannot predict outcomes during ordinary play. (cross-cuts P5)
- **(method, not pillar) — GURPS is a source of candidate mechanics, not authority.** (DEMOTED P8)

### Verdict (critic + my adjudication)
The pillar set as originally written is **not yet sound enough** to validate against, for one reason that matters most: **the two missing yardsticks (action economy, AI counterplay)** leave large parts of the design unjudgeable. The 6 authored pillars survive (P1 sharpened, P3 reframed-but-kept). Repairs are additive, not a teardown. **P9, P10, and C1 are logged as OPEN PILLAR RISKS until the user rules on them** — per the validation doctrine, an anchored party does not get to silently dismiss a substantiated pillar objection.
