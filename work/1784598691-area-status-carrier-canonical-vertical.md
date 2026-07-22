# Area status Carrier canonical vertical

## Scope

This checkpoint follows the revised DCL documents, especially the separation between an Area
delivery gate and a status Rider gate. It covers an Area action whose only effect is a
`StatusApplication` Carrier. It does not cover a numeric Area Carrier with a status Rider; that
existing shape retains its second, independently authored state-resistance gate.

## Contract conclusion

- A pure Area status Carrier uses the Area delivery gate as the Carrier's only target gate.
- `AreaDeliveryGate.QuickContest` therefore requires the referenced state to use
  `ResistanceGate.QuickContest`; both describe the same single contest.
- `AreaDeliveryGate.None` and `AreaDeliveryGate.Dodge` require the referenced state to use
  `ResistanceGate.None`; Dodge is delivery avoidance, not a second state contest.
- Immunity is checked before a pure QuickContest status Carrier consumes target resistance RNG.
- A numeric Area Carrier followed by a status Rider remains different: delivery resolves first,
  then a landed Carrier may consume the Rider's own resistance roll.

## Implementation checkpoint

The canonical capability resolver now classifies a one-Strike, magnitude-free Area
`StatusApplication` Carrier and validates its exact native Carrier/rewrite pair plus the gate/state
agreement.

The Area execution vertical now:

- accepts pure status carriers through SingleResult, StatusPacket, or ConditionalStatusProducer;
- consumes one shared casting roll and at most one target Quick Contest per target;
- never creates a magnitude random site for this shape;
- preserves immunity before target resistance RNG;
- records the one contest as the status Carrier result instead of resolving a second Rider gate;
- materializes the state during the outer transaction commit;
- publishes empty numeric target channels plus the exact native/custom state application;
- retains one payment and one terminal Reaction window for the outer ActionInstance.

The Area evaluation vertical now:

- keeps the shared-caster correlation across targets;
- models Carrier delivery/application exactly once;
- treats delivery immunity as zero delivery, zero landed Strikes, and zero status application;
- exposes zero magnitude and zero HP/MP forecasts;
- projects the same application mass to player forecast and AI.

`DclMagicCorrelatedForecast` gained an explicit delivery-immunity input so a Carrier-level immune
target is removed from both the delivered-target-count distribution and target-local delivery
expectations rather than being treated as an immune Rider after a successful delivery.

## Sentinel coverage added

The smoke sentinel defines a pure QuickContest Area status action with two targets and checks:

- schema and capability acceptance;
- fail-closed rejection when the state gate is `None` but Area delivery is `QuickContest`;
- forecast/AI reuse of delivery probability exactly once;
- zero magnitude/resource channels;
- immunity-aware zero delivery forecast;
- one shared casting draw plus one resistance draw per nonimmune target;
- one applied target and one resisting target;
- exact state materialization and native auxiliary projection;
- one terminal Reaction window.

## Validation state

- The complete smoke project builds with zero warnings and zero errors after the implementation and
  sentinel additions.
- `git diff --check` passes for every touched implementation/test file.
- The first complete smoke execution exposed a misplaced `else if`: a delivered numeric Area Strike
  was incorrectly tested by the failed-delivery magnitude guard. The guard now keys directly on
  `!strikeGate.Landed` rather than on the pure-status branch.
- A clean rebuild succeeds with zero warnings/errors and the complete executable reports
  `formula runtime smoke tests passed` after that correction.
- The proven pure-Area status contract is promoted to `docs/modding/06-code-mod-runtime-dsl.md` and
  `docs/modding/08-dcl-information-requirements.md` without copying this investigation history.
