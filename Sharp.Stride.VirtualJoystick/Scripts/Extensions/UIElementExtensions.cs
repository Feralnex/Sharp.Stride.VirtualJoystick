using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
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

        public static void ScaleSize(this UIElement element, Vector2 originalSize, Vector2 resolutionScale, bool isSquare)
        {
            if (isSquare)
                element.ScaleSquareSize(originalSize, resolutionScale);
            else
                element.ScaleSize(originalSize, resolutionScale);
        }

        public static void ScaleSquareSize(this UIElement element, Vector2 originalSize, Vector2 resolutionScale)
        {
            float size = float.IsNaN(originalSize.X)
                    ? originalSize.Y
                    : originalSize.X;
            bool hasSize = !float.IsNaN(size);

            if (hasSize)
            {
                float scale = Math.Min(resolutionScale.X, resolutionScale.Y);

                element.Width = size * scale;
                element.Height = size * scale;
            }
        }

        public static void ScaleSize(this UIElement element, Vector2 originalSize, Vector2 resolutionScale)
        {
            if (!float.IsNaN(element.Width))
                element.Width = originalSize.X * resolutionScale.X;
            if (!float.IsNaN(element.Height))
                element.Height = originalSize.Y * resolutionScale.Y;
        }

        public static void ScaleMargin(this UIElement element, Thickness originalMargin, Vector2 resolutionScale, bool isSquare)
        {
            if (isSquare)
            {
                float scale = Math.Min(resolutionScale.X, resolutionScale.Y);

                resolutionScale.X = scale;
                resolutionScale.Y = scale;
            }

            element.ScaleMargin(originalMargin, resolutionScale);
        }

        public static void ScaleMargin(this UIElement element, Thickness originalMargin, Vector2 resolutionScale)
        {
            element.Margin = new Thickness(
                originalMargin.Left * resolutionScale.X,
                originalMargin.Top * resolutionScale.Y,
                originalMargin.Right * resolutionScale.X,
                originalMargin.Bottom * resolutionScale.Y);
        }

        public static void ScalePadding(this Control control, Thickness originalPadding, Vector2 resolutionScale, bool isSquare)
        {
            if (isSquare)
            {
                float scale = Math.Min(resolutionScale.X, resolutionScale.Y);

                resolutionScale.X = scale;
                resolutionScale.Y = scale;
            }

            control.ScaleMargin(originalPadding, resolutionScale);
        }

        public static void ScalePadding(this Control control, Thickness originalPadding, Vector2 resolutionScale)
        {
            control.Padding = new Thickness(
                originalPadding.Left * resolutionScale.X,
                originalPadding.Top * resolutionScale.Y,
                originalPadding.Right * resolutionScale.X,
                originalPadding.Bottom * resolutionScale.Y);
        }
    }
}
