namespace fftivc.generic.chronicle.codemod;

internal enum DclCastingOutcome
{
    CriticalSuccess,
    Success,
    OrdinaryFailure,
    CriticalFailure,
    ResourceFailure,
    Interrupted,
    VoluntaryCancellation,
}

internal readonly record struct DclCostCommitment(
    int FinalMpCost,
    int DeclarationMpCoverage,
    int ApprovedHpCap,
    int ProjectedMp,
    int ProjectedHp,
    bool Legal,
    bool RequiresOvercastConfirmation,
    string Reason);

internal readonly record struct DclResourcePayment(
    int CostDue,
    int MpPaid,
    int HpPaid,
    int RemainingMp,
    int RemainingHp,
    bool Legal,
    bool PayerKo,
    string Reason);

internal readonly record struct DclOvercastForecastProjection(
    int FinalMpCost,
    int MpPaidAtDeclaration,
    int HpPaidAtDeclaration,
    int ApprovedHpCap,
    int ProjectedMp,
    int ProjectedHp,
    bool RequiresConfirmation,
    bool Legal,
    bool WouldKoCaster,
    string WarningText,
    string Reason);

internal static class DclMagicResources
{
    public static int FinalMpCost(int baseMpCost, IEnumerable<DclRational> multipliers)
    {
        if (baseMpCost < 0) throw new ArgumentOutOfRangeException(nameof(baseMpCost));
        ArgumentNullException.ThrowIfNull(multipliers);
        DclRational combined = DclRational.FromInteger(1);
        foreach (DclRational multiplier in multipliers)
        {
            if (multiplier <= DclRational.FromInteger(0))
                throw new ArgumentOutOfRangeException(nameof(multipliers), "MP-cost multipliers must be exact and positive.");
            combined *= multiplier;
        }
        if (baseMpCost == 0) return 0;
        return Math.Max(1, checked((int)(DclRational.FromInteger(baseMpCost) * combined).Ceiling()));
    }

    public static DclCostCommitment DeclareCost(
        int currentMp,
        int currentHp,
        int finalMpCost,
        DclOvercastPolicy overcastPolicy,
        bool overcastConfirmed)
    {
        ValidatePools(currentMp, currentHp, finalMpCost);
        int mpCoverage = Math.Min(currentMp, finalMpCost);
        int hpCap = finalMpCost - mpCoverage;
        bool confirmationRequired = hpCap > 0;
        bool legal = hpCap == 0 ||
            (overcastPolicy == DclOvercastPolicy.Allowed && currentHp >= hpCap && overcastConfirmed);
        string reason = legal ? "approved" : overcastPolicy != DclOvercastPolicy.Allowed
            ? "overcast-forbidden"
            : currentHp < hpCap ? "insufficient-hp-at-declaration" : "overcast-not-confirmed";
        return new DclCostCommitment(
            finalMpCost,
            mpCoverage,
            hpCap,
            currentMp - mpCoverage,
            Math.Max(0, currentHp - hpCap),
            legal,
            confirmationRequired,
            reason);
    }

    public static DclOvercastForecastProjection ProjectOvercastForecast(DclCostCommitment commitment)
        => new(
            commitment.FinalMpCost,
            commitment.DeclarationMpCoverage,
            commitment.ApprovedHpCap,
            commitment.ApprovedHpCap,
            commitment.ProjectedMp,
            commitment.ProjectedHp,
            commitment.RequiresOvercastConfirmation,
            commitment.Legal,
            commitment.ApprovedHpCap > 0 && commitment.ProjectedHp == 0,
            commitment.ApprovedHpCap > 0 && commitment.ProjectedHp == 0
                ? "Overcasting will reduce the caster to 0 HP."
                : "",
            commitment.Reason);

    public static bool CanPayFullCost(int currentMp, int currentHp, DclCostCommitment commitment)
    {
        ValidatePools(currentMp, currentHp, commitment.FinalMpCost);
        if (!commitment.Legal) return false;
        int hpRequired = commitment.FinalMpCost - Math.Min(currentMp, commitment.FinalMpCost);
        return hpRequired <= commitment.ApprovedHpCap && hpRequired <= currentHp;
    }

