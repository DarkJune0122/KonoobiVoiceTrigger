namespace VoiceTrigger.VTS;

public sealed class VTubeStudioPlugin
{
    public const string DefaultName = "Unknown";
    public const string DefaultDeveloper = "Unknown";
    public const string DefaultIcon = "";

    public string Name => m_Name;
    public string Developer => m_Developer;
    public string Icon => m_Icon;

    readonly Lock Lock = new();
    volatile string m_Name = DefaultName;
    volatile string m_Developer = DefaultDeveloper;
    volatile string m_Icon = DefaultIcon;

    public void Set(string name, string developer, string icon)
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
