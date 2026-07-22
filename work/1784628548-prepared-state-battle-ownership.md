# Prepared-state battle ownership

## Scope

The offline pass audited the remaining canonical prepared-state gap for Overwatch,
Cover/Bodyguard, Bulwark, and QuickLock. It did not restore Fear, Approach, or any mid-movement
trigger. The live-control attempt remains recorded separately in
`1784627199-canonical-admission-live-control-blocked.md`; the computer-control runtime failed during
its own initialization before opening or interacting with the game.

## Decisions

- Generic Immediate status reprojection was not invented. The canonical DCL assigns each
  Immediate state transition to a mechanics-specific projection owner; Injury/Posture and forced
  movement already have those owners, while arbitrary status Riders do not.
- The state registry is the only persistent lifetime and remaining-use owner for prepared states.
  The prepared runtime no longer stores duplicate mutable Overwatch or protection controllers.
- The canonical battle owns one prepared runtime. Confirmed physical, direct-magic, Area, and
  standalone status application routes through the battle owner when one exists.
- Prepared applications validate their duration clock, initial finite-use agreement, and executable
  Overwatch effect Action identity before the state registry mutates.
- Trigger evaluation reconstructs its short-lived Overwatch or protection controller from the
  current registry instance and current remaining-use count. Replacement, removal, KO cleanup,
  expiry, and checkpoint restore therefore cannot strand a second controller.
- Movement with `MovementSettled=false` exits before dependency validation, use consumption, or
  cancellation. Only the settled final tile can reach revalidation.

## Sentinels

The smoke path uses a battle-owned registry and prepared runtime. It proves that:

1. an Overwatch application whose payload and duration disagree fails before registry mutation;
2. a valid two-use Overwatch state consumes exactly one persistent use after a settled trigger;
3. a newly constructed prepared runtime can continue from that remaining registry use, proving
   that no hidden controller is required;
4. a failed settled revalidation cancels the exact state;
5. an unsettled movement event neither fires nor cancels the state, even when every later
   dependency flag is invalid.

## Verification

- `dotnet build ...smoketests.csproj -c PreparedStateOwnership5 --no-restore`
  - succeeded with zero warnings and zero errors;
- `dotnet .../PreparedStateOwnership5/...smoketests.dll --test-dcl-injury-movement`
  - passed direct forecast/execution, physical execution, Area forecast, multi-Strike Area
    execution, single-Strike Area execution, and the prepared-state sentinels.

## Remaining native boundary

No prepared effect is connected to a per-tile callback. Production binding still requires the
post-route movement-complete event, final trajectory verdict, Action reservation, redirect/result
carrier, expiry callback, and visible-link/presentation carrier. Overwatch firing must reserve and
execute its effect Action sequentially under the battle owner; it must not batch unordered trigger
effects or run before the final tile is authoritative.
