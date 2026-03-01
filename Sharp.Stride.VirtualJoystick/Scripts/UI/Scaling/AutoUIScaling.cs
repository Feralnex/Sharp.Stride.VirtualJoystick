using CommunityToolkit.HighPerformance;
using Sharp.Collections.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using System;
using System.Collections.Generic;

namespace Sharp.Stride.VirtualJoystick.Scripts.UI.Scaling
{
    public partial class AutoUIScaling : IAutoUIScaling
    {
        private readonly Game _game;
        private readonly Dictionary<UIComponent, ScalableUIComponent> _scalableComponents;
        private Size2 _currentResolution;

        public AutoUIScaling(Game game)
        {
            _game = game;
            _scalableComponents = new Dictionary<UIComponent, ScalableUIComponent>();
            _currentResolution = game.Window.ClientBounds.Size;

            _game.Window.ClientSizeChanged += OnClientSizeChanged;
        }

        ~AutoUIScaling()
        {
            _game.Window.ClientSizeChanged -= OnClientSizeChanged;
        }

        public void Add(UIComponent component, Size2 designResolution)
        {
            if (!_scalableComponents.TryGetValue(component, out ScalableUIComponent scalableComponent))
            {
                scalableComponent = new ScalableUIComponent(component, designResolution);

                _scalableComponents.Add(component, scalableComponent);

                CacheScalableElements(component.Page.RootElement, scalableComponent);

                scalableComponent.Scale(_currentResolution);
            }
        }

        private static void CacheScalableElements(UIElement element, ScalableUIComponent data)
        {
            ScalableUIElement scalableElement;

            if (element is TextBlock textBlock)
                scalableElement = new ScalableTextBlock(textBlock);
            else if (element is Control control)
                scalableElement = new ScalableControl(control);
            else 
                scalableElement = new ScalableUIElement(element);

            data.ScalableElements.Add(scalableElement);
            
            foreach (UIElement child in element.VisualChildren)
                CacheScalableElements(child, data);
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            _currentResolution = _game.Window.ClientBounds.Size;

            int count = _scalableComponents.Count;
            DictionaryExtensions.Entry<UIComponent, ScalableUIComponent>[] entries = _scalableComponents.GetEntries();

            for (int index = 0; index < count; index++)
            {
                ref DictionaryExtensions.Entry<UIComponent, ScalableUIComponent> entry = ref entries!.DangerousGetReferenceAt(index);

                if (entry.next >= -1)
                {
                    ScalableUIComponent scalableComponent = entry.value;

                    scalableComponent.Scale(_currentResolution);
                }
            }
        }
    }
}