# DCL Fear autosave learned-byte correction

## Live falsifier

The corrected-plan-composition bundle loaded the prepared autosave and reached Josephine's first
turn. `Abilities > Mystic Arts` was present, but the game rejected it with:

> Unable to use. The unit either lacks any usable abilities or does not meet their requirements.

No combat action was executed, so this run produced no Fear-mechanism result.

The deployed fixture had written `0x80` to current-battle `unit+0x53`. That offset came from the
manual-save roster layout, where learned generic-job blocks begin at `unit+0x32` and Mystic's block
begins at `unit+0x53`.

## Layout correction

The autosave resume battle copies are not manual-save roster records. Their already observed live
behavior identifies `unit+0xC3` as a byte that controls which Mystic ability is exposed in this
battle-record layout:

- `unit+0xC3 = 0x01` exposes Empowerment, Mystic Arts position 2.
- `unit+0xC3 = 0x02` exposes Quiescence, Mystic Arts position 7.
- `unit+0xC3 = 0x80` exposes Umbra, Mystic Arts position 1.

The third result refutes the proposed LSB-first command-list bit order. Subsequent static comparison
found the manual learned block copied byte-for-byte from `unit+0x32` to battle `unit+0xA2`, and the
three live results prove MSB-first masks. The temporary installed command table put Umbra,
Quiescence, and Empowerment at positions 1, 7, and 8, which map exactly to `0x80`, `0x02`, and
`0x01`. Fervor was temporarily position 2 (`0x40`) and is vanilla position 8 (`0x01`).

## Replacement fixture

- Artifact: `work/1784436517-dcl-fear-josephine-fervor-battlebyte-correct-arthur-999hp-fixture.png`
- Manifest: `work/1784436517-dcl-fear-josephine-fervor-battlebyte-correct-arthur-999hp-fixture-manifest.md`
- SHA-256: `09795A0D12766DFFADC3CD90DAFA4402F6FFC43C2D45DA2B3FFC14FEACC2BF3F`
- Josephine secondary command: `unit+0x13`, `19 -> 16`.
- Josephine candidate learned flag: battle-record `unit+0xC3`, `0x00 -> 0x80`.
- Arthur current/max HP: `199 -> 999`.

The builder repacked the container, unpacked it again, and passed its exact changed-byte audit. The
fixture is installed in `autoenhanced.png`; the prior autosave is preserved as
`work/1784436641-fft-autoenhanced-before-restore.png`.

## Second live falsifier

The replacement fixture loaded and opened `Abilities > Mystic Arts`, but the only listed ability
was Umbra. No combat action was executed. This proves that `0x80` at `unit+0xC3` is not Fervor.

## Corrected next gate

1. Restore vanilla Mystic ordering and build an audited replacement fixture with `unit+0xC3 = 0x01`
   without editing the live save in place.
2. Require `Abilities > Mystic Arts` to expose Fervor before executing any combat action.
3. Cast Fervor on Arthur and observe the status result.
4. If Fear lands, require forced flee followed by at most one legal non-hostile action or Wait.
5. Archive the runtime log and require `[DCL-FEAR-PLAN]` to report a composed plan with exact state
   restoration.
