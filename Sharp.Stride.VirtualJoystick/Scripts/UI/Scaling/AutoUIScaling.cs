using Sharp.Stride.VirtualJoystick.Scripts.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using System;
using System.Collections.Generic;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private readonly Game _game;
        private readonly Dictionary<UIComponent, Data> _uiData = new();
        private Size2 _currentResolution;

        public AutoUIScaling(Game game)
        {
            _game = game;
            _uiData = new Dictionary<UIComponent, Data>();
            _currentResolution = game.Window.ClientBounds.Size;

            _game.Window.ClientSizeChanged += OnClientSizeChanged;
        }

        ~AutoUIScaling()
        {
            _game.Window.ClientSizeChanged -= OnClientSizeChanged;
        }

        public void Add(UIComponent ui, Size2 designResolution)
        {
            Data data = new Data(designResolution);

            _uiData[ui] = data;
            ui.Resolution = new Vector3(_currentResolution.Width, _currentResolution.Height, ui.Resolution.Z);

            CacheOriginalLayout(ui.Page.RootElement, data);
            ApplyScaling(ui, data);
        }

        private void CacheOriginalLayout(UIElement element, Data data)
        {
            data.OriginalMarginByElement[element] = element.Margin;
            data.OriginalSizeByElement[element] = new Vector2(element.Width, element.Height);

            foreach (UIElement child in element.VisualChildren)
                CacheOriginalLayout(child, data);
        }

        private void ApplyScaling(UIComponent ui, Data data)
        {
            Vector2 resolutionScale = new Vector2(
                (float)_currentResolution.Width / data.DesignResolution.Width,
                (float)_currentResolution.Height / data.DesignResolution.Height
            );

            ApplyScalingRecursive(ui.Page.RootElement, data, resolutionScale);
        }

        private void ApplyScalingRecursive(UIElement element, Data data, Vector2 resolutioScale)
        {
            Vector2 originalSize = data.OriginalSizeByElement[element];
            Thickness originalMargin = data.OriginalMarginByElement[element];
            bool isSquare = originalSize.X == originalSize.Y;

            element.ScaleSize(originalSize, resolutioScale, isSquare);
            element.ScaleMargin(originalMargin, resolutioScale, isSquare);

            foreach (UIElement child in element.VisualChildren)
                ApplyScalingRecursive(child, data, resolutioScale);
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            _currentResolution = _game.Window.ClientBounds.Size;

            foreach (KeyValuePair<UIComponent, Data> kvp in _uiData)
            {
                UIComponent ui = kvp.Key;
                Data data = kvp.Value;

                ui.Resolution = new Vector3(_currentResolution.Width, _currentResolution.Height, ui.Resolution.Z);

                ApplyScaling(ui, data);
            }
        }
    }
}