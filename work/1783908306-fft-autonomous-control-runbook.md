# FFT Enhanced autonomous control runbook

## Scope

This checkpoint records the controls and safe operating path verified against FFT Enhanced v1.5.0 on the Windows live-test machine. It is an operational reference for unattended Generic Chronicle live tests, not a timeless engine specification.

## Launch path

1. Activate Reloaded-II (`C:\Reloaded-II\Reloaded-II.exe`).
2. On a cold Reloaded-II start, the initial page is **Mod Loader Settings**. Click the fourth icon
   in the left sidebar (FFT profile; Computer Use client coordinates approximately `(27, 202)`) to
   open **Configure Mods**. Do not mistake the initial settings page for the game profile.
3. Use **Launch Application** so the configured Generic Chronicle and Battle Probe mods load.
   At 1000x600 Reloaded-II client size, the verified button coordinate is approximately `(110, 130)`.
4. Wait for `FFT_enhanced.exe` and target the window titled **FINAL FANTASY TACTICS - The Ivalice Chronicles**.
   The injected console appears first and the CRIWARE/title animation can take 15--20 seconds. Poll
   the actual game window until the Enhanced/Classic version selector is visibly present. Never
   start the timed fast-load sequence from the console, CRIWARE splash, or title animation.
5. On the launcher screen, choose **Enhanced > Start Game**. Mouse-clicking the button is more
   reliable than `Enter` on this launcher.
6. As soon as the Enhanced opening movie begins, press `Enter` to skip it.
7. Select **Load**, then **Manual Saves**.
8. Load the first save in the list, identified as **05**. This is the designated live-test save.

### Direct Reloaded launch shortcut

Reloaded-II accepts its official shortcut form and this avoids navigating its launcher UI:

```powershell
C:\Reloaded-II\Reloaded-II.exe --launch "D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe"
```

Prefer `tools/launch_fft_enhanced_test.ps1` for unattended work. It validates that the game is
closed, verifies the Reloaded profile target, can restore a named autosave snapshot, invokes the
same official shortcut, and waits for `FFT_enhanced.exe` to appear. This removes the Reloaded UI
from the normal test path; the first required game input is the Enhanced selector.

The application profile still supplies the enabled-mod list. Verify `C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json` before using the shortcut; the probe isolation set is the mod loader plus Generic Chronicle Battle Probe.

For hash-bound DCL profiles, run `python tools/validate_dcl_live_install.py` before launching. It
checks only configuration and installed artifacts: the exact three-mod isolation set, Enhanced
application target, runtime settings, action-data NXD, equipment/charge tables, runtime item/ability
catalogs, and installed DLL against the current Release build. A failure means the run cannot
produce evidence for that paired profile.
The preflight deliberately performs no process inspection.

The default bundle formerly selected by `tools/install_dcl_live_bundle.py` is retired because it
contains Approach and Fear. The installer intentionally rejects it. Do not use `--apply` until a new
hash-bound pair without those mechanisms becomes the explicit `--pair`. For an approved pair, run a
read-only dry run first and use `--apply` only after FFT and Reloaded are visibly closed. The
installer performs no process inspection, backs up every destination, writes atomically, rolls back
on failure, and requires the exact post-install preflight to pass.

## Test-execution ownership

Autonomous execution is the default. Navigate, launch, load the fixture, perform the battle action,
close the game, and inspect the evidence without waiting for the user. Transfer a live-test action to
the user only when the user explicitly offers to perform that test. A user-performed exception does
not change ownership of later tests.

Treat this runbook as cumulative operational memory: whenever a verified shortcut, input sequence,
fixture, recovery path, or observation makes later tests faster or safer, add it here. Keep hypotheses
and one-off outcomes in their timestamped test journals until they are repeatable.

Do not substitute `steam://rungameid/1004640` for Reloaded-II's **Launch Application** command.
That Steam protocol route can open a fully playable Enhanced session without injecting Reloaded;
require a fresh `Generic Chronicle Battle Runtime Harness` header and the intended hook-install line
after every launch. A stale log from a prior modded process is not proof that the current game is
instrumented.

When Reloaded shows its console, Computer Use exposes two windows for `FFT_enhanced.exe`. The
console window title is the full executable path; do not target or automate it. Select the visual
window whose title is exactly **FINAL FANTASY TACTICS - The Ivalice Chronicles** before sending any
game input. Its verified window geometry is `1282x752`, which preserves the coordinates below.

Do not set `AppArguments` to `/deactiveplay 1` in an attempt to skip the Enhanced/Classic selector.
Current-build static analysis proves that the executable parses this switch as a boolean, and a
live launch proves that it only disables the alternate play-mode button; the selector remains.

### Version 1.5.1 launch anomaly (operationally cleared)

On the first unattended run after the 1.5.1 update, **Enhanced > Start Game** entered a black screen with the feather cursor and did not advance after two minutes. Sending `Enter` or `F` from that screen returned to the version selector; `Space` did not advance it. The same behavior reproduced when the executable was launched directly and from Steam without Reloaded injection, so the symptom was not sufficient evidence of a probe or hook failure.

On 2026-07-13 the user launched the game through Reloaded-II, selected the Reloaded option, and the
game opened normally. Live testing is operational again. If the black screen recurs, close with
`Alt+F4`, verify visually that the game window closed, and retry through Reloaded-II before changing probe code or
interpreting the symptom as a hook regression.

