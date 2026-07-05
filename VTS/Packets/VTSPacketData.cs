using System.Runtime.CompilerServices;
using System.Text;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData
{
    protected const string DefaultPrefix = VTSHelpers.DefaultPrefix;
    protected const string DefaultIndentation = VTSHelpers.DefaultIndentation;

    public override string ToString() => ToString(DefaultPrefix).ToString();
    public string ToString(string prefix) => ToString(b: new(), prefix).ToString();
    public abstract StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder Append<T>(
        StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return VTSHelpers.Append(b, prefix, value, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendLine<T>(
        StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return VTSHelpers.AppendLine(b, prefix, value, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendData<T>(
        StringBuilder b, string prefix, T? data, [CallerArgumentExpression(nameof(data))] string name = "")
    {
        return VTSHelpers.AppendData(b, prefix, data, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendList<T>(
        StringBuilder b, string prefix, IList<T>? list, VTSHelpers.ListWriter<T> writer, [CallerArgumentExpression(nameof(list))] string name = "")
    {
        return VTSHelpers.AppendList(b, prefix, list, writer, name);
    }
}
