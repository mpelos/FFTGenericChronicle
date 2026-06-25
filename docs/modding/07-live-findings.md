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

### Historical focused sub-problem (later refuted by Tests 2b/2c)
At this point in the investigation, the open hypothesis was that if vanilla never killed, **we**
could cause death when our formula was lethal by writing HP=0 and setting the death/status state
ourselves. Tests 2b/2c below refuted that hypothesis: HP=0 and `+0x61|=0x20` are effects/signatures
of death, not triggers. The accepted path now keeps custom HP writes above `MinHpFloor=1` and lets
the engine perform real KO.

### Incidental: Mana Shield
Attacks on a Mana-Shield unit were redirected to MP by the engine; this profile does not rewrite MP
(`RewriteObservedMpLoss=False`), so the full vanilla damage drained MP. Engine behavior, not a bug.
MP / Mana Shield is a separate channel to handle later.

### Historical next-gate order (pre-Test 2b/2c)
1. Data-layer **damage-neuter placeholder** (make all damage actions deal a small fixed nonlethal
   value via OverrideAbilityActionData / ItemWeaponData) - removes the death race and the flicker,
   and doubles as the start of the sentinel action-context channel.
2. Refuted later: death-causing path by writing HP=0 + death state for lethal custom formulas.
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
damage to ~one stat (non-lethal) whichever parameter the routine reads.

The 32 classified IDs beyond the 368-row override table are not a single monster-skill gap: they
are Throw 382-393, Jump 394-405, and Aim/Charge 406-413. Throw/Jump are expected to be covered by
the weapon `Power=1` neuter because the formula catalog routes them through WP; Aim/Charge has a
secondary table, so the generator now emits `AbilityChargeAimData.xml` with Aim +2..+20
`Power=1` (Aim +1 is already 1). Remaining risk: Throw/Jump still need a live spot-check, and
formulas that ignore X/Y/WP/charge Power (`%`-damage / Gravity-style effects) may still need a
separate lever. With weapon + ability + charge/aim neuter deployed, vanilla could no longer
one-shot in the common case, making the historical Test 2b byte-write probe clean enough to refute.

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

### 3. Death-causing write (gate step 2, the action) - BUILT, later refuted

New `Mod.cs` options `CauseDeathOnZeroHp` + `DeathStateWrites[]`. When our formula zeroes a unit's
HP and `CauseDeathOnZeroHp=true`, each `DeathStateWrite` does a read-modify-write on the unit struct
(`Offset`, `Width` Byte/Word/DWord, and one of `Value` / `OrMask` / `AndMask`) so a single status
**bit** can be set without clobbering the field. This was the configurable hook for setting the
death state ourselves once vanilla was neutered. Live Tests 2b/2c proved this does not cause real
KO; the setting remains only as legacy/refuted probe infrastructure.

### Historical live-test plan (kept for audit trail, not the active path)

