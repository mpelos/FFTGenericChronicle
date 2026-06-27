# Input-control hook map — offline RE (2026-06-26)

Status: **offline static RE complete; evade input-control LIVE-TESTED 2026-06-27 → REFUTED.**

> ## ⚠️ LIVE RESULT (2026-06-27): evade input-control is INFEASIBLE — static RE mis-identified the register
> The evade hook at `0x30F49C` was built into the harness and tested live (profile
> `work/battle-runtime-settings.evade-input-test.json`; log
> `work/battleprobe_log.evade-input-FAILED.*.txt`). The byte-write provably works
> (`[EVADE-INPUT event=47 ... id=0x01 before 4B=00 -> after 4B=64] [CONTROL WROTE]`), **but `rbx` at
> `0x30F49C` is the ACTING unit (attacker), NOT the defender** — the opposite of what the two static
> RE passes concluded. Proof: when Agrias (0x1E) attacked Ramza (0x01) for 184, the hook fired with
> `rbx=Agrias` (event 1); it only fired with `rbx=Ramza` when Ramza took his OWN turn (event 47,
> immediately followed by `[CT-DROP id=0x01]`). The producer `0x30F0C4` walks units and reaches
> `0x30F49C` once per ACTING unit; the defender/target and its evade are resolved/read INSIDE the VM
> roll `0x30FA34`. **There is no real-code point that exposes the defender's evade before the roll**
> (consistent with the static finding that no real-code site reads the defender's evade pre-roll).
> ⇒ **Evade input-control (Layer C) is not viable. Neutralize Layer C in DATA** (zero evade bytes /
> `Evadeable` off) and author outcomes with the PROVEN output-control hooks. The `0x30F49C` hook code
> remains in the harness (disabled) but writes the attacker's bytes, so it is not useful for defender
> control. (Reaction input-control via Brave at `0x271D20` is untested and now suspect — its register
> role needs the same live check; data-disable of reactions is simpler and likely the answer.)

Original (now-CORRECTED) offline conclusion follows; the "rbx = target" claims below are REFUTED.

Method: 3 parallel static-RE passes over `FFT_enhanced.exe` (capstone + pefile, image base
0x140000000; real code RVA < ~0x610000, Denuvo VM = E9/E8 thunks into `.edata` > ~0x610000). Frentes 1
(resolution spine) and 2 (evade input) independently converged on the same pre-roll site (`0x30F49C`).

---

## The hook map

| Layer | Input-control hook (RVA) | Register at the hook | Expected bytes | Confidence |
|---|---|---|---|---|
| **Evade (Layer C)** | **`0x30F49C`** — last real instr before the VM roll `0x30FA34` | `rdx=rbx=TARGET` record (array base 0x1853CE0, stride 0x200); attacker NOT in a reg | `48 8B D3 88 4B 41 E8 8D 05 00` (`mov rdx,rbx; mov [rbx+0x41],cl; call 0x30FA34`) | needs-1-live-test |
| Evade — earlier entry | `0x30F442` (where `rbx` becomes the target) | `rbx`=target after `movsxd rbx,edi; shl rbx,9; add rbx,rdx` | `48 63 DF 48 C1 E3 09 48 03 DA ...` | needs-1-live-test |
| **Reactions (Layers A/B)** | **`0x271D20`** (fn; defender-facing Brave% roll in the result/anim tree) — or a guard right before any Brave-gated `call 0x278EE0` | `rcx=[actor+0x148]=DEFENDER`; Brave read at `0x271D4A` (`0F B6 57 2B`) | `48 89 5C 24 10 48 89 6C 24 18 48 89 74 24 20 57` | needs-1-live-test |
| Resolution entry (earliest pre-VM) | `0x38A6D3` (dispatcher's `call 0x30F0C4`) | none — attacker/defender resolved INSIDE the producer | `E8 EC 49 F8 FF` | static-proven |

---

## The resolution spine (confirm → damage), real vs VM

Driven by a two-layer state machine: outer phase-tick → category PRODUCER `0x30F0C4` (switch on global
phase dword `[0x186B044]`) → result DISPATCHER `0x38A4FC` (decision `0x38A6F1 cmp edx,0x300`) → APPLY
`0x30A51C` → pre-clamp `0x30A66F`.

```
PRODUCER 0x30F0C4 case-1 "resolve attack":
  0x30F3DD..0x30F442  target-select loop (21 records, stride 0x200) -> rbx = DEFENDER       [REAL]
  0x30F49C            mov rdx,rbx ; mov [rbx+0x41],cl   (rand%100 scratch)                  [REAL]
  0x30F4A2            call 0x30FA34  = accuracy + evade roll + evade-source combine          [VM]  (E9 -> 0x150CFB562)
                      + the evade-type +0x1C0 write all happen INSIDE this one VM call
  0x30F4AF            test eax,eax ; je (miss)                                               [REAL]
  0x30F4B6            or r12d,0x300   (stage "apply"+damage ONLY on a hit)                   [REAL]
DISPATCHER 0x38A6F1  cmp edx,0x300 -> 0x38A6F9 call 0x30A51C (APPLY)                          [REAL]
APPLY 0x30A51C       newHP = clamp(HP + word[+0x1C6] - word[+0x1C4]) ; hook 0x30A66F          [REAL]
```

- **Reaction vs evade ordering is INSIDE the VM** (`0x30FA34`) → cannot be byte-proven statically
  (consistent with canon: reactions roll before the evade bytes).
- The result/animation SELECTOR `0x205210` is on a SEPARATE (forecast/UI) tree (callers 0x26A683 /
  0x26A7B1 / 0x26A92F), not the dispatcher — reconfirms debit and animation are independent paths.
- **Attacker is never in a register** on the resolution path (target-centric producer; `r13`=image
  base, `r11`=array walker). Resolve the attacker via the actor array (`actor+0x148`) exactly as the
  pre-clamp hook already does.

---

## Layer C (evade) — input-control detail

Real code hands the VM only a TARGET pointer (`rdx` at `0x30F4A2`); the VM reads the evade bytes
(`+0x46/+0x47` weapon, `+0x4A`/`+0x4E` shield, `+0x4B` class) off that pointer internally. So writing
those bytes on the target at `0x30F49C` (or anytime from `0x30F442`) is the correct "deterministic
evade-byte" lever (Option 2). **Confidence needs-1-live-test**: because the read is inside the VM, we
can't statically rule out that it consumes a copy snapshotted earlier in the frame — but all static
evidence favors a live deref (the only argument is the live `rdx` pointer; no real-code pre-copy of
evade exists on this path; the analogous "write struct bytes just before the engine reads them" is
already live-proven for HP via the pre-clamp).

Live test: at `0x30F49C` force target `+0x4B=100`, others `0` → expect class-evade ("Miss",
`+0x1C0=0x04`); force all five `0` → expect a guaranteed hit.

---

## Layers A/B (reactions) — input-control detail

- **No reaction-slot byte in the per-unit battle struct.** A 5-unit dump layout shows the
  equipment/evade fields align perfectly but there is NO clean Reaction/Support/Movement id triad.
  Ability data is fetched through the VM resolver `0x2BB0D4` from the character/skillset object behind
  one of the `+0x70`/`+0x78`/`+0x83` pointers — not a flat struct slot. ⇒ you cannot "blank the
  reaction slot" by a struct write; strip reactions in DATA (ENTD / JobCommand R/S/M) instead.
- **But reactions are NOT fully virtualized.** Brave (`+0x2B`, byte; MaxBrave `+0x2A`, Faith `+0x2D`,
  MaxFaith `+0x2C`) is read in REAL code at 6 Brave%-gated roll sites that funnel through VM primitive
  `0x278EE0`. The Brave value, the `100−Brave` threshold, and the success/fail BRANCH are all REAL;
  only the roll arithmetic is VM. Best reaction-roll candidate: fn `0x271D20` (defender from
  `[actor+0x148]` in `rcx`, facing-modified Brave%, in the result/animation tree).
- ⇒ **Input-control of reactions is feasible** by forcing the defender's Brave (or the derived
  threshold) just before the roll so the native trigger cannot fire. The suppress direction (Brave 0
  vs 100) is set by the comparison and is confirmed in the same live test.

Live test: at `0x271D20` force defender Brave → expect Blade Grasp / Hamedo to NOT trigger on an attack
that normally would.

---

## Corrections to prior notes (important)

1. **`0x226F39` (and `0x226EBC`) is NOT the per-attack evade read.** It is a UI status-panel exporter:
   bulk-copies ~40 fields of a cursor-selected unit (`rbx = unitArray[word[0x7DCF9A]]`) into scratch
   `0x7832E6+`, which has ZERO real-code readers. Writing evade bytes there does nothing to an attack.
   The correct evade-input site is `0x30F49C`. (Fixes the Option-2 anchor in the definitive doc and any
   memory note claiming a per-attack re-read at 0x226EBC/0x226F39.)
2. **Brave offset is `+0x2B`** (live-confirmed: Ramza `+0x2A..+0x2D = 61 61 46 46`). The doc's
   `min_brave_faith` AOB `41 0F B6 5A 2B` resolves into `.edata` (VM), not `.text` — that line is wrong;
   the real-code Brave readers are the 6 sites funneling into `0x278EE0`.

---

## Confidence ledger

- Resolution spine (producer→VM roll→stage→apply), real-vs-VM tags, dispatcher decode: **static-proven.**
- `0x30F49C` is the last real instr before the single VM avoidance roll, target in `rdx=rbx`: **static-proven.**
- Writing target evade bytes at `0x30F49C` achieves input-control: **needs-1-live-test** (VM-read boundary).
- No struct reaction-slot; reactions via VM resolver `0x2BB0D4`: **strong** (layout + absence in all real resolution code).
- Brave `+0x2B` read in real code at the 6 roll sites; threshold/branch REAL: **static-proven.**
- `0x271D20` is specifically the reaction-avoidance roll + forcing Brave suppresses Blade Grasp/Hamedo:
  **needs-1-live-test** (the VM record naming the outcome can't be read statically).
- Attacker absent from registers on the resolution path (resolve via actor array): **static-proven.**

See `1782517714-miss-block-parry-control-definitive.md` (output-control, PROVEN) and
`docs/modding/05-reverse-engineering.md` §4.
