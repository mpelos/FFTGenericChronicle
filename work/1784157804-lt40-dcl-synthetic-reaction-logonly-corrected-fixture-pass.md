# LT40 synthetic-Reaction log-only corrected-fixture pass

## Scope

This bounded live run validates only the generic successful-result-to-pre-selector mechanism. It
enables no synthetic memory write, accepted-order write, effect, status, stat, balance value, or
job-specific behavior.

## Offline preflight

- Full `codemod/run-offline-checks.ps1`: PASS in 57.4 seconds.
- Blank-carrier static anchors: PASS in
  `work/1784157190-dcl-blank-reaction-carrier-analysis.md`.
- Pass-2 source-exclusion/read/clear anchors: PASS in
  `work/1784157052-dcl-reaction-queue-analysis.md`.
- Installed DLL: `21B8A5E95E2DFB1C1C975761AF1E23CED7479B0725584FA78D5A1137BF19FD43`.
- Installed PDB: `285EEF29E8A42C70337B5E1FD1E7A819007EEDBFBB9DE85B2960A5B3BE5775FA`.
- Installed log-only settings:
  `AF99A7D67F11DEF085520BC73A66667ACFF5C83767C7817CBD606334246C242B`.
- Installed corrected fixture:
  `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`.
- Reloaded isolation set: `fftivc.utility.modloader`,
  `fftivc.generic.chronicle.codemod`.

## Startup proof

The archived live dump for accepted defender Rion (`id=0x80`) contains:

- equipped Reaction `unit+0x14 = 443`;
- exact reaction-set `unit+0x94..0x97 = 00 00 04 00`;
- empty candidate `unit+0x1CE = 0`.

The strict analyzer parses these bytes directly from the log and reports
`startup_owner_dumps=1 startup_owner_valid=1`.

## Bounded action sequence

Rion used Auto-battle against Wenyld and selected Throw Shuriken `382`:

- Wenyld HP `396 -> 312`, debit `84`;
- first following pre-selector: `sourceIdx=16`, `candidates=[]`, `producer=none`.

Wenyld then used ordinary Attack against Rion:

- exact source table index `6`, defender table index `16`;
- action type `0x01`, ability `0`;
- Rion HP `277 -> 73`, debit `204`, nonlethal;
- synthetic gate `carrier=443`, `accepted=1`, `replay=0`, `mailbox=armed`;
- following pre-selector: `sourceIdx=6`, `candidates=[]`,
  `producer=synthetic-would-stage`, `syntheticStates=[16:4]:carrier=443`.

The game was closed without saving immediately after the required row. The later Chocobo action had
entered calculation but did not contribute to the gate.

## Validation

Archived evidence:

- `work/1784157661-lt40-dcl-synthetic-reaction-logonly-live-fifth.log`;
- SHA-256 `378C088A90B099914313F6DD0A1E07AB1C54358F6B66248E890DB55AE6D21103`.

Command:

```powershell
python tools/analyze_dcl_synthetic_reaction_live.py work/1784157661-lt40-dcl-synthetic-reaction-logonly-live-fifth.log --carrier 443 --mode log-only --require-startup-owner --expected-reaction-set-hex 00000400
```

Result: PASS with one hook, one gate, one acceptance, one would-stage intent, one valid startup owner,
zero replays, zero staged writes, zero materializations, zero native commits, zero managed commits,
zero cadence consumption, and zero failures.

## Restoration

The game and Reloaded-II were closed before restoration. All six original artifacts match their
pre-test SHA-256:

- DLL `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`;
- PDB `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`;
- settings `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`;
- Reloaded AppConfig `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`;
- game log `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`;
- autosave `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`.

## Decision

The read-only successful-result-to-pre-selector vertical is proven. The earlier `442` was caused by
an inconsistent fixture and is absent with the corrected reaction-set. This gate permits a separate
one-write live test of carrier staging, accepted-order materialization, source retargeting, delivery,
and producer-owned cadence. It does not itself prove any of those write/delivery stages.
