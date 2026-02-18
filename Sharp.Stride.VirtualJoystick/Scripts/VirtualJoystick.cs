using CommunityToolkit.HighPerformance;
using Sharp.Collections;
using Sharp.Collections.Extensions;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Stride.VirtualJoystick.Scripts.Extensions;
using Sharp.Stride.VirtualJoystick.Scripts.Structures;

namespace Sharp.Stride.VirtualJoystick.Scripts
{
    public class VirtualJoystick : SyncScript, IVirtualJoystick
    {
        #region Events

        public event Action<Vector2> StartedDragging
        {
            add => _startedDraggingListeners.Add(value);
            remove => _startedDraggingListeners.Remove(value);
        }
        public event Action<Vector2> StoppedDragging
        {
            add => _stoppedDraggingListeners.Add(value);
            remove => _stoppedDraggingListeners.Remove(value);
        }
        public event Action<Vector2> AbsoluteInputChanged
        {
            add => _absoluteInputChangedListeners.Add(value);
            remove => _absoluteInputChangedListeners.Remove(value);
        }
        public event Action<Vector2> RelativeInputChanged
        {
            add => _relativeInputChangedListeners.Add(value);
            remove => _relativeInputChangedListeners.Remove(value);
        }
        public event Action<float> RadiusChanged
        {
            add => _radiusChangedListeners.Add(value);
            remove => _radiusChangedListeners.Remove(value);
        }
        public event Action<Angle> AbsoluteAngleChanged
        {
            add => _absoluteAngleChangedListeners.Add(value);
            remove => _absoluteAngleChangedListeners.Remove(value);
        }
        public event Action<Angle> RelativeAngleChanged
        {
            add => _relativeAngleChangedListeners.Add(value);
            remove => _relativeAngleChangedListeners.Remove(value);
        }

        private readonly References<Action<Vector2>> _startedDraggingListeners = new References<Action<Vector2>>();
        private readonly References<Action<Vector2>> _stoppedDraggingListeners = new References<Action<Vector2>>();
        private readonly References<Action<Vector2>> _absoluteInputChangedListeners = new References<Action<Vector2>>();
        private readonly References<Action<Vector2>> _relativeInputChangedListeners = new References<Action<Vector2>>();
        private readonly References<Action<float>> _radiusChangedListeners = new References<Action<float>>();
        private readonly References<Action<Angle>> _absoluteAngleChangedListeners = new References<Action<Angle>>();
        private readonly References<Action<Angle>> _relativeAngleChangedListeners = new References<Action<Angle>>();
        private readonly Reference<Action<Vector2>> _dragging = new Reference<Action<Vector2>>();

        #endregion Events

        #region Input

        private List<PointerEvent> _pointerEvents;
        private Value<int> _pointerId;
        private Vector2 _absoluteInput;
        private Vector2 _relativeInput;
        private float _radius;
        private float _absoluteAngleInRadians;
        private float _absoluteAngleInDegrees;
        private float _relativeAngleInRadians;
        private float _relativeAngleInDegrees;

        private float ThumbstickAngleInRadians
        {
            get
            {
                float angle = MathF.Atan2(_absoluteInput.X, _absoluteInput.Y);

                if (angle < 0)
                    angle += MathUtil.TwoPi;

                return angle;
            }
        }
        private float RelativeThumbstickAngleInRadians
        {
            get
            {
                float angleInRadians = (ThumbstickAngleInRadians + AngleOffsetInRadians) % MathUtil.TwoPi;

                return angleInRadians;
            }
        }
        private float AngleOffsetInRadians
        {
            get
            {
                Quaternion rotation = _relativeObject.Transform.Rotation;

                // Calculate siny_cosp, which is part of the formula for yaw (rotation around Y-axis).
                // This comes from 2 * (w * y + x * z) from the quaternion components.
                float siny_cosp = 2 * (rotation.W * rotation.Y + rotation.X * rotation.Z);

                // Calculate cosy_cosp, another part of the formula for yaw.
                // This comes from 1 - 2 * (y * y + z * z) from the quaternion components.
                float cosy_cosp = 1 - 2 * (rotation.Y * rotation.Y + rotation.Z * rotation.Z);

                // Use atan2 to calculate the yaw (rotation around the Y-axis) in radians.
                // atan2 handles both the quadrant and direction (clockwise or counterclockwise) correctly.
                float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

                // If yaw is negative, add 2 * Pi to bring it into the positive range [0, 2π].
                if (yaw < 0)
                    yaw += MathUtil.TwoPi;  // TwoPi is a constant representing 2 * Pi.

                // Return the yaw value in radians, now guaranteed to be non-negative and in [0, 2π].
                return yaw;
            }
        }

