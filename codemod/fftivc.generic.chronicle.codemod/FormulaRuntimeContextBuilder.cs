namespace fftivc.generic.chronicle.codemod;

internal static class FormulaRuntimeContextBuilder
{
    private static readonly (string Name, int ByteIndex, int Mask)[] StatusBits =
    [
        ("crystal", 0, 0x40), ("ko", 0, 0x20), ("undead", 0, 0x10), ("charging", 0, 0x08),
        ("jumping", 0, 0x04), ("defending", 0, 0x02), ("performing", 0, 0x01),
        ("petrify", 1, 0x80), ("traitor", 1, 0x40), ("blind", 1, 0x20), ("confuse", 1, 0x10),
        ("silence", 1, 0x08), ("vampire", 1, 0x04), ("cursed", 1, 0x02), ("treasure", 1, 0x01),
        ("oil", 2, 0x80), ("float", 2, 0x40), ("reraise", 2, 0x20), ("invisible", 2, 0x10),
        ("berserk", 2, 0x08), ("chicken", 2, 0x04), ("frog", 2, 0x02), ("critical", 2, 0x01),
        ("poison", 3, 0x80), ("regen", 3, 0x40), ("protect", 3, 0x20), ("shell", 3, 0x10),
        ("haste", 3, 0x08), ("slow", 3, 0x04), ("stop", 3, 0x02), ("wall", 3, 0x01),
        ("faith", 4, 0x80), ("innocent", 4, 0x40), ("charm", 4, 0x20), ("sleep", 4, 0x10),
        ("immobilize", 4, 0x08), ("disable", 4, 0x04), ("reflect", 4, 0x02), ("doom", 4, 0x01),
    ];

    private static readonly (string Name, int Mask)[] ElementBits =
    [
        ("fire", 0x80), ("lightning", 0x40), ("ice", 0x20), ("wind", 0x10),
        ("earth", 0x08), ("water", 0x04), ("holy", 0x02), ("dark", 0x01),
    ];

    private static readonly string[] ElementAffinityNames = ["absorb", "null", "halve", "weak", "strengthen"];

    public static FormulaContext BuildDclDamageContext(
        RuntimeSettings settings,
        ItemCatalog itemCatalog,
        AbilityCatalog abilityCatalog,
        UnitSnapshot target,
        UnitSnapshot attacker,
        long eventIndex,
        long eventSeed,
        int actionType,
        int abilityId,
        int oldDebit,
        int oldCredit,
        int oldMpDebit = 0,
        int oldMpCredit = 0,
        int actionPayload = -1,
        int activeWeaponItemId = -1,
        int nativeRepeatCount = -1,
        int nativeRepeatIndex = -1,
        int nativeRightWeaponItemId = -1,
        int nativeLeftWeaponItemId = -1,
        int naturalResultFlags = 0)
    {
        var context = new FormulaContext(target, attacker, eventIndex, eventSeed);
        AddSettingsVariables(context, settings);
        AddUnitVariables(context, "target", target);
        AddUnitVariables(context, "t", target);
        AddUnitVariables(context, "attacker", attacker);
        AddUnitVariables(context, "a", attacker);

        AddBaseHpVariables(context, "target", target, itemCatalog);
        AddBaseHpVariables(context, "t", target, itemCatalog);
        AddBaseHpVariables(context, "attacker", attacker, itemCatalog);
        AddBaseHpVariables(context, "a", attacker, itemCatalog);

        var targetSlots = ReadEquipmentSlots(target, settings.EquipmentSlots, itemCatalog);
        var attackerSlots = ReadEquipmentSlots(attacker, settings.AttackerEquipmentSlots, itemCatalog);
        AddSlotVariables(context, "slot", targetSlots);
        AddSlotVariables(context, "targetSlot", targetSlots);
        AddSlotVariables(context, "tslot", targetSlots);
        AddSlotVariables(context, "attackerSlot", attackerSlots);
        AddSlotVariables(context, "aslot", attackerSlots);

        int authoredStrikeCount = 0;
        if (abilityCatalog.TryGet(abilityId, out var ability))
        {
            ability.AddVariables(context, "ability");
            authoredStrikeCount = ability.DclMetadata.IsManagedMultistrike
                ? ability.DclMetadata.StrikeCount
                : 0;
        }
        else
            AbilityCatalogEntry.AddDefaultVariables(context, "ability", abilityId);

        context.Set("action.type", actionType);
        context.Set("action.abilityId", abilityId);
        AddActionPayloadVariables(
            context,
            itemCatalog,
            attacker,
            actionType,
            actionPayload,
            activeWeaponItemId,
            nativeRepeatCount,
            nativeRepeatIndex,
            nativeRightWeaponItemId,
            nativeLeftWeaponItemId);
        context.Set("dcl.oldDebit", oldDebit);
        context.Set("dcl.oldCredit", oldCredit);
        context.Set("dcl.oldMpDebit", oldMpDebit);
        context.Set("dcl.oldMpCredit", oldMpCredit);
        int normalizedResultFlags = naturalResultFlags & 0xFF;
        context.Set("dcl.oldResultFlags", normalizedResultFlags);
        context.Set("dcl.nativeHpDamageResult", (normalizedResultFlags & DclResultFlags.HpDamage) != 0 ? 1 : 0);
        context.Set("dcl.nativeHpCreditResult", (normalizedResultFlags & DclResultFlags.HpCredit) != 0 ? 1 : 0);
        context.Set("dcl.nativeMpDebitResult", (normalizedResultFlags & DclResultFlags.MpDebit) != 0 ? 1 : 0);
        context.Set("dcl.nativeMpCreditResult", (normalizedResultFlags & DclResultFlags.MpCredit) != 0 ? 1 : 0);
        context.Set("dcl.isSelf", target.Ptr == attacker.Ptr ? 1 : 0);
        AddDclMultistrikeVariables(context, new DclMultistrikeAggregate(
            StrikeCount: authoredStrikeCount,
            HitCount: 0,
            CriticalCount: 0,
            AttackMissCount: 0,
            FumbleCount: 0,
            EvadedCount: 0,
            DefendedCount: 0,
            ParryAttempts: 0,
            BlockAttempts: 0,
            TotalDebit: 0));
        return context;
    }

