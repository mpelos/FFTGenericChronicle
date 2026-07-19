# DCL Approach target-authority live result

Evidence archive: `work/1784355832-dcl-approach-target-authority-live.log`

SHA-256: `C85ADA6CF9E54279F4F0DB4D1B3E5C483A7BF1E465FBACAE01D9591B1EDB5D25`

Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
Runtime configuration: Approach owner `443`, delivery `442`, horizontal reach `1..2`, same-layer,
one continuation write.

## Visible protocol

Rion entered Auto-battle with **Attack Enemy** focused on Wenyld. Rion moved and used Throw
Shuriken; Wenyld then moved and used basic Attack against Rion. Janus began the following turn,
entered Rion's configured reach on his terminal route step, and continued to the ordinary Choco
Beak forecast and execution after the synthetic queue rejected its candidate. The game did not
freeze and Janus ended at CT `10`.

## Transaction result

Janus route event `15` is the first corrected final-step handoff. It records cursor `3/3`, the
outside-to-inside edge `4,3,0 -> 5,3,0`, exact Rion candidate mask `0x10000`, delivery `442`, source
mailbox `0:442` correctly selector-excluded, and `command=queue-pass2`.

Pass 2 consumed Rion's candidate but Counter's shared typed helper returned `-2` at `0x283019`.
There was no final-validator row, materialization, pass-2 commit, `442` effect, or Approach resume.
The logged `targetIdx=6` is the pre-helper order residue from Wenyld; the helper rejected before it
could overwrite the target descriptor with source index `0`.

Queue outcome `3` released the owned pause. Janus then executed Choco Beak normally, proving that
the pending/command final-step guard does not trap a rejected movement transaction.

## Offline reconciliation and correction

Typed helper `0x283280` receives source-target validation for Counter. After its preliminary order
check, RVA `0x2832FE` requires dynamic tile-table mark byte `+5 & 0x40` on the source tile. Only the
accepted path at `0x283321` writes the source index and coordinates into the reaction order.
Ordinary hostile actions author this transient target map; an interrupted movement frame does not.

The native bridge now revalidates the mover unit position against the paused actor tile and map
bounds, computes the exact source tile, temporarily sets only bit `0x40` for the synchronous pass-2
call, and restores that bit to its prior value before reading the queue result. Its control block
records `targetMark=before->forced->restored`. The next live falsifier requires that trace to restore
exactly, both Counter validators to return zero, one pass-2 commit/effect, one `0x28 -> 0x11` resume,
and normal completion of the same Janus route.
