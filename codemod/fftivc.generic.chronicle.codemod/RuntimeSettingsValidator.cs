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

    public static SettingsValidationReport Validate(
        RuntimeSettings settings,
        ItemCatalog catalog,
        AbilityCatalog? abilityCatalog = null)
    {
        var report = new SettingsValidationReport();
        report.Info("settings", settings.Describe());
        report.Info("catalog", catalog.Describe());

        ValidateScalarRanges(settings, abilityCatalog, report);
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

    private static void ValidateScalarRanges(
        RuntimeSettings settings,
        AbilityCatalog? abilityCatalog,
        SettingsValidationReport report)
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
        if (settings.DclActionContextMaxAgeMs < 0)
            report.Error("DclActionContextMaxAgeMs", "DclActionContextMaxAgeMs must be nonnegative.");
        else if (settings.DclActionContextMaxAgeMs > 60000)
            report.Warn("DclActionContextMaxAgeMs", "values above 60000 ms can reuse stale calc-entry action context.");
        if (settings.DclComputePointCacheTtlMs <= 0)
            report.Error("DclComputePointCacheTtlMs", "DclComputePointCacheTtlMs must be positive.");
        else if (settings.DclComputePointCacheTtlMs > 60000)
            report.Warn("DclComputePointCacheTtlMs", "values above 60000 ms can retain a stale execution result for pointer reuse.");
        if (settings.DclComputePointNumericEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclComputePointNumericEnabled", "the compute-point writer requires DclPipelineEnabled and calc-entry action context.");
            if (string.IsNullOrWhiteSpace(settings.DclDamageFormula) &&
                string.IsNullOrWhiteSpace(settings.DclHealingFormula) &&
                string.IsNullOrWhiteSpace(settings.DclMpDebitFormula) &&
                string.IsNullOrWhiteSpace(settings.DclMpCreditFormula) &&
                !settings.DclHitControlEnabled)
                report.Error("DclComputePointNumericEnabled", "configure at least one HP/MP formula or authored hit control before enabling the AI-facing writer.");
            if (settings.StagedBundleForceKind >= 0 || settings.StagedBundleForceDmg >= 0 || settings.StagedBundleForceResFlag >= 0)
                report.Error("DclComputePointNumericEnabled", "the LT35 static staged-bundle force fields conflict with the permanent compute-point writer; leave them at -1.");
            report.Warn("DclComputePointNumericEnabled", "publishes the final HP/MP result at the proven post-calc AI boundary and reuses the exact cached execution result at pre-clamp. Instant KO exposes expected lethal debit to AI, rolls once at confirmed execution, and delivers that cached outcome; representative action-family regression remains required.");
        }
        if (settings.DclDecisionMaxLogs < 0)
            report.Error("DclDecisionMaxLogs", "DclDecisionMaxLogs must be nonnegative.");
        if (settings.DclMpTrickleMaxLogs < 0)
            report.Error("DclMpTrickleMaxLogs", "DclMpTrickleMaxLogs must be nonnegative.");
        if (settings.DclMpTrickleEnabled)
        {
            if (string.IsNullOrWhiteSpace(settings.DclMpTrickleFormula))
                report.Error("DclMpTrickleFormula", "DclMpTrickleEnabled requires an explicit per-own-turn credit formula.");
            report.Warn("DclMpTrickleEnabled", "native MaxMP remains the per-battle budget; this poll-driven feature writes only a clamped credit on the unit +0x1B8 own-turn rising edge and requires a live gate.");
        }
        if (settings.DclResultFlagsPreserveMask is < 0 or > DclResultFlags.DefaultPreserveMask)
            report.Error("DclResultFlagsPreserveMask", "the preserve mask must be within 0x00..0x0F; numeric result bits 0xF0 are derived from the final staged channels.");
        if (settings.DclResultFlagsControlEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclResultFlagsControlEnabled", "result-flag ownership runs in the DCL pre-clamp transaction; enable DclPipelineEnabled.");
            if (string.IsNullOrWhiteSpace(settings.DclDamageFormula) &&
                string.IsNullOrWhiteSpace(settings.DclHealingFormula) &&
                string.IsNullOrWhiteSpace(settings.DclMpDebitFormula) &&
                string.IsNullOrWhiteSpace(settings.DclMpCreditFormula) &&
                !settings.DclInstantKoControlEnabled)
                report.Error("DclResultFlagsControlEnabled", "configure at least one staged HP/MP formula or instant-KO rule before taking result-flag ownership.");
            report.Warn("DclResultFlagsControlEnabled", "rebuilds result byte +0x1E5 atomically from final HP debit/credit and MP debit/credit. The selector prioritizes HP damage, then MP debit, then low-bit effects, then HP/MP credit; combined 0x50 and 0x90 flags are native, while partial-shield 0xA0 remains live-gated for presentation.");
        }
        if (settings.DclPipelineEnabled && string.IsNullOrWhiteSpace(settings.DclDamageFormula) &&
            string.IsNullOrWhiteSpace(settings.DclHealingFormula) &&
            string.IsNullOrWhiteSpace(settings.DclMpDebitFormula) &&
            string.IsNullOrWhiteSpace(settings.DclMpCreditFormula) &&
            !settings.DclStatusControlEnabled && !settings.DclInstantKoControlEnabled &&
            !settings.DclPhysicalContestEnabled && !settings.DclMagicEvadeEnabled &&
            !settings.DclReactionTaxonomyEnabled)
            report.Warn("DclDamageFormula", "DclPipelineEnabled has no HP/MP outcome formula, status control, physical contest, or reaction taxonomy, so the DCL path falls through to legacy pre-clamp behavior.");
        if (settings.DclPipelineEnabled && settings.CalcEntryProbeRva <= 0)
            report.Error("CalcEntryProbeRva", "DclPipelineEnabled needs the calc-entry probe for action context; CalcEntryProbeRva must be positive.");
        if (settings.DclPipelineEnabled &&
            (!string.IsNullOrWhiteSpace(settings.DclDamageFormula) ||
             !string.IsNullOrWhiteSpace(settings.DclHealingFormula) ||
             !string.IsNullOrWhiteSpace(settings.DclMpDebitFormula) ||
             !string.IsNullOrWhiteSpace(settings.DclMpCreditFormula) ||
             settings.DclInstantKoControlEnabled))
        {
            // The pre-clamp stub runs the managed (DCL) callback FIRST, then plan/static writes:
            // any of these being armed would silently overwrite the DCL debit in the same hook fire.
            if (settings.PreClampFormulaPlanEnabled)
                report.Error("DclPipelineEnabled", "PreClampFormulaPlanEnabled overwrites the DCL debit after the managed callback; disable the plan when the DCL pipeline is on.");
            if (settings.PreClampDamageRewriteForcedDebit >= 0)
                report.Error("DclPipelineEnabled", "PreClampDamageRewriteForcedDebit overwrites the DCL debit after the managed callback; set it to -1 when the DCL pipeline is on.");
            if (settings.PreClampDamageRewriteForcedCredit >= 0)
                report.Error("DclPipelineEnabled", "PreClampDamageRewriteForcedCredit rewrites staged credit after the managed callback; set it to -1 when the DCL pipeline is on.");
            if (settings.PreClampDamageRewriteLogOnly)
                report.Warn("DclPipelineEnabled", "PreClampDamageRewriteLogOnly makes the managed callback return early, so the DCL formula never runs.");
        }
        if (settings.DclHitDecisionTtlMs < 0)
            report.Error("DclHitDecisionTtlMs", "DclHitDecisionTtlMs must be nonnegative.");
        if (settings.DclHitMaxLogs < 0)
            report.Error("DclHitMaxLogs", "DclHitMaxLogs must be nonnegative.");
        if (settings.DclHitForcedRoll is < -1 or > 99)
            report.Error("DclHitForcedRoll", "DclHitForcedRoll must be -1 (use the RNG) or a fixed roll 0..99.");
        if (settings.DclMissClassEvadeValue is < 0 or > 255)
            report.Error("DclMissClassEvadeValue", "DclMissClassEvadeValue must be within 0..255.");
        if (settings.DclHitControlEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclHitControlEnabled", "hit control needs the DCL pipeline (catalogs + formula-context machinery); enable DclPipelineEnabled.");
            if (settings.CalcEntryEvadeStampEnabled)
                report.Error("DclHitControlEnabled", "CalcEntryEvadeStampEnabled writes the same target evade bytes at the same calc-entry site; the hit-control decision callback subsumes the static stamp — disable it.");
            if (!settings.ItemTableEvadeZeroEnabled)
                report.Error("DclHitControlEnabled", "hit control requires ItemTableEvadeZeroEnabled: the VM derives equipment evade from the loaded item tables, so residual equipment evade would steal a HIT decision.");
            if (!settings.EvadeCopierOverrideEnabled)
                report.Error("DclHitControlEnabled", "hit control requires EvadeCopierOverrideEnabled with an all-zero profile so equip/refresh copiers cannot re-raise evade bytes between decisions.");
            else if (settings.EvadeCopierOverride46 != 0 || settings.EvadeCopierOverride47 != 0 ||
                     settings.EvadeCopierOverride48 != 0 || settings.EvadeCopierOverride49 != 0 ||
                     settings.EvadeCopierOverride4A != 0 || settings.EvadeCopierOverride4B != 0 ||
                     settings.EvadeCopierOverride4C != 0 || settings.EvadeCopierOverride4D != 0 ||
                     settings.EvadeCopierOverride4E != 0)
                report.Error("DclHitControlEnabled", "EvadeCopierOverride46..4E must all be 0 under hit control; the decision callback is the only writer allowed to raise an evade byte.");
            if (string.IsNullOrWhiteSpace(settings.DclHitChanceFormula))
                report.Error("DclHitChanceFormula", "DclHitControlEnabled requires DclHitChanceFormula.");
            if (settings.CalcEntryProbeRva <= 0)
                report.Error("CalcEntryProbeRva", "DclHitControlEnabled hooks calc-entry; CalcEntryProbeRva must be positive.");
            report.Warn("DclHitControlEnabled", "the hit-control decision callback runs managed code on the calc-entry hot path (fires at preview, charge and AI evaluation, not only execution) and forces the binary outcome by writing the target's evade input bytes.");
        }
        if (settings.DclAttackForcedRoll != -1 && settings.DclAttackForcedRoll is < 3 or > 18)
            report.Error("DclAttackForcedRoll", "DclAttackForcedRoll must be -1 (use the mod RNG) or a fixed 3d6 total within 3..18.");
        if (settings.DclDefenseForcedRoll != -1 && settings.DclDefenseForcedRoll is < 3 or > 18)
            report.Error("DclDefenseForcedRoll", "DclDefenseForcedRoll must be -1 (use the mod RNG) or a fixed 3d6 total within 3..18.");
        if (settings.DclGuardMaxLogs < 0)
            report.Error("DclGuardMaxLogs", "DclGuardMaxLogs must be nonnegative.");
        if (settings.DclPhysicalContestEnabled)
        {
            if (!settings.DclHitControlEnabled)
                report.Error("DclPhysicalContestEnabled", "the physical contest is a DCL hit-decision model; enable DclHitControlEnabled.");
            if (!settings.DclMissOutputControlEnabled || !settings.DclMissSelectorOutcomeEnabled)
                report.Error("DclPhysicalContestEnabled", "attack misses, fumbles, and successful defenses require DclMissOutputControlEnabled and DclMissSelectorOutcomeEnabled.");
            if (!settings.DclMissSuppressReactionsEnabled)
                report.Error("DclPhysicalContestEnabled", "authored physical misses and defenses must suppress native hit reactions; enable DclMissSuppressReactionsEnabled.");
            if (settings.PreClampDamageRewriteLogOnly)
                report.Error("DclPhysicalContestEnabled", "PreClampDamageRewriteLogOnly bypasses guard spending and authored miss delivery.");
            if (string.IsNullOrWhiteSpace(settings.DclPhysicalContestConditionFormula) ||
                string.IsNullOrWhiteSpace(settings.DclAttackSkillFormula) ||
                string.IsNullOrWhiteSpace(settings.DclDodgeFormula) ||
                string.IsNullOrWhiteSpace(settings.DclParryFormula) ||
                string.IsNullOrWhiteSpace(settings.DclBlockFormula) ||
                string.IsNullOrWhiteSpace(settings.DclDefenseAllowedFormula) ||
                string.IsNullOrWhiteSpace(settings.DclDefenseModifierFormula) ||
                string.IsNullOrWhiteSpace(settings.DclParryUsesFormula) ||
                string.IsNullOrWhiteSpace(settings.DclBlockUsesFormula))
                report.Error("DclPhysicalContestEnabled", "the applicability, attack, defense, facing-policy, and finite-guard formulas must all be present.");
            if (!settings.DclPreviewHitPctEnabled)
                report.Warn("DclPhysicalContestEnabled", "enable DclPreviewHitPctEnabled for exact two-roll forecast parity.");
            report.Warn("DclPhysicalContestEnabled", "finite Parry/Block charges commit only in the successful apply window and fully refresh on the defender's +0x1B8 own-turn rising edge.");
        }
        if (settings.DclMagicEvadeCapPct is < 0 or > 100)
            report.Error("DclMagicEvadeCapPct", "DclMagicEvadeCapPct must be within 0..100.");
        if (settings.DclMagicEvadeEnabled)
        {
            if (!settings.DclHitControlEnabled)
                report.Error("DclMagicEvadeEnabled", "Magic Evade is a DCL hit-decision model; enable DclHitControlEnabled.");
            if (!settings.DclMissOutputControlEnabled || !settings.DclMissSelectorOutcomeEnabled)
                report.Error("DclMagicEvadeEnabled", "evaded offensive magic requires DclMissOutputControlEnabled and DclMissSelectorOutcomeEnabled.");
            if (settings.PreClampDamageRewriteLogOnly)
                report.Error("DclMagicEvadeEnabled", "PreClampDamageRewriteLogOnly bypasses authored magic-evade delivery.");
            if (string.IsNullOrWhiteSpace(settings.DclMagicEvadeConditionFormula) ||
                string.IsNullOrWhiteSpace(settings.DclMagicEvadeFormula))
                report.Error("DclMagicEvadeEnabled", "the offensive-magic applicability and Magic Evade formulas must both be present.");
            if (!settings.DclPreviewHitPctEnabled)
                report.Warn("DclMagicEvadeEnabled", "enable DclPreviewHitPctEnabled so each target shows the capped Magic Evade hit chance.");
            report.Warn("DclMagicEvadeEnabled", "the applicability formula owns the DCL taxonomy and must exclude healing and status-only actions; formula 0x08 is the catalog's native magic-damage family.");
        }
        if (settings.DclPreviewHitPctEnabled)
        {
            if (!settings.DclHitControlEnabled)
                report.Error("DclPreviewHitPctEnabled", "DCL forecast parity requires DclHitControlEnabled so calc-entry can produce and mirror the authored percentage.");
            if (settings.PreviewHitPctControlEnabled && !settings.PreviewHitPctLogOnly && settings.PreviewHitPctForcedValue >= 0)
                report.Error("DclPreviewHitPctEnabled", "cannot share the preview hook with a static PreviewHitPctForcedValue; use log-only observation or disable the static preview control.");
            report.Warn("DclPreviewHitPctEnabled", "the forecast hit% is replaced with the percentage from the same cached DCL decision used for execution; this changes display only, not the roll.");
        }
        if (settings.DclPreviewAmountEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclPreviewAmountEnabled", "formula-driven forecast amounts require DclPipelineEnabled for calc-entry action context.");
            if (string.IsNullOrWhiteSpace(settings.DclPreviewDamageFormula) &&
                string.IsNullOrWhiteSpace(settings.DclPreviewHealingFormula))
                report.Error("DclPreviewAmountEnabled", "configure at least one of DclPreviewDamageFormula or DclPreviewHealingFormula.");
            if (settings.PreviewForecastPokeEnabled)
                report.Error("DclPreviewAmountEnabled", "the static PreviewForecastPokeEnabled writer conflicts with formula-driven forecast amounts.");
            if (settings.PreviewDamageControlEnabled && !settings.PreviewDamageLogOnly && settings.PreviewDamageForcedValue >= 0)
                report.Error("DclPreviewAmountEnabled", "a static PreviewDamageForcedValue conflicts with formula-driven forecast amounts.");
            report.Warn("DclPreviewAmountEnabled", "preview formulas run with dcl.oldDebit/oldCredit/oldMpDebit/oldMpCredit equal to zero; author them from intrinsic action/unit/equipment inputs.");
        }
        if (settings.DclStatusForcedRoll != -1 && settings.DclStatusForcedRoll is < 3 or > 18)
            report.Error("DclStatusForcedRoll", "DclStatusForcedRoll must be -1 (use the mod RNG) or a fixed 3d6 total within 3..18.");
        if (settings.DclStatusMaxLogs < 0)
            report.Error("DclStatusMaxLogs", "DclStatusMaxLogs must be nonnegative.");
        if (settings.DclStatusControlEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclStatusControlEnabled", "status control requires DclPipelineEnabled for action context and the successful apply hook.");
            if (settings.DclStatusRules.Count == 0)
                report.Error("DclStatusRules", "DclStatusControlEnabled requires at least one per-ability status rule.");
            if (settings.PreClampDamageRewriteLogOnly)
                report.Error("DclStatusControlEnabled", "PreClampDamageRewriteLogOnly bypasses the managed DCL callback, so authored statuses cannot commit.");

            var seenStatusRules = new List<(int AbilityId, int ActionType, int ByteIndex, int Mask)>();
            for (int i = 0; i < settings.DclStatusRules.Count; i++)
            {
                var rule = settings.DclStatusRules[i];
                string scope = $"DclStatusRules.{RuleName(rule.Name, i + 1)}";
                if (rule.AbilityId is < 0 or > 65535)
                    report.Error(scope, "AbilityId must identify one exact action within 0..65535; wildcard status rules are not accepted.");
                if (rule.ActionType is < -1 or > 255)
                    report.Error(scope, "ActionType must be -1 (any) or a byte within 0..255.");
                if (rule.StatusByteIndex is < 0 or > 4)
                    report.Error(scope, "StatusByteIndex must be 0..4.");
                else if (rule.StatusByteIndex == 0 && rule.StatusMask != 0x10)
                    report.Error(scope, "Status byte 0 is lifecycle-sensitive: only the proven Undead mask 0x10 is accepted; Crystal, KO, Charging, Jumping, Defending, and Performing require dedicated ownership.");
                if (rule.StatusMask is < 1 or > 255 || (rule.StatusMask & (rule.StatusMask - 1)) != 0)
                    report.Error(scope, "StatusMask must contain exactly one native status bit (1,2,4,8,16,32,64,128).");
                if (!rule.IsAdd && !rule.IsRemove)
                    report.Error(scope, "Operation must be 'add' or 'remove'.");
                if (!DclStatusGroups.IsSupportedMode(rule.ContestMode))
                    report.Error(scope, "ContestMode must be 'independent', 'all-or-nothing', or 'random-one'.");
                if (!rule.UsesSharedContest && !string.IsNullOrWhiteSpace(rule.ContestGroup))
                    report.Error(scope, "ContestGroup must be empty when ContestMode is independent.");
                if (rule.UsesSharedContest)
                {
                    bool approvedRetainedCarrier = rule.NativeRiderRetainedAsCarrier &&
                                                   DclStatusNativeCarrier.TryGetRequiredBits(rule.AbilityId, out _);
                    bool approvedSuppressedCarrier = rule.NativeRiderSuppressedByData &&
                                                     DclStatusGroupedCarrier.TryGetSuppressedDataBits(rule.AbilityId, out _);
                    bool approvedPostCalcProducer = rule.NativeRiderReplacedPostCalc &&
                        abilityCatalog is not null &&
                        abilityCatalog.TryGet(rule.AbilityId, out var producerAbility) &&
                        DclStatusConditionalProducer.IsSupportedFormula(producerAbility.Formula);
                    if (!approvedRetainedCarrier && !approvedSuppressedCarrier && !approvedPostCalcProducer)
                        report.Error(scope, "shared status contests are restricted to statically mapped retained carriers, grouped data-suppressed damage carriers, or catalog-verified post-calc producers.");
                    if (string.IsNullOrWhiteSpace(rule.ContestGroup))
                        report.Error(scope, "shared status contests require a nonempty ContestGroup.");
                    if (!rule.IsAdd)
                        report.Error(scope, "shared status contests currently support add rules only.");
                    if (!string.IsNullOrWhiteSpace(rule.ConditionFormula))
                        report.Error(scope, "shared status contests cannot use ConditionFormula; every group member must be decided together.");
                    if ((rule.ResistanceFormula ?? "").Contains("status.", StringComparison.OrdinalIgnoreCase))
                        report.Error(scope, "a shared ResistanceFormula cannot reference status.* because one invariant contest is evaluated for the whole group.");
                }
                if (!rule.NativeRiderAbsent && !rule.NativeRiderSuppressedByData &&
                    !rule.NativeRiderRetainedAsCarrier && !rule.NativeRiderReplacedPostCalc)
                    report.Error(scope, "NativeRiderPolicy must be 'absent', 'suppressed-by-data', 'retained-as-carrier', or 'replaced-post-calc'; managed status authority is rejected until native ownership is explicit.");
                if (rule.NativeRiderReplacedPostCalc)
                {
                    if (abilityCatalog is null || !abilityCatalog.Loaded)
                        report.Error(scope, "replaced-post-calc requires the loaded ability catalog to verify formula family and complete native packet ownership.");
                    else if (!abilityCatalog.TryGet(rule.AbilityId, out var producerAbility) ||
                             !DclStatusConditionalProducer.IsSupportedFormula(producerAbility.Formula))
                        report.Error(scope, "replaced-post-calc is restricted to the statically mapped conditional-producer formula families.");
                    if (rule.ActionType != -1)
                        report.Error(scope, "replaced-post-calc rules must use ActionType=-1 so every execution owns its native packet bit.");
                }
                if (rule.NativeRiderRetainedAsCarrier)
                {
                    if (!DclStatusNativeCarrier.TryGetRequiredBits(rule.AbilityId, out _))
                        report.Error(scope, "retained-as-carrier is not approved for this ability; only statically mapped status-only carriers are accepted.");
                    if (rule.ActionType != -1)
                        report.Error(scope, "retained-as-carrier rules must use ActionType=-1 so every execution context owns the native packet bit.");
                    if (!string.IsNullOrWhiteSpace(rule.ConditionFormula))
                        report.Error(scope, "retained-as-carrier rules cannot use ConditionFormula; every inherited packet bit must be decided on every matching target.");
                    string requiredMode = DclStatusNativeCarrier.RequiredContestMode(rule.AbilityId);
                    if (rule.NormalizedContestMode != requiredMode)
                        report.Error(scope, $"retained carrier ability {rule.AbilityId} requires ContestMode='{requiredMode}' for its native packet semantics.");
                }
                if (rule.NativeRiderSuppressedByData && rule.UsesSharedContest)
                {
                    if (!DclStatusGroupedCarrier.TryGetSuppressedDataBits(rule.AbilityId, out _))
                        report.Error(scope, "grouped suppressed-by-data ownership is not approved for this ability.");
                    if (rule.ActionType != -1)
                        report.Error(scope, "grouped suppressed-by-data rules must use ActionType=-1 so every execution owns the removed native rider.");
                    string requiredMode = DclStatusGroupedCarrier.RequiredContestMode(rule.AbilityId);
                    if (rule.NormalizedContestMode != requiredMode)
                        report.Error(scope, $"grouped data carrier ability {rule.AbilityId} requires ContestMode='{requiredMode}'.");
                }
                if (rule.NativeRiderSuppressedByData &&
                    DclStatusConditionalCarrier.TryGetRequiredBit(rule.AbilityId, out var conditionalBit))
                {
                    if (rule.ActionType != -1)
                        report.Error(scope, "conditional data carriers must use ActionType=-1 so every execution is classified by the required target condition.");
                    if (rule.StatusByteIndex != conditionalBit.ByteIndex || rule.StatusMask != conditionalBit.Mask)
                        report.Error(scope, $"conditional data carrier ability {rule.AbilityId} may own only byte {conditionalBit.ByteIndex}/0x{conditionalBit.Mask:X2}.");
                    if (!rule.IsAdd)
                        report.Error(scope, "the Self-Destruct conditional carrier owns an Oil add, not a remove.");
                    if (!DclStatusConditionalCarrier.IsRequiredCondition(rule.AbilityId, rule.ConditionFormula))
                        report.Error(scope, $"conditional data carrier ability {rule.AbilityId} requires ConditionFormula='{DclStatusConditionalCarrier.SelfDestructVictimCondition}' so the caster result never receives the victim-only rider.");
                }
                var ownership = (rule.AbilityId, rule.ActionType, rule.StatusByteIndex, rule.StatusMask);
                bool overlapsExistingOwner = seenStatusRules.Any(existing =>
                    existing.AbilityId == ownership.AbilityId &&
                    existing.ByteIndex == ownership.StatusByteIndex &&
                    existing.Mask == ownership.StatusMask &&
                    (existing.ActionType == ownership.ActionType || existing.ActionType == -1 || ownership.ActionType == -1));
                if (overlapsExistingOwner)
                    report.Error(scope, "overlapping rules for the same ability/status bit are not accepted; one packet bit has one owner for every action type.");
                else
                    seenStatusRules.Add(ownership);
                if (rule.DurationTargetTurns is < 0 or > 99)
                    report.Error(scope, "DurationTargetTurns must be 0 (persistent/native removal only) or a target-turn count within 1..99.");
                if (rule.IsAdd && string.IsNullOrWhiteSpace(rule.ResistanceFormula))
                    report.Error(scope, "add rules require ResistanceFormula; its result is the target number for the 3d6 resistance roll.");
                if (rule.IsRemove && !string.IsNullOrWhiteSpace(rule.ResistanceFormula))
                    report.Warn(scope, "remove rules are connected cures and ignore ResistanceFormula.");
                if (rule.IsRemove && rule.DurationTargetTurns != 0)
                    report.Error(scope, "remove rules cannot own a duration; set DurationTargetTurns to 0.");
            }

            if (settings.DclStatusRules.Any(rule => rule.StatusByteIndex == 0 && rule.StatusMask == 0x10))
                report.Warn("DclStatusRules", "status byte 0 is enabled only for Undead 0x10, whose durable/effective write is live-proven; all other byte-0 lifecycle bits remain blocked.");

            foreach (var carrierGroup in settings.DclStatusRules
                         .Where(rule => rule.NativeRiderRetainedAsCarrier)
                         .GroupBy(rule => rule.AbilityId))
            {
                if (!DclStatusNativeCarrier.TryGetRequiredBits(carrierGroup.Key, out var requiredBits))
                    continue;
                var ownedBits = carrierGroup
                    .Select(rule => new DclNativeStatusBit(rule.StatusByteIndex, (byte)rule.StatusMask))
                    .ToHashSet();
                var missing = requiredBits.Where(bit => !ownedBits.Contains(bit)).ToArray();
                if (missing.Length > 0)
                {
                    string missingText = string.Join(", ", missing.Select(bit => $"byte {bit.ByteIndex}/0x{bit.Mask:X2}"));
                    report.Error(
                        "DclStatusRules",
                        $"retained native carrier ability {carrierGroup.Key} lacks managed ownership for {missingText}; every inherited rider bit must be replaced in the packet.");
                }

                string requiredMode = DclStatusNativeCarrier.RequiredContestMode(carrierGroup.Key);
                if (requiredMode != DclStatusGroups.Independent)
                {
                    var sharedRules = carrierGroup.Where(rule => rule.UsesSharedContest).ToArray();
                    var groupNames = sharedRules.Select(rule => rule.NormalizedContestGroup).Distinct().ToArray();
                    if (sharedRules.Length != carrierGroup.Count() || groupNames.Length != 1 || groupNames[0].Length == 0)
                        report.Error(
                            "DclStatusRules",
                            $"retained native carrier ability {carrierGroup.Key} must place every inherited bit in one nonempty '{requiredMode}' ContestGroup.");
                }
            }

            foreach (var sharedGroup in settings.DclStatusRules
                         .Where(rule => rule.UsesSharedContest)
                         .GroupBy(rule => (rule.AbilityId, rule.ActionType, rule.NormalizedContestGroup)))
            {
                var members = sharedGroup.ToArray();
                string groupScope = $"DclStatusRules.group.{sharedGroup.Key.AbilityId}:{sharedGroup.Key.ActionType}:{sharedGroup.Key.NormalizedContestGroup}";
                if (members.Length < 2)
                    report.Error(groupScope, "a shared status contest requires at least two member bits.");
                if (members.Select(rule => rule.NormalizedContestMode).Distinct().Count() != 1)
                    report.Error(groupScope, "every shared contest member must use the same ContestMode.");
                if (members.Select(rule => (rule.ResistanceFormula ?? "").Trim()).Distinct(StringComparer.Ordinal).Count() != 1)
                    report.Error(groupScope, "every shared contest member must use the same ResistanceFormula because the group rolls once.");
            }

            foreach (var suppressedGroup in settings.DclStatusRules
                         .Where(rule => rule.NativeRiderSuppressedByData && rule.UsesSharedContest)
                         .GroupBy(rule => rule.AbilityId))
            {
                if (!DclStatusGroupedCarrier.TryGetSuppressedDataBits(suppressedGroup.Key, out var requiredBits))
                    continue;
                var ownedBits = suppressedGroup
                    .Select(rule => new DclNativeStatusBit(rule.StatusByteIndex, (byte)rule.StatusMask))
                    .ToHashSet();
                var missing = requiredBits.Where(bit => !ownedBits.Contains(bit)).ToArray();
                var groupNames = suppressedGroup.Select(rule => rule.NormalizedContestGroup).Distinct().ToArray();
                if (missing.Length > 0)
                {
                    string missingText = string.Join(", ", missing.Select(bit => $"byte {bit.ByteIndex}/0x{bit.Mask:X2}"));
                    report.Error(
                        "DclStatusRules",
                        $"grouped data carrier ability {suppressedGroup.Key} lacks managed ownership for {missingText}; every removed rider bit must be replaced.");
                }
                if (groupNames.Length != 1 || groupNames[0].Length == 0)
                    report.Error(
                        "DclStatusRules",
                        $"grouped data carrier ability {suppressedGroup.Key} must place every removed rider bit in one nonempty ContestGroup.");
            }

            foreach (var conditionalGroup in settings.DclStatusRules
                         .Where(rule => rule.NativeRiderSuppressedByData &&
                                        DclStatusConditionalCarrier.TryGetRequiredBit(rule.AbilityId, out _))
                         .GroupBy(rule => rule.AbilityId))
            {
                DclStatusConditionalCarrier.TryGetRequiredBit(conditionalGroup.Key, out var requiredBit);
                if (conditionalGroup.Count() != 1 ||
                    conditionalGroup.Single().StatusByteIndex != requiredBit.ByteIndex ||
                    conditionalGroup.Single().StatusMask != requiredBit.Mask)
                    report.Error("DclStatusRules", $"conditional data carrier ability {conditionalGroup.Key} requires exactly one managed byte {requiredBit.ByteIndex}/0x{requiredBit.Mask:X2} rule.");
            }

            foreach (var producerGroup in settings.DclStatusRules
                         .Where(rule => rule.NativeRiderReplacedPostCalc)
                         .GroupBy(rule => rule.AbilityId))
            {
                if (abilityCatalog is null || !abilityCatalog.Loaded ||
                    !abilityCatalog.TryGet(producerGroup.Key, out var ability))
                    continue;
                var rules = producerGroup.ToArray();
                if (!DclStatusConditionalProducer.TryValidateRules(ability, rules, out string producerError))
                    report.Error("DclStatusRules", $"post-calc producer ability {producerGroup.Key}: {producerError}");

                var matchingPolicies = settings.DclStatusRules
                    .Where(rule => rule.AbilityId == producerGroup.Key)
                    .Select(rule => rule.NativeRiderPolicy)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (matchingPolicies.Length != 1)
                    report.Error("DclStatusRules", $"post-calc producer ability {producerGroup.Key} cannot mix native-rider policies.");
            }

            if (settings.DclStatusRules.Any(rule => rule.NativeRiderReplacedPostCalc) &&
                settings.StagedBundleProbeRva != DclCalcProvenance.OuterSweepReturnRva)
                report.Error("StagedBundleProbeRva", $"post-calc status production requires the proven outer-sweep completion RVA 0x{DclCalcProvenance.OuterSweepReturnRva:X}.");

            report.Warn("DclStatusControlEnabled", "suppressed-by-data rules require the matching fail-closed action-data build; retained-as-carrier rules leave their approved rider intact; replaced-post-calc rules preserve native forecast/AI behavior but replace the complete execution packet at the proven outer-sweep boundary. Equipment immunity remains an automatic resist.");
        }
        if (settings.DclInstantKoControlEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclInstantKoControlEnabled", "instant KO requires DclPipelineEnabled and the pre-clamp staged-debit channel.");
            if (!settings.DclHitControlEnabled || !settings.DclMissOutputControlEnabled || !settings.DclMissSelectorOutcomeEnabled)
                report.Error("DclInstantKoControlEnabled", "instant KO requires authored connect/miss delivery through DclHitControlEnabled, DclMissOutputControlEnabled, and DclMissSelectorOutcomeEnabled.");
            if (settings.PreClampDamageRewriteLogOnly)
                report.Error("DclInstantKoControlEnabled", "PreClampDamageRewriteLogOnly prevents engine-owned lethal debit delivery.");
            if (settings.DclInstantKoRules.Count == 0)
                report.Error("DclInstantKoRules", "DclInstantKoControlEnabled requires at least one exact per-ability rule.");

            var seenInstantKoRules = new HashSet<(int AbilityId, int ActionType)>();
            for (int i = 0; i < settings.DclInstantKoRules.Count; i++)
            {
                var rule = settings.DclInstantKoRules[i];
                string scope = $"DclInstantKoRules.{RuleName(rule.Name, i + 1)}";
                if (rule.AbilityId is < 0 or > 65535)
                    report.Error(scope, "AbilityId must identify one exact action within 0..65535.");
                if (rule.ActionType is < -1 or > 255)
                    report.Error(scope, "ActionType must be -1 (any) or a byte within 0..255.");
                if (string.IsNullOrWhiteSpace(rule.ResistanceFormula))
                    report.Error(scope, "ResistanceFormula is required and supplies the target number for the 3d6 resistance roll.");
                if (!rule.NativeKoSuppressedByData)
                    report.Error(scope, "NativeKoSuppressedByData must be true only after the ability's native KO/Crystal rider is removed in action data; otherwise a resisted DCL roll could still kill.");
                if (!seenInstantKoRules.Add((rule.AbilityId, rule.ActionType)))
                    report.Error(scope, "duplicate AbilityId/ActionType instant-KO ownership is not allowed.");
            }

            report.Warn("DclInstantKoControlEnabled", "successful rules deliver a lethal staged HP debit and let native HP apply own KO; resisted/immune rules never clear or set the Dead bit. Every owned ability requires a data-side native KO suppression before this switch is safe.");
        }
        if (settings.DclReactionMaxLogs < 0)
            report.Error("DclReactionMaxLogs", "DclReactionMaxLogs must be nonnegative.");
        if (settings.DclReactionTaxonomyEnabled)
        {
            if (!settings.DclPipelineEnabled)
                report.Error("DclReactionTaxonomyEnabled", "reaction taxonomy requires DclPipelineEnabled and the shared calc-entry hook.");
            if (settings.DclReactionRules.Count == 0)
                report.Error("DclReactionRules", "DclReactionTaxonomyEnabled requires at least one exact Reaction ability rule.");
            if (settings.DclReactionCalcExitRva <= 0)
                report.Error("DclReactionCalcExitRva", "the computeActionResult exit RVA must be positive.");
            if (string.IsNullOrWhiteSpace(settings.DclReactionCalcExitExpectedBytes))
                report.Error("DclReactionCalcExitExpectedBytes", "an expected-byte guard is required for the Brave restore tail.");
            if (settings.BraveOverrideEnabled)
                report.Error("DclReactionTaxonomyEnabled", "persistent BraveOverrideEnabled conflicts with scoped reaction-chance virtualization.");
            if (settings.ReactionChanceControlEnabled)
                report.Error("DclReactionTaxonomyEnabled", "global ReactionChanceControlEnabled would override taxonomy chances at the real-code reaction sites.");

            var seenReactionIds = new HashSet<int>();
            for (int i = 0; i < settings.DclReactionRules.Count; i++)
            {
                var rule = settings.DclReactionRules[i];
                string scope = $"DclReactionRules.{RuleName(rule.Name, i + 1)}";
                if (rule.AbilityId is < 422 or > 453)
                    report.Error(scope, "AbilityId must be an exact native Reaction record within 422..453.");
                if (!seenReactionIds.Add(rule.AbilityId))
                    report.Error(scope, "duplicate Reaction ability ownership is not allowed.");
                if (!DclReactions.IsSupportedMode(rule.Mode))
                    report.Error(scope, "Mode must be courage, caution, or neutral.");
                if (rule.FlatChance is < -1 or > 100)
                    report.Error(scope, "FlatChance must be -1 (unused) or 0..100.");
                if (rule.NormalizedMode == "neutral" && string.IsNullOrWhiteSpace(rule.ChanceFormula) && rule.FlatChance < 0)
                    report.Error(scope, "neutral reactions require FlatChance 0..100 or ChanceFormula.");
                if (rule.NormalizedMode is "courage" or "caution" && rule.FlatChance >= 0 && string.IsNullOrWhiteSpace(rule.ChanceFormula))
                    report.Warn(scope, "FlatChance is ignored by the default courage/caution curve; use ChanceFormula to author a custom curve.");
                if (!string.IsNullOrWhiteSpace(rule.ConditionFormula))
                    report.Warn(scope,
                        settings.DclSyntheticReactionEnabled && rule.AbilityId == settings.DclSyntheticReactionCarrierId
                            ? "ConditionFormula is evaluated at the synthetic Reaction's configured trigger because its carrier has no native dispatcher branch. It reserves a producer request but does not consume cadence before pass-2 acceptance."
                            : "ConditionFormula can suppress an existing native reaction evaluation by setting its chance to zero. It cannot create a new trigger window the native dispatcher never evaluates, and it does not consume cadence state.");
                if (rule.VmInternalAvoidance)
                    report.Warn(scope, "VmInternalAvoidance reads the exact Reaction id from the calc-entry order record and temporarily virtualizes defender Brave only inside computeActionResult, covering equipped or innate VM-owned avoidance such as Shirahadori. This path is Strong offline and requires a live vertical slice.");
            }

            report.Warn("DclReactionTaxonomyEnabled", "the four real-code reaction gates receive the exact evaluated Reaction id and an authored chance; VmInternalAvoidance rules additionally read the calc-entry order-record id, substitute defender Brave inside computeActionResult, and restore it at the guarded sole exit. Native effects and RNG remain engine-owned. The hybrid path is Strong offline and requires one live vertical slice before deployment.");
        }
        if (settings.DclMissKindValue is < 0 or > 255)
            report.Error("DclMissKindValue", "DclMissKindValue must be within 0..255.");
        if (settings.DclMissOutputControlEnabled)
        {
            if (!settings.DclHitControlEnabled)
                report.Error("DclMissOutputControlEnabled", "output-control miss delivery rides the hit-control decision layer (roll + cache + calc-entry stamp); enable DclHitControlEnabled.");
            if (settings.DclMissKindRva <= 0)
                report.Error("DclMissKindRva", "DclMissKindRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.DclMissKindExpectedBytes))
                report.Error("DclMissKindExpectedBytes", "Expected bytes are required for the result-kind commit hook.");
            report.Warn("DclMissOutputControlEnabled",
                "installs a THIRD managed hook at the result-kind commit site (DclMissKindRva, default 0x205B38): the VM always connects (both outcomes get the all-zero evade stamp), the pre-clamp zeroes the staged debit on a cached MISS, and the hook flips the committed outcome-kind byte +0x1C0 to DclMissKindValue. The site is Strong (static disassembly 2026-07-04) but UNPROVEN live until LT9; the double AOB guard disables the whole feature on any byte mismatch.");
            if (settings.DclMissSelectorOutcomeEnabled && string.IsNullOrWhiteSpace(settings.ResultSelectorProbeExpectedBytes))
                report.Error("DclMissSelectorOutcomeEnabled", "the selector outcome hook requires ResultSelectorProbeExpectedBytes for its AOB guard.");
            if (settings.DclMissSelectorOutcomeEnabled)
                report.Warn("DclMissSelectorOutcomeEnabled", "a cached DCL miss is delivered through selector 0x205210 by forcing kind=miss and +0x1BE=0 (evade/no-damage branch).");
            if (settings.DclMissSuppressReactionsEnabled)
                report.Warn("DclMissSuppressReactionsEnabled", "the four real-code Brave-gate reaction rolls preserve their natural chance on DCL hits and force chance 0 on cached DCL misses.");
        }
        else
        {
            if (settings.DclMissSelectorOutcomeEnabled)
                report.Error("DclMissSelectorOutcomeEnabled", "requires DclMissOutputControlEnabled.");
            if (settings.DclMissSuppressReactionsEnabled)
                report.Error("DclMissSuppressReactionsEnabled", "requires DclMissOutputControlEnabled.");
        }
        ValidateDclMissPresentation(settings, report);
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
        ValidateDclCounterPathProbe(settings, report);
        ValidateDclReactionCommitProbe(settings, report);
        ValidateDclReactionPreSelectorProbe(settings, report);
        ValidateDclReactionMaterializationProbe(settings, report);
        ValidateDclReactionOrderRewrite(settings, report);
        ValidateDclSyntheticReaction(settings, report);
        ValidateDclReactionEffectProbe(settings, report);
        ValidateDclAutoPotionConsumeProbe(settings, report);
        ValidateDclWeaponLineOfFireProbe(settings, report);
        ValidateDclCalcProvenanceProbe(settings, report);
        if (settings.PreviewHitPctControlEnabled || settings.DclPreviewHitPctEnabled)
        {
            if (settings.PreviewHitPctRva <= 0)
                report.Error("PreviewHitPctRva", "PreviewHitPctRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.PreviewHitPctExpectedBytes))
                report.Error("PreviewHitPctExpectedBytes", "Expected bytes are required for the preview hit% hook.");
        }
        if (settings.PreviewHitPctControlEnabled)
        {
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
                report.Error("StagedBundleForceAilment", "Legacy auxiliary-word force must be -1 or a word 0..65535.");
            if (settings.StagedBundleForceDmg is < -1 or > 0xFFFF)
                report.Error("StagedBundleForceDmg", "Force dmg must be -1 or a word 0..65535.");
            if (settings.StagedBundleForceAilment >= 0)
                report.Error("StagedBundleForceAilment", "+0x1A8 is an item/inventory side-effect id, not a staged status id; this legacy write is retired and blocked.");
            if (settings.StagedBundleForceApplyMask >= 0)
                report.Error("StagedBundleForceApplyMask", "+0x1D0 bit 0x08 gates the item/inventory side-effect path, not status application; this legacy write is retired and blocked.");
            if (settings.StagedBundleForceTargetCharId >= 0 &&
                (settings.StagedBundleForceKind >= 0 || settings.StagedBundleForceAilment >= 0 ||
                 settings.StagedBundleForceApplyMask >= 0 || settings.StagedBundleForceDmg >= 0 ||
                 settings.StagedBundleForceResFlag >= 0))
                report.Warn("StagedBundleProbeEnabled", "staged-bundle forcing overwrites the computed effect result before apply; use for controlled LT4 proof captures only.");
        }
        ValidateDclStatusOutputControl(settings, report);
        ValidateStatusPoke(settings, report);
        ValidateMovePoke(settings, report);
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
            if (settings.DclMissSuppressReactionsEnabled)
                report.Error("ReactionChanceControlEnabled", "cannot share the four reaction roll sites with DclMissSuppressReactionsEnabled; disable the global reaction chance control.");
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
            report.Warn("EvadeCopierOverrideEnabled", "airtight evade override detours the verified equip/refresh copier tails (0x2854DB/0x3966BF) and over-stamps the defender's evade bytes every refresh; all=0 forces HIT, one source high forces that avoid type. Retires the EvadeOverride poll.");
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
        if (settings.PreClampDamageRewriteEnabled || settings.DclPipelineEnabled)
        {
            if (settings.PreClampDamageRewriteEnabled)
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
                !settings.PreClampManagedCallbackEnabled &&
                !settings.DclPipelineEnabled)
                report.Error("PreClampDamageRewrite", "Non-log-only mode requires a forced debit, forced credit, PreClampFormulaPlanEnabled, PreClampManagedCallbackEnabled, or DclPipelineEnabled.");
            if (settings.PreClampManagedCallbackEnabled || settings.DclPipelineEnabled)
            {
                report.Warn("PreClampManagedCallbackEnabled", "managed callback calls C# from the native pre-clamp hook; use only for a tightly guarded ABI proof until live-stable.");
                if (settings.PreClampManagedCallbackForcedDebit < 0 &&
                    !settings.PreClampManagedCallbackActorFormulaEnabled &&
                    !(settings.DclPipelineEnabled &&
                      (!string.IsNullOrWhiteSpace(settings.DclDamageFormula) ||
                       !string.IsNullOrWhiteSpace(settings.DclHealingFormula) ||
                       !string.IsNullOrWhiteSpace(settings.DclMpDebitFormula) ||
                       !string.IsNullOrWhiteSpace(settings.DclMpCreditFormula) || settings.DclStatusControlEnabled ||
                       settings.DclInstantKoControlEnabled ||
                       settings.DclPhysicalContestEnabled || settings.DclMagicEvadeEnabled || settings.DclPreviewAmountEnabled ||
                       settings.DclReactionTaxonomyEnabled)))
                    report.Error("PreClampManagedCallback", "Managed callback requires a forced debit, PreClampManagedCallbackActorFormulaEnabled, or a DCL HP/MP/status/KO/hit/reaction outcome.");
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

    // OBSERVE-ONLY DCL counter-path probe: an ExecuteFirst hook at fn entry 0x30C700 (Strong-static
    // candidate for the counter/reaction result-staging path that bypasses computeActionResult 0x3099AC,
    // per work/1783184308-dcl-miss-consumption-and-counter-path.md §Q2). It logs [DCL-CTRPATH] with the
    // record ptr, target idx, result bytes and HP; it never writes engine memory.
    private static void ValidateDclCounterPathProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclCounterPathProbeMaxLogs < 0)
            report.Error("DclCounterPathProbeMaxLogs", "DclCounterPathProbeMaxLogs must be nonnegative.");
        if (settings.DclCounterPathProbeEnabled)
        {
            if (settings.DclCounterPathProbeRva <= 0)
                report.Error("DclCounterPathProbeRva", "DclCounterPathProbeRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.DclCounterPathProbeExpectedBytes))
                report.Error("DclCounterPathProbeExpectedBytes", "Expected bytes are required for the counter-path probe (they double as the AOB guard).");
            report.Warn("DclCounterPathProbeEnabled",
                "observe-only RE probe at the Strong-static counter result-staging site (fn 0x30C700): logs [DCL-CTRPATH] record/targetIdx/e8/e9/hp per fire, no writes to game memory. Counter-specificity is a Hypothesis this test settles (also fires for normal actions => it is a shared commit). Use only for short controlled captures.");
        }
    }

    // Reaction action-queue commit probe/control. Pass 2 is the live-proven native Reaction commit;
    // guarded static actor-construction boundaries also cover passes 0 and 1. Pass 1 carries ordinary
    // actions, while pass 0 remains family-unclassified. Only pass 2 hosts controls.
    private static void ValidateDclReactionCommitProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclReactionCommitProbeMaxLogs < 0)
            report.Error("DclReactionCommitProbeMaxLogs", "DclReactionCommitProbeMaxLogs must be nonnegative.");
        if (settings.DclReactionCommitProbeEnabled)
        {
            if (settings.DclReactionCommitProbeRva <= 0)
                report.Error("DclReactionCommitProbeRva", "DclReactionCommitProbeRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.DclReactionCommitProbeExpectedBytes))
                report.Error("DclReactionCommitProbeExpectedBytes", "Expected bytes are required for the reaction-commit probe (they double as the AOB guard).");
            report.Warn("DclReactionCommitProbeEnabled",
                settings.DclReactionActionReplacementEnabled || settings.DclReactionRetargetEnabled
                    ? "guarded RE hooks cover the proven pass-2 native Reaction commit plus guarded pass-0/1 queue boundaries; only native Reaction ids with agreeing actor copies emit [DCL-REACTION-COMMIT], ordinary pass noise is tagged separately, and only pass 2 hosts the action-replacement/retarget controls."
                    : "observe-only RE hooks cover the proven pass-2 native Reaction commit plus guarded pass-0/1 queue boundaries: native Reaction ids with agreeing actor copies emit [DCL-REACTION-COMMIT], ordinary pass noise is tagged separately, and no cadence or game memory is changed. Use only for a short controlled capture.");
        }

        if (settings.DclReactionActionReplacementEnabled)
        {
            if (!settings.DclReactionCommitProbeEnabled)
                report.Error("DclReactionActionReplacementEnabled", "reaction action replacement requires DclReactionCommitProbeEnabled because it rides the guarded commit hook.");
            if (settings.DclReactionActionReplacementCarrierId is < 422 or > 453)
                report.Error("DclReactionActionReplacementCarrierId", "carrier id must be a native Reaction id in 422..453.");
            if (settings.DclReactionActionReplacementAbilityId is < 0 or > 511)
                report.Error("DclReactionActionReplacementAbilityId", "replacement action id must be within 0..511 (0 is Basic Attack).");
            if (settings.DclReactionActionReplacementMinTargetCount is < 1 or > 8)
                report.Error("DclReactionActionReplacementMinTargetCount", "minimum captured target count must be within 1..8.");
            if (settings.DclReactionActionReplacementMaxWrites is < 1 or > 32)
                report.Error("DclReactionActionReplacementMaxWrites", "maximum live replacement writes must be within 1..32.");
            if (!settings.DclReactionActionReplacementLogOnly)
                report.Error("DclReactionActionReplacementEnabled", "live replacement at the pass-2 commit is retired: carrier delivery overwrites actor+0x142 afterward. Use log-only until a post-materialization/pre-execution boundary is bound.");
            if (settings.DclReactionActionReplacementLogOnly)
                report.Warn("DclReactionActionReplacementEnabled",
                    "reaction action replacement is LOG-ONLY: matching commits report would-write intent but actor+0x142 remains native.");
        }

        if (settings.DclReactionRetargetEnabled)
        {
            if (!settings.DclReactionCommitProbeEnabled)
                report.Error("DclReactionRetargetEnabled", "reaction retarget requires DclReactionCommitProbeEnabled because it rides the guarded pass-2 commit hook.");
            if (settings.DclReactionRetargetCarrierId is < 422 or > 453)
                report.Error("DclReactionRetargetCarrierId", "retarget carrier id must be a native Reaction id in 422..453.");
            if (settings.DclReactionRetargetMaxWrites is < 1 or > 32)
                report.Error("DclReactionRetargetMaxWrites", "maximum live retarget writes must be within 1..32.");
            if (!settings.DclReactionRetargetLogOnly)
                report.Error("DclReactionRetargetEnabled", "live retarget at the pass-2 commit is retired: the target list can be empty/stale and carrier delivery overwrites it afterward. Use log-only until a post-materialization/pre-execution boundary is bound.");
            if (settings.DclReactionRetargetLogOnly)
                report.Warn("DclReactionRetargetEnabled",
                    "reaction retarget is LOG-ONLY: a matching pass-2 commit reports the incoming source as the candidate target without changing actor+0x1A9/+0x1AA.");
        }
    }

    private static void ValidateDclReactionPreSelectorProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclReactionPreSelectorProbeMaxLogs < 0)
            report.Error("DclReactionPreSelectorProbeMaxLogs", "DclReactionPreSelectorProbeMaxLogs must be nonnegative.");
        if (settings.DclReactionPreSelectorProbeEnabled)
        {
            if (settings.DclReactionPreSelectorProbeRva <= 0)
                report.Error("DclReactionPreSelectorProbeRva", "DclReactionPreSelectorProbeRva must be positive.");
            if (string.IsNullOrWhiteSpace(settings.DclReactionPreSelectorProbeExpectedBytes))
                report.Error("DclReactionPreSelectorProbeExpectedBytes", "Expected bytes are required for the pass-2 pre-selector probe.");
            report.Warn("DclReactionPreSelectorProbeEnabled",
                settings.DclSyntheticReactionEnabled
                    ? "pass-2 pre-selector probe/control snapshots all candidate words and consumes the synthetic Reaction mailbox through a dynamic per-defender producer; DclSyntheticReactionLogOnly controls whether the configured carrier is staged."
                    : settings.DclReactionProducerEnabled
                        ? "pass-2 pre-selector probe/control snapshots all candidate words and hosts the separately guarded reaction producer."
                        : "observe-only pass-2 pre-selector probe: snapshots source/eval globals, incoming actor, and all 21 unit+0x1CE candidate words before native consumption. It never stages a carrier or mutates game memory.");
        }

        if (!settings.DclReactionProducerEnabled)
            return;

        if (!settings.DclReactionPreSelectorProbeEnabled)
            report.Error("DclReactionProducerEnabled", "reaction producer requires DclReactionPreSelectorProbeEnabled because it rides the AOB-guarded pass-2 hook.");
        if (settings.DclReactionProducerCarrierId is < 422 or > 453)
            report.Error("DclReactionProducerCarrierId", "producer carrier id must be a native Reaction id in 422..453.");
        if (settings.DclReactionProducerUnitIndex is < 0 or > 20)
            report.Error("DclReactionProducerUnitIndex", "producer battle-unit index must be within 0..20.");
        if (settings.DclReactionProducerMaxWrites is < 1 or > 32)
            report.Error("DclReactionProducerMaxWrites", "maximum live producer writes must be within 1..32.");
        if (!settings.DclReactionProducerLogOnly && !settings.DclReactionCommitProbeEnabled)
            report.Error("DclReactionProducerEnabled", "live reaction production requires DclReactionCommitProbeEnabled so the resulting accepted queue pass is captured.");

        report.Warn("DclReactionProducerEnabled",
            settings.DclReactionProducerLogOnly
                ? "reaction producer is LOG-ONLY: an active configured unit with an empty +0x1CE slot reports would-stage intent without mutation."
                : "reaction producer is LIVE: it stages the configured carrier only into an active configured unit whose +0x1CE slot is empty. Keep MaxWrites=1 and require LT23 pass ownership before use.");
    }

    private static void ValidateDclReactionEffectProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclReactionEffectProbeMaxLogs < 0)
            report.Error("DclReactionEffectProbeMaxLogs", "DclReactionEffectProbeMaxLogs must be nonnegative.");
        if (!settings.DclReactionEffectProbeEnabled)
            return;
        if (settings.DclReactionEffectProbeRva <= 0)
            report.Error("DclReactionEffectProbeRva", "DclReactionEffectProbeRva must be positive.");
        if (string.IsNullOrWhiteSpace(settings.DclReactionEffectProbeExpectedBytes))
            report.Error("DclReactionEffectProbeExpectedBytes", "Expected bytes are required for the state-0x2C reaction effect probe.");
        report.Warn("DclReactionEffectProbeEnabled",
            "observe-only state-0x2C probe after the VM execution workers: captures the executed actor, presentation/action ids, source, and target list before cleanup. It never applies an effect or consumes cadence.");
    }

    private static void ValidateDclSyntheticReaction(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclSyntheticReactionMaxLogs < 0)
            report.Error("DclSyntheticReactionMaxLogs", "synthetic Reaction log cap must be nonnegative.");
        if (!settings.DclSyntheticReactionEnabled)
            return;

        const string scope = "DclSyntheticReactionEnabled";
        int carrierId = settings.DclSyntheticReactionCarrierId;
        if (carrierId is < 422 or > 453)
            report.Error("DclSyntheticReactionCarrierId", "synthetic Reaction carrier id must be a native Reaction id in 422..453.");
        if (settings.DclSyntheticReactionTrigger != "successful-hit-survivor")
            report.Error("DclSyntheticReactionTrigger", "the only currently owned synthetic trigger is 'successful-hit-survivor'.");
        if (!settings.DclPipelineEnabled)
            report.Error(scope, "synthetic Reaction requires DclPipelineEnabled for exact incoming hit/action identity.");
        if (!settings.DclReactionTaxonomyEnabled)
            report.Error(scope, "synthetic Reaction requires DclReactionTaxonomyEnabled for its managed chance rule.");

        var carrierRules = (settings.DclReactionRules ?? [])
            .Where(candidate => candidate.AbilityId == carrierId)
            .ToArray();
        if (carrierRules.Length != 1)
            report.Error(scope, "synthetic Reaction requires exactly one DclReactionRules entry for its configured carrier.");

        if (!settings.DclReactionPreSelectorProbeEnabled)
            report.Error(scope, "synthetic Reaction requires the exact-byte-guarded pass-2 pre-selector producer boundary.");
        if (!settings.DclReactionCommitProbeEnabled)
            report.Error(scope, "synthetic Reaction requires pass-2 commit capture; cadence is forbidden before acceptance.");
        if (settings.DclReactionProducerEnabled)
            report.Error(scope, "disable the fixed-index reaction test producer; the synthetic transaction owns the dynamic per-defender producer on the same hook.");

        if (!settings.DclReactionMaterializationProbeEnabled || !settings.DclReactionOrderRewriteEnabled)
            report.Error(scope, "synthetic Reaction requires the guarded accepted-order rewrite boundary.");
        else
        {
            if (settings.DclReactionOrderRewriteCarrierId != carrierId)
                report.Error(scope, "synthetic Reaction and accepted-order rewrite must use the same exact carrier id.");
            if (!settings.DclSyntheticReactionLogOnly && settings.DclReactionOrderRewriteLogOnly)
                report.Error(scope, "live synthetic Reaction requires a live accepted-order rewrite.");
        }

        if (settings.DclSyntheticReactionForcedRoll is < -1 or > 99)
            report.Error("DclSyntheticReactionForcedRoll", "synthetic Reaction forced roll must be -1 (RNG) or 0..99.");
        if (settings.DclSyntheticReactionMaxWrites is < 1 or > 32)
            report.Error("DclSyntheticReactionMaxWrites", "synthetic Reaction live producer writes must be bounded within 1..32.");

        report.Warn(scope, settings.DclSyntheticReactionLogOnly
            ? "synthetic Reaction is LOG-ONLY: the managed rule and exact dynamic producer intent are audited without staging the configured carrier."
            : "synthetic Reaction is LIVE and bounded: an exact equipped-carrier owner that survives a successful incoming hit may reserve and stage the carrier; accepted-order rewriting owns delivery, and exact pass-2 acceptance consumes cadence once.");
    }

    private static void ValidateDclReactionMaterializationProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclReactionMaterializationProbeMaxLogs < 0)
            report.Error("DclReactionMaterializationProbeMaxLogs", "DclReactionMaterializationProbeMaxLogs must be nonnegative.");
        if (!settings.DclReactionMaterializationProbeEnabled)
            return;
        if (settings.DclReactionMaterializationProbeRva <= 0)
            report.Error("DclReactionMaterializationProbeRva", "DclReactionMaterializationProbeRva must be positive.");
        if (string.IsNullOrWhiteSpace(settings.DclReactionMaterializationProbeExpectedBytes))
            report.Error("DclReactionMaterializationProbeExpectedBytes", "Expected bytes are required for the accepted Reaction materialization probe.");
        report.Warn("DclReactionMaterializationProbeEnabled",
            settings.DclReactionOrderRewriteEnabled
                ? "accepted pass-2 audit hook after carrier-specific order materialization and before actor construction: snapshots exact Reaction/reactor/source identity and all 20 bytes of unit+0x1A0 around the separately validated guarded rewrite controller."
                : "observe-only accepted pass-2 probe after carrier-specific order materialization and before actor construction: snapshots exact Reaction/reactor/source identity and all 20 bytes of unit+0x1A0. It never changes the order or other game memory.");
    }

    private static void ValidateDclReactionOrderRewrite(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (!settings.DclReactionOrderRewriteEnabled)
            return;

        if (!settings.DclReactionMaterializationProbeEnabled)
            report.Error("DclReactionOrderRewriteEnabled",
                "accepted-order rewrite requires DclReactionMaterializationProbeEnabled because it shares that exact-byte-guarded boundary and audit ring.");
        if (settings.DclReactionOrderRewriteCarrierId is < 422 or > 453)
            report.Error("DclReactionOrderRewriteCarrierId", "carrier id must be a native Reaction id in 422..453.");
        if (!settings.DclReactionOrderRewriteActionEnabled && !settings.DclReactionOrderRewriteRetargetSource)
            report.Error("DclReactionOrderRewriteEnabled", "enable action replacement, source retargeting, or both.");
        if (settings.DclReactionOrderRewriteActionEnabled)
        {
            if (settings.DclReactionOrderRewriteActionType is < 0 or > 255)
                report.Error("DclReactionOrderRewriteActionType", "replacement action type must be within 0..255.");
            if (settings.DclReactionOrderRewriteAbilityId is < 0 or > 511)
                report.Error("DclReactionOrderRewriteAbilityId", "replacement ability id must be within 0..511.");
        }
        if (settings.DclReactionOrderRewriteExpectedActionType is < -1 or > 255)
            report.Error("DclReactionOrderRewriteExpectedActionType", "expected native action type must be -1 (any) or 0..255.");
        if (settings.DclReactionOrderRewriteExpectedAbilityId is < -1 or > 511)
            report.Error("DclReactionOrderRewriteExpectedAbilityId", "expected native ability id must be -1 (any) or 0..511.");
        if (!settings.DclReactionOrderRewriteLogOnly &&
            (settings.DclReactionOrderRewriteExpectedActionType < 0 || settings.DclReactionOrderRewriteExpectedAbilityId < 0))
            report.Error("DclReactionOrderRewriteEnabled",
                "live accepted-order writes require exact expected native action type and ability id guards; -1 is allowed only in log-only mode.");
        if (settings.DclReactionOrderRewriteMaxWrites is < 1 or > 32)
            report.Error("DclReactionOrderRewriteMaxWrites", "maximum accepted-order writes must be within 1..32.");

        report.Warn("DclReactionOrderRewriteEnabled",
            settings.DclReactionOrderRewriteLogOnly
                ? "accepted-order rewrite is LOG-ONLY: matching carrier orders are audited at the proven post-materialization/pre-actor boundary without changing game memory."
                : "accepted-order rewrite is LIVE: exact carrier/original-order guards and the bounded write cap apply before actor construction; use only with a controlled fixture and matching effect probe.");
    }

    private static void ValidateDclAutoPotionConsumeProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclAutoPotionConsumeProbeMaxLogs < 0)
            report.Error("DclAutoPotionConsumeProbeMaxLogs", "DclAutoPotionConsumeProbeMaxLogs must be nonnegative.");
        if (!settings.DclAutoPotionConsumeProbeEnabled)
            return;
        if (settings.DclAutoPotionConsumeProbeRva <= 0)
            report.Error("DclAutoPotionConsumeProbeRva", "DclAutoPotionConsumeProbeRva must be positive.");
        if (string.IsNullOrWhiteSpace(settings.DclAutoPotionConsumeProbeExpectedBytes))
            report.Error("DclAutoPotionConsumeProbeExpectedBytes", "Expected bytes are required for the Auto-Potion consumption probe.");
        report.Warn("DclAutoPotionConsumeProbeEnabled",
            "observe-only shared item-consumption probe: records native item/count operands and order context. It never changes inventory or reaction state.");
    }

    private static void ValidateDclWeaponLineOfFireProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclWeaponLineOfFireProbeMaxLogs < 0)
            report.Error("DclWeaponLineOfFireProbeMaxLogs", "DclWeaponLineOfFireProbeMaxLogs must be nonnegative.");
        if (!settings.DclWeaponLineOfFireProbeEnabled)
            return;
        if (settings.DclWeaponLineOfFireArcRva <= 0)
            report.Error("DclWeaponLineOfFireArcRva", "DclWeaponLineOfFireArcRva must be positive.");
        if (settings.DclWeaponLineOfFireDirectRva <= 0)
            report.Error("DclWeaponLineOfFireDirectRva", "DclWeaponLineOfFireDirectRva must be positive.");
        if (settings.DclWeaponLineOfFireArcRva == settings.DclWeaponLineOfFireDirectRva)
            report.Error("DclWeaponLineOfFireProbeEnabled", "Arc and Direct line-of-fire probe RVAs must be distinct.");
        if (string.IsNullOrWhiteSpace(settings.DclWeaponLineOfFireExpectedBytes))
            report.Error("DclWeaponLineOfFireExpectedBytes", "Expected bytes are required for the weapon line-of-fire probe.");
        report.Warn("DclWeaponLineOfFireProbeEnabled",
            "observe-only post-resolver hooks: record Arc/Direct intended versus reached/intercepted unit indices. They never call a resolver or change targeting state.");
    }

    private static void ValidateDclCalcProvenanceProbe(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclCalcProvenanceProbeMaxLogs < 0)
            report.Error("DclCalcProvenanceProbeMaxLogs", "DclCalcProvenanceProbeMaxLogs must be nonnegative.");
        if (!settings.DclCalcProvenanceProbeEnabled)
            return;
        if (settings.CalcEntryProbeRva <= 0)
            report.Error("CalcEntryProbeRva", "The calc-provenance probe needs a positive CalcEntryProbeRva.");
        report.Warn("DclCalcProvenanceProbeEnabled",
            "observe-only calc-entry extension: captures caller/state provenance and never changes native state, formulas, outcomes, or DCL caches.");
    }

    // LEGACY STAGED-AUXILIARY probe/control. LT13 reclassified +0x1A8 as an item id and +0x1D0 bit
    // 0x08 as the item/inventory side-effect gate. Observation remains useful; every legacy write
    // mode is rejected because it can mutate item return/consumption behavior, not status output.
    private static void ValidateDclStatusOutputControl(RuntimeSettings settings, SettingsValidationReport report)
    {
        bool suppress = settings.DclStatusSuppressEnabled;
        bool forceId = settings.DclStatusForceId >= 0;
        bool forceRaw = settings.DclStatusForceValue >= 0;
        bool anyForce = forceId || forceRaw;

        if (settings.DclStatusForceId is < -1 or > 0xFFFF)
            report.Error("DclStatusForceId", "DclStatusForceId must be -1 (off) or a legacy auxiliary word 0..65535.");
        if (settings.DclStatusForceValue is < -1 or > 0xFF)
            report.Error("DclStatusForceValue", "DclStatusForceValue must be -1 (off) or a raw byte 0..255.");
        if (settings.DclStatusForceOffset is < 0 or > 0x1FF)
            report.Error("DclStatusForceOffset", "DclStatusForceOffset must be within the unit struct 0x0..0x1FF.");
        if (settings.DclStatusSuppressMask is < 0 or > 0xFF)
            report.Error("DclStatusSuppressMask", "DclStatusSuppressMask must be a byte 0..255.");
        if (settings.DclStatusResultFlagStatusBit is < 0 or > 0xFF)
            report.Error("DclStatusResultFlagStatusBit", "DclStatusResultFlagStatusBit must be a byte 0..255.");

        if (suppress && anyForce)
            report.Error("DclStatusOutputControl", "DclStatusSuppressEnabled and DclStatusForce* are mutually exclusive; a hit is either suppressed or forced, not both.");

        bool anyWriteMode = suppress || anyForce;
        if (anyWriteMode)
            report.Error("DclStatusOutputControl", "retired unsafe surface: +0x1A8/+0x1D0 control an item/inventory side effect, not status application. Use the durable status arrays (+0x1EF..+0x1F3 with +0x61..+0x65 mirrors) for status authority.");
        if (anyWriteMode && !settings.DclPipelineEnabled)
            report.Error("DclStatusOutputControl", "the legacy staged-auxiliary callback runs inside the DCL pre-clamp callback; enable DclPipelineEnabled.");
        if (settings.DclStatusStageProbeEnabled && !settings.DclPipelineEnabled)
            report.Error("DclStatusStageProbeEnabled", "the staged-auxiliary probe runs inside the DCL pre-clamp callback; enable DclPipelineEnabled.");
    }

    // DCL MISS PRESENTATION (LT10-C): render "Miss" instead of the "0" damage popup on a forced miss.
    // The forced-miss branch of the pre-clamp callback RMWs record+0x1D8 (clear bit 2 = damage-number
    // route, set the glyph bit) and writes the glyph kind to +0x1C0 / mirror +0x360. It rides the same
    // both-hooks output-control gate, so it needs DclMissOutputControlEnabled. Range-check the kind byte
    // and the glyph bit (draw fn 0x2667E0 enters the evade/miss-glyph stage on bits 0x10..0x18).
    private static void ValidateDclMissPresentation(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.DclMissPresentationKind is < 0 or > 255)
            report.Error("DclMissPresentationKind", "DclMissPresentationKind must be a glyph-kind byte 0..255.");
        if (settings.DclMissPresentationGlyphBit is < 0x10 or > 0x18)
            report.Error("DclMissPresentationGlyphBit", "DclMissPresentationGlyphBit must be within the evade/miss-glyph route 0x10..0x18.");

        if (!settings.DclMissPresentationEnabled)
            return;

        if (!settings.DclMissOutputControlEnabled)
            report.Error("DclMissPresentationEnabled", "miss presentation rides the forced-miss branch of the miss output-control path; enable DclMissOutputControlEnabled.");

        report.Warn("DclMissPresentationEnabled",
            "on a forced miss also RMWs the record's \"what-to-draw\" bitfield +0x1D8 (clears the whole draw-bit range 0..24 = number route + special popups, sets DclMissPresentationGlyphBit) and writes DclMissPresentationKind to the glyph-kind byte +0x1C0 and (when DclMissPresentationMirrorWrite is on, default) its mirror +0x360, so the miss renders as a Miss/evade glyph instead of a \"0\" popup. The ORDERING is UNPROVEN live (case A: our write survives to draw / case B: the VM populates +0x1D8/+0x1C0 AFTER our hook and clobbers it — the pres= d8/kind old->new log values distinguish which). The glyph-bit (0x16 class-Miss vs 0x17 generic, kind 0x06) and kind mapping are Hypothesis; both iterate via settings without a rebuild.");
    }

    // DIRECT STATUS POKE (LT10-B, outside actions): one-shot ADD/REMOVE on the durable status master.
    private static void ValidateStatusPoke(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.StatusPokeTargetCharId < -1 || settings.StatusPokeTargetCharId > 0xFF)
            report.Error("StatusPokeTargetCharId", "StatusPokeTargetCharId must be -1 (off) or a charId byte 0..255.");
        if (settings.StatusPokeTargetCharId < 0)
            return;

        string mode = settings.StatusPokeMode ?? "";
        if (!mode.Equals("add", StringComparison.OrdinalIgnoreCase) && !mode.Equals("remove", StringComparison.OrdinalIgnoreCase))
            report.Error("StatusPokeMode", "StatusPokeMode must be \"add\" (OR) or \"remove\" (AND-NOT).");
        if (settings.StatusPokeOffset < 0 || settings.StatusPokeOffset > 0x1FF)
            report.Error("StatusPokeOffset", "StatusPokeOffset must be within the unit struct 0x0..0x1FF.");
        if (settings.StatusPokeMask is < -1 or > 0xFF)
            report.Error("StatusPokeMask", "StatusPokeMask must be -1 (use StatusPokeValue) or a byte 0..255.");
        if (settings.StatusPokeValue is < -1 or > 0xFF)
            report.Error("StatusPokeValue", "StatusPokeValue must be -1 (use StatusPokeMask) or a byte 0..255.");
        int mask = settings.StatusPokeMask >= 0 ? settings.StatusPokeMask : settings.StatusPokeValue;
        if (mask < 0)
            report.Error("StatusPoke", "StatusPokeTargetCharId is set but neither StatusPokeMask nor StatusPokeValue supplies a bit mask.");
        if (settings.StatusPokeMaxWrites < 1 || settings.StatusPokeMaxWrites > 32)
            report.Error("StatusPokeMaxWrites", "StatusPokeMaxWrites must be within 1..32.");
        report.Info("StatusPoke", "one-shot guarded write to a unit's proven durable status master region (add = OR, remove = AND-NOT); confine with StatusPokeMaxWrites.");
    }

    // MOVE-WRITE POKE (LT10-B piggyback): one-shot write of the Move byte +0x42.
    private static void ValidateMovePoke(RuntimeSettings settings, SettingsValidationReport report)
    {
        if (settings.MovePokeTargetCharId < -1 || settings.MovePokeTargetCharId > 0xFF)
            report.Error("MovePokeTargetCharId", "MovePokeTargetCharId must be -1 (off) or a charId byte 0..255.");
        if (settings.MovePokeTargetCharId < 0)
            return;

        if (settings.MovePokeValue < 0 || settings.MovePokeValue > 32)
            report.Error("MovePokeValue", "MovePokeValue must be within 0..32 (Move is a small byte stat).");
        if (settings.MovePokeMaxWrites < 1 || settings.MovePokeMaxWrites > 32)
            report.Error("MovePokeMaxWrites", "MovePokeMaxWrites must be within 1..32.");
        report.Warn("MovePoke", "one-shot write of the Move stat +0x42. Move is Proven as a field but UNPROVEN as a WRITE until LT10-B; confine with MovePokeMaxWrites.");
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
        var dclContext = BuildDclFormulaContext(settings, catalog);

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
        foreach (var variable in settings.DclDerivedVariables)
            ValidateDerivedFormula(dclContext, variable, "DclDerivedVariables", report);
        ValidateFormula(settings.DclDamageFormula, dclContext, "DclDamageFormula", report, allowEmpty: true);
        ValidateFormula(settings.DclHealingFormula, dclContext, "DclHealingFormula", report, allowEmpty: true);
        ValidateFormula(settings.DclMpDebitFormula, dclContext, "DclMpDebitFormula", report, allowEmpty: true);
        ValidateFormula(settings.DclMpCreditFormula, dclContext, "DclMpCreditFormula", report, allowEmpty: true);
        ValidateFormula(settings.DclMpTrickleFormula, dclContext, "DclMpTrickleFormula", report, allowEmpty: true);
        if (settings.DclPreviewAmountEnabled)
        {
            var previewContext = BuildDclHitFormulaContext(settings, catalog);
            foreach (var variable in settings.DclDerivedVariables)
                ValidateDerivedFormula(previewContext, variable, "DclDerivedVariables", report);
            ValidateFormula(settings.DclPreviewDamageFormula, previewContext, "DclPreviewDamageFormula", report, allowEmpty: true);
            ValidateFormula(settings.DclPreviewHealingFormula, previewContext, "DclPreviewHealingFormula", report, allowEmpty: true);
        }
        if (settings.DclHitControlEnabled)
        {
            var hitContext = BuildDclHitFormulaContext(settings, catalog);
            foreach (var variable in settings.DclDerivedVariables)
                ValidateDerivedFormula(hitContext, variable, "DclDerivedVariables", report);
            if (settings.DclPhysicalContestEnabled)
            {
                // Pool-capacity formulas establish the guard state and therefore cannot depend on
                // guard.* themselves. The remaining guard surface becomes available only to the
                // defense-value/policy formulas evaluated after pool initialization at runtime.
                ValidateFormula(settings.DclPhysicalContestConditionFormula, hitContext, "DclPhysicalContestConditionFormula", report);
                ValidateFormula(settings.DclAttackSkillFormula, hitContext, "DclAttackSkillFormula", report);
                ValidateFormula(settings.DclParryUsesFormula, hitContext, "DclParryUsesFormula", report);
                ValidateFormula(settings.DclBlockUsesFormula, hitContext, "DclBlockUsesFormula", report);
                hitContext.Set("guard.parryRemaining", 1);
                hitContext.Set("guard.parryMax", 1);
                hitContext.Set("guard.blockRemaining", 1);
                hitContext.Set("guard.blockMax", 1);
                ValidateFormula(settings.DclDodgeFormula, hitContext, "DclDodgeFormula", report);
                ValidateFormula(settings.DclParryFormula, hitContext, "DclParryFormula", report);
                ValidateFormula(settings.DclBlockFormula, hitContext, "DclBlockFormula", report);
                ValidateFormula(settings.DclDefenseAllowedFormula, hitContext, "DclDefenseAllowedFormula", report);
                ValidateFormula(settings.DclDefenseModifierFormula, hitContext, "DclDefenseModifierFormula", report);
                ValidateFormula(settings.DclHitChanceFormula, hitContext, "DclHitChanceFormula", report);
            }
            else
            {
                ValidateFormula(settings.DclHitChanceFormula, hitContext, "DclHitChanceFormula", report);
            }
            if (settings.DclMagicEvadeEnabled)
            {
                ValidateFormula(settings.DclMagicEvadeConditionFormula, hitContext, "DclMagicEvadeConditionFormula", report);
                ValidateFormula(settings.DclMagicEvadeFormula, hitContext, "DclMagicEvadeFormula", report);
            }
        }
        for (int i = 0; i < settings.DclStatusRules.Count; i++)
        {
            var rule = settings.DclStatusRules[i];
            string scope = $"DclStatusRules.{RuleName(rule.Name, i + 1)}";
            dclContext.Set("status.byteIndex", rule.StatusByteIndex);
            dclContext.Set("status.mask", rule.StatusMask);
            dclContext.Set("status.add", rule.IsAdd ? 1 : 0);
            dclContext.Set("status.remove", rule.IsRemove ? 1 : 0);
            ValidateFormula(rule.ConditionFormula, dclContext, $"{scope}.ConditionFormula", report, allowEmpty: true);
            if (rule.IsAdd)
                ValidateFormula(rule.ResistanceFormula, dclContext, $"{scope}.ResistanceFormula", report);
        }
        for (int i = 0; i < settings.DclInstantKoRules.Count; i++)
        {
            var rule = settings.DclInstantKoRules[i];
            string scope = $"DclInstantKoRules.{RuleName(rule.Name, i + 1)}";
            ValidateFormula(rule.ConditionFormula, dclContext, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.ResistanceFormula, dclContext, $"{scope}.ResistanceFormula", report);
        }
        for (int i = 0; i < settings.DclReactionRules.Count; i++)
        {
            var rule = settings.DclReactionRules[i];
            string scope = $"DclReactionRules.{RuleName(rule.Name, i + 1)}";
            DclReactions.AddRuleVariables(dclContext, rule.AbilityId, 70, rule.FlatChance, rule.NormalizedMode);
            DclReactions.AddIncomingVariables(dclContext, new DclReactionIncomingContext(
                SourceValid: true,
                ActionValid: true,
                SourceIdx: 3,
                TargetIdx: 7,
                SourceCharId: 2,
                ActionType: 1,
                AbilityId: 0,
                HitDecisionKnown: true,
                Hit: false,
                PhysicalOutcome: (int)DclPhysicalOutcome.Defended,
                DefenseKind: (int)DclDefenseKind.Block,
                SourceTurnEpoch: 4,
                TargetTurnEpoch: 6,
                Origin: "validator"));
            ValidateFormula(rule.ConditionFormula, dclContext, $"{scope}.ConditionFormula", report, allowEmpty: true);
            ValidateFormula(rule.ChanceFormula, dclContext, $"{scope}.ChanceFormula", report, allowEmpty: true);
        }
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

    private static FormulaContext BuildDclFormulaContext(RuntimeSettings settings, ItemCatalog catalog)
    {
        var target = BuildSyntheticTarget(settings, catalog);
        var attacker = BuildSyntheticAttacker(settings, catalog);
        return FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings,
            catalog,
            AbilityCatalog.Empty("validator"),
            target,
            attacker,
            eventIndex: 1,
            eventSeed: 12345,
            actionType: 1,
            abilityId: 1,
            oldDebit: 20,
            oldCredit: 0);
    }

    private static FormulaContext BuildDclHitFormulaContext(RuntimeSettings settings, ItemCatalog catalog)
    {
        var target = BuildSyntheticTarget(settings, catalog);
        var attacker = BuildSyntheticAttacker(settings, catalog);
        return FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings,
            catalog,
            AbilityCatalog.Empty("validator"),
            target,
            attacker,
            eventIndex: 1,
            eventSeed: 12345,
            actionType: 1,
            abilityId: 1,
            oldDebit: 0,
            oldCredit: 0);
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
