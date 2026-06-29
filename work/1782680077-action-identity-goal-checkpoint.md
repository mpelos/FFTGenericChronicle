# Action identity goal checkpoint

## Goal

Find the primary runtime source for DCL action identity:

- caster / attacker pointer;
- ability/action id;
- target unit or target batch at HP apply time;
- immediate vs pending vs reaction/counter source;
- hit/batch ownership for multi-hit and AoE.

The desired end state is a runtime model that does not use CT for mod ownership at all. Accepted
ownership must come from native-frame registers/stack/actor structs, pending context, selector
context, and final pre-clamp HP/MP targets. CT may appear in historical logs and comparison reports,
but it is not an accepted fallback for DCL runtime decisions.

## Offline validation already completed

The code already has three independent context channels:

1. **Pending action tracker**
   - Reads caster pending state from unit fields:
     - `+0x61` pending/status flag;
     - `+0x18D` pending timer/phase;
     - `+0x1A2` action id;
     - `+0x1EF` secondary pending/status flag.
   - Opens a resolving batch when pending flags clear while the action id remains.
   - Matches final target HP events using target cache fields:
     - damage: `+0x1C4`;
     - healing credit: `+0x1C6`;
     - charge/metadata: `+0x1D8`;
     - result kind: `+0x1E5`;
     - phase/hit marker: `+0x1BB`.
   - Offline smoke tests cover Cross Slash-style AoE batching and lethal clamp matching.

2. **Immediate action candidate scoring**
   - Uses action state age, active-source marker `+0x1BA == 1`, action id freshness, and
     target/staged-damage evidence.
   - Handles basic attacks where `+0x1A2 == 0` by allowing zero-action active-source candidates.
   - Offline smoke tests cover stale action id rejection and basic-attack active-source scoring.

3. **Pre-clamp actor-context resolver**
   - At the native HP pre-clamp frame, scans captured registers/stack roots for actor structs.
   - An actor struct links to its unit at `actor+0x148`.
   - Candidate action id is read at `actor+0x142`.
   - Existing docs identify this as the best primary path, but cross-battle stability and reactions
     still need live validation.

The existing live logs also support that `+0x1A2` / `actor+0x142` are real ability ids, not opaque
local counters:

| Observed id | Baseline ability |
| ---: | --- |
| `1` | Cure |
| `16` | Fire |
| `158` | Hallowed Bolt |
| `159` | Divine Ruination |
| `257` | Braver |
| `258` | Cross Slash |
| `265` | Choco Beak |

This is still not universal proof for every action class.

The offline gate passes after regenerating the legacy generated formula-context report.

Additional aggregate coverage report:

```text
work/1782694729-action-identity-existing-log-coverage.md
```

The existing log corpus is useful but not sufficient to promote actor-context to the runtime primary
resolver. It shows:

- 67 historical logs with action-identity markers;
- 27 `[PRECLAMP-ACTOR-CTX]` records, 17 resolved, 0 ambiguous, 0 unresolved positive-debit events
  after classifying 2 legacy self-hit/AoE hints;
- action ids observed through current tooling: `0` basic, `1` Cure, `16` Fire, `158` Hallowed Bolt,
  `159` Divine Ruination, `257` Braver, `258` Cross Slash, `265` Choco Beak;
- 6 `[SELECTOR-PROBE]` rows, with the current selector context extractor finding actor references
  in the latest normal-hit baseline;
- many legacy logs predate actor-context logging, so absence of actor-context there is coverage debt,
  not proof of failure.

The remaining offline conclusion is unchanged: existing logs validate the parser and several
surfaces, but the observe-only live probe still needs to cover basic/immediate, charged single-target,
charged AoE, and later reaction/counter/multi-pending edge cases.

Follow-up from the aggregate report:

- The two unresolved positive-debit actor-context events are the same Fire capture copied into two
  logs. The event is not generic "damage without context": it is an AoE/self-hit case where Agrias
  casts Fire and also appears as a damaged target.
- The old actor resolver intentionally excluded actor structs whose linked unit equals the current
  target (`actor+0x148 == target`). That works for normal target damage but fails when the caster is
  also one of the affected targets.