Both runs need the **new DLL** deployed (`codemod\build-deploy.ps1`, Reloaded-II closed). Settings
hot-reload ~1/s; copy the chosen profile to
`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.

- **Test 2a - death-flag capture.** Profile `work/battle-runtime-settings.death-flag-capture.json`.
  **No data neuter deployed** (units must die normally). Observe-only. Let enemies die from vanilla
  damage. Then read the log for `[DEATH-DIFF]`/`[DEATH-FOLLOW]` offsets other than `0x30/0x31` (HP).
  A byte/bit that flips exactly at death is the death/status field.
- **Test 2b - death by HP write.** Deploy the **weapon + ability/spell + charge/aim neuter** data mod
  (`deploy.ps1`, after the `OverrideAbilityActionData` NXD has been built). Profile
  `work/battle-runtime-settings.death-test.json` (`FinalDamageFormula="9999"`, `AffectFoes` only).
  Attack an enemy with a neutered action: vanilla deals a small placeholder, then the reconciler
  forces that foe's HP to 0. This was the hypothesis test; the result below was zombie, not real KO.

Useful watchers:

```powershell
codemod\check-death-gate-readiness.ps1
codemod\prepare-death-gate.ps1 -DryRun -NeuterSpotcheck
codemod\prepare-death-gate.ps1 -NeuterSpotcheck
python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 3 --max-placeholder-damage 30 --max-large-vanilla-rewrites 0 --max-rewrite-failures 0
python tools\analyze_battleprobe_log.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.death-test.json docs\modding\examples\runtime-simulation-death-gate.example.json --no-trace
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.death-test-killflag.json docs\modding\examples\runtime-simulation-death-gate.example.json --no-trace
codemod\prepare-death-gate.ps1 -DryRun
codemod\prepare-death-gate.ps1
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --max-rewrite-failures 0
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --max-death-events 0 --settle-seconds 2 --max-rewrite-failures 0
codemod\prepare-death-gate.ps1 -KillFlag
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --death-writes 1 --max-rewrite-failures 0 --max-death-write-failures 0
```

The readiness check is read-only and, if these legacy probes are intentionally re-audited, should
be run before deploying/launching. The simulator
commands are also offline-only; they prove both death profiles compute the intended HP result
(foe -> 0 HP, allies/healing preserved) before any live write is attempted. The optional
`-NeuterSpotcheck` preparation deploys the same data neuter with a dry-run runtime profile first:
trigger representative attacks/spells/Throw/Jump/Aim and then run the analyzer to confirm observed
`vanillaDamage` deltas are placeholder-sized before replaying the legacy HP=0 probes; the watcher
guard fails if a large vanilla HP rewrite or rewrite failure appears. The HP-only death gate originally had two
watcher branches: one waited for lethal rewrite plus `[DEATH-*]` evidence, and the other waited for
a lethal rewrite with no `[DEATH-*]` events during a short settle window. The killflag branch then
added a concrete `[DEATH-WRITE]` for the mapped `+0x61` KO bit. The results below refuted both
byte-write paths: these commands remain useful only when re-auditing historical probes. Current
custom-formula/sentinel profiles should use `MinHpFloor=1` and let the engine own real KO.

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

### What this enabled at the time
`DeathStateWrites` was configured with `Offset=0x61 (97), Width=Byte, OrMask=0x20 (32)`, enabling
the decisive Test 2b/2c probes: `death-test.json` (HP=0 alone, `CauseDeathOnZeroHp=false`) and
`death-test-killflag.json` (HP=0 + set `+0x61` bit, `CauseDeathOnZeroHp=true`). The open question was
whether `+0x61|=0x20` plus HP=0 was enough, or whether death was tracked outside the unit struct.
Tests 2b/2c answered it decisively: byte writes are not enough and must not be used as the active
death path.

---

## LIVE TEST 2b/2c - death by struct-write (2026-06-21) - DECISIVE: IMPOSSIBLE

Full neuter (weapon + ability) deployed, clean per-target HP-write (any-target profiles in `work/`).
The question from 2a: does replicating death's struct signature (`HP=0` + `+0x61|=0x20`) make the
engine treat the unit as dead? **Answer: no. Two outcomes, both proven live:**

- **HP=0 alone -> zombie.** `[REWRITE …HP 304->0]`, `[DEATH-DIFF +0x30:30->00]` only. The unit
  stands at 0/HP, its CT keeps ticking, it takes turns. (Screenshot: Beowulf 0/314, alive.)
- **HP=0 + `+0x61|=0x20` (`CauseDeathOnZeroHp`) -> STILL zombie.** Log:
  `[DEATH-WRITE +0x61 0->20]`, `[DEATH-DIFF +0x30->00 +0x61:00->20]`, then `[HEALING 0->27->61]` -
  **Regen healed the unit back to life.** Regen does not tick on dead units, so the engine still
  considers it ALIVE. Setting the bit produced a buggy partial state (unit went immune, attacks
  passed through) the engine never expects to see.

### CONCLUSION: death cannot be caused by a memory write - definitive
`+0x61 0x20` is an **effect** of death, not a **trigger**. A real death changes exactly the same
bytes we write (`+0x30->00`, `+0x61:00->20`), yet real deaths die and our writes zombie. Therefore
death is an internal engine **routine** (almost certainly inside the Denuvo-virtualized damage path)
that updates state **outside** the unit struct (turn manager / active-unit list), keyed on the
engine's own damage reaching 0. We can replicate the symptoms but not invoke the routine.

### Implication (locks the architecture)
**DEATH must be owned by vanilla.** Our runtime HP-write owns the *number* (non-lethal custom damage
works - the core custom-formula goal is essentially proven); it cannot own *death*. So lethal
results must be delivered by letting the engine's own (neutered) damage reach 0 - see Test 3.

### Neuter gap surfaced here (real-mod TODO)
Special skillsets bypass the X/Y ability neuter: Cloud's **Materia Blade+** *basic* attack (its
weapon formula ignores the neutered WP) one-shots; **Cloud Limit** and some magic (likely
`%`-damage / Gravity, which ignore X/Y/WP) also slip through; and the classifier
(`HP`+`TargetEnemies` & not `TargetAllies`) is too strict, skipping offensive AoE skills that also
hit allies. Does not block the architecture (these still fall into Test 3's leave-at-1), but needs a
dedicated data/formula route before shipping.

---

## LIVE TEST 3 - engine-owned death via "leave-at-1" (2026-06-21) - PASSED

The architecture turn after 2b/2c. New `Mod.cs` lever **`MinHpFloor`** (default 0):

- every HP write clamps to **>= MinHpFloor**, so we NEVER write 0 -> can never create a zombie;
- `MaybeRewriteHpEvent` **skips** when observed HP is already `<= 0` (`[REWRITE-SKIP-DEATH]`), so we
  never resurrect a kill the engine made.

Flow: **neuter vanilla -> observe hit -> write HP = max(MinHpFloor, hp - customDamage)**. A lethal
result leaves the unit at the floor (1 HP); the engine's **own neutered chip on the next hit** takes
1->0 and the **engine kills it for real** (its real death routine sets `+0x61`, and the unit stays
dead - no Regen revive).

### Log evidence (profile `engine-death-test.json`, `MinHpFloor=1`, `FinalDamageFormula=9999`)
```
hit 1:  [REWRITE …HP 304->1]                              <- our write floors at 1
hit 2:  [DEATH-DIFF +0x30:01->00 +0x61:00->20]            <- ENGINE took 1->0 and killed it
```
Unit died and STAYED dead. **Proves the architecture end-to-end: we own the damage number (arbitrary,
non-lethal HP-write), the engine owns death (let its own damage reach 0).** Cost: death is a **2-hit
kill** (our write floors at 1, the engine chip finishes on the next hit). Clean same-hit death needs
the pre-damage window (stat puppeteering, item A2 below).

### ARCHITECTURE LOCKED
`neuter vanilla -> observe hit -> write HP = max(MinHpFloor, hp - customDamage)`; lethal results
leave the unit at the floor and the engine delivers the real kill. The custom-formula goal
(attacker + target + equipment) is proven viable.

---

## LIVE TEST 4 - attacker resolution by CT (2026-06-21) - PASSED

Until now every `[RUNTIME]` line showed `attacker=none action=none`: we knew the **target** (whose
HP changed) but not the **attacker**, so attacker-dependent formulas could not be computed. Solved by
struct RE.

### Method
Observe-only profile `actor-probe.json`: on every damage event it snapshots the `0x40-0x52` byte
window of **all** registered units (`[ACTOR-PROBE]`). The player ran **6 controlled attacks** and
reported who hit whom; correlating the windows to the attacks identified the field.

### Result: `+0x41` = CT (charge time); attacker = unit whose CT just reset
Team this battle: Ramza `0x01`, Ninja `0x80`, Agrias `0x1E`, Cloud `0x32`, Beowulf `0x1F`.
`+0x40` Speed (stable per unit): 10 / 16 / 12 / 9 / 9. CT `+0x41` per damage event:

```
#  attack (reported)              attacker  HP event        CT  R / Ni / Ag / Cl / Be   lowest
1  Ninja->Agrias (dual, hit 1)    Ninja     0x1E 322->310   70 / 12 / 84 / 63 / 63      Ninja
1  Ninja->Agrias (hit 2)          Ninja     0x1E 310->298   70 / 12 / 84 / 63 / 63      Ninja
2  Agrias->Beowulf                Agrias    0x1F 314->304   90 / 64 /  8 / 81 / 81      Agrias
3  Ramza->Cloud (Mana Shield)     Ramza     -- (MP, no HP event)                        --
4  Beowulf->Agrias                Beowulf   0x1E 298->295   20 / 52 / 64 / 28 /  8      Beowulf
5  Ramza->Agrias                  Ramza     0x1E 295->281    0 / 60 /100 /100 /100      Ramza
6  Cloud->Beowulf (lethal)        Cloud     0x1F 304->0      0 / 60 / 40 /  0 /100      tie->delta
```

- **5/6 resolve by absolute-lowest CT.** #3 (Mana Shield) produced no HP event (engine redirected to
  MP) - consistent, not a miss.
- #6 is the only tie (Ramza=0, Cloud=0). **Delta tiebreak:** Cloud dropped 100(#5)->0(#6) = just
  acted; Ramza was already 0 at #5 and stayed 0. -> attacker = Cloud. **6/6 with the largest-recent-
  drop tiebreak.**
- Corroboration: at #5 the three units that had not yet acted (Agrias/Cloud/Beowulf) were all at
  CT=100 ("charged, waiting in the act queue") while Ramza, who had just acted, was at 0 - exactly
  the FFT CT model.

### Rule (implemented baseline)
The code mod now resolves attackers by CT history: track `+0x41` per unit pointer; at a damage event,
attacker = the registered unit, != target, whose CT recently dropped/reset (`ct-reset`). Ties use the
drop history rather than raw "recent unit" order. The runtime falls back to the old recent-unit
heuristic only when configured/needed.

This is faction-agnostic and does not require an action-dispatcher hook. Formula context now exposes
the attacker and target stats live, including `attacker.ct`/`target.ct`, plus attacker-source flags
such as `a.sourceCt`. A counter fallback also exists: if a unit immediately damages the unit that
just attacked it, the resolver can invert the previous resolved HP-damage pair and mark the source
as `counter-inversion` (`a.sourceCounter`).

This unlocks attacker-dependent custom formulas. Offline and live-profile contracts now include a
demo-style formula path guarded on `a.present` and preserving engine-owned death via `MinHpFloor=1`.

### Still open after Test 4 (where deep RE helps most)
- **Pre-damage window (stat puppeteering, A2):** find a signal that fires BEFORE the HP write (in the
  turn-state around `battle_base_ptr`) so we can overwrite the attacker's stat just before the
  engine's calc -> the engine computes our exact number AND kills same-hit (removes Test 3's 2-hit
  cost). Highest-value RE target.
- **"currently-acting unit" pointer** directly in the battle/action state would be more robust than
  CT inference. Offline support now exists through the opt-in `[HOOK-REGS]` probe at the stable
  `battle_base_ptr` hook; live validation is pending.
- **Action identity.** Coarse action identity is now implementable through sentinel placeholder
  bands (`sentinel-coarse-v1`) and `ActionSignalRules`, but live calibration is still pending and
  the bands may overlap for some formulas.
- **Neuter gap** for special skillsets/formulas that ignore weapon `Power` or action `X/Y`/Aim
  `Power`; see `work/neuter_gap_targets.md`.

---

## LIVE TEST 5 - pre-clamp formula plan table for delayed AoE (2026-06-23) - PASSED

Goal: prove that a resolved action context can compute custom formula damage and feed it into the
engine's own HP-apply path before clamp/UI/final HP, instead of doing a late HP rewrite.

### Setup

Profile:

- `work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`
- `PreClampDamageRewriteEnabled=true`
- `PreClampFormulaPlanEnabled=true`
- `PreClampFormulaPlanRequirePhaseZero=true`
- `FinalDamageFormula="max(1, a.pa * 10 - t.faith)"`

Data mod was disabled. Active user-facing mods were only the utility modloader and the code mod.

Scenario:

1. Cloud confirmed delayed Cross Slash AoE on Agrias.
2. Beowulf, Agrias, and Ninja waited while Cloud's action remained pending.
3. Cross Slash resolved against Ninja and Agrias.

### Result

User observed:

```text
Agrias UI 77, HP 322 -> 245
Ninja  UI 68, HP 276 -> 208
Next active: Ramza
```

Expected vanilla values in this baseline were Agrias `115` and Ninja `273`.

### Log evidence

Artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-post-cross-slash-success.snapshot.txt`

