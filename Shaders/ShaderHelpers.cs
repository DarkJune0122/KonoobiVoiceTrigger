using System.IO;

namespace VoiceTrigger.Shaders;

public static class ShaderHelpers
{
    static readonly string BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shaders");
    public static Uri MakeShaderUri(string shaderFileName)
    {
        string path;
        if (shaderFileName.EndsWith(".cso", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(BasePath, shaderFileName);
        }
        else
        {
            path = Path.Combine(BasePath, shaderFileName + ".cso");
        }
        return new Uri(path, UriKind.Absolute);
    }
}
