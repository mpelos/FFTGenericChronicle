namespace fftivc.generic.chronicle.codemod;

internal sealed record SettingsValidationFinding(string Severity, string Scope, string Message)
{
    public override string ToString() => $"[{Severity}] {Scope}: {Message}";
}

internal sealed class SettingsValidationReport
{
    private readonly List<SettingsValidationFinding> _findings = new();

    public IReadOnlyList<SettingsValidationFinding> Findings => _findings;

    public int ErrorCount => _findings.Count(finding => finding.Severity.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

    public int WarningCount => _findings.Count(finding => finding.Severity.Equals("WARN", StringComparison.OrdinalIgnoreCase));

    public bool Success => ErrorCount == 0;

    public void Error(string scope, string message) => _findings.Add(new SettingsValidationFinding("ERROR", scope, message));

    public void Warn(string scope, string message) => _findings.Add(new SettingsValidationFinding("WARN", scope, message));

    public void Info(string scope, string message) => _findings.Add(new SettingsValidationFinding("INFO", scope, message));
}

internal static class RuntimeSettingsValidator
{
    private const int RawSize = 0x200;

    public static SettingsValidationReport Validate(RuntimeSettings settings, ItemCatalog catalog)
    {
        var report = new SettingsValidationReport();
        report.Info("settings", settings.Describe());
        report.Info("catalog", catalog.Describe());

        ValidateScalarRanges(settings, report);
        ValidateTables(settings, report);
        ValidateMatrices(settings, report);
        ValidateMaps(settings, report);
        ValidateEquipmentSlots("EquipmentSlots", settings.EquipmentSlots, report);
        ValidateEquipmentSlots("AttackerEquipmentSlots", settings.AttackerEquipmentSlots, report);
        ValidateEquipmentRules(settings, report);
        ValidateMemoryTableProbes(settings, report);
        ValidateDeathStateWrites(settings, report);
        ValidateFormulas(settings, catalog, report);
        ValidateIntegratedEvaluation(settings, catalog, report);

        return report;
    }