The installed executable is Steam build `23901820`, SHA-256
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`. The offline audit in
`work/1783985353-runtime-hook-anchor-audit.md` passes all 18 current runtime anchors. Re-run
`python tools/audit_runtime_hook_anchors.py` after every game update before interpreting a live probe.

## Verified keyboard controls

These assignments are read directly from **Settings > Keyboard** and exercised in the game:

| Action | Primary | Built-in alternate / note |
|---|---:|---|
| Confirm | `F` | `Enter` |
| Cancel / back | `Backspace` | `Esc` |
| Move cursor up | `W` | Up arrow |
| Move cursor down | `S` | Down arrow |
| Move cursor left | `A` | Left arrow |
| Move cursor right | `D` | Right arrow |
| Skip movie/dialogue presentation | `Space` | `Enter` skipped the Enhanced opening movie |
| Change display | `1` | Context-sensitive |
| Sort | `T` | Context-sensitive |
| Set/remove favorite | `R` | Units menu |
| Change combat set | `X` | Units menu |

Additional controls shown and verified by the current screen guides:

| Context | Control | Action |
|---|---:|---|
| Settings/menu tabs | `Q` / `E` | Previous / next tab |
| Settings values | `A` / `D` or Left / Right | Change value |
| World map | `C` | Center on Ramza |
| World map | `WASD` | Move cursor |
| World map | `Z` | Zoom out |
| World map | `T` | Locations |
| World map | `Backspace` / `Esc` | Open main menu |
| Units menu | `G` | Move unit |

The Gameplay settings screen has **Fast-forward: Enable**. Community documentation identifies `Ctrl` as the default fast-forward hold key; this still requires an in-battle live verification before relying on it.

## Verified mouse behavior

- Left-click selects menu buttons, tabs, and rows.
- Hover position is visible as the feather cursor and can be used to verify targeting before clicking.
- In the current Computer Use path, one coordinate click commonly moves only the feather. Re-observe
  the game window and send a second click to the same visible control to activate it. Do not assume
  that the first click selected a menu item merely because the feather moved.
- Confirm keys are frame-sampled and a single synthetic pulse can be missed. For a modal **Yes/No**
  prompt whose accepted state has no immediately confirmable child menu, use three or four `Enter`
  pulses separated by roughly `45-60 ms`, then re-observe. Do not reuse that burst inside nested
  menus: six `F` pulses can spill into the next target screen. On the title/Continue screen, where
  short pulses are especially unreliable, a bounded burst of up to twenty `F` pulses with about
  `30 ms` gaps is verified to enter loading; stop as soon as the spinner/state transition appears.
- Menu hover can override a keyboard selection. Before confirming a row with `F`/`Enter`, move the
  feather to the title bar; otherwise a stale hover can activate the adjacent row. On the Enhanced
  title menu, direct clicks are safest, and the visible row coordinate must be used without an
  assumed title-bar offset.
- If a selected row ignores isolated confirmation pulses while navigation still works, keep the
  feather on the title bar and cover several frames with paced `F` pulses. Four pulses were
  sufficient at the Enhanced selector; the load browser required a denser multi-frame burst. Stop
  as soon as the next state is visible so inputs do not spill into a child menu.
- On the current 1.5.1 selector, confirmation can require irregular pacing rather than a fixed
  interval. With the feather on the title bar, use bounded batches of six `F` pulses separated by
  roughly `37-71 ms`, re-observing after every batch. The Enhanced `Start Game` transition required
  two batches in one verified run; `Continue` required two batches of five. The opening movie then
  required six similarly paced `Enter` pulses to reach the title menu. Never send the whole launch
  sequence as one burst because confirmation pulses can spill through nested battle menus.
- In tactical movement and targeting, clicking the visible unit or highlighted tile is reliable when arrow-key movement is obstructed or ambiguous. Verify the target panel before confirming an action.
- Mouse wheel scrolls the keyboard-assignment list.
- Right-click produced no cancel/back action on the Keyboard settings screen; do not rely on it. Use `Esc` or `Backspace`.
- Battlefield camera rotation and battlefield zoom still require an in-battle validation pass.

## Verified tactical-battle controls

- Arrow keys move the tile cursor in movement and target-selection modes; a direction may appear to do nothing when the adjacent tile is blocked or not selectable.
- `F` confirms Move, ability, target, forecast, and facing prompts.
- `Esc` cancels the current tactical selection. After moving, select **Reset Move** to return the unit to its original tile.
- In menus, `W`/`S` and Up/Down move the selection. **Wait** is the third normal turn command.
- Directly clicking a highlighted movement tile or a visible target is faster and less ambiguous than deriving isometric grid directions. Confirm the bottom-right target pane before pressing `F`.
- Key `1` can expose names above battlefield units while targeting. Use it before camera rotation to
  distinguish occluded units such as **Wenyld**, **Janus**, **Herkyna**, and **Timothy**; the feather
  cursor alone still does not prove selection.
- A click on a visible unit can select it even when a foreground rock covers its tile; the target
  pane and height then identify the unit correctly, but `F` still rejects it if the tile is outside
  the action's actual range. Treat target identity and target legality as separate checks.
- The selected unit pane is not proof of melee reach. For `Attack`, require the unit's tile itself
  to be one of the yellow highlighted tiles; a unit can be visually close and selected by name yet
  still be separated by one grid square or an invalid height transition.
- Mouse hover over the battlefield can override or stall keyboard target confirmation. For a reliable manual attack: click the target once, verify its bottom-right information pane, click the game window title bar to move the pointer off the battlefield, then press `F` to enter the forecast and `F` again to execute.
- A selected target is visibly confirmed by the gold target tile/outline and its portrait/name in the bottom-right pane. Do not treat the feather cursor alone as selection proof.
- To create a random encounter from the world map, press `C` to center on Ramza before pressing `F` to search. In formation, `F` selects/places the highlighted unit; after placing the party, press `Space` and click **Yes** to commence.
- In the Save 05 formation carousel, Arthur is immediately followed by Josephine. After locating and
  placing Arthur, select the next placement tile, press `F`, then one `E` selects Josephine. This is
  the shortest verified two-unit setup for Death/Raise tests.
- Auto-battle target selection uses two distinct clicks on the same unit: the first selects and the second confirms. **Attack Enemy** optimizes the action and may choose Throw or another ability instead of basic Attack, so use manual targeting when the probe requires a specific action type.
- In **Auto-battle > Attack Enemy** target selection, `Q`/`E` rotates the battlefield camera. Rotate
  until an enemy is visible, click that unit once to select it and pan the camera, then click its new
  on-screen position a second time. Confirm **Concentrate on attacking this unit? > Yes**. Timeline
  portrait clicks do not substitute for selecting the battlefield unit.
- In the Rion LT40 fixture, let the Auto-battle submenu finish opening before sending its next key.
  From that submenu, two `E` rotations and one `Z` zoom expose Wenyld at the far left. Enter
  **Attack Enemy**, click the partially visible archer near `(24,230)`, require the bottom-right pane
  to say **Wenyld**, Archer, HP `396`, then click her panned position near `(455,360)` and confirm.
  If that edge click only pans the camera, enable the name overlay with `1`, continue rotating `E`
  until **Wenyld** is visible at the edge, click her until the lower-right pane reads
  **Wenyld / Archer / 396 HP**, and only then perform the distinct confirmation click. In the broad
  1280x768 view, the verified post-pan confirmation position is near `(791,450)`.
  Do not use Herkyna for an isolated synthetic-Reaction gate: her forecast exposes native Counter.
- If a move ends outside the required range, cancel back to the turn menu and use **Reset Move**;
  this immediately restores the original tile and reopens movement selection without spending the
  turn. Choose an unobstructed enemy and a visibly highlighted adjacent tile before committing.
- A rejected target can add one extra cancel layer. Dismiss the red error first, then cancel target
  selection and the ability list; three `Esc` presses, verified by screenshots, return to the main
  turn menu in this case.
- For calc-provenance probes that need a forecast-only row and a confirmed row but do not require a
  hostile action, Ramza's **Mettle > Focus** is a reliable self-target fallback. Selecting Focus
  opens a 100% forecast immediately; `Esc` cancels it, while moving the feather to the title bar and
  pressing `F` executes it.

## Probe isolation profile

For runtime-hook probes, isolate the Reloaded FFT profile to:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`