        public Vector2 AbsoluteInput => _absoluteInput;
        public Vector2 RelativeInput => _relativeInput;
        public float Radius => _radius;
        public float AbsoluteAngleInRadians => _absoluteAngleInRadians;
        public float AbsoluteAngleInDegrees => _absoluteAngleInDegrees;
        public float RelativeAngleInRadians => _relativeAngleInRadians;
        public float RelativeAngleInDegrees => _relativeAngleInDegrees;

        #endregion Input

        #region UI

        private Vector3 _surfacePosition;
        private Vector3 _zonePosition;
        private Vector3 _initialThresholdPosition;
        private Vector3 _initialThumbstickPosition;
        private Vector3 _currentThresholdPosition;
        private Vector3 _currentThumbstickPosition;

        private Size2 _previousResolution;
        private Size2 _currentResolution;
        private float _widthScale;
        private float _heightScale;

        private UIComponent UI { get; set; }
        private Canvas Surface { get; set; }
        private Canvas Zone { get; set; }
        private ImageElement Threshold { get; set; }
        private ImageElement Thumbstick { get; set; }

        private Size2 _designResolution = new Size2(1280, 720);
        private Entity _relativeObject;
        private float _inactiveOpacity = 0.5f;
        private float _activeOpacity = 1f;

        public Size2 DesignResolution
        {
            get => _designResolution;
            set => _designResolution = value;
        }
        public Entity RelativeObject
        {
            get => _relativeObject;
            set => _relativeObject = value;
        }
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public float InactiveOpacity
        {
            get => _inactiveOpacity;
            set => _inactiveOpacity = value;
        }
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public float ActiveOpacity
        {
            get => _activeOpacity;
            set => _activeOpacity = value;
        }

        #endregion UI

        public override void Start()
        {
            GetReferences();
            InitializeData();
            SetScale(_currentResolution, _designResolution);
            EnsureProperSurfaceSize();
            ScaleSize(Zone, false, 2);
            ScaleSize(Threshold, true, 4);
            ScaleSize(Thumbstick, true, 2);
            ScaleMargins();
            InitializePositions();
            ResetMargins();
            OverrideAbsolutePositions();
            Deactivate();

            Game.Window.ClientSizeChanged += OnClientSizeChanged;
        }

        public override void Update()
            => _pointerId.Match(OnSomePointingInsideZone, OnNonePointingInsideZone);

        public override void Cancel()
        {
            _startedDraggingListeners.Clear();
            _stoppedDraggingListeners.Clear();
            _absoluteInputChangedListeners.Clear();
            _relativeInputChangedListeners.Clear();
            _radiusChangedListeners.Clear();
            _absoluteAngleChangedListeners.Clear();
            _relativeAngleChangedListeners.Clear();
            _dragging.Clear();

            Game.Window.ClientSizeChanged -= OnClientSizeChanged;
        }

        private void GetReferences()
        {
            UI = Entity.Get<UIComponent>();
            Surface = UI.Page.RootElement as Canvas;
            Zone = Surface.FindVisualChildOfType<Canvas>(nameof(Zone));
            Threshold = Surface.FindVisualChildOfType<ImageElement>(nameof(Threshold));
            Thumbstick = Surface.FindVisualChildOfType<ImageElement>(nameof(Thumbstick));
        }

