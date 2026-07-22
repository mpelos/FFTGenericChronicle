using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed class DclExactIntegerDistribution
{
    private readonly IReadOnlyDictionary<int, BigInteger> _weights;

    public DclExactIntegerDistribution(IReadOnlyDictionary<int, BigInteger> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0 || weights.Any(pair => pair.Value <= 0))
            throw new ArgumentException("An exact value distribution requires positive weight for at least one value.", nameof(weights));
        _weights = new Dictionary<int, BigInteger>(weights);
        TotalOutcomes = _weights.Values.Aggregate(BigInteger.Zero, (sum, weight) => sum + weight);
    }

    public IReadOnlyDictionary<int, BigInteger> Weights => _weights;
    public BigInteger TotalOutcomes { get; }
    public int Minimum => _weights.Keys.Min();
    public int Maximum => _weights.Keys.Max();
    public DclRational ExpectedValue => new(
        _weights.Aggregate(BigInteger.Zero, (sum, pair) => sum + pair.Key * pair.Value),
        TotalOutcomes);

    public DclExactIntegerDistribution Transform(Func<int, int> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        var transformed = new Dictionary<int, BigInteger>();
        foreach ((int value, BigInteger weight) in _weights)
        {
            int next = transform(value);
            transformed[next] = transformed.GetValueOrDefault(next) + weight;
        }
        return new DclExactIntegerDistribution(transformed);
    }

    public static DclExactIntegerDistribution Roll(DclDiceExpression expression)
    {
        var weights = new Dictionary<int, BigInteger> { [expression.Adds] = BigInteger.One };
        for (int dieIndex = 0; dieIndex < expression.Dice; dieIndex++)
        {
            var next = new Dictionary<int, BigInteger>();
            foreach ((int subtotal, BigInteger weight) in weights)
            {
                for (int face = 1; face <= 6; face++)
                    next[subtotal + face] = next.GetValueOrDefault(subtotal + face) + weight;
            }
            weights = next;
        }
        return new DclExactIntegerDistribution(weights);
    }
}

internal sealed record DclExactValueForecast(
    int Minimum,
    int Maximum,
    DclRational ExpectedValue,
    IReadOnlyDictionary<int, DclRational> ProbabilityByValue)
{
    public DclRational ProbabilityOf(int value)
        => ProbabilityByValue.GetValueOrDefault(value, DclRational.FromInteger(0));

    public static DclExactValueForecast Mixture(
        DclRational zeroProbability,
        IEnumerable<(DclRational Probability, DclExactIntegerDistribution Distribution)> components)
    {
        if (zeroProbability < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(zeroProbability));
        ArgumentNullException.ThrowIfNull(components);
        var probabilities = new Dictionary<int, DclRational>();
        if (zeroProbability > DclRational.FromInteger(0))
            probabilities[0] = zeroProbability;
        DclRational total = zeroProbability;
        foreach ((DclRational probability, DclExactIntegerDistribution distribution) in components)
        {
            if (probability < DclRational.FromInteger(0))
                throw new ArgumentOutOfRangeException(nameof(components));
            if (probability == DclRational.FromInteger(0)) continue;
            total += probability;
            foreach ((int value, BigInteger weight) in distribution.Weights)
            {
                DclRational contribution = probability * new DclRational(weight, distribution.TotalOutcomes);
                probabilities[value] = probabilities.GetValueOrDefault(value, DclRational.FromInteger(0)) + contribution;
            }
        }
        if (total != DclRational.FromInteger(1))
            throw new ArgumentException($"Exact value-mixture probabilities must sum to one, found {total}.", nameof(components));
        DclRational expected = probabilities.Aggregate(
            DclRational.FromInteger(0),
            (sum, pair) => sum + DclRational.FromInteger(pair.Key) * pair.Value);
        return new DclExactValueForecast(
            probabilities.Keys.Min(),
            probabilities.Keys.Max(),
            expected,
            probabilities.OrderBy(pair => pair.Key).ToDictionary());
    }
}
