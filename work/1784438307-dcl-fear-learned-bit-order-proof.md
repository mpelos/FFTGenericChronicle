# DCL Fear learned-bit order proof

## Static copy proof

Manual Save 05 roster unit 6 and the clean autosave Josephine record share the complete 60-byte
learned-ability block byte-for-byte. The manual block at `unit+0x32` occurs exactly at
current-battle `unit+0xA2`; the record-layout displacement is `+0x70`. Mystic's manual offset
`unit+0x53` therefore maps to current-battle `unit+0xC3`.

## Live mapping proof

The installed temporary Mystic command table ordered its relevant entries as follows:

- position 1: Umbra;
- position 2: Fervor;
- position 7: Quiescence;
- position 8: Empowerment.

The three prior fixtures produced these exact UI results:

- `unit+0xC3 = 0x80` exposed Umbra;
- `unit+0xC3 = 0x02` exposed Quiescence;
- `unit+0xC3 = 0x01` exposed Empowerment.

All three are explained by MSB-first learned bits. The earlier LSB-first hypothesis is refuted.
Fervor in temporary position 2 would use `0x40`; Fervor in vanilla position 8 uses `0x01`.

## Harness correction

The manual-save fixture builder now computes active masks as
`1 << (7 - (ability_index & 7))`. The installed temporary Mystic reorder was returned to vanilla
order so the next DCL test does not depend on job-list modification. The next audited autosave fixture
must set Josephine's current-battle `unit+0xC3` to `0x01` and verify Fervor in the menu before casting.
