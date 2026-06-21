using System.Diagnostics;
using System.Runtime.InteropServices;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace fftivc.generic.chronicle.codemod;

internal sealed class MemoryTableProbeRunner
{
    private readonly IStartupScanner _scanner;
    private readonly nint _moduleBase;
    private readonly IReadOnlyList<MemoryTableProbe> _probes;
    private readonly Action<string> _line;
    private readonly Action _flush;

    public MemoryTableProbeRunner(
        IStartupScanner scanner,
        nint moduleBase,
        IReadOnlyList<MemoryTableProbe> probes,
        Action<string> line,
        Action flush)
    {
        _scanner = scanner;
        _moduleBase = moduleBase;
        _probes = probes;
        _line = line;
        _flush = flush;
    }

    public void Install()
    {
        foreach (var probe in _probes.Where(probe => probe.Enabled))
        {
            probe.Normalize();
            string probeName = probe.TraceName;
            if (!probe.TryValidate(out string error))
            {
                _line($"[MEMTABLE-SKIP {probeName}] {error}");
                continue;
            }

            _scanner.AddMainModuleScan(probe.Pattern, result =>
            {
                if (!result.Found)
                {
                    _line($"[MEMTABLE-NOTFOUND {probeName}] pattern not found");
                    _flush();
                    return;
                }

                nint instructionAddress = _moduleBase + result.Offset;
                if (!TryResolveRipRelativeTarget(instructionAddress, probe, out nint tableBase, out string resolveError))
                {
                    _line($"[MEMTABLE-FAILED {probeName}] scan=module+0x{result.Offset:X} {resolveError}");
                    _flush();
                    return;
                }

                _line($"[MEMTABLE-FOUND {probeName}] scan=module+0x{result.Offset:X} table=0x{tableBase:X} stride=0x{probe.Stride:X} count={probe.Count} fields={probe.Fields.Count}");
                if (probe.LogRows)
                    LogRows(probe, tableBase);
                _flush();
            });
        }
    }

    private void LogRows(MemoryTableProbe probe, nint tableBase)
    {
        int logged = 0;
        int maxRows = Math.Clamp(probe.MaxRowsToLog, 0, probe.Count);
        int rowReadSize = probe.RowReadSize;

        for (int i = 0; i < probe.Count && logged < maxRows; i++)
        {
            nint rowBase = tableBase + (i * probe.Stride);
            if (!ReadableMemoryRange.IsReadable(rowBase, rowReadSize))
            {
                _line($"[MEMTABLE-ROW-SKIP {probe.TraceName}] row={i} addr=0x{rowBase:X} unreadable size=0x{rowReadSize:X}");
                break;
            }

            if (!probe.TryReadRow(rowBase, i, out var row, out string error))
            {
                _line($"[MEMTABLE-ROW-SKIP {probe.TraceName}] row={i} addr=0x{rowBase:X} {error}");
                continue;
            }

            if (!probe.LogEmptyRows && row.PresenceScore < probe.MinPresenceScore)
                continue;

            _line($"[MEMTABLE-ROW {probe.TraceName}] {row.Trace}");
            logged++;
        }

        if (logged == 0)
            _line($"[MEMTABLE-ROWS {probe.TraceName}] no rows matched presence/log filters");
    }

    private static bool TryResolveRipRelativeTarget(nint instructionAddress, MemoryTableProbe probe, out nint target, out string error)
    {
        target = 0;
        error = "";

        if (!ReadableMemoryRange.IsReadable(instructionAddress + probe.RipRelativeOffset, sizeof(int)))
        {
            error = $"cannot read RIP displacement at 0x{instructionAddress + probe.RipRelativeOffset:X}";
            return false;
        }

        int displacement = Marshal.ReadInt32(instructionAddress + probe.RipRelativeOffset);
        target = MemoryTableProbe.ResolveRipRelativeTarget(instructionAddress, displacement, probe.InstructionLength, probe.TargetAddend);

        for (int i = 0; i < probe.DereferenceCount; i++)
        {
            if (!ReadableMemoryRange.IsReadable(target, IntPtr.Size))
            {
                error = $"dereference {i + 1}/{probe.DereferenceCount} unreadable at 0x{target:X}";
                target = 0;
                return false;
            }
            target = Marshal.ReadIntPtr(target);
        }

        return true;
    }
}