    private static void ValidateScalarRanges(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.MinDamageResponsePermille < 0)
            report.Error("DamageResponseClamp", "MinDamageResponsePermille must be nonnegative.");
        if (settings.MaxDamageResponsePermille < settings.MinDamageResponsePermille)
            report.Error("DamageResponseClamp", "MaxDamageResponsePermille is below MinDamageResponsePermille.");
        if (settings.DamageResponseChipFloor < 0)
            report.Error("DamageResponseChipFloor", "DamageResponseChipFloor must be nonnegative.");
        if (settings.ProofFinalMpLoss < 0)
            report.Error("ProofFinalMpLoss", "ProofFinalMpLoss must be nonnegative.");
        if (settings.ProofFinalMpGain < 0)
            report.Error("ProofFinalMpGain", "ProofFinalMpGain must be nonnegative.");
        if (settings.RecentAttackerWindowMs < 0)
            report.Error("RecentAttackerWindowMs", "RecentAttackerWindowMs must be nonnegative.");
        if (settings.CtDropWindowMs <= 0)
            report.Error("CtDropWindowMs", "CtDropWindowMs must be greater than zero.");
        if (settings.CounterEventWindowMs <= 0)
            report.Error("CounterEventWindowMs", "CounterEventWindowMs must be greater than zero.");
        if (settings.UnitPollIntervalMs <= 0)
            report.Error("UnitPollIntervalMs", "UnitPollIntervalMs must be greater than zero.");
        else if (settings.UnitPollIntervalMs > 100)
            report.Warn("UnitPollIntervalMs", "poll intervals above 100 ms may miss or delay short-lived battle state changes.");
        if (settings.MaxTrackedBattleUnits <= 0)
            report.Error("MaxTrackedBattleUnits", "MaxTrackedBattleUnits must be greater than zero.");
        else if (settings.MaxTrackedBattleUnits < 16)
            report.Warn("MaxTrackedBattleUnits", "values below 16 may skip units in larger battles.");
        if (settings.SuppressOwnRewriteEchoWindowMs < 0)
            report.Error("SuppressOwnRewriteEchoWindowMs", "SuppressOwnRewriteEchoWindowMs must be nonnegative.");
        if (settings.HookRegisterProbeMaxLogs < 0)
            report.Error("HookRegisterProbeMaxLogs", "HookRegisterProbeMaxLogs must be nonnegative.");
        if (settings.HookRegisterProbeEventMaxLogs < 0)
            report.Error("HookRegisterProbeEventMaxLogs", "HookRegisterProbeEventMaxLogs must be nonnegative.");
        if (settings.HookRegisterProbeStackSlots < 0 || settings.HookRegisterProbeStackSlots > 64)
            report.Error("HookRegisterProbeStackSlots", "HookRegisterProbeStackSlots must be within 0..64.");
        if (settings.HookRegisterProbePointerScanBytes < 0 || settings.HookRegisterProbePointerScanBytes > 0x2000)
            report.Error("HookRegisterProbePointerScanBytes", "HookRegisterProbePointerScanBytes must be within 0..0x2000.");
        if (settings.HookRegisterProbePointerMaxLogs < 0)
            report.Error("HookRegisterProbePointerMaxLogs", "HookRegisterProbePointerMaxLogs must be nonnegative.");
        if (settings.HookRegisterProbePointerMaxPointersPerRoot < 0 || settings.HookRegisterProbePointerMaxPointersPerRoot > 64)
            report.Error("HookRegisterProbePointerMaxPointersPerRoot", "HookRegisterProbePointerMaxPointersPerRoot must be within 0..64.");
        if (settings.HookRegisterProbe)
            report.Warn("HookRegisterProbe", "hook register probe is for short RE captures only; keep HookRegisterProbeMaxLogs low.");
        if (settings.HookRegisterProbeOnHpEvent ||
            settings.HookRegisterProbeOnMpEvent ||
            settings.HookRegisterProbeOnCtDrop ||
            settings.HookRegisterProbeOnActionBoundary ||
            settings.HookRegisterProbeOnPendingResolve)
            report.Warn("HookRegisterProbeOnEvent", "event-correlated hook register snapshots are for short RE captures only.");
        if (settings.HookRegisterProbePointerScanBytes > 0)
            report.Warn("HookRegisterProbePointerScanBytes", "pointer scans are read-only but noisy; use only for short controlled RE captures.");
        if (settings.LandmarkProbeMaxLogs < 0)
            report.Error("LandmarkProbeMaxLogs", "LandmarkProbeMaxLogs must be nonnegative.");
        if (settings.LandmarkProbeStackSlots < 0 || settings.LandmarkProbeStackSlots > 64)
            report.Error("LandmarkProbeStackSlots", "LandmarkProbeStackSlots must be within 0..64.");
        if (settings.LandmarkProbeEnabled)
        {
            report.Warn("LandmarkProbeEnabled", "landmark hooks are targeted RE probes; use only for short controlled captures.");
            if (settings.LandmarkProbes.Count(probe => probe.Enabled) == 0)
                report.Error("LandmarkProbes", "LandmarkProbeEnabled requires at least one enabled LandmarkProbe.");
        }
        foreach (var probe in settings.LandmarkProbes)
        {
            probe.Normalize();
            if (!probe.TryValidate(out string error))
                report.Error($"LandmarkProbes.{probe.TraceName}", error);
        }
        ValidateResultSelectorProbe(settings, report);
        if (settings.PreviewHitPctControlEnabled)
        {
            if (settings.PreviewHitPctRva <= 0)
                report.Error("PreviewHitPctRva", "PreviewHitPctRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.PreviewHitPctExpectedBytes))
                report.Error("PreviewHitPctExpectedBytes", "Expected bytes are required for the preview hit% hook.");
            if (settings.PreviewHitPctForcedValue is < -1 or > 0xFFFF)
                report.Error("PreviewHitPctForcedValue", "Forced value must be -1 (observe) or 0..65535.");
            if (!settings.PreviewHitPctLogOnly && settings.PreviewHitPctForcedValue >= 0)
                report.Warn("PreviewHitPctControlEnabled", "preview hit% control overwrites the displayed forecast %; purely visual, does not change the actual roll.");
        }
        if (settings.PreviewDamageControlEnabled)
        {
            if (settings.PreviewDamageForcedValue is < -1 or > 0xFFFF)
                report.Error("PreviewDamageForcedValue", "Forced value must be -1 (observe) or 0..65535.");
            if (!settings.PreviewDamageLogOnly && settings.PreviewDamageForcedValue >= 0)
                report.Warn("PreviewDamageControlEnabled", "preview damage control overwrites the displayed forecast number; purely visual, does not change the real damage.");
        }
        if (settings.PreviewForecastSourceControlEnabled)
        {
            if (settings.PreviewForecastSourceForcedValue is < -1 or > 0xFFFF)
                report.Error("PreviewForecastSourceForcedValue", "Forced value must be -1 (observe) or 0..65535.");
            if (!settings.PreviewForecastSourceLogOnly && settings.PreviewForecastSourceForcedValue >= 0)
                report.Warn("PreviewForecastSourceControlEnabled", "forecast source control rewrites the staged-damage field (+0x1C4) at the preview finalizer; number + HP-bar both follow it, but it does not change the actual applied damage (that is the pre-clamp lever).");
        }
        if (settings.CalcEntryProbeEnabled && settings.CalcEntryProbeRva <= 0)
            report.Error("CalcEntryProbeRva", "CalcEntryProbeRva must be positive.");
        if (settings.RollRngProbeEnabled && settings.RollRngProbeRva <= 0)
            report.Error("RollRngProbeRva", "RollRngProbeRva must be positive.");
        if (settings.StagedBundleProbeEnabled)
        {
            if (settings.StagedBundleProbeRva <= 0)
                report.Error("StagedBundleProbeRva", "StagedBundleProbeRva must be positive.");
            if (settings.StagedBundleForceKind is < -1 or > 0xFF)
                report.Error("StagedBundleForceKind", "Force kind must be -1 or a byte 0..255.");
            if (settings.StagedBundleForceApplyMask is < -1 or > 0xFF)
                report.Error("StagedBundleForceApplyMask", "Force apply mask must be -1 or a byte 0..255.");
            if (settings.StagedBundleForceResFlag is < -1 or > 0xFF)
                report.Error("StagedBundleForceResFlag", "Force result flag must be -1 or a byte 0..255.");
            if (settings.StagedBundleForceAilment is < -1 or > 0xFFFF)
                report.Error("StagedBundleForceAilment", "Force ailment must be -1 or a word 0..65535.");
            if (settings.StagedBundleForceDmg is < -1 or > 0xFFFF)
                report.Error("StagedBundleForceDmg", "Force dmg must be -1 or a word 0..65535.");
            if (settings.StagedBundleForceTargetCharId >= 0 &&
                (settings.StagedBundleForceKind >= 0 || settings.StagedBundleForceAilment >= 0 ||
                 settings.StagedBundleForceApplyMask >= 0 || settings.StagedBundleForceDmg >= 0 ||
                 settings.StagedBundleForceResFlag >= 0))
                report.Warn("StagedBundleProbeEnabled", "staged-bundle forcing overwrites the computed effect result before apply; use for controlled LT4 proof captures only.");
        }
        if (settings.MagicAccuracyControlEnabled)
        {
            if (settings.MagicAccuracyRva <= 0)
                report.Error("MagicAccuracyRva", "MagicAccuracyRva must be positive.");
            if (settings.MagicAccuracyForcedChance is < -1 or > 100)
                report.Error("MagicAccuracyForcedChance", "Forced chance must be -1 (observe) or 0..100.");
            if (settings.MagicAccuracyForcedChance >= 0)
                report.Warn("MagicAccuracyControlEnabled", "magic accuracy control forces the Faith roll chance; 100 makes offensive magic always connect, 0 always fizzle.");
        }
        if (settings.StatusChanceControlEnabled)
        {
            if (settings.StatusChanceRva <= 0)
                report.Error("StatusChanceRva", "StatusChanceRva must be positive.");
            if (settings.StatusChanceForcedChance is < -1 or > 100)
                report.Error("StatusChanceForcedChance", "Forced chance must be -1 (observe) or 0..100.");
            if (settings.StatusChanceForcedChance >= 0)
                report.Warn("StatusChanceControlEnabled", "status chance control forces the infliction roll; 100 always procs, 0 never procs.");
        }
        if (settings.ReactionChanceControlEnabled)
        {
            if (settings.ReactionChanceForcedChance is < -1 or > 100)
                report.Error("ReactionChanceForcedChance", "Forced chance must be -1 (observe) or 0..100.");
            if (settings.ReactionChanceForcedChance >= 0)
                report.Warn("ReactionChanceControlEnabled", "reaction chance control overrides the Brave-gate on all 4 real-code roll sites; 0 suppresses every reaction (Blade Grasp/Hamedo/Counter...), 100 forces them.");
        }
        if (settings.ItemTableEvadeZeroEnabled)
            report.Warn("ItemTableEvadeZeroEnabled", "zeroes weapon W-Ev + shield + accessory evade in the loaded item stat tables every poll (all items, both teams; sanity-gated on Venetian Shield bytes). Class evade (+0x4B) is job-derived and NOT covered — pair with CalcEntryEvadeStamp/EvadeOverride for full force-hit.");
        if (settings.EvadeRecordOverrideEnabled)
        {
            foreach (var (name, v) in new[]
                     {
                         ("44", settings.EvadeRecordOverride44),
                         ("46", settings.EvadeRecordOverride46),
                         ("50", settings.EvadeRecordOverride50),
                     })
                if (v is < -1 or > 0xFF)
                    report.Error($"EvadeRecordOverride{name}", "Packed evade value must be -1 (leave) or 0..255.");
            if (settings.EvadeRecordOverride44 < 0 && settings.EvadeRecordOverride46 < 0 && settings.EvadeRecordOverride50 < 0)
                report.Warn("EvadeRecordOverrideEnabled", "enabled but all packed values are -1; no hook will be installed.");
            else
                report.Warn("EvadeRecordOverrideEnabled", "forces the PACKED evade fields (class/shield/accessory) in the combat-input record for ALL units, both teams — the values the preview and roll actually consume.");
        }
        if (settings.CalcEntryEvadeStampEnabled)
        {
            if (settings.CalcEntryProbeRva <= 0)
                report.Error("CalcEntryProbeRva", "CalcEntryEvadeStamp uses CalcEntryProbeRva; it must be positive.");
            report.Warn("CalcEntryEvadeStampEnabled", "calc-entry evade stamp writes the EvadeCopierOverride* byte profile onto the TARGET unit at computeActionResult, immediately before the VM avoidance roll (per-attack, zero-width race window).");
        }
        if (settings.EvadeCopierOverrideEnabled || settings.CalcEntryEvadeStampEnabled)
        {
            if (settings.EvadeCopierOverrideTargetCharId is < -1 or > 0xFF)
                report.Error("EvadeCopierOverrideTargetCharId", "Target charId must be -1 (all) or a byte 0..255.");
            foreach (var (name, v) in new[]
                     {
                         ("46", settings.EvadeCopierOverride46), ("47", settings.EvadeCopierOverride47),
                         ("48", settings.EvadeCopierOverride48), ("49", settings.EvadeCopierOverride49),
                         ("4A", settings.EvadeCopierOverride4A), ("4B", settings.EvadeCopierOverride4B),
                         ("4C", settings.EvadeCopierOverride4C), ("4D", settings.EvadeCopierOverride4D),
                         ("4E", settings.EvadeCopierOverride4E),
                     })
                if (v is < -1 or > 0xFF)
                    report.Error($"EvadeCopierOverride{name}", "Evade byte must be -1 (leave) or 0..255.");
            report.Warn("EvadeCopierOverrideEnabled", "airtight evade override detours the 3 equip/refresh copier tails (0x59F93C/0x285553/0x396757) and over-stamps the defender's evade bytes every refresh; all=0 forces HIT, one source high forces that avoid type. Retires the EvadeOverride poll.");
        }
        if (settings.PreviewForecastPokeEnabled)
        {
            if (settings.PreviewForecastPokeValue is < -1 or > 0x7FFF)
                report.Error("PreviewForecastPokeValue", "Poke value must be -1 (off) or 0..32767.");
            if (settings.PreviewForecastUnitStride <= 0)
                report.Error("PreviewForecastUnitStride", "Unit stride must be positive.");
            if (settings.PreviewForecastGlobalRva <= 0)
                report.Error("PreviewForecastGlobalRva", "Forecast global RVA must be positive.");
            if (settings.PreviewForecastPokeValue >= 0)
                report.Warn("PreviewForecastPokeEnabled", "forecast poke poll-writes the configured HP amount field (offset 0x6 = unit+0x1C4 damage/debit, 0x8 = unit+0x1C6 healing/credit); drives the preview NUMBER + HP-bar together, but not the real result (that is the pre-clamp lever).");
        }
        if (settings.PreClampDamageRewriteEnabled)
        {
            report.Warn("PreClampDamageRewriteEnabled", "pre-clamp damage rewrite mutates staged engine damage; use only for one-shot controlled proof captures.");
            if (settings.PreClampDamageRewriteRva <= 0)
                report.Error("PreClampDamageRewriteRva", "PreClampDamageRewriteRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.PreClampDamageRewriteExpectedBytes))
                report.Error("PreClampDamageRewriteExpectedBytes", "Expected bytes are required for the pre-clamp hook.");
            if (settings.PreClampDamageRewriteTargetCharId is < -1 or > 0xFF)
                report.Error("PreClampDamageRewriteTargetCharId", "Target char id must be -1 or 0..255.");
            if (settings.PreClampDamageRewriteTargetTeam is < -1 or > 0xFF)
                report.Error("PreClampDamageRewriteTargetTeam", "Target team must be -1 or 0..255.");
            if (settings.PreClampDamageRewriteExpectedDebit is < -1 or > short.MaxValue)
                report.Error("PreClampDamageRewriteExpectedDebit", "Expected debit must be -1 or a signed 16-bit nonnegative value.");
            if (settings.PreClampDamageRewriteExpectedCredit is < -1 or > short.MaxValue)
                report.Error("PreClampDamageRewriteExpectedCredit", "Expected credit must be -1 or a signed 16-bit nonnegative value.");
            if (settings.PreClampDamageRewriteMinHp < 0 || settings.PreClampDamageRewriteMaxHp < settings.PreClampDamageRewriteMinHp)
                report.Error("PreClampDamageRewriteHpRange", "Pre-clamp HP range must be nonnegative and ordered.");
            if (settings.PreClampDamageRewriteForcedDebit is < -1 or > short.MaxValue)
                report.Error("PreClampDamageRewriteForcedDebit", "Forced debit must be -1 or a signed 16-bit nonnegative value.");
            if (settings.PreClampDamageRewriteForcedCredit is < -1 or > short.MaxValue)
                report.Error("PreClampDamageRewriteForcedCredit", "Forced credit must be -1 or a signed 16-bit nonnegative value.");
            if (settings.PreClampManagedCallbackForcedDebit is < -1 or > short.MaxValue)
                report.Error("PreClampManagedCallbackForcedDebit", "Managed callback forced debit must be -1 or a signed 16-bit nonnegative value.");
            if (settings.PreClampManagedCallbackPaMultiplier is < 0 or > 1000)
                report.Error("PreClampManagedCallbackPaMultiplier", "Managed callback PA multiplier must be within 0..1000.");
            if (settings.PreClampManagedCallbackFormulaMinDamage < 0 ||
                settings.PreClampManagedCallbackFormulaMaxDamage < settings.PreClampManagedCallbackFormulaMinDamage ||
                settings.PreClampManagedCallbackFormulaMaxDamage > short.MaxValue)
                report.Error("PreClampManagedCallbackFormulaDamageRange", "Managed callback formula damage range must be nonnegative, ordered, and <= Int16 max.");
            if (settings.PreClampManagedCallbackStackScanBytes < 0 || settings.PreClampManagedCallbackStackScanBytes > 0x4000)
                report.Error("PreClampManagedCallbackStackScanBytes", "Managed callback stack scan bytes must be within 0..0x4000.");
            if (!settings.PreClampDamageRewriteLogOnly &&
                settings.PreClampDamageRewriteForcedDebit < 0 &&
                settings.PreClampDamageRewriteForcedCredit < 0 &&
                !settings.PreClampFormulaPlanEnabled &&
                !settings.PreClampManagedCallbackEnabled)
                report.Error("PreClampDamageRewrite", "Non-log-only mode requires a forced debit, forced credit, PreClampFormulaPlanEnabled, or PreClampManagedCallbackEnabled.");
            if (settings.PreClampManagedCallbackEnabled)
            {
                report.Warn("PreClampManagedCallbackEnabled", "managed callback calls C# from the native pre-clamp hook; use only for a tightly guarded ABI proof until live-stable.");
                if (settings.PreClampManagedCallbackForcedDebit < 0 &&
                    !settings.PreClampManagedCallbackActorFormulaEnabled)
                    report.Error("PreClampManagedCallback", "Managed callback requires a forced debit or PreClampManagedCallbackActorFormulaEnabled.");
                if (settings.PreClampManagedCallbackActorFormulaEnabled)
                    report.Warn("PreClampManagedCallbackActorFormulaEnabled", "actor formula resolves caster/action from the pre-clamp frame; use only in controlled captures until it is live-stable across action families.");
            }
            if (settings.PreClampDamageRewriteMaxWrites <= 0 || settings.PreClampDamageRewriteMaxWrites > 32)
                report.Error("PreClampDamageRewriteMaxWrites", "Max writes must be within 1..32.");
            if (settings.PreClampPointerScanBytes < 0 || settings.PreClampPointerScanBytes > 0x4000)
                report.Error("PreClampPointerScanBytes", "PreClampPointerScanBytes must be within 0..0x4000.");
            if (settings.PreClampPointerMaxLogs < 0)
                report.Error("PreClampPointerMaxLogs", "PreClampPointerMaxLogs must be nonnegative.");
            if (settings.PreClampPointerMaxPointersPerRoot < 0 || settings.PreClampPointerMaxPointersPerRoot > 64)
                report.Error("PreClampPointerMaxPointersPerRoot", "PreClampPointerMaxPointersPerRoot must be within 0..64.");
            if (settings.PreClampPointerScanBytes > 0)
                report.Warn("PreClampPointerScanBytes", "pre-clamp pointer scans are read-only but noisy; use only for short controlled RE captures.");
            if (settings.PreClampActorStructDumpEnabled)
            {
                if (settings.PreClampActorStructDumpBytes <= 0 || settings.PreClampActorStructDumpBytes > 0x4000)
                    report.Error("PreClampActorStructDumpBytes", "PreClampActorStructDumpBytes must be within 1..0x4000.");
                if (settings.PreClampActorStructDumpBytes > settings.PreClampPointerScanBytes)
                    report.Error("PreClampActorStructDumpBytes", "PreClampActorStructDumpBytes cannot exceed PreClampPointerScanBytes (the scan buffer it reads from).");
                if (settings.PreClampActorStructUnitOffset < 0 || settings.PreClampActorStructUnitOffset > 0x4000)
                    report.Error("PreClampActorStructUnitOffset", "PreClampActorStructUnitOffset must be within 0..0x4000.");
                if (settings.PreClampActorStructDumpMaxLogs < 0)
                    report.Error("PreClampActorStructDumpMaxLogs", "PreClampActorStructDumpMaxLogs must be nonnegative.");
                report.Warn("PreClampActorStructDumpEnabled", "actor-struct dumps are read-only but verbose; use only for short controlled RE captures.");
            }
            if (settings.PreClampResolveActorContext)
            {
                if (settings.PreClampActorStructUnitOffset < 0 || settings.PreClampActorStructUnitOffset > 0x4000)
                    report.Error("PreClampActorStructUnitOffset", "PreClampActorStructUnitOffset must be within 0..0x4000.");
                if (settings.PreClampActorActionIdOffset < 0 || settings.PreClampActorActionIdOffset > 0x4000)
                    report.Error("PreClampActorActionIdOffset", "PreClampActorActionIdOffset must be within 0..0x4000.");
                if (settings.PreClampActorContextMaxLogs < 0)
                    report.Error("PreClampActorContextMaxLogs", "PreClampActorContextMaxLogs must be nonnegative.");
                report.Warn("PreClampResolveActorContext", "memory-only actor-context resolver is observe-only (logs [PRECLAMP-ACTOR-CTX]); use for controlled RE captures.");
            }
            if (settings.PreClampLogEquipment)
            {
                if (settings.PreClampEquipBlockOffset < 0 || settings.PreClampEquipBlockOffset > 0x4000)
                    report.Error("PreClampEquipBlockOffset", "PreClampEquipBlockOffset must be within 0..0x4000.");
                if (settings.PreClampEquipMaxLogs < 0)
                    report.Error("PreClampEquipMaxLogs", "PreClampEquipMaxLogs must be nonnegative.");
                if (!settings.PreClampResolveActorContext)
                    report.Warn("PreClampLogEquipment", "equipment readout uses the actor-context resolver for the caster side; enable PreClampResolveActorContext too (target side still logs).");
                report.Warn("PreClampLogEquipment", "live equipment readout is observe-only (logs [PRECLAMP-EQUIP]); use for controlled RE captures.");
            }
            if (settings.PreClampFormulaPlanEnabled)
            {
                if (settings.PreClampFormulaPlanSlots <= 0 || settings.PreClampFormulaPlanSlots > 32)
                    report.Error("PreClampFormulaPlanSlots", "Plan slots must be within 1..32.");
                if (settings.PreClampFormulaPlanWindowMs <= 0 || settings.PreClampFormulaPlanWindowMs > 60_000)
                    report.Error("PreClampFormulaPlanWindowMs", "Plan window must be within 1..60000 ms.");
                if (settings.PreClampFormulaPlanMaxWrites <= 0 || settings.PreClampFormulaPlanMaxWrites > 16)
                    report.Error("PreClampFormulaPlanMaxWrites", "Plan max writes must be within 1..16.");
                if (!settings.TrackPendingActions)
                    report.Warn("PreClampFormulaPlanEnabled", "TrackPendingActions is recommended so plan entries have action context.");
                if (!settings.LogPreClampFormulaCandidates)
                    report.Warn("PreClampFormulaPlanEnabled", "LogPreClampFormulaCandidates is recommended during live validation.");
            }
            if (settings.PreClampFormulaCandidateAllowImmediateAction)
            {
                if (settings.PreClampFormulaCandidateRequirePendingMatch)
                    report.Warn("PreClampFormulaCandidateAllowImmediateAction", "PreClampFormulaCandidateRequirePendingMatch=true prevents immediate-action fallback from being used.");
                if (settings.PreClampImmediateActionMinScore < 0)
                    report.Error("PreClampImmediateActionMinScore", "Immediate action minimum score must be nonnegative.");
                if (settings.PreClampImmediateActionMinMargin < 0)
                    report.Error("PreClampImmediateActionMinMargin", "Immediate action minimum margin must be nonnegative.");
                if (settings.PreClampImmediateActionMaxAgeMs <= 0 || settings.PreClampImmediateActionMaxAgeMs > 60_000)
                    report.Error("PreClampImmediateActionMaxAgeMs", "Immediate action max age must be within 1..60000 ms.");
                if (settings.PreClampImmediateActionAllowZeroActionId)
                    report.Warn("PreClampImmediateActionAllowZeroActionId", "Zero action id sources should be used only for controlled basic-attack validation.");
                if (settings.PreClampImmediateActionPlanMaxWrites <= 0 || settings.PreClampImmediateActionPlanMaxWrites > 16)
                    report.Error("PreClampImmediateActionPlanMaxWrites", "Immediate action plan max writes must be within 1..16.");
                if (!settings.PreClampImmediateActionPlanRequireExpectedHp && settings.PreClampImmediateActionPlanMaxWrites > 1)
                    report.Warn("PreClampImmediateActionPlanRequireExpectedHp", "HP-wildcard immediate plans can rewrite multiple hits from one queued formula; use only when target HP is not part of the intended formula.");
                if (settings.PreClampImmediateActionNearbyUnitScanRadius < 0 || settings.PreClampImmediateActionNearbyUnitScanRadius > 32)
                    report.Error("PreClampImmediateActionNearbyUnitScanRadius", "Immediate action nearby unit scan radius must be within 0..32.");
                if (settings.PreClampImmediateActionPlanEagerTargets)
                    report.Warn("PreClampImmediateActionPlanEagerTargets", "Eager immediate plans prequeue one positive-debit rewrite per nearby possible target; use only for controlled immediate/basic validation.");
                if (!settings.LogActionStateChanges)
                    report.Warn("PreClampFormulaCandidateAllowImmediateAction", "LogActionStateChanges is recommended so immediate action ages are meaningful.");
            }
        }
        if (settings.HpEventProbeMaxLogs < 0)
            report.Error("HpEventProbeMaxLogs", "HpEventProbeMaxLogs must be nonnegative.");
        if (settings.HpEventProbeDiffMax < 0 || settings.HpEventProbeDiffMax > 256)
            report.Error("HpEventProbeDiffMax", "HpEventProbeDiffMax must be within 0..256.");
        if (settings.LogHpEventProbe)
            report.Warn("LogHpEventProbe", "HP event raw diffs are noisy; use only for short controlled RE captures.");
        if (settings.HpEventProbeDumpRaw)
            report.Warn("HpEventProbeDumpRaw", "raw HP event dumps are verbose; keep HpEventProbeMaxLogs low.");
        if (settings.ActionBoundaryProbeMaxLogs < 0)
            report.Error("ActionBoundaryProbeMaxLogs", "ActionBoundaryProbeMaxLogs must be nonnegative.");
        if (settings.ActionBoundaryProbeDiffMax < 0 || settings.ActionBoundaryProbeDiffMax > 128)
            report.Error("ActionBoundaryProbeDiffMax", "ActionBoundaryProbeDiffMax must be within 0..128.");
        if (settings.LogActionBoundaryProbe)
            report.Warn("LogActionBoundaryProbe", "action boundary probes are noisy; use only for short controlled RE captures.");
        if (settings.PendingActionCandidateMaxUnits <= 0)
            report.Error("PendingActionCandidateMaxUnits", "PendingActionCandidateMaxUnits must be greater than zero.");
        else if (settings.PendingActionCandidateMaxUnits > settings.MaxTrackedBattleUnits)
            report.Warn("PendingActionCandidateMaxUnits", "candidate logging above MaxTrackedBattleUnits is usually unnecessary.");
        if (settings.ImmediateActionCandidateMaxUnits <= 0)
            report.Error("ImmediateActionCandidateMaxUnits", "ImmediateActionCandidateMaxUnits must be greater than zero.");
        else if (settings.ImmediateActionCandidateMaxUnits > settings.MaxTrackedBattleUnits)
            report.Warn("ImmediateActionCandidateMaxUnits", "candidate logging above MaxTrackedBattleUnits is usually unnecessary.");
        if (settings.LogImmediateActionCandidatesOnEvent && !settings.LogActionStateChanges)
            report.Warn("LogImmediateActionCandidatesOnEvent", "LogActionStateChanges is recommended so candidate stateAgeMs is meaningful.");
        if (settings.PendingActionResolveWindowMs <= 0 || settings.PendingActionResolveWindowMs > 60_000)
            report.Error("PendingActionResolveWindowMs", "PendingActionResolveWindowMs must be within 1..60000.");
        else if (settings.PendingActionResolveWindowMs > 5000)
            report.Warn("PendingActionResolveWindowMs", "wide pending-action resolve windows increase false-positive attribution risk.");
        if (settings.PendingActionMaxBatchEvents <= 0 || settings.PendingActionMaxBatchEvents > 64)
            report.Error("PendingActionMaxBatchEvents", "PendingActionMaxBatchEvents must be within 1..64.");
        if (settings.PendingActionStaleMs <= 0 || settings.PendingActionStaleMs > 300_000)
            report.Error("PendingActionStaleMs", "PendingActionStaleMs must be within 1..300000.");
        if (settings.TrackPendingActions && !settings.LogActionStateChanges)
            report.Warn("TrackPendingActions", "LogActionStateChanges is recommended while validating pending-action tracker decisions.");
        if (settings.UnknownDiffStart < 0 || settings.UnknownDiffStart >= RawSize)
            report.Error("UnknownDiffStart", $"UnknownDiffStart must be within 0..0x{RawSize - 1:X}.");
        if (settings.UnknownDiffEnd < settings.UnknownDiffStart || settings.UnknownDiffEnd >= RawSize)
            report.Error("UnknownDiffEnd", $"UnknownDiffEnd must be within UnknownDiffStart..0x{RawSize - 1:X}.");
    }

    // OBSERVE-ONLY result/animation selector probe: an ExecuteFirst hook that captures the evade-type
    // byte (cl) and a window of the actor's result record. It never writes engine memory, so the checks
    // here only bound the capture parameters and emit a WARN that this is an RE probe.
    private static void ValidateResultSelectorProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.ResultSelectorProbeMaxLogs < 0)
            report.Error("ResultSelectorProbeMaxLogs", "ResultSelectorProbeMaxLogs must be nonnegative.");
        if (settings.ResultSelectorProbeRecordDumpBytes < 0 || settings.ResultSelectorProbeRecordDumpBytes > 256)
            report.Error("ResultSelectorProbeRecordDumpBytes", "ResultSelectorProbeRecordDumpBytes must be within 0..256.");
        if (settings.ResultSelectorProbeRecordUnitOffset < 0 || settings.ResultSelectorProbeRecordUnitOffset > 0x4000)
            report.Error("ResultSelectorProbeRecordUnitOffset", "ResultSelectorProbeRecordUnitOffset must be within 0..0x4000.");
        if (settings.ResultSelectorProbeEnabled)
        {
            if (settings.ResultSelectorProbeRva <= 0)
                report.Error("ResultSelectorProbeRva", "ResultSelectorProbeRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.ResultSelectorProbeExpectedBytes))
                report.Error("ResultSelectorProbeExpectedBytes", "Expected bytes are required for the result-selector probe.");
            report.Warn("ResultSelectorProbeEnabled", "result-selector probe is an observe-only RE hook (logs [SELECTOR-PROBE] evade-type/record); use only for short controlled captures.");
        }

        ValidateResultSelectorControl(settings, report);
    }

