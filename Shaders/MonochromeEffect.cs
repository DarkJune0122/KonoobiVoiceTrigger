using System.Windows.Media;
using System.Windows.Media.Effects;

namespace VoiceTrigger.Shaders
{
    public sealed class MonochromeEffect : ShaderEffect
    {
        public MonochromeEffect()
        {
            PixelShader = new PixelShader
            {
                UriSource = ShaderHelpers.MakeShaderUri("MonochromeShader"),
            };

            UpdateShaderValue(InputProperty);
        }

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(MonochromeEffect), 0);
    }
}
