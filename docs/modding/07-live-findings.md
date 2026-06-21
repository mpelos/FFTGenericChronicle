# Live Findings - Battle Runtime Code Mod

Running record of what we learn from **actual in-game tests** of the code-mod runtime engine
(`codemod/fftivc.generic.chronicle.codemod`). Each entry: what we ran, what happened, what it
proves, and what it changes. This is the live-validation counterpart to the offline architecture
in `06-code-mod-battle-runtime-architecture.md`.

---

## LIVE TEST 1 - HP-write proof (2026-06-21)

### Setup
- DLL: `iter 20` runtime engine (deployed build 2026-06-20 21:43).
- Profile: `hp-write-proof` (`work/battle-runtime-settings.hp-write-proof.json`) - forces every
  observed damage to exactly 1 via `FinalDamageFormula = "1"`, no context required, no response/DR.
- Mods enabled: `fftivc.utility.modloader` + the code mod. Data mod NOT loaded (not needed).
- Log: `…/FINAL FANTASY TACTICS - The Ivalice Chronicles/battleprobe_log.txt`.

### Player-observed behavior
- Player dealt 420 damage; it landed as 420 and the enemy died normally.
- Ninja dual-wielded a skeleton twice; both hits computed normally; enemy died.
- Goblin Spin Punch dealt 142 and subtracted the player's HP.
- A thrown stone previewed 72 but subtracted 69 (vanilla variance, not the mod).
- **At the start of a unit's own turn, that unit recovered ~100% HP.** Until its turn came, HP
  stayed at the post-hit value.
- A Ninja took a counter: damage number appeared over its head but no HP was subtracted.
- A monster in critical HP (crouch animation) healed to full at its turn, but the crouch animation
  persisted.

### What the log actually shows
Settings loaded correctly (`ProofFinalDamage=1`, `FinalDamageFormula=on`, item catalog 261 rows).
The engine computed `finalDamage=1` and **successfully wrote HP**:

```
[REWRITE id=0x01] rule=FinalDamageFormula vanillaDamage=192 finalDamage=1 HP 375->566
[REWRITE id=0x82] rule=FinalDamageFormula vanillaDamage=408 finalDamage=1 HP 89->496
```

Unit `id=0x01` has full HP = 567. The rewrite produced `567 - 1 = 566`, i.e. the mod **rewound**
the damage: it used the unit's *previous* (full) HP as baseline, applied 1, and wrote `full-1`.
Every `[RUNTIME]` line shows `attacker=none | action=none | targetSlots=none` (no context, as
expected for this profile).

### CONCLUSION 1 (the win): HP-write works live - PROVEN
We can detect a damage event and **write a unit's HP from our C# engine, and it sticks in the
running game.** This was the highest-risk assumption of the whole "own the final result"
architecture (`06`). It is now confirmed. The Denuvo blocker (can't hook the formula routine) is
genuinely bypassed: we never touched the formula routine, only the stable unit pointer + memory
write.

### CONCLUSION 2 (the flaw): the observation model is too coarse - MUST FIX
The "heal to full at turn start" is not a write bug; it is an **event-timing bug**. The
`battle_base_ptr` hook only samples a unit **when that unit is processed (its turn)**, and the
poller reads a single shared buffer fed by that hook. So:

- A unit's HP is only re-observed at its own turn, against a **stale baseline** (its HP from the
  previous turn, usually full).
- Non-lethal damage taken during *other* units' turns is therefore "rewound" to `lastSeenHp - 1`
  when the victim's turn finally arrives -> the 100%-heal-on-turn artifact.
- Lethal damage during another unit's turn kills the unit before its turn comes, so the mod never
  re-samples it -> lethal hits are never observed/rewritten at all (matches "420 killed normally").
- Critical-HP animation persisting = only the HP word was rewritten; animation/critical state is a
  separate field.

This single root cause explains **every** anomaly the player saw.

### THE FIX (offline dev task)
Change the poller from *"read one buffer fed by the hook"* to *"maintain a registry of every unit
pointer seen, and read each unit's HP directly every tick (~25 ms)"*:

- `previousHp` becomes fresh (value from ~25 ms ago, not from the last turn).
- Damage is caught in near-real-time on **any** unit regardless of whose turn it is.
- The turn-rewind artifact disappears; lethal hits become observable too.
- Code already uses `ReadProcessMemory` + `VirtualQuery` (safe against freed pointers), so this is
  a refactor of the poll loop, not new capability. Read the full struct per-pointer only when an
  event fires (for formula context).
- Optional later improvement: find a hook site that fires on HP *write* (damage application) for
  exact events instead of polling.

### Offline implementation status (2026-06-21)
Implemented in `codemod/fftivc.generic.chronicle.codemod/Mod.cs`:

