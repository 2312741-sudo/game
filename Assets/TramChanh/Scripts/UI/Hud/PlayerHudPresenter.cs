using System;
using System.Text;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Hud
{
    /// <summary>
    /// Builds the held-item panel, the active-order list and the order toast into an existing UIDocument root
    /// (the InteractionHUD document) with minimal inline styling. Model events only mark the view dirty;
    /// rendering happens in <see cref="Tick"/>, so a HUD fault can never interrupt a gameplay publish.
    /// Visual design (USS/UXML/fonts/art) is intentionally out of scope.
    /// </summary>
    public sealed class PlayerHudPresenter : IDisposable
    {
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color HintColor = new Color(1f, 0.85f, 0.4f);
        private static readonly Color DimColor = new Color(0.8f, 0.8f, 0.8f);
        private readonly Func<string, string> _resolve;
        private readonly HeldItemFeedbackModel _held;
        private readonly ActiveOrderListModel _orders;
        private readonly OrderToastModel _toast;
        private VisualElement _heldPanel;
        private Label _heldName;
        private Label _heldState;
        private Label _heldNext;
        private Label _heldOrder;
        private VisualElement _ordersPanel;
        private Label _ordersTitle;
        private Label _ordersNext;
        private VisualElement _ordersRows;
        private Label _ordersMore;
        private Label _toastLabel;
        private bool _heldDirty = true;
        private bool _ordersDirty = true;
        private bool _toastDirty = true;
        private readonly Func<double> _now;
        private double _nextPoll;
        private bool _disposed;
        /// <summary>Seconds between held-item polls; events refresh immediately, polling only catches timed changes such as cooking.</summary>
        public const double PollInterval = 0.2d;

        public PlayerHudPresenter(VisualElement root, Func<string, string> resolve, Predicate<string> hasKey, IEventBus events,
            IHeldItemSlot hands, IOrderService orders, Func<double> now)
        {
            if (root == null) { throw new ArgumentNullException(nameof(root)); }
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            _resolve = resolve ?? (key => key);
            _now = now ?? (() => Time.unscaledTimeAsDouble);
            if (hands != null)
            {
                _held = new HeldItemFeedbackModel(hands, events, orders, hasKey);
                _held.Changed += () => _heldDirty = true;
            }
            if (orders != null)
            {
                _orders = new ActiveOrderListModel(orders, events);
                _orders.Changed += () => _ordersDirty = true;
                _toast = new OrderToastModel(orders, events, _now);
                _toast.Changed += () => _toastDirty = true;
            }
            Build(root);
            Tick();
        }

        public HeldItemFeedbackModel Held => _held;
        public ActiveOrderListModel Orders => _orders;
        public OrderToastModel Toast => _toast;
        public string HeldText => Join(_heldName, _heldState, _heldNext, _heldOrder);
        public string OrdersText
        {
            get
            {
                var text = new StringBuilder(Join(_ordersTitle, _ordersNext));
                if (_ordersRows != null)
                {
                    foreach (VisualElement child in _ordersRows.Children())
                    {
                        if (child is Label label && !string.IsNullOrEmpty(label.text)) { text.Append('\n').Append(label.text); }
                    }
                }
                if (_ordersMore != null && !string.IsNullOrEmpty(_ordersMore.text)) { text.Append('\n').Append(_ordersMore.text); }
                return text.ToString();
            }
        }
        public string ToastText => _toastLabel != null && _toast != null && _toast.IsVisible ? _toastLabel.text : string.Empty;

        /// <summary>Polls time-based state (cooking, toast expiry) and renders whatever changed.</summary>
        public void Tick()
        {
            if (_disposed) { return; }
            double now = _now();
            if (_held != null && now >= _nextPoll)
            {
                _nextPoll = now + PollInterval;
                _held.Refresh();
            }
            _toast?.Tick();
            if (_heldDirty) { _heldDirty = false; RenderHeld(); }
            if (_ordersDirty) { _ordersDirty = false; RenderOrders(); }
            if (_toastDirty) { _toastDirty = false; RenderToast(); }
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;
            _held?.Dispose();
            _orders?.Dispose();
            _toast?.Dispose();
            _heldPanel?.RemoveFromHierarchy();
            _ordersPanel?.RemoveFromHierarchy();
            _toastLabel?.RemoveFromHierarchy();
        }

        private string Text(HudText text) => text.Render(_resolve);

        private void Build(VisualElement root)
        {
            if (_held != null)
            {
                _heldPanel = Panel();
                _heldPanel.style.right = 20f;
                _heldPanel.style.top = 20f;
                _heldPanel.style.width = 330f;
                var title = NewLabel(13f, DimColor);
                title.text = Text(HudText.Of(HudKeys.HeldTitle));
                _heldName = NewLabel(20f, Color.white);
                _heldState = NewLabel(15f, Color.white);
                _heldNext = NewLabel(15f, HintColor);
                _heldOrder = NewLabel(14f, DimColor);
                _heldPanel.Add(title);
                _heldPanel.Add(_heldName);
                _heldPanel.Add(_heldState);
                _heldPanel.Add(_heldNext);
                _heldPanel.Add(_heldOrder);
                root.Add(_heldPanel);
            }
            if (_orders != null)
            {
                _ordersPanel = Panel();
                _ordersPanel.style.left = 20f;
                _ordersPanel.style.top = 52f;
                _ordersPanel.style.width = 380f;
                _ordersTitle = NewLabel(17f, Color.white);
                _ordersNext = NewLabel(16f, HintColor);
                _ordersNext.style.marginBottom = 6f;
                _ordersRows = new VisualElement();
                _ordersMore = NewLabel(13f, DimColor);
                _ordersPanel.Add(_ordersTitle);
                _ordersPanel.Add(_ordersNext);
                _ordersPanel.Add(_ordersRows);
                _ordersPanel.Add(_ordersMore);
                root.Add(_ordersPanel);

                _toastLabel = NewLabel(22f, Color.white);
                _toastLabel.style.position = Position.Absolute;
                _toastLabel.style.top = Length.Percent(18f);
                _toastLabel.style.left = Length.Percent(50f);
                _toastLabel.style.width = 560f;
                _toastLabel.style.marginLeft = -280f;
                _toastLabel.style.paddingTop = 10f;
                _toastLabel.style.paddingBottom = 10f;
                _toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                root.Add(_toastLabel);
            }
        }

        private void RenderHeld()
        {
            if (_held == null || _heldPanel == null) { return; }
            _heldName.text = Text(_held.Name);
            _heldState.text = _held.State.IsEmpty ? string.Empty : Text(HudText.Of(HudKeys.HeldStateLabel, _held.State));
            _heldNext.text = _held.NextAction.IsEmpty ? string.Empty : Text(_held.HasItem ? HudText.Of(HudKeys.HeldNextLabel, _held.NextAction) : _held.NextAction);
            _heldOrder.text = Text(_held.Order);
            SetVisible(_heldState, !_held.State.IsEmpty);
            SetVisible(_heldNext, !_held.NextAction.IsEmpty);
            SetVisible(_heldOrder, !_held.Order.IsEmpty);
        }

        private void RenderOrders()
        {
            if (_orders == null || _ordersPanel == null) { return; }
            _ordersTitle.text = Text(_orders.Title);
            _ordersNext.text = Text(_orders.PrimaryNextStep);
            _ordersRows.Clear();
            foreach (OrderRow row in _orders.Rows)
            {
                Label header = NewLabel(16f, row.IsPrimary ? HintColor : Color.white);
                header.style.marginTop = 4f;
                header.text = Text(row.Header);
                _ordersRows.Add(header);
                var items = new StringBuilder();
                foreach (HudText item in row.Items)
                {
                    if (items.Length > 0) { items.Append(", "); }
                    items.Append(Text(item));
                }
                Label itemLabel = NewLabel(14f, DimColor);
                itemLabel.text = items.ToString();
                _ordersRows.Add(itemLabel);
                Label next = NewLabel(14f, row.IsPrimary ? HintColor : DimColor);
                next.text = Text(row.NextStep);
                _ordersRows.Add(next);
            }
            _ordersMore.text = _orders.HiddenCount > 0 ? Text(HudText.Of(HudKeys.OrdersMore, _orders.HiddenCount)) : string.Empty;
            SetVisible(_ordersMore, _orders.HiddenCount > 0);
        }

        private void RenderToast()
        {
            if (_toast == null || _toastLabel == null) { return; }
            _toastLabel.text = Text(_toast.Current);
            _toastLabel.style.backgroundColor = _toast.IsPositive ? new Color(0.1f, 0.45f, 0.2f, 0.85f) : new Color(0.55f, 0.15f, 0.1f, 0.85f);
            SetVisible(_toastLabel, _toast.IsVisible);
        }

        private static VisualElement Panel()
        {
            var panel = new VisualElement();
            panel.pickingMode = PickingMode.Ignore;
            panel.style.position = Position.Absolute;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 8f;
            panel.style.paddingLeft = 12f;
            panel.style.paddingRight = 12f;
            panel.style.backgroundColor = PanelColor;
            return panel;
        }

        private static Label NewLabel(float size, Color color)
        {
            var label = new Label();
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static void SetVisible(VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static string Join(params Label[] labels)
        {
            var text = new StringBuilder();
            foreach (Label label in labels)
            {
                if (label == null || string.IsNullOrEmpty(label.text) || label.style.display.value == DisplayStyle.None) { continue; }
                if (text.Length > 0) { text.Append('\n'); }
                text.Append(label.text);
            }
            return text.ToString();
        }
    }
}
