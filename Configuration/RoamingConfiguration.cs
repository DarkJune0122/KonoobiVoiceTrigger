using System.Text.Json.Serialization;
using VoiceTrigger.VTS;

namespace VoiceTrigger.Configuration;

public sealed class RoamingConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => RoamingConfigurationFilePath;

    [JsonInclude] public ushort VTubeStudioDiscoveryPort { get; set; } = VTSDiscoveryService.KnownVTubeStudioDiscoveryPort;
#if CONSOLE
    [JsonInclude] public bool LogNetworkPackets { get; set; } = true;
#else
    [JsonInclude] public bool LogNetworkPackets { get; set; } = false;
#endif
    [JsonInclude] public double ActivationPower
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 1;
    [JsonInclude] public double NormalActivationDuration
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 7;
    [JsonInclude] public double TriggeredReleaseDuration
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 60;
    [JsonInclude] public double NormalActivationJump
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 0.05;
    [JsonInclude] public double TriggeredActivationJump
    {
        get => Interlocked.CompareExchange(ref field, 0, 1);
        set => Interlocked.Exchange(ref field, value);
    } = 0.02;
    [JsonInclude] public int SelectedResistanceIndex { get; set; } = -1;
    [JsonInclude] public ModelHotkey? SelectedHotkey
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    }
    [JsonInclude] public ModelExpression? SelectedExpression
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    }
    [JsonInclude] public bool EnableFreezing { get; set; } = true;
    [JsonInclude] public double FreezeDuration
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 90;
    [JsonInclude] public bool InstantUnfreezeOnManualNormal { get; set; } = true;
    //[JsonInclude] public bool AllowUnfreezeWhileNormal { get; set; } = true;
    //[JsonInclude] public double NormalUnfreezeDelay { get; set; } = 15;
    //[JsonInclude] public bool AllowUnfreezeWhileTriggered { get; set; } = true;
    //[JsonInclude] public double TriggeredUnfreezeDelay { get; set; } = 30;
    [JsonInclude] public double AuraFrameRate
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 90;
    [JsonInclude] public double AuraFrequency
    {
        get => InterlockedHelpers.Read(ref field);
        set => Interlocked.Exchange(ref field, value);
    } = 0.5;
}
