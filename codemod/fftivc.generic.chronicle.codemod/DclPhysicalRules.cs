namespace fftivc.generic.chronicle.codemod;

internal enum DclSkillDifficulty
{
    Unknown,
    Easy,
    Average,
    Hard,
    VeryHard,
}

internal enum DclAptitudeTier
{
    A,
    B,
    C,
    D,
    E,
}

internal sealed record DclSkillTrainingInput(
    int ControllingAttribute,
    DclSkillDifficulty Difficulty,
    DclAptitudeTier AptitudeTier,
    int JobLevel,
    int ExplicitSkillModifier = 0);

internal sealed record DclSkillTrainingResult(
    int ControllingAttribute,
    DclSkillDifficulty Difficulty,
    DclAptitudeTier AptitudeTier,
    int JobLevel,
    int Rank,
    int BaseScore,
    int ExplicitSkillModifier,
    int FinalScore);

internal readonly record struct DclSkillTrainingPolicyKey(int JobId, string SkillFamily);

internal sealed record DclSkillTrainingPolicy
{
    public DclSkillTrainingPolicy(IReadOnlyDictionary<DclSkillTrainingPolicyKey, DclAptitudeTier> aptitudeByJobAndFamily)
    {
        ArgumentNullException.ThrowIfNull(aptitudeByJobAndFamily);
        if (aptitudeByJobAndFamily.Count == 0)
            throw new ArgumentException("Skill training policy requires at least one job/family aptitude row.", nameof(aptitudeByJobAndFamily));
        if (aptitudeByJobAndFamily.Keys.Any(key => key.JobId <= 0 || string.IsNullOrWhiteSpace(key.SkillFamily)))
            throw new ArgumentException("Skill training policy keys require positive JobId and nonblank SkillFamily.", nameof(aptitudeByJobAndFamily));
        if (aptitudeByJobAndFamily.Values.Any(tier => !Enum.IsDefined(tier)))
            throw new ArgumentException("Skill training policy contains an unknown Aptitude Tier.", nameof(aptitudeByJobAndFamily));
        AptitudeByJobAndFamily = aptitudeByJobAndFamily.ToDictionary(
            pair => new DclSkillTrainingPolicyKey(pair.Key.JobId, NormalizeFamily(pair.Key.SkillFamily)),
            pair => pair.Value);
    }

    public IReadOnlyDictionary<DclSkillTrainingPolicyKey, DclAptitudeTier> AptitudeByJobAndFamily { get; }

    public DclAptitudeTier Resolve(int jobId, string skillFamily)
    {
        var key = new DclSkillTrainingPolicyKey(jobId, NormalizeFamily(skillFamily));
        return AptitudeByJobAndFamily.TryGetValue(key, out DclAptitudeTier tier)
            ? tier
            : throw new KeyNotFoundException($"No DCL Skill aptitude policy exists for job {jobId} and family '{skillFamily}'.");
    }

    private static string NormalizeFamily(string skillFamily)
    {
        if (string.IsNullOrWhiteSpace(skillFamily))
            throw new ArgumentException("Skill family is required.", nameof(skillFamily));
        return skillFamily.Trim().ToLowerInvariant();
    }
}

internal static class DclSkillRules
{
    private static readonly int[] JobLevelTotalJpThresholds = [0, 200, 400, 700, 1100, 1600, 2200, 3000];

    private static readonly int[,] RankTable =
    {
        { 1, 2, 2, 3, 3, 4, 5, 6 },
        { 1, 1, 2, 2, 3, 3, 4, 5 },
        { 1, 1, 1, 2, 2, 3, 3, 4 },
        { 1, 1, 1, 1, 2, 2, 3, 3 },
        { 1, 1, 1, 1, 1, 2, 2, 2 },
    };

    public static int JobRank(DclAptitudeTier tier, int jobLevel)
    {
        if (jobLevel is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(jobLevel));
        if (!Enum.IsDefined(tier)) throw new ArgumentOutOfRangeException(nameof(tier));
        return RankTable[(int)tier, jobLevel - 1];
    }

    public static int NativeJobLevelFromTotalJp(int totalJp)
    {
        totalJp = Math.Max(0, totalJp);
        int level = 1;
        for (int i = 1; i < JobLevelTotalJpThresholds.Length; i++)
        {
            if (totalJp < JobLevelTotalJpThresholds[i])
                break;
            level = i + 1;
        }
        return level;
    }

