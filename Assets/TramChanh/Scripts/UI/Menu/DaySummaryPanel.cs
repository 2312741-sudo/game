using System;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>End-of-day card: orders served and failed, with a Continue button. Pure view; the owner pauses and resumes the game.</summary>
    public sealed class DaySummaryPanel
    {
        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly Label _body;

        public DaySummaryPanel(VisualElement root, Action onContinue)
        {
            _panel = new VisualElement { name = "DaySummary" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f;
            _panel.style.right = 0f;
            _panel.style.top = 0f;
            _panel.style.bottom = 0f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;
            _panel.style.display = DisplayStyle.None;

            var box = new VisualElement();
            box.style.width = 440f;
            box.style.paddingTop = 24f;
            box.style.paddingBottom = 24f;
            box.style.paddingLeft = 28f;
            box.style.paddingRight = 28f;
            box.style.backgroundColor = new Color(0.08f, 0.18f, 0.13f, 0.95f); // Deep Tea Green
            box.style.borderTopWidth = 2.5f;
            box.style.borderBottomWidth = 2.5f;
            box.style.borderLeftWidth = 2.5f;
            box.style.borderRightWidth = 2.5f;
            box.style.borderTopColor = new Color(0.95f, 0.48f, 0.10f, 0.90f); // Brand Orange
            box.style.borderBottomColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderLeftColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderRightColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderTopLeftRadius = 16f;
            box.style.borderTopRightRadius = 16f;
            box.style.borderBottomLeftRadius = 16f;
            box.style.borderBottomRightRadius = 16f;
            _panel.Add(box);

            _title = new Label();
            _title.style.fontSize = 28f;
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.style.color = new Color(0.99f, 0.78f, 0.15f, 1f); // Lime Gold
            _title.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(_title);

            var statBox = new VisualElement();
            statBox.style.backgroundColor = new Color(0.05f, 0.12f, 0.08f, 0.75f);
            statBox.style.marginTop = 14f;
            statBox.style.marginBottom = 18f;
            statBox.style.paddingTop = 14f;
            statBox.style.paddingBottom = 14f;
            statBox.style.paddingLeft = 16f;
            statBox.style.paddingRight = 16f;
            statBox.style.borderTopLeftRadius = 10f;
            statBox.style.borderTopRightRadius = 10f;
            statBox.style.borderBottomLeftRadius = 10f;
            statBox.style.borderBottomRightRadius = 10f;
            box.Add(statBox);

            _body = new Label();
            _body.style.fontSize = 17f;
            _body.style.color = new Color(0.95f, 0.96f, 0.94f, 1f); // Clean cream
            _body.style.whiteSpace = WhiteSpace.Normal;
            _body.style.unityTextAlign = TextAnchor.MiddleCenter;
            statBox.Add(_body);

            var button = new Button(() => onContinue?.Invoke()) { text = "Ngày mới" };
            button.style.fontSize = 19f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.height = 44f;
            button.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f); // Brand Orange
            button.style.color = Color.white;
            button.style.borderTopLeftRadius = 8f;
            button.style.borderTopRightRadius = 8f;
            button.style.borderBottomLeftRadius = 8f;
            button.style.borderBottomRightRadius = 8f;
            button.style.borderTopWidth = 1.5f;
            button.style.borderBottomWidth = 1.5f;
            button.style.borderLeftWidth = 1.5f;
            button.style.borderRightWidth = 1.5f;
            button.style.borderTopColor = new Color(1.0f, 0.68f, 0.28f, 1f);
            button.style.borderBottomColor = new Color(0.80f, 0.38f, 0.05f, 1f);
            button.style.borderLeftColor = new Color(1.0f, 0.68f, 0.28f, 1f);
            button.style.borderRightColor = new Color(0.80f, 0.38f, 0.05f, 1f);
            box.Add(button);

            root.Add(_panel);
        }

        public bool IsVisible => _panel.style.display.value == DisplayStyle.Flex;

        /// <summary>Text of the summary card for one finished day.</summary>
        public static string Describe(DaySummary summary, GameProgress progress)
        {
            string text = "Đã phục vụ: " + summary.Served + "\nKhách bỏ đi: " + summary.Failed
                + "\nTỉ lệ phục vụ: " + summary.ServedPercent + "%";
            if (progress != null)
            {
                text += "\n\nTổng cộng: " + progress.TotalServed + " đơn  |  Ngày tốt nhất: " + progress.BestServedInADay + " đơn";
            }
            return text;
        }

        public void Show(DaySummary summary, GameProgress progress)
        {
            _title.text = "Hết ngày " + summary.Day;
            _body.text = Describe(summary, progress);
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Hide() { _panel.style.display = DisplayStyle.None; }
    }
}
