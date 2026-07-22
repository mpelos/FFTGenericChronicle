using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal enum DclGrowthChannel
{
    St,
    Dx,
    Iq,
    Brave,
    HpModifier,
    MpModifier,
}

internal sealed record DclGrowthRateVector(
    IReadOnlyDictionary<DclGrowthChannel, long> ScaledRates)
{
    public const long GrowthScale = 1_000_000;

    public static DclGrowthRateVector FromExactDecimals(
        IReadOnlyDictionary<DclGrowthChannel, string> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        var scaled = new Dictionary<DclGrowthChannel, long>();
        foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
        {
            if (!rates.TryGetValue(channel, out string? text))
                throw new ArgumentException($"Growth channel {channel} is missing.", nameof(rates));
            DclRational exact = DclRational.ParseExactDecimal(text);
            if (exact < DclRational.FromInteger(0))
                throw new ArgumentOutOfRangeException(nameof(rates), "Ordinary Character Growth rates cannot be negative.");
            DclRational micro = exact * DclRational.FromInteger(GrowthScale);
            if (micro.Denominator != BigInteger.One)
                throw new ArgumentException($"Growth rate {text} for {channel} requires more than six fractional places.", nameof(rates));
            scaled[channel] = checked((long)micro.Numerator);
        }
        if (rates.Keys.Any(channel => !Enum.IsDefined(channel)))
            throw new ArgumentException("Growth vector contains an unknown channel.", nameof(rates));
        return new DclGrowthRateVector(scaled);
    }

    public long Rate(DclGrowthChannel channel)
        => ScaledRates.TryGetValue(channel, out long value)
            ? value
            : throw new InvalidOperationException($"Growth channel {channel} is missing.");

    public DclRational PointEquivalentPerLevel()
    {
        DclRational total = DclRational.FromInteger(0);
        foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
            total += new DclRational(Rate(channel), GrowthScale) * PointCost(channel);
        return total;
    }

    public static DclRational PointCost(DclGrowthChannel channel)
        => channel switch
        {
            DclGrowthChannel.St => DclRational.FromInteger(10),
            DclGrowthChannel.Dx or DclGrowthChannel.Iq => DclRational.FromInteger(20),
            DclGrowthChannel.Brave => new DclRational(5, 4),
            DclGrowthChannel.HpModifier => DclRational.FromInteger(2),
            DclGrowthChannel.MpModifier => DclRational.FromInteger(3),
            _ => throw new ArgumentOutOfRangeException(nameof(channel)),
        };

    public void RequireBudget(DclRational universalGrowthBudget)
    {
        if (PointEquivalentPerLevel() != universalGrowthBudget)
            throw new ArgumentException($"Growth vector is worth {PointEquivalentPerLevel()}, expected {universalGrowthBudget}.");
    }
}

internal sealed record DclPermanentGrowthValues(
    int St,
    int Dx,
    int Iq,
    int Brave,
    int HpModifier,
    int MpModifier)
{
    public int Get(DclGrowthChannel channel)
        => channel switch
        {
            DclGrowthChannel.St => St,
            DclGrowthChannel.Dx => Dx,
            DclGrowthChannel.Iq => Iq,
            DclGrowthChannel.Brave => Brave,
            DclGrowthChannel.HpModifier => HpModifier,
            DclGrowthChannel.MpModifier => MpModifier,
            _ => throw new ArgumentOutOfRangeException(nameof(channel)),
        };

    public DclPermanentGrowthValues Add(DclGrowthChannel channel, int gain)
        => channel switch
        {
            DclGrowthChannel.St => this with { St = checked(St + gain) },
            DclGrowthChannel.Dx => this with { Dx = checked(Dx + gain) },
            DclGrowthChannel.Iq => this with { Iq = checked(Iq + gain) },
            DclGrowthChannel.Brave => this with { Brave = checked(Brave + gain) },
            DclGrowthChannel.HpModifier => this with { HpModifier = checked(HpModifier + gain) },
            DclGrowthChannel.MpModifier => this with { MpModifier = checked(MpModifier + gain) },
            _ => throw new ArgumentOutOfRangeException(nameof(channel)),
        };
}

internal sealed record DclGrowthState(
    int GrowthSchemaRevision,
    int HighestAwardedCharacterLevel,
    IReadOnlyDictionary<DclGrowthChannel, long> GrowthProgressMicro);

