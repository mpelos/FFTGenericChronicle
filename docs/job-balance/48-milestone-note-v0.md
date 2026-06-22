# Campaign Validation Milestone Note V0

Status: Accepted as Marcelo-facing milestone note
Date: 2026-06-21
Depends on:
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `work/gpt-campaign-milestone-note-v0.json`

## Purpose

This is the milestone note to surface before drafting `Campaign Journey Bundle V0`.

It exists because doc 47 closed a major planning phase and created a natural steering point. The
project can now move into W2 journey rows, W3 producer passes, or the external T1/formula-v1 path.

Supersession note, 2026-06-22: doc 61 removes `JP Boost`. This Marcelo-facing note keeps its
overclaim guard intact, but its former JP-Boost risk should now be read as fixed-JP grind-to-break
pacing under ordinary, optimizer, and grind-heavy routing.

This note does not replace doc 47. It is a concise summary for Marcelo of what is proven, what is
not proven, and what decision is useful before more campaign-validation work is drafted.

## Milestone Summary

Generic Chronicle's job-balance planning has reached this state:

| Area | State |
| --- | --- |
| Generic job design roster | Complete and accepted. |
| Validation infrastructure | Accepted mechanic gates exist across docs 08-17 and 33-41. They prove the models compute correctly and reproducibly, not that all skill numbers are already balanced. |
| Vanilla skill/status atlas | Complete enough for design consultation and accepted as reference surface. |
| Concrete-provisional caster/performer clusters | Complete and accepted for docs 42-46. |
| Campaign readiness map | Accepted as doc 47. |
| Full campaign gameplay validation | Not complete. |

The accepted concrete-provisional clusters are:

- White Mage / Black Mage;
- Summoner / Geomancer;
- Time Mage / Mystic;
- Necromancer;
- Bard / Dancer.

These are planning-stable values, not final implementation data.

## What Is Proven

Current evidence proves:

- the intended generic-job roster and replacement jobs are defined;
- Calculator is replaced by Necromancer;
- Mime is replaced by Special Knight;
- Bard/Dancer gender restriction is preserved only for job access, with reaction/support/movement
  parity;
- the key mechanic validation gates exist and have been accepted as reproducible models with
  dual-independent checks and zero-row mismatch;
- those gates do not by themselves prove that current skill values are balanced; real-roster F5
  and T2.1 still have to prove that;
- concrete-provisional values exist for the highest formula-sensitive caster, performer, and dark
  state clusters;
- the campaign readiness map names the exact remaining producer steps and risk priorities.

## What Is Not Proven Yet

The full campaign goal is still open.

Current evidence does not yet prove:

- a fresh-game party of four generics plus Ramza progresses cleanly from Band 0 through Band E;
- exact JP, prerequisites, equipment timing, and fixed-JP progression pacing are healthy;
- Squire/Chemist/Knight/Archer/Monk/Thief/Orator/Dragoon/Samurai/Ninja/Special Knight have
  complete concrete action values;
- candidate reaction/support/movement values are safe in real cross-job builds;
- real-roster F5 confirms no dominant route across damage, sustain, control, mobility, and
  safety/risk;
- Ramza is broad without beating protected specialists in their own lanes.

## Highest Current Risks

Doc 47 makes these the current highest-priority risks, condensed from its full 12-entry register:

1. `Belief x Oil x fire-weak area` is the only already-quantified breach vector:

```text
Salamander, 3 targets, Belief x fire weakness/Oil = 681
681 / 415 = 1.641
```

2. Fixed-JP ordinary, optimizer, or grind-heavy routing can move deep power into earlier encounter
   bands.
3. Time Mage can compress too many axes through tempo, action economy, movement, and spell routing.
4. Caster economy can converge through MP, CT, Faith, and magic-support packages.
5. Mitigation and early physical full-package builds can erase campaign pressure.
6. Equipment-tier and mobility timing can create hidden spikes or universal default routes.
7. Late jobs and final Ramza must be strong without erasing older specialist jobs.

## Decision Point

The next work should be chosen deliberately.

| Option | What it does | When to choose it |
| --- | --- | --- |
| A - Draft W2 `Campaign Journey Bundle V0` | Creates provisional per-band party rows for P0/P1/P5/P2/P3/P4/P6, with encounter anchors and Ramza checks. | Best if the priority is to see the campaign as a player journey immediately. |
| B - Start W3 physical/foundation concrete values | Gives concrete action values to the remaining physical/foundation roster before journey simulation gets deeper. | Best if the priority is reducing unknowns before building W2 ceiling rows. |
| C - Build W3 A2 prerequisite/JP/equipment ledger | Starts exact pacing data for job unlocks, JP, fixed-JP routing, and equipment tiers. | Best if the priority is early power pacing and anti-overgrind/anti-rush proof. |
| D - Push T1/formula-v1 on Windows | Captures real weapon/equipment formula data and replaces WotL-fallback assumptions. | Best if the Windows environment is available and final numeric acceptance is the bottleneck. |

GPT and Claude's current recommendation is:

```text
surface this milestone first, then draft W2 unless Marcelo redirects
```

Reason:

- W2 directly advances the user's stated campaign goal;
- W2 can stay provisional while W1/W3 data is still incomplete;
- W2 gives T2.1 and F5 concrete party rows to test later;
- the milestone is a good point for Marcelo to redirect before more detailed rows are drafted.

## Recommended User-Facing Message

```text
Chegamos num marco importante: o roster de design dos jobs está fechado, os principais clusters
caster/performer/dark-state têm valores concrete-provisional aceitos, e o mapa de readiness da
validação de campanha foi aprovado pelo Claude.

Isso ainda não significa que a campanha está validada. Os gates provam que os modelos estão
corretos e reproduzíveis, não que os números já estão balanceados. O que falta agora é provar a
progressão real com 4 generics + Ramza: unlocks, JP, equipamentos, builds cross-job, RSM e F5 em
roster real.

Próximo passo recomendado: criar o W2 Campaign Journey Bundle V0, com linhas por fase da campanha
para P0/P1/P5 e wall tests. Alternativas seriam priorizar primeiro os valores concretos dos jobs
físicos/foundation, o ledger de JP/equipamentos, ou o dump T1/formula-v1 no Windows.
```

## Claude Review Request

Claude should review whether:

- the milestone summary is faithful to doc 47;
- the "what is proven" and "what is not proven" sections do not overclaim;
- the options A-D are the right steering options for Marcelo;
- the recommended user-facing message is accurate enough to send before W2.

## Claude Review Verdict

Claude accepted this milestone note on 2026-06-21 after the overclaim guard was added.

Accepted caveat:

- mechanic gates prove the models compute correctly and reproducibly;
- they do not prove current skill values are already balanced;
- real-roster F5 and T2.1 still need to prove campaign balance.

The note is cleared to surface before choosing the next campaign-validation work package.
