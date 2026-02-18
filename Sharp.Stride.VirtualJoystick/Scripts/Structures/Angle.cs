using Stride.Core.Mathematics;

namespace Sharp.Stride.VirtualJoystick.Scripts.Structures
{
    public struct Angle
    {
        private float _radians;
        private float _degrees;

        public float Radians
        {
            get => _radians;
            set
            {
                if (_radians != value)
                {
                    _radians = value;
                    _degrees = MathUtil.RadiansToDegrees(_radians);
                }
            }
        }
        public float Degrees
        {
            get => _degrees;
            set
            {
                if (_degrees != value)
                {
                    _degrees = value;
                    _radians = MathUtil.DegreesToRadians(_degrees);
                }
            }
        }

        public Angle(float radians, float degrees)
        {
            _radians = radians;
            _degrees = degrees;
        }
    }
}
