# Reaction input-control (Blade Grasp / Hamedo / Arrow Guard…) — offline-proven 2026-06-27

**Claim: reactions are controllable by the SAME input-control mechanism as evade — write the
defender's Brave (`+0x2B`) on its live battle struct before the roll. `Brave = 0` suppresses the
Brave%-gated reactions; `Brave = 100` forces them. Live confirmation pending (the harness is built).**

## Why this follows from the evade breakthrough

The evade proof established the meta-fact: **the Denuvo VM reads unit-struct bytes LIVE; code is
virtualized, data is not.** Brave (`+0x2B`) is a struct byte. So whatever consumes it — real code or
VM — reads our live write. The only open questions were (a) is `+0x2B` actually consumed by the
reaction roll, and (b) which direction suppresses. Offline RE now answers both.

## The binary evidence (`work/disasm_reaction_scan.py`, output `…scan.out.txt`)

Robust byte-scan (linear capstone sweep desyncs and under-reports — use byte-pattern scans):

- **34 real-code readers of `+0x2B` (Brave)**, RVA < 0x610000.
- **11 real-code callers of the VM roll primitive `0x278EE0`** (`jmp` into `.edata` ⇒ roll arithmetic
  is VM, as expected; only the arithmetic, not the inputs/branch).
- A **cluster of FOUR roll sites at `0x30BE86 / 0x30BEDC / 0x30BF32 / 0x30BF72`**, each with the
  identical canonical pattern, sitting in the combat-resolution region (between APPLY `0x30A51C` /
  pre-clamp `0x30A66F` and PRODUCER `0x30F0C4`):

```
mov ecx, 0x64                 ; ecx = 100  (roll range / max)
movzx edx, byte [rax+0x2b]    ; edx = defender Brave   <-- raw Brave, NOT 100-Brave
call 0x278ee0                 ; roll(100, Brave)  => trigger chance = Brave%
```

  The defender is loaded from a global staging pointer (`mov rax,[rip+0x155f0xx]`) and `+0x2B` is
  dereferenced **live** — there is no real-code pre-copy of Brave on this path. Each site is guarded
  by an applicability flag (`cmp dword [rip+…],0; jne skip`) and an `[r11+0x27]` sign check — i.e. "is
  this reaction equipped / applicable" gates. **Four sites ≈ four Brave-gated reaction checks.**

This is the canonical FFT reaction formula (`trigger% = Brave`). Direction is therefore unambiguous:

| Defender `+0x2B` (Brave) | roll(100, Brave) | Brave%-gated reaction |
|---|---|---|
| `0` | always fails | **never fires** (suppressed) |
| `100` | always passes | **always fires** (forced) |

(Other `0x278EE0` callers use different inputs: `0x304E33` uses `+0x2C` Faith × MA — a magic/Faith
calc; `0x306636` uses `[+0x1A8]`; `0x271D88` & `0x3083xx` compute `100-Brave` — likely Brave-change /
AI, not the avoidance reaction. The avoidance-reaction battery is the `0x30BExx` cluster.)

## The reaction-slot is NOT a struct byte (so Brave is the lever, not slot-blanking)

Prior offline RE (`input-control-hook-map.md`): there is no Reaction/Support/Movement id triad in the
`0x200` battle struct; ability data is fetched via VM resolver `0x2BB0D4` from the character/skillset
object behind a `+0x70`/`+0x78`/`+0x83` pointer. ⇒ you cannot disable a reaction by zeroing a slot
byte. Two viable levers remain:

1. **Brave input-control** (this doc): force defender `+0x2B` before the roll. Dynamic, formula-driven,
   no data mod. Suppress = Brave 0; force = Brave 100.
2. **Data-disable** (fallback): strip the reaction in ENTD / JobCommand (the data mod). Global, simple,
   already-proven mechanism, but requires the data mod enabled and is not per-action.

## Harness (built, compiles, ready to deploy)

`Mod.cs`: `BraveOverride*` knobs mirroring `EvadeOverride*` — the unit poller writes `+0x2A` (MaxBrave),
`+0x2B` (Brave), `+0x2C` (MaxFaith), `+0x2D` (Faith) on each unit's live struct every ~20 ms, plus the
array-span sweep (`BraveOverrideSweepSlots`) so even an untracked defender is covered.
`ApplyBraveOverrideIfEnabled` + `ApplyBraveOverrideSweep`; logs `[BRAVE-OVERRIDE …]`.

## Chicken threshold — v1 test (Brave=0) chickened everyone; mechanism CONFIRMED, value fixed

Live v1 (`2B=0` broadcast): **every unit turned into a chicken on its turn** and the player could not
issue commands. This is itself proof that **the engine reads our live Brave write** (the chicken/panic
state is purely Brave-driven) — but `Brave=0` trips a SEPARATE low-Brave check, distinct from the
reaction roll.

RE of that check (`work/disasm_chicken_threshold.py`): at **`0x30A9BD`** (combat-resolution region,
next to apply `0x30A66F` / reaction cluster `0x30BExx`):

```
0x30A9BD: cmp byte [unit+0x2b], 0x0A   ; Brave vs 10
0x30A9C1: jae  normal                  ; Brave >= 10 -> normal
0x30A9C3: or byte [r8+0x1f], 4         ; Brave < 10  -> set chicken/panic flag
```

⇒ **Brave 0–9 = chicken; Brave ≥ 10 = safe.** (Matches FFT canon: panic below 10, recovers at 10.)
**The real mod must never write Brave < 10** unless it intends to chicken the unit.

So the suppress test floor is **Brave = 10**: no chicken, and reaction trigger drops to ~10% (`roll(100,
10)`) — i.e. the reaction almost never fires. (Reaction% is tied to the same `+0x2B` byte the chicken
check reads, so ~10% is the lowest reaction rate reachable without chickening; that is fine for a clear
suppress signal.)

## The live test (confirmatory) — v2

Profile: `work/battle-runtime-settings.brave-reaction-test.json` (Brave forced to **10** on all units;
chicken-safe).

Setup: a defender that has a Brave%-gated reaction (Counter, **Blade Grasp**, Hamedo, Arrow Guard…).
Attack it physically 5–6 times.

- **PASS (suppress confirmed):** at Brave 10 the reaction fires almost never (vs normally often) and the
  hits land; log shows `[BRAVE-OVERRIDE … set(2B=0A)]`, no chicken.
- **Crisp second confirmation (force):** flip the profile to `2B = 100` → the reaction should fire on
  EVERY hit (no chicken, since 100 ≥ 10). The 10→100 contrast proves Brave drives the reaction.

## Bottom line for the mod

Unified input-control architecture, all via live struct writes the engine renders naturally:

- **hit / miss / block / parry** → defender evade bytes `+0x46/+0x47/+0x4A/+0x4B/+0x4E` (PROVEN).
- **reactions** → defender Brave `+0x2B` (offline-proven; live-confirm pending).
- **damage value** → pre-clamp `0x30A66F` debit (PROVEN).

All formula-driven (attacker + defender + equipment), no data-gutting, no result-forging.
