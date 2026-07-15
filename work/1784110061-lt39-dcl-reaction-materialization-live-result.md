# LT39 Reaction materialization live result

## Objective

Validate the accepted-only Reaction order boundary at RVA `0x2063BD` with the observe-only
materialization probe, then correlate one native Counter through pass-2 commit and state-`0x2C`
delivery.

## Fixture and route

- Runtime profile: `work/1784108723-battle-runtime-settings.lt39-dcl-reaction-materialization.json`
- Autosave fixture: `work/1784104894-fft-autoenhanced-snapshot.png`
- Captured log: `work/1784109869-lt39-dcl-reaction-materialization-live.log`
- Fast load: Enhanced click, 4.2-second wait, `Enter`, 1.6-second wait, direct Continue click, all in
  one input burst.
- Rion began Invisible. Repeated Wait turns did not make enemies target him. Rion advanced to the
  enemy cluster, then used an exact basic Attack on Herkyna. Herkyna's native Counter supplied the
  bounded carrier event.

## Evidence

The three guarded hooks installed without a failure or skip:

- materialization `0x2063BD`
- pass-2 commit `0x206421`
- effect `0x212C2E`

The accepted order row was:

```text
[DCL-REACTION-MATERIALIZED] event=1 reactorIdx=3 sourceIdx=16 reactionId=442 unit=0x1418542E0 order=0x141854480 casterIdx=3 actionType=1 actionId=0 itemId=0 targetMode=5 targetIdx=16 target=(10,0,11) raw=0301000000000000000005100A0000000B000000
```

The same Reaction/source then produced one pass-2 commit through actor reactor index `1` and one
state-`0x2C` effect with executable action `0` delivered to target `[16]`.

The selected unit-table index/caster index (`3`) and the actor execution index (`1`) are distinct
index namespaces. The live analyzer was extended with `--actor-reactor` so it does not falsely
require equality across the actor-construction boundary.

Validation command:

```powershell
python tools\analyze_dcl_reaction_materialization_live.py work\1784109869-lt39-dcl-reaction-materialization-live.log --reaction-id 442 --reactor 3 --actor-reactor 1 --source 16 --expected-action-type 1 --expected-action-id 0 --expected-target 16 --expected-materialized-count 1 --expected-effect-count 1
```

All thirteen checks pass. This proves live that Counter is completely materialized before actor
construction as type `1` / payload `0`, with the incoming source unit index and source coordinates
already present in the 20-byte order.

## Operational result

The game and Reloaded-II were closed. The installed DLL, PDB, runtime settings, Reloaded AppConfig,
game-side probe log, and Enhanced autosave were restored byte-for-byte from their pre-LT39 backups.
All six SHA-256 comparisons matched; the restored autosave hash is
`73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`.

The shortest repeated route is now tiered:

1. same-process **Game Over > Retry > Retry from Start of Battle** when the current injected runtime
   and cumulative log are acceptable;
2. restored autosave plus atomic **Enhanced > skip > Continue** for a clean process;
3. atomic **Enhanced > skip > Load Game**, Manual Saves, first Save 05 card only when establishing a
   new fixture.