internal sealed class MemoryTableProbe
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Pattern { get; set; } = "";
    public int RipRelativeOffset { get; set; } = 3;
    public int InstructionLength { get; set; } = 7;
    public int TargetAddend { get; set; } = 0;
    public int DereferenceCount { get; set; } = 0;
    public int Count { get; set; } = 0;
    public int Stride { get; set; } = 0;
    public bool LogRows { get; set; } = false;
    public bool LogEmptyRows { get; set; } = false;
    public int MaxRowsToLog { get; set; } = 16;
    public int MinPresenceScore { get; set; } = 1;
    public List<MemoryTableFieldProbe> Fields { get; set; } = new();

    public string TraceName => FormulaExpression.NormalizeIdentifierPart(string.IsNullOrWhiteSpace(Name) ? "memory_table" : Name);

    public int RowReadSize => Fields.Count == 0 ? 1 : Fields.Max(field => field.Offset + field.WidthBytes);

    public void Normalize()
    {
        Fields ??= new List<MemoryTableFieldProbe>();
        foreach (var field in Fields)
            field.Normalize();
    }

    public bool TryValidate(out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            error = "Pattern is required";
            return false;
        }
        if (InstructionLength <= 0)
        {
            error = "InstructionLength must be positive";
            return false;
        }
        if (RipRelativeOffset < 0 || RipRelativeOffset + sizeof(int) > InstructionLength + 16)
        {
            error = "RipRelativeOffset is outside a sane instruction window";
            return false;
        }
        if (DereferenceCount is < 0 or > 4)
        {
            error = "DereferenceCount must be between 0 and 4";
            return false;
        }
        if (Count <= 0)
        {
            error = "Count must be positive";
            return false;
        }
        if (Stride <= 0)
        {
            error = "Stride must be positive";
            return false;
        }
        if (Fields.Count == 0)
        {
            error = "At least one field is required";
            return false;
        }
        foreach (var field in Fields)
        {
            if (!field.TryValidate(Stride, out error))
                return false;
        }
        return true;
    }

    public bool TryReadRow(nint rowBase, int rowIndex, out MemoryTableRow row, out string error)
    {
        row = MemoryTableRow.Empty;
        error = "";

        var values = new List<MemoryTableFieldValue>(Fields.Count);
        int presenceScore = 0;
        try
        {
            foreach (var field in Fields)
            {
                long value = field.Read(rowBase);
                values.Add(new MemoryTableFieldValue(field.TraceName, value, field.FormatValue(value)));
                if (field.CountsForPresence(value))
                    presenceScore++;
            }
        }
        catch (Exception ex) when (ex is not AccessViolationException)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }

        row = new MemoryTableRow(rowIndex, rowBase, values, presenceScore);
        return true;
    }

    public static nint ResolveRipRelativeTarget(nint instructionAddress, int displacement, int instructionLength, int targetAddend = 0)
        => instructionAddress + instructionLength + displacement + targetAddend;
}

internal sealed class MemoryTableFieldProbe
{
    public string Name { get; set; } = "";
    public int Offset { get; set; } = 0;
    public string Width { get; set; } = "Byte";
    public bool Hex { get; set; } = false;
    public bool CountForPresence { get; set; } = true;
    public long? EmptyValue { get; set; } = null;

    public string TraceName { get; private set; } = "field";