Reloaded's shared hook dependencies load automatically. Keep the data mod and unrelated mods disabled unless the protocol explicitly requires them. Verify the runtime log contains the intended hook-install lines before navigating into a battle.

LT20 instant-KO is an explicit exception to the normal code-only isolation: it is safe only when
ability id 30 is data-neutralized together with its matching runtime rule. Use the paired files
`work/1783986506-battle-runtime-settings.lt20-dcl-instant-ko.json` and
`work/1783986506-lt20-dcl-instant-ko-live-plan.md`; never enable the rule against the unmodified native
Dead rider.

Bequeath Bacon/Crystal requires no live slot while its native formula `0x57` and action data remain
unchanged. Offline current-build anchors preserve the bounded level gain and native caster Crystal
lifecycle; schedule a live regression only if that formula, its status metadata, or lifecycle tail is
modified.

LT21 reaction taxonomy is prepared but not deployed. Use
`work/1783988894-battle-runtime-settings.lt21-dcl-reaction-taxonomy.json` with
`work/1783988894-lt21-dcl-reaction-taxonomy-live-plan.md`. Its fast diagnostic order is Shirahadori
(VM-scoped Caution and Brave restoration), Counter (exact-id Courage gate), Mana Shield (flat Neutral
diagnostic only; the final Time Mage design is Caution), then one forced DCL miss. Before launch,
require all twelve anchors in the latest
`dcl-reaction-scope-analysis` report and settings-validator success. A persistent Brave change,
chicken state, or missing restore log is a hard stop.

Do not treat LT21's three diagnostic rows as the final reaction roster. The complete native and
final-job implementation inventory is `work/1783989749-dcl-reaction-implementation-manifest.md`;
candidate assignments and Hypothesis categories stay out of deployment profiles until resolved.

## Fast repeated-test load path

Use this path after a test protocol has established the exact Enhanced autosave state that should be
repeated. It bypasses **Load Game**, the save tabs, and manual row selection on every subsequent run.

**Fixture rule:** after a useful formation reaches the exact pre-action state needed by a protocol,
finish that session by preserving an autosave snapshot. Record the snapshot path, hash, acting unit,
target, turn, and visible command-menu state in the test journal. Do not ask the user or an unattended
run to rebuild the same formation on the next repetition. Manual Save 05 is the fixture-construction
baseline; the preserved autosave is the repeated-test entry point.

If the current process reaches **Game Over**, an even shorter same-process retry is available:
confirm **Retry**, select **Retry from Start of Battle** (one `Down` from the first confirmation
choice), and press `F`. This returns to the first actionable turn in about 18 seconds without the
version selector, intro, title menu, or save browser. The runtime DLL and probe log remain from the
same launch, so archive or delimit the log before interpreting exact event counts.

The Enhanced autosave container is:

`C:\Users\mmpel\OneDrive\Documentos\My Games\FINAL FANTASY TACTICS - The Ivalice Chronicles\Steam\76561198044337912\autoenhanced.png`

Manage it only while `FFT_enhanced.exe` is stopped:

1. Capture the desired state once with
   `tools/manage_fft_enhanced_autosave.ps1 -Action Snapshot`.
2. Before each repeated run, restore that named snapshot with
   `tools/manage_fft_enhanced_autosave.ps1 -Action Restore -SnapshotPath <work snapshot>`.
3. Launch through Reloaded-II and choose **Enhanced > Start Game**.
4. Start timing only after the Enhanced/Classic selector is visibly present. Click Enhanced
   **Start Game** (1280x720 client coordinate approximately `(360, 470)`), wait about 4.2 seconds,
   then press `Enter` to skip the opening.
