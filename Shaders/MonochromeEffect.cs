using System.Windows.Media.Effects;

namespace VoiceTrigger.Shaders
{
    public sealed class MonochromeEffect : ShaderEffect
    {
        public MonochromeEffect()
        {
            PixelShader = new PixelShader
            {
                UriSource = new Uri("pack://application:,,,/Shaders/MonochromeShader.ps")
            };
        }
    }
}
