# DCL native reaction effects checkpoint

## Scope

This checkpoint continues the reaction-mechanism investigation after LT28 calc-provenance
instrumentation. It records only the journey state; timeless facts are promoted into `docs/modding/`.

## Offline results

- Mana Shield `445` is no longer an unknown native bundle. The current dispatcher moves the complete
  staged HP debit into staged MP debit whenever current MP is merely nonzero, caps the debit at 999,
  clears HP damage, and lets the normal MP apply clamp to zero. One MP can therefore prevent the whole
  hit. The final DCL needs an authored ratio/floor plus coherent result-flag presentation.
- Nature's Wrath `437` computes terrain from the reactor's own tile and maps the 64 terrain ids through
  RVA `0x67F750` to ordinary Geomancy payloads `126..137`. Those payloads carry their ordinary status
  riders, so the final no-rider reaction requires provenance-aware status suppression; global data
  removal would also break active Geomancy.
- The visible real-code Earplugs `448` branch is formula `0x2A`, exactly Speechcraft actions
  `116..125`. Bardsong `86..92` uses `0x1C` and Dance `93..99` uses `0x1D`; neither enters this branch.
  A VM-internal path remains possible, otherwise the final narrow family needs a synthesized producer.
- Magick Counter `435` copies the exact incoming `orderRecord+2` ability id through global RVA
  `0x7B0778` and reactor `unit+0x1E6`, then emits it as a type-`0x0B` order. It cannot serve unchanged
  as the final basic Rod bolt; Counter `442`'s type-`1`, payload-`0` order is the safer carrier shape.
- Regenerator `428` has a mapped real-code HP-damage trigger and carries staged damage as its payload.
  The branch has no survivor check, so only lethal-hit rejection and native Regen delivery remain live.
- Current-build address cleanup corrected the MP write, damage-math, Chicken threshold, wrong-RVA
  diagnostic, and four reaction Brave-roll anchors in the timeless docs.

Reproducible reports:

- `work/1783998435-dcl-mana-shield-analysis.md`
- `work/1783998780-dcl-natures-wrath-analysis.md`
- `work/1783998780-dcl-earplugs-analysis.md`
- `work/1783999007-dcl-magick-counter-analysis.md`
- `work/1783999096-dcl-regenerator-analysis.md`
- `work/1783998788-runtime-hook-anchor-audit.md`
- `work/1783998848-dcl-reaction-implementation-manifest.md`
- `work/1783998848-dcl-reaction-capabilities.md`

## Verification

- Native-effect analyzers pass every exact-byte/table check.
- Runtime anchor audit passes all 25 anchors.
- Release build succeeds with zero warnings and zero errors.
- Formula/runtime smoke tests pass.
- `git diff --check` reports no patch errors; the existing CRLF warning for
  `work/runtime_formula_context.md` remains unrelated.

## Installed safe state

- `FFT_enhanced.exe` is not running.
- Installed settings remain byte-identical to LT23 reaction-commit observe-only profile.
- Source and installed DLL SHA-256 are both
  `427CE7EFA10B6634A6CAECDDDE9E60E68277B72D864B7FD11ABF051EE3938202`.
- Reloaded-II remains open. The user independently confirmed that selecting Reloaded launches the game
  normally, so the game/profile path is healthy.

## Remaining live boundary

The privileged Computer Use channel still fails during kernel-asset initialization, before window
controls are exposed. Direct Reloaded launch is sufficient for hook-install smokes but not for blind
menu/battle behavior. The next behavior sequence remains LT23 commit ownership, LT28 outer/nested
calc provenance including Rend, then LT26 Auto-Potion consumption and LT27 LoS only where their
offline boundary still requires observation.

## Next offline work

Continue separating final reactions into trigger, native effect, synthesized effect, provenance, and
cadence. The highest-value static targets are Vigilance's pre-roll avoidance phase and the
status/target-inversion delivery needed by Hex Ward; Regenerator's remaining survivor question is live.
