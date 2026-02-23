using Sharp.Stride.VirtualJoystick.Scripts.Extensions;
using Stride.Core.Mathematics;
using Stride.UI;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private class ScalableUIElement
        {
            public virtual UIElement Element { get; }
            public Vector2 OriginalSize { get; }
            public Thickness OriginalMargin { get; }
            public bool IsSquare { get; }

            public ScalableUIElement(UIElement element)
            {
                Element = element;
                OriginalSize = new Vector2(element.Width, element.Height);
                OriginalMargin = element.Margin;
                IsSquare = OriginalSize.X == OriginalSize.Y;
            }

            public virtual void Scale(Vector2 resolutionScale)
            {
                Element.ScaleSize(OriginalSize, resolutionScale, IsSquare);
                Element.ScaleMargin(OriginalMargin, resolutionScale, IsSquare);
            }
        }
    }
}
