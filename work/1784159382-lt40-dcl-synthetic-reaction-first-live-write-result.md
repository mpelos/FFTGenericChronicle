# LT40 first synthetic-Reaction live-write result

## Outcome

The bounded gate failed closed and produced two new facts. It did not validate the complete
transaction.

- The synthetic gate accepted one survived Choco Beak hit against Rion with `sourceIdx=0`.
- The pre-selector staged carrier `443` once on Rion.
- Pass 2 accepted the exact carrier with agreeing ids.
- Carrier `443` materialized natively as `actionType=0`, `actionId=443`, `targetMode=5`,
  `targetIdx=16`.
- The configured exact-original guard expected `1/0`, so the controller reported
  `rewrite=blocked-original`, `rewriteWrites=0`.
- The engine proceeded with the untouched carrier and emitted state-`0x2C` effect `443/443` to
  Rion itself (`targets=[16]`).
- No managed synthetic commit or cadence consumption occurred.

Evidence:

- `work/1784159016-lt40-dcl-synthetic-reaction-live-write-blocked-original.log`;
- SHA-256 `94621F3BF7B1AF16B887FC8A5E663938C06766C99796BAA113A5411500381EFA`.

The strict positive analyzer rejects this capture for missing rewrite, source retarget, final
action `1/0`, managed cadence commit, and delivery to the incoming source.

## Runtime defect exposed

The commit row's `actor` is an actor object, while its `record` is the exact `actor+0x148`
battle-unit pointer. The managed commit path incorrectly passed `actor` to the battle-unit resolver,
making a managed commit impossible. The corrected path passes the captured record and requires
rewrite-owned producer state `5`; a blocked rewrite therefore cannot consume cadence.

## Restoration

The game and Reloaded-II were closed without saving. All six external artifacts were restored to
their exact pre-test hashes:

- DLL `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`;
- PDB `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`;
- settings `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`;
- AppConfig `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`;
- game log `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`;
- autosave `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`.