internal sealed record DclGrowthAwardResult(
    DclGrowthState State,
    DclPermanentGrowthValues PermanentValues,
    IReadOnlyDictionary<DclGrowthChannel, int> IntegerGains,
    int LevelsAwarded,
    bool Changed,
    string Reason);

internal static class DclCharacterGrowth
{
    public const int CurrentSchemaRevision = 1;

    public static DclGrowthState MigratePreDclSave(int currentCharacterLevel)
    {
        ValidateLevel(currentCharacterLevel);
        return new DclGrowthState(
            CurrentSchemaRevision,
            currentCharacterLevel,
            Enum.GetValues<DclGrowthChannel>().ToDictionary(channel => channel, _ => 0L));
    }

    public static DclGrowthAwardResult AwardThroughLevel(
        DclGrowthState state,
        DclPermanentGrowthValues permanentValues,
        int newCharacterLevel,
        DclGrowthRateVector activeJobGrowth,
        DclRational universalGrowthBudget)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(permanentValues);
        ArgumentNullException.ThrowIfNull(activeJobGrowth);
        ValidateLevel(state.HighestAwardedCharacterLevel);
        ValidateLevel(newCharacterLevel);
        if (state.GrowthSchemaRevision > CurrentSchemaRevision)
            return new DclGrowthAwardResult(state, permanentValues, EmptyGains(), 0, false, "unknown-newer-schema-growth-disabled");
        if (state.GrowthSchemaRevision != CurrentSchemaRevision)
            throw new InvalidOperationException("A known older growth schema requires an explicit migration before awards.");
        ValidatePersistentState(state);
        activeJobGrowth.RequireBudget(universalGrowthBudget);
        if (newCharacterLevel <= state.HighestAwardedCharacterLevel)
            return new DclGrowthAwardResult(state, permanentValues, EmptyGains(), 0, false, "level-already-awarded-or-delevel");

        int levels = newCharacterLevel - state.HighestAwardedCharacterLevel;
        var progress = state.GrowthProgressMicro.ToDictionary(pair => pair.Key, pair => pair.Value);
        var gains = Enum.GetValues<DclGrowthChannel>().ToDictionary(channel => channel, _ => 0);
        DclPermanentGrowthValues nextValues = permanentValues;
        foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
        {
            long added = checked(activeJobGrowth.Rate(channel) * levels);
            long accumulated = checked(progress[channel] + added);
            long gain = accumulated / DclGrowthRateVector.GrowthScale;
            progress[channel] = accumulated % DclGrowthRateVector.GrowthScale;
            if (gain > int.MaxValue) throw new OverflowException("Growth award exceeds the permanent integer channel width.");
            gains[channel] = (int)gain;
            nextValues = nextValues.Add(channel, (int)gain);
        }
        var nextState = new DclGrowthState(CurrentSchemaRevision, newCharacterLevel, progress);
        return new DclGrowthAwardResult(nextState, nextValues, gains, levels, true, "growth-awarded");
    }

    public static int PermanentFaith(int recruitmentFaith, IEnumerable<int> permanentChanges)
    {
        ArgumentNullException.ThrowIfNull(permanentChanges);
        int value = recruitmentFaith;
        foreach (int change in permanentChanges) value = checked(value + change);
        return Math.Clamp(value, 0, 100);
    }

    public static int CurrentFaith(int permanentFaith, IEnumerable<int> temporaryChanges)
    {
        if (permanentFaith is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(permanentFaith));
        ArgumentNullException.ThrowIfNull(temporaryChanges);
        int value = permanentFaith;
        foreach (int change in temporaryChanges) value = checked(value + change);
        return Math.Clamp(value, 0, 100);
    }

    private static IReadOnlyDictionary<DclGrowthChannel, int> EmptyGains()
        => Enum.GetValues<DclGrowthChannel>().ToDictionary(channel => channel, _ => 0);

    internal static void ValidatePersistentState(DclGrowthState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateLevel(state.HighestAwardedCharacterLevel);
        if (state.GrowthProgressMicro.Count != Enum.GetValues<DclGrowthChannel>().Length)
            throw new ArgumentException("Growth progress must contain exactly the canonical channels.", nameof(state));
        foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
        {
            if (!state.GrowthProgressMicro.TryGetValue(channel, out long value) || value is < 0 or >= DclGrowthRateVector.GrowthScale)
                throw new ArgumentException($"Growth progress for {channel} must exist inside one retained step.", nameof(state));
        }
    }

    private static void ValidateLevel(int level)
    {
        if (level is < 1 or > 99) throw new ArgumentOutOfRangeException(nameof(level));
    }
}
