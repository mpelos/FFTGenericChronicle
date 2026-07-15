# DCL current mechanism coverage

## Purpose

This is the current journey-side implementation inventory after the status, contest, channel,
Weapon Skill, physical, Weight, magic, facing, and reach passes. It supersedes the conclusions in
`work/1783202375-dcl-mechanism-barrier-inventory.md` where later probes have closed or changed a gate.
It does not replace the timeless owners under `docs/`.

## Coverage by DCL chapter

| DCL area | Current mechanism state | Remaining gate |
| --- | --- | --- |
| `01` attributes | Core stats, raw stats, Brave/Faith, Zodiac, statuses, abilities, coordinates/facing and equipment slots are formula variables. | Persistent Weight→Move write; any missing designed-content flags. |
| `02` physical damage | Same-hit HP delivery is live-proven; modernized raw input→base→DR→wound→Brave→Zodiac→scale passes offline. | Calibrate constants; current profile must pass LT14–LT17 on current build. |
| `03` damage type / armor | Cut/thrust/crush/missile, three native body classes, wound multipliers, missile divisors and Gun penetration are routed. | Author every weapon type/wmod and every armor DR row; validate full-plate matchups. |
| `04` hit / defense | 3d6 attack, crit/fumble, best-defense selection, defender-skill Parry, shield-derived Block, finite pools, own-turn refresh, cache delivery and preview percent exist. Multi-hit consumes independently. | Calibrate Parry/Block formulas and prove them through LT15 live. |
| `05` facing | Coordinates + calibrated compass enum now produce front/side/back; front full, flank `-2` fixture, back no roll. | Diagonal/ranged and multi-tile policy. |
| `06` reach | Native range-2 Pole/Polearm targeting exists; point-blank skill penalty is routed. | Prove counter escape; author stop-hit ability; validate height/bridge/multi-tile distance. |
| `07` Brave | Physical offense, shared active-defense modifier, and a hybrid Courage/Caution/Neutral reaction-chance surface are implemented offline. Four real-code gates receive exact Reaction ids; explicitly marked VM-internal avoidance reads the actual calc-entry order-record id and uses scoped Brave virtualization with guarded restoration, including innate/derived evaluations. All 32 native records and all 16 final-job roster entries have an explicit implementation classification. | Resolve the taxonomy rows marked Hypothesis, implement the final roster's trigger/effect/cadence gaps, run the reaction vertical slice, and author the status/composure rule set. |
| `08` Faith | Two centered permanent-Faith terms drive damage and healing; inverse-Faith status formulas are possible. | Resolve `0.60` floor in `08` versus `0.70` in `11/12`; author status rules. |
| `09` Zodiac | Ordinary twelve-sign grid drives physical/magic damage and physical hit; Serpentarius fallback neutral. | Designed-content Worst flag; decide status-contest use; final current-build live sample. |
| `10` Weapon Skill | Job Level/character level, grade growth, Sword Master candidate, cap/excess, both equipped hands, Crossbow/Gun routes implemented. | Full job×family matrix; author Sword Master; prove active strike hand for mixed Dual Wield. |
| `11` magic | Formula `0x08` damage, `0x0C` healing, spiritual Holy/Dark, affinity/Shell/Zodiac/Faith stack, absorb/null, Undead inversion, preview, Magic Evade, Rod/Staff bolt route, and formula-driven own-turn MP trickle are implemented offline. Native MaxMP is the per-battle budget. | Author special-formula exceptions; affinity bytes live; Rod/Staff range/elements; calibrate MP pools/trickle/costs; LT17/LT18/LT19. |
| `13` statuses / reactions | Canonical status arrays, authored 3d6 add/remove rules, target-turn durations and staged output control implemented. All 294 ability×status rows are explicitly routed; safe Undead writes are whitelisted, native revive and Bequeath/Crystal lifecycle are preserved, offensive KO has an engine-owned lethal-debit mechanism, and reaction taxonomy has a hybrid exact-id native-input mechanism. | Nine offensive-KO rows need per-ability data/formula authoring; full status nature/duration authoring, reskin/text/data, Invite/campaign behavior, final reaction trigger/effect/cadence mechanisms, and the live reaction gate remain. |
| `14` equipment | Full catalog variables, three armor classes, sparse Weight map, seven-slot sum, Move curve, Dodge curve, affinity/status metadata and shield/weapon evade inputs are available. All 261 item records have an explicit DCL route, family role, and completion gate in the sidecar manifest. | Fill the 238 Weight values and remaining per-SKU wmod/DR/parry/block/divisor/element data; resolve head/accessory identities; persistent Move; UI exposure of Weight/breakpoint. |
| `15` jobs | Job id/level/JP and ability slots are exposed; formula surfaces can consume authored job policy. | Actual job redesign/data authoring and complete validation gates; this is content work after mechanisms stabilize. |

## Ability catalog audit

The runtime catalog has `512` ids across `89` non-equivalent `formula_hex` groups; `144` rows have a
blank formula field in this extracted source, mainly item/throw/jump or records whose action math is
owned elsewhere. Therefore a blanket `formula == magic` policy cannot complete the DCL.

