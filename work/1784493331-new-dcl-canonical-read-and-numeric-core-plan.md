# New DCL canonical read and numeric-core checkpoint

## Authority reset

The canonical DCL is the complete `docs/deep-combat-layer/00` through `19` document set introduced
on `origin/main`. Deleted documents and earlier design assumptions are historical evidence only.
`docs/modding/` remains the authority for proven engine surfaces, and dated investigations remain in
`work/`.

The implementation excludes individual job kits and final job balance. Documents 15 and 16 are
still required reading because they define the inputs, progression boundaries, and normalized
records that the combat engine must accept.

## Architectural reading

The new DCL is one transaction-oriented combat engine rather than a collection of formula patches:

1. ST, DX, IQ, and Brave-derived HT produce exact derived characteristics.
2. Rank-normalized initial CT and linear global CT grant one Movement plus one Action.
3. Every action normalizes into the document-19 source, skill, resource, timing, targeting,
   delivery, magnitude, effect, transaction, critical, presentation, and AI profiles.
4. Resolution uses one stable outer ActionInstance, a target-batch snapshot, explicit Strike and
   rider cardinality, native-compatible commit order, and one post-action Reaction window.
5. Every persistent state has explicit storage, source binding, resistance, stacking, duration,
   cleanup, mechanics, presentation, and AI behavior.
6. Forecast, AI, and execution consume the same profile and classifier; only confirmed execution
   consumes random draws.

## Reusable technical evidence

The current code already proves useful engine boundaries for staged HP/MP results, native status
packets, active-weapon identity, native repeats, AI scoring, native KO/revive, accepted Reactions,
final-tile publication, and forecast/apply hooks. Those facts remain useful.

The old settings profile is not the new DCL schema. Raw-Brave Reaction chance, Magic Evade as a
universal percentage, finite Parry uses instead of cumulative `-4`, Approach stop-hit behavior, and
the earlier Fear implementation are nonconforming and must not become the canonical integrated
profile. Old Fear/Approach code remains historical until it is either replaced or removed from the
release path.

## Dependency order

The safest offline-first order is:

1. numeric and probability primitives from document 17;
2. normalized action/state profiles and fail-closed validation from document 19;
3. outer-action, target-batch, Strike, rider, and Reaction transaction state from document 18;
4. attributes, derived characteristics, CT, and turn resources;
5. physical skills, active defenses, damage, equipment, and ranged routing;
6. source/delivery/effect magic, commitments, casting, magnitude, and persistence;
7. custom-state lifecycle and presentation hooks;
8. player forecast, AI parity, integrated profile, and live regression.

## Current implementation slice

The first slice consolidates exact rational decimals, named floor/ceiling/nearest rounding,
whole-percent and permille forecast boundaries, universal percentage rolls, hostile Quick Contests,
exact 46,656-pair enumeration, and pointer-free random-site identity. The physical hit forecast is
then routed through this shared exact probability type instead of maintaining a second enumeration
implementation.

Offline acceptance requires the document-17 boundary cases plus the DCL worked example of skill 12
against Dodge 9 producing exactly `21,924 / 46,656`, displayed as 47 percent.

## Execution-RNG correction

The runtime audit found that the earlier hit controller sampled a binary result during forecast and
kept an unconsumed forecast decision alive through the caster's turn. That behavior conflicts with
the document-17 contract even when it makes forecast and execution appear consistent.

The calc-entry shim already has two synchronous provenance signals: the native caller return address
and the battle-state global. The corrected gate permits execution RNG only for the proven outer-sweep
caller paired with state `0x2A` or native-repeat state `0x2F`. Forecast, AI, unknown, and nested
synthetic calls compute and publish probability only. They use `roll=-1` in diagnostics and never
write the execution cache. Every execution-cache row now obeys the ordinary delivery TTL; there is no
caster-turn forecast lifetime extension.

Offline analyzer fixtures require evaluation rows to be uncached with `roll=-1`, confirmed execution
rows to contain a sampled roll, same-epoch execution reuse to remain identical, and cross-epoch reuse
to fail. The remaining live gate is to observe those four properties in a real forecast followed by
execution; no live test is needed until the next offline slices are complete.

## Normalized authoring slice

The document-19 action and persistent-state records now exist as runtime types rather than an
informal catalog convention. The action model separates source, skill, resource, timing, target,
delivery, magnitude, ordered effects, transaction cardinality, critical behavior, presentation, and
AI policy. The state model requires resistance, immunity, stacking, duration, ticks, cleanup,
payload, mechanics, presentation, and AI ownership.

The validator fails closed on unknown behavior enums, invalid numeric domains, zero-cost overcast,
partial area/delivery/magnitude unions, Internal Direct with active defenses, Block without Defense
Bonus policy, ArmorDividing without a positive divisor, status effects without a referenced state,
incomplete StackToCap, missing duration/tick formulas, and divergent revisions of one ActionId.
These types are not yet the storage loader; they are the normalized boundary that a future XML/NXD
or sidecar loader must produce.

## Outer-action transaction slice

The first document-18 transaction model now provides battle-generation UnitKeys, a monotonic
ActionInstance sequence, declaration/profile revision matching, immutable ordered TargetBatch
membership, finite defense snapshots, per-target/per-Strike plans, stable commit order, semantic roll
identity, target-local KO short-circuiting, effect visibility timing, ResourceFailure before target
snapshot, and exactly one post-action Reaction window.

Native-immediate damage/HP lifecycle effects become visible after their originating Strike.
Status-like effects obey Deferred or explicit Immediate metadata. A Strike skipped because the target
was already KO contributes no roll, damage, state, rider, resource transfer, or Reaction trigger.
Parry resources track cumulative attempts per weapon/limb with the canonical `-4` step; Block is a
single snapshotted attempt. These are offline transaction primitives and are not yet connected to
the native execution hooks.

## Characteristic, initiative, and turn-resource slice

Documents 01 and 02 now have a pure resolver for additive ST/DX/IQ, open-ended Brave-to-HT,
secondary characteristics, exact Basic Lift and quarter-point Basic Speed, max-pool reconciliation,
rank-based initial CT, fixed global CT gain, exact Haste/Slow multipliers, turn reset, and the
independent Movement/Action budget.

Offline coverage includes every published Brave/HT anchor, primary lower bounds, HP/MP/Will,
fractional Lift and Speed, initiative tie-breaks, the `75/50/25` three-unit CT distribution, Slow's
exact `7.5`, Haste's exact `15`, CT reset, Attack preserving Movement, double-Action rejection, and
Stand Up consuming both resources. Native storage mapping for the reinterpreted raw PA/Speed/MA,
HP/MP modifiers, quarter-point CT, and player presentation remains a later integration/proof gate.

## Physical skill, defense, and injury slice

The canonical aptitude table and GURPS skill bands now resolve weapon/shield Rank without an
implicit Rank-0 default. Dodge, Parry, and Block have named formulas; deterministic selection removes
illegal candidates before comparing scores and preserves the `Dodge > Parry > Block` tie order.
Incoming Parry Load uses skill override, else weapon Weight, else exact `ST/10`; the Parry limit uses
exact Basic Lift and doubles only for a two-handed Parry.

The existing literal ST damage table feeds a new shared injury resolver. It owns exact armor
divisors, the fractional-divisor DR-1 rule for unarmored targets, DR-before-wound ordering, all seven
wound multipliers, minimum basic damage, minimum one Injury after positive penetration, IgnoreDR,
Major-Wound/KO short-circuiting, and critical-only capped knockback. Offline cases cover fully
blocked hits, sub-one multipliers, divisor `0.5`, half DR, surviving and lethal Major Wounds, and the
knockback type/survival gates. Equipment DR selection, facing/location selection, native HP commit,
Shock/Stun/Knockdown state commits, and displacement remain integration work.

## Encumbrance and ranged slice

Exact equipped Weight now resolves against Basic Lift through the five canonical encumbrance bands.
Move and Jump apply the exact rational multiplier and floor with minimum one; Dodge receives the
band penalty, while CT, initiative, skills, and damage remain untouched. Boundary tests cover exact
equality at `1x`, `2x`, `3x`, and `6x` Basic Lift plus the open Extra-heavy band.

The ranged core now owns all distance bands through 50 tiles, Effective Skill assembly, Aim bonus
steps, the Bow/Crossbow/Gun/Thrown defense matrix, and a target-bound Aim state. Aim resets on target
change, consumes no RNG at zero Injury, rolls Will once after positive Injury, and cancels directly
on forced movement. Native trajectory, maximum range, facing denial, posture, movement cancellation,
and state persistence/presentation remain carrier integration work.

## Coverage authority correction

The coverage generator no longer reports universal Magic Evade or finite Parry uses as conforming
new-DCL mechanisms. It now distinguishes historical live carriers from the canonical Internal
Direct/magical-defense contract and cumulative repeated-Parry lifecycle. The generated coverage also
includes the normalized authoring, numeric, transaction, characteristic, CT, skill/defense, injury,
encumbrance, and ranged slices implemented in this checkpoint.

## Persistent-state registry and scheduler slice

The runtime now has a battle-generation registry keyed by stable UnitKey and StateInstanceId. It
supports isolated evaluation clones; source-required payloads; typed payload schema checks;
target-local StackKey/discriminator identity; Replace, Refresh, StrongestWins, StackToCap, and
Independent policies; per-contribution Refresh/Replace; target/source turn clocks; global CT;
uses/triggers; permanent and explicit-command lifetimes; cure families; KO/source-loss cleanup; slot
reuse; and battle clearing.

The global scheduler emits ticks chronologically and by stable StateInstanceId. A tick due exactly at
expiry resolves first, then expiry removes the instance. Each callback sees the exact global CT and
can become its own outer ActionInstance. Forecast and AI can apply prospective states to a cloned
registry without mutating execution state or consuming execution identities.

The normalized definition gained two behavior-bearing fields required by documents 08 and 14:
`SourceRequired`, and the `Refresh | Replace` reapplication policy for one StackToCap contribution.
Duration clocks now distinguish GlobalCT, TargetTurn, SourceTurn, UsesOrTriggers, ExplicitCommand,
Permanent, and a named Explicit extension.

## Universal posture/status-rule slice

Pure rules now own the strict one-third Critical threshold and signed ceiling, Stun resources and
end-turn recovery gate, the complete Knocked Down modifier package, raw-Injury Shock accumulation,
Brave temperament for Taunt/Fear, margin-derived duration, EffectStrength, hostile-status and Dispel
Quick Contests, and typed payloads for Shock, Taunt, Fear, Guard Broken, Weapon Bound, and Elemental
Exposure. Native packet/pose/palette/icon integration and action-legality UI remain later carrier
work.

## Magic resource, casting, delivery, and magnitude slice

Documents 11 through 14 now have pure exact resolvers for final MP cost, explicit overcast
authorization, stable ApprovedHPCap, pre-gate ResourceFailure, outcome-dependent settlement, MP-first
payment, lethal overcasting, global CastCT, and per-Strike concentration incidents. The cost preview
retains the full MP+HP split before confirmation; restored MP can reduce HP settlement, while later
MP loss cannot exceed the approved cap.

One shared casting roll classifies BaseSpellScore and every target-relative score. External
Projectile, Beneficial, and Internal Direct delivery use their distinct gates without universal
Magic Evade. Faith, Zodiac, element/Absorb/Null, strongest source boost, Shell, Oil, magic dice,
critical healing, healing caps, and one-time Reflect routing retain exact rationals until their one
defined floor. Offline tests cover critical boundaries, target modifiers that cannot rescue a base
failure, exact probabilities, Absorb healing, and the prohibition on Oil consumption without
positive Fire Injury.

## Magic declaration, targeting, area, and cardinality slice

Cast declaration now validates learned/source/verbal/prerequisite/resource/target legality and
freezes stable identity, profile revision, declaration tile, FinalMPCost, ApprovedHPCap, CastCT, and
global due point. It deliberately does not freeze resolution-time unit statistics. Unit targets
resolve from their current tile without a second range or LoS check; fixed tiles remain fixed;
caster-centered areas use the caster's current tile. Area membership filters explicit allegiance
and current target state before entering a stable battle-slot-ordered TargetBatch.

The normalized area vocabulary now matches the canonical `TrackedUnit | FixedTile | Caster` center
and `None | Dodge | QuickContest` delivery gates. One Base/target casting failure consumes no target
gate RNG. `None` adds no avoidance; `Dodge` rolls once per target per Strike and is bypassed without
RNG on an External critical; `QuickContest` rolls once per target and shares its result across the
complete multi-hit action. A landed hostile rider reuses the shared caster draw, checks immunity
before RNG, rolls resistance once per target, and loses ties. Native target enumeration, current
unit/tile reads, UI forecast, AI, and result-carrier commit remain integration gates.

## Facing, reach, awareness, and universal-action slice

The pure geometry layer now classifies Front, Side, and Back from a cardinal facing vector with the
canonical inclusive diagonal boundary and retains lateral sign for shield-side legality. Active
defense applies Side `-2` and Blind `-4` only after legality; Back, critical, and an immediate first
offensive Action from Invisibility create no defense roll. Blind's source penalty is limited to
VisionRequired delivery. Reach 1/2 remains a binary Manhattan gate subordinate to native path and
height verdicts, while location routing selects combined, Body, or Head DR exactly once.

Invisible hostile units cannot be selected directly, but indiscriminate area membership and
friendly selection remain legal. An immediate first Action suppresses active defense for its full
outer transaction and removes Invisibility at commit; a charged Action removes it at declaration
completion and delivers later from a visible source. Native facing-byte-to-vector decoding and
path/height carriers remain engine-integration gates, not invented geometry.

Turn-resource commands now model Attack, Ready, Reequip, and Stand Up atomically. An
UnreadyAfterAttack weapon loses attack and Parry until Ready spends Action; an Unbalanced weapon
stays Ready but loses Parry until the owner's next turn. Reequip spends Action while preserving
spent Block, cumulative Parry attempts, readiness, and temporary Parry loss. Stand Up spends both
resources, and Don't Act/Don't Move can remove each independently.

## Reaction activation-contract slice

The canonical Reaction metadata now distinguishes AutomaticTrigger, SkillResponse, and
ActivationRoll without a raw-Brave or universal HT gate. ActivationRoll accepts exactly one DX,
HT, IQ, Will, or named-Skill reference plus one explicit modifier. AutomaticTrigger rejects every
MP, HP, item, and cooldown cost; SkillResponse consumes no extra activation RNG and delegates to the
Reaction's natural effect roll.

