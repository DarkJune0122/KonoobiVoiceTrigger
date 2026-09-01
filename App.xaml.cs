using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Windows.Threading;
using VoiceTrigger.Audio;
using VoiceTrigger.Logging;
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
            $"Another app instance detected. This one will shutdown.".Out();
            SignalSingleton();
            Shutdown();
            return;
        }

        try
        {
            LoggerService.Instance.Initialize();
            Initialize();
            VTSDiscoveryService.Instance.Initialize();
            //VTSService.Instance.Initialize();
            SingletonSource = new();
            _ = StartServerPipeAsync(SingletonSource.Token);

            AudioCaptureService.Instance.Initialize();
            //VTSRestartTimer.Tick += MakeRestartAttempt;
            //VTSRestartTimer.Interval = TimeSpan.FromSeconds(1);
            //VTSRestartTimer.Start();
        }
        catch (Exception ex) { ex.Out("Exception during app startup! Shutting down..."); Shutdown(); }
    }

    enum ExitStage : byte
    {
        Normal,
        Terminating,
        Terminated,
    }

    private readonly Lock InterruptLock = new();
    private ExitStage InterruptStage;

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        lock (InterruptLock)
        {
            if (InterruptStage == ExitStage.Terminated)
            {
                return; // Allows closing.
            }
            else if (InterruptStage == ExitStage.Terminating)
            {
                e.Cancel = true; // Cancels all termination attempts until application terminates from the fist one.
                return;
            }
            // Starts termination sequence.
            InterruptStage = ExitStage.Terminating;
            e.Cancel = true;
        }

        // Spawns a termination process.
        // By the end of it application will close by itself.
        ServiceTermination();
    }

    private void MakeRestartAttempt(object? sender, EventArgs e)
    {
        VTubeStudio.Instance.Status.Out("Status: ", ConsoleColor.Gray);
        if (VTubeStudio.Instance.Status == VTSStatus.Offline)
            VTubeStudio.Instance.Start();
    }

    async void ServiceTermination()
    {
        // Note: Doesn't work. Application closes before termination.
        // TODO: Fix termination sequence, either by providing async methods, or by properly releasing LoggerService from a Main Thread.
        try
        {
            SingletonSource?.Cancel();
            //VTubeStudio.Instance.Stop();
            //VTSService.Instance.Terminate();
            AudioCaptureService.Instance.Terminate();
            VTSDiscoveryService.Instance.Terminate();
            Terminate();
            await LoggerService.Instance.Terminate();
        }
        catch (Exception ex) { ex.Out($"Exception while terminating the entire app!\n"); }
        lock (InterruptLock) InterruptStage = ExitStage.Terminated;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
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
            $"Signaled an exising app to open.".Out();
        }
        catch (Exception ex)
        {
            try
            {
                ex.Out("Failed to signal the existing app!\n");
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