Key lines:

```text
[PRECLAMP-PLAN-QUEUE ... id=0x80 hp=276/276 oldDebit=273 forcedDebit=68 ... pending=batch=1/act=258]
[PRECLAMP-PLAN-QUEUE ... id=0x1E hp=322/322 oldDebit=115 forcedDebit=77 ... pending=batch=1/act=258]
[PRECLAMP-REWRITE ... id=0x80 ... oldDebit=273 ... forcedDebit=68 ... action=258 ... live=hp=208 ... dmg1C4=68 ...]
[PRECLAMP-REWRITE ... id=0x1E ... oldDebit=115 ... forcedDebit=77 ... action=258 ... live=hp=245 ... dmg1C4=77 ...]
[DAMAGE ptr=0x141855EE0 id=0x80] 276 -> 208 = 68
[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 245 = 77
```

The runtime resolved the attacker/action as Cloud `0x32`, Cross Slash action `258`, source
`pending-clear`. The formula computed target-specific values from the real attacker and real targets.

### Conclusion

The pre-clamp plan-table architecture works for a delayed AoE action:

- pending-action context identifies the delayed caster/action;
- target damage caches expose each affected target before HP application;
- managed formula code queues target-specific staged result plans;
- the native hook rewrites the engine's staged debit in time;
- UI damage and final HP both use the custom formula result.