        private void InitializeData()
        {
            _pointerEvents = Unsafe.As<List<PointerEvent>>(Input.PointerEvents);
            _pointerId = new Value<int>();
            _absoluteInput = Vector2.Zero;
            _relativeInput = Vector2.Zero;
            _radius = 0f;
            _absoluteAngleInRadians = 0f;
            _absoluteAngleInDegrees = 0f;
            _relativeAngleInRadians = 0f;
            _relativeAngleInDegrees = 0f;
            _previousResolution = _designResolution;
            _currentResolution = Game.Window.ClientBounds.Size;
        }

        private void SetScale(Size2 currentResolution, Size2 previousResolution)
        {
            _widthScale = (float)currentResolution.Width / previousResolution.Width;
            _heightScale = (float)currentResolution.Height / previousResolution.Height;
        }

        private void EnsureProperSurfaceSize()
        {
            if (float.IsNaN(Surface.Width))
                Surface.Width = Surface.Parent.GetWidth(Game.Window);
            if (float.IsNaN(Surface.Height))
                Surface.Height = Surface.Parent.GetHeight(Game.Window);
        }

        private void ScaleSize(UIElement element, bool isSquare, float denominator)
        {
            if (isSquare)
            {
                if (float.IsNaN(element.Width))
                    element.Width = element.Parent.Width / denominator;
                if (float.IsNaN(element.Height))
                    element.Height = element.Width;

                float scale = Math.Min(_widthScale, _heightScale);

                element.Width *= scale;
                element.Height *= scale;
            }
            else
            {
                if (float.IsNaN(element.Width))
                    element.Width = element.Parent.Width / denominator * _widthScale;
                else
                    element.Width *= _widthScale;
                if (float.IsNaN(element.Height))
                    element.Height = element.Parent.Height / denominator * _heightScale;
                else
                    element.Height *= _heightScale;
            }
        }

        private void ScaleMargins()
        {
            Surface.Margin = new Thickness(
                Surface.Margin.Left * _widthScale,
                Surface.Margin.Top * _heightScale,
                Surface.Margin.Right * _widthScale,
                Surface.Margin.Bottom * _heightScale);

            Zone.Margin = new Thickness(
                Zone.Margin.Left * _widthScale,
                Zone.Margin.Top * _heightScale,
                Zone.Margin.Right * _widthScale,
                Zone.Margin.Bottom * _heightScale);

            Threshold.Margin = new Thickness(
                Threshold.Margin.Left * _widthScale,
                Threshold.Margin.Top * _heightScale,
                Threshold.Margin.Right * _widthScale,
                Threshold.Margin.Bottom * _heightScale);

            Thumbstick.Margin = new Thickness(
                Thumbstick.Margin.Left * _widthScale,
                Thumbstick.Margin.Top * _heightScale,
                Thumbstick.Margin.Right * _widthScale,
                Thumbstick.Margin.Bottom * _heightScale);
        }

        private Vector3 ScalePosition(Vector3 position)
        {
            var oldCenter = new Vector2(_previousResolution.Width / 2f, _previousResolution.Height / 2f);
            var newCenter = new Vector2(_currentResolution.Width / 2f, _currentResolution.Height / 2f);
            var oldPosition = new Vector2(position.X, position.Y);
            var relativePosition = oldPosition - oldCenter;
            var scaled = new Vector2(
                relativePosition.X * _widthScale,
                relativePosition.Y * _heightScale
            );
            var newPosition = newCenter + scaled;

            position.X = newPosition.X;
            position.Y = newPosition.Y;

            return position;
        }

