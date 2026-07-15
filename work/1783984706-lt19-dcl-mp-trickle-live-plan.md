# LT19 — DCL own-turn MP trickle

## Prerequisites

Run only after the earlier LT14–LT18 queue and only when the unmodified Enhanced game reaches its
title/menu. The profile is prepared offline and is not installed while the base game launch blocker
remains.

## Objective

Prove that the poll-driven DCL MP economy grants one configured credit at the start of the unit's own
turn, clamps it to MaxMP, and never repeats while the own-turn marker remains active.

Profile: `1783984706-battle-runtime-settings.lt19-dcl-mp-trickle.json`.

## Sequence

1. Launch Enhanced, press Enter to skip the intro, choose Load, Manual Saves, and the first entry
   (save 05).
2. Confirm Ramza is `char id 0x01` in the unit log. If the save uses another id, change only the
   `t.charId == 1` literal to the observed id and revalidate the profile.
3. Spend at least 6 MP with Ramza and record current/max MP before his next turn.
4. End turns until Ramza's turn starts. Observe MP before taking any action.
5. Leave the command menu open for several seconds; MP must not climb again during the same turn.
6. End Ramza's turn and wait for his following turn. A second credit of exactly 3 must occur.
7. Repeat near MaxMP: arrange `MaxMP - currentMP` as 1 or 2, then confirm the credit stops exactly at
   MaxMP.

## Pass gates

- exactly one `[DCL-MP-TRICKLE] ... outcome=credited ... credit=3` per Ramza own-turn rising edge;
- current MP rises by the same exact amount in game state;
- holding the menu open produces no repeated credit;
- all non-Ramza units log no credit or remain unchanged;
- the near-full case clamps to MaxMP;
- no `formula-error`, `write-failed`, false native `MPGAIN`, crash, or state corruption.

## Failure interpretation

- credit fires immediately when attaching during an already-active turn: initialization edge guard is
  wrong;
- repeated credits during one turn: `+0x1B8` is not stable as assumed or state is being discarded;
- log says credited but the game reverts MP: a downstream native writer owns `+0x34` later in the turn;
- no rising edge ever appears: the own-turn marker hypothesis needs a replacement based on CT/action
  ownership;
- only the display changes: a separate MP presentation cache exists and must be refreshed.