This retires late HP-write as the preferred architecture for formula-owned damage in this tested
path. Late HP-write and CT should remain fallback/diagnostic tools; the preferred path is now
pending/action context -> formula -> pre-clamp staged debit/credit rewrite -> engine-owned apply/KO.

### Still open after Test 5

- Prove immediate single-target attacks through the same plan-table path.
- Prove lethal custom damage causes same-hit KO through the engine path.
- Validate another delayed/charged action family beyond Cloud Limit.
- Stress multiple simultaneous pending actions.
- Generalize action identity and equipment/DR inputs for the real combat redesign.

---

## LIVE TEST 6 - formula-backed same-hit KO via pre-clamp plan (2026-06-23) - PASSED

Goal: combine the prior proofs:

1. formula-backed native pre-clamp plan table can replace vanilla staged damage;
2. lethal staged damage consumed by vanilla HP apply can produce a real KO.

### Setup

Profile:

- `work\battle-runtime-settings.preclamp-plan-lethal-braver-demo.json`
- `PreClampDamageRewriteEnabled=true`
- `PreClampFormulaPlanEnabled=true`
- `FinalDamageFormula="9999"`
- late observed HP rewrites disabled
- `CaptureStructOnDeath=true`

Data mod was disabled. Active user-facing mods were the utility modloader and the code mod.

