using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace VoiceTrigger.Extensions;

public sealed class LocalizationResourceManager : INotifyPropertyChanged
{
    private const string IndexerName = "Item";
    private const string IndexerArrayName = "Item[]";

    public static LocalizationResourceManager Instance { get; } = new LocalizationResourceManager();

    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string text] => GetValue(text);
    public CultureInfo CurrentCulture
    {
        get => currentCulture;
        set
        {
            currentCulture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerArrayName));
        }
    }

    ResourceManager resourceManager;
    CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;


    public LocalizationResourceManager()
    {
        resourceManager = Properties.Resources.ResourceManager;

        // Initialize with the current culture by default.
        currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    public void Init(ResourceManager resource) => resourceManager = resource;
    public void Init(ResourceManager resource, CultureInfo initialCulture)
    {
        CurrentCulture = initialCulture;
        Init(resource);
    }

    public string GetValue(string text, string? fallback = null)
    {
        if (resourceManager is null)
        {
            return text;
        }

        var value = resourceManager.GetString(text, CurrentCulture);
        if (value is null)
        {
            if (fallback is null) return $"{nameof(text)}: {text} not found";
            else return fallback;
        }
        else
        {
            value = value.Replace("\\n", "\n");
        }

        return value;
    }
}
