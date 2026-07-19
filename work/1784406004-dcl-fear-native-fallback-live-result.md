# DCL Fear forced-flee native-fallback live result

## Visible result

Fervor successfully applied the DCL Fear carrier to Arthur and changed his model to Chicken. On
Arthur's turn, he moved under native control and the turn ended without offering an action. This is
the stock Chicken behavior, not the intended Fear behavior.

## Runtime result

The dispatcher recognized DCL ownership:

```text
[DCL-FEAR-CHICKEN] unit=0x141855EE0 decision=route
```

The coordinator then failed before route staging:

```text
[DCL-FEAR-FLEE] event=1 state=NativeFallback stage=3
unit=0x141855EE0 actor=0x140D31558
before=6,0,2 selected=6,0,0 restored=6,0,2
routeLength=0 cursorBefore=0 battleStateAfter=0xFFFFFFFF
```

The unit tuple was restored byte-exactly, but the selected X/Y matched the origin and the route
resolver returned no route. The bounded failure path correctly transferred control to native
Chicken, explaining the visible automatic move plus lost action.

## Refuted assumption

The Chicken selector's scratch X/Y/layer is not the winning destination. Static disassembly shows
that the selector writes scratch for every candidate, prepares/evaluates each route, and stores its
score. After the scan, native Chicken clears a `0x240`-byte planning block and calls planner
`0x321390(0xFF, 1)`. That planner chooses the best candidate and writes the winning four-byte record
at RVA `0x1872364`. The current coordinator captured the final enumerated scratch candidate before
the planner ran.

## Next offline change

Mirror the complete native selector-to-planner prefix, capture the winning record, restore the
unit tuple, and only then pass its X/Y/layer to route resolver `0x27C7B4`. Keep native fallback for
any planner, identity, restoration, or route failure. A new live test is required only after the
updated transaction passes its static anchors and offline smoke tests.

