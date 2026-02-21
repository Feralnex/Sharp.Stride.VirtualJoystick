using Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling;
using Stride.Engine;

namespace Sharp.Stride.VirtualJoystick.Scripts
{
    public class CustomGame : Game
    {
        protected override void Initialize()
        {
            base.Initialize();

            Services.AddService<IAutoUIScaling>(new AutoUIScaling(this));
        }
    }
}
