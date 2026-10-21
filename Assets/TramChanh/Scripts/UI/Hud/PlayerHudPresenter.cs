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
    /// All colours and sizes come from <see cref="HudTheme"/>.
    /// </summary>
    public sealed class PlayerHudPresenter : IDisposable
    {
        private static readonly Color HintColor = HudTheme.Hint;
        private static readonly Color DimColor = HudTheme.TextDim;
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
        private readonly System.Collections.Generic.Dictionary<OrderId, VisualElement> _patienceFills = new System.Collections.Generic.Dictionary<OrderId, VisualElement>();
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

        /// <summary>
        /// Returns the share of patience left (0 to 1) for an order, or a negative number when it is not tracked.
        /// Set by the game; the HUD only reads it.
        /// </summary>
        public Func<OrderId, double> PatienceProvider { get; set; }

        /// <summary>Returns the icon name (Resources/UI/Icons) of an order's customer kind, or null. Set by the game; the HUD only reads it.</summary>
        public Func<OrderId, string> CustomerIconProvider { get; set; }

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
                    foreach (Label label in _ordersRows.Query<Label>().ToList())
                    {
                        if (!string.IsNullOrEmpty(label.text)) { text.Append('\n').Append(label.text); }
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
                UpdatePatienceBars();
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
                _heldPanel = Panel(330f);
                _heldPanel.style.right = HudTheme.Margin;
                _heldPanel.style.top = HudTheme.RightColumnTop;
                var title = NewLabel(HudTheme.SmallSize, DimColor);
                title.text = Text(HudText.Of(HudKeys.HeldTitle));
                _heldName = NewLabel(20f, HudTheme.Text);
                _heldName.style.unityFontStyleAndWeight = FontStyle.Bold;
                _heldState = NewLabel(HudTheme.BodySize, HudTheme.Text);
                _heldNext = NewLabel(HudTheme.BodySize, HintColor);
                _heldNext.style.unityFontStyleAndWeight = FontStyle.Bold;
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
                _ordersPanel = Panel(380f);
                _ordersPanel.style.left = HudTheme.Margin;
                _ordersPanel.style.top = HudTheme.LeftColumnTop;
                _ordersTitle = NewLabel(HudTheme.TitleSize, TramChanh.UI.Menu.UiStyle.Gold);
                _ordersTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                _ordersNext = NewLabel(16f, HintColor);
                _ordersNext.style.unityFontStyleAndWeight = FontStyle.Bold;
                _ordersNext.style.marginBottom = 6f;
                _ordersRows = new VisualElement();
                _ordersMore = NewLabel(13f, DimColor);
                _ordersPanel.Add(_ordersTitle);
                _ordersPanel.Add(_ordersNext);
                _ordersPanel.Add(_ordersRows);
                _ordersPanel.Add(_ordersMore);
                root.Add(_ordersPanel);

                _toastLabel = NewLabel(22f, Color.white);
                HudTheme.StyleToast(_toastLabel, 560f, true);
                _toastLabel.style.top = Length.Percent(20f);
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
            _patienceFills.Clear();
            foreach (OrderRow row in _orders.Rows)
            {
                var card = new VisualElement();
                card.pickingMode = PickingMode.Ignore;
                card.style.marginTop = 6f;
                card.style.paddingTop = 5f; card.style.paddingBottom = 5f; card.style.paddingLeft = 8f; card.style.paddingRight = 8f;
                card.style.backgroundColor = row.IsPrimary ? HudTheme.PrimaryCard : HudTheme.PlainCard;
                TramChanh.UI.Menu.UiStyle.Round(card, 8f);
                // The coloured left edge tells the stage at a glance; the primary order gets a thicker one.
                card.style.borderLeftWidth = row.IsPrimary ? 5f : 3f;
                card.style.borderLeftColor = HudTheme.ToneColor(HudTheme.ToneFor(row.Status));
                _ordersRows.Add(card);
                Label header = NewLabel(16f, row.IsPrimary ? HintColor : HudTheme.Text);
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.text = Text(row.Header);
                card.Add(header);
                var items = new StringBuilder();
                foreach (HudText item in row.Items)
                {
                    if (items.Length > 0) { items.Append(", "); }
                    items.Append(Text(item));
                }
                Label itemLabel = NewLabel(14f, DimColor);
                itemLabel.text = items.ToString();
                int firstDrink = -1;
                foreach (string itemId in row.ItemIds)
                {
                    if (MenuBook.LiquidColor(itemId) >= 0) { firstDrink = MenuBook.LiquidColor(itemId); break; }
                }
                if (firstDrink >= 0)
                {
                    // A swatch in the drink's liquid colour, so flavours are told apart at a glance.
                    var swatchLine = new VisualElement();
                    swatchLine.pickingMode = PickingMode.Ignore;
                    swatchLine.style.flexDirection = FlexDirection.Row;
                    swatchLine.style.alignItems = Align.Center;
                    var swatch = new VisualElement();
                    swatch.pickingMode = PickingMode.Ignore;
                    swatch.style.width = 14f;
                    swatch.style.height = 14f;
                    swatch.style.marginRight = 6f;
                    swatch.style.borderTopLeftRadius = 7f;
                    swatch.style.borderTopRightRadius = 7f;
                    swatch.style.borderBottomLeftRadius = 7f;
                    swatch.style.borderBottomRightRadius = 7f;
                    swatch.style.backgroundColor = new Color(((firstDrink >> 16) & 255) / 255f, ((firstDrink >> 8) & 255) / 255f, (firstDrink & 255) / 255f, 1f);
                    swatchLine.Add(swatch);
                    swatchLine.Add(itemLabel);
                    card.Add(swatchLine);
                }
                else
                {
                    card.Add(itemLabel);
                }
                Label next = NewLabel(14f, row.IsPrimary ? HintColor : DimColor);
                next.text = Text(row.NextStep);
                card.Add(next);
                card.Add(NewPatienceBar(row.Id));
            }
            UpdatePatienceBars();
            _ordersMore.text = _orders.HiddenCount > 0 ? Text(HudText.Of(HudKeys.OrdersMore, _orders.HiddenCount)) : string.Empty;
            SetVisible(_ordersMore, _orders.HiddenCount > 0);
        }

        private VisualElement NewPatienceBar(OrderId id)
        {
            var line = new VisualElement();
            line.pickingMode = PickingMode.Ignore;
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;
            line.style.marginTop = 2f;
            string iconName = CustomerIconProvider != null ? CustomerIconProvider(id) : null;
            if (!string.IsNullOrEmpty(iconName))
            {
                Image icon = TramChanh.UI.Menu.UiIcons.Make(iconName, 18f);
                icon.style.marginRight = 6f;
                line.Add(icon);
            }
            var track = new VisualElement();
            track.pickingMode = PickingMode.Ignore;
            track.name = "PatienceTrack";
            track.style.height = 6f;
            track.style.flexGrow = 1f;
            track.style.backgroundColor = HudTheme.Track;
            track.style.height = 8f;
            TramChanh.UI.Menu.UiStyle.Round(track, 4f);
            track.style.overflow = Overflow.Hidden;
            var fill = new VisualElement();
            fill.pickingMode = PickingMode.Ignore;
            fill.name = "PatienceFill";
            fill.style.height = Length.Percent(100f);
            track.Add(fill);
            line.Add(track);
            _patienceFills[id] = fill;
            return line;
        }

        private void UpdatePatienceBars()
        {
            foreach (System.Collections.Generic.KeyValuePair<OrderId, VisualElement> pair in _patienceFills)
            {
                PatienceBar bar = PatienceBar.For(PatienceProvider != null ? PatienceProvider(pair.Key) : -1d);
                VisualElement fill = pair.Value;
                VisualElement track = fill.parent;
                if (track != null) { SetVisible(track, bar.IsVisible); }
                if (!bar.IsVisible) { continue; }
                fill.style.width = Length.Percent(bar.WidthPercent);
                fill.style.backgroundColor = HudTheme.PatienceColor(bar.Level);
            }
        }

        private void RenderToast()
        {
            if (_toast == null || _toastLabel == null) { return; }
            _toastLabel.text = Text(_toast.Current);
            _toastLabel.style.backgroundColor = HudTheme.ToastColor(_toast.IsPositive);
            SetVisible(_toastLabel, _toast.IsVisible);
        }

        private static VisualElement Panel(float width) => HudTheme.NewPanel(width);

        private static Label NewLabel(float size, Color color) => HudTheme.NewLabel(size, color);

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
