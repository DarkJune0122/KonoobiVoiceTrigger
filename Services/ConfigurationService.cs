using System.IO;
using VoiceTrigger.Configuration;

namespace VoiceTrigger.Services;

public static class ConfigurationService
{
    public static readonly string RoamingDirectory;
    public static readonly string LocalDirectory;
    public static readonly string CommonDirectory;

    public static readonly string RoamingConfigurationFilePath;
    public static readonly string LocalConfigurationFilePath;
    public static readonly string CommonConfigurationFilePath;

    public static RoamingConfiguration Roaming { get; } = new();
    public static LocalConfiguration Local { get; } = new();
    public static CommonConfiguration Common { get; } = new();

    static ConfigurationService()
    {
        // Roaming.
        {
            var special = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            RoamingDirectory = Path.Combine(special, "Sandcorp", nameof(VoiceTrigger));
            try { Directory.CreateDirectory(RoamingDirectory); } catch (Exception ex) { ex.Out(); }
            RoamingConfigurationFilePath = Path.Combine(RoamingDirectory, "config.json");
        }

        // Local.
        {
            var special = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LocalDirectory = Path.Combine(special, "Sandcorp", nameof(VoiceTrigger));
            try { Directory.CreateDirectory(LocalDirectory); } catch (Exception ex) { ex.Out(); }
            LocalConfigurationFilePath = Path.Combine(LocalDirectory, "config.json");
        }

        // Common.
        {
            var special = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            CommonDirectory = Path.Combine(special, "Sandcorp", nameof(VoiceTrigger));
            try { Directory.CreateDirectory(CommonDirectory); } catch (Exception ex) { ex.Out(); }
            CommonConfigurationFilePath = Path.Combine(CommonDirectory, "config.json");
        }
    }





    /// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
    /// .
    /// .                                               Public Methods
    /// .
    /// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
    public static void Initialize() => Load();
    public static void Terminate() => Save();
    public static bool Save() => Roaming.Save() && Local.Save() && Common.Save();
    /// <remarks>
    /// Loading should only appear once - at the start of the app.
    /// </remarks>
    public static bool Load() => Roaming.Load() && Local.Load() && Common.Load();
}