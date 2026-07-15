# DCL KO/revive lifecycle offline checkpoint

Date: 2026-07-13

## Question

Separate the DCL's safe handling of lethal damage, revive actions, offensive instant KO, Undead,
and Crystal without treating protected status byte 0 as an ordinary status surface.

## Evidence

- Installed Enhanced executable: Steam build `23901820`, SHA-256
  `841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.
- `work/1783985353-runtime-hook-anchor-audit.md` passes all 18 runtime anchors.
- The corrected disassembly is `work/1783985524-ko-lifecycle-disassembly-analysis.md`; all 11 KO
  landmarks pass their expected-byte guards on the installed executable.
- Native HP apply computes `new HP = current HP + staged credit - staged debit`, clamps it, then
  runs the status/lifecycle tail.
- Formula-owned lethal staged debit producing coherent KO is already live-proven.
- Direct HP=0 / Dead-bit writes are refuted as a death mechanism.
- The KO-target gate consumes native staged flags at `unit+0x1DB/+0x1DC`; Reraise completing through
  the native tail is live-proven. Raise/Arise/Revive preservation on the same path is Strong/static
  and remains a focused live gate.

## Mechanism result

1. Generic `DclStatusRule` permits byte 0 only for proven-safe Undead `0x10`.
2. Raise/Arise/Revive/Squeal retain the native lifecycle. A DCL revive formula may replace only the
   staged HP credit; it never clears Dead directly.
3. Offensive instant KO uses `DclInstantKoRule`. The rule applies the DCL 3d6 resistance contest,
   honors native KO immunity, and on success writes a debit equal to current HP plus same-hit credit.
   Native HP apply therefore owns the actual death.
4. A resisted/immune rule either preserves authored ordinary damage or zeros it for pure-Death
   actions. It never writes Dead.
5. The validator requires an explicit `NativeKoSuppressedByData=true` acknowledgement. The optional
   `tools/build_neuter_data.py --dcl-instant-ko-neuter <ids>` route changes only the selected native
   Dead riders to a harmless formula-0x08/X=Y=1/no-status staging path. It is not enabled by default;
   id `30` isolates Death for the first vertical slice.
6. Crystal/Bequeath Bacon is excluded. Crystalization is a corpse/campaign lifecycle, not lethal HP.

## Coverage change

The 294-row status manifest now reports:

- 195 surface-ready rows;
- 4 native-revive-lifecycle rows;
- 9 instant-KO rows with mechanism complete but data/formula authoring pending;
- 1 Crystal lifecycle mechanism gap;
- 2 Invite campaign gaps;
- 44 authoring rows;
- 39 unresolved status-nature decisions.

Current manifest: `work/1783986363-dcl-status-policy.csv` and `.md`.

## Offline validation

- Fresh .NET build `DclInstantKoValidate2`: zero warnings, zero errors.
- Full formula runtime smoke suite: pass.
- `tools/test_neuter_data.py`: pass, including temporary-SQLite verification of all nine native-rider
  overrides and explicit exclusion of Crystal.
- `tools/test_dcl_status_policy.py`: pass.

## Remaining live gates

- Rewrite Raise/Arise staged credit while verifying HP, Dead removal, turn ownership, animation, and
  forecast stay coherent.
- One pure Death rule: forced resist and forced failure-to-resist after the data neutralizer is paired
  with an authored profile; confirm no zombie state and no native KO leak on resist.
- Do not run these while the unmodified Enhanced game still black-screens before the menu.