- Code change prepared offline: if there is no non-target caster actor and exactly one target-linked
  actor carries a positive action id, the probe now logs it as `verdict=resolved-self`.
- The next live run should verify that the same self-hit/AoE pattern emits `[PRECLAMP-ACTOR-CTX
  ... verdict=resolved-self ... actionId=<ability>]`.

## Probe prepared for live validation

Prepared profile:

```text
work/1782679952-action-identity-primary-probe.json
```

Deployed to:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json
```

The probe is observe-only:

- no HP/MP rewrite;
- no preview rewrite;
- no hit/status/reaction rewrite;
- no Reloaded mod enable/disable changes.

Important log lines:

- `[PRECLAMP-ACTOR-CTX]`: primary candidate, caster + action id from actor context at HP apply.
- `[PRECLAMP-EQUIP]`: target/caster equipment block at the actual damage frame.
- `[PRECLAMP-IMMEDIATE-CANDIDATES]`: active-source scoring for immediate/basic actions.
- `[PENDING-ACTION-TRACK]`: pending action lifecycle.
- `[PENDING-ACTION-MATCH]`: pending batch matched to target HP/heal event.
- `[HP-EVENT-PROBE]`: final target and staged debit/credit details.
- `[ACTION-STATE]` / `[ACTION-BOUNDARY]`: unit action-state diffs.

## First live test question

Does `[PRECLAMP-ACTOR-CTX]` resolve the correct caster and action id for:

1. an immediate basic attack (`actionId=0`, weapon identity from equipment);
2. an immediate named ability (`actionId > 0`);
3. a charged single-target action;
4. a charged AoE action with multiple final targets?

If this passes, actor context becomes the primary candidate for DCL action identity, with pending
tracker and selector context as complementary ownership surfaces.

If this fails only for basic attacks, the fallback likely remains `+0x1BA == 1` active-source plus
equipment identity.

If this fails for charged actions, the pending tracker remains primary for delayed actions until a
better actor/queued-action table is found.

## First live test script

Use the shortest stable save/battle available. Keep only the required mods enabled:

- `Generic Chronicle (Battle Probe)` / code mod;
- any data mod only if the current save/test setup requires it.

Do not enable extra experimental profiles.

Run these actions and then close the game:

1. Basic Attack: Agrias attacks Beowulf.
2. Immediate named ability: Agrias uses Divine Ruination or Hallowed Bolt on Beowulf.
3. Charged single-target: Cloud uses Braver on Beowulf.
4. Charged AoE: Cloud uses Cross Slash where it hits at least two targets.

For each action, record:

- action name;
- attacker/caster;
- target or center;
- whether the action was immediate or resolved after waits;
- UI damage/heal shown;
- final HP change;
- any miss/block/reaction/status/critical.

After the game closes, analyze:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt
```

Main success condition:

- every HP event should have a `[PRECLAMP-ACTOR-CTX]` line with the expected caster and either the
  expected ability id or `0` for a basic attack.

Secondary checks:

- charged actions should also show matching `[PENDING-ACTION-MATCH]`;
- AoE should keep the same caster/action across multiple target HP events;
- immediate basic attack should show a plausible `[PRECLAMP-IMMEDIATE-CANDIDATES]` source with
  `act=0` and `currentActive=1`;
- `[PRECLAMP-EQUIP]` should expose the caster weapon for basic attacks.

## First live test result

Evidence files:

```text
work/1782693058-action-identity-live-observe-log.txt
work/1782693058-action-identity-live-observe-report.md
```

User-observed actions:

1. Agrias -> Beowulf basic attack: UI -151 HP, final HP -151, immediate, no extra effect.
2. Agrias -> Beowulf Divine Ruination/Hallowed Bolt: UI -205 HP, final HP -205, immediate, no extra effect.
3. Cloud -> Beowulf Braver: UI -153 HP, final HP -153, delayed by one wait/turn, no extra effect.
4. Cloud Cross Slash AoE: Beowulf UI/final -230 and Ramza UI/final -187, delayed by two turns, no extra effect.

Analyzer result:

- `[PRECLAMP-ACTOR-CTX]` resolved every positive-debit damage event in the tested set.
- Resolved action ids:
  - `0` = basic attack / implicit weapon;
  - `159` = Divine Ruination;
  - `257` = Braver;
  - `258` = Cross Slash.
