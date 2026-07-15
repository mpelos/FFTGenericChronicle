# DCL reaction carriers checkpoint

## Scope

This checkpoint continues the offline reaction-mechanism investigation after the native-effect
checkpoint. It separates reaction selection, carrier delivery, final DCL effect, provenance, and
cadence. Timeless engine facts are promoted into `docs/modding/`.

## Offline results

- Vigilance `426` is selected from `unit+0x94 & 0x08`. Its visible real-code dispatcher simulates on
  copied state, stages the reaction and incoming action id, restores live globals, and returns a
  boolean. Historical PSX/PSP source identifies the same effect slot as Caution/Vigilance and the
  original effect as Defending. The final transient DCL Dodge bonus therefore requires a managed
  pre-defense-roll modifier and must suppress the native Defending delivery.
- Blank Hex Ward `443` has no native reaction-bit producer. Pass 2 can accept it when externally
  staged, but the generic route targets the reactor. A final source-targeted debuff requires a
  custom landed-hit producer, guarded target-list inversion to the incoming source, managed Blind or
  Brave reduction, and cadence commit. The ordinary ability-data NXD cannot author id `443`.
- Counter Tackle `436` is selected from `unit+0x95 & 0x02` and already emits a source-validated Rush
  `147` order. Rush formula `0x37` retains knockback, while the active neuter data only reduces its
  magnitude inputs. The final Squire rule needs trigger and cadence filtering, not a synthesized
  shove.
- Critical: Recover HP `431` is selected from `unit+0x95 & 0x40` through the native Brave gate. Its
  effect credits `MaxHP-currentHP`, so the native payoff is a full heal. Grit remains design-open;
  if its minor clutch payoff is healing, carrier `431` can be retained only with managed replacement
  of the credited amount.

Reproducible reports:

- `work/1783999597-dcl-vigilance-analysis.md`
- `work/1783999599-dcl-hex-ward-analysis.md`
- `work/1783999833-dcl-counter-tackle-analysis.md`
- `work/1784000025-dcl-critical-recover-hp-analysis.md`
- `work/1784000177-dcl-reaction-capabilities.md`
- `work/1784000177-dcl-reaction-implementation-manifest.md`
- `work/1784000210-runtime-hook-anchor-audit.md`

## Verification

- All four new static analyzers compile.
- Reaction capability validation passes all 16 final reactions.
- Runtime anchor audit passes all 25 anchors.
- Release codemod build succeeds with zero warnings and zero errors.
- Formula/runtime smoke tests pass.
- `git diff --check` passes; the existing line-ending warning for
  `work/runtime_formula_context.md` is unrelated.

## Runtime state and live boundary

- The user independently launched the game normally by selecting the Reloaded profile. This proves
  the game, profile, and Reloaded path are healthy; the earlier inability to drive menus belongs to
  the privileged Computer Use channel.
- Behavior testing remains ordered as LT23 reaction-commit ownership, LT28 outer/nested calc
  provenance including Rend, LT26 Auto-Potion consumption, and LT27 native weapon LoS.
- No blind keyboard injection is authorized as a substitute for visible control.

## Next offline work

The remaining native reaction records are mostly mapped. The next high-value offline boundary is a
guarded reaction-producer/effect architecture: execution-only provenance, reservation versus commit,
source targeting, and amount/status replacement. It must stay disabled by default until LT23 and
LT28 prove which runtime pass owns mutation and persistent cadence.
