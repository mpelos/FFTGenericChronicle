using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using fftivc.generic.chronicle.codemod.Template;

namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Generic Chronicle - Battle Runtime Harness (iter 20).
///
/// Tool for the code-mod battle runtime path: hooks the stable .text anchor
/// 'battle_base_ptr' (rcx = a battle unit), copies the unit's stat block into our buffer, and a
/// background thread logs each distinct unit's stats (so we can SEE data-table edits take effect,
/// e.g. a changed PA multiplier) plus every real damage event via HP deltas. By default this is
/// read-only; an opt-in settings file can enable the first HP rewrite proof. No managed call on
/// the hot path. Output: battleprobe_log.txt next to the game exe.
/// </summary>
public class Mod : ModBase
{
    private readonly IModLoader _modLoader;
    private readonly IReloadedHooks? _hooks;
    private readonly ILogger _logger;
    private readonly IMod _owner;
    private readonly IModConfig _modConfig;

    private readonly StringBuilder _report = new();
    private readonly string _modDirectory;
    private readonly string _logPath;
    private readonly string _settingsPath;
    private RuntimeSettings _settings = new();
    private ItemCatalog _itemCatalog = ItemCatalog.Empty("");
    private BattleFormulaEngine _formulaEngine = null!;
    private BattleContextResolver _contextResolver = null!;
    private DateTime _settingsLastWriteUtc = DateTime.MinValue;
    private string _catalogPath = "";
    private DateTime _catalogLastWriteUtc = DateTime.MinValue;
    private long _lastRuntimeReloadCheckTick;

    private const string BattleBasePtr = "0F B7 41 30 66 89 42 0C"; // movzx eax,[rcx+0x30]; rcx=unit (stable)
    private const int DUMP = 0x180;       // copy 0x00..0x17F (stats + wider suspected gear/action region)
    private const int B_PTR = DUMP;       // native pointer-sized: rcx unit pointer
    private const int B_COUNT = DUMP + 8;
    private const int B_REGS = B_COUNT + 8;
    private const int REGISTER_COUNT = 16;
    private const int B_SIZE = B_REGS + (REGISTER_COUNT * 8);
    private static readonly string[] RegisterNames =
    [
        "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp", "rsp",
        "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
    ];
    private nint _buf;
    private Reloaded.Hooks.Definitions.IAsmHook? _hook;
    private Thread? _poller;
    private volatile bool _running = true;

    private readonly Dictionary<nint, int> _lastHp = new();   // unit pointer -> last HP (damage deltas)
    private readonly Dictionary<nint, int> _lastMp = new();   // unit pointer -> last MP (resource deltas)
    private readonly Dictionary<nint, long> _lastHpSampleTick = new();
    private readonly Dictionary<nint, long> _lastMpSampleTick = new();
    private readonly Dictionary<nint, string> _seen = new();  // unit pointer -> last stat line (re-log if it changes)
    private readonly HashSet<nint> _dumped = new();           // unit pointer -> emitted full struct dump
    private readonly Dictionary<nint, byte[]> _lastRaw = new();
    private readonly Dictionary<nint, int> _diffCounts = new();
    private readonly HashSet<nint> _unitRegistry = new();
    private readonly Dictionary<nint, UnitObservation> _unitObservations = new();
    private readonly Dictionary<nint, byte[]> _recentAliveRaw = new();   // unit -> last raw seen while HP>0 (death capture baseline)
    private readonly Dictionary<nint, byte[]> _deathCaptureRaw = new();  // unit -> raw at last death/follow tick (delayed-flag diff)
    private readonly Dictionary<nint, int> _deathCaptureFollow = new();  // unit -> remaining post-death follow-up dump ticks
    private readonly HashSet<nint> _deathCaptured = new();               // unit -> already captured this death (until revived)
    private readonly ValueRewriteEchoGuard _hpRewriteEchoGuard = new("HP");
    private readonly ValueRewriteEchoGuard _mpRewriteEchoGuard = new("MP");
    private long _battleEventIndex;
    private int _pollErrorCount;
    private int _registryLimitLogCount;
    private int _hookRegisterProbeLogs;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _modDirectory = GetModDirectory();
        _logPath = Path.Combine(AppContext.BaseDirectory, "battleprobe_log.txt");
        _settingsPath = Path.Combine(_modDirectory, "battle-runtime-settings.json");

