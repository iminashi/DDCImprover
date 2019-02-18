using System;
using System.Windows.Media.Effects;
using System.Windows;
using System.Windows.Media;

namespace DDCImprover.WPF.GrayScaleEffect
{
    // 
    /// <summary>
    /// Grayscale pixel shader effect.
    /// </summary>
    /// <remarks>http://bursjootech.blogspot.com/2008/06/grayscale-effect-pixel-shader-effect-in.html</remarks>
    public sealed class GrayScaleEffect : ShaderEffect
    {
        public GrayScaleEffect()
        {
            PixelShader = new PixelShader
            {
                UriSource = new Uri("pack://application:,,,/DDCImprover;component/GrayScaleEffect/GrayscaleEffect.ps")
            };

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(SaturationFactorProperty);
        }

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(GrayScaleEffect), 0);

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public static readonly DependencyProperty SaturationFactorProperty =
            DependencyProperty.Register(
                nameof(SaturationFactor),
                typeof(double),
                typeof(GrayScaleEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0), CoerceSaturationFactor));

        public double SaturationFactor
        {
            get => (double)GetValue(SaturationFactorProperty);
            set => SetValue(SaturationFactorProperty, value);
        }

        private static object CoerceSaturationFactor(DependencyObject d, object value)
        {
            GrayScaleEffect effect = (GrayScaleEffect)d;
            double newFactor = (double)value;

            if (newFactor < 0.0 || newFactor > 1.0)
            {
                return effect.SaturationFactor;
            }

            return newFactor;
        }
    }
}