- the hook path now only discovers/refreshes unit pointers and hook-touch timestamps;
- `_unitRegistry` tracks every battle-unit pointer seen by the hook;
- the poll loop reads every registered pointer directly each tick through
  `ReadableMemoryRange` + `ReadProcessMemory`;
- HP/MP event detection uses the direct live snapshot, so `previousHp` / `previousMp` should be a
  near-real-time baseline;
- HP/MP event logs now include `sampleAgeMs`, the age of the previous sample for that same unit
  pointer. This is the offline-checkable evidence that the baseline came from continuous polling
  rather than from the target's previous turn.
- `UnitPollIntervalMs` (default `25`) and `MaxTrackedBattleUnits` (default `64`) can be changed in
  runtime settings for live proof tuning without rebuilding;
- HP/MP writes now check that the destination word is writable and apply via `WriteProcessMemory`,
  logging a rewrite failure instead of using a raw `Marshal.WriteInt16` path;
- recent-attacker inference still uses hook-touch timestamps, not direct-poll timestamps, so
  continuous polling does not make every unit look like an attacker candidate.

Offline verification passed with `dotnet build`, formula runtime smoke tests, and runtime tooling
smoke tests. `tools/analyze_battleprobe_log.py` now includes an `HP Write Proof Check` that treats
concrete `finalDamage=1` rewrites with no failures and `sampleAgeMs <= 150` as a pass-candidate.
This is still not a live proof until a fresh game log shows it.

### STILL OPEN after Test 1
- **Context resolution remains 100% unproven** (`attacker/action/slots = none`). It is the next and
  biggest gate, but only after the observation model is fixed - reliable observation must come
  first.
- `72->69` confirmed as vanilla damage variance, not the mod.

### Next step
Re-run `hp-write-proof` and confirm **all** damage (non-lethal and lethal) becomes 1
**immediately**, with no turn-rewind. Then run `python tools\analyze_battleprobe_log.py` and
confirm the `HP Write Proof Check` reports fresh `sampleAgeMs` baselines and no rewrite failures.
To wait for the first fresh rewrite automatically, use
`python tools\watch_live_mapping.py --runtime-events 0 --rewrite-events 1`.
Only then proceed to Test 2 (live-noop context mapping).

---

## LIVE TEST D - Data-layer ability formula gate (2026-06-21)

### Setup
- Pure **data mod** test, code mod DISABLED (no log; player reports numbers).
- Proof NXD: `OverrideAbilityActionData` with `Y=99` for Cure(1)/Fire(16)/Thunder(20)/Blizzard(24),
  built via `FF16Tools.CLI sqlite-to-nxd -g fft` from `work/override_ability.proof.sqlite`,
  deployed to `FFTIVC/data/enhanced/nxd/overrideabilityactiondata.nxd`.
- JobData Move/Jump=9 kept as a **positive control** (confirms the data mod actually loaded).

### Result - PASSED
- Units moved/jumped 9 -> data mod loaded (control good).
- Fire damage was **absurdly high** -> the override `Y` change took effect.

### CONCLUSION: the EXE reads override Formula/X/Y - PROVEN
`OverrideAbilityActionData` is read for damage magnitude, not only CT/MP. This was the last
unproven data lever. **Level 1 (the full data-only formula layer) is now confirmed end-to-end:**
job stats (`JobData`), ability Formula/X/Y/Element (`OverrideAbilityActionData`), and weapons
(`ItemWeaponData`, same mechanism) can all be re-pointed/retuned with pure data.

### Strategic implication
A complete **data-only redesign path exists and is proven** (re-point every ability/weapon/job
formula + X/Y + element + status + CT/MP). The code mod (Level 2) is now only required for the
parts data cannot express - the v0.2 type-response/penetration armor matrix (see
`docs/formula-balance/`). The "modest redesign" fallback is fully viable today; the "full redesign"
still depends on the code-mod context-resolution gate.

### Cleanup
Proof NXD and the JobData Move/Jump=9 control were reverted after the test; the data mod is back to
a clean slate. Artifacts kept in `work/` (`override_ability.proof.sqlite`, `nxd_proof/`).

---

## LIVE TEST 1b - HP-write re-test with the fixed observation model (2026-06-21)

### Setup
- DLL: registry + continuous-polling build (deployed 2026-06-21 ~09:59, `UnitPollIntervalMs=25`,
  `MaxTrackedBattleUnits=64`).
- Profile: `hp-write-proof` (`FinalDamageFormula="1"`, AffectAllies+AffectFoes). Friendly-fire test.

