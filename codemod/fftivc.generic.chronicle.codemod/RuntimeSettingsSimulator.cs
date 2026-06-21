using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal sealed class RuntimeSimulationBundle
{
    public List<RuntimeSimulationScenario> Scenarios { get; set; } = new();
}

internal sealed class RuntimeSimulationScenario
{
    public string Name { get; set; } = "default";
    public string EventKind { get; set; } = "hp";
    public int PreviousHp { get; set; } = 50;
    public int CurrentHp { get; set; } = 30;
    public int VanillaDamage { get; set; } = 20;
    public int PreviousMp { get; set; } = 20;
    public int CurrentMp { get; set; } = 12;
    public int? VanillaMpChange { get; set; }
    public long EventIndex { get; set; } = 1;
    public long EventSeed { get; set; } = 12345;
    public RuntimeSimulationExpectation? Expect { get; set; }
    public RuntimeSimulationUnit Target { get; set; } = RuntimeSimulationUnit.TargetDefaults();
    public RuntimeSimulationUnit? Attacker { get; set; } = RuntimeSimulationUnit.AttackerDefaults();
    public string AttackerSource { get; set; } = "scenario";
    public RuntimeSimulationAction? Action { get; set; }

    [JsonIgnore]
    public bool IsMpEvent =>
        EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase) ||
        EventKind.Equals("mpLoss", StringComparison.OrdinalIgnoreCase) ||
        EventKind.Equals("mpGain", StringComparison.OrdinalIgnoreCase);
}

internal sealed class RuntimeSimulationExpectation
{
    public bool? ShouldRewrite { get; set; }
    public int? FinalDamage { get; set; }
    public int? DesiredHp { get; set; }
    public int? FinalMpChange { get; set; }
    public int? DesiredMp { get; set; }
    public string? RuleName { get; set; }
    public List<string> TraceContains { get; set; } = new();

    [JsonIgnore]
    public bool HasAssertions =>
        ShouldRewrite.HasValue ||
        FinalDamage.HasValue ||
        DesiredHp.HasValue ||
        FinalMpChange.HasValue ||
        DesiredMp.HasValue ||
        !string.IsNullOrWhiteSpace(RuleName) ||
        TraceContains.Any(text => !string.IsNullOrWhiteSpace(text));
}

internal sealed class RuntimeSimulationUnit
{
    public string Ptr { get; set; } = "";
    public int? CharId { get; set; }
    public int? Level { get; set; }
    public int? Hp { get; set; }
    public int? MaxHp { get; set; }
    public int? Mp { get; set; }
    public int? MaxMp { get; set; }
    public int? Team { get; set; }
    public bool? IsFoe { get; set; }
    public int? Pa { get; set; }
    public int? Ma { get; set; }
    public int? Speed { get; set; }
    public int? Move { get; set; }
    public int? Jump { get; set; }
    public int? Brave { get; set; }
    public int? Faith { get; set; }
    public Dictionary<string, int> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static RuntimeSimulationUnit TargetDefaults()
        => new()
        {
            Ptr = "0x2000",
            CharId = 0x80,
            Level = 12,
            MaxHp = 250,
            Mp = 18,
            MaxMp = 30,
            Team = 2,
            IsFoe = true,
            Pa = 10,
            Ma = 8,
            Speed = 7,
            Move = 4,
            Jump = 3,
            Brave = 70,
            Faith = 60,
            Raw = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["0x70"] = 172,
            },
        };

    public static RuntimeSimulationUnit AttackerDefaults()
        => new()
        {
            Ptr = "0x1000",
            CharId = 0x01,
            Level = 14,
            Hp = 40,
            MaxHp = 40,
            Mp = 12,
            MaxMp = 20,
            Team = 1,
            IsFoe = false,
            Pa = 12,
            Ma = 7,
            Speed = 8,
            Move = 5,
            Jump = 4,
            Brave = 75,
            Faith = 65,
            Raw = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["0x50"] = 19,
            },
        };

    public UnitSnapshot ToSnapshot(int fallbackHp)
    {
        var raw = new byte[0x180];
        foreach (var (offset, value) in Raw)
            WriteRaw(raw, offset, value);

        return new UnitSnapshot(
            (nint)ParseNumber(string.IsNullOrWhiteSpace(Ptr) ? "0" : Ptr),
            CharId ?? 0,
            Level ?? 1,
            Hp ?? fallbackHp,
            MaxHp ?? Math.Max(fallbackHp, 1),
            Team ?? 0,
            IsFoe ?? false,
            Pa ?? 0,
            Ma ?? 0,
            Speed ?? 0,
            Move ?? 0,
            Jump ?? 0,
            Brave ?? 0,
            Faith ?? 0,
            raw,
            Mp ?? 0,
            MaxMp ?? Math.Max(Mp ?? 0, 0));
    }

    private static void WriteRaw(byte[] raw, string offsetText, int value)
    {
        bool word = offsetText.EndsWith("w", StringComparison.OrdinalIgnoreCase) || value is < 0 or > 255;
        string cleanOffset = word && offsetText.EndsWith("w", StringComparison.OrdinalIgnoreCase)
            ? offsetText[..^1]
            : offsetText;
        int offset = (int)ParseNumber(cleanOffset);
        if (offset < 0 || offset >= raw.Length) return;

        if (word)
        {
            if (offset + 1 >= raw.Length) return;
            raw[offset] = (byte)(value & 0xFF);
            raw[offset + 1] = (byte)((value >> 8) & 0xFF);
            return;
        }

        raw[offset] = (byte)value;
    }

    private static long ParseNumber(string text)
    {
        text = text.Trim();
        return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt64(text[2..], 16)
            : Convert.ToInt64(text);
    }
}

