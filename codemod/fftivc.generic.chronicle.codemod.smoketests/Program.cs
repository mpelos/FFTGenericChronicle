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
        TestItemCatalog(catalog);
        string abilityCatalogPath = Path.Combine(root, "work", "wotl_ability_action_baseline.csv");
        var abilityCatalog = AbilityCatalog.Load(abilityCatalogPath);
        Check(abilityCatalog.Loaded, $"ability catalog loaded: {abilityCatalog.Describe()}");
        TestAbilityCatalog(abilityCatalog);
        TestDclAbilityMetadata(abilityCatalogPath);
        TestDclActionContextCache();
        TestDclComputePointNumericPlan();
        TestDclComputePointCache(catalog);
        TestDclHitDecisionCache();
        TestDclStatusControl(catalog, abilityCatalog);
        TestDclPhysicalContest(catalog);
        TestDclMultistrike();
        TestDclMagicEvade(catalog);
        TestDclWeaponSkill(catalog, abilityCatalog);
        TestDclMagicPipeline(catalog, abilityCatalog);
        TestDclIntegratedScaffold(root, catalog, abilityCatalog);
        TestDclResultFlags(catalog);
        TestDclMpEconomy(catalog);
        TestDclReactions(catalog);
        Check(
            ReferenceEquals(new BattleFormulaEngine(new RuntimeSettings(), catalog, abilityCatalog).AbilityCatalog, abilityCatalog),
            "formula engine should expose the supplied ability catalog");

        var targetRaw = new byte[0x200];
        targetRaw[0x70] = 172; // Leather Armor in work/item_catalog.csv.
        targetRaw[0x4F] = 3;
        targetRaw[0x50] = 4;
        targetRaw[0x51] = 0x83; // facing 3 on map level 1
        targetRaw[0x62] = 0x20; // effective Blind
        targetRaw[0x60] = 0x10; // Sleep immunity
        targetRaw[0x1F0] = 0x20; // durable/master Blind
        targetRaw[0x52] = 0x80; // absorbs Fire
        targetRaw[0x55] = 0x02; // weak to Holy
        var target = new UnitSnapshot((nint)0x2000, 0x80, 5, 30, 50, 2, true, 4, 3, 6, 4, 3, 70, 60, targetRaw, 18, 30);
        UnitSnapshot TargetWithBodyItem(int itemId, nint ptr)
        {
            var raw = (byte[])targetRaw.Clone();
            raw[0x70] = (byte)itemId;
            return target with { Ptr = ptr, Raw = raw };
        }

        var attackerRaw = new byte[0x200];
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

    private static void TestDclAbilityMetadata(string abilityCatalogPath)
    {
        string metadataPath = Path.Combine(Path.GetTempPath(), $"fft-dcl-ability-{Guid.NewGuid():N}.csv");
        string invalidPath = Path.Combine(Path.GetTempPath(), $"fft-dcl-ability-invalid-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(metadataPath,
                "ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count\n" +
                "1,1,healing,none,none,none,managed_resource_commit,17,0\n" +
                "101,1,physical_damage,crush,physical_contest,none,managed_multistrike,11,3\n" +
                "173,1,magic_damage,magic_untyped,magic_evade_per_target_then_status_contest,mixed,native_multistrike_status_rider,9,0\n" +
                "358,1,physical_damage,weapon_defined,physical_contest,none,native_multistrike,7,0\n" +
                "16,0,magic_damage,elemental,magic_evade_per_target,none,none_or_catalog_visuals,22,0\n");
            var catalog = AbilityCatalog.Load(abilityCatalogPath, metadataPath);
            Check(catalog.Loaded, $"base ability catalog must survive metadata loading: {catalog.Describe()}");
            Check(catalog.MetadataCount == 4 && string.IsNullOrWhiteSpace(catalog.MetadataError),
                $"only approved metadata rows should load: {catalog.Describe()}");
            Check(catalog.TryGet(1, out var cure), "metadata catalog should preserve Cure");
            var cureVars = VariablesFor(cure);
            CheckVar(cureVars, "ability.dcl.approved", 1);
            CheckVar(cureVars, "ability.dcl.power", 17);
            CheckVar(cureVars, "ability.dcl.strike_count", 0);
            CheckVar(cureVars, "ability.dcl.action_kind_healing", 1);
            CheckVar(cureVars, "ability.dcl.action_kind_magic_damage", 0);
            CheckVar(cureVars, "ability.dcl.damage_type_none", 1);
            CheckVar(cureVars, "ability.dcl.side_effect_managed_resource_commit", 1);

            Check(catalog.TryGet(101, out var pummel), "metadata catalog should preserve Pummel");
            var pummelVars = VariablesFor(pummel);
            CheckVar(pummelVars, "ability.dcl.approved", 1);
            CheckVar(pummelVars, "ability.dcl.power", 11);
            CheckVar(pummelVars, "ability.dcl.strike_count", 3);
            CheckVar(pummelVars, "ability.dcl.action_kind_physical_damage", 1);
            CheckVar(pummelVars, "ability.dcl.damage_type_crush", 1);
            CheckVar(pummelVars, "ability.dcl.side_effect_managed_multistrike", 1);
            Check(pummel.DclMetadata.IsManagedMultistrike,
                "approved Pummel metadata should arm the managed multistrike route");

            Check(catalog.TryGet(173, out var celestialVoid), "metadata catalog should preserve Celestial Void");
            var celestialVars = VariablesFor(celestialVoid);
            CheckVar(celestialVars, "ability.dcl.strike_count", 0);
            CheckVar(celestialVars, "ability.dcl.avoidance_magic_evade_per_target_then_status_contest", 1);
            CheckVar(celestialVars, "ability.dcl.side_effect_native_multistrike_status_rider", 1);
            Check(celestialVoid.DclMetadata.IsNativeMultistrike &&
                  !celestialVoid.DclMetadata.IsManagedMultistrike &&
                  celestialVoid.DclMetadata.HasManagedStatusRider,
                "the composite policy must preserve native repetition and managed status-rider ownership");

            Check(catalog.TryGet(358, out var barrage), "metadata catalog should preserve Barrage");
            var barrageVars = VariablesFor(barrage);
            CheckVar(barrageVars, "ability.dcl.strike_count", 0);
            CheckVar(barrageVars, "ability.dcl.side_effect_native_multistrike", 1);
            Check(barrage.DclMetadata.IsNativeMultistrike && !barrage.DclMetadata.IsManagedMultistrike,
                "Barrage metadata must not arm the aggregate managed-multistrike route over four native repeats");

            Check(catalog.TryGet(16, out var fire), "metadata catalog should preserve Fire");
            var fireVars = VariablesFor(fire);
            CheckVar(fireVars, "ability.dcl.approved", 0);
            CheckVar(fireVars, "ability.dcl.damage_type_elemental", 0);
            CheckVar(fireVars, "ability.dcl.strike_count", 0);

            File.WriteAllText(invalidPath,
                "ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count\n" +
                "1,1,healing,none,none,none,managed_resource_commit,17,-1\n");
            var failClosed = AbilityCatalog.Load(abilityCatalogPath, invalidPath);
            Check(failClosed.Loaded && failClosed.Count == 512,
                "invalid DCL metadata must not disable the vanilla ability catalog");
            Check(failClosed.MetadataCount == 0 && !string.IsNullOrWhiteSpace(failClosed.MetadataError),
                "invalid strike_count metadata must fail closed with an explicit diagnostic");
            Check(failClosed.TryGet(1, out var unchangedCure), "fail-closed metadata must preserve Cure");
            CheckVar(VariablesFor(unchangedCure), "ability.dcl.approved", 0);

            File.WriteAllText(invalidPath,
                "ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count\n" +
                "101,1,physical_damage,crush,physical_contest,none,managed_multistrike,11,1\n");
            var incompleteMultistrike = AbilityCatalog.Load(abilityCatalogPath, invalidPath);
            Check(incompleteMultistrike.MetadataCount == 0 && !string.IsNullOrWhiteSpace(incompleteMultistrike.MetadataError),
                "managed_multistrike without at least two authored strikes must fail closed");

            File.WriteAllText(invalidPath,
                "ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count\n" +
                "1,1,healing,none,none,none,managed_resource_commit,17,3\n");
            var strayStrikeCount = AbilityCatalog.Load(abilityCatalogPath, invalidPath);
            Check(strayStrikeCount.MetadataCount == 0 && !string.IsNullOrWhiteSpace(strayStrikeCount.MetadataError),
                "strike_count outside managed_multistrike must fail closed");
        }
        finally
        {
            if (File.Exists(metadataPath)) File.Delete(metadataPath);
            if (File.Exists(invalidPath)) File.Delete(invalidPath);
        }
    }

    private static void TestItemCatalog(ItemCatalog catalog)
    {
        Check(catalog.Count == 261, $"item catalog expected 261 rows, got {catalog.Count}");

        Check(catalog.TryGet(1, out var dagger), "item catalog should contain Dagger id 1");
        var daggerVars = VariablesFor(dagger);
        CheckVar(daggerVars, "item.id", 1);
        CheckVar(daggerVars, "item.category_knife", 1);
        CheckVar(daggerVars, "item.weaponRange", 1);
        CheckVar(daggerVars, "item.atkflag_throwable", 1);
        CheckVar(daggerVars, "item.atkflag_twoswords", 1);
        CheckVar(daggerVars, "item.atkflag_striking", 1);
        CheckVar(daggerVars, "item.atkflag_lunging", 0);

        Check(catalog.TryGet(35, out var excalibur), "item catalog should contain Excalibur id 35");
        var excaliburVars = VariablesFor(excalibur);
        CheckVar(excaliburVars, "item.innate_haste", 1);
        CheckVar(excaliburVars, "item.absorb_holy", 1);
        CheckVar(excaliburVars, "item.strong_holy", 1);
        CheckVar(excaliburVars, "item.strong_fire", 0);

        Check(catalog.TryGet(142, out var venetianShield), "item catalog should contain Venetian Shield id 142");
        var shieldVars = VariablesFor(venetianShield);
        CheckVar(shieldVars, "item.shieldPhysicalEvasion", 50);
        CheckVar(shieldVars, "item.shieldMagicalEvasion", 25);
        CheckVar(shieldVars, "item.halve_ice", 1);
        CheckVar(shieldVars, "item.halve_lightning", 1);
        CheckVar(shieldVars, "item.halve_fire", 1);
        CheckVar(shieldVars, "item.halve_holy", 0);

        Check(catalog.TryGet(260, out var ringOfAptitude), "item catalog should contain Ring of Aptitude id 260");
        CheckVar(VariablesFor(ringOfAptitude), "item.boostJp", 1);

        var defaultContext = MakeFormulaContext();
        ItemCatalogEntry.AddDefaultVariables(defaultContext, "item", 999);
        var defaultNames = FormulaVariableNames(defaultContext);
        Check(defaultNames.SetEquals(daggerVars.Keys),
            "item default variable names should match loaded-row variable names");
    }

    private static Dictionary<string, int> VariablesFor(AbilityCatalogEntry entry)
    {
        var context = MakeFormulaContext();
        entry.AddVariables(context, "ability");
        return FormulaVariables(context);
    }

    private static Dictionary<string, int> VariablesFor(ItemCatalogEntry entry)
    {
        var context = MakeFormulaContext();
        entry.AddVariables(context, "item");
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

        cache.Record(3, casterIdx: 2, actionType: 1, abilityId: 99, timestampTicks: 100, actionPayload: 71);
        Check(cache.TryGetLatest(3, nowTicks: 150, maxAgeTicks: 50, out var first), "fresh DCL cache entry should hit at max age");
        Check(first.CasterIdx == 2 && first.ActionType == 1 && first.AbilityId == 99 && first.TimestampTicks == 100 && first.ActionPayload == 71,
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

        cache.Record(4, casterIdx: 5, actionType: 0x0B, abilityId: 13, timestampTicks: 300,
            returnRva: DclCalcProvenance.ForecastTraceReturnRva, battleState: 0x19);
        Check(!cache.TryGetLatestConfirmedExecution(4, nowTicks: 300, maxAgeTicks: 50, out _),
            "forecast provenance must never satisfy the confirmed-execution gate");
        cache.Record(4, casterIdx: 5, actionType: 0x0B, abilityId: 13, timestampTicks: 301,
            returnRva: DclCalcProvenance.OuterSweepReturnRva, battleState: DclCalcProvenance.ConfirmedExecutionBattleState);
        Check(cache.TryGetLatestConfirmedExecution(4, nowTicks: 301, maxAgeTicks: 50, out var execution) &&
              execution.Origin == DclCalcOrigin.OuterSweep && execution.IsConfirmedExecution,
            "the LT28 outer-sweep/state-0x2A pair must satisfy the confirmed-execution gate");
        cache.Record(4, casterIdx: 5, actionType: 1, abilityId: 0, timestampTicks: 302,
            returnRva: DclCalcProvenance.NestedRendAttackReturnRva, battleState: DclCalcProvenance.ConfirmedExecutionBattleState);
        Check(cache.TryGetLatestConfirmedExecution(4, nowTicks: 302, maxAgeTicks: 50, out var preservedOuter) &&
              preservedOuter.AbilityId == 13 && preservedOuter.ActionType == 0x0B,
            "a nested synthetic Rend Attack row must not overwrite the outer action identity");

        var planCache = new DclStatusPlanCache();
        var prepared = new DclPreparedStatusPlan(5, 0x0B, 13, 400,
            [new DclStatusWrite("wall", 3, 0x20, true, 9, 12, false, false, 0)]);
        planCache.Record(4, prepared);
        Check(planCache.TryGet(4, 5, 0x0B, 13, 420, 50, out var cachedPlan) && cachedPlan.Writes.Count == 1,
            "post-calc status decisions should be reusable at pre-clamp without a second roll");
        planCache.Invalidate(4, 5, 0x0B, 13);
        Check(!planCache.TryGet(4, 5, 0x0B, 13, 420, 50, out _),
            "a consumed post-calc status plan should be invalidated");
    }

    private static void TestDclComputePointNumericPlan()
    {
        var damage = DclComputePointNumericPlan.Build(
            12, 0, 0, 0, 0x05,
            authoredHpDebit: 111,
            authoredHpCredit: null,
            authoredMpDebit: null,
            authoredMpCredit: null,
            forcedMiss: false,
            controlResultFlags: true,
            preserveResultFlagsMask: 0x0F);
        Check(damage.HpDebit == 111 && damage.HpCredit == 0 && damage.MpDebit == 0 && damage.MpCredit == 0,
            "compute-point damage plan should replace only the authored HP debit channel");
        Check(damage.ResultFlags == 0x85 && damage.WriteHpDebit && !damage.WriteHpCredit &&
              !damage.WriteMpDebit && !damage.WriteMpCredit && damage.WriteResultFlags,
            "compute-point damage plan should preserve low flags and expose HP damage");

        var healing = DclComputePointNumericPlan.Build(
            0, 12, 0, 0, 0,
            null, 222, null, null,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(healing.HpCredit == 222 && healing.ResultFlags == DclResultFlags.HpCredit && healing.WriteHpCredit,
            "compute-point plan should author HP credit and its result flag");

        var mpDebit = DclComputePointNumericPlan.Build(
            0, 0, 12, 0, 0,
            null, null, 333, null,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(mpDebit.MpDebit == 333 && mpDebit.ResultFlags == DclResultFlags.MpDebit && mpDebit.WriteMpDebit,
            "compute-point plan should author MP debit and its result flag");

        var mpCredit = DclComputePointNumericPlan.Build(
            0, 0, 0, 12, 0,
            null, null, null, 444,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(mpCredit.MpCredit == 444 && mpCredit.ResultFlags == DclResultFlags.MpCredit && mpCredit.WriteMpCredit,
            "compute-point plan should author MP credit and its result flag");

        var composite = DclComputePointNumericPlan.Build(
            1, 2, 3, 4, 0x03,
            10, 20, 30, 40,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(composite.ResultFlags == 0xF3 && composite.WriteHpDebit && composite.WriteHpCredit &&
              composite.WriteMpDebit && composite.WriteMpCredit && composite.WriteResultFlags,
            "compute-point plan should compose all four numeric channels atomically");

        var preservedNatural = DclComputePointNumericPlan.Build(
            4, 0, 0, 5, 0,
            null, null, 6, null,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(preservedNatural.HpDebit == 4 && preservedNatural.MpDebit == 6 && preservedNatural.MpCredit == 5 &&
              preservedNatural.ResultFlags == 0xB0 && !preservedNatural.WriteHpDebit &&
              preservedNatural.WriteMpDebit && !preservedNatural.WriteMpCredit,
            "an authored channel should retain unowned natural channels in value and flag composition");

        var clamped = DclComputePointNumericPlan.Build(
            0, 0, 0, 0, 0,
            -1, short.MaxValue + 1, 99999, -99999,
            forcedMiss: false, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(clamped.HpDebit == 0 && clamped.HpCredit == short.MaxValue &&
              clamped.MpDebit == short.MaxValue && clamped.MpCredit == 0 && clamped.ResultFlags == 0x60,
            "compute-point plan should clamp every authored channel before composing flags");

        var miss = DclComputePointNumericPlan.Build(
            12, 13, 14, 15, 0xA7,
            100, 200, 300, 400,
            forcedMiss: true, controlResultFlags: true, preserveResultFlagsMask: 0x0F);
        Check(miss.HpDebit == 0 && miss.HpCredit == 0 && miss.MpDebit == 0 && miss.MpCredit == 0 &&
              miss.ResultFlags == 0xA7 && miss.WriteHpDebit && miss.WriteHpCredit &&
              miss.WriteMpDebit && miss.WriteMpCredit && !miss.WriteResultFlags,
            "forced miss should zero every numeric channel while preserving the connected native flag for delivery");

        var nativeFlags = DclComputePointNumericPlan.Build(
            5, 0, 0, 0, 0x2A,
            50, null, null, null,
            forcedMiss: false, controlResultFlags: false, preserveResultFlagsMask: 0x0F);
        Check(nativeFlags.HpDebit == 50 && nativeFlags.ResultFlags == 0x2A && !nativeFlags.WriteResultFlags,
            "disabled result-flag control should leave the native byte untouched");
    }

    private static void TestDclComputePointCache(ItemCatalog catalog)
    {
        var cache = new DclComputePointCache();
        var expected = new DclComputedNumericResult(
            CasterIdx: 3,
            ActionType: 0x0B,
            AbilityId: 265,
            ActionPayload: 0,
            TimestampTicks: 100,
            NaturalHpDebit: 122,
            NaturalHpCredit: 0,
            NaturalMpDebit: 0,
            NaturalMpCredit: 0,
            NaturalResultFlags: 0x80,
            HpDebit: 4095,
            HpCredit: 0,
            MpDebit: 0,
            MpCredit: 0,
            ResultFlags: 0x80);
        cache.Record(16, expected);
        Check(cache.TryGet(16, 3, 0x0B, 265, 0, 150, 50, out var actual) && actual == expected,
            "compute-point cache should preserve the exact natural and rewritten execution result");
        Check(!cache.TryGet(16, 3, 0x0B, 266, 0, 150, 50, out _),
            "compute-point cache must reject a different action identity");
        Check(!cache.TryGet(16, 3, 0x0B, 265, 1, 150, 50, out _),
            "compute-point cache must include the active action payload in its identity");
        Check(!cache.TryGet(16, 3, 0x0B, 265, 0, 151, 50, out _),
            "compute-point cache must expire after its bounded delivery window");
        cache.Invalidate(16);
        Check(!cache.TryGet(16, 3, 0x0B, 265, 0, 100, 50, out _),
            "compute-point cache must retire a delivered result");

        var valid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclComputePointNumericEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
        };
        Check(RuntimeSettingsValidator.Validate(valid, catalog).Success,
            "AI-facing compute-point writer should validate with a numeric DCL formula");

        var noPipeline = new RuntimeSettings
        {
            DclComputePointNumericEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
        };
        Check(!RuntimeSettingsValidator.Validate(noPipeline, catalog).Success,
            "compute-point writer must fail closed without the DCL pipeline");

        var staticForceConflict = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclComputePointNumericEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
            StagedBundleForceDmg = 4095,
        };
        Check(!RuntimeSettingsValidator.Validate(staticForceConflict, catalog).Success,
            "the permanent writer must reject the LT35 static damage force surface");

    }

    private static void TestDclHitDecisionCache()
    {
        var cache = new DclHitDecisionCache();
        var expected = new DclHitDecision(
            Hit: false,
            Pct: 50,
            Roll: 12,
            PhysicalOutcome: DclPhysicalOutcome.Defended,
            AttackSkill: 13,
            DefenseKind: DclDefenseKind.Block,
            DefenseTarget: 10,
            DefenseRoll: 8,
            Multistrike: new DclMultistrikeAggregate(
                StrikeCount: 3,
                HitCount: 1,
                CriticalCount: 0,
                AttackMissCount: 1,
                FumbleCount: 0,
                EvadedCount: 0,
                DefendedCount: 1,
                ParryAttempts: 0,
                BlockAttempts: 1,
                TotalDebit: 0));

        Check(
            !cache.TryGetLatest(7, nowTicks: 100, ttlTicks: 50, out _, out _, out _, out _, out _),
            "empty DCL hit-decision cache should miss");

        cache.Record(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, expected, nowTicks: 100);
        Check(
            cache.TryGetLatest(7, nowTicks: 150, ttlTicks: 50, out var actual, out int casterIdx, out int abilityId, out int actionType, out int actionPayload),
            "fresh DCL hit-decision cache entry should hit at max age");
        Check(actual == expected, "late DCL consumer should receive the cached hit decision");
        Check(casterIdx == 2 && abilityId == 16 && actionType == 1 && actionPayload == 11,
            "late DCL consumer should receive the cached action key");
        Check(cache.TryMarkDefenseCommitted(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, out var committed) && committed == expected,
            "the successful apply consumer should commit a cached finite defense exactly once");
        Check(!cache.TryMarkDefenseCommitted(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, out _),
            "a repeated apply callback must not spend the same finite defense twice");

        Check(!cache.MarkConsumed(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, byOutcomeDelivery: true),
            "outcome delivery alone must preserve the decision for a later pre-clamp consumer");
        Check(cache.TryGetLatest(7, nowTicks: 150, ttlTicks: 50, out _, out _, out _, out _, out _),
            "the first half of the consumption handshake must leave the decision readable");
        Check(cache.MarkConsumed(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, byOutcomeDelivery: false),
            "pre-clamp should retire a decision already delivered by the selector");
        Check(!cache.TryGetLatest(7, nowTicks: 150, ttlTicks: 50, out _, out _, out _, out _, out _),
            "a completed selector/pre-clamp handshake must retire the decision immediately");

        cache.Record(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, expected, nowTicks: 200);
        Check(!cache.MarkConsumed(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, byOutcomeDelivery: false),
            "pre-clamp alone must preserve the decision for later outcome delivery");
        Check(!cache.MarkConsumed(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, byOutcomeDelivery: false),
            "duplicate pre-clamp delivery must be idempotent");
        Check(cache.MarkConsumed(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, byOutcomeDelivery: true),
            "selector should retire a decision already consumed by pre-clamp");

        cache.Record(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 11, expected, nowTicks: 300);
        Check(!cache.TryGet(7, casterIdx: 2, abilityId: 16, actionType: 1, actionPayload: 71, nowTicks: 300, ttlTicks: 50, out _),
            "a different active weapon payload must not reuse the first hand's hit decision");

        Check(
            !cache.TryGetLatest(7, nowTicks: 351, ttlTicks: 50, out _, out _, out _, out _, out _),
            "expired DCL hit-decision cache entry should miss");
        Check(
            !cache.TryGetLatest(-1, nowTicks: 100, ttlTicks: 50, out _, out _, out _, out _, out _),
            "negative DCL hit-decision target index should miss");
        Check(
            !cache.TryGetLatest(64, nowTicks: 100, ttlTicks: 50, out _, out _, out _, out _, out _),
            "out-of-range DCL hit-decision target index should miss");
    }

    private static void TestDclMagicEvade(ItemCatalog catalog)
    {
        Check(DclMagicEvade.EvadePercent(-5, 50) == 0,
            "negative Magic Evade should clamp to zero");
        Check(DclMagicEvade.EvadePercent(35, 50) == 35 && DclMagicEvade.HitChancePercent(35, 50) == 65,
            "Magic Evade should convert directly into the complementary hit chance");
        Check(DclMagicEvade.EvadePercent(80, 50) == 50 && DclMagicEvade.HitChancePercent(80, 50) == 50,
            "stacked Magic Evade should respect the configured anti-immunity cap");

        var valid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclHitChanceFormula = "100",
            DclMagicEvadeEnabled = true,
            DclMagicEvadeConditionFormula = "ability.formula == 8",
            DclMagicEvadeFormula =
                "max(targetSlot.right_shield.shieldMagicalEvasion, max(targetSlot.left_shield.shieldMagicalEvasion, targetSlot.accessory.accessoryMagicalEvasion))",
            DclMagicEvadeCapPct = 50,
            DclMissOutputControlEnabled = true,
            DclMissSelectorOutcomeEnabled = true,
            DclPreviewHitPctEnabled = true,
            ItemTableEvadeZeroEnabled = true,
            EvadeCopierOverrideEnabled = true,
            EvadeCopierOverride46 = 0,
            EvadeCopierOverride47 = 0,
            EvadeCopierOverride48 = 0,
            EvadeCopierOverride49 = 0,
            EvadeCopierOverride4A = 0,
            EvadeCopierOverride4B = 0,
            EvadeCopierOverride4C = 0,
            EvadeCopierOverride4D = 0,
            EvadeCopierOverride4E = 0,
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Accessory", Offset = 0x1E, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "Right Shield", Offset = 0x22, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "Left Shield", Offset = 0x26, Width = "UInt16" },
            ],
        };
        var validReport = RuntimeSettingsValidator.Validate(valid, catalog);
        Check(validReport.Success,
            "offensive formula-0x08 Magic Evade with equipment-derived magnitude should pass validation: " +
            string.Join(" | ", validReport.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));

        var invalid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclHitChanceFormula = "100",
            DclMagicEvadeEnabled = true,
            DclMagicEvadeConditionFormula = "",
            DclMagicEvadeFormula = "missingMagicEvade",
            DclMagicEvadeCapPct = 101,
            ItemTableEvadeZeroEnabled = true,
            EvadeCopierOverrideEnabled = true,
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalid, catalog);
        Check(invalidReport.Findings.Any(f => f.Scope == "DclMagicEvadeCapPct" && f.Severity == "ERROR"),
            "validator should reject a Magic Evade cap above 100");
        Check(invalidReport.Findings.Any(f => f.Scope == "DclMagicEvadeEnabled" && f.Severity == "ERROR"),
            "validator should require clean authored-miss delivery and a non-empty applicability formula");
        Check(invalidReport.Findings.Any(f => f.Scope == "DclMagicEvadeFormula" && f.Message.Contains("missingMagicEvade")),
            "validator should reject unknown variables in the Magic Evade formula");
    }

    private static void TestDclWeaponSkill(ItemCatalog catalog, AbilityCatalog abilityCatalog)
    {
        static string WeaponFamilyFormula(string slot) =>
            $"if({slot}.itemId == 0 || {slot}.category_none, 0, " +
            $"if({slot}.category_knife, 1, if({slot}.category_ninjablade, 2, " +
            $"if({slot}.category_sword, 3, if({slot}.category_knightsword, 4, " +
            $"if({slot}.category_fellsword, 5, if({slot}.category_katana, 6, " +
            $"if({slot}.category_axe, 7, if({slot}.category_flail, 8, " +
            $"if({slot}.category_rod, 9, if({slot}.category_staff, 10, " +
            $"if({slot}.category_pole, 11, if({slot}.category_polearm, 12, " +
            $"if({slot}.category_crossbow, 13, if({slot}.category_bow, 14, " +
            $"if({slot}.category_gun, 15, if({slot}.category_book, 16, " +
            $"if({slot}.category_instrument, 17, if({slot}.category_bag, 18, " +
            $"if({slot}.category_cloth, 19, if({slot}.category_throwing, 20, " +
            $"if({slot}.category_bomb, 21, -1))))))))))))))))))))))";

        var settings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclHitChanceFormula = "100",
            DclPreviewHitPctEnabled = true,
            DclPhysicalContestEnabled = true,
            DclPhysicalContestConditionFormula = "action.type == 1 && action.abilityId == 0",
            DclAttackSkillFormula = "dcl.weaponSkill + dcl.zodiacHitModifier + dcl.pointBlank * const.dclPointBlankSkillModifier",
            DclDodgeFormula = "dcl.dodge",
            DclParryFormula = "if(dcl.isMissile, 0, mulDiv(dcl.targetWeaponSkill, 1, 2) + 3)",
            DclBlockFormula = "const.dclBlockBase + mulDiv(tslot.rightshield.shieldPhysicalEvasion, 1, const.dclShieldEvadePctPerBlockPoint)",
            DclDefenseAllowedFormula = "!dcl.facingBack",
            DclDefenseModifierFormula = "mulDiv(50 - t.brave, 1, 20) + dcl.facingSide * const.dclFlankDefenseModifier",
            DclParryUsesFormula = "tslot.rightweapon.itemId > 0 && tslot.rightweapon.itemId < 255",
            DclBlockUsesFormula = "tslot.rightshield.isShield",
            DclMissOutputControlEnabled = true,
            DclMissSelectorOutcomeEnabled = true,
            DclMissSuppressReactionsEnabled = true,
            ItemTableEvadeZeroEnabled = true,
            EvadeCopierOverrideEnabled = true,
            EvadeCopierOverride46 = 0,
            EvadeCopierOverride47 = 0,
            EvadeCopierOverride48 = 0,
            EvadeCopierOverride49 = 0,
            EvadeCopierOverride4A = 0,
            EvadeCopierOverride4B = 0,
            EvadeCopierOverride4C = 0,
            EvadeCopierOverride4D = 0,
            EvadeCopierOverride4E = 0,
            FormulaVariables = new Dictionary<string, int>
            {
                ["dclWeaponSkillCap"] = 16,
                ["dclSwordMasterAbilityId"] = 481,
                ["dclSwordMasterBonus"] = 2,
                // Mechanism fixtures, not final balance decisions.
                ["dclPaToStOffset"] = 4,
                ["dclPenFloorPermille"] = 200,
                ["dclCrossbowOvercapDamagePermille"] = 250,
                ["dclGunOvercapPenetrationPermille"] = 250,
                ["dclCrossbowDrPermille"] = 750,
                ["dclGunDrPermille"] = 500,
                ["dclGlobalScalePermille"] = 1000,
                ["dclDodgeBase"] = 7,
                ["dclEvadePctPerDodgePoint"] = 10,
                ["dclWeightPerDodgePenalty"] = 10,
                ["dclFlankDefenseModifier"] = -2,
                ["dclPointBlankSkillModifier"] = -2,
                ["dclBlockBase"] = 7,
                ["dclShieldEvadePctPerBlockPoint"] = 10,
            },
            FormulaTables = new Dictionary<string, List<int>>
            {
                ["weaponSkillBaseByGrade"] = [5, 7, 9, 11, 13],
                ["weaponSkillRatePermilleByGrade"] = [200, 320, 500, 720, 1000],
                ["gurpsSw"] = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39],
                ["gurpsThr"] = [0, 0, 1, 2, 2, 3, 4, 4, 5, 6, 7, 7, 8, 9, 9, 10, 11, 11, 12, 13, 14, 14, 15, 16, 16, 17, 18, 18, 19, 20, 21, 21, 22, 23, 23, 24, 25, 25, 26, 27],
            },
            FormulaMatrices = new Dictionary<string, List<List<int>>>
            {
                ["drMatrix"] =
                [
                    [9, 8, 3, 8],
                    [2, 2, 2, 2],
                    [0, 0, 0, 0],
                ],
            },
            FormulaMaps = new Dictionary<string, Dictionary<string, int>>
            {
                ["weaponGradeByJobFamily"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["2850"] = 4, // Ninja (89) x Ninja Blade (2) => grade A.
                    ["2861"] = 2, // Ninja (89) x Crossbow (13) => grade C mechanism fixture.
                    ["2863"] = 3, // Ninja (89) x Gun (15) => grade B mechanism fixture.
                },
                ["equipmentWeightByItemId"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["11"] = 3,   // Ninja Blade mechanism fixture.
                    ["71"] = 6,   // Romandan Pistol mechanism fixture.
                    ["77"] = 4,   // Bowgun mechanism fixture.
                    ["142"] = 5,  // Venetian Shield mechanism fixture.
                    ["172"] = 26, // Leather Armor occupies the native heavy Armor class.
                },
            },
            DclDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "dcl.weaponSkillInvestmentMilli", Formula = "2500 * max(0, a.jobLevel - 1) + mulDiv(250 * max(1, a.jobLevel), max(0, a.level - 1), 8)" },
                new FormulaDerivedVariable
                {
                    Name = "dcl.hasSwordMaster",
                    Formula = "a.supportAbilityId == const.dclSwordMasterAbilityId || " +
                              "a.innateAbilityId1 == const.dclSwordMasterAbilityId || a.innateAbilityId2 == const.dclSwordMasterAbilityId || " +
                              "a.innateAbilityId3 == const.dclSwordMasterAbilityId || a.innateAbilityId4 == const.dclSwordMasterAbilityId"
                },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillBonus", Formula = "dcl.hasSwordMaster * const.dclSwordMasterBonus" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponFamily", Formula = WeaponFamilyFormula("aslot.rightweapon") },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponFamily", Formula = WeaponFamilyFormula("aslot.leftweapon") },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponGrade", Formula = "mapOr(weaponGradeByJobFamily, a.jobId * 32 + dcl.rightWeaponFamily, 0)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponGrade", Formula = "mapOr(weaponGradeByJobFamily, a.jobId * 32 + dcl.leftWeaponFamily, 0)" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkillBase", Formula = "table(weaponSkillBaseByGrade, dcl.rightWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkillBase", Formula = "table(weaponSkillBaseByGrade, dcl.leftWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkillRatePermille", Formula = "table(weaponSkillRatePermilleByGrade, dcl.rightWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkillRatePermille", Formula = "table(weaponSkillRatePermilleByGrade, dcl.leftWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkillGrowth", Formula = "mulDiv(dcl.weaponSkillInvestmentMilli, dcl.rightWeaponSkillRatePermille, 1000000)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkillGrowth", Formula = "mulDiv(dcl.weaponSkillInvestmentMilli, dcl.leftWeaponSkillRatePermille, 1000000)" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkillRaw", Formula = "dcl.rightWeaponSkillBase + dcl.rightWeaponSkillGrowth + dcl.weaponSkillBonus" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkillRaw", Formula = "dcl.leftWeaponSkillBase + dcl.leftWeaponSkillGrowth + dcl.weaponSkillBonus" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkill", Formula = "min(const.dclWeaponSkillCap, dcl.rightWeaponSkillRaw)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkill", Formula = "min(const.dclWeaponSkillCap, dcl.leftWeaponSkillRaw)" },
                new FormulaDerivedVariable { Name = "dcl.rightWeaponSkillExcess", Formula = "max(0, dcl.rightWeaponSkillRaw - const.dclWeaponSkillCap)" },
                new FormulaDerivedVariable { Name = "dcl.leftWeaponSkillExcess", Formula = "max(0, dcl.leftWeaponSkillRaw - const.dclWeaponSkillCap)" },
                new FormulaDerivedVariable { Name = "dcl.rightSkillPrimary", Formula = "aslot.rightweapon.category_crossbow || aslot.rightweapon.category_gun" },
                new FormulaDerivedVariable { Name = "dcl.leftSkillPrimary", Formula = "aslot.leftweapon.category_crossbow || aslot.leftweapon.category_gun" },
                new FormulaDerivedVariable { Name = "dcl.rightDamageInput", Formula = "if(dcl.rightSkillPrimary, dcl.rightWeaponSkill, a.rawPa)" },
                new FormulaDerivedVariable { Name = "dcl.leftDamageInput", Formula = "if(dcl.leftSkillPrimary, dcl.leftWeaponSkill, a.rawPa)" },
                new FormulaDerivedVariable { Name = "dcl.rightOvercapDamageUnits", Formula = "aslot.rightweapon.category_crossbow * dcl.rightWeaponSkillExcess" },
                new FormulaDerivedVariable { Name = "dcl.leftOvercapDamageUnits", Formula = "aslot.leftweapon.category_crossbow * dcl.leftWeaponSkillExcess" },
                new FormulaDerivedVariable { Name = "dcl.rightOvercapPenetrationUnits", Formula = "aslot.rightweapon.category_gun * dcl.rightWeaponSkillExcess" },
                new FormulaDerivedVariable { Name = "dcl.leftOvercapPenetrationUnits", Formula = "aslot.leftweapon.category_gun * dcl.leftWeaponSkillExcess" },
                // Compatibility aliases deliberately remain right-hand until the per-strike hand gate is proven.
                new FormulaDerivedVariable { Name = "dcl.weaponFamily", Formula = "dcl.rightWeaponFamily" },
                new FormulaDerivedVariable { Name = "dcl.weaponGrade", Formula = "dcl.rightWeaponGrade" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillBase", Formula = "dcl.rightWeaponSkillBase" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillRatePermille", Formula = "dcl.rightWeaponSkillRatePermille" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillGrowth", Formula = "dcl.rightWeaponSkillGrowth" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillRaw", Formula = "dcl.rightWeaponSkillRaw" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkill", Formula = "dcl.rightWeaponSkill" },
                new FormulaDerivedVariable { Name = "dcl.weaponSkillExcess", Formula = "dcl.rightWeaponSkillExcess" },
                // Modernized LT7 physical spine. Right-hand routing remains an explicit live-test gate.
                new FormulaDerivedVariable { Name = "dcl.isCut", Formula = "aslot.rightweapon.category_sword || aslot.rightweapon.category_knightsword || aslot.rightweapon.category_katana || aslot.rightweapon.category_ninjablade || aslot.rightweapon.category_cloth" },
                new FormulaDerivedVariable { Name = "dcl.isThrust", Formula = "aslot.rightweapon.category_knife || aslot.rightweapon.category_polearm" },
                new FormulaDerivedVariable { Name = "dcl.isMissile", Formula = "aslot.rightweapon.category_bow || aslot.rightweapon.category_crossbow || aslot.rightweapon.category_gun" },
                new FormulaDerivedVariable { Name = "dcl.typeIndex", Formula = "if(dcl.isCut, 0, if(dcl.isThrust, 1, if(dcl.isMissile, 3, 2)))" },
                new FormulaDerivedVariable { Name = "dcl.woundNum", Formula = "if(dcl.isCut, 3, if(dcl.isThrust, 2, 1))" },
                new FormulaDerivedVariable { Name = "dcl.woundDen", Formula = "if(dcl.isCut, 2, 1)" },
                new FormulaDerivedVariable { Name = "armor.heavy", Formula = "tslot.body.category_armor" },
                new FormulaDerivedVariable { Name = "armor.clothes", Formula = "tslot.body.category_clothing" },
                new FormulaDerivedVariable { Name = "armor.robe", Formula = "tslot.body.category_robe" },
                new FormulaDerivedVariable { Name = "armor.classIndex", Formula = "if(armor.heavy, 0, if(armor.clothes, 1, 2))" },
                new FormulaDerivedVariable { Name = "dcl.zodiacValid", Formula = "a.zodiac < 12 && t.zodiac < 12" },
                new FormulaDerivedVariable { Name = "dcl.zodiacDelta", Formula = "if(dcl.zodiacValid, abs(a.zodiac - t.zodiac), 0)" },
                new FormulaDerivedVariable { Name = "dcl.zodiacDistance", Formula = "if(dcl.zodiacValid, min(dcl.zodiacDelta, 12 - dcl.zodiacDelta), -1)" },
                new FormulaDerivedVariable { Name = "dcl.zodiacPermille", Formula = "if(dcl.zodiacDistance == 6, 1200, if(dcl.zodiacDistance == 3, 900, if(dcl.zodiacDistance == 0 || dcl.zodiacDistance == 4, 1100, 1000)))" },
                new FormulaDerivedVariable { Name = "dcl.zodiacHitModifier", Formula = "dcl.zodiacDistance == 6" },
                new FormulaDerivedVariable { Name = "dcl.st", Formula = "dcl.rightDamageInput + const.dclPaToStOffset" },
                new FormulaDerivedVariable { Name = "dcl.base", Formula = "if(dcl.isThrust, tableClamp(gurpsThr, dcl.st), tableClamp(gurpsSw, dcl.st))" },
                new FormulaDerivedVariable { Name = "dcl.overcapRaw", Formula = "mulDiv(dcl.rightOvercapDamageUnits, const.dclCrossbowOvercapDamagePermille, 1000)" },
                new FormulaDerivedVariable { Name = "dcl.wmod", Formula = "aslot.rightweapon.weaponPower" },
                new FormulaDerivedVariable { Name = "dcl.gross", Formula = "dcl.base + dcl.wmod + dcl.overcapRaw" },
                new FormulaDerivedVariable { Name = "dcl.dr", Formula = "matrixClamp(drMatrix, armor.classIndex, dcl.typeIndex)" },
                new FormulaDerivedVariable { Name = "dcl.drPermille", Formula = "if(aslot.rightweapon.category_gun, const.dclGunDrPermille, if(aslot.rightweapon.category_crossbow, const.dclCrossbowDrPermille, 1000))" },
                new FormulaDerivedVariable { Name = "dcl.skillPenetration", Formula = "mulDiv(dcl.rightOvercapPenetrationUnits, const.dclGunOvercapPenetrationPermille, 1000)" },
                new FormulaDerivedVariable { Name = "dcl.effectiveDr", Formula = "max(0, mulDiv(dcl.dr, dcl.drPermille, 1000) - dcl.skillPenetration)" },
                new FormulaDerivedVariable { Name = "dcl.penFloor", Formula = "mulDiv(dcl.gross, const.dclPenFloorPermille, 1000)" },
                new FormulaDerivedVariable { Name = "dcl.penetrating", Formula = "max(dcl.penFloor, dcl.gross - dcl.effectiveDr)" },
                new FormulaDerivedVariable { Name = "dcl.wounded", Formula = "mulDiv(dcl.penetrating, dcl.woundNum, max(1, dcl.woundDen))" },
                new FormulaDerivedVariable { Name = "dcl.traitPermille", Formula = "if(dcl.rightSkillPrimary, 1000, 760 + mulDiv(a.brave, 590, 100))" },
                new FormulaDerivedVariable { Name = "dcl.weaponModel", Formula = "max(1, mulDiv(mulDiv(mulDiv(dcl.wounded, dcl.traitPermille, 1000), dcl.zodiacPermille, 1000), const.dclGlobalScalePermille, 1000))" },
                new FormulaDerivedVariable { Name = "dcl.weightHead", Formula = "mapOr(equipmentWeightByItemId, tslot.head.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightBody", Formula = "mapOr(equipmentWeightByItemId, tslot.body.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightAccessory", Formula = "mapOr(equipmentWeightByItemId, tslot.accessory.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightRightWeapon", Formula = "mapOr(equipmentWeightByItemId, tslot.rightweapon.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightRightShield", Formula = "mapOr(equipmentWeightByItemId, tslot.rightshield.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightLeftWeapon", Formula = "mapOr(equipmentWeightByItemId, tslot.leftweapon.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightLeftShield", Formula = "mapOr(equipmentWeightByItemId, tslot.leftshield.itemId, 0)" },
                new FormulaDerivedVariable { Name = "dcl.weightTotal", Formula = "dcl.weightHead + dcl.weightBody + dcl.weightAccessory + dcl.weightRightWeapon + dcl.weightRightShield + dcl.weightLeftWeapon + dcl.weightLeftShield" },
                new FormulaDerivedVariable { Name = "dcl.movePenalty", Formula = "if(dcl.weightTotal <= 14, 0, if(dcl.weightTotal <= 28, 1, if(dcl.weightTotal <= 40, 2, 3)))" },
                new FormulaDerivedVariable { Name = "dcl.dodgeWeightPenalty", Formula = "mulDiv(dcl.weightTotal, 1, const.dclWeightPerDodgePenalty)" },
                new FormulaDerivedVariable { Name = "dcl.dodge", Formula = "max(3, const.dclDodgeBase + mulDiv(t.physEva, 1, const.dclEvadePctPerDodgePoint) + mulDiv(tslot.accessory.accessoryPhysicalEvasion, 1, const.dclEvadePctPerDodgePoint) - dcl.dodgeWeightPenalty)" },
                new FormulaDerivedVariable { Name = "dcl.facingDx", Formula = "a.x - t.x" },
                new FormulaDerivedVariable { Name = "dcl.facingDy", Formula = "a.y - t.y" },
                new FormulaDerivedVariable { Name = "dcl.facingSeparated", Formula = "abs(dcl.facingDx) + abs(dcl.facingDy) > 0" },
                new FormulaDerivedVariable { Name = "dcl.sourceDirection", Formula = "if(!dcl.facingSeparated, t.facing, if(abs(dcl.facingDx) > abs(dcl.facingDy), if(dcl.facingDx > 0, 3, 1), if(dcl.facingDy > 0, 2, 0)))" },
                new FormulaDerivedVariable { Name = "dcl.oppositeFacing", Formula = "if(t.facing == 0, 2, if(t.facing == 1, 3, if(t.facing == 2, 0, 1)))" },
                new FormulaDerivedVariable { Name = "dcl.facingFront", Formula = "dcl.sourceDirection == t.facing" },
                new FormulaDerivedVariable { Name = "dcl.facingBack", Formula = "dcl.facingSeparated && dcl.sourceDirection == dcl.oppositeFacing" },
                new FormulaDerivedVariable { Name = "dcl.facingSide", Formula = "dcl.facingSeparated && !dcl.facingFront && !dcl.facingBack" },
                new FormulaDerivedVariable { Name = "dcl.gridDistance", Formula = "abs(dcl.facingDx) + abs(dcl.facingDy)" },
                new FormulaDerivedVariable { Name = "dcl.pointBlank", Formula = "aslot.rightweapon.weaponRange == 2 && dcl.gridDistance == 1" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponSkillInvestmentMilli", Formula = "2500 * max(0, t.jobLevel - 1) + mulDiv(250 * max(1, t.jobLevel), max(0, t.level - 1), 8)" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponFamily", Formula = WeaponFamilyFormula("tslot.rightweapon") },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponGrade", Formula = "mapOr(weaponGradeByJobFamily, t.jobId * 32 + dcl.targetWeaponFamily, 0)" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponSkillBase", Formula = "table(weaponSkillBaseByGrade, dcl.targetWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponSkillRatePermille", Formula = "table(weaponSkillRatePermilleByGrade, dcl.targetWeaponGrade)" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponSkillGrowth", Formula = "mulDiv(dcl.targetWeaponSkillInvestmentMilli, dcl.targetWeaponSkillRatePermille, 1000000)" },
                new FormulaDerivedVariable { Name = "dcl.targetWeaponSkill", Formula = "min(const.dclWeaponSkillCap, dcl.targetWeaponSkillBase + dcl.targetWeaponSkillGrowth)" },
            ],
            DclDamageFormula = "if(action.type == 1 && action.abilityId == 0, dcl.weaponModel, dcl.oldDebit)",
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "RightWeapon", Offset = 0x20, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "LeftWeapon", Offset = 0x24, Width = "UInt16" },
            ],
            EquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "Head", Offset = 0x1A, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "Body", Offset = 0x1C, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "Accessory", Offset = 0x1E, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "RightWeapon", Offset = 0x20, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "RightShield", Offset = 0x22, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "LeftWeapon", Offset = 0x24, Width = "UInt16" },
                new EquipmentSlotProbe { Name = "LeftShield", Offset = 0x26, Width = "UInt16" },
            ],
        };

        static byte[] TargetRaw(int zodiac = 6, int bodyId = 172, int rightShieldId = 0xFF,
            int rightWeaponId = 19)
        {
            var raw = new byte[0x200];
            raw[0x03] = 89;
            raw[0x09] = (byte)(zodiac << 4);
            raw[0x1C] = (byte)(bodyId & 0xFF);
            raw[0x1D] = (byte)(bodyId >> 8);
            raw[0x22] = (byte)(rightShieldId & 0xFF);
            raw[0x23] = (byte)(rightShieldId >> 8);
            raw[0x20] = (byte)(rightWeaponId & 0xFF);
            raw[0x21] = (byte)(rightWeaponId >> 8);
            raw[0x4B] = 10; // Fixture C-Ev percentage; equipment evade remains separate.
            raw[0x4F] = 5;
            raw[0x50] = 5;
            raw[0x51] = 0; // Faces -Y.
            int totalJpOffset = 0x11E + (89 - 0x4A) * 2;
            raw[totalJpOffset] = 0xB8;
            raw[totalJpOffset + 1] = 0x0B; // 3000 total JP, Job Level 8.
            return raw;
        }

        var target = new UnitSnapshot((nint)0x5200, 0x80, 50, 300, 300, 2, true, 8, 6, 8, 4, 3, 70, 60,
            TargetRaw(), 20, 20);

        static UnitSnapshot Ninja(byte[] raw, nint ptr, int level = 99, int brave = 70)
            => new(ptr, 0x80, level, 300, 300, 1, false, 12, 6, 10, 5, 4, brave, 60, raw, 20, 20);

        static byte[] NinjaRaw(int weaponId, int totalJp, int supportAbilityId = 0, int innateAbilityId = 0,
            int leftWeaponId = 0xFF, int x = 5, int y = 4)
        {
            var raw = new byte[0x200];
            raw[0x03] = 89;
            raw[0x09] = 0; // Aries.
            raw[0x0A] = (byte)(innateAbilityId & 0xFF);
            raw[0x0B] = (byte)(innateAbilityId >> 8);
            raw[0x16] = (byte)(supportAbilityId & 0xFF);
            raw[0x17] = (byte)(supportAbilityId >> 8);
            raw[0x20] = (byte)(weaponId & 0xFF);
            raw[0x21] = (byte)(weaponId >> 8);
            raw[0x24] = (byte)(leftWeaponId & 0xFF);
            raw[0x25] = (byte)(leftWeaponId >> 8);
            raw[0x38] = 12;
            raw[0x4F] = (byte)x;
            raw[0x50] = (byte)y;
            int totalJpOffset = 0x11E + (89 - 0x4A) * 2;
            raw[totalJpOffset] = (byte)(totalJp & 0xFF);
            raw[totalJpOffset + 1] = (byte)(totalJp >> 8);
            return raw;
        }

        FormulaContext ContextFor(UnitSnapshot attacker, UnitSnapshot? defender = null)
        {
            var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
                settings, catalog, abilityCatalog, defender ?? target, attacker,
                eventIndex: 1, eventSeed: 1, actionType: 1, abilityId: 0, oldDebit: 0, oldCredit: 0);
            Check(FormulaRuntimeContextBuilder.TryApplyDerivedVariables(
                    context, settings.DclDerivedVariables, "DclDerivedVariables", out string error),
                $"Weapon Skill derived chain should evaluate: {error}");
            return context;
        }

        static int Eval(FormulaContext context, string formula)
        {
            Check(FormulaExpression.TryEvaluate(formula, context, out int value, out string error),
                $"Weapon Skill formula '{formula}' should evaluate: {error}");
            return value;
        }

        var masteredWithSupport = ContextFor(Ninja(NinjaRaw(11, 3000, supportAbilityId: 481), (nint)0x5100));
        Check(Eval(masteredWithSupport, "dcl.weaponFamily") == 2, "Ninja Blade should map to family 2");
        Check(Eval(masteredWithSupport, "dcl.weaponGrade") == 4, "Ninja/Ninja Blade should resolve authored grade A");
        Check(Eval(masteredWithSupport, "dcl.weaponSkillInvestmentMilli") == 42000,
            "job-level 8 / character-level 99 investment should be 42000 milli-skill");
        Check(Eval(masteredWithSupport, "dcl.weaponSkillRaw") == 57 &&
              Eval(masteredWithSupport, "dcl.weaponSkill") == 16 &&
              Eval(masteredWithSupport, "dcl.weaponSkillExcess") == 41,
            "grade-A maximum mastery plus Sword Master should produce raw 57, capped 16, excess 41");

        var mixedRanged = ContextFor(Ninja(
            NinjaRaw(77, 3000, leftWeaponId: 71), (nint)0x5108));
        Check(Eval(mixedRanged, "dcl.rightWeaponFamily") == 13 &&
              Eval(mixedRanged, "dcl.leftWeaponFamily") == 15,
            "mixed-family hands should independently resolve Crossbow and Gun");
        Check(Eval(mixedRanged, "dcl.rightWeaponGrade") == 2 &&
              Eval(mixedRanged, "dcl.leftWeaponGrade") == 3,
            "each hand should independently resolve its job x family grade");
        Check(Eval(mixedRanged, "dcl.rightDamageInput") == Eval(mixedRanged, "dcl.rightWeaponSkill") &&
              Eval(mixedRanged, "dcl.leftDamageInput") == Eval(mixedRanged, "dcl.leftWeaponSkill"),
            "Crossbow and Gun should both route capped Weapon Skill into the damage-input surface");
        Check(Eval(mixedRanged, "dcl.rightOvercapDamageUnits") == Eval(mixedRanged, "dcl.rightWeaponSkillExcess") &&
              Eval(mixedRanged, "dcl.rightOvercapPenetrationUnits") == 0,
            "Crossbow should expose over-cap skill only on the raw-damage route");
        Check(Eval(mixedRanged, "dcl.leftOvercapDamageUnits") == 0 &&
              Eval(mixedRanged, "dcl.leftOvercapPenetrationUnits") == Eval(mixedRanged, "dcl.leftWeaponSkillExcess"),
            "Gun should expose over-cap skill only on the penetration route");

        var bladeDamage = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x5109));
        Check(Eval(bladeDamage, "armor.classIndex") == 0 && Eval(bladeDamage, "dcl.weaponModel") == 30,
            "Leather Armor is native Armor/heavy in the current three-class DCL and should yield 30 fixture blade damage");
        Check(Eval(bladeDamage, settings.DclAttackSkillFormula) == 17,
            "Best Zodiac should add one only to the attacker hit target");

        var crossbowDamage = ContextFor(Ninja(NinjaRaw(77, 3000), (nint)0x510A));
        Check(Eval(crossbowDamage, "dcl.rightWeaponSkillRaw") == 30 &&
              Eval(crossbowDamage, "dcl.overcapRaw") == 3 &&
              Eval(crossbowDamage, "dcl.effectiveDr") == 6 &&
              Eval(crossbowDamage, "dcl.weaponModel") == 24,
            "Crossbow should use capped skill, convert overcap to raw damage, partially divide DR, and ignore Brave");

        var gunDamage = ContextFor(Ninja(NinjaRaw(71, 3000), (nint)0x510B));
        Check(Eval(gunDamage, "dcl.rightWeaponSkillRaw") == 41 &&
              Eval(gunDamage, "dcl.skillPenetration") == 6 &&
              Eval(gunDamage, "dcl.effectiveDr") == 0 &&
              Eval(gunDamage, "dcl.weaponModel") == 31,
            "Gun should use capped skill and convert overcap to penetration after its family DR divisor");

        var lowBraveBlade = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x510C, brave: 30));
        var lowBraveCrossbow = ContextFor(Ninja(NinjaRaw(77, 3000), (nint)0x510D, brave: 30));
        Check(Eval(lowBraveBlade, "dcl.weaponModel") == 24 &&
              Eval(lowBraveCrossbow, "dcl.weaponModel") == Eval(crossbowDamage, "dcl.weaponModel"),
            "Brave should scale ordinary physical weapons but remain neutral for skill-primary Crossbow/Gun");

        var badZodiacTarget = target with { Ptr = (nint)0x5203, Raw = TargetRaw(zodiac: 3) };
        var badZodiacBlade = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x510E), badZodiacTarget);
        Check(Eval(badZodiacBlade, "dcl.zodiacPermille") == 900 &&
              Eval(badZodiacBlade, "dcl.zodiacHitModifier") == 0 &&
              Eval(badZodiacBlade, "dcl.weaponModel") == 22,
            "square-sign Zodiac should reduce damage by ten percent without changing the hit target");

        Check(Eval(bladeDamage, "dcl.weightTotal") == 26 &&
              Eval(bladeDamage, "dcl.movePenalty") == 1 &&
              Eval(bladeDamage, settings.DclDodgeFormula) == 6,
            "heavy Armor weight should cross the first Move band and reduce the C-Ev Dodge fixture smoothly");
        var lightTarget = target with { Ptr = (nint)0x5204, Raw = TargetRaw(bodyId: 0xFF) };
        var lightDefense = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x510F), lightTarget);
        Check(Eval(lightDefense, "dcl.weightTotal") == 0 &&
              Eval(lightDefense, "dcl.movePenalty") == 0 &&
              Eval(lightDefense, settings.DclDodgeFormula) == 8,
            "an unweighted target should remain in the Move dead-zone and preserve baseline Dodge");
        var bunkerTarget = target with { Ptr = (nint)0x5205, Raw = TargetRaw(rightShieldId: 142) };
        var bunkerDefense = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x5111), bunkerTarget);
        Check(Eval(bunkerDefense, "dcl.weightTotal") == 31 &&
              Eval(bunkerDefense, "dcl.movePenalty") == 2 &&
              Eval(bunkerDefense, settings.DclDodgeFormula) == 5,
            "heavy Armor plus a heavy shield should cross the bunker Move band and continue the Dodge gradient");
        Check(Eval(bladeDamage, settings.DclDefenseModifierFormula) == -1,
            "Brave 70 should apply a shared -1 active-defense modifier after choosing Dodge/Parry/Block");
        Check(Eval(bladeDamage, "dcl.facingFront") == 1 &&
              Eval(bladeDamage, settings.DclDefenseAllowedFormula) == 1,
            "an attacker north of a -Y-facing target should receive the full front defense");
        var sideAttack = ContextFor(Ninja(NinjaRaw(11, 3000, x: 6, y: 5), (nint)0x5112));
        Check(Eval(sideAttack, "dcl.facingSide") == 1 &&
              Eval(sideAttack, settings.DclDefenseAllowedFormula) == 1 &&
              Eval(sideAttack, settings.DclDefenseModifierFormula) == -3,
            "a side attack should keep the defense roll and combine flank -2 with Brave -1");
        var backAttack = ContextFor(Ninja(NinjaRaw(11, 3000, x: 5, y: 6), (nint)0x5113));
        Check(Eval(backAttack, "dcl.facingBack") == 1 &&
              Eval(backAttack, settings.DclDefenseAllowedFormula) == 0,
            "an attacker south of a -Y-facing target should suppress the defense roll entirely");
        var adjacentSpear = ContextFor(Ninja(NinjaRaw(100, 3000, x: 5, y: 4), (nint)0x5114));
        var spacedSpear = ContextFor(Ninja(NinjaRaw(100, 3000, x: 5, y: 3), (nint)0x5115));
        Check(Eval(adjacentSpear, "dcl.pointBlank") == 1 &&
              Eval(adjacentSpear, settings.DclAttackSkillFormula) == 12,
            "a reach-2 spear at distance 1 should pay the point-blank skill fixture");
        Check(Eval(spacedSpear, "dcl.pointBlank") == 0 &&
              Eval(spacedSpear, settings.DclAttackSkillFormula) == 14,
            "the same reach-2 spear at distance 2 should attack without the point-blank penalty");
        Check(Eval(bladeDamage, "dcl.targetWeaponSkill") == 10 &&
              Eval(bladeDamage, settings.DclParryUsesFormula) == 1 &&
              Eval(bladeDamage, settings.DclParryFormula) == 8,
            "level-50 defender F-grade Sword should derive Parry 8 from Weapon Skill 10");
        Check(Eval(bunkerDefense, settings.DclBlockUsesFormula) == 1 &&
              Eval(bunkerDefense, settings.DclBlockFormula) == 12,
            "Venetian Shield S-Ev 50 should derive Block 12 from the shield fixture curve");
        Check(Eval(crossbowDamage, settings.DclParryFormula) == 0,
            "Parry should be unavailable against missiles while shield Block remains eligible");

        var masteredWithoutSupport = ContextFor(Ninja(NinjaRaw(11, 3000), (nint)0x5110));
        Check(Eval(masteredWithoutSupport, "dcl.weaponSkillRaw") == 55 &&
              Eval(masteredWithoutSupport, "dcl.weaponSkillExcess") == 39,
            "removing Sword Master should remove exactly two raw skill");

        var innateSwordMaster = ContextFor(Ninja(NinjaRaw(11, 3000, innateAbilityId: 481), (nint)0x5120));
        Check(Eval(innateSwordMaster, "dcl.hasSwordMaster") == 1,
            "Sword Master should work from a job innate slot as well as the equipped Support slot");

        var offGrade = ContextFor(Ninja(NinjaRaw(19, 3000), (nint)0x5130));
        Check(Eval(offGrade, "dcl.weaponFamily") == 3 && Eval(offGrade, "dcl.weaponGrade") == 0 &&
              Eval(offGrade, "dcl.weaponSkillRaw") == 13,
            "an unmapped max-level family should fall back to grade F and remain below the skill cap");

        var levelOneDabbler = ContextFor(Ninja(NinjaRaw(11, 0), (nint)0x5140, level: 99));
        Check(Eval(levelOneDabbler, "dcl.weaponSkillInvestmentMilli") == 3062 &&
              Eval(levelOneDabbler, "dcl.weaponSkillRaw") == 16,
            "job-level 1 should gate the level-99 contribution to one eighth");

        var report = RuntimeSettingsValidator.Validate(settings, catalog);
        Check(report.Success,
            "Weapon Skill mechanism settings should pass validation: " +
            string.Join(" | ", report.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));
    }

    private static void TestDclMagicPipeline(ItemCatalog catalog, AbilityCatalog abilityCatalog)
    {
        var settings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclDamageFormula = "if(dcl.isMagicDamage, if(dcl.magicAbsorbed || dcl.targetNull, 0, dcl.magicAmount), if(dcl.isMagicHeal && t.status.undead, dcl.magicAmount, dcl.oldDebit))",
            DclHealingFormula = "if(dcl.isMagicDamage && dcl.magicAbsorbed, dcl.magicAmount, if(dcl.isMagicHeal, if(t.status.undead, 0, dcl.magicAmount), dcl.oldCredit))",
            DclPreviewAmountEnabled = true,
            DclPreviewDamageFormula = "if(dcl.isMagicDamage, if(dcl.magicAbsorbed || dcl.targetNull, 0, dcl.magicAmount), if(dcl.isMagicHeal && t.status.undead, dcl.magicAmount, 0))",
            DclPreviewHealingFormula = "if(dcl.isMagicDamage && dcl.magicAbsorbed, dcl.magicAmount, if(dcl.isMagicHeal, if(t.status.undead, 0, dcl.magicAmount), 0))",
            FormulaVariables = new Dictionary<string, int>
            {
                // Mechanism fixtures, not final balance decisions.
                ["dclFaithLowPermille"] = 700,
                ["dclFaithBandPermille"] = 600,
                ["dclAffinityWeakPermille"] = 1300,
                ["dclAffinityStrongPermille"] = 700,
                ["dclAffinityHalvePermille"] = 500,
                ["dclShellPermille"] = 500,
                ["dclMagicScalePermille"] = 580,
                ["dclMagicStackCapPermille"] = 2500,
            },
            DclDerivedVariables =
            [
                new FormulaDerivedVariable { Name = "dcl.isMagicBolt", Formula = "action.abilityId == 0 && (aslot.rightweapon.category_rod || aslot.rightweapon.category_staff)" },
                new FormulaDerivedVariable { Name = "dcl.isMagicDamage", Formula = "ability.formula == 8 || dcl.isMagicBolt" },
                new FormulaDerivedVariable { Name = "dcl.isMagicHeal", Formula = "ability.formula == 12" },
                new FormulaDerivedVariable { Name = "dcl.elementFire", Formula = "ability.element_fire || (dcl.isMagicBolt && aslot.rightweapon.element_fire)" },
                new FormulaDerivedVariable { Name = "dcl.elementIce", Formula = "ability.element_ice || (dcl.isMagicBolt && aslot.rightweapon.element_ice)" },
                new FormulaDerivedVariable { Name = "dcl.elementLightning", Formula = "ability.element_lightning || (dcl.isMagicBolt && aslot.rightweapon.element_lightning)" },
                new FormulaDerivedVariable { Name = "dcl.elementWind", Formula = "ability.element_wind || (dcl.isMagicBolt && aslot.rightweapon.element_wind)" },
                new FormulaDerivedVariable { Name = "dcl.elementEarth", Formula = "ability.element_earth || (dcl.isMagicBolt && aslot.rightweapon.element_earth)" },
                new FormulaDerivedVariable { Name = "dcl.elementWater", Formula = "ability.element_water || (dcl.isMagicBolt && aslot.rightweapon.element_water)" },
                new FormulaDerivedVariable { Name = "dcl.elementHoly", Formula = "ability.element_holy || (dcl.isMagicBolt && aslot.rightweapon.element_holy)" },
                new FormulaDerivedVariable { Name = "dcl.elementDark", Formula = "ability.element_dark || (dcl.isMagicBolt && aslot.rightweapon.element_dark)" },
                new FormulaDerivedVariable { Name = "dcl.isSpiritual", Formula = "dcl.isMagicDamage && (dcl.elementHoly || dcl.elementDark)" },
                new FormulaDerivedVariable { Name = "dcl.isElemental", Formula = "dcl.isMagicDamage && !dcl.isSpiritual && (dcl.elementFire || dcl.elementIce || dcl.elementLightning || dcl.elementWind || dcl.elementEarth || dcl.elementWater)" },
                new FormulaDerivedVariable { Name = "dcl.targetAbsorb", Formula = "dcl.isElemental && ((dcl.elementFire && t.element.absorb.fire) || (dcl.elementIce && t.element.absorb.ice) || (dcl.elementLightning && t.element.absorb.lightning) || (dcl.elementWind && t.element.absorb.wind) || (dcl.elementEarth && t.element.absorb.earth) || (dcl.elementWater && t.element.absorb.water))" },
                new FormulaDerivedVariable { Name = "dcl.targetNull", Formula = "dcl.isElemental && ((dcl.elementFire && t.element.null.fire) || (dcl.elementIce && t.element.null.ice) || (dcl.elementLightning && t.element.null.lightning) || (dcl.elementWind && t.element.null.wind) || (dcl.elementEarth && t.element.null.earth) || (dcl.elementWater && t.element.null.water))" },
                new FormulaDerivedVariable { Name = "dcl.targetHalve", Formula = "dcl.isElemental && ((dcl.elementFire && t.element.halve.fire) || (dcl.elementIce && t.element.halve.ice) || (dcl.elementLightning && t.element.halve.lightning) || (dcl.elementWind && t.element.halve.wind) || (dcl.elementEarth && t.element.halve.earth) || (dcl.elementWater && t.element.halve.water))" },
                new FormulaDerivedVariable { Name = "dcl.targetWeak", Formula = "dcl.isElemental && ((dcl.elementFire && (t.element.weak.fire || t.status.oil)) || (dcl.elementIce && t.element.weak.ice) || (dcl.elementLightning && t.element.weak.lightning) || (dcl.elementWind && t.element.weak.wind) || (dcl.elementEarth && t.element.weak.earth) || (dcl.elementWater && t.element.weak.water))" },
                new FormulaDerivedVariable { Name = "dcl.targetStrong", Formula = "dcl.isElemental && ((dcl.elementFire && t.element.strengthen.fire) || (dcl.elementIce && t.element.strengthen.ice) || (dcl.elementLightning && t.element.strengthen.lightning) || (dcl.elementWind && t.element.strengthen.wind) || (dcl.elementEarth && t.element.strengthen.earth) || (dcl.elementWater && t.element.strengthen.water))" },
                new FormulaDerivedVariable { Name = "dcl.elementPermille", Formula = "if(dcl.targetNull, 0, if(dcl.targetHalve, const.dclAffinityHalvePermille, if(dcl.targetWeak, const.dclAffinityWeakPermille, if(dcl.targetStrong, const.dclAffinityStrongPermille, 1000))))" },
                new FormulaDerivedVariable { Name = "dcl.shellPermille", Formula = "if(dcl.isElemental && t.status.shell, const.dclShellPermille, 1000)" },
                new FormulaDerivedVariable { Name = "dcl.casterFaithPermille", Formula = "const.dclFaithLowPermille + mulDiv(a.maxFaith, const.dclFaithBandPermille, 100)" },
                new FormulaDerivedVariable { Name = "dcl.targetFaithPermille", Formula = "const.dclFaithLowPermille + mulDiv(t.maxFaith, const.dclFaithBandPermille, 100)" },
                new FormulaDerivedVariable { Name = "dcl.zodiacValid", Formula = "a.zodiac < 12 && t.zodiac < 12" },
                new FormulaDerivedVariable { Name = "dcl.zodiacDelta", Formula = "if(dcl.zodiacValid, abs(a.zodiac - t.zodiac), 0)" },
                new FormulaDerivedVariable { Name = "dcl.zodiacDistance", Formula = "if(dcl.zodiacValid, min(dcl.zodiacDelta, 12 - dcl.zodiacDelta), -1)" },
                new FormulaDerivedVariable { Name = "dcl.zodiacPermille", Formula = "if(dcl.zodiacDistance == 6, 1200, if(dcl.zodiacDistance == 3, 900, if(dcl.zodiacDistance == 0 || dcl.zodiacDistance == 4, 1100, 1000)))" },
                new FormulaDerivedVariable { Name = "dcl.faithCombinedPermille", Formula = "mulDiv(dcl.casterFaithPermille, dcl.targetFaithPermille, 1000)" },
                new FormulaDerivedVariable { Name = "dcl.magicStackPermille", Formula = "min(const.dclMagicStackCapPermille, mulDiv(mulDiv(mulDiv(dcl.faithCombinedPermille, if(dcl.isElemental, dcl.elementPermille, 1000), 1000), dcl.shellPermille, 1000), dcl.zodiacPermille, 1000))" },
                new FormulaDerivedVariable { Name = "dcl.spellPower", Formula = "if(dcl.isMagicBolt, aslot.rightweapon.weaponPower, ability.y)" },
                new FormulaDerivedVariable { Name = "dcl.magicCore", Formula = "a.rawMa * dcl.spellPower" },
                new FormulaDerivedVariable { Name = "dcl.magicAmount", Formula = "if(dcl.targetNull, 0, max(1, mulDiv(mulDiv(dcl.magicCore, dcl.magicStackPermille, 1000), const.dclMagicScalePermille, 1000)))" },
                new FormulaDerivedVariable { Name = "dcl.magicAbsorbed", Formula = "dcl.isMagicDamage && dcl.targetAbsorb" },
            ],
            AttackerEquipmentSlots =
            [
                new EquipmentSlotProbe { Name = "RightWeapon", Offset = 0x20, Width = "UInt16" },
            ],
        };

        static byte[] MagicRaw(int zodiac, int maxFaith, int rawMa, bool shell = false, bool undead = false,
            bool absorbFire = false, bool nullFire = false, bool halveFire = false,
            bool weakFire = false, bool strongFire = false, bool oil = false, int rightWeaponId = 0xFF)
        {
            var raw = new byte[0x200];
            raw[0x09] = (byte)(zodiac << 4);
            raw[0x2C] = (byte)maxFaith;
            raw[0x39] = (byte)rawMa;
            raw[0x20] = (byte)(rightWeaponId & 0xFF);
            raw[0x21] = (byte)(rightWeaponId >> 8);
            if (shell) raw[0x64] |= 0x10;
            if (undead) raw[0x61] |= 0x10;
            if (oil) raw[0x63] |= 0x80;
            if (absorbFire) raw[0x52] |= 0x80;
            if (nullFire) raw[0x53] |= 0x80;
            if (halveFire) raw[0x54] |= 0x80;
            if (weakFire) raw[0x55] |= 0x80;
            if (strongFire) raw[0x56] |= 0x80;
            return raw;
        }

        static UnitSnapshot Mage(byte[] raw, nint ptr, int faith = 60)
            => new(ptr, 0x90, 50, 220, 220, 1, false, 4, 12, 8, 4, 3, 50, faith, raw, 120, 120);
        static UnitSnapshot MagicTarget(byte[] raw, nint ptr, int faith = 60)
            => new(ptr, 0x91, 50, 300, 300, 2, true, 8, 6, 8, 4, 3, 50, faith, raw, 80, 80);

        FormulaContext ContextFor(int abilityId, UnitSnapshot target, int rightWeaponId = 0xFF)
        {
            var attacker = Mage(MagicRaw(zodiac: 0, maxFaith: 60, rawMa: 12, rightWeaponId: rightWeaponId), (nint)0x5300);
            var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
                settings, catalog, abilityCatalog, target, attacker,
                eventIndex: 1, eventSeed: 1, actionType: 1, abilityId: abilityId,
                oldDebit: 77, oldCredit: 55);
            Check(FormulaRuntimeContextBuilder.TryApplyDerivedVariables(
                    context, settings.DclDerivedVariables, "DclDerivedVariables", out string error),
                $"magic derived chain should evaluate: {error}");
            return context;
        }

        static int Eval(FormulaContext context, string formula)
        {
            Check(FormulaExpression.TryEvaluate(formula, context, out int value, out string error),
                $"magic formula '{formula}' should evaluate: {error}");
            return value;
        }

        var neutral = MagicTarget(MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0), (nint)0x5400);
        var fire = ContextFor(16, neutral);
        Check(Eval(fire, "dcl.isMagicDamage") == 1 && Eval(fire, "dcl.isElemental") == 1 &&
              Eval(fire, "dcl.magicAmount") == 131 && Eval(fire, settings.DclDamageFormula) == 131,
            "Fire should use raw MA, spell Y, two centered Faith terms, Best Zodiac, and the 0.58 scale");

        var flameRodBolt = ContextFor(0, neutral, rightWeaponId: 53);
        Check(Eval(flameRodBolt, "dcl.isMagicBolt") == 1 &&
              Eval(flameRodBolt, "dcl.elementFire") == 1 &&
              Eval(flameRodBolt, "dcl.spellPower") == 3 &&
              Eval(flameRodBolt, settings.DclDamageFormula) == 27,
            "Flame Rod basic Attack should become the zero-MP MA/Faith Fire bolt floor");

        var shellFire = ContextFor(16, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, shell: true), (nint)0x5401));
        Check(Eval(shellFire, "dcl.shellPermille") == 500 && Eval(shellFire, "dcl.magicAmount") == 65,
            "Shell should halve elemental magic only");

        var weakFire = ContextFor(16, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, weakFire: true), (nint)0x5402));
        Check(Eval(weakFire, "dcl.elementPermille") == 1300 && Eval(weakFire, "dcl.magicAmount") == 170,
            "elemental weakness should use the bounded everyday affinity band");

        var absorbedFire = ContextFor(16, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, absorbFire: true), (nint)0x5403));
        Check(Eval(absorbedFire, settings.DclDamageFormula) == 0 &&
              Eval(absorbedFire, settings.DclHealingFormula) == 131,
            "elemental absorb should reroute the deterministic amount from HP debit to HP credit");

        var nulledFire = ContextFor(16, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, nullFire: true), (nint)0x5404));
        Check(Eval(nulledFire, "dcl.magicAmount") == 0 && Eval(nulledFire, settings.DclDamageFormula) == 0,
            "designed-content nullification should produce zero rather than the magic minimum of one");

        var holyVsShellAndFireAffinity = ContextFor(15, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, shell: true, absorbFire: true, weakFire: true),
            (nint)0x5405));
        Check(Eval(holyVsShellAndFireAffinity, "dcl.isSpiritual") == 1 &&
              Eval(holyVsShellAndFireAffinity, "dcl.shellPermille") == 1000 &&
              Eval(holyVsShellAndFireAffinity, "dcl.magicAmount") == 468,
            "Holy/Dark should remain spiritual: Faith and Zodiac apply, elemental affinity and Shell do not");

        var cure = ContextFor(1, neutral);
        Check(Eval(cure, "dcl.isMagicHeal") == 1 &&
              Eval(cure, settings.DclDamageFormula) == 77 &&
              Eval(cure, settings.DclHealingFormula) == 131,
            "Cure should share MA/Faith/Zodiac but preserve damage and bypass affinity/Shell");
        var undeadCure = ContextFor(1, MagicTarget(
            MagicRaw(zodiac: 6, maxFaith: 60, rawMa: 0, undead: true), (nint)0x5406));
        int undeadDebit = Eval(undeadCure, settings.DclDamageFormula);
        int undeadCredit = Eval(undeadCure, settings.DclHealingFormula);
        Check(undeadDebit == 131 && undeadCredit == 0,
            $"Undead should invert healing into damage on the same numeric spine (debit={undeadDebit}, credit={undeadCredit}, flag={Eval(undeadCure, "t.status.undead")})");

        var report = RuntimeSettingsValidator.Validate(settings, catalog);
        Check(report.Success,
            "magic mechanism settings should pass validation: " +
            string.Join(" | ", report.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));
    }

    private static void TestDclIntegratedScaffold(string root, ItemCatalog catalog, AbilityCatalog abilityCatalog)
    {
        string path = Path.Combine(root, "work", "1784094553-battle-runtime-settings.dcl-integration-scaffold.json");
        Check(RuntimeSettings.TryLoad(path, out var settings, out string loadError),
            $"integrated scaffold should load: {loadError}");
        Check(settings.DclPipelineEnabled, "integrated scaffold should enable the DCL pipeline");
        Check(!settings.ResultSelectorProbeEnabled,
            "integrated scaffold should not inherit the physical profile's observe-only selector probe");
        Check(settings.DclResultFlagsControlEnabled && settings.DclResultFlagsPreserveMask == 0x0F,
            "integrated scaffold should rebuild numeric HP/MP result bits while preserving low effects");
        Check(settings.DclDerivedVariables.Count(variable =>
                variable.NormalizedName.Equals("dcl.zodiacPermille", StringComparison.OrdinalIgnoreCase)) == 1,
            "strict composition should deduplicate the shared Zodiac derived chain");

        static void PutU16(byte[] raw, int offset, int value)
        {
            raw[offset] = (byte)(value & 0xFF);
            raw[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        static byte[] UnitRaw(int zodiac, int maxFaith, int rawPa, int rawMa, int rightWeaponId,
            int bodyId = 0xFF, bool undead = false)
        {
            var raw = new byte[0x200];
            raw[0x03] = 89; // Ninja; the mechanism profile contains its family-grade fixtures.
            raw[0x09] = (byte)(zodiac << 4);
            raw[0x2A] = 70;
            raw[0x2C] = (byte)maxFaith;
            raw[0x38] = (byte)rawPa;
            raw[0x39] = (byte)rawMa;
            PutU16(raw, 0x20, rightWeaponId);
            PutU16(raw, 0x1C, bodyId);
            PutU16(raw, 0x13C, 3000); // Ninja total JP -> job level 8.
            if (undead) raw[0x61] |= 0x10;
            return raw;
        }

        static UnitSnapshot Actor(byte[] raw, nint ptr, bool foe)
            => new(ptr, foe ? 0x91 : 0x01, 50, 300, 300, foe ? 2 : 1, foe,
                12, 12, 8, 4, 3, 70, 60, raw, 100, 100);

        FormulaContext ContextFor(int abilityId, UnitSnapshot target, UnitSnapshot attacker)
        {
            var context = FormulaRuntimeContextBuilder.BuildDclDamageContext(
                settings, catalog, abilityCatalog, target, attacker,
                eventIndex: 9, eventSeed: 99, actionType: 1, abilityId: abilityId,
                oldDebit: 77, oldCredit: 55);
            Check(FormulaRuntimeContextBuilder.TryApplyDerivedVariables(
                    context, settings.DclDerivedVariables, "DclDerivedVariables", out string error),
                $"integrated scaffold derived chain should evaluate for ability {abilityId}: {error}");
            return context;
        }

        static int Eval(FormulaContext context, string formula)
        {
            Check(FormulaExpression.TryEvaluate(formula, context, out int value, out string error),
                $"integrated formula '{formula}' should evaluate: {error}");
            return value;
        }

        var physicalAttacker = Actor(UnitRaw(0, 60, 12, 12, rightWeaponId: 11), (nint)0x5500, foe: false);
        var heavyTarget = Actor(UnitRaw(6, 60, 8, 6, rightWeaponId: 0xFF, bodyId: 172), (nint)0x5600, foe: true);
        var attack = ContextFor(0, heavyTarget, physicalAttacker);
        int weaponDamage = Eval(attack, "dcl.weaponModel");
        Check(Eval(attack, "dcl.isMagicDamage") == 0 && weaponDamage > 0 &&
              Eval(attack, settings.DclDamageFormula) == weaponDamage,
            "integrated Attack should stay on the physical Weapon Skill/DR/wound route");

        var mage = Actor(UnitRaw(0, 60, 4, 12, rightWeaponId: 0xFF), (nint)0x5501, foe: false);
        var fire = ContextFor(16, heavyTarget, mage);
        int magicDamage = Eval(fire, "dcl.magicAmount");
        Check(Eval(fire, "dcl.isMagicDamage") == 1 && magicDamage > 0 &&
              Eval(fire, settings.DclDamageFormula) == magicDamage,
            "integrated Fire should use the magic/Faith/affinity route instead of physical armor");

        var undeadTarget = Actor(
            UnitRaw(6, 60, 8, 6, rightWeaponId: 0xFF, bodyId: 172, undead: true),
            (nint)0x5601, foe: true);
        var undeadCure = ContextFor(1, undeadTarget, mage);
        int cureAmount = Eval(undeadCure, "dcl.magicAmount");
        Check(Eval(undeadCure, settings.DclDamageFormula) == cureAmount &&
              Eval(undeadCure, settings.DclHealingFormula) == 0,
            "integrated Cure should preserve the Undead debit/credit inversion");

        var barrage = ContextFor(358, heavyTarget, physicalAttacker);
        Check(Eval(barrage, "dcl.isMagicDamage") == 0 && Eval(barrage, "dcl.isMagicHeal") == 0 &&
              Eval(barrage, settings.DclDamageFormula) == 77,
            "non-basic physical abilities should retain the native debit until their policy is authored");

        Check(settings.DclComputePointNumericEnabled && settings.DclInstantKoControlEnabled &&
              settings.DclInstantKoRules.Count == 1,
            "integrated scaffold should enable the AI-facing writer and exactly one proven Death rule");
        Check(settings.DclStatusForcedRoll == -1 && settings.DclHitForcedRoll == -1 &&
              !settings.CalcEntryProbeEnabled && !settings.DclCalcProvenanceProbeEnabled &&
              !settings.StagedBundleProbeEnabled,
            "integrated Death policy must not retain forced rolls or probe-only switches");
        var death = ContextFor(30, heavyTarget, mage);
        var deathRule = settings.DclInstantKoRules.Single();
        Check(deathRule.AbilityId == 30 && deathRule.ActionType == -1 &&
              deathRule.ZeroDamageOnFailure && deathRule.NativeKoSuppressedByData,
            "integrated Death rule should own the exact data-neutralized ability and zero resisted damage");
        Check(deathRule.TryMatches(1, 30, death, out bool deathMatches, out string deathMatchError) && deathMatches,
            $"integrated Death rule should match its action: {deathMatchError}");
        int deathResistance = Eval(death, deathRule.ResistanceFormula);
        var deathAssessment = new DclInstantKoAssessment(
            deathRule.DisplayName, true, false, deathResistance, deathRule.ZeroDamageOnFailure);
        Check(Eval(death, "dcl.isMagicDamage") == 0 && Eval(death, settings.DclDamageFormula) == 77 &&
              deathResistance == 12 && deathAssessment.SuccessPermille == 260 &&
              DclLifecycle.ComputeExpectedInstantKoDebit(300, 0, 0, deathAssessment) == 78,
            "integrated Death should preserve its neutralized carrier, use inverse Faith, and expose exact expected lethal utility");
        var deathResisted = new DclInstantKoDecision(
            deathRule.DisplayName, true, false, deathResistance, 3, true, deathRule.ZeroDamageOnFailure);
        var deathKills = new DclInstantKoDecision(
            deathRule.DisplayName, true, false, deathResistance, 18, false, deathRule.ZeroDamageOnFailure);
        Check(DclLifecycle.ComputeResolvedInstantKoDebit(300, 0, 77, deathResisted) == 0 &&
              DclLifecycle.ComputeResolvedInstantKoDebit(300, 25, 77, deathKills) == 325,
            "integrated Death should zero resistance and include same-hit HP credit in lethal delivery");

        var report = RuntimeSettingsValidator.Validate(settings, catalog);
        Check(report.Success,
            "integrated scaffold settings should pass validation: " +
            string.Join(" | ", report.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));
    }

    private static void TestDclResultFlags(ItemCatalog catalog)
    {
        Check(DclResultFlags.Compose(0x89, 0x0F, hpDebit: 40, hpCredit: 0, mpDebit: 10, mpCredit: 0) == 0xA9,
            "partial HP/MP debit should preserve low effect bits and derive combined flag 0xA0");
        Check(DclResultFlags.Compose(0xF8, 0x0F, hpDebit: 0, hpCredit: 0, mpDebit: 0, mpCredit: 0) == 0x08,
            "zeroed staged channels should clear every stale numeric result bit");
        Check(DclResultFlags.Compose(0, 0x0F, hpDebit: 1, hpCredit: 1, mpDebit: 1, mpCredit: 1) == 0xF0,
            "all four nonzero staged channels should produce the full bitwise numeric bundle");
        Check(DclResultFlags.Compose(0xFF, 0, hpDebit: 0, hpCredit: 0, mpDebit: 0, mpCredit: 0) == 0,
            "a zero preserve mask should produce no result flags when every staged channel is zero");

        Check(DclResultFlags.PrimaryPresentation(0xA0) == DclPrimaryResultPresentation.HpDamage,
            "the native selector should prioritize HP damage for a partial-shield 0xA0 result");
        Check(DclResultFlags.PrimaryPresentation(0x20) == DclPrimaryResultPresentation.MpDebit,
            "the native selector should route an MP-debit-only result through its special branch");
        Check(DclResultFlags.PrimaryPresentation(0x50) == DclPrimaryResultPresentation.Credit,
            "the native selector should share one credit route for combined HP+MP credit 0x50");
        Check(DclResultFlags.PrimaryPresentation(0x90) == DclPrimaryResultPresentation.HpDamage,
            "native HP-damage plus MP-credit 0x90 should keep HP damage as primary presentation");

        var valid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
            DclMpDebitFormula = "dcl.oldMpDebit",
            DclResultFlagsControlEnabled = true,
        };
        Check(RuntimeSettingsValidator.Validate(valid, catalog).Success,
            "result-flag ownership should validate with an explicit staged-channel formula");

        var invalid = new RuntimeSettings
        {
            DclResultFlagsControlEnabled = true,
            DclResultFlagsPreserveMask = 0x10,
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalid, catalog);
        Check(invalidReport.Findings.Any(finding => finding.Scope == "DclResultFlagsPreserveMask"),
            "validator should reject preserving numeric result bits");
        Check(invalidReport.Findings.Any(finding => finding.Scope == "DclResultFlagsControlEnabled" &&
              finding.Message.Contains("DclPipelineEnabled")),
            "validator should require the DCL pre-clamp pipeline for result-flag ownership");
    }

    private static void TestDclStatusControl(ItemCatalog catalog, AbilityCatalog abilityCatalog)
    {
        Check(DclStatusContest.ResistChancePermille(2) == 0, "3d6 resistance below 3 should never pass");
        Check(DclStatusContest.ResistChancePermille(3) == 4, "3d6 resistance 3 should pass only on triple ones");
        Check(DclStatusContest.ResistChancePermille(10) == 500, "3d6 resistance 10 should pass exactly half the outcomes");
        Check(DclStatusContest.ResistChancePermille(18) == 1000, "3d6 resistance 18 should always pass");
        Check(DclStatusContest.Resists(10, 10), "3d6 resistance passes when roll equals target number");
        Check(!DclStatusContest.Resists(11, 10), "3d6 resistance fails when roll exceeds target number");

        var allOrNothing = DclStatusGroups.Resolve(DclStatusGroups.AllOrNothing, 2, resistance: 10, roll: 9);
        Check(allOrNothing.Resisted && allOrNothing.SelectedIndex == -1,
            "an all-or-nothing group should expose one shared resisted contest and select every member");
        var randomOne = DclStatusGroups.Resolve(DclStatusGroups.RandomOne, 2, resistance: 8, roll: 12, selectedIndex: 1);
        Check(!randomOne.Resisted && randomOne.SelectedIndex == 1,
            "a random-one group should preserve one selected member and one shared failed-resistance result");
        bool rejectedRandomSelection = false;
        try
        {
            _ = DclStatusGroups.Resolve(DclStatusGroups.RandomOne, 2, resistance: 8, roll: 12, selectedIndex: 2);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejectedRandomSelection = true;
        }
        Check(rejectedRandomSelection, "random-one should reject a selected member outside the group");

        static DclStatusWrite StatusWrite(int byteIndex, byte mask, bool add, bool resisted = false, bool immune = false) =>
            new("packet-test", byteIndex, mask, add, 10, 9, resisted, immune, 0);

        var packetAdd = DclStatusPacket.Compose(
            new byte[] { 0x01, 0x04, 0, 0, 0 },
            new byte[] { 0, 0x20, 0, 0, 0 },
            0x80,
            [StatusWrite(1, 0x20, add: true)]);
        Check(packetAdd.Add.SequenceEqual(new byte[] { 0x01, 0x24, 0, 0, 0 }) &&
              packetAdd.Remove.SequenceEqual(new byte[] { 0, 0, 0, 0, 0 }),
            "a managed add should preserve unrelated packet bits and clear the same bit from remove");
        Check(packetAdd.ResultFlags == 0x88,
            "a nonempty status packet should set native result bit 0x08 without disturbing other flags");

        var packetRemove = DclStatusPacket.Compose(
            new byte[] { 0, 0x20, 0, 0, 0 },
            new byte[] { 0x02, 0, 0, 0, 0 },
            0,
            [StatusWrite(1, 0x20, add: false)]);
        Check(packetRemove.Add.SequenceEqual(new byte[5]) &&
              packetRemove.Remove.SequenceEqual(new byte[] { 0x02, 0x20, 0, 0, 0 }),
            "a managed remove should clear the same bit from add and preserve unrelated remove bits");

        var packetResisted = DclStatusPacket.Compose(
            new byte[] { 0, 0x20, 0, 0, 0 },
            new byte[] { 0, 0x20, 0, 0, 0 },
            0x08,
            [StatusWrite(1, 0x20, add: true, resisted: true)]);
        Check(packetResisted.Add.All(value => value == 0) && packetResisted.Remove.All(value => value == 0) &&
              packetResisted.ResultFlags == 0,
            "a resisted managed status should clear both owned packet bits and stale result bit 0x08");

        var packetImmune = DclStatusPacket.Compose(
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0x40, 0, 0 },
            0x48,
            [StatusWrite(2, 0x40, add: true, immune: true)]);
        Check(packetImmune.Remove.All(value => value == 0) && packetImmune.ResultFlags == 0x40,
            "an immune managed status should clear its owned packet bit while preserving unrelated result flags");

        var retainedCarrierPacket = DclStatusPacket.Compose(
            new byte[] { 0, 0, 0, 0x30, 0 },
            new byte[5],
            0x08,
            [
                StatusWrite(3, 0x20, add: true),
                StatusWrite(3, 0x10, add: true, resisted: true),
            ]);
        Check(retainedCarrierPacket.Add[3] == 0x20 && retainedCarrierPacket.Remove[3] == 0 &&
              retainedCarrierPacket.ResultFlags == 0x08,
            "retained Kiyomori carrier bits should be independently replaced by the managed contest");

        var failedCarrierPacket = DclStatusPacket.Compose(
            new byte[] { 0, 0, 0, 0x20, 0 },
            new byte[5],
            0x08,
            [new DclStatusWrite("failed-carrier", 3, 0x20, Add: true, Resistance: 0, Roll: -1,
                Resisted: false, Immune: false, DurationTargetTurns: 0, FailClosed: true)]);
        Check(failedCarrierPacket.Add.All(value => value == 0) &&
              failedCarrierPacket.Remove.All(value => value == 0) &&
              failedCarrierPacket.ResultFlags == 0,
            "a retained-carrier evaluation failure must remove the inherited native bit fail-closed");

        var notSelectedPacket = DclStatusPacket.Compose(
            new byte[] { 0, 0, 0, 0, 0x11 },
            new byte[5],
            0x08,
            [new DclStatusWrite("random-one loser", 4, 0x10, Add: true, Resistance: 10, Roll: 11,
                Resisted: false, Immune: false, DurationTargetTurns: 0, NotSelected: true)]);
        Check(notSelectedPacket.Add[4] == 0x01 && notSelectedPacket.Remove[4] == 0 &&
              notSelectedPacket.ResultFlags == 0x08,
            "a nonselected random-one member must clear only its inherited bit and preserve the selected packet bit");

        bool rejectedPacketWidth = false;
        try
        {
            _ = DclStatusPacket.Compose(new byte[4], new byte[5], 0, []);
        }
        catch (ArgumentException)
        {
            rejectedPacketWidth = true;
        }
        Check(rejectedPacketWidth, "status packet composition should reject an invalid native packet width");

        static DclStatusDurationState DurationState(int turns, bool active, bool skip) => new()
        {
            TargetPtr = (nint)1,
            TargetCharId = 1,
            StatusByteIndex = 1,
            StatusMask = 0x20,
            RuleName = "test",
            RemainingTargetTurns = turns,
            WasActive = active,
            SkipFirstFallingEdge = skip,
        };

        var oneTurn = DurationState(1, active: false, skip: false);
        Check(DclStatusDurationTracker.Advance(oneTurn, false) == DclStatusDurationTransition.None,
            "an inactive sample must not spend a target-turn duration");
        Check(DclStatusDurationTracker.Advance(oneTurn, true) == DclStatusDurationTransition.None,
            "the rising active edge must not spend a target-turn duration");
        Check(DclStatusDurationTracker.Advance(oneTurn, false) == DclStatusDurationTransition.Expired,
            "a one-turn status applied while inactive should expire on the next falling active edge");

        var twoTurns = DurationState(2, active: false, skip: false);
        DclStatusDurationTracker.Advance(twoTurns, true);
        Check(DclStatusDurationTracker.Advance(twoTurns, false) == DclStatusDurationTransition.CountedTargetTurn &&
              twoTurns.RemainingTargetTurns == 1,
            "a multi-turn status should decrement once per completed target turn");
        DclStatusDurationTracker.Advance(twoTurns, true);
        Check(DclStatusDurationTracker.Advance(twoTurns, false) == DclStatusDurationTransition.Expired,
            "a multi-turn status should expire on its final completed target turn");

        var appliedDuringTurn = DurationState(1, active: true, skip: true);
        Check(DclStatusDurationTracker.Advance(appliedDuringTurn, false) == DclStatusDurationTransition.SkippedApplicationTurn &&
              appliedDuringTurn.RemainingTargetTurns == 1,
            "a status applied during the target's active turn must not spend duration on that same turn");
        DclStatusDurationTracker.Advance(appliedDuringTurn, true);
        Check(DclStatusDurationTracker.Advance(appliedDuringTurn, false) == DclStatusDurationTransition.Expired,
            "a status applied during an active turn should expire after the target completes its next turn");

        var valid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusForcedRoll = 18,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Attack adds Blind",
                    AbilityId = 0,
                    ActionType = 1,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "absent",
                    ResistanceFormula = "clamp(target.brave / 10, 0, 18)",
                    DurationTargetTurns = 2,
                },
                new DclStatusRule
                {
                    Name = "Eye Drops removes Blind",
                    AbilityId = 1,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "remove",
                    NativeRiderPolicy = "absent",
                },
                new DclStatusRule
                {
                    Name = "Corruption adds Undead",
                    AbilityId = 2,
                    StatusByteIndex = 0,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "absent",
                    ResistanceFormula = "clamp(18 - target.maxFaith / 10, 3, 18)",
                    DurationTargetTurns = 2,
                },
            ],
        };
        var validReport = RuntimeSettingsValidator.Validate(valid, catalog);
        Check(validReport.Success, "valid per-ability DCL status rules should pass validation");

        var validRetainedCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Kiyomori Protect",
                    AbilityId = 81,
                    StatusByteIndex = 3,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "0",
                },
                new DclStatusRule
                {
                    Name = "Kiyomori Shell",
                    AbilityId = 81,
                    StatusByteIndex = 3,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "0",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validRetainedCarrier, catalog).Success,
            "a complete statically approved Kiyomori carrier should pass validation");

        IReadOnlyList<DclNativeStatusBit> wallBits = Array.Empty<DclNativeStatusBit>();
        string wallOperation = "";
        string wallMode = "";
        Check(abilityCatalog.TryGet(13, out var wallAbility) &&
              DclStatusConditionalProducer.TryDescribe(
                  wallAbility,
                  out wallBits,
                  out wallOperation,
                  out wallMode,
                  out _),
            "Wall should belong to the mapped formula-0x0B post-calc producer family");
        Check(wallBits.ToHashSet().SetEquals([new DclNativeStatusBit(3, 0x20), new DclNativeStatusBit(3, 0x10)]) &&
              wallOperation == "add" && wallMode == DclStatusGroups.AllOrNothing,
            "Wall should require complete Protect/Shell all-or-nothing packet ownership");

        var validPostCalcProducer = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Wall Protect",
                    AbilityId = 13,
                    StatusByteIndex = 3,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "replaced-post-calc",
                    ResistanceFormula = "10",
                    ContestGroup = "wall",
                    ContestMode = "all-or-nothing",
                },
                new DclStatusRule
                {
                    Name = "Wall Shell",
                    AbilityId = 13,
                    StatusByteIndex = 3,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "replaced-post-calc",
                    ResistanceFormula = "10",
                    ContestGroup = "wall",
                    ContestMode = "all-or-nothing",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validPostCalcProducer, catalog, abilityCatalog).Success,
            "a complete catalog-verified Wall post-calc producer should pass validation");
        Check(!RuntimeSettingsValidator.Validate(validPostCalcProducer, catalog).Success,
            "post-calc production must fail closed when the ability catalog is unavailable");
        validPostCalcProducer.DclStatusRules.RemoveAt(1);
        Check(RuntimeSettingsValidator.Validate(validPostCalcProducer, catalog, abilityCatalog).Findings.Any(f =>
              f.Scope == "DclStatusRules" && f.Severity == "ERROR" && f.Message.Contains("packet ownership mismatch")),
            "post-calc production must reject an incomplete inherited packet");

        IReadOnlyList<DclNativeStatusBit> songBits = Array.Empty<DclNativeStatusBit>();
        string songOperation = "";
        string songMode = "";
        Check(abilityCatalog.TryGet(91, out var namelessSong) &&
              DclStatusConditionalProducer.TryDescribe(
                  namelessSong,
                  out songBits,
                  out songOperation,
                  out songMode,
                  out _),
            "Nameless Song should reuse the mapped post-calc packet producer");
        Check(songBits.ToHashSet().SetEquals([
                  new DclNativeStatusBit(2, 0x20),
                  new DclNativeStatusBit(3, 0x40),
                  new DclNativeStatusBit(3, 0x20),
                  new DclNativeStatusBit(3, 0x10),
                  new DclNativeStatusBit(3, 0x08),
              ]) && songOperation == "add" && songMode == DclStatusGroups.RandomOne,
            "Nameless Song should require its complete five-status random-one packet");
        var validPerformanceProducer = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules = songBits.Select((bit, index) => new DclStatusRule
            {
                Name = $"Nameless Song {index}",
                AbilityId = 91,
                StatusByteIndex = bit.ByteIndex,
                StatusMask = bit.Mask,
                Operation = "add",
                NativeRiderPolicy = "replaced-post-calc",
                ResistanceFormula = "0",
                ContestGroup = "nameless-song",
                ContestMode = "random-one",
            }).ToList(),
        };
        Check(RuntimeSettingsValidator.Validate(validPerformanceProducer, catalog, abilityCatalog).Success,
            "a complete catalog-verified Nameless Song producer should pass validation");
        var awakeRaw = new byte[0x200];
        var sleepingRaw = new byte[0x200];
        sleepingRaw[0x65] = 0x10;
        var awakePerformer = new UnitSnapshot((nint)0x7100, 1, 1, 1, 1, 0, false,
            1, 1, 1, 1, 1, 50, 50, awakeRaw, 1, 1);
        var sleepingPerformer = new UnitSnapshot((nint)0x7200, 1, 1, 1, 1, 0, false,
            1, 1, 1, 1, 1, 50, 50, sleepingRaw, 1, 1);
        Check(!DclStatusConditionalProducer.IsBlockedByNativeEligibility(namelessSong, awakePerformer) &&
              DclStatusConditionalProducer.IsBlockedByNativeEligibility(namelessSong, sleepingPerformer),
            "performance production must preserve the native caster-Sleep eligibility gate");

        IReadOnlyList<DclNativeStatusBit> celestialBits = Array.Empty<DclNativeStatusBit>();
        string celestialOperation = "";
        string celestialMode = "";
        Check(abilityCatalog.TryGet(173, out var celestialVoid) &&
              celestialVoid.RandomFire != 0 &&
              DclStatusConditionalProducer.IsRandomFireFormula(celestialVoid.Formula) &&
              DclStatusConditionalProducer.TryDescribe(
                  celestialVoid,
                  out celestialBits,
                  out celestialOperation,
                  out celestialMode,
                  out _),
            "Celestial Void should use the mapped RandomFire post-calc producer");
        Check(celestialBits.ToHashSet().SetEquals([
                  new DclNativeStatusBit(1, 0x20),
                  new DclNativeStatusBit(1, 0x10),
                  new DclNativeStatusBit(1, 0x08),
                  new DclNativeStatusBit(2, 0x02),
                  new DclNativeStatusBit(3, 0x80),
                  new DclNativeStatusBit(3, 0x04),
                  new DclNativeStatusBit(4, 0x10),
              ]) && celestialOperation == "add" && celestialMode == DclStatusGroups.RandomOne,
            "Celestial Void should require its complete seven-status random-one packet");
        var validRandomFireProducer = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules = celestialBits.Select((bit, index) => new DclStatusRule
            {
                Name = $"Celestial Void {index}",
                AbilityId = 173,
                StatusByteIndex = bit.ByteIndex,
                StatusMask = bit.Mask,
                Operation = "add",
                NativeRiderPolicy = "replaced-post-calc",
                ResistanceFormula = "0",
                ContestGroup = "celestial-void",
                ContestMode = "random-one",
            }).ToList(),
        };
        Check(RuntimeSettingsValidator.Validate(validRandomFireProducer, catalog, abilityCatalog).Success,
            "a complete catalog-verified Celestial Void producer should pass validation");
        Check(abilityCatalog.TryGet(179, out var corporealVoid) &&
              DclStatusConditionalProducer.IsRandomFireFormula(corporealVoid.Formula) &&
              corporealVoid.RandomFire != 0,
            "Corporeal Void should share the validated formula-0x1F RandomFire producer family");

        var validFormula38Carrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Salve removes Darkness",
                    AbilityId = 149,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "remove",
                    NativeRiderPolicy = "retained-as-carrier",
                },
                new DclStatusRule
                {
                    Name = "Salve removes Silence",
                    AbilityId = 149,
                    StatusByteIndex = 1,
                    StatusMask = 0x08,
                    Operation = "remove",
                    NativeRiderPolicy = "retained-as-carrier",
                },
                new DclStatusRule
                {
                    Name = "Salve removes Poison",
                    AbilityId = 149,
                    StatusByteIndex = 3,
                    StatusMask = 0x80,
                    Operation = "remove",
                    NativeRiderPolicy = "retained-as-carrier",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validFormula38Carrier, catalog).Success,
            "a complete formula-0x38 Salve remove carrier should pass validation");
        validFormula38Carrier.DclStatusRules.RemoveAt(2);
        Check(RuntimeSettingsValidator.Validate(validFormula38Carrier, catalog).Findings.Any(f =>
              f.Scope == "DclStatusRules" && f.Severity == "ERROR" &&
              f.Message.Contains("every inherited rider bit")),
            "a formula-0x38 retained carrier should fail when one native remove bit is unowned");

        var validNightmareCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Nightmare Sleep",
                    AbilityId = 194,
                    StatusByteIndex = 4,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "10",
                    ContestGroup = "nightmare",
                    ContestMode = "random-one",
                },
                new DclStatusRule
                {
                    Name = "Nightmare Death Sentence",
                    AbilityId = 194,
                    StatusByteIndex = 4,
                    StatusMask = 0x01,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "10",
                    ContestGroup = "nightmare",
                    ContestMode = "random-one",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validNightmareCarrier, catalog).Success,
            "a complete Nightmare random-one carrier should pass validation");
        foreach (var rule in validNightmareCarrier.DclStatusRules)
        {
            rule.ContestMode = "independent";
            rule.ContestGroup = "";
        }
        Check(RuntimeSettingsValidator.Validate(validNightmareCarrier, catalog).Findings.Any(f =>
              f.Scope.StartsWith("DclStatusRules.") && f.Severity == "ERROR" &&
              f.Message.Contains("requires ContestMode='random-one'")),
            "Nightmare must reject independent per-bit rolls that change its one-of semantics");
        foreach (var rule in validNightmareCarrier.DclStatusRules)
        {
            rule.ContestMode = "random-one";
            rule.ContestGroup = "nightmare";
        }
        validNightmareCarrier.DclStatusRules.RemoveAt(1);
        var incompleteNightmare = RuntimeSettingsValidator.Validate(validNightmareCarrier, catalog);
        Check(incompleteNightmare.Findings.Any(f => f.Scope == "DclStatusRules" &&
              f.Severity == "ERROR" && f.Message.Contains("every inherited rider bit")) &&
              incompleteNightmare.Findings.Any(f => f.Scope.StartsWith("DclStatusRules.group.") &&
              f.Severity == "ERROR" && f.Message.Contains("at least two")),
            "Nightmare must reject an incomplete random-one group");

        var validPoisonousFrogCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Poisonous Frog Frog",
                    AbilityId = 346,
                    StatusByteIndex = 2,
                    StatusMask = 0x02,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "9",
                    ContestGroup = "poisonous-frog",
                    ContestMode = "all-or-nothing",
                },
                new DclStatusRule
                {
                    Name = "Poisonous Frog Poison",
                    AbilityId = 346,
                    StatusByteIndex = 3,
                    StatusMask = 0x80,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "9",
                    ContestGroup = "poisonous-frog",
                    ContestMode = "all-or-nothing",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validPoisonousFrogCarrier, catalog).Success,
            "a complete Poisonous Frog all-or-nothing carrier should pass validation");

        var validMuramasaCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Muramasa Confusion",
                    AbilityId = 82,
                    StatusByteIndex = 1,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "suppressed-by-data",
                    ResistanceFormula = "10",
                    ContestGroup = "muramasa",
                    ContestMode = "random-one",
                },
                new DclStatusRule
                {
                    Name = "Muramasa Death Sentence",
                    AbilityId = 82,
                    StatusByteIndex = 4,
                    StatusMask = 0x01,
                    Operation = "add",
                    NativeRiderPolicy = "suppressed-by-data",
                    ResistanceFormula = "10",
                    ContestGroup = "muramasa",
                    ContestMode = "random-one",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validMuramasaCarrier, catalog).Success,
            "a complete data-suppressed Muramasa random-one carrier should pass validation");
        validMuramasaCarrier.DclStatusRules.RemoveAt(1);
        Check(RuntimeSettingsValidator.Validate(validMuramasaCarrier, catalog).Findings.Any(f =>
              f.Scope == "DclStatusRules" && f.Severity == "ERROR" &&
              f.Message.Contains("every removed rider bit")),
            "Muramasa must reject a grouped data carrier that does not replace every removed native bit");

        var validSelfDestructCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "Self-Destruct victim Oil",
                    AbilityId = 277,
                    StatusByteIndex = 2,
                    StatusMask = 0x80,
                    Operation = "add",
                    NativeRiderPolicy = "suppressed-by-data",
                    ConditionFormula = "dcl.isSelf == 0",
                    ResistanceFormula = "10",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validSelfDestructCarrier, catalog).Success,
            "the exact victim-only Self-Destruct Oil carrier should pass validation");
        validSelfDestructCarrier.DclStatusRules[0].ConditionFormula = "";
        Check(RuntimeSettingsValidator.Validate(validSelfDestructCarrier, catalog).Findings.Any(f =>
              f.Scope.StartsWith("DclStatusRules.") && f.Severity == "ERROR" &&
              f.Message.Contains("dcl.isSelf == 0")),
            "Self-Destruct must reject a rider rule that could leak Oil onto the caster result");

        validRetainedCarrier.DclStatusRules.RemoveAt(1);
        var incompleteCarrierReport = RuntimeSettingsValidator.Validate(validRetainedCarrier, catalog);
        Check(incompleteCarrierReport.Findings.Any(f => f.Scope == "DclStatusRules" &&
              f.Severity == "ERROR" && f.Message.Contains("every inherited rider bit")),
            "a retained status-only carrier must own every inherited native rider bit");

        var unsupportedCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "unapproved retained carrier",
                    AbilityId = 13,
                    StatusByteIndex = 3,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "0",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(unsupportedCarrier, catalog).Findings.Any(f =>
              f.Scope.StartsWith("DclStatusRules.") && f.Severity == "ERROR" &&
              f.Message.Contains("not approved for this ability")),
            "retained-as-carrier should fail closed outside the statically mapped status-only allowlist");

        validRetainedCarrier = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "conditional carrier is unsafe",
                    AbilityId = 81,
                    StatusByteIndex = 3,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ConditionFormula = "target.hp > 0",
                    ResistanceFormula = "0",
                },
                new DclStatusRule
                {
                    Name = "Kiyomori Shell",
                    AbilityId = 81,
                    StatusByteIndex = 3,
                    StatusMask = 0x10,
                    Operation = "add",
                    NativeRiderPolicy = "retained-as-carrier",
                    ResistanceFormula = "0",
                },
            ],
        };
        Check(RuntimeSettingsValidator.Validate(validRetainedCarrier, catalog).Findings.Any(f =>
              f.Scope.StartsWith("DclStatusRules.") && f.Severity == "ERROR" &&
              f.Message.Contains("cannot use ConditionFormula")),
            "retained carrier bits should reject conditional ownership gaps");

        var duplicateOwner = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    Name = "owner-a",
                    AbilityId = 80,
                    ActionType = 1,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "add",
                    NativeRiderPolicy = "suppressed-by-data",
                    ResistanceFormula = "10",
                },
                new DclStatusRule
                {
                    Name = "owner-b",
                    AbilityId = 80,
                    ActionType = -1,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "remove",
                    NativeRiderPolicy = "suppressed-by-data",
                },
            ],
        };
        var duplicateOwnerReport = RuntimeSettingsValidator.Validate(duplicateOwner, catalog);
        Check(duplicateOwnerReport.Findings.Any(f => f.Scope.StartsWith("DclStatusRules.") &&
              f.Severity == "ERROR" && f.Message.Contains("one packet bit has one owner")),
            "validator should reject wildcard/exact overlap between two managed owners for the same native packet bit");

        var invalid = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclStatusControlEnabled = true,
            DclStatusForcedRoll = 2,
            DclStatusRules =
            [
                new DclStatusRule
                {
                    AbilityId = -1,
                    StatusByteIndex = 0,
                    StatusMask = 3,
                    Operation = "toggle",
                    DurationTargetTurns = 100,
                },
                new DclStatusRule
                {
                    AbilityId = 191,
                    StatusByteIndex = 1,
                    StatusMask = 0x20,
                    Operation = "remove",
                    NativeRiderPolicy = "invalid",
                    DurationTargetTurns = 1,
                },
            ],
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalid, catalog);
        Check(invalidReport.Findings.Any(f => f.Scope == "DclStatusForcedRoll" && f.Severity == "ERROR"),
            "validator should reject a forced status roll outside 3..18");
        Check(invalidReport.Findings.Count(f => f.Scope.StartsWith("DclStatusRules.") && f.Severity == "ERROR") >= 4,
            "validator should reject wildcard, protected byte-0 masks, multi-bit masks, unsupported operation, and formula-less add rules");
        Check(invalidReport.Findings.Count(f => f.Scope.StartsWith("DclStatusRules.") &&
              f.Severity == "ERROR" && f.Message.Contains("DurationTargetTurns")) >= 2,
            "validator should reject out-of-range durations and duration ownership on remove rules");

        Check(DclLifecycle.ComputeLethalDebit(120, 0) == 120 &&
              DclLifecycle.ComputeLethalDebit(120, 35) == 155,
            "instant KO debit must cancel current HP plus any staged same-hit credit");
        Check(DclLifecycle.WouldBeLethal(120, 35, 155) &&
              !DclLifecycle.WouldBeLethal(120, 35, 154),
            "instant KO lethality must use the native HP + credit - debit equation");
        var certainKo = new DclInstantKoAssessment("certain", true, false, 2, true);
        var halfKo = new DclInstantKoAssessment("half", true, false, 10, false);
        var resistedKo = new DclInstantKoAssessment("resisted", true, false, 18, false);
        var immuneKo = new DclInstantKoAssessment("immune", true, true, 0, false);
        Check(certainKo.SuccessPermille == 1000 && halfKo.SuccessPermille == 500 &&
              resistedKo.SuccessPermille == 0 && immuneKo.SuccessPermille == 0,
            "instant KO AI assessment should derive exact success probability from the 3d6 resistance curve");
        Check(DclLifecycle.ComputeExpectedInstantKoDebit(120, 30, 20, certainKo) == 150 &&
              DclLifecycle.ComputeExpectedInstantKoDebit(120, 30, 20, halfKo) == 85 &&
              DclLifecycle.ComputeExpectedInstantKoDebit(120, 30, 20, resistedKo) == 20 &&
              DclLifecycle.ComputeExpectedInstantKoDebit(120, 30, 20, immuneKo) == 20,
            "instant KO AI scoring should blend lethal and failure debit without sampling the execution roll");
        var killDecision = new DclInstantKoDecision("kill", true, false, 2, 18, false, true);
        var zeroFailureDecision = new DclInstantKoDecision("zero", true, false, 10, 10, true, true);
        var keepFailureDecision = new DclInstantKoDecision("keep", true, false, 10, 10, true, false);
        Check(DclLifecycle.ComputeResolvedInstantKoDebit(120, 30, 20, killDecision) == 150 &&
              DclLifecycle.ComputeResolvedInstantKoDebit(120, 30, 20, zeroFailureDecision) == 0 &&
              DclLifecycle.ComputeResolvedInstantKoDebit(120, 30, 20, keepFailureDecision) == 20,
            "instant KO execution should share one lethal/resisted debit policy across compute point and fallback delivery");

        var validInstantKo = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclComputePointNumericEnabled = true,
            DclHitControlEnabled = true,
            DclHitChanceFormula = "100",
            ItemTableEvadeZeroEnabled = true,
            EvadeCopierOverrideEnabled = true,
            EvadeCopierOverride46 = 0,
            EvadeCopierOverride47 = 0,
            EvadeCopierOverride48 = 0,
            EvadeCopierOverride49 = 0,
            EvadeCopierOverride4A = 0,
            EvadeCopierOverride4B = 0,
            EvadeCopierOverride4C = 0,
            EvadeCopierOverride4D = 0,
            EvadeCopierOverride4E = 0,
            DclMissOutputControlEnabled = true,
            DclMissSelectorOutcomeEnabled = true,
            DclInstantKoControlEnabled = true,
            DclInstantKoRules =
            [
                new DclInstantKoRule
                {
                    Name = "Death",
                    AbilityId = 30,
                    ResistanceFormula = "clamp(18 - target.maxFaith / 10, 3, 18)",
                    ZeroDamageOnFailure = true,
                    NativeKoSuppressedByData = true,
                },
            ],
        };
        var validInstantKoReport = RuntimeSettingsValidator.Validate(validInstantKo, catalog);
        Check(validInstantKoReport.Success,
            "a data-neutralized instant-KO rule should compose with the AI-facing compute-point writer: " +
            string.Join(" | ", validInstantKoReport.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));

        var invalidInstantKo = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclInstantKoControlEnabled = true,
            DclInstantKoRules =
            [
                new DclInstantKoRule { AbilityId = 30 },
                new DclInstantKoRule { AbilityId = 30, NativeKoSuppressedByData = true, ResistanceFormula = "10" },
            ],
        };
        var invalidInstantKoReport = RuntimeSettingsValidator.Validate(invalidInstantKo, catalog);
        Check(invalidInstantKoReport.Findings.Any(f => f.Scope == "DclInstantKoControlEnabled" && f.Severity == "ERROR"),
            "instant KO must reject configurations without authored connect/miss delivery");
        Check(invalidInstantKoReport.Findings.Any(f => f.Scope.StartsWith("DclInstantKoRules.") &&
              f.Severity == "ERROR" && f.Message.Contains("NativeKoSuppressedByData")),
            "instant KO must reject a rule whose native KO rider was not acknowledged as data-neutralized");
        Check(invalidInstantKoReport.Findings.Any(f => f.Scope.StartsWith("DclInstantKoRules.") &&
              f.Severity == "ERROR" && f.Message.Contains("duplicate")),
            "instant KO must reject duplicate ownership for the same ability/action pair");
    }

    private static void TestDclReactions(ItemCatalog catalog)
    {
        Check(DclReactions.DefaultChance("courage", 70, -1) == 70,
            "Courage should use Brave as its default reaction chance");
        Check(DclReactions.DefaultChance("caution", 70, -1) == 30,
            "Caution should use inverse Brave as its default reaction chance");
        Check(DclReactions.DefaultChance("neutral", 70, 55) == 55,
            "Neutral should use its authored flat reaction chance");
        Check(DclReactions.DefaultChance("neutral", 70, -1) == -1,
            "Neutral without an authored flat chance or formula must be invalid");

        var raw = new byte[0x200];
        var unit = new UnitSnapshot((nint)1, 1, 1, 100, 100, 1, false, 5, 5, 5, 5, 5, 70, 60, raw, 0, 0);
        var formulaRule = new DclReactionRule
        {
            AbilityId = 446,
            Mode = "caution",
            FlatChance = 5,
            ConditionFormula = "reaction.sourceValid && reaction.incomingActionValid && reaction.incomingDefended",
            ChanceFormula = "reaction.inverseBrave + reaction.flatChance + reaction.sourceValid + reaction.incomingAbilityId",
        };
        var reactionContext = new FormulaContext(unit, unit);
        DclReactions.AddIncomingVariables(reactionContext, new DclReactionIncomingContext(
            SourceValid: true,
            ActionValid: true,
            SourceIdx: 3,
            TargetIdx: 7,
            SourceCharId: 2,
            ActionType: 1,
            AbilityId: 4,
            HitDecisionKnown: true,
            Hit: false,
            PhysicalOutcome: (int)DclPhysicalOutcome.Defended,
            DefenseKind: (int)DclDefenseKind.Parry,
            SourceTurnEpoch: 9,
            TargetTurnEpoch: 5,
            Origin: "smoke"));
        Check(formulaRule.TryGetChance(70, reactionContext, out int formulaChance, out _) && formulaChance == 40,
            "authored reaction curves should evaluate against the runtime reaction formula context");
        Check(formulaRule.TryMatches(reactionContext, out bool reactionMatches, out _) && reactionMatches,
            "reaction trigger conditions should evaluate against incoming action/outcome context");
        reactionContext.Set("reaction.incomingDefended", 0);
        Check(formulaRule.TryMatches(reactionContext, out reactionMatches, out _) && !reactionMatches,
            "reaction trigger conditions should suppress ineligible incoming outcomes");
        reactionContext.Set("reaction.incomingDefended", 1);
        Check(reactionContext.GetVariable("reaction.sourceIdx") == 3 &&
              reactionContext.GetVariable("reaction.targetIdx") == 7 &&
              reactionContext.GetVariable("reaction.incomingActionType") == 1 &&
              reactionContext.GetVariable("reaction.incomingAbilityId") == 4 &&
              reactionContext.GetVariable("reaction.incomingHitKnown") == 1 &&
              reactionContext.GetVariable("reaction.incomingMiss") == 1 &&
              reactionContext.GetVariable("reaction.incomingDefended") == 1 &&
              reactionContext.GetVariable("reaction.incomingDefenseKind") == (int)DclDefenseKind.Parry &&
              reactionContext.GetVariable("reaction.sourceTurnEpoch") == 9 &&
              reactionContext.GetVariable("reaction.targetTurnEpoch") == 5 &&
              reactionContext.GetVariable("reaction.isSelfSource") == 0,
            "reaction formula context should expose the incoming source and action identity");

        var validSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclReactionTaxonomyEnabled = true,
            DclReactionRules =
            [
                new DclReactionRule { Name = "Counter", AbilityId = 442, Mode = "courage" },
                new DclReactionRule { Name = "Shirahadori", AbilityId = 451, Mode = "caution", VmInternalAvoidance = true },
                new DclReactionRule { Name = "Mana Shield", AbilityId = 445, Mode = "neutral", FlatChance = 50 },
                new DclReactionRule
                {
                    Name = "Authored curve",
                    AbilityId = 446,
                    Mode = "caution",
                    FlatChance = 5,
                    ConditionFormula = "reaction.sourceValid && reaction.incomingActionValid",
                    ChanceFormula = "clamp(reaction.inverseBrave + reaction.flatChance + reaction.sourceValid, 0, 100)",
                },
            ],
        };
        var validReport = RuntimeSettingsValidator.Validate(validSettings, catalog);
        Check(validReport.Success,
            "valid exact reaction taxonomy rules should pass validation: " +
            string.Join(" | ", validReport.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));
        Check(validReport.Findings.Any(f => f.Scope.Contains("Shirahadori") && f.Message.Contains("VmInternalAvoidance")),
            "VM-internal avoidance rules should retain an explicit live-validation warning");

        var invalidSettings = new RuntimeSettings
        {
            DclPipelineEnabled = false,
            DclReactionTaxonomyEnabled = true,
            BraveOverrideEnabled = true,
            ReactionChanceControlEnabled = true,
            DclReactionRules =
            [
                new DclReactionRule { Name = "Missing neutral chance", AbilityId = 421, Mode = "neutral" },
                new DclReactionRule { Name = "Duplicate invalid formula", AbilityId = 421, Mode = "surprise", ConditionFormula = "missingReactionCondition", ChanceFormula = "missingReactionValue" },
            ],
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalidSettings, catalog);
        Check(invalidReport.Findings.Any(f => f.Scope == "DclReactionTaxonomyEnabled" && f.Severity == "ERROR" && f.Message.Contains("DclPipelineEnabled")),
            "reaction taxonomy must require the shared DCL calc-entry pipeline");
        Check(invalidReport.Findings.Count(f => f.Scope == "DclReactionTaxonomyEnabled" && f.Severity == "ERROR" &&
              (f.Message.Contains("persistent Brave") || f.Message.Contains("global ReactionChanceControl"))) == 2,
            "reaction taxonomy must reject persistent Brave and global reaction-chance controls");
        Check(invalidReport.Findings.Any(f => f.Scope.StartsWith("DclReactionRules.") && f.Message.Contains("422..453")),
            "reaction taxonomy must reject non-Reaction ability ids");
        Check(invalidReport.Findings.Any(f => f.Scope.StartsWith("DclReactionRules.") && f.Message.Contains("duplicate")),
            "reaction taxonomy must reject duplicate exact Reaction ownership");
        Check(invalidReport.Findings.Any(f => f.Scope.StartsWith("DclReactionRules.") && f.Message.Contains("Mode must")),
            "reaction taxonomy must reject unsupported modes");
        Check(invalidReport.Findings.Any(f => f.Scope.StartsWith("DclReactionRules.") && f.Message.Contains("neutral reactions require")),
            "reaction taxonomy must require a neutral chance");
        Check(invalidReport.Findings.Any(f => f.Scope.EndsWith(".ChanceFormula") && f.Message.Contains("missingReactionValue")),
            "reaction taxonomy must validate authored chance formulas against the reaction context");
        Check(invalidReport.Findings.Any(f => f.Scope.EndsWith(".ConditionFormula") && f.Message.Contains("missingReactionCondition")),
            "reaction taxonomy must validate trigger conditions against the reaction context");

        var cadence = new DclReactionCadenceState();
        Check(!cadence.Observe(targetCharId: 7, activeNow: false) && cadence.OwnTurnEpoch == 0,
            "reaction cadence should initialize without inventing a turn edge");
        Check(cadence.TryConsumeOwnTurnCycle(441) && !cadence.TryConsumeOwnTurnCycle(441),
            "own-turn-cycle cadence should allow one reaction and reject a duplicate in the same cycle");
        cadence.Observe(targetCharId: 7, activeNow: true);
        Check(cadence.OwnTurnEpoch == 1 && cadence.TryConsumeOwnTurnCycle(441),
            "the defender's next active-turn rising edge should refresh own-cycle reactions");

        var firstAction = new DclReactionActionToken(3, 2, 9, 1, 0);
        Check(cadence.CanConsumeAttackerAction(442, firstAction) &&
              cadence.TryConsumeAttackerAction(442, firstAction) &&
              !cadence.CanConsumeAttackerAction(442, firstAction) &&
              !cadence.TryConsumeAttackerAction(442, firstAction),
            "attacker-action cadence should reject duplicate targets/strikes from one action token");
        Check(cadence.TryConsumeAttackerAction(442, firstAction with { SourceTurnEpoch = 10 }),
            "a new source turn epoch should admit the next attacker action");
        Check(!cadence.Observe(targetCharId: 8, activeNow: true) && cadence.OwnTurnEpoch == 0 &&
              cadence.TryConsumeOwnTurnCycle(441),
            "unit-slot reuse with a different character should reset reaction cadence state");

        var hexWard = new DclHexWardCoordinator();
        nint defenderPtr = (nint)0x3000;
        var hexToken = new DclReactionActionToken(16, 0x44, 12, 1, 0);
        var acceptedHex = hexWard.Evaluate(
            defenderPtr, defenderTableIndex: 3, defenderCharId: 0x55, actionToken: hexToken,
            eligible: true, chance: 30, roll: 29);
        Check(acceptedHex.Accepted && acceptedHex.ShouldRequestProducer && !acceptedHex.Replayed,
            "Hex Ward should reserve exactly one producer request when the Caution roll succeeds");
        var replayedHex = hexWard.Evaluate(
            defenderPtr, defenderTableIndex: 3, defenderCharId: 0x55, actionToken: hexToken,
            eligible: true, chance: 30, roll: 0);
        Check(replayedHex.Accepted && !replayedHex.ShouldRequestProducer && replayedHex.Replayed,
            "duplicate Hex Ward gate callbacks for one attacker action must neither reroll nor restage");
        Check(!hexWard.TryCommit(defenderPtr, defenderCharId: 0x55, sourceTableIndex: 15, out _),
            "Hex Ward commit must reject a source that differs from the reserved incoming attacker");
        Check(hexWard.TryCommit(defenderPtr, defenderCharId: 0x55, sourceTableIndex: 16, out var committedHex) &&
              committedHex.Phase == DclHexWardReservationPhase.Committed,
            "the exact pass-2 actor/source pair should commit the accepted Hex Ward reservation");
        Check(!hexWard.TryCommit(defenderPtr, defenderCharId: 0x55, sourceTableIndex: 16, out _),
            "a duplicate pass-2 commit must not deliver Hex Ward twice");
        var committedReplay = hexWard.Evaluate(
            defenderPtr, defenderTableIndex: 3, defenderCharId: 0x55, actionToken: hexToken,
            eligible: true, chance: 100, roll: 0);
        Check(committedReplay.Replayed && !committedReplay.ShouldRequestProducer &&
              committedReplay.Reason == "already-committed",
            "a late callback for the committed attacker token must not reopen the transaction");
        var rejectedHex = hexWard.Evaluate(
            defenderPtr, defenderTableIndex: 3, defenderCharId: 0x55,
            hexToken with { SourceTurnEpoch = 13 }, eligible: true, chance: 30, roll: 30);
        Check(!rejectedHex.Accepted && !rejectedHex.ShouldRequestProducer && rejectedHex.Reason == "chance-failed",
            "Hex Ward should reject the inclusive chance boundary roll and retain that decision idempotently");
    }

    private static void TestDclPhysicalContest(ItemCatalog catalog)
    {
        var dodge = DclPhysicalContest.ChooseBestDefense(
            dodge: 8, parry: 0, parryAvailable: false, block: 0, blockAvailable: false,
            modifier: 0, defenseAllowed: true);
        Check(dodge.Kind == DclDefenseKind.Dodge && dodge.Target == 8 && !dodge.Depletes,
            "Dodge should be the non-depleting defense floor");

        var guarded = DclPhysicalContest.ChooseBestDefense(
            dodge: 8, parry: 11, parryAvailable: true, block: 11, blockAvailable: true,
            modifier: -2, defenseAllowed: true);
        Check(guarded.Kind == DclDefenseKind.Block && guarded.Target == 9 && guarded.Depletes,
            "equal finite defenses should deterministically prefer Block and include the facing modifier");

        var back = DclPhysicalContest.ChooseBestDefense(
            dodge: 8, parry: 12, parryAvailable: true, block: 13, blockAvailable: true,
            modifier: 0, defenseAllowed: false);
        Check(back.Kind == DclDefenseKind.None,
            "a back strike should be representable as no defense roll and no depletion");

        Check(DclPhysicalContest.Resolve(12, 13, dodge, 18).Outcome == DclPhysicalOutcome.AttackMiss,
            "an ordinary attack roll above skill should miss before defense");
        Check(DclPhysicalContest.Resolve(12, 17, dodge, 18).Outcome == DclPhysicalOutcome.AttackFumble,
            "a skill-12 natural 17 should fumble");
        Check(DclPhysicalContest.Resolve(16, 6, guarded, 3).Outcome == DclPhysicalOutcome.CriticalHit,
            "a skill-16 natural 6 should crit and bypass even a successful defense roll");
        Check(DclPhysicalContest.Resolve(12, 10, guarded, 8).Outcome == DclPhysicalOutcome.Defended,
            "a connected ordinary attack should be turned aside when defense rolls at or below target");
        Check(DclPhysicalContest.Resolve(12, 10, guarded, 12).Outcome == DclPhysicalOutcome.Hit,
            "a connected ordinary attack should land when the defense roll exceeds its target");

        int openPct = DclPhysicalContest.HitChancePercent(12, back);
        int dodgePct = DclPhysicalContest.HitChancePercent(12, dodge);
        int guardedPct = DclPhysicalContest.HitChancePercent(12, guarded);
        Check(openPct > dodgePct && dodgePct > guardedPct,
            "exact forecast probability should fall monotonically from back strike to Dodge to stronger guard");
        Check(dodgePct is >= 55 and <= 67,
            "skill 12 against Dodge 8 should remain inside the DCL's attacker-favored baseline band");

        var pool = new DclGuardPool();
        pool.InitializeOrUpdate(targetCharId: 0x82, maxParryUses: 1, maxBlockUses: 2, activeNow: false);
        Check(pool.ParryUses == 1 && pool.BlockUses == 2, "new guard pools should start full");
        Check(pool.Spend(DclDefenseKind.Block) && pool.BlockUses == 1,
            "Block use should spend exactly one finite charge");
        Check(pool.Spend(DclDefenseKind.Parry) && pool.ParryUses == 0,
            "Parry use should spend exactly one finite charge");
        Check(!pool.ObserveActive(false), "inactive polling should not refresh guard");
        Check(pool.ObserveActive(true) && pool.ParryUses == 1 && pool.BlockUses == 2,
            "the defender's own active-turn rising edge should fully refresh finite guards");
        Check(!pool.ObserveActive(true), "repeated active samples must not refresh guard twice");
        pool.InitializeOrUpdate(targetCharId: 0x83, maxParryUses: 3, maxBlockUses: 4, activeNow: false);
        Check(pool.ParryUses == 3 && pool.BlockUses == 4,
            "character-id changes at a reused unit pointer should replace guard ownership");

        var validSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclPhysicalContestEnabled = true,
            DclPhysicalContestConditionFormula = "action.type == 1",
            DclHitChanceFormula = "100",
            DclPreviewHitPctEnabled = true,
            DclMissOutputControlEnabled = true,
            DclMissSelectorOutcomeEnabled = true,
            DclMissSuppressReactionsEnabled = true,
            ItemTableEvadeZeroEnabled = true,
            EvadeCopierOverrideEnabled = true,
            EvadeCopierOverride46 = 0,
            EvadeCopierOverride47 = 0,
            EvadeCopierOverride48 = 0,
            EvadeCopierOverride49 = 0,
            EvadeCopierOverride4A = 0,
            EvadeCopierOverride4B = 0,
            EvadeCopierOverride4C = 0,
            EvadeCopierOverride4D = 0,
            EvadeCopierOverride4E = 0,
            DclAttackSkillFormula = "a.pa + ability.y",
            DclDodgeFormula = "target.speed + guard.blockRemaining",
            DclParryFormula = "target.pa + guard.parryRemaining",
            DclBlockFormula = "target.pa + guard.blockMax",
            DclDefenseAllowedFormula = "target.facing != 2",
            DclDefenseModifierFormula = "if(target.facing == 1, -2, 0)",
            DclParryUsesFormula = "max(1, target.move / 4)",
            DclBlockUsesFormula = "2",
        };
        var validReport = RuntimeSettingsValidator.Validate(validSettings, catalog);
        Check(validReport.Success,
            "a complete physical contest profile should pass validation, including guard variables in formulas");

        var invalidSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHitControlEnabled = true,
            DclPhysicalContestEnabled = true,
            DclPhysicalContestConditionFormula = "action.type == 1",
            DclHitChanceFormula = "100",
            DclAttackForcedRoll = 2,
            DclDefenseForcedRoll = 19,
            DclAttackSkillFormula = "missingPhysicalValue + 1",
            DclDodgeFormula = "1",
            DclParryFormula = "1",
            DclBlockFormula = "1",
            DclDefenseAllowedFormula = "1",
            DclDefenseModifierFormula = "0",
            DclParryUsesFormula = "1",
            DclBlockUsesFormula = "1",
        };
        var invalidReport = RuntimeSettingsValidator.Validate(invalidSettings, catalog);
        Check(invalidReport.Findings.Any(f => f.Scope == "DclAttackForcedRoll" && f.Severity == "ERROR") &&
              invalidReport.Findings.Any(f => f.Scope == "DclDefenseForcedRoll" && f.Severity == "ERROR"),
            "validator should reject forced physical rolls outside 3..18");
        Check(invalidReport.Findings.Any(f => f.Scope == "DclAttackSkillFormula" && f.Message.Contains("missingPhysicalValue")),
            "validator should validate every physical contest formula against the DCL hit context");
        invalidSettings.DclAttackSkillFormula = "1";
        invalidSettings.DclParryUsesFormula = "guard.parryMax";
        var recursivePoolReport = RuntimeSettingsValidator.Validate(invalidSettings, catalog);
        Check(recursivePoolReport.Findings.Any(f => f.Scope == "DclParryUsesFormula" && f.Message.Contains("guard.parryMax")),
            "finite-guard capacity formulas must not recursively depend on guard state");
        Check(invalidReport.Findings.Any(f => f.Scope == "DclPhysicalContestEnabled" && f.Message.Contains("misses")),
            "validator should require authored miss delivery and reaction suppression for the physical contest");
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
            oldCredit: 4,
            oldMpDebit: 5,
            oldMpCredit: 6);

        Check(FormulaExpression.TryEvaluate("a.pa * 2 + ability.y", context, out int mixed, out string mixedError),
            $"DCL unit/ability formula should evaluate: {mixedError}");
        Check(mixed == 38, $"DCL unit/ability formula expected 38, got {mixed}");

        Check(FormulaExpression.TryEvaluate("dcl.oldDebit + dcl.oldCredit + action.type + action.abilityId + const.dclConst", context, out int dclVars, out string dclError),
            $"DCL plumbing formula should evaluate: {dclError}");
        Check(dclVars == 17, $"DCL plumbing formula expected 17, got {dclVars}");
        Check(FormulaExpression.TryEvaluate("dcl.oldMpDebit + dcl.oldMpCredit", context, out int mpChannels, out string mpChannelError),
            $"DCL staged MP channel inputs should evaluate: {mpChannelError}");
        Check(mpChannels == 11, $"DCL staged MP channel inputs expected 11, got {mpChannels}");

        Check(FormulaExpression.TryEvaluate("targetSlot.body.itemId + attackerSlot.weapon.itemId", context, out int slots, out string slotError),
            $"DCL slot formula should evaluate: {slotError}");
        Check(slots == 191, $"DCL slot formula expected 191, got {slots}");

        Check(FormulaExpression.TryEvaluate(
                "target.x + target.y + target.facing + target.mapLevel + target.status.blind + " +
                "target.status.master.blind + target.status.immune.sleep + target.element.absorb.fire + target.element.weak.holy",
                context, out int stateSurface, out string stateError),
            $"DCL geometry/status/element surface should evaluate: {stateError}");
        Check(stateSurface == 16, $"DCL geometry/status/element surface expected 16, got {stateSurface}");
        Check(FormulaExpression.TryEvaluate(
                "target.status.effectiveByte1 + target.status.immunityByte4 + target.element.absorbMask + target.element.weakMask",
                context, out int rawStateSurface, out string rawStateError),
            $"DCL raw status/element masks should evaluate: {rawStateError}");
        Check(rawStateSurface == 178, $"DCL raw status/element masks expected 178, got {rawStateSurface}");

        var mixedWeaponRaw = (byte[])attacker.Raw.Clone();
        mixedWeaponRaw[0x20] = 11; // Ninja Blade, right hand.
        mixedWeaponRaw[0x21] = 0;
        mixedWeaponRaw[0x24] = 71; // Romandan Pistol, left hand.
        mixedWeaponRaw[0x25] = 0;
        var mixedWeaponAttacker = attacker with { Raw = mixedWeaponRaw };
        var leftWeaponContext = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings, itemCatalog, abilityCatalog, target, mixedWeaponAttacker,
            eventIndex: 14, eventSeed: 347, actionType: 1, abilityId: 0,
            oldDebit: 0, oldCredit: 0, actionPayload: 71);
        Check(FormulaExpression.TryEvaluate(
                "action.payloadId + action.weaponKnown * 100 + action.weaponMatchesRight * 1000 + " +
                "action.weaponMatchesLeft * 10000 + action.weaponSideKnown * 100000 + " +
                "action.weaponSide * 1000000 + action.weapon.category_gun * 10000000 + " +
                "action.weapon.weaponFormula * 100000000",
                leftWeaponContext, out int leftWeaponSurface, out string leftWeaponError),
            $"DCL active-weapon payload surface should evaluate: {leftWeaponError}");
        Check(leftWeaponSurface == 312110171,
            $"DCL left active-weapon payload surface expected 312110171, got {leftWeaponSurface}");

        var identicalWeaponRaw = (byte[])mixedWeaponRaw.Clone();
        identicalWeaponRaw[0x24] = 11;
        identicalWeaponRaw[0x25] = 0;
        var identicalWeaponAttacker = attacker with { Raw = identicalWeaponRaw };
        var identicalWeaponContext = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings, itemCatalog, abilityCatalog, target, identicalWeaponAttacker,
            eventIndex: 15, eventSeed: 348, actionType: 1, abilityId: 0,
            oldDebit: 0, oldCredit: 0, actionPayload: 11);
        Check(FormulaExpression.TryEvaluate(
                "action.weaponMatchesRight + action.weaponMatchesLeft * 10 + " +
                "action.weaponSideKnown * 100 + action.weaponSide * 1000 + " +
                "action.weapon.category_ninjablade * 10000",
                identicalWeaponContext, out int identicalWeaponSurface, out string identicalWeaponError),
            $"DCL identical-weapon payload surface should evaluate: {identicalWeaponError}");
        Check(identicalWeaponSurface == 10011,
            $"DCL identical weapons should retain exact weapon identity while leaving side ambiguous; got {identicalWeaponSurface}");

        var progressionRaw = (byte[])target.Raw.Clone();
        progressionRaw[0x03] = 0x4A; // Squire, JP array index 0
        progressionRaw[0x0A] = 0xDD; // innate ability 477 (Dual Wield)
        progressionRaw[0x0B] = 0x01;
        progressionRaw[0x14] = 0xB7; // reaction ability 439 (Gil Snapper)
        progressionRaw[0x15] = 0x01;
        progressionRaw[0x16] = 0xC8; // support ability 456 (Equip Swords)
        progressionRaw[0x17] = 0x01;
        progressionRaw[0x18] = 0xE7; // movement ability 487 (Movement +2)
        progressionRaw[0x19] = 0x01;
        progressionRaw[0xF0] = 123;  // spendable JP
        progressionRaw[0x11E] = 0xBC; // total JP 700 => job level 4
        progressionRaw[0x11F] = 0x02;
        var progressionTarget = target with { Raw = progressionRaw };
        var progressionContext = FormulaRuntimeContextBuilder.BuildDclDamageContext(
            settings, itemCatalog, abilityCatalog, progressionTarget, attacker,
            eventIndex: 13, eventSeed: 346, actionType: 2, abilityId: 1, oldDebit: 0, oldCredit: 0);
        Check(FormulaExpression.TryEvaluate(
                "target.jobId + target.jobIndex + target.jobJp + target.jobTotalJp + target.jobLevel",
                progressionContext, out int progressionSurface, out string progressionError),
            $"DCL current-job progression surface should evaluate: {progressionError}");
        Check(progressionSurface == 901,
            $"DCL current-job progression surface expected 901, got {progressionSurface}");
        Check(FormulaExpression.TryEvaluate(
                "target.innateAbilityId1 + target.innateAbilityId2 + target.innateAbilityId3 + " +
                "target.innateAbilityId4 + target.reactionAbilityId + target.supportAbilityId + " +
                "target.movementAbilityId",
                progressionContext, out int abilitySlotSurface, out string abilitySlotError),
            $"DCL equipped/innate ability surface should evaluate: {abilitySlotError}");
        Check(abilitySlotSurface == 1859,
            $"DCL equipped/innate ability surface expected 1859, got {abilitySlotSurface}");

        Check(FormulaRuntimeContextBuilder.JobLevelFromTotalJp(0) == 1 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(199) == 1 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(200) == 2 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(699) == 3 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(700) == 4 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(2999) == 7 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(3000) == 8 &&
              FormulaRuntimeContextBuilder.JobLevelFromTotalJp(9999) == 8,
            "job-level derivation should honor every total-JP threshold and cap at level 8");

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

        var stagedChannelSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclHealingFormula = "dcl.oldCredit + a.ma",
            DclMpDebitFormula = "dcl.oldMpDebit + 1",
            DclMpCreditFormula = "dcl.oldMpCredit + 2",
        };
        var stagedChannelReport = RuntimeSettingsValidator.Validate(stagedChannelSettings, catalog);
        Check(stagedChannelReport.Success,
            "formula-owned HP-credit and MP debit/credit channels should pass validation without a damage formula");

        stagedChannelSettings.DclHealingFormula = "missingHealingInput + 1";
        stagedChannelSettings.DclMpDebitFormula = "missingMpDebitInput + 1";
        stagedChannelSettings.DclMpCreditFormula = "missingMpCreditInput + 1";
        var invalidStagedChannelReport = RuntimeSettingsValidator.Validate(stagedChannelSettings, catalog);
        Check(invalidStagedChannelReport.Findings.Any(f => f.Scope == "DclHealingFormula" && f.Message.Contains("missingHealingInput")) &&
              invalidStagedChannelReport.Findings.Any(f => f.Scope == "DclMpDebitFormula" && f.Message.Contains("missingMpDebitInput")) &&
              invalidStagedChannelReport.Findings.Any(f => f.Scope == "DclMpCreditFormula" && f.Message.Contains("missingMpCreditInput")),
            "validator should reject unknown inputs independently on every staged credit/MP formula");

        var previewAmountSettings = new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclPreviewAmountEnabled = true,
            DclPreviewDamageFormula = "a.pa + ability.y",
            DclPreviewHealingFormula = "if(ability.formula == 12, a.ma + ability.y, 0)",
        };
        var previewAmountReport = RuntimeSettingsValidator.Validate(previewAmountSettings, catalog);
        Check(previewAmountReport.Success,
            "formula-driven DCL forecast damage/healing should validate independently of hit control");

        previewAmountSettings.PreviewForecastPokeEnabled = true;
        previewAmountSettings.DclPreviewDamageFormula = "missingPreviewInput";
        var invalidPreviewAmountReport = RuntimeSettingsValidator.Validate(previewAmountSettings, catalog);
        Check(invalidPreviewAmountReport.Findings.Any(f => f.Scope == "DclPreviewAmountEnabled" && f.Message.Contains("conflicts")) &&
              invalidPreviewAmountReport.Findings.Any(f => f.Scope == "DclPreviewDamageFormula" && f.Message.Contains("missingPreviewInput")),
            "validator should reject static forecast-writer conflicts and invalid preview formulas");

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

        var previewWithoutHitControlReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPreviewHitPctEnabled = true,
        }, catalog);
        Check(
            previewWithoutHitControlReport.Findings.Any(finding => finding.Scope == "DclPreviewHitPctEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("DclHitControlEnabled")),
            "validator should require hit control under DCL forecast hit% parity");

        var previewStaticConflictReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPreviewHitPctEnabled = true,
            PreviewHitPctControlEnabled = true,
            PreviewHitPctForcedValue = 75,
            PreviewHitPctLogOnly = false,
        }, catalog);
        Check(
            previewStaticConflictReport.Findings.Any(finding => finding.Scope == "DclPreviewHitPctEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("static PreviewHitPctForcedValue")),
            "validator should reject dynamic DCL preview parity sharing the hook with a static forced percentage");

        var missOutputSettings = new RuntimeSettings
        {
            DclMissOutputControlEnabled = true,
            DclMissKindValue = 256,
        };
        var missOutputReport = RuntimeSettingsValidator.Validate(missOutputSettings, catalog);
        Check(
            missOutputReport.Findings.Any(finding => finding.Scope == "DclMissOutputControlEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("DclHitControlEnabled")),
            "validator should require hit control under miss output-control");
        Check(
            missOutputReport.Findings.Any(finding => finding.Scope == "DclMissKindValue" && finding.Severity == "ERROR"),
            "validator should reject DclMissKindValue outside 0..255");

        var selectorNoOutputReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissSelectorOutcomeEnabled = true,
        }, catalog);
        Check(
            selectorNoOutputReport.Findings.Any(finding => finding.Scope == "DclMissSelectorOutcomeEnabled" && finding.Severity == "ERROR"),
            "validator should require miss output-control under selector outcome-control");

        var reactionNoOutputReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissSuppressReactionsEnabled = true,
        }, catalog);
        Check(
            reactionNoOutputReport.Findings.Any(finding => finding.Scope == "DclMissSuppressReactionsEnabled" && finding.Severity == "ERROR"),
            "validator should require miss output-control under selective reaction suppression");

        var reactionHookConflictReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissOutputControlEnabled = true,
            DclMissSuppressReactionsEnabled = true,
            ReactionChanceControlEnabled = true,
        }, catalog);
        Check(
            reactionHookConflictReport.Findings.Any(finding => finding.Scope == "ReactionChanceControlEnabled" && finding.Severity == "ERROR"),
            "validator should reject global and DCL-selective reaction controls sharing the same hooks");

        // LT10-C miss presentation: rides the miss output-control path; glyph bit range-checked.
        var missPresNoOutputReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissPresentationEnabled = true,
            DclMissOutputControlEnabled = false,
        }, catalog);
        Check(
            missPresNoOutputReport.Findings.Any(finding => finding.Scope == "DclMissPresentationEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("DclMissOutputControlEnabled")),
            "validator should require miss output-control under miss presentation");

        var missPresBadGlyphReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissPresentationGlyphBit = 0x1F,
        }, catalog);
        Check(
            missPresBadGlyphReport.Findings.Any(finding => finding.Scope == "DclMissPresentationGlyphBit" && finding.Severity == "ERROR"),
            "validator should reject DclMissPresentationGlyphBit outside 0x10..0x18");

        var missPresBadKindReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMissPresentationKind = 300,
        }, catalog);
        Check(
            missPresBadKindReport.Findings.Any(finding => finding.Scope == "DclMissPresentationKind" && finding.Severity == "ERROR"),
            "validator should reject DclMissPresentationKind outside 0..255");

        // LT10-A counter-path probe: enabled needs a positive RVA + expected AOB bytes; max logs bounded.
        var counterPathBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclCounterPathProbeEnabled = true,
            DclCounterPathProbeRva = 0,
            DclCounterPathProbeExpectedBytes = "",
            DclCounterPathProbeMaxLogs = -1,
        }, catalog);
        Check(
            counterPathBadReport.Findings.Any(finding => finding.Scope == "DclCounterPathProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive DclCounterPathProbeRva when the probe is enabled");
        Check(
            counterPathBadReport.Findings.Any(finding => finding.Scope == "DclCounterPathProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the enabled counter-path probe");
        Check(
            counterPathBadReport.Findings.Any(finding => finding.Scope == "DclCounterPathProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative DclCounterPathProbeMaxLogs");

        // Reaction action-queue commit probe: observe-only, AOB-guarded, and bounded.
        var reactionCommitBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionCommitProbeEnabled = true,
            DclReactionCommitProbeRva = 0,
            DclReactionCommitProbeExpectedBytes = "",
            DclReactionCommitProbeMaxLogs = -1,
        }, catalog);
        Check(
            reactionCommitBadReport.Findings.Any(finding => finding.Scope == "DclReactionCommitProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive DclReactionCommitProbeRva when the probe is enabled");
        Check(
            reactionCommitBadReport.Findings.Any(finding => finding.Scope == "DclReactionCommitProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the enabled reaction-commit probe");
        Check(
            reactionCommitBadReport.Findings.Any(finding => finding.Scope == "DclReactionCommitProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative DclReactionCommitProbeMaxLogs");

        var reactionPreSelectorBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionPreSelectorProbeEnabled = true,
            DclReactionPreSelectorProbeRva = 0,
            DclReactionPreSelectorProbeExpectedBytes = "",
            DclReactionPreSelectorProbeMaxLogs = -1,
        }, catalog);
        Check(
            reactionPreSelectorBadReport.Findings.Any(finding => finding.Scope == "DclReactionPreSelectorProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive DclReactionPreSelectorProbeRva when enabled");
        Check(
            reactionPreSelectorBadReport.Findings.Any(finding => finding.Scope == "DclReactionPreSelectorProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the enabled reaction pre-selector probe");
        Check(
            reactionPreSelectorBadReport.Findings.Any(finding => finding.Scope == "DclReactionPreSelectorProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative DclReactionPreSelectorProbeMaxLogs");

        var reactionProducerBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionProducerEnabled = true,
            DclReactionProducerLogOnly = false,
            DclReactionProducerCarrierId = -1,
            DclReactionProducerUnitIndex = 21,
            DclReactionProducerMaxWrites = 0,
        }, catalog);
        Check(
            reactionProducerBadReport.Findings.Any(finding => finding.Scope == "DclReactionProducerEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("DclReactionPreSelectorProbeEnabled")),
            "validator should require the guarded pre-selector hook for reaction production");
        Check(
            reactionProducerBadReport.Findings.Any(finding => finding.Scope == "DclReactionProducerCarrierId" && finding.Severity == "ERROR"),
            "validator should reject a reaction producer carrier outside 422..453");
        Check(
            reactionProducerBadReport.Findings.Any(finding => finding.Scope == "DclReactionProducerUnitIndex" && finding.Severity == "ERROR"),
            "validator should reject a reaction producer unit index outside 0..20");
        Check(
            reactionProducerBadReport.Findings.Any(finding => finding.Scope == "DclReactionProducerMaxWrites" && finding.Severity == "ERROR"),
            "validator should reject an unbounded reaction producer write count");
        Check(
            reactionProducerBadReport.Findings.Any(finding => finding.Scope == "DclReactionProducerEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("DclReactionCommitProbeEnabled")),
            "validator should require commit capture for live reaction production");

        var reactionProducerLogOnlyReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionPreSelectorProbeEnabled = true,
            DclReactionProducerEnabled = true,
            DclReactionProducerLogOnly = true,
            DclReactionProducerCarrierId = 443,
            DclReactionProducerUnitIndex = 0,
            DclReactionProducerMaxWrites = 1,
        }, catalog);
        Check(
            !reactionProducerLogOnlyReport.Findings.Any(finding => finding.Severity == "ERROR"),
            "valid log-only reaction producer settings should pass validation without a commit probe");

        var reactionMaterializationBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionMaterializationProbeEnabled = true,
            DclReactionMaterializationProbeRva = 0,
            DclReactionMaterializationProbeExpectedBytes = "",
            DclReactionMaterializationProbeMaxLogs = -1,
        }, catalog);
        Check(
            reactionMaterializationBadReport.Findings.Any(finding => finding.Scope == "DclReactionMaterializationProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive reaction materialization probe RVA");
        Check(
            reactionMaterializationBadReport.Findings.Any(finding => finding.Scope == "DclReactionMaterializationProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the reaction materialization probe");
        Check(
            reactionMaterializationBadReport.Findings.Any(finding => finding.Scope == "DclReactionMaterializationProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative reaction materialization probe log cap");

        var reactionOrderRewriteBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionOrderRewriteEnabled = true,
            DclReactionOrderRewriteCarrierId = 454,
            DclReactionOrderRewriteActionEnabled = true,
            DclReactionOrderRewriteActionType = 256,
            DclReactionOrderRewriteAbilityId = 512,
            DclReactionOrderRewriteExpectedActionType = -2,
            DclReactionOrderRewriteExpectedAbilityId = 512,
            DclReactionOrderRewriteMaxWrites = 0,
        }, catalog);
        foreach (string scope in new[]
        {
            "DclReactionOrderRewriteEnabled",
            "DclReactionOrderRewriteCarrierId",
            "DclReactionOrderRewriteActionType",
            "DclReactionOrderRewriteAbilityId",
            "DclReactionOrderRewriteExpectedActionType",
            "DclReactionOrderRewriteExpectedAbilityId",
            "DclReactionOrderRewriteMaxWrites",
        })
        {
            Check(
                reactionOrderRewriteBadReport.Findings.Any(finding => finding.Scope == scope && finding.Severity == "ERROR"),
                $"validator should reject invalid accepted-order rewrite scope {scope}");
        }

        var reactionOrderRewriteLiveReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionMaterializationProbeEnabled = true,
            DclReactionOrderRewriteEnabled = true,
            DclReactionOrderRewriteLogOnly = false,
            DclReactionOrderRewriteCarrierId = 443,
            DclReactionOrderRewriteActionEnabled = true,
            DclReactionOrderRewriteActionType = 0x0B,
            DclReactionOrderRewriteAbilityId = 0,
            DclReactionOrderRewriteRetargetSource = true,
            DclReactionOrderRewriteExpectedActionType = 0,
            DclReactionOrderRewriteExpectedAbilityId = 443,
            DclReactionOrderRewriteMaxWrites = 1,
        }, catalog);
        Check(
            !reactionOrderRewriteLiveReport.Findings.Any(finding => finding.Severity == "ERROR"),
            "valid guarded live accepted-order rewrite should pass validation");

        var reactionOrderRewriteUnguardedLiveReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionMaterializationProbeEnabled = true,
            DclReactionOrderRewriteEnabled = true,
            DclReactionOrderRewriteLogOnly = false,
            DclReactionOrderRewriteCarrierId = 443,
            DclReactionOrderRewriteRetargetSource = true,
            DclReactionOrderRewriteMaxWrites = 1,
        }, catalog);
        Check(
            reactionOrderRewriteUnguardedLiveReport.Findings.Any(finding =>
                finding.Scope == "DclReactionOrderRewriteEnabled" && finding.Severity == "ERROR" && finding.Message.Contains("exact expected")),
            "validator should reject a live accepted-order rewrite without exact native-order guards");

        var hexWardLogOnlyReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclReactionTaxonomyEnabled = true,
            DclReactionRules = [new DclReactionRule { Name = "Hex Ward", AbilityId = 443, Mode = "caution" }],
            DclReactionPreSelectorProbeEnabled = true,
            DclReactionCommitProbeEnabled = true,
            DclReactionMaterializationProbeEnabled = true,
            DclReactionOrderRewriteEnabled = true,
            DclReactionOrderRewriteLogOnly = true,
            DclReactionOrderRewriteCarrierId = 443,
            DclReactionOrderRewriteRetargetSource = true,
            DclReactionOrderRewriteExpectedActionType = 1,
            DclReactionOrderRewriteExpectedAbilityId = 0,
            DclHexWardEnabled = true,
            DclHexWardLogOnly = true,
            DclHexWardEffect = "blind",
            DclHexWardMaxWrites = 1,
        }, catalog);
        Check(!hexWardLogOnlyReport.Findings.Any(finding => finding.Severity == "ERROR"),
            "the complete log-only Hex Ward producer/retarget/commit contract should pass validation: " +
            string.Join(" | ", hexWardLogOnlyReport.Findings.Select(f => $"{f.Severity}:{f.Scope}:{f.Message}")));

        var hexWardUnsafeReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclHexWardEnabled = true,
            DclHexWardLogOnly = false,
            DclHexWardEffect = "unknown",
            DclHexWardForcedRoll = 100,
            DclHexWardBlindDurationTargetTurns = -1,
            DclHexWardBraveDecrease = 0,
            DclHexWardBraveFloor = 101,
            DclHexWardMaxWrites = 0,
        }, catalog);
        foreach (string scope in new[]
        {
            "DclHexWardEnabled",
            "DclHexWardEffect",
            "DclHexWardForcedRoll",
            "DclHexWardBlindDurationTargetTurns",
            "DclHexWardBraveDecrease",
            "DclHexWardBraveFloor",
            "DclHexWardMaxWrites",
        })
        {
            Check(hexWardUnsafeReport.Findings.Any(finding => finding.Scope == scope && finding.Severity == "ERROR"),
                $"validator should reject unsafe Hex Ward scope {scope}");
        }

        var reactionEffectBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionEffectProbeEnabled = true,
            DclReactionEffectProbeRva = 0,
            DclReactionEffectProbeExpectedBytes = "",
            DclReactionEffectProbeMaxLogs = -1,
        }, catalog);
        Check(
            reactionEffectBadReport.Findings.Any(finding => finding.Scope == "DclReactionEffectProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive reaction effect probe RVA");
        Check(
            reactionEffectBadReport.Findings.Any(finding => finding.Scope == "DclReactionEffectProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the reaction effect probe");
        Check(
            reactionEffectBadReport.Findings.Any(finding => finding.Scope == "DclReactionEffectProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative reaction effect probe log cap");

        var autoPotionConsumeBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclAutoPotionConsumeProbeEnabled = true,
            DclAutoPotionConsumeProbeRva = 0,
            DclAutoPotionConsumeProbeExpectedBytes = "",
            DclAutoPotionConsumeProbeMaxLogs = -1,
        }, catalog);
        Check(
            autoPotionConsumeBadReport.Findings.Any(finding => finding.Scope == "DclAutoPotionConsumeProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive DclAutoPotionConsumeProbeRva when enabled");
        Check(
            autoPotionConsumeBadReport.Findings.Any(finding => finding.Scope == "DclAutoPotionConsumeProbeExpectedBytes" && finding.Severity == "ERROR"),
            "validator should require expected bytes for the enabled Auto-Potion consumption probe");
        Check(
            autoPotionConsumeBadReport.Findings.Any(finding => finding.Scope == "DclAutoPotionConsumeProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative DclAutoPotionConsumeProbeMaxLogs");

        var weaponLofBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclWeaponLineOfFireProbeEnabled = true,
            DclWeaponLineOfFireArcRva = 0,
            DclWeaponLineOfFireDirectRva = 0,
            DclWeaponLineOfFireExpectedBytes = "",
            DclWeaponLineOfFireProbeMaxLogs = -1,
        }, catalog);
        foreach (string scope in new[]
        {
            "DclWeaponLineOfFireArcRva",
            "DclWeaponLineOfFireDirectRva",
            "DclWeaponLineOfFireProbeEnabled",
            "DclWeaponLineOfFireExpectedBytes",
            "DclWeaponLineOfFireProbeMaxLogs",
        })
        {
            Check(
                weaponLofBadReport.Findings.Any(finding => finding.Scope == scope && finding.Severity == "ERROR"),
                $"validator should reject invalid weapon line-of-fire setting: {scope}");
        }

        var calcProvenanceBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclCalcProvenanceProbeEnabled = true,
            CalcEntryProbeRva = 0,
            DclCalcProvenanceProbeMaxLogs = -1,
        }, catalog);
        Check(
            calcProvenanceBadReport.Findings.Any(finding => finding.Scope == "CalcEntryProbeRva" && finding.Severity == "ERROR"),
            "validator should reject a non-positive CalcEntryProbeRva for calc provenance");
        Check(
            calcProvenanceBadReport.Findings.Any(finding => finding.Scope == "DclCalcProvenanceProbeMaxLogs" && finding.Severity == "ERROR"),
            "validator should reject a negative DclCalcProvenanceProbeMaxLogs");

        var reactionReplacementBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionActionReplacementEnabled = true,
            DclReactionCommitProbeEnabled = false,
            DclReactionActionReplacementCarrierId = 421,
            DclReactionActionReplacementAbilityId = 512,
            DclReactionActionReplacementMinTargetCount = 0,
            DclReactionActionReplacementMaxWrites = 0,
        }, catalog);
        foreach (string scope in new[]
        {
            "DclReactionActionReplacementEnabled",
            "DclReactionActionReplacementCarrierId",
            "DclReactionActionReplacementAbilityId",
            "DclReactionActionReplacementMinTargetCount",
            "DclReactionActionReplacementMaxWrites",
        })
        {
            Check(
                reactionReplacementBadReport.Findings.Any(finding => finding.Scope == scope && finding.Severity == "ERROR"),
                $"validator should reject invalid reaction replacement scope {scope}");
        }

        var reactionRetargetBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionRetargetEnabled = true,
            DclReactionCommitProbeEnabled = false,
            DclReactionRetargetCarrierId = 454,
            DclReactionRetargetMaxWrites = 0,
        }, catalog);
        foreach (string scope in new[]
        {
            "DclReactionRetargetEnabled",
            "DclReactionRetargetCarrierId",
            "DclReactionRetargetMaxWrites",
        })
        {
            Check(
                reactionRetargetBadReport.Findings.Any(finding => finding.Scope == scope && finding.Severity == "ERROR"),
                $"validator should reject invalid reaction retarget scope {scope}");
        }

        var reactionRetargetLogOnlyReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionCommitProbeEnabled = true,
            DclReactionRetargetEnabled = true,
            DclReactionRetargetLogOnly = true,
            DclReactionRetargetCarrierId = 443,
            DclReactionRetargetMaxWrites = 1,
        }, catalog);
        Check(
            !reactionRetargetLogOnlyReport.Findings.Any(finding => finding.Severity == "ERROR"),
            "valid log-only reaction retarget settings should pass validation");

        var retiredReactionCommitWrites = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclReactionCommitProbeEnabled = true,
            DclReactionActionReplacementEnabled = true,
            DclReactionActionReplacementLogOnly = false,
            DclReactionActionReplacementCarrierId = 442,
            DclReactionActionReplacementAbilityId = 0,
            DclReactionActionReplacementMinTargetCount = 1,
            DclReactionActionReplacementMaxWrites = 1,
            DclReactionRetargetEnabled = true,
            DclReactionRetargetLogOnly = false,
            DclReactionRetargetCarrierId = 443,
            DclReactionRetargetMaxWrites = 1,
        }, catalog);
        Check(
            retiredReactionCommitWrites.Findings.Any(finding =>
                finding.Scope == "DclReactionActionReplacementEnabled" &&
                finding.Severity == "ERROR" && finding.Message.Contains("retired")),
            "validator should retire live action replacement at the pre-materialization commit");
        Check(
            retiredReactionCommitWrites.Findings.Any(finding =>
                finding.Scope == "DclReactionRetargetEnabled" &&
                finding.Severity == "ERROR" && finding.Message.Contains("retired")),
            "validator should retire live retarget at the pre-materialization commit");

        // LT13: the legacy staged-status write surface is reclassified as item/inventory authority.
        // Suppress + force remain mutually exclusive, and every write mode is retired/blocked.
        var statusBothReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
            DclStatusSuppressEnabled = true,
            DclStatusForceId = 5,
        }, catalog);
        Check(
            statusBothReport.Findings.Any(finding => finding.Scope == "DclStatusOutputControl" && finding.Severity == "ERROR" && finding.Message.Contains("mutually exclusive")),
            "validator should reject suppress + force together");

        var statusNoPipelineReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPipelineEnabled = false,
            DclStatusSuppressEnabled = true,
        }, catalog);
        Check(
            statusNoPipelineReport.Findings.Any(finding => finding.Scope == "DclStatusOutputControl" && finding.Severity == "ERROR" && finding.Message.Contains("DclPipelineEnabled")),
            "validator should require the DCL pipeline for status output-control");

        var retiredStatusWriteReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclPipelineEnabled = true,
            DclDamageFormula = "dcl.oldDebit",
            DclStatusSuppressEnabled = true,
        }, catalog);
        Check(
            retiredStatusWriteReport.Findings.Any(finding =>
                finding.Scope == "DclStatusOutputControl" &&
                finding.Severity == "ERROR" &&
                finding.Message.Contains("item/inventory")),
            "validator should reject the retired staged-status write surface as item/inventory authority");

        var retiredBundleAuxReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            StagedBundleProbeEnabled = true,
            StagedBundleForceTargetCharId = 0x01,
            StagedBundleForceAilment = 0x2B,
            StagedBundleForceApplyMask = 0x08,
        }, catalog);
        Check(
            retiredBundleAuxReport.Findings.Any(finding =>
                finding.Scope == "StagedBundleForceAilment" &&
                finding.Severity == "ERROR" &&
                finding.Message.Contains("item/inventory")),
            "validator should reject legacy +0x1A8 forcing");
        Check(
            retiredBundleAuxReport.Findings.Any(finding =>
                finding.Scope == "StagedBundleForceApplyMask" &&
                finding.Severity == "ERROR" &&
                finding.Message.Contains("item/inventory")),
            "validator should reject legacy +0x1D0 forcing");

        // Status output-control force/suppress field ranges: id word, raw value/offset, masks.
        var statusForceRangeReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclStatusForceId = 0x10000,
            DclStatusForceValue = 256,
            DclStatusForceOffset = 0x400,
            DclStatusSuppressMask = 0x100,
            DclStatusResultFlagStatusBit = 0x100,
        }, catalog);
        Check(
            statusForceRangeReport.Findings.Any(finding => finding.Scope == "DclStatusForceId" && finding.Severity == "ERROR"),
            "validator should reject a DclStatusForceId outside -1..65535");
        Check(
            statusForceRangeReport.Findings.Any(finding => finding.Scope == "DclStatusForceValue" && finding.Severity == "ERROR"),
            "validator should reject a DclStatusForceValue outside -1..255");
        Check(
            statusForceRangeReport.Findings.Any(finding => finding.Scope == "DclStatusForceOffset" && finding.Severity == "ERROR"),
            "validator should reject a DclStatusForceOffset outside the unit struct");
        Check(
            statusForceRangeReport.Findings.Any(finding => finding.Scope == "DclStatusSuppressMask" && finding.Severity == "ERROR"),
            "validator should reject a DclStatusSuppressMask outside 0..255");
        Check(
            statusForceRangeReport.Findings.Any(finding => finding.Scope == "DclStatusResultFlagStatusBit" && finding.Severity == "ERROR"),
            "validator should reject a DclStatusResultFlagStatusBit outside 0..255");

        // Direct status poke: invalid offset / mode / missing mask.
        var statusPokeBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            StatusPokeTargetCharId = 0x01,
            StatusPokeMode = "toggle",
            StatusPokeOffset = 0x400,
            StatusPokeMask = -1,
            StatusPokeValue = -1,
        }, catalog);
        Check(
            statusPokeBadReport.Findings.Any(finding => finding.Scope == "StatusPokeMode" && finding.Severity == "ERROR"),
            "validator should reject an unsupported StatusPokeMode");
        Check(
            statusPokeBadReport.Findings.Any(finding => finding.Scope == "StatusPokeOffset" && finding.Severity == "ERROR"),
            "validator should reject a StatusPokeOffset outside the unit struct");
        Check(
            statusPokeBadReport.Findings.Any(finding => finding.Scope == "StatusPoke" && finding.Severity == "ERROR" && finding.Message.Contains("bit mask")),
            "validator should reject a status poke with no mask/value");

        var statusPokeOkReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            StatusPokeTargetCharId = 0x01,
            StatusPokeMode = "add",
            StatusPokeOffset = 0x1EF,
            StatusPokeMask = 0x20,
        }, catalog);
        Check(statusPokeOkReport.Success, "a well-formed status poke should validate");

        // Direct status poke: target/mask/value/max-writes ranges.
        var statusPokeRangeReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            StatusPokeTargetCharId = 0x100,
            StatusPokeMode = "add",
            StatusPokeOffset = 0x1EF,
            StatusPokeMask = 0x100,
            StatusPokeValue = 0x100,
            StatusPokeMaxWrites = 0,
        }, catalog);
        Check(
            statusPokeRangeReport.Findings.Any(finding => finding.Scope == "StatusPokeTargetCharId" && finding.Severity == "ERROR"),
            "validator should reject a StatusPokeTargetCharId outside -1..255");
        Check(
            statusPokeRangeReport.Findings.Any(finding => finding.Scope == "StatusPokeMask" && finding.Severity == "ERROR"),
            "validator should reject a StatusPokeMask outside -1..255");
        Check(
            statusPokeRangeReport.Findings.Any(finding => finding.Scope == "StatusPokeValue" && finding.Severity == "ERROR"),
            "validator should reject a StatusPokeValue outside -1..255");
        Check(
            statusPokeRangeReport.Findings.Any(finding => finding.Scope == "StatusPokeMaxWrites" && finding.Severity == "ERROR"),
            "validator should reject a StatusPokeMaxWrites outside 1..32");

        // Move-write poke: out-of-range value.
        var movePokeBadReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            MovePokeTargetCharId = 0x01,
            MovePokeValue = 99,
        }, catalog);
        Check(
            movePokeBadReport.Findings.Any(finding => finding.Scope == "MovePokeValue" && finding.Severity == "ERROR"),
            "validator should reject a MovePokeValue above 32");

        // Move-write poke: target/max-writes ranges.
        var movePokeRangeReport = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            MovePokeTargetCharId = 0x100,
            MovePokeValue = 5,
            MovePokeMaxWrites = 0,
        }, catalog);
        Check(
            movePokeRangeReport.Findings.Any(finding => finding.Scope == "MovePokeTargetCharId" && finding.Severity == "ERROR"),
            "validator should reject a MovePokeTargetCharId outside -1..255");
        Check(
            movePokeRangeReport.Findings.Any(finding => finding.Scope == "MovePokeMaxWrites" && finding.Severity == "ERROR"),
            "validator should reject a MovePokeMaxWrites outside 1..32");

        // The shipped LT10 profile (status stage PROBE on, write modes off) must validate clean.
        string lt10Path = Path.Combine(root, "work", "battle-runtime-settings.lt10-counterpath-probe.json");
        if (File.Exists(lt10Path))
        {
            Check(RuntimeSettings.TryLoad(lt10Path, out var lt10Settings, out string lt10Error), $"LT10 profile should load: {lt10Error}");
            var lt10Report = RuntimeSettingsValidator.Validate(lt10Settings, catalog);
            Check(lt10Report.ErrorCount == 0, $"LT10 profile should validate with 0 errors, got {lt10Report.ErrorCount}");
            Check(lt10Settings.DclStatusStageProbeEnabled, "LT10 profile should enable the staged-status probe");
            Check(!lt10Settings.DclStatusSuppressEnabled, "LT10 profile should keep status suppress off");
            Check(lt10Settings.DclStatusForceId < 0, "LT10 profile should keep status force off");
        }
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

    private static void TestDclMpEconomy(ItemCatalog catalog)
    {
        var state = new DclMpTrickleState();
        Check(!state.Observe(0x01, true), "attaching during an already-active turn must not grant MP");
        Check(!state.Observe(0x01, true), "a held own-turn marker must not repeat MP trickle");
        Check(!state.Observe(0x01, false), "falling edge must only arm the next own turn");
        Check(state.Observe(0x01, true), "own-turn rising edge must grant exactly one MP trickle opportunity");
        Check(!state.Observe(0x01, true), "same turn must not grant twice");
        Check(!state.Observe(0x02, true), "pointer reuse by another character must reset without granting MP");

        Check(DclMpEconomy.ClampCredit(3, 10, 20) == 3, "ordinary MP trickle should pass through");
        Check(DclMpEconomy.ClampCredit(8, 17, 20) == 3, "MP trickle must clamp to MaxMP");
        Check(DclMpEconomy.ClampCredit(-5, 10, 20) == 0, "negative MP trickle must clamp to zero");
        Check(DclMpEconomy.ClampCredit(5, 20, 20) == 0, "full MP must receive no trickle");

        var valid = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMpTrickleEnabled = true,
            DclMpTrickleFormula = "if(t.status.ko, 0, max(1, t.ma / 4))",
        }, catalog);
        Check(valid.Success, "configured MP trickle formula should pass settings validation: " +
            string.Join(" | ", valid.Findings.Where(finding => finding.Severity == "ERROR")));

        var missingFormula = RuntimeSettingsValidator.Validate(new RuntimeSettings
        {
            DclMpTrickleEnabled = true,
        }, catalog);
        Check(!missingFormula.Success && missingFormula.Findings.Any(finding => finding.Scope == "DclMpTrickleFormula"),
            "enabled MP trickle without a formula must fail settings validation");
    }

    private static void TestDclMultistrike()
    {
        var blockSuccess = DclPhysicalContest.Resolve(
            13, 10, new DclDefenseOption(DclDefenseKind.Block, 13, true), 12);
        var parryFailure = DclPhysicalContest.Resolve(
            13, 10, new DclDefenseOption(DclDefenseKind.Parry, 11, true), 12);
        var critical = DclPhysicalContest.Resolve(
            13, 4, new DclDefenseOption(DclDefenseKind.Dodge, 8, false), 8);
        var aggregate = DclMultistrike.AggregatePhysical(
            [blockSuccess, parryFailure, critical], normalDebit: 18, criticalDebit: 27);

        Check(aggregate.StrikeCount == 3, "managed Pummel must preserve the authored strike count");
        Check(aggregate.HitCount == 2 && aggregate.CriticalCount == 1,
            "each landed strike, including criticals, must contribute independently");
        Check(aggregate.DefendedCount == 1 && aggregate.AttackMissCount == 0,
            "a successful finite defense must cancel only its own strike");
        Check(aggregate.FumbleCount == 0,
            "ordinary misses and defended strikes must not be reported as fumbles");
        Check(aggregate.BlockAttempts == 1 && aggregate.ParryAttempts == 1,
            "selected finite defenses must spend per attempted defense, including a failed Parry");
        Check(aggregate.TotalDebit == 45 && aggregate.AnyHit,
            "aggregate staged debit must sum only landed per-strike damage");

        var allMiss = DclMultistrike.AggregatePhysical(
            [DclPhysicalContest.Resolve(9, 14, new DclDefenseOption(DclDefenseKind.Dodge, 8, false), 10)],
            normalDebit: 18,
            criticalDebit: 27);
        Check(!allMiss.AnyHit && allMiss.TotalDebit == 0 && allMiss.AttackMissCount == 1,
            "an all-miss managed multistrike must stage zero HP debit");
        Check(DclMultistrike.AnyHitChancePercent([50, 50, 50]) == 88,
            "three independent 50% strike chances should report an 88% rounded any-hit chance");
        Check(DclMultistrike.AnyHitChancePercent([]) == 0,
            "an empty multistrike chance set must not report a hit chance");

        int freshBlockPct = DclPhysicalContest.HitChancePercent(
            13,
            new DclDefenseOption(DclDefenseKind.Block, 12, true));
        int naiveFreshBlockPct = DclMultistrike.AnyHitChancePercent(
            [freshBlockPct, freshBlockPct, freshBlockPct]);
        int finiteBlockPct = DclMultistrike.ExactPhysicalAnyHitChancePercent(
            strikeCount: 3,
            initialParryUses: 0,
            initialBlockUses: 1,
            (strikeIndex, parryRemaining, blockRemaining) => new DclPhysicalStrikeProfile(
                AttackSkill: 13,
                Defense: blockRemaining > 0
                    ? new DclDefenseOption(DclDefenseKind.Block, 12, true)
                    : new DclDefenseOption(DclDefenseKind.None, 0, false)));
        Check(finiteBlockPct > naiveFreshBlockPct,
            "exact multistrike forecast must account for a finite Block disappearing on defended no-hit branches");

        int pummelProbePct = DclMultistrike.ExactPhysicalAnyHitChancePercent(
            strikeCount: 3,
            initialParryUses: 1,
            initialBlockUses: 1,
            (strikeIndex, parryRemaining, blockRemaining) => new DclPhysicalStrikeProfile(
                AttackSkill: 12 - strikeIndex,
                Defense: blockRemaining > 0
                    ? new DclDefenseOption(DclDefenseKind.Block, 13, true)
                    : parryRemaining > 0
                        ? new DclDefenseOption(DclDefenseKind.Parry, 11, true)
                        : new DclDefenseOption(DclDefenseKind.Dodge, 8, false)));
        Check(pummelProbePct == 48,
            $"deterministic Pummel probe should expose the exact nominal 48% any-hit forecast, got {pummelProbePct}%");

        var variables = MakeFormulaContext();
        FormulaRuntimeContextBuilder.AddDclMultistrikeVariables(variables, aggregate);
        var strikeVars = FormulaVariables(variables);
        CheckVar(strikeVars, "dcl.strike.count", 3);
        CheckVar(strikeVars, "dcl.strike.hitCount", 2);
        CheckVar(strikeVars, "dcl.strike.normalHitCount", 1);
        CheckVar(strikeVars, "dcl.strike.criticalCount", 1);
        CheckVar(strikeVars, "dcl.strike.evadedCount", 0);
        CheckVar(strikeVars, "dcl.strike.defendedCount", 1);
        CheckVar(strikeVars, "dcl.strike.parryAttempts", 1);
        CheckVar(strikeVars, "dcl.strike.blockAttempts", 1);
        CheckVar(strikeVars, "dcl.strike.anyHit", 1);

        int targetEvadeEvaluations = 0;
        int targetRolls = 0;
        var perTarget = DclMagicMultistrike.Resolve(
            strikeCount: 4,
            DclMagicMultistrikeAvoidance.PerTarget,
            magicEvadeCapPct: 50,
            strikeIndex => { targetEvadeEvaluations++; return 60; },
            strikeIndex => { targetRolls++; return 49; });
        Check(targetEvadeEvaluations == 1 && targetRolls == 1,
            "per-target Magic Evade must make one decision for the whole multistrike target");
        Check(perTarget.Aggregate.HitCount == 4 && perTarget.Aggregate.EvadedCount == 0 && perTarget.AnyHitPct == 50,
            "a successful capped per-target Magic Evade decision must admit every authored strike");

        int[] rawEvade = [10, 20, 30];
        int[] rolls = [95, 10, 75];
        var perStrike = DclMagicMultistrike.Resolve(
            strikeCount: 3,
            DclMagicMultistrikeAvoidance.PerStrike,
            magicEvadeCapPct: 50,
            strikeIndex => rawEvade[strikeIndex],
            strikeIndex => rolls[strikeIndex]);
        Check(perStrike.Aggregate.HitCount == 1 && perStrike.Aggregate.EvadedCount == 2,
            "per-strike Magic Evade must preserve independent landed/evaded counts");
        Check(perStrike.AnyHitPct == 99,
            $"90/80/70 strike chances should round to a 99% any-hit forecast, got {perStrike.AnyHitPct}%");
        Check(perStrike.Strikes.Select(strike => strike.Roll).SequenceEqual(rolls),
            "per-strike Magic Evade must preserve each roll for diagnostics");

        Check(DclNativeRepeat.HasMoreRepeats(true, 6, 1) &&
              DclNativeRepeat.HasMoreRepeats(true, 6, 5) &&
              !DclNativeRepeat.HasMoreRepeats(true, 6, 6) &&
              !DclNativeRepeat.HasMoreRepeats(false, 6, 1),
            "RandomFire decision retention must last through every non-final native repeat only");
        int[] truthPercentiles = [0, 4, 5, 9, 10, 19, 20, 29, 30, 49, 50, 69, 70, 79, 80, 89, 90, 94, 95, 99];
        int[] truthCounts = [1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10];
        Check(truthPercentiles.Select(DclNativeRepeat.TruthRepeatCountFromPercentile).SequenceEqual(truthCounts),
            "Truth/Untruth percentile boundaries must match the native weighted repeat table");
        Check(DclNativeRepeat.Formula5ERepeatCount(2) == 3 && DclNativeRepeat.Formula5ERepeatCount(5) == 6,
            "formula 0x5E must use X+1 repeats for Tri attacks and Dark Whisper");
        Check(DclNativeRepeat.BarrageRepeatCount == 4,
            "formula 0x6A Barrage must use the native fixed four-repeat carrier");
    }

    private static void WriteUInt16(byte[] raw, int offset, int value)
    {
        raw[offset] = (byte)(value & 0xFF);
        raw[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
