using fftivc.generic.chronicle.codemod;

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        string root = FindRepoRoot(AppContext.BaseDirectory);
        string catalogPath = options.CatalogPath ?? Path.Combine(root, "work", "item_catalog.csv");
        var catalog = ItemCatalog.Load(catalogPath);
        var settingsPaths = options.SettingsPaths.Count > 0
            ? options.SettingsPaths
            : DefaultSettingsPaths(root);

        int filesWithErrors = 0;
        foreach (string path in settingsPaths)
        {
            if (!ValidateFile(path, catalog, options.Verbose))
                filesWithErrors++;
        }

        Console.WriteLine(filesWithErrors == 0
            ? $"settings validation passed ({settingsPaths.Count} file(s))"
            : $"settings validation failed ({filesWithErrors}/{settingsPaths.Count} file(s) with errors)");
        return filesWithErrors == 0 ? 0 : 1;
    }

    private static bool ValidateFile(string path, ItemCatalog catalog, bool verbose)
    {
        Console.WriteLine($"== {path}");
        if (!File.Exists(path))
        {
            Console.WriteLine("[ERROR] file: settings file not found");
            Console.WriteLine();
            return false;
        }

        if (!RuntimeSettings.TryLoad(path, out var settings, out string loadError))
        {
            Console.WriteLine($"[ERROR] load: {loadError}");
            Console.WriteLine();
            return false;
        }

        var report = RuntimeSettingsValidator.Validate(settings, catalog);
        foreach (var finding in report.Findings)
        {
            if (!verbose && finding.Severity.Equals("INFO", StringComparison.OrdinalIgnoreCase))
                continue;
            Console.WriteLine(finding);
        }
        Console.WriteLine($"summary: errors={report.ErrorCount}, warnings={report.WarningCount}");
        Console.WriteLine();
        return report.Success;
    }

    private static List<string> DefaultSettingsPaths(string root)
    {
        string[] candidates =
        [
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2-response.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.generated.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.matrix.generated.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.generated.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.live-noop.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.gurps-dr.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.static-dr.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.mp.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.sentinel-bands.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.dry-run.example.json"),
            Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.memtable-probe.disabled.example.json"),
            Path.Combine(root, "work", "battle-runtime-settings.v0.2.generated.json"),
            Path.Combine(root, "work", "battle-runtime-settings.v0.2.matrix.generated.json"),
            Path.Combine(root, "work", "battle-runtime-settings.v0.2.scan.generated.json"),
            Path.Combine(root, "work", "battle-runtime-settings.v0.2.scan.live-noop.json"),
            Path.Combine(root, "work", "battle-runtime-settings.neuter-spotcheck.json"),
            Path.Combine(root, "work", "battle-runtime-settings.death-flag-capture.json"),
            Path.Combine(root, "work", "battle-runtime-settings.hook-register-probe.json"),
            Path.Combine(root, "work", "battle-runtime-settings.custom-formula-demo.json"),
            Path.Combine(root, "work", "battle-runtime-settings.sentinel-coarse-v1.json"),
            Path.Combine(root, "work", "battle-runtime-settings.death-test.json"),
            Path.Combine(root, "work", "battle-runtime-settings.death-test-killflag.json"),
            Path.Combine(root, "work", "memtable-probe-candidates.disabled.json"),
        ];
        return candidates.Where(File.Exists).ToList();
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
            Generic Chronicle runtime settings validator

            Usage:
              dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -c Release -- [settings.json ...] [--catalog work\item_catalog.csv] [--verbose]

            With no settings files, validates the repo's generated/example runtime settings.
            """);
    }

    private sealed class CliOptions
    {
        public List<string> SettingsPaths { get; } = new();
        public string? CatalogPath { get; private set; }
        public bool Verbose { get; private set; }
        public bool ShowHelp { get; private set; }

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg is "-h" or "--help")
                {
                    options.ShowHelp = true;
                }
                else if (arg == "--verbose")
                {
                    options.Verbose = true;
                }
                else if (arg == "--catalog")
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--catalog requires a path");
                    options.CatalogPath = args[++i];
                }
                else
                {
                    options.SettingsPaths.Add(arg);
                }
            }

            return options;
        }
    }
}
