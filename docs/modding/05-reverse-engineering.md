# Reverse-engineering reference — hooking the IVC combat engine

This document defines the static reverse-engineering model for Final Fantasy Tactics: The
Ivalice Chronicles (IVC): which parts of the combat engine are native and hookable, which
parts are virtualized, and which stable native surfaces expose combat inputs and results.

Live unit offsets and action ownership are owned by `04-engine-memory-model.md`. Runtime
settings and implementation contracts are owned by `06-code-mod-runtime-dsl.md`. Formula-ID
behavior is owned by `02-formula-id-catalog.md`.

## 1. Architectural truth

IVC is `FFT_enhanced.exe`, an x86-64 reimplementation built on the Faith engine. It preserves
the classic ability-action data model and a battle-unit layout compatible with the known FFT
model, but it does not contain the original PSX or PSP machine code.

Classic/WotL formulas and structures are useful conceptual maps. Classic MIPS addresses,
patches, signatures, and binaries do not port to IVC.

`FFT_classic.exe` is a sibling Faith-engine executable, not a PSX emulator. Enhanced is the
primary mapped target. Both executables are Denuvo-protected.

## 2. Denuvo boundary

**Proven:** the central damage and accuracy arithmetic runs through Denuvo-virtualized code.
The transient native-looking damage-apply sequence relocates between launches and is absent
from readable executable regions as a stable signature. It cannot be treated as an AOB-hook
target.

The practical boundary is:

- formula arithmetic and several combat rolls execute inside the virtual machine;
- combat data, action records, staged results, dispatcher code, result selection, and HP/MP
  application remain ordinary native memory or native code;
- custom mechanics therefore author inputs before the virtualized calculation or reconcile
  staged outputs on stable native paths.

Hardware breakpoints can observe VM-mediated writes, but registers at the VM dispatcher do not
provide a stable gameplay ABI. They are a diagnostic fallback, not a shipping integration.

## 3. Stable hookable anchors

The following RVAs are relative to image base `0x140000000` in the mapped Steam Enhanced build.
They are native `.text` or `.xcode` sites. A game update can move them; expected bytes and nearby
signatures must validate before a hook is installed.

### 3.1 Native `.text` anchors

```text
battle unit read       0x226D20   movzx eax, word [rcx+0x30]
HP-bar display math    0x30A5ED   rdi = unit; edx = MaxHP; eax = MaxHP-currentHP
JP arithmetic          0x2836DC
EXP arithmetic         0x2836EF
Move read              0x3601E7   movzx eax, byte [rdi+0x42]
action calculation     0x3099AC   rcx = order record; dl = target index
```

Relocation signatures:

```text
battle unit read       0F B7 41 30 66 89 42 0C
HP-bar display math    2B C8 8D 04 11
JP arithmetic          03 C2 8B CF 41 3B C0
EXP arithmetic         0F B7 84 7B 1E 01 00 00
Brave read             41 0F B6 5A 2B
Move read              0F B6 47 42 66 89 43 30
```

The community pattern beginning `0F B7 47 30 2B C2` is not a stable damage-store anchor.

### 3.2 Result dispatch and application

```text
battle-unit/result array  0x1853CE0   stride 0x200; at most 21 entries
result dispatcher          0x38A4FC    event queue; decision branch 0x38A6F1
HP/MP apply                0x30A484    clamp(HP + credit - debit, 0, MaxHP)
staged-debit read          0x30A5D7    movsx eax, word [rbp+6]
result selector            0x205210    r8 = actor; record = [r8+0x148]; cl = result kind
result-kind teardown       0x205B38    mov byte [rdi+0x1C0], r12b
popup renderer             0x266AE0    integer-to-glyph path
```

The dispatcher category encoded in `edx` includes:

```text
0x0300  apply HP/MP/stat
0x0200  status
0x0100  turn done
0xFF00  terminator
0xE000  initialization
```

The HP apply path reads `unit+0x1C4` as debit and `unit+0x1C6` as credit; it does not consult a
separate hit flag. The staged-debit hook is therefore the native result-authority surface for
damage amounts and forced zero-damage outcomes.

### 3.3 Ability-action table read

The `OverrideAbilityActionData` read site is at RVA `0xEEA6E50`. Non-negative override columns
are cast to bytes and merged into the in-memory action definition before combat calculation.
This is the natural data-layer entry for `Flags12`, `Flags34`, `Range`, `EffectArea`, `Vertical`,
`Element`, `Formula`, `X`, `Y`, `InflictStatus`, `CT`, and `MPCost`.

## 4. Result-kind and avoidance control

### 4.1 Result-kind enum

