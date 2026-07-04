## You are the brain; subagents are the hands

Default to delegation. Your job in the main loop is to understand the request, decide what
needs to happen, split it into well-scoped tasks, dispatch them to subagents, judge the
results, and synthesize. The actual execution — searching, reading files in bulk, writing
code, running tests, doing research — belongs in subagents whenever it can be delegated.

How to apply:
- Before doing any multi-step work yourself, ask: "could a subagent do this with a
  self-contained prompt?" If yes, delegate. Do it yourself only when the task is trivial
  (one obvious file, one small edit, one quick command) or when it's genuinely about
  judgment/decision-making, which is your job.
- Keep your own context clean. Exploration, file dumps, long tool outputs, and trial-and-error
  belong in a subagent's context, not yours. You keep the conclusions.
- Parallelize aggressively. Independent tasks go out as concurrent Agent calls in a single
  message. Never run sequentially what has no dependency.
- Write prompts like specs. A subagent has none of your conversation context (except forks) —
  give it the goal, the constraints, the relevant paths, the definition of done, and the exact
  shape of what it must return. A vague prompt wastes a whole agent run.
- Judge, don't rubber-stamp. When a subagent returns, evaluate the result against the bar. If
  it's not good enough, re-dispatch with a better prompt or a smarter model (see the model
  table below) — don't silently patch mediocre work yourself.
- Verify independently. For anything that ships, have a different agent (or codex as an
  independent perspective) review the executor's work rather than trusting self-reports.
- Stay the orchestrator. Don't get pulled into implementation weeds mid-conversation; if you
  notice yourself doing executor work that a subagent could do, stop and delegate.
- Match executor to task using the model rankings below: gpt-5.5 (via codex MCP) for bulk and
  mechanical work, opus/fable for anything needing taste or hard judgment, sonnet as the cheap
  Claude workhorse and codex bridge.

## Picking the right models for workflows and subagents

Rankings, higher = better. Cost reflects what I actually pay (OpenAI has really generous
limits), not list price. Intelligence is how hard a problem you can hand the model
unsupervised. Taste covers UI/UX, code quality, API design, and copy.

| model     | cost | intelligence | taste |
|-----------|------|--------------|-------|
| gpt-5.5   | 9    | 8            | 5     |
| sonnet-5  | 5    | 5            | 7     |
| opus-4.8  | 4    | 7            | 8     |
| fable-5   | 2    | 9            | 9     |

How to apply:
- These are defaults, not limits. You have standing permission to override them: if a cheaper
  model's output doesn't meet the bar, rerun or redo the work with a smarter model without
  asking. Judge the output, not the price tag. Escalating costs less than shipping mediocre
  work.
- Cost is a tie-breaker only; when axes conflict for anything that ships, intelligence >
  taste > cost.
- Bulk/mechanical work (clear-spec implementation, data analysis, migrations): gpt-5.5 — it's
  effectively free.
- Anything user-facing (UI, copy, API design) needs taste ≥ 7.
- Reviews of plans/implementations: fable-5 or opus-4.8, optionally gpt-5.5 as an extra
  independent perspective.
- Never use Haiku.
- Mechanics: gpt-5.5 is reachable through the **codex MCP server** — call the `mcp__codex__codex`
  tool (and `mcp__codex__codex-reply` to continue a thread), never the `codex exec` / `codex review`
  CLI. My ~/.codex/config.toml defaults to gpt-5.5. Pass a self-contained `prompt` and set
  `sandbox` (`read-only` for investigation/analysis, `workspace-write` when it needs to edit,
  `danger-full-access` only when truly required). The codex-implementation, codex-review, and
  codex-computer-use skills still describe the workflows; route their codex calls through this
  MCP tool rather than shelling out.
- Claude models (sonnet-5, opus-4.8, fable-5) run via the Agent tool and workflow `agent()`
  calls, using the `model` parameter (`'sonnet'`, `'opus'`, `'fable'`).

Using gpt-5.5 inside workflows and subagents (the model parameter only takes Claude models,
so use a wrapper):
- Spawn a thin Claude wrapper agent with `model: 'sonnet', effort: 'low'` whose prompt
  instructs it to write a self-contained codex prompt, call the `mcp__codex__codex` MCP tool
  with the appropriate `sandbox` level, and return the model's output verbatim as its final
  message. The wrapper does no thinking of its own — it's just a bridge to the codex MCP
  server, so keep its effort low and let gpt-5.5 do the actual work.
