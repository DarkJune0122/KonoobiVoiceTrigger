using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace VoiceTrigger.VTS
{
    /// <summary>
    /// Implementation extending <see cref="DefaultJsonTypeInfoResolver"/>, to skip any fields marked with <see langword="required"/> keyword.
    /// </summary>
    /// <remarks>
    /// Provided, as for manual packet construction, the <see langword="required"/> keyword is quite useful.
    /// But it should be skipped during deserialization, because it throws <see cref="JsonException"/> if VTS returns an API error message.
    /// </remarks>
    public sealed class VTSJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public static readonly VTSJsonTypeInfoResolver Instance = new();
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo info = base.GetTypeInfo(type, options);
            foreach (JsonPropertyInfo property in info.Properties)
            {
                // Skip only if there is no JsonRequiredAttribute defined.
                if (!property.PropertyType.IsDefined(typeof(JsonRequiredAttribute), false))
                    property.IsRequired = false;
            }
            return info;
        }
    }
}