The byte at result record `+0x1C0`, also passed in `cl` to selector `0x205210`, selects the
rendered outcome.

```text
0x00  hit
0x01  accessory evade
0x02  weapon parry
0x03  shield block
0x04  class evade / “Miss”
0x06  plain failed-accuracy miss
0x0B  Blade Grasp / Shirahadori
```

**Proven:** hit records carry a staged debit and apply damage. Avoidance records carry zero debit
and differ primarily by result kind and presentation fields. Values `0x05` and `0x07..0x0A` are
unclassified.

### 4.2 Authored binary outcome

Render selection and HP application are independent native paths. A coherent authored outcome
must control both:

```text
hit(D):   staged debit = D; staged credit = 0; result kind = 0x00
miss(k):  staged debit = 0; staged credit = 0; result kind = k; result-present flag cleared
```

**Proven:** writing only the result kind changes presentation but does not cancel an already staged
debit. A forced miss must also zero `unit+0x1C4` at the pre-clamp path.

**Proven:** a native miss does not stage damage or emit the same apply work as a hit. The robust
policy is to neutralize native avoidance, let the engine produce a hit candidate, and downgrade
that candidate when the mod's roll misses. Promoting an already-native miss at the dispatcher is
not supported.

The runtime decision cache, selector coordination, and reaction suppression are defined in
`06-code-mod-runtime-dsl.md`.

### 4.3 Native avoidance inputs

Physical avoidance has three semantic layers:

- pre-emptive reactions such as Hamedo;
- reaction avoidance such as Blade Grasp, Arrow Guard, Catch, and Reflect;
- equipment/class evasion.

**Proven:** class evade at unit `+0x4B` is a live input read by native calculation. Equipment evade
is not reliably neutralized by late writes to the unit's derived evade bytes.

**Proven:** equipment evade originates in writable item-stat tables:

```text
weapon     0x14080F690   stride 8   +4 WP, +5 physical evade   128 rows
shield     0x14080FA90   stride 2   +0 physical, +1 magical     16 rows
armor      0x14080FAB0   stride 2   +0 HP, +1 MP                64 rows
accessory  0x14080FB30   stride 2   +0 physical, +1 magical     32 rows
```

The item lookup uses `additional_data_id` to index the stat sub-table. Zeroing the relevant
equipment-evade columns at their source removes equipment avoidance from native preview and
execution. `item_catalog.csv` owns the data mapping.

**Refuted:** the apparent copier at `0x59F550` is combat-related. It belongs to HID gamepad
enumeration. Derived shield values in intermediate records are also not a sufficient universal
runtime lever.

**Proven:** Brave at unit `+0x2B` controls Brave-gated reactions. Values below 10 cross the native
Chicken threshold and must not be used as a neutral reaction-suppression value. Reaction-specific
runtime policy is owned by `06-code-mod-runtime-dsl.md`.

### 4.4 Arbitrary hit percentage

`unit+0x1EA` is a forecast display source, not the authoritative execution chance. Several accuracy
values are recomputed inside the virtual machine immediately before the native RNG trampoline.

**Refuted:** `0x30F49C` exposes the defender or final avoidance verdict. The register identifies the
attacker. **Refuted:** `0x30F4A7` is a general hit-verdict surface; it belongs to CT/turn evaluation.

The mod computes its own percentage and random decision, then authors the binary result through the
native surfaces above. Forecast display uses the same cached percentage; execution uses the cached
decision.

## 5. Formula fingerprints

Known FFT formula constants remain useful for locating or classifying decompiled fragments even
when the final arithmetic is virtualized:

```text
zodiac multipliers   0.50, 0.75, 1.00, 1.25, 1.50
Faith normalization  /100 and paired caster/target Faith terms
Brave reactions      roll against Brave
physical scaling     PA, WP, level, and weapon-family modifiers
```

Fingerprint matches are clues, not hookability proof. A candidate becomes an integration surface
only after its native bytes, arguments, and lifecycle validate.

## 6. Battle-unit layout ownership

The battle-unit array is native memory with `0x200`-byte records. Core examples include HP at
`+0x30`, MP at `+0x34`, Brave at `+0x2B`, Faith at `+0x2D`, PA at `+0x3E`, MA at `+0x3F`, Speed at
`+0x40`, CT at `+0x41`, Move at `+0x42`, and position/facing at `+0x4F..+0x51`.

The complete authoritative offset map, confidence tags, actor resolution, target records, status
arrays, turn ownership, equipment identity, and movement state are defined in
`04-engine-memory-model.md`.

## 7. Modding API feasibility

**Proven:** the public table managers patch Nex/NXD data and do not expose a damage-calculation event
or algorithm hook. Faith Framework provides runtime data editing and debug UI, not a combat hook
registration API.