5. Treat everything from the Enhanced click through the Continue click as **one atomic input
   burst**. Do not take a screenshot, request accessibility state, yield for interpretation, or
   issue the steps as separate tool calls. In the same control call: wait 4.2 seconds, press
   `Enter`, wait 1.6 seconds, and double-click **Continue** at current window-relative coordinate
   `(640, 607)`. This prevents tool latency from leaving the title menu idle long enough to restart
   the opening movie. Do not use the obsolete `(640, 578)` coordinate.
6. Allow roughly 22 seconds for the saved battle/map state to finish loading before inspecting it.

The current window-relative Computer Use runtime no longer applies the older title-bar coordinate
offset: input `(640, 578)` selects **New Game+**, while the visible **Continue** row is approximately
`(640, 607)`. One click can only move the feather; two clicks about 0.65 seconds apart select and
activate Continue reliably. Always use the visible row position from the current 1280x720 capture
instead of assuming the older offset.

This full sequence was repeated from a restored container and returned to the same Ramza turn in
the same battle. The latest atomic-burst validation clicked Continue 5.94 seconds after Enhanced and
reached the loaded-state screenshot at 27.99 seconds. `F` and `Enter` did not reliably activate
Continue in this title-menu context; the direct click is the verified unattended route. Keep
Reloaded-II open after closing the game so the next cold code-mod run needs only **Launch
Application**, the selector, and this atomic burst.

With the Reloaded `--launch` shortcut and an already prepared autosave, the latest validation sent
the Enhanced-to-Continue burst in 5.94 seconds and captured the actionable battle about 14.94
seconds after the Enhanced click. No screenshot, accessibility read, or tool yield occurred inside
the burst. The autosave and Reloaded application config hashes were unchanged after closing.

### Fast one-time Manual Save 05 load path

Use this only when establishing a new fixture. Like the Continue route, send the title-menu inputs as
one atomic burst so tool latency cannot leave the menu idle long enough for the opening movie to
restart:

1. Click Enhanced **Start Game** at Computer Use coordinate approximately `(360, 470)`, wait about
   4.2 seconds, press `Enter`, then wait about 1.0--1.6 seconds for the title menu.
2. Click **Load Game** at approximately `(640, 606)`, wait 0.5--0.7 seconds, then click the same
   coordinate a second time. The first click selects the row and the second activates it. Do not use
   a double-click-speed interval: 0.16 seconds did not activate the row. `F` and `Enter` are also not
   reliable substitutes in this menu.
3. Allow about 8.5 seconds for the save browser to appear. Press `E` twice to change **All** to
   **Manual Saves**.
4. Click the center of the first save card directly at approximately `(640, 172)`. The first card is
   the designated Save 05. Direct clicking avoids a stale feather hover overriding keyboard
   selection.

Do not stop for a screenshot between the Enhanced click and the second Load Game click. Inspect only
after the save browser has had time to appear. This removes the previous race against the repeating
intro and reduces the manual baseline route to one deterministic input burst plus one card click.

The Death/Raise fixture is `work/1784095864-fft-autoenhanced-snapshot.png` (SHA-256
`3C6677ED9E51070D38C13539C2F00B286022D164BD82EDFDE58D354624DEE0E5`). It opens on Josephine's
actionable turn with Arthur adjacent. Josephine's Black Magicks reaches Death after ten `S` inputs
from Fire. Arthur's White Magicks lists Raise after Curaja; pace repeated menu inputs or verify the
label because an unpaced burst can stop one row early. After Arthur queues Raise, use Teleport to a
distant blue tile before confirming his facing: the nearby monster's Claw deals 162 in this fixture
and otherwise leaves the 199-HP caster at 37 before the next cycle.

The same fixture is the shortest verified LT32 ordinary-status carrier setup. On Josephine's turn,
choose **Abilities > Attack**, click the adjacent Arthur, move the feather to the game title bar,
then press `F` for forecast and `F` for execution. The forecast shows 14 damage and Arthur's
Auto-Potion. Under the LT32 profile, execution produces one 14-damage transaction, the red Blind
icon above Arthur, and the independent Auto-Potion response.

In this fixture Arthur has Auto-Potion, while Josephine has Shirahadori; Josephine is not a Counter
fixture. Do not infer an equipped Reaction from arbitrary candidate words such as `unit+0xF2`:
verify the in-game **Status** screen or the reaction-set bitfield owned by the protocol. For a Counter
commit test, establish and snapshot a unit with Counter explicitly equipped before beginning the
bounded capture.

The audited Manual Save 05 Counter fixture is
`work/1784101683-lt23-save05-josephine-counter-learned-verified-fixture.png` (SHA-256
`342EBC4F96705AB0285E502B6B94D1371DBD7FACB7B1A7D5573D8BB80AAF38D7`). It preserves the combined
Death/Raise learned-ability fixture and changes only Josephine's equipped-Reaction word from
Shirahadori `451` to Counter `442`, plus the FF16Tools-owned checksum. The roster-save Reaction word
is at unit-relative `+0x08`; Josephine's absolute Save 05 payload offset is `0x286D0`. The generator
`tools/build_fft_manual_reaction_fixture.py` requires the expected source Reaction and performs a
pack/unpack byte-delta audit, so use it instead of an unaudited hex edit. Josephine's in-game Status
screen was checked after deployment and showed **Counter**, proving the fixture before the actionable
turn autosave was created.

The preferred nonlethal Counter manual fixture is
`work/1784104009-lt23-save05-rion-and-josephine-counter-fixture.png` (SHA-256
`17D2C4C66469CA685E27BDC6932D10BE3D68549444FB812D280062D97CE7BB33`). It changes Rion's Gil
Snapper `439` to learned/equipped Counter `442` and preserves Josephine's verified Counter. In Save
05 formation, Ramza is the first carousel unit and Rion is the second, so one `E` selects Rion.
Rion's in-game Status screen shows Ninja, HP 277, Brave 97, and Counter.

