# LT41F Dual Wield provenance wrong-owner result

## Outcome

This run is invalid for the synthetic owner-`443` / delivery-`442` gate. It loaded
`work/1784104894-fft-autoenhanced-snapshot.png`, which equips Rion with native Counter `442`, instead
of the consistent synthetic-owner fixture.

The live sequence reached Rion's Throw and Wenyld's basic Attack. Both were rewritten to one damage.
After the Wenyld hit, the log reported `candidates=[16:442:active=True]`, `producer=none`, and a native
typed-family rejection for distant source `6`. The game process then lost its window and had to be
terminated. No Janus, synthetic gate, owned materialization, or Dual Wield Counter transaction was
reached.

## Evidence boundary

- Raw capture: `work/1784173929-lt41f-dcl-dual-wield-provenance-crash-live.log`
- SHA-256: `D701653DC1F1E6656A347555B267976FFFB29FF412BCEC63361752E1CE1E357E`
- Size: `31,409` bytes

The capture proves that the provenance/pre-clamp rows work through Wenyld's committed hit, but it
says nothing about the synthetic `442` second strike. The crash cannot be attributed to the
synthetic producer because the required owner was absent.

All six external files were restored byte-for-byte from
`C:\Users\mmpel\AppData\Local\Temp\fftgc-lt41f-dual-wield-1784173623`.

## Correction

The replacement gate must load
`work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`, SHA-256
`415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`, whose Rion owner word and
live reaction-set byte agree on carrier `443`.