Eligibility, awareness, trigger, costs, and finite uses short-circuit before RNG. ActivationRoll
uses the shared exact 3d6 classifier and forecast. One window-cardinality owner prevents duplicate
managed acceptance and can either enforce OncePerWindow or delegate first acceptance to the proven
native cardinality verdict. Binding the normalized record to the existing native accepted-order and
effect dispatch remains an integration gate.

## Deterministic Character Growth slice

Document 15 now has an exact six-decimal micro-unit growth core without assigning any real job
vector. Synthetic vectors validate nonnegative rates and equal point-equivalent budget across ST,
DX, IQ, Brave, HP Modifier, and MP Modifier. Lifetime per-channel fractions persist through job
changes, integer gains floor only at one million micro-units, and checked immutable award results
make the same multiset of job levels order-independent.

HighestAwardedCharacterLevel prevents delevel/relevel farming. Pre-DCL migration preserves the
existing permanent baseline, starts all fractions at zero, and marks the current level as already
awarded. Unknown newer schemas disable growth visibly without resetting data; older schemas require
an explicit migration. Faith remains a separate clamped roster/battle trait and receives no growth
channel. Save serialization, atomic native level-up commit, UI breakdown, and authored real-job
vectors remain integration/content gates.

## Revive and Undead-policy slice

Revive now has an explicit pure profile for eligible states, Immediate versus stored Reraise,
success-versus-restored-HP Faith ownership, and the required Undead family. Applying Faith to both
axes fails validation unless the profile explicitly pays for both. Immediate revival produces a
native HP credit plan and clears KO only after a positive credit; Reraise stores a trigger and does
not masquerade as immediate healing.

Undead behavior is no longer inferred from one universal healing inversion. A complete table must
name normal-target, Undead-target, and Undead-caster behavior for direct healing, regeneration, HP
and MP drain, Raise/Arise, Reraise, Poison, instant KO, and restorative items. Unknown or missing
families fail closed. Binding these plans to the proven native revive/death-clock/Reraise carriers
remains the integration gate.

## Persistent-state presentation slice

The presentation resolver now owns the one-position precedence chain, the complete palette priority,
KO palette suppression, and post-palette Invisibility transparency. Custom state icons are symbolic
native-asset references rather than native status-bit reuse; Shock selects the Doom 1/2/3 counter
from its magnitude.

Selected-state detail is generated from the same normalized definition and live instance, exposing
display name, exact mechanics, source, magnitude, remaining CT/turns/uses or removal command,
stacking, cure families, and icon. Actual status catalog lookup, pose/palette writes, map icon rows,
and selected-unit UI rendering remain native integration gates.

## Correlated magic forecast slice

Multi-target magic forecast now enumerates the one shared caster draw rather than multiplying
independent per-target spell rolls. Conditional on that draw, target-relative SpellScore, critical
External bypass, independent Dodge, and independent Quick Contest branches remain exact rationals.
The result exposes each target's delivery probability plus the complete distribution of how many
targets receive delivery, including perfect correlation for identical unavoidable targets.

The single-target Quick Contest result matches the existing exact Internal Direct enumeration.
Connecting this model to the player forecast and AI target valuation remains presentation/runtime
integration, not a new probability rule.

## Strict normalized-authoring loader slice

The document-19 action/state boundary now has a strict JSON bundle loader with one schema revision,
exact rational strings, string-only behavior enums, a discriminated magnitude union, unknown-field
rejection, duplicate-id rejection, and atomic validation into a temporary registry. A malformed or
partially valid bundle never leaks profiles into execution.

This closes the generic storage-loader mechanism. Producing approved normalized overlays for the
512 real carrier records remains metadata authoring and will be done without assigning draft jobs
or final balance values.

## Normalized item-metadata slice

Items now have a strict native-ID-bound schema for slot, exact Weight, Body/Head DR, additive
characteristic/pool/mobility/resistance modifiers, weapon skill/damage/divisor/reach/range/hands/
ParryLoad/balance/readiness/trajectory, shield coverage, focused magic axes, elements, immunities,
and explicit special properties. The registry rejects IDs outside the native catalog, so it cannot
create a new SKU, and rejects partial slot unions, Internal Direct shield coverage, empty foci, and
unknown JSON fields.

Sentinel metadata for native Broadsword 19 and Leather Armor 172 round-trips through the strict
loader and feeds the canonical weapon-damage, Load/Move/Dodge, location DR, immunity, and bounded
affinity paths. This proves the metadata mechanism without assigning final item balance.

## Native ability-binding slice

A separate strict binding layer now joins one native ability ID and verified formula to exactly one
normalized ActionId/revision. Every binding names its carrier kind, rewrite policy, data
neutralization requirement, and forecast, AI, execution, apply, and presentation boundaries. The
validator rejects stale formulas, missing profiles, revision drift, carrier/cardinality mismatches,
duplicate native ownership, and unknown fields.

Native Fire 16/formula `0x08` is the sentinel binding. The coverage audit lists every unbound record
and permits only explicit exclusions that themselves exist in the 512-record catalog. It reports
509 real records still requiring approved profiles after the one sentinel and the two reserved
records; no permissive fallback is created. This is the intended fail-closed starting point for
materializing real canonical overlays without importing draft jobs.

## Prepared protection and QuickLock slice

Bulwark now has its explicit cancellation predicate. Overwatch owns weapon/tiles/condition/remaining
trigger/expiry payload and revalidates weapon, source, target, range, and native trajectory at each
trigger. A movement trigger cannot fire until the native route is fully settled; tile-by-tile
movement never pauses or creates a hit. Cover/Bodyguard similarly validates both unit identities,
range/adjacency, delivery class, receiver legality, and remaining intercepts, ending without a
redirect when invalid.

Quick now requires a positive authored CT grant large enough to produce the next turn, creates one
QuickLock, rejects recursive Quick while locked, and clears the lock only after that granted turn
resolves. Stop remains the zero-gain CT rate while global state timers continue under the registry
scheduler.

## Canonical single-result magic vertical

The first fail-closed runtime composition now resolves native Fire 16 through its verified formula
and normalized ActionId/revision before any mechanics run. The sentinel vertical composes stable
declaration, full-cost precheck, one shared caster gate, conditional per-target defense, magical
Injury/Absorb magnitude, atomic MP/HP settlement, a one-Strike target transaction, conditional
effect commit, and exactly one post-action Reaction window.

Offline cases cover ordinary delivery, defended delivery, ordinary base failure, and critical
delivery. They preserve cost policy and RNG ownership while preventing failed or defended casts
from planning a visible target effect. This proves one canonical composition seam only; it does not
yet bind the managed resolver to native forecast/AI/execution/apply/presentation carriers, cover
other cardinalities, or reduce the 509-record metadata-authoring queue.

## Canonical runtime loading and transaction composition

The code-mod reload cycle now treats the normalized action/state bundle, native-ID-bound item
bundle, and native ability-binding bundle as one candidate snapshot. Enabling the canonical runtime
requires all three paths; every file, schema, cross-reference, native ID/formula, and profile
revision validates before the active runtime is replaced. A failed reload preserves the previously
loaded runtime. The loader remains disabled by default and is not yet connected to a combat write.

The authoring gate now parses fixed magnitude as strict `Xd6+Y`, retains legal zero-die healing,
rejects free-form formula text, validates delivery/cardinality/physical-route combinations, keeps
the magnitude union consistent with ordered effects, rejects a Rider before its Carrier, and
requires every status effect to resolve a state definition in the same atomic bundle.

ResourceFailure now terminates the declared transaction in `ResourceFailed` before TargetBatch,
casting RNG, debit, effect, or Reaction. Successful commits share a generic canonical batch
executor that normalizes target/Strike order, applies only the effect indexes actually resolved,
preserves target-local KO short-circuiting, exposes deferred effects at outer commit, and opens one
Reaction window. The Fire sentinel now rolls the magnitude expression from its normalized profile
instead of accepting a precomputed amount.

## Canonical healing, status, physical, Area, and Rider verticals

The canonical execution seam now covers direct Beneficial healing, including the missing-HP cap and
the critical rule that maximizes exactly one healing die. Internal Direct status resolves casting,
immunity-before-RNG, one target Quick Contest, resource settlement, ordered state materialization,
and the single outer Reaction window. ResourceFailure is represented as a normal terminal result
and rejects caster, defense, resistance, and magnitude draws that would belong to unreachable
random sites.

The physical executor resolves a verified weapon item and native-repeat ability through the same
catalog. Every Strike selects its current best legal active defense, spends cumulative Parry at -4,
retains one Block resource, bypasses defense on a critical, rolls weapon ST damage, applies DR and
Injury, and stops target-local mechanics after KO. The four-Strike Barrage sentinel produces three
Injuries and one defended Strike while opening only one Reaction window.

The Area executor resolves its tracked center from current positions, accepts the native geometric
membership as the authoritative shape result, freezes the filtered TargetBatch in stable unit-slot
order, shares one caster roll across all targets, and owns the authored target-gate cardinality.
The three-Strike sentinel proves per-target/per-Strike Dodge and magnitude ownership, target-local KO
short-circuiting, Absorb HP updates, and one-time Oil consumption. It rejects surplus random draws
after a path becomes unreachable.

A single-result Damage or Healing Carrier may now own ordered status Riders from the same normalized
profile. Each landed nonimmune Rider reuses the Carrier's caster roll, makes exactly one resistance
roll, awards Quick Contest ties to the target, materializes its referenced state before the outer
Reaction window, and appears as a committed effect only when applied. A defended Carrier attempts no
Rider and consumes no Rider resistance RNG.

## Canonical Revive and Reraise vertical

The normalized action profile now carries the explicit revive policy required by the lifecycle
specification: target states, Immediate versus StoredReraise, the one Faith axis, explicit permission
for a double Faith axis, and the Undead family. Its restored-HP expression is an exact HP resource
magnitude, and StoredReraise must reference a persistent trigger state in the same atomic bundle.

The transaction no longer treats every target that begins at zero HP as unconditionally skipped. A
KO-eligible action may execute its first Strike; KO caused during the action still short-circuits
later Strikes. The canonical lifecycle executor composes casting, ResourceFailure, explicit Undead
policy, restored-HP dice, Faith, native HP-credit-before-KO-clear semantics, stored trigger
materialization, resource settlement, and one Reaction window. Rejected Undead routes consume no
restored-HP RNG, and Reraise stores the expression without rolling or healing at application time.

## Canonical removal, Dispel, and Quick verticals

Named Beneficial StatusRemoval and hostile Dispel are separate executors. StatusRemoval performs its
casting gate and deterministically removes every instance of the referenced state kind. Dispel
stores its selection scope and eligible cure families in the normalized profile, shares one caster
draw, then gives every snapshotted effect instance one independent Quick Contest against its stored
EffectStrength. A tie preserves the effect. Only winning instance identities are removed atomically
after commit planning and before the one Reaction window. Failed casting consumes no effect roll.

Quick composes an exact authored CT magnitude with the existing QuickLock controller and persistent
state registry. A delivered action grants enough CT for the next turn, creates one visible lock, and
commits both ordered effects in one transaction. Casting fails without changing CT or state; a
second Quick is illegal while locked. The granted-turn completion boundary clears controller and
persistent state together and fails closed if their ownership diverges.

## Canonical universal commands and physical turn cost

Ready, Reequip, and Stand Up are loaded normalized system actions, not invented native abilities.
Their deterministic Other delivery uses no skill or RNG. Ready spends Action and changes only an
Unready selected weapon; Reequip spends Action while preserving all current Parry, Block, readiness,
and post-attack balance resources; Stand Up spends both Movement and Action and removes the exact
Knocked Down state during commit. Each command participates in one outer ActionInstance and Reaction
window.

The canonical physical executor now requires the same turn-resource and selected-weapon state that
the command layer owns. It validates payment/readiness before any Strike RNG and, after the full
multi-Strike commit, spends the authored turn cost and applies the weapon's readiness/balance
post-attack state exactly once before the Reaction window.

## Canonical Reaction window and prepared-state bridge

The revision-2 authoring bundle includes normalized Reaction definitions. Every Reaction references
an action loaded in the same atomic bundle. The post-action transaction window resolves candidates
in explicit native order, filters trigger/eligibility/awareness/cost/cardinality before RNG, supports
AutomaticTrigger, SkillResponse, and exactly one authored ActivationRoll reference, and resolves the
accepted effect action without a universal Brave or HT roll. Activation RNG on an ineligible or
native-cardinality-rejected candidate is invalid input.

The prepared-state bridge keeps Overwatch, Cover/Bodyguard, and Bulwark controllers synchronized with
their persistent state instances. Overwatch returns immediately for unsettled movement, before even
checking weapon/source/target/range/trajectory, so it cannot pause or fire tile by tile. A settled
event revalidates every dependency and reserves the normalized effect action while consuming one
trigger. Cover redirects only eligible revalidated delivery to the protector. Invalid controllers,
spent uses, and Bulwark cancellation remove the exact persistent instance atomically.

## Canonical ranged physical vertical

Projectile item metadata now declares Bow, Crossbow, Gun, or Thrown kind exactly when it has a
positive maximum range. The physical executor derives EffectiveSkill from base weapon skill,
target-bound Aim, the canonical distance band, explicit location penalty, and Shock/state penalty.
It requires both native range and trajectory verdicts before attack RNG, matches the action route to
the item's Direct/Arc route, and filters Dodge/Block/Parry through weapon-kind legality before
choosing the best defense. The Bow sentinel proves Aim plus Accuracy, Arc routing, Parry rejection,
legal Block, item-owned thrust/impaling Injury, and one outer turn cost.

## Reflect routing and magical DR order

The direct magic executor now treats its input target as the original declaration target and, when
the normalized action is Reflectable, can rebind the resolution target to the original caster. The
route is resolved before target-relative SpellScore and uses the reflected recipient's defense,
state, HP, Faith, affinity, Shell, and DR inputs. It retains the one caster draw, resource commitment,
magnitude profile, and one-reflection guard.

Magical damage no longer passes raw dice directly into affinity. Manifestation and ArmorDividing
resolve applicable DR/divisor and the canonical wound multiplier first; IgnoreDR/InternalSpiritual
start after that armor stage. Only resulting BaseInjury enters affinity, source boost, Faith, Shell,
Oil, and the final magical rounding boundary. Direct and Area executors share this owner.

## Area healing and the native-integration boundary

The Area executor accepts either the normalized Damage or Healing carrier. Area healing retains one
shared casting draw, rolls magnitude independently per target, caps each target at its own MaxHP,
and does not inherit the direct-Beneficial critical rule that maximizes one healing die. The
ability-2/formula-0x0C sentinel proves that the same raw `3d6+2 = 8` applies 8 healing at 10/20 HP
and 5 healing at 15/20 HP, with one outer Reaction window and two visible target effects.