- Cross Slash produced separate HP/pre-clamp events per target and both resolved to the same
  Cloud actor/action (`actionId=258`).
- `oldDebit=0` rows resolved as `no-caster-actor`; these are tick/credit/passive-style records and
  should not be treated as action-identity failures.

Conclusion:

- Actor context is now a strong primary candidate for covered positive-debit HP events: basic,
  immediate named ability, charged single-target, and charged AoE in this battle/save.
- Keep pending tracker and selector context as complementary ownership surfaces until the remaining
  edge cases pass.

## Next live question

Can the runtime identify or safely classify action context for outcomes that do not necessarily
produce a normal HP apply event?

This is required for:

- miss/block/parry/Blade Grasp-style avoidance;
- Hamedo / First-Strike / counter-style reactions;
- reactions that cancel an attack before `[PRECLAMP-ACTOR-CTX]` fires;
- multiple simultaneous pending actions, where only the currently resolving action should own each
  selector/pre-clamp event.

Prepared selector profile:

```text
work/1782693143-action-identity-selector-probe.json
```

This profile keeps the existing actor-context/pre-clamp logs and additionally enables
`[SELECTOR-PROBE]` at the result/animation selector. It is observe-only: no hit, status, HP, MP, or
preview writes. It should produce selector rows for hit/miss/block/reaction outcomes, including
cases that never reach a positive HP debit.

The deployed DLL for this profile also extends `[SELECTOR-PROBE]` with `ctxRegs=[...]` and
`ctxStack=[...]`. Those fields print selector-frame roots that classify as `actor` or `unit`.
Purpose: determine whether no-HP outcomes carry only the target/result actor or also expose a
caster/source actor/action id somewhere in the selector frame.

The offline analyzer now parses `[SELECTOR-PROBE]` into a `Selector Outcomes` report section and
the aggregate coverage report counts selector events across logs. It also extracts selector-frame
actor refs from `actor=`, `ctxRegs`, and `ctxStack`, including action ids (`act=...`). This is the
main diagnostic for no-HP outcomes: if a reaction/miss never reaches pre-clamp, selector actor refs
tell whether the selector frame exposes only the target actor or also a caster/source actor.

Deploy verification:

- Runtime settings installed at `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`
  match `work/1782693143-action-identity-selector-probe.json` by SHA-256.
- Deployed code mod DLL size: `502784` bytes.
- After adding selector `ctxRegs`/`ctxStack`, the hook was audited and redeployed with the ring-buffer
  register restored before selector-control/record-copy work. Full `codemod/run-offline-checks.ps1`
  passes after this redeploy.

## Selector baseline result

Evidence files:

```text
work/1782694729-selector-baseline-log.txt
work/1782694729-selector-baseline-report.md
```

User-observed actions:

1. Agrias -> Beowulf basic attack: UI -151 HP, final HP -151, immediate, no extra effect.
2. Agrias -> Beowulf Divine Ruination/Hallowed Bolt: UI -205 HP, final HP -205, immediate, no
   extra effect.

Analyzer result:

- `[PRECLAMP-ACTOR-CTX]` resolved both positive-debit HP events.
- Resolved action ids:
  - `0` = basic attack / implicit weapon;
  - `159` = Divine Ruination.
- `[SELECTOR-PROBE]` emitted two normal-hit selector rows (`evadeType=0x00`).
- For the basic attack selector row, the selector frame exposed:
  - the result/target actor for Beowulf (`actor`, `rbx`, `r8`, stack `+0x8`/`+0xA8`);
  - the source/caster actor for Agrias in `rdx`, `r15`, and stack `+0xA0`, with `act=0`.
- For the Divine Ruination selector row, the selector frame exposed:
  - target record/unit for Beowulf;
  - the source/caster actor for Agrias in `rdx`, `r15`, and stack `+0x90`, with `act=159`.

Conclusion:

- For normal-hit outcomes in this battle/save, the selector frame carries enough context to identify
  both the result target and the source caster/action.
- This is a strong signal that no-HP outcomes may still be resolvable from the selector frame even
  when no positive HP pre-clamp event fires.
