namespace fftivc.generic.chronicle.codemod;

internal sealed class FormulaContext
{
    private readonly Dictionary<string, int> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<List<int>>> _matrices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<int, int>> _maps = new(StringComparer.OrdinalIgnoreCase);
    private long _randomCounter;

    public FormulaContext(UnitSnapshot target, UnitSnapshot? attacker = null, long eventIndex = 0, long eventSeed = 0)
    {
        Target = target;
        Attacker = attacker;
        EventIndex = eventIndex;
        EventSeed = eventSeed;
        Set("event.index", ClampToInt(eventIndex));
        Set("event.seed", ClampToInt(eventSeed));
    }

    public UnitSnapshot Target { get; }

    public UnitSnapshot? Attacker { get; }

    public long EventIndex { get; }

    public long EventSeed { get; }

    public void Set(string name, int value)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _variables[name] = value;
    }

    public int GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out int value))
            return value;

        throw new FormulaException($"unknown variable '{name}'");
    }

    public void SetTable(string name, IEnumerable<int>? values)
    {
        if (string.IsNullOrWhiteSpace(name) || values is null) return;
        _tables[name] = values.ToList();
    }

    public int ReadTable(string name, long index, bool clamp, int? fallback = null)
    {
        if (!_tables.TryGetValue(name, out var values) || values.Count == 0)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"unknown or empty table '{name}'");
        }

        long effectiveIndex = index;
        if (clamp)
            effectiveIndex = Math.Min(Math.Max(effectiveIndex, 0), values.Count - 1);

        if (effectiveIndex < 0 || effectiveIndex >= values.Count)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"table '{name}' index {index} is outside 0..{values.Count - 1}");
        }

        return values[(int)effectiveIndex];
    }

    public void SetMatrix(string name, IEnumerable<IEnumerable<int>>? rows)
    {
        if (string.IsNullOrWhiteSpace(name) || rows is null) return;
        _matrices[name] = rows.Select(row => row?.ToList() ?? new List<int>()).ToList();
    }

    public int ReadMatrix(string name, long row, long column, bool clamp, int? fallback = null)
    {
        if (!_matrices.TryGetValue(name, out var rows) || rows.Count == 0)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"unknown or empty matrix '{name}'");
        }

        long effectiveRow = row;
        if (clamp)
            effectiveRow = Math.Min(Math.Max(effectiveRow, 0), rows.Count - 1);

        if (effectiveRow < 0 || effectiveRow >= rows.Count)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"matrix '{name}' row {row} is outside 0..{rows.Count - 1}");
        }

        var values = rows[(int)effectiveRow];
        if (values.Count == 0)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"matrix '{name}' row {effectiveRow} is empty");
        }

        long effectiveColumn = column;
        if (clamp)
            effectiveColumn = Math.Min(Math.Max(effectiveColumn, 0), values.Count - 1);

        if (effectiveColumn < 0 || effectiveColumn >= values.Count)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"matrix '{name}' column {column} is outside 0..{values.Count - 1} for row {effectiveRow}");
        }

        return values[(int)effectiveColumn];
    }

    public void SetMap(string name, IReadOnlyDictionary<string, int>? values)
    {
        if (string.IsNullOrWhiteSpace(name) || values is null) return;

        var parsed = new Dictionary<int, int>();
        foreach (var (key, value) in values)
        {
            if (TryParseMapKey(key, out int parsedKey))
                parsed[parsedKey] = value;
        }

        _maps[name] = parsed;
    }

    public int ReadMap(string name, long key, int? fallback = null)
    {
        if (!_maps.TryGetValue(name, out var values) || values.Count == 0)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"unknown or empty map '{name}'");
        }

        if (key < int.MinValue || key > int.MaxValue)
        {
            if (fallback.HasValue) return fallback.Value;
            throw new FormulaException($"map '{name}' key {key} is outside Int32 range");
        }

        if (values.TryGetValue((int)key, out int value))
            return value;

        if (fallback.HasValue) return fallback.Value;
        throw new FormulaException($"map '{name}' has no key {key}");
    }

    public int ReadTargetByte(long offset)
    {
        return ReadByte(Target, "targetByte", offset);
    }

    public int ReadTargetSByte(long offset)
    {
        return ReadSByte(Target, "targetSByte", offset);
    }

    public int ReadTargetWord(long offset)
    {
        return ReadWord(Target, "targetWord", offset);
    }

    public int ReadTargetShort(long offset)
    {
        return ReadShort(Target, "targetShort", offset);
    }

    public long ReadTargetDWord(long offset)
    {
        return ReadDWord(Target, "targetDWord", offset);
    }

    public int ReadAttackerByte(long offset)
    {
        if (Attacker is null)
            throw new FormulaException("attackerByte is unavailable because attacker context is not mapped");

        return ReadByte(Attacker, "attackerByte", offset);
    }

    public int ReadAttackerSByte(long offset)
    {
        if (Attacker is null)
            throw new FormulaException("attackerSByte is unavailable because attacker context is not mapped");

        return ReadSByte(Attacker, "attackerSByte", offset);
    }

    public int ReadAttackerWord(long offset)
    {
        if (Attacker is null)
            throw new FormulaException("attackerWord is unavailable because attacker context is not mapped");

        return ReadWord(Attacker, "attackerWord", offset);
    }

    public int ReadAttackerShort(long offset)
    {
        if (Attacker is null)
            throw new FormulaException("attackerShort is unavailable because attacker context is not mapped");

        return ReadShort(Attacker, "attackerShort", offset);
    }

    public long ReadAttackerDWord(long offset)
    {
        if (Attacker is null)
            throw new FormulaException("attackerDWord is unavailable because attacker context is not mapped");

        return ReadDWord(Attacker, "attackerDWord", offset);
    }

    public long NextRandomInt(long minInclusive, long maxInclusive)
    {
        if (maxInclusive < minInclusive)
            throw new FormulaException($"random range {minInclusive}..{maxInclusive} is invalid");

        ulong span = unchecked((ulong)(maxInclusive - minInclusive + 1));
        if (span == 0)
            throw new FormulaException("random range is too large");

        ulong sample = SplitMix64(unchecked((ulong)EventSeed) ^
                                  unchecked((ulong)EventIndex * 0x9E3779B97F4A7C15UL) ^
                                  unchecked((ulong)_randomCounter++ * 0xBF58476D1CE4E5B9UL));
        return minInclusive + (long)(sample % span);
    }

    public long RandomIntAt(long index, long minInclusive, long maxInclusive)
        => RandomIntAt(index, 0, minInclusive, maxInclusive);

    public long RandomIntAt(long index, long drawIndex, long minInclusive, long maxInclusive)
    {
        if (maxInclusive < minInclusive)
            throw new FormulaException($"random range {minInclusive}..{maxInclusive} is invalid");

        ulong span = unchecked((ulong)(maxInclusive - minInclusive + 1));
        if (span == 0)
            throw new FormulaException("random range is too large");

        ulong sample = SplitMix64(unchecked((ulong)EventSeed) ^
                                  unchecked((ulong)EventIndex * 0x9E3779B97F4A7C15UL) ^
                                  0xD1B54A32D192ED03UL ^
                                  unchecked((ulong)index * 0x94D049BB133111EBUL) ^
                                  unchecked((ulong)drawIndex * 0xBF58476D1CE4E5B9UL));
        return minInclusive + (long)(sample % span);
    }

    private static int ReadByte(UnitSnapshot unit, string functionName, long offset)
    {
        if (offset < 0 || offset >= unit.Raw.Length)
            throw new FormulaException($"{functionName} offset {offset} is outside 0..{unit.Raw.Length - 1}");

        return unit.Raw[(int)offset];
    }

    private static int ReadSByte(UnitSnapshot unit, string functionName, long offset)
    {
        if (offset < 0 || offset >= unit.Raw.Length)
            throw new FormulaException($"{functionName} offset {offset} is outside 0..{unit.Raw.Length - 1}");

        return unchecked((sbyte)unit.Raw[(int)offset]);
    }

    private static int ReadWord(UnitSnapshot unit, string functionName, long offset)
    {
        if (offset < 0 || offset + 1 >= unit.Raw.Length)
            throw new FormulaException($"{functionName} offset {offset} is outside 0..{unit.Raw.Length - 2}");

        int index = (int)offset;
        return unit.Raw[index] | (unit.Raw[index + 1] << 8);
    }

    private static int ReadShort(UnitSnapshot unit, string functionName, long offset)
    {
        if (offset < 0 || offset + 1 >= unit.Raw.Length)
            throw new FormulaException($"{functionName} offset {offset} is outside 0..{unit.Raw.Length - 2}");

        int index = (int)offset;
        return unchecked((short)(unit.Raw[index] | (unit.Raw[index + 1] << 8)));
    }

    private static long ReadDWord(UnitSnapshot unit, string functionName, long offset)
    {
        if (offset < 0 || offset + 3 >= unit.Raw.Length)
            throw new FormulaException($"{functionName} offset {offset} is outside 0..{unit.Raw.Length - 4}");

        int index = (int)offset;
        uint value = (uint)(unit.Raw[index] |
                            (unit.Raw[index + 1] << 8) |
                            (unit.Raw[index + 2] << 16) |
                            (unit.Raw[index + 3] << 24));
        return value;
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    internal static bool TryParseMapKey(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        text = text.Trim();
        try
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                long parsed = Convert.ToInt64(text[2..], 16);
                if (parsed < int.MinValue || parsed > int.MaxValue) return false;
                value = (int)parsed;
                return true;
            }

            return int.TryParse(text, out value);
        }
        catch
        {
            return false;
        }
    }

    private static ulong SplitMix64(ulong value)
    {
        unchecked
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}