### Log evidence
```
[REWRITE id=0x01] vanillaDamage=270 finalDamage=1 HP 297->566   (full HP = 567)
[REWRITE id=0x01] vanillaDamage=270 finalDamage=1 HP 296->565
[REWRITE id=0x1E] vanillaDamage=180 finalDamage=1 HP 142->321 -> 141->320 -> 148->319
[REWRITE id=0x1F] vanillaDamage=314 finalDamage=1 HP 0->313   <- LETHAL hit
```

### CONCLUSION 1: observation-timing fix WORKS for non-lethal damage
Baseline is now fresh and per-hit: each hit decrements by 1 (567->566->565...), caught within
~25 ms, applied immediately, **no turn-rewind**. The "heal to full at turn start" bug is gone.
Player perception "non-lethal hits never subtract HP" = they subtract exactly 1 (invisible on a
567-HP unit). The reactive model now reconciles non-lethal damage in near-real-time.

### CONCLUSION 2 (architectural): the reactive poller CANNOT prevent death
The lethal line `HP 0->313` shows the mod observed the kill (HP reached 0) and wrote 313 back -
but too late. The engine fires the **death state the instant HP hits 0**, before our 25 ms poll,
and **death is a separate state from the HP value**. Writing HP back does not un-kill the unit.
Polling is reactive; by the time it wakes, the unit is already dead. This is a fundamental limit of
"observe HP delta, then correct," not a tuning issue (a faster poll only narrows, never closes,
the race).

### What this forces (validates the `06` plan)
The reconciler is only viable **paired with a data-layer placeholder that neuters vanilla damage**
to a small, non-lethal, predictable value (ideally one that also encodes action identity = the
sentinel channel). Then the engine never triggers death or shows a wrong number, and our C# engine
owns the real outcome. Without the placeholder: lethal hits kill before interception, and
non-lethal hits visually flicker (e.g. 567->297->566). Test D already proved the data lever exists
(OverrideAbilityActionData Formula/X/Y is read), so the placeholder is buildable.

### New focused sub-problem
If vanilla never kills, **we** must cause death when our formula is lethal: write HP=0 AND set the
death/status state ourselves (locate that field/flag from the struct dumps). Focused RE task, not a
blocker for non-lethal mechanics.

### Incidental: Mana Shield
Attacks on a Mana-Shield unit were redirected to MP by the engine; this profile does not rewrite MP
(`RewriteObservedMpLoss=False`), so the full vanilla damage drained MP. Engine behavior, not a bug.
MP / Mana Shield is a separate channel to handle later.

### Revised next-gate order
1. Data-layer **damage-neuter placeholder** (make all damage actions deal a small fixed nonlethal
   value via OverrideAbilityActionData / ItemWeaponData) - removes the death race and the flicker,
   and doubles as the start of the sentinel action-context channel.
2. Death-causing path (write HP=0 + death state) for when our formula is lethal.
3. Then context resolution (attacker/action/equipment) on top of the clean placeholder signal.

---

## FIX PASS 1 - neuter + death-state instrumentation (2026-06-21)

Built in response to Test 1b's revised next-gate order. Everything here passed offline checks
(`codemod\run-offline-checks.ps1`) but is **not yet live-validated**.

### 1. Data-layer weapon neuter (gate step 1, physical half) - BUILT

`tools/build_neuter_data.py` generates
`mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/ItemWeaponData.xml`, forcing every weapon's
`Power` to 1 (126 weapons; Power 0/1 left untouched). Any weapon-power-based attack (`PA*WP`,
`WP*WP`, `(PA+Sp)/2*WP`, ...) now deals a tiny, non-lethal, but still non-zero delta the reconciler
can observe and own. This removes the death race and the flicker for human physical attacks.

Coverage / gaps (honest): covers attacks that scale with weapon Power. Does NOT cover bare-hands /
monster innate attacks or spell damage - that is now the ability neuter below.

### 1b. Data-layer ability/spell/monster neuter (gate step 1, magic half) - BUILT (2026-06-21)

Test 2b showed the real killers were enemy **spells** (126/157/170 dmg), which the weapon neuter
does not touch. `tools/build_neuter_data.py` now also builds the `OverrideAbilityActionData` NXD
(`mod/.../FFTIVC/data/enhanced/nxd/overrideabilityactiondata.nxd`). It classifies damaging
offensive abilities from `AbilityData.xml` `AIBehaviorFlags` (`HP` + `TargetEnemies`, not
`TargetAllies`) and forces `X=1, Y=1` on those 168 rows (base `Formula`/element/CT/MP left at
inherit). Verified round-trip: Fire/Thunder/Blizzard (16/20/24) -> X=1,Y=1; Cure/Cura (1/2)
untouched. No exe base formulas needed - Test D proved `Y` drives magnitude, so `X=Y=1` collapses
damage to ~one stat (non-lethal) whichever parameter the routine reads. Residual gap: 32 high-id
monster skills (382-413) are beyond the 368-row override table, and `%`-damage / Gravity formulas
ignore X/Y. With weapon + ability neuter deployed, vanilla can no longer one-shot in the common
case, so Test 2b (death by HP=0 write) can be re-run cleanly.