The corresponding actionable Rion autosave fixture is
`work/1784104894-fft-autoenhanced-snapshot.png` (SHA-256
`3A6DDE7F777690F3095FB64CC36CAB190E9AB47B0371192FC3551C0435A41CC7`). It opens directly on
Rion's first ally turn and is the preferred LT23 repeated-test entry point. Rion begins Invisible,
so enemies ignore him until he acts; **Auto-battle > Attack Enemy** can clear that condition and
force combat, but a solo run can become lethal. A 97-Brave Counter can still fail, so absence of a
commit after one hit is a valid no-trigger control rather than a probe failure. Capture only the
bounded event needed and stop.

For LT40 synthetic-Reaction empty-slot tests, do not deploy
`1784152701-synthetic-reaction-carrier443-fixture.png`. That obsolete fixture changed Rion's equipped
word from `442` to `443` but left the live reaction-set byte at `unit+0x96 = 0x08`, which is Counter's
native bit. The corrected fixture is
`work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png` (SHA-256
`415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`). Its audited live copies
change `unit+0x14: 442 -> 443` and `unit+0x96: 0x08 -> 0x04`, preserving unrelated bits. At startup,
verify `unit+0x1CE = 0` before acting. A later `442` candidate means native staging occurred; it is
not by itself a Counter commit or effect, and changing the Auto-battle target does not repair an
inconsistent fixture.

The corrected LT40 log-only gate passed in
`work/1784157661-lt40-dcl-synthetic-reaction-logonly-live-fifth.log` (SHA-256
`378C088A90B099914313F6DD0A1E07AB1C54358F6B66248E890DB55AE6D21103`). After Rion's Throw
Shuriken, the first pre-selector reported `candidates=[]`. Wenyld's nonlethal Attack then armed the
carrier-443 mailbox, and the following pre-selector reported `candidates=[]`,
`producer=synthetic-would-stage`, and state `4`. Validate this fixture class with
`--require-startup-owner --expected-reaction-set-hex 00000400`; the analyzer must report one valid
startup owner, one accepted gate, one would-stage intent, and zero staging/materialization/commits.
Do not substitute `work/1784104894-fft-autoenhanced-snapshot.png`: that older LT23 fixture equips
native Counter `442`, so its post-hit log shows `candidates=[16:442]` and `producer=none` instead of
testing synthetic owner `443`.

The first corrected live-write gate is a negative control, not a successful rewrite. Carrier `443`
staged and reached pass 2, then materialized natively as `actionType=0`, `actionId=443`, target mode
`5`, targeting its owner. The old exact-original guard `1/0` therefore reported
`rewrite=blocked-original`, performed zero order writes, and consumed no managed cadence. The
historical profile `work/1784159380-battle-runtime-settings.synthetic-reaction-live-rewrite.json`
was used only to test the now-refuted post-selector transformation and must not be redeployed. The negative-control log is
`work/1784159016-lt40-dcl-synthetic-reaction-live-write-blocked-original.log`.

The follow-up bounded gate proved the action rewrite and exposed a separate target authority. In
`work/1784160120-lt40-dcl-synthetic-reaction-live-rewrite-target-mismatch.log` (SHA-256
`C9BFF4EF2E82CD4E486B24AEA17CD28BF9C566DFD841A1487EDE3DE32F3F7F3F`), Wenyld at source index
`6` supplied the first surviving hit. Carrier `443` staged once, committed at pass 2, matched native
`0/443`, and materialized as rewritten `1/0` with `targetMode=5`, `targetIdx=6`, and
`rewriteWrites=1`. The managed commit consumed cadence exactly once. Nevertheless state `0x2C`
reported `targets=[16]`, and the calculator resolved Rion targeting himself. Treat order target
`+0x0B/+0x0C/+0x0E/+0x10` as a non-authoritative intermediate for generic carrier `443`; do not run
another source-retarget gate until the later target representation has been identified offline.
The full result checkpoint is
`work/1784160217-lt40-dcl-synthetic-reaction-live-rewrite-result.md`.

The follow-up pre-target-build gate refuted the assumption that `0x2831BD` is common to all accepted
Reaction ids. In `work/1784161607-lt40-dcl-synthetic-reaction-pre-target-build-live.log` (SHA-256
`E1692309AD06B0EA3EB47C3FAF8AFB63E9CF299B75F6DCF5F06D6665832A9DD0`), owner/delivery `443`
staged and committed, but the correctly installed `0x2831BD` hook produced zero events; state `0x2C`
delivered untouched `443` to Rion himself. Static control flow confirms generic `443` jumps from
`0x283003` to common finalization `0x2831CC`, skipping `0x2831BD/0x2831C0`. Do not configure generic
`443` as an order-rewrite or special-materialization delivery id.

The first bounded owner/delivery protocol separates identities: equipped owner `443` retains the isolated fixture
and taxonomy rule, while the producer stages native delivery `442`, whose Counter branch already has
proven `1/0` source-target semantics. Use profile
`work/1784162789-battle-runtime-settings.synthetic-reaction-owner443-delivery442-live.json` and plan
`work/1784162790-lt40-dcl-synthetic-reaction-owner443-delivery442-live-plan.md`. Require
`syntheticDelivery=owned`, one agreeing pass-2 `442` commit, one managed
`carrier=443 delivery=442` cadence commit, and per-strike state-`0x2C` targets equal to the incoming
source. Rion is a Dual Wield Ninja, so the verified fixture is expected to emit two effect rows for
that single accepted Reaction transaction. No order rewrite is enabled in this protocol.

The first owner-`443` / delivery-`442` run is a validation rejection, not a failed hook. Its archived
log is `work/1784163675-lt40-dcl-synthetic-reaction-owner443-delivery442-deviated-live.log`
(SHA-256 `35396781651172F02752AC91E006D859C4D95FD246FC684796E9EB8DA4782B43`). Wenyld at source
index `6` hit from range, the producer staged `442` once for Rion, and the selector consumed it, but
no `0x2831BD` materialization or pass-2 commit followed. Candidate consumption precedes two native
special-delivery validators; Counter cannot be assumed to accept a source outside its basic-attack
reach. The next Chocobo action armed a new request, but the historical profile's one-write cap
correctly prevented a second write. Do not redeploy the `1784162789` profile as a positive gate.