The canonical catalog is atomically loaded by `Mod`, but no native callback invokes a canonical
executor yet. The proven compute-point and pre-clamp paths still consume the older settings-formula,
hit, and status caches. The next integration gate is therefore an explicit projection from the
current native combatant/action snapshot into one normalized ActionInstance and one carrier plan
shared by forecast, AI, confirmed execution, staged apply, and presentation. The bridge must fail
closed for an unbound ability and must not synthesize job adjustments or final content values while
the job documents remain draft.

The first native-facing layer now projects a resolved single-result magic action into two semantic
records: the target result and the caster resource payment. They remain separate even if Reflect
makes both records name the caster. A target Injury may open damage behavior; HP paid for overcast
is explicitly a resource payment, never Injury, and never opens a damage Reaction. ResourceFailure
projects no target, payment, or Reaction record. This exposes the required native commit order:
effect first, resource payment second, one Reaction window third.

`Mod` now creates one mutable canonical battle owner at the proven battle-generation boundary and
discards it on battle end, settings/catalog reload, or unit-slot identity reuse. This owner supplies
the generation-local ActionInstance sequence, persistent-state registry, turn resources, weapon
readiness/balance state, turn serials, and stale-slot cleanup. A separate native snapshot adapter
projects only proven unit fields (slot/character identity, X/Y/layer, team relation, HP, KO/Undead,
revision, finite defenses). It intentionally requires tile height and all nonnative/job-derived
values from their explicit owners rather than guessing them from current vanilla PA/MA/Speed.

## Native multi-carrier projection

Area magic and physical resolution now cross the same semantic native boundary without flattening
their outer transaction. Area projection preserves stable target/Strike identity, shared resource
payment, per-target healing caps, defended results, target-local KO short-circuit continuations, and
one Reaction window. Physical projection preserves the exact attack/defense outcome and Injury for
every Strike while keeping Action, Movement, and weapon readiness/balance costs outside native HP/MP
channels. Both projections emit empty later repeat records after a target-local KO, so a native
visual continuation cannot consume another canonical roll or effect.

Offline smoke coverage proves four-Strike melee, lethal first-Strike short-circuit, single-result
ranged physical, damaging Area, healing Area, and Area ResourceFailure projection. The remaining
runtime bridge must correlate these semantic records with native forecast, AI, execution, apply,
and presentation callbacks; no native callback consumes the new projections yet.

The battle runtime now owns a native Action ledger. One resolved ActionInstance may publish once;
native delivery must consume stable target/Strike records in order, expose presentation only after
that Strike's apply boundary, commit the distinct source resource payment next, acknowledge at most
one Reaction window, settle, and only then retire. Resource payment remains an explicit step even
when the action has no HP/MP cost, so Action/Movement and weapon costs cannot be mistaken for target
damage. Duplicate publication, repeat reordering, early presentation, early payment, and duplicate
Reaction entry fail closed offline.

## Universal Injury consequences

The canonical physical, direct-magic, and Area-magic damage paths now compose the shared immediate
Injury consequences per Strike. Final Injury accumulates raw Shock Injury across later Strikes,
computes the capped penalty from MaxHP, performs a surviving Major-Wound HT roll only when required,
applies the paired Stun/Knocked Down verdict on failure, resolves critical-only capped knockback,
and performs exactly one concentration check or direct cancellation for the same originating
Strike. Misses, defended delivery, Absorb/Null, ResourceFailure, and target-local KO continuation
reject unreachable consequence inputs and random draws.

The semantic native projection carries the complete consequence result beside HP Injury, rather
than leaving Shock/posture/disruption to be recomputed by a later hook. A named Shock registry
handler owns its exceptional stacking rule: it merges new raw Injury into one existing accumulator,
keeps the original next-target-turn expiry, and updates the visible penalty without refreshing the
window. A separate consequence committer materializes Shock and the paired Major-Wound posture
states; the native integration still has to bind this plan to HP/KO, displacement, Charging, state
presentation, and preparation-cancellation carriers.

## Exact direct-magic forecast and AI projection

The direct-magic evaluator now enumerates delivery and magnitude without execution RNG. Arbitrary
`Xd6+Y` expressions use exact convolution with integer outcome weights, so large dice pools do not
expand into an exponential list of roll tuples. The evaluator shares the same casting gate,
External/Beneficial delivery classification, critical rules, DR/affinity/Faith/Shell/Oil route,
Reflect target, and outcome-dependent MP/HP payment as execution. Player forecast retains exact
rationals and only rounds at the named native percentage projection; AI retains exact expectations
and rounds once at the final native scoring field. ResourceFailure exposes no target distribution
or payment.

Offline tests prove exact `2d6`, `100d6`, ordinary and maximize-one-die critical healing,
External-projectile delivery parity, repeated evaluation stability, final resource cost, and the
ResourceFailure boundary.

## Native compute/apply reservation

The canonical Action ledger now reaches the proven native compute-point and pre-clamp boundaries
for an already-published ActionInstance. Compute preparation locates the exact pending
source/ability/target/Strike identity, replaces all four numeric channels from the immutable
projection, and stores the reservation beside the natural and rewritten carrier. Re-entry before
apply reuses that reservation and never advances the Action.

The pre-clamp boundary consumes the same cached reservation, stages the non-HP channels and result
flags atomically, and advances exactly one Strike only after those writes succeed; HP debit remains
the callback's native return channel. Canonical reservations bypass the legacy formula/status/hit
path, preventing a single delivered Strike from being partially recomputed by two semantic owners.
The bridge remains dormant unless a canonical executor has already published a matching Action;
execution publication, resource-payment commit, presentation acknowledgement, and settlement are
the next runtime gates.

The bridge's pure finalization contract is also closed: after every target/Strike is applied, it
prepares the source-owned resource payment (including an explicit no-HP/MP payment for physical
actions), commits it once, acknowledges the declared single Reaction window, settles, and retires
the ActionInstance. ResourceFailure has a separate empty settlement path and can never manufacture
a target or payment reservation. Native callbacks for the source HP/MP write and accepted Reaction
boundary still need to invoke these already-ordered operations.

One execution-publication owner now combines projection and ledger publication for direct magic,
Area magic, and physical actions. It resolves the ability binding from the battle's immutable
catalog in the same operation, so callers cannot project under one revision and publish under
another. Duplicate callback re-entry remains rejected by ActionInstance identity. Offline coverage
walks a full six-carrier Area action through stable application, one source payment, one Reaction,
settlement, and retirement.

## Confirmed-execution random ledger

The battle runtime now owns every DCL execution draw by the document-17 semantic identity:
battle generation, ActionInstance, stable source/target, Strike, roll site, and draw index. It
supports cached 3d6, individual d6 pools, 0..99 percentage rolls, and uniform candidate selection.
Re-entry returns the immutable prior components/result; reuse of one identity for a different
domain fails closed. Shared per-Action caster rolls use an explicit targetless identity, while the
battle owner refuses fabricated or stale UnitKeys. Forecast and AI projections still have no path
that implicitly samples this ledger.

A conditional Injury planner now determines reachability before drawing Major-Wound HT or
concentration. Collapse/KO/direct cancellation suppresses concentration RNG; a charging incident
that survives and has Injury/displacement consumes exactly one concentration roll. Re-entering the
same Strike reuses both sites.

## Direct-magic execution coordination

One direct-magic coordinator now validates the immutable action revision and current observed
source/declared/reflected target, rejects invalid carrier/defense/Rider metadata before RNG, checks
resolution-time resource availability, and then owns the complete reachable draw graph: shared
casting 3d6, conditional active defense, exact damage/healing dice, conditional Injury checks, and
per-Rider resistance. It invokes the canonical resolver and publishes the resulting ActionInstance
in one operation. ResourceFailure publishes an empty Action and consumes zero draws.

The offline sentinel traverses the full vertical from execution RNG through resolution,
publication, native compute reservation, apply, presentation read, source payment, Reaction,
settlement, and retirement. Duplicate execution callback entry reuses all cached random sites and
is rejected at duplicate publication without drawing again.

## Synchronized native unit projection

The native adapter now combines proven raw PA/Speed/MA, Brave, coordinates/layer, effective status,
HP/MP/Faith, and stable slot identity with explicit characteristic, tile-height, revision, and
finite-defense inputs. It derives canonical primary/secondary characteristics without assigning
draft-job values. Execution fails closed while native MaxHP/MaxMP or current pools are not already
synchronized with the canonical characteristic snapshot, preventing a hybrid vanilla/DCL action.

## Canonical equipment snapshot

The proven seven-word native equipment block now joins strict DCL item metadata for Head, Body,
Accessory, both weapon hands, and both shield hands. Empty `0`/`255` slots are explicit; a nonempty
item with missing metadata or the wrong authored slot fails closed. Repeated equipped instances
remain distinct so dual-wield Weight and modifiers are counted correctly.

The resulting snapshot owns total exact Weight, all attribute/pool/Speed/Move/Jump/Dodge/Will/MR
modifiers, Body/Head DR, case-insensitive status immunity and special-property lookup, active-weapon
validation, and target affinity combined with the strongest source boost. This supplies the
non-job equipment inputs needed by characteristics, physical defense/Injury, magic, and status
immunity without creating items or falling back to the vanilla effective stat surface.

## Canonical state snapshot and revision

The state registry now owns a monotonic revision per stable target UnitKey. Every accepted add,
replace, refresh, extension, explicit merge, use consumption, tick advance, expiry, cure, direct
removal, source cleanup, and clear advances the affected target revision; rejected applications do
not. Evaluation clones copy revisions with their instances. A target capture returns revision plus
ordered instances under the same lock, preventing forecast from pairing one state set with another
revision.

One immutable canonical snapshot joins that capture to the four proven five-byte native arrays:
source `+0x57`, immunity `+0x5C`, effective `+0x61`, and durable master `+0x1EF`. Mechanical queries
read typed payloads only. Singular Shock and Aim fail closed on duplicates or payload mismatch;
Elemental Exposure sums its signed typed contributions by element. Aim now persists its stable
target and consecutive steps as a typed payload. The native target adapter was corrected to read
Undead behavior from the effective `+0x61` mirror rather than treating a master-only `+0x1EF` bit
as already effective.

## Focus and synchronized magic mechanics

Applicable focus metadata now resolves by explicit tradition. Ordinary integer modifiers sum,
MP-cost factors multiply exactly, and elemental boosts select the strongest value. The cast
declaration accepts those extra MP factors and freezes their one final ceiling beside additive
CastCT modifiers. Direct and Area magnitude inputs accept an additional integer modifier, so focus
damage/healing uses the identical normalized dice expression in forecast, AI, random planning, and
confirmed execution.

A synchronized magic projector composes the normalized action with caster, declared-target, and
reflected-recipient projections plus their equipment snapshots. It derives the action skill
modifier, focus magnitude, Faith policy, location DR, equipment affinity, typed Elemental Exposure,
one-shot Oil contribution, Shell, Reflect, concentration, CastCT, and MP-cost inputs without draft
job inference or textual-state parsing. Offline sentinels prove focus `2d6 -> 2d6+1` parity,
multiplicative MP cost, CastCT, master/effective separation, and the complete Fire input projection.

The same synchronized state revision now projects shared nonmagic combat inputs: exact Injury target
context (ST, effective HT, accumulated Shock, Charging, Will, and concentration terms), physical
location DR, ranged Aim/Accuracy plus Shock and distance, and native/equipment status immunity with
HT, Will, Spiritual Resistance, or explicitly authored resistance. This closes deterministic input
construction for those axes. The ranged snapshot also carries the exact Aim instance id. A physical
outer-action validates that instance before resolving and removes it after the shot commits but
before Reaction, so firing discharges persistent Aim exactly once. A named lifecycle now removes
that same instance on failed Injury retention or forced movement, preserves it without a write on a
successful/zero-Injury outcome, cancels one owner for posture/trajectory events, and removes every
Aim pointing at a lost stable target. The battle owner exposes movement, posture, trajectory-loss,
and Injury-retention entry points; unit removal/slot replacement already invokes tracked-target
loss automatically. Physical per-Strike Injury now consumes the supplied Aim-retention result only
when a target actually has Aim, suffered positive Injury, and was not already cancelled by forced
movement; success retains and failure removes the exact instance. Miss/defense/KO/no-Aim/forced
movement reject unreachable draws. The execution RNG vocabulary reserves `AimRetention`; a physical
coordinator, direct-magic coordinator, and Area coordinator now sample that site only for a
positive-Injury target that still owns Aim after direct KO/posture/displacement cancellation.

Native characteristic input construction now has one composition owner for equipment. It rejects
prepopulated equipment attribute channels, then inserts equipment ST/DX/IQ and additively composes
HP/MP, Will, exact Basic Speed, Move, and Jump with explicit non-equipment inputs. This prevents a
caller from silently omitting or double-counting the same equipped modifier before the existing
MaxHP/MaxMP synchronization gate.

## Physical confirmed-execution coordination

One physical coordinator now accepts deterministic normalized action, exact equipped weapon,
target, defense, and consequence snapshots only. It rejects stale action/item revisions and any
pre-supplied random result before touching the battle ledger. The physical executor samples Attack,
only-reachable active defense, only-landed weapon dice, conditional Major Wound/concentration, and
conditional Aim retention under stable Action/source/target/Strike/site identities. Turn and weapon
resources must be the exact battle-owned instances. Resolution and immutable native publication are
one operation.

The Barrage sentinel proves four Attack sites, three reachable defenses, and three landed damage
sites: twenty-four individual random values under ten ledger identities. An invalid hybrid request
consumes zero values. A ranged positive-Injury sentinel adds exactly one Aim-retention identity and
removes the failed target Aim before publication.

## Magical Aim retention

Direct magical Injury now performs the same target Aim lifecycle after universal Injury
consequences and before native publication. KO, Stun, Knocked Down, and forced displacement cancel
directly without a retention roll; otherwise positive Injury samples one battle-owned 3d6 site.
The sentinel proves one failed retention after casting and magnitude, with eight random values under
four semantic identities and exact-instance removal.

## Area confirmed-execution coordination

One Area coordinator now converts deterministic current-unit, native-geometry, target, defense,
magnitude, and Injury snapshots into a battle-owned execution. The Area executor lazily samples one
shared caster roll, target Quick Contest or per-Strike Dodge only when reachable, landed damage or
healing dice, conditional Injury consequences, and per-Injury Aim retention. It then publishes the
entire stable multi-target/Strike action once. ResourceFailure publishes an empty action before
center/target resolution and consumes no RNG.