internal static class FormulaExpression
{
    public static bool TryEvaluate(string? expression, FormulaContext context, out int value, out string error)
    {
        value = 0;
        error = "";

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "empty formula";
            return false;
        }

        try
        {
            long raw = new FormulaParser(expression, context).Parse();
            value = ClampToInt(raw);
            return true;
        }
        catch (FormulaException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string NormalizeIdentifierPart(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unnamed";

        var chars = new List<char>(text.Length);
        foreach (char c in text.Trim())
        {
            if (char.IsLetterOrDigit(c))
                chars.Add(char.ToLowerInvariant(c));
            else if (c is '_' or '-' or ' ')
                chars.Add('_');
        }

        string normalized = new string(chars.ToArray()).Trim('_');
        return normalized.Length == 0 ? "unnamed" : normalized;
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }
}

internal sealed class FormulaException : Exception
{
    public FormulaException(string message) : base(message) { }
}

internal sealed class FormulaParser
{
    private readonly FormulaContext _context;
    private readonly string _text;
    private int _pos;
    private bool _suppressEvaluation;

    public FormulaParser(string text, FormulaContext context)
    {
        _text = text;
        _context = context;
    }

    public long Parse()
    {
        long value = ParseOr();
        SkipWhiteSpace();
        if (!IsEnd)
            throw Error($"unexpected token '{Current}'");

        return value;
    }

