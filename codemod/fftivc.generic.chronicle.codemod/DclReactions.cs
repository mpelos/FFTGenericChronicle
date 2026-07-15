namespace fftivc.generic.chronicle.codemod;

internal sealed class DclReactionRule
{
    public string Name { get; set; } = "";
    public int AbilityId { get; set; } = -1;
    public string Mode { get; set; } = "courage";
    public int FlatChance { get; set; } = -1;
    public string ConditionFormula { get; set; } = "";
    public string ChanceFormula { get; set; } = "";
    public bool VmInternalAvoidance { get; set; } = false;

    public string NormalizedMode => (Mode ?? "").Trim().ToLowerInvariant();
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"reaction-{AbilityId}" : Name.Trim();

    public bool TryMatches(FormulaContext context, out bool matches, out string error)
    {
        matches = true;
        error = "";
        if (string.IsNullOrWhiteSpace(ConditionFormula))
            return true;
        if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
            return false;
        matches = value != 0;
        return true;
    }

    public bool TryGetChance(int brave, FormulaContext context, out int chance, out string error)
    {
        chance = 0;
        error = "";
        int boundedBrave = Math.Clamp(brave, 0, 100);
        DclReactions.AddRuleVariables(context, AbilityId, boundedBrave, FlatChance, NormalizedMode);

        if (!string.IsNullOrWhiteSpace(ChanceFormula))
        {
            if (!FormulaExpression.TryEvaluate(ChanceFormula, context, out int formulaChance, out error))
                return false;
            chance = Math.Clamp(formulaChance, 0, 100);
            return true;
        }

        chance = DclReactions.DefaultChance(NormalizedMode, boundedBrave, FlatChance);
        if (chance < 0)
        {
            error = NormalizedMode == "neutral"
                ? "neutral reactions require FlatChance 0..100 or ChanceFormula"
                : $"unsupported reaction mode '{Mode}'";
            return false;
        }
        return true;
    }
}

internal static class DclReactions
{
    public static bool IsSupportedMode(string? mode)
        => mode is not null && mode.Trim().ToLowerInvariant() is "courage" or "caution" or "neutral";

    public static int DefaultChance(string? mode, int brave, int flatChance)
    {
        int boundedBrave = Math.Clamp(brave, 0, 100);
        return (mode ?? "").Trim().ToLowerInvariant() switch
        {
            "courage" => boundedBrave,
            "caution" => 100 - boundedBrave,
            "neutral" when flatChance is >= 0 and <= 100 => flatChance,
            _ => -1,
        };
    }

    public static void AddRuleVariables(
        FormulaContext context,
        int reactionAbilityId,
        int brave,
        int flatChance,
        string? normalizedMode)
    {
        int boundedBrave = Math.Clamp(brave, 0, 100);
        string mode = (normalizedMode ?? "").Trim().ToLowerInvariant();
        context.Set("reaction.abilityId", reactionAbilityId);
        context.Set("reaction.brave", boundedBrave);
        context.Set("reaction.inverseBrave", 100 - boundedBrave);
        context.Set("reaction.flatChance", flatChance);
        context.Set("reaction.isCourage", mode == "courage" ? 1 : 0);
        context.Set("reaction.isCaution", mode == "caution" ? 1 : 0);
        context.Set("reaction.isNeutral", mode == "neutral" ? 1 : 0);
    }

    public static void AddIncomingVariables(FormulaContext context, DclReactionIncomingContext incoming)
    {
        context.Set("reaction.sourceValid", incoming.SourceValid ? 1 : 0);
        context.Set("reaction.incomingActionValid", incoming.ActionValid ? 1 : 0);
        context.Set("reaction.sourceIdx", incoming.SourceIdx);
        context.Set("reaction.targetIdx", incoming.TargetIdx);
        context.Set("reaction.sourceCharId", incoming.SourceCharId);
        context.Set("reaction.incomingActionType", incoming.ActionType);
        context.Set("reaction.incomingAbilityId", incoming.AbilityId);
        context.Set("reaction.incomingHitKnown", incoming.HitDecisionKnown ? 1 : 0);
        context.Set("reaction.incomingHit", incoming.HitDecisionKnown && incoming.Hit ? 1 : 0);
        context.Set("reaction.incomingMiss", incoming.HitDecisionKnown && !incoming.Hit ? 1 : 0);
        context.Set("reaction.incomingPhysicalOutcome", incoming.PhysicalOutcome);
        context.Set("reaction.incomingDefenseKind", incoming.DefenseKind);
        context.Set("reaction.incomingAttackMiss", incoming.PhysicalOutcome == (int)DclPhysicalOutcome.AttackMiss ? 1 : 0);
        context.Set("reaction.incomingFumble", incoming.PhysicalOutcome == (int)DclPhysicalOutcome.AttackFumble ? 1 : 0);
        context.Set("reaction.incomingDefended", incoming.PhysicalOutcome == (int)DclPhysicalOutcome.Defended ? 1 : 0);
        context.Set("reaction.incomingCritical", incoming.PhysicalOutcome == (int)DclPhysicalOutcome.CriticalHit ? 1 : 0);
        context.Set("reaction.sourceTurnEpoch", (int)Math.Clamp(incoming.SourceTurnEpoch, int.MinValue, int.MaxValue));
        context.Set("reaction.targetTurnEpoch", (int)Math.Clamp(incoming.TargetTurnEpoch, int.MinValue, int.MaxValue));
        context.Set("reaction.isSelfSource", incoming.SourceValid && incoming.SourceIdx == incoming.TargetIdx ? 1 : 0);
    }
}

internal readonly record struct DclReactionIncomingContext(
    bool SourceValid,
    bool ActionValid,
    int SourceIdx,
    int TargetIdx,
    int SourceCharId,
    int ActionType,
    int AbilityId,
    bool HitDecisionKnown,
    bool Hit,
    int PhysicalOutcome,
    int DefenseKind,
    long SourceTurnEpoch,
    long TargetTurnEpoch,
    string Origin);

internal readonly record struct DclReactionVirtualizationFrame(
    bool Applied,
    nint UnitPtr,
    byte OriginalBrave,
    byte VirtualChance,
    int ReactionAbilityId);
