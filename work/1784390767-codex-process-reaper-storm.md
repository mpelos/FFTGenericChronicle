# Codex process-reaper storm during live testing

## Symptom

The Windows host became progressively slow while Task Manager showed rapidly changing groups of
`taskkill.exe`, `conhost.exe`, and `git.exe`. `WmiPrvSE.exe` appeared repeatedly. A forced reboot was
required in the first occurrence. The game itself did not crash.

## Evidence

- The WMI operational log recorded 208 failed `Win32_Process` operations from 202 distinct,
  short-lived client PIDs between 12:52:35 and 12:53:19 local time.
- The Codex process registry contained 109 tracked command records, 96 from this long-running task;
  most lacked a current OS PID and several retained old PIDs for builds or Reloaded-II launches.
- Repository search contains no `taskkill` invocation. The only launch helper starts Reloaded-II.
- The process groups began appearing independently of FFT action execution and reappeared while
  read-only shell monitoring was active.

## Interpretation

The process storm belongs to the Codex command/process cleanup path, not the DCL runtime or FFT. The
short-lived clients repeatedly enumerate `Win32_Process`, causing WMI provider activation, while
console cleanup produces `taskkill.exe` and `conhost.exe`. Concurrent `git.exe` instances are
consistent with repository-state refresh during that cleanup cycle.

This incident is separate from the first Chicken-dispatcher slowdown. The old dispatcher defect
admitted non-Chicken units into Chicken planning; the corrected six-byte branch-preserving hook has
now completed two visible single-target Chicken applications without progressive in-game slowdown.

## Direct parent-process proof

A later capture while the processes were still alive removed the remaining attribution uncertainty:

- five `taskkill.exe` instances had direct parent `ChatGPT.exe` PID `16576`; representative command
  lines were `taskkill.exe /pid <pid> /t /f`;
- the simultaneous `git.exe` processes also had direct parent `ChatGPT.exe` PID `16576`; one command
  generated a no-index diff for `work/1784336258-movement-route-pre-dll.dll`, and another hashed a
  timestamped Markdown journal;
- a direct-child `powershell.exe` queried `Win32_Process` for the Codex process monitor;
- each `taskkill.exe` owned its own `conhost.exe` child.

The process storm is therefore **Proven** to originate in the Codex desktop Git/process-monitor path.
The earlier WMI rows are the enumeration side effect of that same tree.

The repository ignore policy now excludes only disposable local rollback binaries and automatic
pre-restore containers (`work/*-pre-dll.dll`, `work/*-pre-pdb.pdb`, and
`work/*-before-restore.png`). Their timestamped manifests, settings, logs, and intentional test
fixtures remain visible. This reduces unnecessary per-binary Git preview work without deleting
evidence or hiding the authoritative autosave snapshots.

A bounded cleanup experiment stopped five direct-child `taskkill.exe` processes. Six replacements
appeared within three seconds. Child termination is therefore refuted as a quiescence mechanism; the
active producer must be reset by restarting the Codex desktop application itself. A full Windows
reboot is unnecessary.

## Safety policy

- Minimize shell-command fan-out during live sessions.
- Stop live testing if `taskkill`/`conhost` counts grow across successive observations.
- Do not start a live test while any Codex-owned `taskkill.exe` remains alive.
- If direct children immediately respawn, restart the Codex desktop application and reopen this task;
  do not repeatedly kill the children.
- After restart, use one bounded process-tree check. Once it reports zero Codex-owned `taskkill`,
  issue no shell commands during the live capture; read the log only after the game is closed.
- Keep the game test bounded and close it after the decisive log row.
- Do not attribute host-wide process churn to the mod without a matching runtime or crash trace.
