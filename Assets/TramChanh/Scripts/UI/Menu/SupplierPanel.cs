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
        private Button _difficulty;

        public SupplierPanel(VisualElement root, SupplierContext context, Action onDone)
        {
            _context = context;
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
            _cash.style.marginTop = 6f;
            _cash.style.marginBottom = 10f;
            box.Add(_cash);

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
                var buy = new Button(() => { if (onBuy != null) { onBuy(captured); } }) { text = "Mua " + Supplier.PackSize };
                buy.style.width = 96f;
                buy.style.height = 32f;
                buy.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f);
                buy.style.color = Color.white;
                row.Add(label);
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
                row.Add(label);
                row.Add(buy);
                box.Add(row);
                _upgradeLabels[info.Id] = label;
                _upgradeButtons[info.Id] = buy;
            }

            _difficulty = new Button(() => { context.CycleDifficulty?.Invoke(); Refresh(string.Empty); context.Changed?.Invoke(); });
            _difficulty.style.height = 32f;
            _difficulty.style.marginTop = 12f;
            _difficulty.style.backgroundColor = new Color(0.2f, 0.45f, 0.32f, 1f);
            _difficulty.style.color = Color.white;
            box.Add(_difficulty);

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

            root.Add(_panel);
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
            _stars.text = "Uy tín: " + Reputation.StarText(c.Reputation.Stars);
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
