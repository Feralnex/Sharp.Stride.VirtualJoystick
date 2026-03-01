using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using System;

namespace Sharp.Stride.VirtualJoystick.Scripts.Extensions
{
    public static class UIElementExtensions
    {
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

            control.ScalePadding(originalPadding, resolutionScale);
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
