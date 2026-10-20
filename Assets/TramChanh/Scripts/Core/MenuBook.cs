using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>One drink flavour that unlocks with reputation.</summary>
    public readonly struct MenuEntry
    {
        public MenuEntry(string id, string displayName, int requiredStars, int price)
        {
            Id = id;
            DisplayName = displayName;
            RequiredStars = requiredStars;
            Price = price;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int RequiredStars { get; }
        /// <summary>Price in VND.</summary>
        public int Price { get; }
    }

    /// <summary>
    /// The drink flavours that come after the basic drink, and when they unlock. Names and prices are the owner's real menu
    /// ("Thực đơn Trạm Chanh", photo IMG_4932 2, prices in thousand VND): the basic drink is Trà Olong - Chanh tươi at 20k; every
    /// other Trà Olong flavour is 30k. A flavour is a different order label with its own name and price; the drink itself is
    /// still made at the same station (drinks are matched by kind, not by flavour). The unlock order is the lead's choice.
    /// </summary>
    public static class MenuBook
    {
        public const string PassionfruitId = "drink.chanh_day";
        public const string MangoId = "drink.xoai";
        public const string PeachId = "drink.dao";
        public const string MixedBerryId = "drink.qua_mong";
        public const string SoursopId = "drink.mang_cau";
        public const int BaseDrinkPrice = 20000;
        public const int BaseCakePrice = 20000;

        private static readonly MenuEntry[] Flavours =
        {
            new MenuEntry(PassionfruitId, "Trà Olong - Chanh dây", 2, 30000),
            new MenuEntry(MangoId, "Trà Olong - Xoài", 3, 30000),
            new MenuEntry(PeachId, "Trà Olong - Đào", 4, 30000),
            new MenuEntry(MixedBerryId, "Trà Olong - Quả mọng", 5, 30000),
            new MenuEntry(SoursopId, "Trà Olong - Mãng cầu", 5, 30000),
        };

        public static IReadOnlyList<MenuEntry> Extra => Flavours;

        public static IEnumerable<string> ExtraIds()
        {
            foreach (MenuEntry entry in Flavours) { yield return entry.Id; }
        }

        /// <summary>The basic drink first, then every flavour whose star requirement is met.</summary>
        public static IReadOnlyList<string> UnlockedDrinkIds(string baseDrinkId, int stars)
        {
            var ids = new List<string>(Flavours.Length + 1);
            if (!string.IsNullOrEmpty(baseDrinkId)) { ids.Add(baseDrinkId); }
            foreach (MenuEntry entry in Flavours)
            {
                if (stars >= entry.RequiredStars) { ids.Add(entry.Id); }
            }
            return ids;
        }

        /// <summary>The flavours that unlock at exactly this star count.</summary>
        public static IReadOnlyList<MenuEntry> UnlockedAt(int stars)
        {
            var list = new List<MenuEntry>();
            foreach (MenuEntry entry in Flavours)
            {
                if (entry.RequiredStars == stars) { list.Add(entry); }
            }
            return list;
        }

        /// <summary>
        /// Liquid colour of a drink as 0xRRGGBB, from the project's tea liquid palette (Docs/Reference/TramChanh_3D_Asset_Pipeline.md):
        /// passion fruit #EDA417, the basic lemon tea amber #E79D32, mixed berry #9B232A. Mango and peach are the lead's choice
        /// (no reference colour exists), soursop a pale green. Returns -1 for ids that are not drinks.
        /// </summary>
        public static int LiquidColor(string itemId)
        {
            switch (itemId)
            {
                case PassionfruitId: return 0xEDA417;
                case MangoId: return 0xF3B33D;
                case PeachId: return 0xF08A5D;
                case MixedBerryId: return 0x9B232A;
                case SoursopId: return 0xC9D6A0;
                default: return itemId != null && itemId.StartsWith("item.drink") ? 0xE79D32 : -1;
            }
        }

        /// <summary>Unit price of one item: a flavour's own price, otherwise the base price of its kind.</summary>
        public static int UnitPrice(string itemId, ItemKind kind, PriceList prices)
        {
            foreach (MenuEntry entry in Flavours)
            {
                if (entry.Id == itemId) { return entry.Price; }
            }
            return prices.BasePrice(kind);
        }
    }
}
