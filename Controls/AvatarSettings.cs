using System.Text.Json.Serialization;
using System.Windows.Media;

namespace VoiceTrigger.Controls;

public sealed class AvatarSettings
{
    public struct Option
    {
        [JsonInclude] public double ActivationPercent;
        [JsonInclude] public string FileName;
        [JsonInclude] public string AuraFileName;
        [JsonIgnore] public ImageSource? ImageSource;
        [JsonIgnore] public ImageSource? AuraImageSource;

        public static Option MakeFallback(ImageSource? fallback) => new Option
        {
            ActivationPercent = 0.0,
            FileName = string.Empty,
            AuraFileName = string.Empty,
            ImageSource = fallback,
            AuraImageSource = null
        };
    }

    [JsonInclude] public long Version = 1;
    [JsonInclude] public Option[]? NormalStates;
    [JsonInclude] public Option[]? ActiveStates;
    [JsonInclude] public Option[]? TriggeredNormalStates;
    [JsonInclude] public Option[]? TriggeredActiveStates;

    public static readonly AvatarSettings Default = new()
    {
        NormalStates = [
            new() { ActivationPercent = 0.0, FileName = "kocalm.png" }
        ],
        ActiveStates = [
            new() { ActivationPercent = 0.0, FileName = "koyap.png" },
            new() { ActivationPercent = 0.35, FileName = "kotalk.png" },
            new() { ActivationPercent = 0.74, FileName = "komald.png" }
        ],
        TriggeredNormalStates = [
            new() { ActivationPercent = 0.0, FileName = "imad.png" }
        ],
        TriggeredActiveStates = [
            new() { ActivationPercent = 0.0, FileName = "imald.png", AuraFileName = "imald-aura.png" }
        ]
    };
}