The three-Strike/two-target sentinel consumes twenty-one individual random values under nine ledger
identities. One target dies on Strike zero and its later Strikes consume nothing; the other defends
once, takes two Injuries, fails Aim retention on the first, and therefore owns no second retention
site. Invalid duplicate target metadata and ResourceFailure both consume zero random values.

## Reaction activation execution ownership

The canonical post-action Reaction window now optionally executes under the battle owner. It
prevalidates every candidate and normalized definition before RNG, requires current observed
source/reactor identities, and rejects pre-supplied activation results. Only an eligible, aware,
affordable `ActivationRoll` candidate that survives native-cardinality eligibility samples one 3d6
site keyed by the outer ActionInstance, reactor, `ReactionActivation`, and native-order ordinal.
`AutomaticTrigger`, `SkillResponse`, and rejected candidates never sample that site.

The sentinel resolves four native-ordered candidates with three accepted Reactions and exactly one
activation draw: three random values under one ledger identity. A hybrid pre-supplied result fails
before the next ActionInstance can alter the ledger.

## End-of-turn Stun recovery ownership

One recovery executor binds the canonical end-of-turn Stun check to a stable turn ActionInstance,
current observed unit, exact Stun instance, and the battle-ledger `Recovery` site. A successful
standard roll removes only that snapshotted instance. A turn that did not begin Stunned neither
rolls nor removes a Stun applied during the current turn.

The sentinel succeeds on one 3d6 roll, removes the exact instance, and then proves that a same-turn
Stun under a different ActionInstance consumes no additional random values and remains present.

## Revive confirmed-execution randomness

The canonical Revive executor now optionally runs under the battle owner. It requires current
observed source/target identities and rejects pre-supplied casting, resistance, or restored-HP
results. A payable action samples the shared casting site, a reachable Internal Direct resistance
site when applicable, and only the exact restored-HP dice for a delivered Immediate route. Rejected,
ineligible, Stored Reraise, and ResourceFailure paths cannot sample restored-HP dice.

The immediate Raise sentinel consumes five random values under three identities: one shared casting
3d6 and two healing dice. ResourceFailure and hybrid injected-roll controls leave the ledger
unchanged.

## Status, removal, Dispel, and Quick execution ownership

Status Carrier, named StatusRemoval, Dispel, and Quick executors now optionally run under the
battle owner. Each requires current observed source/target identities, the exact battle-owned state
registry, and no pre-supplied random result. Internal Direct status samples one shared casting site
and one target resistance site only after a successful nonimmune gate. StatusRemoval and Quick own
only their shared casting sites. Dispel reuses one casting draw and assigns one stable Resistance
draw index to each selected EffectInstance in instance order.

Offline sentinels prove status casting plus resistance at six values/two identities, immunity at
three values/one identity, Dispel at nine values/three identities for two effects, and StatusRemoval
and Quick at three values/one identity each. All state/CT mutations still commit before the single
Reaction window.

## Stored Reraise trigger

Stored Reraise now materializes a typed payload containing the canonical restored-HP expression,
Faith multiplier, and whether Faith modifies restored HP. The trigger executor requires a current
observed native-KO target and the exact persistent instance, samples only the stored healing dice
under a new lifecycle ActionInstance, consumes that instance once, and returns the native HP-credit
channel with an explicit HP-before-KO-clear contract. The trigger is not a damage event and opens no
Reaction window.

The coordinated application/trigger sentinel consumes five values under three identities: one
casting 3d6 when Reraise is stored and two healing dice when KO triggers it. It restores nine HP,
consumes the exact state instance, and stages no HP debit, MP channel, or Reaction.

At this checkpoint, every canonical executor that owns confirmed random sites has a battle-ledger
path. Remaining work is native carrier invocation/publication and presentation, not an unowned RNG
formula.

## Auxiliary native carriers

Canonical status application, named removal, per-instance Dispel, Quick, immediate Revive, and
Stored Reraise now project nonnumeric semantics beside their target Strike: exact state-instance
upserts/removals, exact CT credit, and HP-credit-before-KO-clear. One auxiliary coordinator resolves
and publishes each family atomically under the same battle catalog revision. The ordinary native
ledger then enforces target apply, distinct source payment, one Reaction, settlement, and retirement
in the same order used by numeric actions.

The Stored Reraise payload also retains its originating AbilityId. Its later KO trigger publishes a
LifecycleTransaction containing restored HP credit, exact trigger removal, and KO-clear ordering,
with an explicit no-payment step and no Reaction window.

Offline coverage consumes status, removal, Dispel, Quick, Raise, stored Reraise, and the Reraise KO
trigger through the full ledger. No auxiliary effect is inferred from a numeric result flag.

## Native/custom state apply boundary

`NativeStatusBit?` is now the executable storage discriminator from documents 08 and 19. A state
with a native bit produces one owned add/remove packet mutation; a state without one remains
mechanically registry-only. Authoring rejects byte indexes outside the fixed five-byte vocabulary,
multi-bit masks, and two semantic definitions claiming the same native bit.

The auxiliary apply planner resolves every projected state kind against the loaded canonical
catalog. It preserves unrelated packet bits, clears the owned bit from both lanes before writing,
and makes an add win when one action replaces an old instance with a new instance of the same
native overlay. Custom instance mutations, exact rational CT credit, and HP-before-KO-clear remain
separate typed channels. Fractional or greater-than-127 CT credit cannot silently enter the native
one-byte sign-magnitude carrier.

The proven pre-clamp apply path now stages these owned status packets at `+0x1DB/+0x1E0`, stages an
exactly representable positive CT delta at `+0x1D3`, and rolls back numeric, CT, packet, and result
flags together if native commit fails. Immediate Revive/Reraise keeps native HP credit as the owner
of KO clearing after positive HP restoration.

State application results now retain the exact removed instances as well as their ids. This closes
the semantic-loss case where opposed states share a StackKey but not a Kind. Direct-magic status
Riders carry the resulting instance mutations through target projection, ledger publication, and
native packet planning; they no longer stop at an effect index.

Offline sentinels cover native add, native remove, custom-only state mutation, native replacement,
unrelated-bit preservation, CT `+100 -> 0xE4`, rejection of fractional/128 CT, duplicate native-bit
ownership, exact old/new state kinds, and direct damage-plus-native-status Rider composition.

## Area status Riders

The Area vertical accepts one numeric Damage/Healing Carrier followed by normalized status Riders.
For each target, the Rider gate is reached only after that target's first landed Carrier Strike. It
reuses the shared caster roll, samples one resistance identity per Rider and target, materializes
each successful state exactly once before the outer Reaction window, and attaches its exact state
mutation to the Carrier Strike that reached the gate. Immunity and a target whose Carrier never
lands consume no Rider resistance RNG.

Single-Strike profiles support either application timing. Multi-Strike `Deferred` profiles are
closed offline. Multi-Strike `Immediate` profiles fail closed because committing all state after an
already-resolved batch would falsely claim that the state was mechanically visible to later
Strikes; that route requires a real between-Strike snapshot reprojection adapter before it can be
authored.

The three-Strike sentinel uses one casting identity, three magnitude identities, and exactly one
Rider resistance identity. Only the first Strike carries the native status addition, which plans
the state definition's exact `byte 1 / mask 0x02` packet mutation. An immune control removes the
resistance identity, and a three-times-defended control owns only casting plus three Dodge
identities, with no magnitude, Rider, state, or packet effect.

## Exact Area forecast and AI projection

The correlated Area forecast now models `StrikeCount` rather than treating a multi-Strike target as
one independent delivery. It conditions all targets on one caster roll, branches Dodge per Strike,
branches Quick Contest once per target, exposes the full delivered-target-count distribution, and
computes one Rider application probability after any Carrier landing. Rider immunity contributes
zero without inventing a resistance branch.

One normalized Area evaluation owns declaration legality, ResourceFailure, current center,
TargetBatch membership, exact magnitude expressions, per-target HP state, one-time Oil, Absorb and
healing caps, and KO short-circuiting across Strikes. Its output is projected losslessly into player
forecast ranges/percentages and AI expected values; neither projection samples execution RNG.
Expected executed Strikes come from the HP-aware magnitude state machine, not the larger number of
gates that would have landed if the target survived.

This slice also exposed and closed an execution divergence: `AreaDeliveryGate.None` had discarded
the shared roll's critical classification. It now preserves `CriticalDelivered`, and critical Area
healing rolls one fewer die plus the maximized six exactly as forecast. Offline sentinels cover
multi-Strike Dodge/Rider correlation, immunity, duplicate Rider rejection, low-HP KO truncation,
ResourceFailure-before-batch, player/AI projection parity, per-target healing caps, and the critical
`3d6+2 -> 2d6+8` execution route.

## Universal Injury-state commit

Confirmed physical, direct-magic, and Area-magic coordinators now resolve the canonical `shock`,
`stun`, and `knocked-down` definitions before consuming execution RNG. After the numeric Injury and
Major-Wound result are fixed, the outer transaction materializes Shock plus paired posture states
before the one Reaction window. Shock contributions from several Strikes merge into one stable
instance and retain the original target-turn expiry instead of refreshing it.

Each Strike result retains its exact Injury state applications. Native projection merges them with
status Riders rather than choosing one auxiliary source, so a damage-plus-status Strike can carry
custom Shock and an owned native status bit simultaneously. The apply planner keeps custom states
registry-only while staging any native overlay through its exact packet bit.

Multi-Strike `Immediate` Injury profiles fail closed. Their later defenses and state-derived inputs
cannot claim mechanical visibility until a between-Strike snapshot reprojection adapter exists;
`Deferred` multi-Strike and every single-Strike route are closed offline. Sentinels cover direct,
physical, and Area Shock materialization/publication, multi-Strike Shock merging, Rider+Shock
composition and native/custom apply separation. The coordinator also performs universal-definition
validation before its first random site.

## Exact source-payment apply and lethal overcast cleanup

The source-owned payment path now has an explicit native-pool plan rather than treating settlement
as an annotation on the target carrier. It validates the snapshotted current/max HP and MP pools,
rejects credits or a damage-Reaction flag, applies MP debit before HP debit, and derives the exact
post-payment pools. The projected payer-KO flag must equal the resulting zero-HP condition.

A lethal payment snapshots every exact state removed by the payer's target-KO and source-KO rules.
The managed commit revalidates that set before advancing the ActionInstance payment stage, then
commits payment acknowledgement and the two cleanup families together. A changed cleanup revision
fails closed while the ledger remains at `StrikesApplied`. Offline sentinels cover ordinary
`8 MP + 2 HP`, lethal `3 MP + 5 HP`, both KO-cleanup roles, credit rejection, and stale cleanup.
The native source HP/MP writer is still unbound; it must apply the validated `Before -> After` pools
before invoking this managed commit.

## Exact turn-expiry removals

Target/source turn completion no longer returns only dead instance ids. It snapshots and returns the
complete removed state instances, verifies that every removed id existed at the pre-commit boundary,
and can project their exact state kinds into native auxiliary removals. A sentinel expires one
target-turn native overlay and one source-turn custom state at the same completion boundary and
retains both identities. The proven native turn edge and the removal presentation/write remain to
be bound; the runtime therefore does not yet claim live scheduling integration.

The global scheduler likewise returns one exact commit record for every ordered tick and expiry.
Tick commits retain the pre-advance instance; expiry commits retain the removed instance. The
existing `tick before expiry at the same CT, then stable InstanceId` sentinel now verifies these
records as well as registry state. Resolving each tick's authored effect as its own outer
ActionInstance remains a separate integration gap because `TickProfile.EffectExpression` is not yet
normalized into an executable effect union.

## Deterministic state-registry checkpoint

The battle state registry now serializes a strict schema-revisioned checkpoint with stable
InstanceIds, current global CT, next identity, target-local revisions, source/target identity,
every duration/use/tick field, Strength/margin/stack identity, presentation identity, and a closed
typed-payload discriminator. State definitions are fingerprinted after deterministic normalization;
loading changed authoring fails rather than attaching an old payload to new mechanics.

Restore validates each duration shape, live next-tick/expiry, source requirement, singular stack
identity, StackToCap contribution identity, payload schema, and presentation revision. All outer and
nested UnitKeys are rebased from the saved generation to the newly observed battle generation.
Unchanged state serializes byte-identically; a sentinel round-trips a typed Aim payload and rejects
an altered definition. The native battle save/load owner remains to be mapped and bound, so the
checkpoint mechanism is closed offline but not yet persisted by the game.

## Exact physical forecast and AI projection

Physical evaluation now uses the same normalized action, item-owned damage expression, ranged
legality, defense selection, DR/divisor, and wound rules as confirmed execution without sampling
RNG. A target-local exact state machine enumerates every weighted 3d6 attack total, conditionally
selected defense, weighted defense total, and weapon-damage outcome. It carries the cumulative
Parry-attempt vector and one-use Block through later Strikes, so a first ordinary attack can make
the second Strike prefer Dodge while a first critical/miss leaves Parry unspent.

The evaluation evolves HP, stops later target-local branches on KO, and returns exact distributions
for raw Injury, applied HP loss, final HP, KO, outcome by Strike, selected defense by Strike,
expected hits, and expected reached Strikes. Player and AI projections are lossless views of that
same result. The four-Strike sentinel proves the first-Strike probability against the standalone
contest owner, correlated Parry/Dodge selection on Strike two, normalized final probability mass,
KO short-circuiting, and identical player/AI expected applied HP loss.

## Exact status forecast and AI projection

Unit-targeted status evaluation now mirrors the Internal Direct and Beneficial execution gates
without RNG. Internal Direct enumerates one shared caster total and only the resistance totals
reachable after BaseSpellScore/TargetSpellScore success and immunity. It returns exact delivery
outcomes, the unconditional winning-margin distribution for successful applications, and the same
outcome-dependent MP/HP settlement used by numeric magic. Player and AI projections consume that
single result.

The sentinel matches application probability against the standalone exact Internal-success owner,
proves that winning-margin mass equals application mass, proves player/AI parity, and proves that
immunity and ResourceFailure create no invented resistance/margin branches. Turning winning margins
into duration still depends on normalizing the state's authored duration formula into an executable
policy rather than interpreting its current opaque string ad hoc.

## Exact Dispel forecast and AI projection

Dispel evaluation now mirrors the execution owner's target/family/source selection policy and uses
the exact current registry instances, stable InstanceIds, and stored EffectStrength values. It
enumerates one weighted shared caster total, terminates failed outer gates before resistance, and
then branches one independent weighted resistance total for each selected effect. The resulting
distribution is over complete removed-instance sets, so the correlation caused by the shared caster
roll is retained instead of approximating the effects as independent.

