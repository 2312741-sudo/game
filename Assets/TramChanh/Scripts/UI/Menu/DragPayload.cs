using TramChanh.Core;

namespace TramChanh.UI.Menu
{
    /// <summary>What a dragged card carries: one ingredient pack or one upgrade. Pure data.</summary>
    public readonly struct DragPayload
    {
        private DragPayload(DragKind kind, Ingredient ingredient, UpgradeId upgrade)
        {
            Kind = kind;
            Ingredient = ingredient;
            Upgrade = upgrade;
        }

        public DragKind Kind { get; }
        public Ingredient Ingredient { get; }
        public UpgradeId Upgrade { get; }

        public static DragPayload ForIngredient(Ingredient ingredient) => new DragPayload(DragKind.Ingredient, ingredient, default);
        public static DragPayload ForUpgrade(UpgradeId upgrade) => new DragPayload(DragKind.Upgrade, default, upgrade);
    }
}
