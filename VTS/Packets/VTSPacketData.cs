using System.Runtime.CompilerServices;
using System.Text;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData : IVTSFormattable
{
    protected const string DefaultPrefix = VTSHelpers.DefaultPrefix;
    protected const string DefaultIndentation = VTSHelpers.DefaultIndentation;

    public override string ToString() => ToString(b: new()).ToString();
    public string ToString(string prefix) => ToString(b: new(), prefix).ToString();
    public virtual StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        return b.Append(GetType().FullName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder Append<T>(StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return VTSHelpers.Append(b, prefix, value, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendLine<T>(StringBuilder b, string prefix, T? value, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        return VTSHelpers.AppendLine(b, prefix, value, name);
    }

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendList<T>(StringBuilder b, string prefix, IList<T>? list, [CallerArgumentExpression(nameof(list))] string name = "")
    {
        return VTSHelpers.AppendList(b, prefix, list, name);
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static StringBuilder AppendData<T>(StringBuilder b, string prefix, T? data, [CallerArgumentExpression(nameof(data))] string name = "")
    {
        return VTSHelpers.AppendData(b, prefix, data, name);
    }
}
