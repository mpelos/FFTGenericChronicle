# LT12 — formula-driven forecast hit-% live result

## Result

PASS. `DclPreviewHitPctEnabled` displays the percentage authored by the same per-target DCL hit
decision used by execution.

## Live scenario

- Profile: `work/1783941290-battle-runtime-settings.lt12-dcl-preview-hitpct.json`
- Save: Manual Saves, first entry, 05
- Attacker: Ramza (`charId=0x01`, unit index 16)
- Target: Janus Chocobo (`charId=0x82`, target index 1)
- Action: basic Attack (`ability=0`, `type=0x01`)
- DCL formula percentage: 50
- Deterministic roll: 90
- Natural forecast payload retained: 532 damage, KO, Counter

The forecast panel rendered **50%**. Execution missed, Janus remained at 363/363 HP, and no Counter
occurred.

## Runtime chain

The raw log is `work/1783942565-lt12-dcl-preview-hitpct-live.log`.

```text
[PREVIEW-HITPCT-HOOK] ... forced=any logOnly=0 dcl=1
[DCL-HIT] caster=0x01 target=0x82 ability=0 type=0x01 pct=50 roll=90 outcome=miss cached=0 ...
[DCL-REACTION-GATE] targetIdx=1 casterIdx=16 ability=0 type=0x01 chance=69->0 decision=miss
[DCL] caster=0x01 target=0x82 abilityId=0 ... debit=0 oldDebit=532 outcome=forced-miss
[DCL-SELECTOR] targetIdx=1 casterIdx=16 ability=0 type=0x01 naturalKind=0x00->0x06 resultCode=0x01->0x00 decision=miss
```

The target record logged `hp=363`, the staged debit was rewritten from 532 to 0, and the selector
completed through the miss branch. This promotes formula-driven forecast percentage integration
from Strong offline to Proven live.

## Operational note

Foreground rocks can visually cover a target tile. Clicking the visible unit still resolves its
identity and target pane, but confirmation can correctly fail if it is out of range. Reset Move and
an unobstructed adjacent tile provide the fastest recovery.