    public static int SettlementCost(int finalMpCost, DclCastingOutcome outcome)
    {
        if (finalMpCost < 0) throw new ArgumentOutOfRangeException(nameof(finalMpCost));
        return outcome switch
        {
            DclCastingOutcome.CriticalSuccess or DclCastingOutcome.Success or DclCastingOutcome.CriticalFailure => finalMpCost,
            DclCastingOutcome.OrdinaryFailure => finalMpCost > 0 ? 1 : 0,
            DclCastingOutcome.ResourceFailure or DclCastingOutcome.Interrupted or DclCastingOutcome.VoluntaryCancellation => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
    }

    public static DclResourcePayment Settle(
        int currentMp,
        int currentHp,
        DclCostCommitment commitment,
        DclCastingOutcome outcome)
    {
        ValidatePools(currentMp, currentHp, commitment.FinalMpCost);
        int costDue = SettlementCost(commitment.FinalMpCost, outcome);
        if (outcome == DclCastingOutcome.ResourceFailure)
            return new DclResourcePayment(0, 0, 0, currentMp, currentHp, true, currentHp == 0, "resource-failure-no-debit");
        int mpPaid = Math.Min(currentMp, costDue);
        int hpPaid = costDue - mpPaid;
        bool legal = commitment.Legal && hpPaid <= commitment.ApprovedHpCap && hpPaid <= currentHp;
        if (!legal)
            return new DclResourcePayment(costDue, 0, 0, currentMp, currentHp, false, currentHp == 0, "payment-exceeds-commitment");
        int remainingMp = currentMp - mpPaid;
        int remainingHp = currentHp - hpPaid;
        return new DclResourcePayment(
            costDue,
            mpPaid,
            hpPaid,
            remainingMp,
            remainingHp,
            true,
            remainingHp == 0,
            "paid");
    }

    private static void ValidatePools(int currentMp, int currentHp, int finalMpCost)
    {
        if (currentMp < 0 || currentHp < 0 || finalMpCost < 0)
            throw new ArgumentOutOfRangeException(nameof(currentMp));
    }
}

internal readonly record struct DclCastTiming(
    int CastCt,
    long DeclaredAtGlobalCt,
    long ResolvesAtGlobalCt,
    bool IsImmediate);

internal static class DclCastingTiming
{
    public static DclCastTiming Declare(int baseCastCt, IEnumerable<int> modifiers, long currentGlobalCt)
    {
        if (baseCastCt < 0 || currentGlobalCt < 0) throw new ArgumentOutOfRangeException(nameof(baseCastCt));
        ArgumentNullException.ThrowIfNull(modifiers);
        int castCt = baseCastCt;
        foreach (int modifier in modifiers) castCt = checked(castCt + modifier);
        castCt = Math.Max(0, castCt);
        return new DclCastTiming(castCt, currentGlobalCt, checked(currentGlobalCt + castCt), castCt == 0);
    }
}

internal enum DclConcentrationOutcome
{
    NoIncident,
    DirectCancellation,
    Preserved,
    Interrupted,
}

internal readonly record struct DclConcentrationResult(
    DclConcentrationOutcome Outcome,
    int Score,
    bool Rolled);

internal readonly record struct DclCanonicalConcentrationTargetContext(
    bool Charging,
    int Will,
    int ConcentrationModifier,
    int StatePenaltyMagnitude)
{
    public int Score => DclConcentration.Score(Will, ConcentrationModifier, StatePenaltyMagnitude);

    public void Validate()
    {
        if (Will < 1 || StatePenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(DclCanonicalConcentrationTargetContext));
    }
}

internal static class DclConcentration
{
    public static int Score(int will, int concentrationModifier, int statePenaltyMagnitude)
    {
        if (statePenaltyMagnitude < 0) throw new ArgumentOutOfRangeException(nameof(statePenaltyMagnitude));
        return checked(will - 3 + concentrationModifier - statePenaltyMagnitude);
    }

    public static DclConcentrationResult ResolveStrikeIncident(
        bool stillCharging,
        bool directCancellation,
        int injury,
        int forcedDisplacement,
        int will,
        int concentrationModifier,
        int statePenaltyMagnitude,
        int? roll)
    {
        if (injury < 0 || forcedDisplacement < 0) throw new ArgumentOutOfRangeException(nameof(injury));
        int score = Score(will, concentrationModifier, statePenaltyMagnitude);
        if (!stillCharging || directCancellation)
        {
            if (roll is not null) throw new ArgumentException("Direct cancellation cannot consume concentration RNG.", nameof(roll));
            return new DclConcentrationResult(
                directCancellation ? DclConcentrationOutcome.DirectCancellation : DclConcentrationOutcome.NoIncident,
                score,
                Rolled: false);
        }
        if (injury == 0 && forcedDisplacement == 0)
        {
            if (roll is not null) throw new ArgumentException("A zero-impact Strike cannot consume concentration RNG.", nameof(roll));
            return new DclConcentrationResult(DclConcentrationOutcome.NoIncident, score, Rolled: false);
        }
        if (roll is null) throw new ArgumentNullException(nameof(roll));
        bool success = DclSuccessRoll.Succeeds(roll.Value, score);
        return new DclConcentrationResult(
            success ? DclConcentrationOutcome.Preserved : DclConcentrationOutcome.Interrupted,
            score,
            Rolled: true);
    }
}
