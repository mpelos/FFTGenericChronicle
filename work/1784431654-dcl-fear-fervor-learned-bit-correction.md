# DCL Fear Fervor learned-bit correction

## Scope

This checkpoint corrects the Josephine autosave fixture used to exercise the Fear carrier through
Fervor. No combat action was executed in the live attempts recorded here.

## Evidence

- The canonical `JobCommandData` entry for Mystic Arts is command `16`.
- Its active abilities are ids `46..59` in list positions `1..14`.
- Fervor is ability id `53`, list position `8`.
- The generic Mystic job is id `0x55`. Generic learned blocks start at `unit+0x32`, use three bytes
  per generic job, and are indexed from job id `0x4A`.
- Therefore Mystic's learned block starts at `unit+0x53`, and Fervor uses mask `0x80` in that first
  byte.
- The earlier `unit+0xC3=1` fixture exposed Empowerment. The isolated `unit+0xC3=2` fixture exposed
  Quiescence live. This refutes `unit+0xC3` as the Mystic learned-ability byte.

## Corrected fixture

- Artifact: `work/1784430521-dcl-fear-josephine-fervor-correct-arthur-999hp-fixture.png`
- Manifest: `work/1784430521-dcl-fear-josephine-fervor-correct-arthur-999hp-fixture-manifest.md`
- SHA-256: `ABE5DDCB32F5D5982C75C273B23EEAD67A91BA2DC0F632EB90ADF87820C811FB`
- Josephine secondary command: `19 -> 16`.
- Josephine Fervor learned flag: `unit+0x53`, `0x00 -> 0x80`.
- Arthur current/max HP: `199 -> 999`.
- The builder repacked the container and passed its exact round-trip byte audit.
- The fixture is installed in `autoenhanced.png`; the restore helper created
  `work/1784430532-fft-autoenhanced-before-restore.png` first.
- Exact v7 live-install preflight passed after restoration. Installed code-mod DLL SHA-256 remains
  `E4BDABEDF4805F468504AFC037E0C744D15FEF2D5F50318E428F3549E9137566`.

## Live stop point

The first corrected-fixture launch entered the already documented v1.5.1 black-screen/selector
anomaly. A full FFT/Reloaded restart then left the selector keyboard-navigable but unable to confirm
any item, including About. After another complete restart, Computer Use failed two consecutive
Reloaded window captures with `window capture timed out: timed out waiting on channel`. Inputs were
stopped rather than continuing blind.

No Fervor cast occurred and no DCL status-roll evidence was produced in these attempts.

## Next live gate

1. Launch through Reloaded and use Continue on the installed corrected fixture.
2. Open Josephine's Abilities > Mystic Arts and require the only visible learned row to be Fervor.
3. Cast Fervor on Arthur.
4. Close FFT and Reloaded, then archive the fresh runtime log.
5. Require exactly one `[DCL-STATUS]` roll for the Fervor transaction. A resisted roll may leave
   Arthur human; a successful roll may apply Chicken/Fear, but either outcome must use the one
   producer-owned roll.
6. Restore `work/1784390906-fft-autoenhanced-snapshot.png` after the bounded protocol is complete.