Safe initial routing:

- `0x08`: 39 canonical `MA*Y` damage records, including elemental spells, Holy/Dark and several
  monsters;
- `0x0C`: 6 canonical `MA*Y` heals;
- basic Attack id 0 becomes a magic bolt only when the equipped right weapon is Rod/Staff.

The complete row manifest is `work/1783987218-dcl-ability-classification.csv`; its report is
`work/1783987218-dcl-ability-classification.md`. Every formula and blank-formula record has an
explicit route. Special percentage damage, drains, Samurai/Geomancy/monster damage, hybrid/sword
skills, Raise/Arise, Chakra, and percentage heals remain authoring/reverse-engineering gates.
Preserving vanilla is the safe fallback until each authored rule exists. Bequeath Bacon formula
`0x57` is explicitly classified as a preserved native special after current-build static analysis.

## Status policy audit

The current expanded manifest is `work/1783987219-dcl-status-policy.csv`; its report is
`work/1783987219-dcl-status-policy.md`. It covers all 294 ability×status rows across all 30 observed
native status tokens. Of those rows, 195 use the existing status surface, 5 rows preserve native
lifecycle/special ownership, 9 offensive-KO rows have mechanism coverage but require data/formula authoring,
2 Invite rows require campaign behavior, 44
require numeric/special authoring, and 39 require an explicit status-nature decision.

Generic status rules permit only the proven-safe byte-0 Undead mask `0x10`. Formula-owned lethal
damage continues through pre-clamp so the engine owns KO. Raise/Arise/Revive/Squeal keep the native
revive flags and lifecycle tail; DCL amount authoring may replace only staged HP credit. Offensive KO
uses `DclInstantKoRule`: the data layer removes the native Dead rider, the 3d6 rule decides the result,
and success supplies lethal staged HP so native HP apply owns death. Crystal remains excluded from
generic and lethal-HP routes; Bequeath Bacon preserves formula `0x57` and the native bounded-level plus
caster-Crystal lifecycle. The corrected KO disassembly is
`work/1783985524-ko-lifecycle-disassembly-analysis.md`; the Bequeath/Crystal analysis is
`work/1783987217-bequeath-crystal-disassembly-analysis.md`.

## Reaction taxonomy mechanism

The mechanism checkpoint is `work/1783988755-dcl-reaction-taxonomy-offline.md`; current-build anchors
and the guarded VM scope are in `work/1783988678-dcl-reaction-scope-analysis.md`. The first
single-scope hypothesis was refuted: `computeActionResult` ends before the later real-code post-hit
reaction gates. The implemented hybrid uses exact evaluation ids at all four real-code rolls and
limits Brave virtualization to explicitly marked VM-internal avoidance reactions. Calc entry exposes
the actual evaluation id at `orderRecord+2`, so innate/derived reactions no longer need an
equipped-slot approximation. It builds and passes offline but remains behind one live vertical slice.

The complete classification is `work/1783989749-dcl-reaction-implementation-manifest.csv`, with the
review report at `work/1783989749-dcl-reaction-implementation-manifest.md`. It covers all 32 native
records and the 16 final-job roster entries. The chance input is structurally ready, but five final
reactions still lack their main runtime mechanism, five are partial, three remain design plus
mechanism gates, one is near-native, and two job slots are intentionally open.

## Item catalog audit

The complete row manifest is `work/1783984192-dcl-item-sidecar.csv`; its report is
`work/1783984192-dcl-item-sidecar.md`. It classifies all 261 records, including the item-0 unarmed
sentinel, 123 ordinary equipped weapon SKUs, 37 body armors, 29 headgear pieces, 16 shields, 33
accessories, 14 consumables, 6 thrown payloads, and 2 reserved records. Structural coverage is
complete; final numeric/content authoring remains explicit rather than inferred from vanilla values.

## Next offline priorities

1. Author the nine instant-KO ability routes and the exact 32-row reaction taxonomy, then classify drain,
   percentage/special, multi-hit, and campaign-side-effect families, keeping unsupported routes safe.
2. Resolve the head/accessory equipment identities, then calibrate and author the sidecar's numeric
   Weight/wmod/DR/parry/block/divisor/element fields with dominance checks.
3. Author the full job×weapon-family grade matrix and job ability policies, using the mechanism and
   catalog manifests as hard validation gates.

## Live queue remains bounded

Do not resume while the unmodified game still black-screens after Enhanced start. When it opens:

1. LT14 status add/remove and duration;
2. LT15 physical guard plus mixed-hand swap gate;
3. LT16 four HP/MP channels;
4. LT17 forecast amounts plus numeric magic routing;
5. LT18 Magic Evade;
6. LT19 own-turn MP trickle;
7. one-shot Move poke;
8. affinity-byte and diagonal-facing/reach matrix.
9. reaction taxonomy: Caution Shirahadori, Courage Counter, Neutral Mana Shield, miss composition,
   and Brave restoration.
