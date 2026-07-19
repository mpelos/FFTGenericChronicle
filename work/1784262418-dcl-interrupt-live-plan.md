# DCL Interrupt Focused Live Plan

## Boundary

This probe validates only the job-free combat mechanism. Native Potion (`abilityId=368`,
`actionType=0x06`) is a temporary harmless carrier because the existing Rion fixture already exposes
Items and Potion targets allies. Death (`abilityId=30`) is only the long charged action used as the
observable queue. Neither choice assigns a production skill, item effect, skillset, or job rule.

The existing audited Manual Save 05 fixture already contains:

- Rion in roster unit 3 with Items accessible;
- Josephine in roster unit 6 with Death learned and accessible;
- no additional save edit is needed for this probe.

Seed artifact:
`work/1784104009-lt23-save05-rion-and-josephine-counter-fixture.png`.

## Profiles

- `1784262418-battle-runtime-settings.dcl-interrupt-logonly.json`: forced Interrupt roll 18,
  log-only; reaches `eligible-log-only` but cannot write.
- `1784262418-battle-runtime-settings.dcl-interrupt-resisted.json`: forced roll 3, live transaction
  armed but resistance must stop it before any write.
- `1784262418-battle-runtime-settings.dcl-interrupt-success.json`: forced roll 18, live transaction,
  one-write cap.

All three profiles pass runtime-settings validation. They force the authored Potion hit and leave
native Potion healing unchanged.

## One-time fixture construction

1. Back up the installed DLL/settings, Reloaded AppConfig, live manual save, live autosave, and log.
2. With FFT stopped, deploy the audited Manual Save 05 seed and the log-only profile.
3. Load Manual Saves > first row (Save 05) through the runbook's atomic title sequence.
4. Start a random battle with Rion and Josephine deployed adjacent.
5. Let Josephine cast Death on a durable enemy.
6. Reach Rion's actionable turn while Josephine is still charging.
7. Close FFT at that turn, snapshot `autoenhanced.png`, restore it once, and verify Continue returns
   to the same Rion-turn/Josephine-charging state.

That autosave becomes the immutable start for every pending-action branch.

## Test matrix

### A. Log-only eligibility

Restore the pending fixture, deploy the log-only profile, Continue, and use Rion's Potion on
Josephine.

Pass:

- `[DCL-HIT] ... ability=368 type=0x06 ... outcome=hit`;
- exactly one `[DCL-INTERRUPT] ... outcome=eligible-log-only`;
- pending state shows both Charging mirrors, timer other than 255, and `action=30`;
- `before` and `after` are identical;
- Death later resolves normally.

### B. Forced resistance

Restore the same pending fixture and use the resisted profile. Use Potion on Josephine.

Pass:

- `resistance=(Josephine Brave curve) roll=3 outcome=resisted`;
- no cancellation write and no tracker eviction;
- Death resolves normally.

### C. No-pending negative control

The A/B profile gates its rule with `interrupt.pending`, so it would classify this branch as
`condition-false` before reaching the explicit no-pending guard. Preserve that used profile and use
`work/1784272266-battle-runtime-settings.dcl-interrupt-no-pending.json`, whose otherwise identical
rule condition is always true. Restore the immutable fixture and use Potion on Rion, the guaranteed
non-charging unit, instead of reconstructing a second battle state.

Pass: `outcome=no-pending-action`, no write, no Charging change.

### D. Ability-identity negative control

Restore the pending fixture under the success profile, but use Rion's High Potion on Josephine
instead of Potion. High Potion remains in the same Item action family and can legally target the
allied pending caster, while its ability id does not match the rule.

Pass: High Potion resolves natively, there is no matching Interrupt log, and Death still resolves.

The earlier Throw Shuriken route is invalid for this fixture because Throw cannot legally target
the allied Josephine. It must not be treated as a tested control.

### E. One capped successful cancellation

Restore the pending fixture under the success profile and use Potion on Josephine.

Pass:

- exactly one `roll=18 outcome=cancelled`;
- read-back shows `timer=255`, Charging cleared in both mirrors, source/type/action preserved, and
  `writes=1`;
- Death never resolves even after enough scheduler time;
- no Charging/timeline/presentation residue remains.

The write-cap guard is evaluated only after a matching target is confirmed to have a live pending
action. A second Potion against the already-cancelled Josephine would therefore correctly produce
`no-pending-action`, not `write-cap`; it cannot validate the cap. Validate the cap only after F has
proved recovery by queuing a second real pending action on Josephine in the same process and then
delivering another matching Potion before that action resolves. Pass: the second live-pending event
yields `write-cap`, performs no second write, and the newly queued action resolves normally.

### F. Recovery

Continue E until Josephine's next normal turn. Queue another ordinary charged command against a
legal target, preferably the same self-targeted Death used by the immutable fixture. Then let Rion
deliver Potion to Josephine before the charge resolves.

Pass: the command previews, enters a fresh visible pending state, and returns control normally;
Josephine is not stuck in a hidden pending state. The later Potion observes that fresh pending state
and is stopped by `write-cap`; the second command then resolves normally, closing both recovery and
one-write-cap behavior without treating a no-pending target as cap evidence.

## Stop conditions

Stop live writes immediately if any of these occurs:

- pending action id is not Death `30`;
- source-owned Charging is present;
- the two Charging mirrors disagree;
- read-back verification fails;
- the game crashes or UI remains stuck after the first write;
- more than one cancellation write appears.

Archive each log under a new timestamp before the next profile. Restore all pre-test installed/live
files after the matrix.
