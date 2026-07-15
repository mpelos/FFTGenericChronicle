# DCL reaction taxonomy — offline checkpoint

## Goal

Implement the DCL Courage/Caution/Neutral reaction taxonomy while preserving the engine's native
reaction effects and RNG. Live testing remains deferred until the unmodified Enhanced executable can
reach the menu again.

## Hypotheses and verdicts

### H1 — one `computeActionResult` Brave scope covers every reaction

**Refuted by static call flow.** `computeActionResult` owns the VM-internal avoidance calculation but
returns before the later real-code post-hit reaction dispatcher. Restoring Brave at its exit cannot
affect Counter/Mana-Shield-class gates at `0x30BDxx..0x30BExx`.

### H2 — a hybrid input layer covers both known native paths

**Strong offline.** The four real-code Brave gates retain the exact evaluated Reaction id in `r11d`
or `ebx`; the mod can replace only their chance argument. VM-internal avoidance such as Shirahadori
still consumes defender Brave, so an explicitly marked evaluation temporarily receives its authored
chance during `computeActionResult` and the real Brave byte is restored at the guarded sole exit.

### H3 — calc entry exposes the exact VM reaction id, including innate reactions

**Strong offline.** Reaction staging writes the evaluated id to `orderRecord+2` and mirrors the same
id into `word[0x14186AFF0]`; native reaction formula dispatch then reads the global. Calc entry already
receives the order-record pointer in `rcx`. The VM path now keys rules from `orderRecord+2`, not from
the defender's equipped Reaction slot, so innate/derived evaluations have the same exact-id surface.

## Implemented surface

- `DclReactionRule`: exact native Reaction id, `courage|caution|neutral`, flat/custom curve, and an
  explicit `VmInternalAvoidance` classifier.
- Defaults: Courage = Brave; Caution = `100 - Brave`; Neutral = authored flat chance.
- Formula variables: `reaction.abilityId`, `reaction.brave`, `reaction.inverseBrave`,
  `reaction.flatChance`, and `reaction.isCourage/isCaution/isNeutral`.
- Real-code gates compose taxonomy chance first and DCL-miss suppression second.
- VM-internal rules use a thread-local balanced frame; the restore tail is installed before the entry
  writer. Exact expected-byte guards protect both ends.
- Persistent `BraveOverrideEnabled` and global `ReactionChanceControlEnabled` conflicts are rejected.
- The runtime fails safe to the native chance if context or formula evaluation fails.

## Static evidence

`work/1783988678-dcl-reaction-scope-analysis.md` validates ten anchors against executable SHA-256
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`:

- calc entry `0x3099AC`;
- restore epilogue `0x309FA1`;
- sole return `0x309FB0`;
- real-code reaction rolls `0x30BDEE`, `0x30BE44`, `0x30BE9A`, `0x30BEDA` with their exact-id
  registers;
- exact VM reaction id at `orderRecord+2`, its evaluation-global mirror, and the native formula read.

## Automated validation

- Release build: PASS, zero warnings/errors.
- Complete formula-runtime smoke suite: PASS.
- Courage/Caution/Neutral defaults: PASS.
- exact-id range, duplicate ownership, mode, neutral chance, formula variables, DCL pipeline, and
  conflict validation: PASS.
- static analyzer and Python compilation: PASS.

## Remaining boundary

The exact-id surface now covers equipped, innate, and derived VM evaluations. The remaining offline
boundary is per-ability route classification: only abilities proven to be VM-owned avoidance should
set `VmInternalAvoidance`; the rest stay on the exact real-code gate or retain a design/mechanism gate.

One live vertical slice remains required after the base game opens:

1. Caution Shirahadori at high Brave should use inverse Brave during forecast/execution while the
   displayed/permanent Brave remains unchanged after every calculation.
2. Courage Counter should use direct Brave at the exact real-code gate.
3. Neutral Mana Shield should use an authored flat chance.
4. A DCL miss must still suppress the post-taxonomy reaction chance.
5. Logs must show balanced VM virtualization/restoration and exact-id gate events without chicken,
   stuck Brave, or unrelated native-formula changes.

No runtime profile has been deployed and no live test was attempted.