### 2. Death-state capture instrumentation (gate step 2, the measurement) - BUILT

The death/status flag offset is unmapped (docs/modding/05) and was **not** in any prior log
(diffs were off in the hp-write-proof profile). The continuous poller also never emitted
`[DUMP]`/`[DIFF]` (it ran with `logStructMapping:false`), so a unit dying on another unit's turn was
never struct-diffed. New `Mod.cs` option `CaptureStructOnDeath`: the first tick a unit is **observed
at 0 HP by any cause** (vanilla kill or our own HP=0 write), it logs `[DEATH-DUMP]` (full struct),
`[DEATH-DIFF]` (alive->dead byte changes) and `[DEATH-FOLLOW]` (changes over the next
`DeathCaptureFollowTicks` polls, to catch a flag set a few ms later). Observing at 0 HP rather than
on the transition event means it fires even when our rewrite sets the tracked HP to 0 and no delta
re-fires.

### 3. Death-causing write (gate step 2, the action) - BUILT, OFF by default

New `Mod.cs` options `CauseDeathOnZeroHp` + `DeathStateWrites[]`. When our formula zeroes a unit's
HP and `CauseDeathOnZeroHp=true`, each `DeathStateWrite` does a read-modify-write on the unit struct
(`Offset`, `Width` Byte/Word/DWord, and one of `Value` / `OrMask` / `AndMask`) so a single status
**bit** can be set without clobbering the field. This is the configurable hook for setting the death
state ourselves once vanilla is neutered. It stays inert until the offset from step 2 is filled in -
no rebuild needed, JSON only.

### Live-test plan (run in this order)

Both runs need the **new DLL** deployed (`codemod\build-deploy.ps1`, Reloaded-II closed). Settings
hot-reload ~1/s; copy the chosen profile to
`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.

- **Test 2a - death-flag capture.** Profile `work/battle-runtime-settings.death-flag-capture.json`.
  **No data neuter deployed** (units must die normally). Observe-only. Let enemies die from vanilla
  damage. Then read the log for `[DEATH-DIFF]`/`[DEATH-FOLLOW]` offsets other than `0x30/0x31` (HP).
  A byte/bit that flips exactly at death is the death/status field.
- **Test 2b - death by HP write.** Deploy the **weapon neuter** data mod (`deploy.ps1`). Profile
  `work/battle-runtime-settings.death-test.json` (`FinalDamageFormula="9999"`, `AffectFoes` only).
  Attack an enemy with a (neutered) weapon: vanilla deals ~PA, then the reconciler forces that foe's
  HP to 0. **Does it DIE or ZOMBIE at 0 HP?** If it dies -> writing HP=0 is sufficient, no
  death-state write needed. If it zombies -> fill `DeathStateWrites` from Test 2a, set
  `CauseDeathOnZeroHp=true`, retest.

The outcome of 2b decides whether the death-state RE (2a) is even on the critical path, before we
invest further in it.

---

## LIVE TEST 2a - death-flag capture (2026-06-21) - PASSED

Profile `death-flag-capture` (observe-only, no neuter). Player killed 5 units by normal damage.

### Result: the death/status flag is struct `+0x61`, bit `0x20`
All 5 `[DEATH-DIFF]` lines (char ids 0x82, 0x80, 0x32, 0x01, 0x80 - humans and monsters) were:

```
+0x30->00 (HP)  +0x61: 00->20
```

`+0x61` flips `00->20` on **every** death; nothing else changed consistently (`+0x63:01->00`
appeared once = noise) and `[DEATH-FOLLOW]` was empty (no delayed change within ~1.5s, in the
0x00..0x17F window). So within the unit struct, death = HP 0 **and** `+0x61 |= 0x20`. This maps the
first bit of the previously-unknown status region (docs/modding/05 updated).

### What this enables
`DeathStateWrites` is now configured with `Offset=0x61 (97), Width=Byte, OrMask=0x20 (32)`. Two
profiles are ready for Test 2b: `death-test.json` (HP=0 alone, `CauseDeathOnZeroHp=false`) and
`death-test-killflag.json` (HP=0 + set `+0x61` bit, `CauseDeathOnZeroHp=true`). Settings hot-reload,
so 2b can try HP=0-alone first and, if the unit zombies, swap to the killflag profile without
relaunching. Open question 2b answers: is `+0x61|=0x20` (plus HP=0) enough, or is death also tracked
outside the unit struct (turn manager / AI lists)?
