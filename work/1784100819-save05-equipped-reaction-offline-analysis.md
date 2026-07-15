# Save 05 equipped-Reaction offline analysis

## Question

Can LT23 obtain an explicitly equipped Counter fixture without spending another live session
navigating the Units menus or using the DCL producer to synthesize a Reaction?

## Evidence

FF16Tools unpacked the combined Death/Raise Manual Save 05 fixture to the audited `fftsave.bin`
payload. Manual slot index `4` begins at outer header `0x10 + 4 * 0x9CE4`; its roster begins at
slot-relative `0x518`, contains 54 records, and uses a `0x258`-byte unit stride.

Scanning every active roster record for catalog Reaction ids found a fixed word at unit-relative
`+0x08`. Known live/UI identities agree exactly:

| Unit slot | Identity | `word[unit+0x08]` | Catalog Reaction |
| ---: | --- | ---: | --- |
| 1 | Arthur (`nickname=Arthur`, character `0x80`) | 441 | Auto-Potion |
| 3 | Rion (`nickname=Rion`, character `0x80`) | 439 | Gil Snapper |
| 4 | Leona (`nickname=Leona`, character `0x81`) | 445 | Mana Shield |
| 6 | Josephine (`CharaNameKey=637`, character `0x81`) | 451 | Shirahadori |

Arthur and Josephine are independent live-confirmed Status-screen ground truths from the reusable
LT32/LT38 fixture. Other active roster records also contain valid Reaction ids at `+0x08`, including
Counter `442` in slots 24, 27, and 31. Incidental Reaction-range words elsewhere in the records vary
and are not treated as equipped fields.

## Fixture construction

`tools/build_fft_manual_reaction_fixture.py` was added as a fail-closed generator. It:

1. requires the expected source Reaction id;
2. validates both old and new ids as `Reaction` rows in the ability baseline;
3. edits only the little-endian word at roster-unit `+0x08`;
4. delegates PNG packing and checksum generation to FF16Tools;
5. unpacks the result again and rejects any delta outside checksum bytes `0x4..0x7` and the selected
   Reaction word.

The learned-verified fixture is
`work/1784101683-lt23-save05-josephine-counter-learned-verified-fixture.png`. It changes
Josephine's absolute payload byte `0x286D0` from `0xC3` to `0xBA`, i.e. Reaction `451 -> 442`; the
high byte remains zero. The only other changed bytes are checksum bytes `0x4`, `0x6`, and `0x7`.
Stored and independently recomputed CRC32 both equal `0xE843AB50`.

The learned Reaction/Support/Movement positions occupy the third byte of each generic-job
three-byte ability block. Positions 1 through 6 map to masks `0x80`, `0x40`, `0x20`, `0x10`,
`0x08`, and `0x04`. Counter is Monk R/S/M position 2, so the generator now requires mask `0x40` in
the third Monk byte before equipping it. Josephine has that bit. Arthur does not, and a later
attempt to equip Counter over Arthur's Auto-Potion was rejected before producing artifacts. Rion
does have the bit; the independently audited alternative
`work/1784104009-lt23-save05-rion-and-josephine-counter-fixture.png` changes Rion from Gil Snapper
`439` to Counter `442` and remains pending live deployment.

The generator was also invoked with an intentionally incorrect expected source Reaction (`441`).
It refused the edit with exit code `1` and emitted zero fixture artifacts.

## Conclusion

**Proven live/UI:** roster-save unit `+0x08` is the equipped-Reaction word. The audited Josephine
fixture was deployed and loaded from Manual Save 05; Josephine's Status screen displayed Counter in
the Reaction slot while the other equipped abilities remained Black Magicks, Iaido, Magick Boost,
and Movement +2. This closes the pending UI confirmation and validates the bounded save-edit route.

This route avoids learning/equipping menu navigation and preserves the existing Death/Raise test
capabilities. Once an actionable battle turn is reached, snapshot `autoenhanced.png` and use the
atomic Enhanced-to-Continue fast path for all Counter repetitions.
