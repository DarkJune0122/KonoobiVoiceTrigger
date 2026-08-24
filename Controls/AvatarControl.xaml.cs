using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace VoiceTrigger.Controls;

/// <summary>
/// Interaction logic for AvatarControl.xaml
/// </summary>
[ObservableObject]
public partial class AvatarControl : UserControl
{
    public static readonly DependencyProperty NormalImageProperty =
        DependencyProperty.Register(nameof(NormalImage), typeof(ImageSource),
            typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty ActiveImageProperty =
        DependencyProperty.Register(nameof(ActiveImage), typeof(ImageSource),
            typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty TriggeredNormalImageProperty =
        DependencyProperty.Register(nameof(TriggeredNormalImage), typeof(ImageSource),
            typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty TriggeredActiveImageProperty =
        DependencyProperty.Register(nameof(TriggeredActiveImage), typeof(ImageSource),
            typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty AuthenticatedProperty =
        DependencyProperty.Register(nameof(Authenticated), typeof(bool),
            typeof(AvatarControl), new PropertyMetadata(false));
    public static readonly DependencyProperty AvatarFlagsProperty =
        DependencyProperty.Register(nameof(AvatarFlags), typeof(AvatarFlags),
            typeof(AvatarControl), new PropertyMetadata(AvatarFlags.Normal));

    public ImageSource? NormalImage
    {
        get => (ImageSource?)GetValue(NormalImageProperty);
        set => SetValue(NormalImageProperty, value);
    }
    public ImageSource? ActiveImage
    {
        get => (ImageSource?)GetValue(ActiveImageProperty);
        set => SetValue(ActiveImageProperty, value);
    }
    public ImageSource? TriggeredNormalImage
    {
        get => (ImageSource?)GetValue(TriggeredNormalImageProperty);
        set => SetValue(TriggeredNormalImageProperty, value);
    }
    public ImageSource? TriggeredActiveImage
    {
        get => (ImageSource?)GetValue(TriggeredActiveImageProperty);
        set => SetValue(TriggeredActiveImageProperty, value);
    }
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

    public AvatarControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Renderer.Effect = Authenticated ? null : App.MonochromeEffect;
        Renderer.Source = AvatarFlagsToImageSource(AvatarFlags);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == AuthenticatedProperty)
        {
            // TODO: Enable/Disable effect.
            Renderer.Effect = Authenticated ? null : App.MonochromeEffect;
        }

        if (e.Property == NormalImageProperty ||
            e.Property == ActiveImageProperty ||
            e.Property == TriggeredNormalImageProperty ||
            e.Property == TriggeredActiveImageProperty ||
            e.Property == AvatarFlagsProperty)
        {
            // TODO: Animate jump and a sprite change.
            Renderer.Source = AvatarFlagsToImageSource(AvatarFlags);
        }
    }

    ImageSource? AvatarFlagsToImageSource(AvatarFlags flags) => flags switch
    {
        AvatarFlags.Normal => NormalImage,
        AvatarFlags.Active => ActiveImage,
        AvatarFlags.TriggeredNormal => TriggeredNormalImage,
        AvatarFlags.TriggeredActive => TriggeredActiveImage,
        _ => NormalImage,
    };
}
