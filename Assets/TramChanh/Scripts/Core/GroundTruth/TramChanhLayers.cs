namespace TramChanh.Core.GroundTruth
{
    /// <summary>
    /// Physics layers used by the project (ASSET_INTEGRATION.md §6).
    /// Created by the editor project setup at these fixed indices.
    /// </summary>
    public static class TramChanhLayers
    {
        public const string Environment = "Environment";
        public const string Interactable = "Interactable";
        public const string Player = "Player";
        public const string Npc = "NPC";
        public const string HeldItem = "HeldItem";

        public const int EnvironmentIndex = 8;
        public const int InteractableIndex = 9;
        public const int PlayerIndex = 10;
        public const int NpcIndex = 11;
        public const int HeldItemIndex = 12;

        public static readonly (int Index, string Name)[] All =
        {
            (EnvironmentIndex, Environment),
            (InteractableIndex, Interactable),
            (PlayerIndex, Player),
            (NpcIndex, Npc),
            (HeldItemIndex, HeldItem),
        };
    }
}
