namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclCardinalVector(int X, int Y)
{
    public bool IsCardinalUnit => Math.Abs(X) + Math.Abs(Y) == 1;
}

internal enum DclAttackArc
{
    Front,
    Side,
    Back,
}

internal sealed record DclFacingClassification(
    DclAttackArc Arc,
    int Forward,
    int Lateral,
    int LateralSign)
{
    public bool ActiveDefenseAllowed => Arc != DclAttackArc.Back;
    public int ActiveDefenseModifier => Arc == DclAttackArc.Side ? -2 : 0;
}

internal enum DclPhysicalLocation
{
    NormalCombined,
    Body,
    Head,
}

internal readonly record struct DclInvisibleActionPolicy(
    bool RemoveAtDeclarationCompletion,
    bool RemoveAtOuterActionCommit,
    bool SuppressActiveDefenseForWholeDelivery);

internal static class DclFacingAndTargeting
{
    public static DclFacingClassification ClassifyArc(
        DclBattleTile attacker,
        DclBattleTile defender,
        DclCardinalVector defenderFacing)
    {
        if (!defenderFacing.IsCardinalUnit)
            throw new ArgumentException("Facing must be a cardinal unit vector.", nameof(defenderFacing));
        int attackX = checked(attacker.X - defender.X);
        int attackY = checked(attacker.Y - defender.Y);
        if (attackX == 0 && attackY == 0)
            throw new ArgumentException("Same-tile attacks are outside the DCL because Reach C does not exist.");
        int forward = checked(defenderFacing.X * attackX + defenderFacing.Y * attackY);
        int lateral = checked(defenderFacing.X * attackY - defenderFacing.Y * attackX);
        int absoluteLateral = Math.Abs(lateral);
        DclAttackArc arc = forward > 0 && forward >= absoluteLateral
            ? DclAttackArc.Front
            : forward < 0 && -forward >= absoluteLateral
                ? DclAttackArc.Back
                : DclAttackArc.Side;
        return new DclFacingClassification(arc, forward, lateral, Math.Sign(lateral));
    }

    public static DclDefenseOption ApplyDefenseLegality(
        DclDefenseOption defense,
        DclFacingClassification facing,
        bool criticalAttack,
        bool invisibleImmediateOffense,
        bool defenderBlind,
        int? shieldProtectingLateralSign = null)
    {
        if (defense.Kind == DclDefenseKind.None || criticalAttack || invisibleImmediateOffense || !facing.ActiveDefenseAllowed)
            return new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false);
        if (defense.Kind == DclDefenseKind.Block && facing.Arc == DclAttackArc.Side)
        {
            if (facing.LateralSign == 0 || shieldProtectingLateralSign != facing.LateralSign)
                return new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false);
        }
        int blindPenalty = defenderBlind ? -4 : 0;
        return defense with { Target = checked(defense.Target + facing.ActiveDefenseModifier + blindPenalty) };
    }

    public static int ApplyBlindAttackPenalty(int effectiveSkill, bool sourceBlind, bool visionRequired)
        => checked(effectiveSkill + (sourceBlind && visionRequired ? -6 : 0));

    public static bool IsReachLegal(
        DclBattleTile source,
        DclBattleTile target,
        int reach,
        bool nativePathLegal,
        bool nativeHeightLegal)
    {
        if (reach is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(reach), "FFT DCL melee Reach must be 1 or 2.");
        int distance = checked(Math.Abs(source.X - target.X) + Math.Abs(source.Y - target.Y));
        return distance >= 1 && distance <= reach && nativePathLegal && nativeHeightLegal;
    }

    public static int ApplicableDr(int bodyDr, int headDr, DclPhysicalLocation location)
    {
        if (bodyDr < 0 || headDr < 0) throw new ArgumentOutOfRangeException(nameof(bodyDr));
        return location switch
        {
            DclPhysicalLocation.NormalCombined => checked(bodyDr + headDr),
            DclPhysicalLocation.Body => bodyDr,
            DclPhysicalLocation.Head => headDr,
            _ => throw new ArgumentOutOfRangeException(nameof(location)),
        };
    }

    public static bool CanSelectInvisibleTarget(
        bool targetInvisible,
        bool hostileSource,
        bool directlyUnitTargeted,
        bool sourceOnTargetSide)
        => !targetInvisible || !hostileSource || !directlyUnitTargeted || sourceOnTargetSide;

    public static DclInvisibleActionPolicy InvisibleFirstAction(
        bool sourceInvisibleAtDeclaration,
        bool offensive,
        int castCt)
    {
        if (castCt < 0) throw new ArgumentOutOfRangeException(nameof(castCt));
        if (!sourceInvisibleAtDeclaration)
            return new DclInvisibleActionPolicy(false, false, false);
        bool delayed = castCt > 0;
        return new DclInvisibleActionPolicy(
            RemoveAtDeclarationCompletion: delayed,
            RemoveAtOuterActionCommit: !delayed,
            SuppressActiveDefenseForWholeDelivery: offensive && !delayed);
    }
}