    private static void AddActionPayloadVariables(
        FormulaContext context,
        ItemCatalog itemCatalog,
        UnitSnapshot attacker,
        int actionType,
        int actionPayload,
        int activeWeaponItemId,
        int nativeRepeatCount,
        int nativeRepeatIndex,
        int nativeRightWeaponItemId,
        int nativeLeftWeaponItemId)
    {
        bool payloadKnown = actionPayload >= 0;
        bool nativeWeaponKnown = actionType == 1 && activeWeaponItemId >= 0;
        int resolvedWeaponItemId = nativeWeaponKnown ? activeWeaponItemId : actionPayload;
        bool weaponAction = actionType == 1 && resolvedWeaponItemId >= 0;
        int rightWeapon = Math.Max(0, attacker.ReadUInt16(0x20));
        int leftWeapon = Math.Max(0, attacker.ReadUInt16(0x24));
        bool matchesRight = weaponAction && resolvedWeaponItemId == rightWeapon;
        bool matchesLeft = weaponAction && resolvedWeaponItemId == leftWeapon;
        bool nativeDualWieldSideKnown = nativeWeaponKnown && nativeRepeatCount == 2 &&
                                        nativeRepeatIndex is 0 or 1;
        bool sideKnown = nativeDualWieldSideKnown || matchesRight ^ matchesLeft;
        int weaponSide = nativeDualWieldSideKnown
            ? nativeRepeatIndex + 1
            : sideKnown
                ? (matchesRight ? 1 : 2)
                : 0;

        context.Set("action.payload", actionPayload);
        context.Set("action.payloadId", actionPayload);
        context.Set("action.payloadKnown", payloadKnown ? 1 : 0);
        context.Set("action.weaponItemId", weaponAction ? resolvedWeaponItemId : -1);
        context.Set("action.weaponNativeKnown", nativeWeaponKnown ? 1 : 0);
        context.Set("action.weaponRepeatCount", nativeRepeatCount);
        context.Set("action.weaponRepeatIndex", nativeRepeatIndex);
        context.Set("action.weaponRepeatNumber", nativeRepeatIndex >= 0 ? nativeRepeatIndex + 1 : 0);
        context.Set("action.weaponNativeRightItemId", nativeRightWeaponItemId);
        context.Set("action.weaponNativeLeftItemId", nativeLeftWeaponItemId);
        context.Set("action.weaponKnown", weaponAction && itemCatalog.TryGet(resolvedWeaponItemId, out var item) &&
                                             item.IsSecondaryKind("weapon") != 0 ? 1 : 0);
        context.Set("action.weaponMatchesRight", matchesRight ? 1 : 0);
        context.Set("action.weaponMatchesLeft", matchesLeft ? 1 : 0);
        context.Set("action.weaponSideKnown", sideKnown ? 1 : 0);
        context.Set("action.weaponSide", weaponSide);

        if (weaponAction && itemCatalog.TryGet(resolvedWeaponItemId, out item) && item.IsSecondaryKind("weapon") != 0)
            item.AddVariables(context, "action.weapon");
        else
            ItemCatalogEntry.AddDefaultVariables(context, "action.weapon", weaponAction ? resolvedWeaponItemId : 0);
    }

