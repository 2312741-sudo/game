using System.Collections.Generic;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// The 24 UI icons (Resources/UI/Icons). A missing icon is never an error: the caller gets null and shows text only.
    /// </summary>
    public static class UiIcons
    {
        private const string Folder = "UI/Icons/";
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public const string Money = "IC_Money";
        public const string StarFull = "IC_Star_Full";
        public const string StarEmpty = "IC_Star_Empty";
        public const string Patience = "IC_Patience";
        public const string Goal = "IC_Goal";

        public static string NameFor(Ingredient ingredient)
        {
            switch (ingredient)
            {
                case Ingredient.Ice: return "IC_Ice";
                case Ingredient.Jelly: return "IC_Jelly";
                case Ingredient.Batter: return "IC_Batter";
                default: return "IC_Wrap";
            }
        }

        public static string NameFor(UpgradeId upgrade)
        {
            switch (upgrade)
            {
                case UpgradeId.BiggerIceBin: return "IC_Up_BiggerIceBin";
                case UpgradeId.Pantry: return "IC_Up_Pantry";
                case UpgradeId.StringLights: return "IC_Up_StringLights";
                case UpgradeId.WholesaleDeal: return "IC_Up_WholesaleDeal";
                default: return "IC_Up_GoldenMenuBoard";
            }
        }

        public static string NameFor(NightEvent night)
        {
            switch (night)
            {
                case NightEvent.Rain: return "IC_Rain";
                case NightEvent.HotNight: return "IC_Hot";
                case NightEvent.PowerCut: return "IC_PowerCut";
                case NightEvent.Festival: return "IC_Festival";
                case NightEvent.Inspector: return "IC_Inspector";
                default: return "IC_Clear";
            }
        }

        public static string NameFor(CustomerKind kind)
        {
            switch (kind)
            {
                case CustomerKind.Student: return "IC_Cust_Student";
                case CustomerKind.Family: return "IC_Cust_Family";
                case CustomerKind.Office: return "IC_Cust_Office";
                default: return "IC_Cust_Regular";
            }
        }

        /// <summary>Loads (and caches) an icon; null when the file is missing.</summary>
        public static Texture2D Load(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            if (Cache.TryGetValue(name, out Texture2D cached)) { return cached; }
            Texture2D texture = Resources.Load<Texture2D>(Folder + name);
            if (texture != null) { Cache[name] = texture; }
            return texture;
        }

        /// <summary>A square icon element of <paramref name="size"/> pixels; hidden when the icon file is missing.</summary>
        public static Image Make(string name, float size)
        {
            var image = new Image();
            image.pickingMode = PickingMode.Ignore;
            image.style.width = size;
            image.style.height = size;
            image.style.flexShrink = 0f;
            Texture2D texture = Load(name);
            if (texture != null) { image.image = texture; }
            else { image.style.display = DisplayStyle.None; }
            return image;
        }
    }
}