    public static int GurpsSkillScore(
        int controllingAttribute,
        DclSkillDifficulty difficulty,
        int rank,
        int? untrainedDefaultPenalty = null)
    {
        if (rank is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(rank));
        if (rank == 0)
        {
            if (untrainedDefaultPenalty is null or > 0)
                throw new ArgumentException("Rank 0 requires one explicit nonpositive default penalty.", nameof(untrainedDefaultPenalty));
            return checked(controllingAttribute + untrainedDefaultPenalty.Value);
        }
        int difficultyAdjustment = difficulty switch
        {
            DclSkillDifficulty.Easy => 0,
            DclSkillDifficulty.Average => -1,
            DclSkillDifficulty.Hard => -2,
            DclSkillDifficulty.VeryHard => -3,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
        };
        return checked(controllingAttribute + difficultyAdjustment + rank - 1);
    }

    public static DclSkillTrainingResult ResolveTraining(DclSkillTrainingInput input)
    {
        int rank = JobRank(input.AptitudeTier, input.JobLevel);
        int baseScore = GurpsSkillScore(input.ControllingAttribute, input.Difficulty, rank);
        int finalScore = checked(baseScore + input.ExplicitSkillModifier);
        return new DclSkillTrainingResult(
            input.ControllingAttribute,
            input.Difficulty,
            input.AptitudeTier,
            input.JobLevel,
            rank,
            baseScore,
            input.ExplicitSkillModifier,
            finalScore);
    }
}

internal readonly record struct DclDefenseCandidate(
    DclDefenseKind Kind,
    int Score,
    bool Legal,
    string Reason,
    string? ResourceKey = null);

internal static class DclActiveDefenseRules
{
    public static int Dodge(int baseDodge, int jobEquipmentBonus, int encumbrance, int postureStatePenalty)
        => checked(baseDodge + jobEquipmentBonus - encumbrance - postureStatePenalty);

    public static int Parry(
        int weaponSkill,
        int weaponParryModifier,
        int legalShieldDefenseBonus,
        int repeatedParryPenalty,
        int statePenalty)
    {
        if (repeatedParryPenalty < 0 || statePenalty < 0)
            throw new ArgumentOutOfRangeException(nameof(repeatedParryPenalty), "Named Parry penalties are nonnegative magnitudes.");
        int halfSkill = checked((int)new DclRational(weaponSkill, 2).Floor());
        return checked(halfSkill + 3 + weaponParryModifier + legalShieldDefenseBonus - repeatedParryPenalty - statePenalty);
    }

    public static int Block(
        int shieldSkill,
        int shieldBlockModifier,
        int legalShieldDefenseBonus,
        int statePenalty)
    {
        if (statePenalty < 0) throw new ArgumentOutOfRangeException(nameof(statePenalty));
        int halfSkill = checked((int)new DclRational(shieldSkill, 2).Floor());
        return checked(halfSkill + 3 + shieldBlockModifier + legalShieldDefenseBonus - statePenalty);
    }

    public static DclDefenseCandidate SelectOne(IEnumerable<DclDefenseCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        DclDefenseCandidate[] legal = candidates.Where(candidate => candidate.Legal && candidate.Kind != DclDefenseKind.None).ToArray();
        if (legal.Length == 0)
            return new DclDefenseCandidate(DclDefenseKind.None, 0, Legal: false, "no-legal-active-defense");
        return legal
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => DefensePriority(candidate.Kind))
            .First();
    }

    public static DclRational IncomingParryLoad(
        DclRational? skillOverride,
        DclRational? attackingWeaponWeight,
        int attackerSt)
    {
        DclRational load = skillOverride ?? attackingWeaponWeight ?? new DclRational(attackerSt, 10);
        if (load < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(skillOverride), "IncomingParryLoad cannot be negative.");
        return load;
    }

    public static DclRational ParryLimit(DclRational defenderBasicLift, bool twoHandedParry)
    {
        if (defenderBasicLift < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(defenderBasicLift));
        return defenderBasicLift * DclRational.FromInteger(twoHandedParry ? 2 : 1);
    }

    public static bool ParryLoadIsLegal(DclRational incomingLoad, DclRational parryLimit)
    {
        if (incomingLoad < DclRational.FromInteger(0) || parryLimit < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(incomingLoad));
        return incomingLoad <= parryLimit;
    }

    private static int DefensePriority(DclDefenseKind kind)
        => kind switch
        {
            DclDefenseKind.Dodge => 0,
            DclDefenseKind.Parry => 1,
            DclDefenseKind.Block => 2,
            _ => 3,
        };
}