    private long ParseOr()
    {
        long left = ParseAnd();
        while (Match("||"))
        {
            if (!_suppressEvaluation && Truthy(left))
            {
                WithSuppressedEvaluation(() => ParseAnd());
                left = 1;
                continue;
            }

            long right = ParseAnd();
            left = Truthy(left) || Truthy(right) ? 1 : 0;
        }

        return left;
    }

    private long ParseAnd()
    {
        long left = ParseEquality();
        while (Match("&&"))
        {
            if (!_suppressEvaluation && !Truthy(left))
            {
                WithSuppressedEvaluation(() => ParseEquality());
                left = 0;
                continue;
            }

            long right = ParseEquality();
            left = Truthy(left) && Truthy(right) ? 1 : 0;
        }

        return left;
    }

    private long ParseEquality()
    {
        long left = ParseComparison();
        while (true)
        {
            if (Match("=="))
            {
                long right = ParseComparison();
                left = left == right ? 1 : 0;
            }
            else if (Match("!="))
            {
                long right = ParseComparison();
                left = left != right ? 1 : 0;
            }
            else
            {
                return left;
            }
        }
    }

    private long ParseComparison()
    {
        long left = ParseAdditive();
        while (true)
        {
            if (Match(">="))
            {
                long right = ParseAdditive();
                left = left >= right ? 1 : 0;
            }
            else if (Match("<="))
            {
                long right = ParseAdditive();
                left = left <= right ? 1 : 0;
            }
            else if (Match(">"))
            {
                long right = ParseAdditive();
                left = left > right ? 1 : 0;
            }
            else if (Match("<"))
            {
                long right = ParseAdditive();
                left = left < right ? 1 : 0;
            }
            else
            {
                return left;
            }
        }
    }

    private long ParseAdditive()
    {
        long left = ParseMultiplicative();
        while (true)
        {
            if (Match("+"))
                left += ParseMultiplicative();
            else if (Match("-"))
                left -= ParseMultiplicative();
            else
                return left;
        }
    }