        Line("==== Generic Chronicle Battle Runtime Harness (iter 20) ====");
        Line($"settings path: {_settingsPath}");
        ReloadRuntime(force: true);
        LogEnv();
        try
        {
            var module = Process.GetCurrentProcess().MainModule;
            nint baseAddr = module?.BaseAddress ?? 0;

            var ctl = _modLoader.GetController<IStartupScanner>();
            if (ctl is null || !ctl.TryGetTarget(out var scanner) || scanner is null)
            { Line("ERROR: IStartupScanner unavailable."); Flush(); return; }

            InstallMemoryTableProbes(scanner, baseAddr);

            _buf = Marshal.AllocHGlobal(B_SIZE);
            for (int i = 0; i < B_SIZE; i++) Marshal.WriteByte(_buf, i, 0);

            scanner.AddMainModuleScan(BattleBasePtr, r =>
            {
                if (!r.Found) { Line("[NOTFOUND] battle_base_ptr"); Flush(); return; }
                Line($"[FOUND] battle_base_ptr module+0x{r.Offset:X}");
                Install(baseAddr + r.Offset);
            });
        }
        catch (Exception ex) { Line("EXCEPTION: " + ex); Flush(); }
    }

    private void InstallMemoryTableProbes(IStartupScanner scanner, nint moduleBase)
    {
        int configured = _settings.MemoryTableProbes.Count;
        int enabled = _settings.MemoryTableProbes.Count(probe => probe.Enabled);
        if (configured == 0) return;

        Line($"[MEMTABLE] configured={configured} enabled={enabled}");
        if (enabled == 0)
        {
            Flush();
            return;
        }

        new MemoryTableProbeRunner(scanner, moduleBase, _settings.MemoryTableProbes, Line, Flush).Install();
        Flush();
    }

    private void Install(nint address)
    {
        if (_hooks is null) { Line("no IReloadedHooks"); Flush(); return; }
        try
        {
            string buf = $"0{_buf:X}h";
            var asm = new List<string>
            {
                "use64",
                "push rax",
                "push r8",
                "pushfq",
                $"mov rax, {buf}",
                $"mov r8, [rsp+16]",
                $"mov [rax+{B_REGS + 0 * 8}], r8",
                $"mov [rax+{B_REGS + 1 * 8}], rbx",
                $"mov [rax+{B_REGS + 2 * 8}], rcx",
                $"mov [rax+{B_REGS + 3 * 8}], rdx",
                $"mov [rax+{B_REGS + 4 * 8}], rsi",
                $"mov [rax+{B_REGS + 5 * 8}], rdi",
                $"mov [rax+{B_REGS + 6 * 8}], rbp",
                "lea r8, [rsp+24]",
                $"mov [rax+{B_REGS + 7 * 8}], r8",
                $"mov r8, [rsp+8]",
                $"mov [rax+{B_REGS + 8 * 8}], r8",
                $"mov [rax+{B_REGS + 9 * 8}], r9",
                $"mov [rax+{B_REGS + 10 * 8}], r10",
                $"mov [rax+{B_REGS + 11 * 8}], r11",
                $"mov [rax+{B_REGS + 12 * 8}], r12",
                $"mov [rax+{B_REGS + 13 * 8}], r13",
                $"mov [rax+{B_REGS + 14 * 8}], r14",
                $"mov [rax+{B_REGS + 15 * 8}], r15",
                $"mov [rax+{B_PTR}], rcx",
            };
            for (int off = 0; off < DUMP; off += 4) { asm.Add($"mov r8d, [rcx+{off}]"); asm.Add($"mov [rax+{off}], r8d"); }
            asm.Add($"mov r8d, [rax+{B_COUNT}]"); asm.Add("add r8d, 1"); asm.Add($"mov [rax+{B_COUNT}], r8d");
            asm.AddRange(new[] { "popfq", "pop r8", "pop rax" });

            _hook = _hooks.CreateAsmHook(asm.ToArray(), address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line($"[HOOK] validation harness @ 0x{address:X} (read-only).");
            Flush();
            _poller = new Thread(Poll) { IsBackground = true, Name = "GC-Harness" };
            _poller.Start();
        }
        catch (Exception ex) { Line("HOOK FAILED: " + ex); Flush(); }
    }

    private void Poll()
    {
        int lastHookCount = 0;
        while (_running)
        {
            try
            {
                ReloadRuntime(force: false);
                long nowTick = Stopwatch.GetTimestamp();
                CaptureHookObservation(ref lastHookCount, nowTick);
                PollRegisteredUnits(nowTick);
            }
            catch (Exception ex)
            {
                if (_pollErrorCount++ < 5)
                {
                    Line($"[POLL-ERROR] {ex.GetType().Name}: {ex.Message}");
                    Flush();
                }
            }
            Thread.Sleep(PollIntervalMs);
        }
    }

    private int PollIntervalMs => Math.Clamp(_settings.UnitPollIntervalMs, 1, 1000);

    private void CaptureHookObservation(ref int lastHookCount, long nowTick)
    {
        int count = Marshal.ReadInt32(_buf, B_COUNT);
        if (count == lastHookCount) return;

        lastHookCount = count;
        nint unitPtr = Marshal.ReadIntPtr(_buf, B_PTR);
        byte[] raw = ReadHookDumpBytes();
        if (!TryCreateUnitSnapshot(unitPtr, raw, out var target, out _)) return;

        LogHookRegisterProbeIfEnabled(count, target);
        ProcessObservedUnit(target, nowTick, touchForContext: true, logStructMapping: true);
    }

    private void PollRegisteredUnits(long nowTick)
    {
        if (_unitRegistry.Count == 0) return;

        foreach (var unitPtr in _unitRegistry.ToArray())
        {
            if (!TryReadLiveUnitSnapshot(unitPtr, out var target, out string error))
            {
                ForgetRegisteredUnit(unitPtr, error);
                continue;
            }

            ProcessObservedUnit(target, nowTick, touchForContext: false, logStructMapping: false);
        }
    }

    private void ProcessObservedUnit(UnitSnapshot target, long nowTick, bool touchForContext, bool logStructMapping)
    {
        nint unitPtr = target.Ptr;
        int id = target.CharId;
        int hp = target.Hp;
        int mp = target.Mp;
        bool needsFlush = false;

        if (!_unitRegistry.Contains(unitPtr) && _unitRegistry.Count >= MaxTrackedBattleUnits)
        {
            if (_registryLimitLogCount++ < 8)
            {
                Line($"[UNIT-SKIP ptr=0x{unitPtr:X} id=0x{id:X2}] registry full {_unitRegistry.Count}/{MaxTrackedBattleUnits}");
                Flush();
            }
            return;
        }

        _unitRegistry.Add(unitPtr);
        // Track CT (+0x41) per unit so the attacker resolver can identify who just acted: a unit
        // whose CT dropped between polls just took its turn (classic FFT charge-time reset).
        _unitObservations.TryGetValue(unitPtr, out var previousObservation);
        long ctDropTick = previousObservation?.CtDropTick ?? 0;
        int ctDropAmount = previousObservation?.CtDropAmount ?? 0;
        bool ctDroppedToLow = false;
        if (previousObservation is not null && target.Ct < previousObservation.Unit.Ct)
        {
            ctDropTick = nowTick;
            ctDropAmount = previousObservation.Unit.Ct - target.Ct;
            ctDroppedToLow = target.Ct <= Math.Clamp(_settings.CtLowFallbackMaxCt, 0, 100);
            if (_settings.LogCtResolutionDiagnostics)
            {
                Line(
                    $"[CT-DROP ptr=0x{unitPtr:X} id=0x{id:X2}] " +
                    $"{previousObservation.Unit.Ct}->{target.Ct} amount={ctDropAmount} " +
                    $"touch={(touchForContext ? 1 : 0)}");
                needsFlush = true;
            }
        }
        // Hook touches set a fresh SeenTick (legacy recency signal); polls keep the prior SeenTick
        // (0 until first hook-touched, so poll-discovered units stay out of the recency window while
        // still accumulating CT history). A CT drop into the low fallback band is also an action signal,
        // so keep it eligible for ct-low even when the hook never touched the actor directly.
        long seenTick = (touchForContext || (_settings.ResolveAttackerByLowCtFallback && ctDroppedToLow))
            ? nowTick
            : (previousObservation?.SeenTick ?? 0);
        _unitObservations[unitPtr] = new UnitObservation(target, seenTick, ctDropTick, ctDropAmount);

        if (_settings.CaptureStructOnDeath) DeathCaptureTick(target);

        string lineStats = target.StatLine;
        if (!_seen.TryGetValue(unitPtr, out var prevStats) || prevStats != lineStats)
        {
            _seen[unitPtr] = lineStats;
            Line($"[UNIT ptr=0x{unitPtr:X} id=0x{id:X2} {target.FactionLabel} t{target.Team}] {lineStats}");
            needsFlush = true;
        }

        if (logStructMapping)
        {
            if (_dumped.Add(unitPtr))
            {
                Line($"[DUMP ptr=0x{unitPtr:X} id=0x{id:X2}] {HexDump(target.Raw)}");
                Line($"[CANDIDATES ptr=0x{unitPtr:X} id=0x{id:X2}] {CandidateSummary(target.Raw)}");
                needsFlush = true;
            }
            else
            {
                LogUnknownDiffs(unitPtr, id, target.Raw);
            }
            _lastRaw[unitPtr] = target.Raw;
        }

        int trackingHp = hp;
        int hpSampleAgeMs = _lastHpSampleTick.TryGetValue(unitPtr, out long hpSampleTick)
            ? ElapsedMs(nowTick, hpSampleTick)
            : -1;
        if (_lastHp.TryGetValue(unitPtr, out int prev) && hp != prev)
        {
            if (_hpRewriteEchoGuard.TrySuppress(unitPtr, prev, hp, nowTick, _settings.SuppressOwnRewriteEchoWindowMs, out string echoReason))
            {
                Line($"[REWRITE-ECHO-SKIP ptr=0x{unitPtr:X} id=0x{id:X2}] {echoReason}");
            }
            else
            {
                int signedDamage = prev - hp;
                string eventTag = signedDamage > 0 ? "DAMAGE" : "HEALING";
                Line($"[{eventTag} ptr=0x{unitPtr:X} id=0x{id:X2}] {prev} -> {hp} = {Math.Abs(signedDamage)} sampleAgeMs={hpSampleAgeMs}");
                if (_settings.ActorProbeOnEvent) LogActorProbe(id);
                long eventIndex = Interlocked.Increment(ref _battleEventIndex);
                long eventSeed = ComputeEventSeed(target, eventIndex, prev, hp, signedDamage);
                var attacker = _contextResolver.ResolveRecentAttacker(target, _unitObservations, nowTick);
                if (_settings.LogAttackerCandidates)
                {
                    string resolved = attacker.Unit is null
                        ? "resolved=none"
                        : $"resolved=0x{attacker.Unit.Ptr:X} source={attacker.Source}";
                    Line($"[CTX ptr=0x{unitPtr:X} id=0x{id:X2}] {resolved} {attacker.Summary}");
                }
                trackingHp = MaybeRewriteHpEvent(new DamageEvent(target, prev, hp, signedDamage, attacker.Unit, attacker.Source, EventIndex: eventIndex, EventSeed: eventSeed));
                _contextResolver.RememberHpDamageEvent(target, attacker.Unit, attacker.Source, signedDamage, nowTick, eventIndex);
            }
            needsFlush = true;
        }
        _lastHp[unitPtr] = trackingHp;
        _lastHpSampleTick[unitPtr] = nowTick;

        int trackingMp = mp;
        int mpSampleAgeMs = _lastMpSampleTick.TryGetValue(unitPtr, out long mpSampleTick)
            ? ElapsedMs(nowTick, mpSampleTick)
            : -1;
        if (_lastMp.TryGetValue(unitPtr, out int prevMp) && mp != prevMp)
        {
            if (_mpRewriteEchoGuard.TrySuppress(unitPtr, prevMp, mp, nowTick, _settings.SuppressOwnRewriteEchoWindowMs, out string echoReason))
            {
                Line($"[MP-REWRITE-ECHO-SKIP ptr=0x{unitPtr:X} id=0x{id:X2}] {echoReason}");
            }
            else
            {
                int signedMpChange = mp - prevMp;
                string eventTag = signedMpChange < 0 ? "MPLOSS" : "MPGAIN";
                Line($"[{eventTag} ptr=0x{unitPtr:X} id=0x{id:X2}] {prevMp} -> {mp} = {Math.Abs(signedMpChange)} sampleAgeMs={mpSampleAgeMs}");
                long eventIndex = Interlocked.Increment(ref _battleEventIndex);
                long eventSeed = ComputeEventSeed(target, eventIndex, prevMp, mp, signedMpChange);
                var attacker = _contextResolver.ResolveRecentAttacker(target, _unitObservations, nowTick);
                if (_settings.LogAttackerCandidates)
                {
                    string resolved = attacker.Unit is null
                        ? "resolved=none"
                        : $"resolved=0x{attacker.Unit.Ptr:X} source={attacker.Source}";
                    Line($"[MP-CTX ptr=0x{unitPtr:X} id=0x{id:X2}] {resolved} {attacker.Summary}");
                }
                trackingMp = MaybeRewriteMpEvent(new MpEvent(target, prevMp, mp, signedMpChange, attacker.Unit, attacker.Source, EventIndex: eventIndex, EventSeed: eventSeed));
            }
            needsFlush = true;
        }
        _lastMp[unitPtr] = trackingMp;
        _lastMpSampleTick[unitPtr] = nowTick;

        if (needsFlush) Flush();
    }

    private bool TryReadLiveUnitSnapshot(nint unitPtr, out UnitSnapshot target, out string error)
    {
        target = null!;
        var raw = new byte[DUMP];
        if (!CurrentProcessMemory.TryRead(unitPtr, raw, out error))
            return false;

        return TryCreateUnitSnapshot(unitPtr, raw, out target, out error);
    }

    private static bool TryCreateUnitSnapshot(nint unitPtr, byte[] raw, out UnitSnapshot target, out string error)
    {
        target = null!;
        error = "";

        if (unitPtr == 0)
        {
            error = "null unit pointer";
            return false;
        }
        if (raw.Length < DUMP)
        {
            error = $"short unit snapshot {raw.Length}/0x{DUMP:X}";
            return false;
        }

        int rb(int o) => raw[o];
        int rw(int o) => raw[o] | (raw[o + 1] << 8);

        int id = rb(0);
        int lvl = rb(0x29);
        int hp = rw(0x30);
        int maxhp = rw(0x32);
        int mp = rw(0x34);
        int maxmp = rw(0x36);

        if (lvl is < 1 or > 99)
        {
            error = $"invalid level {lvl}";
            return false;
        }
        if (maxhp is < 1 or > 9999 || hp > maxhp)
        {
            error = $"invalid HP {hp}/{maxhp}";
            return false;
        }
        if (maxmp is < 0 or > 9999 || mp > maxmp)
        {
            error = $"invalid MP {mp}/{maxmp}";
            return false;
        }

        int team = rb(4);
        int foe = rb(5) & 0x10;
        int pa = rb(0x3E);
        int ma = rb(0x3F);
        int spd = rb(0x40);
        int ct = rb(0x41);
        int mov = rb(0x42);
        int jmp = rb(0x43);
        int br = rb(0x2B);
        int fa = rb(0x2D);
        target = new UnitSnapshot(unitPtr, id, lvl, hp, maxhp, team, foe != 0, pa, ma, spd, mov, jmp, br, fa, raw, mp, maxmp, ct);
        return true;
    }

    private void ForgetRegisteredUnit(nint unitPtr, string reason)
    {
        _unitRegistry.Remove(unitPtr);
        _lastHp.Remove(unitPtr);
        _lastMp.Remove(unitPtr);
        _lastHpSampleTick.Remove(unitPtr);
        _lastMpSampleTick.Remove(unitPtr);
        _seen.Remove(unitPtr);
        _dumped.Remove(unitPtr);
        _lastRaw.Remove(unitPtr);
        _diffCounts.Remove(unitPtr);
        _unitObservations.Remove(unitPtr);
        _recentAliveRaw.Remove(unitPtr);
        _deathCaptureRaw.Remove(unitPtr);
        _deathCaptureFollow.Remove(unitPtr);
        _deathCaptured.Remove(unitPtr);
        Line($"[UNIT-LOST ptr=0x{unitPtr:X}] {reason}");
        Flush();
    }

    private int MaxTrackedBattleUnits => Math.Clamp(_settings.MaxTrackedBattleUnits, 1, 512);

    private static int ElapsedMs(long nowTick, long previousTick)
        => (int)Math.Round((nowTick - previousTick) * 1000.0 / Stopwatch.Frequency);

    // Reflection-based: log which mods are actually LOADED in this process + the app's EnabledMods.
    private void LogEnv()
    {
        try
        {
            object? app = _modLoader.GetType().GetMethod("GetAppConfig")?.Invoke(_modLoader, null);
            if (app?.GetType().GetProperty("EnabledMods")?.GetValue(app) is IEnumerable en)
                Line("EnabledMods: " + string.Join(", ", en.Cast<object>().Select(o => o?.ToString())));
        }
        catch (Exception e) { Line("GetAppConfig err: " + e.Message); }

        try
        {
            object? active = _modLoader.GetType().GetMethod("GetActiveMods")?.Invoke(_modLoader, null);
            var ids = new List<string>();
            if (active is IEnumerable mods)
                foreach (var m in mods)
                {
                    object? o = m;
                    var idp = o?.GetType().GetProperty("ModId");
                    if (idp == null)
                    {
                        o = o?.GetType().GetProperty("Config")?.GetValue(o)
                            ?? o?.GetType().GetProperty("Generic")?.GetValue(o);
                        idp = o?.GetType().GetProperty("ModId");
                    }
                    var id = idp?.GetValue(o)?.ToString();
                    if (id != null) ids.Add(id);
                }
            Line($"ACTIVE MODS ({ids.Count}): " + string.Join(", ", ids));
            Line("data mod fftivc.generic.chronicle loaded? " + ids.Contains("fftivc.generic.chronicle"));
        }
        catch (Exception e) { Line("GetActiveMods err: " + e.Message); }
        Flush();
    }

    private void Line(string s) { _logger.WriteLine($"[GC-Probe] {s}"); _report.AppendLine(s); }
    private void Flush() { try { File.WriteAllText(_logPath, _report.ToString()); } catch { } }
    private string GetModDirectory()
    {
        try { return _modLoader.GetDirectoryForModId(_modConfig.ModId); }
        catch { return AppContext.BaseDirectory; }
    }

    private static string ResolveModPath(string path, string modDirectory)
    {
        if (string.IsNullOrWhiteSpace(path)) path = "item_catalog.csv";
        return Path.IsPathRooted(path) ? path : Path.Combine(modDirectory, path);
    }

    private void ReloadRuntime(bool force)
    {
        long nowTick = Stopwatch.GetTimestamp();
        if (!force)
        {
            long elapsedMs = (nowTick - _lastRuntimeReloadCheckTick) * 1000 / Stopwatch.Frequency;
            if (elapsedMs < 1000) return;
        }
        _lastRuntimeReloadCheckTick = nowTick;

        DateTime settingsWrite = File.Exists(_settingsPath)
            ? File.GetLastWriteTimeUtc(_settingsPath)
            : DateTime.MinValue;
        bool settingsChanged = force || settingsWrite != _settingsLastWriteUtc;

        RuntimeSettings nextSettings = _settings;
        if (settingsChanged)
        {
            if (!RuntimeSettings.TryLoad(_settingsPath, out nextSettings, out string error))
            {
                Line($"[SETTINGS-RELOAD-FAILED] {error}");
                Flush();
                return;
            }
        }

        string nextCatalogPath = ResolveModPath(nextSettings.ItemCatalogPath, _modDirectory);
        DateTime catalogWrite = File.Exists(nextCatalogPath)
            ? File.GetLastWriteTimeUtc(nextCatalogPath)
            : DateTime.MinValue;
        bool catalogChanged = force ||
                              settingsChanged ||
                              !nextCatalogPath.Equals(_catalogPath, StringComparison.OrdinalIgnoreCase) ||
                              catalogWrite != _catalogLastWriteUtc;

        ItemCatalog nextCatalog = _itemCatalog;
        if (catalogChanged)
            nextCatalog = ItemCatalog.Load(nextCatalogPath);

        if (!settingsChanged && !catalogChanged) return;

        _settings = nextSettings;
        _itemCatalog = nextCatalog;
        _formulaEngine = new BattleFormulaEngine(_settings, _itemCatalog);
        _contextResolver = new BattleContextResolver(_settings);
        _settingsLastWriteUtc = settingsWrite;
        _catalogPath = nextCatalogPath;
        _catalogLastWriteUtc = catalogWrite;

        string tag = force ? "RUNTIME-INIT" : settingsChanged ? "SETTINGS-RELOAD" : "CATALOG-RELOAD";
        Line($"[{tag}] settings: {_settings.Describe()}");
        Line($"[{tag}] item catalog: {_itemCatalog.Describe()}");
        Flush();
    }

    private static long ComputeEventSeed(UnitSnapshot target, long eventIndex, int previousHp, int currentHp, int vanillaDamage)
    {
        unchecked
        {
            ulong seed = 14695981039346656037UL;
            void Mix(ulong value)
            {
                seed ^= value;
                seed *= 1099511628211UL;
            }

            Mix((ulong)target.Ptr.ToInt64());
            Mix((ulong)target.CharId);
            Mix((ulong)target.Team);
            Mix((ulong)previousHp);
            Mix((ulong)currentHp);
            Mix((ulong)vanillaDamage);
            Mix((ulong)eventIndex);
            return (long)seed;
        }
    }

    private int MaybeRewriteHpEvent(DamageEvent damageEvent)
    {
        // Engine-owned death (MinHpFloor mode): if the engine itself brought this unit to 0 HP, leave it
        // dead. We never write below MinHpFloor, so an observed 0 is a real engine kill; rewriting it would
        // resurrect a unit the engine has already KO'd.
        if (_settings.MinHpFloor > 0 && damageEvent.CurrentHp <= 0)
        {
            Line($"[REWRITE-SKIP-DEATH ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] engine reached 0 HP; leaving dead");
            return damageEvent.CurrentHp;
        }
        var result = _formulaEngine.Evaluate(damageEvent);
        if (_settings.LogResolvedRuntimeContext && !string.IsNullOrWhiteSpace(result.Trace))
            Line($"[RUNTIME ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] {result.Trace}");

        if (!result.ShouldRewrite)
        {
            if (!result.RuleName.Equals("off", StringComparison.OrdinalIgnoreCase))
                Line($"[REWRITE-SKIP ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] reason={result.RuleName}");
            return damageEvent.CurrentHp;
        }
        var decision = RewriteApplication.Decide(_settings.DryRunRewrites, damageEvent.CurrentHp, result.DesiredHp);
        if (decision.ShouldLogDryRun)
        {
            Line($"[REWRITE-DRY-RUN ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] rule={result.RuleName} vanillaDamage={damageEvent.VanillaDamage} finalDamage={result.FinalDamage} HP {damageEvent.CurrentHp}->{result.DesiredHp}");
            return decision.TrackingValue;
        }
        if (!decision.ShouldWrite) return decision.TrackingValue;

        try
        {
            if (!CurrentProcessMemory.TryWriteInt16(damageEvent.Target.Ptr + 0x30, (short)result.DesiredHp, out string writeError))
            {
                Line($"[REWRITE-FAILED ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] {writeError}");
                return damageEvent.CurrentHp;
            }
            _hpRewriteEchoGuard.Remember(damageEvent.Target.Ptr, damageEvent.CurrentHp, result.DesiredHp, Stopwatch.GetTimestamp());
            Line($"[REWRITE ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] rule={result.RuleName} vanillaDamage={damageEvent.VanillaDamage} finalDamage={result.FinalDamage} HP {damageEvent.CurrentHp}->{result.DesiredHp}");
            var readBack = new byte[2];
            if (CurrentProcessMemory.TryRead(damageEvent.Target.Ptr + 0x30, readBack, out string readBackError))
            {
                int readBackHp = readBack[0] | (readBack[1] << 8);
                Line($"[REWRITE-VERIFY ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] readBackHp={readBackHp} desiredHp={result.DesiredHp}");
            }
            else
            {
                Line($"[REWRITE-VERIFY-FAILED ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] {readBackError}");
            }
            if (result.DesiredHp == 0 && _settings.CauseDeathOnZeroHp)
                ApplyDeathStateWrites(damageEvent.Target);
            return decision.TrackingValue;
        }
        catch (Exception ex)
        {
            Line($"[REWRITE-FAILED ptr=0x{damageEvent.Target.Ptr:X} id=0x{damageEvent.Target.CharId:X2}] {ex.GetType().Name}: {ex.Message}");
            return damageEvent.CurrentHp;
        }
    }

    // Snapshot a small byte window of every registered unit at a damage event, so we can correlate which
    // unit just acted (the attacker) with the target taking damage. The window is JSON-tunable.
    private void LogActorProbe(int targetId)
    {
        int start = Math.Clamp(_settings.ActorProbeStart, 0, DUMP - 1);
        int end = Math.Clamp(_settings.ActorProbeEnd, start, DUMP - 1);
        var parts = new List<string>();
        foreach (var ptr in _unitRegistry.ToArray())
        {
            byte[] raw = new byte[DUMP];
            if (!CurrentProcessMemory.TryRead(ptr, raw, out _)) continue;
            parts.Add($"{raw[0]:X2}@{Convert.ToHexString(raw, start, end - start + 1)}");
        }
        Line($"[ACTOR-PROBE tgt=0x{targetId:X2} off=0x{start:X2}-0x{end:X2}] {string.Join(" ", parts)}");
    }

    private int MaybeRewriteMpEvent(MpEvent mpEvent)
    {
        var result = _formulaEngine.EvaluateMp(mpEvent);
        if (_settings.LogResolvedRuntimeContext && !string.IsNullOrWhiteSpace(result.Trace))
            Line($"[RUNTIME-MP ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] {result.Trace}");

        if (!result.ShouldRewrite)
        {
            if (!result.RuleName.Equals("off", StringComparison.OrdinalIgnoreCase))
                Line($"[MP-REWRITE-SKIP ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] reason={result.RuleName}");
            return mpEvent.CurrentMp;
        }
        var decision = RewriteApplication.Decide(_settings.DryRunRewrites, mpEvent.CurrentMp, result.DesiredMp);
        if (decision.ShouldLogDryRun)
        {
            Line($"[MP-REWRITE-DRY-RUN ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] rule={result.RuleName} vanillaMpChange={mpEvent.VanillaMpChange} finalMpChange={result.FinalMpChange} MP {mpEvent.CurrentMp}->{result.DesiredMp}");
            return decision.TrackingValue;
        }
        if (!decision.ShouldWrite) return decision.TrackingValue;

        try
        {
            if (!CurrentProcessMemory.TryWriteInt16(mpEvent.Target.Ptr + 0x34, (short)result.DesiredMp, out string writeError))
            {
                Line($"[MP-REWRITE-FAILED ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] {writeError}");
                return mpEvent.CurrentMp;
            }
            _mpRewriteEchoGuard.Remember(mpEvent.Target.Ptr, mpEvent.CurrentMp, result.DesiredMp, Stopwatch.GetTimestamp());
            Line($"[MP-REWRITE ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] rule={result.RuleName} vanillaMpChange={mpEvent.VanillaMpChange} finalMpChange={result.FinalMpChange} MP {mpEvent.CurrentMp}->{result.DesiredMp}");
            return decision.TrackingValue;
        }
        catch (Exception ex)
        {
            Line($"[MP-REWRITE-FAILED ptr=0x{mpEvent.Target.Ptr:X} id=0x{mpEvent.Target.CharId:X2}] {ex.GetType().Name}: {ex.Message}");
            return mpEvent.CurrentMp;
        }
    }

    private byte[] ReadHookDumpBytes()
    {
        var bytes = new byte[DUMP];
        Marshal.Copy(_buf, bytes, 0, DUMP);
        return bytes;
    }

    private nint[] ReadHookRegisters()
    {
        var registers = new nint[REGISTER_COUNT];
        for (int i = 0; i < registers.Length; i++)
            registers[i] = Marshal.ReadIntPtr(_buf, B_REGS + (i * IntPtr.Size));
        return registers;
    }

    private void LogHookRegisterProbeIfEnabled(int hookCount, UnitSnapshot touched)
    {
        if (!_settings.HookRegisterProbe) return;
        int maxLogs = Math.Clamp(_settings.HookRegisterProbeMaxLogs, 0, 10000);
        if (_hookRegisterProbeLogs >= maxLogs) return;

        nint[] registers = ReadHookRegisters();
        var parts = new List<string>(registers.Length);
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
            parts.Add($"{RegisterNames[i]}=0x{registers[i]:X}:{ClassifyRegisterValue(registers[i], touched)}");

        _hookRegisterProbeLogs++;
        Line($"[HOOK-REGS count={hookCount} ptr=0x{touched.Ptr:X} id=0x{touched.CharId:X2}] {string.Join(" ", parts)}");
        Flush();
    }

    private string ClassifyRegisterValue(nint value, UnitSnapshot touched)
    {
        if (value == 0) return "zero";
        if (value == touched.Ptr) return "unit:touched";
        if (_unitObservations.TryGetValue(value, out var observation))
            return $"unit:id=0x{observation.Unit.CharId:X2}:team={observation.Unit.Team}:hp={observation.Unit.Hp}:ct={observation.Unit.Ct}";
        if (ReadableMemoryRange.IsReadable(value, IntPtr.Size))
            return "readable";
        return "unreadable";
    }

    private void LogUnknownDiffs(nint unitPtr, int id, byte[] raw)
    {
        if (!_settings.LogUnknownFieldDiffs) return;
        if (!_lastRaw.TryGetValue(unitPtr, out var prevRaw)) return;

        int emitted = _diffCounts.GetValueOrDefault(unitPtr);
        if (emitted >= _settings.MaxUnknownDiffsPerUnit) return;

        int start = Math.Clamp(_settings.UnknownDiffStart, 0, DUMP - 1);
        int end = Math.Clamp(_settings.UnknownDiffEnd, start, DUMP - 1);
        var diffs = new List<string>();

        for (int i = start; i <= end; i++)
        {
            if (prevRaw[i] == raw[i]) continue;
            diffs.Add($"+0x{i:X2}:{prevRaw[i]:X2}->{raw[i]:X2}");
            if (diffs.Count >= 12) break;
        }

        if (diffs.Count == 0) return;

        _diffCounts[unitPtr] = emitted + diffs.Count;
        Line($"[DIFF ptr=0x{unitPtr:X} id=0x{id:X2}] {string.Join(" ", diffs)}");
        Flush();
    }

    // Death-state RE capture. While a unit is alive, keep its most recent raw snapshot. The first tick
    // it is OBSERVED at 0 HP (by any cause - vanilla kill OR our own HP=0 write), dump the full struct
    // and diff alive->dead, then keep diffing for a short window to catch the engine setting the
    // death/status flag a few ticks later. The changed offsets are the candidate death/status field,
    // which is currently unmapped (see docs/modding/05, 07). Observing at 0 HP (not the transition
    // event) means this works even when our own rewrite sets _lastHp to 0 and no delta re-fires.
    private void DeathCaptureTick(UnitSnapshot target)
    {
        nint ptr = target.Ptr;

        if (_deathCaptureFollow.TryGetValue(ptr, out int remaining) && remaining > 0)
        {
            if (_deathCaptureRaw.TryGetValue(ptr, out var prevRaw))
            {
                var diffs = StructByteDiffs(prevRaw, target.Raw, 0, DUMP - 1, 24);
                if (diffs.Count > 0)
                {
                    Line($"[DEATH-FOLLOW ptr=0x{ptr:X} id=0x{target.CharId:X2} hp={target.Hp}] {string.Join(" ", diffs)}");
                    Flush();
                }
            }
            _deathCaptureRaw[ptr] = target.Raw;
            _deathCaptureFollow[ptr] = remaining - 1;
            return;
        }

        if (target.Hp > 0)
        {
            _recentAliveRaw[ptr] = target.Raw;
            _deathCaptured.Remove(ptr); // revived/refreshed unit can be captured on a later death
            return;
        }

        // hp == 0, not already captured: a fresh death by any cause. Capture once.
        if (!_deathCaptured.Add(ptr)) return;

        int prevHp = _recentAliveRaw.TryGetValue(ptr, out var aliveRaw)
            ? aliveRaw[0x30] | (aliveRaw[0x31] << 8)
            : -1;
        Line($"[DEATH-DUMP ptr=0x{ptr:X} id=0x{target.CharId:X2} prevHp={prevHp}] {HexDump(target.Raw)}");
        if (aliveRaw is not null)
        {
            var diffs = StructByteDiffs(aliveRaw, target.Raw, 0, DUMP - 1, 24);
            Line($"[DEATH-DIFF ptr=0x{ptr:X} id=0x{target.CharId:X2}] alive->dead {(diffs.Count == 0 ? "none" : string.Join(" ", diffs))}");
        }
        else
        {
            Line($"[DEATH-DIFF ptr=0x{ptr:X} id=0x{target.CharId:X2}] alive->dead no-alive-baseline (unit first seen already dead)");
        }
        _deathCaptureRaw[ptr] = target.Raw;
        _deathCaptureFollow[ptr] = Math.Clamp(_settings.DeathCaptureFollowTicks, 0, 4000);
        Flush();
    }

    private static List<string> StructByteDiffs(byte[] a, byte[] b, int start, int end, int max)
    {
        var diffs = new List<string>();
        int limit = Math.Min(a.Length, b.Length);
        if (limit == 0) return diffs;
        start = Math.Clamp(start, 0, limit - 1);
        end = Math.Clamp(end, start, limit - 1);
        for (int i = start; i <= end; i++)
        {
            if (a[i] == b[i]) continue;
            diffs.Add($"+0x{i:X2}:{a[i]:X2}->{b[i]:X2}");
            if (diffs.Count >= max) break;
        }
        return diffs;
    }

    private void ApplyDeathStateWrites(UnitSnapshot target)
    {
        if (_settings.DeathStateWrites.Count == 0)
        {
            Line($"[DEATH-WRITE-SKIP ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] CauseDeathOnZeroHp is on but no DeathStateWrites are configured");
            Flush();
            return;
        }
        foreach (var write in _settings.DeathStateWrites)
        {
            if (write is null || write.Offset < 0) continue;
            if (!write.TryApply(target.Ptr, out string desc, out string error))
                Line($"[DEATH-WRITE-FAILED ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {(string.IsNullOrWhiteSpace(write.Name) ? "death" : write.Name)}: {error}");
            else
                Line($"[DEATH-WRITE ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {desc}");
        }
        Flush();
    }

    private string CandidateSummary(byte[] raw)
    {
        int start = Math.Clamp(_settings.UnknownDiffStart, 0, DUMP - 1);
        int end = Math.Clamp(_settings.UnknownDiffEnd, start, DUMP - 1);
        var windows = new List<string>();

        for (int windowStart = start; windowStart <= end; windowStart += 0x40)
        {
            int windowEnd = Math.Min(end, windowStart + 0x3F);
            var bytes = new List<string>();
            var words = new List<string>();

            for (int i = windowStart; i <= windowEnd; i++)
            {
                if (raw[i] != 0)
                    bytes.Add($"+0x{i:X2}={raw[i]}");
            }

            for (int i = windowStart; i + 1 <= windowEnd; i += 2)
            {
                int word = raw[i] | (raw[i + 1] << 8);
                if (word is > 0 and <= 511)
                    words.Add($"+0x{i:X2}w={word}");
            }

            string byteText = bytes.Count == 0 ? "none" : string.Join(" ", bytes.Take(14));
            string wordText = words.Count == 0 ? "none" : string.Join(" ", words.Take(10));
            windows.Add($"bytes[{windowStart:X2}-{windowEnd:X2}] {byteText} | words<=511 {wordText}");
        }

        return string.Join(" || ", windows);
    }

    private string HexDump(byte[] raw)
    {
        var sb = new StringBuilder(DUMP * 3);
        for (int i = 0; i < DUMP; i++)
        {
            if (i > 0) sb.Append(i % 16 == 0 ? " | " : " ");
            sb.Append(raw[i].ToString("X2"));
        }
        return sb.ToString();
    }

    public override void Disposing()
    {
        _running = false;
        _hook = null;
        if (_buf != 0) { try { Marshal.FreeHGlobal(_buf); } catch { } _buf = 0; }
    }

    #region For Exports, Serialization etc.
#pragma warning disable CS8618
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}

