using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>The shared look of the in-game UI: deep tea green cards, a thin orange edge, rounded corners. Style only, no logic.</summary>
    public static class UiStyle
    {
        public static readonly Color Card = new Color(0.07f, 0.16f, 0.12f, 0.88f);
        public static readonly Color CardEdge = new Color(0.95f, 0.48f, 0.10f, 0.75f);
        public static readonly Color Gold = new Color(0.99f, 0.82f, 0.28f, 1f);
        public static readonly Color Cream = new Color(0.95f, 0.96f, 0.94f, 1f);
        public static readonly Color Dim = new Color(0.78f, 0.86f, 0.80f, 0.85f);

        public static void Border(VisualElement e, Color color, float width)
        {
            e.style.borderTopWidth = width; e.style.borderBottomWidth = width; e.style.borderLeftWidth = width; e.style.borderRightWidth = width;
            e.style.borderTopColor = color; e.style.borderBottomColor = color; e.style.borderLeftColor = color; e.style.borderRightColor = color;
        }

        public static void Round(VisualElement e, float radius)
        {
            e.style.borderTopLeftRadius = radius; e.style.borderTopRightRadius = radius;
            e.style.borderBottomLeftRadius = radius; e.style.borderBottomRightRadius = radius;
        }

        /// <summary>Turns an element into a card: green fill, orange edge, rounded, padded.</summary>
        public static void MakeCard(VisualElement e, float radius = 12f, float padding = 10f)
        {
            e.style.backgroundColor = Card;
            Border(e, CardEdge, 1.5f);
            Round(e, radius);
            e.style.paddingTop = padding; e.style.paddingBottom = padding;
            e.style.paddingLeft = padding + 4f; e.style.paddingRight = padding + 4f;
        }

        public static readonly Color Orange = new Color(0.95f, 0.48f, 0.10f, 1f);
        public static readonly Color OrangeHover = new Color(1f, 0.58f, 0.22f, 1f);
        public static readonly Color Green = new Color(0.12f, 0.24f, 0.18f, 0.95f);
        public static readonly Color GreenHover = new Color(0.18f, 0.34f, 0.25f, 0.98f);

        /// <summary>Gives a button the shared look: rounded, edged, bold when primary, lighter on hover.</summary>
        public static void StyleButton(Button button, bool primary)
        {
            Color normal = primary ? Orange : Green;
            Color hover = primary ? OrangeHover : GreenHover;
            button.style.backgroundColor = normal;
            button.style.color = primary ? Color.white : Cream;
            button.style.unityFontStyleAndWeight = primary ? FontStyle.Bold : FontStyle.Normal;
            button.style.minHeight = 38f;
            Round(button, 9f);
            Border(button, primary ? new Color(1f, 0.68f, 0.28f, 1f) : new Color(0.24f, 0.44f, 0.34f, 0.8f), 1.5f);
            button.style.marginLeft = 4f;
            button.style.marginRight = 4f;
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.backgroundColor = hover);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = normal);
        }
    }
}
