using System.Diagnostics;
using System.Windows;

namespace VoiceTrigger;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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
        
        //ViewModel = (ViewModel)Current.Resources[nameof(ViewModel)];
        //MainWindow = new MainWindow { DataContext = ViewModel };
        //MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}