        private void InitializePositions()
        {
            _surfacePosition = Surface.GetLocalPosition(Game.Window);
            _zonePosition = Zone.GetLocalPosition(Game.Window);
            _initialThresholdPosition = Threshold.GetLocalPosition(Game.Window);
            _initialThumbstickPosition = new Vector3(_initialThresholdPosition.X + Threshold.Width / 2 - Thumbstick.Width / 2, _initialThresholdPosition.Y + Threshold.Height / 2 - Thumbstick.Height / 2, _initialThresholdPosition.Z);
            _currentThresholdPosition = _initialThresholdPosition;
            _currentThumbstickPosition = _initialThumbstickPosition;
        }

        private void ResetMargins()
        {
            Surface.Margin = new Thickness(0, 0, 0, 0);
            Zone.Margin = new Thickness(0, 0, 0, 0);
            Threshold.Margin = new Thickness(0, 0, 0, 0);
            Thumbstick.Margin = new Thickness(0, 0, 0, 0);
        }

        private void OverrideAbsolutePositions()
        {
            Surface.SetCanvasAbsolutePosition(_surfacePosition);
            Zone.SetCanvasAbsolutePosition(_zonePosition);
            Threshold.SetCanvasAbsolutePosition(_initialThresholdPosition);
            Thumbstick.SetCanvasAbsolutePosition(_initialThumbstickPosition);
        }

        private void OnClientSizeChanged(object sender, EventArgs eventArgs)
        {
            var resolution = Game.Window.ClientBounds.Size;

            SetNewResolution(resolution);
            SetScale(_currentResolution, _previousResolution);
            ScaleSize(Surface, false, 1);
            ScaleSize(Zone, false, 2);
            ScaleSize(Threshold, true, 4);
            ScaleSize(Thumbstick, true, 2);
            _surfacePosition = ScalePosition(_surfacePosition);
            _zonePosition = ScalePosition(_zonePosition);
            _initialThresholdPosition = ScalePosition(_initialThresholdPosition);
            _initialThumbstickPosition = ScalePosition(_initialThumbstickPosition);
            _currentThresholdPosition = ScalePosition(_currentThresholdPosition);
            _currentThumbstickPosition = ScalePosition(_currentThumbstickPosition);
            OverrideAbsolutePositions();
        }

        private void SetNewResolution(Size2 resolution)
        {
            UI.Resolution = new Vector3(resolution.Width, resolution.Height, UI.Resolution.Z);

            _previousResolution = _currentResolution;
            _currentResolution = resolution;
        }

        private void OnSomePointingInsideZone(int pointerId)
            => HandlePointerEvents(0, pointerId);

        private void OnNonePointingInsideZone()
        {
            Span<PointerEvent> span = new Span<PointerEvent>(_pointerEvents.GetItems(), 0, _pointerEvents.Count);

            for (int index = 0; index < span.Length; index++)
            {
                PointerEvent pointerEvent = span.DangerousGetReferenceAt(index);

                if (pointerEvent.EventType == PointerEventType.Pressed
                    && IsPointingInsideZone(pointerEvent.AbsolutePosition))
                {
                    int startIndex = ++index;

                    _pointerId.Set(pointerEvent.PointerId);

                    HandlePointerEvent(pointerEvent);
                    HandlePointerEvents(startIndex, pointerEvent.PointerId);

                    return;
                }
            }
        }

        private bool IsPointingInsideZone(Vector2 pointerPosition)
        {
            Vector2 zonePosition = new Vector2(_zonePosition.X, _zonePosition.Y);
            Size2 zoneSize = new Size2((int)Zone.Size.X, (int)Zone.Size.Y);

            return pointerPosition.X >= zonePosition.X &&
                pointerPosition.X <= zonePosition.X + zoneSize.Width &&
                pointerPosition.Y >= zonePosition.Y &&
                pointerPosition.Y <= zonePosition.Y + zoneSize.Height;
        }

