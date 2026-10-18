using System;
using System.Collections.Generic;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
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

        public SupplierPanel(VisualElement root, Func<Ingredient, bool> onBuy, Action onDone)
        {
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
            box.style.width = 520f;
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

        public void Show(StockBook stock, GameProgress wallet, Supplier supplier, string message = "")
        {
            Refresh(stock, wallet, supplier, message);
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Refresh(StockBook stock, GameProgress wallet, Supplier supplier, string message = "")
        {
            _cash.text = DescribeCash(wallet);
            foreach (Ingredient ingredient in StockBook.Ingredients)
            {
                _rowLabels[ingredient].text = DescribeRow(ingredient, stock, supplier);
                _buyButtons[ingredient].SetEnabled(stock.Space(ingredient) >= Supplier.PackSize && wallet.Money >= supplier.PackPrice(ingredient));
            }
            _message.text = message ?? string.Empty;
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
