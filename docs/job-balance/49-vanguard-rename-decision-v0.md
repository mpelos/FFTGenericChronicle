# Vanguard Rename Decision V0

Status: Accepted after Claude cold re-review
Date: 2026-06-21
Depends on:
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/29-special-knight-v1-proposal.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `work/gpt-vanguard-rename-decision-v0.json`

## Decision

The Mime replacement job is renamed from the placeholder `Special Knight` to `Vanguard`.

`Vanguard` is now the display and planning name for the late generic elite frontline job. Existing
accepted documents that say `Special Knight` should be read as referring to `Vanguard` until a later
dedicated rename sweep updates historical references.

## Rationale

`Vanguard` fits the accepted job identity better than `Special Knight`:

- it communicates both leading the charge and holding the line;
- it supports the job's formation control, local protection, guard pressure, and setup-finisher
  fantasy;
- it avoids implying Holy Knight, Paladin, Oath, Faith, or long-range holy sword identity;
- it reduces the risk of reading the job as merely a stronger Knight variant;
- it works as a concise FFT-style job name beside titles such as Squire, Monk, Mystic, Orator,
  Dragoon, Samurai, Bard, and Dancer.

Rejected or lower-ranked alternatives:

| Candidate | Reason not chosen |
| --- | --- |
| `Vanguard Knight` | Accurate, but blunt and reintroduces the Knight-variant comparison. |
| `Oath Knight` | Strong flavor, but implies vow/holy/faith identity and can collide with Ramza's leadership fantasy. |
| `Aegis Knight` | Clear defense signal, but too shield-only and `Aegis` already exists as a unique action term. |
| `Sentinel` | Clean defender title, but loses the offensive initiative and elite late-job read. |
| `Warden` / `Ward Knight` | Protection flavor, but too jailer-like or awkward as a job display name. |
| `Bastion`, `Bulwark`, `Cataphract`, `Hoplite` | Mechanically adjacent, but too clunky or obscure beside FFT job names. |
| `Marshal` | Good command flavor, but too officer-like and not enough personal frontline protector. |

## Skill Rename Consequence

The V1 proposal currently contains an action placeholder named `Vanguard Break`.

Because the job is now `Vanguard`, that action name becomes circular. The committed guard-pressure
art should be renamed to `Breach` for future design passes.

Historical references:

- `Vanguard Break` in older drafts means the same action role now called `Breach`;
- `Vanguard Art` remains rejected as a free modal action pattern;
- `Intercede`, `Aegis Stance`, `Sunder Guard`, `Commanding Challenge`, and `Decisive Strike` remain
  acceptable placeholder action names.

## Update Policy

Do not perform a broad mechanical sweep of committed docs during this naming decision.

Instead:

1. This document is the authoritative alias note.
2. New documents and drafts should use `Vanguard`.
3. A later dedicated rename sweep should update historical `Special Knight` references in one
   coordinated pass with grep verification and Claude review.

This avoids partial-rename churn while preserving a clear forward name.

## Claude Review Request

Claude should confirm:

- `Vanguard` is the accepted consensus name;
- `Breach` is an acceptable replacement for the old `Vanguard Break` placeholder;
- the alias-note-first policy is sufficient before the later broad rename sweep.
