# LT36 permanent compute-point writer live result

## Purpose

Validate that the permanent DCL numeric writer publishes formula results at the AI-visible
post-calculation boundary, caches only confirmed execution, and reuses that exact result at
pre-clamp without evaluating the formula twice.

## Fixture and route

- Decisive autosave fixture: `work/1784087502-fft-autoenhanced-snapshot.png`.
- Fast path: Reloaded-II -> Enhanced Start Game -> timed intro skip -> Continue.
- Deterministic turn advance: Ramza Wait, then Rion Wait.
- Enemy source/turn owner `3`, Choco Beak ability `265`, candidate targets Ramza `16` and Rion `17`.

## LT36-A: AI ranking and native-outcome gate

- Profile: `work/1784088888-battle-runtime-settings.lt36-dcl-compute-point.json`.
- Log: `work/1784089341-lt36-compute-point-ai-ranking-live.log`.
- Log SHA-256: `3C77CFED2EA41957DC502776F1727173B502E9BFA3E639EBEE959426C47DCB42`.
- The writer changed Ramza's AI bundle from native `122` to `4095`, left Rion at `79`, and the
  enemy selected Ramza.
- Confirmed execution produced native kind `0x0B`, zero debit, and no apply flag. The formula in
  this diagnostic profile still staged `4095`, but the native outcome correctly prevented the
  pre-clamp delivery path. This is an outcome-gate observation, not a writer failure.

## LT36-B: cached execution delivery

- Profile: `work/1784089342-battle-runtime-settings.lt36b-dcl-compute-point-delivery.json`.
- Log: `work/1784089584-lt36b-compute-point-delivery-live.log`.
- Log SHA-256: `AC6465DE53B378CE0BF52FF108AB4FFAC037B2B81B848B9C807438D056EAAD7C`.
- The formula changes only an already-positive native debit for Rion:
  `if(t.charId == 128 && dcl.oldDebit > 0, 4095, dcl.oldDebit)`.
- At AI state `0x05`, the writer published Ramza `122` and Rion `4095`; forecast and confirmed
  execution both selected Rion.
- At execution state `0x2A`, native Rion debit `79` became `4095` and was recorded with `cached=1`.
- Pre-clamp logged `oldDebit=79 debit=4095 computePoint=1`, proving exact cache consumption rather
  than a second formula evaluation. Rion reached 0 HP through the native apply/KO lifecycle.
- No `no-compute-point-result` row occurred for the delivered Rion transaction.

## Conclusion

**Proven live:** the permanent compute-point numeric writer is early enough for protected AI target
ranking and reuses the exact confirmed execution result at the later apply boundary. AI evaluations
remain transient. Native miss/block/reaction outcome kinds remain authoritative unless the separate
DCL hit/output controls explicitly own them.

## Cleanup

- FFT Enhanced and Reloaded-II were closed.
- The decisive autosave was restored and verified as
  `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`.
- Installed code-mod DLL was restored to
  `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`.
- Installed runtime settings were restored to
  `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`.