Scenario:

1. Cloud selected Braver on Beowulf.
2. Preview showed vanilla damage `153`.
3. Cloud confirmed Braver, then waited.
4. Beowulf waited.
5. Braver resolved before Agrias became active.

### Result

User observed:

```text
Beowulf UI 999
Beowulf died: yes
Beowulf HP 0/314
Next active: Agrias
```

### Log Evidence

Artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-lethal-braver-success.snapshot.txt`

Key lines:

```text
[PENDING-ACTION-TRACK ... caster=0x1418562E0/id=0x32 act=257 ...]
[PENDING-ACTION-TRACK resolve-open batch=1 caster=0x1418562E0/id=0x32 act=257 ...]
[PRECLAMP-PLAN-QUEUE ... id=0x1F hp=314/314 oldDebit=153 forcedDebit=9999 ... pending=batch=1/act=257]
[PRECLAMP-FORMULA-CANDIDATE ... id=0x1F hp=314/314 oldDebit=153 forcedDebit=9999 ... attacker=0x1418562E0/id=0x32 source=pending-clear ...]
[PRECLAMP-REWRITE ... id=0x1F ... oldDebit=153 ... forcedDebit=9999 ... action=257 ... live=hp=0 ... dmg1C4=9999 ...]
[DEATH-DIFF ptr=0x1418564E0 id=0x1F] alive->dead +0x30:3A->00 +0x31:01->00 +0x61:00->20 +0x18C:00->01 +0x1BB:00->01 +0x1C4:99->0F +0x1C5:00->27 +0x1DB:00->20 +0x1EF:00->20 +0x1F5:FF->13
[DAMAGE ptr=0x1418564E0 id=0x1F] 314 -> 0 = 314
[HP-EVENT-PROBE ... rawForecastDamage=9999 lethal=1 hpClamp=1 rawForecastOverkill=9685 ...]
```

### Conclusion

Formula-owned lethal damage can be applied through the engine's own HP/KO lifecycle in the same hit.

- The runtime resolved Cloud as the delayed caster and Braver as action `257` via `pending-clear`.
- The target damage cache exposed Beowulf's vanilla staged debit `153` before HP application.
- Managed formula evaluation produced `9999`.
- The native pre-clamp plan rewrote the staged debit from `153` to `9999`.
- Vanilla HP apply clamped Beowulf from `314` to `0` and set the KO/death lifecycle fields,
  including `+0x61:00->20`.
- The UI displayed `999`, consistent with prior evidence that large staged damage is presentation-
  clamped even when raw staged damage is `9999`.

This is the current best architecture:

`pending/action memory context -> formula -> pre-clamp staged debit/credit rewrite -> vanilla apply/KO`.

The old late HP rewrite path and CT resolution remain useful as fallback/debugging tools, but they
should not be the primary route for the final combat redesign.

### Still open after Test 6

- Prove immediate/basic attacks through the same plan-table path.
- Validate non-Cloud charged action families, including actions without MP cost.
- Build a real action identity layer instead of relying only on action ids observed in memory.
- Investigate equipment/DR memory fields and expose them to formulas.
- Stress multiple simultaneous pending actions and reaction/counter flows.

---

## Prepared LIVE TEST 7 - immediate/basic attack through pre-clamp plan (2026-06-23)

Goal: prove that immediate actions can use the same formula-backed pre-clamp plan table without CT
as the primary attacker source.

### Setup

Prepared profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
- `PreClampDamageRewriteEnabled=true`
- `PreClampFormulaPlanEnabled=true`
- `PreClampFormulaCandidateRequirePendingMatch=false`
- `PreClampFormulaCandidateAllowImmediateAction=true`
- `ResolveAttackerByCt=false`
- `ResolveAttackerByLowCtFallback=false`
- `InferAttackerFromRecentUnits=false`
- `RewriteConditionFormula="event.isDamage && a.sourceImmediate && action.sourceImmediate && action.freshActiveAction"`
- `FinalDamageFormula="max(1, a.pa * 10 - t.faith)"`

Data mod should stay disabled. Active user-facing mods should be only:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`

