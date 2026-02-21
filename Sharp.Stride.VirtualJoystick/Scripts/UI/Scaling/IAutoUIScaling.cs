using Stride.Core.Mathematics;
using Stride.Engine;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public interface IAutoUIScaling
    {
        void Add(UIComponent ui, Size2 designResolution);
    }
}
