using System;
using System.Collections.Generic;
using System.Text;
using TramChanh.Core;
using TramChanh.Orders;
using TramChanh.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Orders
{
    /// <summary>
    /// Pre-filled order-entry panel with separate Enter and Send confirmations (DEC-003, DEC-012).
    /// It publishes typed events only; <see cref="Shown"/>/<see cref="Hidden"/> let the host pause or
    /// capture the cursor. Note: if the host frees the cursor through PlayerInputReader, its Interact
    /// action (E / left mouse) re-captures it, so a pointer click needs a host-side guard.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OrderEntryUI : MonoBehaviour
    {
        public const string TitleKey = "order.entry.title";
        public const string EnterKey = "order.entry.enter";
        public const string SendKey = "order.entry.send";
        public const string CloseKey = "order.entry.close";
        public const string ItemKeyPrefix = "item.";

        [SerializeField] private PromptLocalizationTable _table;
        [SerializeField] private string _language = "en";
        private OrderEntryModel _model;
        private VisualElement _panel;
        private Label _title;
        private Label _items;
        private Label _reason;
        private Button _enter;
        private Button _send;
        private Button _close;
        private bool _wasOpen;

        public event Action Shown;
        public event Action Hidden;

        public PromptLocalizationTable Table => _table;
        public string Language => _language;
        public bool IsOpen => _model != null && _model.IsOpen;
        public OrderId OrderId => _model != null ? _model.OrderId : default;
        public IReadOnlyList<ItemRequest> Items => _model != null ? _model.Items : Array.Empty<ItemRequest>();
        public string BlockedReasonKey => _model?.BlockedReasonKey;
        public string TitleText => Localize(TitleKey);
        public string ReasonText => string.IsNullOrEmpty(BlockedReasonKey) ? string.Empty : Localize(BlockedReasonKey);

        public string ItemsText
        {
            get
            {
                var text = new StringBuilder();
                foreach (ItemRequest item in Items)
                {
                    if (text.Length > 0) { text.Append('\n'); }
                    text.Append(Localize(ItemKeyPrefix + item.ItemDefinitionId)).Append("  x").Append(item.Quantity);
                }
                return text.ToString();
            }
        }

        public void Initialize(IEventBus events)
        {
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            Disconnect();
            _model = new OrderEntryModel(events);
            _model.Changed += OnModelChanged;
            _wasOpen = false;
            Render();
        }

        /// <summary>T3 confirmation. Returns false when no entry is open.</summary>
        public bool Enter() => _model != null && _model.Enter();

        /// <summary>T4 confirmation. Returns false when no entry is open.</summary>
        public bool Send() => _model != null && _model.Send();

        /// <summary>Hides the panel without changing the order.</summary>
        public void Cancel() => _model?.Close();

        public void Disconnect()
        {
            if (_model == null) { return; }
            _model.Changed -= OnModelChanged;
            _model.Dispose();
            _model = null;
            if (_wasOpen)
            {
                _wasOpen = false;
                RaiseHidden();
            }
            Render();
        }

        private string Localize(string key) => _table != null ? _table.Resolve(key, _language) : key;

        private void OnModelChanged()
        {
            bool open = _model != null && _model.IsOpen;
            Render();
            if (open && !_wasOpen)
            {
                _wasOpen = true;
                try { Shown?.Invoke(); } catch (Exception fault) { Debug.LogException(fault); }
            }
            else if (!open && _wasOpen)
            {
                _wasOpen = false;
                RaiseHidden();
            }
        }

        private void RaiseHidden()
        {
            try { Hidden?.Invoke(); } catch (Exception fault) { Debug.LogException(fault); }
        }

        private void Start() => BuildView();

        private void OnEnable()
        {
            if (_panel != null) { BuildView(); }
        }

        private void BuildView()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) { return; }
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            _panel = new VisualElement();
            _panel.style.position = Position.Absolute;
            _panel.style.left = Length.Percent(50f);
            _panel.style.top = Length.Percent(50f);
            _panel.style.width = 560f;
            _panel.style.marginLeft = -280f;
            _panel.style.marginTop = -160f;
            _panel.style.paddingTop = 16f;
            _panel.style.paddingBottom = 16f;
            _panel.style.paddingLeft = 20f;
            _panel.style.paddingRight = 20f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.85f);
            _title = NewLabel(26f);
            _items = NewLabel(20f);
            _items.style.marginTop = 12f;
            _items.style.marginBottom = 12f;
            _reason = NewLabel(16f);
            _reason.style.color = new Color(1f, 0.6f, 0.4f);
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.SpaceBetween;
            _enter = NewButton(() => Enter());
            _send = NewButton(() => Send());
            _close = NewButton(Cancel);
            buttons.Add(_enter);
            buttons.Add(_send);
            buttons.Add(_close);
            _panel.Add(_title);
            _panel.Add(_items);
            _panel.Add(_reason);
            _panel.Add(buttons);
            root.Add(_panel);
            Render();
        }

        private static Label NewLabel(float size)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.color = Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Button NewButton(Action onClick)
        {
            var button = new Button(onClick);
            button.style.flexGrow = 1f;
            button.style.fontSize = 18f;
            button.style.whiteSpace = WhiteSpace.Normal;
            return button;
        }

        private void Render()
        {
            if (_panel == null) { return; }
            _panel.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _title.text = TitleText;
            _items.text = ItemsText;
            _reason.text = ReasonText;
            _enter.text = Localize(EnterKey);
            _send.text = Localize(SendKey);
            _close.text = Localize(CloseKey);
        }

        private void OnDestroy() => Disconnect();
    }
}