internal sealed class RuntimeSimulationAction
{
    public string Name { get; set; } = "scenario";
    public string Source { get; set; } = "scenario";
    public Dictionary<string, int> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ActionSignal ToActionSignal()
    {
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Variables)
            variables[FormulaExpression.NormalizeIdentifierPart(key)] = value;
        if (!variables.ContainsKey("signal"))
            variables["signal"] = 1;
        return new ActionSignal(Name, Source, variables);
    }
}

internal sealed record RuntimeSimulationResult(
    string Name,
    string EventKind,
    bool ShouldRewrite,
    int PreviousHp,
    int CurrentHp,
    int VanillaDamage,
    int DesiredHp,
    int FinalDamage,
    int PreviousMp,
    int CurrentMp,
    int VanillaMpChange,
    int DesiredMp,
    int FinalMpChange,
    string RuleName,
    string Trace,
    bool HasExpectations,
    IReadOnlyList<string> ExpectationFailures)
{
    public bool ExpectationsPassed => ExpectationFailures.Count == 0;
}

internal static class RuntimeSettingsSimulator
{
    public static List<RuntimeSimulationScenario> DefaultScenarios()
        => new()
        {
            new RuntimeSimulationScenario
            {
                Name = "default-sword-vs-leather",
                PreviousHp = 250,
                CurrentHp = 230,
                VanillaDamage = 20,
            },
        };