### What changed in the runtime

The pre-clamp formula candidate path can now optionally resolve an immediate-action source when no
pending-action match is present.

The immediate resolver scans registered battle units and scores the action-state fields already
proven useful in the immediate KO probe:

- `unit+0x1A2` action id / last action id;
- `unit+0x1BA` active action marker (`ba` in logs);
- state/action age;
- freshness and staleness;
- exclusion of the HP target as source.

It only accepts a source if:

- the unit is source-like and alive;
- action id is positive;
- fresh active action is present by default;
- score is at least `PreClampImmediateActionMinScore`;
- score beats the next eligible candidate by `PreClampImmediateActionMinMargin`.

New formula variables:

- `attacker.sourceImmediate`, `a.sourceImmediate`
- `action.sourceImmediate`, `act.sourceImmediate`
- immediate-action diagnostics such as `action.freshActiveAction`, `action.actionIdAgeMs`,
  `action.activeActionAgeMs`, `action.margin`, and `action.runnerUpScore`

### Intended live scenario

Use a simple single-target basic attack where the vanilla damage is known and nonlethal. Recommended
first action:

1. Agrias attacks Beowulf with a basic attack.
2. Record UI damage and Beowulf HP loss.
3. Close the game so the log can be captured.

Expected if the immediate pre-clamp path works:

- damage should be formula-owned, roughly `Agrias PA * 10 - Beowulf Faith`;
- from recent known stats, this is expected to be around `45`, not the old vanilla `10`;
- logs should show:
  - `[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=... id=0x1E ...]`;
  - `[PRECLAMP-FORMULA-CANDIDATE ... source=immediate-action ...]`;
  - `[PRECLAMP-PLAN-QUEUE ... context=immediate-action/act=...]`;
  - `[PRECLAMP-REWRITE ... forcedDebit=<formula result> ...]`;
  - final `[DAMAGE ...]` matching the forced formula result.

Expected if it fails safely:

- vanilla damage applies;
- logs should still include `[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=none ...]` or a selected
  candidate whose score/margin/action-age explains why the formula did not queue.

Decision unlocked:

- Pass means immediate/basic attacks can share the same primary architecture:
  `action memory context -> formula -> pre-clamp staged debit -> vanilla apply/KO`.
- Fail means immediate actions need either a lower-level current-action pointer/hook or different
  freshness fields before they can retire CT.

### Live result - first attempt failed safely

Scenario:

- Agrias basic-attacked Beowulf.
- Data mod disabled.
- CT/recent fallback disabled by profile.

