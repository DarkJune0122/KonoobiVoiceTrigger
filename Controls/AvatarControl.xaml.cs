using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VoiceTrigger.Services;
using VoiceTrigger.Shaders;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.Controls;

public sealed class JumpEasingFunction : EasingFunctionBase
{
    protected override Freezable CreateInstanceCore() => new JumpEasingFunction();
    protected override double EaseInCore(double normalizedTime)
    {
        return 4 * normalizedTime * (1 - normalizedTime);
    }
}

public sealed class TwirlEasingFunction : EasingFunctionBase
{
    public double TotalCircles { get; set; } = 3;
    protected override Freezable CreateInstanceCore() => new TwirlEasingFunction();
    protected override double EaseInCore(double normalizedTime)
    {
        return Math.Cos(normalizedTime * Math.PI * 0.5)
             * Math.Cos(TotalCircles * normalizedTime * Math.PI * 2);
    }
}

/// <summary>
/// Interaction logic for AvatarControl.xaml
/// </summary>
[ObservableObject]
public partial class AvatarControl : UserControl
{
    public static readonly DependencyProperty MonochromeEffectProperty =
        DependencyProperty.Register(nameof(MonochromeEffect), typeof(MonochromeEffect),
            typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty AuthenticatedProperty =
        DependencyProperty.Register(nameof(Authenticated), typeof(bool),
            typeof(AvatarControl), new PropertyMetadata(false));
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double),
            typeof(AvatarControl), new PropertyMetadata(0d));
    public static readonly DependencyProperty AvatarFlagsProperty =
        DependencyProperty.Register(nameof(AvatarFlags), typeof(AvatarFlags),
            typeof(AvatarControl), new PropertyMetadata(AvatarFlags.Normal));
    public static readonly DependencyProperty FallbackImageProperty =
        DependencyProperty.Register(nameof(FallbackImage), typeof(ImageSource),
            typeof(AvatarControl), new PropertyMetadata(null));

    public bool Authenticated
    {
        get => (bool)GetValue(AuthenticatedProperty);
        set => SetValue(AuthenticatedProperty, value);
    }
    public AvatarFlags AvatarFlags
    {
        get => (AvatarFlags)GetValue(AvatarFlagsProperty);
        set => SetValue(AvatarFlagsProperty, value);
    }
    public ImageSource? FallbackImage
    {
        get => (ImageSource?)GetValue(FallbackImageProperty);
        set => SetValue(FallbackImageProperty, value);
    }
    public MonochromeEffect MonochromeEffect
    {
        get => (MonochromeEffect)GetValue(MonochromeEffectProperty);
        set => SetValue(MonochromeEffectProperty, value);
    }
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    readonly AvatarSettings Settings;
    readonly DispatcherTimer AuraUpdateTimer;
    readonly long ApplicationStartTick = Environment.TickCount64;

    public AvatarControl()
    {
        InitializeComponent();

        double frameRate = Math.Max(0.0001, ConfigurationService.Roaming.AuraFrameRate);
        AuraUpdateTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1d / frameRate),
        };
        AuraUpdateTimer.Tick += (s, e) =>
        {
            double frequency = Math.Max(0.0001, ConfigurationService.Roaming.AuraFrequency);

            double period = 1000d / frequency;
            double time = (Environment.TickCount64 - ApplicationStartTick) % period;
            const double OpacityRange = 0.2; // Range from 1.0 and lower.
            const double OpacityReduction = 0.2; // Recution from the top 1.0 opacity.
            double opeacity =
                Math.Sin(time * 2 * Math.PI) + 1 * 0.5 // Normalizes to [0:1]
                * OpacityRange + (1 - OpacityRange) // Keeps within a higher opacity range.
                - OpacityReduction;

            AuraRenderer.Opacity = opeacity;
        };
        AuraUpdateTimer.Start();

        string path = Path.Combine(App.ResourcesFolder, "AvatarSettings.json");
        try
        {
            string json = File.ReadAllText(path);
            Settings = JsonSerializer.Deserialize<AvatarSettings>(json, VTSPackets.JsonOptions) ?? AvatarSettings.Default;
            if (Settings.Version != AvatarSettings.Default.Version)
            {
                $"Avatar settings version mismatch! Expected {AvatarSettings.Default.Version}, got {Settings.Version}. Default settings will be used instead.".Out();
                Settings = AvatarSettings.Default;
            }
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to load avatar settings from json! Default settings will be used instead.");
            Settings = AvatarSettings.Default;
        }
        if (Settings == AvatarSettings.Default)
        {
            var raw = JsonSerializer.Serialize(Settings, VTSPackets.JsonOptions);
            try
            {
                File.WriteAllText(path, raw);
            }
            catch (Exception ex) { ex.Out($"Cannot write default settings!"); }
        }

        void TryInitializeSources(AvatarSettings.Option[]? options)
        {
            if (options is null) return;

            // Use index-based loop to avoid struct copy when assigning ImageSource.
            for (int i = 0; i < options.Length; i++)
            {
                ref var opt = ref options[i];

                // Default to fallback first.
                opt.ImageSource = FallbackImage;
                opt.AuraImageSource = null;

                // Ensure resources folder is valid.
                if (string.IsNullOrWhiteSpace(App.ResourcesFolder))
                {
                    // No resources path configured; fallback already set.
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(opt.FileName))
                    opt.ImageSource = LoadImage(opt.FileName, FallbackImage);

                if (!string.IsNullOrWhiteSpace(opt.AuraFileName))
                    opt.AuraImageSource = LoadImage(opt.AuraFileName, FallbackImage);

                static ImageSource? LoadImage(string fileName, ImageSource? fallback)
                {
                    string filePath;
                    try
                    {
                        filePath = Path.Combine(App.ResourcesFolder, "Avatar", fileName);
                    }
                    catch (Exception ex)
                    {
                        ex.Out($"Invalid path for avatar image '{fileName}'. Using fallback.");
                        return fallback;
                    }

                    if (!File.Exists(filePath))
                    {
                        // Missing file -> use fallback
                        return fallback;
                    }

                    try
                    {
                        // Load into BitmapImage using FileStream and OnLoad so the file is not locked.
                        using var fs = File.OpenRead(filePath);
                        var bi = new System.Windows.Media.Imaging.BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bi.StreamSource = fs;
                        bi.EndInit();
                        bi.Freeze();
                        return bi;
                    }
                    catch (Exception ex)
                    {
                        ex.Out($"Failed to load avatar image '{fileName}'. Using fallback.");
                        return fallback;
                    }
                }
            }

            Array.Sort(options, (a, b) => a.ActivationPercent.CompareTo(b.ActivationPercent));
        }

        // Initialize image sources for all option arrays.
        TryInitializeSources(Settings.NormalStates);
        TryInitializeSources(Settings.ActiveStates);
        TryInitializeSources(Settings.TriggeredNormalStates);
        TryInitializeSources(Settings.TriggeredActiveStates);
        Renderer.Effect = Authenticated ? null : MonochromeEffect;
        AuraRenderer.Effect = Authenticated ? null : MonochromeEffect;
        SetAvatarOption(EvaluateImageSource(Settings, AvatarFlags, Progress, FallbackImage));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == AuthenticatedProperty ||
            e.Property == MonochromeEffectProperty)
        {
            Renderer.Effect = Authenticated ? null : MonochromeEffect;
            AuraRenderer.Effect = Authenticated ? null : MonochromeEffect;
            if (e.Property == AuthenticatedProperty)
            {
                if (!Authenticated)
                    Twirl();
                else
                    Jump();
            }
        }

        if (e.Property == FallbackImageProperty)
        {
            SetFallbacks(Settings.NormalStates, FallbackImage);
            SetFallbacks(Settings.ActiveStates, FallbackImage);
            SetFallbacks(Settings.TriggeredNormalStates, FallbackImage);
            SetFallbacks(Settings.TriggeredActiveStates, FallbackImage);
        }

        if (e.Property == ProgressProperty ||
            e.Property == FallbackImageProperty ||
            e.Property == AvatarFlagsProperty)
        {
            if (e.Property == AvatarFlagsProperty)
            {
                if (AvatarFlags.IsActive())
                    Jump();
            }
            SetAvatarOption(EvaluateImageSource(Settings, AvatarFlags, Progress, FallbackImage));
        }
    }

    void Jump()
    {
        if (!IsVisible) return;
        JumpTransform.BeginAnimation(TranslateTransform.YProperty, null);

        DoubleAnimation animation = new()
        {
            From = 0d,
            To = 1.25d,
            Duration = TimeSpan.FromSeconds(0.125),
            EasingFunction = new JumpEasingFunction(),
            AutoReverse = true,
        };

        JumpTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    void Twirl()
    {
        if (!IsVisible) return;
        TwirlTransform.BeginAnimation(TranslateTransform.XProperty, null);

        DoubleAnimation animation = new()
        {
            From = 0d,
            To = 1d,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new TwirlEasingFunction() { TotalCircles = 3.6 },
            AutoReverse = true,
        };

        TwirlTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    void SetAvatarOption(AvatarSettings.Option option)
    {
        if (Renderer.Source != option.ImageSource)
        {
            Renderer.Source = option.ImageSource;
            if (AvatarFlags.IsActive()) Jump();
        }
        AuraRenderer.Source = option.AuraImageSource;
        AuraRenderer.Visibility = option.AuraImageSource is null
            ? Visibility.Hidden : Visibility.Visible;
    }

    static void SetFallbacks(AvatarSettings.Option[]? options, ImageSource? fallback)
    {
        if (options is null) return;
        for (int i = 0; i < options.Length; i++)
        {
            ref var opt = ref options[i];
            opt.ImageSource ??= fallback;
        }
    }

    static AvatarSettings.Option EvaluateImageSource(
        AvatarSettings? settings, AvatarFlags flags,
        double progress, ImageSource? fallback = null) => settings is null
        ? AvatarSettings.Option.MakeFallback(fallback)
        : flags switch
        {
            AvatarFlags.Normal => EvaluateOption(settings.NormalStates, progress, fallback),
            AvatarFlags.Active => EvaluateOption(settings.ActiveStates, progress, fallback),
            AvatarFlags.TriggeredNormal => EvaluateOption(settings.TriggeredNormalStates, progress, fallback),
            AvatarFlags.TriggeredActive => EvaluateOption(settings.TriggeredActiveStates, progress, fallback),
            _ => AvatarSettings.Option.MakeFallback(fallback),
        };

    static AvatarSettings.Option EvaluateOption(
        AvatarSettings.Option[]? options, double progress, ImageSource? fallback = null)
    {
        if (options is null || options.Length == 0)
            return AvatarSettings.Option.MakeFallback(fallback);

        // Find the last option where ActivationPercent <= progress.
        AvatarSettings.Option? selectedOption = null;
        foreach (var option in options)
        {
            if (option.ActivationPercent <= progress)
            {
                selectedOption = option;
            }
            else
            {
                break;
            }
        }

        // If no option was found, use the first one as a fallback.
        selectedOption ??= options[0];
        return selectedOption.Value;
    }
}
