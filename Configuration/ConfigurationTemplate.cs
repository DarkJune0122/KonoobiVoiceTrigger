using System.IO;
using System.Text.Json.Serialization;

namespace VoiceTrigger.Configuration;

public abstract class ConfigurationTemplate
{
    [JsonIgnore] protected abstract string FilePath { get; }
    public bool Save()
    {
        try
        {
            File.WriteAllText(FilePath, Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
            return true;
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to serialize {GetType().Name} to {FilePath}!\n");
            return false;
        }
    }
    public bool Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Newtonsoft.Json.JsonConvert.PopulateObject(File.ReadAllText(FilePath), this);
            return true;
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to deserialize {GetType().Name} from {FilePath}!\n");
            return false;
        }
    }
}