- It is not yet proof for avoidance/reaction outcomes; those are the next required live cases.

Next validation under this profile:

1. run one known avoidance/reaction case if the save can produce it safely, preferably Blade Grasp
   or an equivalent no-HP reaction outcome;
2. if available, run one counter/Hamedo-like reaction to see whether the reaction has its own actor
   context/action id, only selector evidence, or neither.

## Reaction/no-HP forced-input probe

Prepared profile:

```text
work/1782694941-action-identity-reaction-force-probe.json
```

Deployed to:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json
```

Deploy verification:

- Full `codemod/run-offline-checks.ps1` passes.
- Runtime settings installed in Reloaded match
  `work/1782694941-action-identity-reaction-force-probe.json` by SHA-256.
- The deployed DLL size remains `502784` bytes.
- The deploy used `-AllowReloadedOpen` because Reloaded-II was still running; the helper completed
  successfully and installed the settings.

Why this probe exists:

- A pure observe-only no-HP case is ideal, but it depends on the save having a reliable natural miss,
  block, Blade Grasp, Hamedo, or counter setup.
- Existing evidence says Blade Grasp is a native no-HP selector outcome:
  - selector `evadeType=0x0B`;
  - `+0x1BE=0`;
  - `+0x1C4=0`;
  - `+0x1E5=0`;
  - no downstream HP pre-clamp event.
- The latest selector baseline proves normal-hit selector frames expose both target and source
  actor/action through `ctxRegs`/`ctxStack`.
- This probe tries to answer the next question directly: does a native reaction/no-HP selector frame
  also expose source caster/action?

What the profile writes:

- Evade bytes are forced to `0` on live battle structs so ordinary miss/block/parry does not mask the
  reaction being tested.
- Brave `+0x2B` is forced to `100` on live battle structs so equipped Brave%-gated reactions should
  fire reliably.

What the profile does not write:

- no HP rewrite;
- no MP rewrite;
- no preview rewrite;
- no status write;
- no selector result-type/control write.

Primary expected success case:

- User attacks a defender that has Blade Grasp/Shirahadori or a similar Brave%-gated negating
  reaction.
- The reaction fires.
- The log has `[SELECTOR-PROBE ... evadeType=0x0B(blade-grasp) ...]`, no positive HP
  `[PRECLAMP-ACTOR-CTX]` for that attack, and selector `ctxRegs`/`ctxStack` show whether the source
  actor/action is present.

Secondary acceptable cases:

- If Counter or Hamedo fires instead, capture it too. It may create a second action rather than a
  pure no-HP avoidance, which is still valuable for reaction ownership.
- If no reaction fires, the profile should produce a normal connecting hit due to evade=0; this is a
  setup failure, not a selector failure.

Live script for this probe:

1. Launch FFT through Reloaded-II with `Generic Chronicle (Battle Probe)` enabled. Do not change the
   enabled-mod JSON manually.
2. Use a defender that has Blade Grasp/Shirahadori if available. Ramza was used in earlier Blade
   Grasp tests, but any known defender is fine.
3. Attack that defender with a basic physical melee attack.
4. Stop after the first reaction/no-HP outcome, or after two clean non-reaction hits if the setup is
   not firing.
5. Record:
   - attacker -> defender;
   - defender reaction equipped/expected;
   - action used;
   - whether Blade Grasp/Hamedo/Counter/other reaction fired;
   - UI damage;
   - actual HP change;
   - any extra animation/status.
6. Close the game so the log is flushed.

Analysis after the run:

```text
python tools/analyze_action_identity_log.py "<copied log>" -o "<timestamped report>"
python tools/report_action_identity_coverage.py work -o "<timestamped coverage>"
```

Decision rule:

- If selector actor refs include the source actor/action on a reaction/no-HP row, the selector frame
  becomes the primary identity source for no-HP outcomes.
- If selector actor refs include only the defender/target actor, then no-HP outcomes still require a
  pending/current-action source from action-state or a separate upstream hook.
- If the profile only produces normal hits, the next action is a setup correction: identify a
  defender with a known Brave%-gated reaction or switch to a controlled output-painted miss as a
  weaker fallback.

## Reaction/no-HP forced-input probe result

Evidence files:

```text
work/1782695389-reaction-nohp-selector-log.txt
work/1782695389-reaction-nohp-selector-report.md
work/1782695389-action-identity-existing-log-coverage.md
work/1782695865-action-identity-existing-log-coverage.md
```

User-observed actions:

1. Agrias -> Ramza basic attack. Ramza had Shirahadori/Blade Grasp-style reaction. Preview showed
   0% hit chance. Result: parried, no HP damage.
2. Cloud -> Ninja basic attack. Ninja had First Strike. Preview showed 100% hit chance. Result:
   counter-before reaction.

Analyzer result:

- The Shirahadori/Blade Grasp outcome produced a native no-HP selector row:
  - `[SELECTOR-PROBE event=1 evadeType=0x0B(blade-grasp)]`;
  - target/result unit `0x02` (Ramza in this save);
  - `rec+1BE=0x00`;
  - `rec+1C0=0x0B`;
  - `rec+1C4(dmg)=0`;
  - `rec+1E5=0x00`;
  - no positive-debit pre-clamp actor context for the parried attack.
- The same no-HP selector row still exposed the source actor/action:
  - target/self actor refs: `actor`, `rbx`, `r8`, stack `+0x8`, stack `+0xA8` -> unit `0x02`,
    `act=0`;
  - source actor refs: `rdx`, `r15`, stack `+0xA0` -> unit `0x1E`, `act=0`.
- The analyzer now reports this explicitly as:
  - `Selector no-HP outcomes with non-target source actor refs: 1/1`.
- The First Strike/counter-before case produced normal HP-apply rows against the original attacker:
  - target/result unit `0x32` (Cloud in this save);
  - source/caster unit `0x80` (Ninja in this save);
  - `actionId=0`;
  - `[PRECLAMP-ACTOR-CTX]` resolved the source for both positive-debit rows;
  - selector rows for the reaction damage also exposed source actor refs in `rdx`, `r15`, and
    stack `+0xA0`.

Conclusions:

- The selector frame is a strong primary identity source for native no-HP avoidance/reaction outcomes
  that never reach positive HP pre-clamp.
- For Shirahadori/Blade Grasp-style no-HP outcomes, selector context can provide:
  - result target;
  - source actor;
  - source action id (`0` for basic attack).
- First Strike/counter-before damage is not invisible to the HP path. The reaction attack's own
  damage reaches pre-clamp and actor-context resolves the reaction attacker as the source.
- CT is not needed for either of these two tested reaction cases and is not accepted as a runtime
  ownership fallback.

Parser/tooling changes made alongside this result:

- `tools/analyze_action_identity_log.py` now classifies no-HP selector outcomes and separates
  non-target source actor refs from target/self refs.
- `tools/test_action_identity_log.py` now includes a synthetic Blade Grasp selector row and verifies
  the no-HP source summary.
- `tools/report_action_identity_coverage.py` now aggregates no-HP selector outcomes separately:
  total no-HP rows, rows with non-target source actor refs, and no-HP source action ids.
- Aggregate coverage currently shows:
  - selector no-HP outcomes: `3`;
  - no-HP outcomes with non-target source actor refs: `1`;
  - no-HP source action ids seen: only `0` (basic attack / implicit weapon).
- Targeted validation passed:

```text
python -m py_compile tools\analyze_action_identity_log.py tools\report_action_identity_coverage.py tools\test_action_identity_log.py
python tools\test_action_identity_log.py
```

Next validation:

1. Retest a named action that is avoided/no-HP if an easy setup exists, to confirm selector source
   `actionId > 0` on no-HP ability outcomes.
2. Retest Hamedo/First Strike with a named incoming action if possible, to see whether the cancelled
   incoming action also stages a selector row or whether only the reaction attack does.
3. Test two simultaneous pending charged actions after selector identity is wired into the analyzer,
   because multi-pending remains the largest unresolved attribution class.

Offline refinement for the next live test:

- `OverrideAbilityActionData` does not settle whether Agrias' named swordskills are eligible for
  Shirahadori/Blade Grasp no-HP reactions. Rows `155..159` inherit their base action flags from the
  exe (`Flags12=[]`, `Flags34=[]` in the extracted sparse override table), and `AbilityData.xml` only
  exposes high-level AI metadata (`PhysicalAttack`, `HP`, `LinearAttack` for Divine Ruination).
- Existing selector evidence already proves `actionId=159` (Divine Ruination) is visible in selector
  actor refs on a normal hit, so Divine Ruination remains the highest-value candidate. Hallowed Bolt
  (`158`) is the backup candidate because aggregate logs have also seen it through the action
  identity tooling.
- The safe live gate is the preview, not static data: only confirm the named action if the UI shows
  the Shirahadori/Blade Grasp-style no-HP outcome (for this setup, a `0%` hit chance/parry preview).
  If Divine Ruination does not show that outcome, cancel and try Hallowed Bolt or another Agrias
  named physical swordskill.
- Desired proof row: a `[SELECTOR-PROBE]` with no HP pre-clamp, `evadeType=0x0B`, target Ramza, and
  non-target source refs for Agrias with `act=159` or `act=158`.

Aggregate coverage matrix:

```text
work/1782686079-action-identity-existing-log-coverage.md
```

The aggregate report now includes a DCL action-identity requirement matrix. Current matrix summary:

- Covered: HP-apply target/source/action, immediate basic identity, immediate named identity,
  charged/pending identity, selector-frame hit identity, and native no-HP reaction identity for a
  basic attack.
- Partial: AoE/multi-target pending batches, self-hit/self-AoE attribution, and cross-battle
  actor-array stability.
- Missing/Open: native no-HP reaction identity for a named action, multiple simultaneous pending
  actions, and tile/epicenter target reconstruction. Hamedo/First-Strike is now marked partial:
  reaction damage is visible, but the interrupted incoming action still needs authoritative
  target-cache/register proof.

This makes the next live test sharper: the first priority is not generic selector proof anymore; it
is specifically a no-HP selector row whose non-target source actor ref carries `act > 0`.

Named swordskill / Shirahadori live result:

```text
work/1782686404-hallowed-bolt-shirahadori-not-applicable-log.txt
work/1782686404-hallowed-bolt-shirahadori-not-applicable-report.md
work/1782686473-action-identity-existing-log-coverage.md
work/1782697538-hallowed-bolt-shirahadori-not-applicable-log.txt
work/1782697538-hallowed-bolt-shirahadori-not-applicable-report.md
work/1782697538-action-identity-existing-log-coverage.md
```

User-observed result:

```text
Agrias -> Ramza with Hallowed Bolt.
Preview: 100% hit chance, 259 damage.
Result: hit, 259 damage, Silence applied.
Ramza had Shirahadori, but Shirahadori did not apply to this ability.
```

Analyzer result:

- Hallowed Bolt is `actionId=158`.
- Actor context resolved Agrias (`id=0x1E`) as caster/source and Ramza (`id=0x02`) as target on all
  three observed pre-clamp actor contexts.
- Selector-frame actor refs also carried the named action:
  - `rdx -> Agrias act=158`;
  - `r15 -> Agrias act=158`;
  - stack `+0x90 -> Agrias act=158`.
- Selector outcomes were all normal hits (`evadeType=0x00`), with `rec+1C4=259`.
- `rec+1E5` was `0x80` on the pure damage row and `0x88` on rows that also carried the status bit;
  this matches the user-observed Silence side effect.

Conclusion:

- Hallowed Bolt is not a viable named no-HP/Shirahadori candidate in this setup.
- It is still useful positive evidence for selector-frame identity of named actions with side
  effects: source actor refs carry `act=158`, and resultKind `0x88` appears when damage + status are
  bundled.
- Divine Ruination was tested after this and also did not trigger Shirahadori/no-HP, so Agrias'
  swordskill branch is closed for this setup. Future no-HP reaction identity work should use a
  different reaction setup or a controlled no-HP output probe.

Named swordskill / Divine Ruination live result:

```text
work/1782698795-divine-ruination-shirahadori-not-applicable-log.txt
work/1782698795-divine-ruination-shirahadori-not-applicable-report.md
work/1782698795-action-identity-existing-log-coverage.md
```

User-observed result:

```text
Agrias -> Ramza with Divine Ruination.
Result: normal hit; Ramza did not parry and received the damage normally.
```

Analyzer result:

- Divine Ruination is `actionId=159`.
- Actor context resolved Agrias (`id=0x1E`) as caster/source and Ramza (`id=0x01`) as target.
- Selector outcome was a normal hit (`evadeType=0x00`) with `rec+1C4=250`.
- Selector-frame source refs carried the named action:
  - `rdx -> Agrias act=159`;
  - `r15 -> Agrias act=159`;
  - stack `+0x90 -> Agrias act=159`.

Conclusion:

- Divine Ruination is not a viable named no-HP/Shirahadori candidate in this setup.
- It is positive selector-frame identity evidence for `actionId=159`: the target/self actor refs
  stayed on Ramza with `act=0`, while non-target source refs carried Agrias with `act=159`.
- Together with Hallowed Bolt, this closes the current Agrias swordskill/Shirahadori branch. The
  next useful live work is stronger First Strike/Hamedo target-cache register proof for interrupted
  incoming actions.

First Strike / Hamedo cancellation refinement:

```text
work/1782685930-first-strike-cancelled-action-analysis.md
work/1782697916-reaction-nohp-selector-report.md
work/1782697916-action-identity-existing-log-coverage.md
```

The existing First Strike log separates the case into two surfaces:

- The interrupted incoming attack can leave a target cache/action-boundary on the original target
  (`dmg1C4=422`, `f1E5=0x80`, target Ninja), and immediate candidates can include the original
  attacker (Cloud). This did not reach a clean HP apply or selector row in the observed case, and the
  immediate-source heuristic was noisy because multiple units were active-source-like.
- The reaction damage itself resolves through the normal HP/selector path (Ninja -> Cloud) and is
  source-resolvable with actor context and selector actor refs.

The analyzer now includes a weak correlation pass for pre-apply target caches:

- It looks for `[PENDING-ACTION-TARGET]` rows with staged damage (`dmg1C4 > 0`, result flag `0x80`,
  and `bb != 2`) and then lists nearby `[PRECLAMP-FORMULA-CANDIDATE]` rows with the same target and
  same debit.
- This is useful for finding candidate sources around an interrupted incoming action, but it is not
  primary evidence. It is line-near correlation, not register-backed source proof.
- In the First Strike log, the interrupted incoming surface on Ninja (`target=0x141855EE0/id=0x80`,
  `dmg1C4=422`) has four nearby source hints:
  - Agrias / `source=immediate-action`;
  - Cloud / `source=immediate-action`;
  - `none`;
  - Cloud / `source=immediate-action`.
- That mixed result is valuable because it proves the current offline evidence is still too noisy to
  authoritatively name the interrupted incoming source. The next dedicated Hamedo/First Strike probe
  should capture register/stack actor roots directly at the target-cache write/transition, not only
  at later formula-candidate time.

So the open problem is narrower: not "can we identify reaction damage?" (yes, when it reaches HP
apply), but "can we authoritatively identify the incoming action before First Strike/Hamedo cancels
it?" That requires a later dedicated live test with a named incoming action and stronger register /
stack actor refs around the target-cache event.

Next probe prepared for that open problem:

```text
work/1782698530-action-identity-targetcache-register-probe.json
```

What this profile adds:

- `HookRegisterProbeOnTargetCache=true`: emits `[HOOK-REGS-EVENT kind=targetcache ...]` whenever a
  `[PENDING-ACTION-TARGET]` row looks like a pre-apply damage cache (`dmg1C4 > 0`, `f1E5` has the
  damage bit, and phase is not already apply/done).
- `LandmarkProbeEnabled=true` on the two known target-cache writes around unit/action field `+0x1C4`
  (`target-cache-write-1c4` and `target-cache-init-1c4`).
- Actor-struct classification for register/stack values: any pointer that links to a known live unit
  through actor `+0x148` is printed as `actor:id=...:unit=...:act=...`.

The proof we want from the next live run is one of these:

- Strong: a target-cache hook/landmark row for the interrupted defender record contains an actor ref
  for the original incoming attacker with the expected action id before First Strike/Hamedo cancels it.
- Useful negative: target-cache hook/landmark rows appear but contain no source actor refs; then this
  cache transition is target-only and we should hunt a different pre-roll/pre-reaction hook.
- Weak/insufficient: only line-near formula candidates appear; that repeats the old noisy evidence and
  should not be promoted to primary action identity.

Post-test analysis shortcut:

- `tools/analyze_action_identity_log.py` emits a `Target-Cache Register Verdict` section. Read it
  first. It counts `HOOK-REGS-EVENT kind=targetcache` rows with non-target actor refs and separates
  those from target/self-only refs.

First Strike target-cache register result:

```text
work/1782729990-first-strike-targetcache-register-log.txt
work/1782729990-first-strike-targetcache-register-report.md
work/1782729990-action-identity-existing-log-coverage.md
```

User-observed result:

```text
Cloud -> Ninja basic attack.
Ninja had First Strike.
Preview chance was reported as 100; forecast damage around 404.
First Strike fired before Cloud's attack.
Cloud took two UI hits of 396 and died at 0/428.
Ninja did not take damage.
No extra critical/status/effect was observed.
```

Analyzer result:

- The interrupted incoming surface on Ninja (`id=0x80`) appeared as target-cache damage:
  - line 164: `target=0x141855CE0/id=0x80`, `dmg1C4=403`, `chg1D8=130`, `f1E5=0x80`, `bb=1`.
- The target-cache register verdict is positive:
  - `Target-cache hook events with source-candidate refs: 2/2`.
  - line 165: `hookPtr=0x141855EE0`, target `0x141855CE0/id=0x80`, refs to Cloud (`id=0x32`) in
    `rcx`, `rdi`, `r8`, stack `+0x40`, stack `+0x70`.
  - line 183: same `hookPtr=0x141855EE0`, target `0x141855CE0/id=0x80`, refs to Cloud in `rcx`,
    `rdi`, `r8`, stack `+0x90`.
- The reaction damage surface then resolved normally:
  - actor context events 3 and 4 resolved Ninja (`id=0x80`) as caster/source and Cloud (`id=0x32`)
    as target for action `0` basic attack, debit `396`.
  - selector events 1 and 2 showed normal hit rows on Cloud with Ninja actor refs.

Conclusion:

- Basic First Strike cancelled incoming source is register-proven at target-cache time: when Ninja's
  target cache holds the interrupted incoming debit, the hook pointer and multiple register/stack
  unit refs point to Cloud, the original incoming attacker.
- Important nuance: the source-candidate refs in the live basic First Strike capture are direct
  `unit` refs, not source actor refs carrying `act=0`. That is enough to prove source unit ownership,
  but not enough to prove target-cache action-id ownership.
- This upgrades the interrupted-action surface from line-near heuristic to a viable primary signal
  for basic incoming First Strike/Hamedo-like cases.
- Scope limit: this test used a basic attack (`actionId=0`). The next remaining proof is a named
  incoming action into First Strike/Hamedo, to verify whether the same target-cache frame also exposes
  `actionId > 0` or only the source unit.

Current next experiment:

```text
work/1782730599-named-incoming-first-strike-runbook.md
```

Purpose:

- Try exactly one controlled named incoming action into the known Ninja / First Strike setup.
- If First Strike triggers, check whether target-cache source-candidate refs include an actor ref with
  the named `actionId > 0`.
- If First Strike does not trigger on the named action, treat that as a useful negative for the
  current setup and move to the fallback branch instead of grinding repeats.

Fallback priority if the named-interrupt branch is not viable:

1. Multiple simultaneous pending actions.
2. Tile/epicenter target reconstruction.
3. Named no-HP selector outcome through another reaction/control path.

Multiple-pending fallback prepared:

```text
work/1782730951-multi-pending-action-identity-probe.json
work/1782730951-multi-pending-action-identity-runbook.md
```

Purpose:

- Create a live overlap where two charged actions are pending at once.
- Prove whether `[PENDING-ACTION-MATCH]` can resolve the correct caster/action for each HP event
  while `activeBatches`, `trackedPending`, or `trackedResolving` is greater than `1`.
- Use the updated analyzer's `Pending Matches` table to inspect contention columns directly.
