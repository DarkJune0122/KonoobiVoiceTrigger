using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSExpressionStateResponse : VTSResponse<VTSExpressionStateResponseData>;
public sealed class VTSExpressionStateResponseData : VTSResponseData
{
    [JsonPropertyName("modelID")] public required string? ModelID { get; init; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; init; }
    [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; init; }
    [JsonPropertyName("expressions")] public required Expression[]? Expressions { get; init; }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        AppendLine(b, prefix, ModelLoaded);
        AppendLine(b, prefix, ModelName);
        AppendLine(b, prefix, ModelID);
        AppendList(b, prefix, Expressions, Expression.ToString).AppendLine();
        return base.ToString(b, prefix);
    }

    public readonly struct Expression
    {
        [JsonPropertyName("name")] public required string? Name { get; init; }
        [JsonPropertyName("file")] public required string? File { get; init; }
        [JsonPropertyName("active")] public required bool Active { get; init; }
        [JsonPropertyName("deactivateWhenKeyIsLetGo")] public required bool DeactivateWhenKeyIsLetGo { get; init; }
        [JsonPropertyName("autoDeactivateAfterSeconds")] public required bool AutoDeactivateAfterSeconds { get; init; }
        [JsonPropertyName("secondsRemaining")] public required float SecondsRemaining { get; init; }
        [JsonPropertyName("usedInHotkeys")] public required Hotkey[]? UsedInHotkeys { get; init; }
        [JsonPropertyName("parameters")] public required Parameter[]? Parameters { get; init; }

        public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        public static StringBuilder ToString(Expression ex, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => ex.ToString(b, prefix);
        public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        {
            VTSHelpers.AppendLine(b, prefix, Name);
            VTSHelpers.AppendLine(b, prefix, File);
            VTSHelpers.AppendLine(b, prefix, Active);
            VTSHelpers.AppendLine(b, prefix, DeactivateWhenKeyIsLetGo);
            VTSHelpers.AppendLine(b, prefix, AutoDeactivateAfterSeconds);
            VTSHelpers.AppendLine(b, prefix, SecondsRemaining);
            VTSHelpers.AppendLine(b, prefix, DeactivateWhenKeyIsLetGo);
            VTSHelpers.AppendList(b, prefix, UsedInHotkeys, Hotkey.ToString).AppendLine();
            VTSHelpers.AppendList(b, prefix, Parameters, Parameter.ToString);
            return b;
        }

        public readonly struct Hotkey
        {
            [JsonPropertyName("name")] public required string? Name { get; init; }
            [JsonPropertyName("id")] public required string? ID { get; init; }

            public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
            public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
            public static StringBuilder ToString(Hotkey h, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => h.ToString(b, prefix);
            public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
            {
                VTSHelpers.AppendLine(b, prefix, Name);
                VTSHelpers.Append(b, prefix, ID);
                return b;
            }
        }

        public readonly struct Parameter
        {
            [JsonPropertyName("name")] public required string? Name { get; init; }
            [JsonPropertyName("value")] public required float Value { get; init; }

            public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
            public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
            public static StringBuilder ToString(Parameter p, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => p.ToString(b, prefix);
            public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
            {
                VTSHelpers.AppendLine(b, prefix, Name);
                VTSHelpers.Append(b, prefix, Value);
                return b;
            }
        }
    }
}