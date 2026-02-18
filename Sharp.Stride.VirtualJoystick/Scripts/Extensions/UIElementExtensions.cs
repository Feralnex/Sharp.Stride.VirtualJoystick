using Stride.Core.Mathematics;
using Stride.Games;
using Stride.UI;

namespace Sharp.Stride.VirtualJoystick.Scripts.Extensions
{
    public static class UIElementExtensions
    {
        public static Vector3 GetLocalPosition(this UIElement element, GameWindow gameWindow)
        {
            Vector3 position = element.GetCanvasAbsolutePosition();
            Thickness margin = element.Margin;
            Vector2 parentSize = new Vector2(element.Parent.GetWidth(gameWindow), element.Parent.GetHeight(gameWindow));
            Vector2 size = new Vector2(element.GetWidth(gameWindow), element.GetHeight(gameWindow));

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

        public static float GetWidth(this UIElement element, GameWindow gameWindow)
        {
            if (element is not null)
            {
                if (float.IsNaN(element.Width))
                    return element.Parent.GetWidth(gameWindow);
                else
                    return element.Width;
            }
            else
            {
                return gameWindow.ClientBounds.Size.Width;
            }
        }

        public static float GetHeight(this UIElement element, GameWindow gameWindow)
        {
            if (element is not null)
            {
                if (float.IsNaN(element.Height))
                    return element.Parent.GetHeight(gameWindow);
                else
                    return element.Height;
            }
            else
            {
                return gameWindow.ClientBounds.Size.Height;
            }
        }
    }
}
