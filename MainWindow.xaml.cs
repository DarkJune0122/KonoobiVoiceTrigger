using NAudio.CoreAudioApi;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using VoiceTrigger.Services;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;

namespace VoiceTrigger;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        HandleStateChange(WindowState);
        IsVisibleChanged += HandleVisibleChanged;
        HandleVisibleChanged(this, default);
    }

    private void HandleVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        App.OnCapturedVolume -= ProcessAudio;
        if (IsVisible)
        {
            App.OnCapturedVolume += ProcessAudio;
        }
    }

    private void ProcessAudio(float master)
    {
        Console.WriteLine($"TICK! {master}");
        Dispatcher.Invoke(() => SetVolume(master));
        //float[] channels = ArrayPool<float>.Shared.Rent(2);
        //int count = meter.MasterPeakValue;
        //switch (count)
        //{
        //    case 0:
        //        Array.Fill(channels, 0, 0, 2);
        //        HandleVolume(true, count, channels);
        //        break;
        //    case 1:
        //        channels[0] = channels[1] = meter.PeakValues[0];
        //        HandleVolume(false, count, channels);
        //        break;
        //    case 2:
        //        channels[0] = meter.PeakValues[0];
        //        channels[1] = meter.PeakValues[1];
        //        HandleVolume(false, count, channels);
        //        break;
        //    default:
        //        if (count > 0) goto case 2;
        //        else goto case 0;
        //}
    }
    /*
Microphone (2- Fifine Microphone)
Active
32 bit IEEFloat: 48000Hz 1 channels
Shared
Bytes: 3840
Bytes: 0
Bytes: 0
Bytes: 0
     ...

Microphone (Realtek(R) Audio)
Active
32 bit IEEFloat: 48000Hz 2 channels
Shared
Bytes: 34560
Bytes: 7680
Bytes: 11520
Bytes: 11520
Bytes: 15360
TICK! 0
TICK! 0
    ...
     */

    void SetVolume(float volume)
    {
        volume = Math.Clamp(volume, 0, 1);
        if (volume > 0.6f)
        {
            LeftAudio.Fill = Brushes.Red;
            RightAudio.Fill = Brushes.Red;
        }
        else
        {
            LeftAudio.Fill = Brushes.LimeGreen;
            RightAudio.Fill = Brushes.LimeGreen;
        }

        LeftAudio.Width = LeftAudioBackground.ActualWidth * volume;
        RightAudio.Width = RightAudioBackground.ActualWidth * volume;
    }

    private void Icon_Exit(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void Icon_Open(object sender, RoutedEventArgs e) => RestoreWindow();
    private void Icon_RestoreWindow(NotifyIcon sender, RoutedEventArgs e) => RestoreWindow();
    void RestoreWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Window_HideInstead(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        ShowInTaskbar = false;
        Hide();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        HandleStateChange(WindowState);
    }
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        HandleStateChange(WindowState);
        KeepWindowWithinScreen();
    }

    private void KeepWindowWithinScreen()
    {
        if (WindowState != WindowState.Normal) return;
        var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        var workingArea = screen.WorkingArea;
        const double StandardNormalOffset = 20;

        //Console.WriteLine(workingArea);
        //Console.WriteLine(Width);
        //Console.WriteLine(Height);

        // Keeps window within Left-Right bounds.
        if (Width > workingArea.Width - StandardNormalOffset)
        {
            Left = StandardNormalOffset / 2;
            Width = workingArea.Width - StandardNormalOffset;
        }
        else
        {
            double newLeft = Math.Max(workingArea.Left, Left);
            if (newLeft + Width > workingArea.Right)
            {
                newLeft = workingArea.Right - Width;
            }

            Left = newLeft;
        }

        // Keeps window within Top-Bottom bounds.
        if (Height > workingArea.Height - StandardNormalOffset)
        {
            Top = StandardNormalOffset / 2;
            Height = workingArea.Height - StandardNormalOffset;
        }
        else
        {
            double newTop = Math.Max(workingArea.Top, Top);
            if (newTop + Height > workingArea.Bottom)
                newTop = workingArea.Bottom - Height;

            Top = newTop;
        }
    }

    private void Title_Theme(object sender, RoutedEventArgs e) => ThemeService.Instance.NextTheme();
    private void Title_Close(object sender, RoutedEventArgs e) => Close();
    private void HandleStateChange(WindowState state)
    {
        if (state == WindowState.Maximized)
        {
            Header.Width = SystemParameters.WorkArea.Width;
        }
        else
        {
            Header.Width = Width;
        }
    }

    private void Title_Settings(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "mmsys.cpl",
            UseShellExecute = true
        });
    }

    private void Window_Activeted(object sender, EventArgs e)
    {
        if (DataContext is ViewModel model)
            model.RefreshDevices();
    }
}