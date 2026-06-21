using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using fftivc.generic.chronicle.codemod;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOutputOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    private static readonly JsonSerializerOptions ScenarioJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        string root = FindRepoRoot(AppContext.BaseDirectory);
        string settingsPath = options.SettingsPath ?? Path.Combine(root, "work", "battle-runtime-settings.v0.2.generated.json");
        string catalogPath = options.CatalogPath ?? Path.Combine(root, "work", "item_catalog.csv");

        if (!RuntimeSettings.TryLoad(settingsPath, out var settings, out string settingsError))
        {
            Console.Error.WriteLine($"settings load failed: {settingsError}");
            return 1;
        }

        var catalog = ItemCatalog.Load(catalogPath);
        var report = RuntimeSettingsValidator.Validate(settings, catalog);
        if (!report.Success && !options.SkipValidate)
        {
            Console.Error.WriteLine("settings validation failed; use --skip-validate to simulate anyway");
            foreach (var finding in report.Findings.Where(f => f.Severity.Equals("ERROR", StringComparison.OrdinalIgnoreCase)))
                Console.Error.WriteLine(finding);
            return 1;
        }

        List<RuntimeSimulationScenario> scenarios = options.ScenarioPath is null
            ? RuntimeSettingsSimulator.DefaultScenarios()
            : RuntimeSettingsSimulator.LoadScenarios(options.ScenarioPath);
        var results = RuntimeSettingsSimulator.Run(settings, catalog, scenarios);

        if (options.Json)
            Console.WriteLine(JsonSerializer.Serialize(results, JsonOutputOptions));
        else
            PrintText(results, includeTrace: !options.NoTrace);

        if (options.WriteScenariosWithExpectationsPath is not null)
        {
            RuntimeSettingsSimulator.AttachExpectations(scenarios, results);
            WriteScenarioBundle(options.WriteScenariosWithExpectationsPath, scenarios);
        }

        if (!options.IgnoreExpectations &&
            options.WriteScenariosWithExpectationsPath is null &&
            results.Any(result => !result.ExpectationsPassed))
            return 3;

        return results.Any(result =>
                !result.HasExpectations &&
                !result.ShouldRewrite &&
                !result.RuleName.Equals("off", StringComparison.OrdinalIgnoreCase))
            ? 2
            : 0;
    }

    private static void WriteScenarioBundle(string path, List<RuntimeSimulationScenario> scenarios)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var bundle = new RuntimeSimulationBundle { Scenarios = scenarios };
        File.WriteAllText(path, JsonSerializer.Serialize(bundle, ScenarioJsonOptions) + Environment.NewLine);
        Console.Error.WriteLine($"wrote scenarios with expectations -> {path}");
    }

    private static void PrintText(IReadOnlyList<RuntimeSimulationResult> results, bool includeTrace)
    {
        foreach (var result in results)
        {
            Console.WriteLine($"== {result.Name}");
            if (result.EventKind.Equals("mp", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"rewrite={result.ShouldRewrite} vanillaMpChange={result.VanillaMpChange} " +
                    $"mp={result.PreviousMp}->{result.CurrentMp}->{result.DesiredMp} " +
                    $"finalMpChange={result.FinalMpChange} rule={result.RuleName}");
            }
            else
            {
                Console.WriteLine(
                    $"rewrite={result.ShouldRewrite} vanillaDamage={result.VanillaDamage} " +
                    $"hp={result.PreviousHp}->{result.CurrentHp}->{result.DesiredHp} " +
                    $"finalDamage={result.FinalDamage} rule={result.RuleName}");
            }
            if (result.HasExpectations)
            {
                if (result.ExpectationsPassed)
                    Console.WriteLine("expect=pass");
                else
                    foreach (string failure in result.ExpectationFailures)
                        Console.WriteLine($"expect=fail {failure}");
            }
            if (includeTrace && !string.IsNullOrWhiteSpace(result.Trace))
                Console.WriteLine(result.Trace);
            Console.WriteLine();
        }
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "codemod")) &&
                Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Generic Chronicle runtime settings simulator

            Usage:
              dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- [settings.json] [scenarios.json] [--catalog work\item_catalog.csv] [--json]

            With no arguments, simulates work\battle-runtime-settings.v0.2.generated.json against a default sword-vs-leather damage scenario.
            When a scenario has an "expect" block, mismatches return exit code 3 unless --ignore-expectations is passed.
            Use --write-scenarios-with-expectations path.json to refresh scenario expectations from current runtime results.
            Use --no-trace for concise text output.
            Scenario JSON shape:
              HP: { "scenarios": [ { "name": "...", "previousHp": 50, "currentHp": 30, "vanillaDamage": 20, "target": { "raw": { "0x70": 172 } }, "attacker": { "raw": { "0x50": 19 } }, "expect": { "shouldRewrite": true, "finalDamage": 182 } } ] }
              MP: { "scenarios": [ { "name": "...", "eventKind": "mp", "previousMp": 20, "currentMp": 12, "target": { "mp": 12, "maxMp": 30 }, "expect": { "shouldRewrite": true, "finalMpChange": -11, "desiredMp": 9 } } ] }
            """);
    }

    private sealed class CliOptions
    {
        public string? SettingsPath { get; private set; }
        public string? ScenarioPath { get; private set; }
        public string? CatalogPath { get; private set; }
        public bool Json { get; private set; }
        public bool SkipValidate { get; private set; }
        public bool IgnoreExpectations { get; private set; }
        public bool NoTrace { get; private set; }
        public bool ShowHelp { get; private set; }
        public string? WriteScenariosWithExpectationsPath { get; private set; }

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();
            var positional = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg is "-h" or "--help")
                {
                    options.ShowHelp = true;
                }
                else if (arg == "--json")
                {
                    options.Json = true;
                }
                else if (arg == "--skip-validate")
                {
                    options.SkipValidate = true;
                }
                else if (arg == "--ignore-expectations")
                {
                    options.IgnoreExpectations = true;
                }
                else if (arg == "--no-trace")
                {
                    options.NoTrace = true;
                }
                else if (arg == "--write-scenarios-with-expectations")
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--write-scenarios-with-expectations requires a path");
                    options.WriteScenariosWithExpectationsPath = args[++i];
                }
                else if (arg == "--catalog")
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--catalog requires a path");
                    options.CatalogPath = args[++i];
                }
                else
                {
                    positional.Add(arg);
                }
            }

            if (positional.Count > 0) options.SettingsPath = positional[0];
            if (positional.Count > 1) options.ScenarioPath = positional[1];
            return options;
        }
    }
}
