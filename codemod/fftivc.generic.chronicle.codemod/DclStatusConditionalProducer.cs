namespace fftivc.generic.chronicle.codemod;

internal static class DclStatusBitCatalog
{
    private static readonly Dictionary<string, DclNativeStatusBit> Bits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Crystal"] = new(0, 0x40), ["Dead"] = new(0, 0x20), ["Undead"] = new(0, 0x10),
        ["Petrify"] = new(1, 0x80), ["Invite"] = new(1, 0x40), ["Darkness"] = new(1, 0x20),
        ["Confusion"] = new(1, 0x10), ["Silence"] = new(1, 0x08), ["BloodSuck"] = new(1, 0x04),
        ["Oil"] = new(2, 0x80), ["Float"] = new(2, 0x40), ["Reraise"] = new(2, 0x20),
        ["Transparent"] = new(2, 0x10), ["Berserk"] = new(2, 0x08), ["Frog"] = new(2, 0x02),
        ["Poison"] = new(3, 0x80), ["Regen"] = new(3, 0x40), ["Protect"] = new(3, 0x20),
        ["Shell"] = new(3, 0x10), ["Haste"] = new(3, 0x08), ["Slow"] = new(3, 0x04),
        ["Stop"] = new(3, 0x02), ["Faith"] = new(4, 0x80), ["Innocent"] = new(4, 0x40),
        ["Charm"] = new(4, 0x20), ["Sleep"] = new(4, 0x10), ["DontMove"] = new(4, 0x08),
        ["DontAct"] = new(4, 0x04), ["Reflect"] = new(4, 0x02), ["DeathSentence"] = new(4, 0x01),
    };

    public static bool TryGet(string name, out DclNativeStatusBit bit)
        => Bits.TryGetValue((name ?? "").Trim(), out bit);
}

internal static class DclStatusConditionalProducer
{
    private static readonly HashSet<int> SupportedFormulas =
    [
        0x0A, 0x0B, 0x1C, 0x1D, 0x1E, 0x1F, 0x29, 0x2A, 0x33, 0x3D, 0x3F, 0x40, 0x41, 0x50, 0x51, 0x5A,
    ];

    private static readonly HashSet<int> PerformanceFormulas = [0x1C, 0x1D];
    private static readonly HashSet<int> RandomFireFormulas = [0x1E, 0x1F];

    public static bool IsSupportedFormula(int formula) => SupportedFormulas.Contains(formula);

    public static bool IsPerformanceFormula(int formula) => PerformanceFormulas.Contains(formula);

    public static bool IsRandomFireFormula(int formula) => RandomFireFormulas.Contains(formula);

    public static bool IsBlockedByNativeEligibility(AbilityCatalogEntry ability, UnitSnapshot caster)
        // Song/Dance handlers clear the result before their shared chance/finalizer path while the
        // performer is asleep. The managed producer owns the chance but must not resurrect that
        // native stop condition.
        => IsPerformanceFormula(ability.Formula) && (caster.ReadByte(0x65) & 0x10) != 0;

    public static bool TryDescribe(
        AbilityCatalogEntry ability,
        out IReadOnlyList<DclNativeStatusBit> requiredBits,
        out string requiredOperation,
        out string requiredContestMode,
        out string error)
    {
        requiredBits = Array.Empty<DclNativeStatusBit>();
        requiredOperation = "";
        requiredContestMode = DclStatusGroups.Independent;
        error = "";

        if (!IsSupportedFormula(ability.Formula))
        {
            error = $"formula 0x{ability.Formula:X2} is not in the statically mapped conditional-producer family";
            return false;
        }
        if (IsRandomFireFormula(ability.Formula) && ability.RandomFire == 0)
        {
            error = $"formula 0x{ability.Formula:X2} status production requires the native RandomFire carrier";
            return false;
        }

        var bits = new List<DclNativeStatusBit>();
        foreach (string token in (ability.InflictStatuses ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!DclStatusBitCatalog.TryGet(token, out var bit))
            {
                error = $"native status token '{token}' has no packet-bit mapping";
                return false;
            }
            if (bit.ByteIndex == 0 && bit.Mask != 0x10)
            {
                error = $"native status token '{token}' is lifecycle-owned and cannot use the generic packet producer";
                return false;
            }
            bits.Add(bit);
        }

        if (bits.Count == 0)
        {
            error = "ability has no native status payload";
            return false;
        }

        requiredBits = bits.Distinct().ToArray();
        bool cancel = ability.InflictStatusMode.Equals("Cancel", StringComparison.OrdinalIgnoreCase);
        requiredOperation = cancel ? "remove" : "add";
        requiredContestMode = ability.InflictStatusMode.Equals("Random", StringComparison.OrdinalIgnoreCase) && requiredBits.Count > 1
            ? DclStatusGroups.RandomOne
            : !cancel && ability.InflictStatusMode.Equals("AllOrNothing", StringComparison.OrdinalIgnoreCase) && requiredBits.Count > 1
                ? DclStatusGroups.AllOrNothing
                : DclStatusGroups.Independent;
        return true;
    }

    public static bool TryValidateRules(
        AbilityCatalogEntry ability,
        IReadOnlyCollection<DclStatusRule> rules,
        out string error)
    {
        error = "";
        if (!TryDescribe(ability, out var requiredBits, out string operation, out string contestMode, out error))
            return false;
        if (rules.Count == 0)
        {
            error = "no post-calc producer rules are configured";
            return false;
        }
        if (rules.Any(rule => !rule.NativeRiderReplacedPostCalc || rule.ActionType != -1))
        {
            error = "every producer rule must use NativeRiderPolicy='replaced-post-calc' and ActionType=-1";
            return false;
        }

        var ownedBits = rules.Select(rule => new DclNativeStatusBit(rule.StatusByteIndex, (byte)rule.StatusMask)).ToHashSet();
        if (!ownedBits.SetEquals(requiredBits))
        {
            string expected = string.Join(", ", requiredBits.Select(bit => $"{bit.ByteIndex}/0x{bit.Mask:X2}"));
            string actual = string.Join(", ", ownedBits.Select(bit => $"{bit.ByteIndex}/0x{bit.Mask:X2}"));
            error = $"packet ownership mismatch; expected [{expected}], got [{actual}]";
            return false;
        }
        if (rules.Any(rule => !rule.Operation.Equals(operation, StringComparison.OrdinalIgnoreCase)))
        {
            error = $"native mode '{ability.InflictStatusMode}' requires operation '{operation}' for every packet bit";
            return false;
        }
        if (rules.Any(rule => rule.NormalizedContestMode != contestMode))
        {
            error = $"native mode '{ability.InflictStatusMode}' requires ContestMode='{contestMode}'";
            return false;
        }
        if (contestMode != DclStatusGroups.Independent)
        {
            string[] groups = rules.Select(rule => rule.NormalizedContestGroup).Distinct().ToArray();
            if (groups.Length != 1 || groups[0].Length == 0)
            {
                error = $"ContestMode='{contestMode}' requires every native packet bit in one nonempty ContestGroup";
                return false;
            }
        }
        return true;
    }
}
