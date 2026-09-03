using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSExpressionStateResponse : VTSResponse<VTSExpressionStateResponseData>;
public sealed class VTSExpressionStateResponseData : VTSResponseData
{
    [JsonPropertyName("modelID")] public string? ModelID { get; set; }
    [JsonPropertyName("modelName")] public string? ModelName { get; set; }
    [JsonPropertyName("modelLoaded")] public bool ModelLoaded { get; set; }
    [JsonPropertyName("expressions")] public List<Expression>? Expressions { get; set; }

    public override void Reset()
    {
        base.Reset();
        ModelID = default;
        ModelName = default;
        ModelLoaded = default;
        Expressions?.Clear();
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    AppendLine(b, prefix, ModelLoaded);
    //    AppendLine(b, prefix, ModelName);
    //    AppendLine(b, prefix, ModelID);
    //    AppendList(b, prefix, Expressions, Expression.ToString).AppendLine();
    //    return base.ToString(b, prefix);
    //}

    public readonly struct Expression
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("file")] public string? File { get; init; }
        [JsonPropertyName("active")] public bool Active { get; init; }
        [JsonPropertyName("deactivateWhenKeyIsLetGo")] public bool DeactivateWhenKeyIsLetGo { get; init; }
        [JsonPropertyName("autoDeactivateAfterSeconds")] public bool AutoDeactivateAfterSeconds { get; init; }
        [JsonPropertyName("secondsRemaining")] public float SecondsRemaining { get; init; }
        [JsonPropertyName("usedInHotkeys")] public Hotkey[]? UsedInHotkeys { get; init; }
        [JsonPropertyName("parameters")] public Parameter[]? Parameters { get; init; }

        public override string ToString() => VTSPackets.ToLoggingString(this);
        //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        //public static StringBuilder ToString(Expression ex, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => ex.ToString(b, prefix);
        //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        //{
        //    VTSHelpers.AppendLine(b, prefix, Name);
        //    VTSHelpers.AppendLine(b, prefix, File);
        //    VTSHelpers.AppendLine(b, prefix, Active);
        //    VTSHelpers.AppendLine(b, prefix, DeactivateWhenKeyIsLetGo);
        //    VTSHelpers.AppendLine(b, prefix, AutoDeactivateAfterSeconds);
        //    VTSHelpers.AppendLine(b, prefix, SecondsRemaining);
        //    VTSHelpers.AppendLine(b, prefix, DeactivateWhenKeyIsLetGo);
        //    VTSHelpers.AppendList(b, prefix, UsedInHotkeys, Hotkey.ToString).AppendLine();
        //    VTSHelpers.AppendList(b, prefix, Parameters, Parameter.ToString);
        //    return b;
        //}

        public readonly struct Hotkey
        {
            [JsonPropertyName("name")] public string? Name { get; init; }
            [JsonPropertyName("id")] public string? ID { get; init; }

            public override string ToString() => VTSPackets.ToLoggingString(this);
            //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
            //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
            //public static StringBuilder ToString(Hotkey h, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => h.ToString(b, prefix);
            //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
            //{
            //    VTSHelpers.AppendLine(b, prefix, Name);
            //    VTSHelpers.Append(b, prefix, ID);
            //    return b;
            //}
        }

        public readonly struct Parameter
        {
            [JsonPropertyName("name")] public string? Name { get; init; }
            [JsonPropertyName("value")] public float Value { get; init; }

            public override string ToString() => VTSPackets.ToLoggingString(this);
            //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
            //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
            //public static StringBuilder ToString(Parameter p, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => p.ToString(b, prefix);
            //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
            //{
            //    VTSHelpers.AppendLine(b, prefix, Name);
            //    VTSHelpers.Append(b, prefix, Value);
            //    return b;
            //}
        }
    }
}