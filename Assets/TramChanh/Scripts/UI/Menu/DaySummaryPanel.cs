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
            box.style.width = 420f;
            box.style.paddingTop = 20f;
            box.style.paddingBottom = 20f;
            box.style.paddingLeft = 24f;
            box.style.paddingRight = 24f;
            box.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            _panel.Add(box);

            _title = new Label();
            _title.style.fontSize = 26f;
            _title.style.color = Color.white;
            _title.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(_title);

            _body = new Label();
            _body.style.fontSize = 18f;
            _body.style.color = Color.white;
            _body.style.whiteSpace = WhiteSpace.Normal;
            _body.style.unityTextAlign = TextAnchor.MiddleCenter;
            _body.style.marginTop = 14f;
            _body.style.marginBottom = 14f;
            box.Add(_body);

            var button = new Button(() => onContinue?.Invoke()) { text = "Ngày mới" };
            button.style.fontSize = 18f;
            button.style.height = 38f;
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
