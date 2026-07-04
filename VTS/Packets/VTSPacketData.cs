using System.Runtime.CompilerServices;
using System.Text;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData
{
    protected const string DefaultPrefix = "";
    protected const bool DefaultNewLine = false;
    public override string ToString() => ToString(b: new()).ToString();
    public string ToString(string prefix = DefaultPrefix, bool newLine = DefaultNewLine) => ToString(new(), prefix, newLine).ToString();
    public virtual StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        b.Append(GetType().FullName);
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendLineBase(StringBuilder b, string prefix, string name)
    {
        return AppendBase(b, prefix, name, true);
    }
    protected static StringBuilder AppendBase(StringBuilder b, string prefix, string name, bool newLine = false)
    {
        b.Append(prefix);
        b.Append(name);
        b.Append(": ");
        if (newLine) b.AppendLine();
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendLine<T>(StringBuilder b, string prefix, T? value,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return Append(b, prefix, value, true, name);
    }
    protected static StringBuilder Append<T>(StringBuilder b, string prefix, T? value,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
        AppendBase(b, prefix, name, false);
        b.Append(value?.ToString());
        return b;
    }
    protected static StringBuilder Append<T>(StringBuilder b, string prefix, T? value, bool newLine,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
        AppendBase(b, prefix, name, false);
        b.Append(value?.ToString());
        if (newLine) b.AppendLine();
        return b;
    }

    protected static StringBuilder AppendData<T>(StringBuilder b, string prefix, T? data, bool newLine)
    {
        string source = data?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(source)) return b;

        string[] lines = source.Split(["\n", "\r", "\n\r"], StringSplitOptions.None);
        int lineCount = lines.Length;
        if (lineCount == 0) return b;

        b.Append(prefix); b.AppendLine("{");
        for (int i = 0; i < lineCount; i++)
        {
            b.Append(prefix); b.Append("    ");
            b.AppendLine(lines[i]);
        }
        b.Append(prefix); b.Append('}');
        if (newLine) b.AppendLine();
        return b;
    }
}
