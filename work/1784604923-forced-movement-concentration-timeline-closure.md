# Forced Movement concentration timeline closure

## Question

Can standalone and Area Forced Movement resolve concentration against the exact pending charged
action without duplicating incidents, consuming RNG for defended outcomes, or cancelling an action
after Reaction?

## Result

Yes. The canonical battle runtime now owns exactly one attached timeline scheduler. The confirmed
movement request carries an explicit target-owned concentration context (`Charging`, `Will`,
modifier, and state penalty); executable requests validate that snapshot against the attached
timeline before RNG.

A positive delivered displacement creates exactly one concentration incident per target/Strike.
The incident uses one `Concentration` ledger site. A preserved result retains the exact pending
action and due CT. A failed result cancels that exact action in the commit callback before Reaction
dispatch. Resistance, immunity, resource failure, and zero displacement consume no concentration
roll and create no cancellation.

Forecast and AI projections use the same exact 3d6 outcome count multiplied by the delivery
probability. Area aggregation sums the target expectations without inventing a shared roll.

## Validation

- Standalone sentinel: delivered Push cancels both Aim and the exact pending charged action; the
  concentration failure is committed before the single Reaction.
- Area sentinel: the delivered target receives one concentration incident and cancellation; an
  immune target consumes no target or concentration RNG.
- Build configuration: `MovementConcentrationFinal`.
- Result: build succeeded with zero warnings and zero errors; formula runtime smoke tests passed.

No live test is required for this canonical vertical. Live work remains necessary for binding the
native timeline/Charging status and authoritative native map callbacks.

## Next gap

The general injury paths (physical, direct damage, and Area damage) already calculate concentration
outcomes in places, but do not yet share one timeline-owned incident consistently. They must be
audited so injury plus displacement in the same Strike resolves exactly once and cancels only the
exact pending action.
