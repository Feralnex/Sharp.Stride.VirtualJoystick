using Stride.Core.Mathematics;
using System;
using Sharp.Stride.VirtualJoystick.Scripts.Structures;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Controls
{
    public interface IVirtualJoystick
    {
        event Action<Vector2> StartedDragging;
        event Action<Vector2> StoppedDragging;
        event Action<Vector2> AbsoluteInputChanged;
        event Action<Vector2> RelativeInputChanged;
        event Action<float> RadiusChanged;
        event Action<Angle> AbsoluteAngleChanged;
        event Action<Angle> RelativeAngleChanged;

        Vector2 AbsoluteInput { get; }
        Vector2 RelativeInput { get; }
        float Radius { get; }
        float AbsoluteAngleInRadians { get; }
        float AbsoluteAngleInDegrees { get; }
        float RelativeAngleInRadians { get; }
        float RelativeAngleInDegrees { get; }
    }
}