    private long ParseMultiplicative()
    {
        long left = ParseUnary();
        while (true)
        {
            if (Match("*"))
            {
                left *= ParseUnary();
            }
            else if (Match("/"))
            {
                long right = ParseUnary();
                if (_suppressEvaluation) return 0;
                if (right == 0) throw Error("division by zero");
                left /= right;
            }
            else if (Match("%"))
            {
                long right = ParseUnary();
                if (_suppressEvaluation) return 0;
                if (right == 0) throw Error("modulo by zero");
                left %= right;
            }
            else
            {
                return left;
            }
        }
    }

    private long ParseUnary()
    {
        if (Match("+")) return ParseUnary();
        if (Match("-")) return -ParseUnary();
        if (Match("!")) return Truthy(ParseUnary()) ? 0 : 1;
        return ParsePrimary();
    }

    private long ParsePrimary()
    {
        SkipWhiteSpace();

        if (Match("("))
        {
            long value = ParseOr();
            Require(")");
            return value;
        }

        if (IsEnd)
            throw Error("unexpected end of formula");

        if (char.IsDigit(Current))
            return ParseNumber();

        if (IsIdentifierStart(Current))
        {
            string identifier = ParseIdentifier();
            if (Match("("))
                return ParseFunction(identifier);

            if (_suppressEvaluation)
                return 0;

            return _context.GetVariable(identifier);
        }

        throw Error($"unexpected token '{Current}'");
    }

    private long ParseFunction(string name)
    {
        if (IsMatrixFunction(name))
            return ParseMatrixFunction(name);
        if (IsTableFunction(name))
            return ParseTableFunction(name);
        if (IsMapFunction(name))
            return ParseMapFunction(name);
        if (IsIfFunction(name))
            return ParseIfFunction();

        var args = new List<long>();
        if (!Match(")"))
        {
            do
            {
                args.Add(ParseOr());
            }
            while (Match(","));

            Require(")");
        }

        if (_suppressEvaluation)
            return 0;

        return ApplyFunction(name, args);
    }

    private long ParseTableFunction(string functionName)
    {
        string tableName = ParseTableName();
        Require(",");
        long index = ParseOr();
        int? fallback = null;

        if (Match(","))
            fallback = ClampToInt(ParseOr());

        Require(")");

        if (_suppressEvaluation)
            return 0;

        string normalized = functionName.ToLowerInvariant();
        bool clamp = normalized is "tableclamp" or "table_clamp" or "lookupclamp" or "lookup_clamp";
        bool fallbackAllowed = normalized is "tableor" or "table_or" or "lookupor" or "lookup_or";
        if (fallback.HasValue && !fallbackAllowed)
            throw Error($"{functionName} does not accept a fallback argument");

        return _context.ReadTable(tableName, index, clamp, fallback);
    }

    private long ParseMatrixFunction(string functionName)
    {
        string matrixName = ParseTableName();
        Require(",");
        long row = ParseOr();
        Require(",");
        long column = ParseOr();
        int? fallback = null;

        if (Match(","))
            fallback = ClampToInt(ParseOr());

        Require(")");

        if (_suppressEvaluation)
            return 0;

        string normalized = functionName.ToLowerInvariant();
        bool clamp = normalized is "matrixclamp" or "matrix_clamp" or "lookup2dclamp" or
            "lookup_2d_clamp" or "table2dclamp" or "table_2d_clamp";
        bool fallbackAllowed = normalized is "matrixor" or "matrix_or" or "lookup2dor" or
            "lookup_2d_or" or "table2dor" or "table_2d_or";
        if (fallback.HasValue && !fallbackAllowed)
            throw Error($"{functionName} does not accept a fallback argument");

        return _context.ReadMatrix(matrixName, row, column, clamp, fallback);
    }

    private long ParseMapFunction(string functionName)
    {
        string mapName = ParseTableName();
        Require(",");
        long key = ParseOr();
        int? fallback = null;

        if (Match(","))
            fallback = ClampToInt(ParseOr());

        Require(")");

        if (_suppressEvaluation)
            return 0;

        string normalized = functionName.ToLowerInvariant();
        bool fallbackAllowed = normalized is "mapor" or "map_or" or "lookupmapor" or
            "lookup_map_or" or "lookupormap" or "lookup_or_map";
        if (fallback.HasValue && !fallbackAllowed)
            throw Error($"{functionName} does not accept a fallback argument");

        return _context.ReadMap(mapName, key, fallback);
    }