Data patches can select existing `Formula/X/Y/Element` shapes through
`OverrideAbilityActionData`. Arbitrary DCL math requires the Reloaded-II code mod because the
existing catalog cannot express the new algorithm.

The implementation uses native action/result anchors plus a managed formula engine; it does not
attempt to replace the virtualized dispatcher. The concrete runtime contract is defined in
`06-code-mod-runtime-dsl.md`.

### 7.1 Vanilla baseline data

`OverrideAbilityActionData` is a sparse override layer, not a resolved dump of stock action rows.
WotL action data is a useful design baseline because the field model is shared, but IVC values must
be extracted or observed before they are treated as authoritative.

## 8. Stable-touchpoint register classification

At the native battle-unit read site, `rcx` is the touched unit. Other register values can be
classified against the known unit array and readable memory to locate surrounding controller or
action objects.

```text
unit:touched  register equals the unit that triggered the hook
unit:id=N     register equals another known battle unit
readable      pointer targets readable memory but is not classified
unreadable    pointer is not readable according to VirtualQuery
zero          literal zero
```

This technique is a discovery aid. A UI/stat read does not itself prove action ownership. Native
actor context, pending action context, selector context, and final apply targets are the accepted
ownership surfaces; see `04-engine-memory-model.md`.

## 9. CT evidence

`unit+0x41` is CT. It can help correlate immediate actions in diagnostics, but it is **Refuted** as
an ownership authority: Wait changes CT without damage, delayed actions resolve after the caster's
reset, and reactions or passive effects do not require a clean CT transition.

CT must not drive production attribution or DCL mechanics.

## 10. Forecast hit-percentage display

The displayed hit percentage is materialized in native memory at RVA `0x7832C0`. The canonical
copy path is:

```text
0x227FEA  load forecast-object pointer
0x227FFA  load word [object+0x2C]
0x227FFE  mov r10d, 2
0x228004  store AX to [0x7832C0]
```

**Proven:** hooking `0x227FFE` and replacing `AX` before the native store controls the rendered
percentage deterministically. External writes after the panel is drawn do not update the retained
UI until redraw, and redraw recomputes the source; polling the buffer is therefore not a robust
presentation mechanism.

**Proven:** this buffer is visual only. Execution accuracy is independent. DCL forecast control
uses the percentage from the same per-target decision cache that execution consumes.

## 11. Forecast HP amount and applied result

The forecast object is `target unit + 0x1BE`.

```text
object+0x06 == unit+0x1C4   staged HP debit / damage
object+0x08 == unit+0x1C6   staged HP credit / healing
object+0x2C == unit+0x1EA   forecast hit percentage source
```

The staged debit drives the forecast damage number, ghost HP-bar depletion, and later HP apply.
The staged credit drives healing forecast and HP apply. The native apply formula is:

```text
newHP = clamp(currentHP + credit - debit, 0, MaxHP)
```

The forecast UI is retained-mode. Values must be authored at calculation/finalization time for a
correct first render. A later write becomes visible only after the preview is rebuilt.

The universal result surface is the staged debit/credit read at `0x30A5D7`. Formula-specific
forecast writers include:

```text
0x30637E  mov [r8+6], dx     observed magic debit writer
0x308D8F  mov [rcx+6], ax    physical candidate
0x307DC4  mov [r10+6], dx    additional action family
0x309664  mov [r9+6], cx     additional action family
```

The display-number buffer at RVA `0x7832BE` affects only the printed number; it does not control
the ghost bar. A coherent preview must author the underlying staged field, not only paint the
formatted number.

## 12. Calculation entry and RNG boundary

**Proven:** native function `0x3099AC` is the calculation entry for a specific action and target.
`rcx` points to the order record at caster `+0x1A0`; `dl` is the target index. It is used by player
preview, execution, charged-action recalculation, and AI candidate evaluation.

**Proven:** the shared RNG head at `0x278EE0` is a Denuvo trampoline. Accuracy and status rolls may
arrive from VM-internal callers, so native caller hooks do not provide complete per-action roll
control. Brave-gated reaction sites can remain native even when the originating ability calculation
is virtualized.

The stable doctrine is data-first input control plus staged-output reconciliation. Native hooks
must fail closed when expected bytes or contextual invariants do not match.

## Sources

- Talcall, `FFT-1997-Decomp` — conceptual classic-engine function and structure map
- Nenkai, Faith Framework — Nex/NXD runtime and engine framework
- Reloaded-II and Reloaded.Hooks documentation — native hook infrastructure
- FFHacktics WotL action data — design baseline, not an IVC resolved-value authority
