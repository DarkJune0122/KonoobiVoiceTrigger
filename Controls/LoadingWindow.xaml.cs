using System.Windows.Input;
using System.Windows.Media.Animation;

namespace VoiceTrigger.Controls;

/// <summary>
/// Interaction logic for LoadingWindow.xaml
/// </summary>
/// <remarks>
/// Loading window is use to show a loading indicator before <see cref="MainWindow"/> appears.
/// Any crashes will be immediately indicated by the icon dissapearing instantly.
/// </remarks>
public partial class LoadingWindow : Window
{
    public const double FadeDuration = 1;

    public LoadingWindow()
    {
        InitializeComponent();
        Loaded += HandleWindowLoaded;
        Opacity = 0;
    }

    private void HandleWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Moves window to the corner.
        Rect area = SystemParameters.WorkArea;
        Left = area.Right - Width;
        Top = area.Bottom - Height;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            App.Current.TriggerShutdown();
            e.Handled = true;
        }
    }

    bool IsRunning;
    bool IsShowState;
    Action? CompletionAction;

    /// <param name="minimumSceneDuration">How long should the window stay on the screen.</param>
    public void TriggerShow(double minimumSceneDuration = 3)
    {
        if (IsShowState)
        {
            return;
        }

        IsShowState = true;
        if (IsRunning)
        {
            //$"Scheduled toggle to 'show'".Out(ConsoleColor.Cyan);
            CompletionAction = () => ShowAction(minimumSceneDuration);
        }
        else
        {
            //$"Toggled to 'show'".Out(ConsoleColor.Cyan);
            CompletionAction = null;
            ShowAction(minimumSceneDuration);
        }
    }

    public void TriggerHide(bool close = true)
    {
        if (!IsShowState)
        {
            return;
        }

        IsShowState = false;
        if (IsRunning)
        {
            //$"Scheduled toggle to 'hide'".Out(ConsoleColor.Cyan);
            CompletionAction = () => HideAction(close);
        }
        else
        {
            //$"Toggled to 'hide'".Out(ConsoleColor.Cyan);
            CompletionAction = null;
            HideAction(close);
        }
    }

    void ShowAction(double minimumSceneDuration)
    {
        //$"'ShowAction'".Out(ConsoleColor.Cyan);
        DoubleAnimation animation = new()
        {
            From = 0,
            To = 1,
            // Human's peripheral vision makes any fast animations appear instant.
            // 0.5s and 0.75s as proved to look like this, so we use 1s instead.
            Duration = TimeSpan.FromSeconds(FadeDuration),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut },
            AutoReverse = false,
        };
        animation.Completed += async (s, e) =>
        {
            //$"Starting await...".Out(ConsoleColor.Cyan);
            await Task.Delay(TimeSpan.FromSeconds(minimumSceneDuration));
            //$"Awaited!".Out(ConsoleColor.Cyan);
            _ = Application.Current.Dispatcher.BeginInvoke(() => HandleAnimationCompleted(s, e));
        };
        BeginAnimation(OpacityProperty, animation);
        IsRunning = true;
        //$"'ShowAction' IsRunning = true".Out(ConsoleColor.Cyan);
    }

    void HideAction(bool close)
    {
        //$"'HideAction'".Out(ConsoleColor.Cyan);
        DoubleAnimation animation = new()
        {
            From = 1,
            To = 0,
            // Human's peripheral vision makes any fast animations appear instant.
            // 0.5s and 0.75s as proved to look like this, so we use 1s instead.
            Duration = TimeSpan.FromSeconds(FadeDuration),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut },
            AutoReverse = false,
        };
        animation.Completed += (s, e) =>
        {
            //$"Deciding on closure. Close? {close}".Out(ConsoleColor.Cyan);
            if (close) Close();
            else HandleAnimationCompleted(s, e);
        };
        BeginAnimation(OpacityProperty, animation);
        IsRunning = true;
        //$"'HideAction' IsRunning = true".Out(ConsoleColor.Cyan);
    }

    private void HandleAnimationCompleted(object? sender, EventArgs e)
    {
        var action = CompletionAction;
        CompletionAction = null;
        IsRunning = false;
        //$"Handling animation completion. Callback: {action}".Out(ConsoleColor.Cyan);
        action?.Invoke();
    }
}
