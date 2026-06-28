# T-B log analysis → the SELECTOR is the output lever for result-TYPE (2026-06-27)

Status: interim finding from the T-B run log. Confirmation pending **test-alpha** (output-paint-miss).
Commit + docs/modding HELD until test-alpha lands.

## What the T-B log proved
Profile T-B (force-hit + Brave 97 so Blade Grasp fires + pre-clamp ForcedDebit=50 on Ramza) produced
ONE clean selector event on a Blade Grasp:

```
[SELECTOR-PROBE event=1 evadeType=0x0B(unknown) ... record=0x141855CE0 unit:id=0x01/hp=567
   rec+1BB=01 rec+1BE=00 rec+1C0=0B rec+1C4(dmg)=0 rec+1E5=00]
```

- **Blade Grasp = evade-type 0x0B.** It STAGES a result record, but with `+0x1C4(dmg)=0`, `+0x1E5(apply)=0`,
  `+0x1BE(result-present)=0`, `+0x1C0(type)=0x0B`.
- **The pre-clamp (0x30A66F) never fired** on it (no `[PRECLAMP-REWRITE]` write line). Because 0x30A66F is
  the HP-APPLY hook — it runs only when there is an apply event. Blade Grasp sets apply=0 → no apply → the
  pre-clamp is downstream of the gate and sees nothing. This is WHY output (pre-clamp) "couldn't override
  the reaction": we were hooking after the gate the reaction closes.

## The real picture (corrects the earlier model)
There are TWO output surfaces, at different stages:
1. **Selector `0x205210`** — runs at RESULT-TYPE STAGING, BEFORE the apply gate. Sees the per-attack record
   for EVERY outcome (hit / miss / block / parry / reaction), each with:
   - `+0x1C0` evade/animation type (0x00 hit?, 0x01 cloak, 0x06 plain-miss, 0x0B Blade Grasp, …)
   - `+0x1BE` result-present flag (0 = no-damage/evade)
   - `+0x1C4` staged damage
   - `+0x1E5` apply flag
2. **Pre-clamp `0x30A66F`** — runs at HP-APPLY, AFTER the gate. Only sees connecting hits. Good for damage
   magnitude; blind to avoidances.

## Architecture conclusion (the /loop answer taking shape)
We do NOT need output to do the impossible direction (miss→hit / un-negate a reaction); a native avoidance
stages no apply event, so there is nothing to repaint into a hit. Instead:

- **INPUT FLOOR (always-connect):** zero target evade (force-hit) + suppress negating reactions (Brave
  +0x2B below trigger, ≥10 to dodge the chicken floor). Guarantees the engine stages a CONNECTING hit with
  computed damage + apply=1 → a paintable result ALWAYS exists. This is the minimal, designed use of input.
- **OUTPUT PAINT (any outcome):** at the selector, rewrite `+0x1C0`/`+0x1BE` to paint the result TYPE
  (hit ↔ miss/block/parry/dodge animation); at the pre-clamp, rewrite damage magnitude / heal / (status via
  StatusOverride). Because a hit is guaranteed by the floor, painting a "miss" is now trivial output.

So "control miss/block/parry" = **force-hit + paint-the-type-down**, all via memory. The controllable output
direction is HIT→MISS (downgrade); the MISS→HIT direction is handled by the floor, never by output.

## test-alpha (deployed) — proves the controllable direction
`work/battle-runtime-settings.output-paint-miss-test.json`: force-hit (evade 0 broadcast) + Brave 10
(broadcast, BG→10%) + selector control on Ramza (id 1): ForceEvadeType=0x06, ForceResultCode=0, no
pre-clamp. Attack Ramza ×5-6. PASS = Ramza dodges (miss) + takes 0 on a guaranteed hit → output fully
controls the result type. If dodge-animation-but-damage → extend selector control to also zero +0x1C4/+0x1E5.

## Selector anatomy (offline disasm of 0x205210 full body) — predicts test-alpha = PASS
`work/disasm_evadetype.out.txt`. The selector `0x205210(cl=evade-type, rdx, r8=actor, r9=out-record)`
builds the result descriptor in r9/rbx ([rbx]=result-category, [rbx+8]=subcode, [rbx+0x30]=damage). The
pivotal branch:

```
0x205279: cmp byte [rdi+0x1BE], 0   ; rdi = target record [r8+0x148]
0x205280: je   0x2053FA             ; +0x1BE==0 -> EVADE path (never reads +0x1C4 damage)
                                    ; else      -> DAMAGE path (0x2052BA reads word +0x1C4 -> [rbx+0x30])
```

So **+0x1BE is the damage-vs-evade switch**, read straight from the record. test-alpha forces +0x1BE=0
(ForceResultCode=0) AND evade-type +0x1C0/esi=6 (ForceEvadeType=6). With +0x1BE=0 the selector takes the
EVADE path and the +0x1C4 damage read is bypassed entirely → predicted result: **clean miss, 0 damage.**
The dispatcher at 0x1FAB3F (`movzx [+0x1C0]; cmp 6; je miss-handler`) confirms type 6 = plain miss.

Evade-path type->category map (esi switch at 0x2053FA; [rbx] code): type 0->0x12, type 4->0x13,
type 6->0x12, type 1->0x13, type 0xB(Blade Grasp)->0x13-ish. Damage-path categories: 0x14 (normal),
0x17, 0x16, 0x0F, 9. Exact block-vs-parry labels need an empirical PALETTE SWEEP (paint +0x1C0 = 0x00..0x0D
across attacks, user reports animation) — queued as the test AFTER test-alpha confirms paint works.

Related: [[output-control-first]], [[miss-block-parry-control]], [[dcl-combat-gaps]].
