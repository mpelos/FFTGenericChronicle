namespace fftivc.generic.chronicle.codemod;

internal static class FormulaRuntimeContextBuilder
{
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
        int oldCredit)
    {
        var context = new FormulaContext(target, attacker, eventIndex, eventSeed);
        AddSettingsVariables(context, settings);
        AddUnitVariables(context, "target", target);
        AddUnitVariables(context, "t", target);
        AddUnitVariables(context, "attacker", attacker);
        AddUnitVariables(context, "a", attacker);

        var targetSlots = ReadEquipmentSlots(target, settings.EquipmentSlots, itemCatalog);
        var attackerSlots = ReadEquipmentSlots(attacker, settings.AttackerEquipmentSlots, itemCatalog);
        AddSlotVariables(context, "slot", targetSlots);
        AddSlotVariables(context, "targetSlot", targetSlots);
        AddSlotVariables(context, "tslot", targetSlots);
        AddSlotVariables(context, "attackerSlot", attackerSlots);
        AddSlotVariables(context, "aslot", attackerSlots);

        if (abilityCatalog.TryGet(abilityId, out var ability))
            ability.AddVariables(context, "ability");
        else
            AbilityCatalogEntry.AddDefaultVariables(context, "ability", abilityId);

        context.Set("action.type", actionType);
        context.Set("action.abilityId", abilityId);
        context.Set("dcl.oldDebit", oldDebit);
        context.Set("dcl.oldCredit", oldCredit);
        return context;
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
}