The shared result also exposes per-instance removal probability, removed-count probability, expected
removed count, any-removal probability, and outcome-dependent source payment. Player forecast and AI
receive lossless views of the same distribution. Sentinels prove normalized mass, easier removal of
the lower-Strength effect, non-independence of the joint-removal probability, equality between the
expected count and the sum of marginals, projection parity, and ResourceFailure before any casting
or resistance probability space. Native player/AI carrier binding remains separate.

## Exact Quick forecast and AI projection

Quick evaluation now validates the normalized CT magnitude and QuickLock definition against the
current target CT, persistent registry, and lock controller before constructing probability. A
controller/state disagreement fails closed; an existing matching lock or a CT grant that cannot
reach turn eligibility is an illegal use rather than a low-probability cast.

For a legal payable use, one exact Beneficial caster space owns delivery, outcome-sensitive source
payment, the `0 | authored CT grant` distribution, expected CT credit, and lock-application
probability. Player and AI projections are views of that result. Sentinels match the standalone
Beneficial probability, normalize CT mass, prove expected CT, reject ResourceFailure before the
caster space, and reject an active QuickLock without inventing a success chance. Native CT/turn,
forecast, and AI callbacks remain unbound.

## Exact Revive and Reraise forecast and AI projection

Lifecycle evaluation now validates the same normalized revive mode, target eligibility, Undead
family, delivery kind, restored-HP expression, Faith axis, and optional stored-state ownership as
confirmed execution. One exact Beneficial or Internal Direct delivery space feeds explicit route
probabilities for delivery failure, Undead rejection/effect ownership, immediate HP credit, and
Stored Reraise.

Only reachable immediate-credit probability mixes in the restored-HP dice. The evaluator returns
raw, Faith-adjusted, and missing-HP-capped distributions plus exact positive-credit/KO-clear and
effect-application probability. Stored Reraise instead returns trigger-storage probability and
retains the authored expression without rolling or crediting HP. Player and AI share those route and
value distributions. Sentinels cover exact `2d6+2`, `3/2` Faith flooring, cap, normal Raise,
Undead rejection before magnitude, ResourceFailure before delivery, and Reraise storage without an
early HP roll. Native lifecycle forecast/AI binding remains separate.

## Exact named StatusRemoval forecast and AI projection

Beneficial named-removal evaluation snapshots every matching current InstanceId from the target's
registry revision, then uses one exact Beneficial caster space to produce either the complete
matching removal set or no removal. It exposes removal probability and expected removed count with
the same outcome-sensitive source payment as execution. An absent state has one normalized empty
outcome with zero removal chance; ResourceFailure has no delivery or instance-outcome space.

Player and AI projections retain the same exact snapshot/outcome objects. Sentinels cover present,
absent, normalized mass, projection parity, and ResourceFailure. Native forecast/AI binding remains
separate.

## Transactional native source-payment writer

The source-payment writer now consumes only a prevalidated payment plan and an exact native-pool
reader/writer boundary. It rejects a stale before-snapshot, requires MP-first ordering, writes MP
then HP, confirms the complete after-snapshot, and rolls back every completed earlier write when a
later write or readback fails. A no-payment plan performs no write. Native pool validation accepts
`MaxMP = 0`, which is a legal FFT unit state and must not block a zero-cost physical or system
action.

Sentinels cover ordinary `8 MP + 2 HP`, ordered writes, synthetic HP-write failure with MP rollback,
and a zero-MaxMP no-op payer. The writer is deliberately not called from the existing pre-apply
target callback: native target effects have not finished there, so self-target actions would invert
the canonical `target effects -> source payment` order. Binding requires the proven post-target-
apply boundary; managed payment/KO cleanup commits only after this writer succeeds.

## Executable normalized state duration

`Duration.Formula` is no longer accepted as arbitrary text. The compiler recognizes only a positive
fixed integer, the explicit `resolved-at-application` boundary, or the full clamped winning-margin
shape `clamp(min, base + floor(margin / band), max)`. It validates positive bands and ordered bounds;
an incomplete/unclamped expression fails authoring validation.

Every registry application resolves the normalized rule again. Fixed or winning-margin states reject
a caller-supplied duration that differs from the exact result; external materialization still must
provide positive units, while permanent/command states reject units. Status evaluation maps the
unconditional exact winning-margin mass into duration mass and exposes expected duration to both
player and AI projections. Sentinels cover parsing, bounds, fixed mismatch, a two-margin exact
distribution, external fixed-six status projection, and normalized commit behavior.

## Transactional periodic-effect execution

`TickProfile` now names a normalized `PeriodicEffect` action, its exact native carrier ability, the
positive GlobalCT interval, the source policy, and whether a distinct immediate payload exists. The
authoring registry rejects missing/non-periodic actions; the canonical runtime catalog additionally
rejects an absent or mismatched ability binding. Periodic actions are zero-cost, unit-targeted, and
consume no action, movement, CastCT, or concentration.

The battle scheduler now has begin/commit semantics. Begin reserves one retry-stable
ActionInstance without advancing the state cursor. Commit accepts only the exact application held by
the battle's native-action ledger after every strike, payment, Reaction policy, and settlement; only
then does the registry advance or expire the state. Immediate payloads use their own idempotent
begin/commit lifecycle and cannot be recreated after settlement. Offline sentinels prove the missing
carrier rejection, explicit ability/action/source identity, retry stability, no early cursor
advance, distinct immediate/scheduled identities, exact settlement gating, retirement, and one-time
immediate completion.

## Unified GlobalCT timeline

The battle now has one transactional scheduler above charged declarations, periodic state events,
and unit CT clocks. It advances every registered clock with exact Normal/Slow/Haste/Stopped gain,
selects the earliest due timestamp, and reserves only one next step. At a shared timestamp it orders
charged ActionInstances first, then the state registry's tick-before-expiry stream, then eligible
turns by descending CT, initial initiative rank, and stable slot. A granted turn blocks further
GlobalCT until the exact unit completes it.

Charged entries preserve their declaration ActionInstance and ability binding, reject duplicate
source Charging, and clear only after the exact ledger application settles or an explicit
cancellation. Sentinels compose one same-CT charged delivery, periodic tick, two tied turns, tick at
expiry, and final expiry; they prove stable retry identity, no early state cursor, CT reset isolation,
active-turn blocking, ledger retirement, and the canonical priority chain.

Quick now composes with that timeline rather than remaining a detached CT helper. Its explicit CT
grant makes the target immediately eligible at the current GlobalCT, while QuickLock remains in both
the controller and persistent registry throughout the granted turn. Timeline completion clears both
owners together and fails closed on disagreement. The sentinel proves the same-CT grant, CT reset,
exact lock InstanceId removal, and no surviving controller or registry lock.

Charging interruption now targets the timeline's exact source-bound declaration. KO, Stun,
Knocked Down, explicit incapacity, and a failed concentration result map to explicit cancellation
reasons; a preserved concentration result retains the same ActionInstance and due CT. Cancellation
removes no pools, creates no target delivery, and cannot race a cast already reserved for delivery.
Sentinels cover failed concentration, absence of a later due action, preserved identity, and
voluntary cancellation through the same boundary.

## Timeline checkpoint and restore

A strict timeline checkpoint now layers over the state-registry checkpoint. At a settled
between-event/between-turn boundary it captures GlobalCT, the next ActionInstance id, exact
fractional CT, CT rate, initiative rank, completed-turn serial, persistent states/revisions,
QuickLock ownership, and complete pending charged declarations. Restore creates a new battle
generation, rebases every UnitKey, revalidates current authoring/ability bindings and declaration
shape, restores the identity cursor, and reconstructs QuickLock controller ownership from exactly
one matching state per registered unit.

The codec rejects pending timeline steps, active turns, native actions in flight, controller/state
QuickLock disagreement, missing registered lock targets, mismatched saved generations, stale
authoring, duplicate Charging sources, and an ActionInstance cursor that could collide with a saved
cast. Sentinels prove deterministic JSON, exact `77.5` CT, Slow/Haste rates, turn serial, state id,
QuickLock, charged source/target/due CT, generation rebasing, cursor continuation, and unsafe-save
rejection. Native pool/equipment/map persistence remains owned by the native loaded-battle snapshot
and still needs its synchronization binding.

The checkpoint schema also preserves each registered weapon resource's stable key, balance,
readiness property, current Ready/Unready value, and post-Unbalanced-attack Parry suppression.
Restore rejects impossible AlwaysReady/Unready and Balanced/suppressed combinations. Movement and
Action resources remain intentionally absent because active-turn saves are rejected and the next
turn grant resets both resources.

## Battle-owned active-defense cadence

The physical executor previously advanced repeated-Parry and Block state only in a local resolver
copy. Its returned transaction started from the input snapshot again, and the battle runtime had no
owner for the state between separate incoming ActionInstances. This meant one Barrage modeled its
own later Strikes correctly, but a subsequent attack could incorrectly see an unspent first Parry
and available Block unless an external caller manually threaded the values.

Defense snapshots now carry a monotonic revision. The battle runtime owns cumulative Parry-attempt
counts per unit and weapon/limb plus one Block-available flag. The physical coordinator captures and
validates that canonical snapshot before any execution draw, the executor returns its exact final
resources and copies them into the outer transaction, and one batch commit validates every target
before mutating any of them. A stale snapshot, decreasing Parry counter, restored Block, or duplicate
target fails closed. An action that spends nothing does not manufacture a revision. The defender's
`BeginTurn` boundary clears every Parry count, restores Block, and advances the revision so an action
resolved across the boundary cannot commit stale defense state.

The timeline checkpoint schema now includes defense owner identity, revision, Block availability,
and every nonzero Parry counter. Restore rebases the owner UnitKey and rejects negative/zero persisted
revisions, malformed counters, duplicates, and state for an unobserved unit. Offline sentinels prove
that the coordinated Barrage's selected Parry reaches the executor result, settled transaction, and
battle state; a later Block spend rejects a stale replay; the defender's next turn resets both
resources; and a nondefault two-Parry/spent-Block state survives deterministic timeline save/restore.

## Exact Reaction evaluation

Confirmed Reaction execution already owned ordered candidates and battle-ledger RNG, but the exact
player/AI layer stopped at the isolated activation formula. A canonical window evaluator now takes
the same outer ActionInstance and candidate set, rejects sampled activation rolls, preserves native
order, validates normalized effect-action references, and resolves every pre-activation blocking
reason. AutomaticTrigger contributes probability one, SkillResponse contributes probability one
while retaining its downstream natural effect gate, and ActivationRoll contributes the exact 3d6
mass from its single authored reference plus modifier. Rejected trigger, eligibility, native
cardinality, awareness, cost, or use gates contribute zero before any random site.

The base-reference projector selects exactly current DX, HT, IQ, Will, or one named Skill from the
normalized definition; it has no raw-Brave path and does not choose the best attribute. Player
forecast receives rounded display percentages plus the blocking/natural-gate reason, while AI gets
the lossless rational probability per reactor/Reaction and total expected accepted activations.
Sentinels prove ordered AutomaticTrigger and SkillResponse certainty, a score-11 ActivationRoll at
`135/216`, a native-cardinality zero, total expectation `21/8`, exact reference projection, and
strict rejection of execution RNG in evaluation.

Reaction `Binding` is now executable rather than validation-only. Every candidate can retain the
exact target result that triggered it plus explicit identities when the authored mode requires
them. Resolution materializes ReactorToSource, ReactorToTarget, SourceToReactor, or Explicit into a
generation-safe source/target pair before native dispatch; missing trigger targets, partial explicit
routes, foreign generations, and explicit identities on nonexplicit modes fail closed. Confirmed
results carry the route only when accepted, while forecast/AI retain it for prospective targeting.
Sentinels cover all four modes and prove that an Area candidate cannot drift to another enumerated
target.

## Immediate Injury reprojection between Strikes

The physical and Area Damage executors now distinguish authored `Immediate` from `Deferred`
consequences without creating an intermediate Reaction. A surviving Major Wound consumes its
target-local HT roll at the originating Strike. On failure, local Stun and Knocked Down flags alter
only later defenses against that same target: Stun subtracts four from every physical defense,
Knocked Down additionally subtracts three from Dodge or two from Parry/Block, and Area per-Strike
Dodge receives the combined seven-point penalty. Shock remains accumulated for its target-turn
effect and does not change active defense inside the combo.

The exact physical and Area evaluators enumerate the reachable 3d6 HT mass instead of sampling or
using an independent-hit approximation. They carry posture in the correlated HP/defense state,
short-circuit after target KO, and recompute only later defenses. Immediate multi-Strike evaluation
requires exact MaxHP/HT consequence inputs; unsupported delivery families and generic Immediate
status Riders continue to fail closed.

Offline sentinels use two-Strike profiles where the first delivered Injury is a surviving Major
Wound and the HT result collapses the target. In the physical route, the second Dodge score falls
from 12 to 5; in the Area route, the second per-Strike Dodge gate receives the same `-7` and lands.
The exact forecasts assign the matching higher later-Strike hit/debit expectation to Immediate than
Deferred, and the complete smoke suite passes with one outer Reaction window.

## HP/MP ResourceChange and Drain

The authoring union previously accepted `ResourceChange` with a fixed magnitude but had no explicit
direction, source transfer, or Undead-source semantics. A normalized route now distinguishes target
credit, target debit, and target-to-source Drain. The target effect owns its existing Undead row;
Drain adds an independent source-Undead row. Generic handling is limited to one HP/MP Carrier with
no implicit Riders. CT remains owned by CTChange/Quick, while Other and EffectOwned require a named
mechanism executor.

The pure resolver treats these as pool mutations rather than Injury. It caps target debit/credit,
derives Drain transfer only from applied target debit, caps source credit/debit separately, and
reports target/source KO. Exact enumeration retains the correlated magnitude distribution and can
mix it with the full direct-magic delivery space, so forecast and AI share target/source expected
channels, rejection probability, and KO probability without sampling.

Confirmed direct execution owns one semantic `ResourceMagnitudeDie`. Its native plan carries target
channels, an optional source-effect carrier, and the independent casting payment. The application
ledger enforces target Strike, source effect, payment, one Reaction, settlement. Source-effect and
payment planners validate exact pools and KO-cleanup instances before mutation; transactional
writers confirm native readback and roll back partial HP/MP writes. Sentinels prove a capped `1d6`
HP drain, exact expectations, target KO, explicit Undead target/source inversions, MP restoration,
coordinated draw cardinality, source transfer before a zero-cost payment, and final ledger retirement.
An Internal Direct variant additionally proves exact conditional Quick-Contest enumeration and the
confirmed casting/resistance/ResourceMagnitudeDie cardinality.

## ForcedMovement and the settled map verdict

