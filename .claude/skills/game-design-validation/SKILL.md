---
name: game-design-validation
description: >-
  Rigorously pressure-tests an accumulated body of game-design (or any layered systems-design)
  decisions for coherence, internal consistency, and soundness — the pass you run after many
  decisions have stacked on top of each other and doubts are piling up. Use this whenever the user
  wants to "validate", "stress-test", "sanity-check", "audit", "review for coherence", or "make sure
  it all still holds together" across a corpus of design docs — or simply says the decisions are
  accumulating and need a coherence pass — even if they never say the word "validation". It runs a
  layered, adversarial, simulation-backed design-thinking process under /loop. This is NOT for
  generating new design (that's grill-me) — it is for breaking design that already exists. Reach for
  it on phrases like "let's validate everything we decided", "stop and check the whole design", "is
  this all consistent?", "stress-test the combat system", "review the design docs for holes".
---

# Game Design Validation

You are validating a body of design decisions that already exists. The deliverable is **judgment**:
which decisions hold, which contradict each other, which were anchored rather than chosen, and which
doubts are still open. There is no file to transform — the output is a verdict you have earned.

## Why this is hard — the bias you are fighting

The model running this skill (you) has a specific, predictable weakness: **you are pulled to ratify
decisions that already exist.** You read a rationale, it sounds reasonable, you nod and move on. That
nod is the failure mode. Unlike a human's more divergent mind, your reasoning is convergent and
easily anchored — once a choice is on the page you under-rate its alternatives, and you are
sycophantic toward conclusions already reached (especially ones *you* helped reach). A validation run
that doesn't actively counteract this is just **organized confirmation bias**: a beautifully
structured rubber stamp.

So the entire point of this skill is structural de-biasing. The layers and phases below organize the
work; the **adversarial mechanisms are what actually make you think.** If you ever notice yourself
agreeing smoothly with everything, stop — you are doing it wrong.

## Operating doctrine (hold these the whole way through)

- **Understand the philosophy first — the project's *and* the domain's — then hold it as a hard rule.**
  You cannot validate against a yardstick you do not understand. Before you judge anything, internalize
  two things and re-read every verdict against them: (1) the **project's design philosophy** — the feel
  it reaches for, the player fantasy it sells, why it exists at all, and what would make it a *failure*
  even if internally consistent; and (2) the **craft of the domain you are touching** — the established
  principles, known failure modes, and genre conventions of the field (tactical-RPG combat, economy
  design, pacing, whatever applies). A verdict reached without both is uninformed even when it sounds
  rigorous: you will flag deliberate genre moves as "broken" and wave through decisions that violate
  craft the docs never thought to name. This understanding is a **standing constraint**, not a one-time
  Phase-B chore — if a critic's verdict or a sim's read ignores the intended feel, it does not bind.
- **Guilty until proven innocent.** Each pass, your job is to find why a decision is *wrong*, not why
  it's fine. A decision keeps its place only by surviving a genuine attempt to break it. "I couldn't
  refute it" is the bar — not "it sounds reasonable."
- **Steelman the road not taken.** Before you keep a decision, reconstruct the *strongest* version of
  the alternative that was rejected, and re-run the comparison honestly. You are structurally bad at
  this — the unchosen option fades and you under-rate it. Drag it back to full strength on the page,
  then compare. Sometimes you'll concede the alternative wins; that's the skill working.
- **Independence must be structural, not theatrical.** Putting on a "skeptic hat" inside your own
  context is theater — you're still anchored to the rationale you already absorbed. The real de-bias is
  a critic that **never made the decision**: spawn a fresh-context subagent (Task/Agent) and give it a
  **rationale-redacted** view of the docs — the decision's neutral statement + its neighbors + the
  pillars + `philosophy.md` (the intent and domain craft), never the author's defense. **This redaction applies to every critic you spawn, in every
  phase**, not just one. For the most structural decisions, and for every conflict-of-interest one,
  prefer a **different model** (the GPT peer via the Codex MCP); when no peer is available the
  cross-model requirement falls back to **two same-model critics with disjoint prompts**, never silently
  to one.
- **Question before you answer.** Raise doubts first and resist answering in the same breath — both
  within a layer and across the corpus: Phase C raises *all* doubts before Phase D answers *any*. Answer
  with distance and the whole board in view, never from the frame that produced the decision.
- **Evidence or it didn't happen.** A decision stands on a concrete mechanism, a worked scenario, or a
  simulation — never on assertion or vibe. At the micro layers, reasoning alone is *insufficient*; you
  must simulate (see below).
- **Outside-in, then ripple back.** Validate from the outermost layer inward — but every time an inner
  decision changes or a contradiction surfaces, **reopen the outer decisions that depended on it** and
  revalidate. Validation is iterative, never a single waterfall pass.
- **Converge, then stop.** You decide when it's done — but "done" has a definition (see *Convergence*),
  not a feeling.

## The five layers

Stratify every decision into one of these, from outermost (the criteria) to innermost (the values):

- **L0 — Pillars / vision.** The non-negotiables everything is judged against (e.g. "no option is
  strictly better than another", legibility, the intended feel). These are the *yardstick*.
- **L1 — Player experience / fantasies.** Archetypes, the choices meant to feel good, the intended
  counterplay and rock-paper-scissors.
- **L2 — Systems & their connections.** The architecture web — how the big systems wire together
  (attributes ↔ damage ↔ defense ↔ equipment ↔ status ↔ economy ↔ progression).
- **L3 — Mechanics.** The specific rules inside each system.
- **L4 — Parameters.** The numbers, curves, breakpoints, tiers.

You validate **L0 → L4** because you cannot judge a mechanic without first fixing the pillar it must
serve. You ripple **back up** because an inner decision can reveal that an outer one was wrong,
incomplete, or violated.

## The process

### Phase A — Inventory: build the decision register

You cannot validate what you haven't enumerated, and accumulated doubt comes partly from decisions
being scattered across many docs. Read the entire corpus and extract **every decision** into a
register. Fan out with fresh subagents over the docs if the corpus is large. For each decision record:
`id`, `statement`, `layer (L0–L4)`, `rationale`, `alternative(s) rejected`, `dependencies (other ids)`,
`source (doc/section)`, `conflict-of-interest`, `status`. Write each `statement` as a **neutral,
mechanism-only** description with no embedded justification — a statement that argues for itself defeats
the later redaction (and is the easiest way to anchor a critic). Tag `conflict-of-interest` **true by
default** for any decision the validating model helped author; if the whole corpus was co-authored,
treat *all* of it as conflict-of-interest rather than trusting your memory of who decided what. Also
gather every already-open question. Save to `register.md`.

### Phase B — Map: layers + dependency graph

Assign each decision its layer and draw the **dependency graph** (which decisions rest on which). This
graph is what makes ripple-back possible *and* what "load-bearing" is later computed from — which makes
its **edges an attack surface**: under-link a decision and it drops below the load-bearing threshold
(skipping the full critic) *and* vanishes from the contradiction-hunt. So before you trust it, **spawn a
critic to attack the graph itself** — which edges are missing, which in-degree is mis-weighted — and
recompute. Produce a short "how it all connects" map. **No verdicts yet** — just structure. Save to
`map.md`.

Before the pillars, **make the philosophy explicit and earn the domain — write `philosophy.md`.** Capture
(1) the **project's design intent**: the feel it reaches for, the fantasy it sells, why it exists, and
what would count as a *failure* even if every decision were internally consistent; and (2) the **craft of
the domain you are touching**: the field's established principles, known failure modes, and genre
conventions, so you can tell a deliberate genre move from an accident. **Validate both with the user
before judging anything** — a yardstick you misread poisons every downstream verdict, and unlike the
pillars (which you can attack mechanically) the *intent* is the one thing only the user can confirm. This
file is then handed to every critic alongside the pillars, and every verdict is read against it.

Then **pin down the L0 pillars before you judge anything against them** — they are the yardstick, and a
wrong or incomplete pillar set makes every downstream verdict inherit the error. Because you extracted
them while anchored, don't just nod: first **spawn a fresh- or cross-model critic to attack the pillar
set itself** — which pillar is missing, which two quietly conflict, which is really an implementation
detail wearing a pillar's clothes. The user may set or correct the pillars (it is their game), **but a
critic's pillar objection that the user merely waves away stays logged as an open risk until it is
refuted by argument** — the proudest, most anchored party doesn't get to silently dismiss it. This is
the one checkpoint that, done wrong, silently invalidates the whole run.

### Phase C — Raise doubts (do NOT answer them yet)

Sweep each layer top-down and write the questions, tensions, and suspicions to `doubts.md` — *without
resolving any of them* (answer now and you answer anchored). Tag each by layer and **severity**:
*blocking* (violates a pillar, creates a strictly-better option, or contradicts another decision) vs
*non-blocking* (calibration, polish, deferred). But your own sweep is necessarily blinkered — you
helped make these decisions, so you are blind to the doubts you already rationalized away. So after
your pass, **spawn a fresh-context critic per layer to surface the doubts you missed**, and merge its
list into `doubts.md` before Phase D. That is how you reach *every* doubt without your own sweep
ballooning: one disciplined human-side pass, then an independent pass that catches the blind spots.
This `doubts.md` caps the whole run's ceiling — a thin one guarantees a shallow validation.

### Phase D — Adversarial validation, outside-in, with ripple-back  ·  run under /loop

This is the core, and it runs as a **`/loop`** (see *Driving it with /loop*). Build a task list that
walks L0 → L4. For each decision, run the **per-decision protocol**.

**Scale depth to stakes — but define "stakes" objectively, not by your own call.** A decision is
**load-bearing** if it is an L0 or L1 decision, *or* has a (graph-attacked) in-degree ≥ a threshold you
fix up front in `map.md`. Load-bearing decisions each get a full independent critic; the rest are
**batched under one shared, explicitly-named critic transcript** — so "batched" is a recorded choice,
not a silent skip. This is *computed from the map, not self-classified* — letting the anchored party
choose what deserves scrutiny is the obvious escape hatch. **Conflict-of-interest decisions** (tagged in
the register — by default *all* of them when you authored the corpus) get a **cross-model critic**, or
the two-disjoint-prompt same-model fallback when no peer exists; never a single same-model nod.

1. **Refute it — with an independent critic whose verdict binds and is provable.** Spawn a fresh-context
   subagent (Task/Agent, read-only). Do **not** hand it excerpts you chose — you are the anchored party
   and will, even unconsciously, frame it to flatter. Hand it a **rationale-redacted view of the
   register** — each decision's *statement* + its graph-neighbors + the pillars + `philosophy.md`, with
   the `rationale` and `alternatives` columns stripped — so it judges the decision on its merits instead
   of re-reading the author's defense (this also resolves the otherwise-contradiction of "give it the
   register but withhold the rationale": the register *is* the rationale until you redact it). Always
   include `philosophy.md` so the critic judges against the intended feel and the domain's craft, not in
   a vacuum. Prompt it roughly:
   *"Here is a decision, its connected decisions, the pillars, and the project/domain philosophy. Assume
   it is flawed. Make the
   strongest case it breaks a pillar, creates a strictly-better option, contradicts a neighbor, or fails
   in play. Default to 'broken' unless you genuinely cannot. Return: the objection, a **concrete
   reproducible failing scenario**, and a verdict — broken / holds / unsure."*

   **The verdict binds, and must be provable and un-shoppable:**
   - persist the critic's **raw return plus its agent id and timestamp** — never your paraphrase, which
     is just you authoring the verdict you wanted;
   - a `broken` / `unsure` caps the decision at `revise` / `unverified`; you may **not** upgrade it to
     `holds` yourself;
   - **no critic-shopping:** you may seek a new verdict only *after editing the decision* to address the
     prior critic's reproducible scenario (recorded) — never by re-spawning a critic on unchanged text
     until one happens to say `holds`;
   - a `broken` binds **only if it carries that concrete reproducible scenario**; a vague objection with
     none is logged as a non-blocking doubt, so a critic that merely *misread* the decision can't thrash
     a sound one. This is the adjudication path — fix the scenario, not the verdict.
2. **Steelman the alternative** and re-compare.
3. **Hunt contradictions across decisions.** "Which decisions contradict each other" is half the job,
   and a critic shown one decision can't catch a clash with decision #47 it never saw — so step 1's
   critic must receive the **graph-neighbors in full** and be told explicitly to find the contradiction
   among them. Your own consistency check is the anchored step; do not lean on it.
4. **Coherence vs pillars** (does it still serve L0?). For an L0 pillar this is circular, which is why
   pillars are attacked independently up front (Phase B).
5. **At L3–L4, simulate** (required — see next section). Never pass a mechanic or a number on reasoning
   alone.
6. **Verdict + falsification note.** Record `holds` / `revise` (with the fix) / `contradiction` (with
   the ids) — the verdict *being* the persisted critic transcript (id + timestamp) from step 1, never a
   label you typed — **plus a one-line "what would flip this verdict."** A `holds` without its surviving
   attack attached is indistinguishable from a rubber stamp, so it is treated as one (`unverified`).

**Ripple-back rule:** the moment a decision lands `revise` or `contradiction`, reopen its dependents
(outer *and* inner via the graph) and revalidate them this same loop. A change is not "done" until its
ripples are chased.

### Phase E — Synthesis: the validation report

Consolidate into `report.md`: decisions **confirmed**; decisions **to revise** (each with the why and
the proposed change); **contradictions** found; doubts that remain, each triaged to *resolved /
accepted-risk / deferred-to-calibration* with justification; and a cleaned, re-prioritized open-questions
list. For each confirmed decision, record **what attacked it and why it survived** (the critic verdict +
falsification note), not just the `holds` label — a conclusion without its surviving attack is
indistinguishable from a rubber stamp, which is the one thing this skill exists to prevent. Then feed
the revisions back into the design docs (or queue them for the user).

## Simulations at the micro layers (REQUIRED)

Once validation reaches the **micro layers (L3 mechanics and especially L4 parameters), abstract
reasoning is not enough — you must simulate with fictional data.** Numbers and mechanics fail in ways
prose hides; the only way to know a build is balanced is to *run it*. **Simulate to falsify, not to
confirm:** your goal is to hunt the numbers and the matchup where the design *breaks*, not to stage a
tidy case where it works. If the corpus already has a simulation harness or convention, reuse it
rather than inventing one.

For each micro decision under test:

1. **Use the corpus's real parameters where they exist; invent placeholders only for genuinely-unset
   values.** A sim on toy numbers certifies the *shape* of a toy, not the real config — so if the design
   already has tuning, simulate *that*. For values not yet set, derive placeholders from the design's
   stated tiers (or let the fresh-context agent pick them), label them as invented, and **fix them
   before you know the outcome** — numbers reverse-engineered to flatter the conclusion prove nothing.
2. **Build concrete units.** Instantiate the actual archetypes the design cares about (e.g. a plate
   knight, a leather skirmisher, a robe caster, an unarmed monk) with those numbers.
3. **Cover the spectrum of play, not a single snapshot.** A game is exercised across countless
   situations, and one scenario certifies only that one frame — the most common way a sim launders a
   broken decision into a `holds` is by testing it in exactly one comfortable configuration. So before
   you simulate, **enumerate the axes along which *this* game actually varies, then run a spread that
   crosses them** rather than one tidy case. The axes are domain-specific and you derive them from
   `philosophy.md`, but for a combat/systems game they almost always include: **where a unit sits in
   progression** (weak/early vs. fully-developed/late, and the curve between), **what role or build it
   is configured into** (and the contrasting builds it could have taken instead), **what it is matched
   against** (the opposing archetypes, not just a mirror), and **the situational and spatial context
   the encounter imposes** (starting position, who acts first, terrain/range, resource and tempo
   state). The break you are hunting almost always lives in a *corner* of this space — an option that
   is fair head-on but degenerate from an advantageous position, a build that is balanced mid-curve
   and oppressive at the top, a number that only fails in one matchup. Name the axes you covered **and
   the ones you deliberately skipped**, so a skipped corner is a recorded choice, not an invisible gap.
4. **Run it as code, not in your head.** You are unreliable at multi-round arithmetic, and one silent
   slip becomes a confident wrong "read" that the doctrine then launders into *evidence*. Write a
   throwaway script (save it under `simulations/`) that computes the rounds — the damage pipeline,
   defense rolls, the mobility/Weight cost, the turn economy, whatever the decision touches — across
   the scenario spread from step 3, and run it. The script's output is the ground truth; your prose
   only interprets it. For load-bearing sims, split the roles: a fresh-context agent fixes the numbers
   and scenarios and runs the script, you only read the result — so the party that wants it to hold
   isn't the one choosing the inputs *or* the cases.
5. **Read it against intent.** Does the intended balance hold **in every scenario you ran**, or only
   in some? Is any option **strictly better** — anywhere on the spread? Do the designed counters
   actually work? Does the math go degenerate (one-shots, unkillable turtles, dead stats, stalemates)
   in any corner?
6. **Sensitivity sweep.** Re-run with *different* plausible numbers. If the conclusion only holds for
   one magic set of values, that's a **fragility flag** — the design leans on a knife-edge and the
   register should say so. (This sweeps the *numbers*; step 3 sweeps the *situations* — both are
   required, and a decision that holds only at one point of either sweep is fragile.)
7. **Record every sim** under `simulations/` (the setup, the numbers, the scenario spread, the rounds,
   the read) so it is auditable and reusable on the next loop.

A micro decision is **not validated** until a script has *tried to break it* — across the spread of
scenarios (step 3) *and* the sensitivity sweep (step 6) — and failed. A decision exercised in exactly
one configuration, however clean that run looked, stays `unverified`; let the loop come back to it.

## Driving it with /loop (REQUIRED)

Run Phase D as a **`/loop`**. Each iteration advances the validation — a layer or a batch of decisions
with their critics, sims, and ripple-back — updates the artifacts, then assesses convergence. If not
converged, the loop continues; the long, iterative, come-back-to-it nature of real validation is
exactly what `/loop` is for. **You self-determine completion** — the user explicitly wants you to keep
looping until *you* judge the validation genuinely finished, not to stop at a fixed iteration count.

**Convergence — you do not self-certify it; an independent auditor does.** Stop only when ALL hold:

- every decision has a recorded verdict (the raw critic transcript, id + timestamp) **and a
  falsification note**;
- every micro (L3–L4) decision has a **script that tried to break it** (incl. the sensitivity sweep)
  under `simulations/`;
- every load-bearing decision (objectively, per the map) has a `holds` transcript — from a *cross-model*
  critic for conflict-of-interest ids;
- every *blocking* doubt is resolved / *accepted-risk* / *deferred-to-calibration*, in writing;
- ripple-back from the last change has been chased to stillness;
- **a fresh-context convergence auditor signs off.** This is the terminator. Spawn it with a **fixed
  adversarial prompt you do not soften**: *"Assume this run rubber-stamped itself. Default to NOT signing
  off. Re-run the sample, diff it against what's stored, and find the unresolved blocking objection."*
  The auditor (i) recomputes the load-bearing set from `map.md` and checks each member has a genuine
  `holds` transcript **from the right model** (cross-model / two-critic fallback for COI ids); (ii)
  **re-runs a seeded sample** — seed and pool drawn by a recorded script, not your choice — of
  decision-critics and sims from scratch, **persists their outputs, and diffs them against the stored
  transcripts**, so fabricated / paraphrased / shopped verdicts surface; (iii) attacks `report.md` and
  every accepted-risk call. It signs off only on **no unresolved substantiated blocking objection — new
  *or* already-logged** (substantiated = carrying a concrete reproducible scenario; default-to-broken
  vagueness doesn't count, which is what keeps the loop terminable). **No auditor-shopping:** a
  non-signing auditor is answered by a recorded *fix*, never by re-spawning auditors until one signs.

Wanting to stop because the loop feels long is the anchoring bias wearing a deadline. What licenses
stopping is the auditor's clean, freshly-run, *persisted* re-checks — not your own say-so.

## What this cannot do — the threat model, and the floor

Be honest about the limit, because pretending it away is the same bias the skill fights. This process
defends against your **unconscious** bias — anchoring, sycophancy, drift: a model *trying to do well*
that slides into ratifying what already exists. Against that, the redacted independent critics, the
persisted transcripts, the coded sims, and the adversarial auditor genuinely work — they put the honest
path in front of you and make you walk it.

What no process **can** do is stop a *deliberately bad-faith* executor: you spawn every agent and write
every prompt, so a determined cheat could fabricate a transcript or soften an auditor. An LLM cannot
cryptographically bind its own honesty. So the mechanisms aim one notch lower and reachable: make the
honest path the path of least resistance, and force cheating to be **explicit, recorded, and effortful**
rather than passive drift you wouldn't even notice. The **human reading `report.md` is the final
backstop** — which is exactly why every confirmed decision stores the attack it survived, so a person can
audit the auditor. Don't read this candor as permission to coast; it is the reason the artifacts have to
be real.

## Artifacts

Work in a fresh, timestamped validation workspace at **`tmp/validation-{timestamp}/`** — where
`{timestamp}` is generated once at the start of the run (e.g. `tmp/validation-20260626-143000/`) and
reused for the whole run. This path is **independent of which design corpus you are validating** — the
skill is general-purpose, not tied to any one project or system. Always create a new timestamped
directory rather than reusing or writing beside the docs.

Inside it: `philosophy.md` (the project intent + domain craft, written first and handed to every
critic), `register.md`, `map.md`, `doubts.md`, `simulations/`, and the final `report.md`. Keeping
them as files (not just in your head) is itself a de-bias — it externalizes the doubts so they can't
quietly disappear when convenient.

## Working with the user

Surface **blocking findings as you go** — a contradiction or a strictly-better option is worth
interrupting for, not saving for the end. Between major layers, checkpoint briefly so the user can
redirect. And report honestly: if a decision the user (or you) was proud of doesn't survive a sim, say
so plainly with the numbers. The value of this skill is precisely the findings that sting.
