# Basic Attack into First Strike clean result

## Manual result

- Cloud used basic Attack against Ninja.
- Preview chance: 100%.
- Preview damage into Ninja: 403.
- First Strike triggered before Cloud's attack.
- Cloud received two UI damage numbers: 396 and 396.
- Cloud ended at 0/428 HP.
- Ninja received no damage and remained 277/277 HP.
- The original Cloud attack did not resolve after the reaction.
- Active unit after the sequence was Ninja.
- No critical/status/special effect was reported.

## Evidence files

- Raw log: `work/1782732390-basic-first-strike-clean-log.txt`
- Analyzer report: `work/1782732390-basic-first-strike-clean-report.md`

## Runtime sequence

1. The original incoming attack is visible as a target-cache/pre-apply candidate on Ninja:
   - target: `0x141855CE0/id=0x80` (Ninja)
   - source refs: `0x141855EE0/id=0x32` (Cloud)
   - action id: `0` (basic Attack / implicit weapon)
   - staged damage: `403`

2. That original attack cache is cleared before any HP loss on Ninja:
   - Ninja HP remains `277/277`.
   - The target-cache source refs prove the incoming source, but this branch is interrupted/cancelled.

3. The First Strike reaction then applies damage to Cloud:
   - target: `0x141855EE0/id=0x32` (Cloud)
   - caster/source: `0x141855CE0/id=0x80` (Ninja)
   - action id: `0` (basic Attack / implicit weapon)
   - staged damage: `396`

4. The first reaction HP event is resolved by actor context:
   - `PRECLAMP-ACTOR-CTX event=3`
   - `target=0x141855EE0/id=0x32`
   - `caster=0x141855CE0/id=0x80`
   - `actionId=0`
   - HP transition: `428 -> 32`

5. The lethal follow-up/duplicate reaction event reaches Cloud at 32 HP:
   - staged damage remains `396`
   - final HP transition: `32 -> 0`
   - KO status bit `+0x61 = 0x20` appears after the lethal application.

## Interpretation

This capture supports using pre-clamp actor context as a primary source for reaction damage attribution. For First Strike, the reaction does not need CT-based inference: the stack/actor context identifies Ninja as caster and Cloud as target on the HP-applying reaction branch.

This capture also shows that the interrupted original attack can still create a target-cache damage candidate for the intended target. Runtime logic must not treat every target-cache damage candidate as a final HP application. For cancelled/interrupted actions, the authoritative final target list is still the HP event/pre-clamp branch.

## Open questions

- Why the UI shows two 396 damage numbers while the first HP transition is `428 -> 32` and the lethal transition is `32 -> 0`.
- Whether First Strike always surfaces as action id `0` with the reacting unit as caster, or whether weapon/dual-wield variants create additional actor/hit structure.
- Whether other reaction families, especially Hamedo/counter-like reactions, use the same pre-clamp actor-context layout.
