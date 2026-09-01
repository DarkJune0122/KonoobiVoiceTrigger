//using System.Runtime.CompilerServices;
//using System.Text;

//namespace VoiceTrigger.VTS.Packets;

//public static class VTSHelpers
//{
//    public const string DefaultPrefix = "";
//    public const string DefaultIndentation = "    ";
//    public delegate StringBuilder ListWriter<T>(T item, StringBuilder b, string prefix = DefaultPrefix);

//    static StringBuilder AppendBase(
//        StringBuilder b, string prefix, string name)
//    {
//        b.Append(prefix);
//        b.Append(name);
//        b.Append(": ");
//        return b;
//    }

//    public static StringBuilder Append<T>(
//        StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
//    {
//        return AppendBase(b, prefix, name).Append(value?.ToString());
//    }
//    public static StringBuilder AppendLine<T>(
//        StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
//    {
//        return Append(b, prefix, value, name).AppendLine();
//    }

//    public static StringBuilder AppendData<T>(
//        StringBuilder b, string prefix, T? data, [CallerArgumentExpression(nameof(data))] string name = "")
//    {
//        string source = data?.ToString() ?? string.Empty;
//        if (string.IsNullOrEmpty(source))
//            return AppendBase(b, prefix, name);

//        string[] lines = source.Split('\n', StringSplitOptions.None);
//        if (lines.Length == 0)
//            return AppendBase(b, prefix, name);

//        AppendBase(b, prefix, name).AppendLine();
//        b.Append(prefix).AppendLine("{");
//        foreach (string line in lines)
//        {
//            b.Append(prefix).Append(DefaultIndentation);
//            b.AppendLine(line);
//        }
//        b.Append(prefix).Append('}');
//        return b;
//    }

//    public static StringBuilder AppendList<T>(
//        StringBuilder b, string prefix, IList<T>? list, ListWriter<T> writer, [CallerArgumentExpression(nameof(list))] string name = "")
//    {
//        if (list is null || list.Count == 0)
//            return AppendBase(b, prefix, name);

//        AppendBase(b, prefix, name).AppendLine();
//        b.Append(prefix).AppendLine("{");
//        foreach (T item in list)
//        {
//            b.Append(DefaultIndentation).Append(prefix).AppendLine("{");
//            b.Append(prefix);
//            writer(item, b, DefaultIndentation + DefaultIndentation);
//            b.Append(DefaultIndentation).AppendLine(prefix);
//            b.Append(DefaultIndentation).AppendLine("}");
//        }
//        b.Append(prefix).Append('}');
//        return b;
//    }

//    public static StringBuilder StringWriter(string line, StringBuilder b, string prefix = DefaultPrefix)
//    {
//        b.Append(line);
//        return b;
//    }
//}