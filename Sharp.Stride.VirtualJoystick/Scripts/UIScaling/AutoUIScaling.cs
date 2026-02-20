using CommunityToolkit.HighPerformance;
using Sharp.Stride.VirtualJoystick.Scripts.Extensions;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sharp.Stride.VirtualJoystick.Scripts.UIScaling
{
    public delegate Vector3 PositionScalingHandler(UIElement element);

    public partial class AutoUIScaling : IAutoUIScaling
    {
        private Game _game;
        private List<UIComponent> _uis;
        private Dictionary<UIElement, Vector3> _positionByElement;
        private Dictionary<UIElement, PositionScalingHandler> _positionScalingByElement;
        private Size2 _previousResolution;
        private Size2 _currentResolution;
        private Vector2 _resolutionScale;

        public AutoUIScaling(Game game)
        {
            _game = game;
            _uis = new List<UIComponent>();
            _positionByElement = new Dictionary<UIElement, Vector3>();
            _positionScalingByElement = new Dictionary<UIElement, PositionScalingHandler>();
            _previousResolution = _game.Window.ClientBounds.Size;
            _currentResolution = _game.Window.ClientBounds.Size;
            _resolutionScale = new Vector2(1, 1);

            _game.Window.ClientSizeChanged += OnClientSizeChanged;
        }

        ~AutoUIScaling()
        {
            _game.Window.ClientSizeChanged -= OnClientSizeChanged;
        }

        public void Add(UIComponent ui, Size2 designResolution)
        {
            Vector2 resolutionScale = new Vector2()
            {
                X = (float)_currentResolution.Width / designResolution.Width,
                Y = (float)_currentResolution.Height / designResolution.Height
            };

            ui.Resolution = new Vector3(_currentResolution.Width, _currentResolution.Height, ui.Resolution.Z);
            _uis.Add(ui);

            Add(ui.Page.RootElement, resolutionScale);
        }

        public Vector3 GetPosition(UIElement element)
            => _positionByElement.GetValueOrDefault(element);

        public void OverridePosition(UIElement element, Vector3 position)
        {
            if (_positionByElement.ContainsKey(element))
                _positionByElement[element] = position;
        }

        public Vector3 ScalePosition(Vector3 position)
        {
            Vector2 oldCenter = new Vector2(_previousResolution.Width / 2f, _previousResolution.Height / 2f);
            Vector2 newCenter = new Vector2(_currentResolution.Width / 2f, _currentResolution.Height / 2f);
            Vector2 oldPosition = new Vector2(position.X, position.Y);
            Vector2 relativePosition = oldPosition - oldCenter;
            Vector2 scaled = new Vector2(
                relativePosition.X * _resolutionScale.X,
                relativePosition.Y * _resolutionScale.Y
            );
            Vector2 newPosition = newCenter + scaled;

            position.X = newPosition.X;
            position.Y = newPosition.Y;

            return position;
        }

        public void SetCustomScaling(UIElement element, PositionScalingHandler customPositionScaling)
        {
            if (customPositionScaling is not null
                && _positionByElement.ContainsKey(element))
            {
                _positionScalingByElement[element] = customPositionScaling;
                _positionByElement[element] = customPositionScaling.Invoke(element);

                element.SetCanvasAbsolutePosition(_positionByElement[element]);
            }
        }

        private void OnClientSizeChanged(object sender, EventArgs eventArgs)
        {
            Size2 resolution = _game.Window.ClientBounds.Size;

            SetNewResolution(resolution);
            SetScale(_currentResolution, _previousResolution);

            foreach (KeyValuePair<UIElement, Vector3> kvp in _positionByElement)
            {
                UIElement element = kvp.Key;
                Vector3 position = kvp.Value;
                bool isSquare = element.Width == element.Height;

                element.ScaleSize(_resolutionScale, isSquare);

                if (!_positionScalingByElement.ContainsKey(element))
                {
                    position = ScalePosition(position);
                    element.SetCanvasAbsolutePosition(position);

                    _positionByElement[element] = position;
                }
            }

            foreach (KeyValuePair<UIElement, PositionScalingHandler> kvp in _positionScalingByElement)
            {
                UIElement element = kvp.Key;
                PositionScalingHandler customPositionScaling = kvp.Value;
                Vector3 position = customPositionScaling(element);

                _positionByElement[element] = position;
                element.SetCanvasAbsolutePosition(position);
            }
        }

        private void Add(UIElement element, Vector2 resolutionScale)
        {
            bool isSquare = element.Width == element.Height;

            element.ScaleSize(resolutionScale, isSquare);
            element.ScaleMargin(resolutionScale);
            Vector3 position = element.GetLocalPosition(_currentResolution);
            element.Margin = new Thickness(0, 0, 0, 0);
            element.SetCanvasAbsolutePosition(position);

            _positionByElement.Add(element, position);

            element.VisualChildren.ForEach(child => Add(child, resolutionScale));
        }

        private void SetNewResolution(Size2 resolution)
        {
            _previousResolution = _currentResolution;
            _currentResolution = resolution;

            Span<UIComponent> uis = CollectionsMarshal.AsSpan(_uis);

            for (int index = 0; index < uis.Length; index++)
            {
                UIComponent ui = uis.DangerousGetReferenceAt(index);

                ui.Resolution = new Vector3(resolution.Width, resolution.Height, ui.Resolution.Z);
            }
        }

        private void SetScale(Size2 currentResolution, Size2 previousResolution)
        {
            _resolutionScale.X = (float)currentResolution.Width / previousResolution.Width;
            _resolutionScale.Y = (float)currentResolution.Height / previousResolution.Height;
        }
    }
}
