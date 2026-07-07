using System.Diagnostics;
using System.Drawing;
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
    
    public static RootViewModel RootViewModel => (RootViewModel)Current.Resources[nameof(VoiceTrigger.RootViewModel)];
    public static Color IndicatoKobiColor => (Color)Current.Resources[nameof(IndicatoKobiColor)];
    public static Brush IndicatoKobiBrush => (Brush)Current.Resources[nameof(IndicatoKobiBrush)];
    public static Color IndicatoIBOKColor => (Color)Current.Resources[nameof(IndicatoIBOKColor)];
    public static Brush IndicatoIBOKBrush => (Brush)Current.Resources[nameof(IndicatoIBOKBrush)];
    public static Color IndicatoFronzenColor => (Color)Current.Resources[nameof(IndicatoFronzenColor)];
    public static Brush IndicatoFronzenBrush => (Brush)Current.Resources[nameof(IndicatoFronzenBrush)];

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

        RootViewModel = 
        VTubeStudio.Instance.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        VTubeStudio.Instance.Stop();
    }
}