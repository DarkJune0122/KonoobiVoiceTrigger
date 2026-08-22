using CommunityToolkit.Mvvm.ComponentModel;
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
        DependencyProperty.Register(nameof(NormalImage), typeof(ImageSource), typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty ActiveImageProperty =
        DependencyProperty.Register(nameof(ActiveImage), typeof(ImageSource), typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty TriggeredNormalImageProperty =
        DependencyProperty.Register(nameof(TriggeredNormalImage), typeof(ImageSource), typeof(AvatarControl), new PropertyMetadata(null));
    public static readonly DependencyProperty TriggeredActiveImageProperty =
        DependencyProperty.Register(nameof(TriggeredActiveImage), typeof(ImageSource), typeof(AvatarControl), new PropertyMetadata(null));

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

    [ObservableProperty] public partial bool Authenticated { get; set; }
    [ObservableProperty] public partial AvatarFlags AvatarFlags { get; set; }

    public AvatarControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
    }

    partial void OnAuthenticatedChanged(bool value)
    {
        // TODO: Enable/Disable effect.
    }

    partial void OnAvatarFlagsChanged(AvatarFlags value)
    {
        // TODO: Animate jump and a sprite change.
        Renderer.Source = value switch
        {
            AvatarFlags.Normal => NormalImage,
            AvatarFlags.Active => ActiveImage,
            AvatarFlags.TriggeredNormal => TriggeredNormalImage,
            AvatarFlags.TriggeredActive => TriggeredActiveImage,
            _ => NormalImage,
        };
    }
}
