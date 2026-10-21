using TramChanh.Orders;
using TramChanh.UI.Menu;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Hud
{
    /// <summary>How an order stage reads at a glance; drives the colour edge of an order card.</summary>
    public enum StageTone { Waiting, Working, Ready, Done, Failed }

    /// <summary>
    /// The single style source of the in-game HUD (order list, held item, toast, notice, prompt, clock readout).
    /// Colours come from <see cref="UiStyle"/> (tea green card, orange edge, lime gold) so the HUD, the toast and
    /// the prompt look alike and match the menus. Presentation only: no game logic and no text.
    /// </summary>
    public static class HudTheme
    {
        // Font sizes (px). One scale for every HUD element.
        public const float TitleSize = 17f;
        public const float BodySize = 15f;
        public const float SmallSize = 13f;
        public const float ReadoutSize = 20f;
        public const float PromptSize = 22f;
        public const float ReasonSize = 16f;

        // Layout (px). Right column starts below the [F1] hint, left column below the controls legend.
        public const float Margin = 20f;
        public const float LeftColumnTop = 56f;
        public const float RightColumnTop = 56f;
        public const float NoticeTop = 64f;

        public static readonly Color Text = UiStyle.Cream;
        public static readonly Color TextDim = UiStyle.Dim;
        public static readonly Color Hint = UiStyle.Gold;
        public static readonly Color Edge = UiStyle.Orange;
        public static readonly Color PrimaryCard = new Color(0.95f, 0.48f, 0.10f, 0.22f);
        public static readonly Color PlainCard = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color Reason = new Color(1f, 0.70f, 0.52f, 1f);
        public static readonly Color ReasonFill = new Color(0.55f, 0.15f, 0.10f, 0.55f);
        public static readonly Color ToastGood = new Color(0.10f, 0.42f, 0.22f, 0.92f);
        public static readonly Color ToastBad = new Color(0.55f, 0.15f, 0.10f, 0.92f);
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.15f);

        public static StageTone ToneFor(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Ready:
                case OrderStatus.PickedUpByLobby:
                    return StageTone.Ready;
                case OrderStatus.Delivered:
                case OrderStatus.Completed:
                    return StageTone.Done;
                case OrderStatus.Failed:
                    return StageTone.Failed;
                case OrderStatus.SentToStall:
                case OrderStatus.InPreparation:
                    return StageTone.Working;
                default:
                    return StageTone.Waiting;
            }
        }

        public static Color ToneColor(StageTone tone)
        {
            switch (tone)
            {
                case StageTone.Ready: return new Color(0.30f, 0.80f, 0.35f, 1f);
                case StageTone.Done: return new Color(0.55f, 0.70f, 0.60f, 1f);
                case StageTone.Failed: return new Color(0.90f, 0.25f, 0.18f, 1f);
                case StageTone.Working: return Edge;
                default: return UiStyle.Gold;
            }
        }

        public static Color PatienceColor(PatienceLevel level)
        {
            switch (level)
            {
                case PatienceLevel.Danger: return new Color(0.90f, 0.20f, 0.15f, 0.95f);
                case PatienceLevel.Warning: return new Color(0.98f, 0.65f, 0.10f, 0.95f);
                default: return new Color(0.30f, 0.80f, 0.35f, 0.95f);
            }
        }

        public static Color ToastColor(bool positive) => positive ? ToastGood : ToastBad;

        public static Label NewLabel(float size, Color color, bool bold = false)
        {
            var label = new Label();
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold) { label.style.unityFontStyleAndWeight = FontStyle.Bold; }
            return label;
        }

        /// <summary>A HUD side panel: the shared card, absolute, never catches the pointer.</summary>
        public static VisualElement NewPanel(float width)
        {
            var panel = new VisualElement();
            panel.pickingMode = PickingMode.Ignore;
            panel.style.position = Position.Absolute;
            panel.style.width = width;
            UiStyle.MakeCard(panel, 12f, 9f);
            return panel;
        }

        /// <summary>The order toast and the top notice share this: centred, rounded, white edge, bold.</summary>
        public static void StyleToast(Label label, float width, bool positive)
        {
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.left = Length.Percent(50f);
            label.style.width = width;
            label.style.marginLeft = -width * 0.5f;
            label.style.fontSize = 22f;
            label.style.color = Color.white;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.paddingTop = 9f; label.style.paddingBottom = 9f;
            label.style.paddingLeft = 14f; label.style.paddingRight = 14f;
            UiStyle.Round(label, 14f);
            UiStyle.Border(label, new Color(1f, 1f, 1f, 0.35f), 1.5f);
            label.style.backgroundColor = ToastColor(positive);
        }

        /// <summary>The clock / money / star line at the top centre.</summary>
        public static void StyleReadout(Label label)
        {
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.top = 14f;
            label.style.left = Length.Percent(50f);
            label.style.width = 580f;
            label.style.marginLeft = -290f;
            label.style.fontSize = ReadoutSize;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = UiStyle.Gold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.backgroundColor = UiStyle.Card;
            UiStyle.Border(label, UiStyle.CardEdge, 1.5f);
            UiStyle.Round(label, 18f);
            label.style.paddingTop = 5f; label.style.paddingBottom = 5f;
        }

        /// <summary>Small corner chip (controls legend, [F1] hint).</summary>
        public static void StyleChip(Label label, float size)
        {
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.top = Margin;
            label.style.fontSize = size;
            label.style.color = UiStyle.Dim;
            label.style.backgroundColor = new Color(0.07f, 0.16f, 0.12f, 0.70f);
            label.style.paddingTop = 4f; label.style.paddingBottom = 4f;
            label.style.paddingLeft = 9f; label.style.paddingRight = 9f;
            UiStyle.Round(label, 8f);
        }

        /// <summary>Why an action is refused: warm text on a soft red pill, so it reads as a reason, not as the action.</summary>
        public static void StyleReason(Label label)
        {
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = ReasonSize;
            label.style.color = Reason;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.backgroundColor = ReasonFill;
            label.style.marginTop = 4f;
            label.style.paddingTop = 2f; label.style.paddingBottom = 2f;
            label.style.paddingLeft = 10f; label.style.paddingRight = 10f;
            UiStyle.Round(label, 8f);
        }

        /// <summary>Sets a reason's text and hides the pill when it is empty (an empty pill would show as a stray bar).</summary>
        public static void SetReasonText(Label label, string text)
        {
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(label.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Colours a hold ProgressBar in the brand orange on a dark track.</summary>
        public static void StyleProgress(ProgressBar bar)
        {
            bar.pickingMode = PickingMode.Ignore;
            bar.style.marginTop = 4f;
            VisualElement fill = bar.Q<VisualElement>(className: "unity-progress-bar__progress");
            if (fill != null) { fill.style.backgroundColor = UiStyle.Orange; }
            VisualElement background = bar.Q<VisualElement>(className: "unity-progress-bar__background");
            if (background != null)
            {
                background.style.backgroundColor = Track;
                UiStyle.Border(background, UiStyle.CardEdge, 1f);
            }
            Label title = bar.Q<Label>(className: "unity-progress-bar__title");
            if (title != null) { title.style.color = Color.white; }
        }

        /// <summary>The aim dot: a ring that grows and turns orange when something can be used.</summary>
        public static void StyleCrosshair(VisualElement dot, bool active)
        {
            dot.pickingMode = PickingMode.Ignore;
            float size = active ? 12f : 8f;
            dot.style.width = size; dot.style.height = size;
            dot.style.marginLeft = -size * 0.5f; dot.style.marginTop = -size * 0.5f;
            dot.style.backgroundColor = active ? UiStyle.Orange : new Color(1f, 1f, 1f, 0.9f);
            UiStyle.Round(dot, size * 0.5f);
            UiStyle.Border(dot, active ? Color.white : new Color(0f, 0f, 0f, 0.55f), 1.5f);
        }
    }
}
