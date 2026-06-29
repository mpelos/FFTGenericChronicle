# Multi-Hit Basic Attack + Counter Memory Note

## Manual result

Scenario:

- Attacker: Chemist, formerly Ninja, dual-wield.
- Defender: Agrias with Counter.
- Action: Basic Attack.
- Preview chance: 100%.
- Preview damage: 97.

Observed result:

- Defender took 97 and 97.
- Counter dealt 388 to the attacker.
- Attacker ended at 0/380.
- Defender ended at 271/465.
- Original action was not cancelled.
- Active unit after sequence: Agrias.
- No critical/status/extra effect observed.

## Captured files

- Raw log: `work/1782757320-multihit-counter-clean-log.txt`
- Generated report: `work/1782757320-multihit-counter-clean-report.md`

## Useful engine observations

This test is not about proving FFT Counter rules. Those rules are treated as known game mechanics.
The useful part is the runtime shape around a multi-hit action followed by a post-hit reaction.

The two basic-attack hits appear as separate pre-clamp HP events against the same target:

- `PRECLAMP-ACTOR-CTX event=1`: target `id=0x1E`, caster `id=0x80`, `actionId=0`, debit 97, verdict `resolved`.
- `PRECLAMP-ACTOR-CTX event=2`: target `id=0x1E`, caster `id=0x80`, `actionId=0`, debit 97, verdict `resolved`.

The Counter appears as a separate HP event with target/caster inverted:

- `PRECLAMP-ACTOR-CTX event=3`: target `id=0x80`, caster `id=0x1E`, `actionId=0`, debit 388, verdict `resolved`.

The later duplicate/secondary pass for the same counter-shaped lethal event showed a weaker pre-clamp actor context:

- `PRECLAMP-ACTOR-CTX event=6`: target `id=0x80`, `caster=none`, `actionId=-1`, verdict `no-caster-actor`.

However the selector frame for that same target still carried actor references for both sides:

- `SELECTOR-PROBE event=6`: record/actor target `id=0x80`; `rdx` and `r15` point to actor `id=0x1E`, action `0`.

## Current implication

For post-hit reactions, the native frame evidence supports a no-CT path:

1. Prefer pre-clamp actor context when it resolves target, caster, and action id.
2. Use selector-frame actor pairs as fallback when pre-clamp actor context loses the caster.
3. Treat target-cache/pre-clamp damage fields as numeric result carriers, not as sole ownership proof.

Do not use CT for ownership. CT remains diagnostic-only.

## Open caution

The log contains duplicate-looking passes for the same visible sequence. Before promoting this into
`docs/`, compare against another reaction or isolate why the sequence emitted both a resolved and a
weaker caster context for the counter-shaped lethal event.
