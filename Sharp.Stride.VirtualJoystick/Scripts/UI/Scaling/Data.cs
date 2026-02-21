using Stride.Core.Mathematics;
using Stride.UI;
using System.Collections.Generic;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private class Data
        {
            public Size2 DesignResolution { get; }
            public Dictionary<UIElement, Thickness> OriginalMarginByElement { get; }
            public Dictionary<UIElement, Vector2> OriginalSizeByElement { get; }

            public Data(Size2 designResolution)
            {
                DesignResolution = designResolution;
                OriginalMarginByElement = new Dictionary<UIElement, Thickness>();
                OriginalSizeByElement = new Dictionary<UIElement, Vector2>();
            }
        }
    }
}
