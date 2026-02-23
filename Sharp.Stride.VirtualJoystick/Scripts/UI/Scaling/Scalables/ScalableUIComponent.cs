using Stride.Core.Mathematics;
using Stride.Engine;
using System.Collections.Generic;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private class ScalableUIComponent
        {
            public UIComponent Component { get; }
            public Size2 DesignResolution { get; }
            public List<ScalableUIElement> ScalableElements { get; }

            public ScalableUIComponent(UIComponent component, Size2 designResolution)
            {
                Component = component;
                DesignResolution = designResolution;
                ScalableElements = new List<ScalableUIElement>();
            }

            public void Scale(Size2 currentResolution)
            {
                Vector2 resolutionScale = new Vector2(
                    (float)currentResolution.Width / DesignResolution.Width,
                    (float)currentResolution.Height / DesignResolution.Height
                );

                Component.Resolution = new Vector3(currentResolution.Width, currentResolution.Height, Component.Resolution.Z);

                foreach (ScalableUIElement scalableElement in ScalableElements)
                    scalableElement.Scale(resolutionScale);
            }
        }
    }
}