    private static void ValidateResultSelectorControl(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.ResultSelectorControlMaxWrites < 1 || settings.ResultSelectorControlMaxWrites > 32)
            report.Error("ResultSelectorControlMaxWrites", "ResultSelectorControlMaxWrites must be within 1..32.");

        foreach (var (name, value) in new[]
        {
            ("ResultSelectorControlTargetCharId", settings.ResultSelectorControlTargetCharId),
            ("ResultSelectorControlMatchEvadeType", settings.ResultSelectorControlMatchEvadeType),
            ("ResultSelectorControlForceEvadeType", settings.ResultSelectorControlForceEvadeType),
            ("ResultSelectorControlForceResultCode", settings.ResultSelectorControlForceResultCode),
        })
        {
            if (value < -1 || value > 255)
                report.Error(name, $"{name} must be -1 (any / no-change) or 0..255.");
        }

        if (!settings.ResultSelectorControlEnabled)
            return;

        if (!settings.ResultSelectorProbeEnabled)
            report.Error("ResultSelectorControlEnabled",
                "ResultSelectorControlEnabled requires ResultSelectorProbeEnabled (the control rides the selector hook).");

        if (!settings.ResultSelectorControlLogOnly &&
            settings.ResultSelectorControlForceEvadeType < 0 &&
            settings.ResultSelectorControlForceResultCode < 0)
            report.Error("ResultSelectorControlEnabled",
                "Control is live (LogOnly=false) but nothing to force (ForceEvadeType and ForceResultCode are both -1).");

