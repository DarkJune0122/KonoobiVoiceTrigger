using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Windows.Threading;
using VoiceTrigger.Services;
using VoiceTrigger.VTS;

namespace VoiceTrigger;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    const string MutexName = "Sandcorp.VoiceTrigger.Singleton.Mutex";
    const string PipeName = "Sandcorp.VoiceTrigger.Singleton.Pipe";
    const string ShowWindowSignal = "show-window";
    static readonly DispatcherTimer VTSRestartTimer = new();
    static CancellationTokenSource? SingletonSource;
    static Mutex? SingletonMutex;

    public static readonly string ResourcesFolder = Path.Combine(AppContext.BaseDirectory, "Resources");
    public static readonly string LocalAppDataFolder
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sandcorp", "VoiceTrigger");

    public static RootViewModel RootViewModel => (RootViewModel)Current.Resources[nameof(VoiceTrigger.RootViewModel)];
    public static Color IndicatorKobiColor => (Color)Current.Resources[nameof(IndicatorKobiColor)];
    public static Brush IndicatorKobiBrush => (Brush)Current.Resources[nameof(IndicatorKobiBrush)];
    public static Color IndicatorIBOKColor => (Color)Current.Resources[nameof(IndicatorIBOKColor)];
    public static Brush IndicatorIBOKBrush => (Brush)Current.Resources[nameof(IndicatorIBOKBrush)];
    public static Color IndicatorFrozenColor => (Color)Current.Resources[nameof(IndicatorFrozenColor)];
    public static Brush IndicatorFrozenBrush => (Brush)Current.Resources[nameof(IndicatorFrozenBrush)];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try { Directory.CreateDirectory(ResourcesFolder); } catch { }
        try { Directory.CreateDirectory(LocalAppDataFolder); } catch { }

        SingletonMutex = new(true, MutexName, out bool isNew);
        if (!isNew)
        {
            SignalSingleton();
            Shutdown();
            return;
        }

        LogService.Initialize();
        SingletonSource = new();
        _ = StartServerPipeAsync(SingletonSource.Token);

        ConfigurationService.Initialize();
        VTSRestartTimer.Tick += MakeRestartAttempt;
        VTSRestartTimer.Interval = TimeSpan.FromSeconds(1);
        VTSRestartTimer.Start();
    }

    private void MakeRestartAttempt(object? sender, EventArgs e)
    {
        VTubeStudio.Instance.Status.Out("Status: ", ConsoleColor.Gray);
        if (VTubeStudio.Instance.Status == VTSStatus.Offline)
            VTubeStudio.Instance.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        SingletonSource?.Cancel();
        VTubeStudio.Instance.Stop();
        ConfigurationService.Terminate();
        LogService.Terminate();
        SingletonMutex?.Dispose();
    }

    private static void SignalSingleton()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(TimeSpan.FromSeconds(2));
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(ShowWindowSignal);
        }
        catch (Exception ex)
        {
            try
            {
                ex.Out("Failed to signal an existing singleton!");
            }
            catch { }
        }
    }

    private static async Task StartServerPipeAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                $"Trying to start Singleton server...".Out();
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                await server.WaitForConnectionAsync(token);
                using var reader = new StreamReader(server);
                string? line = await reader.ReadLineAsync(token);
                if (!string.IsNullOrEmpty(line))
                {
                    if (line != ShowWindowSignal)
                    {
                        $"Unknown signal received! Signal: {line}".Out(ConsoleColor.Yellow);
                        continue;
                    }

                    Current.Dispatcher.Invoke(() =>
                    {
                        var window = Current.MainWindow;
                        if (window is not null)
                        {
                            if (window.WindowState == WindowState.Minimized)
                                window.WindowState = WindowState.Normal;
                            window.Show();
                            window.Activate();
                        }
                        else
                        {
                            "Main Window is null! Activation signal will be ignored.".Out(ConsoleColor.Yellow);
                        }
                    });
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ex.Out("Failed to start a singleton server!");
                try
                {
                    await Task.Delay(10, token);
                }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}