    public static List<RuntimeSimulationScenario> LoadScenarios(string path)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var bundle = JsonSerializer.Deserialize<RuntimeSimulationBundle>(File.ReadAllText(path), options)
            ?? new RuntimeSimulationBundle();
        return bundle.Scenarios.Count == 0 ? DefaultScenarios() : bundle.Scenarios;
    }

    public static IReadOnlyList<RuntimeSimulationResult> Run(
        RuntimeSettings settings,
        ItemCatalog catalog,
        IEnumerable<RuntimeSimulationScenario> scenarios)
    {
        var engine = new BattleFormulaEngine(settings, catalog);
        var results = new List<RuntimeSimulationResult>();
        foreach (var scenario in scenarios)
        {
            UnitSnapshot target = scenario.Target.ToSnapshot(scenario.CurrentHp);
            UnitSnapshot? attacker = scenario.Attacker?.ToSnapshot(scenario.Attacker.Hp ?? scenario.Attacker.MaxHp ?? 40);
            ActionSignal? action = scenario.Action?.ToActionSignal();

            if (scenario.IsMpEvent)
            {
                int targetMaxMp = target.MaxMp > 0
                    ? target.MaxMp
                    : Math.Max(scenario.PreviousMp, scenario.CurrentMp);
                target = target with
                {
                    Mp = scenario.CurrentMp,
                    MaxMp = Math.Max(targetMaxMp, scenario.CurrentMp),
                };
                int vanillaMpChange = scenario.VanillaMpChange ?? scenario.CurrentMp - scenario.PreviousMp;
                var mpEvent = new MpEvent(
                    target,
                    scenario.PreviousMp,
                    scenario.CurrentMp,
                    vanillaMpChange,
                    attacker,
                    attacker is null ? "none" : scenario.AttackerSource,
                    action,
                    scenario.EventIndex,
                    scenario.EventSeed);
                MpResult mpResult = engine.EvaluateMp(mpEvent);
                IReadOnlyList<string> mpExpectationFailures = EvaluateMpExpectations(scenario, mpResult);
                results.Add(new RuntimeSimulationResult(
                    scenario.Name,
                    "mp",
                    mpResult.ShouldRewrite,
                    scenario.PreviousHp,
                    scenario.CurrentHp,
                    scenario.VanillaDamage,
                    target.Hp,
                    0,
                    scenario.PreviousMp,
                    scenario.CurrentMp,
                    vanillaMpChange,
                    mpResult.DesiredMp,
                    mpResult.FinalMpChange,
                    mpResult.RuleName,
                    mpResult.Trace,
                    scenario.Expect?.HasAssertions == true,
                    mpExpectationFailures));
                continue;
            }

            var damageEvent = new DamageEvent(
                target,
                scenario.PreviousHp,
                scenario.CurrentHp,
                scenario.VanillaDamage,
                attacker,
                attacker is null ? "none" : scenario.AttackerSource,
                action,
                scenario.EventIndex,
                scenario.EventSeed);
            DamageResult result = engine.Evaluate(damageEvent);
            IReadOnlyList<string> expectationFailures = EvaluateExpectations(scenario, result);
            results.Add(new RuntimeSimulationResult(
                scenario.Name,
                "hp",
                result.ShouldRewrite,
                scenario.PreviousHp,
                scenario.CurrentHp,
                scenario.VanillaDamage,
                result.DesiredHp,
                result.FinalDamage,
                target.Mp,
                target.Mp,
                0,
                target.Mp,
                0,
                result.RuleName,
                result.Trace,
                scenario.Expect?.HasAssertions == true,
                expectationFailures));
        }

        return results;
    }

    public static void AttachExpectations(
        IReadOnlyList<RuntimeSimulationScenario> scenarios,
        IReadOnlyList<RuntimeSimulationResult> results)
    {
        if (scenarios.Count != results.Count)
            throw new InvalidOperationException(
                $"scenario/result count mismatch: scenarios={scenarios.Count}, results={results.Count}");

        for (int i = 0; i < scenarios.Count; i++)
        {
            RuntimeSimulationResult result = results[i];
            scenarios[i].Expect = new RuntimeSimulationExpectation
            {
                ShouldRewrite = result.ShouldRewrite,
                FinalDamage = result.EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase) ? null : result.FinalDamage,
                DesiredHp = result.EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase) ? null : result.DesiredHp,
                FinalMpChange = result.EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase) ? result.FinalMpChange : null,
                DesiredMp = result.EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase) ? result.DesiredMp : null,
                RuleName = result.RuleName,
            };
        }
    }

    private static IReadOnlyList<string> EvaluateExpectations(RuntimeSimulationScenario scenario, DamageResult result)
    {
        var expect = scenario.Expect;
        if (expect?.HasAssertions != true)
            return Array.Empty<string>();

        var failures = new List<string>();
        if (expect.ShouldRewrite.HasValue && result.ShouldRewrite != expect.ShouldRewrite.Value)
            failures.Add($"shouldRewrite expected {expect.ShouldRewrite.Value}, got {result.ShouldRewrite}");
        if (expect.FinalDamage.HasValue && result.FinalDamage != expect.FinalDamage.Value)
            failures.Add($"finalDamage expected {expect.FinalDamage.Value}, got {result.FinalDamage}");
        if (expect.DesiredHp.HasValue && result.DesiredHp != expect.DesiredHp.Value)
            failures.Add($"desiredHp expected {expect.DesiredHp.Value}, got {result.DesiredHp}");
        if (!string.IsNullOrWhiteSpace(expect.RuleName) &&
            !string.Equals(result.RuleName, expect.RuleName, StringComparison.OrdinalIgnoreCase))
            failures.Add($"ruleName expected '{expect.RuleName}', got '{result.RuleName}'");

        foreach (string text in expect.TraceContains.Where(text => !string.IsNullOrWhiteSpace(text)))
        {
            if (!result.Trace.Contains(text, StringComparison.OrdinalIgnoreCase))
                failures.Add($"trace expected to contain '{text}'");
        }

        return failures;
    }

    private static IReadOnlyList<string> EvaluateMpExpectations(RuntimeSimulationScenario scenario, MpResult result)
    {
        var expect = scenario.Expect;
        if (expect?.HasAssertions != true)
            return Array.Empty<string>();

        var failures = new List<string>();
        if (expect.ShouldRewrite.HasValue && result.ShouldRewrite != expect.ShouldRewrite.Value)
            failures.Add($"shouldRewrite expected {expect.ShouldRewrite.Value}, got {result.ShouldRewrite}");
        if (expect.FinalMpChange.HasValue && result.FinalMpChange != expect.FinalMpChange.Value)
            failures.Add($"finalMpChange expected {expect.FinalMpChange.Value}, got {result.FinalMpChange}");
        if (expect.DesiredMp.HasValue && result.DesiredMp != expect.DesiredMp.Value)
            failures.Add($"desiredMp expected {expect.DesiredMp.Value}, got {result.DesiredMp}");
        if (!string.IsNullOrWhiteSpace(expect.RuleName) &&
            !string.Equals(result.RuleName, expect.RuleName, StringComparison.OrdinalIgnoreCase))
            failures.Add($"ruleName expected '{expect.RuleName}', got '{result.RuleName}'");

        foreach (string text in expect.TraceContains.Where(text => !string.IsNullOrWhiteSpace(text)))
        {
            if (!result.Trace.Contains(text, StringComparison.OrdinalIgnoreCase))
                failures.Add($"trace expected to contain '{text}'");
        }

        return failures;
    }
}
