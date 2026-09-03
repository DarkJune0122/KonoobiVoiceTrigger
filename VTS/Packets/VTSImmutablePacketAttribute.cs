namespace VoiceTrigger.VTS.Packets;

/// <summary>
/// Marks an attribute which has a static instance field and never changes under normal circumstances.
/// </summary>
/// <remarks>
/// Used for avoiding pooling and in some cases - using JsonSerializer cache instead.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class VTSImmutablePacketAttribute : Attribute;
