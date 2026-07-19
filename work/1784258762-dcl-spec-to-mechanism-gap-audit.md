# DCL specification-to-mechanism gap audit

## Scope

This audit compares the structural combat requirements in `docs/deep-combat-layer/00` through
`14` with the generated whole-DCL implementation matrix. Job design, job assignments, final
numeric calibration, and `15-job-authoring.md` are excluded.

## Result

The 30-row matrix was not a complete inventory of the job-free DCL. It covered the central numeric
pipeline well, but grouped equipment and statuses too coarsely and therefore hid mechanisms that
have no integration yet. A green 30-row check proved only that those 30 declared rows were
well-formed; it did not prove that every structural requirement in the DCL had a row.

The following requirements need explicit ownership in the coverage report:

| Requirement | Current technical state | Correct status |
| --- | --- | --- |
| Critical/fumble windows and ranged defense coverage | Already implemented inside the physical contest: critical bypasses defense, fumble misses, missile disables Parry while Block remains available. | Keep inside the existing physical-contest row. |
| Weight to Dodge and Move | Weight aggregation, coarse Move penalty, fine Dodge penalty, and Dodge consumption exist in the formula fixture. The computed Move penalty is not applied; only a bounded `MovePoke` exists and has no live movement/UI proof. | `partial-live-gated` |
| Common unarmed damage | The specification requires thrust-like base PA minus an untrained-fist penalty. The current generic physical formula falls through to swing base with zero weapon power, and Martial Arts is job-derived. | `integration-missing` for common unarmed; job-derived Martial Arts remains excluded. |
| Weapon-family special routing | Core damage type/range/penetration routing exists. Flail's Parry/Block penalties and several non-job item special identities are sidecar notes rather than runtime rules. | `integration-missing` |
| Stop-hit trigger surface | Synthetic Reaction delivery can emit a native basic weapon order, but no movement/approach event producer detects entry into reach. Ability assignment remains job content and is excluded. | `integration-missing` |
| Physical Stun and Knockdown semantics | Per-action resistance and target-turn duration exist, and the native Don't Act/Don't Move flags are available. Source-specific physical duration/animation/turn-behavior integration has no complete vertical proof. | `partial-live-gated` |
| Fear | No voluntary enemy-target filter or forced-flee behavior is implemented. Reactions must remain unaffected. | `integration-missing` |
| Taunt | Neither directed-taunter compulsion nor the specified one-turn Berserk fallback is integrated. | `integration-missing` |
| Interrupt | Pending/charged action observation exists, but no execution-owned cancellation transaction is implemented. Ordinary damage correctly remains non-interrupting. | `integration-missing` |
| Full status category roster | Technical ownership exists for all native status transactions, but the DCL's physical/mental/inverted-mental/magical category and duration policy is not authored for the full roster. | `metadata-authoring` |
| Player-facing DCL readability | Forecast amount/hit and miss/status presentation surfaces exist. The DCL still does not define or implement a complete vocabulary for damage matchup, current defense ladder/depletion, Weight breakpoint, trait modifiers, and status-source semantics. | `design-open` |

## Immediate consequence

The coverage generator must accept `metadata-authoring` and `design-open` because its own completion
rule already names them. The newly explicit rows prevent the unified sentinel profile and final live
regression from being mistaken for whole-DCL completion while these mechanisms remain absent.

## Next offline priority

Weight to Move is the closest missing structural vertical: its input map, aggregation, curve, unit
field, and one-shot probe already exist. Before a persistent writer is enabled, the live gate must
prove that `unit+0x42` controls both the displayed Move value and reachable tiles and establish when
the engine restores the derived byte. Offline work can still specify restoration, pointer reuse,
settings reload, and clamp invariants around that single unknown.