User observed:

```text
Preview/UI damage: 151
Beowulf HP after hit: 163/314
Agrias only attacked, did not finish the turn
```

Interpretation:

- Vanilla damage was `151`, and final HP loss was also `151`.
- No formula rewrite happened.

Key log evidence:

```text
[PRECLAMP-IMMEDIATE-CANDIDATES ... oldDebit=151 ... selected=none] ... Agrias ... act=0 ... b8=1/ba=0 ...
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=151 forcedDebit=151 shouldStage=0 queuedPlan=0 ... source=none ...]
[ACTION-STATE ptr=... id=0x1E ... act=0 ... b8=1 ba=1 ...]
[DAMAGE ptr=... id=0x1F] 314 -> 163 = 151
```

Conclusion:

- The profile failed in the safe direction: no confident attacker, no plan queued, vanilla damage
  passed through unchanged.
- The target damage cache appeared before Agrias had the stronger active marker:
  - during target cache: Agrias `b8=1`, `ba=0`, `act=0`;
  - shortly before HP write: Agrias `ba=1`, but `act` still `0`.
- Basic attacks may not use `+0x1A2` action id, or may represent "Attack" as zero.

Runtime follow-up prepared:

- Allow zero action id as an immediate source only when explicitly configured.
- Treat fresh `ba=1` with `act=0` as a possible basic/immediate source.
- When a source enters fresh `ba=1`, rescan other registered units for live target damage caches and
  re-run pre-clamp formula candidate evaluation before HP apply.
- Updated `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json` with:
  - `PreClampImmediateActionAllowZeroActionId=true`;
  - `PreClampImmediateActionMinScore=1600`.

Expected Test 7b:

- Repeat Agrias basic attack on Beowulf.
- If the new source-triggered rescan catches the window, damage should become formula-owned:
  `max(1, Agrias PA * 10 - Beowulf Faith)`.
- Logs should show `selected=.../id=0x1E/act=0` and `source=immediate-action`.

### Live result - second attempt found the source, but the freshness gate was too strict

Scenario:

- Agrias basic-attacked Beowulf.
- Data mod disabled.
- CT/recent fallback disabled by profile.

User observed:

```text
Preview damage: 151
UI damage: 151
Beowulf HP loss: 151
No critical
Agrias only attacked
```

Interpretation:

- This was still vanilla damage. The runtime did not queue a formula plan.

Log artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-7b-agrias-beowulf.snapshot.txt`

Key log evidence:

```text
[ACTION-STATE ... Agrias ... act=0 ... b8=1 ba=1 bb=1]
[PRECLAMP-IMMEDIATE-CANDIDATES ... oldDebit=151 ... selected=none]
  Agrias active-source-like score=250 eligible=0 act=0 freshActive=0
    stateAge=28302 actionAge=42928 activeAge=29386 ... b8=1/ba=1/bb=1
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=151 forcedDebit=151 shouldStage=0 queuedPlan=0 ... source=none ...]
```

Conclusion:

- The runtime did see Agrias as `active-source-like` with `act=0/ba=1`.
- The rejection was caused by `freshActiveAction=0`, because the active marker was about 29 seconds
  old at HP apply.
- For live basic actions, the active marker can remain valid across player/animation delay, so
  freshness should be a diagnostic or optional gate, not a mandatory rule.

Runtime follow-up prepared for Test 7c:

- Split `currentActiveAction` from `freshActiveAction`.
- Allow explicitly configured zero-action-id immediate sources to remain selectable while `ba=1`
  is currently set, even if the marker is no longer fresh.
- Expose `action.currentActiveAction` / `act.currentActiveAction` to formulas.
- Updated immediate-basic profile:
  - `PreClampImmediateActionRequireFreshActive=false`;
  - `RewriteConditionFormula="event.isDamage && a.sourceImmediate && action.sourceImmediate && action.currentActiveAction"`.

Expected Test 7c:

- Repeat Agrias basic attack on Beowulf.
- If this is the missing piece, logs should show `selected=... id=0x1E/act=0/currentActive=1`,
  `source=immediate-action`, a queued plan, and a pre-clamp rewrite.
- If it still fails, the next suspect is phase gating (`PreClampFormulaPlanRequirePhaseZero`) or the
  plan queue timing relative to the native pre-clamp hook.
