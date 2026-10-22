using System;
using System.Collections.Generic;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>Everything the between-days shop shows and changes. The owner (the game) applies the effects.</summary>
    public sealed class SupplierContext
    {
        public StockBook Stock;
        public GameProgress Wallet;
        public Supplier Supplier;
        public UpgradeBook Upgrades;
        public Reputation Reputation;
        public Func<Difficulty> GetDifficulty;
        public Action CycleDifficulty;
        /// <summary>Runs after anything was bought or changed, so the game can apply the effects and save.</summary>
        public Action Changed;
        /// <summary>Text under the lists, for example what tomorrow night brings.</summary>
        public string Forecast = string.Empty;
        /// <summary>Tomorrow's night event, for its icon.</summary>
        public NightEvent ForecastNight = NightEvent.Clear;
    }

    /// <summary>
    /// Between-days shop: shows the stock and the cash, one "Mua" button per ingredient pack and a button to open the
    /// stall. Pure view; the owner decides what a purchase does and pauses and resumes the game.
    /// </summary>
    public sealed class SupplierPanel
    {
        private readonly VisualElement _panel;
        private readonly Label _cash;
        private readonly Label _message;
        private readonly Dictionary<Ingredient, Label> _rowLabels = new Dictionary<Ingredient, Label>();
        private readonly Dictionary<Ingredient, Button> _buyButtons = new Dictionary<Ingredient, Button>();
        private readonly Dictionary<UpgradeId, Label> _upgradeLabels = new Dictionary<UpgradeId, Label>();
        private readonly Dictionary<UpgradeId, Button> _upgradeButtons = new Dictionary<UpgradeId, Button>();
        private readonly SupplierContext _context;
        private Label _stars;
        private VisualElement _starRow;
        private Image _nightIcon;
        private Button _difficulty;
        private readonly DragSession _drag;
        private VisualElement _basket;
        private Label _basketText;
        private Label _ghost;

        public SupplierPanel(VisualElement root, SupplierContext context, Action onDone)
        {
            _context = context;
            _drag = new DragSession(new DragRules(context));
            Func<Ingredient, bool> onBuy = ingredient =>
            {
                bool bought = context.Supplier.TryBuyPack(context.Stock, context.Wallet, ingredient);
                Refresh(bought ? string.Empty : "Không đủ tiền hoặc kho đã đầy.");
                if (bought) { context.Changed?.Invoke(); }
                return bought;
            };
            _panel = new VisualElement { name = "Supplier" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f;
            _panel.style.right = 0f;
            _panel.style.top = 0f;
            _panel.style.bottom = 0f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;
            _panel.style.display = DisplayStyle.None;

            _ghost = new Label();
            _ghost.pickingMode = PickingMode.Ignore;
            _ghost.style.position = Position.Absolute;
            _ghost.style.display = DisplayStyle.None;
            _ghost.style.fontSize = 16f;
            _ghost.style.unityFontStyleAndWeight = FontStyle.Bold;
            _ghost.style.color = Color.white;
            _ghost.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 0.9f);
            _ghost.style.paddingLeft = 12f;
            _ghost.style.paddingRight = 12f;
            _ghost.style.paddingTop = 6f;
            _ghost.style.paddingBottom = 6f;
            SetBorder(_ghost, new Color(1f, 0.85f, 0.4f, 1f), 1.5f, 10f);

            var box = new VisualElement();
            box.style.width = 560f;
            box.style.paddingTop = 22f;
            box.style.paddingBottom = 22f;
            box.style.paddingLeft = 26f;
            box.style.paddingRight = 26f;
            box.style.backgroundColor = new Color(0.08f, 0.18f, 0.13f, 0.96f);
            SetBorder(box, new Color(0.95f, 0.48f, 0.10f, 0.9f), 2.5f, 16f);
            _panel.Add(box);

            var title = new Label("Nhà cung cấp");
            title.style.fontSize = 26f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.99f, 0.78f, 0.15f, 1f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(title);

            _cash = new Label();
            _cash.style.fontSize = 18f;
            _cash.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
            _cash.style.unityTextAlign = TextAnchor.MiddleCenter;
            var cashRow = new VisualElement();
            cashRow.style.flexDirection = FlexDirection.Row;
            cashRow.style.justifyContent = Justify.Center;
            cashRow.style.alignItems = Align.Center;
            cashRow.style.marginTop = 6f;
            cashRow.style.marginBottom = 10f;
            Image moneyIcon = UiIcons.Make(UiIcons.Money, 26f);
            moneyIcon.style.marginRight = 8f;
            cashRow.Add(moneyIcon);
            cashRow.Add(_cash);
            box.Add(cashRow);

            foreach (Ingredient ingredient in StockBook.Ingredients)
            {
                Ingredient captured = ingredient;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 4f;
                var label = new Label();
                label.style.flexGrow = 1f;
                label.style.fontSize = 17f;
                label.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
                Image ingredientIcon = UiIcons.Make(UiIcons.NameFor(ingredient), 30f);
                ingredientIcon.style.marginRight = 8f;
                VisualElement card = MakeDragCard(DragPayload.ForIngredient(ingredient));
                card.Add(ingredientIcon);
                card.Add(label);
                row.Add(card);
                var buy = new Button(() => { if (onBuy != null) { onBuy(captured); } }) { text = "Mua " + Supplier.PackSize };
                buy.style.width = 96f;
                buy.style.height = 32f;
                buy.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f);
                buy.style.color = Color.white;
                row.Add(buy);
                box.Add(row);
                _rowLabels[ingredient] = label;
                _buyButtons[ingredient] = buy;
            }

            _stars = new Label();
            _stars.style.fontSize = 18f;
            _stars.style.color = new Color(0.99f, 0.78f, 0.15f, 1f);
            _stars.style.unityTextAlign = TextAnchor.MiddleCenter;
            _stars.style.marginTop = 12f;
            box.Add(_stars);
            _starRow = new VisualElement();
            _starRow.style.flexDirection = FlexDirection.Row;
            _starRow.style.justifyContent = Justify.Center;
            _starRow.style.marginTop = 2f;
            box.Add(_starRow);

            foreach (UpgradeInfo info in UpgradeBook.All)
            {
                UpgradeId captured = info.Id;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 3f;
                var label = new Label();
                label.style.flexGrow = 1f;
                label.style.fontSize = 15f;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
                Image upgradeIcon = UiIcons.Make(UiIcons.NameFor(info.Id), 30f);
                upgradeIcon.style.marginRight = 8f;
                VisualElement card = MakeDragCard(DragPayload.ForUpgrade(captured));
                card.Add(upgradeIcon);
                card.Add(label);
                row.Add(card);
                var buy = new Button(() =>
                {
                    bool bought = context.Upgrades.TryBuy(captured, context.Wallet, context.Reputation);
                    Refresh(bought ? "Đã mua: " + UpgradeBook.Info(captured).Name : "Chưa mua được.");
                    if (bought) { context.Changed?.Invoke(); }
                }) { text = "Mua" };
                buy.style.width = 96f;
                buy.style.height = 30f;
                buy.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f);
                buy.style.color = Color.white;
                row.Add(buy);
                box.Add(row);
                _upgradeLabels[info.Id] = label;
                _upgradeButtons[info.Id] = buy;
            }

            _basket = new VisualElement { name = "SupplierBasket" };
            _basket.style.marginTop = 12f;
            _basket.style.height = 46f;
            _basket.style.justifyContent = Justify.Center;
            _basket.style.alignItems = Align.Center;
            _basketText = new Label(IdleBasketText);
            _basketText.style.fontSize = 15f;
            _basketText.pickingMode = PickingMode.Ignore;
            _basket.Add(_basketText);
            SetBasket(null, false);
            box.Add(_basket);

            _difficulty = new Button(() => { context.CycleDifficulty?.Invoke(); Refresh(string.Empty); context.Changed?.Invoke(); });
            _difficulty.style.height = 32f;
            _difficulty.style.marginTop = 12f;
            _difficulty.style.backgroundColor = new Color(0.2f, 0.45f, 0.32f, 1f);
            _difficulty.style.color = Color.white;
            box.Add(_difficulty);

            _nightIcon = UiIcons.Make(UiIcons.NameFor(NightEvent.Clear), 34f);
            _nightIcon.style.alignSelf = Align.Center;
            _nightIcon.style.marginTop = 10f;
            box.Add(_nightIcon);

            _message = new Label();
            _message.style.fontSize = 15f;
            _message.style.color = new Color(1f, 0.85f, 0.4f);
            _message.style.marginTop = 10f;
            _message.style.unityTextAlign = TextAnchor.MiddleCenter;
            _message.style.whiteSpace = WhiteSpace.Normal;
            box.Add(_message);

            var done = new Button(() => onDone?.Invoke()) { text = "Mở hàng" };
            done.style.fontSize = 19f;
            done.style.unityFontStyleAndWeight = FontStyle.Bold;
            done.style.height = 44f;
            done.style.marginTop = 14f;
            done.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f);
            done.style.color = Color.white;
            box.Add(done);

            _panel.Add(_ghost);

            root.Add(_panel);
        }

        private const string IdleBasketText = "Giỏ mua: kéo thẻ vào đây";
        private static readonly Color BasketIdle = new Color(0.18f, 0.34f, 0.25f, 0.9f);
        private static readonly Color BasketReady = new Color(0.95f, 0.48f, 0.10f, 0.9f);
        private static readonly Color BasketValid = new Color(0.2f, 0.7f, 0.35f, 1f);
        private static readonly Color BasketInvalid = new Color(0.8f, 0.2f, 0.2f, 1f);

        /// <summary>A draggable card (icon plus text). Dropping it on the basket buys through <see cref="DragRules"/>.</summary>
        private VisualElement MakeDragCard(DragPayload payload)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.flexGrow = 1f;
            var drag = new DragCardManipulator(_drag, payload, _panel, _ghost, ZoneAt);
            drag.DragStarted += () => { card.style.opacity = 0.45f; SetBasket(null); };
            drag.Hovering += (zone, verdict) =>
            {
                if (zone == DropZone.Basket) { SetBasket(verdict); }
                else { SetBasket(null); }
            };
            drag.Dropped += outcome =>
            {
                Refresh(outcome.Message);
                if (outcome.Bought) { _context.Changed?.Invoke(); }
            };
            drag.Ended += () => { card.style.opacity = 1f; SetBasket(null, false); };
            card.AddManipulator(drag);
            return card;
        }

        private DropZone ZoneAt(Vector2 panelPosition)
        {
            return _basket.worldBound.Contains(panelPosition) ? DropZone.Basket : DropZone.None;
        }

        /// <summary>Basket look: null = nothing hovering (lit orange while a drag runs), Valid = green, any refusal = red.</summary>
        private void SetBasket(DropVerdict? verdict, bool dragging = true)
        {
            Color color = !dragging ? BasketIdle : verdict == null ? BasketReady : verdict.Value == DropVerdict.Valid ? BasketValid : BasketInvalid;
            _basket.style.backgroundColor = new Color(color.r, color.g, color.b, 0.35f);
            SetBorder(_basket, color, 2f, 10f);
            _basketText.text = verdict == null || !dragging ? IdleBasketText : DragRules.MessageFor(verdict.Value);
            _basketText.style.color = Color.white;
        }

        public bool IsVisible => _panel.style.display.value == DisplayStyle.Flex;

        /// <summary>Text of one stock row, for example "Đá   12/40   (gói 10 = 5.000đ)".</summary>
        public static string DescribeRow(Ingredient ingredient, StockBook stock, Supplier supplier)
        {
            return IngredientUse.DisplayName(ingredient) + "   " + stock.Level(ingredient) + "/" + stock.Capacity(ingredient)
                + "   (gói " + Supplier.PackSize + " = " + PriceList.Format(supplier.PackPrice(ingredient)) + ")";
        }

        public static string DescribeCash(GameProgress wallet) => "Tiền đang có: " + PriceList.Format(wallet.Money);

        public static string DescribeUpgrade(UpgradeInfo info, UpgradeStatus status)
        {
            string state;
            switch (status)
            {
                case UpgradeStatus.Owned: state = "đã có"; break;
                case UpgradeStatus.NeedsStars: state = "cần " + info.RequiredStars + " sao"; break;
                default: state = PriceList.Format(info.Cost); break;
            }
            return info.Name + ": " + info.Effect + " (" + state + ")";
        }

        public void Show(string message = "")
        {
            Refresh(message);
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Refresh(string message = "")
        {
            SupplierContext c = _context;
            _cash.text = DescribeCash(c.Wallet);
            foreach (Ingredient ingredient in StockBook.Ingredients)
            {
                _rowLabels[ingredient].text = DescribeRow(ingredient, c.Stock, c.Supplier);
                _buyButtons[ingredient].SetEnabled(c.Stock.Space(ingredient) >= Supplier.PackSize && c.Wallet.Money >= c.Supplier.PackPrice(ingredient));
            }
            _stars.text = "Uy tín";
            _starRow.Clear();
            for (int i = 0; i < 5; i++)
            {
                Image star = UiIcons.Make(i < c.Reputation.Stars ? UiIcons.StarFull : UiIcons.StarEmpty, 28f);
                star.style.marginLeft = 2f;
                star.style.marginRight = 2f;
                _starRow.Add(star);
            }
            Texture2D nightTexture = UiIcons.Load(UiIcons.NameFor(c.ForecastNight));
            if (nightTexture != null) { _nightIcon.image = nightTexture; _nightIcon.style.display = DisplayStyle.Flex; }
            else { _nightIcon.style.display = DisplayStyle.None; }
            foreach (UpgradeInfo info in UpgradeBook.All)
            {
                UpgradeStatus status = c.Upgrades.StatusOf(info.Id, c.Wallet, c.Reputation);
                _upgradeLabels[info.Id].text = DescribeUpgrade(info, status);
                _upgradeButtons[info.Id].SetEnabled(status == UpgradeStatus.Available);
            }
            _difficulty.text = "Độ khó: " + Difficulties.Profile(c.GetDifficulty()).Name + "  (bấm để đổi)";
            _message.text = string.IsNullOrEmpty(message) ? c.Forecast : message + "\n" + c.Forecast;
        }

        public void Hide() { _panel.style.display = DisplayStyle.None; }

        private static void SetBorder(VisualElement e, Color color, float width, float radius)
        {
            e.style.borderTopWidth = width; e.style.borderBottomWidth = width; e.style.borderLeftWidth = width; e.style.borderRightWidth = width;
            e.style.borderTopColor = color; e.style.borderBottomColor = color; e.style.borderLeftColor = color; e.style.borderRightColor = color;
            e.style.borderTopLeftRadius = radius; e.style.borderTopRightRadius = radius; e.style.borderBottomLeftRadius = radius; e.style.borderBottomRightRadius = radius;
        }
    }
}
