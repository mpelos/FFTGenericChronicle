# Fast-load shortcut and LT23 Counter checkpoint

## Load-path result

Repeated tests now use the Enhanced autosave plus the title-menu **Continue** row. From the visible
Enhanced/Classic selector, the verified atomic sequence is:

1. click Enhanced at Computer Use `(360, 470)`;
2. wait about 4.2 seconds and press `Enter` to skip the opening;
3. wait about 1.6 seconds and click Continue at `(640, 578)`;
4. wait about 22 seconds before inspection.

The sequence repeatedly returned to the exact actionable battle turn in about 28 seconds from the
selector. No screenshot or interpretation call is allowed between the Enhanced click and Continue,
because tool latency lets the opening restart.

The one-time Manual Save 05 route is also deterministic. After skipping the opening, **Load Game**
requires two clicks at `(640, 606)` separated by 0.5--0.7 seconds. The first selects and the second
activates the row; a 0.16-second double click did not activate it. After about 8.5 seconds, `E`, `E`
opens Manual Saves and a direct click at `(640, 172)` loads the first card, Save 05.

The living operational procedure is in
`work/1783908306-fft-autonomous-control-runbook.md`.

## Additional fixture acceleration

The game updated `autoenhanced.png` at the next ally-turn boundary. Closing at Arthur's actionable
turn and snapshotting produced:

- `work/1784103536-fft-autoenhanced-snapshot.png`
- SHA-256 `3D5BB86CD66BFCD3B353B7E652CF80B4A2BBC23EAF8F2C7A5BB2D060FC9781ED`

Continue loaded this state directly with Josephine alive in the safe southeast position and Arthur
ready to act, removing the remaining enemy-turn wait. This snapshot is evidence for the general
turn-boundary optimization, not the final LT23 fixture.

## Counter fixture result

Josephine's deployed Manual Save 05 fixture showed Counter in her in-game Status screen. This proves
that roster-save unit `+0x08` is the equipped-Reaction word. The generator now also validates the
learned Reaction bit before equipping it. Reaction/Support/Movement positions 1 through 6 map to
third-byte masks `0x80`, `0x40`, `0x20`, `0x10`, `0x08`, and `0x04`; Counter is Monk position 2 and
therefore requires `0x40`.

Arthur does not have Counter learned. The generator rejected the attempted Auto-Potion-to-Counter
edit before producing artifacts. Rion does have Counter learned, so an audited non-deployed
alternative was produced:

- `work/1784104009-lt23-save05-rion-and-josephine-counter-fixture.png`
- source Reaction on Rion: Gil Snapper `439`
- destination Reaction: Counter `442`
- Josephine's previously verified Counter remains unchanged

Rion is the next live-test carrier candidate because Josephine's 91 HP made the observed enemy hit
exactly lethal. The lethal control log is
`work/1784102954-lt23-counter-lethal-control-live.log`; it records Josephine `91 -> 0` and no Counter
commit, which is expected for a dead reactor.

Enhanced basic Attack excluded allied units: mouse hover displayed the ally information pane, but
`F` did not accept the ally and keyboard target cycling moved to an enemy. Friendly-fire basic Attack
is therefore refuted as the LT23 trigger route.

## Handoff

1. Deploy the audited Rion/Counter manual-save fixture while the game is stopped.
2. Use the atomic Manual Save 05 route once, verify Counter on Rion's Status screen, and build an
   actionable Rion autosave.
3. Use Continue for all pass-2 repetitions and correlate visible Counter with
   `[DCL-REACTION-COMMIT] reactionId=442`.
4. Capture Auto-Potion as the second native Reaction family before promoting the commit site to
   Proven.

The game and Reloaded-II were closed. The installed DLL, runtime settings, Reloaded AppConfig,
manual save, autosave, and prior log were restored and matched all six pre-test SHA-256 hashes. The
full offline suite completed successfully afterward.