    public int WidthBytes => Width.ToLowerInvariant() switch
    {
        "byte" or "uint8" => 1,
        "sbyte" or "int8" => 1,
        "word" or "uint16" => 2,
        "short" or "int16" => 2,
        "dword" or "uint32" => 4,
        "int" or "int32" => 4,
        "qword" or "uint64" => 8,
        "long" or "int64" => 8,
        _ => 0,
    };

    public void Normalize()
    {
        TraceName = FormulaExpression.NormalizeIdentifierPart(string.IsNullOrWhiteSpace(Name) ? $"offset_{Offset:X}" : Name);
        Width = string.IsNullOrWhiteSpace(Width) ? "Byte" : Width.Trim();
    }

    public bool TryValidate(int stride, out string error)
    {
        error = "";
        if (Offset < 0)
        {
            error = $"{TraceName}: Offset must be nonnegative";
            return false;
        }
        if (WidthBytes <= 0)
        {
            error = $"{TraceName}: unsupported Width '{Width}'";
            return false;
        }
        if (Offset + WidthBytes > stride)
        {
            error = $"{TraceName}: field range 0x{Offset:X}+{WidthBytes} exceeds stride 0x{stride:X}";
            return false;
        }
        return true;
    }

    public long Read(nint rowBase)
    {
        nint address = rowBase + Offset;
        return Width.ToLowerInvariant() switch
        {
            "byte" or "uint8" => Marshal.ReadByte(address),
            "sbyte" or "int8" => unchecked((sbyte)Marshal.ReadByte(address)),
            "word" or "uint16" => (ushort)Marshal.ReadInt16(address),
            "short" or "int16" => Marshal.ReadInt16(address),
            "dword" or "uint32" => unchecked((uint)Marshal.ReadInt32(address)),
            "int" or "int32" => Marshal.ReadInt32(address),
            "qword" or "uint64" => unchecked((long)(ulong)Marshal.ReadInt64(address)),
            "long" or "int64" => Marshal.ReadInt64(address),
            _ => throw new InvalidOperationException($"unsupported Width '{Width}'"),
        };
    }

    public bool CountsForPresence(long value)
    {
        if (!CountForPresence) return false;
        return EmptyValue.HasValue ? value != EmptyValue.Value : value != 0;
    }

    public string FormatValue(long value)
        => Hex ? $"0x{value:X}" : value.ToString();
}

internal sealed record MemoryTableRow(
    int Index,
    nint Address,
    IReadOnlyList<MemoryTableFieldValue> Values,
    int PresenceScore)
{
    public static MemoryTableRow Empty { get; } = new(-1, 0, Array.Empty<MemoryTableFieldValue>(), 0);

    public string Trace
        => $"row={Index} addr=0x{Address:X} present={PresenceScore} " +
           string.Join(" ", Values.Select(value => $"{value.Name}={value.FormattedValue}"));
}

internal sealed record MemoryTableFieldValue(string Name, long Value, string FormattedValue);

internal static class ReadableMemoryRange
{
    private const uint MemCommit = 0x1000;
    private const uint PageNoAccess = 0x01;
    private const uint PageGuard = 0x100;

    public static bool IsReadable(nint address, int length)
    {
        if (address == 0 || length <= 0) return false;
        if (VirtualQuery(address, out var info, (nuint)Marshal.SizeOf<MemoryBasicInformation>()) == 0)
            return false;
        if (info.State != MemCommit) return false;
        if ((info.Protect & PageNoAccess) != 0 || (info.Protect & PageGuard) != 0)
            return false;

        ulong start = unchecked((ulong)address.ToInt64());
        ulong regionStart = unchecked((ulong)info.BaseAddress.ToInt64());
        ulong regionEnd = regionStart + info.RegionSize;
        ulong end = start + (ulong)length;
        return start >= regionStart && end >= start && end <= regionEnd;
    }

    [DllImport("kernel32.dll")]
    private static extern nuint VirtualQuery(nint lpAddress, out MemoryBasicInformation lpBuffer, nuint dwLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public ushort PartitionId;
        public nuint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
}
