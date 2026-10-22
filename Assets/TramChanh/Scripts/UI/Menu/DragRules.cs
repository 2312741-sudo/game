using System;
using TramChanh.Core;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// The one place that decides what dropping a card on a slot means in the supplier shop. Pure C#, no UI Toolkit.
    /// A real drop calls the same buy functions as the "Mua" buttons (<see cref="Supplier.TryBuyPack"/>,
    /// <see cref="UpgradeBook.TryBuy"/>), so the buying law lives only in Core.
    /// </summary>
    public sealed class DragRules
    {
        private readonly SupplierContext _context;

        public DragRules(SupplierContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>Would a drop of this card on this zone buy something right now? Changes nothing.</summary>
        public DropVerdict Evaluate(DragPayload payload, DropZone zone)
        {
            if (zone != DropZone.Basket) { return DropVerdict.OutsideZone; }
            if (payload.Kind == DragKind.Ingredient)
            {
                Ingredient ingredient = payload.Ingredient;
                if (_context.Stock.Space(ingredient) < Supplier.PackSize) { return DropVerdict.StockFull; }
                if (_context.Wallet.Money < _context.Supplier.PackPrice(ingredient)) { return DropVerdict.NoMoney; }
                return DropVerdict.Valid;
            }
            switch (_context.Upgrades.StatusOf(payload.Upgrade, _context.Wallet, _context.Reputation))
            {
                case UpgradeStatus.Owned: return DropVerdict.AlreadyOwned;
                case UpgradeStatus.NeedsStars: return DropVerdict.NeedsStars;
                case UpgradeStatus.NeedsMoney: return DropVerdict.NoMoney;
                default: return DropVerdict.Valid;
            }
        }

        /// <summary>Drops the card: buys through Core when valid, otherwise changes nothing. The caller refreshes and saves.</summary>
        public DropOutcome Drop(DragPayload payload, DropZone zone)
        {
            DropVerdict verdict = Evaluate(payload, zone);
            if (verdict != DropVerdict.Valid) { return new DropOutcome(verdict, false, MessageFor(verdict)); }
            bool bought = payload.Kind == DragKind.Ingredient
                ? _context.Supplier.TryBuyPack(_context.Stock, _context.Wallet, payload.Ingredient)
                : _context.Upgrades.TryBuy(payload.Upgrade, _context.Wallet, _context.Reputation);
            if (!bought) { return new DropOutcome(DropVerdict.NoMoney, false, MessageFor(DropVerdict.NoMoney)); }
            return new DropOutcome(DropVerdict.Valid, true, "Đã mua: " + GhostText(payload));
        }

        /// <summary>Text on the ghost that follows the pointer.</summary>
        public static string GhostText(DragPayload payload)
        {
            return payload.Kind == DragKind.Ingredient
                ? IngredientUse.DisplayName(payload.Ingredient) + " +" + Supplier.PackSize
                : UpgradeBook.Info(payload.Upgrade).Name;
        }

        public static string MessageFor(DropVerdict verdict)
        {
            switch (verdict)
            {
                case DropVerdict.Valid: return "Thả để mua.";
                case DropVerdict.NoMoney: return "Không đủ tiền.";
                case DropVerdict.StockFull: return "Kho đã đầy.";
                case DropVerdict.NeedsStars: return "Chưa đủ sao uy tín.";
                case DropVerdict.AlreadyOwned: return "Đã có rồi.";
                default: return "Thả vào ô Giỏ mua để mua.";
            }
        }
    }
}