The effect union previously named ForcedMovement without a distance/direction schema or a shared
map-result owner. Generic authoring now records positive tiles and AwayFromSource, TowardSource,
SourceFacing, or a normalized ExplicitVector. Multi-Strike Immediate profiles fail validation until
later-Strike position reprojection exists. Native map logic remains authoritative for obstruction,
height, edges, landing, and falls.

One immutable native movement verdict now crosses forecast, AI, confirmed execution, projection,
and apply. The pure resolver rejects target/origin/distance/direction drift, distinguishes a blocked
zero-tile result, and short-circuits KO before requesting map work. Positive settlement marks Aim
cancellation and a concentration incident; attempted movement that resolves to zero marks neither.
The auxiliary writer validates the origin before invoking native movement and the exact destination
afterward. It never iterates the route or opens a per-tile hook.

The standalone executor owns External Projectile, Internal Direct, or Beneficial delivery,
ResourceFailure-before-RNG, exact payment, one settled target transaction, and one Reaction window.
Its evaluator shares exact delivery/resource mass and expected moved tiles with player/AI.
Sentinels prove explicit-vector validation, Immediate fail-closed, blocked and KO paths, stale map
verdict rejection, forecast/AI parity, confirmed ledger draw cardinality, movement readback, and
payment/Reaction only after the complete displacement. Critical knockback and damage Riders still
need to be routed through this carrier rather than only carrying a tile count.

## Native post-target apply boundary and source settlement

The canonical target apply router was reachable only after an action had already been published,
and `Mod.cs` had no boundary for the later source effect or payment. Static disassembly of the
current Enhanced executable shows that state-apply starts at `0x30A484`, writes/clamps target HP and
MP at `0x30A62B`/`0x30A634`, runs its status/lifecycle tail, and converges at epilogue
`0x30AB4D`. The entry's preserved `r14d` is the exact `0..20` target slot. The installed-build
anchor audit accepts bytes `48 8B 5C 24 60 48 8B 6C 24 70` at that convergence.

A new terminal post-apply queue reserves only the last target/Strike before ledger commit, derives
the exact expected target pool, and consumes the ticket only when the epilogue observes the same
battle generation, UnitKey, action/application, Strike order, and HP/MP readback. The coordinator
then commits Drain/source ResourceChange and action payment in separate source-pool transactions.
It deliberately leaves Reaction acknowledgement, presentation, settlement, and retirement for
their later native boundaries.

The live hook is guarded by `DclCanonicalPostApplyEnabled` and defaults off. A direct current-HP
write does not own native KO lifecycle; the callback therefore rejects any source effect or payment
whose projection reaches zero HP before mutating the source. Offline sentinels cover precommit
ticket reservation, exact target readback, source transfer, payment, later Reaction acknowledgement,
retirement, stale-stage rejection, settings gating, and the complete smoke suite. Remaining proof:
publish a real canonical execution from native snapshots, live-prove this epilogue once on a
nonlethal action, and bind a true native lethal source-pool carrier before lethal overcast or an
Undead-source drain debit is enabled.

## Native terminal Reaction-window convergence

Pass-2 commit and state `0x2C` cannot close the canonical window: pass 2 has no row when no Reaction
is accepted, while `0x2C` counts delivered execution transactions and repeats for Dual Wield. Static
control flow supplies a later universal convergence. Dispatcher state `0x2F` enters trace handler
`0xD90CDD2`, calls the complete three-pass queue at `0xD90CF99`, and branches away when the queue
returns one. A zero return falls through to `0xD90CFA2`, the sole direct caller of post-chain cleanup
`0x206050`; cleanup then reaches owner `0x205F28` and dispatcher state `0x28`.

The handler runs after the ordinary outer action and again after delivered Reactions, so the same
fallthrough represents an empty first scan and an exhausted multi-Reaction chain. A dedicated
analyzer validates the state/thunk/queue/cleanup/resume graph and exact bytes. The canonical runtime
now prepares one completion ticket before source effect/payment commit, exposes it only once the
application is payment-committed, and at the empty-queue hook acknowledges the declared Reaction
window, settles, and retires the action. The guard requires state `0x2F`, the current battle
generation, and a singleton exact ledger application. It cannot require actor/source equality
because the final reactor owns the execution actor after a chain.

`DclCanonicalReactionCompletionEnabled` is disabled by default and composes only with the canonical
runtime plus post-apply owner. Battle reset/disposal clear its ticket. Offline coverage includes an
empty-or-chained terminal scan represented by one completion commit, exact single retirement,
duplicate/stale rejection, settings gating, static anchor audit, and the full smoke suite. Live proof
still waits for the missing native canonical execution publisher.

## Core Reaction reachability and exact native effect identity — 2026-07-20

The core physical, direct-magic, and Area coordinators opened their transactions with no
`DclCanonicalReactionWindowRequest`, so their native projections could never contain the
Reaction result even though the transaction layer supported it. Each confirmed coordinator now
constructs the exact battle-owned request, including an explicit empty candidate set, and passes
the resulting window through execution, projection, publication, and the native action plan under
the same outer ActionInstance. Offline sentinels prove one accepted physical candidate and empty
direct/Area windows without changing automatic/empty RNG cardinality.

The native completion boundary cannot identify a logical Reaction from `ActionId` alone. Static
and earlier live evidence separate `actor+0x18C`, which retains the Reaction presentation/dispatch
ability, from `actor+0x142`, which retains the executable effect ability. A new strict native
Reaction binding bundle therefore records `ReactionId`, both numeric native ids, and the exact
effect `ActionId`/profile revision. It loads atomically after the ordinary ability bindings and
rejects missing Reaction coverage, unknown JSON, duplicates, nonexistent native records, stale
effect revisions, and an effect ability bound to a different Action. Runtime settings now require
the fourth bundle path.

The native action plan retains the exact binding with every Reaction candidate. Accepted results
must have the matching effect Action and route; rejected results must have neither. Effect
completion no longer increments once merely because a Reaction id was seen. It consumes one exact
effect Strike at a time in accepted native order, matching the observed reactor plus both numeric
ids. The four-Strike physical sentinel produces four acknowledgements but increments accepted
Reaction completion only once. Wrong effect id, early terminal acknowledgement, duplicate order,
and stale ownership fail closed.

The current-build state-`0x2C` boundary at `0x212C2E` now has a guarded managed hook behind
`DclCanonicalReactionEffectCompletionEnabled`, disabled by default. It reads the actor-linked unit,
both native ids, and acknowledges only the singleton payment-committed completion ticket. The
observe-only probe cannot be enabled at the same RVA. The terminal state-`0x2F` hook accepts the
application in `PaymentCommitted` or `ReactionOpened` stage but still refuses to settle until every
accepted effect Strike is complete. Build and complete smoke pass offline. Managed live proof waits
for a real canonical execution publisher and deployed approved binding artifacts.

## Reaction-window closure across auxiliary action families — 2026-07-20

The first reachability fix covered physical and direct/Area magic only. Status application, named
status removal, Dispel, Quick, Revive, and standalone ForcedMovement still opened the transaction's
Reaction stage without supplying a window request, so their execution results and native plans
carried no candidate result. Confirmed auxiliary coordinators now construct the same exact
battle-owned request, including an explicit empty candidate list, and reject a caller-supplied
battle or window owner.

Each auxiliary executor now retains the resolved window result and each auxiliary native projector
passes the same object to the action plan. ResourceFailure remains pre-window and draw-free. The
separate Stored-Reraise KO trigger remains intentionally no-payment/no-Reaction. Offline coverage
includes a status candidate rejected before activation, empty confirmed windows for status,
removal, Dispel, Quick, Revive, and ForcedMovement, reference-identical execution/projection plans,
and unchanged execution RNG counts. Build and full smoke pass.

## Reaction preflight atomicity — 2026-07-20

Transaction commit originally validated the Reaction window only after effect callbacks and state
mutations had run. A malformed candidate could therefore throw after part of the action was already
committed. Coordinators that own RNG also needed the same validation before their first draw, not
merely at final transaction commit.

The Reaction window now exposes a draw-free preflight that validates battle/runtime/source
ownership, stable unique candidate order, current observed reactor generations, unique
reactor/Reaction pairs, normalized Reaction and native binding presence, exact effect route, and
activation-mode roll/reference cardinality. Physical, direct-magic, Area, status application,
status removal, Dispel, Quick, Revive, and ForcedMovement all invoke it before their first reachable
RNG or mutation; the transaction layer repeats it as a final invariant guard.

The regression sentinel submits an unknown Reaction to a confirmed coordinated status action and
asserts unchanged execution-draw count, random-source count, state-instance count, and empty native
application ledger. The complete build and smoke suite pass.

## Strict native Reaction-window publication — 2026-07-20

The native plan previously allowed `ReactionWindowOpened=true` with a null window result. That
shape made an empty-but-resolved window indistinguishable from a caller that forgot to propagate
the execution result, and terminal settlement silently treated both as zero accepted effects.

Plan validation now requires the declared-window flag and resolved result to be present or absent
together, with the existing same-ActionInstance and exact candidate/binding/route checks applied to
the present case. Coordinated production paths already satisfy this invariant. Lower-level carrier
sentinels now resolve explicit empty windows before publication, and a negative sentinel proves the
flag-only shape cannot enter the battle ledger. Build and full smoke pass.

## Typed Taunt and defensive-state mechanics — 2026-07-20

Taunt, Guard Broken, Weapon Bound, and Bulwark already had typed payloads and checkpoint support,
but consumers still lacked one immutable mechanics projection. The canonical state snapshot now
resolves Taunt Movement/Action legality, universal-normal-Attack identity, provocateur targeting,
native target legality, and invalid-provocateur expiry without interpreting descriptive text.

The same snapshot revision now resolves Guard Broken Block suppression and Parry penalty, exact
per-equipment-slot Weapon Bound attack/Parry/Reaction suppression and Weapon Skill modifier, plus
Bulwark Block, DR, displacement-resistance, and passability terms. Location DR consumes Bulwark's
modifier and floors the combined result at zero. Sentinels cover allowed Movement, rejected wrong
Action, exact provocateur Attack, stale-source expiry, the complete defense record, and equipment
DR plus Bulwark DR. Fear remains deliberately untouched. Build and full smoke pass.

## State-aware physical planning and confirmed Taunt gate — 2026-07-20

The typed state record is now consumed rather than merely exposed. A battle-owned physical state
plan captures source/target custom-state revisions and transforms one deterministic Strike list for
both exact evaluation and confirmed execution. Source-slot Weapon Bound applies its exact Weapon
Skill modifier or rejects attacks from that slot. Target Guard Broken/Weapon Bound alter only
Parry/Block legality and score, Bulwark alters Block and DR, and Dodge remains unchanged.

The player and AI physical projections are produced from that same transformed list. Taunt is
checked before evaluation or execution RNG: a non-normal physical skill produces no evaluation,
forecast, AI result, random draw, or native application. The explicitly identified universal
normal Attack remains legal only against the current observed provocateur with legal native range,
vertical, and trajectory verdicts. Direct magic, Area, status application/removal, Dispel, Quick,
Revive, and standalone ForcedMovement confirmed coordinators now apply the same pre-RNG Taunt gate.

Sentinels cover wrong-action rejection, legal provocateur Attack, player/AI suppression, exact
Weapon Skill/Parry/Block/DR transformation, slot-bound attack suppression, and unchanged RNG/native
ledgers. Fear remains untouched. Build and full smoke pass.

## Confirmed system commands and transaction-wide state revisions — 2026-07-20

Ready, Reequip, and Stand Up previously executed only against caller-owned resource objects. A
confirmed semantic coordinator now resolves the source snapshot, registered weapon state,
battle-owned turn and defense resources, Taunt legality, deterministic mutation, and explicit
Reaction request under one battle owner. Stale source state, stale defense state, unknown weapon,
Taunt, and invalid Reaction metadata fail before command mutation. The commands remain normalized
system actions rather than fabricated native ability ids; native menu/result carriers remain a
separate proof gate.

The transaction executor now accepts the confirmed battle state registry and validates every
target snapshot revision before planning or commit callbacks. Confirmed direct, Area, physical,
status/removal/Dispel/Quick/Revive, ForcedMovement, and system-command paths pass that registry.
Their coordinators reject stale candidates before RNG where they own sampling. Fixtures now use
the actual revision of their battle registry instead of synthetic revision labels.

This invariant exposed premature Aim mutation in physical and Area resolution: failed retention or
direct cancellation removed Aim while the action was still planning. Aim retention/cancellation
now has a pure planning form. Multi-Strike resolution carries a prospective target-local Aim flag,
and the exact instance removal happens only inside the successful outer commit. Later Strikes stop
checking after a prospectively failed retention without changing registry state early. Regression
sentinels cover stale auxiliary, command, Area, ForcedMovement, direct magic, physical, and central
commit paths with unchanged RNG/mutation ledgers. Build and full smoke pass.

## Shared nonphysical Taunt planning gate — 2026-07-20

Nonphysical confirmed coordinators rejected Taunt before execution RNG, but family forecast/AI
callers still had to remember the same gate independently. A generic RNG-free planning coordinator
now validates current source/target identities and target state revisions, evaluates Taunt once,
and invokes the exact family evaluator only when legal. An illegal action has no evaluation object;
a legal action returns the same evaluation object used by both player and AI projections. The
sentinel proves the evaluator callback is never invoked under Taunt and that legal planning retains
reference identity. Fear remains untouched. Build and full smoke pass.

## Slot-bound Weapon Bound Reaction suppression — 2026-07-20

Weapon Bound exposed `SuppressWeaponReactions`, but Reaction candidates had no weapon-slot
provenance and neither confirmed resolution nor evaluation consumed it. The candidate contract now
has an optional exact equipment slot. Battle-aware state projection reads the reactor's current
typed Weapon Bound payload and clears eligibility only when the candidate slot matches.

All confirmed coordinators now construct their request through this projection before Reaction
preflight. Battle-aware Reaction evaluation projects the same candidate set before player/AI
probability enumeration. Candidates without weapon delivery or from another slot are unchanged;
blank slots fail closed. The sentinel proves main-hand suppression, off-hand preservation, and zero
activation probability. Producing the exact slot from the native Reaction carrier remains a live
integration gate. Build and full smoke pass.

## Bulwark semantic boundary and direct-magic Aim atomicity — 2026-07-20

The canonical Bulwark section defines `DisplacementResistance` and `PassabilityPolicy` as authored
payload fields and requires selection/path preview to expose the result, but it does not define a
displacement contest, numeric formula, or concrete map-passability behavior. Repository-wide
search found no second canonical owner. The typed projection remains valid; consuming either value
is an explicit design/native-owner gate rather than an implementation guess. Fear remains
untouched.

