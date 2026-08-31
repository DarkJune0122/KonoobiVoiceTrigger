using VTS.Core;

namespace VoiceTrigger.VTS;

public sealed class CustomVTSLogger : IVTSLogger
{
    public static readonly CustomVTSLogger Instance = new();
    const string Prefix = "[VTS]";
    public void Log(string message) => message.Out(Prefix);
    public void LogError(string error) => error.Out(Prefix, ConsoleColor.Red);
    public void LogError(Exception error) => error.Out(Prefix);
    public void LogWarning(string warning) => warning.Out(Prefix, ConsoleColor.Yellow);
}