internal static class CurrentProcessMemory
{
    public static bool TryRead(nint address, byte[] buffer, out string error)
    {
        error = "";
        if (buffer.Length == 0) return true;
        if (!ReadableMemoryRange.IsReadable(address, buffer.Length))
        {
            error = $"range not readable 0x{address:X}+0x{buffer.Length:X}";
            return false;
        }

        if (!ReadProcessMemory(GetCurrentProcess(), address, buffer, (nuint)buffer.Length, out nuint bytesRead) ||
            bytesRead != (nuint)buffer.Length)
        {
            error = $"ReadProcessMemory failed bytes={bytesRead}/{buffer.Length} win32={Marshal.GetLastWin32Error()}";
            return false;
        }

        return true;
    }

    public static bool TryWriteInt16(nint address, short value, out string error)
    {
        error = "";
        if (!ReadableMemoryRange.IsWritable(address, sizeof(short)))
        {
            error = $"range not writable 0x{address:X}+0x{sizeof(short):X}";
            return false;
        }

        byte[] bytes = BitConverter.GetBytes(value);
        if (!WriteProcessMemory(GetCurrentProcess(), address, bytes, (nuint)bytes.Length, out nuint bytesWritten) ||
            bytesWritten != (nuint)bytes.Length)
        {
            error = $"WriteProcessMemory failed bytes={bytesWritten}/{bytes.Length} win32={Marshal.GetLastWin32Error()}";
            return false;
        }

        return true;
    }

    public static bool TryWriteBytes(nint address, byte[] bytes, out string error)
    {
        error = "";
        if (bytes.Length == 0) return true;
        if (!ReadableMemoryRange.IsWritable(address, bytes.Length))
        {
            error = $"range not writable 0x{address:X}+0x{bytes.Length:X}";
            return false;
        }

        if (!WriteProcessMemory(GetCurrentProcess(), address, bytes, (nuint)bytes.Length, out nuint bytesWritten) ||
            bytesWritten != (nuint)bytes.Length)
        {
            error = $"WriteProcessMemory failed bytes={bytesWritten}/{bytes.Length} win32={Marshal.GetLastWin32Error()}";
            return false;
        }

        return true;
    }

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        nint hProcess,
        nint lpBaseAddress,
        [Out] byte[] lpBuffer,
        nuint nSize,
        out nuint lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        nint hProcess,
        nint lpBaseAddress,
        byte[] lpBuffer,
        nuint nSize,
        out nuint lpNumberOfBytesWritten);
}

internal sealed record UnitSnapshot(
    nint Ptr,
    int CharId,
    int Level,
    int Hp,
    int MaxHp,
    int Team,
    bool IsFoe,
    int Pa,
    int Ma,
    int Speed,
    int Move,
    int Jump,
    int Brave,
    int Faith,
    byte[] Raw,
    int Mp = 0,
    int MaxMp = 0,
    int Ct = 0)
{
    public string FactionLabel => IsFoe ? "foe " : "ally";
    public string StatLine => $"Lv{Level} HP{MaxHp} MP{MaxMp} PA{Pa} MA{Ma} Sp{Speed} CT{Ct} Mv{Move} Jp{Jump} Br{Brave} Fa{Faith}";

    public int ReadByte(int offset) => offset >= 0 && offset < Raw.Length ? Raw[offset] : -1;

    public int ReadUInt16(int offset)
    {
        if (offset < 0 || offset + 1 >= Raw.Length) return -1;
        return Raw[offset] | (Raw[offset + 1] << 8);
    }
}

internal sealed record DamageEvent(
    UnitSnapshot Target,
    int PreviousHp,
    int CurrentHp,
    int VanillaDamage,
    UnitSnapshot? Attacker = null,
    string AttackerSource = "none",
    ActionSignal? Action = null,
    long EventIndex = 0,
    long EventSeed = 0)
{
    public bool IsDamage => VanillaDamage > 0;

    public bool IsHealing => VanillaDamage < 0;

    public int VanillaHealing => Math.Max(0, -VanillaDamage);

    public int VanillaDamageAbs => Math.Abs(VanillaDamage);

    public int ObservedHpDelta => CurrentHp - PreviousHp;
}

internal sealed record MpEvent(
    UnitSnapshot Target,
    int PreviousMp,
    int CurrentMp,
    int VanillaMpChange,
    UnitSnapshot? Attacker = null,
    string AttackerSource = "none",
    ActionSignal? Action = null,
    long EventIndex = 0,
    long EventSeed = 0)
{
    public bool IsMpLoss => VanillaMpChange < 0;

    public bool IsMpGain => VanillaMpChange > 0;

    public int VanillaMpLoss => Math.Max(0, -VanillaMpChange);

    public int VanillaMpGain => Math.Max(0, VanillaMpChange);

    public int VanillaMpChangeAbs => Math.Abs(VanillaMpChange);

    public int ObservedMpDelta => CurrentMp - PreviousMp;
}

internal sealed record ActionSignal(string Name, string Source, Dictionary<string, int> Variables)
{
    public int Get(string name)
        => Variables.TryGetValue(FormulaExpression.NormalizeIdentifierPart(name), out int value) ? value : 0;
}

internal sealed record DamageResult(bool ShouldRewrite, int DesiredHp, int FinalDamage, string RuleName)
{
    public string Trace { get; init; } = "";

    public static DamageResult NoRewrite(int currentHp, string reason = "off") => new(false, currentHp, 0, reason);
}

internal sealed record MpResult(bool ShouldRewrite, int DesiredMp, int FinalMpChange, string RuleName)
{
    public string Trace { get; init; } = "";

    public static MpResult NoRewrite(int currentMp, string reason = "off") => new(false, currentMp, 0, reason);
}

internal sealed record RewriteApplicationDecision(bool ShouldWrite, bool ShouldLogDryRun, int TrackingValue);

internal static class RewriteApplication
{
    public static RewriteApplicationDecision Decide(bool dryRun, int currentValue, int desiredValue)
    {
        if (dryRun)
            return new RewriteApplicationDecision(false, true, currentValue);
        if (currentValue == desiredValue)
            return new RewriteApplicationDecision(false, false, currentValue);
        return new RewriteApplicationDecision(true, false, desiredValue);
    }
}

internal sealed class ValueRewriteEchoGuard
{
    private readonly Dictionary<nint, PendingRewrite> _pending = new();
    private readonly string _label;

    public ValueRewriteEchoGuard(string label)
    {
        _label = string.IsNullOrWhiteSpace(label) ? "value" : label;
    }

    public void Remember(nint unitPtr, int fromValue, int toValue, long nowTick)
    {
        if (fromValue == toValue)
            return;
        _pending[unitPtr] = new PendingRewrite(fromValue, toValue, nowTick);
    }

    public bool TrySuppress(nint unitPtr, int previousTrackedValue, int currentValue, long nowTick, int windowMs, out string reason)
    {
        reason = "";
        if (!_pending.TryGetValue(unitPtr, out var pending))
            return false;

        int boundedWindowMs = Math.Clamp(windowMs, 0, 10000);
        long elapsedMs = (nowTick - pending.Tick) * 1000 / Stopwatch.Frequency;
        if (elapsedMs > boundedWindowMs)
        {
            _pending.Remove(unitPtr);
            return false;
        }

        if (pending.FromValue == previousTrackedValue && pending.ToValue == currentValue)
        {
            _pending.Remove(unitPtr);
            reason = $"expected rewrite echo {_label} {previousTrackedValue}->{currentValue} ageMs={elapsedMs}";
            return true;
        }

        if (pending.ToValue == previousTrackedValue || pending.FromValue != previousTrackedValue)
            _pending.Remove(unitPtr);

        return false;
    }

    private sealed record PendingRewrite(int FromValue, int ToValue, long Tick);
}

internal sealed record DamageResponse(int RawPermille, int Permille, int RuleCount, bool Clamped, string RuleName)
{
    public static DamageResponse Neutral { get; } = new(1000, 1000, 0, false, "NoDamageResponse");
}

internal sealed class BattleFormulaEngine
{
    private readonly RuntimeSettings _settings;
    private readonly ItemCatalog _itemCatalog;

    public BattleFormulaEngine(RuntimeSettings settings, ItemCatalog itemCatalog)
    {
        _settings = settings;
        _itemCatalog = itemCatalog;
    }

    public DamageResult Evaluate(DamageEvent e)
    {
        if (e.IsDamage && !_settings.RewriteObservedDamage) return DamageResult.NoRewrite(e.CurrentHp);
        if (e.IsHealing && !_settings.RewriteObservedHealing) return DamageResult.NoRewrite(e.CurrentHp);
        if (!e.IsDamage && !e.IsHealing) return DamageResult.NoRewrite(e.CurrentHp);
        if (e.Target.IsFoe && !_settings.AffectFoes) return DamageResult.NoRewrite(e.CurrentHp);
        if (!e.Target.IsFoe && !_settings.AffectAllies) return DamageResult.NoRewrite(e.CurrentHp);

        var targetSlots = ReadEquipmentSlots(e.Target, _settings.EquipmentSlots);
        var attackerSlots = ReadEquipmentSlots(e.Attacker, _settings.AttackerEquipmentSlots);

        var actionResolution = ResolveActionSignal(e, targetSlots, attackerSlots);
        if (!actionResolution.Success)
            return DamageResult.NoRewrite(e.CurrentHp, actionResolution.RuleName);
        e = actionResolution.Event;

        var equipment = ResolveEquipmentDr(e, targetSlots, attackerSlots);
        if (!equipment.Success) return DamageResult.NoRewrite(e.CurrentHp, equipment.RuleName);

        int equipmentDr = equipment.Dr;
        string equipmentRule = equipment.RuleName;
        var response = ResolveDamageResponse(e, equipmentDr, targetSlots, attackerSlots);
        if (!response.Success) return DamageResult.NoRewrite(e.CurrentHp, response.RuleName);

        var formulaContext = BuildFormulaContext(e, equipmentDr, response.Value, targetSlots, attackerSlots);
        if (!PrepareFinalFormulaContext(formulaContext, out string preparationError))
            return DamageResult.NoRewrite(e.CurrentHp, preparationError);
        if (!ShouldRewriteByFormula(formulaContext, out bool rewriteAllowed, out string rewriteConditionError))
            return DamageResult.NoRewrite(e.CurrentHp, rewriteConditionError);
        if (!rewriteAllowed)
            return DamageResult.NoRewrite(e.CurrentHp, "RewriteConditionFormula=0");

        var resolved = ResolveFinalDamage(e, formulaContext);
        if (!resolved.Success) return DamageResult.NoRewrite(e.CurrentHp, resolved.RuleName);

        int finalDamage = resolved.FinalDamage;
        string ruleName = resolved.RuleName;
        if (e.IsDamage && _settings.ApplyEquipmentDr && equipmentDr > 0)
        {
            finalDamage = Math.Max(0, finalDamage - equipmentDr);
            ruleName = $"{ruleName}+{equipmentRule}";
        }
        if (e.IsDamage && _settings.ApplyDamageResponseRules && response.Value.RuleCount > 0)
        {
            int beforeResponse = finalDamage;
            finalDamage = MulDiv(finalDamage, response.Value.Permille, 1000);
            if (beforeResponse > 0 && _settings.DamageResponseChipFloor > 0)
                finalDamage = Math.Max(_settings.DamageResponseChipFloor, finalDamage);
            ruleName = $"{ruleName}+{response.Value.RuleName}";
        }
        finalDamage = e.IsHealing
            ? Math.Clamp(finalDamage, -9999, 0)
            : Math.Clamp(finalDamage, 0, 9999);
        int hpFloor = Math.Clamp(_settings.MinHpFloor, 0, e.Target.MaxHp);
        int desiredHp = Math.Clamp(e.PreviousHp - finalDamage, hpFloor, e.Target.MaxHp);
        formulaContext.Set("result.finalDamage", finalDamage);
        formulaContext.Set("result.desiredHp", desiredHp);
        formulaContext.Set("result.shouldRewrite", 1);
        string trace = _settings.LogResolvedRuntimeContext
            ? BuildRuntimeTrace(e, targetSlots, attackerSlots, equipmentDr, equipmentRule, response.Value, ruleName, finalDamage, formulaContext)
            : "";
        return new DamageResult(true, desiredHp, finalDamage, ruleName) { Trace = trace };
    }

