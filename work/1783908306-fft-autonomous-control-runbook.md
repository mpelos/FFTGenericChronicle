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

Do not set `AppArguments` to `/deactiveplay 1` in an attempt to skip the Enhanced/Classic selector.
Current-build static analysis proves that the executable parses this switch as a boolean, and a
live launch proves that it only disables the alternate play-mode button; the selector remains.

### Version 1.5.1 launch anomaly (operationally cleared)

On the first unattended run after the 1.5.1 update, **Enhanced > Start Game** entered a black screen with the feather cursor and did not advance after two minutes. Sending `Enter` or `F` from that screen returned to the version selector; `Space` did not advance it. The same behavior reproduced when the executable was launched directly and from Steam without Reloaded injection, so the symptom was not sufficient evidence of a probe or hook failure.

On 2026-07-13 the user launched the game through Reloaded-II, selected the Reloaded option, and the
game opened normally. Live testing is operational again. If the black screen recurs, close with
`Alt+F4`, verify the process state, and retry through Reloaded-II before changing probe code or
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
- Menu hover can override a keyboard selection. Before confirming a row with `F`/`Enter`, move the
  feather to the title bar; otherwise a stale hover can activate the adjacent row. On the Enhanced
  title menu, direct clicks are safest, and the visible row coordinate must be used without an
  assumed title-bar offset.
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
   `Enter`, wait 1.6 seconds, and click **Continue** directly. At 1280x720, Computer Use input
   `(640, 578)` maps to the visible Continue row around screen y=606. This prevents tool latency
   from leaving the title menu idle long enough to restart the opening movie.
6. Allow roughly 22 seconds for the saved battle/map state to finish loading before inspecting it.

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

The game also updates `autoenhanced.png` at battle turn boundaries. If fixture preparation has
already survived the required AI turns, close at the next actionable ally turn and snapshot that
newer container. This can remove the remaining AI wait from every repetition. Verify the resumed
unit, HP, position, and unused actions after the first Continue load before treating it as the new
canonical fixture.

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

## Safe close path

Preferred route when no save is required:

1. Use `Esc`/`Backspace` until the in-game main menu is visible.
2. Open **Options**.
3. Select **Exit Game**.
4. Confirm the exit prompt.
5. Verify that `FFT_enhanced.exe` has stopped; leave Reloaded-II open for the next run.

Fallback: after evidence has been captured and no save is wanted, `Alt+F4` closes FFT Enhanced immediately even from a battle, without an in-game confirmation prompt. Always follow it with an `FFT_enhanced` process check.

### Automation privilege boundary

The Reloaded-launched game runs at an integrity level that rejects ordinary desktop `SendKeys`,
mouse injection, and non-privileged `SendInput` from the workspace process. Screen capture still
works, which can make failed input look deceptively successful. Require the privileged Computer Use
channel for unattended navigation; if it cannot initialize, do not send blind fallback sequences.
`Process.CloseMainWindow()` has closed the version-selector instance cleanly without saving and is a
safe recovery when no battle/test has begun. Always verify that `FFT_enhanced` has stopped.

If the privileged control connection fails before exposing window controls, retry its documented
connection procedure once. Treat a repeated failure as a control-channel problem, not evidence of a
Reloaded/game regression. Direct launch remains useful for hook-install smokes, but unattended menu
or battle tests must wait for privileged control; never replace it with blind `SendKeys` or custom
input injection.

## Safety rules for unattended tests

- Begin a new live-test fixture from **Load > Manual Saves > 05** unless its protocol names another save. Repeated A/B runs should restore the protocol's exact autosave snapshot and use the fast Continue path.
- Never choose **Save** unless the test protocol explicitly requires it.
- Capture a screenshot before every irreversible transition (starting a battle, overwriting a save, returning to title, or exiting).
- Prefer `Esc`/`Backspace` to unwind uncertain menus.
- Verify the screen after every navigation burst; do not send long blind key sequences.
- Close only after the test evidence and runtime logs have been captured.

## Continuous maintenance rule

This is a living operational runbook. Whenever an unattended test reveals a control, shortcut, menu route, visual cue, recovery procedure, timing detail, or other technique that makes future tests faster, safer, or more repeatable, Codex may add it here. Keep entries concise and distinguish verified behavior from hypotheses or still-pending checks.