The first validation follow-up profile
`work/1784164245-battle-runtime-settings.synthetic-reaction-owner443-delivery442-validation-live.json`
is historical and must not be redeployed. Its archived log is
`work/1784165727-lt40-dcl-synthetic-reaction-adjacent-accepted-wenyld-miss-live.log` (SHA-256
`4E4F7F723E454CC3B98883A57E64848E1BECE780963155DC9C3875E76311F067`). Wenyld's chosen basic
Attack produced no HP event, so no distant request armed. Choco Beak armed the only request; adjacent
delivery `442` passed final result RVA `0x28315C`, materialized native `1/0` toward source `0`,
committed cadence once, and emitted exactly two source-target state-`0x2C` rows. This is the positive
owner/delivery vertical slice. It does not classify the distant rejection.

Static dispatch corrects the earlier result-site map. Counter `442` shares typed-family result RVA
`0x283019` with ids `435/436/437`; Bonecrusher `434` alone uses the separate typed result RVA
`0x283148`. All surviving special paths converge on final result RVA `0x28315C`. Therefore every
validation build must install **three** hooks: `typed-family`, `typed-bonecrusher`, and `final`.

The completed tri-validator profile is
`work/1784166334-battle-runtime-settings.synthetic-reaction-owner443-delivery442-trivalidator-live.json`
with plan
`work/1784166336-lt40-dcl-synthetic-reaction-owner443-delivery442-trivalidator-live-plan.md`. It
forced hit chance `100`/roll `0` through the validated all-zero evade baseline and fixed probe damage
to one point, so Wenyld and Choco Beak both armed nonlethal requests in one run. The archived capture
is `work/1784167467-lt40-dcl-synthetic-reaction-owner443-delivery442-trivalidator-live.log`. Source
`6` returned `-2` at `typed-family` with mailbox `2->6` and no downstream delivery. Source `0`
returned zero at `typed-family` and `final`, then produced one owned materialization, one
native/managed commit, and exactly two source-target Dual Wield effects. The two rows are one
Reaction transaction, not duplicate Reactions. This distinction between a consumed candidate and
an accepted delivery is a reusable failure classifier.

The tri-validator capture is deliberately nonlethal: its `DclDamageFormula` is `1`, and its log
keeps Rion alive. If a watched battle shows Choco Beak killing Rion, that visual run is not this
capture and cannot corroborate its `442` rows. Never translate an internal id into a claimed visible
animation from log evidence alone; record visible action order separately and reconcile HP, source,
target, and stop point before calling the two views the same run. A lethal staged result must not arm
the `successful-hit-survivor` producer.

The LT41H capture `work/1784201674-lt41h-dcl-dual-wield-safe-final-hook-live.log` proves the exact
Dual Wield repeat signature. The first outer-sweep result uses battle state `0x2A`; the second uses
`0x2F` with the same caster, action type, ability, payload, and target. This occurs for the synthetic
Counter and for an ordinary Attack. When debugging a missing second rewrite, inspect calc provenance
first: never infer hand or strike identity from HP delta, effect ordinal, elapsed time, or
`unit+0x1BB`. Run `python tools/analyze_dcl_native_repeat_provenance_live.py <log>` to classify the
pair. Both execution states must reach the managed numeric/status pipeline independently.

For a fixed-runtime regression, run
`python tools/analyze_dcl_native_repeat_provenance_live.py <log> --minimum-pairs 2 --expect-fixed`.
The gate compares each row with its own native pre-clamp debit; it deliberately does not require the
two native amounts to match. LT41I proves both rewrites for Counter and ordinary Attack in
`work/1784250813-lt41i-dcl-native-repeat-fixed-game.log`.

The verified 1280x768 LT41I targeting route starts from Rion's **Auto-battle > Attack Enemy** target
selection: press `E` three times, press `Z` once, click Wenyld at approximately `(1245,559)`, and
verify the lower-right pane says **Wenyld / Archer / 396 HP**. The camera pans; click the same unit at
approximately `(805,460)`, then confirm **Yes** with `Enter`. Coordinates are window-relative and
must be paired with the name/HP check rather than used blindly.

When Wenyld is occluded, Timothy is a faster mixed-weapon carrier target for a test that needs only
Rion's ordinary Dual Wield calculation. From the same 1280x720 Auto-battle target screen, click the
visible Timothy near `(1200,510)`, allow the camera pan, click him again near `(830,450)`, and confirm
**Yes** with `F`. Verify the bottom-right name before the second click. This substitution is not valid
for a protocol that specifically requires Wenyld's Archer action or source index. Clicking Timothy's
timeline portrait does not select him on the battlefield.

An active-weapon gate must inspect the repeat carrier and native staged result, not only battle
state. A completed mixed-weapon pair has positive native debit on both rows and routes `0/17` then
`1/18`. A `0x2F` row with native debit zero can retain or reinitialize `0/17`; classify it as a
canceled or non-completing follow-up, not as proof that the off-hand capture failed. Use
`tools/analyze_dcl_active_weapon_live.py`, which enforces this distinction and requires at least one
completed ordinary-owner pair.

