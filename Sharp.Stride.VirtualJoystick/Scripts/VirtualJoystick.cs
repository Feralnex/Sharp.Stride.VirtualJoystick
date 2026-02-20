using CommunityToolkit.HighPerformance;
using Sharp.Collections;
using Sharp.Collections.Extensions;
using Sharp.Stride.VirtualJoystick.Scripts.Structures;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        private IAutoUIScaling AutoScaling { get; set; }
        private Vector3 ZonePosition => AutoScaling.GetPosition(Zone);
        private Vector3 InitialThresholdPosition => AutoScaling.GetPosition(Threshold);
        private Vector3 InitialThumbstickPosition => AutoScaling.GetPosition(Thumbstick);

        private Vector3 _currentThresholdPosition;
        private Vector3 _currentThumbstickPosition;

        private UIComponent UI { get; set; }
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
            Deactivate();

            AutoScaling = Services.GetService<IAutoUIScaling>();
            AutoScaling.Add(UI, _designResolution);
            AutoScaling.SetCustomScaling(Thumbstick, OnScaleThumbstickPosition);
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
        }

        private void GetReferences()
        {
            UIComponent ui = Entity.Get<UIComponent>();
            UIElement root = ui.Page.RootElement;

            UI = ui;
            Zone = root.FindVisualChildOfType<Canvas>(nameof(Zone));
            Threshold = root.FindVisualChildOfType<ImageElement>(nameof(Threshold));
            Thumbstick = root.FindVisualChildOfType<ImageElement>(nameof(Thumbstick));
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
        }

        private Vector3 OnScaleThumbstickPosition(UIElement element)
        {
            var thresholdPosition = AutoScaling.GetPosition(Threshold);
            var thumbstickPosition = new Vector3()
            {
                X = thresholdPosition.X + Threshold.Width / 2 - Thumbstick.Width / 2,
                Y = thresholdPosition.Y + Threshold.Height / 2 - Thumbstick.Height / 2,
                Z = thresholdPosition.Z
            };

            return thumbstickPosition;
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
            Vector2 zonePosition = new Vector2(ZonePosition.X, ZonePosition.Y);
            Size2 zoneSize = new Size2((int)Zone.Width, (int)Zone.Height);

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
            _currentThresholdPosition.X = position.X - ZonePosition.X - Threshold.Width / 2;
            _currentThresholdPosition.Y = position.Y - ZonePosition.Y - Threshold.Height / 2;
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

            Threshold.SetCanvasAbsolutePosition(InitialThresholdPosition);
            Thumbstick.SetCanvasAbsolutePosition(InitialThumbstickPosition);

            _absoluteInputChangedListeners?.IfSome(OnSomeInputChanged, _absoluteInput);
            _relativeInputChangedListeners?.IfSome(OnSomeRelativeInputChanged, _relativeInput);
            _radiusChangedListeners?.IfSome(OnSomeRadiusChanged, _radius);
            _stoppedDraggingListeners?.IfSome(OnSomeStoppedDragging, position);
        }

        private void OnDragging(Vector2 position)
        {
            Vector2 localPoint = new Vector2(
                x: position.X - ZonePosition.X - _currentThresholdPosition.X - Threshold.Width / 2,
                y: position.Y - ZonePosition.Y - _currentThresholdPosition.Y - Threshold.Height / 2);
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