The transaction-wide state-revision audit found one remaining asymmetry: direct magic resolved and
mutated target Aim only after its canonical transaction had already committed. Direct magic now
plans cancellation/retention without registry mutation, samples the reachable Aim-retention site
before commit, and removes the exact planned instance only inside the successful commit callback.
Its result and native auxiliary projection retain that same plan. Build and the complete formula
runtime smoke suite pass with the existing exact draw-count and removed-instance sentinels.

## Versioned Character Growth persistence — 2026-07-20

The universal Character Growth mechanism now has strict per-character persistence without
authoring any real job vector. The deterministic JSON record owns the schema revision, monotonic
highest-awarded level, and all six signed 64-bit micro-unit accumulators in fixed order. Loading a
current record rejects unknown fields, duplicates, missing channels, invalid levels, and progress
outside one retained step. Saving the same decoded record is deterministic.

An unknown newer schema is deliberately not decoded or normalized: growth is visibly disabled and
the original JSON is retained byte-for-byte for the next save. This prevents an older mod from
discarding future progress. A known older schema still requires an explicit migration. Stable
native roster identity and atomic native permanent-stat/save writes remain integration gates.
Build and full smoke pass.

## New area-schema closure and direct-magic Block cadence — 2026-07-20

The authoring audit found two stale generic enum values from before the new DCL: `Explicit` area
center and `Explicit` area delivery gate. Documents 12 and 19 define only
TrackedUnit/FixedTile/Caster and None/Dodge/QuickContest respectively, while no executor consumed
the stale values. Both values are removed from the strict schema, and JSON sentinels prove they
fail during loading instead of producing an unresolvable runtime profile.

The same audit found that direct External Projectile magic accepted a caller-supplied Block without
checking `Blockable` and never spent the defender's finite Block. A shared defense policy now
validates authored Dodge/Block legality, rejects ordinary Parry and stale defense snapshots, and
requires the current battle-owned Block. The confirmed coordinator spends Block on every reachable
noncritical attempt regardless of success, increments the defense revision, and rejects a second
attempt before RNG or publication. Exact evaluation exposes the correlated defense-attempt and
Block-spend probabilities to both player forecast and AI; critical delivery bypasses both. Build
and full smoke pass.

## ForcedMovement External Projectile Block cadence — 2026-07-20

The follow-up audit of every `ResolveExternal` caller found the same finite-Block ownership gap in
standalone ForcedMovement. Its confirmed coordinator now captures and validates the target's
battle-owned defense revision before RNG, rejects non-authored defense choices, and commits the
spent Block after every reachable noncritical attempt whether the defense succeeds or fails. A
second request with the old available-Block snapshot fails before casting/defense RNG or native
publication.

ForcedMovement evaluation now exposes exact defense-attempt and Block-spend probabilities through
both player and AI projections. The shared probability owner enumerates the correlated casting
roll once, so critical delivery correctly bypasses active defense and does not spend Block. Offline
sentinels cover the failed-Block spend, battle revision, rejected retry, spent-resource evaluation,
and exact projection parity. Build and full smoke pass.

## Touch delivery numeric gate — 2026-07-20

The canonical delivery audit found that `Touch` existed in the normalized enum but had no resolver,
and its validator still permitted Block even though documents 13 and 19 allow only authored Dodge
or Parry. Authoring now rejects Touch Block.

The new pure Touch gate treats SpellScore as the per-Strike attack skill, classifies the ordinary
3d6 physical outcomes, bypasses defense on critical attack, and selects only authored Dodge/Parry.
Named Parry resources apply their cumulative `-4` step before selection and spend once on every
reachable attempt whether the Parry succeeds or fails; a later resolution can therefore reselect
reusable Dodge. Exact enumeration shares hit, defense-attempt, and Parry-spend probability with
player and AI projections. Sentinels cover critical bypass, failed-Parry spending, defense
reselection, exact probability normalization, and invalid Block authoring. Effect composition,
outer transaction/resource settlement, and native Touch carriers remain the next family layer.

## Touch canonical action vertical — 2026-07-20

The Touch gate now composes through the direct numeric action family without adding a parallel
effect pipeline. A payable Touch samples `Attack` instead of `Casting`, conditionally samples one
active-defense draw, translates its physical outcome into the shared delivery carrier, then reuses
Damage/Healing/ResourceChange, status Riders, Injury/Aim, payment, target transaction, and the one
outer Reaction window. The result retains the original physical contest through native projection.

Confirmed coordination captures every named Parry resource from the battle, commits the updated
revision once, and rejects stale retries before RNG. `ResourceFailure` produces no attack, defense,
magnitude, Parry spend, effect, or Reaction. Exact canonical evaluation combines the same Touch
outcome mass with effect dice, caps, payment outcomes, and player/AI projections, including a named
Parry-spend probability.

Because Touch authors `NativeDirect` or `NativeArc`, both confirmed execution and evaluation now
require an immutable matching range-and-trajectory verdict. A false verdict suppresses forecast/AI
or rejects confirmation before RNG. Sentinels cover the complete failed-Parry damage vertical,
Shock, MP payment, empty Reaction window, native HP/state carrier, target/payment/Reaction
settlement order, physical-outcome retention, stale replay, ResourceFailure, exact forecast/AI, and
illegal trajectory. Native production of that verdict and the real Dodge/Parry candidates remains
the live integration gate. Build and full smoke pass.

## Strict delivery legality matrix — 2026-07-20

The post-Touch schema audit found that several profiles could pass normalization and then be
unconditionally rejected by their executor. The strict authoring contract now rejects ordinary
Parry on External Projectile, Block or resistance on Touch, active defense or a per-Strike target
gate on Beneficial, a second active defense on Rider, and resistance characteristics on Physical
Attack, External Projectile, Touch, or Beneficial. Internal Direct and Area QuickContest keep their
explicit resistance owners. Sentinels cover the newly closed External, Touch, and Beneficial
combinations. Build and full smoke pass.

## Physical status-Rider vertical — 2026-07-20

The delivery-family audit found that the canonical physical executor still required exactly one
Damage effect. Documents 08 and 18 require a landed physical Strike to resolve its ordered Riders
without rolling the Carrier again. The physical family now accepts one Damage Carrier followed by
status Riders. Each landed Strike reuses its Effective Skill and attack roll as the acting side of
the Rider Quick Contest; a miss, successful defense, immunity, or KO-short-circuited continuation
consumes no Rider resistance draw.

Rider state applications remain prospective through resolution and materialize inside the outer
commit before its single Reaction window. The exact state instance stays attached to the same
native Strike through auxiliary projection and ledger publication. Confirmed coordination owns all
reachable attack, defense, damage, Injury, Aim, and Rider draws. Exact evaluation adds per-Strike
Rider application marginals to both player and AI projections while retaining correlated Parry,
Block, HP, and KO state.

The generic physical owner currently accepts only a state whose authored resistance gate is
`QuickContest`. `None`, `SuccessRoll`, and `Explicit` fail closed rather than inheriting Quick
Contest semantics. Multi-Strike Immediate status Riders also fail closed because no between-Strike
status reprojection is defined. Sentinels cover applied and defended Strikes, exact forecast/AI
parity, durable state materialization, native projection, battle-owned conditional RNG, publication,
payment boundary, Reaction acknowledgement, settlement, and retirement. Build and full smoke pass.

## Authored Rider resistance gates and direct evaluation — 2026-07-20

The follow-up audit showed that the physical vertical above was too restrictive: the referenced
state already owns the Rider resistance gate, so treating every generic Rider as a Quick Contest
would silently contradict the authoring contract. The shared Rider resolver now derives and
executes `None`, `SuccessRoll`, or `QuickContest` from that state. Immunity is checked before the
gate; `None` consumes no target roll, `SuccessRoll` applies only when the target fails its roll, and
`QuickContest` retains the Carrier score and roll against the target. `Explicit` continues to fail
closed until a named mechanic owns it.

Direct, Touch, Area, and physical confirmed execution sample target RNG only for a reachable gate
that needs it. Their evaluation paths use the same authored gate. Direct and Touch evaluation now
expose exact per-Rider application probability while retaining the Carrier's casting or Touch
attack roll and every delivery-defense branch. Area evaluation uses the same gate semantics per
target after its first landed Carrier. Player projections round only at the presentation boundary;
AI projections retain the exact rational values.

Offline sentinels cover each generic gate, Quick Contest ties, immunity, Explicit rejection,
shared-roll correlation, delivery-defense suppression, Area gate probabilities, physical
per-Strike state projection, conditional battle-ledger RNG, and player/AI parity. Build and full
smoke pass.

## Atomic executable-family capability gate — 2026-07-20

The normalized schema intentionally describes more effect combinations than the runtime currently
implements. The audit found that a schema-valid profile could also acquire a native ability binding
and then fail only when a family-specific executor was called in battle. That violated document
19's requirement that forecast, AI, execution, and carrier identity all be consumable before a
profile enters the battle runtime.

Atomic runtime construction now classifies every bound ability as physical Damage, direct numeric,
Area numeric, status application, named status removal, Dispel, Quick, Revive, standalone
ForcedMovement, or an explicitly preserved native special. The classification validates the whole
ordered delivery/magnitude/effect shape, carrier kind, rewrite policy, supported within-action
timing, referenced Rider resistance gates, and the gate agreement between a standalone status
Carrier and its referenced state. The runtime retains that family for later native dispatch.

Schema-valid vocabulary with no complete owner remains authorable but cannot load as executable
content. Sentinels reject Damage plus ForcedMovement Rider, a numeric action using a managed-
producer rewrite, an unnamed Explicit Rider, multi-Strike Immediate status Riders, and a Beneficial
status Carrier that references a QuickContest state. Existing canonical sentinel bindings all
classify successfully. Build and full smoke pass.

## Canonical forecast/AI family dispatcher — 2026-07-20

The native integration audit found that the compute/apply hook could already consume a published
canonical Strike, but forecast and AI still lacked one family-level entry point. Calling each exact
evaluator directly would require the hook to infer the vertical again and risk divergent player/AI
routing.

One RNG-free dispatcher now resolves only the atomic ability-family classification and accepts that
family's exact evaluation input. It routes physical, direct/Touch, Area, status application, named
status removal, Dispel, Quick, Revive, and standalone ForcedMovement to their existing exact
evaluator, then returns the evaluator result together with both player and AI projections. An
explicitly preserved native special returns a typed passthrough instead of a fabricated DCL plan.
The dispatched ability id and the family input's ability id must agree.

Sentinels prove direct numeric result/projection parity, reject a mismatched family input object,
and reject an input carrying another ability identity. This closes family selection and keeps
native field capture/writing as the remaining integration concern. Build and full smoke pass.

## Canonical confirmed-execution family dispatcher — 2026-07-20

The execution side now mirrors the RNG-free family dispatcher. One entry resolves the atomically
classified ability family and accepts only its deterministic coordinator request. Physical,
direct/Touch, Area, status application, named removal, Dispel, Quick, Revive, and standalone
ForcedMovement route to the existing resolve-and-publish owner. The dispatcher returns the typed
resolution and the exact application published in the battle ledger; a preserved native special
returns passthrough without creating an application.

The dispatcher never accepts execution rolls. The battle owner remains responsible for every
reachable random site, state/resource commit, payment, application, and Reaction window. Physical,
direct, Area, and ForcedMovement requests already contain their Reaction candidates and reject a
second dispatcher list. Auxiliary families receive their candidate list only at the dispatcher-to-
coordinator boundary.

The direct numeric sentinel now runs through this dispatcher and preserves its exact damage, Injury
state, payment, empty Reaction window, RNG counts, and published carrier. Additional sentinels reject
a foreign family input, a mismatched ability id, and duplicate Reaction ownership before any draw,
state mutation, or publication. Build and full smoke pass.

## Source-owned native snapshot batch — 2026-07-20

The first native-input audit showed that the existing unit adapter was individually correct but left
the caller responsible for joining source allegiance, observed identities, equipment, state registry,
and defense cadence across multiple unit rows. A hook assembling those pieces independently could
mix revisions or retain a replaced character identity.

The new batch projector requires the current observed source row and unique current unit slots. It
derives equipment from the atomic catalog, state from the battle registry, and finite Parry/Block
resources from the battle cadence, then composes each unit through the existing strict adapter with
one source-team relation owner. Tile height and every non-equipment characteristic adjustment remain
explicit inputs. The batch never infers draft-job values from vanilla effective stats.

The sentinel proves one synchronized batch retains the exact canonical primary/secondary snapshot,
empty normalized equipment, source relation, and battle-owned Block availability. Replaced character
identity and duplicate slots fail before projection. The existing hybrid-pool sentinel continues to
reject native MaxHP/MaxMP that disagree with the supplied canonical characteristic model. Build and
full smoke pass.

## Native outer-action and TargetBatch admission — 2026-07-20

Static disassembly rejects the candidate order-record pointer as an ActionInstance key: it is the
reused source-unit record at `unit+0x1A0`. The result producer itself supplies the exact boundary.
Function `0x281CE8..0x282231` calls target-list builder `0x282754`; at `0x281EF7` its local 21-byte
list is complete and no target has reached `computeActionResult`. After the target loop,
`0x2821EC` increments repeat index `0x7B0763`, compares count `0x7B0762`, and publishes continuation.
`tools/analyze_dcl_outer_action_batch.py` guards every anchor and generated a passing current-build
report.

The battle runtime now owns a fail-closed native admission ledger. A monotonic sweep serial plus
state `0x2A` reserves one ActionInstance. A `NativeRepeat` reserves at index zero and reuses only that
identity across contiguous state-`0x2F` indexes with exact source, action, count, and target-batch
equality; a skipped index or changed field is rejected. Nonrepeat carriers require one normalized
Strike. Repeated native projections now use Strike-major/target-minor order, matching one complete
target sweep per repeat rather than the transaction model's internal target-major planning order.

`RandomFireRepeat` was removed from executable canonical capability. Its native selector chooses one
target immediately before each repeat, so the complete target sequence does not exist at the first
snapshot and cannot satisfy document 18. It remains legacy-mapped evidence, but canonical execution
will not load it until DCL owns and injects the full sequence. Sentinels cover ordinary admission,
four-repeat identity reuse, divergent-index rejection, RandomFire capability rejection, and repeated
Area carrier order. Build and full smoke pass.

## First admitted family composition: DirectNumeric — 2026-07-20

The first end-to-end native-family bridge now composes a nonrepeat DirectNumeric request from one
admitted outer sweep and the synchronized batch that contains its source, declared target, and final
resolution target. The composer derives IQ magnitude, current/max pools, Faith, native Silence,
focus skill/damage/healing/concentration/cost/CastCT modifiers, element affinity, Shell/Oil/Reflect,
equipment plus Bulwark DR, and Injury context exactly once. Fixed ResourceChange magnitudes use the
same snapshot mechanics without inventing Faith or element effects.

