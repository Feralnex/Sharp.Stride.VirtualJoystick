using Stride.Core.Mathematics;
using Stride.UI;
using System;

namespace Sharp.Stride.VirtualJoystick.Scripts.Extensions
{
    public static class UIElementExtensions
    {
        public static Vector3 GetLocalPosition(this UIElement element, Size2 resolution)
        {
            Vector3 position = element.GetCanvasAbsolutePosition();
            Thickness margin = element.Margin;
            Vector2 parentSize = new Vector2(element.Parent.GetWidth(resolution.Width), element.Parent.GetHeight(resolution.Height));
            Vector2 size = new Vector2(element.GetWidth(resolution.Width), element.GetHeight(resolution.Height));

            if (element.HorizontalAlignment == HorizontalAlignment.Stretch ||
                element.HorizontalAlignment == HorizontalAlignment.Center)
                position.X += parentSize.X / 2 + margin.Left / 2 - margin.Right / 2 - size.X / 2;
            else if (element.HorizontalAlignment == HorizontalAlignment.Left)
                position.X += margin.Left;
            else if (element.HorizontalAlignment == HorizontalAlignment.Right)
                position.X += parentSize.X - margin.Right - size.X;

            if (element.VerticalAlignment == VerticalAlignment.Stretch ||
                element.VerticalAlignment == VerticalAlignment.Center)
                position.Y += parentSize.Y / 2 + margin.Top / 2 - margin.Bottom / 2 - size.Y / 2;
            else if (element.VerticalAlignment == VerticalAlignment.Top)
                position.Y += margin.Top;
            else if (element.VerticalAlignment == VerticalAlignment.Bottom)
                position.Y += parentSize.Y - margin.Bottom - size.Y;

            return position;
        }

        public static float GetWidth(this UIElement element, float defaultWidth)
        {
            if (element is not null)
            {
                if (float.IsNaN(element.Width))
                    return defaultWidth;
                else
                    return element.Width;
            }
            else
            {
                return defaultWidth;
            }
        }

        public static float GetHeight(this UIElement element, float defaultHeight)
        {
            if (element is not null)
            {
                if (float.IsNaN(element.Height))
                    return defaultHeight;
                else
                    return element.Height;
            }
            else
            {
                return defaultHeight;
            }
        }

        public static void ScaleSize(this UIElement element, Vector2 resolutionScale, bool isSquare)
        {
            if (isSquare)
                element.ScaleSquare(resolutionScale);
            else
                element.ScaleSize(resolutionScale);
        }

        public static void ScaleSquare(this UIElement element, Vector2 resolutionScale)
        {
            float size = float.IsNaN(element.Width)
                    ? element.Height
                    : element.Width;
            bool hasSize = !float.IsNaN(size);

            if (hasSize)
            {
                float scale = Math.Min(resolutionScale.X, resolutionScale.Y);

                element.Width = size * scale;
                element.Height = size * scale;
            }
        }

        public static void ScaleSize(this UIElement element, Vector2 resolutionScale)
        {
            if (!float.IsNaN(element.Width))
                element.Width *= resolutionScale.X;
            if (!float.IsNaN(element.Height))
                element.Height *= resolutionScale.Y;
        }

        public static void ScaleMargin(this UIElement element, Vector2 resolutionScale)
        {
            element.Margin = new Thickness(
                element.Margin.Left * resolutionScale.X,
                element.Margin.Top * resolutionScale.Y,
                element.Margin.Right * resolutionScale.X,
                element.Margin.Bottom * resolutionScale.Y);
        }
    }
}