The first shorter Dual Wield fixture, `work/1784171084-dcl-dual-wield-fast-ct-order-fixture.png`, is
**live-refuted** for Continue: editing Choco CT only in main/fturn aliases still loaded runtime CT `0`.
The source's attack aliases also carried `0`. The corrected fixture
`work/1784171803-dcl-dual-wield-fast-attack-ct-order-fixture.png` additionally changes Choco
`0 -> 100` in both attack aliases and **live-proven** makes Janus the next actor. A passive Rion Wait
still did not reproduce Choco Beak. The layered
`work/1784172418-dcl-dual-wield-fast-visible-fixture.png` then cleared Invisible correctly: the live
startup dump had effective `unit+0x63 = 0x20`, with mask `0x10` absent and Reraise preserved. Janus
nevertheless ended the turn without any calc or attack, so status/CT edits alone are a refuted
shortcut for this AI decision. Recreate **Auto-battle > Attack Enemy > Wenyld** and checkpoint the
autosave after Rion's actual Throw instead; this preserves the action history that preceded the
proven LT40 Wenyld/Janus sequence. The reusable CT builder is
`tools/build_fft_autosave_ct_fixture.py`; scoped `main:` and `attack:` edits are round-tripped, and
every delta outside requested `unit+0x41` bytes and recomputed member CRCs is rejected. The bounded
status-layer builder is `tools/build_fft_autosave_status_clear_fixture.py`.

### Unified job-free DCL sentinel profile

The v3-v7 unified sentinel profiles that enable `DclApproachEnabled` or any `DclFear*` control are
retired and must not be installed or used for DCL completion evidence. Stop-hit and every other
mid-route interruption are outside the DCL; position-triggered abilities may begin only after the
ordinary movement route has finished. Fear has no active specification and must not be investigated,
implemented, enabled, or live-tested. The safe disabled profile captured after this retirement is
`work/1784466102-battle-runtime-settings.dcl-no-approach-no-fear.json`.

The offline-composed unified technical profile is
`work/1784168025-battle-runtime-settings.dcl-unified-sentinel.json`, generated from
`work/1784168025-dcl-runtime-composition-manifest.json`. Its mandatory pair contract is
`work/1784168025-dcl-unified-sentinel-runtime-data-pair.json`; validate both composition freshness
and runtime/data pairing before any deployment. The paired NXD is an isolated `work/` artifact and
must replace only the probe deployment copy, never the repository's production data-mod payload.

This profile enables physical/magic/result/status/KO/multistrike/Reaction mechanisms together. It is
not final balance and contains no job policy. Do not deploy it ad hoc: first create a bounded live
matrix, back up every touched Reloaded/game artifact, and name the exact fixture and stop rule. Any
useful navigation, fixture, log-classification, or restoration shortcut discovered during that
matrix may be added here.

For an exact manual action on this fixture, advance Rion toward the visible enemy cluster using the
furthest fully blue tile and end each approach turn with **Wait**. After movement, the menu cursor
rests on **Abilities**, so **Wait** is one `Down`, not two. Once adjacent, use **Abilities > Attack**
and click the enemy; the attack removes Invisible. Herkyna the Goblin immediately exposes a native
Counter carrier in the forecast and is a deterministic LT39 materialization target. In the verified
run, its accepted Counter order was type `1`, payload `0`, target mode `5`, and targeted the incoming
source before actor construction.

Movement to the unit's current tile consumes Move/Teleport but does not expose **Reset Move**. Before
confirming a destination, require that the selected white tile is visibly different from the unit's
current outlined tile. If the proposed tile is not blue, the game may still highlight it but rejects
confirmation with “Select a tile within movement range”; choose a fully blue tile instead.

The helper refuses Snapshot/Restore while the game is running, creates a timestamped backup before
Restore, and verifies the restored SHA-256. A snapshot is an experiment fixture: name the intended
battle state in the test journal and never assume that the newest autosave still represents it.

Use **Load > Manual Saves > 05** only to establish a new fixture or when a protocol explicitly needs
the manual-save baseline. Once the desired live state has been reached, snapshot it and use the fast
Continue path for all A/B repetitions.

To create a combat fixture from save 05, enter the desired encounter, deploy only the unit(s) needed
by the protocol, wait until the exact actionable turn is visible, close the game with `Alt+F4`, and
snapshot `autoenhanced.png` while the process is stopped. A subsequent atomic **Continue** load
returns directly to that actionable turn; the formation sequence and preceding AI turns do not need
to be repeated. This fixture-construction route was verified with Josephine alone at the start of her
turn and returned to the same command menu in 28.12 seconds from the Enhanced/Classic selector.

The former Josephine/Arthur Fear fixtures are historical evidence only. Do not restore them for new
tests and do not schedule any remaining Fear authorization repetitions.

The game also updates `autoenhanced.png` at battle turn boundaries. If fixture preparation has
already survived the required AI turns, close at the next actionable ally turn and snapshot that
newer container. This can remove the remaining AI wait from every repetition. Verify the resumed
unit, HP, position, and unused actions after the first Continue load before treating it as the new
canonical fixture.
Closing at the start of an enemy turn is not sufficient: a live attempt closed with Janus active at
CT 100, but `autoenhanced.png` remained byte-identical to the pre-action source. Treat an unchanged
hash and timestamp as proof that no new checkpoint was written.

During Teleport/movement selection, a mouse click is a hover-driven tile selection. Keep the feather
on the intended blue tile and press `F` immediately; moving the pointer to the title bar before `F`
can discard the tile and confirm a different map position. If the camera pans after the click, use
the newly visible grid to click the final tile and confirm without moving the pointer.

Enhanced basic **Attack** target cycling excludes allied units. An ally can appear in the information
pane under mouse hover, but `F` does not accept it as a legal Attack target and keyboard cycling skips
to an enemy. Do not plan Counter probes around friendly-fire basic attacks.

