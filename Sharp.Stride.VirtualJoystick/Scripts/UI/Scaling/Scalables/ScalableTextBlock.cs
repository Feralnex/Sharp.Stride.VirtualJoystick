using Stride.Core.Mathematics;
using Stride.UI.Controls;
using System.Runtime.CompilerServices;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private class ScalableTextBlock : ScalableUIElement
        {
            public override TextBlock Element => Unsafe.As<TextBlock>(base.Element);
            public float OriginalTextSize { get; }

            public ScalableTextBlock(TextBlock textBlock)
                : base(textBlock)
            {
                OriginalTextSize = textBlock.TextSize;
            }

            public override void Scale(Vector2 resolutionScale)
            {
                base.Scale(resolutionScale);

                Element.TextSize = OriginalTextSize * resolutionScale.Y;
            }
        }
    }
}
