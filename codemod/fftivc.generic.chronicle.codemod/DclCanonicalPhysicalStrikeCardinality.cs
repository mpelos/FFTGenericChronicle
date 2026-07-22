namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Resolves the StrikeCount owned by one physical ActionInstance. Most bindings use the profile's
/// exact count. A native normal-Attack binding may explicitly permit either its single-hand form or
/// the authored two-Strike Dual Wield maximum without changing ability or profile identity.
/// </summary>
internal static class DclCanonicalPhysicalStrikeCardinality
{
    public static DclActionProfile ResolveEffectiveProfile(
        DclAbilityBinding binding,
        DclActionProfile profile,
        IReadOnlyList<DclTargetResolutionSnapshot> targets,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(strikes);
        if (targets.Count == 0)
            throw new ArgumentException("Physical resolution requires a nonempty TargetBatch.", nameof(targets));
        if (targets.Select(target => target.Target).Distinct().Count() != targets.Count)
            throw new ArgumentException("Physical TargetBatch cannot duplicate a target identity.", nameof(targets));

        int[] indexes = strikes.Select(strike => strike.StrikeIndex).Distinct().Order().ToArray();
        if (indexes.Length == 0 || !indexes.SequenceEqual(Enumerable.Range(0, indexes.Length)) ||
            !DclAbilityBindingContract.SupportsEffectiveStrikeCount(binding, profile, indexes.Length))
            throw new ArgumentException(
                "Physical Strike indexes do not match the binding's exact ActionInstance cardinality policy.",
                nameof(strikes));
        if (strikes.Count != checked(targets.Count * indexes.Length) ||
            strikes.Select(strike => (strike.Target, strike.StrikeIndex)).Distinct().Count() != strikes.Count)
            throw new ArgumentException(
                "Physical resolution requires exactly one input per TargetBatch member and Strike index.",
                nameof(strikes));
        foreach (DclTargetResolutionSnapshot target in targets)
        {
            int[] targetIndexes = strikes
                .Where(strike => strike.Target == target.Target)
                .Select(strike => strike.StrikeIndex)
                .Order()
                .ToArray();
            if (!targetIndexes.SequenceEqual(indexes))
                throw new ArgumentException(
                    "Every physical target must carry the same complete Strike index set.",
                    nameof(strikes));
        }

        return indexes.Length == profile.TransactionProfile.StrikeCount
            ? profile
            : profile with
            {
                TransactionProfile = profile.TransactionProfile with { StrikeCount = indexes.Length },
            };
    }
}