Charged actions are not complete when the ability becomes disabled and its label first appears in
the timeline. Choose **Wait** to end the caster's turn, allow the timeline entry to resolve, and
require a visible execution consequence (for example the MP debit and the target's HP/KO state)
before closing the game or collecting the runtime log.

After a fresh Windows boot, start Steam before launching FFT through Reloaded-II. At the Enhanced
selector, click the game title bar to establish focus and use a short irregular burst of `F`
presses; on the intro, use a short irregular burst of `Enter` presses to skip it. On the title menu,
**Continue** plus the same focused `F` burst is the fastest route into a prepared autosave. Verify
each transition visually because a single early input can be consumed before the menu is ready.

In the current Josephine/Fervor fixture, the first click on Arthur's sprite may only select the map
tile. Two distinct clicks near Arthur's head around `(570,356)` are reliable; the lower-right pane
must identify **Arthur**, **Summoner**, and the expected HP before `F` confirms the forecast. The
forecast must then name Arthur and show the expected status chance before accepting **Unit**.

At 1280x768, key `1` cycles the battle display overlays. Treat it as a visibility aid, not a target
identity source. The lower-right information pane is authoritative: confirm the unit name, job, and
HP there before accepting any mouse-selected target. A mouse hover can override the keyboard cursor
in menus and facing selection; click the title bar around `(700,12)`, refresh, and only then press
`F` when the keyboard-selected entry must win. Do not apply this title-bar step while confirming a
movement tile, because movement selection intentionally follows the hovered tile.

From the Item list with Potion selected, one `Up` wraps to Phoenix Down. From Black Magicks with
Fire selected, two `Up` inputs select Death (`Fire -> Flare -> Death`). Enhanced charged-spell
targeting then presents **Unit / Tile / Cancel**; the default is **Unit**, so press `F` once after
verifying the target. These are verified shortcuts for the current fixture and should be
revalidated if the equipped command list changes.

For bounded Interrupt tests, Retry, Continue, or a runtime-settings hot reload resets the in-memory
write counter. Never use any of them between the success attempt and the write-cap attempt. A
condition-false action does not consume the budget and is a safe intervening control.
`DclInterruptMaxWrites` is nonnegative and `0` means unlimited; the temporary pre-clamp ASM harness
has a separate `PreClampDamageRewriteMaxWrites` range of `1..32`.

Static `PreClampDamageRewrite*` filters and limits are embedded when the ASM hook is installed; a
settings hot reload does not rebuild that hook. `PreClampManagedCallbackForcedDebit`, by contrast,
is read dynamically and can be hot-reloaded as a temporary survival harness. The late HP rewriter
skips a target the engine already considers dead, so a post-KO rewrite cannot be used to rescue an
instant-KO test. Record these harnesses explicitly and never treat their combat magnitudes as
production evidence.

## Safe close path

Preferred route when no save is required:

1. Use `Esc`/`Backspace` until the in-game main menu is visible.
2. Open **Options**.
3. Select **Exit Game**.
4. Confirm the exit prompt.
5. Verify visually that the game window closed; leave Reloaded-II open for the next run.

Fallback: after evidence has been captured and no save is wanted, `Alt+F4` closes FFT Enhanced immediately even from a battle, without an in-game confirmation prompt.

## Reaction validation probe safety

Never deploy a final-validator `AsmHook` beginning at RVA `0x28315C`. Reloaded steals at least seven
bytes, so that placement covers the native restore entry at `0x283160`. Distant Counter rejection
jumps directly from the typed-family path to `0x283160` and can then enter the middle of the detour,
terminating the game with `0xC0000005` at that exact RVA.

The safe binding starts at validator call RVA `0x283157`, uses `ExecuteAfter`, and sets an exact hook
length of `7` for `call + test`. Its relocated span ends at `0x28315E`, preserving both the
conditional branch and the external restore target. Before any live Reaction probe, require the
smoke-test invariant
`DCL_REACTION_FINAL_VALIDATION_HOOK_RVA + DCL_REACTION_FINAL_VALIDATION_HOOK_LENGTH <= 0x283160`.
The typed-family hooks at `0x283019` and `0x283148` must use the three-argument `CreateAsmHook`
overload. Do not pass an explicit length of zero: Reloaded treats it as a zero-byte memory-permission
request and refuses to install the hook.

### Automation privilege boundary

The Reloaded-launched game runs at an integrity level that rejects ordinary desktop `SendKeys`,
mouse injection, and non-privileged `SendInput` from the workspace process. Screen capture still
works, which can make failed input look deceptively successful. Require the privileged Computer Use
channel for unattended navigation; if it cannot initialize, do not send blind fallback sequences.
`Alt+F4` is the recovery path when no battle/test has begun and the game window must be closed
without saving.

If the privileged control connection fails before exposing window controls, retry its documented
connection procedure once. Treat a repeated failure as a control-channel problem, not evidence of a
Reloaded/game regression. Direct launch remains useful for hook-install smokes, but unattended menu
or battle tests must wait for privileged control; never replace it with blind `SendKeys` or custom
input injection.

## Safety rules for unattended tests

- Do not inspect, monitor, diagnose, or terminate Codex/Windows host processes as part of DCL work.
  Host stability is outside this runbook; stay on the game, enabled-mod configuration, and test log.
- Perform the bounded game interaction without unrelated system diagnostics. Close the game after
  the decisive action, then inspect/copy only the Reloaded and Generic Chronicle logs.
- Begin a new live-test fixture from **Load > Manual Saves > 05** unless its protocol names another save. Repeated A/B runs should restore the protocol's exact autosave snapshot and use the fast Continue path.
- Never choose **Save** unless the test protocol explicitly requires it.
- Capture a screenshot before every irreversible transition (starting a battle, overwriting a save, returning to title, or exiting).
- Prefer `Esc`/`Backspace` to unwind uncertain menus.
- Verify the screen after every navigation burst; do not send long blind key sequences.
- Close only after the test evidence and runtime logs have been captured.

## Continuous maintenance rule

This is a living operational runbook. Whenever an unattended test reveals a control, shortcut, menu route, visual cue, recovery procedure, timing detail, or other technique that makes future tests faster, safer, or more repeatable, Codex may add it here. Keep entries concise and distinguish verified behavior from hypotheses or still-pending checks.
