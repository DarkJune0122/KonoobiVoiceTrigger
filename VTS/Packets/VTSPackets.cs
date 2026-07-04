using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public static class VTSPackets
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    };
}
