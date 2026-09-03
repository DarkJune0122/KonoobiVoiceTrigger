using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSECModelLoaded : VTSEventConfig
{
    [JsonPropertyName("modelID")] public List<string>? ModelIDs { get; set; }

    public override void Reset()
    {
        base.Reset();
        ModelIDs?.Clear();
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    AppendList(b, prefix, ModelIDs, VTSHelpers.StringWriter);
    //    return b;
    //}
}