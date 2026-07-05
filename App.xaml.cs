using System.Diagnostics;
using System.IO;
using VoiceTrigger.VTS;

namespace VoiceTrigger;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly string LocalAppDataFolder
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sandcorp", "Voice Trigger");
    static App()
    {
        try
        {
            Directory.CreateDirectory(LocalAppDataFolder);
        }
        catch (Exception ex)
        {
            ex.Out($"File access is restricted. Information won't save between sessions.\n");
        }
    }

    public static ViewModel ViewModel { get; private set; } = null!;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Process process = Process.GetProcessById(Environment.ProcessId);
        var all = Process.GetProcessesByName(process.ProcessName);
        if (all.Length > 1)
        {
            Shutdown();
            return;
        }

        //VTubeStudioDiscovery.Instance.Start();
        VTubeStudio.Instance.Start();

        //ViewModel = (ViewModel)Current.Resources[nameof(ViewModel)];
        //MainWindow = new MainWindow { DataContext = ViewModel };
        //MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        //VTubeStudioDiscovery.Instance.Stop();
        VTubeStudio.Instance.Stop();
    }
}