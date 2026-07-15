# DCL formula-0x38 retained-carrier checkpoint

## Native result

The formula dispatch table points `0x38` directly at status finalizer thunk `0x306988`. All 22
catalog members declare `Hit_(100%) Status`; no real-code chance/prerequisite wrapper can discard the
result before pre-clamp packet replacement.

## Fail-closed partition

Seventeen actions are representable by the current packet/contest model:

`149,181,182,187,188,189,190,191,192,193,195,287,326,327,328,350,356`.

They contain only a one-bit all-or-nothing result, deterministic cancel set, or independent
`Separate` bits. `retained-as-carrier` now requires complete native-bit ownership for each of them.

Five actions remain excluded:

- Suffocate `183`: Dead lifecycle;
- Nightmare `194`: random one-of Sleep/Death Sentence;
- Finishing Touch `262`: Dead lifecycle plus random one-of bundle;
- Toot `313`: random one-of Confusion/Sleep;
- Poisonous Frog `346`: Frog/Poison share one all-or-nothing contest.

## Verification

- `tools/analyze_dcl_formula38_carriers.py` guards the direct dispatch, 100% formula contract, exact
  22-row inventory, and 17/5 partition.
- Runtime smoke validates complete Salve remove ownership, rejects one missing inherited bit, and
  keeps Nightmare outside the carrier allowlist.
- `work/1784020869-battle-runtime-settings.dcl-formula38-salve-carrier.json` is a validator-clean
  three-bit remove profile with no action-data mutation.

## Remaining gate

One formula-`0x38` action must prove that its all-zero HP/MP result reaches exactly one managed
pre-clamp callback and one native packet commit. Grouped/random/KO members require their dedicated
mechanisms and are not authorized by that proof.