    private long ApplyFunction(string name, List<long> args)
    {
        switch (name.ToLowerInvariant())
        {
            case "min":
                RequireArgCount(name, args, 1, int.MaxValue);
                return args.Min();
            case "max":
                RequireArgCount(name, args, 1, int.MaxValue);
                return args.Max();
            case "clamp":
                RequireArgCount(name, args, 3, 3);
                return Math.Min(Math.Max(args[0], args[1]), args[2]);
            case "abs":
                RequireArgCount(name, args, 1, 1);
                return Math.Abs(args[0]);
            case "sign":
                RequireArgCount(name, args, 1, 1);
                return Math.Sign(args[0]);
            case "hasbit":
            case "has_bit":
            case "bitset":
            case "bit_set":
            case "bit":
                RequireArgCount(name, args, 2, 2);
                return HasBit(args[0], args[1], name) ? 1 : 0;
            case "hasanybits":
            case "has_any_bits":
            case "anybits":
            case "any_bits":
            case "hasany":
            case "has_any":
                RequireArgCount(name, args, 2, 2);
                return HasAnyBits(args[0], args[1], name) ? 1 : 0;
            case "hasallbits":
            case "has_all_bits":
            case "allbits":
            case "all_bits":
            case "hasall":
            case "has_all":
                RequireArgCount(name, args, 2, 2);
                return HasAllBits(args[0], args[1], name) ? 1 : 0;
            case "nobits":
            case "no_bits":
            case "hasnobits":
            case "has_no_bits":
                RequireArgCount(name, args, 2, 2);
                return NoBits(args[0], args[1], name) ? 1 : 0;
            case "bitand":
            case "bit_and":
            case "band":
                RequireArgCount(name, args, 2, int.MaxValue);
                return AggregateBits(args, (left, right) => left & right, name);
            case "bitor":
            case "bit_or":
            case "bor":
                RequireArgCount(name, args, 2, int.MaxValue);
                return AggregateBits(args, (left, right) => left | right, name);
            case "bitxor":
            case "bit_xor":
            case "bxor":
                RequireArgCount(name, args, 2, int.MaxValue);
                return AggregateBits(args, (left, right) => left ^ right, name);
            case "shiftleft":
            case "shift_left":
            case "shl":
                RequireArgCount(name, args, 2, 2);
                return ShiftLeft(args[0], args[1], name);
            case "shiftright":
            case "shift_right":
            case "shr":
                RequireArgCount(name, args, 2, 2);
                return ShiftRight(args[0], args[1], name);
            case "bitextract":
            case "bit_extract":
            case "extractbits":
            case "extract_bits":
            case "bitfield":
            case "bits":
                RequireArgCount(name, args, 3, 3);
                return ExtractBits(args[0], args[1], args[2], name);
            case "signedbitextract":
            case "signed_bit_extract":
            case "extractsignedbits":
            case "extract_signed_bits":
            case "signedbits":
            case "signed_bits":
                RequireArgCount(name, args, 3, 3);
                return ExtractSignedBits(args[0], args[1], args[2], name);
            case "floordiv":
            case "floor_div":
                RequireArgCount(name, args, 2, 2);
                return FloorDiv(args[0], args[1]);
            case "ceildiv":
            case "ceil_div":
                RequireArgCount(name, args, 2, 2);
                return CeilDiv(args[0], args[1]);
            case "rounddiv":
            case "round_div":
                RequireArgCount(name, args, 2, 2);
                return RoundDiv(args[0], args[1]);
            case "muldiv":
            case "mul_div":
            case "muldivfloor":
            case "mul_div_floor":
            case "scale":
                RequireArgCount(name, args, 3, 3);
                return FloorDiv(args[0] * args[1], args[2]);
            case "muldivceil":
            case "mul_div_ceil":
            case "scaleceil":
            case "scale_ceil":
                RequireArgCount(name, args, 3, 3);
                return CeilDiv(args[0] * args[1], args[2]);
            case "muldivround":
            case "mul_div_round":
            case "scaleround":
            case "scale_round":
                RequireArgCount(name, args, 3, 3);
                return RoundDiv(args[0] * args[1], args[2]);
            case "dicemin":
            case "dice_min":
                RequireArgCount(name, args, 3, 3);
                return DiceMin(args[0], args[1], args[2]);
            case "dicemax":
            case "dice_max":
                RequireArgCount(name, args, 3, 3);
                return DiceMax(args[0], args[1], args[2]);
            case "diceavg":
            case "diceaverage":
            case "dice_avg":
            case "dice_average":
                RequireArgCount(name, args, 3, 3);
                return DiceAverage(args[0], args[1], args[2]);
            case "diceavground":
            case "diceaverageround":
            case "dice_avg_round":
            case "dice_average_round":
                RequireArgCount(name, args, 3, 3);
                return DiceAverageRound(args[0], args[1], args[2]);
            case "diceavgceil":
            case "diceaverageceil":
            case "dice_avg_ceil":
            case "dice_average_ceil":
                RequireArgCount(name, args, 3, 3);
                return DiceAverageCeil(args[0], args[1], args[2]);
            case "diceroll":
            case "dice_roll":
            case "rolldice":
            case "roll_dice":
            case "roll":
                RequireArgCount(name, args, 3, 3);
                return DiceRoll(args[0], args[1], args[2]);
            case "dicerollat":
            case "dice_roll_at":
            case "rollat":
            case "roll_at":
                RequireArgCount(name, args, 4, 4);
                return DiceRollAt(args[0], args[1], args[2], args[3]);
            case "rand":
            case "random":
            case "randomint":
            case "random_int":
                RequireArgCount(name, args, 2, 2);
                return _context.NextRandomInt(args[0], args[1]);
            case "randat":
            case "rand_at":
            case "randomat":
            case "random_at":
            case "randomintat":
            case "random_int_at":
                RequireArgCount(name, args, 3, 3);
                return _context.RandomIntAt(args[0], args[1], args[2]);
            case "targetbyte":
            case "tbyte":
            case "byte":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadTargetByte(args[0]);
            case "targetsbyte":
            case "target_sbyte":
            case "tsbyte":
            case "sbyte":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadTargetSByte(args[0]);
            case "targetword":
            case "tword":
            case "word":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadTargetWord(args[0]);
            case "targetshort":
            case "target_short":
            case "tshort":
            case "short":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadTargetShort(args[0]);
            case "targetdword":
            case "target_dword":
            case "tdword":
            case "dword":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadTargetDWord(args[0]);
            case "attackerbyte":
            case "abyte":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadAttackerByte(args[0]);
            case "attackersbyte":
            case "attacker_sbyte":
            case "asbyte":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadAttackerSByte(args[0]);
            case "attackerword":
            case "aword":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadAttackerWord(args[0]);
            case "attackershort":
            case "attacker_short":
            case "ashort":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadAttackerShort(args[0]);
            case "attackerdword":
            case "attacker_dword":
            case "adword":
                RequireArgCount(name, args, 1, 1);
                return _context.ReadAttackerDWord(args[0]);
            default:
                throw Error($"unknown function '{name}'");
        }
    }

