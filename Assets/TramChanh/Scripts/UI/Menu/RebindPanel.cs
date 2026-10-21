using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// "Phím điều khiển" panel (UI Toolkit, built in code like <see cref="PauseMenu"/>): one row per rebindable key with its current
    /// key, a button to change it, a per-row reset, a swap-on-conflict toggle, "Đặt lại tất cả" and "Quay lại".
    /// Pure view over a <see cref="RebindService"/>; the owner decides when to show it and what Back does.
    /// </summary>
    public sealed class RebindPanel
    {
        private sealed class Row
        {
            public RebindSlot Slot;
            public Button KeyButton;
        }

        private readonly VisualElement _panel;
        private readonly RebindService _service;
        private readonly List<Row> _rows = new List<Row>();
        private readonly Label _status;
        private readonly Toggle _swap;
        private readonly Button _cancel;
        private string _listeningSlotId;

        public RebindPanel(VisualElement root, RebindService service, Action onBack, string titleText = "PHÍM ĐIỀU KHIỂN")
        {
            if (root == null) { throw new ArgumentNullException(nameof(root)); }
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _panel = new VisualElement { name = "RebindPanel" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f; _panel.style.right = 0f; _panel.style.top = 0f; _panel.style.bottom = 0f;
            _panel.style.backgroundColor = new Color(0.03f, 0.06f, 0.05f, 0.80f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;
            _panel.style.display = DisplayStyle.None;

            var box = new VisualElement();
            box.style.width = 520f;
            UiStyle.MakeCard(box, 16f, 22f);
            box.style.backgroundColor = new Color(0.08f, 0.17f, 0.12f, 0.96f);
            _panel.Add(box);

            var title = new Label(titleText);
            title.style.fontSize = 26f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = UiStyle.Orange;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 10f;
            box.Add(title);

            foreach (RebindSlot slot in service.Model.Slots)
            {
                var row = new Row { Slot = slot };
                var line = new VisualElement();
                line.style.flexDirection = FlexDirection.Row;
                line.style.alignItems = Align.Center;
                line.style.marginTop = 5f;

                var name = new Label(slot.Label);
                name.style.flexGrow = 1f;
                name.style.color = UiStyle.Cream;
                name.style.fontSize = 15f;
                line.Add(name);

                var keyButton = new Button(() => OnKeyClicked(slot.Id)) { text = RebindPathText.Display(slot.CurrentPath) };
                UiStyle.StyleButton(keyButton, false);
                keyButton.style.width = 140f;
                row.KeyButton = keyButton;
                line.Add(keyButton);

                var reset = new Button(() => Report(_service.Reset(slot.Id))) { text = "Mặc định" };
                UiStyle.StyleButton(reset, false);
                reset.style.width = 90f;
                line.Add(reset);

                box.Add(line);
                _rows.Add(row);
            }

            _swap = new Toggle("Đổi chỗ khi trùng phím") { value = true };
            _swap.style.marginTop = 12f;
            _swap.style.color = UiStyle.Gold;
            box.Add(_swap);

            _status = new Label(string.Empty);
            _status.style.color = UiStyle.Dim;
            _status.style.fontSize = 13f;
            _status.style.minHeight = 34f;
            _status.style.whiteSpace = WhiteSpace.Normal;
            _status.style.marginTop = 6f;
            _status.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(_status);

            _cancel = new Button(() => _service.CancelRebind()) { text = "Hủy đổi phím" };
            UiStyle.StyleButton(_cancel, false);
            _cancel.style.display = DisplayStyle.None;
            box.Add(_cancel);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.Center;
            buttons.style.marginTop = 8f;
            var resetAll = new Button(() => Report(_service.ResetAll())) { text = "Đặt lại tất cả" };
            UiStyle.StyleButton(resetAll, false);
            var back = new Button(() => { _service.CancelRebind(); SetVisible(false); onBack?.Invoke(); }) { text = "Quay lại" };
            UiStyle.StyleButton(back, true);
            buttons.Add(resetAll);
            buttons.Add(back);
            box.Add(buttons);

            _service.Changed += Refresh;
            root.Add(_panel);
        }

        public bool IsVisible => _panel.style.display.value == DisplayStyle.Flex;
        public bool IsListening => !string.IsNullOrEmpty(_listeningSlotId);
        public string StatusText => _status.text;

        public void SetVisible(bool visible)
        {
            if (visible == IsVisible) { return; }
            if (visible) { Refresh(); SetStatus("Bấm vào một phím để đổi, rồi nhấn phím mới. Esc để hủy."); }
            else { _service.CancelRebind(); }
            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Re-reads every slot's current key into its button.</summary>
        public void Refresh()
        {
            foreach (Row row in _rows)
            {
                row.KeyButton.text = row.Slot.Id == _listeningSlotId ? "Nhấn phím mới..." : RebindPathText.Display(row.Slot.CurrentPath);
            }
        }

        private void OnKeyClicked(string slotId)
        {
            if (_service.IsRebinding) { return; }
            _listeningSlotId = slotId;
            _cancel.style.display = DisplayStyle.Flex;
            SetStatus("Nhấn phím hoặc nút chuột mới...");
            Refresh();
            bool started = _service.BeginRebind(slotId, _swap.value, result =>
            {
                _listeningSlotId = null;
                _cancel.style.display = DisplayStyle.None;
                Report(result);
                Refresh();
            });
            if (!started)
            {
                _listeningSlotId = null;
                _cancel.style.display = DisplayStyle.None;
                SetStatus("Không đổi được phím này.");
                Refresh();
            }
        }

        private void Report(RebindResult result)
        {
            if (result != null && !string.IsNullOrEmpty(result.Message)) { SetStatus(result.Message); }
            Refresh();
        }

        private void SetStatus(string text)
        {
            _status.text = text ?? string.Empty;
        }
    }
}
