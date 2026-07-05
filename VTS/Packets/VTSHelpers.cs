using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text;

namespace VoiceTrigger.VTS.Packets;

public interface IVTSFormattable
{
    public StringBuilder ToString(IndentedTextWriter w, string prefix = VTSHelpers.DefaultPrefix);
}

public static class VTSHelpers
{
    public const string DefaultPrefix = "";
    public const string DefaultIndentation = "    ";

    static StringBuilder AppendBase(StringBuilder b, string prefix, string name)
    {
        b.Append(prefix);
        b.Append(name);
        b.Append(": ");
        return b;
    }

    public static StringBuilder Append<T>(StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return AppendBase(b, prefix, name).Append(value?.ToString());
    }
    public static StringBuilder AppendLine<T>(StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return Append(b, prefix, value, name).AppendLine();
    }

    /*public static StringBuilder AppendArray<T>(StringBuilder b, string prefix, T?[]? array, [CallerArgumentExpression(nameof(array))] string name = "")
    {
        if (!typeof(T).IsValueType || !typeof(IVTSFormattable).IsAssignableFrom(typeof(T)))
        {
            return AppendList(b, prefix, array, name);
        }

        if (array is null || array.Length == 0)
            return AppendBase(b, prefix, name);
    }
    public static StringBuilder AppendList<T>(StringBuilder b, string prefix, IList<T>? list, [CallerArgumentExpression(nameof(list))] string name = "")
    {
        if (list is null || list.Count == 0)
            return AppendBase(b, prefix, name);

        AppendBase(b, prefix, name).AppendLine();
        b.Append(prefix).AppendLine("{");
        foreach (T item in list)
        {
            b.Append(prefix);
            if (item is null)
            {
                b.AppendLine();
                continue;
            }
            if (item is IVTSFormattable f)
            {
                f.ToString(b, DefaultIndentation).AppendLine();
            }
            else
            {
                b.AppendLine(item.ToString());
            }
        }
        b.Append(prefix).Append('}');
        return b;
    }*/

    public static StringBuilder AppendData<T>(StringBuilder b, string prefix, T? data, [CallerArgumentExpression(nameof(data))] string name = "")
    {
        string source = data?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(source))
            return AppendBase(b, prefix, name);

        string[] lines = source.Split('\n', StringSplitOptions.None);
        if (lines.Length == 0)
            return AppendBase(b, prefix, name);

        AppendBase(b, prefix, name).AppendLine();
        b.Append(prefix).AppendLine("{");
        foreach (string line in lines)
        {
            b.Append(prefix).Append(DefaultIndentation);
            b.AppendLine(line);
        }
        b.Append(prefix).Append('}');
        return b;
    }
}