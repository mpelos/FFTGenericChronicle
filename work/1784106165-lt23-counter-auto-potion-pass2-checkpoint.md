# LT23 Counter and Auto-Potion pass-2 checkpoint

## Question

Which of the three post-store action-queue boundaries owns an accepted native Reaction, and what
reactor/source/target shape is present there?

## Controls

- The negative baseline contains only pass-1 traffic: blank action id `0` and ordinary Claw `280`.
- Rion's audited Save 05 fixture equips learned Counter `442`; the in-game Status screen confirms it.
- The Death/Raise autosave supplies Arthur's visible Auto-Potion `441` response to Josephine's
  forecasted 14-damage basic Attack.
- All three hooks retain exact-byte guards and run observe-only.

## Counter result

`work/1784105542-lt23-rion-counter-pass2-live.log` records Rion surviving `277 -> 37` HP, followed by
one accepted event at pass 2:

`reactorIdx=4 sourceIdx=0 reactionId=442 actor18C=442 actor142=442 targetCount=1 targets=[0]`

The same target then receives chained `192 -> 3` and `3 -> 0` damage transactions, matching the
native Dual Wield Counter execution. The authoritative automated analysis is
`work/1784105715-lt23-rion-counter-pass2-live-analysis.md`; every check passes.

## Auto-Potion result

`work/1784106049-lt23-auto-potion-pass2-live.log` records one accepted event at pass 2:

`reactorIdx=2 sourceIdx=17 reactionId=441 actor18C=441 actor142=441 targetCount=0 targets=[]`

The empty explicit target list is the native self-directed Auto-Potion shape. The visible forecast
and execution identify the reaction independently of the log. The authoritative automated analysis
is `work/1784106074-lt23-auto-potion-pass2-live-analysis.md`; every check passes.

## Conclusion

RVA `0x206421` / pass 2 is the accepted native Reaction commit boundary. The result holds across an
offensive targeted response and an item-based self response. Each visible Reaction emits one pass-2
commit with agreeing actor ids and correct reactor/source ownership. Pass 1 is generic queue traffic
and cannot own Reaction cadence.

This closes the LT23 commit-classification gate. Synthetic production, retargeting, managed effect
delivery, and persistent cadence remain separate LT29/LT30/LT31 verticals.
