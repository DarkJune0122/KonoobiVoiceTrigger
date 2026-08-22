using System.Windows.Data;
using System.Windows.Markup;

namespace VoiceTrigger.Extensions;

public class TranslateExtension(string key) : MarkupExtension
{
    public string Key { get; set; } = key;
    public string Context { get; set; } = string.Empty;
    public TranslateExtension() : this(string.Empty) { }
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        string keyToUse = Key;
        if (!string.IsNullOrWhiteSpace(Context))
        {
            keyToUse = $"{Context}/{Key}";
        }

        var binding = new Binding($"[{keyToUse}]")
        {
            Mode = BindingMode.OneWay,
            Source = LocalizationResourceManager.Instance,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
