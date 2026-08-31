namespace VoiceTrigger.VTS;

public sealed class VSTPlugin(ITokenStorage storage, string? name = null, string? developer = null, string? icon = null)
{
    public const string? DefaultName = "Unknown";
    public const string? DefaultDeveloper = "Unknown";
    public const string? DefaultIcon = null;

    public string? Name => m_Name;
    public string? Developer => m_Developer;
    public string? Icon => m_Icon;
    public ITokenStorage TokenStorage { get; } = storage;

    readonly Lock Lock = new();
    volatile string? m_Name = name ?? DefaultName;
    volatile string? m_Developer = developer ?? DefaultDeveloper;
    volatile string? m_Icon = icon ?? DefaultIcon;

    public void Set(string? name, string? developer, string? icon = null)
    {
        name ??= DefaultName;
        developer ??= DefaultDeveloper;
        icon ??= DefaultIcon;

        lock (Lock)
        {
            m_Name = name;
            m_Developer = developer;
            m_Icon = icon;
        }
    }
}