    private long ParseIfFunction()
    {
        long condition = ParseOr();
        Require(",");
        string trueExpression = ReadBranchUntil(',');
        Require(",");
        string falseExpression = ReadBranchUntil(')');
        Require(")");

        if (_suppressEvaluation)
            return 0;

        string selected = Truthy(condition) ? trueExpression : falseExpression;
        if (string.IsNullOrWhiteSpace(selected))
            throw Error("if branch is empty");

        return new FormulaParser(selected, _context).Parse();
    }

    private string ReadBranchUntil(char delimiter)
    {
        SkipWhiteSpace();
        int start = _pos;
        int depth = 0;
        char quote = '\0';

        while (!IsEnd)
        {
            char c = Current;
            if (quote != '\0')
            {
                _pos++;
                if (c == quote)
                {
                    if (!IsEnd && Current == quote)
                        _pos++;
                    else
                        quote = '\0';
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                _pos++;
                continue;
            }

            if (c == '(')
            {
                depth++;
                _pos++;
                continue;
            }

            if (c == ')')
            {
                if (depth == 0)
                    break;
                depth--;
                _pos++;
                continue;
            }

            if (c == delimiter && depth == 0)
                break;

            _pos++;
        }

        string expression = _text[start.._pos].Trim();
        if (expression.Length == 0)
            throw Error("expected expression");
        return expression;
    }

    private long ParseNumber()
    {
        int start = _pos;
        if (Current == '0' && _pos + 1 < _text.Length && (_text[_pos + 1] is 'x' or 'X'))
        {
            _pos += 2;
            int hexStart = _pos;
            while (!IsEnd && Uri.IsHexDigit(Current))
                _pos++;

            if (hexStart == _pos)
                throw Error("expected hex digits after 0x");

            return Convert.ToInt64(_text[hexStart.._pos], 16);
        }

        while (!IsEnd && char.IsDigit(Current))
            _pos++;

        return long.Parse(_text[start.._pos]);
    }

    private string ParseIdentifier()
    {
        int start = _pos;
        _pos++;
        while (!IsEnd && IsIdentifierPart(Current))
            _pos++;

        return _text[start.._pos];
    }

    private void Require(string token)
    {
        if (!Match(token))
            throw Error($"expected '{token}'");
    }

    private void RequireArgCount(string name, List<long> args, int min, int max)
    {
        if (_suppressEvaluation)
            return;

        if (args.Count < min || args.Count > max)
        {
            string expected = min == max ? min.ToString() : $"{min}..{max}";
            throw Error($"{name} expects {expected} argument(s), got {args.Count}");
        }
    }

    private void WithSuppressedEvaluation(Action parse)
    {
        bool previous = _suppressEvaluation;
        _suppressEvaluation = true;
        try
        {
            parse();
        }
        finally
        {
            _suppressEvaluation = previous;
        }
    }

    private bool Match(string token)
    {
        SkipWhiteSpace();
        if (_pos + token.Length > _text.Length)
            return false;

        if (string.CompareOrdinal(_text, _pos, token, 0, token.Length) != 0)
            return false;

        _pos += token.Length;
        return true;
    }

    private void SkipWhiteSpace()
    {
        while (!IsEnd && char.IsWhiteSpace(Current))
            _pos++;
    }

    private FormulaException Error(string message) => new($"{message} at position {_pos}");

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '.';

    private static bool IsTableFunction(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower is "table" or "lookup" or "tableclamp" or "table_clamp" or "lookupclamp" or
            "lookup_clamp" or "tableor" or "table_or" or "lookupor" or "lookup_or";
    }

    private static bool IsMatrixFunction(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower is "matrix" or "lookup2d" or "lookup_2d" or "table2d" or "table_2d" or
            "matrixclamp" or "matrix_clamp" or "lookup2dclamp" or "lookup_2d_clamp" or
            "table2dclamp" or "table_2d_clamp" or "matrixor" or "matrix_or" or
            "lookup2dor" or "lookup_2d_or" or "table2dor" or "table_2d_or";
    }

    private static bool IsMapFunction(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower is "map" or "lookupmap" or "lookup_map" or "mapor" or "map_or" or
            "lookupmapor" or "lookup_map_or" or "lookupormap" or "lookup_or_map";
    }

    private static bool IsIfFunction(string name) => name.Equals("if", StringComparison.OrdinalIgnoreCase);

    private long DiceMin(long dice, long sides, long adds) => CheckedDice(dice, sides, adds, "diceMin").Dice + adds;

    private long DiceMax(long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceMax");
        return checkedDice.Dice * checkedDice.Sides + adds;
    }

    private long DiceAverage(long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceAvg");
        return checkedDice.Dice * (checkedDice.Sides + 1) / 2 + adds;
    }

    private long DiceAverageRound(long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceAvgRound");
        return RoundDiv(checkedDice.Dice * (checkedDice.Sides + 1), 2) + adds;
    }

    private long DiceAverageCeil(long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceAvgCeil");
        return CeilDiv(checkedDice.Dice * (checkedDice.Sides + 1), 2) + adds;
    }

    private long DiceRoll(long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceRoll");
        long total = adds;
        for (long i = 0; i < checkedDice.Dice; i++)
            total += _context.NextRandomInt(1, checkedDice.Sides);
        return total;
    }

    private long DiceRollAt(long index, long dice, long sides, long adds)
    {
        var checkedDice = CheckedDice(dice, sides, adds, "diceRollAt");
        long total = adds;
        for (long i = 0; i < checkedDice.Dice; i++)
            total += _context.RandomIntAt(index, i, 1, checkedDice.Sides);
        return total;
    }

    private long FloorDiv(long numerator, long denominator)
    {
        if (denominator == 0)
            throw Error("division by zero");

        long quotient = numerator / denominator;
        long remainder = numerator % denominator;
        if (remainder != 0 && ((remainder > 0) != (denominator > 0)))
            quotient--;
        return quotient;
    }

    private long CeilDiv(long numerator, long denominator)
    {
        if (denominator == 0)
            throw Error("division by zero");

        long quotient = numerator / denominator;
        long remainder = numerator % denominator;
        if (remainder != 0 && ((remainder > 0) == (denominator > 0)))
            quotient++;
        return quotient;
    }

    private long RoundDiv(long numerator, long denominator)
    {
        if (denominator == 0)
            throw Error("division by zero");

        double rounded = Math.Round((double)numerator / denominator, MidpointRounding.AwayFromZero);
        if (rounded > long.MaxValue) return long.MaxValue;
        if (rounded < long.MinValue) return long.MinValue;
        return (long)rounded;
    }

    private bool HasBit(long value, long bitIndex, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        int bit = CheckedBitIndex(bitIndex, functionName);
        return (value & (1L << bit)) != 0;
    }

    private bool HasAnyBits(long value, long mask, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        RequireNonNegative(mask, functionName, "mask");
        return (value & mask) != 0;
    }

    private bool HasAllBits(long value, long mask, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        RequireNonNegative(mask, functionName, "mask");
        return mask != 0 && (value & mask) == mask;
    }

    private bool NoBits(long value, long mask, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        RequireNonNegative(mask, functionName, "mask");
        return (value & mask) == 0;
    }

    private long AggregateBits(List<long> args, Func<long, long, long> operation, string functionName)
    {
        long value = args[0];
        RequireNonNegative(value, functionName, "value");
        for (int i = 1; i < args.Count; i++)
        {
            RequireNonNegative(args[i], functionName, $"arg{i + 1}");
            value = operation(value, args[i]);
        }
        return value;
    }

    private long ShiftLeft(long value, long bits, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        int shift = CheckedBitIndex(bits, functionName);
        return value << shift;
    }

    private long ShiftRight(long value, long bits, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        int shift = CheckedBitIndex(bits, functionName);
        return value >> shift;
    }

    private long ExtractBits(long value, long startBit, long bitWidth, string functionName)
    {
        RequireNonNegative(value, functionName, "value");
        int start = CheckedBitIndex(startBit, functionName);
        int width = CheckedBitWidth(start, bitWidth, functionName);
        long mask = (1L << width) - 1;
        return (value >> start) & mask;
    }

    private long ExtractSignedBits(long value, long startBit, long bitWidth, string functionName)
    {
        long extracted = ExtractBits(value, startBit, bitWidth, functionName);
        int width = (int)bitWidth;
        long signBit = 1L << (width - 1);
        if ((extracted & signBit) == 0)
            return extracted;

        return extracted - (1L << width);
    }

    private int CheckedBitIndex(long bitIndex, string functionName)
    {
        if (bitIndex is < 0 or > 62)
            throw Error($"{functionName} bit index {bitIndex} is outside 0..62");
        return (int)bitIndex;
    }

    private int CheckedBitWidth(int startBit, long bitWidth, string functionName)
    {
        if (bitWidth is < 1 or > 62)
            throw Error($"{functionName} bit width {bitWidth} is outside 1..62");
        if (startBit + bitWidth > 63)
            throw Error($"{functionName} bit range {startBit}..{startBit + bitWidth - 1} exceeds bit 62");
        return (int)bitWidth;
    }

    private void RequireNonNegative(long value, string functionName, string argName)
    {
        if (value < 0)
            throw Error($"{functionName} {argName} cannot be negative");
    }

    private (long Dice, long Sides) CheckedDice(long dice, long sides, long adds, string functionName)
    {
        if (dice < 0) throw Error($"{functionName} dice count cannot be negative");
        if (dice > 100) throw Error($"{functionName} dice count {dice} is too high");
        if (sides < 1) throw Error($"{functionName} side count must be >= 1");
        if (sides > 1000) throw Error($"{functionName} side count {sides} is too high");
        if (adds is < -100000 or > 100000) throw Error($"{functionName} adds {adds} is outside -100000..100000");
        return (dice, sides);
    }

    private string ParseTableName()
    {
        SkipWhiteSpace();
        if (IsEnd)
            throw Error("expected table name");

        if (Current == '"' || Current == '\'')
            return ParseStringLiteral();

        if (!IsIdentifierStart(Current))
            throw Error("expected table name");

        return ParseIdentifier();
    }

    private string ParseStringLiteral()
    {
        char quote = Current;
        _pos++;
        var sb = new System.Text.StringBuilder();

        while (!IsEnd)
        {
            char c = Current;
            _pos++;
            if (c == quote)
                return sb.ToString();

            if (c == '\\')
            {
                if (IsEnd)
                    throw Error("unterminated escape sequence");

                char escaped = Current;
                _pos++;
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => escaped,
                });
            }
            else
            {
                sb.Append(c);
            }
        }

        throw Error("unterminated string literal");
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    private static bool Truthy(long value) => value != 0;

    private bool IsEnd => _pos >= _text.Length;

    private char Current => _text[_pos];
}
