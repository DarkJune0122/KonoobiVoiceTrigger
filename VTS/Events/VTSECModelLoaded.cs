using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSECModelLoaded : VTSEventConfig
{
    [JsonPropertyName("modelID")] public string[]? ModelIDs { get; set; } = [];

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        AppendList(b, prefix, ModelIDs, VTSHelpers.StringWriter);
        return b;
    }
}