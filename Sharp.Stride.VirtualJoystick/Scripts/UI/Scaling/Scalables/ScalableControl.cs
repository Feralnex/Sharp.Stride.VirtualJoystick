using Sharp.Stride.VirtualJoystick.Scripts.Extensions;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using System.Runtime.CompilerServices;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private class ScalableControl : ScalableUIElement
        {
            public override Control Element => Unsafe.As<Control>(base.Element);
            public Thickness OriginalPadding { get; }

            public ScalableControl(Control control)
                : base(control)
            {
                OriginalPadding = control.Padding;
            }

            public override void Scale(Vector2 resolutionScale)
            {
                base.Scale(resolutionScale);

                Element.ScalePadding(OriginalPadding, resolutionScale, IsSquare);
            }
        }
    }
}