    public MpResult EvaluateMp(MpEvent e)
    {
        if (e.IsMpLoss && !_settings.RewriteObservedMpLoss) return MpResult.NoRewrite(e.CurrentMp);
        if (e.IsMpGain && !_settings.RewriteObservedMpGain) return MpResult.NoRewrite(e.CurrentMp);
        if (!e.IsMpLoss && !e.IsMpGain) return MpResult.NoRewrite(e.CurrentMp);
        if (e.Target.IsFoe && !_settings.AffectFoes) return MpResult.NoRewrite(e.CurrentMp);
        if (!e.Target.IsFoe && !_settings.AffectAllies) return MpResult.NoRewrite(e.CurrentMp);

        var targetSlots = ReadEquipmentSlots(e.Target, _settings.EquipmentSlots);
        var attackerSlots = ReadEquipmentSlots(e.Attacker, _settings.AttackerEquipmentSlots);

        var actionResolution = ResolveMpActionSignal(e, targetSlots, attackerSlots);
        if (!actionResolution.Success)
            return MpResult.NoRewrite(e.CurrentMp, actionResolution.RuleName);
        e = actionResolution.Event;

        var formulaContext = BuildMpFormulaContext(e, targetSlots, attackerSlots);
        if (!PrepareFinalFormulaContext(formulaContext, out string preparationError))
            return MpResult.NoRewrite(e.CurrentMp, preparationError);
        if (!ShouldRewriteMpByFormula(formulaContext, out bool rewriteAllowed, out string rewriteConditionError))
            return MpResult.NoRewrite(e.CurrentMp, rewriteConditionError);
        if (!rewriteAllowed)
            return MpResult.NoRewrite(e.CurrentMp, "MpRewriteConditionFormula=0");

        var resolved = ResolveFinalMpChange(e, formulaContext);
        if (!resolved.Success) return MpResult.NoRewrite(e.CurrentMp, resolved.RuleName);

        int finalMpChange = e.IsMpLoss
            ? Math.Clamp(resolved.FinalMpChange, -9999, 0)
            : Math.Clamp(resolved.FinalMpChange, 0, 9999);
        int desiredMp = Math.Clamp(e.PreviousMp + finalMpChange, 0, e.Target.MaxMp);
        formulaContext.Set("result.finalMpChange", finalMpChange);
        formulaContext.Set("result.desiredMp", desiredMp);
        formulaContext.Set("result.shouldRewriteMp", 1);
        string trace = _settings.LogResolvedRuntimeContext
            ? BuildMpRuntimeTrace(e, targetSlots, attackerSlots, resolved.RuleName, finalMpChange, formulaContext)
            : "";
        return new MpResult(true, desiredMp, finalMpChange, resolved.RuleName) { Trace = trace };
    }

    private string BuildRuntimeTrace(
        DamageEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots,
        int equipmentDr,
        string equipmentRule,
        DamageResponse response,
        string finalRule,
        int finalDamage,
        FormulaContext context)
    {
        string eventKind = e.IsDamage ? "damage" : e.IsHealing ? "healing" : "other";
        string attacker = e.Attacker is null
            ? "none"
            : $"0x{e.Attacker.Ptr:X}:{CleanTraceValue(e.AttackerSource)}";

        return string.Join(" | ",
            $"event={eventKind}",
            $"attacker={attacker}",
            DescribeAction(e.Action),
            DescribeSlots("targetSlots", targetSlots),
            DescribeSlots("attackerSlots", attackerSlots),
            $"equipmentDr={equipmentDr}:{CleanTraceValue(equipmentRule)}",
            $"response=raw{response.RawPermille}/permille{response.Permille}/rules{response.RuleCount}/clamped{(response.Clamped ? 1 : 0)}:{CleanTraceValue(response.RuleName)}",
            DescribeTraceVariables(context),
            $"final={finalDamage}:{CleanTraceValue(finalRule)}");
    }

    private string BuildMpRuntimeTrace(
        MpEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots,
        string finalRule,
        int finalMpChange,
        FormulaContext context)
    {
        string eventKind = e.IsMpLoss ? "mpLoss" : e.IsMpGain ? "mpGain" : "mpOther";
        string attacker = e.Attacker is null
            ? "none"
            : $"0x{e.Attacker.Ptr:X}:{CleanTraceValue(e.AttackerSource)}";

        return string.Join(" | ",
            $"event={eventKind}",
            $"attacker={attacker}",
            DescribeAction(e.Action),
            DescribeSlots("targetSlots", targetSlots),
            DescribeSlots("attackerSlots", attackerSlots),
            DescribeTraceVariables(context),
            $"finalMpChange={finalMpChange}:{CleanTraceValue(finalRule)}");
    }

    private string DescribeTraceVariables(FormulaContext context)
    {
        if (_settings.FormulaTraceVariables.Count == 0)
            return "vars=none";

        var values = new List<string>();
        foreach (var variable in _settings.FormulaTraceVariables.Take(24))
        {
            string name = variable.NormalizedName;
            if (string.IsNullOrWhiteSpace(name))
            {
                values.Add("unnamed=ERR(empty-name)");
                continue;
            }

            if (!FormulaExpression.TryEvaluate(variable.Formula, context, out int value, out string error))
                values.Add($"{name}=ERR({CleanTraceValue(error)})");
            else
                values.Add($"{name}={value}");
        }

        return "vars=" + string.Join(",", values);
    }

