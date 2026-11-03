using System.Globalization;
using NsunsTrainer.Models;

namespace NsunsTrainer.Scanning;

/// <summary>Conversion between text, raw bits and display for each value type.</summary>
public static class ValueCodec
{
    public static int SizeOf(ScanValueType type) => type switch
    {
        ScanValueType.Int32 or ScanValueType.Float => 4,
        _ => 8
    };

    public static bool TryParse(ScanValueType type, string text, out ulong raw)
    {
        raw = 0;
        text = (text ?? "").Trim().Replace(',', '.');
        var culture = CultureInfo.InvariantCulture;

        switch (type)
        {
            case ScanValueType.Int32:
                if (!int.TryParse(text, NumberStyles.Integer, culture, out var i)) return false;
                raw = (uint)i;
                return true;

            case ScanValueType.Float:
                if (!float.TryParse(text, NumberStyles.Float, culture, out var f)) return false;
                raw = BitConverter.SingleToUInt32Bits(f);
                return true;

            case ScanValueType.Int64:
                if (!long.TryParse(text, NumberStyles.Integer, culture, out var l)) return false;
                raw = (ulong)l;
                return true;

            case ScanValueType.Double:
                if (!double.TryParse(text, NumberStyles.Float, culture, out var d)) return false;
                raw = BitConverter.DoubleToUInt64Bits(d);
                return true;
        }

        return false;
    }

    public static string Format(ScanValueType type, ulong raw)
    {
        var culture = CultureInfo.InvariantCulture;
        return type switch
        {
            ScanValueType.Int32 => ((int)(uint)raw).ToString(culture),
            ScanValueType.Float => BitConverter.UInt32BitsToSingle((uint)raw).ToString("G7", culture),
            ScanValueType.Int64 => ((long)raw).ToString(culture),
            _ => BitConverter.UInt64BitsToDouble(raw).ToString("G15", culture)
        };
    }

    public static ulong ReadRaw(byte[] data, int offset, int size) =>
        size == 4 ? BitConverter.ToUInt32(data, offset) : BitConverter.ToUInt64(data, offset);
}