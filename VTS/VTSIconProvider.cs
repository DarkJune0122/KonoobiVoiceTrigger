using System.IO;
using VoiceTrigger.Logging;

namespace VoiceTrigger.VTS;

public static class VTSIconProvider
{
    static readonly string IconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
    public static readonly string IconBase64;
    static VTSIconProvider()
    {
        // Reads app icon as Base64 to use as plugin preview.
        string image = string.Empty;
        try
        {
            if (File.Exists(IconPath))
            {
                byte[] bytes = File.ReadAllBytes(IconPath);
                image = Convert.ToBase64String(bytes);
            }
            else
            {
                $"[{nameof(VTSIconProvider)}] 'icon.png' is missing in a root folder. Plugin will have no icon.".Out(ConsoleColor.Yellow);
            }
        }
        catch (Exception ex)
        {
            ex.Out($"[{nameof(VTSIconProvider)}] Cannot load 'icon.png' for a plugin! Plugin will have no icon.\n");
        }
        IconBase64 = image;
    }
}