        private void HandlePointerEvents(int startIndex, int pointerId)
        {
            Span<PointerEvent> span = new Span<PointerEvent>(_pointerEvents.GetItems(), 0, _pointerEvents.Count);

            for (int index = startIndex; index < span.Length; index++)
            {
                PointerEvent pointerEvent = span.DangerousGetReferenceAt(index);

                if (pointerEvent.PointerId == pointerId)
                    HandlePointerEvent(pointerEvent);
            }
        }

        private void HandlePointerEvent(PointerEvent pointerEvent)
        {
            if (pointerEvent.EventType == PointerEventType.Pressed)
            {
                OnPointerDown(pointerEvent.AbsolutePosition);
            }
            else if (pointerEvent.EventType == PointerEventType.Released)
            {
                OnPointerUp(pointerEvent.AbsolutePosition);
            }
            else if (pointerEvent.EventType == PointerEventType.Moved)
            {
                _dragging.IfSome(OnSomeDragging, pointerEvent.AbsolutePosition);
            }
        }

        private void OnPointerDown(Vector2 position)
        {
            _currentThresholdPosition.X = position.X - _zonePosition.X - Threshold.Width / 2;
            _currentThresholdPosition.Y = position.Y - _zonePosition.Y - Threshold.Height / 2;
            _currentThumbstickPosition.X = _currentThresholdPosition.X + Threshold.Width / 2 - Thumbstick.Width / 2;
            _currentThumbstickPosition.Y = _currentThresholdPosition.Y + Threshold.Height / 2 - Thumbstick.Height / 2;

            _startedDraggingListeners?.IfSome(OnSomeStartedDragging, position);
            Activate();
            _dragging.Set(OnDragging);

            Threshold.SetCanvasAbsolutePosition(_currentThresholdPosition);
            Thumbstick.SetCanvasAbsolutePosition(_currentThumbstickPosition);

            _absoluteInputChangedListeners?.IfSome(OnSomeInputChanged, _absoluteInput);
            _relativeInputChangedListeners?.IfSome(OnSomeRelativeInputChanged, _relativeInput);
            _radiusChangedListeners?.IfSome(OnSomeRadiusChanged, _radius);
        }

        private void OnPointerUp(Vector2 position)
        {
            if (!_dragging.HasSome)
                return;

            Deactivate();
            _dragging.Clear();
            _pointerId.Clear();

            _absoluteInput = Vector2.Zero;
            _relativeInput = Vector2.Zero;
            _radius = 0f;

            Threshold.SetCanvasAbsolutePosition(_initialThresholdPosition);
            Thumbstick.SetCanvasAbsolutePosition(_initialThumbstickPosition);

            _absoluteInputChangedListeners?.IfSome(OnSomeInputChanged, _absoluteInput);
            _relativeInputChangedListeners?.IfSome(OnSomeRelativeInputChanged, _relativeInput);
            _radiusChangedListeners?.IfSome(OnSomeRadiusChanged, _radius);
            _stoppedDraggingListeners?.IfSome(OnSomeStoppedDragging, position);
        }

