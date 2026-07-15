# Runtime Hook Anchor Audit

- Executable: `D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe`
- Steam build ID: `23901820`
- SHA-256: `841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`
- Result: `PASS` (25/25 anchors)

| Anchor | RVA | Result | Actual bytes | Nearby exact matches | Role |
| --- | ---: | --- | --- | --- | --- |
| `preview-hit-pct` | `0x227F86` | PASS | `41 BA 02 00 00 00` | `0x227F86` | DCL forecast hit percentage |
| `result-selector` | `0x205210` | PASS | `48 89 5C 24 08 48 89 6C 24 10` | `0x205210` | result/outcome selector |
| `dcl-miss-kind` | `0x205B38` | PASS | `44 88 A7 C0 01 00 00` | `0x205B38` | execution miss-kind commit |
| `calc-entry` | `0x3099AC` | PASS | `48 89 5C 24 18 55 56 57` | `0x3099AC` | per-action/per-target calculation entry |
| `pre-clamp` | `0x30A5D7` | PASS | `0F BF 45 06` | `0x30A5D7` | same-result HP/status apply window |
| `staged-bundle` | `0x281F12` | PASS | `48 FF C3 48 83 FB 15` | `0x281F12` | post-calculation staged bundle |
| `evade-input` | `0x30F404` | PASS | `48 8B D3 88 4B 41` | `0x30F404` | pre-roll target input |
| `counter-path` | `0x30C700` | PASS | `48 89 5C 24 08 57 48 83 EC 20 80 79 01 FF` | `0x30C700` | counter/reaction staging probe |
| `reaction-commit-p2` | `0x206421` | PASS | `40 88 B3 D3 01 00 00` | `0x206421` | accepted-reaction queue pass-2 commit probe/control |
| `reaction-commit-p0` | `0x2066AE` | PASS | `40 88 B3 D3 01 00 00` | `0x2066AE` | accepted-reaction queue pass-0 commit probe |
| `reaction-commit-p1` | `0x206743` | PASS | `40 88 B7 D3 01 00 00` | `0x206743` | accepted-reaction queue pass-1 commit probe |
| `reaction-preselector-p2` | `0x2063A9` | PASS | `48 8D 4D D2 E8 86 CA 07 00` | `0x2063A9` | pass-2 candidate snapshot before exact-id selector |
| `auto-potion-consume` | `0x2816B2` | PASS | `2A CB 43 88 8C 05 00 7C 1A 01` | `0x2816B2` | shared item inventory decrement before native subtraction |
| `weapon-lof-arc-result` | `0x28030B` | PASS | `8B F8 85 F6 78 08 85 C0 78 07 3B F0 75 03 44 8A FB` | `0x28030B` | Arc resolver result before target-equality gate |
| `weapon-lof-direct-result` | `0x2803A3` | PASS | `8B F8 85 F6 78 08 85 C0 78 07 3B F0 75 03 44 8A FB` | `0x2803A3` | Direct resolver result before target-equality gate |
| `roll-verdict` | `0x30F40F` | PASS | `44 8B D0 BD 01 00 00 00 85 C0` | `0x30F40F` | post-avoidance verdict |
| `reaction-r1` | `0x30BDEE` | PASS | `E8 75 D0 F6 FF` | `0x30BDEE` | Brave reaction roll 1 |
| `reaction-r2` | `0x30BE44` | PASS | `E8 1F D0 F6 FF` | `0x30BE44` | Brave reaction roll 2 |
| `reaction-r3` | `0x30BE9A` | PASS | `E8 C9 CF F6 FF` | `0x30BE9A` | Brave reaction roll 3 |
| `reaction-r4` | `0x30BEDA` | PASS | `E8 89 CF F6 FF` | `0x30BEDA` | Brave reaction roll 4 |
| `evade-copier-b` | `0x2854DB` | PASS | `4C 8D 5C 24 60 49 8B 5B 10` | `0x2854DB` | equipment evade copier |
| `evade-copier-c` | `0x3966BF` | PASS | `48 8B D7 48 8B CE` | `0x3966BF` | equipment evade copier twin |
| `magic-chance` | `0x304D96` | PASS | `B9 64 00 00 00` | `0x304D96` | legacy magic accuracy probe |
| `status-chance` | `0x30659B` | PASS | `8D 4B 5C` | `0x30659B` | legacy native status chance probe |
| `roll-rng` | `0x278E68` | PASS | `E9 EB 38 98 0E` | `0x278E68` | shared native RNG trampoline |

A failed anchor disables its guarded runtime feature. Nearby matches are diagnostic candidates only; they are not accepted automatically.