    public static void AddDclMultistrikeVariables(
        FormulaContext context,
        DclMultistrikeAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Set("dcl.strike.count", aggregate.StrikeCount);
        // index/number are populated only while a per-strike contest formula is executing. Zero in
        // preview and aggregate damage contexts means that no individual strike is selected.
        context.Set("dcl.strike.index", 0);
        context.Set("dcl.strike.number", 0);
        context.Set("dcl.strike.hitCount", aggregate.HitCount);
        context.Set("dcl.strike.normalHitCount", Math.Max(0, aggregate.HitCount - aggregate.CriticalCount));
        context.Set("dcl.strike.criticalCount", aggregate.CriticalCount);
        context.Set("dcl.strike.attackMissCount", aggregate.AttackMissCount);
        context.Set("dcl.strike.fumbleCount", aggregate.FumbleCount);
        context.Set("dcl.strike.evadedCount", aggregate.EvadedCount);
        context.Set("dcl.strike.defendedCount", aggregate.DefendedCount);
        context.Set("dcl.strike.parryAttempts", aggregate.ParryAttempts);
        context.Set("dcl.strike.blockAttempts", aggregate.BlockAttempts);
        context.Set("dcl.strike.anyHit", aggregate.AnyHit ? 1 : 0);
    }

    public static bool TryApplyDerivedVariables(
        FormulaContext context,
        List<FormulaDerivedVariable> variables,
        string groupName,
        out string error)
    {
        error = "";

        foreach (var variable in variables ?? [])
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

    public static void AddSettingsVariables(FormulaContext context, RuntimeSettings settings)
    {
        foreach (var kv in settings.FormulaVariables ?? [])
        {
            context.Set(kv.Key, kv.Value);
            context.Set($"const.{kv.Key}", kv.Value);
        }
        foreach (var kv in settings.FormulaTables ?? [])
            context.SetTable(kv.Key, kv.Value);
        foreach (var kv in settings.FormulaMatrices ?? [])
            context.SetMatrix(kv.Key, kv.Value);
        foreach (var kv in settings.FormulaMaps ?? [])
            context.SetMap(kv.Key, kv.Value);
    }

    public static List<BattleFormulaEngine.EquipmentSlotValue> ReadEquipmentSlots(
        UnitSnapshot? unit,
        List<EquipmentSlotProbe> probes,
        ItemCatalog itemCatalog)
    {
        var slots = new List<BattleFormulaEngine.EquipmentSlotValue>();
        foreach (var slot in probes ?? [])
        {
            string name = string.IsNullOrWhiteSpace(slot.Name) ? $"offset_{slot.Offset:X2}" : slot.Name;
            string variableName = FormulaExpression.NormalizeIdentifierPart(name);

            if (unit is null)
            {
                slots.Add(new BattleFormulaEngine.EquipmentSlotValue(name, variableName, 0, null, false, -1, "", 0));
                continue;
            }

            var resolved = slot.Resolve(unit, itemCatalog);
            if (!resolved.Present)
            {
                slots.Add(new BattleFormulaEngine.EquipmentSlotValue(name, variableName, 0, null, false, resolved.Offset, resolved.Width, resolved.MatchCount));
                continue;
            }

            slots.Add(new BattleFormulaEngine.EquipmentSlotValue(name, variableName, resolved.ItemId, resolved.Item, true, resolved.Offset, resolved.Width, resolved.MatchCount));
        }

        return slots;
    }

    public static void AddSlotVariables(
        FormulaContext context,
        string prefix,
        List<BattleFormulaEngine.EquipmentSlotValue> slots)
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

    public static void AddUnitVariables(FormulaContext context, string prefix, UnitSnapshot? unit)
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

        int B(int offset) => unit is null ? 0 : Math.Max(0, unit.ReadByte(offset));
        int U16(int offset) => unit is null ? 0 : Math.Max(0, unit.ReadUInt16(offset));
        int jobId = B(0x03);
        int jobIndex = jobId - 0x4A;
        int spendableJp = jobIndex is >= 0 and < 23 ? U16(0xF0 + jobIndex * 2) : 0;
        int totalJp = jobIndex is >= 0 and < 23 ? U16(0x11E + jobIndex * 2) : 0;
        context.Set($"{prefix}.job", jobId);
        context.Set($"{prefix}.jobId", jobId);
        context.Set($"{prefix}.jobIndex", Math.Max(0, jobIndex));
        context.Set($"{prefix}.jobJp", spendableJp);
        context.Set($"{prefix}.jobTotalJp", totalJp);
        context.Set($"{prefix}.jobLevel", jobIndex is >= 0 and < 23 ? DclSkillRules.NativeJobLevelFromTotalJp(totalJp) : 0);
        context.Set($"{prefix}.innateAbilityId1", U16(0x0A));
        context.Set($"{prefix}.innateAbilityId2", U16(0x0C));
        context.Set($"{prefix}.innateAbilityId3", U16(0x0E));
        context.Set($"{prefix}.innateAbilityId4", U16(0x10));
        context.Set($"{prefix}.reactionAbilityId", U16(0x14));
        context.Set($"{prefix}.supportAbilityId", U16(0x16));
        context.Set($"{prefix}.movementAbilityId", U16(0x18));
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
        context.Set($"{prefix}.evade48", B(0x48));
        context.Set($"{prefix}.accessoryPhysEva", B(0x49));
        context.Set($"{prefix}.shieldPhysParry", B(0x4A));
        context.Set($"{prefix}.physEva", B(0x4B));
        context.Set($"{prefix}.evade4C", B(0x4C));
        context.Set($"{prefix}.evade4D", B(0x4D));
        context.Set($"{prefix}.shieldMagParry", B(0x4E));
        context.Set($"{prefix}.magEvaRawMax", Math.Max(Math.Max(B(0x48), B(0x4C)), Math.Max(B(0x4D), B(0x4E))));
        context.Set($"{prefix}.x", B(0x4F));
        context.Set($"{prefix}.y", B(0x50));
        context.Set($"{prefix}.facing", B(0x51) & 0x7F);
        context.Set($"{prefix}.mapLevel", (B(0x51) >> 7) & 1);
        context.Set($"{prefix}.turnActive", B(0x1B8) == 1 ? 1 : 0);
        context.Set($"{prefix}.actionOwner", B(0x1BA));

        for (int i = 0; i < 5; i++)
        {
            context.Set($"{prefix}.status.sourceByte{i}", B(0x57 + i));
            context.Set($"{prefix}.status.immunityByte{i}", B(0x5C + i));
            context.Set($"{prefix}.status.effectiveByte{i}", B(0x61 + i));
            context.Set($"{prefix}.status.masterByte{i}", B(0x1EF + i));
        }
        foreach (var (name, byteIndex, mask) in StatusBits)
        {
            context.Set($"{prefix}.status.{name}", (B(0x61 + byteIndex) & mask) != 0 ? 1 : 0);
            context.Set($"{prefix}.status.source.{name}", (B(0x57 + byteIndex) & mask) != 0 ? 1 : 0);
            context.Set($"{prefix}.status.immune.{name}", (B(0x5C + byteIndex) & mask) != 0 ? 1 : 0);
            context.Set($"{prefix}.status.master.{name}", (B(0x1EF + byteIndex) & mask) != 0 ? 1 : 0);
        }

        for (int affinity = 0; affinity < ElementAffinityNames.Length; affinity++)
        {
            int value = B(0x52 + affinity);
            string affinityName = ElementAffinityNames[affinity];
            context.Set($"{prefix}.element.{affinityName}Mask", value);
            foreach (var (elementName, mask) in ElementBits)
                context.Set($"{prefix}.element.{affinityName}.{elementName}", (value & mask) != 0 ? 1 : 0);
        }
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

    public static void AddBaseHpVariables(
        FormulaContext context,
        string prefix,
        UnitSnapshot? unit,
        ItemCatalog itemCatalog)
    {
        var resolution = DclBaseHp.Resolve(unit, itemCatalog);
        context.Set($"{prefix}.baseHp", resolution.BaseHp);
        context.Set($"{prefix}.baseHpResolved", resolution.Resolved ? 1 : 0);
        context.Set($"{prefix}.equipmentHpBonus", resolution.EquipmentHpBonus);
        context.Set($"{prefix}.headItemId", resolution.HeadItemId);
        context.Set($"{prefix}.bodyItemId", resolution.BodyItemId);
    }

    internal static int JobLevelFromTotalJp(int totalJp)
        => DclSkillRules.NativeJobLevelFromTotalJp(totalJp);
}
