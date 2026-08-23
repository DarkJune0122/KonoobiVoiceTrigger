using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace VoiceTrigger.Services;

public static class LogService
{
    [MemberNotNullWhen(true, nameof(LogStream))]
    [MemberNotNullWhen(true, nameof(LogWriter))]
    public static bool IsInitialized { get; private set; }

    static readonly string LogDirectory = Path.Combine(ConfigurationService.RoamingDirectory, "Logs");
    static readonly string LogFilePath = Path.Combine(LogDirectory, "now.log");
    static readonly string LogFileBackupFormat = Path.Combine(LogDirectory, "{0}.log");
    static FileStream? LogStream;
    static StreamWriter? LogWriter;

    public static void Initialize()
    {
        if (!IsInitialized)
        {
            Directory.CreateDirectory(LogDirectory);
            LogStream = new FileStream(LogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            LogWriter = new StreamWriter(LogStream);

            IsInitialized = true;
            $"{nameof(LogService)} initialized!".Out();
        }
    }
    public static void Log(string? content)
    {
        if (IsInitialized)
        {
            content ??= string.Empty;
            string[] lines = content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                LogWriter.Write('[');
                LogWriter.Write(DateTime.Now.ToString("HH:mm:ss.fff"));
                LogWriter.Write(']');
                LogWriter.Write(' ');
                LogWriter.WriteLine(line);
            }

            LogWriter.Flush();
        }
    }
    public static void Terminate()
    {
        if (IsInitialized)
        {
            $"Terminating {nameof(LogService)}...".Out();
            LogStream.Close();
            for (int i = 0; i < 1024; i++)
            {
                string file = LogFileBackupFormat.Replace("{0}", $"{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}{(i == 0 ? "" : $" ({i})")}");
                if (File.Exists(file)) continue;
                try
                {
                    File.Move(LogFilePath, file);
                }
                catch (Exception ex) { ex.Out("Failed to move current log file!\n"); }
                break;
            }

            IsInitialized = false;
        }
    }
}
