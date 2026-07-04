using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
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
/// read-only; an opt-in settings file can enable controlled HP rewrite proofs. Output:
/// battleprobe_log.txt next to the game exe.
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
    private AbilityCatalog _abilityCatalog = AbilityCatalog.Empty("");
    private BattleFormulaEngine _formulaEngine = null!;
    private BattleContextResolver _contextResolver = null!;
    private readonly PendingActionTracker _pendingActionTracker = new();
    private readonly DclActionContextCache _dclActionCache = new();
    private DateTime _settingsLastWriteUtc = DateTime.MinValue;
    private string _catalogPath = "";
    private DateTime _catalogLastWriteUtc = DateTime.MinValue;
    private string _abilityCatalogPath = "";
    private DateTime _abilityCatalogLastWriteUtc = DateTime.MinValue;
    private long _lastRuntimeReloadCheckTick;

    private const string BattleBasePtr = "0F B7 41 30 66 89 42 0C"; // movzx eax,[rcx+0x30]; rcx=unit (stable)
    private const int DUMP = 0x200;       // copy 0x00..0x1FF (stats + suspected gear/action/death region)
    private const int BattleUnitStride = 0x200;
    private const int BattleUnitTableRva = 0x1853CE0;
    private const int PlanExpectedAny = -1;
    private const int PlanExpectedPositiveDebit = -2;
    private const int B_PTR = DUMP;       // native pointer-sized: rcx unit pointer
    private const int B_COUNT = DUMP + 8;
    private const int B_REGS = B_COUNT + 8;
    private const int REGISTER_COUNT = 16;
    private const int B_SIZE = B_REGS + (REGISTER_COUNT * 8);
    private const int LANDMARK_RING_SIZE = 128;
    private const int LANDMARK_RING_MASK = LANDMARK_RING_SIZE - 1;
    private const int LANDMARK_SLOT_SIZE = 0x90;
    private const int L_COUNT = 0;
    private const int L_EVENTS = 0x10;
    private const int L_SEQ = 0;
    private const int L_ID = 4;
    private const int L_REGS = 8;
    private const int LANDMARK_BUFFER_SIZE = L_EVENTS + (LANDMARK_RING_SIZE * LANDMARK_SLOT_SIZE);
    // Result/animation selector probe (OBSERVE-ONLY). Captures the evade-type byte (cl) the caller
    // passes to the result selector, plus a window of the actor's result RECORD at [actor+unitOffset].
    // Ring of fixed-size slots; the hot path never calls managed code (all formatting in the poller).
    private const int SELECTOR_RING_SIZE = 32;
    private const int SELECTOR_RING_MASK = SELECTOR_RING_SIZE - 1;
    private const int SELECTOR_RECORD_DUMP_MAX = 256;        // ResultSelectorProbeRecordDumpBytes upper bound
    private const int SELECTOR_RECORD_DUMP_BASE = 0x1B8;     // window starts here (covers +0x1BB..+0x1E5)
    private const int S_COUNT = 0;
    private const int S_CTRL_WRITES = 4;    // dword (header): total control writes performed this session (persistent)
    private const int S_EVENTS = 0x10;
    private const int S_SEQ = 0;            // dword: sequence (written last so a partial slot is detectable)
    private const int S_EVADE = 4;          // dword: evade-type from cl (zero-extended)
    private const int S_ACTOR = 8;          // native ptr: r8 (actor object)
    private const int S_RECORD = 16;        // native ptr: [r8+unitOffset] (result record), 0 if guarded
    private const int S_RECORD_DUMP = 24;   // record window [record+SELECTOR_RECORD_DUMP_BASE ..]
    private const int S_CTRL_INFO = S_RECORD_DUMP + SELECTOR_RECORD_DUMP_MAX; // dword: [action(0/1/2), forcedEvade, forcedResult, 0]
    private const int S_CONTEXT_REGS = S_CTRL_INFO + 8; // qwords: original rax..r15 at selector entry
    private const int SELECTOR_CONTEXT_REG_COUNT = 16;
    private const int S_CONTEXT_STACK = S_CONTEXT_REGS + (SELECTOR_CONTEXT_REG_COUNT * 8); // qwords from original rsp
    private const int SELECTOR_CONTEXT_STACK_SLOTS = 24;
    private const int SELECTOR_SLOT_SIZE = S_CONTEXT_STACK + (SELECTOR_CONTEXT_STACK_SLOTS * 8);
    private const int SELECTOR_BUFFER_SIZE = S_EVENTS + (SELECTOR_RING_SIZE * SELECTOR_SLOT_SIZE);

    // Evade-INPUT probe/control buffer (hook at RVA 0x30F49C; rbx = target unit just before the VM roll).
    private const int EVADE_INPUT_RING_SIZE = 32;
    private const int EVADE_INPUT_RING_MASK = EVADE_INPUT_RING_SIZE - 1;
    private const int EI_COUNT = 0;          // dword header: total events
    private const int EI_CTRL_WRITES = 4;    // dword header: live control writes (persistent)
    private const int EI_EVENTS = 0x10;
    private const int EI_SEQ = 0;            // dword (slot): sequence, written last
    private const int EI_ID = 4;             // dword: target charId [rbx+0]
    private const int EI_HP = 8;             // dword: target HP word[rbx+0x30]
    private const int EI_CTRL = 12;          // dword: control marker (0 none, 1 logonly, 2 wrote)
    private const int EI_TARGET = 16;        // qword: target ptr (rbx)
    private const int EI_BEFORE = 24;        // qword: [rbx+0x46] before (bytes 0x46..0x4D)
    private const int EI_AFTER = 32;         // qword: [rbx+0x46] after
    private const int EI_BEFORE_4E = 40;     // dword: byte[rbx+0x4E] before
    private const int EI_AFTER_4E = 44;      // dword: byte[rbx+0x4E] after
    private const int EVADE_INPUT_SLOT_SIZE = 48;
    private const int EVADE_INPUT_BUFFER_SIZE = EI_EVENTS + (EVADE_INPUT_RING_SIZE * EVADE_INPUT_SLOT_SIZE);

    // Roll-VERDICT probe/control buffer (hook at RVA 0x30F4A7 = "mov r10d,eax" right after the single VM
    // avoidance roll 0x30FA34 returns). eax IS the hit/miss verdict in REAL code (next: test eax,eax; je
    // miss). Forcing eax here overrides native evade+reactions (both virtualized inside the roll) WITHOUT
    // any data change; the original "mov r10d,eax" propagates our value to the second consumer. rbx = the
    // acting unit (attacker) at this point (live-confirmed; the producer walks units and selects the actor).
    private const int ROLL_VERDICT_RING_SIZE = 32;
    private const int ROLL_VERDICT_RING_MASK = ROLL_VERDICT_RING_SIZE - 1;
    private const int RV_COUNT = 0;          // dword header: total events
    private const int RV_CTRL_WRITES = 4;    // dword header: live control overrides (persistent)
    private const int RV_EVENTS = 0x10;
    private const int RV_SEQ = 0;            // dword (slot): sequence, written last
    private const int RV_ID = 4;             // dword: acting-unit charId [rbx+0]
    private const int RV_NATIVE = 8;         // dword: native verdict eax (pre-override)
    private const int RV_FINAL = 12;         // dword: final verdict eax (post-override)
    private const int RV_UNIT = 16;          // qword: acting-unit ptr (rbx)
    private const int RV_CTRL = 24;          // dword: control marker (0 none, 1 logonly, 2 forced)
    private const int ROLL_VERDICT_SLOT_SIZE = 32;
    private const int ROLL_VERDICT_BUFFER_SIZE = RV_EVENTS + (ROLL_VERDICT_RING_SIZE * ROLL_VERDICT_SLOT_SIZE);
    private const int PRECLAMP_RING_SIZE = 32;
    private const int PRECLAMP_RING_MASK = PRECLAMP_RING_SIZE - 1;
    private const int PRECLAMP_SLOT_SIZE = 0x440;
    private const int P_COUNT = 0;
    private const int P_STATIC_WRITES = 4;
    private const int P_PLAN_WRITES = 8;
    private const int P_MANAGED_CALLBACKS = 12;
    private const int P_EVENTS = 0x10;
    private const int P_SEQ = 0;
    private const int P_UNIT = 8;
    private const int P_STATE = 16;
    private const int P_ID = 24;
    private const int P_TEAM = 28;
    private const int P_HP = 32;
    private const int P_MAX_HP = 36;
    private const int P_OLD_DEBIT = 40;
    private const int P_OLD_CREDIT = 44;
    private const int P_FORCED_DEBIT = 48;
    private const int P_FORCED_CREDIT = 52;
    private const int P_FLAGS = 56;
    private const int P_ACTION = 60;
    private const int P_REGS = 64;
    private const int P_STACK_DUMP = 0xC0;
    private const int PRECLAMP_STACK_DUMP_SIZE = 0x100;
    private const int P_STATE_DUMP = P_STACK_DUMP + PRECLAMP_STACK_DUMP_SIZE;
    private const int PRECLAMP_STATE_DUMP_SIZE = 0x80;
    private const int P_UNIT_DUMP = P_STATE_DUMP + PRECLAMP_STATE_DUMP_SIZE;
    private const int PRECLAMP_UNIT_DUMP_SIZE = 0x200;
    private const int PRECLAMP_EVENT_BUFFER_SIZE = P_EVENTS + (PRECLAMP_RING_SIZE * PRECLAMP_SLOT_SIZE);
    private const int PRECLAMP_PLAN_MAX_SLOTS = 32;
    private const int PRECLAMP_PLAN_SLOT_SIZE = 0x50;
    private const int P_PLAN_TABLE = PRECLAMP_EVENT_BUFFER_SIZE;
    private const int PLAN_ACTIVE = 0;
    private const int PLAN_TARGET = 8;
    private const int PLAN_EXPECTED_DEBIT = 16;
    private const int PLAN_EXPECTED_CREDIT = 20;
    private const int PLAN_FORCED_DEBIT = 24;
    private const int PLAN_FORCED_CREDIT = 28;
    private const int PLAN_MAX_WRITES = 32;
    private const int PLAN_WRITE_COUNT = 36;
    private const int PLAN_FLAGS = 40;
    private const int PLAN_ACTION = 44;
    private const int PLAN_BATCH = 48;
    private const int PLAN_CREATED_TICK = 56;
    private const int PLAN_EXPECTED_HP = 64;
    private const int PLAN_EXPECTED_MAX_HP = 68;
    private const int PRECLAMP_BUFFER_SIZE = PRECLAMP_EVENT_BUFFER_SIZE + (PRECLAMP_PLAN_MAX_SLOTS * PRECLAMP_PLAN_SLOT_SIZE);
    private const int PREVIEW_HITPCT_BUFFER_SIZE = 64;
    private const int PREVIEW_DAMAGE_BUFFER_SIZE = 320;
    private const int PREVIEW_SOURCE_BUFFER_SIZE = 320;
    private const int CALC_ENTRY_RING_SLOTS = 64;
    private const int CALC_ENTRY_BUFFER_SIZE = 16 + (CALC_ENTRY_RING_SLOTS * 16); // [0] u32 count; 16B slots
    private const int LT3_ROLL_BUFFER_SIZE = 48;                                   // +16 magic slot, +32 status slot
    private const int STAGED_BUNDLE_HEADER = 28;                                   // [0]count [4]forceChar [8]kind [12]ail [16]mask [20]dmg [24]resFlag
    private const int STAGED_BUNDLE_RING_SLOTS = 64;
    private const int STAGED_BUNDLE_BUFFER_SIZE = STAGED_BUNDLE_HEADER + (STAGED_BUNDLE_RING_SLOTS * 16);
    private static readonly string[] RegisterNames =
    [
        "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp", "rsp",
        "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
    ];
    private nint _buf;
    private nint _landmarkBuf;
    private nint _preClampDamageRewriteBuf;
    private nint _resultSelectorProbeBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _hook;
    private Reloaded.Hooks.Definitions.IAsmHook? _preClampDamageRewriteHook;
    private PreClampManagedCallback? _preClampManagedCallback;
    private Reloaded.Hooks.Definitions.IReverseWrapper<PreClampManagedCallback>? _preClampManagedCallbackWrapper;
    private DclHitDecisionCallback? _dclHitDecisionCallback;
    private Reloaded.Hooks.Definitions.IReverseWrapper<DclHitDecisionCallback>? _dclHitDecisionWrapper;
    private Reloaded.Hooks.Definitions.IAsmHook? _resultSelectorProbeHook;
    private nint _evadeInputProbeBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _evadeInputProbeHook;
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _evadeCopierHooks = new();
    private nint _rollVerdictProbeBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _rollVerdictProbeHook;
    private nint _previewHitPctBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _previewHitPctHook;
    private nint _previewDamageBuf;
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _previewDamageHooks = new();
    private nint _previewSourceBuf;
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _previewSourceHooks = new();
    private nint _calcEntryBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _calcEntryHook;
    private nint _lt3RollBuf;
    private nint _reactionRollBuf;
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _reactionChanceHooks = new();
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _evadeRecordHooks = new();
    private Reloaded.Hooks.Definitions.IAsmHook? _magicAccuracyHook;
    private Reloaded.Hooks.Definitions.IAsmHook? _statusChanceHook;
    private nint _rollRngBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _rollRngHook;
    private nint _stagedBundleBuf;
    private Reloaded.Hooks.Definitions.IAsmHook? _stagedBundleHook;
    private readonly List<Reloaded.Hooks.Definitions.IAsmHook> _landmarkHooks = new();
    private readonly Dictionary<int, LandmarkProbe> _landmarkProbesById = new();
    private Thread? _poller;
    private volatile bool _running = true;

    private readonly Dictionary<nint, int> _lastHp = new();   // unit pointer -> last HP (damage deltas)
    private readonly Dictionary<nint, int> _lastMp = new();   // unit pointer -> last MP (resource deltas)
    private readonly Dictionary<nint, long> _lastHpSampleTick = new();
    private readonly Dictionary<nint, long> _lastMpSampleTick = new();
    private readonly Dictionary<nint, string> _lastActionProbeState = new();
    private readonly Dictionary<nint, string> _lastPreClampFormulaCandidateKey = new();
    private readonly Dictionary<string, long> _preClampFormulaPlanKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<nint, long> _lastActionProbeChangeTick = new();
    private readonly Dictionary<nint, int> _lastActionProbeActionId = new();
    private readonly Dictionary<nint, long> _lastActionProbeActionIdChangeTick = new();
    private readonly Dictionary<nint, int> _lastActionProbeActiveActionId = new();
    private readonly Dictionary<nint, long> _lastActionProbeActiveActionChangeTick = new();
    private readonly Dictionary<nint, string> _seen = new();  // unit pointer -> last stat line (re-log if it changes)
    private readonly HashSet<nint> _dumped = new();           // unit pointer -> emitted full struct dump
    private readonly Dictionary<nint, byte[]> _lastRaw = new();
    private readonly Dictionary<nint, int> _diffCounts = new();
    private readonly HashSet<nint> _unitRegistry = new();
    private readonly Dictionary<nint, UnitObservation> _unitObservations = new();
    private UnitObservationView[] _unitObservationSnapshot = Array.Empty<UnitObservationView>();
    private readonly Dictionary<nint, byte[]> _recentAliveRaw = new();   // unit -> last raw seen while HP>0 (death capture baseline)
    private readonly Dictionary<nint, byte[]> _deathCaptureRaw = new();  // unit -> raw at last death/follow tick (delayed-flag diff)
    private readonly Dictionary<nint, int> _deathCaptureFollow = new();  // unit -> remaining post-death follow-up dump ticks
    private readonly HashSet<nint> _deathCaptured = new();               // unit -> already captured this death (until revived)
    private readonly Dictionary<nint, byte[]> _lastObservedRaw = new();   // unit -> previous poll/hook raw for HP-event diffs
    private readonly ValueRewriteEchoGuard _hpRewriteEchoGuard = new("HP");
    private readonly ValueRewriteEchoGuard _mpRewriteEchoGuard = new("MP");
    private long _battleEventIndex;
    private long _lastHookObservationTick;
    private int _pollErrorCount;
    private int _registryLimitLogCount;
    private int _hookRegisterProbeLogs;
    private int _hookRegisterProbeEventLogs;
    private int _hookRegisterPointerScanLogs;
    private int _preClampPointerScanLogs;
    private int _preClampActorDumpLogs;
    private int _preClampActorCtxLogs;
    private int _preClampEquipLogs;
    private int _hpEventProbeLogs;
    private int _actionBoundaryProbeLogs;
    private int _preClampFormulaCandidateLogs;
    private int _landmarkProbeLogs;
    private int _lastLandmarkProbeSequence;
    private int _lastPreClampDamageRewriteSequence;
    private int _lastPreClampManagedCallbackSequence;
    private readonly object _calcEntryDrainGate = new();
    private int _calcEntryDclSeen;
    private readonly object _dclDecisionLogGate = new();
    private readonly Queue<string> _dclDecisionLogQueue = new();
    private int _dclDecisionLogCount;
    private long _dclCacheMissCount;
    private readonly DclHitDecisionCache _dclHitDecisionCache = new();
    private readonly object _dclHitStampedTargetGate = new();
    private readonly bool[] _dclHitStampedTargets = new bool[64];
    private readonly object _dclHitRngGate = new();
    private Random? _dclHitRng;
    private int _dclHitLogCount;
    private long _dclHitFailCount;
    private long _preClampManagedFormulaResolvedCount;
    private long _preClampManagedFormulaUnresolvedCount;
    private long _lastPreClampManagedFormulaResolvedLogged;
    private long _lastPreClampManagedFormulaUnresolvedLogged;
    private int _preClampManagedLastTargetId = -1;
    private int _preClampManagedLastCasterId = -1;
    private int _preClampManagedLastActionId = -1;
    private int _preClampManagedLastDebit = -1;
    private int _preClampManagedLastOldDebit = -1;
    private long _preClampManagedLastActorBase;
    private int _resultSelectorProbeLogs;
    private int _lastResultSelectorProbeSequence;
    private int _evadeInputProbeLogs;
    private int _lastEvadeInputProbeSequence;
    private int _rollVerdictProbeLogs;
    private int _lastRollVerdictProbeSequence;
    private int _evadeOverrideLogs;
    private int _braveOverrideLogs;
    private int _statusOverrideLogs;
    private long _probeEventIndex;
    private nint _moduleBase;
    private int _moduleSize;

    [Reloaded.Hooks.Definitions.X64.FunctionAttribute(
        [
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rcx,
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rdx,
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.r8,
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.r9,
        ],
        Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rax,
        true)]
    private delegate long PreClampManagedCallback(long targetPtr, long statePtr, long originalRsp, long bufferPtr);

    [Reloaded.Hooks.Definitions.X64.FunctionAttribute(
        [
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rcx,
            Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rdx,
        ],
        Reloaded.Hooks.Definitions.X64.FunctionAttribute.Register.rax,
        true)]
    private delegate long DclHitDecisionCallback(long orderRecordPtr, long targetIdx);

    private static readonly int[] ActionBoundaryOffsets =
    [
        0x30, 0x31, 0x61, 0x63,
        0x18C, 0x18D,
        0x1A0, 0x1A1, 0x1A2, 0x1A3,
        0x1B8, 0x1B9, 0x1BA, 0x1BB,
        0x1C4, 0x1C5, 0x1C6, 0x1C7,
        0x1D8, 0x1DB, 0x1DD, 0x1E0, 0x1E5, 0x1EF, 0x1F1, 0x1F5,
    ];

    private static readonly (int Rva, string Name)[] CodeLandmarks =
    [
        (0x205210, "result-selector"),
        (0x226D98, "battle-base-ptr"),
        (0x2D7AC0, "target-cache-write-1c4"),
        (0x2D7AEC, "target-cache-init-1c4"),
        (0x2F2EC1, "ko-stack-outer"),
        (0x2F37A2, "ko-stack-mid"),
        (0x2F3884, "ko-stack-inner"),
        (0x30A685, "damage-mult-2"),
        (0x30A6D3, "ko-write-1f5"),
        (0x30A908, "ko-write-61"),
        (0x30A911, "ko-write-1ef"),
        (0x30AAFC, "death-state-write-1bb"),
        (0x30D42A, "ko-read-1ef"),
        (0x30D432, "ko-mask-write-1ef"),
        (0x30D43C, "ko-write-61-late"),
    ];

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
            _moduleBase = baseAddr;
            _moduleSize = module?.ModuleMemorySize ?? 0;

            var ctl = _modLoader.GetController<IStartupScanner>();
            if (ctl is null || !ctl.TryGetTarget(out var scanner) || scanner is null)
            { Line("ERROR: IStartupScanner unavailable."); Flush(); return; }

            InstallMemoryTableProbes(scanner, baseAddr);

            _buf = Marshal.AllocHGlobal(B_SIZE);
            for (int i = 0; i < B_SIZE; i++) Marshal.WriteByte(_buf, i, 0);
            InstallLandmarkProbesIfEnabled(baseAddr);
            InstallPreClampDamageRewriteIfEnabled(baseAddr);
            InstallResultSelectorProbeIfEnabled(baseAddr);
            InstallEvadeInputProbeIfEnabled(baseAddr);
            InstallEvadeCopierOverrideIfEnabled(baseAddr);
            InstallRollVerdictProbeIfEnabled(baseAddr);
            InstallPreviewHitPctControlIfEnabled(baseAddr);
            InstallPreviewDamageControlIfEnabled(baseAddr);
            InstallPreviewForecastSourceControlIfEnabled(baseAddr);
            InstallCalcEntryProbeIfEnabled(baseAddr);
            InstallMagicAccuracyControlIfEnabled(baseAddr);
            InstallStatusChanceControlIfEnabled(baseAddr);
            InstallReactionChanceControlIfEnabled(baseAddr);
            InstallEvadeRecordOverrideIfEnabled(baseAddr);
            InstallRollRngProbeIfEnabled(baseAddr);
            InstallStagedBundleProbeIfEnabled(baseAddr);

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

    private void InstallLandmarkProbesIfEnabled(nint moduleBase)
    {
        if (!_settings.LandmarkProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[LANDMARK-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        var enabled = _settings.LandmarkProbes
            .Where(probe => probe.Enabled)
            .ToList();
        if (enabled.Count == 0)
            return;

        _landmarkBuf = Marshal.AllocHGlobal(LANDMARK_BUFFER_SIZE);
        for (int i = 0; i < LANDMARK_BUFFER_SIZE; i++)
            Marshal.WriteByte(_landmarkBuf, i, 0);

        Line($"[LANDMARK] configured={_settings.LandmarkProbes.Count} enabled={enabled.Count} ring={LANDMARK_RING_SIZE}");
        int id = 1;
        foreach (var probe in enabled)
        {
            probe.Normalize();
            string name = probe.TraceName;
            if (!probe.TryValidate(out string error))
            {
                Line($"[LANDMARK-SKIP {name}] {error}");
                continue;
            }

            nint address = moduleBase + probe.Rva;
            if (!ValidateLandmarkBytes(address, probe, out string byteError))
            {
                Line($"[LANDMARK-SKIP {name}] rva=0x{probe.Rva:X} {byteError}");
                continue;
            }

            try
            {
                var asm = BuildLandmarkHookAsm(id);
                _landmarkHooks.Add(_hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate());
                _landmarkProbesById[id] = probe;
                Line(
                    $"[LANDMARK-HOOK {name}] id={id} rva=0x{probe.Rva:X} addr=0x{address:X} " +
                    $"base={probe.BaseRegister} access={probe.Access} expected={probe.ExpectedBytes}");
                id++;
            }
            catch (Exception ex)
            {
                Line($"[LANDMARK-FAILED {name}] rva=0x{probe.Rva:X} {ex.GetType().Name}: {ex.Message}");
            }
        }

        Flush();
    }

    // PREVIEW HIT-% display control. The differential memory scan located the on-screen
    // attack hit% at static buffer 0x1407832C0 (RVA 0x7832C0). Real code copies it there
    // from a live forecast object: at RVA 0x227FFA `movzx eax, word [rbp+0x2C]` loads the
    // computed %, and 0x228004 `mov word [0x7832C0], ax` stores it for the renderer. We hook
    // 0x227FFE (`mov r10d, 2`, a clean non-RIP instruction between the load and the store):
    // ExecuteFirst runs our asm, then the stolen `mov r10d,2`, then falls into the store. By
    // setting AX to a forced value first, the game's own store writes OUR value at copy time,
    // BEFORE the renderer reads the buffer -> the displayed % is deterministically ours, no
    // race. Buffer records: [0]=fire count, [4]=last natural %, [8]=forced value (-1=logOnly),
    // [12]=site RVA. Read it externally (addr printed at install) to verify without the screen.
    private void InstallPreviewHitPctControlIfEnabled(nint moduleBase)
    {
        if (!_settings.PreviewHitPctControlEnabled)
            return;
        if (_hooks is null)
        {
            Line("[PREVIEW-HITPCT-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _previewHitPctBuf = Marshal.AllocHGlobal(PREVIEW_HITPCT_BUFFER_SIZE);
        for (int i = 0; i < PREVIEW_HITPCT_BUFFER_SIZE; i++)
            Marshal.WriteByte(_previewHitPctBuf, i, 0);

        nint address = moduleBase + _settings.PreviewHitPctRva;
        if (!ValidateExpectedBytes(address, _settings.PreviewHitPctExpectedBytes, out string byteError))
        {
            Line($"[PREVIEW-HITPCT-SKIP] rva=0x{_settings.PreviewHitPctRva:X} {byteError}");
            Flush();
            return;
        }

        try
        {
            var asm = BuildPreviewHitPctHookAsm();
            _previewHitPctHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line(
                $"[PREVIEW-HITPCT-HOOK] rva=0x{_settings.PreviewHitPctRva:X} addr=0x{address:X} " +
                $"buf=0x{_previewHitPctBuf:X} forced={FormatMaybeInt(_settings.PreviewHitPctForcedValue)} " +
                $"logOnly={(_settings.PreviewHitPctLogOnly ? 1 : 0)}");
        }
        catch (Exception ex)
        {
            Line($"[PREVIEW-HITPCT-FAILED] rva=0x{_settings.PreviewHitPctRva:X} {ex.GetType().Name}: {ex.Message}");
        }

        Flush();
    }

    private string[] BuildPreviewHitPctHookAsm()
    {
        string buf = $"0{_previewHitPctBuf:X}h";
        bool force = _settings.PreviewHitPctForcedValue >= 0 && !_settings.PreviewHitPctLogOnly;
        int forced = Math.Clamp(_settings.PreviewHitPctForcedValue, 0, 0xFFFF);
        string forcedRecord = force ? forced.ToString() : "0FFFFFFFFh";
        string siteId = $"0{_settings.PreviewHitPctRva:X}h";

        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "pushfq",
            $"mov rax, {buf}",
            "test rax, rax",
            "jz .skip",
            "mov ecx, [rax]",            // fire count
            "add ecx, 1",
            "mov [rax], ecx",
            "mov ecx, [rsp+16]",         // original rax (flags=8 + rcx=8 below it)
            "and ecx, 0FFFFh",           // low word = natural hit%
            "mov [rax+4], ecx",
            $"mov dword [rax+8], {forcedRecord}",
            $"mov dword [rax+12], {siteId}",
            ".skip:",
            "popfq",
            "pop rcx",
            "pop rax",
        };
        if (force)
            asm.Add($"mov ax, {forced}");   // game's own store at 0x228004 now writes OUR value
        return asm.ToArray();
    }

    // PREVIEW DAMAGE display control. The on-screen forecast DAMAGE number lives at static buffer
    // 0x1407832BE (RVA 0x7832BE), written by a single real-code store at 0x228488 (`mov word
    // [0x7832BE], dx`). That store is a JUMP TARGET (a format-dispatch's branches all jmp onto it) AND
    // RIP-relative, so we do NOT hook it directly. Instead we hook the terminal `jmp 0x228488` of each
    // numeric branch (clean 5-byte E9 where the value is already in dx) with ExecuteFirst: set dx, then
    // the stolen jmp falls into the unmodified store, which writes OUR dx -> the displayed number is
    // ours, no race. Confirmed every branch parks the value in dx. WHICH branch an action uses varies
    // (a basic attack/Fire forecast does NOT use 0x2280D7), so we hook ALL numeric branches; the buffer
    // records per-branch fire count + last natural so we can see which branch a given preview used.
    // ValidateExpectedBytes skips any branch whose bytes don't match (safe). Buffer: [0]=total fires;
    // per site i at 16+i*16: [+0]=fires [+4]=last natural [+8]=site RVA.
    private static readonly (int Rva, string Bytes)[] PreviewDamageBranchSites =
    {
        (0x22802F, "E9 54 04 00 00"),
        (0x228050, "E9 33 04 00 00"),
        (0x22806E, "E9 15 04 00 00"),
        (0x2280D7, "E9 AC 03 00 00"),
        (0x228125, "E9 5E 03 00 00"),
        (0x228195, "E9 EE 02 00 00"),
        (0x2281E9, "E9 9A 02 00 00"),
        (0x228316, "E9 6D 01 00 00"),
    };

    private void InstallPreviewDamageControlIfEnabled(nint moduleBase)
    {
        if (!_settings.PreviewDamageControlEnabled)
            return;
        if (_hooks is null)
        {
            Line("[PREVIEW-DAMAGE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _previewDamageBuf = Marshal.AllocHGlobal(PREVIEW_DAMAGE_BUFFER_SIZE);
        for (int i = 0; i < PREVIEW_DAMAGE_BUFFER_SIZE; i++)
            Marshal.WriteByte(_previewDamageBuf, i, 0);

        int installed = 0;
        for (int slot = 0; slot < PreviewDamageBranchSites.Length; slot++)
        {
            var (rva, bytes) = PreviewDamageBranchSites[slot];
            nint address = moduleBase + rva;
            if (!ValidateExpectedBytes(address, bytes, out string byteError))
            {
                Line($"[PREVIEW-DAMAGE-SKIP] rva=0x{rva:X} slot={slot} {byteError}");
                continue;
            }
            try
            {
                var asm = BuildPreviewDamageHookAsm(rva, slot);
                var hook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
                _previewDamageHooks.Add(hook);
                installed++;
                Line($"[PREVIEW-DAMAGE-HOOK] rva=0x{rva:X} addr=0x{address:X} slot={slot}");
            }
            catch (Exception ex)
            {
                Line($"[PREVIEW-DAMAGE-FAILED] rva=0x{rva:X} slot={slot} {ex.GetType().Name}: {ex.Message}");
            }
        }
        Line($"[PREVIEW-DAMAGE-SUMMARY] buf=0x{_previewDamageBuf:X} installed={installed}/{PreviewDamageBranchSites.Length} " +
            $"forced={FormatMaybeInt(_settings.PreviewDamageForcedValue)} logOnly={(_settings.PreviewDamageLogOnly ? 1 : 0)}");
        Flush();
    }

    private string[] BuildPreviewDamageHookAsm(int siteRva, int slot)
    {
        bool force = _settings.PreviewDamageForcedValue >= 0 && !_settings.PreviewDamageLogOnly;
        int forced = Math.Clamp(_settings.PreviewDamageForcedValue, 0, 0xFFFF);
        string total = $"0{_previewDamageBuf:X}h";
        string slotAddr = $"0{(_previewDamageBuf + 16 + (slot * 16)):X}h";
        string siteId = $"0{siteRva:X}h";

        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "pushfq",
            $"mov rax, {total}",
            "test rax, rax",
            "jz .skip",
            "add dword [rax], 1",         // [buf+0] total fire count
            $"mov rax, {slotAddr}",        // this branch's slot record
            "add dword [rax], 1",         // slot fire count
            "movzx ecx, dx",              // natural forecast number (dx = value about to be stored)
            "mov [rax+4], ecx",           // slot last natural
            $"mov dword [rax+8], {siteId}",// slot site RVA
            ".skip:",
            "popfq",
            "pop rcx",
            "pop rax",
        };
        if (force)
            asm.Add($"mov dx, {forced}");   // stolen `jmp 0x228488` falls into the store, which writes OUR dx
        return asm.ToArray();
    }

    // FORECAST DAMAGE SOURCE control — the COHERENT lever (number + HP-bar + apply all agree).
    // The forecast "object" (global 0x142FF3CF8) is just unit_table[idx]+0x1BE, so obj+0x6 == unit+0x1C4 ==
    // the staged HP-damage field. The preview NUMBER (formatter at 0x2284xx), the HP-bar ghost-depletion
    // block, AND the apply path all read this one field; the pre-clamp hook (0x30A66F) rewrites it at
    // RESOLUTION. Painting the display number (0x7832BE) was cosmetic because it never touched +0x1C4 —
    // so the bar stayed natural. Controlling obj+0x6 at the finalizers that COMPUTE it makes number + bar
    // coherent (race-free: we write at the engine's own store, before any downstream read). The engine has
    // a few formula finalizers that write obj+0x6; we hook each (ExecuteFirst, force the store's register)
    // so whichever an action uses, +0x1C4 = our value. Per-site counters reveal which finalizer fired.
    // Buffer: [0]=total fires; per site i at 16+i*16: [+0]=fires [+4]=last natural [+8]=site RVA.
    private static readonly (int Rva, string Bytes, string Reg)[] PreviewForecastSourceSites =
    {
        (0x30637E, "66 41 89 50 06", "dx"),   // mov [r8+6],dx  — confirmed live: MAGIC (Fire) finalizer
        (0x307DC4, "66 41 89 52 06", "dx"),   // mov [r10+6],dx — (100-p1)(100-p2)*base/10000 path
        (0x309664, "66 41 89 51 06", "cx"),   // mov [r9+6],cx  — product-clamped path
        (0x308D8F, "66 89 41 06",    "ax"),   // mov [rcx+6],ax — Q15-scaled store (physical-attack candidate)
    };

    private void InstallPreviewForecastSourceControlIfEnabled(nint moduleBase)
    {
        if (!_settings.PreviewForecastSourceControlEnabled)
            return;
        if (_hooks is null)
        {
            Line("[PREVIEW-SOURCE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _previewSourceBuf = Marshal.AllocHGlobal(PREVIEW_SOURCE_BUFFER_SIZE);
        for (int i = 0; i < PREVIEW_SOURCE_BUFFER_SIZE; i++)
            Marshal.WriteByte(_previewSourceBuf, i, 0);

        int installed = 0;
        for (int slot = 0; slot < PreviewForecastSourceSites.Length; slot++)
        {
            var (rva, bytes, reg) = PreviewForecastSourceSites[slot];
            nint address = moduleBase + rva;
            if (!ValidateExpectedBytes(address, bytes, out string byteError))
            {
                Line($"[PREVIEW-SOURCE-SKIP] rva=0x{rva:X} slot={slot} {byteError}");
                continue;
            }
            try
            {
                var asm = BuildPreviewForecastSourceHookAsm(rva, slot, reg);
                var hook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
                _previewSourceHooks.Add(hook);
                installed++;
                Line($"[PREVIEW-SOURCE-HOOK] rva=0x{rva:X} addr=0x{address:X} slot={slot} reg={reg}");
            }
            catch (Exception ex)
            {
                Line($"[PREVIEW-SOURCE-FAILED] rva=0x{rva:X} slot={slot} {ex.GetType().Name}: {ex.Message}");
            }
        }
        Line($"[PREVIEW-SOURCE-SUMMARY] buf=0x{_previewSourceBuf:X} installed={installed}/{PreviewForecastSourceSites.Length} " +
            $"forced={FormatMaybeInt(_settings.PreviewForecastSourceForcedValue)} logOnly={(_settings.PreviewForecastSourceLogOnly ? 1 : 0)}");
        Flush();
    }

    private string[] BuildPreviewForecastSourceHookAsm(int siteRva, int slot, string reg)
    {
        bool force = _settings.PreviewForecastSourceForcedValue >= 0 && !_settings.PreviewForecastSourceLogOnly;
        int forced = Math.Clamp(_settings.PreviewForecastSourceForcedValue, 0, 0xFFFF);
        string total = $"0{_previewSourceBuf:X}h";
        string slotAddr = $"0{(_previewSourceBuf + 16 + (slot * 16)):X}h";
        string siteId = $"0{siteRva:X}h";

        // rbx is the scratch (NOT a store register — sites use dx/cx — so the natural value in {reg}
        // survives our bookkeeping and we read it cleanly).
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rbx",
            "pushfq",
            $"movzx ebx, {reg}",            // capture natural value FIRST (before rax is used as scratch — matters when reg is ax)
            $"mov rax, {total}",
            "test rax, rax",
            "jz .skip",
            "add dword [rax], 1",          // [buf+0] total fire count
            $"mov rax, {slotAddr}",         // this finalizer's slot record
            "add dword [rax], 1",          // slot fire count
            "mov [rax+4], ebx",            // slot last natural (captured above)
            $"mov dword [rax+8], {siteId}", // slot site RVA
            ".skip:",
            "popfq",
            "pop rbx",
            "pop rax",
        };
        if (force)
            asm.Add($"mov {reg}, {forced}");   // the engine's own store then writes OUR value to obj+0x6 (= +0x1C4)
        return asm.ToArray();
    }

    // CALC-ENTRY PROBE (LT3) — log-only ring on computeActionResult 0x309A44, the single real-code
    // entry the VM calls per (action, target) for forecast, execution and (hypothesis) AI evaluation.
    // rcx = the caster's 0x14-byte order record (@ caster+0x1A0: [0] caster slot idx, [1] type,
    // [2..3] ability id), dl = target unit index. One enemy turn with this probe answers the AI
    // same-calc question and gives the PREVIEW-time ability id.
    private void InstallCalcEntryProbeIfEnabled(nint moduleBase)
    {
        bool probe = _settings.CalcEntryProbeEnabled || _settings.DclPipelineEnabled;
        bool stamp = _settings.CalcEntryEvadeStampEnabled;
        bool hitControl = _settings.DclHitControlEnabled;
        if (!probe && !stamp && !hitControl)
            return;
        if (_hooks is null)
        {
            Line("[CALC-PROBE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        int rva = _settings.CalcEntryProbeRva;
        nint address = moduleBase + rva;
        if (!ValidateExpectedBytes(address, "48 89 5C 24 18 55 56 57", out string byteError))
        {
            Line($"[CALC-PROBE-SKIP] rva=0x{rva:X} {byteError}");
            Flush();
            return;
        }

        var asm = new List<string> { "use64" };
        if (probe)
        {
            _calcEntryBuf = Marshal.AllocHGlobal(CALC_ENTRY_BUFFER_SIZE);
            for (int i = 0; i < CALC_ENTRY_BUFFER_SIZE; i++)
                Marshal.WriteByte(_calcEntryBuf, i, 0);

            string buf = $"0{_calcEntryBuf:X}h";
            asm.AddRange(
            [
                "push rax",
                "push rbx",
                "push rsi",
                "pushfq",
                $"mov rax, {buf}",
                "mov ebx, dword [rax]",       // ring write index (total count)
                "and ebx, 0x3F",              // 64-slot ring; 32-bit op zero-extends rbx
                "shl ebx, 4",                 // *16 bytes per slot
                "lea rsi, [rax + rbx + 16]",
                "mov qword [rsi], rcx",       // order-record ptr
                "mov dword [rsi+8], edx",     // target unit index (dl)
                "mov ebx, dword [rcx]",       // casterIdx | type | abilityId  (captured at fire time)
                "mov dword [rsi+12], ebx",
                // Publish AFTER the slot is fully written (x86 TSO keeps store order): a consumer
                // that sees the new count never reads a torn/stale slot.
                "add dword [rax], 1",
                "popfq",
                "pop rsi",
                "pop rbx",
                "pop rax",
            ]);
        }
        if (hitControl)
        {
            _dclHitDecisionCallback = DclHitDecisionCallbackImpl;
            _dclHitDecisionWrapper = _hooks.CreateReverseWrapper(_dclHitDecisionCallback);
            if (_dclHitDecisionWrapper is null || _dclHitDecisionWrapper.WrapperPointer == 0)
            {
                Line("[DCL-HIT-SKIP] decision callback reverse wrapper unavailable");
                hitControl = false;
            }
            else
            {
                int seed = unchecked(Environment.TickCount ^ (int)Stopwatch.GetTimestamp());
                lock (_dclHitRngGate)
                    _dclHitRng = new Random(seed);
                asm.AddRange(BuildDclHitDecisionShimLines());
                Line($"[DCL-HIT-INSTALL] seed={seed} forcedRoll={FormatMaybeInt(_settings.DclHitForcedRoll)} " +
                     $"ttlMs={_settings.DclHitDecisionTtlMs} missClassEvade={Math.Clamp(_settings.DclMissClassEvadeValue, 0, 255)} " +
                     $"maxLogs={_settings.DclHitMaxLogs} formula={(string.IsNullOrWhiteSpace(_settings.DclHitChanceFormula) ? "off" : "on")}");
            }
        }
        // The static evade stamp and the hit-control decision callback write the same target bytes;
        // when both are configured (validator forbids it) the decision callback owns the site.
        if (stamp && !hitControl)
            asm.AddRange(BuildCalcEntryEvadeStampLines(moduleBase));
        else if (stamp)
            Line("[DCL-HIT] CalcEntryEvadeStamp suppressed: the hit-control decision callback owns the target evade bytes at this site");

        try
        {
            _calcEntryHook = _hooks.CreateAsmHook(asm.ToArray(), address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line($"[CALC-PROBE-HOOK] rva=0x{rva:X} addr=0x{address:X} probe={(probe ? $"on buf=0x{_calcEntryBuf:X} ring={CALC_ENTRY_RING_SLOTS}" : "off")} " +
                 $"evadeStamp={(stamp && !hitControl ? "ON " + DescribeEvadeCopierOverride() : "off")} hitControl={(hitControl ? "ON" : "off")}");
        }
        catch (Exception ex)
        {
            Line($"[CALC-PROBE-FAILED] {ex.GetType().Name}: {ex.Message}");
        }
        Flush();
    }

    // CALC-ENTRY EVADE STAMP — the per-attack input injection the engine doesn't have. ExecuteFirst
    // at computeActionResult 0x309A44 (dl = target unit index): resolve the TARGET's unit struct and
    // stamp the configured Family-1 evade bytes microseconds before the VM avoidance roll inside this
    // very call. Closes the residual leak seen in LT5-A (a VM-side/init writer the static RE cannot
    // see re-stamped shield evade once): no state edge can intervene between this stamp and the roll.
    // Reuses the EvadeCopierOverride* value profile (one forced-evade profile, two delivery points).
    private string[] BuildCalcEntryEvadeStampLines(nint moduleBase)
    {
        nint unitTable = moduleBase + 0x1853CE0;   // unit struct table (stride 0x200), image-base-relative
        int target = _settings.EvadeCopierOverrideTargetCharId;
        (int Off, int Val)[] bytes =
        {
            (0x46, _settings.EvadeCopierOverride46), (0x47, _settings.EvadeCopierOverride47),
            (0x48, _settings.EvadeCopierOverride48), (0x49, _settings.EvadeCopierOverride49),
            (0x4A, _settings.EvadeCopierOverride4A), (0x4B, _settings.EvadeCopierOverride4B),
            (0x4C, _settings.EvadeCopierOverride4C), (0x4D, _settings.EvadeCopierOverride4D),
            (0x4E, _settings.EvadeCopierOverride4E),
        };
        var c = new List<string>
        {
            "push rax",
            "push rbx",
            "pushfq",
            "movzx eax, dl",              // target unit index
            "cmp eax, 0x40",
            "jae .ces_done",              // guard: 64 slots max
            "shl eax, 9",                 // idx * 0x200
            $"mov rbx, 0{unitTable:X}h",
            "add rbx, rax",               // rbx = target unit struct
        };
        if (target >= 0)
        {
            c.Add($"cmp byte [rbx], {target & 0xFF}");
            c.Add("jne .ces_done");
        }
        foreach (var (off, val) in bytes)
            if (val >= 0)
                c.Add($"mov byte [rbx+{off:X}h], {val & 0xFF}");
        c.Add(".ces_done:");
        c.Add("popfq");
        c.Add("pop rbx");
        c.Add("pop rax");
        return c.ToArray();
    }

    // DCL HIT-CONTROL DECISION SHIM — appended into the SAME calc-entry asm hook, AFTER the probe
    // ring-write block (single hook at 0x309A44: deterministic internal order, no cross-hook
    // activation-order dependence; this codebase composes same-site logic by appending blocks into
    // one CreateAsmHook, exactly like probe+stamp did for LT5/LT7). At shim entry the registers
    // still hold the original call args — the probe block saves/restores all of its scratch — so
    // rcx = caster order record and dl = target unit index. We save every volatile register +
    // flags, marshal (rcx, zero-extended dl) into the managed decision callback via the reverse
    // wrapper, and restore everything, so neither the preceding probe ring-write nor the stolen
    // prologue bytes (mov [rsp+18],rbx / push rbp / push rsi / push rdi) see any modified state.
    // Stack alignment: the hook sits on the function's FIRST instruction, so rsp % 16 == 8 at
    // entry (return address pushed); 8 pushes (64 bytes) keep rsp ≡ 8; sub rsp, 0x88 (0x20 shadow
    // + 0x60 xmm save + 8 pad) makes rsp % 16 == 0 at the call, per the Microsoft x64 ABI.
    // xmm0-5 (volatile) are saved around the managed call, mirroring the pre-clamp callback shim.
    private string[] BuildDclHitDecisionShimLines()
    {
        string callback = $"0{_dclHitDecisionWrapper!.WrapperPointer:X}h";
        return
        [
            "push rax",
            "push rcx",
            "push rdx",
            "push r8",
            "push r9",
            "push r10",
            "push r11",
            "pushfq",
            // rcx already = order-record ptr (arg1); arg2 = target unit index from the live dl.
            "movzx edx, dl",
            "sub rsp, 88h",
            "movdqu [rsp+20h], xmm0",
            "movdqu [rsp+30h], xmm1",
            "movdqu [rsp+40h], xmm2",
            "movdqu [rsp+50h], xmm3",
            "movdqu [rsp+60h], xmm4",
            "movdqu [rsp+70h], xmm5",
            $"mov r11, {callback}",
            "call r11",
            "movdqu xmm0, [rsp+20h]",
            "movdqu xmm1, [rsp+30h]",
            "movdqu xmm2, [rsp+40h]",
            "movdqu xmm3, [rsp+50h]",
            "movdqu xmm4, [rsp+60h]",
            "movdqu xmm5, [rsp+70h]",
            "add rsp, 88h",
            "popfq",
            "pop r11",
            "pop r10",
            "pop r9",
            "pop r8",
            "pop rdx",
            "pop rcx",
            "pop rax",
        ];
    }

    private int _calcEntrySeen;
    private const int CALC_ENTRY_LOG_PER_POLL = 12;
    private const int CALC_TURN_OWNER_GLOBAL_RVA = 0x7B0708;   // dword: current turn-owner unit index
    private void CaptureCalcEntryProbeEvents(long nowTick)
        => DrainCalcEntryProbeEvents(nowTick, emitLogs: true, ref _calcEntrySeen);

    private void DrainCalcEntryProbeEventsForDcl(long nowTick)
        => DrainCalcEntryProbeEvents(nowTick, emitLogs: false, ref _calcEntryDclSeen);

    // NOTE: cache entries are stamped with DRAIN time, not fire time (the asm stub records no
    // timestamp). The poller drains every UnitPollIntervalMs and the DCL hook drains at entry, so
    // stamp lag is bounded by the poll cadence in practice; DclActionContextMaxAgeMs is a guard on
    // top of that, not an exact event-age bound. The real action's own calc-entry always lands
    // later in the ring than any stale preview and overwrites the per-target cache on the same drain.
    private void DrainCalcEntryProbeEvents(long nowTick, bool emitLogs, ref int seen)
    {
        List<string>? lines = null;
        lock (_calcEntryDrainGate)
        {
            if (_calcEntryBuf == 0)
                return;

            int count = Marshal.ReadInt32(_calcEntryBuf);
            if (count == seen)
                return;

            int fresh = count - seen;
            int start = fresh < 0
                ? Math.Max(0, count - Math.Min(count, CALC_ENTRY_RING_SLOTS))
                : Math.Max(seen, count - Math.Min(fresh, CALC_ENTRY_RING_SLOTS));
            int logged = 0;
            long unitTable = _moduleBase.ToInt64() + _settings.PreviewForecastUnitTableRva;
            int turnOwner = emitLogs ? Marshal.ReadInt32(_moduleBase + CALC_TURN_OWNER_GLOBAL_RVA) : -1;
            if (emitLogs)
                lines = new List<string>();

            for (int n = start; n < count; n++)
            {
                nint slot = _calcEntryBuf + 16 + ((n & (CALC_ENTRY_RING_SLOTS - 1)) * 16);
                long rec = Marshal.ReadInt64(slot);
                int targetIdx = Marshal.ReadInt32(slot + 8) & 0xFF;
                int packed = Marshal.ReadInt32(slot + 12);
                int casterIdx = packed & 0xFF;
                int type = (packed >> 8) & 0xFF;
                int abilityId = (packed >> 16) & 0xFFFF;
                _dclActionCache.Record(targetIdx, casterIdx, type, abilityId, nowTick);

                if (!emitLogs || logged >= CALC_ENTRY_LOG_PER_POLL)
                    continue;

                long casterRel = rec - 0x1A0 - unitTable;
                long casterSlot = (casterRel >= 0 && casterRel % 0x200 == 0) ? casterRel / 0x200 : -1;
                int casterTeam = casterSlot >= 0
                    ? Marshal.ReadByte((nint)(unitTable + casterSlot * 0x200 + 0x04))
                    : -1;
                lines!.Add($"[CALC] n={n} rec=0x{rec:X} casterSlot={casterSlot} casterIdx={casterIdx} type=0x{type:X2} " +
                           $"abilityId={abilityId} (0x{abilityId:X4}) targetIdx={targetIdx} casterTeam={casterTeam} turnOwner={turnOwner}");
                logged++;
            }

            if (emitLogs && count - start > logged)
                lines!.Add($"[CALC-BULK] +{count - start - logged} more fires this poll (total={count})");
            seen = count;
        }

        if (lines is null || lines.Count == 0)
            return;

        foreach (string line in lines)
            Line(line);
        Flush();
    }

    private void CaptureDclDecisionLogs()
    {
        List<string> lines;
        lock (_dclDecisionLogGate)
        {
            if (_dclDecisionLogQueue.Count == 0)
                return;

            lines = new List<string>(_dclDecisionLogQueue.Count);
            while (_dclDecisionLogQueue.Count > 0)
                lines.Add(_dclDecisionLogQueue.Dequeue());
        }

        foreach (string line in lines)
            Line(line);
        Flush();
    }

    // MAGIC-ACCURACY control (LT3) — hook 0x304E2E (`mov ecx, 0x64`, just AFTER the natural chance
    // lands in edx at 0x304E2B and just BEFORE roll(100, edx) at 0x304E33). Captures the natural
    // Faith-scaled chance; optionally forces edx (100 = magic always-hits, 0 = always-misses).
    private void InstallMagicAccuracyControlIfEnabled(nint moduleBase)
    {
        if (!_settings.MagicAccuracyControlEnabled)
            return;
        EnsureLt3RollBuf();
        InstallChanceHook(moduleBase, _settings.MagicAccuracyRva, "B9 64 00 00 00", _lt3RollBuf + 16,
            _settings.MagicAccuracyForcedChance, "MAGICROLL", h => _magicAccuracyHook = h);
    }

    // STATUS-CHANCE control (LT3) — hook 0x306633 (`lea ecx, [rbx+0x5C]`, AFTER `movzx edx,[g_7B07AC]`
    // at 0x30662C loads the natural staged status chance, BEFORE roll(100, edx) at 0x306636).
    // The LT2 data-poke of g_7B07AC was refuted (engine rewrites it at compute time); this hook fires
    // at compute time so the forced value is what the roll consumes.
    private void InstallStatusChanceControlIfEnabled(nint moduleBase)
    {
        if (!_settings.StatusChanceControlEnabled)
            return;
        EnsureLt3RollBuf();
        InstallChanceHook(moduleBase, _settings.StatusChanceRva, "8D 4B 5C", _lt3RollBuf + 32,
            _settings.StatusChanceForcedChance, "STATUSROLL", h => _statusChanceHook = h);
    }

    private void EnsureLt3RollBuf()
    {
        if (_lt3RollBuf != 0)
            return;
        _lt3RollBuf = Marshal.AllocHGlobal(LT3_ROLL_BUFFER_SIZE);
        for (int i = 0; i < LT3_ROLL_BUFFER_SIZE; i++)
            Marshal.WriteByte(_lt3RollBuf, i, 0);
    }

    private void InstallChanceHook(nint moduleBase, int rva, string expectedBytes, nint slotAddr,
        int forcedChance, string tag, Action<Reloaded.Hooks.Definitions.IAsmHook> store)
    {
        if (_hooks is null)
        {
            Line($"[{tag}-SKIP] no IReloadedHooks");
            Flush();
            return;
        }
        nint address = moduleBase + rva;
        if (!ValidateExpectedBytes(address, expectedBytes, out string byteError))
        {
            Line($"[{tag}-SKIP] rva=0x{rva:X} {byteError}");
            Flush();
            return;
        }
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "pushfq",
            $"mov rax, 0{slotAddr:X}h",
            "add dword [rax], 1",         // [slot+0] fire count
            "mov dword [rax+4], edx",     // [slot+4] last natural chance
            "popfq",
            "pop rax",
        };
        if (forcedChance >= 0)
            asm.Add($"mov edx, {Math.Clamp(forcedChance, 0, 100)}");  // the roll consumes OUR chance
        try
        {
            store(_hooks.CreateAsmHook(asm.ToArray(), address, AsmHookBehaviour.ExecuteFirst).Activate());
            Line($"[{tag}-HOOK] rva=0x{rva:X} addr=0x{address:X} forced={FormatMaybeInt(forcedChance)}");
        }
        catch (Exception ex)
        {
            Line($"[{tag}-FAILED] rva=0x{rva:X} {ex.GetType().Name}: {ex.Message}");
        }
        Flush();
    }

    // ROLL-RNG PROBE (LT3b, log-only) — ring probe on the shared RNG `roll(ecx=range, edx=chance)`
    // head 0x278EE0 (a Denuvo trampoline: `jmp` into the VM — the CALLERS are real code, so the
    // return address on the stack identifies every real roll site). Captures (retaddr, range, chance)
    // per call: one battle maps which of the 11 static callers actually fire for magic accuracy,
    // status infliction, reactions, crits — replacing per-site guesswork (LT3a: 0x304E2E/0x306633
    // never fired for Fire/Blind).
    private void InstallRollRngProbeIfEnabled(nint moduleBase)
    {
        if (!_settings.RollRngProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[RNG-PROBE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }
        int rva = _settings.RollRngProbeRva;
        nint address = moduleBase + rva;
        if (!ValidateExpectedBytes(address, "E9 0B F3 F4 0E", out string byteError))
        {
            Line($"[RNG-PROBE-SKIP] rva=0x{rva:X} {byteError}");
            Flush();
            return;
        }

        _rollRngBuf = Marshal.AllocHGlobal(CALC_ENTRY_BUFFER_SIZE);
        for (int i = 0; i < CALC_ENTRY_BUFFER_SIZE; i++)
            Marshal.WriteByte(_rollRngBuf, i, 0);

        string buf = $"0{_rollRngBuf:X}h";
        string[] asm =
        {
            "use64",
            "push rax",
            "push rbx",
            "push rsi",
            "pushfq",
            $"mov rax, {buf}",
            "mov ebx, dword [rax]",
            "add dword [rax], 1",
            "and ebx, 0x3F",
            "shl ebx, 4",
            "lea rsi, [rax + rbx + 16]",
            "mov rbx, [rsp+32]",          // return address (4 pushes deep) = the real caller site
            "mov qword [rsi], rbx",
            "mov dword [rsi+8], ecx",     // range
            "mov dword [rsi+12], edx",    // chance
            "popfq",
            "pop rsi",
            "pop rbx",
            "pop rax",
        };
        try
        {
            _rollRngHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line($"[RNG-PROBE-HOOK] rva=0x{rva:X} addr=0x{address:X} buf=0x{_rollRngBuf:X}");
        }
        catch (Exception ex)
        {
            Line($"[RNG-PROBE-FAILED] {ex.GetType().Name}: {ex.Message}");
        }
        Flush();
    }

    private int _rollRngSeen;
    private readonly Dictionary<long, (int Count, int Range, int Chance)> _rollRngCallers = new();
    private readonly Dictionary<long, long> _rollRngLastLogTick = new();
    private void CaptureRollRngProbeEvents(long nowTick)
    {
        if (_rollRngBuf == 0)
            return;
        int count = Marshal.ReadInt32(_rollRngBuf);
        if (count == _rollRngSeen)
            return;
        int fresh = count - _rollRngSeen;
        int start = Math.Max(_rollRngSeen, count - Math.Min(fresh, CALC_ENTRY_RING_SLOTS));
        var touched = new HashSet<long>();
        for (int n = start; n < count; n++)
        {
            nint slot = _rollRngBuf + 16 + ((n & (CALC_ENTRY_RING_SLOTS - 1)) * 16);
            long ret = Marshal.ReadInt64(slot);
            int range = Marshal.ReadInt32(slot + 8);
            int chance = Marshal.ReadInt32(slot + 12);
            _rollRngCallers.TryGetValue(ret, out var agg);
            _rollRngCallers[ret] = (agg.Count + 1, range, chance);
            touched.Add(ret);
        }
        long throttle = Stopwatch.Frequency;                 // 1 s per caller
        foreach (long ret in touched)
        {
            bool isNew = !_rollRngLastLogTick.TryGetValue(ret, out long last);
            if (!isNew && nowTick - last < throttle)
                continue;
            _rollRngLastLogTick[ret] = nowTick;
            var agg = _rollRngCallers[ret];
            long callerRva = ret - _moduleBase.ToInt64();
            Line($"[RNG] caller=0x{callerRva:X}{(isNew ? " NEW" : "")} count={agg.Count} last=(range={agg.Range}, chance={agg.Chance})");
        }
        _rollRngSeen = count;
        Flush();
    }

    // STAGED-BUNDLE PROBE (LT4) — the OUTPUT window. Hook at the sweep post-call 0x281F8A: the VM's
    // computeActionResult (0x309A44) has just staged the whole effect bundle onto the target, and the
    // engine has NOT applied it yet. The target index (unit index) is still on the stack at
    // [rbp+rbx-0x28] (rbx = the sweep loop index; both rbp and rbx are callee-saved across the call).
    // We log target+0x1C0 (evade kind) / +0x1C4 (staged dmg) / +0x1A8 (ailment) / +0x1D0 (apply mask)
    // / +0x1E5 (result flag), and OPTIONALLY overwrite each (gated on the target charId) to prove
    // OUTPUT control of status infliction and magic miss->hit without touching the VM roll.
    // Buffer: [0]u32 ring count, [4]forceCharId, [8]forceKind, [12]forceAilment, [16]forceMask,
    // [20]forceDmg (each -1 = leave alone), then 16-byte ring slots.
    private void InstallStagedBundleProbeIfEnabled(nint moduleBase)
    {
        if (!_settings.StagedBundleProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[BUNDLE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }
        int rva = _settings.StagedBundleProbeRva;
        nint address = moduleBase + rva;
        if (!ValidateExpectedBytes(address, "48 FF C3 48 83 FB 15", out string byteError))
        {
            Line($"[BUNDLE-SKIP] rva=0x{rva:X} {byteError}");
            Flush();
            return;
        }

        _stagedBundleBuf = Marshal.AllocHGlobal(STAGED_BUNDLE_BUFFER_SIZE);
        for (int i = 0; i < STAGED_BUNDLE_BUFFER_SIZE; i++)
            Marshal.WriteByte(_stagedBundleBuf, i, 0);
        Marshal.WriteInt32(_stagedBundleBuf + 4, _settings.StagedBundleForceTargetCharId);
        Marshal.WriteInt32(_stagedBundleBuf + 8, _settings.StagedBundleForceKind);
        Marshal.WriteInt32(_stagedBundleBuf + 12, _settings.StagedBundleForceAilment);
        Marshal.WriteInt32(_stagedBundleBuf + 16, _settings.StagedBundleForceApplyMask);
        Marshal.WriteInt32(_stagedBundleBuf + 20, _settings.StagedBundleForceDmg);
        Marshal.WriteInt32(_stagedBundleBuf + 24, _settings.StagedBundleForceResFlag);

        string buf = $"0{_stagedBundleBuf:X}h";
        string[] asm =
        {
            "use64",
            "push rax", "push rcx", "push rdx", "push r8", "push r9",
            "pushfq",
            "movzx eax, byte [rbp+rbx-0x28]",   // eax = target unit index (0xFF empties already skipped)
            "cmp eax, 0x40",
            "jae .done",                          // guard against a garbage index
            "mov rdx, 0141853CE0h",
            "mov rcx, rax",
            "shl rcx, 9",                         // *0x200
            "add rcx, rdx",                       // rcx = target unit ptr
            $"mov r8, {buf}",
            // ---- ring log ----
            "mov edx, dword [r8]",
            "add dword [r8], 1",
            "and edx, 0x3F",
            "shl edx, 4",
            "add rdx, r8",
            "add rdx, 28",                        // rdx = slot ptr (header = 28)
            "mov byte [rdx], al",                 // targetIdx
            "mov r9b, byte [rcx]",       "mov byte [rdx+1], r9b",       // target charId
            "mov r9b, byte [rcx+0x1C0]", "mov byte [rdx+2], r9b",       // evade kind
            "mov r9w, word [rcx+0x1C4]", "mov word [rdx+4], r9w",       // staged dmg
            "mov r9w, word [rcx+0x1A8]", "mov word [rdx+6], r9w",       // ailment
            "mov r9b, byte [rcx+0x1D0]", "mov byte [rdx+8], r9b",       // apply mask
            "mov r9b, byte [rcx+0x1E5]", "mov byte [rdx+9], r9b",       // result flag
            // ---- optional forcing, gated on charId ----
            "mov r9d, dword [r8+4]",              // forceCharId
            "cmp r9d, 0",
            "jl .done",                           // -1 => observe only
            "movzx eax, byte [rcx]",              // target charId
            "cmp r9d, eax",
            "jne .done",
            "mov r9d, dword [r8+8]",  "cmp r9d, 0", "jl .noKind", "mov byte [rcx+0x1C0], r9b", ".noKind:",
            "mov r9d, dword [r8+12]", "cmp r9d, 0", "jl .noAil",  "mov word [rcx+0x1A8], r9w", ".noAil:",
            "mov r9d, dword [r8+16]", "cmp r9d, 0", "jl .noMask", "mov byte [rcx+0x1D0], r9b", ".noMask:",
            "mov r9d, dword [r8+20]", "cmp r9d, 0", "jl .noDmg",  "mov word [rcx+0x1C4], r9w", ".noDmg:",
            "mov r9d, dword [r8+24]", "cmp r9d, 0", "jl .noRes",  "mov byte [rcx+0x1E5], r9b", ".noRes:",
            ".done:",
            "popfq",
            "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",
        };
        try
        {
            _stagedBundleHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line($"[BUNDLE-HOOK] rva=0x{rva:X} addr=0x{address:X} buf=0x{_stagedBundleBuf:X} " +
                 $"forceChar={FormatMaybeInt(_settings.StagedBundleForceTargetCharId)} " +
                 $"kind={FormatMaybeInt(_settings.StagedBundleForceKind)} ail={FormatMaybeInt(_settings.StagedBundleForceAilment)} " +
                 $"mask={FormatMaybeInt(_settings.StagedBundleForceApplyMask)} dmg={FormatMaybeInt(_settings.StagedBundleForceDmg)} " +
                 $"resFlag={FormatMaybeInt(_settings.StagedBundleForceResFlag)}");
        }
        catch (Exception ex)
        {
            Line($"[BUNDLE-FAILED] {ex.GetType().Name}: {ex.Message}");
        }
        Flush();
    }

    private int _stagedBundleSeen;
    private const int STAGED_BUNDLE_LOG_PER_POLL = 16;
    private void CaptureStagedBundleProbeEvents()
    {
        if (_stagedBundleBuf == 0)
            return;
        int count = Marshal.ReadInt32(_stagedBundleBuf);
        if (count == _stagedBundleSeen)
            return;
        int fresh = count - _stagedBundleSeen;
        int start = Math.Max(_stagedBundleSeen, count - Math.Min(fresh, STAGED_BUNDLE_RING_SLOTS));
        int logged = 0;
        for (int n = start; n < count && logged < STAGED_BUNDLE_LOG_PER_POLL; n++, logged++)
        {
            nint slot = _stagedBundleBuf + STAGED_BUNDLE_HEADER + ((n & (STAGED_BUNDLE_RING_SLOTS - 1)) * 16);
            int targetIdx = Marshal.ReadByte(slot);
            int charId = Marshal.ReadByte(slot + 1);
            int kind = Marshal.ReadByte(slot + 2);
            int dmg = (ushort)Marshal.ReadInt16(slot + 4);
            int ailment = (ushort)Marshal.ReadInt16(slot + 6);
            int mask = Marshal.ReadByte(slot + 8);
            int resFlag = Marshal.ReadByte(slot + 9);
            Line($"[BUNDLE] n={n} targetIdx={targetIdx} charId=0x{charId:X2} kind=0x{kind:X2} " +
                 $"stagedDmg={dmg} ailment=0x{ailment:X4} applyMask=0x{mask:X2} resFlag=0x{resFlag:X2}");
        }
        if (count - start > logged)
            Line($"[BUNDLE-BULK] +{count - start - logged} more this poll (total={count})");
        _stagedBundleSeen = count;
        Flush();
    }

    private int _magicRollSeen;
    private int _statusRollSeen;
    // REACTION CHANCE CONTROL — the 4 real-code Brave-gate roll sites (reaction trigger% = defender
    // Brave; canon Phase A: Blade Grasp / Hamedo / Arrow Guard / Catch / Counter...). Each site is
    // `mov ecx,0x64; movzx edx,[def+0x2B]; call 0x278EE0` and arms the reaction when eax==0, so the
    // trigger probability IS edx: forced 0 = reactions NEVER arm (suppress), 100 = ALWAYS (force).
    // The only combat roll in real code (LT3b) — hook the call sites and override edx.
    private static readonly (int Rva, string Expected, string Tag)[] ReactionRollSites =
    {
        (0x30BE86, "E8 55 D0 F6 FF", "R1"),
        (0x30BEDC, "E8 FF CF F6 FF", "R2"),
        (0x30BF32, "E8 A9 CF F6 FF", "R3"),
        (0x30BF72, "E8 69 CF F6 FF", "R4"),
    };

    private void InstallReactionChanceControlIfEnabled(nint moduleBase)
    {
        if (!_settings.ReactionChanceControlEnabled)
            return;
        _reactionRollBuf = Marshal.AllocHGlobal(64);
        for (int i = 0; i < 64; i++)
            Marshal.WriteByte(_reactionRollBuf, i, 0);
        for (int i = 0; i < ReactionRollSites.Length; i++)
        {
            var site = ReactionRollSites[i];
            InstallChanceHook(moduleBase, site.Rva, site.Expected, _reactionRollBuf + i * 16,
                _settings.ReactionChanceForcedChance, $"REACTROLL-{site.Tag}", h => _reactionChanceHooks.Add(h));
        }
    }

    // EVADE RECORD OVERRIDE — the definitive equipment-evade runtime lever (RE 2026-07-03,
    // work/dcl-shield-evade-read-path.md). The preview/roll do NOT read the unit's evade bytes live:
    // 3 combat-input record builders pack them into a separate record at action SETUP — class 1:1
    // (record+0x44 = unit+0x4B) but shield/accessory as a DERIVED MAX (record+0x46 =
    // MAX(unit+0x4A, unit+0x49); record+0x50 = MAX(unit+0x4D, unit+0x4E)) — which is why a late
    // unit-byte stamp is ignored for shield (LT5-A2 fail). Fix: ExecuteFirst at each packed STORE
    // (`mov [rec+off], ax`) injecting `mov eax, VALUE` so the engine's own store writes OUR value.
    // eax is reloaded before every subsequent use in all 3 builders, so the clobber is free.
    private static readonly (int Rva, string Expected, string Tag)[] EvadeRecordStoreSites =
    {
        (0x284BEC, "66 89 43 44", "B1+44"), (0x284C00, "66 89 43 46", "B1+46"), (0x284C28, "66 89 43 50", "B1+50"),
        (0x3602D6, "66 89 43 44", "B2+44"), (0x3602EA, "66 89 43 46", "B2+46"), (0x360313, "66 89 43 50", "B2+50"),
        (0x396468, "66 89 47 44", "B3+44"), (0x39647C, "66 89 47 46", "B3+46"), (0x3964A5, "66 89 47 50", "B3+50"),
    };

    private void InstallEvadeRecordOverrideIfEnabled(nint moduleBase)
    {
        if (!_settings.EvadeRecordOverrideEnabled)
            return;
        if (_hooks is null)
        {
            Line("[EVADE-RECORD-SKIP] no IReloadedHooks");
            Flush();
            return;
        }
        foreach (var site in EvadeRecordStoreSites)
        {
            int value = site.Tag[^2..] switch
            {
                "44" => _settings.EvadeRecordOverride44,
                "46" => _settings.EvadeRecordOverride46,
                "50" => _settings.EvadeRecordOverride50,
                _ => -1,
            };
            if (value < 0)
                continue;
            nint address = moduleBase + site.Rva;
            if (!ValidateExpectedBytes(address, site.Expected, out string byteError))
            {
                Line($"[EVADE-RECORD-SKIP] site={site.Tag} rva=0x{site.Rva:X} {byteError}");
                continue;
            }
            try
            {
                string[] asm = { "use64", $"mov eax, {value & 0xFF}" };
                _evadeRecordHooks.Add(_hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate());
                Line($"[EVADE-RECORD-HOOK] site={site.Tag} rva=0x{site.Rva:X} force={value & 0xFF}");
            }
            catch (Exception ex)
            {
                Line($"[EVADE-RECORD-FAILED] site={site.Tag} rva=0x{site.Rva:X} {ex.GetType().Name}: {ex.Message}");
            }
        }
        Flush();
    }

    private readonly int[] _reactionRollSeen = new int[4];
    private void CaptureReactionRollEvents()
    {
        if (_reactionRollBuf == 0)
            return;
        for (int i = 0; i < ReactionRollSites.Length; i++)
        {
            int count = Marshal.ReadInt32(_reactionRollBuf + i * 16);
            if (count == _reactionRollSeen[i])
                continue;
            Line($"[REACTROLL-{ReactionRollSites[i].Tag}] fires={count} " +
                 $"lastNaturalBrave={Marshal.ReadInt32(_reactionRollBuf + i * 16 + 4)} " +
                 $"forced={FormatMaybeInt(_settings.ReactionChanceForcedChance)}");
            _reactionRollSeen[i] = count;
            Flush();
        }
    }

    private void CaptureLt3RollProbeEvents()
    {
        if (_lt3RollBuf == 0)
            return;
        int magicCount = Marshal.ReadInt32(_lt3RollBuf + 16);
        if (magicCount != _magicRollSeen)
        {
            Line($"[MAGICROLL] fires={magicCount} lastNaturalChance={Marshal.ReadInt32(_lt3RollBuf + 20)} " +
                 $"forced={FormatMaybeInt(_settings.MagicAccuracyForcedChance)}");
            _magicRollSeen = magicCount;
            Flush();
        }
        int statusCount = Marshal.ReadInt32(_lt3RollBuf + 32);
        if (statusCount != _statusRollSeen)
        {
            Line($"[STATUSROLL] fires={statusCount} lastNaturalChance={Marshal.ReadInt32(_lt3RollBuf + 36)} " +
                 $"forced={FormatMaybeInt(_settings.StatusChanceForcedChance)}");
            _statusRollSeen = statusCount;
            Flush();
        }
    }

    private void InstallPreClampDamageRewriteIfEnabled(nint moduleBase)
    {
        // DclPipelineEnabled is a one-switch pipeline: it implies this hook (its delivery lever).
        if (!_settings.PreClampDamageRewriteEnabled && !_settings.DclPipelineEnabled)
            return;
        if (_hooks is null)
        {
            Line("[PRECLAMP-REWRITE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        if (_settings.PreClampDamageRewriteForcedDebit < 0 &&
            _settings.PreClampDamageRewriteForcedCredit < 0 &&
            !_settings.PreClampDamageRewriteLogOnly &&
            !_settings.PreClampFormulaPlanEnabled &&
            !IsPreClampManagedCallbackEnabled(_settings))
        {
            Line("[PRECLAMP-REWRITE-SKIP] no forced debit/credit configured");
            Flush();
            return;
        }

        _preClampDamageRewriteBuf = Marshal.AllocHGlobal(PRECLAMP_BUFFER_SIZE);
        for (int i = 0; i < PRECLAMP_BUFFER_SIZE; i++)
            Marshal.WriteByte(_preClampDamageRewriteBuf, i, 0);

        nint address = moduleBase + _settings.PreClampDamageRewriteRva;
        if (!ValidateExpectedBytes(address, _settings.PreClampDamageRewriteExpectedBytes, out string byteError))
        {
            Line($"[PRECLAMP-REWRITE-SKIP] rva=0x{_settings.PreClampDamageRewriteRva:X} {byteError}");
            Flush();
            return;
        }

        try
        {
            if (IsPreClampManagedCallbackEnabled(_settings))
            {
                _preClampManagedCallback = PreClampManagedCallbackImpl;
                _preClampManagedCallbackWrapper = _hooks.CreateReverseWrapper(_preClampManagedCallback);
                if (_preClampManagedCallbackWrapper is null || _preClampManagedCallbackWrapper.WrapperPointer == 0)
                {
                    Line("[PRECLAMP-REWRITE-SKIP] managed callback reverse wrapper unavailable");
                    Flush();
                    return;
                }
            }

            var asm = BuildPreClampDamageRewriteAsm();
            _preClampDamageRewriteHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line(
                $"[PRECLAMP-REWRITE-HOOK] rva=0x{_settings.PreClampDamageRewriteRva:X} addr=0x{address:X} " +
                $"targetId={FormatMaybeByte(_settings.PreClampDamageRewriteTargetCharId)} " +
                $"expectedDebit={FormatMaybeInt(_settings.PreClampDamageRewriteExpectedDebit)} " +
                $"expectedCredit={FormatMaybeInt(_settings.PreClampDamageRewriteExpectedCredit)} " +
                $"forcedDebit={FormatMaybeInt(_settings.PreClampDamageRewriteForcedDebit)} " +
                $"forcedCredit={FormatMaybeInt(_settings.PreClampDamageRewriteForcedCredit)} " +
                $"maxWrites={_settings.PreClampDamageRewriteMaxWrites} logOnly={(_settings.PreClampDamageRewriteLogOnly ? 1 : 0)} " +
                $"planEnabled={(_settings.PreClampFormulaPlanEnabled ? 1 : 0)} planSlots={PlanSlotCount} " +
                $"managedCallback={(IsPreClampManagedCallbackEnabled(_settings) ? 1 : 0)} " +
                $"managedForcedDebit={FormatMaybeInt(_settings.PreClampManagedCallbackForcedDebit)} " +
                $"managedActorFormula={(_settings.PreClampManagedCallbackActorFormulaEnabled ? 1 : 0)} " +
                $"dclPipeline={(_settings.DclPipelineEnabled ? 1 : 0)} dclFormula={(string.IsNullOrWhiteSpace(_settings.DclDamageFormula) ? "off" : "on")}");
        }
        catch (Exception ex)
        {
            Line($"[PRECLAMP-REWRITE-FAILED] rva=0x{_settings.PreClampDamageRewriteRva:X} {ex.GetType().Name}: {ex.Message}");
        }

        Flush();
    }

    private string[] BuildPreClampDamageRewriteAsm()
    {
        string buf = $"0{_preClampDamageRewriteBuf:X}h";
        int maxWrites = Math.Max(1, _settings.PreClampDamageRewriteMaxWrites);
        int minHp = Math.Clamp(_settings.PreClampDamageRewriteMinHp, 0, 9999);
        int maxHp = Math.Clamp(_settings.PreClampDamageRewriteMaxHp, minHp, 9999);
        int forcedDebit = ClampInt16Immediate(_settings.PreClampDamageRewriteForcedDebit);
        int forcedCredit = ClampInt16Immediate(_settings.PreClampDamageRewriteForcedCredit);
        bool forceDebit = _settings.PreClampDamageRewriteForcedDebit >= 0 && !_settings.PreClampDamageRewriteLogOnly;
        bool forceCredit = _settings.PreClampDamageRewriteForcedCredit >= 0 && !_settings.PreClampDamageRewriteLogOnly;
        bool staticEnabled = _settings.PreClampDamageRewriteForcedDebit >= 0 ||
                             _settings.PreClampDamageRewriteForcedCredit >= 0 ||
                             _settings.PreClampDamageRewriteLogOnly;
        bool planEnabled = _settings.PreClampFormulaPlanEnabled;
        bool planWrites = planEnabled && !_settings.PreClampDamageRewriteLogOnly;
        bool managedCallbackEnabled = IsPreClampManagedCallbackEnabled(_settings) &&
                                      _preClampManagedCallbackWrapper is not null &&
                                      _preClampManagedCallbackWrapper.WrapperPointer != 0;
        int planSlots = PlanSlotCount;
        int flags = (_settings.PreClampDamageRewriteLogOnly ? 1 : 0) |
                    (forceDebit ? 2 : 0) |
                    (forceCredit ? 4 : 0);

        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "push rdx",
            "push r8",
            "push r9",
            "push r10",
            "push r11",
            "pushfq",
            $"mov rax, {buf}",
            "test rdi, rdi",
            "jz .done",
            "test rbp, rbp",
            "jz .done",
        };

        void AddManagedCallback()
        {
            if (!managedCallbackEnabled || _preClampManagedCallbackWrapper is null)
                return;

            string callback = $"0{_preClampManagedCallbackWrapper.WrapperPointer:X}h";
            asm.AddRange(
            [
                // rcx/rdi = target unit, rdx/rbp = staged state record, r8 = hook-save stack,
                // r9 = pre-clamp buffer. The callback returns a forced debit in rax, or -1 to skip.
                "mov rcx, rdi",
                "mov rdx, rbp",
                "mov r8, rsp",
                $"mov r9, {buf}",
                "sub rsp, 80h",
                "movdqu [rsp+20h], xmm0",
                "movdqu [rsp+30h], xmm1",
                "movdqu [rsp+40h], xmm2",
                "movdqu [rsp+50h], xmm3",
                "movdqu [rsp+60h], xmm4",
                "movdqu [rsp+70h], xmm5",
                $"mov r11, {callback}",
                "call r11",
                "movdqu xmm0, [rsp+20h]",
                "movdqu xmm1, [rsp+30h]",
                "movdqu xmm2, [rsp+40h]",
                "movdqu xmm3, [rsp+50h]",
                "movdqu xmm4, [rsp+60h]",
                "movdqu xmm5, [rsp+70h]",
                "add rsp, 80h",
                "mov r10, rax",
                $"mov rax, {buf}",
                $"add dword [rax+{P_MANAGED_CALLBACKS}], 1",
                "cmp r10d, -1",
                "je .managed_callback_skip_debit_write",
                "mov [rbp+6], r10w",
                "mov word [rbp+8], 0",
                ".managed_callback_skip_debit_write:",
            ]);
        }

        void AddRecordEventFromStatic()
        {
            asm.AddRange(
            [
                $"mov ecx, [rax+{P_COUNT}]",
                "add ecx, 1",
                $"mov [rax+{P_COUNT}], ecx",
                "mov r8d, ecx",
                $"and r8d, {PRECLAMP_RING_MASK}",
                $"imul r8, r8, {PRECLAMP_SLOT_SIZE}",
                "add r8, rax",
                $"add r8, {P_EVENTS}",
                $"mov dword [r8+{P_SEQ}], ecx",
                $"mov [r8+{P_UNIT}], rdi",
                $"mov [r8+{P_STATE}], rbp",
                "movzx edx, byte [rdi]",
                $"mov dword [r8+{P_ID}], edx",
                "movzx edx, byte [rdi+4]",
                $"mov dword [r8+{P_TEAM}], edx",
                "movzx edx, word [rdi+30h]",
                $"mov dword [r8+{P_HP}], edx",
                "movzx edx, word [rdi+32h]",
                $"mov dword [r8+{P_MAX_HP}], edx",
                "movsx edx, word [rbp+6]",
                $"mov dword [r8+{P_OLD_DEBIT}], edx",
                "movsx edx, word [rbp+8]",
                $"mov dword [r8+{P_OLD_CREDIT}], edx",
                $"mov dword [r8+{P_FORCED_DEBIT}], {forcedDebit}",
                $"mov dword [r8+{P_FORCED_CREDIT}], {forcedCredit}",
                $"mov dword [r8+{P_FLAGS}], {flags}",
                $"mov dword [r8+{P_ACTION}], -1",
            ]);
            AddStorePreClampRegisters();
        }

        void AddRecordEventFromPlan()
        {
            asm.AddRange(
            [
                $"mov ecx, [rax+{P_COUNT}]",
                "add ecx, 1",
                $"mov [rax+{P_COUNT}], ecx",
                $"mov r11d, [rax+{P_PLAN_WRITES}]",
                "add r11d, 1",
                $"mov [rax+{P_PLAN_WRITES}], r11d",
                "mov r8d, ecx",
                $"and r8d, {PRECLAMP_RING_MASK}",
                $"imul r8, r8, {PRECLAMP_SLOT_SIZE}",
                "add r8, rax",
                $"add r8, {P_EVENTS}",
                $"mov dword [r8+{P_SEQ}], ecx",
                $"mov [r8+{P_UNIT}], rdi",
                $"mov [r8+{P_STATE}], rbp",
                "movzx edx, byte [rdi]",
                $"mov dword [r8+{P_ID}], edx",
                "movzx edx, byte [rdi+4]",
                $"mov dword [r8+{P_TEAM}], edx",
                "movzx edx, word [rdi+30h]",
                $"mov dword [r8+{P_HP}], edx",
                "movzx edx, word [rdi+32h]",
                $"mov dword [r8+{P_MAX_HP}], edx",
                "movsx edx, word [rbp+6]",
                $"mov dword [r8+{P_OLD_DEBIT}], edx",
                "movsx edx, word [rbp+8]",
                $"mov dword [r8+{P_OLD_CREDIT}], edx",
                $"mov edx, [r9+{PLAN_FORCED_DEBIT}]",
                $"mov dword [r8+{P_FORCED_DEBIT}], edx",
                $"mov edx, [r9+{PLAN_FORCED_CREDIT}]",
                $"mov dword [r8+{P_FORCED_CREDIT}], edx",
                $"mov edx, [r9+{PLAN_FLAGS}]",
                $"mov dword [r8+{P_FLAGS}], edx",
                $"mov edx, [r9+{PLAN_ACTION}]",
                $"mov dword [r8+{P_ACTION}], edx",
            ]);
            AddStorePreClampRegisters();
        }

        void AddStorePreClampRegisters()
        {
            asm.AddRange(
            [
                // Original volatile registers are on the hook-save stack:
                // push rax, rcx, rdx, r8, r9, r10, r11, pushfq.
                $"mov r11, [rsp+56]",
                $"mov [r8+{P_REGS + 0 * 8}], r11",
                $"mov [r8+{P_REGS + 1 * 8}], rbx",
                $"mov r11, [rsp+48]",
                $"mov [r8+{P_REGS + 2 * 8}], r11",
                $"mov r11, [rsp+40]",
                $"mov [r8+{P_REGS + 3 * 8}], r11",
                $"mov [r8+{P_REGS + 4 * 8}], rsi",
                $"mov [r8+{P_REGS + 5 * 8}], rdi",
                $"mov [r8+{P_REGS + 6 * 8}], rbp",
                "lea r11, [rsp+64]",
                $"mov [r8+{P_REGS + 7 * 8}], r11",
                $"mov r11, [rsp+32]",
                $"mov [r8+{P_REGS + 8 * 8}], r11",
                $"mov r11, [rsp+24]",
                $"mov [r8+{P_REGS + 9 * 8}], r11",
                $"mov r11, [rsp+16]",
                $"mov [r8+{P_REGS + 10 * 8}], r11",
                $"mov r11, [rsp+8]",
                $"mov [r8+{P_REGS + 11 * 8}], r11",
                $"mov [r8+{P_REGS + 12 * 8}], r12",
                $"mov [r8+{P_REGS + 13 * 8}], r13",
                $"mov [r8+{P_REGS + 14 * 8}], r14",
                $"mov [r8+{P_REGS + 15 * 8}], r15",
            ]);

            for (int offset = 0; offset < PRECLAMP_STACK_DUMP_SIZE; offset += 8)
            {
                asm.Add($"mov r11, [rsp+{64 + offset}]");
                asm.Add($"mov [r8+{P_STACK_DUMP + offset}], r11");
            }

            for (int offset = 0; offset < PRECLAMP_STATE_DUMP_SIZE; offset += 8)
            {
                asm.Add($"mov r11, [rbp+{offset}]");
                asm.Add($"mov [r8+{P_STATE_DUMP + offset}], r11");
            }

            for (int offset = 0; offset < PRECLAMP_UNIT_DUMP_SIZE; offset += 8)
            {
                asm.Add($"mov r11, [rdi+{offset}]");
                asm.Add($"mov [r8+{P_UNIT_DUMP + offset}], r11");
            }
        }

        AddManagedCallback();

        if (planEnabled)
        {
            asm.AddRange(
            [
                "xor r10d, r10d",
                ".plan_loop:",
                $"cmp r10d, {planSlots}",
                "jge .static_checks",
                "mov r9, rax",
                $"add r9, {P_PLAN_TABLE}",
                "mov r11d, r10d",
                $"imul r11, r11, {PRECLAMP_PLAN_SLOT_SIZE}",
                "add r9, r11",
                $"cmp dword [r9+{PLAN_ACTIVE}], 1",
                "jne .plan_next",
                $"mov r11, [r9+{PLAN_TARGET}]",
                "cmp r11, rdi",
                "jne .plan_next",
                $"mov edx, [r9+{PLAN_EXPECTED_HP}]",
                "cmp edx, -1",
                "je .plan_skip_expected_hp",
                "movzx edx, word [rdi+30h]",
                $"cmp edx, [r9+{PLAN_EXPECTED_HP}]",
                "jne .plan_next",
                ".plan_skip_expected_hp:",
                $"mov edx, [r9+{PLAN_EXPECTED_MAX_HP}]",
                "cmp edx, -1",
                "je .plan_skip_expected_max_hp",
                "movzx edx, word [rdi+32h]",
                $"cmp edx, [r9+{PLAN_EXPECTED_MAX_HP}]",
                "jne .plan_next",
                ".plan_skip_expected_max_hp:",
                $"mov edx, [r9+{PLAN_EXPECTED_DEBIT}]",
                $"cmp edx, {PlanExpectedAny}",
                "je .plan_skip_expected_debit",
                $"cmp edx, {PlanExpectedPositiveDebit}",
                "jne .plan_exact_expected_debit",
                "movsx edx, word [rbp+6]",
                "cmp edx, 1",
                "jl .plan_next",
                "jmp .plan_skip_expected_debit",
                ".plan_exact_expected_debit:",
                "movsx edx, word [rbp+6]",
                $"cmp edx, [r9+{PLAN_EXPECTED_DEBIT}]",
                "jne .plan_next",
                ".plan_skip_expected_debit:",
                $"mov edx, [r9+{PLAN_EXPECTED_CREDIT}]",
                $"cmp edx, {PlanExpectedAny}",
                "je .plan_skip_expected_credit",
                "movsx edx, word [rbp+8]",
                $"cmp edx, [r9+{PLAN_EXPECTED_CREDIT}]",
                "jne .plan_next",
                ".plan_skip_expected_credit:",
                $"mov edx, [r9+{PLAN_WRITE_COUNT}]",
                $"cmp edx, [r9+{PLAN_MAX_WRITES}]",
                "jge .plan_deactivate",
                "add edx, 1",
                $"mov [r9+{PLAN_WRITE_COUNT}], edx",
            ]);
            AddRecordEventFromPlan();
            if (planWrites)
            {
                asm.AddRange(
                [
                    $"mov edx, [r9+{PLAN_FORCED_DEBIT}]",
                    "cmp edx, -1",
                    "je .plan_skip_debit_write",
                    "mov [rbp+6], dx",
                    ".plan_skip_debit_write:",
                    $"mov edx, [r9+{PLAN_FORCED_CREDIT}]",
                    "cmp edx, -1",
                    "je .plan_skip_credit_write",
                    "mov [rbp+8], dx",
                    ".plan_skip_credit_write:",
                ]);
            }
            asm.AddRange(
            [
                $"mov edx, [r9+{PLAN_WRITE_COUNT}]",
                $"cmp edx, [r9+{PLAN_MAX_WRITES}]",
                "jl .done",
                ".plan_deactivate:",
                $"mov dword [r9+{PLAN_ACTIVE}], 0",
                "jmp .done",
                ".plan_next:",
                "inc r10d",
                "jmp .plan_loop",
            ]);
        }

        asm.Add(".static_checks:");
        if (!staticEnabled)
        {
            asm.Add("jmp .done");
        }
        else
        {
            asm.Add($"mov ecx, [rax+{P_STATIC_WRITES}]");
            asm.Add($"cmp ecx, {maxWrites}");
            asm.Add("jge .done");

            if (_settings.PreClampDamageRewriteTargetCharId >= 0)
            {
                asm.Add("movzx edx, byte [rdi]");
                asm.Add($"cmp edx, {_settings.PreClampDamageRewriteTargetCharId}");
                asm.Add("jne .done");
            }

            if (_settings.PreClampDamageRewriteTargetTeam >= 0)
            {
                asm.Add("movzx edx, byte [rdi+4]");
                asm.Add($"cmp edx, {_settings.PreClampDamageRewriteTargetTeam}");
                asm.Add("jne .done");
            }

            asm.Add("movzx edx, word [rdi+30h]");
            asm.Add($"cmp edx, {minHp}");
            asm.Add("jl .done");
            asm.Add($"cmp edx, {maxHp}");
            asm.Add("jg .done");

            if (_settings.PreClampDamageRewriteExpectedDebit >= 0)
            {
                asm.Add("movsx edx, word [rbp+6]");
                asm.Add($"cmp edx, {_settings.PreClampDamageRewriteExpectedDebit}");
                asm.Add("jne .done");
            }

            if (_settings.PreClampDamageRewriteExpectedCredit >= 0)
            {
                asm.Add("movsx edx, word [rbp+8]");
                asm.Add($"cmp edx, {_settings.PreClampDamageRewriteExpectedCredit}");
                asm.Add("jne .done");
            }

            asm.Add("add ecx, 1");
            asm.Add($"mov [rax+{P_STATIC_WRITES}], ecx");
            AddRecordEventFromStatic();

            if (forceDebit)
                asm.Add($"mov word [rbp+6], {forcedDebit}");
            if (forceCredit)
                asm.Add($"mov word [rbp+8], {forcedCredit}");
        }

        asm.AddRange([".done:", "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax"]);
        return asm.ToArray();
    }

    // OBSERVE-ONLY result/animation selector probe. Hooks the result selector prologue (ExecuteFirst)
    // and records: the evade-type byte the caller passes in cl, the actor object (r8), the actor's
    // result record [r8+RecordUnitOffset], a byte window of that record, and a small selector-frame
    // register/stack snapshot. The context snapshot is for no-HP outcomes where pre-clamp never fires.
    private void InstallResultSelectorProbeIfEnabled(nint moduleBase)
    {
        if (!_settings.ResultSelectorProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[SELECTOR-PROBE-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _resultSelectorProbeBuf = Marshal.AllocHGlobal(SELECTOR_BUFFER_SIZE);
        for (int i = 0; i < SELECTOR_BUFFER_SIZE; i++)
            Marshal.WriteByte(_resultSelectorProbeBuf, i, 0);

        nint address = moduleBase + _settings.ResultSelectorProbeRva;
        if (!ValidateExpectedBytes(address, _settings.ResultSelectorProbeExpectedBytes, out string byteError))
        {
            Line($"[SELECTOR-PROBE-SKIP] rva=0x{_settings.ResultSelectorProbeRva:X} {byteError}");
            Flush();
            return;
        }

        try
        {
            var asm = BuildResultSelectorProbeAsm();
            _resultSelectorProbeHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line(
                $"[SELECTOR-PROBE-HOOK] rva=0x{_settings.ResultSelectorProbeRva:X} addr=0x{address:X} " +
                $"actorReg={_settings.ResultSelectorProbeActorRegister} recordUnitOffset=0x{ResultSelectorRecordUnitOffset:X} " +
                $"dumpBytes={ResultSelectorRecordDumpBytes} ctxRegs={SELECTOR_CONTEXT_REG_COUNT} ctxStack={SELECTOR_CONTEXT_STACK_SLOTS} " +
                $"maxLogs={_settings.ResultSelectorProbeMaxLogs} " +
                $"expected={_settings.ResultSelectorProbeExpectedBytes} {DescribeSelectorControl()}");
        }
        catch (Exception ex)
        {
            Line($"[SELECTOR-PROBE-FAILED] rva=0x{_settings.ResultSelectorProbeRva:X} {ex.GetType().Name}: {ex.Message}");
        }

        Flush();
    }

    // Clamp to the 8-byte-aligned dump window the slot can hold.
    private int ResultSelectorRecordDumpBytes
        => Math.Clamp(_settings.ResultSelectorProbeRecordDumpBytes, 0, SELECTOR_RECORD_DUMP_MAX) & ~7;

    private int ResultSelectorRecordUnitOffset
        => Math.Clamp(_settings.ResultSelectorProbeRecordUnitOffset, 0, 0x4000);

    private string[] BuildResultSelectorProbeAsm()
    {
        string buf = $"0{_resultSelectorProbeBuf:X}h";
        int recordUnitOffset = ResultSelectorRecordUnitOffset;
        int dumpBytes = ResultSelectorRecordDumpBytes;

        // ExecuteFirst: at entry cl = evade-type, r8 = actor. We use rax (ring base) and r9 (slot/scratch);
        // both are saved/restored, and flags are preserved with pushfq/popfq so the original prologue runs
        // unchanged. cl is read from the saved rcx on the hook stack (so our pushes can't perturb it).
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "push r8",
            "push r9",
            "pushfq",
            $"mov rax, {buf}",
            // claim a sequence number and compute the ring slot into r9
            $"mov ecx, [rax+{S_COUNT}]",
            "add ecx, 1",
            $"mov [rax+{S_COUNT}], ecx",
            "mov r9d, ecx",
            $"and r9d, {SELECTOR_RING_MASK}",
            $"imul r9, r9, {SELECTOR_SLOT_SIZE}",
            "add r9, rax",
            $"add r9, {S_EVENTS}",
            // header: zero seq first (partial-slot guard), evade-type, actor, record
            $"mov dword [r9+{S_SEQ}], 0",
            // zero the control marker so a stale ring entry never reads as a control event
            $"mov dword [r9+{S_CTRL_INFO}], 0",
            // evade-type = saved cl (movzx from the byte of saved rcx on the hook stack at [rsp+24])
            "movzx ecx, byte [rsp+24]",
            $"mov dword [r9+{S_EVADE}], ecx",
            // actor = saved r8 (hook stack: pushfq=+0, r9=+8, r8=+16)
            "mov r8, [rsp+16]",
            $"mov [r9+{S_ACTOR}], r8",
            $"mov qword [r9+{S_RECORD}], 0",
            // selector-frame context snapshot. Original stack begins at rsp+40 after our pushes:
            // push rax, push rcx, push r8, push r9, pushfq.
            "mov rax, [rsp+32]",
            $"mov [r9+{S_CONTEXT_REGS + 0x00}], rax", // rax
            $"mov [r9+{S_CONTEXT_REGS + 0x08}], rbx",
            "mov rax, [rsp+24]",
            $"mov [r9+{S_CONTEXT_REGS + 0x10}], rax", // rcx
            $"mov [r9+{S_CONTEXT_REGS + 0x18}], rdx",
            $"mov [r9+{S_CONTEXT_REGS + 0x20}], rsi",
            $"mov [r9+{S_CONTEXT_REGS + 0x28}], rdi",
            $"mov [r9+{S_CONTEXT_REGS + 0x30}], rbp",
            "lea rax, [rsp+40]",
            $"mov [r9+{S_CONTEXT_REGS + 0x38}], rax", // original rsp
            "mov rax, [rsp+16]",
            $"mov [r9+{S_CONTEXT_REGS + 0x40}], rax", // r8
            "mov rax, [rsp+8]",
            $"mov [r9+{S_CONTEXT_REGS + 0x48}], rax", // r9
            $"mov [r9+{S_CONTEXT_REGS + 0x50}], r10",
            $"mov [r9+{S_CONTEXT_REGS + 0x58}], r11",
            $"mov [r9+{S_CONTEXT_REGS + 0x60}], r12",
            $"mov [r9+{S_CONTEXT_REGS + 0x68}], r13",
            $"mov [r9+{S_CONTEXT_REGS + 0x70}], r14",
            $"mov [r9+{S_CONTEXT_REGS + 0x78}], r15",
            // guard: skip record load when actor==0 or record==0
            "test r8, r8",
            "jz .selector_store_seq",
            $"mov r8, [r8+{recordUnitOffset}]",
            "test r8, r8",
            "jz .selector_store_seq",
            $"mov [r9+{S_RECORD}], r8",
            $"mov rax, {buf}",
        };

        for (int slot = 0; slot < SELECTOR_CONTEXT_STACK_SLOTS; slot++)
        {
            int stackOffset = 40 + (slot * 8);
            asm.Add($"mov rax, [rsp+{stackOffset}]");
            asm.Add($"mov [r9+{S_CONTEXT_STACK + (slot * 8)}], rax");
        }
        asm.Add($"mov rax, {buf}");

        // OPTIONAL guarded control write: with r8 = record (non-null), rax = ring base, r9 = slot base,
        // force the evade-type (and/or result-code) so the engine renders the outcome we choose. Emits
        // nothing when control is disabled (pure observe). All immediates baked from settings.
        asm.AddRange(BuildSelectorControlLines());

        // copy the record window [record+SELECTOR_RECORD_DUMP_BASE ..] into the slot (8 bytes at a time)
        for (int offset = 0; offset < dumpBytes; offset += 8)
        {
            asm.Add($"mov rax, [r8+{SELECTOR_RECORD_DUMP_BASE + offset}]");
            asm.Add($"mov [r9+{S_RECORD_DUMP + offset}], rax");
        }

        // publish the slot by writing the real sequence last (reload count from the ring base)
        asm.AddRange(
        [
            ".selector_store_seq:",
            $"mov rax, {buf}",
            $"mov ecx, [rax+{S_COUNT}]",
            $"mov [r9+{S_SEQ}], ecx",
            "popfq",
            "pop r9",
            "pop r8",
            "pop rcx",
            "pop rax",
        ]);
        return asm.ToArray();
    }

    // Builds the guarded control write injected into the selector hook. Register state at the
    // insertion point: r8 = result record (non-null), rax = ring base, r9 = slot base, ecx = free
    // scratch, and the natural evade-type (the selector's cl argument) is the saved rcx low byte at
    // [rsp+24]. We force BOTH that saved cl (so the live argument carries our value through the
    // epilogue's `pop rcx`) AND the persisted record field +0x1C0, covering whichever the engine
    // reads. Fail-closed: any guard miss jumps to .selector_ctrl_done and nothing is written.
    // Emits nothing at all when control is disabled (pure observe-only behavior is unchanged).
    private string[] BuildSelectorControlLines()
    {
        if (!_settings.ResultSelectorControlEnabled)
            return [];

        int maxWrites = Math.Clamp(_settings.ResultSelectorControlMaxWrites, 1, SELECTOR_RING_SIZE);
        int targetId = _settings.ResultSelectorControlTargetCharId;
        int matchEvade = _settings.ResultSelectorControlMatchEvadeType;
        int forceEvade = _settings.ResultSelectorControlForceEvadeType;
        int forceResult = _settings.ResultSelectorControlForceResultCode;
        bool logOnly = _settings.ResultSelectorControlLogOnly;

        // 255 (0xFF) is the "no value" sentinel in the per-slot marker; real evade-types are 0x00..0x06.
        int forceEvadeMark = forceEvade >= 0 ? (forceEvade & 0xFF) : 255;
        int forceResultMark = forceResult >= 0 ? (forceResult & 0xFF) : 255;

        const int recordEvadeOffset = 0x1C0;   // +0x1C0 evade-type (the animation lever)
        const int recordResultOffset = 0x1BE;  // +0x1BE staged-result-present (0 = evade/no-damage)
        const int savedClStackOffset = 24;     // saved rcx low byte on the hook stack (= cl argument)

        var c = new List<string>();
        if (!logOnly)
        {
            // MaxWrites cap (counts live writes only; persistent across the session)
            c.Add($"mov ecx, [rax+{S_CTRL_WRITES}]");
            c.Add($"cmp ecx, {maxWrites}");
            c.Add("jae .selector_ctrl_done");
        }
        if (targetId >= 0)
        {
            // record char id is at +0x00
            c.Add("movzx ecx, byte [r8]");
            c.Add($"cmp ecx, {targetId & 0xFF}");
            c.Add("jne .selector_ctrl_done");
        }
        if (matchEvade >= 0)
        {
            // only act when the engine's natural evade-type equals this (the saved cl argument)
            c.Add($"movzx ecx, byte [rsp+{savedClStackOffset}]");
            c.Add($"cmp ecx, {matchEvade & 0xFF}");
            c.Add("jne .selector_ctrl_done");
        }
        if (logOnly)
        {
            // dry-run: record intent, perform NO write, do not consume the write budget
            c.Add($"mov byte [r9+{S_CTRL_INFO}], 1");
            c.Add($"mov byte [r9+{S_CTRL_INFO + 1}], {forceEvadeMark}");
            c.Add($"mov byte [r9+{S_CTRL_INFO + 2}], {forceResultMark}");
        }
        else
        {
            // live: spend one write from the budget, then force the chosen fields
            c.Add($"mov ecx, [rax+{S_CTRL_WRITES}]");
            c.Add("add ecx, 1");
            c.Add($"mov [rax+{S_CTRL_WRITES}], ecx");
            if (forceEvade >= 0)
            {
                c.Add($"mov byte [r8+{recordEvadeOffset}], {forceEvade & 0xFF}");   // persist record evade-type
                c.Add($"mov byte [rsp+{savedClStackOffset}], {forceEvade & 0xFF}"); // and force the live cl argument
            }
            if (forceResult >= 0)
                c.Add($"mov byte [r8+{recordResultOffset}], {forceResult & 0xFF}");
            c.Add($"mov byte [r9+{S_CTRL_INFO}], 2");
            c.Add($"mov byte [r9+{S_CTRL_INFO + 1}], {forceEvadeMark}");
            c.Add($"mov byte [r9+{S_CTRL_INFO + 2}], {forceResultMark}");
        }
        c.Add(".selector_ctrl_done:");
        return c.ToArray();
    }

    // One-line control summary for the install log.
    private string DescribeSelectorControl()
    {
        if (!_settings.ResultSelectorControlEnabled)
            return "(control=off, observe-only)";
        static string Opt(int v) => v < 0 ? "any" : $"0x{v & 0xFF:X2}";
        string mode = _settings.ResultSelectorControlLogOnly ? "LOGONLY(dry-run)" : "LIVE";
        return
            $"(control={mode} max={_settings.ResultSelectorControlMaxWrites} " +
            $"target={(_settings.ResultSelectorControlTargetCharId < 0 ? "any" : $"0x{_settings.ResultSelectorControlTargetCharId & 0xFF:X2}")} " +
            $"match={Opt(_settings.ResultSelectorControlMatchEvadeType)} " +
            $"forceEvade={Opt(_settings.ResultSelectorControlForceEvadeType)} " +
            $"forceResult={Opt(_settings.ResultSelectorControlForceResultCode)})";
    }

    // EVADE-INPUT probe/control. Hooks RVA 0x30F49C (the last real instruction before the single VM
    // avoidance roll 0x30FA34; the target unit is in rbx). Optionally writes the target's evade INPUT
    // bytes (+0x46/+0x47 weapon, +0x4A/+0x4E shield, +0x4B class) just before the VM reads them, so the
    // engine's native roll produces our pre-decided outcome (all 0 => guaranteed hit; one source = 100 =>
    // that evade type). Logs before/after bytes so the write and its effect are visible. The roll itself
    // is virtualized, so whether the VM honors the write is exactly what the live test settles.
    private void InstallEvadeInputProbeIfEnabled(nint moduleBase)
    {
        if (!_settings.EvadeInputProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[EVADE-INPUT-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _evadeInputProbeBuf = Marshal.AllocHGlobal(EVADE_INPUT_BUFFER_SIZE);
        for (int i = 0; i < EVADE_INPUT_BUFFER_SIZE; i++)
            Marshal.WriteByte(_evadeInputProbeBuf, i, 0);

        nint address = moduleBase + _settings.EvadeInputProbeRva;
        if (!ValidateExpectedBytes(address, _settings.EvadeInputProbeExpectedBytes, out string byteError))
        {
            Line($"[EVADE-INPUT-SKIP] rva=0x{_settings.EvadeInputProbeRva:X} {byteError}");
            Flush();
            return;
        }

        try
        {
            var asm = BuildEvadeInputProbeAsm();
            _evadeInputProbeHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line(
                $"[EVADE-INPUT-HOOK] rva=0x{_settings.EvadeInputProbeRva:X} addr=0x{address:X} " +
                $"maxLogs={_settings.EvadeInputProbeMaxLogs} expected={_settings.EvadeInputProbeExpectedBytes} " +
                $"{DescribeEvadeInputControl()}");
        }
        catch (Exception ex)
        {
            Line($"[EVADE-INPUT-FAILED] rva=0x{_settings.EvadeInputProbeRva:X} {ex.GetType().Name}: {ex.Message}");
        }

        Flush();
    }

    private string[] BuildEvadeInputProbeAsm()
    {
        string buf = $"0{_evadeInputProbeBuf:X}h";

        // ExecuteFirst: at entry rbx = target unit record. We save/restore rax, rcx, r8, r9 and flags so
        // the original prologue (mov rdx,rbx; mov [rbx+0x41],cl; call 0x30FA34) runs unchanged after us.
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "push r8",
            "push r9",
            "pushfq",
            "test rbx, rbx",
            "jz .ei_done",
            $"mov rax, {buf}",
            // claim a sequence number and compute the ring slot into r9
            $"mov ecx, [rax+{EI_COUNT}]",
            "add ecx, 1",
            $"mov [rax+{EI_COUNT}], ecx",
            "mov r9d, ecx",
            $"and r9d, {EVADE_INPUT_RING_MASK}",
            $"imul r9, r9, {EVADE_INPUT_SLOT_SIZE}",
            "add r9, rax",
            $"add r9, {EI_EVENTS}",
            // header: zero seq first (partial-slot guard) and the control marker
            $"mov dword [r9+{EI_SEQ}], 0",
            $"mov dword [r9+{EI_CTRL}], 0",
            // target identity + BEFORE snapshot (rbx = target unit)
            "movzx ecx, byte [rbx]",
            $"mov [r9+{EI_ID}], ecx",
            "movzx ecx, word [rbx+30h]",
            $"mov [r9+{EI_HP}], ecx",
            $"mov [r9+{EI_TARGET}], rbx",
            "mov r8, [rbx+46h]",
            $"mov [r9+{EI_BEFORE}], r8",
            "movzx ecx, byte [rbx+4Eh]",
            $"mov [r9+{EI_BEFORE_4E}], ecx",
        };

        // optional guarded control write (forces the target's evade input bytes)
        asm.AddRange(BuildEvadeInputControlLines());

        // AFTER snapshot, then publish the slot by writing the real sequence last
        asm.AddRange(
        [
            "mov r8, [rbx+46h]",
            $"mov [r9+{EI_AFTER}], r8",
            "movzx ecx, byte [rbx+4Eh]",
            $"mov [r9+{EI_AFTER_4E}], ecx",
            $"mov rax, {buf}",
            $"mov ecx, [rax+{EI_COUNT}]",
            $"mov [r9+{EI_SEQ}], ecx",
            ".ei_done:",
            "popfq",
            "pop r9",
            "pop r8",
            "pop rcx",
            "pop rax",
        ]);
        return asm.ToArray();
    }

    // Guarded control write injected into the evade-input hook. Register state: rbx = target unit,
    // rax = ring base, r9 = slot base, ecx = free scratch. Fail-closed: any guard miss jumps to
    // .ei_ctrl_done and nothing is written. Emits nothing when control is disabled (pure observe).
    private string[] BuildEvadeInputControlLines()
    {
        if (!_settings.EvadeInputControlEnabled)
            return [];

        int maxWrites = Math.Clamp(_settings.EvadeInputControlMaxWrites, 1, EVADE_INPUT_RING_SIZE);
        int targetId = _settings.EvadeInputControlTargetCharId;
        bool logOnly = _settings.EvadeInputControlLogOnly;

        var c = new List<string>();
        if (!logOnly)
        {
            c.Add($"mov ecx, [rax+{EI_CTRL_WRITES}]");
            c.Add($"cmp ecx, {maxWrites}");
            c.Add("jae .ei_ctrl_done");
        }
        if (targetId >= 0)
        {
            c.Add("movzx ecx, byte [rbx]");
            c.Add($"cmp ecx, {targetId & 0xFF}");
            c.Add("jne .ei_ctrl_done");
        }
        if (logOnly)
        {
            c.Add($"mov byte [r9+{EI_CTRL}], 1");
        }
        else
        {
            c.Add($"mov ecx, [rax+{EI_CTRL_WRITES}]");
            c.Add("add ecx, 1");
            c.Add($"mov [rax+{EI_CTRL_WRITES}], ecx");
            if (_settings.EvadeInputForce46 >= 0) c.Add($"mov byte [rbx+46h], {_settings.EvadeInputForce46 & 0xFF}");
            if (_settings.EvadeInputForce47 >= 0) c.Add($"mov byte [rbx+47h], {_settings.EvadeInputForce47 & 0xFF}");
            if (_settings.EvadeInputForce4A >= 0) c.Add($"mov byte [rbx+4Ah], {_settings.EvadeInputForce4A & 0xFF}");
            if (_settings.EvadeInputForce4B >= 0) c.Add($"mov byte [rbx+4Bh], {_settings.EvadeInputForce4B & 0xFF}");
            if (_settings.EvadeInputForce4E >= 0) c.Add($"mov byte [rbx+4Eh], {_settings.EvadeInputForce4E & 0xFF}");
            c.Add($"mov byte [r9+{EI_CTRL}], 2");
        }
        c.Add(".ei_ctrl_done:");
        return c.ToArray();
    }

    private string DescribeEvadeInputControl()
    {
        if (!_settings.EvadeInputControlEnabled)
            return "(control=off, observe-only)";
        static string Opt(int v) => v < 0 ? "--" : $"0x{v & 0xFF:X2}";
        string mode = _settings.EvadeInputControlLogOnly ? "LOGONLY(dry-run)" : "LIVE";
        return
            $"(control={mode} max={_settings.EvadeInputControlMaxWrites} " +
            $"target={(_settings.EvadeInputControlTargetCharId < 0 ? "any" : $"0x{_settings.EvadeInputControlTargetCharId & 0xFF:X2}")} " +
            $"force[46={Opt(_settings.EvadeInputForce46)} 47={Opt(_settings.EvadeInputForce47)} " +
            $"4A={Opt(_settings.EvadeInputForce4A)} 4B={Opt(_settings.EvadeInputForce4B)} 4E={Opt(_settings.EvadeInputForce4E)}])";
    }

    // ── EVADE-COPIER OVERRIDE (airtight, race-free defender evade control) ──────────────────────
    // RE 2026-07-03 (work/dcl-evade-recompute-site.md + dcl-miss-block-parry-DEFINITIVE-2026-07-03.md):
    // the defender's Family-1 evade bytes (+0x46/47 weapon, +0x48/49 accessory, +0x4A/4E shield,
    // +0x4B class) have NO per-attack real-code writer — the VM reads them live from the struct at
    // roll time. Their ONLY real-code writers are 3 equip/refresh COPIERS that re-stamp them from
    // equipment on state edges; that async re-stamp is what beat the 20ms EvadeOverride poll ~50% of
    // the time. Fix: detour each copier's TAIL (after its equipment copy, unit ptr still live) and
    // over-stamp our values. Because these own every legit path that changes the bytes, the value
    // persists to the VM roll with no race — retires the poll. all=0 ⇒ forced HIT; one source high ⇒
    // that avoid type. Hook sites are fixed RE discoveries (not user-tunable); values/target are settings.
    // NOTE: former site A (0x59F93C, fn 0x59F550) REMOVED 2026-07-03 — RE follow-up proved that fn is
    // HID gamepad enumeration (HidP_GetValueCaps consumers), a byte-scan false positive; hooking it
    // stamped a HIDP_VALUE_CAPS heap array, not units. Equipment evade is table-derived anyway
    // (see ItemTableEvadeZero).
    private static readonly (int Rva, string Reg, string Expected, string Tag)[] EvadeCopierSites =
    {
        (0x285553, "rbx", "4C 8D 5C 24 60 49 8B 5B 10", "B/0x285394"),   // weapon-parry tail (pre-epilogue)
        (0x396757, "rsi", "48 8B D7 48 8B CE",          "C/0x3965B0"),   // weapon-parry twin (rsi callee-saved)
    };

    private void InstallEvadeCopierOverrideIfEnabled(nint moduleBase)
    {
        if (!_settings.EvadeCopierOverrideEnabled)
            return;
        if (_hooks is null)
        {
            Line("[EVADE-COPIER-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        foreach (var site in EvadeCopierSites)
        {
            nint address = moduleBase + site.Rva;
            if (!ValidateExpectedBytes(address, site.Expected, out string byteError))
            {
                Line($"[EVADE-COPIER-SKIP] site={site.Tag} rva=0x{site.Rva:X} {byteError}");
                continue;
            }
            try
            {
                var asm = BuildEvadeCopierAsm(site.Reg);
                _evadeCopierHooks.Add(_hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate());
                Line($"[EVADE-COPIER-HOOK] site={site.Tag} rva=0x{site.Rva:X} addr=0x{address:X} unit={site.Reg} {DescribeEvadeCopierOverride()}");
            }
            catch (Exception ex)
            {
                Line($"[EVADE-COPIER-FAILED] site={site.Tag} rva=0x{site.Rva:X} {ex.GetType().Name}: {ex.Message}");
            }
        }
        Flush();
    }

    // ExecuteFirst at a copier tail: `reg` = defender unit ptr (live). Force-stamp each configured
    // evade byte (value >= 0). Only flags are clobbered (via the optional charId filter), so we
    // pushfq/popfq and touch no GP register. Fail-open jump target = .ec_done (before popfq).
    private string[] BuildEvadeCopierAsm(string reg)
    {
        int target = _settings.EvadeCopierOverrideTargetCharId;
        (int Off, int Val)[] bytes =
        {
            (0x46, _settings.EvadeCopierOverride46), (0x47, _settings.EvadeCopierOverride47),
            (0x48, _settings.EvadeCopierOverride48), (0x49, _settings.EvadeCopierOverride49),
            (0x4A, _settings.EvadeCopierOverride4A), (0x4B, _settings.EvadeCopierOverride4B),
            (0x4C, _settings.EvadeCopierOverride4C), (0x4D, _settings.EvadeCopierOverride4D),
            (0x4E, _settings.EvadeCopierOverride4E),
        };
        var asm = new List<string> { "use64", "pushfq" };
        if (target >= 0)
        {
            asm.Add($"cmp byte [{reg}], {target & 0xFF}");
            asm.Add("jne .ec_done");
        }
        foreach (var (off, val) in bytes)
            if (val >= 0)
                asm.Add($"mov byte [{reg}+{off:X}h], {val & 0xFF}");
        asm.Add(".ec_done:");
        asm.Add("popfq");
        return asm.ToArray();
    }

    private string DescribeEvadeCopierOverride()
    {
        static string Opt(int v) => v < 0 ? "--" : $"0x{v & 0xFF:X2}";
        string tgt = _settings.EvadeCopierOverrideTargetCharId < 0
            ? "any" : $"0x{_settings.EvadeCopierOverrideTargetCharId & 0xFF:X2}";
        return
            $"(target={tgt} force[46={Opt(_settings.EvadeCopierOverride46)} 47={Opt(_settings.EvadeCopierOverride47)} " +
            $"48={Opt(_settings.EvadeCopierOverride48)} 49={Opt(_settings.EvadeCopierOverride49)} " +
            $"4A={Opt(_settings.EvadeCopierOverride4A)} 4B={Opt(_settings.EvadeCopierOverride4B)} " +
            $"4C={Opt(_settings.EvadeCopierOverride4C)} 4D={Opt(_settings.EvadeCopierOverride4D)} " +
            $"4E={Opt(_settings.EvadeCopierOverride4E)}])";
    }

    private void CaptureEvadeInputProbeEvents(long nowTick)
    {
        if (_evadeInputProbeBuf == 0 || !_settings.EvadeInputProbeEnabled)
            return;

        int count = Marshal.ReadInt32(_evadeInputProbeBuf, EI_COUNT);
        if (count == _lastEvadeInputProbeSequence)
            return;

        bool needsFlush = false;
        int start = _lastEvadeInputProbeSequence + 1;
        if (count - _lastEvadeInputProbeSequence > EVADE_INPUT_RING_SIZE)
        {
            int lost = count - _lastEvadeInputProbeSequence - EVADE_INPUT_RING_SIZE;
            Line($"[EVADE-INPUT-LOST last={_lastEvadeInputProbeSequence} current={count} lost={lost}]");
            start = count - EVADE_INPUT_RING_SIZE + 1;
            needsFlush = true;
        }

        int maxLogs = Math.Clamp(_settings.EvadeInputProbeMaxLogs, 0, 100_000);
        for (int sequence = start; sequence <= count; sequence++)
        {
            int slot = EI_EVENTS + ((sequence & EVADE_INPUT_RING_MASK) * EVADE_INPUT_SLOT_SIZE);
            int recordedSequence = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_SEQ);
            if (recordedSequence != sequence)
                continue;
            if (_evadeInputProbeLogs >= maxLogs)
                continue;

            Line(FormatEvadeInputProbeHit(sequence, slot, nowTick));
            _evadeInputProbeLogs++;
            needsFlush = true;
        }

        _lastEvadeInputProbeSequence = count;
        if (needsFlush)
            Flush();
    }

    private string FormatEvadeInputProbeHit(int sequence, int slot, long nowTick)
    {
        int id = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_ID) & 0xFF;
        int hp = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_HP) & 0xFFFF;
        nint target = Marshal.ReadIntPtr(_evadeInputProbeBuf, slot + EI_TARGET);
        int ctrl = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_CTRL) & 0xFF;

        var before = new byte[8];
        var after = new byte[8];
        Marshal.Copy(IntPtr.Add(_evadeInputProbeBuf, slot + EI_BEFORE), before, 0, 8);
        Marshal.Copy(IntPtr.Add(_evadeInputProbeBuf, slot + EI_AFTER), after, 0, 8);
        int before4E = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_BEFORE_4E) & 0xFF;
        int after4E = Marshal.ReadInt32(_evadeInputProbeBuf, slot + EI_AFTER_4E) & 0xFF;

        // before/after hold record bytes 0x46..0x4D; the evade inputs are +0x46/+0x47/+0x4A/+0x4B and +0x4E.
        static string Ev(byte[] w, int b4e) =>
            $"46={w[0]:X2} 47={w[1]:X2} 4A={w[4]:X2} 4B={w[5]:X2} 4E={b4e:X2}";

        string ctrlText = ctrl switch
        {
            2 => " [CONTROL WROTE]",
            1 => " [CONTROL would-write(LogOnly)]",
            _ => "",
        };

        return
            $"[EVADE-INPUT event={sequence} target=0x{target:X} id=0x{id:X2} hp={hp} " +
            $"before({Ev(before, before4E)}) after({Ev(after, after4E)}) now={nowTick}]{ctrlText}";
    }

    private void InstallRollVerdictProbeIfEnabled(nint moduleBase)
    {
        if (!_settings.RollVerdictProbeEnabled)
            return;
        if (_hooks is null)
        {
            Line("[ROLL-VERDICT-SKIP] no IReloadedHooks");
            Flush();
            return;
        }

        _rollVerdictProbeBuf = Marshal.AllocHGlobal(ROLL_VERDICT_BUFFER_SIZE);
        for (int i = 0; i < ROLL_VERDICT_BUFFER_SIZE; i++)
            Marshal.WriteByte(_rollVerdictProbeBuf, i, 0);

        nint address = moduleBase + _settings.RollVerdictProbeRva;
        if (!ValidateExpectedBytes(address, _settings.RollVerdictProbeExpectedBytes, out string byteError))
        {
            Line($"[ROLL-VERDICT-SKIP] rva=0x{_settings.RollVerdictProbeRva:X} {byteError}");
            Flush();
            return;
        }

        try
        {
            var asm = BuildRollVerdictProbeAsm();
            _rollVerdictProbeHook = _hooks.CreateAsmHook(asm, address, AsmHookBehaviour.ExecuteFirst).Activate();
            Line(
                $"[ROLL-VERDICT-HOOK] rva=0x{_settings.RollVerdictProbeRva:X} addr=0x{address:X} " +
                $"maxLogs={_settings.RollVerdictProbeMaxLogs} expected={_settings.RollVerdictProbeExpectedBytes} " +
                $"{DescribeRollVerdictControl()}");
        }
        catch (Exception ex)
        {
            Line($"[ROLL-VERDICT-FAILED] rva=0x{_settings.RollVerdictProbeRva:X} {ex.GetType().Name}: {ex.Message}");
        }

        Flush();
    }

    private string[] BuildRollVerdictProbeAsm()
    {
        string buf = $"0{_rollVerdictProbeBuf:X}h";

        // ExecuteFirst at 0x30F4A7 ("mov r10d,eax"). At entry: eax = native verdict, rbx = acting unit.
        // We deliberately do NOT push/pop rax: we save the native verdict in r8d, optionally override it,
        // and write the final verdict back to eax at exit so the stolen "mov r10d,eax" propagates it. Only
        // rcx/r8/r9 and flags are saved.
        var asm = new List<string>
        {
            "use64",
            "push rcx",
            "push r8",
            "push r9",
            "pushfq",
            "test rbx, rbx",
            "jz .rv_done",
            "mov r8d, eax",               // r8d = native verdict (default final)
            $"mov rax, {buf}",
            // claim a sequence number and compute the ring slot into r9
            $"mov ecx, [rax+{RV_COUNT}]",
            "add ecx, 1",
            $"mov [rax+{RV_COUNT}], ecx",
            "mov r9d, ecx",
            $"and r9d, {ROLL_VERDICT_RING_MASK}",
            $"imul r9, r9, {ROLL_VERDICT_SLOT_SIZE}",
            "add r9, rax",
            $"add r9, {RV_EVENTS}",
            // header: zero seq first (partial-slot guard) and the control marker
            $"mov dword [r9+{RV_SEQ}], 0",
            $"mov dword [r9+{RV_CTRL}], 0",
            // acting-unit identity + native verdict + unit ptr
            "movzx ecx, byte [rbx]",
            $"mov [r9+{RV_ID}], ecx",
            $"mov [r9+{RV_NATIVE}], r8d",
            $"mov [r9+{RV_UNIT}], rbx",
        };

        // optional guarded override (may set r8d = forced verdict)
        asm.AddRange(BuildRollVerdictControlLines());

        // record the final verdict, publish the slot (seq last), and apply the verdict to eax
        asm.AddRange(
        [
            $"mov [r9+{RV_FINAL}], r8d",
            $"mov rax, {buf}",
            $"mov ecx, [rax+{RV_COUNT}]",
            $"mov [r9+{RV_SEQ}], ecx",
            "mov eax, r8d",               // final verdict -> eax (stolen "mov r10d,eax" propagates it)
            ".rv_done:",
            "popfq",
            "pop r9",
            "pop r8",
            "pop rcx",
        ]);
        return asm.ToArray();
    }

    // Guarded verdict override injected into the roll-verdict hook. Register state: rbx = acting unit,
    // rax = ring base, r9 = slot base, r8d = current final verdict, ecx = free scratch. Fail-closed: any
    // guard miss jumps to .rv_ctrl_done leaving r8d (native) intact. ForceVerdict<0 behaves as log-only.
    private string[] BuildRollVerdictControlLines()
    {
        if (!_settings.RollVerdictControlEnabled)
            return [];

        int maxWrites = Math.Clamp(_settings.RollVerdictControlMaxWrites, 1, ROLL_VERDICT_RING_SIZE);
        int targetId = _settings.RollVerdictControlTargetCharId;
        int forceVerdict = _settings.RollVerdictControlForceVerdict;
        bool logOnly = _settings.RollVerdictControlLogOnly || forceVerdict < 0;

        var c = new List<string>();
        if (!logOnly)
        {
            c.Add($"mov ecx, [rax+{RV_CTRL_WRITES}]");
            c.Add($"cmp ecx, {maxWrites}");
            c.Add("jae .rv_ctrl_done");
        }
        if (targetId >= 0)
        {
            c.Add("movzx ecx, byte [rbx]");
            c.Add($"cmp ecx, {targetId & 0xFF}");
            c.Add("jne .rv_ctrl_done");
        }
        if (logOnly)
        {
            c.Add($"mov byte [r9+{RV_CTRL}], 1");
        }
        else
        {
            c.Add($"mov ecx, [rax+{RV_CTRL_WRITES}]");
            c.Add("add ecx, 1");
            c.Add($"mov [rax+{RV_CTRL_WRITES}], ecx");
            c.Add($"mov r8d, {forceVerdict & 0xFF}");   // OVERRIDE the verdict
            c.Add($"mov byte [r9+{RV_CTRL}], 2");
        }
        c.Add(".rv_ctrl_done:");
        return c.ToArray();
    }

    private string DescribeRollVerdictControl()
    {
        if (!_settings.RollVerdictControlEnabled)
            return "(control=off, observe-only)";
        int forceVerdict = _settings.RollVerdictControlForceVerdict;
        bool logOnly = _settings.RollVerdictControlLogOnly || forceVerdict < 0;
        string mode = logOnly ? "LOGONLY(dry-run)" : "LIVE";
        string verdict = forceVerdict switch
        {
            0 => "0=miss",
            1 => "1=hit",
            2 => "2=special",
            < 0 => "observe",
            _ => $"{forceVerdict}",
        };
        return
            $"(control={mode} force={verdict} max={_settings.RollVerdictControlMaxWrites} " +
            $"target={(_settings.RollVerdictControlTargetCharId < 0 ? "any" : $"0x{_settings.RollVerdictControlTargetCharId & 0xFF:X2}")})";
    }

    private void CaptureRollVerdictProbeEvents(long nowTick)
    {
        if (_rollVerdictProbeBuf == 0 || !_settings.RollVerdictProbeEnabled)
            return;

        int count = Marshal.ReadInt32(_rollVerdictProbeBuf, RV_COUNT);
        if (count == _lastRollVerdictProbeSequence)
            return;

        bool needsFlush = false;
        int start = _lastRollVerdictProbeSequence + 1;
        if (count - _lastRollVerdictProbeSequence > ROLL_VERDICT_RING_SIZE)
        {
            int lost = count - _lastRollVerdictProbeSequence - ROLL_VERDICT_RING_SIZE;
            Line($"[ROLL-VERDICT-LOST last={_lastRollVerdictProbeSequence} current={count} lost={lost}]");
            start = count - ROLL_VERDICT_RING_SIZE + 1;
            needsFlush = true;
        }

        int maxLogs = Math.Clamp(_settings.RollVerdictProbeMaxLogs, 0, 100_000);
        for (int sequence = start; sequence <= count; sequence++)
        {
            int slot = RV_EVENTS + ((sequence & ROLL_VERDICT_RING_MASK) * ROLL_VERDICT_SLOT_SIZE);
            int recordedSequence = Marshal.ReadInt32(_rollVerdictProbeBuf, slot + RV_SEQ);
            if (recordedSequence != sequence)
                continue;
            if (_rollVerdictProbeLogs >= maxLogs)
                continue;

            Line(FormatRollVerdictProbeHit(sequence, slot, nowTick));
            _rollVerdictProbeLogs++;
            needsFlush = true;
        }

        _lastRollVerdictProbeSequence = count;
        if (needsFlush)
            Flush();
    }

    private string FormatRollVerdictProbeHit(int sequence, int slot, long nowTick)
    {
        int id = Marshal.ReadInt32(_rollVerdictProbeBuf, slot + RV_ID) & 0xFF;
        int native = Marshal.ReadInt32(_rollVerdictProbeBuf, slot + RV_NATIVE);
        int final = Marshal.ReadInt32(_rollVerdictProbeBuf, slot + RV_FINAL);
        nint unit = Marshal.ReadIntPtr(_rollVerdictProbeBuf, slot + RV_UNIT);
        int ctrl = Marshal.ReadInt32(_rollVerdictProbeBuf, slot + RV_CTRL) & 0xFF;

        string ctrlText = ctrl switch
        {
            2 => native != final ? " [VERDICT FORCED]" : " [VERDICT FORCED(no-op)]",
            1 => " [would-force(LogOnly)]",
            _ => "",
        };
        static string V(int v) => v switch { 0 => "0(miss)", 1 => "1(hit)", 2 => "2(special)", _ => $"{v}" };

        return
            $"[ROLL-VERDICT event={sequence} actor=0x{unit:X} id=0x{id:X2} " +
            $"native={V(native)} final={V(final)} now={nowTick}]{ctrlText}";
    }

    private static bool ValidateLandmarkBytes(nint address, LandmarkProbe probe, out string error)
        => ValidateExpectedBytes(address, probe.ExpectedBytes, out error);

    private static bool ValidateExpectedBytes(nint address, string expectedBytes, out string error)
    {
        error = "";
        byte[] expected;
        try
        {
            expected = ParseExpectedBytes(expectedBytes);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            error = $"invalid ExpectedBytes: {ex.Message}";
            return false;
        }
        if (expected.Length == 0)
            return true;

        var actual = new byte[expected.Length];
        if (!CurrentProcessMemory.TryRead(address, actual, out error))
            return false;

        for (int i = 0; i < expected.Length; i++)
        {
            if (actual[i] == expected[i])
                continue;

            error =
                $"expected bytes {FormatBytePattern(expected)} but found {FormatBytePattern(actual)}";
            return false;
        }

        return true;
    }

    private static byte[] ParseExpectedBytes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var bytes = new List<byte>();
        foreach (string token in text.Split([' ', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            string clean = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? token[2..]
                : token;
            if (clean is "?" or "??")
                throw new FormatException("wildcards are not supported in LandmarkProbe.ExpectedBytes");
            bytes.Add(Convert.ToByte(clean, 16));
        }

        return bytes.ToArray();
    }

    private static string FormatBytePattern(byte[] bytes)
        => bytes.Length == 0 ? "-" : string.Join(" ", bytes.Select(value => value.ToString("X2")));

    private static string FormatMaybeInt(int value)
        => value switch
        {
            PlanExpectedAny => "any",
            PlanExpectedPositiveDebit => "positive",
            < 0 => value.ToString(),
            _ => value.ToString(),
        };

    private static string FormatMaybeByte(int value)
        => value < 0 ? "any" : $"0x{value:X2}";

    private static bool IsPreClampManagedCallbackEnabled(RuntimeSettings settings)
        => settings.PreClampManagedCallbackEnabled || settings.DclPipelineEnabled;

    private static int ClampInt16Immediate(int value)
        => value < 0 ? -1 : Math.Clamp(value, short.MinValue, short.MaxValue);

    private long PreClampManagedCallbackImpl(long targetPtrRaw, long statePtrRaw, long hookStackPtrRaw, long bufferPtrRaw)
    {
        try
        {
            var settings = _settings;
            if (!IsPreClampManagedCallbackEnabled(settings) || settings.PreClampDamageRewriteLogOnly)
                return -1;

            nint targetPtr = (nint)targetPtrRaw;
            nint statePtr = (nint)statePtrRaw;
            nint hookStackPtr = (nint)hookStackPtrRaw;
            if (targetPtr == 0 || statePtr == 0)
                return -1;

            if (!PreClampManagedCallbackPassesGuards(targetPtr, statePtr, settings, out int targetId, out int oldDebit, out int oldCredit))
                return -1;

            if (settings.DclPipelineEnabled &&
                !string.IsNullOrWhiteSpace(settings.DclDamageFormula) &&
                TryEvaluateDclPreClampDamage(targetPtr, hookStackPtr, settings, targetId, oldDebit, oldCredit, out int dclDebit))
                return dclDebit;

            if (settings.PreClampManagedCallbackActorFormulaEnabled)
            {
                if (TryResolvePreClampManagedActor(targetPtr, hookStackPtr, settings, out var caster, out nint actorBase, out int actionId) &&
                    TryReadUnitByte(caster.Ptr, 0x3E, out int casterPa) &&
                    TryReadUnitByte(targetPtr, 0x2D, out int targetFaith))
                {
                    int multiplier = Math.Clamp(settings.PreClampManagedCallbackPaMultiplier, 0, 1000);
                    int minDamage = Math.Clamp(settings.PreClampManagedCallbackFormulaMinDamage, 0, short.MaxValue);
                    int maxDamage = Math.Clamp(settings.PreClampManagedCallbackFormulaMaxDamage, minDamage, short.MaxValue);
                    int formulaDebit = Math.Clamp((casterPa * multiplier) - targetFaith, minDamage, maxDamage);
                    RecordPreClampManagedFormulaTrace(
                        resolved: true,
                        targetId,
                        caster.CharId,
                        actionId,
                        formulaDebit,
                        oldDebit,
                        actorBase);
                    return ClampInt16Immediate(formulaDebit);
                }

                RecordPreClampManagedFormulaTrace(
                    resolved: false,
                    targetId,
                    casterId: -1,
                    actionId: -1,
                    debit: -1,
                    oldDebit,
                    actorBase: 0);
            }

            int forcedDebit = settings.PreClampManagedCallbackForcedDebit;
            if (forcedDebit < 0)
                return -1;

            return ClampInt16Immediate(forcedDebit);
        }
        catch
        {
            return -1;
        }
    }

    private bool TryEvaluateDclPreClampDamage(
        nint targetPtr,
        nint hookStackPtr,
        RuntimeSettings settings,
        int targetId,
        int oldDebit,
        int oldCredit,
        out int dclDebit)
    {
        dclDebit = -1;
        long nowTick = Stopwatch.GetTimestamp();
        DrainCalcEntryProbeEventsForDcl(nowTick);

        if (!TryGetUnitTableIndex(targetPtr, out int targetIdx, out long unitTable))
        {
            QueueDclCacheMiss(settings, "target-not-unit-table", targetIdx: -1, targetId, nowTick);
            return false;
        }

        long maxAgeTicks = StopwatchTicksFromMilliseconds(settings.DclActionContextMaxAgeMs);
        if (!_dclActionCache.TryGetLatest(targetIdx, nowTick, maxAgeTicks, out var actionContext))
        {
            QueueDclCacheMiss(settings, "no-calc-entry", targetIdx, targetId, nowTick);
            return false;
        }

        if ((uint)actionContext.CasterIdx >= 64)
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-ERR] reason=caster-index-out-of-range target=0x{targetId:X2} targetIdx={targetIdx} casterIdx={actionContext.CasterIdx}");
            return false;
        }

        nint casterPtr = (nint)(unitTable + actionContext.CasterIdx * BattleUnitStride);
        if (!TryReadLiveUnitSnapshot(targetPtr, out var target, out string targetError))
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-ERR] reason=target-read-failed target=0x{targetId:X2} targetIdx={targetIdx} error={CleanDclLogValue(targetError)}");
            return false;
        }

        if (!TryReadLiveUnitSnapshot(casterPtr, out var attacker, out string casterError))
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-ERR] reason=caster-read-failed target=0x{targetId:X2} targetIdx={targetIdx} casterIdx={actionContext.CasterIdx} error={CleanDclLogValue(casterError)}");
            return false;
        }

        if (TryResolvePreClampManagedActor(targetPtr, hookStackPtr, settings, out var resolvedCaster, out _, out _) &&
            resolvedCaster.Ptr != casterPtr)
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-MISMATCH] target=0x{target.CharId:X2} targetIdx={targetIdx} cacheCasterIdx={actionContext.CasterIdx} " +
                $"cacheCaster=0x{attacker.CharId:X2} frameCaster=0x{resolvedCaster.CharId:X2} cachePtr=0x{casterPtr:X} framePtr=0x{resolvedCaster.Ptr:X}");
        }

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        int syntheticCurrentHp = Math.Clamp(target.Hp - Math.Max(0, oldDebit), 0, Math.Max(0, target.MaxHp));
        long eventSeed = ComputeEventSeed(target, eventIndex, target.Hp, syntheticCurrentHp, oldDebit);
        var itemCatalog = _itemCatalog;
        var abilityCatalog = _abilityCatalog;
        var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings,
            itemCatalog,
            abilityCatalog,
            target,
            attacker,
            eventIndex,
            eventSeed,
            actionContext.ActionType,
            actionContext.AbilityId,
            oldDebit,
            oldCredit);

        if (!FormulaRuntimeContextBuilder.TryApplyDerivedVariables(context, settings.DclDerivedVariables, "DclDerivedVariables", out string derivedError))
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-ERR] target=0x{target.CharId:X2} caster=0x{attacker.CharId:X2} abilityId={actionContext.AbilityId} " +
                $"actionType=0x{actionContext.ActionType:X2} oldDebit={oldDebit} error={CleanDclLogValue(derivedError)}");
            return false;
        }

        if (!FormulaExpression.TryEvaluate(settings.DclDamageFormula, context, out int formulaDebit, out string error))
        {
            QueueDclDecisionLog(
                settings,
                $"[DCL-ERR] target=0x{target.CharId:X2} caster=0x{attacker.CharId:X2} abilityId={actionContext.AbilityId} " +
                $"actionType=0x{actionContext.ActionType:X2} oldDebit={oldDebit} error={CleanDclLogValue(error)}");
            return false;
        }

        dclDebit = Math.Clamp(formulaDebit, 0, short.MaxValue);
        string abilityName = abilityCatalog.TryGet(actionContext.AbilityId, out var ability)
            ? ability.Name
            : "unknown";
        QueueDclDecisionLog(
            settings,
            $"[DCL] caster=0x{attacker.CharId:X2} target=0x{target.CharId:X2} abilityId={actionContext.AbilityId} " +
            $"ability={CleanDclLogValue(abilityName)} actionType=0x{actionContext.ActionType:X2} result={formulaDebit} debit={dclDebit} oldDebit={oldDebit}");
        _dclHitDecisionCache.Invalidate(targetIdx, actionContext.CasterIdx, actionContext.AbilityId, actionContext.ActionType);
        return true;
    }

    private bool TryGetUnitTableIndex(nint unitPtr, out int unitIdx, out long unitTable)
    {
        unitIdx = -1;
        unitTable = _moduleBase.ToInt64() + BattleUnitTableRva;
        if (_moduleBase == 0 || unitPtr == 0)
            return false;

        long relative = unitPtr.ToInt64() - unitTable;
        if (relative < 0 || relative % BattleUnitStride != 0)
            return false;

        long slot = relative / BattleUnitStride;
        if (slot is < 0 or >= 64)
            return false;

        unitIdx = (int)slot;
        return true;
    }

    private static long StopwatchTicksFromMilliseconds(int milliseconds)
    {
        if (milliseconds <= 0)
            return 0;

        return ((long)Math.Min(milliseconds, 3_600_000) * Stopwatch.Frequency) / 1000;
    }

    private void QueueDclCacheMiss(RuntimeSettings settings, string reason, int targetIdx, int targetId, long nowTick)
    {
        long miss = Interlocked.Increment(ref _dclCacheMissCount);
        if (miss != 1 && miss % 64 != 0)
            return;

        QueueDclDecisionLog(
            settings,
            $"[DCL-MISS] reason={reason} miss={miss} target=0x{targetId:X2} targetIdx={targetIdx} now={nowTick}");
    }

    private void QueueDclDecisionLog(RuntimeSettings settings, string line)
    {
        int maxLogs = Math.Clamp(settings.DclDecisionMaxLogs, 0, 100_000);
        if (maxLogs == 0)
            return;

        int logIndex = Interlocked.Increment(ref _dclDecisionLogCount);
        if (logIndex > maxLogs)
            return;

        lock (_dclDecisionLogGate)
            _dclDecisionLogQueue.Enqueue(line);
    }

    private static string CleanDclLogValue(string value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ').Trim();

    // DCL HIT CONTROL — the managed decision callback invoked from the calc-entry shim (0x309A44),
    // BEFORE the VM's avoidance roll inside the same call. Computes the authored hit% from the full
    // (attacker, target, equipment, ability) context, rolls the mod's own RNG, and forces the
    // binary outcome by stamping the TARGET's Family-1 evade input bytes (the inputs the VM honors):
    //   HIT  -> 0x46..0x4E all 0 (Concentrate-equivalent: no evade source can win -> connects);
    //   MISS -> all 0 except class evade +0x4B = DclMissClassEvadeValue (job-derived class evade is
    //           read live by the VM; 100 forces a guaranteed class-evade "Miss" — proven LT5-B).
    // Decisions are cached per (caster, target, ability, type) for DclHitDecisionTtlMs so
    // preview/charge/AI refires of the same action reuse ONE rolled outcome. Every failure with a
    // valid target restores the force-hit baseline; exceptions are swallowed and counted. No file
    // I/O ever happens here — logs are queued and flushed by the poller.
    private static readonly int[] DclHitEvadeOffsets = [0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E];

    private void ZeroDclTargetEvade(int targetIdx)
    {
        if ((uint)targetIdx >= 64)
            return;

        lock (_dclHitStampedTargetGate)
        {
            if (TryZeroDclTargetEvadeBytes(targetIdx))
                _dclHitStampedTargets[targetIdx] = false;
        }
    }

    private bool TryZeroDclTargetEvadeBytes(int targetIdx)
    {
        try
        {
            long unitTable = _moduleBase.ToInt64() + BattleUnitTableRva;
            nint targetPtr = (nint)(unitTable + (long)targetIdx * BattleUnitStride);
            foreach (int offset in DclHitEvadeOffsets)
                Marshal.WriteByte(targetPtr, offset, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void StampDclHitDecision(
        int targetIdx,
        int casterIdx,
        int abilityId,
        int actionType,
        DclHitDecision decision,
        long nowTick,
        bool cached,
        nint targetPtr,
        int missClassEvade)
    {
        if ((uint)targetIdx >= 64)
            return;

        lock (_dclHitStampedTargetGate)
        {
            if (!cached)
                _dclHitDecisionCache.Record(targetIdx, casterIdx, abilityId, actionType, decision, nowTick);

            _dclHitStampedTargets[targetIdx] = true;
            if (decision.Hit)
            {
                Marshal.WriteByte(targetPtr, 0x4B, 0);
                foreach (int offset in DclHitEvadeOffsets)
                {
                    if (offset != 0x4B)
                        Marshal.WriteByte(targetPtr, offset, 0);
                }
            }
            else
            {
                foreach (int offset in DclHitEvadeOffsets)
                {
                    if (offset != 0x4B)
                        Marshal.WriteByte(targetPtr, offset, 0);
                }
                Marshal.WriteByte(targetPtr, 0x4B, (byte)missClassEvade);
            }
        }
    }

    private long DclHitDecisionCallbackImpl(long orderRecordPtrRaw, long targetIdxRaw)
    {
        int targetIdx = -1;
        try
        {
            var settings = _settings;
            if (!settings.DclHitControlEnabled)
                return 0;

            nint orderRecordPtr = (nint)orderRecordPtrRaw;
            targetIdx = (int)(targetIdxRaw & 0xFF);
            if ((uint)targetIdx >= 64)
            {
                QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=target-index-oob targetIdx={targetIdx}");
                targetIdx = -1;
                return 0;
            }
            if (orderRecordPtr == 0)
            {
                ZeroDclTargetEvade(targetIdx);
                QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=null-order-record targetIdx={targetIdx}");
                return 0;
            }

            // Same packing the probe stub captures: dword[record] = casterIdx | type<<8 | abilityId<<16.
            int packed = Marshal.ReadInt32(orderRecordPtr);
            int casterIdx = packed & 0xFF;
            int actionType = (packed >> 8) & 0xFF;
            int abilityId = (packed >> 16) & 0xFFFF;
            if ((uint)casterIdx >= 64)
            {
                ZeroDclTargetEvade(targetIdx);
                QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=caster-index-oob casterIdx={casterIdx} targetIdx={targetIdx}");
                return 0;
            }

            long nowTick = Stopwatch.GetTimestamp();
            long ttlTicks = StopwatchTicksFromMilliseconds(settings.DclHitDecisionTtlMs);
            bool cached = _dclHitDecisionCache.TryGet(targetIdx, casterIdx, abilityId, actionType, nowTick, ttlTicks, out var decision);
            if (!cached)
            {
                if (!TryComputeDclHitDecision(settings, targetIdx, casterIdx, actionType, abilityId, out decision))
                {
                    ZeroDclTargetEvade(targetIdx);
                    return 0;
                }
            }

            long unitTable = _moduleBase.ToInt64() + BattleUnitTableRva;
            nint targetPtr = (nint)(unitTable + (long)targetIdx * BattleUnitStride);
            nint casterPtr = (nint)(unitTable + (long)casterIdx * BattleUnitStride);
            int missClassEvade = Math.Clamp(settings.DclMissClassEvadeValue, 0, 255);
            StampDclHitDecision(targetIdx, casterIdx, abilityId, actionType, decision, nowTick, cached, targetPtr, missClassEvade);

            int casterCharId = TryReadUnitByte(casterPtr, 0, out int cid) ? cid : -1;
            int targetCharId = TryReadUnitByte(targetPtr, 0, out int tid) ? tid : -1;
            QueueDclHitLog(
                settings,
                $"[DCL-HIT] caster=0x{casterCharId:X2} target=0x{targetCharId:X2} ability={abilityId} type=0x{actionType:X2} " +
                $"pct={decision.Pct} roll={decision.Roll} outcome={(decision.Hit ? "hit" : "miss")} cached={(cached ? 1 : 0)}");
            return 0;
        }
        catch
        {
            Interlocked.Increment(ref _dclHitFailCount);
            // Fail-open must not leave a stale MISS stamp: restore the force-hit baseline
            // for the target when it was already validated before the exception.
            if ((uint)targetIdx < 64)
            {
                try { ZeroDclTargetEvade(targetIdx); } catch { }
            }
            return 0;
        }
    }

    private bool TryComputeDclHitDecision(
        RuntimeSettings settings,
        int targetIdx,
        int casterIdx,
        int actionType,
        int abilityId,
        out DclHitDecision decision)
    {
        decision = default;
        if (string.IsNullOrWhiteSpace(settings.DclHitChanceFormula))
        {
            ZeroDclTargetEvade(targetIdx);
            QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=empty-formula targetIdx={targetIdx}");
            return false;
        }

        long unitTable = _moduleBase.ToInt64() + BattleUnitTableRva;
        nint targetPtr = (nint)(unitTable + (long)targetIdx * BattleUnitStride);
        nint casterPtr = (nint)(unitTable + (long)casterIdx * BattleUnitStride);
        if (!TryReadLiveUnitSnapshot(targetPtr, out var target, out string targetError))
        {
            ZeroDclTargetEvade(targetIdx);
            QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=target-read-failed targetIdx={targetIdx} error={CleanDclLogValue(targetError)}");
            return false;
        }
        if (!TryReadLiveUnitSnapshot(casterPtr, out var attacker, out string casterError))
        {
            ZeroDclTargetEvade(targetIdx);
            QueueDclHitLog(settings, $"[DCL-HIT-MISS] reason=caster-read-failed casterIdx={casterIdx} targetIdx={targetIdx} error={CleanDclLogValue(casterError)}");
            return false;
        }

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        long eventSeed = ComputeEventSeed(target, eventIndex, target.Hp, target.Hp, 0);
        var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings,
            _itemCatalog,
            _abilityCatalog,
            target,
            attacker,
            eventIndex,
            eventSeed,
            actionType,
            abilityId,
            oldDebit: 0,   // pre-roll: no staged result exists yet; dcl.oldDebit/oldCredit are 0 here
            oldCredit: 0);

        if (!FormulaRuntimeContextBuilder.TryApplyDerivedVariables(context, settings.DclDerivedVariables, "DclDerivedVariables", out string derivedError))
        {
            ZeroDclTargetEvade(targetIdx);
            QueueDclHitLog(
                settings,
                $"[DCL-HIT-ERR] caster=0x{attacker.CharId:X2} target=0x{target.CharId:X2} ability={abilityId} type=0x{actionType:X2} error={CleanDclLogValue(derivedError)}");
            return false;
        }
        if (!FormulaExpression.TryEvaluate(settings.DclHitChanceFormula, context, out int rawPct, out string formulaError))
        {
            ZeroDclTargetEvade(targetIdx);
            QueueDclHitLog(
                settings,
                $"[DCL-HIT-ERR] caster=0x{attacker.CharId:X2} target=0x{target.CharId:X2} ability={abilityId} type=0x{actionType:X2} error={CleanDclLogValue(formulaError)}");
            return false;
        }

        int pct = Math.Clamp(rawPct, 0, 100);
        int forcedRoll = settings.DclHitForcedRoll;
        int roll;
        if (forcedRoll is >= 0 and <= 99)
        {
            roll = forcedRoll;
        }
        else
        {
            lock (_dclHitRngGate)
                roll = (_dclHitRng ??= new Random()).Next(100);
        }

        decision = new DclHitDecision(roll < pct, pct, roll);
        return true;
    }

    private void CaptureDclHitStaleStamps(long nowTick)
    {
        var settings = _settings;
        if (!settings.DclHitControlEnabled)
            return;

        long ttlTicks = StopwatchTicksFromMilliseconds(settings.DclHitDecisionTtlMs);
        for (int targetIdx = 0; targetIdx < 64; targetIdx++)
        {
            bool tracked;
            lock (_dclHitStampedTargetGate)
                tracked = _dclHitStampedTargets[targetIdx];

            if (!tracked)
                continue;

            if (_dclHitDecisionCache.HasLiveDecision(targetIdx, nowTick, ttlTicks))
                continue;

            lock (_dclHitStampedTargetGate)
            {
                if (!_dclHitStampedTargets[targetIdx])
                    continue;

                if (_dclHitDecisionCache.HasLiveDecision(targetIdx, Stopwatch.GetTimestamp(), ttlTicks))
                    continue;

                if (TryZeroDclTargetEvadeBytes(targetIdx))
                    _dclHitStampedTargets[targetIdx] = false;
            }
        }
    }

    private void QueueDclHitLog(RuntimeSettings settings, string line)
    {
        int maxLogs = Math.Clamp(settings.DclHitMaxLogs, 0, 100_000);
        if (maxLogs == 0)
            return;

        int logIndex = Interlocked.Increment(ref _dclHitLogCount);
        if (logIndex > maxLogs)
            return;

        // Shares the DCL decision queue (drained by the poller; the hook thread never touches the
        // log file) but with its own DclHitMaxLogs budget, independent of DclDecisionMaxLogs.
        lock (_dclDecisionLogGate)
            _dclDecisionLogQueue.Enqueue(line);
    }

    private static bool PreClampManagedCallbackPassesGuards(
        nint targetPtr,
        nint statePtr,
        RuntimeSettings settings,
        out int targetId,
        out int oldDebit,
        out int oldCredit)
    {
        targetId = -1;
        oldDebit = 0;
        oldCredit = 0;
        try
        {
            targetId = Marshal.ReadByte(targetPtr);
            if (settings.PreClampDamageRewriteTargetCharId >= 0 &&
                targetId != settings.PreClampDamageRewriteTargetCharId)
                return false;

            if (settings.PreClampDamageRewriteTargetTeam >= 0 &&
                Marshal.ReadByte(targetPtr, 4) != settings.PreClampDamageRewriteTargetTeam)
                return false;

            int hp = (ushort)Marshal.ReadInt16(targetPtr, 0x30);
            int minHp = Math.Clamp(settings.PreClampDamageRewriteMinHp, 0, 9999);
            int maxHp = Math.Clamp(settings.PreClampDamageRewriteMaxHp, minHp, 9999);
            if (hp < minHp || hp > maxHp)
                return false;

            oldDebit = Marshal.ReadInt16(statePtr, 6);
            oldCredit = Marshal.ReadInt16(statePtr, 8);
            if (settings.PreClampDamageRewriteExpectedDebit >= 0 &&
                oldDebit != settings.PreClampDamageRewriteExpectedDebit)
                return false;

            if (settings.PreClampDamageRewriteExpectedCredit >= 0 &&
                oldCredit != settings.PreClampDamageRewriteExpectedCredit)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryResolvePreClampManagedActor(
        nint targetPtr,
        nint hookStackPtr,
        RuntimeSettings settings,
        out UnitObservationView caster,
        out nint actorBase,
        out int actionId)
    {
        caster = default;
        actorBase = 0;
        actionId = -1;
        if (hookStackPtr == 0)
            return false;

        UnitObservationView[] units = Volatile.Read(ref _unitObservationSnapshot);
        if (units.Length == 0)
            return false;

        int unitOff = Math.Clamp(settings.PreClampActorStructUnitOffset, 0, 0x4000);
        int actOff = Math.Clamp(settings.PreClampActorActionIdOffset, 0, 0x4000);
        int stackBytes = Math.Clamp(settings.PreClampManagedCallbackStackScanBytes, 0, 0x4000);

        var seenRoots = new nint[64];
        var casterUnits = new nint[8];
        var selfUnits = new nint[4];
        int seenRootCount = 0;
        int distinctCasterCount = 0;
        int distinctSelfCount = 0;
        UnitObservationView firstCaster = default;
        nint firstCasterActor = 0;
        int firstCasterAction = -1;
        UnitObservationView firstSelf = default;
        nint firstSelfActor = 0;
        int firstSelfAction = -1;

        void VisitRoot(nint root)
        {
            if (root == 0)
                return;
            if (!RememberDistinct(seenRoots, ref seenRootCount, root))
                return;
            if (TryFindObservedUnit(root, units, out _))
                return;
            if (!TryReadLivePtr(root + unitOff, out var linkedUnit))
                return;
            if (!TryFindObservedUnit(linkedUnit, units, out var unit))
                return;

            int aid = TryReadLiveU16(root + actOff, out int liveActionId) ? liveActionId : -1;
            if (linkedUnit != targetPtr)
            {
                int before = distinctCasterCount;
                if (RememberDistinct(casterUnits, ref distinctCasterCount, linkedUnit) && before == 0)
                {
                    firstCaster = unit;
                    firstCasterActor = root;
                    firstCasterAction = aid;
                }
            }
            else if (aid > 0)
            {
                int before = distinctSelfCount;
                if (RememberDistinct(selfUnits, ref distinctSelfCount, linkedUnit) && before == 0)
                {
                    firstSelf = unit;
                    firstSelfActor = root;
                    firstSelfAction = aid;
                }
            }
        }

        // Saved volatile registers from the hook prologue.
        VisitRoot(ReadPointerOrZero(hookStackPtr + 56)); // original rax
        VisitRoot(ReadPointerOrZero(hookStackPtr + 48)); // original rcx
        VisitRoot(ReadPointerOrZero(hookStackPtr + 40)); // original rdx
        VisitRoot(ReadPointerOrZero(hookStackPtr + 32)); // original r8
        VisitRoot(ReadPointerOrZero(hookStackPtr + 24)); // original r9
        VisitRoot(ReadPointerOrZero(hookStackPtr + 16)); // original r10
        VisitRoot(ReadPointerOrZero(hookStackPtr + 8));  // original r11

        nint originalStack = hookStackPtr + 64;
        for (int offset = 0; offset + IntPtr.Size <= stackBytes; offset += IntPtr.Size)
            VisitRoot(ReadPointerOrZero(originalStack + offset));

        if (distinctCasterCount == 1)
        {
            caster = firstCaster;
            actorBase = firstCasterActor;
            actionId = firstCasterAction;
            return true;
        }

        if (distinctCasterCount == 0 && distinctSelfCount == 1)
        {
            caster = firstSelf;
            actorBase = firstSelfActor;
            actionId = firstSelfAction;
            return true;
        }

        return false;
    }

    private static bool RememberDistinct(nint[] values, ref int count, nint value)
    {
        int searchable = Math.Min(count, values.Length);
        for (int i = 0; i < searchable; i++)
        {
            if (values[i] == value)
                return false;
        }

        if (count < values.Length)
            values[count] = value;
        count++;
        return true;
    }

    private static bool TryFindObservedUnit(nint ptr, UnitObservationView[] units, out UnitObservationView unit)
    {
        foreach (var candidate in units)
        {
            if (candidate.Ptr != ptr)
                continue;
            unit = candidate;
            return true;
        }

        unit = default;
        return false;
    }

    private static nint ReadPointerOrZero(nint address)
        => TryReadLivePtr(address, out var value) ? value : 0;

    private static bool TryReadUnitByte(nint unitPtr, int offset, out int value)
    {
        value = 0;
        try
        {
            value = Marshal.ReadByte(unitPtr, offset);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RecordPreClampManagedFormulaTrace(
        bool resolved,
        int targetId,
        int casterId,
        int actionId,
        int debit,
        int oldDebit,
        nint actorBase)
    {
        Volatile.Write(ref _preClampManagedLastTargetId, targetId);
        Volatile.Write(ref _preClampManagedLastCasterId, casterId);
        Volatile.Write(ref _preClampManagedLastActionId, actionId);
        Volatile.Write(ref _preClampManagedLastDebit, debit);
        Volatile.Write(ref _preClampManagedLastOldDebit, oldDebit);
        Volatile.Write(ref _preClampManagedLastActorBase, actorBase.ToInt64());
        if (resolved)
            Interlocked.Increment(ref _preClampManagedFormulaResolvedCount);
        else
            Interlocked.Increment(ref _preClampManagedFormulaUnresolvedCount);
    }

    private string[] BuildLandmarkHookAsm(int landmarkId)
    {
        string buf = $"0{_landmarkBuf:X}h";
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push r8",
            "push r9",
            "pushfq",
            $"mov rax, {buf}",
            $"mov r8d, [rax+{L_COUNT}]",
            "add r8d, 1",
            $"mov [rax+{L_COUNT}], r8d",
            "mov r9d, r8d",
            $"and r9d, {LANDMARK_RING_MASK}",
            $"imul r9, r9, {LANDMARK_SLOT_SIZE}",
            "add r9, rax",
            $"add r9, {L_EVENTS}",
            $"mov dword [r9+{L_SEQ}], 0",
            $"mov dword [r9+{L_ID}], {landmarkId}",
        };

        void StoreOriginalFromStack(int registerIndex, int stackOffset)
        {
            asm.Add($"mov r8, [rsp+{stackOffset}]");
            asm.Add($"mov [r9+{L_REGS + registerIndex * 8}], r8");
        }

        StoreOriginalFromStack(0, 24); // rax
        asm.Add($"mov [r9+{L_REGS + 1 * 8}], rbx");
        asm.Add($"mov [r9+{L_REGS + 2 * 8}], rcx");
        asm.Add($"mov [r9+{L_REGS + 3 * 8}], rdx");
        asm.Add($"mov [r9+{L_REGS + 4 * 8}], rsi");
        asm.Add($"mov [r9+{L_REGS + 5 * 8}], rdi");
        asm.Add($"mov [r9+{L_REGS + 6 * 8}], rbp");
        asm.Add("lea r8, [rsp+32]");
        asm.Add($"mov [r9+{L_REGS + 7 * 8}], r8");
        StoreOriginalFromStack(8, 16); // r8
        StoreOriginalFromStack(9, 8);  // r9
        asm.Add($"mov [r9+{L_REGS + 10 * 8}], r10");
        asm.Add($"mov [r9+{L_REGS + 11 * 8}], r11");
        asm.Add($"mov [r9+{L_REGS + 12 * 8}], r12");
        asm.Add($"mov [r9+{L_REGS + 13 * 8}], r13");
        asm.Add($"mov [r9+{L_REGS + 14 * 8}], r14");
        asm.Add($"mov [r9+{L_REGS + 15 * 8}], r15");
        asm.Add($"mov r8d, [rax+{L_COUNT}]");
        asm.Add($"mov [r9+{L_SEQ}], r8d");
        asm.AddRange(new[] { "popfq", "pop r9", "pop r8", "pop rax" });
        return asm.ToArray();
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
                CaptureLandmarkProbeEvents(nowTick);
                CapturePreClampDamageRewriteEvents(nowTick);
                CaptureResultSelectorProbeEvents(nowTick);
                CaptureEvadeInputProbeEvents(nowTick);
                CaptureRollVerdictProbeEvents(nowTick);
                CaptureCalcEntryProbeEvents(nowTick);
                CaptureDclDecisionLogs();
                if (_settings.DclHitControlEnabled)
                    CaptureDclHitStaleStamps(nowTick);
                CaptureLt3RollProbeEvents();
                CaptureReactionRollEvents();
                CaptureRollRngProbeEvents(nowTick);
                CaptureStagedBundleProbeEvents();
                PollRegisteredUnits(nowTick);
                PokeForecastPreviewIfEnabled();
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

    // FORECAST PREVIEW POKE — the universal preview HP-amount lever. The forecast "object" pointed to by
    // global 0x142FF3CF8 is just target_unit+0x1BE, so obj+0x6 == unit+0x1C4 is the staged damage/debit
    // field and obj+0x8 == unit+0x1C6 is the staged heal/credit field. The selected field drives BOTH the
    // on-screen number and the HP-bar ghost.
    // The engine computes obj+6 ONCE per preview (does NOT rewrite per-frame — proven 300/300 writes stuck),
    // so a 20ms poll-write holds our value; number + bar both follow it. This supersedes the finalizer hooks
    // (which only covered some action types) and the cosmetic display-number paint. Safe: we only write when
    // the global points cleanly into the unit table; at action resolution the producer re-stages +0x1C4
    // before apply, so a preview poke never leaks into the real result (the pre-clamp hook owns the result).
    private int _forecastPokeCount;
    private void PokeForecastPreviewIfEnabled()
    {
        if (!_settings.PreviewForecastPokeEnabled || _settings.PreviewForecastPokeValue < 0 || _moduleBase == 0)
            return;
        nint globalAddr = _moduleBase + _settings.PreviewForecastGlobalRva;
        nint obj = Marshal.ReadIntPtr(globalAddr);          // forecast object = target_unit + ObjOffset
        if (obj == 0) return;                               // null when no preview is displayed
        long stride = _settings.PreviewForecastUnitStride;
        if (stride <= 0) return;
        long unitTable = _moduleBase.ToInt64() + _settings.PreviewForecastUnitTableRva;
        long rel = obj.ToInt64() - _settings.PreviewForecastObjOffset - unitTable;
        long maxUnits = Math.Max(1, _settings.MaxTrackedBattleUnits);
        if (rel < 0 || rel > stride * maxUnits || (rel % stride) != 0)
            return;                                         // not aligned into the unit table -> skip (no corruption)
        short value = (short)Math.Clamp(_settings.PreviewForecastPokeValue, 0, 0x7FFF);
        Marshal.WriteInt16(obj + _settings.PreviewForecastDamageFieldOffset, value);
        if (_forecastPokeCount++ < 3)
        {
            Line($"[FORECAST-POKE] obj=0x{obj.ToInt64():X} unitIdx={rel / stride} wrote unit+0x{_settings.PreviewForecastObjOffset + _settings.PreviewForecastDamageFieldOffset:X}={value}");
            Flush();
        }
    }

    private void CaptureHookObservation(ref int lastHookCount, long nowTick)
    {
        int count = Marshal.ReadInt32(_buf, B_COUNT);
        if (count == lastHookCount) return;

        lastHookCount = count;
        nint unitPtr = Marshal.ReadIntPtr(_buf, B_PTR);
        byte[] raw = ReadHookDumpBytes();
        if (!TryCreateUnitSnapshot(unitPtr, raw, out var target, out _)) return;

        _lastHookObservationTick = nowTick;
        LogHookRegisterProbeIfEnabled(count, target);
        ProcessObservedUnit(target, nowTick, touchForContext: true, logStructMapping: true);
    }

    private void CaptureLandmarkProbeEvents(long nowTick)
    {
        if (_landmarkBuf == 0 || !_settings.LandmarkProbeEnabled)
            return;

        int count = Marshal.ReadInt32(_landmarkBuf, L_COUNT);
        if (count == _lastLandmarkProbeSequence)
            return;

        bool needsFlush = false;
        int start = _lastLandmarkProbeSequence + 1;
        if (count - _lastLandmarkProbeSequence > LANDMARK_RING_SIZE)
        {
            int lost = count - _lastLandmarkProbeSequence - LANDMARK_RING_SIZE;
            Line($"[LANDMARK-LOST last={_lastLandmarkProbeSequence} current={count} lost={lost}]");
            start = count - LANDMARK_RING_SIZE + 1;
            needsFlush = true;
        }

        int maxLogs = Math.Clamp(_settings.LandmarkProbeMaxLogs, 0, 10_000);
        for (int sequence = start; sequence <= count; sequence++)
        {
            int slot = L_EVENTS + ((sequence & LANDMARK_RING_MASK) * LANDMARK_SLOT_SIZE);
            int recordedSequence = Marshal.ReadInt32(_landmarkBuf, slot + L_SEQ);
            if (recordedSequence != sequence)
                continue;

            int probeId = Marshal.ReadInt32(_landmarkBuf, slot + L_ID);
            if (!_landmarkProbesById.TryGetValue(probeId, out var probe))
                continue;

            if (_landmarkProbeLogs >= maxLogs)
                continue;

            var registers = ReadLandmarkRegisters(slot);
            string line = FormatLandmarkHit(sequence, probeId, probe, registers, nowTick);
            if (line.Length == 0)
                continue;

            Line(line);
            _landmarkProbeLogs++;
            needsFlush = true;
        }

        _lastLandmarkProbeSequence = count;
        if (needsFlush)
            Flush();
    }

    private void CapturePreClampDamageRewriteEvents(long nowTick)
    {
        if (_preClampDamageRewriteBuf == 0 || !_settings.PreClampDamageRewriteEnabled)
            return;

        int count = Marshal.ReadInt32(_preClampDamageRewriteBuf, P_COUNT);
        int managedCallbackCount = Marshal.ReadInt32(_preClampDamageRewriteBuf, P_MANAGED_CALLBACKS);
        bool managedCallbackChanged = managedCallbackCount != _lastPreClampManagedCallbackSequence;
        if (managedCallbackChanged)
        {
            Line($"[PRECLAMP-MANAGED-CALLBACK calls={managedCallbackCount} now={nowTick}]");
            _lastPreClampManagedCallbackSequence = managedCallbackCount;
        }
        bool managedFormulaChanged = LogPreClampManagedFormulaTraceIfChanged(nowTick);

        if (count == _lastPreClampDamageRewriteSequence)
        {
            if (managedCallbackChanged || managedFormulaChanged)
                Flush();
            return;
        }

        bool needsFlush = managedCallbackChanged || managedFormulaChanged;
        int start = _lastPreClampDamageRewriteSequence + 1;
        if (count - _lastPreClampDamageRewriteSequence > PRECLAMP_RING_SIZE)
        {
            int lost = count - _lastPreClampDamageRewriteSequence - PRECLAMP_RING_SIZE;
            Line($"[PRECLAMP-REWRITE-LOST last={_lastPreClampDamageRewriteSequence} current={count} lost={lost}]");
            start = count - PRECLAMP_RING_SIZE + 1;
            needsFlush = true;
        }

        for (int sequence = start; sequence <= count; sequence++)
        {
            int slot = P_EVENTS + ((sequence & PRECLAMP_RING_MASK) * PRECLAMP_SLOT_SIZE);
            int recordedSequence = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_SEQ);
            if (recordedSequence != sequence)
                continue;

            Line(FormatPreClampDamageRewriteHit(sequence, slot, nowTick));
            LogPreClampPointerScanIfEnabled(sequence, slot, nowTick);
            ResolveAndLogActorContextIfEnabled(sequence, slot, nowTick);
            needsFlush = true;
        }

        _lastPreClampDamageRewriteSequence = count;
        if (needsFlush)
            Flush();
    }

    private bool LogPreClampManagedFormulaTraceIfChanged(long nowTick)
    {
        bool logged = false;
        long resolved = Interlocked.Read(ref _preClampManagedFormulaResolvedCount);
        if (resolved != _lastPreClampManagedFormulaResolvedLogged)
        {
            _lastPreClampManagedFormulaResolvedLogged = resolved;
            Line(
                $"[PRECLAMP-MANAGED-FORMULA resolved={resolved} now={nowTick} " +
                $"target=0x{Volatile.Read(ref _preClampManagedLastTargetId):X2} " +
                $"caster=0x{Volatile.Read(ref _preClampManagedLastCasterId):X2} " +
                $"actor=0x{Volatile.Read(ref _preClampManagedLastActorBase):X} " +
                $"actionId={Volatile.Read(ref _preClampManagedLastActionId)} " +
                $"oldDebit={Volatile.Read(ref _preClampManagedLastOldDebit)} " +
                $"debit={Volatile.Read(ref _preClampManagedLastDebit)}]");
            logged = true;
        }

        long unresolved = Interlocked.Read(ref _preClampManagedFormulaUnresolvedCount);
        if (unresolved != _lastPreClampManagedFormulaUnresolvedLogged)
        {
            _lastPreClampManagedFormulaUnresolvedLogged = unresolved;
            Line(
                $"[PRECLAMP-MANAGED-FORMULA unresolved={unresolved} now={nowTick} " +
                $"target=0x{Volatile.Read(ref _preClampManagedLastTargetId):X2} " +
                $"oldDebit={Volatile.Read(ref _preClampManagedLastOldDebit)}]");
            logged = true;
        }

        return logged;
    }

    private string FormatPreClampDamageRewriteHit(int sequence, int slot, long nowTick)
    {
        nint unitPtr = Marshal.ReadIntPtr(_preClampDamageRewriteBuf, slot + P_UNIT);
        nint statePtr = Marshal.ReadIntPtr(_preClampDamageRewriteBuf, slot + P_STATE);
        int id = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_ID);
        int team = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_TEAM);
        int hp = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_HP);
        int maxHp = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_MAX_HP);
        int oldDebit = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_OLD_DEBIT);
        int oldCredit = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_OLD_CREDIT);
        int forcedDebit = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_FORCED_DEBIT);
        int forcedCredit = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_FORCED_CREDIT);
        int flags = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_FLAGS);
        int action = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_ACTION);
        byte[] unitDump = ReadPreClampBytes(slot, P_UNIT_DUMP, PRECLAMP_UNIT_DUMP_SIZE);
        UnitSnapshot? preClampUnit = TryCreateUnitSnapshot(unitPtr, unitDump, out var capturedUnit, out _)
            ? capturedUnit
            : null;
        string preClampFields = preClampUnit is null
            ? "pre=unreadable"
            : $"pre={ActionBoundaryFields(preClampUnit.Raw)}";
        UnitSnapshot? liveUnit = null;
        string live = TryReadLiveUnitSnapshot(unitPtr, out var unit, out string error)
            ? $"live={ActionBoundaryFields(unit.Raw)}"
            : $"live={error.Replace(' ', '_')}";
        if (unit is not null)
            liveUnit = unit;
        string actionText = action >= 0 ? $" action={action}" : "";
        nint[] registers = ReadPreClampRegisters(slot);
        byte[] stackDump = ReadPreClampBytes(slot, P_STACK_DUMP, PRECLAMP_STACK_DUMP_SIZE);
        byte[] stateDump = ReadPreClampBytes(slot, P_STATE_DUMP, PRECLAMP_STATE_DUMP_SIZE);
        UnitSnapshot? referenceUnit = preClampUnit ?? liveUnit;
        string regs = FormatPreClampRegisters(registers, referenceUnit);
        string stack = FormatCapturedStackSlots(stackDump, referenceUnit);
        string stackPart = stack.Length == 0 ? "" : $" stack={stack}";
        return
            $"[PRECLAMP-REWRITE event={sequence} ptr=0x{unitPtr:X} state=0x{statePtr:X} id=0x{id:X2} " +
            $"team={team} hp={hp}/{maxHp} oldDebit={oldDebit} oldCredit={oldCredit} " +
            $"forcedDebit={forcedDebit} forcedCredit={forcedCredit} flags=0x{flags:X}{actionText} now={nowTick} " +
            $"{preClampFields} {live}] regs={regs}{stackPart} stateDump={FormatHexBytes(stateDump)}";
    }

    private void CaptureResultSelectorProbeEvents(long nowTick)
    {
        if (_resultSelectorProbeBuf == 0 || !_settings.ResultSelectorProbeEnabled)
            return;

        int count = Marshal.ReadInt32(_resultSelectorProbeBuf, S_COUNT);
        if (count == _lastResultSelectorProbeSequence)
            return;

        bool needsFlush = false;
        int start = _lastResultSelectorProbeSequence + 1;
        if (count - _lastResultSelectorProbeSequence > SELECTOR_RING_SIZE)
        {
            int lost = count - _lastResultSelectorProbeSequence - SELECTOR_RING_SIZE;
            Line($"[SELECTOR-PROBE-LOST last={_lastResultSelectorProbeSequence} current={count} lost={lost}]");
            start = count - SELECTOR_RING_SIZE + 1;
            needsFlush = true;
        }

        int maxLogs = Math.Clamp(_settings.ResultSelectorProbeMaxLogs, 0, 100_000);
        for (int sequence = start; sequence <= count; sequence++)
        {
            int slot = S_EVENTS + ((sequence & SELECTOR_RING_MASK) * SELECTOR_SLOT_SIZE);
            int recordedSequence = Marshal.ReadInt32(_resultSelectorProbeBuf, slot + S_SEQ);
            if (recordedSequence != sequence)
                continue;
            if (_resultSelectorProbeLogs >= maxLogs)
                continue;

            Line(FormatResultSelectorProbeHit(sequence, slot, nowTick));
            _resultSelectorProbeLogs++;
            needsFlush = true;
        }

        _lastResultSelectorProbeSequence = count;
        if (needsFlush)
            Flush();
    }

    private string FormatResultSelectorProbeHit(int sequence, int slot, long nowTick)
    {
        int evadeType = Marshal.ReadInt32(_resultSelectorProbeBuf, slot + S_EVADE) & 0xFF;
        nint actorPtr = Marshal.ReadIntPtr(_resultSelectorProbeBuf, slot + S_ACTOR);
        nint recordPtr = Marshal.ReadIntPtr(_resultSelectorProbeBuf, slot + S_RECORD);

        // The copied window starts at SELECTOR_RECORD_DUMP_BASE (0x1B8) in the record. Map record
        // offsets into the window so the interesting fields can be read regardless of dump length.
        int dumpBytes = ResultSelectorRecordDumpBytes;
        var window = new byte[dumpBytes];
        if (dumpBytes > 0)
            Marshal.Copy(IntPtr.Add(_resultSelectorProbeBuf, slot + S_RECORD_DUMP), window, 0, dumpBytes);

        int RecB(int recordOffset)
        {
            int idx = recordOffset - SELECTOR_RECORD_DUMP_BASE;
            return idx >= 0 && idx < window.Length ? window[idx] : -1;
        }
        int RecW(int recordOffset)
        {
            int lo = RecB(recordOffset);
            int hi = RecB(recordOffset + 1);
            return lo < 0 || hi < 0 ? -1 : (lo | (hi << 8));
        }
        static string Hx(int value) => value < 0 ? "--" : value.ToString("X2");

        // Resolve the unit so the line carries id/team/hp like the pre-clamp formatter. The record
        // begins with the unit/char id at +0x00; the actor links to the same unit pointer family.
        UnitSnapshot? recordUnit = recordPtr != 0 && TryReadLiveUnitSnapshot(recordPtr, out var ru, out _) ? ru : null;
        string unitText = recordUnit is not null
            ? $"unit:id=0x{recordUnit.CharId:X2}/team={recordUnit.Team}/hp={recordUnit.Hp}/ct={recordUnit.Ct}"
            : (recordPtr == 0 ? "unit:none(record-null)" : $"unit:{ClassifyRegisterValue(recordPtr, null, "record")}");
        string actorText = FormatSelectorContextValue(actorPtr, recordUnit);
        string selectorContext = FormatSelectorContext(slot, recordUnit);

        // control marker written by BuildSelectorControlLines: 0 none, 1 would-write (LogOnly), 2 wrote
        int ctrlAction = Marshal.ReadByte(_resultSelectorProbeBuf, slot + S_CTRL_INFO);
        string ctrlText = "";
        if (ctrlAction != 0)
        {
            int forcedEvade = Marshal.ReadByte(_resultSelectorProbeBuf, slot + S_CTRL_INFO + 1);
            int forcedResult = Marshal.ReadByte(_resultSelectorProbeBuf, slot + S_CTRL_INFO + 2);
            string verb = ctrlAction == 2 ? "WROTE" : "would-write(LogOnly)";
            string feText = forcedEvade == 0xFF ? "--" : $"0x{forcedEvade:X2}({DescribeEvadeType(forcedEvade)})";
            string frText = forcedResult == 0xFF ? "--" : $"0x{forcedResult:X2}";
            ctrlText = $" [CONTROL {verb} evadeType={feText} resultCode={frText}]";
        }

        return
            $"[SELECTOR-PROBE event={sequence} evadeType=0x{evadeType:X2}({DescribeEvadeType(evadeType)}) " +
            $"actor=0x{actorPtr:X}:{actorText} record=0x{recordPtr:X} {unitText} now={nowTick} " +
            $"rec+1BB={Hx(RecB(0x1BB))} rec+1BE={Hx(RecB(0x1BE))} rec+1C0={Hx(RecB(0x1C0))} " +
            $"rec+1C4(dmg)={(RecW(0x1C4) < 0 ? "----" : RecW(0x1C4).ToString())} rec+1E5={Hx(RecB(0x1E5))}] " +
            $"window={FormatResultSelectorWindow(window)} {selectorContext}{ctrlText}";
    }

    private string FormatSelectorContext(int slot, UnitSnapshot? recordUnit)
    {
        var regParts = new List<string>();
        for (int i = 0; i < SELECTOR_CONTEXT_REG_COUNT && i < RegisterNames.Length; i++)
        {
            nint value = Marshal.ReadIntPtr(_resultSelectorProbeBuf, slot + S_CONTEXT_REGS + (i * IntPtr.Size));
            string text = FormatSelectorContextValue(value, recordUnit);
            if (IsSelectorContextInteresting(text))
                regParts.Add($"{RegisterNames[i]}=0x{value:X}:{text}");
        }

        var stackParts = new List<string>();
        for (int i = 0; i < SELECTOR_CONTEXT_STACK_SLOTS; i++)
        {
            nint value = Marshal.ReadIntPtr(_resultSelectorProbeBuf, slot + S_CONTEXT_STACK + (i * IntPtr.Size));
            string text = FormatSelectorContextValue(value, recordUnit);
            if (IsSelectorContextInteresting(text))
                stackParts.Add($"+0x{i * IntPtr.Size:X}=0x{value:X}:{text}");
        }

        string regs = regParts.Count == 0 ? "none" : string.Join(",", regParts);
        string stack = stackParts.Count == 0 ? "none" : string.Join(",", stackParts);
        return $"ctxRegs=[{regs}] ctxStack=[{stack}]";
    }

    private string FormatSelectorContextValue(nint value, UnitSnapshot? recordUnit)
    {
        if (value == 0)
            return "zero";

        int unitOff = ResultSelectorRecordUnitOffset;
        int actOff = Math.Clamp(_settings.PreClampActorActionIdOffset, 0, 0x4000);
        if (TryReadLivePtr(value + unitOff, out var linked) &&
            _unitObservations.TryGetValue(linked, out var obs))
        {
            int actionId = TryReadLiveU16(value + actOff, out var aid) ? aid : -1;
            string role = recordUnit is not null && linked == recordUnit.Ptr ? "actor:record-unit" : "actor";
            return $"{role}:id=0x{obs.Unit.CharId:X2}:unit=0x{linked:X}:act={actionId}";
        }

        return ClassifyRegisterValue(value, recordUnit, "selector-record");
    }

    private static bool IsSelectorContextInteresting(string text)
        => text.StartsWith("actor", StringComparison.Ordinal) ||
           text.StartsWith("unit", StringComparison.Ordinal);

    private static string DescribeEvadeType(int evadeType)
        => evadeType switch
        {
            // live-validated 2026-06-26 (see docs/modding/04-re-strategy.md)
            0x00 => "hit",
            0x01 => "cloak/accessory-evade",
            0x02 => "weapon-parry",
            0x03 => "shield-block",
            0x04 => "class-evade",
            0x06 => "miss",
            0x0B => "blade-grasp",
            _ => "unknown",
        };

    private static string FormatResultSelectorWindow(byte[] window)
    {
        if (window.Length == 0)
            return "(none)";
        var sb = new StringBuilder(window.Length * 3 + 8);
        sb.Append($"0x{SELECTOR_RECORD_DUMP_BASE:X3}:");
        for (int i = 0; i < window.Length; i++)
        {
            sb.Append(i % 16 == 0 ? (i == 0 ? " " : " | ") : " ");
            sb.Append(window[i].ToString("X2"));
        }
        return sb.ToString();
    }

    private byte[] ReadPreClampBytes(int slot, int offset, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(IntPtr.Add(_preClampDamageRewriteBuf, slot + offset), bytes, 0, length);
        return bytes;
    }

    private nint[] ReadPreClampRegisters(int slot)
    {
        var registers = new nint[REGISTER_COUNT];
        for (int i = 0; i < registers.Length; i++)
            registers[i] = Marshal.ReadIntPtr(_preClampDamageRewriteBuf, slot + P_REGS + (i * IntPtr.Size));
        return registers;
    }

    private string FormatPreClampRegisters(nint[] registers, UnitSnapshot? reference)
    {
        var parts = new List<string>(registers.Length);
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
            parts.Add($"{RegisterNames[i]}=0x{registers[i]:X}:{ClassifyRegisterValue(registers[i], reference, "preclamp")}");
        return string.Join(" ", parts);
    }

    private string FormatCapturedStackSlots(byte[] stackDump, UnitSnapshot? reference)
    {
        var parts = new List<string>();
        for (int offset = 0; offset + IntPtr.Size <= stackDump.Length; offset += IntPtr.Size)
        {
            long raw = BitConverter.ToInt64(stackDump, offset);
            if (raw == 0) continue;
            var value = (nint)raw;
            parts.Add($"+0x{offset:X}=0x{value:X}:{ClassifyRegisterValue(value, reference, "preclamp")}");
        }

        return string.Join(" ", parts);
    }

    private void LogPreClampPointerScanIfEnabled(int sequence, int slot, long nowTick)
    {
        int scanBytes = Math.Clamp(_settings.PreClampPointerScanBytes, 0, 0x4000);
        if (scanBytes <= 0) return;

        int maxLogs = Math.Clamp(_settings.PreClampPointerMaxLogs, 0, 1000);
        if (_preClampPointerScanLogs >= maxLogs) return;

        nint unitPtr = Marshal.ReadIntPtr(_preClampDamageRewriteBuf, slot + P_UNIT);
        int id = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_ID);
        byte[] unitDump = ReadPreClampBytes(slot, P_UNIT_DUMP, PRECLAMP_UNIT_DUMP_SIZE);
        UnitSnapshot? reference = TryCreateUnitSnapshot(unitPtr, unitDump, out var capturedUnit, out _)
            ? capturedUnit
            : null;
        nint[] registers = ReadPreClampRegisters(slot);
        byte[] stackDump = ReadPreClampBytes(slot, P_STACK_DUMP, PRECLAMP_STACK_DUMP_SIZE);
        int maxPointers = Math.Clamp(_settings.PreClampPointerMaxPointersPerRoot, 0, 64);

        var roots = new List<string>();
        var seenRoots = new HashSet<nint>();
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
            AddPointerScanRoot(RegisterNames[i], registers[i]);

        for (int offset = 0; offset + IntPtr.Size <= stackDump.Length; offset += IntPtr.Size)
        {
            nint value = (nint)BitConverter.ToInt64(stackDump, offset);
            AddPointerScanRoot($"stack+0x{offset:X}", value);
        }

        _preClampPointerScanLogs++;
        string body = roots.Count == 0 ? "no-readable-nonunit-roots" : string.Join(" ", roots);
        Line($"[PRECLAMP-PTRSCAN event={sequence} targetPtr=0x{unitPtr:X} id=0x{id:X2} now={nowTick}] {body}");

        void AddPointerScanRoot(string name, nint root)
        {
            if (root == 0) return;
            if (_unitObservations.ContainsKey(root)) return;
            if (!seenRoots.Add(root)) return;
            if (!ReadableMemoryRange.IsReadable(root, Math.Min(scanBytes, IntPtr.Size))) return;

            string summary = ScanPointerRoot(name, root, reference, scanBytes, maxPointers);
            if (summary.Length > 0)
                roots.Add(summary);
        }
    }

    private static string FormatHexBytes(byte[] raw)
    {
        var sb = new StringBuilder(raw.Length * 3);
        for (int i = 0; i < raw.Length; i++)
        {
            if (i > 0) sb.Append(i % 16 == 0 ? " | " : " ");
            sb.Append(raw[i].ToString("X2"));
        }
        return sb.ToString();
    }

    private nint[] ReadLandmarkRegisters(int slot)
    {
        var registers = new nint[REGISTER_COUNT];
        for (int i = 0; i < registers.Length; i++)
            registers[i] = Marshal.ReadIntPtr(_landmarkBuf, slot + L_REGS + (i * IntPtr.Size));
        return registers;
    }

    private string FormatLandmarkHit(int sequence, int probeId, LandmarkProbe probe, nint[] registers, long nowTick)
    {
        nint basePtr = TryGetRegister(registers, probe.BaseRegister, out var registerValue) ? registerValue : 0;
        UnitSnapshot? baseUnit = null;
        string baseRead = "baseRead=none";
        if (basePtr != 0)
        {
            if (TryReadLiveUnitSnapshot(basePtr, out var unit, out string error))
            {
                baseUnit = unit;
                baseRead = $"baseRead=unit:id=0x{unit.CharId:X2}/team={unit.Team}/hp={unit.Hp}/ct={unit.Ct}";
            }
            else
            {
                baseRead = $"baseRead={error.Replace(' ', '_')}";
            }
        }
        if (baseUnit is null)
            return "";

        string fields = $"fields={ActionBoundaryFields(baseUnit.Raw)}/raw={FormatLandmarkOffsets(baseUnit.Raw, probe.InterestingOffsets)}";
        string regs = FormatLandmarkRegisters(registers, baseUnit);
        string stack = FormatLandmarkStackSlots(registers.Length > 7 ? registers[7] : 0, baseUnit);
        string stackPart = stack.Length == 0 ? "" : $" stack={stack}";
        return
            $"[LANDMARK-HIT event={sequence} id={probeId} name={probe.TraceName} rva=0x{probe.Rva:X} " +
            $"access={probe.Access} base={probe.BaseRegister}=0x{basePtr:X}:{ClassifyRegisterValue(basePtr, baseUnit, "base")} " +
            $"now={nowTick} {baseRead} {fields}] regs={regs}{stackPart}";
    }

    private static bool TryGetRegister(nint[] registers, string name, out nint value)
    {
        value = 0;
        for (int i = 0; i < RegisterNames.Length && i < registers.Length; i++)
        {
            if (!string.Equals(RegisterNames[i], name, StringComparison.OrdinalIgnoreCase))
                continue;
            value = registers[i];
            return true;
        }

        return false;
    }

    private string FormatLandmarkRegisters(nint[] registers, UnitSnapshot? reference)
    {
        var parts = new List<string>(registers.Length);
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
            parts.Add($"{RegisterNames[i]}=0x{registers[i]:X}:{ClassifyRegisterValue(registers[i], reference, "base")}");
        return string.Join(" ", parts);
    }

    private string FormatLandmarkStackSlots(nint rsp, UnitSnapshot? reference)
    {
        int slots = Math.Clamp(_settings.LandmarkProbeStackSlots, 0, 64);
        if (slots == 0 || rsp == 0) return "";

        var parts = new List<string>(slots);
        for (int i = 0; i < slots; i++)
        {
            nint slotAddress = rsp + (i * IntPtr.Size);
            if (!ReadableMemoryRange.IsReadable(slotAddress, IntPtr.Size))
                continue;
            nint value;
            try
            {
                value = Marshal.ReadIntPtr(slotAddress);
            }
            catch
            {
                continue;
            }

            if (value == 0) continue;
            parts.Add($"+0x{i * IntPtr.Size:X}=0x{value:X}:{ClassifyRegisterValue(value, reference, "base")}");
        }

        return string.Join(",", parts);
    }

    private static string FormatLandmarkOffsets(byte[] raw, IReadOnlyList<int> offsets)
    {
        if (offsets.Count == 0)
            return "none";

        var parts = new List<string>(offsets.Count);
        foreach (int offset in offsets)
        {
            if (offset < 0 || offset >= raw.Length)
                continue;
            string value = raw[offset].ToString("X2");
            if (offset + 1 < raw.Length)
                value += raw[offset + 1].ToString("X2");
            parts.Add($"+0x{offset:X}={value}");
        }

        return parts.Count == 0 ? "none" : string.Join("/", parts);
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

        ApplyEvadeOverrideSweep();
        ApplyBraveOverrideSweep();
        ApplyStatusOverrideSweep();
        ApplyItemTableEvadeZeroIfEnabled();
    }

    // ITEM-TABLE EVADE ZERO — kills every equipment-evade leg at the SOURCE (RE 2026-07-03,
    // work/dcl-item-table-runtime-poke.md). The VM derives weapon/shield/accessory evade from item
    // stat tables baked at fixed VAs in a writable section (fn 0x2B8CB8 chain). Zeroing the per-item
    // evade bytes makes the VM's own derivation produce 0 for every unit, both teams, preview
    // included — the one lever a VM-internal reader cannot bypass (it reads DATA we own).
    //   Weapon    0x80F690 + aid*8, evade byte +5 (128 rows; +4 = WP, untouched)
    //   Shield    0x80FA90, 16 rows * 2 (phys, magic)
    //   Accessory 0x80FB30, 32 rows * 2 (phys, magic)
    // Sanity gate: shield row 14 (Venetian Shield) at 0x80FAAC must read 32 19 (50/25) before the
    // FIRST write; on mismatch the poke is disabled for the session (layout drift = game patch).
    // Re-poked every poll: the section is writable, so a VM-side loader may re-stamp between battles.
    private const int ITEMTABLE_WEAPON_RVA = 0x80F690;
    private const int ITEMTABLE_SHIELD_RVA = 0x80FA90;
    private const int ITEMTABLE_ACCESSORY_RVA = 0x80FB30;
    private const int ITEMTABLE_SANITY_RVA = 0x80FAAC;
    private bool _itemTableSanityChecked;
    private bool _itemTableSanityOk;

    private void ApplyItemTableEvadeZeroIfEnabled()
    {
        if (!_settings.ItemTableEvadeZeroEnabled || _moduleBase == 0)
            return;
        if (!_itemTableSanityChecked)
        {
            _itemTableSanityChecked = true;
            byte s0 = Marshal.ReadByte(_moduleBase + ITEMTABLE_SANITY_RVA);
            byte s1 = Marshal.ReadByte(_moduleBase + ITEMTABLE_SANITY_RVA + 1);
            _itemTableSanityOk = s0 == 0x32 && s1 == 0x19;
            Line(_itemTableSanityOk
                ? "[ITEMTABLE-EVADE-ZERO] sanity ok (VenetianShield 32 19) — zeroing weapon W-Ev + shield + accessory evade every poll"
                : $"[ITEMTABLE-EVADE-SKIP] sanity FAILED at rva=0x{ITEMTABLE_SANITY_RVA:X}: got {s0:X2} {s1:X2}, expected 32 19 (layout drift?) — poke disabled");
            Flush();
        }
        if (!_itemTableSanityOk)
            return;
        for (int i = 0; i < 128; i++)
            Marshal.WriteByte(_moduleBase + ITEMTABLE_WEAPON_RVA + i * 8 + 5, 0);
        for (int i = 0; i < 0x20; i++)
            Marshal.WriteByte(_moduleBase + ITEMTABLE_SHIELD_RVA + i, 0);
        for (int i = 0; i < 0x40; i++)
            Marshal.WriteByte(_moduleBase + ITEMTABLE_ACCESSORY_RVA + i, 0);
    }

    // Boost evade on EVERY unit in the battle, not just registry-tracked ones. The poller only tracks
    // units that a hook touched or discovery found; the defender of an attack may never be tracked (e.g.
    // Ramza was hit but never registered, so his evade was never overridden -> invalid test). We walk the
    // address span of the tracked units (which bracket the unit array, stride 0x200) plus a margin, read
    // each slot, and override any that validates as a unit. ReadProcessMemory is safe on unmapped pages.
    private void ApplyEvadeOverrideSweep()
    {
        if (!_settings.EvadeOverrideEnabled || _settings.EvadeOverrideTargetCharId >= 0)
            return;
        int slots = Math.Clamp(_settings.EvadeOverrideSweepSlots, 0, 256);
        if (slots == 0 || _unitRegistry.Count == 0)
            return;

        const int STRIDE = 0x200;
        nint min = nint.MaxValue, max = 0;
        foreach (var p in _unitRegistry)
        {
            if (p < min) min = p;
            if (p > max) max = p;
        }
        nint margin = (nint)slots * STRIDE;
        for (nint addr = min - margin; addr <= max + margin; addr += STRIDE)
        {
            if (_unitRegistry.Contains(addr))
                continue; // tracked units are already boosted by the poll loop
            if (TryReadLiveUnitSnapshot(addr, out var snap, out _))
                ApplyEvadeOverrideIfEnabled(snap);
        }
    }

    // Persistent INPUT-control test (the original idea): write a unit's avoidance INPUT bytes (weapon
    // evade +0x46/+0x47, shield evade +0x4A/+0x4E, class evade +0x4B) on its LIVE battle struct every
    // poll, BEFORE the engine's VM avoidance roll reads them. If the engine then honors these (the unit
    // dodges / the on-screen hit% drops when attacked), the VM reads live memory => input-control works.
    // If it ignores them (takes the hit at normal odds), the VM consumed a copy snapshotted earlier =>
    // input-control of evade is dead and we use output-control. Defender-agnostic: we set the struct of a
    // chosen unit and let it be attacked, so we never need to identify the defender at the roll moment.
    private void ApplyEvadeOverrideIfEnabled(UnitSnapshot target)
    {
        if (!_settings.EvadeOverrideEnabled)
            return;
        int want = _settings.EvadeOverrideTargetCharId;
        if (want >= 0 && target.CharId != (want & 0xFF))
            return;

        nint p = target.Ptr;
        bool changed = false;
        void W(int off, int val)
        {
            if (val < 0)
                return;
            if (target.ReadByte(off) != (val & 0xFF))
                changed = true;
            Marshal.WriteByte(p, off, (byte)(val & 0xFF));
        }
        W(0x46, _settings.EvadeOverride46);
        W(0x47, _settings.EvadeOverride47);
        W(0x48, _settings.EvadeOverride48);
        W(0x49, _settings.EvadeOverride49);
        W(0x4A, _settings.EvadeOverride4A);
        W(0x4B, _settings.EvadeOverride4B);
        W(0x4C, _settings.EvadeOverride4C);
        W(0x4D, _settings.EvadeOverride4D);
        W(0x4E, _settings.EvadeOverride4E);

        // Log only when we actually changed a value (first set, or the engine reset it between polls),
        // throttled, so persistent rewriting doesn't spam the log.
        if (changed && _evadeOverrideLogs < Math.Clamp(_settings.EvadeOverrideMaxLogs, 0, 100_000))
        {
            _evadeOverrideLogs++;
            static string F(int v) => v < 0 ? "--" : $"{v & 0xFF:X2}";
            Line(
                $"[EVADE-OVERRIDE ptr=0x{p:X} id=0x{target.CharId:X2}] " +
                $"was(46={target.ReadByte(0x46):X2} 47={target.ReadByte(0x47):X2} 49={target.ReadByte(0x49):X2} 4A={target.ReadByte(0x4A):X2} " +
                $"4B={target.ReadByte(0x4B):X2} 4E={target.ReadByte(0x4E):X2}) " +
                $"set(46={F(_settings.EvadeOverride46)} 47={F(_settings.EvadeOverride47)} 49={F(_settings.EvadeOverride49)} 4A={F(_settings.EvadeOverride4A)} " +
                $"4B={F(_settings.EvadeOverride4B)} 4E={F(_settings.EvadeOverride4E)})");
            Flush();
        }
    }

    // Same array-span sweep as evade, for the Brave/Faith bytes (reaction-control test). Independent
    // gating so we can run a Brave-only test (evade off, brave on) or vice-versa.
    private void ApplyBraveOverrideSweep()
    {
        if (!_settings.BraveOverrideEnabled || _settings.BraveOverrideTargetCharId >= 0)
            return;
        int slots = Math.Clamp(_settings.BraveOverrideSweepSlots, 0, 256);
        if (slots == 0 || _unitRegistry.Count == 0)
            return;

        const int STRIDE = 0x200;
        nint min = nint.MaxValue, max = 0;
        foreach (var p in _unitRegistry)
        {
            if (p < min) min = p;
            if (p > max) max = p;
        }
        nint margin = (nint)slots * STRIDE;
        for (nint addr = min - margin; addr <= max + margin; addr += STRIDE)
        {
            if (_unitRegistry.Contains(addr))
                continue; // tracked units are already handled by the poll loop
            if (TryReadLiveUnitSnapshot(addr, out var snap, out _))
                ApplyBraveOverrideIfEnabled(snap);
        }
    }

    // INPUT-control test for REACTIONS (Blade Grasp, Hamedo, Arrow Guard...). These trigger on a
    // Brave%-gated roll; the offline RE found the defender's Brave (+0x2B) is read by the engine and the
    // success/fail branch is REAL code (only the roll arithmetic is VM). Since evade input-control proved
    // the VM honors LIVE struct memory, writing the defender's Brave on its live struct before the roll
    // should control whether a reaction fires: Brave 0 => reaction never triggers (a forced hit lands);
    // Brave 100 => it always triggers. Bytes: +0x2A MaxBrave, +0x2B Brave, +0x2C MaxFaith, +0x2D Faith.
    private void ApplyBraveOverrideIfEnabled(UnitSnapshot target)
    {
        if (!_settings.BraveOverrideEnabled)
            return;
        int want = _settings.BraveOverrideTargetCharId;
        if (want >= 0 && target.CharId != (want & 0xFF))
            return;

        nint p = target.Ptr;
        bool changed = false;
        void W(int off, int val)
        {
            if (val < 0)
                return;
            if (target.ReadByte(off) != (val & 0xFF))
                changed = true;
            Marshal.WriteByte(p, off, (byte)(val & 0xFF));
        }
        W(0x2A, _settings.BraveOverride2A);
        W(0x2B, _settings.BraveOverride2B);
        W(0x2C, _settings.BraveOverride2C);
        W(0x2D, _settings.BraveOverride2D);

        if (changed && _braveOverrideLogs < Math.Clamp(_settings.BraveOverrideMaxLogs, 0, 100_000))
        {
            _braveOverrideLogs++;
            static string F(int v) => v < 0 ? "--" : $"{v & 0xFF:X2}";
            Line(
                $"[BRAVE-OVERRIDE ptr=0x{p:X} id=0x{target.CharId:X2}] " +
                $"was(2A={target.ReadByte(0x2A):X2} 2B={target.ReadByte(0x2B):X2} 2C={target.ReadByte(0x2C):X2} 2D={target.ReadByte(0x2D):X2}) " +
                $"set(2A={F(_settings.BraveOverride2A)} 2B={F(_settings.BraveOverride2B)} 2C={F(_settings.BraveOverride2C)} 2D={F(_settings.BraveOverride2D)})");
            Flush();
        }
    }

    // Same array-span sweep as evade/brave, for the STATUS bytes (status-control test). Unlike those, this
    // sweep ALSO runs for a specific TargetCharId so we can reliably reach ONE unit (e.g. Ramza) even if it
    // is untracked, WITHOUT broadcasting a turn-disabling status to every unit; the per-unit charId filter
    // in ApplyStatusOverrideIfEnabled restricts the write to the chosen unit.
    private void ApplyStatusOverrideSweep()
    {
        if (!_settings.StatusOverrideEnabled)
            return;
        int slots = Math.Clamp(_settings.StatusOverrideSweepSlots, 0, 256);
        if (slots == 0 || _unitRegistry.Count == 0)
            return;

        const int STRIDE = 0x200;
        nint min = nint.MaxValue, max = 0;
        foreach (var p in _unitRegistry)
        {
            if (p < min) min = p;
            if (p > max) max = p;
        }
        nint margin = (nint)slots * STRIDE;
        for (nint addr = min - margin; addr <= max + margin; addr += STRIDE)
        {
            if (_unitRegistry.Contains(addr))
                continue;
            if (TryReadLiveUnitSnapshot(addr, out var snap, out _))
                ApplyStatusOverrideIfEnabled(snap);
        }
    }

    // INPUT-control test for STATUS effects. Offline RE: the effective status byte +0x61 is recomputed in
    // real code as +0x61 = (+0x1EF & 0xF2) | +0x57, and the engine tests +0x61 everywhere (KO 0x20,
    // control-flip 0x10, petrify 0x40, can't-act 0x08). The infliction roll is VM but the status BYTES are
    // data the VM reads, so OR-ing a mask onto +0x1EF (master, survives recompute) AND +0x61 (effective)
    // forces a status; the 20ms poll re-applies per-turn-cleared bits. Each setting is an OR MASK (>=0) or
    // -1 to skip. +0x57 is the innate/equipment source (OR it too if the status is equip-sourced).
    private void ApplyStatusOverrideIfEnabled(UnitSnapshot target)
    {
        if (!_settings.StatusOverrideEnabled)
            return;
        int want = _settings.StatusOverrideTargetCharId;
        if (want >= 0 && target.CharId != (want & 0xFF))
            return;

        nint p = target.Ptr;
        bool changed = false;
        void Or(int off, int mask)
        {
            if (mask < 0)
                return;
            int cur = target.ReadByte(off);
            int next = cur | (mask & 0xFF);
            if (next != cur)
                changed = true;
            Marshal.WriteByte(p, off, (byte)next);
        }
        Or(0x1EF, _settings.StatusOverride1EF);
        Or(0x61, _settings.StatusOverride61);
        Or(0x57, _settings.StatusOverride57);

        if (changed && _statusOverrideLogs < Math.Clamp(_settings.StatusOverrideMaxLogs, 0, 100_000))
        {
            _statusOverrideLogs++;
            static string F(int v) => v < 0 ? "--" : $"{v & 0xFF:X2}";
            Line(
                $"[STATUS-OVERRIDE ptr=0x{p:X} id=0x{target.CharId:X2}] " +
                $"was(57={target.ReadByte(0x57):X2} 61={target.ReadByte(0x61):X2} 1EF={target.ReadByte(0x1EF):X2}) " +
                $"or(57={F(_settings.StatusOverride57)} 61={F(_settings.StatusOverride61)} 1EF={F(_settings.StatusOverride1EF)})");
            Flush();
        }
    }

    private void ProcessObservedUnit(UnitSnapshot target, long nowTick, bool touchForContext, bool logStructMapping)
    {
        nint unitPtr = target.Ptr;
        int id = target.CharId;
        int hp = target.Hp;
        int mp = target.Mp;
        bool needsFlush = false;
        _lastObservedRaw.TryGetValue(unitPtr, out var previousObservedRaw);

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
        ApplyEvadeOverrideIfEnabled(target);
        ApplyBraveOverrideIfEnabled(target);
        ApplyStatusOverrideIfEnabled(target);
        if (touchForContext)
            DiscoverNearbyBattleUnits(unitPtr);
        UnitSnapshot? actionProbeUnit = null;
        ActionProbeState? actionProbeState = null;
        if (_settings.LogActionStateChanges || _settings.TrackPendingActions || _settings.LogImmediateActionCandidatesOnEvent)
        {
            if (TryReadActionProbeSnapshot(unitPtr, id, out var probeUnit, out var probeState))
            {
                actionProbeUnit = probeUnit;
                actionProbeState = probeState;
                TrackActionProbeAges(unitPtr, probeState, nowTick);
            }
        }

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
            if (_settings.HookRegisterProbeOnCtDrop)
            {
                long probeEventIndex = Interlocked.Increment(ref _probeEventIndex);
                LogHookRegisterProbeEventIfEnabled("ctdrop", probeEventIndex, target, nowTick);
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
        RefreshUnitObservationSnapshot();

        if (actionProbeUnit is not null && actionProbeState is not null)
        {
            if (ObservePendingActionStateIfEnabled(actionProbeUnit, actionProbeState, nowTick, touchForContext))
                needsFlush = true;
            if (LogActionStateChangeIfEnabled(actionProbeUnit, actionProbeState, nowTick, touchForContext))
                needsFlush = true;
            if (LogPreClampFormulaCandidateIfEnabled(actionProbeUnit, actionProbeState, nowTick))
                needsFlush = true;
            if (QueueEagerImmediatePreClampFormulaPlansIfEnabled(actionProbeUnit, actionProbeState, nowTick))
                needsFlush = true;
            if (ReevaluatePreClampFormulaTargetsForImmediateActionIfEnabled(actionProbeUnit, actionProbeState, nowTick))
                needsFlush = true;
        }

        if (LogActionBoundaryProbeIfEnabled(target, previousObservedRaw, nowTick, touchForContext))
            needsFlush = true;

        if (actionProbeUnit is null && _settings.TrackPendingActions)
        {
            var lines = _pendingActionTracker.ForgetUnit(unitPtr);
            foreach (string line in lines)
                Line(line);
            if (lines.Count > 0)
                needsFlush = true;
        }

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
                LogHpEventProbeIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, previousObservedRaw, prev, hp, signedDamage, actionProbeState);
                LogHookRegisterProbeEventIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, nowTick);
                LogPendingActionCandidatesIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, nowTick);
                LogImmediateActionCandidatesIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, actionProbeState, prev, hp, signedDamage, nowTick);
                PendingActionMatch? pendingCandidate = null;
                PendingActionMatch? pendingMatch = null;
                if (signedDamage != 0 && RefreshPendingActionTrackerForEvent(nowTick))
                    needsFlush = true;
                if (signedDamage != 0)
                {
                    var pendingResult = MatchPendingActionIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, actionProbeState, signedDamage, nowTick);
                    pendingCandidate = pendingResult.Match;
                    pendingMatch = ShouldUsePendingActionMatch(pendingCandidate) ? pendingCandidate : null;
                    if (pendingResult.Logged)
                        needsFlush = true;
                }
                long eventSeed = ComputeEventSeed(target, eventIndex, prev, hp, signedDamage);
                var fallbackAttacker = _contextResolver.ResolveRecentAttacker(target, _unitObservations, nowTick);
                UnitSnapshot? attackerUnit = pendingMatch?.Caster ?? fallbackAttacker.Unit;
                string attackerSource = pendingMatch?.Source ?? fallbackAttacker.Source;
                ActionSignal? pendingAction = pendingMatch is null ? null : BuildPendingActionSignal(pendingMatch);
                if (_settings.LogAttackerCandidates)
                {
                    string resolved = attackerUnit is null
                        ? "resolved=none"
                        : $"resolved=0x{attackerUnit.Ptr:X} source={attackerSource}";
                    if (pendingMatch is not null)
                    {
                        string fallback = fallbackAttacker.Unit is null
                            ? "fallback=none"
                            : $"fallback=0x{fallbackAttacker.Unit.Ptr:X} fallbackSource={fallbackAttacker.Source}";
                        string pending =
                            $"pending=batch={pendingMatch.BatchId}/act={pendingMatch.ActionId}/event={pendingMatch.BatchEvent}/{pendingMatch.MaxBatchEvents}" +
                            $"/confidence={pendingMatch.Confidence}/score={pendingMatch.Score}";
                        Line($"[CTX ptr=0x{unitPtr:X} id=0x{id:X2}] {resolved} {pending} {fallback} {fallbackAttacker.Summary}");
                    }
                    else
                    {
                        string rejected = pendingCandidate is null
                            ? ""
                            : $"pendingRejected=batch={pendingCandidate.BatchId}/act={pendingCandidate.ActionId}/confidence={pendingCandidate.Confidence}/reason=no-hp-cache-match ";
                        Line($"[CTX ptr=0x{unitPtr:X} id=0x{id:X2}] {resolved} {rejected}{fallbackAttacker.Summary}");
                    }
                }
                trackingHp = MaybeRewriteHpEvent(new DamageEvent(target, prev, hp, signedDamage, attackerUnit, attackerSource, pendingAction, EventIndex: eventIndex, EventSeed: eventSeed));
                _contextResolver.RememberHpDamageEvent(target, attackerUnit, attackerSource, signedDamage, nowTick, eventIndex);
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
                LogHookRegisterProbeEventIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, nowTick);
                LogPendingActionCandidatesIfEnabled(eventTag.ToLowerInvariant(), eventIndex, target, nowTick);
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
        _lastObservedRaw[unitPtr] = target.Raw;

        if (needsFlush) Flush();
    }

    private void RefreshUnitObservationSnapshot()
    {
        if (_unitObservations.Count == 0)
        {
            Volatile.Write(ref _unitObservationSnapshot, Array.Empty<UnitObservationView>());
            return;
        }

        var snapshot = new UnitObservationView[_unitObservations.Count];
        int index = 0;
        foreach (var observation in _unitObservations.Values)
        {
            var unit = observation.Unit;
            snapshot[index++] = new UnitObservationView(
                unit.Ptr,
                unit.CharId,
                unit.Team,
                unit.Hp,
                unit.MaxHp,
                unit.Pa,
                unit.Ma,
                unit.Brave,
                unit.Faith);
        }

        if (index != snapshot.Length)
            Array.Resize(ref snapshot, index);
        Volatile.Write(ref _unitObservationSnapshot, snapshot);
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
        if (team > 16)
        {
            error = $"invalid team {team}";
            return false;
        }
        if (ct > 100)
        {
            error = $"invalid CT {ct}";
            return false;
        }
        if (pa > 127 || ma > 127 || spd > 127)
        {
            error = $"invalid combat stats PA/MA/Sp {pa}/{ma}/{spd}";
            return false;
        }
        if (mov > 32 || jmp > 32)
        {
            error = $"invalid mobility Mv/Jp {mov}/{jmp}";
            return false;
        }
        if (br > 100 || fa > 100)
        {
            error = $"invalid Brave/Faith {br}/{fa}";
            return false;
        }

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
        _lastActionProbeState.Remove(unitPtr);
        _lastActionProbeChangeTick.Remove(unitPtr);
        _lastActionProbeActionId.Remove(unitPtr);
        _lastActionProbeActionIdChangeTick.Remove(unitPtr);
        _lastActionProbeActiveActionId.Remove(unitPtr);
        _lastActionProbeActiveActionChangeTick.Remove(unitPtr);
        foreach (string line in _pendingActionTracker.ForgetUnit(unitPtr))
            Line(line);
        _dumped.Remove(unitPtr);
        _lastRaw.Remove(unitPtr);
        _diffCounts.Remove(unitPtr);
        _unitObservations.Remove(unitPtr);
        RefreshUnitObservationSnapshot();
        _recentAliveRaw.Remove(unitPtr);
        _deathCaptureRaw.Remove(unitPtr);
        _deathCaptureFollow.Remove(unitPtr);
        _deathCaptured.Remove(unitPtr);
        _lastObservedRaw.Remove(unitPtr);
        Line($"[UNIT-LOST ptr=0x{unitPtr:X}] {reason}");
        Flush();
    }

    private int MaxTrackedBattleUnits => Math.Clamp(_settings.MaxTrackedBattleUnits, 1, 512);

    private int PlanSlotCount => Math.Clamp(_settings.PreClampFormulaPlanSlots, 1, PRECLAMP_PLAN_MAX_SLOTS);

    private sealed record PreClampFormulaPlanContext(string Source, int ActionId, long BatchId)
    {
        public string LogText => BatchId >= 0
            ? $"pending=batch={BatchId}/act={ActionId} context={Source}/act={ActionId}"
            : $"pending=none context={Source}/act={ActionId}";

        public bool IsImmediateAction => Source.Equals("immediate-action", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ImmediatePreClampActionMatch(
        UnitSnapshot Unit,
        ActionProbeState State,
        ImmediateActionCandidateScore Score,
        int StateAgeMs,
        int SeenAgeMs,
        int CtDropAgeMs,
        int ActionIdAgeMs,
        int ActiveActionAgeMs,
        int RunnerUpScore,
        int Margin);

    private static int ElapsedMs(long nowTick, long previousTick)
        => (int)Math.Round((nowTick - previousTick) * 1000.0 / Stopwatch.Frequency);

    private void PrunePreClampFormulaPlans(long nowTick)
    {
        int windowMs = Math.Clamp(_settings.PreClampFormulaPlanWindowMs, 1, 60_000);
        foreach (var (key, tick) in _preClampFormulaPlanKeys.ToArray())
        {
            if (ElapsedMs(nowTick, tick) <= windowMs)
                continue;
            _preClampFormulaPlanKeys.Remove(key);
        }

        if (_preClampDamageRewriteBuf == 0)
            return;

        int slots = PlanSlotCount;
        for (int i = 0; i < slots; i++)
        {
            int slot = P_PLAN_TABLE + (i * PRECLAMP_PLAN_SLOT_SIZE);
            if (Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + PLAN_ACTIVE) == 0)
                continue;

            long createdTick = Marshal.ReadInt64(_preClampDamageRewriteBuf, slot + PLAN_CREATED_TICK);
            if (createdTick > 0 && ElapsedMs(nowTick, createdTick) <= windowMs)
                continue;

            Marshal.WriteInt32(_preClampDamageRewriteBuf, slot + PLAN_ACTIVE, 0);
        }
    }

    private bool TryQueuePreClampFormulaPlan(
        string key,
        UnitSnapshot target,
        PreClampFormulaPlanContext? planContext,
        int oldDebit,
        int oldCredit,
        int forcedDebit,
        int forcedCredit,
        long nowTick)
    {
        if (!_settings.PreClampFormulaPlanEnabled || _preClampDamageRewriteBuf == 0)
            return false;

        PrunePreClampFormulaPlans(nowTick);
        if (_preClampFormulaPlanKeys.ContainsKey(key))
            return false;

        int slots = PlanSlotCount;
        int chosenSlot = -1;
        for (int i = 0; i < slots; i++)
        {
            int slot = P_PLAN_TABLE + (i * PRECLAMP_PLAN_SLOT_SIZE);
            if (Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + PLAN_ACTIVE) != 0)
                continue;
            chosenSlot = i;
            break;
        }

        if (chosenSlot < 0)
        {
            Line($"[PRECLAMP-PLAN-SKIP ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] no free plan slots ({slots})");
            return true;
        }

        int offset = P_PLAN_TABLE + (chosenSlot * PRECLAMP_PLAN_SLOT_SIZE);
        bool isImmediatePlan = planContext?.IsImmediateAction == true;
        int maxWrites = isImmediatePlan
            ? Math.Clamp(_settings.PreClampImmediateActionPlanMaxWrites, 1, 16)
            : Math.Clamp(_settings.PreClampFormulaPlanMaxWrites, 1, 16);
        int expectedHp = isImmediatePlan && !_settings.PreClampImmediateActionPlanRequireExpectedHp
            ? -1
            : target.Hp;
        int clampedDebit = forcedDebit < 0 ? -1 : Math.Clamp(forcedDebit, short.MinValue, short.MaxValue);
        int clampedCredit = forcedCredit < 0 ? -1 : Math.Clamp(forcedCredit, short.MinValue, short.MaxValue);
        int flags = (_settings.PreClampDamageRewriteLogOnly ? 1 : 0) |
                    (clampedDebit >= 0 && !_settings.PreClampDamageRewriteLogOnly ? 2 : 0) |
                    (clampedCredit >= 0 && !_settings.PreClampDamageRewriteLogOnly ? 4 : 0) |
                    8;

        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_ACTIVE, 0);
        Marshal.WriteIntPtr(_preClampDamageRewriteBuf, offset + PLAN_TARGET, target.Ptr);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_EXPECTED_DEBIT, oldDebit);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_EXPECTED_CREDIT, oldCredit);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_FORCED_DEBIT, clampedDebit);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_FORCED_CREDIT, clampedCredit);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_MAX_WRITES, maxWrites);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_WRITE_COUNT, 0);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_FLAGS, flags);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_ACTION, planContext?.ActionId ?? -1);
        Marshal.WriteInt64(_preClampDamageRewriteBuf, offset + PLAN_BATCH, planContext?.BatchId ?? -1);
        Marshal.WriteInt64(_preClampDamageRewriteBuf, offset + PLAN_CREATED_TICK, nowTick);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_EXPECTED_HP, expectedHp);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_EXPECTED_MAX_HP, target.MaxHp);
        Marshal.WriteInt32(_preClampDamageRewriteBuf, offset + PLAN_ACTIVE, 1);
        _preClampFormulaPlanKeys[key] = nowTick;

        string pending = planContext?.LogText ?? "pending=none context=none";
        Line(
            $"[PRECLAMP-PLAN-QUEUE slot={chosenSlot} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2} " +
            $"hp={target.Hp}/{target.MaxHp} oldDebit={FormatMaybeInt(oldDebit)} oldCredit={FormatMaybeInt(oldCredit)} " +
            $"forcedDebit={clampedDebit} forcedCredit={clampedCredit} maxWrites={maxWrites} flags=0x{flags:X} " +
            $"expectedHp={FormatMaybeInt(expectedHp)} {pending} now={nowTick}]");
        return true;
    }

    private void TrackActionProbeAges(nint unitPtr, ActionProbeState state, long nowTick)
    {
        if (!_lastActionProbeActionId.TryGetValue(unitPtr, out int previousActionId) ||
            previousActionId != state.ActionId)
        {
            _lastActionProbeActionId[unitPtr] = state.ActionId;
            _lastActionProbeActionIdChangeTick[unitPtr] = nowTick;
        }

        int activeActionId = state.IsActiveSourceMarker ? state.ActionId > 0 ? state.ActionId : -1 : 0;
        if (!_lastActionProbeActiveActionId.TryGetValue(unitPtr, out int previousActiveActionId) ||
            previousActiveActionId != activeActionId)
        {
            _lastActionProbeActiveActionId[unitPtr] = activeActionId;
            _lastActionProbeActiveActionChangeTick[unitPtr] = nowTick;
        }
    }

    private int ActionIdAgeMs(nint unitPtr, long nowTick)
        => _lastActionProbeActionIdChangeTick.TryGetValue(unitPtr, out long tick)
            ? ElapsedMs(nowTick, tick)
            : -1;

    private int ActiveActionAgeMs(nint unitPtr, long nowTick)
        => _lastActionProbeActiveActionChangeTick.TryGetValue(unitPtr, out long tick)
            ? ElapsedMs(nowTick, tick)
            : -1;

    private void DiscoverNearbyBattleUnits(nint anchor)
    {
        if (!_settings.PreClampImmediateActionPlanEagerTargets)
            return;

        int radius = Math.Clamp(_settings.PreClampImmediateActionNearbyUnitScanRadius, 0, 32);
        if (radius <= 0)
            return;

        long anchorValue = anchor.ToInt64();
        bool changed = false;
        for (int delta = -radius; delta <= radius; delta++)
        {
            if (_unitRegistry.Count >= MaxTrackedBattleUnits)
                break;

            long candidateValue = anchorValue + ((long)delta * BattleUnitStride);
            if (candidateValue <= 0)
                continue;

            nint candidatePtr = new(candidateValue);
            if (_unitRegistry.Contains(candidatePtr))
                continue;

            if (!TryReadLiveUnitSnapshot(candidatePtr, out var unit, out _))
                continue;

            _unitRegistry.Add(candidatePtr);
            changed |= _unitObservations.TryAdd(candidatePtr, new UnitObservation(unit, SeenTick: 0, CtDropTick: 0, CtDropAmount: 0));
        }
        if (changed)
            RefreshUnitObservationSnapshot();
    }

    private bool QueueEagerImmediatePreClampFormulaPlansIfEnabled(
        UnitSnapshot source,
        ActionProbeState sourceState,
        long nowTick)
    {
        if (!_settings.PreClampFormulaCandidateAllowImmediateAction ||
            !_settings.PreClampFormulaPlanEnabled ||
            !_settings.PreClampImmediateActionPlanEagerTargets ||
            !_settings.TrackPendingActions ||
            !sourceState.IsActiveSourceMarker)
        {
            return false;
        }

        int maxAgeMs = Math.Clamp(_settings.PreClampImmediateActionMaxAgeMs, 1, 60_000);
        int minScore = _settings.PreClampImmediateActionMinScore;
        int stateAgeMs = _lastActionProbeChangeTick.TryGetValue(source.Ptr, out long actionChangeTick)
            ? ElapsedMs(nowTick, actionChangeTick)
            : -1;
        int actionIdAgeMs = ActionIdAgeMs(source.Ptr, nowTick);
        int activeActionAgeMs = ActiveActionAgeMs(source.Ptr, nowTick);
        _unitObservations.TryGetValue(source.Ptr, out var sourceObservation);
        int seenAgeMs = sourceObservation is not null && sourceObservation.SeenTick > 0
            ? ElapsedMs(nowTick, sourceObservation.SeenTick)
            : -1;
        int ctDropAgeMs = sourceObservation is not null && sourceObservation.CtDropTick > 0
            ? ElapsedMs(nowTick, sourceObservation.CtDropTick)
            : -1;
        var score = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                false,
                source.Hp,
                sourceState.ActionId,
                sourceState.HasPrimaryPendingFlag,
                sourceState.HasSecondaryPendingFlag,
                sourceState.PendingTimer,
                sourceState.ActiveMarker2,
                stateAgeMs,
                seenAgeMs,
                ctDropAgeMs,
                sourceState.ForecastDamage,
                actionIdAgeMs,
                activeActionAgeMs)
            {
                AllowZeroActionIdActiveSource = _settings.PreClampImmediateActionAllowZeroActionId,
            });
        bool currentActiveAllowed = !_settings.PreClampImmediateActionRequireFreshActive && score.CurrentActiveAction;
        bool freshEnough = _settings.PreClampImmediateActionRequireFreshActive
            ? score.FreshActiveAction && activeActionAgeMs >= 0 && activeActionAgeMs <= maxAgeMs
            : currentActiveAllowed ||
              (score.FreshActiveAction && activeActionAgeMs >= 0 && activeActionAgeMs <= maxAgeMs) ||
              (score.FreshActionId && actionIdAgeMs >= 0 && actionIdAgeMs <= maxAgeMs);
        bool hasActionIdentity = sourceState.ActionId > 0 ||
                                 (_settings.PreClampImmediateActionAllowZeroActionId && sourceState.IsActiveSourceMarker);
        if (!score.SourceLike || !freshEnough || score.Score < minScore || !hasActionIdentity)
            return false;

        var sourceMatch = new ImmediatePreClampActionMatch(
            source,
            sourceState,
            score,
            stateAgeMs,
            seenAgeMs,
            ctDropAgeMs,
            actionIdAgeMs,
            activeActionAgeMs,
            RunnerUpScore: int.MinValue,
            Margin: int.MaxValue);

        bool loggedOrQueued = false;
        bool canLogCandidate = _settings.LogPreClampFormulaCandidates &&
                               _preClampFormulaCandidateLogs < Math.Clamp(_settings.PreClampFormulaCandidateMaxLogs, 0, 1000);
        long sourceEpoch = _lastActionProbeActiveActionChangeTick.TryGetValue(source.Ptr, out long epoch)
            ? epoch
            : nowTick;

        foreach (nint ptr in _unitRegistry.OrderBy(p => p).ToArray())
        {
            if (ptr == source.Ptr)
                continue;

            if (!_unitObservations.TryGetValue(ptr, out var known))
            {
                if (!TryReadLiveUnitSnapshot(ptr, out var discovered, out _))
                    continue;
                known = new UnitObservation(discovered, SeenTick: 0, CtDropTick: 0, CtDropAmount: 0);
                _unitObservations[ptr] = known;
                RefreshUnitObservationSnapshot();
            }

            if (!TryReadActionProbeSnapshot(ptr, known.Unit.CharId, out var target, out var targetState))
                continue;
            TrackActionProbeAges(ptr, targetState, nowTick);
            if (target.Hp <= 0)
                continue;

            long eventIndex = Interlocked.Increment(ref _probeEventIndex);
            var actionSignal = BuildImmediateActionSignal(sourceMatch, targetState);
            const int syntheticDamage = 1;
            int syntheticCurrentHp = Math.Max(0, target.Hp - syntheticDamage);
            long eventSeed = ComputeEventSeed(target, eventIndex, target.Hp, syntheticCurrentHp, syntheticDamage);
            var stagedEvent = new DamageEvent(
                target,
                target.Hp,
                syntheticCurrentHp,
                syntheticDamage,
                source,
                "immediate-action",
                actionSignal,
                eventIndex,
                eventSeed);
            var formulaResult = _formulaEngine.EvaluateForStagedApply(stagedEvent);
            if (!formulaResult.ShouldRewrite || !stagedEvent.IsDamage)
                continue;

            int forcedDebit = Math.Clamp(formulaResult.FinalDamage, 0, 9999);
            string key = string.Join("|",
                "eager-immediate",
                source.Ptr.ToString("X"),
                ptr.ToString("X"),
                sourceEpoch,
                sourceState.ActionId,
                forcedDebit,
                formulaResult.RuleName);
            bool queuedPlan = TryQueuePreClampFormulaPlan(
                key,
                target,
                new PreClampFormulaPlanContext("immediate-action", sourceState.ActionId, -1),
                PlanExpectedPositiveDebit,
                oldCredit: 0,
                forcedDebit,
                forcedCredit: 0,
                nowTick);
            loggedOrQueued |= queuedPlan;

            if (!canLogCandidate)
                continue;

            _preClampFormulaCandidateLogs++;
            canLogCandidate = _preClampFormulaCandidateLogs < Math.Clamp(_settings.PreClampFormulaCandidateMaxLogs, 0, 1000);
            Line(
                $"[PRECLAMP-EAGER-PLAN-CANDIDATE event={eventIndex} source=0x{source.Ptr:X}/id=0x{source.CharId:X2} " +
                $"target=0x{target.Ptr:X}/id=0x{target.CharId:X2} hp={target.Hp}/{target.MaxHp} " +
                $"forcedDebit={forcedDebit} shouldStage=1 queuedPlan={(queuedPlan ? 1 : 0)} rule={formulaResult.RuleName} " +
                $"score={score.Score} currentActive={(score.CurrentActiveAction ? 1 : 0)} actionAge={actionIdAgeMs}ms " +
                $"activeAge={activeActionAgeMs}ms syntheticVanilla={syntheticDamage} now={nowTick} action={sourceState.AllFields}]");
            if (_settings.LogResolvedRuntimeContext && !string.IsNullOrWhiteSpace(formulaResult.Trace))
                Line($"[PRECLAMP-EAGER-RUNTIME source=0x{source.Ptr:X}/id=0x{source.CharId:X2} target=0x{target.Ptr:X}/id=0x{target.CharId:X2}] {formulaResult.Trace}");
            loggedOrQueued = true;
        }

        return loggedOrQueued;
    }

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

    private static string ResolveModPath(string path, string modDirectory, string defaultFile = "item_catalog.csv")
    {
        if (string.IsNullOrWhiteSpace(path)) path = defaultFile;
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

        string nextAbilityCatalogPath = ResolveModPath(nextSettings.AbilityCatalogPath, _modDirectory, "wotl_ability_action_baseline.csv");
        DateTime abilityCatalogWrite = File.Exists(nextAbilityCatalogPath)
            ? File.GetLastWriteTimeUtc(nextAbilityCatalogPath)
            : DateTime.MinValue;
        bool abilityCatalogChanged = force ||
                                     settingsChanged ||
                                     !nextAbilityCatalogPath.Equals(_abilityCatalogPath, StringComparison.OrdinalIgnoreCase) ||
                                     abilityCatalogWrite != _abilityCatalogLastWriteUtc;

        AbilityCatalog nextAbilityCatalog = _abilityCatalog;
        if (abilityCatalogChanged)
            nextAbilityCatalog = AbilityCatalog.Load(nextAbilityCatalogPath);

        if (!settingsChanged && !catalogChanged && !abilityCatalogChanged) return;

        _settings = nextSettings;
        _itemCatalog = nextCatalog;
        _abilityCatalog = nextAbilityCatalog;
        _formulaEngine = new BattleFormulaEngine(_settings, _itemCatalog, _abilityCatalog);
        _contextResolver = new BattleContextResolver(_settings);
        if (settingsChanged)
            _pendingActionTracker.Reset();
        _settingsLastWriteUtc = settingsWrite;
        _catalogPath = nextCatalogPath;
        _catalogLastWriteUtc = catalogWrite;
        _abilityCatalogPath = nextAbilityCatalogPath;
        _abilityCatalogLastWriteUtc = abilityCatalogWrite;

        string tag = force ? "RUNTIME-INIT" : settingsChanged ? "SETTINGS-RELOAD" : catalogChanged ? "CATALOG-RELOAD" : "ABILITY-CATALOG-RELOAD";
        Line($"[{tag}] settings: {_settings.Describe()}");
        Line($"[{tag}] item catalog: {_itemCatalog.Describe()}");
        Line($"[{tag}] ability catalog: {_abilityCatalog.Describe()}");
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

    private void LogHpEventProbeIfEnabled(
        string kind,
        long eventIndex,
        UnitSnapshot target,
        byte[]? previousRaw,
        int previousHp,
        int currentHp,
        int signedDamage,
        ActionProbeState? actionProbeState)
    {
        if (!_settings.LogHpEventProbe) return;

        int maxLogs = Math.Clamp(_settings.HpEventProbeMaxLogs, 0, 1000);
        if (_hpEventProbeLogs >= maxLogs) return;

        _hpEventProbeLogs++;
        int absoluteDelta = Math.Abs(signedDamage);
        bool isDamage = signedDamage > 0;
        bool isLethal = isDamage && currentHp <= 0;
        int appliedHpLoss = isDamage ? absoluteDelta : 0;
        int appliedHpGain = isDamage ? 0 : absoluteDelta;
        int rawForecastDamage = actionProbeState?.ForecastDamage ?? 0;
        int overkill = isDamage ? Math.Max(0, absoluteDelta - Math.Max(0, previousHp)) : 0;
        int rawForecastOverkill = isDamage && rawForecastDamage > 0
            ? Math.Max(0, rawForecastDamage - Math.Max(0, previousHp))
            : 0;
        bool hpClamp = isDamage && rawForecastDamage > appliedHpLoss && currentHp <= 0;
        int diffMax = Math.Clamp(_settings.HpEventProbeDiffMax, 0, 256);
        string rawDiff = previousRaw is null
            ? "no-raw-baseline"
            : string.Join(" ", StructByteDiffs(previousRaw, target.Raw, 0, DUMP - 1, diffMax));
        if (string.IsNullOrWhiteSpace(rawDiff))
            rawDiff = "none";

        string actionFields = actionProbeState is null ? "none" : actionProbeState.AllFields;
        Line(
            $"[HP-EVENT-PROBE kind={kind} event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2} " +
            $"prevHp={previousHp} currentHp={currentHp} delta={absoluteDelta} appliedHpLoss={appliedHpLoss} " +
            $"appliedHpGain={appliedHpGain} rawForecastDamage={rawForecastDamage} lethal={(isLethal ? 1 : 0)} " +
            $"hpClamp={(hpClamp ? 1 : 0)} overkill={overkill} rawForecastOverkill={rawForecastOverkill} " +
            $"maxHp={target.MaxHp} team={target.Team} foe={(target.IsFoe ? 1 : 0)} " +
            $"ct={target.Ct} action={actionFields}] diff={rawDiff}");

        if (!_settings.HpEventProbeDumpRaw) return;

        if (previousRaw is not null)
            Line($"[HP-EVENT-PRE-RAW event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {HexDump(previousRaw)}");
        else
            Line($"[HP-EVENT-PRE-RAW event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] no-raw-baseline");
        Line($"[HP-EVENT-POST-RAW event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {HexDump(target.Raw)}");
    }

    private void LogImmediateActionCandidatesIfEnabled(
        string kind,
        long eventIndex,
        UnitSnapshot target,
        ActionProbeState? targetState,
        int previousHp,
        int currentHp,
        int signedDamage,
        long nowTick)
    {
        if (!_settings.LogImmediateActionCandidatesOnEvent) return;

        int maxUnits = Math.Clamp(_settings.ImmediateActionCandidateMaxUnits, 1, 128);
        int appliedHpLoss = signedDamage > 0 ? Math.Abs(signedDamage) : 0;
        int rawTargetForecastDamage = targetState?.ForecastDamage ?? 0;
        bool hpClamp = signedDamage > 0 &&
                       currentHp <= 0 &&
                       rawTargetForecastDamage > appliedHpLoss;
        var candidates = new List<(int Score, string Text)>();

        foreach (nint ptr in _unitRegistry.OrderBy(p => p))
        {
            _unitObservations.TryGetValue(ptr, out var known);
            int expectedId = known?.Unit.CharId ?? -1;
            if (!TryReadActionProbeSnapshot(ptr, expectedId, out var unit, out var state))
                continue;
            TrackActionProbeAges(ptr, state, nowTick);

            bool isTarget = ptr == target.Ptr;
            int stateAgeMs = _lastActionProbeChangeTick.TryGetValue(ptr, out long actionChangeTick)
                ? ElapsedMs(nowTick, actionChangeTick)
                : -1;
            int actionIdAgeMs = ActionIdAgeMs(ptr, nowTick);
            int activeActionAgeMs = ActiveActionAgeMs(ptr, nowTick);
            int seenAgeMs = known is not null && known.SeenTick > 0 ? ElapsedMs(nowTick, known.SeenTick) : -1;
            int ctDropAgeMs = known is not null && known.CtDropTick > 0 ? ElapsedMs(nowTick, known.CtDropTick) : -1;
            int rawDamage = state.ForecastDamage;
            bool exactAppliedMatch = appliedHpLoss > 0 && rawDamage == appliedHpLoss;
            bool lethalClampMatch = appliedHpLoss > 0 &&
                                    currentHp <= 0 &&
                                    previousHp > 0 &&
                                    rawDamage >= previousHp;
            var scoreResult = ImmediateActionCandidateScoring.Evaluate(
                new ImmediateActionCandidateScoreInput(
                    isTarget,
                    unit.Hp,
                    state.ActionId,
                    state.HasPrimaryPendingFlag,
                    state.HasSecondaryPendingFlag,
                    state.PendingTimer,
                    state.ActiveMarker2,
                    stateAgeMs,
                    seenAgeMs,
                    ctDropAgeMs,
                    rawDamage,
                    actionIdAgeMs,
                    activeActionAgeMs)
                {
                    AllowZeroActionIdActiveSource = _settings.PreClampImmediateActionAllowZeroActionId,
                });
            bool relevant = state.LooksRelevant ||
                            scoreResult.SourceLike ||
                            isTarget ||
                            (stateAgeMs >= 0 && stateAgeMs <= 5000) ||
                            (actionIdAgeMs >= 0 && actionIdAgeMs <= 5000) ||
                            (activeActionAgeMs >= 0 && activeActionAgeMs <= 5000) ||
                            (seenAgeMs >= 0 && seenAgeMs <= 5000) ||
                            (ctDropAgeMs >= 0 && ctDropAgeMs <= 5000);
            if (!relevant && !_settings.LogAllPendingActionCandidates)
                continue;

            candidates.Add(
                (scoreResult.Score,
                 $"0x{ptr:X}/id=0x{unit.CharId:X2}/role={scoreResult.Role}/score={scoreResult.Score}/hp={unit.Hp}/ct={unit.Ct}" +
                 $"/seenAgeMs={seenAgeMs}/ctDropAgeMs={ctDropAgeMs}/ctDrop={known?.CtDropAmount ?? 0}" +
                 $"/stateAgeMs={stateAgeMs}/actionIdAgeMs={actionIdAgeMs}/activeActionAgeMs={activeActionAgeMs}" +
                 $"/currentActive={(scoreResult.CurrentActiveAction ? 1 : 0)}" +
                 $"/freshAct={(scoreResult.FreshActionId ? 1 : 0)}/freshActive={(scoreResult.FreshActiveAction ? 1 : 0)}" +
                 $"/staleAct={(scoreResult.StaleActionId ? 1 : 0)}/staleActive={(scoreResult.StaleActiveAction ? 1 : 0)}" +
                 $"/rawDmg={rawDamage}/exactApplied={(exactAppliedMatch ? 1 : 0)}" +
                 $"/lethalClamp={(lethalClampMatch ? 1 : 0)}/{state.AllFields}"));
        }

        string summary = candidates.Count == 0
            ? "none"
            : string.Join(" ", candidates
                .OrderByDescending(candidate => candidate.Score)
                .Take(maxUnits)
                .Select(candidate => candidate.Text));
        Line(
            $"[IMMEDIATE-ACTION-CANDIDATES kind={kind} event={eventIndex} target=0x{target.Ptr:X}/id=0x{target.CharId:X2} " +
            $"prevHp={previousHp} currentHp={currentHp} appliedHpLoss={appliedHpLoss} " +
            $"rawTargetForecastDamage={rawTargetForecastDamage} hpClamp={(hpClamp ? 1 : 0)} now={nowTick}] {summary}");
    }

    private void LogPendingActionCandidatesIfEnabled(string kind, long eventIndex, UnitSnapshot target, long nowTick)
    {
        if (!_settings.LogPendingActionCandidatesOnEvent) return;

        const int pendingProbeRawSize = 0x200;
        int maxUnits = Math.Clamp(_settings.PendingActionCandidateMaxUnits, 1, 128);
        var parts = new List<string>();
        foreach (nint ptr in _unitRegistry.OrderBy(p => p).Take(maxUnits))
        {
            byte[] raw = new byte[pendingProbeRawSize];
            if (!CurrentProcessMemory.TryRead(ptr, raw, out _)) continue;
            if (!TryCreateUnitSnapshot(ptr, raw, out var unit, out _)) continue;

            int pendingFlag = unit.ReadByte(0x61);
            int pendingTimer = unit.ReadByte(0x18D);
            int actionId = unit.ReadUInt16(0x1A2);
            int forecastDamage = unit.ReadUInt16(0x1C4);
            int forecastCredit = unit.ReadUInt16(0x1C6);
            int forecastCharge = unit.ReadByte(0x1D8);
            int forecastFlag = unit.ReadByte(0x1E5);
            int pendingFlag2 = unit.ReadByte(0x1EF);
            int activeMarker = unit.ReadByte(0x1B8);
            int phaseMarker = unit.ReadByte(0x1BB);

            bool looksRelevant =
                pendingFlag != 0 ||
                pendingTimer != 0xFF ||
                actionId != 0 ||
                forecastDamage != 0 ||
                forecastCredit != 0 ||
                forecastCharge != 0 ||
                forecastFlag != 0 ||
                pendingFlag2 != 0 ||
                activeMarker != 0 ||
                phaseMarker != 0;
            if (!looksRelevant && !_settings.LogAllPendingActionCandidates) continue;

            parts.Add(
                $"0x{ptr:X}/id=0x{unit.CharId:X2}/hp={unit.Hp}/ct={unit.Ct}" +
                $"/s61={pendingFlag}/t18D={pendingTimer}/act={actionId}" +
                $"/dmg1C4={forecastDamage}/cred1C6={forecastCredit}/chg1D8={forecastCharge}/f1E5={forecastFlag}" +
                $"/f1EF={pendingFlag2}/b8={activeMarker}/bb={phaseMarker}");
        }

        string summary = parts.Count == 0 ? "none" : string.Join(" ", parts);
        Line($"[PENDING-ACTION-CANDIDATES kind={kind} event={eventIndex} target=0x{target.Ptr:X}/id=0x{target.CharId:X2} now={nowTick}] {summary}");
    }

    private bool TryReadActionProbeSnapshot(
        nint unitPtr,
        int expectedId,
        out UnitSnapshot unit,
        out ActionProbeState state)
    {
        unit = null!;
        state = null!;

        const int actionProbeRawSize = 0x200;
        byte[] raw = new byte[actionProbeRawSize];
        if (!CurrentProcessMemory.TryRead(unitPtr, raw, out _)) return false;
        if (!TryCreateUnitSnapshot(unitPtr, raw, out unit, out _)) return false;
        if (unit.CharId != expectedId) return false;

        state = ActionProbeState.From(unit);
        return true;
    }

    private bool ObservePendingActionStateIfEnabled(UnitSnapshot unit, ActionProbeState state, long nowTick, bool touchForContext)
    {
        if (!_settings.TrackPendingActions) return false;

        var lines = _pendingActionTracker.ObserveUnit(unit, state, _settings, nowTick, touchForContext);
        foreach (string line in lines)
        {
            Line(line);
            if (IsPendingResolveOpenLine(line))
                LogPendingResolveHookRegisterIfEnabled(unit, nowTick);
            if (IsPreApplyTargetCacheLine(line, state))
                LogTargetCacheHookRegisterIfEnabled(unit, nowTick);
        }
        return lines.Count > 0;
    }

    private (bool Logged, PendingActionMatch? Match) MatchPendingActionIfEnabled(
        string kind,
        long eventIndex,
        UnitSnapshot target,
        ActionProbeState? targetState,
        int observedHpLoss,
        long nowTick)
    {
        if (!_settings.TrackPendingActions) return (false, null);

        var result = _pendingActionTracker.MatchHpEvent(kind, eventIndex, target, targetState, observedHpLoss, _settings, nowTick);
        foreach (string line in result.Lines)
            Line(line);
        return (result.Lines.Count > 0, result.Match);
    }

    private bool ReevaluatePreClampFormulaTargetsForImmediateActionIfEnabled(
        UnitSnapshot source,
        ActionProbeState sourceState,
        long nowTick)
    {
        if (!_settings.PreClampFormulaCandidateAllowImmediateAction)
            return false;
        if (!sourceState.IsActiveSourceMarker)
            return false;

        int maxAgeMs = Math.Clamp(_settings.PreClampImmediateActionMaxAgeMs, 1, 60_000);
        int activeAgeMs = ActiveActionAgeMs(source.Ptr, nowTick);
        if (_settings.PreClampImmediateActionRequireFreshActive &&
            (activeAgeMs < 0 || activeAgeMs > maxAgeMs))
            return false;

        bool logged = false;
        foreach (nint ptr in _unitRegistry.OrderBy(p => p).ToArray())
        {
            if (ptr == source.Ptr) continue;
            _unitObservations.TryGetValue(ptr, out var known);
            int expectedId = known?.Unit.CharId ?? -1;
            if (!TryReadActionProbeSnapshot(ptr, expectedId, out var target, out var targetState))
                continue;
            TrackActionProbeAges(ptr, targetState, nowTick);
            if (targetState.ForecastDamage <= 0 && targetState.ForecastCredit <= 0)
                continue;

            if (LogPreClampFormulaCandidateIfEnabled(target, targetState, nowTick))
                logged = true;
        }

        return logged;
    }

    private bool LogPreClampFormulaCandidateIfEnabled(UnitSnapshot target, ActionProbeState targetState, long nowTick)
    {
        bool canLogCandidate = _settings.LogPreClampFormulaCandidates &&
                               _preClampFormulaCandidateLogs < Math.Clamp(_settings.PreClampFormulaCandidateMaxLogs, 0, 1000);
        bool canQueuePlan = _settings.PreClampFormulaPlanEnabled;
        if ((!canLogCandidate && !canQueuePlan) || !_settings.TrackPendingActions)
            return false;

        if (targetState.ForecastDamage <= 0 && targetState.ForecastCredit <= 0)
        {
            _lastPreClampFormulaCandidateKey.Remove(target.Ptr);
            return false;
        }

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        var pendingResult = _pendingActionTracker.MatchTargetCache(
            "preclamp-cache",
            eventIndex,
            target,
            targetState,
            _settings,
            nowTick);
        PendingActionMatch? pendingCandidate = pendingResult.Match;
        PendingActionMatch? pendingMatch = ShouldUsePendingActionMatch(pendingCandidate) ? pendingCandidate : null;
        if (_settings.PreClampFormulaCandidateRequirePendingMatch && pendingMatch is null)
            return false;

        string immediateCandidateLine = "";
        ImmediatePreClampActionMatch? immediateMatch = pendingMatch is null
            ? ResolveImmediatePreClampActionIfEnabled(target, targetState, nowTick, out immediateCandidateLine)
            : null;
        var fallbackAttacker = immediateMatch is null
            ? _contextResolver.ResolveRecentAttacker(target, _unitObservations, nowTick)
            : new ResolvedAttacker(null, "none", "");
        UnitSnapshot? attackerUnit = pendingMatch?.Caster ?? immediateMatch?.Unit ?? fallbackAttacker.Unit;
        string attackerSource = pendingMatch?.Source ?? (immediateMatch is not null ? "immediate-action" : fallbackAttacker.Source);
        ActionSignal? actionSignal = pendingMatch is not null
            ? BuildPendingActionSignal(pendingMatch)
            : immediateMatch is not null
                ? BuildImmediateActionSignal(immediateMatch, targetState)
                : null;
        PreClampFormulaPlanContext? planContext = pendingMatch is not null
            ? new PreClampFormulaPlanContext(pendingMatch.Source, pendingMatch.ActionId, pendingMatch.BatchId)
            : immediateMatch is not null
                ? new PreClampFormulaPlanContext("immediate-action", immediateMatch.State.ActionId, -1)
                : null;
        int stagedHpDelta = targetState.ForecastCredit > 0 && targetState.ForecastDamage <= 0
            ? targetState.ForecastCredit
            : -targetState.ForecastDamage;
        int syntheticCurrentHp = Math.Clamp(target.Hp + stagedHpDelta, 0, target.MaxHp);
        int stagedVanillaDamage = target.Hp - syntheticCurrentHp;
        long eventSeed = ComputeEventSeed(target, eventIndex, target.Hp, syntheticCurrentHp, stagedVanillaDamage);
        var stagedEvent = new DamageEvent(
            target,
            target.Hp,
            syntheticCurrentHp,
            stagedVanillaDamage,
            attackerUnit,
            attackerSource,
            actionSignal,
            eventIndex,
            eventSeed);
        var formulaResult = _formulaEngine.EvaluateForStagedApply(stagedEvent);

        string attackerKey = attackerUnit is null ? "none" : attackerUnit.Ptr.ToString("X");
        string key = string.Join("|",
            target.Ptr.ToString("X"),
            pendingMatch?.BatchId.ToString() ?? "none",
            planContext?.ActionId.ToString() ?? "none",
            planContext?.Source ?? "none",
            attackerKey,
            targetState.ForecastDamage,
            targetState.ForecastCredit,
            formulaResult.ShouldRewrite ? "1" : "0",
            formulaResult.FinalDamage,
            formulaResult.RuleName);
        if (_lastPreClampFormulaCandidateKey.TryGetValue(target.Ptr, out string? previousKey) &&
            previousKey == key)
            return false;

        _lastPreClampFormulaCandidateKey[target.Ptr] = key;
        int forcedDebit = formulaResult.ShouldRewrite && stagedEvent.IsDamage
            ? Math.Clamp(formulaResult.FinalDamage, 0, 9999)
            : stagedEvent.IsHealing ? 0 : targetState.ForecastDamage;
        int forcedCredit = formulaResult.ShouldRewrite && stagedEvent.IsHealing
            ? Math.Clamp(-formulaResult.FinalDamage, 0, 9999)
            : stagedEvent.IsDamage ? 0 : targetState.ForecastCredit;
        bool shouldQueuePlan = canQueuePlan &&
                               formulaResult.ShouldRewrite &&
                               (stagedEvent.IsDamage || stagedEvent.IsHealing) &&
                               (!_settings.PreClampFormulaPlanRequirePhaseZero || targetState.PhaseMarker == 0);
        bool queuedPlan = shouldQueuePlan &&
                          TryQueuePreClampFormulaPlan(
                              key,
                              target,
                              planContext,
                              targetState.ForecastDamage,
                              targetState.ForecastCredit,
                              forcedDebit,
                              forcedCredit,
                              nowTick);

        if (!canLogCandidate)
            return queuedPlan;

        _preClampFormulaCandidateLogs++;
        foreach (string line in pendingResult.Lines)
            Line(line);
        if (!string.IsNullOrWhiteSpace(immediateCandidateLine))
            Line(immediateCandidateLine);

        string attacker = attackerUnit is null
            ? "none"
            : $"0x{attackerUnit.Ptr:X}/id=0x{attackerUnit.CharId:X2}";
        string pending = pendingMatch is null
            ? "pending=none"
            : $"pending=batch={pendingMatch.BatchId}/act={pendingMatch.ActionId}/event={pendingMatch.BatchEvent}/{pendingMatch.MaxBatchEvents}/confidence={pendingMatch.Confidence}/score={pendingMatch.Score}";
        string immediate = immediateMatch is null
            ? "immediate=none"
            : $"immediate=0x{immediateMatch.Unit.Ptr:X}/id=0x{immediateMatch.Unit.CharId:X2}/act={immediateMatch.State.ActionId}/score={immediateMatch.Score.Score}/runnerUp={immediateMatch.RunnerUpScore}/margin={immediateMatch.Margin}/currentActive={(immediateMatch.Score.CurrentActiveAction ? 1 : 0)}/actionAge={immediateMatch.ActionIdAgeMs}ms/activeAge={immediateMatch.ActiveActionAgeMs}ms";
        Line(
            $"[PRECLAMP-FORMULA-CANDIDATE event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2} " +
            $"hp={target.Hp}/{target.MaxHp} oldDebit={targetState.ForecastDamage} oldCredit={targetState.ForecastCredit} " +
            $"forcedDebit={forcedDebit} forcedCredit={forcedCredit} eventKind={(stagedEvent.IsHealing ? "healing" : stagedEvent.IsDamage ? "damage" : "other")} " +
            $"shouldStage={(formulaResult.ShouldRewrite ? 1 : 0)} queuedPlan={(queuedPlan ? 1 : 0)} rule={formulaResult.RuleName} " +
            $"attacker={attacker} source={attackerSource} {pending} {immediate} now={nowTick} action={targetState.AllFields}]");
        if (_settings.LogResolvedRuntimeContext && !string.IsNullOrWhiteSpace(formulaResult.Trace))
            Line($"[PRECLAMP-FORMULA-RUNTIME ptr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {formulaResult.Trace}");
        return true;
    }

    private static ActionSignal BuildPendingActionSignal(PendingActionMatch match)
    {
        int targetCacheDamage = match.CurrentTargetCacheDamage > 0
            ? match.CurrentTargetCacheDamage
            : match.RecentTargetCacheDamage;
        int targetCacheCredit = match.CurrentTargetCacheCredit > 0
            ? match.CurrentTargetCacheCredit
            : match.RecentTargetCacheCredit;
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["signal"] = match.ActionId,
            ["id"] = match.ActionId,
            ["actionId"] = match.ActionId,
            ["batch"] = ClampFormulaInt(match.BatchId),
            ["batchEvent"] = match.BatchEvent,
            ["batchMaxEvents"] = match.MaxBatchEvents,
            ["batchAgeMs"] = match.BatchAgeMs,
            ["score"] = match.Score,
            ["observedHpLoss"] = match.ObservedHpLoss,
            ["targetCacheDamage"] = targetCacheDamage,
            ["targetCacheCredit"] = targetCacheCredit,
            ["targetCacheHealing"] = targetCacheCredit,
            ["targetCacheAmount"] = targetCacheCredit > 0 && targetCacheDamage <= 0 ? targetCacheCredit : targetCacheDamage,
            ["currentTargetCacheDamage"] = match.CurrentTargetCacheDamage,
            ["currentTargetCacheCredit"] = match.CurrentTargetCacheCredit,
            ["recentTargetCacheDamage"] = match.RecentTargetCacheDamage,
            ["recentTargetCacheCredit"] = match.RecentTargetCacheCredit,
            ["damageCacheMatch"] = match.CurrentDamageCacheMatches || match.RecentDamageCacheMatches ? 1 : 0,
            ["currentDamageCacheMatch"] = match.CurrentDamageCacheMatches ? 1 : 0,
            ["recentDamageCacheMatch"] = match.RecentDamageCacheMatches ? 1 : 0,
            ["creditCacheMatch"] = match.CurrentCreditCacheMatches || match.RecentCreditCacheMatches ? 1 : 0,
            ["currentCreditCacheMatch"] = match.CurrentCreditCacheMatches ? 1 : 0,
            ["recentCreditCacheMatch"] = match.RecentCreditCacheMatches ? 1 : 0,
            ["exactDamageCacheMatch"] = match.CurrentDamageCacheExactMatches || match.RecentDamageCacheExactMatches ? 1 : 0,
            ["currentExactDamageCacheMatch"] = match.CurrentDamageCacheExactMatches ? 1 : 0,
            ["recentExactDamageCacheMatch"] = match.RecentDamageCacheExactMatches ? 1 : 0,
            ["exactCreditCacheMatch"] = match.CurrentCreditCacheExactMatches || match.RecentCreditCacheExactMatches ? 1 : 0,
            ["currentExactCreditCacheMatch"] = match.CurrentCreditCacheExactMatches ? 1 : 0,
            ["recentExactCreditCacheMatch"] = match.RecentCreditCacheExactMatches ? 1 : 0,
            ["lethalClampDamageCacheMatch"] = match.CurrentDamageCacheLethalClampMatches || match.RecentDamageCacheLethalClampMatches ? 1 : 0,
            ["currentLethalClampDamageCacheMatch"] = match.CurrentDamageCacheLethalClampMatches ? 1 : 0,
            ["recentLethalClampDamageCacheMatch"] = match.RecentDamageCacheLethalClampMatches ? 1 : 0,
            ["hasCurrentTargetMetadata"] = match.HasCurrentTargetMetadata ? 1 : 0,
            ["confidenceDamageCache"] = match.Confidence.StartsWith("damage-cache", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            ["confidenceRecentDamageCache"] = match.Confidence.StartsWith("recent-damage-cache", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            ["confidenceLethalClampDamageCache"] = match.Confidence.Contains("lethal-clamp", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            ["confidenceRecentResolve"] = match.Confidence.Equals("recent-resolve", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
        };
        return new ActionSignal($"pending-action-{match.ActionId}", match.Source, variables);
    }

    private ImmediatePreClampActionMatch? ResolveImmediatePreClampActionIfEnabled(
        UnitSnapshot target,
        ActionProbeState targetState,
        long nowTick,
        out string candidateLine)
    {
        candidateLine = "";
        if (!_settings.PreClampFormulaCandidateAllowImmediateAction)
            return null;

        int maxUnits = Math.Clamp(_settings.ImmediateActionCandidateMaxUnits, 1, 128);
        int minScore = _settings.PreClampImmediateActionMinScore;
        int minMargin = _settings.PreClampImmediateActionMinMargin;
        int maxAgeMs = Math.Clamp(_settings.PreClampImmediateActionMaxAgeMs, 1, 60_000);
        var candidates = new List<ImmediatePreClampActionMatch>();
        var summary = new List<(int Score, string Text)>();

        foreach (nint ptr in _unitRegistry.OrderBy(p => p))
        {
            _unitObservations.TryGetValue(ptr, out var known);
            int expectedId = known?.Unit.CharId ?? -1;
            if (!TryReadActionProbeSnapshot(ptr, expectedId, out var unit, out var state))
                continue;
            TrackActionProbeAges(ptr, state, nowTick);

            bool isTarget = ptr == target.Ptr;
            int stateAgeMs = _lastActionProbeChangeTick.TryGetValue(ptr, out long actionChangeTick)
                ? ElapsedMs(nowTick, actionChangeTick)
                : -1;
            int actionIdAgeMs = ActionIdAgeMs(ptr, nowTick);
            int activeActionAgeMs = ActiveActionAgeMs(ptr, nowTick);
            int seenAgeMs = known is not null && known.SeenTick > 0 ? ElapsedMs(nowTick, known.SeenTick) : -1;
            int ctDropAgeMs = known is not null && known.CtDropTick > 0 ? ElapsedMs(nowTick, known.CtDropTick) : -1;
            var score = ImmediateActionCandidateScoring.Evaluate(
                new ImmediateActionCandidateScoreInput(
                    isTarget,
                    unit.Hp,
                    state.ActionId,
                    state.HasPrimaryPendingFlag,
                    state.HasSecondaryPendingFlag,
                    state.PendingTimer,
                    state.ActiveMarker2,
                    stateAgeMs,
                    seenAgeMs,
                    ctDropAgeMs,
                    Math.Max(state.ForecastDamage, state.ForecastCredit),
                    actionIdAgeMs,
                    activeActionAgeMs)
                {
                    AllowZeroActionIdActiveSource = _settings.PreClampImmediateActionAllowZeroActionId,
                });
            bool currentActiveAllowed = !_settings.PreClampImmediateActionRequireFreshActive && score.CurrentActiveAction;
            bool freshEnough = _settings.PreClampImmediateActionRequireFreshActive
                ? score.FreshActiveAction && activeActionAgeMs >= 0 && activeActionAgeMs <= maxAgeMs
                : currentActiveAllowed ||
                  (score.FreshActiveAction && activeActionAgeMs >= 0 && activeActionAgeMs <= maxAgeMs) ||
                  (score.FreshActionId && actionIdAgeMs >= 0 && actionIdAgeMs <= maxAgeMs);
            bool hasActionIdentity = state.ActionId > 0 ||
                                     (_settings.PreClampImmediateActionAllowZeroActionId && state.IsActiveSourceMarker);
            bool eligible = score.SourceLike &&
                            freshEnough &&
                            score.Score >= minScore &&
                            hasActionIdentity;
            string eligibleText = eligible ? "eligible=1" : "eligible=0";
            summary.Add(
                 (score.Score,
                 $"0x{ptr:X}/id=0x{unit.CharId:X2}/{score.Role}/score={score.Score}/{eligibleText}" +
                 $"/act={state.ActionId}/currentActive={(score.CurrentActiveAction ? 1 : 0)}" +
                 $"/freshAct={(score.FreshActionId ? 1 : 0)}/freshActive={(score.FreshActiveAction ? 1 : 0)}" +
                 $"/stateAge={stateAgeMs}/actionAge={actionIdAgeMs}/activeAge={activeActionAgeMs}/seenAge={seenAgeMs}/ctDropAge={ctDropAgeMs}" +
                 $"/hp={unit.Hp}/ct={unit.Ct}/{state.AllFields}"));

            if (!eligible)
                continue;

            candidates.Add(new ImmediatePreClampActionMatch(
                unit,
                state,
                score,
                stateAgeMs,
                seenAgeMs,
                ctDropAgeMs,
                actionIdAgeMs,
                activeActionAgeMs,
                RunnerUpScore: int.MinValue,
                Margin: int.MaxValue));
        }

        var ordered = candidates
            .OrderByDescending(candidate => candidate.Score.Score)
            .ThenBy(candidate => candidate.ActiveActionAgeMs < 0 ? int.MaxValue : candidate.ActiveActionAgeMs)
            .ThenBy(candidate => candidate.ActionIdAgeMs < 0 ? int.MaxValue : candidate.ActionIdAgeMs)
            .ThenBy(candidate => candidate.Unit.Ptr.ToInt64())
            .ToList();
        ImmediatePreClampActionMatch? best = ordered.FirstOrDefault();
        int runnerUpScore = ordered.Count > 1 ? ordered[1].Score.Score : int.MinValue;
        int margin = best is null || runnerUpScore == int.MinValue ? int.MaxValue : best.Score.Score - runnerUpScore;
        if (best is not null)
            best = best with { RunnerUpScore = runnerUpScore, Margin = margin };
        bool marginOk = best is not null && margin >= minMargin;
        if (!marginOk)
            best = null;

        string selected = best is null
            ? "selected=none"
            : $"selected=0x{best.Unit.Ptr:X}/id=0x{best.Unit.CharId:X2}/act={best.State.ActionId}/score={best.Score.Score}/runnerUp={runnerUpScore}/margin={margin}";
        string body = summary.Count == 0
            ? "none"
            : string.Join(" ", summary
                .OrderByDescending(candidate => candidate.Score)
                .Take(maxUnits)
                .Select(candidate => candidate.Text));
        candidateLine =
            $"[PRECLAMP-IMMEDIATE-CANDIDATES target=0x{target.Ptr:X}/id=0x{target.CharId:X2} " +
            $"oldDebit={targetState.ForecastDamage} oldCredit={targetState.ForecastCredit} minScore={minScore} minMargin={minMargin} maxAgeMs={maxAgeMs} " +
            $"requireFreshActive={(_settings.PreClampImmediateActionRequireFreshActive ? 1 : 0)} {selected}] {body}";
        return best;
    }

    private static ActionSignal BuildImmediateActionSignal(ImmediatePreClampActionMatch match, ActionProbeState targetState)
    {
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["signal"] = match.State.ActionId,
            ["id"] = match.State.ActionId,
            ["actionId"] = match.State.ActionId,
            ["score"] = match.Score.Score,
            ["runnerUpScore"] = match.RunnerUpScore == int.MinValue ? 0 : match.RunnerUpScore,
            ["margin"] = match.Margin == int.MaxValue ? 9999 : Math.Clamp(match.Margin, -9999, 9999),
            ["stateAgeMs"] = match.StateAgeMs,
            ["seenAgeMs"] = match.SeenAgeMs,
            ["ctDropAgeMs"] = match.CtDropAgeMs,
            ["actionIdAgeMs"] = match.ActionIdAgeMs,
            ["activeActionAgeMs"] = match.ActiveActionAgeMs,
            ["currentActiveAction"] = match.Score.CurrentActiveAction ? 1 : 0,
            ["freshActionId"] = match.Score.FreshActionId ? 1 : 0,
            ["freshActiveAction"] = match.Score.FreshActiveAction ? 1 : 0,
            ["staleActionId"] = match.Score.StaleActionId ? 1 : 0,
            ["staleActiveAction"] = match.Score.StaleActiveAction ? 1 : 0,
            ["activeMarker2"] = match.State.ActiveMarker2,
            ["pendingFlag"] = match.State.PendingFlag,
            ["pendingFlag2"] = match.State.PendingFlag2,
            ["pendingTimer"] = match.State.PendingTimer,
            ["targetCacheDamage"] = targetState.ForecastDamage,
            ["targetCacheCredit"] = targetState.ForecastCredit,
            ["targetCacheHealing"] = targetState.ForecastCredit,
            ["targetCacheAmount"] = targetState.ForecastCredit > 0 && targetState.ForecastDamage <= 0
                ? targetState.ForecastCredit
                : targetState.ForecastDamage,
            ["currentTargetCacheDamage"] = targetState.ForecastDamage,
            ["currentTargetCacheCredit"] = targetState.ForecastCredit,
            ["recentTargetCacheDamage"] = 0,
            ["recentTargetCacheCredit"] = 0,
            ["damageCacheMatch"] = targetState.ForecastDamage > 0 ? 1 : 0,
            ["currentDamageCacheMatch"] = targetState.ForecastDamage > 0 ? 1 : 0,
            ["creditCacheMatch"] = targetState.ForecastCredit > 0 ? 1 : 0,
            ["currentCreditCacheMatch"] = targetState.ForecastCredit > 0 ? 1 : 0,
            ["hasCurrentTargetMetadata"] = targetState.ForecastCharge != 0 || targetState.ForecastFlag != 0 || targetState.PhaseMarker != 0 ? 1 : 0,
        };
        return new ActionSignal($"immediate-action-{match.State.ActionId}", "immediate-action", variables);
    }

    private static bool ShouldUsePendingActionMatch(PendingActionMatch? match)
        => match is not null && match.HasHpCacheMatch;

    private static int ClampFormulaInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    private bool RefreshPendingActionTrackerForEvent(long nowTick)
    {
        if (!_settings.TrackPendingActions) return false;

        bool logged = false;
        foreach (nint ptr in _unitRegistry.OrderBy(p => p).ToArray())
        {
            if (!_unitObservations.TryGetValue(ptr, out var observation)) continue;
            if (!TryReadActionProbeSnapshot(ptr, observation.Unit.CharId, out var unit, out var state)) continue;
            TrackActionProbeAges(ptr, state, nowTick);

            var lines = _pendingActionTracker.ObserveUnit(unit, state, _settings, nowTick, touchForContext: false);
            foreach (string line in lines)
            {
                Line(line);
                if (IsPendingResolveOpenLine(line))
                    LogPendingResolveHookRegisterIfEnabled(unit, nowTick);
                if (IsPreApplyTargetCacheLine(line, state))
                    LogTargetCacheHookRegisterIfEnabled(unit, nowTick);
            }
            logged |= lines.Count > 0;
        }

        return logged;
    }

    private static bool IsPendingResolveOpenLine(string line)
        => line.StartsWith("[PENDING-ACTION-TRACK resolve-open ", StringComparison.Ordinal);

    private static bool IsPreApplyTargetCacheLine(string line, ActionProbeState state)
        => line.StartsWith("[PENDING-ACTION-TARGET ", StringComparison.Ordinal) &&
           state.ForecastDamage > 0 &&
           (state.ForecastFlag & 0x80) != 0 &&
           state.PhaseMarker != 2;

    private void LogPendingResolveHookRegisterIfEnabled(UnitSnapshot caster, long nowTick)
    {
        if (!_settings.HookRegisterProbeOnPendingResolve) return;

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        LogHookRegisterProbeEventIfEnabled("pendingresolve", eventIndex, caster, nowTick);
    }

    private void LogTargetCacheHookRegisterIfEnabled(UnitSnapshot target, long nowTick)
    {
        if (!_settings.HookRegisterProbeOnTargetCache) return;

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        LogHookRegisterProbeEventIfEnabled("targetcache", eventIndex, target, nowTick);
    }

    private bool LogActionStateChangeIfEnabled(UnitSnapshot unit, ActionProbeState state, long nowTick, bool touchForContext)
    {
        if (!_settings.LogActionStateChanges) return false;

        string stateKey = state.Key;
        nint unitPtr = unit.Ptr;

        if (_lastActionProbeState.TryGetValue(unitPtr, out string? previous) && previous == stateKey)
            return false;

        _lastActionProbeState[unitPtr] = stateKey;
        _lastActionProbeChangeTick[unitPtr] = nowTick;
        Line(
            $"[ACTION-STATE ptr=0x{unitPtr:X} id=0x{unit.CharId:X2} now={nowTick} touch={(touchForContext ? 1 : 0)}] " +
            $"hp={unit.Hp} ct={unit.Ct} s61={state.PendingFlag} t18D={state.PendingTimer} act={state.ActionId} " +
            $"dmg1C4={state.ForecastDamage} cred1C6={state.ForecastCredit} chg1D8={state.ForecastCharge} f1E5={state.ForecastFlag} f1EF={state.PendingFlag2} " +
            $"b8={state.ActiveMarker} ba={state.ActiveMarker2} bb={state.PhaseMarker}");
        return true;
    }

    private bool LogActionBoundaryProbeIfEnabled(UnitSnapshot target, byte[]? previousRaw, long nowTick, bool touchForContext)
    {
        if (!_settings.LogActionBoundaryProbe) return false;
        if (previousRaw is null) return false;

        int maxLogs = Math.Clamp(_settings.ActionBoundaryProbeMaxLogs, 0, 10_000);
        if (_actionBoundaryProbeLogs >= maxLogs) return false;

        int diffMax = Math.Clamp(_settings.ActionBoundaryProbeDiffMax, 0, 128);
        var diffs = ActionBoundaryDiffs(previousRaw, target.Raw, diffMax);
        if (diffs.Count == 0) return false;

        long eventIndex = Interlocked.Increment(ref _probeEventIndex);
        int hookAgeMs = _lastHookObservationTick > 0 ? ElapsedMs(nowTick, _lastHookObservationTick) : -1;
        _actionBoundaryProbeLogs++;
        Line(
            $"[ACTION-BOUNDARY event={eventIndex} ptr=0x{target.Ptr:X} id=0x{target.CharId:X2} now={nowTick} " +
            $"touch={(touchForContext ? 1 : 0)} hookAgeMs={hookAgeMs} reason={ActionBoundaryReasons(previousRaw, target.Raw)} " +
            $"prev={ActionBoundaryFields(previousRaw)} curr={ActionBoundaryFields(target.Raw)}] diff={string.Join(" ", diffs)}");
        LogHookRegisterProbeEventIfEnabled("actionboundary", eventIndex, target, nowTick);
        return true;
    }

    private static List<string> ActionBoundaryDiffs(byte[] previousRaw, byte[] currentRaw, int max)
    {
        var diffs = new List<string>();
        int limit = Math.Min(previousRaw.Length, currentRaw.Length);
        if (limit == 0 || max <= 0) return diffs;

        foreach (int offset in ActionBoundaryOffsets)
        {
            if (offset < 0 || offset >= limit) continue;
            if (previousRaw[offset] == currentRaw[offset]) continue;
            diffs.Add($"+0x{offset:X2}:{previousRaw[offset]:X2}->{currentRaw[offset]:X2}");
            if (diffs.Count >= max) break;
        }
        return diffs;
    }

    private static string ActionBoundaryReasons(byte[] previousRaw, byte[] currentRaw)
    {
        var reasons = new List<string>();
        int prevHp = ReadUInt16Raw(previousRaw, 0x30);
        int currHp = ReadUInt16Raw(currentRaw, 0x30);
        int prevDamage = ReadUInt16Raw(previousRaw, 0x1C4);
        int currDamage = ReadUInt16Raw(currentRaw, 0x1C4);
        int prevCredit = ReadUInt16Raw(previousRaw, 0x1C6);
        int currCredit = ReadUInt16Raw(currentRaw, 0x1C6);
        int prevAction = ReadUInt16Raw(previousRaw, 0x1A2);
        int currAction = ReadUInt16Raw(currentRaw, 0x1A2);

        if (prevHp != currHp) reasons.Add(currHp == 0 && prevHp > 0 ? "hp-zero" : "hp-change");
        if (prevDamage != currDamage) reasons.Add("forecast-damage-change");
        if (prevCredit != currCredit) reasons.Add("forecast-credit-change");
        if (prevAction != currAction) reasons.Add("action-id-change");
        if (ReadByteRaw(previousRaw, 0x1D8) != ReadByteRaw(currentRaw, 0x1D8)) reasons.Add("forecast-charge-change");
        if (ReadByteRaw(previousRaw, 0x1E5) != ReadByteRaw(currentRaw, 0x1E5)) reasons.Add("forecast-flag-change");
        if (ReadByteRaw(previousRaw, 0x1BB) != ReadByteRaw(currentRaw, 0x1BB)) reasons.Add("phase-change");
        if (ReadByteRaw(previousRaw, 0x61) != ReadByteRaw(currentRaw, 0x61) ||
            ReadByteRaw(previousRaw, 0x1EF) != ReadByteRaw(currentRaw, 0x1EF))
            reasons.Add("status-pending-change");
        if (DeathStateChanged(previousRaw, currentRaw)) reasons.Add("death-state-change");

        return reasons.Count == 0 ? "interesting-offset-change" : string.Join(",", reasons);
    }

    private static bool DeathStateChanged(byte[] previousRaw, byte[] currentRaw)
    {
        int[] offsets = [0x61, 0x63, 0x18C, 0x1BB, 0x1DB, 0x1EF, 0x1F1, 0x1F5];
        foreach (int offset in offsets)
        {
            if (ReadByteRaw(previousRaw, offset) != ReadByteRaw(currentRaw, offset))
                return true;
        }
        return false;
    }

    private static string ActionBoundaryFields(byte[] raw)
        => $"hp={ReadUInt16Raw(raw, 0x30)}/ct={ReadByteRaw(raw, 0x41)}" +
           $"/s61={ReadByteRaw(raw, 0x61)}/t18D={ReadByteRaw(raw, 0x18D)}" +
           $"/act={ReadUInt16Raw(raw, 0x1A2)}/dmg1C4={ReadUInt16Raw(raw, 0x1C4)}" +
           $"/cred1C6={ReadUInt16Raw(raw, 0x1C6)}" +
           $"/chg1D8={ReadByteRaw(raw, 0x1D8)}/f1E5={ReadByteRaw(raw, 0x1E5)}" +
           $"/f1EF={ReadByteRaw(raw, 0x1EF)}/b8={ReadByteRaw(raw, 0x1B8)}" +
           $"/ba={ReadByteRaw(raw, 0x1BA)}/bb={ReadByteRaw(raw, 0x1BB)}";

    private static int ReadByteRaw(byte[] raw, int offset)
        => offset >= 0 && offset < raw.Length ? raw[offset] : 0;

    private static int ReadUInt16Raw(byte[] raw, int offset)
        => ReadByteRaw(raw, offset) | (ReadByteRaw(raw, offset + 1) << 8);

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

        _hookRegisterProbeLogs++;
        Line($"[HOOK-REGS count={hookCount} ptr=0x{touched.Ptr:X} id=0x{touched.CharId:X2}] {FormatHookRegisterSnapshot(touched)}");
        Flush();
    }

    private void LogHookRegisterProbeEventIfEnabled(string kind, long eventIndex, UnitSnapshot target, long nowTick)
    {
        if (!ShouldLogHookRegisterProbeEvent(kind))
            return;

        int maxLogs = Math.Clamp(_settings.HookRegisterProbeEventMaxLogs, 0, 1000);
        if (_hookRegisterProbeEventLogs >= maxLogs) return;

        int hookCount = Marshal.ReadInt32(_buf, B_COUNT);
        nint hookPtr = Marshal.ReadIntPtr(_buf, B_PTR);
        int hookAgeMs = _lastHookObservationTick > 0 ? ElapsedMs(nowTick, _lastHookObservationTick) : -1;
        _hookRegisterProbeEventLogs++;
        Line(
            $"[HOOK-REGS-EVENT kind={kind} event={eventIndex} hookCount={hookCount} " +
            $"hookAgeMs={hookAgeMs} hookPtr=0x{hookPtr:X} targetPtr=0x{target.Ptr:X} id=0x{target.CharId:X2}] " +
            FormatHookRegisterSnapshot(target));
        LogHookPointerScanEventIfEnabled(kind, eventIndex, target);
        Flush();
    }

    private bool ShouldLogHookRegisterProbeEvent(string kind)
        => kind switch
        {
            "damage" or "healing" => _settings.HookRegisterProbeOnHpEvent,
            "mploss" or "mpgain" => _settings.HookRegisterProbeOnMpEvent,
            "ctdrop" => _settings.HookRegisterProbeOnCtDrop,
            "actionboundary" => _settings.HookRegisterProbeOnActionBoundary,
            "pendingresolve" => _settings.HookRegisterProbeOnPendingResolve,
            "targetcache" => _settings.HookRegisterProbeOnTargetCache,
            _ => false,
        };

    private string ClassifyRegisterValue(nint value, UnitSnapshot touched)
        => ClassifyRegisterValue(value, touched, "touched");

    private string ClassifyRegisterValue(nint value, UnitSnapshot? reference, string referenceName)
    {
        if (value == 0) return "zero";
        if (reference is not null && value == reference.Ptr) return $"unit:{referenceName}";
        if (_unitObservations.TryGetValue(value, out var observation))
            return $"unit:id=0x{observation.Unit.CharId:X2}:team={observation.Unit.Team}:hp={observation.Unit.Hp}:ct={observation.Unit.Ct}";
        if (TryReadLiveUnitSnapshot(value, out var possibleUnit, out _))
            return $"unit?:id=0x{possibleUnit.CharId:X2}:team={possibleUnit.Team}:hp={possibleUnit.Hp}:ct={possibleUnit.Ct}";
        if (TryClassifyActorStruct(value, out string actorText))
            return actorText;
        string? moduleAddress = ClassifyModuleAddress(value);
        if (moduleAddress is not null)
            return moduleAddress;
        if (ReadableMemoryRange.IsReadable(value, IntPtr.Size))
            return "readable";
        return "unreadable";
    }

    private bool TryClassifyActorStruct(nint value, out string text)
    {
        text = "";
        if (value == 0 || _unitObservations.ContainsKey(value))
            return false;

        int unitOff = _settings.PreClampActorStructUnitOffset;
        int actionOff = _settings.PreClampActorActionIdOffset;
        if (!TryReadLivePtr(value + unitOff, out var linkedUnit) || linkedUnit == 0)
            return false;

        int unitId;
        if (_unitObservations.TryGetValue(linkedUnit, out var observed))
        {
            unitId = observed.Unit.CharId;
        }
        else if (TryReadLiveUnitSnapshot(linkedUnit, out var liveUnit, out _))
        {
            unitId = liveUnit.CharId;
        }
        else
        {
            return false;
        }

        int actionId = TryReadLiveU16(value + actionOff, out int aid) ? aid : -1;
        text = $"actor:id=0x{unitId:X2}:unit=0x{linkedUnit:X}:act={actionId}";
        return true;
    }

    private string? ClassifyModuleAddress(nint value)
    {
        if (_moduleBase == 0 || value == 0)
            return null;

        long rva = value.ToInt64() - _moduleBase.ToInt64();
        if (rva < 0 || (_moduleSize > 0 && rva >= _moduleSize))
            return null;

        var nearest = CodeLandmarks
            .Select(landmark => new { landmark.Name, Distance = rva - landmark.Rva })
            .OrderBy(candidate => Math.Abs(candidate.Distance))
            .FirstOrDefault();
        if (nearest is null)
            return $"module+0x{rva:X}";

        if (nearest.Distance == 0)
            return $"module+0x{rva:X}:{nearest.Name}";

        if (Math.Abs(nearest.Distance) <= 0x20)
            return $"module+0x{rva:X}:near-{nearest.Name}{FormatSignedHex(nearest.Distance)}";

        return $"module+0x{rva:X}";
    }

    private static string FormatSignedHex(long value)
        => value >= 0 ? $"+0x{value:X}" : $"-0x{-value:X}";

    private string FormatHookRegisterSnapshot(UnitSnapshot reference)
    {
        nint[] registers = ReadHookRegisters();
        var parts = new List<string>(registers.Length + 1);
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
            parts.Add($"{RegisterNames[i]}=0x{registers[i]:X}:{ClassifyRegisterValue(registers[i], reference)}");

        string stack = FormatHookStackSlots(registers.Length > 7 ? registers[7] : 0, reference);
        if (stack.Length > 0)
            parts.Add($"stack={stack}");

        return string.Join(" ", parts);
    }

    private string FormatHookStackSlots(nint rsp, UnitSnapshot reference)
    {
        int slots = Math.Clamp(_settings.HookRegisterProbeStackSlots, 0, 64);
        if (slots == 0 || rsp == 0) return "";

        var parts = new List<string>(slots);
        for (int i = 0; i < slots; i++)
        {
            nint slotAddress = rsp + (i * IntPtr.Size);
            if (!ReadableMemoryRange.IsReadable(slotAddress, IntPtr.Size))
                continue;
            nint value;
            try
            {
                value = Marshal.ReadIntPtr(slotAddress);
            }
            catch
            {
                continue;
            }

            if (value == 0) continue;
            parts.Add($"+0x{i * IntPtr.Size:X}=0x{value:X}:{ClassifyRegisterValue(value, reference)}");
        }

        return string.Join(",", parts);
    }

    private void LogHookPointerScanEventIfEnabled(string kind, long eventIndex, UnitSnapshot target)
    {
        int scanBytes = Math.Clamp(_settings.HookRegisterProbePointerScanBytes, 0, 0x2000);
        if (scanBytes <= 0) return;

        int maxLogs = Math.Clamp(_settings.HookRegisterProbePointerMaxLogs, 0, 1000);
        if (_hookRegisterPointerScanLogs >= maxLogs) return;

        nint[] registers = ReadHookRegisters();
        var roots = new List<string>();
        for (int i = 0; i < registers.Length && i < RegisterNames.Length; i++)
        {
            nint root = registers[i];
            if (root == 0) continue;
            if (_unitObservations.ContainsKey(root)) continue;
            if (!ReadableMemoryRange.IsReadable(root, Math.Min(scanBytes, IntPtr.Size))) continue;

            string summary = ScanPointerRoot(
                RegisterNames[i],
                root,
                target,
                scanBytes,
                Math.Clamp(_settings.HookRegisterProbePointerMaxPointersPerRoot, 0, 64));
            if (summary.Length > 0)
                roots.Add(summary);
        }

        _hookRegisterPointerScanLogs++;
        string body = roots.Count == 0 ? "no-readable-nonunit-roots" : string.Join(" ", roots);
        Line($"[HOOK-PTRSCAN-EVENT kind={kind} event={eventIndex} targetPtr=0x{target.Ptr:X} id=0x{target.CharId:X2}] {body}");
    }

    private string ScanPointerRoot(string name, nint root, UnitSnapshot? target, int scanBytes, int maxPointers)
    {
        var raw = new byte[scanBytes];
        if (!CurrentProcessMemory.TryRead(root, raw, out _))
            return "";

        var hits = new List<string>();
        var readable = new List<string>();
        for (int off = 0; off <= raw.Length - IntPtr.Size; off += IntPtr.Size)
        {
            nint value = (nint)BitConverter.ToInt64(raw, off);
            if (value == 0) continue;
            if (_unitObservations.TryGetValue(value, out var observation))
            {
                string label = target is not null && value == target.Ptr
                    ? "target"
                    : $"unit:id=0x{observation.Unit.CharId:X2}:team={observation.Unit.Team}";
                hits.Add($"+0x{off:X}->0x{value:X}:{label}");
                MaybeDumpActorStruct(root, off, value, observation, raw);
                continue;
            }

            if (readable.Count < maxPointers && ReadableMemoryRange.IsReadable(value, IntPtr.Size))
                readable.Add($"+0x{off:X}->0x{value:X}:readable");
        }

        if (hits.Count == 0 && readable.Count == 0)
            return $"{name}@0x{root:X}:nohits";

        string hitText = hits.Count == 0 ? "hits=none" : "hits=" + string.Join(",", hits.Take(16));
        string readableText = readable.Count == 0 ? "ptrs=none" : "ptrs=" + string.Join(",", readable);
        return $"{name}@0x{root:X}:{hitText};{readableText}";
    }

    // When a scanned root links to a registered unit at the configured actor->unit offset (observed +0x148,
    // 0x548-stride participant array), the root is a battle actor/participant struct. Dump a raw byte window
    // so offline analysis can locate the resolving action id and target/charge fields inside it. The bytes
    // come from the buffer ScanPointerRoot already read, so no extra process read is needed.
    private void MaybeDumpActorStruct(nint root, int unitOffset, nint unitValue, UnitObservation observation, byte[] raw)
    {
        if (!_settings.PreClampActorStructDumpEnabled) return;
        if (unitOffset != _settings.PreClampActorStructUnitOffset) return;
        if (_preClampActorDumpLogs >= Math.Clamp(_settings.PreClampActorStructDumpMaxLogs, 0, 1000)) return;

        int dumpBytes = Math.Clamp(_settings.PreClampActorStructDumpBytes, 0, raw.Length);
        if (dumpBytes <= 0) return;

        _preClampActorDumpLogs++;

        var bytes = new List<string>(dumpBytes + dumpBytes / 16);
        for (int i = 0; i < dumpBytes; i++)
        {
            if (i > 0 && (i % 16) == 0) bytes.Add("|");
            bytes.Add(raw[i].ToString("X2"));
        }

        Line($"[PRECLAMP-ACTOR-DUMP root=0x{root:X} unitOff=+0x{unitOffset:X} unit=0x{unitValue:X}/id=0x{observation.Unit.CharId:X2} bytes={dumpBytes}] {string.Join(" ", bytes)}");
    }

    // Observe-only memory-only action-context resolver (register book 2.4). From the pre-clamp frame:
    //   target   = pre-clamp unit pointer
    //   caster   = stack/register actor (root whose +UnitOffset is a registered unit) whose unit != target
    //              or, for self-hit/self-AoE, the target actor itself when it carries a live action id
    //   actionId = caster actor + ActionIdOffset
    // Emits [PRECLAMP-ACTOR-CTX] for head-to-head comparison with the pending tracker / CT. No behavior
    // change. Correlate with [PENDING-ACTION-MATCH]/[CTX] for the same target+now in analysis.
    private void ResolveAndLogActorContextIfEnabled(int sequence, int slot, long nowTick)
    {
        if (!_settings.PreClampResolveActorContext) return;
        if (_preClampActorCtxLogs >= Math.Clamp(_settings.PreClampActorContextMaxLogs, 0, 1000)) return;

        nint targetPtr = Marshal.ReadIntPtr(_preClampDamageRewriteBuf, slot + P_UNIT);
        int targetId = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_ID);
        int oldDebit = Marshal.ReadInt32(_preClampDamageRewriteBuf, slot + P_OLD_DEBIT);
        int unitOff = _settings.PreClampActorStructUnitOffset;
        int actOff = _settings.PreClampActorActionIdOffset;

        // Candidate roots: captured pre-clamp registers + stack slots.
        var roots = new List<nint>(ReadPreClampRegisters(slot));
        byte[] stackDump = ReadPreClampBytes(slot, P_STACK_DUMP, PRECLAMP_STACK_DUMP_SIZE);
        for (int off = 0; off + IntPtr.Size <= stackDump.Length; off += IntPtr.Size)
            roots.Add((nint)BitConverter.ToInt64(stackDump, off));

        // An actor struct is a root whose +unitOff dereferences to a registered unit.
        var actors = new List<(nint actorBase, nint unit, int unitId, int actionId)>();
        var seen = new HashSet<nint>();
        foreach (var root in roots)
        {
            if (root == 0 || !seen.Add(root)) continue;
            if (_unitObservations.ContainsKey(root)) continue; // a unit struct, not an actor wrapper
            if (!TryReadLivePtr(root + unitOff, out var linked)) continue;
            if (!_unitObservations.TryGetValue(linked, out var obs)) continue;
            int actionId = TryReadLiveU16(root + actOff, out var aid) ? aid : -1;
            actors.Add((root, linked, obs.Unit.CharId, actionId));
        }

        var casterActors = actors.Where(a => a.unit != targetPtr).ToList();
        var distinctCasters = casterActors.Select(a => a.unit).Distinct().ToList();
        var selfActors = actors.Where(a => a.unit == targetPtr && a.actionId > 0).ToList();
        var distinctSelfCasters = selfActors.Select(a => a.unit).Distinct().ToList();

        string casterText, actionText, verdict;
        nint casterUnit = 0;
        int casterUnitId = -1;
        if (distinctCasters.Count == 1)
        {
            var c = casterActors[0];
            casterText = $"caster=0x{c.unit:X}/id=0x{c.unitId:X2} casterActor=0x{c.actorBase:X}";
            actionText = $"actionId={c.actionId}";
            verdict = "resolved";
            casterUnit = c.unit;
            casterUnitId = c.unitId;
        }
        else if (distinctCasters.Count == 0 && distinctSelfCasters.Count == 1)
        {
            var c = selfActors[0];
            casterText = $"caster=0x{c.unit:X}/id=0x{c.unitId:X2} casterActor=0x{c.actorBase:X}";
            actionText = $"actionId={c.actionId}";
            verdict = "resolved-self";
            casterUnit = c.unit;
            casterUnitId = c.unitId;
        }
        else if (distinctCasters.Count == 0)
        {
            casterText = "caster=none";
            actionText = "actionId=-1";
            verdict = "no-caster-actor";
        }
        else
        {
            casterText = "caster=ambiguous(" + string.Join(",", distinctCasters.Select(u => $"0x{u:X}")) + ")";
            actionText = "actionId=-1";
            verdict = "ambiguous";
        }

        _preClampActorCtxLogs++;
        string actorList = actors.Count == 0
            ? "none"
            : string.Join(",", actors.Select(a => $"0x{a.actorBase:X}->id=0x{a.unitId:X2}/act={a.actionId}"));
        Line($"[PRECLAMP-ACTOR-CTX event={sequence} now={nowTick} target=0x{targetPtr:X}/id=0x{targetId:X2} " +
             $"oldDebit={oldDebit} {casterText} {actionText} verdict={verdict} actors=[{actorList}]");

        // Observe-only live equipment readout (validates equipment identity in battle, register book 3.1.5).
        // Logs the target's and the resolved caster's equipment block at the actual damage frame. Gated to
        // real staged damage (oldDebit != 0) to skip passive credit/tick events.
        if (_settings.PreClampLogEquipment && oldDebit != 0 &&
            _preClampEquipLogs < Math.Clamp(_settings.PreClampEquipMaxLogs, 0, 1000))
        {
            _preClampEquipLogs++;
            Line($"[PRECLAMP-EQUIP event={sequence} side=target ptr=0x{targetPtr:X}/id=0x{targetId:X2} {FormatEquip(targetPtr)}");
            if (casterUnit != 0)
                Line($"[PRECLAMP-EQUIP event={sequence} side=caster ptr=0x{casterUnit:X}/id=0x{casterUnitId:X2} {FormatEquip(casterUnit)}");
        }
    }

    // Reads the 7-word equipment block (16-bit LE item-id words) at unitPtr + PreClampEquipBlockOffset:
    // head, body, accessory, R-weapon, R-shield, L-weapon, L-shield. Join item_catalog.csv offline for names.
    private string FormatEquip(nint unitPtr)
    {
        int baseOff = _settings.PreClampEquipBlockOffset;
        var buf = new byte[14];
        if (unitPtr == 0 || !CurrentProcessMemory.TryRead(unitPtr + baseOff, buf, out _))
            return $"equip=unreadable@+0x{baseOff:X}";
        int W(int i) => buf[i] | (buf[i + 1] << 8);
        return $"equip[+0x{baseOff:X}]=[head={W(0)} body={W(2)} acc={W(4)} rWeapon={W(6)} rShield={W(8)} lWeapon={W(10)} lShield={W(12)}]";
    }

    private static bool TryReadLivePtr(nint addr, out nint val)
    {
        val = 0;
        var buf = new byte[IntPtr.Size];
        if (!CurrentProcessMemory.TryRead(addr, buf, out _)) return false;
        val = (nint)BitConverter.ToInt64(buf, 0);
        return true;
    }

    private static bool TryReadLiveU16(nint addr, out int val)
    {
        val = 0;
        var buf = new byte[2];
        if (!CurrentProcessMemory.TryRead(addr, buf, out _)) return false;
        val = buf[0] | (buf[1] << 8);
        return true;
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
        _preClampDamageRewriteHook = null;
        _preClampManagedCallbackWrapper = null;
        _preClampManagedCallback = null;
        _resultSelectorProbeHook = null;
        _evadeInputProbeHook = null;
        _rollVerdictProbeHook = null;
        _landmarkHooks.Clear();
        if (_buf != 0) { try { Marshal.FreeHGlobal(_buf); } catch { } _buf = 0; }
        if (_landmarkBuf != 0) { try { Marshal.FreeHGlobal(_landmarkBuf); } catch { } _landmarkBuf = 0; }
        if (_preClampDamageRewriteBuf != 0) { try { Marshal.FreeHGlobal(_preClampDamageRewriteBuf); } catch { } _preClampDamageRewriteBuf = 0; }
        if (_resultSelectorProbeBuf != 0) { try { Marshal.FreeHGlobal(_resultSelectorProbeBuf); } catch { } _resultSelectorProbeBuf = 0; }
        if (_evadeInputProbeBuf != 0) { try { Marshal.FreeHGlobal(_evadeInputProbeBuf); } catch { } _evadeInputProbeBuf = 0; }
        if (_rollVerdictProbeBuf != 0) { try { Marshal.FreeHGlobal(_rollVerdictProbeBuf); } catch { } _rollVerdictProbeBuf = 0; }
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

internal readonly record struct UnitObservationView(
    nint Ptr,
    int CharId,
    int Team,
    int Hp,
    int MaxHp,
    int Pa,
    int Ma,
    int Brave,
    int Faith);

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
    private readonly AbilityCatalog _abilityCatalog;

    public BattleFormulaEngine(RuntimeSettings settings, ItemCatalog itemCatalog, AbilityCatalog? abilityCatalog = null)
    {
        _settings = settings;
        _itemCatalog = itemCatalog;
        _abilityCatalog = abilityCatalog ?? AbilityCatalog.Empty("");
    }

    public AbilityCatalog AbilityCatalog => _abilityCatalog;

    public DamageResult Evaluate(DamageEvent e)
        => EvaluateCore(e, requireRewriteGate: true);

    public DamageResult EvaluateForStagedApply(DamageEvent e)
        => EvaluateCore(e, requireRewriteGate: false);

    private DamageResult EvaluateCore(DamageEvent e, bool requireRewriteGate)
    {
        if (requireRewriteGate && e.IsDamage && !_settings.RewriteObservedDamage) return DamageResult.NoRewrite(e.CurrentHp);
        if (requireRewriteGate && e.IsHealing && !_settings.RewriteObservedHealing) return DamageResult.NoRewrite(e.CurrentHp);
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
        => FormulaRuntimeContextBuilder.TryApplyDerivedVariables(context, variables, groupName, out error);

    private FormulaContext BuildFormulaContext(
        DamageEvent e,
        int equipmentDr,
        DamageResponse response,
        List<EquipmentSlotValue> targetSlots,
        List<EquipmentSlotValue> attackerSlots)
    {
        var context = new FormulaContext(e.Target, e.Attacker, e.EventIndex, e.EventSeed);

        FormulaRuntimeContextBuilder.AddSettingsVariables(context, _settings);

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
        context.Set("attacker.sourcePending", IsPendingActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourcePending", IsPendingActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("attacker.sourceImmediate", IsImmediateActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourceImmediate", IsImmediateActionSource(e.AttackerSource) ? 1 : 0);
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

        FormulaRuntimeContextBuilder.AddSettingsVariables(context, _settings);

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
        context.Set("attacker.sourcePending", IsPendingActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourcePending", IsPendingActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("attacker.sourceImmediate", IsImmediateActionSource(e.AttackerSource) ? 1 : 0);
        context.Set("a.sourceImmediate", IsImmediateActionSource(e.AttackerSource) ? 1 : 0);
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

    private static bool IsPendingActionSource(string source)
        => source.StartsWith("pending-", StringComparison.OrdinalIgnoreCase);

    private static bool IsImmediateActionSource(string source)
        => source.StartsWith("immediate-", StringComparison.OrdinalIgnoreCase);

    private void AddActionVariables(FormulaContext context, string prefix, ActionSignal? action, DamageEvent e)
    {
        context.Set($"{prefix}.present", action is null ? 0 : 1);
        context.Set($"{prefix}.sourceVanillaDamage", action?.Source.Equals("vanilla-damage", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0);
        context.Set($"{prefix}.sourcePending", action is not null && IsPendingActionSource(action.Source) ? 1 : 0);
        context.Set($"{prefix}.sourceImmediate", action is not null && IsImmediateActionSource(action.Source) ? 1 : 0);
        context.Set($"{prefix}.signal", action?.Get("signal") ?? 0);
        context.Set($"{prefix}.id", action?.Get("id") ?? 0);
        context.Set($"{prefix}.actionId", action?.Get("actionId") ?? 0);
        context.Set($"{prefix}.batch", action?.Get("batch") ?? 0);
        context.Set($"{prefix}.batchEvent", action?.Get("batchEvent") ?? 0);
        context.Set($"{prefix}.batchMaxEvents", action?.Get("batchMaxEvents") ?? 0);
        context.Set($"{prefix}.batchAgeMs", action?.Get("batchAgeMs") ?? 0);
        context.Set($"{prefix}.score", action?.Get("score") ?? 0);
        context.Set($"{prefix}.observedHpLoss", action?.Get("observedHpLoss") ?? 0);
        context.Set($"{prefix}.targetCacheDamage", action?.Get("targetCacheDamage") ?? 0);
        context.Set($"{prefix}.targetCacheCredit", action?.Get("targetCacheCredit") ?? 0);
        context.Set($"{prefix}.targetCacheHealing", action?.Get("targetCacheHealing") ?? 0);
        context.Set($"{prefix}.targetCacheAmount", action?.Get("targetCacheAmount") ?? 0);
        context.Set($"{prefix}.currentTargetCacheDamage", action?.Get("currentTargetCacheDamage") ?? 0);
        context.Set($"{prefix}.currentTargetCacheCredit", action?.Get("currentTargetCacheCredit") ?? 0);
        context.Set($"{prefix}.recentTargetCacheDamage", action?.Get("recentTargetCacheDamage") ?? 0);
        context.Set($"{prefix}.recentTargetCacheCredit", action?.Get("recentTargetCacheCredit") ?? 0);
        context.Set($"{prefix}.damageCacheMatch", action?.Get("damageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentDamageCacheMatch", action?.Get("currentDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentDamageCacheMatch", action?.Get("recentDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.creditCacheMatch", action?.Get("creditCacheMatch") ?? 0);
        context.Set($"{prefix}.currentCreditCacheMatch", action?.Get("currentCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.recentCreditCacheMatch", action?.Get("recentCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.exactDamageCacheMatch", action?.Get("exactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentExactDamageCacheMatch", action?.Get("currentExactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentExactDamageCacheMatch", action?.Get("recentExactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.exactCreditCacheMatch", action?.Get("exactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.currentExactCreditCacheMatch", action?.Get("currentExactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.recentExactCreditCacheMatch", action?.Get("recentExactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.lethalClampDamageCacheMatch", action?.Get("lethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentLethalClampDamageCacheMatch", action?.Get("currentLethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentLethalClampDamageCacheMatch", action?.Get("recentLethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.hasCurrentTargetMetadata", action?.Get("hasCurrentTargetMetadata") ?? 0);
        context.Set($"{prefix}.confidenceDamageCache", action?.Get("confidenceDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceRecentDamageCache", action?.Get("confidenceRecentDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceLethalClampDamageCache", action?.Get("confidenceLethalClampDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceRecentResolve", action?.Get("confidenceRecentResolve") ?? 0);
        context.Set($"{prefix}.runnerUpScore", action?.Get("runnerUpScore") ?? 0);
        context.Set($"{prefix}.margin", action?.Get("margin") ?? 0);
        context.Set($"{prefix}.stateAgeMs", action?.Get("stateAgeMs") ?? 0);
        context.Set($"{prefix}.seenAgeMs", action?.Get("seenAgeMs") ?? 0);
        context.Set($"{prefix}.ctDropAgeMs", action?.Get("ctDropAgeMs") ?? 0);
        context.Set($"{prefix}.actionIdAgeMs", action?.Get("actionIdAgeMs") ?? 0);
        context.Set($"{prefix}.activeActionAgeMs", action?.Get("activeActionAgeMs") ?? 0);
        context.Set($"{prefix}.currentActiveAction", action?.Get("currentActiveAction") ?? 0);
        context.Set($"{prefix}.freshActionId", action?.Get("freshActionId") ?? 0);
        context.Set($"{prefix}.freshActiveAction", action?.Get("freshActiveAction") ?? 0);
        context.Set($"{prefix}.staleActionId", action?.Get("staleActionId") ?? 0);
        context.Set($"{prefix}.staleActiveAction", action?.Get("staleActiveAction") ?? 0);
        context.Set($"{prefix}.activeMarker2", action?.Get("activeMarker2") ?? 0);
        context.Set($"{prefix}.pendingFlag", action?.Get("pendingFlag") ?? 0);
        context.Set($"{prefix}.pendingFlag2", action?.Get("pendingFlag2") ?? 0);
        context.Set($"{prefix}.pendingTimer", action?.Get("pendingTimer") ?? 0);
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
        context.Set($"{prefix}.sourcePending", action is not null && IsPendingActionSource(action.Source) ? 1 : 0);
        context.Set($"{prefix}.sourceImmediate", action is not null && IsImmediateActionSource(action.Source) ? 1 : 0);
        context.Set($"{prefix}.signal", action?.Get("signal") ?? 0);
        context.Set($"{prefix}.id", action?.Get("id") ?? 0);
        context.Set($"{prefix}.actionId", action?.Get("actionId") ?? 0);
        context.Set($"{prefix}.batch", action?.Get("batch") ?? 0);
        context.Set($"{prefix}.batchEvent", action?.Get("batchEvent") ?? 0);
        context.Set($"{prefix}.batchMaxEvents", action?.Get("batchMaxEvents") ?? 0);
        context.Set($"{prefix}.batchAgeMs", action?.Get("batchAgeMs") ?? 0);
        context.Set($"{prefix}.score", action?.Get("score") ?? 0);
        context.Set($"{prefix}.observedHpLoss", action?.Get("observedHpLoss") ?? 0);
        context.Set($"{prefix}.targetCacheDamage", action?.Get("targetCacheDamage") ?? 0);
        context.Set($"{prefix}.targetCacheCredit", action?.Get("targetCacheCredit") ?? 0);
        context.Set($"{prefix}.targetCacheHealing", action?.Get("targetCacheHealing") ?? 0);
        context.Set($"{prefix}.targetCacheAmount", action?.Get("targetCacheAmount") ?? 0);
        context.Set($"{prefix}.currentTargetCacheDamage", action?.Get("currentTargetCacheDamage") ?? 0);
        context.Set($"{prefix}.currentTargetCacheCredit", action?.Get("currentTargetCacheCredit") ?? 0);
        context.Set($"{prefix}.recentTargetCacheDamage", action?.Get("recentTargetCacheDamage") ?? 0);
        context.Set($"{prefix}.recentTargetCacheCredit", action?.Get("recentTargetCacheCredit") ?? 0);
        context.Set($"{prefix}.damageCacheMatch", action?.Get("damageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentDamageCacheMatch", action?.Get("currentDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentDamageCacheMatch", action?.Get("recentDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.creditCacheMatch", action?.Get("creditCacheMatch") ?? 0);
        context.Set($"{prefix}.currentCreditCacheMatch", action?.Get("currentCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.recentCreditCacheMatch", action?.Get("recentCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.exactDamageCacheMatch", action?.Get("exactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentExactDamageCacheMatch", action?.Get("currentExactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentExactDamageCacheMatch", action?.Get("recentExactDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.exactCreditCacheMatch", action?.Get("exactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.currentExactCreditCacheMatch", action?.Get("currentExactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.recentExactCreditCacheMatch", action?.Get("recentExactCreditCacheMatch") ?? 0);
        context.Set($"{prefix}.lethalClampDamageCacheMatch", action?.Get("lethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.currentLethalClampDamageCacheMatch", action?.Get("currentLethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.recentLethalClampDamageCacheMatch", action?.Get("recentLethalClampDamageCacheMatch") ?? 0);
        context.Set($"{prefix}.hasCurrentTargetMetadata", action?.Get("hasCurrentTargetMetadata") ?? 0);
        context.Set($"{prefix}.confidenceDamageCache", action?.Get("confidenceDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceRecentDamageCache", action?.Get("confidenceRecentDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceLethalClampDamageCache", action?.Get("confidenceLethalClampDamageCache") ?? 0);
        context.Set($"{prefix}.confidenceRecentResolve", action?.Get("confidenceRecentResolve") ?? 0);
        context.Set($"{prefix}.runnerUpScore", action?.Get("runnerUpScore") ?? 0);
        context.Set($"{prefix}.margin", action?.Get("margin") ?? 0);
        context.Set($"{prefix}.stateAgeMs", action?.Get("stateAgeMs") ?? 0);
        context.Set($"{prefix}.seenAgeMs", action?.Get("seenAgeMs") ?? 0);
        context.Set($"{prefix}.ctDropAgeMs", action?.Get("ctDropAgeMs") ?? 0);
        context.Set($"{prefix}.actionIdAgeMs", action?.Get("actionIdAgeMs") ?? 0);
        context.Set($"{prefix}.activeActionAgeMs", action?.Get("activeActionAgeMs") ?? 0);
        context.Set($"{prefix}.currentActiveAction", action?.Get("currentActiveAction") ?? 0);
        context.Set($"{prefix}.freshActionId", action?.Get("freshActionId") ?? 0);
        context.Set($"{prefix}.freshActiveAction", action?.Get("freshActiveAction") ?? 0);
        context.Set($"{prefix}.staleActionId", action?.Get("staleActionId") ?? 0);
        context.Set($"{prefix}.staleActiveAction", action?.Get("staleActiveAction") ?? 0);
        context.Set($"{prefix}.activeMarker2", action?.Get("activeMarker2") ?? 0);
        context.Set($"{prefix}.pendingFlag", action?.Get("pendingFlag") ?? 0);
        context.Set($"{prefix}.pendingFlag2", action?.Get("pendingFlag2") ?? 0);
        context.Set($"{prefix}.pendingTimer", action?.Get("pendingTimer") ?? 0);
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
        => FormulaRuntimeContextBuilder.AddSlotVariables(context, prefix, slots);

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
        => FormulaRuntimeContextBuilder.AddUnitVariables(context, prefix, unit);

    private List<EquipmentSlotValue> ReadEquipmentSlots(UnitSnapshot? unit, List<EquipmentSlotProbe> probes)
        => FormulaRuntimeContextBuilder.ReadEquipmentSlots(unit, probes, _itemCatalog);

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

internal sealed class LandmarkProbe
{
    private static readonly string[] AllowedBaseRegisters =
    [
        "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp", "rsp",
        "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
    ];

    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public int Rva { get; set; } = 0;
    public string BaseRegister { get; set; } = "rdi";
    public string Access { get; set; } = "";
    public string ExpectedBytes { get; set; } = "";
    public List<int> InterestingOffsets { get; set; } = new();

    public string TraceName { get; private set; } = "landmark";

    public void Normalize()
    {
        TraceName = FormulaExpression.NormalizeIdentifierPart(string.IsNullOrWhiteSpace(Name) ? $"rva_{Rva:X}" : Name);
        BaseRegister = string.IsNullOrWhiteSpace(BaseRegister) ? "rdi" : BaseRegister.Trim().ToLowerInvariant();
        Access = string.IsNullOrWhiteSpace(Access) ? "observe" : Access.Trim();
        ExpectedBytes = ExpectedBytes?.Trim() ?? "";
        InterestingOffsets ??= new List<int>();
    }

    public bool TryValidate(out string error)
    {
        error = "";
        if (Rva <= 0)
        {
            error = "Rva must be positive.";
            return false;
        }

        if (!AllowedBaseRegisters.Any(name => string.Equals(name, BaseRegister, StringComparison.OrdinalIgnoreCase)))
        {
            error = $"BaseRegister '{BaseRegister}' is not one of {string.Join(",", AllowedBaseRegisters)}.";
            return false;
        }

        if (InterestingOffsets.Any(offset => offset < 0 || offset >= 0x200))
        {
            error = "InterestingOffsets must stay within 0x000..0x1FF.";
            return false;
        }

        return true;
    }
}

// One write applied to a unit's struct when our formula kills it (HP forced to 0), used to set the
// death/status state the engine would normally set itself. Read-modify-write so a single status BIT
// can be set without clobbering the rest of the field. Configured in JSON once the offset is mapped.
internal sealed class DeathStateWrite
{
    private const int CopiedRawSize = 0x200;

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
    public bool DclPipelineEnabled { get; set; } = false;
    public string DclDamageFormula { get; set; } = "";
    public List<FormulaDerivedVariable> DclDerivedVariables { get; set; } = new();
    public int DclActionContextMaxAgeMs { get; set; } = 5000;
    public int DclDecisionMaxLogs { get; set; } = 40;
    // DCL HIT CONTROL — authored hit% + own RNG + binary outcome forcing at calc-entry (0x309A44).
    // The managed decision callback evaluates DclHitChanceFormula in the full DCL context (pre-roll:
    // dcl.oldDebit/oldCredit are 0), rolls the mod's own RNG (or uses DclHitForcedRoll), and stamps
    // the TARGET's evade input bytes before the VM avoidance roll: HIT => 0x46..0x4E all 0; MISS =>
    // all 0 except class evade +0x4B = DclMissClassEvadeValue (100 = guaranteed class-evade "Miss",
    // proven LT5-B / 2026-06-27). Requires the LT5-A4 baseline stack (ItemTableEvadeZero +
    // all-zero EvadeCopierOverride) so residual equipment evade cannot steal a HIT decision.
    public bool DclHitControlEnabled { get; set; } = false;
    public string DclHitChanceFormula { get; set; } = "";
    public int DclHitDecisionTtlMs { get; set; } = 2500;    // decision reuse window per (caster,target,ability,type)
    public int DclHitForcedRoll { get; set; } = -1;         // -1 = real RNG; 0..99 = deterministic roll (live tests)
    public int DclHitMaxLogs { get; set; } = 400;           // [DCL-HIT*] log budget (separate from DclDecisionMaxLogs)
    public int DclMissClassEvadeValue { get; set; } = 100;  // byte for +0x4B on MISS (LT5-B proven force-Miss value)
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
    public string AbilityCatalogPath { get; set; } = "wotl_ability_action_baseline.csv";
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
    // Legacy diagnostic attacker resolution by CT (+0x41). Useful for comparing old captures, but
    // too fragile for DCL ownership; native actor/pending/selector context is the accepted path.
    public bool ResolveAttackerByCt { get; set; } = false;
    public int CtDropWindowMs { get; set; } = 4000;
    // Polling can miss the exact CT drop if the first observed post-action frame already has low CT.
    // This fallback only considers alive, non-target units that were hook-touched recently and still
    // have a near-reset CT value.
    public bool ResolveAttackerByLowCtFallback { get; set; } = false;
    public int CtLowFallbackMaxCt { get; set; } = 20;
    public int CtLowFallbackWindowMs { get; set; } = 1500;
    public bool LogCtResolutionDiagnostics { get; set; } = false;
    // Legacy diagnostic counter inversion from the previous HP-damage pair. Native actor/selector
    // context is preferred for reactions.
    public bool ResolveCounterFromRecentDamage { get; set; } = false;
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
    public bool HookRegisterProbeOnHpEvent { get; set; } = false;
    public bool HookRegisterProbeOnMpEvent { get; set; } = false;
    public bool HookRegisterProbeOnCtDrop { get; set; } = false;
    public bool HookRegisterProbeOnActionBoundary { get; set; } = false;
    public bool HookRegisterProbeOnPendingResolve { get; set; } = false;
    public bool HookRegisterProbeOnTargetCache { get; set; } = false;
    public int HookRegisterProbeEventMaxLogs { get; set; } = 64;
    public int HookRegisterProbeStackSlots { get; set; } = 0;
    public int HookRegisterProbePointerScanBytes { get; set; } = 0;
    public int HookRegisterProbePointerMaxLogs { get; set; } = 64;
    public int HookRegisterProbePointerMaxPointersPerRoot { get; set; } = 8;
    public bool LandmarkProbeEnabled { get; set; } = false;
    public int LandmarkProbeMaxLogs { get; set; } = 160;
    public int LandmarkProbeStackSlots { get; set; } = 16;
    public List<LandmarkProbe> LandmarkProbes { get; set; } = new();

    // OBSERVE-ONLY result/animation selector probe. Hooks the result selector prologue (RVA 0x205210)
    // and logs the evade-type byte the caller passes in cl (0x00=hit, 0x04=block/guard, 0x01/2/3=guard
    // variants, 0x06=miss) plus a window of the actor's result record at [actor+RecordUnitOffset]. This
    // captures, for the first time, what a MISS/BLOCK looks like in memory. No write/force capability.
    public bool ResultSelectorProbeEnabled { get; set; } = false;
    public int ResultSelectorProbeRva { get; set; } = 0x205210;
    public string ResultSelectorProbeExpectedBytes { get; set; } = "48 89 5C 24 08 48 89 6C 24 10";
    public string ResultSelectorProbeActorRegister { get; set; } = "r8";
    public int ResultSelectorProbeRecordUnitOffset { get; set; } = 0x148;
    public int ResultSelectorProbeMaxLogs { get; set; } = 256;
    public int ResultSelectorProbeRecordDumpBytes { get; set; } = 64;
    // Result-selector CONTROL (rides the observe hook; requires ResultSelectorProbeEnabled). Two opt-ins
    // (Enabled + !LogOnly) before any write. Forces the rendered outcome by writing the evade-type/result-code.
    public bool ResultSelectorControlEnabled { get; set; } = false;   // master; false => observe-only (unchanged)
    public bool ResultSelectorControlLogOnly { get; set; } = true;    // SAFETY: true => log would-write intent, never write
    public int ResultSelectorControlMaxWrites { get; set; } = 1;      // 1..32 cap on live writes per session
    public int ResultSelectorControlTargetCharId { get; set; } = -1;  // -1=any; else require record[+0x00] charId == this
    public int ResultSelectorControlMatchEvadeType { get; set; } = -1;// -1=any; else only act when natural evade-type == this
    public int ResultSelectorControlForceEvadeType { get; set; } = -1;// -1=no change; else byte -> cl + [record+0x1C0]
    public int ResultSelectorControlForceResultCode { get; set; } = -1;// -1=no change; else byte -> [record+0x1BE] (0 for evade)

    // Evade-INPUT control (hook at RVA 0x30F49C, the last real instr before the single VM avoidance roll
    // 0x30FA34; target unit in rbx). Writes the target's evade INPUT bytes (+0x46/+0x47 weapon, +0x4A/+0x4E
    // shield, +0x4B class) just before the VM reads them, so the engine's native roll produces our outcome
    // (all 0 => guaranteed hit; one source = 100 => that evade type). Probe logs before/after bytes.
    public bool EvadeInputProbeEnabled { get; set; } = false;
    public int EvadeInputProbeRva { get; set; } = 0x30F49C;
    public string EvadeInputProbeExpectedBytes { get; set; } = "48 8B D3 88 4B 41";
    public int EvadeInputProbeMaxLogs { get; set; } = 64;
    public bool EvadeInputControlEnabled { get; set; } = false;   // master; false => observe-only
    public bool EvadeInputControlLogOnly { get; set; } = true;    // SAFETY: true => log would-write intent, never write
    public int EvadeInputControlMaxWrites { get; set; } = 1;      // 1..32 cap on live writes per session
    public int EvadeInputControlTargetCharId { get; set; } = -1;  // -1=any; else require [rbx+0x00] charId == this
    public int EvadeInputForce46 { get; set; } = -1;             // weapon evade 1 (-1 leave; else byte)
    public int EvadeInputForce47 { get; set; } = -1;             // weapon evade 2
    public int EvadeInputForce4A { get; set; } = -1;             // shield evade 1
    public int EvadeInputForce4B { get; set; } = -1;             // class evade
    public int EvadeInputForce4E { get; set; } = -1;             // shield evade 2

    // Roll-VERDICT control (hook at RVA 0x30F4A7 = "mov r10d,eax" right after the VM avoidance roll
    // 0x30FA34 returns). eax is the hit/miss verdict in REAL code (then: test eax,eax; je miss). Forcing
    // eax overrides native evade+reactions (both virtualized inside the roll) with NO data change. Set
    // ForceVerdict=1 => guaranteed hit (apply path runs; author damage at the pre-clamp); 0 => miss; 2 =
    // engine "special" outcome. The probe logs native vs final eax per roll. Observe-only by default.
    public bool RollVerdictProbeEnabled { get; set; } = false;
    public int RollVerdictProbeRva { get; set; } = 0x30F4A7;
    public string RollVerdictProbeExpectedBytes { get; set; } = "44 8B D0 BD 01 00 00 00 85 C0";
    public int RollVerdictProbeMaxLogs { get; set; } = 128;
    public bool RollVerdictControlEnabled { get; set; } = false;  // master; false => observe-only
    public bool RollVerdictControlLogOnly { get; set; } = true;   // SAFETY: true => log would-override intent, never write
    public int RollVerdictControlMaxWrites { get; set; } = 1;     // 1..32 cap on live overrides per session
    public int RollVerdictControlTargetCharId { get; set; } = -1; // -1=any acting unit; else require [rbx+0x00] charId == this
    public int RollVerdictControlForceVerdict { get; set; } = -1; // -1=observe; 0=miss; 1=hit; 2=engine special

    // Persistent evade INPUT override (the original input-control test). The unit poller writes these evade
    // bytes on the LIVE battle struct of the matching unit every poll (~20ms), before the VM avoidance roll
    // reads them. Set 0x4B (class evade) etc. to 100 on a unit, then have it attacked: if it dodges / the
    // hit% forecast drops, the VM reads live memory => input-control viable. -1 leaves a byte untouched.
    public bool EvadeOverrideEnabled { get; set; } = false;
    public int EvadeOverrideTargetCharId { get; set; } = -1;   // -1=all tracked units; else only this charId (+0x00)
    public int EvadeOverride46 { get; set; } = -1;             // weapon parry R %
    public int EvadeOverride47 { get; set; } = -1;             // weapon parry L %
    public int EvadeOverride48 { get; set; } = -1;             // accessory magic-evade partner (inferred)
    public int EvadeOverride49 { get; set; } = -1;             // accessory/cloak physical evade (inferred -> evadeType 0x01)
    public int EvadeOverride4A { get; set; } = -1;             // shield physical parry %
    public int EvadeOverride4B { get; set; } = -1;             // class/physical evasion %
    public int EvadeOverride4C { get; set; } = -1;             // magic-evasion partner (inferred)
    public int EvadeOverride4D { get; set; } = -1;             // accessory magick partner (inferred)
    public int EvadeOverride4E { get; set; } = -1;             // shield magick parry %
    public int EvadeOverrideMaxLogs { get; set; } = 64;
    // When broadcasting (TargetCharId<0), also sweep this many 0x200-slots beyond the tracked-unit span
    // and boost any valid unit found there. Catches units the poller never registered (e.g. the defender
    // of an attack that hasn't been near a hook). 0 = only tracked units. ReadProcessMemory is safe on
    // unmapped addresses (returns false), so a generous margin is harmless.
    public int EvadeOverrideSweepSlots { get; set; } = 0;

    // EvadeCopierOverride — airtight, race-free replacement for the EvadeOverride poll. Hooks the 3
    // real-code equip/refresh copier tails (fixed RVAs) and over-stamps the defender's evade bytes so
    // they persist to the VM roll with no race (RE work/dcl-miss-block-parry-DEFINITIVE-2026-07-03.md).
    // Values -1 = leave that byte. TargetCharId -1 = every unit; else only the unit whose +0x00 == id.
    // all=0 ⇒ forced HIT; one source (46/47 weapon, 49 accessory, 4A/4E shield, 4B class) high ⇒ that avoid type.
    public bool EvadeCopierOverrideEnabled { get; set; } = false;
    public int EvadeCopierOverrideTargetCharId { get; set; } = -1;
    public int EvadeCopierOverride46 { get; set; } = -1;   // weapon parry R %
    public int EvadeCopierOverride47 { get; set; } = -1;   // weapon parry L %
    public int EvadeCopierOverride48 { get; set; } = -1;   // accessory magic-evade partner (inferred)
    public int EvadeCopierOverride49 { get; set; } = -1;   // accessory/cloak physical evade -> evadeType 0x01
    public int EvadeCopierOverride4A { get; set; } = -1;   // shield physical block %
    public int EvadeCopierOverride4B { get; set; } = -1;   // class/physical evasion %
    public int EvadeCopierOverride4C { get; set; } = -1;   // magic-evasion partner (inferred)
    public int EvadeCopierOverride4D { get; set; } = -1;   // accessory magick partner (inferred)
    public int EvadeCopierOverride4E { get; set; } = -1;   // shield magick block %

    // CalcEntryEvadeStamp — per-attack delivery of the SAME EvadeCopierOverride* value profile:
    // stamps the target's evade bytes at computeActionResult (CalcEntryProbeRva, 0x309A44) right
    // before the VM avoidance roll inside that call. Zero-width race window; closes the residual
    // VM-side/init re-stamp leak seen in LT5-A. Works with or without CalcEntryProbeEnabled.
    public bool CalcEntryEvadeStampEnabled { get; set; } = false;

    // ReactionChanceControl — the 4 real-code Brave-gate reaction roll sites (0x30BE86/0x30BEDC/
    // 0x30BF32/0x30BF72). ForcedChance: -1 = observe only (log fires + natural Brave), 0 = suppress
    // ALL reactions (Blade Grasp/Hamedo/Counter... never arm), 100 = force every reaction.
    public bool ReactionChanceControlEnabled { get; set; } = false;
    public int ReactionChanceForcedChance { get; set; } = -1;

    // EvadeRecordOverride — forces the PACKED evade fields in the combat-input record at the 3
    // builder store sites (the values the preview/roll actually consume; unit-byte stamps do NOT
    // reach the shield leg). 44 = class (copy of +0x4B), 46 = shield/accessory physical
    // (MAX(+0x4A,+0x49)), 50 = shield/accessory magick (MAX(+0x4D,+0x4E)). -1 = leave natural.
    // GLOBAL (all units both teams) — no per-unit filter at these sites yet.
    public bool EvadeRecordOverrideEnabled { get; set; } = false;
    public int EvadeRecordOverride44 { get; set; } = -1;
    public int EvadeRecordOverride46 { get; set; } = -1;
    public int EvadeRecordOverride50 { get; set; } = -1;

    // ItemTableEvadeZero — zeroes the per-item evade bytes in the LOADED item stat tables (fixed
    // VAs, writable) every poll: weapon W-Ev, shield phys/magic, accessory phys/magic. The VM derives
    // equipment evade from these tables, so its own derivation yields 0 for every unit — the
    // source-level equipment-evade kill (armor HP/MP table untouched). Sanity-gated (Venetian 32 19).
    public bool ItemTableEvadeZeroEnabled { get; set; } = false;

    // Persistent Brave/Faith INPUT override (the reaction input-control test). Same live-write mechanism
    // as EvadeOverride, for the bytes that gate Brave%-rolled reactions (Blade Grasp, Hamedo, Arrow
    // Guard...). Set 2B (Brave) to 0 on a unit that has a reaction + normally-high Brave, then attack it:
    // if the reaction stops firing, the VM read the live Brave => reaction input-control viable. Set 2B
    // to 100 to force a reaction. -1 leaves a byte untouched. +0x2A MaxBrave, +0x2B Brave, +0x2C MaxFaith,
    // +0x2D Faith.
    public bool BraveOverrideEnabled { get; set; } = false;
    public int BraveOverrideTargetCharId { get; set; } = -1;    // -1=all tracked units; else only this charId (+0x00)
    public int BraveOverride2A { get; set; } = -1;             // MaxBrave
    public int BraveOverride2B { get; set; } = -1;             // Brave (the reaction-roll input)
    public int BraveOverride2C { get; set; } = -1;             // MaxFaith
    public int BraveOverride2D { get; set; } = -1;             // Faith
    public int BraveOverrideMaxLogs { get; set; } = 64;
    public int BraveOverrideSweepSlots { get; set; } = 0;      // same broadcast sweep as EvadeOverrideSweepSlots

    // Persistent STATUS INPUT override (status-control test). OR a bit MASK onto the status bytes every
    // poll. Offline RE: +0x61 effective = (+0x1EF & 0xF2) | +0x57; engine tests +0x61 (KO 0x20, control-
    // flip 0x10, petrify 0x40, can't-act 0x08, charging 0x04). Force a status by OR-ing the master +0x1EF
    // AND the mirror +0x61 (e.g. 0x10 = control-flip = the most visual: unit switches to AI/ally). -1 =
    // skip. +0x57 = innate/equip source (OR it for equip-sourced statuses).
    public bool StatusOverrideEnabled { get; set; } = false;
    public int StatusOverrideTargetCharId { get; set; } = -1;  // -1=all tracked units; else only this charId (+0x00)
    public int StatusOverride1EF { get; set; } = -1;           // master/volatile status bitmask to OR
    public int StatusOverride61 { get; set; } = -1;            // effective status bitmask to OR (mirror)
    public int StatusOverride57 { get; set; } = -1;            // innate/equipment status bitmask to OR
    public int StatusOverrideMaxLogs { get; set; } = 64;
    public int StatusOverrideSweepSlots { get; set; } = 0;     // same broadcast sweep as EvadeOverrideSweepSlots

    // Preview hit-% display control (DCL Layer 1). Hooks the real-code copy of the computed
    // hit% into the on-screen forecast buffer (RVA 0x7832C0) and forces a chosen value at copy
    // time, so the displayed % is deterministically ours (no race with the engine's recompute).
    public bool PreviewHitPctControlEnabled { get; set; } = false;
    public int PreviewHitPctRva { get; set; } = 0x227FFE;                       // `mov r10d,2`, between the load and the store
    public string PreviewHitPctExpectedBytes { get; set; } = "41 BA 02 00 00 00";
    public int PreviewHitPctForcedValue { get; set; } = -1;                     // 0..65535 to force; -1 = observe only
    public bool PreviewHitPctLogOnly { get; set; } = false;                     // record natural % without overwriting

    // PREVIEW DAMAGE display control (twin of the hit% hook). Hooks the terminal `jmp 0x228488` of the
    // forecast-number dispatch and sets dx before the store at 0x228488 writes the on-screen number to
    // 0x1407832BE. Default RVA 0x2280D7 = primary attack/damage branch. Purely visual; does not change
    // the real damage (that is the pre-clamp lever).
    public bool PreviewDamageControlEnabled { get; set; } = false;
    public int PreviewDamageForcedValue { get; set; } = -1;                     // 0..65535 to force; -1 = observe only
    public bool PreviewDamageLogOnly { get; set; } = false;                     // record natural number without overwriting (hooks all numeric branches)

    // FORECAST DAMAGE SOURCE control — the COHERENT lever. Hooks the finalizers that write obj+0x6
    // (== unit+0x1C4, the staged-damage field that drives the preview NUMBER + HP-bar + apply). Forcing
    // this makes the on-screen number AND the ghost HP-bar both reflect our value, unlike the cosmetic
    // display-number paint. Pair with the pre-clamp force (same +0x1C4) so preview == result.
    public bool PreviewForecastSourceControlEnabled { get; set; } = false;
    public int PreviewForecastSourceForcedValue { get; set; } = -1;             // 0..65535 to force; -1 = observe only
    public bool PreviewForecastSourceLogOnly { get; set; } = false;             // record natural staged-damage without overwriting

    // CALC-ENTRY PROBE (LT3, log-only) — ring-buffer probe on computeActionResult (0x309A44), the single
    // real-code per-(action,target) calc entry. Logs caster slot/type/ability id + target index + caster
    // team + turn owner per fire: the PREVIEW-time ability id and the AI same-calc discriminator.
    public bool CalcEntryProbeEnabled { get; set; } = false;
    public int CalcEntryProbeRva { get; set; } = 0x309A44;

    // MAGIC-ACCURACY control (LT3) — hook at 0x304E2E captures the natural Faith-scaled chance in edx
    // (loaded at 0x304E2B) and can force it before roll(100, edx) at 0x304E33. 100 = always-hit, 0 =
    // always-miss, -1 = observe only.
    public bool MagicAccuracyControlEnabled { get; set; } = false;
    public int MagicAccuracyRva { get; set; } = 0x304E2E;
    public int MagicAccuracyForcedChance { get; set; } = -1;

    // STATUS-CHANCE control (LT3) — hook at 0x306633 captures/forces the status infliction chance in edx
    // (loaded from g_7B07AC at 0x30662C) before roll(100, edx) at 0x306636. Compute-time, so it wins where
    // the LT2 data-poke of g_7B07AC lost. 100 = always-proc, 0 = never-proc, -1 = observe only.
    public bool StatusChanceControlEnabled { get; set; } = false;
    public int StatusChanceRva { get; set; } = 0x306633;
    public int StatusChanceForcedChance { get; set; } = -1;

    // ROLL-RNG PROBE (LT3b, log-only) — ring probe on the shared RNG head (0x278EE0, a Denuvo
    // trampoline whose CALLERS are real code). Logs (caller RVA, range, chance) per call to map every
    // real roll site (magic accuracy, status infliction, reactions, crits) in one battle.
    public bool RollRngProbeEnabled { get; set; } = false;
    public int RollRngProbeRva { get; set; } = 0x278EE0;

    // STAGED-BUNDLE PROBE (LT4) — the OUTPUT window at the sweep post-call (0x281F8A), right after the
    // VM stages the effect bundle on the target and before apply. Logs target+0x1C0/+0x1C4/+0x1A8/
    // +0x1D0/+0x1E5, and (gated on ForceTargetCharId) overwrites any field whose Force* is >= 0:
    // Kind (+0x1C0), Ailment (+0x1A8), ApplyMask (+0x1D0), Dmg (+0x1C4). Proves output control of
    // status infliction and magic miss->hit without touching the (VM-internal) roll.
    public bool StagedBundleProbeEnabled { get; set; } = false;
    public int StagedBundleProbeRva { get; set; } = 0x281F8A;
    public int StagedBundleForceTargetCharId { get; set; } = -1;   // -1 = observe only; else force only this charId
    public int StagedBundleForceKind { get; set; } = -1;           // -1 = leave; else byte -> +0x1C0
    public int StagedBundleForceAilment { get; set; } = -1;        // -1 = leave; else word -> +0x1A8
    public int StagedBundleForceApplyMask { get; set; } = -1;      // -1 = leave; else byte -> +0x1D0
    public int StagedBundleForceDmg { get; set; } = -1;            // -1 = leave; else word -> +0x1C4
    public int StagedBundleForceResFlag { get; set; } = -1;        // -1 = leave; else byte -> +0x1E5 (0x80 hit/apply, 0x00 miss)

    // FORECAST PREVIEW POKE — the universal preview HP-amount lever. With DamageFieldOffset=0x6 it writes
    // obj+0x6 == unit+0x1C4 (damage/debit); with 0x8 it writes obj+0x8 == unit+0x1C6 (healing/credit).
    // Drives the on-screen forecast NUMBER and the HP-bar ghost together. Set Enabled + PokeValue; structural
    // RVAs default to the known stable addresses (no ASLR) but stay overridable in case of a game update.
    public bool PreviewForecastPokeEnabled { get; set; } = false;
    public int PreviewForecastPokeValue { get; set; } = -1;                     // staged HP amount to show; -1 = off
    public int PreviewForecastGlobalRva { get; set; } = 0x2FF3CF8;              // global holding the forecast object ptr (0x142FF3CF8)
    public int PreviewForecastUnitTableRva { get; set; } = 0x1853CE0;          // unit table base (0x141853CE0)
    public int PreviewForecastUnitStride { get; set; } = 0x200;                // per-unit stride
    public int PreviewForecastObjOffset { get; set; } = 0x1BE;                 // forecast object = unit + 0x1BE
    public int PreviewForecastDamageFieldOffset { get; set; } = 0x6;           // obj+0x6 damage/debit; obj+0x8 healing/credit

    public bool PreClampDamageRewriteEnabled { get; set; } = false;
    public int PreClampDamageRewriteRva { get; set; } = 0x30A66F;
    public string PreClampDamageRewriteExpectedBytes { get; set; } = "0F BF 45 06";
    public int PreClampDamageRewriteTargetCharId { get; set; } = -1;
    public int PreClampDamageRewriteTargetTeam { get; set; } = -1;
    public int PreClampDamageRewriteExpectedDebit { get; set; } = -1;
    public int PreClampDamageRewriteExpectedCredit { get; set; } = -1;
    public int PreClampDamageRewriteMinHp { get; set; } = 1;
    public int PreClampDamageRewriteMaxHp { get; set; } = 9999;
    public int PreClampDamageRewriteForcedDebit { get; set; } = -1;
    public int PreClampDamageRewriteForcedCredit { get; set; } = -1;
    public int PreClampDamageRewriteMaxWrites { get; set; } = 1;
    public bool PreClampDamageRewriteLogOnly { get; set; } = false;
    public bool PreClampManagedCallbackEnabled { get; set; } = false;
    public int PreClampManagedCallbackForcedDebit { get; set; } = -1;
    public bool PreClampManagedCallbackActorFormulaEnabled { get; set; } = false;
    public int PreClampManagedCallbackPaMultiplier { get; set; } = 10;
    public int PreClampManagedCallbackFormulaMinDamage { get; set; } = 1;
    public int PreClampManagedCallbackFormulaMaxDamage { get; set; } = 9999;
    public int PreClampManagedCallbackStackScanBytes { get; set; } = 0x100;
    public bool LogPreClampFormulaCandidates { get; set; } = false;
    public int PreClampFormulaCandidateMaxLogs { get; set; } = 64;
    public bool PreClampFormulaCandidateRequirePendingMatch { get; set; } = true;
    public bool PreClampFormulaCandidateAllowImmediateAction { get; set; } = false;
    public int PreClampImmediateActionMinScore { get; set; } = 1800;
    public int PreClampImmediateActionMinMargin { get; set; } = 300;
    public int PreClampImmediateActionMaxAgeMs { get; set; } = 1500;
    public bool PreClampImmediateActionRequireFreshActive { get; set; } = true;
    public bool PreClampImmediateActionAllowZeroActionId { get; set; } = false;
    public int PreClampImmediateActionPlanMaxWrites { get; set; } = 1;
    public bool PreClampImmediateActionPlanRequireExpectedHp { get; set; } = true;
    public bool PreClampImmediateActionPlanEagerTargets { get; set; } = false;
    public int PreClampImmediateActionNearbyUnitScanRadius { get; set; } = 8;
    public bool PreClampFormulaPlanEnabled { get; set; } = false;
    public int PreClampFormulaPlanSlots { get; set; } = 16;
    public int PreClampFormulaPlanWindowMs { get; set; } = 3000;
    public int PreClampFormulaPlanMaxWrites { get; set; } = 1;
    public bool PreClampFormulaPlanRequirePhaseZero { get; set; } = true;
    public int PreClampPointerScanBytes { get; set; } = 0;
    public int PreClampPointerMaxLogs { get; set; } = 64;
    public int PreClampPointerMaxPointersPerRoot { get; set; } = 8;

    // Actor-struct dump: during the pre-clamp pointer scan, when a scanned root turns out to be a battle
    // participant/actor struct (it links to a registered unit at PreClampActorStructUnitOffset, observed
    // as +0x148 with a 0x548 stride array), dump a raw byte window of that struct. Goal: find where the
    // resolving action id (e.g. Cross Slash 0x0102) and target/charge fields live inside the actor struct,
    // so caster+action can be read straight from engine memory at damage time (retiring CT + pending tracker).
    public bool PreClampActorStructDumpEnabled { get; set; } = false;
    public int PreClampActorStructDumpBytes { get; set; } = 0x200;
    public int PreClampActorStructUnitOffset { get; set; } = 0x148;
    public int PreClampActorStructDumpMaxLogs { get; set; } = 32;

    // Observe-only memory-only action-context resolver. At the pre-clamp damage frame, derive
    // caster + action id from the battle actor array (register book 2.4): caster = stack/register actor
    // whose +PreClampActorStructUnitOffset != target, or a target-linked actor with actionId > 0 for
    // self-hit/self-AoE; actionId = caster actor + PreClampActorActionIdOffset.
    // Emits [PRECLAMP-ACTOR-CTX] so it can be compared head-to-head with the pending tracker / CT before
    // being made primary. Does not change behavior.
    public bool PreClampResolveActorContext { get; set; } = false;
    public int PreClampActorActionIdOffset { get; set; } = 0x142;
    public int PreClampActorContextMaxLogs { get; set; } = 64;

    // Observe-only live equipment readout at the pre-clamp damage frame. Logs [PRECLAMP-EQUIP] for the
    // target and the resolved caster: the 7-word equipment block (item-id words at PreClampEquipBlockOffset:
    // head, body, accessory, R-weapon, R-shield, L-weapon, L-shield). Validates that equipment identity for
    // both sides can be read live in battle (register book 3.1.5). Needs PreClampResolveActorContext for the
    // caster side. Does not change behavior.
    public bool PreClampLogEquipment { get; set; } = false;
    public int PreClampEquipBlockOffset { get; set; } = 0x1A;
    public int PreClampEquipMaxLogs { get; set; } = 64;

    // Actor probe: on each HP damage event, snapshot a small byte window of EVERY registered unit so we can
    // find which unit just acted (the attacker) by its turn-state/CT signature. Window is JSON-tunable (no
    // rebuild) so we can widen/narrow the search. Goal: map reliable attacker resolution + a pre-damage signal.
    public bool ActorProbeOnEvent { get; set; } = false;
    public int ActorProbeStart { get; set; } = 0x40;
    public int ActorProbeEnd { get; set; } = 0x44;
    public bool LogHpEventProbe { get; set; } = false;
    public int HpEventProbeMaxLogs { get; set; } = 32;
    public int HpEventProbeDiffMax { get; set; } = 64;
    public bool HpEventProbeDumpRaw { get; set; } = false;
    public bool LogActionBoundaryProbe { get; set; } = false;
    public int ActionBoundaryProbeMaxLogs { get; set; } = 96;
    public int ActionBoundaryProbeDiffMax { get; set; } = 32;
    public bool LogPendingActionCandidatesOnEvent { get; set; } = false;
    public bool LogAllPendingActionCandidates { get; set; } = false;
    public int PendingActionCandidateMaxUnits { get; set; } = 16;
    public bool LogImmediateActionCandidatesOnEvent { get; set; } = false;
    public int ImmediateActionCandidateMaxUnits { get; set; } = 16;
    public bool LogActionStateChanges { get; set; } = false;
    public bool TrackPendingActions { get; set; } = false;
    public int PendingActionResolveWindowMs { get; set; } = 5000;
    public int PendingActionMaxBatchEvents { get; set; } = 16;
    public int PendingActionStaleMs { get; set; } = 30000;

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
        DclDerivedVariables ??= new List<FormulaDerivedVariable>();
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
        LandmarkProbes ??= new List<LandmarkProbe>();
        foreach (var probe in LandmarkProbes)
            probe.Normalize();
        PreClampDamageRewriteExpectedBytes = PreClampDamageRewriteExpectedBytes?.Trim() ?? "";
        ResultSelectorProbeExpectedBytes = ResultSelectorProbeExpectedBytes?.Trim() ?? "";
        ResultSelectorProbeActorRegister = string.IsNullOrWhiteSpace(ResultSelectorProbeActorRegister)
            ? "r8"
            : ResultSelectorProbeActorRegister.Trim().ToLowerInvariant();
        DeathStateWrites ??= new List<DeathStateWrite>();
    }

    public string Describe()
        => $"RewriteObservedDamage={RewriteObservedDamage}, RewriteObservedHealing={RewriteObservedHealing}, RewriteObservedMpLoss={RewriteObservedMpLoss}, RewriteObservedMpGain={RewriteObservedMpGain}, DryRunRewrites={DryRunRewrites}, ProofFinalDamage={ProofFinalDamage}, ProofFinalHealing={ProofFinalHealing}, ProofFinalMpLoss={ProofFinalMpLoss}, ProofFinalMpGain={ProofFinalMpGain}, FlatDamageReduction={FlatDamageReduction}, RewriteConditionFormula={(string.IsNullOrWhiteSpace(RewriteConditionFormula) ? "off" : "on")}, FinalDamageFormula={(string.IsNullOrWhiteSpace(FinalDamageFormula) ? "off" : "on")}, DclPipelineEnabled={DclPipelineEnabled}, DclDamageFormula={(string.IsNullOrWhiteSpace(DclDamageFormula) ? "off" : "on")}, DclActionContextMaxAgeMs={DclActionContextMaxAgeMs}, DclDecisionMaxLogs={DclDecisionMaxLogs}, DclDerivedVariables={DclDerivedVariables.Count}, MpRewriteConditionFormula={(string.IsNullOrWhiteSpace(MpRewriteConditionFormula) ? "off" : "on")}, FinalMpChangeFormula={(string.IsNullOrWhiteSpace(FinalMpChangeFormula) ? "off" : "on")}, FormulaVariables={FormulaVariables.Count}, FormulaPreActionVariables={FormulaPreActionVariables.Count}, FormulaPreResponseVariables={FormulaPreResponseVariables.Count}, FormulaDerivedVariables={FormulaDerivedVariables.Count}, FormulaTraceVariables={FormulaTraceVariables.Count}, FormulaTables={FormulaTables.Count}, FormulaMatrices={FormulaMatrices.Count}, FormulaMaps={FormulaMaps.Count}, ItemCatalogPath={ItemCatalogPath}, AbilityCatalogPath={AbilityCatalogPath}, ActionSignalRules={ActionSignalRules.Count}, DamageRules={DamageRules.Count}, MpRules={MpRules.Count}, ApplyEquipmentDr={ApplyEquipmentDr}, EquipmentSlots={EquipmentSlots.Count}, AttackerEquipmentSlots={AttackerEquipmentSlots.Count}, EquipmentDrRules={EquipmentDrRules.Count}, ApplyDamageResponseRules={ApplyDamageResponseRules}, DamageResponseRules={DamageResponseRules.Count}, DamageResponseClamp={MinDamageResponsePermille}-{MaxDamageResponsePermille}, AffectAllies={AffectAllies}, AffectFoes={AffectFoes}, InferAttackerFromRecentUnits={InferAttackerFromRecentUnits}, RecentAttackerWindowMs={RecentAttackerWindowMs}, ResolveAttackerByCt={ResolveAttackerByCt}, CtDropWindowMs={CtDropWindowMs}, ResolveAttackerByLowCtFallback={ResolveAttackerByLowCtFallback}, CtLowFallbackMaxCt={CtLowFallbackMaxCt}, CtLowFallbackWindowMs={CtLowFallbackWindowMs}, LogCtResolutionDiagnostics={LogCtResolutionDiagnostics}, ResolveCounterFromRecentDamage={ResolveCounterFromRecentDamage}, CounterEventWindowMs={CounterEventWindowMs}, LogAttackerCandidates={LogAttackerCandidates}, LogResolvedRuntimeContext={LogResolvedRuntimeContext}, UnitPollIntervalMs={UnitPollIntervalMs}, MaxTrackedBattleUnits={MaxTrackedBattleUnits}, SuppressOwnRewriteEchoWindowMs={SuppressOwnRewriteEchoWindowMs}, MemoryTableProbes={MemoryTableProbes.Count}/{MemoryTableProbes.Count(probe => probe.Enabled)} enabled, LogUnknownFieldDiffs={LogUnknownFieldDiffs}, UnknownDiff=0x{UnknownDiffStart:X2}-0x{UnknownDiffEnd:X2}, CaptureStructOnDeath={CaptureStructOnDeath}, CauseDeathOnZeroHp={CauseDeathOnZeroHp}, DeathStateWrites={DeathStateWrites.Count}, MinHpFloor={MinHpFloor}, HookRegisterProbe={HookRegisterProbe}/{HookRegisterProbeMaxLogs}, HookRegisterProbeOnHpEvent={HookRegisterProbeOnHpEvent}, HookRegisterProbeOnMpEvent={HookRegisterProbeOnMpEvent}, HookRegisterProbeOnCtDrop={HookRegisterProbeOnCtDrop}, HookRegisterProbeOnActionBoundary={HookRegisterProbeOnActionBoundary}, HookRegisterProbeOnPendingResolve={HookRegisterProbeOnPendingResolve}, HookRegisterProbeOnTargetCache={HookRegisterProbeOnTargetCache}, HookRegisterProbeEventMaxLogs={HookRegisterProbeEventMaxLogs}, HookRegisterProbeStackSlots={HookRegisterProbeStackSlots}, HookRegisterProbePointerScanBytes={HookRegisterProbePointerScanBytes}, HookRegisterProbePointerMaxLogs={HookRegisterProbePointerMaxLogs}, HookRegisterProbePointerMaxPointersPerRoot={HookRegisterProbePointerMaxPointersPerRoot}, LandmarkProbeEnabled={LandmarkProbeEnabled}, LandmarkProbeMaxLogs={LandmarkProbeMaxLogs}, LandmarkProbeStackSlots={LandmarkProbeStackSlots}, LandmarkProbes={LandmarkProbes.Count}/{LandmarkProbes.Count(probe => probe.Enabled)} enabled, ResultSelectorProbeEnabled={ResultSelectorProbeEnabled}, ResultSelectorProbeRva=0x{ResultSelectorProbeRva:X}, ResultSelectorProbeActorRegister={ResultSelectorProbeActorRegister}, ResultSelectorProbeRecordUnitOffset=0x{ResultSelectorProbeRecordUnitOffset:X}, ResultSelectorProbeMaxLogs={ResultSelectorProbeMaxLogs}, ResultSelectorProbeRecordDumpBytes={ResultSelectorProbeRecordDumpBytes}, PreClampDamageRewriteEnabled={PreClampDamageRewriteEnabled}, PreClampDamageRewriteRva=0x{PreClampDamageRewriteRva:X}, PreClampDamageRewriteTargetCharId={PreClampDamageRewriteTargetCharId}, PreClampDamageRewriteExpectedDebit={PreClampDamageRewriteExpectedDebit}, PreClampDamageRewriteForcedDebit={PreClampDamageRewriteForcedDebit}, PreClampDamageRewriteMaxWrites={PreClampDamageRewriteMaxWrites}, PreClampManagedCallbackEnabled={PreClampManagedCallbackEnabled}, PreClampManagedCallbackForcedDebit={PreClampManagedCallbackForcedDebit}, PreClampManagedCallbackActorFormulaEnabled={PreClampManagedCallbackActorFormulaEnabled}, PreClampManagedCallbackPaMultiplier={PreClampManagedCallbackPaMultiplier}, PreClampManagedCallbackStackScanBytes={PreClampManagedCallbackStackScanBytes}, PreClampPointerScanBytes={PreClampPointerScanBytes}, PreClampPointerMaxLogs={PreClampPointerMaxLogs}, PreClampPointerMaxPointersPerRoot={PreClampPointerMaxPointersPerRoot}, PreClampActorStructDumpEnabled={PreClampActorStructDumpEnabled}, PreClampActorStructDumpBytes={PreClampActorStructDumpBytes}, PreClampActorStructUnitOffset=0x{PreClampActorStructUnitOffset:X}, PreClampActorStructDumpMaxLogs={PreClampActorStructDumpMaxLogs}, PreClampResolveActorContext={PreClampResolveActorContext}, PreClampActorActionIdOffset=0x{PreClampActorActionIdOffset:X}, PreClampActorContextMaxLogs={PreClampActorContextMaxLogs}, PreClampLogEquipment={PreClampLogEquipment}, PreClampEquipBlockOffset=0x{PreClampEquipBlockOffset:X}, PreClampEquipMaxLogs={PreClampEquipMaxLogs}, LogPreClampFormulaCandidates={LogPreClampFormulaCandidates}/{PreClampFormulaCandidateMaxLogs}, PreClampFormulaCandidateRequirePendingMatch={PreClampFormulaCandidateRequirePendingMatch}, PreClampFormulaCandidateAllowImmediateAction={PreClampFormulaCandidateAllowImmediateAction}, PreClampImmediateActionMinScore={PreClampImmediateActionMinScore}, PreClampImmediateActionMinMargin={PreClampImmediateActionMinMargin}, PreClampImmediateActionMaxAgeMs={PreClampImmediateActionMaxAgeMs}, PreClampImmediateActionRequireFreshActive={PreClampImmediateActionRequireFreshActive}, PreClampImmediateActionAllowZeroActionId={PreClampImmediateActionAllowZeroActionId}, PreClampImmediateActionPlanMaxWrites={PreClampImmediateActionPlanMaxWrites}, PreClampImmediateActionPlanRequireExpectedHp={PreClampImmediateActionPlanRequireExpectedHp}, PreClampImmediateActionPlanEagerTargets={PreClampImmediateActionPlanEagerTargets}, PreClampImmediateActionNearbyUnitScanRadius={PreClampImmediateActionNearbyUnitScanRadius}, PreClampFormulaPlanEnabled={PreClampFormulaPlanEnabled}, PreClampFormulaPlanSlots={PreClampFormulaPlanSlots}, PreClampFormulaPlanWindowMs={PreClampFormulaPlanWindowMs}, PreClampFormulaPlanMaxWrites={PreClampFormulaPlanMaxWrites}, PreClampFormulaPlanRequirePhaseZero={PreClampFormulaPlanRequirePhaseZero}, ActorProbeOnEvent={ActorProbeOnEvent}, ActorProbe=0x{ActorProbeStart:X2}-0x{ActorProbeEnd:X2}, LogHpEventProbe={LogHpEventProbe}/{HpEventProbeMaxLogs}, HpEventProbeDiffMax={HpEventProbeDiffMax}, HpEventProbeDumpRaw={HpEventProbeDumpRaw}, LogActionBoundaryProbe={LogActionBoundaryProbe}/{ActionBoundaryProbeMaxLogs}, ActionBoundaryProbeDiffMax={ActionBoundaryProbeDiffMax}, LogPendingActionCandidatesOnEvent={LogPendingActionCandidatesOnEvent}, LogAllPendingActionCandidates={LogAllPendingActionCandidates}, PendingActionCandidateMaxUnits={PendingActionCandidateMaxUnits}, LogImmediateActionCandidatesOnEvent={LogImmediateActionCandidatesOnEvent}, ImmediateActionCandidateMaxUnits={ImmediateActionCandidateMaxUnits}, LogActionStateChanges={LogActionStateChanges}, TrackPendingActions={TrackPendingActions}, PendingActionResolveWindowMs={PendingActionResolveWindowMs}, PendingActionMaxBatchEvents={PendingActionMaxBatchEvents}, PendingActionStaleMs={PendingActionStaleMs}";
}