    private static string DescribeAction(ActionSignal? action)
    {
        if (action is null) return "action=none";

        var variables = action.Variables
            .Where(kv => kv.Value != 0 &&
                         !kv.Key.Equals("signal", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaDamage", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaDamageAbs", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaHealing", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaMpChange", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaMpChangeAbs", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaMpLoss", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("vanillaMpGain", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("isDamage", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("isHealing", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("isMpLoss", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("isMpGain", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("isMpChange", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .Select(kv => $"{FormulaExpression.NormalizeIdentifierPart(kv.Key)}={kv.Value}");
        string joined = string.Join(",", variables);
        string suffix = string.IsNullOrWhiteSpace(joined) ? "" : $":vars={joined}";
        return $"action={CleanTraceValue(action.Name)}:source={CleanTraceValue(action.Source)}:signal={action.Get("signal")}{suffix}";
    }

    private static string DescribeSlots(string label, List<EquipmentSlotValue> slots)
    {
        if (slots.Count == 0) return $"{label}=none";
        return $"{label}=" + string.Join(",", slots.Select(DescribeSlot));
    }

    private static string DescribeSlot(EquipmentSlotValue slot)
    {
        string state = slot.Present ? "present" : slot.MatchCount > 1 ? "ambiguous" : "missing";
        string item = slot.Item is null
            ? slot.ItemId.ToString()
            : $"{slot.ItemId}:{CleanTraceValue(slot.Item.Name)}";
        string offset = slot.Offset >= 0 ? $"0x{slot.Offset:X}" : "?";
        string width = string.IsNullOrWhiteSpace(slot.Width) ? "?" : CleanTraceValue(slot.Width);
        return $"{slot.VariableName}({state},id={item},off={offset},width={width},matches={slot.MatchCount})";
    }

    private static string CleanTraceValue(string value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ').Trim();

    private (bool Success, int FinalDamage, string RuleName) ResolveFinalDamage(DamageEvent e, FormulaContext context)
    {
        foreach (var rule in _settings.DamageRules)
        {
            string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? "DamageRules" : rule.Name;
            if (!rule.TryMatches(e, context, out bool matches, out string matchError))
                return (false, 0, $"{ruleName}: ConditionFormula: {matchError}");
            if (!matches) continue;

            if (!rule.TryApply(e.VanillaDamage, context, out int ruleDamage, out string error))
                return (false, 0, $"{ruleName}: {error}");
            return (true, ruleDamage, ruleName);
        }

        if (!string.IsNullOrWhiteSpace(_settings.FinalDamageFormula))
        {
            if (!FormulaExpression.TryEvaluate(_settings.FinalDamageFormula, context, out int formulaDamage, out string error))
                return (false, 0, $"FinalDamageFormula: {error}");
            return (true, formulaDamage, "FinalDamageFormula");
        }

        if (e.IsDamage && _settings.FlatDamageReduction > 0)
            return (true, Math.Max(0, e.VanillaDamage - _settings.FlatDamageReduction), "FlatDamageReduction");

        if (e.IsHealing)
            return (true, -Math.Clamp(_settings.ProofFinalHealing, 0, 9999), "ProofFinalHealing");

        return (true, Math.Clamp(_settings.ProofFinalDamage, 0, 9999), "ProofFinalDamage");
    }

    private (bool Success, int FinalMpChange, string RuleName) ResolveFinalMpChange(MpEvent e, FormulaContext context)
    {
        foreach (var rule in _settings.MpRules)
        {
            string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? "MpRules" : rule.Name;
            if (!rule.TryMatches(e, context, out bool matches, out string matchError))
                return (false, 0, $"{ruleName}: ConditionFormula: {matchError}");
            if (!matches) continue;

            if (!rule.TryApply(e.VanillaMpChange, context, out int ruleMpChange, out string error))
                return (false, 0, $"{ruleName}: {error}");
            return (true, ruleMpChange, ruleName);
        }

        if (!string.IsNullOrWhiteSpace(_settings.FinalMpChangeFormula))
        {
            if (!FormulaExpression.TryEvaluate(_settings.FinalMpChangeFormula, context, out int formulaChange, out string error))
                return (false, 0, $"FinalMpChangeFormula: {error}");
            return (true, formulaChange, "FinalMpChangeFormula");
        }

        if (e.IsMpGain)
            return (true, Math.Clamp(_settings.ProofFinalMpGain, 0, 9999), "ProofFinalMpGain");

        return (true, -Math.Clamp(_settings.ProofFinalMpLoss, 0, 9999), "ProofFinalMpLoss");
    }

    private bool PrepareFinalFormulaContext(FormulaContext context, out string error)
    {
        if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out error))
            return false;
        if (!ApplyFormulaVariables(_settings.FormulaPreResponseVariables, context, "FormulaPreResponseVariables", out error))
            return false;
        if (!ApplyDerivedVariables(context, out error))
            return false;

        return true;
    }

    private bool ShouldRewriteByFormula(FormulaContext context, out bool rewriteAllowed, out string error)
        => ShouldRewriteByFormula(context, _settings.RewriteConditionFormula, "RewriteConditionFormula", out rewriteAllowed, out error);

    private bool ShouldRewriteMpByFormula(FormulaContext context, out bool rewriteAllowed, out string error)
        => ShouldRewriteByFormula(context, _settings.MpRewriteConditionFormula, "MpRewriteConditionFormula", out rewriteAllowed, out error);

    private static bool ShouldRewriteByFormula(
        FormulaContext context,
        string formula,
        string formulaName,
        out bool rewriteAllowed,
        out string error)
    {
        rewriteAllowed = true;
        error = "";

        if (string.IsNullOrWhiteSpace(formula))
            return true;

        if (!FormulaExpression.TryEvaluate(formula, context, out int value, out string formulaError))
        {
            rewriteAllowed = false;
            error = $"{formulaName}: {formulaError}";
            return false;
        }

        rewriteAllowed = value != 0;
        return true;
    }

    private bool ApplyDerivedVariables(FormulaContext context, out string error)
        => ApplyFormulaVariables(_settings.FormulaDerivedVariables, context, "FormulaDerivedVariables", out error);

    private static bool ApplyFormulaVariables(
        List<FormulaDerivedVariable> variables,
        FormulaContext context,
        string groupName,
        out string error)
    {
        error = "";

        foreach (var variable in variables)
        {
            string name = variable.NormalizedName;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = $"{groupName}: variable has an empty name";
                return false;
            }

            if (!FormulaExpression.TryEvaluate(variable.Formula, context, out int value, out string formulaError))
            {
                error = $"{groupName}: {name}: {formulaError}";
                return false;
            }

            context.Set(name, value);
            if (variable.SetConstAlias)
                context.Set($"const.{name}", value);
        }

        return true;
    }

    private FormulaContext BuildFormulaContext(
        DamageEvent e,
        int equipmentDr,
        DamageResponse response,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        var context = new FormulaContext(e.Target, e.Attacker, e.EventIndex, e.EventSeed);

        foreach (var kv in _settings.FormulaVariables)
        {
            context.Set(kv.Key, kv.Value);
            context.Set($"const.{kv.Key}", kv.Value);
        }
        foreach (var kv in _settings.FormulaTables)
            context.SetTable(kv.Key, kv.Value);
        foreach (var kv in _settings.FormulaMatrices)
            context.SetMatrix(kv.Key, kv.Value);
        foreach (var kv in _settings.FormulaMaps)
            context.SetMap(kv.Key, kv.Value);

        context.Set("vanillaDamage", e.VanillaDamage);
        context.Set("vanillaDamageAbs", e.VanillaDamageAbs);
        context.Set("vanillaHealing", e.VanillaHealing);
        context.Set("observedHpDelta", e.ObservedHpDelta);
        context.Set("observedHpLoss", Math.Max(0, -e.ObservedHpDelta));
        context.Set("observedHpGain", Math.Max(0, e.ObservedHpDelta));
        context.Set("previousHp", e.PreviousHp);
        context.Set("currentHp", e.CurrentHp);
        context.Set("vanillaMpChange", 0);
        context.Set("vanillaMpDelta", 0);
        context.Set("vanillaMpChangeAbs", 0);
        context.Set("vanillaMpLoss", 0);
        context.Set("vanillaMpGain", 0);
        context.Set("observedMpDelta", 0);
        context.Set("observedMpLoss", 0);
        context.Set("observedMpGain", 0);
        context.Set("previousMp", e.Target.Mp);
        context.Set("currentMp", e.Target.Mp);
        context.Set("equipmentDr", equipmentDr);
        AddDamageResponseVariables(context, response);
        context.Set("event.isDamage", e.IsDamage ? 1 : 0);
        context.Set("event.isHealing", e.IsHealing ? 1 : 0);
        context.Set("event.isHpLoss", e.IsDamage ? 1 : 0);
        context.Set("event.isHpGain", e.IsHealing ? 1 : 0);
        context.Set("event.isMpLoss", 0);
        context.Set("event.isMpGain", 0);
        context.Set("event.isMpChange", 0);
        context.Set("event.index", ClampToInt(e.EventIndex));
        context.Set("event.seed", ClampToInt(e.EventSeed));
        AddUnitVariables(context, "target", e.Target);
        AddUnitVariables(context, "t", e.Target);
        AddUnitVariables(context, "attacker", e.Attacker);
        AddUnitVariables(context, "a", e.Attacker);
        context.Set("attacker.inferred", e.Attacker is not null && !e.AttackerSource.Equals("none", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.inferred", e.Attacker is not null && !e.AttackerSource.Equals("none", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("attacker.sourceRecent", e.AttackerSource.Equals("recent-unit", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.sourceRecent", e.AttackerSource.Equals("recent-unit", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("attacker.sourceCt", IsCtAttackerSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourceCt", IsCtAttackerSource(e.AttackerSource) ? 1 : 0);
        context.Set("attacker.sourceCounter", e.AttackerSource.Equals("counter-inversion", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.sourceCounter", e.AttackerSource.Equals("counter-inversion", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        AddActionVariables(context, "action", e.Action, e);
        AddActionVariables(context, "act", e.Action, e);

        AddSlotVariables(context, "slot", targetSlots);       // backwards-compatible target alias
        AddSlotVariables(context, "targetSlot", targetSlots);
        AddSlotVariables(context, "tslot", targetSlots);
        AddSlotVariables(context, "attackerSlot", attackerSlots);
        AddSlotVariables(context, "aslot", attackerSlots);

        return context;
    }

    private FormulaContext BuildMpFormulaContext(
        MpEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        var context = new FormulaContext(e.Target, e.Attacker, e.EventIndex, e.EventSeed);

        foreach (var kv in _settings.FormulaVariables)
        {
            context.Set(kv.Key, kv.Value);
            context.Set($"const.{kv.Key}", kv.Value);
        }
        foreach (var kv in _settings.FormulaTables)
            context.SetTable(kv.Key, kv.Value);
        foreach (var kv in _settings.FormulaMatrices)
            context.SetMatrix(kv.Key, kv.Value);
        foreach (var kv in _settings.FormulaMaps)
            context.SetMap(kv.Key, kv.Value);

        context.Set("vanillaDamage", 0);
        context.Set("vanillaDamageAbs", 0);
        context.Set("vanillaHealing", 0);
        context.Set("observedHpDelta", 0);
        context.Set("observedHpLoss", 0);
        context.Set("observedHpGain", 0);
        context.Set("previousHp", e.Target.Hp);
        context.Set("currentHp", e.Target.Hp);
        context.Set("vanillaMpChange", e.VanillaMpChange);
        context.Set("vanillaMpDelta", e.VanillaMpChange);
        context.Set("vanillaMpChangeAbs", e.VanillaMpChangeAbs);
        context.Set("vanillaMpLoss", e.VanillaMpLoss);
        context.Set("vanillaMpGain", e.VanillaMpGain);
        context.Set("observedMpDelta", e.ObservedMpDelta);
        context.Set("observedMpLoss", Math.Max(0, -e.ObservedMpDelta));
        context.Set("observedMpGain", Math.Max(0, e.ObservedMpDelta));
        context.Set("previousMp", e.PreviousMp);
        context.Set("currentMp", e.CurrentMp);
        context.Set("equipmentDr", 0);
        AddDamageResponseVariables(context, DamageResponse.Neutral);
        context.Set("event.isDamage", 0);
        context.Set("event.isHealing", 0);
        context.Set("event.isHpLoss", 0);
        context.Set("event.isHpGain", 0);
        context.Set("event.isMpLoss", e.IsMpLoss ? 1 : 0);
        context.Set("event.isMpGain", e.IsMpGain ? 1 : 0);
        context.Set("event.isMpChange", e.IsMpLoss || e.IsMpGain ? 1 : 0);
        context.Set("event.index", ClampToInt(e.EventIndex));
        context.Set("event.seed", ClampToInt(e.EventSeed));
        AddUnitVariables(context, "target", e.Target);
        AddUnitVariables(context, "t", e.Target);
        AddUnitVariables(context, "attacker", e.Attacker);
        AddUnitVariables(context, "a", e.Attacker);
        context.Set("attacker.inferred", e.Attacker is not null && !e.AttackerSource.Equals("none", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.inferred", e.Attacker is not null && !e.AttackerSource.Equals("none", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("attacker.sourceRecent", e.AttackerSource.Equals("recent-unit", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.sourceRecent", e.AttackerSource.Equals("recent-unit", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("attacker.sourceCt", IsCtAttackerSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourceCt", IsCtAttackerSource(e.AttackerSource) ? 1 : 0);
        context.Set("attacker.sourceCounter", e.AttackerSource.Equals("counter-inversion", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        context.Set("a.sourceCounter", e.AttackerSource.Equals("counter-inversion", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        AddMpActionVariables(context, "action", e.Action, e);
        AddMpActionVariables(context, "act", e.Action, e);

        AddSlotVariables(context, "slot", targetSlots);
        AddSlotVariables(context, "targetSlot", targetSlots);
        AddSlotVariables(context, "tslot", targetSlots);
        AddSlotVariables(context, "attackerSlot", attackerSlots);
        AddSlotVariables(context, "aslot", attackerSlots);

        return context;
    }

    private (bool Success, DamageEvent Event, string RuleName) ResolveActionSignal(
        DamageEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        if (e.Action is not null) return (true, e, "ActionSignal");
        var context = BuildFormulaContext(e, 0, DamageResponse.Neutral, targetSlots, attackerSlots);
        if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out string preActionError))
            return (false, e, preActionError);

        for (int i = 0; i < _settings.ActionSignalRules.Count; i++)
        {
            var rule = _settings.ActionSignalRules[i];
            string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? $"ActionSignal{i + 1}" : rule.Name;
            if (!rule.TryMatches(e, context, out bool matches, out string matchError))
                return (false, e, $"{ruleName}: ConditionFormula: {matchError}");
            if (!matches) continue;

            if (!rule.TryToSignal(i, e, context, out var signal, out string signalError))
                return (false, e, $"{ruleName}: VariableFormulas: {signalError}");
            return (true, e with { Action = signal }, ruleName);
        }

        return (true, e, "NoActionSignal");
    }

    private (bool Success, MpEvent Event, string RuleName) ResolveMpActionSignal(
        MpEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        if (e.Action is not null) return (true, e, "ActionSignal");
        var context = BuildMpFormulaContext(e, targetSlots, attackerSlots);
        if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out string preActionError))
            return (false, e, preActionError);

        for (int i = 0; i < _settings.ActionSignalRules.Count; i++)
        {
            var rule = _settings.ActionSignalRules[i];
            string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? $"ActionSignal{i + 1}" : rule.Name;
            if (!rule.TryMatches(e, context, out bool matches, out string matchError))
                return (false, e, $"{ruleName}: ConditionFormula: {matchError}");
            if (!matches) continue;

            if (!rule.TryToSignal(i, e, context, out var signal, out string signalError))
                return (false, e, $"{ruleName}: VariableFormulas: {signalError}");
            return (true, e with { Action = signal }, ruleName);
        }

        return (true, e, "NoActionSignal");
    }

    private static bool IsCtAttackerSource(string source)
        => source.StartsWith("ct-", StringComparison.OrdinalIgnoreCase);

    private void AddActionVariables(FormulaContext context, string prefix, ActionSignal? action, DamageEvent e)
    {
        context.Set($"{prefix}.present", action is null ? 0 : 1);
        context.Set($"{prefix}.sourceVanillaDamage", action?.Source.Equals("vanilla-damage", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0);
        context.Set($"{prefix}.signal", action?.Get("signal") ?? 0);
        context.Set($"{prefix}.vanillaDamage", e.VanillaDamage);
        context.Set($"{prefix}.vanillaDamageAbs", e.VanillaDamageAbs);
        context.Set($"{prefix}.vanillaHealing", e.VanillaHealing);
        context.Set($"{prefix}.vanillaMpChange", 0);
        context.Set($"{prefix}.vanillaMpChangeAbs", 0);
        context.Set($"{prefix}.vanillaMpLoss", 0);
        context.Set($"{prefix}.vanillaMpGain", 0);
        context.Set($"{prefix}.isDamage", e.IsDamage ? 1 : 0);
        context.Set($"{prefix}.isHealing", e.IsHealing ? 1 : 0);
        context.Set($"{prefix}.isMpLoss", 0);
        context.Set($"{prefix}.isMpGain", 0);
        context.Set($"{prefix}.isMpChange", 0);
        context.Set($"{prefix}.sourceMpChange", 0);

        foreach (var rule in _settings.ActionSignalRules)
        {
            if (rule.Variables is not null)
            {
                foreach (var key in rule.Variables.Keys)
                    context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", 0);
            }
            if (rule.VariableFormulas is not null)
            {
                foreach (var key in rule.VariableFormulas.Keys)
                    context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", 0);
            }
        }

        if (action is null) return;

        foreach (var kv in action.Variables)
            context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(kv.Key)}", kv.Value);
    }

    private void AddMpActionVariables(FormulaContext context, string prefix, ActionSignal? action, MpEvent e)
    {
        context.Set($"{prefix}.present", action is null ? 0 : 1);
        context.Set($"{prefix}.sourceVanillaDamage", 0);
        context.Set($"{prefix}.sourceMpChange", action?.Source.Equals("mp-change", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0);
        context.Set($"{prefix}.signal", action?.Get("signal") ?? 0);
        context.Set($"{prefix}.vanillaDamage", 0);
        context.Set($"{prefix}.vanillaDamageAbs", 0);
        context.Set($"{prefix}.vanillaHealing", 0);
        context.Set($"{prefix}.vanillaMpChange", e.VanillaMpChange);
        context.Set($"{prefix}.vanillaMpChangeAbs", e.VanillaMpChangeAbs);
        context.Set($"{prefix}.vanillaMpLoss", e.VanillaMpLoss);
        context.Set($"{prefix}.vanillaMpGain", e.VanillaMpGain);
        context.Set($"{prefix}.isDamage", 0);
        context.Set($"{prefix}.isHealing", 0);
        context.Set($"{prefix}.isMpLoss", e.IsMpLoss ? 1 : 0);
        context.Set($"{prefix}.isMpGain", e.IsMpGain ? 1 : 0);
        context.Set($"{prefix}.isMpChange", e.IsMpLoss || e.IsMpGain ? 1 : 0);

        foreach (var rule in _settings.ActionSignalRules)
        {
            if (rule.Variables is not null)
            {
                foreach (var key in rule.Variables.Keys)
                    context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", 0);
            }
            if (rule.VariableFormulas is not null)
            {
                foreach (var key in rule.VariableFormulas.Keys)
                    context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", 0);
            }
        }

        if (action is null) return;

        foreach (var kv in action.Variables)
            context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(kv.Key)}", kv.Value);
    }

    private static void AddSlotVariables(FormulaContext context, string prefix, List<EquipmentSlotValue> slots)
    {
        foreach (var slot in slots)
        {
            string baseName = $"{prefix}.{slot.VariableName}";
            context.Set(baseName, slot.ItemId);
            context.Set($"{baseName}.present", slot.Present ? 1 : 0);
            context.Set($"{baseName}.itemId", slot.ItemId);
            context.Set($"{baseName}.offset", slot.Offset);
            context.Set($"{baseName}.scanMatches", slot.MatchCount);
            context.Set($"{baseName}.ambiguous", !slot.Present && slot.MatchCount > 1 ? 1 : 0);
            context.Set($"{baseName}.widthByte", slot.Width.Equals("Byte", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            context.Set($"{baseName}.widthWord", slot.Width.Equals("Word", StringComparison.OrdinalIgnoreCase) ||
                                         slot.Width.Equals("UInt16", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            if (slot.Item is not null)
                slot.Item.AddVariables(context, baseName);
            else
                ItemCatalogEntry.AddDefaultVariables(context, baseName, slot.ItemId);
        }
    }

    private static void AddDamageResponseVariables(FormulaContext context, DamageResponse response)
    {
        context.Set("damageResponsePermille", response.Permille);
        context.Set("damageResponse.rawPermille", response.RawPermille);
        context.Set("damageResponse.permille", response.Permille);
        context.Set("damageResponse.ruleCount", response.RuleCount);
        context.Set("damageResponse.clamped", response.Clamped ? 1 : 0);
        context.Set("responsePermille", response.Permille);
        context.Set("response.rawPermille", response.RawPermille);
        context.Set("response.permille", response.Permille);
        context.Set("typeResponsePermille", response.Permille);
        context.Set("typeResponse.rawPermille", response.RawPermille);
        context.Set("typeResponse.permille", response.Permille);
        context.Set("combinedResponsePermille", response.RawPermille);
        context.Set("combinedResponse.permille", response.RawPermille);
        context.Set("boundedResponsePermille", response.Permille);
        context.Set("boundedResponse.permille", response.Permille);
    }

    private static void AddUnitVariables(FormulaContext context, string prefix, UnitSnapshot? unit)
    {
        context.Set($"{prefix}.present", unit is null ? 0 : 1);
        context.Set($"{prefix}.charId", unit?.CharId ?? 0);
        context.Set($"{prefix}.level", unit?.Level ?? 0);
        context.Set($"{prefix}.hp", unit?.Hp ?? 0);
        context.Set($"{prefix}.maxHp", unit?.MaxHp ?? 0);
        context.Set($"{prefix}.mp", unit?.Mp ?? 0);
        context.Set($"{prefix}.maxMp", unit?.MaxMp ?? 0);
        context.Set($"{prefix}.team", unit?.Team ?? 0);
        context.Set($"{prefix}.isFoe", unit?.IsFoe == true ? 1 : 0);
        context.Set($"{prefix}.isAlly", unit is not null && !unit.IsFoe ? 1 : 0);
        context.Set($"{prefix}.pa", unit?.Pa ?? 0);
        context.Set($"{prefix}.ma", unit?.Ma ?? 0);
        context.Set($"{prefix}.speed", unit?.Speed ?? 0);
        context.Set($"{prefix}.ct", unit?.Ct ?? 0);
        context.Set($"{prefix}.move", unit?.Move ?? 0);
        context.Set($"{prefix}.jump", unit?.Jump ?? 0);
        context.Set($"{prefix}.brave", unit?.Brave ?? 0);
        context.Set($"{prefix}.faith", unit?.Faith ?? 0);
    }

    private List<EquipmentSlotValue> ReadEquipmentSlots(UnitSnapshot? unit, List<EquipmentSlotProbe> probes)
    {
        var slots = new List<EquipmentSlotValue>();
        foreach (var slot in probes)
        {
            string name = string.IsNullOrWhiteSpace(slot.Name) ? $"offset_{slot.Offset:X2}" : slot.Name;
            string variableName = FormulaExpression.NormalizeIdentifierPart(name);

            if (unit is null)
            {
                slots.Add(new EquipmentSlotValue(name, variableName, 0, null, false, -1, "", 0));
                continue;
            }

            var resolved = slot.Resolve(unit, _itemCatalog);
            if (!resolved.Present)
            {
                slots.Add(new EquipmentSlotValue(name, variableName, 0, null, false, resolved.Offset, resolved.Width, resolved.MatchCount));
                continue;
            }

            slots.Add(new EquipmentSlotValue(name, variableName, resolved.ItemId, resolved.Item, true, resolved.Offset, resolved.Width, resolved.MatchCount));
        }

        return slots;
    }

    private (bool Success, int Dr, string RuleName) ResolveEquipmentDr(
        DamageEvent e,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        if (!e.IsDamage || targetSlots.Count == 0 || _settings.EquipmentDrRules.Count == 0)
            return (true, 0, "NoEquipmentDR");

        int total = 0;
        var names = new List<string>();

        foreach (var slot in targetSlots)
        {
            if (!slot.Present) continue;

            foreach (var rule in _settings.EquipmentDrRules)
            {
                if (!rule.Matches(slot.Name, slot.ItemId, slot.Item, e.Action)) continue;

                var context = BuildFormulaContext(e, total, DamageResponse.Neutral, targetSlots, attackerSlots);
                context.Set("slotItemId", slot.ItemId);
                context.Set("slot.itemId", slot.ItemId);
                context.Set("item.id", slot.ItemId);
                if (slot.Item is not null)
                    slot.Item.AddVariables(context, "item");
                else
                    ItemCatalogEntry.AddDefaultVariables(context, "item", slot.ItemId);

                if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out string preActionError))
                    return (false, 0, $"EquipmentDR {rule.DisplayName(slot)}: {preActionError}");
                if (!ApplyFormulaVariables(_settings.FormulaPreResponseVariables, context, "FormulaPreResponseVariables", out string preResponseError))
                    return (false, 0, $"EquipmentDR {rule.DisplayName(slot)}: {preResponseError}");

                if (!rule.TryMatchesCondition(context, out bool conditionMatches, out string conditionError))
                    return (false, 0, $"EquipmentDR {rule.DisplayName(slot)}: ConditionFormula: {conditionError}");
                if (!conditionMatches) continue;

                if (!rule.TryGetDamageReduction(context, out int damageReduction, out string error))
                {
                    return (false, 0, $"EquipmentDR {rule.DisplayName(slot)}: {error}");
                }

                total += Math.Max(0, damageReduction);
                names.Add(string.IsNullOrWhiteSpace(rule.Name) ? $"{slot.Name}:{slot.ItemId}" : rule.Name);
            }
        }

        return total == 0
            ? (true, 0, "NoEquipmentDR")
            : (true, total, "EquipmentDR(" + string.Join("+", names.Take(4)) + ")");
    }

    private (bool Success, DamageResponse Value, string RuleName) ResolveDamageResponse(
        DamageEvent e,
        int equipmentDr,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        if (!e.IsDamage || _settings.DamageResponseRules.Count == 0)
            return (true, DamageResponse.Neutral, DamageResponse.Neutral.RuleName);

        int rawPermille = 1000;
        int ruleCount = 0;
        var names = new List<string>();

        foreach (var rule in _settings.DamageResponseRules)
        {
            if (!rule.MatchesAction(e.Action)) continue;

            if (rule.UsesSlotMatch)
            {
                foreach (var slot in targetSlots)
                {
                    if (!slot.Present) continue;
                    if (!rule.MatchesSlot(slot.Name, slot.ItemId, slot.Item)) continue;

                    var current = BuildIntermediateResponse(rawPermille, ruleCount, names);
                    var context = BuildFormulaContext(e, equipmentDr, current, targetSlots, attackerSlots);
                    AddRuleItemVariables(context, slot);

                    if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out string preActionError))
                        return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(slot)}: {preActionError}");
                    if (!ApplyFormulaVariables(_settings.FormulaPreResponseVariables, context, "FormulaPreResponseVariables", out string preResponseError))
                        return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(slot)}: {preResponseError}");

                    if (!rule.TryMatchesCondition(context, out bool conditionMatches, out string conditionError))
                        return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(slot)}: ConditionFormula: {conditionError}");
                    if (!conditionMatches) continue;

                    if (!rule.TryGetMultiplierPermille(context, out int multiplierPermille, out string multiplierError))
                        return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(slot)}: {multiplierError}");

                    rawPermille = MulDiv(rawPermille, multiplierPermille, 1000);
                    ruleCount++;
                    names.Add(string.IsNullOrWhiteSpace(rule.Name) ? $"{slot.Name}:{slot.ItemId}" : rule.Name);
                }

                continue;
            }

            {
                var current = BuildIntermediateResponse(rawPermille, ruleCount, names);
                var context = BuildFormulaContext(e, equipmentDr, current, targetSlots, attackerSlots);
                AddDefaultRuleItemVariables(context);

                if (!ApplyFormulaVariables(_settings.FormulaPreActionVariables, context, "FormulaPreActionVariables", out string preActionError))
                    return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(null)}: {preActionError}");
                if (!ApplyFormulaVariables(_settings.FormulaPreResponseVariables, context, "FormulaPreResponseVariables", out string preResponseError))
                    return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(null)}: {preResponseError}");

                if (!rule.TryMatchesCondition(context, out bool conditionMatches, out string conditionError))
                    return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(null)}: ConditionFormula: {conditionError}");
                if (!conditionMatches) continue;

                if (!rule.TryGetMultiplierPermille(context, out int multiplierPermille, out string multiplierError))
                    return (false, DamageResponse.Neutral, $"DamageResponse {rule.DisplayName(null)}: {multiplierError}");

                rawPermille = MulDiv(rawPermille, multiplierPermille, 1000);
                ruleCount++;
                names.Add(string.IsNullOrWhiteSpace(rule.Name) ? "global" : rule.Name);
            }
        }

        if (ruleCount == 0)
            return (true, DamageResponse.Neutral, DamageResponse.Neutral.RuleName);

        int min = Math.Max(0, _settings.MinDamageResponsePermille);
        int max = Math.Max(min, _settings.MaxDamageResponsePermille);
        int bounded = Math.Clamp(rawPermille, min, max);
        var response = new DamageResponse(
            rawPermille,
            bounded,
            ruleCount,
            bounded != rawPermille,
            "DamageResponse(" + string.Join("+", names.Take(4)) + ")");
        return (true, response, response.RuleName);
    }

    private DamageResponse BuildIntermediateResponse(int rawPermille, int ruleCount, List<string> names)
    {
        int min = Math.Max(0, _settings.MinDamageResponsePermille);
        int max = Math.Max(min, _settings.MaxDamageResponsePermille);
        int bounded = Math.Clamp(rawPermille, min, max);
        return new DamageResponse(
            rawPermille,
            bounded,
            ruleCount,
            bounded != rawPermille,
            ruleCount == 0 ? DamageResponse.Neutral.RuleName : "DamageResponse(" + string.Join("+", names.Take(4)) + ")");
    }

    private static void AddRuleItemVariables(FormulaContext context, EquipmentSlotValue slot)
    {
        context.Set("slotItemId", slot.ItemId);
        context.Set("slot.itemId", slot.ItemId);
        context.Set("item.id", slot.ItemId);
        if (slot.Item is not null)
            slot.Item.AddVariables(context, "item");
        else
            ItemCatalogEntry.AddDefaultVariables(context, "item", slot.ItemId);
    }

    private static void AddDefaultRuleItemVariables(FormulaContext context)
    {
        context.Set("slotItemId", 0);
        context.Set("slot.itemId", 0);
        context.Set("item.id", 0);
        ItemCatalogEntry.AddDefaultVariables(context, "item");
    }

    internal sealed record EquipmentSlotValue(
        string Name,
        string VariableName,
        int ItemId,
        ItemCatalogEntry? Item,
        bool Present,
        int Offset,
        string Width,
        int MatchCount);

    private static int MulDiv(int value, int numerator, int denominator)
    {
        if (denominator == 0) return 0;
        long result = (long)value * numerator / denominator;
        return ClampToInt(result);
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }
}

internal sealed class DamageRule
{
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "Any"; // Any, Ally, Foe
    public string EventKind { get; set; } = "Any"; // Any, Damage/HpLoss, Healing/HpGain
    public int? Team { get; set; }
    public int? CharId { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public int? ActionSignal { get; set; }
    public string RequiredActionVariable { get; set; } = "";
    public string ConditionFormula { get; set; } = "";
    public string FinalDamageFormula { get; set; } = "";
    public int? FinalDamage { get; set; }
    public int? FlatDamageReduction { get; set; }
    public int ScaleNumerator { get; set; } = 1;
    public int ScaleDenominator { get; set; } = 1;
    public int? MinFinalDamage { get; set; }
    public int? MaxFinalDamage { get; set; }

    public bool TryMatches(DamageEvent e, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        var target = e.Target;
        var action = e.Action;

        if (!FactionMatches(target)) return true;
        if (Team.HasValue && target.Team != Team.Value) return true;
        if (CharId.HasValue && target.CharId != CharId.Value) return true;
        if (MinLevel.HasValue && target.Level < MinLevel.Value) return true;
        if (MaxLevel.HasValue && target.Level > MaxLevel.Value) return true;
        if (!EventKindMatches(e)) return true;
        if (ActionSignal.HasValue && (action?.Get("signal") ?? 0) != ActionSignal.Value) return true;
        if (!string.IsNullOrWhiteSpace(RequiredActionVariable) &&
            (action?.Get(RequiredActionVariable) ?? 0) == 0) return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;

            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }

    public bool TryApply(int vanillaDamage, FormulaContext context, out int finalDamage, out string error)
    {
        finalDamage = 0;
        error = "";

        int value;
        if (!string.IsNullOrWhiteSpace(FinalDamageFormula))
        {
            if (!FormulaExpression.TryEvaluate(FinalDamageFormula, context, out value, out error))
                return false;
        }
        else
        {
            value = FinalDamage ?? vanillaDamage;
        }

        if (FlatDamageReduction.HasValue)
            value = Math.Max(0, value - FlatDamageReduction.Value);

        if (ScaleNumerator != 1 || ScaleDenominator != 1)
        {
            int denominator = Math.Max(1, ScaleDenominator);
            value = value * ScaleNumerator / denominator;
        }

        if (MinFinalDamage.HasValue) value = Math.Max(MinFinalDamage.Value, value);
        if (MaxFinalDamage.HasValue) value = Math.Min(MaxFinalDamage.Value, value);
        finalDamage = value;
        return true;
    }

    private bool FactionMatches(UnitSnapshot target)
    {
        if (Faction.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
        if (Faction.Equals("Foe", StringComparison.OrdinalIgnoreCase)) return target.IsFoe;
        if (Faction.Equals("Ally", StringComparison.OrdinalIgnoreCase)) return !target.IsFoe;
        return true;
    }

    private bool EventKindMatches(DamageEvent e)
    {
        if (string.IsNullOrWhiteSpace(EventKind) ||
            EventKind.Equals("Any", StringComparison.OrdinalIgnoreCase))
            return true;
        if (EventKind.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpChange", StringComparison.OrdinalIgnoreCase))
            return e.IsDamage || e.IsHealing;
        if (EventKind.Equals("Damage", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpLoss", StringComparison.OrdinalIgnoreCase))
            return e.IsDamage;
        if (EventKind.Equals("Healing", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("Heal", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpGain", StringComparison.OrdinalIgnoreCase))
            return e.IsHealing;
        return false;
    }
}

internal sealed class MpRule
{
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "Any"; // Any, Ally, Foe
    public string EventKind { get; set; } = "Any"; // Any, Loss, Gain
    public int? Team { get; set; }
    public int? CharId { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public int? ActionSignal { get; set; }
    public string RequiredActionVariable { get; set; } = "";
    public string ConditionFormula { get; set; } = "";
    public string FinalMpChangeFormula { get; set; } = "";
    public int? FinalMpChange { get; set; }
    public int ScaleNumerator { get; set; } = 1;
    public int ScaleDenominator { get; set; } = 1;
    public int? MinFinalMpChange { get; set; }
    public int? MaxFinalMpChange { get; set; }

    public bool TryMatches(MpEvent e, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        var target = e.Target;

        if (!FactionMatches(target)) return true;
        if (!EventKindMatches(e)) return true;
        if (Team.HasValue && target.Team != Team.Value) return true;
        if (CharId.HasValue && target.CharId != CharId.Value) return true;
        if (MinLevel.HasValue && target.Level < MinLevel.Value) return true;
        if (MaxLevel.HasValue && target.Level > MaxLevel.Value) return true;
        if (ActionSignal.HasValue && (e.Action?.Get("signal") ?? 0) != ActionSignal.Value) return true;
        if (!string.IsNullOrWhiteSpace(RequiredActionVariable) &&
            (e.Action?.Get(RequiredActionVariable) ?? 0) == 0) return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;

            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }

    public bool TryApply(int vanillaMpChange, FormulaContext context, out int finalMpChange, out string error)
    {
        finalMpChange = 0;
        error = "";

        int value;
        if (!string.IsNullOrWhiteSpace(FinalMpChangeFormula))
        {
            if (!FormulaExpression.TryEvaluate(FinalMpChangeFormula, context, out value, out error))
                return false;
        }
        else
        {
            value = FinalMpChange ?? vanillaMpChange;
        }

        if (ScaleNumerator != 1 || ScaleDenominator != 1)
        {
            int denominator = Math.Max(1, ScaleDenominator);
            value = value * ScaleNumerator / denominator;
        }

        if (MinFinalMpChange.HasValue) value = Math.Max(MinFinalMpChange.Value, value);
        if (MaxFinalMpChange.HasValue) value = Math.Min(MaxFinalMpChange.Value, value);
        finalMpChange = value;
        return true;
    }

    private bool FactionMatches(UnitSnapshot target)
    {
        if (Faction.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
        if (Faction.Equals("Foe", StringComparison.OrdinalIgnoreCase)) return target.IsFoe;
        if (Faction.Equals("Ally", StringComparison.OrdinalIgnoreCase)) return !target.IsFoe;
        return true;
    }

    private bool EventKindMatches(MpEvent e)
    {
        if (string.IsNullOrWhiteSpace(EventKind) ||
            EventKind.Equals("Any", StringComparison.OrdinalIgnoreCase))
            return true;
        if (EventKind.Equals("Loss", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("MpLoss", StringComparison.OrdinalIgnoreCase))
            return e.IsMpLoss;
        if (EventKind.Equals("Gain", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("MpGain", StringComparison.OrdinalIgnoreCase))
            return e.IsMpGain;
        return false;
    }
}

internal sealed class ActionSignalRule
{
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "Any"; // Any, Ally, Foe
    public string EventKind { get; set; } = "Any"; // Any, HP/Damage/Healing, MP/MpLoss/MpGain
    public int? Team { get; set; }
    public int? CharId { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public int? VanillaDamage { get; set; }
    public int? MinVanillaDamage { get; set; }
    public int? MaxVanillaDamage { get; set; }
    public int? VanillaMpChange { get; set; }
    public int? MinVanillaMpChange { get; set; }
    public int? MaxVanillaMpChange { get; set; }
    public int Signal { get; set; } = 0;
    public Dictionary<string, int> Variables { get; set; } = new();
    public string ConditionFormula { get; set; } = "";
    public Dictionary<string, string> VariableFormulas { get; set; } = new();

    public bool TryMatches(DamageEvent e, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        var target = e.Target;
        if (!FactionMatches(target)) return true;
        if (Team.HasValue && target.Team != Team.Value) return true;
        if (CharId.HasValue && target.CharId != CharId.Value) return true;
        if (MinLevel.HasValue && target.Level < MinLevel.Value) return true;
        if (MaxLevel.HasValue && target.Level > MaxLevel.Value) return true;
        if (!EventKindMatches(e)) return true;
        if (HasMpChangeFilter) return true;
        if (VanillaDamage.HasValue && e.VanillaDamage != VanillaDamage.Value) return true;
        if (MinVanillaDamage.HasValue && e.VanillaDamage < MinVanillaDamage.Value) return true;
        if (MaxVanillaDamage.HasValue && e.VanillaDamage > MaxVanillaDamage.Value) return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;

            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }

    public bool TryMatches(MpEvent e, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        var target = e.Target;
        if (!FactionMatches(target)) return true;
        if (Team.HasValue && target.Team != Team.Value) return true;
        if (CharId.HasValue && target.CharId != CharId.Value) return true;
        if (MinLevel.HasValue && target.Level < MinLevel.Value) return true;
        if (MaxLevel.HasValue && target.Level > MaxLevel.Value) return true;
        if (!EventKindMatches(e)) return true;
        if (HasDamageFilter) return true;
        if (VanillaMpChange.HasValue && e.VanillaMpChange != VanillaMpChange.Value) return true;
        if (MinVanillaMpChange.HasValue && e.VanillaMpChange < MinVanillaMpChange.Value) return true;
        if (MaxVanillaMpChange.HasValue && e.VanillaMpChange > MaxVanillaMpChange.Value) return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;

            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }

    public bool TryToSignal(int ruleIndex, DamageEvent e, FormulaContext context, out ActionSignal signal, out string error)
    {
        error = "";
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["signal"] = Signal != 0 ? Signal : ruleIndex + 1,
            ["vanillaDamage"] = e.VanillaDamage,
            ["vanillaDamageAbs"] = e.VanillaDamageAbs,
            ["vanillaHealing"] = e.VanillaHealing,
            ["vanillaMpChange"] = 0,
            ["vanillaMpChangeAbs"] = 0,
            ["vanillaMpLoss"] = 0,
            ["vanillaMpGain"] = 0,
            ["isDamage"] = e.IsDamage ? 1 : 0,
            ["isHealing"] = e.IsHealing ? 1 : 0,
            ["isMpLoss"] = 0,
            ["isMpGain"] = 0,
            ["isMpChange"] = 0,
        };

        foreach (var kv in Variables ?? [])
            variables[FormulaExpression.NormalizeIdentifierPart(kv.Key)] = kv.Value;

        foreach (var kv in VariableFormulas ?? [])
        {
            string variableName = FormulaExpression.NormalizeIdentifierPart(kv.Key);
            if (!FormulaExpression.TryEvaluate(kv.Value, context, out int value, out string formulaError))
            {
                signal = new ActionSignal("", "", new Dictionary<string, int>());
                error = $"{variableName}: {formulaError}";
                return false;
            }

            variables[variableName] = value;
            context.Set($"action.{variableName}", value);
            context.Set($"act.{variableName}", value);
        }

        string name = string.IsNullOrWhiteSpace(Name) ? $"ActionSignal{ruleIndex + 1}" : Name;
        signal = new ActionSignal(name, "vanilla-damage", variables);
        return true;
    }

    public bool TryToSignal(int ruleIndex, MpEvent e, FormulaContext context, out ActionSignal signal, out string error)
    {
        error = "";
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["signal"] = Signal != 0 ? Signal : ruleIndex + 1,
            ["vanillaDamage"] = 0,
            ["vanillaDamageAbs"] = 0,
            ["vanillaHealing"] = 0,
            ["vanillaMpChange"] = e.VanillaMpChange,
            ["vanillaMpChangeAbs"] = e.VanillaMpChangeAbs,
            ["vanillaMpLoss"] = e.VanillaMpLoss,
            ["vanillaMpGain"] = e.VanillaMpGain,
            ["isDamage"] = 0,
            ["isHealing"] = 0,
            ["isMpLoss"] = e.IsMpLoss ? 1 : 0,
            ["isMpGain"] = e.IsMpGain ? 1 : 0,
            ["isMpChange"] = e.IsMpLoss || e.IsMpGain ? 1 : 0,
        };

        foreach (var kv in Variables ?? [])
            variables[FormulaExpression.NormalizeIdentifierPart(kv.Key)] = kv.Value;

        foreach (var kv in VariableFormulas ?? [])
        {
            string variableName = FormulaExpression.NormalizeIdentifierPart(kv.Key);
            if (!FormulaExpression.TryEvaluate(kv.Value, context, out int value, out string formulaError))
            {
                signal = new ActionSignal("", "", new Dictionary<string, int>());
                error = $"{variableName}: {formulaError}";
                return false;
            }

            variables[variableName] = value;
            context.Set($"action.{variableName}", value);
            context.Set($"act.{variableName}", value);
        }

        string name = string.IsNullOrWhiteSpace(Name) ? $"ActionSignal{ruleIndex + 1}" : Name;
        signal = new ActionSignal(name, "mp-change", variables);
        return true;
    }

    private bool HasDamageFilter =>
        VanillaDamage.HasValue ||
        MinVanillaDamage.HasValue ||
        MaxVanillaDamage.HasValue;

    private bool HasMpChangeFilter =>
        VanillaMpChange.HasValue ||
        MinVanillaMpChange.HasValue ||
        MaxVanillaMpChange.HasValue;

    private bool FactionMatches(UnitSnapshot target)
    {
        if (Faction.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
        if (Faction.Equals("Foe", StringComparison.OrdinalIgnoreCase)) return target.IsFoe;
        if (Faction.Equals("Ally", StringComparison.OrdinalIgnoreCase)) return !target.IsFoe;
        return true;
    }

    private bool EventKindMatches(DamageEvent e)
    {
        if (string.IsNullOrWhiteSpace(EventKind) ||
            EventKind.Equals("Any", StringComparison.OrdinalIgnoreCase))
            return true;
        if (EventKind.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpChange", StringComparison.OrdinalIgnoreCase))
            return e.IsDamage || e.IsHealing;
        if (EventKind.Equals("Damage", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpLoss", StringComparison.OrdinalIgnoreCase))
            return e.IsDamage;
        if (EventKind.Equals("Healing", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("Heal", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("HpGain", StringComparison.OrdinalIgnoreCase))
            return e.IsHealing;
        return false;
    }

    private bool EventKindMatches(MpEvent e)
    {
        if (string.IsNullOrWhiteSpace(EventKind) ||
            EventKind.Equals("Any", StringComparison.OrdinalIgnoreCase))
            return true;
        if (EventKind.Equals("MP", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("MpChange", StringComparison.OrdinalIgnoreCase))
            return e.IsMpLoss || e.IsMpGain;
        if (EventKind.Equals("Loss", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("MpLoss", StringComparison.OrdinalIgnoreCase))
            return e.IsMpLoss;
        if (EventKind.Equals("Gain", StringComparison.OrdinalIgnoreCase) ||
            EventKind.Equals("MpGain", StringComparison.OrdinalIgnoreCase))
            return e.IsMpGain;
        return false;
    }
}

internal sealed class EquipmentSlotProbe
{
    public string Name { get; set; } = "";
    public int Offset { get; set; } = -1;
    public string Width { get; set; } = "Byte"; // Byte, UInt16
    public int SearchStart { get; set; } = -1;
    public int SearchEnd { get; set; } = -1;
    public string SearchWidth { get; set; } = "Any"; // Any, Byte, UInt16
    public bool AllowAmbiguousSearchMatch { get; set; } = false;
    public bool AllowItemIdZero { get; set; } = false;
    public int ItemId { get; set; } = -1;
    public int? MinItemId { get; set; }
    public int? MaxItemId { get; set; }
    public string ItemCategory { get; set; } = "";
    public string TypeFlag { get; set; } = "";
    public string SecondaryKind { get; set; } = "";
    public string NameContains { get; set; } = "";
    public int? MinArmorHpBonus { get; set; }
    public int? MaxArmorHpBonus { get; set; }
    public int? MinWeaponPower { get; set; }
    public int? MaxWeaponPower { get; set; }

    public ResolvedEquipmentSlot Resolve(UnitSnapshot target, ItemCatalog catalog)
    {
        if (Offset >= 0)
        {
            int itemId = ReadItemId(target);
            if (itemId < 0) return ResolvedEquipmentSlot.Missing(-1, Width, 0);
            catalog.TryGet(itemId, out var exactItem);
            return new ResolvedEquipmentSlot(true, itemId, exactItem, Offset, Width, 1);
        }

        return Search(target, catalog);
    }

    private int ReadItemId(UnitSnapshot target)
    {
        if (Offset < 0) return -1;
        if (Width.Equals("UInt16", StringComparison.OrdinalIgnoreCase) ||
            Width.Equals("Word", StringComparison.OrdinalIgnoreCase))
            return target.ReadUInt16(Offset);
        return target.ReadByte(Offset);
    }

    private ResolvedEquipmentSlot Search(UnitSnapshot target, ItemCatalog catalog)
    {
        if (!HasSearchFilter())
            return ResolvedEquipmentSlot.Missing(-1, SearchWidth, 0);

        int start = SearchStart >= 0 ? SearchStart : 0x44;
        int end = SearchEnd >= 0 ? SearchEnd : target.Raw.Length - 1;
        start = Math.Clamp(start, 0, Math.Max(0, target.Raw.Length - 1));
        end = Math.Clamp(end, start, Math.Max(0, target.Raw.Length - 1));

        var matches = new List<ResolvedEquipmentSlot>();
        if (!SearchWidth.Equals("UInt16", StringComparison.OrdinalIgnoreCase) &&
            !SearchWidth.Equals("Word", StringComparison.OrdinalIgnoreCase))
            ScanByte(target, catalog, start, end, matches);

        if (!SearchWidth.Equals("Byte", StringComparison.OrdinalIgnoreCase))
            ScanWord(target, catalog, start, end, matches);

        if (matches.Count == 0)
            return ResolvedEquipmentSlot.Missing(-1, SearchWidth, 0);
        if (matches.Count > 1 && !AllowAmbiguousSearchMatch)
            return ResolvedEquipmentSlot.Missing(matches[0].Offset, matches[0].Width, matches.Count);

        var first = matches[0];
        return first with { MatchCount = matches.Count };
    }

    private void ScanByte(UnitSnapshot target, ItemCatalog catalog, int start, int end, List<ResolvedEquipmentSlot> matches)
    {
        for (int offset = start; offset <= end; offset++)
        {
            int itemId = target.ReadByte(offset);
            TryAddMatch(catalog, itemId, offset, "Byte", matches);
        }
    }

    private void ScanWord(UnitSnapshot target, ItemCatalog catalog, int start, int end, List<ResolvedEquipmentSlot> matches)
    {
        for (int offset = start; offset < end; offset++)
        {
            int itemId = target.ReadUInt16(offset);
            TryAddMatch(catalog, itemId, offset, "Word", matches);
        }
    }

    private void TryAddMatch(ItemCatalog catalog, int itemId, int offset, string width, List<ResolvedEquipmentSlot> matches)
    {
        if (itemId < 0) return;
        if (itemId == 0 && !AllowItemIdZero) return;
        if (!catalog.TryGet(itemId, out var item)) return;
        if (!MatchesItem(itemId, item)) return;
        matches.Add(new ResolvedEquipmentSlot(true, itemId, item, offset, width, 1));
    }

    private bool HasSearchFilter()
        => ItemId >= 0 ||
           MinItemId.HasValue ||
           MaxItemId.HasValue ||
           !string.IsNullOrWhiteSpace(ItemCategory) ||
           !string.IsNullOrWhiteSpace(TypeFlag) ||
           !string.IsNullOrWhiteSpace(SecondaryKind) ||
           !string.IsNullOrWhiteSpace(NameContains) ||
           MinArmorHpBonus.HasValue ||
           MaxArmorHpBonus.HasValue ||
           MinWeaponPower.HasValue ||
           MaxWeaponPower.HasValue;

    private bool MatchesItem(int itemId, ItemCatalogEntry item)
    {
        if (ItemId >= 0 && itemId != ItemId) return false;
        if (MinItemId.HasValue && itemId < MinItemId.Value) return false;
        if (MaxItemId.HasValue && itemId > MaxItemId.Value) return false;
        if (!string.IsNullOrWhiteSpace(ItemCategory) &&
            !item.ItemCategory.Equals(ItemCategory, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(TypeFlag) && !item.HasTypeFlag(TypeFlag)) return false;
        if (!string.IsNullOrWhiteSpace(SecondaryKind) &&
            !item.SecondaryKind.Equals(SecondaryKind, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(NameContains) &&
            !item.Name.Contains(NameContains, StringComparison.OrdinalIgnoreCase)) return false;
        if (MinArmorHpBonus.HasValue && item.ArmorHpBonus < MinArmorHpBonus.Value) return false;
        if (MaxArmorHpBonus.HasValue && item.ArmorHpBonus > MaxArmorHpBonus.Value) return false;
        if (MinWeaponPower.HasValue && item.WeaponPower < MinWeaponPower.Value) return false;
        if (MaxWeaponPower.HasValue && item.WeaponPower > MaxWeaponPower.Value) return false;
        return true;
    }

    internal sealed record ResolvedEquipmentSlot(
        bool Present,
        int ItemId,
        ItemCatalogEntry? Item,
        int Offset,
        string Width,
        int MatchCount)
    {
        public static ResolvedEquipmentSlot Missing(int offset, string width, int matchCount)
            => new(false, 0, null, offset, width, matchCount);
    }
}

internal sealed class DamageResponseRule
{
    public string Name { get; set; } = "";
    public string Slot { get; set; } = "";
    public int ItemId { get; set; } = -1;
    public int? MinItemId { get; set; }
    public int? MaxItemId { get; set; }
    public string ItemCategory { get; set; } = "";
    public string TypeFlag { get; set; } = "";
    public string SecondaryKind { get; set; } = "";
    public string NameContains { get; set; } = "";
    public int? MinArmorHpBonus { get; set; }
    public int? MaxArmorHpBonus { get; set; }
    public int? MinWeaponPower { get; set; }
    public int? MaxWeaponPower { get; set; }
    public int? ActionSignal { get; set; }
    public string RequiredActionVariable { get; set; } = "";
    public string ConditionFormula { get; set; } = "";
    public string MultiplierFormula { get; set; } = "";
    public int? MultiplierPermille { get; set; }
    public int MultiplierNumerator { get; set; } = 1;
    public int MultiplierDenominator { get; set; } = 1;

    public bool UsesSlotMatch =>
        !string.IsNullOrWhiteSpace(Slot) ||
        ItemId >= 0 ||
        MinItemId.HasValue ||
        MaxItemId.HasValue ||
        !string.IsNullOrWhiteSpace(ItemCategory) ||
        !string.IsNullOrWhiteSpace(TypeFlag) ||
        !string.IsNullOrWhiteSpace(SecondaryKind) ||
        !string.IsNullOrWhiteSpace(NameContains) ||
        MinArmorHpBonus.HasValue ||
        MaxArmorHpBonus.HasValue ||
        MinWeaponPower.HasValue ||
        MaxWeaponPower.HasValue;

    public bool MatchesAction(ActionSignal? action)
    {
        if (ActionSignal.HasValue && (action?.Get("signal") ?? 0) != ActionSignal.Value) return false;
        if (!string.IsNullOrWhiteSpace(RequiredActionVariable) &&
            (action?.Get(RequiredActionVariable) ?? 0) == 0) return false;
        return true;
    }

    public bool MatchesSlot(string slotName, int itemId, ItemCatalogEntry? item)
    {
        bool slotMatches = string.IsNullOrWhiteSpace(Slot) ||
                           Slot.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
                           Slot.Equals(slotName, StringComparison.OrdinalIgnoreCase);
        if (!slotMatches) return false;
        if (ItemId >= 0 && itemId != ItemId) return false;
        if (MinItemId.HasValue && itemId < MinItemId.Value) return false;
        if (MaxItemId.HasValue && itemId > MaxItemId.Value) return false;

        bool needsCatalog = !string.IsNullOrWhiteSpace(ItemCategory) ||
                            !string.IsNullOrWhiteSpace(TypeFlag) ||
                            !string.IsNullOrWhiteSpace(SecondaryKind) ||
                            !string.IsNullOrWhiteSpace(NameContains) ||
                            MinArmorHpBonus.HasValue ||
                            MaxArmorHpBonus.HasValue ||
                            MinWeaponPower.HasValue ||
                            MaxWeaponPower.HasValue;
        if (needsCatalog && item is null) return false;

        if (item is not null)
        {
            if (!string.IsNullOrWhiteSpace(ItemCategory) &&
                !item.ItemCategory.Equals(ItemCategory, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(TypeFlag) && !item.HasTypeFlag(TypeFlag)) return false;
            if (!string.IsNullOrWhiteSpace(SecondaryKind) &&
                !item.SecondaryKind.Equals(SecondaryKind, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(NameContains) &&
                !item.Name.Contains(NameContains, StringComparison.OrdinalIgnoreCase)) return false;
            if (MinArmorHpBonus.HasValue && item.ArmorHpBonus < MinArmorHpBonus.Value) return false;
            if (MaxArmorHpBonus.HasValue && item.ArmorHpBonus > MaxArmorHpBonus.Value) return false;
            if (MinWeaponPower.HasValue && item.WeaponPower < MinWeaponPower.Value) return false;
            if (MaxWeaponPower.HasValue && item.WeaponPower > MaxWeaponPower.Value) return false;
        }

        return true;
    }

    public bool TryMatchesCondition(FormulaContext context, out bool matches, out string error)
    {
        matches = true;
        error = "";

        if (string.IsNullOrWhiteSpace(ConditionFormula))
            return true;

        if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
        {
            matches = false;
            return false;
        }

        matches = value != 0;
        return true;
    }

    public bool TryGetMultiplierPermille(FormulaContext context, out int multiplierPermille, out string error)
    {
        multiplierPermille = 1000;
        error = "";

        if (!string.IsNullOrWhiteSpace(MultiplierFormula))
        {
            if (!FormulaExpression.TryEvaluate(MultiplierFormula, context, out multiplierPermille, out error))
                return false;
        }
        else if (MultiplierPermille.HasValue)
        {
            multiplierPermille = MultiplierPermille.Value;
        }
        else
        {
            if (MultiplierDenominator <= 0)
            {
                error = "MultiplierDenominator must be greater than zero";
                return false;
            }

            multiplierPermille = ClampToInt((long)MultiplierNumerator * 1000 / MultiplierDenominator);
        }

        if (multiplierPermille < 0)
        {
            error = "response multiplier must be non-negative";
            return false;
        }

        return true;
    }

    internal string DisplayName(BattleFormulaEngine.EquipmentSlotValue? slot)
    {
        if (!string.IsNullOrWhiteSpace(Name)) return Name;
        return slot is null ? "global" : $"{slot.Name}:{slot.ItemId}";
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }
}

internal sealed class EquipmentDrRule
{
    public string Name { get; set; } = "";
    public string Slot { get; set; } = "Any";
    public int ItemId { get; set; } = -1;
    public int? MinItemId { get; set; }
    public int? MaxItemId { get; set; }
    public string ItemCategory { get; set; } = "";
    public string TypeFlag { get; set; } = "";
    public string SecondaryKind { get; set; } = "";
    public string NameContains { get; set; } = "";
    public int? MinArmorHpBonus { get; set; }
    public int? MaxArmorHpBonus { get; set; }
    public int? MinWeaponPower { get; set; }
    public int? MaxWeaponPower { get; set; }
    public int? ActionSignal { get; set; }
    public string RequiredActionVariable { get; set; } = "";
    public string ConditionFormula { get; set; } = "";
    public string DamageReductionFormula { get; set; } = "";
    public int DamageReduction { get; set; } = 0;

    public bool Matches(string slotName, int itemId, ItemCatalogEntry? item, ActionSignal? action)
    {
        bool slotMatches = Slot.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
                           Slot.Equals(slotName, StringComparison.OrdinalIgnoreCase);
        if (!slotMatches) return false;
        if (ItemId >= 0 && itemId != ItemId) return false;
        if (MinItemId.HasValue && itemId < MinItemId.Value) return false;
        if (MaxItemId.HasValue && itemId > MaxItemId.Value) return false;
        if (ActionSignal.HasValue && (action?.Get("signal") ?? 0) != ActionSignal.Value) return false;
        if (!string.IsNullOrWhiteSpace(RequiredActionVariable) &&
            (action?.Get(RequiredActionVariable) ?? 0) == 0) return false;

        bool needsCatalog = !string.IsNullOrWhiteSpace(ItemCategory) ||
                            !string.IsNullOrWhiteSpace(TypeFlag) ||
                            !string.IsNullOrWhiteSpace(SecondaryKind) ||
                            !string.IsNullOrWhiteSpace(NameContains) ||
                            MinArmorHpBonus.HasValue ||
                            MaxArmorHpBonus.HasValue ||
                            MinWeaponPower.HasValue ||
                            MaxWeaponPower.HasValue;
        if (needsCatalog && item is null) return false;

        if (item is not null)
        {
            if (!string.IsNullOrWhiteSpace(ItemCategory) &&
                !item.ItemCategory.Equals(ItemCategory, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(TypeFlag) && !item.HasTypeFlag(TypeFlag)) return false;
            if (!string.IsNullOrWhiteSpace(SecondaryKind) &&
                !item.SecondaryKind.Equals(SecondaryKind, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(NameContains) &&
                !item.Name.Contains(NameContains, StringComparison.OrdinalIgnoreCase)) return false;
            if (MinArmorHpBonus.HasValue && item.ArmorHpBonus < MinArmorHpBonus.Value) return false;
            if (MaxArmorHpBonus.HasValue && item.ArmorHpBonus > MaxArmorHpBonus.Value) return false;
            if (MinWeaponPower.HasValue && item.WeaponPower < MinWeaponPower.Value) return false;
            if (MaxWeaponPower.HasValue && item.WeaponPower > MaxWeaponPower.Value) return false;
        }

        return true;
    }

    public bool TryMatchesCondition(FormulaContext context, out bool matches, out string error)
    {
        matches = true;
        error = "";

        if (string.IsNullOrWhiteSpace(ConditionFormula))
            return true;

        if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
        {
            matches = false;
            return false;
        }

        matches = value != 0;
        return true;
    }

    public bool TryGetDamageReduction(FormulaContext context, out int damageReduction, out string error)
    {
        damageReduction = 0;
        error = "";

        if (!string.IsNullOrWhiteSpace(DamageReductionFormula))
            return FormulaExpression.TryEvaluate(DamageReductionFormula, context, out damageReduction, out error);

        damageReduction = DamageReduction;
        return true;
    }

    internal string DisplayName(BattleFormulaEngine.EquipmentSlotValue slot)
        => string.IsNullOrWhiteSpace(Name) ? $"{slot.Name}:{slot.ItemId}" : Name;
}

internal sealed class FormulaDerivedVariable
{
    public string Name { get; set; } = "";
    public string Formula { get; set; } = "";
    public bool SetConstAlias { get; set; } = false;

    public string NormalizedName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "";
            var parts = Name
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(FormulaExpression.NormalizeIdentifierPart)
                .Where(part => !string.IsNullOrWhiteSpace(part));
            return string.Join(".", parts);
        }
    }
}

// One write applied to a unit's struct when our formula kills it (HP forced to 0), used to set the
// death/status state the engine would normally set itself. Read-modify-write so a single status BIT
// can be set without clobbering the rest of the field. Configured in JSON once the offset is mapped.
internal sealed class DeathStateWrite
{
    private const int CopiedRawSize = 0x180;

    public string Name { get; set; } = "";
    public int Offset { get; set; } = -1;
    public string Width { get; set; } = "Byte";   // Byte | Word | DWord
    public int? Value { get; set; }                // overwrite the field with this value
    public int? OrMask { get; set; }               // field |= OrMask  (set status bit(s))
    public int? AndMask { get; set; }              // field &= AndMask (clear bit(s))

    internal bool TryValidate(out int width, out long mask, out string error)
    {
        width = 0;
        mask = 0;
        error = "";

        if (Offset < 0) { error = "negative offset"; return false; }
        if (!TryGetWidthBytes(out width, out error)) return false;
        if (Offset + width > CopiedRawSize)
        {
            error = $"Offset 0x{Offset:X}+{width} exceeds copied unit snapshot size 0x{CopiedRawSize:X}.";
            return false;
        }
        if (Value is null && OrMask is null && AndMask is null) { error = "no Value/OrMask/AndMask"; return false; }

        mask = FieldMask(width);
        if (!FitsWidth(Value, nameof(Value), mask, out error)) return false;
        if (!FitsWidth(OrMask, nameof(OrMask), mask, out error)) return false;
        if (!FitsWidth(AndMask, nameof(AndMask), mask, out error)) return false;
        return true;
    }

    internal bool TryGetWidthBytes(out int width, out string error)
    {
        error = "";
        width = (Width ?? "Byte").Trim().ToLowerInvariant() switch
        {
            "byte" or "uint8" or "int8" or "sbyte" => 1,
            "word" or "short" or "uint16" or "int16" => 2,
            "dword" or "int" or "uint32" or "int32" => 4,
            _ => 0,
        };

        if (width != 0) return true;
        error = $"unsupported Width '{Width}'. Use Byte, Word, or DWord.";
        return false;
    }

    private static long FieldMask(int width)
        => width == 4 ? 0xFFFF_FFFFL : (1L << (8 * width)) - 1;

    private static bool FitsWidth(int? value, string field, long mask, out string error)
    {
        error = "";
        if (!value.HasValue) return true;
        if (value.Value < 0)
        {
            error = $"{field} must be nonnegative.";
            return false;
        }
        if (value.Value <= mask) return true;
        error = $"{field}=0x{value.Value:X} exceeds field mask 0x{mask:X}.";
        return false;
    }

    public bool TryApply(nint basePtr, out string desc, out string error)
    {
        desc = "";
        error = "";
        if (!TryValidate(out int width, out long mask, out error)) return false;

        nint addr = basePtr + Offset;
        var cur = new byte[width];
        if (!CurrentProcessMemory.TryRead(addr, cur, out error)) return false;

        long current = 0;
        for (int i = 0; i < width; i++) current |= (long)cur[i] << (8 * i);
        long next = Value ?? current;
        if (OrMask.HasValue) next |= (long)OrMask.Value;
        if (AndMask.HasValue) next &= (long)AndMask.Value;
        next &= mask;

        var bytes = new byte[width];
        for (int i = 0; i < width; i++) bytes[i] = (byte)((next >> (8 * i)) & 0xFF);
        if (!CurrentProcessMemory.TryWriteBytes(addr, bytes, out error)) return false;

        desc = $"{(string.IsNullOrWhiteSpace(Name) ? "death" : Name)} +0x{Offset:X2} w{width} {current & mask:X}->{next & mask:X}";
        return true;
    }
}

internal sealed class RuntimeSettings
{
    public bool RewriteObservedDamage { get; set; } = false;
    public bool RewriteObservedHealing { get; set; } = false;
    public bool RewriteObservedMpLoss { get; set; } = false;
    public bool RewriteObservedMpGain { get; set; } = false;
    public bool DryRunRewrites { get; set; } = false;
    public int ProofFinalDamage { get; set; } = 1;
    public int ProofFinalHealing { get; set; } = 1;
    public int ProofFinalMpLoss { get; set; } = 1;
    public int ProofFinalMpGain { get; set; } = 1;
    public int FlatDamageReduction { get; set; } = 0;
    public string RewriteConditionFormula { get; set; } = "";
    public string FinalDamageFormula { get; set; } = "";
    public string MpRewriteConditionFormula { get; set; } = "";
    public string FinalMpChangeFormula { get; set; } = "";
    public Dictionary<string, int> FormulaVariables { get; set; } = new();
    public List<FormulaDerivedVariable> FormulaPreActionVariables { get; set; } = new();
    public List<FormulaDerivedVariable> FormulaPreResponseVariables { get; set; } = new();
    public List<FormulaDerivedVariable> FormulaDerivedVariables { get; set; } = new();
    public List<FormulaDerivedVariable> FormulaTraceVariables { get; set; } = new();
    public Dictionary<string, List<int>> FormulaTables { get; set; } = new();
    public Dictionary<string, List<List<int>>> FormulaMatrices { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> FormulaMaps { get; set; } = new();
    public string ItemCatalogPath { get; set; } = "item_catalog.csv";
    public bool AffectAllies { get; set; } = true;
    public bool AffectFoes { get; set; } = true;
    public List<ActionSignalRule> ActionSignalRules { get; set; } = new();
    public List<DamageRule> DamageRules { get; set; } = new();
    public List<MpRule> MpRules { get; set; } = new();
    public bool ApplyEquipmentDr { get; set; } = false;
    public List<EquipmentSlotProbe> EquipmentSlots { get; set; } = new();
    public List<EquipmentSlotProbe> AttackerEquipmentSlots { get; set; } = new();
    public List<EquipmentDrRule> EquipmentDrRules { get; set; } = new();
    public bool ApplyDamageResponseRules { get; set; } = false;
    public int MinDamageResponsePermille { get; set; } = 0;
    public int MaxDamageResponsePermille { get; set; } = 10000;
    public int DamageResponseChipFloor { get; set; } = 0;
    public List<DamageResponseRule> DamageResponseRules { get; set; } = new();
    public bool LogUnknownFieldDiffs { get; set; } = true;
    public int UnknownDiffStart { get; set; } = 0x44;
    public int UnknownDiffEnd { get; set; } = 0x17F;
    public int MaxUnknownDiffsPerUnit { get; set; } = 160;
    public bool InferAttackerFromRecentUnits { get; set; } = false;
    public bool LogAttackerCandidates { get; set; } = true;
    public int RecentAttackerWindowMs { get; set; } = 1500;
    // Attacker resolution by CT (+0x41): the attacker is the registered unit whose CT just reset
    // (the unit that just acted). Live-proven; primary path. CtDropWindowMs bounds how recently the
    // CT drop must have been observed for it to count.
    public bool ResolveAttackerByCt { get; set; } = true;
    public int CtDropWindowMs { get; set; } = 4000;
    // Polling can miss the exact CT drop if the first observed post-action frame already has low CT.
    // This fallback only considers alive, non-target units that were hook-touched recently and still
    // have a near-reset CT value.
    public bool ResolveAttackerByLowCtFallback { get; set; } = false;
    public int CtLowFallbackMaxCt { get; set; } = 20;
    public int CtLowFallbackWindowMs { get; set; } = 1500;
    public bool LogCtResolutionDiagnostics { get; set; } = false;
    // Counterattacks do not reset CT. When a target immediately damages the unit that just attacked it,
    // invert the previous resolved HP-damage pair and treat the previous target as the counter attacker.
    public bool ResolveCounterFromRecentDamage { get; set; } = true;
    public int CounterEventWindowMs { get; set; } = 1500;
    public bool PreferOpposingTeamAttacker { get; set; } = true;
    public int MaxAttackerCandidatesToLog { get; set; } = 4;
    public bool LogResolvedRuntimeContext { get; set; } = false;
    public int UnitPollIntervalMs { get; set; } = 25;
    public int MaxTrackedBattleUnits { get; set; } = 64;
    public int SuppressOwnRewriteEchoWindowMs { get; set; } = 1000;
    public List<MemoryTableProbe> MemoryTableProbes { get; set; } = new();

    // Death-state RE capture: when a unit's HP transitions to 0, dump its struct and diff alive->dead
    // (plus a short follow-up window) to find the death/status flag offset, which is not yet mapped.
    public bool CaptureStructOnDeath { get; set; } = false;
    public int DeathCaptureFollowTicks { get; set; } = 40; // ~1s at 25ms: catch a delayed flag set

    // Hook register probe: opt-in RE aid for the stable battle_base_ptr touchpoint. It logs the x64
    // register state at the hook and classifies values that look like known battle-unit pointers.
    public bool HookRegisterProbe { get; set; } = false;
    public int HookRegisterProbeMaxLogs { get; set; } = 32;

    // Actor probe: on each HP damage event, snapshot a small byte window of EVERY registered unit so we can
    // find which unit just acted (the attacker) by its turn-state/CT signature. Window is JSON-tunable (no
    // rebuild) so we can widen/narrow the search. Goal: map reliable attacker resolution + a pre-damage signal.
    public bool ActorProbeOnEvent { get; set; } = false;
    public int ActorProbeStart { get; set; } = 0x40;
    public int ActorProbeEnd { get; set; } = 0x44;

    // Death-causing write: once vanilla damage is neutered, the engine no longer kills, so when OUR
    // formula zeroes a unit's HP we must set the death state ourselves. Off until the offset is mapped;
    // then list DeathStateWrites in JSON (no rebuild needed) and set CauseDeathOnZeroHp=true.
    public bool CauseDeathOnZeroHp { get; set; } = false;
    public List<DeathStateWrite> DeathStateWrites { get; set; } = new();

    // Engine-owned death ("leave-at-1"): never write HP below this floor, so OUR writes can't create a
    // zombie at 0 (writing 0 does not trigger the engine death routine; the death state lives outside the
    // unit struct). With MinHpFloor=1, a "lethal" formula result leaves the target at 1 HP and the engine's
    // own (neutered) damage delivers the real kill on the next hit, which we then leave alone. 0 = disabled.
    public int MinHpFloor { get; set; } = 0;

    public static RuntimeSettings Load(string path)
        => TryLoad(path, out var settings, out _) ? settings : new RuntimeSettings();

    public static bool TryLoad(string path, out RuntimeSettings settings, out string error)
    {
        settings = new RuntimeSettings();
        error = "";

        try
        {
            if (!File.Exists(path)) return true;
            settings = JsonSerializer.Deserialize<RuntimeSettings>(File.ReadAllText(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            }) ?? new RuntimeSettings();
            settings.Normalize();
            return true;
        }
        catch (Exception ex)
        {
            settings = new RuntimeSettings();
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private void Normalize()
    {
        ActionSignalRules ??= new List<ActionSignalRule>();
        foreach (var rule in ActionSignalRules)
        {
            if (rule is not null)
            {
                rule.Variables ??= new Dictionary<string, int>();
                rule.VariableFormulas ??= new Dictionary<string, string>();
            }
        }
        DamageRules ??= new List<DamageRule>();
        MpRules ??= new List<MpRule>();
        FormulaVariables ??= new Dictionary<string, int>();
        FormulaPreActionVariables ??= new List<FormulaDerivedVariable>();
        FormulaPreResponseVariables ??= new List<FormulaDerivedVariable>();
        FormulaDerivedVariables ??= new List<FormulaDerivedVariable>();
        FormulaTraceVariables ??= new List<FormulaDerivedVariable>();
        FormulaTables ??= new Dictionary<string, List<int>>();
        FormulaMatrices ??= new Dictionary<string, List<List<int>>>();
        FormulaMaps ??= new Dictionary<string, Dictionary<string, int>>();
        EquipmentSlots ??= new List<EquipmentSlotProbe>();
        AttackerEquipmentSlots ??= new List<EquipmentSlotProbe>();
        EquipmentDrRules ??= new List<EquipmentDrRule>();
        DamageResponseRules ??= new List<DamageResponseRule>();
        MemoryTableProbes ??= new List<MemoryTableProbe>();
        foreach (var probe in MemoryTableProbes)
            probe.Normalize();
        DeathStateWrites ??= new List<DeathStateWrite>();
    }

    public string Describe()
        => $"RewriteObservedDamage={RewriteObservedDamage}, RewriteObservedHealing={RewriteObservedHealing}, RewriteObservedMpLoss={RewriteObservedMpLoss}, RewriteObservedMpGain={RewriteObservedMpGain}, DryRunRewrites={DryRunRewrites}, ProofFinalDamage={ProofFinalDamage}, ProofFinalHealing={ProofFinalHealing}, ProofFinalMpLoss={ProofFinalMpLoss}, ProofFinalMpGain={ProofFinalMpGain}, FlatDamageReduction={FlatDamageReduction}, RewriteConditionFormula={(string.IsNullOrWhiteSpace(RewriteConditionFormula) ? "off" : "on")}, FinalDamageFormula={(string.IsNullOrWhiteSpace(FinalDamageFormula) ? "off" : "on")}, MpRewriteConditionFormula={(string.IsNullOrWhiteSpace(MpRewriteConditionFormula) ? "off" : "on")}, FinalMpChangeFormula={(string.IsNullOrWhiteSpace(FinalMpChangeFormula) ? "off" : "on")}, FormulaVariables={FormulaVariables.Count}, FormulaPreActionVariables={FormulaPreActionVariables.Count}, FormulaPreResponseVariables={FormulaPreResponseVariables.Count}, FormulaDerivedVariables={FormulaDerivedVariables.Count}, FormulaTraceVariables={FormulaTraceVariables.Count}, FormulaTables={FormulaTables.Count}, FormulaMatrices={FormulaMatrices.Count}, FormulaMaps={FormulaMaps.Count}, ItemCatalogPath={ItemCatalogPath}, ActionSignalRules={ActionSignalRules.Count}, DamageRules={DamageRules.Count}, MpRules={MpRules.Count}, ApplyEquipmentDr={ApplyEquipmentDr}, EquipmentSlots={EquipmentSlots.Count}, AttackerEquipmentSlots={AttackerEquipmentSlots.Count}, EquipmentDrRules={EquipmentDrRules.Count}, ApplyDamageResponseRules={ApplyDamageResponseRules}, DamageResponseRules={DamageResponseRules.Count}, DamageResponseClamp={MinDamageResponsePermille}-{MaxDamageResponsePermille}, AffectAllies={AffectAllies}, AffectFoes={AffectFoes}, InferAttackerFromRecentUnits={InferAttackerFromRecentUnits}, RecentAttackerWindowMs={RecentAttackerWindowMs}, ResolveAttackerByCt={ResolveAttackerByCt}, CtDropWindowMs={CtDropWindowMs}, ResolveAttackerByLowCtFallback={ResolveAttackerByLowCtFallback}, CtLowFallbackMaxCt={CtLowFallbackMaxCt}, CtLowFallbackWindowMs={CtLowFallbackWindowMs}, LogCtResolutionDiagnostics={LogCtResolutionDiagnostics}, ResolveCounterFromRecentDamage={ResolveCounterFromRecentDamage}, CounterEventWindowMs={CounterEventWindowMs}, LogAttackerCandidates={LogAttackerCandidates}, LogResolvedRuntimeContext={LogResolvedRuntimeContext}, UnitPollIntervalMs={UnitPollIntervalMs}, MaxTrackedBattleUnits={MaxTrackedBattleUnits}, SuppressOwnRewriteEchoWindowMs={SuppressOwnRewriteEchoWindowMs}, MemoryTableProbes={MemoryTableProbes.Count}/{MemoryTableProbes.Count(probe => probe.Enabled)} enabled, LogUnknownFieldDiffs={LogUnknownFieldDiffs}, UnknownDiff=0x{UnknownDiffStart:X2}-0x{UnknownDiffEnd:X2}, CaptureStructOnDeath={CaptureStructOnDeath}, CauseDeathOnZeroHp={CauseDeathOnZeroHp}, DeathStateWrites={DeathStateWrites.Count}, MinHpFloor={MinHpFloor}, HookRegisterProbe={HookRegisterProbe}/{HookRegisterProbeMaxLogs}, ActorProbeOnEvent={ActorProbeOnEvent}, ActorProbe=0x{ActorProbeStart:X2}-0x{ActorProbeEnd:X2}";
}
