using Sharp.Stride.VirtualJoystick.Scripts.UIScaling;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;

namespace Sharp.Stride.VirtualJoystick
{
    public interface IAutoUIScaling
    {
        void Add(UIComponent ui, Size2 designResolution);
        Vector3 GetPosition(UIElement element);
        void OverridePosition(UIElement element, Vector3 position);
        Vector3 ScalePosition(Vector3 position);
        void SetCustomScaling(UIElement element, PositionScalingHandler customPositionScaling);
    }
}