Tradition skill, learned/source/prerequisite access, Zodiac compatibility, chosen defense, and other
command/job-policy facts stay explicit. This avoids interpreting draft jobs while still providing a
complete deterministic coordinator input. Wrong family/shape, repeated or Area admission, missing
batch identities, stale UnitKeys, and hybrid pools fail before resolution.

The sentinel admits Fire, composes its request, dispatches it through the canonical confirmed family
entry, consumes only battle-owned RNG, and publishes the result under the exact same ActionInstance.
It also rejects a final target absent from the synchronized batch. Release build and the full smoke
suite pass.

## Admitted Physical, Area, and auxiliary composition — 2026-07-20

The native-family bridge now extends beyond DirectNumeric. PhysicalDamage accepts the complete
admitted sequence, including all NativeRepeat sweep indexes, and produces every target/Strike from
one synchronized source/target batch. It derives weapon metadata, source ST, target pool/state,
Injury context, location DR, and ranged Aim/Shock terms while keeping job Weapon Skill, defense
candidates, equipment resource key, route verdicts, and Riders explicit. Its four-Strike sentinel
preserves one ActionInstance through battle-owned execution and one publication.

AreaNumeric likewise consumes the complete admitted sequence. Native membership is frozen before
the first calculation and canonical allegiance/state filtering forms the TargetBatch. Source focus,
shared casting, target Faith/affinity/Shell/Oil/DR/Injury, per-Strike Dodge and magnitude, and
Strike-major publication all execute under the admitted identity. The three-Strike Area sentinel
consumes one shared casting site plus exactly three Dodge and three magnitude sites.

A common nonrepeat unit-target magic boundary now owns declaration identity, Silence, focus, cost,
CastCT, SpellScore, Zodiac, and pools. StatusApplication derives Will/immunity from the synchronized
target and materializes its exact state. Named StatusRemoval removes the exact matching registry
instance. AllEligible Dispel freezes the complete ordered eligible InstanceId set and resolves each
effect independently. Each end-to-end sentinel uses only battle-owned rolls and publishes under the
original admitted ActionInstance. Quick carries an explicit target clock and QuickLock controller.
Revive reads KO/Undead/current and maximum HP from one synchronized native row while retaining
explicit Faith/Undead lifecycle policy and optional Stored-Reraise materialization. ForcedMovement
retains the final immutable native map verdict, rejects a target/origin mismatch, and never opens an
intermediate-tile resolution site. All three now have end-to-end sentinels through dispatch,
battle-owned RNG, mutation, and publication. Native hook capture remains. Release build and the full
smoke suite pass.

## Classified native request composition — 2026-07-20

One entry now resolves the already-validated ability family and accepts only its exact explicit
policy-input record. It routes complete admissions and the synchronized batch to Direct, Physical,
Area, StatusApplication, StatusRemoval, Dispel, Quick, Revive, or ForcedMovement composition. It
does not inspect formula, animation, result kind, or object shape to invent another family. Mixed
ability admissions and a foreign input type fail before any coordinator request exists. The Direct
sentinel now enters through this boundary; a StatusRemoval-shaped input for Direct fails closed.
Release build and full smoke pass.

## Managed pre-calculation admission hook — 2026-07-20

The static boundary is now carried into a guarded managed hook at `0x281EF7`. Disassembly proves
that `r14` is the exact source order record (`unit+0x1A0`), `[rbp-0x28]` is the complete 21-byte
target list, and the function body has the stack alignment required by the reverse wrapper. The
hook preserves flags, volatile general registers, and `xmm0..xmm5`, then passes only those two
proven addresses to managed code before the first calculation.

The callback validates battle generation, execution state, source slot and character identity,
every target slot and character identity, action type, ability id, repeat count, and repeat index.
It supplies a monotonic sweep serial to the battle-owned admission ledger. Nonrepeat actions are
emitted immediately; `NativeRepeat` actions remain private until the final contiguous sweep, so no
family composer can observe a partial outer action. Any identity or continuity divergence clears
the pending capture and admission state and disables the hook for the process without mutating the
native target sweep.

`DclCanonicalAdmissionEnabled` is disabled by default and requires the canonical runtime. Anchor
bytes, installer gating, orphan-setting rejection, complete four-repeat capture, and exact retained
identity are covered offline. Release build completes with zero warnings/errors and the full smoke
suite passes. The hook has not been live-proven and deliberately does not yet compose or publish a
family request. The next integration gap is synchronized native family-policy input capture at this
boundary, followed by a narrow log-only live proof before enabling canonical result publication.

## Frozen native TargetBatch rows — 2026-07-20

The managed capture now freezes every complete `0x200` source and target row on the index-zero
pre-calculation sweep. This closes a repeat-action correctness hole: waiting for the terminal sweep
would otherwise expose HP, pools, and states already changed by earlier Strikes. Continuation
sweeps still validate the same exact observed UnitKeys but cannot overwrite the retained rows.

Projection from the completed action requires exactly one explicit unit-policy record per frozen
row. Non-equipment characteristic adjustments, secondary inputs, tile height, Parry resource keys,
and explicit eligibility remain named inputs rather than inferred job content. Missing, duplicate,
or extra policy rows fail before a canonical snapshot exists. The sentinel mutates the live source
raw array and target HP across later repeats, then proves the completed action and projected batch
retain the original PA and HP. Release build completes with zero warnings/errors and full smoke
passes from an isolated output directory.

The classified composition boundary now accepts the completed captured action directly. It first
projects the frozen rows from the exact unit-policy set and then selects the already-implemented
family composer from the immutable ability classification. The Direct sentinel now follows the
whole offline path `native sweep -> frozen action -> unit projection -> family policy -> canonical
request` under one ActionInstance. A mismatched admission identity, missing policy row, wrong family
policy type, or stale pool stops before execution RNG or publication.

The admission callback no longer treats a poller-order race as identity corruption. It can register
a live source or target row that is absent from the canonical battle registry at the synchronous
boundary. If the slot is already registered to a different character, it still fails closed and
disables admission. This preserves generation safety without making the first action against a
newly observed unit nondeterministically disarm the hook. Release build and full smoke pass.

The admission log now records source slot/character, action type, ability, repeat index/count,
target count, and every target slot/character. This makes the live proof capable of checking exact
identity and cardinality rather than accepting a generic hook hit. The smoke-test executable can
emit its atomically validated canonical sentinel catalog plus a mutation-free admission settings
profile. Fixture `1784545101-*` validates with zero errors and one expected not-yet-live warning;
`work/1784545200-canonical-admission-live-runbook.md` owns the bounded Fire procedure. The supported
Windows-control skill currently fails during its own kernel initialization, so no live execution
was attempted through an unreviewed fallback.

## Selected unit versus affected TargetBatch — 2026-07-20

Static execution analysis identifies RVA `0x7B0792` as a selected-unit byte read at `0x281E36`,
immediately before the affected-target builder. This is a better boundary input than the old
pre-confirm `actor+0x1BC` diagnostic, which live evidence already showed can be zero. The selected
unit remains semantically distinct from the expanded target list, especially for Area and Reflect.

Admission now retains optional `SelectedUnit` across exact repeat continuity and freezes its native
row even when it is not an affected resolution target. `DirectNumeric` captured composition requires
that selected identity to equal its explicit declared target; the affected list may still contain a
different resolution target. Other families do not reinterpret the field without their own proof.
The analyzer guards the new `0x281E36` byte anchor. Release build and full smoke pass; a negative
sentinel rejects a Direct policy whose declared target differs from the captured selected unit.

## Selected tile / epicenter identity — 2026-07-20

The staged order tuple is no longer ambiguous. Existing static and live-proven Reaction-order
evidence maps `order+0x0C/+0x0E/+0x10` to X/map-level/Y, copied from native unit
`+0x4F/+0x51.bit7/+0x50`. Arc resolution independently consumes the same three-word tuple.

Canonical admission now reads it synchronously as `DclBattleTile`, rejects invalid negative X/Y or
map levels outside `0..1`, and retains it under the ActionInstance. Every continuation Strike must
carry the same tuple. Captured classified composition checks a unit-target tuple against the frozen
selected-unit tile and checks fixed-tile policy against the captured epicenter before producing a
coordinator request. Negative smoke sentinels cover changed repeat coordinates and a selected tile
that disagrees with the frozen selected unit. Isolated Release build and the full smoke suite pass.

## Physical weapon identity per Strike â€” 2026-07-20

The new DCL requires Dual Wield to remain one outer action while main-hand and off-hand Strikes use
independent weapon skill, damage, readiness, and slot-bound state. The earlier canonical physical
path still normalized one action-wide weapon and resource key, which would silently clone the main
hand across both Strikes.

Native admission now captures the active item id and normalized hand for each sweep from repeat
count/index plus the right/left globals. Hand identity is retained separately from item equality.
The physical composer requires the captured pair to match the synchronized equipment slot and
rejects an explicit per-Strike policy that disagrees. Actions without a native weapon carrier may
still supply a complete explicit mapping.

Forecast, AI, typed state projection, confirmed execution, damage-type/DR selection, ranged route,
readiness, and weapon-bound state now resolve from the weapon assigned to each Strike. Distinct hand
states commit once at outer settlement; action/movement resources and the Reaction window remain
once per ActionInstance. Barrage and other longer native weapon sequences continue to select the
primary hand for every Strike.

The integrated Dual Wield sentinel captures item `19`/right followed by item `20`/left, verifies
independent skills and damage distributions, commits only the off-hand `UnreadyAfterAttack` state,
and publishes one two-Strike outer action. Negative sentinels reject native/explicit hand mismatch
and an item attributed to the wrong synchronized equipment slot. The isolated Release build
completes with zero warnings/errors and the full smoke suite passes.

## Promoted-left, unarmed, and normal-Attack cardinality â€” 2026-07-20

Auditing the active-hand path against native normalization exposed two cases not covered by the
mixed-weapon fixture. A lone left-slot weapon is promoted into the native Primary global, so
Primary cannot be translated blindly to the right equipment slot. Native item zero is also the
existing `Nothing Equipped` weapon row and represents the canonical unarmed route; treating zero as
missing would discard a legal Attack.

Captured-hand translation now uses the frozen equipment snapshot. Primary maps to the right slot
when occupied or to the left slot only when native promotion is possible; OffHand requires the
actual left slot behind an occupied primary. Item zero maps to a distinct unarmed limb resource and
resolves authored Brawling/thrust/crushing metadata without fabricating an equipped SKU. Sentinels
cover mixed hands, promoted-left Primary, wrong-slot rejection, and unarmed metadata/equipment
validation.

The audit also showed that native ability `0` cannot be bound only to a fixed two-Strike fixture:
ordinary Attack emits one sweep without Dual Wield and two with it. Ability bindings now default to
exact profile cardinality, while the explicit `SingleOrProfileMaximum` policy is legal only for a
physical two-Strike maximum on `NativeRepeat`. Admission, composition, forecast/AI, typed state
planning, execution, and the transaction core all derive the same effective one-or-two count for
that ActionInstance. Barrage and other repeat carriers remain exact. An end-to-end unarmed sentinel
uses the same ability/profile revision, admits one native sweep, consumes four reachable draws,
settles one Strike, pays once, and opens one Reaction window. The isolated Release build completes
with zero warnings/errors and the full smoke suite passes.

The shared physical planning coordinator now also reads each selected hand's battle-owned readiness
before enumerating player forecast or AI outcomes. After the Dual Wield sentinel commits its
`UnreadyAfterAttack` off-hand, a second plan fails as `weapon-unready:left-weapon` with no evaluation
object even though the main hand remains Ready. Confirmed execution already enforced this gate; the
change closes forecast/execution legality parity.

## GlobalCT turn grant ordering — 2026-07-20

The scheduler already matches the revised DCL turn contract. At one GlobalCT boundary it reserves
charged actions before scheduled state work and state work before unit turns. Eligible units are
ordered by exact current CT, then the unique initial initiative rank, then stable unit slot. A
granted turn remains the sole active turn until its completion boundary; attempts to advance
GlobalCT or reserve another unit turn fail closed. Granting the turn resets only the selected unit
to zero, leaving every other eligible unit at the same boundary with its exact accumulated CT.

The smoke matrix now proves both relevant branches: equal CT uses the earlier initiative rank, and
a later-ranked Hasted unit at CT 110 acts before an earlier-ranked unit at CT 105. Both cases verify
that the waiting unit retains its CT while the selected unit alone resets. The full smoke suite
passes after the additional sentinels.

## Area ResourceChange and shared Drain cap — 2026-07-20

The normalized authoring contract permits generic HP/MP ResourceChange under Area delivery, but the
canonical capability resolver and Area vertical previously accepted only Damage or Healing. This
was an offline completeness gap: a schema-valid Area Drain could load only as unsupported despite
having no design dependency on jobs.

AreaNumeric now accepts its single ResourceChange Carrier. Confirmed execution advances every
target pool in stable target/Strike order and carries one evolving source pool across the whole
TargetBatch. A Drain therefore cannot refill its source cap independently for every target. Target
KO short-circuits later Strikes locally; ResourceChange never creates Injury, Shock, Major Wound,
Aim disruption, concentration incidents, or an intermediate Reaction window. Native projection
emits each target HP/MP result, one aggregate source-side effect, the separate action payment, and
one terminal Reaction window.

The RNG-free evaluator conditions on the one shared caster roll. For each caster result it performs
an exact dynamic convolution over target delivery gates, per-Strike magnitude distributions,
target pool state, target KO, and the current shared source pool. Player projection retains target
HP/MP ranges plus the aggregate source distribution; AI receives the corresponding exact expected
target/source channels and source-KO probability.

The integrated sentinel drains two five-HP targets for four HP each into one source with only two
HP of capacity. Both targets lose four, while the source receives two total rather than four. Its
forecast source expectation is exactly two times the shared delivery probability, execution owns
only the shared casting draw because the `0d6+4` magnitude has no dice, native publication retains
two target carriers plus one source carrier, payment occurs once, and one Reaction window opens.
The full smoke suite passes.

The same audit exposed a fail-closed requirement for declared magnitude cardinality. Generic
physical and Area executors sample per target per Strike; one-Strike PerTarget is equivalent. They
must not silently reinterpret Shared, multi-Strike PerTarget, or Explicit authoring. Atomic
capability loading now rejects those shapes until a named executor owns their distinct random-site
identity. A negative sentinel proves Shared Area ResourceChange cannot enter the runtime through
the generic family.
