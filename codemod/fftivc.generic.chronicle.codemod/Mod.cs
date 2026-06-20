using System.Diagnostics;
using System.Text;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.Sigscan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using fftivc.generic.chronicle.codemod.Template;

namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Generic Chronicle - Battle Probe (diagnostic, iteration 1).
///
/// Does NOT hook or change anything. It only AOB-scans FFT_enhanced.exe for the battle-relevant
/// signatures we collected from the public cheat table / Nenkai layouts, and logs whether each
/// matches THIS build and at what module offset. Purpose: confirm we can locate the right code
/// before we hook it. Results go to the Reloaded console AND to battleprobe_log.txt next to this
/// mod's dll, so they are easy to copy back.
/// </summary>
public class Mod : ModBase
{
    private readonly IModLoader _modLoader;
    private readonly IReloadedHooks? _hooks;
    private readonly ILogger _logger;
    private readonly IMod _owner;
    private readonly IModConfig _modConfig;

    private readonly StringBuilder _report = new();
    private readonly string _logPath;

    // (name, pattern, note). Patterns are "IDA-style" hex with ?? wildcards.
    // Sources: bbfox FFT_enhanced.CT (Steam v1.0) + Nenkai OverrideAbilityActionData.layout.
    private static readonly (string Name, string Pattern, string Note)[] Signatures =
    {
        ("battle_base_ptr",   "0F B7 41 30 66 89 42 0C",                         "selected/battle unit base pointer site"),
        ("damage_multiplier", "0F B7 47 30 2B C2 85 C0 41 0F 4E CE 8A D1 E8 F2", "damage scaling site; result word stored at [rax+0x06]"),
        ("damage_mult_2",     "2B C8 8D 04 11",                                  "secondary damage-mult site"),
        ("jp_multiplier",     "03 C2 8B CF 41 3B C0",                            "JP gain site"),
        ("xp_multiplier",     "0F B7 84 7B 1E 01 00 00",                         "EXP gain site"),
        ("min_brave_faith",   "41 0F B6 5A 2B",                                  "brave/faith read site"),
        ("min_spd_jmp_mov",   "0F B6 47 42 66 89 43 30",                         "speed/jump/move read site"),
    };

    // Documented read-site RVA from OverrideAbilityActionData.layout (build "PC/Steam patch 1").
    private const long OverrideReadSiteRva = 0xEEA6E50;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;

        _logPath = Path.Combine(AppContext.BaseDirectory, "battleprobe_log.txt");

        Line("==== Generic Chronicle Battle Probe ====");
        try
        {
            var proc = Process.GetCurrentProcess();
            var module = proc.MainModule;
            nint baseAddr = module?.BaseAddress ?? 0;
            long size = module?.ModuleMemorySize ?? 0;
            Line($"process : {proc.ProcessName}.exe");
            Line($"module  : {module?.ModuleName}  base=0x{baseAddr:X}  size=0x{size:X}");

            // Sanity-check the documented read-site RVA against the real module size.
            string within = OverrideReadSiteRva < size ? "WITHIN module" : "OUT OF RANGE (build differs / bad RVA)";
            Line($"override read-site RVA 0x{OverrideReadSiteRva:X} -> {within} (would be VA 0x{(baseAddr + OverrideReadSiteRva):X})");

            var scannerCtl = _modLoader.GetController<IStartupScanner>();
            if (scannerCtl is null || !scannerCtl.TryGetTarget(out var scanner) || scanner is null)
            {
                Line("ERROR: IStartupScanner controller unavailable. Is Reloaded.Memory.SigScan.ReloadedII enabled?");
                Flush();
                return;
            }

            Line($"scanning {Signatures.Length} signatures (async; results below)...");
            foreach (var sig in Signatures)
            {
                var captured = sig;
                scanner.AddMainModuleScan(captured.Pattern, result =>
                {
                    if (result.Found)
                        Line($"[FOUND]    {captured.Name,-18} module+0x{result.Offset:X}  (VA 0x{(baseAddr + result.Offset):X})  - {captured.Note}");
                    else
                        Line($"[NOTFOUND] {captured.Name,-18} pattern did not match this build - {captured.Note}");
                    Flush();
                });
            }
        }
        catch (Exception ex)
        {
            Line("EXCEPTION: " + ex);
            Flush();
        }
    }

    private void Line(string s)
    {
        _logger.WriteLine($"[GC-Probe] {s}");
        _report.AppendLine(s);
    }

    private void Flush()
    {
        try { File.WriteAllText(_logPath, _report.ToString()); }
        catch { /* ignore */ }
    }

    #region For Exports, Serialization etc.
#pragma warning disable CS8618
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