        private void OnDragging(Vector2 position)
        {
            Vector2 localPoint = new Vector2(
                x: position.X - _zonePosition.X - _currentThresholdPosition.X - Threshold.Width / 2,
                y: position.Y - _zonePosition.Y - _currentThresholdPosition.Y - Threshold.Height / 2);
            Vector2 clampedLocalPoint = ClampLocalPoint(localPoint, Threshold.Size);
            Vector2 input = CalculateInput(clampedLocalPoint);
            Vector2 thumbstickPosition = CalculateThumbstickPosition(input, Threshold.Size);
            float radius = input.Length();

            _absoluteInput = input;
            _absoluteInput.Y *= -1;
            _relativeInput = CalculateRelativeInput(radius, RelativeThumbstickAngleInRadians);
            _radius = radius;
            _absoluteAngleInRadians = ThumbstickAngleInRadians;
            _absoluteAngleInDegrees = MathUtil.RadiansToDegrees(_absoluteAngleInRadians);
            _relativeAngleInRadians = CalculateRotationAngle(_absoluteAngleInRadians, AngleOffsetInRadians);
            _relativeAngleInDegrees = MathUtil.RadiansToDegrees(_relativeAngleInRadians);

            _currentThumbstickPosition.X = _currentThresholdPosition.X + thumbstickPosition.X + Thumbstick.Width / 2;
            _currentThumbstickPosition.Y = _currentThresholdPosition.Y + thumbstickPosition.Y + Thumbstick.Height / 2;

            Thumbstick.SetCanvasAbsolutePosition(_currentThumbstickPosition);

            _absoluteInputChangedListeners?.IfSome(OnSomeInputChanged, _absoluteInput);
            _relativeInputChangedListeners?.IfSome(OnSomeRelativeInputChanged, _relativeInput);
            _radiusChangedListeners?.IfSome(OnSomeRadiusChanged, _radius);
            _absoluteAngleChangedListeners?.IfSome(OnSomeAngleChanged, new Angle(_absoluteAngleInRadians, _absoluteAngleInDegrees));
            _relativeAngleChangedListeners?.IfSome(OnSomeRelativeAngleChanged, new Angle(_relativeAngleInRadians, _relativeAngleInDegrees));
        }

        private void Activate()
        {
            Threshold.Opacity = _activeOpacity;
            Thumbstick.Opacity = _activeOpacity;
        }

        private void Deactivate()
        {
            Threshold.Opacity = _inactiveOpacity;
            Thumbstick.Opacity = _inactiveOpacity;
        }

        private void OnSomeStartedDragging(Action<Vector2> startedDragging, Vector2 input)
            => startedDragging(input);

        private void OnSomeStoppedDragging(Action<Vector2> stoppedDragging, Vector2 input)
            => stoppedDragging(input);

        private void OnSomeInputChanged(Action<Vector2> inputChanged, Vector2 input)
            => inputChanged(input);

        private void OnSomeRelativeInputChanged(Action<Vector2> relativeInputChanged, Vector2 relativeInput)
            => relativeInputChanged(relativeInput);

        private void OnSomeRadiusChanged(Action<float> relativeInputChanged, float radius)
            => relativeInputChanged(radius);

        private void OnSomeAngleChanged(Action<Angle> angleChanged, Angle angle)
            => angleChanged(angle);

        private void OnSomeRelativeAngleChanged(Action<Angle> relativeAngleChanged, Angle relativeAngle)
            => relativeAngleChanged(relativeAngle);

        private void OnSomeDragging(Action<Vector2> onDragging, Vector2 position)
            => onDragging(position);

        private static Vector2 ClampLocalPoint(Vector2 localPoint, Vector3 threshold)
        {
            localPoint.X /= threshold.X;
            localPoint.Y /= threshold.Y;

            return localPoint;
        }

        private static Vector2 CalculateInput(Vector2 localPoint)
        {
            Vector2 input = localPoint * 2;

            if (input.Length() > 1.0f)
                input.Normalize();

            return input;
        }

        private static Vector2 CalculateRelativeInput(float radius, float angleInRadians)
        {
            float xValue = radius * MathF.Sin(angleInRadians);
            float yValue = radius * MathF.Cos(angleInRadians);
            Vector2 relativeInput = new Vector2(xValue, yValue);

            if (relativeInput.Length() > 1.0f)
                relativeInput.Normalize();

            return relativeInput;
        }

        private static Vector2 CalculateThumbstickPosition(Vector2 input, Vector3 thresholdSize)
        {
            float xPosition = input.X * (thresholdSize.X / 2.5f);
            float yPosition = input.Y * (thresholdSize.Y / 2.5f);

            return new Vector2(xPosition, yPosition);
        }

        private static float CalculateRotationAngle(float angleInRadians, float radiansOffset)
        {
            float threshold = MathUtil.TwoPi - radiansOffset;
            return angleInRadians < threshold ? radiansOffset + angleInRadians : angleInRadians - threshold;
        }
    }
}
