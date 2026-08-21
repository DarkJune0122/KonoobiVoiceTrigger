using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Interop;
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

    private void FluentWindow_Activated(object sender, EventArgs e)
    {
        if (DataContext is RootViewModel model)
            model.Activate();
        else $"Not a {nameof(RootViewModel)}!".Out(ConsoleColor.Yellow);
    }

    private void FluentWindow_Deactivated(object sender, EventArgs e)
    {
        if (DataContext is RootViewModel model)
            model.Deactivate();
        else $"Not a {nameof(RootViewModel)}!".Out(ConsoleColor.Yellow);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        //if (e.Key == Key.Y)
        //{
        //    TestBox.ItemsSource = new string[] { "a", "b", "c" };
        //    TestBox.SelectedIndex = 2;
        //}
        //if (e.Key == Key.I)
        //{
        //    TestBox.ItemsSource = Array.Empty<string>();
        //    TestBox.SelectedItem.Out("Selected: ", ConsoleColor.Yellow);
        //}
    }
}