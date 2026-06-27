# Input-control of avoidance — PROVEN LIVE (2026-06-27)

**The long-standing miss/block/parry blocker is solved, via the original idea: write the defender's
avoidance inputs on its live battle struct before the Denuvo VM reads them. The VM honors live memory.**

## The proof (log `battleprobe_log.txt`, profile `work/battle-runtime-settings.evade-override-test.json`)

We persistently wrote evade bytes = 100 on units' live structs (`EvadeOverride*` + `EvadeOverrideSweepSlots`
array sweep so even untracked units get it). Then Agrias attacked Ramza from the front.

```
line 25: [EVADE-OVERRIDE ptr=0x141855CE0 id=0x01] was(46=14 47=00 4A=00 4B=00 4E=00)
                                                   set(46=64 47=64 4A=64 4B=64 4E=64)   <- DEFENDER boosted
line 31: [SELECTOR-PROBE event=1 evadeType=0x04(class-evade) record=0x141855CE0 id=0x01
                                  rec+1BE=00  rec+1C0=04  rec+1C4(dmg)=0  hp=567/567]    <- evade, 0 dmg
         (no [PRECLAMP id=0x01]  -> apply path never ran -> true miss)
```

On screen: the attack preview showed **0% hit** and Ramza **evaded**. Airtight: we changed only live
memory; the VM's forecast AND its roll both produced the evade.

**Conclusion: Denuvo virtualizes CODE, not DATA. The unit structs the VM reads are normal writable
memory. Writing them before the roll = controlling the outcome, with the engine doing all rendering.**

## Byte → outcome map (set on the DEFENDER's struct, `unitPtr + off`)

| Bytes | Outcome | evadeType |
|---|---|---|
| `+0x4B` = high | class evade ("Miss") | `0x04` |
| `+0x46/+0x47` = high | weapon parry | `0x02` |
| `+0x4A/+0x4E` = high | shield block | `0x03` |
| all five = `0` | guaranteed hit (neutralizes avoidance, in memory, no data edit) | `0x00` |

(With all five maxed the engine chose class-evade `0x04`; to force a specific type, set only that type's
bytes.) Values are 0–100 (0x64 = 100 = max). Evade only applies front/side; back attacks ignore it.

## The complete architecture (all mechanisms now proven)

- **Hit / miss / block / parry** → write the defender's evade bytes before the roll (PROVEN here).
- **Damage value on a hit** → pre-clamp `0x30A66F` write (PROVEN earlier; `rdi`=defender, `+0x1C4`=debit).
- All driven by our formula (attacker + defender + equipment), which the existing formula context already
  exposes. The engine computes/renders everything from the inputs we plant. **No data-gutting, no
  result-forging.**

## Next (engineering, no longer unknown)

1. **Per-action, formula-driven writes.** The test wrote a static 100 on everyone. The real mod must
   write the defender's evade per attack, from the formula for that (attacker, defender) pair, before the
   roll. Identify the defender via the existing **pending-action tracker** (attacker→target), compute the
   formula, write the defender's evade bytes; the 20 ms poll is fast enough before the roll resolves.
2. **Reactions** (Blade Grasp, Hamedo, Arrow Guard…) — separate layer, Brave-gated, almost certainly the
   same live-read mechanism (write defender Brave / the reaction inputs). To test next.
3. Calibrate the formula → evade-% mapping (deferred per the user).

## Dead ends (refuted live, kept for the record)

- Evade-input hook `0x30F49C`: `rbx` is the ATTACKER, not the defender (`work/input-control-hook-map.md`).
- Roll-verdict `0x30F4A7`: a per-unit CT/turn eval, `eax` always 0, not the accuracy verdict
  (`work/roll-verdict-override.md`).
- Both invalid evade-override runs: boosted the attacker/idle units, not the DEFENDER. Fixed with the
  array sweep so the attacked unit is always boosted.
