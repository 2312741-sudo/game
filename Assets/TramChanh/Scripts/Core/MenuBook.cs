using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>One drink flavour that unlocks with reputation.</summary>
    public readonly struct MenuEntry
    {
        public MenuEntry(string id, string displayName, int requiredStars)
        {
            Id = id;
            DisplayName = displayName;
            RequiredStars = requiredStars;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int RequiredStars { get; }
    }

    /// <summary>
    /// The drink flavours that come after the basic drink, and when they unlock. A flavour is a different order label with its
    /// own name; the drink itself is still made at the same station (drinks are matched by kind, not by flavour).
    /// Flavours and names are the owner's (photos IMG_5213 and IMG_5214; the second one is named "trà ô long chanh tươi" by the owner).
    /// Prices stay the base drink price until the owner gives the real prices.
    /// </summary>
    public static class MenuBook
    {
        public const string PassionfruitId = "drink.chanh_day";
        public const string OolongLemonId = "drink.o_long_chanh_tuoi";

        private static readonly MenuEntry[] Flavours =
        {
            new MenuEntry(PassionfruitId, "Trà chanh dây", 2),
            new MenuEntry(OolongLemonId, "Trà ô long chanh tươi", 3),
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

        /// <summary>The flavour that unlocks at exactly this star count, or null.</summary>
        public static MenuEntry? UnlockedAt(int stars)
        {
            foreach (MenuEntry entry in Flavours)
            {
                if (entry.RequiredStars == stars) { return entry; }
            }
            return null;
        }
    }
}
