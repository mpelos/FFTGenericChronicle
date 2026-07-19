# LT41C attack-alias CT gate — no Choco Beak result

## Outcome

The corrected attack-alias CT fixture passed its turn-order gate: after Rion ended his open turn,
Janus the adjacent Chocobo became the next active unit at CT `100`. Janus did not attack Rion and the
battle returned to Rion's command menu with HP still `277/277`.

Raw capture:
`work/1784172185-lt41c-dcl-dual-wield-attack-ct-no-chocobeak-live.log`
(SHA-256 `AA239444CC43B85359705EAA359A2F4B77D3E9ED6CA5D1DE65CD16A393768ACE`,
17,964 bytes).

## Cause

Rion begins this fixture Invisible. The original LT40 Auto-battle action removed Invisible before
Choco's turn; LT41C used Wait, so the AI continued to ignore Rion. No attack calc or HP transaction
occurred. The run therefore provides no second-strike provenance evidence.

The exact offline status representation is effective byte `unit+0x63` and durable master byte
`unit+0x1F1`, both using mask `0x10`. In the source Rion carries effective `0x30`, master `0x10`, and
source `unit+0x59 = 0x20`; the latter is Reraise and must remain untouched.

## Restoration

FFT and Reloaded-II were closed at the contrary action boundary. DLL, PDB, runtime settings,
AppConfig, battle log, and autosave were restored, with all six pre-test hashes verified.
