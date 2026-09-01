using System.IO;
using System.Text.Json.Serialization;
using VoiceTrigger.Logging;

namespace VoiceTrigger.Configuration;

public abstract class ConfigurationTemplate
{
    [JsonIgnore] protected abstract string FilePath { get; }
    public bool Save()
    {
        try
        {
            $"Saving {GetType().Name}...".Out();
            string result = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(FilePath, result);
            $"Saved! {GetType().Name}!".Out();
            return true;
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to save {GetType().Name}!\n");
            return false;
        }
    }
    public bool Load()
    {
        try
        {
            $"Loading {GetType().Name}...".Out();
            if (File.Exists(FilePath))
                Newtonsoft.Json.JsonConvert.PopulateObject(File.ReadAllText(FilePath), this);
            $"Loaded! {GetType().Name}!".Out();
            return true;
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to load {GetType().Name}!\n");
            return false;
        }
    }
}
