using fftivc.generic.chronicle.codemod;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class Program
{
    private static int Main()
    {
        string root = FindRepoRoot(AppContext.BaseDirectory);
        string catalogPath = Path.Combine(root, "work", "item_catalog.csv");
        var catalog = ItemCatalog.Load(catalogPath);
        Check(catalog.Loaded, $"item catalog loaded: {catalog.Describe()}");
        string abilityCatalogPath = Path.Combine(root, "work", "wotl_ability_action_baseline.csv");
        var abilityCatalog = AbilityCatalog.Load(abilityCatalogPath);
        Check(abilityCatalog.Loaded, $"ability catalog loaded: {abilityCatalog.Describe()}");
        TestAbilityCatalog(abilityCatalog);
        TestDclActionContextCache();
        Check(
            ReferenceEquals(new BattleFormulaEngine(new RuntimeSettings(), catalog, abilityCatalog).AbilityCatalog, abilityCatalog),
            "formula engine should expose the supplied ability catalog");

        var targetRaw = new byte[0x180];
        targetRaw[0x70] = 172; // Leather Armor in work/item_catalog.csv.
        var target = new UnitSnapshot((nint)0x2000, 0x80, 5, 30, 50, 2, true, 4, 3, 6, 4, 3, 70, 60, targetRaw, 18, 30);
        UnitSnapshot TargetWithBodyItem(int itemId, nint ptr)
        {
            var raw = (byte[])targetRaw.Clone();
            raw[0x70] = (byte)itemId;
            return target with { Ptr = ptr, Raw = raw };
        }

        var attackerRaw = new byte[0x180];
        attackerRaw[0x44] = 99;
        attackerRaw[0x50] = 19; // Broadsword in work/item_catalog.csv.
        var attacker = new UnitSnapshot((nint)0x1000, 0x01, 6, 40, 40, 1, false, 12, 5, 7, 5, 4, 75, 65, attackerRaw, 12, 20);
        TestDclFormulaContextBuilder(catalog, abilityCatalog, target, attacker);

        TestPendingActionLethalClampMatch();
        TestImmediateActionCandidateScoring();

        var expressionSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "clamp(a.pa * 2 + vanillaDamage - equipmentDr, 0, t.hp)",
            ApplyEquipmentDr = false,
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Name = "Armor DR from catalog",
                    Slot = "Body",
                    ItemCategory = "Armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var expressionResult = new BattleFormulaEngine(expressionSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(expressionResult.ShouldRewrite, "attacker formula should rewrite");
        Check(expressionResult.FinalDamage == 30, $"attacker formula final damage expected 30, got {expressionResult.FinalDamage}");
        Check(expressionResult.DesiredHp == 20, $"attacker formula desired HP expected 20, got {expressionResult.DesiredHp}");

        // Newly-mapped attribute variables (job/zodiac/gender/raw stats/job growth) wired into the
        // formula context from confirmed struct offsets. Verifies byte, hi-nibble, and bit decoding.
        var attrAtkRaw = new byte[0x180];
        attrAtkRaw[0x03] = 88;     // job id (Samurai)
        attrAtkRaw[0x06] = 0x80;   // gender flags: Male (bit7)
        attrAtkRaw[0x09] = 0x4A;   // zodiac in hi-nibble -> 4 (low nibble ignored)
        attrAtkRaw[0x38] = 15;     // raw/base PA
        attrAtkRaw[0x8B] = 70;     // HP multiplier
        attrAtkRaw[0x91] = 122;    // PA multiplier
        var attrAttacker = new UnitSnapshot((nint)0x1500, 0x01, 71, 9999, 9999, 1, false, 20, 7, 16, 6, 4, 97, 72, attrAtkRaw, 41, 41);
        var attrTgtRaw = new byte[0x180];
        attrTgtRaw[0x06] = 0x40;   // gender flags: Female (bit6)
        var attrTarget = new UnitSnapshot((nint)0x2500, 0x80, 50, 9999, 9999, 2, true, 5, 3, 6, 4, 3, 70, 60, attrTgtRaw, 18, 30);
        var attrSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            ApplyEquipmentDr = false,
            FinalDamageFormula = "a.job + a.zodiac + a.isMale + a.rawPa + a.hpMult + a.paMult + t.isFemale",
        };
        var attrResult = new BattleFormulaEngine(attrSettings, catalog)
            .Evaluate(new DamageEvent(attrTarget, 9999, 9979, 20, attrAttacker));
        // 88 + 4 + 1 + 15 + 70 + 122 + 1 = 301
        Check(attrResult.FinalDamage == 301, $"attribute formula expected 301, got {attrResult.FinalDamage}");
        Check(attrResult.ShouldRewrite, "attribute formula should rewrite");

        var stagedSettings = new RuntimeSettings
        {
            RewriteObservedDamage = false,
            RewriteConditionFormula = "event.isDamage && a.present",
            FinalDamageFormula = "a.pa * 3 + vanillaDamage",
        };
        var stagedLateResult = new BattleFormulaEngine(stagedSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(!stagedLateResult.ShouldRewrite, "late HP rewrite should stay off when RewriteObservedDamage is false");
        var stagedApplyResult = new BattleFormulaEngine(stagedSettings, catalog)
            .EvaluateForStagedApply(new DamageEvent(target, 50, 30, 20, attacker));
        Check(stagedApplyResult.ShouldRewrite, "staged apply evaluation should bypass the late rewrite gate");
        Check(stagedApplyResult.FinalDamage == 56, $"staged apply final damage expected 56, got {stagedApplyResult.FinalDamage}");
        Check(stagedApplyResult.DesiredHp == 0, $"staged apply desired HP expected 0, got {stagedApplyResult.DesiredHp}");

        var mpAwareResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "vanillaDamage + t.mp + a.maxMp - t.maxMp + a.mp",
        }, catalog).Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(mpAwareResult.ShouldRewrite, "MP-aware formula should rewrite");
        Check(mpAwareResult.FinalDamage == 40, $"MP-aware formula expected 40, got {mpAwareResult.FinalDamage}");

        var ignoredMpLoss = new BattleFormulaEngine(new RuntimeSettings
        {
            FinalMpChangeFormula = "-99",
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(!ignoredMpLoss.ShouldRewrite, "MP loss should not rewrite unless RewriteObservedMpLoss is enabled");
        Check(ignoredMpLoss.DesiredMp == 12, $"ignored MP loss should preserve current MP 12, got {ignoredMpLoss.DesiredMp}");

        var proofMpLoss = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            ProofFinalMpLoss = 5,
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(proofMpLoss.ShouldRewrite, "proof MP loss should rewrite when enabled");
        Check(proofMpLoss.FinalMpChange == -5, $"proof MP loss final change expected -5, got {proofMpLoss.FinalMpChange}");
        Check(proofMpLoss.DesiredMp == 15, $"proof MP loss desired MP expected 15, got {proofMpLoss.DesiredMp}");

        var formulaMpLoss = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            FinalMpChangeFormula = "-clamp(vanillaMpLoss + t.ma + a.mp, 0, previousMp)",
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(formulaMpLoss.ShouldRewrite, "formula MP loss should rewrite");
        Check(formulaMpLoss.FinalMpChange == -20, $"formula MP loss final change expected -20, got {formulaMpLoss.FinalMpChange}");
        Check(formulaMpLoss.DesiredMp == 0, $"formula MP loss desired MP expected 0, got {formulaMpLoss.DesiredMp}");

        var formulaMpGain = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpGain = true,
            FinalMpChangeFormula = "min(vanillaMpGain + t.ma + a.mp, t.maxMp - previousMp)",
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 18 }, 10, 18, 8, attacker));
        Check(formulaMpGain.ShouldRewrite, "formula MP gain should rewrite");
        Check(formulaMpGain.FinalMpChange == 20, $"formula MP gain final change expected 20, got {formulaMpGain.FinalMpChange}");
        Check(formulaMpGain.DesiredMp == 30, $"formula MP gain desired MP expected 30, got {formulaMpGain.DesiredMp}");

        var mpGateBlocked = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            MpRewriteConditionFormula = "event.isMpGain",
            FinalMpChangeFormula = "-1",
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(!mpGateBlocked.ShouldRewrite, "MP rewrite condition should block a nonmatching MP event");
        Check(mpGateBlocked.RuleName == "MpRewriteConditionFormula=0", $"MP gate blocked reason expected MpRewriteConditionFormula=0, got {mpGateBlocked.RuleName}");

        var actionMpLoss = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            FinalMpChangeFormula = "-(vanillaMpLoss + action.sourceMpChange + action.spell)",
            LogResolvedRuntimeContext = true,
            FormulaTraceVariables =
            [
                new FormulaDerivedVariable { Name = "mp.loss", Formula = "vanillaMpLoss" },
                new FormulaDerivedVariable { Name = "result.mp", Formula = "result.finalMpChange" },
            ],
        }, catalog).EvaluateMp(new MpEvent(
            target with { Mp = 12 },
            20,
            12,
            -8,
            attacker,
            "validator",
            new ActionSignal("Spell MP cost", "mp-change", new Dictionary<string, int> { ["signal"] = 22, ["spell"] = 1 })));
        Check(actionMpLoss.ShouldRewrite, "manual MP action context should rewrite");
        Check(actionMpLoss.FinalMpChange == -10, $"manual MP action final change expected -10, got {actionMpLoss.FinalMpChange}");
        Check(actionMpLoss.DesiredMp == 10, $"manual MP action desired MP expected 10, got {actionMpLoss.DesiredMp}");
        Check(actionMpLoss.Trace.Contains("event=mpLoss"), "MP trace should include event kind");
        Check(actionMpLoss.Trace.Contains("finalMpChange=-10:FinalMpChangeFormula"), "MP trace should include final MP change");

        var mpSignalSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteObservedMpLoss = true,
            FinalDamageFormula = "vanillaDamage + action.spell * 100",
            FinalMpChangeFormula = "-min(previousMp, vanillaMpLoss + action.spellPower + action.sourceMpChange)",
            LogResolvedRuntimeContext = true,
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "MP spell sentinel",
                    VanillaMpChange = -8,
                    Signal = 33,
                    Variables = new Dictionary<string, int> { ["spell"] = 1 },
                    VariableFormulas = new Dictionary<string, string> { ["spellPower"] = "vanillaMpLoss + t.ma" },
                },
            ],
        };
        var mpSignalResult = new BattleFormulaEngine(mpSignalSettings, catalog)
            .EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(mpSignalResult.ShouldRewrite, "MP action signal should classify a matching MP delta");
        Check(mpSignalResult.FinalMpChange == -20, $"MP action signal final change expected -20, got {mpSignalResult.FinalMpChange}");
        Check(mpSignalResult.DesiredMp == 0, $"MP action signal desired MP expected 0, got {mpSignalResult.DesiredMp}");
        Check(mpSignalResult.Trace.Contains("action=MP spell sentinel:source=mp-change:signal=33:vars=spell=1,spellpower=11"), "MP action signal trace should include derived action variables");

        var mpSignalShouldNotMatchHp = new BattleFormulaEngine(mpSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(mpSignalShouldNotMatchHp.ShouldRewrite, "HP event should still rewrite with MP-only signal rule present");
        Check(mpSignalShouldNotMatchHp.FinalDamage == 20, $"MP-only action signal should not classify HP event, got final damage {mpSignalShouldNotMatchHp.FinalDamage}");

        var mpRuleSettings = new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            RewriteObservedMpGain = true,
            FinalMpChangeFormula = "vanillaMpChange",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "MP spell sentinel",
                    VanillaMpChange = -8,
                    Signal = 33,
                    Variables = new Dictionary<string, int> { ["spell"] = 1 },
                    VariableFormulas = new Dictionary<string, string> { ["spellPower"] = "vanillaMpLoss + t.ma" },
                },
            ],
            MpRules =
            [
                new MpRule
                {
                    Name = "Spell MP rule",
                    EventKind = "Loss",
                    RequiredActionVariable = "spell",
                    FinalMpChangeFormula = "-min(previousMp, action.spellPower + 1)",
                },
                new MpRule
                {
                    Name = "Small gain rule",
                    EventKind = "Gain",
                    MaxFinalMpChange = 5,
                },
            ],
        };
        var mpRuleLossResult = new BattleFormulaEngine(mpRuleSettings, catalog)
            .EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(mpRuleLossResult.ShouldRewrite, "MP rule should rewrite matching spell loss");
        Check(mpRuleLossResult.RuleName == "Spell MP rule", $"MP rule loss expected Spell MP rule, got {mpRuleLossResult.RuleName}");
        Check(mpRuleLossResult.FinalMpChange == -12, $"MP rule loss final change expected -12, got {mpRuleLossResult.FinalMpChange}");
        Check(mpRuleLossResult.DesiredMp == 8, $"MP rule loss desired MP expected 8, got {mpRuleLossResult.DesiredMp}");

        var mpRuleGainResult = new BattleFormulaEngine(mpRuleSettings, catalog)
            .EvaluateMp(new MpEvent(target with { Mp = 18 }, 10, 18, 8, attacker));
        Check(mpRuleGainResult.ShouldRewrite, "MP gain rule should rewrite matching gain");
        Check(mpRuleGainResult.RuleName == "Small gain rule", $"MP gain rule expected Small gain rule, got {mpRuleGainResult.RuleName}");
        Check(mpRuleGainResult.FinalMpChange == 5, $"MP gain rule final change expected 5, got {mpRuleGainResult.FinalMpChange}");
        Check(mpRuleGainResult.DesiredMp == 15, $"MP gain rule desired MP expected 15, got {mpRuleGainResult.DesiredMp}");

        var badMpRuleResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedMpLoss = true,
            MpRules =
            [
                new MpRule { Name = "Bad MP rule", FinalMpChangeFormula = "missingVariable + 1" },
            ],
        }, catalog).EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(!badMpRuleResult.ShouldRewrite, "invalid MP rule formula should skip rewrite");
        Check(badMpRuleResult.RuleName.Contains("Bad MP rule"), "invalid MP rule should name the failing rule");

        var fallbackSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(a.present, a.pa, t.pa) + equipmentDr",
            ApplyEquipmentDr = false,
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var fallbackResult = new BattleFormulaEngine(fallbackSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(fallbackResult.ShouldRewrite, "fallback formula should rewrite without attacker context");
        Check(fallbackResult.FinalDamage == 5, $"fallback formula final damage expected 5, got {fallbackResult.FinalDamage}");
        Check(fallbackResult.DesiredHp == 45, $"fallback formula desired HP expected 45, got {fallbackResult.DesiredHp}");

        var invalidSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "attackerByte(0x44)",
        };
        var invalidResult = new BattleFormulaEngine(invalidSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(!invalidResult.ShouldRewrite, "attackerByte without attacker context should skip rewrite");

        var lazyIfNoAttackerSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(a.present, attackerByte(0x44), max(7, 3))",
        };
        var lazyIfNoAttackerResult = new BattleFormulaEngine(lazyIfNoAttackerSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(lazyIfNoAttackerResult.ShouldRewrite, "lazy if should skip unavailable attackerByte branch");
        Check(lazyIfNoAttackerResult.FinalDamage == 7, $"lazy if no-attacker expected 7, got {lazyIfNoAttackerResult.FinalDamage}");

        var lazyIfWithAttackerResult = new BattleFormulaEngine(lazyIfNoAttackerSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(lazyIfWithAttackerResult.ShouldRewrite, "lazy if should evaluate attackerByte branch when attacker exists");
        Check(lazyIfWithAttackerResult.FinalDamage == 99, $"lazy if attacker expected 99, got {lazyIfWithAttackerResult.FinalDamage}");

        var lazyNestedIfResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(1, min(8, 9), attackerByte(0x44))",
        }, catalog).Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(lazyNestedIfResult.ShouldRewrite, "lazy if should parse nested function branches");
        Check(lazyNestedIfResult.FinalDamage == 8, $"lazy nested if expected 8, got {lazyNestedIfResult.FinalDamage}");

        var lazyAndNoAttackerSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "a.present && attackerByte(0x44)",
        };
        var lazyAndNoAttackerResult = new BattleFormulaEngine(lazyAndNoAttackerSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(lazyAndNoAttackerResult.ShouldRewrite, "lazy && should skip unavailable attackerByte branch");
        Check(lazyAndNoAttackerResult.FinalDamage == 0, $"lazy && no-attacker expected 0, got {lazyAndNoAttackerResult.FinalDamage}");

        var lazyAndWithAttackerResult = new BattleFormulaEngine(lazyAndNoAttackerSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(lazyAndWithAttackerResult.ShouldRewrite, "lazy && should evaluate right branch when left is true");
        Check(lazyAndWithAttackerResult.FinalDamage == 1, $"lazy && attacker expected 1, got {lazyAndWithAttackerResult.FinalDamage}");

        var lazyOrNoAttackerResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "!a.present || attackerByte(0x44)",
        }, catalog).Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(lazyOrNoAttackerResult.ShouldRewrite, "lazy || should skip unavailable attackerByte branch");
        Check(lazyOrNoAttackerResult.FinalDamage == 1, $"lazy || no-attacker expected 1, got {lazyOrNoAttackerResult.FinalDamage}");

        var healedTarget = target with { Hp = 40 };
        var ignoredHealingResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteObservedHealing = false,
            FinalDamageFormula = "-10",
        }, catalog).Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(!ignoredHealingResult.ShouldRewrite, "healing should not rewrite unless RewriteObservedHealing is enabled");

        var formulaHealingSettings = new RuntimeSettings
        {
            RewriteObservedHealing = true,
            FinalDamageFormula = "if(event.isHealing && observedHpGain == vanillaHealing && vanillaDamageAbs == vanillaHealing, -vanillaHealing - t.ma, 99)",
        };
        var formulaHealingResult = new BattleFormulaEngine(formulaHealingSettings, catalog)
            .Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(formulaHealingResult.ShouldRewrite, "formula healing should rewrite when enabled");
        Check(formulaHealingResult.FinalDamage == -13, $"formula healing final damage expected -13, got {formulaHealingResult.FinalDamage}");
        Check(formulaHealingResult.DesiredHp == 43, $"formula healing desired HP expected 43, got {formulaHealingResult.DesiredHp}");

        var proofHealingResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedHealing = true,
            ProofFinalHealing = 4,
        }, catalog).Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(proofHealingResult.ShouldRewrite, "proof healing should rewrite when enabled");
        Check(proofHealingResult.FinalDamage == -4, $"proof healing final damage expected -4, got {proofHealingResult.FinalDamage}");
        Check(proofHealingResult.DesiredHp == 34, $"proof healing desired HP expected 34, got {proofHealingResult.DesiredHp}");

        var healingWithDrSettings = new RuntimeSettings
        {
            RewriteObservedHealing = true,
            FinalDamageFormula = "-vanillaHealing - equipmentDr",
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var healingWithDrResult = new BattleFormulaEngine(healingWithDrSettings, catalog)
            .Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(healingWithDrResult.ShouldRewrite, "healing with configured DR should still rewrite");
        Check(healingWithDrResult.FinalDamage == -10, $"healing should not receive equipment DR, got {healingWithDrResult.FinalDamage}");
        Check(healingWithDrResult.DesiredHp == 40, $"healing with DR desired HP expected 40, got {healingWithDrResult.DesiredHp}");

        var tableSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "max(0, tableClamp(swing, a.pa) + tableOr(missing, a.pa, 7) - equipmentDr)",
            ApplyEquipmentDr = false,
            FormulaTables = new Dictionary<string, List<int>>
            {
                ["swing"] = Enumerable.Range(0, 21).Select(i => i * 2).ToList(),
                ["thrust"] = Enumerable.Range(0, 21).ToList(),
            },
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var tableResult = new BattleFormulaEngine(tableSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(tableResult.ShouldRewrite, "table formula should rewrite");
        Check(tableResult.FinalDamage == 30, $"table formula final damage expected 30, got {tableResult.FinalDamage}");
        Check(tableResult.DesiredHp == 20, $"table formula desired HP expected 20, got {tableResult.DesiredHp}");

        var tableFailureSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "table(swing, 99)",
            FormulaTables = new Dictionary<string, List<int>>
            {
                ["swing"] = [0, 1, 2],
            },
        };
        var tableFailureResult = new BattleFormulaEngine(tableFailureSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(!tableFailureResult.ShouldRewrite, "out-of-range table lookup should skip rewrite");

        var matrixContext = new FormulaContext(target, attacker, eventIndex: 7, eventSeed: 77);
        matrixContext.SetMatrix("response", new[]
        {
            new[] { 650, 750 },
            new[] { 950, 1000 },
        });
        Check(FormulaExpression.TryEvaluate("matrix(response, 0, 1)", matrixContext, out int matrixExact, out string matrixExactError), $"matrix exact should evaluate: {matrixExactError}");
        Check(matrixExact == 750, $"matrix exact expected 750, got {matrixExact}");
        Check(FormulaExpression.TryEvaluate("matrixClamp(response, 99, 99)", matrixContext, out int matrixClamp, out string matrixClampError), $"matrix clamp should evaluate: {matrixClampError}");
        Check(matrixClamp == 1000, $"matrix clamp expected 1000, got {matrixClamp}");
        Check(FormulaExpression.TryEvaluate("matrixOr(missingResponse, 9, 9, 123)", matrixContext, out int matrixFallback, out string matrixFallbackError), $"matrix fallback should evaluate: {matrixFallbackError}");
        Check(matrixFallback == 123, $"matrix fallback expected 123, got {matrixFallback}");

        var matrixSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FormulaVariables = new Dictionary<string, int>
            {
                ["damageType"] = 0,
                ["armorClass"] = 1,
            },
            FormulaMatrices = new Dictionary<string, List<List<int>>>
            {
                ["response"] =
                [
                    [650, 750],
                    [950, 1000],
                ],
            },
            FinalDamageFormula = "matrix(response, damageType, armorClass)",
        };
        var matrixResult = new BattleFormulaEngine(matrixSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(matrixResult.ShouldRewrite, "matrix runtime formula should rewrite");
        Check(matrixResult.FinalDamage == 750, $"matrix runtime formula expected 750, got {matrixResult.FinalDamage}");

        var mapSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FormulaMaps = new Dictionary<string, Dictionary<string, int>>
            {
                ["armorDrByItem"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["172"] = 3,
                    ["0xAE"] = 6,
                },
                ["weaponFamilyByItem"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["19"] = 11,
                },
            },
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            FinalDamageFormula = "vanillaDamage + map(armorDrByItem, slot.body.itemId) + mapOr(weaponFamilyByItem, aslot.weapon.itemId, 0)",
        };
        var mapResult = new BattleFormulaEngine(mapSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(mapResult.ShouldRewrite, "map formula should rewrite");
        Check(mapResult.FinalDamage == 34, $"map formula expected 34, got {mapResult.FinalDamage}");

        var mapFallbackTarget = TargetWithBodyItem(1, (nint)0x2010);
        var mapFallbackSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FormulaMaps = mapSettings.FormulaMaps,
            EquipmentSlots = mapSettings.EquipmentSlots,
            FinalDamageFormula = "vanillaDamage + mapOr(armorDrByItem, slot.body.itemId, 0)",
        };
        var mapFallbackResult = new BattleFormulaEngine(mapFallbackSettings, catalog)
            .Evaluate(new DamageEvent(mapFallbackTarget, 50, 30, 20, attacker));
        Check(mapFallbackResult.ShouldRewrite, "map fallback formula should rewrite");
        Check(mapFallbackResult.FinalDamage == 20, $"map fallback formula expected 20, got {mapFallbackResult.FinalDamage}");

        var mapFailureSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FormulaMaps = mapSettings.FormulaMaps,
            EquipmentSlots = mapSettings.EquipmentSlots,
            FinalDamageFormula = "map(armorDrByItem, slot.body.itemId)",
        };
        var mapFailureResult = new BattleFormulaEngine(mapFailureSettings, catalog)
            .Evaluate(new DamageEvent(mapFallbackTarget, 50, 30, 20, attacker));
        Check(!mapFailureResult.ShouldRewrite, "missing map key without fallback should skip rewrite");

        var diceTableSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "max(0, diceAvg(tableClamp(swingDice, a.pa), 6, tableClamp(swingAdds, a.pa)) - equipmentDr)",
            ApplyEquipmentDr = false,
            FormulaTables = new Dictionary<string, List<int>>
            {
                ["swingDice"] = Enumerable.Range(0, 21).Select(i => i < 10 ? 1 : 2).ToList(),
                ["swingAdds"] = Enumerable.Range(0, 21).Select(i => i - 10).ToList(),
            },
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var diceTableResult = new BattleFormulaEngine(diceTableSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(diceTableResult.ShouldRewrite, "dice table formula should rewrite");
        Check(diceTableResult.FinalDamage == 8, $"dice table final damage expected 8, got {diceTableResult.FinalDamage}");
        Check(diceTableResult.DesiredHp == 42, $"dice table desired HP expected 42, got {diceTableResult.DesiredHp}");

        CheckFormula("diceMin(2, 6, -1)", target, attacker, 1);
        CheckFormula("diceMax(2, 6, -1)", target, attacker, 11);
        CheckFormula("diceAvg(2, 6, -1)", target, attacker, 6);
        CheckFormula("diceAvg(1, 6, 0)", target, attacker, 3);
        CheckFormula("diceAvgRound(1, 6, 0)", target, attacker, 4);
        CheckFormula("diceAvgCeil(1, 6, 0)", target, attacker, 4);
        CheckFormula("diceAverageRound(2, 6, -1)", target, attacker, 6);
        CheckFormula("floorDiv(7, 2)", target, attacker, 3);
        CheckFormula("ceilDiv(7, 2)", target, attacker, 4);
        CheckFormula("roundDiv(5, 2)", target, attacker, 3);
        CheckFormula("mulDiv(15, 3, 2)", target, attacker, 22);
        CheckFormula("mulDivCeil(15, 3, 2)", target, attacker, 23);
        CheckFormula("mulDivRound(15, 3, 2)", target, attacker, 23);
        CheckFormula("hasBit(0x20, 5)", target, attacker, 1);
        CheckFormula("hasBit(0x20, 4)", target, attacker, 0);
        CheckFormula("hasAnyBits(0x28, 0x08)", target, attacker, 1);
        CheckFormula("hasAllBits(0x28, 0x28)", target, attacker, 1);
        CheckFormula("hasAllBits(0x28, 0x38)", target, attacker, 0);
        CheckFormula("noBits(0x20, 0x08)", target, attacker, 1);
        CheckFormula("bitAnd(0x2A, 0x0F)", target, attacker, 10);
        CheckFormula("bitOr(0x20, 0x08)", target, attacker, 40);
        CheckFormula("bitXor(0x28, 0x08)", target, attacker, 32);
        CheckFormula("shl(1, 5)", target, attacker, 32);
        CheckFormula("shr(0x20, 5)", target, attacker, 1);
        CheckFormula("rand(5, 5)", target, attacker, 5);
        CheckFormula("event.index", target, attacker, 42, 42, 123456);
        var flaggedTargetRaw = (byte[])targetRaw.Clone();
        flaggedTargetRaw[0x90] = 0x28;
        flaggedTargetRaw[0x91] = 0x02;
        flaggedTargetRaw[0x92] = 0xF6;
        flaggedTargetRaw[0x94] = 0x34;
        flaggedTargetRaw[0x95] = 0x12;
        flaggedTargetRaw[0x96] = 0xFC;
        flaggedTargetRaw[0x97] = 0xFF;
        flaggedTargetRaw[0x98] = 0x78;
        flaggedTargetRaw[0x99] = 0x56;
        flaggedTargetRaw[0x9A] = 0x34;
        flaggedTargetRaw[0x9B] = 0x12;
        var flaggedTarget = target with { Raw = flaggedTargetRaw };
        CheckFormula("hasBit(targetByte(0x90), 5)", flaggedTarget, attacker, 1);
        CheckFormula("hasAnyBits(targetByte(0x90), 0x08)", flaggedTarget, attacker, 1);
        CheckFormula("hasAllBits(targetWord(0x90), 0x0228)", flaggedTarget, attacker, 1);
        CheckFormula("targetSByte(0x92)", flaggedTarget, attacker, -10);
        CheckFormula("targetWord(0x94)", flaggedTarget, attacker, 4660);
        CheckFormula("targetShort(0x96)", flaggedTarget, attacker, -4);
        CheckFormula("bitExtract(targetDWord(0x98), 8, 8)", flaggedTarget, attacker, 86);
        CheckFormula("signedBitExtract(0x0E, 0, 4)", flaggedTarget, attacker, -2);
        CheckFormula("bits(0xAC, 2, 3)", flaggedTarget, attacker, 3);
        var packedAttackerRaw = (byte[])attackerRaw.Clone();
        packedAttackerRaw[0x60] = 0xFE;
        packedAttackerRaw[0x62] = 0xFD;
        packedAttackerRaw[0x63] = 0xFF;
        packedAttackerRaw[0x64] = 0x78;
        packedAttackerRaw[0x65] = 0x56;
        packedAttackerRaw[0x66] = 0x34;
        packedAttackerRaw[0x67] = 0x12;
        var packedAttacker = attacker with { Raw = packedAttackerRaw };
        CheckFormula("attackerSByte(0x60) + attackerShort(0x62) + bitExtract(attackerDWord(0x64), 16, 8)", flaggedTarget, packedAttacker, 47);
        Check(!FormulaExpression.TryEvaluate("hasBit(1, 63)", new FormulaContext(target, attacker), out _, out string bitError), "out-of-range bit should fail validation");
        Check(bitError.Contains("outside 0..62"), $"out-of-range bit error should explain range, got {bitError}");
        Check(!FormulaExpression.TryEvaluate("bitExtract(1, 62, 2)", new FormulaContext(target, attacker), out _, out string bitRangeError), "out-of-range bit extract should fail validation");
        Check(bitRangeError.Contains("exceeds bit 62"), $"out-of-range bit extract error should explain range, got {bitRangeError}");

        var packedFieldSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "bitExtract(targetDWord(0x98), 8, 8) + targetSByte(0x92) + targetShort(0x96)",
        };
        var packedFieldResult = new BattleFormulaEngine(packedFieldSettings, catalog)
            .Evaluate(new DamageEvent(flaggedTarget, 100, 80, 20, attacker));
        Check(packedFieldResult.ShouldRewrite, "packed raw field formula should rewrite");
        Check(packedFieldResult.FinalDamage == 72, $"packed raw field formula expected 72, got {packedFieldResult.FinalDamage}");

        var statusFlagSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteConditionFormula = "status.poison",
            FinalDamageFormula = "vanillaDamage + status.oil * 10",
            FormulaPreActionVariables =
            [
                new FormulaDerivedVariable { Name = "status.poison", Formula = "hasBit(targetByte(0x90), 5)" },
                new FormulaDerivedVariable { Name = "status.oil", Formula = "hasAnyBits(targetWord(0x90), 0x0200)" },
            ],
        };
        var statusFlagResult = new BattleFormulaEngine(statusFlagSettings, catalog)
            .Evaluate(new DamageEvent(flaggedTarget, 50, 30, 20, attacker));
        Check(statusFlagResult.ShouldRewrite, "status bit formula should rewrite when the gate flag is set");
        Check(statusFlagResult.FinalDamage == 30, $"status bit formula expected 30, got {statusFlagResult.FinalDamage}");

        var statusFlagBlocked = new BattleFormulaEngine(statusFlagSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(!statusFlagBlocked.ShouldRewrite, "status bit rewrite gate should block when the flag is absent");
        Check(statusFlagBlocked.RuleName == "RewriteConditionFormula=0", $"status bit blocked reason expected RewriteConditionFormula=0, got {statusFlagBlocked.RuleName}");

        var seededRollA = EvalFormula("diceRoll(2, 6, -1)", target, attacker, 42, 123456);
        var seededRollB = EvalFormula("diceRoll(2, 6, -1)", target, attacker, 42, 123456);
        Check(seededRollA == seededRollB, "same event seed should produce same diceRoll result");
        Check(seededRollA is >= 1 and <= 11, $"diceRoll(2,6,-1) should be in 1..11, got {seededRollA}");
        var seededSequence = EvalFormula("diceRoll(1, 6, 0) + diceRoll(1, 6, 0)", target, attacker, 42, 123456);
        Check(seededSequence is >= 2 and <= 12, $"two diceRoll calls should be in 2..12, got {seededSequence}");
        var indexedRandom = EvalFormula("randAt(7, 1, 100)", target, attacker, 42, 123456);
        var indexedRandomAfterSequential = EvalFormula("rand(1, 100) * 0 + randAt(7, 1, 100)", target, attacker, 42, 123456);
        var indexedRandomBeforeSequential = EvalFormula("randAt(7, 1, 100) + rand(1, 100) * 0", target, attacker, 42, 123456);
        Check(indexedRandom == indexedRandomAfterSequential, "randAt should not depend on earlier sequential rand calls");
        Check(indexedRandom == indexedRandomBeforeSequential, "randAt should not depend on later sequential rand calls");
        Check(indexedRandom is >= 1 and <= 100, $"randAt(7,1,100) should be in 1..100, got {indexedRandom}");
        var indexedDice = EvalFormula("diceRollAt(7, 2, 6, -1)", target, attacker, 42, 123456);
        var indexedDiceRepeated = EvalFormula("diceRollAt(7, 2, 6, -1) - diceRollAt(7, 2, 6, -1)", target, attacker, 42, 123456);
        var indexedDiceAfterSequential = EvalFormula("rand(1, 100) * 0 + diceRollAt(7, 2, 6, -1)", target, attacker, 42, 123456);
        Check(indexedDiceRepeated == 0, "diceRollAt should return the same roll for the same index inside one formula");
        Check(indexedDice == indexedDiceAfterSequential, "diceRollAt should not depend on earlier sequential rand calls");
        Check(indexedDice is >= 1 and <= 11, $"diceRollAt(7,2,6,-1) should be in 1..11, got {indexedDice}");

        var attackerWeaponSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(aslot.weapon.category_sword, tableClamp(swing, a.pa), tableClamp(thrust, a.pa)) + aslot.weapon.weaponPower - equipmentDr",
            ApplyEquipmentDr = false,
            FormulaTables = new Dictionary<string, List<int>>
            {
                ["swing"] = Enumerable.Range(0, 21).Select(i => i * 2).ToList(),
                ["thrust"] = Enumerable.Range(0, 21).ToList(),
            },
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)"
                }
            ],
        };
        var attackerWeaponResult = new BattleFormulaEngine(attackerWeaponSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(attackerWeaponResult.ShouldRewrite, "attacker weapon formula should rewrite");
        Check(attackerWeaponResult.FinalDamage == 27, $"attacker weapon final damage expected 27, got {attackerWeaponResult.FinalDamage}");
        Check(attackerWeaponResult.DesiredHp == 23, $"attacker weapon desired HP expected 23, got {attackerWeaponResult.DesiredHp}");

        var missingAttackerWeaponResult = new BattleFormulaEngine(attackerWeaponSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(missingAttackerWeaponResult.ShouldRewrite, "missing-attacker weapon formula should still rewrite with default slot vars");
        Check(missingAttackerWeaponResult.FinalDamage == 0, $"missing-attacker weapon final damage expected 0, got {missingAttackerWeaponResult.FinalDamage}");
        Check(missingAttackerWeaponResult.DesiredHp == 50, $"missing-attacker weapon desired HP expected 50, got {missingAttackerWeaponResult.DesiredHp}");

        var scannedSlotSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "aslot.weapon.weaponPower + slot.body.armorHpBonus + aslot.weapon.offset + slot.body.offset + aslot.weapon.scanMatches + slot.body.scanMatches",
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe
                {
                    Name = "Weapon",
                    SearchStart = 0x44,
                    SearchEnd = 0x80,
                    SearchWidth = "Byte",
                    SecondaryKind = "weapon",
                    ItemCategory = "Sword",
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe
                {
                    Name = "Body",
                    SearchStart = 0x44,
                    SearchEnd = 0x80,
                    SearchWidth = "Byte",
                    SecondaryKind = "armor",
                    TypeFlag = "Armor",
                },
            ],
        };
        var scannedSlotResult = new BattleFormulaEngine(scannedSlotSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(scannedSlotResult.ShouldRewrite, "catalog-scanned slots should rewrite when exactly one match exists");
        Check(scannedSlotResult.FinalDamage == 208, $"catalog-scanned slots expected 208, got {scannedSlotResult.FinalDamage}");

        var ambiguousRaw = (byte[])targetRaw.Clone();
        ambiguousRaw[0x72] = 173; // Linen Cuirass: a second armor candidate.
        var ambiguousTarget = target with { Raw = ambiguousRaw };
        var ambiguousScanSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "slot.body.present * 10 + slot.body.ambiguous + slot.body.scanMatches",
            EquipmentSlots =
            [
                new EquipmentSlotProbe
                {
                    Name = "Body",
                    SearchStart = 0x70,
                    SearchEnd = 0x72,
                    SearchWidth = "Byte",
                    SecondaryKind = "armor",
                    TypeFlag = "Armor",
                },
            ],
        };
        var ambiguousScanResult = new BattleFormulaEngine(ambiguousScanSettings, catalog)
            .Evaluate(new DamageEvent(ambiguousTarget, 50, 30, 20, attacker));
        Check(ambiguousScanResult.ShouldRewrite, "ambiguous scan should still expose scan metadata");
        Check(ambiguousScanResult.FinalDamage == 3, $"ambiguous scan expected metadata value 3, got {ambiguousScanResult.FinalDamage}");

        var firstAmbiguousScanSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "slot.body.itemId + slot.body.scanMatches",
            EquipmentSlots =
            [
                new EquipmentSlotProbe
                {
                    Name = "Body",
                    SearchStart = 0x70,
                    SearchEnd = 0x72,
                    SearchWidth = "Byte",
                    SecondaryKind = "armor",
                    TypeFlag = "Armor",
                    AllowAmbiguousSearchMatch = true,
                },
            ],
        };
        var firstAmbiguousScanResult = new BattleFormulaEngine(firstAmbiguousScanSettings, catalog)
            .Evaluate(new DamageEvent(ambiguousTarget, 50, 30, 20, attacker));
        Check(firstAmbiguousScanResult.ShouldRewrite, "ambiguous scan should use first match when explicitly allowed");
        Check(firstAmbiguousScanResult.FinalDamage == 174, $"first ambiguous scan expected 174, got {firstAmbiguousScanResult.FinalDamage}");

        var conditionalDamageRuleSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            DamageRules =
            [
                new DamageRule
                {
                    Name = "Sword attacker condition",
                    ConditionFormula = "a.present && aslot.weapon.category_sword && t.isFoe",
                    FinalDamageFormula = "a.pa + aslot.weapon.weaponPower",
                },
                new DamageRule { Name = "Fallback condition rule", FinalDamage = 1 },
            ],
        };
        var conditionalDamageRuleResult = new BattleFormulaEngine(conditionalDamageRuleSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(conditionalDamageRuleResult.ShouldRewrite, "conditional damage rule should rewrite");
        Check(conditionalDamageRuleResult.FinalDamage == 16, $"conditional damage rule expected 16, got {conditionalDamageRuleResult.FinalDamage}");

        var conditionalDamageRuleFallback = new BattleFormulaEngine(conditionalDamageRuleSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(conditionalDamageRuleFallback.ShouldRewrite, "conditional damage fallback should rewrite");
        Check(conditionalDamageRuleFallback.FinalDamage == 1, $"conditional damage fallback expected 1, got {conditionalDamageRuleFallback.FinalDamage}");

        var eventKindDamageRuleSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteObservedHealing = true,
            DamageRules =
            [
                new DamageRule { Name = "Damage only rule", EventKind = "Damage", FinalDamage = 7 },
                new DamageRule { Name = "Healing only rule", EventKind = "Healing", FinalDamageFormula = "-12" },
            ],
        };
        var eventKindDamageRuleResult = new BattleFormulaEngine(eventKindDamageRuleSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(eventKindDamageRuleResult.ShouldRewrite, "event-kind damage rule should rewrite damage");
        Check(eventKindDamageRuleResult.RuleName == "Damage only rule", $"event-kind damage rule expected Damage only rule, got {eventKindDamageRuleResult.RuleName}");
        Check(eventKindDamageRuleResult.FinalDamage == 7, $"event-kind damage rule expected 7, got {eventKindDamageRuleResult.FinalDamage}");

        var eventKindHealingRuleResult = new BattleFormulaEngine(eventKindDamageRuleSettings, catalog)
            .Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(eventKindHealingRuleResult.ShouldRewrite, "event-kind healing rule should rewrite healing");
        Check(eventKindHealingRuleResult.RuleName == "Healing only rule", $"event-kind healing rule expected Healing only rule, got {eventKindHealingRuleResult.RuleName}");
        Check(eventKindHealingRuleResult.FinalDamage == -12, $"event-kind healing rule expected -12, got {eventKindHealingRuleResult.FinalDamage}");

        var badConditionRuleResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            DamageRules =
            [
                new DamageRule { Name = "Bad condition", ConditionFormula = "missingVariable + 1", FinalDamage = 1 },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badConditionRuleResult.ShouldRewrite, "invalid damage rule condition should skip rewrite");

        var contextFormulaSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "a.present * 10 + a.inferred * 2 + a.sourceRecent",
        };
        var contextFormulaResult = new BattleFormulaEngine(contextFormulaSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker, "recent-unit"));
        Check(contextFormulaResult.ShouldRewrite, "context source formula should rewrite");
        Check(contextFormulaResult.FinalDamage == 13, $"context source formula expected 13, got {contextFormulaResult.FinalDamage}");

        var ctContextResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "t.ct + a.ct + a.sourceCt * 100 + a.sourceCounter * 1000",
        }, catalog).Evaluate(new DamageEvent(target with { Ct = 70 }, 50, 49, 1, attacker with { Ct = 12 }, "ct-reset"));
        Check(ctContextResult.ShouldRewrite, "CT context formula should rewrite");
        Check(ctContextResult.FinalDamage == 182, $"CT context formula expected 182, got {ctContextResult.FinalDamage}");

        var ctLowContextResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "t.ct + a.ct + a.sourceCt * 100 + a.sourceCounter * 1000",
        }, catalog).Evaluate(new DamageEvent(target with { Ct = 70 }, 50, 49, 1, attacker with { Ct = 12 }, "ct-low"));
        Check(ctLowContextResult.ShouldRewrite, "CT-low context formula should rewrite");
        Check(ctLowContextResult.FinalDamage == 182, $"CT-low context formula expected 182, got {ctLowContextResult.FinalDamage}");

        var counterContextResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "a.sourceCounter * 10 + a.sourceCt",
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker, "counter-inversion"));
        Check(counterContextResult.ShouldRewrite, "counter source context formula should rewrite");
        Check(counterContextResult.FinalDamage == 10, $"counter source context formula expected 10, got {counterContextResult.FinalDamage}");

        var pendingContextResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "a.sourcePending * 1000 + action.sourcePending * 100 + action.id + action.batchEvent + action.damageCacheMatch * 10 + action.targetCacheDamage",
        }, catalog).Evaluate(new DamageEvent(
            target,
            50,
            30,
            20,
            attacker,
            "pending-clear",
            new ActionSignal("pending-action-258", "pending-clear", new Dictionary<string, int>
            {
                ["signal"] = 258,
                ["id"] = 258,
                ["batchEvent"] = 2,
                ["damageCacheMatch"] = 1,
                ["targetCacheDamage"] = 187,
            })));
        Check(pendingContextResult.ShouldRewrite, "pending source context formula should rewrite");
        Check(pendingContextResult.FinalDamage == 1557, $"pending source context formula expected 1557, got {pendingContextResult.FinalDamage}");

        var immediateContextResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "a.sourceImmediate * 1000 + action.sourceImmediate * 100 + action.id + action.currentActiveAction * 20 + action.freshActiveAction * 10 + action.margin",
        }, catalog).Evaluate(new DamageEvent(
            target,
            50,
            30,
            20,
            attacker,
            "immediate-action",
            new ActionSignal("immediate-action-1", "immediate-action", new Dictionary<string, int>
            {
                ["signal"] = 1,
                ["id"] = 1,
                ["currentActiveAction"] = 1,
                ["freshActiveAction"] = 1,
                ["margin"] = 300,
            })));
        Check(immediateContextResult.ShouldRewrite, "immediate source context formula should rewrite");
        Check(immediateContextResult.FinalDamage == 1431, $"immediate source context formula expected 1431, got {immediateContextResult.FinalDamage}");

        var actionSignalSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "action.present * 100 + action.sourceVanillaDamage * 10 + action.signal + action.swing + act.cut",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Sword swing sentinel",
                    VanillaDamage = 7,
                    Signal = 41,
                    Variables = new Dictionary<string, int>
                    {
                        ["swing"] = 1,
                        ["cut"] = 1,
                    },
                },
            ],
        };
        var actionSignalResult = new BattleFormulaEngine(actionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 7, attacker));
        Check(actionSignalResult.ShouldRewrite, "action signal formula should rewrite");
        Check(actionSignalResult.FinalDamage == 153, $"action signal formula expected 153, got {actionSignalResult.FinalDamage}");

        var noActionSignalResult = new BattleFormulaEngine(actionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 8, attacker));
        Check(noActionSignalResult.ShouldRewrite, "no-action signal formula should still rewrite with zero action vars");
        Check(noActionSignalResult.FinalDamage == 0, $"no-action signal formula expected 0, got {noActionSignalResult.FinalDamage}");

        var formulaActionSignalSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "action.present * 100 + action.signal + action.power + action.woundNum",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Formula-coded heavy swing",
                    ConditionFormula = "event.isDamage && vanillaDamageAbs >= 10",
                    Signal = 5,
                    Variables = new Dictionary<string, int>
                    {
                        ["swing"] = 1,
                    },
                    VariableFormulas = new Dictionary<string, string>
                    {
                        ["power"] = "vanillaDamageAbs + t.level",
                        ["woundNum"] = "if(vanillaDamageAbs >= 12, 3, 1)",
                    },
                },
            ],
        };
        var formulaActionSignalResult = new BattleFormulaEngine(formulaActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 38, 12, attacker));
        Check(formulaActionSignalResult.ShouldRewrite, "formula action signal should rewrite");
        Check(formulaActionSignalResult.FinalDamage == 125, $"formula action signal expected 125, got {formulaActionSignalResult.FinalDamage}");

        var noFormulaActionSignalResult = new BattleFormulaEngine(formulaActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 42, 8, attacker));
        Check(noFormulaActionSignalResult.ShouldRewrite, "missing formula action signal should still rewrite with zero action vars");
        Check(noFormulaActionSignalResult.FinalDamage == 0, $"missing formula action signal expected 0, got {noFormulaActionSignalResult.FinalDamage}");

        var slotAwareActionSignalSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(action.swing, action.weaponPower + action.statPower + action.armorBonus, 1)",
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Weapon-derived swing signal",
                    ConditionFormula = "a.present && aslot.weapon.category_sword && slot.body.category_armor",
                    Signal = 12,
                    Variables = new Dictionary<string, int>
                    {
                        ["swing"] = 1,
                        ["cut"] = 1,
                    },
                    VariableFormulas = new Dictionary<string, string>
                    {
                        ["weaponPower"] = "aslot.weapon.weaponPower",
                        ["statPower"] = "a.pa",
                        ["armorBonus"] = "slot.body.armorHpBonus",
                    },
                },
            ],
        };
        var slotAwareActionSignalResult = new BattleFormulaEngine(slotAwareActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(slotAwareActionSignalResult.ShouldRewrite, "slot-aware action signal should rewrite");
        Check(slotAwareActionSignalResult.FinalDamage == 26, $"slot-aware action signal expected 26, got {slotAwareActionSignalResult.FinalDamage}");

        var slotAwareMissingAttackerResult = new BattleFormulaEngine(slotAwareActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20));
        Check(slotAwareMissingAttackerResult.ShouldRewrite, "slot-aware missing attacker fallback should rewrite");
        Check(slotAwareMissingAttackerResult.FinalDamage == 1, $"slot-aware missing attacker expected 1, got {slotAwareMissingAttackerResult.FinalDamage}");

        var preActionSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "action.swing * 10 + pre.weaponblade + pre.targetarmor + action.weaponPower",
            FormulaPreActionVariables =
            [
                new FormulaDerivedVariable { Name = "pre.weaponBlade", Formula = "aslot.weapon.category_sword || aslot.weapon.category_knightsword" },
                new FormulaDerivedVariable { Name = "pre.targetArmor", Formula = "slot.body.isArmor" },
            ],
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Pre-action blade swing",
                    ConditionFormula = "pre.weaponblade && pre.targetarmor",
                    Variables = new Dictionary<string, int> { ["swing"] = 1 },
                    VariableFormulas = new Dictionary<string, string> { ["weaponPower"] = "aslot.weapon.weaponPower" },
                },
            ],
        };
        var preActionResult = new BattleFormulaEngine(preActionSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(preActionResult.ShouldRewrite, "pre-action variables should drive action signal selection");
        Check(preActionResult.FinalDamage == 16, $"pre-action signal expected 16, got {preActionResult.FinalDamage}");

        var badActionConditionResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            ActionSignalRules =
            [
                new ActionSignalRule { ConditionFormula = "missingVariable + 1" },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badActionConditionResult.ShouldRewrite, "invalid action signal condition should skip rewrite");

        var badActionVariableResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 1,
                    VariableFormulas = new Dictionary<string, string> { ["bad"] = "missingVariable + 1" },
                },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badActionVariableResult.ShouldRewrite, "invalid action signal variable formula should skip rewrite");

        var healingActionSignalResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedHealing = true,
            FinalDamageFormula = "-action.healPower",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    ConditionFormula = "event.isHealing && vanillaHealing >= 10",
                    Signal = 9,
                    Variables = new Dictionary<string, int> { ["heal"] = 1 },
                    VariableFormulas = new Dictionary<string, string> { ["healPower"] = "vanillaHealing + t.ma" },
                },
            ],
        }, catalog).Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(healingActionSignalResult.ShouldRewrite, "healing formula action signal should rewrite");
        Check(healingActionSignalResult.FinalDamage == -13, $"healing formula action signal expected -13, got {healingActionSignalResult.FinalDamage}");
        Check(healingActionSignalResult.DesiredHp == 43, $"healing formula action signal desired HP expected 43, got {healingActionSignalResult.DesiredHp}");

        var eventKindActionSignalSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteObservedHealing = true,
            RewriteObservedMpLoss = true,
            FinalDamageFormula = "if(event.isHealing, -action.healPower, vanillaDamage + action.swing * 100 + action.spell * 1000)",
            FinalMpChangeFormula = "-min(previousMp, vanillaMpLoss + action.spell * 10 + action.swing * 100)",
            LogResolvedRuntimeContext = true,
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Controlled damage swing",
                    EventKind = "Damage",
                    ConditionFormula = "t.charId == 0x80",
                    Variables = new Dictionary<string, int> { ["swing"] = 1 },
                },
                new ActionSignalRule
                {
                    Name = "Controlled healing",
                    EventKind = "Healing",
                    Variables = new Dictionary<string, int> { ["heal"] = 1 },
                    VariableFormulas = new Dictionary<string, string> { ["healPower"] = "vanillaHealing + 1" },
                },
                new ActionSignalRule
                {
                    Name = "Controlled MP spell",
                    EventKind = "MpLoss",
                    Variables = new Dictionary<string, int> { ["spell"] = 1 },
                },
            ],
        };
        var eventKindDamageResult = new BattleFormulaEngine(eventKindActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker));
        Check(eventKindDamageResult.ShouldRewrite, "event-kind damage action signal should rewrite");
        Check(eventKindDamageResult.FinalDamage == 120, $"event-kind damage signal expected 120, got {eventKindDamageResult.FinalDamage}");
        Check(eventKindDamageResult.Trace.Contains("action=Controlled damage swing"), "event-kind damage trace should name the damage signal");

        var eventKindHealingResult = new BattleFormulaEngine(eventKindActionSignalSettings, catalog)
            .Evaluate(new DamageEvent(healedTarget, 30, 40, -10, attacker));
        Check(eventKindHealingResult.ShouldRewrite, "event-kind healing action signal should rewrite");
        Check(eventKindHealingResult.FinalDamage == -11, $"event-kind healing signal expected -11, got {eventKindHealingResult.FinalDamage}");
        Check(eventKindHealingResult.Trace.Contains("action=Controlled healing"), "event-kind healing trace should name the healing signal");

        var eventKindMpLossResult = new BattleFormulaEngine(eventKindActionSignalSettings, catalog)
            .EvaluateMp(new MpEvent(target with { Mp = 12 }, 20, 12, -8, attacker));
        Check(eventKindMpLossResult.ShouldRewrite, "event-kind MP action signal should rewrite");
        Check(eventKindMpLossResult.FinalMpChange == -18, $"event-kind MP signal expected -18, got {eventKindMpLossResult.FinalMpChange}");
        Check(eventKindMpLossResult.RuleName == "FinalMpChangeFormula", $"event-kind MP rule expected FinalMpChangeFormula, got {eventKindMpLossResult.RuleName}");

        var actionRuleSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    MinVanillaDamage = 9,
                    MaxVanillaDamage = 9,
                    Signal = 77,
                    Variables = new Dictionary<string, int>
                    {
                        ["thrust"] = 1,
                    },
                },
            ],
            DamageRules =
            [
                new DamageRule { Name = "Thrust action rule", RequiredActionVariable = "thrust", FinalDamage = 11 },
                new DamageRule { Name = "Fallback action rule", FinalDamage = 3 },
            ],
        };
        var thrustRuleResult = new BattleFormulaEngine(actionRuleSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 9, attacker));
        Check(thrustRuleResult.ShouldRewrite, "thrust action rule should rewrite");
        Check(thrustRuleResult.FinalDamage == 11, $"thrust action rule expected 11, got {thrustRuleResult.FinalDamage}");

        var fallbackRuleResult = new BattleFormulaEngine(actionRuleSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 8, attacker));
        Check(fallbackRuleResult.ShouldRewrite, "fallback action rule should rewrite");
        Check(fallbackRuleResult.FinalDamage == 3, $"fallback action rule expected 3, got {fallbackRuleResult.FinalDamage}");

        var typedDrSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "max(0, 20 - equipmentDr)",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Signal = 10,
                    Variables = new Dictionary<string, int> { ["cut"] = 1 },
                },
                new ActionSignalRule
                {
                    VanillaDamage = 8,
                    Signal = 11,
                    Variables = new Dictionary<string, int> { ["impale"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Name = "Body armor cut DR",
                    Slot = "Body",
                    ItemCategory = "Armor",
                    RequiredActionVariable = "cut",
                    DamageReduction = 5,
                },
                new EquipmentDrRule
                {
                    Name = "Body armor impale DR",
                    Slot = "Body",
                    ItemCategory = "Armor",
                    ActionSignal = 11,
                    DamageReduction = 2,
                },
            ],
        };
        var cutDrResult = new BattleFormulaEngine(typedDrSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(cutDrResult.ShouldRewrite, "cut typed DR should rewrite");
        Check(cutDrResult.FinalDamage == 15, $"cut typed DR expected final damage 15, got {cutDrResult.FinalDamage}");

        var impaleDrResult = new BattleFormulaEngine(typedDrSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 42, 8, attacker));
        Check(impaleDrResult.ShouldRewrite, "impale typed DR should rewrite");
        Check(impaleDrResult.FinalDamage == 18, $"impale typed DR expected final damage 18, got {impaleDrResult.FinalDamage}");

        var untypedDrResult = new BattleFormulaEngine(typedDrSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 41, 9, attacker));
        Check(untypedDrResult.ShouldRewrite, "untyped DR should rewrite");
        Check(untypedDrResult.FinalDamage == 20, $"untyped DR expected final damage 20, got {untypedDrResult.FinalDamage}");

        var conditionalDrSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "20 - equipmentDr",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Variables = new Dictionary<string, int> { ["cut"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Name = "Cut armor condition",
                    Slot = "Body",
                    ItemCategory = "Armor",
                    ConditionFormula = "action.cut && item.armorHpBonus >= 10",
                    DamageReduction = 6,
                },
            ],
        };
        var conditionalDrResult = new BattleFormulaEngine(conditionalDrSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(conditionalDrResult.ShouldRewrite, "conditional DR should rewrite");
        Check(conditionalDrResult.FinalDamage == 14, $"conditional DR expected final damage 14, got {conditionalDrResult.FinalDamage}");

        var conditionalDrNoActionResult = new BattleFormulaEngine(conditionalDrSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 41, 9, attacker));
        Check(conditionalDrNoActionResult.ShouldRewrite, "conditional DR no-action fallback should rewrite");
        Check(conditionalDrNoActionResult.FinalDamage == 20, $"conditional DR no-action expected final damage 20, got {conditionalDrNoActionResult.FinalDamage}");

        var rewriteConditionSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteConditionFormula = "action.swing && equipmentDr >= 1",
            FinalDamageFormula = "99",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Variables = new Dictionary<string, int> { ["swing"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Name = "Any body armor DR",
                    Slot = "Body",
                    SecondaryKind = "armor",
                    DamageReductionFormula = "max(1, item.armorHpBonus / 20)",
                },
            ],
        };
        var rewriteConditionAllowed = new BattleFormulaEngine(rewriteConditionSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(rewriteConditionAllowed.ShouldRewrite, "rewrite condition should allow matching actions");
        Check(rewriteConditionAllowed.FinalDamage == 99, $"rewrite condition allowed expected 99, got {rewriteConditionAllowed.FinalDamage}");

        var rewriteConditionBlocked = new BattleFormulaEngine(rewriteConditionSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 41, 9, attacker));
        Check(!rewriteConditionBlocked.ShouldRewrite, "rewrite condition should block unmatched actions");
        Check(rewriteConditionBlocked.RuleName == "RewriteConditionFormula=0", $"rewrite condition blocked reason expected RewriteConditionFormula=0, got {rewriteConditionBlocked.RuleName}");
        Check(rewriteConditionBlocked.DesiredHp == 41, $"rewrite condition blocked should preserve current HP 41, got {rewriteConditionBlocked.DesiredHp}");

        var badRewriteConditionResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            RewriteConditionFormula = "missingGateValue + 1",
            FinalDamageFormula = "99",
        }, catalog).Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(!badRewriteConditionResult.ShouldRewrite, "invalid rewrite condition should skip rewrite");
        Check(badRewriteConditionResult.RuleName.Contains("RewriteConditionFormula"), "invalid rewrite condition should name the gate");

        var responseVariableSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "mulDiv(200, damageResponse.permille, 1000)",
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Variables = new Dictionary<string, int> { ["swing"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            DamageResponseRules =
            [
                new DamageResponseRule
                {
                    Name = "Leather swing response",
                    Slot = "Body",
                    ItemId = 172,
                    RequiredActionVariable = "swing",
                    MultiplierPermille = 950,
                },
            ],
        };
        var responseVariableResult = new BattleFormulaEngine(responseVariableSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(responseVariableResult.ShouldRewrite, "manual damage response formula should rewrite");
        Check(responseVariableResult.FinalDamage == 190, $"manual damage response expected 190, got {responseVariableResult.FinalDamage}");

        var runtimeTraceSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            LogResolvedRuntimeContext = true,
            FinalDamageFormula = "200",
            FormulaTraceVariables =
            [
                new FormulaDerivedVariable { Name = "gross", Formula = "200" },
                new FormulaDerivedVariable { Name = "result.final", Formula = "result.finalDamage" },
                new FormulaDerivedVariable { Name = "armor.hp", Formula = "slot.body.armorHpBonus" },
                new FormulaDerivedVariable { Name = "bad", Formula = "missingTraceValue + 1" },
            ],
            ApplyDamageResponseRules = true,
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Runtime trace swing",
                    VanillaDamage = 7,
                    Signal = 101,
                    Variables = new Dictionary<string, int> { ["swing"] = 1 },
                },
            ],
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" }
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            DamageResponseRules =
            [
                new DamageResponseRule
                {
                    Name = "Leather swing response",
                    Slot = "Body",
                    ItemId = 172,
                    RequiredActionVariable = "swing",
                    MultiplierPermille = 950,
                },
            ],
        };
        var runtimeTraceResult = new BattleFormulaEngine(runtimeTraceSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker, "test-attacker"));
        Check(runtimeTraceResult.ShouldRewrite, "runtime trace should still rewrite");
        Check(runtimeTraceResult.FinalDamage == 190, $"runtime trace final damage expected 190, got {runtimeTraceResult.FinalDamage}");
        Check(runtimeTraceResult.Trace.Contains("attacker=0x1000:test-attacker"), "runtime trace should include attacker source");
        Check(runtimeTraceResult.Trace.Contains("action=Runtime trace swing:source=vanilla-damage:signal=101:vars=swing=1"), "runtime trace should include action variables");
        Check(runtimeTraceResult.Trace.Contains("targetSlots=body(present,id=172:"), "runtime trace should include target body item");
        Check(runtimeTraceResult.Trace.Contains("attackerSlots=weapon(present,id=19:"), "runtime trace should include attacker weapon item");
        Check(runtimeTraceResult.Trace.Contains("response=raw950/permille950/rules1/clamped0:"), "runtime trace should include response details");
        Check(runtimeTraceResult.Trace.Contains("vars=gross=200,result.final=190,armor.hp=10,bad=ERR("), "runtime trace should include formula trace variables and non-fatal trace errors");
        Check(runtimeTraceResult.Trace.Contains("final=190:FinalDamageFormula+DamageResponse(Leather swing response)"), "runtime trace should include final rule");

        var clampedResponseVariableSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "damageResponse.rawPermille + damageResponse.permille + damageResponse.clamped",
            MaxDamageResponsePermille = 2500,
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "Oversized global response", MultiplierPermille = 3000 },
            ],
        };
        var clampedResponseVariableResult = new BattleFormulaEngine(clampedResponseVariableSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(clampedResponseVariableResult.ShouldRewrite, "clamped damage response variables should rewrite");
        Check(clampedResponseVariableResult.FinalDamage == 5501, $"clamped response variables expected 5501, got {clampedResponseVariableResult.FinalDamage}");

        var autoResponseSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "200",
            ApplyDamageResponseRules = true,
            MaxDamageResponsePermille = 2500,
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "Auto capped response", MultiplierPermille = 3000 },
            ],
        };
        var autoResponseResult = new BattleFormulaEngine(autoResponseSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(autoResponseResult.ShouldRewrite, "auto damage response should rewrite");
        Check(autoResponseResult.FinalDamage == 500, $"auto damage response expected 500, got {autoResponseResult.FinalDamage}");

        var responseChipFloorSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            ApplyDamageResponseRules = true,
            MinDamageResponsePermille = 250,
            MaxDamageResponsePermille = 2500,
            DamageResponseChipFloor = 1,
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "Chip floor response", MultiplierPermille = 250 },
            ],
        };
        var responseChipFloorResult = new BattleFormulaEngine(responseChipFloorSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(responseChipFloorResult.ShouldRewrite, "damage response chip floor should rewrite");
        Check(responseChipFloorResult.FinalDamage == 1, $"damage response chip floor expected 1, got {responseChipFloorResult.FinalDamage}");

        var plateTarget = TargetWithBodyItem(177, (nint)0x2100); // Plate Mail.
        var mailTarget = TargetWithBodyItem(175, (nint)0x2200);  // Chainmail.
        var v02ResponseSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "100",
            ApplyDamageResponseRules = true,
            MinDamageResponsePermille = 250,
            MaxDamageResponsePermille = 2500,
            DamageResponseChipFloor = 1,
            ActionSignalRules =
            [
                new ActionSignalRule { VanillaDamage = 7, Variables = new Dictionary<string, int> { ["swing"] = 1 } },
                new ActionSignalRule { VanillaDamage = 8, Variables = new Dictionary<string, int> { ["thrust"] = 1 } },
                new ActionSignalRule { VanillaDamage = 9, Variables = new Dictionary<string, int> { ["crush"] = 1 } },
                new ActionSignalRule { VanillaDamage = 10, Variables = new Dictionary<string, int> { ["missile"] = 1 } },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "plate swing", Slot = "Body", ItemId = 177, RequiredActionVariable = "swing", MultiplierPermille = 650 },
                new DamageResponseRule { Name = "plate thrust", Slot = "Body", ItemId = 177, RequiredActionVariable = "thrust", MultiplierPermille = 650 },
                new DamageResponseRule { Name = "plate crush", Slot = "Body", ItemId = 177, RequiredActionVariable = "crush", MultiplierPermille = 1150 },
                new DamageResponseRule { Name = "plate missile", Slot = "Body", ItemId = 177, RequiredActionVariable = "missile", MultiplierPermille = 800 },
                new DamageResponseRule { Name = "mail swing", Slot = "Body", ItemId = 175, RequiredActionVariable = "swing", MultiplierPermille = 750 },
                new DamageResponseRule { Name = "mail thrust", Slot = "Body", ItemId = 175, RequiredActionVariable = "thrust", MultiplierPermille = 1100 },
                new DamageResponseRule { Name = "mail crush", Slot = "Body", ItemId = 175, RequiredActionVariable = "crush", MultiplierPermille = 950 },
                new DamageResponseRule { Name = "mail missile", Slot = "Body", ItemId = 175, RequiredActionVariable = "missile", MultiplierPermille = 1100 },
            ],
        };
        var plateSwingResult = new BattleFormulaEngine(v02ResponseSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 43, 7, attacker));
        Check(plateSwingResult.ShouldRewrite, "v0.2 plate swing response should rewrite");
        Check(plateSwingResult.FinalDamage == 65, $"v0.2 plate swing expected 65, got {plateSwingResult.FinalDamage}");

        var plateCrushResult = new BattleFormulaEngine(v02ResponseSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 41, 9, attacker));
        Check(plateCrushResult.ShouldRewrite, "v0.2 plate crush response should rewrite");
        Check(plateCrushResult.FinalDamage == 115, $"v0.2 plate crush expected 115, got {plateCrushResult.FinalDamage}");

        var mailThrustResult = new BattleFormulaEngine(v02ResponseSettings, catalog)
            .Evaluate(new DamageEvent(mailTarget, 50, 42, 8, attacker));
        Check(mailThrustResult.ShouldRewrite, "v0.2 mail thrust response should rewrite");
        Check(mailThrustResult.FinalDamage == 110, $"v0.2 mail thrust expected 110, got {mailThrustResult.FinalDamage}");

        var armorClassSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "armor.plate * 10 + armor.mail * 2 + armor.light",
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            FormulaPreResponseVariables =
            [
                new FormulaDerivedVariable { Name = "armor.plate", Formula = "slot.body.category_armor && slot.body.armorHpBonus >= 60" },
                new FormulaDerivedVariable { Name = "armor.mail", Formula = "slot.body.category_armor && slot.body.armorHpBonus >= 40 && slot.body.armorHpBonus < 60" },
                new FormulaDerivedVariable { Name = "armor.light", Formula = "slot.body.category_clothing || slot.body.category_robe || (slot.body.category_armor && slot.body.armorHpBonus < 40)" },
            ],
        };
        var plateClassResult = new BattleFormulaEngine(armorClassSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 43, 7, attacker));
        Check(plateClassResult.ShouldRewrite, "pre-response plate class formula should rewrite");
        Check(plateClassResult.FinalDamage == 10, $"pre-response plate class expected 10, got {plateClassResult.FinalDamage}");

        var mailClassResult = new BattleFormulaEngine(armorClassSettings, catalog)
            .Evaluate(new DamageEvent(mailTarget, 50, 43, 7, attacker));
        Check(mailClassResult.ShouldRewrite, "pre-response mail class formula should rewrite");
        Check(mailClassResult.FinalDamage == 2, $"pre-response mail class expected 2, got {mailClassResult.FinalDamage}");

        var preResponseDrSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "20 - equipmentDr",
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            FormulaPreResponseVariables =
            [
                new FormulaDerivedVariable { Name = "armor.plate", Formula = "slot.body.category_armor && slot.body.armorHpBonus >= 60" },
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Name = "Rare plate DR",
                    Slot = "Body",
                    ItemCategory = "Armor",
                    ConditionFormula = "armor.plate",
                    DamageReduction = 7,
                },
            ],
        };
        var preResponseDrResult = new BattleFormulaEngine(preResponseDrSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 43, 7, attacker));
        Check(preResponseDrResult.ShouldRewrite, "pre-response variable should be available to DR rules");
        Check(preResponseDrResult.FinalDamage == 13, $"pre-response DR expected 13, got {preResponseDrResult.FinalDamage}");

        var v02ClassResponseSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "100",
            ApplyDamageResponseRules = true,
            MinDamageResponsePermille = 250,
            MaxDamageResponsePermille = 2500,
            DamageResponseChipFloor = 1,
            ActionSignalRules =
            [
                new ActionSignalRule { VanillaDamage = 7, Variables = new Dictionary<string, int> { ["swing"] = 1 } },
                new ActionSignalRule { VanillaDamage = 8, Variables = new Dictionary<string, int> { ["thrust"] = 1 } },
                new ActionSignalRule { VanillaDamage = 9, Variables = new Dictionary<string, int> { ["crush"] = 1 } },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            FormulaPreResponseVariables =
            [
                new FormulaDerivedVariable { Name = "armor.plate", Formula = "slot.body.category_armor && slot.body.armorHpBonus >= 60" },
                new FormulaDerivedVariable { Name = "armor.mail", Formula = "slot.body.category_armor && slot.body.armorHpBonus >= 40 && slot.body.armorHpBonus < 60" },
            ],
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "plate swing", ConditionFormula = "armor.plate && action.swing", MultiplierPermille = 650 },
                new DamageResponseRule { Name = "plate thrust", ConditionFormula = "armor.plate && action.thrust", MultiplierPermille = 650 },
                new DamageResponseRule { Name = "plate crush", ConditionFormula = "armor.plate && action.crush", MultiplierPermille = 1150 },
                new DamageResponseRule { Name = "mail swing", ConditionFormula = "armor.mail && action.swing", MultiplierPermille = 750 },
                new DamageResponseRule { Name = "mail thrust", ConditionFormula = "armor.mail && action.thrust", MultiplierPermille = 1100 },
            ],
        };
        var classPlateSwingResult = new BattleFormulaEngine(v02ClassResponseSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 43, 7, attacker));
        Check(classPlateSwingResult.ShouldRewrite, "v0.2 class plate swing should rewrite");
        Check(classPlateSwingResult.FinalDamage == 65, $"v0.2 class plate swing expected 65, got {classPlateSwingResult.FinalDamage}");

        var classPlateCrushResult = new BattleFormulaEngine(v02ClassResponseSettings, catalog)
            .Evaluate(new DamageEvent(plateTarget, 50, 41, 9, attacker));
        Check(classPlateCrushResult.ShouldRewrite, "v0.2 class plate crush should rewrite");
        Check(classPlateCrushResult.FinalDamage == 115, $"v0.2 class plate crush expected 115, got {classPlateCrushResult.FinalDamage}");

        var classMailThrustResult = new BattleFormulaEngine(v02ClassResponseSettings, catalog)
            .Evaluate(new DamageEvent(mailTarget, 50, 42, 8, attacker));
        Check(classMailThrustResult.ShouldRewrite, "v0.2 class mail thrust should rewrite");
        Check(classMailThrustResult.FinalDamage == 110, $"v0.2 class mail thrust expected 110, got {classMailThrustResult.FinalDamage}");

        var badPreResponseResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            FormulaPreResponseVariables =
            [
                new FormulaDerivedVariable { Name = "bad", Formula = "missingVariable + 1" },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badPreResponseResult.ShouldRewrite, "invalid pre-response variable should skip rewrite");

        var badResponseFormulaResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            DamageResponseRules =
            [
                new DamageResponseRule { Name = "Bad response formula", MultiplierFormula = "missingVariable + 1" },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badResponseFormulaResult.ShouldRewrite, "invalid damage response formula should skip rewrite");

        var badDrConditionResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    ConditionFormula = "missingVariable + 1",
                    DamageReduction = 1,
                },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badDrConditionResult.ShouldRewrite, "invalid equipment DR condition should skip rewrite");

        var gurpsWoundingSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "mulDiv(max(0, grossDamage - equipmentDr), if(action.cut, 3, if(action.impale, 2, 1)), if(action.cut, 2, 1))",
            FormulaVariables = new Dictionary<string, int> { ["grossDamage"] = 20 },
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Signal = 10,
                    Variables = new Dictionary<string, int> { ["cut"] = 1 },
                },
                new ActionSignalRule
                {
                    VanillaDamage = 8,
                    Signal = 11,
                    Variables = new Dictionary<string, int> { ["impale"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    RequiredActionVariable = "cut",
                    DamageReduction = 5,
                },
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    RequiredActionVariable = "impale",
                    DamageReduction = 2,
                },
            ],
        };
        var cutWoundingResult = new BattleFormulaEngine(gurpsWoundingSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(cutWoundingResult.ShouldRewrite, "cut wounding formula should rewrite");
        Check(cutWoundingResult.FinalDamage == 22, $"cut wounding expected final damage 22, got {cutWoundingResult.FinalDamage}");

        var impaleWoundingResult = new BattleFormulaEngine(gurpsWoundingSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 42, 8, attacker));
        Check(impaleWoundingResult.ShouldRewrite, "impale wounding formula should rewrite");
        Check(impaleWoundingResult.FinalDamage == 36, $"impale wounding expected final damage 36, got {impaleWoundingResult.FinalDamage}");

        var divZeroResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "mulDiv(1, 1, 0)",
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!divZeroResult.ShouldRewrite, "division by zero formula should skip rewrite");

        var derivedGurpsSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "finalDamage",
            FormulaVariables = new Dictionary<string, int> { ["grossDamage"] = 20 },
            FormulaDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "penetrating", Formula = "max(0, grossDamage - equipmentDr)" },
                new FormulaDerivedVariable { Name = "wound.num", Formula = "if(action.cut, 3, if(action.impale, 2, 1))" },
                new FormulaDerivedVariable { Name = "wound.den", Formula = "if(action.cut, 2, 1)" },
                new FormulaDerivedVariable { Name = "finalDamage", Formula = "mulDiv(penetrating, wound.num, wound.den)" },
            ],
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    VanillaDamage = 7,
                    Variables = new Dictionary<string, int> { ["cut"] = 1 },
                },
                new ActionSignalRule
                {
                    VanillaDamage = 8,
                    Variables = new Dictionary<string, int> { ["impale"] = 1 },
                },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    RequiredActionVariable = "cut",
                    DamageReduction = 5,
                },
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    RequiredActionVariable = "impale",
                    DamageReduction = 2,
                },
            ],
        };
        var derivedCutResult = new BattleFormulaEngine(derivedGurpsSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 43, 7, attacker));
        Check(derivedCutResult.ShouldRewrite, "derived GURPS cut formula should rewrite");
        Check(derivedCutResult.FinalDamage == 22, $"derived GURPS cut expected final damage 22, got {derivedCutResult.FinalDamage}");

        var derivedImpaleResult = new BattleFormulaEngine(derivedGurpsSettings, catalog)
            .Evaluate(new DamageEvent(target, 50, 42, 8, attacker));
        Check(derivedImpaleResult.ShouldRewrite, "derived GURPS impale formula should rewrite");
        Check(derivedImpaleResult.FinalDamage == 36, $"derived GURPS impale expected final damage 36, got {derivedImpaleResult.FinalDamage}");

        var badDerivedResult = new BattleFormulaEngine(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "1",
            FormulaDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "bad", Formula = "missingVariable + 1" },
            ],
        }, catalog).Evaluate(new DamageEvent(target, 50, 49, 1, attacker));
        Check(!badDerivedResult.ShouldRewrite, "invalid derived variable should skip rewrite");

        var rolledDerivedSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "finalDamage",
            FormulaDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "grossDamage", Formula = "diceRoll(2, 6, 0)" },
                new FormulaDerivedVariable { Name = "finalDamage", Formula = "max(0, grossDamage - equipmentDr)" },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" }
            ],
            EquipmentDrRules =
            [
                new EquipmentDrRule
                {
                    Slot = "Body",
                    ItemCategory = "Armor",
                    DamageReduction = 3,
                },
            ],
        };
        var rolledEvent = new DamageEvent(target, 50, 49, 1, attacker, EventIndex: 77, EventSeed: 0xBEEFL);
        var rolledDerivedA = new BattleFormulaEngine(rolledDerivedSettings, catalog).Evaluate(rolledEvent);
        var rolledDerivedB = new BattleFormulaEngine(rolledDerivedSettings, catalog).Evaluate(rolledEvent);
        Check(rolledDerivedA.ShouldRewrite && rolledDerivedB.ShouldRewrite, "rolled derived formula should rewrite");
        Check(rolledDerivedA.FinalDamage == rolledDerivedB.FinalDamage, "same event should produce same rolled derived final damage");
        Check(rolledDerivedA.FinalDamage is >= 0 and <= 9, $"rolled derived final damage should be in 0..9, got {rolledDerivedA.FinalDamage}");

        long now = Stopwatch.GetTimestamp();
        long TickAgo(int ms) => now - (long)(ms * Stopwatch.Frequency / 1000.0);
        var sameTeamRecent = new UnitSnapshot((nint)0x3000, 0x82, 5, 25, 25, target.Team, target.IsFoe, 5, 3, 7, 4, 3, 60, 60, new byte[0x180]);
        var resolverObservations = new Dictionary<nint, UnitObservation>
        {
            [target.Ptr] = new UnitObservation(target, now),
            [sameTeamRecent.Ptr] = new UnitObservation(sameTeamRecent, TickAgo(10)),
            [attacker.Ptr] = new UnitObservation(attacker, TickAgo(250)),
        };
        var passiveResolver = new BattleContextResolver(new RuntimeSettings
        {
            InferAttackerFromRecentUnits = false,
            RecentAttackerWindowMs = 1000,
            PreferOpposingTeamAttacker = true,
        }).ResolveRecentAttacker(target, resolverObservations, now);
        Check(passiveResolver.Unit is null, "passive recent-attacker resolver should not inject attacker");
        Check(passiveResolver.Summary.Contains("ptr=0x1000"), "passive resolver should still summarize attacker candidate");

        var activeResolver = new BattleContextResolver(new RuntimeSettings
        {
            InferAttackerFromRecentUnits = true,
            RecentAttackerWindowMs = 1000,
            PreferOpposingTeamAttacker = true,
        }).ResolveRecentAttacker(target, resolverObservations, now);
        Check(activeResolver.Unit?.Ptr == attacker.Ptr, "active resolver should prefer opposing recent unit over same-team unit");
        Check(activeResolver.Source == "recent-unit", $"active resolver source expected recent-unit, got {activeResolver.Source}");

        // CT-based attacker resolution (live-proven: attacker = the unit whose CT (+0x41) just reset).
        var ctTarget = new UnitSnapshot((nint)0x4100, 0x1E, 12, 322, 360, 0, false, 12, 8, 12, 4, 3, 70, 60, new byte[0x180], Ct: 84);
        var ctJustActed = new UnitSnapshot((nint)0x4200, 0x80, 12, 250, 250, 0, false, 16, 6, 16, 4, 3, 70, 60, new byte[0x180], Ct: 12);
        var ctCharging = new UnitSnapshot((nint)0x4300, 0x01, 14, 297, 567, 0, false, 18, 7, 10, 4, 3, 75, 65, new byte[0x180], Ct: 70);
        var ctSettings = new RuntimeSettings { ResolveAttackerByCt = true, CtDropWindowMs = 4000, InferAttackerFromRecentUnits = false };
        var ctObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, now),
            [ctJustActed.Ptr] = new UnitObservation(ctJustActed, TickAgo(300), CtDropTick: TickAgo(40), CtDropAmount: 95),
            [ctCharging.Ptr] = new UnitObservation(ctCharging, TickAgo(300)), // still charging, no CT drop
        };
        var ctResolved = new BattleContextResolver(ctSettings).ResolveRecentAttacker(ctTarget, ctObs, now);
        Check(ctResolved.Unit?.Ptr == ctJustActed.Ptr, $"CT resolver should pick the unit whose CT just reset (0x4200), got 0x{ctResolved.Unit?.Ptr:X}");
        Check(ctResolved.Source == "ct-reset", $"CT resolver source expected ct-reset, got {ctResolved.Source}");
        Check(ctResolved.Summary.Contains("CT="), "CT resolver summary should list ctCandidates with CT values");

        // Tiebreak: two units both at CT 0 (queue drain) -> the one that dropped most recently wins
        // (mirrors the live Cloud-vs-Ramza case where both read CT=0 but Cloud had just acted).
        var ctActedEarlier = new UnitSnapshot((nint)0x4400, 0x01, 14, 280, 567, 0, false, 18, 7, 10, 4, 3, 75, 65, new byte[0x180], Ct: 0);
        var ctActedNow = new UnitSnapshot((nint)0x4500, 0x32, 12, 304, 360, 0, false, 9, 6, 9, 4, 3, 70, 60, new byte[0x180], Ct: 0);
        var ctTieObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, now),
            [ctActedEarlier.Ptr] = new UnitObservation(ctActedEarlier, TickAgo(300), CtDropTick: TickAgo(220), CtDropAmount: 100),
            [ctActedNow.Ptr] = new UnitObservation(ctActedNow, TickAgo(300), CtDropTick: TickAgo(30), CtDropAmount: 100),
        };
        var ctTieResolved = new BattleContextResolver(ctSettings).ResolveRecentAttacker(ctTarget, ctTieObs, now);
        Check(ctTieResolved.Unit?.Ptr == ctActedNow.Ptr, $"CT resolver tiebreak should pick the most recent CT drop (0x4500), got 0x{ctTieResolved.Unit?.Ptr:X}");

        // A CT drop older than the window must be ignored, falling through to the legacy path (none here).
        var ctStaleObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, now),
            [ctActedNow.Ptr] = new UnitObservation(ctActedNow, TickAgo(9000), CtDropTick: TickAgo(9000), CtDropAmount: 100),
        };
        var ctStaleResolved = new BattleContextResolver(ctSettings).ResolveRecentAttacker(ctTarget, ctStaleObs, now);
        Check(ctStaleResolved.Unit is null, "CT resolver should ignore a stale CT drop (older than CtDropWindowMs) and fall through");

        // If the exact drop was missed, a recently hook-touched unit with near-reset CT is still a
        // useful conservative fallback for live tests. Poll-only/stale low-CT units stay ignored.
        var ctLowSettings = new RuntimeSettings
        {
            ResolveAttackerByCt = true,
            ResolveAttackerByLowCtFallback = true,
            CtLowFallbackMaxCt = 25,
            CtLowFallbackWindowMs = 1000,
            InferAttackerFromRecentUnits = false,
        };
        var ctLowObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, now),
            [ctJustActed.Ptr] = new UnitObservation(ctJustActed with { Ct = 8 }, TickAgo(120)),
            [ctCharging.Ptr] = new UnitObservation(ctCharging, TickAgo(20)), // recent but CT too high
        };
        var ctLowResolved = new BattleContextResolver(ctLowSettings).ResolveRecentAttacker(ctTarget, ctLowObs, now);
        Check(ctLowResolved.Unit?.Ptr == ctJustActed.Ptr, $"CT-low fallback should pick the recent low-CT unit (0x4200), got 0x{ctLowResolved.Unit?.Ptr:X}");
        Check(ctLowResolved.Source == "ct-low", $"CT-low fallback source expected ct-low, got {ctLowResolved.Source}");
        Check(ctLowResolved.Summary.Contains("ctLowCandidates="), "CT-low fallback summary should list low-CT candidates");

        var ctLowStaleObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, now),
            [ctJustActed.Ptr] = new UnitObservation(ctJustActed with { Ct = 8 }, TickAgo(2000)),
        };
        var ctLowStaleResolved = new BattleContextResolver(ctLowSettings).ResolveRecentAttacker(ctTarget, ctLowStaleObs, now);
        Check(ctLowStaleResolved.Unit is null, "CT-low fallback should ignore stale low-CT candidates");
        Check(ctLowStaleResolved.Summary.Contains("ctCandidates=none"), "CT miss summary should say when no CT-drop candidates exist");

        // Counterattacks do not reset CT. If B was just damaged by A and then A takes damage, resolve B
        // by inverting the previous resolved damage pair.
        var counterResolver = new BattleContextResolver(new RuntimeSettings
        {
            ResolveAttackerByCt = true,
            ResolveCounterFromRecentDamage = true,
            CounterEventWindowMs = 1500,
            InferAttackerFromRecentUnits = false,
        });
        counterResolver.RememberHpDamageEvent(ctTarget, ctJustActed, "ct-reset", signedDamage: 12, eventTick: TickAgo(100), eventIndex: 700);
        var counterObs = new Dictionary<nint, UnitObservation>
        {
            [ctTarget.Ptr] = new UnitObservation(ctTarget, TickAgo(400)),       // previous victim B, no CT drop
            [ctJustActed.Ptr] = new UnitObservation(ctJustActed, TickAgo(400)), // now target A, excluded
        };
        var counterResolved = counterResolver.ResolveRecentAttacker(ctJustActed, counterObs, now);
        Check(counterResolved.Unit?.Ptr == ctTarget.Ptr, $"counter inversion should resolve previous target as attacker (0x4100), got 0x{counterResolved.Unit?.Ptr:X}");
        Check(counterResolved.Source == "counter-inversion", $"counter inversion source expected counter-inversion, got {counterResolved.Source}");
        Check(counterResolved.Summary.Contains("counterPrevious=event#700"), "counter inversion summary should include previous event id");

        var staleCounterResolver = new BattleContextResolver(new RuntimeSettings
        {
            ResolveAttackerByCt = true,
            ResolveCounterFromRecentDamage = true,
            CounterEventWindowMs = 50,
            InferAttackerFromRecentUnits = false,
        });
        staleCounterResolver.RememberHpDamageEvent(ctTarget, ctJustActed, "ct-reset", signedDamage: 12, eventTick: TickAgo(100), eventIndex: 701);
        var staleCounterResolved = staleCounterResolver.ResolveRecentAttacker(ctJustActed, counterObs, now);
        Check(staleCounterResolved.Unit is null, "counter inversion should ignore an event older than CounterEventWindowMs");

        byte[] ActionRaw(
            int pendingFlag = 0,
            int pendingTimer = 0xFF,
            int actionId = 0,
            int forecastDamage = 0,
            int forecastCharge = 0,
            int forecastFlag = 0,
            int pendingFlag2 = 0,
            int phaseMarker = 0)
        {
            var raw = new byte[0x200];
            raw[0x61] = (byte)pendingFlag;
            raw[0x18D] = (byte)pendingTimer;
            raw[0x1A2] = (byte)(actionId & 0xFF);
            raw[0x1A3] = (byte)((actionId >> 8) & 0xFF);
            raw[0x1C4] = (byte)(forecastDamage & 0xFF);
            raw[0x1C5] = (byte)((forecastDamage >> 8) & 0xFF);
            raw[0x1D8] = (byte)forecastCharge;
            raw[0x1E5] = (byte)forecastFlag;
            raw[0x1EF] = (byte)pendingFlag2;
            raw[0x1BB] = (byte)phaseMarker;
            return raw;
        }

        long TickAfter(int ms) => now + (long)(ms * Stopwatch.Frequency / 1000.0);
        var pendingTrackerSettings = new RuntimeSettings
        {
            TrackPendingActions = true,
            PendingActionResolveWindowMs = 1500,
            PendingActionMaxBatchEvents = 8,
            PendingActionStaleMs = 10_000,
        };
        var pendingTracker = new PendingActionTracker();
        var cloudPending = new UnitSnapshot(
            (nint)0x6000, 0x32, 12, 428, 428, 0, false, 9, 6, 9, 4, 3, 70, 60,
            ActionRaw(pendingFlag: 8, pendingTimer: 3, actionId: 258, pendingFlag2: 8),
            Ct: 46);
        var pendingEnter = pendingTracker.ObserveUnit(
            cloudPending,
            ActionProbeState.From(cloudPending),
            pendingTrackerSettings,
            TickAfter(0),
            touchForContext: false);
        Check(pendingEnter.Any(line => line.Contains("enter") && line.Contains("act=258")), "pending tracker should record Cloud entering Cross Slash pending state");

        var cloudTimerLow = cloudPending with
        {
            Raw = ActionRaw(pendingFlag: 8, pendingTimer: 1, actionId: 258, pendingFlag2: 8),
        };
        var pendingUpdate = pendingTracker.ObserveUnit(
            cloudTimerLow,
            ActionProbeState.From(cloudTimerLow),
            pendingTrackerSettings,
            TickAfter(250),
            touchForContext: false);
        Check(pendingUpdate.Any(line => line.Contains("update") && line.Contains("t18D=1")), "pending tracker should log pending timer changes");

        var cloudPreClear = cloudPending with
        {
            Raw = ActionRaw(pendingFlag: 8, pendingTimer: 0xFF, actionId: 258, pendingFlag2: 8),
        };
        pendingTracker.ObserveUnit(cloudPreClear, ActionProbeState.From(cloudPreClear), pendingTrackerSettings, TickAfter(500), touchForContext: false);

        var cloudCleared = cloudPending with
        {
            Raw = ActionRaw(pendingFlag: 0, pendingTimer: 0xFF, actionId: 258, pendingFlag2: 0),
        };
        var resolveOpen = pendingTracker.ObserveUnit(
            cloudCleared,
            ActionProbeState.From(cloudCleared),
            pendingTrackerSettings,
            TickAfter(525),
            touchForContext: false);
        Check(resolveOpen.Any(line => line.Contains("resolve-open") && line.Contains("act=258")), "pending tracker should open a resolving batch when pending flags clear");

        var agriasHit = new UnitSnapshot(
            (nint)0x6100, 0x1E, 12, 207, 322, 0, false, 11, 7, 12, 4, 3, 70, 63,
            ActionRaw(forecastDamage: 115, forecastCharge: 2, forecastFlag: 128, phaseMarker: 2),
            Ct: 100);
        var agriasPreClampCandidate = pendingTracker.MatchTargetCache(
            "preclamp-cache",
            899,
            agriasHit,
            ActionProbeState.From(agriasHit),
            pendingTrackerSettings,
            TickAfter(545));
        Check(agriasPreClampCandidate.Lines.Any(line => line.Contains("resolved=0x6000") && line.Contains("consume=0")), "pending tracker target-cache match should resolve without consuming a batch event");
        Check(agriasPreClampCandidate.Match?.BatchEvent == 0, $"pending tracker target-cache match expected preview batch event 0, got {agriasPreClampCandidate.Match?.BatchEvent}");

        var agriasMatch = pendingTracker.MatchHpEvent(
            "damage",
            900,
            agriasHit,
            ActionProbeState.From(agriasHit),
            observedHpLoss: 115,
            pendingTrackerSettings,
            TickAfter(550));
        Check(agriasMatch.Lines.Any(line => line.Contains("resolved=0x6000") && line.Contains("batch=1") && line.Contains("confidence=damage-cache")), "pending tracker should attribute Agrias damage to Cloud's resolving Cross Slash batch");
        Check(agriasMatch.Match is not null, "pending tracker should return structured action context for Agrias damage");
        Check(agriasMatch.Match?.Caster.Ptr == cloudPending.Ptr, "pending tracker structured match should identify Cloud as caster");
        Check(agriasMatch.Match?.Source == "pending-clear", $"pending tracker structured match expected source pending-clear, got {agriasMatch.Match?.Source}");
        Check(agriasMatch.Match?.ActionId == 258, $"pending tracker structured match expected action 258, got {agriasMatch.Match?.ActionId}");
        Check(agriasMatch.Match?.CurrentDamageCacheMatches == true, "pending tracker structured match should carry current damage-cache confidence");
        Check(agriasMatch.Match?.HasDamageCacheMatch == true, "pending tracker structured match should mark cache-backed matches as usable for primary context");
        Check(agriasMatch.Match?.CurrentTargetCacheDamage == 115, $"pending tracker structured match expected target cache damage 115, got {agriasMatch.Match?.CurrentTargetCacheDamage}");

        var ninjaHit = new UnitSnapshot(
            (nint)0x6200, 0x80, 12, 3, 276, 0, false, 15, 6, 16, 4, 3, 70, 60,
            ActionRaw(forecastDamage: 273, forecastCharge: 2, forecastFlag: 136, phaseMarker: 2),
            Ct: 100);
        var ninjaMatch = pendingTracker.MatchHpEvent(
            "damage",
            901,
            ninjaHit,
            ActionProbeState.From(ninjaHit),
            observedHpLoss: 273,
            pendingTrackerSettings,
            TickAfter(575));
        Check(ninjaMatch.Lines.Any(line => line.Contains("resolved=0x6000") && line.Contains("batch=1") && line.Contains("batchEvent=2/8")), "pending tracker should keep AoE follow-up HP events in the same resolving batch");
        Check(ninjaMatch.Match?.BatchEvent == 2, $"pending tracker structured AoE match expected batch event 2, got {ninjaMatch.Match?.BatchEvent}");
        Check(ninjaMatch.Match?.ObservedHpLoss == 273, $"pending tracker structured AoE match expected observed loss 273, got {ninjaMatch.Match?.ObservedHpLoss}");

        var recentCacheTracker = new PendingActionTracker();
        var recentCasterPending = cloudPending with
        {
            Raw = ActionRaw(pendingFlag: 8, pendingTimer: 2, actionId: 257, pendingFlag2: 8),
        };
        recentCacheTracker.ObserveUnit(
            recentCasterPending,
            ActionProbeState.From(recentCasterPending),
            pendingTrackerSettings,
            TickAfter(1_000),
            touchForContext: false);
        var agriasForecastCache = agriasHit with
        {
            Raw = ActionRaw(forecastDamage: 115, forecastCharge: 2, forecastFlag: 128, phaseMarker: 0),
        };
        recentCacheTracker.ObserveUnit(
            agriasForecastCache,
            ActionProbeState.From(agriasForecastCache),
            pendingTrackerSettings,
            TickAfter(1_025),
            touchForContext: false);
        var agriasCacheCleared = agriasHit with
        {
            Raw = ActionRaw(),
        };
        recentCacheTracker.ObserveUnit(
            agriasCacheCleared,
            ActionProbeState.From(agriasCacheCleared),
            pendingTrackerSettings,
            TickAfter(1_050),
            touchForContext: false);
        var recentCasterCleared = recentCasterPending with
        {
            Raw = ActionRaw(pendingFlag: 0, pendingTimer: 0xFF, actionId: 257, pendingFlag2: 0),
        };
        recentCacheTracker.ObserveUnit(
            recentCasterCleared,
            ActionProbeState.From(recentCasterCleared),
            pendingTrackerSettings,
            TickAfter(1_075),
            touchForContext: false);
        var recentCacheMatch = recentCacheTracker.MatchHpEvent(
            "damage",
            902,
            agriasCacheCleared,
            ActionProbeState.From(agriasCacheCleared),
            observedHpLoss: 115,
            pendingTrackerSettings,
            TickAfter(1_100));
        Check(recentCacheMatch.Lines.Any(line => line.Contains("confidence=recent-damage-cache") && line.Contains("recentCache=dmg1C4=115")), "pending tracker should retain recent target damage cache evidence after the live field clears");
        Check(recentCacheMatch.Match?.RecentDamageCacheMatches == true, "pending tracker structured match should preserve recent damage-cache evidence");
        Check(recentCacheMatch.Match?.RecentTargetCacheDamage == 115, $"pending tracker structured match expected recent target cache damage 115, got {recentCacheMatch.Match?.RecentTargetCacheDamage}");

        var echoGuard = new ValueRewriteEchoGuard("HP");
        echoGuard.Remember((nint)0x5000, 30, 18, now);
        Check(
            echoGuard.TrySuppress((nint)0x5000, 30, 18, now + Stopwatch.Frequency / 10, 1000, out string echoReason),
            "rewrite echo guard should suppress exact pending rewrite delta");
        Check(echoReason.Contains("30->18"), "rewrite echo guard should describe suppressed delta");
        Check(
            !echoGuard.TrySuppress((nint)0x5000, 30, 18, now + Stopwatch.Frequency / 10, 1000, out _),
            "rewrite echo guard should consume a suppressed delta");

        echoGuard.Remember((nint)0x5001, 30, 18, now);
        Check(
            !echoGuard.TrySuppress((nint)0x5001, 30, 18, now + Stopwatch.Frequency * 2, 1000, out _),
            "rewrite echo guard should not suppress expired deltas");

        echoGuard.Remember((nint)0x5002, 30, 18, now);
        Check(
            !echoGuard.TrySuppress((nint)0x5002, 18, 12, now + Stopwatch.Frequency / 10, 1000, out _),
            "rewrite echo guard should not suppress a real delta after the desired HP");

        var normalRewriteDecision = RewriteApplication.Decide(false, 30, 18);
        Check(normalRewriteDecision.ShouldWrite, "normal rewrite decision should write changed values");
        Check(!normalRewriteDecision.ShouldLogDryRun, "normal rewrite decision should not log dry-run");
        Check(normalRewriteDecision.TrackingValue == 18, "normal rewrite decision should advance tracking to desired value");

        var normalNoOpDecision = RewriteApplication.Decide(false, 18, 18);
        Check(!normalNoOpDecision.ShouldWrite, "normal no-op rewrite decision should not write");
        Check(!normalNoOpDecision.ShouldLogDryRun, "normal no-op rewrite decision should not log dry-run");
        Check(normalNoOpDecision.TrackingValue == 18, "normal no-op rewrite decision should keep current tracking");

        var dryRunRewriteDecision = RewriteApplication.Decide(true, 30, 18);
        Check(!dryRunRewriteDecision.ShouldWrite, "dry-run rewrite decision should not write changed values");
        Check(dryRunRewriteDecision.ShouldLogDryRun, "dry-run rewrite decision should log changed values");
        Check(dryRunRewriteDecision.TrackingValue == 30, "dry-run rewrite decision should keep observed tracking");

        var dryRunNoOpDecision = RewriteApplication.Decide(true, 18, 18);
        Check(!dryRunNoOpDecision.ShouldWrite, "dry-run no-op rewrite decision should not write");
        Check(dryRunNoOpDecision.ShouldLogDryRun, "dry-run no-op rewrite decision should still log evaluation");
        Check(dryRunNoOpDecision.TrackingValue == 18, "dry-run no-op rewrite decision should keep current tracking");

        if (OperatingSystem.IsWindows())
        {
            TestMemoryTableProbe();
            TestDeathStateWrite();
        }
        else
        {
            Console.WriteLine("skipping memory probe / death-state write tests (VirtualQuery is Windows-only)");
        }
        TestRuntimeSettingsLoad();
        TestExampleSettingsLoad(root);
        TestRuntimeSettingsValidator(root, catalog);
        TestRuntimeSettingsSimulator(root, catalog);
        TestGeneratedRuntimeSettings(root, catalog, target, attacker);

        Console.WriteLine("formula runtime smoke tests passed");
        return 0;
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "work", "item_catalog.csv")) &&
                Directory.Exists(Path.Combine(dir.FullName, "codemod")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("could not find repo root from " + start);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static int EvalFormula(string expression, UnitSnapshot target, UnitSnapshot attacker, long eventIndex = 0, long eventSeed = 0)
    {
        var context = new FormulaContext(target, attacker, eventIndex, eventSeed);
        if (!FormulaExpression.TryEvaluate(expression, context, out int actual, out string error))
            throw new InvalidOperationException($"{expression} failed: {error}");
        return actual;
    }

    private static void CheckFormula(string expression, UnitSnapshot target, UnitSnapshot attacker, int expected, long eventIndex = 0, long eventSeed = 0)
    {
        int actual = EvalFormula(expression, target, attacker, eventIndex, eventSeed);
        Check(actual == expected, $"{expression} expected {expected}, got {actual}");
    }

    private static void TestAbilityCatalog(AbilityCatalog catalog)
    {
        Check(catalog.Count == 512, $"ability catalog expected 512 rows, got {catalog.Count}");
        Check(catalog.TryGet(1, out var cure), "ability catalog should contain Cure id 1");
        Check(cure.Name == "Cure", $"Cure name expected Cure, got {cure.Name}");

        var cureVars = VariablesFor(cure);
        CheckVar(cureVars, "ability.id", 1);
        CheckVar(cureVars, "ability.formula", 0x0C);
        CheckVar(cureVars, "ability.y", 14);
        CheckVar(cureVars, "ability.range", 4);
        CheckVar(cureVars, "ability.mp_cost", 6);
        CheckVar(cureVars, "ability.used_by_enemies", 1);
        CheckVar(cureVars, "ability.reflectable", 1);
        CheckVar(cureVars, "ability.evadeable", 0);

        Check(catalog.TryGet(158, out var hallowedBolt), "ability catalog should contain Hallowed Bolt id 158");
        var hallowedBoltVars = VariablesFor(hallowedBolt);
        CheckVar(hallowedBoltVars, "ability.formula", 0x2D);
        CheckVar(hallowedBoltVars, "ability.element_lightning", 1);
        CheckVar(hallowedBoltVars, "ability.element_holy", 1);
        CheckVar(hallowedBoltVars, "ability.element_fire", 0);
        CheckVar(hallowedBoltVars, "ability.weapon_strike", 1);
        CheckVar(hallowedBoltVars, "ability.inflict_silence", 1);
        CheckVar(hallowedBoltVars, "ability.inflict_poison", 0);
        CheckVar(hallowedBoltVars, "ability.inflict_mode_separate", 1);
        CheckVar(hallowedBoltVars, "ability.inflict_mode_all", 0);

        Check(catalog.TryGet(5, out var raise), "ability catalog should contain Raise id 5");
        var raiseVars = VariablesFor(raise);
        CheckVar(raiseVars, "ability.inflict_status", 0x20);
        CheckVar(raiseVars, "ability.inflict_dead", 1);
        CheckVar(raiseVars, "ability.inflict_mode_cancel", 1);

        Check(catalog.TryGet(145, out var rendMagick), "ability catalog should contain Rend Magick id 145");
        var rendVars = VariablesFor(rendMagick);
        CheckVar(rendVars, "ability.weapon_range", 1);
        CheckVar(rendVars, "ability.shirahadori", 1);
        CheckVar(rendVars, "ability.evadeable", 1);
        CheckVar(rendVars, "ability.normal_attack", 0);

        Check(catalog.TryGet(487, out var movePlusTwo), "ability catalog should contain Move +2 id 487");
        var moveVars = VariablesFor(movePlusTwo);
        Check(movePlusTwo.Name == "Movement +2", $"Move +2 IVC name expected Movement +2, got {movePlusTwo.Name}");
        CheckVar(moveVars, "ability.formula", 0);
        CheckVar(moveVars, "ability.jp_cost", 560);
        CheckVar(moveVars, "ability.used_by_enemies", 0);
        CheckVar(moveVars, "ability.weapon_range", 0);

        var defaultContext = MakeFormulaContext();
        AbilityCatalogEntry.AddDefaultVariables(defaultContext, "ability", 999);
        var defaultNames = FormulaVariableNames(defaultContext);
        Check(defaultNames.SetEquals(cureVars.Keys), "ability default variable names should match loaded-row variable names");
    }

    private static Dictionary<string, int> VariablesFor(AbilityCatalogEntry entry)
    {
        var context = MakeFormulaContext();
        entry.AddVariables(context, "ability");
        return FormulaVariables(context);
    }

    private static FormulaContext MakeFormulaContext()
        => new(new UnitSnapshot((nint)0x1, 0, 1, 1, 1, 0, false, 1, 1, 1, 1, 1, 50, 50, new byte[0x180], 1, 1));

    private static Dictionary<string, int> FormulaVariables(FormulaContext context)
    {
        var field = typeof(FormulaContext).GetField("_variables", BindingFlags.Instance | BindingFlags.NonPublic);
        var variables = field?.GetValue(context) as Dictionary<string, int>;
        return variables ?? throw new InvalidOperationException("could not inspect FormulaContext variables");
    }

    private static HashSet<string> FormulaVariableNames(FormulaContext context)
        => FormulaVariables(context).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static void CheckVar(Dictionary<string, int> variables, string name, int expected)
    {
        Check(variables.TryGetValue(name, out int actual), $"missing formula variable {name}");
        Check(actual == expected, $"{name} expected {expected}, got {actual}");
    }

    private static void TestDclActionContextCache()
    {
        var cache = new DclActionContextCache();
        Check(!cache.TryGetLatest(3, nowTicks: 100, maxAgeTicks: 50, out _), "empty DCL cache should miss");

        cache.Record(3, casterIdx: 2, actionType: 1, abilityId: 99, timestampTicks: 100);
        Check(cache.TryGetLatest(3, nowTicks: 150, maxAgeTicks: 50, out var first), "fresh DCL cache entry should hit at max age");
        Check(first.CasterIdx == 2 && first.ActionType == 1 && first.AbilityId == 99 && first.TimestampTicks == 100,
            "fresh DCL cache entry should preserve action context");

        cache.Record(3, casterIdx: 7, actionType: 4, abilityId: 101, timestampTicks: 140);
        Check(cache.TryGetLatest(3, nowTicks: 145, maxAgeTicks: 50, out var overwrite), "overwritten DCL cache entry should hit");
        Check(overwrite.CasterIdx == 7 && overwrite.ActionType == 4 && overwrite.AbilityId == 101,
            "DCL cache should keep latest per target");

        cache.Record(3, casterIdx: 9, actionType: 9, abilityId: 9, timestampTicks: 130);
        Check(cache.TryGetLatest(3, nowTicks: 145, maxAgeTicks: 50, out var stillLatest), "older DCL cache record should not evict latest");
        Check(stillLatest.CasterIdx == 7 && stillLatest.AbilityId == 101, "DCL cache should ignore older timestamps");

        Check(!cache.TryGetLatest(3, nowTicks: 191, maxAgeTicks: 50, out _), "expired DCL cache entry should miss");
        cache.Record(-1, casterIdx: 1, actionType: 1, abilityId: 1, timestampTicks: 200);
        cache.Record(64, casterIdx: 1, actionType: 1, abilityId: 1, timestampTicks: 200);
        Check(!cache.TryGetLatest(-1, nowTicks: 200, maxAgeTicks: 50, out _), "negative DCL target index should miss");
        Check(!cache.TryGetLatest(64, nowTicks: 200, maxAgeTicks: 50, out _), "out-of-range DCL target index should miss");
    }

    private static void TestDclFormulaContextBuilder(
        ItemCatalog itemCatalog,
        AbilityCatalog abilityCatalog,
        UnitSnapshot target,
        UnitSnapshot attacker)
    {
        var settings = new RuntimeSettings
        {
            FormulaVariables = new Dictionary<string, int> { ["dclConst"] = 3 },
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Body", Offset = 0x70, Width = "Byte" },
            ],
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Weapon", Offset = 0x50, Width = "Byte" },
            ],
        };

        var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings,
            itemCatalog,
            abilityCatalog,
            target,
            attacker,
            eventIndex: 12,
            eventSeed: 345,
            actionType: 2,
            abilityId: 1,
            oldDebit: 7,
            oldCredit: 4);

        Check(FormulaExpression.TryEvaluate("a.pa * 2 + ability.y", context, out int mixed, out string mixedError),
            $"DCL unit/ability formula should evaluate: {mixedError}");
        Check(mixed == 38, $"DCL unit/ability formula expected 38, got {mixed}");

        Check(FormulaExpression.TryEvaluate("dcl.oldDebit + dcl.oldCredit + action.type + action.abilityId + const.dclConst", context, out int dclVars, out string dclError),
            $"DCL plumbing formula should evaluate: {dclError}");
        Check(dclVars == 17, $"DCL plumbing formula expected 17, got {dclVars}");

        Check(FormulaExpression.TryEvaluate("targetSlot.body.itemId + attackerSlot.weapon.itemId", context, out int slots, out string slotError),
            $"DCL slot formula should evaluate: {slotError}");
        Check(slots == 191, $"DCL slot formula expected 191, got {slots}");

        var derivedChain = new List<FormulaDerivedVariable>
        {
            new() { Name = "dcl.st", Formula = "a.pa + 4" },
            new() { Name = "dcl.gross", Formula = "dcl.st * 2 + ability.y" },
        };
        Check(FormulaRuntimeContextBuilder.TryApplyDerivedVariables(context, derivedChain, "DclDerivedVariables", out string chainError),
            $"DCL derived chain should apply: {chainError}");
        Check(FormulaExpression.TryEvaluate("dcl.gross", context, out int gross, out string grossError),
            $"DCL derived variable should be readable: {grossError}");
        Check(gross == 46, $"DCL derived chain expected 46, got {gross}");

        var badChain = new List<FormulaDerivedVariable>
        {
            new() { Name = "dcl.bad", Formula = "missingDclDerivedInput + 1" },
        };
        Check(!FormulaRuntimeContextBuilder.TryApplyDerivedVariables(context, badChain, "DclDerivedVariables", out string badError) &&
              badError.Contains("missingDclDerivedInput"),
            "DCL derived chain should fail loudly on unknown inputs");
    }

    private static void TestMemoryTableProbe()
    {
        nint resolved = MemoryTableProbe.ResolveRipRelativeTarget((nint)0x1000, 0x20, 7);
        Check(resolved == (nint)0x1027, $"RIP target expected 0x1027, got 0x{resolved:X}");

        var probe = new MemoryTableProbe
        {
            Name = "Roster Unit Table",
            Pattern = "48 8D 05 ?? ?? ?? ??",
            Count = 2,
            Stride = 8,
            LogRows = true,
            Fields =
            [
                new MemoryTableFieldProbe { Name = "unitIndex", Offset = 1, Width = "Byte", EmptyValue = 0xFF },
                new MemoryTableFieldProbe { Name = "job", Offset = 2, Width = "Byte" },
                new MemoryTableFieldProbe { Name = "rosterWord", Offset = 4, Width = "UInt16", Hex = true, CountForPresence = false },
            ],
        };
        probe.Normalize();
        Check(probe.TryValidate(out string probeError), $"memory table probe should validate: {probeError}");
        Check(probe.TraceName == "roster_unit_table", $"normalized probe name expected roster_unit_table, got {probe.TraceName}");

        nint table = Marshal.AllocHGlobal(16);
        try
        {
            for (int i = 0; i < 16; i++)
                Marshal.WriteByte(table, i, 0);

            Marshal.WriteByte(table, 1, 7);
            Marshal.WriteByte(table, 2, 12);
            Marshal.WriteInt16(table, 4, 0x1234);
            Marshal.WriteByte(table + 8, 1, 0xFF);

            Check(ReadableMemoryRange.IsReadable(table, 16), "allocated probe table should be readable");
            var copied = new byte[16];
            Check(CurrentProcessMemory.TryRead(table, copied, out string processReadError), $"current process memory read should succeed: {processReadError}");
            Check(copied[1] == 7, "current process memory read should copy byte values");
            Check((copied[4] | copied[5] << 8) == 0x1234, "current process memory read should copy word values");
            Check(ReadableMemoryRange.IsWritable(table, 16), "allocated probe table should be writable");
            Check(CurrentProcessMemory.TryWriteInt16(table + 6, 0x2345, out string processWriteError), $"current process memory write should succeed: {processWriteError}");
            Check(Marshal.ReadInt16(table, 6) == 0x2345, "current process memory write should update word values");
            Check(!CurrentProcessMemory.TryRead(0, new byte[4], out string nullReadError), "null process memory read should fail");
            Check(nullReadError.Contains("not readable"), $"null process memory read should explain readability, got {nullReadError}");
            Check(!CurrentProcessMemory.TryWriteInt16(0, 1, out string nullWriteError), "null process memory write should fail");
            Check(nullWriteError.Contains("not writable"), $"null process memory write should explain writability, got {nullWriteError}");

            Check(probe.TryReadRow(table, 0, out var occupied, out string rowError), $"occupied row should read: {rowError}");
            Check(occupied.PresenceScore == 2, $"occupied row presence expected 2, got {occupied.PresenceScore}");
            Check(occupied.Values[0].Value == 7, "unitIndex field should read byte value");
            Check(occupied.Values[2].FormattedValue == "0x1234", $"word field should be hex formatted, got {occupied.Values[2].FormattedValue}");
            Check(occupied.Trace.Contains("unitindex=7"), "row trace should include normalized unitIndex field");

            Check(probe.TryReadRow(table + 8, 1, out var empty, out string emptyError), $"empty row should read: {emptyError}");
            Check(empty.PresenceScore == 0, $"empty row presence expected 0, got {empty.PresenceScore}");
        }
        finally
        {
            Marshal.FreeHGlobal(table);
        }

        var invalidProbe = new MemoryTableProbe
        {
            Pattern = "AA BB",
            Count = 1,
            Stride = 2,
            Fields = [new MemoryTableFieldProbe { Name = "tooWide", Offset = 1, Width = "UInt16" }],
        };
        invalidProbe.Normalize();
        Check(!invalidProbe.TryValidate(out string invalidError), "field exceeding stride should fail validation");
        Check(invalidError.Contains("exceeds stride"), $"invalid probe error should explain stride, got {invalidError}");
    }

    private static void TestDeathStateWrite()
    {
        var koFlag = new DeathStateWrite { Name = "KO flag", Offset = 0x61, Width = "Byte", OrMask = 0x20 };
        Check(koFlag.TryValidate(out int koWidth, out long koMask, out string koValidateError), $"KO flag write should validate: {koValidateError}");
        Check(koWidth == 1 && koMask == 0xFF, $"KO flag write width/mask expected 1/0xFF, got {koWidth}/0x{koMask:X}");

        nint unit = Marshal.AllocHGlobal(0x200);
        try
        {
            for (int i = 0; i < 0x200; i++)
                Marshal.WriteByte(unit, i, 0);

            Check(koFlag.TryApply(unit, out string koDesc, out string koError), $"KO flag write should apply: {koError}");
            Check(Marshal.ReadByte(unit, 0x61) == 0x20, "KO flag write should set bit 0x20");
            Check(koDesc.Contains("+0x61") && koDesc.Contains("0->20"), $"KO flag desc should show offset and transition, got {koDesc}");

            Marshal.WriteByte(unit, 0x61, 0x21);
            Check(koFlag.TryApply(unit, out _, out string koAgainError), $"KO flag write should preserve existing bits: {koAgainError}");
            Check(Marshal.ReadByte(unit, 0x61) == 0x21, "KO flag OR write should preserve existing status bits");

            Marshal.WriteInt16(unit, 0x70, unchecked((short)0xABCD));
            var clearHighStatus = new DeathStateWrite { Name = "Clear high status byte", Offset = 0x70, Width = "Word", AndMask = 0x00FF };
            Check(clearHighStatus.TryApply(unit, out string clearDesc, out string clearError), $"word AND write should apply: {clearError}");
            Check(Marshal.ReadByte(unit, 0x70) == 0xCD && Marshal.ReadByte(unit, 0x71) == 0x00, "word AND write should clear the high byte only");
            Check(clearDesc.Contains("ABCD->CD"), $"word AND desc should show masked transition, got {clearDesc}");

            var dwordValue = new DeathStateWrite { Name = "DWord value", Offset = 0x74, Width = "DWord", Value = 0x12345678 };
            Check(dwordValue.TryApply(unit, out _, out string dwordError), $"dword value write should apply: {dwordError}");
            Check(Marshal.ReadByte(unit, 0x74) == 0x78, "dword value byte 0 should be little-endian");
            Check(Marshal.ReadByte(unit, 0x75) == 0x56, "dword value byte 1 should be little-endian");
            Check(Marshal.ReadByte(unit, 0x76) == 0x34, "dword value byte 2 should be little-endian");
            Check(Marshal.ReadByte(unit, 0x77) == 0x12, "dword value byte 3 should be little-endian");
        }
        finally
        {
            Marshal.FreeHGlobal(unit);
        }

        var badWidth = new DeathStateWrite { Offset = 0x61, Width = "Nibble", OrMask = 1 };
        Check(!badWidth.TryValidate(out _, out _, out string badWidthError), "bad death write width should fail validation");
        Check(badWidthError.Contains("unsupported Width"), $"bad width error should explain width, got {badWidthError}");

        var tooWide = new DeathStateWrite { Offset = 0x1FF, Width = "Word", OrMask = 1 };
        Check(!tooWide.TryValidate(out _, out _, out string tooWideError), "death write exceeding copied snapshot should fail validation");
        Check(tooWideError.Contains("exceeds copied unit snapshot"), $"too-wide error should explain snapshot size, got {tooWideError}");

        var badMask = new DeathStateWrite { Offset = 0x61, Width = "Byte", OrMask = 0x100 };
        Check(!badMask.TryValidate(out _, out _, out string badMaskError), "byte death write mask > 0xFF should fail validation");
        Check(badMaskError.Contains("exceeds field mask"), $"bad mask error should explain field mask, got {badMaskError}");

        var noOp = new DeathStateWrite { Offset = 0x61, Width = "Byte" };
        Check(!noOp.TryValidate(out _, out _, out string noOpError), "death write without Value/OrMask/AndMask should fail validation");
        Check(noOpError.Contains("no Value/OrMask/AndMask"), $"no-op error should explain missing operation, got {noOpError}");
    }

    private static void TestRuntimeSettingsLoad()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gc-runtime-settings-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """
                {
                  "DryRunRewrites": true,
                  "RewriteObservedDamage": true,
                  "UnitPollIntervalMs": 12,
                  "MaxTrackedBattleUnits": 32,
                  "HookRegisterProbe": true,
                  "HookRegisterProbeMaxLogs": 7,
                  "RewriteConditionFormula": "action.present",
                  "FormulaPreActionVariables": [
                    { "Name": "pre.weaponBlade", "Formula": "1" }
                  ],
                  "FormulaPreResponseVariables": [
                    { "Name": "armor.plate", "Formula": "1" }
                  ],
                  "FormulaDerivedVariables": [
                    { "Name": "probe.value", "Formula": "1" }
                  ],
                  "FormulaTraceVariables": [
                    { "Name": "trace.final", "Formula": "result.finalDamage" }
                  ],
                  "FormulaMatrices": {
                    "response": [
                      [650, 750],
                      [950, 1000]
                    ]
                  },
                  "FormulaMaps": {
                    "armorDrByItem": {
                      "172": 3,
                      "0xAE": 6
                    }
                  },
                  "ActionSignalRules": [
                    { "VanillaDamage": 7, "Variables": { "cut": 1 } }
                  ],
                  "DamageResponseRules": [
                    { "ConditionFormula": "action.cut", "MultiplierPermille": 650 }
                  ],
                  "MemoryTableProbes": [
                    {
                      "Name": "Roster Unit Table",
                      "Enabled": false,
                      "Pattern": "48 8D 05 ?? ?? ?? ??",
                      "Count": 55,
                      "Stride": 600,
                      "Fields": [
                        { "Name": "unitIndex", "Offset": 1, "Width": "Byte", "EmptyValue": 255 },
                        { "Name": "job", "Offset": 2, "Width": "Byte" }
                      ]
                    }
                  ]
                }
                """);

            Check(RuntimeSettings.TryLoad(path, out var loaded, out string validError), $"valid settings should load: {validError}");
            Check(loaded.DryRunRewrites, "loaded settings should preserve DryRunRewrites");
            Check(loaded.RewriteObservedDamage, "loaded settings should preserve RewriteObservedDamage");
            Check(loaded.Describe().Contains("DryRunRewrites=True"), "settings description should include DryRunRewrites");
            Check(loaded.UnitPollIntervalMs == 12, $"loaded settings should preserve UnitPollIntervalMs, got {loaded.UnitPollIntervalMs}");
            Check(loaded.MaxTrackedBattleUnits == 32, $"loaded settings should preserve MaxTrackedBattleUnits, got {loaded.MaxTrackedBattleUnits}");
            Check(loaded.HookRegisterProbe, "loaded settings should preserve HookRegisterProbe");
            Check(loaded.HookRegisterProbeMaxLogs == 7, $"loaded settings should preserve HookRegisterProbeMaxLogs, got {loaded.HookRegisterProbeMaxLogs}");
            Check(loaded.Describe().Contains("UnitPollIntervalMs=12"), "settings description should include UnitPollIntervalMs");
            Check(loaded.Describe().Contains("MaxTrackedBattleUnits=32"), "settings description should include MaxTrackedBattleUnits");
            Check(loaded.Describe().Contains("HookRegisterProbe=True/7"), "settings description should include HookRegisterProbe");
            Check(loaded.RewriteConditionFormula == "action.present", "loaded settings should preserve RewriteConditionFormula");
            Check(loaded.FormulaPreActionVariables.Count == 1, "loaded settings should preserve pre-action variables");
            Check(loaded.FormulaPreResponseVariables.Count == 1, "loaded settings should preserve pre-response variables");
            Check(loaded.FormulaDerivedVariables.Count == 1, "loaded settings should preserve derived variables");
            Check(loaded.FormulaTraceVariables.Count == 1, "loaded settings should preserve trace variables");
            Check(loaded.FormulaMatrices.Count == 1, "loaded settings should preserve formula matrices");
            Check(loaded.FormulaMatrices["response"][1][1] == 1000, "loaded formula matrix should preserve values");
            Check(loaded.FormulaMaps.Count == 1, "loaded settings should preserve formula maps");
            Check(loaded.FormulaMaps["armorDrByItem"]["0xAE"] == 6, "loaded formula map should preserve hex-key values");
            Check(loaded.ActionSignalRules.Count == 1, "loaded settings should preserve action signal rules");
            Check(loaded.ActionSignalRules[0].Variables["cut"] == 1, "loaded action variables should normalize");
            Check(loaded.DamageResponseRules.Count == 1, "loaded settings should preserve damage response rules");
            Check(loaded.DamageResponseRules[0].MultiplierPermille == 650, "loaded damage response multiplier should preserve permille");
            Check(loaded.MemoryTableProbes.Count == 1, "loaded settings should preserve memory table probes");
            Check(loaded.MemoryTableProbes[0].TraceName == "roster_unit_table", "memory table probe name should normalize");
            Check(loaded.MemoryTableProbes[0].Fields.Count == 2, "memory table probe fields should load");

            File.WriteAllText(path, "{ this is not json");
            Check(!RuntimeSettings.TryLoad(path, out _, out string invalidError), "invalid settings should fail cleanly");
            Check(!string.IsNullOrWhiteSpace(invalidError), "invalid settings failure should include an error");
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    private static void TestExampleSettingsLoad(string root)
    {
        string path = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2-response.example.json");
        Check(File.Exists(path), "v0.2 response example settings file should exist");
        Check(RuntimeSettings.TryLoad(path, out var loaded, out string error), $"v0.2 response example should load: {error}");
        Check(loaded.RewriteObservedDamage, "v0.2 response example should enable damage rewrite");
        Check(loaded.FormulaPreResponseVariables.Count >= 4, "v0.2 response example should define armor class variables");
        Check(loaded.ActionSignalRules.Count >= 4, "v0.2 response example should define action signal placeholders");
        Check(loaded.DamageResponseRules.Count >= 8, "v0.2 response example should define response rules");

        string generatedPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.generated.example.json");
        Check(File.Exists(generatedPath), "generated v0.2 settings example file should exist");
        Check(RuntimeSettings.TryLoad(generatedPath, out var generated, out string generatedError), $"generated v0.2 settings example should load: {generatedError}");
        Check(generated.ActionSignalRules.Count >= 18, "generated v0.2 settings should define weapon-family action rules");
        Check(generated.DamageResponseRules.Count >= 16, "generated v0.2 settings should define armor/type response matrix");
        Check(generated.FormulaTraceVariables.Count >= 5, "generated v0.2 settings should expose formula trace variables");

        string matrixGeneratedPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.matrix.generated.example.json");
        Check(File.Exists(matrixGeneratedPath), "matrix generated v0.2 settings example file should exist");
        Check(RuntimeSettings.TryLoad(matrixGeneratedPath, out var matrixGenerated, out string matrixGeneratedError), $"matrix generated v0.2 settings example should load: {matrixGeneratedError}");
        Check(matrixGenerated.ActionSignalRules.Count >= 18, "matrix generated v0.2 settings should define weapon-family action rules");
        Check(matrixGenerated.DamageResponseRules.Count == 1, "matrix generated v0.2 settings should collapse response rules into one formula rule");
        Check(matrixGenerated.FormulaMatrices.Count == 1, "matrix generated v0.2 settings should define one formula matrix");
        Check(matrixGenerated.FormulaMatrices.ContainsKey("armorResponsePermille"), "matrix generated v0.2 settings should define armorResponsePermille");
        Check(matrixGenerated.FormulaTraceVariables.Count >= 9, "matrix generated v0.2 settings should expose matrix trace variables");

        string scanGeneratedPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.generated.example.json");
        Check(File.Exists(scanGeneratedPath), "scan generated v0.2 settings example file should exist");
        Check(RuntimeSettings.TryLoad(scanGeneratedPath, out var scanGenerated, out string scanGeneratedError), $"scan generated v0.2 settings example should load: {scanGeneratedError}");
        Check(scanGenerated.ActionSignalRules.Count >= 18, "scan generated v0.2 settings should define weapon-family action rules");
        Check(scanGenerated.DamageResponseRules.Count >= 16, "scan generated v0.2 settings should define armor/type response matrix");

        string liveNoopPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.live-noop.example.json");
        Check(File.Exists(liveNoopPath), "scan live-noop v0.2 settings example file should exist");
        Check(RuntimeSettings.TryLoad(liveNoopPath, out var liveNoop, out string liveNoopError), $"scan live-noop v0.2 settings example should load: {liveNoopError}");
        Check(liveNoop.FinalDamageFormula == "vanillaDamage", "scan live-noop should preserve vanilla damage");
        Check(!liveNoop.ApplyDamageResponseRules, "scan live-noop should not apply damage response to HP");
        Check(liveNoop.InferAttackerFromRecentUnits, "scan live-noop should enable recent attacker inference for mapping");
        Check(liveNoop.LogResolvedRuntimeContext, "scan live-noop should enable resolved runtime traces");
        Check(!liveNoop.LogUnknownFieldDiffs, "scan live-noop should keep low-level diff noise off");

        string gurpsDrPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.gurps-dr.example.json");
        Check(File.Exists(gurpsDrPath), "GURPS-DR example settings file should exist");
        Check(RuntimeSettings.TryLoad(gurpsDrPath, out var gurpsDr, out string gurpsDrError), $"GURPS-DR example should load: {gurpsDrError}");
        Check(gurpsDr.FormulaTables.Count >= 4, "GURPS-DR example should define swing/thrust damage tables");
        Check(gurpsDr.EquipmentDrRules.Count == 1, "GURPS-DR example should define one armor DR rule");
        Check(!gurpsDr.ApplyEquipmentDr, "GURPS-DR example should subtract DR inside the formula");
        Check(gurpsDr.FormulaTraceVariables.Count >= 8, "GURPS-DR example should expose DR formula trace variables");

        string dryRunPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.dry-run.example.json");
        Check(File.Exists(dryRunPath), "dry-run example settings file should exist");
        Check(RuntimeSettings.TryLoad(dryRunPath, out var dryRun, out string dryRunError), $"dry-run example should load: {dryRunError}");
        Check(dryRun.DryRunRewrites, "dry-run example should enable DryRunRewrites");
        Check(dryRun.RewriteObservedDamage && dryRun.RewriteObservedHealing, "dry-run example should enable HP rewrite evaluation");
        Check(dryRun.RewriteObservedMpLoss && dryRun.RewriteObservedMpGain, "dry-run example should enable MP rewrite evaluation");
    }

    private static void TestRuntimeSettingsValidator(string root, ItemCatalog catalog)
    {
        string responsePath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2-response.example.json");
        Check(RuntimeSettings.TryLoad(responsePath, out var responseSettings, out string responseError), $"validator fixture should load: {responseError}");
        var validReport = RuntimeSettingsValidator.Validate(responseSettings, catalog);
        Check(validReport.Success, "validator should accept v0.2 response example settings");

        var missingContextRiskReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "attackerByte(0x44)",
        }, catalog);
        Check(missingContextRiskReport.Success, "missing-context risk should be a warning, not an error");
        Check(
            missingContextRiskReport.Findings.Any(finding =>
                finding.Severity.Equals("WARN", StringComparison.OrdinalIgnoreCase) &&
                finding.Scope == "MissingContextDamageEvaluation"),
            "validator should warn when damage formula fails without attacker/action context");

        var missingContextSafeReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "if(a.present, attackerByte(0x44), vanillaDamage)",
        }, catalog);
        Check(missingContextSafeReport.Success, "missing-context safe settings should validate");
        Check(
            !missingContextSafeReport.Findings.Any(finding => finding.Scope == "MissingContextDamageEvaluation"),
            "validator should not warn when formula has a no-attacker fallback");

        var validMapReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FormulaMaps = new Dictionary<string, Dictionary<string, int>>
            {
                ["damageByChar"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["0x80"] = 12,
                },
            },
            FinalDamageFormula = "mapOr(damageByChar, t.charId, vanillaDamage)",
        }, catalog);
        Check(validMapReport.Success, "validator should accept valid formula maps");

        var validDeathWriteReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "9999",
            CauseDeathOnZeroHp = true,
            DeathStateWrites =
            [
                new DeathStateWrite { Name = "KO flag", Offset = 0x61, Width = "Byte", OrMask = 0x20 },
            ],
        }, catalog);
        Check(validDeathWriteReport.Success, "validator should accept the mapped KO flag death-state write");

        var missingDeathWriteReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "9999",
            CauseDeathOnZeroHp = true,
        }, catalog);
        Check(!missingDeathWriteReport.Success, "validator should reject CauseDeathOnZeroHp without DeathStateWrites");
        Check(
            missingDeathWriteReport.Findings.Any(finding => finding.Scope == "DeathStateWrites" && finding.Message.Contains("no DeathStateWrites")),
            "validator should explain missing death-state writes");

        var invalidDeathWriteReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "9999",
            CauseDeathOnZeroHp = true,
            DeathCaptureFollowTicks = -1,
            DeathStateWrites =
            [
                new DeathStateWrite { Name = "Bad width", Offset = 0x61, Width = "Nibble", OrMask = 0x01 },
                new DeathStateWrite { Name = "Too wide", Offset = 0x1FF, Width = "Word", OrMask = 0x01 },
                new DeathStateWrite { Name = "Bad byte mask", Offset = 0x61, Width = "Byte", OrMask = 0x100 },
                new DeathStateWrite { Name = "No op", Offset = 0x61, Width = "Byte" },
            ],
        }, catalog);
        Check(!invalidDeathWriteReport.Success, "validator should reject invalid death-state writes");
        Check(
            invalidDeathWriteReport.Findings.Any(finding => finding.Scope == "DeathCaptureFollowTicks" && finding.Severity == "ERROR"),
            "validator should report negative death capture follow ticks");
        Check(
            invalidDeathWriteReport.Findings.Any(finding => finding.Scope.Contains("Bad width") && finding.Message.Contains("unsupported Width")),
            "validator should report invalid death write width");
        Check(
            invalidDeathWriteReport.Findings.Any(finding => finding.Scope.Contains("Too wide") && finding.Message.Contains("exceeds copied unit snapshot")),
            "validator should report death write offset overflow");
        Check(
            invalidDeathWriteReport.Findings.Any(finding => finding.Scope.Contains("Bad byte mask") && finding.Message.Contains("exceeds field mask")),
            "validator should report out-of-range death write masks");
        Check(
            invalidDeathWriteReport.Findings.Any(finding => finding.Scope.Contains("No op") && finding.Message.Contains("no Value/OrMask/AndMask")),
            "validator should report no-op death writes");

        var invalidSettings = new RuntimeSettings
        {
            RewriteObservedDamage = true,
            UnitPollIntervalMs = 0,
            MaxTrackedBattleUnits = 0,
            HookRegisterProbeMaxLogs = -1,
            RewriteConditionFormula = "missingGateValue + 1",
            FinalDamageFormula = "missingVariable + 1",
            DclPipelineEnabled = true,
            DclDamageFormula = "ability.y + missingDclValue",
            DclHitControlEnabled = true,
            DclHitChanceFormula = "ability.y + missingHitValue",
            DclHitForcedRoll = 100,
            DclMissClassEvadeValue = 256,
            DclDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "dcl.badDerived", Formula = "missingDclDerivedValue + 1" },
            ],
            FormulaPreActionVariables =
            [
                new FormulaDerivedVariable { Name = "badPreAction", Formula = "missingPreActionValue + 1" },
            ],
            FormulaTraceVariables =
            [
                new FormulaDerivedVariable { Name = "badTrace", Formula = "missingTraceValue + 1" },
            ],
            FormulaMatrices = new Dictionary<string, List<List<int>>>
            {
                ["bad"] = [new List<int>()],
            },
            FormulaMaps = new Dictionary<string, Dictionary<string, int>>
            {
                ["bad"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["not-an-int"] = 1,
                },
            },
            ActionSignalRules =
            [
                new ActionSignalRule
                {
                    Name = "Bad action event",
                    EventKind = "Typo",
                },
            ],
            DamageRules =
            [
                new DamageRule
                {
                    Name = "Bad damage event",
                    EventKind = "Typo",
                    FinalDamage = 1,
                },
            ],
            MpRules =
            [
                new MpRule
                {
                    Name = "Bad MP rule",
                    EventKind = "Typo",
                    FinalMpChangeFormula = "missingMpRuleValue + 1",
                    ScaleDenominator = 0,
                },
            ],
            MemoryTableProbes =
            [
                new MemoryTableProbe
                {
                    Name = "Bad Probe",
                    Pattern = "",
                    Count = 1,
                    Stride = 1,
                    Fields = [new MemoryTableFieldProbe { Name = "TooWide", Offset = 0, Width = "UInt16" }],
                },
            ],
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalidSettings, catalog);
        Check(!invalidReport.Success, "validator should reject bad settings");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "FinalDamageFormula" && finding.Message.Contains("missingVariable")),
            "validator should report unknown variable in FinalDamageFormula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "DclDamageFormula" && finding.Message.Contains("missingDclValue")),
            "validator should report unknown variable in DclDamageFormula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.StartsWith("DclDerivedVariables") && finding.Message.Contains("missingDclDerivedValue")),
            "validator should report unknown variable in DclDerivedVariables");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "DclHitChanceFormula" && finding.Message.Contains("missingHitValue")),
            "validator should report unknown variable in DclHitChanceFormula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "DclHitControlEnabled" && finding.Message.Contains("ItemTableEvadeZeroEnabled")),
            "validator should require the item-table evade-zero baseline under hit control");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "DclHitForcedRoll"),
            "validator should reject DclHitForcedRoll outside -1..99");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "DclMissClassEvadeValue"),
            "validator should reject DclMissClassEvadeValue outside 0..255");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "RewriteConditionFormula" && finding.Message.Contains("missingGateValue")),
            "validator should report unknown variable in RewriteConditionFormula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("FormulaPreActionVariables")),
            "validator should report invalid pre-action formula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("FormulaTraceVariables")),
            "validator should report invalid trace formula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("FormulaMatrices")),
            "validator should report invalid formula matrix");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("FormulaMaps")),
            "validator should report invalid formula map");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("ActionSignalRules.Bad action event") && finding.Message.Contains("unsupported EventKind")),
            "validator should report invalid action signal event kind");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("DamageRules.Bad damage event") && finding.Message.Contains("unsupported EventKind")),
            "validator should report invalid damage rule event kind");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("MpRules.Bad MP rule") && finding.Message.Contains("unsupported EventKind")),
            "validator should report invalid MP rule event kind");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("MpRules.Bad MP rule.FinalMpChangeFormula") && finding.Message.Contains("missingMpRuleValue")),
            "validator should report invalid MP rule formula");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("MpRules.Bad MP rule") && finding.Message.Contains("ScaleDenominator")),
            "validator should report invalid MP rule scale denominator");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope.Contains("MemoryTableProbes")),
            "validator should report invalid memory table probe");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "UnitPollIntervalMs" && finding.Severity == "ERROR"),
            "validator should report invalid poll interval");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "MaxTrackedBattleUnits" && finding.Severity == "ERROR"),
            "validator should report invalid max tracked units");
        Check(
            invalidReport.Findings.Any(finding => finding.Scope == "HookRegisterProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should report invalid hook register probe max logs");

        var hitDivZeroSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
            DclHitChanceFormula = "100 / dcl.oldDebit",
        };
        var hitDivZeroReport = RuntimeSettingsValidator.Validate(hitDivZeroSettings, catalog);
        Check(
            hitDivZeroReport.Findings.Any(finding => finding.Scope == "DclHitChanceFormula" && finding.Severity == "ERROR"),
            "hit formula dividing by dcl.oldDebit must fail validation under hit control (oldDebit=0 in the hit context)");
    }

    private static void TestRuntimeSettingsSimulator(string root, ItemCatalog catalog)
    {
        string generatedPath = Path.Combine(root, "work", "battle-runtime-settings.v0.2.generated.json");
        Check(RuntimeSettings.TryLoad(generatedPath, out var generated, out string generatedError), $"sim generated settings should load: {generatedError}");
        var generatedResults = RuntimeSettingsSimulator.Run(generated, catalog, RuntimeSettingsSimulator.DefaultScenarios());
        Check(generatedResults.Count == 1, "default simulator should emit one result");
        Check(generatedResults[0].ShouldRewrite, "generated simulator default should rewrite");
        Check(generatedResults[0].FinalDamage == 182, $"generated simulator final damage expected 182, got {generatedResults[0].FinalDamage}");
        Check(generatedResults[0].DesiredHp == 68, $"generated simulator desired HP expected 68, got {generatedResults[0].DesiredHp}");
        Check(generatedResults[0].Trace.Contains("DamageResponse(leather swing)"), "generated simulator trace should include leather swing response");
        Check(generatedResults[0].Trace.Contains("trace.finaldamage=182"), "generated simulator trace should include final damage trace variable");

        string liveNoopPath = Path.Combine(root, "work", "battle-runtime-settings.v0.2.scan.live-noop.json");
        Check(RuntimeSettings.TryLoad(liveNoopPath, out var liveNoop, out string liveNoopError), $"sim live-noop settings should load: {liveNoopError}");
        var liveNoopResults = RuntimeSettingsSimulator.Run(liveNoop, catalog, RuntimeSettingsSimulator.DefaultScenarios());
        Check(liveNoopResults[0].ShouldRewrite, "live-noop simulator default should pass rewrite gate");
        Check(liveNoopResults[0].FinalDamage == 20, $"live-noop simulator should preserve vanilla damage 20, got {liveNoopResults[0].FinalDamage}");
        Check(liveNoopResults[0].DesiredHp == 230, $"live-noop simulator desired HP expected 230, got {liveNoopResults[0].DesiredHp}");

        var mpTarget = RuntimeSimulationUnit.TargetDefaults();
        mpTarget.Mp = 7;
        mpTarget.MaxMp = null;
        var mpAttacker = RuntimeSimulationUnit.AttackerDefaults();
        mpAttacker.Mp = 5;
        mpAttacker.MaxMp = 9;
        var mpScenarioResults = RuntimeSettingsSimulator.Run(new RuntimeSettings
        {
            RewriteObservedDamage = true,
            FinalDamageFormula = "t.mp + t.maxMp + a.mp + a.maxMp",
        }, catalog, [
            new RuntimeSimulationScenario
            {
                Name = "mp-aware-scenario",
                PreviousHp = 100,
                CurrentHp = 90,
                VanillaDamage = 10,
                Target = mpTarget,
                Attacker = mpAttacker,
            },
        ]);
        Check(mpScenarioResults[0].FinalDamage == 28, $"MP-aware simulator expected 28, got {mpScenarioResults[0].FinalDamage}");

        string mpSettingsPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.mp.example.json");
        Check(RuntimeSettings.TryLoad(mpSettingsPath, out var mpSettings, out string mpSettingsError), $"MP settings should load: {mpSettingsError}");
        string mpScenarioPath = Path.Combine(root, "docs", "modding", "examples", "runtime-simulation-mp.example.json");
        var mpScenarios = RuntimeSettingsSimulator.LoadScenarios(mpScenarioPath);
        Check(mpScenarios.Count == 2, $"MP simulation should contain 2 scenarios, got {mpScenarios.Count}");
        var mpResults = RuntimeSettingsSimulator.Run(mpSettings, catalog, mpScenarios);
        Check(mpResults.All(result => result.EventKind == "mp"), "MP simulation should emit MP results");
        Check(mpResults.All(result => result.HasExpectations), "MP simulation should assert all scenarios");
        Check(
            mpResults.All(result => result.ExpectationsPassed),
            "MP simulation expectations should pass: " +
            string.Join("; ", mpResults.SelectMany(result => result.ExpectationFailures)));
        Check(mpResults[0].FinalMpChange == -11 && mpResults[0].DesiredMp == 9, "MP loss simulation should rewrite to desired MP 9");
        Check(mpResults[1].FinalMpChange == 11 && mpResults[1].DesiredMp == 21, "MP gain simulation should rewrite to desired MP 21");

        string scenarioPath = Path.Combine(root, "docs", "modding", "examples", "runtime-simulation-scenarios.example.json");
        var scenarios = RuntimeSettingsSimulator.LoadScenarios(scenarioPath);
        Check(scenarios.Count == 3, $"runtime simulation example should contain three scenarios, got {scenarios.Count}");
        var scenarioResults = RuntimeSettingsSimulator.Run(generated, catalog, scenarios);
        Check(scenarioResults.All(result => result.HasExpectations), "runtime simulation example should assert all scenarios");
        Check(
            scenarioResults.All(result => result.ExpectationsPassed),
            "runtime simulation example expectations should pass: " +
            string.Join("; ", scenarioResults.SelectMany(result => result.ExpectationFailures)));

        var mismatchScenario = RuntimeSettingsSimulator.DefaultScenarios()[0];
        mismatchScenario.Expect = new RuntimeSimulationExpectation { FinalDamage = -1 };
        var mismatchResults = RuntimeSettingsSimulator.Run(generated, catalog, [mismatchScenario]);
        Check(mismatchResults[0].HasExpectations, "simulator should report expectation metadata");
        Check(!mismatchResults[0].ExpectationsPassed, "simulator should fail mismatched expectations");
        Check(
            mismatchResults[0].ExpectationFailures.Any(failure => failure.Contains("finalDamage")),
            "simulator expectation failure should describe the mismatched field");

        string matrixPath = Path.Combine(root, "docs", "modding", "examples", "runtime-simulation-matrix.v0.2.example.json");
        var matrixScenarios = RuntimeSettingsSimulator.LoadScenarios(matrixPath);
        Check(matrixScenarios.Count == 20, $"runtime simulation matrix should contain 20 scenarios, got {matrixScenarios.Count}");
        var matrixResults = RuntimeSettingsSimulator.Run(generated, catalog, matrixScenarios);
        Check(matrixResults.All(result => result.HasExpectations), "runtime simulation matrix should assert all scenarios");
        Check(
            matrixResults.All(result => result.ExpectationsPassed),
            "runtime simulation matrix expectations should pass: " +
            string.Join("; ", matrixResults.SelectMany(result => result.ExpectationFailures)));
        Check(
            matrixResults.Select(result => result.RuleName).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 16,
            "runtime simulation matrix should cover the armor/damage response matrix");
        Check(
            matrixResults.Any(result => result.Trace.Contains("id=0:Nothing Equipped")),
            "runtime simulation matrix should cover fists/no-weapon action resolution");

        string matrixResponseSettingsPath = Path.Combine(root, "work", "battle-runtime-settings.v0.2.matrix.generated.json");
        Check(RuntimeSettings.TryLoad(matrixResponseSettingsPath, out var matrixResponseSettings, out string matrixResponseSettingsError), $"matrix-response settings should load: {matrixResponseSettingsError}");
        string matrixResponsePath = Path.Combine(root, "docs", "modding", "examples", "runtime-simulation-matrix-response.v0.2.example.json");
        var matrixResponseScenarios = RuntimeSettingsSimulator.LoadScenarios(matrixResponsePath);
        Check(matrixResponseScenarios.Count == 20, $"runtime simulation matrix-response should contain 20 scenarios, got {matrixResponseScenarios.Count}");
        var matrixResponseResults = RuntimeSettingsSimulator.Run(matrixResponseSettings, catalog, matrixResponseScenarios);
        Check(matrixResponseResults.All(result => result.HasExpectations), "runtime simulation matrix-response should assert all scenarios");
        Check(
            matrixResponseResults.All(result => result.ExpectationsPassed),
            "runtime simulation matrix-response expectations should pass: " +
            string.Join("; ", matrixResponseResults.SelectMany(result => result.ExpectationFailures)));
        Check(
            matrixResponseResults.Select(result => result.FinalDamage).SequenceEqual(matrixResults.Select(result => result.FinalDamage)),
            "matrix-response policy should preserve the v0.2 response damage matrix");
        Check(
            matrixResponseResults.Any(result => result.Trace.Contains("trace.matrixresponsepermille=")),
            "matrix-response policy trace should include matrix response permille");
        var matrixResponseRuleNames = matrixResponseResults
            .Select(result => result.RuleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Check(
            matrixResponseRuleNames.Count == 1 &&
            matrixResponseRuleNames[0] == "FinalDamageFormula+DamageResponse(matrix response)",
            "matrix-response policy should route through the single matrix response rule");

        string gurpsDrSettingsPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.gurps-dr.example.json");
        Check(RuntimeSettings.TryLoad(gurpsDrSettingsPath, out var gurpsDrSettings, out string gurpsDrError), $"GURPS-DR settings should load: {gurpsDrError}");
        string gurpsDrScenarioPath = Path.Combine(root, "docs", "modding", "examples", "runtime-simulation-gurps-dr.example.json");
        var gurpsDrScenarios = RuntimeSettingsSimulator.LoadScenarios(gurpsDrScenarioPath);
        Check(gurpsDrScenarios.Count == 5, $"GURPS-DR simulation should contain 5 scenarios, got {gurpsDrScenarios.Count}");
        var gurpsDrResults = RuntimeSettingsSimulator.Run(gurpsDrSettings, catalog, gurpsDrScenarios);
        Check(gurpsDrResults.All(result => result.HasExpectations), "GURPS-DR simulation should assert all scenarios");
        Check(
            gurpsDrResults.All(result => result.ExpectationsPassed),
            "GURPS-DR simulation expectations should pass: " +
            string.Join("; ", gurpsDrResults.SelectMany(result => result.ExpectationFailures)));
        Check(
            gurpsDrResults.Any(result => result.Trace.Contains("equipmentDr=6:EquipmentDR(Body armor DR)")),
            "GURPS-DR simulation should prove plate DR is read from armor metadata");
        Check(
            gurpsDrResults.Any(result => result.Trace.Contains("trace.penetratingdamage=0")),
            "GURPS-DR simulation should prove armor can fully stop a weak hit");
    }

    private static void TestGeneratedRuntimeSettings(string root, ItemCatalog catalog, UnitSnapshot target, UnitSnapshot attacker)
    {
        string path = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.generated.example.json");
        Check(RuntimeSettings.TryLoad(path, out var generated, out string error), $"generated runtime settings should load: {error}");

        var result = new BattleFormulaEngine(generated, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, attacker, EventIndex: 123, EventSeed: 456));
        Check(result.ShouldRewrite, "generated runtime settings should rewrite a sword/leather damage event");
        Check(result.FinalDamage == 182, $"generated runtime settings expected sword/leather damage 182, got {result.FinalDamage}");

        var cleanAttackerRaw = new byte[0x180];
        cleanAttackerRaw[0x50] = 19;
        var cleanAttacker = attacker with { Raw = cleanAttackerRaw };
        string scanPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.generated.example.json");
        Check(RuntimeSettings.TryLoad(scanPath, out var scanGenerated, out string scanError), $"scan generated runtime settings should load: {scanError}");
        var scanResult = new BattleFormulaEngine(scanGenerated, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, cleanAttacker, EventIndex: 123, EventSeed: 456));
        Check(scanResult.ShouldRewrite, "scan generated runtime settings should rewrite a sword/leather damage event");
        Check(scanResult.FinalDamage == 182, $"scan generated runtime settings expected sword/leather damage 182, got {scanResult.FinalDamage}");

        string liveNoopPath = Path.Combine(root, "docs", "modding", "examples", "battle-runtime-settings.v0.2.scan.live-noop.example.json");
        Check(RuntimeSettings.TryLoad(liveNoopPath, out var liveNoop, out string liveNoopError), $"scan live-noop runtime settings should load: {liveNoopError}");
        var liveNoopResult = new BattleFormulaEngine(liveNoop, catalog)
            .Evaluate(new DamageEvent(target, 50, 30, 20, cleanAttacker, "recent-unit", EventIndex: 123, EventSeed: 456));
        Check(liveNoopResult.ShouldRewrite, "scan live-noop runtime settings should pass the rewrite gate");
        Check(liveNoopResult.FinalDamage == 20, $"scan live-noop should preserve vanilla damage 20, got {liveNoopResult.FinalDamage}");
        Check(liveNoopResult.DesiredHp == 30, $"scan live-noop should preserve current HP 30, got {liveNoopResult.DesiredHp}");
        Check(liveNoopResult.Trace.Contains("response=raw950/permille950/rules1/clamped0:"), "scan live-noop should still resolve leather swing response");
        Check(liveNoopResult.Trace.Contains("final=20:FinalDamageFormula"), "scan live-noop trace should show vanilla-preserving final formula");
    }

    private static void TestPendingActionLethalClampMatch()
    {
        var settings = new RuntimeSettings
        {
            PendingActionResolveWindowMs = 5000,
            PendingActionMaxBatchEvents = 8,
            PendingActionStaleMs = 5000,
        };
        var tracker = new PendingActionTracker();
        long now = Stopwatch.GetTimestamp();

        var casterPending = MakeActionProbeUnit((nint)0x1000, 0x32, hp: 100, pendingFlag: 8, pendingTimer: 2, actionId: 258, pendingFlag2: 8);
        var enterLines = tracker.ObserveUnit(casterPending, ActionProbeState.From(casterPending), settings, now, touchForContext: false);
        Check(enterLines.Any(line => line.Contains("[PENDING-ACTION-TRACK enter")), "pending tracker should enter a live pending action");

        var casterCleared = MakeActionProbeUnit((nint)0x1000, 0x32, hp: 100, pendingTimer: 0xFF, actionId: 258);
        var clearLines = tracker.ObserveUnit(casterCleared, ActionProbeState.From(casterCleared), settings, now + Stopwatch.Frequency / 100, touchForContext: false);
        Check(clearLines.Any(line => line.Contains("[PENDING-ACTION-TRACK resolve-open")), "pending tracker should open a resolving batch");

        var lethalTarget = MakeActionProbeUnit(
            (nint)0x2000,
            0x80,
            hp: 0,
            forecastDamage: 50,
            forecastCharge: 130,
            forecastFlag: 128,
            phaseMarker: 1);
        var result = tracker.MatchHpEvent(
            "damage",
            eventIndex: 1,
            lethalTarget,
            ActionProbeState.From(lethalTarget),
            observedHpLoss: 15,
            settings,
            now + Stopwatch.Frequency / 50);

        Check(result.Match is not null, "lethal clamp target cache should produce a pending match when a batch is active");
        Check(result.Match!.HasDamageCacheMatch, "lethal clamp target cache should count as damage-cache evidence");
        Check(result.Match.CurrentDamageCacheMatches, "current lethal clamp should set current damage-cache match");
        Check(!result.Match.CurrentDamageCacheExactMatches, "current lethal clamp should not be reported as an exact damage match");
        Check(result.Match.CurrentDamageCacheLethalClampMatches, "current lethal clamp should set the lethal-clamp flag");
        Check(result.Match.Confidence == "damage-cache-lethal-clamp", $"lethal clamp confidence should be damage-cache-lethal-clamp, got {result.Match.Confidence}");
        Check(result.Lines.Any(line => line.Contains("lethalClamp=1")), "pending match log should expose lethalClamp=1");
    }

    private static void TestImmediateActionCandidateScoring()
    {
        var ramzaRush = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: false,
                UnitHp: 446,
                ActionId: 147,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 1,
                StateAgeMs: 1170,
                SeenAgeMs: 109_929,
                CtDropAgeMs: 250_361,
                RawDamage: 0,
                ActionIdAgeMs: 1170,
                ActiveActionAgeMs: 1170));

        var staleCloudCrossSlash = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: false,
                UnitHp: 378,
                ActionId: 258,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 1,
                StateAgeMs: 1170,
                SeenAgeMs: 252_167,
                CtDropAgeMs: -1,
                RawDamage: 0,
                ActionIdAgeMs: 452_841,
                ActiveActionAgeMs: 452_841));

        var lethalTarget = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: true,
                UnitHp: 0,
                ActionId: 0,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 0,
                StateAgeMs: 0,
                SeenAgeMs: 0,
                CtDropAgeMs: 264_929,
                RawDamage: 33,
                ActionIdAgeMs: 0,
                ActiveActionAgeMs: 0));

        var basicAttackActiveSource = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: false,
                UnitHp: 322,
                ActionId: 0,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 1,
                StateAgeMs: 5,
                SeenAgeMs: 500,
                CtDropAgeMs: 2000,
                RawDamage: 0,
                ActionIdAgeMs: 20_000,
                ActiveActionAgeMs: 5)
            {
                AllowZeroActionIdActiveSource = true,
            });

        var basicAttackActiveSourceWithoutOptIn = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: false,
                UnitHp: 322,
                ActionId: 0,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 1,
                StateAgeMs: 5,
                SeenAgeMs: 500,
                CtDropAgeMs: 2000,
                RawDamage: 0,
                ActionIdAgeMs: 20_000,
                ActiveActionAgeMs: 5));

        var delayedBasicAttackActiveSource = ImmediateActionCandidateScoring.Evaluate(
            new ImmediateActionCandidateScoreInput(
                IsTarget: false,
                UnitHp: 322,
                ActionId: 0,
                HasPrimaryPendingFlag: false,
                HasSecondaryPendingFlag: false,
                PendingTimer: 0xFF,
                ActiveMarker2: 1,
                StateAgeMs: 28_302,
                SeenAgeMs: 24_742,
                CtDropAgeMs: -1,
                RawDamage: 0,
                ActionIdAgeMs: 42_928,
                ActiveActionAgeMs: 29_386)
            {
                AllowZeroActionIdActiveSource = true,
            });

        Check(ramzaRush.Role == "source-like", "fresh Ramza Rush should be source-like");
        Check(ramzaRush.FreshActionId, "fresh Ramza Rush should mark fresh action id");
        Check(ramzaRush.CurrentActiveAction, "fresh Ramza Rush should mark current active action");
        Check(ramzaRush.FreshActiveAction, "fresh Ramza Rush should mark fresh active action");
        Check(staleCloudCrossSlash.StaleActionId, "old Cloud Cross Slash action id should be stale");
        Check(staleCloudCrossSlash.StaleActiveAction, "old Cloud Cross Slash active action should be stale");
        Check(ramzaRush.Score > staleCloudCrossSlash.Score, "fresh Ramza Rush should beat stale Cloud Cross Slash");
        Check(ramzaRush.Score > lethalTarget.Score, "fresh Ramza Rush should beat the target-side lethal cache candidate");
        Check(basicAttackActiveSource.Role == "active-source-like", "opt-in active marker with action id 0 should be a basic-action source candidate");
        Check(basicAttackActiveSource.CurrentActiveAction, "opt-in action id 0 source should mark current active action");
        Check(basicAttackActiveSource.FreshActiveAction, "opt-in action id 0 source should use fresh active marker");
        Check(basicAttackActiveSource.Score >= 1600, $"basic active source should have a usable score, got {basicAttackActiveSource.Score}");
        Check(!basicAttackActiveSourceWithoutOptIn.SourceLike, "action id 0 active marker should not be source-like without opt-in");
        Check(delayedBasicAttackActiveSource.CurrentActiveAction, "delayed basic attack should still be current active action");
        Check(!delayedBasicAttackActiveSource.FreshActiveAction, "delayed basic attack should no longer be fresh");
        Check(delayedBasicAttackActiveSource.StaleActiveAction, "delayed basic attack should report stale active age diagnostically");
        Check(delayedBasicAttackActiveSource.Score >= 1600, $"delayed basic active source should remain selectable, got {delayedBasicAttackActiveSource.Score}");
    }

    private static UnitSnapshot MakeActionProbeUnit(
        nint ptr,
        int charId,
        int hp,
        int pendingFlag = 0,
        int pendingTimer = 0xFF,
        int actionId = 0,
        int forecastDamage = 0,
        int forecastCharge = 0,
        int forecastFlag = 0,
        int pendingFlag2 = 0,
        int activeMarker = 0,
        int activeMarker2 = 0,
        int phaseMarker = 0)
    {
        var raw = new byte[0x200];
        raw[0x61] = (byte)pendingFlag;
        raw[0x18D] = (byte)pendingTimer;
        WriteUInt16(raw, 0x1A2, actionId);
        WriteUInt16(raw, 0x1C4, forecastDamage);
        raw[0x1D8] = (byte)forecastCharge;
        raw[0x1E5] = (byte)forecastFlag;
        raw[0x1EF] = (byte)pendingFlag2;
        raw[0x1B8] = (byte)activeMarker;
        raw[0x1BA] = (byte)activeMarker2;
        raw[0x1BB] = (byte)phaseMarker;
        return new UnitSnapshot(ptr, charId, 50, hp, 100, 0, false, 10, 8, 9, 4, 3, 70, 60, raw, 20, 30, 50);
    }

    private static void WriteUInt16(byte[] raw, int offset, int value)
    {
        raw[offset] = (byte)(value & 0xFF);
        raw[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
