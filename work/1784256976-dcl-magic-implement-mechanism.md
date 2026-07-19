# DCL magic-implement mechanism closure

## Gap

The job-free unified profile implemented the shared MA/Faith/element/Zodiac pipeline and weapon
bolts, but it did not implement the equipment contract in `deep-combat-layer/14`: Rod amplifies
offensive magic and Staff amplifies healing/support. It also used weapon Power as the bolt's spell
tier, conflating two independent dials.

## Implemented mechanism

- `dcl.offensiveMagicMod` is the larger equipped Rod Power, never a sum.
- `dcl.supportMagicMod` is the larger equipped Staff Power, never a sum.
- Named damage magic uses `raw MA + offensiveMagicMod`.
- Healing uses `raw MA + supportMagicMod`.
- A weapon bolt uses the active native weapon. Rod Power increases its MA; Staff does not, so the
  Staff remains useful but weaker as specified.
- `const.dclMagicBoltSpellPower` is a separate mechanism fixture. Final per-item modifier and bolt
  tier values remain balance authoring, not a technical claim.
- Either equipment hand is recognized for named magic. Synthetic two-implement states select the
  stronger applicable modifier and do not stack.

## Offline falsifiers

The C# suite proves:

1. Rod increases Fire from either hand.
2. Two Rods equal the stronger Rod alone.
3. Staff increases Cure; Rod does not.
4. Rod bolt and Staff bolt share a bolt tier, while only Rod adds its implement modifier.
5. Mixed Knife/Rod native repeats classify only the active Rod as a magic bolt.
6. The same assertions hold when loading the actually composed integration scaffold.

Composition and C# smoke tests pass. The remaining magic live work is integration regression of
preview, execution, affinity, Magic Evade, absorb/AoE, and presentation; no additional probe is
needed to define the implement formula surface. Reflect is not defined by the job-free DCL documents
and appears only in job-side drafts/legacy material, so it is deliberately excluded from this track.
