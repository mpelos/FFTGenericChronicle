# DCL active-weapon carrier checkpoint

## Question

The LT41I ordinary Dual Wield pair carried payload `18` in both state-`0x2A` and state-`0x2F`
calculation rows. The Counter pair likewise carried payload `124` in both rows even though `124`
was the preceding thrown Yagyu Darkrood. The investigation needed to determine whether
`orderRecord+8` was actually the active-hand identity and, if not, locate the native carrier.

## Reproducible static result

`tools/analyze_dcl_active_weapon_payload.py` checks the current executable and managed integration.
Its generated report is:

- `work/1784253161-dcl-active-weapon-routing-analysis.md`
- SHA-256 `C94E6F029009E3B33618AD70216CDB80F7116E3C33F0C031FF108F81861430E6`

All byte anchors pass. The protected repeat initializer copies `unit+0x20/+0x24` into normalized
weapon globals at RVAs `0x7B0764/0x7B0766` and stores repeat count/index at
`0x7B0762/0x7B0763`. The selector at `0x309AB5..0x309AE6` chooses:

- normalized right/primary for repeat count greater than two;
- normalized right/primary for repeat index zero;
- normalized left/off-hand otherwise.

The order payload remains a separate formula/order carrier.

## Saved-fixture equipment proof

The exact autosave fixture used by the successful sequence is:

- `work/1784172418-dcl-dual-wield-fast-visible-fixture.png`
- SHA-256 `B4DC074EA2344168D3F88CBDAB487C381D0A79A457B89A39BC8FF96FA66E0FED`

FF16Tools unpacked the container into a temporary directory. Searching the exact Rion signature
`8010035900089003` and reading little-endian words `record+0x20/+0x24` produced the same result in
all three live-state copies:

| Component | Record | Right | Left |
| --- | ---: | ---: | ---: |
| `resume_en00_fturn.sav` | `0x83AF8` | `17` | `18` |
| `resume_en00_main.sav` | `0x83AF8` | `17` | `18` |
| `resume_enbtl_main.sav` | `0x83AF8` | `17` | `18` |

Item `17` is Iga Blade and item `18` is Koga Blade. The authoritative LT41I game log is:

- `work/1784250813-lt41i-dcl-native-repeat-fixed-game.log`
- SHA-256 `ECA1BDB7CDC681284AE01E54A1F1DE5D2CBED5BC2E5B3BF1D526A1F1F917A814`

Its ordinary rows `n=31/32` both carry payload `18`, while native debits are `210/126`. The
fixture and log together refute the former assumption that payload equality identifies one hand.

## Managed integration

The calc-entry ring now captures payload, repeat count/index, and both normalized weapons in the
same synchronous hook. `DclActionContext` retains them. Formula construction uses the derived
native active weapon for `action.weaponItemId`, `action.weapon.*`, and weapon side, while preserving
`action.payload*` unchanged. Smoketests prove right/left routing for different ids and side retention
for equal ids.

## Remaining live gate

One bounded regression must confirm that the newly added managed capture reports repeat indices
`0/1` and active weapons `17/18` on the already-proven mixed-weapon state-`0x2A -> 0x2F` pair. This
is an integration regression only; the native carrier and selection rule require no further live
discovery.