        if (settings.ResultSelectorControlLogOnly)
            report.Warn("ResultSelectorControlEnabled",
                "result-selector CONTROL armed in LOG-ONLY (dry-run): logs would-write intent, performs NO writes.");
        else
            report.Warn("ResultSelectorControlEnabled",
                "result-selector CONTROL is LIVE: it writes the evade-type/result-code on matching results. " +
                "Confine blast radius with MaxWrites / TargetCharId / MatchEvadeType.");
    }

    private static void ValidateTables(RuntimeSettings settings, SettingsValidationReport report)
    {
        foreach (var (name, values) in settings.FormulaTables)
        {
            string scope = $"FormulaTables.{name}";
            if (string.IsNullOrWhiteSpace(name))
                report.Error(scope, "table name is empty.");
            if (values is null || values.Count == 0)
                report.Error(scope, "table has no values.");
        }
    }

    private static void ValidateMatrices(RuntimeSettings settings, SettingsValidationReport report)
    {
        foreach (var (name, rows) in settings.FormulaMatrices)
        {
            string scope = $"FormulaMatrices.{name}";
            if (string.IsNullOrWhiteSpace(name))
                report.Error(scope, "matrix name is empty.");
            if (rows is null || rows.Count == 0)
            {
                report.Error(scope, "matrix has no rows.");
                continue;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] is null || rows[i].Count == 0)
                    report.Error($"{scope}[{i}]", "matrix row has no values.");
            }
        }
    }

    private static void ValidateMaps(RuntimeSettings settings, SettingsValidationReport report)
    {
        foreach (var (name, values) in settings.FormulaMaps)
        {
            string scope = $"FormulaMaps.{name}";
            if (string.IsNullOrWhiteSpace(name))
                report.Error(scope, "map name is empty.");
            if (values is null || values.Count == 0)
            {
                report.Error(scope, "map has no entries.");
                continue;
            }

            foreach (string key in values.Keys)
            {
                if (!FormulaContext.TryParseMapKey(key, out _))
                    report.Error(scope, $"map key '{key}' is not a valid Int32 decimal or 0x hex key.");
            }
        }
    }

    private static void ValidateEquipmentSlots(string groupName, List<EquipmentSlotProbe> slots, SettingsValidationReport report)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            string name = string.IsNullOrWhiteSpace(slot.Name) ? $"#{i + 1}" : slot.Name;
            string scope = $"{groupName}.{name}";

            if (slot.Offset >= 0)
            {
                int width = SlotWidthBytes(slot.Width);
                if (width == 0)
                    report.Error(scope, $"unsupported Width '{slot.Width}'. Use Byte or UInt16/Word.");
                else if (slot.Offset + width > RawSize)
                    report.Error(scope, $"Offset 0x{slot.Offset:X}+{width} exceeds copied unit snapshot size 0x{RawSize:X}.");
            }
            else
            {
                if (!HasSearchFilter(slot))
                    report.Warn(scope, "slot has no fixed Offset and no item search filter; it will always resolve as missing.");
                if (slot.SearchStart >= 0 && slot.SearchEnd >= 0 && slot.SearchEnd < slot.SearchStart)
                    report.Error(scope, "SearchEnd is below SearchStart.");
                if (slot.SearchWidth is not null &&
                    !slot.SearchWidth.Equals("Any", StringComparison.OrdinalIgnoreCase) &&
                    !slot.SearchWidth.Equals("Byte", StringComparison.OrdinalIgnoreCase) &&
                    !slot.SearchWidth.Equals("UInt16", StringComparison.OrdinalIgnoreCase) &&
                    !slot.SearchWidth.Equals("Word", StringComparison.OrdinalIgnoreCase))
                    report.Error(scope, $"unsupported SearchWidth '{slot.SearchWidth}'. Use Any, Byte, or UInt16/Word.");
            }
        }
    }

    private static void ValidateEquipmentRules(RuntimeSettings settings, SettingsValidationReport report)
    {
        var targetSlots = new HashSet<string>(
            settings.EquipmentSlots.Select(slot => slot.Name).Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var rule in settings.EquipmentDrRules)
        {
            string scope = $"EquipmentDrRules.{RuleName(rule.Name)}";
            if (!rule.Slot.Equals("Any", StringComparison.OrdinalIgnoreCase) && !targetSlots.Contains(rule.Slot))
                report.Warn(scope, $"Slot '{rule.Slot}' is not configured in EquipmentSlots.");
        }

        foreach (var rule in settings.DamageResponseRules)
        {
            string scope = $"DamageResponseRules.{RuleName(rule.Name)}";
            if (rule.UsesSlotMatch &&
                !string.IsNullOrWhiteSpace(rule.Slot) &&
                !rule.Slot.Equals("Any", StringComparison.OrdinalIgnoreCase) &&
                !targetSlots.Contains(rule.Slot))
                report.Warn(scope, $"Slot '{rule.Slot}' is not configured in EquipmentSlots.");
            if (rule.MultiplierDenominator <= 0 && string.IsNullOrWhiteSpace(rule.MultiplierFormula) && !rule.MultiplierPermille.HasValue)
                report.Error(scope, "MultiplierDenominator must be greater than zero.");
            if (rule.MultiplierPermille.HasValue && rule.MultiplierPermille.Value < 0)
                report.Error(scope, "MultiplierPermille must be nonnegative.");
        }
    }

    private static void ValidateMemoryTableProbes(RuntimeSettings settings, SettingsValidationReport report)
    {
        foreach (var probe in settings.MemoryTableProbes)
        {
            probe.Normalize();
            string scope = $"MemoryTableProbes.{probe.TraceName}";
            if (!probe.TryValidate(out string error))
                report.Error(scope, error);
            if (probe.Enabled)
                report.Warn(scope, "probe is enabled; live runs will scan process memory. Keep disabled until reviewed.");
        }
    }

    private static void ValidateDeathStateWrites(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DeathCaptureFollowTicks < 0)
            report.Error("DeathCaptureFollowTicks", "DeathCaptureFollowTicks must be nonnegative.");
        else if (settings.DeathCaptureFollowTicks > 4000)
            report.Warn("DeathCaptureFollowTicks", "runtime clamps DeathCaptureFollowTicks to 4000.");

        if (settings.CauseDeathOnZeroHp && !settings.RewriteObservedDamage)
            report.Warn("CauseDeathOnZeroHp", "death-state writes only run after an HP damage rewrite sets desired HP to zero.");
        if (settings.CauseDeathOnZeroHp && settings.DryRunRewrites)
            report.Warn("CauseDeathOnZeroHp", "DryRunRewrites prevents both HP writes and death-state writes.");
        if (settings.CauseDeathOnZeroHp && settings.DeathStateWrites.Count == 0)
            report.Error("DeathStateWrites", "CauseDeathOnZeroHp is enabled but no DeathStateWrites are configured.");

        for (int i = 0; i < settings.DeathStateWrites.Count; i++)
        {
            var write = settings.DeathStateWrites[i];
            string scope = $"DeathStateWrites.{RuleName(write?.Name ?? "", i + 1)}";
            if (write is null)
            {
                report.Error(scope, "write entry is null.");
                continue;
            }

            if (!write.TryValidate(out int width, out _, out string error))
            {
                report.Error(scope, error);
                continue;
            }

            if (write.Offset + width > RawSize)
                report.Error(scope, $"Offset 0x{write.Offset:X}+{width} exceeds copied unit snapshot size 0x{RawSize:X}.");
        }
    }

    private static void ValidateFormulas(RuntimeSettings settings, ItemCatalog catalog, SettingsValidationReport report)
    {
        var context = BuildRichFormulaContext(settings, catalog);

        foreach (var variable in settings.FormulaPreActionVariables)
            ValidateDerivedFormula(context, variable, "FormulaPreActionVariables", report);

        foreach (var variable in settings.FormulaPreResponseVariables)
            ValidateDerivedFormula(context, variable, "FormulaPreResponseVariables", report);

        for (int i = 0; i < settings.ActionSignalRules.Count; i++)
        {
            var rule = settings.ActionSignalRules[i];
            string scope = $"ActionSignalRules.{RuleName(rule.Name, i + 1)}";
            if (!IsSupportedActionSignalEventKind(rule.EventKind))
                report.Error(scope, $"unsupported EventKind '{rule.EventKind}'. Use Any, HP/HpChange, Damage/HpLoss, Healing/HpGain, MP/MpChange, Loss/MpLoss, or Gain/MpGain.");
            ValidateFormula(rule.ConditionFormula, context, $"{scope}.ConditionFormula", report, allowEmpty: true);
            foreach (var (name, formula) in rule.VariableFormulas ?? [])
                ValidateFormula(formula, context, $"{scope}.VariableFormulas.{name}", report);
        }

        foreach (var variable in settings.FormulaDerivedVariables)
            ValidateDerivedFormula(context, variable, "FormulaDerivedVariables", report);

        foreach (var variable in settings.FormulaTraceVariables)
            ValidateTraceFormula(context, variable, "FormulaTraceVariables", report);

        ValidateFormula(settings.RewriteConditionFormula, context, "RewriteConditionFormula", report, allowEmpty: true);

        for (int i = 0; i < settings.DamageRules.Count; i++)
        {
            var rule = settings.DamageRules[i];
            string scope = $"DamageRules.{RuleName(rule.Name, i + 1)}";
            if (!IsSupportedDamageRuleEventKind(rule.EventKind))
                report.Error(scope, $"unsupported EventKind '{rule.EventKind}'. Use Any, HP/HpChange, Damage/HpLoss, or Healing/HpGain.");
            ValidateFormula(rule.ConditionFormula, context, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.FinalDamageFormula, context, $"{scope}.FinalDamageFormula", report, allowEmpty: true);
            if (rule.ScaleDenominator <= 0)
                report.Error(scope, "ScaleDenominator must be greater than zero.");
        }

        for (int i = 0; i < settings.MpRules.Count; i++)
        {
            var rule = settings.MpRules[i];
            string scope = $"MpRules.{RuleName(rule.Name, i + 1)}";
            if (!IsSupportedMpEventKind(rule.EventKind))
                report.Error(scope, $"unsupported EventKind '{rule.EventKind}'. Use Any, Loss/MpLoss, or Gain/MpGain.");
            ValidateFormula(rule.ConditionFormula, context, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.FinalMpChangeFormula, context, $"{scope}.FinalMpChangeFormula", report, allowEmpty: true);
            if (rule.ScaleDenominator <= 0)
                report.Error(scope, "ScaleDenominator must be greater than zero.");
        }

        for (int i = 0; i < settings.EquipmentDrRules.Count; i++)
        {
            var rule = settings.EquipmentDrRules[i];
            string scope = $"EquipmentDrRules.{RuleName(rule.Name, i + 1)}";
            ValidateFormula(rule.ConditionFormula, context, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.DamageReductionFormula, context, $"{scope}.DamageReductionFormula", report, allowEmpty: true);
        }

        for (int i = 0; i < settings.DamageResponseRules.Count; i++)
        {
            var rule = settings.DamageResponseRules[i];
            string scope = $"DamageResponseRules.{RuleName(rule.Name, i + 1)}";
            ValidateFormula(rule.ConditionFormula, context, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.MultiplierFormula, context, $"{scope}.MultiplierFormula", report, allowEmpty: true);
        }

        ValidateFormula(settings.FinalDamageFormula, context, "FinalDamageFormula", report, allowEmpty: true);
        ValidateFormula(settings.MpRewriteConditionFormula, context, "MpRewriteConditionFormula", report, allowEmpty: true);
        ValidateFormula(settings.FinalMpChangeFormula, context, "FinalMpChangeFormula", report, allowEmpty: true);
    }

    private static bool IsSupportedMpEventKind(string? eventKind)
        => string.IsNullOrWhiteSpace(eventKind) ||
           eventKind.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Loss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MpLoss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Gain", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MpGain", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedDamageRuleEventKind(string? eventKind)
        => string.IsNullOrWhiteSpace(eventKind) ||
           eventKind.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpChange", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Damage", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpLoss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Healing", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Heal", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpGain", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedActionSignalEventKind(string? eventKind)
        => string.IsNullOrWhiteSpace(eventKind) ||
           eventKind.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpChange", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Damage", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpLoss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Healing", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Heal", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("HpGain", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MP", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MpChange", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Loss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MpLoss", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("Gain", StringComparison.OrdinalIgnoreCase) ||
           eventKind.Equals("MpGain", StringComparison.OrdinalIgnoreCase);

    private static void ValidateIntegratedEvaluation(RuntimeSettings settings, ItemCatalog catalog, SettingsValidationReport report)
    {
        var target = BuildSyntheticTarget(settings, catalog);
        var attacker = BuildSyntheticAttacker(settings, catalog);
        var action = BuildSyntheticAction(settings);
        var engine = new BattleFormulaEngine(settings, catalog);

        var damageSettingsEnabled = settings.RewriteObservedDamage;
        var healingSettingsEnabled = settings.RewriteObservedHealing;
        var mpLossSettingsEnabled = settings.RewriteObservedMpLoss;
        var mpGainSettingsEnabled = settings.RewriteObservedMpGain;
        var affectAllies = settings.AffectAllies;
        var affectFoes = settings.AffectFoes;
        settings.RewriteObservedDamage = true;
        settings.RewriteObservedHealing = true;
        settings.RewriteObservedMpLoss = true;
        settings.RewriteObservedMpGain = true;
        settings.AffectAllies = true;
        settings.AffectFoes = true;

        try
        {
            var damage = engine.Evaluate(new DamageEvent(target, 50, 30, 20, attacker, "validator", action, EventIndex: 1, EventSeed: 12345));
            if (!damage.ShouldRewrite && !IsIntentionalNoRewrite(damage.RuleName))
                report.Error("IntegratedDamageEvaluation", damage.RuleName);

            var missingContextDamage = engine.Evaluate(new DamageEvent(target, 50, 30, 20, null, "none", null, EventIndex: 3, EventSeed: 12347));
            if (!missingContextDamage.ShouldRewrite && !IsIntentionalNoRewrite(missingContextDamage.RuleName))
            {
                report.Warn(
                    "MissingContextDamageEvaluation",
                    "settings skip the HP rewrite when attacker/action context is absent: " +
                    $"{missingContextDamage.RuleName}. Guard optional context with a.present/action.present " +
                    "or add a vanillaDamage fallback if this settings file should be live-mapping safe.");
            }

            var healedTarget = target with { Hp = 40 };
            var healing = engine.Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker, "validator", action, EventIndex: 2, EventSeed: 12346));
            if (!healing.ShouldRewrite && !IsIntentionalNoRewrite(healing.RuleName))
                report.Error("IntegratedHealingEvaluation", healing.RuleName);

            var mpLossTarget = target with { Mp = 12 };
            var mpLoss = engine.EvaluateMp(new MpEvent(mpLossTarget, 20, 12, -8, attacker, "validator", action, EventIndex: 4, EventSeed: 12348));
            if (!mpLoss.ShouldRewrite && !IsIntentionalNoRewrite(mpLoss.RuleName))
                report.Error("IntegratedMpLossEvaluation", mpLoss.RuleName);

            var mpGainTarget = target with { Mp = 18 };
            var mpGain = engine.EvaluateMp(new MpEvent(mpGainTarget, 10, 18, 8, attacker, "validator", action, EventIndex: 5, EventSeed: 12349));
            if (!mpGain.ShouldRewrite && !IsIntentionalNoRewrite(mpGain.RuleName))
                report.Error("IntegratedMpGainEvaluation", mpGain.RuleName);
        }
        finally
        {
            settings.RewriteObservedDamage = damageSettingsEnabled;
            settings.RewriteObservedHealing = healingSettingsEnabled;
            settings.RewriteObservedMpLoss = mpLossSettingsEnabled;
            settings.RewriteObservedMpGain = mpGainSettingsEnabled;
            settings.AffectAllies = affectAllies;
            settings.AffectFoes = affectFoes;
        }
    }

    private static bool IsIntentionalNoRewrite(string reason)
        => reason.Equals("off", StringComparison.OrdinalIgnoreCase) ||
           reason.Equals("RewriteConditionFormula=0", StringComparison.OrdinalIgnoreCase) ||
           reason.Equals("MpRewriteConditionFormula=0", StringComparison.OrdinalIgnoreCase);

    private static void ValidateDerivedFormula(
        FormulaContext context,
        FormulaDerivedVariable variable,
        string group,
        SettingsValidationReport report)
    {
        string name = variable.NormalizedName;
        if (string.IsNullOrWhiteSpace(name))
        {
            report.Error(group, "variable has an empty name.");
            return;
        }

        if (ValidateFormula(variable.Formula, context, $"{group}.{name}", report))
        {
            FormulaExpression.TryEvaluate(variable.Formula, context, out int value, out _);
            context.Set(name, value);
            if (variable.SetConstAlias)
                context.Set($"const.{name}", value);
        }
    }

    private static bool ValidateFormula(
        string? formula,
        FormulaContext context,
        string scope,
        SettingsValidationReport report,
        bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            if (!allowEmpty)
                report.Error(scope, "formula is empty.");
            return allowEmpty;
        }

        if (FormulaExpression.TryEvaluate(formula, context, out _, out string error))
            return true;

        report.Error(scope, error);
        return false;
    }

    private static void ValidateTraceFormula(
        FormulaContext context,
        FormulaDerivedVariable variable,
        string group,
        SettingsValidationReport report)
    {
        string name = variable.NormalizedName;
        if (string.IsNullOrWhiteSpace(name))
        {
            report.Error(group, "trace variable has an empty name.");
            return;
        }

        ValidateFormula(variable.Formula, context, $"{group}.{name}", report);
    }

    private static FormulaContext BuildRichFormulaContext(RuntimeSettings settings, ItemCatalog catalog)
    {
        var target = BuildSyntheticTarget(settings, catalog);
        var attacker = BuildSyntheticAttacker(settings, catalog);
        var context = new FormulaContext(target, attacker, eventIndex: 1, eventSeed: 12345);

        foreach (var (name, value) in settings.FormulaVariables)
        {
            context.Set(name, value);
            context.Set($"const.{name}", value);
        }
        foreach (var (name, values) in settings.FormulaTables)
            context.SetTable(name, values);
        foreach (var (name, rows) in settings.FormulaMatrices)
            context.SetMatrix(name, rows);
        foreach (var (name, values) in settings.FormulaMaps)
            context.SetMap(name, values);

        SetCommonEventVariables(context);
        context.Set("result.finalDamage", 20);
        context.Set("result.desiredHp", 30);
        context.Set("result.shouldRewrite", 1);
        AddUnitVariables(context, "target", target);
        AddUnitVariables(context, "t", target);
        AddUnitVariables(context, "attacker", attacker);
        AddUnitVariables(context, "a", attacker);
        context.Set("attacker.inferred", 1);
        context.Set("a.inferred", 1);
        context.Set("attacker.sourceRecent", 1);
        context.Set("a.sourceRecent", 1);
        context.Set("attacker.sourceCt", 1);
        context.Set("a.sourceCt", 1);
        context.Set("attacker.sourceCounter", 1);
        context.Set("a.sourceCounter", 1);
        context.Set("attacker.sourcePending", 1);
        context.Set("a.sourcePending", 1);
        context.Set("attacker.sourceImmediate", 1);
        context.Set("a.sourceImmediate", 1);
        AddActionVariables(context, settings);
        AddSlotVariables(context, "slot", settings.EquipmentSlots, catalog, preferWeapon: false);
        AddSlotVariables(context, "targetSlot", settings.EquipmentSlots, catalog, preferWeapon: false);
        AddSlotVariables(context, "tslot", settings.EquipmentSlots, catalog, preferWeapon: false);
        AddSlotVariables(context, "attackerSlot", settings.AttackerEquipmentSlots, catalog, preferWeapon: true);
        AddSlotVariables(context, "aslot", settings.AttackerEquipmentSlots, catalog, preferWeapon: true);
        AddDamageResponseVariables(context);
        AddRuleItemVariables(context, catalog);

        return context;
    }

    private static UnitSnapshot BuildSyntheticTarget(RuntimeSettings settings, ItemCatalog catalog)
    {
        var raw = new byte[RawSize];
        raw[0x70] = 172;
        foreach (var slot in settings.EquipmentSlots.Where(slot => slot.Offset >= 0))
            WriteSlot(raw, slot, SyntheticItemId(slot, catalog, preferWeapon: false));
        return new UnitSnapshot((nint)0x2000, 0x80, 12, 30, 50, 2, true, 10, 8, 7, 4, 3, 70, 60, raw, 18, 30);
    }

    private static UnitSnapshot BuildSyntheticAttacker(RuntimeSettings settings, ItemCatalog catalog)
    {
        var raw = new byte[RawSize];
        raw[0x50] = 19;
        foreach (var slot in settings.AttackerEquipmentSlots.Where(slot => slot.Offset >= 0))
            WriteSlot(raw, slot, SyntheticItemId(slot, catalog, preferWeapon: true));
        return new UnitSnapshot((nint)0x1000, 0x01, 14, 40, 40, 1, false, 12, 7, 8, 5, 4, 75, 65, raw, 12, 20);
    }

    private static void WriteSlot(byte[] raw, EquipmentSlotProbe slot, int itemId)
    {
        if (slot.Offset < 0 || slot.Offset >= raw.Length) return;
        if (slot.Width.Equals("UInt16", StringComparison.OrdinalIgnoreCase) ||
            slot.Width.Equals("Word", StringComparison.OrdinalIgnoreCase))
        {
            if (slot.Offset + 1 >= raw.Length) return;
            raw[slot.Offset] = (byte)(itemId & 0xFF);
            raw[slot.Offset + 1] = (byte)((itemId >> 8) & 0xFF);
            return;
        }

        raw[slot.Offset] = (byte)Math.Clamp(itemId, 0, 255);
    }

    private static int SyntheticItemId(EquipmentSlotProbe slot, ItemCatalog catalog, bool preferWeapon)
    {
        if (slot.ItemId >= 0) return slot.ItemId;
        if (preferWeapon ||
            slot.SecondaryKind.Equals("weapon", StringComparison.OrdinalIgnoreCase) ||
            slot.TypeFlag.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
            return catalog.TryGet(19, out _) ? 19 : 1;
        return catalog.TryGet(172, out _) ? 172 : 1;
    }

    private static ActionSignal BuildSyntheticAction(RuntimeSettings settings)
    {
        var variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["signal"] = settings.ActionSignalRules.FirstOrDefault()?.Signal is > 0
                ? settings.ActionSignalRules.First().Signal
                : 1,
            ["vanillaDamage"] = 20,
            ["vanillaDamageAbs"] = 20,
            ["vanillaHealing"] = 0,
            ["vanillaMpChange"] = -8,
            ["vanillaMpChangeAbs"] = 8,
            ["vanillaMpLoss"] = 8,
            ["vanillaMpGain"] = 0,
            ["isDamage"] = 1,
            ["isHealing"] = 0,
            ["isMpLoss"] = 1,
            ["isMpGain"] = 0,
            ["isMpChange"] = 1,
        };

        foreach (var rule in settings.ActionSignalRules)
        {
            if (rule.Variables is not null)
            {
                foreach (var (key, value) in rule.Variables)
                    variables[FormulaExpression.NormalizeIdentifierPart(key)] = value == 0 ? 1 : value;
            }
            if (rule.VariableFormulas is not null)
            {
                foreach (var key in rule.VariableFormulas.Keys)
                    variables[FormulaExpression.NormalizeIdentifierPart(key)] = 1;
            }
        }

        return new ActionSignal("validator", "validator", variables);
    }

    private static void SetCommonEventVariables(FormulaContext context)
    {
        context.Set("vanillaDamage", 20);
        context.Set("vanillaDamageAbs", 20);
        context.Set("vanillaHealing", 10);
        context.Set("observedHpDelta", -20);
        context.Set("observedHpLoss", 20);
        context.Set("observedHpGain", 0);
        context.Set("previousHp", 50);
        context.Set("currentHp", 30);
        context.Set("vanillaMpChange", -8);
        context.Set("vanillaMpDelta", -8);
        context.Set("vanillaMpChangeAbs", 8);
        context.Set("vanillaMpLoss", 8);
        context.Set("vanillaMpGain", 0);
        context.Set("observedMpDelta", -8);
        context.Set("observedMpLoss", 8);
        context.Set("observedMpGain", 0);
        context.Set("previousMp", 20);
        context.Set("currentMp", 12);
        context.Set("equipmentDr", 3);
        context.Set("event.isDamage", 1);
        context.Set("event.isHealing", 0);
        context.Set("event.isHpLoss", 1);
        context.Set("event.isHpGain", 0);
        context.Set("event.isMpLoss", 1);
        context.Set("event.isMpGain", 0);
        context.Set("event.isMpChange", 1);
        context.Set("result.finalMpChange", -8);
        context.Set("result.desiredMp", 12);
        context.Set("result.shouldRewriteMp", 1);
    }

    private static void AddUnitVariables(FormulaContext context, string prefix, UnitSnapshot unit)
    {
        context.Set($"{prefix}.present", 1);
        context.Set($"{prefix}.charId", unit.CharId);
        context.Set($"{prefix}.level", unit.Level);
        context.Set($"{prefix}.hp", unit.Hp);
        context.Set($"{prefix}.maxHp", unit.MaxHp);
        context.Set($"{prefix}.mp", unit.Mp);
        context.Set($"{prefix}.maxMp", unit.MaxMp);
        context.Set($"{prefix}.team", unit.Team);
        context.Set($"{prefix}.isFoe", unit.IsFoe ? 1 : 0);
        context.Set($"{prefix}.isAlly", unit.IsFoe ? 0 : 1);
        context.Set($"{prefix}.pa", unit.Pa);
        context.Set($"{prefix}.ma", unit.Ma);
        context.Set($"{prefix}.speed", unit.Speed);
        context.Set($"{prefix}.ct", unit.Ct);
        context.Set($"{prefix}.move", unit.Move);
        context.Set($"{prefix}.jump", unit.Jump);
        context.Set($"{prefix}.brave", unit.Brave);
        context.Set($"{prefix}.faith", unit.Faith);

        // Mirror of Mod.AddUnitVariables newly-mapped attributes so formulas using them validate.
        int B(int offset) => Math.Max(0, unit.ReadByte(offset));
        context.Set($"{prefix}.job", B(0x03));
        context.Set($"{prefix}.zodiac", B(0x09) >> 4);
        int genderFlags = B(0x06);
        context.Set($"{prefix}.genderFlags", genderFlags);
        context.Set($"{prefix}.isMale", (genderFlags & 0x80) != 0 ? 1 : 0);
        context.Set($"{prefix}.isFemale", (genderFlags & 0x40) != 0 ? 1 : 0);
        context.Set($"{prefix}.isMonster", (genderFlags & 0x20) != 0 ? 1 : 0);
        context.Set($"{prefix}.maxBrave", B(0x2A));
        context.Set($"{prefix}.maxFaith", B(0x2C));
        context.Set($"{prefix}.rawPa", B(0x38));
        context.Set($"{prefix}.rawMa", B(0x39));
        context.Set($"{prefix}.rawSpeed", B(0x3A));
        context.Set($"{prefix}.weaponAtk", B(0x44));
        context.Set($"{prefix}.weaponAtkL", B(0x45));
        context.Set($"{prefix}.weaponParry", B(0x46));
        context.Set($"{prefix}.weaponParryL", B(0x47));
        context.Set($"{prefix}.shieldPhysParry", B(0x4A));
        context.Set($"{prefix}.physEva", B(0x4B));
        context.Set($"{prefix}.shieldMagParry", B(0x4E));
        context.Set($"{prefix}.hpGrowth", B(0x8A));
        context.Set($"{prefix}.hpMult", B(0x8B));
        context.Set($"{prefix}.mpGrowth", B(0x8C));
        context.Set($"{prefix}.mpMult", B(0x8D));
        context.Set($"{prefix}.spdGrowth", B(0x8E));
        context.Set($"{prefix}.spdMult", B(0x8F));
        context.Set($"{prefix}.paGrowth", B(0x90));
        context.Set($"{prefix}.paMult", B(0x91));
        context.Set($"{prefix}.maGrowth", B(0x92));
        context.Set($"{prefix}.maMult", B(0x93));
    }

    private static void AddActionVariables(FormulaContext context, RuntimeSettings settings)
    {
        foreach (string prefix in new[] { "action", "act" })
        {
            context.Set($"{prefix}.present", 1);
            context.Set($"{prefix}.sourceVanillaDamage", 1);
            context.Set($"{prefix}.sourcePending", 1);
            context.Set($"{prefix}.sourceImmediate", 1);
            context.Set($"{prefix}.signal", 1);
            context.Set($"{prefix}.id", 1);
            context.Set($"{prefix}.actionId", 1);
            context.Set($"{prefix}.batch", 1);
            context.Set($"{prefix}.batchEvent", 1);
            context.Set($"{prefix}.batchMaxEvents", 8);
            context.Set($"{prefix}.batchAgeMs", 100);
            context.Set($"{prefix}.score", 100);
            context.Set($"{prefix}.observedHpLoss", 20);
            context.Set($"{prefix}.targetCacheDamage", 20);
            context.Set($"{prefix}.targetCacheCredit", 20);
            context.Set($"{prefix}.targetCacheHealing", 20);
            context.Set($"{prefix}.targetCacheAmount", 20);
            context.Set($"{prefix}.currentTargetCacheDamage", 20);
            context.Set($"{prefix}.currentTargetCacheCredit", 20);
            context.Set($"{prefix}.recentTargetCacheDamage", 20);
            context.Set($"{prefix}.recentTargetCacheCredit", 20);
            context.Set($"{prefix}.damageCacheMatch", 1);
            context.Set($"{prefix}.currentDamageCacheMatch", 1);
            context.Set($"{prefix}.recentDamageCacheMatch", 1);
            context.Set($"{prefix}.creditCacheMatch", 1);
            context.Set($"{prefix}.currentCreditCacheMatch", 1);
            context.Set($"{prefix}.recentCreditCacheMatch", 1);
            context.Set($"{prefix}.exactDamageCacheMatch", 1);
            context.Set($"{prefix}.currentExactDamageCacheMatch", 1);
            context.Set($"{prefix}.recentExactDamageCacheMatch", 1);
            context.Set($"{prefix}.exactCreditCacheMatch", 1);
            context.Set($"{prefix}.currentExactCreditCacheMatch", 1);
            context.Set($"{prefix}.recentExactCreditCacheMatch", 1);
            context.Set($"{prefix}.lethalClampDamageCacheMatch", 1);
            context.Set($"{prefix}.currentLethalClampDamageCacheMatch", 1);
            context.Set($"{prefix}.recentLethalClampDamageCacheMatch", 1);
            context.Set($"{prefix}.hasCurrentTargetMetadata", 1);
            context.Set($"{prefix}.confidenceDamageCache", 1);
            context.Set($"{prefix}.confidenceRecentDamageCache", 1);
            context.Set($"{prefix}.confidenceLethalClampDamageCache", 1);
            context.Set($"{prefix}.confidenceRecentResolve", 1);
            context.Set($"{prefix}.runnerUpScore", 100);
            context.Set($"{prefix}.margin", 300);
            context.Set($"{prefix}.stateAgeMs", 100);
            context.Set($"{prefix}.seenAgeMs", 100);
            context.Set($"{prefix}.ctDropAgeMs", 100);
            context.Set($"{prefix}.actionIdAgeMs", 100);
            context.Set($"{prefix}.activeActionAgeMs", 100);
            context.Set($"{prefix}.currentActiveAction", 1);
            context.Set($"{prefix}.freshActionId", 1);
            context.Set($"{prefix}.freshActiveAction", 1);
            context.Set($"{prefix}.staleActionId", 0);
            context.Set($"{prefix}.staleActiveAction", 0);
            context.Set($"{prefix}.activeMarker2", 1);
            context.Set($"{prefix}.pendingFlag", 0);
            context.Set($"{prefix}.pendingFlag2", 0);
            context.Set($"{prefix}.pendingTimer", 255);
            context.Set($"{prefix}.vanillaDamage", 20);
            context.Set($"{prefix}.vanillaDamageAbs", 20);
            context.Set($"{prefix}.vanillaHealing", 0);
            context.Set($"{prefix}.vanillaMpChange", -8);
            context.Set($"{prefix}.vanillaMpChangeAbs", 8);
            context.Set($"{prefix}.vanillaMpLoss", 8);
            context.Set($"{prefix}.vanillaMpGain", 0);
            context.Set($"{prefix}.isDamage", 1);
            context.Set($"{prefix}.isHealing", 0);
            context.Set($"{prefix}.isMpLoss", 1);
            context.Set($"{prefix}.isMpGain", 0);
            context.Set($"{prefix}.isMpChange", 1);
            context.Set($"{prefix}.sourceMpChange", 0);

            foreach (var rule in settings.ActionSignalRules)
            {
                foreach (var (key, value) in rule.Variables ?? [])
                    context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", value == 0 ? 1 : value);
                if (rule.VariableFormulas is not null)
                {
                    foreach (var key in rule.VariableFormulas.Keys)
                        context.Set($"{prefix}.{FormulaExpression.NormalizeIdentifierPart(key)}", 1);
                }
            }
        }
    }

    private static void AddSlotVariables(
        FormulaContext context,
        string prefix,
        List<EquipmentSlotProbe> slots,
        ItemCatalog catalog,
        bool preferWeapon)
    {
        foreach (var slot in slots)
        {
            string name = FormulaExpression.NormalizeIdentifierPart(string.IsNullOrWhiteSpace(slot.Name) ? $"offset_{slot.Offset:X}" : slot.Name);
            string baseName = $"{prefix}.{name}";
            int itemId = SyntheticItemId(slot, catalog, preferWeapon);
            context.Set(baseName, itemId);
            context.Set($"{baseName}.present", 1);
            context.Set($"{baseName}.itemId", itemId);
            context.Set($"{baseName}.offset", slot.Offset >= 0 ? slot.Offset : Math.Max(0, slot.SearchStart));
            context.Set($"{baseName}.scanMatches", 1);
            context.Set($"{baseName}.ambiguous", 0);
            context.Set($"{baseName}.widthByte", slot.Width.Equals("Byte", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            context.Set($"{baseName}.widthWord", slot.Width.Equals("UInt16", StringComparison.OrdinalIgnoreCase) || slot.Width.Equals("Word", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

            if (catalog.TryGet(itemId, out var item))
                item.AddVariables(context, baseName);
            else
                ItemCatalogEntry.AddDefaultVariables(context, baseName, itemId);
        }
    }

    private static void AddDamageResponseVariables(FormulaContext context)
    {
        context.Set("damageResponsePermille", 950);
        context.Set("damageResponse.rawPermille", 950);
        context.Set("damageResponse.permille", 950);
        context.Set("damageResponse.ruleCount", 1);
        context.Set("damageResponse.clamped", 0);
        context.Set("responsePermille", 950);
        context.Set("response.rawPermille", 950);
        context.Set("response.permille", 950);
        context.Set("typeResponsePermille", 950);
        context.Set("typeResponse.rawPermille", 950);
        context.Set("typeResponse.permille", 950);
        context.Set("combinedResponsePermille", 950);
        context.Set("combinedResponse.permille", 950);
        context.Set("boundedResponsePermille", 950);
        context.Set("boundedResponse.permille", 950);
    }

    private static void AddRuleItemVariables(FormulaContext context, ItemCatalog catalog)
    {
        int itemId = catalog.TryGet(172, out var item) ? 172 : 0;
        context.Set("slotItemId", itemId);
        context.Set("slot.itemId", itemId);
        context.Set("item.id", itemId);
        if (item is not null)
            item.AddVariables(context, "item");
        else
            ItemCatalogEntry.AddDefaultVariables(context, "item", itemId);
    }

    private static bool HasSearchFilter(EquipmentSlotProbe slot)
        => slot.ItemId >= 0 ||
           slot.MinItemId.HasValue ||
           slot.MaxItemId.HasValue ||
           !string.IsNullOrWhiteSpace(slot.ItemCategory) ||
           !string.IsNullOrWhiteSpace(slot.TypeFlag) ||
           !string.IsNullOrWhiteSpace(slot.SecondaryKind) ||
           !string.IsNullOrWhiteSpace(slot.NameContains) ||
           slot.MinArmorHpBonus.HasValue ||
           slot.MaxArmorHpBonus.HasValue ||
           slot.MinWeaponPower.HasValue ||
           slot.MaxWeaponPower.HasValue;

    private static int SlotWidthBytes(string width)
        => width.Equals("Byte", StringComparison.OrdinalIgnoreCase) ? 1 :
           width.Equals("UInt16", StringComparison.OrdinalIgnoreCase) ||
           width.Equals("Word", StringComparison.OrdinalIgnoreCase) ? 2 :
           0;

    private static string RuleName(string name, int index = 0)
        => string.IsNullOrWhiteSpace(name) ? (index > 0 ? $"#{index}" : "unnamed") : name;
}
